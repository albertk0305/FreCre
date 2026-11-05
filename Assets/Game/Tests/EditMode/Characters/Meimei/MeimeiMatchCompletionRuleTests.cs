using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiMatchCompletionRuleTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [TestCase(
            BoardMatchTier.Three,
            MeimeiCup.First,
            0)]
        [TestCase(
            BoardMatchTier.Four,
            MeimeiCup.Second,
            1)]
        [TestCase(
            BoardMatchTier.FiveOrMore,
            MeimeiCup.Final,
            2)]
        public void CompletedMatchGrantsExpectedCupAndTipsiness(
            BoardMatchTier tier,
            MeimeiCup expectedCup,
            int expectedTipsiness)
        {
            TestBattle battle = CreateBattle();

            CombatActionExecutionResult result = battle.ExecuteMatch(tier);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(battle.State.HasCup(expectedCup), Is.True);
            Assert.That(battle.State.CupCount, Is.EqualTo(1));
            Assert.That(battle.State.Tipsiness,
                Is.EqualTo(expectedTipsiness));
        }

        [TestCase(
            BoardMatchTier.Three,
            BoardMatchTier.Four,
            BoardMatchTier.FiveOrMore)]
        [TestCase(
            BoardMatchTier.Three,
            BoardMatchTier.FiveOrMore,
            BoardMatchTier.Four)]
        [TestCase(
            BoardMatchTier.Four,
            BoardMatchTier.Three,
            BoardMatchTier.FiveOrMore)]
        [TestCase(
            BoardMatchTier.Four,
            BoardMatchTier.FiveOrMore,
            BoardMatchTier.Three)]
        [TestCase(
            BoardMatchTier.FiveOrMore,
            BoardMatchTier.Three,
            BoardMatchTier.Four)]
        [TestCase(
            BoardMatchTier.FiveOrMore,
            BoardMatchTier.Four,
            BoardMatchTier.Three)]
        public void EveryCupOrderCreatesOneAdditionalAndResetsCups(
            BoardMatchTier first,
            BoardMatchTier second,
            BoardMatchTier third)
        {
            TestBattle battle = CreateBattle();

            Assert.That(AdditionalResults(battle.ExecuteMatch(first)),
                Is.Empty);
            Assert.That(AdditionalResults(battle.ExecuteMatch(second)),
                Is.Empty);
            CombatActionExecutionResult completed =
                battle.ExecuteMatch(third);

            Assert.That(AdditionalResults(completed), Has.Length.EqualTo(1));
            Assert.That(battle.State.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(battle.State.CupCount, Is.Zero);
            Assert.That(battle.State.Tipsiness, Is.EqualTo(3));
        }

        [Test]
        public void DuplicateCupIsIgnoredWhileTipsinessStillClampsAtFive()
        {
            TestBattle battle = CreateBattle();

            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
            battle.ExecuteMatch(BoardMatchTier.Three);
            battle.ExecuteMatch(BoardMatchTier.Three);

            Assert.That(battle.State.Cups,
                Is.EqualTo(MeimeiCup.First | MeimeiCup.Final));
            Assert.That(battle.State.CupCount, Is.EqualTo(2));
            Assert.That(battle.State.Tipsiness, Is.EqualTo(5));
        }

        [TestCase(0, 10.00d)]
        [TestCase(5, 12.00d)]
        public void CompletedCupsCreateCanonicalAdditional(
            int awakening,
            double expectedCoefficient)
        {
            TestBattle battle = CreateBattle(awakening);
            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);

            CombatActionExecutionResult result = battle.ExecuteMatch(
                BoardMatchTier.FiveOrMore);
            DamageActionResult additional =
                AdditionalResults(result).Single();
            var action = (DamageAction)additional.Action;

            Assert.That(action.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(action.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Additional));
            Assert.That(action.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Fire));
            Assert.That(action.ContextRequest.AttackTags,
                Is.EqualTo(AttackTag.None));
            Assert.That(action.ContextRequest.SkillCoefficient,
                Is.EqualTo(expectedCoefficient));
            Assert.That(action.ContextRequest.AppliesCombo, Is.False);
            Assert.That(action.ContextRequest.FinalComboCount, Is.Zero);
            Assert.That(action.ContextRequest.CanCritical, Is.True);
            Assert.That(action.CanTriggerFollowUp, Is.False);
            Assert.That(action.RootActionId,
                Is.EqualTo(result.ActionResults[0].Action.RootActionId));
            Assert.That(action.SourceActionId,
                Is.EqualTo(result.ActionResults[0].Action.ActionId));
            Assert.That(additional.DamageResult.ComboMultiplier,
                Is.EqualTo(1d));
            Assert.That(additional.Context.IsCritical, Is.False);
            Assert.That(additional.DamageResult.FinalDamage,
                Is.EqualTo((long)(expectedCoefficient * 1000d)));
            Assert.That(battle.State.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(battle.State.Tipsiness, Is.EqualTo(2));
        }

        [Test]
        public void AdditionalUsesExistingAttackTypeDamageModifier()
        {
            TestBattle battle = CreateBattle();
            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);
            battle.Meimei.Effects.ApplyEffect(new EffectInstance(
                runtimeId: 1,
                effectId: "effect_test_additional_increase",
                sourceId: battle.Meimei.CharacterId,
                category: EffectCategory.Buff,
                modifierType: EffectModifierType.AttackTypeDamageIncrease,
                magnitude: 0.25d,
                remainingTurns: null,
                creationOrder: 1,
                attackTypeMask: AttackTypeMask.Additional));

            DamageActionResult additional = AdditionalResults(
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore)).Single();

            Assert.That(additional.Context
                    .AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
            Assert.That(additional.DamageResult.FinalDamage,
                Is.EqualTo(12500L));
        }

        [Test]
        public void AdditionalExecutesBeforeNextQueuedCharacterAction()
        {
            TestBattle battle = CreateBattle(includeAlly: true);
            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);
            DamageAction meimeiMatch = battle.CreateMatch(
                BoardMatchTier.FiveOrMore);
            DamageAction allyMatch = battle.CreateAllyMatch();

            CombatActionExecutionResult result = battle.Execute(
                new CombatAction[] { meimeiMatch, allyMatch });

            Assert.That(result.ActionResults.Select(item => item.Action
                    .Origin),
                Is.EqualTo(new[]
                {
                    ActionOrigin.Match,
                    ActionOrigin.Additional,
                    ActionOrigin.Match
                }));
            Assert.That(result.ActionResults.Select(item => item.Action
                    .ActionId),
                Is.EqualTo(new long[] { 1L, 3L, 2L }));
        }

        [Test]
        public void DuplicateCompletionContextIsProcessedOnlyOnce()
        {
            TestBattle battle = CreateBattle();
            var observer = new ContextObserverRule();
            DamageAction match = battle.CreateMatch(BoardMatchTier.Four);

            battle.Execute(
                new CombatAction[] { match },
                observer,
                battle.Rule);
            int tipsiness = battle.State.Tipsiness;
            long lastIssuedId = battle.ActionIds.LastIssuedId;
            IReadOnlyList<CombatAction> duplicate =
                battle.Rule.CreateDerivedActions(observer.Context);

            Assert.That(duplicate, Is.Empty);
            Assert.That(battle.State.Tipsiness, Is.EqualTo(tipsiness));
            Assert.That(battle.State.CupCount, Is.EqualTo(1));
            Assert.That(battle.ActionIds.LastIssuedId,
                Is.EqualTo(lastIssuedId));
        }

        [Test]
        public void AdditionalAndOtherCharacterMatchesDoNotGrantState()
        {
            TestBattle battle = CreateBattle(includeAlly: true);

            CombatActionExecutionResult allyResult = battle.Execute(
                new CombatAction[] { battle.CreateAllyMatch() });

            Assert.That(allyResult.CompletedActionCount, Is.EqualTo(1));
            Assert.That(battle.State.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(battle.State.Tipsiness, Is.Zero);

            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);
            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

            Assert.That(battle.State.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(battle.State.Tipsiness, Is.EqualTo(2));
        }

        [Test]
        public void LethalMatchStopsBeforeStateGainOrAdditional()
        {
            TestBattle battle = CreateBattle(bossHp: 100);

            CombatActionExecutionResult result = battle.ExecuteMatch(
                BoardMatchTier.Three);

            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(AdditionalResults(result), Is.Empty);
            Assert.That(battle.State.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(battle.State.Tipsiness, Is.Zero);
        }

        private TestBattle CreateBattle(
            int awakening = 0,
            bool includeAlly = false,
            long bossHp = 1000000)
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            createdObjects.Add(config);
            return new TestBattle(
                config,
                awakening,
                includeAlly,
                bossHp);
        }

        private static DamageActionResult[] AdditionalResults(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Where(item =>
                    item.Action.Origin == ActionOrigin.Additional)
                .ToArray();
        }

        private sealed class TestBattle
        {
            private readonly MeimeiMatchActionProvider provider;
            private readonly CombatActionExecutor executor;

            public TestBattle(
                MeimeiCombatConfig config,
                int awakening,
                bool includeAlly,
                long bossHp)
            {
                Meimei = new CharacterBattleState(
                    MeimeiRules.CharacterId,
                    0,
                    ElementType.Fire,
                    900,
                    1000d);
                Ally = new CharacterBattleState(
                    "character_test_ally",
                    1,
                    ElementType.Fire,
                    1000,
                    500d);
                Party = new PartyBattleState(includeAlly
                    ? new[] { Meimei, Ally }
                    : new[] { Meimei });
                Boss = new BossBattleState(
                    "meimei_completion_test_boss",
                    ElementType.Light,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                State = new MeimeiBattleState(Meimei, 5);
                Rule = new MeimeiMatchCompletionRule(
                    State,
                    config,
                    awakening,
                    ActionIds);
                provider = new MeimeiMatchActionProvider(
                    config,
                    awakening);
                executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { Rule }));
            }

            public CharacterBattleState Meimei { get; }
            public CharacterBattleState Ally { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MeimeiBattleState State { get; }
            public MeimeiMatchCompletionRule Rule { get; }

            public DamageAction CreateMatch(BoardMatchTier tier)
            {
                return (DamageAction)provider.CreateActions(
                    Meimei,
                    Party,
                    Boss,
                    tier,
                    1,
                    ActionIds)[0];
            }

            public DamageAction CreateAllyMatch()
            {
                return new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        Ally,
                        Party,
                        Boss,
                        ElementType.Fire,
                        AttackType.Match,
                        AttackTag.Match3,
                        1d,
                        appliesCombo: true,
                        finalComboCount: 1,
                        canCritical: true));
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier)
            {
                return Execute(new CombatAction[] { CreateMatch(tier) });
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] rules)
            {
                if (rules == null || rules.Length == 0)
                {
                    return executor.Execute(new CombatActionQueue(actions));
                }

                var customExecutor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
                return customExecutor.Execute(
                    new CombatActionQueue(actions));
            }
        }

        private sealed class ContextObserverRule : ICombatTriggerRule
        {
            public CombatActionTriggerContext Context { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.CompletedResult is DamageActionResult
                    && context.CompletedAction.Origin == ActionOrigin.Match)
                {
                    Context = context;
                }

                return Array.Empty<CombatAction>();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0.70d, 1, false)]
        [TestCase(BoardMatchTier.Four, 1.20d, 2, false)]
        [TestCase(BoardMatchTier.FiveOrMore, 1.90d, 3, true)]
        public void MatchCreatesDamageThenCovenantAndBlessing(
            BoardMatchTier tier,
            double coefficient,
            int covenantGain,
            bool expectsBlessing)
        {
            using (var battle = new TestBattle(tier))
            {
                int expectedCount = 1 + covenantGain
                    + (expectsBlessing ? 1 : 0);
                Assert.That(battle.Actions, Has.Count.EqualTo(expectedCount));
                DamageAction damage = battle.MatchDamage;
                AssertMatchDamage(damage, tier, coefficient, 1);

                for (int index = 0; index < covenantGain; index++)
                {
                    var apply = battle.Actions[index + 1]
                        as ApplyEffectAction;
                    Assert.That(apply, Is.Not.Null);
                    Assert.That(apply.ActionId, Is.EqualTo(index + 2));
                    Assert.That(apply.Origin,
                        Is.EqualTo(ActionOrigin.System));
                    Assert.That(apply.TargetType,
                        Is.EqualTo(CombatEffectTargetType.Boss));
                    Assert.That(apply.Effect.EffectId,
                        Is.EqualTo(LilithRules.CovenantEffectId));
                    AssertLineage(apply, damage);
                }

                if (expectsBlessing)
                {
                    var blessing = battle.Actions[expectedCount - 1]
                        as ApplyEffectAction;
                    Assert.That(blessing, Is.Not.Null);
                    Assert.That(blessing.TargetType,
                        Is.EqualTo(CombatEffectTargetType.Party));
                    Assert.That(blessing.Effect.EffectId,
                        Is.EqualTo(LilithRules.FallenBlessingEffectId));
                    AssertLineage(blessing, damage);
                }

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(covenantGain));
                Assert.That(battle.Party.Effects.FindByEffectId(
                        LilithRules.FallenBlessingEffectId).Count,
                    Is.EqualTo(expectsBlessing ? 1 : 0));
            }
        }

        [TestCase(0, BoardMatchTier.Three, 0.70d)]
        [TestCase(3, BoardMatchTier.Three, 0.75d)]
        [TestCase(3, BoardMatchTier.Four, 1.30d)]
        [TestCase(3, BoardMatchTier.FiveOrMore, 1.90d)]
        [TestCase(4, BoardMatchTier.FiveOrMore, 2.10d)]
        [TestCase(5, BoardMatchTier.Three, 0.75d)]
        [TestCase(6, BoardMatchTier.FiveOrMore, 2.10d)]
        public void ProviderUsesResolvedAwakeningMatchCoefficient(
            int awakening,
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(tier, awakening))
            {
                Assert.That(
                    battle.MatchDamage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
            }
        }

        [Test]
        public void AwakeningSixStrengthensOnlyCovenantMagnitude()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 6))
            {
                var covenant = (ApplyEffectAction)battle.Actions[1];

                Assert.That(covenant.Effect.Magnitude,
                    Is.EqualTo(0.018d));
                Assert.That(battle.MatchDamage.ContextRequest
                        .SkillCoefficient,
                    Is.EqualTo(0.75d));
            }
        }

        [Test]
        public void MatchUsesComboDarkElementAndCommonModifiers()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                finalComboCount: 3))
            {
                battle.Character.Effects.ApplyEffect(new EffectInstance(
                    100L,
                    "effect_test_dark_damage",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.ElementDamageIncrease,
                    0.20d,
                    remainingTurns: null,
                    creationOrder: 100L,
                    elementFilter: ElementType.Dark));
                battle.Party.Effects.ApplyEffect(new EffectInstance(
                    101L,
                    "effect_test_dealt_damage",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.DealtDamageIncrease,
                    0.10d,
                    remainingTurns: null,
                    creationOrder: 101L));

                DamageActionResult damage = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.AttackElement,
                    Is.EqualTo(ElementType.Dark));
                Assert.That(((DamageAction)damage.Action).ContextRequest
                        .AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(damage.Context.AppliesCombo, Is.True);
                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                    Is.EqualTo(0.20d));
                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.10d));
            }
        }

        [Test]
        public void MatchDamageUsesOldCovenantThenAppliesNewStack()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                DamageActionResult first = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(first.Context.TargetTakenDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(1));
            }

            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.AddCovenant(3);
                DamageActionResult match = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(match.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.045d).Within(0.000000001d));
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(4));
                DamageActionResult followUp = battle.ExecuteSystemDamage()
                    .ActionResults.OfType<DamageActionResult>().Single();
                Assert.That(
                    followUp.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.06d).Within(0.000000001d));
            }
        }

        [Test]
        public void MaximumCovenantDoesNotBlockMatchDamageOrOverflow()
        {
            using (var battle = new TestBattle(BoardMatchTier.FiveOrMore))
            {
                battle.AddCovenant(12);

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.ActionResults
                        .OfType<DamageActionResult>().Count(),
                    Is.EqualTo(1));
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(12));
            }
        }

        [Test]
        public void ConsecutiveMatchesAccumulateIndependently()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.Execute();
                battle.RecreateActions(BoardMatchTier.Four);
                DamageActionResult second = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();
                battle.RecreateActions(BoardMatchTier.FiveOrMore);
                DamageActionResult third = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(second.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.015d).Within(0.000000001d));
                Assert.That(third.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.045d).Within(0.000000001d));
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(6));
            }
        }

        [Test]
        public void DamageTriggerRunsBeforePendingCovenantApplication()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.AddCovenant(3);
                var trigger = new FollowUpDamageRule(
                    battle.MatchDamage.ActionId,
                    battle.Character,
                    battle.Party,
                    battle.Boss);

                CombatActionExecutionResult result = battle.Execute(trigger);
                DamageActionResult[] damage = result.ActionResults
                    .OfType<DamageActionResult>().ToArray();

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[2],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(damage, Has.Length.EqualTo(2));
                Assert.That(damage[0].Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.045d).Within(0.000000001d));
                Assert.That(damage[1].Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.045d).Within(0.000000001d));
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(4));
            }
        }

        [TestCase(0, 0.20d, 800L)]
        [TestCase(4, 0.25d, 750L)]
        public void FallenBlessingLastsAppliedTurnAndFollowingTurn(
            int awakening,
            double expectedReduction,
            long expectedDamage)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening,
                characterHp: 10000L))
            {
                battle.Execute();
                EffectInstance blessing = battle.Party.Effects
                    .FindByEffectId(LilithRules.FallenBlessingEffectId)
                    .Single();
                Assert.That(blessing.Magnitude,
                    Is.EqualTo(expectedReduction));

                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(expectedDamage));
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(blessing.RemainingTurns, Is.EqualTo(1));
                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(expectedDamage));
                battle.Party.Effects.ProcessTurnEnd();

                Assert.That(battle.Party.Effects.FindByEffectId(
                        LilithRules.FallenBlessingEffectId), Is.Empty);
                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(1000L));
            }
        }

        [TestCase(0, 0.20d, 800L)]
        [TestCase(4, 0.25d, 750L)]
        public void FallenBlessingReducesTwoBossActionsAndRefreshes(
            int awakening,
            double expectedReduction,
            long expectedDamage)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening,
                characterHp: 10000L))
            {
                battle.Execute();
                EffectInstance blessing = battle.Party.Effects
                    .FindByEffectId(LilithRules.FallenBlessingEffectId)
                    .Single();
                Assert.That(blessing.Magnitude,
                    Is.EqualTo(expectedReduction));
                Assert.That(blessing.RemainingTurns, Is.EqualTo(2));

                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(expectedDamage));
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(blessing.RemainingTurns, Is.EqualTo(1));

                battle.RecreateActions(BoardMatchTier.FiveOrMore);
                battle.Execute();
                Assert.That(battle.Party.Effects.FindByEffectId(
                        LilithRules.FallenBlessingEffectId),
                    Has.Count.EqualTo(1));
                Assert.That(blessing.RemainingTurns, Is.EqualTo(2));
                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(expectedDamage));

                battle.Party.Effects.ProcessTurnEnd();
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(battle.Party.Effects.FindByEffectId(
                        LilithRules.FallenBlessingEffectId), Is.Empty);
                Assert.That(battle.ExecuteBossDamage().AppliedHpDamage,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void FallenBlessingDoesNotReduceDirectHpCost()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                characterHp: 10000L))
            {
                battle.Execute();
                long hpBefore = battle.Party.CurrentHp;
                var cost = new PartyHpCostAction(
                    9000L,
                    ActionOrigin.System,
                    battle.Party,
                    referenceMaxHp: 1000L,
                    costRate: 0.15d);

                PartyHpCostActionResult result = battle.ExecuteActions(
                        new CombatAction[] { cost })
                    .ActionResults.OfType<PartyHpCostActionResult>()
                    .Single();

                Assert.That(result.RequestedHpCost, Is.EqualTo(150L));
                Assert.That(result.AppliedHpCost, Is.EqualTo(150L));
                Assert.That(battle.Party.CurrentHp,
                    Is.EqualTo(hpBefore - 150L));
            }
        }

        [Test]
        public void MatchFactoryAcceptsOrderedIdsAndEffectLineage()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                createActions: false))
            {
                var factory = new MatchEventCombatActionFactory(
                    battle.Party,
                    battle.Boss,
                    battle.Provider,
                    battle.ActionIds);

                IReadOnlyList<CombatAction> actions =
                    factory.CreateCharacterRootActions(
                        CreateExecution(BoardMatchTier.FiveOrMore, 2),
                        battle.Character);

                Assert.That(actions, Has.Count.EqualTo(5));
                for (int index = 0; index < actions.Count; index++)
                {
                    Assert.That(actions[index].ActionId,
                        Is.EqualTo(index + 1));
                }

                DamageAction damage = (DamageAction)actions[0];
                AssertMatchDamage(
                    damage,
                    BoardMatchTier.FiveOrMore,
                    1.90d,
                    2);
                foreach (CombatAction action in actions.Skip(1))
                {
                    AssertLineage(action, damage);
                }
            }
        }

        private static void AssertMatchDamage(
            DamageAction damage,
            BoardMatchTier tier,
            double coefficient,
            int finalComboCount)
        {
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(damage.RootActionId, Is.EqualTo(damage.ActionId));
            Assert.That(damage.SourceActionId, Is.Null);
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Dark));
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(Tag(tier)));
            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(coefficient));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
            Assert.That(damage.ContextRequest.FinalComboCount,
                Is.EqualTo(finalComboCount));
            Assert.That(damage.ContextRequest.CanCritical, Is.True);
        }

        private static void AssertLineage(
            CombatAction action,
            DamageAction damage)
        {
            Assert.That(action.RootActionId,
                Is.EqualTo(damage.ActionId));
            Assert.That(action.SourceActionId,
                Is.EqualTo(damage.ActionId));
        }

        private static AttackTag Tag(BoardMatchTier tier)
        {
            return tier == BoardMatchTier.Three
                ? AttackTag.Match3
                : tier == BoardMatchTier.Four
                    ? AttackTag.Match4
                    : AttackTag.Match5Plus;
        }

        private static MatchEventExecution CreateExecution(
            BoardMatchTier tier,
            int finalComboCount)
        {
            int count = tier == BoardMatchTier.Three
                ? 3
                : tier == BoardMatchTier.Four ? 4 : 6;
            var positions = new List<BoardPosition>(count);
            for (int index = 0; index < count; index++)
            {
                positions.Add(new BoardPosition(index, 0));
            }

            MatchEvent matchEvent = CreateInternal<MatchEvent>(
                0,
                0,
                0,
                ElementType.Dark,
                tier,
                positions[0],
                positions,
                positions.Count);
            return CreateInternal<MatchEventExecution>(
                1L,
                matchEvent,
                finalComboCount);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private sealed class FollowUpDamageRule : ICombatTriggerRule
        {
            private readonly long matchActionId;
            private readonly CharacterBattleState attacker;
            private readonly PartyBattleState party;
            private readonly BossBattleState boss;

            public FollowUpDamageRule(
                long matchActionId,
                CharacterBattleState attacker,
                PartyBattleState party,
                BossBattleState boss)
            {
                this.matchActionId = matchActionId;
                this.attacker = attacker;
                this.party = party;
                this.boss = boss;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.ActionId != matchActionId)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new DamageAction(
                        9000L,
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            attacker,
                            party,
                            boss,
                            ElementType.Dark,
                            AttackType.Additional,
                            AttackTag.None,
                            0d,
                            appliesCombo: false,
                            finalComboCount: 0,
                            canCritical: false),
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly LilithCombatConfig config;
            private readonly LilithResolvedCombatRules rules;
            private long nextEffectId = 1000L;

            public TestBattle(
                BoardMatchTier tier,
                int awakening = 0,
                int finalComboCount = 1,
                long bossHp = 1000000L,
                long characterHp = 1000L,
                bool createActions = true)
            {
                config = LilithTestConfig.Create();
                rules = LilithCombatRuleResolver.Resolve(
                    config,
                    awakening);
                Character = new CharacterBattleState(
                    LilithRules.CharacterId,
                    0,
                    ElementType.Dark,
                    characterHp,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Dark,
                    bossHp,
                    1000d);
                Provider = new LilithMatchActionProvider(
                    config,
                    awakening);
                ActionIds = new CombatActionIdSequence();
                FinalComboCount = finalComboCount;
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        Party,
                        Boss,
                        tier,
                        FinalComboCount,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public LilithMatchActionProvider Provider { get; }
            public CombatActionIdSequence ActionIds { get; }
            public int FinalComboCount { get; }
            public IReadOnlyList<CombatAction> Actions { get; private set; }
            public DamageAction MatchDamage => (DamageAction)Actions[0];

            public void RecreateActions(BoardMatchTier tier)
            {
                Actions = Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    FinalComboCount,
                    ActionIds);
            }

            public void AddCovenant(int count)
            {
                for (int index = 0; index < count; index++)
                {
                    Boss.Effects.ApplyEffect(
                        LilithCovenantEffectFactory.Create(
                            nextEffectId++,
                            rules));
                }
            }

            public CombatActionExecutionResult Execute(
                params ICombatTriggerRule[] triggerRules)
            {
                return ExecuteActions(Actions, triggerRules);
            }

            public CombatActionExecutionResult ExecuteSystemDamage()
            {
                var action = new DamageAction(
                    8000L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Dark,
                        AttackType.Active,
                        AttackTag.None,
                        0d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                return ExecuteActions(new CombatAction[] { action });
            }

            public BossDamageActionResult ExecuteBossDamage()
            {
                var action = new BossDamageAction(
                    7000L,
                    new BossDamageContextBuildRequest(
                        Boss,
                        Party,
                        1d,
                        AttackTag.None));
                return ExecuteActions(new CombatAction[] { action })
                    .ActionResults.OfType<BossDamageActionResult>()
                    .Single();
            }

            public CombatActionExecutionResult ExecuteActions(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] triggerRules)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(triggerRules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

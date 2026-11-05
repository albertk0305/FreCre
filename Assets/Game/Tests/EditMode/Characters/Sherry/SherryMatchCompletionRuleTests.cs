using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryMatchCompletionRuleTests
    {
        [Test]
        public void CompletedMatchesUpdateRankAndPreviousTierInOrder()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.Execute(battle.Match(BoardMatchTier.Three));
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Three));

                battle.Execute(battle.Match(BoardMatchTier.Three));
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));

                battle.Execute(battle.Match(BoardMatchTier.Four));
                Assert.That(battle.State.StylishRank, Is.EqualTo(2));
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Four));
            }
        }

        [Test]
        public void MaximumRankStillUpdatesPreviousTier()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(7, SherryMatchTier.FiveOrMore);

                battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(battle.State.StylishRank, Is.EqualTo(7));
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.FiveOrMore));
            }
        }

        [Test]
        public void CreatingDamageDoesNotUpdateRankBeforeCompletion()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.Match(
                    BoardMatchTier.Three);

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(battle.State.StylishRank, Is.Zero);
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.None));
            }
        }

        [Test]
        public void CompletedZeroDamageStillUpdatesRank()
        {
            using (var battle = new SherryCombatTestBattle(attack: 0d))
            {
                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.Three));
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.AppliedDamage, Is.Zero);
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Three));
            }
        }

        [Test]
        public void FullyReducedDamageStillUpdatesRank()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.Boss.Effects.ApplyEffect(new EffectInstance(
                    9000L,
                    "effect_test_full_damage_reduction",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.TargetTakenDamageReduction,
                    1d,
                    remainingTurns: null,
                    creationOrder: 9000L));

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.Three));
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.AppliedDamage, Is.Zero);
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Three));
            }
        }

        [Test]
        public void LethalMatchStopsBeforeRankAndEffectTrigger()
        {
            using (var battle = new SherryCombatTestBattle(bossHp: 1L))
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(battle.State.StylishRank, Is.EqualTo(4));
                Assert.That(battle.OniIntimidation(), Is.Null);
            }
        }

        [Test]
        public void EarlierLethalActionClearsPendingSherryMatch()
        {
            using (var battle = new SherryCombatTestBattle(bossHp: 1L))
            {
                var actions = new List<CombatAction>();
                actions.AddRange(
                    battle.Match(BoardMatchTier.Three));
                actions.AddRange(
                    battle.Match(BoardMatchTier.Four));

                CombatActionExecutionResult execution =
                    battle.Execute(actions);

                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.ClearedRemainingActions, Is.True);
                Assert.That(battle.State.StylishRank, Is.Zero);
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.None));
            }
        }

        [TestCase(ActionOrigin.Active, AttackType.Active)]
        [TestCase(ActionOrigin.Additional, AttackType.Additional)]
        [TestCase(ActionOrigin.Chase, AttackType.Chase)]
        [TestCase(ActionOrigin.DamageOverTime,
            AttackType.DamageOverTime)]
        public void NonMatchDamageDoesNotUpdateState(
            ActionOrigin origin,
            AttackType attackType)
        {
            using (var battle = new SherryCombatTestBattle())
            {
                long actionId = battle.ActionIds.Next();
                long? rootId = origin == ActionOrigin.Additional
                        || origin == ActionOrigin.Chase
                    ? 9000L
                    : (long?)null;
                long? sourceId = rootId;
                var damage = new DamageAction(
                    actionId,
                    origin,
                    new DamageContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        ElementType.Fire,
                        attackType,
                        AttackTag.Match3,
                        0d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    rootId,
                    sourceId);

                battle.Execute(new CombatAction[] { damage });

                Assert.That(battle.State.StylishRank, Is.Zero);
                Assert.That(battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.None));
            }
        }

        [Test]
        public void OtherCharacterMatchDoesNotUpdateSherryState()
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                var sherry = new CharacterBattleState(
                    SherryRules.CharacterId,
                    0,
                    ElementType.Fire,
                    1000L,
                    100d);
                var other = new CharacterBattleState(
                    "character_other_fire",
                    1,
                    ElementType.Fire,
                    1000L,
                    100d);
                var party = new PartyBattleState(
                    new CharacterBattleState[] { sherry, other });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Fire,
                    100000L,
                    100d);
                var state = new SherryBattleState(sherry, 7);
                var ids = new CombatActionIdSequence();
                var rule = new SherryMatchCompletionRule(
                    state,
                    config,
                    0,
                    ids);
                var action = new DamageAction(
                    ids.Next(),
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        other,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Match,
                        AttackTag.Match3,
                        1d,
                        appliesCombo: true,
                        finalComboCount: 1,
                        canCritical: true));
                var executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { rule }));

                executor.Execute(
                    new CombatActionQueue(
                        new CombatAction[] { action }));

                Assert.That(state.StylishRank, Is.Zero);
                Assert.That(state.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ExecutionSessionProcessesOneCompletionOnlyOnce()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(battle.State.StylishRank, Is.EqualTo(5));
                Assert.That(execution.ActionResults
                        .OfType<ApplyEffectActionResult>().Count(),
                    Is.EqualTo(1));
                Assert.That(battle.Boss.Effects.FindByEffectId(
                        SherryRules.OniIntimidationEffectId).Count,
                    Is.EqualTo(1));
            }
        }

        [TestCase(0, BoardMatchTier.Three, 0.03d)]
        [TestCase(0, BoardMatchTier.Four, 0.06d)]
        [TestCase(0, BoardMatchTier.FiveOrMore, 0.10d)]
        [TestCase(4, BoardMatchTier.Three, 0.04d)]
        [TestCase(4, BoardMatchTier.Four, 0.08d)]
        [TestCase(4, BoardMatchTier.FiveOrMore, 0.13d)]
        public void RankFiveAppliesTierAndAwakeningEffect(
            int awakening,
            BoardMatchTier tier,
            double expectedMagnitude)
        {
            using (var battle = new SherryCombatTestBattle(awakening))
            {
                SherryMatchTier sherryTier = ResolveSherryTier(tier);
                battle.RaiseRankTo(4, sherryTier);

                battle.Execute(battle.Match(tier));
                EffectInstance effect = battle.OniIntimidation();

                Assert.That(battle.State.StylishRank, Is.EqualTo(5));
                Assert.That(effect, Is.Not.Null);
                Assert.That(effect.EffectId,
                    Is.EqualTo(SherryRules.OniIntimidationEffectId));
                Assert.That(effect.Category,
                    Is.EqualTo(EffectCategory.Debuff));
                Assert.That(effect.ModifierType,
                    Is.EqualTo(
                        EffectModifierType.TargetTakenDamageIncrease));
                Assert.That(effect.Magnitude,
                    Is.EqualTo(expectedMagnitude));
                Assert.That(effect.RemainingTurns, Is.EqualTo(2));
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.ReplaceWithStronger));
            }
        }

        [Test]
        public void RankFourAfterCompletionDoesNotApplyEffect()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(3, SherryMatchTier.Three);

                battle.Execute(battle.Match(BoardMatchTier.Three));

                Assert.That(battle.State.StylishRank, Is.EqualTo(4));
                Assert.That(battle.OniIntimidation(), Is.Null);
            }
        }

        [Test]
        public void StrongerEffectReplacesAndWeakerEffectIsIgnored()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(5, SherryMatchTier.Three);
                battle.Execute(battle.Match(BoardMatchTier.Three));
                EffectInstance weak = battle.OniIntimidation();
                Assert.That(weak.Magnitude, Is.EqualTo(0.03d));

                battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));
                EffectInstance strong = battle.OniIntimidation();
                Assert.That(strong, Is.Not.SameAs(weak));
                Assert.That(strong.Magnitude, Is.EqualTo(0.10d));

                battle.Boss.Effects.ProcessTurnEnd();
                Assert.That(strong.RemainingTurns, Is.EqualTo(1));

                battle.Execute(battle.Match(BoardMatchTier.Three));
                Assert.That(battle.OniIntimidation(), Is.SameAs(strong));
                Assert.That(strong.Magnitude, Is.EqualTo(0.10d));
                Assert.That(strong.RemainingTurns, Is.EqualTo(1));
                Assert.That(battle.Boss.Effects.FindByEffectId(
                        SherryRules.OniIntimidationEffectId).Count,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void EqualMagnitudeKeepsLongerDurationOnly()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(5, SherryMatchTier.Four);
                battle.Execute(battle.Match(BoardMatchTier.Four));
                EffectInstance original = battle.OniIntimidation();
                battle.Boss.Effects.ProcessTurnEnd();
                Assert.That(original.RemainingTurns, Is.EqualTo(1));

                battle.Execute(battle.Match(BoardMatchTier.Four));
                Assert.That(battle.OniIntimidation(),
                    Is.SameAs(original));
                Assert.That(original.RemainingTurns, Is.EqualTo(2));

                battle.Execute(battle.Match(BoardMatchTier.Four));
                Assert.That(battle.OniIntimidation(),
                    Is.SameAs(original));
                Assert.That(original.RemainingTurns, Is.EqualTo(2));
            }
        }

        [Test]
        public void EffectLastsTwoTurnEndsThenExpires()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);
                battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));
                var turns = new BattleTurnEndProcessor(
                    battle.Party,
                    battle.Boss);

                turns.ProcessTurnEnd();
                Assert.That(battle.OniIntimidation().RemainingTurns,
                    Is.EqualTo(1));

                turns.ProcessTurnEnd();
                Assert.That(battle.OniIntimidation(), Is.Null);
            }
        }

        [Test]
        public void NewEffectMissesCurrentDamageButAffectsNextDamage()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);

                CombatActionExecutionResult first = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));
                DamageActionResult firstDamage = first.ActionResults
                    .OfType<DamageActionResult>().Single();
                Assert.That(firstDamage.Context
                    .TargetTakenDamageIncreaseRateSum, Is.Zero);
                Assert.That(battle.OniIntimidation().Magnitude,
                    Is.EqualTo(0.10d));

                CombatActionExecutionResult next = battle.Execute(
                    battle.Match(BoardMatchTier.Three));
                DamageActionResult nextDamage = next.ActionResults
                    .OfType<DamageActionResult>().Single();
                Assert.That(nextDamage.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.10d));
            }
        }

        [Test]
        public void OniIntimidationAddsWithOtherTakenDamageIncrease()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);
                battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));
                battle.Boss.Effects.ApplyEffect(new EffectInstance(
                    9000L,
                    "effect_test_other_taken_damage",
                    "test_source",
                    EffectCategory.Debuff,
                    EffectModifierType.TargetTakenDamageIncrease,
                    0.05d,
                    remainingTurns: null,
                    creationOrder: 9000L));

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.Three));
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(0.000000000001d));
            }
        }

        [Test]
        public void DerivedEffectRunsBeforeLaterRuleAction()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(4, SherryMatchTier.FiveOrMore);
                var laterAttack = new DerivedAdditionalRule(
                    battle.Character,
                    battle.ActionIds);

                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore),
                    laterAttack);

                Assert.That(execution.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(execution.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
                DamageActionResult additional =
                    execution.ActionResults[2] as DamageActionResult;
                Assert.That(additional, Is.Not.Null);
                Assert.That(((DamageAction)additional.Action).Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(additional.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.10d));
            }
        }

        private static SherryMatchTier ResolveSherryTier(
            BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return SherryMatchTier.Three;
                case BoardMatchTier.Four:
                    return SherryMatchTier.Four;
                case BoardMatchTier.FiveOrMore:
                    return SherryMatchTier.FiveOrMore;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private sealed class DerivedAdditionalRule : ICombatTriggerRule
        {
            private readonly CharacterBattleState attacker;
            private readonly CombatActionIdSequence actionIds;
            private bool created;

            public DerivedAdditionalRule(
                CharacterBattleState attacker,
                CombatActionIdSequence actionIds)
            {
                this.attacker = attacker;
                this.actionIds = actionIds;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (created
                    || !(context.CompletedResult is DamageActionResult result)
                    || !(result.Action is DamageAction damage)
                    || damage.Origin != ActionOrigin.Match)
                {
                    return Array.Empty<CombatAction>();
                }

                created = true;
                return new CombatAction[]
                {
                    new DamageAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        new DamageContextBuildRequest(
                            attacker,
                            context.Party,
                            context.Boss,
                            ElementType.Fire,
                            AttackType.Additional,
                            AttackTag.None,
                            1d,
                            appliesCombo: false,
                            finalComboCount: 0,
                            canCritical: false),
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }
    }
}

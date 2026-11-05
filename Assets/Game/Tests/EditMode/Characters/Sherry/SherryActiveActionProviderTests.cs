using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryActiveActionProviderTests
    {
        [TestCase(0)]
        [TestCase(6)]
        public void JackpotCoefficientUsesExactRankSnapshotForRanksOneToSeven(
            int awakening)
        {
            double maximum = awakening >= 6 ? 23d : 18d;
            for (int rank = 1; rank <= 7; rank++)
            {
                using (var battle = new SherryCombatTestBattle(awakening))
                {
                    battle.RaiseRankTo(rank, SherryMatchTier.Three);
                    Assert.That(
                        battle.State.StylishRank,
                        Is.EqualTo(rank),
                        $"Rank setup failed for Stylish rank {rank}.");
                    var provider = new SherryActiveActionProvider(
                        battle.State,
                        battle.Config,
                        awakening);

                    DamageAction action = SingleDamage(provider.CreateActions(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        battle.ActionIds));
                    double expected = maximum
                        * (Math.Pow(1.65d, rank) - 1d)
                        / (Math.Pow(1.65d, 7d) - 1d);

                    Assert.That(action.ContextRequest.SkillCoefficient,
                        Is.EqualTo(expected).Within(1e-12d));
                }
            }
        }

        [TestCase(0, 0.20d)]
        [TestCase(5, 0.25d)]
        public void StylishIncreaseUsesAwakeningMaximum(
            int awakening,
            double expectedMaximum)
        {
            using (var battle = new SherryCombatTestBattle(awakening))
            {
                battle.RaiseRankTo(7, SherryMatchTier.Three);
                var provider = new SherryActiveActionProvider(
                    battle.State,
                    battle.Config,
                    awakening);

                DamageAction action = SingleDamage(provider.CreateActions(
                    battle.Character,
                    battle.Party,
                    battle.Boss,
                    battle.ActionIds));

                Assert.That(
                    action.ContextRequest.ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(expectedMaximum).Within(1e-12d));
            }
        }

        [Test]
        public void RankedUseCreatesOneFireActiveCriticalNonComboRootDamage()
        {
            using (var fixture = new ActiveFixture())
            {
                fixture.Battle.RaiseRankTo(3, SherryMatchTier.Three);

                Assert.That(fixture.Executor.TryBeginExecution(
                    0,
                    out ActiveAbilityCombatExecutionSession session), Is.True);
                DamageAction action = session.NextAction as DamageAction;

                Assert.That(action, Is.Not.Null);
                Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Active));
                Assert.That(action.RootActionId, Is.EqualTo(action.ActionId));
                Assert.That(action.SourceActionId, Is.Null);
                Assert.That(action.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Fire));
                Assert.That(action.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Active));
                Assert.That(action.ContextRequest.AttackTags,
                    Is.EqualTo(AttackTag.None));
                Assert.That(action.ContextRequest.CanCritical, Is.True);
                Assert.That(action.ContextRequest.AppliesCombo, Is.False);
                Assert.That(action.ContextRequest.FinalComboCount, Is.Zero);
            }
        }

        [Test]
        public void SuccessfulUseConsumesOnceAndPreservesSnapshotsAndState()
        {
            using (var fixture = new ActiveFixture())
            {
                fixture.Battle.RaiseRankTo(7, SherryMatchTier.Three);
                fixture.Battle.State.CompleteMatch(SherryMatchTier.Four);
                EffectInstance oni = ApplyOni(fixture.Battle);

                Assert.That(fixture.Executor.TryBeginExecution(
                    0,
                    out ActiveAbilityCombatExecutionSession session), Is.True);
                DamageAction action = (DamageAction)session.NextAction;

                Assert.That(fixture.Battle.State.StylishRank, Is.Zero);
                Assert.That(fixture.Battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Four));
                Assert.That(fixture.Battle.OniIntimidation(), Is.SameAs(oni));
                Assert.That(action.ContextRequest.SkillCoefficient,
                    Is.EqualTo(18d).Within(1e-12d));
                Assert.That(
                    action.ContextRequest.ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(0.20d).Within(1e-12d));

                fixture.Battle.State.CompleteMatch(SherryMatchTier.Three);
                Assert.That(fixture.Executor.TryExecute(0, out _), Is.False);
                Assert.That(fixture.Battle.State.StylishRank, Is.EqualTo(1));
            }
        }

        [Test]
        public void StylishIncreaseAddsToExistingDealtDamageIncrease()
        {
            using (var fixture = new ActiveFixture())
            {
                fixture.Battle.RaiseRankTo(7, SherryMatchTier.Three);
                fixture.Battle.Character.Effects.ApplyEffect(
                    new EffectInstance(
                        8000L,
                        "effect_test_dealt_increase",
                        "test_source",
                        EffectCategory.Buff,
                        EffectModifierType.DealtDamageIncrease,
                        0.10d,
                        remainingTurns: null,
                        creationOrder: 8000L));

                Assert.That(fixture.Executor.TryExecute(
                    0,
                    out CombatActionExecutionResult result), Is.True);
                var damage = (DamageActionResult)result.ActionResults[0];

                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.30d).Within(1e-12d));
            }
        }

        [Test]
        public void LethalJackpotStillConsumesCommittedRank()
        {
            using (var fixture = new ActiveFixture(bossHp: 1L))
            {
                fixture.Battle.RaiseRankTo(2, SherryMatchTier.Three);

                Assert.That(fixture.Executor.TryExecute(
                    0,
                    out CombatActionExecutionResult result), Is.True);

                Assert.That(result.BossDefeated, Is.True);
                Assert.That(fixture.Battle.State.StylishRank, Is.Zero);
            }
        }

        [Test]
        public void ZeroRankCommitsCooldownWithoutActionsOrCombatSideEffects()
        {
            using (var fixture = new ActiveFixture())
            {
                fixture.Battle.State.CompleteMatch(SherryMatchTier.Four);
                fixture.Battle.State.ConsumeAllRank();
                EffectInstance oni = ApplyOni(fixture.Battle);
                long hpBefore = fixture.Battle.Boss.CurrentHp;
                long actionIdBefore = fixture.Battle.ActionIds.LastIssuedId;
                var score = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());

                Assert.That(fixture.Executor.TryBeginExecution(
                    0,
                    out ActiveAbilityCombatExecutionSession session), Is.True);

                Assert.That(session.IsCompleted, Is.True);
                Assert.That(session.NextAction, Is.Null);
                Assert.That(session.Result.CompletedActionCount, Is.Zero);
                Assert.That(score.Collect(session.Result), Is.Zero);
                Assert.That(score.DamageScore, Is.Zero);
                Assert.That(fixture.Battle.Boss.CurrentHp, Is.EqualTo(hpBefore));
                Assert.That(fixture.Battle.ActionIds.LastIssuedId,
                    Is.EqualTo(actionIdBefore));
                Assert.That(fixture.Random.CallCount, Is.Zero);
                Assert.That(fixture.Trigger.CallCount, Is.Zero);
                Assert.That(fixture.Battle.State.StylishRank, Is.Zero);
                Assert.That(fixture.Battle.State.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Four));
                Assert.That(fixture.Battle.OniIntimidation(), Is.SameAs(oni));
                Assert.That(fixture.RuntimeState.UsedThisTurn, Is.True);
                Assert.That(fixture.RuntimeState.RemainingCooldown,
                    Is.EqualTo(1));
                Assert.That(fixture.Coordinator.Context.Phase,
                    Is.EqualTo(BattlePhase.PlayerInput));
            }
        }

        [Test]
        public void ZeroRankBlocksSameTurnAndCanBeUsedNextTurn()
        {
            using (var fixture = new ActiveFixture())
            {
                Assert.That(fixture.Executor.TryExecute(0, out _), Is.True);
                Assert.That(fixture.Executor.TryExecute(0, out _), Is.False);

                AdvanceTurn(fixture.Coordinator);

                Assert.That(fixture.RuntimeState.CanUse, Is.True);
                Assert.That(fixture.Executor.TryExecute(0, out var result),
                    Is.True);
                Assert.That(result.CompletedActionCount, Is.Zero);
            }
        }

        [Test]
        public void AfterJackpotSameTierDoesNotIncreaseButDifferentTierDoes()
        {
            using (var fixture = new ActiveFixture())
            {
                fixture.Battle.State.CompleteMatch(SherryMatchTier.Four);

                Assert.That(fixture.Executor.TryExecute(0, out _), Is.True);
                Assert.That(fixture.Battle.State.StylishRank, Is.Zero);

                fixture.Battle.State.CompleteMatch(SherryMatchTier.Four);
                Assert.That(fixture.Battle.State.StylishRank, Is.Zero);
                fixture.Battle.State.CompleteMatch(SherryMatchTier.Three);
                Assert.That(fixture.Battle.State.StylishRank, Is.EqualTo(1));
            }
        }

        [Test]
        public void ProviderFailureBeforeCommitPreservesRank()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(3, SherryMatchTier.Three);
                var provider = new SherryActiveActionProvider(
                    battle.State,
                    battle.Config,
                    0);
                var other = new ValorChronicle.Battle.Combat.State
                    .CharacterBattleState(
                        "character_other",
                        1,
                        ElementType.Fire,
                        1000L,
                        100d);

                Assert.That(() => provider.CreateActions(
                    other,
                    battle.Party,
                    battle.Boss,
                    battle.ActionIds), Throws.ArgumentException);
                Assert.That(battle.State.StylishRank, Is.EqualTo(3));
            }
        }

        private static DamageAction SingleDamage(
            IReadOnlyList<CombatAction> actions)
        {
            Assert.That(actions.Count, Is.EqualTo(1));
            return (DamageAction)actions[0];
        }

        private static EffectInstance ApplyOni(SherryCombatTestBattle battle)
        {
            var effect = new EffectInstance(
                9000L,
                SherryRules.OniIntimidationEffectId,
                SherryRules.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                0.03d,
                remainingTurns: 2,
                creationOrder: 9000L,
                stackPolicy: EffectStackPolicy.ReplaceWithStronger);
            return battle.Boss.Effects.ApplyEffect(effect);
        }

        private static void AdvanceTurn(BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(),
                consumesTurn: true), Is.True);
            coordinator.ExecuteRemainingMatchEvents();
            Assert.That(coordinator.CompleteBossAction(), Is.True);
        }

        private sealed class ActiveFixture : IDisposable
        {
            public ActiveFixture(int awakening = 0, long bossHp = 1000000L)
            {
                Battle = new SherryCombatTestBattle(
                    awakening,
                    bossHp: bossHp);
                Provider = new SherryActiveActionProvider(
                    Battle.State,
                    Battle.Config,
                    awakening);
                Coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { Provider.CooldownTurns });
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.Register(
                    SherryRules.CharacterId,
                    SherryRules.ActiveAbilityId,
                    Provider);
                var binding = new ActiveAbilityBinding(
                    0,
                    Battle.Character.PartySlotIndex,
                    SherryRules.CharacterId,
                    SherryRules.ActiveAbilityId);
                Random = new CountingRandomSource();
                Trigger = new CountingTriggerRule();
                var actionExecutor = new CombatActionExecutor(
                    Battle.Boss,
                    Battle.Party,
                    new DamageContextFactory(Random),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { Trigger }));
                Executor = new ActiveAbilityCombatExecutor(
                    Coordinator,
                    Battle.Party,
                    Battle.Boss,
                    actionExecutor,
                    Battle.ActionIds,
                    new ActiveAbilityBinding[] { binding },
                    registry);
                Assert.That(Coordinator.StartBattle(), Is.True);
            }

            public SherryCombatTestBattle Battle { get; }
            public SherryActiveActionProvider Provider { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public ActiveAbilityCombatExecutor Executor { get; }
            public CountingRandomSource Random { get; }
            public CountingTriggerRule Trigger { get; }
            public ActiveAbilityRuntimeState RuntimeState =>
                Coordinator.Context.ActiveAbilities[0];

            public void Dispose()
            {
                Battle.Dispose();
            }
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            public int CallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                CallCount++;
                return minInclusive;
            }

            public float NextFloat()
            {
                CallCount++;
                return 0.5f;
            }
        }

        private sealed class CountingTriggerRule : ICombatTriggerRule
        {
            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                CallCount++;
                return Array.Empty<CombatAction>();
            }
        }
    }
}

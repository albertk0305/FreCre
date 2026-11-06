using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Thalassion;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Thalassion
{
    public sealed class ThalassionBossCombatActionProviderTests
    {
        [TestCase(1, 0.95d)]
        [TestCase(2, 1.15d)]
        [TestCase(3, 1.35d)]
        [TestCase(4, 1.55d)]
        [TestCase(5, 1.75d)]
        [TestCase(6, 1.95d)]
        [TestCase(7, 2.15d)]
        [TestCase(8, 2.35d)]
        [TestCase(9, 2.55d)]
        [TestCase(10, 2.75d)]
        public void FourthActionUsesRemainingShieldStackCoefficient(
            int remainingStacks,
            double expectedCoefficient)
        {
            BattleHarness battle = CreateAtFourthAction(remainingStacks);

            Assert.That(
                battle.Runtime.GetIntentForecast(1)[0].SkillId,
                Is.EqualTo(ThalassionRules.PressureBurstSkillId));
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 4));
            BossDamageAction damage = plan.CombatActions
                .OfType<BossDamageAction>()
                .Single();

            Assert.That(
                damage.ContextRequest.AttackCoefficient,
                Is.EqualTo(expectedCoefficient).Within(0.000000001d));
            battle.Executor.Execute(new CombatActionQueue(
                plan.CombatActions));
            Assert.That(
                battle.Provider.TryCommitCompletedAction(),
                Is.True);
            Assert.That(battle.Runtime.PatternIndex, Is.Zero);
            Assert.That(battle.Runtime.IsShieldActive, Is.False);
        }

        [Test]
        public void FourthActionUsesFadingWaveWhenShieldWasBroken()
        {
            BattleHarness battle = CreateAtFourthAction(0);

            Assert.That(
                battle.Runtime.GetIntentForecast(1)[0].SkillId,
                Is.EqualTo(ThalassionRules.FadingWaveSkillId));
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 4));
            BossDamageAction damage = plan.CombatActions
                .OfType<BossDamageAction>()
                .Single();

            Assert.That(
                damage.ContextRequest.AttackCoefficient,
                Is.EqualTo(0.55d));
            battle.Executor.Execute(new CombatActionQueue(
                plan.CombatActions));
            Assert.That(
                battle.Provider.TryCommitCompletedAction(),
                Is.True);
            Assert.That(battle.Runtime.PatternIndex, Is.Zero);
        }

        [Test]
        public void FourActionCycleCreatesAndFinallyRemovesShield()
        {
            BattleHarness battle = CreateBattle();

            ExecuteAndCommit(battle, 1);
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(battle.Runtime.IsShieldActive, Is.False);

            ExecuteAndCommit(battle, 2);
            Assert.That(
                battle.Runtime.CurrentShieldStackCount,
                Is.EqualTo(10));
            Assert.That(
                battle.Runtime.CurrentVisualStateId,
                Is.EqualTo(ThalassionRules.ShieldedVisualStateId));

            ExecuteAndCommit(battle, 3);
            Assert.That(
                battle.Runtime.CurrentShieldStackCount,
                Is.EqualTo(10));

            ExecuteAndCommit(battle, 4);
            Assert.That(battle.Runtime.PatternIndex, Is.Zero);
            Assert.That(battle.Runtime.IsShieldActive, Is.False);
            Assert.That(battle.Runtime.CurrentVisualStateId, Is.Empty);
        }

        private static BattleHarness CreateAtFourthAction(
            int remainingStacks)
        {
            BattleHarness battle = CreateBattle();
            ExecuteAndCommit(battle, 1);
            ExecuteAndCommit(battle, 2);
            if (remainingStacks < 10)
            {
                long shieldRuntimeId = battle.Runtime
                    .TryGetShieldEffect(out var shield)
                        ? shield.RuntimeId
                        : throw new InvalidOperationException();
                battle.Boss.Effects.ConsumeStacks(
                    shieldRuntimeId,
                    10 - remainingStacks);
            }

            ExecuteAndCommit(battle, 3);
            return battle;
        }

        private static BattleHarness CreateBattle()
        {
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero",
                    0,
                    ElementType.Grass,
                    100000,
                    100d)
            });
            var boss = new BossBattleState(
                ThalassionRules.BossId,
                ElementType.Water,
                100000,
                500d);
            var runtime = new ThalassionBattleRuntimeState(
                boss,
                ThalassionTestConfig.Create());
            var provider = new ThalassionBossCombatActionProvider(runtime);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            return new BattleHarness(
                boss,
                party,
                runtime,
                provider,
                executor,
                new CombatActionIdSequence());
        }

        private static void ExecuteAndCommit(
            BattleHarness battle,
            int turn)
        {
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, turn));
            battle.Executor.Execute(new CombatActionQueue(
                plan.CombatActions));
            Assert.That(
                battle.Provider.TryCommitCompletedAction(),
                Is.True);
        }

        private static BossCombatActionContext Context(
            BattleHarness battle,
            int turn)
        {
            return (BossCombatActionContext)Activator.CreateInstance(
                typeof(BossCombatActionContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    battle.Boss,
                    battle.Party,
                    turn,
                    battle.ActionIds
                },
                culture: null);
        }

        private sealed class BattleHarness
        {
            public BattleHarness(
                BossBattleState boss,
                PartyBattleState party,
                ThalassionBattleRuntimeState runtime,
                ThalassionBossCombatActionProvider provider,
                CombatActionExecutor executor,
                CombatActionIdSequence actionIds)
            {
                Boss = boss;
                Party = party;
                Runtime = runtime;
                Provider = provider;
                Executor = executor;
                ActionIds = actionIds;
            }

            public BossBattleState Boss { get; }
            public PartyBattleState Party { get; }
            public ThalassionBattleRuntimeState Runtime { get; }
            public ThalassionBossCombatActionProvider Provider { get; }
            public CombatActionExecutor Executor { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}

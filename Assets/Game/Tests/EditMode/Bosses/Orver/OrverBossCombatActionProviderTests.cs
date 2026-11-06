using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Orver;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Orver
{
    public sealed class OrverBossCombatActionProviderTests
    {
        [Test]
        public void FourActionCycleRepeatsAndAdvancesOnlyOnCommit()
        {
            BattleHarness battle = CreateBattle(rootCount: 0);

            for (int expectedIndex = 1; expectedIndex <= 8;
                expectedIndex++)
            {
                int before = battle.Runtime.PatternIndex;
                battle.Provider.CreatePlan(Context(battle, expectedIndex));
                Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(before));
                Assert.That(battle.Runtime.HasPendingAction, Is.True);
                Assert.That(
                    battle.Provider.TryCommitCompletedAction(),
                    Is.True);
                Assert.That(
                    battle.Runtime.PatternIndex,
                    Is.EqualTo(expectedIndex % OrverRules.PatternCount));
            }

            Assert.That(
                battle.Provider.TryCommitCompletedAction(),
                Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RootInfestationCreatesExactlyOneRootBelowMaximum(
            int rootCount)
        {
            BattleHarness battle = CreateAtPattern(1, rootCount);

            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 2));

            Assert.That(plan.CombatActions, Is.Empty);
            Assert.That(plan.BoardCommand, Is.Not.Null);
            Assert.That(plan.BoardCommand.Kind,
                Is.EqualTo(BattleBoardMutationKind.CreateRoot));
            Assert.That(plan.BoardCommand.RequestedCount, Is.EqualTo(1));
            Assert.That(plan.BoardCommand.MaximumCount, Is.EqualTo(2));
            Assert.That(battle.Runtime.PendingRootCount,
                Is.EqualTo(rootCount));
        }

        [Test]
        public void AncientWrathAtMaximumRootsDoesNotCreateRoot()
        {
            BattleHarness battle = CreateAtPattern(1, rootCount: 2);

            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 2));
            BossDamageAction damage = plan.CombatActions
                .OfType<BossDamageAction>()
                .Single();

            Assert.That(plan.BoardCommand, Is.Null);
            Assert.That(
                damage.ContextRequest.AttackCoefficient,
                Is.EqualTo(1.80d));
            Assert.That(battle.Runtime.PendingActionKind,
                Is.EqualTo(OrverActionKind.AncientWrath));
        }

        [Test]
        public void UncommittedMutationKeepsPatternAndReplanReadsFreshRoots()
        {
            BattleHarness battle = CreateAtPattern(1, rootCount: 0);

            BossActionPlan failedPlan = battle.Provider.CreatePlan(
                Context(battle, 2));
            Assert.That(failedPlan.BoardCommand, Is.Not.Null);
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));

            battle.Board.RootCount = 2;
            BossActionPlan retriedPlan = battle.Provider.CreatePlan(
                Context(battle, 2));

            Assert.That(retriedPlan.BoardCommand, Is.Null);
            Assert.That(retriedPlan.CombatActions.Single(),
                Is.TypeOf<BossDamageAction>());
            Assert.That(battle.Runtime.PendingRootCount, Is.EqualTo(2));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
        }

        [Test]
        public void LifeSiphonUsesOneRootSnapshotAndOrdersDamageBeforeHeal()
        {
            BattleHarness battle = CreateAtPattern(3, rootCount: 1);
            battle.Board.NextRootCount = 2;

            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 4));

            Assert.That(battle.Board.ReadCount, Is.EqualTo(1));
            Assert.That(plan.CombatActions.Count, Is.EqualTo(2));
            var damage = (BossDamageAction)plan.CombatActions[0];
            var heal = (BossHealAction)plan.CombatActions[1];
            Assert.That(damage.ActionId, Is.Not.EqualTo(heal.ActionId));
            Assert.That(heal.ActionId, Is.EqualTo(damage.ActionId + 1));
            Assert.That(damage.ContextRequest.AttackCoefficient,
                Is.EqualTo(1.45d).Within(0.000000001d));
            Assert.That(heal.MaxHpHealingCoefficient,
                Is.EqualTo(0.01d).Within(0.000000001d));
            Assert.That(battle.Runtime.PendingRootCount, Is.EqualTo(1));
        }

        [Test]
        public void PartyIncapacitationSkipsLifeSiphonHealing()
        {
            BattleHarness battle = CreateAtPattern(
                3,
                rootCount: 2,
                partyMaxHp: 100,
                bossAttack: 100d);
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 4));
            var executor = new CombatActionExecutor(
                battle.Boss,
                battle.Party,
                new DamageContextFactory(new SeededRandomSource(1)));

            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(plan.CombatActions));

            Assert.That(result.PartyIncapacitated, Is.True);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<BossDamageActionResult>());
            Assert.That(battle.Boss.CurrentHp,
                Is.EqualTo(battle.Boss.MaxHp));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(3));
        }

        [Test]
        public void IntentIsStateNeutralAndReevaluatesCurrentRootCount()
        {
            BattleHarness battle = CreateAtPattern(3, rootCount: 1);

            string first = battle.Provider.GetIntentForecast(1)[0].SkillId;
            string repeated =
                battle.Provider.GetIntentForecast(1)[0].SkillId;
            Assert.That(first, Is.EqualTo(OrverRules.LifeSiphonSkillId));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(3));
            Assert.That(battle.Runtime.HasPendingAction, Is.False);

            battle.Board.RootCount = 0;
            string changed =
                battle.Provider.GetIntentForecast(1)[0].SkillId;
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 4));

            Assert.That(changed,
                Is.EqualTo(OrverRules.WitheredStrikeSkillId));
            Assert.That(battle.Runtime.PendingAction.SkillId,
                Is.EqualTo(changed));
            Assert.That(plan.CombatActions.Count, Is.EqualTo(1));
        }

        [Test]
        public void DelayedConditionalActionKeepsPatternAndUsesLatestRoots()
        {
            BattleHarness battle = CreateAtPattern(1, rootCount: 2);

            Assert.That(
                battle.Provider.GetIntentForecast(1)[0].SkillId,
                Is.EqualTo(OrverRules.AncientWrathSkillId));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(battle.Runtime.HasPendingAction, Is.False);

            battle.Board.RootCount = 0;
            BossActionPlan plan = battle.Provider.CreatePlan(
                Context(battle, 2));

            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(battle.Runtime.PendingActionKind,
                Is.EqualTo(OrverActionKind.RootInfestation));
            Assert.That(plan.CombatActions, Is.Empty);
            Assert.That(plan.BoardCommand.Kind,
                Is.EqualTo(BattleBoardMutationKind.CreateRoot));
        }

        [Test]
        public void ForecastStopsBeforeUnknownFutureConditionalAction()
        {
            BattleHarness first = CreateBattle(rootCount: 0);
            Assert.That(first.Provider.GetIntentForecast(4).Select(
                preview => preview.SkillId),
                Is.EqualTo(new[]
                {
                    OrverRules.AncientTimberStrikeSkillId
                }));

            BattleHarness second = CreateAtPattern(1, rootCount: 2);
            Assert.That(second.Provider.GetIntentForecast(4).Select(
                preview => preview.SkillId),
                Is.EqualTo(new[]
                {
                    OrverRules.AncientWrathSkillId,
                    OrverRules.TwistedRootLashSkillId
                }));
        }

        [Test]
        public void SeparateBattlesHaveIndependentRuntimeState()
        {
            BattleHarness first = CreateBattle(rootCount: 0);
            BattleHarness second = CreateBattle(rootCount: 2);

            first.Provider.CreatePlan(Context(first, 1));
            Assert.That(first.Provider.TryCommitCompletedAction(), Is.True);

            Assert.That(first.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(second.Runtime.PatternIndex, Is.Zero);
            Assert.That(second.Runtime.HasPendingAction, Is.False);
        }

        private static BattleHarness CreateAtPattern(
            int patternIndex,
            int rootCount,
            long partyMaxHp = 100000,
            double bossAttack = 500d)
        {
            BattleHarness battle = CreateBattle(
                rootCount,
                partyMaxHp,
                bossAttack);
            for (int index = 0; index < patternIndex; index++)
            {
                battle.Provider.CreatePlan(Context(battle, index + 1));
                Assert.That(
                    battle.Provider.TryCommitCompletedAction(),
                    Is.True);
            }

            battle.Board.ResetReads();
            return battle;
        }

        private static BattleHarness CreateBattle(
            int rootCount,
            long partyMaxHp = 100000,
            double bossAttack = 500d)
        {
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero",
                    0,
                    ElementType.Grass,
                    partyMaxHp,
                    100d)
            });
            var boss = new BossBattleState(
                OrverRules.BossId,
                ElementType.Grass,
                100000,
                bossAttack);
            var runtime = new OrverBattleRuntimeState(
                OrverTestConfig.Create());
            var board = new FakeBoardStateSource(rootCount);
            var provider = new OrverBossCombatActionProvider(
                runtime,
                board);
            return new BattleHarness(
                boss,
                party,
                runtime,
                provider,
                board,
                new CombatActionIdSequence());
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

        private sealed class FakeBoardStateSource : IBattleBoardStateSource
        {
            public FakeBoardStateSource(int rootCount)
            {
                RootCount = rootCount;
            }

            public int RootCount { get; set; }
            public int? NextRootCount { get; set; }
            public int ReadCount { get; private set; }

            public int GetRootCount()
            {
                ReadCount++;
                int result = RootCount;
                if (NextRootCount.HasValue)
                {
                    RootCount = NextRootCount.Value;
                    NextRootCount = null;
                }

                return result;
            }

            public void ResetReads()
            {
                ReadCount = 0;
            }
        }

        private sealed class BattleHarness
        {
            public BattleHarness(
                BossBattleState boss,
                PartyBattleState party,
                OrverBattleRuntimeState runtime,
                OrverBossCombatActionProvider provider,
                FakeBoardStateSource board,
                CombatActionIdSequence actionIds)
            {
                Boss = boss;
                Party = party;
                Runtime = runtime;
                Provider = provider;
                Board = board;
                ActionIds = actionIds;
            }

            public BossBattleState Boss { get; }
            public PartyBattleState Party { get; }
            public OrverBattleRuntimeState Runtime { get; }
            public OrverBossCombatActionProvider Provider { get; }
            public FakeBoardStateSource Board { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}

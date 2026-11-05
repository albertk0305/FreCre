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
    public sealed class MeimeiCriticalChanceTests
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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void AwakeningZeroThroughFiveAddsNoTipsinessCriticalChance(
            int awakening)
        {
            BattleFixture battle = CreateBattle(
                awakening,
                initialTipsiness: 5,
                randomSample: 0f);

            DamageAction action = battle.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 1);

            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0d));
            DamageActionResult result = battle.Execute(
                action,
                currentTurn: 1).ActionResults
                .OfType<DamageActionResult>()
                .Single();
            Assert.That(result.WasCritical, Is.False);
            Assert.That(battle.Random.NextFloatCallCount, Is.Zero);
        }

        [TestCase(0, 0d)]
        [TestCase(1, 0.06d)]
        [TestCase(3, 0.18d)]
        [TestCase(5, 0.30d)]
        public void AwakeningSixUsesSixPercentPerEffectiveTipsiness(
            int tipsiness,
            double expectedChance)
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: tipsiness,
                randomSample: 0.99f);

            DamageAction action = battle.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 1);

            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(expectedChance).Within(0.000000001d));
        }

        [TestCase(BoardMatchTier.Three)]
        [TestCase(BoardMatchTier.Four)]
        [TestCase(BoardMatchTier.FiveOrMore)]
        public void EveryMatchTierUsesCommonCriticalPipeline(
            BoardMatchTier tier)
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 1,
                randomSample: 0.05f);
            DamageAction action = battle.CreateMatch(tier, currentTurn: 2);

            DamageActionResult match = battle.Execute(
                action,
                currentTurn: 2).ActionResults
                .OfType<DamageActionResult>()
                .First();

            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.06d).Within(0.000000001d));
            Assert.That(match.WasCritical, Is.True);
            Assert.That(battle.Random.NextFloatCallCount,
                Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void CurrentMatchGainAffectsNextMatchButNotCurrentMatch()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 1,
                randomSample: 0.07f);
            DamageAction current = battle.CreateMatch(
                BoardMatchTier.Four,
                currentTurn: 3);

            DamageActionResult currentResult = battle.Execute(
                current,
                currentTurn: 3).ActionResults
                .OfType<DamageActionResult>()
                .Single();
            DamageAction next = battle.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 3);
            DamageActionResult nextResult = battle.Execute(
                next,
                currentTurn: 3).ActionResults
                .OfType<DamageActionResult>()
                .Single();

            Assert.That(current.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.06d).Within(0.000000001d));
            Assert.That(currentResult.WasCritical, Is.False);
            Assert.That(battle.State.Tipsiness, Is.EqualTo(2));
            Assert.That(next.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.12d).Within(0.000000001d));
            Assert.That(nextResult.WasCritical, Is.True);
        }

        [Test]
        public void AdditionalUsesTipsinessGainedBySourceMatch()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 1,
                randomSample: 0.10f);
            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);
            DamageAction source = battle.CreateMatch(
                BoardMatchTier.FiveOrMore,
                currentTurn: 4);

            CombatActionExecutionResult result = battle.Execute(
                source,
                currentTurn: 4);
            DamageActionResult match = result.ActionResults
                .OfType<DamageActionResult>()
                .Single(item => item.Action.Origin == ActionOrigin.Match);
            DamageActionResult additional = result.ActionResults
                .OfType<DamageActionResult>()
                .Single(item =>
                    item.Action.Origin == ActionOrigin.Additional);
            var additionalAction = (DamageAction)additional.Action;

            Assert.That(source.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.06d).Within(0.000000001d));
            Assert.That(match.WasCritical, Is.False);
            Assert.That(battle.State.Tipsiness, Is.EqualTo(3));
            Assert.That(additionalAction.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(additional.WasCritical, Is.True);
        }

        [Test]
        public void EffectCriticalChanceAddsToMeimeiBaseChance()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 3,
                randomSample: 0.25f);
            battle.Meimei.Effects.ApplyEffect(new EffectInstance(
                runtimeId: 1,
                effectId: "effect_test_critical_chance",
                sourceId: "test_source",
                category: EffectCategory.Buff,
                modifierType: EffectModifierType.CriticalChanceIncrease,
                magnitude: 0.10d,
                remainingTurns: null,
                creationOrder: 1));
            DamageAction action = battle.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 1);

            DamageActionResult result = battle.Execute(
                action,
                currentTurn: 1).ActionResults
                .OfType<DamageActionResult>()
                .Single();

            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(result.WasCritical, Is.True);
            Assert.That(battle.Random.NextFloatCallCount, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotUsesMaximumWithoutAddingValuesAndExpiresNextTurn()
        {
            BattleFixture snapshotHigher = CreateBattle(
                awakening: 6,
                initialTipsiness: 5,
                randomSample: 0.99f);
            snapshotHigher.State.RecordActiveTipsinessSnapshot(5);
            Assert.That(
                snapshotHigher.State.TryConsumeTipsiness(4),
                Is.True);

            DamageAction sameTurn = snapshotHigher.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 5);
            DamageAction nextTurn = snapshotHigher.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 6);

            Assert.That(sameTurn.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(nextTurn.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.06d).Within(0.000000001d));

            BattleFixture currentHigher = CreateBattle(
                awakening: 6,
                initialTipsiness: 1,
                randomSample: 0.99f);
            currentHigher.State.RecordActiveTipsinessSnapshot(5);
            currentHigher.State.GainTipsiness(4);
            DamageAction currentWins = currentHigher.CreateMatch(
                BoardMatchTier.Three,
                currentTurn: 5);

            Assert.That(currentWins.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.30d).Within(0.000000001d));
        }

        [Test]
        public void SameTurnSnapshotAlsoAppliesToAdditional()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 5,
                randomSample: 0.99f);
            battle.State.RecordActiveTipsinessSnapshot(7);
            Assert.That(battle.State.TryConsumeTipsiness(5), Is.True);
            battle.State.TryAcquireCup(MeimeiCup.First);
            battle.State.TryAcquireCup(MeimeiCup.Second);
            DamageAction source = battle.CreateMatch(
                BoardMatchTier.FiveOrMore,
                currentTurn: 7);

            DamageActionResult additional = battle.Execute(
                    source,
                    currentTurn: 7)
                .ActionResults
                .OfType<DamageActionResult>()
                .Single(item =>
                    item.Action.Origin == ActionOrigin.Additional);
            var action = (DamageAction)additional.Action;

            Assert.That(source.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(battle.State.Tipsiness, Is.EqualTo(2));
        }

        [Test]
        public void FailedActiveSimulationLeavesSnapshotAndTipsinessUntouched()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 5,
                randomSample: 0.99f);
            SimulateActiveResolution(
                battle.State,
                currentTurn: 8,
                activeSucceeded: false,
                tipsinessConsumed: 5);

            Assert.That(battle.State.Tipsiness, Is.EqualTo(5));
            Assert.That(battle.State.ActiveTipsinessSnapshot, Is.Zero);
            Assert.That(battle.State.ActiveTipsinessSnapshotTurn, Is.Zero);
            Assert.That(battle.State.HasActiveTipsinessSnapshotForTurn(8),
                Is.False);
        }

        [Test]
        public void MeimeiStateDoesNotAffectAnotherCharacterAttack()
        {
            BattleFixture battle = CreateBattle(
                awakening: 6,
                initialTipsiness: 5,
                randomSample: 0f,
                includeAlly: true);
            var action = new DamageAction(
                battle.ActionIds.Next(),
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    battle.Ally,
                    battle.Party,
                    battle.Boss,
                    ElementType.Fire,
                    AttackType.Match,
                    AttackTag.Match3,
                    1d,
                    appliesCombo: true,
                    finalComboCount: 1,
                    canCritical: true));

            DamageActionResult result = battle.Execute(
                action,
                currentTurn: 1,
                includeMeimeiRule: false).ActionResults
                .OfType<DamageActionResult>()
                .Single();

            Assert.That(action.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0d));
            Assert.That(result.WasCritical, Is.False);
            Assert.That(battle.Random.NextFloatCallCount, Is.Zero);
        }

        [Test]
        public void AwakeningSixProviderRequiresSharedBattleState()
        {
            MeimeiCombatConfig config = Config();

            Assert.Throws<ArgumentNullException>(() =>
                new MeimeiMatchActionProvider(config, awakening: 6));
        }

        private BattleFixture CreateBattle(
            int awakening,
            int initialTipsiness,
            float randomSample,
            bool includeAlly = false)
        {
            return new BattleFixture(
                Config(),
                awakening,
                initialTipsiness,
                randomSample,
                includeAlly);
        }

        private MeimeiCombatConfig Config()
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private static void SimulateActiveResolution(
            MeimeiBattleState state,
            int currentTurn,
            bool activeSucceeded,
            int tipsinessConsumed)
        {
            if (!activeSucceeded)
            {
                return;
            }

            state.RecordActiveTipsinessSnapshot(currentTurn);
            state.TryConsumeTipsiness(tipsinessConsumed);
        }

        private sealed class BattleFixture
        {
            private readonly MeimeiMatchActionProvider provider;
            private readonly MeimeiMatchCompletionRule rule;

            public BattleFixture(
                MeimeiCombatConfig config,
                int awakening,
                int initialTipsiness,
                float randomSample,
                bool includeAlly)
            {
                Meimei = new CharacterBattleState(
                    MeimeiRules.CharacterId,
                    0,
                    ElementType.Fire,
                    900,
                    1000d);
                Ally = new CharacterBattleState(
                    "character_critical_test_ally",
                    1,
                    ElementType.Fire,
                    1000,
                    500d);
                Party = new PartyBattleState(includeAlly
                    ? new[] { Meimei, Ally }
                    : new[] { Meimei });
                Boss = new BossBattleState(
                    "meimei_critical_test_boss",
                    ElementType.Light,
                    1000000,
                    100d);
                ActionIds = new CombatActionIdSequence();
                State = new MeimeiBattleState(Meimei, 5);
                if (initialTipsiness > 0)
                {
                    State.GainTipsiness(initialTipsiness);
                }

                provider = new MeimeiMatchActionProvider(
                    State,
                    config,
                    awakening);
                rule = new MeimeiMatchCompletionRule(
                    State,
                    config,
                    awakening,
                    ActionIds);
                Random = new FixedRandomSource(randomSample);
            }

            public CharacterBattleState Meimei { get; }
            public CharacterBattleState Ally { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MeimeiBattleState State { get; }
            public FixedRandomSource Random { get; }

            public DamageAction CreateMatch(
                BoardMatchTier tier,
                int currentTurn)
            {
                return (DamageAction)provider.CreateActions(
                    Meimei,
                    Party,
                    Boss,
                    tier,
                    finalComboCount: 1,
                    ActionIds,
                    currentTurn)[0];
            }

            public CombatActionExecutionResult Execute(
                DamageAction action,
                int currentTurn,
                bool includeMeimeiRule = true)
            {
                var rules = includeMeimeiRule
                    ? new ICombatTriggerRule[] { rule }
                    : Array.Empty<ICombatTriggerRule>();
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(Random),
                    new CombatTriggerResolver(rules));
                CombatActionExecutionSession session =
                    executor.BeginExecution(
                        new CombatAction[] { action },
                        currentTurn);
                while (session.TryExecuteNext(out _))
                {
                }

                return session.Result;
            }
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly float sample;

            public FixedRandomSource(float sample)
            {
                if (float.IsNaN(sample)
                    || float.IsInfinity(sample)
                    || sample < 0f
                    || sample >= 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(sample));
                }

                this.sample = sample;
            }

            public int NextFloatCallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (minInclusive >= maxExclusive)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(maxExclusive));
                }

                return minInclusive;
            }

            public float NextFloat()
            {
                NextFloatCallCount++;
                return sample;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0.03d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 0.06d, AttackTag.Match4)]
        public void ThreeAndFourCreateCostThenMatchDamage(
            BoardMatchTier tier,
            double expectedCostRate,
            AttackTag expectedTag)
        {
            using (var battle = new TestBattle(tier))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                var cost = battle.Actions[0] as PartyHpCostAction;
                var damage = battle.Actions[1] as DamageAction;

                Assert.That(cost, Is.Not.Null);
                Assert.That(cost.Origin, Is.EqualTo(ActionOrigin.Match));
                Assert.That(cost.TargetParty, Is.SameAs(battle.Party));
                Assert.That(cost.ReferenceMaxHp,
                    Is.EqualTo(battle.Character.MaxHp));
                Assert.That(cost.CostRate, Is.EqualTo(expectedCostRate));
                AssertRoot(cost);
                AssertMatchDamage(battle, damage, expectedTag);
                Assert.That(damage.ActionId, Is.EqualTo(cost.ActionId + 1L));
            }
        }

        [TestCase(BoardMatchTier.Three, 0.03d, 3L)]
        [TestCase(BoardMatchTier.Four, 0.06d, 6L)]
        public void HpCostUsesBeatriceMaxHpAndCommonFloorRule(
            BoardMatchTier tier,
            double expectedRate,
            long expectedCost)
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                var beatrice = Character(
                    BeatriceBloodstoneRules.CharacterId,
                    0,
                    ElementType.Dark,
                    101L,
                    1000d);
                var ally = Character(
                    "character_ally",
                    1,
                    ElementType.Fire,
                    899L,
                    1d);
                var party = new PartyBattleState(
                    new[] { beatrice, ally });
                BossBattleState boss = Boss(ElementType.Dark);
                var provider = new BeatriceBloodstoneMatchActionProvider(
                    config);
                IReadOnlyList<CombatAction> actions = provider.CreateActions(
                    beatrice,
                    party,
                    boss,
                    tier,
                    1,
                    new CombatActionIdSequence());

                var cost = (PartyHpCostAction)actions[0];
                Assert.That(cost.ReferenceMaxHp, Is.EqualTo(101L));
                Assert.That(cost.ReferenceMaxHp,
                    Is.Not.EqualTo(party.MaxHp));
                Assert.That(cost.CostRate, Is.EqualTo(expectedRate));

                CombatActionExecutionResult result = Execute(
                    party,
                    boss,
                    actions);
                var costResult =
                    (PartyHpCostActionResult)result.ActionResults[0];
                Assert.That(costResult.RequestedHpCost,
                    Is.EqualTo(expectedCost));
                Assert.That(costResult.AppliedHpCost,
                    Is.EqualTo(expectedCost));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase(BoardMatchTier.Three, 530L, 0.90d)]
        [TestCase(BoardMatchTier.Three, 520L, 1.10d)]
        [TestCase(BoardMatchTier.Four, 560L, 1.50d)]
        [TestCase(BoardMatchTier.Four, 550L, 2.30d)]
        public void ThreeAndFourResolveThresholdAfterCost(
            BoardMatchTier tier,
            long hpBefore,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(tier))
            {
                battle.SetPartyHp(hpBefore);

                CombatActionExecutionResult execution = battle.Execute();
                var cost = (PartyHpCostActionResult)
                    execution.ActionResults[0];
                var damage = (DamageActionResult)
                    execution.ActionResults[1];

                Assert.That(cost.HpAfter,
                    Is.EqualTo(tier == BoardMatchTier.Three
                        ? hpBefore - 30L
                        : hpBefore - 60L));
                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(1e-12d));
            }
        }

        [TestCase(BoardMatchTier.Three, 1.22d)]
        [TestCase(BoardMatchTier.Four, 2.53d)]
        public void AwakeningFourStrengthensSingleMatchCoefficient(
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(tier, awakening: 4))
            {
                battle.SetPartyHp(400L);

                CombatActionExecutionResult execution = battle.Execute();
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(1e-12d));
                Assert.That(battle.Actions.OfType<DamageAction>().Count(),
                    Is.EqualTo(1));
                Assert.That(battle.Actions.Any(action =>
                    action.Origin == ActionOrigin.Additional), Is.False);
            }
        }

        [Test]
        public void HpOneAppliesZeroCostAndStillExecutesDamage()
        {
            using (var battle = new TestBattle(BoardMatchTier.Four))
            {
                battle.SetPartyHp(1L);

                CombatActionExecutionResult execution = battle.Execute();

                Assert.That(execution.CompletedActionCount, Is.EqualTo(2));
                Assert.That(
                    ((PartyHpCostActionResult)execution.ActionResults[0])
                        .AppliedHpCost,
                    Is.Zero);
                Assert.That(execution.ActionResults[1],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(
                    ((DamageActionResult)execution.ActionResults[1])
                        .Context.SkillCoefficient,
                    Is.EqualTo(2.30d).Within(1e-12d));
            }
        }

        [Test]
        public void HpCostPreservesShieldAndDoesNotEmitHitDamageOrScore()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.Party.Shields.Add(new ShieldInstance(
                    1L,
                    "test_shield",
                    500L,
                    1,
                    3,
                    1L));
                var observer = new ResultObserverRule();

                CombatActionExecutionResult execution = battle.Execute(
                    observer);
                var collector = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());
                long collected = collector.Collect(execution);
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(500L));
                Assert.That(observer.PartyHpCostCount, Is.EqualTo(1));
                Assert.That(observer.BossDamageCount, Is.Zero);
                Assert.That(collected, Is.EqualTo(damage.AppliedDamage));
                Assert.That(collector.DamageScore,
                    Is.EqualTo(damage.AppliedDamage));
            }
        }

        [TestCase(0, 1000L, 2.70d)]
        [TestCase(0, 750L, 3.20d)]
        [TestCase(0, 500L, 3.70d)]
        [TestCase(0, 250L, 4.20d)]
        [TestCase(0, 1L, 4.698d)]
        [TestCase(5, 1000L, 3.00d)]
        [TestCase(5, 750L, 3.55d)]
        [TestCase(5, 500L, 4.10d)]
        [TestCase(5, 250L, 4.65d)]
        [TestCase(5, 1L, 5.1978d)]
        public void FiveMatchUsesExecutionTimePartyHpCoefficient(
            int awakening,
            long currentHp,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening))
            {
                battle.SetPartyHp(currentHp);

                CombatActionExecutionResult execution = battle.Execute();
                var damage = (DamageActionResult)
                    execution.ActionResults.Single();

                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(1e-12d));
            }
        }

        [Test]
        public void FiveMatchCreatesOnlyIdentifiableMatchDamage()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(1));
                Assert.That(battle.Actions.OfType<PartyHpCostAction>(),
                    Is.Empty);
                Assert.That(battle.Actions.OfType<HealAction>(), Is.Empty);
                DamageAction damage = (DamageAction)battle.Actions[0];
                AssertMatchDamage(
                    battle,
                    damage,
                    AttackTag.Match5Plus);
            }
        }

        [TestCase(0, 1000L, 0d)]
        [TestCase(0, 500L, 0.10d)]
        [TestCase(3, 500L, 0.12d)]
        [TestCase(3, 1L, 0.23976d)]
        public void PassiveIsResolvedDynamicallyForMatchDamage(
            int awakening,
            long currentHp,
            double expectedRate)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening))
            {
                battle.SetPartyHp(currentHp);

                DamageActionResult damage = (DamageActionResult)
                    battle.Execute().ActionResults.Single();

                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(expectedRate).Within(1e-12d));
                Assert.That(
                    battle.MatchDamage.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.Zero);
            }
        }

        [Test]
        public void PassiveAddsToExistingDealtDamageIncreaseOnce()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                battle.SetPartyHp(500L);
                battle.Character.Effects.ApplyEffect(Effect(
                    1L,
                    EffectModifierType.DealtDamageIncrease,
                    0.20d));

                DamageActionResult damage = (DamageActionResult)
                    battle.Execute().ActionResults.Single();

                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.30d).Within(1e-12d));
                Assert.That(damage.DamageResult.DealtDamageMultiplier,
                    Is.EqualTo(1.30d).Within(1e-12d));
            }
        }

        [Test]
        public void HpCostUpdatesCoefficientAndPassiveBeforeDamageBuild()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.SetPartyHp(520L);

                CombatActionExecutionResult execution = battle.Execute();
                var damage = (DamageActionResult)
                    execution.ActionResults[1];

                Assert.That(battle.Party.CurrentHp, Is.EqualTo(490L));
                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(1.10d).Within(1e-12d));
                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.102d).Within(1e-12d));
            }
        }

        [Test]
        public void HpChangedAfterActionCreationIsReadAtExecution()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                Assert.That(battle.Party.CurrentHp,
                    Is.EqualTo(battle.Party.MaxHp));
                battle.SetPartyHp(250L);

                DamageActionResult damage = (DamageActionResult)
                    battle.Execute().ActionResults.Single();

                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(4.20d).Within(1e-12d));
                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.15d).Within(1e-12d));
            }
        }

        [Test]
        public void FactoryAcceptsCostBeforeDamageAsIndependentRoots()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                createActions: false))
            {
                var factory = new MatchEventCombatActionFactory(
                    battle.Party,
                    battle.Boss,
                    battle.Provider,
                    battle.ActionIds);

                IReadOnlyList<CombatAction> actions =
                    factory.CreateCharacterRootActions(
                        CreateExecution(BoardMatchTier.Three, 2),
                        battle.Character);

                Assert.That(actions.Count, Is.EqualTo(2));
                Assert.That(actions[0], Is.TypeOf<PartyHpCostAction>());
                Assert.That(actions[1], Is.TypeOf<DamageAction>());
                Assert.That(actions, Has.All.Matches<CombatAction>(
                    action => action.RootActionId == action.ActionId
                        && !action.SourceActionId.HasValue));
                Assert.That(
                    ((DamageAction)actions[1]).ContextRequest.FinalComboCount,
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void RepeatedMatchEventsUseUniqueSequentialIds()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                createActions: false))
            {
                IReadOnlyList<CombatAction> first =
                    battle.Provider.CreateActions(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Three,
                        1,
                        battle.ActionIds);
                IReadOnlyList<CombatAction> second =
                    battle.Provider.CreateActions(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Four,
                        1,
                        battle.ActionIds);

                Assert.That(first.Concat(second).Select(action => action.ActionId),
                    Is.EqualTo(new long[] { 1L, 2L, 3L, 4L }));
                Assert.That(first.Concat(second).Select(action => action.ActionId)
                    .Distinct().Count(), Is.EqualTo(4));

                CombatActionExecutionResult execution = Execute(
                    battle.Party,
                    battle.Boss,
                    first.Concat(second).ToArray());
                Assert.That(execution.ActionResults.Select(
                    result => result.GetType()), Is.EqualTo(new[]
                    {
                        typeof(PartyHpCostActionResult),
                        typeof(DamageActionResult),
                        typeof(PartyHpCostActionResult),
                        typeof(DamageActionResult)
                    }));
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(910L));
            }
        }

        [Test]
        public void CostCompletionTriggerObservationDoesNotDisplaceDamage()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                var observer = new ResultObserverRule();

                CombatActionExecutionResult execution = battle.Execute(
                    observer);

                Assert.That(execution.ActionResults[0],
                    Is.TypeOf<PartyHpCostActionResult>());
                Assert.That(execution.ActionResults[1],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(observer.ResultTypes, Is.EqualTo(new[]
                {
                    typeof(PartyHpCostActionResult),
                    typeof(DamageActionResult)
                }));
            }
        }

        [Test]
        public void TerminalBattleClearsCostBeforeMutation()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                CombatActionExecutor executor = battle.CreateExecutor();
                executor.Execute(new CombatActionQueue(new CombatAction[]
                {
                    new DamageAction(
                        100L,
                        ActionOrigin.System,
                        new DamageContextBuildRequest(
                            battle.Character,
                            battle.Party,
                            battle.Boss,
                            ElementType.Dark,
                            AttackType.Match,
                            AttackTag.None,
                            100000d,
                            false,
                            0,
                            false))
                }));
                long hpBefore = battle.Party.CurrentHp;

                CombatActionExecutionResult result = executor.Execute(
                    new CombatActionQueue(battle.Actions));

                Assert.That(battle.Boss.IsDefeated, Is.True);
                Assert.That(result.CompletedActionCount, Is.Zero);
                Assert.That(result.StoppedEarly, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(hpBefore));
            }
        }

        [Test]
        public void MatchUsesComboCriticalAffinityAndOnlyMatchModifiers()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                finalComboCount: 3,
                bossElement: ElementType.Light))
            {
                battle.Character.Effects.ApplyEffect(Effect(
                    1L,
                    EffectModifierType.CriticalChanceIncrease,
                    1d));
                battle.Character.Effects.ApplyEffect(Effect(
                    2L,
                    EffectModifierType.AttackTypeDamageIncrease,
                    0.25d,
                    AttackTypeMask.Match));
                battle.Character.Effects.ApplyEffect(Effect(
                    3L,
                    EffectModifierType.AttackTypeDamageIncrease,
                    0.50d,
                    AttackTypeMask.Additional));

                DamageActionResult damage = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.AppliesCombo, Is.True);
                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(damage.Context.IsCritical, Is.True);
                Assert.That(damage.DamageResult.CriticalMultiplier,
                    Is.EqualTo(1.50d));
                Assert.That(damage.DamageResult.ElementAffinityMultiplier,
                    Is.EqualTo(1.30d));
                Assert.That(damage.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
            }
        }

        [Test]
        public void InvalidConfigAttackerAndTierAreRejected()
        {
            Assert.That(
                () => new BeatriceBloodstoneMatchActionProvider(null),
                Throws.ArgumentNullException
                    .With.Property("ParamName").EqualTo("config"));

            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                createActions: false))
            {
                CharacterBattleState wrong = Character(
                    "character_wrong",
                    1,
                    ElementType.Dark,
                    1000L,
                    1000d);
                Assert.That(
                    () => battle.Provider.CreateActions(
                        wrong,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Three,
                        1,
                        battle.ActionIds),
                    Throws.ArgumentException
                        .With.Property("ParamName").EqualTo("attacker"));
                Assert.That(
                    () => battle.Provider.CreateActions(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        (BoardMatchTier)99,
                        1,
                        battle.ActionIds),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("matchTier"));
            }
        }

        private static void AssertMatchDamage(
            TestBattle battle,
            DamageAction damage,
            AttackTag expectedTag)
        {
            Assert.That(damage, Is.Not.Null);
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
            AssertRoot(damage);
            Assert.That(damage.ContextRequest.Attacker,
                Is.SameAs(battle.Character));
            Assert.That(damage.ContextRequest.Party,
                Is.SameAs(battle.Party));
            Assert.That(damage.ContextRequest.TargetBoss,
                Is.SameAs(battle.Boss));
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Dark));
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(expectedTag));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
            Assert.That(damage.ContextRequest.CanCritical, Is.True);
        }

        private static void AssertRoot(CombatAction action)
        {
            Assert.That(action.RootActionId, Is.EqualTo(action.ActionId));
            Assert.That(action.SourceActionId, Is.Null);
        }

        private static EffectInstance Effect(
            long runtimeId,
            EffectModifierType modifierType,
            double magnitude,
            AttackTypeMask attackTypeMask = AttackTypeMask.None)
        {
            return new EffectInstance(
                runtimeId,
                $"test_effect_{runtimeId}",
                "test_source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                runtimeId,
                attackTypeMask: attackTypeMask);
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            ElementType element,
            long maxHp,
            double attack)
        {
            return new CharacterBattleState(
                id,
                slot,
                element,
                maxHp,
                attack);
        }

        private static BossBattleState Boss(ElementType element)
        {
            return new BossBattleState(
                "training_boss",
                element,
                100000000L,
                100d);
        }

        private static CombatActionExecutionResult Execute(
            PartyBattleState party,
            BossBattleState boss,
            IReadOnlyList<CombatAction> actions,
            params ICombatTriggerRule[] rules)
        {
            return new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(rules))
                .Execute(new CombatActionQueue(actions));
        }

        private static BossDamageResult BossDamage(long damage)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                damage,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }

        private static MatchEventExecution CreateExecution(
            BoardMatchTier tier,
            int finalComboCount)
        {
            int count = tier == BoardMatchTier.Three
                ? 3
                : tier == BoardMatchTier.Four ? 4 : 5;
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

        private sealed class TestBattle : IDisposable
        {
            private readonly BeatriceBloodstoneCombatConfig config;

            public TestBattle(
                BoardMatchTier tier,
                int awakening = 0,
                int finalComboCount = 1,
                ElementType bossElement = ElementType.Dark,
                bool createActions = true)
            {
                config = BeatriceBloodstoneTestConfig.Create();
                Character = CharacterBattleStateFactory();
                Party = new PartyBattleState(new[] { Character });
                Boss = BeatriceBloodstoneMatchActionProviderTests.Boss(
                    bossElement);
                Provider = new BeatriceBloodstoneMatchActionProvider(
                    config,
                    awakening);
                ActionIds = new CombatActionIdSequence();
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        Party,
                        Boss,
                        tier,
                        finalComboCount,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public BeatriceBloodstoneMatchActionProvider Provider { get; }
            public CombatActionIdSequence ActionIds { get; }
            public IReadOnlyList<CombatAction> Actions { get; }
            public DamageAction MatchDamage =>
                (DamageAction)Actions[Actions.Count - 1];

            public void SetPartyHp(long currentHp)
            {
                if (currentHp <= 0L || currentHp > Party.CurrentHp)
                {
                    throw new ArgumentOutOfRangeException(nameof(currentHp));
                }

                PartyDamageApplier.Apply(
                    Party,
                    BossDamage(Party.CurrentHp - currentHp));
            }

            public CombatActionExecutionResult Execute(
                params ICombatTriggerRule[] rules)
            {
                return BeatriceBloodstoneMatchActionProviderTests.Execute(
                    Party,
                    Boss,
                    Actions,
                    rules);
            }

            public CombatActionExecutor CreateExecutor()
            {
                return new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }

            private static CharacterBattleState CharacterBattleStateFactory()
            {
                return Character(
                    BeatriceBloodstoneRules.CharacterId,
                    0,
                    ElementType.Dark,
                    1000L,
                    1000d);
            }
        }

        private sealed class ResultObserverRule : ICombatTriggerRule
        {
            public int PartyHpCostCount { get; private set; }
            public int BossDamageCount { get; private set; }
            public List<Type> ResultTypes { get; } = new List<Type>();

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                ResultTypes.Add(context.CompletedResult.GetType());
                if (context.CompletedResult is PartyHpCostActionResult)
                {
                    PartyHpCostCount++;
                }

                if (context.CompletedResult is BossDamageActionResult)
                {
                    BossDamageCount++;
                }

                return Array.Empty<CombatAction>();
            }
        }
    }
}

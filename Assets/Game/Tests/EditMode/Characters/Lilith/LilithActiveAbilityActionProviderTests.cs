using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithActiveAbilityActionProviderTests
    {
        [TestCase(10000L, 1500L, 8500L)]
        [TestCase(1000L, 150L, 850L)]
        [TestCase(100L, 15L, 85L)]
        [TestCase(99L, 14L, 85L)]
        [TestCase(10L, 1L, 9L)]
        [TestCase(5L, 0L, 5L)]
        [TestCase(1L, 0L, 1L)]
        public void ActiveCostsFloorOfCurrentHpAndKeepsAtLeastOne(
            long currentHp,
            long expectedCost,
            long expectedHp)
        {
            using (var battle = new TestBattle(currentHp))
            {
                CombatActionExecutionResult result = battle.ExecuteActive();
                var cost = (PartyHpCostActionResult)result.ActionResults[0];

                Assert.That(cost.RequestedHpCost, Is.EqualTo(expectedCost));
                Assert.That(cost.AppliedHpCost, Is.EqualTo(expectedCost));
                Assert.That(cost.HpBefore, Is.EqualTo(currentHp));
                Assert.That(cost.HpAfter, Is.EqualTo(expectedHp));
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(expectedHp));
            }
        }

        [Test]
        public void CostReferenceSnapshotsCurrentHpInsteadOfMaximumHp()
        {
            using (var battle = new TestBattle(1000L))
            {
                battle.SetCurrentHp(99L);

                IReadOnlyList<CombatAction> actions = battle.CreateActions();
                var cost = (PartyHpCostAction)actions[0];
                battle.SetCurrentHp(90L);
                CombatActionExecutionResult result = battle.Execute(actions);

                Assert.That(battle.Party.MaxHp, Is.EqualTo(1000L));
                Assert.That(cost.ReferenceMaxHp, Is.EqualTo(99L));
                Assert.That(cost.CostRate, Is.EqualTo(0.15d));
                Assert.That(
                    ((PartyHpCostActionResult)result.ActionResults[0])
                        .AppliedHpCost,
                    Is.EqualTo(14L));
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(76L));
            }
        }

        [TestCase(0, 2)]
        [TestCase(3, 5)]
        [TestCase(10, 12)]
        [TestCase(11, 12)]
        [TestCase(12, 12)]
        public void ActiveAddsTwoCovenantStacksWithoutOverflow(
            int initialStacks,
            int expectedStacks)
        {
            using (var battle = new TestBattle(1000L))
            {
                battle.AddCovenant(initialStacks);

                CombatActionExecutionResult result = battle.ExecuteActive();

                Assert.That(result.ActionResults, Has.Count.EqualTo(3));
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<PartyHpCostActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(result.ActionResults[2],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(battle.Boss),
                    Is.EqualTo(expectedStacks));
            }
        }

        [Test]
        public void ActiveActionsAreOrderedIndependentActiveRoots()
        {
            using (var battle = new TestBattle(1000L))
            {
                IReadOnlyList<CombatAction> actions = battle.CreateActions();

                Assert.That(actions, Has.Count.EqualTo(3));
                Assert.That(actions[0], Is.TypeOf<PartyHpCostAction>());
                Assert.That(actions[1], Is.TypeOf<ApplyEffectAction>());
                Assert.That(actions[2], Is.TypeOf<ApplyEffectAction>());
                Assert.That(actions.OfType<DamageAction>(), Is.Empty);
                for (int index = 0; index < actions.Count; index++)
                {
                    Assert.That(actions[index].ActionId,
                        Is.EqualTo(index + 1));
                    Assert.That(actions[index].Origin,
                        Is.EqualTo(ActionOrigin.Active));
                    Assert.That(actions[index].RootActionId,
                        Is.EqualTo(actions[index].ActionId));
                    Assert.That(actions[index].SourceActionId, Is.Null);
                    Assert.That(actions[index].CanTriggerFollowUp, Is.False);
                }

                foreach (ApplyEffectAction apply in
                    actions.OfType<ApplyEffectAction>())
                {
                    Assert.That(apply.TargetType,
                        Is.EqualTo(CombatEffectTargetType.Boss));
                    Assert.That(apply.Effect.EffectId,
                        Is.EqualTo(LilithRules.CovenantEffectId));
                }

                Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(8));
                Assert.That(LilithRules.ActiveAbilityId,
                    Is.EqualTo("active_lilith_soul_bargain"));
            }
        }

        [Test]
        public void SessionAppliesCostBeforeEachCovenantStack()
        {
            using (var battle = new TestBattle(1000L))
            {
                ActiveAbilityCombatExecutor executor =
                    battle.CreateActiveExecutor(out BattleFlowCoordinator flow);
                flow.StartBattle();

                Assert.That(executor.TryBeginExecution(
                    0,
                    out ActiveAbilityCombatExecutionSession session),
                    Is.True);
                Assert.That(flow.Context.ActiveAbilities[0]
                    .RemainingCooldown, Is.EqualTo(8));

                Assert.That(session.TryExecuteNext(out var costStep), Is.True);
                Assert.That(costStep.ActionStep.Result,
                    Is.TypeOf<PartyHpCostActionResult>());
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(850L));
                Assert.That(CovenantCount(battle), Is.Zero);

                Assert.That(session.TryExecuteNext(out _), Is.True);
                Assert.That(CovenantCount(battle), Is.EqualTo(1));
                Assert.That(session.TryExecuteNext(out _), Is.True);
                Assert.That(CovenantCount(battle), Is.EqualTo(2));
                Assert.That(session.IsCompleted, Is.True);
            }
        }

        [Test]
        public void CostIgnoresShieldAndDamageReductionAndEmitsNoDamage()
        {
            using (var battle = new TestBattle(1000L))
            {
                battle.Party.Shields.Add(new ShieldInstance(
                    1L,
                    "test_shield",
                    500L,
                    1,
                    3,
                    1L));
                EffectInstance fallenBlessing =
                    battle.CreateFallenBlessing(2L);
                EffectInstance otherReduction = new EffectInstance(
                    3L,
                    "effect_test_other_party_reduction",
                    "character_other",
                    EffectCategory.Buff,
                    EffectModifierType.PartyDamageReduction,
                    0.50d,
                    null,
                    3L,
                    EffectStackPolicy.Unique);
                battle.Party.Effects.ApplyEffect(fallenBlessing);
                battle.Party.Effects.ApplyEffect(otherReduction);
                var trigger = new DamageResultTrackingRule();

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions(),
                    trigger);
                var score = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());

                Assert.That(battle.Party.CurrentHp, Is.EqualTo(850L));
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(500L));
                Assert.That(battle.Party.Effects.GetActiveEffects(),
                    Does.Contain(fallenBlessing));
                Assert.That(battle.Party.Effects.GetActiveEffects(),
                    Does.Contain(otherReduction));
                Assert.That(trigger.DamageResultCount, Is.Zero);
                Assert.That(result.ActionResults.OfType<DamageActionResult>(),
                    Is.Empty);
                Assert.That(result.ActionResults
                    .OfType<BossDamageActionResult>(), Is.Empty);
                Assert.That(score.Collect(result), Is.Zero);
                Assert.That(score.DamageScore, Is.Zero);
            }
        }

        [Test]
        public void ZeroCostStillUsesActiveAddsCovenantAndStartsCooldown()
        {
            using (var battle = new TestBattle(5L))
            {
                ActiveAbilityCombatExecutor executor =
                    battle.CreateActiveExecutor(out BattleFlowCoordinator flow);
                flow.StartBattle();

                Assert.That(executor.CanExecute(0), Is.True);
                Assert.That(executor.TryExecute(0, out var result), Is.True);

                var cost = (PartyHpCostActionResult)result.ActionResults[0];
                Assert.That(cost.AppliedHpCost, Is.Zero);
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(5L));
                Assert.That(CovenantCount(battle), Is.EqualTo(2));
                Assert.That(flow.Context.ActiveAbilities[0]
                    .RemainingCooldown, Is.EqualTo(8));
                Assert.That(executor.CanExecute(0), Is.False);
                Assert.That(executor.TryExecute(0, out _), Is.False);
            }
        }

        [Test]
        public void AwakeningSixChangesOnlyCovenantMagnitude()
        {
            using (var battle = new TestBattle(1000L, awakening: 6))
            {
                IReadOnlyList<CombatAction> actions = battle.CreateActions();
                var cost = (PartyHpCostAction)actions[0];
                ApplyEffectAction covenant = actions
                    .OfType<ApplyEffectAction>()
                    .First();

                Assert.That(cost.CostRate, Is.EqualTo(0.15d));
                Assert.That(covenant.Effect.Magnitude,
                    Is.EqualTo(0.018d));
                Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(8));
                Assert.That(actions.OfType<ApplyEffectAction>().Count(),
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void CooldownEightIsUsableOnTurnsOneNineSeventeenAndTwentyFive()
        {
            using (var battle = new TestBattle(10000L))
            {
                ActiveAbilityCombatExecutor executor =
                    battle.CreateActiveExecutor(out BattleFlowCoordinator flow);
                flow.StartBattle();

                for (int turn = 1; turn <= 25; turn++)
                {
                    bool expected = turn == 1
                        || turn == 9
                        || turn == 17
                        || turn == 25;
                    Assert.That(executor.CanExecute(0), Is.EqualTo(expected),
                        $"Turn {turn}");
                    if (expected)
                    {
                        Assert.That(executor.TryExecute(0, out _), Is.True,
                            $"Turn {turn}");
                        Assert.That(flow.Context.ActiveAbilities[0]
                            .RemainingCooldown, Is.EqualTo(8));
                    }

                    BattleFlowTestSupport.CompleteTurnWithSingleMatch(flow);
                }
            }
        }

        [Test]
        public void BeatriceActiveUsesHpAndCovenantAfterLilithActive()
        {
            LilithCombatConfig lilithConfig = LilithTestConfig.Create();
            BeatriceBloodstoneCombatConfig beatriceConfig =
                ValorChronicle.Tests.EditMode.Characters.Beatrice
                    .BeatriceBloodstoneTestConfig.Create();
            try
            {
                var lilith = new CharacterBattleState(
                    LilithRules.CharacterId,
                    0,
                    ElementType.Dark,
                    1000L,
                    1000d);
                var beatrice = new CharacterBattleState(
                    BeatriceBloodstoneRules.CharacterId,
                    1,
                    ElementType.Dark,
                    1000L,
                    1000d);
                var party = new PartyBattleState(
                    new[] { lilith, beatrice });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Dark,
                    1000000L,
                    1000d);
                var ids = new CombatActionIdSequence();
                var lilithProvider = new LilithActiveAbilityActionProvider(
                    lilithConfig);
                var beatriceProvider =
                    new BeatriceBloodstoneActiveActionProvider(
                        beatriceConfig);
                var executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));

                executor.Execute(new CombatActionQueue(
                    lilithProvider.CreateActions(
                        lilith,
                        party,
                        boss,
                        ids)));
                DamageAction beatriceAction = (DamageAction)
                    beatriceProvider.CreateActions(
                        beatrice,
                        party,
                        boss,
                        ids).Single();
                DamageActionResult result = (DamageActionResult)executor
                    .Execute(new CombatActionQueue(
                        new CombatAction[] { beatriceAction }))
                    .ActionResults.Single();

                Assert.That(party.MaxHp, Is.EqualTo(2000L));
                Assert.That(party.CurrentHp, Is.EqualTo(1700L));
                Assert.That(result.Context.SkillCoefficient,
                    Is.EqualTo(3.8d).Within(0.0000001d));
                Assert.That(result.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.03d).Within(0.0000001d));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lilithConfig);
                UnityEngine.Object.DestroyImmediate(beatriceConfig);
            }
        }

        private static int CovenantCount(TestBattle battle)
        {
            return LilithCovenantEffectFactory.GetStackCount(battle.Boss);
        }

        private sealed class DamageResultTrackingRule : ICombatTriggerRule
        {
            public int DamageResultCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.CompletedResult is DamageActionResult
                    || context.CompletedResult is BossDamageActionResult)
                {
                    DamageResultCount++;
                }

                return Array.Empty<CombatAction>();
            }
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly LilithCombatConfig config;
            private readonly LilithResolvedCombatRules rules;
            private long nextEffectId = 1000L;

            public TestBattle(long partyHp, int awakening = 0)
            {
                config = LilithTestConfig.Create();
                rules = LilithCombatRuleResolver.Resolve(config, awakening);
                Character = new CharacterBattleState(
                    LilithRules.CharacterId,
                    0,
                    ElementType.Dark,
                    partyHp,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Dark,
                    1000000L,
                    1000d);
                ActionIds = new CombatActionIdSequence();
                Provider = new LilithActiveAbilityActionProvider(
                    config,
                    awakening);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public LilithActiveAbilityActionProvider Provider { get; }

            public IReadOnlyList<CombatAction> CreateActions()
            {
                return Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    ActionIds);
            }

            public CombatActionExecutionResult ExecuteActive()
            {
                return Execute(CreateActions());
            }

            public CombatActionExecutionResult Execute(
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

            public ActiveAbilityCombatExecutor CreateActiveExecutor(
                out BattleFlowCoordinator flow)
            {
                flow = new BattleFlowCoordinator(
                    25,
                    new[] { Provider.CooldownTurns });
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.Register(
                    LilithRules.CharacterId,
                    LilithRules.ActiveAbilityId,
                    Provider);
                return new ActiveAbilityCombatExecutor(
                    flow,
                    Party,
                    Boss,
                    new CombatActionExecutor(
                        Boss,
                        Party,
                        new DamageContextFactory(
                            new SeededRandomSource(123))),
                    ActionIds,
                    new[]
                    {
                        new ActiveAbilityBinding(
                            0,
                            Character.PartySlotIndex,
                            LilithRules.CharacterId,
                            LilithRules.ActiveAbilityId)
                    },
                    registry);
            }

            public void SetCurrentHp(long hp)
            {
                if (hp <= 0L || hp > Party.CurrentHp)
                {
                    throw new ArgumentOutOfRangeException(nameof(hp));
                }

                PartyDamageApplier.Apply(
                    Party,
                    BossDamageCalculator.Calculate(
                        new BossDamageContext(
                            Party.CurrentHp - hp,
                            0d,
                            0d,
                            1d,
                            0d,
                            0d,
                            0d,
                            0d)));
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

            public EffectInstance CreateFallenBlessing(long runtimeId)
            {
                return LilithFallenBlessingEffectFactory.Create(
                    runtimeId,
                    rules);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

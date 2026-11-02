using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielActiveActionProviderTests
    {
        [TestCase(0, 0.45d, 0d)]
        [TestCase(2, 0.45d, 0.05d)]
        [TestCase(6, 0.52d, 0.05d)]
        public void CreatesCostThenShieldWithResolvedAwakeningValues(
            int awakening,
            double expectedShieldRate,
            double expectedLocalIncrease)
        {
            using (var battle = new TestBattle(awakening))
            {
                IReadOnlyList<CombatAction> actions =
                    battle.CreateActions(playerTurnNumber: 7);

                Assert.That(actions.Count, Is.EqualTo(2));
                Assert.That(actions[0], Is.TypeOf<PartyHpCostAction>());
                Assert.That(actions[1], Is.TypeOf<ShieldAction>());
                Assert.That(actions.OfType<DamageAction>(), Is.Empty);

                var cost = (PartyHpCostAction)actions[0];
                Assert.That(cost.TargetParty, Is.SameAs(battle.Party));
                Assert.That(cost.ReferenceMaxHp,
                    Is.EqualTo(battle.Party.CurrentHp));
                Assert.That(cost.CostRate, Is.EqualTo(0.05d));
                Assert.That(cost.RoundingMode,
                    Is.EqualTo(PartyHpCostRoundingMode.Ceiling));

                var shield = (ShieldAction)actions[1];
                Assert.That(shield.ContextRequest.ShieldCoefficient,
                    Is.EqualTo(expectedShieldRate));
                Assert.That(shield.ContextRequest.AppliesCombo, Is.False);
                Assert.That(shield.ContextRequest.FinalComboCount,
                    Is.Zero);
                Assert.That(
                    shield.ContextRequest.ActionLocalShieldAmountIncreaseRate,
                    Is.EqualTo(expectedLocalIncrease));
                Assert.That(shield.GrantRequest.SourceId,
                    Is.EqualTo(LucielRules.ActiveAbilityId));
                Assert.That(shield.GrantRequest.CreatedTurn, Is.EqualTo(7));
                Assert.That(shield.GrantRequest.RemainingTurns, Is.Null);
                Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(8));
                AssertIndependentActiveRoots(actions);
            }
        }

        [Test]
        public void ExecutionUsesCeilingCurrentHpAndNoComboShield()
        {
            using (var battle = new TestBattle())
            {
                battle.SetCurrentHp(101L);
                IReadOnlyList<CombatAction> actions = battle.CreateActions();
                CombatActionExecutionResult result = battle.Execute(actions);

                var cost = (PartyHpCostActionResult)result.ActionResults[0];
                var shield = (ShieldActionResult)result.ActionResults[1];
                Assert.That(cost.RequestedHpCost, Is.EqualTo(6L));
                Assert.That(cost.AppliedHpCost, Is.EqualTo(6L));
                Assert.That(cost.HpAfter, Is.EqualTo(95L));
                Assert.That(shield.GenerationResult.ComboMultiplier,
                    Is.EqualTo(1d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(450L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(450L));
                Assert.That(battle.Party.Shields.ActiveShields.Single()
                        .RemainingTurns,
                    Is.Null);
            }
        }

        [TestCase(0, 450L)]
        [TestCase(2, 472L)]
        [TestCase(6, 546L)]
        public void ExecutionUsesResolvedShieldCoefficientAndLocalIncrease(
            int awakening,
            long expectedShield)
        {
            using (var battle = new TestBattle(awakening))
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions());
                var shield = (ShieldActionResult)result.ActionResults[1];

                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(expectedShield));
            }
        }

        [Test]
        public void CostSnapshotsCurrentHpAtActionCreation()
        {
            using (var battle = new TestBattle())
            {
                battle.SetCurrentHp(101L);
                IReadOnlyList<CombatAction> actions = battle.CreateActions();
                battle.SetCurrentHp(100L);

                CombatActionExecutionResult result = battle.Execute(actions);
                var action = (PartyHpCostAction)actions[0];
                var cost = (PartyHpCostActionResult)result.ActionResults[0];

                Assert.That(action.ReferenceMaxHp, Is.EqualTo(101L));
                Assert.That(cost.RequestedHpCost, Is.EqualTo(6L));
                Assert.That(cost.AppliedHpCost, Is.EqualTo(6L));
                Assert.That(cost.HpAfter, Is.EqualTo(94L));
            }
        }

        [Test]
        public void AtOneHpZeroActualCostStillCreatesShield()
        {
            using (var battle = new TestBattle())
            {
                battle.SetCurrentHp(1L);

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions());
                var cost = (PartyHpCostActionResult)result.ActionResults[0];
                var shield = (ShieldActionResult)result.ActionResults[1];

                Assert.That(cost.RequestedHpCost, Is.EqualTo(1L));
                Assert.That(cost.AppliedHpCost, Is.Zero);
                Assert.That(cost.HpAfter, Is.EqualTo(1L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(450L));
            }
        }

        [Test]
        public void FullShieldCapStillPaysHpCostAndExecutesShieldAction()
        {
            using (var battle = new TestBattle())
            {
                battle.AddShieldAtPartyCap();

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions());
                var cost = (PartyHpCostActionResult)result.ActionResults[0];
                var shield = (ShieldActionResult)result.ActionResults[1];

                Assert.That(cost.AppliedHpCost, Is.EqualTo(50L));
                Assert.That(battle.Party.CurrentHp, Is.EqualTo(950L));
                Assert.That(shield.ApplicationResult.RequestedShieldAmount,
                    Is.EqualTo(450L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.Zero);
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void HpCostBypassesShieldAndReductionAndEmitsNoDamageResult()
        {
            using (var battle = new TestBattle())
            {
                battle.Party.Shields.Add(new ShieldInstance(
                    runtimeId: 1000L,
                    sourceId: "test_existing_shield",
                    initialAmount: 200L,
                    createdTurn: 1,
                    remainingTurns: null,
                    creationOrder: 1000L));
                battle.Party.Effects.ApplyEffect(new EffectInstance(
                    1001L,
                    "effect_test_damage_reduction",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.PartyDamageReduction,
                    0.90d,
                    remainingTurns: null,
                    creationOrder: 1001L,
                    stackPolicy: EffectStackPolicy.Unique));
                var tracker = new DamageResultTracker();

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions(),
                    tracker);

                Assert.That(battle.Party.CurrentHp, Is.EqualTo(950L));
                Assert.That(result.ActionResults.OfType<DamageActionResult>(),
                    Is.Empty);
                Assert.That(result.ActionResults
                    .OfType<BossDamageActionResult>(), Is.Empty);
                Assert.That(tracker.DamageResultCount, Is.Zero);
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(650L));
            }
        }

        private static void AssertIndependentActiveRoots(
            IReadOnlyList<CombatAction> actions)
        {
            for (int index = 0; index < actions.Count; index++)
            {
                Assert.That(actions[index].ActionId, Is.EqualTo(index + 1));
                Assert.That(actions[index].Origin,
                    Is.EqualTo(ActionOrigin.Active));
                Assert.That(actions[index].RootActionId,
                    Is.EqualTo(actions[index].ActionId));
                Assert.That(actions[index].SourceActionId, Is.Null);
            }
        }

        private sealed class DamageResultTracker : ICombatTriggerRule
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
            private readonly LucielCombatConfig config;

            public TestBattle(int awakening = 0)
            {
                config = LucielTestConfig.Create();
                Character = new CharacterBattleState(
                    LucielRules.CharacterId,
                    partySlotIndex: 0,
                    ElementType.Dark,
                    maxHp: 1000L,
                    attack: 100d);
                Party = new PartyBattleState(
                    new CharacterBattleState[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    maxHp: 100000L,
                    attack: 100d);
                ActionIds = new CombatActionIdSequence();
                Provider = new LucielActiveActionProvider(
                    config,
                    awakening);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public LucielActiveActionProvider Provider { get; }

            public IReadOnlyList<CombatAction> CreateActions(
                int playerTurnNumber = 1)
            {
                return Provider.CreateActions(
                    Character,
                    Party,
                    playerTurnNumber,
                    ActionIds);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] rules)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void SetCurrentHp(long currentHp)
            {
                if (currentHp <= 0L || currentHp > Party.CurrentHp)
                {
                    throw new ArgumentOutOfRangeException(nameof(currentHp));
                }

                long cost = Party.CurrentHp - currentHp;
                if (cost > 0L)
                {
                    PartyHpCostApplier.Apply(
                        Party,
                        referenceMaxHp: cost,
                        costRate: 1d);
                }
            }

            public void AddShieldAtPartyCap()
            {
                Party.Shields.Add(new ShieldInstance(
                    runtimeId: 1000L,
                    sourceId: "test_cap_shield",
                    initialAmount: Party.MaxHp,
                    createdTurn: 1,
                    remainingTurns: null,
                    creationOrder: 1000L));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

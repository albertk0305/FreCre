using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneActiveAndStateTests
    {
        private BeatriceBloodstoneCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = BeatriceBloodstoneTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(0, 1000L, 3.50d, 0d)]
        [TestCase(0, 750L, 4.00d, 0.05d)]
        [TestCase(0, 500L, 4.50d, 0.10d)]
        [TestCase(0, 250L, 5.00d, 0.15d)]
        [TestCase(0, 1L, 5.498d, 0.1998d)]
        [TestCase(6, 1000L, 4.00d, 0d)]
        [TestCase(6, 500L, 5.20d, 0.12d)]
        [TestCase(6, 1L, 6.3976d, 0.23976d)]
        public void ActiveResolvesCoefficientAndPassiveAtExecutionHp(
            int awakening,
            long hp,
            double expectedCoefficient,
            double expectedDealtIncrease)
        {
            TestBattle battle = CreateBattle(awakening);
            DamageAction action = battle.CreateActive();
            SetHp(battle.Party, hp);

            DamageActionResult result = (DamageActionResult)Execute(
                battle,
                new CombatAction[] { action }).ActionResults.Single();

            Assert.That(result.Context.SkillCoefficient,
                Is.EqualTo(expectedCoefficient).Within(0.0000001d));
            Assert.That(result.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(expectedDealtIncrease).Within(0.0000001d));
        }

        [Test]
        public void ActiveCreatesExactlyOneIndependentRootDamage()
        {
            TestBattle battle = CreateBattle(awakening: 0);

            IReadOnlyList<CombatAction> actions = battle.Provider
                .CreateActions(
                    battle.Character,
                    battle.Party,
                    battle.Boss,
                    battle.ActionIds);

            Assert.That(actions.Count, Is.EqualTo(1));
            var damage = actions[0] as DamageAction;
            Assert.That(damage, Is.Not.Null);
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Active));
            Assert.That(damage.RootActionId, Is.EqualTo(damage.ActionId));
            Assert.That(damage.SourceActionId, Is.Null);
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Active));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(AttackTag.None));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.False);
            Assert.That(actions.OfType<PartyHpCostAction>(), Is.Empty);
            Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(8));
        }

        [Test]
        public void ActionCreatedAtFullHpUsesLaterHalfHpState()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            DamageAction action = battle.CreateActive();

            SetHp(battle.Party, 500L);
            DamageActionResult result = (DamageActionResult)Execute(
                battle,
                new CombatAction[] { action }).ActionResults[0];

            Assert.That(result.Context.SkillCoefficient,
                Is.EqualTo(4.5d));
            Assert.That(result.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.0000001d));
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(500L));
        }

        [Test]
        public void ActiveFilterAndPassiveAddToExistingDealtIncreaseOnce()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.Character.Effects.ApplyEffect(AttackTypeEffect(
                1L,
                0.15d,
                AttackTypeMask.Active));
            battle.Character.Effects.ApplyEffect(AttackTypeEffect(
                2L,
                0.50d,
                AttackTypeMask.Match));
            battle.Party.Effects.ApplyEffect(DealtDamageEffect(
                3L,
                0.05d));
            SetHp(battle.Party, 500L);

            DamageActionResult result = (DamageActionResult)Execute(
                battle,
                new CombatAction[] { battle.CreateActive() })
                .ActionResults[0];

            Assert.That(result.Context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.0000001d));
            Assert.That(result.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.0000001d));
            Assert.That(result.DamageResult.FinalDamage,
                Is.EqualTo(5951L));
        }

        [Test]
        public void ActiveExecutorCommitsEightTurnCooldownWithoutHpCost()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var coordinator = new BattleFlowCoordinator(25, new[] { 8 });
            var registry = new ActiveAbilityActionProviderRegistry();
            registry.Register(
                BeatriceBloodstoneRules.CharacterId,
                BeatriceBloodstoneRules.ActiveAbilityId,
                battle.Provider);
            var activeExecutor = new ActiveAbilityCombatExecutor(
                coordinator,
                battle.Party,
                battle.Boss,
                new CombatActionExecutor(
                    battle.Boss,
                    battle.Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123))),
                battle.ActionIds,
                new[]
                {
                    new ActiveAbilityBinding(
                        0,
                        0,
                        BeatriceBloodstoneRules.CharacterId,
                        BeatriceBloodstoneRules.ActiveAbilityId)
                },
                registry);
            coordinator.StartBattle();
            long hpBefore = battle.Party.CurrentHp;

            Assert.That(activeExecutor.CanExecute(0), Is.True);
            Assert.That(activeExecutor.TryExecute(0, out var result),
                Is.True);
            Assert.That(result.ActionResults.Count, Is.EqualTo(1));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(8));
            Assert.That(activeExecutor.CanExecute(0), Is.False);
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(hpBefore));
        }

        [Test]
        public void StatusVisibilityTracksBoundPartyAndNeverShowsBadge()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var state = new BeatriceBloodstoneBattleState(
                battle.Character);

            Assert.That(state.IsVisible, Is.False);
            Assert.That(state.IsBadgeVisible, Is.False);
            Assert.That(state.CurrentCount, Is.Zero);
            Assert.That(state.StatusId,
                Is.EqualTo(BeatriceBloodstoneRules.PassiveSkillId));

            state.BindPartyBattleState(battle.Party);
            Assert.That(state.IsVisible, Is.False);
            SetHp(battle.Party, 999L);
            Assert.That(state.IsVisible, Is.True);

            Execute(
                battle,
                new CombatAction[]
                {
                    new HealAction(
                        battle.ActionIds.Next(),
                        ActionOrigin.System,
                        new HealingContextBuildRequest(
                            battle.Character,
                            battle.Party,
                            healingCoefficient: 1d,
                            appliesCombo: false,
                            finalComboCount: 0))
                });
            Assert.That(state.IsVisible, Is.False);
        }

        [Test]
        public void StatusRejectsAnotherPartyAndRebinding()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var state = new BeatriceBloodstoneBattleState(
                battle.Character);
            var stranger = new CharacterBattleState(
                "character_stranger", 1, ElementType.Fire, 100L, 10d);
            var otherParty = new PartyBattleState(
                new CharacterBattleState[] { stranger });

            Assert.That(
                () => state.BindPartyBattleState(otherParty),
                Throws.ArgumentException);
            state.BindPartyBattleState(battle.Party);
            Assert.That(
                () => state.BindPartyBattleState(otherParty),
                Throws.ArgumentException);
        }

        private TestBattle CreateBattle(int awakening)
        {
            var character = new CharacterBattleState(
                BeatriceBloodstoneRules.CharacterId,
                0,
                ElementType.Dark,
                1000L,
                1000d);
            var party = new PartyBattleState(
                new CharacterBattleState[] { character });
            var boss = new BossBattleState(
                "training_boss", ElementType.Dark, 100000000L, 100d);
            return new TestBattle(
                character,
                party,
                boss,
                new CombatActionIdSequence(),
                new BeatriceBloodstoneActiveActionProvider(
                    config,
                    awakening));
        }

        private static CombatActionExecutionResult Execute(
            TestBattle battle,
            IReadOnlyList<CombatAction> actions)
        {
            return new CombatActionExecutor(
                battle.Boss,
                battle.Party,
                new DamageContextFactory(new SeededRandomSource(123)))
                .Execute(new CombatActionQueue(actions));
        }

        private static void SetHp(PartyBattleState party, long hp)
        {
            long damage = party.CurrentHp - hp;
            if (damage <= 0L)
            {
                return;
            }

            PartyDamageApplier.Apply(
                party,
                BossDamageCalculator.Calculate(new BossDamageContext(
                    damage, 0d, 0d, 1d, 0d, 0d, 0d, 0d)));
        }

        private static EffectInstance AttackTypeEffect(
            long id,
            double magnitude,
            AttackTypeMask attackTypes)
        {
            return new EffectInstance(
                id,
                $"test_effect_{id}",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                magnitude,
                2,
                id,
                attackTypeMask: attackTypes);
        }

        private static EffectInstance DealtDamageEffect(
            long id,
            double magnitude)
        {
            return new EffectInstance(
                id,
                $"test_effect_{id}",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.DealtDamageIncrease,
                magnitude,
                2,
                id);
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                BeatriceBloodstoneActiveActionProvider provider)
            {
                Character = character;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                Provider = provider;
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BeatriceBloodstoneActiveActionProvider Provider { get; }

            public DamageAction CreateActive()
            {
                return (DamageAction)Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    ActionIds)[0];
            }
        }
    }
}

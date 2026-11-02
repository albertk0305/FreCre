using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0, 0.06d, 0d, 1)]
        [TestCase(BoardMatchTier.Three, 2, 0.06d, 0.05d, 1)]
        [TestCase(BoardMatchTier.Three, 3, 0.07d, 0.05d, 1)]
        [TestCase(BoardMatchTier.Four, 0, 0.11d, 0d, 1)]
        [TestCase(BoardMatchTier.Four, 3, 0.12d, 0.05d, 1)]
        [TestCase(BoardMatchTier.FiveOrMore, 0, 0.18d, 0d, 2)]
        [TestCase(BoardMatchTier.FiveOrMore, 4, 0.20d, 0.05d, 2)]
        public void CreatesConfirmedActionsAndAwakeningValues(
            BoardMatchTier tier,
            int awakening,
            double expectedShieldRate,
            double expectedLocalIncrease,
            int expectedActionCount)
        {
            using (var battle = new TestBattle(awakening))
            {
                IReadOnlyList<CombatAction> actions = battle.CreateActions(
                    tier,
                    finalComboCount: 3,
                    playerTurnNumber: 7);

                Assert.That(actions.Count,
                    Is.EqualTo(expectedActionCount));
                Assert.That(actions[0], Is.TypeOf<ShieldAction>());
                Assert.That(actions.OfType<DamageAction>(), Is.Empty);

                var shield = (ShieldAction)actions[0];
                Assert.That(shield.ContextRequest.ShieldCoefficient,
                    Is.EqualTo(expectedShieldRate));
                Assert.That(shield.ContextRequest.AppliesCombo, Is.True);
                Assert.That(shield.ContextRequest.FinalComboCount,
                    Is.EqualTo(3));
                Assert.That(
                    shield.ContextRequest.ActionLocalShieldAmountIncreaseRate,
                    Is.EqualTo(expectedLocalIncrease));
                Assert.That(shield.GrantRequest.RemainingTurns, Is.Null);
                Assert.That(shield.GrantRequest.CreatedTurn, Is.EqualTo(7));
                Assert.That(shield.GrantRequest.SourceId,
                    Is.EqualTo(ExpectedSkillId(tier)));

                AssertIndependentRoots(actions, ActionOrigin.Match);
                if (tier == BoardMatchTier.FiveOrMore)
                {
                    AssertMatch5Effect(
                        (ApplyEffectAction)actions[1],
                        awakening >= 4 ? 0.14d : 0.12d);
                }
            }
        }

        [Test]
        public void FiveMatchExecutesShieldBeforePartyBuff()
        {
            using (var battle = new TestBattle(awakening: 4))
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions(
                        BoardMatchTier.FiveOrMore,
                        finalComboCount: 3,
                        playerTurnNumber: 2));

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<ShieldActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
                var shield = (ShieldActionResult)result.ActionResults[0];
                Assert.That(shield.GenerationResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(246L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(246L));
                Assert.That(battle.Party.Shields.ActiveShields.Single()
                        .RemainingTurns,
                    Is.Null);
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Party.Effects.GetActiveEffects().Single()
                        .ModifierType,
                    Is.EqualTo(EffectModifierType.DealtDamageIncrease));
            }
        }

        [Test]
        public void LocalAndCollectedShieldIncreasesAddInCommonPipeline()
        {
            using (var battle = new TestBattle(awakening: 2))
            {
                battle.Party.Effects.ApplyEffect(new EffectInstance(
                    100L,
                    "effect_test_shield_increase",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.ShieldAmountIncrease,
                    0.18d,
                    remainingTurns: null,
                    creationOrder: 100L,
                    stackPolicy: EffectStackPolicy.Unique));

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions(
                        BoardMatchTier.Three,
                        finalComboCount: 1,
                        playerTurnNumber: 1));
                var shield = (ShieldActionResult)result.ActionResults.Single();

                Assert.That(shield.Context.ShieldAmountIncreaseRateSum,
                    Is.EqualTo(0.23d).Within(0.000000001d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(77L));
            }
        }

        [Test]
        public void FullShieldCapDoesNotPreventMatch5Buff()
        {
            using (var battle = new TestBattle())
            {
                battle.AddShieldAtPartyCap();

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateActions(
                        BoardMatchTier.FiveOrMore,
                        finalComboCount: 1,
                        playerTurnNumber: 1));
                var shield = (ShieldActionResult)result.ActionResults[0];

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.Zero);
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void RepeatedMatch5RefreshesOneEffectInstance()
        {
            using (var battle = new TestBattle())
            {
                battle.Execute(battle.CreateActions(
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    playerTurnNumber: 1));
                EffectInstance first = battle.Party.Effects
                    .GetActiveEffects().Single();
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(first.RemainingTurns, Is.EqualTo(2));

                battle.Execute(battle.CreateActions(
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    playerTurnNumber: 2));

                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Party.Effects.GetActiveEffects().Single(),
                    Is.SameAs(first));
                Assert.That(first.RemainingTurns, Is.EqualTo(3));
            }
        }

        private static void AssertMatch5Effect(
            ApplyEffectAction action,
            double expectedMagnitude)
        {
            Assert.That(action.TargetType,
                Is.EqualTo(CombatEffectTargetType.Party));
            Assert.That(action.Effect.EffectId,
                Is.EqualTo(LucielRules.Match5DamageIncreaseEffectId));
            Assert.That(action.Effect.SourceId,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(action.Effect.Category,
                Is.EqualTo(EffectCategory.Buff));
            Assert.That(action.Effect.ModifierType,
                Is.EqualTo(EffectModifierType.DealtDamageIncrease));
            Assert.That(action.Effect.Magnitude,
                Is.EqualTo(expectedMagnitude));
            Assert.That(action.Effect.RemainingTurns, Is.EqualTo(3));
            Assert.That(action.Effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(action.Effect.ModifierGroupId, Is.Null);
            Assert.That(action.Effect.ModifierGroupCap, Is.Null);
        }

        private static void AssertIndependentRoots(
            IReadOnlyList<CombatAction> actions,
            ActionOrigin origin)
        {
            for (int index = 0; index < actions.Count; index++)
            {
                Assert.That(actions[index].ActionId, Is.EqualTo(index + 1));
                Assert.That(actions[index].Origin, Is.EqualTo(origin));
                Assert.That(actions[index].RootActionId,
                    Is.EqualTo(actions[index].ActionId));
                Assert.That(actions[index].SourceActionId, Is.Null);
            }
        }

        private static string ExpectedSkillId(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return LucielRules.Match3SkillId;
                case BoardMatchTier.Four:
                    return LucielRules.Match4SkillId;
                case BoardMatchTier.FiveOrMore:
                    return LucielRules.Match5SkillId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
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
                    maxHp: 1050L,
                    attack: 100d);
                Party = new PartyBattleState(
                    new CharacterBattleState[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    maxHp: 100000L,
                    attack: 100d);
                ActionIds = new CombatActionIdSequence();
                Provider = new LucielMatchActionProvider(config, awakening);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public LucielMatchActionProvider Provider { get; }

            public IReadOnlyList<CombatAction> CreateActions(
                BoardMatchTier tier,
                int finalComboCount,
                int playerTurnNumber)
            {
                return Provider.CreateActions(
                    Character,
                    Party,
                    tier,
                    finalComboCount,
                    playerTurnNumber,
                    ActionIds);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
                return executor.Execute(new CombatActionQueue(actions));
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

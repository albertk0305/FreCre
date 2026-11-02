using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class PartyHpCostActionTests
    {
        [Test]
        public void Action_DefaultsToFloorRounding()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out _);

            var action = new PartyHpCostAction(
                1,
                ActionOrigin.System,
                party,
                101,
                0.05d);

            Assert.That(action.RoundingMode,
                Is.EqualTo(PartyHpCostRoundingMode.Floor));
        }

        [TestCase(0.03d, 3L)]
        [TestCase(0.06d, 6L)]
        public void Execute_CalculatesCostFromExplicitMaxHpWithFloor(
            double costRate,
            long expectedCost)
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);

            PartyHpCostActionResult result = ExecuteCost(
                party,
                boss,
                referenceMaxHp: 101,
                costRate);

            Assert.That(result.RequestedHpCost, Is.EqualTo(expectedCost));
            Assert.That(result.AppliedHpCost, Is.EqualTo(expectedCost));
            Assert.That(result.HpBefore, Is.EqualTo(1000L));
            Assert.That(result.HpAfter, Is.EqualTo(1000L - expectedCost));
            Assert.That(party.CurrentHp, Is.EqualTo(result.HpAfter));
        }

        [TestCase(10000L, 500L, 500L, 9500L)]
        [TestCase(1001L, 51L, 51L, 950L)]
        [TestCase(101L, 6L, 6L, 95L)]
        [TestCase(20L, 1L, 1L, 19L)]
        [TestCase(2L, 1L, 1L, 1L)]
        [TestCase(1L, 1L, 0L, 1L)]
        public void Execute_CeilingRoundsCurrentHpCostAndKeepsAtLeastOne(
            long currentHp,
            long expectedRequestedCost,
            long expectedAppliedCost,
            long expectedHpAfter)
        {
            CreateBattle(
                currentHp,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);

            PartyHpCostActionResult result = ExecuteCost(
                party,
                boss,
                currentHp,
                0.05d,
                PartyHpCostRoundingMode.Ceiling);

            Assert.That(result.RequestedHpCost,
                Is.EqualTo(expectedRequestedCost));
            Assert.That(result.AppliedHpCost,
                Is.EqualTo(expectedAppliedCost));
            Assert.That(result.HpBefore, Is.EqualTo(currentHp));
            Assert.That(result.HpAfter, Is.EqualTo(expectedHpAfter));
            Assert.That(party.CurrentHp, Is.EqualTo(expectedHpAfter));
        }

        [Test]
        public void Execute_ActionPassesCeilingModeToApplier()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            PartyHpCostAction action = Cost(
                1,
                party,
                101,
                0.05d,
                PartyHpCostRoundingMode.Ceiling);

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(
                    new CombatActionQueue(
                        new CombatAction[] { action }));
            var result = (PartyHpCostActionResult)
                execution.ActionResults[0];

            Assert.That(action.RoundingMode,
                Is.EqualTo(PartyHpCostRoundingMode.Ceiling));
            Assert.That(result.RequestedHpCost, Is.EqualTo(6L));
            Assert.That(result.AppliedHpCost, Is.EqualTo(6L));
            Assert.That(result.HpAfter, Is.EqualTo(994L));
        }

        [Test]
        public void Applier_OverflowingRawCostSaturatesRequestedCost()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out _);

            PartyHpCostApplicationResult result =
                PartyHpCostApplier.Apply(
                    party,
                    long.MaxValue,
                    double.MaxValue,
                    PartyHpCostRoundingMode.Ceiling);

            Assert.That(result.RequestedHpCost,
                Is.EqualTo(long.MaxValue));
            Assert.That(result.AppliedHpCost, Is.EqualTo(999L));
            Assert.That(result.HpAfter, Is.EqualTo(1L));
        }

        [Test]
        public void ActionAndApplier_RejectUndefinedRoundingMode()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out _);
            var invalid = (PartyHpCostRoundingMode)999;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PartyHpCostAction(
                    1,
                    ActionOrigin.System,
                    party,
                    1000,
                    0.05d,
                    roundingMode: invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartyHpCostApplier.Apply(
                    party,
                    1000,
                    0.05d,
                    invalid));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-0.01d)]
        public void ActionAndApplier_RejectInvalidCostRate(double costRate)
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out _);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PartyHpCostAction(
                    1,
                    ActionOrigin.System,
                    party,
                    1000,
                    costRate));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PartyHpCostApplier.Apply(
                    party,
                    1000,
                    costRate));
        }

        [Test]
        public void Execute_CapsCostAtCurrentHpMinusOne()
        {
            CreateBattle(
                100,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            PartyDamageApplier.Apply(party, BossDamage(95));

            PartyHpCostActionResult result = ExecuteCost(
                party,
                boss,
                referenceMaxHp: 100,
                costRate: 0.10d);

            Assert.That(result.RequestedHpCost, Is.EqualTo(10L));
            Assert.That(result.AppliedHpCost, Is.EqualTo(4L));
            Assert.That(result.HpBefore, Is.EqualTo(5L));
            Assert.That(result.HpAfter, Is.EqualTo(1L));
            Assert.That(party.IsIncapacitated, Is.False);
        }

        [Test]
        public void Execute_AtOneHpSucceedsWithZeroAppliedCost()
        {
            CreateBattle(
                100,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            PartyDamageApplier.Apply(party, BossDamage(99));

            PartyHpCostActionResult result = ExecuteCost(
                party,
                boss,
                referenceMaxHp: 100,
                costRate: 0.06d);

            Assert.That(result.RequestedHpCost, Is.EqualTo(6L));
            Assert.That(result.AppliedHpCost, Is.Zero);
            Assert.That(result.HpBefore, Is.EqualTo(1L));
            Assert.That(result.HpAfter, Is.EqualTo(1L));
        }

        [Test]
        public void Execute_AtOneHpContinuesToFollowingDamageAction()
        {
            CreateBattle(
                100,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            PartyDamageApplier.Apply(party, BossDamage(99));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Cost(1, party, 100, 0.06d),
                Damage(2, character, party, boss, 1d)
            });

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(queue);

            Assert.That(execution.CompletedActionCount, Is.EqualTo(2));
            Assert.That(execution.StoppedEarly, Is.False);
            Assert.That(
                ((PartyHpCostActionResult)execution.ActionResults[0])
                    .AppliedHpCost,
                Is.Zero);
            Assert.That(execution.ActionResults[1],
                Is.TypeOf<DamageActionResult>());
            Assert.That(boss.CurrentHp, Is.EqualTo(9900L));
        }

        [Test]
        public void Execute_DoesNotConsumeOrChangeShields()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            party.Shields.Add(new ShieldInstance(
                1,
                "test_source",
                500,
                1,
                3,
                1));

            PartyHpCostActionResult result = ExecuteCost(
                party,
                boss,
                referenceMaxHp: 1000,
                costRate: 0.06d);

            Assert.That(result.AppliedHpCost, Is.EqualTo(60L));
            Assert.That(party.CurrentHp, Is.EqualTo(940L));
            Assert.That(party.Shields.TotalShield, Is.EqualTo(500L));
        }

        [Test]
        public void Execute_DoesNotConsumeHitReductionOrEmitBossDamageResult()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            EffectInstance reduction = new EffectInstance(
                1,
                "next_hit_reduction",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.NextBossHitDamageReduction,
                0.40d,
                null,
                1,
                EffectStackPolicy.Unique);
            party.Effects.ApplyEffect(reduction);
            var hitRule = new BossDamageResultTrackingRule();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(
                    new ICombatTriggerRule[] { hitRule }));

            CombatActionExecutionResult execution = executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    Cost(1, party, 1000, 0.06d)
                }));

            Assert.That(execution.ActionResults[0],
                Is.TypeOf<PartyHpCostActionResult>());
            Assert.That(hitRule.BossDamageResultCount, Is.Zero);
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { reduction }));
        }

        [Test]
        public void Execute_HpCostIsExcludedFromEnemyDamageAndScore()
        {
            CreateBattle(
                1000,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Cost(1, party, 1000, 0.06d),
                Damage(2, character, party, boss, 1d)
            });

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(queue);
            var collector = new BattleDamageScoreCollector(
                new BattleDamageScoreAccumulator());

            Assert.That(boss.CurrentHp, Is.EqualTo(9900L));
            Assert.That(collector.Collect(execution), Is.EqualTo(100L));
            Assert.That(collector.DamageScore, Is.EqualTo(100L));
        }

        [Test]
        public void Execute_SequentialActionsUseLatestPartyHp()
        {
            CreateBattle(
                100,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            var observedHp = new List<long>();
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Cost(1, party, 100, 0.10d),
                Cost(2, party, 100, 0.20d),
                new DamageAction(
                    3,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Active,
                        AttackTag.None,
                        1d,
                        false,
                        0,
                        false,
                        skillCoefficientResolver: request =>
                        {
                            observedHp.Add(request.Party.CurrentHp);
                            return request.Party.CurrentHp / 100d;
                        }))
            });

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(queue);

            var first = (PartyHpCostActionResult)execution.ActionResults[0];
            var second = (PartyHpCostActionResult)execution.ActionResults[1];
            var damage = (DamageActionResult)execution.ActionResults[2];
            Assert.That(first.HpBefore, Is.EqualTo(100L));
            Assert.That(first.HpAfter, Is.EqualTo(90L));
            Assert.That(second.HpBefore, Is.EqualTo(90L));
            Assert.That(second.HpAfter, Is.EqualTo(70L));
            Assert.That(observedHp, Is.EqualTo(new long[] { 70L }));
            Assert.That(damage.AppliedDamage, Is.EqualTo(70L));
        }

        [Test]
        public void Execute_ExistingBossDamageStillUsesShieldThenHp()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            party.Shields.Add(new ShieldInstance(
                1,
                "test_source",
                100,
                1,
                3,
                1));
            var action = new BossDamageAction(
                1,
                new BossDamageContextBuildRequest(
                    boss,
                    party,
                    1d,
                    AttackTag.None,
                    BossDamageHitType.Direct));

            var result = (BossDamageActionResult)Executor(party, boss)
                .Execute(new CombatActionQueue(
                    new CombatAction[] { action }))
                .ActionResults[0];

            Assert.That(result.ApplicationResult.RequestedDamage,
                Is.EqualTo(1000L));
            Assert.That(result.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(100L));
            Assert.That(result.ApplicationResult.HpDamage,
                Is.EqualTo(900L));
            Assert.That(party.CurrentHp, Is.EqualTo(100L));
        }

        [Test]
        public void Execute_RejectsPartyFromAnotherBattleBeforeMutation()
        {
            CreateBattle(
                1000,
                out _,
                out PartyBattleState party,
                out BossBattleState boss);
            var otherParty = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "other",
                    0,
                    ElementType.Fire,
                    1000,
                    100d)
            });

            Assert.Throws<InvalidOperationException>(() =>
                Executor(party, boss).Execute(
                    new CombatActionQueue(new CombatAction[]
                    {
                        Cost(1, otherParty, 1000, 0.06d)
                    })));
            Assert.That(otherParty.CurrentHp, Is.EqualTo(1000L));
        }

        private static PartyHpCostActionResult ExecuteCost(
            PartyBattleState party,
            BossBattleState boss,
            long referenceMaxHp,
            double costRate,
            PartyHpCostRoundingMode roundingMode =
                PartyHpCostRoundingMode.Floor)
        {
            return (PartyHpCostActionResult)Executor(party, boss)
                .Execute(new CombatActionQueue(new CombatAction[]
                {
                    Cost(
                        1,
                        party,
                        referenceMaxHp,
                        costRate,
                        roundingMode)
                }))
                .ActionResults[0];
        }

        private static PartyHpCostAction Cost(
            long actionId,
            PartyBattleState party,
            long referenceMaxHp,
            double costRate,
            PartyHpCostRoundingMode roundingMode =
                PartyHpCostRoundingMode.Floor)
        {
            return new PartyHpCostAction(
                actionId,
                ActionOrigin.System,
                party,
                referenceMaxHp,
                costRate,
                roundingMode: roundingMode);
        }

        private static DamageAction Damage(
            long actionId,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            double coefficient)
        {
            return new DamageAction(
                actionId,
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    character,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Active,
                    AttackTag.None,
                    coefficient,
                    false,
                    0,
                    false));
        }

        private static CombatActionExecutor Executor(
            PartyBattleState party,
            BossBattleState boss)
        {
            return new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
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

        private static void CreateBattle(
            long partyHp,
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                partyHp,
                100d);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                1000d);
        }

        private sealed class BossDamageResultTrackingRule
            : ICombatTriggerRule
        {
            public int BossDamageResultCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.CompletedResult is BossDamageActionResult)
                {
                    BossDamageResultCount++;
                }

                return Array.Empty<CombatAction>();
            }
        }
    }
}

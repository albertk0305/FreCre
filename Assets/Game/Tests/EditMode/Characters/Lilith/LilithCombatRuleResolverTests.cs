using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Lilith;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithCombatRuleResolverTests
    {
        [TestCase(0, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(1, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(2, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(3, 0.75d, 1.30d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(4, 0.75d, 1.30d, 2.10d, 0.25d, 3.00d, 0.015d)]
        [TestCase(5, 0.75d, 1.30d, 2.10d, 0.25d, 3.30d, 0.015d)]
        [TestCase(6, 0.75d, 1.30d, 2.10d, 0.25d, 3.30d, 0.018d)]
        public void ResolveAppliesOnlySpecifiedAwakeningChanges(
            int awakening,
            double expectedMatch3,
            double expectedMatch4,
            double expectedMatch5,
            double expectedDamageReduction,
            double expectedExplosionCoefficient,
            double expectedContractDamageRate)
        {
            WithRules(awakening, rules =>
            {
                Assert.That(rules.Match3Coefficient,
                    Is.EqualTo(expectedMatch3));
                Assert.That(rules.Match4Coefficient,
                    Is.EqualTo(expectedMatch4));
                Assert.That(rules.Match5Coefficient,
                    Is.EqualTo(expectedMatch5));
                Assert.That(rules.CorruptedBlessingDamageReductionRate,
                    Is.EqualTo(expectedDamageReduction));
                Assert.That(rules.ContractExplosionCoefficientPerUnit,
                    Is.EqualTo(expectedExplosionCoefficient));
                Assert.That(rules.ContractDamageIncreaseRatePerStack,
                    Is.EqualTo(expectedContractDamageRate));
            });
        }

        [Test]
        public void ResolvedRulesExposeSharedContractMatchAndActivePolicies()
        {
            WithRules(0, rules =>
            {
                Assert.That(rules.InitialContractStackCount, Is.Zero);
                Assert.That(rules.ContractMaxStackCount, Is.EqualTo(12));
                Assert.That(rules.ContractRemainingTurns, Is.Null);
                Assert.That(rules.ContractStackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(rules.Match3ContractGainAmount, Is.EqualTo(1));
                Assert.That(rules.Match4ContractGainAmount, Is.EqualTo(2));
                Assert.That(rules.Match5ContractGainAmount, Is.EqualTo(3));
                Assert.That(rules.ActiveContractGainAmount, Is.EqualTo(2));
                Assert.That(rules.ContractExplosionMinimumStackCount,
                    Is.EqualTo(4));
                Assert.That(rules.ContractConsumptionUnit, Is.EqualTo(4));
                Assert.That(rules.MatchAttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(rules.MatchUsesCombo, Is.True);
                Assert.That(rules.ApplyContractAfterMatchDamage, Is.True);

                Assert.That(rules.CorruptedBlessingDurationTurns,
                    Is.EqualTo(2));
                Assert.That(rules.CorruptedBlessingMaxStackCount,
                    Is.EqualTo(1));
                Assert.That(rules.CorruptedBlessingStackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(rules.CorruptedBlessingAffectsDirectHpCost,
                    Is.False);

                Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(rules.ActiveCurrentPartyHpCostRate,
                    Is.EqualTo(0.15d));
                Assert.That(rules.ActiveAvailableAtBattleStart, Is.True);
                Assert.That(rules.ActiveHpCostUsesCurrentPartyHp, Is.True);
                Assert.That(rules.ActiveMinimumPartyHpAfterCost,
                    Is.EqualTo(1L));
                Assert.That(rules.ActiveHpCostRoundsDown, Is.True);
                Assert.That(rules.ActiveUsableWhenActualHpCostIsZero,
                    Is.True);
                Assert.That(rules.ActiveHpCostIgnoresShields, Is.True);
                Assert.That(rules.ActiveHpCostIgnoresDamageReduction,
                    Is.True);
                Assert.That(rules.ApplyContractAfterActiveHpCost, Is.True);

                Assert.That(rules.ContractExplosionAttackType,
                    Is.EqualTo(AttackType.Passive));
                Assert.That(rules.ContractExplosionUsesCombo, Is.False);
                Assert.That(
                    rules.ContractExplosionRunsAtMostOncePerBossAction,
                    Is.True);
            });
        }

        [TestCase(3, 0.70d, 1)]
        [TestCase(4, 1.20d, 2)]
        [TestCase(5, 1.90d, 3)]
        [TestCase(6, 1.90d, 3)]
        [TestCase(12, 1.90d, 3)]
        public void MatchRulesBucketFiveAndLargerMatches(
            int matchSize,
            double expectedCoefficient,
            int expectedContractGain)
        {
            WithRules(0, rules =>
            {
                Assert.That(rules.GetMatchCoefficient(matchSize),
                    Is.EqualTo(expectedCoefficient));
                Assert.That(rules.GetMatchContractGainAmount(matchSize),
                    Is.EqualTo(expectedContractGain));
                Assert.That(rules.CanApplyCorruptedBlessing(matchSize),
                    Is.EqualTo(matchSize >= 5));
            });
        }

        [TestCase(0, 0, 0, 0, false)]
        [TestCase(1, 0, 1, 0, false)]
        [TestCase(2, 0, 2, 0, false)]
        [TestCase(3, 0, 3, 0, false)]
        [TestCase(4, 4, 0, 1, true)]
        [TestCase(5, 4, 1, 1, true)]
        [TestCase(6, 4, 2, 1, true)]
        [TestCase(7, 4, 3, 1, true)]
        [TestCase(8, 8, 0, 2, true)]
        [TestCase(9, 8, 1, 2, true)]
        [TestCase(10, 8, 2, 2, true)]
        [TestCase(11, 8, 3, 2, true)]
        [TestCase(12, 12, 0, 3, true)]
        public void ResolveContractExplosionConsumesCompleteUnits(
            int stackCount,
            int expectedConsumed,
            int expectedRemaining,
            int expectedUnits,
            bool expectedTrigger)
        {
            WithRules(0, rules =>
            {
                LilithContractExplosionResult result =
                    rules.ResolveContractExplosion(stackCount);
                Assert.That(result.ConsumedStackCount,
                    Is.EqualTo(expectedConsumed));
                Assert.That(result.RemainingStackCount,
                    Is.EqualTo(expectedRemaining));
                Assert.That(result.ExplosionUnitCount,
                    Is.EqualTo(expectedUnits));
                Assert.That(result.DamageCoefficient,
                    Is.EqualTo(expectedUnits * 3.00d));
                Assert.That(result.ShouldTrigger,
                    Is.EqualTo(expectedTrigger));
            });
        }

        [TestCase(0, 4, 3.00d)]
        [TestCase(0, 8, 6.00d)]
        [TestCase(0, 12, 9.00d)]
        [TestCase(5, 4, 3.30d)]
        [TestCase(5, 8, 6.60d)]
        [TestCase(5, 12, 9.90d)]
        public void ExplosionCoefficientUsesAwakeningFiveValuePerUnit(
            int awakening,
            int stackCount,
            double expectedCoefficient)
        {
            WithRules(awakening, rules =>
                Assert.That(
                    rules.ResolveContractExplosion(stackCount)
                        .DamageCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(0.0000001d)));
        }

        [TestCase(0, 12, 0.18d)]
        [TestCase(6, 12, 0.216d)]
        public void ContractDamageIncreaseUsesResolvedPerStackRate(
            int awakening,
            int stackCount,
            double expected)
        {
            WithRules(awakening, rules =>
                Assert.That(
                    rules.CalculateContractDamageIncreaseRate(stackCount),
                    Is.EqualTo(expected).Within(0.0000001d)));
        }

        [Test]
        public void ResolutionAndCalculationsAreDeterministic()
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                LilithResolvedCombatRules first =
                    LilithCombatRuleResolver.Resolve(config, 6);
                LilithResolvedCombatRules second =
                    LilithCombatRuleResolver.Resolve(config, 6);
                LilithContractExplosionResult firstExplosion =
                    first.ResolveContractExplosion(11);
                LilithContractExplosionResult secondExplosion =
                    second.ResolveContractExplosion(11);

                Assert.That(second.Match3Coefficient,
                    Is.EqualTo(first.Match3Coefficient));
                Assert.That(second.ContractDamageIncreaseRatePerStack,
                    Is.EqualTo(first.ContractDamageIncreaseRatePerStack));
                Assert.That(secondExplosion.ConsumedStackCount,
                    Is.EqualTo(firstExplosion.ConsumedStackCount));
                Assert.That(secondExplosion.RemainingStackCount,
                    Is.EqualTo(firstExplosion.RemainingStackCount));
                Assert.That(secondExplosion.DamageCoefficient,
                    Is.EqualTo(firstExplosion.DamageCoefficient));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                Assert.That(
                    () => LilithCombatRuleResolver.Resolve(
                        config,
                        awakening),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("awakening"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveRejectsMissingOrInvalidConfig()
        {
            Assert.That(
                () => LilithCombatRuleResolver.Resolve(null, 0),
                Throws.ArgumentNullException
                    .With.Property("ParamName").EqualTo("config"));

            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                LilithTestConfig.SetField(
                    config,
                    "contractMaxStackCount",
                    0);
                Assert.That(
                    () => LilithCombatRuleResolver.Resolve(config, 0),
                    Throws.ArgumentException
                        .With.Property("ParamName").EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(13)]
        public void ContractCalculationsRejectUnsupportedStackCounts(
            int stackCount)
        {
            WithRules(0, rules =>
            {
                Assert.That(
                    () => rules.ResolveContractExplosion(stackCount),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName")
                        .EqualTo("contractStackCount"));
                Assert.That(
                    () => rules.CalculateContractDamageIncreaseRate(
                        stackCount),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName")
                        .EqualTo("contractStackCount"));
            });
        }

        [TestCase(0)]
        [TestCase(2)]
        public void MatchRulesRejectUnsupportedSmallMatchSize(int matchSize)
        {
            WithRules(0, rules =>
            {
                Assert.That(
                    () => rules.GetMatchCoefficient(matchSize),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("matchSize"));
                Assert.That(
                    () => rules.GetMatchContractGainAmount(matchSize),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("matchSize"));
            });
        }

        private static void WithRules(
            int awakening,
            Action<LilithResolvedCombatRules> assertion)
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                assertion(LilithCombatRuleResolver.Resolve(
                    config,
                    awakening));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

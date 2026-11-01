using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Lilith;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsSpecifiedValues()
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                Assert.That(LilithRules.CharacterId,
                    Is.EqualTo("character_lilith"));
                Assert.That(config.InitialContractStackCount, Is.Zero);
                Assert.That(config.ContractMaxStackCount, Is.EqualTo(12));
                Assert.That(config.ContractRemainingTurns, Is.Null);
                Assert.That(config.ContractStackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(config.Match3ContractGainAmount, Is.EqualTo(1));
                Assert.That(config.Match4ContractGainAmount, Is.EqualTo(2));
                Assert.That(config.Match5ContractGainAmount, Is.EqualTo(3));
                Assert.That(config.ActiveContractGainAmount, Is.EqualTo(2));
                Assert.That(config.ContractExplosionMinimumStackCount,
                    Is.EqualTo(4));
                Assert.That(config.ContractConsumptionUnit, Is.EqualTo(4));
                Assert.That(config.ContractDamageIncreaseRatePerStack,
                    Is.EqualTo(0.015d));
                Assert.That(
                    config.Awakening6ContractDamageIncreaseRatePerStack,
                    Is.EqualTo(0.018d));
                Assert.That(config.ContractExplosionCoefficientPerUnit,
                    Is.EqualTo(3.00d));
                Assert.That(
                    config.Awakening5ContractExplosionCoefficientPerUnit,
                    Is.EqualTo(3.30d));

                Assert.That(config.Match3Coefficient, Is.EqualTo(0.70d));
                Assert.That(config.Match4Coefficient, Is.EqualTo(1.20d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(1.90d));
                Assert.That(config.Awakening3Match3Coefficient,
                    Is.EqualTo(0.75d));
                Assert.That(config.Awakening3Match4Coefficient,
                    Is.EqualTo(1.30d));
                Assert.That(config.Awakening4Match5Coefficient,
                    Is.EqualTo(2.10d));
                Assert.That(config.MatchAttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(config.MatchUsesCombo, Is.True);
                Assert.That(config.ApplyContractAfterMatchDamage, Is.True);

                Assert.That(config.CorruptedBlessingDamageReductionRate,
                    Is.EqualTo(0.20d));
                Assert.That(
                    config.Awakening4CorruptedBlessingDamageReductionRate,
                    Is.EqualTo(0.25d));
                Assert.That(config.CorruptedBlessingDurationTurns,
                    Is.EqualTo(2));
                Assert.That(config.CorruptedBlessingMaxStackCount,
                    Is.EqualTo(1));
                Assert.That(config.CorruptedBlessingStackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(config.CorruptedBlessingAffectsDirectHpCost,
                    Is.False);

                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.ActiveCurrentPartyHpCostRate,
                    Is.EqualTo(0.15d));
                Assert.That(config.ActiveAvailableAtBattleStart, Is.True);
                Assert.That(config.ActiveHpCostUsesCurrentPartyHp, Is.True);
                Assert.That(config.ActiveMinimumPartyHpAfterCost,
                    Is.EqualTo(1L));
                Assert.That(config.ActiveHpCostRoundsDown, Is.True);
                Assert.That(config.ActiveUsableWhenActualHpCostIsZero,
                    Is.True);
                Assert.That(config.ActiveHpCostIgnoresShields, Is.True);
                Assert.That(config.ActiveHpCostIgnoresDamageReduction,
                    Is.True);
                Assert.That(config.ApplyContractAfterActiveHpCost, Is.True);

                Assert.That(config.ContractExplosionAttackType,
                    Is.EqualTo(AttackType.Passive));
                Assert.That(config.ContractExplosionUsesCombo, Is.False);
                Assert.That(
                    config.ContractExplosionRunsAtMostOncePerBossAction,
                    Is.True);
                Assert.That(config.TryValidate(out string error), Is.True);
                Assert.That(error, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("contractDamageIncreaseRatePerStack", -0.001d)]
        [TestCase("awakening6ContractDamageIncreaseRatePerStack", 1.001d)]
        [TestCase("contractExplosionCoefficientPerUnit", double.NaN)]
        [TestCase("awakening5ContractExplosionCoefficientPerUnit", -0.01d)]
        [TestCase("match3Coefficient", double.PositiveInfinity)]
        [TestCase("awakening4Match5Coefficient", -0.01d)]
        [TestCase("corruptedBlessingDamageReductionRate", -0.01d)]
        [TestCase("awakening4CorruptedBlessingDamageReductionRate", 1.01d)]
        [TestCase("activeCurrentPartyHpCostRate", 1.01d)]
        public void InvalidDoubleIsRejected(string fieldName, double value)
        {
            WithInvalidField(fieldName, value);
        }

        [TestCase("initialContractStackCount", 1)]
        [TestCase("contractMaxStackCount", 0)]
        [TestCase("contractMaxStackCount", 10)]
        [TestCase("match3ContractGainAmount", 0)]
        [TestCase("match5ContractGainAmount", 13)]
        [TestCase("activeContractGainAmount", 0)]
        [TestCase("contractExplosionMinimumStackCount", 3)]
        [TestCase("contractConsumptionUnit", 0)]
        [TestCase("contractConsumptionUnit", 5)]
        [TestCase("corruptedBlessingDurationTurns", 0)]
        [TestCase("corruptedBlessingMaxStackCount", 2)]
        [TestCase("activeCooldownTurns", 0)]
        [TestCase("activeCooldownTurns", -1)]
        public void InvalidIntegerIsRejected(string fieldName, int value)
        {
            WithInvalidField(fieldName, value);
        }

        [Test]
        public void InvalidEnhancementOrderingAndStackPolicyAreRejected()
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                LilithTestConfig.SetField(
                    config,
                    "awakening3Match3Coefficient",
                    0.69d);
                Assert.That(config.TryValidate(out _), Is.False);

                LilithTestConfig.SetField(
                    config,
                    "awakening3Match3Coefficient",
                    0.75d);
                LilithTestConfig.SetField(
                    config,
                    "corruptedBlessingStackPolicy",
                    EffectStackPolicy.ReplaceWithNewest);
                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static void WithInvalidField(
            string fieldName,
            object value)
        {
            LilithCombatConfig config = LilithTestConfig.Create();
            try
            {
                LilithTestConfig.SetField(config, fieldName, value);
                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

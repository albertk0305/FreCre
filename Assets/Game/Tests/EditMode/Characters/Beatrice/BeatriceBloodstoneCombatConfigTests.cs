using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsSpecifiedValues()
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                Assert.That(config.PassiveMaxDealtDamageIncreaseRate, Is.EqualTo(0.20d));
                Assert.That(config.Awakening3PassiveMaxDealtDamageIncreaseRate, Is.EqualTo(0.24d));
                Assert.That(config.Match3HpCostRate, Is.EqualTo(0.03d));
                Assert.That(config.Match4HpCostRate, Is.EqualTo(0.06d));
                Assert.That(config.LowHpThresholdRate, Is.EqualTo(0.50d));
                Assert.That(config.Match3BaseCoefficient, Is.EqualTo(0.90d));
                Assert.That(config.Match3LowHpBonusCoefficient, Is.EqualTo(0.20d));
                Assert.That(config.Match4BaseCoefficient, Is.EqualTo(1.50d));
                Assert.That(config.Match4LowHpBonusCoefficient, Is.EqualTo(0.80d));
                Assert.That(config.Match5BaseCoefficient, Is.EqualTo(2.70d));
                Assert.That(config.Match5LostHpBonusCoefficient, Is.EqualTo(2.00d));
                Assert.That(config.ActiveBaseCoefficient, Is.EqualTo(3.50d));
                Assert.That(config.ActiveLostHpBonusCoefficient, Is.EqualTo(2.00d));
                Assert.That(config.Match5BaseLifestealRate, Is.EqualTo(0.10d));
                Assert.That(config.Match5LostHpLifestealRate, Is.EqualTo(0.20d));
                Assert.That(config.ActiveBaseLifestealRate, Is.EqualTo(0.15d));
                Assert.That(config.ActiveLostHpLifestealRate, Is.EqualTo(0.20d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.Awakening4Match3BaseCoefficient, Is.EqualTo(1.00d));
                Assert.That(config.Awakening4Match3LowHpBonusCoefficient, Is.EqualTo(0.22d));
                Assert.That(config.Awakening4Match4BaseCoefficient, Is.EqualTo(1.65d));
                Assert.That(config.Awakening4Match4LowHpBonusCoefficient, Is.EqualTo(0.88d));
                Assert.That(config.Awakening5Match5BaseCoefficient, Is.EqualTo(3.00d));
                Assert.That(config.Awakening5Match5LostHpBonusCoefficient, Is.EqualTo(2.20d));
                Assert.That(config.Awakening6ActiveBaseCoefficient, Is.EqualTo(4.00d));
                Assert.That(config.Awakening6ActiveLostHpBonusCoefficient, Is.EqualTo(2.40d));
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match3HpCostRate", -0.01d)]
        [TestCase("match4HpCostRate", 1.01d)]
        [TestCase("lowHpThresholdRate", 0d)]
        [TestCase("passiveMaxDealtDamageIncreaseRate", double.NaN)]
        [TestCase("awakening3PassiveMaxDealtDamageIncreaseRate", double.PositiveInfinity)]
        [TestCase("match5BaseLifestealRate", 1.01d)]
        [TestCase("activeLostHpLifestealRate", -0.01d)]
        public void InvalidRateIsRejected(string fieldName, double value)
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                BeatriceBloodstoneTestConfig.SetDouble(
                    config,
                    fieldName,
                    value);

                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void InclusiveRateBoundariesAreValid()
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                BeatriceBloodstoneTestConfig.SetDouble(
                    config,
                    "match3HpCostRate",
                    0d);
                BeatriceBloodstoneTestConfig.SetDouble(
                    config,
                    "lowHpThresholdRate",
                    1d);
                BeatriceBloodstoneTestConfig.SetDouble(
                    config,
                    "passiveMaxDealtDamageIncreaseRate",
                    1d);

                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match3BaseCoefficient", -0.01d)]
        [TestCase("match4LowHpBonusCoefficient", double.NaN)]
        [TestCase("match5LostHpBonusCoefficient", double.NegativeInfinity)]
        [TestCase("awakening6ActiveLostHpBonusCoefficient", double.PositiveInfinity)]
        public void InvalidCoefficientIsRejected(string fieldName, double value)
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                BeatriceBloodstoneTestConfig.SetDouble(
                    config,
                    fieldName,
                    value);

                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match5BaseLifestealRate", "match5LostHpLifestealRate")]
        [TestCase("activeBaseLifestealRate", "activeLostHpLifestealRate")]
        public void CombinedLifestealAboveOneIsRejected(
            string baseField,
            string lostHpField)
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                BeatriceBloodstoneTestConfig.SetDouble(config, baseField, 0.60d);
                BeatriceBloodstoneTestConfig.SetDouble(config, lostHpField, 0.41d);

                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveCooldownIsRejected(int cooldown)
        {
            BeatriceBloodstoneCombatConfig config =
                BeatriceBloodstoneTestConfig.Create();
            try
            {
                BeatriceBloodstoneTestConfig.SetInt(
                    config,
                    "activeCooldownTurns",
                    cooldown);

                Assert.That(config.TryValidate(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

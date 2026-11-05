using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Sherry;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsDesignedValues()
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                Assert.That(config.StylishMaxRank, Is.EqualTo(7));
                Assert.That(config.StylishDamageExponentBase,
                    Is.EqualTo(1.6d));
                Assert.That(config.BaseStylishMaxDamageIncreaseRate,
                    Is.EqualTo(0.20d));
                Assert.That(config.Awakening5StylishMaxDamageIncreaseRate,
                    Is.EqualTo(0.25d));
                Assert.That(config.JackpotExponentBase,
                    Is.EqualTo(1.65d));
                Assert.That(config.BaseJackpotMaxCoefficient,
                    Is.EqualTo(18d));
                Assert.That(config.Awakening6JackpotMaxCoefficient,
                    Is.EqualTo(23d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(1));
                Assert.That(config.Match3Coefficient, Is.EqualTo(0.75d));
                Assert.That(config.Awakening3Match3Coefficient,
                    Is.EqualTo(0.80d));
                Assert.That(config.Match4Coefficient, Is.EqualTo(1.30d));
                Assert.That(config.Awakening3Match4Coefficient,
                    Is.EqualTo(1.40d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(2.10d));
                Assert.That(config.Awakening3Match5Coefficient,
                    Is.EqualTo(2.25d));
                Assert.That(config.Match3OniIntimidationRate,
                    Is.EqualTo(0.03d));
                Assert.That(config.Match4OniIntimidationRate,
                    Is.EqualTo(0.06d));
                Assert.That(config.Match5OniIntimidationRate,
                    Is.EqualTo(0.10d));
                Assert.That(config.Awakening4Match3OniIntimidationRate,
                    Is.EqualTo(0.04d));
                Assert.That(config.Awakening4Match4OniIntimidationRate,
                    Is.EqualTo(0.08d));
                Assert.That(config.Awakening4Match5OniIntimidationRate,
                    Is.EqualTo(0.13d));
                Assert.That(config.OniIntimidationDurationTurns,
                    Is.EqualTo(2));
                Assert.That(config.OniIntimidationMinimumRank,
                    Is.EqualTo(5));
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("stylishDamageExponentBase", 1d,
            "StylishDamageExponentBase")]
        [TestCase("jackpotExponentBase", double.NaN,
            "JackpotExponentBase")]
        [TestCase("baseStylishMaxDamageIncreaseRate", -0.01d,
            "BaseStylishMaxDamageIncreaseRate")]
        [TestCase("awakening5StylishMaxDamageIncreaseRate", 0.19d,
            "Awakening5StylishMaxDamageIncreaseRate")]
        [TestCase("baseJackpotMaxCoefficient", 0d,
            "BaseJackpotMaxCoefficient")]
        [TestCase("awakening6JackpotMaxCoefficient", 17d,
            "Awakening6JackpotMaxCoefficient")]
        [TestCase("match3Coefficient", double.PositiveInfinity,
            "Match3Coefficient")]
        [TestCase("awakening3Match4Coefficient", 1.20d,
            "Awakening3Match4Coefficient")]
        [TestCase("match5OniIntimidationRate", 1.01d,
            "Match5OniIntimidationRate")]
        [TestCase("awakening4Match3OniIntimidationRate", 0.02d,
            "Awakening4Match3OniIntimidationRate")]
        public void InvalidDoubleIsRejected(
            string propertyName,
            double value,
            string expectedField)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryTestConfig.SetDouble(config, propertyName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("stylishMaxRank", 6, "StylishMaxRank")]
        [TestCase("activeCooldownTurns", 0, "ActiveCooldownTurns")]
        [TestCase("oniIntimidationDurationTurns", 0,
            "OniIntimidationDurationTurns")]
        [TestCase("oniIntimidationMinimumRank", 0,
            "OniIntimidationMinimumRank")]
        [TestCase("oniIntimidationMinimumRank", 8,
            "OniIntimidationMinimumRank")]
        public void InvalidIntegerIsRejected(
            string propertyName,
            int value,
            string expectedField)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryTestConfig.SetInt(config, propertyName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

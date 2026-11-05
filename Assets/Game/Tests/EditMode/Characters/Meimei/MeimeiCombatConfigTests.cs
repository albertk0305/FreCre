using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Meimei;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsConfirmedM1Values()
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            try
            {
                Assert.That(config.Match3Coefficient, Is.EqualTo(1.25d));
                Assert.That(config.Match4Coefficient, Is.EqualTo(2.20d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(3.60d));
                Assert.That(config.DrunkenImmortalDanceCoefficient,
                    Is.EqualTo(10.00d));
                Assert.That(config.TipsinessMaxAmount, Is.EqualTo(5));
                Assert.That(config.Match4TipsinessGainAmount,
                    Is.EqualTo(1));
                Assert.That(config.Match5TipsinessGainAmount,
                    Is.EqualTo(2));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(1));
                Assert.That(config.Awakening3Match3Coefficient,
                    Is.EqualTo(1.40d));
                Assert.That(config.Awakening3Match4Coefficient,
                    Is.EqualTo(2.50d));
                Assert.That(config.Awakening4Match5Coefficient,
                    Is.EqualTo(4.10d));
                Assert.That(
                    config.Awakening5DrunkenImmortalDanceCoefficient,
                    Is.EqualTo(12.00d));
                Assert.That(config.Awakening6CriticalChancePerTipsiness,
                    Is.EqualTo(0.06d));
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match3Coefficient", 0d)]
        [TestCase("match4Coefficient", -0.01d)]
        [TestCase("match5Coefficient", double.NaN)]
        [TestCase("drunkenImmortalDanceCoefficient",
            double.PositiveInfinity)]
        [TestCase("awakening3Match3Coefficient", 0d)]
        [TestCase("awakening3Match4Coefficient", -1d)]
        [TestCase("awakening4Match5Coefficient", double.NaN)]
        [TestCase("awakening5DrunkenImmortalDanceCoefficient", 0d)]
        [TestCase("awakening6CriticalChancePerTipsiness", 1.01d)]
        public void InvalidDoubleIsRejected(
            string fieldName,
            double value)
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            try
            {
                MeimeiTestConfig.SetDouble(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(ToPropertyName(fieldName)));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("tipsinessMaxAmount", 0, "TipsinessMaxAmount")]
        [TestCase("tipsinessMaxAmount", 6, "TipsinessMaxAmount")]
        [TestCase("match4TipsinessGainAmount", 0,
            "Match4TipsinessGainAmount")]
        [TestCase("match5TipsinessGainAmount", 6,
            "Match5TipsinessGainAmount")]
        [TestCase("activeCooldownTurns", 0, "ActiveCooldownTurns")]
        public void InvalidIntegerIsRejected(
            string fieldName,
            int value,
            string expectedField)
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            try
            {
                MeimeiTestConfig.SetInt(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static string ToPropertyName(string fieldName)
        {
            return char.ToUpperInvariant(fieldName[0])
                + fieldName.Substring(1);
        }
    }
}

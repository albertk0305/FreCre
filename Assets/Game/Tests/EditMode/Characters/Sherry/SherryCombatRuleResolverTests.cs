using System;
using NUnit.Framework;
using ValorChronicle.Characters.Sherry;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryCombatRuleResolverTests
    {
        [TestCase(0, 0.75d, 1.30d, 2.10d, 0.03d, 0.06d, 0.10d,
            0.20d, 18d)]
        [TestCase(1, 0.75d, 1.30d, 2.10d, 0.03d, 0.06d, 0.10d,
            0.20d, 18d)]
        [TestCase(2, 0.75d, 1.30d, 2.10d, 0.03d, 0.06d, 0.10d,
            0.20d, 18d)]
        [TestCase(3, 0.80d, 1.40d, 2.25d, 0.03d, 0.06d, 0.10d,
            0.20d, 18d)]
        [TestCase(4, 0.80d, 1.40d, 2.25d, 0.04d, 0.08d, 0.13d,
            0.20d, 18d)]
        [TestCase(5, 0.80d, 1.40d, 2.25d, 0.04d, 0.08d, 0.13d,
            0.25d, 18d)]
        [TestCase(6, 0.80d, 1.40d, 2.25d, 0.04d, 0.08d, 0.13d,
            0.25d, 23d)]
        public void ResolveAppliesEveryAwakeningThreshold(
            int awakening,
            double match3,
            double match4,
            double match5,
            double intimidation3,
            double intimidation4,
            double intimidation5,
            double stylishMaximum,
            double jackpotMaximum)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(config, awakening);

                Assert.That(rules.StylishMaxRank, Is.EqualTo(7));
                Assert.That(rules.StylishDamageExponentBase,
                    Is.EqualTo(1.6d));
                Assert.That(rules.JackpotExponentBase,
                    Is.EqualTo(1.65d));
                Assert.That(rules.Match3Coefficient, Is.EqualTo(match3));
                Assert.That(rules.Match4Coefficient, Is.EqualTo(match4));
                Assert.That(rules.Match5Coefficient, Is.EqualTo(match5));
                Assert.That(rules.Match3OniIntimidationRate,
                    Is.EqualTo(intimidation3));
                Assert.That(rules.Match4OniIntimidationRate,
                    Is.EqualTo(intimidation4));
                Assert.That(rules.Match5OniIntimidationRate,
                    Is.EqualTo(intimidation5));
                Assert.That(rules.StylishMaxDamageIncreaseRate,
                    Is.EqualTo(stylishMaximum));
                Assert.That(rules.JackpotMaxCoefficient,
                    Is.EqualTo(jackpotMaximum));
                Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(1));
                Assert.That(rules.OniIntimidationDurationTurns,
                    Is.EqualTo(2));
                Assert.That(rules.OniIntimidationMinimumRank,
                    Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase(0, 0.20d)]
        [TestCase(5, 0.25d)]
        public void StylishFormulaMatchesTemplateForEveryRank(
            int awakening,
            double maximumRate)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(config, awakening);
                for (int rank = 0; rank <= 7; rank++)
                {
                    double expected = maximumRate
                        * (Math.Pow(1.6d, rank) - 1d)
                        / (Math.Pow(1.6d, 7) - 1d);

                    Assert.That(
                        rules.ResolveStylishDamageIncreaseRate(rank),
                        Is.EqualTo(expected).Within(0.000000000001d),
                        $"Rank {rank}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase(0, 18d)]
        [TestCase(6, 23d)]
        public void JackpotFormulaMatchesTemplateForEveryRank(
            int awakening,
            double maximumCoefficient)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(config, awakening);
                for (int rank = 0; rank <= 7; rank++)
                {
                    double expected = maximumCoefficient
                        * (Math.Pow(1.65d, rank) - 1d)
                        / (Math.Pow(1.65d, 7) - 1d);

                    Assert.That(
                        rules.ResolveJackpotCoefficient(rank),
                        Is.EqualTo(expected).Within(0.000000000001d),
                        $"Rank {rank}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(8)]
        public void FormulaRejectsRankOutsideSupportedRange(int rank)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(config, 0);

                Assert.That(
                    () => rules.ResolveStylishDamageIncreaseRate(rank),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(
                    () => rules.ResolveJackpotCoefficient(rank),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolverRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            SherryCombatConfig config = SherryTestConfig.Create();
            try
            {
                Assert.That(
                    () => SherryCombatRuleResolver.Resolve(
                        config,
                        awakening),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("awakening"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

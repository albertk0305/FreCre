using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneCombatRuleResolverTests
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

        [TestCase(0, 0.20d, 0.90d, 0.20d, 1.50d, 0.80d, 2.70d, 2.00d, 3.50d, 2.00d)]
        [TestCase(2, 0.20d, 0.90d, 0.20d, 1.50d, 0.80d, 2.70d, 2.00d, 3.50d, 2.00d)]
        [TestCase(3, 0.24d, 0.90d, 0.20d, 1.50d, 0.80d, 2.70d, 2.00d, 3.50d, 2.00d)]
        [TestCase(4, 0.24d, 1.00d, 0.22d, 1.65d, 0.88d, 2.70d, 2.00d, 3.50d, 2.00d)]
        [TestCase(5, 0.24d, 1.00d, 0.22d, 1.65d, 0.88d, 3.00d, 2.20d, 3.50d, 2.00d)]
        [TestCase(6, 0.24d, 1.00d, 0.22d, 1.65d, 0.88d, 3.00d, 2.20d, 4.00d, 2.40d)]
        public void ResolveAppliesEachAwakeningOnce(
            int awakening,
            double passiveMaximum,
            double match3Base,
            double match3Bonus,
            double match4Base,
            double match4Bonus,
            double match5Base,
            double match5Bonus,
            double activeBase,
            double activeBonus)
        {
            BeatriceBloodstoneResolvedCombatRules rules = Resolve(awakening);

            Assert.That(rules.PassiveMaxDealtDamageIncreaseRate, Is.EqualTo(passiveMaximum));
            Assert.That(rules.Match3HpCostRate, Is.EqualTo(0.03d));
            Assert.That(rules.Match4HpCostRate, Is.EqualTo(0.06d));
            Assert.That(rules.LowHpThresholdRate, Is.EqualTo(0.50d));
            Assert.That(rules.Match3BaseCoefficient, Is.EqualTo(match3Base));
            Assert.That(rules.Match3LowHpBonusCoefficient, Is.EqualTo(match3Bonus));
            Assert.That(rules.Match4BaseCoefficient, Is.EqualTo(match4Base));
            Assert.That(rules.Match4LowHpBonusCoefficient, Is.EqualTo(match4Bonus));
            Assert.That(rules.Match5BaseCoefficient, Is.EqualTo(match5Base));
            Assert.That(rules.Match5LostHpBonusCoefficient, Is.EqualTo(match5Bonus));
            Assert.That(rules.ActiveBaseCoefficient, Is.EqualTo(activeBase));
            Assert.That(rules.ActiveLostHpBonusCoefficient, Is.EqualTo(activeBonus));
            Assert.That(rules.Match5BaseLifestealRate, Is.EqualTo(0.10d));
            Assert.That(rules.Match5LostHpLifestealRate, Is.EqualTo(0.20d));
            Assert.That(rules.ActiveBaseLifestealRate, Is.EqualTo(0.15d));
            Assert.That(rules.ActiveLostHpLifestealRate, Is.EqualTo(0.20d));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
        }

        [TestCase(0, 100L, 100L, 0d)]
        [TestCase(0, 75L, 100L, 0.05d)]
        [TestCase(0, 50L, 100L, 0.10d)]
        [TestCase(0, 25L, 100L, 0.15d)]
        [TestCase(0, 1L, 100L, 0.198d)]
        [TestCase(3, 100L, 100L, 0d)]
        [TestCase(3, 50L, 100L, 0.12d)]
        [TestCase(3, 1L, 100L, 0.2376d)]
        public void PassiveUsesSharedPartyLostHpRate(
            int awakening,
            long currentHp,
            long maxHp,
            double expected)
        {
            Assert.That(
                Resolve(awakening).ResolvePassiveDealtDamageIncreaseRate(
                    currentHp,
                    maxHp),
                Is.EqualTo(expected).Within(0.0000000001d));
        }

        [TestCase(50L, 0.90d, 1.50d)]
        [TestCase(49L, 1.10d, 2.30d)]
        [TestCase(1L, 1.10d, 2.30d)]
        public void Match3AndMatch4UseStrictAfterCostThreshold(
            long hpAfterCost,
            double expectedMatch3,
            double expectedMatch4)
        {
            BeatriceBloodstoneResolvedCombatRules rules = Resolve(0);

            Assert.That(
                rules.ResolveMatch3Coefficient(hpAfterCost, 100L),
                Is.EqualTo(expectedMatch3).Within(0.0000000001d));
            Assert.That(
                rules.ResolveMatch4Coefficient(hpAfterCost, 100L),
                Is.EqualTo(expectedMatch4).Within(0.0000000001d));
        }

        [Test]
        public void MatchCostCanCrossLowHpThresholdBeforeDamageResolution()
        {
            BeatriceBloodstoneResolvedCombatRules rules = Resolve(0);

            Assert.That(rules.IsLowPartyHp(50L, 100L), Is.False);
            Assert.That(rules.ResolveMatch3Coefficient(47L, 100L), Is.EqualTo(1.10d));
            Assert.That(rules.ResolveMatch4Coefficient(44L, 100L), Is.EqualTo(2.30d));
        }

        [TestCase(100L, 100L, 0d, 2.70d, 0.10d, 3.50d, 0.15d)]
        [TestCase(75L, 100L, 0.25d, 3.20d, 0.15d, 4.00d, 0.20d)]
        [TestCase(50L, 100L, 0.50d, 3.70d, 0.20d, 4.50d, 0.25d)]
        [TestCase(25L, 100L, 0.75d, 4.20d, 0.25d, 5.00d, 0.30d)]
        [TestCase(1L, 100L, 0.99d, 4.68d, 0.298d, 5.48d, 0.348d)]
        public void Match5AndActiveScaleContinuouslyWithSharedPartyHp(
            long currentHp,
            long maxHp,
            double expectedLostRate,
            double expectedMatch5Coefficient,
            double expectedMatch5Lifesteal,
            double expectedActiveCoefficient,
            double expectedActiveLifesteal)
        {
            BeatriceBloodstoneResolvedCombatRules rules = Resolve(0);
            BeatriceBloodstoneScalingResult match5 =
                rules.ResolveMatch5(currentHp, maxHp);
            BeatriceBloodstoneScalingResult active =
                rules.ResolveActive(currentHp, maxHp);

            Assert.That(match5.LostPartyHpRate, Is.EqualTo(expectedLostRate).Within(1e-12d));
            Assert.That(match5.SkillCoefficient, Is.EqualTo(expectedMatch5Coefficient).Within(1e-12d));
            Assert.That(match5.LifestealRate, Is.EqualTo(expectedMatch5Lifesteal).Within(1e-12d));
            Assert.That(active.LostPartyHpRate, Is.EqualTo(expectedLostRate).Within(1e-12d));
            Assert.That(active.SkillCoefficient, Is.EqualTo(expectedActiveCoefficient).Within(1e-12d));
            Assert.That(active.LifestealRate, Is.EqualTo(expectedActiveLifesteal).Within(1e-12d));
        }

        [Test]
        public void DamageAwakeningsDoNotEnhanceLifestealOrCooldown()
        {
            BeatriceBloodstoneScalingResult match5 =
                Resolve(5).ResolveMatch5(50L, 100L);
            BeatriceBloodstoneResolvedCombatRules awakening6 = Resolve(6);
            BeatriceBloodstoneScalingResult active =
                awakening6.ResolveActive(50L, 100L);

            Assert.That(match5.SkillCoefficient, Is.EqualTo(4.10d).Within(1e-12d));
            Assert.That(match5.LifestealRate, Is.EqualTo(0.20d).Within(1e-12d));
            Assert.That(active.SkillCoefficient, Is.EqualTo(5.20d).Within(1e-12d));
            Assert.That(active.LifestealRate, Is.EqualTo(0.25d).Within(1e-12d));
            Assert.That(awakening6.ActiveCooldownTurns, Is.EqualTo(8));
        }

        [TestCase(-10L, 1d)]
        [TestCase(0L, 1d)]
        [TestCase(100L, 0d)]
        [TestCase(150L, 0d)]
        public void LostHpRateNormalizesCurrentHp(
            long currentHp,
            double expectedLostRate)
        {
            Assert.That(
                Resolve(0).ResolveLostPartyHpRate(currentHp, 100L),
                Is.EqualTo(expectedLostRate));
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void HpCalculationRejectsNonPositiveMaximum(long maxHp)
        {
            Assert.That(
                () => Resolve(0).ResolveMatch5(1L, maxHp),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("maxPartyHp"));
        }

        [Test]
        public void RepeatedResolutionIsDeterministicAndDoesNotMutateRules()
        {
            BeatriceBloodstoneResolvedCombatRules rules = Resolve(6);

            BeatriceBloodstoneScalingResult first = rules.ResolveActive(37L, 100L);
            BeatriceBloodstoneScalingResult second = rules.ResolveActive(37L, 100L);

            Assert.That(second.LostPartyHpRate, Is.EqualTo(first.LostPartyHpRate));
            Assert.That(second.SkillCoefficient, Is.EqualTo(first.SkillCoefficient));
            Assert.That(second.LifestealRate, Is.EqualTo(first.LifestealRate));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideSupportedRange(int awakening)
        {
            Assert.That(
                () => Resolve(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        [Test]
        public void ResolveRejectsInvalidOrMissingConfig()
        {
            BeatriceBloodstoneTestConfig.SetDouble(
                config,
                "match3HpCostRate",
                -1d);

            Assert.That(
                () => Resolve(0),
                Throws.ArgumentException
                    .With.Property("ParamName").EqualTo("config"));
            Assert.That(
                () => BeatriceBloodstoneCombatRuleResolver.Resolve(null, 0),
                Throws.ArgumentNullException
                    .With.Property("ParamName").EqualTo("config"));
        }

        private BeatriceBloodstoneResolvedCombatRules Resolve(int awakening)
        {
            return BeatriceBloodstoneCombatRuleResolver.Resolve(
                config,
                awakening);
        }
    }
}

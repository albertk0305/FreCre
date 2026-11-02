using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Luciel;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielCombatRuleResolverTests
    {
        [TestCase(0, 0d, 0.06d, 0.11d, 0.18d, 0.12d, 0.05d, 0.45d)]
        [TestCase(1, 0d, 0.06d, 0.11d, 0.18d, 0.12d, 0.05d, 0.45d)]
        [TestCase(2, 0.05d, 0.06d, 0.11d, 0.18d, 0.12d, 0.05d, 0.45d)]
        [TestCase(3, 0.05d, 0.07d, 0.12d, 0.18d, 0.12d, 0.05d, 0.45d)]
        [TestCase(4, 0.05d, 0.07d, 0.12d, 0.20d, 0.14d, 0.05d, 0.45d)]
        [TestCase(5, 0.05d, 0.07d, 0.12d, 0.20d, 0.14d, 0.06d, 0.45d)]
        [TestCase(6, 0.05d, 0.07d, 0.12d, 0.20d, 0.14d, 0.06d, 0.52d)]
        public void ResolveAppliesReachedAwakeningRulesExactlyOnce(
            int awakening,
            double expectedLocalShieldIncrease,
            double expectedMatch3ShieldRate,
            double expectedMatch4ShieldRate,
            double expectedMatch5ShieldRate,
            double expectedMatch5DamageIncrease,
            double expectedPassiveReduction,
            double expectedActiveShieldRate)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielResolvedCombatRules rules =
                    LucielCombatRuleResolver.Resolve(config, awakening);

                Assert.That(rules.ActionLocalShieldAmountIncreaseRate,
                    Is.EqualTo(expectedLocalShieldIncrease));
                Assert.That(rules.Match3ShieldMaxHpRate,
                    Is.EqualTo(expectedMatch3ShieldRate));
                Assert.That(rules.Match4ShieldMaxHpRate,
                    Is.EqualTo(expectedMatch4ShieldRate));
                Assert.That(rules.Match5ShieldMaxHpRate,
                    Is.EqualTo(expectedMatch5ShieldRate));
                Assert.That(rules.Match5PartyDealtDamageIncreaseRate,
                    Is.EqualTo(expectedMatch5DamageIncrease));
                Assert.That(rules.PassiveDamageReductionRate,
                    Is.EqualTo(expectedPassiveReduction));
                Assert.That(rules.ActiveShieldMaxHpRate,
                    Is.EqualTo(expectedActiveShieldRate));
                AssertCommonRules(rules);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveDoesNotMutateConfigOrCompoundAwakenings()
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielCombatRuleResolver.Resolve(config, 6);
                LucielResolvedCombatRules baseRules =
                    LucielCombatRuleResolver.Resolve(config, 0);
                LucielResolvedCombatRules finalRules =
                    LucielCombatRuleResolver.Resolve(config, 6);

                Assert.That(config.Match3ShieldMaxHpRate,
                    Is.EqualTo(0.06d));
                Assert.That(config.ActiveShieldMaxHpRate,
                    Is.EqualTo(0.45d));
                Assert.That(baseRules.ActionLocalShieldAmountIncreaseRate,
                    Is.Zero);
                Assert.That(baseRules.Match3ShieldMaxHpRate,
                    Is.EqualTo(0.06d));
                Assert.That(finalRules.ActionLocalShieldAmountIncreaseRate,
                    Is.EqualTo(0.05d));
                Assert.That(finalRules.Match3ShieldMaxHpRate,
                    Is.EqualTo(0.07d));
                Assert.That(finalRules.ActiveShieldMaxHpRate,
                    Is.EqualTo(0.52d));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideZeroToSix(int awakening)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                Assert.That(
                    () => LucielCombatRuleResolver.Resolve(
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
        public void ResolveRejectsNullAndInvalidConfig()
        {
            Assert.That(
                () => LucielCombatRuleResolver.Resolve(null, 0),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("config"));

            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetInt(
                    config,
                    "activeCooldownTurns",
                    7);

                Assert.That(
                    () => LucielCombatRuleResolver.Resolve(config, 0),
                    Throws.TypeOf<ArgumentException>()
                        .With.Property("ParamName").EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static void AssertCommonRules(
            LucielResolvedCombatRules rules)
        {
            Assert.That(rules.Match5BuffDurationTurns, Is.EqualTo(3));
            Assert.That(rules.Match5BuffStackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(rules.ActiveCurrentPartyHpCostRate,
                Is.EqualTo(0.05d));
            Assert.That(rules.ActiveHpCostRoundingMode,
                Is.EqualTo(PartyHpCostRoundingMode.Ceiling));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(rules.MatchShieldUsesComboMultiplier, Is.True);
            Assert.That(rules.ActiveShieldUsesComboMultiplier, Is.False);
            Assert.That(rules.ShieldDurationTurns, Is.Null);
            Assert.That(rules.ActiveAvailableAtBattleStart, Is.True);
            Assert.That(rules.ActiveHpCostUsesCurrentPartyHp, Is.True);
            Assert.That(rules.ActiveMinimumPartyHpAfterCost,
                Is.EqualTo(1L));
            Assert.That(rules.ActiveUsableWhenActualHpCostIsZero, Is.True);
            Assert.That(rules.ActiveHpCostIgnoresShields, Is.True);
            Assert.That(rules.ActiveHpCostIgnoresDamageReduction, Is.True);
        }
    }
}

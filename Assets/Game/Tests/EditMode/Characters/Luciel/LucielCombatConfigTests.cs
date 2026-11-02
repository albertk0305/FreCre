using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Luciel;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsConfirmedValues()
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                Assert.That(config.PassiveDamageReductionRate,
                    Is.EqualTo(0.05d));
                Assert.That(config.Awakening5PassiveDamageReductionRate,
                    Is.EqualTo(0.06d));
                Assert.That(config.Match3ShieldMaxHpRate,
                    Is.EqualTo(0.06d));
                Assert.That(config.Awakening3Match3ShieldMaxHpRate,
                    Is.EqualTo(0.07d));
                Assert.That(config.Match4ShieldMaxHpRate,
                    Is.EqualTo(0.11d));
                Assert.That(config.Awakening3Match4ShieldMaxHpRate,
                    Is.EqualTo(0.12d));
                Assert.That(config.Match5ShieldMaxHpRate,
                    Is.EqualTo(0.18d));
                Assert.That(config.Awakening4Match5ShieldMaxHpRate,
                    Is.EqualTo(0.20d));
                Assert.That(config.Match5PartyDealtDamageIncreaseRate,
                    Is.EqualTo(0.12d));
                Assert.That(
                    config.Awakening4Match5PartyDealtDamageIncreaseRate,
                    Is.EqualTo(0.14d));
                Assert.That(config.Match5BuffDurationTurns, Is.EqualTo(3));
                Assert.That(config.Match5BuffStackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(config.ActiveCurrentPartyHpCostRate,
                    Is.EqualTo(0.05d));
                Assert.That(config.ActiveHpCostRoundingMode,
                    Is.EqualTo(PartyHpCostRoundingMode.Ceiling));
                Assert.That(config.ActiveShieldMaxHpRate,
                    Is.EqualTo(0.45d));
                Assert.That(config.Awakening6ActiveShieldMaxHpRate,
                    Is.EqualTo(0.52d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.Awakening2ShieldAmountIncreaseRate,
                    Is.EqualTo(0.05d));
                Assert.That(config.MatchShieldUsesComboMultiplier, Is.True);
                Assert.That(config.ActiveShieldUsesComboMultiplier,
                    Is.False);
                Assert.That(config.ShieldDurationTurns, Is.Null);
                Assert.That(config.ActiveAvailableAtBattleStart, Is.True);
                Assert.That(config.ActiveHpCostUsesCurrentPartyHp, Is.True);
                Assert.That(config.ActiveMinimumPartyHpAfterCost,
                    Is.EqualTo(1L));
                Assert.That(config.ActiveUsableWhenActualHpCostIsZero,
                    Is.True);
                Assert.That(config.ActiveHpCostIgnoresShields, Is.True);
                Assert.That(config.ActiveHpCostIgnoresDamageReduction,
                    Is.True);
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("passiveDamageReductionRate", -0.01d,
            "PassiveDamageReductionRate")]
        [TestCase("awakening5PassiveDamageReductionRate", double.NaN,
            "Awakening5PassiveDamageReductionRate")]
        [TestCase("match3ShieldMaxHpRate", double.PositiveInfinity,
            "Match3ShieldMaxHpRate")]
        [TestCase("awakening3Match4ShieldMaxHpRate", 1.01d,
            "Awakening3Match4ShieldMaxHpRate")]
        [TestCase("match5PartyDealtDamageIncreaseRate", -0.01d,
            "Match5PartyDealtDamageIncreaseRate")]
        [TestCase("activeCurrentPartyHpCostRate", double.NaN,
            "ActiveCurrentPartyHpCostRate")]
        [TestCase("activeShieldMaxHpRate", 1.01d,
            "ActiveShieldMaxHpRate")]
        [TestCase("awakening2ShieldAmountIncreaseRate", -0.01d,
            "Awakening2ShieldAmountIncreaseRate")]
        public void InvalidRateIsRejected(
            string fieldName,
            double value,
            string expectedProperty)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetDouble(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedProperty));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("awakening5PassiveDamageReductionRate", 0.04d)]
        [TestCase("awakening3Match3ShieldMaxHpRate", 0.05d)]
        [TestCase("awakening3Match4ShieldMaxHpRate", 0.10d)]
        [TestCase("awakening4Match5ShieldMaxHpRate", 0.17d)]
        [TestCase("awakening4Match5PartyDealtDamageIncreaseRate", 0.11d)]
        [TestCase("awakening6ActiveShieldMaxHpRate", 0.44d)]
        public void EnhancementBelowBaseIsRejected(
            string fieldName,
            double value)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetDouble(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain("awakening values"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match5BuffDurationTurns", 2,
            "Match5BuffDurationTurns")]
        [TestCase("match5BuffDurationTurns", 4,
            "Match5BuffDurationTurns")]
        [TestCase("activeCooldownTurns", 7, "ActiveCooldownTurns")]
        [TestCase("activeCooldownTurns", 9, "ActiveCooldownTurns")]
        public void WrongIntegerContractIsRejected(
            string fieldName,
            int value,
            string expectedProperty)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetInt(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedProperty));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("matchShieldUsesComboMultiplier", false,
            "MatchShieldUsesComboMultiplier")]
        [TestCase("activeShieldUsesComboMultiplier", true,
            "ActiveShieldUsesComboMultiplier")]
        public void WrongComboContractIsRejected(
            string fieldName,
            bool value,
            string expectedProperty)
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetBool(config, fieldName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedProperty));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void WrongEffectAndRoundingPoliciesAreRejected()
        {
            LucielCombatConfig config = LucielTestConfig.Create();
            try
            {
                LucielTestConfig.SetEnum(
                    config,
                    "match5BuffStackPolicy",
                    (int)EffectStackPolicy.Unique);
                Assert.That(config.TryValidate(out string effectError),
                    Is.False);
                Assert.That(effectError,
                    Does.Contain("Match5BuffStackPolicy"));

                Object.DestroyImmediate(config);
                config = LucielTestConfig.Create();
                LucielTestConfig.SetEnum(
                    config,
                    "activeHpCostRoundingMode",
                    (int)PartyHpCostRoundingMode.Floor);
                Assert.That(config.TryValidate(out string roundingError),
                    Is.False);
                Assert.That(roundingError,
                    Does.Contain("ActiveHpCostRoundingMode"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

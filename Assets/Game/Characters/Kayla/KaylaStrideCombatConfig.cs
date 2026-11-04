using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Kayla
{
    [CreateAssetMenu(
        fileName = "KaylaStrideCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Kayla Stride")]
    public sealed class KaylaStrideCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient = 0.80d;

        [SerializeField]
        private double match4Coefficient = 1.40d;

        [SerializeField]
        private double match5Coefficient = 2.20d;

        [SerializeField]
        private double match5CriticalDamageIncreaseRate = 0.20d;

        [SerializeField]
        private int match5BuffDurationTurns = 2;

        [SerializeField]
        private double activeCriticalChanceIncreaseRate = 0.15d;

        [SerializeField]
        private int activeDurationTurns = 2;

        [SerializeField]
        private int activeCooldownTurns = 4;

        [SerializeField]
        private double chargeCoefficientPerStack = 1.20d;

        [SerializeField]
        private int chargeEnhancementThreshold = 5;

        [SerializeField]
        private double chargeEnhancementMultiplier = 1.50d;

        [SerializeField]
        private int chargeMaxStacks = 10;

        [SerializeField]
        private double awakening3Match3Coefficient = 0.85d;

        [SerializeField]
        private double awakening3Match4Coefficient = 1.50d;

        [SerializeField]
        private double awakening4Match5Coefficient = 2.40d;

        [SerializeField]
        private double awakening4Match5CriticalDamageIncreaseRate = 0.25d;

        [SerializeField]
        private int awakening5ActiveCooldownTurns = 3;

        [SerializeField]
        private double awakening6ChargeCoefficientPerStack = 1.35d;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public double Match5CriticalDamageIncreaseRate =>
            match5CriticalDamageIncreaseRate;
        public int Match5BuffDurationTurns => match5BuffDurationTurns;
        public double ActiveCriticalChanceIncreaseRate =>
            activeCriticalChanceIncreaseRate;
        public int ActiveDurationTurns => activeDurationTurns;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double ChargeCoefficientPerStack =>
            chargeCoefficientPerStack;
        public int ChargeEnhancementThreshold =>
            chargeEnhancementThreshold;
        public double ChargeEnhancementMultiplier =>
            chargeEnhancementMultiplier;
        public int ChargeMaxStacks => chargeMaxStacks;
        public double Awakening3Match3Coefficient =>
            awakening3Match3Coefficient;
        public double Awakening3Match4Coefficient =>
            awakening3Match4Coefficient;
        public double Awakening4Match5Coefficient =>
            awakening4Match5Coefficient;
        public double Awakening4Match5CriticalDamageIncreaseRate =>
            awakening4Match5CriticalDamageIncreaseRate;
        public int Awakening5ActiveCooldownTurns =>
            awakening5ActiveCooldownTurns;
        public double Awakening6ChargeCoefficientPerStack =>
            awakening6ChargeCoefficientPerStack;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFinitePositive(match3Coefficient)
                || !IsFinitePositive(match4Coefficient)
                || !IsFinitePositive(match5Coefficient)
                || !IsFinitePositive(awakening3Match3Coefficient)
                || !IsFinitePositive(awakening3Match4Coefficient)
                || !IsFinitePositive(awakening4Match5Coefficient))
            {
                errorMessage =
                    "All match coefficients must be finite and positive.";
                return false;
            }

            if (awakening3Match3Coefficient < match3Coefficient
                || awakening3Match4Coefficient < match4Coefficient
                || awakening4Match5Coefficient < match5Coefficient)
            {
                errorMessage = "Awakening match coefficients cannot be "
                    + "lower than their base values.";
                return false;
            }

            if (!IsRate(match5CriticalDamageIncreaseRate)
                || !IsRate(
                    awakening4Match5CriticalDamageIncreaseRate))
            {
                errorMessage = "Match 5 Critical Damage increases must be "
                    + "finite rates between zero and one.";
                return false;
            }

            if (awakening4Match5CriticalDamageIncreaseRate
                < match5CriticalDamageIncreaseRate)
            {
                errorMessage = "The Awakening 4 Critical Damage increase "
                    + "cannot be lower than the base value.";
                return false;
            }

            if (!IsRate(activeCriticalChanceIncreaseRate))
            {
                errorMessage = "Active Critical Chance increase must be a "
                    + "finite rate between zero and one.";
                return false;
            }

            if (match5BuffDurationTurns <= 0
                || activeDurationTurns <= 0)
            {
                errorMessage =
                    "Effect durations must be greater than zero.";
                return false;
            }

            if (activeCooldownTurns < 0
                || awakening5ActiveCooldownTurns < 0
                || awakening5ActiveCooldownTurns > activeCooldownTurns)
            {
                errorMessage = "Active cooldown values must be non-negative "
                    + "and the Awakening 5 value cannot exceed the base.";
                return false;
            }

            if (!IsFinitePositive(chargeCoefficientPerStack)
                || !IsFinitePositive(
                    awakening6ChargeCoefficientPerStack)
                || !IsFinitePositive(chargeEnhancementMultiplier)
                || chargeEnhancementMultiplier < 1d
                || awakening6ChargeCoefficientPerStack
                    < chargeCoefficientPerStack)
            {
                errorMessage = "Charge coefficients and multiplier must be "
                    + "finite and positive.";
                return false;
            }

            if (chargeMaxStacks <= 0
                || chargeEnhancementThreshold <= 0
                || chargeEnhancementThreshold > chargeMaxStacks)
            {
                errorMessage = "Charge stack limits and enhancement "
                    + "threshold are invalid.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsRate(double value)
        {
            return IsFinite(value) && value >= 0d && value <= 1d;
        }

        private static bool IsFinitePositive(double value)
        {
            return IsFinite(value) && value > 0d;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lancelot
{
    [CreateAssetMenu(
        fileName = "LancelotCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Lancelot")]
    public sealed class LancelotCombatConfig : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient;

        [SerializeField]
        private double match4Coefficient;

        [SerializeField]
        private double match5Coefficient;

        [SerializeField]
        private double passiveCriticalChanceIncreaseRate;

        [SerializeField]
        private double match4CriticalChanceIncreaseRate;

        [SerializeField]
        private double match5CriticalChanceIncreaseRate;

        [SerializeField]
        private double activeCriticalDamageIncreaseRate;

        [SerializeField]
        private int activeDurationTurns;

        [SerializeField]
        private int activeCooldownTurns;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public double PassiveCriticalChanceIncreaseRate =>
            passiveCriticalChanceIncreaseRate;
        public double Match4CriticalChanceIncreaseRate =>
            match4CriticalChanceIncreaseRate;
        public double Match5CriticalChanceIncreaseRate =>
            match5CriticalChanceIncreaseRate;
        public double ActiveCriticalDamageIncreaseRate =>
            activeCriticalDamageIncreaseRate;
        public int ActiveDurationTurns => activeDurationTurns;
        public int ActiveCooldownTurns => activeCooldownTurns;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFinitePositive(match3Coefficient))
            {
                errorMessage =
                    "Match3Coefficient must be finite and greater than zero.";
                return false;
            }

            if (!IsFinitePositive(match4Coefficient))
            {
                errorMessage =
                    "Match4Coefficient must be finite and greater than zero.";
                return false;
            }

            if (!IsFinitePositive(match5Coefficient))
            {
                errorMessage =
                    "Match5Coefficient must be finite and greater than zero.";
                return false;
            }

            if (!IsProbabilityIncrease(passiveCriticalChanceIncreaseRate))
            {
                errorMessage = "PassiveCriticalChanceIncreaseRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (!IsProbabilityIncrease(match4CriticalChanceIncreaseRate))
            {
                errorMessage = "Match4CriticalChanceIncreaseRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (!IsProbabilityIncrease(match5CriticalChanceIncreaseRate))
            {
                errorMessage = "Match5CriticalChanceIncreaseRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (!IsFiniteNonNegative(activeCriticalDamageIncreaseRate))
            {
                errorMessage = "ActiveCriticalDamageIncreaseRate must be "
                    + "finite and non-negative.";
                return false;
            }

            if (activeDurationTurns <= 0)
            {
                errorMessage =
                    "ActiveDurationTurns must be greater than zero.";
                return false;
            }

            if (activeCooldownTurns <= 0)
            {
                errorMessage =
                    "ActiveCooldownTurns must be greater than zero.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsProbabilityIncrease(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }

        private static bool IsFinitePositive(double value)
        {
            return IsFiniteNonNegative(value) && value > 0d;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}

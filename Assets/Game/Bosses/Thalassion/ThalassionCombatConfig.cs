using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Thalassion
{
    [CreateAssetMenu(
        fileName = "ThalassionCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Thalassion")]
    public sealed class ThalassionCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private int initialShieldStacks;
        [SerializeField] private int maximumShieldStacks;
        [SerializeField] private double shieldDamageReductionRate;
        [SerializeField] private double tidalStrikeCoefficient;
        [SerializeField] private double ragingFistCoefficient;
        [SerializeField] private double pressureBurstBaseCoefficient;
        [SerializeField] private double pressureBurstPerStackCoefficient;
        [SerializeField] private double fadingWaveCoefficient;

        public int InitialShieldStacks => initialShieldStacks;
        public int MaximumShieldStacks => maximumShieldStacks;
        public double ShieldDamageReductionRate =>
            shieldDamageReductionRate;
        public double TidalStrikeCoefficient => tidalStrikeCoefficient;
        public double RagingFistCoefficient => ragingFistCoefficient;
        public double PressureBurstBaseCoefficient =>
            pressureBurstBaseCoefficient;
        public double PressureBurstPerStackCoefficient =>
            pressureBurstPerStackCoefficient;
        public double FadingWaveCoefficient => fadingWaveCoefficient;

        public override bool TryValidate(out string errorMessage)
        {
            if (initialShieldStacks <= 0)
            {
                errorMessage = "InitialShieldStacks must be positive.";
                return false;
            }

            if (maximumShieldStacks <= 0
                || initialShieldStacks > maximumShieldStacks)
            {
                errorMessage = "MaximumShieldStacks must be positive and "
                    + "at least InitialShieldStacks.";
                return false;
            }

            if (!IsFiniteRate(shieldDamageReductionRate))
            {
                errorMessage = "ShieldDamageReductionRate must be finite "
                    + "and between zero and one.";
                return false;
            }

            if (!IsFiniteNonNegative(tidalStrikeCoefficient)
                || !IsFiniteNonNegative(ragingFistCoefficient)
                || !IsFiniteNonNegative(pressureBurstBaseCoefficient)
                || !IsFiniteNonNegative(
                    pressureBurstPerStackCoefficient)
                || !IsFiniteNonNegative(fadingWaveCoefficient))
            {
                errorMessage = "All damage coefficients must be finite and "
                    + "non-negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}

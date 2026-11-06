using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Orver
{
    [CreateAssetMenu(
        fileName = "OrverCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Orver")]
    public sealed class OrverCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private double ancientTimberStrikeCoefficient = 0.75d;
        [SerializeField] private double ancientWrathCoefficient = 1.80d;
        [SerializeField] private double twistedRootLashCoefficient = 1.05d;
        [SerializeField] private double conditionalBaseDamageCoefficient = 0.55d;
        [SerializeField] private double damageCoefficientPerRoot = 0.90d;
        [SerializeField] private double healingCoefficientPerRoot = 0.01d;
        [SerializeField] private int maximumRootCount = 2;

        public double AncientTimberStrikeCoefficient =>
            ancientTimberStrikeCoefficient;
        public double AncientWrathCoefficient => ancientWrathCoefficient;
        public double TwistedRootLashCoefficient =>
            twistedRootLashCoefficient;
        public double ConditionalBaseDamageCoefficient =>
            conditionalBaseDamageCoefficient;
        public double DamageCoefficientPerRoot => damageCoefficientPerRoot;
        public double HealingCoefficientPerRoot => healingCoefficientPerRoot;
        public int MaximumRootCount => maximumRootCount;
        public int RootCreationCount => OrverRules.RootCreationCount;
        public int PatternCount => OrverRules.PatternCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(ancientTimberStrikeCoefficient))
            {
                errorMessage = "AncientTimberStrikeCoefficient must be "
                    + "finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(ancientWrathCoefficient))
            {
                errorMessage = "AncientWrathCoefficient must be finite "
                    + "and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(twistedRootLashCoefficient))
            {
                errorMessage = "TwistedRootLashCoefficient must be finite "
                    + "and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(conditionalBaseDamageCoefficient)
                || !IsFiniteNonNegative(damageCoefficientPerRoot)
                || !IsFiniteNonNegative(healingCoefficientPerRoot))
            {
                errorMessage = "Conditional damage and healing "
                    + "coefficients must be finite and non-negative.";
                return false;
            }

            if (maximumRootCount != 2)
            {
                errorMessage = "MaximumRootCount must be exactly 2.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}

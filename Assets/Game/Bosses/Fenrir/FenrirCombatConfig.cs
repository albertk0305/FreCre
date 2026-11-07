using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Fenrir
{
    [CreateAssetMenu(
        fileName = "FenrirCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Fenrir")]
    public sealed class FenrirCombatConfig : CombatConfigDefinition
    {
        [SerializeField]
        private double savageRendCoefficient;

        [SerializeField]
        private double rampagingClawCoefficient;

        [SerializeField]
        private double frenzyAttackIncreaseRate;

        [SerializeField]
        private double frenzyTakenDamageReductionRate;

        [SerializeField]
        private int woundThreshold;

        [SerializeField]
        private int frenzyRequiredCriticalHits;

        public double SavageRendCoefficient => savageRendCoefficient;
        public double RampagingClawCoefficient => rampagingClawCoefficient;
        public double FrenzyAttackIncreaseRate => frenzyAttackIncreaseRate;
        public double FrenzyTakenDamageReductionRate =>
            frenzyTakenDamageReductionRate;
        public int WoundThreshold => woundThreshold;
        public int FrenzyRequiredCriticalHits =>
            frenzyRequiredCriticalHits;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(savageRendCoefficient)
                || !IsFiniteNonNegative(rampagingClawCoefficient)
                || !IsFiniteNonNegative(frenzyAttackIncreaseRate)
                || !IsFiniteRate(frenzyTakenDamageReductionRate))
            {
                errorMessage = "Fenrir coefficients and rates must be "
                    + "finite and non-negative, and damage reduction must "
                    + "not exceed one.";
                return false;
            }

            if (woundThreshold != 3)
            {
                errorMessage = "Fenrir requires a wound threshold of 3.";
                return false;
            }

            if (frenzyRequiredCriticalHits != 3)
            {
                errorMessage = "Fenrir requires 3 critical hits to end "
                    + "Frenzy.";
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

        private static bool IsFiniteRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }
    }
}

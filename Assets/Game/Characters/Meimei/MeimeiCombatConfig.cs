using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Meimei
{
    [CreateAssetMenu(
        fileName = "MeimeiCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Meimei")]
    public sealed class MeimeiCombatConfig : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient = 1.25d;

        [SerializeField]
        private double match4Coefficient = 2.20d;

        [SerializeField]
        private double match5Coefficient = 3.60d;

        [SerializeField]
        private double drunkenImmortalDanceCoefficient = 10.00d;

        [SerializeField]
        private int tipsinessMaxAmount = MeimeiRules.TipsinessMaxAmount;

        [SerializeField]
        private int match4TipsinessGainAmount = 1;

        [SerializeField]
        private int match5TipsinessGainAmount = 2;

        [SerializeField]
        private int activeCooldownTurns = 1;

        [SerializeField]
        private double awakening3Match3Coefficient = 1.40d;

        [SerializeField]
        private double awakening3Match4Coefficient = 2.50d;

        [SerializeField]
        private double awakening4Match5Coefficient = 4.10d;

        [SerializeField]
        private double awakening5DrunkenImmortalDanceCoefficient = 12.00d;

        [SerializeField]
        private double awakening6CriticalChancePerTipsiness = 0.06d;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public double DrunkenImmortalDanceCoefficient =>
            drunkenImmortalDanceCoefficient;
        public int TipsinessMaxAmount => tipsinessMaxAmount;
        public int Match4TipsinessGainAmount =>
            match4TipsinessGainAmount;
        public int Match5TipsinessGainAmount =>
            match5TipsinessGainAmount;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening3Match3Coefficient =>
            awakening3Match3Coefficient;
        public double Awakening3Match4Coefficient =>
            awakening3Match4Coefficient;
        public double Awakening4Match5Coefficient =>
            awakening4Match5Coefficient;
        public double Awakening5DrunkenImmortalDanceCoefficient =>
            awakening5DrunkenImmortalDanceCoefficient;
        public double Awakening6CriticalChancePerTipsiness =>
            awakening6CriticalChancePerTipsiness;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFinitePositive(match3Coefficient))
            {
                errorMessage = "Match3Coefficient must be finite and positive.";
                return false;
            }

            if (!IsFinitePositive(match4Coefficient))
            {
                errorMessage = "Match4Coefficient must be finite and positive.";
                return false;
            }

            if (!IsFinitePositive(match5Coefficient))
            {
                errorMessage = "Match5Coefficient must be finite and positive.";
                return false;
            }

            if (!IsFinitePositive(drunkenImmortalDanceCoefficient))
            {
                errorMessage = "DrunkenImmortalDanceCoefficient must be "
                    + "finite and positive.";
                return false;
            }

            if (tipsinessMaxAmount != MeimeiRules.TipsinessMaxAmount)
            {
                errorMessage = "TipsinessMaxAmount must match Meimei's "
                    + $"fixed maximum of {MeimeiRules.TipsinessMaxAmount}.";
                return false;
            }

            if (match4TipsinessGainAmount <= 0
                || match4TipsinessGainAmount > tipsinessMaxAmount)
            {
                errorMessage = "Match4TipsinessGainAmount must be between "
                    + "one and TipsinessMaxAmount.";
                return false;
            }

            if (match5TipsinessGainAmount <= 0
                || match5TipsinessGainAmount > tipsinessMaxAmount)
            {
                errorMessage = "Match5TipsinessGainAmount must be between "
                    + "one and TipsinessMaxAmount.";
                return false;
            }

            if (activeCooldownTurns <= 0)
            {
                errorMessage = "ActiveCooldownTurns must be positive.";
                return false;
            }

            if (!IsFinitePositive(awakening3Match3Coefficient))
            {
                errorMessage = "Awakening3Match3Coefficient must be finite "
                    + "and positive.";
                return false;
            }

            if (!IsFinitePositive(awakening3Match4Coefficient))
            {
                errorMessage = "Awakening3Match4Coefficient must be finite "
                    + "and positive.";
                return false;
            }

            if (!IsFinitePositive(awakening4Match5Coefficient))
            {
                errorMessage = "Awakening4Match5Coefficient must be finite "
                    + "and positive.";
                return false;
            }

            if (!IsFinitePositive(
                    awakening5DrunkenImmortalDanceCoefficient))
            {
                errorMessage = "Awakening5DrunkenImmortalDanceCoefficient "
                    + "must be finite and positive.";
                return false;
            }

            if (!IsProbabilityIncrease(
                    awakening6CriticalChancePerTipsiness))
            {
                errorMessage = "Awakening6CriticalChancePerTipsiness must "
                    + "be finite, positive, and no greater than one.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 0d;
        }

        private static bool IsProbabilityIncrease(double value)
        {
            return IsFinitePositive(value) && value <= 1d;
        }
    }
}

using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Beatrice
{
    [CreateAssetMenu(
        fileName = "BeatriceBloodstoneCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Beatrice Bloodstone")]
    public sealed class BeatriceBloodstoneCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField] private double passiveMaxDealtDamageIncreaseRate = 0.20d;
        [SerializeField] private double awakening3PassiveMaxDealtDamageIncreaseRate = 0.24d;
        [SerializeField] private double match3HpCostRate = 0.03d;
        [SerializeField] private double match4HpCostRate = 0.06d;
        [SerializeField] private double lowHpThresholdRate = 0.50d;
        [SerializeField] private double match3BaseCoefficient = 0.90d;
        [SerializeField] private double match3LowHpBonusCoefficient = 0.20d;
        [SerializeField] private double match4BaseCoefficient = 1.50d;
        [SerializeField] private double match4LowHpBonusCoefficient = 0.80d;
        [SerializeField] private double match5BaseCoefficient = 2.70d;
        [SerializeField] private double match5LostHpBonusCoefficient = 2.00d;
        [SerializeField] private double activeBaseCoefficient = 3.50d;
        [SerializeField] private double activeLostHpBonusCoefficient = 2.00d;
        [SerializeField] private double match5BaseLifestealRate = 0.10d;
        [SerializeField] private double match5LostHpLifestealRate = 0.20d;
        [SerializeField] private double activeBaseLifestealRate = 0.15d;
        [SerializeField] private double activeLostHpLifestealRate = 0.20d;
        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField] private double awakening4Match3BaseCoefficient = 1.00d;
        [SerializeField] private double awakening4Match3LowHpBonusCoefficient = 0.22d;
        [SerializeField] private double awakening4Match4BaseCoefficient = 1.65d;
        [SerializeField] private double awakening4Match4LowHpBonusCoefficient = 0.88d;
        [SerializeField] private double awakening5Match5BaseCoefficient = 3.00d;
        [SerializeField] private double awakening5Match5LostHpBonusCoefficient = 2.20d;
        [SerializeField] private double awakening6ActiveBaseCoefficient = 4.00d;
        [SerializeField] private double awakening6ActiveLostHpBonusCoefficient = 2.40d;

        public double PassiveMaxDealtDamageIncreaseRate => passiveMaxDealtDamageIncreaseRate;
        public double Awakening3PassiveMaxDealtDamageIncreaseRate => awakening3PassiveMaxDealtDamageIncreaseRate;
        public double Match3HpCostRate => match3HpCostRate;
        public double Match4HpCostRate => match4HpCostRate;
        public double LowHpThresholdRate => lowHpThresholdRate;
        public double Match3BaseCoefficient => match3BaseCoefficient;
        public double Match3LowHpBonusCoefficient => match3LowHpBonusCoefficient;
        public double Match4BaseCoefficient => match4BaseCoefficient;
        public double Match4LowHpBonusCoefficient => match4LowHpBonusCoefficient;
        public double Match5BaseCoefficient => match5BaseCoefficient;
        public double Match5LostHpBonusCoefficient => match5LostHpBonusCoefficient;
        public double ActiveBaseCoefficient => activeBaseCoefficient;
        public double ActiveLostHpBonusCoefficient => activeLostHpBonusCoefficient;
        public double Match5BaseLifestealRate => match5BaseLifestealRate;
        public double Match5LostHpLifestealRate => match5LostHpLifestealRate;
        public double ActiveBaseLifestealRate => activeBaseLifestealRate;
        public double ActiveLostHpLifestealRate => activeLostHpLifestealRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening4Match3BaseCoefficient => awakening4Match3BaseCoefficient;
        public double Awakening4Match3LowHpBonusCoefficient => awakening4Match3LowHpBonusCoefficient;
        public double Awakening4Match4BaseCoefficient => awakening4Match4BaseCoefficient;
        public double Awakening4Match4LowHpBonusCoefficient => awakening4Match4LowHpBonusCoefficient;
        public double Awakening5Match5BaseCoefficient => awakening5Match5BaseCoefficient;
        public double Awakening5Match5LostHpBonusCoefficient => awakening5Match5LostHpBonusCoefficient;
        public double Awakening6ActiveBaseCoefficient => awakening6ActiveBaseCoefficient;
        public double Awakening6ActiveLostHpBonusCoefficient => awakening6ActiveLostHpBonusCoefficient;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsRate(passiveMaxDealtDamageIncreaseRate)) return InvalidRate(nameof(PassiveMaxDealtDamageIncreaseRate), out errorMessage);
            if (!IsRate(awakening3PassiveMaxDealtDamageIncreaseRate)) return InvalidRate(nameof(Awakening3PassiveMaxDealtDamageIncreaseRate), out errorMessage);
            if (!IsRate(match3HpCostRate)) return InvalidRate(nameof(Match3HpCostRate), out errorMessage);
            if (!IsRate(match4HpCostRate)) return InvalidRate(nameof(Match4HpCostRate), out errorMessage);
            if (!IsRate(lowHpThresholdRate) || lowHpThresholdRate <= 0d) return InvalidRate(nameof(LowHpThresholdRate), out errorMessage);
            if (!IsCoefficient(match3BaseCoefficient)) return InvalidCoefficient(nameof(Match3BaseCoefficient), out errorMessage);
            if (!IsCoefficient(match3LowHpBonusCoefficient)) return InvalidCoefficient(nameof(Match3LowHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(match4BaseCoefficient)) return InvalidCoefficient(nameof(Match4BaseCoefficient), out errorMessage);
            if (!IsCoefficient(match4LowHpBonusCoefficient)) return InvalidCoefficient(nameof(Match4LowHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(match5BaseCoefficient)) return InvalidCoefficient(nameof(Match5BaseCoefficient), out errorMessage);
            if (!IsCoefficient(match5LostHpBonusCoefficient)) return InvalidCoefficient(nameof(Match5LostHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(activeBaseCoefficient)) return InvalidCoefficient(nameof(ActiveBaseCoefficient), out errorMessage);
            if (!IsCoefficient(activeLostHpBonusCoefficient)) return InvalidCoefficient(nameof(ActiveLostHpBonusCoefficient), out errorMessage);
            if (!IsRate(match5BaseLifestealRate)) return InvalidRate(nameof(Match5BaseLifestealRate), out errorMessage);
            if (!IsRate(match5LostHpLifestealRate)) return InvalidRate(nameof(Match5LostHpLifestealRate), out errorMessage);
            if (match5BaseLifestealRate + match5LostHpLifestealRate > 1d)
            {
                errorMessage = "Match5 lifesteal rate cannot exceed one at maximum lost HP.";
                return false;
            }
            if (!IsRate(activeBaseLifestealRate)) return InvalidRate(nameof(ActiveBaseLifestealRate), out errorMessage);
            if (!IsRate(activeLostHpLifestealRate)) return InvalidRate(nameof(ActiveLostHpLifestealRate), out errorMessage);
            if (activeBaseLifestealRate + activeLostHpLifestealRate > 1d)
            {
                errorMessage = "Active lifesteal rate cannot exceed one at maximum lost HP.";
                return false;
            }
            if (activeCooldownTurns <= 0)
            {
                errorMessage = "ActiveCooldownTurns must be greater than zero.";
                return false;
            }
            if (!IsCoefficient(awakening4Match3BaseCoefficient)) return InvalidCoefficient(nameof(Awakening4Match3BaseCoefficient), out errorMessage);
            if (!IsCoefficient(awakening4Match3LowHpBonusCoefficient)) return InvalidCoefficient(nameof(Awakening4Match3LowHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(awakening4Match4BaseCoefficient)) return InvalidCoefficient(nameof(Awakening4Match4BaseCoefficient), out errorMessage);
            if (!IsCoefficient(awakening4Match4LowHpBonusCoefficient)) return InvalidCoefficient(nameof(Awakening4Match4LowHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(awakening5Match5BaseCoefficient)) return InvalidCoefficient(nameof(Awakening5Match5BaseCoefficient), out errorMessage);
            if (!IsCoefficient(awakening5Match5LostHpBonusCoefficient)) return InvalidCoefficient(nameof(Awakening5Match5LostHpBonusCoefficient), out errorMessage);
            if (!IsCoefficient(awakening6ActiveBaseCoefficient)) return InvalidCoefficient(nameof(Awakening6ActiveBaseCoefficient), out errorMessage);
            if (!IsCoefficient(awakening6ActiveLostHpBonusCoefficient)) return InvalidCoefficient(nameof(Awakening6ActiveLostHpBonusCoefficient), out errorMessage);

            errorMessage = null;
            return true;
        }

        private static bool IsRate(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && value >= 0d && value <= 1d;
        }

        private static bool IsCoefficient(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && value >= 0d;
        }

        private static bool InvalidRate(string name, out string errorMessage)
        {
            errorMessage = $"{name} must be finite and between zero and one.";
            return false;
        }

        private static bool InvalidCoefficient(string name, out string errorMessage)
        {
            errorMessage = $"{name} must be finite and non-negative.";
            return false;
        }
    }
}

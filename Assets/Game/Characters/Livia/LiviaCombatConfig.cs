using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Livia
{
    [CreateAssetMenu(fileName = "LiviaCombatConfig", menuName = "Valor Chronicle/Combat Configs/Livia")]
    public sealed class LiviaCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private double match3HealingCoefficient = 0.14d;
        [SerializeField] private double match4HealingCoefficient = 0.24d;
        [SerializeField] private double match5HealingCoefficient = 0.36d;
        [SerializeField] private double passivePartyHealingIncreaseRate = 0.12d;
        [SerializeField] private double match5PartyDamageReductionRate = 0.08d;
        [SerializeField] private int match5DamageReductionDurationTurns = 2;
        [SerializeField] private double activeHealingCoefficient = 0.50d;
        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField] private double awakening2HealingIncreaseRate = 0.05d;
        [SerializeField] private double awakening3Match3HealingCoefficient = 0.15d;
        [SerializeField] private double awakening3Match4HealingCoefficient = 0.26d;
        [SerializeField] private double awakening4Match5HealingCoefficient = 0.40d;
        [SerializeField] private double awakening5PassivePartyHealingIncreaseRate = 0.15d;
        [SerializeField] private double awakening6ActiveHealingCoefficient = 0.60d;

        public double Match3HealingCoefficient => match3HealingCoefficient;
        public double Match4HealingCoefficient => match4HealingCoefficient;
        public double Match5HealingCoefficient => match5HealingCoefficient;
        public double PassivePartyHealingIncreaseRate => passivePartyHealingIncreaseRate;
        public double Match5PartyDamageReductionRate => match5PartyDamageReductionRate;
        public int Match5DamageReductionDurationTurns => match5DamageReductionDurationTurns;
        public double ActiveHealingCoefficient => activeHealingCoefficient;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening2HealingIncreaseRate => awakening2HealingIncreaseRate;
        public double Awakening3Match3HealingCoefficient => awakening3Match3HealingCoefficient;
        public double Awakening3Match4HealingCoefficient => awakening3Match4HealingCoefficient;
        public double Awakening4Match5HealingCoefficient => awakening4Match5HealingCoefficient;
        public double Awakening5PassivePartyHealingIncreaseRate => awakening5PassivePartyHealingIncreaseRate;
        public double Awakening6ActiveHealingCoefficient => awakening6ActiveHealingCoefficient;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(match3HealingCoefficient)
                || !IsFiniteNonNegative(match4HealingCoefficient)
                || !IsFiniteNonNegative(match5HealingCoefficient)
                || !IsRate(passivePartyHealingIncreaseRate)
                || !IsRate(match5PartyDamageReductionRate)
                || !IsFiniteNonNegative(activeHealingCoefficient)
                || !IsRate(awakening2HealingIncreaseRate)
                || !IsFiniteNonNegative(awakening3Match3HealingCoefficient)
                || !IsFiniteNonNegative(awakening3Match4HealingCoefficient)
                || !IsFiniteNonNegative(awakening4Match5HealingCoefficient)
                || !IsRate(awakening5PassivePartyHealingIncreaseRate)
                || !IsFiniteNonNegative(awakening6ActiveHealingCoefficient))
            {
                errorMessage = "Livia's coefficients must be finite and non-negative, and rates must not exceed 100%.";
                return false;
            }

            if (match5DamageReductionDurationTurns <= 0)
            {
                errorMessage = "Livia's Match-5 damage reduction duration must be positive.";
                return false;
            }

            if (activeCooldownTurns < 0)
            {
                errorMessage = "Livia's active cooldown cannot be negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        }

        private static bool IsRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }
    }
}

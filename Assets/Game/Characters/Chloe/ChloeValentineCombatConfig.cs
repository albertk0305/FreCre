using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Chloe
{
    [CreateAssetMenu(
        fileName = "ChloeValentineCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Chloe Valentine")]
    public sealed class ChloeValentineCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private double match3HealingCoefficient = 0.07d;
        [SerializeField] private double match4HealingCoefficient = 0.12d;
        [SerializeField] private double match5HealingCoefficient = 0.20d;
        [SerializeField] private double match5CriticalChanceIncreaseRate = 0.12d;
        [SerializeField] private int match5BuffDurationTurns = 2;
        [SerializeField] private double activeHealingCoefficient = 0.30d;
        [SerializeField] private double activeCriticalDamageIncreaseRate = 0.25d;
        [SerializeField] private int activeBuffDurationTurns = 3;
        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField] private double passivePartyAttackIncreaseRate = 0.05d;
        [SerializeField] private double awakening2HealingIncreaseRate = 0.05d;
        [SerializeField] private double awakening3Match3HealingCoefficient = 0.08d;
        [SerializeField] private double awakening3Match4HealingCoefficient = 0.13d;
        [SerializeField] private double awakening4Match5HealingCoefficient = 0.22d;
        [SerializeField] private double awakening4CriticalChanceIncreaseRate = 0.15d;
        [SerializeField] private double awakening5ActiveHealingCoefficient = 0.35d;
        [SerializeField] private double awakening5CriticalDamageIncreaseRate = 0.30d;
        [SerializeField] private double awakening6PartyAttackIncreaseRate = 0.07d;

        public double Match3HealingCoefficient => match3HealingCoefficient;
        public double Match4HealingCoefficient => match4HealingCoefficient;
        public double Match5HealingCoefficient => match5HealingCoefficient;
        public double Match5CriticalChanceIncreaseRate =>
            match5CriticalChanceIncreaseRate;
        public int Match5BuffDurationTurns => match5BuffDurationTurns;
        public double ActiveHealingCoefficient => activeHealingCoefficient;
        public double ActiveCriticalDamageIncreaseRate =>
            activeCriticalDamageIncreaseRate;
        public int ActiveBuffDurationTurns => activeBuffDurationTurns;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double PassivePartyAttackIncreaseRate =>
            passivePartyAttackIncreaseRate;
        public double Awakening2HealingIncreaseRate =>
            awakening2HealingIncreaseRate;
        public double Awakening3Match3HealingCoefficient =>
            awakening3Match3HealingCoefficient;
        public double Awakening3Match4HealingCoefficient =>
            awakening3Match4HealingCoefficient;
        public double Awakening4Match5HealingCoefficient =>
            awakening4Match5HealingCoefficient;
        public double Awakening4CriticalChanceIncreaseRate =>
            awakening4CriticalChanceIncreaseRate;
        public double Awakening5ActiveHealingCoefficient =>
            awakening5ActiveHealingCoefficient;
        public double Awakening5CriticalDamageIncreaseRate =>
            awakening5CriticalDamageIncreaseRate;
        public double Awakening6PartyAttackIncreaseRate =>
            awakening6PartyAttackIncreaseRate;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsRate(match3HealingCoefficient)
                || !IsRate(match4HealingCoefficient)
                || !IsRate(match5HealingCoefficient)
                || !IsRate(match5CriticalChanceIncreaseRate)
                || !IsRate(activeHealingCoefficient)
                || !IsRate(activeCriticalDamageIncreaseRate)
                || !IsRate(passivePartyAttackIncreaseRate)
                || !IsRate(awakening2HealingIncreaseRate)
                || !IsRate(awakening3Match3HealingCoefficient)
                || !IsRate(awakening3Match4HealingCoefficient)
                || !IsRate(awakening4Match5HealingCoefficient)
                || !IsRate(awakening4CriticalChanceIncreaseRate)
                || !IsRate(awakening5ActiveHealingCoefficient)
                || !IsRate(awakening5CriticalDamageIncreaseRate)
                || !IsRate(awakening6PartyAttackIncreaseRate))
            {
                errorMessage = "All Chloe rates must be finite and between zero and one.";
                return false;
            }

            if (match5BuffDurationTurns <= 0
                || activeBuffDurationTurns <= 0)
            {
                errorMessage = "All Chloe buff durations must be positive.";
                return false;
            }

            if (activeCooldownTurns <= 0)
            {
                errorMessage = "Chloe's active cooldown must be positive.";
                return false;
            }

            if (awakening3Match3HealingCoefficient < match3HealingCoefficient
                || awakening3Match4HealingCoefficient < match4HealingCoefficient
                || awakening4Match5HealingCoefficient < match5HealingCoefficient
                || awakening4CriticalChanceIncreaseRate
                    < match5CriticalChanceIncreaseRate
                || awakening5ActiveHealingCoefficient
                    < activeHealingCoefficient
                || awakening5CriticalDamageIncreaseRate
                    < activeCriticalDamageIncreaseRate
                || awakening6PartyAttackIncreaseRate
                    < passivePartyAttackIncreaseRate)
            {
                errorMessage = "Chloe awakening values cannot be weaker than their base values.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsRate(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d
                && value <= 1d;
        }
    }
}


using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Chloe
{
    public sealed class ChloeValentineResolvedCombatRules
    {
        internal ChloeValentineResolvedCombatRules(
            double match3HealingCoefficient,
            double match4HealingCoefficient,
            double match5HealingCoefficient,
            double match5CriticalChanceIncreaseRate,
            int match5BuffDurationTurns,
            double activeHealingCoefficient,
            double activeCriticalDamageIncreaseRate,
            int activeBuffDurationTurns,
            int activeCooldownTurns,
            double passivePartyAttackIncreaseRate,
            double actionLocalHealingIncreaseRate)
        {
            Match3HealingCoefficient = match3HealingCoefficient;
            Match4HealingCoefficient = match4HealingCoefficient;
            Match5HealingCoefficient = match5HealingCoefficient;
            Match5CriticalChanceIncreaseRate = match5CriticalChanceIncreaseRate;
            Match5BuffDurationTurns = match5BuffDurationTurns;
            ActiveHealingCoefficient = activeHealingCoefficient;
            ActiveCriticalDamageIncreaseRate = activeCriticalDamageIncreaseRate;
            ActiveBuffDurationTurns = activeBuffDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
            PassivePartyAttackIncreaseRate = passivePartyAttackIncreaseRate;
            ActionLocalHealingIncreaseRate = actionLocalHealingIncreaseRate;
        }

        public double Match3HealingCoefficient { get; }
        public double Match4HealingCoefficient { get; }
        public double Match5HealingCoefficient { get; }
        public double Match5CriticalChanceIncreaseRate { get; }
        public int Match5BuffDurationTurns { get; }
        public double ActiveHealingCoefficient { get; }
        public double ActiveCriticalDamageIncreaseRate { get; }
        public int ActiveBuffDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
        public double PassivePartyAttackIncreaseRate { get; }
        public double ActionLocalHealingIncreaseRate { get; }
    }

    public static class ChloeValentineCombatRuleResolver
    {
        public static ChloeValentineResolvedCombatRules Resolve(
            ChloeValentineCombatConfig config,
            int awakening)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(nameof(awakening));
            }

            return new ChloeValentineResolvedCombatRules(
                awakening >= 3
                    ? config.Awakening3Match3HealingCoefficient
                    : config.Match3HealingCoefficient,
                awakening >= 3
                    ? config.Awakening3Match4HealingCoefficient
                    : config.Match4HealingCoefficient,
                awakening >= 4
                    ? config.Awakening4Match5HealingCoefficient
                    : config.Match5HealingCoefficient,
                awakening >= 4
                    ? config.Awakening4CriticalChanceIncreaseRate
                    : config.Match5CriticalChanceIncreaseRate,
                config.Match5BuffDurationTurns,
                awakening >= 5
                    ? config.Awakening5ActiveHealingCoefficient
                    : config.ActiveHealingCoefficient,
                awakening >= 5
                    ? config.Awakening5CriticalDamageIncreaseRate
                    : config.ActiveCriticalDamageIncreaseRate,
                config.ActiveBuffDurationTurns,
                config.ActiveCooldownTurns,
                awakening >= 6
                    ? config.Awakening6PartyAttackIncreaseRate
                    : config.PassivePartyAttackIncreaseRate,
                awakening >= 2
                    ? config.Awakening2HealingIncreaseRate
                    : 0d);
        }
    }
}


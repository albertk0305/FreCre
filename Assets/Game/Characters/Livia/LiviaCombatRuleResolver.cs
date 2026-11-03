using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaResolvedCombatRules
    {
        internal LiviaResolvedCombatRules(
            double match3HealingCoefficient,
            double match4HealingCoefficient,
            double match5HealingCoefficient,
            double passivePartyHealingIncreaseRate,
            double match5PartyDamageReductionRate,
            int match5DamageReductionDurationTurns,
            double activeHealingCoefficient,
            int activeCooldownTurns,
            double actionLocalHealingIncreaseRate)
        {
            Match3HealingCoefficient = match3HealingCoefficient;
            Match4HealingCoefficient = match4HealingCoefficient;
            Match5HealingCoefficient = match5HealingCoefficient;
            PassivePartyHealingIncreaseRate = passivePartyHealingIncreaseRate;
            Match5PartyDamageReductionRate = match5PartyDamageReductionRate;
            Match5DamageReductionDurationTurns = match5DamageReductionDurationTurns;
            ActiveHealingCoefficient = activeHealingCoefficient;
            ActiveCooldownTurns = activeCooldownTurns;
            ActionLocalHealingIncreaseRate = actionLocalHealingIncreaseRate;
        }

        public double Match3HealingCoefficient { get; }
        public double Match4HealingCoefficient { get; }
        public double Match5HealingCoefficient { get; }
        public double PassivePartyHealingIncreaseRate { get; }
        public double Match5PartyDamageReductionRate { get; }
        public int Match5DamageReductionDurationTurns { get; }
        public double ActiveHealingCoefficient { get; }
        public int ActiveCooldownTurns { get; }
        public double ActionLocalHealingIncreaseRate { get; }
    }

    public static class LiviaCombatRuleResolver
    {
        public static LiviaResolvedCombatRules Resolve(
            LiviaCombatConfig config,
            int awakening)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!config.TryValidate(out string errorMessage))
                throw new ArgumentException(errorMessage, nameof(config));
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
                throw new ArgumentOutOfRangeException(nameof(awakening));

            return new LiviaResolvedCombatRules(
                awakening >= 3 ? config.Awakening3Match3HealingCoefficient : config.Match3HealingCoefficient,
                awakening >= 3 ? config.Awakening3Match4HealingCoefficient : config.Match4HealingCoefficient,
                awakening >= 4 ? config.Awakening4Match5HealingCoefficient : config.Match5HealingCoefficient,
                awakening >= 5 ? config.Awakening5PassivePartyHealingIncreaseRate : config.PassivePartyHealingIncreaseRate,
                config.Match5PartyDamageReductionRate,
                config.Match5DamageReductionDurationTurns,
                awakening >= 6 ? config.Awakening6ActiveHealingCoefficient : config.ActiveHealingCoefficient,
                config.ActiveCooldownTurns,
                awakening >= 2 ? config.Awakening2HealingIncreaseRate : 0d);
        }
    }
}

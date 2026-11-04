using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideResolvedCombatRules
    {
        internal KaylaStrideResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            double match5CriticalDamageIncreaseRate,
            int match5BuffDurationTurns,
            double activeCriticalChanceIncreaseRate,
            int activeDurationTurns,
            int activeCooldownTurns,
            double chargeCoefficientPerStack,
            int chargeEnhancementThreshold,
            double chargeEnhancementMultiplier,
            int chargeMaxStacks)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            Match5CriticalDamageIncreaseRate =
                match5CriticalDamageIncreaseRate;
            Match5BuffDurationTurns = match5BuffDurationTurns;
            ActiveCriticalChanceIncreaseRate =
                activeCriticalChanceIncreaseRate;
            ActiveDurationTurns = activeDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
            ChargeCoefficientPerStack = chargeCoefficientPerStack;
            ChargeEnhancementThreshold = chargeEnhancementThreshold;
            ChargeEnhancementMultiplier = chargeEnhancementMultiplier;
            ChargeMaxStacks = chargeMaxStacks;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public double Match5CriticalDamageIncreaseRate { get; }
        public int Match5BuffDurationTurns { get; }
        public double ActiveCriticalChanceIncreaseRate { get; }
        public int ActiveDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
        public double ChargeCoefficientPerStack { get; }
        public int ChargeEnhancementThreshold { get; }
        public double ChargeEnhancementMultiplier { get; }
        public int ChargeMaxStacks { get; }

        public double ResolveChargeCoefficient(int consumedStacks)
        {
            if (consumedStacks <= 0 || consumedStacks > ChargeMaxStacks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(consumedStacks));
            }

            double coefficient = ChargeCoefficientPerStack
                * consumedStacks;
            double result = consumedStacks >= ChargeEnhancementThreshold
                ? coefficient * ChargeEnhancementMultiplier
                : coefficient;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    "Resolved Charge coefficient must remain finite.");
            }

            return result;
        }
    }

    public static class KaylaStrideCombatRuleResolver
    {
        private const int Match3And4EnhancementAwakening = 3;
        private const int Match5EnhancementAwakening = 4;
        private const int ActiveEnhancementAwakening = 5;
        private const int ChargeEnhancementAwakening = 6;

        public static KaylaStrideResolvedCombatRules Resolve(
            KaylaStrideCombatConfig config,
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
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new KaylaStrideResolvedCombatRules(
                awakening >= Match3And4EnhancementAwakening
                    ? config.Awakening3Match3Coefficient
                    : config.Match3Coefficient,
                awakening >= Match3And4EnhancementAwakening
                    ? config.Awakening3Match4Coefficient
                    : config.Match4Coefficient,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening4Match5Coefficient
                    : config.Match5Coefficient,
                awakening >= Match5EnhancementAwakening
                    ? config
                        .Awakening4Match5CriticalDamageIncreaseRate
                    : config.Match5CriticalDamageIncreaseRate,
                config.Match5BuffDurationTurns,
                config.ActiveCriticalChanceIncreaseRate,
                config.ActiveDurationTurns,
                awakening >= ActiveEnhancementAwakening
                    ? config.Awakening5ActiveCooldownTurns
                    : config.ActiveCooldownTurns,
                awakening >= ChargeEnhancementAwakening
                    ? config.Awakening6ChargeCoefficientPerStack
                    : config.ChargeCoefficientPerStack,
                config.ChargeEnhancementThreshold,
                config.ChargeEnhancementMultiplier,
                config.ChargeMaxStacks);
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Characters.Lancelot
{
    public sealed class LancelotResolvedCombatRules
    {
        internal LancelotResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            double passiveCriticalChanceIncreaseRate,
            double match4CriticalChanceIncreaseRate,
            double match5CriticalChanceIncreaseRate,
            double baseCriticalDamageMultiplier,
            double activeCriticalDamageIncreaseRate,
            int activeDurationTurns,
            int activeCooldownTurns)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            PassiveCriticalChanceIncreaseRate =
                passiveCriticalChanceIncreaseRate;
            Match4CriticalChanceIncreaseRate =
                match4CriticalChanceIncreaseRate;
            Match5CriticalChanceIncreaseRate =
                match5CriticalChanceIncreaseRate;
            BaseCriticalDamageMultiplier = baseCriticalDamageMultiplier;
            ActiveCriticalDamageIncreaseRate =
                activeCriticalDamageIncreaseRate;
            ActiveDurationTurns = activeDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public double PassiveCriticalChanceIncreaseRate { get; }
        public double Match4CriticalChanceIncreaseRate { get; }
        public double Match5CriticalChanceIncreaseRate { get; }
        public double BaseCriticalDamageMultiplier { get; }
        public double ActiveCriticalDamageIncreaseRate { get; }
        public int ActiveDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
    }

    public static class LancelotCombatRuleResolver
    {
        public const int MinimumAwakening = 0;
        public const int MaximumAwakening = 6;

        private const int MatchEnhancementAwakening = 3;
        private const int ActiveEnhancementAwakening = 4;
        private const int CriticalChanceEnhancementAwakening = 5;
        private const int CriticalDamageEnhancementAwakening = 6;

        private const double EnhancedMatch3Coefficient = 1.47d;
        private const double EnhancedMatch4Coefficient = 2.65d;
        private const double EnhancedMatch5Coefficient = 4.75d;
        private const double EnhancedActiveCriticalDamageIncreaseRate = 0.65d;
        private const double AwakeningCriticalChanceIncreaseRate = 0.08d;
        private const double AwakeningCriticalDamageIncreaseRate = 0.20d;

        public static LancelotResolvedCombatRules Resolve(
            LancelotCombatConfig config,
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

            if (awakening < MinimumAwakening
                || awakening > MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between {MinimumAwakening} and "
                        + $"{MaximumAwakening}.");
            }

            bool hasEnhancedMatches =
                awakening >= MatchEnhancementAwakening;
            return new LancelotResolvedCombatRules(
                hasEnhancedMatches
                    ? EnhancedMatch3Coefficient
                    : config.Match3Coefficient,
                hasEnhancedMatches
                    ? EnhancedMatch4Coefficient
                    : config.Match4Coefficient,
                hasEnhancedMatches
                    ? EnhancedMatch5Coefficient
                    : config.Match5Coefficient,
                config.PassiveCriticalChanceIncreaseRate
                    + (awakening >= CriticalChanceEnhancementAwakening
                        ? AwakeningCriticalChanceIncreaseRate
                        : 0d),
                config.Match4CriticalChanceIncreaseRate,
                config.Match5CriticalChanceIncreaseRate,
                DamageContextBuildRequest.DefaultBaseCriticalDamageMultiplier
                    + (awakening >= CriticalDamageEnhancementAwakening
                        ? AwakeningCriticalDamageIncreaseRate
                        : 0d),
                awakening >= ActiveEnhancementAwakening
                    ? EnhancedActiveCriticalDamageIncreaseRate
                    : config.ActiveCriticalDamageIncreaseRate,
                config.ActiveDurationTurns,
                config.ActiveCooldownTurns);
        }
    }
}

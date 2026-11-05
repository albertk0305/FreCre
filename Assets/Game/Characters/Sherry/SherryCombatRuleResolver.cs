using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryResolvedCombatRules
    {
        internal SherryResolvedCombatRules(
            int stylishMaxRank,
            double stylishDamageExponentBase,
            double stylishMaxDamageIncreaseRate,
            double jackpotExponentBase,
            double jackpotMaxCoefficient,
            int activeCooldownTurns,
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            double match3OniIntimidationRate,
            double match4OniIntimidationRate,
            double match5OniIntimidationRate,
            int oniIntimidationDurationTurns,
            int oniIntimidationMinimumRank)
        {
            StylishMaxRank = stylishMaxRank;
            StylishDamageExponentBase = stylishDamageExponentBase;
            StylishMaxDamageIncreaseRate =
                stylishMaxDamageIncreaseRate;
            JackpotExponentBase = jackpotExponentBase;
            JackpotMaxCoefficient = jackpotMaxCoefficient;
            ActiveCooldownTurns = activeCooldownTurns;
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            Match3OniIntimidationRate = match3OniIntimidationRate;
            Match4OniIntimidationRate = match4OniIntimidationRate;
            Match5OniIntimidationRate = match5OniIntimidationRate;
            OniIntimidationDurationTurns =
                oniIntimidationDurationTurns;
            OniIntimidationMinimumRank =
                oniIntimidationMinimumRank;
        }

        public int StylishMaxRank { get; }
        public double StylishDamageExponentBase { get; }
        public double StylishMaxDamageIncreaseRate { get; }
        public double JackpotExponentBase { get; }
        public double JackpotMaxCoefficient { get; }
        public int ActiveCooldownTurns { get; }
        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public double Match3OniIntimidationRate { get; }
        public double Match4OniIntimidationRate { get; }
        public double Match5OniIntimidationRate { get; }
        public int OniIntimidationDurationTurns { get; }
        public int OniIntimidationMinimumRank { get; }

        public double ResolveMatchCoefficient(SherryMatchTier matchTier)
        {
            switch (matchTier)
            {
                case SherryMatchTier.Three:
                    return Match3Coefficient;
                case SherryMatchTier.Four:
                    return Match4Coefficient;
                case SherryMatchTier.FiveOrMore:
                    return Match5Coefficient;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be Three, Four, or FiveOrMore.");
            }
        }

        public double ResolveOniIntimidationRate(
            SherryMatchTier matchTier)
        {
            switch (matchTier)
            {
                case SherryMatchTier.Three:
                    return Match3OniIntimidationRate;
                case SherryMatchTier.Four:
                    return Match4OniIntimidationRate;
                case SherryMatchTier.FiveOrMore:
                    return Match5OniIntimidationRate;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be Three, Four, or FiveOrMore.");
            }
        }

        public double ResolveStylishDamageIncreaseRate(int stylishRank)
        {
            ValidateRank(stylishRank, nameof(stylishRank));
            return ResolveExponentialValue(
                stylishRank,
                StylishMaxRank,
                StylishDamageExponentBase,
                StylishMaxDamageIncreaseRate);
        }

        public double ResolveJackpotCoefficient(int consumedRank)
        {
            ValidateRank(consumedRank, nameof(consumedRank));
            return ResolveExponentialValue(
                consumedRank,
                StylishMaxRank,
                JackpotExponentBase,
                JackpotMaxCoefficient);
        }

        private void ValidateRank(int rank, string parameterName)
        {
            if (rank < 0 || rank > StylishMaxRank)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rank,
                    $"Rank must be between zero and {StylishMaxRank}.");
            }
        }

        private static double ResolveExponentialValue(
            int rank,
            int maximumRank,
            double exponentBase,
            double maximumValue)
        {
            if (rank == 0)
            {
                return 0d;
            }

            double numerator = Math.Pow(exponentBase, rank) - 1d;
            double denominator =
                Math.Pow(exponentBase, maximumRank) - 1d;
            return maximumValue * numerator / denominator;
        }
    }

    public static class SherryCombatRuleResolver
    {
        private const int MatchEnhancementAwakening = 3;
        private const int OniIntimidationEnhancementAwakening = 4;
        private const int StylishEnhancementAwakening = 5;
        private const int JackpotEnhancementAwakening = 6;

        public static SherryResolvedCombatRules Resolve(
            SherryCombatConfig config,
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

            bool enhancedMatch =
                awakening >= MatchEnhancementAwakening;
            bool enhancedIntimidation =
                awakening >= OniIntimidationEnhancementAwakening;
            return new SherryResolvedCombatRules(
                config.StylishMaxRank,
                config.StylishDamageExponentBase,
                awakening >= StylishEnhancementAwakening
                    ? config.Awakening5StylishMaxDamageIncreaseRate
                    : config.BaseStylishMaxDamageIncreaseRate,
                config.JackpotExponentBase,
                awakening >= JackpotEnhancementAwakening
                    ? config.Awakening6JackpotMaxCoefficient
                    : config.BaseJackpotMaxCoefficient,
                config.ActiveCooldownTurns,
                enhancedMatch
                    ? config.Awakening3Match3Coefficient
                    : config.Match3Coefficient,
                enhancedMatch
                    ? config.Awakening3Match4Coefficient
                    : config.Match4Coefficient,
                enhancedMatch
                    ? config.Awakening3Match5Coefficient
                    : config.Match5Coefficient,
                enhancedIntimidation
                    ? config.Awakening4Match3OniIntimidationRate
                    : config.Match3OniIntimidationRate,
                enhancedIntimidation
                    ? config.Awakening4Match4OniIntimidationRate
                    : config.Match4OniIntimidationRate,
                enhancedIntimidation
                    ? config.Awakening4Match5OniIntimidationRate
                    : config.Match5OniIntimidationRate,
                config.OniIntimidationDurationTurns,
                config.OniIntimidationMinimumRank);
        }
    }
}

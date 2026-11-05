using System;
using ValorChronicle.Battle.Board;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksResolvedCombatRules
    {
        internal HazelSparksResolvedCombatRules(
            double match3DamageCoefficient,
            double match4DamageCoefficient,
            double match5DamageCoefficient,
            int match3SharpeningGain,
            int match4SharpeningGain,
            int match5SharpeningGain,
            int maximumSharpeningStacks,
            int initialSharpeningStacks,
            double artisansTouchRatePerStack,
            double forgedMomentumRatePerOverflow,
            int forgedMomentumDurationTurns,
            int activeCooldownTurns,
            int activeBlockConversionCount)
        {
            Match3DamageCoefficient = match3DamageCoefficient;
            Match4DamageCoefficient = match4DamageCoefficient;
            Match5DamageCoefficient = match5DamageCoefficient;
            Match3SharpeningGain = match3SharpeningGain;
            Match4SharpeningGain = match4SharpeningGain;
            Match5SharpeningGain = match5SharpeningGain;
            MaximumSharpeningStacks = maximumSharpeningStacks;
            InitialSharpeningStacks = initialSharpeningStacks;
            ArtisansTouchRatePerStack = artisansTouchRatePerStack;
            ForgedMomentumRatePerOverflow =
                forgedMomentumRatePerOverflow;
            ForgedMomentumDurationTurns = forgedMomentumDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveBlockConversionCount = activeBlockConversionCount;
        }

        public double Match3DamageCoefficient { get; }
        public double Match4DamageCoefficient { get; }
        public double Match5DamageCoefficient { get; }
        public int Match3SharpeningGain { get; }
        public int Match4SharpeningGain { get; }
        public int Match5SharpeningGain { get; }
        public int MaximumSharpeningStacks { get; }
        public int InitialSharpeningStacks { get; }
        public double ArtisansTouchRatePerStack { get; }
        public double ForgedMomentumRatePerOverflow { get; }
        public int ForgedMomentumDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveBlockConversionCount { get; }

        public double GetDamageCoefficient(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return Match3DamageCoefficient;
                case BoardMatchTier.Four:
                    return Match4DamageCoefficient;
                case BoardMatchTier.FiveOrMore:
                    return Match5DamageCoefficient;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public int GetSharpeningGain(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return Match3SharpeningGain;
                case BoardMatchTier.Four:
                    return Match4SharpeningGain;
                case BoardMatchTier.FiveOrMore:
                    return Match5SharpeningGain;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public double CalculateArtisansTouchMagnitude(int sharpeningStacks)
        {
            if (sharpeningStacks < 0
                || sharpeningStacks > MaximumSharpeningStacks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sharpeningStacks));
            }

            return sharpeningStacks * ArtisansTouchRatePerStack;
        }

        public double CalculateForgedMomentumMagnitude(int overflowAmount)
        {
            if (overflowAmount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(overflowAmount));
            }

            return overflowAmount * ForgedMomentumRatePerOverflow;
        }
    }

    public static class HazelSparksCombatRuleResolver
    {
        public static HazelSparksResolvedCombatRules Resolve(
            HazelSparksCombatConfig config,
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

            return new HazelSparksResolvedCombatRules(
                awakening >= 3
                    ? config.Awakening3Match3DamageCoefficient
                    : config.Match3DamageCoefficient,
                awakening >= 3
                    ? config.Awakening3Match4DamageCoefficient
                    : config.Match4DamageCoefficient,
                awakening >= 3
                    ? config.Awakening3Match5DamageCoefficient
                    : config.Match5DamageCoefficient,
                config.Match3SharpeningGain,
                config.Match4SharpeningGain,
                config.Match5SharpeningGain,
                config.MaximumSharpeningStacks,
                awakening >= 6
                    ? config.Awakening6InitialSharpeningStacks
                    : config.InitialSharpeningStacks,
                awakening >= 4
                    ? config.Awakening4ArtisansTouchRatePerStack
                    : config.ArtisansTouchRatePerStack,
                awakening >= 5
                    ? config.Awakening5ForgedMomentumRatePerOverflow
                    : config.ForgedMomentumRatePerOverflow,
                config.ForgedMomentumDurationTurns,
                config.ActiveCooldownTurns,
                config.ActiveBlockConversionCount);
        }
    }
}

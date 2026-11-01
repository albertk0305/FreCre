using System;

namespace ValorChronicle.Characters.Beatrice
{
    public readonly struct BeatriceBloodstoneScalingResult
    {
        internal BeatriceBloodstoneScalingResult(
            double lostPartyHpRate,
            double skillCoefficient,
            double lifestealRate)
        {
            LostPartyHpRate = lostPartyHpRate;
            SkillCoefficient = skillCoefficient;
            LifestealRate = lifestealRate;
        }

        public double LostPartyHpRate { get; }
        public double SkillCoefficient { get; }
        public double LifestealRate { get; }
    }

    public sealed class BeatriceBloodstoneResolvedCombatRules
    {
        internal BeatriceBloodstoneResolvedCombatRules(
            double passiveMaxDealtDamageIncreaseRate,
            double match3HpCostRate,
            double match4HpCostRate,
            double lowHpThresholdRate,
            double match3BaseCoefficient,
            double match3LowHpBonusCoefficient,
            double match4BaseCoefficient,
            double match4LowHpBonusCoefficient,
            double match5BaseCoefficient,
            double match5LostHpBonusCoefficient,
            double activeBaseCoefficient,
            double activeLostHpBonusCoefficient,
            double match5BaseLifestealRate,
            double match5LostHpLifestealRate,
            double activeBaseLifestealRate,
            double activeLostHpLifestealRate,
            int activeCooldownTurns)
        {
            PassiveMaxDealtDamageIncreaseRate =
                passiveMaxDealtDamageIncreaseRate;
            Match3HpCostRate = match3HpCostRate;
            Match4HpCostRate = match4HpCostRate;
            LowHpThresholdRate = lowHpThresholdRate;
            Match3BaseCoefficient = match3BaseCoefficient;
            Match3LowHpBonusCoefficient = match3LowHpBonusCoefficient;
            Match4BaseCoefficient = match4BaseCoefficient;
            Match4LowHpBonusCoefficient = match4LowHpBonusCoefficient;
            Match5BaseCoefficient = match5BaseCoefficient;
            Match5LostHpBonusCoefficient = match5LostHpBonusCoefficient;
            ActiveBaseCoefficient = activeBaseCoefficient;
            ActiveLostHpBonusCoefficient = activeLostHpBonusCoefficient;
            Match5BaseLifestealRate = match5BaseLifestealRate;
            Match5LostHpLifestealRate = match5LostHpLifestealRate;
            ActiveBaseLifestealRate = activeBaseLifestealRate;
            ActiveLostHpLifestealRate = activeLostHpLifestealRate;
            ActiveCooldownTurns = activeCooldownTurns;
        }

        public double PassiveMaxDealtDamageIncreaseRate { get; }
        public double Match3HpCostRate { get; }
        public double Match4HpCostRate { get; }
        public double LowHpThresholdRate { get; }
        public double Match3BaseCoefficient { get; }
        public double Match3LowHpBonusCoefficient { get; }
        public double Match4BaseCoefficient { get; }
        public double Match4LowHpBonusCoefficient { get; }
        public double Match5BaseCoefficient { get; }
        public double Match5LostHpBonusCoefficient { get; }
        public double ActiveBaseCoefficient { get; }
        public double ActiveLostHpBonusCoefficient { get; }
        public double Match5BaseLifestealRate { get; }
        public double Match5LostHpLifestealRate { get; }
        public double ActiveBaseLifestealRate { get; }
        public double ActiveLostHpLifestealRate { get; }
        public int ActiveCooldownTurns { get; }

        public double ResolveLostPartyHpRate(
            long currentPartyHp,
            long maxPartyHp)
        {
            double normalizedHpRate = ResolveNormalizedPartyHpRate(
                currentPartyHp,
                maxPartyHp);
            return 1d - normalizedHpRate;
        }

        public double ResolvePassiveDealtDamageIncreaseRate(
            long currentPartyHp,
            long maxPartyHp)
        {
            return ResolveLostPartyHpRate(currentPartyHp, maxPartyHp)
                * PassiveMaxDealtDamageIncreaseRate;
        }

        public bool IsLowPartyHp(long currentPartyHp, long maxPartyHp)
        {
            return ResolveNormalizedPartyHpRate(currentPartyHp, maxPartyHp)
                < LowHpThresholdRate;
        }

        public double ResolveMatch3Coefficient(
            long partyHpAfterCost,
            long maxPartyHp)
        {
            return Match3BaseCoefficient
                + (IsLowPartyHp(partyHpAfterCost, maxPartyHp)
                    ? Match3LowHpBonusCoefficient
                    : 0d);
        }

        public double ResolveMatch4Coefficient(
            long partyHpAfterCost,
            long maxPartyHp)
        {
            return Match4BaseCoefficient
                + (IsLowPartyHp(partyHpAfterCost, maxPartyHp)
                    ? Match4LowHpBonusCoefficient
                    : 0d);
        }

        public BeatriceBloodstoneScalingResult ResolveMatch5(
            long currentPartyHp,
            long maxPartyHp)
        {
            double lostPartyHpRate = ResolveLostPartyHpRate(
                currentPartyHp,
                maxPartyHp);
            return new BeatriceBloodstoneScalingResult(
                lostPartyHpRate,
                Match5BaseCoefficient
                    + (lostPartyHpRate * Match5LostHpBonusCoefficient),
                Match5BaseLifestealRate
                    + (lostPartyHpRate * Match5LostHpLifestealRate));
        }

        public BeatriceBloodstoneScalingResult ResolveActive(
            long currentPartyHp,
            long maxPartyHp)
        {
            double lostPartyHpRate = ResolveLostPartyHpRate(
                currentPartyHp,
                maxPartyHp);
            return new BeatriceBloodstoneScalingResult(
                lostPartyHpRate,
                ActiveBaseCoefficient
                    + (lostPartyHpRate * ActiveLostHpBonusCoefficient),
                ActiveBaseLifestealRate
                    + (lostPartyHpRate * ActiveLostHpLifestealRate));
        }

        private static double ResolveNormalizedPartyHpRate(
            long currentPartyHp,
            long maxPartyHp)
        {
            if (maxPartyHp <= 0L)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxPartyHp),
                    maxPartyHp,
                    "Maximum party HP must be greater than zero.");
            }

            long normalizedCurrentHp = currentPartyHp;
            if (normalizedCurrentHp < 0L)
            {
                normalizedCurrentHp = 0L;
            }
            else if (normalizedCurrentHp > maxPartyHp)
            {
                normalizedCurrentHp = maxPartyHp;
            }

            return (double)normalizedCurrentHp / maxPartyHp;
        }
    }

    public static class BeatriceBloodstoneCombatRuleResolver
    {
        public const int MinimumAwakening = 0;
        public const int MaximumAwakening = 6;

        private const int PassiveEnhancementAwakening = 3;
        private const int MatchEnhancementAwakening = 4;
        private const int Match5EnhancementAwakening = 5;
        private const int ActiveEnhancementAwakening = 6;

        public static BeatriceBloodstoneResolvedCombatRules Resolve(
            BeatriceBloodstoneCombatConfig config,
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

            return new BeatriceBloodstoneResolvedCombatRules(
                awakening >= PassiveEnhancementAwakening
                    ? config.Awakening3PassiveMaxDealtDamageIncreaseRate
                    : config.PassiveMaxDealtDamageIncreaseRate,
                config.Match3HpCostRate,
                config.Match4HpCostRate,
                config.LowHpThresholdRate,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match3BaseCoefficient
                    : config.Match3BaseCoefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match3LowHpBonusCoefficient
                    : config.Match3LowHpBonusCoefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match4BaseCoefficient
                    : config.Match4BaseCoefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match4LowHpBonusCoefficient
                    : config.Match4LowHpBonusCoefficient,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening5Match5BaseCoefficient
                    : config.Match5BaseCoefficient,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening5Match5LostHpBonusCoefficient
                    : config.Match5LostHpBonusCoefficient,
                awakening >= ActiveEnhancementAwakening
                    ? config.Awakening6ActiveBaseCoefficient
                    : config.ActiveBaseCoefficient,
                awakening >= ActiveEnhancementAwakening
                    ? config.Awakening6ActiveLostHpBonusCoefficient
                    : config.ActiveLostHpBonusCoefficient,
                config.Match5BaseLifestealRate,
                config.Match5LostHpLifestealRate,
                config.ActiveBaseLifestealRate,
                config.ActiveLostHpLifestealRate,
                config.ActiveCooldownTurns);
        }
    }
}

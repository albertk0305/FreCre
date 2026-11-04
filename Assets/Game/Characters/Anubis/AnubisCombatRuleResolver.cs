using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Anubis
{
    public sealed class AnubisResolvedCombatRules
    {
        internal AnubisResolvedCombatRules(
            double match3DamageCoefficient,
            double match4DamageCoefficient,
            double match5DamageCoefficient,
            double match3RadianceRate,
            double match4RadianceRate,
            double match5RadianceRate,
            int radianceDurationTurns,
            double passiveLightDamageIncreaseRate,
            int activeCooldownTurns,
            int activeBlockConversionCount)
        {
            Match3DamageCoefficient = match3DamageCoefficient;
            Match4DamageCoefficient = match4DamageCoefficient;
            Match5DamageCoefficient = match5DamageCoefficient;
            Match3RadianceRate = match3RadianceRate;
            Match4RadianceRate = match4RadianceRate;
            Match5RadianceRate = match5RadianceRate;
            RadianceDurationTurns = radianceDurationTurns;
            PassiveLightDamageIncreaseRate = passiveLightDamageIncreaseRate;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveBlockConversionCount = activeBlockConversionCount;
        }

        public double Match3DamageCoefficient { get; }
        public double Match4DamageCoefficient { get; }
        public double Match5DamageCoefficient { get; }
        public double Match3RadianceRate { get; }
        public double Match4RadianceRate { get; }
        public double Match5RadianceRate { get; }
        public int RadianceDurationTurns { get; }
        public double PassiveLightDamageIncreaseRate { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveBlockConversionCount { get; }
    }

    public static class AnubisCombatRuleResolver
    {
        public static AnubisResolvedCombatRules Resolve(
            AnubisCombatConfig config,
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

            return new AnubisResolvedCombatRules(
                awakening >= 3
                    ? config.Awakening3Match3DamageCoefficient
                    : config.Match3DamageCoefficient,
                awakening >= 3
                    ? config.Awakening3Match4DamageCoefficient
                    : config.Match4DamageCoefficient,
                awakening >= 3
                    ? config.Awakening3Match5DamageCoefficient
                    : config.Match5DamageCoefficient,
                awakening >= 4
                    ? config.Awakening4Match3RadianceRate
                    : config.Match3RadianceRate,
                awakening >= 4
                    ? config.Awakening4Match4RadianceRate
                    : config.Match4RadianceRate,
                awakening >= 4
                    ? config.Awakening4Match5RadianceRate
                    : config.Match5RadianceRate,
                config.RadianceDurationTurns,
                awakening >= 5
                    ? config.Awakening5PassiveLightDamageIncreaseRate
                    : config.PassiveLightDamageIncreaseRate,
                config.ActiveCooldownTurns,
                awakening >= 6
                    ? config.Awakening6ActiveBlockConversionCount
                    : config.ActiveBlockConversionCount);
        }
    }
}

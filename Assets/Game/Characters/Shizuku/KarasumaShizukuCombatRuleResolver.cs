using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Shizuku
{
    public sealed class KarasumaShizukuResolvedCombatRules
    {
        internal KarasumaShizukuResolvedCombatRules(
            double match3DamageCoefficient,
            double match4DamageCoefficient,
            double match5DamageCoefficient,
            double match3AttackIncreaseRate,
            double match4AttackIncreaseRate,
            double match5AttackIncreaseRate,
            int attackBuffDurationTurns,
            double passiveDarkDamageIncreaseRate,
            int activeCooldownTurns,
            int activeBlockConversionCount)
        {
            Match3DamageCoefficient = match3DamageCoefficient;
            Match4DamageCoefficient = match4DamageCoefficient;
            Match5DamageCoefficient = match5DamageCoefficient;
            Match3AttackIncreaseRate = match3AttackIncreaseRate;
            Match4AttackIncreaseRate = match4AttackIncreaseRate;
            Match5AttackIncreaseRate = match5AttackIncreaseRate;
            AttackBuffDurationTurns = attackBuffDurationTurns;
            PassiveDarkDamageIncreaseRate = passiveDarkDamageIncreaseRate;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveBlockConversionCount = activeBlockConversionCount;
        }

        public double Match3DamageCoefficient { get; }
        public double Match4DamageCoefficient { get; }
        public double Match5DamageCoefficient { get; }
        public double Match3AttackIncreaseRate { get; }
        public double Match4AttackIncreaseRate { get; }
        public double Match5AttackIncreaseRate { get; }
        public int AttackBuffDurationTurns { get; }
        public double PassiveDarkDamageIncreaseRate { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveBlockConversionCount { get; }
    }

    public static class KarasumaShizukuCombatRuleResolver
    {
        public static KarasumaShizukuResolvedCombatRules Resolve(
            KarasumaShizukuCombatConfig config,
            int awakening)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!config.TryValidate(out string errorMessage))
                throw new ArgumentException(errorMessage, nameof(config));
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
                throw new ArgumentOutOfRangeException(nameof(awakening), awakening,
                    $"Awakening must be between {CharacterBuildResolver.MinimumAwakening} and {CharacterBuildResolver.MaximumAwakening}.");

            return new KarasumaShizukuResolvedCombatRules(
                awakening >= 4 ? config.Awakening4Match3DamageCoefficient : config.Match3DamageCoefficient,
                awakening >= 4 ? config.Awakening4Match4DamageCoefficient : config.Match4DamageCoefficient,
                awakening >= 5 ? config.Awakening5Match5DamageCoefficient : config.Match5DamageCoefficient,
                awakening >= 4 ? config.Awakening4Match3AttackIncreaseRate : config.Match3AttackIncreaseRate,
                awakening >= 4 ? config.Awakening4Match4AttackIncreaseRate : config.Match4AttackIncreaseRate,
                awakening >= 5 ? config.Awakening5Match5AttackIncreaseRate : config.Match5AttackIncreaseRate,
                config.AttackBuffDurationTurns,
                awakening >= 3 ? config.Awakening3PassiveDarkDamageIncreaseRate : config.PassiveDarkDamageIncreaseRate,
                config.ActiveCooldownTurns,
                awakening >= 6 ? config.Awakening6ActiveBlockConversionCount : config.ActiveBlockConversionCount);
        }
    }
}

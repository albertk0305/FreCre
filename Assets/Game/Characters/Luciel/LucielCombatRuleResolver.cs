using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielResolvedCombatRules
    {
        internal LucielResolvedCombatRules(
            LucielCombatConfig config,
            int awakening)
        {
            PassiveDamageReductionRate = awakening >= 5
                ? config.Awakening5PassiveDamageReductionRate
                : config.PassiveDamageReductionRate;
            Match3ShieldMaxHpRate = awakening >= 3
                ? config.Awakening3Match3ShieldMaxHpRate
                : config.Match3ShieldMaxHpRate;
            Match4ShieldMaxHpRate = awakening >= 3
                ? config.Awakening3Match4ShieldMaxHpRate
                : config.Match4ShieldMaxHpRate;
            Match5ShieldMaxHpRate = awakening >= 4
                ? config.Awakening4Match5ShieldMaxHpRate
                : config.Match5ShieldMaxHpRate;
            Match5PartyDealtDamageIncreaseRate = awakening >= 4
                ? config.Awakening4Match5PartyDealtDamageIncreaseRate
                : config.Match5PartyDealtDamageIncreaseRate;
            Match5BuffDurationTurns = config.Match5BuffDurationTurns;
            Match5BuffStackPolicy = config.Match5BuffStackPolicy;
            ActiveCurrentPartyHpCostRate =
                config.ActiveCurrentPartyHpCostRate;
            ActiveHpCostRoundingMode = config.ActiveHpCostRoundingMode;
            ActiveShieldMaxHpRate = awakening >= 6
                ? config.Awakening6ActiveShieldMaxHpRate
                : config.ActiveShieldMaxHpRate;
            ActiveCooldownTurns = config.ActiveCooldownTurns;
            ActionLocalShieldAmountIncreaseRate = awakening >= 2
                ? config.Awakening2ShieldAmountIncreaseRate
                : 0d;
            MatchShieldUsesComboMultiplier =
                config.MatchShieldUsesComboMultiplier;
            ActiveShieldUsesComboMultiplier =
                config.ActiveShieldUsesComboMultiplier;
            ShieldDurationTurns = config.ShieldDurationTurns;
            ActiveAvailableAtBattleStart =
                config.ActiveAvailableAtBattleStart;
            ActiveHpCostUsesCurrentPartyHp =
                config.ActiveHpCostUsesCurrentPartyHp;
            ActiveMinimumPartyHpAfterCost =
                config.ActiveMinimumPartyHpAfterCost;
            ActiveUsableWhenActualHpCostIsZero =
                config.ActiveUsableWhenActualHpCostIsZero;
            ActiveHpCostIgnoresShields =
                config.ActiveHpCostIgnoresShields;
            ActiveHpCostIgnoresDamageReduction =
                config.ActiveHpCostIgnoresDamageReduction;
        }

        public double PassiveDamageReductionRate { get; }
        public double Match3ShieldMaxHpRate { get; }
        public double Match4ShieldMaxHpRate { get; }
        public double Match5ShieldMaxHpRate { get; }
        public double Match5PartyDealtDamageIncreaseRate { get; }
        public int Match5BuffDurationTurns { get; }
        public EffectStackPolicy Match5BuffStackPolicy { get; }
        public double ActiveCurrentPartyHpCostRate { get; }
        public PartyHpCostRoundingMode ActiveHpCostRoundingMode { get; }
        public double ActiveShieldMaxHpRate { get; }
        public int ActiveCooldownTurns { get; }
        public double ActionLocalShieldAmountIncreaseRate { get; }
        public bool MatchShieldUsesComboMultiplier { get; }
        public bool ActiveShieldUsesComboMultiplier { get; }
        public int? ShieldDurationTurns { get; }
        public bool ActiveAvailableAtBattleStart { get; }
        public bool ActiveHpCostUsesCurrentPartyHp { get; }
        public long ActiveMinimumPartyHpAfterCost { get; }
        public bool ActiveUsableWhenActualHpCostIsZero { get; }
        public bool ActiveHpCostIgnoresShields { get; }
        public bool ActiveHpCostIgnoresDamageReduction { get; }
    }

    public static class LucielCombatRuleResolver
    {
        public static LucielResolvedCombatRules Resolve(
            LucielCombatConfig config,
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

            return new LucielResolvedCombatRules(config, awakening);
        }
    }
}

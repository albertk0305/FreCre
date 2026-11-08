using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Relics.Stats
{
    public sealed class RelicStatBonusSummary
    {
        public RelicStatBonusSummary(
            double maxHpIncreaseRateSum,
            double attackIncreaseRateSum,
            IReadOnlyList<RelicStatBonusSource> sources = null)
            : this(
                maxHpIncreaseRateSum,
                0d,
                attackIncreaseRateSum,
                0d,
                sources)
        {
        }

        public RelicStatBonusSummary(
            double maxHpIncreaseRateSum,
            double maxHpReductionRateSum,
            double attackIncreaseRateSum,
            double attackReductionRateSum,
            IReadOnlyList<RelicStatBonusSource> sources = null)
            : this(
                maxHpIncreaseRateSum,
                maxHpReductionRateSum,
                attackIncreaseRateSum,
                attackReductionRateSum,
                0d,
                0d,
                sources)
        {
        }

        public RelicStatBonusSummary(
            double maxHpIncreaseRateSum,
            double maxHpReductionRateSum,
            double attackIncreaseRateSum,
            double attackReductionRateSum,
            double criticalChanceIncreaseRateSum,
            double criticalDamageIncreaseRateSum,
            IReadOnlyList<RelicStatBonusSource> sources = null)
        {
            ValidateRate(
                maxHpIncreaseRateSum,
                nameof(maxHpIncreaseRateSum));
            ValidateRate(
                maxHpReductionRateSum,
                nameof(maxHpReductionRateSum));
            ValidateRate(
                attackIncreaseRateSum,
                nameof(attackIncreaseRateSum));
            ValidateRate(
                attackReductionRateSum,
                nameof(attackReductionRateSum));
            ValidateRate(
                criticalChanceIncreaseRateSum,
                nameof(criticalChanceIncreaseRateSum));
            ValidateRate(
                criticalDamageIncreaseRateSum,
                nameof(criticalDamageIncreaseRateSum));
            ValidatePositiveMultiplier(
                maxHpIncreaseRateSum,
                maxHpReductionRateSum,
                "maximum HP");
            ValidatePositiveMultiplier(
                attackIncreaseRateSum,
                attackReductionRateSum,
                "attack");
            MaxHpIncreaseRateSum = maxHpIncreaseRateSum;
            MaxHpReductionRateSum = maxHpReductionRateSum;
            AttackIncreaseRateSum = attackIncreaseRateSum;
            AttackReductionRateSum = attackReductionRateSum;
            CriticalChanceIncreaseRateSum =
                criticalChanceIncreaseRateSum;
            CriticalDamageIncreaseRateSum =
                criticalDamageIncreaseRateSum;
            Sources = sources ?? Array.Empty<RelicStatBonusSource>();
        }

        public static RelicStatBonusSummary None { get; } =
            new RelicStatBonusSummary(0d, 0d);

        public double MaxHpIncreaseRateSum { get; }
        public double MaxHpReductionRateSum { get; }
        public double AttackIncreaseRateSum { get; }
        public double AttackReductionRateSum { get; }
        public double CriticalChanceIncreaseRateSum { get; }
        public double CriticalDamageIncreaseRateSum { get; }
        public double MaxHpMultiplier =>
            1d + MaxHpIncreaseRateSum - MaxHpReductionRateSum;
        public double AttackMultiplier =>
            1d + AttackIncreaseRateSum - AttackReductionRateSum;
        public IReadOnlyList<RelicStatBonusSource> Sources { get; }

        private static void ValidateRate(double rate, string parameterName)
        {
            if (double.IsNaN(rate)
                || double.IsInfinity(rate)
                || rate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rate,
                    "A relic stat increase rate must be finite and "
                        + "non-negative.");
            }
        }

        private static void ValidatePositiveMultiplier(
            double increaseRate,
            double reductionRate,
            string statName)
        {
            double multiplier = 1d + increaseRate - reductionRate;
            if (double.IsNaN(multiplier)
                || double.IsInfinity(multiplier)
                || multiplier <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reductionRate),
                    reductionRate,
                    $"The final relic {statName} multiplier must be "
                        + "finite and positive.");
            }
        }
    }

    public sealed class RelicStatBonusSource
    {
        public RelicStatBonusSource(
            string definitionId,
            string displayNameKey,
            string descriptionKey,
            RelicEffectType effectType,
            int awakening,
            double rate)
            : this(
                definitionId,
                displayNameKey,
                descriptionKey,
                effectType,
                awakening,
                rate,
                "rate",
                false,
                default,
                AttackTypeMask.None,
                AttackTag.None,
                AttackTag.None,
                RelicEffectConditionType.None,
                0d)
        {
        }

        public RelicStatBonusSource(
            string definitionId,
            string displayNameKey,
            string descriptionKey,
            RelicEffectType effectType,
            int awakening,
            double rate,
            string descriptionValueKey,
            bool hasElementFilter,
            ElementType elementFilter,
            AttackTypeMask attackTypeMask,
            AttackTag requiredAttackTags,
            AttackTag excludedAttackTags,
            RelicEffectConditionType conditionType,
            double conditionThreshold)
        {
            DefinitionId = definitionId;
            DisplayNameKey = displayNameKey;
            DescriptionKey = descriptionKey;
            EffectType = effectType;
            Awakening = awakening;
            Rate = rate;
            DescriptionValueKey = descriptionValueKey;
            HasElementFilter = hasElementFilter;
            ElementFilter = elementFilter;
            AttackTypeMask = attackTypeMask;
            RequiredAttackTags = requiredAttackTags;
            ExcludedAttackTags = excludedAttackTags;
            ConditionType = conditionType;
            ConditionThreshold = conditionThreshold;
        }

        public string DefinitionId { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public RelicEffectType EffectType { get; }
        public int Awakening { get; }
        public double Rate { get; }
        public string DescriptionValueKey { get; }
        public bool HasElementFilter { get; }
        public ElementType ElementFilter { get; }
        public AttackTypeMask AttackTypeMask { get; }
        public AttackTag RequiredAttackTags { get; }
        public AttackTag ExcludedAttackTags { get; }
        public RelicEffectConditionType ConditionType { get; }
        public double ConditionThreshold { get; }
    }
}

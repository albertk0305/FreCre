using System;
using System.Collections.Generic;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Stats
{
    public sealed class RelicStatBonusResolver
    {
        private readonly DefinitionDatabase definitionDatabase;

        public RelicStatBonusResolver(
            DefinitionDatabase definitionDatabase)
        {
            this.definitionDatabase = definitionDatabase
                ?? throw new ArgumentNullException(nameof(definitionDatabase));
        }

        public RelicStatBonusSummary Resolve(
            ProfileSaveData profile,
            CharacterSaveData character)
        {
            if (!definitionDatabase.IsInitialized
                || profile?.RelicInstances == null
                || character?.EquippedRelicInstanceIds == null)
            {
                return RelicStatBonusSummary.None;
            }

            double maxHpIncreaseRateSum = 0d;
            double maxHpReductionRateSum = 0d;
            double attackIncreaseRateSum = 0d;
            double attackReductionRateSum = 0d;
            double criticalChanceIncreaseRateSum = 0d;
            double criticalDamageIncreaseRateSum = 0d;
            var sources = new List<RelicStatBonusSource>();
            var resolvedInstanceIds = new HashSet<string>(
                StringComparer.Ordinal);
            int slotCount = Math.Min(
                character.EquippedRelicInstanceIds.Count,
                SaveRules.RelicSlotCountPerCharacter);
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                string instanceId =
                    character.EquippedRelicInstanceIds[slotIndex];
                if (string.IsNullOrEmpty(instanceId)
                    || !resolvedInstanceIds.Add(instanceId)
                    || !TryFindUniqueInstance(
                        profile.RelicInstances,
                        instanceId,
                        out RelicInstanceSaveData instance)
                    || instance.Awakening < SaveRules.RelicMinAwakening
                    || instance.Awakening > SaveRules.RelicMaxAwakening
                    || !definitionDatabase.TryGetRelic(
                        instance.RelicDefinitionId,
                        out RelicDefinition definition)
                    || definition == null)
                {
                    continue;
                }

                IReadOnlyList<RelicEffectEntry> effects =
                    definition.Effects;
                for (int effectIndex = 0;
                    effectIndex < effects.Count;
                    effectIndex++)
                {
                    RelicEffectEntry effect = effects[effectIndex];
                    if (effect == null
                        || !Enum.IsDefined(
                            typeof(RelicEffectType),
                            effect.EffectType)
                        || effect.EffectType == RelicEffectType.None
                        || !TryGetValidRate(
                            effect,
                            instance.Awakening,
                            out double rate))
                    {
                        continue;
                    }

                    switch (effect.EffectType)
                    {
                        case RelicEffectType.MaxHpIncrease:
                            maxHpIncreaseRateSum += rate;
                            break;
                        case RelicEffectType.MaxHpReduction:
                            maxHpReductionRateSum += rate;
                            break;
                        case RelicEffectType.AttackIncrease:
                            attackIncreaseRateSum += rate;
                            break;
                        case RelicEffectType.AttackReduction:
                            attackReductionRateSum += rate;
                            break;
                        case RelicEffectType.CriticalChanceIncrease:
                            criticalChanceIncreaseRateSum += rate;
                            break;
                        case RelicEffectType.CriticalDamageIncrease:
                            criticalDamageIncreaseRateSum += rate;
                            break;
                    }

                    sources.Add(new RelicStatBonusSource(
                        definition.Id,
                        definition.DisplayNameKey,
                        definition.DescriptionKey,
                        effect.EffectType,
                        instance.Awakening,
                        rate,
                        effect.DescriptionValueKey,
                        effect.HasElementFilter,
                        effect.ElementFilter,
                        effect.AttackTypeMask,
                        effect.RequiredAttackTags,
                        effect.ExcludedAttackTags,
                        effect.ConditionType,
                        effect.ConditionThreshold));
                }
            }

            if (!IsValidRate(maxHpIncreaseRateSum)
                || !IsValidRate(maxHpReductionRateSum)
                || !IsValidRate(attackIncreaseRateSum)
                || !IsValidRate(attackReductionRateSum)
                || !IsValidRate(criticalChanceIncreaseRateSum)
                || !IsValidRate(criticalDamageIncreaseRateSum)
                || 1d + maxHpIncreaseRateSum
                    - maxHpReductionRateSum <= 0d
                || 1d + attackIncreaseRateSum
                    - attackReductionRateSum <= 0d)
            {
                return RelicStatBonusSummary.None;
            }

            return new RelicStatBonusSummary(
                maxHpIncreaseRateSum,
                maxHpReductionRateSum,
                attackIncreaseRateSum,
                attackReductionRateSum,
                criticalChanceIncreaseRateSum,
                criticalDamageIncreaseRateSum,
                sources.AsReadOnly());
        }

        private static bool TryFindUniqueInstance(
            IReadOnlyList<RelicInstanceSaveData> instances,
            string instanceId,
            out RelicInstanceSaveData match)
        {
            match = null;
            for (int index = 0; index < instances.Count; index++)
            {
                RelicInstanceSaveData candidate = instances[index];
                if (candidate == null
                    || !string.Equals(
                        candidate.InstanceId,
                        instanceId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    match = null;
                    return false;
                }

                match = candidate;
            }

            return match != null;
        }

        private static bool TryGetValidRate(
            RelicEffectEntry effect,
            int awakening,
            out double rate)
        {
            try
            {
                rate = effect.GetEffectRate(awakening);
                return IsValidRate(rate);
            }
            catch (InvalidOperationException)
            {
                rate = 0d;
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                rate = 0d;
                return false;
            }
        }

        private static bool IsValidRate(double rate)
        {
            return !double.IsNaN(rate)
                && !double.IsInfinity(rate)
                && rate >= 0d;
        }
    }
}

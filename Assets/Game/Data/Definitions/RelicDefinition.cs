using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;

namespace ValorChronicle.Data.Definitions
{
    [Serializable]
    public sealed class RelicEffectEntry
    {
        [SerializeField]
        private RelicEffectType effectType;

        [SerializeField]
        private double[] effectRates = Array.Empty<double>();

        [SerializeField]
        private string descriptionValueKey = "rate";

        [SerializeField]
        private bool hasElementFilter;

        [SerializeField]
        private ElementType elementFilter;

        [SerializeField]
        private AttackTypeMask attackTypeMask;

        [SerializeField]
        private AttackTag requiredAttackTags;

        [SerializeField]
        private AttackTag excludedAttackTags;

        [SerializeField]
        private RelicEffectConditionType conditionType;

        [SerializeField]
        private double conditionThreshold;

        public RelicEffectType EffectType => effectType;
        public IReadOnlyList<double> EffectRates =>
            Array.AsReadOnly(effectRates ?? Array.Empty<double>());
        public string DescriptionValueKey => descriptionValueKey;
        public bool HasElementFilter => hasElementFilter;
        public ElementType ElementFilter => elementFilter;
        public AttackTypeMask AttackTypeMask => attackTypeMask;
        public AttackTag RequiredAttackTags => requiredAttackTags;
        public AttackTag ExcludedAttackTags => excludedAttackTags;
        public RelicEffectConditionType ConditionType => conditionType;
        public double ConditionThreshold => conditionThreshold;

        public double GetEffectRate(int awakening)
        {
            if (awakening < 0
                || awakening >= RelicDefinition.AwakeningValueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(awakening));
            }

            if (effectRates == null
                || effectRates.Length
                    != RelicDefinition.AwakeningValueCount)
            {
                throw new InvalidOperationException(
                    "A relic effect must define exactly "
                        + $"{RelicDefinition.AwakeningValueCount} "
                        + "awakening rates.");
            }

            return effectRates[awakening];
        }
    }

    [CreateAssetMenu(
        fileName = "RelicDefinition",
        menuName = "Valor Chronicle/Definitions/Relic")]
    public sealed class RelicDefinition : GameDefinition
    {
        public const int AwakeningValueCount = 6;

        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private RelicEffectEntry[] effects =
            Array.Empty<RelicEffectEntry>();

        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public IReadOnlyList<RelicEffectEntry> Effects =>
            Array.AsReadOnly(effects ?? Array.Empty<RelicEffectEntry>());

        // Compatibility aliases for existing single-effect consumers.
        public RelicEffectType EffectType => FirstEffect?.EffectType
            ?? RelicEffectType.None;
        public IReadOnlyList<double> EffectRates => FirstEffect?.EffectRates
            ?? Array.Empty<double>();

        public double GetEffectRate(int awakening)
        {
            RelicEffectEntry effect = FirstEffect;
            if (effect == null)
            {
                throw new InvalidOperationException(
                    $"Relic '{Id}' must define at least one effect.");
            }

            return effect.GetEffectRate(awakening);
        }

        public double GetEffectRate(int effectIndex, int awakening)
        {
            if (effects == null
                || effectIndex < 0
                || effectIndex >= effects.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(effectIndex));
            }

            RelicEffectEntry effect = effects[effectIndex]
                ?? throw new InvalidOperationException(
                    $"Relic '{Id}' effect {effectIndex} is missing.");
            return effect.GetEffectRate(awakening);
        }

        public IReadOnlyDictionary<string, double>
            GetDescriptionValues(int awakening)
        {
            var values = new Dictionary<string, double>(
                StringComparer.Ordinal);
            IReadOnlyList<RelicEffectEntry> entries = Effects;
            for (int index = 0; index < entries.Count; index++)
            {
                RelicEffectEntry entry = entries[index]
                    ?? throw new InvalidOperationException(
                        $"Relic '{Id}' effect {index} is missing.");
                string key = entry.DescriptionValueKey;
                if (string.IsNullOrWhiteSpace(key)
                    || values.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Relic '{Id}' effect {index} has an invalid or "
                            + "duplicate description value key.");
                }

                values.Add(key, entry.GetEffectRate(awakening));
            }

            return new ReadOnlyDictionary<string, double>(values);
        }

        private RelicEffectEntry FirstEffect =>
            effects != null && effects.Length > 0 ? effects[0] : null;
    }
}

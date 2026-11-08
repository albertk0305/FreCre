using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class CharacterBattleState
    {
        public const int MinimumPartySlotIndex = 0;
        public const int MaximumPartySlotIndex = 4;

        public CharacterBattleState(
            string characterId,
            int partySlotIndex,
            ElementType element,
            long maxHp,
            double attack,
            double permanentAttackIncreaseRate = 0d,
            double permanentAttackReductionRate = 0d,
            IReadOnlyList<RelicStatBonusSource> permanentRelicEffects = null,
            double permanentCriticalChanceIncreaseRate = 0d,
            double permanentCriticalDamageIncreaseRate = 0d)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            if (partySlotIndex < MinimumPartySlotIndex
                || partySlotIndex > MaximumPartySlotIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex),
                    partySlotIndex,
                    $"Party slot index must be between "
                        + $"{MinimumPartySlotIndex} and "
                        + $"{MaximumPartySlotIndex}.");
            }

            ValidateElement(element);
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHp),
                    maxHp,
                    "Maximum HP must be positive.");
            }

            if (double.IsNaN(attack)
                || double.IsInfinity(attack)
                || attack < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attack),
                    attack,
                    "Attack must be finite and non-negative.");
            }

            if (double.IsNaN(permanentAttackIncreaseRate)
                || double.IsInfinity(permanentAttackIncreaseRate)
                || permanentAttackIncreaseRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(permanentAttackIncreaseRate),
                    permanentAttackIncreaseRate,
                    "Permanent attack increase rate must be finite and "
                        + "non-negative.");
            }

            if (double.IsNaN(permanentAttackReductionRate)
                || double.IsInfinity(permanentAttackReductionRate)
                || permanentAttackReductionRate < 0d
                || 1d + permanentAttackIncreaseRate
                    - permanentAttackReductionRate <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(permanentAttackReductionRate),
                    permanentAttackReductionRate,
                    "Permanent attack reduction rate must be finite, "
                        + "non-negative, and leave a positive final "
                        + "attack multiplier.");
            }

            ValidateNonNegativeRate(
                permanentCriticalChanceIncreaseRate,
                nameof(permanentCriticalChanceIncreaseRate));
            ValidateNonNegativeRate(
                permanentCriticalDamageIncreaseRate,
                nameof(permanentCriticalDamageIncreaseRate));

            CharacterId = characterId;
            PartySlotIndex = partySlotIndex;
            Element = element;
            MaxHp = maxHp;
            Attack = attack;
            PermanentAttackIncreaseRate = permanentAttackIncreaseRate;
            PermanentAttackReductionRate = permanentAttackReductionRate;
            PermanentCriticalChanceIncreaseRate =
                permanentCriticalChanceIncreaseRate;
            PermanentCriticalDamageIncreaseRate =
                permanentCriticalDamageIncreaseRate;
            PermanentRelicEffects = permanentRelicEffects
                ?? Array.Empty<RelicStatBonusSource>();
            Effects = new EffectCollection();
        }

        public string CharacterId { get; }
        public int PartySlotIndex { get; }
        public ElementType Element { get; }
        public long MaxHp { get; }
        public double Attack { get; }
        public double PermanentAttackIncreaseRate { get; }
        public double PermanentAttackReductionRate { get; }
        public double PermanentCriticalChanceIncreaseRate { get; }
        public double PermanentCriticalDamageIncreaseRate { get; }
        public IReadOnlyList<RelicStatBonusSource>
            PermanentRelicEffects { get; }
        public EffectCollection Effects { get; }

        private static void ValidateElement(ElementType element)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(element),
                    element,
                    "Element must be a defined value.");
            }
        }

        private static void ValidateNonNegativeRate(
            double rate,
            string parameterName)
        {
            if (double.IsNaN(rate)
                || double.IsInfinity(rate)
                || rate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rate,
                    "Permanent rate must be finite and non-negative.");
            }
        }
    }
}

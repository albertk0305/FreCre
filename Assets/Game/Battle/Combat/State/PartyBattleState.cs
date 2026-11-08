using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class PartyBattleState
    {
        public const int MaximumCharacterCount = 5;

        private readonly ReadOnlyCollection<CharacterBattleState> characters;
        private readonly ReadOnlyCollection<RelicStatBonusSource>
            permanentRelicEffects;

        public PartyBattleState(
            IReadOnlyList<CharacterBattleState> characters)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            if (characters.Count == 0
                || characters.Count > MaximumCharacterCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characters),
                    characters.Count,
                    $"Party must contain between 1 and "
                        + $"{MaximumCharacterCount} characters.");
            }

            var copiedCharacters =
                new CharacterBattleState[characters.Count];
            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            var partySlots = new HashSet<int>();
            long maximumHp = 0;
            for (int index = 0; index < characters.Count; index++)
            {
                CharacterBattleState character = characters[index];
                if (character == null)
                {
                    throw new ArgumentException(
                        "Party cannot contain a null character.",
                        nameof(characters));
                }

                if (!characterIds.Add(character.CharacterId))
                {
                    throw new ArgumentException(
                        $"Duplicate character ID: {character.CharacterId}.",
                        nameof(characters));
                }

                if (!partySlots.Add(character.PartySlotIndex))
                {
                    throw new ArgumentException(
                        $"Duplicate party slot: "
                            + $"{character.PartySlotIndex}.",
                        nameof(characters));
                }

                copiedCharacters[index] = character;
                maximumHp = checked(maximumHp + character.MaxHp);
            }

            Array.Sort(
                copiedCharacters,
                (left, right) => left.PartySlotIndex.CompareTo(
                    right.PartySlotIndex));
            this.characters = Array.AsReadOnly(copiedCharacters);
            var relicEffects = new List<RelicStatBonusSource>();
            double partyDamageReductionRate = 0d;
            for (int characterIndex = 0;
                characterIndex < copiedCharacters.Length;
                characterIndex++)
            {
                IReadOnlyList<RelicStatBonusSource> sources =
                    copiedCharacters[characterIndex].PermanentRelicEffects;
                for (int sourceIndex = 0;
                    sourceIndex < sources.Count;
                    sourceIndex++)
                {
                    RelicStatBonusSource source = sources[sourceIndex];
                    if (source == null)
                    {
                        continue;
                    }

                    relicEffects.Add(source);
                    if (source.EffectType
                        == RelicEffectType.PartyDamageReduction)
                    {
                        partyDamageReductionRate += source.Rate;
                        if (double.IsNaN(partyDamageReductionRate)
                            || double.IsInfinity(
                                partyDamageReductionRate))
                        {
                            throw new OverflowException(
                                "Permanent party damage reduction must "
                                    + "remain finite.");
                        }
                    }
                }
            }

            permanentRelicEffects = relicEffects.AsReadOnly();
            PermanentPartyDamageReductionRate =
                partyDamageReductionRate;
            MaxHp = maximumHp;
            CurrentHp = maximumHp;
            Shields = new ShieldCollection();
            Effects = new EffectCollection();
        }

        public IReadOnlyList<CharacterBattleState> Characters => characters;
        public IReadOnlyList<RelicStatBonusSource>
            PermanentRelicEffects => permanentRelicEffects;
        public double PermanentPartyDamageReductionRate { get; }
        public long MaxHp { get; }
        public long CurrentHp { get; private set; }
        public ShieldCollection Shields { get; }
        public EffectCollection Effects { get; }
        public bool IsIncapacitated => CurrentHp == 0;

        internal long ApplyHpDamage(long damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            long appliedDamage = Math.Min(damage, CurrentHp);
            CurrentHp -= appliedDamage;
            return appliedDamage;
        }

        internal long ApplyHpCost(long requestedCost)
        {
            if (requestedCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedCost));
            }

            long maximumApplicableCost = CurrentHp > 1
                ? CurrentHp - 1
                : 0;
            long appliedCost = Math.Min(
                requestedCost,
                maximumApplicableCost);
            CurrentHp -= appliedCost;
            return appliedCost;
        }

        internal long ApplyHpHealing(long healing)
        {
            if (healing < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healing));
            }

            long missingHp = MaxHp - CurrentHp;
            long appliedHealing = Math.Min(healing, missingHp);
            CurrentHp += appliedHealing;
            return appliedHealing;
        }
    }
}

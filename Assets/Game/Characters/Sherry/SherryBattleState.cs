using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Sherry
{
    public enum SherryMatchTier
    {
        None = 0,
        Three = 1,
        Four = 2,
        FiveOrMore = 3
    }

    public sealed class SherryBattleState : ICharacterCombatStatusSource
    {
        private int stylishRank;

        public SherryBattleState(
            CharacterBattleState owner,
            int stylishMaxRank)
        {
            Owner = owner
                ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                owner.CharacterId,
                SherryRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Sherry battle state requires Sherry as its owner.",
                    nameof(owner));
            }

            if (stylishMaxRank
                != SherryCombatConfig.RequiredStylishMaxRank)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stylishMaxRank),
                    stylishMaxRank,
                    $"Stylish maximum rank must be "
                        + $"{SherryCombatConfig.RequiredStylishMaxRank}.");
            }

            StylishMaxRank = stylishMaxRank;
            PreviousMatchTier = SherryMatchTier.None;
        }

        public CharacterBattleState Owner { get; }
        public int StylishRank => stylishRank;
        public int StylishMaxRank { get; }
        public SherryMatchTier PreviousMatchTier { get; private set; }
        public string StatusId => SherryRules.StylishStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => StylishRank;
        public bool IsVisible => true;

        public bool CompleteMatch(SherryMatchTier matchTier)
        {
            ValidateCompletedMatchTier(matchTier);

            bool shouldGainRank = PreviousMatchTier
                    == SherryMatchTier.None
                || PreviousMatchTier != matchTier;
            bool increased = false;
            if (shouldGainRank && stylishRank < StylishMaxRank)
            {
                stylishRank++;
                increased = true;
            }

            PreviousMatchTier = matchTier;
            return increased;
        }

        public int ConsumeAllRank()
        {
            int consumedRank = stylishRank;
            stylishRank = 0;
            return consumedRank;
        }

        private static void ValidateCompletedMatchTier(
            SherryMatchTier matchTier)
        {
            if (matchTier != SherryMatchTier.Three
                && matchTier != SherryMatchTier.Four
                && matchTier != SherryMatchTier.FiveOrMore)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchTier),
                    matchTier,
                    "A completed Sherry match must be Three, Four, or "
                        + "FiveOrMore.");
            }
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksSharpeningGainResult
    {
        internal HazelSparksSharpeningGainResult(
            int previousSharpening,
            int currentSharpening,
            int requestedGain,
            int overflowGain,
            int currentTurnOverflow,
            int playerTurnNumber)
        {
            PreviousSharpening = previousSharpening;
            CurrentSharpening = currentSharpening;
            RequestedGain = requestedGain;
            OverflowGain = overflowGain;
            CurrentTurnOverflow = currentTurnOverflow;
            PlayerTurnNumber = playerTurnNumber;
        }

        public int PreviousSharpening { get; }
        public int CurrentSharpening { get; }
        public int RequestedGain { get; }
        public int OverflowGain { get; }
        public int CurrentTurnOverflow { get; }
        public int PlayerTurnNumber { get; }
        public bool SharpeningChanged =>
            CurrentSharpening != PreviousSharpening;
        public bool HasOverflow => OverflowGain > 0;
    }

    public sealed class HazelSparksBattleState
        : ICharacterCombatStatusSource
    {
        private int sharpeningStacks;
        private int recordedTurnOverflow;
        private int overflowTurnNumber;

        public HazelSparksBattleState(
            CharacterBattleState owner,
            int maximumSharpeningStacks,
            int initialSharpeningStacks)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                    owner.CharacterId,
                    HazelSparksRules.CharacterId,
                    StringComparison.Ordinal)
                || owner.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Hazel battle state requires the canonical Fire Hazel "
                        + "owner.",
                    nameof(owner));
            }

            if (maximumSharpeningStacks <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSharpeningStacks));
            }

            if (initialSharpeningStacks <= 0
                || initialSharpeningStacks > maximumSharpeningStacks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialSharpeningStacks));
            }

            MaximumSharpeningStacks = maximumSharpeningStacks;
            sharpeningStacks = initialSharpeningStacks;
        }

        public CharacterBattleState Owner { get; }
        public int SharpeningStacks => sharpeningStacks;
        public int MaximumSharpeningStacks { get; }
        public int OverflowTurnNumber => overflowTurnNumber;
        public string StatusId => HazelSparksRules.SharpeningStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => SharpeningStacks;
        public bool IsVisible => true;

        public int GetOverflowForTurn(int playerTurnNumber)
        {
            ValidateTurn(playerTurnNumber);
            return overflowTurnNumber == playerTurnNumber
                ? recordedTurnOverflow
                : 0;
        }

        public HazelSparksSharpeningGainResult GainSharpening(
            int gain,
            int playerTurnNumber)
        {
            if (gain <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gain),
                    gain,
                    "Sharpening gain must be positive.");
            }

            ValidateTurn(playerTurnNumber);
            if (overflowTurnNumber != playerTurnNumber)
            {
                overflowTurnNumber = playerTurnNumber;
                recordedTurnOverflow = 0;
            }

            int previousSharpening = sharpeningStacks;
            long total = (long)previousSharpening + gain;
            sharpeningStacks = (int)Math.Min(
                MaximumSharpeningStacks,
                total);
            int overflowGain = (int)Math.Max(
                0L,
                total - MaximumSharpeningStacks);
            recordedTurnOverflow = checked(
                recordedTurnOverflow + overflowGain);

            return new HazelSparksSharpeningGainResult(
                previousSharpening,
                sharpeningStacks,
                gain,
                overflowGain,
                recordedTurnOverflow,
                playerTurnNumber);
        }

        private static void ValidateTurn(int playerTurnNumber)
        {
            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber),
                    playerTurnNumber,
                    "Player turn number must be positive.");
            }
        }
    }
}

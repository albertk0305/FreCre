using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStoneMutationResult
        : IBattleBoardMutationResult
    {
        private readonly ReadOnlyCollection<BoardFallingStonePlacement>
            placements;

        internal BoardFallingStoneMutationResult(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardFallingStonePlacement> placements,
            int safeCandidateCount)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            if (safeCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(safeCandidateCount));
            }

            var copy = new BoardFallingStonePlacement[placements.Count];
            for (int index = 0; index < copy.Length; index++)
            {
                copy[index] = placements[index]
                    ?? throw new ArgumentException(
                        "FallingStone placements cannot contain null.",
                        nameof(placements));
            }

            RequestedCount = requestedCount;
            SafeCandidateCount = safeCandidateCount;
            this.placements = Array.AsReadOnly(copy);
        }

        public BoardState Board { get; }
        public int RequestedCount { get; }
        public IReadOnlyList<BoardFallingStonePlacement> Placements =>
            placements;
        public int CreatedCount => placements.Count;
        public int SafeCandidateCount { get; }
        public BoardShuffleResult ShuffleResult => null;
        public bool WasShuffled => false;
        public bool Succeeded => true;
    }
}

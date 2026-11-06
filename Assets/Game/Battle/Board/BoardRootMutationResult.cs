using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public enum BoardRootMutationFailure
    {
        None = 0,
        UnableToCreatePlayableBoard = 1
    }

    public sealed class BoardRootMutationResult
        : IBattleBoardMutationResult
    {
        private readonly ReadOnlyCollection<BoardRootPlacement> placements;

        internal BoardRootMutationResult(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardRootPlacement> placements,
            int candidateEvaluationCount,
            BoardShuffleResult shuffleResult,
            BoardRootMutationFailure failure)
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

            if (candidateEvaluationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidateEvaluationCount));
            }

            if (!Enum.IsDefined(typeof(BoardRootMutationFailure), failure))
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }

            var copy = new BoardRootPlacement[placements.Count];
            for (int index = 0; index < copy.Length; index++)
            {
                copy[index] = placements[index]
                    ?? throw new ArgumentException(
                        "Root placements cannot contain null.",
                        nameof(placements));
            }

            RequestedCount = requestedCount;
            CandidateEvaluationCount = candidateEvaluationCount;
            ShuffleResult = shuffleResult;
            Failure = failure;
            this.placements = Array.AsReadOnly(copy);
        }

        public BoardState Board { get; }
        public int RequestedCount { get; }
        public IReadOnlyList<BoardRootPlacement> Placements => placements;
        public int CreatedCount => placements.Count;
        public int CandidateEvaluationCount { get; }
        public BoardShuffleResult ShuffleResult { get; }
        public bool WasShuffled => ShuffleResult?.WasShuffled == true;
        public BoardRootMutationFailure Failure { get; }
        public bool Succeeded => Failure == BoardRootMutationFailure.None;
    }
}

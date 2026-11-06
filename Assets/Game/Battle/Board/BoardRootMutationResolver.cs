using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRootMutationResolver
    {
        private readonly IRandomSource randomSource;
        private readonly BoardBlockIdGenerator idGenerator;
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly BoardShuffler shuffler;

        public BoardRootMutationResolver(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator,
            BoardMoveAnalyzer moveAnalyzer,
            BoardShuffler shuffler)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            this.shuffler = shuffler
                ?? throw new ArgumentNullException(nameof(shuffler));
        }

        public BoardRootMutationResult Resolve(
            BoardState board,
            int requestedCount,
            int maximumRootCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (maximumRootCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumRootCount));
            }

            ValidateStableFullBoard(board);
            if (BoardMatchFinder.FindMatches(board).Count != 0)
            {
                throw new InvalidOperationException(
                    "Root mutation requires a board without completed matches.");
            }

            CollectBoardData(
                board,
                out List<BoardPosition> candidates,
                out HashSet<long> existingRuntimeIds,
                out int currentRootCount);
            if (currentRootCount > maximumRootCount)
            {
                throw new InvalidOperationException(
                    "The board already exceeds the maximum Root count.");
            }

            int targetCount = Math.Min(
                Math.Min(
                    requestedCount,
                    maximumRootCount - currentRootCount),
                candidates.Count);
            if (targetCount == 0)
            {
                return Success(
                    board.Clone(),
                    requestedCount,
                    Array.Empty<BoardRootPlacement>(),
                    evaluationCount: 0,
                    shuffleResult: null);
            }

            ShuffleCandidates(candidates);
            List<IReadOnlyList<BoardPosition>> selections =
                BuildSelections(candidates, targetCount);
            int evaluationCount = 0;

            for (int index = 0; index < selections.Count; index++)
            {
                evaluationCount++;
                BuildCandidate(
                    board,
                    selections[index],
                    existingRuntimeIds,
                    out BoardState candidate,
                    out IReadOnlyList<BoardRootPlacement> placements);
                if (moveAnalyzer.HasAnyValidSwap(candidate))
                {
                    return Success(
                        candidate,
                        requestedCount,
                        placements,
                        evaluationCount,
                        shuffleResult: null);
                }
            }

            for (int index = 0; index < selections.Count; index++)
            {
                evaluationCount++;
                BuildCandidate(
                    board,
                    selections[index],
                    existingRuntimeIds,
                    out BoardState candidate,
                    out IReadOnlyList<BoardRootPlacement> placements);
                try
                {
                    BoardShuffleResult shuffleResult =
                        shuffler.EnsurePlayable(candidate);
                    return Success(
                        shuffleResult.Board,
                        requestedCount,
                        placements,
                        evaluationCount,
                        shuffleResult);
                }
                catch (InvalidOperationException)
                {
                    // Try the next finite Root placement candidate.
                }
            }

            return new BoardRootMutationResult(
                board.Clone(),
                requestedCount,
                Array.Empty<BoardRootPlacement>(),
                evaluationCount,
                shuffleResult: null,
                BoardRootMutationFailure.UnableToCreatePlayableBoard);
        }

        private void ShuffleCandidates(List<BoardPosition> candidates)
        {
            for (int index = candidates.Count - 1; index >= 1; index--)
            {
                int swapIndex = randomSource.Next(0, index + 1);
                BoardPosition temporary = candidates[index];
                candidates[index] = candidates[swapIndex];
                candidates[swapIndex] = temporary;
            }
        }

        private static List<IReadOnlyList<BoardPosition>> BuildSelections(
            IReadOnlyList<BoardPosition> candidates,
            int count)
        {
            var selections = new List<IReadOnlyList<BoardPosition>>();
            if (count == 0)
            {
                selections.Add(Array.Empty<BoardPosition>());
                return selections;
            }

            BuildSelectionsRecursive(
                candidates,
                count,
                startIndex: 0,
                new List<BoardPosition>(count),
                selections);
            return selections;
        }

        private static void BuildSelectionsRecursive(
            IReadOnlyList<BoardPosition> candidates,
            int remaining,
            int startIndex,
            List<BoardPosition> current,
            List<IReadOnlyList<BoardPosition>> selections)
        {
            if (remaining == 0)
            {
                selections.Add(current.ToArray());
                return;
            }

            int finalStart = candidates.Count - remaining;
            for (int index = startIndex; index <= finalStart; index++)
            {
                current.Add(candidates[index]);
                BuildSelectionsRecursive(
                    candidates,
                    remaining - 1,
                    index + 1,
                    current,
                    selections);
                current.RemoveAt(current.Count - 1);
            }
        }

        private void BuildCandidate(
            BoardState board,
            IReadOnlyList<BoardPosition> selected,
            ISet<long> existingRuntimeIds,
            out BoardState candidate,
            out IReadOnlyList<BoardRootPlacement> placements)
        {
            candidate = board.Clone();
            var generatedIds = new HashSet<long>();
            var created = new List<BoardRootPlacement>(selected.Count);
            for (int index = 0; index < selected.Count; index++)
            {
                BoardPosition position = selected[index];
                BoardBlock replaced = board.Get(position);
                long runtimeId = idGenerator.Next();
                if (existingRuntimeIds.Contains(runtimeId)
                    || !generatedIds.Add(runtimeId))
                {
                    throw new InvalidOperationException(
                        "BoardBlockIdGenerator returned an existing runtime ID.");
                }

                var root = new BoardBlock(
                    runtimeId,
                    BoardBlockType.Root,
                    element: null,
                    durability: 2);
                candidate.Set(position, root);
                created.Add(new BoardRootPlacement(
                    position,
                    replaced,
                    root));
            }

            placements = created.AsReadOnly();
        }

        private static void CollectBoardData(
            BoardState board,
            out List<BoardPosition> candidates,
            out HashSet<long> runtimeIds,
            out int rootCount)
        {
            candidates = new List<BoardPosition>();
            runtimeIds = new HashSet<long>();
            rootCount = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = board.Get(position);
                if (!runtimeIds.Add(block.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate board runtime ID {block.RuntimeId}.");
                }

                if (block.BlockType == BoardBlockType.Normal)
                {
                    candidates.Add(position);
                }
                else if (block.BlockType == BoardBlockType.Root)
                {
                    rootCount++;
                }
            }
        }

        private static void ValidateStableFullBoard(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                if (!board.IsOccupied(position))
                {
                    throw new InvalidOperationException(
                        $"Root mutation requires a full board: {position}.");
                }
            }
        }

        private static BoardRootMutationResult Success(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardRootPlacement> placements,
            int evaluationCount,
            BoardShuffleResult shuffleResult)
        {
            return new BoardRootMutationResult(
                board,
                requestedCount,
                placements,
                evaluationCount,
                shuffleResult,
                BoardRootMutationFailure.None);
        }
    }
}

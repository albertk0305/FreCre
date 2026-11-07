using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStoneMutationResolver
    {
        public const int MaximumSupportedFallingStoneCount = 2;

        private readonly IRandomSource randomSource;
        private readonly BoardBlockIdGenerator idGenerator;
        private readonly BoardMoveAnalyzer moveAnalyzer;

        public BoardFallingStoneMutationResolver(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator,
            BoardMoveAnalyzer moveAnalyzer)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
        }

        public BoardFallingStoneMutationResult Resolve(
            BoardState board,
            int requestedCount,
            int maximumCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (requestedCount < 0 || requestedCount > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedCount),
                    requestedCount,
                    "FallingStone mutation can request at most one block.");
            }

            if (maximumCount < 0
                || maximumCount > MaximumSupportedFallingStoneCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCount),
                    maximumCount,
                    "Maximum FallingStone count must be between zero "
                        + "and two.");
            }

            ValidateStableFullBoard(board);
            if (BoardMatchFinder.FindMatches(board).Count != 0)
            {
                throw new InvalidOperationException(
                    "FallingStone mutation requires a board without "
                        + "completed matches.");
            }

            CollectBoardData(
                board,
                out List<BoardPosition> candidates,
                out HashSet<long> runtimeIds,
                out int currentCount);
            if (currentCount > maximumCount)
            {
                throw new InvalidOperationException(
                    "The board already exceeds the maximum FallingStone "
                        + "count.");
            }

            if (requestedCount == 0
                || currentCount >= maximumCount
                || candidates.Count == 0)
            {
                return SuccessNoOp(board, requestedCount, 0);
            }

            var safeCandidates = new List<BoardPosition>();
            for (int index = 0; index < candidates.Count; index++)
            {
                BoardPosition position = candidates[index];
                BoardState candidate = board.Clone();
                BoardBlock replaced = candidate.Get(position);
                candidate.Set(
                    position,
                    new BoardBlock(
                        replaced.RuntimeId,
                        BoardBlockType.FallingStone,
                        element: null));
                if (moveAnalyzer.HasAnyValidSwap(candidate))
                {
                    safeCandidates.Add(position);
                }
            }

            if (safeCandidates.Count == 0)
            {
                return SuccessNoOp(board, requestedCount, 0);
            }

            BoardPosition selected = safeCandidates[randomSource.Next(
                0,
                safeCandidates.Count)];
            long runtimeId = idGenerator.Next();
            if (runtimeIds.Contains(runtimeId))
            {
                throw new InvalidOperationException(
                    "BoardBlockIdGenerator returned an existing runtime ID.");
            }

            BoardState resultBoard = board.Clone();
            BoardBlock replacedBlock = resultBoard.Get(selected);
            var fallingStone = new BoardBlock(
                runtimeId,
                BoardBlockType.FallingStone,
                element: null);
            resultBoard.Set(selected, fallingStone);
            var placement = new BoardFallingStonePlacement(
                selected,
                replacedBlock,
                fallingStone);
            return new BoardFallingStoneMutationResult(
                resultBoard,
                requestedCount,
                new[] { placement },
                safeCandidates.Count);
        }

        private static BoardFallingStoneMutationResult SuccessNoOp(
            BoardState board,
            int requestedCount,
            int safeCandidateCount)
        {
            return new BoardFallingStoneMutationResult(
                board.Clone(),
                requestedCount,
                Array.Empty<BoardFallingStonePlacement>(),
                safeCandidateCount);
        }

        private static void CollectBoardData(
            BoardState board,
            out List<BoardPosition> candidates,
            out HashSet<long> runtimeIds,
            out int fallingStoneCount)
        {
            candidates = new List<BoardPosition>();
            runtimeIds = new HashSet<long>();
            fallingStoneCount = 0;
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate board runtime ID {block.RuntimeId}.");
                    }

                    if (block.BlockType == BoardBlockType.FallingStone)
                    {
                        fallingStoneCount++;
                    }
                    else if (y > 0
                        && block.BlockType == BoardBlockType.Normal)
                    {
                        candidates.Add(position);
                    }
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
                        "FallingStone mutation requires a full board: "
                            + position + ".");
                }
            }
        }
    }
}

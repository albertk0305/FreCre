using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public static class BoardRootImpactResolver
    {
        public static BoardRootImpactResult Resolve(
            BoardState board,
            IReadOnlyList<BoardMatch> matches)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (matches == null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            var changes = new List<BoardBlockStateChange>();
            var destroyedPositions = new List<BoardPosition>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition rootPosition = BoardPosition.FromIndex(index);
                BoardBlock root = board.Get(rootPosition);
                if (root == null || root.BlockType != BoardBlockType.Root)
                {
                    continue;
                }

                int adjacentGroupCount = CountAdjacentGroups(
                    rootPosition,
                    matches);
                if (adjacentGroupCount <= 0)
                {
                    continue;
                }

                int remainingDurability = Math.Max(
                    0,
                    root.Durability - adjacentGroupCount);
                if (remainingDurability == 0)
                {
                    destroyedPositions.Add(rootPosition);
                }
                else
                {
                    changes.Add(new BoardBlockStateChange(
                        rootPosition,
                        root,
                        root.WithRootDurability(remainingDurability)));
                }
            }

            return new BoardRootImpactResult(changes, destroyedPositions);
        }

        internal static int CountAdjacentGroups(
            BoardPosition rootPosition,
            IReadOnlyList<BoardMatch> matches)
        {
            int count = 0;
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                BoardMatch match = matches[matchIndex]
                    ?? throw new InvalidOperationException(
                        $"Match at index {matchIndex} is null.");
                bool isAdjacent = false;
                for (int positionIndex = 0;
                    positionIndex < match.Positions.Count;
                    positionIndex++)
                {
                    BoardPosition matched = match.Positions[positionIndex];
                    int horizontal = Math.Abs(
                        rootPosition.X - matched.X);
                    int vertical = Math.Abs(rootPosition.Y - matched.Y);
                    if (horizontal <= 1
                        && vertical <= 1
                        && horizontal + vertical > 0)
                    {
                        isAdjacent = true;
                        break;
                    }
                }

                if (isAdjacent)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public sealed class BoardRootImpactResult
    {
        private readonly ReadOnlyCollection<BoardBlockStateChange> changes;
        private readonly ReadOnlyCollection<BoardPosition> destroyedPositions;

        internal BoardRootImpactResult(
            IReadOnlyList<BoardBlockStateChange> changes,
            IReadOnlyList<BoardPosition> destroyedPositions)
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (destroyedPositions == null)
            {
                throw new ArgumentNullException(nameof(destroyedPositions));
            }

            var copiedChanges = new BoardBlockStateChange[changes.Count];
            for (int index = 0; index < copiedChanges.Length; index++)
            {
                copiedChanges[index] = changes[index]
                    ?? throw new ArgumentException(
                        "Root changes cannot contain null.",
                        nameof(changes));
            }

            var copiedPositions = new BoardPosition[destroyedPositions.Count];
            for (int index = 0; index < copiedPositions.Length; index++)
            {
                copiedPositions[index] = destroyedPositions[index];
            }

            this.changes = Array.AsReadOnly(copiedChanges);
            this.destroyedPositions = Array.AsReadOnly(copiedPositions);
        }

        public IReadOnlyList<BoardBlockStateChange> Changes => changes;
        public IReadOnlyList<BoardPosition> DestroyedPositions =>
            destroyedPositions;
    }
}

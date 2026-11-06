using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardRootMutationResolverTests
    {
        private static readonly int[] PlayableElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        private static readonly int[] OneMoveElements =
        {
            4, 3, 1, 4, 0,
            4, 1, 0, 4, 0,
            1, 3, 0, 3, 2,
            0, 2, 4, 2, 0,
            3, 3, 1, 4, 1,
            4, 2, 0, 0, 2
        };

        private static readonly int[] DeadBoardElements =
        {
            3, 0, 2, 1, 0,
            0, 2, 3, 1, 2,
            4, 4, 0, 2, 4,
            1, 2, 0, 0, 4,
            0, 1, 2, 1, 2,
            0, 4, 4, 0, 3
        };

        [Test]
        public void ResolveCreatesFullDurabilityRootWithoutMutatingSource()
        {
            BoardState board = CreateBoard(PlayableElements);
            BoardBlock[] original = Capture(board);
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board),
                Is.True);

            BoardRootMutationResult result = CreateResolver(
                new SeededRandomSource(1701),
                firstId: 1000).Resolve(
                    board,
                    requestedCount: 1,
                    maximumRootCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(result.Placements[0].RootBlock.BlockType,
                Is.EqualTo(BoardBlockType.Root));
            Assert.That(result.Placements[0].RootBlock.Durability,
                Is.EqualTo(2));
            Assert.That(CountRoots(result.Board), Is.EqualTo(1));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
            AssertReferencesUnchanged(board, original);
        }

        [Test]
        public void ResolveKeepsMaximumOfTwoRoots()
        {
            BoardState board = CreateBoard(PlayableElements);
            SetRoot(board, new BoardPosition(0, 0), 100, 2);
            SetRoot(board, new BoardPosition(5, 4), 101, 1);
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board),
                Is.True);

            BoardRootMutationResult result = CreateResolver(
                new SeededRandomSource(22),
                firstId: 1000).Resolve(
                    board,
                    requestedCount: 1,
                    maximumRootCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.Zero);
            Assert.That(CountRoots(result.Board), Is.EqualTo(2));
            Assert.That(result.Board.Get(new BoardPosition(0, 0)).Durability,
                Is.EqualTo(2));
            Assert.That(result.Board.Get(new BoardPosition(5, 4)).Durability,
                Is.EqualTo(1));
        }

        [Test]
        public void ResolveChecksOtherCandidatesWhenAPlacementRemovesLastMove()
        {
            BoardState board = CreateBoard(OneMoveElements);
            Assert.That(new BoardMoveAnalyzer().FindValidSwaps(board).Count,
                Is.EqualTo(1));

            BoardRootMutationResult result = CreateResolver(
                new SeededRandomSource(941),
                firstId: 1000).Resolve(
                    board,
                    requestedCount: 1,
                    maximumRootCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(result.CandidateEvaluationCount,
                Is.GreaterThanOrEqualTo(1));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void ShufflerPreservesRootPositionIdentityAndDurability()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var rootPosition = new BoardPosition(5, 4);
            SetRoot(board, rootPosition, 100, 1);
            BoardBlock root = board.Get(rootPosition);
            BoardBlock[] original = Capture(board);
            var moveAnalyzer = new BoardMoveAnalyzer();
            var shuffler = new BoardShuffler(
                new SeededRandomSource(271828),
                moveAnalyzer);

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(moveAnalyzer.HasAnyValidSwap(board), Is.False);

            BoardShuffleResult result = shuffler.EnsurePlayable(board);

            Assert.That(result.WasShuffled, Is.True);
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(moveAnalyzer.HasAnyValidSwap(result.Board), Is.True);
            Assert.That(result.Board.Get(rootPosition), Is.SameAs(root));
            Assert.That(result.Board.Get(rootPosition).RuntimeId,
                Is.EqualTo(100));
            Assert.That(result.Board.Get(rootPosition).Durability,
                Is.EqualTo(1));
            AssertReferencesUnchanged(board, original);
        }

        [Test]
        public void NoNormalCandidateCompletesWithoutCreatingRoot()
        {
            BoardState board = CreateObstacleBoard(normalPosition: null);

            BoardRootMutationResult result = CreateResolver(
                new SeededRandomSource(1),
                firstId: 1000).Resolve(
                    board,
                    requestedCount: 1,
                    maximumRootCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.Zero);
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardsContainSameReferences(result.Board, board);
        }

        [Test]
        public void UnrecoverableCreationFailureLeavesSourceBoardAtomic()
        {
            var normalPosition = new BoardPosition(2, 2);
            BoardState board = CreateObstacleBoard(normalPosition);
            BoardBlock[] original = Capture(board);
            var moveAnalyzer = new BoardMoveAnalyzer();
            var random = new MinimumRandomSource();
            var shuffler = new BoardShuffler(
                random,
                moveAnalyzer,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);
            var resolver = new BoardRootMutationResolver(
                random,
                new BoardBlockIdGenerator(1000),
                moveAnalyzer,
                shuffler);

            BoardRootMutationResult result = resolver.Resolve(
                board,
                requestedCount: 1,
                maximumRootCount: 2);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(
                BoardRootMutationFailure.UnableToCreatePlayableBoard));
            Assert.That(result.CreatedCount, Is.Zero);
            AssertReferencesUnchanged(board, original);
            AssertBoardsContainSameReferences(result.Board, board);
        }

        [Test]
        public void MutationCompletionStoresRootResultThroughCommonContract()
        {
            BoardState board = CreateBoard(PlayableElements);
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board),
                Is.True);
            BoardRootMutationResult result = CreateResolver(
                new SeededRandomSource(73),
                firstId: 1000).Resolve(
                    board,
                    requestedCount: 1,
                    maximumRootCount: 2);
            var command = new BattleBoardMutationCommand(
                BattleBoardMutationKind.CreateRoot,
                requestedCount: 1,
                maximumCount: 2);

            var completion = new BattleBoardMutationCompletion(
                command,
                result,
                BattleBoardMutationCompletionStatus.Completed);

            Assert.That(completion.Succeeded, Is.True);
            Assert.That(completion.MutationResult, Is.SameAs(result));
            Assert.That(completion.Result, Is.Null);
        }

        private static BoardRootMutationResolver CreateResolver(
            IRandomSource random,
            int firstId)
        {
            var moveAnalyzer = new BoardMoveAnalyzer();
            return new BoardRootMutationResolver(
                random,
                new BoardBlockIdGenerator(firstId),
                moveAnalyzer,
                new BoardShuffler(random, moveAnalyzer));
        }

        private static BoardState CreateBoard(int[] elements)
        {
            Assert.That(elements, Is.Not.Null);
            Assert.That(elements.Length,
                Is.EqualTo(BoardConstants.CellCount));
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(
                        position,
                        new BoardBlock(
                            position.ToIndex() + 1,
                            BoardBlockType.Normal,
                            (ElementType)elements[
                                (x * BoardConstants.Height) + y]));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static BoardState CreateObstacleBoard(
            BoardPosition? normalPosition)
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                bool isNormal = normalPosition.HasValue
                    && normalPosition.Value == position;
                board.Set(
                    position,
                    isNormal
                        ? new BoardBlock(
                            index + 1,
                            BoardBlockType.Normal,
                            ElementType.Fire)
                        : new BoardBlock(
                            index + 1,
                            BoardBlockType.Rock,
                            null));
            }

            return board;
        }

        private static void SetRoot(
            BoardState board,
            BoardPosition position,
            long runtimeId,
            int durability)
        {
            board.Set(position, new BoardBlock(
                runtimeId,
                BoardBlockType.Root,
                null,
                durability));
        }

        private static int CountRoots(BoardState board)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                if (board.Get(BoardPosition.FromIndex(index)).BlockType
                    == BoardBlockType.Root)
                {
                    count++;
                }
            }

            return count;
        }

        private static BoardBlock[] Capture(BoardState board)
        {
            var blocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return blocks;
        }

        private static void AssertReferencesUnchanged(
            BoardState board,
            IReadOnlyList<BoardBlock> expected)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                Assert.That(board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(expected[index]));
            }
        }

        private static void AssertBoardsContainSameReferences(
            BoardState actual,
            BoardState expected)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(actual.Get(position),
                    Is.SameAs(expected.Get(position)));
            }
        }

        private sealed class MinimumRandomSource : IRandomSource
        {
            public int Next(int minInclusive, int maxExclusive)
            {
                return minInclusive;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }
    }
}

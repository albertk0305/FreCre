using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardFallingStoneMutationResolverTests
    {
        private static readonly int[] Elements =
        {
            0, 0, 1, 4, 0, 4, 0, 4, 3, 4,
            1, 3, 0, 3, 1, 0, 4, 1, 1, 4,
            0, 0, 2, 4, 4, 1, 2, 4, 0, 0
        };

        [Test]
        public void ResolveCreatesAtMostOneAboveBottomAndPreservesAValidMove()
        {
            BoardFallingStoneMutationResult result = Resolver(17).Resolve(
                Board(), requestedCount: 1, maximumCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(result.Placements[0].Position.Y, Is.GreaterThan(0));
            Assert.That(result.Placements[0].ReplacedBlock.BlockType,
                Is.EqualTo(BoardBlockType.Normal));
            Assert.That(result.Placements[0].FallingStoneBlock.BlockType,
                Is.EqualTo(BoardBlockType.FallingStone));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void ExistingMaximumReturnsSuccessfulNoOp()
        {
            BoardState board = Board();
            AddStone(board, new BoardPosition(0, 1), 100);
            AddStone(board, new BoardPosition(1, 1), 101);

            BoardFallingStoneMutationResult result = Resolver(17).Resolve(
                board, requestedCount: 1, maximumCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.Zero);
            Assert.That(CountStones(result.Board), Is.EqualTo(2));
        }

        [Test]
        public void SameSeedProducesSamePlacement()
        {
            BoardState initialBoard = Board();
            BoardFallingStoneMutationResult first = Resolver(2468).Resolve(
                initialBoard.Clone(), 1, 2);
            BoardFallingStoneMutationResult second = Resolver(2468).Resolve(
                initialBoard.Clone(), 1, 2);

            Assert.That(second.Placements.Select(item => item.Position),
                Is.EqualTo(first.Placements.Select(item => item.Position)));
        }

        [Test]
        public void NoSafeCandidateReturnsSuccessfulNoOpWithoutShuffle()
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                board.Set(BoardPosition.FromIndex(index),
                    new BoardBlock(index + 1, BoardBlockType.Rock, null));
            }
            var candidate = new BoardPosition(2, 2);
            board.Set(candidate,
                new BoardBlock(100, BoardBlockType.Normal, ElementType.Fire));

            BoardFallingStoneMutationResult result = Resolver(1).Resolve(
                board, requestedCount: 1, maximumCount: 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount, Is.Zero);
            Assert.That(result.SafeCandidateCount, Is.Zero);
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(result.Board.Get(candidate).BlockType,
                Is.EqualTo(BoardBlockType.Normal));
        }

        private static BoardFallingStoneMutationResolver Resolver(int seed) =>
            new BoardFallingStoneMutationResolver(
                new SeededRandomSource(seed),
                new BoardBlockIdGenerator(1000),
                new BoardMoveAnalyzer());

        private static BoardState Board()
        {
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(position, new BoardBlock(
                        position.ToIndex() + 1,
                        BoardBlockType.Normal,
                        (ElementType)Elements[
                            (x * BoardConstants.Height) + y]));
                }
            }

            AssertValidMutationInput(board);
            return board;
        }

        private static void AssertValidMutationInput(BoardState board)
        {
            var runtimeIds = new HashSet<long>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));
                Assert.That(block, Is.Not.Null,
                    $"Fixture cell {index} must be occupied.");
                Assert.That(runtimeIds.Add(block.RuntimeId), Is.True,
                    $"Fixture RuntimeId {block.RuntimeId} must be unique.");
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty,
                "FallingStone mutation input must already be stabilized.");
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board), Is.True,
                "FallingStone mutation input must preserve a valid swap.");
        }

        private static void AddStone(BoardState board, BoardPosition position, long id) =>
            board.Set(position, new BoardBlock(id, BoardBlockType.FallingStone, null));

        private static int CountStones(BoardState board)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
                if (board.Get(BoardPosition.FromIndex(index)).BlockType == BoardBlockType.FallingStone) count++;
            return count;
        }
    }
}

using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardFallingStoneSettlementResolverTests
    {
        [Test]
        public void StackedStonesSettleAndBreakInSeparateFinitePhases()
        {
            BoardState board = FullBoard();
            board.Set(new BoardPosition(2, 0), Stone(100));
            board.Set(new BoardPosition(2, 1), Stone(101));

            BoardFallingStoneSettlementResult result =
                new BoardFallingStoneSettlementResolver().Resolve(board);

            Assert.That(result.Phases, Has.Count.EqualTo(2));
            Assert.That(result.Phases[0].Collapse.Removals[0].RuntimeId,
                Is.EqualTo(100));
            Assert.That(result.Phases[1].Collapse.Removals[0].RuntimeId,
                Is.EqualTo(101));
            Assert.That(result.Board.Get(new BoardPosition(2, 3)), Is.Null);
            Assert.That(result.Board.Get(new BoardPosition(2, 4)), Is.Null);
        }

        [Test]
        public void BottomStonesInDifferentColumnsBreakInOnePhase()
        {
            BoardState board = FullBoard();
            board.Set(new BoardPosition(1, 0), Stone(100));
            board.Set(new BoardPosition(4, 0), Stone(101));

            BoardFallingStoneSettlementResult result =
                new BoardFallingStoneSettlementResolver().Resolve(board);

            Assert.That(result.Phases, Has.Count.EqualTo(1));
            Assert.That(result.Phases[0].Collapse.Removals,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void AdjacentMatchDoesNotRemoveFallingStone()
        {
            BoardState board = PatternBoard();
            for (int x = 0; x < 3; x++)
            {
                BoardPosition position = new BoardPosition(x, 3);
                board.Set(position, new BoardBlock(
                    board.Get(position).RuntimeId,
                    BoardBlockType.Normal,
                    ElementType.Fire));
            }
            var stonePosition = new BoardPosition(3, 3);
            BoardBlock stone = Stone(100);
            board.Set(stonePosition, stone);
            var stepResolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new SeededRandomSource(3),
                    new BoardBlockIdGenerator(1000)));

            Assert.That(stepResolver.TryResolve(board, out BoardCascadeStep step),
                Is.True);
            Assert.That(step.Collapse.Removals.Any(
                item => item.RuntimeId == stone.RuntimeId), Is.False);
            Assert.That(step.Board.Get(stonePosition).RuntimeId,
                Is.EqualTo(stone.RuntimeId));
        }

        private static BoardBlock Stone(long id) =>
            new BoardBlock(id, BoardBlockType.FallingStone, null);

        private static BoardState FullBoard()
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
                board.Set(BoardPosition.FromIndex(index),
                    new BoardBlock(index + 1, BoardBlockType.Normal,
                        (ElementType)(index % 5)));
            return board;
        }

        private static BoardState PatternBoard()
        {
            var board = new BoardState();
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(position, new BoardBlock(
                        position.ToIndex() + 1,
                        BoardBlockType.Normal,
                        (ElementType)((x + (2 * y)) % 5)));
                }
            }
            return board;
        }
    }
}

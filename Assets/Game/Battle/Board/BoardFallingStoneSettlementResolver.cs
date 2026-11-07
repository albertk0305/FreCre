using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStoneSettlementResolver
    {
        public const int MaximumSettlementPhases = BoardConstants.CellCount;

        public BoardFallingStoneSettlementResult Resolve(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            BoardState current = board.Clone();
            var phases = new List<BoardFallingStoneSettlementPhase>();
            for (int phaseIndex = 0;
                phaseIndex < MaximumSettlementPhases;
                phaseIndex++)
            {
                List<BoardPosition> bottomStones =
                    CollectBottomFallingStones(current);
                if (bottomStones.Count == 0)
                {
                    return new BoardFallingStoneSettlementResult(
                        current,
                        phases);
                }

                BoardCollapseResult collapse = BoardCollapseResolver.Resolve(
                    current,
                    bottomStones);
                phases.Add(new BoardFallingStoneSettlementPhase(collapse));
                current = collapse.Board;
            }

            throw new InvalidOperationException(
                "FallingStone settlement exceeded its finite phase limit.");
        }

        private static List<BoardPosition> CollectBottomFallingStones(
            BoardState board)
        {
            var positions = new List<BoardPosition>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                var position = new BoardPosition(x, 0);
                BoardBlock block = board.Get(position);
                if (block != null
                    && block.BlockType == BoardBlockType.FallingStone)
                {
                    positions.Add(position);
                }
            }

            return positions;
        }
    }
}

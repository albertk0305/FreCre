using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardFallingStoneSettlementPresentationPhase
    {
        internal BoardFallingStoneSettlementPresentationPhase(
            BoardFallingStoneSettlementPhase phase)
        {
            if (phase == null)
            {
                throw new ArgumentNullException(nameof(phase));
            }

            Removals = phase.Collapse.Removals;
            Moves = phase.Collapse.Moves;
            Board = phase.Board;
        }

        public IReadOnlyList<BoardBlockRemoval> Removals { get; }
        public IReadOnlyList<BoardBlockMove> Moves { get; }
        public BoardState Board { get; }
    }
}

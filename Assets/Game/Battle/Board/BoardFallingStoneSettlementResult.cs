using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStoneSettlementResult
    {
        private readonly ReadOnlyCollection<BoardFallingStoneSettlementPhase>
            phases;

        internal BoardFallingStoneSettlementResult(
            BoardState board,
            IReadOnlyList<BoardFallingStoneSettlementPhase> phases)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (phases == null)
            {
                throw new ArgumentNullException(nameof(phases));
            }

            var copy = new BoardFallingStoneSettlementPhase[phases.Count];
            for (int index = 0; index < copy.Length; index++)
            {
                copy[index] = phases[index]
                    ?? throw new ArgumentException(
                        "Settlement phases cannot contain null.",
                        nameof(phases));
            }

            this.phases = Array.AsReadOnly(copy);
        }

        public BoardState Board { get; }
        public IReadOnlyList<BoardFallingStoneSettlementPhase> Phases =>
            phases;
    }
}

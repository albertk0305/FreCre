using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardCascadeStepPresentationPlan
    {
        private readonly ReadOnlyCollection<BoardBlockRemoval> removals;
        private readonly ReadOnlyCollection<BoardBlockStateChange>
            stateChanges;
        private readonly ReadOnlyCollection<BoardBlockMove> moves;
        private readonly ReadOnlyCollection<BoardBlockSpawn> spawns;
        private readonly ReadOnlyCollection<
            BoardFallingStoneSettlementPresentationPhase> settlementPhases;

        internal BoardCascadeStepPresentationPlan(
            IReadOnlyList<BoardBlockStateChange> stateChanges,
            IReadOnlyList<BoardBlockRemoval> removals,
            IReadOnlyList<BoardBlockMove> moves,
            IReadOnlyList<BoardFallingStoneSettlementPhase> settlementPhases,
            IReadOnlyList<BoardBlockSpawn> spawns,
            BoardState collapseBoard,
            BoardState settledBoard,
            BoardState board)
        {
            this.stateChanges = CopyAsReadOnly(
                stateChanges,
                nameof(stateChanges));
            this.removals = CopyAsReadOnly(removals, nameof(removals));
            this.moves = CopyAsReadOnly(moves, nameof(moves));
            this.spawns = CopyAsReadOnly(spawns, nameof(spawns));
            if (settlementPhases == null)
            {
                throw new ArgumentNullException(nameof(settlementPhases));
            }

            var copiedSettlementPhases =
                new BoardFallingStoneSettlementPresentationPhase[
                    settlementPhases.Count];
            for (int index = 0; index < settlementPhases.Count; index++)
            {
                copiedSettlementPhases[index] =
                    new BoardFallingStoneSettlementPresentationPhase(
                        settlementPhases[index]);
            }

            this.settlementPhases = Array.AsReadOnly(
                copiedSettlementPhases);
            CollapseBoard = collapseBoard
                ?? throw new ArgumentNullException(nameof(collapseBoard));
            SettledBoard = settledBoard
                ?? throw new ArgumentNullException(nameof(settledBoard));
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public IReadOnlyList<BoardBlockStateChange> StateChanges =>
            stateChanges;
        public IReadOnlyList<BoardBlockRemoval> Removals => removals;
        public IReadOnlyList<BoardBlockMove> Moves => moves;
        public IReadOnlyList<BoardFallingStoneSettlementPresentationPhase>
            SettlementPhases => settlementPhases;
        public IReadOnlyList<BoardBlockSpawn> Spawns => spawns;
        public BoardState CollapseBoard { get; }
        public BoardState SettledBoard { get; }
        public BoardState Board { get; }

        private static ReadOnlyCollection<T> CopyAsReadOnly<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index]
                    ?? throw new ArgumentException(
                        "Presentation plan entries cannot contain null.",
                        parameterName);
            }

            return Array.AsReadOnly(copy);
        }
    }
}

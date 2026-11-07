using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCascadeStep
    {
        private readonly ReadOnlyCollection<BoardMatch> matches;
        private readonly ReadOnlyCollection<BoardBlockStateChange>
            stateChanges;
        private readonly ReadOnlyCollection<BoardFallingStoneSettlementPhase>
            settlementPhases;

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            BoardCollapseResult collapse,
            BoardRefillResult refill)
            : this(
                matches,
                Array.Empty<BoardBlockStateChange>(),
                collapse,
                Array.Empty<BoardFallingStoneSettlementPhase>(),
                refill)
        {
        }

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            IReadOnlyList<BoardBlockStateChange> stateChanges,
            BoardCollapseResult collapse,
            BoardRefillResult refill)
            : this(
                matches,
                stateChanges,
                collapse,
                Array.Empty<BoardFallingStoneSettlementPhase>(),
                refill)
        {
        }

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            IReadOnlyList<BoardBlockStateChange> stateChanges,
            BoardCollapseResult collapse,
            IReadOnlyList<BoardFallingStoneSettlementPhase>
                settlementPhases,
            BoardRefillResult refill)
        {
            if (matches == null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            Collapse = collapse
                ?? throw new ArgumentNullException(nameof(collapse));
            Refill = refill
                ?? throw new ArgumentNullException(nameof(refill));
            if (stateChanges == null)
            {
                throw new ArgumentNullException(nameof(stateChanges));
            }

            if (settlementPhases == null)
            {
                throw new ArgumentNullException(nameof(settlementPhases));
            }

            var copiedMatches = new BoardMatch[matches.Count];
            for (int index = 0; index < matches.Count; index++)
            {
                BoardMatch match = matches[index];
                if (match == null)
                {
                    throw new ArgumentException(
                        "Matches cannot contain null.",
                        nameof(matches));
                }

                copiedMatches[index] = match;
            }

            this.matches = Array.AsReadOnly(copiedMatches);

            var copiedChanges = new BoardBlockStateChange[stateChanges.Count];
            for (int index = 0; index < copiedChanges.Length; index++)
            {
                copiedChanges[index] = stateChanges[index]
                    ?? throw new ArgumentException(
                        "State changes cannot contain null.",
                        nameof(stateChanges));
            }

            this.stateChanges = Array.AsReadOnly(copiedChanges);

            var copiedPhases = new BoardFallingStoneSettlementPhase[
                settlementPhases.Count];
            for (int index = 0; index < copiedPhases.Length; index++)
            {
                copiedPhases[index] = settlementPhases[index]
                    ?? throw new ArgumentException(
                        "Settlement phases cannot contain null.",
                        nameof(settlementPhases));
            }

            this.settlementPhases = Array.AsReadOnly(copiedPhases);
        }

        public IReadOnlyList<BoardMatch> Matches => matches;
        public IReadOnlyList<BoardBlockStateChange> StateChanges =>
            stateChanges;
        public BoardCollapseResult Collapse { get; }
        public IReadOnlyList<BoardFallingStoneSettlementPhase>
            SettlementPhases => settlementPhases;
        public BoardRefillResult Refill { get; }
        public BoardState Board => Refill.Board;
        public int MatchEventCount => matches.Count;
        public int RemovedBlockCount
        {
            get
            {
                int count = Collapse.Removals.Count;
                for (int index = 0; index < settlementPhases.Count; index++)
                {
                    count += settlementPhases[index]
                        .Collapse.Removals.Count;
                }

                return count;
            }
        }
    }
}

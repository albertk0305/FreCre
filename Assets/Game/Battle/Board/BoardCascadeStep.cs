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

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            BoardCollapseResult collapse,
            BoardRefillResult refill)
            : this(
                matches,
                Array.Empty<BoardBlockStateChange>(),
                collapse,
                refill)
        {
        }

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            IReadOnlyList<BoardBlockStateChange> stateChanges,
            BoardCollapseResult collapse,
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
        }

        public IReadOnlyList<BoardMatch> Matches => matches;
        public IReadOnlyList<BoardBlockStateChange> StateChanges =>
            stateChanges;
        public BoardCollapseResult Collapse { get; }
        public BoardRefillResult Refill { get; }
        public BoardState Board => Refill.Board;
        public int MatchEventCount => matches.Count;
        public int RemovedBlockCount => Collapse.Removals.Count;
    }
}

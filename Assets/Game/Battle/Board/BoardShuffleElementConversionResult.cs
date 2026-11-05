using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public enum BoardShuffleElementConversionFailure
    {
        None = 0,
        UnableToCreatePlayableShuffle = 1,
        UnableToCreatePlayableFinalBoard = 2
    }

    public sealed class BoardShuffleElementConversionResult
    {
        private readonly ReadOnlyCollection<BoardShuffleEntry> shuffleEntries;
        private readonly ReadOnlyCollection<BoardElementConversionEntry>
            conversionEntries;

        internal BoardShuffleElementConversionResult(
            BoardState board,
            int requestedConversionCount,
            IReadOnlyList<BoardShuffleEntry> shuffleEntries,
            IReadOnlyList<BoardElementConversionEntry> conversionEntries,
            int shuffleAttemptCount,
            int playableShuffleCandidateCount,
            int conversionRandomAttemptCount,
            int conversionDeterministicCombinationCount,
            BoardShuffleElementConversionFailure failure)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (requestedConversionCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedConversionCount));
            }

            if (shuffleAttemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shuffleAttemptCount));
            }

            if (playableShuffleCandidateCount < 0
                || playableShuffleCandidateCount > shuffleAttemptCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playableShuffleCandidateCount));
            }

            if (conversionRandomAttemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(conversionRandomAttemptCount));
            }

            if (conversionDeterministicCombinationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(conversionDeterministicCombinationCount));
            }

            if (!Enum.IsDefined(
                    typeof(BoardShuffleElementConversionFailure),
                    failure))
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }

            this.shuffleEntries = CopyEntries(
                shuffleEntries,
                nameof(shuffleEntries));
            this.conversionEntries = CopyEntries(
                conversionEntries,
                nameof(conversionEntries));
            RequestedConversionCount = requestedConversionCount;
            ShuffleAttemptCount = shuffleAttemptCount;
            PlayableShuffleCandidateCount = playableShuffleCandidateCount;
            ConversionRandomAttemptCount = conversionRandomAttemptCount;
            ConversionDeterministicCombinationCount =
                conversionDeterministicCombinationCount;
            Failure = failure;
        }

        public BoardState Board { get; }
        public int RequestedConversionCount { get; }
        public IReadOnlyList<BoardShuffleEntry> ShuffleEntries =>
            shuffleEntries;
        public IReadOnlyList<BoardElementConversionEntry> ConversionEntries =>
            conversionEntries;
        public int ConvertedCount => conversionEntries.Count;
        public int ShuffleAttemptCount { get; }
        public int PlayableShuffleCandidateCount { get; }
        public int ConversionRandomAttemptCount { get; }
        public int ConversionDeterministicCombinationCount { get; }
        public BoardShuffleElementConversionFailure Failure { get; }
        public bool Succeeded =>
            Failure == BoardShuffleElementConversionFailure.None;

        private static ReadOnlyCollection<T> CopyEntries<T>(
            IReadOnlyList<T> entries,
            string parameterName)
            where T : class
        {
            if (entries == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                copy[index] = entries[index]
                    ?? throw new ArgumentException(
                        "Entries cannot contain null.",
                        parameterName);
            }

            return Array.AsReadOnly(copy);
        }
    }
}

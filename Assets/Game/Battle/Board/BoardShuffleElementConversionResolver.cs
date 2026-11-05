using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardShuffleElementConversionResolver
    {
        public const int DefaultMaxShuffleAttempts = 16;
        public const int DefaultMaxRandomConversionAttempts = 16;
        public const int DefaultMaxDeterministicConversionAttempts = 512;

        private readonly BoardShuffler shuffler;
        private readonly BoardElementConversionResolver conversionResolver;
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly int maxShuffleAttempts;

        public BoardShuffleElementConversionResolver(
            IRandomSource randomSource,
            BoardMoveAnalyzer moveAnalyzer,
            int maxShuffleAttempts = DefaultMaxShuffleAttempts,
            int maxRandomConversionAttempts =
                DefaultMaxRandomConversionAttempts,
            int maxDeterministicConversionAttempts =
                DefaultMaxDeterministicConversionAttempts)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            if (maxShuffleAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxShuffleAttempts));
            }

            this.maxShuffleAttempts = maxShuffleAttempts;
            shuffler = new BoardShuffler(
                randomSource,
                moveAnalyzer,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);
            conversionResolver = new BoardElementConversionResolver(
                randomSource,
                moveAnalyzer,
                maxRandomConversionAttempts,
                maxDeterministicConversionAttempts);
        }

        public BoardShuffleElementConversionResult Resolve(
            BoardState board,
            ElementType targetElement,
            int requestedConversionCount)
        {
            ValidateInputs(
                board,
                targetElement,
                requestedConversionCount);

            int playableShuffleCandidateCount = 0;
            int conversionRandomAttemptCount = 0;
            int conversionDeterministicCombinationCount = 0;
            int shuffleAttemptCount = 0;
            BoardShuffleResult shuffleOnlyFallback = null;
            int initialTargetConversionCount = Math.Min(
                requestedConversionCount,
                CountEligibleBlocks(board, targetElement));
            for (int targetConversionCount = initialTargetConversionCount;
                targetConversionCount >= 0;
                targetConversionCount--)
            {
                if (targetConversionCount == 0
                    && shuffleOnlyFallback != null)
                {
                    return Success(
                        shuffleOnlyFallback,
                        requestedConversionCount,
                        Array.Empty<BoardElementConversionEntry>(),
                        shuffleAttemptCount,
                        playableShuffleCandidateCount,
                        conversionRandomAttemptCount,
                        conversionDeterministicCombinationCount);
                }

                for (int attempt = 0;
                    attempt < maxShuffleAttempts;
                    attempt++)
                {
                    shuffleAttemptCount++;
                    if (!shuffler.TryCreatePlayablePermutation(
                            board,
                            out BoardShuffleResult shuffle))
                    {
                        continue;
                    }

                    playableShuffleCandidateCount++;
                    if (shuffleOnlyFallback == null)
                    {
                        shuffleOnlyFallback = shuffle;
                    }

                    if (targetConversionCount == 0)
                    {
                        return Success(
                            shuffle,
                            requestedConversionCount,
                            Array.Empty<BoardElementConversionEntry>(),
                            shuffleAttemptCount,
                            playableShuffleCandidateCount,
                            conversionRandomAttemptCount,
                            conversionDeterministicCombinationCount);
                    }

                    BoardElementConversionResult conversion =
                        conversionResolver.Resolve(
                            shuffle.Board,
                            targetElement,
                            targetConversionCount);
                    conversionRandomAttemptCount +=
                        conversion.RandomAttemptCount;
                    conversionDeterministicCombinationCount +=
                        conversion.DeterministicCombinationCount;
                    if (!conversion.Succeeded
                        || !moveAnalyzer.HasAnyValidSwap(conversion.Board))
                    {
                        continue;
                    }

                    return new BoardShuffleElementConversionResult(
                        conversion.Board,
                        requestedConversionCount,
                        shuffle.Entries,
                        conversion.Entries,
                        shuffleAttemptCount,
                        playableShuffleCandidateCount,
                        conversionRandomAttemptCount,
                        conversionDeterministicCombinationCount,
                        BoardShuffleElementConversionFailure.None);
                }
            }

            BoardShuffleElementConversionFailure failure =
                playableShuffleCandidateCount == 0
                    ? BoardShuffleElementConversionFailure
                        .UnableToCreatePlayableShuffle
                    : BoardShuffleElementConversionFailure
                        .UnableToCreatePlayableFinalBoard;
            return new BoardShuffleElementConversionResult(
                board.Clone(),
                requestedConversionCount,
                Array.Empty<BoardShuffleEntry>(),
                Array.Empty<BoardElementConversionEntry>(),
                shuffleAttemptCount,
                playableShuffleCandidateCount,
                conversionRandomAttemptCount,
                conversionDeterministicCombinationCount,
                failure);
        }

        private static BoardShuffleElementConversionResult Success(
            BoardShuffleResult shuffle,
            int requestedConversionCount,
            IReadOnlyList<BoardElementConversionEntry> conversionEntries,
            int shuffleAttemptCount,
            int playableShuffleCandidateCount,
            int conversionRandomAttemptCount,
            int conversionDeterministicCombinationCount)
        {
            return new BoardShuffleElementConversionResult(
                shuffle.Board,
                requestedConversionCount,
                shuffle.Entries,
                conversionEntries,
                shuffleAttemptCount,
                playableShuffleCandidateCount,
                conversionRandomAttemptCount,
                conversionDeterministicCombinationCount,
                BoardShuffleElementConversionFailure.None);
        }

        private static int CountEligibleBlocks(
            BoardState board,
            ElementType targetElement)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));
                if (block.BlockType == BoardBlockType.Normal
                    && block.Element.HasValue
                    && block.Element.Value != targetElement)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateInputs(
            BoardState board,
            ElementType targetElement,
            int requestedConversionCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!Enum.IsDefined(typeof(ElementType), targetElement))
            {
                throw new ArgumentOutOfRangeException(nameof(targetElement));
            }

            if (requestedConversionCount < 0
                || requestedConversionCount > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedConversionCount),
                    requestedConversionCount,
                    "Requested conversion count must be between 0 and 5.");
            }

            var runtimeIds = new HashSet<long>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = board.Get(position);
                if (block == null)
                {
                    throw new InvalidOperationException(
                        $"Shuffle conversion requires a full board: {position}.");
                }

                if (!runtimeIds.Add(block.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate board runtime ID {block.RuntimeId}.");
                }
            }
        }
    }
}

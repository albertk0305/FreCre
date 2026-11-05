using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardShuffleElementConversionResolverTests
    {
        private static readonly int[] PlayableElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        [Test]
        public void Resolve_ForcesShuffleEvenWhenInputIsAlreadyPlayable()
        {
            BoardState board = CreateBoard(PlayableElements);
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board), Is.True);

            BoardShuffleElementConversionResult result = Resolve(
                board,
                requestedCount: 0,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ShuffleAttemptCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.ShuffleEntries, Is.Not.Empty);
            Assert.That(HasChangedShuffleEntry(result), Is.True);
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void Resolve_ShufflesOnlyNormalElementsAndPreservesUniqueData()
        {
            BoardState board = CreateBoard(PlayableElements);
            BoardBlock special = ReplaceBlock(
                board,
                5,
                3,
                BoardBlockType.Special,
                ElementType.Dark);
            BoardBlock locked = ReplaceBlock(
                board,
                5,
                4,
                BoardBlockType.Locked,
                ElementType.Water);
            BoardBlock rock = ReplaceBlock(
                board,
                4,
                4,
                BoardBlockType.Rock,
                null);
            long[] runtimeIds = CaptureRuntimeIds(board);

            BoardShuffleElementConversionResult result = Resolve(
                board,
                requestedCount: 3,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Board.Get(new BoardPosition(5, 3)),
                Is.SameAs(special));
            Assert.That(result.Board.Get(new BoardPosition(5, 4)),
                Is.SameAs(locked));
            Assert.That(result.Board.Get(new BoardPosition(4, 4)),
                Is.SameAs(rock));
            AssertRuntimeIdsRemainAtPositions(result.Board, runtimeIds);
            foreach (BoardElementConversionEntry entry in result.ConversionEntries)
            {
                Assert.That(entry.BlockType, Is.EqualTo(BoardBlockType.Normal));
                Assert.That(entry.PreviousElement, Is.Not.EqualTo(ElementType.Fire));
                Assert.That(entry.NewElement, Is.EqualTo(ElementType.Fire));
            }
        }

        [Test]
        public void Resolve_ShufflePreservesElementMultisetWithoutRegeneration()
        {
            BoardShuffleElementConversionResult result = Resolve(
                CreateBoard(PlayableElements),
                requestedCount: 0,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            CollectionAssert.AreEquivalent(
                CollectPreviousElements(result.ShuffleEntries),
                CollectNewElements(result.ShuffleEntries));
        }

        [Test]
        public void Resolve_AcceptsInputThatAlreadyContainsCompletedMatch()
        {
            BoardState board = CreateBoard(PlayableElements);
            SetElement(board, 0, 0, ElementType.Fire);
            SetElement(board, 1, 0, ElementType.Fire);
            SetElement(board, 2, 0, ElementType.Fire);
            Assert.That(BoardMatchFinder.FindMatches(board), Is.Not.Empty);

            BoardShuffleElementConversionResult result = Resolve(
                board,
                requestedCount: 3,
                seed: 7);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ConvertedCount, Is.EqualTo(3));
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Resolve_ConvertsExactlyRequestedFireCountAfterShuffle(
            int requestedCount)
        {
            BoardState board = CreateBoard(PlayableElements);
            int fireBefore = CountNormalElement(board, ElementType.Fire);

            BoardShuffleElementConversionResult result = Resolve(
                board,
                requestedCount,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedConversionCount,
                Is.EqualTo(requestedCount));
            Assert.That(result.ConvertedCount, Is.EqualTo(requestedCount));
            Assert.That(
                CountNormalElement(result.Board, ElementType.Fire),
                Is.EqualTo(fireBefore + requestedCount));
            AssertConversionPositionsAreUnique(result);
        }

        [Test]
        public void Resolve_ConversionMayCreateCompletedMatches()
        {
            BoardShuffleElementConversionResult result = Resolve(
                CreateBoard(PlayableElements),
                requestedCount: 2,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Not.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void Resolve_RetriesConversionPositionsOnSameShuffleCandidate()
        {
            BoardState board = CreateMappedRetryInput();
            var randomValues = CreateForcedMappedPlayablePermutation();
            randomValues.AddRange(new[]
            {
                1, 2, 14, 19,
                0, 1, 2, 3
            });

            BoardShuffleElementConversionResult result = CreateResolver(
                new SequenceThenSeededRandomSource(randomValues, 17),
                maxShuffleAttempts: 8,
                maxRandomConversionAttempts: 2,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ShuffleAttemptCount, Is.EqualTo(1));
            Assert.That(result.PlayableShuffleCandidateCount, Is.EqualTo(1));
            Assert.That(result.ConversionRandomAttemptCount, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_RetriesWithNewShuffleAfterConversionFailure()
        {
            BoardState board = CreateMappedRetryInput();
            var randomValues = CreateForcedMappedPlayablePermutation();
            randomValues.AddRange(new[] { 1, 2, 14, 19 });

            BoardShuffleElementConversionResult result = CreateResolver(
                new SequenceThenSeededRandomSource(randomValues, 17),
                maxShuffleAttempts: 64,
                maxRandomConversionAttempts: 1,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ShuffleAttemptCount, Is.GreaterThan(1));
            Assert.That(result.PlayableShuffleCandidateCount,
                Is.GreaterThan(1));
            Assert.That(result.ConversionRandomAttemptCount,
                Is.GreaterThan(1));
        }

        [Test]
        public void Resolve_ReducesFiveToFourOnlyAfterFullTargetFails()
        {
            BoardState board = CreateFiveCandidateReductionInput();
            int fireBefore = CountNormalElement(board, ElementType.Fire);
            var randomValues = CreateForcedPlayablePermutation(
                normalBlockCount: 8);
            randomValues.AddRange(new[] { 0, 1, 2, 3, 4 });
            randomValues.AddRange(CreateForcedPlayablePermutation(
                normalBlockCount: 8));
            randomValues.AddRange(new[] { 1, 2, 3, 4 });

            BoardShuffleElementConversionResult result = CreateResolver(
                new SequenceThenSeededRandomSource(randomValues, 17),
                maxShuffleAttempts: 1,
                maxRandomConversionAttempts: 1,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    5);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedConversionCount, Is.EqualTo(5));
            Assert.That(result.ConvertedCount, Is.EqualTo(4));
            Assert.That(result.ShuffleAttemptCount, Is.EqualTo(2));
            Assert.That(result.PlayableShuffleCandidateCount, Is.EqualTo(2));
            Assert.That(
                CountNormalElement(result.Board, ElementType.Fire),
                Is.EqualTo(fireBefore + result.ConvertedCount));
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void Resolve_OneCandidateMayReduceToShuffleOnlySuccess()
        {
            BoardState board = CreateSingleCandidateReductionInput();
            int fireBefore = CountNormalElement(board, ElementType.Fire);
            var randomValues = CreateForcedPlayablePermutation(
                normalBlockCount: 4);
            randomValues.Add(0);

            BoardShuffleElementConversionResult result = CreateResolver(
                new SequenceThenSeededRandomSource(randomValues, 17),
                maxShuffleAttempts: 1,
                maxRandomConversionAttempts: 1,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedConversionCount, Is.EqualTo(2));
            Assert.That(result.ConvertedCount, Is.Zero);
            Assert.That(result.ConversionEntries, Is.Empty);
            Assert.That(result.ShuffleAttemptCount, Is.EqualTo(1));
            Assert.That(result.PlayableShuffleCandidateCount, Is.EqualTo(1));
            Assert.That(
                CountNormalElement(result.Board, ElementType.Fire),
                Is.EqualTo(fireBefore));
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void Resolve_AllShuffleCandidatesFailSafelyWithoutRegeneration()
        {
            BoardState board = CreateUniformBoard(ElementType.Fire);
            BoardBlock[] original = CaptureBlocks(board);

            BoardShuffleElementConversionResult result = CreateResolver(
                new SeededRandomSource(11),
                maxShuffleAttempts: 4).Resolve(
                    board,
                    ElementType.Fire,
                    0);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(BoardShuffleElementConversionFailure
                    .UnableToCreatePlayableShuffle));
            Assert.That(result.ConvertedCount, Is.Zero);
            Assert.That(result.ShuffleEntries, Is.Empty);
            AssertBoardReferences(board, original);
            AssertBoardReferences(result.Board, original);
        }

        [Test]
        public void Resolve_AllTargetCountsFailPreservesOriginalBoard()
        {
            BoardState board = CreateMappedRetryInput();
            BoardBlock[] original = CaptureBlocks(board);

            BoardShuffleElementConversionResult result = CreateResolver(
                new MaximumRandomSource(),
                maxShuffleAttempts: 1,
                maxRandomConversionAttempts: 1,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    4);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(BoardShuffleElementConversionFailure
                    .UnableToCreatePlayableShuffle));
            Assert.That(result.ShuffleAttemptCount, Is.EqualTo(5));
            AssertBoardReferences(board, original);
            AssertBoardReferences(result.Board, original);
        }

        [Test]
        public void Resolve_TotalShuffleSearchIsBoundedAcrossTargetCounts()
        {
            const int attemptsPerTarget = 2;
            BoardState board = CreateMappedRetryInput();

            BoardShuffleElementConversionResult result = CreateResolver(
                new MaximumRandomSource(),
                maxShuffleAttempts: attemptsPerTarget,
                maxRandomConversionAttempts: 1,
                maxDeterministicConversionAttempts: 0).Resolve(
                    board,
                    ElementType.Fire,
                    5);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.ShuffleAttemptCount,
                Is.EqualTo((5 + 1) * attemptsPerTarget));
        }

        [Test]
        public void Resolve_SameSeedAndInputProduceSameAtomicResult()
        {
            BoardState board = CreateBoard(PlayableElements);

            BoardShuffleElementConversionResult first = Resolve(
                board,
                requestedCount: 5,
                seed: 20261104);
            BoardShuffleElementConversionResult second = Resolve(
                board,
                requestedCount: 5,
                seed: 20261104);

            Assert.That(second.Succeeded, Is.EqualTo(first.Succeeded));
            Assert.That(second.Failure, Is.EqualTo(first.Failure));
            Assert.That(second.ShuffleAttemptCount,
                Is.EqualTo(first.ShuffleAttemptCount));
            Assert.That(second.ConvertedCount, Is.EqualTo(first.ConvertedCount));
            AssertBoardsEqual(first.Board, second.Board);
        }

        [Test]
        public void Resolve_NeverChangesInputBoardOnSuccess()
        {
            BoardState board = CreateBoard(PlayableElements);
            BoardBlock[] original = CaptureBlocks(board);

            BoardShuffleElementConversionResult result = Resolve(
                board,
                requestedCount: 5,
                seed: 20261104);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardReferences(board, original);
        }

        [Test]
        public void Resolve_RejectsInvalidInputBeforeRandomConsumption()
        {
            var random = new CountingRandomSource();
            BoardShuffleElementConversionResolver resolver =
                CreateResolver(random);
            BoardState empty = new BoardState();
            BoardState duplicateIds = CreateBoard(PlayableElements);
            BoardPosition duplicatePosition = new BoardPosition(0, 1);
            BoardBlock duplicate = duplicateIds.Get(new BoardPosition(0, 0));
            duplicateIds.Set(
                duplicatePosition,
                new BoardBlock(
                    duplicate.RuntimeId,
                    BoardBlockType.Normal,
                    ElementType.Water));

            Assert.Throws<ArgumentNullException>(() =>
                resolver.Resolve(null, ElementType.Fire, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resolver.Resolve(
                    CreateBoard(PlayableElements),
                    (ElementType)999,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resolver.Resolve(CreateBoard(PlayableElements), ElementType.Fire, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resolver.Resolve(CreateBoard(PlayableElements), ElementType.Fire, 6));
            Assert.Throws<InvalidOperationException>(() =>
                resolver.Resolve(empty, ElementType.Fire, 0));
            Assert.Throws<InvalidOperationException>(() =>
                resolver.Resolve(duplicateIds, ElementType.Fire, 0));
            Assert.That(random.CallCount, Is.Zero);
        }

        private static BoardShuffleElementConversionResult Resolve(
            BoardState board,
            int requestedCount,
            int seed)
        {
            return CreateResolver(new SeededRandomSource(seed)).Resolve(
                board,
                ElementType.Fire,
                requestedCount);
        }

        private static BoardShuffleElementConversionResolver CreateResolver(
            IRandomSource randomSource,
            int maxShuffleAttempts =
                BoardShuffleElementConversionResolver.DefaultMaxShuffleAttempts,
            int maxRandomConversionAttempts =
                BoardShuffleElementConversionResolver
                    .DefaultMaxRandomConversionAttempts,
            int maxDeterministicConversionAttempts =
                BoardShuffleElementConversionResolver
                    .DefaultMaxDeterministicConversionAttempts)
        {
            return new BoardShuffleElementConversionResolver(
                randomSource,
                new BoardMoveAnalyzer(),
                maxShuffleAttempts,
                maxRandomConversionAttempts,
                maxDeterministicConversionAttempts);
        }

        private static BoardState CreateBoard(IReadOnlyList<int> elements)
        {
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(
                        position,
                        new BoardBlock(
                            position.ToIndex() + 1,
                            BoardBlockType.Normal,
                            (ElementType)elements[
                                (x * BoardConstants.Height) + y]));
                }
            }

            return board;
        }

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var elements = new int[BoardConstants.CellCount];
            for (int index = 0; index < elements.Length; index++)
            {
                elements[index] = (int)element;
            }

            return CreateBoard(elements);
        }

        private static BoardState CreateMappedRetryInput()
        {
            BoardState board = CreateBoard(PlayableElements);
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = board.Get(position);
                ElementType mapped = block.Element.Value == ElementType.Fire
                    ? ElementType.Water
                    : block.Element.Value == ElementType.Water
                        ? ElementType.Fire
                        : block.Element.Value;
                board.Set(
                    position,
                    new BoardBlock(
                        block.RuntimeId,
                        block.BlockType,
                        mapped));
            }

            SwapElements(
                board,
                new BoardPosition(0, 0),
                new BoardPosition(0, 2));
            return board;
        }

        private static List<int> CreateForcedMappedPlayablePermutation()
        {
            var values = new List<int>();
            for (int index = BoardConstants.CellCount - 1; index >= 3; index--)
            {
                values.Add(index);
            }

            values.Add(0);
            values.Add(1);
            return values;
        }

        private static List<int> CreateForcedPlayablePermutation(
            int normalBlockCount)
        {
            var values = new List<int>();
            for (int index = normalBlockCount - 1; index >= 2; index--)
            {
                values.Add(index);
            }

            values.Add(0);
            return values;
        }

        private static BoardState CreateFiveCandidateReductionInput()
        {
            BoardState board = CreateRockBoard();
            SetNormal(board, 0, 0, ElementType.Fire);
            SetNormal(board, 1, 0, ElementType.Water);
            SetNormal(board, 1, 1, ElementType.Fire);
            SetNormal(board, 2, 0, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);
            SetNormal(board, 5, 0, ElementType.Grass);
            SetNormal(board, 5, 2, ElementType.Light);
            SetNormal(board, 5, 4, ElementType.Dark);
            SwapElements(
                board,
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
            return board;
        }

        private static BoardState CreateSingleCandidateReductionInput()
        {
            BoardState board = CreateRockBoard();
            SetNormal(board, 0, 0, ElementType.Fire);
            SetNormal(board, 1, 0, ElementType.Water);
            SetNormal(board, 1, 1, ElementType.Fire);
            SetNormal(board, 2, 0, ElementType.Fire);
            SwapElements(
                board,
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
            return board;
        }

        private static BoardState CreateRockBoard()
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(
                    position,
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Rock,
                        null));
            }

            return board;
        }

        private static void SetNormal(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            BoardBlock previous = board.Get(position);
            board.Set(
                position,
                new BoardBlock(
                    previous.RuntimeId,
                    BoardBlockType.Normal,
                    element));
        }

        private static BoardBlock ReplaceBlock(
            BoardState board,
            int x,
            int y,
            BoardBlockType blockType,
            ElementType? element)
        {
            var position = new BoardPosition(x, y);
            BoardBlock previous = board.Get(position);
            var replacement = new BoardBlock(
                previous.RuntimeId,
                blockType,
                element);
            board.Set(position, replacement);
            return replacement;
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            BoardBlock previous = board.Get(position);
            board.Set(
                position,
                new BoardBlock(
                    previous.RuntimeId,
                    previous.BlockType,
                    element));
        }

        private static void SwapElements(
            BoardState board,
            BoardPosition first,
            BoardPosition second)
        {
            BoardBlock firstBlock = board.Get(first);
            BoardBlock secondBlock = board.Get(second);
            board.Set(
                first,
                new BoardBlock(
                    firstBlock.RuntimeId,
                    firstBlock.BlockType,
                    secondBlock.Element));
            board.Set(
                second,
                new BoardBlock(
                    secondBlock.RuntimeId,
                    secondBlock.BlockType,
                    firstBlock.Element));
        }

        private static int CountNormalElement(
            BoardState board,
            ElementType element)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));
                if (block.BlockType == BoardBlockType.Normal
                    && block.Element == element)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasChangedShuffleEntry(
            BoardShuffleElementConversionResult result)
        {
            for (int index = 0; index < result.ShuffleEntries.Count; index++)
            {
                if (result.ShuffleEntries[index].Changed)
                {
                    return true;
                }
            }

            return false;
        }

        private static ElementType[] CollectPreviousElements(
            IReadOnlyList<BoardShuffleEntry> entries)
        {
            var elements = new ElementType[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                elements[index] = entries[index].PreviousElement;
            }

            return elements;
        }

        private static ElementType[] CollectNewElements(
            IReadOnlyList<BoardShuffleEntry> entries)
        {
            var elements = new ElementType[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                elements[index] = entries[index].NewElement;
            }

            return elements;
        }

        private static void AssertConversionPositionsAreUnique(
            BoardShuffleElementConversionResult result)
        {
            var positions = new HashSet<BoardPosition>();
            for (int index = 0; index < result.ConversionEntries.Count; index++)
            {
                Assert.That(
                    positions.Add(result.ConversionEntries[index].Position),
                    Is.True);
            }
        }

        private static BoardBlock[] CaptureBlocks(BoardState board)
        {
            var blocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return blocks;
        }

        private static long[] CaptureRuntimeIds(BoardState board)
        {
            var runtimeIds = new long[BoardConstants.CellCount];
            for (int index = 0; index < runtimeIds.Length; index++)
            {
                runtimeIds[index] = board.Get(
                    BoardPosition.FromIndex(index)).RuntimeId;
            }

            return runtimeIds;
        }

        private static void AssertRuntimeIdsRemainAtPositions(
            BoardState board,
            IReadOnlyList<long> expected)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)).RuntimeId,
                    Is.EqualTo(expected[index]));
            }
        }

        private static void AssertBoardReferences(
            BoardState board,
            IReadOnlyList<BoardBlock> expected)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(expected[index]));
            }
        }

        private static void AssertBoardsEqual(
            BoardState first,
            BoardState second)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock firstBlock = first.Get(position);
                BoardBlock secondBlock = second.Get(position);
                Assert.That(secondBlock.RuntimeId,
                    Is.EqualTo(firstBlock.RuntimeId));
                Assert.That(secondBlock.BlockType,
                    Is.EqualTo(firstBlock.BlockType));
                Assert.That(secondBlock.Element,
                    Is.EqualTo(firstBlock.Element));
            }
        }

        private sealed class SequenceThenSeededRandomSource : IRandomSource
        {
            private readonly IReadOnlyList<int> values;
            private readonly SeededRandomSource fallback;
            private int index;

            public SequenceThenSeededRandomSource(
                IReadOnlyList<int> values,
                int fallbackSeed)
            {
                this.values = values
                    ?? throw new ArgumentNullException(nameof(values));
                fallback = new SeededRandomSource(fallbackSeed);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (index >= values.Count)
                {
                    return fallback.Next(minInclusive, maxExclusive);
                }

                int value = values[index++];
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Random value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                return fallback.NextFloat();
            }
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            public int CallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                CallCount++;
                return minInclusive;
            }

            public float NextFloat()
            {
                CallCount++;
                return 0f;
            }
        }

        private sealed class MaximumRandomSource : IRandomSource
        {
            public int Next(int minInclusive, int maxExclusive)
            {
                return maxExclusive - 1;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }
    }
}

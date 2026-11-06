using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardRootCascadeTests
    {
        [Test]
        public void RootModelRequiresDurabilityAndCloneChangesStayIndependent()
        {
            var position = new BoardPosition(2, 2);
            BoardState board = CreateBoardWithoutMatches();
            var root = new BoardBlock(
                100,
                BoardBlockType.Root,
                element: null,
                durability: 2);
            board.Set(position, root);
            BoardState clone = board.Clone();

            clone.Set(position, root.WithRootDurability(1));

            Assert.That(board.Get(position), Is.SameAs(root));
            Assert.That(board.Get(position).Durability, Is.EqualTo(2));
            Assert.That(clone.Get(position).RuntimeId, Is.EqualTo(100));
            Assert.That(clone.Get(position).Durability, Is.EqualTo(1));
            Assert.That(new BoardBlock(
                101,
                BoardBlockType.Normal,
                ElementType.Fire).Durability, Is.Zero);
            Assert.That(new BoardBlock(
                102,
                BoardBlockType.Rock,
                null).Durability, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardBlock(
                103,
                BoardBlockType.Root,
                null,
                durability: 0));
            Assert.Throws<ArgumentException>(() => new BoardBlock(
                104,
                BoardBlockType.Rock,
                null,
                durability: 1));
        }

        [Test]
        public void OneAdjacentGroupDamagesRootOnceIncludingDiagonals()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Fire);
            SetRoot(board, 1, 1, runtimeId: 100, durability: 2);

            BoardCascadeStep step = ResolveStep(board);

            Assert.That(step.StateChanges.Count, Is.EqualTo(1));
            BoardBlockStateChange change = step.StateChanges[0];
            Assert.That(change.RuntimeId, Is.EqualTo(100));
            Assert.That(change.PreviousBlock.Durability, Is.EqualTo(2));
            Assert.That(change.CurrentBlock.Durability, Is.EqualTo(1));
            Assert.That(step.Collapse.Removals.Count(item =>
                item.RuntimeId == 100), Is.Zero);
        }

        [Test]
        public void DurabilityOneRootIsDestroyedAndBoardCollapsesAndRefills()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            SetRoot(board, 1, 1, runtimeId: 100, durability: 1);

            BoardCascadeStep step = ResolveStep(board);

            Assert.That(step.StateChanges, Is.Empty);
            Assert.That(step.Collapse.Removals.Any(item =>
                item.RuntimeId == 100), Is.True);
            Assert.That(FindByRuntimeId(step.Board, 100), Is.Null);
            AssertBoardIsFull(step.Board);
        }

        [Test]
        public void TwoIndependentGroupsDestroyFullDurabilityRoot()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Fire);
            SetHorizontal(board, 3, 5, 2, ElementType.Water);
            SetRoot(board, 2, 1, runtimeId: 100, durability: 2);
            IReadOnlyList<BoardMatch> matches =
                BoardMatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.GreaterThanOrEqualTo(2));

            BoardCascadeStep step = ResolveStep(board);

            Assert.That(step.StateChanges, Is.Empty);
            Assert.That(step.Collapse.Removals.Count(item =>
                item.RuntimeId == 100), Is.EqualTo(1));
        }

        [Test]
        public void ConnectedTGroupDamagesRootOnlyOnce()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Grass);
            SetVertical(board, 1, 1, 3, ElementType.Grass);
            SetRoot(board, 0, 1, runtimeId: 100, durability: 2);

            IReadOnlyList<BoardMatch> matches =
                BoardMatchFinder.FindMatches(board);
            BoardMatch connected = matches.Single(match =>
                match.Element == ElementType.Grass
                && match.Positions.Contains(new BoardPosition(1, 1))
                && match.Positions.Contains(new BoardPosition(2, 2)));
            Assert.That(connected.BlockCount, Is.GreaterThanOrEqualTo(5));
            BoardRootImpactResult impact =
                BoardRootImpactResolver.Resolve(board, matches);

            Assert.That(impact.Changes.Count, Is.EqualTo(1));
            Assert.That(impact.Changes[0].CurrentBlock.Durability,
                Is.EqualTo(1));
            Assert.That(impact.DestroyedPositions, Is.Empty);
        }

        [Test]
        public void SeparatedSameElementGroupsAreCountedIndependently()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetHorizontal(board, 3, 5, 2, ElementType.Dark);
            SetRoot(board, 2, 1, runtimeId: 100, durability: 2);

            IReadOnlyList<BoardMatch> matches =
                BoardMatchFinder.FindMatches(board);
            Assert.That(matches.Count(match =>
                match.Element == ElementType.Dark),
                Is.GreaterThanOrEqualTo(2));
            BoardRootImpactResult impact =
                BoardRootImpactResolver.Resolve(board, matches);

            Assert.That(impact.DestroyedPositions,
                Does.Contain(new BoardPosition(2, 1)));
        }

        [Test]
        public void OneGroupCanDamageMultipleRootsIndependently()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 2, ElementType.Light);
            SetRoot(board, 0, 1, runtimeId: 100, durability: 2);
            SetRoot(board, 4, 1, runtimeId: 101, durability: 2);

            BoardRootImpactResult impact = BoardRootImpactResolver.Resolve(
                board,
                BoardMatchFinder.FindMatches(board));

            Assert.That(impact.Changes.Select(item => item.RuntimeId),
                Is.EquivalentTo(new long[] { 100, 101 }));
            Assert.That(impact.Changes.All(item =>
                item.CurrentBlock.Durability == 1), Is.True);
        }

        [Test]
        public void LaterCascadeStepCanDamageRootAgain()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Light);
            SetRoot(board, 3, 4, runtimeId: 100, durability: 2);
            var stepResolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new PrefixedRandomSource(
                        new[] { 0, 0, 0 },
                        fallbackSeed: 314159),
                    new BoardBlockIdGenerator(1000)));

            BoardCascadeResult cascade =
                new BoardCascadeResolver(stepResolver).Resolve(board);

            Assert.That(cascade.CascadeCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(cascade.Steps.Skip(1).Any(step =>
                step.StateChanges.Any(change =>
                    change.RuntimeId == 100)), Is.True);
        }

        [Test]
        public void RockKeepsOrthogonalOnlyRuleWhileRootUsesDiagonalRule()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Fire);
            SetRock(board, 3, 1, runtimeId: 100);
            SetRoot(board, 3, 3, runtimeId: 101, durability: 2);

            BoardCascadeStep step = ResolveStep(board);

            Assert.That(step.Collapse.Removals.Any(item =>
                item.RuntimeId == 100), Is.False);
            Assert.That(step.StateChanges.Any(item =>
                item.RuntimeId == 101
                && item.CurrentBlock.Durability == 1), Is.True);
        }

        [Test]
        public void SpriteSetUsesDurabilityAndAllowsUnassignedRootSprites()
        {
            var spriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            var texture = new Texture2D(2, 2);
            Sprite intact = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                Vector2.zero);
            Sprite damaged = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                Vector2.zero);
            try
            {
                spriteSet.Configure(
                    null, null, null, null, null, null,
                    intact,
                    damaged);
                Assert.That(spriteSet.GetSprite(Root(100, 2)),
                    Is.SameAs(intact));
                Assert.That(spriteSet.GetSprite(Root(100, 1)),
                    Is.SameAs(damaged));

                spriteSet.Configure(
                    null, null, null, null, null, null,
                    null,
                    null);
                Assert.That(spriteSet.GetSprite(Root(100, 2)), Is.Null);
                Assert.That(spriteSet.GetSprite(Root(100, 1)), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(intact);
                UnityEngine.Object.DestroyImmediate(damaged);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(spriteSet);
            }
        }

        [Test]
        public void PresentationPlanKeepsStateChangeOnTheSameRuntimeId()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Fire);
            SetRoot(board, 1, 1, runtimeId: 100, durability: 2);
            BoardCascadeStep step = ResolveStep(board);

            BoardCascadeStepPresentationPlan plan =
                new BoardCascadeStepPresentationPlanner().Build(board, step);

            Assert.That(plan.StateChanges.Count, Is.EqualTo(1));
            Assert.That(plan.StateChanges[0].PreviousBlock.RuntimeId,
                Is.EqualTo(100));
            Assert.That(plan.StateChanges[0].CurrentBlock.RuntimeId,
                Is.EqualTo(100));
            Assert.That(plan.StateChanges[0].CurrentBlock.Durability,
                Is.EqualTo(1));
        }

        private static BoardCascadeStep ResolveStep(BoardState board)
        {
            var resolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new SeededRandomSource(2718),
                    new BoardBlockIdGenerator(1000)));
            Assert.That(resolver.TryResolve(board, out BoardCascadeStep step),
                Is.True);
            return step;
        }

        private static BoardState CreateBoardWithoutMatches()
        {
            var board = new BoardState();
            long runtimeId = 1;
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            runtimeId++,
                            BoardBlockType.Normal,
                            (ElementType)((x + (2 * y)) % 5)));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static void SetHorizontal(
            BoardState board,
            int minX,
            int maxX,
            int y,
            ElementType element)
        {
            for (int x = minX; x <= maxX; x++)
            {
                SetNormal(board, x, y, element);
            }
        }

        private static void SetVertical(
            BoardState board,
            int x,
            int minY,
            int maxY,
            ElementType element)
        {
            for (int y = minY; y <= maxY; y++)
            {
                SetNormal(board, x, y, element);
            }
        }

        private static void SetNormal(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            BoardPosition position = new BoardPosition(x, y);
            board.Set(position, new BoardBlock(
                board.Get(position).RuntimeId,
                BoardBlockType.Normal,
                element));
        }

        private static void SetRoot(
            BoardState board,
            int x,
            int y,
            long runtimeId,
            int durability)
        {
            board.Set(
                new BoardPosition(x, y),
                Root(runtimeId, durability));
        }

        private static BoardBlock Root(long runtimeId, int durability)
        {
            return new BoardBlock(
                runtimeId,
                BoardBlockType.Root,
                null,
                durability);
        }

        private static void SetRock(
            BoardState board,
            int x,
            int y,
            long runtimeId)
        {
            board.Set(
                new BoardPosition(x, y),
                new BoardBlock(runtimeId, BoardBlockType.Rock, null));
        }

        private static BoardBlock FindByRuntimeId(
            BoardState board,
            long runtimeId)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));
                if (block?.RuntimeId == runtimeId)
                {
                    return block;
                }
            }

            return null;
        }

        private static void AssertBoardIsFull(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(board.Get(BoardPosition.FromIndex(index)),
                    Is.Not.Null);
            }
        }

        private sealed class PrefixedRandomSource : IRandomSource
        {
            private readonly Queue<int> prefix;
            private readonly SeededRandomSource fallback;

            public PrefixedRandomSource(
                IEnumerable<int> prefix,
                int fallbackSeed)
            {
                this.prefix = new Queue<int>(prefix);
                fallback = new SeededRandomSource(fallbackSeed);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (prefix.Count == 0)
                {
                    return fallback.Next(minInclusive, maxExclusive);
                }

                int value = prefix.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        "Prefixed random value is outside the requested range.");
                }

                return value;
            }

            public float NextFloat()
            {
                return fallback.NextFloat();
            }
        }
    }
}

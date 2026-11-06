using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BattleBoardStateSourceTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RootCountUsesOnlyLatestStableBoardSnapshot()
        {
            root = new GameObject("BattleBoardStateSourceTests");
            var controller = root.AddComponent<BattleBoardController>();
            var source = (IBattleBoardStateSource)controller;
            BoardState twoRoots = CreateBoardWithRoots(2);
            SetCurrentBoard(controller, twoRoots);
            CaptureStableSnapshot(controller, notifyObservers: false);

            Assert.That(source.GetRootCount(), Is.EqualTo(2));

            int notificationCount = 0;
            controller.StableBoardChanged += () => notificationCount++;
            SetCurrentBoard(controller, CreateBoardWithRoots(0));

            Assert.That(source.GetRootCount(), Is.EqualTo(2));

            CaptureStableSnapshot(controller, notifyObservers: true);

            Assert.That(source.GetRootCount(), Is.Zero);
            Assert.That(notificationCount, Is.EqualTo(1));
        }

        [Test]
        public void DamagedRootStillCountsAsOneRoot()
        {
            root = new GameObject("BattleBoardStateSourceTests");
            var controller = root.AddComponent<BattleBoardController>();
            BoardState board = CreateBoardWithRoots(0);
            board.Set(
                new BoardPosition(2, 2),
                new BoardBlock(
                    100,
                    BoardBlockType.Root,
                    element: null,
                    durability: 1));
            SetCurrentBoard(controller, board);
            CaptureStableSnapshot(controller, notifyObservers: false);

            Assert.That(
                ((IBattleBoardStateSource)controller).GetRootCount(),
                Is.EqualTo(1));
        }

        private static BoardState CreateBoardWithRoots(int rootCount)
        {
            var board = new BoardState();
            long runtimeId = 1;
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    bool isRoot = runtimeId <= rootCount;
                    board.Set(
                        new BoardPosition(x, y),
                        isRoot
                            ? new BoardBlock(
                                runtimeId,
                                BoardBlockType.Root,
                                element: null,
                                durability: 2)
                            : new BoardBlock(
                                runtimeId,
                                BoardBlockType.Normal,
                                ElementType.Fire));
                    runtimeId++;
                }
            }

            return board;
        }

        private static void SetCurrentBoard(
            BattleBoardController controller,
            BoardState board)
        {
            typeof(BattleBoardController)
                .GetProperty(
                    nameof(BattleBoardController.CurrentBoard),
                    BindingFlags.Instance | BindingFlags.Public)
                .GetSetMethod(nonPublic: true)
                .Invoke(controller, new object[] { board });
        }

        private static void CaptureStableSnapshot(
            BattleBoardController controller,
            bool notifyObservers)
        {
            typeof(BattleBoardController)
                .GetMethod(
                    "CaptureStableBoardSnapshot",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { notifyObservers });
        }
    }
}

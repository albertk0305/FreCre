using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardCascadeStepPresentationPlanner
    {
        public BoardCascadeStepPresentationPlan Build(
            BoardState beforeBoard,
            BoardCascadeStep step)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            Dictionary<long, BoardPosition> beforePositions =
                CollectRuntimeIds(beforeBoard, false, "beforeBoard");
            BoardState boardAfterStateChanges = ValidateStateChanges(
                beforeBoard,
                step);
            ValidateRemovals(beforeBoard, step, out HashSet<long> removedIds);
            ValidateMoves(
                boardAfterStateChanges,
                step.Collapse.Board,
                step.Collapse.Moves,
                removedIds);
            ValidateCollapseBoard(
                boardAfterStateChanges,
                step.Collapse,
                removedIds);
            BoardState settledBoard = ValidateSettlementPhases(
                step.Collapse.Board,
                step.SettlementPhases);
            ValidateSpawns(beforePositions, settledBoard, step);
            ValidateFinalBoard(settledBoard, step);

            return new BoardCascadeStepPresentationPlan(
                step.StateChanges,
                step.Collapse.Removals,
                step.Collapse.Moves,
                step.SettlementPhases,
                step.Refill.Spawns,
                step.Collapse.Board,
                settledBoard,
                step.Board);
        }

        private static BoardState ValidateStateChanges(
            BoardState beforeBoard,
            BoardCascadeStep step)
        {
            BoardRootImpactResult expected =
                BoardRootImpactResolver.Resolve(beforeBoard, step.Matches);
            if (expected.Changes.Count != step.StateChanges.Count)
            {
                throw new InvalidOperationException(
                    "Cascade Root state changes do not match the resolved "
                        + "match groups.");
            }

            BoardState changedBoard = beforeBoard.Clone();
            var positions = new HashSet<BoardPosition>();
            for (int index = 0; index < step.StateChanges.Count; index++)
            {
                BoardBlockStateChange actual = step.StateChanges[index];
                BoardBlockStateChange expectedChange =
                    expected.Changes[index];
                if (!positions.Add(actual.Position)
                    || actual.Position != expectedChange.Position
                    || !BlocksMatch(
                        actual.PreviousBlock,
                        expectedChange.PreviousBlock)
                    || !BlocksMatch(
                        actual.CurrentBlock,
                        expectedChange.CurrentBlock)
                    || !BlocksMatch(
                        beforeBoard.Get(actual.Position),
                        actual.PreviousBlock))
                {
                    throw new InvalidOperationException(
                        "A cascade Root state change is invalid at "
                            + $"{actual.Position}.");
                }

                changedBoard.Set(actual.Position, actual.CurrentBlock);
            }

            return changedBoard;
        }

        private static void ValidateRemovals(
            BoardState beforeBoard,
            BoardCascadeStep step,
            out HashSet<long> removedIds)
        {
            BoardCascadeRemovalValidator.Validate(
                step.Matches,
                step.Collapse.Removals);

            var removalPositions = new HashSet<BoardPosition>();
            removedIds = new HashSet<long>();
            for (int index = 0;
                index < step.Collapse.Removals.Count;
                index++)
            {
                BoardBlockRemoval removal = step.Collapse.Removals[index]
                    ?? throw new InvalidOperationException(
                        "A removal cannot be null.");
                if (!removalPositions.Add(removal.Position))
                {
                    throw new InvalidOperationException(
                        $"Removal position {removal.Position} is duplicated.");
                }

                if (!removedIds.Add(removal.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Removal RuntimeId {removal.RuntimeId} is duplicated.");
                }

                BoardBlock beforeBlock = beforeBoard.Get(removal.Position);
                if (!BlocksMatch(beforeBlock, removal.Block))
                {
                    throw new InvalidOperationException(
                        "A removal must match beforeBoard at " +
                        $"{removal.Position}.");
                }
            }

        }

        private static void ValidateMoves(
            BoardState beforeBoard,
            BoardState collapseBoard,
            IReadOnlyList<BoardBlockMove> moves,
            HashSet<long> removedIds)
        {
            var runtimeIds = new HashSet<long>();
            var fromPositions = new HashSet<BoardPosition>();
            var toPositions = new HashSet<BoardPosition>();

            for (int index = 0; index < moves.Count; index++)
            {
                BoardBlockMove move = moves[index]
                    ?? throw new InvalidOperationException(
                        "A collapse move cannot be null.");
                if (removedIds.Contains(move.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "A removed RuntimeId cannot also move.");
                }

                if (!runtimeIds.Add(move.RuntimeId)
                    || !fromPositions.Add(move.From)
                    || !toPositions.Add(move.To))
                {
                    throw new InvalidOperationException(
                        "Collapse moves cannot duplicate RuntimeIds, From " +
                        "positions, or To positions.");
                }

                if (move.From == move.To
                    || move.From.X != move.To.X
                    || move.To.Y >= move.From.Y)
                {
                    throw new InvalidOperationException(
                        "A collapse move must travel downward in one column.");
                }

                if (!BlocksMatch(beforeBoard.Get(move.From), move.Block))
                {
                    throw new InvalidOperationException(
                        $"A collapse move must match beforeBoard at {move.From}.");
                }

                if (!BlocksMatch(collapseBoard.Get(move.To), move.Block))
                {
                    throw new InvalidOperationException(
                        "A collapse move must match Collapse.Board at " +
                        $"{move.To}.");
                }
            }
        }

        private static void ValidateCollapseBoard(
            BoardState beforeBoard,
            BoardCollapseResult collapse,
            HashSet<long> removedIds)
        {
            var moveTargets = new Dictionary<long, BoardPosition>();
            for (int index = 0; index < collapse.Moves.Count; index++)
            {
                BoardBlockMove move = collapse.Moves[index];
                moveTargets.Add(move.RuntimeId, move.To);
            }

            var expected = new Dictionary<BoardPosition, BoardBlock>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = beforeBoard.Get(position);
                    if (block == null || removedIds.Contains(block.RuntimeId))
                    {
                        continue;
                    }

                    BoardPosition target = moveTargets.TryGetValue(
                        block.RuntimeId,
                        out BoardPosition movedTarget)
                        ? movedTarget
                        : position;
                    if (expected.ContainsKey(target))
                    {
                        throw new InvalidOperationException(
                            $"More than one block collapses to {target}.");
                    }

                    expected.Add(target, block);
                }
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    expected.TryGetValue(position, out BoardBlock block);
                    if (!BlocksMatch(block, collapse.Board.Get(position)))
                    {
                        throw new InvalidOperationException(
                            "Collapse.Board does not match removals and moves " +
                            $"at {position}.");
                    }
                }
            }
        }

        private static BoardState ValidateSettlementPhases(
            BoardState initialBoard,
            IReadOnlyList<BoardFallingStoneSettlementPhase> phases)
        {
            BoardState current = initialBoard;
            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                BoardFallingStoneSettlementPhase phase = phases[phaseIndex]
                    ?? throw new InvalidOperationException(
                        "A FallingStone settlement phase cannot be null.");
                var removedIds = new HashSet<long>();
                var positions = new HashSet<BoardPosition>();
                for (int index = 0;
                    index < phase.Collapse.Removals.Count;
                    index++)
                {
                    BoardBlockRemoval removal =
                        phase.Collapse.Removals[index];
                    BoardBlock source = current.Get(removal.Position);
                    if (removal.Position.Y != 0
                        || removal.Block.BlockType
                            != BoardBlockType.FallingStone
                        || !positions.Add(removal.Position)
                        || !removedIds.Add(removal.RuntimeId)
                        || !BlocksMatch(source, removal.Block))
                    {
                        throw new InvalidOperationException(
                            $"Invalid FallingStone settlement removal in "
                                + $"phase {phaseIndex}.");
                    }
                }

                if (removedIds.Count == 0)
                {
                    throw new InvalidOperationException(
                        "A FallingStone settlement phase must remove at "
                            + "least one bottom-row stone.");
                }

                ValidateMoves(
                    current,
                    phase.Board,
                    phase.Collapse.Moves,
                    removedIds);
                ValidateCollapseBoard(current, phase.Collapse, removedIds);
                current = phase.Board;
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                BoardBlock bottom = current.Get(new BoardPosition(x, 0));
                if (bottom != null
                    && bottom.BlockType == BoardBlockType.FallingStone)
                {
                    throw new InvalidOperationException(
                        "Settlement cannot leave a FallingStone on the "
                            + "bottom row.");
                }
            }

            return current;
        }

        private static void ValidateSpawns(
            Dictionary<long, BoardPosition> beforePositions,
            BoardState settledBoard,
            BoardCascadeStep step)
        {
            var runtimeIds = new HashSet<long>();
            var targets = new HashSet<BoardPosition>();
            for (int index = 0; index < step.Refill.Spawns.Count; index++)
            {
                BoardBlockSpawn spawn = step.Refill.Spawns[index]
                    ?? throw new InvalidOperationException(
                        "A refill spawn cannot be null.");
                if (beforePositions.ContainsKey(spawn.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "A refill spawn must have a new RuntimeId.");
                }

                if (!runtimeIds.Add(spawn.RuntimeId)
                    || !targets.Add(spawn.Target))
                {
                    throw new InvalidOperationException(
                        "Refill spawns cannot duplicate RuntimeIds or targets.");
                }

                if (spawn.SourceY < BoardConstants.Height)
                {
                    throw new InvalidOperationException(
                        "A refill source must be above the board.");
                }

                if (settledBoard.IsOccupied(spawn.Target))
                {
                    throw new InvalidOperationException(
                        $"A refill target {spawn.Target} must be empty.");
                }

                ValidateSupportedBlock(spawn.Block, "refill spawn");
                if (!BlocksMatch(step.Board.Get(spawn.Target), spawn.Block))
                {
                    throw new InvalidOperationException(
                        $"A refill spawn must match Board at {spawn.Target}.");
                }
            }
        }

        private static void ValidateFinalBoard(
            BoardState settledBoard,
            BoardCascadeStep step)
        {
            Dictionary<long, BoardPosition> finalPositions =
                CollectRuntimeIds(step.Board, true, "step.Board");
            if (finalPositions.Count != BoardConstants.CellCount)
            {
                throw new InvalidOperationException(
                    "A cascade step final Board must occupy all 30 cells.");
            }

            var spawnIds = new HashSet<long>();
            for (int index = 0; index < step.Refill.Spawns.Count; index++)
            {
                spawnIds.Add(step.Refill.Spawns[index].RuntimeId);
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock collapseBlock = settledBoard.Get(position);
                    BoardBlock finalBlock = step.Board.Get(position);
                    if (collapseBlock != null
                        && !BlocksMatch(collapseBlock, finalBlock))
                    {
                        throw new InvalidOperationException(
                            "Refill Board changed a surviving block at " +
                            $"{position}.");
                    }

                    if (collapseBlock == null
                        && !spawnIds.Contains(finalBlock.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            "Every empty collapse position must be supplied " +
                            $"by a refill spawn at {position}.");
                    }
                }
            }
        }

        private static Dictionary<long, BoardPosition> CollectRuntimeIds(
            BoardState board,
            bool requireSupported,
            string boardName)
        {
            var positions = new Dictionary<long, BoardPosition>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        continue;
                    }

                    if (requireSupported)
                    {
                        ValidateSupportedBlock(block, boardName);
                    }

                    if (positions.ContainsKey(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"RuntimeId {block.RuntimeId} is duplicated in " +
                            $"{boardName}.");
                    }

                    positions.Add(block.RuntimeId, position);
                }
            }

            return positions;
        }

        private static void ValidateSupportedBlock(
            BoardBlock block,
            string context)
        {
            bool isSupportedNormal = block != null
                && block.BlockType == BoardBlockType.Normal
                && block.Element.HasValue;
            bool isSupportedRock = block != null
                && block.BlockType == BoardBlockType.Rock
                && !block.Element.HasValue;
            bool isSupportedFallingStone = block != null
                && block.BlockType == BoardBlockType.FallingStone
                && !block.Element.HasValue;
            bool isSupportedRoot = block != null
                && block.BlockType == BoardBlockType.Root
                && !block.Element.HasValue
                && block.Durability >= 1
                && block.Durability <= 2;
            if (!isSupportedNormal
                && !isSupportedRock
                && !isSupportedRoot
                && !isSupportedFallingStone)
            {
                throw new NotSupportedException(
                    $"Only supported Normal, Rock, Root, and FallingStone "
                        + "blocks are "
                        + $"allowed in {context}.");
            }
        }

        private static bool BlocksMatch(BoardBlock expected, BoardBlock actual)
        {
            if (expected == null || actual == null)
            {
                return expected == null && actual == null;
            }

            return expected.RuntimeId == actual.RuntimeId
                && expected.BlockType == actual.BlockType
                && expected.Element == actual.Element
                && expected.Durability == actual.Durability;
        }
    }
}

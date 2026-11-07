using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStonePlacement
    {
        internal BoardFallingStonePlacement(
            BoardPosition position,
            BoardBlock replacedBlock,
            BoardBlock fallingStoneBlock)
        {
            ReplacedBlock = replacedBlock
                ?? throw new ArgumentNullException(nameof(replacedBlock));
            FallingStoneBlock = fallingStoneBlock
                ?? throw new ArgumentNullException(
                    nameof(fallingStoneBlock));
            if (position.Y == 0
                || replacedBlock.BlockType != BoardBlockType.Normal
                || fallingStoneBlock.BlockType
                    != BoardBlockType.FallingStone)
            {
                throw new ArgumentException(
                    "A FallingStone placement must replace a non-bottom "
                        + "Normal block.");
            }

            Position = position;
        }

        public BoardPosition Position { get; }
        public BoardBlock ReplacedBlock { get; }
        public BoardBlock FallingStoneBlock { get; }
    }
}

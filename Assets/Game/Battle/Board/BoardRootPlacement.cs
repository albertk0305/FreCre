using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRootPlacement
    {
        internal BoardRootPlacement(
            BoardPosition position,
            BoardBlock replacedBlock,
            BoardBlock rootBlock)
        {
            ReplacedBlock = replacedBlock
                ?? throw new ArgumentNullException(nameof(replacedBlock));
            RootBlock = rootBlock
                ?? throw new ArgumentNullException(nameof(rootBlock));
            if (replacedBlock.BlockType != BoardBlockType.Normal
                || rootBlock.BlockType != BoardBlockType.Root
                || rootBlock.Durability != 2)
            {
                throw new ArgumentException(
                    "A Root placement must replace a Normal block with a "
                        + "full-durability Root.");
            }

            Position = position;
        }

        public BoardPosition Position { get; }
        public BoardBlock ReplacedBlock { get; }
        public BoardBlock RootBlock { get; }
    }
}

using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlockStateChange
    {
        public BoardBlockStateChange(
            BoardPosition position,
            BoardBlock previousBlock,
            BoardBlock currentBlock)
        {
            PreviousBlock = previousBlock
                ?? throw new ArgumentNullException(nameof(previousBlock));
            CurrentBlock = currentBlock
                ?? throw new ArgumentNullException(nameof(currentBlock));
            if (previousBlock.RuntimeId != currentBlock.RuntimeId)
            {
                throw new ArgumentException(
                    "A block state change must preserve RuntimeId.",
                    nameof(currentBlock));
            }

            if (previousBlock.BlockType != BoardBlockType.Root
                || currentBlock.BlockType != BoardBlockType.Root
                || previousBlock.Durability <= currentBlock.Durability)
            {
                throw new ArgumentException(
                    "A Root state change must reduce Root durability.",
                    nameof(currentBlock));
            }

            Position = position;
        }

        public BoardPosition Position { get; }
        public BoardBlock PreviousBlock { get; }
        public BoardBlock CurrentBlock { get; }
        public long RuntimeId => CurrentBlock.RuntimeId;
    }
}

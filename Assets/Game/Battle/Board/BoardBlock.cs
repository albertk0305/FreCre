using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlock
    {
        public BoardBlock(
            long runtimeId,
            BoardBlockType blockType,
            ElementType? element,
            int durability = 0)
        {
            if (blockType == BoardBlockType.Normal && !element.HasValue)
            {
                throw new ArgumentException(
                    "A normal block must have an element.",
                    nameof(element));
            }

            if ((blockType == BoardBlockType.Rock
                    || blockType == BoardBlockType.FallingStone
                    || blockType == BoardBlockType.Root)
                && element.HasValue)
            {
                throw new ArgumentException(
                    $"A {blockType} block cannot have an element.",
                    nameof(element));
            }

            if (blockType == BoardBlockType.Root)
            {
                if (durability < 1 || durability > 2)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(durability),
                        durability,
                        "Root durability must be one or two.");
                }
            }
            else if (durability != 0)
            {
                throw new ArgumentException(
                    "Only Root blocks can have durability.",
                    nameof(durability));
            }

            RuntimeId = runtimeId;
            BlockType = blockType;
            Element = element;
            Durability = durability;
        }

        public long RuntimeId { get; }
        public BoardBlockType BlockType { get; }
        public ElementType? Element { get; }
        public int Durability { get; }

        public BoardBlock WithRootDurability(int durability)
        {
            if (BlockType != BoardBlockType.Root)
            {
                throw new InvalidOperationException(
                    "Only Root blocks can change Root durability.");
            }

            return new BoardBlock(
                RuntimeId,
                BoardBlockType.Root,
                element: null,
                durability);
        }
    }
}

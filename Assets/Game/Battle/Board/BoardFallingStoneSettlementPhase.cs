using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardFallingStoneSettlementPhase
    {
        internal BoardFallingStoneSettlementPhase(
            BoardCollapseResult collapse)
        {
            Collapse = collapse
                ?? throw new ArgumentNullException(nameof(collapse));
            if (collapse.Removals.Count == 0)
            {
                throw new ArgumentException(
                    "A FallingStone settlement phase must remove at least "
                        + "one block.",
                    nameof(collapse));
            }

            for (int index = 0; index < collapse.Removals.Count; index++)
            {
                BoardBlockRemoval removal = collapse.Removals[index];
                if (removal.Position.Y != 0
                    || removal.Block.BlockType
                        != BoardBlockType.FallingStone)
                {
                    throw new ArgumentException(
                        "Settlement phases may remove only bottom-row "
                            + "FallingStone blocks.",
                        nameof(collapse));
                }
            }
        }

        public BoardCollapseResult Collapse { get; }
        public BoardState Board => Collapse.Board;
    }
}

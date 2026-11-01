namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class PartyHpCostApplicationResult
    {
        internal PartyHpCostApplicationResult(
            long requestedHpCost,
            long appliedHpCost,
            long hpBefore,
            long hpAfter)
        {
            RequestedHpCost = requestedHpCost;
            AppliedHpCost = appliedHpCost;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
        }

        public long RequestedHpCost { get; }
        public long AppliedHpCost { get; }
        public long HpBefore { get; }
        public long HpAfter { get; }
    }
}

namespace ValorChronicle.Battle.Combat.Effects
{
    public sealed class EffectStackConsumeResult
    {
        public EffectStackConsumeResult(
            long runtimeId,
            bool effectFound,
            int requestedAmount,
            int consumedAmount,
            int stackCountBefore,
            int stackCountAfter,
            bool wasRemoved)
        {
            RuntimeId = runtimeId;
            EffectFound = effectFound;
            RequestedAmount = requestedAmount;
            ConsumedAmount = consumedAmount;
            StackCountBefore = stackCountBefore;
            StackCountAfter = stackCountAfter;
            WasRemoved = wasRemoved;
        }

        public long RuntimeId { get; }
        public bool EffectFound { get; }
        public int RequestedAmount { get; }
        public int ConsumedAmount { get; }
        public int StackCountBefore { get; }
        public int StackCountAfter { get; }
        public bool WasRemoved { get; }
    }
}

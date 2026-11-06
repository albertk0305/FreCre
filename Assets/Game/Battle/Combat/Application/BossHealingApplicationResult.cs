namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class BossHealingApplicationResult
    {
        internal BossHealingApplicationResult(
            double maxHpHealingCoefficient,
            long requestedHealing,
            long appliedHealing,
            long hpBefore,
            long hpAfter)
        {
            MaxHpHealingCoefficient = maxHpHealingCoefficient;
            RequestedHealing = requestedHealing;
            AppliedHealing = appliedHealing;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
        }

        public double MaxHpHealingCoefficient { get; }
        public long RequestedHealing { get; }
        public long AppliedHealing { get; }
        public long HpBefore { get; }
        public long HpAfter { get; }
        public long OverhealAmount => RequestedHealing - AppliedHealing;
    }
}

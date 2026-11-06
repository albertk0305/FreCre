using System;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleDamageScoreAccumulator
    {
        public long CumulativeAppliedDamage { get; private set; }
        public long CumulativeAppliedBossHealing { get; private set; }
        public long DamageScore =>
            CumulativeAppliedDamage <= CumulativeAppliedBossHealing
                ? 0L
                : CumulativeAppliedDamage
                    - CumulativeAppliedBossHealing;

        public void Add(long appliedDamage)
        {
            Apply(appliedDamage, 0L);
        }

        public void AddBossHealing(long appliedBossHealing)
        {
            Apply(0L, appliedBossHealing);
        }

        public void Apply(
            long appliedDamage,
            long appliedBossHealing)
        {
            if (appliedDamage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(appliedDamage),
                    appliedDamage,
                    "Applied damage cannot be negative.");
            }

            if (appliedBossHealing < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(appliedBossHealing),
                    appliedBossHealing,
                    "Applied boss healing cannot be negative.");
            }

            long cumulativeDamage = checked(
                CumulativeAppliedDamage + appliedDamage);
            long cumulativeHealing = checked(
                CumulativeAppliedBossHealing + appliedBossHealing);
            CumulativeAppliedDamage = cumulativeDamage;
            CumulativeAppliedBossHealing = cumulativeHealing;
        }
    }
}

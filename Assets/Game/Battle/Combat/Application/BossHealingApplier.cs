using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class BossHealingApplier
    {
        private const double Int64UpperExclusive =
            9223372036854775808d;

        public static BossHealingApplicationResult Apply(
            BossBattleState boss,
            double maxHpHealingCoefficient)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (double.IsNaN(maxHpHealingCoefficient)
                || double.IsInfinity(maxHpHealingCoefficient)
                || maxHpHealingCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHpHealingCoefficient));
            }

            double rawHealing = boss.MaxHp * maxHpHealingCoefficient;
            if (double.IsInfinity(rawHealing)
                || rawHealing >= Int64UpperExclusive)
            {
                throw new OverflowException(
                    "Calculated boss healing exceeds Int64 capacity.");
            }

            long requestedHealing = (long)Math.Floor(rawHealing);
            long hpBefore = boss.CurrentHp;
            long appliedHealing = boss.IsDefeated
                ? 0L
                : boss.ApplyHealing(requestedHealing);
            return new BossHealingApplicationResult(
                maxHpHealingCoefficient,
                requestedHealing,
                appliedHealing,
                hpBefore,
                boss.CurrentHp);
        }
    }
}

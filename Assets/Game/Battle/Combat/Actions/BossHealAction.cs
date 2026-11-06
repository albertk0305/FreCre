using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class BossHealAction : CombatAction
    {
        public BossHealAction(
            long actionId,
            BossBattleState targetBoss,
            double maxHpHealingCoefficient,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(
                actionId,
                ActionOrigin.System,
                rootActionId,
                sourceActionId)
        {
            TargetBoss = targetBoss
                ?? throw new ArgumentNullException(nameof(targetBoss));
            if (double.IsNaN(maxHpHealingCoefficient)
                || double.IsInfinity(maxHpHealingCoefficient)
                || maxHpHealingCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHpHealingCoefficient),
                    maxHpHealingCoefficient,
                    "Boss healing coefficient must be finite and "
                        + "non-negative.");
            }

            MaxHpHealingCoefficient = maxHpHealingCoefficient;
        }

        public BossBattleState TargetBoss { get; }
        public double MaxHpHealingCoefficient { get; }
    }
}

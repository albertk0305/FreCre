using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class PartyHpCostAction : CombatAction
    {
        public PartyHpCostAction(
            long actionId,
            ActionOrigin origin,
            PartyBattleState targetParty,
            long referenceMaxHp,
            double costRate,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            TargetParty = targetParty
                ?? throw new ArgumentNullException(nameof(targetParty));
            if (referenceMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(referenceMaxHp),
                    referenceMaxHp,
                    "Reference maximum HP must be positive.");
            }

            if (double.IsNaN(costRate)
                || double.IsInfinity(costRate)
                || costRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(costRate),
                    costRate,
                    "HP cost rate must be finite and non-negative.");
            }

            ReferenceMaxHp = referenceMaxHp;
            CostRate = costRate;
        }

        public PartyBattleState TargetParty { get; }
        public long ReferenceMaxHp { get; }
        public double CostRate { get; }
    }
}

using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class PartyHpCostApplier
    {
        public static PartyHpCostApplicationResult Apply(
            PartyBattleState party,
            long referenceMaxHp,
            double costRate)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (referenceMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(referenceMaxHp));
            }

            if (double.IsNaN(costRate)
                || double.IsInfinity(costRate)
                || costRate < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(costRate));
            }

            double rawCost = referenceMaxHp * costRate;
            long requestedHpCost = rawCost >= long.MaxValue
                ? long.MaxValue
                : (long)Math.Floor(rawCost);
            long hpBefore = party.CurrentHp;
            long appliedHpCost = party.ApplyHpCost(requestedHpCost);
            return new PartyHpCostApplicationResult(
                requestedHpCost,
                appliedHpCost,
                hpBefore,
                party.CurrentHp);
        }
    }
}

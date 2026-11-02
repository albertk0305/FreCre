using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Application
{
    public static class PartyHpCostApplier
    {
        public static PartyHpCostApplicationResult Apply(
            PartyBattleState party,
            long referenceMaxHp,
            double costRate,
            PartyHpCostRoundingMode roundingMode =
                PartyHpCostRoundingMode.Floor)
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

            if (!Enum.IsDefined(
                    typeof(PartyHpCostRoundingMode),
                    roundingMode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roundingMode),
                    roundingMode,
                    "HP cost rounding mode must be defined.");
            }

            double rawCost = referenceMaxHp * costRate;
            long requestedHpCost = ResolveRequestedCost(
                rawCost,
                roundingMode);
            long hpBefore = party.CurrentHp;
            long appliedHpCost = party.ApplyHpCost(requestedHpCost);
            return new PartyHpCostApplicationResult(
                requestedHpCost,
                appliedHpCost,
                hpBefore,
                party.CurrentHp);
        }

        private static long ResolveRequestedCost(
            double rawCost,
            PartyHpCostRoundingMode roundingMode)
        {
            if (double.IsNaN(rawCost) || rawCost < 0d)
            {
                throw new OverflowException(
                    "Calculated HP cost must be non-negative and not NaN.");
            }

            if (rawCost >= long.MaxValue)
            {
                return long.MaxValue;
            }

            switch (roundingMode)
            {
                case PartyHpCostRoundingMode.Floor:
                    return (long)Math.Floor(rawCost);
                case PartyHpCostRoundingMode.Ceiling:
                    return (long)Math.Ceiling(rawCost);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(roundingMode),
                        roundingMode,
                        "HP cost rounding mode must be defined.");
            }
        }
    }
}

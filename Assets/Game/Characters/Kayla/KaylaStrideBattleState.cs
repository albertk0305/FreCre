using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideBattleState
        : ICharacterCombatStatusSource
    {
        private readonly HashSet<long> chargeAttackActionIds =
            new HashSet<long>();
        private int chargeStacks;
        private long? reservedChargeActionId;
        private int reservedChargeStacks;

        public KaylaStrideBattleState(
            CharacterBattleState owner,
            int maximumChargeStacks,
            int initialChargeStacks = 0)
        {
            Owner = owner
                ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                owner.CharacterId,
                KaylaStrideRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Kayla battle state requires Kayla Stride as its "
                        + "owner.",
                    nameof(owner));
            }

            if (maximumChargeStacks <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumChargeStacks));
            }

            if (initialChargeStacks < 0
                || initialChargeStacks > maximumChargeStacks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialChargeStacks));
            }

            MaximumChargeStacks = maximumChargeStacks;
            chargeStacks = initialChargeStacks;
        }

        public CharacterBattleState Owner { get; }
        public int ChargeStacks => chargeStacks;
        public int MaximumChargeStacks { get; }
        public bool HasCharge => chargeStacks > 0;
        public bool HasReservedChargeAttack =>
            reservedChargeActionId.HasValue;
        public long? ReservedChargeActionId => reservedChargeActionId;
        public int ReservedChargeStacks => reservedChargeStacks;

        public string StatusId => KaylaStrideRules.ChargeStatusId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => ChargeStacks;
        public bool IsVisible => HasCharge;

        public bool TryGainCharge()
        {
            if (chargeStacks >= MaximumChargeStacks)
            {
                return false;
            }

            chargeStacks++;
            return true;
        }

        public bool TryReserveAllCharge(
            long actionId,
            out int reservedStacks)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            reservedStacks = 0;
            if (!HasCharge || HasReservedChargeAttack)
            {
                return false;
            }

            if (!chargeAttackActionIds.Add(actionId))
            {
                throw new InvalidOperationException(
                    $"Charge Action ID {actionId} was already reserved.");
            }

            reservedStacks = chargeStacks;
            chargeStacks = 0;
            reservedChargeActionId = actionId;
            reservedChargeStacks = reservedStacks;
            return true;
        }

        public bool IsChargeAttackAction(long actionId)
        {
            return chargeAttackActionIds.Contains(actionId);
        }

        public bool TryCompleteReservedChargeAttack(long actionId)
        {
            if (reservedChargeActionId != actionId)
            {
                return false;
            }

            reservedChargeActionId = null;
            reservedChargeStacks = 0;
            return true;
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneBattleState
        : ICharacterCombatStatusSource,
          ICharacterCombatStatusBadgeVisibility,
          IPartyBattleStateCombatStatusSource
    {
        public BeatriceBloodstoneBattleState(CharacterBattleState owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                owner.CharacterId,
                BeatriceBloodstoneRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Beatrice battle state requires Beatrice Bloodstone.",
                    nameof(owner));
            }
        }

        public CharacterBattleState Owner { get; }
        public PartyBattleState Party { get; private set; }
        public string StatusId => BeatriceBloodstoneRules.PassiveSkillId;
        public string CharacterId => Owner.CharacterId;
        public int PartySlotIndex => Owner.PartySlotIndex;
        public int CurrentCount => 0;
        public bool IsVisible =>
            Party != null && Party.CurrentHp < Party.MaxHp;
        public bool IsBadgeVisible => false;

        public void BindPartyBattleState(PartyBattleState party)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            bool containsOwner = false;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], Owner))
                {
                    containsOwner = true;
                    break;
                }
            }

            if (!containsOwner)
            {
                throw new ArgumentException(
                    "The bound party must contain the status owner.",
                    nameof(party));
            }

            if (Party != null && !ReferenceEquals(Party, party))
            {
                throw new InvalidOperationException(
                    "A combat status source cannot change battle parties.");
            }

            Party = party;
        }
    }
}

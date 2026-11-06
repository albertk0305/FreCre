using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class ConsumeEffectStackAction : CombatAction
    {
        public ConsumeEffectStackAction(
            long actionId,
            ActionOrigin origin,
            CharacterBattleState target,
            long effectRuntimeId,
            int amount,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Character,
                target,
                null,
                null,
                effectRuntimeId,
                amount,
                rootActionId,
                sourceActionId)
        {
        }

        public ConsumeEffectStackAction(
            long actionId,
            ActionOrigin origin,
            PartyBattleState target,
            long effectRuntimeId,
            int amount,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Party,
                null,
                target,
                null,
                effectRuntimeId,
                amount,
                rootActionId,
                sourceActionId)
        {
        }

        public ConsumeEffectStackAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState target,
            long effectRuntimeId,
            int amount,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Boss,
                null,
                null,
                target,
                effectRuntimeId,
                amount,
                rootActionId,
                sourceActionId)
        {
        }

        private ConsumeEffectStackAction(
            long actionId,
            ActionOrigin origin,
            CombatEffectTargetType targetType,
            CharacterBattleState targetCharacter,
            PartyBattleState targetParty,
            BossBattleState targetBoss,
            long effectRuntimeId,
            int amount,
            long? rootActionId,
            long? sourceActionId)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            if (effectRuntimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectRuntimeId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            TargetType = targetType;
            TargetCharacter = targetCharacter;
            TargetParty = targetParty;
            TargetBoss = targetBoss;
            EffectRuntimeId = effectRuntimeId;
            Amount = amount;
            if (TargetEffects == null)
            {
                throw new ArgumentNullException("target");
            }
        }

        public CombatEffectTargetType TargetType { get; }
        public CharacterBattleState TargetCharacter { get; }
        public PartyBattleState TargetParty { get; }
        public BossBattleState TargetBoss { get; }
        public long EffectRuntimeId { get; }
        public int Amount { get; }

        internal EffectCollection TargetEffects
        {
            get
            {
                switch (TargetType)
                {
                    case CombatEffectTargetType.Character:
                        return TargetCharacter?.Effects;
                    case CombatEffectTargetType.Party:
                        return TargetParty?.Effects;
                    case CombatEffectTargetType.Boss:
                        return TargetBoss?.Effects;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

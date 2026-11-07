using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class ReplaceEffectAction : CombatAction
    {
        public ReplaceEffectAction(
            long actionId,
            ActionOrigin origin,
            CharacterBattleState target,
            long existingEffectRuntimeId,
            EffectInstance replacement,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Character,
                target,
                null,
                null,
                existingEffectRuntimeId,
                replacement,
                rootActionId,
                sourceActionId)
        {
        }

        public ReplaceEffectAction(
            long actionId,
            ActionOrigin origin,
            PartyBattleState target,
            long existingEffectRuntimeId,
            EffectInstance replacement,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Party,
                null,
                target,
                null,
                existingEffectRuntimeId,
                replacement,
                rootActionId,
                sourceActionId)
        {
        }

        public ReplaceEffectAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState target,
            long existingEffectRuntimeId,
            EffectInstance replacement,
            long? rootActionId = null,
            long? sourceActionId = null)
            : this(
                actionId,
                origin,
                CombatEffectTargetType.Boss,
                null,
                null,
                target,
                existingEffectRuntimeId,
                replacement,
                rootActionId,
                sourceActionId)
        {
        }

        private ReplaceEffectAction(
            long actionId,
            ActionOrigin origin,
            CombatEffectTargetType targetType,
            CharacterBattleState targetCharacter,
            PartyBattleState targetParty,
            BossBattleState targetBoss,
            long existingEffectRuntimeId,
            EffectInstance replacement,
            long? rootActionId,
            long? sourceActionId)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            if (existingEffectRuntimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(existingEffectRuntimeId));
            }

            TargetType = targetType;
            TargetCharacter = targetCharacter;
            TargetParty = targetParty;
            TargetBoss = targetBoss;
            ExistingEffectRuntimeId = existingEffectRuntimeId;
            Replacement = replacement
                ?? throw new ArgumentNullException(nameof(replacement));
            if (TargetEffects == null)
            {
                throw new ArgumentNullException("target");
            }
        }

        public CombatEffectTargetType TargetType { get; }
        public CharacterBattleState TargetCharacter { get; }
        public PartyBattleState TargetParty { get; }
        public BossBattleState TargetBoss { get; }
        public long ExistingEffectRuntimeId { get; }
        public EffectInstance Replacement { get; }

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

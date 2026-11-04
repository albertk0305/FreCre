using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideCriticalChargeRule
        : ICombatTriggerRule
    {
        private readonly KaylaStrideBattleState battleState;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public KaylaStrideCriticalChargeRule(
            KaylaStrideBattleState battleState)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || !processedActionIds.Add(damage.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            if (battleState.IsChargeAttackAction(damage.ActionId))
            {
                battleState.TryCompleteReservedChargeAttack(
                    damage.ActionId);
                return Array.Empty<CombatAction>();
            }

            if (!result.WasCritical
                || result.AppliedDamage <= 0
                || context.PlayerTurnNumber <= 0
                || context.Party.IsIncapacitated
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || !ContainsCharacter(
                    context.Party,
                    damage.ContextRequest.Attacker)
                || !IsEligibleAttackType(
                    damage.ContextRequest.AttackType)
                || !BelongsToPlayerAttackExecution(context, damage))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.TryGainCharge();
            return Array.Empty<CombatAction>();
        }

        private static bool BelongsToPlayerAttackExecution(
            CombatActionTriggerContext context,
            DamageAction damage)
        {
            AttackType attackType = damage.ContextRequest.AttackType;
            if (attackType == AttackType.Match
                || attackType == AttackType.Active)
            {
                ActionOrigin expectedOrigin = attackType == AttackType.Match
                    ? ActionOrigin.Match
                    : ActionOrigin.Active;
                return damage.Origin == expectedOrigin
                    && damage.RootActionId == damage.ActionId
                    && !damage.SourceActionId.HasValue;
            }

            if ((attackType != AttackType.Additional
                    && attackType != AttackType.Chase)
                || damage.RootActionId == damage.ActionId
                || !context.History.TryGetResult(
                    damage.RootActionId,
                    out CombatActionResult rootResult))
            {
                return false;
            }

            CombatAction rootAction = rootResult.Action;
            return rootAction.RootActionId == rootAction.ActionId
                && !rootAction.SourceActionId.HasValue
                && (rootAction.Origin == ActionOrigin.Match
                    || rootAction.Origin == ActionOrigin.Active);
        }

        private static bool IsEligibleAttackType(AttackType attackType)
        {
            return attackType == AttackType.Match
                || attackType == AttackType.Active
                || attackType == AttackType.Additional
                || attackType == AttackType.Chase;
        }

        private static bool ContainsCharacter(
            PartyBattleState party,
            CharacterBattleState character)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Fenrir
{
    public sealed class FenrirFrenzyCriticalHitRule : ICombatTriggerRule
    {
        private readonly FenrirBattleRuntimeState runtimeState;
        private readonly CombatActionIdSequence actionIds;

        public FenrirFrenzyCriticalHitRule(
            FenrirBattleRuntimeState runtimeState,
            CombatActionIdSequence actionIds)
        {
            this.runtimeState = runtimeState
                ?? throw new ArgumentNullException(nameof(runtimeState));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damageAction)
                || !result.WasCritical
                || result.AppliedDamage < 1
                || !FenrirRules.IsFrenzyCriticalAttackType(
                    damageAction.ContextRequest.AttackType)
                || !runtimeState.TryGetFrenzyEffect(
                    out EffectInstance frenzy))
            {
                return Array.Empty<CombatAction>();
            }

            runtimeState.EnsureStableState();
            var actions = new List<CombatAction>(2)
            {
                new ConsumeEffectStackAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    context.Boss,
                    frenzy.RuntimeId,
                    1,
                    context.RootActionId,
                    context.ActionId)
            };
            if (frenzy.StackCount == 1)
            {
                if (!runtimeState.TryGetFrenzyAttackEffect(
                    out EffectInstance attackEffect))
                {
                    throw new InvalidOperationException(
                        "Fenrir Frenzy attack effect is missing.");
                }

                actions.Add(new RemoveEffectAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    context.Boss,
                    attackEffect.RuntimeId,
                    context.RootActionId,
                    context.ActionId));
            }

            return actions.AsReadOnly();
        }
    }
}

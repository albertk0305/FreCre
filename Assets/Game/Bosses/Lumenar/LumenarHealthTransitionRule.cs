using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Lumenar
{
    public sealed class LumenarHealthTransitionRule : ICombatTriggerRule
    {
        private readonly LumenarBattleRuntimeState runtimeState;
        private readonly CombatActionIdSequence actionIds;

        public LumenarHealthTransitionRule(LumenarBattleRuntimeState runtimeState, CombatActionIdSequence actionIds)
        {
            this.runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            this.actionIds = actionIds ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(CombatActionTriggerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.CompletedResult is ReplaceEffectActionResult replacementResult)
            {
                var action = (ReplaceEffectAction)replacementResult.Action;
                if (!ReferenceEquals(action.TargetBoss, context.Boss)
                    || !string.Equals(replacementResult.AppliedEffect.EffectId,
                        LumenarRules.StoneFormEffectId, StringComparison.Ordinal))
                    return Array.Empty<CombatAction>();
                LumenarEffectFactory.GetRequiredActiveFormEffect(context.Boss, LumenarForm.Stone);
                if (!runtimeState.TryCompleteTransition(action.ActionId))
                {
                    throw new InvalidOperationException(
                        "A successful Stone form replacement did not match "
                            + "the pending Lumenar transition.");
                }

                return Array.Empty<CombatAction>();
            }

            if (!(context.CompletedResult is DamageActionResult damage)
                || damage.AppliedDamage <= 0 || !runtimeState.CanBeginTransition())
                return Array.Empty<CombatAction>();

            EffectInstance light = LumenarEffectFactory.GetRequiredActiveFormEffect(context.Boss, LumenarForm.Light);
            long actionId = actionIds.Next();
            var replacement = new ReplaceEffectAction(actionId, ActionOrigin.System, context.Boss,
                light.RuntimeId, LumenarEffectFactory.CreateStone(actionId, runtimeState.Config),
                context.RootActionId, context.ActionId);
            runtimeState.BeginTransition(actionId);
            return new CombatAction[] { replacement };
        }
    }
}

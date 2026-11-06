using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Thalassion
{
    public sealed class ThalassionElementalShieldHitRule
        : ICombatTriggerRule
    {
        private readonly ThalassionBattleRuntimeState runtimeState;
        private readonly CombatActionIdSequence actionIds;

        public ThalassionElementalShieldHitRule(
            ThalassionBattleRuntimeState runtimeState,
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
                || result.AppliedDamage <= 0
                || damageAction.ContextRequest.AttackType
                    == AttackType.DamageOverTime
                || (damageAction.ContextRequest.AttackTags
                    & AttackTag.FixedDamage) != 0
                || !runtimeState.TryGetShieldEffect(
                    out EffectInstance shield))
            {
                return Array.Empty<CombatAction>();
            }

            return new CombatAction[]
            {
                new ConsumeEffectStackAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    context.Boss,
                    shield.RuntimeId,
                    1,
                    context.RootActionId,
                    context.ActionId)
            };
        }
    }
}

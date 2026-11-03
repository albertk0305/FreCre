using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lancelot
{
    public sealed class LancelotActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly LancelotResolvedCombatRules rules;

        public LancelotActiveActionProvider(
            LancelotCombatConfig config,
            int awakening)
        {
            rules = LancelotCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => rules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(context.Character, context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    LancelotRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Actor must be Light Lancelot.",
                    nameof(actor));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                LancelotRules.ActiveEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.CriticalDamageIncrease,
                rules.ActiveCriticalDamageIncreaseRate,
                rules.ActiveDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    actionId,
                    ActionOrigin.Active,
                    actor,
                    effect)
            };
        }
    }
}

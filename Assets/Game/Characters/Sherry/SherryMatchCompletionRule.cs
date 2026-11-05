using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryMatchCompletionRule : ICombatTriggerRule
    {
        private readonly SherryBattleState battleState;
        private readonly SherryResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;

        public SherryMatchCompletionRule(
            SherryBattleState battleState,
            SherryCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = SherryCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.StylishMaxRank != combatRules.StylishMaxRank)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "Stylish maximum rank.",
                    nameof(battleState));
            }

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

            if (!TryResolveCompletedMatch(
                    context,
                    out SherryMatchTier matchTier))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.CompleteMatch(matchTier);
            if (battleState.StylishRank
                < combatRules.OniIntimidationMinimumRank)
            {
                return Array.Empty<CombatAction>();
            }

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                SherryRules.OniIntimidationEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                combatRules.ResolveOniIntimidationRate(matchTier),
                combatRules.OniIntimidationDurationTurns,
                effectActionId,
                EffectStackPolicy.ReplaceWithStronger);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.System,
                    context.Boss,
                    effect,
                    context.RootActionId,
                    context.ActionId)
            };
        }

        private bool TryResolveCompletedMatch(
            CombatActionTriggerContext context,
            out SherryMatchTier matchTier)
        {
            matchTier = SherryMatchTier.None;
            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || damage.ContextRequest.AttackType != AttackType.Match
                || damage.ContextRequest.AttackElement != ElementType.Fire
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || context.Boss.IsDefeated
                || context.Party.IsIncapacitated
                || !context.CanTriggerFollowUp)
            {
                return false;
            }

            switch (damage.ContextRequest.AttackTags)
            {
                case AttackTag.Match3:
                    matchTier = SherryMatchTier.Three;
                    return true;
                case AttackTag.Match4:
                    matchTier = SherryMatchTier.Four;
                    return true;
                case AttackTag.Match5Plus:
                    matchTier = SherryMatchTier.FiveOrMore;
                    return true;
                default:
                    return false;
            }
        }
    }
}

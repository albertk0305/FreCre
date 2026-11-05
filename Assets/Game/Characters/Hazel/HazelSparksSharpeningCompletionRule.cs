using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksSharpeningCompletionRule
        : ICombatTriggerRule
    {
        private readonly HazelSparksBattleState battleState;
        private readonly HazelSparksResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public HazelSparksSharpeningCompletionRule(
            HazelSparksBattleState battleState,
            HazelSparksCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = HazelSparksCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.MaximumSharpeningStacks
                    != combatRules.MaximumSharpeningStacks
                || battleState.SharpeningStacks
                    != combatRules.InitialSharpeningStacks)
            {
                throw new ArgumentException(
                    "Hazel battle state must use the resolved initial and "
                        + "maximum Sharpening values.",
                    nameof(battleState));
            }

            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public InitialPartyEffectDefinition
            CreateInitialArtisansTouchEffect()
        {
            return new InitialPartyEffectDefinition(
                HazelSparksRules.ArtisansTouchEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                combatRules.CalculateArtisansTouchMagnitude(
                    battleState.SharpeningStacks),
                remainingTurns: null,
                EffectStackPolicy.ReplaceWithNewest,
                elementFilter: ElementType.Fire);
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
                    out DamageAction match,
                    out int sharpeningGain)
                || !processedActionIds.Add(match.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            HazelSparksSharpeningGainResult gainResult =
                battleState.GainSharpening(
                    sharpeningGain,
                    context.PlayerTurnNumber);
            var actions = new List<CombatAction>(2);
            if (gainResult.SharpeningChanged)
            {
                actions.Add(CreateArtisansTouchAction(context));
            }

            if (gainResult.HasOverflow)
            {
                actions.Add(CreateForgedMomentumAction(
                    context,
                    gainResult.CurrentTurnOverflow));
            }

            return actions.AsReadOnly();
        }

        private ApplyEffectAction CreateArtisansTouchAction(
            CombatActionTriggerContext context)
        {
            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                HazelSparksRules.ArtisansTouchEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                combatRules.CalculateArtisansTouchMagnitude(
                    battleState.SharpeningStacks),
                remainingTurns: null,
                actionId,
                EffectStackPolicy.ReplaceWithNewest,
                elementFilter: ElementType.Fire);
            return new ApplyEffectAction(
                actionId,
                ActionOrigin.System,
                context.Party,
                effect,
                context.RootActionId,
                context.ActionId);
        }

        private ApplyEffectAction CreateForgedMomentumAction(
            CombatActionTriggerContext context,
            int currentTurnOverflow)
        {
            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                HazelSparksRules.ForgedMomentumEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                combatRules.CalculateForgedMomentumMagnitude(
                    currentTurnOverflow),
                combatRules.ForgedMomentumDurationTurns,
                actionId,
                EffectStackPolicy.ReplaceWithNewest);
            return new ApplyEffectAction(
                actionId,
                ActionOrigin.System,
                context.Party,
                effect,
                context.RootActionId,
                context.ActionId);
        }

        private bool TryResolveCompletedMatch(
            CombatActionTriggerContext context,
            out DamageAction match,
            out int sharpeningGain)
        {
            match = null;
            sharpeningGain = 0;
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
                || !context.CanTriggerAttackFollowUp
                || context.PlayerTurnNumber <= 0)
            {
                return false;
            }

            BoardMatchTier tier;
            switch (damage.ContextRequest.AttackTags)
            {
                case AttackTag.Match3:
                    tier = BoardMatchTier.Three;
                    break;
                case AttackTag.Match4:
                    tier = BoardMatchTier.Four;
                    break;
                case AttackTag.Match5Plus:
                    tier = BoardMatchTier.FiveOrMore;
                    break;
                default:
                    return false;
            }

            match = damage;
            sharpeningGain = combatRules.GetSharpeningGain(tier);
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Meimei
{
    public sealed class MeimeiMatchCompletionRule : ICombatTriggerRule
    {
        private readonly MeimeiBattleState battleState;
        private readonly MeimeiResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public MeimeiMatchCompletionRule(
            MeimeiBattleState battleState,
            MeimeiCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = MeimeiCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.TipsinessMaxAmount
                != combatRules.TipsinessMaxAmount)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "tipsiness maximum.",
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
                    out DamageAction match,
                    out MeimeiCup acquiredCup,
                    out int tipsinessGain)
                || !processedActionIds.Add(match.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.TryAcquireCup(acquiredCup);
            if (tipsinessGain > 0)
            {
                battleState.GainTipsiness(tipsinessGain);
            }

            if (!battleState.HasAllCups)
            {
                return Array.Empty<CombatAction>();
            }

            var additional = new DamageAction(
                actionIds.Next(),
                ActionOrigin.Additional,
                new DamageContextBuildRequest(
                    battleState.Owner,
                    context.Party,
                    context.Boss,
                    ElementType.Fire,
                    AttackType.Additional,
                    AttackTag.None,
                    combatRules.DrunkenImmortalDanceCoefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: true,
                    baseCriticalChance:
                        ResolveBaseCriticalChance(
                            context.PlayerTurnNumber)),
                context.RootActionId,
                context.ActionId);
            if (!battleState.TryConsumeCompletedCups())
            {
                throw new InvalidOperationException(
                    "Meimei's completed cups could not be consumed.");
            }

            return new CombatAction[] { additional };
        }

        private double ResolveBaseCriticalChance(int currentTurn)
        {
            int effectiveTipsiness = currentTurn > 0
                ? battleState.GetEffectiveTipsiness(currentTurn)
                : battleState.Tipsiness;
            return DamageContextBuildRequest.DefaultBaseCriticalChance
                + combatRules.CalculateCriticalChanceIncreaseRate(
                    effectiveTipsiness);
        }

        private bool TryResolveCompletedMatch(
            CombatActionTriggerContext context,
            out DamageAction match,
            out MeimeiCup acquiredCup,
            out int tipsinessGain)
        {
            match = null;
            acquiredCup = MeimeiCup.None;
            tipsinessGain = 0;
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
                || !context.CanTriggerAttackFollowUp)
            {
                return false;
            }

            switch (damage.ContextRequest.AttackTags)
            {
                case AttackTag.Match3:
                    acquiredCup = MeimeiCup.First;
                    break;
                case AttackTag.Match4:
                    acquiredCup = MeimeiCup.Second;
                    tipsinessGain =
                        combatRules.Match4TipsinessGainAmount;
                    break;
                case AttackTag.Match5Plus:
                    acquiredCup = MeimeiCup.Final;
                    tipsinessGain =
                        combatRules.Match5TipsinessGainAmount;
                    break;
                default:
                    return false;
            }

            match = damage;
            return true;
        }
    }
}

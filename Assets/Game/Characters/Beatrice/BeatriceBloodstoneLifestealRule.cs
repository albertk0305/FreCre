using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneLifestealRule
        : ICombatTriggerRule
    {
        private readonly BeatriceBloodstoneBattleState battleState;
        private readonly BeatriceBloodstoneResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedDamageActionIds =
            new HashSet<long>();

        public BeatriceBloodstoneLifestealRule(
            BeatriceBloodstoneBattleState battleState,
            BeatriceBloodstoneCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = BeatriceBloodstoneCombatRuleResolver.Resolve(
                config,
                awakening);
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public BeatriceBloodstoneBattleState BattleState => battleState;

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!TryResolveLifesteal(
                context,
                out DamageActionResult damageResult,
                out double lifestealRate))
            {
                return Array.Empty<CombatAction>();
            }

            if (!processedDamageActionIds.Add(
                    damageResult.Action.ActionId)
                || damageResult.AppliedDamage <= 0L)
            {
                return Array.Empty<CombatAction>();
            }

            double baseHealing = damageResult.AppliedDamage
                * lifestealRate;
            double healingCoefficient = baseHealing
                / battleState.Owner.MaxHp;
            return new CombatAction[]
            {
                new HealAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    new HealingContextBuildRequest(
                        battleState.Owner,
                        context.Party,
                        healingCoefficient,
                        appliesCombo: false,
                        finalComboCount: 0),
                    context.RootActionId,
                    context.ActionId)
            };
        }

        private bool TryResolveLifesteal(
            CombatActionTriggerContext context,
            out DamageActionResult damageResult,
            out double lifestealRate)
        {
            damageResult = context.CompletedResult as DamageActionResult;
            lifestealRate = 0d;
            if (damageResult == null
                || !(damageResult.Action is DamageAction damage)
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss))
            {
                return false;
            }

            long currentHp = context.Party.CurrentHp;
            long maxHp = context.Party.MaxHp;
            if (damage.Origin == ActionOrigin.Match
                && damage.ContextRequest.AttackType == AttackType.Match
                && (damage.ContextRequest.AttackTags
                    & AttackTag.Match5Plus) != 0)
            {
                lifestealRate = combatRules.ResolveMatch5(
                    currentHp,
                    maxHp).LifestealRate;
                return true;
            }

            if (damage.Origin == ActionOrigin.Active
                && damage.ContextRequest.AttackType == AttackType.Active)
            {
                lifestealRate = combatRules.ResolveActive(
                    currentHp,
                    maxHp).LifestealRate;
                return true;
            }

            return false;
        }
    }
}

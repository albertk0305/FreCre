using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Thalassion
{
    public sealed class ThalassionBossCombatActionProvider
        : IBossCombatActionProvider,
            IBossCombatActionPlanProvider,
            IBossCombatActionCompletionHandler
    {
        private readonly ThalassionBattleRuntimeState runtimeState;
        private bool hasPendingAction;
        private ThalassionActionKind pendingActionKind;
        private BossBattleState pendingBoss;

        public ThalassionBossCombatActionProvider(
            ThalassionBattleRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState
                ?? throw new ArgumentNullException(nameof(runtimeState));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            BossCombatActionContext context)
        {
            return CreatePlan(context).CombatActions;
        }

        public BossActionPlan CreatePlan(BossCombatActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateBoss(context.Boss);
            ThalassionActionKind actionKind = runtimeState.NextActionKind;
            runtimeState.BeginPendingAction(actionKind);

            var actions = new List<CombatAction>(1);
            switch (actionKind)
            {
                case ThalassionActionKind.TidalStrike:
                    AddDamageAction(
                        actions,
                        context,
                        runtimeState.Config.TidalStrikeCoefficient,
                        AttackTag.None);
                    break;
                case ThalassionActionKind.AbyssalAegis:
                    if (runtimeState.IsShieldActive)
                    {
                        throw new InvalidOperationException(
                            "Abyssal Aegis cannot replace an active shield.");
                    }

                    long applyActionId = context.ActionIds.Next();
                    actions.Add(new ApplyEffectAction(
                        applyActionId,
                        ActionOrigin.System,
                        context.Boss,
                        ThalassionElementalShieldEffectFactory.Create(
                            applyActionId,
                            runtimeState.Config)));
                    break;
                case ThalassionActionKind.RagingFist:
                    AddDamageAction(
                        actions,
                        context,
                        runtimeState.Config.RagingFistCoefficient,
                        AttackTag.None);
                    break;
                case ThalassionActionKind.PressureBurst:
                    int stacks = runtimeState.CurrentShieldStackCount;
                    if (stacks <= 0)
                    {
                        throw new InvalidOperationException(
                            "Pressure Burst requires an active shield.");
                    }

                    double pressureCoefficient =
                        runtimeState.Config.PressureBurstBaseCoefficient
                        + (stacks * runtimeState.Config
                            .PressureBurstPerStackCoefficient);
                    AddDamageAction(
                        actions,
                        context,
                        pressureCoefficient,
                        AttackTag.Heavy);
                    break;
                case ThalassionActionKind.FadingWave:
                    AddDamageAction(
                        actions,
                        context,
                        runtimeState.Config.FadingWaveCoefficient,
                        AttackTag.None);
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported Thalassion action kind.");
            }

            hasPendingAction = true;
            pendingActionKind = actionKind;
            pendingBoss = context.Boss;
            return new BossActionPlan(actions);
        }

        public bool TryCommitCompletedAction()
        {
            if (!hasPendingAction || pendingBoss == null)
            {
                return false;
            }

            EffectInstance shieldToRemove = null;
            switch (pendingActionKind)
            {
                case ThalassionActionKind.AbyssalAegis:
                    if (!runtimeState.TryGetShieldEffect(
                            out EffectInstance createdShield)
                        || createdShield.StackCount
                            != runtimeState.Config.InitialShieldStacks)
                    {
                        return false;
                    }

                    break;
                case ThalassionActionKind.PressureBurst:
                    if (!runtimeState.TryGetShieldEffect(out shieldToRemove))
                    {
                        return false;
                    }

                    break;
                case ThalassionActionKind.FadingWave:
                    if (runtimeState.IsShieldActive)
                    {
                        return false;
                    }

                    break;
            }

            if (!runtimeState.TryCommitPendingAction())
            {
                return false;
            }

            if (shieldToRemove != null
                && !pendingBoss.Effects.RemoveEffect(
                    shieldToRemove.RuntimeId))
            {
                throw new InvalidOperationException(
                    "Completed Pressure Burst could not remove its shield.");
            }

            hasPendingAction = false;
            pendingBoss = null;
            return true;
        }

        private static void AddDamageAction(
            ICollection<CombatAction> actions,
            BossCombatActionContext context,
            double coefficient,
            AttackTag attackTags)
        {
            actions.Add(new BossDamageAction(
                context.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    context.Boss,
                    context.Party,
                    coefficient,
                    attackTags,
                    BossDamageHitType.Direct)));
        }

        private static void ValidateBoss(BossBattleState boss)
        {
            if (boss == null
                || !string.Equals(
                    boss.BossId,
                    ThalassionRules.BossId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Thalassion BossBattleState is required.",
                    nameof(boss));
            }
        }
    }
}

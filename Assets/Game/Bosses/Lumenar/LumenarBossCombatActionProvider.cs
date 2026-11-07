using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Lumenar
{
    public sealed class LumenarBossCombatActionProvider : IBossCombatActionProvider,
        IBossCombatActionPlanProvider, IBossCombatActionCompletionHandler
    {
        private readonly LumenarBattleRuntimeState runtimeState;
        public LumenarBossCombatActionProvider(LumenarBattleRuntimeState runtimeState) =>
            this.runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));

        public IReadOnlyList<CombatAction> CreateRootActions(BossCombatActionContext context) => CreatePlan(context).CombatActions;

        public BossActionPlan CreatePlan(BossCombatActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!string.Equals(context.Boss.BossId, LumenarRules.BossId, StringComparison.Ordinal))
                throw new ArgumentException("Lumenar BossBattleState is required.", nameof(context));
            LumenarBossIntent intent = runtimeState.NextIntent;
            runtimeState.BeginPendingAction();
            var actions = new CombatAction[]
            {
                new BossDamageAction(context.ActionIds.Next(), new BossDamageContextBuildRequest(
                    context.Boss, context.Party, intent.DamageCoefficient, AttackTag.None, BossDamageHitType.Direct))
            };
            BattleBoardMutationCommand command = intent.CreatesFallingStone
                ? new BattleBoardMutationCommand(BattleBoardMutationKind.CreateFallingStone,
                    runtimeState.Config.FallingStoneCreationCount, runtimeState.Config.MaximumFallingStoneCount)
                : null;
            return new BossActionPlan(actions, command);
        }

        public bool TryCommitCompletedAction() => runtimeState.TryCommitPendingAction();
    }
}

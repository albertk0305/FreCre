using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Orver
{
    public sealed class OrverBossCombatActionProvider
        : IBossCombatActionProvider,
            IBossCombatActionPlanProvider,
            IBossCombatActionCompletionHandler,
            IBossIntentSource,
            IBossBoardStateConsumer
    {
        private readonly OrverBattleRuntimeState runtimeState;
        private IBattleBoardStateSource boardStateSource;

        public OrverBossCombatActionProvider(
            OrverBattleRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState
                ?? throw new ArgumentNullException(nameof(runtimeState));
        }

        public OrverBossCombatActionProvider(
            OrverBattleRuntimeState runtimeState,
            IBattleBoardStateSource boardStateSource)
            : this(runtimeState)
        {
            BindBoardStateSource(boardStateSource);
        }

        public OrverBattleRuntimeState RuntimeState => runtimeState;

        public void BindBoardStateSource(
            IBattleBoardStateSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (boardStateSource != null
                && !ReferenceEquals(boardStateSource, source))
            {
                throw new InvalidOperationException(
                    "Orver Board state source is already bound for this "
                        + "battle runtime.");
            }

            boardStateSource = source;
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

            ValidateBoss(context);
            int rootCount = GetValidatedRootCount();
            OrverResolvedAction resolved = OrverActionResolver.Resolve(
                runtimeState.PatternIndex,
                rootCount,
                runtimeState.Config);
            var actions = new List<CombatAction>(2);
            if (resolved.DealsDamage)
            {
                actions.Add(new BossDamageAction(
                    context.ActionIds.Next(),
                    new BossDamageContextBuildRequest(
                        context.Boss,
                        context.Party,
                        resolved.DamageCoefficient,
                        AttackTag.None,
                        BossDamageHitType.Direct)));
            }

            if (resolved.HealsBoss)
            {
                actions.Add(new BossHealAction(
                    context.ActionIds.Next(),
                    context.Boss,
                    resolved.HealingCoefficient));
            }

            BattleBoardMutationCommand boardCommand = resolved.CreatesRoot
                ? new BattleBoardMutationCommand(
                    BattleBoardMutationKind.CreateRoot,
                    runtimeState.Config.RootCreationCount,
                    runtimeState.Config.MaximumRootCount)
                : null;
            runtimeState.BeginPendingAction(resolved);
            return new BossActionPlan(actions, boardCommand);
        }

        public bool TryCommitCompletedAction()
        {
            return runtimeState.TryCommitPendingAction();
        }

        public IReadOnlyList<BossIntentPreview> GetIntentForecast(int count)
        {
            if (count <= 0)
            {
                return Array.Empty<BossIntentPreview>();
            }

            int currentRootCount = GetValidatedRootCount();
            var forecast = new List<BossIntentPreview>(count);
            for (int offset = 0; offset < count; offset++)
            {
                int patternIndex =
                    (runtimeState.PatternIndex + offset)
                    % runtimeState.Config.PatternCount;
                if (offset > 0
                    && OrverActionResolver.IsConditionalPattern(
                        patternIndex))
                {
                    break;
                }

                OrverResolvedAction resolved = OrverActionResolver.Resolve(
                    patternIndex,
                    offset == 0 ? currentRootCount : 0,
                    runtimeState.Config);
                forecast.Add(new BossIntentPreview(
                    offset + 1,
                    resolved.SkillId));
            }

            if (forecast.Count == 0)
            {
                return Array.Empty<BossIntentPreview>();
            }

            return forecast.AsReadOnly();
        }

        private int GetValidatedRootCount()
        {
            IBattleBoardStateSource source = boardStateSource
                ?? throw new InvalidOperationException(
                    "Orver Board state source must be bound before Intent "
                        + "or action planning is requested.");
            int rootCount = source.GetRootCount();
            if (rootCount < 0
                || rootCount > runtimeState.Config.MaximumRootCount)
            {
                throw new InvalidOperationException(
                    $"Board Root count must be between 0 and "
                        + $"{runtimeState.Config.MaximumRootCount}, but was "
                        + $"{rootCount}.");
            }

            return rootCount;
        }

        private static void ValidateBoss(BossCombatActionContext context)
        {
            if (!string.Equals(
                    context.Boss.BossId,
                    OrverRules.BossId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Orver BossBattleState is required.",
                    nameof(context));
            }
        }
    }
}

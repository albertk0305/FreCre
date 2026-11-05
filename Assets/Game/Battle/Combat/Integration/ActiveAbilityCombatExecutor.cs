using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityCombatExecutor
    {
        private readonly BattleFlowCoordinator coordinator;
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly CombatActionIdSequence actionIds;
        private readonly ActiveAbilityBinding[] bindingsByIndex;
        private readonly ActiveAbilityActionProviderRegistry providers;
        private readonly IBattleBoardElementConversionSink
            boardElementConversionSink;
        private readonly IBattleBoardShuffleElementConversionSink
            boardShuffleElementConversionSink;

        public ActiveAbilityCombatExecutor(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ActiveAbilityBinding> bindings,
            ActiveAbilityActionProviderRegistry providers,
            IBattleBoardElementConversionSink boardElementConversionSink = null,
            IBattleBoardShuffleElementConversionSink
                boardShuffleElementConversionSink = null)
        {
            this.coordinator = coordinator
                ?? throw new ArgumentNullException(nameof(coordinator));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.providers = providers
                ?? throw new ArgumentNullException(nameof(providers));
            this.boardElementConversionSink = boardElementConversionSink;
            this.boardShuffleElementConversionSink =
                boardShuffleElementConversionSink;
            bindingsByIndex = CreateBindings(bindings);
        }

        public bool TryExecute(
            int activeAbilityIndex,
            out CombatActionExecutionResult result)
        {
            result = null;
            if (!TryBeginExecution(
                activeAbilityIndex,
                out ActiveAbilityCombatExecutionSession session))
            {
                return false;
            }

            while (session.TryExecuteNext(out _))
            {
            }

            result = session.Result;
            return true;
        }

        public bool TryBeginExecution(
            int activeAbilityIndex,
            out ActiveAbilityCombatExecutionSession session)
        {
            session = null;
            if (!TryResolveAvailableActive(
                activeAbilityIndex,
                out ActiveAbilityBinding binding,
                out CharacterBattleState character,
                out IActiveAbilityActionProvider provider,
                out IActiveAbilityBoardPlanProvider boardProvider,
                out IActiveAbilityBoardShuffleElementConversionPlanProvider
                    boardShuffleElementConversionProvider))
            {
                return false;
            }

            if (boardProvider != null)
            {
                return TryBeginBoardExecution(
                    activeAbilityIndex,
                    binding,
                    character,
                    boardProvider,
                    out session);
            }

            if (boardShuffleElementConversionProvider != null)
            {
                return TryBeginBoardShuffleElementConversionExecution(
                    activeAbilityIndex,
                    binding,
                    character,
                    boardShuffleElementConversionProvider,
                    out session);
            }

            var context = new ActiveAbilityActionContext(
                binding,
                character,
                party,
                boss,
                actionIds,
                coordinator.Context.CurrentTurn);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> rootActions =
                provider.CreateRootActions(context);
            ValidateRootActions(rootActions, idBeforeProvider);
            CombatActionExecutionSession actionSession =
                executor.BeginExecution(
                    rootActions,
                    coordinator.Context.CurrentTurn);
            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            session = new ActiveAbilityCombatExecutionSession(
                binding,
                character,
                actionSession);
            return true;
        }

        public bool CanExecute(int activeAbilityIndex)
        {
            return TryResolveAvailableActive(
                activeAbilityIndex,
                out _,
                out _,
                out _,
                out _,
                out _);
        }

        public bool TryGetSingleBinding(
            int partySlotIndex,
            string characterId,
            out ActiveAbilityBinding binding)
        {
            binding = null;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            for (int index = 0; index < bindingsByIndex.Length; index++)
            {
                ActiveAbilityBinding candidate = bindingsByIndex[index];
                if (candidate == null
                    || candidate.PartySlotIndex != partySlotIndex
                    || !string.Equals(
                        candidate.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (binding != null)
                {
                    binding = null;
                    return false;
                }

                binding = candidate;
            }

            return binding != null;
        }

        private bool TryResolveAvailableActive(
            int activeAbilityIndex,
            out ActiveAbilityBinding binding,
            out CharacterBattleState character,
            out IActiveAbilityActionProvider provider,
            out IActiveAbilityBoardPlanProvider boardProvider,
            out IActiveAbilityBoardShuffleElementConversionPlanProvider
                boardShuffleElementConversionProvider)
        {
            binding = null;
            character = null;
            provider = null;
            boardProvider = null;
            boardShuffleElementConversionProvider = null;
            if (!coordinator.CanUseActiveAbility(activeAbilityIndex))
            {
                return false;
            }

            binding = bindingsByIndex[activeAbilityIndex];
            character = binding != null
                ? FindBoundCharacter(binding)
                : null;
            if (character == null)
            {
                return false;
            }

            if (providers.TryResolve(
                binding.CharacterId,
                binding.ActiveAbilityId,
                out provider))
            {
                return true;
            }

            if (providers.TryResolveBoard(
                    binding.CharacterId,
                    binding.ActiveAbilityId,
                    out boardProvider))
            {
                return boardElementConversionSink != null
                    && boardElementConversionSink.CanAcceptElementConversion;
            }

            return providers.TryResolveBoardShuffleElementConversion(
                    binding.CharacterId,
                    binding.ActiveAbilityId,
                    out boardShuffleElementConversionProvider)
                && boardShuffleElementConversionSink != null
                && boardShuffleElementConversionSink
                    .CanAcceptShuffleElementConversion;
        }

        private bool TryBeginBoardExecution(
            int activeAbilityIndex,
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            IActiveAbilityBoardPlanProvider provider,
            out ActiveAbilityCombatExecutionSession session)
        {
            session = null;
            var planContext = new ActiveAbilityBoardPlanContext(
                binding,
                character,
                party,
                boss,
                coordinator.Context.CurrentTurn);
            ActiveAbilityBoardElementConversionPlan plan =
                provider.CreateBoardPlan(planContext)
                ?? throw new InvalidOperationException(
                    "Board Active providers must return a Board plan.");

            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            BoardElementConversionResult conversionResult;
            bool converted;
            try
            {
                converted = boardElementConversionSink.TryConvertElements(
                    plan.TargetElement,
                    plan.RequestedCount,
                    out conversionResult);
            }
            catch
            {
                RollbackCommittedActive(activeAbilityIndex);
                throw;
            }

            if (!converted
                || conversionResult == null
                || !conversionResult.Succeeded
                || conversionResult.RequestedCount != plan.RequestedCount
                || !HasExpectedTargetElement(
                    conversionResult,
                    plan.TargetElement))
            {
                RollbackCommittedActive(activeAbilityIndex);
                return false;
            }

            var successContext = new ActiveAbilityBoardSuccessContext(
                planContext,
                plan,
                conversionResult);
            if (provider is IActiveAbilityBoardSuccessCommitter committer)
            {
                try
                {
                    committer.CommitBoardSuccess(successContext);
                }
                catch (Exception exception)
                {
                    // The Board and Active runtime are already committed.
                    // A listener failure cannot safely turn this use into a
                    // reported failure or roll the Board back.
                    GameLogger.Exception(exception);
                }
            }

            IReadOnlyList<CombatAction> rootActions =
                Array.Empty<CombatAction>();
            if (provider is IActiveAbilityBoardSuccessActionProvider
                actionProvider)
            {
                long idBeforeProvider = actionIds.LastIssuedId;
                rootActions = actionProvider.CreateBoardSuccessActions(
                    new ActiveAbilityBoardSuccessActionContext(
                        successContext,
                        actionIds,
                        coordinator.Context.CurrentTurn));
                ValidateRootActions(
                    rootActions,
                    idBeforeProvider,
                    requireAtLeastOne: false);
            }

            CombatActionExecutionSession actionSession =
                executor.BeginExecution(
                    rootActions,
                    coordinator.Context.CurrentTurn);

            session = new ActiveAbilityCombatExecutionSession(
                binding,
                character,
                actionSession);
            return true;
        }

        private bool TryBeginBoardShuffleElementConversionExecution(
            int activeAbilityIndex,
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            IActiveAbilityBoardShuffleElementConversionPlanProvider provider,
            out ActiveAbilityCombatExecutionSession session)
        {
            session = null;
            var planContext = new ActiveAbilityBoardPlanContext(
                binding,
                character,
                party,
                boss,
                coordinator.Context.CurrentTurn);
            ActiveAbilityBoardShuffleElementConversionPlan plan =
                provider.CreateBoardPlan(planContext)
                ?? throw new InvalidOperationException(
                    "Composite Board Active providers must return a plan.");

            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            BoardShuffleElementConversionResult boardResult;
            bool committed;
            try
            {
                committed = boardShuffleElementConversionSink
                    .TryShuffleAndConvertElements(
                        plan.TargetElement,
                        plan.RequestedCount,
                        out boardResult);
            }
            catch
            {
                RollbackCommittedActive(activeAbilityIndex);
                throw;
            }

            if (!committed
                || !IsValidBoardShuffleElementConversionResult(
                    boardResult,
                    plan))
            {
                RollbackCommittedActive(activeAbilityIndex);
                return false;
            }

            var successContext =
                new ActiveAbilityBoardShuffleElementConversionSuccessContext(
                    planContext,
                    plan,
                    boardResult);
            if (provider is
                IActiveAbilityBoardShuffleElementConversionSuccessCommitter
                    committer)
            {
                try
                {
                    committer.CommitBoardSuccess(successContext);
                }
                catch (Exception exception)
                {
                    // The Board and cooldown are already committed. Keep the
                    // failure visible; providers must prevalidate and use a
                    // non-throwing battle-state commit.
                    GameLogger.Exception(exception);
                    throw;
                }
            }

            CombatActionExecutionSession actionSession =
                executor.BeginExecution(
                    Array.Empty<CombatAction>(),
                    coordinator.Context.CurrentTurn);
            session = new ActiveAbilityCombatExecutionSession(
                binding,
                character,
                actionSession);
            return true;
        }

        private static bool IsValidBoardShuffleElementConversionResult(
            BoardShuffleElementConversionResult result,
            ActiveAbilityBoardShuffleElementConversionPlan plan)
        {
            if (result == null
                || !result.Succeeded
                || result.RequestedConversionCount != plan.RequestedCount
                || result.ConvertedCount < 0
                || result.ConvertedCount > plan.RequestedCount
                || result.ConversionEntries.Count != result.ConvertedCount)
            {
                return false;
            }

            for (int index = 0;
                index < result.ConversionEntries.Count;
                index++)
            {
                BoardElementConversionEntry entry =
                    result.ConversionEntries[index];
                if (entry.BlockType != BoardBlockType.Normal
                    || entry.PreviousElement == plan.TargetElement
                    || entry.NewElement != plan.TargetElement)
                {
                    return false;
                }
            }

            return true;
        }

        private void RollbackCommittedActive(int activeAbilityIndex)
        {
            if (!coordinator.TryRollbackActiveAbilityUse(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "A failed Board Active could not restore its runtime state.");
            }
        }

        private static bool HasExpectedTargetElement(
            BoardElementConversionResult result,
            ElementType targetElement)
        {
            for (int index = 0; index < result.Entries.Count; index++)
            {
                if (result.Entries[index].NewElement != targetElement)
                {
                    return false;
                }
            }

            return result.ConvertedCount > 0;
        }

        private ActiveAbilityBinding[] CreateBindings(
            IReadOnlyList<ActiveAbilityBinding> bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            var result = new ActiveAbilityBinding[
                coordinator.Context.ActiveAbilities.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                ActiveAbilityBinding binding = bindings[index]
                    ?? throw new ArgumentException(
                        "Active bindings cannot contain null.",
                        nameof(bindings));
                if (binding.ActiveAbilityIndex >= result.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(bindings),
                        binding.ActiveAbilityIndex,
                        "Active binding index is outside runtime state.");
                }

                if (result[binding.ActiveAbilityIndex] != null)
                {
                    throw new ArgumentException(
                        $"Duplicate active binding index: "
                            + $"{binding.ActiveAbilityIndex}.",
                        nameof(bindings));
                }

                if (FindBoundCharacter(binding) == null)
                {
                    throw new InvalidOperationException(
                        "Active binding must match its party slot and "
                            + "character ID.");
                }

                result[binding.ActiveAbilityIndex] = binding;
            }

            return result;
        }

        private CharacterBattleState FindBoundCharacter(
            ActiveAbilityBinding binding)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex == binding.PartySlotIndex)
                {
                    return string.Equals(
                        character.CharacterId,
                        binding.CharacterId,
                        StringComparison.Ordinal)
                            ? character
                            : null;
                }
            }

            return null;
        }

        private void ValidateRootActions(
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider,
            bool requireAtLeastOne = true)
        {
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Active providers must return a root action collection.");
            }

            if (requireAtLeastOne && actions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Active providers must return at least one root action.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Active providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int actionIndex = 0;
                actionIndex < actions.Count;
                actionIndex++)
            {
                CombatAction action = actions[actionIndex]
                    ?? throw new InvalidOperationException(
                        "Active providers cannot return null actions.");
                long expectedActionId = checked(
                    idBeforeProvider + actionIndex + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action.Origin != ActionOrigin.Active)
                {
                    throw new InvalidOperationException(
                        "Active root actions must preserve provider order, "
                            + "use new battle-scoped IDs, have no source, "
                            + "and use Active origin.");
                }
            }
        }
    }

    public sealed class ActiveAbilityCombatExecutionSession
    {
        private readonly CombatActionExecutionSession actionSession;

        internal ActiveAbilityCombatExecutionSession(
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            CombatActionExecutionSession actionSession)
        {
            Binding = binding
                ?? throw new ArgumentNullException(nameof(binding));
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            this.actionSession = actionSession
                ?? throw new ArgumentNullException(nameof(actionSession));
        }

        public ActiveAbilityBinding Binding { get; }
        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public bool IsCompleted => actionSession.IsCompleted;
        public CombatAction NextAction => actionSession.NextAction;
        public CombatActionExecutionResult Result => actionSession.Result;

        public bool TryExecuteNext(
            out ActiveCombatActionExecutionStepResult stepResult)
        {
            stepResult = null;
            if (!actionSession.TryExecuteNext(
                out CombatActionExecutionStepResult actionStep))
            {
                return false;
            }

            stepResult = new ActiveCombatActionExecutionStepResult(
                Character,
                actionStep);
            return true;
        }
    }

    public sealed class ActiveCombatActionExecutionStepResult
    {
        internal ActiveCombatActionExecutionStepResult(
            CharacterBattleState character,
            CombatActionExecutionStepResult actionStep)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            ActionStep = actionStep
                ?? throw new ArgumentNullException(nameof(actionStep));
        }

        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public CombatActionExecutionStepResult ActionStep { get; }
    }
}

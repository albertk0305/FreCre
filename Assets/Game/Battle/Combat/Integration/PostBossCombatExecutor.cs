using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class PostBossCombatExecutor
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly CombatActionIdSequence actionIds;
        private readonly IReadOnlyList<PostBossActionProviderBinding>
            bindings;
        private readonly HashSet<long> createdActionIds =
            new HashSet<long>();

        public PostBossCombatExecutor(
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            CombatActionIdSequence actionIds,
            IReadOnlyList<PostBossActionProviderBinding> bindings)
        {
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.bindings = CopyAndValidateBindings(bindings);
        }

        public PostBossCombatExecutionSession BeginExecution(int currentTurn)
        {
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTurn));
            }

            return new PostBossCombatExecutionSession(this, currentTurn);
        }

        public bool HasProviders => bindings.Count > 0;

        private CombatActionExecutionSession BeginProviderExecution(
            PostBossActionProviderBinding binding,
            int currentTurn)
        {
            var context = new PostBossActionContext(
                binding.Character,
                party,
                boss,
                currentTurn,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> actions =
                binding.Provider.CreateRootActions(context);
            ValidateProviderActions(binding, actions, idBeforeProvider);
            return executor.BeginExecution(actions, currentTurn);
        }

        private IReadOnlyList<PostBossActionProviderBinding>
            CopyAndValidateBindings(
                IReadOnlyList<PostBossActionProviderBinding> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new PostBossActionProviderBinding[source.Count];
            int previousSlot = -1;
            for (int index = 0; index < source.Count; index++)
            {
                PostBossActionProviderBinding binding = source[index]
                    ?? throw new ArgumentException(
                        $"Post-boss provider binding {index} is null.",
                        nameof(source));
                if (binding.Character.PartySlotIndex <= previousSlot)
                {
                    throw new ArgumentException(
                        "Post-boss provider bindings must use unique party "
                            + "slots in ascending order.",
                        nameof(source));
                }

                ValidatePartyCharacter(binding.Character);
                previousSlot = binding.Character.PartySlotIndex;
                copy[index] = binding;
            }

            return Array.AsReadOnly(copy);
        }

        private void ValidatePartyCharacter(CharacterBattleState character)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], character))
                {
                    return;
                }
            }

            throw new ArgumentException(
                "Post-boss provider character must belong to the battle "
                    + "party.");
        }

        private void ValidateProviderActions(
            PostBossActionProviderBinding binding,
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider)
        {
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Post-boss action providers must return an action "
                        + "collection.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Post-boss action providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int index = 0; index < actions.Count; index++)
            {
                CombatAction action = actions[index]
                    ?? throw new InvalidOperationException(
                        "Post-boss action providers cannot return null "
                            + "actions.");
                long expectedActionId = checked(
                    idBeforeProvider + index + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action.Origin != ActionOrigin.System
                    || !createdActionIds.Add(action.ActionId))
                {
                    throw new InvalidOperationException(
                        "Post-boss actions must be System roots in provider "
                            + "order and use new battle-scoped IDs.");
                }

                if (action is DamageAction damage
                    && (!ReferenceEquals(
                            damage.ContextRequest.Attacker,
                            binding.Character)
                        || !ReferenceEquals(
                            damage.ContextRequest.Party,
                            party)
                        || !ReferenceEquals(
                            damage.ContextRequest.TargetBoss,
                            boss)))
                {
                    throw new InvalidOperationException(
                        "Post-boss DamageAction context must use the bound "
                            + "character and current battle states.");
                }
            }
        }

        public sealed class PostBossCombatExecutionSession
        {
            private readonly PostBossCombatExecutor owner;
            private readonly int currentTurn;
            private int bindingIndex = -1;
            private CombatActionExecutionSession actionSession;

            internal PostBossCombatExecutionSession(
                PostBossCombatExecutor owner,
                int currentTurn)
            {
                this.owner = owner
                    ?? throw new ArgumentNullException(nameof(owner));
                this.currentTurn = currentTurn;
                BeginNextProviderOrComplete();
            }

            public bool IsCompleted { get; private set; }
            public CharacterBattleState CurrentCharacter { get; private set; }

            public bool TryGetNextAction(
                out CharacterBattleState character,
                out CombatAction action)
            {
                character = CurrentCharacter;
                action = actionSession?.NextAction;
                return character != null && action != null;
            }

            public bool TryExecuteNext(
                out PostBossCombatExecutionStepResult stepResult)
            {
                stepResult = null;
                if (CurrentCharacter == null
                    || actionSession == null
                    || !actionSession.TryExecuteNext(
                        out CombatActionExecutionStepResult actionStep))
                {
                    return false;
                }

                stepResult = new PostBossCombatExecutionStepResult(
                    CurrentCharacter,
                    actionStep);
                return true;
            }

            public bool TryCompleteCurrentProvider(
                out CombatActionExecutionResult result)
            {
                result = null;
                if (IsCompleted
                    || actionSession == null
                    || !actionSession.IsCompleted)
                {
                    return false;
                }

                result = actionSession.Result;
                actionSession = null;
                CurrentCharacter = null;
                if (owner.boss.IsDefeated
                    || owner.party.IsIncapacitated)
                {
                    IsCompleted = true;
                    return true;
                }

                BeginNextProviderOrComplete();
                return true;
            }

            private void BeginNextProviderOrComplete()
            {
                bindingIndex++;
                if (bindingIndex >= owner.bindings.Count)
                {
                    IsCompleted = true;
                    return;
                }

                PostBossActionProviderBinding binding =
                    owner.bindings[bindingIndex];
                CurrentCharacter = binding.Character;
                actionSession = owner.BeginProviderExecution(
                    binding,
                    currentTurn);
            }
        }
    }

    public sealed class PostBossCombatExecutionStepResult
    {
        internal PostBossCombatExecutionStepResult(
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

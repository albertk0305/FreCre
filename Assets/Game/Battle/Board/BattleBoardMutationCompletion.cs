using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BattleBoardMutationCompletion
    {
        public BattleBoardMutationCompletion(
            BattleBoardMutationCommand command,
            BoardRockMutationResult result,
            BattleBoardMutationCompletionStatus status,
            Exception failure = null)
            : this(
                command,
                (IBattleBoardMutationResult)result,
                status,
                failure)
        {
        }

        public BattleBoardMutationCompletion(
            BattleBoardMutationCommand command,
            IBattleBoardMutationResult mutationResult,
            BattleBoardMutationCompletionStatus status,
            Exception failure = null)
        {
            Command = command
                ?? throw new ArgumentNullException(nameof(command));
            if (!Enum.IsDefined(
                typeof(BattleBoardMutationCompletionStatus),
                status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (status == BattleBoardMutationCompletionStatus.Completed
                && (mutationResult == null
                    || !mutationResult.Succeeded
                    || failure != null))
            {
                throw new ArgumentException(
                    "A completed board mutation requires a successful "
                        + "result and no failure.",
                    nameof(mutationResult));
            }

            MutationResult = mutationResult;
            Result = mutationResult as BoardRockMutationResult;
            Status = status;
            Failure = failure;
        }

        public BattleBoardMutationCommand Command { get; }
        // Retained for existing Rock consumers. New mutation kinds should use
        // MutationResult and cast to their concrete result when needed.
        public BoardRockMutationResult Result { get; }
        public IBattleBoardMutationResult MutationResult { get; }
        public BattleBoardMutationCompletionStatus Status { get; }
        public Exception Failure { get; }
        public bool Succeeded =>
            Status == BattleBoardMutationCompletionStatus.Completed;
    }
}

using System;

namespace ValorChronicle.Bosses.Orver
{
    public sealed class OrverBattleRuntimeState
    {
        private readonly OrverCombatConfig config;
        private OrverResolvedAction pendingAction;
        private int pendingPatternIndex = -1;

        public OrverBattleRuntimeState(OrverCombatConfig config)
        {
            this.config = config
                ?? throw new ArgumentNullException(nameof(config));
            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }
        }

        public OrverCombatConfig Config => config;
        public int PatternIndex { get; private set; }
        public bool HasPendingAction => pendingAction != null;
        public OrverResolvedAction PendingAction => pendingAction;
        public int PendingPatternIndex => pendingPatternIndex;
        public int? PendingRootCount => pendingAction?.RootCount;
        public OrverActionKind? PendingActionKind =>
            pendingAction?.ActionKind;
        public double? PendingDamageCoefficient =>
            pendingAction?.DamageCoefficient;
        public double? PendingHealingCoefficient =>
            pendingAction?.HealingCoefficient;

        public void BeginPendingAction(OrverResolvedAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            OrverResolvedAction expected = OrverActionResolver.Resolve(
                PatternIndex,
                action.RootCount,
                config);
            if (action.ActionKind != expected.ActionKind
                || !string.Equals(
                    action.SkillId,
                    expected.SkillId,
                    StringComparison.Ordinal)
                || action.DamageCoefficient
                    != expected.DamageCoefficient
                || action.HealingCoefficient
                    != expected.HealingCoefficient
                || action.CreatesRoot != expected.CreatesRoot)
            {
                throw new InvalidOperationException(
                    "Pending action must match the current Orver pattern "
                        + "and captured Root count.");
            }

            pendingPatternIndex = PatternIndex;
            pendingAction = action;
        }

        public bool TryCommitPendingAction()
        {
            if (pendingAction == null
                || pendingPatternIndex != PatternIndex)
            {
                return false;
            }

            pendingAction = null;
            pendingPatternIndex = -1;
            PatternIndex = (PatternIndex + 1) % config.PatternCount;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Lumenar
{
    public sealed class LumenarBattleRuntimeState : IBossVisualStateSource, IBossIntentSource
    {
        private readonly BossBattleState boss;
        private bool hasPendingAction;
        private LumenarForm pendingActionForm;
        private int pendingPatternIndex;
        private long pendingTransitionActionId;

        public LumenarBattleRuntimeState(BossBattleState boss, LumenarCombatConfig config)
        {
            this.boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (!string.Equals(boss.BossId, LumenarRules.BossId, StringComparison.Ordinal))
                throw new ArgumentException("Lumenar BossBattleState is required.", nameof(boss));
            if (!config.TryValidate(out string errorMessage)) throw new ArgumentException(errorMessage, nameof(config));
        }

        public LumenarCombatConfig Config { get; }
        public LumenarForm CurrentForm { get; private set; } = LumenarForm.Light;
        public int PatternIndex { get; private set; }
        public bool TransitionPending => pendingTransitionActionId > 0;
        public bool TransitionCompleted { get; private set; }
        public string CurrentVisualStateId => CurrentForm == LumenarForm.Light
            ? LumenarRules.LightVisualStateId : LumenarRules.StoneVisualStateId;
        public LumenarBossIntent NextIntent => ResolveIntent(CurrentForm, PatternIndex);

        public bool CanBeginTransition()
        {
            long threshold = (boss.MaxHp / 2L) + (boss.MaxHp % 2L);
            return CurrentForm == LumenarForm.Light && boss.CurrentHp > 0
                && boss.CurrentHp < threshold && !TransitionCompleted && !TransitionPending;
        }

        public void BeginTransition(long replaceActionId)
        {
            if (replaceActionId <= 0) throw new ArgumentOutOfRangeException(nameof(replaceActionId));
            if (!CanBeginTransition()) throw new InvalidOperationException("Lumenar cannot begin a form transition now.");
            pendingTransitionActionId = replaceActionId;
            hasPendingAction = false;
        }

        public bool TryCompleteTransition(long replaceActionId)
        {
            if (!TransitionPending || pendingTransitionActionId != replaceActionId) return false;
            pendingTransitionActionId = 0;
            TransitionCompleted = true;
            CurrentForm = LumenarForm.Stone;
            PatternIndex = 0;
            hasPendingAction = false;
            return true;
        }

        public void BeginPendingAction()
        {
            if (TransitionPending) throw new InvalidOperationException("A boss action cannot begin during transition.");
            if (hasPendingAction)
            {
                if (pendingActionForm != CurrentForm || pendingPatternIndex != PatternIndex)
                    throw new InvalidOperationException("The pending Lumenar action is stale.");
                return;
            }
            hasPendingAction = true;
            pendingActionForm = CurrentForm;
            pendingPatternIndex = PatternIndex;
        }

        public bool TryCommitPendingAction()
        {
            if (!hasPendingAction || pendingActionForm != CurrentForm || pendingPatternIndex != PatternIndex) return false;
            hasPendingAction = false;
            PatternIndex = (PatternIndex + 1) % LumenarRules.PatternCountPerForm;
            return true;
        }

        public IReadOnlyList<BossIntentPreview> GetIntentForecast(int count)
        {
            if (count <= 0) return Array.Empty<BossIntentPreview>();
            var result = new BossIntentPreview[count];
            for (int offset = 0; offset < count; offset++)
            {
                int index = (PatternIndex + offset) % LumenarRules.PatternCountPerForm;
                result[offset] = new BossIntentPreview(offset + 1, ResolveIntent(CurrentForm, index).SkillId);
            }
            return Array.AsReadOnly(result);
        }

        private LumenarBossIntent ResolveIntent(LumenarForm form, int patternIndex)
        {
            if (patternIndex < 0 || patternIndex >= LumenarRules.PatternCountPerForm)
                throw new ArgumentOutOfRangeException(nameof(patternIndex));
            if (form == LumenarForm.Light)
                return patternIndex == 0
                    ? new LumenarBossIntent(LumenarActionKind.RadiantStrike, Config.RadiantStrikeCoefficient, false)
                    : new LumenarBossIntent(LumenarActionKind.CelestialJudgment, Config.CelestialJudgmentCoefficient, false);
            return patternIndex == 0
                    ? new LumenarBossIntent(LumenarActionKind.StoneStrike, Config.StoneStrikeCoefficient, false)
                : new LumenarBossIntent(LumenarActionKind.StonefallCurse, Config.StonefallCurseCoefficient, true);
        }
    }
}

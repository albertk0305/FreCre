using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Thalassion
{
    public sealed class ThalassionBattleRuntimeState
        : IBossVisualStateSource,
            IBossIntentSource
    {
        private readonly BossBattleState boss;
        private readonly ThalassionCombatConfig config;
        private bool hasPendingAction;
        private int pendingPatternIndex;
        private ThalassionActionKind pendingActionKind;

        public ThalassionBattleRuntimeState(
            BossBattleState boss,
            ThalassionCombatConfig config)
        {
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.config = config
                ?? throw new ArgumentNullException(nameof(config));
            if (!string.Equals(
                    boss.BossId,
                    ThalassionRules.BossId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Thalassion BossBattleState is required.",
                    nameof(boss));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }
        }

        public ThalassionCombatConfig Config => config;
        public int PatternIndex { get; private set; }
        public bool IsShieldActive => CurrentShieldStackCount > 0;
        public int CurrentShieldStackCount =>
            TryGetShieldEffect(out EffectInstance effect)
                ? effect.StackCount
                : 0;
        public string CurrentVisualStateId =>
            IsShieldActive
                ? ThalassionRules.ShieldedVisualStateId
                : string.Empty;
        public ThalassionActionKind NextActionKind =>
            ResolveActionKind(PatternIndex, IsShieldActive);

        public bool TryGetShieldEffect(out EffectInstance effect)
        {
            IReadOnlyList<EffectInstance> matches =
                boss.Effects.FindByEffectId(
                    ThalassionRules.ElementalShieldEffectId);
            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Thalassion cannot have multiple elemental shields.");
            }

            effect = matches.Count == 1 ? matches[0] : null;
            return effect != null;
        }

        public IReadOnlyList<BossIntentPreview> GetIntentForecast(int count)
        {
            if (count <= 0)
            {
                return Array.Empty<BossIntentPreview>();
            }

            var forecast = new BossIntentPreview[count];
            bool shieldExpected = IsShieldActive;
            for (int offset = 0; offset < count; offset++)
            {
                int patternIndex =
                    (PatternIndex + offset) % ThalassionRules.PatternCount;
                ThalassionActionKind kind = ResolveActionKind(
                    patternIndex,
                    shieldExpected);
                forecast[offset] = new BossIntentPreview(
                    offset + 1,
                    ThalassionRules.GetSkillId(kind));

                if (patternIndex == (int)ThalassionActionKind.AbyssalAegis)
                {
                    shieldExpected = true;
                }
                else if (patternIndex == 3)
                {
                    shieldExpected = false;
                }
            }

            return Array.AsReadOnly(forecast);
        }

        public void BeginPendingAction(ThalassionActionKind actionKind)
        {
            if (actionKind != NextActionKind)
            {
                throw new InvalidOperationException(
                    "Pending action must match the current Thalassion "
                        + "pattern state.");
            }

            if (hasPendingAction)
            {
                if (pendingPatternIndex != PatternIndex
                    || pendingActionKind != actionKind)
                {
                    throw new InvalidOperationException(
                        "The pending Thalassion action no longer matches "
                            + "the runtime state.");
                }

                return;
            }

            hasPendingAction = true;
            pendingPatternIndex = PatternIndex;
            pendingActionKind = actionKind;
        }

        public bool TryCommitPendingAction()
        {
            if (!hasPendingAction
                || pendingPatternIndex != PatternIndex)
            {
                return false;
            }

            hasPendingAction = false;
            PatternIndex = (PatternIndex + 1)
                % ThalassionRules.PatternCount;
            return true;
        }

        private static ThalassionActionKind ResolveActionKind(
            int patternIndex,
            bool shieldActive)
        {
            switch (patternIndex)
            {
                case 0:
                    return ThalassionActionKind.TidalStrike;
                case 1:
                    return ThalassionActionKind.AbyssalAegis;
                case 2:
                    return ThalassionActionKind.RagingFist;
                case 3:
                    return shieldActive
                        ? ThalassionActionKind.PressureBurst
                        : ThalassionActionKind.FadingWave;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(patternIndex));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Fenrir
{
    public sealed class FenrirBattleRuntimeState
        : IBossIntentSource,
            IBossVisualStateSource
    {
        private readonly BossBattleState boss;
        public FenrirBattleRuntimeState(
            BossBattleState boss,
            FenrirCombatConfig config)
        {
            this.boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (!string.Equals(
                    boss.BossId,
                    FenrirRules.BossId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Fenrir BossBattleState is required.",
                    nameof(boss));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            EnsureStableState();
        }

        public FenrirCombatConfig Config { get; }
        public bool IsFrenzied => TryGetFrenzyEffect(out _);
        public int FrenzyRemainingCriticalHits =>
            TryGetFrenzyEffect(out EffectInstance effect)
                ? effect.StackCount
                : 0;
        public string CurrentVisualStateId => IsFrenzied
            ? FenrirRules.FrenziedVisualStateId
            : string.Empty;
        public string NextSkillId => IsFrenzied
            ? FenrirRules.RampagingClawSkillId
            : FenrirRules.SavageRendSkillId;

        public IReadOnlyList<BossIntentPreview> GetIntentForecast(int count)
        {
            if (count <= 0)
            {
                return Array.Empty<BossIntentPreview>();
            }

            return new[] { new BossIntentPreview(1, NextSkillId) };
        }

        public int GetWoundStackCount(PartyBattleState party)
        {
            return TryGetWoundEffect(party, out EffectInstance effect)
                ? effect.StackCount
                : 0;
        }

        public bool TryGetWoundEffect(
            PartyBattleState party,
            out EffectInstance effect)
        {
            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            return TryGetSingle(
                party.Effects,
                FenrirRules.WoundEffectId,
                out effect);
        }

        public bool TryGetFrenzyEffect(out EffectInstance effect)
        {
            return TryGetSingle(
                boss.Effects,
                FenrirRules.FrenzyEffectId,
                out effect);
        }

        public bool TryGetFrenzyAttackEffect(out EffectInstance effect)
        {
            return TryGetSingle(
                boss.Effects,
                FenrirRules.FrenzyAttackEffectId,
                out effect);
        }

        public void EnsureStableState(PartyBattleState party = null)
        {
            EffectInstance wound = null;
            bool hasWound = party != null
                && TryGetWoundEffect(party, out wound);
            bool hasFrenzy = TryGetFrenzyEffect(out EffectInstance frenzy);
            bool hasFrenzyAttack = TryGetFrenzyAttackEffect(out _);
            if (hasFrenzy != hasFrenzyAttack)
            {
                throw new InvalidOperationException(
                    "Fenrir Frenzy effects must be active together.");
            }

            if (hasFrenzy && hasWound)
            {
                throw new InvalidOperationException(
                    "Fenrir cannot retain Wound while Frenzied.");
            }

            if (hasWound
                && (wound.StackCount <= 0
                    || wound.StackCount >= Config.WoundThreshold))
            {
                throw new InvalidOperationException(
                    "Fenrir Wound must remain below its transition "
                        + "threshold in a stable state.");
            }

            if (hasFrenzy
                && (frenzy.StackCount <= 0
                    || frenzy.StackCount
                        > Config.FrenzyRequiredCriticalHits))
            {
                throw new InvalidOperationException(
                    "Fenrir Frenzy has an invalid remaining hit count.");
            }
        }

        private static bool TryGetSingle(
            EffectCollection collection,
            string effectId,
            out EffectInstance effect)
        {
            IReadOnlyList<EffectInstance> matches =
                collection.FindByEffectId(effectId);
            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Fenrir cannot have multiple '{effectId}' effects.");
            }

            effect = matches.Count == 1 ? matches[0] : null;
            return effect != null;
        }
    }
}

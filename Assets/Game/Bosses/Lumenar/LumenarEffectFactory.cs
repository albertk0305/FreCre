using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Lumenar
{
    public static class LumenarEffectFactory
    {
        public static EffectInstance InitializeBattle(BossBattleState boss, LumenarCombatConfig config, CombatActionIdSequence actionIds)
        {
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (CountActiveFormEffects(boss) != 0) throw new InvalidOperationException("Lumenar form effects were already initialized.");
            long runtimeId = actionIds.Next();
            EffectInstance effect = CreateLight(runtimeId, config);
            boss.Effects.ApplyEffect(effect);
            return effect;
        }

        public static EffectInstance CreateLight(long runtimeId, LumenarCombatConfig config) =>
            Create(runtimeId, LumenarRules.LightFormEffectId, EffectModifierType.BossDealtDamageIncrease,
                config?.LightDamageIncreaseRate ?? throw new ArgumentNullException(nameof(config)));

        public static EffectInstance CreateStone(long runtimeId, LumenarCombatConfig config) =>
            Create(runtimeId, LumenarRules.StoneFormEffectId, EffectModifierType.TargetTakenDamageReduction,
                config?.StoneDamageReductionRate ?? throw new ArgumentNullException(nameof(config)));

        public static EffectInstance GetRequiredActiveFormEffect(BossBattleState boss, LumenarForm form)
        {
            string expected = form == LumenarForm.Light ? LumenarRules.LightFormEffectId : LumenarRules.StoneFormEffectId;
            EffectInstance match = null;
            int count = 0;
            IReadOnlyList<EffectInstance> effects = boss?.Effects.GetActiveEffects()
                ?? throw new ArgumentNullException(nameof(boss));
            for (int index = 0; index < effects.Count; index++)
            {
                if (!IsFormEffect(effects[index].EffectId)) continue;
                count++;
                if (string.Equals(effects[index].EffectId, expected, StringComparison.Ordinal)) match = effects[index];
            }
            if (count != 1 || match == null)
                throw new InvalidOperationException("Lumenar must have exactly one form effect matching " + form + ".");
            return match;
        }

        public static int CountActiveFormEffects(BossBattleState boss)
        {
            int count = 0;
            IReadOnlyList<EffectInstance> effects = boss?.Effects.GetActiveEffects()
                ?? throw new ArgumentNullException(nameof(boss));
            for (int index = 0; index < effects.Count; index++) if (IsFormEffect(effects[index].EffectId)) count++;
            return count;
        }

        private static EffectInstance Create(long runtimeId, string effectId, EffectModifierType modifierType, double magnitude) =>
            new EffectInstance(runtimeId, effectId, LumenarRules.BossId, EffectCategory.Buff, modifierType,
                magnitude, remainingTurns: null, creationOrder: runtimeId, EffectStackPolicy.Unique);

        private static bool IsFormEffect(string effectId) =>
            string.Equals(effectId, LumenarRules.LightFormEffectId, StringComparison.Ordinal)
            || string.Equals(effectId, LumenarRules.StoneFormEffectId, StringComparison.Ordinal);
    }
}

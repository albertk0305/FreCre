using System;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Bosses.Fenrir
{
    public static class FenrirEffectFactory
    {
        public static EffectInstance CreateWound(
            long runtimeId,
            FenrirCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new EffectInstance(
                runtimeId,
                FenrirRules.WoundEffectId,
                FenrirRules.BossId,
                EffectCategory.Debuff,
                EffectModifierType.PresentationOnly,
                0d,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.StackCount,
                stackCount: 1,
                maxStackCount: config.WoundThreshold,
                stackMagnitudeMode: EffectStackMagnitudeMode.Fixed);
        }

        public static EffectInstance CreateFrenzy(
            long runtimeId,
            FenrirCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new EffectInstance(
                runtimeId,
                FenrirRules.FrenzyEffectId,
                FenrirRules.BossId,
                EffectCategory.Buff,
                EffectModifierType.TargetTakenDamageReduction,
                config.FrenzyTakenDamageReductionRate,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.StackCount,
                stackCount: config.FrenzyRequiredCriticalHits,
                maxStackCount: config.FrenzyRequiredCriticalHits,
                stackMagnitudeMode: EffectStackMagnitudeMode.Fixed);
        }

        public static EffectInstance CreateFrenzyAttack(
            long runtimeId,
            FenrirCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new EffectInstance(
                runtimeId,
                FenrirRules.FrenzyAttackEffectId,
                FenrirRules.BossId,
                EffectCategory.Buff,
                EffectModifierType.BossAttackIncrease,
                config.FrenzyAttackIncreaseRate,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.Unique);
        }
    }
}

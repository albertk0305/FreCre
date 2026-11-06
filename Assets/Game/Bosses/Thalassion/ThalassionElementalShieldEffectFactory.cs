using System;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Bosses.Thalassion
{
    public static class ThalassionElementalShieldEffectFactory
    {
        public static EffectInstance Create(
            long runtimeId,
            ThalassionCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            return new EffectInstance(
                runtimeId,
                ThalassionRules.ElementalShieldEffectId,
                ThalassionRules.BossId,
                EffectCategory.Buff,
                EffectModifierType.TargetTakenDamageReduction,
                config.ShieldDamageReductionRate,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.StackCount,
                stackCount: config.InitialShieldStacks,
                maxStackCount: config.MaximumShieldStacks,
                stackMagnitudeMode: EffectStackMagnitudeMode.Fixed);
        }
    }
}

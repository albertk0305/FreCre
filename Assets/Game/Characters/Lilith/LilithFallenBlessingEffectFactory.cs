using System;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Characters.Lilith
{
    public static class LilithFallenBlessingEffectFactory
    {
        public static EffectInstance Create(
            long runtimeId,
            LilithResolvedCombatRules combatRules)
        {
            if (combatRules == null)
            {
                throw new ArgumentNullException(nameof(combatRules));
            }

            return new EffectInstance(
                runtimeId,
                LilithRules.FallenBlessingEffectId,
                LilithRules.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.PartyDamageReduction,
                combatRules.CorruptedBlessingDamageReductionRate,
                combatRules.CorruptedBlessingDurationTurns,
                runtimeId,
                combatRules.CorruptedBlessingStackPolicy,
                stackCount: 1,
                maxStackCount:
                    combatRules.CorruptedBlessingMaxStackCount);
        }
    }
}

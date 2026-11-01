using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Lilith
{
    public static class LilithCovenantEffectFactory
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
                LilithRules.CovenantEffectId,
                LilithRules.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                combatRules.ContractDamageIncreaseRatePerStack,
                combatRules.ContractRemainingTurns,
                runtimeId,
                combatRules.ContractStackPolicy,
                stackCount: 1,
                maxStackCount: combatRules.ContractMaxStackCount,
                attackTypeMask: AttackTypeMask.None);
        }

        public static int GetStackCount(BossBattleState boss)
        {
            return TryGetEffect(boss, out EffectInstance effect)
                ? effect.StackCount
                : 0;
        }

        public static bool TryGetEffect(
            BossBattleState boss,
            out EffectInstance effect)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            IReadOnlyList<EffectInstance> matches =
                boss.Effects.FindByEffectId(
                    LilithRules.CovenantEffectId);
            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Lilith covenant must use one StackCount effect.");
            }

            effect = matches.Count == 1 ? matches[0] : null;
            return effect != null;
        }
    }
}

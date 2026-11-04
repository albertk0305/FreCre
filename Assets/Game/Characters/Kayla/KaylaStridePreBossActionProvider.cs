using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStridePreBossActionProvider
        : IPreBossActionProvider
    {
        private readonly KaylaStrideBattleState battleState;
        private readonly KaylaStrideResolvedCombatRules combatRules;

        public KaylaStridePreBossActionProvider(
            KaylaStrideBattleState battleState,
            KaylaStrideCombatConfig config,
            int awakening)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = KaylaStrideCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.MaximumChargeStacks
                != combatRules.ChargeMaxStacks)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "maximum Charge stacks.",
                    nameof(battleState));
            }
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            PreBossActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateContext(context);
            if (context.Boss.IsDefeated
                || context.Party.IsIncapacitated
                || !battleState.HasCharge)
            {
                return Array.Empty<CombatAction>();
            }

            long actionId = context.ActionIds.Next();
            if (!battleState.TryReserveAllCharge(
                actionId,
                out int consumedStacks))
            {
                throw new InvalidOperationException(
                    "Charge was available but could not be reserved.");
            }

            var action = new DamageAction(
                actionId,
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    context.Character,
                    context.Party,
                    context.Boss,
                    ElementType.Light,
                    AttackType.Additional,
                    AttackTag.None,
                    combatRules.ResolveChargeCoefficient(consumedStacks),
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: true));
            return new CombatAction[] { action };
        }

        private void ValidateContext(PreBossActionContext context)
        {
            if (!ReferenceEquals(context.Character, battleState.Owner)
                || !string.Equals(
                    context.Character.CharacterId,
                    KaylaStrideRules.CharacterId,
                    StringComparison.Ordinal)
                || context.Character.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Pre-boss context must use this Light Kayla owner.",
                    nameof(context));
            }

            bool containsOwner = false;
            for (int index = 0;
                index < context.Party.Characters.Count;
                index++)
            {
                if (ReferenceEquals(
                    context.Party.Characters[index],
                    battleState.Owner))
                {
                    containsOwner = true;
                    break;
                }
            }

            if (!containsOwner)
            {
                throw new ArgumentException(
                    "Kayla must belong to the current party.",
                    nameof(context));
            }
        }
    }
}

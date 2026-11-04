using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly KaylaStrideResolvedCombatRules combatRules;

        public KaylaStrideActiveActionProvider(
            KaylaStrideCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public KaylaStrideActiveActionProvider(
            KaylaStrideCombatConfig config,
            int awakening)
        {
            combatRules = KaylaStrideCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(
                context.Character,
                context.Party,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, party, actionIds);

            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                KaylaStrideRules.ActiveEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.CriticalChanceIncrease,
                combatRules.ActiveCriticalChanceIncreaseRate,
                combatRules.ActiveDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    actionId,
                    ActionOrigin.Active,
                    party,
                    effect)
            };
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    KaylaStrideRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Actor must be Light Kayla Stride.",
                    nameof(actor));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

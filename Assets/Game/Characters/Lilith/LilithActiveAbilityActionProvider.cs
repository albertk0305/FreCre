using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lilith
{
    public sealed class LilithActiveAbilityActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly LilithResolvedCombatRules combatRules;

        public LilithActiveAbilityActionProvider(LilithCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public LilithActiveAbilityActionProvider(
            LilithCombatConfig config,
            int awakening)
        {
            combatRules = LilithCombatRuleResolver.Resolve(
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
                context.Boss,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, party, boss, actionIds);

            var actions = new CombatAction[1
                + combatRules.ActiveContractGainAmount];
            actions[0] = new PartyHpCostAction(
                actionIds.Next(),
                ActionOrigin.Active,
                party,
                party.CurrentHp,
                combatRules.ActiveCurrentPartyHpCostRate);

            for (int index = 0;
                index < combatRules.ActiveContractGainAmount;
                index++)
            {
                long actionId = actionIds.Next();
                actions[index + 1] = new ApplyEffectAction(
                    actionId,
                    ActionOrigin.Active,
                    boss,
                    LilithCovenantEffectFactory.Create(
                        actionId,
                        combatRules));
            }

            return Array.AsReadOnly(actions);
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    LilithRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Actor must be Lilith with Dark element.",
                    nameof(actor));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            bool belongsToParty = false;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], actor))
                {
                    belongsToParty = true;
                    break;
                }
            }

            if (!belongsToParty)
            {
                throw new ArgumentException(
                    "Actor must belong to the target party.",
                    nameof(actor));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

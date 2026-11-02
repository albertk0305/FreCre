using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly LucielResolvedCombatRules combatRules;

        public LucielActiveActionProvider(LucielCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public LucielActiveActionProvider(
            LucielCombatConfig config,
            int awakening)
        {
            combatRules = LucielCombatRuleResolver.Resolve(
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
                context.PlayerTurnNumber,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            PartyBattleState party,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, party, playerTurnNumber, actionIds);

            long costActionId = actionIds.Next();
            var costAction = new PartyHpCostAction(
                costActionId,
                ActionOrigin.Active,
                party,
                party.CurrentHp,
                combatRules.ActiveCurrentPartyHpCostRate,
                roundingMode: combatRules.ActiveHpCostRoundingMode);

            long shieldActionId = actionIds.Next();
            var shieldAction = new ShieldAction(
                shieldActionId,
                ActionOrigin.Active,
                new ShieldGenerationContextBuildRequest(
                    actor,
                    party,
                    combatRules.ActiveShieldMaxHpRate,
                    combatRules.ActiveShieldUsesComboMultiplier,
                    finalComboCount: 0,
                    actionLocalShieldAmountIncreaseRate:
                        combatRules.ActionLocalShieldAmountIncreaseRate),
                new ShieldGrantRequest(
                    runtimeId: shieldActionId,
                    sourceId: LucielRules.ActiveAbilityId,
                    createdTurn: playerTurnNumber,
                    remainingTurns: combatRules.ShieldDurationTurns,
                    creationOrder: shieldActionId));

            return new CombatAction[]
            {
                costAction,
                shieldAction
            };
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    LucielRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Actor must be Dark Luciel.",
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

            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

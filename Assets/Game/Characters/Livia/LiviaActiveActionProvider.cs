using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaActiveActionProvider : IActiveAbilityActionProvider
    {
        private readonly LiviaResolvedCombatRules rules;

        public LiviaActiveActionProvider(LiviaCombatConfig config, int awakening)
        {
            rules = LiviaCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => rules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return CreateActions(context.Character, context.Party, context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!string.Equals(source.CharacterId, LiviaRules.CharacterId, StringComparison.Ordinal)
                || source.Element != ElementType.Water)
                throw new ArgumentException("Source must be Water Livia.", nameof(source));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));

            long actionId = actionIds.Next();
            return new CombatAction[]
            {
                new HealAction(
                    actionId,
                    ActionOrigin.Active,
                    new HealingContextBuildRequest(
                        source,
                        party,
                        rules.ActiveHealingCoefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        rules.ActionLocalHealingIncreaseRate))
            };
        }
    }
}

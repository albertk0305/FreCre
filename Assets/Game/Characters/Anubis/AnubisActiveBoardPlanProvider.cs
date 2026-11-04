using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Anubis
{
    public sealed class AnubisActiveBoardPlanProvider
        : IActiveAbilityBoardPlanProvider
    {
        private readonly AnubisResolvedCombatRules combatRules;

        public AnubisActiveBoardPlanProvider(
            AnubisCombatConfig config,
            int awakening)
        {
            combatRules = AnubisCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidateContext(context);
            return new ActiveAbilityBoardElementConversionPlan(
                ElementType.Light,
                combatRules.ActiveBlockConversionCount);
        }

        private static void ValidateContext(
            ActiveAbilityBoardPlanContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CharacterBattleState character = context.Character;
            if (character.Element != ElementType.Light
                || !string.Equals(
                    character.CharacterId,
                    AnubisRules.CharacterId,
                    StringComparison.Ordinal)
                || context.Binding.PartySlotIndex
                    != character.PartySlotIndex
                || !string.Equals(
                    context.Binding.CharacterId,
                    AnubisRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    context.Binding.ActiveAbilityId,
                    AnubisRules.ActiveAbilityId,
                    StringComparison.Ordinal)
                || !ContainsCharacterAtExpectedSlot(
                    context.Party,
                    character))
            {
                throw new InvalidOperationException(
                    "Anubis Active context does not match its Light character, binding, and party slot.");
            }
        }

        private static bool ContainsCharacterAtExpectedSlot(
            PartyBattleState party,
            CharacterBattleState expected)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex != expected.PartySlotIndex)
                {
                    continue;
                }

                return ReferenceEquals(character, expected)
                    && string.Equals(
                        character.CharacterId,
                        expected.CharacterId,
                        StringComparison.Ordinal);
            }

            return false;
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksActiveBoardPlanProvider
        : IActiveAbilityBoardPlanProvider
    {
        private readonly HazelSparksResolvedCombatRules combatRules;

        public HazelSparksActiveBoardPlanProvider(
            HazelSparksCombatConfig config,
            int awakening)
        {
            combatRules = HazelSparksCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidateContext(context);
            return new ActiveAbilityBoardElementConversionPlan(
                ElementType.Fire,
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
            if (character.Element != ElementType.Fire
                || !string.Equals(
                    character.CharacterId,
                    HazelSparksRules.CharacterId,
                    StringComparison.Ordinal)
                || context.Binding.PartySlotIndex
                    != character.PartySlotIndex
                || !string.Equals(
                    context.Binding.CharacterId,
                    HazelSparksRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    context.Binding.ActiveAbilityId,
                    HazelSparksRules.ActiveAbilityId,
                    StringComparison.Ordinal)
                || !ContainsCharacterAtExpectedSlot(
                    context.Party,
                    character))
            {
                throw new InvalidOperationException(
                    "Hazel Active context does not match its Fire "
                        + "character, binding, and party slot.");
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

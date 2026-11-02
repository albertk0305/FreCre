using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Shizuku
{
    public sealed class KarasumaShizukuActiveBoardPlanProvider
        : IActiveAbilityBoardPlanProvider
    {
        private readonly KarasumaShizukuResolvedCombatRules combatRules;

        public KarasumaShizukuActiveBoardPlanProvider(
            KarasumaShizukuCombatConfig config,
            int awakening)
        {
            combatRules = KarasumaShizukuCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidateContext(context);
            return new ActiveAbilityBoardElementConversionPlan(
                ElementType.Dark,
                combatRules.ActiveBlockConversionCount);
        }

        private static void ValidateContext(ActiveAbilityBoardPlanContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            CharacterBattleState character = context.Character;
            if (character.Element != ElementType.Dark
                || !string.Equals(character.CharacterId, KarasumaShizukuRules.CharacterId, StringComparison.Ordinal)
                || context.Binding.PartySlotIndex != character.PartySlotIndex
                || !string.Equals(context.Binding.CharacterId, KarasumaShizukuRules.CharacterId, StringComparison.Ordinal)
                || !string.Equals(context.Binding.ActiveAbilityId, KarasumaShizukuRules.ActiveAbilityId, StringComparison.Ordinal)
                || !ContainsCharacterAtExpectedSlot(context.Party, character))
                throw new InvalidOperationException(
                    "Shizuku Active context does not match its Dark character, binding, and party slot.");
        }

        private static bool ContainsCharacterAtExpectedSlot(
            PartyBattleState party,
            CharacterBattleState expected)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex != expected.PartySlotIndex) continue;
                return ReferenceEquals(character, expected)
                    && string.Equals(character.CharacterId, expected.CharacterId, StringComparison.Ordinal);
            }

            return false;
        }
    }
}

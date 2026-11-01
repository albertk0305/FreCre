using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId =>
            BeatriceBloodstoneRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(BeatriceBloodstoneCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            ValidateInputs(
                member,
                character,
                boss,
                actionIds,
                matchProviders,
                activeProviders);

            var config = member.CharacterDefinition.CombatConfig
                as BeatriceBloodstoneCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Beatrice requires BeatriceBloodstoneCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Beatrice's CombatConfig is invalid. {errorMessage}");
            }

            BeatriceBloodstoneResolvedCombatRules rules =
                BeatriceBloodstoneCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new BeatriceBloodstoneBattleState(character);
            var matchProvider = new BeatriceBloodstoneMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new BeatriceBloodstoneActiveActionProvider(
                config,
                member.Awakening);
            var lifestealRule = new BeatriceBloodstoneLifestealRule(
                battleState,
                config,
                member.Awakening,
                actionIds);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                BeatriceBloodstoneRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    BeatriceBloodstoneRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    new ICombatTriggerRule[] { lifestealRule },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState });
        }

        private static void ValidateInputs(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            if (matchProviders == null)
            {
                throw new ArgumentNullException(nameof(matchProviders));
            }

            if (activeProviders == null)
            {
                throw new ArgumentNullException(nameof(activeProviders));
            }

            if (!string.Equals(
                    member.CharacterId,
                    BeatriceBloodstoneRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || character.PartySlotIndex != member.PartySlotIndex
                || character.Element != ElementType.Dark
                || member.CharacterDefinition.Element != ElementType.Dark)
            {
                throw new InvalidOperationException(
                    "Beatrice's registrar requires the canonical Dark "
                        + "Beatrice party member and battle state.");
            }
        }
    }
}

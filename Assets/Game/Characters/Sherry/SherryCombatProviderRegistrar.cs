using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => SherryRules.CharacterId;

        public Type SupportedCombatConfigType => typeof(SherryCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
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
                    CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    CharacterId,
                    StringComparison.Ordinal)
                || character.PartySlotIndex != member.PartySlotIndex
                || character.Element != ElementType.Fire)
            {
                throw new InvalidOperationException(
                    "Sherry's registrar requires the canonical Fire "
                        + "Sherry member and character state in the same "
                        + "party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as SherryCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Sherry requires SherryCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Sherry's CombatConfig is invalid. {errorMessage}");
            }

            var battleState = new SherryBattleState(
                character,
                config.StylishMaxRank);
            var matchProvider = new SherryMatchActionProvider(
                battleState,
                config,
                member.Awakening);
            var activeProvider = new SherryActiveActionProvider(
                battleState,
                config,
                member.Awakening);
            var completionRule = new SherryMatchCompletionRule(
                battleState,
                config,
                member.Awakening,
                actionIds);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                SherryRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    SherryRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[] { completionRule },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState });
        }
    }
}

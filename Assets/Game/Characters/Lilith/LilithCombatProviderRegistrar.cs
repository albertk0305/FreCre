using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Lilith
{
    public sealed class LilithCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => LilithRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(LilithCombatConfig);

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
                as LilithCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Lilith requires LilithCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Lilith's CombatConfig is invalid. {errorMessage}");
            }

            LilithResolvedCombatRules rules =
                LilithCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var matchProvider = new LilithMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new LilithActiveAbilityActionProvider(
                config,
                member.Awakening);
            var postBossProvider = new LilithPostBossActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                LilithRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    LilithRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    Array.Empty<ICombatTriggerRule>(),
                    Array.Empty<CombatResourceRequirement>(),
                    Array.Empty<ICharacterCombatStatusSource>())
                .WithPostBossActionProviders(
                    new IPostBossActionProvider[] { postBossProvider });
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
                    LilithRules.CharacterId,
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
                    "Lilith's registrar requires the canonical Dark "
                        + "Lilith party member and battle state.");
            }
        }
    }
}

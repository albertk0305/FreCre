using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => HazelSparksRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(HazelSparksCombatConfig);

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
                    member.CharacterId,
                    StringComparison.Ordinal)
                || member.PartySlotIndex != character.PartySlotIndex
                || character.Element != ElementType.Fire)
            {
                throw new InvalidOperationException(
                    "Hazel registrar requires the canonical Fire "
                        + "character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as HazelSparksCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Hazel requires HazelSparksCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Hazel CombatConfig is invalid. {errorMessage}");
            }

            HazelSparksResolvedCombatRules rules =
                HazelSparksCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new HazelSparksBattleState(
                character,
                rules.MaximumSharpeningStacks,
                rules.InitialSharpeningStacks);
            var completionRule =
                new HazelSparksSharpeningCompletionRule(
                    battleState,
                    config,
                    member.Awakening,
                    actionIds);
            var activeProvider = new HazelSparksActiveBoardPlanProvider(
                config,
                member.Awakening);

            matchProviders.Register(
                CharacterId,
                new HazelSparksMatchActionProvider(
                    config,
                    member.Awakening,
                    battleState));
            activeProviders.RegisterBoard(
                CharacterId,
                HazelSparksRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    HazelSparksRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[] { completionRule },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState })
                .WithInitialPartyEffects(
                    new[]
                    {
                        completionRule.CreateInitialArtisansTouchEffect()
                    });
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => KaylaStrideRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(KaylaStrideCombatConfig);

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
                || member.PartySlotIndex != character.PartySlotIndex
                || character.Element != ElementType.Light
                || member.CharacterDefinition.Element != ElementType.Light)
            {
                throw new InvalidOperationException(
                    "Kayla's registrar requires the canonical Light "
                        + "character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as KaylaStrideCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Kayla requires KaylaStrideCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Kayla's CombatConfig is invalid. {errorMessage}");
            }

            KaylaStrideResolvedCombatRules rules =
                KaylaStrideCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new KaylaStrideBattleState(
                character,
                rules.ChargeMaxStacks);
            var matchProvider = new KaylaStrideMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new KaylaStrideActiveActionProvider(
                config,
                member.Awakening);
            var preBossProvider =
                new KaylaStridePreBossActionProvider(
                    battleState,
                    config,
                    member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                KaylaStrideRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    KaylaStrideRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new KaylaStrideCriticalChargeRule(battleState)
                    },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[]
                    {
                        battleState
                    })
                .WithPreBossActionProviders(
                    new IPreBossActionProvider[]
                    {
                        preBossProvider
                    });
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Anubis
{
    public sealed class AnubisCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => AnubisRules.CharacterId;
        public Type SupportedCombatConfigType => typeof(AnubisCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (matchProviders == null) throw new ArgumentNullException(nameof(matchProviders));
            if (activeProviders == null) throw new ArgumentNullException(nameof(activeProviders));

            if (!string.Equals(member.CharacterId, CharacterId, StringComparison.Ordinal)
                || !string.Equals(character.CharacterId, member.CharacterId, StringComparison.Ordinal)
                || member.PartySlotIndex != character.PartySlotIndex
                || character.Element != ElementType.Light)
            {
                throw new InvalidOperationException(
                    "Anubis registrar requires the canonical Light character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as AnubisCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Anubis requires AnubisCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Anubis CombatConfig is invalid. {errorMessage}");
            }

            AnubisResolvedCombatRules rules =
                AnubisCombatRuleResolver.Resolve(config, member.Awakening);
            var activeProvider = new AnubisActiveBoardPlanProvider(
                config,
                member.Awakening);

            matchProviders.Register(
                CharacterId,
                new AnubisMatchActionProvider(config, member.Awakening));
            activeProviders.RegisterBoard(
                CharacterId,
                AnubisRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    AnubisRules.ActiveAbilityId,
                    activeProvider.CooldownTurns)
                .WithInitialPartyEffects(
                    new[]
                    {
                        new InitialPartyEffectDefinition(
                            AnubisRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.ElementDamageIncrease,
                            rules.PassiveLightDamageIncreaseRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique,
                            elementFilter: ElementType.Light)
                    });
        }
    }
}

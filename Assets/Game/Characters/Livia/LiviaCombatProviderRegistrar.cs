using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => LiviaRules.CharacterId;
        public Type SupportedCombatConfigType => typeof(LiviaCombatConfig);

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
                || !string.Equals(character.CharacterId, CharacterId, StringComparison.Ordinal)
                || member.PartySlotIndex != character.PartySlotIndex
                || character.Element != ElementType.Water)
                throw new InvalidOperationException(
                    "Livia's registrar requires the canonical Water character and matching party slot.");

            var config = member.CharacterDefinition.CombatConfig as LiviaCombatConfig;
            if (config == null)
                throw new InvalidOperationException("Livia requires LiviaCombatConfig.");
            if (!config.TryValidate(out string errorMessage))
                throw new InvalidOperationException($"Livia's CombatConfig is invalid. {errorMessage}");

            LiviaResolvedCombatRules rules =
                LiviaCombatRuleResolver.Resolve(config, member.Awakening);
            var activeProvider = new LiviaActiveActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(
                CharacterId,
                new LiviaMatchActionProvider(config, member.Awakening));
            activeProviders.Register(
                CharacterId,
                LiviaRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    LiviaRules.ActiveAbilityId,
                    activeProvider.CooldownTurns)
                .WithInitialPartyEffects(
                    new InitialPartyEffectDefinition[]
                    {
                        new InitialPartyEffectDefinition(
                            LiviaRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.HealingIncrease,
                            rules.PassivePartyHealingIncreaseRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique)
                    });
        }
    }
}

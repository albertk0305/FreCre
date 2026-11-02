using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Shizuku
{
    public sealed class KarasumaShizukuCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => KarasumaShizukuRules.CharacterId;
        public Type SupportedCombatConfigType => typeof(KarasumaShizukuCombatConfig);

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
                || character.Element != ElementType.Dark)
                throw new InvalidOperationException(
                    "Shizuku's registrar requires the canonical Dark character and matching party slot.");

            var config = member.CharacterDefinition.CombatConfig as KarasumaShizukuCombatConfig;
            if (config == null)
                throw new InvalidOperationException("Shizuku requires KarasumaShizukuCombatConfig.");
            if (!config.TryValidate(out string errorMessage))
                throw new InvalidOperationException($"Shizuku's CombatConfig is invalid. {errorMessage}");

            KarasumaShizukuResolvedCombatRules rules =
                KarasumaShizukuCombatRuleResolver.Resolve(config, member.Awakening);
            var activeProvider = new KarasumaShizukuActiveBoardPlanProvider(
                config,
                member.Awakening);

            matchProviders.Register(
                CharacterId,
                new KarasumaShizukuMatchActionProvider(config, member.Awakening));
            activeProviders.RegisterBoard(
                CharacterId,
                KarasumaShizukuRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    KarasumaShizukuRules.ActiveAbilityId,
                    activeProvider.CooldownTurns)
                .WithInitialPartyEffects(
                    new InitialPartyEffectDefinition[]
                    {
                        new InitialPartyEffectDefinition(
                            KarasumaShizukuRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.ElementDamageIncrease,
                            rules.PassiveDarkDamageIncreaseRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique,
                            elementFilter: ElementType.Dark)
                    });
        }
    }
}

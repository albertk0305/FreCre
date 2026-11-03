using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Lancelot
{
    public sealed class LancelotCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        private const long PassiveEffectRuntimeId = 1L << 61;

        public string CharacterId => LancelotRules.CharacterId;
        public Type SupportedCombatConfigType =>
            typeof(LancelotCombatConfig);

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
                || character.Element != ElementType.Light)
            {
                throw new InvalidOperationException(
                    "Lancelot's registrar requires the canonical Light "
                        + "character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as LancelotCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Lancelot requires LancelotCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Lancelot's CombatConfig is invalid. {errorMessage}");
            }

            var activeProvider = new LancelotActiveActionProvider(
                config,
                member.Awakening);
            matchProviders.Register(
                CharacterId,
                new LancelotMatchActionProvider(config, member.Awakening));
            activeProviders.Register(
                CharacterId,
                LancelotRules.ActiveAbilityId,
                activeProvider);

            LancelotResolvedCombatRules rules =
                LancelotCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            character.Effects.ApplyEffect(new EffectInstance(
                PassiveEffectRuntimeId,
                LancelotRules.PassiveEffectId,
                CharacterId,
                EffectCategory.Buff,
                EffectModifierType.PresentationOnly,
                rules.PassiveCriticalChanceIncreaseRate,
                remainingTurns: null,
                creationOrder: PassiveEffectRuntimeId,
                EffectStackPolicy.Unique));

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    LancelotRules.ActiveAbilityId,
                    activeProvider.CooldownTurns);
        }
    }
}

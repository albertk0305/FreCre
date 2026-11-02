using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => LucielRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(LucielCombatConfig);

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
                as LucielCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Luciel requires LucielCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Luciel's CombatConfig is invalid. {errorMessage}");
            }

            LucielResolvedCombatRules rules =
                LucielCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var matchProvider = new LucielMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new LucielActiveActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                LucielRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    LucielRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns)
                .WithInitialPartyEffects(
                    new InitialPartyEffectDefinition[]
                    {
                        new InitialPartyEffectDefinition(
                            LucielRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.PartyDamageReduction,
                            rules.PassiveDamageReductionRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique)
                    });
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
                    LucielRules.CharacterId,
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
                    "Luciel's registrar requires the canonical Dark Luciel "
                        + "party member and battle state.");
            }
        }
    }
}

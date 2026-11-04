using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Chloe
{
    public sealed class ChloeValentineCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => ChloeValentineRules.CharacterId;
        public Type SupportedCombatConfigType =>
            typeof(ChloeValentineCombatConfig);

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
                || character.Element != ElementType.Light)
            {
                throw new InvalidOperationException(
                    "Chloe's registrar requires the canonical Light character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as ChloeValentineCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Chloe requires ChloeValentineCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Chloe's CombatConfig is invalid. {errorMessage}");
            }

            ChloeValentineResolvedCombatRules rules =
                ChloeValentineCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var activeProvider = new ChloeValentineActiveActionProvider(
                config,
                member.Awakening);
            matchProviders.Register(
                CharacterId,
                new ChloeValentineMatchActionProvider(
                    config,
                    member.Awakening));
            activeProviders.Register(
                CharacterId,
                ChloeValentineRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    ChloeValentineRules.ActiveAbilityId,
                    activeProvider.CooldownTurns)
                .WithInitialPartyEffects(
                    new InitialPartyEffectDefinition[]
                    {
                        new InitialPartyEffectDefinition(
                            ChloeValentineRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.AttackIncrease,
                            rules.PassivePartyAttackIncreaseRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique)
                    });
        }
    }
}


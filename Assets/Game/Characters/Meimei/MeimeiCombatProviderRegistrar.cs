using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Meimei
{
    public sealed class MeimeiCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => MeimeiRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(MeimeiCombatConfig);

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
                || character.Element != ElementType.Fire)
            {
                throw new InvalidOperationException(
                    "Meimei's registrar requires the canonical Fire "
                        + "character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as MeimeiCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Meimei requires MeimeiCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Meimei's CombatConfig is invalid. {errorMessage}");
            }

            MeimeiResolvedCombatRules rules =
                MeimeiCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new MeimeiBattleState(
                character,
                rules.TipsinessMaxAmount);
            matchProviders.Register(
                CharacterId,
                new MeimeiMatchActionProvider(
                    battleState,
                    config,
                    member.Awakening));
            var activeProvider = new MeimeiActiveBoardPlanProvider(
                battleState,
                config,
                member.Awakening);
            activeProviders.RegisterBoardShuffleElementConversion(
                CharacterId,
                MeimeiRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    MeimeiRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new MeimeiMatchCompletionRule(
                            battleState,
                            config,
                            member.Awakening,
                            actionIds)
                    },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[]
                    {
                        new MeimeiCombatStatusSource(
                            battleState,
                            MeimeiCombatStatusKind.FirstCup),
                        new MeimeiCombatStatusSource(
                            battleState,
                            MeimeiCombatStatusKind.SecondCup),
                        new MeimeiCombatStatusSource(
                            battleState,
                            MeimeiCombatStatusKind.FinalCup),
                        new MeimeiCombatStatusSource(
                            battleState,
                            MeimeiCombatStatusKind.Tipsiness)
                    });
        }
    }

    public sealed class MeimeiActiveBoardPlanProvider
        : IActiveAbilityBoardShuffleElementConversionPlanProvider,
            IActiveAbilityBoardShuffleElementConversionSuccessCommitter
    {
        private readonly MeimeiBattleState battleState;
        private readonly MeimeiResolvedCombatRules combatRules;

        public MeimeiActiveBoardPlanProvider(
            MeimeiBattleState battleState,
            MeimeiCombatConfig config,
            int awakening)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = MeimeiCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public ActiveAbilityBoardShuffleElementConversionPlan CreateBoardPlan(
            ActiveAbilityBoardPlanContext context)
        {
            ValidateContext(context);
            return new ActiveAbilityBoardShuffleElementConversionPlan(
                ElementType.Fire,
                battleState.Tipsiness);
        }

        public void CommitBoardSuccess(
            ActiveAbilityBoardShuffleElementConversionSuccessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateContext(context.PlanContext);
            if (!context.Result.Succeeded
                || context.Plan.TargetElement != ElementType.Fire
                || context.Plan.RequestedCount != battleState.Tipsiness
                || context.Result.RequestedConversionCount
                    != context.Plan.RequestedCount
                || context.Result.ConvertedCount < 0
                || context.Result.ConvertedCount > battleState.Tipsiness)
            {
                throw new InvalidOperationException(
                    "Meimei Active success does not match its current "
                        + "tipsiness and Fire conversion plan.");
            }

            battleState.CommitSuccessfulActive(
                context.CurrentTurn,
                context.Result.ConvertedCount,
                combatRules.CriticalChancePerTipsiness > 0d);
        }

        private void ValidateContext(ActiveAbilityBoardPlanContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CharacterBattleState character = context.Character;
            if (!ReferenceEquals(character, battleState.Owner)
                || character.Element != ElementType.Fire
                || !string.Equals(
                    character.CharacterId,
                    MeimeiRules.CharacterId,
                    StringComparison.Ordinal)
                || context.Binding.PartySlotIndex
                    != character.PartySlotIndex
                || !string.Equals(
                    context.Binding.CharacterId,
                    MeimeiRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    context.Binding.ActiveAbilityId,
                    MeimeiRules.ActiveAbilityId,
                    StringComparison.Ordinal)
                || !ContainsOwner(context))
            {
                throw new InvalidOperationException(
                    "Meimei Active context does not match its Fire owner, "
                        + "binding, and party slot.");
            }
        }

        private bool ContainsOwner(ActiveAbilityBoardPlanContext context)
        {
            for (int index = 0;
                index < context.Party.Characters.Count;
                index++)
            {
                CharacterBattleState character =
                    context.Party.Characters[index];
                if (character.PartySlotIndex
                    != battleState.Owner.PartySlotIndex)
                {
                    continue;
                }

                return ReferenceEquals(character, battleState.Owner);
            }

            return false;
        }
    }
}

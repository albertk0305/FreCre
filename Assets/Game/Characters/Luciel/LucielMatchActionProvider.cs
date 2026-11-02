using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly LucielResolvedCombatRules combatRules;

        public LucielMatchActionProvider(LucielCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public LucielMatchActionProvider(
            LucielCombatConfig config,
            int awakening)
        {
            combatRules = LucielCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(
                context.Character,
                context.Party,
                context.MatchEvent.Tier,
                context.FinalComboCount,
                context.PlayerTurnNumber,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier matchTier,
            int finalComboCount,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(
                source,
                party,
                matchTier,
                finalComboCount,
                playerTurnNumber,
                actionIds);

            long shieldActionId = actionIds.Next();
            var shieldAction = new ShieldAction(
                shieldActionId,
                ActionOrigin.Match,
                new ShieldGenerationContextBuildRequest(
                    source,
                    party,
                    ResolveShieldRate(matchTier),
                    combatRules.MatchShieldUsesComboMultiplier,
                    finalComboCount,
                    combatRules.ActionLocalShieldAmountIncreaseRate),
                new ShieldGrantRequest(
                    runtimeId: shieldActionId,
                    sourceId: ResolveSkillId(matchTier),
                    createdTurn: playerTurnNumber,
                    remainingTurns: combatRules.ShieldDurationTurns,
                    creationOrder: shieldActionId));

            if (matchTier != BoardMatchTier.FiveOrMore)
            {
                return new CombatAction[] { shieldAction };
            }

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                LucielRules.Match5DamageIncreaseEffectId,
                source.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.DealtDamageIncrease,
                combatRules.Match5PartyDealtDamageIncreaseRate,
                combatRules.Match5BuffDurationTurns,
                effectActionId,
                combatRules.Match5BuffStackPolicy);

            return new CombatAction[]
            {
                shieldAction,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Match,
                    party,
                    effect)
            };
        }

        private double ResolveShieldRate(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3ShieldMaxHpRate;
                case BoardMatchTier.Four:
                    return combatRules.Match4ShieldMaxHpRate;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5ShieldMaxHpRate;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static string ResolveSkillId(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return LucielRules.Match3SkillId;
                case BoardMatchTier.Four:
                    return LucielRules.Match4SkillId;
                case BoardMatchTier.FiveOrMore:
                    return LucielRules.Match5SkillId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static void ValidateInputs(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier matchTier,
            int finalComboCount,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!string.Equals(
                    source.CharacterId,
                    LucielRules.CharacterId,
                    StringComparison.Ordinal)
                || source.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Source must be Dark Luciel.",
                    nameof(source));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (!Enum.IsDefined(typeof(BoardMatchTier), matchTier))
            {
                throw new ArgumentOutOfRangeException(nameof(matchTier));
            }

            if (finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount));
            }

            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

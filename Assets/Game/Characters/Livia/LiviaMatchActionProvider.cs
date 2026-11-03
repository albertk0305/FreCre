using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaMatchActionProvider : IMatchEventActionProvider
    {
        private readonly LiviaResolvedCombatRules rules;

        public LiviaMatchActionProvider(LiviaCombatConfig config, int awakening)
        {
            rules = LiviaCombatRuleResolver.Resolve(config, awakening);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return CreateActions(
                context.Character,
                context.Party,
                context.MatchEvent.Tier,
                context.FinalComboCount,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(source, party, matchTier, finalComboCount, actionIds);

            long healActionId = actionIds.Next();
            var heal = new HealAction(
                healActionId,
                ActionOrigin.Match,
                new HealingContextBuildRequest(
                    source,
                    party,
                    ResolveHealingCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    rules.ActionLocalHealingIncreaseRate));

            if (matchTier != BoardMatchTier.FiveOrMore)
                return new CombatAction[] { heal };

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                LiviaRules.Match5DamageReductionEffectId,
                source.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.PartyDamageReduction,
                rules.Match5PartyDamageReductionRate,
                rules.Match5DamageReductionDurationTurns,
                effectActionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                heal,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Match,
                    party,
                    effect)
            };
        }

        private double ResolveHealingCoefficient(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three: return rules.Match3HealingCoefficient;
                case BoardMatchTier.Four: return rules.Match4HealingCoefficient;
                case BoardMatchTier.FiveOrMore: return rules.Match5HealingCoefficient;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static void ValidateInputs(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier tier,
            int combo,
            CombatActionIdSequence actionIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!string.Equals(source.CharacterId, LiviaRules.CharacterId, StringComparison.Ordinal)
                || source.Element != ElementType.Water)
                throw new ArgumentException("Source must be Water Livia.", nameof(source));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (!Enum.IsDefined(typeof(BoardMatchTier), tier))
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (combo <= 0) throw new ArgumentOutOfRangeException(nameof(combo));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
        }
    }
}

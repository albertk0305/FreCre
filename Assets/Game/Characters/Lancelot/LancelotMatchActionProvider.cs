using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lancelot
{
    public sealed class LancelotMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly LancelotResolvedCombatRules rules;

        public LancelotMatchActionProvider(
            LancelotCombatConfig config,
            int awakening)
        {
            rules = LancelotCombatRuleResolver.Resolve(config, awakening);
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
                context.Boss,
                context.MatchEvent.Tier,
                context.FinalComboCount,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(
                attacker,
                party,
                boss,
                matchTier,
                finalComboCount,
                actionIds);

            long actionId = actionIds.Next();
            var request = new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                ElementType.Light,
                AttackType.Match,
                ResolveMatchTag(matchTier),
                ResolveSkillCoefficient(matchTier),
                appliesCombo: true,
                finalComboCount: finalComboCount,
                canCritical: true,
                baseCriticalChance:
                    DamageContextBuildRequest.DefaultBaseCriticalChance
                    + rules.PassiveCriticalChanceIncreaseRate
                    + ResolveMatchCriticalChanceIncrease(matchTier),
                baseCriticalDamageMultiplier:
                    rules.BaseCriticalDamageMultiplier);
            return new CombatAction[]
            {
                new DamageAction(actionId, ActionOrigin.Match, request)
            };
        }

        private double ResolveSkillCoefficient(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return rules.Match3Coefficient;
                case BoardMatchTier.Four:
                    return rules.Match4Coefficient;
                case BoardMatchTier.FiveOrMore:
                    return rules.Match5Coefficient;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private double ResolveMatchCriticalChanceIncrease(
            BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return 0d;
                case BoardMatchTier.Four:
                    return rules.Match4CriticalChanceIncreaseRate;
                case BoardMatchTier.FiveOrMore:
                    return rules.Match5CriticalChanceIncreaseRate;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static AttackTag ResolveMatchTag(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three:
                    return AttackTag.Match3;
                case BoardMatchTier.Four:
                    return AttackTag.Match4;
                case BoardMatchTier.FiveOrMore:
                    return AttackTag.Match5Plus;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static void ValidateInputs(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (!string.Equals(
                    attacker.CharacterId,
                    LancelotRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Attacker must be Light Lancelot.",
                    nameof(attacker));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
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

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

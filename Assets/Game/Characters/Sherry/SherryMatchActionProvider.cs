using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly SherryBattleState battleState;
        private readonly SherryResolvedCombatRules combatRules;

        public SherryMatchActionProvider(
            SherryBattleState battleState,
            SherryCombatConfig config,
            int awakening)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = SherryCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.StylishMaxRank != combatRules.StylishMaxRank)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "Stylish maximum rank.",
                    nameof(battleState));
            }
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

            SherryMatchTier sherryTier = ResolveSherryMatchTier(matchTier);
            double stylishIncreaseRate =
                combatRules.ResolveStylishDamageIncreaseRate(
                    battleState.StylishRank);
            long actionId = actionIds.Next();
            return new CombatAction[]
            {
                new DamageAction(
                    actionId,
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        attacker,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Match,
                        ResolveMatchTag(matchTier),
                        combatRules.ResolveMatchCoefficient(sherryTier),
                        appliesCombo: true,
                        finalComboCount,
                        canCritical: true,
                        actionLocalDealtDamageIncreaseRate:
                            stylishIncreaseRate))
            };
        }

        private void ValidateInputs(
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

            if (!ReferenceEquals(attacker, battleState.Owner)
                || attacker.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Attacker must be this battle state's Fire Sherry.",
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

        private static SherryMatchTier ResolveSherryMatchTier(
            BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return SherryMatchTier.Three;
                case BoardMatchTier.Four:
                    return SherryMatchTier.Four;
                case BoardMatchTier.FiveOrMore:
                    return SherryMatchTier.FiveOrMore;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static AttackTag ResolveMatchTag(
            BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return AttackTag.Match3;
                case BoardMatchTier.Four:
                    return AttackTag.Match4;
                case BoardMatchTier.FiveOrMore:
                    return AttackTag.Match5Plus;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }
    }
}

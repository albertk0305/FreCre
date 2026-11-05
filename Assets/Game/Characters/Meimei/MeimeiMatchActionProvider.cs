using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Meimei
{
    public sealed class MeimeiMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly MeimeiBattleState battleState;
        private readonly MeimeiResolvedCombatRules combatRules;

        public MeimeiMatchActionProvider(MeimeiCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public MeimeiMatchActionProvider(
            MeimeiCombatConfig config,
            int awakening)
            : this(null, config, awakening)
        {
        }

        public MeimeiMatchActionProvider(
            MeimeiBattleState battleState,
            MeimeiCombatConfig config,
            int awakening)
        {
            this.battleState = battleState;
            combatRules = MeimeiCombatRuleResolver.Resolve(
                config,
                awakening);
            if (combatRules.CriticalChancePerTipsiness > 0d
                && battleState == null)
            {
                throw new ArgumentNullException(
                    nameof(battleState),
                    "Awakening 6 requires Meimei's battle state.");
            }

            if (battleState != null
                && battleState.TipsinessMaxAmount
                    != combatRules.TipsinessMaxAmount)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "tipsiness maximum.",
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
                context.ActionIds,
                context.PlayerTurnNumber);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            return CreateActions(
                attacker,
                party,
                boss,
                matchTier,
                finalComboCount,
                actionIds,
                currentTurn: 1);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds,
            int currentTurn)
        {
            ValidateInputs(
                attacker,
                party,
                boss,
                matchTier,
                finalComboCount,
                actionIds);
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTurn),
                    currentTurn,
                    "Current turn must be positive.");
            }

            if (battleState != null
                && !ReferenceEquals(attacker, battleState.Owner))
            {
                throw new ArgumentException(
                    "Attacker must own the configured Meimei battle state.",
                    nameof(attacker));
            }

            var damage = new DamageAction(
                actionIds.Next(),
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Match,
                    ResolveMatchTag(matchTier),
                    ResolveSkillCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true,
                    baseCriticalChance:
                        ResolveBaseCriticalChance(currentTurn)));
            return new CombatAction[] { damage };
        }

        private double ResolveBaseCriticalChance(int currentTurn)
        {
            if (battleState == null)
            {
                return DamageContextBuildRequest
                    .DefaultBaseCriticalChance;
            }

            int effectiveTipsiness =
                battleState.GetEffectiveTipsiness(currentTurn);
            return DamageContextBuildRequest.DefaultBaseCriticalChance
                + combatRules.CalculateCriticalChanceIncreaseRate(
                    effectiveTipsiness);
        }

        private double ResolveSkillCoefficient(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3Coefficient;
                case BoardMatchTier.Four:
                    return combatRules.Match4Coefficient;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5Coefficient;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static AttackTag ResolveMatchTag(BoardMatchTier matchTier)
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
                    MeimeiRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Attacker must be the canonical Fire Meimei.",
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

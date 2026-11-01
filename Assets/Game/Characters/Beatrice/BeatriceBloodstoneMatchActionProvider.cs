using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly BeatriceBloodstoneResolvedCombatRules combatRules;

        public BeatriceBloodstoneMatchActionProvider(
            BeatriceBloodstoneCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public BeatriceBloodstoneMatchActionProvider(
            BeatriceBloodstoneCombatConfig config,
            int awakening)
        {
            combatRules = BeatriceBloodstoneCombatRuleResolver.Resolve(
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

            AttackTag matchTag = ResolveMatchTag(matchTier);
            if (matchTier == BoardMatchTier.FiveOrMore)
            {
                return new CombatAction[]
                {
                    CreateDamage(
                        actionIds.Next(),
                        attacker,
                        party,
                        boss,
                        matchTier,
                        matchTag,
                        finalComboCount)
                };
            }

            double hpCostRate = matchTier == BoardMatchTier.Three
                ? combatRules.Match3HpCostRate
                : combatRules.Match4HpCostRate;
            return new CombatAction[]
            {
                new PartyHpCostAction(
                    actionIds.Next(),
                    ActionOrigin.Match,
                    party,
                    attacker.MaxHp,
                    hpCostRate),
                CreateDamage(
                    actionIds.Next(),
                    attacker,
                    party,
                    boss,
                    matchTier,
                    matchTag,
                    finalComboCount)
            };
        }

        private DamageAction CreateDamage(
            long actionId,
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            AttackTag matchTag,
            int finalComboCount)
        {
            return new DamageAction(
                actionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Dark,
                    AttackType.Match,
                    matchTag,
                    ResolveBaseCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true,
                    skillCoefficientResolver: request =>
                        ResolveSkillCoefficient(matchTier, request),
                    dealtDamageIncreaseRateResolver: request =>
                        combatRules
                            .ResolvePassiveDealtDamageIncreaseRate(
                                request.Party.CurrentHp,
                                request.Party.MaxHp)));
        }

        private double ResolveBaseCoefficient(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3BaseCoefficient;
                case BoardMatchTier.Four:
                    return combatRules.Match4BaseCoefficient;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5BaseCoefficient;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier));
            }
        }

        private double ResolveSkillCoefficient(
            BoardMatchTier matchTier,
            DamageContextBuildRequest request)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.ResolveMatch3Coefficient(
                        request.Party.CurrentHp,
                        request.Party.MaxHp);
                case BoardMatchTier.Four:
                    return combatRules.ResolveMatch4Coefficient(
                        request.Party.CurrentHp,
                        request.Party.MaxHp);
                case BoardMatchTier.FiveOrMore:
                    return combatRules.ResolveMatch5(
                        request.Party.CurrentHp,
                        request.Party.MaxHp).SkillCoefficient;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier));
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
                        nameof(matchTier));
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
                    BeatriceBloodstoneRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Attacker must be Beatrice Bloodstone with Dark element.",
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

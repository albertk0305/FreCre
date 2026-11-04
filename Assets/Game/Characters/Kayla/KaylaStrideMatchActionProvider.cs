using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly KaylaStrideResolvedCombatRules combatRules;

        public KaylaStrideMatchActionProvider(
            KaylaStrideCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public KaylaStrideMatchActionProvider(
            KaylaStrideCombatConfig config,
            int awakening)
        {
            combatRules = KaylaStrideCombatRuleResolver.Resolve(
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

            long damageActionId = actionIds.Next();
            var damage = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Light,
                    AttackType.Match,
                    ResolveMatchTag(matchTier),
                    ResolveMatchCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true));

            if (matchTier != BoardMatchTier.FiveOrMore)
            {
                return new CombatAction[] { damage };
            }

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                KaylaStrideRules.Match5EffectId,
                attacker.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.CriticalDamageIncrease,
                combatRules.Match5CriticalDamageIncreaseRate,
                combatRules.Match5BuffDurationTurns,
                effectActionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                damage,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Match,
                    party,
                    effect)
            };
        }

        private double ResolveMatchCoefficient(BoardMatchTier matchTier)
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
                    KaylaStrideRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Attacker must be Light Kayla Stride.",
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

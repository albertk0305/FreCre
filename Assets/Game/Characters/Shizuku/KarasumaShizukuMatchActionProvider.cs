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

namespace ValorChronicle.Characters.Shizuku
{
    public sealed class KarasumaShizukuMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly KarasumaShizukuResolvedCombatRules combatRules;

        public KarasumaShizukuMatchActionProvider(
            KarasumaShizukuCombatConfig config,
            int awakening)
        {
            combatRules = KarasumaShizukuCombatRuleResolver.Resolve(config, awakening);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
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
            ValidateInputs(attacker, party, boss, matchTier, finalComboCount, actionIds);

            long damageActionId = actionIds.Next();
            var damageAction = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Dark,
                    AttackType.Match,
                    ResolveMatchTag(matchTier),
                    ResolveDamageCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true));

            long effectActionId = actionIds.Next();
            var attackIncreaseEffect = new EffectInstance(
                effectActionId,
                KarasumaShizukuRules.AttackIncreaseEffectId,
                attacker.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                ResolveAttackIncreaseRate(matchTier),
                combatRules.AttackBuffDurationTurns,
                effectActionId,
                EffectStackPolicy.ReplaceWithStronger);

            return new CombatAction[]
            {
                damageAction,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Match,
                    party,
                    attackIncreaseEffect)
            };
        }

        private double ResolveDamageCoefficient(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three: return combatRules.Match3DamageCoefficient;
                case BoardMatchTier.Four: return combatRules.Match4DamageCoefficient;
                case BoardMatchTier.FiveOrMore: return combatRules.Match5DamageCoefficient;
                default: throw new ArgumentOutOfRangeException(nameof(matchTier));
            }
        }

        private double ResolveAttackIncreaseRate(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three: return combatRules.Match3AttackIncreaseRate;
                case BoardMatchTier.Four: return combatRules.Match4AttackIncreaseRate;
                case BoardMatchTier.FiveOrMore: return combatRules.Match5AttackIncreaseRate;
                default: throw new ArgumentOutOfRangeException(nameof(matchTier));
            }
        }

        private static AttackTag ResolveMatchTag(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three: return AttackTag.Match3;
                case BoardMatchTier.Four: return AttackTag.Match4;
                case BoardMatchTier.FiveOrMore: return AttackTag.Match5Plus;
                default: throw new ArgumentOutOfRangeException(nameof(matchTier));
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
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (!string.Equals(attacker.CharacterId, KarasumaShizukuRules.CharacterId, StringComparison.Ordinal)
                || attacker.Element != ElementType.Dark)
                throw new ArgumentException("Attacker must be Dark Karasuma Shizuku.", nameof(attacker));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (!Enum.IsDefined(typeof(BoardMatchTier), matchTier))
                throw new ArgumentOutOfRangeException(nameof(matchTier));
            if (finalComboCount <= 0) throw new ArgumentOutOfRangeException(nameof(finalComboCount));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
        }
    }
}

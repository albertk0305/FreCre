using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lilith
{
    public sealed class LilithMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly LilithResolvedCombatRules combatRules;

        public LilithMatchActionProvider(LilithCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public LilithMatchActionProvider(
            LilithCombatConfig config,
            int awakening)
        {
            combatRules = LilithCombatRuleResolver.Resolve(
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

            int matchSize = ResolveMatchSize(matchTier);
            long damageActionId = actionIds.Next();
            var damage = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Dark,
                    combatRules.MatchAttackType,
                    ResolveMatchTag(matchTier),
                    combatRules.GetMatchCoefficient(matchSize),
                    combatRules.MatchUsesCombo,
                    finalComboCount,
                    canCritical: true));

            int covenantGain =
                combatRules.GetMatchContractGainAmount(matchSize);
            int capacity = 1 + covenantGain
                + (combatRules.CanApplyCorruptedBlessing(matchSize)
                    ? 1
                    : 0);
            var actions = new List<CombatAction>(capacity)
            {
                damage
            };
            for (int index = 0; index < covenantGain; index++)
            {
                long covenantActionId = actionIds.Next();
                actions.Add(new ApplyEffectAction(
                    covenantActionId,
                    ActionOrigin.System,
                    boss,
                    LilithCovenantEffectFactory.Create(
                        covenantActionId,
                        combatRules),
                    damageActionId,
                    damageActionId));
            }

            if (combatRules.CanApplyCorruptedBlessing(matchSize))
            {
                long blessingActionId = actionIds.Next();
                actions.Add(new ApplyEffectAction(
                    blessingActionId,
                    ActionOrigin.System,
                    party,
                    LilithFallenBlessingEffectFactory.Create(
                        blessingActionId,
                        combatRules),
                    damageActionId,
                    damageActionId));
            }

            return actions.AsReadOnly();
        }

        private static int ResolveMatchSize(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return 3;
                case BoardMatchTier.Four:
                    return 4;
                case BoardMatchTier.FiveOrMore:
                    return 5;
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
                    LilithRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Attacker must be Lilith with Dark element.",
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

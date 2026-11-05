using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly HazelSparksResolvedCombatRules combatRules;
        private readonly HazelSparksBattleState battleState;

        public HazelSparksMatchActionProvider(
            HazelSparksCombatConfig config,
            int awakening)
            : this(config, awakening, battleState: null)
        {
        }

        public HazelSparksMatchActionProvider(
            HazelSparksCombatConfig config,
            int awakening,
            HazelSparksBattleState battleState)
        {
            combatRules = HazelSparksCombatRuleResolver.Resolve(
                config,
                awakening);
            this.battleState = battleState;
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ValidateContext(context);
            long actionId = context.ActionIds.Next();
            return new CombatAction[]
            {
                new DamageAction(
                    actionId,
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        context.Character,
                        context.Party,
                        context.Boss,
                        ElementType.Fire,
                        AttackType.Match,
                        ResolveMatchTag(context.MatchEvent.Tier),
                        combatRules.GetDamageCoefficient(
                            context.MatchEvent.Tier),
                        appliesCombo: true,
                        context.FinalComboCount,
                        canCritical: true))
            };
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

        private void ValidateContext(MatchEventActionContext context)
        {
            if (!string.Equals(
                    context.Character.CharacterId,
                    HazelSparksRules.CharacterId,
                    StringComparison.Ordinal)
                || context.Character.Element != ElementType.Fire)
            {
                throw new InvalidOperationException(
                    "Hazel's Match provider requires the canonical Fire "
                        + "Hazel character.");
            }

            if (battleState != null
                && !ReferenceEquals(context.Character, battleState.Owner))
            {
                throw new InvalidOperationException(
                    "Hazel's Match provider and combat state must share "
                        + "the same owner instance.");
            }

            if (!Enum.IsDefined(
                    typeof(BoardMatchTier),
                    context.MatchEvent.Tier))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context.MatchEvent.Tier));
            }
        }
    }
}

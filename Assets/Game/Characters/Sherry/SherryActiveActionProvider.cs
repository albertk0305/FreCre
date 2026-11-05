using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryActiveActionProvider
        : IActiveAbilityActionProvider,
          IActiveAbilityEmptyRootActionProvider,
          IActiveAbilitySuccessCommitter
    {
        private readonly SherryBattleState battleState;
        private readonly SherryResolvedCombatRules combatRules;

        public SherryActiveActionProvider(
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

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(
                context.Character,
                context.Party,
                context.Boss,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, party, boss, actionIds);

            int rankAtUseStart = battleState.StylishRank;
            if (rankAtUseStart == 0)
            {
                return Array.Empty<CombatAction>();
            }

            double jackpotCoefficient =
                combatRules.ResolveJackpotCoefficient(rankAtUseStart);
            double stylishIncreaseRate =
                combatRules.ResolveStylishDamageIncreaseRate(
                    rankAtUseStart);
            long actionId = actionIds.Next();
            return new CombatAction[]
            {
                new DamageAction(
                    actionId,
                    ActionOrigin.Active,
                    new DamageContextBuildRequest(
                        actor,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Active,
                        AttackTag.None,
                        jackpotCoefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: true,
                        actionLocalDealtDamageIncreaseRate:
                            stylishIncreaseRate))
            };
        }

        public void CommitActiveSuccess(
            ActiveAbilityActionContext context)
        {
            battleState.ConsumeAllRank();
        }

        private void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!ReferenceEquals(actor, battleState.Owner)
                || actor.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Actor must be this battle state's Fire Sherry.",
                    nameof(actor));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

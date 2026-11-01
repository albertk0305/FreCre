using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lilith
{
    public sealed class LilithPostBossActionProvider
        : IPostBossActionProvider
    {
        private readonly LilithResolvedCombatRules combatRules;

        public LilithPostBossActionProvider(LilithCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public LilithPostBossActionProvider(
            LilithCombatConfig config,
            int awakening)
        {
            combatRules = LilithCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            PostBossActionContext context)
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
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(attacker, party, boss, actionIds);
            if (party.IsIncapacitated || boss.IsDefeated)
            {
                return Array.Empty<CombatAction>();
            }

            if (!LilithCovenantEffectFactory.TryGetEffect(
                    boss,
                    out var covenant))
            {
                return Array.Empty<CombatAction>();
            }

            LilithContractExplosionResult explosion = combatRules
                .ResolveContractExplosion(covenant.StackCount);
            if (!explosion.ShouldTrigger)
            {
                return Array.Empty<CombatAction>();
            }

            var actions = new List<CombatAction>(
                2 + explosion.RemainingStackCount);
            actions.Add(new DamageAction(
                actionIds.Next(),
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Dark,
                    combatRules.ContractExplosionAttackType,
                    AttackTag.None,
                    explosion.DamageCoefficient,
                    combatRules.ContractExplosionUsesCombo,
                    finalComboCount: 0,
                    canCritical: true)));
            actions.Add(new RemoveEffectAction(
                actionIds.Next(),
                ActionOrigin.System,
                boss,
                covenant.RuntimeId));

            for (int index = 0;
                index < explosion.RemainingStackCount;
                index++)
            {
                long actionId = actionIds.Next();
                actions.Add(new ApplyEffectAction(
                    actionId,
                    ActionOrigin.System,
                    boss,
                    LilithCovenantEffectFactory.Create(
                        actionId,
                        combatRules)));
            }

            return actions.AsReadOnly();
        }

        private static void ValidateInputs(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
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

            bool belongsToParty = false;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], attacker))
                {
                    belongsToParty = true;
                    break;
                }
            }

            if (!belongsToParty)
            {
                throw new ArgumentException(
                    "Attacker must belong to the target party.",
                    nameof(attacker));
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

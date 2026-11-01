using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly BeatriceBloodstoneResolvedCombatRules combatRules;

        public BeatriceBloodstoneActiveActionProvider(
            BeatriceBloodstoneCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public BeatriceBloodstoneActiveActionProvider(
            BeatriceBloodstoneCombatConfig config,
            int awakening)
        {
            combatRules = BeatriceBloodstoneCombatRuleResolver.Resolve(
                config,
                awakening);
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

            return new CombatAction[]
            {
                new DamageAction(
                    actionIds.Next(),
                    ActionOrigin.Active,
                    new DamageContextBuildRequest(
                        actor,
                        party,
                        boss,
                        ElementType.Dark,
                        AttackType.Active,
                        AttackTag.None,
                        combatRules.ActiveBaseCoefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: true,
                        skillCoefficientResolver: request =>
                            combatRules.ResolveActive(
                                request.Party.CurrentHp,
                                request.Party.MaxHp).SkillCoefficient,
                        dealtDamageIncreaseRateResolver: request =>
                            combatRules
                                .ResolvePassiveDealtDamageIncreaseRate(
                                    request.Party.CurrentHp,
                                    request.Party.MaxHp)))
            };
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    BeatriceBloodstoneRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Dark)
            {
                throw new ArgumentException(
                    "Actor must be Beatrice Bloodstone with Dark element.",
                    nameof(actor));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            bool belongsToParty = false;
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], actor))
                {
                    belongsToParty = true;
                    break;
                }
            }

            if (!belongsToParty)
            {
                throw new ArgumentException(
                    "Actor must belong to the target party.",
                    nameof(actor));
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

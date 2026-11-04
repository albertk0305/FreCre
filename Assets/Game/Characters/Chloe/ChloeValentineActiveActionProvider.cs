using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Chloe
{
    public sealed class ChloeValentineActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly ChloeValentineResolvedCombatRules rules;

        public ChloeValentineActiveActionProvider(
            ChloeValentineCombatConfig config,
            int awakening)
        {
            rules = ChloeValentineCombatRuleResolver.Resolve(config, awakening);
        }

        public int CooldownTurns => rules.ActiveCooldownTurns;

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
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(source, party, actionIds);

            long healActionId = actionIds.Next();
            var heal = new HealAction(
                healActionId,
                ActionOrigin.Active,
                new HealingContextBuildRequest(
                    source,
                    party,
                    rules.ActiveHealingCoefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    rules.ActionLocalHealingIncreaseRate));
            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                ChloeValentineRules.ActiveCriticalDamageEffectId,
                source.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.CriticalDamageIncrease,
                rules.ActiveCriticalDamageIncreaseRate,
                rules.ActiveBuffDurationTurns,
                effectActionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                heal,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Active,
                    party,
                    effect)
            };
        }

        private static void ValidateInputs(
            CharacterBattleState source,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!string.Equals(
                    source.CharacterId,
                    ChloeValentineRules.CharacterId,
                    StringComparison.Ordinal)
                || source.Element != ElementType.Light)
            {
                throw new ArgumentException(
                    "Source must be Light Chloe Valentine.",
                    nameof(source));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}


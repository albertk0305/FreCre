using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Bosses.Fenrir
{
    public sealed class FenrirBossCombatActionProvider
        : IBossCombatActionProvider
    {
        private readonly FenrirBattleRuntimeState runtimeState;

        public FenrirBossCombatActionProvider(
            FenrirBattleRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState
                ?? throw new ArgumentNullException(nameof(runtimeState));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            BossCombatActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(
                    context.Boss.BossId,
                    FenrirRules.BossId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Fenrir BossBattleState is required.",
                    nameof(context));
            }

            runtimeState.EnsureStableState(context.Party);
            var actions = new List<CombatAction>(4);
            bool isFrenzied = runtimeState.IsFrenzied;
            double coefficient = isFrenzied
                ? runtimeState.Config.RampagingClawCoefficient
                : runtimeState.Config.SavageRendCoefficient;
            actions.Add(new BossDamageAction(
                context.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    context.Boss,
                    context.Party,
                    coefficient,
                    AttackTag.None,
                    BossDamageHitType.Direct)));

            if (isFrenzied)
            {
                return actions.AsReadOnly();
            }

            int woundCount = runtimeState.GetWoundStackCount(context.Party);
            if (woundCount < runtimeState.Config.WoundThreshold - 1)
            {
                long woundActionId = context.ActionIds.Next();
                actions.Add(new ApplyEffectAction(
                    woundActionId,
                    ActionOrigin.System,
                    context.Party,
                    FenrirEffectFactory.CreateWound(
                        woundActionId,
                        runtimeState.Config)));
                return actions.AsReadOnly();
            }

            if (woundCount != runtimeState.Config.WoundThreshold - 1
                || !runtimeState.TryGetWoundEffect(
                    context.Party,
                    out EffectInstance wound))
            {
                throw new InvalidOperationException(
                    "Fenrir Wound state cannot enter Frenzy.");
            }

            actions.Add(new ConsumeEffectStackAction(
                context.ActionIds.Next(),
                ActionOrigin.System,
                context.Party,
                wound.RuntimeId,
                wound.StackCount));
            long attackActionId = context.ActionIds.Next();
            actions.Add(new ApplyEffectAction(
                attackActionId,
                ActionOrigin.System,
                context.Boss,
                FenrirEffectFactory.CreateFrenzyAttack(
                    attackActionId,
                    runtimeState.Config)));
            long frenzyActionId = context.ActionIds.Next();
            actions.Add(new ApplyEffectAction(
                frenzyActionId,
                ActionOrigin.System,
                context.Boss,
                FenrirEffectFactory.CreateFrenzy(
                    frenzyActionId,
                    runtimeState.Config)));
            return actions.AsReadOnly();
        }
    }
}

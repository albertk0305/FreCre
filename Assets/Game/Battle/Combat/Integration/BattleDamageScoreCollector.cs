using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BattleDamageScoreCollector
    {
        private readonly BattleDamageScoreAccumulator accumulator;
        private readonly HashSet<long> collectedActionIds =
            new HashSet<long>();

        public BattleDamageScoreCollector(
            BattleDamageScoreAccumulator accumulator)
        {
            this.accumulator = accumulator
                ?? throw new ArgumentNullException(nameof(accumulator));
        }

        public long DamageScore => accumulator.DamageScore;
        public long CumulativeAppliedDamage =>
            accumulator.CumulativeAppliedDamage;
        public long CumulativeAppliedBossHealing =>
            accumulator.CumulativeAppliedBossHealing;

        public long CollectAppliedAction(CombatActionResult actionResult)
        {
            if (actionResult == null)
            {
                throw new ArgumentNullException(nameof(actionResult));
            }

            if (actionResult.Action == null)
            {
                throw new ArgumentException(
                    "An action result must reference its action.",
                    nameof(actionResult));
            }

            long actionId = actionResult.Action.ActionId;
            if (!collectedActionIds.Add(actionId))
            {
                return 0L;
            }

            long appliedDamage = actionResult is DamageActionResult damage
                ? damage.AppliedDamage
                : 0L;
            long appliedBossHealing =
                actionResult is BossHealActionResult healing
                    ? healing.AppliedHealing
                    : 0L;
            try
            {
                accumulator.Apply(appliedDamage, appliedBossHealing);
            }
            catch
            {
                collectedActionIds.Remove(actionId);
                throw;
            }

            return appliedDamage;
        }

        public long Collect(CombatActionExecutionResult executionResult)
        {
            if (executionResult == null)
            {
                throw new ArgumentNullException(nameof(executionResult));
            }

            var actionIdsInResult = new HashSet<long>();
            var newActionIds = new List<long>();
            long appliedDamage = 0L;
            long appliedBossHealing = 0L;
            IReadOnlyList<CombatActionResult> results =
                executionResult.ActionResults;
            for (int index = 0; index < results.Count; index++)
            {
                CombatActionResult result = results[index];
                if (result == null || result.Action == null)
                {
                    throw new ArgumentException(
                        "Execution result cannot contain a null action result.",
                        nameof(executionResult));
                }

                long actionId = result.Action.ActionId;
                if (!actionIdsInResult.Add(actionId))
                {
                    throw new ArgumentException(
                        $"Execution result contains duplicate action ID "
                            + $"{actionId}.",
                        nameof(executionResult));
                }

                if (collectedActionIds.Contains(actionId))
                {
                    continue;
                }

                newActionIds.Add(actionId);
                if (result is DamageActionResult damageResult)
                {
                    appliedDamage = checked(
                        appliedDamage + damageResult.AppliedDamage);
                }
                else if (result is BossHealActionResult healingResult)
                {
                    appliedBossHealing = checked(
                        appliedBossHealing
                            + healingResult.AppliedHealing);
                }
            }

            accumulator.Apply(appliedDamage, appliedBossHealing);
            for (int index = 0; index < newActionIds.Count; index++)
            {
                collectedActionIds.Add(newActionIds[index]);
            }

            return appliedDamage;
        }
    }
}

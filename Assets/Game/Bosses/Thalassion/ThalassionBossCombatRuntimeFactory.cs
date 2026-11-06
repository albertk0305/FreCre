using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Thalassion
{
    public sealed class ThalassionBossCombatRuntimeFactory
        : IBossCombatRuntimeFactory
    {
        public string BossId => ThalassionRules.BossId;

        public bool TryValidate(
            BossDefinition definition,
            out string errorMessage)
        {
            if (definition == null)
            {
                errorMessage = "BossDefinition is missing.";
                return false;
            }

            if (!string.Equals(
                    definition.Id,
                    BossId,
                    StringComparison.Ordinal))
            {
                errorMessage = $"BossDefinition '{definition.Id}' is not "
                    + "supported by the Thalassion runtime factory.";
                return false;
            }

            if (definition.Element != ElementType.Water)
            {
                errorMessage = "Thalassion requires the Water element.";
                return false;
            }

            if (!(definition.CombatConfig
                is ThalassionCombatConfig config))
            {
                errorMessage = "Thalassion requires a "
                    + "ThalassionCombatConfig.";
                return false;
            }

            if (!config.TryValidate(out string configError))
            {
                errorMessage = "Thalassion combat config is invalid. "
                    + configError;
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public BossCombatRuntime Create(
            BossDefinition definition,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            if (!TryValidate(definition, out string errorMessage))
            {
                throw new ArgumentException(
                    errorMessage,
                    nameof(definition));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (!string.Equals(
                    boss.BossId,
                    definition.Id,
                    StringComparison.Ordinal)
                || boss.Element != definition.Element)
            {
                throw new ArgumentException(
                    "BossBattleState must match the validated definition.",
                    nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            var runtimeState = new ThalassionBattleRuntimeState(
                boss,
                (ThalassionCombatConfig)definition.CombatConfig);
            var actionProvider =
                new ThalassionBossCombatActionProvider(runtimeState);
            var shieldHitRule = new ThalassionElementalShieldHitRule(
                runtimeState,
                actionIds);
            return new BossCombatRuntime(
                actionProvider,
                runtimeState,
                runtimeState,
                new ICombatTriggerRule[] { shieldHitRule });
        }
    }
}

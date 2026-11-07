using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Fenrir
{
    public sealed class FenrirBossCombatRuntimeFactory
        : IBossCombatRuntimeFactory
    {
        public string BossId => FenrirRules.BossId;

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
                    + "supported by the Fenrir runtime factory.";
                return false;
            }

            if (definition.Element != ElementType.Dark)
            {
                errorMessage = "Fenrir requires the Dark element.";
                return false;
            }

            if (!(definition.CombatConfig is FenrirCombatConfig config))
            {
                errorMessage = "Fenrir requires a FenrirCombatConfig.";
                return false;
            }

            if (!config.TryValidate(out string configError))
            {
                errorMessage = "Fenrir combat config is invalid. "
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
                throw new ArgumentException(errorMessage, nameof(definition));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
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

            var runtimeState = new FenrirBattleRuntimeState(
                boss,
                (FenrirCombatConfig)definition.CombatConfig);
            var actionProvider =
                new FenrirBossCombatActionProvider(runtimeState);
            var criticalHitRule = new FenrirFrenzyCriticalHitRule(
                runtimeState,
                actionIds);
            return new BossCombatRuntime(
                actionProvider,
                runtimeState,
                runtimeState,
                new ICombatTriggerRule[] { criticalHitRule });
        }
    }
}

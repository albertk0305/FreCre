using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Orver
{
    public sealed class OrverBossCombatRuntimeFactory
        : IBossCombatRuntimeFactory
    {
        public string BossId => OrverRules.BossId;

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
                    + "supported by the Orver runtime factory.";
                return false;
            }

            if (!(definition.CombatConfig is OrverCombatConfig config))
            {
                errorMessage = "Orver requires an OrverCombatConfig.";
                return false;
            }

            if (definition.Element != ElementType.Grass)
            {
                errorMessage = "Orver requires the Grass element.";
                return false;
            }

            if (!config.TryValidate(out string configError))
            {
                errorMessage = "Orver combat config is invalid. "
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

            var runtimeState = new OrverBattleRuntimeState(
                (OrverCombatConfig)definition.CombatConfig);
            var actionProvider = new OrverBossCombatActionProvider(
                runtimeState);
            return new BossCombatRuntime(
                actionProvider,
                actionProvider);
        }
    }
}

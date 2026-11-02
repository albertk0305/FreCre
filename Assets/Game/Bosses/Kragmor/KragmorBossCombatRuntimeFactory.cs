using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBossCombatRuntimeFactory
        : IBossCombatRuntimeFactory
    {
        public string BossId => KragmorRules.BossId;

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
                    + $"supported by the {nameof(KragmorBossCombatRuntimeFactory)}.";
                return false;
            }

            if (definition.Element != ElementType.Fire)
            {
                errorMessage = "Kragmor requires the Fire element.";
                return false;
            }

            if (!(definition.CombatConfig is KragmorCombatConfig config))
            {
                errorMessage = "Kragmor requires a KragmorCombatConfig.";
                return false;
            }

            if (!config.TryValidate(out string configError))
            {
                errorMessage = "Kragmor combat config is invalid. "
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
                    "BossBattleState must match the validated "
                        + "BossDefinition.",
                    nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            var config = (KragmorCombatConfig)definition.CombatConfig;
            var runtimeState = new KragmorBattleRuntimeState(config);
            KragmorDefenseEffectFactory.InitializeBattle(
                boss,
                runtimeState,
                actionIds);
            var actionProvider = new KragmorBossCombatActionProvider(
                runtimeState);
            return new BossCombatRuntime(
                actionProvider,
                (IBossIntentSource)actionProvider,
                runtimeState);
        }
    }
}

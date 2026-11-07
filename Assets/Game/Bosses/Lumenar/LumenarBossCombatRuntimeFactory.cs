using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Lumenar
{
    public sealed class LumenarBossCombatRuntimeFactory : IBossCombatRuntimeFactory
    {
        public string BossId => LumenarRules.BossId;

        public bool TryValidate(BossDefinition definition, out string errorMessage)
        {
            if (definition == null)
            {
                errorMessage = "BossDefinition is missing.";
                return false;
            }
            if (!string.Equals(definition.Id, BossId, StringComparison.Ordinal) || definition.Element != ElementType.Light)
            {
                errorMessage = "Lumenar requires ID 'lumenar' and the Light element.";
                return false;
            }
            if (!(definition.CombatConfig is LumenarCombatConfig config))
            {
                errorMessage = "Lumenar requires a LumenarCombatConfig.";
                return false;
            }
            if (!config.TryValidate(out string configError))
            {
                errorMessage = "Lumenar combat config is invalid. " + configError;
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

        public BossCombatRuntime Create(BossDefinition definition, BossBattleState boss, CombatActionIdSequence actionIds)
        {
            if (!TryValidate(definition, out string errorMessage)) throw new ArgumentException(errorMessage, nameof(definition));
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (!string.Equals(boss.BossId, definition.Id, StringComparison.Ordinal) || boss.Element != definition.Element)
                throw new ArgumentException("BossBattleState must match the definition.", nameof(boss));
            var config = (LumenarCombatConfig)definition.CombatConfig;
            var state = new LumenarBattleRuntimeState(boss, config);
            LumenarEffectFactory.InitializeBattle(boss, config, actionIds);
            var provider = new LumenarBossCombatActionProvider(state);
            var transition = new LumenarHealthTransitionRule(state, actionIds);
            return new BossCombatRuntime(provider, state, state, new ICombatTriggerRule[] { transition });
        }
    }
}

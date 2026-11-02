using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Entry
{
    /// <summary>Contains validated content required to compose one battle.</summary>
    public sealed class BattleEncounterDefinition
    {
        internal BattleEncounterDefinition(
            BossDefinition boss,
            BossDifficultyStats difficulty,
            IBossCombatRuntimeFactory runtimeFactory)
        {
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Difficulty = difficulty
                ?? throw new ArgumentNullException(nameof(difficulty));
            RuntimeFactory = runtimeFactory
                ?? throw new ArgumentNullException(nameof(runtimeFactory));
            if (!string.Equals(
                    RuntimeFactory.BossId,
                    Boss.Id,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Runtime Factory Boss ID must match the "
                        + "BossDefinition ID.",
                    nameof(runtimeFactory));
            }

            if (!RuntimeFactory.TryValidate(Boss, out string errorMessage))
            {
                throw new ArgumentException(
                    "Runtime Factory rejected the BossDefinition. "
                        + errorMessage,
                    nameof(runtimeFactory));
            }
        }

        public BossDefinition Boss { get; }
        public BossDifficultyStats Difficulty { get; }
        public IBossCombatRuntimeFactory RuntimeFactory { get; }
    }
}

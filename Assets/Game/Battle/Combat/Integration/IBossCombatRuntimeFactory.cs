using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossCombatRuntimeFactory
    {
        string BossId { get; }

        bool TryValidate(
            BossDefinition definition,
            out string errorMessage);

        BossCombatRuntime Create(
            BossDefinition definition,
            BossBattleState boss,
            CombatActionIdSequence actionIds);
    }
}

using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Results;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Entry
{
    /// <summary>Resolves supported battle content without creating runtime state.</summary>
    public static class BattleEncounterFactory
    {
        public static bool IsSupportedBoss(
            BossDefinition boss,
            out string error)
        {
            return IsSupportedBoss(
                boss,
                BossCombatRuntimeFactoryCatalog.CreateDefault(),
                out error);
        }

        public static bool IsSupportedBoss(
            BossDefinition boss,
            BossCombatRuntimeFactoryCatalog runtimeFactories,
            out string error)
        {
            if (boss == null)
            {
                error = "BossDefinition is missing.";
                return false;
            }

            if (runtimeFactories == null)
            {
                error = "A Boss runtime Factory catalog is required.";
                return false;
            }

            if (!runtimeFactories.TryGetFactory(
                    boss.Id,
                    out IBossCombatRuntimeFactory runtimeFactory))
            {
                error = $"Boss '{boss.Id}' has no implemented battle "
                    + "composition.";
                return false;
            }

            if (!runtimeFactory.TryValidate(boss, out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool TryCreate(
            DefinitionDatabase database,
            BattleEntryRequest request,
            out BattleEncounterDefinition encounter,
            out string error)
        {
            return TryCreate(
                database,
                request,
                BossCombatRuntimeFactoryCatalog.CreateDefault(),
                out encounter,
                out error);
        }

        public static bool TryCreate(
            DefinitionDatabase database,
            BattleEntryRequest request,
            BossCombatRuntimeFactoryCatalog runtimeFactories,
            out BattleEncounterDefinition encounter,
            out string error)
        {
            encounter = null;
            if (database == null || !database.IsInitialized)
            {
                error = "An initialized DefinitionDatabase is required.";
                return false;
            }

            if (request == null)
            {
                error = "BattleEntryRequest is missing.";
                return false;
            }

            if (!database.TryGetBoss(
                request.BossId,
                out BossDefinition boss))
            {
                error = $"BossDefinition '{request.BossId}' was not found.";
                return false;
            }

            if (!IsKnownDifficulty(request.DifficultyId))
            {
                error = $"DifficultyId '{request.DifficultyId}' is not recognized.";
                return false;
            }

            if (runtimeFactories == null)
            {
                error = "A Boss runtime Factory catalog is required.";
                return false;
            }

            if (!runtimeFactories.TryGetFactory(
                    boss.Id,
                    out IBossCombatRuntimeFactory runtimeFactory))
            {
                error = $"Boss '{boss.Id}' has no implemented battle "
                    + "composition.";
                return false;
            }

            if (!runtimeFactory.TryValidate(boss, out error))
            {
                return false;
            }

            if (!boss.TryGetDifficultyStats(
                request.DifficultyId,
                out BossDifficultyStats difficulty))
            {
                error = $"Boss difficulty is not available. BossId={boss.Id}; "
                    + $"DifficultyId={request.DifficultyId}.";
                return false;
            }

            encounter = new BattleEncounterDefinition(
                boss,
                difficulty,
                runtimeFactory);
            error = string.Empty;
            return true;
        }

        private static bool IsKnownDifficulty(string difficultyId)
        {
            for (int index = 0;
                index < BattleDifficultyIds.RequiredIds.Count;
                index++)
            {
                if (string.Equals(
                    BattleDifficultyIds.RequiredIds[index],
                    difficultyId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

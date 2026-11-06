using System;
using System.Collections.Generic;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Bosses.Thalassion;
using ValorChronicle.Core.IDs;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossCombatRuntimeFactoryCatalog
    {
        private readonly Dictionary<string, IBossCombatRuntimeFactory>
            factoriesByBossId =
                new Dictionary<string, IBossCombatRuntimeFactory>(
                    StringComparer.Ordinal);

        public int Count => factoriesByBossId.Count;

        public static BossCombatRuntimeFactoryCatalog CreateDefault()
        {
            var catalog = new BossCombatRuntimeFactoryCatalog();
            catalog.Register(new KragmorBossCombatRuntimeFactory());
            catalog.Register(new ThalassionBossCombatRuntimeFactory());
            return catalog;
        }

        public void Register(IBossCombatRuntimeFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (!ContentIdValidator.TryValidate(
                    factory.BossId,
                    out string idError))
            {
                throw new ArgumentException(
                    $"Boss runtime Factory has an invalid Boss ID. "
                        + idError,
                    nameof(factory));
            }

            if (factoriesByBossId.ContainsKey(factory.BossId))
            {
                throw new ArgumentException(
                    $"A Boss runtime Factory for '{factory.BossId}' is "
                        + "already registered.",
                    nameof(factory));
            }

            factoriesByBossId.Add(factory.BossId, factory);
        }

        public bool TryGetFactory(
            string bossId,
            out IBossCombatRuntimeFactory factory)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                factory = null;
                return false;
            }

            return factoriesByBossId.TryGetValue(bossId, out factory);
        }
    }
}

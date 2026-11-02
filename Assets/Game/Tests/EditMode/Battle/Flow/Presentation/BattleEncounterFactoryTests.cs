using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleEncounterFactoryTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DefaultCatalogResolvesKragmorFactory()
        {
            KragmorCombatConfig config = KragmorTestConfig.Create();
            createdObjects.Add(config);
            BossDefinition boss = CreateBoss(
                KragmorRules.BossId,
                ElementType.Fire,
                config,
                NormalStats());
            DefinitionDatabase database = CreateDatabase(boss);

            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest(
                    KragmorRules.BossId,
                    BattleDifficultyIds.Normal),
                out BattleEncounterDefinition encounter,
                out string error);

            Assert.That(created, Is.True, error);
            Assert.That(encounter.Boss, Is.SameAs(boss));
            Assert.That(encounter.Difficulty.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Normal));
            Assert.That(encounter.RuntimeFactory,
                Is.TypeOf<KragmorBossCombatRuntimeFactory>());
        }

        [Test]
        public void InjectedCatalogResolvesSecondBossFactory()
        {
            BossDefinition boss = CreateBoss(
                "test_boss",
                ElementType.Water,
                null,
                NormalStats());
            DefinitionDatabase database = CreateDatabase(boss);
            var runtimeFactory = new TestBossRuntimeFactory("test_boss");
            var catalog = new BossCombatRuntimeFactoryCatalog();
            catalog.Register(runtimeFactory);

            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest(
                    "test_boss",
                    BattleDifficultyIds.Normal),
                catalog,
                out BattleEncounterDefinition encounter,
                out string error);

            Assert.That(created, Is.True, error);
            Assert.That(encounter.RuntimeFactory,
                Is.SameAs(runtimeFactory));
            Assert.That(runtimeFactory.ValidateCount, Is.GreaterThan(0));
        }

        [Test]
        public void UnregisteredBossIsRejectedWithoutRuntimeCreation()
        {
            BossDefinition boss = CreateBoss(
                "test_boss",
                ElementType.Water,
                null,
                NormalStats());
            DefinitionDatabase database = CreateDatabase(boss);

            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest(
                    "test_boss",
                    BattleDifficultyIds.Normal),
                new BossCombatRuntimeFactoryCatalog(),
                out BattleEncounterDefinition encounter,
                out string error);

            Assert.That(created, Is.False);
            Assert.That(encounter, Is.Null);
            Assert.That(error, Does.Contain("no implemented"));
        }

        [Test]
        public void InvalidKragmorDefinitionIsRejected()
        {
            BossDefinition boss = CreateBoss(
                KragmorRules.BossId,
                ElementType.Fire,
                null,
                NormalStats());
            DefinitionDatabase database = CreateDatabase(boss);

            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest(
                    KragmorRules.BossId,
                    BattleDifficultyIds.Normal),
                out BattleEncounterDefinition encounter,
                out string error);

            Assert.That(created, Is.False);
            Assert.That(encounter, Is.Null);
            Assert.That(error, Does.Contain("KragmorCombatConfig"));
        }

        [TestCase("difficulty_unknown")]
        [TestCase(BattleDifficultyIds.Hard)]
        public void MissingOrUnknownDifficultyIsRejected(string difficultyId)
        {
            BossDefinition boss = CreateBoss(
                "test_boss",
                ElementType.Water,
                null,
                NormalStats());
            DefinitionDatabase database = CreateDatabase(boss);
            var catalog = new BossCombatRuntimeFactoryCatalog();
            catalog.Register(new TestBossRuntimeFactory("test_boss"));

            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest("test_boss", difficultyId),
                catalog,
                out BattleEncounterDefinition encounter,
                out _);

            Assert.That(created, Is.False);
            Assert.That(encounter, Is.Null);
        }

        private BossDefinition CreateBoss(
            string id,
            ElementType element,
            CombatConfigDefinition combatConfig,
            params BossDifficultyStats[] difficultyStats)
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(boss);
            SetField(boss, "id", id);
            SetField(boss, "element", element);
            SetField(boss, "turnLimit", 25);
            SetField(boss, "combatConfig", combatConfig);
            SetField(boss, "difficultyStats", difficultyStats);
            return boss;
        }

        private DefinitionDatabase CreateDatabase(BossDefinition boss)
        {
            var database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "bosses", new[] { boss });
            database.Initialize();
            return database;
        }

        private static BossDifficultyStats NormalStats()
        {
            return new BossDifficultyStats(
                BattleDifficultyIds.Normal,
                12000,
                350d);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class TestBossRuntimeFactory
            : IBossCombatRuntimeFactory
        {
            public TestBossRuntimeFactory(string bossId)
            {
                BossId = bossId;
            }

            public string BossId { get; }
            public int ValidateCount { get; private set; }

            public bool TryValidate(
                BossDefinition definition,
                out string errorMessage)
            {
                ValidateCount++;
                bool valid = definition != null
                    && string.Equals(
                        definition.Id,
                        BossId,
                        StringComparison.Ordinal);
                errorMessage = valid ? string.Empty : "Boss ID mismatch.";
                return valid;
            }

            public BossCombatRuntime Create(
                BossDefinition definition,
                BossBattleState boss,
                CombatActionIdSequence actionIds)
            {
                throw new InvalidOperationException(
                    "Encounter creation must not create runtime state.");
            }
        }
    }
}

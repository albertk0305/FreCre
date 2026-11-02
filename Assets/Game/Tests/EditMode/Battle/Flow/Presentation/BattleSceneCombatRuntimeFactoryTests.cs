using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatRuntimeFactoryTests
    {
        private const string MareaDefinitionPath =
            "Assets/Data/Characters/marea_bluefang.asset";

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
        public void CompositionCreatesRuntimeExactlyOnceAndUsesCommonContracts()
        {
            var runtimeFactory = new TestBossRuntimeFactory("test_boss");
            BattleEncounterDefinition encounter = CreateEncounter(
                runtimeFactory);

            using BattleSceneCombatComposition composition = Compose(
                encounter);

            Assert.That(runtimeFactory.CreateCount, Is.EqualTo(1));
            Assert.That(composition.BossRuntime,
                Is.SameAs(runtimeFactory.CreatedRuntimes[0]));
            Assert.That(composition.CombatActionProvider,
                Is.SameAs(runtimeFactory.CreatedProviders[0]));
            Assert.That(composition.BossIntentSource,
                Is.SameAs(runtimeFactory.CreatedProviders[0]));
            Assert.That(composition.Boss.BossId, Is.EqualTo("test_boss"));
            Assert.That(composition.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Normal));
            Assert.That(composition.Boss.MaxHp, Is.EqualTo(12000));
            Assert.That(composition.Boss.Attack, Is.EqualTo(350d));
            Assert.That(composition.FlowSetup.TurnLimit, Is.EqualTo(25));
            Assert.That(composition.BossDefinition.CombatConfig,
                Is.TypeOf<TestBossCombatConfig>());
            Assert.That(composition.KragmorRuntimeState, Is.Null);
            Assert.That(composition.BossActionProvider, Is.Null);

            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge bridge =
                composition.CreateBridge(coordinator);
            Assert.That(
                GetField<IBossCombatActionProvider>(
                    bridge,
                    "bossActionProvider"),
                Is.SameAs(runtimeFactory.CreatedProviders[0]));
            Assert.That(
                GetField<string>(bridge, "difficultyId"),
                Is.EqualTo(BattleDifficultyIds.Normal));
            Assert.That(
                GetField<BossBattleState>(bridge, "boss").BossId,
                Is.EqualTo("test_boss"));
            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(coordinator.NotifyBossDefeated(), Is.True);
            Assert.That(bridge.LastFinalResult.BossId,
                Is.EqualTo("test_boss"));
            Assert.That(bridge.LastFinalResult.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Normal));
        }

        [Test]
        public void SeparateCompositionsReceiveSeparateRuntimeInstances()
        {
            var runtimeFactory = new TestBossRuntimeFactory("test_boss");
            BattleEncounterDefinition encounter = CreateEncounter(
                runtimeFactory);

            using BattleSceneCombatComposition first = Compose(encounter);
            using BattleSceneCombatComposition second = Compose(encounter);

            Assert.That(runtimeFactory.CreateCount, Is.EqualTo(2));
            Assert.That(first.BossRuntime, Is.Not.SameAs(second.BossRuntime));
            Assert.That(first.CombatActionProvider,
                Is.Not.SameAs(second.CombatActionProvider));
            Assert.That(first.Boss, Is.Not.SameAs(second.Boss));
        }

        [Test]
        public void BootstrapExposesCommonIntentSourceWithoutBossCasting()
        {
            var runtimeFactory = new TestBossRuntimeFactory("test_boss");
            BattleEncounterDefinition encounter = CreateEncounter(
                runtimeFactory);
            using BattleSceneCombatComposition composition = Compose(
                encounter);
            var root = new GameObject("CommonBossBootstrapTest");
            root.SetActive(false);
            createdObjects.Add(root);
            BattleSceneCombatBootstrap bootstrap =
                root.AddComponent<BattleSceneCombatBootstrap>();

            Assert.That(bootstrap.BossIntentSource, Is.Null);
            SetProperty(bootstrap, "CombatComposition", composition);

            Assert.That(bootstrap.BossIntentSource,
                Is.SameAs(runtimeFactory.CreatedProviders[0]));
            Assert.That(bootstrap.BossVisualStateSource, Is.Null);
        }

        private BattleEncounterDefinition CreateEncounter(
            TestBossRuntimeFactory runtimeFactory)
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            createdObjects.Add(boss);
            SetField(boss, "id", runtimeFactory.BossId);
            SetField(boss, "element", ElementType.Water);
            SetField(boss, "turnLimit", 25);
            var config =
                ScriptableObject.CreateInstance<TestBossCombatConfig>();
            createdObjects.Add(config);
            SetField(boss, "combatConfig", config);
            SetField(
                boss,
                "difficultyStats",
                new[]
                {
                    new BossDifficultyStats(
                        BattleDifficultyIds.Normal,
                        12000,
                        350d)
                });

            var database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "bosses", new[] { boss });
            database.Initialize();

            var catalog = new BossCombatRuntimeFactoryCatalog();
            catalog.Register(runtimeFactory);
            bool created = BattleEncounterFactory.TryCreate(
                database,
                new BattleEntryRequest(
                    runtimeFactory.BossId,
                    BattleDifficultyIds.Normal),
                catalog,
                out BattleEncounterDefinition encounter,
                out string error);
            Assert.That(created, Is.True, error);
            return encounter;
        }

        private static BattleSceneCombatComposition Compose(
            BattleEncounterDefinition encounter)
        {
            CharacterDefinition marea =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    MareaDefinitionPath);
            Assert.That(marea, Is.Not.Null);
            var member = new BattlePartyMemberInput(
                MareaBluefangRules.CharacterId,
                partySlotIndex: 0,
                level: 1,
                awakening: 0,
                characterDefinition: marea);
            var registrars =
                new CharacterCombatProviderRegistrarCatalog(
                    new ICharacterCombatProviderRegistrar[]
                    {
                        new MareaBluefangCombatProviderRegistrar()
                    });
            return new BattleSceneCombatComposition(
                new[] { member },
                registrars,
                encounter,
                new SeededRandomSource(1),
                BattleResultBalanceDefaults.Create());
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

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(target, value);
        }

        private sealed class TestBossRuntimeFactory
            : IBossCombatRuntimeFactory
        {
            public TestBossRuntimeFactory(string bossId)
            {
                BossId = bossId;
            }

            public string BossId { get; }
            public int CreateCount { get; private set; }
            public List<TestBossActionProvider> CreatedProviders { get; } =
                new List<TestBossActionProvider>();
            public List<BossCombatRuntime> CreatedRuntimes { get; } =
                new List<BossCombatRuntime>();

            public bool TryValidate(
                BossDefinition definition,
                out string errorMessage)
            {
                bool valid = definition != null
                    && string.Equals(
                        definition.Id,
                        BossId,
                        StringComparison.Ordinal)
                    && definition.CombatConfig
                        is TestBossCombatConfig;
                errorMessage = valid
                    ? string.Empty
                    : "Boss ID or CombatConfig mismatch.";
                return valid;
            }

            public BossCombatRuntime Create(
                BossDefinition definition,
                BossBattleState boss,
                CombatActionIdSequence actionIds)
            {
                CreateCount++;
                var provider = new TestBossActionProvider();
                var runtime = new BossCombatRuntime(provider, provider);
                CreatedProviders.Add(provider);
                CreatedRuntimes.Add(runtime);
                return runtime;
            }
        }

        private sealed class TestBossActionProvider
            : IBossCombatActionProvider,
                IBossIntentSource
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return Array.Empty<CombatAction>();
            }

            public IReadOnlyList<BossIntentPreview> GetIntentForecast(
                int count)
            {
                return count <= 0
                    ? Array.Empty<BossIntentPreview>()
                    : new[]
                    {
                        new BossIntentPreview(1, "test_boss_action")
                    };
            }
        }

        private sealed class TestBossCombatConfig
            : CombatConfigDefinition
        {
            public override bool TryValidate(out string errorMessage)
            {
                errorMessage = string.Empty;
                return true;
            }
        }
    }
}

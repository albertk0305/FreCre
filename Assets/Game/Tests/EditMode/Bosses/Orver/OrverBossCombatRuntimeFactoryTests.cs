using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Orver;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Orver
{
    public sealed class OrverBossCombatRuntimeFactoryTests
    {
        [Test]
        public void DefaultCatalogRegistersStatelessOrverFactory()
        {
            BossCombatRuntimeFactoryCatalog catalog =
                BossCombatRuntimeFactoryCatalog.CreateDefault();

            Assert.That(catalog.TryGetFactory(
                OrverRules.BossId,
                out IBossCombatRuntimeFactory factory), Is.True);
            Assert.That(factory,
                Is.TypeOf<OrverBossCombatRuntimeFactory>());
            Assert.That(factory, Is.Not.InstanceOf<IBossBoardStateConsumer>());
        }

        [Test]
        public void CreateBuildsRuntimeWhoseProviderRequiresBattleBinding()
        {
            OrverCombatConfig config = OrverTestConfig.Create();
            BossDefinition definition = CreateDefinition(config);
            var boss = new BossBattleState(
                OrverRules.BossId,
                ElementType.Grass,
                100000,
                500d);
            var factory = new OrverBossCombatRuntimeFactory();

            BossCombatRuntime runtime = factory.Create(
                definition,
                boss,
                new CombatActionIdSequence());

            Assert.That(runtime.ActionProvider,
                Is.TypeOf<OrverBossCombatActionProvider>());
            Assert.That(runtime.IntentSource,
                Is.SameAs(runtime.ActionProvider));
            Assert.That(runtime.ActionProvider,
                Is.InstanceOf<IBossBoardStateConsumer>());
            Assert.Throws<ArgumentException>(() => factory.Create(
                null,
                boss,
                new CombatActionIdSequence()));
            Assert.Throws<InvalidOperationException>(() =>
                runtime.IntentSource.GetIntentForecast(1));

            var consumer = (IBossBoardStateConsumer)runtime.ActionProvider;
            var source = new FakeBoardStateSource(1);
            consumer.BindBoardStateSource(source);

            Assert.That(
                runtime.IntentSource.GetIntentForecast(1)[0].SkillId,
                Is.EqualTo(OrverRules.AncientTimberStrikeSkillId));
            consumer.BindBoardStateSource(source);
            Assert.Throws<InvalidOperationException>(() =>
                consumer.BindBoardStateSource(
                    new FakeBoardStateSource(2)));
        }

        [Test]
        public void SeparateFactoryCreatesDoNotShareBoardStateSource()
        {
            OrverCombatConfig config = OrverTestConfig.Create();
            BossDefinition definition = CreateDefinition(config);
            var factory = new OrverBossCombatRuntimeFactory();
            BossCombatRuntime first = factory.Create(
                definition,
                new BossBattleState(
                    OrverRules.BossId,
                    ElementType.Grass,
                    100000,
                    500d),
                new CombatActionIdSequence());
            BossCombatRuntime second = factory.Create(
                definition,
                new BossBattleState(
                    OrverRules.BossId,
                    ElementType.Grass,
                    100000,
                    500d),
                new CombatActionIdSequence());

            ((IBossBoardStateConsumer)first.ActionProvider)
                .BindBoardStateSource(new FakeBoardStateSource(0));

            Assert.That(first.IntentSource.GetIntentForecast(1),
                Has.Count.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() =>
                second.IntentSource.GetIntentForecast(1));
        }

        [Test]
        public void ValidationRejectsWrongConfigAndInvalidConfig()
        {
            var factory = new OrverBossCombatRuntimeFactory();
            BossDefinition missingConfig = CreateDefinition(null);
            BossDefinition invalidConfig = CreateDefinition(
                OrverTestConfig.Create(maximumRootCount: 3));

            Assert.That(factory.TryValidate(
                missingConfig,
                out string missingError), Is.False);
            Assert.That(missingError, Does.Contain("OrverCombatConfig"));
            Assert.That(factory.TryValidate(
                invalidConfig,
                out string invalidError), Is.False);
            Assert.That(invalidError, Does.Contain("MaximumRootCount"));

            BossDefinition wrongElement = CreateDefinition(
                OrverTestConfig.Create(),
                ElementType.Fire);
            Assert.That(factory.TryValidate(
                wrongElement,
                out string elementError), Is.False);
            Assert.That(elementError, Does.Contain("Grass"));
        }

        private static BossDefinition CreateDefinition(
            OrverCombatConfig config,
            ElementType element = ElementType.Grass)
        {
            var definition = ScriptableObject.CreateInstance<
                BossDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = OrverRules.BossId;
            serialized.FindProperty("element").enumValueIndex =
                (int)element;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private sealed class FakeBoardStateSource : IBattleBoardStateSource
        {
            private readonly int rootCount;

            public FakeBoardStateSource(int rootCount)
            {
                this.rootCount = rootCount;
            }

            public int GetRootCount()
            {
                return rootCount;
            }
        }
    }
}

using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Kragmor
{
    public sealed class KragmorBossCombatRuntimeFactoryTests
    {
        [Test]
        public void DefaultCatalogRegistersKragmorAndRejectsDuplicate()
        {
            BossCombatRuntimeFactoryCatalog catalog =
                BossCombatRuntimeFactoryCatalog.CreateDefault();

            Assert.That(catalog.Count, Is.EqualTo(3));
            Assert.That(catalog.TryGetFactory(
                KragmorRules.BossId,
                out IBossCombatRuntimeFactory factory), Is.True);
            Assert.That(factory,
                Is.TypeOf<KragmorBossCombatRuntimeFactory>());
            Assert.That(catalog.TryGetFactory(
                "missing_boss",
                out IBossCombatRuntimeFactory missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(catalog.TryGetFactory(
                string.Empty,
                out _), Is.False);
            Assert.Throws<ArgumentException>(() =>
                catalog.Register(new KragmorBossCombatRuntimeFactory()));
            Assert.Throws<ArgumentNullException>(() =>
                catalog.Register(null));
            Assert.Throws<ArgumentException>(() =>
                catalog.Register(new InvalidIdFactory()));
        }

        [Test]
        public void BossCombatRuntimeRequiresActionProvider()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BossCombatRuntime(actionProvider: null));
        }

        [Test]
        public void ValidationRejectsInvalidKragmorConfig()
        {
            var factory = new KragmorBossCombatRuntimeFactory();
            BossDefinition definition = CreateDefinition(
                KragmorTestConfig.Create(
                    earthCollapseCoefficient: double.NaN));

            Assert.That(factory.TryValidate(
                definition,
                out string error), Is.False);
            Assert.That(error, Does.Contain("EarthCollapseCoefficient"));
        }

        [Test]
        public void CreateBuildsConsistentRuntimeAndInitialDefenseEffect()
        {
            KragmorCombatConfig config = KragmorTestConfig.Create();
            BossDefinition definition = CreateDefinition(config);
            var boss = new BossBattleState(
                KragmorRules.BossId,
                ElementType.Fire,
                maxHp: 100000,
                attack: 850d);
            var actionIds = new CombatActionIdSequence();
            var factory = new KragmorBossCombatRuntimeFactory();

            BossCombatRuntime runtime = factory.Create(
                definition,
                boss,
                actionIds);

            Assert.That(runtime.ActionProvider,
                Is.TypeOf<KragmorBossCombatActionProvider>());
            Assert.That(runtime.IntentSource,
                Is.SameAs(runtime.ActionProvider));
            var provider =
                (KragmorBossCombatActionProvider)runtime.ActionProvider;
            Assert.That(runtime.VisualStateSource,
                Is.SameAs(provider.RuntimeState));
            Assert.That(provider.RuntimeState.Config, Is.SameAs(config));
            Assert.That(boss.Effects.Count, Is.EqualTo(1));
            Assert.That(
                boss.Effects.GetActiveEffects()[0].EffectId,
                Is.EqualTo(KragmorRules.VolcanicCarapaceEffectId));
            Assert.That(provider.RuntimeState.PatternIndex, Is.Zero);
            Assert.That(
                runtime.IntentSource.GetIntentForecast(1)[0].SkillId,
                Is.EqualTo(KragmorRules.ColossusIronFistSkillId));
        }

        [Test]
        public void CreateRejectsSecondInitializationOnSameBoss()
        {
            BossDefinition definition = CreateDefinition(
                KragmorTestConfig.Create());
            var boss = new BossBattleState(
                KragmorRules.BossId,
                ElementType.Fire,
                maxHp: 100000,
                attack: 850d);
            var actionIds = new CombatActionIdSequence();
            var factory = new KragmorBossCombatRuntimeFactory();

            factory.Create(definition, boss, actionIds);

            Assert.Throws<InvalidOperationException>(() =>
                factory.Create(definition, boss, actionIds));
        }

        private static BossDefinition CreateDefinition(
            KragmorCombatConfig config)
        {
            var definition = ScriptableObject.CreateInstance<
                BossDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                KragmorRules.BossId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Fire;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private sealed class InvalidIdFactory
            : IBossCombatRuntimeFactory
        {
            public string BossId => string.Empty;

            public bool TryValidate(
                BossDefinition definition,
                out string errorMessage)
            {
                errorMessage = "Invalid test Factory.";
                return false;
            }

            public BossCombatRuntime Create(
                BossDefinition definition,
                BossBattleState boss,
                CombatActionIdSequence actionIds)
            {
                throw new InvalidOperationException(
                    "Invalid test Factory cannot create a runtime.");
            }
        }
    }
}

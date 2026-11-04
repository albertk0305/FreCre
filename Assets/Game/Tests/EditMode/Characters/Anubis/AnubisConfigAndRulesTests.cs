using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Anubis;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Anubis
{
    public sealed class AnubisConfigAndRulesTests
    {
        private AnubisCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = AnubisTestContent.CreateConfig();
            definition = AnubisTestContent.CreateDefinition(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void DefaultConfigIsValid()
        {
            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.RadianceDurationTurns, Is.EqualTo(1));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
        }

        [TestCase(0, 0.40d, 0.70d, 1.10d, 0.04d, 0.08d, 0.12d, 0.08d, 4)]
        [TestCase(3, 0.45d, 0.80d, 1.25d, 0.04d, 0.08d, 0.12d, 0.08d, 4)]
        [TestCase(4, 0.45d, 0.80d, 1.25d, 0.05d, 0.10d, 0.15d, 0.08d, 4)]
        [TestCase(5, 0.45d, 0.80d, 1.25d, 0.05d, 0.10d, 0.15d, 0.10d, 4)]
        [TestCase(6, 0.45d, 0.80d, 1.25d, 0.05d, 0.10d, 0.15d, 0.10d, 5)]
        public void ResolverAppliesAwakeningBoundaries(
            int awakening,
            double match3,
            double match4,
            double match5,
            double radiance3,
            double radiance4,
            double radiance5,
            double passive,
            int conversionCount)
        {
            AnubisResolvedCombatRules rules =
                AnubisCombatRuleResolver.Resolve(config, awakening);

            Assert.That(rules.Match3DamageCoefficient, Is.EqualTo(match3));
            Assert.That(rules.Match4DamageCoefficient, Is.EqualTo(match4));
            Assert.That(rules.Match5DamageCoefficient, Is.EqualTo(match5));
            Assert.That(rules.Match3RadianceRate, Is.EqualTo(radiance3));
            Assert.That(rules.Match4RadianceRate, Is.EqualTo(radiance4));
            Assert.That(rules.Match5RadianceRate, Is.EqualTo(radiance5));
            Assert.That(rules.PassiveLightDamageIncreaseRate,
                Is.EqualTo(passive));
            Assert.That(rules.ActiveBlockConversionCount,
                Is.EqualTo(conversionCount));
        }

        [Test]
        public void BuildModifierProducesCanonicalLevelEndpoints()
        {
            var resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[] { new AnubisBuildModifier() });

            ResolvedCharacterBuild level1 = resolver.Resolve(
                definition,
                level: 1,
                awakening: 0);
            ResolvedCharacterBuild level100 = resolver.Resolve(
                definition,
                level: 100,
                awakening: 6);

            Assert.That(level1.MaxHp, Is.EqualTo(1100));
            Assert.That(level1.Attack, Is.EqualTo(120));
            Assert.That(level100.MaxHp, Is.EqualTo(4515));
            Assert.That(level100.Attack, Is.EqualTo(683));
        }
    }

    internal static class AnubisTestContent
    {
        public static AnubisCombatConfig CreateConfig(
            int cooldown = 8)
        {
            var config = ScriptableObject.CreateInstance<AnubisCombatConfig>();
            var serialized = new SerializedObject(config);
            Set(serialized, "match3DamageCoefficient", 0.40d);
            Set(serialized, "match4DamageCoefficient", 0.70d);
            Set(serialized, "match5DamageCoefficient", 1.10d);
            Set(serialized, "match3RadianceRate", 0.04d);
            Set(serialized, "match4RadianceRate", 0.08d);
            Set(serialized, "match5RadianceRate", 0.12d);
            Set(serialized, "radianceDurationTurns", 1);
            Set(serialized, "passiveLightDamageIncreaseRate", 0.08d);
            Set(serialized, "awakening3Match3DamageCoefficient", 0.45d);
            Set(serialized, "awakening3Match4DamageCoefficient", 0.80d);
            Set(serialized, "awakening3Match5DamageCoefficient", 1.25d);
            Set(serialized, "awakening4Match3RadianceRate", 0.05d);
            Set(serialized, "awakening4Match4RadianceRate", 0.10d);
            Set(serialized, "awakening4Match5RadianceRate", 0.15d);
            Set(serialized, "awakening5PassiveLightDamageIncreaseRate", 0.10d);
            Set(serialized, "activeCooldownTurns", cooldown);
            Set(serialized, "activeBlockConversionCount", 4);
            Set(serialized, "awakening6ActiveBlockConversionCount", 5);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static CharacterDefinition CreateDefinition(
            AnubisCombatConfig config)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = AnubisRules.CharacterId;
            serialized.FindProperty("displayNameKey").stringValue = "anubis";
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Light;
            serialized.FindProperty("level1Hp").intValue = 1100;
            serialized.FindProperty("level1Attack").intValue = 120;
            serialized.FindProperty("level100Hp").intValue = 4300;
            serialized.FindProperty("level100Attack").intValue = 650;
            serialized.FindProperty("combatConfig").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void Set(
            SerializedObject serialized,
            string property,
            double value)
        {
            serialized.FindProperty(property).doubleValue = value;
        }

        private static void Set(
            SerializedObject serialized,
            string property,
            int value)
        {
            serialized.FindProperty(property).intValue = value;
        }
    }
}

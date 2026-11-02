using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Data.Definitions;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielBuildModifierTests
    {
        private LucielCombatConfig config;
        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            config = LucielTestConfig.Create();
            definition = LucielTestConfig.Definition(config);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new LucielBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(config);
        }

        [TestCase(1, 1450L, 80L)]
        [TestCase(50, 3702L, 226L)]
        [TestCase(100, 6000L, 375L)]
        public void AwakeningZeroUsesCommonLinearGrowth(
            int level,
            long expectedMaxHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level,
                awakening: 0);

            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier, Is.EqualTo(1d));
            Assert.That(result.AttackMultiplier, Is.EqualTo(1d));
        }

        [TestCase(0, 6000L, 375L, 1d)]
        [TestCase(1, 6300L, 375L, 1.05d)]
        [TestCase(2, 6300L, 375L, 1.05d)]
        [TestCase(3, 6300L, 375L, 1.05d)]
        [TestCase(4, 6300L, 375L, 1.05d)]
        [TestCase(5, 6300L, 375L, 1.05d)]
        [TestCase(6, 6300L, 375L, 1.05d)]
        public void Level100OnlyAwakeningOneChangesPermanentStats(
            int awakening,
            long expectedMaxHp,
            long expectedAttack,
            double expectedMaxHpMultiplier)
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level: 100,
                awakening: awakening);

            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(expectedMaxHpMultiplier));
            Assert.That(result.AttackMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void AwakeningOneUsesCommonFinalStatRoundingAtLevelOne()
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level: 1,
                awakening: 1);

            Assert.That(result.MaxHp, Is.EqualTo(1523L));
            Assert.That(result.Attack, Is.EqualTo(80L));
        }

        [Test]
        public void ModifierUsesCanonicalCharacterId()
        {
            var modifier = new LucielBuildModifier();

            Assert.That(modifier.CharacterId,
                Is.EqualTo("character_luciel"));
            Assert.That(modifier.CharacterId,
                Is.EqualTo(LucielRules.CharacterId));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            var modifier = new LucielBuildModifier();

            Assert.That(
                () => modifier.GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }
    }
}

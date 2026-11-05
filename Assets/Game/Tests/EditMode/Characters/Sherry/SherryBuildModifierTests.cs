using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryBuildModifierTests
    {
        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            SetField(
                definition,
                typeof(GameDefinition),
                "id",
                SherryRules.CharacterId);
            SetField(definition, "level1Hp", 1000);
            SetField(definition, "level1Attack", 150);
            SetField(definition, "level100Hp", 3900);
            SetField(definition, "level100Attack", 820);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new SherryBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 1000L, 150L)]
        [TestCase(100, 0, 3900L, 820L)]
        [TestCase(1, 1, 1000L, 158L)]
        [TestCase(100, 1, 3900L, 861L)]
        [TestCase(1, 2, 1050L, 158L)]
        [TestCase(100, 2, 4095L, 861L)]
        [TestCase(100, 6, 4095L, 861L)]
        public void ResolveReturnsExpectedStats(
            int level,
            int awakening,
            long expectedMaxHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level,
                awakening);

            Assert.That(result.CharacterId,
                Is.EqualTo(SherryRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(awakening >= 2 ? 1.05d : 1d));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(awakening >= 1 ? 1.05d : 1d));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            var modifier = new SherryBuildModifier();

            Assert.That(
                () => modifier.GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        private static void SetField(
            CharacterDefinition target,
            string fieldName,
            object value)
        {
            SetField(
                target,
                typeof(CharacterDefinition),
                fieldName,
                value);
        }

        private static void SetField(
            CharacterDefinition target,
            Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}

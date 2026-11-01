using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneBuildModifierTests
    {
        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            definition = CreateDefinition(BeatriceBloodstoneRules.CharacterId);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new BeatriceBloodstoneBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 900L, 180L)]
        [TestCase(1, 1, 900L, 189L)]
        [TestCase(1, 2, 945L, 189L)]
        [TestCase(70, 0, 2642L, 786L)]
        [TestCase(70, 1, 2642L, 825L)]
        [TestCase(70, 2, 2774L, 825L)]
        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 3, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
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

            Assert.That(result.CharacterId, Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier, Is.EqualTo(awakening >= 2 ? 1.05d : 1d));
            Assert.That(result.AttackMultiplier, Is.EqualTo(awakening >= 1 ? 1.05d : 1d));
        }

        [Test]
        public void AwakeningOneUsesCommonAwayFromZeroRounding()
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                100,
                1);

            Assert.That(1050d * 1.05d, Is.EqualTo(1102.5d));
            Assert.That(result.Attack, Is.EqualTo(1103L));
        }

        [TestCase(0, 1.00d, 1.00d)]
        [TestCase(1, 1.00d, 1.05d)]
        [TestCase(2, 1.05d, 1.05d)]
        [TestCase(3, 1.05d, 1.05d)]
        [TestCase(4, 1.05d, 1.05d)]
        [TestCase(5, 1.05d, 1.05d)]
        [TestCase(6, 1.05d, 1.05d)]
        public void ModifierSeparatesAwakeningThresholds(
            int awakening,
            double expectedMaxHpMultiplier,
            double expectedAttackMultiplier)
        {
            CharacterBuildStatMultipliers result =
                new BeatriceBloodstoneBuildModifier()
                    .GetStatMultipliers(awakening);

            Assert.That(result.MaxHpMultiplier, Is.EqualTo(expectedMaxHpMultiplier));
            Assert.That(result.AttackMultiplier, Is.EqualTo(expectedAttackMultiplier));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(int awakening)
        {
            Assert.That(
                () => new BeatriceBloodstoneBuildModifier()
                    .GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        [Test]
        public void OtherCharacterRemainsUnmodified()
        {
            CharacterDefinition other = CreateDefinition("character_other");
            try
            {
                ResolvedCharacterBuild result = resolver.Resolve(
                    other,
                    100,
                    6);

                Assert.That(result.MaxHp, Is.EqualTo(3400L));
                Assert.That(result.Attack, Is.EqualTo(1050L));
                Assert.That(result.MaxHpMultiplier, Is.EqualTo(1d));
                Assert.That(result.AttackMultiplier, Is.EqualTo(1d));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        private static CharacterDefinition CreateDefinition(string id)
        {
            CharacterDefinition result =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(result, typeof(GameDefinition), "id", id);
            SetField(result, typeof(CharacterDefinition), "level1Hp", 900);
            SetField(result, typeof(CharacterDefinition), "level1Attack", 180);
            SetField(result, typeof(CharacterDefinition), "level100Hp", 3400);
            SetField(result, typeof(CharacterDefinition), "level100Attack", 1050);
            return result;
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

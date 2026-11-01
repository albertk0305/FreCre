using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    internal static class LilithTestConfig
    {
        public static LilithCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<LilithCombatConfig>();
        }

        public static CharacterDefinition Definition(
            LilithCombatConfig config)
        {
            CharacterDefinition result =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(
                result,
                typeof(GameDefinition),
                "id",
                LilithRules.CharacterId);
            SetField(
                result,
                typeof(CharacterDefinition),
                "element",
                ElementType.Dark);
            SetField(result, typeof(CharacterDefinition), "level1Hp", 1000);
            SetField(result, typeof(CharacterDefinition), "level1Attack", 150);
            SetField(result, typeof(CharacterDefinition), "level100Hp", 3900);
            SetField(result, typeof(CharacterDefinition), "level100Attack", 820);
            SetField(
                result,
                typeof(CharacterDefinition),
                "combatConfig",
                config);
            SetField(
                result,
                typeof(CharacterDefinition),
                "skillIds",
                new[]
                {
                    LilithRules.Match3SkillId,
                    LilithRules.Match4SkillId,
                    LilithRules.Match5SkillId,
                    LilithRules.PassiveSkillId,
                    LilithRules.ActiveAbilityId
                });
            return result;
        }

        public static void SetField(
            LilithCombatConfig config,
            string fieldName,
            object value)
        {
            SetField(
                config,
                typeof(LilithCombatConfig),
                fieldName,
                value);
        }

        public static void SetField(
            CharacterDefinition definition,
            Type declaringType,
            string fieldName,
            object value)
        {
            SetField((object)definition, declaringType, fieldName, value);
        }

        private static void SetField(
            object target,
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

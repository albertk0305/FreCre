using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    internal static class LucielTestConfig
    {
        public static LucielCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<LucielCombatConfig>();
        }

        public static CharacterDefinition Definition(
            LucielCombatConfig config)
        {
            var definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                LucielRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Dark;
            serialized.FindProperty("level1Hp").intValue = 1450;
            serialized.FindProperty("level1Attack").intValue = 80;
            serialized.FindProperty("level100Hp").intValue = 6000;
            serialized.FindProperty("level100Attack").intValue = 375;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        public static void SetDouble(
            LucielCombatConfig config,
            string fieldName,
            double value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(fieldName).doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            LucielCombatConfig config,
            string fieldName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(
            LucielCombatConfig config,
            string fieldName,
            bool value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(fieldName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(
            LucielCombatConfig config,
            string fieldName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(fieldName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

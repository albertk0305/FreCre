using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    internal static class BeatriceBloodstoneTestConfig
    {
        public static BeatriceBloodstoneCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<
                BeatriceBloodstoneCombatConfig>();
        }

        public static CharacterDefinition Definition(
            BeatriceBloodstoneCombatConfig config)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                BeatriceBloodstoneRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Dark;
            serialized.FindProperty("level1Hp").intValue = 1000;
            serialized.FindProperty("level1Attack").intValue = 1000;
            serialized.FindProperty("level100Hp").intValue = 1000;
            serialized.FindProperty("level100Attack").intValue = 1000;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        public static void SetDouble(
            BeatriceBloodstoneCombatConfig config,
            string fieldName,
            double value)
        {
            var serialized = new SerializedObject(config);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            BeatriceBloodstoneCombatConfig config,
            string fieldName,
            int value)
        {
            var serialized = new SerializedObject(config);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

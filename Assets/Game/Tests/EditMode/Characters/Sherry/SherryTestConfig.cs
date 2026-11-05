using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Sherry;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    internal static class SherryTestConfig
    {
        public static SherryCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<SherryCombatConfig>();
        }

        public static void SetDouble(
            SherryCombatConfig config,
            string propertyName,
            double value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            SherryCombatConfig config,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

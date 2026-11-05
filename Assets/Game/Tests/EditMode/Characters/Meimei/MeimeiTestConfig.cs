using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Meimei;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    internal static class MeimeiTestConfig
    {
        public static MeimeiCombatConfig Create()
        {
            return ScriptableObject.CreateInstance<MeimeiCombatConfig>();
        }

        public static void SetDouble(
            MeimeiCombatConfig config,
            string propertyName,
            double value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).doubleValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(
            MeimeiCombatConfig config,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

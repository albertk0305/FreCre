using UnityEditor;
using UnityEngine;
using ValorChronicle.Bosses.Thalassion;

namespace ValorChronicle.Tests.EditMode.Bosses.Thalassion
{
    internal static class ThalassionTestConfig
    {
        public static ThalassionCombatConfig Create()
        {
            var config = ScriptableObject.CreateInstance<
                ThalassionCombatConfig>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("initialShieldStacks").intValue = 10;
            serialized.FindProperty("maximumShieldStacks").intValue = 10;
            SetDouble(serialized, "shieldDamageReductionRate", 0.8d);
            SetDouble(serialized, "tidalStrikeCoefficient", 0.75d);
            SetDouble(serialized, "ragingFistCoefficient", 0.95d);
            SetDouble(
                serialized,
                "pressureBurstBaseCoefficient",
                0.75d);
            SetDouble(
                serialized,
                "pressureBurstPerStackCoefficient",
                0.20d);
            SetDouble(serialized, "fadingWaveCoefficient", 0.55d);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void SetDouble(
            SerializedObject serialized,
            string propertyName,
            double value)
        {
            serialized.FindProperty(propertyName).doubleValue = value;
        }
    }
}

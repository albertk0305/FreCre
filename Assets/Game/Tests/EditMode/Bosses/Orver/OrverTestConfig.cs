using UnityEditor;
using UnityEngine;
using ValorChronicle.Bosses.Orver;

namespace ValorChronicle.Tests.EditMode.Bosses.Orver
{
    internal static class OrverTestConfig
    {
        public static OrverCombatConfig Create(
            double ancientTimberStrikeCoefficient = 0.75d,
            double ancientWrathCoefficient = 1.80d,
            double twistedRootLashCoefficient = 1.05d,
            double conditionalBaseDamageCoefficient = 0.55d,
            double damageCoefficientPerRoot = 0.90d,
            double healingCoefficientPerRoot = 0.01d,
            int maximumRootCount = 2)
        {
            var config = ScriptableObject.CreateInstance<OrverCombatConfig>();
            var serialized = new SerializedObject(config);
            SetDouble(
                serialized,
                "ancientTimberStrikeCoefficient",
                ancientTimberStrikeCoefficient);
            SetDouble(
                serialized,
                "ancientWrathCoefficient",
                ancientWrathCoefficient);
            SetDouble(
                serialized,
                "twistedRootLashCoefficient",
                twistedRootLashCoefficient);
            SetDouble(
                serialized,
                "conditionalBaseDamageCoefficient",
                conditionalBaseDamageCoefficient);
            SetDouble(
                serialized,
                "damageCoefficientPerRoot",
                damageCoefficientPerRoot);
            SetDouble(
                serialized,
                "healingCoefficientPerRoot",
                healingCoefficientPerRoot);
            serialized.FindProperty("maximumRootCount").intValue =
                maximumRootCount;
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

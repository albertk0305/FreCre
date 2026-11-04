using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Anubis
{
    [CreateAssetMenu(
        fileName = "AnubisCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Anubis")]
    public sealed class AnubisCombatConfig : CombatConfigDefinition
    {
        public const int RequiredRadianceDurationTurns = 1;
        public const int RequiredActiveCooldownTurns = 8;

        [SerializeField] private double match3DamageCoefficient = 0.40d;
        [SerializeField] private double match4DamageCoefficient = 0.70d;
        [SerializeField] private double match5DamageCoefficient = 1.10d;
        [SerializeField] private double match3RadianceRate = 0.04d;
        [SerializeField] private double match4RadianceRate = 0.08d;
        [SerializeField] private double match5RadianceRate = 0.12d;
        [SerializeField] private int radianceDurationTurns =
            RequiredRadianceDurationTurns;
        [SerializeField] private double passiveLightDamageIncreaseRate = 0.08d;
        [SerializeField] private double awakening3Match3DamageCoefficient = 0.45d;
        [SerializeField] private double awakening3Match4DamageCoefficient = 0.80d;
        [SerializeField] private double awakening3Match5DamageCoefficient = 1.25d;
        [SerializeField] private double awakening4Match3RadianceRate = 0.05d;
        [SerializeField] private double awakening4Match4RadianceRate = 0.10d;
        [SerializeField] private double awakening4Match5RadianceRate = 0.15d;
        [SerializeField] private double awakening5PassiveLightDamageIncreaseRate = 0.10d;
        [SerializeField] private int activeCooldownTurns =
            RequiredActiveCooldownTurns;
        [SerializeField] private int activeBlockConversionCount = 4;
        [SerializeField] private int awakening6ActiveBlockConversionCount = 5;

        public double Match3DamageCoefficient => match3DamageCoefficient;
        public double Match4DamageCoefficient => match4DamageCoefficient;
        public double Match5DamageCoefficient => match5DamageCoefficient;
        public double Match3RadianceRate => match3RadianceRate;
        public double Match4RadianceRate => match4RadianceRate;
        public double Match5RadianceRate => match5RadianceRate;
        public int RadianceDurationTurns => radianceDurationTurns;
        public double PassiveLightDamageIncreaseRate =>
            passiveLightDamageIncreaseRate;
        public double Awakening3Match3DamageCoefficient =>
            awakening3Match3DamageCoefficient;
        public double Awakening3Match4DamageCoefficient =>
            awakening3Match4DamageCoefficient;
        public double Awakening3Match5DamageCoefficient =>
            awakening3Match5DamageCoefficient;
        public double Awakening4Match3RadianceRate =>
            awakening4Match3RadianceRate;
        public double Awakening4Match4RadianceRate =>
            awakening4Match4RadianceRate;
        public double Awakening4Match5RadianceRate =>
            awakening4Match5RadianceRate;
        public double Awakening5PassiveLightDamageIncreaseRate =>
            awakening5PassiveLightDamageIncreaseRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveBlockConversionCount => activeBlockConversionCount;
        public int Awakening6ActiveBlockConversionCount =>
            awakening6ActiveBlockConversionCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFinitePositive(match3DamageCoefficient)
                || !IsFinitePositive(match4DamageCoefficient)
                || !IsFinitePositive(match5DamageCoefficient)
                || !IsFiniteUnitRate(match3RadianceRate)
                || !IsFiniteUnitRate(match4RadianceRate)
                || !IsFiniteUnitRate(match5RadianceRate)
                || !IsFiniteUnitRate(passiveLightDamageIncreaseRate)
                || !IsFinitePositive(awakening3Match3DamageCoefficient)
                || !IsFinitePositive(awakening3Match4DamageCoefficient)
                || !IsFinitePositive(awakening3Match5DamageCoefficient)
                || !IsFiniteUnitRate(awakening4Match3RadianceRate)
                || !IsFiniteUnitRate(awakening4Match4RadianceRate)
                || !IsFiniteUnitRate(awakening4Match5RadianceRate)
                || !IsFiniteUnitRate(
                    awakening5PassiveLightDamageIncreaseRate))
            {
                errorMessage = "Anubis rates and coefficients must be finite, positive, and valid rates.";
                return false;
            }

            if (awakening3Match3DamageCoefficient < match3DamageCoefficient
                || awakening3Match4DamageCoefficient < match4DamageCoefficient
                || awakening3Match5DamageCoefficient < match5DamageCoefficient
                || awakening4Match3RadianceRate < match3RadianceRate
                || awakening4Match4RadianceRate < match4RadianceRate
                || awakening4Match5RadianceRate < match5RadianceRate
                || awakening5PassiveLightDamageIncreaseRate
                    < passiveLightDamageIncreaseRate)
            {
                errorMessage = "Anubis awakening values cannot be lower than their base values.";
                return false;
            }

            if (radianceDurationTurns != RequiredRadianceDurationTurns)
            {
                errorMessage = $"RadianceDurationTurns must be {RequiredRadianceDurationTurns}.";
                return false;
            }

            if (activeCooldownTurns != RequiredActiveCooldownTurns)
            {
                errorMessage = $"ActiveCooldownTurns must be {RequiredActiveCooldownTurns}.";
                return false;
            }

            if (activeBlockConversionCount <= 0
                || awakening6ActiveBlockConversionCount
                    < activeBlockConversionCount)
            {
                errorMessage = "Active conversion counts must be positive, and the awakening value cannot be lower.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteUnitRate(double value)
        {
            return IsFinitePositive(value) && value <= 1d;
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 0d;
        }
    }
}

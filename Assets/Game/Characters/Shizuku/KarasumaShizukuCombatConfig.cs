using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Shizuku
{
    [CreateAssetMenu(
        fileName = "KarasumaShizukuCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Karasuma Shizuku")]
    public sealed class KarasumaShizukuCombatConfig
        : CombatConfigDefinition
    {
        public const int RequiredAttackBuffDurationTurns = 2;
        public const int RequiredActiveCooldownTurns = 8;

        [SerializeField] private double match3DamageCoefficient = 0.40d;
        [SerializeField] private double match4DamageCoefficient = 0.70d;
        [SerializeField] private double match5DamageCoefficient = 1.10d;
        [SerializeField] private double match3AttackIncreaseRate = 0.05d;
        [SerializeField] private double match4AttackIncreaseRate = 0.08d;
        [SerializeField] private double match5AttackIncreaseRate = 0.12d;
        [SerializeField] private int attackBuffDurationTurns =
            RequiredAttackBuffDurationTurns;
        [SerializeField] private double passiveDarkDamageIncreaseRate = 0.08d;
        [SerializeField] private double awakening3PassiveDarkDamageIncreaseRate = 0.10d;
        [SerializeField] private double awakening4Match3DamageCoefficient = 0.45d;
        [SerializeField] private double awakening4Match4DamageCoefficient = 0.75d;
        [SerializeField] private double awakening4Match3AttackIncreaseRate = 0.06d;
        [SerializeField] private double awakening4Match4AttackIncreaseRate = 0.09d;
        [SerializeField] private double awakening5Match5DamageCoefficient = 1.20d;
        [SerializeField] private double awakening5Match5AttackIncreaseRate = 0.13d;
        [SerializeField] private int activeCooldownTurns = RequiredActiveCooldownTurns;
        [SerializeField] private int activeBlockConversionCount = 4;
        [SerializeField] private int awakening6ActiveBlockConversionCount = 5;

        public double Match3DamageCoefficient => match3DamageCoefficient;
        public double Match4DamageCoefficient => match4DamageCoefficient;
        public double Match5DamageCoefficient => match5DamageCoefficient;
        public double Match3AttackIncreaseRate => match3AttackIncreaseRate;
        public double Match4AttackIncreaseRate => match4AttackIncreaseRate;
        public double Match5AttackIncreaseRate => match5AttackIncreaseRate;
        public int AttackBuffDurationTurns => attackBuffDurationTurns;
        public double PassiveDarkDamageIncreaseRate => passiveDarkDamageIncreaseRate;
        public double Awakening3PassiveDarkDamageIncreaseRate => awakening3PassiveDarkDamageIncreaseRate;
        public double Awakening4Match3DamageCoefficient => awakening4Match3DamageCoefficient;
        public double Awakening4Match4DamageCoefficient => awakening4Match4DamageCoefficient;
        public double Awakening4Match3AttackIncreaseRate => awakening4Match3AttackIncreaseRate;
        public double Awakening4Match4AttackIncreaseRate => awakening4Match4AttackIncreaseRate;
        public double Awakening5Match5DamageCoefficient => awakening5Match5DamageCoefficient;
        public double Awakening5Match5AttackIncreaseRate => awakening5Match5AttackIncreaseRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveBlockConversionCount => activeBlockConversionCount;
        public int Awakening6ActiveBlockConversionCount => awakening6ActiveBlockConversionCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteUnitRate(match3DamageCoefficient)
                || !IsFiniteUnitRate(match4DamageCoefficient)
                || !IsFinitePositive(match5DamageCoefficient)
                || !IsFiniteUnitRate(match3AttackIncreaseRate)
                || !IsFiniteUnitRate(match4AttackIncreaseRate)
                || !IsFiniteUnitRate(match5AttackIncreaseRate)
                || !IsFiniteUnitRate(passiveDarkDamageIncreaseRate)
                || !IsFiniteUnitRate(awakening3PassiveDarkDamageIncreaseRate)
                || !IsFiniteUnitRate(awakening4Match3DamageCoefficient)
                || !IsFiniteUnitRate(awakening4Match4DamageCoefficient)
                || !IsFiniteUnitRate(awakening4Match3AttackIncreaseRate)
                || !IsFiniteUnitRate(awakening4Match4AttackIncreaseRate)
                || !IsFinitePositive(awakening5Match5DamageCoefficient)
                || !IsFiniteUnitRate(awakening5Match5AttackIncreaseRate))
            {
                errorMessage = "Shizuku rates and coefficients must be finite, non-negative, and valid rates.";
                return false;
            }

            if (awakening3PassiveDarkDamageIncreaseRate < passiveDarkDamageIncreaseRate
                || awakening4Match3DamageCoefficient < match3DamageCoefficient
                || awakening4Match4DamageCoefficient < match4DamageCoefficient
                || awakening4Match3AttackIncreaseRate < match3AttackIncreaseRate
                || awakening4Match4AttackIncreaseRate < match4AttackIncreaseRate
                || awakening5Match5DamageCoefficient < match5DamageCoefficient
                || awakening5Match5AttackIncreaseRate < match5AttackIncreaseRate)
            {
                errorMessage = "Shizuku awakening values cannot be lower than their base values.";
                return false;
            }

            if (attackBuffDurationTurns != RequiredAttackBuffDurationTurns)
            {
                errorMessage = $"AttackBuffDurationTurns must be {RequiredAttackBuffDurationTurns}.";
                return false;
            }

            if (activeCooldownTurns != RequiredActiveCooldownTurns)
            {
                errorMessage = $"ActiveCooldownTurns must be {RequiredActiveCooldownTurns}.";
                return false;
            }

            if (activeBlockConversionCount <= 0
                || awakening6ActiveBlockConversionCount < activeBlockConversionCount)
            {
                errorMessage = "Active conversion counts must be positive, and the awakening value cannot be lower.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteUnitRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }

        private static bool IsFinitePositive(double value)
        {
            return IsFiniteNonNegative(value) && value > 0d;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        }
    }
}

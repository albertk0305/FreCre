using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    [CreateAssetMenu(
        fileName = "HazelSparksCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Hazel Sparks")]
    public sealed class HazelSparksCombatConfig : CombatConfigDefinition
    {
        public const int RequiredForgedMomentumDurationTurns = 1;
        public const int RequiredActiveCooldownTurns = 4;
        public const int RequiredActiveBlockConversionCount = 2;

        [SerializeField] private double match3DamageCoefficient = 0.40d;
        [SerializeField] private double match4DamageCoefficient = 0.70d;
        [SerializeField] private double match5DamageCoefficient = 1.10d;
        [SerializeField] private int match3SharpeningGain = 1;
        [SerializeField] private int match4SharpeningGain = 2;
        [SerializeField] private int match5SharpeningGain = 3;
        [SerializeField] private int maximumSharpeningStacks = 9;
        [SerializeField] private int initialSharpeningStacks = 1;
        [SerializeField] private double artisansTouchRatePerStack = 0.01d;
        [SerializeField] private double forgedMomentumRatePerOverflow = 0.03d;
        [SerializeField] private int forgedMomentumDurationTurns =
            RequiredForgedMomentumDurationTurns;
        [SerializeField] private double awakening3Match3DamageCoefficient =
            0.45d;
        [SerializeField] private double awakening3Match4DamageCoefficient =
            0.80d;
        [SerializeField] private double awakening3Match5DamageCoefficient =
            1.25d;
        [SerializeField] private double awakening4ArtisansTouchRatePerStack =
            0.012d;
        [SerializeField] private double awakening5ForgedMomentumRatePerOverflow =
            0.04d;
        [SerializeField] private int awakening6InitialSharpeningStacks = 3;
        [SerializeField] private int activeCooldownTurns =
            RequiredActiveCooldownTurns;
        [SerializeField] private int activeBlockConversionCount =
            RequiredActiveBlockConversionCount;

        public double Match3DamageCoefficient => match3DamageCoefficient;
        public double Match4DamageCoefficient => match4DamageCoefficient;
        public double Match5DamageCoefficient => match5DamageCoefficient;
        public int Match3SharpeningGain => match3SharpeningGain;
        public int Match4SharpeningGain => match4SharpeningGain;
        public int Match5SharpeningGain => match5SharpeningGain;
        public int MaximumSharpeningStacks => maximumSharpeningStacks;
        public int InitialSharpeningStacks => initialSharpeningStacks;
        public double ArtisansTouchRatePerStack =>
            artisansTouchRatePerStack;
        public double ForgedMomentumRatePerOverflow =>
            forgedMomentumRatePerOverflow;
        public int ForgedMomentumDurationTurns =>
            forgedMomentumDurationTurns;
        public double Awakening3Match3DamageCoefficient =>
            awakening3Match3DamageCoefficient;
        public double Awakening3Match4DamageCoefficient =>
            awakening3Match4DamageCoefficient;
        public double Awakening3Match5DamageCoefficient =>
            awakening3Match5DamageCoefficient;
        public double Awakening4ArtisansTouchRatePerStack =>
            awakening4ArtisansTouchRatePerStack;
        public double Awakening5ForgedMomentumRatePerOverflow =>
            awakening5ForgedMomentumRatePerOverflow;
        public int Awakening6InitialSharpeningStacks =>
            awakening6InitialSharpeningStacks;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveBlockConversionCount => activeBlockConversionCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFinitePositive(match3DamageCoefficient)
                || !IsFinitePositive(match4DamageCoefficient)
                || !IsFinitePositive(match5DamageCoefficient)
                || !IsFinitePositive(awakening3Match3DamageCoefficient)
                || !IsFinitePositive(awakening3Match4DamageCoefficient)
                || !IsFinitePositive(awakening3Match5DamageCoefficient))
            {
                errorMessage = "Hazel's damage coefficients must be finite "
                    + "and positive.";
                return false;
            }

            if (match3DamageCoefficient > match4DamageCoefficient
                || match4DamageCoefficient > match5DamageCoefficient
                || awakening3Match3DamageCoefficient
                    > awakening3Match4DamageCoefficient
                || awakening3Match4DamageCoefficient
                    > awakening3Match5DamageCoefficient
                || awakening3Match3DamageCoefficient
                    < match3DamageCoefficient
                || awakening3Match4DamageCoefficient
                    < match4DamageCoefficient
                || awakening3Match5DamageCoefficient
                    < match5DamageCoefficient)
            {
                errorMessage = "Hazel's Match coefficients must increase by "
                    + "tier, and awakening values cannot be lower.";
                return false;
            }

            if (match3SharpeningGain <= 0
                || match4SharpeningGain < match3SharpeningGain
                || match5SharpeningGain < match4SharpeningGain)
            {
                errorMessage = "Hazel's Sharpening gains must be positive "
                    + "and cannot decrease by Match tier.";
                return false;
            }

            if (maximumSharpeningStacks <= 0
                || initialSharpeningStacks <= 0
                || initialSharpeningStacks > maximumSharpeningStacks
                || awakening6InitialSharpeningStacks
                    < initialSharpeningStacks
                || awakening6InitialSharpeningStacks
                    > maximumSharpeningStacks)
            {
                errorMessage = "Hazel's initial Sharpening must be within "
                    + "the configured maximum, including Awakening 6.";
                return false;
            }

            if (!IsFiniteUnitRate(artisansTouchRatePerStack)
                || !IsFiniteUnitRate(
                    awakening4ArtisansTouchRatePerStack)
                || awakening4ArtisansTouchRatePerStack
                    < artisansTouchRatePerStack
                || !IsFiniteUnitRate(forgedMomentumRatePerOverflow)
                || !IsFiniteUnitRate(
                    awakening5ForgedMomentumRatePerOverflow)
                || awakening5ForgedMomentumRatePerOverflow
                    < forgedMomentumRatePerOverflow)
            {
                errorMessage = "Hazel's Effect rates must be finite positive "
                    + "rates, and awakening values cannot be lower.";
                return false;
            }

            if (forgedMomentumDurationTurns
                != RequiredForgedMomentumDurationTurns)
            {
                errorMessage = "ForgedMomentumDurationTurns must be "
                    + $"{RequiredForgedMomentumDurationTurns}.";
                return false;
            }

            if (activeCooldownTurns != RequiredActiveCooldownTurns
                || activeBlockConversionCount
                    != RequiredActiveBlockConversionCount)
            {
                errorMessage = "Hazel's Active must use the required "
                    + $"{RequiredActiveCooldownTurns}-turn cooldown and "
                    + $"{RequiredActiveBlockConversionCount}-block "
                    + "conversion count.";
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

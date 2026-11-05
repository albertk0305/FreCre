using System;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Sherry
{
    [CreateAssetMenu(
        fileName = "SherryCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Sherry")]
    public sealed class SherryCombatConfig : CombatConfigDefinition
    {
        public const int RequiredStylishMaxRank = 7;

        [SerializeField]
        private int stylishMaxRank = RequiredStylishMaxRank;

        [SerializeField]
        private double stylishDamageExponentBase = 1.6d;

        [SerializeField]
        private double baseStylishMaxDamageIncreaseRate = 0.20d;

        [SerializeField]
        private double awakening5StylishMaxDamageIncreaseRate = 0.25d;

        [SerializeField]
        private double jackpotExponentBase = 1.65d;

        [SerializeField]
        private double baseJackpotMaxCoefficient = 18.00d;

        [SerializeField]
        private double awakening6JackpotMaxCoefficient = 23.00d;

        [SerializeField]
        private int activeCooldownTurns = 1;

        [SerializeField]
        private double match3Coefficient = 0.75d;

        [SerializeField]
        private double awakening3Match3Coefficient = 0.80d;

        [SerializeField]
        private double match4Coefficient = 1.30d;

        [SerializeField]
        private double awakening3Match4Coefficient = 1.40d;

        [SerializeField]
        private double match5Coefficient = 2.10d;

        [SerializeField]
        private double awakening3Match5Coefficient = 2.25d;

        [SerializeField]
        private double match3OniIntimidationRate = 0.03d;

        [SerializeField]
        private double match4OniIntimidationRate = 0.06d;

        [SerializeField]
        private double match5OniIntimidationRate = 0.10d;

        [SerializeField]
        private double awakening4Match3OniIntimidationRate = 0.04d;

        [SerializeField]
        private double awakening4Match4OniIntimidationRate = 0.08d;

        [SerializeField]
        private double awakening4Match5OniIntimidationRate = 0.13d;

        [SerializeField]
        private int oniIntimidationDurationTurns = 2;

        [SerializeField]
        private int oniIntimidationMinimumRank = 5;

        public int StylishMaxRank => stylishMaxRank;
        public double StylishDamageExponentBase =>
            stylishDamageExponentBase;
        public double BaseStylishMaxDamageIncreaseRate =>
            baseStylishMaxDamageIncreaseRate;
        public double Awakening5StylishMaxDamageIncreaseRate =>
            awakening5StylishMaxDamageIncreaseRate;
        public double JackpotExponentBase => jackpotExponentBase;
        public double BaseJackpotMaxCoefficient =>
            baseJackpotMaxCoefficient;
        public double Awakening6JackpotMaxCoefficient =>
            awakening6JackpotMaxCoefficient;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Match3Coefficient => match3Coefficient;
        public double Awakening3Match3Coefficient =>
            awakening3Match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Awakening3Match4Coefficient =>
            awakening3Match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public double Awakening3Match5Coefficient =>
            awakening3Match5Coefficient;
        public double Match3OniIntimidationRate =>
            match3OniIntimidationRate;
        public double Match4OniIntimidationRate =>
            match4OniIntimidationRate;
        public double Match5OniIntimidationRate =>
            match5OniIntimidationRate;
        public double Awakening4Match3OniIntimidationRate =>
            awakening4Match3OniIntimidationRate;
        public double Awakening4Match4OniIntimidationRate =>
            awakening4Match4OniIntimidationRate;
        public double Awakening4Match5OniIntimidationRate =>
            awakening4Match5OniIntimidationRate;
        public int OniIntimidationDurationTurns =>
            oniIntimidationDurationTurns;
        public int OniIntimidationMinimumRank =>
            oniIntimidationMinimumRank;

        public override bool TryValidate(out string errorMessage)
        {
            if (stylishMaxRank != RequiredStylishMaxRank)
            {
                errorMessage = $"StylishMaxRank must be "
                    + $"{RequiredStylishMaxRank}.";
                return false;
            }

            if (!IsFiniteGreaterThanOne(stylishDamageExponentBase))
            {
                errorMessage = "StylishDamageExponentBase must be finite "
                    + "and greater than one.";
                return false;
            }

            if (!IsRate(baseStylishMaxDamageIncreaseRate))
            {
                errorMessage = "BaseStylishMaxDamageIncreaseRate must be "
                    + "between zero and one.";
                return false;
            }

            if (!IsRate(awakening5StylishMaxDamageIncreaseRate)
                || awakening5StylishMaxDamageIncreaseRate
                    < baseStylishMaxDamageIncreaseRate)
            {
                errorMessage = "Awakening5StylishMaxDamageIncreaseRate must "
                    + "be between zero and one and not be lower than the "
                    + "base rate.";
                return false;
            }

            if (!IsFiniteGreaterThanOne(jackpotExponentBase))
            {
                errorMessage = "JackpotExponentBase must be finite and "
                    + "greater than one.";
                return false;
            }

            if (!IsFinitePositive(baseJackpotMaxCoefficient))
            {
                errorMessage = "BaseJackpotMaxCoefficient must be finite "
                    + "and greater than zero.";
                return false;
            }

            if (!IsFinitePositive(awakening6JackpotMaxCoefficient)
                || awakening6JackpotMaxCoefficient
                    < baseJackpotMaxCoefficient)
            {
                errorMessage = "Awakening6JackpotMaxCoefficient must be "
                    + "finite, positive, and not lower than the base "
                    + "coefficient.";
                return false;
            }

            if (activeCooldownTurns <= 0)
            {
                errorMessage =
                    "ActiveCooldownTurns must be greater than zero.";
                return false;
            }

            if (!ValidateEnhancedCoefficient(
                match3Coefficient,
                awakening3Match3Coefficient,
                "Match3Coefficient",
                "Awakening3Match3Coefficient",
                out errorMessage)
                || !ValidateEnhancedCoefficient(
                    match4Coefficient,
                    awakening3Match4Coefficient,
                    "Match4Coefficient",
                    "Awakening3Match4Coefficient",
                    out errorMessage)
                || !ValidateEnhancedCoefficient(
                    match5Coefficient,
                    awakening3Match5Coefficient,
                    "Match5Coefficient",
                    "Awakening3Match5Coefficient",
                    out errorMessage))
            {
                return false;
            }

            if (!ValidateEnhancedRate(
                match3OniIntimidationRate,
                awakening4Match3OniIntimidationRate,
                "Match3OniIntimidationRate",
                "Awakening4Match3OniIntimidationRate",
                out errorMessage)
                || !ValidateEnhancedRate(
                    match4OniIntimidationRate,
                    awakening4Match4OniIntimidationRate,
                    "Match4OniIntimidationRate",
                    "Awakening4Match4OniIntimidationRate",
                    out errorMessage)
                || !ValidateEnhancedRate(
                    match5OniIntimidationRate,
                    awakening4Match5OniIntimidationRate,
                    "Match5OniIntimidationRate",
                    "Awakening4Match5OniIntimidationRate",
                    out errorMessage))
            {
                return false;
            }

            if (oniIntimidationDurationTurns <= 0)
            {
                errorMessage = "OniIntimidationDurationTurns must be "
                    + "greater than zero.";
                return false;
            }

            if (oniIntimidationMinimumRank <= 0
                || oniIntimidationMinimumRank > stylishMaxRank)
            {
                errorMessage = "OniIntimidationMinimumRank must be between "
                    + "one and StylishMaxRank.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateEnhancedCoefficient(
            double baseValue,
            double enhancedValue,
            string baseName,
            string enhancedName,
            out string errorMessage)
        {
            if (!IsFinitePositive(baseValue))
            {
                errorMessage = $"{baseName} must be finite and greater "
                    + "than zero.";
                return false;
            }

            if (!IsFinitePositive(enhancedValue)
                || enhancedValue < baseValue)
            {
                errorMessage = $"{enhancedName} must be finite, positive, "
                    + $"and not lower than {baseName}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateEnhancedRate(
            double baseValue,
            double enhancedValue,
            string baseName,
            string enhancedName,
            out string errorMessage)
        {
            if (!IsRate(baseValue))
            {
                errorMessage = $"{baseName} must be between zero and one.";
                return false;
            }

            if (!IsRate(enhancedValue) || enhancedValue < baseValue)
            {
                errorMessage = $"{enhancedName} must be between zero and "
                    + $"one and not lower than {baseName}.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteGreaterThanOne(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 1d;
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 0d;
        }

        private static bool IsRate(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d
                && value <= 1d;
        }
    }
}

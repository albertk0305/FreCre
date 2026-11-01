using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lilith
{
    [CreateAssetMenu(
        fileName = "LilithCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Lilith")]
    public sealed class LilithCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private int initialContractStackCount;
        [SerializeField] private int contractMaxStackCount = 12;
        [SerializeField] private int match3ContractGainAmount = 1;
        [SerializeField] private int match4ContractGainAmount = 2;
        [SerializeField] private int match5ContractGainAmount = 3;
        [SerializeField] private int activeContractGainAmount = 2;
        [SerializeField] private int contractExplosionMinimumStackCount = 4;
        [SerializeField] private int contractConsumptionUnit = 4;
        [SerializeField]
        private double contractDamageIncreaseRatePerStack = 0.015d;
        [SerializeField]
        private double awakening6ContractDamageIncreaseRatePerStack = 0.018d;
        [SerializeField]
        private double contractExplosionCoefficientPerUnit = 3.00d;
        [SerializeField]
        private double awakening5ContractExplosionCoefficientPerUnit = 3.30d;

        [SerializeField] private double match3Coefficient = 0.70d;
        [SerializeField] private double match4Coefficient = 1.20d;
        [SerializeField] private double match5Coefficient = 1.90d;
        [SerializeField]
        private double awakening3Match3Coefficient = 0.75d;
        [SerializeField]
        private double awakening3Match4Coefficient = 1.30d;
        [SerializeField]
        private double awakening4Match5Coefficient = 2.10d;

        [SerializeField]
        private double corruptedBlessingDamageReductionRate = 0.20d;
        [SerializeField]
        private double awakening4CorruptedBlessingDamageReductionRate = 0.25d;
        [SerializeField]
        private int corruptedBlessingDurationTurns = 2;
        [SerializeField]
        private int corruptedBlessingMaxStackCount = 1;
        [SerializeField]
        private EffectStackPolicy corruptedBlessingStackPolicy =
            EffectStackPolicy.RefreshDuration;

        [SerializeField] private int activeCooldownTurns = 8;
        [SerializeField]
        private double activeCurrentPartyHpCostRate = 0.15d;

        public int InitialContractStackCount => initialContractStackCount;
        public int ContractMaxStackCount => contractMaxStackCount;
        public int? ContractRemainingTurns => null;
        public EffectStackPolicy ContractStackPolicy =>
            EffectStackPolicy.StackCount;
        public int Match3ContractGainAmount => match3ContractGainAmount;
        public int Match4ContractGainAmount => match4ContractGainAmount;
        public int Match5ContractGainAmount => match5ContractGainAmount;
        public int ActiveContractGainAmount => activeContractGainAmount;
        public int ContractExplosionMinimumStackCount =>
            contractExplosionMinimumStackCount;
        public int ContractConsumptionUnit => contractConsumptionUnit;
        public double ContractDamageIncreaseRatePerStack =>
            contractDamageIncreaseRatePerStack;
        public double Awakening6ContractDamageIncreaseRatePerStack =>
            awakening6ContractDamageIncreaseRatePerStack;
        public double ContractExplosionCoefficientPerUnit =>
            contractExplosionCoefficientPerUnit;
        public double Awakening5ContractExplosionCoefficientPerUnit =>
            awakening5ContractExplosionCoefficientPerUnit;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public double Awakening3Match3Coefficient =>
            awakening3Match3Coefficient;
        public double Awakening3Match4Coefficient =>
            awakening3Match4Coefficient;
        public double Awakening4Match5Coefficient =>
            awakening4Match5Coefficient;
        public AttackType MatchAttackType => AttackType.Match;
        public bool MatchUsesCombo => true;
        public bool ApplyContractAfterMatchDamage => true;

        public double CorruptedBlessingDamageReductionRate =>
            corruptedBlessingDamageReductionRate;
        public double Awakening4CorruptedBlessingDamageReductionRate =>
            awakening4CorruptedBlessingDamageReductionRate;
        public int CorruptedBlessingDurationTurns =>
            corruptedBlessingDurationTurns;
        public int CorruptedBlessingMaxStackCount =>
            corruptedBlessingMaxStackCount;
        public EffectStackPolicy CorruptedBlessingStackPolicy =>
            corruptedBlessingStackPolicy;
        public int CorruptedBlessingMinimumMatchSize => 5;
        public bool CorruptedBlessingAffectsDirectHpCost => false;

        public int ActiveCooldownTurns => activeCooldownTurns;
        public double ActiveCurrentPartyHpCostRate =>
            activeCurrentPartyHpCostRate;
        public bool ActiveAvailableAtBattleStart => true;
        public bool ActiveHpCostUsesCurrentPartyHp => true;
        public long ActiveMinimumPartyHpAfterCost => 1L;
        public bool ActiveHpCostRoundsDown => true;
        public bool ActiveUsableWhenActualHpCostIsZero => true;
        public bool ActiveHpCostIgnoresShields => true;
        public bool ActiveHpCostIgnoresDamageReduction => true;
        public bool ApplyContractAfterActiveHpCost => true;

        public AttackType ContractExplosionAttackType =>
            AttackType.Passive;
        public bool ContractExplosionUsesCombo => false;
        public bool ContractExplosionRunsAtMostOncePerBossAction => true;

        public override bool TryValidate(out string errorMessage)
        {
            if (initialContractStackCount != 0)
            {
                errorMessage = "InitialContractStackCount must be zero.";
                return false;
            }

            if (contractMaxStackCount <= 0)
            {
                errorMessage = "ContractMaxStackCount must be greater than zero.";
                return false;
            }

            if (contractConsumptionUnit <= 0
                || contractConsumptionUnit > contractMaxStackCount)
            {
                errorMessage = "ContractConsumptionUnit must be between one "
                    + "and ContractMaxStackCount.";
                return false;
            }

            if (contractMaxStackCount % contractConsumptionUnit != 0)
            {
                errorMessage = "ContractMaxStackCount must be divisible by "
                    + "ContractConsumptionUnit.";
                return false;
            }

            if (contractExplosionMinimumStackCount
                != contractConsumptionUnit)
            {
                errorMessage = "ContractExplosionMinimumStackCount must match "
                    + "ContractConsumptionUnit.";
                return false;
            }

            if (!IsValidContractGain(match3ContractGainAmount)
                || !IsValidContractGain(match4ContractGainAmount)
                || !IsValidContractGain(match5ContractGainAmount)
                || !IsValidContractGain(activeContractGainAmount))
            {
                errorMessage = "Contract gain amounts must be between one and "
                    + "ContractMaxStackCount.";
                return false;
            }

            if (!IsFiniteUnitRate(contractDamageIncreaseRatePerStack)
                || !IsFiniteUnitRate(
                    awakening6ContractDamageIncreaseRatePerStack))
            {
                errorMessage = "Contract damage increase rates must be finite "
                    + "and between zero and one.";
                return false;
            }

            if (awakening6ContractDamageIncreaseRatePerStack
                < contractDamageIncreaseRatePerStack)
            {
                errorMessage = "Awakening6ContractDamageIncreaseRatePerStack "
                    + "cannot be less than its base value.";
                return false;
            }

            if (!ValidateCoefficients(out errorMessage))
            {
                return false;
            }

            if (!IsFiniteUnitRate(corruptedBlessingDamageReductionRate)
                || !IsFiniteUnitRate(
                    awakening4CorruptedBlessingDamageReductionRate))
            {
                errorMessage = "Corrupted Blessing damage reduction rates must "
                    + "be finite and between zero and one.";
                return false;
            }

            if (awakening4CorruptedBlessingDamageReductionRate
                < corruptedBlessingDamageReductionRate)
            {
                errorMessage = "Awakening4CorruptedBlessingDamageReductionRate "
                    + "cannot be less than its base value.";
                return false;
            }

            if (corruptedBlessingDurationTurns <= 0)
            {
                errorMessage = "CorruptedBlessingDurationTurns must be greater "
                    + "than zero.";
                return false;
            }

            if (corruptedBlessingMaxStackCount != 1)
            {
                errorMessage = "CorruptedBlessingMaxStackCount must be one.";
                return false;
            }

            if (corruptedBlessingStackPolicy
                != EffectStackPolicy.RefreshDuration)
            {
                errorMessage = "CorruptedBlessingStackPolicy must refresh "
                    + "duration.";
                return false;
            }

            if (activeCooldownTurns <= 0)
            {
                errorMessage = "ActiveCooldownTurns must be greater than zero.";
                return false;
            }

            if (!IsFiniteUnitRate(activeCurrentPartyHpCostRate))
            {
                errorMessage = "ActiveCurrentPartyHpCostRate must be finite "
                    + "and between zero and one.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool IsValidContractGain(int amount)
        {
            return amount > 0 && amount <= contractMaxStackCount;
        }

        private bool ValidateCoefficients(out string errorMessage)
        {
            double[] coefficients =
            {
                contractExplosionCoefficientPerUnit,
                awakening5ContractExplosionCoefficientPerUnit,
                match3Coefficient,
                match4Coefficient,
                match5Coefficient,
                awakening3Match3Coefficient,
                awakening3Match4Coefficient,
                awakening4Match5Coefficient
            };
            for (int index = 0; index < coefficients.Length; index++)
            {
                if (!IsFiniteNonNegative(coefficients[index]))
                {
                    errorMessage = "All damage coefficients must be finite and "
                        + "non-negative.";
                    return false;
                }
            }

            if (awakening3Match3Coefficient < match3Coefficient
                || awakening3Match4Coefficient < match4Coefficient
                || awakening4Match5Coefficient < match5Coefficient
                || awakening5ContractExplosionCoefficientPerUnit
                    < contractExplosionCoefficientPerUnit)
            {
                errorMessage = "Awakening damage coefficients cannot be less "
                    + "than their base values.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteUnitRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}

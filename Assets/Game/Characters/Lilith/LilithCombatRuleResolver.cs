using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Lilith
{
    public readonly struct LilithContractExplosionResult
    {
        internal LilithContractExplosionResult(
            int consumedStackCount,
            int remainingStackCount,
            int explosionUnitCount,
            double damageCoefficient)
        {
            ConsumedStackCount = consumedStackCount;
            RemainingStackCount = remainingStackCount;
            ExplosionUnitCount = explosionUnitCount;
            DamageCoefficient = damageCoefficient;
        }

        public int ConsumedStackCount { get; }
        public int RemainingStackCount { get; }
        public int ExplosionUnitCount { get; }
        public double DamageCoefficient { get; }
        public bool ShouldTrigger => ConsumedStackCount > 0;
    }

    public sealed class LilithResolvedCombatRules
    {
        internal LilithResolvedCombatRules(
            LilithCombatConfig config,
            int awakening)
        {
            InitialContractStackCount = config.InitialContractStackCount;
            ContractMaxStackCount = config.ContractMaxStackCount;
            ContractRemainingTurns = config.ContractRemainingTurns;
            ContractStackPolicy = config.ContractStackPolicy;
            Match3ContractGainAmount = config.Match3ContractGainAmount;
            Match4ContractGainAmount = config.Match4ContractGainAmount;
            Match5ContractGainAmount = config.Match5ContractGainAmount;
            ActiveContractGainAmount = config.ActiveContractGainAmount;
            ContractExplosionMinimumStackCount =
                config.ContractExplosionMinimumStackCount;
            ContractConsumptionUnit = config.ContractConsumptionUnit;
            ContractDamageIncreaseRatePerStack = awakening >= 6
                ? config.Awakening6ContractDamageIncreaseRatePerStack
                : config.ContractDamageIncreaseRatePerStack;
            ContractExplosionCoefficientPerUnit = awakening >= 5
                ? config.Awakening5ContractExplosionCoefficientPerUnit
                : config.ContractExplosionCoefficientPerUnit;

            Match3Coefficient = awakening >= 3
                ? config.Awakening3Match3Coefficient
                : config.Match3Coefficient;
            Match4Coefficient = awakening >= 3
                ? config.Awakening3Match4Coefficient
                : config.Match4Coefficient;
            Match5Coefficient = awakening >= 4
                ? config.Awakening4Match5Coefficient
                : config.Match5Coefficient;
            MatchAttackType = config.MatchAttackType;
            MatchUsesCombo = config.MatchUsesCombo;
            ApplyContractAfterMatchDamage =
                config.ApplyContractAfterMatchDamage;

            CorruptedBlessingDamageReductionRate = awakening >= 4
                ? config.Awakening4CorruptedBlessingDamageReductionRate
                : config.CorruptedBlessingDamageReductionRate;
            CorruptedBlessingDurationTurns =
                config.CorruptedBlessingDurationTurns;
            CorruptedBlessingMaxStackCount =
                config.CorruptedBlessingMaxStackCount;
            CorruptedBlessingStackPolicy =
                config.CorruptedBlessingStackPolicy;
            CorruptedBlessingMinimumMatchSize =
                config.CorruptedBlessingMinimumMatchSize;
            CorruptedBlessingAffectsDirectHpCost =
                config.CorruptedBlessingAffectsDirectHpCost;

            ActiveCooldownTurns = config.ActiveCooldownTurns;
            ActiveCurrentPartyHpCostRate =
                config.ActiveCurrentPartyHpCostRate;
            ActiveAvailableAtBattleStart =
                config.ActiveAvailableAtBattleStart;
            ActiveHpCostUsesCurrentPartyHp =
                config.ActiveHpCostUsesCurrentPartyHp;
            ActiveMinimumPartyHpAfterCost =
                config.ActiveMinimumPartyHpAfterCost;
            ActiveHpCostRoundsDown = config.ActiveHpCostRoundsDown;
            ActiveUsableWhenActualHpCostIsZero =
                config.ActiveUsableWhenActualHpCostIsZero;
            ActiveHpCostIgnoresShields =
                config.ActiveHpCostIgnoresShields;
            ActiveHpCostIgnoresDamageReduction =
                config.ActiveHpCostIgnoresDamageReduction;
            ApplyContractAfterActiveHpCost =
                config.ApplyContractAfterActiveHpCost;

            ContractExplosionAttackType =
                config.ContractExplosionAttackType;
            ContractExplosionUsesCombo =
                config.ContractExplosionUsesCombo;
            ContractExplosionRunsAtMostOncePerBossAction =
                config.ContractExplosionRunsAtMostOncePerBossAction;
        }

        public int InitialContractStackCount { get; }
        public int ContractMaxStackCount { get; }
        public int? ContractRemainingTurns { get; }
        public EffectStackPolicy ContractStackPolicy { get; }
        public int Match3ContractGainAmount { get; }
        public int Match4ContractGainAmount { get; }
        public int Match5ContractGainAmount { get; }
        public int ActiveContractGainAmount { get; }
        public int ContractExplosionMinimumStackCount { get; }
        public int ContractConsumptionUnit { get; }
        public double ContractDamageIncreaseRatePerStack { get; }
        public double ContractExplosionCoefficientPerUnit { get; }
        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public AttackType MatchAttackType { get; }
        public bool MatchUsesCombo { get; }
        public bool ApplyContractAfterMatchDamage { get; }
        public double CorruptedBlessingDamageReductionRate { get; }
        public int CorruptedBlessingDurationTurns { get; }
        public int CorruptedBlessingMaxStackCount { get; }
        public EffectStackPolicy CorruptedBlessingStackPolicy { get; }
        public int CorruptedBlessingMinimumMatchSize { get; }
        public bool CorruptedBlessingAffectsDirectHpCost { get; }
        public int ActiveCooldownTurns { get; }
        public double ActiveCurrentPartyHpCostRate { get; }
        public bool ActiveAvailableAtBattleStart { get; }
        public bool ActiveHpCostUsesCurrentPartyHp { get; }
        public long ActiveMinimumPartyHpAfterCost { get; }
        public bool ActiveHpCostRoundsDown { get; }
        public bool ActiveUsableWhenActualHpCostIsZero { get; }
        public bool ActiveHpCostIgnoresShields { get; }
        public bool ActiveHpCostIgnoresDamageReduction { get; }
        public bool ApplyContractAfterActiveHpCost { get; }
        public AttackType ContractExplosionAttackType { get; }
        public bool ContractExplosionUsesCombo { get; }
        public bool ContractExplosionRunsAtMostOncePerBossAction { get; }

        public double GetMatchCoefficient(int matchSize)
        {
            ValidateMatchSize(matchSize);
            if (matchSize == 3)
            {
                return Match3Coefficient;
            }

            return matchSize == 4
                ? Match4Coefficient
                : Match5Coefficient;
        }

        public int GetMatchContractGainAmount(int matchSize)
        {
            ValidateMatchSize(matchSize);
            if (matchSize == 3)
            {
                return Match3ContractGainAmount;
            }

            return matchSize == 4
                ? Match4ContractGainAmount
                : Match5ContractGainAmount;
        }

        public bool CanApplyCorruptedBlessing(int matchSize)
        {
            ValidateMatchSize(matchSize);
            return matchSize >= CorruptedBlessingMinimumMatchSize;
        }

        public double CalculateContractDamageIncreaseRate(
            int contractStackCount)
        {
            ValidateContractStackCount(contractStackCount);
            return ContractDamageIncreaseRatePerStack
                * contractStackCount;
        }

        public LilithContractExplosionResult ResolveContractExplosion(
            int contractStackCount)
        {
            ValidateContractStackCount(contractStackCount);
            int consumedStackCount = contractStackCount
                / ContractConsumptionUnit
                * ContractConsumptionUnit;
            int explosionUnitCount = consumedStackCount
                / ContractConsumptionUnit;
            return new LilithContractExplosionResult(
                consumedStackCount,
                contractStackCount - consumedStackCount,
                explosionUnitCount,
                explosionUnitCount
                    * ContractExplosionCoefficientPerUnit);
        }

        private void ValidateContractStackCount(int contractStackCount)
        {
            if (contractStackCount < InitialContractStackCount
                || contractStackCount > ContractMaxStackCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contractStackCount),
                    contractStackCount,
                    $"Contract stack count must be between "
                        + $"{InitialContractStackCount} and "
                        + $"{ContractMaxStackCount}.");
            }
        }

        private static void ValidateMatchSize(int matchSize)
        {
            if (matchSize < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchSize),
                    matchSize,
                    "Match size must be at least three.");
            }
        }
    }

    public static class LilithCombatRuleResolver
    {
        public static LilithResolvedCombatRules Resolve(
            LilithCombatConfig config,
            int awakening)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new LilithResolvedCombatRules(config, awakening);
        }
    }
}

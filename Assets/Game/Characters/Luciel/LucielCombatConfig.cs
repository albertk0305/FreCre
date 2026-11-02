using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Luciel
{
    [CreateAssetMenu(
        fileName = "LucielCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Luciel")]
    public sealed class LucielCombatConfig : CombatConfigDefinition
    {
        public const int RequiredMatch5BuffDurationTurns = 3;
        public const int RequiredActiveCooldownTurns = 8;

        [SerializeField]
        private double passiveDamageReductionRate = 0.05d;

        [SerializeField]
        private double awakening5PassiveDamageReductionRate = 0.06d;

        [SerializeField] private double match3ShieldMaxHpRate = 0.06d;
        [SerializeField]
        private double awakening3Match3ShieldMaxHpRate = 0.07d;
        [SerializeField] private double match4ShieldMaxHpRate = 0.11d;
        [SerializeField]
        private double awakening3Match4ShieldMaxHpRate = 0.12d;
        [SerializeField] private double match5ShieldMaxHpRate = 0.18d;
        [SerializeField]
        private double awakening4Match5ShieldMaxHpRate = 0.20d;

        [SerializeField]
        private double match5PartyDealtDamageIncreaseRate = 0.12d;

        [SerializeField]
        private double awakening4Match5PartyDealtDamageIncreaseRate = 0.14d;

        [SerializeField]
        private int match5BuffDurationTurns =
            RequiredMatch5BuffDurationTurns;

        [SerializeField]
        private EffectStackPolicy match5BuffStackPolicy =
            EffectStackPolicy.RefreshDuration;

        [SerializeField]
        private double activeCurrentPartyHpCostRate = 0.05d;

        [SerializeField]
        private PartyHpCostRoundingMode activeHpCostRoundingMode =
            PartyHpCostRoundingMode.Ceiling;

        [SerializeField] private double activeShieldMaxHpRate = 0.45d;
        [SerializeField]
        private double awakening6ActiveShieldMaxHpRate = 0.52d;
        [SerializeField]
        private int activeCooldownTurns = RequiredActiveCooldownTurns;

        [SerializeField]
        private double awakening2ShieldAmountIncreaseRate = 0.05d;

        [SerializeField]
        private bool matchShieldUsesComboMultiplier = true;

        [SerializeField]
        private bool activeShieldUsesComboMultiplier;

        public double PassiveDamageReductionRate =>
            passiveDamageReductionRate;
        public double Awakening5PassiveDamageReductionRate =>
            awakening5PassiveDamageReductionRate;
        public double Match3ShieldMaxHpRate => match3ShieldMaxHpRate;
        public double Awakening3Match3ShieldMaxHpRate =>
            awakening3Match3ShieldMaxHpRate;
        public double Match4ShieldMaxHpRate => match4ShieldMaxHpRate;
        public double Awakening3Match4ShieldMaxHpRate =>
            awakening3Match4ShieldMaxHpRate;
        public double Match5ShieldMaxHpRate => match5ShieldMaxHpRate;
        public double Awakening4Match5ShieldMaxHpRate =>
            awakening4Match5ShieldMaxHpRate;
        public double Match5PartyDealtDamageIncreaseRate =>
            match5PartyDealtDamageIncreaseRate;
        public double Awakening4Match5PartyDealtDamageIncreaseRate =>
            awakening4Match5PartyDealtDamageIncreaseRate;
        public int Match5BuffDurationTurns => match5BuffDurationTurns;
        public EffectStackPolicy Match5BuffStackPolicy =>
            match5BuffStackPolicy;
        public double ActiveCurrentPartyHpCostRate =>
            activeCurrentPartyHpCostRate;
        public PartyHpCostRoundingMode ActiveHpCostRoundingMode =>
            activeHpCostRoundingMode;
        public double ActiveShieldMaxHpRate => activeShieldMaxHpRate;
        public double Awakening6ActiveShieldMaxHpRate =>
            awakening6ActiveShieldMaxHpRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double Awakening2ShieldAmountIncreaseRate =>
            awakening2ShieldAmountIncreaseRate;
        public bool MatchShieldUsesComboMultiplier =>
            matchShieldUsesComboMultiplier;
        public bool ActiveShieldUsesComboMultiplier =>
            activeShieldUsesComboMultiplier;
        public int? ShieldDurationTurns => null;
        public bool ActiveAvailableAtBattleStart => true;
        public bool ActiveHpCostUsesCurrentPartyHp => true;
        public long ActiveMinimumPartyHpAfterCost => 1L;
        public bool ActiveUsableWhenActualHpCostIsZero => true;
        public bool ActiveHpCostIgnoresShields => true;
        public bool ActiveHpCostIgnoresDamageReduction => true;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteUnitRate(passiveDamageReductionRate))
            {
                return InvalidRate(
                    nameof(PassiveDamageReductionRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(
                    awakening5PassiveDamageReductionRate))
            {
                return InvalidRate(
                    nameof(Awakening5PassiveDamageReductionRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(match3ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Match3ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(awakening3Match3ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Awakening3Match3ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(match4ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Match4ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(awakening3Match4ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Awakening3Match4ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(match5ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Match5ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(awakening4Match5ShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Awakening4Match5ShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(match5PartyDealtDamageIncreaseRate))
            {
                return InvalidRate(
                    nameof(Match5PartyDealtDamageIncreaseRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(
                    awakening4Match5PartyDealtDamageIncreaseRate))
            {
                return InvalidRate(
                    nameof(
                        Awakening4Match5PartyDealtDamageIncreaseRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(activeCurrentPartyHpCostRate))
            {
                return InvalidRate(
                    nameof(ActiveCurrentPartyHpCostRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(activeShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(ActiveShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(awakening6ActiveShieldMaxHpRate))
            {
                return InvalidRate(
                    nameof(Awakening6ActiveShieldMaxHpRate),
                    out errorMessage);
            }

            if (!IsFiniteUnitRate(awakening2ShieldAmountIncreaseRate))
            {
                return InvalidRate(
                    nameof(Awakening2ShieldAmountIncreaseRate),
                    out errorMessage);
            }

            if (awakening5PassiveDamageReductionRate
                    < passiveDamageReductionRate
                || awakening3Match3ShieldMaxHpRate
                    < match3ShieldMaxHpRate
                || awakening3Match4ShieldMaxHpRate
                    < match4ShieldMaxHpRate
                || awakening4Match5ShieldMaxHpRate
                    < match5ShieldMaxHpRate
                || awakening4Match5PartyDealtDamageIncreaseRate
                    < match5PartyDealtDamageIncreaseRate
                || awakening6ActiveShieldMaxHpRate
                    < activeShieldMaxHpRate)
            {
                errorMessage = "Luciel awakening values cannot be lower "
                    + "than their base values.";
                return false;
            }

            if (match5BuffDurationTurns
                != RequiredMatch5BuffDurationTurns)
            {
                errorMessage = $"Match5BuffDurationTurns must be "
                    + $"{RequiredMatch5BuffDurationTurns}.";
                return false;
            }

            if (match5BuffStackPolicy
                != EffectStackPolicy.RefreshDuration)
            {
                errorMessage = "Match5BuffStackPolicy must refresh duration.";
                return false;
            }

            if (activeHpCostRoundingMode
                != PartyHpCostRoundingMode.Ceiling)
            {
                errorMessage = "ActiveHpCostRoundingMode must be Ceiling.";
                return false;
            }

            if (activeCooldownTurns != RequiredActiveCooldownTurns)
            {
                errorMessage = $"ActiveCooldownTurns must be "
                    + $"{RequiredActiveCooldownTurns}.";
                return false;
            }

            if (!matchShieldUsesComboMultiplier)
            {
                errorMessage = "MatchShieldUsesComboMultiplier must be true.";
                return false;
            }

            if (activeShieldUsesComboMultiplier)
            {
                errorMessage = "ActiveShieldUsesComboMultiplier must be "
                    + "false.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool InvalidRate(
            string propertyName,
            out string errorMessage)
        {
            errorMessage = $"{propertyName} must be finite and between "
                + "zero and one.";
            return false;
        }

        private static bool IsFiniteUnitRate(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d
                && value <= 1d;
        }
    }
}

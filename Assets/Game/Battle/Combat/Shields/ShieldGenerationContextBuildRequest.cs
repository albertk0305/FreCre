using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Shields
{
    public sealed class ShieldGenerationContextBuildRequest
    {
        public ShieldGenerationContextBuildRequest(
            CharacterBattleState source,
            PartyBattleState party,
            double shieldCoefficient,
            bool appliesCombo,
            int finalComboCount,
            double actionLocalShieldAmountIncreaseRate = 0d)
        {
            Source = source
                ?? throw new ArgumentNullException(nameof(source));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            if (double.IsNaN(shieldCoefficient)
                || double.IsInfinity(shieldCoefficient)
                || shieldCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shieldCoefficient));
            }

            if (appliesCombo && finalComboCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            if (double.IsNaN(actionLocalShieldAmountIncreaseRate)
                || double.IsInfinity(actionLocalShieldAmountIncreaseRate)
                || actionLocalShieldAmountIncreaseRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionLocalShieldAmountIncreaseRate),
                    actionLocalShieldAmountIncreaseRate,
                    "Action-local shield amount increase must be finite "
                        + "and non-negative.");
            }

            ShieldCoefficient = shieldCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            ActionLocalShieldAmountIncreaseRate =
                actionLocalShieldAmountIncreaseRate;
        }

        public CharacterBattleState Source { get; }
        public PartyBattleState Party { get; }
        public double ShieldCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public double ActionLocalShieldAmountIncreaseRate { get; }
    }
}

using System;
using ValorChronicle.Battle.Combat.Modifiers;

namespace ValorChronicle.Battle.Combat.Shields
{
    public static class ShieldGenerationContextFactory
    {
        public static ShieldGenerationContext Build(
            ShieldGenerationContextBuildRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            SupportModifierSnapshot modifiers =
                SupportModifierCollector.Collect(
                    request.Source,
                    request.Party);
            double shieldAmountIncreaseRateSum = Add(
                modifiers.ShieldAmountIncreaseRateSum,
                request.ActionLocalShieldAmountIncreaseRate);
            return new ShieldGenerationContext(
                request.Source.MaxHp,
                request.ShieldCoefficient,
                request.AppliesCombo,
                request.FinalComboCount,
                shieldAmountIncreaseRateSum);
        }

        private static double Add(double left, double right)
        {
            double result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    "Combined shield amount increase must remain finite.");
            }

            return result;
        }
    }
}

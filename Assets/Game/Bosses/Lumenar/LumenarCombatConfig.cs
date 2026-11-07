using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Lumenar
{
    [CreateAssetMenu(fileName = "LumenarCombatConfig", menuName = "Valor Chronicle/Combat Configs/Lumenar")]
    public sealed class LumenarCombatConfig : CombatConfigDefinition
    {
        [SerializeField] private double radiantStrikeCoefficient;
        [SerializeField] private double celestialJudgmentCoefficient;
        [SerializeField] private double stoneStrikeCoefficient;
        [SerializeField] private double stonefallCurseCoefficient;
        [SerializeField] private double lightDamageIncreaseRate;
        [SerializeField] private double stoneDamageReductionRate;
        [SerializeField] private int fallingStoneCreationCount;
        [SerializeField] private int maximumFallingStoneCount;

        public double RadiantStrikeCoefficient => radiantStrikeCoefficient;
        public double CelestialJudgmentCoefficient => celestialJudgmentCoefficient;
        public double StoneStrikeCoefficient => stoneStrikeCoefficient;
        public double StonefallCurseCoefficient => stonefallCurseCoefficient;
        public double LightDamageIncreaseRate => lightDamageIncreaseRate;
        public double StoneDamageReductionRate => stoneDamageReductionRate;
        public int FallingStoneCreationCount => fallingStoneCreationCount;
        public int MaximumFallingStoneCount => maximumFallingStoneCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!Valid(radiantStrikeCoefficient) || !Valid(celestialJudgmentCoefficient)
                || !Valid(stoneStrikeCoefficient) || !Valid(stonefallCurseCoefficient)
                || !Valid(lightDamageIncreaseRate) || !Valid(stoneDamageReductionRate))
            {
                errorMessage = "All Lumenar coefficients and rates must be finite and non-negative.";
                return false;
            }

            if (fallingStoneCreationCount != 1 || maximumFallingStoneCount != 2)
            {
                errorMessage = "Lumenar requires one FallingStone per action and a maximum of two.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool Valid(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}

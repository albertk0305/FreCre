using System;

namespace ValorChronicle.Bosses.Thalassion
{
    public static class ThalassionRules
    {
        public const string BossId = "thalassion";
        public const string ElementalShieldEffectId =
            "effect_thalassion_elemental_shield";
        public const string ShieldedVisualStateId = "thalassion_shielded";
        public const int PatternCount = 4;

        public const string TidalStrikeSkillId =
            "boss_action_thalassion_tidal_strike";
        public const string AbyssalAegisSkillId =
            "boss_action_thalassion_abyssal_aegis";
        public const string RagingFistSkillId =
            "boss_action_thalassion_raging_fist";
        public const string PressureBurstSkillId =
            "boss_action_thalassion_pressure_burst";
        public const string FadingWaveSkillId =
            "boss_action_thalassion_fading_wave";

        public static string GetSkillId(ThalassionActionKind actionKind)
        {
            switch (actionKind)
            {
                case ThalassionActionKind.TidalStrike:
                    return TidalStrikeSkillId;
                case ThalassionActionKind.AbyssalAegis:
                    return AbyssalAegisSkillId;
                case ThalassionActionKind.RagingFist:
                    return RagingFistSkillId;
                case ThalassionActionKind.PressureBurst:
                    return PressureBurstSkillId;
                case ThalassionActionKind.FadingWave:
                    return FadingWaveSkillId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionKind));
            }
        }
    }
}

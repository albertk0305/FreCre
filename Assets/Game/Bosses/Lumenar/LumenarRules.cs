using System;

namespace ValorChronicle.Bosses.Lumenar
{
    public static class LumenarRules
    {
        public const string BossId = "lumenar";
        public const string RadiantStrikeSkillId = "boss_action_lumenar_radiant_strike";
        public const string CelestialJudgmentSkillId = "boss_action_lumenar_celestial_judgment";
        public const string StoneStrikeSkillId = "boss_action_lumenar_stone_strike";
        public const string StonefallCurseSkillId = "boss_action_lumenar_stonefall_curse";
        public const string LightFormEffectId = "effect_lumenar_luminous_form";
        public const string StoneFormEffectId = "effect_lumenar_stone_form";
        public const string LightVisualStateId = "lumenar_light_form";
        public const string StoneVisualStateId = "lumenar_stone_form";
        public const int PatternCountPerForm = 2;

        public static string GetSkillId(LumenarActionKind kind)
        {
            switch (kind)
            {
                case LumenarActionKind.RadiantStrike: return RadiantStrikeSkillId;
                case LumenarActionKind.CelestialJudgment: return CelestialJudgmentSkillId;
                case LumenarActionKind.StoneStrike: return StoneStrikeSkillId;
                case LumenarActionKind.StonefallCurse: return StonefallCurseSkillId;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}

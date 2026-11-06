using System;

namespace ValorChronicle.Bosses.Orver
{
    public static class OrverRules
    {
        public const string BossId = "orver";
        public const string AncientTimberStrikeSkillId =
            "boss_action_orver_ancient_strike";
        public const string RootInfestationSkillId =
            "boss_action_orver_root_infestation";
        public const string AncientWrathSkillId =
            "boss_action_orver_ancient_wrath";
        public const string TwistedRootLashSkillId =
            "boss_action_orver_root_lash";
        public const string LifeSiphonSkillId =
            "boss_action_orver_life_siphon";
        public const string WitheredStrikeSkillId =
            "boss_action_orver_withered_strike";
        public const int PatternCount = 4;
        public const int RootCreationCount = 1;

        public static string GetSkillId(OrverActionKind actionKind)
        {
            switch (actionKind)
            {
                case OrverActionKind.AncientTimberStrike:
                    return AncientTimberStrikeSkillId;
                case OrverActionKind.RootInfestation:
                    return RootInfestationSkillId;
                case OrverActionKind.AncientWrath:
                    return AncientWrathSkillId;
                case OrverActionKind.TwistedRootLash:
                    return TwistedRootLashSkillId;
                case OrverActionKind.LifeSiphon:
                    return LifeSiphonSkillId;
                case OrverActionKind.WitheredStrike:
                    return WitheredStrikeSkillId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionKind),
                        actionKind,
                        "Unsupported Orver action kind.");
            }
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Attacks;

namespace ValorChronicle.Bosses.Fenrir
{
    public static class FenrirRules
    {
        public const string BossId = "fenrir";
        public const string SavageRendSkillId =
            "boss_action_fenrir_savage_rend";
        public const string RampagingClawSkillId =
            "boss_action_fenrir_rampaging_claw";
        public const string WoundEffectId = "effect_fenrir_wound";
        public const string FrenzyEffectId = "effect_fenrir_frenzy";
        public const string FrenzyAttackEffectId =
            "effect_fenrir_frenzy_attack";
        public const string FrenziedVisualStateId = "fenrir_frenzied";

        public static bool IsFrenzyCriticalAttackType(
            AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Match:
                case AttackType.Active:
                case AttackType.Additional:
                case AttackType.Chase:
                    return true;
                case AttackType.DamageOverTime:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attackType));
            }
        }
    }
}

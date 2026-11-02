using System;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossIntentPreview
    {
        public BossIntentPreview(
            int turnsUntilAction,
            string skillId)
        {
            if (turnsUntilAction <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsUntilAction),
                    turnsUntilAction,
                    "Turns until action must be positive.");
            }

            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException(
                    "Skill ID cannot be null or whitespace.",
                    nameof(skillId));
            }

            TurnsUntilAction = turnsUntilAction;
            SkillId = skillId;
        }

        public int TurnsUntilAction { get; }
        public string SkillId { get; }
    }
}

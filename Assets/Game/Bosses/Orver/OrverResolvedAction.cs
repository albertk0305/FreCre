using System;

namespace ValorChronicle.Bosses.Orver
{
    public sealed class OrverResolvedAction
    {
        public OrverResolvedAction(
            OrverActionKind actionKind,
            string skillId,
            int rootCount,
            double damageCoefficient,
            double healingCoefficient,
            bool createsRoot)
        {
            if (!Enum.IsDefined(typeof(OrverActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }

            if (rootCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rootCount));
            }

            ValidateCoefficient(
                damageCoefficient,
                nameof(damageCoefficient));
            ValidateCoefficient(
                healingCoefficient,
                nameof(healingCoefficient));
            ActionKind = actionKind;
            SkillId = string.IsNullOrWhiteSpace(skillId)
                ? throw new ArgumentException(
                    "Skill ID cannot be null or whitespace.",
                    nameof(skillId))
                : skillId;
            RootCount = rootCount;
            DamageCoefficient = damageCoefficient;
            HealingCoefficient = healingCoefficient;
            CreatesRoot = createsRoot;
        }

        public OrverActionKind ActionKind { get; }
        public string SkillId { get; }
        public int RootCount { get; }
        public double DamageCoefficient { get; }
        public double HealingCoefficient { get; }
        public bool CreatesRoot { get; }
        public bool DealsDamage => DamageCoefficient > 0d;
        public bool HealsBoss => HealingCoefficient > 0d;

        private static void ValidateCoefficient(
            double value,
            string parameterName)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}

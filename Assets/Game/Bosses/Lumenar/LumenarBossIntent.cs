using System;

namespace ValorChronicle.Bosses.Lumenar
{
    public sealed class LumenarBossIntent
    {
        internal LumenarBossIntent(LumenarActionKind actionKind, double damageCoefficient, bool createsFallingStone)
        {
            if (!Enum.IsDefined(typeof(LumenarActionKind), actionKind)) throw new ArgumentOutOfRangeException(nameof(actionKind));
            if (double.IsNaN(damageCoefficient) || double.IsInfinity(damageCoefficient) || damageCoefficient < 0d)
                throw new ArgumentOutOfRangeException(nameof(damageCoefficient));
            ActionKind = actionKind;
            SkillId = LumenarRules.GetSkillId(actionKind);
            DamageCoefficient = damageCoefficient;
            CreatesFallingStone = createsFallingStone;
        }

        public LumenarActionKind ActionKind { get; }
        public string SkillId { get; }
        public double DamageCoefficient { get; }
        public bool CreatesFallingStone { get; }
    }
}

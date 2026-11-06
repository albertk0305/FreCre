using System;

namespace ValorChronicle.Bosses.Orver
{
    public static class OrverActionResolver
    {
        public static bool IsConditionalPattern(int patternIndex)
        {
            ValidatePatternIndex(patternIndex);
            return patternIndex == 1 || patternIndex == 3;
        }

        public static OrverResolvedAction Resolve(
            int patternIndex,
            int rootCount,
            OrverCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            ValidatePatternIndex(patternIndex);
            if (rootCount < 0 || rootCount > config.MaximumRootCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rootCount),
                    rootCount,
                    $"Root count must be between 0 and "
                        + $"{config.MaximumRootCount}.");
            }

            switch (patternIndex)
            {
                case 0:
                    return Build(
                        OrverActionKind.AncientTimberStrike,
                        rootCount,
                        config.AncientTimberStrikeCoefficient);
                case 1:
                    return rootCount < config.MaximumRootCount
                        ? Build(
                            OrverActionKind.RootInfestation,
                            rootCount,
                            damageCoefficient: 0d,
                            createsRoot: true)
                        : Build(
                            OrverActionKind.AncientWrath,
                            rootCount,
                            config.AncientWrathCoefficient);
                case 2:
                    return Build(
                        OrverActionKind.TwistedRootLash,
                        rootCount,
                        config.TwistedRootLashCoefficient);
                case 3:
                    double damageCoefficient =
                        config.ConditionalBaseDamageCoefficient
                        + (rootCount * config.DamageCoefficientPerRoot);
                    double healingCoefficient =
                        rootCount * config.HealingCoefficientPerRoot;
                    return Build(
                        rootCount == 0
                            ? OrverActionKind.WitheredStrike
                            : OrverActionKind.LifeSiphon,
                        rootCount,
                        damageCoefficient,
                        healingCoefficient);
                default:
                    throw new InvalidOperationException(
                        "Unsupported Orver pattern index.");
            }
        }

        private static OrverResolvedAction Build(
            OrverActionKind actionKind,
            int rootCount,
            double damageCoefficient,
            double healingCoefficient = 0d,
            bool createsRoot = false)
        {
            return new OrverResolvedAction(
                actionKind,
                OrverRules.GetSkillId(actionKind),
                rootCount,
                damageCoefficient,
                healingCoefficient,
                createsRoot);
        }

        private static void ValidatePatternIndex(int patternIndex)
        {
            if (patternIndex < 0
                || patternIndex >= OrverRules.PatternCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(patternIndex),
                    patternIndex,
                    $"Pattern index must be between 0 and "
                        + $"{OrverRules.PatternCount - 1}.");
            }
        }
    }
}

using System;
using NUnit.Framework;
using ValorChronicle.Bosses.Orver;

namespace ValorChronicle.Tests.EditMode.Bosses.Orver
{
    public sealed class OrverActionResolverTests
    {
        [TestCase(0, 0, OrverActionKind.AncientTimberStrike,
            OrverRules.AncientTimberStrikeSkillId, 0.75d, 0d, false)]
        [TestCase(1, 0, OrverActionKind.RootInfestation,
            OrverRules.RootInfestationSkillId, 0d, 0d, true)]
        [TestCase(1, 1, OrverActionKind.RootInfestation,
            OrverRules.RootInfestationSkillId, 0d, 0d, true)]
        [TestCase(1, 2, OrverActionKind.AncientWrath,
            OrverRules.AncientWrathSkillId, 1.80d, 0d, false)]
        [TestCase(2, 2, OrverActionKind.TwistedRootLash,
            OrverRules.TwistedRootLashSkillId, 1.05d, 0d, false)]
        [TestCase(3, 0, OrverActionKind.WitheredStrike,
            OrverRules.WitheredStrikeSkillId, 0.55d, 0d, false)]
        [TestCase(3, 1, OrverActionKind.LifeSiphon,
            OrverRules.LifeSiphonSkillId, 1.45d, 0.01d, false)]
        [TestCase(3, 2, OrverActionKind.LifeSiphon,
            OrverRules.LifeSiphonSkillId, 2.35d, 0.02d, false)]
        public void ResolveReturnsExpectedBranch(
            int patternIndex,
            int rootCount,
            OrverActionKind expectedKind,
            string expectedSkillId,
            double expectedDamage,
            double expectedHealing,
            bool expectedCreatesRoot)
        {
            OrverResolvedAction action = OrverActionResolver.Resolve(
                patternIndex,
                rootCount,
                OrverTestConfig.Create());

            Assert.That(action.ActionKind, Is.EqualTo(expectedKind));
            Assert.That(action.SkillId, Is.EqualTo(expectedSkillId));
            Assert.That(action.RootCount, Is.EqualTo(rootCount));
            Assert.That(action.DamageCoefficient,
                Is.EqualTo(expectedDamage).Within(0.000000001d));
            Assert.That(action.HealingCoefficient,
                Is.EqualTo(expectedHealing).Within(0.000000001d));
            Assert.That(action.CreatesRoot,
                Is.EqualTo(expectedCreatesRoot));
        }

        [Test]
        public void ResolveRejectsInvalidPatternAndRootCount()
        {
            OrverCombatConfig config = OrverTestConfig.Create();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OrverActionResolver.Resolve(-1, 0, config));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OrverActionResolver.Resolve(4, 0, config));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OrverActionResolver.Resolve(0, -1, config));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                OrverActionResolver.Resolve(0, 3, config));
        }

        [Test]
        public void ConfigDefaultsValidateAndInvalidValuesAreRejected()
        {
            var defaults = UnityEngine.ScriptableObject.CreateInstance<
                OrverCombatConfig>();

            Assert.That(defaults.TryValidate(out string defaultError),
                Is.True, defaultError);
            Assert.That(defaults.MaximumRootCount, Is.EqualTo(2));
            Assert.That(defaults.PatternCount, Is.EqualTo(4));
            Assert.That(defaults.AncientTimberStrikeCoefficient,
                Is.EqualTo(0.75d));
            Assert.That(OrverTestConfig.Create(
                ancientWrathCoefficient: double.NaN).TryValidate(out _),
                Is.False);
            Assert.That(OrverTestConfig.Create(
                maximumRootCount: 3).TryValidate(out _),
                Is.False);
        }

        [Test]
        public void SkillIdsAreUnique()
        {
            string[] ids =
            {
                OrverRules.AncientTimberStrikeSkillId,
                OrverRules.RootInfestationSkillId,
                OrverRules.AncientWrathSkillId,
                OrverRules.TwistedRootLashSkillId,
                OrverRules.LifeSiphonSkillId,
                OrverRules.WitheredStrikeSkillId
            };

            Assert.That(ids, Is.Unique);
        }
    }
}

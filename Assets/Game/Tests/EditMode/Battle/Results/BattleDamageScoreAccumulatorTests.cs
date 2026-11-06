using System;
using NUnit.Framework;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleDamageScoreAccumulatorTests
    {
        [Test]
        public void Add_AccumulatesAppliedDamage()
        {
            var accumulator = new BattleDamageScoreAccumulator();

            accumulator.Add(120L);
            accumulator.Add(0L);
            accumulator.Add(380L);

            Assert.That(accumulator.DamageScore, Is.EqualTo(500L));
        }

        [Test]
        public void Add_RejectsNegativeAppliedDamage()
        {
            var accumulator = new BattleDamageScoreAccumulator();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => accumulator.Add(-1L));
            Assert.That(accumulator.DamageScore, Is.Zero);
        }

        [Test]
        public void Add_ThrowsWhenDamageScoreOverflows()
        {
            var accumulator = new BattleDamageScoreAccumulator();
            accumulator.Add(long.MaxValue);

            Assert.Throws<OverflowException>(() => accumulator.Add(1L));
            Assert.That(accumulator.DamageScore, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void BossHealingReducesScoreWithZeroLowerBound()
        {
            var accumulator = new BattleDamageScoreAccumulator();

            accumulator.Add(10000L);
            accumulator.AddBossHealing(1320L);
            Assert.That(accumulator.DamageScore, Is.EqualTo(8680L));

            accumulator.Add(5000L);
            Assert.That(accumulator.DamageScore, Is.EqualTo(13680L));

            accumulator.AddBossHealing(20000L);
            Assert.That(accumulator.DamageScore, Is.Zero);
            Assert.That(accumulator.CumulativeAppliedDamage,
                Is.EqualTo(15000L));
            Assert.That(accumulator.CumulativeAppliedBossHealing,
                Is.EqualTo(21320L));
        }

        [Test]
        public void Apply_RejectsNegativeHealingWithoutPartialMutation()
        {
            var accumulator = new BattleDamageScoreAccumulator();
            accumulator.Add(100L);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                accumulator.Apply(50L, -1L));
            Assert.That(accumulator.CumulativeAppliedDamage,
                Is.EqualTo(100L));
            Assert.That(accumulator.CumulativeAppliedBossHealing, Is.Zero);
            Assert.That(accumulator.DamageScore, Is.EqualTo(100L));
        }
    }
}

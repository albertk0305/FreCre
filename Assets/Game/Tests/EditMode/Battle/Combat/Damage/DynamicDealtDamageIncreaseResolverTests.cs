using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class DynamicDealtDamageIncreaseResolverTests
    {
        [Test]
        public void Build_WithoutResolverPreservesExistingDamage()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageResult result = Calculate(Request(
                attacker,
                party,
                boss));

            Assert.That(result.FinalDamage, Is.EqualTo(1000L));
        }

        [Test]
        public void Build_ZeroResolverPreservesExistingDamage()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: request => 0d));

            Assert.That(context.DealtDamageIncreaseRateSum, Is.Zero);
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1000L));
        }

        [Test]
        public void Build_TenPercentResolverUsesExistingDamageCategory()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: request => 0.10d));

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1100L));
        }

        [Test]
        public void Build_AddsActionLocalAndDynamicRates()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                actionLocalRate: 0.15d,
                dealtDamageResolver: request => 0.10d));

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.25d).Within(0.000000001d));
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1250L));
        }

        [Test]
        public void Build_AddsDynamicRateWithExistingEffect()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(new EffectInstance(
                1,
                "existing_dealt_damage",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.DealtDamageIncrease,
                0.20d,
                2,
                1));

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: request => 0.10d));

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1300L));
        }

        [Test]
        public void Build_ResolverReadsHpAtExecutionTime()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            DamageContextBuildRequest request = Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: ResolveLostHpRateAtTwentyPercent);

            PartyDamageApplier.Apply(party, BossDamage(50));
            DamageContext context = Build(request);

            Assert.That(party.CurrentHp, Is.EqualTo(50L));
            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1100L));
        }

        [Test]
        public void Execute_HpCostBeforeDamageUsesLatestHp()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            var damage = new DamageAction(
                2,
                ActionOrigin.System,
                Request(
                    attacker,
                    party,
                    boss,
                    dealtDamageResolver:
                        ResolveLostHpRateAtTwentyPercent));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new PartyHpCostAction(
                    1,
                    ActionOrigin.System,
                    party,
                    referenceMaxHp: 100,
                    costRate: 0.50d),
                damage
            });
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));

            CombatActionExecutionResult execution = executor.Execute(queue);

            Assert.That(execution.CompletedActionCount, Is.EqualTo(2));
            var result = (DamageActionResult)execution.ActionResults[1];
            Assert.That(party.CurrentHp, Is.EqualTo(50L));
            Assert.That(result.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(result.AppliedDamage, Is.EqualTo(1100L));
        }

        [Test]
        public void Build_SkillAndDealtDamageResolversAreIndependent()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                skillResolver: request => 2d,
                dealtDamageResolver: request => 0.10d));

            Assert.That(context.SkillCoefficient, Is.EqualTo(2d));
            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(2200L));
        }

        [TestCase(AttackType.Match)]
        [TestCase(AttackType.Active)]
        [TestCase(AttackType.Additional)]
        [TestCase(AttackType.Chase)]
        public void Build_PreservesAttackTypeAndComboProcessing(
            AttackType attackType)
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            DamageContext baseline = Build(Request(
                attacker,
                party,
                boss,
                attackType,
                appliesCombo: true,
                finalComboCount: 3));
            DamageContext dynamic = Build(Request(
                attacker,
                party,
                boss,
                attackType,
                appliesCombo: true,
                finalComboCount: 3,
                dealtDamageResolver: request => 0.10d));

            DamageResult baselineResult =
                DamageCalculator.Calculate(baseline);
            DamageResult dynamicResult =
                DamageCalculator.Calculate(dynamic);
            Assert.That(dynamic.AppliesCombo, Is.True);
            Assert.That(dynamic.FinalComboCount, Is.EqualTo(3));
            Assert.That(dynamicResult.ComboMultiplier,
                Is.EqualTo(baselineResult.ComboMultiplier));
            Assert.That(dynamicResult.RawDamage,
                Is.EqualTo(baselineResult.RawDamage * 1.10d)
                    .Within(0.000000001d));
        }

        [Test]
        public void Build_InvokesDynamicResolverExactlyOnce()
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            int callCount = 0;

            DamageContext context = Build(Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: request =>
                {
                    callCount++;
                    return 0.10d;
                }));

            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
        }

        [TestCase(-0.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Build_RejectsInvalidResolvedRate(double resolvedRate)
        {
            CreateBattle(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            DamageContextBuildRequest request = Request(
                attacker,
                party,
                boss,
                dealtDamageResolver: ignored => resolvedRate);

            Assert.Throws<ArgumentOutOfRangeException>(() => Build(request));
        }

        private static double ResolveLostHpRateAtTwentyPercent(
            DamageContextBuildRequest request)
        {
            double currentHpRate =
                (double)request.Party.CurrentHp / request.Party.MaxHp;
            return (1d - currentHpRate) * 0.20d;
        }

        private static DamageContextBuildRequest Request(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType = AttackType.Match,
            bool appliesCombo = false,
            int finalComboCount = 0,
            double actionLocalRate = 0d,
            DamageSkillCoefficientResolver skillResolver = null,
            DamageDealtDamageIncreaseRateResolver
                dealtDamageResolver = null)
        {
            return new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                ElementType.Fire,
                attackType,
                AttackTag.None,
                1d,
                appliesCombo,
                finalComboCount,
                false,
                actionLocalDealtDamageIncreaseRate: actionLocalRate,
                skillCoefficientResolver: skillResolver,
                dealtDamageIncreaseRateResolver: dealtDamageResolver);
        }

        private static DamageContext Build(
            DamageContextBuildRequest request)
        {
            return new DamageContextFactory(new SeededRandomSource(1))
                .Build(request);
        }

        private static DamageResult Calculate(
            DamageContextBuildRequest request)
        {
            return DamageCalculator.Calculate(Build(request));
        }

        private static BossDamageResult BossDamage(long damage)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                damage,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }

        private static void CreateBattle(
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Fire,
                100,
                1000d);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                100d);
        }
    }
}

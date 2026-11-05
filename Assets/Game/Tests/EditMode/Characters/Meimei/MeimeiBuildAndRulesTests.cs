using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiBuildAndRulesTests
    {
        private MeimeiCombatConfig config;
        private CharacterDefinition definition;
        private CharacterBuildResolver buildResolver;

        [SetUp]
        public void SetUp()
        {
            config = MeimeiTestConfig.Create();
            definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(
                definition,
                typeof(GameDefinition),
                "id",
                MeimeiRules.CharacterId);
            SetField(definition, "element", ElementType.Fire);
            SetField(definition, "level1Hp", 900);
            SetField(definition, "level1Attack", 180);
            SetField(definition, "level100Hp", 3400);
            SetField(definition, "level100Attack", 1050);
            SetField(definition, "combatConfig", config);
            buildResolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new MeimeiBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(1, 0, 900L, 180L)]
        [TestCase(50, 0, 2137L, 611L)]
        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 3, 3570L, 1103L)]
        [TestCase(100, 4, 3570L, 1103L)]
        [TestCase(100, 5, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
        public void BuildUsesLinearStatsAndAwakeningMultipliers(
            int level,
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = buildResolver.Resolve(
                definition,
                level,
                awakening);

            Assert.That(result.CharacterId,
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [TestCase(0, 1.25d, 2.20d, 3.60d, 10.00d, 0d)]
        [TestCase(1, 1.25d, 2.20d, 3.60d, 10.00d, 0d)]
        [TestCase(2, 1.25d, 2.20d, 3.60d, 10.00d, 0d)]
        [TestCase(3, 1.40d, 2.50d, 3.60d, 10.00d, 0d)]
        [TestCase(4, 1.40d, 2.50d, 4.10d, 10.00d, 0d)]
        [TestCase(5, 1.40d, 2.50d, 4.10d, 12.00d, 0d)]
        [TestCase(6, 1.40d, 2.50d, 4.10d, 12.00d, 0.06d)]
        public void RulesResolveEveryAwakeningThreshold(
            int awakening,
            double match3,
            double match4,
            double match5,
            double additional,
            double criticalPerTipsiness)
        {
            MeimeiResolvedCombatRules rules =
                MeimeiCombatRuleResolver.Resolve(config, awakening);

            Assert.That(rules.Match3Coefficient, Is.EqualTo(match3));
            Assert.That(rules.Match4Coefficient, Is.EqualTo(match4));
            Assert.That(rules.Match5Coefficient, Is.EqualTo(match5));
            Assert.That(rules.DrunkenImmortalDanceCoefficient,
                Is.EqualTo(additional));
            Assert.That(rules.CriticalChancePerTipsiness,
                Is.EqualTo(criticalPerTipsiness));
            Assert.That(rules.TipsinessMaxAmount, Is.EqualTo(5));
            Assert.That(rules.Match4TipsinessGainAmount, Is.EqualTo(1));
            Assert.That(rules.Match5TipsinessGainAmount, Is.EqualTo(2));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(1));
        }

        [Test]
        public void AwakeningSixCriticalChanceUsesEffectiveTipsiness()
        {
            MeimeiResolvedCombatRules rules =
                MeimeiCombatRuleResolver.Resolve(config, 6);

            Assert.That(rules.CalculateCriticalChanceIncreaseRate(0),
                Is.Zero);
            Assert.That(rules.CalculateCriticalChanceIncreaseRate(3),
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(rules.CalculateCriticalChanceIncreaseRate(5),
                Is.EqualTo(0.30d).Within(0.000000001d));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolverAndBuildModifierRejectInvalidAwakening(
            int awakening)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MeimeiCombatRuleResolver.Resolve(config, awakening));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MeimeiBuildModifier().GetStatMultipliers(awakening));
        }

        [Test]
        public void LookupSkillDescriptionsUseResolvedAwakeningValues()
        {
            var provider = new MeimeiLookupDescriptionProvider();

            CharacterLookupDescriptionDefinition match3Before =
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionDefinition match3After =
                provider.ResolveSkill(
                    definition,
                    3,
                    CharacterLookupSkillType.Match3);
            CharacterLookupDescriptionDefinition match4After =
                provider.ResolveSkill(
                    definition,
                    3,
                    CharacterLookupSkillType.Match4);
            CharacterLookupDescriptionDefinition match5After =
                provider.ResolveSkill(
                    definition,
                    4,
                    CharacterLookupSkillType.Match5);
            CharacterLookupDescriptionDefinition passiveBefore =
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Passive);
            CharacterLookupDescriptionDefinition passiveAfter =
                provider.ResolveSkill(
                    definition,
                    5,
                    CharacterLookupSkillType.Passive);
            CharacterLookupDescriptionDefinition active =
                provider.ResolveSkill(
                    definition,
                    6,
                    CharacterLookupSkillType.Active);

            Assert.That(provider.CharacterId,
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(match3Before.Title.Key,
                Is.EqualTo("meimei.skill.match3.name"));
            AssertArgument(match3Before, 0, "coefficient", "125");
            AssertArgument(match3After, 0, "coefficient", "140");
            AssertArgument(match4After, 0, "coefficient", "250");
            AssertArgument(match4After, 0, "tipsiness_gain", 1);
            AssertArgument(match5After, 0, "coefficient", "410");
            AssertArgument(match5After, 0, "tipsiness_gain", 2);
            Assert.That(passiveBefore.Sections.Count, Is.EqualTo(2));
            AssertArgument(passiveBefore, 1, "coefficient", "1000");
            AssertArgument(passiveAfter, 1, "coefficient", "1200");
            AssertArgument(active, 0, "maximum_conversion", 5);
            AssertArgument(active, 0, "cooldown", 1);
        }

        [Test]
        public void LookupAwakeningsExposeEveryChangedRule()
        {
            var provider = new MeimeiLookupDescriptionProvider();
            CharacterLookupDescriptionDefinition[] awakenings = new
                CharacterLookupDescriptionDefinition[6];
            for (int stage = 1; stage <= 6; stage++)
            {
                awakenings[stage - 1] =
                    provider.ResolveAwakening(definition, stage);
                Assert.That(awakenings[stage - 1].Title.Key,
                    Is.EqualTo("character.lookup.awakening.title"));
                Assert.That(
                    awakenings[stage - 1].Sections[0].Key,
                    Is.EqualTo(
                        $"meimei.awakening.{stage}.description"));
                Assert.That(
                    awakenings[stage - 1].Title.Arguments["stage"],
                    Is.EqualTo(stage));
            }

            AssertArgument(awakenings[0], 0,
                "multiplier_before", "100");
            AssertArgument(awakenings[0], 0,
                "multiplier_after", "105");
            AssertArgument(awakenings[1], 0,
                "multiplier_before", "100");
            AssertArgument(awakenings[1], 0,
                "multiplier_after", "105");
            AssertArgument(awakenings[2], 0, "match3_before", "125");
            AssertArgument(awakenings[2], 0, "match3_after", "140");
            AssertArgument(awakenings[2], 0, "match4_before", "220");
            AssertArgument(awakenings[2], 0, "match4_after", "250");
            AssertArgument(awakenings[3], 0, "match5_before", "360");
            AssertArgument(awakenings[3], 0, "match5_after", "410");
            AssertArgument(awakenings[4], 0,
                "additional_before", "1000");
            AssertArgument(awakenings[4], 0,
                "additional_after", "1200");
            AssertArgument(awakenings[5], 0,
                "critical_before", "0");
            AssertArgument(awakenings[5], 0,
                "critical_after", "6");
        }

        [Test]
        public void LookupRejectsWrongDefinitionAndInvalidStages()
        {
            var provider = new MeimeiLookupDescriptionProvider();

            Assert.Throws<ArgumentNullException>(() =>
                provider.ResolveSkill(
                    null,
                    0,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 0));
            SetField(
                definition,
                typeof(GameDefinition),
                "id",
                "character_other");
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));
        }

        private static void AssertArgument(
            CharacterLookupDescriptionDefinition description,
            int sectionIndex,
            string key,
            object expected)
        {
            Assert.That(
                description.Sections[sectionIndex]
                    .Arguments.ContainsKey(key),
                Is.True,
                key);
            Assert.That(
                description.Sections[sectionIndex].Arguments[key],
                Is.EqualTo(expected),
                key);
        }

        private static void SetField(
            CharacterDefinition target,
            string fieldName,
            object value)
        {
            SetField(
                target,
                typeof(CharacterDefinition),
                fieldName,
                value);
        }

        private static void SetField(
            CharacterDefinition target,
            Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}

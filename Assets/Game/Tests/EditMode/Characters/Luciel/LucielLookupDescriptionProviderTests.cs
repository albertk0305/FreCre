using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielLookupDescriptionProviderTests
    {
        private LucielCombatConfig config;
        private CharacterDefinition definition;
        private LucielLookupDescriptionProvider provider;

        [SetUp]
        public void SetUp()
        {
            config = LucielTestConfig.Create();
            definition = LucielTestConfig.Definition(config);
            provider = new LucielLookupDescriptionProvider();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(0, "6", "11", "18", "12", "5", "45", "0")]
        [TestCase(1, "6", "11", "18", "12", "5", "45", "0")]
        [TestCase(2, "6", "11", "18", "12", "5", "45", "5")]
        [TestCase(3, "7", "12", "18", "12", "5", "45", "5")]
        [TestCase(4, "7", "12", "20", "14", "5", "45", "5")]
        [TestCase(5, "7", "12", "20", "14", "6", "45", "5")]
        [TestCase(6, "7", "12", "20", "14", "6", "52", "5")]
        public void SkillDescriptionsUseResolvedRulesForEveryAwakening(
            int awakening,
            string match3Shield,
            string match4Shield,
            string match5Shield,
            string match5DamageIncrease,
            string passiveReduction,
            string activeShield,
            string localShieldIncrease)
        {
            IReadOnlyDictionary<string, object> match3 = SkillArguments(
                awakening,
                CharacterLookupSkillType.Match3);
            IReadOnlyDictionary<string, object> match4 = SkillArguments(
                awakening,
                CharacterLookupSkillType.Match4);
            IReadOnlyDictionary<string, object> match5 = SkillArguments(
                awakening,
                CharacterLookupSkillType.Match5);
            IReadOnlyDictionary<string, object> passive = SkillArguments(
                awakening,
                CharacterLookupSkillType.Passive);
            IReadOnlyDictionary<string, object> active = SkillArguments(
                awakening,
                CharacterLookupSkillType.Active);

            Assert.That(match3["shield_rate"], Is.EqualTo(match3Shield));
            Assert.That(match4["shield_rate"], Is.EqualTo(match4Shield));
            Assert.That(match5["shield_rate"], Is.EqualTo(match5Shield));
            Assert.That(match5["damage_increase"],
                Is.EqualTo(match5DamageIncrease));
            Assert.That(passive["damage_reduction"],
                Is.EqualTo(passiveReduction));
            Assert.That(active["shield_rate"], Is.EqualTo(activeShield));
            Assert.That(match3["shield_amount_increase"],
                Is.EqualTo(localShieldIncrease));
            Assert.That(match4["shield_amount_increase"],
                Is.EqualTo(localShieldIncrease));
            Assert.That(match5["shield_amount_increase"],
                Is.EqualTo(localShieldIncrease));
            Assert.That(active["shield_amount_increase"],
                Is.EqualTo(localShieldIncrease));
        }

        [Test]
        public void SkillDescriptionsExposeConfirmedKeysAndFixedContracts()
        {
            CharacterLookupDescriptionDefinition passive =
                provider.ResolveSkill(
                    definition,
                    awakening: 0,
                    CharacterLookupSkillType.Passive);
            CharacterLookupDescriptionDefinition match5 =
                provider.ResolveSkill(
                    definition,
                    awakening: 0,
                    CharacterLookupSkillType.Match5);
            CharacterLookupDescriptionDefinition active =
                provider.ResolveSkill(
                    definition,
                    awakening: 0,
                    CharacterLookupSkillType.Active);

            Assert.That(passive.Title.Key,
                Is.EqualTo("luciel.skill.passive.name"));
            Assert.That(passive.Sections[0].Key,
                Is.EqualTo("luciel.skill.passive.description"));
            Assert.That(match5.Sections[0].Arguments["duration"],
                Is.EqualTo(3));
            Assert.That(active.Title.Key,
                Is.EqualTo("luciel.skill.active.name"));
            Assert.That(active.Sections[0].Key,
                Is.EqualTo("luciel.skill.active.description"));
            Assert.That(active.Sections[0].Arguments["current_hp_cost"],
                Is.EqualTo("5"));
            Assert.That(active.Sections[0].Arguments["minimum_hp"],
                Is.EqualTo(1L));
            Assert.That(active.Sections[0].Arguments["cooldown"],
                Is.EqualTo(8));
        }

        [Test]
        public void AwakeningDescriptionsExposeEveryStageDelta()
        {
            AssertAwakening(1, "multiplier_before", "100");
            AssertAwakening(1, "multiplier_after", "105");
            AssertAwakening(2, "shield_increase_before", "0");
            AssertAwakening(2, "shield_increase_after", "5");
            AssertAwakening(3, "match3_before", "6");
            AssertAwakening(3, "match3_after", "7");
            AssertAwakening(3, "match4_before", "11");
            AssertAwakening(3, "match4_after", "12");
            AssertAwakening(4, "shield_before", "18");
            AssertAwakening(4, "shield_after", "20");
            AssertAwakening(4, "damage_increase_before", "12");
            AssertAwakening(4, "damage_increase_after", "14");
            AssertAwakening(5, "reduction_before", "5");
            AssertAwakening(5, "reduction_after", "6");
            AssertAwakening(6, "shield_before", "45");
            AssertAwakening(6, "shield_after", "52");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void AwakeningDescriptionsUseStandardLocalizationKeys(
            int stage)
        {
            CharacterLookupDescriptionDefinition description =
                provider.ResolveAwakening(definition, stage);

            Assert.That(description.Title.Key,
                Is.EqualTo("character.lookup.awakening.title"));
            Assert.That(description.Title.Arguments["stage"],
                Is.EqualTo(stage));
            var name = (CharacterLookupLocalizedText)
                description.Title.Arguments["name"];
            Assert.That(name.Key,
                Is.EqualTo($"luciel.awakening.{stage}.name"));
            Assert.That(description.Sections[0].Key,
                Is.EqualTo($"luciel.awakening.{stage}.description"));
        }

        [Test]
        public void LookupCallsDoNotMutateDefinitionOrConfig()
        {
            for (int awakening = 0; awakening <= 6; awakening++)
            {
                provider.ResolveSkill(
                    definition,
                    awakening,
                    CharacterLookupSkillType.Match3);
                provider.ResolveSkill(
                    definition,
                    awakening,
                    CharacterLookupSkillType.Match5);
                provider.ResolveSkill(
                    definition,
                    awakening,
                    CharacterLookupSkillType.Active);
            }

            for (int stage = 1; stage <= 6; stage++)
            {
                provider.ResolveAwakening(definition, stage);
            }

            Assert.That(definition.Id,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(definition.CombatConfig, Is.SameAs(config));
            Assert.That(config.Match3ShieldMaxHpRate, Is.EqualTo(0.06d));
            Assert.That(config.Awakening3Match3ShieldMaxHpRate,
                Is.EqualTo(0.07d));
            Assert.That(config.Match5PartyDealtDamageIncreaseRate,
                Is.EqualTo(0.12d));
            Assert.That(config.ActiveShieldMaxHpRate, Is.EqualTo(0.45d));
        }

        [Test]
        public void ProviderRejectsWrongDefinitionConfigAndStages()
        {
            Assert.Throws<ArgumentNullException>(() =>
                provider.ResolveSkill(
                    null,
                    0,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    -1,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    7,
                    CharacterLookupSkillType.Match3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    (CharacterLookupSkillType)999));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 7));

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "character_other";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));

            serialized.FindProperty("id").stringValue =
                LucielRules.CharacterId;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));
        }

        [Test]
        public void ProviderRejectsInvalidCombatConfig()
        {
            LucielTestConfig.SetInt(config, "activeCooldownTurns", 7);

            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Active));
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveAwakening(definition, 2));
        }

        private IReadOnlyDictionary<string, object> SkillArguments(
            int awakening,
            CharacterLookupSkillType type)
        {
            return provider.ResolveSkill(
                definition,
                awakening,
                type).Sections[0].Arguments;
        }

        private void AssertAwakening(
            int stage,
            string argument,
            object expected)
        {
            CharacterLookupDescriptionDefinition description =
                provider.ResolveAwakening(definition, stage);
            Assert.That(description.Sections[0].Arguments[argument],
                Is.EqualTo(expected));
        }
    }
}

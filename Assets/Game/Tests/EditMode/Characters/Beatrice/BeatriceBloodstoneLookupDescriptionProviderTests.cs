using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneLookupDescriptionProviderTests
    {
        private BeatriceBloodstoneCombatConfig config;
        private CharacterDefinition definition;
        private BeatriceBloodstoneLookupDescriptionProvider provider;

        [SetUp]
        public void SetUp()
        {
            config = BeatriceBloodstoneTestConfig.Create();
            definition = BeatriceBloodstoneTestConfig.Definition(config);
            provider = new BeatriceBloodstoneLookupDescriptionProvider();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void SkillIdsAndLookupKeysCoverAllFiveSkills()
        {
            Assert.That(provider.CharacterId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(BeatriceBloodstoneRules.PassiveSkillId,
                Is.EqualTo("passive_beatrice_bloodstone_crimson_thirst"));
            Assert.That(BeatriceBloodstoneRules.Match3SkillId,
                Is.EqualTo("skill_beatrice_bloodstone_match3"));
            Assert.That(BeatriceBloodstoneRules.Match4SkillId,
                Is.EqualTo("skill_beatrice_bloodstone_match4"));
            Assert.That(BeatriceBloodstoneRules.Match5SkillId,
                Is.EqualTo("skill_beatrice_bloodstone_match5"));
            Assert.That(BeatriceBloodstoneRules.ActiveAbilityId,
                Is.EqualTo(
                    "active_beatrice_bloodstone_crimson_eclipse"));

            AssertSkillKey(CharacterLookupSkillType.Passive, "passive");
            AssertSkillKey(CharacterLookupSkillType.Match3, "match3");
            AssertSkillKey(CharacterLookupSkillType.Match4, "match4");
            AssertSkillKey(CharacterLookupSkillType.Match5, "match5");
            AssertSkillKey(CharacterLookupSkillType.Active, "active");
        }

        [Test]
        public void SkillDescriptionsUseResolvedPartyHpRules()
        {
            CharacterLookupDescriptionDefinition passive0 = Skill(
                CharacterLookupSkillType.Passive,
                awakening: 0);
            CharacterLookupDescriptionDefinition passive3 = Skill(
                CharacterLookupSkillType.Passive,
                awakening: 3);
            CharacterLookupDescriptionDefinition match3Before = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 0);
            CharacterLookupDescriptionDefinition match3After = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 4);
            CharacterLookupDescriptionDefinition match4Before = Skill(
                CharacterLookupSkillType.Match4,
                awakening: 0);
            CharacterLookupDescriptionDefinition match4After = Skill(
                CharacterLookupSkillType.Match4,
                awakening: 4);
            CharacterLookupDescriptionDefinition match5Before = Skill(
                CharacterLookupSkillType.Match5,
                awakening: 0);
            CharacterLookupDescriptionDefinition match5After = Skill(
                CharacterLookupSkillType.Match5,
                awakening: 5);
            CharacterLookupDescriptionDefinition activeBefore = Skill(
                CharacterLookupSkillType.Active,
                awakening: 0);
            CharacterLookupDescriptionDefinition activeAfter = Skill(
                CharacterLookupSkillType.Active,
                awakening: 6);

            AssertArgument(passive0, "max_damage_increase", "20");
            AssertArgument(passive3, "max_damage_increase", "24");

            AssertArgument(match3Before, "hp_cost", "3");
            AssertArgument(match3Before, "base_coefficient", "90");
            AssertArgument(match3Before, "low_hp_threshold", "50");
            AssertArgument(match3Before,
                "conditional_coefficient", "20");
            AssertArgument(match3After, "base_coefficient", "100");
            AssertArgument(match3After,
                "conditional_coefficient", "22");

            AssertArgument(match4Before, "hp_cost", "6");
            AssertArgument(match4Before, "base_coefficient", "150");
            AssertArgument(match4Before,
                "conditional_coefficient", "80");
            AssertArgument(match4After, "base_coefficient", "165");
            AssertArgument(match4After,
                "conditional_coefficient", "88");

            AssertArgument(match5Before, "base_coefficient", "270");
            AssertArgument(match5Before,
                "lost_hp_bonus_coefficient", "200");
            AssertArgument(match5Before, "lifesteal_min", "10");
            AssertArgument(match5Before, "lifesteal_max", "30");
            AssertArgument(match5After, "base_coefficient", "300");
            AssertArgument(match5After,
                "lost_hp_bonus_coefficient", "220");
            AssertArgument(match5After, "lifesteal_max", "30");

            AssertArgument(activeBefore, "base_coefficient", "350");
            AssertArgument(activeBefore,
                "lost_hp_bonus_coefficient", "200");
            AssertArgument(activeBefore, "lifesteal_min", "15");
            AssertArgument(activeBefore, "lifesteal_max", "35");
            AssertArgument(activeBefore, "cooldown", 8);
            AssertArgument(activeAfter, "base_coefficient", "400");
            AssertArgument(activeAfter,
                "lost_hp_bonus_coefficient", "240");
            AssertArgument(activeAfter, "lifesteal_max", "35");
            AssertArgument(activeAfter, "cooldown", 8);
        }

        [Test]
        public void EveryAwakeningLevelCanResolveEverySkill()
        {
            foreach (int awakening in Enumerable.Range(0, 7))
            {
                foreach (CharacterLookupSkillType skillType in Enum.GetValues(
                    typeof(CharacterLookupSkillType)))
                {
                    CharacterLookupDescriptionDefinition description =
                        provider.ResolveSkill(
                            definition,
                            awakening,
                            skillType);

                    Assert.That(description, Is.Not.Null);
                    Assert.That(description.Sections.Count,
                        Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void AwakeningDescriptionsExposeAdjacentResolvedValues()
        {
            CharacterLookupDescriptionDefinition awakening1 = Awakening(1);
            CharacterLookupDescriptionDefinition awakening2 = Awakening(2);
            CharacterLookupDescriptionDefinition awakening3 = Awakening(3);
            CharacterLookupDescriptionDefinition awakening4 = Awakening(4);
            CharacterLookupDescriptionDefinition awakening5 = Awakening(5);
            CharacterLookupDescriptionDefinition awakening6 = Awakening(6);

            AssertArgument(awakening1, "multiplier_before", "100");
            AssertArgument(awakening1, "multiplier_after", "105");
            AssertArgument(awakening2, "multiplier_before", "100");
            AssertArgument(awakening2, "multiplier_after", "105");
            AssertArgument(awakening3, "damage_before", "20");
            AssertArgument(awakening3, "damage_after", "24");

            AssertArgument(awakening4, "match3_base_before", "90");
            AssertArgument(awakening4, "match3_base_after", "100");
            AssertArgument(awakening4,
                "match3_conditional_before", "20");
            AssertArgument(awakening4,
                "match3_conditional_after", "22");
            AssertArgument(awakening4, "match4_base_before", "150");
            AssertArgument(awakening4, "match4_base_after", "165");
            AssertArgument(awakening4,
                "match4_conditional_before", "80");
            AssertArgument(awakening4,
                "match4_conditional_after", "88");

            AssertArgument(awakening5, "base_before", "270");
            AssertArgument(awakening5, "base_after", "300");
            AssertArgument(awakening5,
                "lost_hp_bonus_before", "200");
            AssertArgument(awakening5,
                "lost_hp_bonus_after", "220");

            AssertArgument(awakening6, "base_before", "350");
            AssertArgument(awakening6, "base_after", "400");
            AssertArgument(awakening6,
                "lost_hp_bonus_before", "200");
            AssertArgument(awakening6,
                "lost_hp_bonus_after", "240");

            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionDefinition description =
                    Awakening(stage);
                Assert.That(description.Title.Key,
                    Is.EqualTo("character.lookup.awakening.title"));
                Assert.That(description.Title.Arguments["stage"],
                    Is.EqualTo(stage));
                var name = description.Title.Arguments["name"]
                    as CharacterLookupLocalizedText;
                Assert.That(name, Is.Not.Null);
                Assert.That(name.Key,
                    Is.EqualTo($"beatrice.awakening.{stage}.name"));
                Assert.That(description.Sections[0].Key,
                    Is.EqualTo(
                        $"beatrice.awakening.{stage}.description"));
            }
        }

        [Test]
        public void ConfigChangesFlowIntoLookupArguments()
        {
            BeatriceBloodstoneTestConfig.SetDouble(
                config,
                "match3HpCostRate",
                0.04d);
            BeatriceBloodstoneTestConfig.SetDouble(
                config,
                "match3BaseCoefficient",
                0.95d);
            BeatriceBloodstoneTestConfig.SetDouble(
                config,
                "activeBaseLifestealRate",
                0.16d);
            BeatriceBloodstoneTestConfig.SetInt(
                config,
                "activeCooldownTurns",
                9);

            CharacterLookupDescriptionDefinition match3 = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 0);
            CharacterLookupDescriptionDefinition active = Skill(
                CharacterLookupSkillType.Active,
                awakening: 0);

            AssertArgument(match3, "hp_cost", "4");
            AssertArgument(match3, "base_coefficient", "95");
            AssertArgument(active, "lifesteal_min", "16");
            AssertArgument(active, "lifesteal_max", "36");
            AssertArgument(active, "cooldown", 9);
        }

        [Test]
        public void ProviderRejectsInvalidDefinitionsAwakeningsAndSkillType()
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
                provider.ResolveAwakening(definition, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveAwakening(definition, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    (CharacterLookupSkillType)999));

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "wrong_character";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));

            CharacterDefinition missingConfig =
                BeatriceBloodstoneTestConfig.Definition(null);
            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    provider.ResolveSkill(
                        missingConfig,
                        0,
                        CharacterLookupSkillType.Match3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(missingConfig);
            }
        }

        [Test]
        public void DefaultRuntimeRegistrationProjectsFreshFactoriesOnly()
        {
            var registration = new CharacterRuntimeRegistration(
                BeatriceBloodstoneRules.CharacterId,
                () => new BeatriceBloodstoneBuildModifier(),
                () => new BeatriceBloodstoneCombatProviderRegistrar(),
                () => new BeatriceBloodstoneLookupDescriptionProvider());
            var isolatedCatalog = new CharacterRuntimeRegistrationCatalog(
                new[] { registration });
            CharacterRuntimeRegistrationCatalog defaultCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(BeatriceBloodstoneCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<BeatriceBloodstoneBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<BeatriceBloodstoneCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<BeatriceBloodstoneLookupDescriptionProvider>());
            Assert.That(registration.CreateBuildModifier(),
                Is.Not.SameAs(registration.CreateBuildModifier()));
            Assert.That(registration.CreateCombatRegistrar(),
                Is.Not.SameAs(registration.CreateCombatRegistrar()));
            Assert.That(registration.CreateLookupProvider(),
                Is.Not.SameAs(registration.CreateLookupProvider()));
            Assert.That(isolatedCatalog.Registrations.Count,
                Is.EqualTo(1));
            CharacterRuntimeRegistration defaultRegistration =
                defaultCatalog.Registrations.Single(item =>
                    item.CharacterId
                        == BeatriceBloodstoneRules.CharacterId);
            Assert.That(defaultCatalog.Registrations.Count(item =>
                    item.CharacterId
                        == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(defaultRegistration.SupportedCombatConfigType,
                Is.EqualTo(typeof(BeatriceBloodstoneCombatConfig)));
            Assert.That(defaultRegistration.CreateBuildModifier(),
                Is.TypeOf<BeatriceBloodstoneBuildModifier>());
            Assert.That(defaultRegistration.CreateCombatRegistrar(),
                Is.TypeOf<BeatriceBloodstoneCombatProviderRegistrar>());
            Assert.That(defaultRegistration.CreateLookupProvider(),
                Is.TypeOf<BeatriceBloodstoneLookupDescriptionProvider>());
            Assert.That(defaultRegistration.CreateBuildModifier(),
                Is.Not.SameAs(
                    defaultRegistration.CreateBuildModifier()));
            Assert.That(defaultRegistration.CreateCombatRegistrar(),
                Is.Not.SameAs(
                    defaultRegistration.CreateCombatRegistrar()));
            Assert.That(defaultRegistration.CreateLookupProvider(),
                Is.Not.SameAs(
                    defaultRegistration.CreateLookupProvider()));
            Assert.That(defaultCatalog.Registrations.Select(item =>
                item.CharacterId).Distinct().Count(),
                Is.EqualTo(defaultCatalog.Registrations.Count));
        }

        private CharacterLookupDescriptionDefinition Skill(
            CharacterLookupSkillType skillType,
            int awakening)
        {
            return provider.ResolveSkill(
                definition,
                awakening,
                skillType);
        }

        private CharacterLookupDescriptionDefinition Awakening(int stage)
        {
            return provider.ResolveAwakening(definition, stage);
        }

        private void AssertSkillKey(
            CharacterLookupSkillType skillType,
            string tier)
        {
            CharacterLookupDescriptionDefinition description = Skill(
                skillType,
                awakening: 0);
            Assert.That(description.Title.Key,
                Is.EqualTo($"beatrice.skill.{tier}.name"));
            Assert.That(description.Sections[0].Key,
                Is.EqualTo($"beatrice.skill.{tier}.description"));
        }

        private static void AssertArgument(
            CharacterLookupDescriptionDefinition description,
            string key,
            object expected)
        {
            Assert.That(description.Sections.Count, Is.EqualTo(1));
            Assert.That(description.Sections[0].Arguments.ContainsKey(key),
                Is.True,
                $"Missing lookup argument '{key}'.");
            Assert.That(description.Sections[0].Arguments[key],
                Is.EqualTo(expected));
        }
    }
}

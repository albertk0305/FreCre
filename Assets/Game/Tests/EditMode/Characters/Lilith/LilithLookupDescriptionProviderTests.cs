using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithLookupDescriptionProviderTests
    {
        private LilithCombatConfig config;
        private CharacterDefinition definition;
        private LilithLookupDescriptionProvider provider;

        [SetUp]
        public void SetUp()
        {
            config = LilithTestConfig.Create();
            definition = LilithTestConfig.Definition(config);
            provider = new LilithLookupDescriptionProvider();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void StableIdsAndLookupKeysCoverAllFiveSkills()
        {
            Assert.That(provider.CharacterId,
                Is.EqualTo(LilithRules.CharacterId));
            Assert.That(LilithRules.Match3SkillId,
                Is.EqualTo("skill_lilith_match3"));
            Assert.That(LilithRules.Match4SkillId,
                Is.EqualTo("skill_lilith_match4"));
            Assert.That(LilithRules.Match5SkillId,
                Is.EqualTo("skill_lilith_match5"));
            Assert.That(LilithRules.PassiveSkillId,
                Is.EqualTo("passive_lilith_demonic_covenant"));
            Assert.That(LilithRules.ActiveAbilityId,
                Is.EqualTo("active_lilith_soul_bargain"));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                LilithRules.Match3SkillId,
                LilithRules.Match4SkillId,
                LilithRules.Match5SkillId,
                LilithRules.PassiveSkillId,
                LilithRules.ActiveAbilityId
            }));

            AssertSkillKey(CharacterLookupSkillType.Match3, "match3");
            AssertSkillKey(CharacterLookupSkillType.Match4, "match4");
            AssertSkillKey(CharacterLookupSkillType.Match5, "match5");
            AssertSkillKey(CharacterLookupSkillType.Passive, "passive");
            AssertSkillKey(CharacterLookupSkillType.Active, "active");
        }

        [Test]
        public void SkillArgumentsUseResolvedAwakeningRules()
        {
            CharacterLookupDescriptionDefinition match3Before = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 0);
            CharacterLookupDescriptionDefinition match3After = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 3);
            CharacterLookupDescriptionDefinition match4After = Skill(
                CharacterLookupSkillType.Match4,
                awakening: 3);
            CharacterLookupDescriptionDefinition match5Before = Skill(
                CharacterLookupSkillType.Match5,
                awakening: 0);
            CharacterLookupDescriptionDefinition match5After = Skill(
                CharacterLookupSkillType.Match5,
                awakening: 4);
            CharacterLookupDescriptionDefinition passiveBefore = Skill(
                CharacterLookupSkillType.Passive,
                awakening: 0);
            CharacterLookupDescriptionDefinition passiveFive = Skill(
                CharacterLookupSkillType.Passive,
                awakening: 5);
            CharacterLookupDescriptionDefinition passiveSix = Skill(
                CharacterLookupSkillType.Passive,
                awakening: 6);
            CharacterLookupDescriptionDefinition active = Skill(
                CharacterLookupSkillType.Active,
                awakening: 6);

            AssertArgument(match3Before, "damage_coefficient", "70");
            AssertArgument(match3Before, "contract_gain", 1);
            AssertArgument(match3After, "damage_coefficient", "75");
            AssertArgument(match4After, "damage_coefficient", "130");
            AssertArgument(match4After, "contract_gain", 2);

            AssertArgument(match5Before, "damage_coefficient", "190");
            AssertArgument(match5Before, "damage_reduction", "20");
            AssertArgument(match5Before, "duration_turns", 2);
            AssertArgument(match5After, "damage_coefficient", "210");
            AssertArgument(match5After, "damage_reduction", "25");
            AssertArgument(match5After, "contract_gain", 3);

            AssertArgument(passiveBefore,
                "damage_increase_per_stack", "1.5");
            AssertArgument(passiveBefore,
                "explosion_coefficient_per_unit", "300");
            AssertArgument(passiveBefore, "contract_max", 12);
            AssertArgument(passiveBefore, "consumption_unit", 4);
            AssertArgument(passiveFive,
                "explosion_coefficient_per_unit", "330");
            AssertArgument(passiveSix,
                "damage_increase_per_stack", "1.8");

            AssertArgument(active, "current_hp_cost", "15");
            AssertArgument(active, "contract_gain", 2);
            AssertArgument(active, "minimum_hp", 1L);
            AssertArgument(active, "cooldown", 8);
        }

        [Test]
        public void EveryAwakeningCanResolveEverySkill()
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
            AssertArgument(awakening3, "match3_before", "70");
            AssertArgument(awakening3, "match3_after", "75");
            AssertArgument(awakening3, "match4_before", "120");
            AssertArgument(awakening3, "match4_after", "130");
            AssertArgument(awakening4, "match5_before", "190");
            AssertArgument(awakening4, "match5_after", "210");
            AssertArgument(awakening4,
                "damage_reduction_before", "20");
            AssertArgument(awakening4,
                "damage_reduction_after", "25");
            AssertArgument(awakening5, "explosion_before", "300");
            AssertArgument(awakening5, "explosion_after", "330");
            AssertArgument(awakening6,
                "damage_increase_before", "1.5");
            AssertArgument(awakening6,
                "damage_increase_after", "1.8");

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
                    Is.EqualTo($"lilith.awakening.{stage}.name"));
                Assert.That(description.Sections[0].Key,
                    Is.EqualTo(
                        $"lilith.awakening.{stage}.description"));
            }
        }

        [Test]
        public void ConfigChangesFlowIntoLookupArguments()
        {
            LilithTestConfig.SetField(
                config,
                "match3Coefficient",
                0.71d);
            LilithTestConfig.SetField(
                config,
                "activeCurrentPartyHpCostRate",
                0.16d);
            LilithTestConfig.SetField(
                config,
                "activeCooldownTurns",
                9);

            CharacterLookupDescriptionDefinition match3 = Skill(
                CharacterLookupSkillType.Match3,
                awakening: 0);
            CharacterLookupDescriptionDefinition active = Skill(
                CharacterLookupSkillType.Active,
                awakening: 0);

            AssertArgument(match3, "damage_coefficient", "71");
            AssertArgument(active, "current_hp_cost", "16");
            AssertArgument(active, "cooldown", 9);
        }

        [Test]
        public void ProviderRejectsInvalidDefinitionsAndSelections()
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

            LilithTestConfig.SetField(
                definition,
                typeof(GameDefinition),
                "id",
                "wrong_character");
            Assert.Throws<ArgumentException>(() =>
                provider.ResolveSkill(
                    definition,
                    0,
                    CharacterLookupSkillType.Match3));

            CharacterDefinition missing = LilithTestConfig.Definition(null);
            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    provider.ResolveSkill(
                        missing,
                        0,
                        CharacterLookupSkillType.Match3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(missing);
            }
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
                Is.EqualTo($"lilith.skill.{tier}.name"));
            Assert.That(description.Sections[0].Key,
                Is.EqualTo($"lilith.skill.{tier}.description"));
        }

        private static void AssertArgument(
            CharacterLookupDescriptionDefinition description,
            string key,
            object expected)
        {
            Assert.That(description.Sections.Count,
                Is.EqualTo(1));
            Assert.That(description.Sections[0].Arguments.ContainsKey(key),
                Is.True,
                $"Missing lookup argument '{key}'.");
            Assert.That(description.Sections[0].Arguments[key],
                Is.EqualTo(expected));
        }
    }
}

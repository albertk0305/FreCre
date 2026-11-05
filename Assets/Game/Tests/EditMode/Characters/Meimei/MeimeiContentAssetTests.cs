using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiContentAssetTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Meimei/meimei_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/meimei.asset";
        private const string SkillFolder = "Assets/Data/Skills/Meimei/";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/meimei_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/MeimeiFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/MeimeiFull.png";

        private static readonly string[] MainSkillIds =
        {
            "skill_meimei_drunken_fist",
            "skill_meimei_staggering_chain_kick",
            "skill_meimei_dead_drunk_barrage",
            "passive_meimei_three_cup_drunken_fist",
            MeimeiRules.ActiveAbilityId
        };

        private static readonly string[] StatusSkillIds =
        {
            MeimeiRules.FirstCupStatusId,
            MeimeiRules.SecondCupStatusId,
            MeimeiRules.FinalCupStatusId,
            MeimeiRules.TipsinessStatusId
        };

        [Test]
        public void CombatConfigAssetContainsCanonicalValues()
        {
            MeimeiCombatConfig config = Load<MeimeiCombatConfig>(ConfigPath);

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.Match3Coefficient, Is.EqualTo(1.25d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(2.20d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(3.60d));
            Assert.That(config.DrunkenImmortalDanceCoefficient,
                Is.EqualTo(10.00d));
            Assert.That(config.TipsinessMaxAmount, Is.EqualTo(5));
            Assert.That(config.Match4TipsinessGainAmount, Is.EqualTo(1));
            Assert.That(config.Match5TipsinessGainAmount, Is.EqualTo(2));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(1));
            Assert.That(config.Awakening3Match3Coefficient,
                Is.EqualTo(1.40d));
            Assert.That(config.Awakening3Match4Coefficient,
                Is.EqualTo(2.50d));
            Assert.That(config.Awakening4Match5Coefficient,
                Is.EqualTo(4.10d));
            Assert.That(config.Awakening5DrunkenImmortalDanceCoefficient,
                Is.EqualTo(12.00d));
            Assert.That(config.Awakening6CriticalChancePerTipsiness,
                Is.EqualTo(0.06d));
        }

        [Test]
        public void CharacterDefinitionUsesCanonicalIdentityStatsAndSkills()
        {
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
            MeimeiCombatConfig config = Load<MeimeiCombatConfig>(ConfigPath);

            Assert.That(definition.Id, Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("meimei"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(definition.Level1Hp, Is.EqualTo(900));
            Assert.That(definition.Level1Attack, Is.EqualTo(180));
            Assert.That(definition.Level100Hp, Is.EqualTo(3400));
            Assert.That(definition.Level100Attack, Is.EqualTo(1050));
            Assert.That(definition.CombatConfig, Is.SameAs(config));
            Assert.That(definition.SkillIds, Is.EqualTo(MainSkillIds));
            Assert.That(definition.SkillIds, Has.Count.EqualTo(5));
            Assert.That(definition.SkillIds, Is.Unique);
            Assert.That(definition.SkillIds.Intersect(StatusSkillIds),
                Is.Empty);
        }

        [TestCase(
            "skill_meimei_drunken_fist",
            "meimei.skill.match3",
            SkillKind.Match)]
        [TestCase(
            "skill_meimei_staggering_chain_kick",
            "meimei.skill.match4",
            SkillKind.Match)]
        [TestCase(
            "skill_meimei_dead_drunk_barrage",
            "meimei.skill.match5",
            SkillKind.Match)]
        [TestCase(
            "passive_meimei_three_cup_drunken_fist",
            "meimei.skill.passive",
            SkillKind.Passive)]
        [TestCase(
            MeimeiRules.ActiveAbilityId,
            "meimei.skill.active",
            SkillKind.Active)]
        public void MainSkillDefinitionsUseCanonicalMetadata(
            string id,
            string localizationPrefix,
            SkillKind kind)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                SkillFolder + id + ".asset");

            Assert.That(skill.Id, Is.EqualTo(id));
            Assert.That(skill.DisplayNameKey,
                Is.EqualTo(localizationPrefix + ".name"));
            Assert.That(skill.DescriptionKey,
                Is.EqualTo(localizationPrefix + ".description"));
            Assert.That(skill.SkillKind, Is.EqualTo(kind));
            AssertOptionalSpriteAsset(skill.Icon);
        }

        [TestCase(
            MeimeiRules.FirstCupStatusId,
            "meimei.status.first_cup",
            "Assets/Sprites/Effects/Meimei/FirstCup.png")]
        [TestCase(
            MeimeiRules.SecondCupStatusId,
            "meimei.status.second_cup",
            "Assets/Sprites/Effects/Meimei/SecondCup.png")]
        [TestCase(
            MeimeiRules.FinalCupStatusId,
            "meimei.status.final_cup",
            "Assets/Sprites/Effects/Meimei/ThirdCup.png")]
        [TestCase(
            MeimeiRules.TipsinessStatusId,
            "meimei.status.tipsiness",
            "Assets/Sprites/Effects/Meimei/Tipsiness.png")]
        public void StatusSkillDefinitionsUseCanonicalIcons(
            string id,
            string localizationPrefix,
            string iconPath)
        {
            SkillDefinition status = Load<SkillDefinition>(
                SkillFolder + id + ".asset");

            Assert.That(status.Id, Is.EqualTo(id));
            Assert.That(status.DisplayNameKey,
                Is.EqualTo(localizationPrefix + ".name"));
            Assert.That(status.DescriptionKey,
                Is.EqualTo(localizationPrefix + ".description"));
            Assert.That(status.SkillKind, Is.EqualTo(SkillKind.Passive));
            Assert.That(AssetDatabase.GetAssetPath(status.Icon),
                Is.EqualTo(iconPath));
        }

        [Test]
        public void PresentationUsesInspectorAssignedMeimeiSprites()
        {
            CharacterPresentationDefinition presentation = Load<
                CharacterPresentationDefinition>(PresentationPath);

            Assert.That(presentation.CharacterId,
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(
                AssetDatabase.GetAssetPath(presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
        }

        [Test]
        public void DatabaseAndCatalogContainMeimeiWithoutDuplicateIds()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            var expectedSkills = new HashSet<string>(
                MainSkillIds.Concat(StatusSkillIds),
                StringComparer.Ordinal);

            database.Initialize();
            presentationCatalog.Initialize();
            Assert.That(database.TryGetCharacter(
                MeimeiRules.CharacterId,
                out CharacterDefinition definition), Is.True);
            Assert.That(definition, Is.SameAs(
                Load<CharacterDefinition>(CharacterPath)));
            foreach (string skillId in expectedSkills)
            {
                Assert.That(database.TryGetSkill(
                    skillId,
                    out SkillDefinition skill), Is.True, skillId);
                Assert.That(skill, Is.Not.Null, skillId);
            }

            Assert.That(database.Skills.Count(skill => skill != null
                    && expectedSkills.Contains(skill.Id)),
                Is.EqualTo(expectedSkills.Count));
            Assert.That(presentationCatalog.TryGet(
                MeimeiRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation, Is.SameAs(Load<
                CharacterPresentationDefinition>(PresentationPath)));
        }

        [Test]
        public void DefaultRuntimeCatalogUsesMeimeiFactories()
        {
            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item => item.CharacterId
                        == MeimeiRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(MeimeiCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<MeimeiBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<MeimeiCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<MeimeiLookupDescriptionProvider>());
            Assert.That(
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                Is.Not.Null);
        }

        [Test]
        public void LocalizationResolvesEveryMeimeiDynamicDescription()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            TextAsset localizationAsset = Load<TextAsset>(LocalizationPath);
            database.Initialize();
            var localization = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    localizationAsset.name,
                    localizationAsset.text)
            });
            CharacterLookupDescriptionResolver resolver =
                CharacterLookupDescriptionResolverFactory.CreateDefault(
                    database,
                    localization);
            var skillTypes = new[]
            {
                CharacterLookupSkillType.Match3,
                CharacterLookupSkillType.Match4,
                CharacterLookupSkillType.Match5,
                CharacterLookupSkillType.Passive,
                CharacterLookupSkillType.Active
            };

            foreach (string locale in new[] { "en", "ko" })
            {
                localization.SetLocale(locale);
                Assert.That(localization.GetText("meimei"), Is.Not.Empty);
                foreach (CharacterLookupSkillType skillType in skillTypes)
                {
                    CharacterLookupDescriptionResult result =
                        resolver.ResolveSkill(
                            MeimeiRules.CharacterId,
                            currentAwakening: 6,
                            skillType);
                    AssertResolved(result);
                }

                for (int stage = 1; stage <= 6; stage++)
                {
                    AssertResolved(resolver.ResolveAwakening(
                        MeimeiRules.CharacterId,
                        stage));
                }
            }
        }

        [Test]
        public void CanonicalRegistrationValidationAcceptsMeimeiContent()
        {
            IReadOnlyList<string> issues =
                CharacterContentRegistrationValidator.Validate(
                    Load<DefinitionDatabase>(DatabasePath),
                    Load<CharacterPresentationCatalog>(
                        PresentationCatalogPath),
                    CharacterRuntimeRegistrationCatalog.CreateDefault());

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        private static void AssertResolved(
            CharacterLookupDescriptionResult result)
        {
            Assert.That(result.IsSuccess, Is.True, result.Error);
            Assert.That(result.Text, Does.Not.Contain("[["));
            Assert.That(result.Text, Does.Not.Contain("{"));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void AssertOptionalSpriteAsset(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(sprite), Is.Not.Empty);
        }
    }
}

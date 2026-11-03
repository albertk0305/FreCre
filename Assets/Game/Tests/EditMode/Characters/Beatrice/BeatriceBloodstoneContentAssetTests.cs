using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneContentAssetTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Beatrice/"
            + "beatrice_bloodstone_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/beatrice_bloodstone.asset";
        private const string SkillDirectory =
            "Assets/Data/Skills/Beatrice/";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/"
            + "beatrice_bloodstone_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/BeatriceFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/BeatriceFull.png";

        private static readonly string[] SkillIds =
        {
            BeatriceBloodstoneRules.Match3SkillId,
            BeatriceBloodstoneRules.Match4SkillId,
            BeatriceBloodstoneRules.Match5SkillId,
            BeatriceBloodstoneRules.PassiveSkillId,
            BeatriceBloodstoneRules.ActiveAbilityId
        };

        [Test]
        public void CombatConfigAssetContainsCanonicalValues()
        {
            BeatriceBloodstoneCombatConfig config = Load<
                BeatriceBloodstoneCombatConfig>(ConfigPath);

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.PassiveMaxDealtDamageIncreaseRate,
                Is.EqualTo(0.20d));
            Assert.That(config.Awakening3PassiveMaxDealtDamageIncreaseRate,
                Is.EqualTo(0.24d));
            Assert.That(config.Match3HpCostRate, Is.EqualTo(0.03d));
            Assert.That(config.Match4HpCostRate, Is.EqualTo(0.06d));
            Assert.That(config.LowHpThresholdRate, Is.EqualTo(0.50d));
            Assert.That(config.Match3BaseCoefficient, Is.EqualTo(0.90d));
            Assert.That(config.Match3LowHpBonusCoefficient,
                Is.EqualTo(0.20d));
            Assert.That(config.Match4BaseCoefficient, Is.EqualTo(1.50d));
            Assert.That(config.Match4LowHpBonusCoefficient,
                Is.EqualTo(0.80d));
            Assert.That(config.Match5BaseCoefficient, Is.EqualTo(2.70d));
            Assert.That(config.Match5LostHpBonusCoefficient,
                Is.EqualTo(2.00d));
            Assert.That(config.ActiveBaseCoefficient, Is.EqualTo(3.50d));
            Assert.That(config.ActiveLostHpBonusCoefficient,
                Is.EqualTo(2.00d));
            Assert.That(config.Match5BaseLifestealRate,
                Is.EqualTo(0.10d));
            Assert.That(config.Match5LostHpLifestealRate,
                Is.EqualTo(0.20d));
            Assert.That(config.ActiveBaseLifestealRate,
                Is.EqualTo(0.15d));
            Assert.That(config.ActiveLostHpLifestealRate,
                Is.EqualTo(0.20d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.Awakening4Match3BaseCoefficient,
                Is.EqualTo(1.00d));
            Assert.That(config.Awakening4Match3LowHpBonusCoefficient,
                Is.EqualTo(0.22d));
            Assert.That(config.Awakening4Match4BaseCoefficient,
                Is.EqualTo(1.65d));
            Assert.That(config.Awakening4Match4LowHpBonusCoefficient,
                Is.EqualTo(0.88d));
            Assert.That(config.Awakening5Match5BaseCoefficient,
                Is.EqualTo(3.00d));
            Assert.That(config.Awakening5Match5LostHpBonusCoefficient,
                Is.EqualTo(2.20d));
            Assert.That(config.Awakening6ActiveBaseCoefficient,
                Is.EqualTo(4.00d));
            Assert.That(config.Awakening6ActiveLostHpBonusCoefficient,
                Is.EqualTo(2.40d));
        }

        [Test]
        public void CharacterAndSkillsUseCanonicalIdentityAndMetadata()
        {
            CharacterDefinition character = Load<CharacterDefinition>(
                CharacterPath);
            BeatriceBloodstoneCombatConfig config = Load<
                BeatriceBloodstoneCombatConfig>(ConfigPath);

            Assert.That(character.Id,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(character.DisplayNameKey, Is.EqualTo("beatrice"));
            Assert.That(character.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(character.Level1Hp, Is.EqualTo(900));
            Assert.That(character.Level1Attack, Is.EqualTo(180));
            Assert.That(character.Level100Hp, Is.EqualTo(3400));
            Assert.That(character.Level100Attack, Is.EqualTo(1050));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(character.SkillIds, Is.EqualTo(SkillIds));
            Assert.That(character.SkillIds.Distinct().Count(),
                Is.EqualTo(SkillIds.Length));

            AssertSkill(
                BeatriceBloodstoneRules.Match3SkillId,
                SkillKind.Match,
                "match3");
            AssertSkill(
                BeatriceBloodstoneRules.Match4SkillId,
                SkillKind.Match,
                "match4");
            AssertSkill(
                BeatriceBloodstoneRules.Match5SkillId,
                SkillKind.Match,
                "match5");
            AssertSkill(
                BeatriceBloodstoneRules.PassiveSkillId,
                SkillKind.Passive,
                "passive");
            AssertSkill(
                BeatriceBloodstoneRules.ActiveAbilityId,
                SkillKind.Active,
                "active");
        }

        [Test]
        public void PresentationUsesValidExistingBeatriceSprites()
        {
            CharacterPresentationDefinition presentation = Load<
                CharacterPresentationDefinition>(PresentationPath);

            Assert.That(presentation.CharacterId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
            AssertSpriteReference(
                presentation.FaceSprite,
                "ed08c2d472b083249946577fe82ac203");
            AssertSpriteReference(
                presentation.PreviewSprite,
                "496aa55083b55e345acfc7c1e4d374c8");
        }

        [Test]
        public void DatabasePresentationAndRuntimeCatalogsResolveBeatrice()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentations = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            CharacterRuntimeRegistrationCatalog runtime =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            database.Initialize();
            presentations.Initialize();

            Assert.That(database.TryGetCharacter(
                    BeatriceBloodstoneRules.CharacterId,
                    out CharacterDefinition character),
                Is.True);
            Assert.That(character.CombatConfig,
                Is.TypeOf<BeatriceBloodstoneCombatConfig>());
            foreach (string skillId in SkillIds)
            {
                Assert.That(database.TryGetSkill(
                        skillId,
                        out SkillDefinition skill),
                    Is.True,
                    skillId);
                Assert.That(skill, Is.Not.Null, skillId);
            }

            Assert.That(presentations.TryGet(
                    BeatriceBloodstoneRules.CharacterId,
                    out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);
            Assert.That(runtime.Registrations.Count(registration =>
                    registration.CharacterId
                        == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(runtime.Registrations.Select(registration =>
                    registration.CharacterId).Distinct().Count(),
                Is.EqualTo(runtime.Registrations.Count));
        }

        [Test]
        public void LocalizationResolvesAllSkillsAndAwakeningsInBothLocales()
        {
            TextAsset textAsset = Load<TextAsset>(LocalizationPath);
            LocalizationTable table = LocalizationTableParser.Parse(
                textAsset.name,
                textAsset.text);
            var localization = new LocalizationService(new[] { table });
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            database.Initialize();
            CharacterLookupDescriptionResolver resolver =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateLookupDescriptionResolver(
                        database,
                        localization);

            foreach (string locale in new[] { "en", "ko" })
            {
                localization.SetLocale(locale);
                Assert.That(localization.GetText("beatrice"),
                    Does.Not.StartWith("[["));
                foreach (CharacterLookupSkillType skillType in
                    Enum.GetValues(typeof(CharacterLookupSkillType)))
                {
                    for (int awakening = 0; awakening <= 6; awakening++)
                    {
                        CharacterLookupDescriptionResult result =
                            resolver.ResolveSkill(
                                BeatriceBloodstoneRules.CharacterId,
                                awakening,
                                skillType);
                        Assert.That(result.IsSuccess,
                            Is.True,
                            result.Error);
                        Assert.That(result.Text, Does.Not.Contain("{"));
                    }
                }

                for (int stage = 1; stage <= 6; stage++)
                {
                    CharacterLookupDescriptionResult result =
                        resolver.ResolveAwakening(
                            BeatriceBloodstoneRules.CharacterId,
                            stage);
                    Assert.That(result.IsSuccess, Is.True, result.Error);
                    Assert.That(result.Text, Does.Not.Contain("{"));
                }
            }

            Assert.That(table.Entries[
                    "beatrice.skill.match3.description"]["en"],
                Does.Contain("shared party HP"));
            Assert.That(table.Entries[
                    "beatrice.skill.match3.description"]["en"],
                Does.Contain("Beatrice's final Max HP"));
            Assert.That(table.Entries[
                    "beatrice.skill.match3.description"]["ko"],
                Does.Contain("파티 공용 HP"));
            Assert.That(table.Entries[
                    "beatrice.skill.match3.description"]["ko"],
                Does.Contain("베아트리스 자신의 최종 최대 HP"));
        }

        [Test]
        public void ContentCrossValidationPassesWithAcquisitionEntries()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentations = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            CharacterRuntimeRegistrationCatalog runtime =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            database.Initialize();
            presentations.Initialize();

            ValidationReport dataReport = DataValidator.Validate(database);
            IReadOnlyList<string> registrationIssues =
                CharacterContentRegistrationValidator.Validate(
                    database,
                    presentations,
                    runtime);

            Assert.That(dataReport.HasErrors,
                Is.False,
                string.Join("\n", dataReport.Issues.Select(
                    issue => issue.Message)));
            Assert.That(registrationIssues, Is.Empty);
            Assert.That(database.TryGetGacha(
                    "gacha_character_standard",
                    out GachaDefinition standardCharacterGacha),
                Is.True);
            Assert.That(standardCharacterGacha.CharacterPoolIds.Count(id => id
                    == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(standardCharacterGacha.CharacterPoolIds,
                Does.Contain("character_marea_bluefang"));
            Assert.That(database.ExchangeCatalog.CharacterIds.Count(id =>
                    id == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(database.ExchangeCatalog.CharacterIds,
                Does.Contain("character_marea_bluefang"));
            Assert.That(database.TryGetCharacter(
                    BeatriceBloodstoneRules.CharacterId,
                    out CharacterDefinition acquisitionDefinition),
                Is.True);
            Assert.That(acquisitionDefinition.Id,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
        }

        private static void AssertSkill(
            string skillId,
            SkillKind expectedKind,
            string localizationTier)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                SkillDirectory + skillId + ".asset");
            Assert.That(skill.Id, Is.EqualTo(skillId));
            Assert.That(skill.SkillKind, Is.EqualTo(expectedKind));
            Assert.That(skill.DisplayNameKey,
                Is.EqualTo(
                    $"beatrice.skill.{localizationTier}.name"));
            Assert.That(skill.DescriptionKey,
                Is.EqualTo(
                    $"beatrice.skill.{localizationTier}.description"));
        }

        private static void AssertSpriteReference(
            Sprite sprite,
            string expectedGuid)
        {
            Assert.That(sprite, Is.Not.Null);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    sprite,
                    out string guid,
                    out long localId),
                Is.True);
            Assert.That(guid, Is.EqualTo(expectedGuid));
            Assert.That(localId, Is.Not.EqualTo(0));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithContentAssetTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Lilith/lilith_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/lilith.asset";
        private const string SkillDirectory =
            "Assets/Data/Skills/Lilith/";
        private const string EffectDirectory =
            "Assets/Data/Effects/Lilith/";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/lilith_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/LilithFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/LilithFull.png";

        private static readonly string[] SkillIds =
        {
            LilithRules.Match3SkillId,
            LilithRules.Match4SkillId,
            LilithRules.Match5SkillId,
            LilithRules.PassiveSkillId,
            LilithRules.ActiveAbilityId
        };

        [Test]
        public void CombatConfigAndCharacterContainCanonicalValues()
        {
            LilithCombatConfig config = Load<LilithCombatConfig>(ConfigPath);
            CharacterDefinition character = Load<CharacterDefinition>(
                CharacterPath);

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(config.InitialContractStackCount, Is.Zero);
            Assert.That(config.ContractMaxStackCount, Is.EqualTo(12));
            Assert.That(config.Match3ContractGainAmount, Is.EqualTo(1));
            Assert.That(config.Match4ContractGainAmount, Is.EqualTo(2));
            Assert.That(config.Match5ContractGainAmount, Is.EqualTo(3));
            Assert.That(config.ActiveContractGainAmount, Is.EqualTo(2));
            Assert.That(config.ContractExplosionMinimumStackCount,
                Is.EqualTo(4));
            Assert.That(config.ContractConsumptionUnit, Is.EqualTo(4));
            Assert.That(config.ContractDamageIncreaseRatePerStack,
                Is.EqualTo(0.015d));
            Assert.That(config.Awakening6ContractDamageIncreaseRatePerStack,
                Is.EqualTo(0.018d));
            Assert.That(config.ContractExplosionCoefficientPerUnit,
                Is.EqualTo(3.00d));
            Assert.That(config.Awakening5ContractExplosionCoefficientPerUnit,
                Is.EqualTo(3.30d));
            Assert.That(config.Match3Coefficient, Is.EqualTo(0.70d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(1.20d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(1.90d));
            Assert.That(config.Awakening3Match3Coefficient,
                Is.EqualTo(0.75d));
            Assert.That(config.Awakening3Match4Coefficient,
                Is.EqualTo(1.30d));
            Assert.That(config.Awakening4Match5Coefficient,
                Is.EqualTo(2.10d));
            Assert.That(config.CorruptedBlessingDamageReductionRate,
                Is.EqualTo(0.20d));
            Assert.That(
                config.Awakening4CorruptedBlessingDamageReductionRate,
                Is.EqualTo(0.25d));
            Assert.That(config.CorruptedBlessingDurationTurns,
                Is.EqualTo(2));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ActiveCurrentPartyHpCostRate,
                Is.EqualTo(0.15d));
            Assert.That(config.ActiveMinimumPartyHpAfterCost,
                Is.EqualTo(1L));
            Assert.That(config.ActiveUsableWhenActualHpCostIsZero,
                Is.True);

            Assert.That(character.Id, Is.EqualTo(LilithRules.CharacterId));
            Assert.That(character.DisplayNameKey, Is.EqualTo("lilith"));
            Assert.That(character.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(character.Level1Hp, Is.EqualTo(1000));
            Assert.That(character.Level1Attack, Is.EqualTo(150));
            Assert.That(character.Level100Hp, Is.EqualTo(3900));
            Assert.That(character.Level100Attack, Is.EqualTo(820));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(character.SkillIds, Is.EqualTo(SkillIds));

            AssertSkill(LilithRules.Match3SkillId, SkillKind.Match, "match3");
            AssertSkill(LilithRules.Match4SkillId, SkillKind.Match, "match4");
            AssertSkill(LilithRules.Match5SkillId, SkillKind.Match, "match5");
            AssertSkill(
                LilithRules.PassiveSkillId,
                SkillKind.Passive,
                "passive");
            AssertSkill(
                LilithRules.ActiveAbilityId,
                SkillKind.Active,
                "active");
            AssertEffect(
                LilithRules.CovenantEffectId,
                "demonic_covenant");
            AssertEffect(
                LilithRules.FallenBlessingEffectId,
                "fallen_blessing");
        }

        [Test]
        public void DatabasePresentationAndRuntimeCatalogResolveLilith()
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
                    LilithRules.CharacterId,
                    out CharacterDefinition character),
                Is.True);
            Assert.That(character.CombatConfig,
                Is.TypeOf<LilithCombatConfig>());
            foreach (string skillId in SkillIds)
            {
                Assert.That(database.TryGetSkill(
                        skillId,
                        out SkillDefinition skill),
                    Is.True,
                    skillId);
                Assert.That(skill, Is.Not.Null, skillId);
            }

            foreach (string effectId in new[]
            {
                LilithRules.CovenantEffectId,
                LilithRules.FallenBlessingEffectId
            })
            {
                EffectDefinition source = Load<EffectDefinition>(
                    EffectDirectory + effectId + ".asset");
                Assert.That(database.TryGetEffect(
                        effectId,
                        out EffectDefinition effect),
                    Is.True,
                    effectId);
                Assert.That(effect, Is.SameAs(source), effectId);
                AssertOptionalSpriteAsset(effect.Icon);
            }

            Assert.That(presentations.TryGet(
                    LilithRules.CharacterId,
                    out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));

            CharacterRuntimeRegistration registration = runtime
                .Registrations.Single(item =>
                    item.CharacterId == LilithRules.CharacterId);
            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(LilithCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<LilithBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<LilithCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<LilithLookupDescriptionProvider>());
        }

        [Test]
        public void LocalizationResolvesAllLookupTextInBothLocales()
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
                AssertLocalized(localization, "lilith");
                AssertLocalized(
                    localization,
                    "lilith.effect.demonic_covenant.name");
                AssertLocalized(
                    localization,
                    "lilith.effect.fallen_blessing.name");

                foreach (CharacterLookupSkillType skillType in
                    Enum.GetValues(typeof(CharacterLookupSkillType)))
                {
                    for (int awakening = 0; awakening <= 6; awakening++)
                    {
                        CharacterLookupDescriptionResult result =
                            resolver.ResolveSkill(
                                LilithRules.CharacterId,
                                awakening,
                                skillType);
                        Assert.That(result.IsSuccess, Is.True, result.Error);
                        Assert.That(result.Text, Does.Not.Contain("{"));
                    }
                }

                for (int stage = 1; stage <= 6; stage++)
                {
                    CharacterLookupDescriptionResult result =
                        resolver.ResolveAwakening(
                            LilithRules.CharacterId,
                            stage);
                    Assert.That(result.IsSuccess, Is.True, result.Error);
                    Assert.That(result.Text, Does.Not.Contain("{"));
                }
            }
        }

        [Test]
        public void ContentCrossValidationPasses()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentations = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            CharacterRuntimeRegistrationCatalog runtime =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            database.Initialize();
            presentations.Initialize();

            ValidationReport report = DataValidator.Validate(database);
            IReadOnlyList<string> registrationIssues =
                CharacterContentRegistrationValidator.Validate(
                    database,
                    presentations,
                    runtime);

            Assert.That(report.HasErrors,
                Is.False,
                string.Join("\n", report.Issues.Select(
                    issue => issue.Message)));
            Assert.That(registrationIssues, Is.Empty);
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
                Is.EqualTo($"lilith.skill.{localizationTier}.name"));
            Assert.That(skill.DescriptionKey,
                Is.EqualTo($"lilith.skill.{localizationTier}.description"));
            Assert.That(skill.Icon, Is.Null);
        }

        private static void AssertLocalized(
            LocalizationService localization,
            string key)
        {
            Assert.That(localization.GetText(key),
                Does.Not.StartWith("[["),
                key);
        }

        private static void AssertEffect(
            string effectId,
            string localizationTier)
        {
            EffectDefinition effect = Load<EffectDefinition>(
                EffectDirectory + effectId + ".asset");
            Assert.That(effect.Id, Is.EqualTo(effectId));
            Assert.That(effect.DisplayNameKey,
                Is.EqualTo($"lilith.effect.{localizationTier}.name"));
            Assert.That(effect.DescriptionKey,
                Is.EqualTo(
                    $"lilith.effect.{localizationTier}.description"));
            AssertOptionalSpriteAsset(effect.Icon);
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

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}

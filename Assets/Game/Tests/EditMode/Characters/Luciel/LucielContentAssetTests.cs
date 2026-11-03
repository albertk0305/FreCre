using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Editor.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielContentAssetTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Luciel/luciel_combat_config.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/luciel.asset";
        private const string SkillDirectory =
            "Assets/Data/Skills/Luciel/";
        private const string EffectDirectory =
            "Assets/Data/Effects/Luciel/";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/luciel_presentation.asset";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string LocalizationPath =
            "Assets/Data/Localization/characters.tsv";
        private const string FaceSpritePath =
            "Assets/Sprites/Character/Face/LucielFace.png";
        private const string PreviewSpritePath =
            "Assets/Sprites/Character/Full/LucielFull.png";

        private static readonly string[] SkillIds =
        {
            LucielRules.Match3SkillId,
            LucielRules.Match4SkillId,
            LucielRules.Match5SkillId,
            LucielRules.PassiveSkillId,
            LucielRules.ActiveAbilityId
        };

        private static readonly string[] EffectIds =
        {
            LucielRules.PassiveEffectId,
            LucielRules.Match5DamageIncreaseEffectId
        };

        [Test]
        public void CombatConfigAndCharacterContainCanonicalValues()
        {
            LucielCombatConfig config = Load<LucielCombatConfig>(
                ConfigPath);
            CharacterDefinition character = Load<CharacterDefinition>(
                CharacterPath);

            Assert.That(config.TryValidate(out string error), Is.True,
                error);
            Assert.That(config.PassiveDamageReductionRate,
                Is.EqualTo(0.05d));
            Assert.That(config.Awakening5PassiveDamageReductionRate,
                Is.EqualTo(0.06d));
            Assert.That(config.Match3ShieldMaxHpRate, Is.EqualTo(0.06d));
            Assert.That(config.Awakening3Match3ShieldMaxHpRate,
                Is.EqualTo(0.07d));
            Assert.That(config.Match4ShieldMaxHpRate, Is.EqualTo(0.11d));
            Assert.That(config.Awakening3Match4ShieldMaxHpRate,
                Is.EqualTo(0.12d));
            Assert.That(config.Match5ShieldMaxHpRate, Is.EqualTo(0.18d));
            Assert.That(config.Awakening4Match5ShieldMaxHpRate,
                Is.EqualTo(0.20d));
            Assert.That(config.Match5PartyDealtDamageIncreaseRate,
                Is.EqualTo(0.12d));
            Assert.That(
                config.Awakening4Match5PartyDealtDamageIncreaseRate,
                Is.EqualTo(0.14d));
            Assert.That(config.Match5BuffDurationTurns, Is.EqualTo(3));
            Assert.That(config.Match5BuffStackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(config.ActiveCurrentPartyHpCostRate,
                Is.EqualTo(0.05d));
            Assert.That(config.ActiveHpCostRoundingMode,
                Is.EqualTo(PartyHpCostRoundingMode.Ceiling));
            Assert.That(config.ActiveShieldMaxHpRate, Is.EqualTo(0.45d));
            Assert.That(config.Awakening6ActiveShieldMaxHpRate,
                Is.EqualTo(0.52d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.Awakening2ShieldAmountIncreaseRate,
                Is.EqualTo(0.05d));
            Assert.That(config.MatchShieldUsesComboMultiplier, Is.True);
            Assert.That(config.ActiveShieldUsesComboMultiplier, Is.False);
            Assert.That(config.ShieldDurationTurns, Is.Null);

            Assert.That(character.Id, Is.EqualTo(LucielRules.CharacterId));
            Assert.That(character.DisplayNameKey, Is.EqualTo("luciel"));
            Assert.That(character.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(character.Level1Hp, Is.EqualTo(1450));
            Assert.That(character.Level1Attack, Is.EqualTo(80));
            Assert.That(character.Level100Hp, Is.EqualTo(6000));
            Assert.That(character.Level100Attack, Is.EqualTo(375));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(character.SkillIds, Is.EqualTo(SkillIds));
        }

        [Test]
        public void SkillsAndEffectsUseCanonicalIdsAndValidOptionalIcons()
        {
            AssertSkill(LucielRules.Match3SkillId, SkillKind.Match,
                "match3");
            AssertSkill(LucielRules.Match4SkillId, SkillKind.Match,
                "match4");
            AssertSkill(LucielRules.Match5SkillId, SkillKind.Match,
                "match5");
            AssertSkill(LucielRules.PassiveSkillId, SkillKind.Passive,
                "passive");
            AssertSkill(LucielRules.ActiveAbilityId, SkillKind.Active,
                "active");

            AssertEffect(LucielRules.PassiveEffectId,
                "blessing_of_fallen_angel");
            AssertEffect(LucielRules.Match5DamageIncreaseEffectId,
                "match5_dealt_damage_increase");
        }

        [Test]
        public void DatabasePresentationAndRuntimeRegistrationResolveLuciel()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            CharacterPresentationCatalog presentations = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            CharacterRuntimeRegistrationCatalog runtime =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            database.Initialize();
            presentations.Initialize();

            Assert.That(database.Characters, Has.Count.EqualTo(14));
            Assert.That(database.Characters.Select(item => item.Id),
                Is.Unique);
            Assert.That(runtime.Registrations, Has.Count.EqualTo(14));
            Assert.That(runtime.Registrations.Select(item =>
                item.CharacterId), Is.Unique);
            foreach (CharacterRuntimeRegistration registration in
                runtime.Registrations)
            {
                Assert.That(database.TryGetCharacter(
                        registration.CharacterId,
                        out _),
                    Is.True,
                    registration.CharacterId);
            }

            Assert.That(database.TryGetCharacter(
                    LucielRules.CharacterId,
                    out CharacterDefinition character),
                Is.True);
            Assert.That(character.CombatConfig,
                Is.TypeOf<LucielCombatConfig>());
            foreach (string skillId in SkillIds)
            {
                Assert.That(database.TryGetSkill(skillId, out _), Is.True,
                    skillId);
            }

            foreach (string effectId in EffectIds)
            {
                Assert.That(database.TryGetEffect(effectId, out _), Is.True,
                    effectId);
            }

            Assert.That(presentations.TryGet(
                    LucielRules.CharacterId,
                    out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(AssetDatabase.GetAssetPath(presentation),
                Is.EqualTo(PresentationPath));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo(FaceSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                    presentation.PreviewSprite),
                Is.EqualTo(PreviewSpritePath));
        }

        [Test]
        public void LucielIsRegisteredExactlyOnceInEachAcquisitionSource()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);

            Assert.That(database.Gachas, Is.Not.Empty);
            foreach (GachaDefinition gacha in database.Gachas)
            {
                Assert.That(gacha.CharacterPoolIds.Count(id =>
                        id == LucielRules.CharacterId),
                    Is.EqualTo(1),
                    gacha.Id);
            }

            Assert.That(database.ExchangeCatalog, Is.Not.Null);
            Assert.That(database.ExchangeCatalog.CharacterIds.Count(id =>
                    id == LucielRules.CharacterId),
                Is.EqualTo(1));
        }

        [Test]
        public void LocalizationResolvesEveryLookupInBothLocales()
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
                    .CreateLookupDescriptionResolver(database, localization);

            foreach (string locale in new[] { "en", "ko" })
            {
                localization.SetLocale(locale);
                AssertLocalized(localization, "luciel");
                foreach (string effectTier in new[]
                {
                    "blessing_of_fallen_angel",
                    "match5_dealt_damage_increase"
                })
                {
                    AssertLocalized(localization,
                        $"effect.luciel.{effectTier}.name");
                    AssertLocalized(localization,
                        $"effect.luciel.{effectTier}.description");
                }

                foreach (CharacterLookupSkillType skillType in
                    Enum.GetValues(typeof(CharacterLookupSkillType)))
                {
                    for (int awakening = 0; awakening <= 6; awakening++)
                    {
                        CharacterLookupDescriptionResult result =
                            resolver.ResolveSkill(
                                LucielRules.CharacterId,
                                awakening,
                                skillType);
                        Assert.That(result.IsSuccess, Is.True,
                            result.Error);
                        Assert.That(result.Text, Does.Not.Contain("{"));
                    }
                }

                for (int stage = 1; stage <= 6; stage++)
                {
                    CharacterLookupDescriptionResult result =
                        resolver.ResolveAwakening(
                            LucielRules.CharacterId,
                            stage);
                    Assert.That(result.IsSuccess, Is.True, result.Error);
                    Assert.That(result.Text, Does.Not.Contain("{"));
                }
            }
        }

        [Test]
        public void ContentCrossValidationPassesWithoutDuplicateIds()
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
                string.Join("\n", report.Issues.Select(item =>
                    item.Message)));
            Assert.That(registrationIssues, Is.Empty);
        }

        private static void AssertSkill(
            string id,
            SkillKind kind,
            string localizationTier)
        {
            SkillDefinition skill = Load<SkillDefinition>(
                SkillDirectory + id + ".asset");
            Assert.That(skill.Id, Is.EqualTo(id));
            Assert.That(skill.SkillKind, Is.EqualTo(kind));
            Assert.That(skill.DisplayNameKey,
                Is.EqualTo($"luciel.skill.{localizationTier}.name"));
            Assert.That(skill.DescriptionKey,
                Is.EqualTo(
                    $"luciel.skill.{localizationTier}.description"));
            AssertOptionalSpriteAsset(skill.Icon);
        }

        private static void AssertEffect(string id, string tier)
        {
            EffectDefinition effect = Load<EffectDefinition>(
                EffectDirectory + id + ".asset");
            Assert.That(effect.Id, Is.EqualTo(id));
            Assert.That(effect.DisplayNameKey,
                Is.EqualTo($"effect.luciel.{tier}.name"));
            Assert.That(effect.DescriptionKey,
                Is.EqualTo($"effect.luciel.{tier}.description"));
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

        private static void AssertLocalized(
            LocalizationService localization,
            string key)
        {
            Assert.That(localization.GetText(key),
                Does.Not.StartWith("[["),
                key);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}


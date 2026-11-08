using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicContentDefinitionTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private static readonly double[] ExpectedRates =
        {
            0.05d,
            0.06d,
            0.07d,
            0.08d,
            0.09d,
            0.10d
        };

        [TestCase(
            "relic_victors_broken_sword",
            RelicEffectType.AttackIncrease)]
        [TestCase(
            "relic_world_tree_bark",
            RelicEffectType.MaxHpIncrease)]
        public void ProductionDatabase_ResolvesConfirmedRelicData(
            string relicId,
            RelicEffectType expectedEffectType)
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.EffectType, Is.EqualTo(expectedEffectType));
            Assert.That(relic.Effects, Has.Count.EqualTo(1));
            Assert.That(relic.Effects[0].DescriptionValueKey,
                Is.EqualTo("rate"));
            Assert.That(relic.EffectRates, Has.Count.EqualTo(6));
            Assert.That(relic.EffectRates, Is.EqualTo(ExpectedRates));
            for (int awakening = 0;
                awakening < ExpectedRates.Length;
                awakening++)
            {
                Assert.That(
                    relic.GetEffectRate(awakening),
                    Is.EqualTo(ExpectedRates[awakening]));
            }

            Assert.That(database.TryGetRelicPresentation(
                relicId,
                out RelicPresentationDefinition presentation), Is.True);
            Assert.That(presentation.RelicId, Is.EqualTo(relicId));
            Assert.That(
                presentation.Icon == null
                    || !string.IsNullOrEmpty(
                        AssetDatabase.GetAssetPath(presentation.Icon)),
                Is.True,
                "An assigned relic icon must be a persisted Sprite asset.");
        }

        [Test]
        public void ProductionRelicContent_PassesDataValidation()
        {
            ValidationReport report = DataValidator.Validate(LoadDatabase());

            Assert.That(
                report.HasErrors,
                Is.False,
                string.Join("\n", report.Issues));
        }

        [Test]
        public void ProductionDatabase_ResolvesCriticalRelicData()
        {
            DefinitionDatabase database = LoadDatabase();

            AssertCriticalRelic(
                database,
                "relic_clairvoyants_monocle",
                new[]
                {
                    (RelicEffectType.CriticalChanceIncrease,
                        "rate",
                        new[] { 0.04d, 0.05d, 0.06d, 0.07d, 0.08d, 0.10d })
                });
            AssertCriticalRelic(
                database,
                "relic_executioners_bloodstone",
                new[]
                {
                    (RelicEffectType.CriticalDamageIncrease,
                        "rate",
                        new[] { 0.10d, 0.12d, 0.14d, 0.16d, 0.18d, 0.20d })
                });
            AssertCriticalRelic(
                database,
                "relic_twin_stars_destiny_stone",
                new[]
                {
                    (RelicEffectType.CriticalChanceIncrease,
                        "critical_chance",
                        new[] { 0.02d, 0.03d, 0.04d, 0.04d, 0.05d, 0.06d }),
                    (RelicEffectType.CriticalDamageIncrease,
                        "critical_damage",
                        new[] { 0.06d, 0.07d, 0.08d, 0.10d, 0.11d, 0.12d })
                });
        }

        [Test]
        public void ProductionLocalization_FormatsCriticalRelicDescriptions()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService localization = catalog.CreateService();
            localization.SetLocale("en");

            AssertDescription(
                database,
                localization,
                "relic_clairvoyants_monocle",
                0,
                "Clairvoyant's Monocle",
                "Increases the equipped character's critical chance by 4%.");
            AssertDescription(
                database,
                localization,
                "relic_executioners_bloodstone",
                5,
                "Executioner's Bloodstone",
                "Increases the equipped character's critical damage by 20%.");
            AssertDescription(
                database,
                localization,
                "relic_twin_stars_destiny_stone",
                5,
                "Twin Stars' Destiny Stone",
                "Increases the equipped character's critical chance by 6% "
                    + "and critical damage by 12%.");

            localization.SetLocale("ko");
            AssertDescription(
                database,
                localization,
                "relic_twin_stars_destiny_stone",
                0,
                "쌍성의 운명석",
                "장착한 캐릭터의 치명타 확률이 2%, 치명타 피해가 6% "
                    + "증가합니다.");
        }

        [Test]
        public void ProductionLocalization_ResolvesRelicNamesAndDescriptions()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService localization = catalog.CreateService();
            localization.SetLocale("ko");

            AssertLocalized(
                database,
                localization,
                "relic_victors_broken_sword",
                "승전자의 부러진 검",
                "장착한 캐릭터의 공격력이 5% 증가합니다.");
            AssertLocalized(
                database,
                localization,
                "relic_world_tree_bark",
                "세계수의 수피",
                "장착한 캐릭터의 최대 HP가 5% 증가합니다.");
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static void AssertCriticalRelic(
            DefinitionDatabase database,
            string relicId,
            (RelicEffectType Type, string Key, double[] Rates)[] expected)
        {
            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.Effects, Has.Count.EqualTo(expected.Length));
            for (int effectIndex = 0;
                effectIndex < expected.Length;
                effectIndex++)
            {
                Assert.That(relic.Effects[effectIndex].EffectType,
                    Is.EqualTo(expected[effectIndex].Type));
                Assert.That(relic.Effects[effectIndex].DescriptionValueKey,
                    Is.EqualTo(expected[effectIndex].Key));
                Assert.That(relic.Effects[effectIndex].EffectRates,
                    Is.EqualTo(expected[effectIndex].Rates));
                Assert.That(relic.Effects[effectIndex].HasElementFilter,
                    Is.False);
            }

            Assert.That(database.TryGetRelicPresentation(
                relicId,
                out RelicPresentationDefinition presentation), Is.True);
            Assert.That(presentation.RelicId, Is.EqualTo(relicId));
            Assert.That(
                presentation.Icon == null
                    || !string.IsNullOrEmpty(
                        AssetDatabase.GetAssetPath(presentation.Icon)),
                Is.True,
                "An assigned relic icon must be a persisted Sprite asset.");
        }

        private static void AssertDescription(
            DefinitionDatabase database,
            LocalizationService localization,
            string relicId,
            int awakening,
            string expectedName,
            string expectedDescription)
        {
            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            var arguments = relic.GetDescriptionValues(awakening)
                .ToDictionary(
                    pair => pair.Key,
                    pair => (object)(pair.Value * 100d).ToString(
                        "0.##",
                        CultureInfo.InvariantCulture));
            Assert.That(localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(expectedName));
            Assert.That(
                localization.Format(relic.DescriptionKey, arguments),
                Is.EqualTo(expectedDescription));
        }

        private static void AssertLocalized(
            DefinitionDatabase database,
            LocalizationService localization,
            string relicId,
            string expectedName,
            string expectedDescription)
        {
            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(
                localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(expectedName));
            Assert.That(
                localization.Format(
                    relic.DescriptionKey,
                    new Dictionary<string, object>
                    {
                        ["rate"] = 5
                    }),
                Is.EqualTo(expectedDescription));
        }
    }
}

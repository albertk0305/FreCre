using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Orver;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode.Bosses.Orver
{
    public sealed class OrverContentRegistrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string DefaultSpritePath =
            "Assets/Sprites/Boss/Orver/OrverIdle.png";
        private const string FaceSpritePath =
            "Assets/Sprites/Boss/Orver/OrverFace.png";

        [Test]
        public void CanonicalDatabaseRegistersOrverContent()
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.TryGetBoss(
                OrverRules.BossId,
                out BossDefinition boss), Is.True);
            Assert.That(boss.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(ElementAffinityResolver.Resolve(
                ElementType.Fire,
                boss.Element), Is.GreaterThan(1d));
            Assert.That(boss.TurnLimit, Is.EqualTo(25));
            Assert.That(boss.DisplayNameKey, Is.EqualTo("orver"));
            Assert.That(boss.CombatConfig,
                Is.TypeOf<OrverCombatConfig>());
            Assert.That(boss.ActionOrSkillIds, Has.Count.EqualTo(6));
            Assert.That(boss.DifficultyStats, Has.Count.EqualTo(5));

            long[] expectedHp = { 33000, 66000, 99000, 138000, 198000 };
            double[] expectedAttack = { 500d, 850d, 1200d, 1550d, 2100d };
            for (int index = 0;
                index < BattleDifficultyIds.RequiredIds.Count;
                index++)
            {
                Assert.That(boss.TryGetDifficultyStats(
                    BattleDifficultyIds.RequiredIds[index],
                    out BossDifficultyStats stats), Is.True);
                Assert.That(stats.MaxHp, Is.EqualTo(expectedHp[index]));
                Assert.That(stats.Attack,
                    Is.EqualTo(expectedAttack[index]));
            }

            foreach (string skillId in boss.ActionOrSkillIds)
            {
                Assert.That(database.TryGetSkill(
                    skillId,
                    out SkillDefinition skill), Is.True, skillId);
                Assert.That(skill.SkillKind,
                    Is.EqualTo(SkillKind.BossAction));
            }

            Assert.That(database.TryGetBossPresentation(
                OrverRules.BossId,
                out BossPresentationDefinition presentation), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(
                presentation.DefaultSprite), Is.EqualTo(DefaultSpritePath));
            Assert.That(AssetDatabase.GetAssetPath(
                presentation.FaceSprite), Is.EqualTo(FaceSpritePath));
            Assert.That(presentation.StateVisuals, Is.Empty);
        }

        [Test]
        public void CanonicalOrverIsSupportedForEveryDifficulty()
        {
            DefinitionDatabase database = LoadDatabase();
            Assert.That(database.TryGetBoss(
                OrverRules.BossId,
                out BossDefinition boss), Is.True);
            Assert.That(BattleEncounterFactory.IsSupportedBoss(
                boss,
                out string supportError), Is.True, supportError);

            foreach (string difficultyId
                in BattleDifficultyIds.RequiredIds)
            {
                Assert.That(BattleEncounterFactory.TryCreate(
                    database,
                    new BattleEntryRequest(
                        OrverRules.BossId,
                        difficultyId),
                    out BattleEncounterDefinition encounter,
                    out string error), Is.True, error);
                Assert.That(encounter.Boss, Is.SameAs(boss));
                Assert.That(encounter.Difficulty.DifficultyId,
                    Is.EqualTo(difficultyId));
                Assert.That(encounter.RuntimeFactory,
                    Is.TypeOf<OrverBossCombatRuntimeFactory>());
            }
        }

        [Test]
        public void LocalizationContainsBossAndAllActionText()
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService service = catalog.CreateService();

            Assert.That(service.GetText("orver", "en"),
                Is.EqualTo("Orver"));
            Assert.That(service.GetText("orver", "ko"),
                Is.EqualTo("오르베르"));
            Assert.That(service.GetText("orver.description", "en"),
                Is.Not.EqualTo(LocalizationService.MissingKey(
                    "orver.description")));
            Assert.That(service.GetText("orver.description", "ko"),
                Is.Not.EqualTo(LocalizationService.MissingKey(
                    "orver.description")));
            string[] keys =
            {
                "ancient_strike",
                "root_infestation",
                "ancient_wrath",
                "root_lash",
                "life_siphon",
                "withered_strike"
            };
            foreach (string key in keys)
            {
                string nameKey = $"boss_action.orver.{key}.name";
                string descriptionKey =
                    $"boss_action.orver.{key}.description";
                Assert.That(service.GetText(nameKey, "en"),
                    Is.Not.EqualTo(LocalizationService.MissingKey(nameKey)));
                Assert.That(service.GetText(nameKey, "ko"),
                    Is.Not.EqualTo(LocalizationService.MissingKey(nameKey)));
                Assert.That(service.GetText(descriptionKey, "en"),
                    Is.Not.EqualTo(
                        LocalizationService.MissingKey(descriptionKey)));
                Assert.That(service.GetText(descriptionKey, "ko"),
                    Is.Not.EqualTo(
                        LocalizationService.MissingKey(descriptionKey)));
            }
        }

        [Test]
        public void CanonicalPresentationHasNoMissingSpriteError()
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.DoesNotThrow(() => DataValidator.Validate(database));
            ValidationReport report = DataValidator.Validate(database);
            Assert.That(database.TryGetBossPresentation(
                OrverRules.BossId,
                out BossPresentationDefinition presentation), Is.True);
            Assert.That(report.Issues.Any(issue =>
                issue.Severity == ValidationSeverity.Error
                && issue.Message.Contains(
                    "Boss presentation default sprite is missing: orver")),
                Is.False);
            Assert.That(presentation.DefaultSprite, Is.Not.Null);
            Assert.That(presentation.FaceSprite, Is.Not.Null);

            int missingIconCount = database.Skills.Count(skill =>
                skill != null
                && skill.Id.StartsWith("boss_action_orver_")
                && skill.Icon == null);
            Assert.That(report.Issues.Count(issue =>
                issue.Severity == ValidationSeverity.Warning
                && issue.Message.Contains("boss_action_orver_")
                && issue.Message.Contains("Icon is missing")),
                Is.EqualTo(missingIconCount));
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }
    }
}

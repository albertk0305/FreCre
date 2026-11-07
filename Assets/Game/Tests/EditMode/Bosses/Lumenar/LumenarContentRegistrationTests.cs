using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Bosses.Lumenar;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;

namespace ValorChronicle.Tests.EditMode.Bosses.Lumenar
{
    public sealed class LumenarContentRegistrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        [Test]
        public void CanonicalDatabaseRegistersLumenarDefinitionsAndValidOptionalSprites()
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.TryGetBoss(
                LumenarRules.BossId, out BossDefinition boss), Is.True);
            Assert.That(boss.Element, Is.EqualTo(ElementType.Light));
            Assert.That(boss.TurnLimit, Is.EqualTo(25));
            Assert.That(boss.CombatConfig,
                Is.TypeOf<LumenarCombatConfig>());
            Assert.That(boss.ActionOrSkillIds, Is.EqualTo(new[]
            {
                LumenarRules.RadiantStrikeSkillId,
                LumenarRules.CelestialJudgmentSkillId,
                LumenarRules.StoneStrikeSkillId,
                LumenarRules.StonefallCurseSkillId
            }));
            foreach (string skillId in boss.ActionOrSkillIds)
            {
                Assert.That(database.TryGetSkill(
                    skillId, out SkillDefinition skill), Is.True, skillId);
                Assert.That(skill.SkillKind, Is.EqualTo(SkillKind.BossAction));
                AssertOptionalSprite(skill.Icon, skillId);
            }
            Assert.That(database.TryGetEffect(
                LumenarRules.LightFormEffectId, out EffectDefinition light),
                Is.True);
            Assert.That(database.TryGetEffect(
                LumenarRules.StoneFormEffectId, out EffectDefinition stone),
                Is.True);
            AssertOptionalSprite(light.Icon, LumenarRules.LightFormEffectId);
            AssertOptionalSprite(stone.Icon, LumenarRules.StoneFormEffectId);

            Assert.That(database.TryGetBossPresentation(
                LumenarRules.BossId,
                out BossPresentationDefinition presentation), Is.True);
            Assert.That(presentation.BossId, Is.EqualTo(LumenarRules.BossId));
            Assert.That(presentation.AllowMissingSprites, Is.True);
            AssertOptionalSprite(presentation.DefaultSprite, "defaultSprite");
            AssertOptionalSprite(presentation.FaceSprite, "faceSprite");
            Assert.That(presentation.StateVisuals, Has.Count.EqualTo(2));
            Assert.That(presentation.StateVisuals.Select(
                entry => entry.VisualStateId), Is.EquivalentTo(new[]
                {
                    LumenarRules.LightVisualStateId,
                    LumenarRules.StoneVisualStateId
                }));
            foreach (BossVisualStateEntry entry in presentation.StateVisuals)
            {
                AssertOptionalSprite(entry.Sprite, entry.VisualStateId);
                bool hasSprite = presentation.TryGetStateSprite(
                    entry.VisualStateId, out Sprite resolvedSprite);
                Assert.That(hasSprite, Is.EqualTo(entry.Sprite != null));
                Assert.That(resolvedSprite, Is.SameAs(entry.Sprite));
            }

            ValidationReport report = DataValidator.Validate(database);
            Assert.That(report.Issues.Any(
                issue => issue.Severity == ValidationSeverity.Error
                    && issue.DefinitionId == LumenarRules.BossId), Is.False);
        }

        [Test]
        public void CanonicalLumenarIsBattleSupported()
        {
            DefinitionDatabase database = LoadDatabase();
            Assert.That(database.TryGetBoss(
                LumenarRules.BossId, out BossDefinition boss), Is.True);
            Assert.That(BattleEncounterFactory.IsSupportedBoss(
                boss, out string error), Is.True, error);
            BossCombatRuntimeFactoryCatalog catalog =
                BossCombatRuntimeFactoryCatalog.CreateDefault();
            Assert.That(catalog.TryGetFactory(
                LumenarRules.BossId,
                out IBossCombatRuntimeFactory factory), Is.True);
            Assert.That(factory,
                Is.TypeOf<LumenarBossCombatRuntimeFactory>());
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static void AssertOptionalSprite(Sprite sprite, string context)
        {
            if (sprite == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(sprite);
            Assert.That(path, Is.Not.Empty,
                $"{context} must reference a persisted Sprite asset.");
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(path),
                Does.Contain(sprite),
                $"{context} must resolve to the serialized Sprite asset.");
        }
    }
}

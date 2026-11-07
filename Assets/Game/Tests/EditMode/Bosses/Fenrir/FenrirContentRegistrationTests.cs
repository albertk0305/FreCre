using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Fenrir;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Fenrir
{
    public sealed class FenrirContentRegistrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        [Test]
        public void DatabaseRegistersFenrirContentAndFactory()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();

            Assert.That(
                database.TryGetBoss(
                    FenrirRules.BossId,
                    out BossDefinition boss),
                Is.True);
            Assert.That(boss.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(boss.TurnLimit, Is.EqualTo(25));
            Assert.That(boss.CombatConfig, Is.TypeOf<FenrirCombatConfig>());
            Assert.That(boss.ActionOrSkillIds,
                Is.EqualTo(new[]
                {
                    FenrirRules.SavageRendSkillId,
                    FenrirRules.RampagingClawSkillId
                }));
            Assert.That(
                boss.DifficultyStats.Select(item =>
                    (item.DifficultyId, item.MaxHp, item.Attack)),
                Is.EqualTo(new[]
                {
                    ("difficulty_intro", 33000L, 500d),
                    ("difficulty_normal", 66000L, 850d),
                    ("difficulty_advanced", 99000L, 1200d),
                    ("difficulty_hard", 138000L, 1550d),
                    ("difficulty_challenge", 198000L, 2100d)
                }));

            AssertBossAction(database, FenrirRules.SavageRendSkillId);
            AssertBossAction(database, FenrirRules.RampagingClawSkillId);
            Assert.That(database.TryGetEffect(
                    FenrirRules.WoundEffectId,
                    out EffectDefinition wound),
                Is.True);
            Assert.That(wound.HideInHud, Is.False);
            Assert.That(database.TryGetEffect(
                    FenrirRules.FrenzyEffectId,
                    out EffectDefinition frenzy),
                Is.True);
            Assert.That(frenzy.HideInHud, Is.False);
            Assert.That(database.TryGetEffect(
                    FenrirRules.FrenzyAttackEffectId,
                    out EffectDefinition attack),
                Is.True);
            Assert.That(attack.HideInHud, Is.True);

            Assert.That(database.TryGetBossPresentation(
                    FenrirRules.BossId,
                    out BossPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation.AllowMissingSprites, Is.True);
            Assert.That(presentation.StateVisuals.Single().VisualStateId,
                Is.EqualTo(FenrirRules.FrenziedVisualStateId));
            AssertOptionalSprite(
                presentation.DefaultSprite,
                "Fenrir default sprite");
            AssertOptionalSprite(
                presentation.FaceSprite,
                "Fenrir face sprite");
            AssertOptionalSprite(
                presentation.StateVisuals.Single().Sprite,
                "Fenrir Frenzied sprite");

            BossCombatRuntimeFactoryCatalog factories =
                BossCombatRuntimeFactoryCatalog.CreateDefault();
            Assert.That(factories.TryGetFactory(
                    FenrirRules.BossId,
                    out IBossCombatRuntimeFactory factory),
                Is.True);
            Assert.That(factory, Is.TypeOf<FenrirBossCombatRuntimeFactory>());
            Assert.That(factory.TryValidate(boss, out string errorMessage),
                Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public void FactoryCreatesIndependentRuntimeObjects()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            database.Initialize();
            Assert.That(database.TryGetBoss(
                    FenrirRules.BossId,
                    out BossDefinition definition),
                Is.True);
            var factory = new FenrirBossCombatRuntimeFactory();
            var firstBoss = new BossBattleState(
                FenrirRules.BossId,
                ElementType.Dark,
                100,
                10d);
            var secondBoss = new BossBattleState(
                FenrirRules.BossId,
                ElementType.Dark,
                100,
                10d);

            BossCombatRuntime first = factory.Create(
                definition,
                firstBoss,
                new CombatActionIdSequence());
            BossCombatRuntime second = factory.Create(
                definition,
                secondBoss,
                new CombatActionIdSequence());

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.ActionProvider, Is.Not.SameAs(
                second.ActionProvider));
            Assert.That(first.IntentSource, Is.Not.SameAs(
                second.IntentSource));
            Assert.That(first.TriggerRules.Single(), Is.Not.SameAs(
                second.TriggerRules.Single()));
        }

        private static void AssertBossAction(
            DefinitionDatabase database,
            string skillId)
        {
            Assert.That(database.TryGetSkill(
                    skillId,
                    out SkillDefinition skill),
                Is.True);
            Assert.That(skill.Id, Is.EqualTo(skillId));
            Assert.That(skill.SkillKind, Is.EqualTo(SkillKind.BossAction));
            AssertOptionalSprite(skill.Icon, $"{skillId} icon");
        }

        private static void AssertOptionalSprite(
            Sprite sprite,
            string context)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(
                AssetDatabase.GetAssetPath(sprite),
                Is.Not.Empty,
                $"{context} must reference a project asset when assigned.");
        }
    }
}

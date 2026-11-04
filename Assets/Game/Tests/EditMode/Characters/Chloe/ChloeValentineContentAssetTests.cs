using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Chloe;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Chloe
{
    public sealed class ChloeValentineContentAssetTests
    {
        [Test]
        public void CharacterContentIsRegisteredAcrossRuntimeAndDatabases()
        {
            CharacterDefinition character = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(
                "Assets/Data/Characters/chloe_valentine.asset");
            ChloeValentineCombatConfig config = AssetDatabase.LoadAssetAtPath<
                ChloeValentineCombatConfig>(
                "Assets/Data/CombatConfigs/Chloe/chloe_valentine_combat_config.asset");
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(
                "Assets/Data/Database/DefinitionDatabase.asset");
            CharacterPresentationCatalog presentations =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationCatalog>(
                    "Assets/Data/Party/CharacterPresentationCatalog.asset");
            GachaDefinition gacha = AssetDatabase.LoadAssetAtPath<
                GachaDefinition>(
                "Assets/Data/Gachas/gacha_character_standard.asset");
            ExchangeCatalogDefinition exchange =
                AssetDatabase.LoadAssetAtPath<ExchangeCatalogDefinition>(
                    "Assets/Data/Exchange/ExchangeCatalogDefinition.asset");

            Assert.That(character, Is.Not.Null);
            Assert.That(config, Is.Not.Null);
            Assert.That(database, Is.Not.Null);
            Assert.That(presentations, Is.Not.Null);
            Assert.That(gacha, Is.Not.Null);
            Assert.That(exchange, Is.Not.Null);
            Assert.That(character.Id,
                Is.EqualTo(ChloeValentineRules.CharacterId));
            Assert.That(character.Element, Is.EqualTo(ElementType.Light));
            Assert.That(character.Level1Hp, Is.EqualTo(1450));
            Assert.That(character.Level1Attack, Is.EqualTo(80));
            Assert.That(character.Level100Hp, Is.EqualTo(5800));
            Assert.That(character.Level100Attack, Is.EqualTo(400));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(character.SkillIds, Is.EqualTo(new[]
            {
                ChloeValentineRules.Match3SkillId,
                ChloeValentineRules.Match4SkillId,
                ChloeValentineRules.Match5SkillId,
                ChloeValentineRules.PassiveSkillId,
                ChloeValentineRules.ActiveAbilityId
            }));

            database.Initialize();
            Assert.That(database.TryGetCharacter(
                ChloeValentineRules.CharacterId,
                out CharacterDefinition registeredCharacter), Is.True);
            Assert.That(registeredCharacter, Is.SameAs(character));
            foreach (string skillId in character.SkillIds)
            {
                Assert.That(database.TryGetSkill(skillId, out _), Is.True,
                    skillId);
            }

            Assert.That(database.TryGetEffect(
                ChloeValentineRules.PassiveEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                ChloeValentineRules.Match5CriticalChanceEffectId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                ChloeValentineRules.ActiveCriticalDamageEffectId,
                out _), Is.True);
            presentations.Initialize();
            Assert.That(presentations.TryGet(
                ChloeValentineRules.CharacterId,
                out CharacterPresentationDefinition presentation), Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);
            Assert.That(presentation.PreviewSprite, Is.Not.Null);
            Assert.That(gacha.CharacterPoolIds.Count(id =>
                id == ChloeValentineRules.CharacterId), Is.EqualTo(1));
            Assert.That(exchange.CharacterIds.Count(id =>
                id == ChloeValentineRules.CharacterId), Is.EqualTo(1));
            Assert.That(gacha.SinglePullCost, Is.EqualTo(100));
            Assert.That(gacha.TenPullCost, Is.EqualTo(1000));
            Assert.That(gacha.PityPullCount, Is.EqualTo(50));
            Assert.That(exchange.CharacterExchangeCost, Is.EqualTo(60));

            CharacterRuntimeRegistrationCatalog runtime =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            Assert.That(runtime.Registrations.Count(item =>
                item.CharacterId == ChloeValentineRules.CharacterId),
                Is.EqualTo(1));
        }
    }
}


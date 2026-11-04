using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Anubis;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Anubis
{
    public sealed class AnubisContentAssetTests
    {
        [Test]
        public void CharacterConfigAndDefinitionsUseCanonicalValues()
        {
            CharacterDefinition character = Load<CharacterDefinition>(
                "Assets/Data/Characters/anubis.asset");
            var config = character.CombatConfig as AnubisCombatConfig;

            Assert.That(character.Id, Is.EqualTo(AnubisRules.CharacterId));
            Assert.That(character.Element, Is.EqualTo(ElementType.Light));
            Assert.That(character.Level1Hp, Is.EqualTo(1100));
            Assert.That(character.Level1Attack, Is.EqualTo(120));
            Assert.That(character.Level100Hp, Is.EqualTo(4300));
            Assert.That(character.Level100Attack, Is.EqualTo(650));
            Assert.That(character.SkillIds, Is.EqualTo(new[]
            {
                AnubisRules.Match3SkillId,
                AnubisRules.Match4SkillId,
                AnubisRules.Match5SkillId,
                AnubisRules.PassiveSkillId,
                AnubisRules.ActiveAbilityId
            }));
            Assert.That(config, Is.Not.Null);
            Assert.That(config.TryValidate(out string error), Is.True, error);
        }

        [Test]
        public void DatabaseCatalogAndRuntimeCatalogRegisterAnubis()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                "Assets/Data/Database/DefinitionDatabase.asset");
            CharacterPresentationCatalog presentation = Load<
                CharacterPresentationCatalog>(
                "Assets/Data/Party/CharacterPresentationCatalog.asset");
            database.Initialize();
            presentation.Initialize();

            Assert.That(database.TryGetCharacter(
                AnubisRules.CharacterId,
                out _), Is.True);
            Assert.That(database.TryGetEffect(
                AnubisRules.PassiveEffectId,
                out EffectDefinition passive), Is.True);
            Assert.That(database.TryGetEffect(
                AnubisRules.RadianceEffectId,
                out EffectDefinition radiance), Is.True);
            Assert.That(passive.Id, Is.EqualTo(AnubisRules.PassiveEffectId));
            Assert.That(radiance.Id, Is.EqualTo(AnubisRules.RadianceEffectId));
            Assert.That(presentation.TryGet(
                AnubisRules.CharacterId,
                out CharacterPresentationDefinition definition), Is.True);
            Assert.That(definition.CharacterId,
                Is.EqualTo(AnubisRules.CharacterId));

            CharacterRuntimeRegistration registration =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Single(item =>
                        item.CharacterId == AnubisRules.CharacterId);
            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(AnubisCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<AnubisBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<AnubisCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<AnubisLookupDescriptionProvider>());
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}

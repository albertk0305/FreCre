using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveSystemFactoryTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        private readonly List<string> roots = new List<string>();
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object value in objects)
                UnityEngine.Object.DestroyImmediate(value);
            objects.Clear();
            foreach (string root in roots)
                if (Directory.Exists(root)) Directory.Delete(root, true);
            roots.Clear();
        }

        [Test]
        public void Create_RealDatabase_ComposesServiceWithGameDefaultProfile()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_composition_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            DefinitionDatabase database = LoadDatabase();

            SaveService service = SaveSystemFactory.Create(root, database);
            SaveLoadResult result = service.LoadOrCreate("profile_composed");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.CreatedNewProfile));
            Assert.That(service.HasCurrentProfile, Is.True);
            Assert.That(service.CanWriteCurrentProfile, Is.True);
            Assert.That(File.Exists(Path.Combine(root, "profile.save")), Is.True);
            Assert.That(result.ProfileSnapshot.Currencies.GachaCurrency,
                Is.EqualTo(NewProfileFactory.InitialGold));
            Assert.That(
                result.ProfileSnapshot.Characters.Select(
                    character => character.CharacterId),
                Is.EqualTo(NewProfileFactory.GameStartingCharacterIds));
            Assert.That(
                result.ProfileSnapshot.Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(
                    NewProfileFactory.GameStartingPartyCharacterIds));
        }

        [Test]
        public void Create_ExistingProfileDoesNotRegrantMissingDefaults()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_composition_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            DefinitionDatabase database = LoadDatabase();
            SaveService first = SaveSystemFactory.Create(root, database);
            Assert.That(first.LoadOrCreate("profile_existing").IsSuccess,
                Is.True);
            Assert.That(first.ExecuteTransaction(profile =>
            {
                profile.Currencies.GachaCurrency = 4321L;
                profile.Characters[0].Level = 7;
                profile.Characters.RemoveAll(character =>
                    character.CharacterId
                        == NewProfileFactory.GameStartingCharacterIds[4]);
                profile.Party.Presets[0].CharacterSlotIds[1] =
                    SaveRules.EmptyId;
            }).IsSuccess, Is.True);

            SaveService restarted = SaveSystemFactory.Create(root, database);
            SaveLoadResult reloaded = restarted.LoadOrCreate("ignored");

            Assert.That(reloaded.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(reloaded.ProfileSnapshot.Currencies.GachaCurrency,
                Is.EqualTo(4321L));
            Assert.That(reloaded.ProfileSnapshot.Characters, Has.Count.EqualTo(4));
            Assert.That(reloaded.ProfileSnapshot.Characters[0].Level,
                Is.EqualTo(7));
            Assert.That(reloaded.ProfileSnapshot.Characters.Any(character =>
                    character.CharacterId
                        == NewProfileFactory.GameStartingCharacterIds[4]),
                Is.False);
            Assert.That(
                reloaded.ProfileSnapshot.Party.Presets[0].CharacterSlotIds[1],
                Is.EqualTo(SaveRules.EmptyId));
        }

        [Test]
        public void Create_UninitializedDatabase_ThrowsBeforeSaveIo()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_composition_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            objects.Add(database);

            Assert.Throws<InvalidOperationException>(
                () => SaveSystemFactory.Create(root, database));
            Assert.That(Directory.Exists(root), Is.False);
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            foreach (string characterId in
                NewProfileFactory.GameStartingCharacterIds)
            {
                Assert.That(database.TryGetCharacter(characterId, out _),
                    Is.True,
                    characterId);
            }

            return database;
        }
    }
}

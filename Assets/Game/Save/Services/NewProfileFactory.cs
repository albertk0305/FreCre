using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Services
{
    public sealed class NewProfileFactory
    {
        public const long InitialGold = 10000L;

        public static readonly IReadOnlyList<string> GameStartingCharacterIds =
            Array.AsReadOnly(new[]
            {
                "character_marea_bluefang",
                "character_livia",
                "character_shimotsuki_miyuki",
                "character_serena_coralspear",
                "character_fiora_amberwing"
            });

        public static readonly IReadOnlyList<string>
            GameStartingPartyCharacterIds = Array.AsReadOnly(new[]
            {
                "character_livia",
                "character_fiora_amberwing",
                "character_shimotsuki_miyuki",
                "character_serena_coralspear",
                "character_marea_bluefang"
            });

        private readonly IReadOnlyList<string> startingCharacterIds;
        private readonly IReadOnlyList<string> startingPartyCharacterIds;
        private readonly long initialGold;

        public NewProfileFactory()
            : this(Array.Empty<string>(), Array.Empty<string>(), 0L)
        {
        }

        private NewProfileFactory(
            IReadOnlyList<string> startingCharacterIds,
            IReadOnlyList<string> startingPartyCharacterIds,
            long initialGold)
        {
            this.startingCharacterIds = startingCharacterIds
                ?? throw new ArgumentNullException(nameof(startingCharacterIds));
            this.startingPartyCharacterIds = startingPartyCharacterIds
                ?? throw new ArgumentNullException(
                    nameof(startingPartyCharacterIds));
            if (startingCharacterIds.Count > SaveRules.PartySlotCount)
            {
                throw new ArgumentException(
                    "Starting characters cannot exceed the party slot count.",
                    nameof(startingCharacterIds));
            }
            if (initialGold < 0L)
                throw new ArgumentOutOfRangeException(nameof(initialGold));

            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < startingCharacterIds.Count; index++)
            {
                string characterId = startingCharacterIds[index];
                if (string.IsNullOrWhiteSpace(characterId)
                    || !uniqueIds.Add(characterId))
                {
                    throw new ArgumentException(
                        "Starting character IDs must be non-empty and unique.",
                        nameof(startingCharacterIds));
                }
            }

            if (startingPartyCharacterIds.Count > SaveRules.PartySlotCount)
            {
                throw new ArgumentException(
                    "Starting party cannot exceed the party slot count.",
                    nameof(startingPartyCharacterIds));
            }

            var uniquePartyIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0;
                index < startingPartyCharacterIds.Count;
                index++)
            {
                string characterId = startingPartyCharacterIds[index];
                if (!uniqueIds.Contains(characterId)
                    || !uniquePartyIds.Add(characterId))
                {
                    throw new ArgumentException(
                        "Starting party IDs must be unique owned characters.",
                        nameof(startingPartyCharacterIds));
                }
            }

            this.initialGold = initialGold;
        }

        public static NewProfileFactory CreateGameDefaults()
        {
            return new NewProfileFactory(
                GameStartingCharacterIds,
                GameStartingPartyCharacterIds,
                InitialGold);
        }

        public ProfileSaveData Create(string profileId, long utcUnixSeconds)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile ID cannot be empty or whitespace.", nameof(profileId));
            }

            return new ProfileSaveData
            {
                SaveVersion = SaveRules.CurrentSaveVersion,
                ProfileId = profileId,
                CreatedAtUtcUnixSeconds = utcUnixSeconds,
                LastSavedAtUtcUnixSeconds = utcUnixSeconds,
                Currencies = new CurrencySaveData
                {
                    // GachaCurrency is the persisted Gold balance used by the
                    // main screen, battle rewards, redeem codes, and gacha.
                    GachaCurrency = initialGold
                },
                Characters = CreateStartingCharacters(),
                RelicInstances = new List<RelicInstanceSaveData>(),
                Party = CreateDefaultParty(startingPartyCharacterIds),
                GachaStates = new List<GachaStateSaveData>(),
                BossRecords = new List<BossRecordSaveData>(),
                UnlockedContentIds = new List<string>(),
                CompletedTutorialIds = new List<string>()
            };
        }

        private List<CharacterSaveData> CreateStartingCharacters()
        {
            var characters = new List<CharacterSaveData>(
                startingCharacterIds.Count);
            for (int index = 0; index < startingCharacterIds.Count; index++)
            {
                characters.Add(new CharacterSaveData
                {
                    CharacterId = startingCharacterIds[index],
                    Level = SaveRules.CharacterMinLevel,
                    Awakening = SaveRules.CharacterMinAwakening,
                    IsFavorite = false,
                    IsNew = false
                });
            }

            return characters;
        }

        private static PartySaveData CreateDefaultParty(
            IReadOnlyList<string> startingCharacterIds)
        {
            var presets = new List<PartyPresetSaveData>(
                SaveRules.PartyPresetCount);
            for (int presetIndex = 0;
                presetIndex < SaveRules.PartyPresetCount;
                presetIndex++)
            {
                presets.Add(CreatePreset(
                    presetIndex,
                    presetIndex == 0
                        ? startingCharacterIds
                        : Array.Empty<string>()));
            }

            return new PartySaveData
            {
                ActivePresetIndex = 0,
                Presets = presets,
                LastBossId = SaveRules.EmptyId,
                LastDifficultyId = SaveRules.EmptyId
            };
        }

        private static PartyPresetSaveData CreatePreset(
            int presetIndex,
            IReadOnlyList<string> characterIds)
        {
            var characterSlotIds = new List<string>(
                SaveRules.PartySlotCount);
            for (int slotIndex = 0;
                slotIndex < SaveRules.PartySlotCount;
                slotIndex++)
            {
                characterSlotIds.Add(slotIndex < characterIds.Count
                    ? characterIds[slotIndex]
                    : SaveRules.EmptyId);
            }

            return new PartyPresetSaveData
            {
                PresetId = SaveRules.GetDefaultPartyPresetId(presetIndex),
                CharacterSlotIds = characterSlotIds
            };
        }
    }
}

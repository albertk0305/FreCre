using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Acquisition;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Gacha;
using ValorChronicle.Party;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneAcquisitionIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string StandardCharacterGachaId =
            "gacha_character_standard";

        private static readonly string[] ExistingCharacterIds =
        {
            "character_marea_bluefang",
            "character_serena_coralspear",
            "character_shimotsuki_miyuki",
            "character_fiora_amberwing",
            "character_brina_ashwood",
            "character_naira_windhorn",
            "character_sakuragi_koharu",
            "character_baek_hyeon"
        };

        private DefinitionDatabase database;

        [SetUp]
        public void SetUp()
        {
            database = AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                DatabasePath);
            Assert.That(database, Is.Not.Null, DatabasePath);
            database.Initialize();
        }

        [Test]
        public void AcquisitionAssetsPreserveExistingConfiguration()
        {
            Assert.That(database.TryGetGacha(
                    StandardCharacterGachaId,
                    out GachaDefinition gacha),
                Is.True);
            ExchangeCatalogDefinition exchange = database.ExchangeCatalog;

            Assert.That(gacha.SinglePullCost, Is.EqualTo(100L));
            Assert.That(gacha.TenPullCost, Is.EqualTo(1000L));
            Assert.That(gacha.PityPullCount, Is.EqualTo(50));
            Assert.That(gacha.RewardEntries.Sum(entry => entry.Weight),
                Is.EqualTo(GachaDefinition.RequiredTotalWeight));
            Assert.That(gacha.RewardEntries.Single(entry =>
                    entry.RewardKind
                        == GachaRewardKind.RandomCharacter).Weight,
                Is.EqualTo(20));
            Assert.That(gacha.CharacterPoolIds.Count(id => id
                    == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(gacha.CharacterPoolIds.Take(
                    ExistingCharacterIds.Length),
                Is.EqualTo(ExistingCharacterIds));
            Assert.That(gacha.CharacterPoolIds.Last(),
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));

            Assert.That(exchange, Is.Not.Null);
            Assert.That(exchange.CharacterExchangeCost, Is.EqualTo(60L));
            Assert.That(exchange.CharacterIds.Count(id => id
                    == BeatriceBloodstoneRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(exchange.CharacterIds.Take(
                    ExistingCharacterIds.Length),
                Is.EqualTo(ExistingCharacterIds));
            Assert.That(exchange.CharacterIds.Last(),
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
        }

        [Test]
        public void DeterministicGachaSelectsBeatriceAndUsesCommonGrantRules()
        {
            var calculator = new GachaCalculator(
                database,
                new QueueRandomSource(
                    0,
                    ExistingCharacterIds.Length));
            var state = new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 12,
                TotalPullCount = 40
            };

            GachaCalculationResult calculation = calculator.GenerateSingle(
                StandardCharacterGachaId,
                state);
            Assert.That(calculation.Pulls.Count, Is.EqualTo(1));
            GachaPullResult pull = calculation.Pulls[0];
            Assert.That(pull, Is.Not.Null);

            Assert.That(pull.RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(pull.RewardId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(pull.PityCountBefore, Is.EqualTo(12));
            Assert.That(pull.PityCountAfter, Is.Zero);
            Assert.That(calculation.ResultingState.PityCount, Is.Zero);

            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            Assert.That(profile.Characters.Count, Is.Zero);
            CharacterSaveData existingCharacter = ExistingCharacter();
            profile.Characters.Add(existingCharacter);
            var updater = new RewardGrantProfileUpdater(
                database,
                new FixedRelicInstanceIdProvider());
            RewardGrantBatchResult first = updater.Apply(
                profile,
                calculation);

            Assert.That(first.Grants.Count, Is.EqualTo(1));
            RewardGrantResult firstGrant = first.Grants[0];
            Assert.That(firstGrant.OriginalRewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(firstGrant.OriginalRewardId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(firstGrant.ContentId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(firstGrant.GrantKind,
                Is.EqualTo(RewardGrantKind.NewCharacter));
            Assert.That(Beatrice(profile).Awakening, Is.Zero);
            AssertExistingCharacterIsUnchanged(profile);

            Beatrice(profile).Awakening = SaveRules.CharacterMaxAwakening;
            profile.Currencies.HeroTokens = 7L;
            GachaCalculationResult duplicate = new GachaCalculator(
                database,
                new QueueRandomSource(
                    0,
                    ExistingCharacterIds.Length)).GenerateSingle(
                        StandardCharacterGachaId,
                        new GachaStateSaveData
                        {
                            GachaId = StandardCharacterGachaId
                        });
            Assert.That(duplicate.Pulls.Count, Is.EqualTo(1));
            Assert.That(duplicate.Pulls[0].RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(duplicate.Pulls[0].RewardId,
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            RewardGrantBatchResult duplicateBatch = updater.Apply(
                profile,
                duplicate);
            Assert.That(duplicateBatch.Grants.Count, Is.EqualTo(1));
            RewardGrantResult duplicateGrant = duplicateBatch.Grants[0];

            Assert.That(duplicateGrant.GrantKind,
                Is.EqualTo(RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens));
            Assert.That(duplicateGrant.CurrencyGrant.Amount,
                Is.EqualTo(60L));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(67L));
            Assert.That(Beatrice(profile).Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            AssertExistingCharacterIsUnchanged(profile);
        }

        [TestCase(0, 1)]
        [TestCase(5, 6)]
        public void OwnedBeatriceAcquisitionIncreasesAwakeningOnce(
            int before,
            int after)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            Assert.That(profile.Characters.Count, Is.Zero);
            CharacterSaveData existingCharacter = ExistingCharacter();
            profile.Characters.Add(existingCharacter);
            profile.Characters.Add(Character(before));
            var updater = new CharacterAcquisitionProfileUpdater(database);

            CharacterAcquisitionResult result = updater.Apply(
                profile,
                BeatriceBloodstoneRules.CharacterId);

            Assert.That(result.Kind,
                Is.EqualTo(CharacterAcquisitionKind.AwakeningIncreased));
            Assert.That(result.PreviousAwakening, Is.EqualTo(before));
            Assert.That(result.NewAwakening, Is.EqualTo(after));
            Assert.That(Beatrice(profile).Awakening, Is.EqualTo(after));
            Assert.That(profile.Characters.Count, Is.EqualTo(2));
            AssertExistingCharacterIsUnchanged(profile);
        }

        [Test]
        public void ExchangeCatalogUsesCurrencyAndAwakeningPolicies()
        {
            ProfileSaveData insufficient = Profile(heroTokens: 59L);
            CharacterExchangeListItem insufficientItem = Beatrice(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    insufficient));
            Assert.That(insufficientItem.Cost, Is.EqualTo(60L));
            Assert.That(insufficientItem.CanExchange, Is.False);
            Assert.That(insufficientItem.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.InsufficientCurrency),
                Is.True);

            ProfileSaveData available = Profile(heroTokens: 60L);
            available.Characters.Add(Character(5));
            CharacterExchangeListItem availableItem = Beatrice(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    available));
            Assert.That(availableItem.CanExchange, Is.True);
            Assert.That(availableItem.Progress.ResultingAwakening,
                Is.EqualTo(6));

            ProfileSaveData maximum = Profile(heroTokens: 60L);
            maximum.Characters.Add(Character(6));
            CharacterExchangeListItem maximumItem = Beatrice(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    maximum));
            Assert.That(maximumItem.CanExchange, Is.False);
            Assert.That(maximumItem.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening),
                Is.True);
        }

        [Test]
        public void ExchangeAcquisitionPersistsAndAllowsPartyPlacement()
        {
            ProfileSaveData profile = Profile(heroTokens: 60L);
            profile.GachaStates = new List<GachaStateSaveData>
            {
                new GachaStateSaveData
                {
                    GachaId = StandardCharacterGachaId,
                    PityCount = 17,
                    TotalPullCount = 33
                }
            };
            SaveContext context = Context(profile);
            var party = new PartyPersistenceService(
                context.SaveService,
                new PartyPresetEditor());

            Assert.That(party.ConfirmEdit(
                    presetIndex: 0,
                    targetSlotIndex: 4,
                    previewCharacterId:
                        BeatriceBloodstoneRules.CharacterId).IsSuccess,
                Is.False);

            ExchangeTransactionOperationResult exchange =
                context.Exchange.Execute(new ExchangeTransactionRequest(
                    ExchangeContentKind.Character,
                    BeatriceBloodstoneRules.CharacterId));

            Assert.That(exchange.IsSuccess, Is.True, exchange.Message);
            Assert.That(exchange.WasPersisted, Is.True);
            Assert.That(exchange.TransactionResult.CurrencyBefore,
                Is.EqualTo(60L));
            Assert.That(exchange.TransactionResult.CurrencyAfter, Is.Zero);
            Assert.That(exchange.TransactionResult.IsNewCharacter, Is.True);
            ProfileSaveData acquired = Current(context);
            Assert.That(Beatrice(acquired).Awakening, Is.Zero);
            Assert.That(acquired.GachaStates.Single().PityCount,
                Is.EqualTo(17));
            Assert.That(acquired.GachaStates.Single().TotalPullCount,
                Is.EqualTo(33));

            PartyPersistenceOperationResult placed = party.ConfirmEdit(
                presetIndex: 0,
                targetSlotIndex: 4,
                previewCharacterId:
                    BeatriceBloodstoneRules.CharacterId);
            Assert.That(placed.IsSuccess, Is.True);
            Assert.That(placed.WasChanged, Is.True);

            ProfileSaveData reloaded = Reload(context);
            Assert.That(reloaded.Party.Presets[0].CharacterSlotIds[4],
                Is.EqualTo(BeatriceBloodstoneRules.CharacterId));
            Assert.That(Beatrice(reloaded).Awakening, Is.Zero);
            BattlePartyResolutionResult battle =
                new BattlePartyResolver(database).Resolve(reloaded);
            Assert.That(battle.IsSuccess, Is.True, battle.ErrorMessage);
            Assert.That(battle.Members.Single(member => member.CharacterId
                    == BeatriceBloodstoneRules.CharacterId)
                .PartySlotIndex,
                Is.EqualTo(4));
        }

        [Test]
        public void ExchangeAwakensToSixThenRejectsFurtherExchange()
        {
            ProfileSaveData profile = Profile(heroTokens: 120L);
            profile.Characters.Add(Character(5));
            profile.GachaStates = new List<GachaStateSaveData>
            {
                new GachaStateSaveData
                {
                    GachaId = StandardCharacterGachaId,
                    PityCount = 9,
                    TotalPullCount = 12
                }
            };
            SaveContext context = Context(profile);
            var request = new ExchangeTransactionRequest(
                ExchangeContentKind.Character,
                BeatriceBloodstoneRules.CharacterId);

            ExchangeTransactionOperationResult first =
                context.Exchange.Execute(request);
            ExchangeTransactionOperationResult second =
                context.Exchange.Execute(request);
            ProfileSaveData current = Current(context);

            Assert.That(first.IsSuccess, Is.True, first.Message);
            Assert.That(Beatrice(current).Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(current.Currencies.HeroTokens, Is.EqualTo(60L));
            Assert.That(current.GachaStates.Single().PityCount,
                Is.EqualTo(9));
            Assert.That(second.Status,
                Is.EqualTo(ExchangeTransactionStatus.MaximumAwakening));
            Assert.That(second.TransactionResult, Is.Null);
        }

        private SaveContext Context(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100L));
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return new SaveContext(
                repository,
                saveService,
                new ExchangeTransactionService(
                    saveService,
                    database,
                    new FixedRelicInstanceIdProvider()));
        }

        private static ProfileSaveData Profile(long heroTokens)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.HeroTokens = heroTokens;
            return profile;
        }

        private static CharacterSaveData Character(int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = BeatriceBloodstoneRules.CharacterId,
                Level = SaveRules.CharacterMinLevel,
                Awakening = awakening
            };
        }

        private static CharacterSaveData ExistingCharacter()
        {
            return new CharacterSaveData
            {
                CharacterId = ExistingCharacterIds[0],
                Level = 7,
                Awakening = 2,
                IsFavorite = true
            };
        }

        private static void AssertExistingCharacterIsUnchanged(
            ProfileSaveData profile)
        {
            CharacterSaveData actual = profile.Characters.Single(character =>
                character.CharacterId == ExistingCharacterIds[0]);
            Assert.That(actual.Level, Is.EqualTo(7));
            Assert.That(actual.Awakening, Is.EqualTo(2));
            Assert.That(actual.IsFavorite, Is.True);
        }

        private static CharacterSaveData Beatrice(ProfileSaveData profile)
        {
            return profile.Characters.Single(character =>
                character.CharacterId
                    == BeatriceBloodstoneRules.CharacterId);
        }

        private static CharacterExchangeListItem Beatrice(
            ExchangeCatalogData catalog)
        {
            return catalog.Characters.Single(item =>
                item.CharacterId
                    == BeatriceBloodstoneRules.CharacterId);
        }

        private static ProfileSaveData Current(SaveContext context)
        {
            return context.SaveService.GetCurrentProfileSnapshot();
        }

        private static ProfileSaveData Reload(SaveContext context)
        {
            SaveService service = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(200L));
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service.GetCurrentProfileSnapshot();
        }

        private sealed class QueueRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public QueueRandomSource(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No deterministic random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new InvalidOperationException(
                    "Gacha must use integer randomness.");
            }
        }

        private sealed class FixedRelicInstanceIdProvider
            : IRelicInstanceIdProvider
        {
            public string CreateRelicInstanceId()
            {
                return "beatrice_test_relic_instance";
            }
        }

        private sealed class SaveContext
        {
            public SaveContext(
                FakeSaveRepository repository,
                SaveService saveService,
                ExchangeTransactionService exchange)
            {
                Repository = repository;
                SaveService = saveService;
                Exchange = exchange;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public ExchangeTransactionService Exchange { get; }
        }
    }
}

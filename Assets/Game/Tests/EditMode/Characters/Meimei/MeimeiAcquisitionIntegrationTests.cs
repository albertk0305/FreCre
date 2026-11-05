using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Acquisition;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Gacha;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Party;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiAcquisitionIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";
        private const string StandardCharacterGachaId =
            "gacha_character_standard";
        private const string StandardRelicGachaId =
            "gacha_relic_standard";

        private static readonly string[] PreviousCharacterIds =
        {
            "character_marea_bluefang",
            "character_serena_coralspear",
            "character_shimotsuki_miyuki",
            "character_fiora_amberwing",
            "character_brina_ashwood",
            "character_naira_windhorn",
            "character_sakuragi_koharu",
            "character_baek_hyeon",
            "character_beatrice_bloodstone",
            "character_lilith",
            "character_karasuma_shizuku",
            "character_luciel",
            "character_livia",
            "character_lancelot",
            "character_kayla_stride",
            "character_anubis",
            "character_chloe_valentine"
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
        public void CanonicalAcquisitionAssetsAppendMeimeiExactlyOnce()
        {
            Assert.That(database.TryGetGacha(
                    StandardCharacterGachaId,
                    out GachaDefinition gacha),
                Is.True);
            Assert.That(database.TryGetGacha(
                    StandardRelicGachaId,
                    out GachaDefinition relicGacha),
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
            Assert.That(gacha.CharacterPoolIds.Take(
                    PreviousCharacterIds.Length),
                Is.EqualTo(PreviousCharacterIds));
            Assert.That(gacha.CharacterPoolIds.Count(id => id
                    == MeimeiRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(gacha.CharacterPoolIds, Is.Unique);

            Assert.That(relicGacha.CharacterPoolIds.Count(id => id
                    == MeimeiRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(relicGacha.CharacterPoolIds, Is.Unique);

            Assert.That(exchange, Is.Not.Null);
            Assert.That(exchange.CharacterExchangeCost, Is.EqualTo(60L));
            Assert.That(exchange.CharacterIds.Take(
                    PreviousCharacterIds.Length),
                Is.EqualTo(PreviousCharacterIds));
            Assert.That(exchange.CharacterIds.Count(id => id
                    == MeimeiRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(exchange.CharacterIds, Is.Unique);

            CharacterPresentationCatalog presentations =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationCatalog>(
                    PresentationCatalogPath);
            Assert.That(presentations, Is.Not.Null);
            presentations.Initialize();
            Assert.That(presentations.TryGet(
                MeimeiRules.CharacterId,
                out CharacterPresentationDefinition presentation),
                Is.True);
            Assert.That(presentation.FaceSprite, Is.Not.Null);
            Assert.That(presentation.PreviewSprite, Is.Not.Null);
            Assert.That(CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .Registrations.Count(registration =>
                        registration.CharacterId
                            == MeimeiRules.CharacterId),
                Is.EqualTo(1));
        }

        [Test]
        public void DeterministicSingleAndTenPullsSelectMeimei()
        {
            database.TryGetGacha(
                StandardCharacterGachaId,
                out GachaDefinition gacha);
            int meimeiIndex = gacha.CharacterPoolIds
                .ToList()
                .IndexOf(MeimeiRules.CharacterId);
            var state = new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 12,
                TotalPullCount = 40
            };
            GachaCalculationResult single = new GachaCalculator(
                    database,
                    new QueueRandomSource(0, meimeiIndex))
                .GenerateSingle(StandardCharacterGachaId, state);

            Assert.That(single.Pulls, Has.Count.EqualTo(1));
            Assert.That(single.Pulls[0].RewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(single.Pulls[0].RewardId,
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(single.Pulls[0].PityCountBefore, Is.EqualTo(12));
            Assert.That(single.Pulls[0].PityCountAfter, Is.Zero);

            int[] tenValues = Enumerable.Range(0, 10)
                .SelectMany(_ => new[] { 0, meimeiIndex })
                .ToArray();
            GachaCalculationResult ten = new GachaCalculator(
                    database,
                    new QueueRandomSource(tenValues))
                .GenerateTen(
                    StandardCharacterGachaId,
                    new GachaStateSaveData
                    {
                        GachaId = StandardCharacterGachaId
                    });

            Assert.That(ten.Pulls, Has.Count.EqualTo(10));
            Assert.That(ten.Pulls.All(pull =>
                    pull.RewardKind == GachaRewardKind.RandomCharacter
                    && pull.RewardId == MeimeiRules.CharacterId),
                Is.True);
            Assert.That(ten.ResultingState.PityCount, Is.Zero);
            Assert.That(ten.ResultingState.TotalPullCount, Is.EqualTo(10));
        }

        [Test]
        public void CommonAcquisitionRulesHandleEveryMeimeiOwnershipState()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            var updater = new CharacterAcquisitionProfileUpdater(database);

            CharacterAcquisitionResult acquired = updater.Apply(
                profile,
                MeimeiRules.CharacterId);
            Assert.That(acquired.Kind,
                Is.EqualTo(CharacterAcquisitionKind.NewCharacter));
            Assert.That(Meimei(profile).Awakening, Is.Zero);
            Assert.That(profile.Characters.Count(character =>
                    character.CharacterId == MeimeiRules.CharacterId),
                Is.EqualTo(1));

            CharacterAcquisitionResult awakened = updater.Apply(
                profile,
                MeimeiRules.CharacterId);
            Assert.That(awakened.Kind,
                Is.EqualTo(CharacterAcquisitionKind.AwakeningIncreased));
            Assert.That(awakened.PreviousAwakening, Is.Zero);
            Assert.That(awakened.NewAwakening, Is.EqualTo(1));

            Meimei(profile).Awakening = 5;
            CharacterAcquisitionResult maximum = updater.Apply(
                profile,
                MeimeiRules.CharacterId);
            Assert.That(maximum.Kind,
                Is.EqualTo(CharacterAcquisitionKind.AwakeningIncreased));
            Assert.That(Meimei(profile).Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));

            profile.Currencies.HeroTokens = 7L;
            CharacterAcquisitionResult duplicate = updater.Apply(
                profile,
                MeimeiRules.CharacterId);
            Assert.That(duplicate.Kind,
                Is.EqualTo(CharacterAcquisitionKind
                    .MaxAwakeningConvertedToHeroTokens));
            Assert.That(duplicate.CurrencyGrant.Amount, Is.EqualTo(60L));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(67L));
            Assert.That(Meimei(profile).Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(profile.Characters.Count(character =>
                    character.CharacterId == MeimeiRules.CharacterId),
                Is.EqualTo(1));
        }

        [Test]
        public void GuaranteedGachaPersistsMeimeiAndResetsPity()
        {
            GachaDefinition gacha = GetCharacterGacha();
            int meimeiIndex = gacha.CharacterPoolIds
                .ToList()
                .IndexOf(MeimeiRules.CharacterId);
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.GachaCurrency = 100L;
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 49,
                TotalPullCount = 49
            });
            GachaContext context = GachaContextFor(
                profile,
                new QueueRandomSource(meimeiIndex));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(StandardCharacterGachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.WasPersisted, Is.True);
            Assert.That(operation.TransactionResult.GoldSpent,
                Is.EqualTo(100L));
            Assert.That(operation.TransactionResult.Pulls[0]
                .WasGuaranteedByPity, Is.True);
            Assert.That(operation.TransactionResult.Pulls[0].ContentId,
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(operation.TransactionResult.Pulls[0].GrantKind,
                Is.EqualTo(RewardGrantKind.NewCharacter));
            ProfileSaveData current = Current(context.SaveService);
            Assert.That(current.Currencies.GachaCurrency, Is.Zero);
            Assert.That(Meimei(current).Awakening, Is.Zero);
            Assert.That(current.GachaStates.Single().PityCount, Is.Zero);
            Assert.That(current.GachaStates.Single().TotalPullCount,
                Is.EqualTo(50));

            ProfileSaveData reloaded = Reload(
                context.Repository,
                unixTime: 200L);
            Assert.That(Meimei(reloaded).Awakening, Is.Zero);
            Assert.That(reloaded.GachaStates.Single().PityCount, Is.Zero);
        }

        [Test]
        public void MaximumAwakeningGachaDuplicatePaysTokensAndResetsPity()
        {
            int meimeiIndex = GetCharacterGacha().CharacterPoolIds
                .ToList()
                .IndexOf(MeimeiRules.CharacterId);
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.GachaCurrency = 100L;
            profile.Currencies.HeroTokens = 5L;
            profile.Characters.Add(Character(6));
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 49,
                TotalPullCount = 70
            });
            GachaContext context = GachaContextFor(
                profile,
                new QueueRandomSource(meimeiIndex));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(StandardCharacterGachaId);
            ProfileSaveData current = Current(context.SaveService);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.Pulls[0].GrantKind,
                Is.EqualTo(RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens));
            Assert.That(operation.TransactionResult.Pulls[0]
                .GrantedCurrencyAmount, Is.EqualTo(60L));
            Assert.That(current.Currencies.HeroTokens, Is.EqualTo(65L));
            Assert.That(Meimei(current).Awakening, Is.EqualTo(6));
            Assert.That(current.Characters.Count(character =>
                    character.CharacterId == MeimeiRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(current.GachaStates.Single().PityCount, Is.Zero);
        }

        [Test]
        public void ExchangeCatalogUsesSharedCurrencyAndAwakeningPolicies()
        {
            CharacterExchangeListItem insufficient = Meimei(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    Profile(heroTokens: 59L)));
            Assert.That(insufficient.Cost, Is.EqualTo(60L));
            Assert.That(insufficient.CanExchange, Is.False);
            Assert.That(insufficient.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.InsufficientCurrency),
                Is.True);

            ProfileSaveData availableProfile = Profile(heroTokens: 60L);
            availableProfile.Characters.Add(Character(5));
            CharacterExchangeListItem available = Meimei(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    availableProfile));
            Assert.That(available.CanExchange, Is.True);
            Assert.That(available.Progress.ResultingAwakening,
                Is.EqualTo(6));

            ProfileSaveData maximumProfile = Profile(heroTokens: 60L);
            maximumProfile.Characters.Add(Character(6));
            CharacterExchangeListItem maximum = Meimei(
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    maximumProfile));
            Assert.That(maximum.CanExchange, Is.False);
            Assert.That(maximum.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening),
                Is.True);
        }

        [Test]
        public void ExchangePersistsMeimeiAndLeavesGachaPityUnchanged()
        {
            ProfileSaveData profile = Profile(heroTokens: 60L);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 17,
                TotalPullCount = 33
            });
            ExchangeContext context = ExchangeContextFor(profile);
            var party = new PartyPersistenceService(
                context.SaveService,
                new PartyPresetEditor());

            ExchangeTransactionOperationResult operation =
                context.Service.Execute(new ExchangeTransactionRequest(
                    ExchangeContentKind.Character,
                    MeimeiRules.CharacterId));

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.WasPersisted, Is.True);
            Assert.That(operation.TransactionResult.CurrencyBefore,
                Is.EqualTo(60L));
            Assert.That(operation.TransactionResult.CurrencyAfter, Is.Zero);
            Assert.That(operation.TransactionResult.IsNewCharacter, Is.True);
            ProfileSaveData current = Current(context.SaveService);
            Assert.That(Meimei(current).Awakening, Is.Zero);
            Assert.That(current.GachaStates.Single().PityCount,
                Is.EqualTo(17));
            Assert.That(current.GachaStates.Single().TotalPullCount,
                Is.EqualTo(33));

            PartyPersistenceOperationResult placed = party.ConfirmEdit(
                presetIndex: 0,
                targetSlotIndex: 4,
                previewCharacterId: MeimeiRules.CharacterId);
            Assert.That(placed.IsSuccess, Is.True);
            ProfileSaveData reloaded = Reload(
                context.Repository,
                unixTime: 200L);
            Assert.That(reloaded.Party.Presets[0].CharacterSlotIds[4],
                Is.EqualTo(MeimeiRules.CharacterId));
            Assert.That(Meimei(reloaded).Awakening, Is.Zero);
            BattlePartyResolutionResult battle =
                new BattlePartyResolver(database).Resolve(reloaded);
            Assert.That(battle.IsSuccess, Is.True, battle.ErrorMessage);
            Assert.That(battle.Members.Single(member =>
                    member.CharacterId == MeimeiRules.CharacterId)
                .PartySlotIndex, Is.EqualTo(4));
        }

        [Test]
        public void FailedExchangesPreserveCurrencyOwnershipAndPity()
        {
            ProfileSaveData insufficientProfile = Profile(heroTokens: 59L);
            insufficientProfile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 8,
                TotalPullCount = 20
            });
            ExchangeContext insufficient = ExchangeContextFor(
                insufficientProfile);
            string insufficientBefore = Serialize(
                Current(insufficient.SaveService));
            ExchangeTransactionOperationResult insufficientResult =
                insufficient.Service.Execute(
                    new ExchangeTransactionRequest(
                        ExchangeContentKind.Character,
                        MeimeiRules.CharacterId));
            Assert.That(insufficientResult.Status,
                Is.EqualTo(ExchangeTransactionStatus.InsufficientCurrency));
            Assert.That(Serialize(Current(insufficient.SaveService)),
                Is.EqualTo(insufficientBefore));

            ProfileSaveData maximumProfile = Profile(heroTokens: 120L);
            maximumProfile.Characters.Add(Character(6));
            maximumProfile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = StandardCharacterGachaId,
                PityCount = 9,
                TotalPullCount = 30
            });
            ExchangeContext maximum = ExchangeContextFor(maximumProfile);
            string maximumBefore = Serialize(Current(maximum.SaveService));
            ExchangeTransactionOperationResult maximumResult =
                maximum.Service.Execute(new ExchangeTransactionRequest(
                    ExchangeContentKind.Character,
                    MeimeiRules.CharacterId));
            Assert.That(maximumResult.Status,
                Is.EqualTo(ExchangeTransactionStatus.MaximumAwakening));
            Assert.That(Serialize(Current(maximum.SaveService)),
                Is.EqualTo(maximumBefore));
        }

        [Test]
        public void ExchangeAwakensFiveToSixThenRejectsAnotherRequest()
        {
            ProfileSaveData profile = Profile(heroTokens: 120L);
            profile.Characters.Add(Character(5));
            ExchangeContext context = ExchangeContextFor(profile);
            var request = new ExchangeTransactionRequest(
                ExchangeContentKind.Character,
                MeimeiRules.CharacterId);

            ExchangeTransactionOperationResult first =
                context.Service.Execute(request);
            ExchangeTransactionOperationResult second =
                context.Service.Execute(request);
            ProfileSaveData current = Current(context.SaveService);

            Assert.That(first.IsSuccess, Is.True, first.Message);
            Assert.That(Meimei(current).Awakening,
                Is.EqualTo(SaveRules.CharacterMaxAwakening));
            Assert.That(current.Currencies.HeroTokens, Is.EqualTo(60L));
            Assert.That(second.Status,
                Is.EqualTo(ExchangeTransactionStatus.MaximumAwakening));
            Assert.That(second.TransactionResult, Is.Null);
            Assert.That(current.Characters.Count(character =>
                    character.CharacterId == MeimeiRules.CharacterId),
                Is.EqualTo(1));
        }

        private GachaDefinition GetCharacterGacha()
        {
            Assert.That(database.TryGetGacha(
                    StandardCharacterGachaId,
                    out GachaDefinition gacha),
                Is.True);
            return gacha;
        }

        private GachaContext GachaContextFor(
            ProfileSaveData profile,
            IRandomSource random)
        {
            SaveContext context = SaveContextFor(profile);
            return new GachaContext(
                context.Repository,
                context.SaveService,
                new GachaTransactionService(
                    context.SaveService,
                    database,
                    random,
                    new FixedRelicInstanceIdProvider()));
        }

        private ExchangeContext ExchangeContextFor(ProfileSaveData profile)
        {
            SaveContext context = SaveContextFor(profile);
            return new ExchangeContext(
                context.Repository,
                context.SaveService,
                new ExchangeTransactionService(
                    context.SaveService,
                    database,
                    new FixedRelicInstanceIdProvider()));
        }

        private static SaveContext SaveContextFor(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = Serialize(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100L));
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return new SaveContext(repository, saveService);
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
                CharacterId = MeimeiRules.CharacterId,
                Level = SaveRules.CharacterMinLevel,
                Awakening = awakening
            };
        }

        private static CharacterSaveData Meimei(ProfileSaveData profile)
        {
            return profile.Characters.Single(character =>
                character.CharacterId == MeimeiRules.CharacterId);
        }

        private static CharacterExchangeListItem Meimei(
            ExchangeCatalogData catalog)
        {
            return catalog.Characters.Single(item =>
                item.CharacterId == MeimeiRules.CharacterId);
        }

        private static ProfileSaveData Current(SaveService saveService)
        {
            return saveService.GetCurrentProfileSnapshot();
        }

        private static ProfileSaveData Reload(
            FakeSaveRepository repository,
            long unixTime)
        {
            SaveService service = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(unixTime));
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service.GetCurrentProfileSnapshot();
        }

        private static string Serialize(ProfileSaveData profile)
        {
            return new NewtonsoftJsonSaveSerializer().Serialize(profile);
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
                return "meimei_acquisition_test_relic";
            }
        }

        private class SaveContext
        {
            public SaveContext(
                FakeSaveRepository repository,
                SaveService saveService)
            {
                Repository = repository;
                SaveService = saveService;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
        }

        private sealed class GachaContext : SaveContext
        {
            public GachaContext(
                FakeSaveRepository repository,
                SaveService saveService,
                GachaTransactionService service)
                : base(repository, saveService)
            {
                Service = service;
            }

            public GachaTransactionService Service { get; }
        }

        private sealed class ExchangeContext : SaveContext
        {
            public ExchangeContext(
                FakeSaveRepository repository,
                SaveService saveService,
                ExchangeTransactionService service)
                : base(repository, saveService)
            {
                Service = service;
            }

            public ExchangeTransactionService Service { get; }
        }
    }
}

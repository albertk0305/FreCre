using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Battle;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryRegistrationIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/sherry.asset";
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Sherry/sherry_combat_config.asset";
        private const string PresentationPath =
            "Assets/Data/CharacterPresentations/sherry_presentation.asset";
        private const string PresentationCatalogPath =
            "Assets/Data/Party/CharacterPresentationCatalog.asset";

        [Test]
        public void DefaultRuntimeCatalogRegistersSherryExactlyOnce()
        {
            CharacterRuntimeRegistrationCatalog catalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            CharacterRuntimeRegistration registration = catalog.Registrations
                .Single(item => item.CharacterId == SherryRules.CharacterId);

            Assert.That(catalog.Registrations.Count(item =>
                    item.CharacterId == SherryRules.CharacterId),
                Is.EqualTo(1));
            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(SherryCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<SherryBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<SherryCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<SherryLookupDescriptionProvider>());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public void RegistrarUsesSlotOwnerAndOneSharedBattleState(int slot)
        {
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
            var member = new BattlePartyMemberInput(
                SherryRules.CharacterId,
                slot,
                level: 100,
                awakening: 6,
                definition);
            var sherry = new CharacterBattleState(
                SherryRules.CharacterId,
                slot,
                ElementType.Fire,
                4095,
                861d);
            var party = new PartyBattleState(
                new CharacterBattleState[] { sherry });
            var boss = new BossBattleState(
                "boss_sherry_registration_test",
                ElementType.Fire,
                1000000,
                100d);
            var actionIds = new CombatActionIdSequence();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            CharacterCombatProviderRegistrationResult result =
                new SherryCombatProviderRegistrar().Register(
                    member,
                    sherry,
                    boss,
                    actionIds,
                    matches,
                    actives);

            Assert.That(matches.TryResolve(
                SherryRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(actives.TryResolve(
                SherryRules.CharacterId,
                SherryRules.ActiveAbilityId,
                out IActiveAbilityActionProvider active), Is.True);
            Assert.That(match, Is.TypeOf<SherryMatchActionProvider>());
            Assert.That(active, Is.TypeOf<SherryActiveActionProvider>());
            Assert.That(result.ActiveAbilityId,
                Is.EqualTo(SherryRules.ActiveAbilityId));
            Assert.That(result.ActiveCooldownTurns, Is.EqualTo(1));
            Assert.That(result.TriggerRules.Single(),
                Is.TypeOf<SherryMatchCompletionRule>());
            Assert.That(result.ResourceRequirements, Is.Empty);
            ICharacterCombatStatusSource status =
                result.CombatStatusSources.Single();
            Assert.That(status, Is.TypeOf<SherryBattleState>());
            Assert.That(status.CharacterId,
                Is.EqualTo(SherryRules.CharacterId));
            Assert.That(status.PartySlotIndex, Is.EqualTo(slot));
            Assert.That(status.CurrentCount, Is.Zero);
            Assert.That(status.IsVisible, Is.True);

            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(result.TriggerRules));
            executor.Execute(new CombatActionQueue(
                ((SherryMatchActionProvider)match).CreateActions(
                    sherry,
                    party,
                    boss,
                    BoardMatchTier.Three,
                    finalComboCount: 1,
                    actionIds)));
            Assert.That(status.CurrentCount, Is.EqualTo(1));

            ((IActiveAbilitySuccessCommitter)active)
                .CommitActiveSuccess(context: null);
            Assert.That(status.CurrentCount, Is.Zero);
        }

        [Test]
        public void EachRegistrationCreatesIndependentBattleState()
        {
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
            ICharacterCombatStatusSource first = Register(definition, 0);
            ICharacterCombatStatusSource second = Register(definition, 1);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.CurrentCount, Is.Zero);
            Assert.That(second.CurrentCount, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        public void DefaultBattleCompositionConnectsSlotAndResetsOnReentry(
            int slot)
        {
            CharacterDefinition definition = Load<CharacterDefinition>(
                CharacterPath);
            var member = new BattlePartyMemberInput(
                SherryRules.CharacterId,
                slot,
                level: 1,
                awakening: 0,
                definition);

            using (BattleSceneCombatComposition first = Compose(member))
            {
                var state = (SherryBattleState)first.CombatStatusSources
                    .Single();
                Assert.That(state.PartySlotIndex, Is.EqualTo(slot));
                Assert.That(first.MatchProviders.TryResolve(
                    SherryRules.CharacterId,
                    out IMatchEventActionProvider provider), Is.True);
                first.Executor.Execute(new CombatActionQueue(
                    ((SherryMatchActionProvider)provider).CreateActions(
                        first.Party.Characters.Single(),
                        first.Party,
                        first.Boss,
                        BoardMatchTier.Four,
                        1,
                        first.ActionIds)));
                Assert.That(state.StylishRank, Is.EqualTo(1));
                Assert.That(state.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.Four));
            }

            using (BattleSceneCombatComposition second = Compose(member))
            {
                var state = (SherryBattleState)second.CombatStatusSources
                    .Single();
                Assert.That(state.StylishRank, Is.Zero);
                Assert.That(state.PreviousMatchTier,
                    Is.EqualTo(SherryMatchTier.None));
            }
        }

        [Test]
        public void ContentAssetsAreRegisteredAndOptionalIconsAreValid()
        {
            SherryCombatConfig config = Load<SherryCombatConfig>(ConfigPath);
            CharacterDefinition character = Load<CharacterDefinition>(
                CharacterPath);
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            database.Initialize();

            Assert.That(config.TryValidate(out string error), Is.True, error);
            Assert.That(character.Id, Is.EqualTo(SherryRules.CharacterId));
            Assert.That(character.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(character.Level1Hp, Is.EqualTo(1000));
            Assert.That(character.Level1Attack, Is.EqualTo(150));
            Assert.That(character.Level100Hp, Is.EqualTo(3900));
            Assert.That(character.Level100Attack, Is.EqualTo(820));
            Assert.That(character.CombatConfig, Is.SameAs(config));
            Assert.That(character.SkillIds, Is.EqualTo(new[]
            {
                SherryRules.Match3SkillId,
                SherryRules.Match4SkillId,
                SherryRules.Match5SkillId,
                SherryRules.PassiveSkillId,
                SherryRules.ActiveAbilityId
            }));
            Assert.That(database.TryGetCharacter(
                SherryRules.CharacterId,
                out _), Is.True);
            foreach (string skillId in character.SkillIds)
            {
                Assert.That(database.TryGetSkill(
                    skillId,
                    out SkillDefinition skill), Is.True, skillId);
                Assert.That(skill.Id, Is.EqualTo(skillId));
                AssertOptionalSpriteAsset(
                    skill.Icon,
                    skillId == SherryRules.PassiveSkillId
                        ? "Assets/Sprites/Effects/Sherry/StylishRank.png"
                        : null);
            }

            Assert.That(database.TryGetEffect(
                SherryRules.OniIntimidationEffectId,
                out EffectDefinition effect), Is.True);
            Assert.That(effect.Id,
                Is.EqualTo(SherryRules.OniIntimidationEffectId));
            AssertOptionalSpriteAsset(
                effect.Icon,
                "Assets/Sprites/Effects/Sherry/OnisIntimidation.png");
        }

        [Test]
        public void PresentationLocalizationGachaAndExchangeIncludeSherry()
        {
            CharacterPresentationDefinition presentation = Load<
                CharacterPresentationDefinition>(PresentationPath);
            Assert.That(presentation.CharacterId,
                Is.EqualTo(SherryRules.CharacterId));
            Assert.That(AssetDatabase.GetAssetPath(presentation.FaceSprite),
                Is.EqualTo("Assets/Sprites/Character/Face/SherryFace.png"));
            Assert.That(AssetDatabase.GetAssetPath(presentation.PreviewSprite),
                Is.EqualTo("Assets/Sprites/Character/Full/SherryFull.png"));

            CharacterPresentationCatalog presentationCatalog = Load<
                CharacterPresentationCatalog>(PresentationCatalogPath);
            presentationCatalog.Initialize();
            Assert.That(presentationCatalog.TryGet(
                SherryRules.CharacterId,
                out CharacterPresentationDefinition catalogEntry), Is.True);
            Assert.That(catalogEntry, Is.SameAs(presentation));

            GachaDefinition characterGacha = Load<GachaDefinition>(
                "Assets/Data/Gachas/gacha_character_standard.asset");
            GachaDefinition relicGacha = Load<GachaDefinition>(
                "Assets/Data/Gachas/gacha_relic_standard.asset");
            Assert.That(characterGacha.CharacterPoolIds,
                Does.Contain(SherryRules.CharacterId));
            Assert.That(relicGacha.CharacterPoolIds,
                Does.Contain(SherryRules.CharacterId));
            Assert.That(characterGacha.PityPullCount, Is.EqualTo(50));
            Assert.That(relicGacha.PityPullCount, Is.EqualTo(20));
            Assert.That(characterGacha.RewardEntries.Single(item =>
                    item.RewardKind == GachaRewardKind.RandomCharacter).Weight,
                Is.EqualTo(20));
            Assert.That(relicGacha.RewardEntries.Single(item =>
                    item.RewardKind == GachaRewardKind.RandomCharacter).Weight,
                Is.EqualTo(5));

            ExchangeCatalogDefinition exchange = Load<
                ExchangeCatalogDefinition>(
                    "Assets/Data/Exchange/ExchangeCatalogDefinition.asset");
            Assert.That(exchange.CharacterIds,
                Does.Contain(SherryRules.CharacterId));
            Assert.That(exchange.CharacterExchangeCost, Is.EqualTo(60));
        }

        [Test]
        public void LookupDescriptionsResolveForBothLocalesAndAwakenings()
        {
            DefinitionDatabase database = Load<DefinitionDatabase>(
                DatabasePath);
            database.Initialize();
            LocalizationCatalog localizationCatalog = Load<
                LocalizationCatalog>(
                    "Assets/Data/Localization/LocalizationCatalog.asset");
            LocalizationService localization =
                localizationCatalog.CreateService();
            CharacterLookupDescriptionResolver resolver =
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateLookupDescriptionResolver(database, localization);

            CharacterLookupDescriptionResult active = resolver.ResolveSkill(
                SherryRules.CharacterId,
                currentAwakening: 6,
                CharacterLookupSkillType.Active);
            Assert.That(active.IsSuccess, Is.True, active.Error);
            Assert.That(active.Text, Does.Contain("2300"));
            Assert.That(active.Text, Does.Contain("Cooldown: 1 turn"));
            for (int stage = 1; stage <= 6; stage++)
            {
                CharacterLookupDescriptionResult awakening =
                    resolver.ResolveAwakening(SherryRules.CharacterId, stage);
                Assert.That(awakening.IsSuccess, Is.True, awakening.Error);
            }

            localization.SetLocale("ko");
            CharacterLookupDescriptionResult korean = resolver.ResolveSkill(
                SherryRules.CharacterId,
                currentAwakening: 0,
                CharacterLookupSkillType.Passive);
            Assert.That(korean.IsSuccess, Is.True, korean.Error);
            Assert.That(korean.Text, Does.Contain("스타일리쉬"));
        }

        private static ICharacterCombatStatusSource Register(
            CharacterDefinition definition,
            int slot)
        {
            var character = new CharacterBattleState(
                SherryRules.CharacterId,
                slot,
                ElementType.Fire,
                1000,
                150d);
            var result = new SherryCombatProviderRegistrar().Register(
                new BattlePartyMemberInput(
                    SherryRules.CharacterId,
                    slot,
                    1,
                    0,
                    definition),
                character,
                new BossBattleState(
                    "boss_sherry_state_test_" + slot,
                    ElementType.Fire,
                    10000,
                    100d),
                new CombatActionIdSequence(),
                new MatchEventActionProviderRegistry(),
                new ActiveAbilityActionProviderRegistry());
            return result.CombatStatusSources.Single();
        }

        private static BattleSceneCombatComposition Compose(
            BattlePartyMemberInput member)
        {
            BossDefinition boss = Load<BossDefinition>(
                "Assets/Data/Bosses/boss_kragmor.asset");
            var config = boss.CombatConfig as KragmorCombatConfig;
            Assert.That(config, Is.Not.Null);
            return new BattleSceneCombatComposition(
                new[] { member },
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                config,
                new BossDifficultyStats(
                    "difficulty_sherry_registration_test",
                    1000000,
                    100d),
                new SeededRandomSource(123),
                BattleResultBalanceDefaults.Create());
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void AssertOptionalSpriteAsset(
            UnityEngine.Sprite sprite,
            string expectedPath = null)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
            string actualPath = AssetDatabase.GetAssetPath(sprite);
            Assert.That(actualPath, Is.Not.Empty);
            if (expectedPath != null)
            {
                Assert.That(actualPath, Is.EqualTo(expectedPath));
            }
        }
    }
}

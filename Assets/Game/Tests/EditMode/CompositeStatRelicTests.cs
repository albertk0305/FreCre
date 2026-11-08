using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Stats;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class CompositeStatRelicTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string CharacterPath =
            "Assets/Data/Characters/marea_bluefang.asset";
        private const string PioneersEmblem =
            "relic_pioneers_emblem";
        private const string BloodiedContract =
            "relic_bloodied_gladiators_contract";
        private const string SilentGiantsHeart =
            "relic_silent_giants_heart";

        private static readonly double[] PioneerAttackRates =
            { 0.03d, 0.04d, 0.04d, 0.05d, 0.05d, 0.06d };
        private static readonly double[] PioneerMaxHpRates =
            { 0.03d, 0.03d, 0.04d, 0.04d, 0.05d, 0.06d };
        private static readonly double[] TradeoffReductionRates =
            { 0.12d, 0.16d, 0.20d, 0.24d, 0.28d, 0.32d };
        private static readonly double[] TradeoffIncreaseRates =
            { 0.08d, 0.10d, 0.12d, 0.14d, 0.16d, 0.18d };

        private static IEnumerable<TestCaseData> DamagePaths
        {
            get
            {
                yield return new TestCaseData(
                    AttackType.Match,
                    AttackTag.Match3);
                yield return new TestCaseData(
                    AttackType.Match,
                    AttackTag.Match4);
                yield return new TestCaseData(
                    AttackType.Match,
                    AttackTag.Match5Plus);
                yield return new TestCaseData(
                    AttackType.Active,
                    AttackTag.None);
                yield return new TestCaseData(
                    AttackType.Additional,
                    AttackTag.None);
                yield return new TestCaseData(
                    AttackType.Chase,
                    AttackTag.None);
                yield return new TestCaseData(
                    AttackType.DamageOverTime,
                    AttackTag.None);
            }
        }

        [Test]
        public void ProductionContentRegistersCompositeStatRelics()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationService localization = LoadLocalization();

            AssertContent(
                database,
                localization,
                PioneersEmblem,
                new[]
                {
                    Effect(
                        RelicEffectType.AttackIncrease,
                        "attack_rate",
                        PioneerAttackRates),
                    Effect(
                        RelicEffectType.MaxHpIncrease,
                        "max_hp_rate",
                        PioneerMaxHpRates)
                },
                "Pioneer's Emblem",
                "Increases the equipped character's ATK by 3% and maximum "
                    + "HP by 3%.",
                "Increases the equipped character's ATK by 6% and maximum "
                    + "HP by 6%.",
                "개척자의 문장",
                "장착한 캐릭터의 공격력이 3%, 최대 HP가 3% 증가합니다.",
                "장착한 캐릭터의 공격력이 6%, 최대 HP가 6% 증가합니다.");
            AssertContent(
                database,
                localization,
                BloodiedContract,
                new[]
                {
                    Effect(
                        RelicEffectType.MaxHpReduction,
                        "max_hp_reduction_rate",
                        TradeoffReductionRates),
                    Effect(
                        RelicEffectType.AttackIncrease,
                        "attack_rate",
                        TradeoffIncreaseRates)
                },
                "Bloodied Gladiator's Contract",
                "Reduces the equipped character's maximum HP by 12% and "
                    + "increases ATK by 8%.",
                "Reduces the equipped character's maximum HP by 32% and "
                    + "increases ATK by 18%.",
                "핏빛 투사의 계약",
                "장착한 캐릭터의 최대 HP가 12% 감소하는 대신 공격력이 8% "
                    + "증가합니다.",
                "장착한 캐릭터의 최대 HP가 32% 감소하는 대신 공격력이 18% "
                    + "증가합니다.");
            AssertContent(
                database,
                localization,
                SilentGiantsHeart,
                new[]
                {
                    Effect(
                        RelicEffectType.AttackReduction,
                        "attack_reduction_rate",
                        TradeoffReductionRates),
                    Effect(
                        RelicEffectType.MaxHpIncrease,
                        "max_hp_rate",
                        TradeoffIncreaseRates)
                },
                "Silent Giant's Heart",
                "Reduces the equipped character's ATK by 12% and increases "
                    + "maximum HP by 8%.",
                "Reduces the equipped character's ATK by 32% and increases "
                    + "maximum HP by 18%.",
                "침묵하는 거인의 심장",
                "장착한 캐릭터의 공격력이 12% 감소하는 대신 최대 HP가 8% "
                    + "증가합니다.",
                "장착한 캐릭터의 공격력이 32% 감소하는 대신 최대 HP가 18% "
                    + "증가합니다.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void ResolverUsesOneAwakeningForEveryCompositeEffect(
            int awakening)
        {
            DefinitionDatabase database = LoadDatabase();

            RelicStatBonusSummary pioneer = Resolve(
                database,
                Equip("pioneer", PioneersEmblem, awakening));
            RelicStatBonusSummary contract = Resolve(
                database,
                Equip("contract", BloodiedContract, awakening));
            RelicStatBonusSummary heart = Resolve(
                database,
                Equip("heart", SilentGiantsHeart, awakening));

            AssertSummary(
                pioneer,
                PioneerAttackRates[awakening],
                0d,
                PioneerMaxHpRates[awakening],
                0d);
            AssertSummary(
                contract,
                TradeoffIncreaseRates[awakening],
                0d,
                0d,
                TradeoffReductionRates[awakening]);
            AssertSummary(
                heart,
                0d,
                TradeoffReductionRates[awakening],
                TradeoffIncreaseRates[awakening],
                0d);
            Assert.That(pioneer.Sources, Has.Count.EqualTo(2));
            Assert.That(contract.Sources, Has.Count.EqualTo(2));
            Assert.That(heart.Sources, Has.Count.EqualTo(2));
            Assert.That(
                pioneer.Sources.Concat(contract.Sources)
                    .Concat(heart.Sources)
                    .All(source => source.Awakening == awakening),
                Is.True);
        }

        [Test]
        public void ExistingSingleStatRelicsAddWithCompositeRelics()
        {
            DefinitionDatabase database = LoadDatabase();
            RelicStatBonusSummary pioneer = Resolve(
                database,
                Equip("pioneer", PioneersEmblem, 5),
                Equip(
                    "sword",
                    "relic_victors_broken_sword",
                    5),
                Equip("bark", "relic_world_tree_bark", 5));
            RelicStatBonusSummary contract = Resolve(
                database,
                Equip("contract", BloodiedContract, 5),
                Equip("bark", "relic_world_tree_bark", 5));
            RelicStatBonusSummary heart = Resolve(
                database,
                Equip("heart", SilentGiantsHeart, 5),
                Equip(
                    "sword",
                    "relic_victors_broken_sword",
                    5));

            Assert.That(pioneer.AttackMultiplier,
                Is.EqualTo(1.16d).Within(1e-12d));
            Assert.That(pioneer.MaxHpMultiplier,
                Is.EqualTo(1.16d).Within(1e-12d));
            Assert.That(contract.AttackMultiplier,
                Is.EqualTo(1.18d).Within(1e-12d));
            Assert.That(contract.MaxHpMultiplier,
                Is.EqualTo(0.78d).Within(1e-12d));
            Assert.That(heart.AttackMultiplier,
                Is.EqualTo(0.78d).Within(1e-12d));
            Assert.That(heart.MaxHpMultiplier,
                Is.EqualTo(1.18d).Within(1e-12d));
        }

        [Test]
        public void AllThreeCompositeRelicsProduceNinetyTwoPercentStats()
        {
            RelicStatBonusSummary result = ResolveAllThree(LoadDatabase());

            AssertSummary(result, 0.24d, 0.32d, 0.24d, 0.32d);
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(0.92d).Within(1e-12d));
            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(0.92d).Within(1e-12d));
            Assert.That(result.Sources, Has.Count.EqualTo(6));
        }

        [TestCaseSource(nameof(DamagePaths))]
        public void CompositeAttackModifierAppliesOnceThroughDamagePaths(
            AttackType attackType,
            AttackTag attackTag)
        {
            RelicStatBonusSummary bonuses = ResolveAllThree(LoadDatabase());
            CharacterBattleState attacker = BuildCharacter(bonuses);
            var party = new PartyBattleState(new[] { attacker });
            var boss = new BossBattleState(
                "boss",
                attacker.Element,
                100000,
                100d);

            DamageContext context = BuildDamageContext(
                attacker,
                party,
                boss,
                attackType,
                attackTag);
            DamageResult result = DamageCalculator.Calculate(context);
            double expectedFinalAttack = attacker.Attack * 0.92d;

            Assert.That(context.AttackIncreaseRateSum,
                Is.EqualTo(-0.08d).Within(1e-12d));
            Assert.That(result.FinalAttack,
                Is.EqualTo(expectedFinalAttack).Within(1e-9d));
            Assert.That(result.FinalDamage,
                Is.EqualTo((long)Math.Floor(expectedFinalAttack)));
        }

        [Test]
        public void PermanentAndRuntimeAttackModifiersShareOneAdditiveCategory()
        {
            RelicStatBonusSummary bonuses = ResolveAllThree(LoadDatabase());
            CharacterBattleState attacker = BuildCharacter(bonuses);
            var party = new PartyBattleState(new[] { attacker });
            var boss = new BossBattleState(
                "boss",
                attacker.Element,
                100000,
                100d);
            attacker.Effects.ApplyEffect(new EffectInstance(
                1,
                "runtime_attack",
                "source",
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                0.10d,
                2,
                1));

            DamageContext context = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Active,
                AttackTag.None);

            Assert.That(context.AttackIncreaseRateSum,
                Is.EqualTo(0.02d).Within(1e-12d));
            Assert.That(DamageCalculator.Calculate(context).FinalAttack,
                Is.EqualTo(attacker.Attack * 1.02d).Within(1e-9d));
        }

        [TestCase(BloodiedContract, 1.18d, 0.68d)]
        [TestCase(SilentGiantsHeart, 0.68d, 1.18d)]
        public void TradeoffRelicsChangeCombatAndPartyHpOnce(
            string relicId,
            double expectedAttackMultiplier,
            double expectedMaxHpMultiplier)
        {
            DefinitionDatabase database = LoadDatabase();
            RelicStatBonusSummary bonuses = Resolve(
                database,
                Equip("relic", relicId, 5));
            CharacterDefinition definition = LoadCharacterDefinition();
            CharacterBuildResolver resolver =
                CharacterBuildResolverFactory.CreateDefault();
            ResolvedCharacterBuild baseline = resolver.Resolve(
                definition,
                level: 1,
                awakening: 0);
            ResolvedCharacterBuild build = resolver.Resolve(
                definition,
                level: 1,
                awakening: 0,
                relicBonuses: bonuses);
            CharacterBattleState character = CharacterBattleStateFactory.Create(
                definition,
                build,
                0,
                bonuses);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                character.Element,
                100000,
                100d);
            DamageResult damage = DamageCalculator.Calculate(
                BuildDamageContext(
                    character,
                    party,
                    boss,
                    AttackType.Match,
                    AttackTag.Match3));

            Assert.That(build.MaxHp, Is.EqualTo(
                CharacterStatCalculator.RoundFinalStat(
                    baseline.MaxHp * expectedMaxHpMultiplier)));
            Assert.That(party.MaxHp, Is.EqualTo(build.MaxHp));
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp));
            Assert.That(damage.FinalAttack,
                Is.EqualTo(build.Attack * expectedAttackMultiplier)
                    .Within(1e-9d));
        }

        [TestCase(
            "relic_saintess_silver_chalice",
            RelicEffectType.HealingIncrease)]
        [TestCase(
            "relic_titans_carapace",
            RelicEffectType.ShieldAmountIncrease)]
        public void FinalMaxHpAndSupportRelicsRemainSeparate(
            string supportRelicId,
            RelicEffectType supportEffectType)
        {
            DefinitionDatabase database = LoadDatabase();
            RelicStatBonusSummary bonuses = Resolve(
                database,
                Equip("pioneer", PioneersEmblem, 5),
                Equip("contract", BloodiedContract, 5),
                Equip("heart", SilentGiantsHeart, 5),
                Equip("support", supportRelicId, 5));
            CharacterBattleState source = BuildCharacter(bonuses);
            var party = new PartyBattleState(new[] { source });
            double expectedRaw = source.MaxHp * 0.10d * 1.18d;

            Assert.That(bonuses.MaxHpMultiplier,
                Is.EqualTo(0.92d).Within(1e-12d));
            if (supportEffectType == RelicEffectType.HealingIncrease)
            {
                HealingResult healing = HealingCalculator.Calculate(
                    HealingContextFactory.Build(
                        new HealingContextBuildRequest(
                            source,
                            party,
                            0.10d,
                            false,
                            0)));
                Assert.That(healing.SourceMaxHp, Is.EqualTo(source.MaxHp));
                Assert.That(healing.RawHealing,
                    Is.EqualTo(expectedRaw).Within(1e-9d));
                Assert.That(healing.FinalHealing,
                    Is.EqualTo((long)Math.Floor(expectedRaw)));
            }
            else
            {
                ShieldGenerationResult shield =
                    ShieldGenerationCalculator.Calculate(
                        ShieldGenerationContextFactory.Build(
                            new ShieldGenerationContextBuildRequest(
                                source,
                                party,
                                0.10d,
                                false,
                                0)));
                Assert.That(shield.SourceMaxHp, Is.EqualTo(source.MaxHp));
                Assert.That(shield.RawShieldAmount,
                    Is.EqualTo(expectedRaw).Within(1e-9d));
                Assert.That(shield.FinalShieldAmount,
                    Is.EqualTo((long)Math.Floor(expectedRaw)));
            }
        }

        [Test]
        public void CompositeStatsDoNotLeakToAnotherPartyMember()
        {
            RelicStatBonusSummary bonuses = ResolveAllThree(LoadDatabase());
            CharacterBattleState owner = BuildCharacter(bonuses);
            var other = new CharacterBattleState(
                "other",
                1,
                owner.Element,
                1000,
                1000d);
            var party = new PartyBattleState(new[] { owner, other });
            var boss = new BossBattleState(
                "boss",
                owner.Element,
                100000,
                100d);

            DamageContext otherContext = BuildDamageContext(
                other,
                party,
                boss,
                AttackType.Active,
                AttackTag.None);

            Assert.That(otherContext.AttackIncreaseRateSum, Is.Zero);
            Assert.That(DamageCalculator.Calculate(otherContext).FinalAttack,
                Is.EqualTo(1000d));
        }

        private static void AssertContent(
            DefinitionDatabase database,
            LocalizationService localization,
            string relicId,
            IReadOnlyList<EffectExpectation> expectedEffects,
            string englishName,
            string englishAtZero,
            string englishAtFive,
            string koreanName,
            string koreanAtZero,
            string koreanAtFive)
        {
            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.Effects, Has.Count.EqualTo(2));
            for (int index = 0; index < expectedEffects.Count; index++)
            {
                RelicEffectEntry effect = relic.Effects[index];
                EffectExpectation expected = expectedEffects[index];
                Assert.That(effect.EffectType, Is.EqualTo(expected.Type));
                Assert.That(effect.DescriptionValueKey,
                    Is.EqualTo(expected.Key));
                Assert.That(effect.EffectRates, Is.EqualTo(expected.Rates));
                Assert.That(effect.HasElementFilter, Is.False);
                Assert.That(effect.AttackTypeMask,
                    Is.EqualTo(AttackTypeMask.None));
                Assert.That(effect.RequiredAttackTags,
                    Is.EqualTo(AttackTag.None));
                Assert.That(effect.ExcludedAttackTags,
                    Is.EqualTo(AttackTag.None));
                Assert.That(effect.ConditionType,
                    Is.EqualTo(RelicEffectConditionType.None));
            }

            Assert.That(database.TryGetRelicPresentation(
                relicId,
                out RelicPresentationDefinition presentation), Is.True);
            Assert.That(presentation.RelicId, Is.EqualTo(relicId));
            Assert.That(
                presentation.Icon == null
                    || !string.IsNullOrEmpty(
                        AssetDatabase.GetAssetPath(presentation.Icon)),
                Is.True,
                "An assigned relic icon must be a persisted Sprite asset.");

            localization.SetLocale("en");
            Assert.That(localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(englishName));
            Assert.That(FormatDescription(localization, relic, 0),
                Is.EqualTo(englishAtZero));
            Assert.That(FormatDescription(localization, relic, 5),
                Is.EqualTo(englishAtFive));
            localization.SetLocale("ko");
            Assert.That(localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(koreanName));
            Assert.That(FormatDescription(localization, relic, 0),
                Is.EqualTo(koreanAtZero));
            Assert.That(FormatDescription(localization, relic, 5),
                Is.EqualTo(koreanAtFive));
        }

        private static RelicStatBonusSummary ResolveAllThree(
            DefinitionDatabase database)
        {
            return Resolve(
                database,
                Equip("pioneer", PioneersEmblem, 5),
                Equip("contract", BloodiedContract, 5),
                Equip("heart", SilentGiantsHeart, 5));
        }

        private static RelicStatBonusSummary Resolve(
            DefinitionDatabase database,
            params RelicEquip[] equips)
        {
            var character = new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                Awakening = 0,
                EquippedRelicInstanceIds =
                    SaveRules.CreateEmptyRelicInstanceIdSlots()
            };
            var instances = new List<RelicInstanceSaveData>(equips.Length);
            for (int index = 0; index < equips.Length; index++)
            {
                RelicEquip equip = equips[index];
                character.EquippedRelicInstanceIds[index] =
                    equip.InstanceId;
                instances.Add(new RelicInstanceSaveData
                {
                    InstanceId = equip.InstanceId,
                    RelicDefinitionId = equip.DefinitionId,
                    Awakening = equip.Awakening
                });
            }

            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = instances
            };
            return new RelicStatBonusResolver(database).Resolve(
                profile,
                character);
        }

        private static CharacterBattleState BuildCharacter(
            RelicStatBonusSummary bonuses)
        {
            CharacterDefinition definition = LoadCharacterDefinition();
            ResolvedCharacterBuild build =
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    definition,
                    level: 1,
                    awakening: 0,
                    relicBonuses: bonuses);
            return CharacterBattleStateFactory.Create(
                definition,
                build,
                0,
                bonuses);
        }

        private static DamageContext BuildDamageContext(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType,
            AttackTag attackTag)
        {
            return new DamageContextFactory(
                new SeededRandomSource(1)).Build(
                    new DamageContextBuildRequest(
                        attacker,
                        party,
                        boss,
                        attacker.Element,
                        attackType,
                        attackTag,
                        1d,
                        false,
                        0,
                        false));
        }

        private static void AssertSummary(
            RelicStatBonusSummary summary,
            double attackIncrease,
            double attackReduction,
            double maxHpIncrease,
            double maxHpReduction)
        {
            Assert.That(summary.AttackIncreaseRateSum,
                Is.EqualTo(attackIncrease).Within(1e-12d));
            Assert.That(summary.AttackReductionRateSum,
                Is.EqualTo(attackReduction).Within(1e-12d));
            Assert.That(summary.MaxHpIncreaseRateSum,
                Is.EqualTo(maxHpIncrease).Within(1e-12d));
            Assert.That(summary.MaxHpReductionRateSum,
                Is.EqualTo(maxHpReduction).Within(1e-12d));
        }

        private static string FormatDescription(
            LocalizationService localization,
            RelicDefinition relic,
            int awakening)
        {
            var values = relic.GetDescriptionValues(awakening)
                .ToDictionary(
                    pair => pair.Key,
                    pair => (object)(pair.Value * 100d).ToString(
                        "0.##",
                        CultureInfo.InvariantCulture));
            return localization.Format(relic.DescriptionKey, values);
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static LocalizationService LoadLocalization()
        {
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog.CreateService();
        }

        private static CharacterDefinition LoadCharacterDefinition()
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    CharacterPath);
            Assert.That(definition, Is.Not.Null);
            return definition;
        }

        private static EffectExpectation Effect(
            RelicEffectType type,
            string key,
            double[] rates)
        {
            return new EffectExpectation(type, key, rates);
        }

        private static RelicEquip Equip(
            string instanceId,
            string definitionId,
            int awakening)
        {
            return new RelicEquip(instanceId, definitionId, awakening);
        }

        private sealed class EffectExpectation
        {
            public EffectExpectation(
                RelicEffectType type,
                string key,
                double[] rates)
            {
                Type = type;
                Key = key;
                Rates = rates;
            }

            public RelicEffectType Type { get; }
            public string Key { get; }
            public double[] Rates { get; }
        }

        private sealed class RelicEquip
        {
            public RelicEquip(
                string instanceId,
                string definitionId,
                int awakening)
            {
                InstanceId = instanceId;
                DefinitionId = definitionId;
                Awakening = awakening;
            }

            public string InstanceId { get; }
            public string DefinitionId { get; }
            public int Awakening { get; }
        }
    }
}

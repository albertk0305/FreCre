using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Stats;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ElementDamageRelicTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        private static readonly double[] ExpectedRates =
        {
            0.08d,
            0.09d,
            0.10d,
            0.11d,
            0.12d,
            0.14d
        };

        [TestCase(
            "relic_ember_lords_crown",
            ElementType.Fire,
            "Ember Lord's Crown",
            "Fire",
            "잿불 군주의 왕관",
            "불")]
        [TestCase(
            "relic_deep_sea_kings_blue_pearl",
            ElementType.Water,
            "Deep Sea King's Blue Pearl",
            "Water",
            "심해왕의 푸른 진주",
            "물")]
        [TestCase(
            "relic_primordial_trees_life_seed",
            ElementType.Grass,
            "Primordial Tree's Life Seed",
            "Grass",
            "태고목의 생명 씨앗",
            "풀")]
        [TestCase(
            "relic_morning_stars_radiance",
            ElementType.Light,
            "Morning Star's Radiance",
            "Light",
            "새벽별의 성휘",
            "빛")]
        [TestCase(
            "relic_eclipses_obsidian",
            ElementType.Dark,
            "Eclipse Obsidian",
            "Dark",
            "월식의 흑요석",
            "어둠")]
        public void ProductionContentRegistersElementRelic(
            string relicId,
            ElementType expectedElement,
            string expectedEnglishName,
            string expectedEnglishElement,
            string expectedKoreanName,
            string expectedKoreanElement)
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.Effects, Has.Count.EqualTo(1));
            RelicEffectEntry effect = relic.Effects[0];
            Assert.That(effect.EffectType,
                Is.EqualTo(RelicEffectType.ElementDamageIncrease));
            Assert.That(effect.EffectRates, Is.EqualTo(ExpectedRates));
            Assert.That(effect.DescriptionValueKey, Is.EqualTo("rate"));
            Assert.That(effect.HasElementFilter, Is.True);
            Assert.That(effect.ElementFilter, Is.EqualTo(expectedElement));
            Assert.That(effect.AttackTypeMask,
                Is.EqualTo(AttackTypeMask.None));
            Assert.That(effect.RequiredAttackTags, Is.EqualTo(AttackTag.None));
            Assert.That(effect.ExcludedAttackTags, Is.EqualTo(AttackTag.None));

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

            LocalizationService localization = LoadLocalization();
            localization.SetLocale("en");
            Assert.That(localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(expectedEnglishName));
            Assert.That(FormatDescription(localization, relic, 0),
                Is.EqualTo("Increases the equipped character's "
                    + $"{expectedEnglishElement} damage by 8%."));
            Assert.That(FormatDescription(localization, relic, 5),
                Is.EqualTo("Increases the equipped character's "
                    + $"{expectedEnglishElement} damage by 14%."));

            localization.SetLocale("ko");
            Assert.That(localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(expectedKoreanName));
            Assert.That(FormatDescription(localization, relic, 0),
                Is.EqualTo("장착한 캐릭터가 가하는 "
                    + $"{expectedKoreanElement}속성 피해가 8% 증가합니다."));
            Assert.That(FormatDescription(localization, relic, 5),
                Is.EqualTo("장착한 캐릭터가 가하는 "
                    + $"{expectedKoreanElement}속성 피해가 14% 증가합니다."));
        }

        [Test]
        public void ResolverPreservesElementSourcesAndIgnoresDuplicateSlots()
        {
            DefinitionDatabase database = LoadDatabase();
            var character = new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                Awakening = 0,
                EquippedRelicInstanceIds =
                    SaveRules.CreateEmptyRelicInstanceIdSlots()
            };
            character.EquippedRelicInstanceIds[0] = "fire";
            character.EquippedRelicInstanceIds[1] = "water";
            character.EquippedRelicInstanceIds[2] = "fire";
            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    Instance(
                        "fire",
                        "relic_ember_lords_crown",
                        5),
                    Instance(
                        "water",
                        "relic_deep_sea_kings_blue_pearl",
                        0)
                }
            };

            RelicStatBonusSummary result = new RelicStatBonusResolver(
                database).Resolve(profile, character);

            Assert.That(result.Sources, Has.Count.EqualTo(2));
            Assert.That(result.Sources.Select(source => source.ElementFilter),
                Is.EqualTo(new[] { ElementType.Fire, ElementType.Water }));
            Assert.That(result.Sources.Select(source => source.Rate),
                Is.EqualTo(new[] { 0.14d, 0.08d }));
            Assert.That(result.AttackIncreaseRateSum, Is.Zero);
            Assert.That(result.MaxHpIncreaseRateSum, Is.Zero);
            Assert.That(result.CriticalChanceIncreaseRateSum, Is.Zero);
            Assert.That(result.CriticalDamageIncreaseRateSum, Is.Zero);
        }

        [TestCase(ElementType.Fire)]
        [TestCase(ElementType.Water)]
        [TestCase(ElementType.Grass)]
        [TestCase(ElementType.Light)]
        [TestCase(ElementType.Dark)]
        public void PermanentRelicUsesActualAttackElement(
            ElementType relicElement)
        {
            CreateStates(
                new[] { Source("element_relic", relicElement, 0.14d) },
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss,
                characterElement: NextElement(relicElement));

            DamageContext matching = BuildContext(
                attacker,
                party,
                boss,
                relicElement,
                AttackType.Match,
                AttackTag.Match3);
            DamageContext different = BuildContext(
                attacker,
                party,
                boss,
                NextElement(relicElement),
                AttackType.Match,
                AttackTag.Match3);

            Assert.That(matching.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.14d));
            Assert.That(different.ElementDamageIncreaseRateSum, Is.Zero);
        }

        [TestCase(AttackType.Match, AttackTag.Match3)]
        [TestCase(AttackType.Match, AttackTag.Match4)]
        [TestCase(AttackType.Match, AttackTag.Match5Plus)]
        [TestCase(AttackType.Active, AttackTag.None)]
        [TestCase(AttackType.Additional, AttackTag.None)]
        [TestCase(AttackType.Chase, AttackTag.None)]
        [TestCase(AttackType.DamageOverTime, AttackTag.None)]
        public void PermanentElementRelicAppliesThroughCommonDamagePaths(
            AttackType attackType,
            AttackTag attackTag)
        {
            CreateStates(
                new[] { Source("water_relic", ElementType.Water, 0.14d) },
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);

            DamageContext context = BuildContext(
                attacker,
                party,
                boss,
                ElementType.Water,
                attackType,
                attackTag);

            Assert.That(context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.14d));
            Assert.That(DamageCalculator.Calculate(context)
                .ElementDamageMultiplier,
                Is.EqualTo(1.14d).Within(1e-12d));
        }

        [Test]
        public void PermanentRelicAddsWithRuntimeElementBuffAndMultipliesAttack()
        {
            CreateStates(
                new[] { Source("water_relic", ElementType.Water, 0.14d) },
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss,
                permanentAttackIncreaseRate: 0.10d);
            attacker.Effects.ApplyEffect(new EffectInstance(
                1,
                "runtime_water",
                "active",
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                0.25d,
                2,
                1,
                elementFilter: ElementType.Water));

            DamageContext context = BuildContext(
                attacker,
                party,
                boss,
                ElementType.Water,
                AttackType.Active,
                AttackTag.Elemental);
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(context.AttackIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.39d).Within(1e-12d));
            Assert.That(result.FinalAttack, Is.EqualTo(1100d));
            Assert.That(result.ElementDamageMultiplier,
                Is.EqualTo(1.39d).Within(1e-12d));
            Assert.That(result.FinalDamage, Is.EqualTo(1529L));
        }

        [Test]
        public void DifferentElementRelicsRemainIndependentAndOwnerScoped()
        {
            var attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Grass,
                100,
                1000d,
                permanentRelicEffects: new[]
                {
                    Source("fire_relic", ElementType.Fire, 0.14d),
                    Source("water_relic", ElementType.Water, 0.12d)
                });
            var other = new CharacterBattleState(
                "other",
                1,
                ElementType.Water,
                100,
                1000d,
                permanentRelicEffects: new[]
                {
                    Source("other_water", ElementType.Water, 0.14d)
                });
            var party = new PartyBattleState(new[] { attacker, other });
            var boss = new BossBattleState(
                "boss",
                ElementType.Water,
                100000,
                100d);

            DamageContext fire = BuildContext(
                attacker,
                party,
                boss,
                ElementType.Fire,
                AttackType.Match,
                AttackTag.Match3);
            DamageContext water = BuildContext(
                attacker,
                party,
                boss,
                ElementType.Water,
                AttackType.Match,
                AttackTag.Match3);
            DamageContext grass = BuildContext(
                attacker,
                party,
                boss,
                ElementType.Grass,
                AttackType.Match,
                AttackTag.Match3);

            Assert.That(fire.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.14d));
            Assert.That(water.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.12d));
            Assert.That(grass.ElementDamageIncreaseRateSum, Is.Zero);
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

        private static RelicInstanceSaveData Instance(
            string instanceId,
            string definitionId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = awakening
            };
        }

        private static RelicStatBonusSource Source(
            string definitionId,
            ElementType element,
            double rate)
        {
            return new RelicStatBonusSource(
                definitionId,
                definitionId + ".name",
                definitionId + ".description",
                RelicEffectType.ElementDamageIncrease,
                5,
                rate,
                "rate",
                true,
                element,
                AttackTypeMask.None,
                AttackTag.None,
                AttackTag.None,
                RelicEffectConditionType.None,
                0d);
        }

        private static void CreateStates(
            IReadOnlyList<RelicStatBonusSource> sources,
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss,
            ElementType characterElement = ElementType.Fire,
            double permanentAttackIncreaseRate = 0d)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                characterElement,
                100,
                1000d,
                permanentAttackIncreaseRate,
                permanentRelicEffects: sources);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Water,
                100000,
                100d);
        }

        private static DamageContext BuildContext(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            ElementType attackElement,
            AttackType attackType,
            AttackTag attackTag)
        {
            var request = new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                attackElement,
                attackType,
                attackTag,
                1d,
                false,
                0,
                false);
            return new DamageContextFactory(
                new SeededRandomSource(1)).Build(request);
        }

        private static ElementType NextElement(ElementType element)
        {
            return (ElementType)(((int)element + 1) % 5);
        }
    }
}

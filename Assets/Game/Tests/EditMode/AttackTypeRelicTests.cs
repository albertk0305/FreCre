using System;
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
    public sealed class AttackTypeRelicTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string PerfectBondSeal =
            "relic_perfect_bond_seal";
        private const string TrackersChain =
            "relic_trackers_chain";
        private const string OverlordsSeal =
            "relic_overlords_seal";

        private static readonly double[] AttackTypeRates =
            { 0.06d, 0.07d, 0.08d, 0.09d, 0.10d, 0.12d };
        private static readonly double[] DealtDamageRates =
            { 0.04d, 0.05d, 0.06d, 0.07d, 0.08d, 0.09d };

        private static IEnumerable<TestCaseData> PerfectBondCases
        {
            get
            {
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match3,
                    0d);
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match4,
                    0d);
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match5Plus,
                    0.12d);
                yield return Case(
                    AttackType.Additional,
                    AttackTag.Match5Plus,
                    0d);
                yield return Case(
                    AttackType.Chase,
                    AttackTag.Match5Plus,
                    0d);
                yield return Case(
                    AttackType.DamageOverTime,
                    AttackTag.None,
                    0d);
            }
        }

        private static IEnumerable<TestCaseData> TrackerCases
        {
            get
            {
                yield return Case(
                    AttackType.Additional,
                    AttackTag.None,
                    0.12d);
                yield return Case(
                    AttackType.Chase,
                    AttackTag.None,
                    0.12d);
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match5Plus,
                    0d);
                yield return Case(
                    AttackType.Active,
                    AttackTag.None,
                    0d);
                yield return Case(
                    AttackType.DamageOverTime,
                    AttackTag.None,
                    0d);
                yield return Case(
                    AttackType.Passive,
                    AttackTag.None,
                    0d);
                yield return Case(
                    AttackType.Additional,
                    AttackTag.FixedDamage,
                    0d);
                yield return Case(
                    AttackType.Chase,
                    AttackTag.FixedDamage,
                    0d);
            }
        }

        private static IEnumerable<TestCaseData> OverlordCases
        {
            get
            {
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match3,
                    0.09d);
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match4,
                    0.09d);
                yield return Case(
                    AttackType.Match,
                    AttackTag.Match5Plus,
                    0.09d);
                yield return Case(
                    AttackType.Active,
                    AttackTag.None,
                    0.09d);
                yield return Case(
                    AttackType.Additional,
                    AttackTag.None,
                    0.09d);
                yield return Case(
                    AttackType.Chase,
                    AttackTag.None,
                    0.09d);
                yield return Case(
                    AttackType.DamageOverTime,
                    AttackTag.None,
                    0.09d);
                yield return Case(
                    AttackType.Passive,
                    AttackTag.None,
                    0d);
                foreach (AttackType attackType in new[]
                {
                    AttackType.Match,
                    AttackType.Active,
                    AttackType.Additional,
                    AttackType.Chase,
                    AttackType.DamageOverTime
                })
                {
                    yield return Case(
                        attackType,
                        AttackTag.FixedDamage,
                        0d);
                }
            }
        }

        [Test]
        public void ProductionContentRegistersAttackTypeRelics()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationService localization = LoadLocalization();

            AssertContent(
                database,
                localization,
                PerfectBondSeal,
                RelicEffectType.AttackTypeDamageIncrease,
                AttackTypeRates,
                AttackTypeMask.Match,
                AttackTag.Match5Plus,
                AttackTag.None,
                "Perfect Bond Seal",
                "Increases damage dealt by attacks triggered by a match of "
                    + "5 or more by 6%.",
                "Increases damage dealt by attacks triggered by a match of "
                    + "5 or more by 12%.",
                "완전한 결속의 인장",
                "5매칭 이상으로 발동한 공격의 피해가 6% 증가합니다.",
                "5매칭 이상으로 발동한 공격의 피해가 12% 증가합니다.");
            AssertContent(
                database,
                localization,
                TrackersChain,
                RelicEffectType.AttackTypeDamageIncrease,
                AttackTypeRates,
                AttackTypeMask.Additional | AttackTypeMask.Chase,
                AttackTag.None,
                AttackTag.FixedDamage,
                "Tracker's Chain",
                "Increases additional and chase attack damage by 6%.",
                "Increases additional and chase attack damage by 12%.",
                "추적자의 사슬",
                "추가 공격과 추격 공격의 피해가 6% 증가합니다.",
                "추가 공격과 추격 공격의 피해가 12% 증가합니다.");
            AssertContent(
                database,
                localization,
                OverlordsSeal,
                RelicEffectType.DealtDamageIncrease,
                DealtDamageRates,
                AttackTypeMask.Match
                    | AttackTypeMask.Active
                    | AttackTypeMask.Additional
                    | AttackTypeMask.Chase
                    | AttackTypeMask.DamageOverTime,
                AttackTag.None,
                AttackTag.FixedDamage,
                "Overlord's Seal",
                "Increases damage dealt by 4%. Does not apply to fixed "
                    + "damage.",
                "Increases damage dealt by 9%. Does not apply to fixed "
                    + "damage.",
                "패왕의 인장",
                "가하는 피해가 4% 증가합니다. 고정 피해에는 적용되지 "
                    + "않습니다.",
                "가하는 피해가 9% 증가합니다. 고정 피해에는 적용되지 "
                    + "않습니다.");
        }

        [TestCase(PerfectBondSeal, 0, 0.06d)]
        [TestCase(PerfectBondSeal, 5, 0.12d)]
        [TestCase(TrackersChain, 0, 0.06d)]
        [TestCase(TrackersChain, 5, 0.12d)]
        [TestCase(OverlordsSeal, 0, 0.04d)]
        [TestCase(OverlordsSeal, 5, 0.09d)]
        public void ResolverUsesEquippedAwakeningAndPreservesFilters(
            string relicId,
            int awakening,
            double expectedRate)
        {
            DefinitionDatabase database = LoadDatabase();
            RelicStatBonusSummary result = Resolve(
                database,
                relicId,
                awakening);

            Assert.That(result.Sources, Has.Count.EqualTo(1));
            RelicStatBonusSource source = result.Sources[0];
            Assert.That(source.DefinitionId, Is.EqualTo(relicId));
            Assert.That(source.Awakening, Is.EqualTo(awakening));
            Assert.That(source.Rate,
                Is.EqualTo(expectedRate).Within(1e-12d));
            Assert.That(source.AttackTypeMask,
                Is.EqualTo(ExpectedMask(relicId)));
            Assert.That(source.RequiredAttackTags,
                Is.EqualTo(relicId == PerfectBondSeal
                    ? AttackTag.Match5Plus
                    : AttackTag.None));
            Assert.That(source.ExcludedAttackTags,
                Is.EqualTo(relicId == PerfectBondSeal
                    ? AttackTag.None
                    : AttackTag.FixedDamage));
        }

        [TestCaseSource(nameof(PerfectBondCases))]
        public void PerfectBondSealRequiresMatchFivePlus(
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate)
        {
            DamageContext context = BuildProductionContext(
                PerfectBondSeal,
                5,
                attackType,
                attackTags);

            Assert.That(context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
            Assert.That(context.DealtDamageIncreaseRateSum, Is.Zero);
        }

        [Test]
        public void PerfectBondSealScalesTheWholeResolvedSkillCoefficient()
        {
            DamageContext context = BuildProductionContext(
                PerfectBondSeal,
                5,
                AttackType.Match,
                AttackTag.Match5Plus,
                skillCoefficient: 2.50d);
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(result.SkillCoefficient, Is.EqualTo(2.50d));
            Assert.That(result.AttackTypeDamageMultiplier,
                Is.EqualTo(1.12d).Within(1e-12d));
            Assert.That(result.RawDamage,
                Is.EqualTo(2800d).Within(1e-9d));
            Assert.That(result.FinalDamage, Is.EqualTo(2800L));
        }

        [TestCaseSource(nameof(TrackerCases))]
        public void TrackersChainAppliesOnlyToNonFixedAdditionalAndChase(
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate)
        {
            DamageContext context = BuildProductionContext(
                TrackersChain,
                5,
                attackType,
                attackTags);

            Assert.That(context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
        }

        [TestCaseSource(nameof(OverlordCases))]
        public void OverlordsSealAppliesToNormalDamageButNotFixedOrPassive(
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate)
        {
            DamageContext context = BuildProductionContext(
                OverlordsSeal,
                5,
                attackType,
                attackTags);

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
            Assert.That(context.TargetTakenDamageIncreaseRateSum, Is.Zero);
        }

        [TestCase(
            PerfectBondSeal,
            AttackType.Match,
            AttackTag.Match5Plus,
            0.06d,
            1060L)]
        [TestCase(
            TrackersChain,
            AttackType.Additional,
            AttackTag.None,
            0.06d,
            1060L)]
        [TestCase(
            OverlordsSeal,
            AttackType.Active,
            AttackTag.None,
            0.04d,
            1040L)]
        public void ZeroAwakeningRatesFlowToFinalDamage(
            string relicId,
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate,
            long expectedDamage)
        {
            DamageContext context = BuildProductionContext(
                relicId,
                0,
                attackType,
                attackTags);
            DamageResult result = DamageCalculator.Calculate(context);

            double actualRate = relicId == OverlordsSeal
                ? context.DealtDamageIncreaseRateSum
                : context.AttackTypeDamageIncreaseRateSum;
            Assert.That(actualRate,
                Is.EqualTo(expectedRate).Within(1e-12d));
            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [TestCase(
            PerfectBondSeal,
            AttackType.Match,
            AttackTag.Match5Plus,
            1120L)]
        [TestCase(
            TrackersChain,
            AttackType.Additional,
            AttackTag.None,
            1120L)]
        [TestCase(
            TrackersChain,
            AttackType.Chase,
            AttackTag.None,
            1120L)]
        [TestCase(
            OverlordsSeal,
            AttackType.Active,
            AttackTag.None,
            1090L)]
        [TestCase(
            OverlordsSeal,
            AttackType.DamageOverTime,
            AttackTag.None,
            1090L)]
        [TestCase(
            OverlordsSeal,
            AttackType.Additional,
            AttackTag.FixedDamage,
            1000L)]
        public void ProductionFiltersDriveFinalIntegerDamage(
            string relicId,
            AttackType attackType,
            AttackTag attackTags,
            long expectedDamage)
        {
            DamageResult result = DamageCalculator.Calculate(
                BuildProductionContext(
                    relicId,
                    5,
                    attackType,
                    attackTags));

            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [TestCase(
            AttackType.Match,
            AttackTag.Match5Plus,
            0.12d)]
        [TestCase(
            AttackType.Active,
            AttackTag.Match5Plus,
            0d)]
        [TestCase(
            AttackType.Match,
            AttackTag.Match4,
            0d)]
        [TestCase(
            AttackType.Match,
            AttackTag.Match5Plus | AttackTag.FixedDamage,
            0d)]
        public void CombinedTypeRequiredAndExcludedFiltersAreHonored(
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate)
        {
            RelicStatBonusSource source = Source(
                "combined_filter",
                RelicEffectType.AttackTypeDamageIncrease,
                0.12d,
                AttackTypeMask.Match,
                AttackTag.Match5Plus,
                AttackTag.FixedDamage);
            DamageContext context = BuildContext(
                new[] { source },
                attackType,
                attackTags);

            Assert.That(context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
        }

        [Test]
        public void FilterlessPermanentDealtDamageSourceAppliesToPassive()
        {
            DamageContext context = BuildContext(
                new[]
                {
                    Source(
                        "unfiltered",
                        RelicEffectType.DealtDamageIncrease,
                        0.07d,
                        AttackTypeMask.None,
                        AttackTag.None,
                        AttackTag.None)
                },
                AttackType.Passive,
                AttackTag.None);

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.07d).Within(1e-12d));
        }

        [Test]
        public void RelicAndRuntimeBonusesAddWithinCategoriesAndMultiplyAcrossThem()
        {
            var sources = new[]
            {
                Source(
                    "element",
                    RelicEffectType.ElementDamageIncrease,
                    0.14d,
                    AttackTypeMask.None,
                    AttackTag.None,
                    AttackTag.None,
                    true,
                    ElementType.Fire),
                Source(
                    "perfect_bond",
                    RelicEffectType.AttackTypeDamageIncrease,
                    0.12d,
                    AttackTypeMask.Match,
                    AttackTag.Match5Plus,
                    AttackTag.None),
                Source(
                    "overlord",
                    RelicEffectType.DealtDamageIncrease,
                    0.09d,
                    AttackTypeMask.Match,
                    AttackTag.None,
                    AttackTag.FixedDamage)
            };
            CreateStates(
                sources,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(RuntimeEffect(
                1,
                EffectModifierType.AttackTypeDamageIncrease,
                0.08d,
                AttackTypeMask.Match,
                AttackTag.Match5Plus));
            attacker.Effects.ApplyEffect(RuntimeEffect(
                2,
                EffectModifierType.DealtDamageIncrease,
                0.11d));

            DamageContext context = BuildContext(
                attacker,
                party,
                boss,
                AttackType.Match,
                AttackTag.Match5Plus);
            DamageResult result = DamageCalculator.Calculate(context);
            double expectedRaw = 1000d * 1.14d * 1.20d * 1.20d;

            Assert.That(context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.14d).Within(1e-12d));
            Assert.That(context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.20d).Within(1e-12d));
            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.20d).Within(1e-12d));
            Assert.That(result.RawDamage,
                Is.EqualTo(expectedRaw).Within(1e-9d));
            Assert.That(result.FinalDamage,
                Is.EqualTo((long)Math.Floor(expectedRaw)));
        }

        [Test]
        public void OverlordAndTargetTakenIncreaseRemainSeparate()
        {
            CreateStates(
                new[]
                {
                    Source(
                        "overlord",
                        RelicEffectType.DealtDamageIncrease,
                        0.09d,
                        AttackTypeMask.Match,
                        AttackTag.None,
                        AttackTag.FixedDamage)
                },
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(new EffectInstance(
                1,
                "taken_increase",
                "source",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                0.20d,
                2,
                1));

            DamageContext context = BuildContext(
                attacker,
                party,
                boss,
                AttackType.Match,
                AttackTag.Match3);
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.09d).Within(1e-12d));
            Assert.That(context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.20d).Within(1e-12d));
            Assert.That(result.DealtDamageMultiplier,
                Is.EqualTo(1.09d).Within(1e-12d));
            Assert.That(result.TargetTakenDamageMultiplier,
                Is.EqualTo(1.20d).Within(1e-12d));
            Assert.That(result.FinalDamage, Is.EqualTo(1308L));
        }

        [Test]
        public void AttackTypeRelicsApplyOnlyToTheExecutingCharacter()
        {
            var owner = new CharacterBattleState(
                "owner",
                0,
                ElementType.Fire,
                1000,
                1000d,
                permanentRelicEffects: new[]
                {
                    Source(
                        "tracker",
                        RelicEffectType.AttackTypeDamageIncrease,
                        0.12d,
                        AttackTypeMask.Additional | AttackTypeMask.Chase,
                        AttackTag.None,
                        AttackTag.FixedDamage)
                });
            var executor = new CharacterBattleState(
                "executor",
                1,
                ElementType.Fire,
                1000,
                1000d);
            var party = new PartyBattleState(new[] { owner, executor });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                100d);

            DamageContext context = BuildContext(
                executor,
                party,
                boss,
                AttackType.Chase,
                AttackTag.None);

            Assert.That(context.AttackTypeDamageIncreaseRateSum, Is.Zero);
            Assert.That(DamageCalculator.Calculate(context).FinalDamage,
                Is.EqualTo(1000L));
        }

        [Test]
        public void ResolverIgnoresRepeatedSlotsForOneRelicInstance()
        {
            DefinitionDatabase database = LoadDatabase();
            var character = Character("tracker", "tracker");
            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    Instance("tracker", TrackersChain, 5)
                }
            };

            RelicStatBonusSummary result = new RelicStatBonusResolver(
                database).Resolve(profile, character);

            Assert.That(result.Sources, Has.Count.EqualTo(1));
            Assert.That(result.Sources[0].Rate,
                Is.EqualTo(0.12d).Within(1e-12d));
        }

        private static void AssertContent(
            DefinitionDatabase database,
            LocalizationService localization,
            string relicId,
            RelicEffectType effectType,
            double[] rates,
            AttackTypeMask mask,
            AttackTag requiredTags,
            AttackTag excludedTags,
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
            Assert.That(relic.Effects, Has.Count.EqualTo(1));
            RelicEffectEntry effect = relic.Effects[0];
            Assert.That(effect.EffectType, Is.EqualTo(effectType));
            Assert.That(effect.EffectRates, Is.EqualTo(rates));
            Assert.That(effect.DescriptionValueKey, Is.EqualTo("rate"));
            Assert.That(effect.HasElementFilter, Is.False);
            Assert.That(effect.AttackTypeMask, Is.EqualTo(mask));
            Assert.That(effect.RequiredAttackTags, Is.EqualTo(requiredTags));
            Assert.That(effect.ExcludedAttackTags, Is.EqualTo(excludedTags));
            Assert.That(effect.ConditionType,
                Is.EqualTo(RelicEffectConditionType.None));

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

        private static DamageContext BuildProductionContext(
            string relicId,
            int awakening,
            AttackType attackType,
            AttackTag attackTags,
            double skillCoefficient = 1d)
        {
            RelicStatBonusSummary bonuses = Resolve(
                LoadDatabase(),
                relicId,
                awakening);
            return BuildContext(
                bonuses.Sources,
                attackType,
                attackTags,
                skillCoefficient);
        }

        private static DamageContext BuildContext(
            IReadOnlyList<RelicStatBonusSource> sources,
            AttackType attackType,
            AttackTag attackTags,
            double skillCoefficient = 1d)
        {
            CreateStates(
                sources,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            return BuildContext(
                attacker,
                party,
                boss,
                attackType,
                attackTags,
                skillCoefficient);
        }

        private static DamageContext BuildContext(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType,
            AttackTag attackTags,
            double skillCoefficient = 1d)
        {
            return new DamageContextFactory(
                new SeededRandomSource(1)).Build(
                    new DamageContextBuildRequest(
                        attacker,
                        party,
                        boss,
                        ElementType.Fire,
                        attackType,
                        attackTags,
                        skillCoefficient,
                        false,
                        0,
                        false));
        }

        private static void CreateStates(
            IReadOnlyList<RelicStatBonusSource> sources,
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Fire,
                1000,
                1000d,
                permanentRelicEffects: sources);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                100d);
        }

        private static RelicStatBonusSource Source(
            string id,
            RelicEffectType effectType,
            double rate,
            AttackTypeMask mask,
            AttackTag requiredTags,
            AttackTag excludedTags,
            bool hasElementFilter = false,
            ElementType elementFilter = default)
        {
            return new RelicStatBonusSource(
                id,
                id + ".name",
                id + ".description",
                effectType,
                5,
                rate,
                "rate",
                hasElementFilter,
                elementFilter,
                mask,
                requiredTags,
                excludedTags,
                RelicEffectConditionType.None,
                0d);
        }

        private static EffectInstance RuntimeEffect(
            long id,
            EffectModifierType type,
            double rate,
            AttackTypeMask mask = AttackTypeMask.None,
            AttackTag requiredTags = AttackTag.None)
        {
            return new EffectInstance(
                id,
                "runtime_" + id,
                "source",
                EffectCategory.Buff,
                type,
                rate,
                2,
                id,
                attackTypeMask: mask,
                requiredAttackTags: requiredTags);
        }

        private static RelicStatBonusSummary Resolve(
            DefinitionDatabase database,
            string relicId,
            int awakening)
        {
            var character = Character("relic");
            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    Instance("relic", relicId, awakening)
                }
            };
            return new RelicStatBonusResolver(database).Resolve(
                profile,
                character);
        }

        private static CharacterSaveData Character(params string[] slots)
        {
            List<string> equipped =
                SaveRules.CreateEmptyRelicInstanceIdSlots();
            for (int index = 0; index < slots.Length; index++)
            {
                equipped[index] = slots[index];
            }

            return new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                Awakening = 0,
                EquippedRelicInstanceIds = equipped
            };
        }

        private static RelicInstanceSaveData Instance(
            string instanceId,
            string relicId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = relicId,
                Awakening = awakening
            };
        }

        private static AttackTypeMask ExpectedMask(string relicId)
        {
            switch (relicId)
            {
                case PerfectBondSeal:
                    return AttackTypeMask.Match;
                case TrackersChain:
                    return AttackTypeMask.Additional
                        | AttackTypeMask.Chase;
                case OverlordsSeal:
                    return AttackTypeMask.Match
                        | AttackTypeMask.Active
                        | AttackTypeMask.Additional
                        | AttackTypeMask.Chase
                        | AttackTypeMask.DamageOverTime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(relicId));
            }
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

        private static TestCaseData Case(
            AttackType attackType,
            AttackTag attackTags,
            double expectedRate)
        {
            return new TestCaseData(attackType, attackTags, expectedRate);
        }
    }
}

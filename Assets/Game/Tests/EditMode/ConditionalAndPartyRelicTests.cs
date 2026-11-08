using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Application;
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
    public sealed class ConditionalAndPartyRelicTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private const string LastEmber = "relic_last_ember";
        private const string GuardianCrest =
            "relic_indomitable_guardian_crest";

        private static readonly double[] LastEmberRates =
            { 0.08d, 0.10d, 0.12d, 0.14d, 0.16d, 0.18d };
        private static readonly double[] GuardianRates =
            { 0.015d, 0.02d, 0.025d, 0.03d, 0.035d, 0.04d };

        [Test]
        public void ProductionContentRegistersAllTwentyRelics()
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.Relics, Has.Count.EqualTo(20));
            Assert.That(database.RelicPresentations, Has.Count.EqualTo(20));
            AssertRelicContent(
                database,
                LastEmber,
                RelicEffectType.DealtDamageIncrease,
                LastEmberRates,
                RelicEffectConditionType.PartyHpAtOrBelow,
                0.5d,
                AttackTypeMask.Match
                    | AttackTypeMask.Active
                    | AttackTypeMask.Additional
                    | AttackTypeMask.Chase
                    | AttackTypeMask.DamageOverTime,
                AttackTag.FixedDamage);
            AssertRelicContent(
                database,
                GuardianCrest,
                RelicEffectType.PartyDamageReduction,
                GuardianRates,
                RelicEffectConditionType.None,
                0d,
                AttackTypeMask.None,
                AttackTag.None);
        }

        [Test]
        public void ProductionLocalizationFormatsConditionalAndDecimalRates()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationService localization = LoadLocalization();
            Assert.That(database.TryGetRelic(
                LastEmber,
                out RelicDefinition ember), Is.True);
            Assert.That(database.TryGetRelic(
                GuardianCrest,
                out RelicDefinition guardian), Is.True);

            localization.SetLocale("en");
            Assert.That(localization.GetText(ember.DisplayNameKey),
                Is.EqualTo("Last Ember"));
            Assert.That(FormatDescription(localization, ember, 0),
                Is.EqualTo("When the party's current HP is 50% or lower, "
                    + "increases damage dealt by the equipped character by "
                    + "8%. Does not apply to fixed damage."));
            Assert.That(FormatDescription(localization, ember, 5),
                Does.Contain("18%"));
            Assert.That(localization.GetText(guardian.DisplayNameKey),
                Is.EqualTo("Indomitable Guardian Crest"));
            Assert.That(FormatDescription(localization, guardian, 0),
                Is.EqualTo("Reduces damage taken by the party from boss "
                    + "attacks by 1.5%."));
            Assert.That(FormatDescription(localization, guardian, 5),
                Does.EndWith("4%."));

            localization.SetLocale("ko");
            Assert.That(localization.GetText(ember.DisplayNameKey),
                Is.EqualTo("최후의 불씨"));
            Assert.That(FormatDescription(localization, ember, 0),
                Does.Contain("50% 이하"));
            Assert.That(localization.GetText(guardian.DisplayNameKey),
                Is.EqualTo("불굴의 수호문장"));
            Assert.That(FormatDescription(localization, guardian, 0),
                Does.Contain("1.5%"));
        }

        [TestCase(10000, 0d)]
        [TestCase(5001, 0d)]
        [TestCase(5000, 0.18d)]
        [TestCase(4999, 0.18d)]
        [TestCase(1000, 0.18d)]
        public void LastEmberUsesInclusiveFiftyPercentBoundary(
            long currentHp,
            double expectedRate)
        {
            CreateDamageStates(
                10000,
                ProductionSources(LastEmber, 5),
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, currentHp);

            DamageContext context = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Match,
                AttackTag.Match3);

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
        }

        [TestCase(5000, 0d)]
        [TestCase(4999, 0.18d)]
        public void LastEmberHandlesOddMaximumHpWithoutBoundaryDrift(
            long currentHp,
            double expectedRate)
        {
            CreateDamageStates(
                9999,
                ProductionSources(LastEmber, 5),
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, currentHp);

            DamageContext context = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Active,
                AttackTag.None);

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(expectedRate).Within(1e-12d));
        }

        [Test]
        public void LastEmberReevaluatesBetweenActionsAndNotWithinContext()
        {
            CreateDamageStates(
                10000,
                ProductionSources(LastEmber, 5),
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, 6000);
            DamageContext first = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Match,
                AttackTag.Match3);

            ReduceHpTo(party, 4000);
            DamageContext second = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Additional,
                AttackTag.None);
            PartyHealingApplier.Apply(
                party,
                Healing(3000));
            DamageResult alreadyStarted = DamageCalculator.Calculate(second);
            DamageContext third = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Chase,
                AttackTag.None);

            Assert.That(first.DealtDamageIncreaseRateSum, Is.Zero);
            Assert.That(second.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.18d).Within(1e-12d));
            Assert.That(alreadyStarted.FinalDamage, Is.EqualTo(1180L));
            Assert.That(party.CurrentHp, Is.EqualTo(7000));
            Assert.That(third.DealtDamageIncreaseRateSum, Is.Zero);
        }

        [TestCase(AttackType.Match, AttackTag.Match3, 1180L)]
        [TestCase(AttackType.Match, AttackTag.Match4, 1180L)]
        [TestCase(AttackType.Match, AttackTag.Match5Plus, 1180L)]
        [TestCase(AttackType.Active, AttackTag.None, 1180L)]
        [TestCase(AttackType.Additional, AttackTag.None, 1180L)]
        [TestCase(AttackType.Chase, AttackTag.None, 1180L)]
        [TestCase(AttackType.DamageOverTime, AttackTag.None, 1180L)]
        [TestCase(AttackType.Passive, AttackTag.None, 1000L)]
        [TestCase(AttackType.Active, AttackTag.FixedDamage, 1000L)]
        public void LastEmberUsesEstablishedDealtDamageAttackScope(
            AttackType attackType,
            AttackTag tags,
            long expectedDamage)
        {
            CreateDamageStates(
                10000,
                ProductionSources(LastEmber, 5),
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, 5000);

            DamageResult result = DamageCalculator.Calculate(
                BuildDamageContext(
                    attacker,
                    party,
                    boss,
                    attackType,
                    tags));

            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [TestCase(0, 1080L)]
        [TestCase(5, 1180L)]
        public void LastEmberAwakeningRateFlowsToFinalIntegerDamage(
            int awakening,
            long expectedDamage)
        {
            CreateDamageStates(
                10000,
                ProductionSources(LastEmber, awakening),
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, 5000);

            DamageResult result = DamageCalculator.Calculate(
                BuildDamageContext(
                    attacker,
                    party,
                    boss,
                    AttackType.Active,
                    AttackTag.None));

            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [Test]
        public void LastEmberAddsWithinDealtDamageAndMultipliesOtherCategories()
        {
            var sources = new List<RelicStatBonusSource>(
                ProductionSources(LastEmber, 5))
            {
                Source(
                    "element",
                    RelicEffectType.ElementDamageIncrease,
                    0.14d),
                Source(
                    "attack_type",
                    RelicEffectType.AttackTypeDamageIncrease,
                    0.12d,
                    AttackTypeMask.Match)
            };
            CreateDamageStates(
                10000,
                sources,
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            ReduceHpTo(party, 5000);
            attacker.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.DealtDamageIncrease,
                0.20d));

            DamageContext context = BuildDamageContext(
                attacker,
                party,
                boss,
                AttackType.Match,
                AttackTag.Match3);
            DamageResult result = DamageCalculator.Calculate(context);
            double expectedRaw = 1000d * 1.14d * 1.12d * 1.38d;

            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.38d).Within(1e-12d));
            Assert.That(context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.14d).Within(1e-12d));
            Assert.That(context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.12d).Within(1e-12d));
            Assert.That(result.FinalDamage,
                Is.EqualTo((long)Math.Floor(expectedRaw)));
        }

        [Test]
        public void LastEmberAppliesOnlyToItsEquippedCharacter()
        {
            var owner = Character(
                "owner",
                0,
                5000,
                ProductionSources(LastEmber, 5));
            var executor = Character("executor", 1, 5000);
            var party = new PartyBattleState(new[] { owner, executor });
            ReduceHpTo(party, 5000);
            var boss = Boss();

            DamageContext ownerContext = BuildDamageContext(
                owner,
                party,
                boss,
                AttackType.Active,
                AttackTag.None);
            DamageContext executorContext = BuildDamageContext(
                executor,
                party,
                boss,
                AttackType.Active,
                AttackTag.None);

            Assert.That(ownerContext.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.18d).Within(1e-12d));
            Assert.That(executorContext.DealtDamageIncreaseRateSum, Is.Zero);
        }

        [TestCase(0, 0.015d)]
        [TestCase(5, 0.04d)]
        public void GuardianCrestFlowsFromResolverToParty(
            int awakening,
            double expectedRate)
        {
            IReadOnlyList<RelicStatBonusSource> sources =
                ProductionSources(GuardianCrest, awakening);
            var character = Character("guardian", 0, 10000, sources);
            var party = new PartyBattleState(new[] { character });

            Assert.That(sources, Has.Count.EqualTo(1));
            Assert.That(sources[0].Rate,
                Is.EqualTo(expectedRate).Within(1e-12d));
            Assert.That(party.PermanentPartyDamageReductionRate,
                Is.EqualTo(expectedRate).Within(1e-12d));
        }

        [Test]
        public void GuardianCrestsSumAcrossEquippedPartyMembers()
        {
            var characters = new List<CharacterBattleState>();
            for (int slot = 0; slot < 5; slot++)
            {
                characters.Add(Character(
                    "guardian_" + slot,
                    slot,
                    2000,
                    ProductionSources(GuardianCrest, 5)));
            }

            var party = new PartyBattleState(characters);
            BossDamageResult result = CalculateBossDamage(party, 1000d);

            Assert.That(party.PermanentPartyDamageReductionRate,
                Is.EqualTo(0.20d).Within(1e-12d));
            Assert.That(result.FinalDamageBeforeShield, Is.EqualTo(800L));
        }

        [Test]
        public void GuardianCrestsSumMixedAwakeningsAndSkipUnequippedMembers()
        {
            var party = new PartyBattleState(new[]
            {
                Character(
                    "basic",
                    0,
                    2500,
                    ProductionSources(GuardianCrest, 0)),
                Character(
                    "second",
                    1,
                    2500,
                    ProductionSources(GuardianCrest, 2)),
                Character("unequipped", 2, 2500),
                Character(
                    "maximum",
                    3,
                    2500,
                    ProductionSources(GuardianCrest, 5))
            });

            Assert.That(party.PermanentPartyDamageReductionRate,
                Is.EqualTo(0.08d).Within(1e-12d));
            Assert.That(CalculateBossDamage(party, 1000d)
                .FinalDamageBeforeShield, Is.EqualTo(920L));
        }

        [Test]
        public void GuardianCrestAndRuntimeReductionAddAndRespectCap()
        {
            var character = Character(
                "guardian",
                0,
                10000,
                Source(
                    GuardianCrest,
                    RelicEffectType.PartyDamageReduction,
                    0.20d));
            var party = new PartyBattleState(new[] { character });
            party.Effects.ApplyEffect(Effect(
                1,
                EffectModifierType.PartyDamageReduction,
                0.15d,
                turns: 1));

            BossDamageResult combined = CalculateBossDamage(party, 1000d);
            party.Effects.ProcessTurnEnd();
            BossDamageResult permanentOnly = CalculateBossDamage(
                party,
                1000d);
            party.Effects.ApplyEffect(Effect(
                2,
                EffectModifierType.PartyDamageReduction,
                0.60d));
            BossDamageResult capped = CalculateBossDamage(party, 1000d);

            Assert.That(combined.FinalDamageBeforeShield, Is.EqualTo(650L));
            Assert.That(permanentOnly.FinalDamageBeforeShield,
                Is.EqualTo(800L));
            Assert.That(capped.EffectivePartyDamageReductionRate,
                Is.EqualTo(0.70d).Within(1e-12d));
            Assert.That(capped.FinalDamageBeforeShield, Is.EqualTo(300L));
        }

        [TestCase(1000, 800, 0, 200)]
        [TestCase(500, 500, 300, 0)]
        public void GuardianReductionRunsBeforeShieldAndHpApplication(
            long shieldAmount,
            long expectedAbsorbed,
            long expectedHpDamage,
            long expectedShieldAfter)
        {
            var character = Character(
                "guardian",
                0,
                10000,
                Source(
                    GuardianCrest,
                    RelicEffectType.PartyDamageReduction,
                    0.20d));
            var party = new PartyBattleState(new[] { character });
            party.Shields.Add(new ShieldInstance(
                1,
                "test_shield",
                shieldAmount,
                1,
                null,
                1));

            PartyDamageApplicationResult result = PartyDamageApplier.Apply(
                party,
                CalculateBossDamage(party, 1000d));

            Assert.That(result.RequestedDamage, Is.EqualTo(800L));
            Assert.That(result.ShieldAbsorbedDamage,
                Is.EqualTo(expectedAbsorbed));
            Assert.That(result.HpDamage, Is.EqualTo(expectedHpDamage));
            Assert.That(party.CurrentHp,
                Is.EqualTo(10000L - expectedHpDamage));
            Assert.That(party.Shields.TotalShield,
                Is.EqualTo(expectedShieldAfter));
        }

        [Test]
        public void GuardianReductionDoesNotChangeHpCosts()
        {
            var character = Character(
                "guardian",
                0,
                10000,
                Source(
                    GuardianCrest,
                    RelicEffectType.PartyDamageReduction,
                    0.20d));
            var party = new PartyBattleState(new[] { character });

            PartyHpCostApplicationResult result = PartyHpCostApplier.Apply(
                party,
                party.MaxHp,
                0.10d);

            Assert.That(result.AppliedHpCost, Is.EqualTo(1000L));
            Assert.That(party.CurrentHp, Is.EqualTo(9000L));
        }

        [Test]
        public void ResolverDoesNotCountRepeatedSlotsForOneGuardianInstance()
        {
            CharacterSaveData character = SaveCharacter(
                "guardian_instance",
                "guardian_instance");
            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    new RelicInstanceSaveData
                    {
                        InstanceId = "guardian_instance",
                        RelicDefinitionId = GuardianCrest,
                        Awakening = 5
                    }
                }
            };

            RelicStatBonusSummary result = new RelicStatBonusResolver(
                LoadDatabase()).Resolve(profile, character);

            Assert.That(result.Sources, Has.Count.EqualTo(1));
            Assert.That(result.Sources[0].Rate,
                Is.EqualTo(0.04d).Within(1e-12d));
        }

        private static void AssertRelicContent(
            DefinitionDatabase database,
            string id,
            RelicEffectType effectType,
            double[] rates,
            RelicEffectConditionType conditionType,
            double conditionThreshold,
            AttackTypeMask attackTypeMask,
            AttackTag excludedTags)
        {
            Assert.That(database.TryGetRelic(
                id,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.Effects, Has.Count.EqualTo(1));
            RelicEffectEntry effect = relic.Effects[0];
            Assert.That(effect.EffectType, Is.EqualTo(effectType));
            Assert.That(effect.EffectRates, Is.EqualTo(rates));
            Assert.That(effect.DescriptionValueKey, Is.EqualTo("rate"));
            Assert.That(effect.AttackTypeMask, Is.EqualTo(attackTypeMask));
            Assert.That(effect.ExcludedAttackTags,
                Is.EqualTo(excludedTags));
            Assert.That(effect.ConditionType, Is.EqualTo(conditionType));
            Assert.That(effect.ConditionThreshold,
                Is.EqualTo(conditionThreshold).Within(1e-12d));
            Assert.That(database.TryGetRelicPresentation(
                id,
                out RelicPresentationDefinition presentation), Is.True);
            Assert.That(presentation.RelicId, Is.EqualTo(id));
            Assert.That(
                presentation.Icon == null
                    || !string.IsNullOrEmpty(
                        AssetDatabase.GetAssetPath(presentation.Icon)),
                Is.True,
                "An assigned relic icon must be a persisted Sprite asset.");
        }

        private static IReadOnlyList<RelicStatBonusSource>
            ProductionSources(string relicId, int awakening)
        {
            CharacterSaveData character = SaveCharacter("instance");
            var profile = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { character },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    new RelicInstanceSaveData
                    {
                        InstanceId = "instance",
                        RelicDefinitionId = relicId,
                        Awakening = awakening
                    }
                }
            };
            return new RelicStatBonusResolver(LoadDatabase())
                .Resolve(profile, character)
                .Sources;
        }

        private static CharacterSaveData SaveCharacter(
            params string[] slots)
        {
            List<string> equipped =
                SaveRules.CreateEmptyRelicInstanceIdSlots();
            for (int index = 0; index < slots.Length; index++)
            {
                equipped[index] = slots[index];
            }

            return new CharacterSaveData
            {
                CharacterId = "character",
                Level = 1,
                Awakening = 0,
                EquippedRelicInstanceIds = equipped
            };
        }

        private static RelicStatBonusSource Source(
            string id,
            RelicEffectType effectType,
            double rate,
            AttackTypeMask attackTypeMask = AttackTypeMask.None)
        {
            return new RelicStatBonusSource(
                id,
                id + ".name",
                id + ".description",
                effectType,
                5,
                rate,
                "rate",
                false,
                default,
                attackTypeMask,
                AttackTag.None,
                AttackTag.None,
                RelicEffectConditionType.None,
                0d);
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            long maxHp,
            IReadOnlyList<RelicStatBonusSource> sources = null)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Fire,
                maxHp,
                1000d,
                permanentRelicEffects: sources);
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            long maxHp,
            RelicStatBonusSource source)
        {
            return Character(
                id,
                slot,
                maxHp,
                new[] { source });
        }

        private static void CreateDamageStates(
            long maxHp,
            IReadOnlyList<RelicStatBonusSource> sources,
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = Character("attacker", 0, maxHp, sources);
            party = new PartyBattleState(new[] { attacker });
            boss = Boss();
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                1000d);
        }

        private static DamageContext BuildDamageContext(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType,
            AttackTag tags)
        {
            return new DamageContextFactory(
                new SeededRandomSource(1)).Build(
                    new DamageContextBuildRequest(
                        attacker,
                        party,
                        boss,
                        ElementType.Fire,
                        attackType,
                        tags,
                        1d,
                        false,
                        0,
                        false));
        }

        private static void ReduceHpTo(
            PartyBattleState party,
            long targetHp)
        {
            Assert.That(targetHp, Is.InRange(1L, party.CurrentHp));
            long damage = party.CurrentHp - targetHp;
            if (damage > 0)
            {
                PartyDamageApplier.Apply(party, Damage(damage));
            }
        }

        private static BossDamageResult Damage(long amount)
        {
            return BossDamageCalculator.Calculate(new BossDamageContext(
                amount,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                0d));
        }

        private static ValorChronicle.Battle.Combat.Healing.HealingResult
            Healing(long amount)
        {
            return ValorChronicle.Battle.Combat.Healing.HealingCalculator
                .Calculate(
                    new ValorChronicle.Battle.Combat.Healing.HealingContext(
                        amount,
                        1d,
                        false,
                        0,
                        0d));
        }

        private static BossDamageResult CalculateBossDamage(
            PartyBattleState party,
            double baseDamage)
        {
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100000,
                baseDamage);
            BossDamageContext context = BossDamageContextFactory.Build(
                new BossDamageContextBuildRequest(
                    boss,
                    party,
                    1d,
                    AttackTag.None));
            return BossDamageCalculator.Calculate(context);
        }

        private static EffectInstance Effect(
            long id,
            EffectModifierType modifierType,
            double magnitude,
            int? turns = 2)
        {
            return new EffectInstance(
                id,
                "effect_" + id,
                "source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                turns,
                id);
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
    }
}

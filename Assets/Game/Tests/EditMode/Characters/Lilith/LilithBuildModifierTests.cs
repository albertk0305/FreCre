using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithBuildModifierTests
    {
        private CharacterDefinition definition;
        private LilithCombatConfig combatConfig;
        private CharacterBuildResolver buildResolver;

        [SetUp]
        public void SetUp()
        {
            definition = CreateDefinition();
            combatConfig = LilithTestConfig.Create();
            buildResolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new LilithBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(combatConfig);
        }

        [Test]
        public void ModifierUsesLilithCharacterId()
        {
            Assert.That(new LilithBuildModifier().CharacterId,
                Is.EqualTo(LilithRules.CharacterId));
        }

        [TestCase(0, 1.00d, 1.00d)]
        [TestCase(1, 1.00d, 1.05d)]
        [TestCase(2, 1.05d, 1.05d)]
        [TestCase(3, 1.05d, 1.05d)]
        [TestCase(4, 1.05d, 1.05d)]
        [TestCase(5, 1.05d, 1.05d)]
        [TestCase(6, 1.05d, 1.05d)]
        public void ModifierSeparatesPermanentStatAwakenings(
            int awakening,
            double expectedHpMultiplier,
            double expectedAttackMultiplier)
        {
            CharacterBuildStatMultipliers result =
                new LilithBuildModifier()
                    .GetStatMultipliers(awakening);

            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(expectedHpMultiplier));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(expectedAttackMultiplier));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            Assert.That(
                () => new LilithBuildModifier()
                    .GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        [TestCase(1, 0, 1000L, 150L)]
        [TestCase(1, 1, 1000L, 158L)]
        [TestCase(1, 2, 1050L, 158L)]
        [TestCase(50, 0, 2435L, 482L)]
        [TestCase(50, 2, 2557L, 506L)]
        [TestCase(100, 0, 3900L, 820L)]
        [TestCase(100, 1, 3900L, 861L)]
        [TestCase(100, 2, 4095L, 861L)]
        [TestCase(100, 3, 4095L, 861L)]
        [TestCase(100, 4, 4095L, 861L)]
        [TestCase(100, 5, 4095L, 861L)]
        [TestCase(100, 6, 4095L, 861L)]
        public void BuildResolverReturnsExpectedLevelAndAwakeningStats(
            int level,
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = buildResolver.Resolve(
                definition,
                level,
                awakening);

            Assert.That(result.CharacterId,
                Is.EqualTo(LilithRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [Test]
        public void HalfStatUsesCommonAwayFromZeroRounding()
        {
            ResolvedCharacterBuild result = buildResolver.Resolve(
                definition,
                level: 1,
                awakening: 1);

            Assert.That(150d * 1.05d, Is.EqualTo(157.5d));
            Assert.That(result.Attack, Is.EqualTo(158L));
        }

        [Test]
        public void BuildResolutionIsDeterministic()
        {
            ResolvedCharacterBuild first = buildResolver.Resolve(
                definition,
                level: 50,
                awakening: 6);
            ResolvedCharacterBuild second = buildResolver.Resolve(
                definition,
                level: 50,
                awakening: 6);

            Assert.That(second.MaxHp, Is.EqualTo(first.MaxHp));
            Assert.That(second.Attack, Is.EqualTo(first.Attack));
            Assert.That(second.MaxHpMultiplier,
                Is.EqualTo(first.MaxHpMultiplier));
            Assert.That(second.AttackMultiplier,
                Is.EqualTo(first.AttackMultiplier));
        }

        [TestCase(
            0, 1d, 1d, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(
            1, 1d, 1.05d, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(
            2, 1.05d, 1.05d, 0.70d, 1.20d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(
            3, 1.05d, 1.05d, 0.75d, 1.30d, 1.90d, 0.20d, 3.00d, 0.015d)]
        [TestCase(
            4, 1.05d, 1.05d, 0.75d, 1.30d, 2.10d, 0.25d, 3.00d, 0.015d)]
        [TestCase(
            5, 1.05d, 1.05d, 0.75d, 1.30d, 2.10d, 0.25d, 3.30d, 0.015d)]
        [TestCase(
            6, 1.05d, 1.05d, 0.75d, 1.30d, 2.10d, 0.25d, 3.30d, 0.018d)]
        public void BuildAndCombatAwakeningThresholdsStaySeparated(
            int awakening,
            double hpMultiplier,
            double attackMultiplier,
            double match3,
            double match4,
            double match5,
            double blessing,
            double explosion,
            double covenant)
        {
            CharacterBuildStatMultipliers build =
                new LilithBuildModifier()
                    .GetStatMultipliers(awakening);
            LilithResolvedCombatRules combat =
                LilithCombatRuleResolver.Resolve(
                    combatConfig,
                    awakening);

            Assert.That(build.MaxHpMultiplier, Is.EqualTo(hpMultiplier));
            Assert.That(build.AttackMultiplier,
                Is.EqualTo(attackMultiplier));
            Assert.That(combat.Match3Coefficient, Is.EqualTo(match3));
            Assert.That(combat.Match4Coefficient, Is.EqualTo(match4));
            Assert.That(combat.Match5Coefficient, Is.EqualTo(match5));
            Assert.That(combat.CorruptedBlessingDamageReductionRate,
                Is.EqualTo(blessing));
            Assert.That(combat.ContractExplosionCoefficientPerUnit,
                Is.EqualTo(explosion));
            Assert.That(combat.ContractDamageIncreaseRatePerStack,
                Is.EqualTo(covenant));
            Assert.That(combat.ContractMaxStackCount, Is.EqualTo(12));
        }

        [TestCase(BoardMatchTier.Three, 0.70d)]
        [TestCase(BoardMatchTier.Four, 1.20d)]
        [TestCase(BoardMatchTier.FiveOrMore, 1.90d)]
        public void AwakeningOneResolvedAttackFlowsIntoEveryMatchTier(
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            ResolvedCharacterBuild build = buildResolver.Resolve(
                definition,
                level: 100,
                awakening: 1);
            TestCombatState state = CreateCombatState(build);
            var provider = new LilithMatchActionProvider(
                combatConfig,
                awakening: 1);
            IReadOnlyList<CombatAction> actions = provider.CreateActions(
                state.Character,
                state.Party,
                state.Boss,
                tier,
                finalComboCount: 1,
                state.ActionIds);

            DamageActionResult damage = Execute(state, actions)
                .ActionResults.OfType<DamageActionResult>().Single();

            Assert.That(damage.Context.BaseAttack, Is.EqualTo(861d));
            Assert.That(damage.Context.SkillCoefficient,
                Is.EqualTo(expectedCoefficient));
            Assert.That(damage.DamageResult.FinalAttack,
                Is.EqualTo(861d));
        }

        [TestCase(0, 820d)]
        [TestCase(1, 861d)]
        [TestCase(2, 861d)]
        public void ResolvedAttackFlowsIntoPassiveWithoutCoefficientDuplication(
            int awakening,
            double expectedAttack)
        {
            ResolvedCharacterBuild build = buildResolver.Resolve(
                definition,
                level: 100,
                awakening: awakening);
            TestCombatState state = CreateCombatState(build);
            LilithResolvedCombatRules rules =
                LilithCombatRuleResolver.Resolve(
                    combatConfig,
                    awakening);
            AddCovenant(state.Boss, rules, count: 4);
            var provider = new LilithPostBossActionProvider(
                combatConfig,
                awakening);

            DamageActionResult damage = Execute(
                    state,
                    provider.CreateActions(
                        state.Character,
                        state.Party,
                        state.Boss,
                        state.ActionIds))
                .ActionResults.OfType<DamageActionResult>().Single();

            Assert.That(damage.Context.BaseAttack,
                Is.EqualTo(expectedAttack));
            Assert.That(damage.Context.SkillCoefficient,
                Is.EqualTo(3.00d));
        }

        [Test]
        public void AwakeningTwoHpJoinsPartyAndActiveStillCostsCurrentHp()
        {
            ResolvedCharacterBuild build = buildResolver.Resolve(
                definition,
                level: 100,
                awakening: 2);
            var lilith = new CharacterBattleState(
                LilithRules.CharacterId,
                0,
                ElementType.Dark,
                build.MaxHp,
                build.Attack);
            var ally = new CharacterBattleState(
                "character_ally",
                1,
                ElementType.Fire,
                1000L,
                100d);
            var party = new PartyBattleState(new[] { lilith, ally });
            var boss = new BossBattleState(
                "training_boss",
                ElementType.Dark,
                1000000L,
                100d);
            var ids = new CombatActionIdSequence();
            var provider = new LilithActiveAbilityActionProvider(
                combatConfig,
                awakening: 2);
            SetPartyHp(party, 2000L);

            CombatActionExecutionResult result = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(123)))
                .Execute(new CombatActionQueue(provider.CreateActions(
                    lilith,
                    party,
                    boss,
                    ids)));
            var cost = (PartyHpCostActionResult)result.ActionResults[0];

            Assert.That(build.MaxHp, Is.EqualTo(4095L));
            Assert.That(party.MaxHp, Is.EqualTo(5095L));
            Assert.That(cost.HpBefore, Is.EqualTo(2000L));
            Assert.That(cost.RequestedHpCost, Is.EqualTo(300L));
            Assert.That(cost.AppliedHpCost, Is.EqualTo(300L));
            Assert.That(party.CurrentHp, Is.EqualTo(1700L));
            Assert.That(LilithCovenantEffectFactory.GetStackCount(boss),
                Is.EqualTo(2));
        }

        private static CharacterDefinition CreateDefinition()
        {
            CharacterDefinition result =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(
                result,
                typeof(GameDefinition),
                "id",
                LilithRules.CharacterId);
            SetField(result, typeof(CharacterDefinition), "level1Hp", 1000);
            SetField(result, typeof(CharacterDefinition), "level1Attack", 150);
            SetField(result, typeof(CharacterDefinition), "level100Hp", 3900);
            SetField(result, typeof(CharacterDefinition), "level100Attack", 820);
            return result;
        }

        private static TestCombatState CreateCombatState(
            ResolvedCharacterBuild build)
        {
            var character = new CharacterBattleState(
                LilithRules.CharacterId,
                0,
                ElementType.Dark,
                build.MaxHp,
                build.Attack);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "training_boss",
                ElementType.Dark,
                100000000L,
                100d);
            return new TestCombatState(
                character,
                party,
                boss,
                new CombatActionIdSequence());
        }

        private static CombatActionExecutionResult Execute(
            TestCombatState state,
            IReadOnlyList<CombatAction> actions)
        {
            return new CombatActionExecutor(
                    state.Boss,
                    state.Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)))
                .Execute(new CombatActionQueue(actions));
        }

        private static void AddCovenant(
            BossBattleState boss,
            LilithResolvedCombatRules rules,
            int count)
        {
            for (int index = 0; index < count; index++)
            {
                long id = 1000L + index;
                boss.Effects.ApplyEffect(
                    LilithCovenantEffectFactory.Create(id, rules));
            }
        }

        private static void SetPartyHp(PartyBattleState party, long hp)
        {
            long damage = party.CurrentHp - hp;
            PartyDamageApplier.Apply(
                party,
                BossDamageCalculator.Calculate(new BossDamageContext(
                    damage,
                    0d,
                    0d,
                    1d,
                    0d,
                    0d,
                    0d,
                    0d)));
        }

        private static void SetField(
            CharacterDefinition target,
            Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class TestCombatState
        {
            public TestCombatState(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds)
            {
                Character = character;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}

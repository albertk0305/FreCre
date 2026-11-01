using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneLifestealRuleTests
    {
        private BeatriceBloodstoneCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = BeatriceBloodstoneTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(ActionOrigin.Match, AttackType.Match, AttackTag.Match5Plus,
            1000L, 100L)]
        [TestCase(ActionOrigin.Match, AttackType.Match, AttackTag.Match5Plus,
            500L, 200L)]
        [TestCase(ActionOrigin.Match, AttackType.Match, AttackTag.Match5Plus,
            1L, 299L)]
        [TestCase(ActionOrigin.Active, AttackType.Active, AttackTag.None,
            1000L, 150L)]
        [TestCase(ActionOrigin.Active, AttackType.Active, AttackTag.None,
            500L, 250L)]
        [TestCase(ActionOrigin.Active, AttackType.Active, AttackTag.None,
            1L, 349L)]
        public void EligibleDamageCreatesStandardNonComboHealing(
            ActionOrigin origin,
            AttackType attackType,
            AttackTag tags,
            long initialHp,
            long expectedFinalHealing)
        {
            TestBattle battle = CreateBattle(initialHp: initialHp);
            DamageAction damage = battle.CreateDamage(
                origin,
                attackType,
                tags,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Execute(damage);

            Assert.That(result.ActionResults.Count, Is.EqualTo(2));
            var damageResult = result.ActionResults[0] as DamageActionResult;
            var healResult = result.ActionResults[1] as HealActionResult;
            Assert.That(damageResult, Is.Not.Null);
            Assert.That(healResult, Is.Not.Null);
            Assert.That(damageResult.AppliedDamage, Is.EqualTo(1000L));
            Assert.That(healResult.FinalHealing,
                Is.EqualTo(expectedFinalHealing));
            Assert.That(healResult.AppliedHealing,
                Is.EqualTo(Math.Min(
                    expectedFinalHealing,
                    1000L - initialHp)));
            Assert.That(battle.Party.CurrentHp, Is.LessThanOrEqualTo(1000L));
            Assert.That(healResult.Context.AppliesCombo, Is.False);
            Assert.That(healResult.HealingResult.ComboMultiplier,
                Is.EqualTo(1d));
            Assert.That(healResult.Action.RootActionId,
                Is.EqualTo(damage.ActionId));
            Assert.That(healResult.Action.SourceActionId,
                Is.EqualTo(damage.ActionId));
        }

        [Test]
        public void LethalDamageUsesExistingTerminationAndSkipsHealing()
        {
            TestBattle battle = CreateBattle(
                initialHp: 950L,
                bossHp: 40L);
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Active,
                AttackType.Active,
                AttackTag.None,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Execute(damage);

            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.ActionResults.Count, Is.EqualTo(1),
                "A lethal hit terminates before trigger-derived healing.");
            Assert.That(result.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
        }

        [Test]
        public void HealingIncreaseIsCollectedExactlyOnceAndFloorIsUsed()
        {
            TestBattle battle = CreateBattle(initialHp: 500L);
            battle.Party.Effects.ApplyEffect(new EffectInstance(
                1L,
                "test_healing_increase",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.HealingIncrease,
                0.10d,
                2,
                1L));
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Active,
                AttackType.Active,
                AttackTag.None,
                coefficient: 0.333d);

            CombatActionExecutionResult result = battle.Execute(damage);
            var heal = (HealActionResult)result.ActionResults[1];

            Assert.That(((DamageActionResult)result.ActionResults[0])
                .AppliedDamage, Is.EqualTo(333L));
            Assert.That(heal.Context.HealingIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(heal.FinalHealing, Is.EqualTo(91L));
        }

        [TestCase(ActionOrigin.Match, AttackType.Match, AttackTag.None)]
        [TestCase(ActionOrigin.Match, AttackType.Match, AttackTag.Match4)]
        [TestCase(ActionOrigin.Additional, AttackType.Additional,
            AttackTag.Match5Plus)]
        [TestCase(ActionOrigin.Chase, AttackType.Chase, AttackTag.None)]
        [TestCase(ActionOrigin.DamageOverTime, AttackType.DamageOverTime,
            AttackTag.None)]
        public void IneligibleDamageDoesNotCreateHealing(
            ActionOrigin origin,
            AttackType attackType,
            AttackTag tags)
        {
            TestBattle battle = CreateBattle(initialHp: 500L);
            IReadOnlyList<DamageAction> actions = battle.CreateDamageSequence(
                origin,
                attackType,
                tags,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Execute(actions);

            Assert.That(result.ActionResults.Count,
                Is.EqualTo(actions.Count));
            if (actions.Count == 2)
            {
                Assert.That(actions[1].RootActionId,
                    Is.EqualTo(actions[0].ActionId));
                Assert.That(actions[1].SourceActionId,
                    Is.EqualTo(actions[0].ActionId));
            }

            Assert.That(result.ActionResults.OfType<HealActionResult>(),
                Is.Empty);
        }

        [Test]
        public void ZeroAppliedDamageCreatesNoHealing()
        {
            TestBattle battle = CreateBattle(initialHp: 500L, attack: 0d);
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Active,
                AttackType.Active,
                AttackTag.None,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Execute(damage);

            Assert.That(((DamageActionResult)result.ActionResults[0])
                .AppliedDamage, Is.Zero);
            Assert.That(result.ActionResults.Count, Is.EqualTo(1));
        }

        [Test]
        public void SameCompletedDamageCannotCreateHealingTwice()
        {
            TestBattle battle = CreateBattle(initialHp: 500L);
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Active,
                AttackType.Active,
                AttackTag.None,
                coefficient: 1d);
            CombatActionExecutor executor = battle.CreateExecutor(
                Array.Empty<ICombatTriggerRule>());
            DamageActionResult completed = (DamageActionResult)executor
                .Execute(new CombatActionQueue(
                    new CombatAction[] { damage }))
                .ActionResults[0];
            var history = new CombatActionExecutionHistory(
                new CombatActionResult[] { completed });
            var context = new CombatActionTriggerContext(
                completed,
                battle.Boss,
                battle.Party,
                history);

            IReadOnlyList<CombatAction> first = battle.Rule
                .CreateDerivedActions(context);
            IReadOnlyList<CombatAction> second = battle.Rule
                .CreateDerivedActions(context);

            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(second, Is.Empty);
        }

        [Test]
        public void HpChangedAfterActionCreationDeterminesLifestealRate()
        {
            TestBattle battle = CreateBattle(initialHp: 1000L);
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Active,
                AttackType.Active,
                AttackTag.None,
                coefficient: 1d);
            SetHp(battle.Party, 500L);

            CombatActionExecutionResult result = battle.Execute(damage);

            Assert.That(((HealActionResult)result.ActionResults[1])
                .FinalHealing, Is.EqualTo(250L));
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(6)]
        public void AwakeningDoesNotChangeLifestealRate(int awakening)
        {
            TestBattle battle = CreateBattle(
                initialHp: 500L,
                awakening: awakening);
            DamageAction damage = battle.CreateDamage(
                ActionOrigin.Match,
                AttackType.Match,
                AttackTag.Match5Plus,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Execute(damage);

            Assert.That(((HealActionResult)result.ActionResults[1])
                .FinalHealing, Is.EqualTo(200L));
        }

        private TestBattle CreateBattle(
            long initialHp,
            long bossHp = 100000000L,
            double attack = 1000d,
            int awakening = 0)
        {
            var character = new CharacterBattleState(
                BeatriceBloodstoneRules.CharacterId,
                0,
                ElementType.Dark,
                1000L,
                attack);
            var party = new PartyBattleState(
                new CharacterBattleState[] { character });
            var boss = new BossBattleState(
                "training_boss", ElementType.Dark, bossHp, 100d);
            var ids = new CombatActionIdSequence();
            var state = new BeatriceBloodstoneBattleState(character);
            state.BindPartyBattleState(party);
            var rule = new BeatriceBloodstoneLifestealRule(
                state,
                config,
                awakening,
                ids);
            SetHp(party, initialHp);
            return new TestBattle(character, party, boss, ids, rule);
        }

        private static void SetHp(PartyBattleState party, long hp)
        {
            long damage = party.CurrentHp - hp;
            if (damage <= 0L)
            {
                return;
            }

            PartyDamageApplier.Apply(
                party,
                BossDamageCalculator.Calculate(new BossDamageContext(
                    damage, 0d, 0d, 1d, 0d, 0d, 0d, 0d)));
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                BeatriceBloodstoneLifestealRule rule)
            {
                Character = character;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                Rule = rule;
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BeatriceBloodstoneLifestealRule Rule { get; }

            public DamageAction CreateDamage(
                ActionOrigin origin,
                AttackType attackType,
                AttackTag tags,
                double coefficient)
            {
                return new DamageAction(
                    ActionIds.Next(),
                    origin,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Dark,
                        attackType,
                        tags,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
            }

            public IReadOnlyList<DamageAction> CreateDamageSequence(
                ActionOrigin origin,
                AttackType attackType,
                AttackTag tags,
                double coefficient)
            {
                if (origin != ActionOrigin.Additional
                    && origin != ActionOrigin.Chase)
                {
                    return new[]
                    {
                        CreateDamage(
                            origin,
                            attackType,
                            tags,
                            coefficient)
                    };
                }

                DamageAction root = CreateDamage(
                    ActionOrigin.System,
                    AttackType.Match,
                    AttackTag.None,
                    coefficient: 0d);
                var followUp = new DamageAction(
                    ActionIds.Next(),
                    origin,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Dark,
                        attackType,
                        tags,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    rootActionId: root.ActionId,
                    sourceActionId: root.ActionId);
                return new[] { root, followUp };
            }

            public CombatActionExecutionResult Execute(DamageAction damage)
            {
                return Execute(new[] { damage });
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<DamageAction> actions)
            {
                var queueActions = new CombatAction[actions.Count];
                for (int index = 0; index < actions.Count; index++)
                {
                    queueActions[index] = actions[index];
                }

                return CreateExecutor(new ICombatTriggerRule[] { Rule })
                    .Execute(new CombatActionQueue(queueActions));
            }

            public CombatActionExecutor CreateExecutor(
                IReadOnlyList<ICombatTriggerRule> rules)
            {
                return new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
            }
        }
    }
}

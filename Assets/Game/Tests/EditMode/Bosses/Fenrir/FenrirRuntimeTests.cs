using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Fenrir;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Fenrir
{
    public sealed class FenrirRuntimeTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Fenrir/fenrir_combat_config.asset";

        [Test]
        public void NewBattleStartsNormalWithoutWoundAndForecastsSavageRend()
        {
            Harness battle = CreateBattle();

            Assert.That(battle.State.IsFrenzied, Is.False);
            Assert.That(battle.State.GetWoundStackCount(battle.Party), Is.Zero);
            Assert.That(battle.State.FrenzyRemainingCriticalHits, Is.Zero);
            Assert.That(battle.State.CurrentVisualStateId, Is.Empty);
            Assert.That(
                battle.State.GetIntentForecast(4).Select(item => item.SkillId),
                Is.EqualTo(new[] { FenrirRules.SavageRendSkillId }));
        }

        [Test]
        public void SavageRendBuildsWoundEvenWhenShieldAbsorbsAllDamage()
        {
            Harness battle = CreateBattle();
            battle.Party.Shields.Add(new ShieldInstance(
                900,
                "test_shield",
                1000,
                1,
                5,
                900));

            CombatActionExecutionResult result = ExecuteBossAction(battle);
            var damage = result.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();

            Assert.That(damage.AppliedHpDamage, Is.Zero);
            Assert.That(battle.State.GetWoundStackCount(battle.Party),
                Is.EqualTo(1));
        }

        [Test]
        public void ThirdSavageRendConsumesWoundAndEntersFrenzyWithoutStackThree()
        {
            Harness battle = CreateBattle();

            ExecuteBossAction(battle);
            ExecuteBossAction(battle);
            Assert.That(battle.State.GetWoundStackCount(battle.Party),
                Is.EqualTo(2));
            CombatActionExecutionResult third = ExecuteBossAction(battle);

            Assert.That(third.ActionResults.Select(item => item.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(BossDamageActionResult),
                    typeof(ConsumeEffectStackActionResult),
                    typeof(ApplyEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));
            Assert.That(battle.State.GetWoundStackCount(battle.Party), Is.Zero);
            Assert.That(battle.State.IsFrenzied, Is.True);
            Assert.That(battle.State.FrenzyRemainingCriticalHits,
                Is.EqualTo(3));
            Assert.That(battle.State.CurrentVisualStateId,
                Is.EqualTo(FenrirRules.FrenziedVisualStateId));
            Assert.That(battle.State.GetIntentForecast(3).Single().SkillId,
                Is.EqualTo(FenrirRules.RampagingClawSkillId));
        }

        [Test]
        public void RampagingClawAppliesAttackIncreaseAndCoefficientSeparately()
        {
            Harness battle = CreateFrenziedBattle();

            CombatActionExecutionResult result = ExecuteBossAction(battle);
            var damage = result.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();

            Assert.That(damage.Context.AttackIncreaseRateSum,
                Is.EqualTo(0.6d));
            Assert.That(damage.Context.AttackCoefficient,
                Is.EqualTo(1.2d));
            Assert.That(damage.DamageResult.FinalAttack, Is.EqualTo(160d));
            Assert.That(damage.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(192L));
            Assert.That(battle.State.GetWoundStackCount(battle.Party), Is.Zero);
        }

        [Test]
        public void FrenzyReductionStaysTwentyPercentAtEveryStackCount()
        {
            Harness battle = CreateFrenziedBattle();

            for (int remaining = 3; remaining >= 1; remaining--)
            {
                DamageActionResult damage = ExecutePlayerDamage(
                    battle,
                    AttackType.Active,
                    ActionOrigin.Active,
                    canCritical: remaining > 1,
                    coefficient: 1d)
                    .ActionResults.OfType<DamageActionResult>().Single();
                Assert.That(damage.Context.TargetTakenDamageReductionRateSum,
                    Is.EqualTo(0.2d));
                Assert.That(damage.AppliedDamage, Is.EqualTo(80L));
                if (remaining > 1)
                {
                    Assert.That(battle.State.FrenzyRemainingCriticalHits,
                        Is.EqualTo(remaining - 1));
                }
            }
        }

        [Test]
        public void NonCriticalZeroDamageAndDotDoNotConsumeFrenzy()
        {
            Harness battle = CreateFrenziedBattle();

            ExecutePlayerDamage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: false,
                coefficient: 1d);
            ExecutePlayerDamage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: true,
                coefficient: 0d);
            ExecutePlayerDamage(
                battle,
                AttackType.DamageOverTime,
                ActionOrigin.DamageOverTime,
                canCritical: true,
                coefficient: 1d);

            Assert.That(battle.State.FrenzyRemainingCriticalHits,
                Is.EqualTo(3));
        }

        [TestCase(AttackType.Additional, ActionOrigin.Additional)]
        [TestCase(AttackType.Chase, ActionOrigin.Chase)]
        public void IndependentFollowUpCriticalConsumesOneCount(
            AttackType attackType,
            ActionOrigin origin)
        {
            Harness battle = CreateFrenziedBattle();
            DamageAction root = Damage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: false,
                coefficient: 0d);
            DamageAction followUp = Damage(
                battle,
                attackType,
                origin,
                canCritical: true,
                coefficient: 1d,
                root.ActionId,
                root.ActionId);

            battle.Executor.Execute(
                new CombatActionQueue(new CombatAction[] { root, followUp }));

            Assert.That(battle.State.FrenzyRemainingCriticalHits,
                Is.EqualTo(2));
        }

        [Test]
        public void FinalCriticalUsesReductionAndFollowingDamageDoesNot()
        {
            Harness battle = CreateFrenziedBattle();
            ConsumeFrenzyCount(battle, 2);
            DamageAction finalCritical = Damage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: true,
                coefficient: 1d);
            DamageAction following = Damage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: false,
                coefficient: 1d);

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(
                    new CombatAction[] { finalCritical, following }));
            DamageActionResult[] damages = result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();

            Assert.That(damages[0].AppliedDamage, Is.EqualTo(80L));
            Assert.That(damages[1].AppliedDamage, Is.EqualTo(100L));
            Assert.That(battle.State.IsFrenzied, Is.False);
            Assert.That(battle.State.FrenzyRemainingCriticalHits, Is.Zero);
            Assert.That(battle.State.CurrentVisualStateId, Is.Empty);
            Assert.That(battle.State.GetIntentForecast(1).Single().SkillId,
                Is.EqualTo(FenrirRules.SavageRendSkillId));
        }

        [Test]
        public void BossActionAfterFrenzyEndsUsesSavageRendAndRestartsWound()
        {
            Harness battle = CreateFrenziedBattle();
            ConsumeFrenzyCount(battle, 2);
            ExecutePlayerDamage(
                battle,
                AttackType.Active,
                ActionOrigin.Active,
                canCritical: true,
                coefficient: 1d);

            CombatActionExecutionResult bossResult = ExecuteBossAction(battle);
            var damage = bossResult.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();

            Assert.That(damage.Context.AttackCoefficient, Is.EqualTo(0.8d));
            Assert.That(damage.Context.AttackIncreaseRateSum, Is.Zero);
            Assert.That(battle.State.GetWoundStackCount(battle.Party),
                Is.EqualTo(1));
        }

        [Test]
        public void SeparateBattlesDoNotShareEffectsOrRuntimeState()
        {
            Harness first = CreateFrenziedBattle();
            Harness second = CreateBattle();

            Assert.That(first.State.IsFrenzied, Is.True);
            Assert.That(second.State.IsFrenzied, Is.False);
            Assert.That(second.State.GetWoundStackCount(second.Party), Is.Zero);
            Assert.That(first.State, Is.Not.SameAs(second.State));
        }

        private static Harness CreateBattle()
        {
            FenrirCombatConfig config =
                AssetDatabase.LoadAssetAtPath<FenrirCombatConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            var boss = new BossBattleState(
                FenrirRules.BossId,
                ElementType.Dark,
                10000,
                100d);
            var character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Dark,
                10000,
                100d);
            var party = new PartyBattleState(new[] { character });
            var ids = new CombatActionIdSequence();
            var state = new FenrirBattleRuntimeState(boss, config);
            var provider = new FenrirBossCombatActionProvider(state);
            var rule = new FenrirFrenzyCriticalHitRule(state, ids);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(7)),
                new CombatTriggerResolver(new ICombatTriggerRule[] { rule }));
            return new Harness(
                boss,
                party,
                character,
                state,
                provider,
                executor,
                ids);
        }

        private static Harness CreateFrenziedBattle()
        {
            Harness battle = CreateBattle();
            ExecuteBossAction(battle);
            ExecuteBossAction(battle);
            ExecuteBossAction(battle);
            Assert.That(battle.State.IsFrenzied, Is.True);
            return battle;
        }

        private static CombatActionExecutionResult ExecuteBossAction(
            Harness battle)
        {
            IReadOnlyList<CombatAction> actions =
                battle.Provider.CreateRootActions(Context(battle));
            return battle.Executor.Execute(new CombatActionQueue(actions));
        }

        private static CombatActionExecutionResult ExecutePlayerDamage(
            Harness battle,
            AttackType attackType,
            ActionOrigin origin,
            bool canCritical,
            double coefficient)
        {
            DamageAction action = Damage(
                battle,
                attackType,
                origin,
                canCritical,
                coefficient);
            return battle.Executor.Execute(
                new CombatActionQueue(new CombatAction[] { action }));
        }

        private static DamageAction Damage(
            Harness battle,
            AttackType attackType,
            ActionOrigin origin,
            bool canCritical,
            double coefficient,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            return new DamageAction(
                battle.ActionIds.Next(),
                origin,
                new DamageContextBuildRequest(
                    battle.Character,
                    battle.Party,
                    battle.Boss,
                    ElementType.Dark,
                    attackType,
                    AttackTag.None,
                    coefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: canCritical,
                    baseCriticalChance: canCritical ? 1d : 0d,
                    baseCriticalDamageMultiplier: 1d),
                rootActionId,
                sourceActionId);
        }

        private static void ConsumeFrenzyCount(Harness battle, int amount)
        {
            Assert.That(
                battle.State.TryGetFrenzyEffect(out EffectInstance frenzy),
                Is.True);
            battle.Executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                new ConsumeEffectStackAction(
                    battle.ActionIds.Next(),
                    ActionOrigin.System,
                    battle.Boss,
                    frenzy.RuntimeId,
                    amount)
            }));
        }

        private static BossCombatActionContext Context(Harness battle)
        {
            return (BossCombatActionContext)Activator.CreateInstance(
                typeof(BossCombatActionContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    battle.Boss,
                    battle.Party,
                    1,
                    battle.ActionIds
                },
                culture: null);
        }

        private sealed class Harness
        {
            public Harness(
                BossBattleState boss,
                PartyBattleState party,
                CharacterBattleState character,
                FenrirBattleRuntimeState state,
                FenrirBossCombatActionProvider provider,
                CombatActionExecutor executor,
                CombatActionIdSequence actionIds)
            {
                Boss = boss;
                Party = party;
                Character = character;
                State = state;
                Provider = provider;
                Executor = executor;
                ActionIds = actionIds;
            }

            public BossBattleState Boss { get; }
            public PartyBattleState Party { get; }
            public CharacterBattleState Character { get; }
            public FenrirBattleRuntimeState State { get; }
            public FenrirBossCombatActionProvider Provider { get; }
            public CombatActionExecutor Executor { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}

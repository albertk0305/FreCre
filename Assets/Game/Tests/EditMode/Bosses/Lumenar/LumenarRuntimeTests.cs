using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Lumenar;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Lumenar
{
    public sealed class LumenarRuntimeTests
    {
        private const string ConfigPath =
            "Assets/Data/CombatConfigs/Lumenar/lumenar_combat_config.asset";

        [Test]
        public void ExactHalfDoesNotTransitionButBelowHalfDoes()
        {
            Harness battle = Create(maxHp: 100);

            CombatActionExecutionResult first = ExecuteDamage(
                battle, 0.50d, AttackType.Active, ActionOrigin.Active);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(50));
            Assert.That(battle.State.CurrentForm, Is.EqualTo(LumenarForm.Light));
            Assert.That(first.ActionResults, Has.Count.EqualTo(1));

            CombatActionExecutionResult second = ExecuteDamage(
                battle, 0.01d, AttackType.Active, ActionOrigin.Active);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(49));
            Assert.That(second.ActionResults.Select(item => item.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DamageActionResult),
                    typeof(ReplaceEffectActionResult)
                }));
            AssertStone(battle);
        }

        [Test]
        public void OddMaximumUsesExactIntegerThreshold()
        {
            Harness battle = Create(maxHp: 101);

            ExecuteDamage(battle, 0.50d, AttackType.Active, ActionOrigin.Active);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(51));
            Assert.That(battle.State.CurrentForm, Is.EqualTo(LumenarForm.Light));
            ExecuteDamage(battle, 0.01d, AttackType.Active, ActionOrigin.Active);
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(50));
            AssertStone(battle);
        }

        [Test]
        public void LethalDamageKeepsLightForm()
        {
            Harness battle = Create(maxHp: 100);

            ExecuteDamage(battle, 1d, AttackType.Active, ActionOrigin.Active);

            Assert.That(battle.Boss.IsDefeated, Is.True);
            Assert.That(battle.State.CurrentForm, Is.EqualTo(LumenarForm.Light));
            Assert.That(battle.State.TransitionCompleted, Is.False);
        }

        [TestCase(AttackType.Match, ActionOrigin.Match, AttackTag.None)]
        [TestCase(AttackType.Active, ActionOrigin.Active, AttackTag.FixedDamage)]
        [TestCase(AttackType.Additional, ActionOrigin.Additional, AttackTag.None)]
        [TestCase(AttackType.Chase, ActionOrigin.Chase, AttackTag.None)]
        [TestCase(AttackType.DamageOverTime, ActionOrigin.DamageOverTime, AttackTag.None)]
        public void EveryNonlethalDamageTypeCanTriggerTransition(
            AttackType attackType,
            ActionOrigin origin,
            AttackTag attackTags)
        {
            Harness battle = Create(maxHp: 100);

            ExecuteDamage(
                battle, 0.51d, attackType, origin, attackTags);

            AssertStone(battle);
        }

        [Test]
        public void FollowingRootDamageImmediatelyUsesStoneReduction()
        {
            Harness battle = Create(maxHp: 100);
            DamageAction first = Damage(battle, 0.51d,
                AttackType.Active, ActionOrigin.Active);
            DamageAction second = Damage(battle, 0.10d,
                AttackType.Additional, ActionOrigin.Additional,
                rootActionId: first.ActionId,
                sourceActionId: first.ActionId);

            Assert.That(second.RootActionId, Is.EqualTo(first.ActionId));
            Assert.That(second.SourceActionId, Is.EqualTo(first.ActionId));

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(new CombatAction[] { first, second }));

            Assert.That(result.ActionResults.Select(item => item.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DamageActionResult),
                    typeof(ReplaceEffectActionResult),
                    typeof(DamageActionResult)
                }));
            var following = (DamageActionResult)result.ActionResults[2];
            Assert.That(following.AppliedDamage, Is.EqualTo(8));
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(41));
        }

        [Test]
        public void StonePatternStartsAtFirstActionAndSecondAddsBoardCommand()
        {
            Harness battle = Create(maxHp: 100);
            ExecuteDamage(battle, 0.51d, AttackType.Active, ActionOrigin.Active);
            var provider = new LumenarBossCombatActionProvider(battle.State);

            BossActionPlan first = provider.CreatePlan(Context(battle, 1));
            Assert.That(first.BoardCommand, Is.Null);
            Assert.That(((BossDamageAction)first.CombatActions.Single())
                .ContextRequest.AttackCoefficient, Is.EqualTo(0.75d));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            BossActionPlan second = provider.CreatePlan(Context(battle, 2));
            Assert.That(second.BoardCommand.Kind,
                Is.EqualTo(BattleBoardMutationKind.CreateFallingStone));
            Assert.That(second.BoardCommand.RequestedCount, Is.EqualTo(1));
            Assert.That(second.BoardCommand.MaximumCount, Is.EqualTo(2));
        }

        private static Harness Create(long maxHp)
        {
            LumenarCombatConfig config =
                AssetDatabase.LoadAssetAtPath<LumenarCombatConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            var boss = new BossBattleState(
                LumenarRules.BossId, ElementType.Light, maxHp, 100d);
            var character = new CharacterBattleState(
                "hero", 0, ElementType.Light, 1000, 100d);
            var party = new PartyBattleState(new[] { character });
            var ids = new CombatActionIdSequence();
            var state = new LumenarBattleRuntimeState(boss, config);
            LumenarEffectFactory.InitializeBattle(boss, config, ids);
            var rule = new LumenarHealthTransitionRule(state, ids);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(new ICombatTriggerRule[] { rule }));
            return new Harness(
                boss, party, character, state, executor, ids);
        }

        private static CombatActionExecutionResult ExecuteDamage(
            Harness battle,
            double coefficient,
            AttackType attackType,
            ActionOrigin origin,
            AttackTag attackTags = AttackTag.None)
        {
            if (origin != ActionOrigin.Additional
                && origin != ActionOrigin.Chase)
            {
                return battle.Executor.Execute(
                    new CombatActionQueue(new CombatAction[]
                    {
                        Damage(
                            battle,
                            coefficient,
                            attackType,
                            origin,
                            attackTags)
                    }));
            }

            DamageAction root = Damage(
                battle,
                0d,
                AttackType.Active,
                ActionOrigin.Active);
            DamageAction derived = Damage(
                battle,
                coefficient,
                attackType,
                origin,
                attackTags,
                rootActionId: root.ActionId,
                sourceActionId: root.ActionId);
            Assert.That(derived.RootActionId, Is.EqualTo(root.ActionId));
            Assert.That(derived.SourceActionId, Is.EqualTo(root.ActionId));
            return battle.Executor.Execute(
                new CombatActionQueue(new CombatAction[] { root, derived }));
        }

        private static DamageAction Damage(
            Harness battle,
            double coefficient,
            AttackType attackType,
            ActionOrigin origin,
            AttackTag attackTags = AttackTag.None,
            long? rootActionId = null,
            long? sourceActionId = null) => new DamageAction(
                battle.ActionIds.Next(),
                origin,
                new DamageContextBuildRequest(
                    battle.Character,
                    battle.Party,
                    battle.Boss,
                    ElementType.Light,
                    attackType,
                    attackTags,
                    coefficient,
                    false,
                    0,
                    false),
                rootActionId,
                sourceActionId);

        private static BossCombatActionContext Context(Harness battle, int turn) =>
            (BossCombatActionContext)Activator.CreateInstance(
                typeof(BossCombatActionContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    battle.Boss, battle.Party, turn, battle.ActionIds
                },
                culture: null);

        private static void AssertStone(Harness battle)
        {
            Assert.That(battle.State.CurrentForm, Is.EqualTo(LumenarForm.Stone));
            Assert.That(battle.State.PatternIndex, Is.Zero);
            Assert.That(battle.State.TransitionCompleted, Is.True);
            Assert.That(battle.State.TransitionPending, Is.False);
            Assert.That(battle.Boss.Effects.FindByEffectId(
                LumenarRules.LightFormEffectId), Is.Empty);
            Assert.That(battle.Boss.Effects.FindByEffectId(
                LumenarRules.StoneFormEffectId), Has.Count.EqualTo(1));
        }

        private sealed class Harness
        {
            public Harness(BossBattleState boss, PartyBattleState party,
                CharacterBattleState character, LumenarBattleRuntimeState state,
                CombatActionExecutor executor, CombatActionIdSequence actionIds)
            {
                Boss = boss; Party = party; Character = character;
                State = state; Executor = executor; ActionIds = actionIds;
            }
            public BossBattleState Boss { get; }
            public PartyBattleState Party { get; }
            public CharacterBattleState Character { get; }
            public LumenarBattleRuntimeState State { get; }
            public CombatActionExecutor Executor { get; }
            public CombatActionIdSequence ActionIds { get; }
        }
    }
}

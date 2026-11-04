using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Chloe;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Chloe
{
    public sealed class ChloeValentineCombatTests
    {
        private ChloeValentineCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<
                ChloeValentineCombatConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildAndRulesApplyEachAwakeningAtItsOwnedLayer()
        {
            var build = new ChloeValentineBuildModifier();
            Assert.That(build.GetStatMultipliers(0).MaxHpMultiplier,
                Is.EqualTo(1d));
            Assert.That(build.GetStatMultipliers(1).MaxHpMultiplier,
                Is.EqualTo(1.05d));
            Assert.That(build.GetStatMultipliers(6).AttackMultiplier,
                Is.EqualTo(1d));

            ChloeValentineResolvedCombatRules zero =
                ChloeValentineCombatRuleResolver.Resolve(config, 0);
            ChloeValentineResolvedCombatRules two =
                ChloeValentineCombatRuleResolver.Resolve(config, 2);
            ChloeValentineResolvedCombatRules three =
                ChloeValentineCombatRuleResolver.Resolve(config, 3);
            ChloeValentineResolvedCombatRules four =
                ChloeValentineCombatRuleResolver.Resolve(config, 4);
            ChloeValentineResolvedCombatRules five =
                ChloeValentineCombatRuleResolver.Resolve(config, 5);
            ChloeValentineResolvedCombatRules six =
                ChloeValentineCombatRuleResolver.Resolve(config, 6);

            Assert.That(zero.ActionLocalHealingIncreaseRate, Is.Zero);
            Assert.That(two.ActionLocalHealingIncreaseRate,
                Is.EqualTo(0.05d));
            Assert.That(three.Match3HealingCoefficient,
                Is.EqualTo(0.08d));
            Assert.That(three.Match4HealingCoefficient,
                Is.EqualTo(0.13d));
            Assert.That(four.Match5HealingCoefficient,
                Is.EqualTo(0.22d));
            Assert.That(four.Match5CriticalChanceIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(five.ActiveHealingCoefficient,
                Is.EqualTo(0.35d));
            Assert.That(five.ActiveCriticalDamageIncreaseRate,
                Is.EqualTo(0.30d));
            Assert.That(six.PassivePartyAttackIncreaseRate,
                Is.EqualTo(0.07d));
        }

        [Test]
        public void MatchFiveAtFullHpExecutesHealThenCriticalChanceBuff()
        {
            TestBattle battle = CreateBattle(maxHp: 6090);
            var provider = new ChloeValentineMatchActionProvider(config, 4);
            CombatAction[] actions = provider.CreateActions(
                    battle.Chloe,
                    battle.Party,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    battle.ActionIds)
                .ToArray();

            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0], Is.TypeOf<HealAction>());
            Assert.That(actions[1], Is.TypeOf<ApplyEffectAction>());
            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(actions));

            Assert.That(result.CompletedActionCount, Is.EqualTo(2));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<HealActionResult>());
            Assert.That(result.ActionResults[0].Action,
                Is.TypeOf<HealAction>());
            Assert.That(result.ActionResults[1],
                Is.TypeOf<ApplyEffectActionResult>());
            Assert.That(result.ActionResults[1].Action,
                Is.TypeOf<ApplyEffectAction>());

            HealActionResult heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            EffectInstance effect = battle.Party.Effects
                .FindByEffectId(
                    ChloeValentineRules.Match5CriticalChanceEffectId)
                .Single();
            Assert.That(heal.AppliedHealing, Is.Zero);
            Assert.That(heal.FinalHealing, Is.EqualTo(1406L));
            Assert.That(effect.ModifierType,
                Is.EqualTo(EffectModifierType.CriticalChanceIncrease));
            Assert.That(effect.Magnitude, Is.EqualTo(0.15d));
            Assert.That(effect.RemainingTurns, Is.EqualTo(2));

            new BattleTurnEndProcessor(
                battle.Party,
                battle.Boss).ProcessTurnEnd();
            battle.Executor.Execute(new CombatActionQueue(
                provider.CreateActions(
                    battle.Chloe,
                    battle.Party,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    battle.ActionIds)));
            Assert.That(battle.Party.Effects.FindByEffectId(
                    ChloeValentineRules.Match5CriticalChanceEffectId)
                .Count(), Is.EqualTo(1));
            Assert.That(effect.RemainingTurns, Is.EqualTo(2));
        }

        [Test]
        public void AwakeningTwoAddsToCommonHealingIncreaseExactlyOnce()
        {
            TestBattle battle = CreateBattle(maxHp: 1000);
            battle.Party.Effects.ApplyEffect(new EffectInstance(
                900,
                "test_healing_increase",
                "test",
                EffectCategory.Buff,
                EffectModifierType.HealingIncrease,
                0.10d,
                remainingTurns: 2,
                creationOrder: 900,
                stackPolicy: EffectStackPolicy.RefreshDuration));
            var provider = new ChloeValentineMatchActionProvider(config, 2);

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(provider.CreateActions(
                    battle.Chloe,
                    battle.Party,
                    BoardMatchTier.Three,
                    finalComboCount: 1,
                    battle.ActionIds)));

            HealActionResult heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            Assert.That(heal.Context.HealingIncreaseRateSum,
                Is.EqualTo(0.15d).Within(0.000000001d));
            Assert.That(heal.FinalHealing, Is.EqualTo(80L));
        }

        [Test]
        public void ActiveAtFullHpExecutesHealThenCriticalDamageBuff()
        {
            TestBattle battle = CreateBattle(maxHp: 6090);
            var provider = new ChloeValentineActiveActionProvider(config, 5);
            CombatAction[] actions = provider.CreateActions(
                    battle.Chloe,
                    battle.Party,
                    battle.ActionIds)
                .ToArray();

            Assert.That(provider.CooldownTurns, Is.EqualTo(8));
            Assert.That(actions, Has.Length.EqualTo(2));
            Assert.That(actions[0], Is.TypeOf<HealAction>());
            Assert.That(actions[1], Is.TypeOf<ApplyEffectAction>());
            Assert.That(((HealAction)actions[0]).ContextRequest.AppliesCombo,
                Is.False);
            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(actions));

            Assert.That(result.CompletedActionCount, Is.EqualTo(2));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<HealActionResult>());
            Assert.That(result.ActionResults[0].Action,
                Is.TypeOf<HealAction>());
            Assert.That(result.ActionResults[1],
                Is.TypeOf<ApplyEffectActionResult>());
            Assert.That(result.ActionResults[1].Action,
                Is.TypeOf<ApplyEffectAction>());

            HealActionResult heal = result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            EffectInstance effect = battle.Party.Effects
                .FindByEffectId(
                    ChloeValentineRules.ActiveCriticalDamageEffectId)
                .Single();
            Assert.That(heal.AppliedHealing, Is.Zero);
            Assert.That(heal.FinalHealing, Is.EqualTo(2238L));
            Assert.That(effect.ModifierType,
                Is.EqualTo(EffectModifierType.CriticalDamageIncrease));
            Assert.That(effect.Magnitude, Is.EqualTo(0.30d));
            Assert.That(effect.RemainingTurns, Is.EqualTo(3));
        }

        [Test]
        public void RegistrarProvidesUniquePermanentPartyAttackEffect()
        {
            CharacterDefinition definition = CreateDefinition(config);
            try
            {
                TestBattle battle = CreateBattle(maxHp: 6090);
                var member = new BattlePartyMemberInput(
                    ChloeValentineRules.CharacterId,
                    partySlotIndex: 0,
                    level: 100,
                    awakening: 6,
                    definition);
                var matchProviders = new MatchEventActionProviderRegistry();
                var activeProviders = new ActiveAbilityActionProviderRegistry();

                CharacterCombatProviderRegistrationResult result =
                    new ChloeValentineCombatProviderRegistrar().Register(
                        member,
                        battle.Chloe,
                        battle.Boss,
                        battle.ActionIds,
                        matchProviders,
                        activeProviders);

                Assert.That(result.HasActiveAbility, Is.True);
                Assert.That(result.ActiveAbilityId,
                    Is.EqualTo(ChloeValentineRules.ActiveAbilityId));
                Assert.That(result.ActiveCooldownTurns, Is.EqualTo(8));
                InitialPartyEffectDefinition passive =
                    result.InitialPartyEffects.Single();
                Assert.That(passive.EffectId,
                    Is.EqualTo(ChloeValentineRules.PassiveEffectId));
                Assert.That(passive.ModifierType,
                    Is.EqualTo(EffectModifierType.AttackIncrease));
                Assert.That(passive.Magnitude, Is.EqualTo(0.07d));
                Assert.That(passive.RemainingTurns, Is.Null);
                Assert.That(passive.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.Unique));
                Assert.That(result.TriggerRules, Is.Empty);
                Assert.That(result.CombatStatusSources, Is.Empty);
                Assert.That(result.ResourceRequirements, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static CharacterDefinition CreateDefinition(
            ChloeValentineCombatConfig combatConfig)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                ChloeValentineRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Light;
            serialized.FindProperty("level1Hp").intValue = 1450;
            serialized.FindProperty("level1Attack").intValue = 80;
            serialized.FindProperty("level100Hp").intValue = 5800;
            serialized.FindProperty("level100Attack").intValue = 400;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                combatConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static TestBattle CreateBattle(long maxHp)
        {
            var chloe = new CharacterBattleState(
                ChloeValentineRules.CharacterId,
                partySlotIndex: 0,
                element: ElementType.Light,
                maxHp: maxHp,
                attack: 400d);
            var party = new PartyBattleState(new[] { chloe });
            var boss = new BossBattleState(
                "boss",
                element: ElementType.Dark,
                maxHp: 1000000,
                attack: 100d);
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            return new TestBattle(
                chloe,
                party,
                boss,
                actionIds,
                executor);
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState chloe,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                CombatActionExecutor executor)
            {
                Chloe = chloe;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                Executor = executor;
            }

            public CharacterBattleState Chloe { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public CombatActionExecutor Executor { get; }
        }
    }
}


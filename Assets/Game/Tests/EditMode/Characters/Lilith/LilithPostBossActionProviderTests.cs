using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Results;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithPostBossActionProviderTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FewerThanFourStacksCreatesNoActionsOrEffects(int stacks)
        {
            using (var battle = new TestBattle())
            {
                battle.AddCovenant(stacks);
                int effectCount = battle.Boss.Effects.Count;

                IReadOnlyList<CombatAction> actions = battle.CreateActions();

                Assert.That(actions, Is.Empty);
                Assert.That(battle.ActionIds.LastIssuedId, Is.Zero);
                Assert.That(battle.Boss.Effects.Count,
                    Is.EqualTo(effectCount));
                Assert.That(CovenantCount(battle), Is.EqualTo(stacks));
            }
        }

        [TestCase(4, 0, 3.00d, 0)]
        [TestCase(7, 0, 3.00d, 3)]
        [TestCase(8, 0, 6.00d, 0)]
        [TestCase(11, 0, 6.00d, 3)]
        [TestCase(12, 0, 9.00d, 0)]
        [TestCase(4, 5, 3.30d, 0)]
        [TestCase(8, 5, 6.60d, 0)]
        [TestCase(12, 5, 9.90d, 0)]
        public void CreatesOnePassiveDamageThenExactConsumptionActions(
            int stacks,
            int awakening,
            double expectedCoefficient,
            int remainingStacks)
        {
            using (var battle = new TestBattle(awakening))
            {
                battle.AddCovenant(stacks);

                IReadOnlyList<CombatAction> actions = battle.CreateActions();

                Assert.That(actions, Has.Count.EqualTo(2 + remainingStacks));
                var damage = actions[0] as DamageAction;
                Assert.That(damage, Is.Not.Null);
                Assert.That(damage.ContextRequest.Attacker,
                    Is.SameAs(battle.Lilith));
                Assert.That(damage.ContextRequest.TargetBoss,
                    Is.SameAs(battle.Boss));
                Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.System));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Passive));
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Dark));
                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(0.000000001d));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.False);
                Assert.That(damage.ContextRequest.FinalComboCount, Is.Zero);
                Assert.That(damage.ContextRequest.CanCritical, Is.True);
                Assert.That(damage.CanTriggerFollowUp, Is.False);
                Assert.That(actions[1], Is.TypeOf<RemoveEffectAction>());
                Assert.That(actions.Skip(2).All(action =>
                    action is ApplyEffectAction), Is.True);

                for (int index = 0; index < actions.Count; index++)
                {
                    Assert.That(actions[index].ActionId,
                        Is.EqualTo(index + 1));
                    Assert.That(actions[index].Origin,
                        Is.EqualTo(ActionOrigin.System));
                    Assert.That(actions[index].RootActionId,
                        Is.EqualTo(actions[index].ActionId));
                    Assert.That(actions[index].SourceActionId, Is.Null);
                }
            }
        }

        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(7, 3)]
        [TestCase(8, 0)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(11, 3)]
        [TestCase(12, 0)]
        public void ExecutionConsumesOnlyCompleteFourStackUnits(
            int stacks,
            int expectedRemaining)
        {
            using (var battle = new TestBattle())
            {
                battle.AddCovenant(stacks);

                CombatActionExecutionResult result = battle.ExecuteActions(
                    battle.CreateActions());

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<RemoveEffectActionResult>());
                Assert.That(CovenantCount(battle),
                    Is.EqualTo(expectedRemaining));
                Assert.That(battle.Boss.Effects.FindByEffectId(
                        LilithRules.CovenantEffectId).Count,
                    Is.EqualTo(expectedRemaining == 0 ? 0 : 1));
            }
        }

        [Test]
        public void PostBossSessionDamagesBeforeItConsumesCovenant()
        {
            using (var battle = new TestBattle(awakening: 6))
            {
                battle.AddCovenant(12);
                var postBoss = battle.CreatePostBossExecutor();
                PostBossCombatExecutor.PostBossCombatExecutionSession session =
                    postBoss.BeginExecution(currentTurn: 1);

                Assert.That(session.TryExecuteNext(out var damageStep),
                    Is.True);
                var damage = damageStep.ActionStep.Result
                    as DamageActionResult;
                Assert.That(damage, Is.Not.Null);
                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(9.90d).Within(0.000000001d));
                Assert.That(damage.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.216d).Within(0.000000001d));
                Assert.That(CovenantCount(battle), Is.EqualTo(12));

                Assert.That(session.TryExecuteNext(out var removeStep),
                    Is.True);
                Assert.That(removeStep.ActionStep.Result,
                    Is.TypeOf<RemoveEffectActionResult>());
                Assert.That(CovenantCount(battle), Is.Zero);
                Assert.That(session.TryCompleteCurrentProvider(out _),
                    Is.True);
                Assert.That(session.IsCompleted, Is.True);
            }
        }

        [TestCase(0, 0.015d)]
        [TestCase(6, 0.018d)]
        public void RemainderKeepsResolvedMagnitudeAndUnlimitedDuration(
            int awakening,
            double expectedMagnitude)
        {
            using (var battle = new TestBattle(awakening))
            {
                battle.AddCovenant(7);
                battle.ExecuteActions(battle.CreateActions());

                EffectInstance remainder = battle.Boss.Effects
                    .FindByEffectId(LilithRules.CovenantEffectId)
                    .Single();
                Assert.That(remainder.StackCount, Is.EqualTo(3));
                Assert.That(remainder.Magnitude,
                    Is.EqualTo(expectedMagnitude));
                Assert.That(remainder.RemainingTurns, Is.Null);
                Assert.That(remainder.MaxStackCount, Is.EqualTo(12));

                DamageActionResult nextDamage = battle.ExecuteProbeDamage();
                Assert.That(nextDamage.Context
                        .TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(3 * expectedMagnitude)
                        .Within(0.000000001d));
            }
        }

        [Test]
        public void ZeroDamageStillConsumesCovenant()
        {
            using (var battle = new TestBattle())
            {
                battle.AddCovenant(4);
                battle.Lilith.Effects.ApplyEffect(Effect(
                    1500L,
                    "effect_test_zero_dealt_damage",
                    EffectModifierType.DealtDamageReduction,
                    1d));

                CombatActionExecutionResult result = battle.ExecuteActions(
                    battle.CreateActions());
                DamageActionResult damage = result.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(damage.DamageResult.FinalDamage, Is.Zero);
                Assert.That(CovenantCount(battle), Is.Zero);
                Assert.That(result.ActionResults,
                    Has.Exactly(1).TypeOf<RemoveEffectActionResult>());
            }
        }

        [Test]
        public void PassiveUsesCommonModifiersAndRejectsLegacyTypeMasks()
        {
            using (var battle = new TestBattle(
                bossElement: ElementType.Light))
            {
                battle.AddCovenant(4);
                battle.Lilith.Effects.ApplyEffect(Effect(
                    2001,
                    "effect_test_dark",
                    EffectModifierType.ElementDamageIncrease,
                    0.20d,
                    element: ElementType.Dark));
                battle.Party.Effects.ApplyEffect(Effect(
                    2002,
                    "effect_test_dealt",
                    EffectModifierType.DealtDamageIncrease,
                    0.10d));
                battle.Lilith.Effects.ApplyEffect(Effect(
                    2003,
                    "effect_test_passive",
                    EffectModifierType.AttackTypeDamageIncrease,
                    0.18d,
                    mask: AttackTypeMask.Passive));
                battle.Lilith.Effects.ApplyEffect(Effect(
                    2004,
                    "effect_test_legacy_types",
                    EffectModifierType.AttackTypeDamageIncrease,
                    0.90d,
                    mask: AttackTypeMask.Match
                        | AttackTypeMask.Active
                        | AttackTypeMask.Additional
                        | AttackTypeMask.Chase));
                battle.Boss.Effects.ApplyEffect(Effect(
                    2005,
                    "effect_test_boss_increase",
                    EffectModifierType.TargetTakenDamageIncrease,
                    0.30d));
                battle.Boss.Effects.ApplyEffect(Effect(
                    2006,
                    "effect_test_boss_reduction",
                    EffectModifierType.TargetTakenDamageReduction,
                    0.10d,
                    remainingTurns: 1));

                DamageActionResult result = battle.ExecuteExplosionDamage();

                Assert.That(result.Context.AppliesCombo, Is.False);
                Assert.That(result.DamageResult.ComboMultiplier,
                    Is.EqualTo(1d));
                Assert.That(result.Context.ElementDamageIncreaseRateSum,
                    Is.EqualTo(0.20d));
                Assert.That(result.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.10d));
                Assert.That(result.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.18d));
                Assert.That(result.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.36d).Within(0.000000001d));
                Assert.That(result.Context.TargetTakenDamageReductionRateSum,
                    Is.EqualTo(0.10d));
                Assert.That(result.DamageResult.ElementAffinityMultiplier,
                    Is.EqualTo(1.30d));
                Assert.That(battle.Boss.Effects.FindByEffectId(
                    "effect_test_boss_reduction"), Has.Count.EqualTo(1));
                battle.Boss.Effects.ProcessTurnEnd();
                Assert.That(battle.Boss.Effects.FindByEffectId(
                    "effect_test_boss_reduction"), Is.Empty);
            }
        }

        [Test]
        public void ExplosionAllowsOneCriticalRollForAllConsumedUnits()
        {
            var random = new CountingRandomSource(0.25f);
            using (var battle = new TestBattle())
            {
                battle.AddCovenant(12);
                battle.Lilith.Effects.ApplyEffect(Effect(
                    3001,
                    "effect_test_critical_chance",
                    EffectModifierType.CriticalChanceIncrease,
                    0.50d));

                DamageActionResult result = battle.ExecuteExplosionDamage(
                    random);

                Assert.That(result.Context.CanCritical, Is.True);
                Assert.That(result.Context.IsCritical, Is.True);
                Assert.That(random.NextFloatCallCount, Is.EqualTo(1));
                Assert.That(result.DamageResult.SkillCoefficient,
                    Is.EqualTo(9.00d));
            }
        }

        [Test]
        public void TerminalBattleCreatesNoExplosionActions()
        {
            using (var defeatedBoss = new TestBattle(bossHp: 1L))
            {
                defeatedBoss.AddCovenant(4);
                defeatedBoss.ExecuteProbeDamage(coefficient: 1d);
                Assert.That(defeatedBoss.Boss.IsDefeated, Is.True);
                Assert.That(defeatedBoss.CreateActions(), Is.Empty);
            }

            using (var defeatedParty = new TestBattle(partyHp: 1L))
            {
                defeatedParty.AddCovenant(4);
                PartyDamageApplier.Apply(
                    defeatedParty.Party,
                    BossDamageCalculator.Calculate(
                        new BossDamageContext(
                            1d, 0d, 0d, 1d, 0d, 0d, 0d, 0d)));
                Assert.That(defeatedParty.Party.IsIncapacitated, Is.True);
                Assert.That(defeatedParty.CreateActions(), Is.Empty);
            }
        }

        [Test]
        public void LethalExplosionScoresOnceAndStopsRemainingProviders()
        {
            using (var battle = new TestBattle(bossHp: 100L))
            {
                battle.AddCovenant(4);
                var other = new CharacterBattleState(
                    "character_other",
                    1,
                    ElementType.Fire,
                    1000L,
                    100d);
                var party = new PartyBattleState(
                    new[] { battle.Lilith, other });
                var tracking = new TrackingProvider();
                var executor = battle.CreateExecutor(party);
                var postBoss = new PostBossCombatExecutor(
                    party,
                    battle.Boss,
                    executor,
                    battle.ActionIds,
                    new[]
                    {
                        new PostBossActionProviderBinding(
                            battle.Lilith,
                            battle.Provider),
                        new PostBossActionProviderBinding(other, tracking)
                    });
                var session = postBoss.BeginExecution(25);

                Assert.That(session.TryExecuteNext(out _), Is.True);
                Assert.That(session.TryCompleteCurrentProvider(
                    out CombatActionExecutionResult result), Is.True);

                Assert.That(battle.Boss.IsDefeated, Is.True);
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(tracking.CallCount, Is.Zero);
                Assert.That(result.StoppedEarly, Is.True);
                Assert.That(result.ActionResults, Has.Count.EqualTo(1));
                Assert.That(CovenantCount(battle), Is.EqualTo(4));
                var score = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());
                Assert.That(score.Collect(result), Is.EqualTo(100L));
                Assert.That(score.Collect(result), Is.Zero);
                Assert.That(score.DamageScore, Is.EqualTo(100L));
            }
        }

        [Test]
        public void LaterLilithProviderUsesKragmorStateFromEarlierProvider()
        {
            using (var battle = new TestBattle(
                partySlotIndex: 1,
                bossElement: ElementType.Dark))
            {
                battle.AddCovenant(4);
                var first = new CharacterBattleState(
                    "character_first",
                    0,
                    ElementType.Fire,
                    1000L,
                    100d);
                var party = new PartyBattleState(
                    new[] { first, battle.Lilith });
                KragmorCombatConfig kragmorConfig =
                    KragmorTestConfig.Create();
                try
                {
                    var exposureProvider = new ApplyEffectProvider(
                        KragmorDefenseEffectFactory.Create(
                            KragmorDefenseState.CoreExposure,
                            9000L,
                            kragmorConfig));
                    var postBoss = new PostBossCombatExecutor(
                        party,
                        battle.Boss,
                        battle.CreateExecutor(party),
                        battle.ActionIds,
                        new[]
                        {
                            new PostBossActionProviderBinding(
                                first,
                                exposureProvider),
                            new PostBossActionProviderBinding(
                                battle.Lilith,
                                battle.Provider)
                        });
                    var session = postBoss.BeginExecution(25);
                    DamageActionResult lilithDamage = null;

                    while (!session.IsCompleted)
                    {
                        while (session.TryExecuteNext(out var step))
                        {
                            if (step.ActionStep.Result
                                is DamageActionResult damage)
                            {
                                lilithDamage = damage;
                            }
                        }

                        session.TryCompleteCurrentProvider(out _);
                    }

                    Assert.That(exposureProvider.CallCount, Is.EqualTo(1));
                    Assert.That(lilithDamage, Is.Not.Null);
                    Assert.That(lilithDamage.Context
                            .TargetTakenDamageIncreaseRateSum,
                        Is.EqualTo(0.36d).Within(0.000000001d));
                    Assert.That(CovenantCount(battle), Is.Zero);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(kragmorConfig);
                }
            }
        }

        private static int CovenantCount(TestBattle battle)
        {
            return LilithCovenantEffectFactory.GetStackCount(battle.Boss);
        }

        private static EffectInstance Effect(
            long id,
            string effectId,
            EffectModifierType modifierType,
            double magnitude,
            ElementType? element = null,
            AttackTypeMask mask = AttackTypeMask.None,
            int? remainingTurns = null)
        {
            return new EffectInstance(
                id,
                effectId,
                "test_source",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                remainingTurns,
                id,
                elementFilter: element,
                attackTypeMask: mask);
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            private readonly float value;

            public CountingRandomSource(float value)
            {
                this.value = value;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                return minInclusive;
            }

            public float NextFloat()
            {
                NextFloatCallCount++;
                return value;
            }

            public int NextFloatCallCount { get; private set; }
        }

        private sealed class TrackingProvider : IPostBossActionProvider
        {
            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                PostBossActionContext context)
            {
                CallCount++;
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class ApplyEffectProvider : IPostBossActionProvider
        {
            private readonly EffectInstance effect;

            public ApplyEffectProvider(EffectInstance effect)
            {
                this.effect = effect;
            }

            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                PostBossActionContext context)
            {
                CallCount++;
                return new CombatAction[]
                {
                    new ApplyEffectAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        context.Boss,
                        effect)
                };
            }
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly LilithCombatConfig config;
            private readonly LilithResolvedCombatRules combatRules;
            private long nextEffectId = 10000L;

            public TestBattle(
                int awakening = 0,
                long partyHp = 10000L,
                long bossHp = 1000000000L,
                int partySlotIndex = 0,
                ElementType bossElement = ElementType.Dark)
            {
                config = LilithTestConfig.Create();
                combatRules = LilithCombatRuleResolver.Resolve(
                    config,
                    awakening);
                Lilith = new CharacterBattleState(
                    LilithRules.CharacterId,
                    partySlotIndex,
                    ElementType.Dark,
                    partyHp,
                    1000d);
                Party = new PartyBattleState(new[] { Lilith });
                Boss = new BossBattleState(
                    "training_boss",
                    bossElement,
                    bossHp,
                    1000d);
                ActionIds = new CombatActionIdSequence();
                Provider = new LilithPostBossActionProvider(
                    config,
                    awakening);
            }

            public CharacterBattleState Lilith { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public LilithPostBossActionProvider Provider { get; }

            public void AddCovenant(int count)
            {
                for (int index = 0; index < count; index++)
                {
                    Boss.Effects.ApplyEffect(
                        LilithCovenantEffectFactory.Create(
                            nextEffectId++,
                            combatRules));
                }
            }

            public IReadOnlyList<CombatAction> CreateActions()
            {
                return Provider.CreateActions(
                    Lilith,
                    Party,
                    Boss,
                    ActionIds);
            }

            public CombatActionExecutionResult ExecuteActions(
                IReadOnlyList<CombatAction> actions,
                IRandomSource random = null)
            {
                return CreateExecutor(Party, random)
                    .Execute(new CombatActionQueue(actions));
            }

            public DamageActionResult ExecuteExplosionDamage(
                IRandomSource random = null)
            {
                return ExecuteActions(CreateActions(), random)
                    .ActionResults.OfType<DamageActionResult>().Single();
            }

            public DamageActionResult ExecuteProbeDamage(
                double coefficient = 0d)
            {
                var action = new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        Lilith,
                        Party,
                        Boss,
                        ElementType.Dark,
                        AttackType.Passive,
                        AttackTag.None,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                return ExecuteActions(new CombatAction[] { action })
                    .ActionResults.OfType<DamageActionResult>().Single();
            }

            public PostBossCombatExecutor CreatePostBossExecutor()
            {
                return new PostBossCombatExecutor(
                    Party,
                    Boss,
                    CreateExecutor(Party),
                    ActionIds,
                    new[]
                    {
                        new PostBossActionProviderBinding(Lilith, Provider)
                    });
            }

            public CombatActionExecutor CreateExecutor(
                PartyBattleState party,
                IRandomSource random = null)
            {
                return new CombatActionExecutor(
                    Boss,
                    party,
                    new DamageContextFactory(
                        random ?? new SeededRandomSource(123)));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

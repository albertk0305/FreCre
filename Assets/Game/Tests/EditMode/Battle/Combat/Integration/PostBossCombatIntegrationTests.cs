using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class PostBossCombatIntegrationTests
    {
        [Test]
        public void NoProvidersPreservesExistingTurnCompletion()
        {
            TestBattle battle = CreateBattle();
            battle.Boss.Effects.ApplyEffect(new EffectInstance(
                800,
                "test_commit_order",
                "test",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                0.10d,
                1,
                800));
            int effectCountAtCommit = -1;
            battle.BossProvider.CommitObserver = () =>
                effectCountAtCommit = battle.Boss.Effects.Count;

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(effectCountAtCommit, Is.Zero);
            Assert.That(battle.BossProvider.CommitCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [Test]
        public void OneProviderRunsOnceAfterBossCommitAndBeforeTurnEnd()
        {
            var order = new List<string>();
            TestBattle battle = CreateBattle(
                createBossActions: context => Array.Empty<CombatAction>(),
                onBossCommit: () => order.Add("commit"),
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                    {
                        order.Add("post");
                        Assert.That(context.Character, Is.SameAs(character));
                        Assert.That(context.CurrentTurn, Is.EqualTo(1));
                        return Array.Empty<CombatAction>();
                    })
                });

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(order, Is.EqualTo(new[] { "commit", "post" }));
            Assert.That(battle.Providers[0].CallCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void MultipleProvidersUseSlotOrderAndLatestBossState()
        {
            var calls = new List<string>();
            long hpObservedBySecond = -1;
            TestBattle battle = CreateBattle(
                bossHp: 1000,
                partySlots: new[] { 0, 3 },
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                    {
                        calls.Add(character.CharacterId);
                        return new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        };
                    }),
                    character => new DelegatePostBossProvider(context =>
                    {
                        calls.Add(character.CharacterId);
                        hpObservedBySecond = context.Boss.CurrentHp;
                        return new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        };
                    })
                });

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(calls, Is.EqualTo(new[] { "hero_0", "hero_3" }));
            Assert.That(hpObservedBySecond, Is.EqualTo(900));
            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(800));
        }

        [Test]
        public void EmptyBossAndProviderActionsStillCompleteTurn()
        {
            TestBattle battle = CreateBattle(
                createBossActions: context => Array.Empty<CombatAction>(),
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        Array.Empty<CombatAction>())
                });

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Providers[0].CallCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(2));
        }

        [Test]
        public void BoardMutationMustCompleteBeforePostBossBegins()
        {
            var sink = new RecordingBoardMutationSink();
            var order = new List<string>();
            TestBattle battle = CreateBattle(
                boardCommand: new BattleBoardMutationCommand(
                    BattleBoardMutationKind.CreateRock,
                    requestedCount: 1,
                    maximumCount: 3),
                boardMutationSink: sink,
                onBossCommit: () => order.Add("commit"),
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                    {
                        order.Add("post");
                        return new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        };
                    })
                });

            Assert.That(battle.Bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(battle.Bridge.TryGetNextBossCombatAction(out _),
                Is.False);
            Assert.That(battle.Bridge.TryCompleteBossActionCombat(), Is.True);
            Assert.That(battle.Bridge.HasPendingBossBoardMutation, Is.True);
            Assert.That(battle.Providers[0].CallCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);

            sink.Complete(CombatIntegrationTestSupport
                .CreateSuccessfulBoardMutation(sink.LastCommand));

            Assert.That(order, Is.EqualTo(new[] { "commit", "post" }));
            Assert.That(battle.Bridge.IsPostBossCombatInProgress, Is.True);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            CompletePostBoss(battle.Bridge);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void BossLethalDamageSkipsCommitAndPostBoss()
        {
            TestBattle battle = CreateBattle(
                partyHp: 1,
                createBossActions: context => new CombatAction[]
                {
                    CombatIntegrationTestSupport.BossDamage(context, 1d)
                },
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        Array.Empty<CombatAction>())
                });

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(battle.Providers[0].CallCount, Is.Zero);
            Assert.That(battle.BossProvider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
        }

        [Test]
        public void LethalPostBossDamageWinsWithoutTurnEndAndScoresOnce()
        {
            TestBattle battle = CreateBattle(
                bossHp: 50,
                partySlots: new[] { 0, 1 },
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        }),
                    character => new DelegatePostBossProvider(context =>
                        Array.Empty<CombatAction>())
                });
            int stepCount = 0;
            battle.Bridge.PostBossCombatActionStepApplied += step =>
                stepCount++;

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(50));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(battle.Providers[1].CallCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
        }

        [Test]
        public void PostBossWaitsForExplicitActionCompletionBeforeTurnEnd()
        {
            TestBattle battle = CreateBattle(
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        })
                });

            Assert.That(battle.Bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(battle.Bridge.TryCompleteBossActionCombat(), Is.True);
            Assert.That(battle.Bridge.TryGetNextPostBossCombatAction(
                out CharacterBattleState actor,
                out CombatAction action), Is.True);
            Assert.That(actor.PartySlotIndex, Is.Zero);
            Assert.That(action, Is.TypeOf<DamageAction>());
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);

            Assert.That(battle.Bridge.TryApplyNextPostBossCombatAction(out _),
                Is.True);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Bridge.TryCompletePostBossCombatProvider(),
                Is.True);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        [Test]
        public void ExpiringBossModifierRemainsActiveForPostBossDamage()
        {
            TestBattle battle = CreateBattle(
                bossHp: 1000,
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        new CombatAction[]
                        {
                            PassiveDamage(context, 1d)
                        })
                });
            battle.Boss.Effects.ApplyEffect(new EffectInstance(
                900,
                "test_expiring_taken_damage",
                "test",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                0.50d,
                1,
                900));

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Boss.CurrentHp, Is.EqualTo(850));
            Assert.That(battle.Boss.Effects.Count, Is.Zero);
        }

        [Test]
        public void TurnLimitIsCheckedAfterPostBossCompletes()
        {
            int providerCalls = 0;
            TestBattle battle = CreateBattle(
                turnLimit: 1,
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                    {
                        providerCalls++;
                        return Array.Empty<CombatAction>();
                    })
                });

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(providerCalls, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
        }

        [Test]
        public void RegistrationDefaultsEmptyAndCanAttachOneProvider()
        {
            var provider = new DelegatePostBossProvider(context =>
                Array.Empty<CombatAction>());
            CharacterCombatProviderRegistrationResult empty =
                CharacterCombatProviderRegistrationResult
                    .WithoutActiveAbility();
            CharacterCombatProviderRegistrationResult configured = empty
                .WithPostBossActionProviders(
                    new IPostBossActionProvider[] { provider });

            Assert.That(empty.PostBossActionProviders, Is.Empty);
            Assert.That(configured.PostBossActionProviders,
                Is.EqualTo(new[] { provider }));
            Assert.That(empty.PostBossActionProviders, Is.Empty);
        }

        [Test]
        public void SeparateBattlesDoNotSharePostBossExecutionState()
        {
            TestBattle first = CreateBattle(
                providerFactories: new Func<CharacterBattleState,
                    IPostBossActionProvider>[]
                {
                    character => new DelegatePostBossProvider(context =>
                        new CombatAction[] { PassiveDamage(context, 1d) })
                });
            Assert.That(first.Bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(first.Bridge.TryCompleteBossActionCombat(), Is.True);
            Assert.That(first.Bridge.IsPostBossCombatInProgress, Is.True);
            Assert.That(first.Bridge.CancelPostBossCombat(), Is.True);

            TestBattle second = CreateBattle();

            Assert.That(second.Bridge.IsPostBossCombatInProgress, Is.False);
            Assert.That(second.Bridge.ResolveBossAction(1), Is.True);
            Assert.That(second.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
        }

        private static DamageAction PassiveDamage(
            PostBossActionContext context,
            double coefficient)
        {
            return new DamageAction(
                context.ActionIds.Next(),
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    context.Character,
                    context.Party,
                    context.Boss,
                    context.Character.Element,
                    AttackType.Passive,
                    AttackTag.None,
                    coefficient,
                    false,
                    0,
                    false));
        }

        private static void CompletePostBoss(BattleFlowCombatBridge bridge)
        {
            while (bridge.IsPostBossCombatInProgress)
            {
                while (bridge.TryGetNextPostBossCombatAction(out _, out _))
                {
                    Assert.That(
                        bridge.TryApplyNextPostBossCombatAction(out _),
                        Is.True);
                }

                Assert.That(bridge.TryCompletePostBossCombatProvider(),
                    Is.True);
            }
        }

        private static TestBattle CreateBattle(
            int turnLimit = 2,
            long partyHp = 1000,
            long bossHp = 10000,
            int[] partySlots = null,
            Func<BossCombatActionContext, IReadOnlyList<CombatAction>>
                createBossActions = null,
            Action onBossCommit = null,
            BattleBoardMutationCommand boardCommand = null,
            IBattleBoardMutationSink boardMutationSink = null,
            Func<CharacterBattleState, IPostBossActionProvider>[]
                providerFactories = null)
        {
            int[] slots = partySlots ?? new[] { 0 };
            var characters = new CharacterBattleState[slots.Length];
            for (int index = 0; index < slots.Length; index++)
            {
                characters[index] = new CharacterBattleState(
                    $"hero_{slots[index]}",
                    slots[index],
                    ElementType.Dark,
                    partyHp,
                    100d);
            }

            var party = new PartyBattleState(characters);
            var boss = new BossBattleState(
                "boss",
                ElementType.Dark,
                bossHp,
                100d);
            var coordinator = new BattleFlowCoordinator(turnLimit);
            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));

            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(Array.Empty<ICombatTriggerRule>()));
            var bossProvider = new TrackingBossProvider(
                createBossActions
                    ?? (context => Array.Empty<CombatAction>()),
                onBossCommit,
                boardCommand);
            var providers = new List<DelegatePostBossProvider>();
            var bindings = new List<PostBossActionProviderBinding>();
            Func<CharacterBattleState, IPostBossActionProvider>[] factories =
                providerFactories
                ?? Array.Empty<Func<CharacterBattleState,
                    IPostBossActionProvider>>();
            for (int index = 0; index < factories.Length; index++)
            {
                IPostBossActionProvider provider = factories[index](
                    characters[index]);
                var tracked = provider as DelegatePostBossProvider
                    ?? throw new InvalidOperationException(
                        "Test providers must use DelegatePostBossProvider.");
                providers.Add(tracked);
                bindings.Add(new PostBossActionProviderBinding(
                    characters[index],
                    provider));
            }

            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                new DelegateMatchEventActionProvider(context =>
                    Array.Empty<CombatAction>()),
                bossProvider,
                actionIds,
                Array.Empty<ActiveAbilityBinding>(),
                new ActiveAbilityActionProviderRegistry(),
                boardMutationSink,
                difficultyId: null,
                resultBalance: null,
                postBossBindings: bindings);
            return new TestBattle(
                coordinator,
                party,
                boss,
                bridge,
                bossProvider,
                providers);
        }

        private sealed class DelegatePostBossProvider
            : IPostBossActionProvider
        {
            private readonly Func<PostBossActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegatePostBossProvider(
                Func<PostBossActionContext,
                    IReadOnlyList<CombatAction>> createActions)
            {
                this.createActions = createActions
                    ?? throw new ArgumentNullException(nameof(createActions));
            }

            public int CallCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                PostBossActionContext context)
            {
                CallCount++;
                return createActions(context);
            }
        }

        private sealed class TrackingBossProvider
            : IBossCombatActionProvider,
                IBossCombatActionPlanProvider,
                IBossCombatActionCompletionHandler
        {
            private readonly Func<BossCombatActionContext,
                IReadOnlyList<CombatAction>> createActions;
            private readonly Action onCommit;
            private readonly BattleBoardMutationCommand boardCommand;

            public TrackingBossProvider(
                Func<BossCombatActionContext,
                    IReadOnlyList<CombatAction>> createActions,
                Action onCommit,
                BattleBoardMutationCommand boardCommand)
            {
                this.createActions = createActions;
                this.onCommit = onCommit;
                this.boardCommand = boardCommand;
            }

            public int CommitCount { get; private set; }
            public Action CommitObserver { get; set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return createActions(context);
            }

            public BossActionPlan CreatePlan(BossCombatActionContext context)
            {
                return new BossActionPlan(
                    createActions(context),
                    boardCommand);
            }

            public bool TryCommitCompletedAction()
            {
                CommitCount++;
                onCommit?.Invoke();
                CommitObserver?.Invoke();
                return true;
            }
        }

        private sealed class TestBattle
        {
            public TestBattle(
                BattleFlowCoordinator coordinator,
                PartyBattleState party,
                BossBattleState boss,
                BattleFlowCombatBridge bridge,
                TrackingBossProvider bossProvider,
                IReadOnlyList<DelegatePostBossProvider> providers)
            {
                Coordinator = coordinator;
                Party = party;
                Boss = boss;
                Bridge = bridge;
                BossProvider = bossProvider;
                Providers = providers;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public BattleFlowCombatBridge Bridge { get; }
            public TrackingBossProvider BossProvider { get; }
            public IReadOnlyList<DelegatePostBossProvider> Providers { get; }
        }
    }
}

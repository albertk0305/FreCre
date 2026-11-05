using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class ActiveAbilityEmptyActionIntegrationTests
    {
        [Test]
        public void OptInEmptyProviderCommitsSuccessfulUse()
        {
            var provider = new OptInEmptyProvider();
            ActiveHarness harness = CreateHarness(provider);
            provider.RuntimeStateSource = () => harness.RuntimeState;

            Assert.That(
                harness.Executor.TryBeginExecution(0, out var session),
                Is.True);

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.NextAction, Is.Null);
            Assert.That(session.Result.CompletedActionCount, Is.Zero);
            Assert.That(provider.CommitCount, Is.EqualTo(1));
            Assert.That(provider.CommitObservedUsedThisTurn, Is.True);
            Assert.That(harness.RuntimeState.UsedThisTurn, Is.True);
            Assert.That(harness.RuntimeState.RemainingCooldown,
                Is.EqualTo(1));
        }

        [Test]
        public void OrdinaryEmptyProviderRemainsRejected()
        {
            ActiveHarness harness = CreateHarness(
                new OrdinaryEmptyProvider());

            Assert.That(
                () => harness.Executor.TryBeginExecution(0, out _),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(harness.RuntimeState.CanUse, Is.True);
            Assert.That(harness.RuntimeState.UsedThisTurn, Is.False);
            Assert.That(harness.RuntimeState.RemainingCooldown, Is.Zero);
        }

        [Test]
        public void OrdinaryRootActionStillExecutesNormally()
        {
            ActiveHarness harness = CreateHarness(
                new NormalDamageProvider());

            Assert.That(
                harness.Executor.TryExecute(0, out var result),
                Is.True);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(harness.RuntimeState.UsedThisTurn, Is.True);
        }

        [Test]
        public void InvalidRootActionSkipsSuccessCommitAndCooldown()
        {
            var provider = new InvalidRootCommitProvider();
            ActiveHarness harness = CreateHarness(provider);

            Assert.That(
                () => harness.Executor.TryBeginExecution(0, out _),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(provider.CommitCount, Is.Zero);
            Assert.That(harness.RuntimeState.CanUse, Is.True);
            Assert.That(harness.RuntimeState.UsedThisTurn, Is.False);
        }

        [Test]
        public void ProviderCreationFailureSkipsSuccessCommitAndCooldown()
        {
            var provider = new ThrowingCommitProvider();
            ActiveHarness harness = CreateHarness(provider);

            Assert.That(
                () => harness.Executor.TryBeginExecution(0, out _),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(provider.CommitCount, Is.Zero);
            Assert.That(harness.RuntimeState.CanUse, Is.True);
        }

        [Test]
        public void EmptyUseBlocksSameTurnAndRecoversNextTurn()
        {
            ActiveHarness harness = CreateHarness(
                new OptInEmptyProvider());

            Assert.That(harness.Executor.TryExecute(0, out _), Is.True);
            Assert.That(harness.Executor.TryExecute(0, out _), Is.False);

            AdvanceTurn(harness.Coordinator);

            Assert.That(harness.RuntimeState.CanUse, Is.True);
            Assert.That(harness.Executor.TryExecute(0, out _), Is.True);
        }

        private static ActiveHarness CreateHarness(
            IActiveAbilityActionProvider provider)
        {
            var character = new CharacterBattleState(
                "character_test_active",
                0,
                ElementType.Fire,
                1000L,
                100d);
            var party = new PartyBattleState(
                new CharacterBattleState[] { character });
            var boss = new BossBattleState(
                "boss_test_active",
                ElementType.Fire,
                100000L,
                100d);
            var coordinator = new BattleFlowCoordinator(25, new[] { 1 });
            var actionIds = new CombatActionIdSequence();
            var registry = new ActiveAbilityActionProviderRegistry();
            registry.Register(
                character.CharacterId,
                "active_test",
                provider);
            var binding = new ActiveAbilityBinding(
                0,
                character.PartySlotIndex,
                character.CharacterId,
                "active_test");
            var actionExecutor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)));
            var executor = new ActiveAbilityCombatExecutor(
                coordinator,
                party,
                boss,
                actionExecutor,
                actionIds,
                new ActiveAbilityBinding[] { binding },
                registry);
            Assert.That(coordinator.StartBattle(), Is.True);
            return new ActiveHarness(
                coordinator,
                character,
                party,
                boss,
                actionIds,
                executor);
        }

        private static void AdvanceTurn(BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(),
                    consumesTurn: true),
                Is.True);
            coordinator.ExecuteRemainingMatchEvents();
            Assert.That(coordinator.CompleteBossAction(), Is.True);
        }

        private sealed class ActiveHarness
        {
            public ActiveHarness(
                BattleFlowCoordinator coordinator,
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                ActiveAbilityCombatExecutor executor)
            {
                Coordinator = coordinator;
                Character = character;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                Executor = executor;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public ActiveAbilityCombatExecutor Executor { get; }
            public ActiveAbilityRuntimeState RuntimeState =>
                Coordinator.Context.ActiveAbilities[0];
        }

        private sealed class OptInEmptyProvider
            : IActiveAbilityActionProvider,
              IActiveAbilityEmptyRootActionProvider,
              IActiveAbilitySuccessCommitter
        {
            public Func<ActiveAbilityRuntimeState> RuntimeStateSource
            {
                get;
                set;
            }

            public int CommitCount { get; private set; }
            public bool CommitObservedUsedThisTurn { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return Array.Empty<CombatAction>();
            }

            public void CommitActiveSuccess(
                ActiveAbilityActionContext context)
            {
                CommitCount++;
                CommitObservedUsedThisTurn = RuntimeStateSource != null
                    && RuntimeStateSource().UsedThisTurn;
            }
        }

        private sealed class OrdinaryEmptyProvider
            : IActiveAbilityActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class NormalDamageProvider
            : IActiveAbilityActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    CreateDamage(
                        context,
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        AttackType.Active)
                };
            }
        }

        private sealed class InvalidRootCommitProvider
            : IActiveAbilityActionProvider,
              IActiveAbilitySuccessCommitter
        {
            public int CommitCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    CreateDamage(
                        context,
                        context.ActionIds.Next(),
                        ActionOrigin.Match,
                        AttackType.Match)
                };
            }

            public void CommitActiveSuccess(
                ActiveAbilityActionContext context)
            {
                CommitCount++;
            }
        }

        private sealed class ThrowingCommitProvider
            : IActiveAbilityActionProvider,
              IActiveAbilitySuccessCommitter
        {
            public int CommitCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                throw new InvalidOperationException("create failed");
            }

            public void CommitActiveSuccess(
                ActiveAbilityActionContext context)
            {
                CommitCount++;
            }
        }

        private static DamageAction CreateDamage(
            ActiveAbilityActionContext context,
            long actionId,
            ActionOrigin origin,
            AttackType attackType)
        {
            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    context.Character,
                    context.Party,
                    context.Boss,
                    ElementType.Fire,
                    attackType,
                    AttackTag.None,
                    1d,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false));
        }
    }
}

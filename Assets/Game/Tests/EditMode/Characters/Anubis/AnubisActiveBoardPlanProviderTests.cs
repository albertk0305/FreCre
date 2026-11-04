using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Anubis;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Anubis
{
    public sealed class AnubisActiveBoardPlanProviderTests
    {
        [TestCase(0, 4)]
        [TestCase(5, 4)]
        [TestCase(6, 5)]
        public void SuccessfulUseRequestsLightConversionCount(
            int awakening,
            int expectedCount)
        {
            using (var battle = new TestBattle(awakening, succeeds: true))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.True);
                Assert.That(battle.Sink.Target, Is.EqualTo(ElementType.Light));
                Assert.That(battle.Sink.RequestedCount,
                    Is.EqualTo(expectedCount));
                Assert.That(battle.ActiveState.RemainingCooldown,
                    Is.EqualTo(8));
            }
        }

        [Test]
        public void FailedConversionRollsBackCooldownAndUse()
        {
            using (var battle = new TestBattle(6, succeeds: false))
            {
                Assert.That(battle.Executor.TryExecute(0, out _), Is.False);
                Assert.That(battle.ActiveState.UsedThisTurn, Is.False);
                Assert.That(battle.ActiveState.RemainingCooldown, Is.Zero);
            }
        }

        private sealed class TestBattle : System.IDisposable
        {
            private readonly AnubisCombatConfig config;

            public TestBattle(int awakening, bool succeeds)
            {
                config = AnubisTestContent.CreateConfig();
                var character = new CharacterBattleState(
                    AnubisRules.CharacterId,
                    0,
                    ElementType.Light,
                    1100,
                    120d);
                var party = new PartyBattleState(new[] { character });
                var boss = new BossBattleState(
                    "anubis_active_test_boss",
                    ElementType.Fire,
                    100000,
                    100d);
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.RegisterBoard(
                    AnubisRules.CharacterId,
                    AnubisRules.ActiveAbilityId,
                    new AnubisActiveBoardPlanProvider(config, awakening));
                Coordinator = new BattleFlowCoordinator(25, new[] { 8 });
                Sink = new RecordingSink(succeeds);
                Executor = new ActiveAbilityCombatExecutor(
                    Coordinator,
                    party,
                    boss,
                    new CombatActionExecutor(
                        boss,
                        party,
                        new DamageContextFactory(new SeededRandomSource(1))),
                    new CombatActionIdSequence(),
                    new[]
                    {
                        new ActiveAbilityBinding(
                            0,
                            0,
                            AnubisRules.CharacterId,
                            AnubisRules.ActiveAbilityId)
                    },
                    registry,
                    Sink);
                Coordinator.StartBattle();
            }

            public BattleFlowCoordinator Coordinator { get; }
            public ActiveAbilityCombatExecutor Executor { get; }
            public RecordingSink Sink { get; }
            public ActiveAbilityRuntimeState ActiveState =>
                Coordinator.Context.ActiveAbilities[0];

            public void Dispose()
            {
                Object.DestroyImmediate(config);
            }
        }

        private sealed class RecordingSink : IBattleBoardElementConversionSink
        {
            private static readonly int[] BoardElements =
            {
                3, 0, 2, 1, 0,
                0, 2, 3, 1, 2,
                4, 4, 0, 2, 4,
                1, 2, 0, 0, 4,
                0, 1, 2, 1, 2,
                0, 4, 4, 0, 3
            };

            private readonly bool succeeds;

            public RecordingSink(bool succeeds)
            {
                this.succeeds = succeeds;
            }

            public bool CanAcceptElementConversion => true;
            public ElementType Target { get; private set; }
            public int RequestedCount { get; private set; }

            public bool TryConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardElementConversionResult result)
            {
                Target = targetElement;
                RequestedCount = requestedCount;
                if (!succeeds)
                {
                    result = null;
                    return false;
                }

                result = new BoardElementConversionResolver(
                    new SeededRandomSource(7),
                    new BoardMoveAnalyzer()).Resolve(
                        CreateBoard(),
                        targetElement,
                        requestedCount);
                return result.Succeeded;
            }

            private static BoardState CreateBoard()
            {
                var board = new BoardState();
                for (int index = 0;
                    index < BoardConstants.CellCount;
                    index++)
                {
                    board.Set(
                        BoardPosition.FromIndex(index),
                        new BoardBlock(
                            index + 1,
                            BoardBlockType.Normal,
                            (ElementType)BoardElements[index]));
                }

                return board;
            }
        }
    }
}

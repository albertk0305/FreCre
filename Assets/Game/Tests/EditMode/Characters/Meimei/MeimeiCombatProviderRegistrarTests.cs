using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiCombatProviderRegistrarTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void RegistersMatchCompletionAndCompositeBoardActive()
        {
            RegisteredBattle battle = RegisterBattle(awakening: 0);

            Assert.That(battle.Registration.HasActiveAbility, Is.True);
            Assert.That(battle.Registration.ActiveAbilityId,
                Is.EqualTo(MeimeiRules.ActiveAbilityId));
            Assert.That(battle.Registration.ActiveCooldownTurns,
                Is.EqualTo(1));
            Assert.That(battle.Registration.TriggerRules,
                Has.Count.EqualTo(1));
            Assert.That(battle.MatchProviders.Count, Is.EqualTo(1));
            Assert.That(battle.ActiveProviders.Count, Is.EqualTo(1));
            Assert.That(
                battle.ActiveProviders
                    .TryResolveBoardShuffleElementConversion(
                        MeimeiRules.CharacterId,
                        MeimeiRules.ActiveAbilityId,
                        out IActiveAbilityBoardShuffleElementConversionPlanProvider
                            activeProvider),
                Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<MeimeiActiveBoardPlanProvider>());
            Assert.That(battle.Registration.CombatStatusSources,
                Has.Count.EqualTo(4));
            Assert.That(battle.Registration.CombatStatusSources
                    .Select(item => item.StatusId),
                Is.EqualTo(new[]
                {
                    MeimeiRules.FirstCupStatusId,
                    MeimeiRules.SecondCupStatusId,
                    MeimeiRules.FinalCupStatusId,
                    MeimeiRules.TipsinessStatusId
                }));
            ICharacterCombatStatusSource firstCup =
                battle.Registration.CombatStatusSources[0];
            ICharacterCombatStatusSource secondCup =
                battle.Registration.CombatStatusSources[1];
            ICharacterCombatStatusSource finalCup =
                battle.Registration.CombatStatusSources[2];
            ICharacterCombatStatusSource tipsiness =
                battle.Registration.CombatStatusSources[3];
            Assert.That(firstCup.IsVisible, Is.False);
            Assert.That(secondCup.IsVisible, Is.False);
            Assert.That(finalCup.IsVisible, Is.False);
            Assert.That(tipsiness.IsVisible, Is.True);
            Assert.That(tipsiness.CurrentCount, Is.Zero);

            Assert.That(AdditionalResults(battle.ExecuteMatch(
                    BoardMatchTier.Three)),
                Is.Empty);
            Assert.That(firstCup.IsVisible, Is.True);
            Assert.That(secondCup.IsVisible, Is.False);
            Assert.That(AdditionalResults(battle.ExecuteMatch(
                    BoardMatchTier.Four)),
                Is.Empty);
            Assert.That(secondCup.IsVisible, Is.True);
            Assert.That(tipsiness.CurrentCount, Is.EqualTo(1));
            DamageActionResult[] additional = AdditionalResults(
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore));

            Assert.That(additional, Has.Length.EqualTo(1));
            Assert.That(additional[0].Context.SkillCoefficient,
                Is.EqualTo(10d));
            Assert.That(firstCup.IsVisible, Is.False);
            Assert.That(secondCup.IsVisible, Is.False);
            Assert.That(finalCup.IsVisible, Is.False);
            Assert.That(tipsiness.CurrentCount, Is.EqualTo(3));
        }

        [Test]
        public void SeparateRegistrationsDoNotShareCups()
        {
            RegisteredBattle first = RegisterBattle(awakening: 0);
            RegisteredBattle second = RegisterBattle(awakening: 0);
            first.ExecuteMatch(BoardMatchTier.Three);
            first.ExecuteMatch(BoardMatchTier.Four);

            Assert.That(first.Registration.CombatStatusSources[0].IsVisible,
                Is.True);
            Assert.That(first.Registration.CombatStatusSources[3]
                    .CurrentCount,
                Is.EqualTo(1));
            Assert.That(second.Registration.CombatStatusSources[0].IsVisible,
                Is.False);
            Assert.That(second.Registration.CombatStatusSources[3]
                    .CurrentCount,
                Is.Zero);

            Assert.That(AdditionalResults(second.ExecuteMatch(
                    BoardMatchTier.FiveOrMore)),
                Is.Empty);
            Assert.That(AdditionalResults(first.ExecuteMatch(
                    BoardMatchTier.FiveOrMore)),
                Has.Length.EqualTo(1));
        }

        [Test]
        public void AwakeningRulesFlowThroughRegisteredProviderAndTrigger()
        {
            RegisteredBattle battle = RegisterBattle(awakening: 5);

            DamageActionResult match = MatchResult(
                battle.ExecuteMatch(BoardMatchTier.Three));
            battle.ExecuteMatch(BoardMatchTier.Four);
            DamageActionResult additional = AdditionalResults(
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore)).Single();

            Assert.That(match.Context.SkillCoefficient,
                Is.EqualTo(1.40d));
            Assert.That(additional.Context.SkillCoefficient,
                Is.EqualTo(12.00d));
        }

        [Test]
        public void ActiveUsesSharedTipsinessConsumesActualAndSnapshotsTurn()
        {
            RegisteredBattle battle = RegisterBattle(awakening: 6);
            battle.ExecuteMatch(BoardMatchTier.Four);
            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
            var sink = new CompositeBoardSink(
                CreateBoard(),
                seed: 23);

            Assert.That(battle.TryExecuteActive(
                sink,
                out BattleFlowCoordinator coordinator), Is.True);

            Assert.That(sink.LastTargetElement, Is.EqualTo(ElementType.Fire));
            Assert.That(sink.LastRequestedCount, Is.EqualTo(3));
            Assert.That(sink.LastResult.Succeeded, Is.True);
            Assert.That(battle.Registration.CombatStatusSources[3]
                    .CurrentCount,
                Is.EqualTo(3 - sink.LastResult.ConvertedCount));
            Assert.That(coordinator.Context.ActiveAbilities[0].UsedThisTurn,
                Is.True);
            Assert.That(
                coordinator.Context.ActiveAbilities[0].RemainingCooldown,
                Is.EqualTo(1));
            Assert.That(battle.TryExecuteActive(sink, out _), Is.False);

            DamageAction sameTurn = battle.CreateMatchAction(
                BoardMatchTier.Three,
                currentTurn: 1);
            DamageAction nextTurn = battle.CreateMatchAction(
                BoardMatchTier.Three,
                currentTurn: 2);
            Assert.That(sameTurn.ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.18d).Within(1e-12));
            Assert.That(nextTurn.ContextRequest.BaseCriticalChance,
                Is.EqualTo(
                    (3 - sink.LastResult.ConvertedCount) * 0.06d)
                    .Within(1e-12));

            BattleFlowTestSupport.CompleteTurnWithSingleMatch(coordinator);
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(coordinator.Context.ActiveAbilities[0].CanUse,
                Is.True);
        }

        [Test]
        public void ActiveBoardFailurePreservesTipsinessSnapshotAndCooldown()
        {
            RegisteredBattle battle = RegisterBattle(awakening: 6);
            battle.ExecuteMatch(BoardMatchTier.Four);
            battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
            var sink = new CompositeBoardSink(CreateBoard(), seed: 23)
            {
                ReturnFalse = true
            };

            Assert.That(battle.TryExecuteActive(
                sink,
                out BattleFlowCoordinator coordinator), Is.False);

            Assert.That(coordinator.Context.ActiveAbilities[0].UsedThisTurn,
                Is.False);
            Assert.That(
                coordinator.Context.ActiveAbilities[0].RemainingCooldown,
                Is.Zero);
            Assert.That(battle.Registration.CombatStatusSources[3]
                    .CurrentCount,
                Is.EqualTo(3));
            Assert.That(battle.CreateMatchAction(
                    BoardMatchTier.Three,
                    currentTurn: 2).ContextRequest.BaseCriticalChance,
                Is.EqualTo(0.18d).Within(1e-12));
        }

        [Test]
        public void RegisterRejectsMissingWrongAndInvalidConfig()
        {
            var registrar = new MeimeiCombatProviderRegistrar();
            CharacterBattleState meimei = Character();
            BossBattleState boss = Boss();
            var ids = new CombatActionIdSequence();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            CharacterDefinition missing = Definition(null);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(missing, 0),
                    meimei,
                    boss,
                    ids,
                    matches,
                    actives));

            var invalid = MeimeiTestConfig.Create();
            createdObjects.Add(invalid);
            MeimeiTestConfig.SetInt(
                invalid,
                "tipsinessMaxAmount",
                6);
            CharacterDefinition invalidDefinition = Definition(invalid);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(invalidDefinition, 0),
                    meimei,
                    boss,
                    ids,
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));

            CharacterDefinition valid = Definition(Config());
            var wrong = new CharacterBattleState(
                "character_other",
                0,
                ElementType.Fire,
                900,
                180d);
            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(valid, 0),
                    wrong,
                    boss,
                    ids,
                    new MatchEventActionProviderRegistry(),
                    new ActiveAbilityActionProviderRegistry()));
        }

        private RegisteredBattle RegisterBattle(int awakening)
        {
            MeimeiCombatConfig config = Config();
            CharacterDefinition definition = Definition(config);
            CharacterBattleState meimei = Character();
            var party = new PartyBattleState(new[] { meimei });
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult registration =
                new MeimeiCombatProviderRegistrar().Register(
                    Member(definition, awakening),
                    meimei,
                    boss,
                    actionIds,
                    matches,
                    actives);
            Assert.That(matches.TryResolve(
                MeimeiRules.CharacterId,
                out IMatchEventActionProvider provider), Is.True);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(registration.TriggerRules));
            return new RegisteredBattle(
                party,
                boss,
                actionIds,
                matches,
                actives,
                registration,
                (MeimeiMatchActionProvider)provider,
                executor,
                meimei);
        }

        private MeimeiCombatConfig Config()
        {
            MeimeiCombatConfig config = MeimeiTestConfig.Create();
            createdObjects.Add(config);
            return config;
        }

        private CharacterDefinition Definition(
            CombatConfigDefinition config)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue =
                MeimeiRules.CharacterId;
            serialized.FindProperty("element").enumValueIndex =
                (int)ElementType.Fire;
            serialized.FindProperty("level1Hp").intValue = 900;
            serialized.FindProperty("level1Attack").intValue = 180;
            serialized.FindProperty("level100Hp").intValue = 3400;
            serialized.FindProperty("level100Attack").intValue = 1050;
            serialized.FindProperty("combatConfig").objectReferenceValue =
                config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static BattlePartyMemberInput Member(
            CharacterDefinition definition,
            int awakening)
        {
            return new BattlePartyMemberInput(
                MeimeiRules.CharacterId,
                0,
                1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                MeimeiRules.CharacterId,
                0,
                ElementType.Fire,
                900,
                1000d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "meimei_registrar_test_boss",
                ElementType.Light,
                1000000,
                100d);
        }

        private static BoardState CreateBoard()
        {
            int[] elements =
            {
                3, 0, 2, 1, 0,
                0, 2, 3, 1, 2,
                4, 4, 0, 2, 4,
                1, 2, 0, 0, 4,
                0, 1, 2, 1, 2,
                0, 4, 4, 0, 3
            };
            var board = new BoardState();
            for (int index = 0; index < elements.Length; index++)
            {
                board.Set(
                    BoardPosition.FromIndex(index),
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[index]));
            }

            return board;
        }

        private static DamageActionResult[] AdditionalResults(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Where(item =>
                    item.Action.Origin == ActionOrigin.Additional)
                .ToArray();
        }

        private static DamageActionResult MatchResult(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Single(item => item.Action.Origin == ActionOrigin.Match);
        }

        private sealed class RegisteredBattle
        {
            private readonly PartyBattleState party;
            private readonly BossBattleState boss;
            private readonly CombatActionIdSequence actionIds;
            private readonly MeimeiMatchActionProvider provider;
            private readonly CombatActionExecutor executor;
            private readonly CharacterBattleState meimei;
            private BattleFlowCoordinator activeCoordinator;
            private ActiveAbilityCombatExecutor activeAbilityExecutor;
            private IBattleBoardShuffleElementConversionSink activeSink;

            public RegisteredBattle(
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders,
                CharacterCombatProviderRegistrationResult registration,
                MeimeiMatchActionProvider provider,
                CombatActionExecutor executor,
                CharacterBattleState meimei)
            {
                this.party = party;
                this.boss = boss;
                this.actionIds = actionIds;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Registration = registration;
                this.provider = provider;
                this.executor = executor;
                this.meimei = meimei;
            }

            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders
            {
                get;
            }
            public CharacterCombatProviderRegistrationResult Registration
            {
                get;
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier)
            {
                return executor.Execute(new CombatActionQueue(
                    provider.CreateActions(
                        meimei,
                        party,
                        boss,
                        tier,
                        1,
                        actionIds)));
            }

            public DamageAction CreateMatchAction(
                BoardMatchTier tier,
                int currentTurn)
            {
                return (DamageAction)provider.CreateActions(
                    meimei,
                    party,
                    boss,
                    tier,
                    1,
                    actionIds,
                    currentTurn)[0];
            }

            public bool TryExecuteActive(
                IBattleBoardShuffleElementConversionSink sink,
                out BattleFlowCoordinator coordinator)
            {
                if (activeAbilityExecutor == null)
                {
                    activeSink = sink
                        ?? throw new ArgumentNullException(nameof(sink));
                    activeCoordinator = new BattleFlowCoordinator(
                        25,
                        new[] { Registration.ActiveCooldownTurns });
                    activeAbilityExecutor = new ActiveAbilityCombatExecutor(
                        activeCoordinator,
                        party,
                        boss,
                        executor,
                        actionIds,
                        new[]
                        {
                            new ActiveAbilityBinding(
                                0,
                                meimei.PartySlotIndex,
                                MeimeiRules.CharacterId,
                                MeimeiRules.ActiveAbilityId)
                        },
                        ActiveProviders,
                        boardElementConversionSink: null,
                        boardShuffleElementConversionSink: activeSink);
                    activeCoordinator.StartBattle();
                }
                else if (!ReferenceEquals(activeSink, sink))
                {
                    throw new InvalidOperationException(
                        "Registered test battle uses one Board sink.");
                }

                coordinator = activeCoordinator;
                return activeAbilityExecutor.TryExecute(0, out _);
            }
        }

        private sealed class CompositeBoardSink
            : IBattleBoardShuffleElementConversionSink
        {
            private readonly int seed;

            public CompositeBoardSink(BoardState board, int seed)
            {
                Board = board;
                this.seed = seed;
            }

            public bool CanAcceptShuffleElementConversion => true;
            public bool ReturnFalse { get; set; }
            public BoardState Board { get; private set; }
            public ElementType LastTargetElement { get; private set; }
            public int LastRequestedCount { get; private set; }
            public BoardShuffleElementConversionResult LastResult
            {
                get;
                private set;
            }

            public bool TryShuffleAndConvertElements(
                ElementType targetElement,
                int requestedCount,
                out BoardShuffleElementConversionResult result)
            {
                LastTargetElement = targetElement;
                LastRequestedCount = requestedCount;
                if (ReturnFalse)
                {
                    result = null;
                    return false;
                }

                result = new BoardShuffleElementConversionResolver(
                    new SeededRandomSource(seed),
                    new BoardMoveAnalyzer()).Resolve(
                        Board,
                        targetElement,
                        requestedCount);
                LastResult = result;
                if (!result.Succeeded)
                {
                    return false;
                }

                Board = result.Board;
                return true;
            }
        }
    }
}

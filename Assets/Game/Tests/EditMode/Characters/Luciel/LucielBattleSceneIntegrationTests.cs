using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Characters.Shizuku;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielBattleSceneIntegrationTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";

        private readonly List<BattleSceneCombatComposition> compositions =
            new List<BattleSceneCombatComposition>();

        [TearDown]
        public void TearDown()
        {
            for (int index = compositions.Count - 1; index >= 0; index--)
            {
                compositions[index].Dispose();
            }

            compositions.Clear();
        }

        [Test]
        public void OwnedPresetProgressFlowsIntoProductionComposition()
        {
            DefinitionDatabase database = Database();
            var profile = new NewProfileFactory().Create(
                "luciel_integration",
                utcUnixSeconds: 1);
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = LucielRules.CharacterId,
                Level = 100,
                Awakening = 6
            });
            profile.Party.Presets[0].CharacterSlotIds[2] =
                LucielRules.CharacterId;

            BattlePartyResolutionResult resolution =
                new BattlePartyResolver(database).Resolve(profile);

            Assert.That(resolution.IsSuccess, Is.True,
                resolution.ErrorMessage);
            BattlePartyMemberInput input = resolution.Members.Single();
            Assert.That(input.CharacterId,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(input.PartySlotIndex, Is.EqualTo(2));
            Assert.That(input.Level, Is.EqualTo(100));
            Assert.That(input.Awakening, Is.EqualTo(6));
            Assert.That(input.CharacterDefinition.CombatConfig,
                Is.TypeOf<LucielCombatConfig>());

            BattleSceneCombatComposition composition = CreateComposition(
                resolution.Members);
            CharacterBattleState luciel = composition.Party.Characters
                .Single();
            Assert.That(luciel.PartySlotIndex, Is.EqualTo(2));
            Assert.That(luciel.MaxHp, Is.EqualTo(6300L));
            Assert.That(luciel.Attack, Is.EqualTo(375d));
            Assert.That(composition.MatchProviders.TryResolve(
                LucielRules.CharacterId,
                out _), Is.True);
            Assert.That(composition.ActiveProviders.TryResolve(
                LucielRules.CharacterId,
                LucielRules.ActiveAbilityId,
                out _), Is.True);
            Assert.That(composition.ActiveBindings.Single().CharacterId,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(composition.FlowSetup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 8 }));

            EffectInstance passive = composition.Party.Effects
                .FindByEffectId(LucielRules.PassiveEffectId).Single();
            Assert.That(passive.Magnitude, Is.EqualTo(0.06d));
            Assert.That(passive.RemainingTurns, Is.Null);
            Assert.That(passive.ModifierType,
                Is.EqualTo(EffectModifierType.PartyDamageReduction));

            BossDamageActionResult damage = ExecuteBossHit(composition);
            Assert.That(damage.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.06d));
            Assert.That(damage.DamageResult
                    .EffectivePartyDamageReductionRate,
                Is.EqualTo(0.06d));
        }

        [Test]
        public void PartyWithoutLucielHasNoLucielPassiveOrProviders()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(
                    NairaWindhornRules.CharacterId,
                    level: 100,
                    awakening: 6,
                    partySlotIndex: 0));

            Assert.That(composition.Party.Effects.FindByEffectId(
                LucielRules.PassiveEffectId), Is.Empty);
            Assert.That(composition.MatchProviders.TryResolve(
                LucielRules.CharacterId,
                out _), Is.False);
            Assert.That(composition.ActiveProviders.TryResolve(
                LucielRules.CharacterId,
                LucielRules.ActiveAbilityId,
                out _), Is.False);
            Assert.That(ExecuteBossHit(composition).Context
                .PartyDamageReductionRateSum, Is.Zero);
        }

        [Test]
        public void Match5BuffAppliesBeforeLaterSlotDamage()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(LucielRules.CharacterId, 100, 6, 0),
                Member(KarasumaShizukuRules.CharacterId, 100, 0, 1));
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Dark,
                blockCount: 5)), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(ShieldAction),
                    typeof(ApplyEffectAction),
                    typeof(DamageAction),
                    typeof(ApplyEffectAction)
                }));
            ShieldActionResult shield = bridge.LastMatchExecutionResult
                .ActionResults.OfType<ShieldActionResult>().Single();
            DamageActionResult laterDamage = bridge
                .LastMatchExecutionResult.ActionResults
                .OfType<DamageActionResult>().Single();
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(1323L));
            Assert.That(shield.GenerationResult.SourceMaxHp,
                Is.EqualTo(6300L));
            Assert.That(shield.GenerationResult.ComboMultiplier,
                Is.EqualTo(1d));
            Assert.That(shield.GenerationResult
                .ShieldAmountIncreaseMultiplier, Is.EqualTo(1.05d));
            Assert.That(laterDamage.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.14d));

            EffectInstance buff = composition.Party.Effects.FindByEffectId(
                LucielRules.Match5DamageIncreaseEffectId).Single();
            Assert.That(buff.Magnitude, Is.EqualTo(0.14d));
            Assert.That(buff.RemainingTurns, Is.EqualTo(3));
            Assert.That(buff.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(composition.Party.Shields.ActiveShields.Single()
                .RemainingTurns, Is.Null);
        }

        [Test]
        public void Match5BuffDoesNotApplyRetroactivelyToEarlierSlotDamage()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(KarasumaShizukuRules.CharacterId, 100, 0, 0),
                Member(LucielRules.CharacterId, 100, 6, 1));
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(bridge.ResolveMatchEvent(BeginMatch(
                bridge.Coordinator,
                ElementType.Dark,
                blockCount: 5)), Is.True);

            Assert.That(bridge.LastMatchExecutionResult.ActionResults
                    .Select(result => result.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DamageAction),
                    typeof(ApplyEffectAction),
                    typeof(ShieldAction),
                    typeof(ApplyEffectAction)
                }));
            DamageActionResult earlierDamage = bridge
                .LastMatchExecutionResult.ActionResults
                .OfType<DamageActionResult>().Single();
            Assert.That(earlierDamage.Context.DealtDamageIncreaseRateSum,
                Is.Zero);
            Assert.That(composition.Party.Effects.FindByEffectId(
                    LucielRules.Match5DamageIncreaseEffectId),
                Has.Count.EqualTo(1));
        }

        [Test]
        public void ActiveUsesCeilingCostThenShieldAndStartsCooldown()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(LucielRules.CharacterId, 100, 6, 0),
                Member(NairaWindhornRules.CharacterId, 100, 6, 1));
            BattleFlowCombatBridge bridge = Start(composition);

            Assert.That(composition.Party.MaxHp, Is.EqualTo(12810L));
            Assert.That(bridge.CanUseActive(0), Is.True);
            Assert.That(bridge.TryUseActive(0), Is.True);

            Assert.That(bridge.LastActiveExecutionResult.ActionResults
                    .Select(result => result.Action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(PartyHpCostAction),
                    typeof(ShieldAction)
                }));
            var cost = (PartyHpCostActionResult)bridge
                .LastActiveExecutionResult.ActionResults[0];
            var shield = (ShieldActionResult)bridge
                .LastActiveExecutionResult.ActionResults[1];
            Assert.That(cost.RequestedHpCost, Is.EqualTo(641L));
            Assert.That(cost.AppliedHpCost, Is.EqualTo(641L));
            Assert.That(cost.HpAfter, Is.EqualTo(12169L));
            Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(3439L));
            Assert.That(shield.GenerationResult.SourceMaxHp,
                Is.EqualTo(6300L));
            Assert.That(shield.GenerationResult.ComboMultiplier,
                Is.EqualTo(1d));
            Assert.That(shield.GenerationResult
                .ShieldAmountIncreaseMultiplier, Is.EqualTo(1.05d));
            Assert.That(composition.Party.Shields.ActiveShields.Single()
                .RemainingTurns, Is.Null);
            Assert.That(bridge.Coordinator.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(8));
            Assert.That(bridge.Coordinator.Context.ActiveAbilities[0]
                .UsedThisTurn, Is.True);
            Assert.That(bridge.TryUseActive(0), Is.False);
        }

        [Test]
        public void LucielAndNairaCreateIndependentShieldInstances()
        {
            BattleSceneCombatComposition composition = CreateComposition(
                Member(LucielRules.CharacterId, 100, 6, 0),
                Member(NairaWindhornRules.CharacterId, 100, 6, 1));
            BattleFlowCombatBridge bridge = Start(composition);
            MatchEventExecution first = BeginTwoMatches(
                bridge.Coordinator,
                ElementType.Dark,
                ElementType.Grass);

            Assert.That(bridge.ResolveMatchEvent(first), Is.True);
            ShieldActionResult lucielShield = bridge
                .LastMatchExecutionResult.ActionResults
                .OfType<ShieldActionResult>().Single();
            Assert.That(bridge.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution second), Is.True);
            Assert.That(bridge.ResolveMatchEvent(second), Is.True);
            ShieldActionResult nairaShield = bridge
                .LastMatchExecutionResult.ActionResults
                .OfType<ShieldActionResult>().Single();

            Assert.That(lucielShield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(486L));
            Assert.That(lucielShield.Context
                .ShieldAmountIncreaseRateSum, Is.EqualTo(0.05d));
            Assert.That(nairaShield.ApplicationResult.GrantedShieldAmount,
                Is.EqualTo(546L));
            Assert.That(nairaShield.Context
                .ShieldAmountIncreaseRateSum, Is.Zero);
            Assert.That(composition.Party.Shields.TotalShield,
                Is.EqualTo(1032L));
            Assert.That(composition.Party.Shields.ActiveShields,
                Has.Count.EqualTo(2));
            ShieldInstance lucielInstance = composition.Party.Shields
                .ActiveShields.Single(item => item.SourceId
                    == LucielRules.Match3SkillId);
            ShieldInstance nairaInstance = composition.Party.Shields
                .ActiveShields.Single(item => item.SourceId
                    == NairaWindhornRules.Match3AbilityId);
            Assert.That(lucielInstance.RemainingTurns, Is.Null);
            Assert.That(nairaInstance.RemainingTurns, Is.EqualTo(3));
            Assert.That(lucielInstance.RuntimeId,
                Is.Not.EqualTo(nairaInstance.RuntimeId));
        }

        [Test]
        public void NewCompositionDoesNotReuseShieldOrMatchBuffState()
        {
            BattleSceneCombatComposition first = CreateComposition(
                Member(LucielRules.CharacterId, 100, 6, 0));
            BattleFlowCombatBridge firstBridge = Start(first);
            Assert.That(firstBridge.ResolveMatchEvent(BeginMatch(
                firstBridge.Coordinator,
                ElementType.Dark,
                blockCount: 5)), Is.True);

            BattleSceneCombatComposition second = CreateComposition(
                Member(LucielRules.CharacterId, 100, 6, 0));

            Assert.That(first.Party.Shields.TotalShield,
                Is.GreaterThan(0L));
            Assert.That(first.Party.Effects.FindByEffectId(
                    LucielRules.Match5DamageIncreaseEffectId),
                Has.Count.EqualTo(1));
            Assert.That(second.Party.Shields.TotalShield, Is.Zero);
            Assert.That(second.Party.Effects.FindByEffectId(
                LucielRules.Match5DamageIncreaseEffectId), Is.Empty);
            EffectInstance firstPassive = first.Party.Effects
                .FindByEffectId(LucielRules.PassiveEffectId).Single();
            EffectInstance secondPassive = second.Party.Effects
                .FindByEffectId(LucielRules.PassiveEffectId).Single();
            Assert.That(firstPassive, Is.Not.SameAs(secondPassive));
            Assert.That(first.Party.Shields,
                Is.Not.SameAs(second.Party.Shields));
            Assert.That(first.Party.Effects,
                Is.Not.SameAs(second.Party.Effects));
        }

        private BattleSceneCombatComposition CreateComposition(
            params BattlePartyMemberInput[] members)
        {
            return CreateComposition(
                (IReadOnlyList<BattlePartyMemberInput>)members);
        }

        private BattleSceneCombatComposition CreateComposition(
            IReadOnlyList<BattlePartyMemberInput> members)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetBoss(
                KragmorRules.BossId,
                out BossDefinition boss), Is.True);
            BossDifficultyStats difficulty = boss.DifficultyStats[0];
            var composition = new BattleSceneCombatComposition(
                members,
                CharacterRuntimeRegistrationCatalog.CreateDefault()
                    .CreateCombatRegistrarCatalog(),
                boss,
                (KragmorCombatConfig)boss.CombatConfig,
                difficulty,
                new SeededRandomSource(24680),
                database.BattleResultBalance.CreateBalance());
            compositions.Add(composition);
            return composition;
        }

        private static BattlePartyMemberInput Member(
            string characterId,
            int level,
            int awakening,
            int partySlotIndex)
        {
            DefinitionDatabase database = Database();
            Assert.That(database.TryGetCharacter(
                characterId,
                out CharacterDefinition definition), Is.True);
            return new BattlePartyMemberInput(
                definition.Id,
                partySlotIndex,
                level,
                awakening,
                definition);
        }

        private static BattleFlowCombatBridge Start(
            BattleSceneCombatComposition composition)
        {
            var coordinator = new BattleFlowCoordinator(
                composition.FlowSetup.TurnLimit,
                composition.FlowSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge bridge = composition.CreateBridge(
                coordinator);
            Assert.That(coordinator.StartBattle(), Is.True);
            return bridge;
        }

        private static MatchEventExecution BeginMatch(
            BattleFlowCoordinator coordinator,
            ElementType element,
            int blockCount)
        {
            var positions = new BoardPosition[blockCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = new BoardPosition(index, 0);
            }

            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(element, positions)
                    }),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            return execution;
        }

        private static MatchEventExecution BeginTwoMatches(
            BattleFlowCoordinator coordinator,
            ElementType firstElement,
            ElementType secondElement)
        {
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            firstElement,
                            new BoardPosition(0, 0),
                            new BoardPosition(1, 0),
                            new BoardPosition(2, 0)),
                        BattleFlowTestSupport.Match(
                            secondElement,
                            new BoardPosition(0, 1),
                            new BoardPosition(1, 1),
                            new BoardPosition(2, 1))
                    }),
                true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution), Is.True);
            return execution;
        }

        private static BossDamageActionResult ExecuteBossHit(
            BattleSceneCombatComposition composition)
        {
            long actionId = composition.ActionIds.Next();
            CombatActionExecutionResult execution = composition.Executor
                .Execute(new CombatActionQueue(
                    new CombatAction[]
                    {
                        new BossDamageAction(
                            actionId,
                            new BossDamageContextBuildRequest(
                                composition.Boss,
                                composition.Party,
                                attackCoefficient: 1d,
                                attackTags: AttackTag.None))
                    }));
            return execution.ActionResults.OfType<BossDamageActionResult>()
                .Single();
        }

        private static DefinitionDatabase Database()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null, DatabasePath);
            database.Initialize();
            return database;
        }
    }
}


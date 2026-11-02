using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Luciel;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Characters.Luciel
{
    public sealed class LucielCombatProviderRegistrarTests
    {
        private LucielCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = LucielTestConfig.Create();
            definition = LucielTestConfig.Definition(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(config);
        }

        [TestCase(0, 0.05d)]
        [TestCase(4, 0.05d)]
        [TestCase(5, 0.06d)]
        [TestCase(6, 0.06d)]
        public void RegistrationDeclaresIndefiniteUniquePartyPassive(
            int awakening,
            double expectedRate)
        {
            Registration registration = Register(awakening);
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();

            Assert.That(passive.EffectId,
                Is.EqualTo(LucielRules.PassiveEffectId));
            Assert.That(passive.SourceId,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(passive.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(passive.ModifierType,
                Is.EqualTo(EffectModifierType.PartyDamageReduction));
            Assert.That(passive.Magnitude, Is.EqualTo(expectedRate));
            Assert.That(passive.RemainingTurns, Is.Null);
            Assert.That(passive.StackPolicy,
                Is.EqualTo(EffectStackPolicy.Unique));
            Assert.That(passive.StackCount, Is.EqualTo(1));
            Assert.That(passive.MaxStackCount, Is.EqualTo(1));
            Assert.That(passive.ElementFilter, Is.Null);
            Assert.That(passive.AttackTypeMask,
                Is.EqualTo(AttackTypeMask.None));
            Assert.That(passive.RequiredAttackTags,
                Is.EqualTo(AttackTag.None));
        }

        [Test]
        public void RegistrationConnectsMatchAndActiveProviders()
        {
            Registration registration = Register(awakening: 2);

            Assert.That(registration.Registrar.CharacterId,
                Is.EqualTo(LucielRules.CharacterId));
            Assert.That(registration.Registrar.SupportedCombatConfigType,
                Is.EqualTo(typeof(LucielCombatConfig)));
            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(LucielRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(registration.Result.TriggerRules, Is.Empty);
            Assert.That(registration.Result.ResourceRequirements, Is.Empty);
            Assert.That(registration.Result.CombatStatusSources, Is.Empty);
            Assert.That(registration.MatchProviders.TryResolve(
                LucielRules.CharacterId,
                out IMatchEventActionProvider matchProvider), Is.True);
            Assert.That(matchProvider,
                Is.TypeOf<LucielMatchActionProvider>());
            Assert.That(registration.ActiveProviders.TryResolve(
                LucielRules.CharacterId,
                LucielRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeProvider), Is.True);
            Assert.That(activeProvider,
                Is.TypeOf<LucielActiveActionProvider>());
            Assert.That(((LucielActiveActionProvider)activeProvider)
                    .CooldownTurns,
                Is.EqualTo(8));
        }

        [Test]
        public void RegistrarAndBuildModifierUseSameCharacterId()
        {
            var registrar = new LucielCombatProviderRegistrar();
            var buildModifier = new LucielBuildModifier();

            Assert.That(registrar.CharacterId,
                Is.EqualTo(buildModifier.CharacterId));
            Assert.That(registrar.CharacterId,
                Is.EqualTo(LucielRules.CharacterId));
        }

        [Test]
        public void RegistrationDoesNotConsumeActionIds()
        {
            var ids = new CombatActionIdSequence();

            Register(awakening: 0, ids);

            Assert.That(ids.LastIssuedId, Is.Zero);
        }

        [Test]
        public void UniquePassiveDoesNotDuplicateAndNeverExpires()
        {
            Registration registration = Register(awakening: 0);
            var party = new PartyBattleState(
                new[] { registration.Character });
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();

            EffectInstance first = party.Effects.ApplyEffect(
                CreateEffect(passive, 100L));
            EffectInstance second = party.Effects.ApplyEffect(
                CreateEffect(passive, 101L));
            for (int turn = 0; turn < 10; turn++)
            {
                party.Effects.ProcessTurnEnd();
            }

            Assert.That(party.Effects.Count, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.RemainingTurns, Is.Null);
        }

        [Test]
        public void PassiveReducesBossDamageAndSumsWithOtherReduction()
        {
            Registration registration = Register(awakening: 0);
            var party = CreatePartyWithPassive(registration);
            party.Effects.ApplyEffect(PartyReduction(
                runtimeId: 101L,
                effectId: "effect_test_other_reduction",
                magnitude: 0.10d));

            BossDamageActionResult result = ExecuteBossDamage(
                registration,
                party);

            Assert.That(result.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.15d).Within(0.000000001d));
            Assert.That(result.DamageResult
                    .EffectivePartyDamageReductionRate,
                Is.EqualTo(0.15d).Within(0.000000001d));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(85L));
        }

        [Test]
        public void CombinedPartyReductionUsesExistingSeventyPercentCap()
        {
            Registration registration = Register(awakening: 0);
            var party = CreatePartyWithPassive(registration);
            party.Effects.ApplyEffect(PartyReduction(
                runtimeId: 101L,
                effectId: "effect_test_large_reduction",
                magnitude: 0.69d));

            BossDamageActionResult result = ExecuteBossDamage(
                registration,
                party);

            Assert.That(result.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.74d).Within(0.000000001d));
            Assert.That(result.DamageResult
                    .EffectivePartyDamageReductionRate,
                Is.EqualTo(0.70d));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(30L));
        }

        [Test]
        public void PassiveReductionIsCalculatedBeforeShieldAbsorption()
        {
            Registration registration = Register(awakening: 0);
            var party = CreatePartyWithPassive(registration);
            party.Shields.Add(new ShieldInstance(
                runtimeId: 200L,
                sourceId: "test_shield",
                initialAmount: 50L,
                createdTurn: 1,
                remainingTurns: null,
                creationOrder: 200L));

            BossDamageActionResult result = ExecuteBossDamage(
                registration,
                party);

            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(95L));
            Assert.That(result.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(50L));
            Assert.That(result.ApplicationResult.HpDamage,
                Is.EqualTo(45L));
        }

        [Test]
        public void PassiveDoesNotReduceActiveHpCost()
        {
            Registration registration = Register(awakening: 0);
            var party = CreatePartyWithPassive(registration);
            registration.ActiveProviders.TryResolve(
                LucielRules.CharacterId,
                LucielRules.ActiveAbilityId,
                out IActiveAbilityActionProvider resolved);
            var active = (LucielActiveActionProvider)resolved;
            IReadOnlyList<CombatAction> actions = active.CreateActions(
                registration.Character,
                party,
                playerTurnNumber: 1,
                registration.ActionIds);

            CombatActionExecutionResult execution = Execute(
                registration.Boss,
                party,
                actions);
            var cost = (PartyHpCostActionResult)execution.ActionResults[0];

            Assert.That(cost.RequestedHpCost, Is.EqualTo(50L));
            Assert.That(cost.AppliedHpCost, Is.EqualTo(50L));
            Assert.That(party.CurrentHp, Is.EqualTo(950L));
            Assert.That(party.Effects.FindByEffectId(
                    LucielRules.PassiveEffectId).Count,
                Is.EqualTo(1));
        }

        [Test]
        public void PassiveAndMatch5BuffRemainSeparateEffects()
        {
            Registration registration = Register(awakening: 4);
            var party = CreatePartyWithPassive(registration);
            registration.MatchProviders.TryResolve(
                LucielRules.CharacterId,
                out IMatchEventActionProvider resolved);
            var match = (LucielMatchActionProvider)resolved;

            Execute(
                registration.Boss,
                party,
                match.CreateActions(
                    registration.Character,
                    party,
                    BoardMatchTier.FiveOrMore,
                    finalComboCount: 1,
                    playerTurnNumber: 1,
                    registration.ActionIds));

            Assert.That(party.Effects.Count, Is.EqualTo(2));
            EffectInstance passive = party.Effects.FindByEffectId(
                LucielRules.PassiveEffectId).Single();
            EffectInstance matchBuff = party.Effects.FindByEffectId(
                LucielRules.Match5DamageIncreaseEffectId).Single();
            Assert.That(passive.ModifierType,
                Is.EqualTo(EffectModifierType.PartyDamageReduction));
            Assert.That(passive.StackPolicy,
                Is.EqualTo(EffectStackPolicy.Unique));
            Assert.That(matchBuff.ModifierType,
                Is.EqualTo(EffectModifierType.DealtDamageIncrease));
            Assert.That(matchBuff.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
        }

        [Test]
        public void SeparateBattlesReceiveSeparatePassiveInstances()
        {
            Registration firstRegistration = Register(awakening: 0);
            Registration secondRegistration = Register(awakening: 0);
            var firstParty = new PartyBattleState(
                new[] { firstRegistration.Character });
            var secondParty = new PartyBattleState(
                new[] { secondRegistration.Character });

            EffectInstance first = firstParty.Effects.ApplyEffect(
                CreateEffect(
                    firstRegistration.Result.InitialPartyEffects.Single(),
                    100L));
            EffectInstance second = secondParty.Effects.ApplyEffect(
                CreateEffect(
                    secondRegistration.Result.InitialPartyEffects.Single(),
                    100L));

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(firstParty.Effects.Count, Is.EqualTo(1));
            Assert.That(secondParty.Effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateProviderRegistrationIsRejected()
        {
            var registrar = new LucielCombatProviderRegistrar();
            var character = Character();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var ids = new CombatActionIdSequence();
            BattlePartyMemberInput member = Member(definition, 0);

            registrar.Register(
                member,
                character,
                Boss(),
                ids,
                matches,
                actives);

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    member,
                    character,
                    Boss(),
                    ids,
                    matches,
                    actives));
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(actives.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidConfigIsRejectedBeforeProviderRegistration()
        {
            LucielTestConfig.SetInt(config, "activeCooldownTurns", 7);
            var registrar = new LucielCombatProviderRegistrar();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(definition, 0),
                    Character(),
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives));
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);
        }

        [Test]
        public void MissingConfigIsRejectedBeforeProviderRegistration()
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("combatConfig").objectReferenceValue =
                null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var registrar = new LucielCombatProviderRegistrar();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            Assert.Throws<InvalidOperationException>(() =>
                registrar.Register(
                    Member(definition, 0),
                    Character(),
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives));
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);
        }

        [Test]
        public void RegisterRejectsNullDependencies()
        {
            var registrar = new LucielCombatProviderRegistrar();
            BattlePartyMemberInput member = Member(definition, 0);
            CharacterBattleState character = Character();
            BossBattleState boss = Boss();
            var ids = new CombatActionIdSequence();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                null, character, boss, ids, matches, actives));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                member, null, boss, ids, matches, actives));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                member, character, null, ids, matches, actives));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                member, character, boss, null, matches, actives));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                member, character, boss, ids, null, actives));
            Assert.Throws<ArgumentNullException>(() => registrar.Register(
                member, character, boss, ids, matches, null));
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);
        }

        private Registration Register(
            int awakening,
            CombatActionIdSequence actionIds = null)
        {
            var registrar = new LucielCombatProviderRegistrar();
            var character = Character();
            var boss = Boss();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var ids = actionIds ?? new CombatActionIdSequence();
            CharacterCombatProviderRegistrationResult result =
                registrar.Register(
                    Member(definition, awakening),
                    character,
                    boss,
                    ids,
                    matches,
                    actives);
            return new Registration(
                registrar,
                character,
                boss,
                ids,
                result,
                matches,
                actives);
        }

        private static PartyBattleState CreatePartyWithPassive(
            Registration registration)
        {
            var party = new PartyBattleState(
                new[] { registration.Character });
            party.Effects.ApplyEffect(CreateEffect(
                registration.Result.InitialPartyEffects.Single(),
                runtimeId: 100L));
            return party;
        }

        private static EffectInstance CreateEffect(
            InitialPartyEffectDefinition source,
            long runtimeId)
        {
            return new EffectInstance(
                runtimeId,
                source.EffectId,
                source.SourceId,
                source.Category,
                source.ModifierType,
                source.Magnitude,
                source.RemainingTurns,
                runtimeId,
                source.StackPolicy,
                source.StackCount,
                source.MaxStackCount,
                source.ElementFilter,
                source.AttackTypeMask,
                source.RequiredAttackTags);
        }

        private static EffectInstance PartyReduction(
            long runtimeId,
            string effectId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.PartyDamageReduction,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.Unique);
        }

        private static BossDamageActionResult ExecuteBossDamage(
            Registration registration,
            PartyBattleState party)
        {
            var action = new BossDamageAction(
                registration.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    registration.Boss,
                    party,
                    attackCoefficient: 1d,
                    attackTags: AttackTag.None));
            return (BossDamageActionResult)Execute(
                registration.Boss,
                party,
                new CombatAction[] { action }).ActionResults.Single();
        }

        private static CombatActionExecutionResult Execute(
            BossBattleState boss,
            PartyBattleState party,
            IReadOnlyList<CombatAction> actions)
        {
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(
                    new SeededRandomSource(123)));
            return executor.Execute(new CombatActionQueue(actions));
        }

        private static BattlePartyMemberInput Member(
            CharacterDefinition value,
            int awakening)
        {
            return new BattlePartyMemberInput(
                LucielRules.CharacterId,
                partySlotIndex: 0,
                level: 100,
                awakening,
                value);
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                LucielRules.CharacterId,
                partySlotIndex: 0,
                ElementType.Dark,
                maxHp: 1000L,
                attack: 100d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "luciel_registrar_test_boss",
                ElementType.Light,
                maxHp: 100000L,
                attack: 100d);
        }

        private sealed class Registration
        {
            public Registration(
                LucielCombatProviderRegistrar registrar,
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                CharacterCombatProviderRegistrationResult result,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                Registrar = registrar;
                Character = character;
                Boss = boss;
                ActionIds = actionIds;
                Result = result;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
            }

            public LucielCombatProviderRegistrar Registrar { get; }
            public CharacterBattleState Character { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders
            {
                get;
            }
        }
    }
}

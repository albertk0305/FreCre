using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithCombatProviderRegistrarTests
    {
        private LilithCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = LilithTestConfig.Create();
            definition = LilithTestConfig.Definition(config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void RegistrarExposesCanonicalIdAndConfigType()
        {
            var registrar = new LilithCombatProviderRegistrar();

            Assert.That(registrar.CharacterId,
                Is.EqualTo(LilithRules.CharacterId));
            Assert.That(registrar.SupportedCombatConfigType,
                Is.EqualTo(typeof(LilithCombatConfig)));
        }

        [Test]
        public void RegistrationProvidesMatchActiveAndOnePostBossProvider()
        {
            Registration registration = Register(slot: 2, awakening: 6);

            Assert.That(registration.Result.HasActiveAbility, Is.True);
            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(LilithRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(registration.MatchProviders.TryResolve(
                LilithRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(match, Is.TypeOf<LilithMatchActionProvider>());
            Assert.That(registration.ActiveProviders.TryResolve(
                LilithRules.CharacterId,
                LilithRules.ActiveAbilityId,
                out IActiveAbilityActionProvider active), Is.True);
            Assert.That(active,
                Is.TypeOf<LilithActiveAbilityActionProvider>());
            Assert.That(registration.Result.PostBossActionProviders,
                Has.Count.EqualTo(1));
            Assert.That(registration.Result.PostBossActionProviders[0],
                Is.TypeOf<LilithPostBossActionProvider>());
            Assert.That(registration.Result.TriggerRules, Is.Empty);
            Assert.That(registration.Result.ResourceRequirements, Is.Empty);
            Assert.That(registration.Result.CombatStatusSources, Is.Empty);
        }

        [TestCase(0, 0.70d, 0.015d, 3.00d)]
        [TestCase(1, 0.70d, 0.015d, 3.00d)]
        [TestCase(2, 0.70d, 0.015d, 3.00d)]
        [TestCase(3, 0.75d, 0.015d, 3.00d)]
        [TestCase(4, 0.75d, 0.015d, 3.00d)]
        [TestCase(5, 0.75d, 0.015d, 3.30d)]
        [TestCase(6, 0.75d, 0.018d, 3.30d)]
        public void AwakeningFlowsThroughAllProvidersAndSharedIds(
            int awakening,
            double expectedMatch3Coefficient,
            double expectedContractMagnitude,
            double expectedExplosionCoefficient)
        {
            Registration registration = Register(slot: 0, awakening);
            Assert.That(registration.MatchProviders.TryResolve(
                LilithRules.CharacterId,
                out IMatchEventActionProvider matchInterface), Is.True);
            Assert.That(registration.ActiveProviders.TryResolve(
                LilithRules.CharacterId,
                LilithRules.ActiveAbilityId,
                out IActiveAbilityActionProvider activeInterface), Is.True);
            var match = (LilithMatchActionProvider)matchInterface;
            var active = (LilithActiveAbilityActionProvider)activeInterface;
            var postBoss = (LilithPostBossActionProvider)registration.Result
                .PostBossActionProviders.Single();
            var party = new PartyBattleState(
                new[] { registration.Character });

            IReadOnlyList<CombatAction> matchActions = match.CreateActions(
                registration.Character,
                party,
                registration.Boss,
                BoardMatchTier.Three,
                finalComboCount: 1,
                registration.ActionIds);
            IReadOnlyList<CombatAction> activeActions = active.CreateActions(
                registration.Character,
                party,
                registration.Boss,
                registration.ActionIds);
            LilithResolvedCombatRules rules =
                LilithCombatRuleResolver.Resolve(config, awakening);
            AddCovenant(registration.Boss, rules, 4);
            IReadOnlyList<CombatAction> postBossActions =
                postBoss.CreateActions(
                    registration.Character,
                    party,
                    registration.Boss,
                    registration.ActionIds);

            Assert.That(((DamageAction)matchActions[0]).ContextRequest
                    .SkillCoefficient,
                Is.EqualTo(expectedMatch3Coefficient));
            Assert.That(((ApplyEffectAction)matchActions[1]).Effect.Magnitude,
                Is.EqualTo(expectedContractMagnitude));
            Assert.That(((PartyHpCostAction)activeActions[0]).CostRate,
                Is.EqualTo(0.15d));
            Assert.That(activeActions.OfType<ApplyEffectAction>()
                    .All(action =>
                        action.Effect.Magnitude
                            == expectedContractMagnitude),
                Is.True);
            var passive = (DamageAction)postBossActions[0];
            Assert.That(passive.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Passive));
            Assert.That(passive.ContextRequest.SkillCoefficient,
                Is.EqualTo(expectedExplosionCoefficient));

            CombatAction[] allActions = matchActions
                .Concat(activeActions)
                .Concat(postBossActions)
                .ToArray();
            for (int index = 0; index < allActions.Length; index++)
            {
                Assert.That(allActions[index].ActionId,
                    Is.EqualTo(index + 1));
            }
        }

        [Test]
        public void InvalidConfigOrCharacterIsRejectedBeforeRegistryMutation()
        {
            LilithTestConfig.SetField(
                config,
                "contractMaxStackCount",
                0);
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            Assert.That(
                () => new LilithCombatProviderRegistrar().Register(
                    Member(0, 0),
                    Character(slot: 0),
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives),
                Throws.InvalidOperationException.With.Message.Contains(
                    "CombatConfig is invalid"));
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);

            LilithTestConfig.SetField(
                config,
                "contractMaxStackCount",
                12);
            var wrongCharacter = new CharacterBattleState(
                "character_other",
                0,
                ElementType.Dark,
                1000L,
                100d);
            Assert.That(
                () => new LilithCombatProviderRegistrar().Register(
                    Member(0, 0),
                    wrongCharacter,
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives),
                Throws.InvalidOperationException);
            Assert.That(matches.Count, Is.Zero);
            Assert.That(actives.Count, Is.Zero);
        }

        [Test]
        public void MissingConfigIsRejectedBeforeRegistryMutation()
        {
            CharacterDefinition missing = LilithTestConfig.Definition(null);
            try
            {
                var matches = new MatchEventActionProviderRegistry();
                var actives = new ActiveAbilityActionProviderRegistry();
                var member = new BattlePartyMemberInput(
                    LilithRules.CharacterId,
                    0,
                    1,
                    0,
                    missing);

                Assert.That(
                    () => new LilithCombatProviderRegistrar().Register(
                        member,
                        Character(slot: 0),
                        Boss(),
                        new CombatActionIdSequence(),
                        matches,
                        actives),
                    Throws.InvalidOperationException.With.Message.Contains(
                        "requires LilithCombatConfig"));
                Assert.That(matches.Count, Is.Zero);
                Assert.That(actives.Count, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(missing);
            }
        }

        [Test]
        public void DuplicateRegistrationIsRejectedByExistingRegistries()
        {
            var registrar = new LilithCombatProviderRegistrar();
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var actionIds = new CombatActionIdSequence();
            CharacterBattleState character = Character(slot: 0);
            BattlePartyMemberInput member = Member(0, 0);

            registrar.Register(
                member,
                character,
                Boss(),
                actionIds,
                matches,
                actives);

            Assert.That(
                () => registrar.Register(
                    member,
                    character,
                    Boss(),
                    actionIds,
                    matches,
                    actives),
                Throws.InvalidOperationException.With.Message.Contains(
                    "match provider is already registered"));
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(actives.Count, Is.EqualTo(1));
        }

        [Test]
        public void SeparateRegistrationsCreateIndependentProviders()
        {
            Registration first = Register(slot: 0, awakening: 0);
            Registration second = Register(slot: 4, awakening: 0);
            first.MatchProviders.TryResolve(
                LilithRules.CharacterId,
                out IMatchEventActionProvider firstMatch);
            second.MatchProviders.TryResolve(
                LilithRules.CharacterId,
                out IMatchEventActionProvider secondMatch);
            first.ActiveProviders.TryResolve(
                LilithRules.CharacterId,
                LilithRules.ActiveAbilityId,
                out IActiveAbilityActionProvider firstActive);
            second.ActiveProviders.TryResolve(
                LilithRules.CharacterId,
                LilithRules.ActiveAbilityId,
                out IActiveAbilityActionProvider secondActive);

            Assert.That(firstMatch, Is.Not.SameAs(secondMatch));
            Assert.That(firstActive, Is.Not.SameAs(secondActive));
            Assert.That(first.Result.PostBossActionProviders.Single(),
                Is.Not.SameAs(
                    second.Result.PostBossActionProviders.Single()));
        }

        [Test]
        public void DefaultRuntimeRegistrationCreatesFreshComponentsOnly()
        {
            CharacterRuntimeRegistrationCatalog defaultCatalog =
                CharacterRuntimeRegistrationCatalog.CreateDefault();
            CharacterRuntimeRegistration registration = defaultCatalog
                .Registrations.Single(item =>
                    item.CharacterId == LilithRules.CharacterId);

            Assert.That(registration.SupportedCombatConfigType,
                Is.EqualTo(typeof(LilithCombatConfig)));
            Assert.That(registration.CreateBuildModifier(),
                Is.TypeOf<LilithBuildModifier>());
            Assert.That(registration.CreateCombatRegistrar(),
                Is.TypeOf<LilithCombatProviderRegistrar>());
            Assert.That(registration.CreateLookupProvider(),
                Is.TypeOf<LilithLookupDescriptionProvider>());
            Assert.That(registration.CreateBuildModifier(),
                Is.Not.SameAs(registration.CreateBuildModifier()));
            Assert.That(registration.CreateCombatRegistrar(),
                Is.Not.SameAs(registration.CreateCombatRegistrar()));
            Assert.That(registration.CreateLookupProvider(),
                Is.Not.SameAs(registration.CreateLookupProvider()));
            Assert.That(defaultCatalog.Registrations.Count(item =>
                    item.CharacterId == LilithRules.CharacterId),
                Is.EqualTo(1));
        }

        [Test]
        public void RegistrarCatalogRemainsCompatibleWithExistingCharacter()
        {
            var catalog = new CharacterCombatProviderRegistrarCatalog(
                new ICharacterCombatProviderRegistrar[]
                {
                    new BeatriceBloodstoneCombatProviderRegistrar(),
                    new LilithCombatProviderRegistrar()
                });
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();

            CharacterCombatProviderRegistrationResult result =
                catalog.Register(
                    Member(1, 0),
                    Character(slot: 1),
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives);

            Assert.That(result.ActiveAbilityId,
                Is.EqualTo(LilithRules.ActiveAbilityId));
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(actives.Count, Is.EqualTo(1));
        }

        private Registration Register(int slot, int awakening)
        {
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var actionIds = new CombatActionIdSequence();
            CharacterBattleState character = Character(slot);
            BossBattleState boss = Boss();
            CharacterCombatProviderRegistrationResult result =
                new LilithCombatProviderRegistrar().Register(
                    Member(slot, awakening),
                    character,
                    boss,
                    actionIds,
                    matches,
                    actives);
            return new Registration(
                character,
                boss,
                actionIds,
                matches,
                actives,
                result);
        }

        private BattlePartyMemberInput Member(int slot, int awakening)
        {
            return new BattlePartyMemberInput(
                LilithRules.CharacterId,
                slot,
                1,
                awakening,
                definition);
        }

        private static CharacterBattleState Character(int slot)
        {
            return new CharacterBattleState(
                LilithRules.CharacterId,
                slot,
                ElementType.Dark,
                1000L,
                1000d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "registrar_test_boss",
                ElementType.Light,
                100000000L,
                100d);
        }

        private static void AddCovenant(
            BossBattleState boss,
            LilithResolvedCombatRules rules,
            int count)
        {
            for (int index = 0; index < count; index++)
            {
                boss.Effects.ApplyEffect(
                    LilithCovenantEffectFactory.Create(
                        10000L + index,
                        rules));
            }
        }

        private sealed class Registration
        {
            public Registration(
                CharacterBattleState character,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders,
                CharacterCombatProviderRegistrationResult result)
            {
                Character = character;
                Boss = boss;
                ActionIds = actionIds;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Result = result;
            }

            public CharacterBattleState Character { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
        }
    }
}

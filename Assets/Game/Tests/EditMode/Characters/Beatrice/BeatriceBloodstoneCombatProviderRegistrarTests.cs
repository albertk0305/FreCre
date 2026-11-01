using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Beatrice;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneCombatProviderRegistrarTests
    {
        private BeatriceBloodstoneCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = BeatriceBloodstoneTestConfig.Create();
            definition = BeatriceBloodstoneTestConfig.Definition(config);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void RegistrationProvidesMatchActiveLifestealAndStatus()
        {
            Registration registration = Register(slot: 2, awakening: 6);

            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(BeatriceBloodstoneRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(registration.MatchProviders.TryResolve(
                BeatriceBloodstoneRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(match,
                Is.TypeOf<BeatriceBloodstoneMatchActionProvider>());
            Assert.That(registration.ActiveProviders.TryResolve(
                BeatriceBloodstoneRules.CharacterId,
                BeatriceBloodstoneRules.ActiveAbilityId,
                out IActiveAbilityActionProvider active), Is.True);
            Assert.That(active,
                Is.TypeOf<BeatriceBloodstoneActiveActionProvider>());
            Assert.That(registration.Result.ResourceRequirements, Is.Empty);

            var rule = registration.Result.TriggerRules.Single()
                as BeatriceBloodstoneLifestealRule;
            var status = registration.Result.CombatStatusSources.Single()
                as BeatriceBloodstoneBattleState;
            Assert.That(rule, Is.Not.Null);
            Assert.That(status, Is.Not.Null);
            Assert.That(rule.BattleState, Is.SameAs(status));
            Assert.That(status.PartySlotIndex, Is.EqualTo(2));
            Assert.That(status.IsBadgeVisible, Is.False);
        }

        [Test]
        public void SeparateRegistrationsHaveIndependentBattleState()
        {
            Registration first = Register(slot: 0, awakening: 0);
            Registration second = Register(slot: 4, awakening: 0);

            Assert.That(first.Result.CombatStatusSources.Single(),
                Is.Not.SameAs(second.Result.CombatStatusSources.Single()));
            Assert.That(first.Result.TriggerRules.Single(),
                Is.Not.SameAs(second.Result.TriggerRules.Single()));
        }

        [Test]
        public void StatusBindsToRuntimePartyBeforeAnyBeatriceAction()
        {
            Registration registration = Register(slot: 0, awakening: 0);
            var party = new PartyBattleState(
                new CharacterBattleState[] { registration.Character });
            var status = (BeatriceBloodstoneBattleState)registration.Result
                .CombatStatusSources.Single();
            var binder = (IPartyBattleStateCombatStatusSource)status;

            binder.BindPartyBattleState(party);

            Assert.That(status.Party, Is.SameAs(party));
            Assert.That(status.IsVisible, Is.False);
        }

        [Test]
        public void RegistrarRejectsNonCanonicalCharacterWithoutMutation()
        {
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            var wrongCharacter = new CharacterBattleState(
                "character_other", 0, ElementType.Dark, 1000L, 1000d);

            Assert.That(
                () => new BeatriceBloodstoneCombatProviderRegistrar()
                    .Register(
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

        private Registration Register(int slot, int awakening)
        {
            var character = new CharacterBattleState(
                BeatriceBloodstoneRules.CharacterId,
                slot,
                ElementType.Dark,
                1000L,
                1000d);
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult result =
                new BeatriceBloodstoneCombatProviderRegistrar().Register(
                    Member(slot, awakening),
                    character,
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives);
            return new Registration(
                character,
                matches,
                actives,
                result);
        }

        private BattlePartyMemberInput Member(int slot, int awakening)
        {
            return new BattlePartyMemberInput(
                BeatriceBloodstoneRules.CharacterId,
                slot,
                1,
                awakening,
                definition);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "registrar_test_boss",
                ElementType.Light,
                1000000L,
                100d);
        }

        private sealed class Registration
        {
            public Registration(
                CharacterBattleState character,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders,
                CharacterCombatProviderRegistrationResult result)
            {
                Character = character;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
                Result = result;
            }

            public CharacterBattleState Character { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
        }
    }
}

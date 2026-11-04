using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Anubis;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Anubis
{
    public sealed class AnubisCombatProviderRegistrarTests
    {
        private AnubisCombatConfig config;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            config = AnubisTestContent.CreateConfig();
            definition = AnubisTestContent.CreateDefinition(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(config);
        }

        [TestCase(0, 0.08d)]
        [TestCase(4, 0.08d)]
        [TestCase(5, 0.10d)]
        [TestCase(6, 0.10d)]
        public void RegistrationDeclaresIndefiniteLightPassive(
            int awakening,
            double expectedRate)
        {
            Registration registration = Register(awakening);
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();

            Assert.That(passive.EffectId,
                Is.EqualTo(AnubisRules.PassiveEffectId));
            Assert.That(passive.ModifierType,
                Is.EqualTo(EffectModifierType.ElementDamageIncrease));
            Assert.That(passive.Magnitude, Is.EqualTo(expectedRate));
            Assert.That(passive.RemainingTurns, Is.Null);
            Assert.That(passive.StackPolicy,
                Is.EqualTo(EffectStackPolicy.Unique));
            Assert.That(passive.ElementFilter, Is.EqualTo(ElementType.Light));
        }

        [Test]
        public void RegistrationProvidesMatchAndBoardActiveOnly()
        {
            Registration registration = Register(awakening: 0);

            Assert.That(registration.Result.ActiveAbilityId,
                Is.EqualTo(AnubisRules.ActiveAbilityId));
            Assert.That(registration.Result.ActiveCooldownTurns,
                Is.EqualTo(8));
            Assert.That(registration.Result.TriggerRules, Is.Empty);
            Assert.That(registration.Result.CombatStatusSources, Is.Empty);
            Assert.That(registration.MatchProviders.TryResolve(
                AnubisRules.CharacterId,
                out IMatchEventActionProvider match), Is.True);
            Assert.That(match, Is.TypeOf<AnubisMatchActionProvider>());
            Assert.That(registration.ActiveProviders.TryResolveBoard(
                AnubisRules.CharacterId,
                AnubisRules.ActiveAbilityId,
                out IActiveAbilityBoardPlanProvider active), Is.True);
            Assert.That(active, Is.TypeOf<AnubisActiveBoardPlanProvider>());
        }

        [Test]
        public void PassiveAppliesToSelfAndAllyLightButNotFire()
        {
            Registration registration = Register(awakening: 0);
            var ally = new CharacterBattleState(
                "test_light_ally",
                1,
                ElementType.Light,
                1000,
                100d);
            var party = new PartyBattleState(
                new[] { registration.Character, ally });
            BossBattleState boss = Boss();
            InitialPartyEffectDefinition passive = registration.Result
                .InitialPartyEffects.Single();
            party.Effects.ApplyEffect(CreateEffect(passive, 100));

            Assert.That(CombatModifierCollector.CollectAllyDamage(
                    registration.Character,
                    party,
                    boss,
                    ElementType.Light).ElementDamageIncreaseRateSum,
                Is.EqualTo(0.08d));
            Assert.That(CombatModifierCollector.CollectAllyDamage(
                    ally,
                    party,
                    boss,
                    ElementType.Light).ElementDamageIncreaseRateSum,
                Is.EqualTo(0.08d));
            Assert.That(CombatModifierCollector.CollectAllyDamage(
                    registration.Character,
                    party,
                    boss,
                    ElementType.Fire).ElementDamageIncreaseRateSum,
                Is.Zero);
        }

        private Registration Register(int awakening)
        {
            var character = new CharacterBattleState(
                AnubisRules.CharacterId,
                0,
                ElementType.Light,
                1100,
                120d);
            var matches = new MatchEventActionProviderRegistry();
            var actives = new ActiveAbilityActionProviderRegistry();
            CharacterCombatProviderRegistrationResult result =
                new AnubisCombatProviderRegistrar().Register(
                    new BattlePartyMemberInput(
                        AnubisRules.CharacterId,
                        0,
                        100,
                        awakening,
                        definition),
                    character,
                    Boss(),
                    new CombatActionIdSequence(),
                    matches,
                    actives);
            return new Registration(character, result, matches, actives);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "anubis_registrar_test_boss",
                ElementType.Dark,
                100000,
                100d);
        }

        private static EffectInstance CreateEffect(
            InitialPartyEffectDefinition source,
            long id)
        {
            return new EffectInstance(
                id,
                source.EffectId,
                source.SourceId,
                source.Category,
                source.ModifierType,
                source.Magnitude,
                source.RemainingTurns,
                id,
                source.StackPolicy,
                source.StackCount,
                source.MaxStackCount,
                source.ElementFilter,
                source.AttackTypeMask,
                source.RequiredAttackTags);
        }

        private sealed class Registration
        {
            public Registration(
                CharacterBattleState character,
                CharacterCombatProviderRegistrationResult result,
                MatchEventActionProviderRegistry matchProviders,
                ActiveAbilityActionProviderRegistry activeProviders)
            {
                Character = character;
                Result = result;
                MatchProviders = matchProviders;
                ActiveProviders = activeProviders;
            }

            public CharacterBattleState Character { get; }
            public CharacterCombatProviderRegistrationResult Result { get; }
            public MatchEventActionProviderRegistry MatchProviders { get; }
            public ActiveAbilityActionProviderRegistry ActiveProviders { get; }
        }
    }
}

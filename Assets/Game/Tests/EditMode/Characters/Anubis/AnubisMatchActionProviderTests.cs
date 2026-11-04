using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Anubis;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Anubis
{
    public sealed class AnubisMatchActionProviderTests
    {
        private AnubisCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = AnubisTestContent.CreateConfig();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [TestCase(BoardMatchTier.Three, 0.40d, 0.04d)]
        [TestCase(BoardMatchTier.Four, 0.70d, 0.08d)]
        [TestCase(BoardMatchTier.FiveOrMore, 1.10d, 0.12d)]
        public void MatchCreatesDamageThenOneTurnRadiance(
            BoardMatchTier tier,
            double coefficient,
            double radiance)
        {
            Battle battle = CreateBattle(awakening: 0);
            var actions = battle.Provider.CreateActions(
                battle.Anubis,
                battle.Party,
                battle.Boss,
                tier,
                1,
                new CombatActionIdSequence());

            Assert.That(actions.Count, Is.EqualTo(2));
            var damage = actions[0] as DamageAction;
            var effect = actions[1] as ApplyEffectAction;
            Assert.That(damage, Is.Not.Null);
            Assert.That(effect, Is.Not.Null);
            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(coefficient));
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Light));
            Assert.That(effect.TargetParty, Is.SameAs(battle.Party));
            Assert.That(effect.Effect.EffectId,
                Is.EqualTo(AnubisRules.RadianceEffectId));
            Assert.That(effect.Effect.Magnitude, Is.EqualTo(radiance));
            Assert.That(effect.Effect.RemainingTurns, Is.EqualTo(1));
            Assert.That(effect.Effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.ReplaceWithStronger));
            Assert.That(effect.Effect.ElementFilter,
                Is.EqualTo(ElementType.Light));
        }

        [Test]
        public void RadianceKeepsHighestValueAndExpiresAtTurnEnd()
        {
            Battle battle = CreateBattle(awakening: 0);
            battle.Execute(BoardMatchTier.Three);
            battle.Execute(BoardMatchTier.Four);
            Assert.That(Radiance(battle).Magnitude, Is.EqualTo(0.08d));

            battle.Execute(BoardMatchTier.Four);
            Assert.That(Radiance(battle).Magnitude, Is.EqualTo(0.08d));

            battle.Execute(BoardMatchTier.FiveOrMore);
            battle.Execute(BoardMatchTier.Three);
            Assert.That(Radiance(battle).Magnitude, Is.EqualTo(0.12d));
            Assert.That(battle.Party.Effects.FindByEffectId(
                AnubisRules.RadianceEffectId), Has.Count.EqualTo(1));

            battle.Party.Effects.ProcessTurnEnd();
            Assert.That(battle.Party.Effects.FindByEffectId(
                AnubisRules.RadianceEffectId), Is.Empty);
        }

        [Test]
        public void NewRadianceDoesNotAffectItsOwnDamageButAffectsLaterLightAttack()
        {
            Battle battle = CreateBattle(awakening: 0);
            CombatActionExecutionResult first = battle.Execute(
                BoardMatchTier.FiveOrMore);
            DamageActionResult firstDamage = first.ActionResults
                .OfType<DamageActionResult>().Single();

            CombatActionExecutionResult second = battle.Execute(
                BoardMatchTier.Three);
            DamageActionResult secondDamage = second.ActionResults
                .OfType<DamageActionResult>().Single();

            Assert.That(firstDamage.Context.ElementDamageIncreaseRateSum,
                Is.Zero);
            Assert.That(secondDamage.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.12d));
        }

        private static EffectInstance Radiance(Battle battle)
        {
            return battle.Party.Effects.FindByEffectId(
                AnubisRules.RadianceEffectId).Single();
        }

        private Battle CreateBattle(int awakening)
        {
            var anubis = new CharacterBattleState(
                AnubisRules.CharacterId,
                0,
                ElementType.Light,
                1100,
                120d);
            var party = new PartyBattleState(new[] { anubis });
            var boss = new BossBattleState(
                "anubis_match_test_boss",
                ElementType.Light,
                1000000,
                100d);
            return new Battle(
                anubis,
                party,
                boss,
                new AnubisMatchActionProvider(config, awakening));
        }

        private sealed class Battle
        {
            public Battle(
                CharacterBattleState anubis,
                PartyBattleState party,
                BossBattleState boss,
                AnubisMatchActionProvider provider)
            {
                Anubis = anubis;
                Party = party;
                Boss = boss;
                Provider = provider;
                Executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(1)));
                ActionIds = new CombatActionIdSequence();
            }

            public CharacterBattleState Anubis { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public AnubisMatchActionProvider Provider { get; }
            public CombatActionExecutor Executor { get; }
            public CombatActionIdSequence ActionIds { get; }

            public CombatActionExecutionResult Execute(BoardMatchTier tier)
            {
                return Executor.Execute(new CombatActionQueue(
                    Provider.CreateActions(
                        Anubis,
                        Party,
                        Boss,
                        tier,
                        1,
                        ActionIds)));
            }
        }
    }
}

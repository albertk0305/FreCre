using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiMatchActionProviderTests
    {
        private MeimeiCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = MeimeiTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(BoardMatchTier.Three, 0, 1.25d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 0, 2.20d, AttackTag.Match4)]
        [TestCase(
            BoardMatchTier.FiveOrMore,
            0,
            3.60d,
            AttackTag.Match5Plus)]
        [TestCase(BoardMatchTier.Three, 3, 1.40d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 3, 2.50d, AttackTag.Match4)]
        [TestCase(
            BoardMatchTier.FiveOrMore,
            4,
            4.10d,
            AttackTag.Match5Plus)]
        public void CreatesCanonicalFireMatchDamage(
            BoardMatchTier tier,
            int awakening,
            double expectedCoefficient,
            AttackTag expectedTag)
        {
            CharacterBattleState meimei = Character();
            var party = new PartyBattleState(new[] { meimei });
            BossBattleState boss = Boss();
            var actionIds = new CombatActionIdSequence();
            var provider = new MeimeiMatchActionProvider(
                config,
                awakening);

            IReadOnlyList<CombatAction> actions = provider.CreateActions(
                meimei,
                party,
                boss,
                tier,
                finalComboCount: 1,
                actionIds);

            Assert.That(actions.Count, Is.EqualTo(1));
            DamageAction damage = actions[0] as DamageAction;
            Assert.That(damage, Is.Not.Null);
            Assert.That(damage.ActionId, Is.EqualTo(1L));
            Assert.That(damage.RootActionId, Is.EqualTo(1L));
            Assert.That(damage.SourceActionId, Is.Null);
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(damage.ContextRequest.Attacker, Is.SameAs(meimei));
            Assert.That(damage.ContextRequest.Party, Is.SameAs(party));
            Assert.That(damage.ContextRequest.TargetBoss, Is.SameAs(boss));
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Fire));
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(expectedTag));
            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(expectedCoefficient));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
            Assert.That(damage.ContextRequest.FinalComboCount,
                Is.EqualTo(1));
            Assert.That(damage.ContextRequest.CanCritical, Is.True);
        }

        [Test]
        public void ExecutesThroughSharedDamagePipelineWithCombo()
        {
            CharacterBattleState meimei = Character();
            var party = new PartyBattleState(new[] { meimei });
            BossBattleState boss = Boss();
            IReadOnlyList<CombatAction> actions =
                new MeimeiMatchActionProvider(config).CreateActions(
                    meimei,
                    party,
                    boss,
                    BoardMatchTier.Three,
                    finalComboCount: 3,
                    new CombatActionIdSequence());
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(
                    Array.Empty<ICombatTriggerRule>()));

            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(actions));
            DamageActionResult damage =
                (DamageActionResult)result.ActionResults[0];

            Assert.That(damage.Context.SkillCoefficient,
                Is.EqualTo(1.25d));
            Assert.That(damage.DamageResult.ComboMultiplier,
                Is.EqualTo(1.12d));
            Assert.That(damage.Context.IsCritical, Is.False);
            Assert.That(damage.DamageResult.FinalDamage,
                Is.EqualTo(1400L));
            Assert.That(damage.AppliedDamage, Is.EqualTo(1400L));
        }

        [Test]
        public void CreatingMatchActionDoesNotMutateBattleState()
        {
            CharacterBattleState meimei = Character();
            var party = new PartyBattleState(new[] { meimei });
            var state = new MeimeiBattleState(meimei, 5);

            new MeimeiMatchActionProvider(config).CreateActions(
                meimei,
                party,
                Boss(),
                BoardMatchTier.Four,
                1,
                new CombatActionIdSequence());

            Assert.That(state.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(state.Tipsiness, Is.Zero);
        }

        [Test]
        public void RejectsNonCanonicalAttackerAndInvalidInputs()
        {
            var provider = new MeimeiMatchActionProvider(config);
            CharacterBattleState meimei = Character();
            var party = new PartyBattleState(new[] { meimei });
            BossBattleState boss = Boss();
            var ids = new CombatActionIdSequence();
            var wrong = new CharacterBattleState(
                "character_other",
                0,
                ElementType.Fire,
                900,
                180d);

            Assert.Throws<ArgumentException>(() => provider.CreateActions(
                wrong,
                party,
                boss,
                BoardMatchTier.Three,
                1,
                ids));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                provider.CreateActions(
                    meimei,
                    party,
                    boss,
                    BoardMatchTier.Three,
                    0,
                    ids));
            Assert.Throws<ArgumentNullException>(() =>
                new MeimeiMatchActionProvider(null));
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
                "meimei_match_test_boss",
                ElementType.Light,
                1000000,
                100d);
        }
    }
}

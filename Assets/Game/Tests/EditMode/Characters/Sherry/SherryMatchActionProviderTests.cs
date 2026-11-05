using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0.75d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 1.30d, AttackTag.Match4)]
        [TestCase(BoardMatchTier.FiveOrMore, 2.10d,
            AttackTag.Match5Plus)]
        public void BaseAwakeningCreatesOneRootMatchDamage(
            BoardMatchTier tier,
            double expectedCoefficient,
            AttackTag expectedTag)
        {
            using (var battle = new SherryCombatTestBattle())
            {
                var actions = battle.Match(tier);

                Assert.That(actions.Count, Is.EqualTo(1));
                DamageAction damage = actions.Single() as DamageAction;
                Assert.That(damage, Is.Not.Null);
                Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
                Assert.That(damage.RootActionId,
                    Is.EqualTo(damage.ActionId));
                Assert.That(damage.SourceActionId, Is.Null);
                Assert.That(damage.ContextRequest.Attacker,
                    Is.SameAs(battle.Character));
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Fire));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(damage.ContextRequest.AttackTags,
                    Is.EqualTo(expectedTag));
                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
                Assert.That(damage.ContextRequest.CanCritical, Is.True);
                Assert.That(actions.Any(action =>
                        action.Origin == ActionOrigin.Additional),
                    Is.False);
            }
        }

        [TestCase(BoardMatchTier.Three, 0.80d)]
        [TestCase(BoardMatchTier.Four, 1.40d)]
        [TestCase(BoardMatchTier.FiveOrMore, 2.25d)]
        public void AwakeningThreeUsesEnhancedCoefficients(
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            using (var battle = new SherryCombatTestBattle(awakening: 3))
            {
                DamageAction damage =
                    (DamageAction)battle.Match(tier).Single();

                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
            }
        }

        [Test]
        public void FiveOrMoreUsesOneSharedTierAndMatchTag()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                DamageAction damage = (DamageAction)battle.Match(
                    BoardMatchTier.FiveOrMore).Single();

                Assert.That(damage.ContextRequest.AttackTags,
                    Is.EqualTo(AttackTag.Match5Plus));
                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(2.10d));
            }
        }

        [Test]
        public void MatchUsesComboMultiplierAndAllowsCritical()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                var execution = battle.Execute(
                    battle.Match(BoardMatchTier.Three, 3));
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.AppliesCombo, Is.True);
                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.Context.CanCritical, Is.True);
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void MatchSnapshotsStylishIncreaseForEveryRank(int rank)
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(rank, SherryMatchTier.Three);
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(battle.Config, 0);

                DamageAction damage = (DamageAction)battle.Match(
                    BoardMatchTier.Three).Single();

                Assert.That(
                    damage.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(
                        rules.ResolveStylishDamageIncreaseRate(rank))
                        .Within(0.000000000001d));
            }
        }

        [Test]
        public void StylishAndExternalDealtDamageIncreaseAreAddedOnce()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                battle.RaiseRankTo(5, SherryMatchTier.Three);
                battle.Character.Effects.ApplyEffect(new EffectInstance(
                    9000L,
                    "effect_test_dealt_damage",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.DealtDamageIncrease,
                    0.10d,
                    remainingTurns: null,
                    creationOrder: 9000L));
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(battle.Config, 0);
                double stylish =
                    rules.ResolveStylishDamageIncreaseRate(5);

                var execution = battle.Execute(
                    battle.Match(BoardMatchTier.Three));
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();

                Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.10d + stylish)
                        .Within(0.000000000001d));
            }
        }

        [Test]
        public void ConsecutiveMatchesSnapshotLatestCompletedRank()
        {
            using (var battle = new SherryCombatTestBattle())
            {
                SherryResolvedCombatRules rules =
                    SherryCombatRuleResolver.Resolve(battle.Config, 0);
                DamageAction first = (DamageAction)battle.Match(
                    BoardMatchTier.Three).Single();
                Assert.That(first.ContextRequest
                    .ActionLocalDealtDamageIncreaseRate, Is.Zero);
                Assert.That(battle.State.StylishRank, Is.Zero);

                battle.Execute(new CombatAction[] { first });
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));

                DamageAction repeated = (DamageAction)battle.Match(
                    BoardMatchTier.Three).Single();
                Assert.That(repeated.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(
                        rules.ResolveStylishDamageIncreaseRate(1)));
                battle.Execute(new CombatAction[] { repeated });
                Assert.That(battle.State.StylishRank, Is.EqualTo(1));

                DamageAction changed = (DamageAction)battle.Match(
                    BoardMatchTier.Four).Single();
                Assert.That(changed.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(
                        rules.ResolveStylishDamageIncreaseRate(1)));
                battle.Execute(new CombatAction[] { changed });
                Assert.That(battle.State.StylishRank, Is.EqualTo(2));
            }
        }
    }
}

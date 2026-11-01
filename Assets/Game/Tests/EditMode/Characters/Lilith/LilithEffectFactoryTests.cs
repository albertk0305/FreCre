using System;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Lilith;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Lilith
{
    public sealed class LilithEffectFactoryTests
    {
        [TestCase(0, 0.015d)]
        [TestCase(6, 0.018d)]
        public void CovenantUsesResolvedUnfilteredStackDefinition(
            int awakening,
            double expectedMagnitude)
        {
            using (var scope = new ConfigScope(awakening))
            {
                EffectInstance effect =
                    LilithCovenantEffectFactory.Create(1L, scope.Rules);

                Assert.That(effect.EffectId,
                    Is.EqualTo(LilithRules.CovenantEffectId));
                Assert.That(effect.SourceId,
                    Is.EqualTo(LilithRules.CharacterId));
                Assert.That(effect.Category,
                    Is.EqualTo(EffectCategory.Debuff));
                Assert.That(effect.ModifierType,
                    Is.EqualTo(
                        EffectModifierType.TargetTakenDamageIncrease));
                Assert.That(effect.Magnitude,
                    Is.EqualTo(expectedMagnitude));
                Assert.That(effect.RemainingTurns, Is.Null);
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(effect.StackCount, Is.EqualTo(1));
                Assert.That(effect.MaxStackCount, Is.EqualTo(12));
                Assert.That(effect.AttackTypeMask,
                    Is.EqualTo(AttackTypeMask.None));
                Assert.That(effect.RequiredAttackTags,
                    Is.EqualTo(AttackTag.None));
            }
        }

        [Test]
        public void CovenantStartsAbsentStacksToTwelveAndNeverExpires()
        {
            using (var scope = new ConfigScope())
            {
                var boss = CreateBoss();
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(boss),
                    Is.Zero);

                for (long id = 1L; id <= 12L; id++)
                {
                    boss.Effects.ApplyEffect(
                        LilithCovenantEffectFactory.Create(
                            id,
                            scope.Rules));
                    Assert.That(
                        LilithCovenantEffectFactory.GetStackCount(boss),
                        Is.EqualTo((int)id));
                }

                for (long id = 13L; id <= 15L; id++)
                {
                    boss.Effects.ApplyEffect(
                        LilithCovenantEffectFactory.Create(
                            id,
                            scope.Rules));
                }

                for (int turn = 0; turn < 20; turn++)
                {
                    boss.Effects.ProcessTurnEnd();
                }

                Assert.That(boss.Effects.FindByEffectId(
                    LilithRules.CovenantEffectId), Has.Count.EqualTo(1));
                Assert.That(
                    LilithCovenantEffectFactory.GetStackCount(boss),
                    Is.EqualTo(12));
                Assert.That(
                    LilithCovenantEffectFactory.TryGetEffect(
                        boss,
                        out EffectInstance effect),
                    Is.True);
                Assert.That(effect.RemainingTurns, Is.Null);
                Assert.That(effect.EffectiveMagnitude,
                    Is.EqualTo(0.18d).Within(0.000000001d));
            }
        }

        [TestCase(AttackType.Match)]
        [TestCase(AttackType.Active)]
        [TestCase(AttackType.Additional)]
        [TestCase(AttackType.Chase)]
        [TestCase(AttackType.DamageOverTime)]
        [TestCase(AttackType.Passive)]
        public void CovenantAppliesToEveryAttackTypeAndOtherCharacters(
            AttackType attackType)
        {
            using (var scope = new ConfigScope())
            {
                var lilith = new CharacterBattleState(
                    LilithRules.CharacterId,
                    0,
                    ElementType.Dark,
                    1000L,
                    1000d);
                var other = new CharacterBattleState(
                    "character_other",
                    1,
                    ElementType.Fire,
                    1000L,
                    1000d);
                var party = new PartyBattleState(
                    new[] { lilith, other });
                BossBattleState boss = CreateBoss();
                for (long id = 1L; id <= 4L; id++)
                {
                    boss.Effects.ApplyEffect(
                        LilithCovenantEffectFactory.Create(
                            id,
                            scope.Rules));
                }

                var damage = new DamageAction(
                    100L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        other,
                        party,
                        boss,
                        ElementType.Fire,
                        attackType,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                DamageActionResult result = Execute(
                        boss,
                        party,
                        new CombatAction[] { damage })
                    .ActionResults.OfType<DamageActionResult>()
                    .Single();

                Assert.That(
                    result.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.06d).Within(0.000000001d));
            }
        }

        [TestCase(0, 0.20d)]
        [TestCase(4, 0.25d)]
        public void FallenBlessingUsesResolvedRefreshDefinition(
            int awakening,
            double expectedMagnitude)
        {
            using (var scope = new ConfigScope(awakening))
            {
                EffectInstance effect =
                    LilithFallenBlessingEffectFactory.Create(
                        1L,
                        scope.Rules);

                Assert.That(effect.EffectId,
                    Is.EqualTo(LilithRules.FallenBlessingEffectId));
                Assert.That(effect.SourceId,
                    Is.EqualTo(LilithRules.CharacterId));
                Assert.That(effect.Category,
                    Is.EqualTo(EffectCategory.Buff));
                Assert.That(effect.ModifierType,
                    Is.EqualTo(EffectModifierType.PartyDamageReduction));
                Assert.That(effect.Magnitude,
                    Is.EqualTo(expectedMagnitude));
                Assert.That(effect.RemainingTurns, Is.EqualTo(2));
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(effect.StackCount, Is.EqualTo(1));
                Assert.That(effect.MaxStackCount, Is.EqualTo(1));
            }
        }

        private static BossBattleState CreateBoss()
        {
            return new BossBattleState(
                "training_boss",
                ElementType.Light,
                1000000L,
                1000d);
        }

        private static CombatActionExecutionResult Execute(
            BossBattleState boss,
            PartyBattleState party,
            CombatAction[] actions)
        {
            return new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)))
                .Execute(new CombatActionQueue(actions));
        }

        private sealed class ConfigScope : IDisposable
        {
            private readonly LilithCombatConfig config;

            public ConfigScope(int awakening = 0)
            {
                config = LilithTestConfig.Create();
                Rules = LilithCombatRuleResolver.Resolve(
                    config,
                    awakening);
            }

            public LilithResolvedCombatRules Rules { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}

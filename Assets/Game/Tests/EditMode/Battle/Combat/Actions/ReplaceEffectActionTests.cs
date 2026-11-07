using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class ReplaceEffectActionTests
    {
        [Test]
        public void ExecuteAtomicallyReplacesEffectAndReturnsBothInstances()
        {
            Battle(out BossBattleState boss, out PartyBattleState party);
            EffectInstance light = Effect(10, "effect_light");
            EffectInstance stone = Effect(11, "effect_stone");
            boss.Effects.ApplyEffect(light);
            var action = new ReplaceEffectAction(
                1, ActionOrigin.System, boss, light.RuntimeId, stone);

            CombatActionExecutionResult execution = Executor(boss, party)
                .Execute(new CombatActionQueue(new CombatAction[] { action }));

            var result = (ReplaceEffectActionResult)execution.ActionResults[0];
            Assert.That(result.RemovedEffect, Is.SameAs(light));
            Assert.That(result.AppliedEffect, Is.SameAs(stone));
            Assert.That(boss.Effects.GetActiveEffects(), Is.EqualTo(new[] { stone }));
        }

        [Test]
        public void DuplicateReplacementIdentityLeavesCollectionUnchanged()
        {
            Battle(out BossBattleState boss, out PartyBattleState party);
            EffectInstance light = Effect(10, "effect_light");
            EffectInstance retained = Effect(11, "effect_retained");
            boss.Effects.ApplyEffect(light);
            boss.Effects.ApplyEffect(retained);
            EffectInstance invalid = Effect(11, "effect_stone");
            var action = new ReplaceEffectAction(
                1, ActionOrigin.System, boss, light.RuntimeId, invalid);

            Assert.Throws<ArgumentException>(() => Executor(boss, party)
                .Execute(new CombatActionQueue(new CombatAction[] { action })));
            Assert.That(boss.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { light, retained }));
        }

        private static EffectInstance Effect(long id, string effectId) =>
            new EffectInstance(id, effectId, "test", EffectCategory.Buff,
                EffectModifierType.TargetTakenDamageReduction, 0.2d,
                remainingTurns: null, creationOrder: id,
                EffectStackPolicy.Unique);

        private static CombatActionExecutor Executor(
            BossBattleState boss, PartyBattleState party) =>
            new CombatActionExecutor(boss, party,
                new DamageContextFactory(new SeededRandomSource(1)));

        private static void Battle(
            out BossBattleState boss, out PartyBattleState party)
        {
            boss = new BossBattleState("test_boss", ElementType.Light, 1000, 100d);
            party = new PartyBattleState(new[]
            {
                new CharacterBattleState("hero", 0, ElementType.Light, 1000, 100d)
            });
        }
    }
}

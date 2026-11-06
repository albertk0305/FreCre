using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Thalassion;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Thalassion
{
    public sealed class ThalassionElementalShieldTests
    {
        [Test]
        public void TenthAttackIsReducedAndEleventhAttackIsNot()
        {
            CreateBattle(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss,
                out ThalassionBattleRuntimeState runtime,
                out CombatActionIdSequence actionIds,
                out CombatActionExecutor executor);
            boss.Effects.ApplyEffect(
                ThalassionElementalShieldEffectFactory.Create(
                    actionIds.Next(),
                    runtime.Config));

            var attacks = new List<CombatAction>();
            for (int index = 0; index < 11; index++)
            {
                attacks.Add(Damage(
                    actionIds.Next(),
                    attacker,
                    party,
                    boss,
                    ActionOrigin.Active,
                    AttackType.Active,
                    AttackTag.None,
                    coefficient: 1d));
            }

            CombatActionExecutionResult result = executor.Execute(
                new CombatActionQueue(attacks));
            DamageActionResult[] damageResults = result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();

            Assert.That(damageResults, Has.Length.EqualTo(11));
            Assert.That(
                damageResults.Take(10).Select(item => item.AppliedDamage),
                Is.All.EqualTo(20L));
            Assert.That(damageResults[10].AppliedDamage, Is.EqualTo(100L));
            Assert.That(runtime.CurrentShieldStackCount, Is.Zero);
            Assert.That(runtime.IsShieldActive, Is.False);
        }

        [Test]
        public void DotFixedAndZeroDamageDoNotConsumeShield()
        {
            CreateBattle(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss,
                out ThalassionBattleRuntimeState runtime,
                out CombatActionIdSequence actionIds,
                out CombatActionExecutor executor);
            boss.Effects.ApplyEffect(
                ThalassionElementalShieldEffectFactory.Create(
                    actionIds.Next(),
                    runtime.Config));
            var attacks = new CombatAction[]
            {
                Damage(
                    actionIds.Next(), attacker, party, boss,
                    ActionOrigin.DamageOverTime,
                    AttackType.DamageOverTime,
                    AttackTag.None,
                    1d),
                Damage(
                    actionIds.Next(), attacker, party, boss,
                    ActionOrigin.Active,
                    AttackType.Active,
                    AttackTag.FixedDamage,
                    1d),
                Damage(
                    actionIds.Next(), attacker, party, boss,
                    ActionOrigin.Active,
                    AttackType.Active,
                    AttackTag.None,
                    0d)
            };

            executor.Execute(new CombatActionQueue(attacks));

            Assert.That(runtime.CurrentShieldStackCount, Is.EqualTo(10));
        }

        [Test]
        public void DirectAndIndependentAdditionalAttackConsumeSeparately()
        {
            CreateBattle(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss,
                out ThalassionBattleRuntimeState runtime,
                out CombatActionIdSequence actionIds,
                out CombatActionExecutor executor);
            boss.Effects.ApplyEffect(
                ThalassionElementalShieldEffectFactory.Create(
                    actionIds.Next(),
                    runtime.Config));
            long rootId = actionIds.Next();
            long additionalId = actionIds.Next();
            var attacks = new CombatAction[]
            {
                Damage(
                    rootId, attacker, party, boss,
                    ActionOrigin.Active,
                    AttackType.Active,
                    AttackTag.None,
                    1d),
                new DamageAction(
                    additionalId,
                    ActionOrigin.Additional,
                    Request(
                        attacker,
                        party,
                        boss,
                        AttackType.Additional,
                        AttackTag.None,
                        1d),
                    rootId,
                    rootId)
            };

            executor.Execute(new CombatActionQueue(attacks));

            Assert.That(runtime.CurrentShieldStackCount, Is.EqualTo(8));
        }

        private static void CreateBattle(
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss,
            out ThalassionBattleRuntimeState runtime,
            out CombatActionIdSequence actionIds,
            out CombatActionExecutor executor)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Water,
                1000,
                100d);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                ThalassionRules.BossId,
                ElementType.Water,
                100000,
                500d);
            runtime = new ThalassionBattleRuntimeState(
                boss,
                ThalassionTestConfig.Create());
            actionIds = new CombatActionIdSequence();
            var rule = new ThalassionElementalShieldHitRule(
                runtime,
                actionIds);
            executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(
                    new ICombatTriggerRule[] { rule }));
        }

        private static DamageAction Damage(
            long actionId,
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            ActionOrigin origin,
            AttackType attackType,
            AttackTag attackTags,
            double coefficient)
        {
            return new DamageAction(
                actionId,
                origin,
                Request(
                    attacker,
                    party,
                    boss,
                    attackType,
                    attackTags,
                    coefficient));
        }

        private static DamageContextBuildRequest Request(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackType attackType,
            AttackTag attackTags,
            double coefficient)
        {
            return new DamageContextBuildRequest(
                attacker,
                party,
                boss,
                ElementType.Water,
                attackType,
                attackTags,
                coefficient,
                false,
                0,
                false);
        }
    }
}

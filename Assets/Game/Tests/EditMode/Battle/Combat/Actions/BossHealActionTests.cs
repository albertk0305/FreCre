using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class BossHealActionTests
    {
        [Test]
        public void Execute_HealsBossFromMaximumHpCoefficient()
        {
            CreateBattle(
                bossMaxHp: 66000,
                partyMaxHp: 1000,
                characterAttack: 16000d,
                bossAttack: 0d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            DamageBoss(executor, character, party, boss, actionId: 1);

            var result = (BossHealActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    new BossHealAction(2, boss, 0.02d)
                })).ActionResults[0];

            Assert.That(result.Action.ActionId, Is.EqualTo(2));
            Assert.That(result.MaxHpHealingCoefficient, Is.EqualTo(0.02d));
            Assert.That(result.RequestedHealing, Is.EqualTo(1320L));
            Assert.That(result.AppliedHealing, Is.EqualTo(1320L));
            Assert.That(result.ApplicationResult.HpBefore,
                Is.EqualTo(50000L));
            Assert.That(result.HpAfter, Is.EqualTo(51320L));
            Assert.That(boss.CurrentHp, Is.EqualTo(51320L));
            Assert.That(party.CurrentHp, Is.EqualTo(1000L));
            Assert.That(party.Shields.TotalShield, Is.Zero);
        }

        [Test]
        public void Execute_CapsHealingAtMissingHpAndRecordsActualAmount()
        {
            CreateBattle(
                bossMaxHp: 66000,
                partyMaxHp: 1000,
                characterAttack: 500d,
                bossAttack: 0d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            DamageBoss(executor, character, party, boss, actionId: 1);

            var result = (BossHealActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    new BossHealAction(2, boss, 0.02d)
                })).ActionResults[0];

            Assert.That(result.RequestedHealing, Is.EqualTo(1320L));
            Assert.That(result.AppliedHealing, Is.EqualTo(500L));
            Assert.That(result.ApplicationResult.OverhealAmount,
                Is.EqualTo(820L));
            Assert.That(result.HpAfter, Is.EqualTo(66000L));
        }

        [Test]
        public void Execute_FullHpAppliesZero()
        {
            CreateBattle(
                bossMaxHp: 1000,
                partyMaxHp: 1000,
                characterAttack: 0d,
                bossAttack: 0d,
                out _,
                out _,
                out BossBattleState boss,
                out CombatActionExecutor executor);

            var result = (BossHealActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    new BossHealAction(1, boss, 0.20d)
                })).ActionResults[0];

            Assert.That(result.RequestedHealing, Is.EqualTo(200L));
            Assert.That(result.AppliedHealing, Is.Zero);
            Assert.That(result.HpAfter, Is.EqualTo(1000L));
        }

        [Test]
        public void Execute_FractionalHealingIsFlooredAtFinalCalculation()
        {
            CreateBattle(
                bossMaxHp: 25999,
                partyMaxHp: 1000,
                characterAttack: 1000d,
                bossAttack: 0d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            DamageBoss(executor, character, party, boss, actionId: 1);

            var result = (BossHealActionResult)executor.Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    new BossHealAction(2, boss, 0.02d)
                })).ActionResults[0];

            Assert.That(result.RequestedHealing, Is.EqualTo(519L));
            Assert.That(result.AppliedHealing, Is.EqualTo(519L));
            Assert.That(result.HpAfter, Is.EqualTo(25518L));
        }

        [Test]
        public void Execute_PartyIncapacitationClearsFollowingBossHeal()
        {
            CreateBattle(
                bossMaxHp: 1000,
                partyMaxHp: 100,
                characterAttack: 500d,
                bossAttack: 100d,
                out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss,
                out CombatActionExecutor executor);
            DamageBoss(executor, character, party, boss, actionId: 1);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new BossDamageAction(
                    2,
                    new BossDamageContextBuildRequest(
                        boss,
                        party,
                        1d,
                        AttackTag.None)),
                new BossHealAction(3, boss, 0.50d)
            });

            CombatActionExecutionResult result = executor.Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.PartyIncapacitated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(result.ActionResults[0],
                Is.TypeOf<BossDamageActionResult>());
            Assert.That(boss.CurrentHp, Is.EqualTo(500L));
        }

        private static void DamageBoss(
            CombatActionExecutor executor,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            long actionId)
        {
            executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                new DamageAction(
                    actionId,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Fire,
                        AttackType.Active,
                        AttackTag.None,
                        1d,
                        false,
                        0,
                        false))
            }));
        }

        private static void CreateBattle(
            long bossMaxHp,
            long partyMaxHp,
            double characterAttack,
            double bossAttack,
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss,
            out CombatActionExecutor executor)
        {
            character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                partyMaxHp,
                characterAttack);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                bossMaxHp,
                bossAttack);
            executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }
    }
}

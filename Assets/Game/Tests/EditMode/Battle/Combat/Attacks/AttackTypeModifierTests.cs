using NUnit.Framework;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Attacks
{
    public sealed class AttackTypeModifierTests
    {
        [TestCase(AttackType.Match, 0, AttackTypeMask.Match, 1)]
        [TestCase(AttackType.Active, 1, AttackTypeMask.Active, 2)]
        [TestCase(AttackType.Additional, 2, AttackTypeMask.Additional, 4)]
        [TestCase(AttackType.Chase, 3, AttackTypeMask.Chase, 8)]
        [TestCase(AttackType.DamageOverTime, 4,
            AttackTypeMask.DamageOverTime, 16)]
        [TestCase(AttackType.Passive, 5, AttackTypeMask.Passive, 32)]
        public void AttackTypeValuesAndMaskBitsRemainStable(
            AttackType attackType,
            int expectedTypeValue,
            AttackTypeMask mask,
            int expectedMaskValue)
        {
            Assert.That((int)attackType, Is.EqualTo(expectedTypeValue));
            Assert.That((int)mask, Is.EqualTo(expectedMaskValue));
            Assert.That(AttackTypeMask.All & mask, Is.EqualTo(mask));
        }

        [TestCase(AttackType.Match, 0d)]
        [TestCase(AttackType.Active, 0d)]
        [TestCase(AttackType.Additional, 0.12d)]
        [TestCase(AttackType.Chase, 0.12d)]
        [TestCase(AttackType.DamageOverTime, 0d)]
        [TestCase(AttackType.Passive, 0d)]
        public void AdditionalAndChaseMaskMatchesOnlySelectedTypes(
            AttackType attackType,
            double expected)
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "follow_up_damage",
                0.12d,
                AttackTypeMask.Additional | AttackTypeMask.Chase));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    attackType,
                    AttackTag.None);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expected).Within(0.000000001d));
        }

        [TestCase(AttackType.Match, 0d)]
        [TestCase(AttackType.Active, 0d)]
        [TestCase(AttackType.Additional, 0d)]
        [TestCase(AttackType.Chase, 0d)]
        [TestCase(AttackType.DamageOverTime, 0d)]
        [TestCase(AttackType.Passive, 0.18d)]
        public void PassiveMaskMatchesOnlyPassive(
            AttackType attackType,
            double expected)
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "passive_damage",
                0.18d,
                AttackTypeMask.Passive));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    attackType,
                    AttackTag.None);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expected).Within(0.000000001d));
        }

        [Test]
        public void LegacyAttackTypeMasksDoNotApplyToPassive()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            const AttackTypeMask legacyTypes =
                AttackTypeMask.Match
                | AttackTypeMask.Active
                | AttackTypeMask.Additional
                | AttackTypeMask.Chase
                | AttackTypeMask.DamageOverTime;
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "legacy_attack_types",
                0.50d,
                legacyTypes));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.Passive,
                    AttackTag.None);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.Zero);
        }

        [TestCase(AttackType.Match, AttackTag.Match3, 0d)]
        [TestCase(AttackType.Match, AttackTag.Match4, 0d)]
        [TestCase(AttackType.Match, AttackTag.Match5Plus, 0.15d)]
        [TestCase(AttackType.Additional, AttackTag.Match5Plus, 0d)]
        public void RequiredTagsMustAllMatch(
            AttackType attackType,
            AttackTag attackTags,
            double expected)
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "match_five_damage",
                0.15d,
                AttackTypeMask.Match,
                AttackTag.Match5Plus));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    attackType,
                    attackTags);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(expected).Within(0.000000001d));
        }

        [Test]
        public void MatchingEffectsFromDifferentSourcesSumByCategory()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "additional_personal",
                0.12d,
                AttackTypeMask.Additional));
            party.Effects.ApplyEffect(AttackTypeEffect(
                2,
                "additional_party",
                0.10d,
                AttackTypeMask.Additional));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.Additional,
                    AttackTag.None);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.22d).Within(0.000000001d));
        }

        [Test]
        public void LegacyCollectorDoesNotAssumeAnAttackType()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "match_damage",
                0.5d,
                AttackTypeMask.Match));

            AllyDamageModifierSnapshot snapshot =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water);

            Assert.That(
                snapshot.AttackTypeDamageIncreaseRateSum,
                Is.Zero);
        }

        [Test]
        public void DamageOverTimeTypeIsIndependentFromFixedDamageTag()
        {
            CreateStates(
                out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "damage_over_time",
                0.10d,
                AttackTypeMask.DamageOverTime));
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                2,
                "fixed_damage_over_time",
                0.20d,
                AttackTypeMask.DamageOverTime,
                AttackTag.FixedDamage));

            AllyDamageModifierSnapshot attackBased =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.DamageOverTime,
                    AttackTag.None);
            AllyDamageModifierSnapshot fixedDamageOverTime =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.DamageOverTime,
                    AttackTag.FixedDamage);
            AllyDamageModifierSnapshot immediateFixedDamage =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.Active,
                    AttackTag.FixedDamage);

            Assert.That(
                attackBased.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.10d).Within(0.000000001d));
            Assert.That(
                fixedDamageOverTime.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.30d).Within(0.000000001d));
            Assert.That(
                immediateFixedDamage.AttackTypeDamageIncreaseRateSum,
                Is.Zero);
        }

        private static EffectInstance AttackTypeEffect(
            long id,
            string effectId,
            double magnitude,
            AttackTypeMask mask,
            AttackTag requiredTags = AttackTag.None)
        {
            return new EffectInstance(
                id,
                effectId,
                "source",
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                magnitude,
                2,
                id,
                attackTypeMask: mask,
                requiredAttackTags: requiredTags);
        }

        private static void CreateStates(
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Water,
                100,
                100d);
            party = new PartyBattleState(
                new[] { attacker });
            boss = new BossBattleState(
                "boss",
                ElementType.Water,
                1000,
                100d);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Beatrice
{
    public sealed class BeatriceBloodstoneLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly BeatriceBloodstoneBuildModifier buildModifier =
            new BeatriceBloodstoneBuildModifier();

        public string CharacterId => BeatriceBloodstoneRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            BeatriceBloodstoneCombatConfig config = RequireConfig(definition);
            BeatriceBloodstoneResolvedCombatRules rules =
                BeatriceBloodstoneCombatRuleResolver.Resolve(
                    config,
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "beatrice.skill.match3.name",
                        "beatrice.skill.match3.description",
                        Arguments(
                            ("hp_cost", Percent(rules.Match3HpCostRate)),
                            ("base_coefficient", Percent(
                                rules.Match3BaseCoefficient)),
                            ("low_hp_threshold", Percent(
                                rules.LowHpThresholdRate)),
                            ("conditional_coefficient", Percent(
                                rules.Match3LowHpBonusCoefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "beatrice.skill.match4.name",
                        "beatrice.skill.match4.description",
                        Arguments(
                            ("hp_cost", Percent(rules.Match4HpCostRate)),
                            ("base_coefficient", Percent(
                                rules.Match4BaseCoefficient)),
                            ("low_hp_threshold", Percent(
                                rules.LowHpThresholdRate)),
                            ("conditional_coefficient", Percent(
                                rules.Match4LowHpBonusCoefficient))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "beatrice.skill.match5.name",
                        "beatrice.skill.match5.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.Match5BaseCoefficient)),
                            ("lost_hp_bonus_coefficient", Percent(
                                rules.Match5LostHpBonusCoefficient)),
                            ("lifesteal_min", Percent(
                                rules.Match5BaseLifestealRate)),
                            ("lifesteal_max", Percent(
                                rules.Match5BaseLifestealRate
                                    + rules
                                        .Match5LostHpLifestealRate))));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "beatrice.skill.passive.name",
                        "beatrice.skill.passive.description",
                        Arguments(
                            ("max_damage_increase", Percent(
                                rules
                                    .PassiveMaxDealtDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "beatrice.skill.active.name",
                        "beatrice.skill.active.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.ActiveBaseCoefficient)),
                            ("lost_hp_bonus_coefficient", Percent(
                                rules.ActiveLostHpBonusCoefficient)),
                            ("lifesteal_min", Percent(
                                rules.ActiveBaseLifestealRate)),
                            ("lifesteal_max", Percent(
                                rules.ActiveBaseLifestealRate
                                    + rules.ActiveLostHpLifestealRate)),
                            ("cooldown", rules.ActiveCooldownTurns)));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(skillType),
                        skillType,
                        "Skill type is not supported.");
            }
        }

        public CharacterLookupDescriptionDefinition ResolveAwakening(
            CharacterDefinition definition,
            int awakeningStage)
        {
            if (awakeningStage < 1 || awakeningStage > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakeningStage));
            }

            BeatriceBloodstoneCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                BeatriceBloodstoneResolvedCombatRules before =
                    BeatriceBloodstoneCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                BeatriceBloodstoneResolvedCombatRules after =
                    BeatriceBloodstoneCombatRuleResolver.Resolve(
                        config,
                        awakeningStage);
                description = AwakeningDescription(
                    awakeningStage,
                    before,
                    after);
            }

            return new CharacterLookupDescriptionDefinition(
                Text(
                    AwakeningTitleKey,
                    Arguments(
                        ("stage", awakeningStage),
                        ("name", Text(
                            $"beatrice.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(
            int awakeningStage)
        {
            var before = buildModifier.GetStatMultipliers(
                awakeningStage - 1);
            var after = buildModifier.GetStatMultipliers(awakeningStage);
            bool attack = awakeningStage == 1;
            return Text(
                $"beatrice.awakening.{awakeningStage}.description",
                Arguments(
                    ("multiplier_before", Percent(
                        attack
                            ? before.AttackMultiplier
                            : before.MaxHpMultiplier)),
                    ("multiplier_after", Percent(
                        attack
                            ? after.AttackMultiplier
                            : after.MaxHpMultiplier))));
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            BeatriceBloodstoneResolvedCombatRules before,
            BeatriceBloodstoneResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "beatrice.awakening.3.description",
                        Arguments(
                            ("damage_before", Percent(before
                                .PassiveMaxDealtDamageIncreaseRate)),
                            ("damage_after", Percent(after
                                .PassiveMaxDealtDamageIncreaseRate))));
                case 4:
                    return Text(
                        "beatrice.awakening.4.description",
                        Arguments(
                            ("match3_base_before", Percent(
                                before.Match3BaseCoefficient)),
                            ("match3_base_after", Percent(
                                after.Match3BaseCoefficient)),
                            ("match3_conditional_before", Percent(
                                before.Match3LowHpBonusCoefficient)),
                            ("match3_conditional_after", Percent(
                                after.Match3LowHpBonusCoefficient)),
                            ("match4_base_before", Percent(
                                before.Match4BaseCoefficient)),
                            ("match4_base_after", Percent(
                                after.Match4BaseCoefficient)),
                            ("match4_conditional_before", Percent(
                                before.Match4LowHpBonusCoefficient)),
                            ("match4_conditional_after", Percent(
                                after.Match4LowHpBonusCoefficient))));
                case 5:
                    return Text(
                        "beatrice.awakening.5.description",
                        Arguments(
                            ("base_before", Percent(
                                before.Match5BaseCoefficient)),
                            ("base_after", Percent(
                                after.Match5BaseCoefficient)),
                            ("lost_hp_bonus_before", Percent(
                                before.Match5LostHpBonusCoefficient)),
                            ("lost_hp_bonus_after", Percent(
                                after.Match5LostHpBonusCoefficient))));
                case 6:
                    return Text(
                        "beatrice.awakening.6.description",
                        Arguments(
                            ("base_before", Percent(
                                before.ActiveBaseCoefficient)),
                            ("base_after", Percent(
                                after.ActiveBaseCoefficient)),
                            ("lost_hp_bonus_before", Percent(
                                before.ActiveLostHpBonusCoefficient)),
                            ("lost_hp_bonus_after", Percent(
                                after.ActiveLostHpBonusCoefficient))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition Skill(
            string titleKey,
            string descriptionKey,
            IReadOnlyDictionary<string, object> arguments)
        {
            return new CharacterLookupDescriptionDefinition(
                Text(titleKey),
                new[] { Text(descriptionKey, arguments) });
        }

        private static BeatriceBloodstoneCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    BeatriceBloodstoneRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Beatrice's lookup provider requires Beatrice's "
                        + "definition.",
                    nameof(definition));
            }

            return definition.CombatConfig
                    as BeatriceBloodstoneCombatConfig
                ?? throw new InvalidOperationException(
                    "Beatrice requires BeatriceBloodstoneCombatConfig.");
        }

        private static CharacterLookupLocalizedText Text(
            string key,
            IReadOnlyDictionary<string, object> arguments = null)
        {
            return new CharacterLookupLocalizedText(key, arguments);
        }

        private static IReadOnlyDictionary<string, object> Arguments(
            params (string key, object value)[] values)
        {
            var arguments = new Dictionary<string, object>(
                values.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < values.Length; index++)
            {
                arguments.Add(values[index].key, values[index].value);
            }

            return arguments;
        }

        private static string Percent(double rate)
        {
            return (rate * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }
    }
}

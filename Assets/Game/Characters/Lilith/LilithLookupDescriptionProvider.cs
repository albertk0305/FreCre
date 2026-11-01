using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lilith
{
    public sealed class LilithLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly LilithBuildModifier buildModifier =
            new LilithBuildModifier();

        public string CharacterId => LilithRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            LilithCombatConfig config = RequireConfig(definition);
            LilithResolvedCombatRules rules =
                LilithCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "lilith.skill.match3.name",
                        "lilith.skill.match3.description",
                        Arguments(
                            ("damage_coefficient", Percent(
                                rules.Match3Coefficient)),
                            ("contract_gain",
                                rules.Match3ContractGainAmount)));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "lilith.skill.match4.name",
                        "lilith.skill.match4.description",
                        Arguments(
                            ("damage_coefficient", Percent(
                                rules.Match4Coefficient)),
                            ("contract_gain",
                                rules.Match4ContractGainAmount)));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "lilith.skill.match5.name",
                        "lilith.skill.match5.description",
                        Arguments(
                            ("damage_coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("contract_gain",
                                rules.Match5ContractGainAmount),
                            ("damage_reduction", Percent(rules
                                .CorruptedBlessingDamageReductionRate)),
                            ("duration_turns",
                                rules.CorruptedBlessingDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "lilith.skill.passive.name",
                        "lilith.skill.passive.description",
                        Arguments(
                            ("contract_max",
                                rules.ContractMaxStackCount),
                            ("damage_increase_per_stack", Percent(rules
                                .ContractDamageIncreaseRatePerStack)),
                            ("consumption_unit",
                                rules.ContractConsumptionUnit),
                            ("explosion_coefficient_per_unit", Percent(
                                rules
                                    .ContractExplosionCoefficientPerUnit))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "lilith.skill.active.name",
                        "lilith.skill.active.description",
                        Arguments(
                            ("current_hp_cost", Percent(
                                rules.ActiveCurrentPartyHpCostRate)),
                            ("contract_gain",
                                rules.ActiveContractGainAmount),
                            ("minimum_hp",
                                rules.ActiveMinimumPartyHpAfterCost),
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

            LilithCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                LilithResolvedCombatRules before =
                    LilithCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                LilithResolvedCombatRules after =
                    LilithCombatRuleResolver.Resolve(
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
                            $"lilith.awakening.{awakeningStage}.name")))),
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
                $"lilith.awakening.{awakeningStage}.description",
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
            LilithResolvedCombatRules before,
            LilithResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "lilith.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3Coefficient)),
                            ("match3_after", Percent(
                                after.Match3Coefficient)),
                            ("match4_before", Percent(
                                before.Match4Coefficient)),
                            ("match4_after", Percent(
                                after.Match4Coefficient))));
                case 4:
                    return Text(
                        "lilith.awakening.4.description",
                        Arguments(
                            ("match5_before", Percent(
                                before.Match5Coefficient)),
                            ("match5_after", Percent(
                                after.Match5Coefficient)),
                            ("damage_reduction_before", Percent(before
                                .CorruptedBlessingDamageReductionRate)),
                            ("damage_reduction_after", Percent(after
                                .CorruptedBlessingDamageReductionRate))));
                case 5:
                    return Text(
                        "lilith.awakening.5.description",
                        Arguments(
                            ("explosion_before", Percent(before
                                .ContractExplosionCoefficientPerUnit)),
                            ("explosion_after", Percent(after
                                .ContractExplosionCoefficientPerUnit))));
                case 6:
                    return Text(
                        "lilith.awakening.6.description",
                        Arguments(
                            ("damage_increase_before", Percent(before
                                .ContractDamageIncreaseRatePerStack)),
                            ("damage_increase_after", Percent(after
                                .ContractDamageIncreaseRatePerStack))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static LilithCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    LilithRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Lilith's lookup provider requires Lilith's "
                        + "definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as LilithCombatConfig
                ?? throw new InvalidOperationException(
                    "Lilith requires LilithCombatConfig.");
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

using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly LucielBuildModifier buildModifier =
            new LucielBuildModifier();

        public string CharacterId => LucielRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            LucielResolvedCombatRules rules =
                LucielCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return ShieldSkill(
                        "match3",
                        rules.Match3ShieldMaxHpRate,
                        rules.ActionLocalShieldAmountIncreaseRate);
                case CharacterLookupSkillType.Match4:
                    return ShieldSkill(
                        "match4",
                        rules.Match4ShieldMaxHpRate,
                        rules.ActionLocalShieldAmountIncreaseRate);
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "luciel.skill.match5.name",
                        "luciel.skill.match5.description",
                        Arguments(
                            ("shield_rate", Percent(
                                rules.Match5ShieldMaxHpRate)),
                            ("shield_amount_increase", Percent(rules
                                .ActionLocalShieldAmountIncreaseRate)),
                            ("damage_increase", Percent(rules
                                .Match5PartyDealtDamageIncreaseRate)),
                            ("duration",
                                rules.Match5BuffDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "luciel.skill.passive.name",
                        "luciel.skill.passive.description",
                        Arguments(
                            ("damage_reduction", Percent(
                                rules.PassiveDamageReductionRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "luciel.skill.active.name",
                        "luciel.skill.active.description",
                        Arguments(
                            ("current_hp_cost", Percent(
                                rules.ActiveCurrentPartyHpCostRate)),
                            ("minimum_hp",
                                rules.ActiveMinimumPartyHpAfterCost),
                            ("shield_rate", Percent(
                                rules.ActiveShieldMaxHpRate)),
                            ("shield_amount_increase", Percent(rules
                                .ActionLocalShieldAmountIncreaseRate)),
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

            LucielCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1)
            {
                CharacterBuildStatMultipliers before =
                    buildModifier.GetStatMultipliers(0);
                CharacterBuildStatMultipliers after =
                    buildModifier.GetStatMultipliers(1);
                description = Text(
                    "luciel.awakening.1.description",
                    Arguments(
                        ("multiplier_before", Percent(
                            before.MaxHpMultiplier)),
                        ("multiplier_after", Percent(
                            after.MaxHpMultiplier))));
            }
            else
            {
                LucielResolvedCombatRules before =
                    LucielCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                LucielResolvedCombatRules after =
                    LucielCombatRuleResolver.Resolve(
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
                            $"luciel.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            LucielResolvedCombatRules before,
            LucielResolvedCombatRules after)
        {
            switch (stage)
            {
                case 2:
                    return Text(
                        "luciel.awakening.2.description",
                        Arguments(
                            ("shield_increase_before", Percent(before
                                .ActionLocalShieldAmountIncreaseRate)),
                            ("shield_increase_after", Percent(after
                                .ActionLocalShieldAmountIncreaseRate))));
                case 3:
                    return Text(
                        "luciel.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3ShieldMaxHpRate)),
                            ("match3_after", Percent(
                                after.Match3ShieldMaxHpRate)),
                            ("match4_before", Percent(
                                before.Match4ShieldMaxHpRate)),
                            ("match4_after", Percent(
                                after.Match4ShieldMaxHpRate))));
                case 4:
                    return Text(
                        "luciel.awakening.4.description",
                        Arguments(
                            ("shield_before", Percent(
                                before.Match5ShieldMaxHpRate)),
                            ("shield_after", Percent(
                                after.Match5ShieldMaxHpRate)),
                            ("damage_increase_before", Percent(before
                                .Match5PartyDealtDamageIncreaseRate)),
                            ("damage_increase_after", Percent(after
                                .Match5PartyDealtDamageIncreaseRate))));
                case 5:
                    return Text(
                        "luciel.awakening.5.description",
                        Arguments(
                            ("reduction_before", Percent(
                                before.PassiveDamageReductionRate)),
                            ("reduction_after", Percent(
                                after.PassiveDamageReductionRate))));
                case 6:
                    return Text(
                        "luciel.awakening.6.description",
                        Arguments(
                            ("shield_before", Percent(
                                before.ActiveShieldMaxHpRate)),
                            ("shield_after", Percent(
                                after.ActiveShieldMaxHpRate))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition ShieldSkill(
            string tier,
            double shieldRate,
            double shieldAmountIncreaseRate)
        {
            return Skill(
                $"luciel.skill.{tier}.name",
                $"luciel.skill.{tier}.description",
                Arguments(
                    ("shield_rate", Percent(shieldRate)),
                    ("shield_amount_increase", Percent(
                        shieldAmountIncreaseRate))));
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

        private static LucielCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    LucielRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Luciel's lookup provider requires Luciel's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as LucielCombatConfig
                ?? throw new InvalidOperationException(
                    "Luciel requires LucielCombatConfig.");
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

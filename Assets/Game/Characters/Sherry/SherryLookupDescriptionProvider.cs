using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Sherry
{
    public sealed class SherryLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly SherryBuildModifier buildModifier =
            new SherryBuildModifier();

        public string CharacterId => SherryRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            SherryCombatConfig config = RequireConfig(definition);
            SherryResolvedCombatRules rules =
                SherryCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill("match3", rules.Match3Coefficient,
                        rules.Match3OniIntimidationRate, rules);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill("match4", rules.Match4Coefficient,
                        rules.Match4OniIntimidationRate, rules);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill("match5", rules.Match5Coefficient,
                        rules.Match5OniIntimidationRate, rules);
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "sherry.skill.passive.name",
                        "sherry.skill.passive.description",
                        Arguments(
                            ("max_rank", rules.StylishMaxRank),
                            ("max_damage_increase", Percent(
                                rules.StylishMaxDamageIncreaseRate)),
                            ("exponent_base", Number(
                                rules.StylishDamageExponentBase))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "sherry.skill.active.name",
                        "sherry.skill.active.description",
                        JackpotArguments(rules));
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
                throw new ArgumentOutOfRangeException(nameof(awakeningStage));
            }

            SherryCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                var before = buildModifier.GetStatMultipliers(
                    awakeningStage - 1);
                var after = buildModifier.GetStatMultipliers(awakeningStage);
                bool attack = awakeningStage == 1;
                description = Text(
                    $"sherry.awakening.{awakeningStage}.description",
                    Arguments(
                        ("multiplier_before", Percent(attack
                            ? before.AttackMultiplier
                            : before.MaxHpMultiplier)),
                        ("multiplier_after", Percent(attack
                            ? after.AttackMultiplier
                            : after.MaxHpMultiplier))));
            }
            else
            {
                SherryResolvedCombatRules before =
                    SherryCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                SherryResolvedCombatRules after =
                    SherryCombatRuleResolver.Resolve(config, awakeningStage);
                description = ResolveRuleAwakening(
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
                            $"sherry.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string suffix,
            double coefficient,
            double oniRate,
            SherryResolvedCombatRules rules)
        {
            return Skill(
                $"sherry.skill.{suffix}.name",
                $"sherry.skill.{suffix}.description",
                Arguments(
                    ("coefficient", Percent(coefficient)),
                    ("minimum_rank", rules.OniIntimidationMinimumRank),
                    ("oni_rate", Percent(oniRate)),
                    ("duration", rules.OniIntimidationDurationTurns)));
        }

        private static CharacterLookupLocalizedText ResolveRuleAwakening(
            int stage,
            SherryResolvedCombatRules before,
            SherryResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "sherry.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3Coefficient)),
                            ("match3_after", Percent(after.Match3Coefficient)),
                            ("match4_before", Percent(before.Match4Coefficient)),
                            ("match4_after", Percent(after.Match4Coefficient)),
                            ("match5_before", Percent(before.Match5Coefficient)),
                            ("match5_after", Percent(after.Match5Coefficient))));
                case 4:
                    return Text(
                        "sherry.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3OniIntimidationRate)),
                            ("match3_after", Percent(after.Match3OniIntimidationRate)),
                            ("match4_before", Percent(before.Match4OniIntimidationRate)),
                            ("match4_after", Percent(after.Match4OniIntimidationRate)),
                            ("match5_before", Percent(before.Match5OniIntimidationRate)),
                            ("match5_after", Percent(after.Match5OniIntimidationRate))));
                case 5:
                    return Text(
                        "sherry.awakening.5.description",
                        Arguments(
                            ("damage_before", Percent(before.StylishMaxDamageIncreaseRate)),
                            ("damage_after", Percent(after.StylishMaxDamageIncreaseRate))));
                case 6:
                    return Text(
                        "sherry.awakening.6.description",
                        Arguments(
                            ("jackpot_before", Percent(before.JackpotMaxCoefficient)),
                            ("jackpot_after", Percent(after.JackpotMaxCoefficient))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static IReadOnlyDictionary<string, object> JackpotArguments(
            SherryResolvedCombatRules rules)
        {
            var values = new List<(string key, object value)>
            {
                ("max_rank", rules.StylishMaxRank),
                ("max_coefficient", Percent(rules.JackpotMaxCoefficient)),
                ("cooldown", rules.ActiveCooldownTurns)
            };
            for (int rank = 1; rank <= rules.StylishMaxRank; rank++)
            {
                values.Add(($"rank{rank}", Percent(
                    rules.ResolveJackpotCoefficient(rank))));
            }

            return Arguments(values.ToArray());
        }

        private static SherryCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    SherryRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Sherry's lookup provider requires Sherry's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as SherryCombatConfig
                ?? throw new InvalidOperationException(
                    "Sherry requires SherryCombatConfig.");
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
            var result = new Dictionary<string, object>(
                values.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < values.Length; index++)
            {
                result.Add(values[index].key, values[index].value);
            }

            return result;
        }

        private static string Percent(double rate)
        {
            return (rate * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        private static string Number(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}

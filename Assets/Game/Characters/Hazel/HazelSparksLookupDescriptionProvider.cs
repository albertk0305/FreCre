using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Hazel
{
    public sealed class HazelSparksLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";
        private readonly HazelSparksBuildModifier buildModifier =
            new HazelSparksBuildModifier();

        public string CharacterId => HazelSparksRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            HazelSparksCombatConfig config = RequireConfig(definition);
            HazelSparksResolvedCombatRules rules =
                HazelSparksCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill(
                        "match3",
                        rules.Match3DamageCoefficient,
                        rules.Match3SharpeningGain,
                        rules);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill(
                        "match4",
                        rules.Match4DamageCoefficient,
                        rules.Match4SharpeningGain,
                        rules);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill(
                        "match5",
                        rules.Match5DamageCoefficient,
                        rules.Match5SharpeningGain,
                        rules);
                case CharacterLookupSkillType.Passive:
                    return PassiveSkill(rules);
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "hazel.skill.active.name",
                        "hazel.skill.active.description",
                        Arguments(
                            ("block_count",
                                rules.ActiveBlockConversionCount),
                            ("cooldown", rules.ActiveCooldownTurns)));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(skillType));
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

            HazelSparksCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                CharacterBuildStatMultipliers before =
                    buildModifier.GetStatMultipliers(awakeningStage - 1);
                CharacterBuildStatMultipliers after =
                    buildModifier.GetStatMultipliers(awakeningStage);
                bool maxHp = awakeningStage == 1;
                description = Text(
                    $"hazel.awakening.{awakeningStage}.description",
                    Arguments(
                        ("multiplier_before", Percent(maxHp
                            ? before.MaxHpMultiplier
                            : before.AttackMultiplier)),
                        ("multiplier_after", Percent(maxHp
                            ? after.MaxHpMultiplier
                            : after.AttackMultiplier))));
            }
            else
            {
                HazelSparksResolvedCombatRules before =
                    HazelSparksCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                HazelSparksResolvedCombatRules after =
                    HazelSparksCombatRuleResolver.Resolve(
                        config,
                        awakeningStage);
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
                            $"hazel.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupDescriptionDefinition PassiveSkill(
            HazelSparksResolvedCombatRules rules)
        {
            return new CharacterLookupDescriptionDefinition(
                Text("hazel.skill.passive.name"),
                new[]
                {
                    Text(
                        "hazel.skill.passive.description",
                        Arguments(
                            ("rate_per_stack", Percent(
                                rules.ArtisansTouchRatePerStack)),
                            ("max_stacks",
                                rules.MaximumSharpeningStacks),
                            ("max_damage_increase", Percent(
                                rules.CalculateArtisansTouchMagnitude(
                                    rules.MaximumSharpeningStacks))))),
                    Text(
                        "hazel.resource.sharpening.description",
                        Arguments(
                            ("initial_stacks",
                                rules.InitialSharpeningStacks),
                            ("max_stacks",
                                rules.MaximumSharpeningStacks),
                            ("match3_gain",
                                rules.Match3SharpeningGain),
                            ("match4_gain",
                                rules.Match4SharpeningGain),
                            ("match5_gain",
                                rules.Match5SharpeningGain))),
                    Text(
                        "hazel.effect.forged_momentum.description",
                        Arguments(
                            ("rate_per_overflow", Percent(
                                rules.ForgedMomentumRatePerOverflow)),
                            ("duration",
                                rules.ForgedMomentumDurationTurns)))
                });
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string tier,
            double coefficient,
            int sharpeningGain,
            HazelSparksResolvedCombatRules rules)
        {
            return Skill(
                $"hazel.skill.{tier}.name",
                $"hazel.skill.{tier}.description",
                Arguments(
                    ("coefficient", Percent(coefficient)),
                    ("sharpening_gain", sharpeningGain),
                    ("max_stacks", rules.MaximumSharpeningStacks),
                    ("overflow_attack_increase", Percent(
                        rules.ForgedMomentumRatePerOverflow))));
        }

        private static CharacterLookupLocalizedText ResolveRuleAwakening(
            int stage,
            HazelSparksResolvedCombatRules before,
            HazelSparksResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "hazel.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3DamageCoefficient)),
                            ("match3_after", Percent(
                                after.Match3DamageCoefficient)),
                            ("match4_before", Percent(
                                before.Match4DamageCoefficient)),
                            ("match4_after", Percent(
                                after.Match4DamageCoefficient)),
                            ("match5_before", Percent(
                                before.Match5DamageCoefficient)),
                            ("match5_after", Percent(
                                after.Match5DamageCoefficient))));
                case 4:
                    return Text(
                        "hazel.awakening.4.description",
                        Arguments(
                            ("rate_before", Percent(
                                before.ArtisansTouchRatePerStack)),
                            ("rate_after", Percent(
                                after.ArtisansTouchRatePerStack)),
                            ("maximum_before", Percent(
                                before.CalculateArtisansTouchMagnitude(
                                    before.MaximumSharpeningStacks))),
                            ("maximum_after", Percent(
                                after.CalculateArtisansTouchMagnitude(
                                    after.MaximumSharpeningStacks)))));
                case 5:
                    return Text(
                        "hazel.awakening.5.description",
                        Arguments(
                            ("rate_before", Percent(
                                before.ForgedMomentumRatePerOverflow)),
                            ("rate_after", Percent(
                                after.ForgedMomentumRatePerOverflow))));
                case 6:
                    return Text(
                        "hazel.awakening.6.description",
                        Arguments(
                            ("initial_before",
                                before.InitialSharpeningStacks),
                            ("initial_after",
                                after.InitialSharpeningStacks),
                            ("damage_before", Percent(
                                before.CalculateArtisansTouchMagnitude(
                                    before.InitialSharpeningStacks))),
                            ("damage_after", Percent(
                                after.CalculateArtisansTouchMagnitude(
                                    after.InitialSharpeningStacks)))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static HazelSparksCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    HazelSparksRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Hazel lookup provider requires Hazel's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as HazelSparksCombatConfig
                ?? throw new InvalidOperationException(
                    "Hazel requires HazelSparksCombatConfig.");
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

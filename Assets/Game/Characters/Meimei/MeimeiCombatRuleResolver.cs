using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Meimei
{
    public sealed class MeimeiResolvedCombatRules
    {
        internal MeimeiResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            double drunkenImmortalDanceCoefficient,
            int tipsinessMaxAmount,
            int match4TipsinessGainAmount,
            int match5TipsinessGainAmount,
            int activeCooldownTurns,
            double criticalChancePerTipsiness)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            DrunkenImmortalDanceCoefficient =
                drunkenImmortalDanceCoefficient;
            TipsinessMaxAmount = tipsinessMaxAmount;
            Match4TipsinessGainAmount = match4TipsinessGainAmount;
            Match5TipsinessGainAmount = match5TipsinessGainAmount;
            ActiveCooldownTurns = activeCooldownTurns;
            CriticalChancePerTipsiness = criticalChancePerTipsiness;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public double DrunkenImmortalDanceCoefficient { get; }
        public int TipsinessMaxAmount { get; }
        public int Match4TipsinessGainAmount { get; }
        public int Match5TipsinessGainAmount { get; }
        public int ActiveCooldownTurns { get; }
        public double CriticalChancePerTipsiness { get; }

        public double CalculateCriticalChanceIncreaseRate(
            int effectiveTipsiness)
        {
            if (effectiveTipsiness < 0
                || effectiveTipsiness > TipsinessMaxAmount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectiveTipsiness),
                    effectiveTipsiness,
                    $"Effective tipsiness must be between zero and "
                        + $"{TipsinessMaxAmount}.");
            }

            return CriticalChancePerTipsiness * effectiveTipsiness;
        }
    }

    public static class MeimeiCombatRuleResolver
    {
        private const int Match3And4EnhancementAwakening = 3;
        private const int Match5EnhancementAwakening = 4;
        private const int PassiveEnhancementAwakening = 5;
        private const int CriticalChanceAwakening = 6;

        public static MeimeiResolvedCombatRules Resolve(
            MeimeiCombatConfig config,
            int awakening)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new MeimeiResolvedCombatRules(
                awakening >= Match3And4EnhancementAwakening
                    ? config.Awakening3Match3Coefficient
                    : config.Match3Coefficient,
                awakening >= Match3And4EnhancementAwakening
                    ? config.Awakening3Match4Coefficient
                    : config.Match4Coefficient,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening4Match5Coefficient
                    : config.Match5Coefficient,
                awakening >= PassiveEnhancementAwakening
                    ? config.Awakening5DrunkenImmortalDanceCoefficient
                    : config.DrunkenImmortalDanceCoefficient,
                config.TipsinessMaxAmount,
                config.Match4TipsinessGainAmount,
                config.Match5TipsinessGainAmount,
                config.ActiveCooldownTurns,
                awakening >= CriticalChanceAwakening
                    ? config.Awakening6CriticalChancePerTipsiness
                    : 0d);
        }
    }

    public sealed class MeimeiLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly MeimeiBuildModifier buildModifier =
            new MeimeiBuildModifier();

        public string CharacterId => MeimeiRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            MeimeiCombatConfig config = RequireConfig(definition);
            MeimeiResolvedCombatRules rules =
                MeimeiCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "meimei.skill.match3.name",
                        "meimei.skill.match3.description",
                        Arguments(("coefficient", Percent(
                            rules.Match3Coefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "meimei.skill.match4.name",
                        "meimei.skill.match4.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match4Coefficient)),
                            ("tipsiness_gain",
                                rules.Match4TipsinessGainAmount)));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "meimei.skill.match5.name",
                        "meimei.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("tipsiness_gain",
                                rules.Match5TipsinessGainAmount)));
                case CharacterLookupSkillType.Passive:
                    return new CharacterLookupDescriptionDefinition(
                        Text("meimei.skill.passive.name"),
                        new[]
                        {
                            Text(
                                "meimei.skill.passive.description",
                                Arguments(
                                    ("tipsiness_max",
                                        rules.TipsinessMaxAmount))),
                            Text(
                                "meimei.skill.passive.additional_description",
                                Arguments(
                                    ("coefficient", Percent(rules
                                        .DrunkenImmortalDanceCoefficient))))
                        });
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "meimei.skill.active.name",
                        "meimei.skill.active.description",
                        Arguments(
                            ("maximum_conversion",
                                rules.TipsinessMaxAmount),
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

            MeimeiCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                CharacterBuildStatMultipliers before =
                    buildModifier.GetStatMultipliers(awakeningStage - 1);
                CharacterBuildStatMultipliers after =
                    buildModifier.GetStatMultipliers(awakeningStage);
                description = Text(
                    $"meimei.awakening.{awakeningStage}.description",
                    awakeningStage == 1
                        ? Arguments(
                            ("multiplier_before", Percent(
                                before.AttackMultiplier)),
                            ("multiplier_after", Percent(
                                after.AttackMultiplier)))
                        : Arguments(
                            ("multiplier_before", Percent(
                                before.MaxHpMultiplier)),
                            ("multiplier_after", Percent(
                                after.MaxHpMultiplier))));
            }
            else
            {
                MeimeiResolvedCombatRules before =
                    MeimeiCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                MeimeiResolvedCombatRules after =
                    MeimeiCombatRuleResolver.Resolve(
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
                            $"meimei.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            MeimeiResolvedCombatRules before,
            MeimeiResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 3:
                    return Text(
                        "meimei.awakening.3.description",
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
                        "meimei.awakening.4.description",
                        Arguments(
                            ("match5_before", Percent(
                                before.Match5Coefficient)),
                            ("match5_after", Percent(
                                after.Match5Coefficient))));
                case 5:
                    return Text(
                        "meimei.awakening.5.description",
                        Arguments(
                            ("additional_before", Percent(before
                                .DrunkenImmortalDanceCoefficient)),
                            ("additional_after", Percent(after
                                .DrunkenImmortalDanceCoefficient))));
                case 6:
                    return Text(
                        "meimei.awakening.6.description",
                        Arguments(
                            ("critical_before", Percent(
                                before.CriticalChancePerTipsiness)),
                            ("critical_after", Percent(
                                after.CriticalChancePerTipsiness))));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(awakeningStage));
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

        private static MeimeiCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    MeimeiRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Meimei's lookup provider requires Meimei's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as MeimeiCombatConfig
                ?? throw new InvalidOperationException(
                    "Meimei requires MeimeiCombatConfig.");
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

using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Chloe
{
    public sealed class ChloeValentineLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";
        private readonly ChloeValentineBuildModifier buildModifier =
            new ChloeValentineBuildModifier();

        public string CharacterId => ChloeValentineRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            ChloeValentineResolvedCombatRules rules =
                ChloeValentineCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "chloe.skill.match3.name",
                        "chloe.skill.match3.description",
                        Arguments(("healing", Percent(rules.Match3HealingCoefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "chloe.skill.match4.name",
                        "chloe.skill.match4.description",
                        Arguments(("healing", Percent(rules.Match4HealingCoefficient))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "chloe.skill.match5.name",
                        "chloe.skill.match5.description",
                        Arguments(
                            ("healing", Percent(rules.Match5HealingCoefficient)),
                            ("critical_chance", Percent(rules.Match5CriticalChanceIncreaseRate)),
                            ("duration", rules.Match5BuffDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "chloe.skill.passive.name",
                        "chloe.skill.passive.description",
                        Arguments(("attack_increase", Percent(rules.PassivePartyAttackIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "chloe.skill.active.name",
                        "chloe.skill.active.description",
                        Arguments(
                            ("healing", Percent(rules.ActiveHealingCoefficient)),
                            ("critical_damage", Percent(rules.ActiveCriticalDamageIncreaseRate)),
                            ("duration", rules.ActiveBuffDurationTurns),
                            ("cooldown", rules.ActiveCooldownTurns)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(skillType));
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

            ChloeValentineCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1)
            {
                CharacterBuildStatMultipliers before =
                    buildModifier.GetStatMultipliers(0);
                CharacterBuildStatMultipliers after =
                    buildModifier.GetStatMultipliers(1);
                description = Text(
                    "chloe.awakening.1.description",
                    Arguments(
                        ("multiplier_before", Percent(before.MaxHpMultiplier)),
                        ("multiplier_after", Percent(after.MaxHpMultiplier))));
            }
            else
            {
                ChloeValentineResolvedCombatRules before =
                    ChloeValentineCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                ChloeValentineResolvedCombatRules after =
                    ChloeValentineCombatRuleResolver.Resolve(
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
                        ("name", Text($"chloe.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            ChloeValentineResolvedCombatRules before,
            ChloeValentineResolvedCombatRules after)
        {
            switch (stage)
            {
                case 2:
                    return Text(
                        "chloe.awakening.2.description",
                        Arguments(
                            ("healing_before", Percent(before.ActionLocalHealingIncreaseRate)),
                            ("healing_after", Percent(after.ActionLocalHealingIncreaseRate))));
                case 3:
                    return Text(
                        "chloe.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3HealingCoefficient)),
                            ("match3_after", Percent(after.Match3HealingCoefficient)),
                            ("match4_before", Percent(before.Match4HealingCoefficient)),
                            ("match4_after", Percent(after.Match4HealingCoefficient))));
                case 4:
                    return Text(
                        "chloe.awakening.4.description",
                        Arguments(
                            ("healing_before", Percent(before.Match5HealingCoefficient)),
                            ("healing_after", Percent(after.Match5HealingCoefficient)),
                            ("critical_before", Percent(before.Match5CriticalChanceIncreaseRate)),
                            ("critical_after", Percent(after.Match5CriticalChanceIncreaseRate))));
                case 5:
                    return Text(
                        "chloe.awakening.5.description",
                        Arguments(
                            ("healing_before", Percent(before.ActiveHealingCoefficient)),
                            ("healing_after", Percent(after.ActiveHealingCoefficient)),
                            ("critical_before", Percent(before.ActiveCriticalDamageIncreaseRate)),
                            ("critical_after", Percent(after.ActiveCriticalDamageIncreaseRate))));
                case 6:
                    return Text(
                        "chloe.awakening.6.description",
                        Arguments(
                            ("attack_before", Percent(before.PassivePartyAttackIncreaseRate)),
                            ("attack_after", Percent(after.PassivePartyAttackIncreaseRate))));
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

        private static ChloeValentineCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    ChloeValentineRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Chloe's lookup provider requires Chloe's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as ChloeValentineCombatConfig
                ?? throw new InvalidOperationException(
                    "Chloe requires ChloeValentineCombatConfig.");
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


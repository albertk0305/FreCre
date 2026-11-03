using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey = "character.lookup.awakening.title";
        private readonly LiviaBuildModifier buildModifier = new LiviaBuildModifier();

        public string CharacterId => LiviaRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            LiviaResolvedCombatRules rules = LiviaCombatRuleResolver.Resolve(
                RequireConfig(definition),
                awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "livia.skill.match3.name",
                        "livia.skill.match3.description",
                        Arguments(("healing", Percent(rules.Match3HealingCoefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "livia.skill.match4.name",
                        "livia.skill.match4.description",
                        Arguments(("healing", Percent(rules.Match4HealingCoefficient))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "livia.skill.match5.name",
                        "livia.skill.match5.description",
                        Arguments(
                            ("healing", Percent(rules.Match5HealingCoefficient)),
                            ("damage_reduction", Percent(rules.Match5PartyDamageReductionRate)),
                            ("duration", rules.Match5DamageReductionDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "livia.skill.passive.name",
                        "livia.skill.passive.description",
                        Arguments(("healing_increase", Percent(rules.PassivePartyHealingIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "livia.skill.active.name",
                        "livia.skill.active.description",
                        Arguments(
                            ("healing", Percent(rules.ActiveHealingCoefficient)),
                            ("cooldown", rules.ActiveCooldownTurns)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(skillType), skillType, "Skill type is not supported.");
            }
        }

        public CharacterLookupDescriptionDefinition ResolveAwakening(
            CharacterDefinition definition,
            int awakeningStage)
        {
            if (awakeningStage < 1 || awakeningStage > 6)
                throw new ArgumentOutOfRangeException(nameof(awakeningStage));

            LiviaCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1)
            {
                var before = buildModifier.GetStatMultipliers(0);
                var after = buildModifier.GetStatMultipliers(1);
                description = Text(
                    "livia.awakening.1.description",
                    Arguments(
                        ("multiplier_before", Percent(before.MaxHpMultiplier)),
                        ("multiplier_after", Percent(after.MaxHpMultiplier))));
            }
            else
            {
                LiviaResolvedCombatRules before = LiviaCombatRuleResolver.Resolve(config, awakeningStage - 1);
                LiviaResolvedCombatRules after = LiviaCombatRuleResolver.Resolve(config, awakeningStage);
                description = AwakeningDescription(awakeningStage, before, after);
            }

            return new CharacterLookupDescriptionDefinition(
                Text(
                    AwakeningTitleKey,
                    Arguments(
                        ("stage", awakeningStage),
                        ("name", Text($"livia.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            LiviaResolvedCombatRules before,
            LiviaResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 2:
                    return Text(
                        "livia.awakening.2.description",
                        Arguments(
                            ("healing_before", Percent(before.ActionLocalHealingIncreaseRate)),
                            ("healing_after", Percent(after.ActionLocalHealingIncreaseRate))));
                case 3:
                    return Text(
                        "livia.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3HealingCoefficient)),
                            ("match3_after", Percent(after.Match3HealingCoefficient)),
                            ("match4_before", Percent(before.Match4HealingCoefficient)),
                            ("match4_after", Percent(after.Match4HealingCoefficient))));
                case 4:
                    return Text(
                        "livia.awakening.4.description",
                        Arguments(
                            ("match5_before", Percent(before.Match5HealingCoefficient)),
                            ("match5_after", Percent(after.Match5HealingCoefficient))));
                case 5:
                    return Text(
                        "livia.awakening.5.description",
                        Arguments(
                            ("passive_before", Percent(before.PassivePartyHealingIncreaseRate)),
                            ("passive_after", Percent(after.PassivePartyHealingIncreaseRate))));
                case 6:
                    return Text(
                        "livia.awakening.6.description",
                        Arguments(
                            ("active_before", Percent(before.ActiveHealingCoefficient)),
                            ("active_after", Percent(after.ActiveHealingCoefficient))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(awakeningStage));
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

        private static LiviaCombatConfig RequireConfig(CharacterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!string.Equals(definition.Id, LiviaRules.CharacterId, StringComparison.Ordinal))
                throw new ArgumentException("Livia's lookup provider requires Livia's definition.", nameof(definition));
            return definition.CombatConfig as LiviaCombatConfig
                ?? throw new InvalidOperationException("Livia requires LiviaCombatConfig.");
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
            var arguments = new Dictionary<string, object>(values.Length, StringComparer.Ordinal);
            for (int index = 0; index < values.Length; index++)
                arguments.Add(values[index].key, values[index].value);
            return arguments;
        }

        private static string Percent(double rate)
        {
            return (rate * 100d).ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Lancelot
{
    public sealed class LancelotLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly LancelotBuildModifier buildModifier =
            new LancelotBuildModifier();

        public string CharacterId => LancelotRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            LancelotResolvedCombatRules rules =
                LancelotCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "lancelot.skill.match3.name",
                        "lancelot.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match3Coefficient)),
                            ("critical_chance", Percent(
                                rules.PassiveCriticalChanceIncreaseRate))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "lancelot.skill.match4.name",
                        "lancelot.skill.match4.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match4Coefficient)),
                            ("critical_chance", Percent(
                                rules.Match4CriticalChanceIncreaseRate))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "lancelot.skill.match5.name",
                        "lancelot.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("critical_chance", Percent(
                                rules.Match5CriticalChanceIncreaseRate))));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "lancelot.skill.passive.name",
                        "lancelot.skill.passive.description",
                        Arguments(
                            ("critical_chance", Percent(
                                rules.PassiveCriticalChanceIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "lancelot.skill.active.name",
                        "lancelot.skill.active.description",
                        Arguments(
                            ("critical_damage", Percent(
                                rules.ActiveCriticalDamageIncreaseRate)),
                            ("duration", rules.ActiveDurationTurns),
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

            LancelotCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1 || awakeningStage == 2)
            {
                var before = buildModifier.GetStatMultipliers(
                    awakeningStage - 1);
                var after = buildModifier.GetStatMultipliers(awakeningStage);
                bool attack = awakeningStage == 1;
                description = Text(
                    $"lancelot.awakening.{awakeningStage}.description",
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
            else
            {
                LancelotResolvedCombatRules before =
                    LancelotCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                LancelotResolvedCombatRules after =
                    LancelotCombatRuleResolver.Resolve(
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
                            $"lancelot.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            LancelotResolvedCombatRules before,
            LancelotResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 3:
                    return Text(
                        "lancelot.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3Coefficient)),
                            ("match3_after", Percent(
                                after.Match3Coefficient)),
                            ("match4_before", Percent(
                                before.Match4Coefficient)),
                            ("match4_after", Percent(
                                after.Match4Coefficient)),
                            ("match5_before", Percent(
                                before.Match5Coefficient)),
                            ("match5_after", Percent(
                                after.Match5Coefficient))));
                case 4:
                    return Text(
                        "lancelot.awakening.4.description",
                        Arguments(
                            ("active_before", Percent(
                                before.ActiveCriticalDamageIncreaseRate)),
                            ("active_after", Percent(
                                after.ActiveCriticalDamageIncreaseRate))));
                case 5:
                    return Text(
                        "lancelot.awakening.5.description",
                        Arguments(
                            ("critical_before", Percent(
                                before.PassiveCriticalChanceIncreaseRate)),
                            ("critical_after", Percent(
                                after.PassiveCriticalChanceIncreaseRate))));
                case 6:
                    return Text(
                        "lancelot.awakening.6.description",
                        Arguments(
                            ("critical_damage_before", Percent(
                                before.BaseCriticalDamageMultiplier)),
                            ("critical_damage_after", Percent(
                                after.BaseCriticalDamageMultiplier))));
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

        private static LancelotCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    LancelotRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Lancelot's lookup provider requires Lancelot's "
                        + "definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as LancelotCombatConfig
                ?? throw new InvalidOperationException(
                    "Lancelot requires LancelotCombatConfig.");
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

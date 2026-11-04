using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Kayla
{
    public sealed class KaylaStrideLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly KaylaStrideBuildModifier buildModifier =
            new KaylaStrideBuildModifier();

        public string CharacterId => KaylaStrideRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            KaylaStrideResolvedCombatRules rules =
                KaylaStrideCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "kayla.skill.match3.name",
                        "kayla.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match3Coefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "kayla.skill.match4.name",
                        "kayla.skill.match4.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match4Coefficient))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "kayla.skill.match5.name",
                        "kayla.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("critical_damage", Percent(rules
                                .Match5CriticalDamageIncreaseRate)),
                            ("duration",
                                rules.Match5BuffDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "kayla.skill.passive.name",
                        "kayla.skill.passive.description",
                        Arguments(
                            ("charge_coefficient", Percent(rules
                                .ChargeCoefficientPerStack)),
                            ("enhancement_threshold",
                                rules.ChargeEnhancementThreshold),
                            ("enhancement_multiplier", Decimal(rules
                                .ChargeEnhancementMultiplier)),
                            ("maximum_charge",
                                rules.ChargeMaxStacks)));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "kayla.skill.active.name",
                        "kayla.skill.active.description",
                        Arguments(
                            ("critical_chance", Percent(rules
                                .ActiveCriticalChanceIncreaseRate)),
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

            KaylaStrideCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1 || awakeningStage == 2)
            {
                CharacterBuildStatMultipliers before = buildModifier
                    .GetStatMultipliers(awakeningStage - 1);
                CharacterBuildStatMultipliers after = buildModifier
                    .GetStatMultipliers(awakeningStage);
                bool attack = awakeningStage == 1;
                description = Text(
                    $"kayla.awakening.{awakeningStage}.description",
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
                KaylaStrideResolvedCombatRules before =
                    KaylaStrideCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                KaylaStrideResolvedCombatRules after =
                    KaylaStrideCombatRuleResolver.Resolve(
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
                            $"kayla.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            KaylaStrideResolvedCombatRules before,
            KaylaStrideResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 3:
                    return Text(
                        "kayla.awakening.3.description",
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
                        "kayla.awakening.4.description",
                        Arguments(
                            ("match5_before", Percent(
                                before.Match5Coefficient)),
                            ("match5_after", Percent(
                                after.Match5Coefficient)),
                            ("critical_damage_before", Percent(before
                                .Match5CriticalDamageIncreaseRate)),
                            ("critical_damage_after", Percent(after
                                .Match5CriticalDamageIncreaseRate))));
                case 5:
                    return Text(
                        "kayla.awakening.5.description",
                        Arguments(
                            ("cooldown_before",
                                before.ActiveCooldownTurns),
                            ("cooldown_after",
                                after.ActiveCooldownTurns)));
                case 6:
                    return Text(
                        "kayla.awakening.6.description",
                        Arguments(
                            ("charge_before", Percent(
                                before.ChargeCoefficientPerStack)),
                            ("charge_after", Percent(
                                after.ChargeCoefficientPerStack))));
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

        private static KaylaStrideCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                definition.Id,
                KaylaStrideRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Kayla's lookup provider requires Kayla's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as KaylaStrideCombatConfig
                ?? throw new InvalidOperationException(
                    "Kayla requires KaylaStrideCombatConfig.");
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

        private static string Decimal(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}

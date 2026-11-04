using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Anubis
{
    public sealed class AnubisLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";
        private readonly AnubisBuildModifier buildModifier =
            new AnubisBuildModifier();

        public string CharacterId => AnubisRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            AnubisCombatConfig config = RequireConfig(definition);
            AnubisResolvedCombatRules rules =
                AnubisCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill(
                        "match3",
                        rules.Match3DamageCoefficient,
                        rules.Match3RadianceRate);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill(
                        "match4",
                        rules.Match4DamageCoefficient,
                        rules.Match4RadianceRate);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill(
                        "match5",
                        rules.Match5DamageCoefficient,
                        rules.Match5RadianceRate);
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "anubis.skill.passive.name",
                        "anubis.skill.passive.description",
                        Arguments(("damage_increase", Percent(
                            rules.PassiveLightDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "anubis.skill.active.name",
                        "anubis.skill.active.description",
                        Arguments(
                            ("block_count", rules.ActiveBlockConversionCount),
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

            AnubisCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                AnubisResolvedCombatRules before =
                    AnubisCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                AnubisResolvedCombatRules after =
                    AnubisCombatRuleResolver.Resolve(
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
                            $"anubis.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(int stage)
        {
            CharacterBuildStatMultipliers before =
                buildModifier.GetStatMultipliers(stage - 1);
            CharacterBuildStatMultipliers after =
                buildModifier.GetStatMultipliers(stage);
            bool attack = stage == 1;
            return Text(
                $"anubis.awakening.{stage}.description",
                Arguments(
                    ("multiplier_before", Percent(attack
                        ? before.AttackMultiplier
                        : before.MaxHpMultiplier)),
                    ("multiplier_after", Percent(attack
                        ? after.AttackMultiplier
                        : after.MaxHpMultiplier))));
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            AnubisResolvedCombatRules before,
            AnubisResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "anubis.awakening.3.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3DamageCoefficient)),
                            ("match3_after", Percent(after.Match3DamageCoefficient)),
                            ("match4_before", Percent(before.Match4DamageCoefficient)),
                            ("match4_after", Percent(after.Match4DamageCoefficient)),
                            ("match5_before", Percent(before.Match5DamageCoefficient)),
                            ("match5_after", Percent(after.Match5DamageCoefficient))));
                case 4:
                    return Text(
                        "anubis.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(before.Match3RadianceRate)),
                            ("match3_after", Percent(after.Match3RadianceRate)),
                            ("match4_before", Percent(before.Match4RadianceRate)),
                            ("match4_after", Percent(after.Match4RadianceRate)),
                            ("match5_before", Percent(before.Match5RadianceRate)),
                            ("match5_after", Percent(after.Match5RadianceRate))));
                case 5:
                    return Text(
                        "anubis.awakening.5.description",
                        Arguments(
                            ("passive_before", Percent(before.PassiveLightDamageIncreaseRate)),
                            ("passive_after", Percent(after.PassiveLightDamageIncreaseRate))));
                case 6:
                    return Text(
                        "anubis.awakening.6.description",
                        Arguments(
                            ("blocks_before", before.ActiveBlockConversionCount),
                            ("blocks_after", after.ActiveBlockConversionCount)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string tier,
            double coefficient,
            double radianceRate)
        {
            return Skill(
                $"anubis.skill.{tier}.name",
                $"anubis.skill.{tier}.description",
                Arguments(
                    ("coefficient", Percent(coefficient)),
                    ("radiance", Percent(radianceRate))));
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

        private static AnubisCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                definition.Id,
                AnubisRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Anubis lookup provider requires Anubis definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as AnubisCombatConfig
                ?? throw new InvalidOperationException(
                    "Anubis requires AnubisCombatConfig.");
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

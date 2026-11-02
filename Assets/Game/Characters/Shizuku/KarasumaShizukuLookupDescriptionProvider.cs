using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Shizuku
{
    public sealed class KarasumaShizukuLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly KarasumaShizukuBuildModifier buildModifier =
            new KarasumaShizukuBuildModifier();

        public string CharacterId => KarasumaShizukuRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            KarasumaShizukuCombatConfig config = RequireConfig(definition);
            KarasumaShizukuResolvedCombatRules rules =
                KarasumaShizukuCombatRuleResolver.Resolve(
                    config,
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill(
                        "match3",
                        rules.Match3DamageCoefficient,
                        rules.Match3AttackIncreaseRate,
                        rules.AttackBuffDurationTurns);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill(
                        "match4",
                        rules.Match4DamageCoefficient,
                        rules.Match4AttackIncreaseRate,
                        rules.AttackBuffDurationTurns);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill(
                        "match5",
                        rules.Match5DamageCoefficient,
                        rules.Match5AttackIncreaseRate,
                        rules.AttackBuffDurationTurns);
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "shizuku.skill.passive.name",
                        "shizuku.skill.passive.description",
                        Arguments(
                            ("damage_increase", Percent(
                                rules.PassiveDarkDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "shizuku.skill.active.name",
                        "shizuku.skill.active.description",
                        Arguments(
                            ("block_count",
                                rules.ActiveBlockConversionCount),
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

            KarasumaShizukuCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                KarasumaShizukuResolvedCombatRules before =
                    KarasumaShizukuCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                KarasumaShizukuResolvedCombatRules after =
                    KarasumaShizukuCombatRuleResolver.Resolve(
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
                            $"shizuku.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(
            int awakeningStage)
        {
            CharacterBuildStatMultipliers before =
                buildModifier.GetStatMultipliers(awakeningStage - 1);
            CharacterBuildStatMultipliers after =
                buildModifier.GetStatMultipliers(awakeningStage);
            bool attack = awakeningStage == 1;
            return Text(
                $"shizuku.awakening.{awakeningStage}.description",
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
            KarasumaShizukuResolvedCombatRules before,
            KarasumaShizukuResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "shizuku.awakening.3.description",
                        Arguments(
                            ("passive_before", Percent(before
                                .PassiveDarkDamageIncreaseRate)),
                            ("passive_after", Percent(after
                                .PassiveDarkDamageIncreaseRate))));
                case 4:
                    return Text(
                        "shizuku.awakening.4.description",
                        Arguments(
                            ("match3_damage_before", Percent(before
                                .Match3DamageCoefficient)),
                            ("match3_damage_after", Percent(after
                                .Match3DamageCoefficient)),
                            ("match3_buff_before", Percent(before
                                .Match3AttackIncreaseRate)),
                            ("match3_buff_after", Percent(after
                                .Match3AttackIncreaseRate)),
                            ("match4_damage_before", Percent(before
                                .Match4DamageCoefficient)),
                            ("match4_damage_after", Percent(after
                                .Match4DamageCoefficient)),
                            ("match4_buff_before", Percent(before
                                .Match4AttackIncreaseRate)),
                            ("match4_buff_after", Percent(after
                                .Match4AttackIncreaseRate))));
                case 5:
                    return Text(
                        "shizuku.awakening.5.description",
                        Arguments(
                            ("damage_before", Percent(before
                                .Match5DamageCoefficient)),
                            ("damage_after", Percent(after
                                .Match5DamageCoefficient)),
                            ("buff_before", Percent(before
                                .Match5AttackIncreaseRate)),
                            ("buff_after", Percent(after
                                .Match5AttackIncreaseRate))));
                case 6:
                    return Text(
                        "shizuku.awakening.6.description",
                        Arguments(
                            ("blocks_before",
                                before.ActiveBlockConversionCount),
                            ("blocks_after",
                                after.ActiveBlockConversionCount)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string tier,
            double coefficient,
            double attackIncreaseRate,
            int durationTurns)
        {
            return Skill(
                $"shizuku.skill.{tier}.name",
                $"shizuku.skill.{tier}.description",
                Arguments(
                    ("coefficient", Percent(coefficient)),
                    ("attack_increase", Percent(attackIncreaseRate)),
                    ("duration", durationTurns)));
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

        private static KarasumaShizukuCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    KarasumaShizukuRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Shizuku's lookup provider requires Shizuku's "
                        + "definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as KarasumaShizukuCombatConfig
                ?? throw new InvalidOperationException(
                    "Shizuku requires KarasumaShizukuCombatConfig.");
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

using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.IDs;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Data.Validation
{
    public static class DataValidator
    {
        public static ValidationReport Validate(DefinitionDatabase database)
        {
            var report = new ValidationReport();

            if (database == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] DefinitionDatabase is missing.");
                return report;
            }

            var globalIds = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, SkillDefinition> skillsById =
                CollectSkillsById(database.Skills);

            ValidateCharacters(
                database.Characters,
                skillsById,
                globalIds,
                report);
            ValidateBosses(database.Bosses, skillsById, globalIds, report);
            ValidateSkills(database.Skills, globalIds, report);
            ValidateEffects(database.Effects, globalIds, report);
            ValidateResources(database.Resources, globalIds, report);
            ValidateRelics(database.Relics, globalIds, report);
            ValidateGachas(
                database.Gachas,
                database.Characters,
                database.Relics,
                globalIds,
                report);
            ValidateExchangeCatalog(
                database.ExchangeCatalog,
                database.Characters,
                database.Relics,
                report);
            ValidateRelicPresentations(
                database.RelicPresentations,
                database.Relics,
                report);
            ValidateBossPresentations(
                database.BossPresentations,
                database.Bosses,
                report);
            ValidateBattleResultBalance(
                database.BattleResultBalance,
                report);

            return report;
        }

        private static void ValidateExchangeCatalog(
            ExchangeCatalogDefinition catalog,
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyList<RelicDefinition> relics,
            ValidationReport report)
        {
            if (catalog == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] ExchangeCatalogDefinition is missing.");
                return;
            }

            if (catalog.CharacterExchangeCost <= 0L)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] Character exchange cost must be "
                        + "greater than zero.",
                    context: catalog);
            }

            if (catalog.RelicExchangeCost <= 0L)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] Relic exchange cost must be greater "
                        + "than zero.",
                    context: catalog);
            }

            ValidateExchangeIds(
                catalog,
                "character",
                catalog.CharacterIds,
                CollectDefinitionIds(characters),
                report);
            ValidateExchangeIds(
                catalog,
                "relic",
                catalog.RelicIds,
                CollectDefinitionIds(relics),
                report);
        }

        private static void ValidateExchangeIds(
            ExchangeCatalogDefinition catalog,
            string contentType,
            IReadOnlyList<string> ids,
            ISet<string> registeredIds,
            ValidationReport report)
        {
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < ids.Count; index++)
            {
                string id = ids[index];
                if (!uniqueIds.Add(id ?? string.Empty))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Duplicate Exchange "
                            + $"{contentType} ID: "
                            + $"{(string.IsNullOrWhiteSpace(id) ? "<empty>" : id)}.",
                        id,
                        catalog);
                }

                if (string.IsNullOrWhiteSpace(id)
                    || !registeredIds.Contains(id))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Exchange {contentType} list "
                            + "references missing ID: "
                            + $"{(string.IsNullOrWhiteSpace(id) ? "<empty>" : id)}.",
                        id,
                        catalog);
                }
            }
        }

        private static void ValidateBattleResultBalance(
            BattleResultBalanceDefinition definition,
            ValidationReport report)
        {
            if (definition == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] BattleResultBalanceDefinition is "
                        + "missing.");
                return;
            }

            if (!BattleResultBalanceValidator.TryValidate(
                definition.CreateBalance(),
                out string errorMessage))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] BattleResultBalanceDefinition is "
                        + $"invalid. {errorMessage}",
                    context: definition);
            }
        }

        public static void LogReport(ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            for (int i = 0; i < report.Issues.Count; i++)
            {
                ValidationIssue issue = report.Issues[i];

                if (issue.Severity == ValidationSeverity.Warning)
                {
                    GameLogger.Warning(issue.Message, issue.Context);
                }
                else
                {
                    GameLogger.Error(issue.Message, issue.Context);
                }
            }
        }

        private static Dictionary<string, SkillDefinition> CollectSkillsById(
            IReadOnlyList<SkillDefinition> skills)
        {
            var definitions = new Dictionary<string, SkillDefinition>(
                StringComparer.Ordinal);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];

                if (skill != null && !string.IsNullOrEmpty(skill.Id))
                {
                    if (!definitions.ContainsKey(skill.Id))
                    {
                        definitions.Add(skill.Id, skill);
                    }
                }
            }

            return definitions;
        }

        private static void ValidateCharacters(
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition definition = characters[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(CharacterDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(CharacterDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);

                if (definition.CombatConfig == null)
                {
                    AddWarning(
                        report,
                        definition,
                        "Combat config is missing");
                }
                else if (!definition.CombatConfig.TryValidate(
                    out string configError))
                {
                    AddError(
                        report,
                        definition,
                        $"Combat config is invalid. {configError}");
                }

                if (definition.Level1Hp <= 0)
                {
                    AddError(report, definition, "Lv.1 HP must be greater than zero");
                }

                if (definition.Level1Attack <= 0)
                {
                    AddError(report, definition, "Lv.1 ATK must be greater than zero");
                }

                if (definition.Level100Hp <= 0)
                {
                    AddError(report, definition, "Lv.100 HP must be greater than zero");
                }

                if (definition.Level100Attack <= 0)
                {
                    AddError(report, definition, "Lv.100 ATK must be greater than zero");
                }

                if (definition.Level100Hp < definition.Level1Hp)
                {
                    AddError(report, definition, "Lv.100 HP cannot be lower than Lv.1 HP");
                }

                if (definition.Level100Attack < definition.Level1Attack)
                {
                    AddError(report, definition, "Lv.100 ATK cannot be lower than Lv.1 ATK");
                }

                ValidateSkillReferences(
                    definition.SkillIds,
                    skillsById,
                    "skill",
                    SkillReferenceOwner.Character,
                    definition,
                    report);

                if (definition.SkillIds.Count == 0)
                {
                    AddWarning(report, definition, "Skill ID list is empty");
                }
            }
        }

        private static void ValidateBosses(
            IReadOnlyList<BossDefinition> bosses,
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < bosses.Count; i++)
            {
                BossDefinition definition = bosses[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(BossDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(BossDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);

                if (definition.CombatConfig == null)
                {
                    AddWarning(
                        report,
                        definition,
                        "Combat config is missing");
                }
                else if (!definition.CombatConfig.TryValidate(
                    out string configError))
                {
                    AddError(
                        report,
                        definition,
                        $"Combat config is invalid. {configError}");
                }

                if (definition.TurnLimit <= 0)
                {
                    AddError(report, definition, "Turn limit must be greater than zero");
                }

                ValidateBossDifficulties(definition, report);

                ValidateSkillReferences(
                    definition.ActionOrSkillIds,
                    skillsById,
                    "action or skill",
                    SkillReferenceOwner.Boss,
                    definition,
                    report);

                if (definition.ActionOrSkillIds.Count == 0)
                {
                    AddWarning(report, definition, "Action or skill ID list is empty");
                }
            }
        }

        private static void ValidateBossDifficulties(
            BossDefinition definition,
            ValidationReport report)
        {
            var difficultyIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<BossDifficultyStats> stats =
                definition.DifficultyStats;

            for (int index = 0; index < stats.Count; index++)
            {
                BossDifficultyStats entry = stats[index];
                if (entry == null)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty stat at index {index} is null");
                    continue;
                }

                if (!ContentIdValidator.TryValidate(
                    entry.DifficultyId,
                    out string idError))
                {
                    string displayedId = string.IsNullOrEmpty(
                        entry.DifficultyId)
                        ? "<empty>"
                        : entry.DifficultyId;
                    AddError(
                        report,
                        definition,
                        $"Invalid difficulty ID {displayedId}. {idError}");
                }

                if (!string.IsNullOrEmpty(entry.DifficultyId)
                    && !difficultyIds.Add(entry.DifficultyId))
                {
                    AddError(
                        report,
                        definition,
                        $"Duplicate difficulty ID {entry.DifficultyId}");
                }

                if (entry.MaxHp <= 0)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty Max HP must be greater than zero "
                            + $"({entry.DifficultyId})");
                }

                if (double.IsNaN(entry.Attack)
                    || double.IsInfinity(entry.Attack)
                    || entry.Attack < 0d)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty ATK must be finite and non-negative "
                            + $"({entry.DifficultyId})");
                }
            }
        }

        private static void ValidateSkills(
            IReadOnlyList<SkillDefinition> skills,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition definition = skills[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(SkillDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(SkillDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                if (definition.SkillKind == SkillKind.BossAction)
                {
                    WarnIfIconMissing(definition.Icon, definition, report);
                }

                if (!Enum.IsDefined(
                        typeof(SkillKind),
                        definition.SkillKind))
                {
                    AddError(report, definition, "Skill kind is invalid");
                }
            }
        }

        private static void ValidateEffects(
            IReadOnlyList<EffectDefinition> effects,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < effects.Count; index++)
            {
                EffectDefinition definition = effects[index];
                if (definition == null)
                {
                    AddNullDefinition(
                        report,
                        nameof(EffectDefinition),
                        index);
                    continue;
                }

                ValidateCommon(
                    definition,
                    nameof(EffectDefinition),
                    typeIds,
                    globalIds,
                    report);
                WarnIfDisplayNameKeyMissing(
                    definition.DisplayNameKey,
                    definition,
                    report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                WarnIfIconMissing(definition.Icon, definition, report);
            }
        }

        private static void ValidateResources(
            IReadOnlyList<ResourceDefinition> resources,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < resources.Count; index++)
            {
                ResourceDefinition definition = resources[index];
                if (definition == null)
                {
                    AddNullDefinition(
                        report,
                        nameof(ResourceDefinition),
                        index);
                    continue;
                }

                ValidateCommon(
                    definition,
                    nameof(ResourceDefinition),
                    typeIds,
                    globalIds,
                    report);
                WarnIfDisplayNameKeyMissing(
                    definition.DisplayNameKey,
                    definition,
                    report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                WarnIfIconMissing(definition.Icon, definition, report);
            }
        }

        private static void ValidateRelics(
            IReadOnlyList<RelicDefinition> relics,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < relics.Count; i++)
            {
                RelicDefinition definition = relics[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(RelicDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(RelicDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);

                IReadOnlyList<RelicEffectEntry> effects =
                    definition.Effects;
                if (effects.Count == 0)
                {
                    AddError(
                        report,
                        definition,
                        "At least one relic effect is required");
                    continue;
                }

                var descriptionKeys = new HashSet<string>(
                    StringComparer.Ordinal);
                var effectScopes = new HashSet<string>(
                    StringComparer.Ordinal);
                bool hasAttackIncrease = false;
                bool hasAttackReduction = false;
                bool hasMaxHpIncrease = false;
                bool hasMaxHpReduction = false;
                for (int effectIndex = 0;
                    effectIndex < effects.Count;
                    effectIndex++)
                {
                    RelicEffectEntry effect = effects[effectIndex];
                    if (effect == null)
                    {
                        AddError(
                            report,
                            definition,
                            $"Effect entry {effectIndex} is missing");
                        continue;
                    }

                    if (!Enum.IsDefined(
                            typeof(RelicEffectType),
                            effect.EffectType)
                        || effect.EffectType == RelicEffectType.None)
                    {
                        AddError(
                            report,
                            definition,
                            $"Effect entry {effectIndex} type is invalid: "
                                + effect.EffectType);
                    }

                    ValidateRelicEffectRates(
                        definition,
                        effect,
                        effectIndex,
                        report);
                    ValidateRelicEffectFilters(
                        definition,
                        effect,
                        effectIndex,
                        report);

                    if (string.IsNullOrWhiteSpace(
                            effect.DescriptionValueKey)
                        || !descriptionKeys.Add(
                            effect.DescriptionValueKey))
                    {
                        AddError(
                            report,
                            definition,
                            $"Effect entry {effectIndex} description "
                                + "value key is missing or duplicated");
                    }

                    string scope = string.Join(
                        "|",
                        (int)effect.EffectType,
                        effect.HasElementFilter,
                        (int)effect.ElementFilter,
                        (int)effect.AttackTypeMask,
                        (int)effect.RequiredAttackTags,
                        (int)effect.ExcludedAttackTags,
                        (int)effect.ConditionType,
                        effect.ConditionThreshold);
                    if (!effectScopes.Add(scope))
                    {
                        AddError(
                            report,
                            definition,
                            $"Effect entry {effectIndex} duplicates an "
                                + "existing effect scope");
                    }

                    hasAttackIncrease |= effect.EffectType
                        == RelicEffectType.AttackIncrease;
                    hasAttackReduction |= effect.EffectType
                        == RelicEffectType.AttackReduction;
                    hasMaxHpIncrease |= effect.EffectType
                        == RelicEffectType.MaxHpIncrease;
                    hasMaxHpReduction |= effect.EffectType
                        == RelicEffectType.MaxHpReduction;
                }

                if (hasAttackIncrease && hasAttackReduction)
                {
                    AddError(
                        report,
                        definition,
                        "Attack increase and reduction effects conflict");
                }

                if (hasMaxHpIncrease && hasMaxHpReduction)
                {
                    AddError(
                        report,
                        definition,
                        "Maximum HP increase and reduction effects conflict");
                }
            }
        }

        private static void ValidateRelicEffectRates(
            RelicDefinition definition,
            RelicEffectEntry effect,
            int effectIndex,
            ValidationReport report)
        {
            IReadOnlyList<double> rates = effect.EffectRates;
            if (rates.Count != RelicDefinition.AwakeningValueCount)
            {
                AddError(
                    report,
                    definition,
                    $"Effect entry {effectIndex} awakening rate count "
                        + $"must be {RelicDefinition.AwakeningValueCount}");
                return;
            }

            for (int awakening = 0; awakening < rates.Count; awakening++)
            {
                double rate = rates[awakening];
                bool boundedByOne = effect.EffectType
                        == RelicEffectType.CriticalChanceIncrease
                    || effect.EffectType
                        == RelicEffectType.PartyDamageReduction;
                bool reductionMustLeavePositive = effect.EffectType
                        == RelicEffectType.AttackReduction
                    || effect.EffectType
                        == RelicEffectType.MaxHpReduction;
                if (double.IsNaN(rate)
                    || double.IsInfinity(rate)
                    || rate < 0d
                    || (boundedByOne && rate > 1d)
                    || (reductionMustLeavePositive && rate >= 1d))
                {
                    AddError(
                        report,
                        definition,
                        $"Effect entry {effectIndex} awakening "
                            + $"{awakening} rate is outside its valid range");
                }
            }
        }

        private static void ValidateRelicEffectFilters(
            RelicDefinition definition,
            RelicEffectEntry effect,
            int effectIndex,
            ValidationReport report)
        {
            const AttackTag allTags = AttackTag.Match3
                | AttackTag.Match4
                | AttackTag.Match5Plus
                | AttackTag.Heavy
                | AttackTag.MultiHit
                | AttackTag.Elemental
                | AttackTag.FixedDamage
                | AttackTag.MaxHpRatio;
            bool isElementEffect = effect.EffectType
                == RelicEffectType.ElementDamageIncrease;
            bool supportsAttackFilter = effect.EffectType
                    == RelicEffectType.AttackTypeDamageIncrease
                || effect.EffectType
                    == RelicEffectType.DealtDamageIncrease;
            bool hasAttackFilter = effect.AttackTypeMask
                    != AttackTypeMask.None
                || effect.RequiredAttackTags != AttackTag.None
                || effect.ExcludedAttackTags != AttackTag.None;

            if (effect.HasElementFilter != isElementEffect
                || (effect.HasElementFilter
                    && !Enum.IsDefined(
                        typeof(ElementType),
                        effect.ElementFilter)))
            {
                AddError(
                    report,
                    definition,
                    $"Effect entry {effectIndex} has an invalid element "
                        + "filter");
            }

            if ((effect.AttackTypeMask & ~AttackTypeMask.All) != 0
                || (effect.RequiredAttackTags & ~allTags) != 0
                || (effect.ExcludedAttackTags & ~allTags) != 0
                || (effect.RequiredAttackTags
                    & effect.ExcludedAttackTags) != 0
                || (hasAttackFilter && !supportsAttackFilter)
                || (effect.EffectType
                        == RelicEffectType.AttackTypeDamageIncrease
                    && effect.AttackTypeMask == AttackTypeMask.None))
            {
                AddError(
                    report,
                    definition,
                    $"Effect entry {effectIndex} has an invalid attack "
                        + "type or tag filter");
            }

            if (!Enum.IsDefined(
                    typeof(RelicEffectConditionType),
                    effect.ConditionType)
                || (effect.ConditionType
                        == RelicEffectConditionType.None
                    && effect.ConditionThreshold != 0d)
                || (effect.ConditionType
                        == RelicEffectConditionType.PartyHpAtOrBelow
                    && (effect.EffectType
                            != RelicEffectType.DealtDamageIncrease
                        || effect.ConditionThreshold <= 0d
                        || effect.ConditionThreshold > 1d
                        || double.IsNaN(effect.ConditionThreshold)
                        || double.IsInfinity(effect.ConditionThreshold))))
            {
                AddError(
                    report,
                    definition,
                    $"Effect entry {effectIndex} has an invalid condition");
            }
        }

        private static void ValidateGachas(
            IReadOnlyList<GachaDefinition> gachas,
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyList<RelicDefinition> relics,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> characterIds = CollectDefinitionIds(characters);
            HashSet<string> relicIds = CollectDefinitionIds(relics);

            for (int index = 0; index < gachas.Count; index++)
            {
                GachaDefinition definition = gachas[index];
                if (definition == null)
                {
                    AddNullDefinition(
                        report,
                        nameof(GachaDefinition),
                        index);
                    continue;
                }

                ValidateCommon(
                    definition,
                    nameof(GachaDefinition),
                    typeIds,
                    globalIds,
                    report);
                ValidateGachaRules(definition, report);

                bool hasCharacterReward = false;
                bool hasRelicReward = false;
                long totalWeight = 0L;
                var rewardKeys = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<GachaRewardEntry> rewards =
                    definition.RewardEntries;
                if (rewards.Count == 0)
                {
                    AddError(report, definition, "Reward list is empty");
                }

                for (int rewardIndex = 0;
                    rewardIndex < rewards.Count;
                    rewardIndex++)
                {
                    GachaRewardEntry reward = rewards[rewardIndex];
                    if (reward == null)
                    {
                        AddError(
                            report,
                            definition,
                            $"Reward entry at index {rewardIndex} is null");
                        continue;
                    }

                    if (!Enum.IsDefined(
                            typeof(GachaRewardKind),
                            reward.RewardKind))
                    {
                        AddError(
                            report,
                            definition,
                            $"Reward kind is invalid at index "
                                + $"{rewardIndex}: {reward.RewardKind}");
                    }
                    else if (reward.RewardKind
                        == GachaRewardKind.RandomCharacter)
                    {
                        hasCharacterReward = true;
                    }
                    else if (reward.RewardKind
                        == GachaRewardKind.RandomRelic)
                    {
                        hasRelicReward = true;
                    }

                    if (reward.Quantity <= 0L)
                    {
                        AddError(
                            report,
                            definition,
                            $"Reward quantity must be greater than zero at "
                                + $"index {rewardIndex}");
                    }

                    if (reward.Weight <= 0)
                    {
                        AddError(
                            report,
                            definition,
                            $"Reward weight must be greater than zero at "
                                + $"index {rewardIndex}");
                    }

                    totalWeight += reward.Weight;
                    string rewardKey =
                        $"{(int)reward.RewardKind}\u001f{reward.Quantity}";
                    if (!rewardKeys.Add(rewardKey))
                    {
                        AddError(
                            report,
                            definition,
                            $"Duplicate reward kind and quantity: "
                                + $"{reward.RewardKind}/{reward.Quantity}");
                    }
                }

                if (totalWeight != GachaDefinition.RequiredTotalWeight)
                {
                    AddError(
                        report,
                        definition,
                        $"Reward weight total must be "
                            + $"{GachaDefinition.RequiredTotalWeight}, but "
                            + $"was {totalWeight}");
                }

                ValidateGachaPool(
                    definition,
                    "character",
                    definition.CharacterPoolIds,
                    characterIds,
                    report);
                ValidateGachaPool(
                    definition,
                    "relic",
                    definition.RelicPoolIds,
                    relicIds,
                    report);

                if (hasCharacterReward
                    && definition.CharacterPoolIds.Count == 0)
                {
                    AddError(
                        report,
                        definition,
                        "Character reward requires a non-empty character pool");
                }

                if (hasRelicReward && definition.RelicPoolIds.Count == 0)
                {
                    AddError(
                        report,
                        definition,
                        "Relic reward requires a non-empty relic pool");
                }

                if (definition.GachaKind == GachaKind.Character
                    && definition.CharacterPoolIds.Count == 0)
                {
                    AddError(
                        report,
                        definition,
                        "Character pity requires a non-empty character pool");
                }
                else if (definition.GachaKind == GachaKind.Relic
                    && definition.RelicPoolIds.Count == 0)
                {
                    AddError(
                        report,
                        definition,
                        "Relic pity requires a non-empty relic pool");
                }
            }
        }

        private static void ValidateGachaRules(
            GachaDefinition definition,
            ValidationReport report)
        {
            if (!Enum.IsDefined(typeof(GachaKind), definition.GachaKind))
            {
                AddError(
                    report,
                    definition,
                    $"Gacha kind is invalid: {definition.GachaKind}");
            }

            if (definition.SinglePullCost <= 0L)
            {
                AddError(
                    report,
                    definition,
                    "Single-pull cost must be greater than zero");
            }

            if (definition.TenPullCost <= 0L)
            {
                AddError(
                    report,
                    definition,
                    "Ten-pull cost must be greater than zero");
            }
            else if (definition.SinglePullCost > 0L
                && (definition.SinglePullCost > long.MaxValue / 10L
                    || definition.TenPullCost
                        != definition.SinglePullCost * 10L))
            {
                AddError(
                    report,
                    definition,
                    "Ten-pull cost must equal ten single pulls");
            }

            if (definition.PityPullCount <= 0)
            {
                AddError(
                    report,
                    definition,
                    "Pity pull count must be greater than zero");
            }
        }

        private static void ValidateGachaPool(
            GachaDefinition definition,
            string poolName,
            IReadOnlyList<string> poolIds,
            ISet<string> registeredIds,
            ValidationReport report)
        {
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < poolIds.Count; index++)
            {
                string id = poolIds[index];
                if (string.IsNullOrWhiteSpace(id)
                    || !registeredIds.Contains(id))
                {
                    AddError(
                        report,
                        definition,
                        $"Gacha {poolName} pool references missing ID: "
                            + $"{(string.IsNullOrWhiteSpace(id) ? "<empty>" : id)}");
                    continue;
                }

                if (!uniqueIds.Add(id))
                {
                    AddError(
                        report,
                        definition,
                        $"Duplicate Gacha {poolName} pool ID: {id}");
                }
            }
        }

        private static HashSet<string> CollectDefinitionIds<TDefinition>(
            IReadOnlyList<TDefinition> definitions)
            where TDefinition : GameDefinition
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < definitions.Count; index++)
            {
                string id = definitions[index]?.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static void ValidateRelicPresentations(
            IReadOnlyList<RelicPresentationDefinition> presentations,
            IReadOnlyList<RelicDefinition> relics,
            ValidationReport report)
        {
            var relicIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < relics.Count; index++)
            {
                RelicDefinition relic = relics[index];
                if (relic != null && !string.IsNullOrEmpty(relic.Id))
                {
                    relicIds.Add(relic.Id);
                }
            }

            var presentedRelicIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < presentations.Count; index++)
            {
                RelicPresentationDefinition presentation =
                    presentations[index];
                if (presentation == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Null "
                            + $"{nameof(RelicPresentationDefinition)} at "
                            + $"index {index}.");
                    continue;
                }

                string relicId = presentation.RelicId;
                if (!ContentIdValidator.TryValidate(
                        relicId,
                        out string relicIdError))
                {
                    string displayedId = string.IsNullOrEmpty(relicId)
                        ? "<empty>"
                        : relicId;
                    report.Add(
                        ValidationSeverity.Error,
                        "[DataValidator] Invalid RelicPresentation relic "
                            + $"ID {displayedId}. {relicIdError}",
                        relicId,
                        presentation);
                }
                else
                {
                    if (!relicIds.Contains(relicId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "[DataValidator] Missing relic reference: "
                                + relicId,
                            relicId,
                            presentation);
                    }

                    if (!presentedRelicIds.Add(relicId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "[DataValidator] Duplicate relic presentation "
                                + $"for Relic ID: {relicId}",
                            relicId,
                            presentation);
                    }
                }

                if (presentation.Icon == null)
                {
                    report.Add(
                        ValidationSeverity.Warning,
                        "[DataValidator] Relic presentation icon is "
                            + $"missing: {relicId}",
                        relicId,
                        presentation);
                }
            }

            foreach (string relicId in relicIds)
            {
                if (!presentedRelicIds.Contains(relicId))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "[DataValidator] Relic presentation is missing: "
                            + relicId,
                        relicId);
                }
            }
        }

        private static void ValidateCommon(
            GameDefinition definition,
            string definitionTypeName,
            ISet<string> typeIds,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            string id = definition.Id;

            if (!ContentIdValidator.TryValidate(id, out string errorMessage))
            {
                string message = string.IsNullOrEmpty(id)
                    ? $"[DataValidator] Missing content ID: {definitionTypeName}"
                    : $"[DataValidator] Invalid content ID format: {id}. {errorMessage}";

                report.Add(ValidationSeverity.Error, message, id, definition);
            }

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!typeIds.Add(id))
            {
                report.Add(
                    ValidationSeverity.Error,
                    $"[DataValidator] Duplicate {definitionTypeName} ID: {id}",
                    id,
                    definition);
            }

            if (globalIds.TryGetValue(id, out string existingType))
            {
                report.Add(
                    ValidationSeverity.Error,
                    $"[DataValidator] Duplicate content ID: {id} " +
                    $"({existingType} and {definitionTypeName})",
                    id,
                    definition);
            }
            else
            {
                globalIds.Add(id, definitionTypeName);
            }
        }

        private static void ValidateSkillReferences(
            IReadOnlyList<string> referenceIds,
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
            string referenceType,
            SkillReferenceOwner ownerType,
            GameDefinition owner,
            ValidationReport report)
        {
            var referencedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < referenceIds.Count; i++)
            {
                string referenceId = referenceIds[i];

                if (!string.IsNullOrEmpty(referenceId)
                    && !referencedIds.Add(referenceId))
                {
                    AddError(
                        report,
                        owner,
                        $"Duplicate {referenceType} reference: "
                            + referenceId);
                    continue;
                }

                if (string.IsNullOrEmpty(referenceId)
                    || !skillsById.TryGetValue(
                        referenceId,
                        out SkillDefinition skill))
                {
                    string displayedId = string.IsNullOrEmpty(referenceId)
                        ? "<empty>"
                        : referenceId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Missing {referenceType} "
                            + $"reference: {displayedId}",
                        owner.Id,
                        owner);
                    continue;
                }

                if (ownerType == SkillReferenceOwner.Character
                    && skill.SkillKind == SkillKind.BossAction)
                {
                    AddError(
                        report,
                        owner,
                        $"Character cannot reference BossAction skill: "
                            + referenceId);
                }
                else if (ownerType == SkillReferenceOwner.Boss
                    && skill.SkillKind != SkillKind.BossAction)
                {
                    AddError(
                        report,
                        owner,
                        $"Boss action list must reference BossAction skill: "
                            + referenceId);
                }
            }
        }

        private static void ValidateBossPresentations(
            IReadOnlyList<BossPresentationDefinition> presentations,
            IReadOnlyList<BossDefinition> bosses,
            ValidationReport report)
        {
            var bossIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < bosses.Count; index++)
            {
                BossDefinition boss = bosses[index];
                if (boss != null && !string.IsNullOrEmpty(boss.Id))
                {
                    bossIds.Add(boss.Id);
                }
            }

            var presentedBossIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < presentations.Count; index++)
            {
                BossPresentationDefinition presentation =
                    presentations[index];
                if (presentation == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Null "
                            + $"{nameof(BossPresentationDefinition)} at "
                            + $"index {index}.");
                    continue;
                }

                string bossId = presentation.BossId;
                if (!ContentIdValidator.TryValidate(
                        bossId,
                        out string bossIdError))
                {
                    string displayedId = string.IsNullOrEmpty(bossId)
                        ? "<empty>"
                        : bossId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Invalid BossPresentation Boss ID "
                            + $"{displayedId}. {bossIdError}",
                        bossId,
                        presentation);
                }
                else
                {
                    if (!bossIds.Contains(bossId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            $"[DataValidator] Missing boss reference: "
                                + bossId,
                            bossId,
                            presentation);
                    }

                    if (!presentedBossIds.Add(bossId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            $"[DataValidator] Duplicate boss presentation "
                                + $"for Boss ID: {bossId}",
                            bossId,
                            presentation);
                    }
                }

                if (presentation.DefaultSprite == null
                    && !presentation.AllowMissingSprites)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Boss presentation default sprite "
                            + $"is missing: {bossId}",
                        bossId,
                        presentation);
                }

                ValidateBossVisualStates(presentation, report);
            }
        }

        private static void ValidateBossVisualStates(
            BossPresentationDefinition presentation,
            ValidationReport report)
        {
            var visualStateIds = new HashSet<string>(
                StringComparer.Ordinal);
            IReadOnlyList<BossVisualStateEntry> visuals =
                presentation.StateVisuals;
            for (int index = 0; index < visuals.Count; index++)
            {
                BossVisualStateEntry entry = visuals[index];
                if (entry == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Null boss visual state at index "
                            + $"{index}: {presentation.BossId}",
                        presentation.BossId,
                        presentation);
                    continue;
                }

                if (!ContentIdValidator.TryValidate(
                        entry.VisualStateId,
                        out string stateIdError))
                {
                    string displayedId = string.IsNullOrEmpty(
                        entry.VisualStateId)
                        ? "<empty>"
                        : entry.VisualStateId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Invalid boss visual state ID "
                            + $"{displayedId}. {stateIdError}",
                        presentation.BossId,
                        presentation);
                }
                else if (!visualStateIds.Add(entry.VisualStateId))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Duplicate boss visual state ID: "
                            + entry.VisualStateId,
                        presentation.BossId,
                        presentation);
                }

                if (entry.Sprite == null
                    && !presentation.AllowMissingSprites)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Boss visual state sprite is "
                            + $"missing: {entry.VisualStateId}",
                        presentation.BossId,
                        presentation);
                }
            }
        }

        private static void WarnIfDisplayNameKeyMissing(
            string displayNameKey,
            GameDefinition definition,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(displayNameKey))
            {
                AddWarning(report, definition, "Display name localization key is empty");
            }
        }

        private static void WarnIfDescriptionKeyMissing(
            string descriptionKey,
            GameDefinition definition,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(descriptionKey))
            {
                AddWarning(
                    report,
                    definition,
                    "Description localization key is empty");
            }
        }

        private static void WarnIfIconMissing(
            UnityEngine.Sprite icon,
            GameDefinition definition,
            ValidationReport report)
        {
            if (icon == null)
            {
                AddWarning(report, definition, "Icon is missing");
            }
        }

        private static void AddNullDefinition(
            ValidationReport report,
            string definitionTypeName,
            int index)
        {
            report.Add(
                ValidationSeverity.Error,
                $"[DataValidator] Null {definitionTypeName} at index {index}.");
        }

        private static void AddError(
            ValidationReport report,
            GameDefinition definition,
            string message)
        {
            report.Add(
                ValidationSeverity.Error,
                $"[DataValidator] {message}: {definition.Id}",
                definition.Id,
                definition);
        }

        private static void AddWarning(
            ValidationReport report,
            GameDefinition definition,
            string message)
        {
            report.Add(
                ValidationSeverity.Warning,
                $"[DataValidator] {message}: {definition.Id}",
                definition.Id,
                definition);
        }

        private enum SkillReferenceOwner
        {
            Character,
            Boss
        }
    }
}

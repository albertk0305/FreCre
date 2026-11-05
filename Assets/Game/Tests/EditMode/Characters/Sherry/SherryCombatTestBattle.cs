using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    internal sealed class SherryCombatTestBattle : IDisposable
    {
        public SherryCombatTestBattle(
            int awakening = 0,
            double attack = 1000d,
            long bossHp = 1000000L)
        {
            Config = SherryTestConfig.Create();
            Character = new CharacterBattleState(
                SherryRules.CharacterId,
                0,
                ElementType.Fire,
                1000L,
                attack);
            Party = new PartyBattleState(
                new CharacterBattleState[] { Character });
            Boss = new BossBattleState(
                "training_boss",
                ElementType.Fire,
                bossHp,
                100d);
            State = new SherryBattleState(
                Character,
                SherryCombatConfig.RequiredStylishMaxRank);
            ActionIds = new CombatActionIdSequence();
            Provider = new SherryMatchActionProvider(
                State,
                Config,
                awakening);
            CompletionRule = new SherryMatchCompletionRule(
                State,
                Config,
                awakening,
                ActionIds);
        }

        public SherryCombatConfig Config { get; }
        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public SherryBattleState State { get; }
        public CombatActionIdSequence ActionIds { get; }
        public SherryMatchActionProvider Provider { get; }
        public SherryMatchCompletionRule CompletionRule { get; }

        public IReadOnlyList<CombatAction> Match(
            BoardMatchTier tier,
            int finalComboCount = 1)
        {
            return Provider.CreateActions(
                Character,
                Party,
                Boss,
                tier,
                finalComboCount,
                ActionIds);
        }

        public CombatActionExecutionResult Execute(
            IReadOnlyList<CombatAction> actions,
            params ICombatTriggerRule[] rulesAfterSherry)
        {
            var rules = new ICombatTriggerRule[
                1 + rulesAfterSherry.Length];
            rules[0] = CompletionRule;
            Array.Copy(
                rulesAfterSherry,
                0,
                rules,
                1,
                rulesAfterSherry.Length);
            var executor = new CombatActionExecutor(
                Boss,
                Party,
                new DamageContextFactory(new SeededRandomSource(123)),
                new CombatTriggerResolver(rules));
            return executor.Execute(new CombatActionQueue(actions));
        }

        public void RaiseRankTo(
            int rank,
            SherryMatchTier nextMatchTier)
        {
            if (rank < 0 || rank > State.StylishMaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            SherryMatchTier first;
            SherryMatchTier second;
            switch (nextMatchTier)
            {
                case SherryMatchTier.Three:
                    first = SherryMatchTier.Four;
                    second = SherryMatchTier.FiveOrMore;
                    break;
                case SherryMatchTier.Four:
                    first = SherryMatchTier.Three;
                    second = SherryMatchTier.FiveOrMore;
                    break;
                case SherryMatchTier.FiveOrMore:
                    first = SherryMatchTier.Three;
                    second = SherryMatchTier.Four;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(nextMatchTier));
            }

            for (int index = 0; index < rank; index++)
            {
                State.CompleteMatch(index % 2 == 0 ? first : second);
            }
        }

        public EffectInstance OniIntimidation()
        {
            IReadOnlyList<EffectInstance> effects =
                Boss.Effects.FindByEffectId(
                    SherryRules.OniIntimidationEffectId);
            return effects.Count == 1 ? effects[0] : null;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Config);
        }
    }
}

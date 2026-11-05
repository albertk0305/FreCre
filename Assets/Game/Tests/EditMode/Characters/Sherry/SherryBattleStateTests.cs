using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Sherry;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Sherry
{
    public sealed class SherryBattleStateTests
    {
        [Test]
        public void NewBattleStartsAtZeroWithNoPreviousMatch()
        {
            SherryBattleState state = CreateState();

            Assert.That(state.StylishRank, Is.Zero);
            Assert.That(state.StylishMaxRank, Is.EqualTo(7));
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.None));
            ICharacterCombatStatusSource source = state;
            Assert.That(source.StatusId,
                Is.EqualTo(SherryRules.StylishStatusId));
            Assert.That(source.CharacterId,
                Is.EqualTo(SherryRules.CharacterId));
            Assert.That(source.PartySlotIndex, Is.Zero);
            Assert.That(source.CurrentCount, Is.Zero);
            Assert.That(source.IsVisible, Is.True);
        }

        [Test]
        public void FirstCompletedMatchGrantsOneRank()
        {
            SherryBattleState state = CreateState();

            Assert.That(state.CompleteMatch(SherryMatchTier.Three),
                Is.True);
            Assert.That(state.StylishRank, Is.EqualTo(1));
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.Three));
        }

        [Test]
        public void RepeatingSameMatchUpdatesNothingExceptExistingRecord()
        {
            SherryBattleState state = CreateState();
            state.CompleteMatch(SherryMatchTier.Four);

            Assert.That(state.CompleteMatch(SherryMatchTier.Four),
                Is.False);
            Assert.That(state.StylishRank, Is.EqualTo(1));
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.Four));
        }

        [Test]
        public void DifferentMatchGrantsRankAndUpdatesPreviousTier()
        {
            SherryBattleState state = CreateState();
            state.CompleteMatch(SherryMatchTier.Three);

            Assert.That(state.CompleteMatch(SherryMatchTier.Four),
                Is.True);
            Assert.That(state.StylishRank, Is.EqualTo(2));
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.Four));
        }

        [Test]
        public void RankCapsAtMaximumButPreviousTierStillUpdates()
        {
            SherryBattleState state = CreateState();
            SherryMatchTier tier = SherryMatchTier.Three;
            for (int index = 0; index < 7; index++)
            {
                state.CompleteMatch(tier);
                tier = tier == SherryMatchTier.Three
                    ? SherryMatchTier.Four
                    : SherryMatchTier.Three;
            }

            Assert.That(state.StylishRank, Is.EqualTo(7));
            Assert.That(state.CompleteMatch(SherryMatchTier.FiveOrMore),
                Is.False);
            Assert.That(state.StylishRank, Is.EqualTo(7));
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.FiveOrMore));
        }

        [Test]
        public void EveryFiveOrMoreMatchUsesOneSharedTier()
        {
            SherryBattleState state = CreateState();

            Assert.That(state.CompleteMatch(
                SherryMatchTier.FiveOrMore), Is.True);
            Assert.That(state.CompleteMatch(
                SherryMatchTier.FiveOrMore), Is.False);
            Assert.That(state.StylishRank, Is.EqualTo(1));
        }

        [Test]
        public void ConsumeAllRankPreservesPreviousMatchTier()
        {
            SherryBattleState state = CreateState();
            state.CompleteMatch(SherryMatchTier.Three);
            state.CompleteMatch(SherryMatchTier.Four);

            Assert.That(state.ConsumeAllRank(), Is.EqualTo(2));
            Assert.That(state.StylishRank, Is.Zero);
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.Four));
            Assert.That(state.ConsumeAllRank(), Is.Zero);
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.Four));
        }

        [TestCase(SherryMatchTier.None)]
        [TestCase((SherryMatchTier)999)]
        public void InvalidCompletedMatchTierIsRejected(
            SherryMatchTier matchTier)
        {
            SherryBattleState state = CreateState();

            Assert.That(
                () => state.CompleteMatch(matchTier),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("matchTier"));
            Assert.That(state.StylishRank, Is.Zero);
            Assert.That(state.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.None));
        }

        [Test]
        public void SeparateBattleInstancesKeepIndependentState()
        {
            SherryBattleState first = CreateState();
            SherryBattleState second = CreateState();

            first.CompleteMatch(SherryMatchTier.Three);
            first.CompleteMatch(SherryMatchTier.Four);

            Assert.That(first.StylishRank, Is.EqualTo(2));
            Assert.That(second.StylishRank, Is.Zero);
            Assert.That(second.PreviousMatchTier,
                Is.EqualTo(SherryMatchTier.None));
            Assert.That(first.Owner, Is.Not.SameAs(second.Owner));
        }

        [Test]
        public void InvalidOwnerAndMaximumAreRejected()
        {
            Assert.That(
                () => new SherryBattleState(
                    CreateCharacter("character_other"),
                    7),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("owner"));
            Assert.That(
                () => new SherryBattleState(
                    CreateCharacter(SherryRules.CharacterId),
                    0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("stylishMaxRank"));
            Assert.That(
                () => new SherryBattleState(
                    CreateCharacter(SherryRules.CharacterId),
                    8),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("stylishMaxRank"));
        }

        private static SherryBattleState CreateState()
        {
            return new SherryBattleState(
                CreateCharacter(SherryRules.CharacterId),
                SherryCombatConfig.RequiredStylishMaxRank);
        }

        private static CharacterBattleState CreateCharacter(
            string characterId)
        {
            return new CharacterBattleState(
                characterId,
                0,
                ElementType.Fire,
                1000,
                150d);
        }
    }
}

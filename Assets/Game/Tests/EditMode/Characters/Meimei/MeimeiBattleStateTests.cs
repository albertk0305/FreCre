using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Meimei;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Meimei
{
    public sealed class MeimeiBattleStateTests
    {
        private MeimeiCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = MeimeiTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void NewStateStartsEmpty()
        {
            MeimeiBattleState state = CreateState();

            Assert.That(state.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(state.CupCount, Is.Zero);
            Assert.That(state.HasAllCups, Is.False);
            Assert.That(state.Tipsiness, Is.Zero);
            Assert.That(state.TipsinessMaxAmount, Is.EqualTo(5));
            Assert.That(state.ActiveTipsinessSnapshot, Is.Zero);
            Assert.That(state.ActiveTipsinessSnapshotTurn, Is.Zero);
        }

        [Test]
        public void StatusSourcesExposeIndependentCupsInCanonicalOrder()
        {
            MeimeiBattleState state = CreateState();
            MeimeiCombatStatusSource[] sources = CreateStatusSources(state);
            state.TryAcquireCup(MeimeiCup.Final);
            state.TryAcquireCup(MeimeiCup.First);

            Assert.That(sources[0].StatusId,
                Is.EqualTo(MeimeiRules.FirstCupStatusId));
            Assert.That(sources[1].StatusId,
                Is.EqualTo(MeimeiRules.SecondCupStatusId));
            Assert.That(sources[2].StatusId,
                Is.EqualTo(MeimeiRules.FinalCupStatusId));
            Assert.That(sources[3].StatusId,
                Is.EqualTo(MeimeiRules.TipsinessStatusId));
            string[] distinctStatusIds = sources
                .Select(item => item.StatusId)
                .Distinct()
                .ToArray();
            Assert.That(distinctStatusIds, Has.Length.EqualTo(4));
            Assert.That(sources[0].IsVisible, Is.True);
            Assert.That(sources[1].IsVisible, Is.False);
            Assert.That(sources[2].IsVisible, Is.True);
            Assert.That(sources[0].CurrentCount, Is.EqualTo(1));
            Assert.That(sources[2].CurrentCount, Is.EqualTo(1));
            Assert.That(sources[0].IsBadgeVisible, Is.False);
            Assert.That(sources[2].IsBadgeVisible, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void TipsinessStatusIsVisibleAtEveryAmount(int amount)
        {
            MeimeiBattleState state = CreateState();
            MeimeiCombatStatusSource source =
                CreateStatusSources(state)[3];
            if (amount > 0)
            {
                state.GainTipsiness(amount);
            }

            Assert.That(source.IsVisible, Is.True);
            Assert.That(source.CurrentCount, Is.EqualTo(amount));
            Assert.That(source.IsBadgeVisible, Is.True);
        }

        [Test]
        public void CupAndTipsinessSourcesTrackConsumptionIndependently()
        {
            MeimeiBattleState state = CreateState();
            MeimeiCombatStatusSource[] sources = CreateStatusSources(state);
            AcquireAllCups(state);
            state.GainTipsiness(5);

            Assert.That(state.TryConsumeCompletedCups(), Is.True);

            Assert.That(sources[0].IsVisible, Is.False);
            Assert.That(sources[1].IsVisible, Is.False);
            Assert.That(sources[2].IsVisible, Is.False);
            Assert.That(sources[3].CurrentCount, Is.EqualTo(5));
            state.CommitSuccessfulActive(1, 3, false);
            Assert.That(sources[3].CurrentCount, Is.EqualTo(2));
        }

        [Test]
        public void SeparateBattlesExposeOnlyTheirOwnOwnerAndState()
        {
            MeimeiBattleState first = CreateState(partySlotIndex: 1);
            MeimeiBattleState second = CreateState(partySlotIndex: 4);
            MeimeiCombatStatusSource firstCup =
                CreateStatusSources(first)[0];
            MeimeiCombatStatusSource secondCup =
                CreateStatusSources(second)[0];
            first.TryAcquireCup(MeimeiCup.First);

            Assert.That(firstCup.PartySlotIndex, Is.EqualTo(1));
            Assert.That(secondCup.PartySlotIndex, Is.EqualTo(4));
            Assert.That(firstCup.IsVisible, Is.True);
            Assert.That(secondCup.IsVisible, Is.False);
        }

        [Test]
        public void SuccessfulActiveSnapshotsBeforeConsumptionAndConsumesActual()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(5);

            state.CommitSuccessfulActive(
                currentTurn: 3,
                convertedCount: 3,
                recordTipsinessSnapshot: true);

            Assert.That(state.Tipsiness, Is.EqualTo(2));
            Assert.That(state.ActiveTipsinessSnapshot, Is.EqualTo(5));
            Assert.That(state.ActiveTipsinessSnapshotTurn, Is.EqualTo(3));
            Assert.That(state.GetEffectiveTipsiness(3), Is.EqualTo(5));
            Assert.That(state.GetEffectiveTipsiness(4), Is.EqualTo(2));
        }

        [Test]
        public void ShuffleOnlySuccessCanSnapshotWithoutConsumingTipsiness()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(4);

            state.CommitSuccessfulActive(
                currentTurn: 2,
                convertedCount: 0,
                recordTipsinessSnapshot: true);

            Assert.That(state.Tipsiness, Is.EqualTo(4));
            Assert.That(state.ActiveTipsinessSnapshot, Is.EqualTo(4));
            Assert.That(state.ActiveTipsinessSnapshotTurn, Is.EqualTo(2));
        }

        [Test]
        public void InvalidActiveConsumptionLeavesStateUnchanged()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(2);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.CommitSuccessfulActive(
                    currentTurn: 5,
                    convertedCount: 3,
                    recordTipsinessSnapshot: true));

            Assert.That(state.Tipsiness, Is.EqualTo(2));
            Assert.That(state.ActiveTipsinessSnapshot, Is.Zero);
            Assert.That(state.ActiveTipsinessSnapshotTurn, Is.Zero);
        }

        [TestCase(
            MeimeiCup.First,
            MeimeiCup.Second,
            MeimeiCup.Final)]
        [TestCase(
            MeimeiCup.Final,
            MeimeiCup.First,
            MeimeiCup.Second)]
        [TestCase(
            MeimeiCup.Second,
            MeimeiCup.Final,
            MeimeiCup.First)]
        [TestCase(
            MeimeiCup.First,
            MeimeiCup.Final,
            MeimeiCup.Second)]
        [TestCase(
            MeimeiCup.Second,
            MeimeiCup.First,
            MeimeiCup.Final)]
        [TestCase(
            MeimeiCup.Final,
            MeimeiCup.Second,
            MeimeiCup.First)]
        public void CupsCanBeCompletedInAnyOrder(
            MeimeiCup first,
            MeimeiCup second,
            MeimeiCup third)
        {
            MeimeiBattleState state = CreateState();

            Assert.That(state.TryAcquireCup(first), Is.True);
            Assert.That(state.HasAllCups, Is.False);
            Assert.That(state.TryAcquireCup(second), Is.True);
            Assert.That(state.HasAllCups, Is.False);
            Assert.That(state.TryAcquireCup(third), Is.True);

            Assert.That(state.HasFirstCup, Is.True);
            Assert.That(state.HasSecondCup, Is.True);
            Assert.That(state.HasFinalCup, Is.True);
            Assert.That(state.CupCount, Is.EqualTo(3));
            Assert.That(state.HasAllCups, Is.True);
        }

        [Test]
        public void DuplicateCupIsIgnored()
        {
            MeimeiBattleState state = CreateState();

            Assert.That(state.TryAcquireCup(MeimeiCup.First), Is.True);
            Assert.That(state.TryAcquireCup(MeimeiCup.First), Is.False);

            Assert.That(state.CupCount, Is.EqualTo(1));
            Assert.That(state.Cups, Is.EqualTo(MeimeiCup.First));
        }

        [Test]
        public void CompletedCupsConsumeTogetherWithoutChangingTipsiness()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(4);
            AcquireAllCups(state);

            Assert.That(state.TryConsumeCompletedCups(), Is.True);

            Assert.That(state.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(state.CupCount, Is.Zero);
            Assert.That(state.HasAllCups, Is.False);
            Assert.That(state.Tipsiness, Is.EqualTo(4));
            Assert.That(state.TryConsumeCompletedCups(), Is.False);
        }

        [Test]
        public void IncompleteCupsCannotBeConsumed()
        {
            MeimeiBattleState state = CreateState();
            state.TryAcquireCup(MeimeiCup.First);
            state.TryAcquireCup(MeimeiCup.Final);

            Assert.That(state.TryConsumeCompletedCups(), Is.False);
            Assert.That(state.CupCount, Is.EqualTo(2));
        }

        [Test]
        public void CupsPersistWhenTurnNumberAdvances()
        {
            MeimeiBattleState state = CreateState();
            state.TryAcquireCup(MeimeiCup.Second);
            state.RecordActiveTipsinessSnapshot(currentTurn: 1);

            Assert.That(state.GetEffectiveTipsiness(currentTurn: 2),
                Is.Zero);
            Assert.That(state.HasSecondCup, Is.True);
            Assert.That(state.CupCount, Is.EqualTo(1));
        }

        [Test]
        public void TipsinessGainClampsAtMaximumAndReportsAppliedAmount()
        {
            MeimeiBattleState state = CreateState();

            Assert.That(state.GainTipsiness(1), Is.EqualTo(1));
            Assert.That(state.GainTipsiness(2), Is.EqualTo(2));
            Assert.That(state.GainTipsiness(5), Is.EqualTo(2));
            Assert.That(state.GainTipsiness(1), Is.Zero);
            Assert.That(state.Tipsiness, Is.EqualTo(5));
        }

        [Test]
        public void TipsinessConsumesOnlyWhenFullAmountIsAvailable()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(4);

            Assert.That(state.TryConsumeTipsiness(3), Is.True);
            Assert.That(state.Tipsiness, Is.EqualTo(1));
            Assert.That(state.TryConsumeTipsiness(2), Is.False);
            Assert.That(state.Tipsiness, Is.EqualTo(1));
            Assert.That(state.TryConsumeTipsiness(1), Is.True);
            Assert.That(state.Tipsiness, Is.Zero);
        }

        [Test]
        public void ActiveSnapshotUsesHigherCurrentOrRecordedValueForTurn()
        {
            MeimeiBattleState state = CreateState();
            state.GainTipsiness(4);
            state.RecordActiveTipsinessSnapshot(currentTurn: 3);
            Assert.That(state.TryConsumeTipsiness(4), Is.True);

            Assert.That(state.ActiveTipsinessSnapshot, Is.EqualTo(4));
            Assert.That(state.ActiveTipsinessSnapshotTurn, Is.EqualTo(3));
            Assert.That(state.HasActiveTipsinessSnapshotForTurn(3), Is.True);
            Assert.That(state.GetEffectiveTipsiness(3), Is.EqualTo(4));

            state.GainTipsiness(5);
            Assert.That(state.GetEffectiveTipsiness(3), Is.EqualTo(5));
            Assert.That(state.HasActiveTipsinessSnapshotForTurn(4), Is.False);
            Assert.That(state.GetEffectiveTipsiness(4), Is.EqualTo(5));
        }

        [Test]
        public void AwakeningSixCriticalRateUsesActiveSnapshotForCurrentTurn()
        {
            MeimeiBattleState state = CreateState();
            MeimeiResolvedCombatRules rules =
                MeimeiCombatRuleResolver.Resolve(config, 6);
            state.GainTipsiness(3);
            state.RecordActiveTipsinessSnapshot(currentTurn: 2);
            Assert.That(state.TryConsumeTipsiness(3), Is.True);

            double currentTurnRate =
                rules.CalculateCriticalChanceIncreaseRate(
                    state.GetEffectiveTipsiness(2));
            double nextTurnRate =
                rules.CalculateCriticalChanceIncreaseRate(
                    state.GetEffectiveTipsiness(3));

            Assert.That(currentTurnRate,
                Is.EqualTo(0.18d).Within(0.000000001d));
            Assert.That(nextTurnRate, Is.Zero);
        }

        [Test]
        public void InvalidMutationsAreRejectedWithoutChangingState()
        {
            MeimeiBattleState state = CreateState();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.TryAcquireCup(MeimeiCup.All));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.GainTipsiness(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.TryConsumeTipsiness(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                state.RecordActiveTipsinessSnapshot(0));
            Assert.That(state.Cups, Is.EqualTo(MeimeiCup.None));
            Assert.That(state.Tipsiness, Is.Zero);
        }

        [Test]
        public void InvalidOwnerOrMaximumIsRejected()
        {
            CharacterBattleState wrongId = CreateCharacter(
                "character_other",
                ElementType.Fire);
            CharacterBattleState wrongElement = CreateCharacter(
                MeimeiRules.CharacterId,
                ElementType.Water);

            Assert.That(
                () => new MeimeiBattleState(wrongId, 5),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("owner"));
            Assert.That(
                () => new MeimeiBattleState(wrongElement, 5),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("owner"));
            Assert.That(
                () => new MeimeiBattleState(CreateCharacter(), 0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("tipsinessMaxAmount"));
            Assert.That(
                () => new MeimeiBattleState(CreateCharacter(), 6),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("tipsinessMaxAmount"));
        }

        private static MeimeiBattleState CreateState()
        {
            return CreateState(partySlotIndex: 0);
        }

        private static MeimeiBattleState CreateState(int partySlotIndex)
        {
            return new MeimeiBattleState(
                CreateCharacter(partySlotIndex),
                5);
        }

        private static CharacterBattleState CreateCharacter()
        {
            return CreateCharacter(partySlotIndex: 0);
        }

        private static CharacterBattleState CreateCharacter(
            int partySlotIndex)
        {
            return CreateCharacter(
                MeimeiRules.CharacterId,
                ElementType.Fire,
                partySlotIndex);
        }

        private static CharacterBattleState CreateCharacter(
            string characterId,
            ElementType element,
            int partySlotIndex = 0)
        {
            return new CharacterBattleState(
                characterId,
                partySlotIndex,
                element,
                900,
                180d);
        }

        private static MeimeiCombatStatusSource[] CreateStatusSources(
            MeimeiBattleState state)
        {
            return new[]
            {
                new MeimeiCombatStatusSource(
                    state,
                    MeimeiCombatStatusKind.FirstCup),
                new MeimeiCombatStatusSource(
                    state,
                    MeimeiCombatStatusKind.SecondCup),
                new MeimeiCombatStatusSource(
                    state,
                    MeimeiCombatStatusKind.FinalCup),
                new MeimeiCombatStatusSource(
                    state,
                    MeimeiCombatStatusKind.Tipsiness)
            };
        }

        private static void AcquireAllCups(MeimeiBattleState state)
        {
            state.TryAcquireCup(MeimeiCup.First);
            state.TryAcquireCup(MeimeiCup.Second);
            state.TryAcquireCup(MeimeiCup.Final);
        }
    }
}

using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Meimei
{
    [Flags]
    public enum MeimeiCup
    {
        None = 0,
        First = 1 << 0,
        Second = 1 << 1,
        Final = 1 << 2,
        All = First | Second | Final
    }

    public enum MeimeiCombatStatusKind
    {
        FirstCup = 0,
        SecondCup = 1,
        FinalCup = 2,
        Tipsiness = 3
    }

    public sealed class MeimeiBattleState
    {
        private MeimeiCup cups;
        private int tipsiness;
        private int activeTipsinessSnapshot;
        private int activeTipsinessSnapshotTurn;

        public MeimeiBattleState(
            CharacterBattleState owner,
            int tipsinessMaxAmount)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                    owner.CharacterId,
                    MeimeiRules.CharacterId,
                    StringComparison.Ordinal)
                || owner.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Meimei battle state requires the canonical Fire "
                        + "Meimei owner.",
                    nameof(owner));
            }

            if (tipsinessMaxAmount != MeimeiRules.TipsinessMaxAmount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tipsinessMaxAmount),
                    tipsinessMaxAmount,
                    "Tipsiness maximum must match Meimei's fixed maximum "
                        + $"of {MeimeiRules.TipsinessMaxAmount}.");
            }

            TipsinessMaxAmount = tipsinessMaxAmount;
        }

        public CharacterBattleState Owner { get; }
        public MeimeiCup Cups => cups;
        public bool HasFirstCup => HasCup(MeimeiCup.First);
        public bool HasSecondCup => HasCup(MeimeiCup.Second);
        public bool HasFinalCup => HasCup(MeimeiCup.Final);
        public bool HasAllCups => cups == MeimeiCup.All;
        public int CupCount => CountCups(cups);
        public int Tipsiness => tipsiness;
        public int TipsinessMaxAmount { get; }
        public int ActiveTipsinessSnapshot => activeTipsinessSnapshot;
        public int ActiveTipsinessSnapshotTurn =>
            activeTipsinessSnapshotTurn;

        public bool HasCup(MeimeiCup cup)
        {
            ValidateSingleCup(cup);
            return (cups & cup) != 0;
        }

        public bool TryAcquireCup(MeimeiCup cup)
        {
            ValidateSingleCup(cup);
            if ((cups & cup) != 0)
            {
                return false;
            }

            cups |= cup;
            return true;
        }

        public bool TryConsumeCompletedCups()
        {
            if (!HasAllCups)
            {
                return false;
            }

            cups = MeimeiCup.None;
            return true;
        }

        public int GainTipsiness(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Tipsiness gain must be positive.");
            }

            int applied = Math.Min(
                amount,
                TipsinessMaxAmount - tipsiness);
            tipsiness += applied;
            return applied;
        }

        public bool TryConsumeTipsiness(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Tipsiness consumption must be positive.");
            }

            if (amount > tipsiness)
            {
                return false;
            }

            tipsiness -= amount;
            return true;
        }

        public void CommitSuccessfulActive(
            int currentTurn,
            int convertedCount,
            bool recordTipsinessSnapshot)
        {
            ValidateTurn(currentTurn);
            if (convertedCount < 0 || convertedCount > tipsiness)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(convertedCount),
                    convertedCount,
                    "Converted count must be consumable from current "
                        + "tipsiness.");
            }

            int tipsinessBeforeConsumption = tipsiness;
            if (recordTipsinessSnapshot)
            {
                activeTipsinessSnapshot = tipsinessBeforeConsumption;
                activeTipsinessSnapshotTurn = currentTurn;
            }

            tipsiness = tipsinessBeforeConsumption - convertedCount;
        }

        public void RecordActiveTipsinessSnapshot(int currentTurn)
        {
            ValidateTurn(currentTurn);
            activeTipsinessSnapshot = tipsiness;
            activeTipsinessSnapshotTurn = currentTurn;
        }

        public bool HasActiveTipsinessSnapshotForTurn(int currentTurn)
        {
            ValidateTurn(currentTurn);
            return activeTipsinessSnapshotTurn == currentTurn;
        }

        public int GetEffectiveTipsiness(int currentTurn)
        {
            ValidateTurn(currentTurn);
            return activeTipsinessSnapshotTurn == currentTurn
                ? Math.Max(tipsiness, activeTipsinessSnapshot)
                : tipsiness;
        }

        private static int CountCups(MeimeiCup value)
        {
            int count = 0;
            if ((value & MeimeiCup.First) != 0)
            {
                count++;
            }

            if ((value & MeimeiCup.Second) != 0)
            {
                count++;
            }

            if ((value & MeimeiCup.Final) != 0)
            {
                count++;
            }

            return count;
        }

        private static void ValidateSingleCup(MeimeiCup cup)
        {
            if (cup != MeimeiCup.First
                && cup != MeimeiCup.Second
                && cup != MeimeiCup.Final)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cup),
                    cup,
                    "Cup must identify exactly one of Meimei's three cups.");
            }
        }

        private static void ValidateTurn(int currentTurn)
        {
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTurn),
                    currentTurn,
                    "Current turn must be positive.");
            }
        }
    }

    public sealed class MeimeiCombatStatusSource
        : ICharacterCombatStatusSource,
            ICharacterCombatStatusBadgeVisibility
    {
        private readonly MeimeiBattleState battleState;

        public MeimeiCombatStatusSource(
            MeimeiBattleState battleState,
            MeimeiCombatStatusKind kind)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            if (!Enum.IsDefined(typeof(MeimeiCombatStatusKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Kind = kind;
        }

        public MeimeiCombatStatusKind Kind { get; }
        public string StatusId
        {
            get
            {
                switch (Kind)
                {
                    case MeimeiCombatStatusKind.FirstCup:
                        return MeimeiRules.FirstCupStatusId;
                    case MeimeiCombatStatusKind.SecondCup:
                        return MeimeiRules.SecondCupStatusId;
                    case MeimeiCombatStatusKind.FinalCup:
                        return MeimeiRules.FinalCupStatusId;
                    case MeimeiCombatStatusKind.Tipsiness:
                        return MeimeiRules.TipsinessStatusId;
                    default:
                        throw new InvalidOperationException(
                            "Unsupported Meimei combat status kind.");
                }
            }
        }

        public string CharacterId => battleState.Owner.CharacterId;
        public int PartySlotIndex => battleState.Owner.PartySlotIndex;
        public int CurrentCount
        {
            get
            {
                switch (Kind)
                {
                    case MeimeiCombatStatusKind.FirstCup:
                        return battleState.HasFirstCup ? 1 : 0;
                    case MeimeiCombatStatusKind.SecondCup:
                        return battleState.HasSecondCup ? 1 : 0;
                    case MeimeiCombatStatusKind.FinalCup:
                        return battleState.HasFinalCup ? 1 : 0;
                    case MeimeiCombatStatusKind.Tipsiness:
                        return battleState.Tipsiness;
                    default:
                        throw new InvalidOperationException(
                            "Unsupported Meimei combat status kind.");
                }
            }
        }

        public bool IsVisible => Kind == MeimeiCombatStatusKind.Tipsiness
            || CurrentCount > 0;
        public bool IsBadgeVisible =>
            Kind == MeimeiCombatStatusKind.Tipsiness;
    }
}

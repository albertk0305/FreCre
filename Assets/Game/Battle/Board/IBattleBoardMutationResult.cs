namespace ValorChronicle.Battle.Board
{
    public interface IBattleBoardMutationResult
    {
        BoardState Board { get; }
        BoardShuffleResult ShuffleResult { get; }
        bool WasShuffled { get; }
        bool Succeeded { get; }
    }
}

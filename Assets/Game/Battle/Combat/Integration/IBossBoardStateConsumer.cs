using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossBoardStateConsumer
    {
        void BindBoardStateSource(IBattleBoardStateSource boardStateSource);
    }
}

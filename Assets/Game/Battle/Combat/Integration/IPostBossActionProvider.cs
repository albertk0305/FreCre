using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IPostBossActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            PostBossActionContext context);
    }

    public sealed class PostBossActionContext
    {
        public PostBossActionContext(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            int currentTurn,
            CombatActionIdSequence actionIds)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            Boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTurn));
            }

            CurrentTurn = currentTurn;
            ActionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public int CurrentTurn { get; }
        public CombatActionIdSequence ActionIds { get; }
    }

    public sealed class PostBossActionProviderBinding
    {
        public PostBossActionProviderBinding(
            CharacterBattleState character,
            IPostBossActionProvider provider)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Provider = provider
                ?? throw new ArgumentNullException(nameof(provider));
        }

        public CharacterBattleState Character { get; }
        public IPostBossActionProvider Provider { get; }
    }
}

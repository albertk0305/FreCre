using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IActiveAbilityActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context);
    }

    /// <summary>
    /// Opts a provider into a successful Active use with no root actions.
    /// Providers without this marker must return at least one root action.
    /// </summary>
    public interface IActiveAbilityEmptyRootActionProvider
    {
    }

    /// <summary>
    /// Receives the single success commit after root actions are validated,
    /// an execution session is prepared, and Active runtime use is committed.
    /// Implementations must limit this callback to non-throwing battle-local
    /// state changes.
    /// </summary>
    public interface IActiveAbilitySuccessCommitter
    {
        void CommitActiveSuccess(ActiveAbilityActionContext context);
    }
}

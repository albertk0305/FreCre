using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossCombatRuntime
    {
        public BossCombatRuntime(
            IBossCombatActionProvider actionProvider,
            IBossIntentSource intentSource = null,
            IBossVisualStateSource visualStateSource = null)
        {
            ActionProvider = actionProvider
                ?? throw new ArgumentNullException(nameof(actionProvider));
            IntentSource = intentSource;
            VisualStateSource = visualStateSource;
        }

        public IBossCombatActionProvider ActionProvider { get; }
        public IBossIntentSource IntentSource { get; }
        public IBossVisualStateSource VisualStateSource { get; }
    }
}

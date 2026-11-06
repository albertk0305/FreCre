using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossCombatRuntime
    {
        public BossCombatRuntime(
            IBossCombatActionProvider actionProvider,
            IBossIntentSource intentSource = null,
            IBossVisualStateSource visualStateSource = null,
            IReadOnlyList<ICombatTriggerRule> triggerRules = null)
        {
            ActionProvider = actionProvider
                ?? throw new ArgumentNullException(nameof(actionProvider));
            IntentSource = intentSource;
            VisualStateSource = visualStateSource;
            if (triggerRules == null || triggerRules.Count == 0)
            {
                TriggerRules = Array.Empty<ICombatTriggerRule>();
                return;
            }

            var copiedRules = new ICombatTriggerRule[triggerRules.Count];
            for (int index = 0; index < triggerRules.Count; index++)
            {
                copiedRules[index] = triggerRules[index]
                    ?? throw new ArgumentException(
                        "Boss trigger rules cannot contain null.",
                        nameof(triggerRules));
            }

            TriggerRules = new ReadOnlyCollection<ICombatTriggerRule>(
                copiedRules);
        }

        public IBossCombatActionProvider ActionProvider { get; }
        public IBossIntentSource IntentSource { get; }
        public IBossVisualStateSource VisualStateSource { get; }
        public IReadOnlyList<ICombatTriggerRule> TriggerRules { get; }
    }
}

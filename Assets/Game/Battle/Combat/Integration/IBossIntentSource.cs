using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IBossIntentSource
    {
        IReadOnlyList<BossIntentPreview> GetIntentForecast(int count);
    }
}

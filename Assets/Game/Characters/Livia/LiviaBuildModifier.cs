using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Livia
{
    public sealed class LiviaBuildModifier : ICharacterBuildModifier
    {
        public string CharacterId => LiviaRules.CharacterId;

        public CharacterBuildStatMultipliers GetStatMultipliers(int awakening)
        {
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
                throw new ArgumentOutOfRangeException(nameof(awakening));

            return new CharacterBuildStatMultipliers(
                maxHpMultiplier: awakening >= 1 ? 1.05d : 1d,
                attackMultiplier: 1d);
        }
    }
}

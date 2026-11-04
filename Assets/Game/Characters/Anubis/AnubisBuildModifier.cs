using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Anubis
{
    public sealed class AnubisBuildModifier : ICharacterBuildModifier
    {
        private const double AwakeningStatMultiplier = 1.05d;

        public string CharacterId => AnubisRules.CharacterId;

        public CharacterBuildStatMultipliers GetStatMultipliers(int awakening)
        {
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(nameof(awakening));
            }

            return new CharacterBuildStatMultipliers(
                maxHpMultiplier: awakening >= 2
                    ? AwakeningStatMultiplier
                    : 1d,
                attackMultiplier: awakening >= 1
                    ? AwakeningStatMultiplier
                    : 1d);
        }
    }
}

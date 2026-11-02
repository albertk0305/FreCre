using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Luciel
{
    public sealed class LucielBuildModifier : ICharacterBuildModifier
    {
        private const double Awakening1MaxHpMultiplier = 1.05d;

        public string CharacterId => LucielRules.CharacterId;

        public CharacterBuildStatMultipliers GetStatMultipliers(
            int awakening)
        {
            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new CharacterBuildStatMultipliers(
                maxHpMultiplier: awakening >= 1
                    ? Awakening1MaxHpMultiplier
                    : 1d,
                attackMultiplier: 1d);
        }
    }
}

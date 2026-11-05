using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityActionProviderRegistry
    {
        private readonly Dictionary<ProviderKey, IActiveAbilityActionProvider>
            providers =
                new Dictionary<ProviderKey, IActiveAbilityActionProvider>();
        private readonly Dictionary<ProviderKey, IActiveAbilityBoardPlanProvider>
            boardProviders =
                new Dictionary<ProviderKey, IActiveAbilityBoardPlanProvider>();
        private readonly Dictionary<ProviderKey,
            IActiveAbilityBoardShuffleElementConversionPlanProvider>
            boardShuffleElementConversionProviders =
                new Dictionary<ProviderKey,
                    IActiveAbilityBoardShuffleElementConversionPlanProvider>();

        public int Count => providers.Count + boardProviders.Count
            + boardShuffleElementConversionProviders.Count;

        public void Register(
            string characterId,
            string activeAbilityId,
            IActiveAbilityActionProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var key = new ProviderKey(characterId, activeAbilityId);
            if (Contains(key))
            {
                throw new InvalidOperationException(
                    $"An active provider is already registered for "
                        + $"'{characterId}/{activeAbilityId}'.");
            }

            providers.Add(key, provider);
        }

        public void RegisterBoard(
            string characterId,
            string activeAbilityId,
            IActiveAbilityBoardPlanProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var key = new ProviderKey(characterId, activeAbilityId);
            if (Contains(key))
            {
                throw new InvalidOperationException(
                    $"An active provider is already registered for "
                        + $"'{characterId}/{activeAbilityId}'.");
            }

            boardProviders.Add(key, provider);
        }

        public void RegisterBoardShuffleElementConversion(
            string characterId,
            string activeAbilityId,
            IActiveAbilityBoardShuffleElementConversionPlanProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var key = new ProviderKey(characterId, activeAbilityId);
            if (Contains(key))
            {
                throw new InvalidOperationException(
                    $"An active provider is already registered for "
                        + $"'{characterId}/{activeAbilityId}'.");
            }

            boardShuffleElementConversionProviders.Add(key, provider);
        }

        public bool TryResolve(
            string characterId,
            string activeAbilityId,
            out IActiveAbilityActionProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            return providers.TryGetValue(
                new ProviderKey(characterId, activeAbilityId),
                out provider);
        }

        public bool TryResolveBoard(
            string characterId,
            string activeAbilityId,
            out IActiveAbilityBoardPlanProvider provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            return boardProviders.TryGetValue(
                new ProviderKey(characterId, activeAbilityId),
                out provider);
        }

        public bool TryResolveBoardShuffleElementConversion(
            string characterId,
            string activeAbilityId,
            out IActiveAbilityBoardShuffleElementConversionPlanProvider
                provider)
        {
            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            return boardShuffleElementConversionProviders.TryGetValue(
                new ProviderKey(characterId, activeAbilityId),
                out provider);
        }

        private bool Contains(ProviderKey key)
        {
            return providers.ContainsKey(key)
                || boardProviders.ContainsKey(key)
                || boardShuffleElementConversionProviders.ContainsKey(key);
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "ID cannot be null or whitespace.",
                    parameterName);
            }
        }

        private readonly struct ProviderKey : IEquatable<ProviderKey>
        {
            public ProviderKey(string characterId, string activeAbilityId)
            {
                CharacterId = characterId;
                ActiveAbilityId = activeAbilityId;
            }

            private string CharacterId { get; }
            private string ActiveAbilityId { get; }

            public bool Equals(ProviderKey other)
            {
                return string.Equals(
                        CharacterId,
                        other.CharacterId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        ActiveAbilityId,
                        other.ActiveAbilityId,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is ProviderKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((CharacterId != null
                                ? StringComparer.Ordinal.GetHashCode(
                                    CharacterId)
                                : 0) * 397)
                        ^ (ActiveAbilityId != null
                            ? StringComparer.Ordinal.GetHashCode(
                                ActiveAbilityId)
                            : 0);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The reconciled nineteen-front Snorri inventory, still behind the registration gate.</summary>
    public static class SnorriCardContent
    {
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAll.Value;
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealt.Value;
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { SnorriHandicapContent.RuneShards });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAll =
            new Lazy<IReadOnlyList<CardDefinition>>(Compose);
        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealt =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                AllDefinitions.Where(card => !ReturnedToBoxAtSetup.Contains(card.Id)).ToArray()));

        private static IReadOnlyList<CardDefinition> Compose()
        {
            var definitions = SnorriAttackFaces.Definitions
                .Concat(new[] { SnorriSilverFace.Silver })
                .Concat(SnorriBlueMovementFaces.Definitions)
                .Concat(new[]
                {
                    SnorriBlueSupportFaces.AncestralBoon,
                    SnorriBlueSupportFaces.AncestralGrace
                })
                .Concat(SnorriGreenFaces.Definitions)
                .Concat(new[]
                {
                    SnorriUltimateContent.Definition,
                    SnorriHandicapContent.Definition
                })
                .ToArray();
            if (definitions.Length != 19 ||
                definitions.Select(card => card.Id).Distinct().Count() != 19)
                throw new InvalidOperationException(
                    "Snorri's printed inventory must contain nineteen distinct definitions.");
            return Array.AsReadOnly(definitions);
        }
    }
}

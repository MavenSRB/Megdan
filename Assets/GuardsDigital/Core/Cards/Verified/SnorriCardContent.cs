using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The reconciled nineteen-front Snorri inventory, still behind the registration gate.</summary>
    public static partial class SnorriCardContent
    {
        public const string ContentRevision = "snorri-9-17a-rev-1";
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } =
            Array.AsReadOnly(new[]
            {
                SnorriAttackFaces.RuneSigils,
                SnorriSilverFace.InscribeTheRunes,
                SnorriAttackFaces.RunicDagger,
                SnorriBlueMovementFaces.SafePassage,
                SnorriGreenFaces.OathOfEndurance
            });

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

        public static IReadOnlyList<CardInstance> CreateDealtCards(
            HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined || string.IsNullOrWhiteSpace(instanceIdPrefix))
                throw new ArgumentException("A dealt hand requires a hero and instance prefix.");
            return Array.AsReadOnly(DealtDefinitions.Select(definition =>
            {
                var id = new CardInstanceId(instanceIdPrefix + "-" + definition.Id.Value);
                return StartingHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(id, definition.Reference, ownerId)
                    : CardInstance.CreateInDeck(id, definition.Reference, ownerId);
            }).ToArray());
        }

        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }
    }
}

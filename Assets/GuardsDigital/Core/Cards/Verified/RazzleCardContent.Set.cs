using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21j. Razzle's printed set reconciled against his shard's nineteen-row table, his
    /// boxed Handicap (Stunted Doubles, composed at 9.21d), and the dealing: his p.6 starting hand and
    /// the deck the rest of the set forms. The per-block lists of 9.21d-9.21h are replaced by the one
    /// aggregate here.
    /// </summary>
    public static partial class RazzleCardContent
    {
        /// <summary>His shard's nineteen rows, in the table's order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>p.6: the Gold and Silver basics and each colour's Tier I.</summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup => LazyReturnedToBoxAtSetup.Value;

        /// <summary>Every printed face, in inventory order - the list the corpus sweeps walk.</summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The faces a match deals from: everything but the boxed Handicap.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>The inventory rows no block has composed - empty since 9.21h; the dealing requires it.</summary>
        public static IReadOnlyList<CardDefinitionId> NotYetComposed => LazyNotYetComposed.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Razzle prints no composed card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Razzle's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix.",
                    nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}");
                    return StartingHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray());
        }

        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyInventoryOrder =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                StuntDoubles, CrowdControl,
                PhantomStrike, HitAndGone, Rummage, IntoThinAir, Ransack,
                Alleyoop, GroupPerformance, MagicTrick, TeamSpirit, AaaandItsGone,
                Tightrope, HighWire, Theatrics, WireDancers, Spectacle,
                TwinStrike,
                StuntedDoubles
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                StuntDoubles, CrowdControl, PhantomStrike, Alleyoop, Tightrope
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { StuntedDoubles }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(InventoryOrder
                .Where(id => !StartingHand.Contains(id) && !ReturnedToBoxAtSetup.Contains(id))
                .ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(Compose);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyNotYetComposed =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(InventoryOrder
                .Where(id => LazyAllDefinitions.Value.All(definition => !definition.Id.Equals(id)))
                .ToArray()));

        /// <summary>
        /// The composed faces in inventory order, refused if any is not in the inventory table - a face
        /// composed but never inventoried is a load-time error.
        /// </summary>
        private static IReadOnlyList<CardDefinition> Compose()
        {
            var composed = new[]
                {
                    // 9.21d: the Gold basic and the boxed Handicap.
                    StuntDoublesDefinition, StuntedDoublesDefinition,
                    // 9.21e: the three removal Attacks and Crowd Control.
                    PhantomStrikeDefinition, HitAndGoneDefinition, IntoThinAirDefinition, CrowdControlDefinition,
                    // 9.21f: the nine moves and swaps of his bodies.
                    AlleyoopDefinition, GroupPerformanceDefinition, MagicTrickDefinition,
                    TeamSpiritDefinition, AaaandItsGoneDefinition,
                    TightropeDefinition, HighWireDefinition, TheatricsDefinition, WireDancersDefinition,
                    // 9.21g: the two counting Attacks.
                    RummageDefinition, RansackDefinition,
                    // 9.21h: the two repeats by a different body.
                    SpectacleDefinition, TwinStrikeDefinition
                }
                .ToDictionary(definition => definition.Id);
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Razzle's printed set is his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder
                .Where(composed.ContainsKey)
                .Select(id => composed[id])
                .ToArray());
        }
    }
}

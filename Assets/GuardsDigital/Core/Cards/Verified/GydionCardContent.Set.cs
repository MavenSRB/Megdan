using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30i. Gydion's printed set reconciled against his shard's nineteen-row inventory
    /// table and twenty-two-row Spells table, his boxed Handicap (Cheap Trick), and the dealing: his
    /// p.6 starting hand, the deck the rest of the fronts form, and his spellbook, which setup fills
    /// with all twenty-two spells (GY-Q1).
    /// </summary>
    public static partial class GydionCardContent
    {
        /// <summary>His shard's nineteen action fronts, in the inventory table's order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>p.6: the Gold and Silver basics and each colour's Tier I.</summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        /// <summary>p.6: "return the remaining Tier II, Tier III, and Tier IV cards to your deck".</summary>
        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        /// <summary>p.20: the Handicap returns to the box at setup.</summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup => LazyReturnedToBoxAtSetup.Value;

        /// <summary>The nineteen composed action fronts, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> ActionCardDefinitions => LazyActionCards.Value;

        /// <summary>
        /// Every printed face - the nineteen action fronts in inventory order, then the twenty-two spells
        /// in the shard's order - the list the corpus sweeps walk (milestone 9.8a's trap: a
        /// <c>*CardContent</c> class declares it).
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>
        /// The action fronts a match deals into his hand and deck: every front but the boxed Handicap.
        /// His spells are not dealt; setup puts them in the spellbook (<see cref="CreateSpellbook"/>).
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>The inventory rows no block has composed - empty since 9.30i; the dealing requires it.</summary>
        public static IReadOnlyList<CardDefinitionId> NotYetComposed => LazyNotYetComposed.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Gydion prints no card '{id}'.", nameof(id));

        /// <summary>His eighteen dealt fronts: p.6's five in hand, the rest in his deck.</summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            RequireOwnerAndPrefix(ownerId, instanceIdPrefix);
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

        /// <summary>GY-Q1: all twenty-two spells, Wish included, in his spellbook at setup.</summary>
        public static IReadOnlyList<CardInstance> CreateSpellbook(HeroId ownerId, string instanceIdPrefix)
        {
            RequireOwnerAndPrefix(ownerId, instanceIdPrefix);
            return Array.AsReadOnly(SpellDefinitions
                .Select(spell => CardInstance.CreateInSpellbook(
                    new CardInstanceId($"{instanceIdPrefix}-{spell.Id.Value}"), spell.Reference, ownerId))
                .ToArray());
        }

        /// <summary>
        /// His hand as setup leaves it: the dealt fronts and the spells as the hand's definitions, p.6's
        /// five in hand, thirteen in the deck, twenty-two in the book.
        /// </summary>
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions.Concat(SpellDefinitions),
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                CreateSpellbook(ownerId, instanceIdPrefix));
        }

        private static void RequireOwnerAndPrefix(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Gydion's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix.",
                    nameof(instanceIdPrefix));
            }
        }

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyInventoryOrder =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                Cantrip, PrepareSpells,
                ElementaryEvocation, LesserEvocation, LesserNecromancy, GreaterEvocation, GreaterNecromancy,
                ElementaryEnchantment, LesserEnchantment, LesserTransmutation, GreaterEnchantment,
                GreaterTransmutation,
                ElementaryAbjuration, LesserAbjuration, LesserConjuration, GreaterAbjuration, GreaterConjuration,
                TheArchwizard,
                CheapTrick
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                Cantrip, PrepareSpells, ElementaryEvocation, ElementaryEnchantment, ElementaryAbjuration
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { CheapTrick }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(InventoryOrder
                .Where(id => !StartingHand.Contains(id) && !ReturnedToBoxAtSetup.Contains(id))
                .ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyActionCards =
            new Lazy<IReadOnlyList<CardDefinition>>(ComposeActionCards);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyActionCards.Value.Concat(SpellDefinitions).ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyActionCards.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyNotYetComposed =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(InventoryOrder
                .Where(id => LazyActionCards.Value.All(definition => !definition.Id.Equals(id)))
                .ToArray()));

        /// <summary>
        /// The composed fronts in inventory order, refused if any is not in the inventory table - a face
        /// composed but never inventoried is a load-time error.
        /// </summary>
        private static IReadOnlyList<CardDefinition> ComposeActionCards()
        {
            var composed = CastingCardDefinitions
                .Concat(ArchwizardCardDefinitions)
                .ToDictionary(definition => definition.Id);
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Gydion's action fronts are his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder
                .Where(composed.ContainsKey)
                .Select(id => composed[id])
                .ToArray());
        }
    }
}

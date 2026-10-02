using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.32h. Takahide's printed set reconciled against his shard's twenty-one-row table,
    /// his boxed Handicap (Shameful Display), and the dealing: his p.6 starting hand - Float like a
    /// Butterfly (by its own text), Bushido and the three Tier I faces - and the deck the rest form,
    /// Sting like a Bee and Strike like a Tiger among them by their own text.
    /// </summary>
    public static partial class TakahideCardContent
    {
        // -------------------------------------------------------------------------------------------
        // Shameful Display - p.20's boxed Handicap
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId ShamefulDisplay => Id("shameful-display");

        public const string ShamefulDisplayText = "You are defeated.";

        /// <summary>
        /// Silver Handicap: Initiative 5, a Basic Defense printing "!" in place of a value, no ribbon
        /// icons, and the text "You are defeated." p.6 returns it to the box at setup and p.20 brings it
        /// back only for an odd player count, which no configuration in this build is. Every earlier
        /// Handicap ships its action with its text deferred to that configuration; this one's "!" is
        /// p.14's Block icon, and no Block condition this build declares says "You are defeated", so
        /// its definition carries the transcription and no composed action until that configuration
        /// exists - a face that can never be dealt is not given a reading no card needs.
        /// </summary>
        public static CardDefinition ShamefulDisplayDefinition => LazyShamefulDisplay.Value;

        private static readonly Lazy<CardDefinition> LazyShamefulDisplay = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                ShamefulDisplay, ContentRevision, "Shameful Display", new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                Array.Empty<ActionDefinition>(),
                null, CardSourceContent.Transcribed("Basic Defense: ! " + ShamefulDisplayText)));

        // -------------------------------------------------------------------------------------------
        // The set
        // -------------------------------------------------------------------------------------------

        /// <summary>His shard's twenty-one rows, in the table's order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>
        /// p.6: the basic cards that start in the hand and each colour's Tier I. Of his three gold
        /// cards only Float like a Butterfly starts in the hand ("This card starts the game in your
        /// hand"); Sting like a Bee and Strike like a Tiger start in the deck by their own text.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        /// <summary>p.6: "return the remaining Tier II, Tier III, and Tier IV cards to your deck".</summary>
        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        /// <summary>p.20: the Handicap returns to the box at setup.</summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup => LazyReturnedToBoxAtSetup.Value;

        /// <summary>Every printed face, in inventory order - the list the corpus sweeps walk.</summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The twenty definitions a match deals from - everything but the boxed Handicap.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>The inventory rows no block has composed - empty since 9.32h; the dealing requires it.</summary>
        public static IReadOnlyList<CardDefinitionId> NotYetComposed => LazyNotYetComposed.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Takahide prints no card '{id}'.", nameof(id));

        /// <summary>His twenty dealt fronts: p.6's five in hand, the other fifteen in his deck.</summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Takahide's cards require a defined owner.", nameof(ownerId));
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

        /// <summary>His hand as setup leaves it: p.6's five in hand and fifteen in the deck.</summary>
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
                StingLikeABee, FloatLikeAButterfly, StrikeLikeATiger, Bushido,
                SetAnExample, LeadFromTheFront, SpinningBlade, HoldMySake, BladeHelix,
                ProvenWarrior, ChosenChampion, CalculatedRisk, TheRightHand, TacticalGambit,
                ComeToAid, BringTheRelief, PledgeOfAllegiance, CommitReserves, LoyalRetainer,
                ReadyForWar,
                ShamefulDisplay
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                FloatLikeAButterfly, Bushido, SetAnExample, ProvenWarrior, ComeToAid
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { ShamefulDisplay }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(InventoryOrder
                .Where(id => !StartingHand.Contains(id) && !ReturnedToBoxAtSetup.Contains(id))
                .ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(ComposeAllDefinitions);

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
        private static IReadOnlyList<CardDefinition> ComposeAllDefinitions()
        {
            var composed = BasicAndRedDefinitions
                .Concat(BlueAndGreenDefinitions)
                .Concat(UltimateDefinitions)
                .Concat(new[] { ShamefulDisplayDefinition })
                .ToDictionary(definition => definition.Id);
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Takahide's faces are his shard's twenty-one-row table and nothing else. Composed " +
                    "but not in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder
                .Where(composed.ContainsKey)
                .Select(id => composed[id])
                .ToArray());
        }
    }
}

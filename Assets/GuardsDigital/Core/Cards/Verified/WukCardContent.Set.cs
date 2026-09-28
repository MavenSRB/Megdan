using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114e. Wuk's Ultimate, his boxed Handicap, and the printed set reconciled against his
    /// shard's nineteen-row inventory table. Every definition here is Lazy: a static read across partial
    /// files runs in Unity's initialisation order, not the harness's (carried lesson 12).
    /// </summary>
    public static partial class WukCardContent
    {
        // -------------------------------------------------------------------------------------------
        // March of Nature - p.16's Ultimate
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId MarchOfNature => LazyMarchOfNatureId.Value;

        private static readonly Lazy<CardDefinitionId> LazyMarchOfNatureId =
            new Lazy<CardDefinitionId>(() => Id("march-of-nature"));

        public const string MarchOfNatureText =
            "Each time after you resolve a card, you may place a Tree token in radius.";

        public const string MarchOfNatureReclaimNode = "march-of-nature-reclaim";
        public const string MarchOfNaturePlaceNode = "march-of-nature-place";

        /// <summary>
        /// "you may place a Tree token in radius" - p.18's short-supply step and one optional supply
        /// Place, both reaching the Ultimate's own printed Radius 3 with p.16's items (the reach Tali's
        /// Reign of Winter reads off its rule). The same pair Mystic Saplings composes three times.
        /// </summary>
        public static EffectDefinition MarchOfNatureTriggeredEffect => LazyMarchEffect.Value;

        private static readonly Lazy<EffectDefinition> LazyMarchEffect = new Lazy<EffectDefinition>(() =>
        {
            var root = new EffectNodeId("march-of-nature-root");
            var reclaim = new EffectNodeId(MarchOfNatureReclaimNode);
            var place = new EffectNodeId(MarchOfNaturePlaceNode);
            return new EffectDefinition(
                new EffectDefinitionId(MarchOfNature.Value + "-ultimate"),
                ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { reclaim, place }),
                    new EffectNodeDefinition(
                        reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        WukComponentContent.ReclaimTreeV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        WukComponentContent.PlaceTreeInRadiusV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                        })
                });
        });

        /// <summary>
        /// Purple, Tier IV. Milestone 8.114e's <see cref="UltimateTriggerCondition.AfterControllerResolvesCard"/>
        /// at the one checkpoint an action's completion raises, carrying its printed Radius 3.
        /// </summary>
        public static CardDefinition MarchOfNatureDefinition => LazyUltimate.Value;

        private static readonly Lazy<CardDefinition> LazyUltimate = new Lazy<CardDefinition>(() => new CardDefinition(
            MarchOfNature, ContentRevision, "March of Nature", default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.TriggeredAbility(
                "march-of-nature-after-card-resolution",
                "WukMarchOfNature",
                MarchOfNatureTriggeredEffect.Reference,
                UltimateTriggerCondition.AfterControllerResolvesCard,
                new[] { TimingCheckpoint.ActionEnded },
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) }),
            CardSourceContent.Transcribed(MarchOfNatureText)));

        /// <summary>Every triggered effect Wuk's content declares.</summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects() =>
            Array.AsReadOnly(new[] { MarchOfNatureTriggeredEffect });

        // -------------------------------------------------------------------------------------------
        // Makeshift Weapon - p.20's boxed Handicap
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId MakeshiftWeapon => LazyMakeshiftId.Value;

        private static readonly Lazy<CardDefinitionId> LazyMakeshiftId =
            new Lazy<CardDefinitionId>(() => Id("makeshift-weapon"));

        public const string MakeshiftWeaponText =
            "Choose one - • Target a hero adjacent to you. • Remove a Tree token adjacent to you. " +
            "Target a hero in range.";

        /// <summary>
        /// Gold Handicap: Initiative 10, Defense 2, Movement 1, Basic Attack - Ranged 3, Range 2. p.6
        /// returns it to the box at setup and p.20 brings it back only for an odd player count, which no
        /// configuration in this build is - so its text is deferred to that configuration, exactly as
        /// every earlier hero's Handicap is.
        /// </summary>
        public static CardDefinition MakeshiftWeaponDefinition => LazyMakeshift.Value;

        private static readonly Lazy<CardDefinition> LazyMakeshift = new Lazy<CardDefinition>(() => new CardDefinition(
            MakeshiftWeapon, ContentRevision, "Makeshift Weapon", new InitiativeValue(10),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryRangedAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged, default,
                    null,
                    PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                    PrintedDefenseResolution.None,
                    CardTextTiming.None),
                Movement(1),
                Defense(2)
            },
            null, CardSourceContent.Transcribed(MakeshiftWeaponText)));

        // -------------------------------------------------------------------------------------------
        // The set
        // -------------------------------------------------------------------------------------------

        /// <summary>His shard's nineteen rows, in the table's order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>p.6: the Gold and Silver basics and each colour's Tier I.</summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup => LazyReturnedToBoxAtSetup.Value;

        /// <summary>
        /// Every printed face composed so far, in inventory order - the list the corpus sweeps walk.
        /// Until 8.114b1 and 8.114c1 compose the trampling moves and Dominance it holds fifteen of the
        /// nineteen; <see cref="NotYetComposed"/> names the four still owed rather than letting the set
        /// pass as whole.
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The faces a match deals from: everything but the boxed Handicap.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>
        /// The inventory rows no block has composed yet. Milestone 8.114e: Trample and Angry Stampede,
        /// owed to 8.114b1 (paused on WU-Q5), and Claim and Assert Dominance, owed to 8.114c1. The
        /// dealing (8.114f) requires this to be empty.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> NotYetComposed => LazyNotYetComposed.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Wuk prints no composed card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Wuk's cards require a defined owner.", nameof(ownerId));
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
                TreeSlam, MysticSaplings,
                WukRedFaces.NaturesProtector, WukRedFaces.NaturesGuardian, WukRedFaces.Trample,
                WukRedFaces.NaturesChampion, WukRedFaces.AngryStampede,
                WukBlueFaces.GiftsOfNature, WukBlueFaces.TreeOfPlenty, WukBlueFaces.ClaimDominance,
                WukBlueFaces.Abundance, WukBlueFaces.AssertDominance,
                WukGreenFaces.TossAway, WukGreenFaces.MightyThrow, WukGreenFaces.IntoTheCanopy,
                WukGreenFaces.MonstrousThrow, WukGreenFaces.TreetopRide,
                MarchOfNature,
                MakeshiftWeapon
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                TreeSlam, MysticSaplings, WukRedFaces.NaturesProtector, WukBlueFaces.GiftsOfNature,
                WukGreenFaces.TossAway
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { MakeshiftWeapon }));

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
        /// composed but never inventoried is a load-time error. Inventoried faces not yet composed are
        /// reported by <see cref="NotYetComposed"/> instead of refused, because 8.114b1 is paused and the
        /// sweeps must still walk every face that exists.
        /// </summary>
        private static IReadOnlyList<CardDefinition> Compose()
        {
            var composed = new[] { TreeSlamDefinition, MysticSaplingsDefinition }
                .Concat(WukRedFaces.AttackDefinitions)
                .Concat(WukBlueFaces.RetrievalDefinitions)
                .Concat(WukGreenFaces.Definitions)
                .Concat(new[] { MarchOfNatureDefinition, MakeshiftWeaponDefinition })
                .ToDictionary(definition => definition.Id);
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Wuk's printed set is his shard's nineteen-row table and nothing else. Composed but not " +
                    "in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder
                .Where(composed.ContainsKey)
                .Select(id => composed[id])
                .ToArray());
        }
    }
}

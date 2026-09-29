using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8f. Mortimer's printed set reconciled against his shard's nineteen-row table, his
    /// boxed Handicap, and the dealing: his p.6 starting hand and the deck the rest of the set forms.
    /// </summary>
    public static partial class MortimerCardContent
    {
        // -------------------------------------------------------------------------------------------
        // Shallow Graves - p.20's boxed Handicap
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId ShallowGraves { get; } = Id("shallow-graves");

        public const string ShallowGravesText =
            "Target a hero adjacent to you. Before the attack, Choose up to two times - Move a Zombie " +
            "token in radius 1 space. / Remove a Zombie token adjacent to the target; if you do, gain " +
            "+1 Attack.";

        /// <summary>
        /// Gold Handicap: Initiative 10, Defense 1, Movement 1, Basic Attack 3, Radius 4. p.6 returns
        /// it to the box at setup and p.20 brings it back only for an odd player count, which no
        /// configuration in this build is - so its text is deferred to that configuration, exactly as
        /// every earlier hero's Handicap is.
        /// </summary>
        public static CardDefinition ShallowGravesDefinition => LazyShallowGraves.Value;

        private static readonly Lazy<CardDefinition> LazyShallowGraves = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                ShallowGraves, ContentRevision, "Shallow Graves", new InitiativeValue(10),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        MortimerAttackFaces.PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 4)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        null,
                        PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                        PrintedDefenseResolution.None,
                        CardTextTiming.None),
                    Movement(1),
                    Defense(1)
                },
                null, CardSourceContent.Transcribed(ShallowGravesText)));

        // -------------------------------------------------------------------------------------------
        // The set
        // -------------------------------------------------------------------------------------------

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

        /// <summary>The inventory rows no block has composed - empty since 9.8e; the dealing requires it.</summary>
        public static IReadOnlyList<CardDefinitionId> NotYetComposed => LazyNotYetComposed.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Mortimer prints no composed card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Mortimer's cards require a defined owner.", nameof(ownerId));
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
                MortimerAttackFaces.KnifeOfTheLivingDead, Awaken,
                MortimerAttackFaces.CrawlingDead, MortimerAttackFaces.WalkingDead, MortimerAttackFaces.Braains,
                MortimerAttackFaces.RacingDead, MortimerAttackFaces.Braaaaaaains,
                MortimerBlueFaces.CorpseSlam, MortimerBlueFaces.MorbidMosh, MortimerBlueFaces.RobbingZombies,
                MortimerBlueFaces.MacabreMayhem, MortimerBlueFaces.StalkingScalpers,
                MortimerGreenFaces.StageDive, MortimerGreenFaces.CrowdDrift, MortimerGreenFaces.GatheringHorde,
                MortimerGreenFaces.CrowdSurf, MortimerGreenFaces.ArmyOfDarkness,
                MasterOfPuppets,
                ShallowGraves
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                MortimerAttackFaces.KnifeOfTheLivingDead, Awaken, MortimerAttackFaces.CrawlingDead,
                MortimerBlueFaces.CorpseSlam, MortimerGreenFaces.StageDive
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { ShallowGraves }));

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
            var composed = new[] { AwakenDefinition, MasterOfPuppetsDefinition, ShallowGravesDefinition }
                .Concat(MortimerAttackFaces.Definitions)
                .Concat(MortimerBlueFaces.Definitions)
                .Concat(MortimerGreenFaces.Definitions)
                .ToDictionary(definition => definition.Id);
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Mortimer's printed set is his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder
                .Where(composed.ContainsKey)
                .Select(id => composed[id])
                .ToArray());
        }
    }
}

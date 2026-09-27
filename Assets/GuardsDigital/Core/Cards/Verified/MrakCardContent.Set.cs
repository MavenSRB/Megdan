using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.112f. Mrak's Ultimate, his boxed Handicap, and the printed set reconciled against
    /// his shard's nineteen-row inventory table.
    /// </summary>
    public static partial class MrakCardContent
    {
        // -------------------------------------------------------------------------------------------
        // Rock and a Hard Place - p.16's Ultimate
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId RockAndAHardPlace { get; } = Id("rock-and-a-hard-place");

        public const string RockAndAHardPlaceText =
            "Once per turn, after you place one or more Rock tokens into one or more spaces adjacent " +
            "to one or more enemy heroes, each of those heroes discards a card, if able.";

        /// <summary>
        /// "each of those heroes discards a card, if able" - one run per hero, the hero arriving as the
        /// frame's opposing hero (the after-marker-given trigger's own shape, Bain's A Complicated
        /// Profession). "If able" is p.17's: an empty hand gives nothing.
        /// </summary>
        public static EffectDefinition RockAndAHardPlaceTriggeredEffect => LazyTriggeredEffect.Value;

        private static readonly Lazy<EffectDefinition> LazyTriggeredEffect = new Lazy<EffectDefinition>(() => new EffectDefinition(
            new EffectDefinitionId(RockAndAHardPlace.Value + "-ultimate"),
            ContentRevision,
            new EffectNodeId("hard-place-root"),
            new[]
            {
                new EffectNodeDefinition(
                    new EffectNodeId("hard-place-root"), EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("hard-place-discard") }),
                new EffectNodeDefinition(
                    new EffectNodeId("hard-place-discard"), EffectNodeKind.Capability,
                    EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticCardStateCapabilities.HeroParameter,
                            EffectRoleKind.OpposingHero,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                    })
            }));

        public static CardDefinition RockAndAHardPlaceDefinition => LazyUltimate.Value;

        private static readonly Lazy<CardDefinition> LazyUltimate = new Lazy<CardDefinition>(() => new CardDefinition(
            RockAndAHardPlace, ContentRevision, "Rock and a Hard Place", default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.AfterTokensPlacedAbility(
                "rock-and-a-hard-place-after-tokens-placed",
                "MrakRockAndAHardPlace",
                RockAndAHardPlaceTriggeredEffect.Reference,
                new AfterTokensPlacedTrigger(MrakComponentContent.RockId),
                Array.Empty<PrintedValueDefinition>()),
            CardSourceContent.Transcribed(RockAndAHardPlaceText)));

        /// <summary>Every triggered effect Mrak's content declares.</summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects() =>
            Array.AsReadOnly(new[] { RockAndAHardPlaceTriggeredEffect });

        // -------------------------------------------------------------------------------------------
        // Lesser Fissure - p.20's boxed Handicap
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId LesserFissure { get; } = Id("lesser-fissure");

        public const string LesserFissureText =
            "Target a hero adjacent to you. After the attack: Place a rock token in each of the " +
            "first three empty spaces in the straight line from you in the direction of the attack.";

        /// <summary>
        /// Gold Handicap: Initiative 10, Defense 2, Movement 1, Basic Attack 3. p.6 returns it to the
        /// box at setup and p.20 brings it back only for an odd player count, which no configuration in
        /// this build is - so its text is deferred to that configuration, exactly as every earlier
        /// hero's Handicap is.
        /// </summary>
        public static CardDefinition LesserFissureDefinition => LazyLesserFissure.Value;

        // Lazy for the static-init reason AllDefinitions records: this partial file's initialisers can
        // run before the other file's, whose action ids this face reads (measured in Unity at 8.112f -
        // "Action definition requires a defined ID" - where the dotnet harness's file order hid it).
        private static readonly Lazy<CardDefinition> LazyLesserFissure = new Lazy<CardDefinition>(() => new CardDefinition(
            LesserFissure, ContentRevision, "Lesser Fissure", new InitiativeValue(10),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    null,
                    PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                    PrintedDefenseResolution.None,
                    CardTextTiming.None),
                Movement(1),
                Defense(2)
            },
            null, CardSourceContent.Transcribed(LesserFissureText)));

        // -------------------------------------------------------------------------------------------
        // The set
        // -------------------------------------------------------------------------------------------

        /// <summary>His shard's nineteen rows, in the table's order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>p.6: the Gold and Silver basics and each colour's Tier I.</summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup =>
            LazyReturnedToBoxAtSetup.Value;

        /// <summary>
        /// Every printed face, in inventory order - the list the corpus sweeps walk. Lazy, so reading it
        /// never races the static initialisation of the face classes it joins (the static-init cycle
        /// Tali's dealing met).
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The eighteen a match deals from: everything but the boxed Handicap.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Mrak prints no card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Mrak's cards require a defined owner.", nameof(ownerId));
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
                Fissure, StoneGrip,
                MrakRedFaces.SeismicSlam, MrakRedFaces.SeismicAssault, MrakRedFaces.StoneCarapace,
                MrakRedFaces.Epicentre, MrakRedFaces.RockSolid,
                MrakBlueFaces.BoulderRush, MrakBlueFaces.BoulderBlitz, MrakBlueFaces.StompingStep,
                MrakBlueFaces.Boulderdozer, MrakBlueFaces.GroundShaker,
                MrakGreenFaces.TreacherousGround, MrakGreenFaces.Rockslide, MrakGreenFaces.RollingStone,
                MrakGreenFaces.Avalanche, MrakGreenFaces.StrollingStone,
                RockAndAHardPlace,
                LesserFissure
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                Fissure, StoneGrip, MrakRedFaces.SeismicSlam, MrakBlueFaces.BoulderRush,
                MrakGreenFaces.TreacherousGround
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[] { LesserFissure }));

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

        /// <summary>
        /// The composed faces, refused unless they are exactly the inventory table's nineteen - a face
        /// composed but never inventoried, or inventoried but composed by no block, is a load-time
        /// error rather than a silently short deck.
        /// </summary>
        private static IReadOnlyList<CardDefinition> Compose()
        {
            var composed = new[] { FissureDefinition, StoneGripDefinition }
                .Concat(MrakRedFaces.Definitions)
                .Concat(MrakBlueFaces.Definitions)
                .Concat(MrakGreenFaces.Definitions)
                .Concat(new[] { RockAndAHardPlaceDefinition, LesserFissureDefinition })
                .ToDictionary(definition => definition.Id);
            var missing = InventoryOrder.Where(id => !composed.ContainsKey(id)).ToArray();
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (missing.Length > 0 || uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Mrak's printed set is his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " + string.Join(", ", uninventoried.Select(id => id.Value)) +
                    "; in the table and composed by no block: " +
                    string.Join(", ", missing.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder.Select(id => composed[id]).ToArray());
        }
    }
}

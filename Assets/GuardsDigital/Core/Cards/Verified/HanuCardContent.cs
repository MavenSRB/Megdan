using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's nineteen printed faces as ONE set - milestone 8.71g, and the twenty-first
    /// hero this project builds.
    ///
    /// <para><b>What this file is and what the six partials beside it are.</b> His faces were composed
    /// by SIX blocks - 8.71a's Hurry up!, 8.71b's The Ultimate Trick, 8.71c's Gold basic and five Reds,
    /// 8.71d's three Blue journeys, 8.71e's two co-moves and 8.71f's five Greens - five of them running
    /// at once on five branches, each under a placeholder revision and each in its own partial file,
    /// because a C# partial class cannot merge one property from several files and an aggregate declared
    /// twice is a compile error rather than a merge. Those partials stay where they are and keep their
    /// own faces. What this file mints is the things that are true of the SET and could not be written
    /// until every face existed - the one real <see cref="ContentRevision"/>, the conventional action
    /// identities his whole set shares, <see cref="AllDefinitions"/> in his shard's own inventory order,
    /// the eighteen <see cref="DealtDefinitions"/>, p.6's <see cref="StartingHand"/> and
    /// <see cref="Deck"/>, the one card <see cref="ReturnedToBoxAtSetup"/>, and the boxed Handicap
    /// itself.</para>
    ///
    /// <para><b>The three coloured ladders are stated whole from this milestone on.</b> p.15 forks each
    /// colour twice - Helping Hand climbs to Even the Odds or Outnumber and either of those to Trusted
    /// Sidekick or Pile On; Unexpected Journey to There and Back Again or This Way! and either of those
    /// to Safe Travels or That Way!; Monkey Trick to Monkey Twist or Hear Nothing and either of those to
    /// Monkey Business or See Nothing - and THREE of those six Blue pairs crossed a block boundary,
    /// which is why 8.71d and 8.71e each left their own half unstated rather than naming an id a sibling
    /// branch owned. Every pair is complete in the partials now. The Red and the Green ladders never
    /// crossed a boundary and were whole from their own blocks. The faces on no ladder at all are his
    /// Gold and Silver basics, the boxed Handicap, and the Ultimate, which p.16 UNLOCKS rather than
    /// upgrades into.</para>
    ///
    /// <para><b>The revision is real from this milestone on.</b> <see cref="ContentRevision"/> reads
    /// <c>hanu-8-71g-rev-1</c> - every dealt hero's <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c>
    /// spelling - and replaces the six deliberately malformed placeholders the composition blocks
    /// carried between them. The project's placeholder rule is that a placeholder must not be a value
    /// that could be mistaken for a real one, which is why those strings are gone from the tree rather
    /// than left behind in a comment: every one of the nineteen definitions, every effect body they
    /// reach, the three Active-effect definitions his journeys create and the three end-of-turn bodies
    /// those arm now carry one string, which is what <c>HanuPrintedSetTests</c> asserts from both
    /// ends.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Hanu
    /// content, so no configured match can deal these eighteen cards and nothing reachable can start
    /// them. That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every snapshot's
    /// bytes and every corpus digest while the content never moved. 8.71h is the block that flips it,
    /// and until it does <c>HanuPrintedSetTests</c> asserts the gate from this side: the exact faces
    /// this build refuses, and the exact contracts they name.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// is transcribed from the extracted card faces at milestone 8.71's inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_HANU.md</c> shard. The table there and the rows in these partials are
    /// meant to be read side by side. Nothing is derived.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, and the one string every body of his is composed
        /// under. Minted at 8.71g, where the set became whole - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "hanu-8-71g-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself. Every definition id of his is prefixed with it.
        /// </summary>
        public const string ContentSetId = "hanu";

        // -----------------------------------------------------------------------------------------
        // The conventional action identities, declared once for his whole set.
        //
        // An action identity is LOCAL to its card - every hero in the roster prints "primary-skill" on
        // its coloured Skill faces - so these strings are the corpus's own and only the C# members are
        // his. Each composition block declared the ones its faces needed, because six partials of one
        // class being written at once cannot share a member without colliding on it; the wave merge
        // kept exactly one copy in HanuCardContent.CoMoves.cs and 8.71g moves it here, where the set's
        // own facts live.
        // -----------------------------------------------------------------------------------------

        /// <summary>p.9's Gold band with a non-Ranged Attack primary - Fight and Flight, and the boxed
        /// Handicap under HA-05's mislabelled band.</summary>
        public static ActionDefinitionId PrimaryBasicAttack { get; } =
            new ActionDefinitionId("primary-basic-attack");

        /// <summary>
        /// p.9's Silver band with a Skill primary - "BASIC SKILL - RANGED", which is Hurry up! alone. It
        /// is a separate local identity from <see cref="PrimarySkill"/> for the reason every earlier set
        /// keeps its basic and non-basic primaries apart: p.15's level-up replaces coloured cards and
        /// never a basic, so the two are different slots on the dashboard rather than two printings of
        /// one.
        /// </summary>
        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        /// <summary>p.9's Red band with a Ranged Attack primary - all five of his Reds.</summary>
        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        /// <summary>The coloured Skill primary - his five Blues and his five Greens.</summary>
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        /// <summary>p.9's Movement ribbon, whose own card text p.11 suppresses on every face of his.</summary>
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>p.14's numeric Defense ribbon, spent by discarding the card from hand.</summary>
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        // -----------------------------------------------------------------------------------------
        // p.20's boxed Handicap, which is this milestone's own face.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His Gold Handicap - rulebook p.20's card. Initiative 12, secondary Defense 1, secondary
        /// Movement 1, and an Attack of 1 with no Range icon at all.
        ///
        /// <para>Inventory rather than deck content: p.6's setup returns it to the box and p.20 brings
        /// it back only for an odd player count, which is why it is the one card in
        /// <see cref="ReturnedToBoxAtSetup"/>. Its printed values, words, colour and both secondary
        /// icons ship here all the same - what the established deferral defers is the COMPOSITION of
        /// its text, not the card.</para>
        /// </summary>
        public static CardDefinitionId FightOrFlight { get; } =
            new CardDefinitionId("hanu-fight-or-flight");

        /// <summary>
        /// Fight or Flight's printed sentence, transcribed. It is Fight and Flight's with "a unit"
        /// narrowed to "a hero" in the first clause, which is the only thing the two faces' words
        /// differ in - and the narrowing is inside the deferral rather than composed here.
        /// </summary>
        public const string FightOrFlightText =
            "Target a hero adjacent to you. If the target is not defeated, After the attack: If able, " +
            "move 3 spaces in a straight line.";

        // -----------------------------------------------------------------------------------------
        // The set.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His shard's printed-inventory table, as identities in its own row order: the Gold basic, the
        /// Silver basic, the five Reds, the five Blues in the order the table prints them (the three
        /// journeys and the two co-moves interleaved by Tier), the five Greens, the Purple Ultimate and
        /// p.20's Handicap last.
        ///
        /// <para>It is stated rather than derived because the order is a FACT about the source document,
        /// and <see cref="AllDefinitions"/> is built from it - so a face composed by a block and left
        /// out of the table, or a table row naming a face nobody composed, is a loud failure rather than
        /// a silently short list.</para>
        ///
        /// <para><b>Every list on this page is lazy, and for a reason milestone 8.61i paid for
        /// once.</b> A C# static field initializer runs in textual order within one file, and the order
        /// ACROSS the files of a partial class is the compiler's. Every id below but
        /// <see cref="FightOrFlight"/> is declared in another partial of this class, so an eager list
        /// here reads whichever of them the compiler had not reached yet as an UNDEFINED id - which is
        /// the static-initialization trap milestone 8.37 paid 1,423 failing cases for and 8.38d wrote
        /// the convention against.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>
        /// p.6's five starting cards - one of each colour a hero holds at setup: his Gold basic, his
        /// Silver basic, and the Tier I of each of Red, Blue and Green.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and p.16's
        /// unlock: the six Tier II faces, the six Tier III faces, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is exactly why
        /// <see cref="DealtDefinitions"/> is eighteen where <see cref="AllDefinitions"/> is nineteen.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup =>
            LazyReturnedToBoxAtSetup.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyInventoryOrder =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                FightAndFlight, HurryUp,
                HelpingHand, EvenTheOdds, Outnumber, TrustedSidekick, PileOn,
                UnexpectedJourney, ThereAndBackAgain, ThisWay, SafeTravels, ThatWay,
                MonkeyTrick, MonkeyTwist, HearNothing, MonkeyBusiness, SeeNothing,
                TheUltimateTrick,
                FightOrFlight
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                FightAndFlight, HurryUp, HelpingHand, UnexpectedJourney, MonkeyTrick
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                EvenTheOdds, Outnumber, TrustedSidekick, PileOn,
                ThereAndBackAgain, ThisWay, SafeTravels, ThatWay,
                MonkeyTwist, HearNothing, MonkeyBusiness, SeeNothing,
                TheUltimateTrick
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(
                () => Array.AsReadOnly(new[] { FightOrFlight }));

        /// <summary>
        /// All nineteen faces, in his shard's own inventory order.
        ///
        /// <para>Built lazily, which is the convention milestone 8.38d adopted after a
        /// static-initialization cycle cost milestone 8.37 1,423 failing cases on one
        /// <c>NullReferenceException</c> and then cost a later session <c>NUnit failed to load</c> with
        /// zero tests discovered while <c>dotnet build</c> reported no errors at all.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>
        /// The eighteen definitions a match deals from - everything but the boxed Handicap card.
        ///
        /// <para>It was deliberately EMPTY from 8.71a until this milestone, and both halves of that were
        /// measured: <c>ConfiguredCardArtworkCatalogTests</c> demands the member exist at all, and
        /// <c>DealtCorpusCompositionValidationTests</c> validates every face it returns against the
        /// capabilities the build REGISTERS - so a composed-but-unregistered face inside it was a red.
        /// 8.57f's precedent is what makes the eighteen sayable here: that case validates against the
        /// registration list PLUS every declaration the build deliberately withholds, which is the only
        /// reading that covers a composition milestone at all. The registration debt is asserted from
        /// the other side, by <c>HanuPrintedSetTests</c>, which names the exact faces this build still
        /// refuses and the exact contracts they name.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>One of his nineteen printed faces, by identity.</summary>
        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Hanu prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(
            HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Hanu's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix " +
                    "that distinguishes one seat's cards from another's.",
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

        /// <summary>
        /// One seat's whole setup as a hand a configured match can be composed from: p.6's five in hand,
        /// the thirteen its deck keeps, and the eighteen definitions both draw on.
        /// </summary>
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(Compose);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        /// <summary>
        /// Every face the six composition blocks declare, plus this milestone's own Handicap, gathered
        /// into the shard's inventory order. A face a block composed and the table does not name, or a
        /// table row no block composes, is refused here rather than quietly dropped from his set.
        /// </summary>
        private static IReadOnlyList<CardDefinition> Compose()
        {
            var composed = ComposedFaces()
                .GroupBy(definition => definition.Id)
                .ToDictionary(group => group.Key, group => group.First());

            var missing = InventoryOrder.Where(id => !composed.ContainsKey(id)).ToArray();
            var uninventoried = composed.Keys.Where(id => !InventoryOrder.Contains(id)).ToArray();
            if (missing.Length > 0 || uninventoried.Length > 0)
            {
                throw new InvalidOperationException(
                    "Hanu's printed set is his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " +
                    string.Join(", ", uninventoried.Select(id => id.Value)) +
                    "; in the table and composed by no block: " +
                    string.Join(", ", missing.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder.Select(id => composed[id]).ToArray());
        }

        /// <summary>The six blocks' own lists, each still declared by the partial that owns it.</summary>
        private static IEnumerable<CardDefinition> ComposedFaces() =>
            RedDefinitions()
                .Concat(HurryUpDefinitions())
                .Concat(JourneyDefinitions())
                .Concat(CoMoveDefinitions())
                .Concat(GreenDefinitions())
                .Concat(UltimateDefinitions())
                .Concat(new[] { BuildFightOrFlight() });

        /// <summary>
        /// Fight or Flight, p.20's boxed Handicap - the one face milestone 8.71g composes itself,
        /// because it belongs to no block's clause and is dealt by nothing.
        ///
        /// <para><b>Its band is read as a printed Attack under a mislabelled ribbon (HA-05).</b> The
        /// card prints the words "Basic Skill" over an ATTACK value of 1 and no Range icon at all, and
        /// its sentence is Fight and Flight's "Target a hero adjacent to you ..." - the mirror of that
        /// face's "Basic Attack". A Skill has no Attack value to print, so the band is a printing slip
        /// and the action is the adjacent Attack the number and the words both describe. Nothing runs on
        /// it either way: p.6 returns the card to the box.</para>
        ///
        /// <para><b>The parked face composes no body at all</b>, and its whole printed sentence is what
        /// the established deferral covers - the "Target a HERO" its first clause narrows to included,
        /// which is why this row declares no hero-only narrowing, no after-attack rider and no straight
        /// line. Composing half a card is what every earlier Handicap refused to do (Arien's Ceremonial
        /// Blade, Widget's Follow Along, Swift's Backup, Bain's Side Quest, Rowenna's Earn the Spurs,
        /// Min's Chop Down and Brynn's Home Turf each carry the same string), and doing it here would
        /// make Hanu the one hero whose boxed face is partly live. A face that shipped a shortened
        /// version of itself is the fault <c>BorrowedCardTextRefusal.PrintedTextNotComposed</c> exists
        /// to refuse one layer up.</para>
        /// </summary>
        private static CardDefinition BuildFightOrFlight() => new CardDefinition(
            FightOrFlight,
            ContentRevision,
            "Fight or Flight",
            new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryBasicAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    null,
                    PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                    PrintedDefenseResolution.None,
                    CardTextTiming.None),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(FightOrFlightText));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's nineteen printed faces as ONE set - milestone 8.69f, and the twentieth hero
    /// this project builds.
    ///
    /// <para><b>What this file is and what the five partials beside it are.</b> His faces were composed
    /// by FIVE blocks - 8.69a's Familiar Ground, High Ground, Elevated Ambush and Over the Top, 8.69b's
    /// three split Reds, 8.69c's Decoy and two guides, 8.69d's three tracking swaps and 8.69e's five
    /// Greens - four of them running at once on four branches, each under a placeholder revision and
    /// each in its own partial file, because a C# partial class cannot merge one property from several
    /// files and an aggregate declared twice is a compile error rather than a merge. Those partials stay
    /// where they are and keep their own faces. What this file mints is the things that are true of the
    /// SET and could not be written until every face existed - the one real <see cref="ContentRevision"/>,
    /// the conventional action identities his whole set shares, <see cref="AllDefinitions"/> in his
    /// shard's own inventory order, the eighteen <see cref="DealtDefinitions"/>, p.6's
    /// <see cref="StartingHand"/> and <see cref="Deck"/>, the one card
    /// <see cref="ReturnedToBoxAtSetup"/>, and the boxed Handicap itself.</para>
    ///
    /// <para><b>The three coloured ladders are stated whole from this milestone on.</b> p.15 forks each
    /// colour twice - High Ground climbs to Elevated Ambush or Split Attack and either of those to Peak
    /// Precision or Split Throw; Tread Lightly to Cover Tracks or Mountain Guide and either of those to
    /// Hide Traces or Expedition Leader; Bear Trap to Log Trap or True Grit and either of those to
    /// Deadfall Trap or Die Hard - and four of those six pairs crossed a block boundary, which is why
    /// 8.69a, 8.69c and 8.69d each left their own half unstated rather than naming an id a sibling
    /// branch owned. Every pair is complete in the partials now. The three faces on no ladder at all
    /// are his Gold and Silver basics, the boxed Handicap, and the Ultimate, which p.16 UNLOCKS rather
    /// than upgrades into.</para>
    ///
    /// <para><b>The revision is real from this milestone on.</b> <see cref="ContentRevision"/> reads
    /// <c>brynn-8-69f-rev-1</c> - every dealt hero's <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c>
    /// spelling - and replaces the five deliberately malformed placeholders the composition blocks
    /// carried between them. The project's placeholder rule is that a placeholder must not be a value
    /// that could be mistaken for a real one, which is why those strings are gone from the tree rather
    /// than left behind in a comment: every one of the nineteen definitions and every effect body they
    /// reach now carries one string, which is what <c>BrynnPrintedSetTests</c> asserts from both
    /// ends.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Brynn
    /// content, so no configured match can deal these eighteen cards and nothing reachable can start
    /// them. That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved. 8.69g is the block that
    /// flips it, and until it does <c>BrynnPrintedSetTests</c> asserts the gate from this side: the
    /// exact faces this build refuses, and the exact contracts they name.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// is transcribed from the extracted card faces at milestone 8.69's inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_BRYNN.md</c> shard. The table there and the rows in these partials are
    /// meant to be read side by side. Nothing is derived.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, and the one string every effect body of his is
        /// composed under. Minted at 8.69f, where the set became whole - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "brynn-8-69f-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself. Every definition id of his is prefixed with it.
        /// </summary>
        public const string ContentSetId = "brynn";

        // -----------------------------------------------------------------------------------------
        // The conventional action identities, declared once for his whole set.
        //
        // An action identity is LOCAL to its card - every hero in the roster prints "primary-attack"
        // on its Attack faces - so these strings are the corpus's own and only the C# members are his.
        // Each composition block declared the ones its faces needed, because five partials of one
        // class being written at once cannot share a member without colliding on it; 8.69f folds them
        // into one declaration per identity, which is the fold the blocks' own remarks asked for.
        // -----------------------------------------------------------------------------------------

        /// <summary>p.9's Gold band with a Ranged Attack primary - "BASIC ATTACK - RANGED".</summary>
        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        /// <summary>p.9's Red band with an Attack primary - four of his five Reds.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        /// <summary>
        /// p.9's Silver band with a Skill primary - "BASIC SKILL", which is Decoy alone. It is a
        /// separate local identity from <see cref="PrimarySkill"/> for the reason every earlier set
        /// keeps its basic and non-basic primaries apart: p.15's level-up replaces coloured cards and
        /// never a basic, so the two are different slots on the dashboard rather than two printings of
        /// one.
        /// </summary>
        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        /// <summary>The coloured Skill primary - his three Blue swaps, two guides and five Greens.</summary>
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
        /// His Gold Handicap - rulebook p.20's card. Initiative 11, secondary Defense 1, secondary
        /// Movement 1, Attack 2 and Range 2.
        ///
        /// <para>Inventory rather than deck content: p.6's setup returns it to the box and p.20 brings
        /// it back only for an odd player count, which is why it is the one card in
        /// <see cref="ReturnedToBoxAtSetup"/>. Its printed values, words, colour and both secondary
        /// icons ship here all the same - what the established deferral defers is the COMPOSITION of
        /// its text, not the card.</para>
        /// </summary>
        public static CardDefinitionId HomeTurf { get; } = new CardDefinitionId("brynn-home-turf");

        /// <summary>
        /// Home Turf's printed sentence, transcribed. It is Familiar Ground's with "a unit" narrowed to
        /// "a hero" in the first bullet, which is the only thing the two faces' words differ in.
        /// </summary>
        public const string HomeTurfText =
            "Choose one - • Target a hero adjacent to you. • Target a hero in range who is adjacent " +
            "to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles ).";

        // -----------------------------------------------------------------------------------------
        // The set.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His shard's printed-inventory table, as identities in its own row order: the Gold basic, the
        /// Silver basic, the five Reds, the five Blues, the five Greens, the Purple Ultimate and p.20's
        /// Handicap last.
        ///
        /// <para>It is stated rather than derived because the order is a FACT about the source
        /// document, and <see cref="AllDefinitions"/> is built from it - so a face composed by a block
        /// and left out of the table, or a table row naming a face nobody composed, is a loud failure
        /// rather than a silently short list.</para>
        ///
        /// <para><b>Every list on this page is lazy, and for a reason milestone 8.61i paid for
        /// once.</b> A C# static field initializer runs in textual order within one file, and the order
        /// ACROSS the files of a partial class is the compiler's. Every id below is declared in another
        /// partial of this class, so an eager list here reads whichever of them the compiler had not
        /// reached yet as an UNDEFINED id - which is the static-initialization trap milestone 8.37 paid
        /// 1,423 failing cases for and 8.38d wrote the convention against.</para>
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
                FamiliarGround, Decoy,
                HighGround, ElevatedAmbush, SplitAttack, PeakPrecision, SplitThrow,
                TreadLightly, CoverTracks, MountainGuide, HideTraces, ExpeditionLeader,
                BearTrap, LogTrap, TrueGrit, DeadfallTrap, DieHard,
                OverTheTop,
                HomeTurf
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                FamiliarGround, Decoy, HighGround, TreadLightly, BearTrap
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                ElevatedAmbush, SplitAttack, PeakPrecision, SplitThrow,
                CoverTracks, MountainGuide, HideTraces, ExpeditionLeader,
                LogTrap, TrueGrit, DeadfallTrap, DieHard,
                OverTheTop
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(
                () => Array.AsReadOnly(new[] { HomeTurf }));

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
        /// <para>It was deliberately EMPTY from 8.69a until this milestone, and both halves of that
        /// were measured: <c>ConfiguredCardArtworkCatalogTests</c> demands the member exist at all, and
        /// <c>DealtCorpusCompositionValidationTests</c> validates every face it returns against the
        /// capabilities the build REGISTERS - so a composed-but-unregistered face inside it was a red.
        /// 8.57f's precedent is what makes the eighteen sayable here: that case validates against the
        /// registration list PLUS every declaration the build deliberately withholds, which is the only
        /// reading that covers a composition milestone at all. The registration debt is asserted from
        /// the other side, by <c>BrynnPrintedSetTests</c>, which names the exact faces this build still
        /// refuses and the exact contracts they name.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>One of his nineteen printed faces, by identity.</summary>
        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Brynn prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(
            HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Brynn's cards require a defined owner.", nameof(ownerId));
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
        /// One seat's whole setup as a hand a configured match can be composed from: p.6's five in
        /// hand, the thirteen its deck keeps, and the eighteen definitions both draw on.
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
        /// Every face the five composition blocks declare, plus this milestone's own Handicap, gathered
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
                    "Brynn's printed set is his shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " +
                    string.Join(", ", uninventoried.Select(id => id.Value)) +
                    "; in the table and composed by no block: " +
                    string.Join(", ", missing.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder.Select(id => composed[id]).ToArray());
        }

        /// <summary>The five blocks' own lists, each still declared by the partial that owns it.</summary>
        private static IEnumerable<CardDefinition> ComposedFaces() =>
            ObstacleDefinitions
                .Concat(SplitAttackFaces())
                .Concat(GuidesDefinitions)
                .Concat(SwapDefinitions)
                .Concat(TrapAndGritDefinitions)
                .Concat(new[] { BuildHomeTurf() });

        /// <summary>
        /// Home Turf, p.20's boxed Handicap - the one face milestone 8.69f composes itself, because it
        /// belongs to no block's clause and is dealt by nothing.
        ///
        /// <para><b>Its band is read as a printed Attack under a mislabelled ribbon (BR-05).</b> The
        /// card prints the words "Basic Skill - Ranged" over an ATTACK value of 2 and a Range icon of
        /// 2, and its sentence is Familiar Ground's "Target a hero ..." - the mirror of that face's
        /// "Basic Attack - Ranged". A Skill has no Attack value to print and reaches nobody through a
        /// Range icon, so the band is a printing slip and the action is the Ranged Attack the numbers
        /// and the words both describe. Nothing runs on it either way: p.6 returns the card to the box.
        /// </para>
        ///
        /// <para><b>The parked face composes no body at all</b>, and its whole printed sentence is what
        /// the established deferral covers - the "Target a HERO" its first bullet narrows to included,
        /// which is why this row declares no hero-only narrowing and no modal. Composing half a card is
        /// what every earlier Handicap refused to do (Arien's Ceremonial Blade, Widget's Follow Along,
        /// Swift's Backup, Bain's Side Quest, Rowenna's Earn the Spurs and Min's Chop Down each carry
        /// the same string), and doing it here would make Brynn the one hero whose boxed face is partly
        /// live. A face that shipped a shortened version of itself is the fault
        /// <c>BorrowedCardTextRefusal.PrintedTextNotComposed</c> exists to refuse one layer up.</para>
        /// </summary>
        private static CardDefinition BuildHomeTurf() => new CardDefinition(
            HomeTurf,
            ContentRevision,
            "Home Turf",
            new InitiativeValue(11),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryRangedAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
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
            CardSourceContent.Transcribed(HomeTurfText));
    }
}

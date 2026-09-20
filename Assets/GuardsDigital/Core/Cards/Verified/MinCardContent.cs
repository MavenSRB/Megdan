using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's nineteen printed faces as ONE set - milestone 8.61i, and the nineteenth
    /// hero this project builds.
    ///
    /// <para><b>What this file is and what the eight partials beside it are.</b> Her faces were
    /// composed by EIGHT blocks running at once - 8.61a's Smoke Bomb, 8.61b's three Blocks, 8.61c's
    /// five Stances, 8.61d's three mine faces, 8.61f's two Grenades, 8.61g's Gold pair and 8.61h's
    /// Inner Strength, Perfect Self and Flurry of Blows - each under a placeholder revision and each in
    /// its own partial file, because a C# partial class cannot merge one property from several files
    /// and an aggregate declared twice is a compile error rather than a merge. Those partials stay
    /// where they are and keep their own faces: what this file mints is the things that are true of the
    /// SET and could not be written until every face existed - the one real
    /// <see cref="ContentRevision"/>, the conventional action identities her whole set shares,
    /// <see cref="AllDefinitions"/> in her shard's own inventory order, the eighteen
    /// <see cref="DealtDefinitions"/>, p.6's <see cref="StartingHand"/> and <see cref="Deck"/>, and the
    /// one card <see cref="ReturnedToBoxAtSetup"/>.</para>
    ///
    /// <para><b>The three coloured ladders are stated whole here, and only here.</b> p.15 forks each
    /// colour twice - Crane Stance climbs to Tiger or Viper Stance and either of those to Dragon or
    /// Cobra Stance; Trip Mine climbs to Cluster Mine or Death Grenade and either of those to Mine
    /// Field or Holy Death Grenade; Poof! climbs to Vanish or Inner Strength and either of those to
    /// Ruse or Perfect Self - and four of those six pairs cross a block boundary, which is why 8.61d,
    /// 8.61f, 8.61b and 8.61h each left their own half unstated rather than naming an id a sibling
    /// branch owned. Every pair is complete in the partials now; the two faces on no ladder at all are
    /// her two Gold basics, and the Ultimate, which p.16 UNLOCKS rather than upgrades into.</para>
    ///
    /// <para><b>The revision is real from this milestone on.</b> <see cref="ContentRevision"/> reads
    /// <c>min-8-61i-rev-1</c> - every dealt hero's <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c>
    /// spelling - and replaces the seven deliberately malformed placeholders the composition blocks
    /// carried. The project's placeholder rule is that a placeholder must not be a value that could be
    /// mistaken for a real one, which is why those strings are gone from the tree rather than left
    /// behind in a comment: every one of the nineteen definitions and every effect body they reach now
    /// carries one string, which is what <c>MinPrintedSetTests</c> asserts from both ends.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Min
    /// content, so no configured match can deal these eighteen cards and nothing reachable can start
    /// them. That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved. 8.61j is the block that
    /// flips it, and until it does <c>MinPrintedSetTests</c> asserts the gate from this side: the exact
    /// faces this build refuses, and the exact contracts they name.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// is transcribed from the extracted card faces at milestone 8.61's inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_MIN.md</c> shard. The table there and the rows in these partials are
    /// meant to be read side by side. Nothing is derived.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, and the one string every effect body of hers is
        /// composed under. Minted at 8.61i, where the set became whole - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "min-8-61i-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself. Every definition id of hers is prefixed with it.
        /// </summary>
        public const string ContentSetId = "min";

        // -----------------------------------------------------------------------------------------
        // The conventional action identities, declared once for her whole set.
        //
        // An action identity is LOCAL to its card - every hero in the roster prints "primary-attack"
        // on its Attack faces - so these strings are the corpus's own and only the C# members are
        // hers. Each composition block declared the ones its faces needed under a block-scoped member
        // name, because five partials of one class being written at once cannot share a member without
        // colliding on it; 8.61i folds them into one declaration per identity, which is the fold the
        // blocks' own remarks asked for.
        // -----------------------------------------------------------------------------------------

        /// <summary>p.9's Red band with an Attack primary - her five Stances.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        /// <summary>p.9's Gold band with a Ranged Attack primary - "BASIC ATTACK - RANGED".</summary>
        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        /// <summary>
        /// p.9's Silver band with a Skill primary - "BASIC SKILL". It is a separate local identity from
        /// <see cref="PrimarySkill"/> for the reason every earlier set keeps its basic and non-basic
        /// primaries apart: p.15's level-up replaces coloured cards and never a basic, so the two are
        /// different slots on the dashboard rather than two printings of one.
        /// </summary>
        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        /// <summary>The coloured Skill primary - her three mine faces, both Grenades and two Greens.</summary>
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.14's Block, printed as the card's PRIMARY action - Poof!, Vanish and Ruse. A Block is
        /// performed as a reaction and printed as the primary, which is why the three faces that carry
        /// one declare no <see cref="SecondaryDefense"/> at all.
        /// </summary>
        public static ActionDefinitionId PrimaryBlock { get; } =
            new ActionDefinitionId("primary-block");

        /// <summary>p.9's Movement ribbon, whose own card text p.11 suppresses.</summary>
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>p.14's numeric Defense ribbon, spent by discarding the card from hand.</summary>
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        // -----------------------------------------------------------------------------------------
        // The set.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her shard's printed-inventory table, as identities in its own row order: the Gold basic, the
        /// Silver basic, the five Reds, the five Blues, the five Greens, the Purple Ultimate and p.20's
        /// Handicap last.
        ///
        /// <para>It is stated rather than derived because the order is a FACT about the source
        /// document, and <see cref="AllDefinitions"/> is built from it - so a face composed by a block
        /// and left out of the table, or a table row naming a face nobody composed, is a loud failure
        /// rather than a silently short list.</para>
        ///
        /// <para><b>Every list on this page is lazy, and for a reason this milestone paid for
        /// once.</b> A C# static field initializer runs in textual order within one file, and the
        /// order ACROSS the files of a partial class is the compiler's. Nine of the ids below are
        /// declared in other partials of this class, so an eager list here reads whichever of them the
        /// compiler had not reached yet as an UNDEFINED id - which is the static-initialization trap
        /// milestone 8.37 paid 1,423 failing cases for and 8.38d wrote the convention against.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;

        /// <summary>
        /// p.6's five starting cards - one of each colour a hero holds at setup: her Gold basic, her
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
                FastAsLightning, SmokeBomb,
                CraneStance, TigerStance, ViperStance, DragonStance, CobraStance,
                TripMine, ClusterMine, DeathGrenade, MineField, HolyDeathGrenade,
                Poof, Vanish, InnerStrength, Ruse, PerfectSelf,
                FlurryOfBlows,
                ChopDown
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                FastAsLightning, SmokeBomb, CraneStance, TripMine, Poof
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                TigerStance, ViperStance, DragonStance, CobraStance,
                ClusterMine, DeathGrenade, MineField, HolyDeathGrenade,
                Vanish, InnerStrength, Ruse, PerfectSelf,
                FlurryOfBlows
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyReturnedToBoxAtSetup =
            new Lazy<IReadOnlyList<CardDefinitionId>>(
                () => Array.AsReadOnly(new[] { ChopDown }));

        /// <summary>
        /// All nineteen faces, in her shard's own inventory order.
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
        /// <para>It was deliberately EMPTY from 8.61a until this milestone, and both halves of that
        /// were measured: <c>ConfiguredCardArtworkCatalogTests</c> demands the member exist at all, and
        /// <c>DealtCorpusCompositionValidationTests</c> validates every face it returns against the
        /// capabilities the build REGISTERS - so a composed-but-unregistered face inside it was a red.
        /// 8.57f's precedent is what makes the eighteen sayable here: that case validates against the
        /// registration list PLUS every declaration the build deliberately withholds, which is the only
        /// reading that covers a composition milestone at all. The registration debt is asserted from
        /// the other side, by <c>MinPrintedSetTests</c>, which names the exact faces this build still
        /// refuses and the exact contracts they name.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        /// <summary>One of her nineteen printed faces, by identity.</summary>
        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Min prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(
            HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Min's cards require a defined owner.", nameof(ownerId));
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

        /// <summary>Her content-set identity spelled into a definition id.</summary>
        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(Compose);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        /// <summary>
        /// Every face the eight composition blocks declare, gathered into the shard's inventory order.
        /// A face a block composed and the table does not name, or a table row no block composes, is
        /// refused here rather than quietly dropped from her set.
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
                    "Min's printed set is her shard's nineteen-row table and nothing else. Composed " +
                    "but not in the table: " +
                    string.Join(", ", uninventoried.Select(id => id.Value)) +
                    "; in the table and composed by no block: " +
                    string.Join(", ", missing.Select(id => id.Value)) + ".");
            }

            return Array.AsReadOnly(InventoryOrder.Select(id => composed[id]).ToArray());
        }

        /// <summary>The eight blocks' own lists, each still declared by the partial that owns it.</summary>
        private static IEnumerable<CardDefinition> ComposedFaces() =>
            BorrowedRiderDefinitions
                .Concat(new[] { SmokeBombDefinition() })
                .Concat(StanceDefinitions)
                .Concat(MineDefinitions())
                .Concat(GrenadeCards().Select(GrenadeDefinition))
                .Concat(VanishingDefinitions)
                .Concat(DisciplineDefinitions);
    }
}

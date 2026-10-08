using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 9.32a. A turn slot holding a card placed facedown (Takahide's Bushido, p.10), as a
    /// seat may see it. A facedown card is hidden information (p.4), and a card instance id names its
    /// definition, so every seat but the owner's is shown the slot with the card withheld - a card is
    /// there, which every player can see, and which card is not. The current round's slots and the
    /// completed-round history are masked alike, because the history outlives the card's own facedown
    /// state (it returns to the hand at round end).
    ///
    /// <para>A projection filter and never an authorization one, as <see cref="FacedownTokenMask"/> is:
    /// the authority, the Director's read model, the snapshot and the replay keep the identity.</para>
    /// </summary>
    /// <summary>
    /// Milestone 9.32g. Whether the bounded public log still remembers a hand card's face (p.8: a
    /// retrieved card stays known at the table): some row names it, and no row at or after the last
    /// one that names it moved it into its owner's deck, where p.4 hides it again. A gold the table
    /// saw go into the deck and come back out unseen - Bushido swapping the gold in hand for one of
    /// the deck's two - is no longer named to another seat. Ready for War's two golds are withheld
    /// the same way, although a seat that tracked every gold could tell which they were: where the
    /// table's knowledge depends on counting, the projection errs on the side of withholding.
    /// </summary>
    internal static class RetrievedCardMemory
    {
        internal static bool Remembers(
            CardInstance card, IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> publicLog)
        {
            var log = publicLog ?? Array.Empty<GuardsDigital.Core.Diagnostics.MatchLogEntry>();
            var named = log.Where(entry => entry.SubjectCardInstanceId.Equals(card.Id))
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            if (named == 0)
            {
                return false;
            }

            var definition = card.DefinitionReference.DefinitionId;
            var intoDeck = log.Where(entry => entry.SubjectHeroId.Equals(card.OwnerId) &&
                    entry.CardMoves.Any(move => move.To == GuardsDigital.Core.Effects.CardMoveZone.Deck &&
                        move.Definition.Equals(definition)))
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            return named > intoDeck;
        }
    }

    internal static class CardMoveMask
    {
        /// <summary>
        /// Milestone 9.32g. The log as <paramref name="seat"/> may read it: a row's card moves are
        /// left whole on the seat that owns the row's hero, and on every other seat each move the table
        /// did not see (<see cref="GuardsDigital.Core.Effects.CardMoveFact.IsSeenByTheTable()"/>) loses its
        /// card. The same list is returned where nothing is hidden, so the composition stays an
        /// identity for every match without a swap.
        /// </summary>
        internal static IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> Mask(
            IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> log, MatchSeat seat)
        {
            if (log == null || !log.Any(entry => Hides(entry, seat)))
            {
                return log;
            }

            return Array.AsReadOnly(log
                .Select(entry => Hides(entry, seat)
                    ? entry.WithCardMoves(entry.CardMoves.Select(move =>
                        move.IsSeenByTheTable() ? move : move.Unnamed()))
                    : entry)
                .ToArray());
        }

        private static bool Hides(GuardsDigital.Core.Diagnostics.MatchLogEntry entry, MatchSeat seat) =>
            entry.CardMoves.Any(move => !move.IsSeenByTheTable() && move.Definition.IsDefined) &&
            !(seat != null && seat.Owns(entry.SubjectHeroId));
    }

    internal static class FacedownSlotMask
    {
        internal static IReadOnlyList<RoundTurnSlot> Mask(
            IReadOnlyList<RoundTurnSlot> slots, MatchSeat seat,
            IReadOnlyList<CardInstance> cards, MatchParticipants participants,
            IReadOnlyList<CardDefinition> definitions = null)
        {
            if (slots == null || slots.Count == 0 || !slots.Any(slot => Hides(slot, seat)))
            {
                return slots;
            }

            return Array.AsReadOnly(slots
                .Select(slot => Hides(slot, seat) ? MaskSlot(slot, seat, cards, participants, definitions) : slot)
                .ToArray());
        }

        internal static IReadOnlyList<CompletedRoundRecord> MaskRounds(
            IReadOnlyList<CompletedRoundRecord> rounds, MatchSeat seat,
            IReadOnlyList<CardInstance> cards, MatchParticipants participants,
            IReadOnlyList<CardDefinition> definitions = null)
        {
            if (rounds == null || rounds.Count == 0 ||
                !rounds.Any(round => round.TurnSlots.Any(slot => Hides(slot, seat))))
            {
                return rounds;
            }

            return Array.AsReadOnly(rounds
                .Select(round => round.TurnSlots.Any(slot => Hides(slot, seat))
                    ? new CompletedRoundRecord(
                        round.RoundIndex,
                        Mask(round.TurnSlots, seat, cards, participants, definitions),
                        round.EndOfRoundOutcomes)
                    : round)
                .ToArray());
        }

        private static bool Hides(RoundTurnSlot slot, MatchSeat seat) =>
            slot.Assignments.Any(assignment => Hides(assignment, seat));

        private static bool Hides(TurnCardAssignment assignment, MatchSeat seat) =>
            assignment.IsFacedown && assignment.CardInstanceId.HasValue &&
            !(seat != null && seat.Owns(assignment.HeroId));

        private static RoundTurnSlot MaskSlot(
            RoundTurnSlot slot, MatchSeat seat,
            IReadOnlyList<CardInstance> cards, MatchParticipants participants,
            IReadOnlyList<CardDefinition> definitions) =>
            new RoundTurnSlot(
                slot.RoundIndex,
                slot.TurnIndex,
                slot.Assignments.Select(assignment => Hides(assignment, seat)
                    ? TurnCardAssignment.Withheld(
                        assignment.HeroId,
                        KnownColorOf(cards?.FirstOrDefault(card => card.Id.Equals(assignment.CardInstanceId.Value)),
                            definitions))
                    : assignment),
                cards,
                participants);

        /// <summary>
        /// Milestone 9.32g. The colour every seat knows a facedown resting card has, though not which
        /// card it is: the one producer of such a card, <c>swap-owned-color-card-with-deck-card@1</c>
        /// (Bushido), swaps for a card of the colour its own text names, so that colour is table
        /// knowledge (the user's 2026-10-02 ruling: "it can't be anything else"). Read off the
        /// definition the host holds; <see cref="CardColor.None"/> where it cannot be read. Asked of a
        /// card the slot RECORDED as facedown, so a completed round keeps its colour after p.8 has
        /// returned the card faceup to the hand. p.10 gives the card no colour for any rule, and no rule
        /// reads this.
        /// </summary>
        internal static CardColor KnownColorOf(CardInstance card, IReadOnlyList<CardDefinition> definitions) =>
            card == null || definitions == null
                ? CardColor.None
                : definitions.FirstOrDefault(definition => definition.Reference.Equals(card.DefinitionReference))
                    ?.Progression.Color ?? CardColor.None;
    }

    /// <summary>
    /// Milestone 8.61d. Rulebook p.4's facedown component, applied to one seat's view of the board.
    ///
    /// <para><b>What the source says.</b> Min the Dragonmonk's three Blue faces place their mines
    /// "facedown in radius", and her pile holds two kinds - a blast and a dud - that differ only in
    /// what happens when one is removed. p.4 makes a facedown component hidden information: the hero
    /// who placed it knows which is which and nobody else does, until the mine is removed and 8.61e
    /// reveals it.</para>
    ///
    /// <para><b>It is a projection filter and never an authorization filter</b> - milestone 8.40f's
    /// rule. The authority, the Director's read model, the snapshot, the restore and the replay all
    /// carry the real definitions and are untouched here; what changes is the object one seat is
    /// handed. So an Undo, a resync and a reconnect re-derive the mask from authoritative state rather
    /// than having to remember it, exactly as the narrowed effect frame does.</para>
    ///
    /// <para><b>The wire keeps its shape.</b> <c>NetworkTokenRow</c> already writes the definition ID
    /// of whatever <c>SeatProjection.PlacedTokens</c> holds, so masking before the codec runs masks the
    /// wire as well - and a masked row is the same seven fields it always was. The instance ID is NOT
    /// masked, because it is the handle the Clear offer and every displacement name and a substituted
    /// one would leave a seat unable to answer its own offers; the kind is kept out of it at the source
    /// instead - see <c>SyntheticTokenCapabilities.PlaceHandler</c>'s instance-identity label.</para>
    ///
    /// <para><b>Nothing about the rules reads the mask.</b> Both kinds print the same properties, so a
    /// masked board answers every movement, push, Place, Clear and cleanup question exactly as the real
    /// one does. That is what keeps a masked client's own legality byte-for-byte the host's rather than
    /// a second opinion about it.</para>
    /// </summary>
    internal static class FacedownTokenMask
    {
        /// <summary>
        /// <paramref name="placedTokens"/> as <paramref name="seat"/> may see it: every token whose
        /// definition hides its kind, and whose controller this seat does not own, wearing the
        /// definition's own facedown face instead.
        /// </summary>
        /// <remarks>
        /// The ordinary case - a board with no facedown token on it, which is every board in the build
        /// until Min is dealt - is answered with the source list itself, so this allocates nothing on
        /// every path it has nothing to say about.
        /// </remarks>
        internal static IReadOnlyList<PlacedToken> Mask(
            IReadOnlyList<PlacedToken> placedTokens, MatchSeat seat)
        {
            if (placedTokens == null || placedTokens.Count == 0)
            {
                return placedTokens;
            }

            var masked = (PlacedToken[])null;
            for (var index = 0; index < placedTokens.Count; index++)
            {
                var substitute = Substitute(placedTokens[index], seat);
                if (ReferenceEquals(substitute, placedTokens[index]))
                {
                    continue;
                }

                if (masked == null)
                {
                    masked = placedTokens.ToArray();
                }

                masked[index] = substitute;
            }

            return masked == null ? placedTokens : Array.AsReadOnly(masked);
        }

        /// <summary>
        /// The same substitution applied to the tokens one Clear option offers, which carry the
        /// board's own instances and are where the label a player reads is composed from.
        /// </summary>
        /// <remarks>
        /// The instance IDs and their order are untouched, so every subset invocation the rebuilt
        /// option enumerates is the same command the authority would accept.
        /// </remarks>
        internal static IReadOnlyList<ClearOptionProjection> MaskClearOptions(
            IReadOnlyList<ClearOptionProjection> options, MatchSeat seat)
        {
            if (options == null || options.Count == 0)
            {
                return options;
            }

            var masked = (ClearOptionProjection[])null;
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                var tokens = Mask(option.AdjacentTokens, seat);
                if (ReferenceEquals(tokens, option.AdjacentTokens))
                {
                    continue;
                }

                if (masked == null)
                {
                    masked = options.ToArray();
                }

                masked[index] = new ClearOptionProjection(
                    option.ReplacementOffer, option.ReplacedAttackActionDefinitionId, tokens);
            }

            return masked == null ? options : Array.AsReadOnly(masked);
        }

        /// <summary>
        /// One token as this seat sees it. The controller's own seat, and only it, sees the kind: p.4's
        /// hidden information is hidden from allies too, because the card's own "facedown" says the
        /// piece is face down on the table and an ally reads the same table an opponent does.
        /// </summary>
        private static PlacedToken Substitute(PlacedToken token, MatchSeat seat)
        {
            if (token == null || !token.Definition.HidesItsKind ||
                (seat != null && token.ControllerHeroId.IsDefined &&
                    seat.Owns(token.ControllerHeroId)))
            {
                return token;
            }

            return new PlacedToken(
                token.Id,
                token.Definition.FacedownFace,
                token.OwnerHeroId,
                token.ControllerHeroId,
                token.SourceCardInstanceId,
                token.Coordinate);
        }
    }

    /// <summary>
    /// Milestone 8.30a. Who the authority is currently waiting on, and nothing about what they were
    /// asked.
    /// </summary>
    /// <remarks>
    /// <para>Every seat receives this, because "we are waiting for Blue" is public and is what a
    /// waiting client has to show. The decision's own content reaches only the seat that owns it.</para>
    /// <para>The owner is read off the authoritative pending object, never inferred: each of the ten
    /// families already records its own chooser, so this derives nothing and restates the authority.</para>
    /// </remarks>
    public readonly struct PendingDecisionOwner : IEquatable<PendingDecisionOwner>
    {
        private PendingDecisionOwner(
            PendingDecisionFamilyId familyId, IReadOnlyList<HeroId> heroIds, TeamSide teamSide)
        {
            FamilyId = familyId;
            HeroIds = heroIds;
            TeamSide = teamSide;
        }

        public static readonly PendingDecisionOwner None =
            new PendingDecisionOwner(default, Array.Empty<HeroId>(), TeamSide.None);

        internal static PendingDecisionOwner ForHero(PendingDecisionFamilyId familyId, HeroId heroId) =>
            new PendingDecisionOwner(familyId, new[] { heroId }, TeamSide.None);

        /// <summary>
        /// Milestone 8.35u. Several heroes awaited at once - rulebook p.15's simultaneous leveling,
        /// where every hero that bought a level is asked its own question at the same moment.
        /// </summary>
        internal static PendingDecisionOwner ForHeroes(
            PendingDecisionFamilyId familyId, IEnumerable<HeroId> heroIds)
        {
            var awaited = (heroIds ?? throw new ArgumentNullException(nameof(heroIds))).ToArray();
            if (awaited.Length == 0 || awaited.Any(heroId => !heroId.IsDefined) ||
                awaited.Distinct().Count() != awaited.Length)
            {
                throw new ArgumentException(
                    "A hero-owned decision is awaited on at least one defined hero, each named once.",
                    nameof(heroIds));
            }

            return new PendingDecisionOwner(familyId, Array.AsReadOnly(awaited), TeamSide.None);
        }

        internal static PendingDecisionOwner ForTeam(PendingDecisionFamilyId familyId, TeamSide side) =>
            new PendingDecisionOwner(familyId, Array.Empty<HeroId>(), TeamSide.None == side
                ? TeamSide.None
                : side);

        public PendingDecisionFamilyId FamilyId { get; }

        /// <summary>
        /// Milestone 8.35u. Every hero the authority is waiting on, empty for a team-owned decision.
        ///
        /// <para>This is the public half in full: "we are waiting for these players" is what a table
        /// can see and what a waiting client has to show. What each of them was asked stays with the
        /// seat that owns it.</para>
        /// </summary>
        public IReadOnlyList<HeroId> HeroIds { get; }

        /// <summary>
        /// The hero that may answer, or undefined for a team-owned decision.
        ///
        /// <para><b>Milestone 8.35u: when several heroes are awaited this returns the first in the
        /// authority's own order, and does not throw.</b> That is a deliberate departure from the
        /// throwing single-item accessors on the pause record, and the difference is what each is for:
        /// this one is read by surfaces to say whose input is wanted, and a throw on a surface reader
        /// path is what froze a live playtest at milestone 8.35e. Nothing authorizes anything from it -
        /// authorization is <see cref="IsOwnedBy"/> and the per-item command filtering in
        /// <see cref="SeatProjectionFactory.AuthorizedCommands"/>, both of which read the whole set.</para>
        /// </summary>
        public HeroId HeroId => HeroIds.Count == 0 ? default : HeroIds[0];

        /// <summary>The side that may answer, or <c>None</c> for a hero-owned decision.</summary>
        public TeamSide TeamSide { get; }

        public bool IsDefined => FamilyId.IsDefined;

        public bool IsTeamOwned => TeamSide != TeamSide.None;

        /// <summary>
        /// Whether this seat is one of the seats being waited on. With several heroes awaited it is
        /// true for each of them at once - which is the whole of milestone 8.35u at this boundary, and
        /// why it must be read instead of comparing against <see cref="HeroId"/>.
        /// </summary>
        public bool IsOwnedBy(MatchSeat seat)
        {
            if (seat == null || !IsDefined)
            {
                return false;
            }

            return IsTeamOwned
                ? seat.PlaysFor(TeamSide)
                : HeroIds.Any(seat.Owns);
        }

        /// <summary>
        /// Milestone 8.35u. Written out rather than inherited, because the awaited heroes became a LIST
        /// and <see cref="ValueType"/>'s field-wise default would then compare list references - so two
        /// owners naming the same heroes would have reported themselves different, silently, in every
        /// equality the projections and their fixtures perform.
        /// </summary>
        public bool Equals(PendingDecisionOwner other) =>
            FamilyId.Equals(other.FamilyId) && TeamSide == other.TeamSide &&
            HeroIds.SequenceEqual(other.HeroIds);

        public override bool Equals(object obj) => obj is PendingDecisionOwner other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FamilyId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TeamSide;
                return HeroIds.Aggregate(hashCode, (current, heroId) =>
                    (current * 397) ^ heroId.GetHashCode());
            }
        }

        public static bool operator ==(PendingDecisionOwner left, PendingDecisionOwner right) =>
            left.Equals(right);

        public static bool operator !=(PendingDecisionOwner left, PendingDecisionOwner right) =>
            !left.Equals(right);

        public override string ToString() => !IsDefined
            ? "none"
            : IsTeamOwned
                ? $"{FamilyId.Value} ({TeamSide})"
                : $"{FamilyId.Value} ({string.Join(", ", HeroIds.Select(heroId => heroId.Value))})";
    }

    /// <summary>
    /// Milestone 8.30a. What is publicly countable about one hero's cards without naming any of them.
    /// </summary>
    /// <remarks>
    /// Hand and deck sizes are public at the table - cards are dealt, played and discarded in the
    /// open, so anyone can count them - while the identities behind those counts are not. Publishing
    /// the count and withholding the cards is exactly that distinction, and it is what lets a client
    /// draw an opponent's hand as backs without ever holding a card it may not see.
    /// </remarks>
    public sealed class SeatHandSummary
    {
        internal SeatHandSummary(
            HeroId heroId,
            int handCount,
            int deckCount,
            int spellbookCount,
            IEnumerable<SpellbookBackCount> spellbookBacks = null,
            IEnumerable<CardColor> facedownDiscardColors = null)
        {
            HeroId = heroId;
            HandCount = handCount;
            DeckCount = deckCount;
            SpellbookCount = spellbookCount;
            FacedownDiscardColors = Array.AsReadOnly((facedownDiscardColors ?? Enumerable.Empty<CardColor>())
                .OrderBy(color => color)
                .ToArray());
            SpellbookBacks = Array.AsReadOnly((spellbookBacks ?? Enumerable.Empty<SpellbookBackCount>())
                .Where(back => back.Count > 0)
                .OrderBy(back => back.Back)
                .ToArray());
            if (SpellbookBacks.Sum(back => back.Count) != spellbookCount &&
                SpellbookBacks.Count > 0)
            {
                throw new ArgumentException(
                    "The book's backs count the same spells the book's size does.", nameof(spellbookBacks));
            }
        }

        public HeroId HeroId { get; }
        public int HandCount { get; }
        public int DeckCount { get; }

        /// <summary>
        /// Milestone 9.32g. One entry per facedown card in this hero's discard pile, naming only the
        /// colour every seat knows it has (see <see cref="FacedownSlotMask.KnownColorOf"/>): another seat
        /// draws the pile's hatched gold without holding the card, which its projection withholds
        /// (a facedown card is never public, p.4).
        /// </summary>
        public IReadOnlyList<CardColor> FacedownDiscardColors { get; }

        /// <summary>
        /// Milestone 9.30b. How many spell cards lie facedown in this hero's spellbook - zero for a
        /// hero who owns none. Public for the reason the hand size is: every seat can count the book,
        /// while only its owner may read the faces. The removed spells need no count here: they lie
        /// faceup in an open zone, so every seat already holds them as cards.
        /// </summary>
        public int SpellbookCount { get; }

        /// <summary>
        /// Milestone 9.30h. The same facedown book counted by the BACK each spell shows - what every
        /// seat at the table sees of it (GY-Q4: "every seat sees tier backs, counts and the faceup
        /// pile"; lesson 25). One row per back the book holds, in back order, summing to
        /// <see cref="SpellbookCount"/>; empty for a hero who owns no book or whose book is empty.
        /// A back names no spell: see <see cref="SpellbookBackCount.BackOf"/> for why the three basic
        /// spells and the Tier I spells are one back.
        /// </summary>
        public IReadOnlyList<SpellbookBackCount> SpellbookBacks { get; }

        /// <summary>
        /// Milestone 9.30b. Every hero's summary, in participant order, derived once for the
        /// Director's projection and handed to every seat unchanged.
        /// </summary>
        internal static IReadOnlyList<SeatHandSummary> Derive(
            MatchParticipants participants,
            IReadOnlyList<CardInstance> cards,
            IReadOnlyList<CardDefinition> definitions = null)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            var byReference = (definitions ?? Array.Empty<CardDefinition>())
                .GroupBy(definition => definition.Reference)
                .ToDictionary(group => group.Key, group => group.First());
            return Array.AsReadOnly(participants.Heroes
                .Select(hero =>
                {
                    var book = cards
                        .Where(card => card.OwnerId.Equals(hero.Id) &&
                            card.State.Location == CardLocation.Spellbook)
                        .ToArray();
                    return new SeatHandSummary(
                        hero.Id,
                        // Milestone 8.40f. The count a printed hand-size narrowing reads is THIS count,
                        // asked of one query rather than written twice, so a player can check a greyed
                        // candidate against the number their own screen already shows them.
                        HeroHandQuery.Count(cards, hero.Id),
                        cards.Count(card => card.OwnerId.Equals(hero.Id) &&
                            card.State.Location == CardLocation.Deck),
                        book.Length,
                        // Milestone 9.30h. Counted only where every book spell's definition is known,
                        // which is every authoritative projection; a caller without definitions gets
                        // the size alone, as before.
                        book.All(card => byReference.ContainsKey(card.DefinitionReference))
                            ? book
                                .GroupBy(card => SpellbookBackCount.BackOf(
                                    byReference[card.DefinitionReference].Progression))
                                .Select(group => new SpellbookBackCount(group.Key, group.Count()))
                            : null,
                        // Milestone 9.32g. The pile's facedown cards, by the colour every seat knows.
                        cards.Where(card => card.OwnerId.Equals(hero.Id) &&
                                card.State.Location == CardLocation.DiscardPile &&
                                CardLifecycleAuthority.IsFacedownRestingCard(card))
                            .Select(card => FacedownSlotMask.KnownColorOf(card, definitions)));
                })
                .ToArray());
        }
    }

    /// <summary>
    /// Milestone 9.30h. How many facedown spells of one hero's book show one back.
    /// </summary>
    /// <remarks>
    /// <para><b>A back is what the table sees of a facedown spell.</b> The spell backs print the
    /// spell's Tier, so a seat that cannot read a book's faces still sees how many Tier II spells it
    /// holds - which is why approved mockup C draws the other seats' book as four stacks of backs
    /// with counts, and why this count is projected to every seat rather than derived by a client
    /// from a printed set it does not hold.</para>
    /// <para><b>Four backs, not five.</b> Verified 9.30h on the Tabletopia corpus: the three basic
    /// (Gold) spells' backs are byte-identical to the three Tier I spells' backs
    /// (<c>spells-tt__7721627__back.png</c> and five more), so a basic spell and a Tier I spell are
    /// one back at the table. Counting them apart would publish what no player can see; they are
    /// counted together under <see cref="CardTier.I"/>. Tier II, Tier III and Wish's Tier IV lamp are
    /// each their own back.</para>
    /// </remarks>
    public readonly struct SpellbookBackCount : IEquatable<SpellbookBackCount>
    {
        public SpellbookBackCount(CardTier back, int count)
        {
            if (back == CardTier.None || !Enum.IsDefined(typeof(CardTier), back))
            {
                throw new ArgumentOutOfRangeException(nameof(back), back, "A spell back is Tier I to IV.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A count is never negative.");
            }

            Back = back;
            Count = count;
        }

        /// <summary>The back: <see cref="CardTier.I"/> (basic and Tier I spells) to <see cref="CardTier.IV"/>.</summary>
        public CardTier Back { get; }

        public int Count { get; }

        /// <summary>
        /// The back a spell definition shows: its Tier, and the Tier I back for a basic spell, which
        /// prints none (see the type's remarks).
        /// </summary>
        public static CardTier BackOf(CardProgressionProfile progression)
        {
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            return progression.Tier == CardTier.None ? CardTier.I : progression.Tier;
        }

        public bool Equals(SpellbookBackCount other) => Back == other.Back && Count == other.Count;

        public override bool Equals(object obj) => obj is SpellbookBackCount other && Equals(other);

        public override int GetHashCode() => ((int)Back * 397) ^ Count;

        public override string ToString() => Back + "x" + Count;
    }

    /// <summary>
    /// Milestone 8.30b. The pending decisions, their companion envelopes and the offers, each scoped
    /// to the seat that may act on it.
    /// </summary>
    /// <remarks>
    /// <para>Gathered into one object so the scoping rule is stated once rather than repeated down a
    /// twenty-parameter constructor, and so that the rule itself is readable: a decision and its
    /// preview reach the seat that answers it, a resolution envelope also reaches the seat that is
    /// acting, and an offer reaches only the actor and only while nothing is pending.</para>
    /// <para>It holds no reference to the projection it was built from. Everything here was copied
    /// out at construction, which is what keeps the full-trust read model unreachable from a seat.</para>
    /// </remarks>
    public sealed class SeatPendingSurface
    {
        internal SeatPendingSurface(
            MatchSessionProjection source,
            MatchSeat seat,
            bool ownsDecision,
            bool isActor,
            IReadOnlyCollection<CardInstanceId> nameableCardIds = null)
        {
            var standing = source.DecisionContract != null;
            var resolving = ownsDecision || isActor;
            var offering = isActor && !standing;

            InitiativeChoice = ownsDecision ? source.PendingInitiativeChoice : null;
            MovementChoice = ownsDecision ? source.PendingMovementChoice : null;
            AttackTargetChoice = ownsDecision ? source.PendingAttackTargetChoice : null;
            AttackTargetDefensePreviews = ownsDecision
                ? source.PendingAttackTargetDefensePreviews
                : Array.Empty<AttackTargetDefensePreviewProjection>();
            DefenseChoice = ownsDecision ? source.PendingDefenseChoice : null;
            DefensePreview = ownsDecision ? source.PendingDefensePreview : null;
            EffectChoice = ownsDecision ? source.PendingEffectChoice : null;
            EffectChoicePreview = ownsDecision
                ? source.PendingEffectChoicePreview
                : Array.Empty<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>();
            DisplacedMinionCorrection = ownsDecision ? source.PendingDisplacedMinionCorrection : null;
            MinionBattleChoice = ownsDecision ? source.PendingMinionBattleChoice : null;
            HeroRespawnChoice = ownsDecision ? source.PendingHeroRespawnChoice : null;
            ActingBodyChoice = ownsDecision ? source.PendingActingBodyChoice : null;
            // Milestone 8.35u. Narrowed to this seat's own open item rather than handed over whole.
            // A pause that stands on several heroes carries several option lists, and an option list
            // names cards in that hero's HAND - so another hero's options are the deck-and-hand
            // subtraction rule in a different costume, and they are ABSENT here rather than present
            // and unrendered, which is what F-ER2-07 means by a separate read model. Who is still
            // choosing stays public, through DecisionOwner.HeroIds; what they may choose does not.
            // This is exactly the line rulebook p.7's commitments already draw.
            //
            // A seat owns exactly one hero, so the narrowed pause holds at most one item and every
            // single-item reader downstream keeps working unchanged.
            SystemTransitionStep = ownsDecision
                ? NarrowSystemTransition(source.PendingSystemTransitionStep, seat)
                : null;
            SystemTransitionDescription = ownsDecision
                ? NarrowSystemTransitionDescription(source, SystemTransitionStep)
                : null;
            RemovalPrevention = ownsDecision ? source.PendingRemovalPrevention : null;

            // Milestone 8.35g. Unscoped, unlike every owner-private line above it, and deliberately:
            // the release pause is a fact every seat must be able to SEE - it is what the whole table
            // is waiting on - and it discloses nothing private. The hero and side it names are
            // already public through DecisionOwner, and the card it names has finished resolving, so
            // it sits in the public turn area. Only the seat that OWNS it is published an answer.
            TurnRelease = source.PendingTurnRelease;

            MovementResolution = resolving ? source.PendingMovementResolution : null;
            AttackResolution = resolving ? source.PendingAttackResolution : null;

            // Milestone 8.40f. `resolving` is the right predicate for WHETHER a seat sees the frame
            // and the wrong one for WHAT it sees, and Bain's Blue line is the sentence that makes the
            // difference matter: the hero who is acting is handed the live frame, and a frame that
            // has recorded an enemy's answer carries that enemy's card instance id in
            // CompletedOutcomes and AcceptedChoices - and, while the pause stands, the enemy's whole
            // HAND in PendingChoice.Candidates. Bain guesses a colour against a card his own screen
            // already named.
            //
            // It is narrowed rather than withheld, because the acting seat genuinely needs the frame
            // (its identity is what a bot's own intent reads, and what tells a surface a resolution
            // is running). Two rules, both derived from what this seat may already name:
            //
            // (1) the pause travels only to the seat that OWNS it. A non-owner is handed no
            //     PendingEffectChoice on the line above and no DecisionContract at all, so a copy of
            //     the same pause riding inside the frame is duplication that could only leak.
            // (2) a card-instance fact naming a card this seat may not see is dropped. The set is
            //     exactly this seat's own OwnCards + PublicCards, so the frame can name nothing the
            //     projection does not already name - which is why the reveal alone is what makes the
            //     card readable, in every seat at once, and why an Undo taken before the reveal puts
            //     it back out of reach without anything here having to know about Undo.
            //
            // This is an application-layer arm for one printed shape. EffectChoiceRequest gains no
            // visibility field, and IsPublic's fail-closed switch is untouched: the rule here is a
            // CONSEQUENCE of IsPublic's answer rather than a second opinion about it.
            SkillResolution = resolving
                ? NarrowResolution(source.PendingSkillResolution, ownsDecision, nameableCardIds)
                : null;

            TurnActions = offering
                ? source.AvailableTurnActions
                : Array.Empty<ActionOffer>();
            FastTravelOptions = offering
                ? source.AvailableFastTravelOptions
                : Array.Empty<FastTravelOptionProjection>();
            // Milestone 8.61d. A Clear option carries the board's own PlacedToken instances, and the
            // selection label a player reads is composed from each one's DisplayName - so an enemy hero
            // standing beside a facedown mine would have been offered "Mine (blast)" in a list. The
            // same substitution SeatProjection applies to the board applies here, for the same reason
            // and from the same one owner. The wire is unaffected either way: the detail codec writes
            // instance IDs only, and a network client rebuilds this option from ITS OWN already-masked
            // PlacedTokens.
            ClearOptions = offering
                ? FacedownTokenMask.MaskClearOptions(source.AvailableClearOptions, seat)
                : Array.Empty<ClearOptionProjection>();
            HoldOffer = offering ? source.AvailableHoldOffer : null;
        }

        /// <summary>
        /// Milestone 8.40f. The running effect frame as this seat may see it: its standing pause only
        /// where this seat owns the decision, and no card-instance fact naming a card this seat's own
        /// projection does not already name.
        /// </summary>
        /// <remarks>
        /// <para>It is a NARROWING and not a summary, deliberately: identity, continuation, actor,
        /// controller and repeat state are untouched, so everything a surface or a bot reads off this
        /// frame today still reads. What is dropped is exactly what a seat could not have learned any
        /// other way.</para>
        /// <para><b>An unbounded set means "narrow nothing", which is what a caller that has not
        /// derived one gets.</b> The set is the seat's own <c>OwnCards + PublicCards</c>, derived once
        /// in <see cref="SeatProjectionFactory.ForSeat"/> and handed down rather than recomputed, so
        /// the cards the frame may name and the cards <see cref="SeatProjection.Cards"/> holds are one
        /// list rather than two that can disagree.</para>
        /// </remarks>
        internal static GuardsDigital.Core.Rules.EffectResolution.PendingResolution NarrowResolution(
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution frame,
            bool ownsDecision,
            IReadOnlyCollection<CardInstanceId> nameableCardIds)
        {
            if (frame == null)
            {
                return null;
            }

            var pause = ownsDecision ? frame.PendingChoice : null;

            // The ordinary case - a frame naming nothing this seat may not see - is answered with the
            // frame itself, so this arm allocates nothing on every path it has nothing to say about.
            if (ReferenceEquals(pause, frame.PendingChoice) &&
                (nameableCardIds == null || NamesOnly(frame, nameableCardIds)))
            {
                return frame;
            }

            var outcomes = frame.CompletedOutcomes;
            var choices = frame.AcceptedChoices;
            if (nameableCardIds != null)
            {
                outcomes = frame.CompletedOutcomes
                    .Select(outcome => new GuardsDigital.Core.Rules.EffectResolution.EffectNodeOutcome(
                        outcome.NodeId,
                        outcome.ResultKind,
                        outcome.Outputs.Where(output => MayName(output.Value, nameableCardIds))))
                    .ToArray();
                choices = frame.AcceptedChoices
                    .Select(choice => new GuardsDigital.Core.Rules.EffectResolution.EffectChoiceRecord(
                        choice.NodeId,
                        choice.ChoiceId,
                        choice.ResolvedBy,
                        choice.Selected.Where(candidate =>
                            MayName(candidate.Value, nameableCardIds))))
                    .ToArray();
            }

            return frame.With(frame.Continuation, outcomes, choices, pause);
        }

        /// <summary>
        /// Whether this frame's recorded facts already name nothing outside
        /// <paramref name="nameableCardIds"/>, which is true of every frame in the build but the one
        /// Bain's Blue line stands inside.
        /// </summary>
        private static bool NamesOnly(
            GuardsDigital.Core.Rules.EffectResolution.PendingResolution frame,
            IReadOnlyCollection<CardInstanceId> nameableCardIds) =>
            frame.CompletedOutcomes.All(outcome =>
                outcome.Outputs.All(output => MayName(output.Value, nameableCardIds))) &&
            frame.AcceptedChoices.All(choice =>
                choice.Selected.All(candidate => MayName(candidate.Value, nameableCardIds)));

        /// <summary>
        /// Whether a published fact may be named to a seat holding <paramref name="nameableCardIds"/>.
        /// Every value that is not a card is unaffected - a hero, a space, a number or a flag is not
        /// hidden information, and dropping one would break a reader for no privacy gain.
        /// </summary>
        private static bool MayName(
            GuardsDigital.Core.Effects.EffectValue value,
            IReadOnlyCollection<CardInstanceId> nameableCardIds) =>
            value.Kind != GuardsDigital.Core.Effects.EffectValueKind.CardInstance ||
            nameableCardIds.Contains(value.CardInstanceId);

        /// <summary>
        /// Milestone 8.35u. The paused step as this seat may see it: its own open item and nothing
        /// else when several stand, and the pause unchanged when only one does.
        /// </summary>
        private static GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            NarrowSystemTransition(
                GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep step, MatchSeat seat)
        {
            if (step == null || !step.IsConcurrent)
            {
                return step;
            }

            var own = step.OpenItems.Where(open =>
                open.ItemId.IsDefined &&
                IsLevelUpItem(open.ItemId) &&
                seat.Owns(GuardsDigital.Core.Rules.Progression.HeroLevelUpItem
                    .Read(open.ItemId).HeroId)).ToArray();

            // A seat that owns none of the open items is being waited on for nothing, which the
            // ownership rule above has already decided; returning null rather than an empty pause is
            // the same statement the surface makes for every family this seat does not own.
            return own.Length == 0
                ? null
                : new GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep(
                    step.InstanceId,
                    step.DefinitionId,
                    step.StepId,
                    own,
                    step.CompletedStepIds,
                    step.CompletedItemIds);
        }

        /// <summary>
        /// The description of the narrowed pause, chosen by item rather than by position - the
        /// Director's list is in hero-id order and this seat's own question is rarely first.
        /// </summary>
        private static GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            NarrowSystemTransitionDescription(
                MatchSessionProjection source,
                GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep narrowed)
        {
            if (narrowed == null)
            {
                return null;
            }

            if (ReferenceEquals(narrowed, source.PendingSystemTransitionStep))
            {
                return source.PendingSystemTransitionDescription;
            }

            // THE DESCRIPTION CARRIES ITS PAUSE, so handing this seat the Director's description would
            // hand it the Director's pause - and a surface reading `description.PendingStep.
            // Invocations` would then hold every hero's answers, which is precisely what narrowing the
            // pause was for. It is rebuilt against the narrowed pause instead. That the constructor
            // re-checks the options against it is the point: a description and the pause it travels
            // with cannot disagree about what this seat was offered.
            var itemId = narrowed.OpenItems[0].ItemId;
            var own = source.PendingSystemTransitionDescriptions
                .FirstOrDefault(description => description.ItemId.Equals(itemId));
            return own == null
                ? null
                : new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription(
                    own.Kind,
                    narrowed,
                    own.HeroId,
                    own.Options,
                    own.Level,
                    own.PaidCoins,
                    own.RemainingCoins,
                    own.ItemId);
        }

        private static bool IsLevelUpItem(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId itemId)
        {
            try
            {
                GuardsDigital.Core.Rules.Progression.HeroLevelUpItem.Read(itemId);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public PendingInitiativeChoice InitiativeChoice { get; }
        public PendingMovementChoice MovementChoice { get; }
        public PendingAttackTargetChoice AttackTargetChoice { get; }
        public IReadOnlyList<AttackTargetDefensePreviewProjection> AttackTargetDefensePreviews { get; }
        public PendingDefenseResponseChoice DefenseChoice { get; }
        public PendingDefensePreviewProjection DefensePreview { get; }
        public PendingEffectChoice EffectChoice { get; }

        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            EffectChoicePreview { get; }

        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            DisplacedMinionCorrection { get; }

        public PendingMinionBattleChoice MinionBattleChoice { get; }
        public PendingHeroRespawnChoice HeroRespawnChoice { get; }

        /// <summary>Milestone 9.21b. "Which of you acts", for the seat that owns it only.</summary>
        public PendingActingBodyChoice ActingBodyChoice { get; }

        public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            SystemTransitionStep { get; }

        public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            SystemTransitionDescription { get; }

        public RemovalPreventionPrompt RemovalPrevention { get; }

        /// <summary>Milestone 8.35g. The finished turn awaiting its own player, public to every seat.</summary>
        public GuardsDigital.Core.Cards.PendingTurnRelease TurnRelease { get; }
        public PendingCardActionResolution MovementResolution { get; }
        public PendingAttackResolution AttackResolution { get; }
        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution SkillResolution { get; }
        public IReadOnlyList<ActionOffer> TurnActions { get; }
        public IReadOnlyList<FastTravelOptionProjection> FastTravelOptions { get; }
        public IReadOnlyList<ClearOptionProjection> ClearOptions { get; }
        public ActionOffer? HoldOffer { get; }
    }

    /// <summary>
    /// Milestone 8.30a. One seat's authorized view of the match.
    /// </summary>
    /// <remarks>
    /// <para>A separate read model rather than a filtered <see cref="MatchSessionProjection"/>, which
    /// is the point of `F-ER2-07`: another seat's hand is <em>absent</em> here, not concealed by a
    /// view that still holds it. Nothing a presentation forgets to hide can leak, because nothing it
    /// could forget is in the object.</para>
    /// <para>Director-only authority is absent for the same structural reason: there is no
    /// accepted-command history, no snapshot, no diagnostic log, no Undo, no restart, and no
    /// Tie-Breaker override on this type or on the client that carries it.</para>
    /// <para>What a seat may actually submit is <see cref="AuthorizedCommands"/>, and the host
    /// refuses anything outside it. The set is derived from the same authoritative state this
    /// projection reports, so a client never decides its own legality and the two can never
    /// disagree.</para>
    /// </remarks>
    public sealed class SeatProjection : IMatchReadModel, IExperimentalReadModel
    {
        private readonly SeatPendingSurface pending;
        private readonly IReadOnlyList<CardInstance> visibleCards;

        internal SeatProjection(
            MatchSeat seat,
            long version,
            MatchSessionProjection source,
            IReadOnlyList<CardInstance> ownCards,
            IReadOnlyList<CardInstance> publicCards,
            IReadOnlyList<SeatHandSummary> handSummaries,
            PendingDecisionOwner decisionOwner,
            PendingDecisionContract decisionContract,
            IReadOnlyList<DirectorCommand> authorizedCommands,
            SeatPendingSurface pendingSurface,
            TeamAgreementProjection teamAgreement,
            bool isSeatUndoAvailable,
            MatchUndoMarker undoMarker = default,
            ExperimentalMatchOptions experiments = null,
            IReadOnlyList<TeammateMarker> teammateMarkers = null,
            long coordinationRevision = 0, int coordinationGeneration = 0, MatchClockState clock = null)
        {
            Experiments = experiments ?? ExperimentalMatchOptions.Disabled;
            TeammateMarkers = teammateMarkers ?? Array.Empty<TeammateMarker>();
            CoordinationRevision = coordinationRevision;
            CoordinationGeneration = coordinationGeneration;
            Clock = clock ?? MatchClockState.Disabled;
            var alliedHeroes = source.Setup.Heroes.Where(h => h.TeamSide == seat.TeamSide && !seat.Owns(h.HeroId)).Select(h => h.HeroId).ToArray();
            AlliedHandCards = Experiments.TeammateCoordination
                ? Array.AsReadOnly(source.Cards.Where(c => alliedHeroes.Contains(c.OwnerId) && c.State.Location == CardLocation.Hand).ToArray())
                : (IReadOnlyList<CardInstance>)Array.Empty<CardInstance>();
            CardInspections = HeroCardInspection.ForSeat(source, seat, Experiments);
            IsSeatUndoAvailable = isSeatUndoAvailable;
            UndoMarker = undoMarker;
            // Milestone 8.35r. The SAME log the Director reads, taken whole rather than filtered.
            // Every kind the selection admits is table-public, so there is nothing here to scope
            // away - and taking the source's own derived value is what makes that identity a fact of
            // the composition rather than a claim a filter would have to keep proving.
            // Milestone 9.32g: the same rows, except that a card move no other seat saw (a swap's
            // hand, deck or facedown card, approved mockup F) is named on its owner's seat alone -
            // the facedown slot's own rule, below, applied to the log.
            MatchLog = CardMoveMask.Mask(source.MatchLog, seat);

            // Milestone 8.53. The SAME board the Director reads, taken whole rather than filtered,
            // for the reason the log above it is: every column counts an event the whole table
            // watched happen, so there is nothing here to scope away - and taking the source's own
            // value makes that identity a fact of the composition rather than a claim a filter
            // would have to keep proving.
            Scoreboard = source.Scoreboard;
            pending = pendingSurface;
            visibleCards = Array.AsReadOnly(ownCards.Concat(publicCards).ToArray());
            ActiveEffectSourceCardInstanceIds = source.ActiveEffectSourceCardInstanceIds;
            Seat = seat;
            Version = version;
            Setup = source.Setup;
            SelectedMap = source.SelectedMap;
            Participants = source.Participants;
            Definitions = source.Definitions;
            Heroes = source.Heroes;
            // Milestone 9.21i. Public: which body acts is as open as where each body stands.
            ActingBodies = source.ActingBodies;
            Minions = source.Minions;
            Obstacles = source.Obstacles;
            // Milestone 8.61d. The one place a facedown token stops naming its kind. See
            // MaskFacedownTokens: it is a projection filter and never an authorization one, so the
            // authority, the Director's read model, the snapshot and the replay are untouched, and
            // every seat that is not the token's controller is handed a board whose mines are mines.
            //
            // It is done in the constructor rather than derived in SeatProjectionFactory.ForSeat, so
            // that no present or future composer of a seat view can build one that forgot.
            PlacedTokens = FacedownTokenMask.Mask(source.PlacedTokens, seat);
            HeldMarkers = source.HeldMarkers;
            RoundIndex = source.RoundIndex;
            TurnIndex = source.TurnIndex;
            Phase = source.Phase;
            TieBreakerState = source.TieBreakerState;
            // Milestone 9.32a. A slot whose card was placed facedown names it only to its owner.
            TurnSlots = FacedownSlotMask.Mask(
                source.TurnSlots, seat, source.Cards, source.Participants, source.Definitions);
            // Milestone 8.35w. Taken from the source rather than re-derived, so a seat's
            // retained round window IS the host's own - which is what keeps one wire frame bounded
            // and keeps the host's view and every client's byte-for-byte identical. Milestone 9.32a:
            // identical except where a slot's card was placed facedown, which only its owner's seat
            // may name; the window's rounds and slots are the same and only that identity is absent.
            CompletedRounds = FacedownSlotMask.MaskRounds(
                source.CompletedRounds, seat, source.Cards, source.Participants, source.Definitions);
            CurrentEndOfRoundStage = source.CurrentEndOfRoundStage;
            Commitments = source.Commitments;
            SelectedActor = source.SelectedActor;
            IsTurnActive = source.IsTurnActive;
            TeamLifeCounters = source.TeamLifeCounters;
            RemainingWaveCounters = source.RemainingWaveCounters;
            TeamVictoryTallies = source.TeamVictoryTallies;
            HeroProgression = source.HeroProgression;
            HeroDefenseModifiers = source.HeroDefenseModifiers;
            Outcome = source.Outcome;
            OwnCards = ownCards;
            PublicCards = publicCards;
            HandSummaries = handSummaries;
            DecisionOwner = decisionOwner;
            DecisionContract = decisionContract;
            AuthorizedCommands = authorizedCommands;
            TeamAgreement = teamAgreement;
        }

        public ExperimentalMatchOptions Experiments { get; }
        public IReadOnlyList<CardInstance> AlliedHandCards { get; }
        public IReadOnlyList<HeroCardInspection> CardInspections { get; }
        public IReadOnlyList<TeammateMarker> TeammateMarkers { get; }
        public long CoordinationRevision { get; }
        public int CoordinationGeneration { get; }
        public MatchClockState Clock { get; }

        public MatchSeat Seat { get; }

        /// <summary>
        /// The authoritative projection version this view was built from. A submission carries the
        /// version it was decided against, and the host refuses anything else.
        /// </summary>
        public long Version { get; }
        public bool IsInputEnabled => true;

        /// <summary>
        /// Milestone 8.35f. Whether this seat may step its own turn back by one accepted command
        /// right now. The host publishes it and authorizes the rewind by the same rule, so a
        /// surface never decides its own undo any more than it decides its own legality.
        /// </summary>
        public bool IsSeatUndoAvailable { get; }

        public QuickGameSetupState Setup { get; }
        public BoardMapDefinition SelectedMap { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<HeroOccupancy> Heroes { get; }
        public IReadOnlyList<MinionOccupancy> Minions { get; }
        public IReadOnlyList<ObstacleOccupancy> Obstacles { get; }
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }
        public IReadOnlyList<MarkerAttachment> HeldMarkers { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public TieBreakerState TieBreakerState { get; }
        public IReadOnlyList<RoundTurnSlot> TurnSlots { get; }
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds { get; }
        public EndOfRoundStage? CurrentEndOfRoundStage { get; }

        /// <summary>
        /// Every hero's commitment status, with the card identity only once it is revealed. The
        /// authority already publishes it this way; a facedown commitment has no identity to hide.
        /// </summary>
        public IReadOnlyList<MatchCommitmentProjection> Commitments { get; }

        public SelectedInitiativeActor SelectedActor { get; }
        public bool IsTurnActive { get; }
        public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; }

        /// <summary>
        /// Milestone 8.35z60. p.8's live Wave-counter pool, taken from the source's own
        /// read rather than re-derived - it is public and un-scoped, so a seat's copy is byte-for-byte
        /// the Director's.
        /// </summary>
        public int RemainingWaveCounters { get; }

        /// <summary>
        /// Milestone 9.30g. Every team's count toward a printed team victory (Gydion's Wish), taken
        /// from the source - public and un-scoped (GY-Q10), so a seat's copy is the Director's.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Victory.TeamVictoryTally> TeamVictoryTallies { get; }

        public IReadOnlyList<HeroProgressionProjection> HeroProgression { get; }
        public IReadOnlyList<HeroDefenseModifierProjection> HeroDefenseModifiers { get; }

        /// <summary>
        /// Milestone 8.53. The match scoreboard - identical to every other seat's and to the
        /// Director's, because every column counts something the whole table watched happen.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry> Scoreboard { get; }
        public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome { get; }

        /// <summary>
        /// Every card this seat's own hero owns, wherever it is - hand, deck, turn area, discard,
        /// item slot, Ultimate area, spellbook (its faces this seat's alone) or removed-spell pile.
        /// </summary>
        public IReadOnlyList<CardInstance> OwnCards { get; }

        /// <summary>
        /// The other heroes' cards whose identity is already public: a revealed commitment, a card
        /// some public projection field already names, or a card in a location the table can read.
        ///
        /// <para>Their hands and decks are absent, and absent together: a client that could see one
        /// opponent deck could subtract it from the printed set and read the hand.</para>
        /// </summary>
        public IReadOnlyList<CardInstance> PublicCards { get; }

        /// <summary>The countable shape of every hero's hidden cards, naming none of them.</summary>
        public IReadOnlyList<SeatHandSummary> HandSummaries { get; }

        /// <summary>Who the authority is waiting on, or <see cref="PendingDecisionOwner.None"/>.</summary>
        public PendingDecisionOwner DecisionOwner { get; }

        /// <summary>
        /// Milestone 8.35r. The seat-public match log - identical to every other seat's and to the
        /// Director's, because its selection carries nothing a seat could not already see.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog { get; }

        /// <summary>Milestone 8.35r. The rewind currently standing, or none.</summary>
        public MatchUndoMarker UndoMarker { get; }

        /// <summary>
        /// The standing decision's exact answers, and <c>null</c> unless this seat owns it. A seat
        /// waiting on another seat's decision sees <see cref="DecisionOwner"/> and no answers.
        /// </summary>
        public PendingDecisionContract DecisionContract { get; }

        /// <summary>
        /// Every command this seat may submit right now, in a stable order, and empty while it is
        /// this seat's turn to wait.
        /// </summary>
        public IReadOnlyList<DirectorCommand> AuthorizedCommands { get; }

        /// <summary>The public agreement proposal and statements, including only this seat's controls.</summary>
        public TeamAgreementProjection TeamAgreement { get; }

        /// <summary>Whether the authority is waiting on a decision this seat does not own.</summary>
        public bool IsWaitingForAnotherSeat =>
            DecisionOwner.IsDefined && !DecisionOwner.IsOwnedBy(Seat);

        /// <summary>
        /// Milestone 8.30b. The pending decisions, envelopes and offers this seat may act on.
        /// </summary>
        public SeatPendingSurface Pending => pending;

        // Milestone 8.30b: the rest of IMatchReadModel. Everything below is either shared state this
        // projection already carries or a forward to the scoped pending surface, so one HUD renders
        // the Director's view and a seat's view from the same reads.

        /// <summary>
        /// The cards this seat may name: its own, plus the ones rulebook p.4 calls open information.
        /// Another hero's hand and deck are not in the list at all.
        /// </summary>
        public IReadOnlyList<CardInstance> Cards => visibleCards;

        public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds { get; }

        public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
            ActiveEffectSourceCardInstanceIds.Contains(cardInstanceId);

        public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
            HeldMarkers.Where(marker => marker.HolderHeroId.Equals(holderHeroId));

        public MatchCommitmentProjection GetCommitment(HeroId heroId) =>
            Commitments.FirstOrDefault(value => value.HeroId.Equals(heroId));

        public PendingInitiativeChoice PendingInitiativeChoice => pending.InitiativeChoice;
        public PendingCardActionResolution PendingMovementResolution => pending.MovementResolution;
        public bool HasPendingMovementResolution => PendingMovementResolution != null;
        public PendingMovementChoice PendingMovementChoice => pending.MovementChoice;
        public PendingAttackResolution PendingAttackResolution => pending.AttackResolution;
        public bool HasPendingAttackResolution => PendingAttackResolution != null;
        public PendingAttackTargetChoice PendingAttackTargetChoice => pending.AttackTargetChoice;
        public IReadOnlyList<AttackTargetDefensePreviewProjection>
            PendingAttackTargetDefensePreviews => pending.AttackTargetDefensePreviews;
        public PendingDefenseResponseChoice PendingDefenseChoice => pending.DefenseChoice;
        public PendingDefensePreviewProjection PendingDefensePreview => pending.DefensePreview;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            pending.SkillResolution;
        public bool HasPendingSkillResolution => PendingSkillResolution != null;

        /// <summary>
        /// Milestone 8.58. The resolving card. A seat keeps the frame whole (8.40f narrows what it may
        /// NAME, never the frame's own source card), so this is the same read the host makes: whichever
        /// of the three card-action frames is running.
        /// </summary>
        public CardInstanceId ResolvingSourceCardInstanceId =>
            PendingSkillResolution?.SourceCardInstanceId ??
            PendingAttackResolution?.SourceCardInstanceId ??
            PendingMovementResolution?.SourceCardInstanceId ??
            (PendingEffectChoice != null && SelectedActor.IsDefined
                ? SelectedActor.CardInstanceId
                : default);

        public PendingEffectChoice PendingEffectChoice => pending.EffectChoice;

        public IReadOnlyList<GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidatePreview>
            PendingEffectChoicePreview => pending.EffectChoicePreview;

        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => pending.DisplacedMinionCorrection;

        public PendingMinionBattleChoice PendingMinionBattleChoice => pending.MinionBattleChoice;
        public PendingHeroRespawnChoice PendingHeroRespawnChoice => pending.HeroRespawnChoice;
        public PendingActingBodyChoice PendingActingBodyChoice => pending.ActingBodyChoice;

        /// <summary>Milestone 9.21i. The source's settled acting bodies, for every seat.</summary>
        public IReadOnlyList<HeroActingBody> ActingBodies { get; }

        public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
            PendingSystemTransitionStep => pending.SystemTransitionStep;

        public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
            PendingSystemTransitionDescription => pending.SystemTransitionDescription;

        public RemovalPreventionPrompt PendingRemovalPrevention => pending.RemovalPrevention;
        public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease => pending.TurnRelease;
        public IReadOnlyList<ActionOffer> AvailableTurnActions => pending.TurnActions;

        public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
            pending.FastTravelOptions;

        public IReadOnlyList<ClearOptionProjection> AvailableClearOptions => pending.ClearOptions;
        public ActionOffer? AvailableHoldOffer => pending.HoldOffer;
    }

    /// <summary>
    /// Milestone 8.30a. Builds one seat's authorized view, and the set of commands that view may
    /// answer with, from the authoritative projection.
    /// </summary>
    /// <remarks>
    /// It is the single derivation for both. The host authorizes a submission against exactly the
    /// set it published, so "the client offered a control the host then refused" is not a state this
    /// design can reach.
    /// </remarks>
    internal static class SeatProjectionFactory
    {
        public static SeatProjection ForSeat(
            MatchSessionProjection source,
            MatchSeat seat,
            TeamAgreementProjection teamAgreement = null,
            bool isSeatUndoAvailable = false,
            MatchUndoMarker undoMarker = default,
            ExperimentalMatchOptions experiments = null,
            IReadOnlyList<TeammateMarker> teammateMarkers = null,
            long coordinationRevision = 0, int coordinationGeneration = 0, MatchClockState clock = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (seat == null) throw new ArgumentNullException(nameof(seat));

            var owner = OwnerOfStandingDecision(source);
            var ownsDecision = owner.IsOwnedBy(seat);

            var ownCards = source.Cards
                .Where(card => card.OwnerId.Equals(seat.HeroId))
                .ToArray();
            var publicCards = source.Cards
                .Where(card => !card.OwnerId.Equals(seat.HeroId))
                .Where(card => IsPublic(card, source.MatchLog))
                .ToArray();
            // Milestone 9.30b. The source's own list, taken whole as the match log is: every count in
            // it is table-public, so there is nothing to scope away.
            var handSummaries = source.HandSummaries;

            // Milestone 8.71b. The seat that PERFORMS the selected actor's action, which is that
            // hero's own seat unless Hanu the Trickster's The Ultimate Trick stands over it: "You
            // choose the next action, and how it is performed for a hero you target with the
            // 'Hurry Up!'." The delegated hero's own seat becomes a spectator of its own hero's
            // action - it is offered nothing, because there is nothing it may submit - and the
            // deciding seat is offered the offers, which is the whole of the printed sentence at
            // this boundary.
            var isActor = source.IsTurnActive && source.SelectedActor.IsDefined &&
                seat.Owns(DecidingActor(source));

            // Milestone 8.40f. The cards this seat may name, derived once here and handed to the
            // pending surface rather than recomputed there: the running effect frame published to a
            // seat may name nothing this list does not. See SeatPendingSurface.NarrowResolution.
            var nameableCardIds = new HashSet<CardInstanceId>(
                ownCards.Concat(publicCards).Select(card => card.Id));
            return new SeatProjection(
                seat,
                source.Version,
                source,
                Array.AsReadOnly(ownCards),
                Array.AsReadOnly(publicCards),
                handSummaries,
                owner,
                ownsDecision ? NarrowContract(source.DecisionContract, seat) : null,
                AuthorizedCommands(source, seat, owner),
                new SeatPendingSurface(source, seat, ownsDecision, isActor, nameableCardIds),
                teamAgreement,
                isSeatUndoAvailable,
                undoMarker, experiments, teammateMarkers, coordinationRevision, coordinationGeneration, clock);
        }

        /// <summary>
        /// The standing decision's owner, read off whichever authoritative pending object the
        /// contract names.
        /// </summary>
        /// <remarks>
        /// Switching on the contract's own family, rather than testing the pending objects in some
        /// order of this method's choosing, is what keeps the two from disagreeing - and a family
        /// added without an owner rule fails loudly here instead of quietly belonging to nobody.
        /// </remarks>
        public static PendingDecisionOwner OwnerOfStandingDecision(MatchSessionProjection source) =>
            Delegated(source, DeclaredOwnerOfStandingDecision(source));

        /// <summary>
        /// Milestone 8.71b. The delegation standing over the selected actor's own action, or
        /// <see cref="DelegatedAction.None"/> - see <see cref="DelegatedActionQuery"/>.
        /// </summary>
        public static DelegatedAction DelegationOf(MatchSessionProjection source) =>
            source == null
                ? throw new ArgumentNullException(nameof(source))
                : source.IsTurnActive && source.SelectedActor.IsDefined
                    ? DelegatedActionQuery.ForActor(
                        source.Definitions, source.Cards, source.Heroes,
                        source.SelectedActor.ActorId)
                    : DelegatedAction.None;

        /// <summary>
        /// Milestone 8.71b. The hero whose seat may start and answer the selected actor's action.
        /// </summary>
        public static HeroId DecidingActor(MatchSessionProjection source)
        {
            var delegation = DelegationOf(source);
            return delegation.IsDefined
                ? delegation.DecidingHeroId
                : source.SelectedActor.ActorId;
        }

        /// <summary>
        /// Milestone 8.71b. The families a delegated action re-homes to the deciding seat, and the
        /// families it deliberately leaves where the authority put them.
        /// </summary>
        /// <remarks>
        /// <para><b>HA-Q3, as the user settled it on 2026-09-21.</b> The Ultimate hands over "the
        /// next action, and how it is performed", which is the three families a hero answers
        /// BECAUSE they are performing one: where their Movement goes, which unit their Attack
        /// takes, and every question their card text asks THEM.</para>
        /// <para>Every other family is named below rather than falling through, because a family
        /// nobody classified would belong to the delegation by accident:
        /// <list type="bullet">
        /// <item><b>Defense</b> - the page's own exception, and the user's: a hero's answer to
        /// somebody else's attack is never the action they are performing.</item>
        /// <item><b>HeroRespawn</b> and <b>RemovalPrevention</b> - the same shape as a Defense.
        /// They ask a hero about their own unit being removed, which is not how an action is
        /// performed, and a hero defeated INSIDE their delegated action still answers their own
        /// p.13 question.</item>
        /// <item><b>TurnRelease</b> - milestone 8.35g's question is whether this player's TURN is
        /// finished, asked after the action is over. It never reaches a delegation anyway: the
        /// action that ends resolves the card, and the rewrite the delegation is read from ends
        /// with it.</item>
        /// <item><b>SystemTransition</b> - p.15's leveling and p.6's blocked-respawn Place, both
        /// outside any action. The user's answer names end-of-round choices explicitly.</item>
        /// <item><b>Initiative</b>, <b>MinionBattle</b> and <b>Correction</b> - team-owned, so
        /// re-homing one would take a decision from three players the card does not mention. They
        /// are also structurally out of reach, because only a single-hero owner is ever
        /// re-homed.</item>
        /// </list></para>
        /// </remarks>
        public static readonly IReadOnlyList<PendingDecisionFamilyId> DelegatedDecisionFamilies =
            Array.AsReadOnly(new[]
            {
                PendingDecisionFamilies.Movement,
                PendingDecisionFamilies.AttackTarget,
                PendingDecisionFamilies.Effect,

                // Milestone 9.21b. Which body performs the action is how it is performed (RZ-Q4 picks it
                // as the action is chosen), so it goes to the seat that starts the action - the same seat
                // the start commands already go to under a delegation.
                PendingDecisionFamilies.ActingBody
            });

        /// <summary>Milestone 8.71b. The families a delegation never re-homes, stated rather than omitted.</summary>
        public static readonly IReadOnlyList<PendingDecisionFamilyId> UndelegatedDecisionFamilies =
            Array.AsReadOnly(new[]
            {
                PendingDecisionFamilies.Initiative,
                PendingDecisionFamilies.Defense,
                PendingDecisionFamilies.Correction,
                PendingDecisionFamilies.MinionBattle,
                PendingDecisionFamilies.HeroRespawn,
                PendingDecisionFamilies.SystemTransition,
                PendingDecisionFamilies.RemovalPrevention,
                PendingDecisionFamilies.TurnRelease,
                PendingDecisionFamilies.RevealRetrieval
            });

        /// <summary>
        /// Milestone 8.71b. The declared owner, re-homed to the deciding seat where a delegation
        /// covers it.
        /// </summary>
        /// <remarks>
        /// <para>It re-homes only a decision whose single owner IS the hero the delegation is
        /// about, and the delegation is read against the SELECTED ACTOR - so a card text that names
        /// a third hero as its chooser is untouched by construction. Hanu's own This Way! asks the
        /// friendly hero for a distance on that hero's seat (milestone 8.71e); a delegation over
        /// some other hero's turn must not swallow it, and cannot.</para>
        /// </remarks>
        private static PendingDecisionOwner Delegated(
            MatchSessionProjection source, PendingDecisionOwner declared)
        {
            if (!declared.IsDefined || declared.IsTeamOwned || declared.HeroIds.Count != 1 ||
                !DelegatedDecisionFamilies.Contains(declared.FamilyId))
            {
                return declared;
            }

            var delegation = DelegationOf(source);
            return delegation.Delegates(declared.HeroIds[0])
                ? PendingDecisionOwner.ForHero(declared.FamilyId, delegation.DecidingHeroId)
                : declared;
        }

        private static PendingDecisionOwner DeclaredOwnerOfStandingDecision(
            MatchSessionProjection source)
        {
            var contract = source.DecisionContract;
            if (contract == null)
            {
                return PendingDecisionOwner.None;
            }

            var family = contract.FamilyId;
            if (family.Equals(PendingDecisionFamilies.Initiative))
            {
                return PendingDecisionOwner.ForTeam(family, source.PendingInitiativeChoice.OwningSide);
            }

            if (family.Equals(PendingDecisionFamilies.Movement))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingMovementChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.AttackTarget))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingAttackTargetChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Defense))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingDefenseChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Effect))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingEffectChoice.ChooserId);
            }

            if (family.Equals(PendingDecisionFamilies.Correction))
            {
                return PendingDecisionOwner.ForTeam(
                    family, source.PendingDisplacedMinionCorrection.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.MinionBattle))
            {
                return PendingDecisionOwner.ForTeam(
                    family, source.PendingMinionBattleChoice.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.HeroRespawn))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingHeroRespawnChoice.HeroId);
            }

            if (family.Equals(PendingDecisionFamilies.ActingBody))
            {
                // Milestone 9.21b. The hero whose card resolved picks which of him acts (RZ-Q4).
                return PendingDecisionOwner.ForHero(family, source.PendingActingBodyChoice.HeroId);
            }

            if (family.Equals(PendingDecisionFamilies.SystemTransition))
            {
                // Milestone 8.35d. Rulebook p.15's Upgrade a Card is one player's own decision
                // about one player's own deck, and the paused item already says whose: the
                // level-up transition pauses once per purchased level and names the hero, the
                // Level and the half of it in the item id. Reading the owner back out of that id
                // restates the authority rather than deriving anything, in the same sense as every
                // other arm here, and it needs no new persisted or wire field.
                //
                // Live multiplayer testing on 2026-09-03 falsified the team reading below it: all
                // three seats of a side were authorized to answer one teammate's upgrade, and a
                // teammate consuming that answer is what made leveling look skipped for the player
                // whose hero it was about. Every other transition - milestone 6.11's blocked
                // respawn Place among them - is a question about the team's board and stays with
                // the side the authority named.
                //
                // Milestone 8.35u runs that same derivation ONCE PER OPEN ITEM, because p.15 now
                // asks every hero that bought a level at the same time. Evaluating ownership once
                // over a set of items is exactly how the 8.35d defect would come back one level up:
                // whichever hero happened to be named would receive every hero's upgrade commands
                // and the others would receive none, with the identical symptom - leveling looking
                // skipped for the player whose hero it was about.
                var step = source.PendingSystemTransitionStep;
                if (step.DefinitionId.Equals(
                        GuardsDigital.Core.Rules.Progression.HeroLevelUpSystemTransition.DefinitionId) &&
                    step.OpenItems.All(open => open.ItemId.IsDefined))
                {
                    return PendingDecisionOwner.ForHeroes(
                        family,
                        step.OpenItems.Select(open =>
                            GuardsDigital.Core.Rules.Progression.HeroLevelUpItem.Read(open.ItemId).HeroId));
                }

                // Every other transition - milestone 6.11's blocked respawn Place among them - stops
                // on one item at a time and is a question about the team's board, so it keeps the
                // side the authority named.
                return PendingDecisionOwner.ForTeam(family, step.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.RemovalPrevention))
            {
                return PendingDecisionOwner.ForHero(
                    family, source.PendingRemovalPrevention.DecidingHeroId);
            }

            if (family.Equals(PendingDecisionFamilies.TurnRelease))
            {
                // Milestone 8.35g. The turn belongs to the hero who just finished resolving it, and
                // so does the decision to hand it on. It is a hero rule rather than a team one for
                // the reason p.15's upgrade became one at milestone 8.35d: a teammate pressing it
                // takes a choice that was not theirs, and here it would end another player's turn.
                return PendingDecisionOwner.ForHero(family, source.PendingTurnRelease.ActorId);
            }

            if (family.Equals(PendingDecisionFamilies.RevealRetrieval))
            {
                return PendingDecisionOwner.ForHero(family, source.PendingRevealRetrieval[0].HeroId);
            }

            throw new InvalidOperationException(
                "Pending decision family '" + family.Value + "' has no seat-ownership rule. Every " +
                "family a projection can publish must name the seat that may answer it, or the " +
                "decision belongs to nobody and no client can be authorized to answer it.");
        }

        /// <summary>
        /// Every command this seat may submit against this exact projection.
        /// </summary>
        /// <remarks>
        /// <para>The order below is the authority's own: a standing decision suspends everything
        /// else, then the commitment the authority is currently asking for, then the selected
        /// actor's turn actions.</para>
        /// <para><c>AdvanceEndOfRound</c> is offered to nobody, and milestone 8.35d removed the arm
        /// that offered it to everybody. Milestone 8.30a's reading - that a command carrying no
        /// choice is "a continuation any seat may press" - was falsified by live multiplayer
        /// testing on 2026-09-03: six players racing one shared button stepped p.8's stages past
        /// each other's pauses, and the round read as stuck to everyone who was not the one seat
        /// holding the standing question. The sequence is now run by the seat-serving host itself
        /// in <see cref="MatchHost.SubmitForSeat"/>, one stage at a time, stopping only where a
        /// stage asks somebody something - so a seat's surface holds the answers it owns and never
        /// the round's pace. The Director's own direct surface still advances manually.</para>
        /// <para><c>BeginSystemTransition</c> is absent: no surface offers it and no rule gives a
        /// player the choice of starting one.</para>
        /// </remarks>
        public static IReadOnlyList<DirectorCommand> AuthorizedCommands(
            MatchSessionProjection source, MatchSeat seat, PendingDecisionOwner owner)
        {
            if (source.Phase == RoundResolutionPhase.GameEnded)
            {
                return Array.Empty<DirectorCommand>();
            }

            if (source.DecisionContract != null)
            {
                if (source.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.Initiative))
                {
                    // Player seats state bounded public preferences through the host agreement
                    // coordinator. Only Director mode receives the final rules command directly.
                    return Array.Empty<DirectorCommand>();
                }

                if (source.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))
                {
                    // Milestone 9.29. As the Initiative above: each choosing seat states its own
                    // picks through the host agreement coordinator, which submits the Selects and
                    // the Confirm once every pick matches. The shared board selection 8.30n handed
                    // to three seats at once is what the 2026-09-30 playtest could not use.
                    return Array.Empty<DirectorCommand>();
                }

                if (!owner.IsOwnedBy(seat))
                {
                    return Array.Empty<DirectorCommand>();
                }

                if (source.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.SystemTransition))
                {
                    // Milestone 8.35u. THE point of this milestone at this boundary. A pause that
                    // stands on several heroes publishes every hero's answers into one contract, and
                    // "this seat owns the contract" is no longer the same statement as "this seat may
                    // submit this command". Each command names the item it answers, and the item names
                    // the hero, so the filter restates milestone 8.35d's ownership rule per command
                    // rather than per contract. Without it, whichever seat matched first would be
                    // offered every hero's upgrades - the 8.35d live defect exactly, one level up.
                    return SystemTransitionCommandsFor(source.DecisionContract, seat);
                }

                return source.DecisionContract.Commands;
            }

            if (source.Phase == RoundResolutionPhase.EndOfRound)
            {
                // Milestone 8.35d. Nothing, and that is the whole of it: with no decision standing
                // inside end of round there is no question for a seat to answer, and the stage the
                // round is on is the host's to run.
                return Array.Empty<DirectorCommand>();
            }

            if (source.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                return CommitmentCommands(source, seat);
            }

            // Milestone 8.71b. The action's START commands - the primary, the secondary, p.12's
            // Hold and p.11's Fast Travel and p.13's Clear replacements - go to the seat that
            // DECIDES this actor's action rather than to the actor's own. With no delegation
            // standing those are the same seat and this is the line it always was.
            if (!source.IsTurnActive ||
                !source.SelectedActor.IsDefined ||
                !seat.Owns(DecidingActor(source)))
            {
                return Array.Empty<DirectorCommand>();
            }

            return TurnActionCommands(source);
        }

        /// <summary>
        /// Milestone 8.35u. The standing contract as this seat may see it.
        /// </summary>
        /// <remarks>
        /// <para>Narrowing <see cref="AuthorizedCommands"/> alone was NOT enough, and finding that out
        /// is the second half of R2. A seat's <c>DecisionContract</c> is read directly by the prompt
        /// presenters - <c>MatchFeaturePresenters</c> requires a contract of the matching family and
        /// draws from its commands - so a seat holding the whole contract would have been DRAWN every
        /// hero's upgrade options even while the host refused all but its own. That is both the 8.35d
        /// symptom at the surface and a privacy leak, because an option id names the two cards it
        /// moves and those are in another hero's hand.</para>
        /// <para>Only the system-transition family is narrowed here. Every other family is returned
        /// untouched deliberately: the Initiative and Minion-Battle arms of
        /// <see cref="AuthorizedCommands"/> withhold commands for reasons about who may EMIT them
        /// rather than about who may see the question, and rebuilding their contracts from the
        /// authorized set would change what those surfaces draw.</para>
        /// </remarks>
        private static PendingDecisionContract NarrowContract(
            PendingDecisionContract contract, MatchSeat seat)
        {
            if (contract == null ||
                !contract.FamilyId.Equals(PendingDecisionFamilies.SystemTransition))
            {
                return contract;
            }

            var own = SystemTransitionCommandsFor(contract, seat);
            return own.Count == contract.Commands.Count
                ? contract
                : own.Count == 0 ? null : new PendingDecisionContract(contract.FamilyId, own);
        }

        /// <summary>
        /// Milestone 8.35u. The answers in a system-transition contract that belong to this seat.
        /// </summary>
        /// <remarks>
        /// <para>Only rulebook p.15's leveling ever puts more than one hero's answers into one
        /// contract, and only its item ids name a hero, so every other transition falls through the
        /// last line and keeps the team-wide authorization the authority gave it - milestone 6.11's
        /// blocked respawn Place included, which is a question about the team's board and stays one.
        /// The filter is the same shape the Minion-Battle arm above already uses: an owned contract
        /// whose commands are still not all this seat's to send.</para>
        /// <para>Read as a rule: a seat may answer a level-up item when the item's own hero is the
        /// hero this seat plays. That is milestone 8.35d's sentence unchanged; what 8.35u changed is
        /// only how many times it has to be asked.</para>
        /// </remarks>
        private static IReadOnlyList<DirectorCommand> SystemTransitionCommandsFor(
            PendingDecisionContract contract, MatchSeat seat)
        {
            var levelUp = contract.Commands.Where(command =>
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                command.SystemTransitionChoice.ItemId.IsDefined &&
                IsLevelUpItem(command.SystemTransitionChoice.ItemId)).ToArray();
            if (levelUp.Length == 0)
            {
                return contract.Commands;
            }

            return Array.AsReadOnly(contract.Commands.Where(command =>
                !levelUp.Contains(command) ||
                seat.Owns(GuardsDigital.Core.Rules.Progression.HeroLevelUpItem
                    .Read(command.SystemTransitionChoice.ItemId).HeroId)).ToArray());
        }

        /// <summary>
        /// Whether an item id is one rulebook p.15's leveling minted - decided by trying to read it
        /// rather than by matching its shape, because the reader is the one authority on that encoding
        /// and a second copy of the format here could disagree with it.
        /// </summary>
        private static bool IsLevelUpItem(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId itemId)
        {
            try
            {
                GuardsDigital.Core.Rules.Progression.HeroLevelUpItem.Read(itemId);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// This seat's own commitment answer, offered to every seat the authority is still waiting
        /// on rather than to one at a time.
        /// </summary>
        /// <remarks>
        /// The Director's own surface asks each hero in turn, which is a serialization of one
        /// question a single controller has to ask somehow. Rulebook p.7 is not serialized: every
        /// player chooses a card facedown at the same time, and the commitment authority already
        /// tracks each hero independently. Six seats are where that difference becomes visible, so
        /// the seat surface authorizes every awaiting seat at once.
        /// </remarks>
        private static IReadOnlyList<DirectorCommand> CommitmentCommands(
            MatchSessionProjection source, MatchSeat seat)
        {
            var asked = source.Commitments.FirstOrDefault(value =>
                value.HeroId.Equals(seat.HeroId) &&
                value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            if (!asked.HeroId.IsDefined)
            {
                return Array.Empty<DirectorCommand>();
            }

            var commands = source.Cards
                .Where(card => card.OwnerId.Equals(asked.HeroId) &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .Select(card => DirectorCommand.CommitCard(asked.HeroId, card.Id))
                .ToList();
            if (asked.CanCommitTwoCards)
            {
                var handCards = source.Cards.Where(card => card.OwnerId.Equals(asked.HeroId) &&
                    card.State.Location == CardLocation.Hand)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal).ToArray();
                for (var first = 0; first < handCards.Length; first++)
                    for (var second = first + 1; second < handCards.Length; second++)
                        commands.Add(DirectorCommand.CommitTwoCards(
                            asked.HeroId, handCards[first].Id, handCards[second].Id));
            }
            if (asked.CanPass)
            {
                commands.Add(DirectorCommand.Pass(asked.HeroId));
            }

            return Array.AsReadOnly(commands.ToArray());
        }

        private static IReadOnlyList<DirectorCommand> TurnActionCommands(MatchSessionProjection source)
        {
            var commands = new List<DirectorCommand>();
            commands.AddRange(source.AvailableTurnActions
                .Where(offer => offer.IsAvailable)
                .Select(offer => DirectorCommand.StartAction(
                    offer.SourceCardInstanceId, offer.ActionDefinitionId)));
            foreach (var option in source.AvailableFastTravelOptions)
            {
                commands.AddRange(option.LegalDestinations.Select(destination =>
                    DirectorCommand.PerformFastTravel(
                        option.ReplacedMovementActionDefinitionId, destination)));
            }

            foreach (var option in source.AvailableClearOptions)
            {
                commands.AddRange(option.SubsetInvocations);
            }

            // The projected Hold offer rather than a fresh rules query: the authority already decided
            // it, and an application-layer surface that re-derived an offer would be the second
            // answer 8.7 removed.
            if (source.AvailableHoldOffer.HasValue && source.AvailableHoldOffer.Value.IsAvailable)
            {
                commands.Add(DirectorCommand.PerformHold(source.SelectedActor.CardInstanceId));
            }

            return Array.AsReadOnly(commands.ToArray());
        }

        /// <summary>
        /// Whether another hero's card may be named to this seat.
        /// </summary>
        /// <remarks>
        /// <para>This is rulebook p.4's Hidden Information section, restated: a player may always be
        /// asked to read "any of their unresolved cards, resolved cards, and discarded cards, as well
        /// as visible information on their dashboards", while "cards in the deck", "cards in hand"
        /// and "any facedown cards" are hidden. Both halves are needed - a facedown card is hidden
        /// wherever it lies, which p.7's rule that a resolved or discarded card can be facedown makes
        /// a real case and not a hypothetical one.</para>
        /// <para>The switch is fail-closed on purpose: a location this rule has not considered
        /// withholds the card rather than publishing it, so adding one is a visible gap in a client
        /// rather than a silent leak.</para>
        /// <para>Hand and deck are withheld <b>together</b>. A hero's printed deck is public
        /// knowledge, so a client that could read an opponent's deck could subtract it from the
        /// printed set and read the hand. What remains derivable is the union of the two, which is
        /// inherent to a known printed deck and is not something a projection can take back.</para>
        /// </remarks>
        private static bool IsPublic(
            CardInstance card,
            IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> publicLog)
        {
            if (card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return false;
            }

            // Milestone 8.40f. The four open zones are asked of `PublicCardZoneQuery` rather than
            // relisted, because a second reader now needs the identical list: `name-color@1`'s guess
            // domain subtracts what the open zones account for from a hero's printed deck, and a zone
            // the two disagreed about would rule out a colour no seat can actually see. The list is
            // the same four members; nothing is added and nothing removed, and the switch below is
            // still fail-closed, so a location this rule has not considered withholds the card.
            // Milestone 9.30b made the list five: the faceup removed-spell pile is open, while the
            // facedown spellbook falls through to the fail-closed default.
            if (PublicCardZoneQuery.IsAnOpenZone(card.State.Location))
            {
                return true;
            }

            switch (card.State.Location)
            {
                case CardLocation.Hand:
                    // A retrieved card remains known at the physical table after p.8 returns it.
                    // Retrieval resets lifecycle state so the card can be played again; the bounded
                    // public log, not a rules-facing card flag, is the memory of its revealed ID.
                    // Milestone 9.32g: until a later row moves it into the deck (RetrievedCardMemory).
                    return RetrievedCardMemory.Remembers(card, publicLog);
                default:
                    return false;
            }
        }
    }
}

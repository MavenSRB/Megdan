using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Where the cards a borrow may reach are standing, as the printed clause names them.
    ///
    /// <para>Two pools, because the roster prints two. Ursafar's Silver reaches "one of your
    /// <b>active</b> cards with an active effect" - the dashboard, in p.17's sideways sense - and his
    /// two borrowing Greens reach "one of your <b>discarded</b> cards", which is the same pool Tali's
    /// Commune with Spirits names.</para>
    ///
    /// <para>Neither pool is the hand, and that is the whole difficulty this vocabulary exists for: an
    /// action offer has always been built from the one card the turn committed, and these three faces
    /// start one from a card that is somewhere else entirely.</para>
    /// </summary>
    public enum BorrowedActionPool
    {
        None = 0,

        /// <summary>
        /// `one of your discarded cards` - rulebook p.10's Discarded state, owned by the borrowing
        /// hero. Ursafar's Instinctive Reaction and Evolutionary Response; Tali's Commune with Spirits,
        /// which narrows the same pool by colour.
        /// </summary>
        OwnedDiscardedCards = 1,

        /// <summary>
        /// `one of your active cards with an active effect` - Ursafar's Angry Roar, and the roster's
        /// only face printing two qualifiers on one pool.
        ///
        /// <para>The user's 2026-09-06 ruling on what those two words mean is what this member
        /// implements, and it is the reason both survive: "active card" is p.17's sideways signal - a
        /// card counting as active - while "with an active effect" asks whether the card's <em>text
        /// prints</em> an active-effect keyword at all. Before the Ultimate the two sets coincide, and
        /// the second qualifier looks redundant. After it, Unbound Fury makes every resolved card count
        /// as active, and the second qualifier is the only thing still narrowing the pool - so the card
        /// is written to stay correct under its own Ultimate, and a reading that collapsed the two
        /// would either widen the pool to every resolved card or refuse the widening the Tier IV card
        /// exists to give.</para>
        /// </summary>
        OwnedActiveCardsWithAnActiveEffect = 2,

        /// <summary>
        /// Milestone 8.38c. `your rightmost resolved card` - Swift's Reload! and her p.20 Handicap
        /// Backup, the roster's first faces to borrow from a pool defined by ORDER rather than by a
        /// card's state.
        ///
        /// <para><b>It names exactly one card, and that is the whole difference.</b> The two pools
        /// above are per-card predicates: a card is in the discard pile or it is not, it counts as
        /// active or it does not, and the answer for one card never depends on another. This one is a
        /// selection over an ordered set - rulebook p.6's turn area holds the cards a hero has
        /// resolved this round in committed order, and "rightmost" picks the last of them. So the
        /// candidate set is at most one card however many are standing there.</para>
        ///
        /// <para><b>It excludes the borrowing card itself.</b> Reload! is the card being resolved when
        /// its own text asks this question, and its assignment is already in the round's turn slots -
        /// so a selector that read the slots naively would name Reload! as its own rightmost resolved
        /// card. What the face means is the card resolved BEFORE this one, which is why the query
        /// takes the source instance and drops it. A borrow chain gets the same treatment for free.</para>
        ///
        /// <para><b>If that one card cannot supply a primary, the step is impossible rather than
        /// falling through to an earlier card.</b> The face names one card - "your rightmost resolved
        /// card" - and a reading that walked backwards until it found a borrowable one would be
        /// answering a sentence the card does not print. <see cref="BorrowRefusal"/>'s four arms apply
        /// to the selected card and nothing else, so a first turn with nothing resolved, and a
        /// rightmost card that prints no primary, both leave the pool empty by the same route.</para>
        ///
        /// <para>See row SW-Q1 in the Swift shard for the recorded question this leaves open:
        /// "rightmost" is the turn area's committed order here, because a committed order is what this
        /// engine has and a physical layout is not something it models.</para>
        /// </summary>
        OwnedRightmostResolvedCard = 3,

        /// <summary>
        /// Milestone 8.39f. `a card in the previous turn slot` - Cutter's Legend of the Skies, "The
        /// first time each turn after you perform a primary action, you may perform the primary action
        /// of a card in the previous turn slot."
        ///
        /// <para><b>A turn slot is printed on a hero's OWN dashboard, and that is what settles the
        /// scope this face leaves open.</b> The card says "<em>a</em> card" where every other borrow in
        /// this build says "one of <em>your</em> cards", which reads as though the slot might be a
        /// table-wide position holding six heroes' cards - and the engine's own
        /// <c>RoundTurnSlot</c> reinforces the impression, because it carries one assignment for every
        /// configured hero. The rulebook settles it outright at p.6 step 6: "place your card Faceup on
        /// the current Turn slot of <b>your hero dashboard</b>." There is no table-wide turn slot to
        /// read. Each hero has their own, the engine's per-turn record is an aggregate of the six, and
        /// "the previous turn slot" can only be the borrower's. So the possessive is absent from the
        /// printed sentence because "your" is already carried by "turn slot", not because the clause
        /// reaches across the table. Recorded as row CU-Q3 in the Cutter shard.</para>
        ///
        /// <para><b>It names a SLOT, where milestone 8.38c's pool names a POSITION IN AN ORDER, and
        /// that is why it is a fourth member rather than a second reading of the third.</b> "Your
        /// rightmost resolved card" walks back through the cards you have resolved this round and takes
        /// the last one still yours; this reads one slot by its index and takes whatever is standing in
        /// it. On the boards a match ordinarily reaches the two name the same card, because a hero
        /// commits one card per turn - but a slot the borrower passed in holds no card of theirs, and
        /// the two readings part company there: this pool is EMPTY, where Swift's would hand back the
        /// card from the slot before. Sharing the member would have made Cutter's face print Swift's
        /// sentence.</para>
        ///
        /// <para><b>Turn 1 of a round has no previous slot</b>, so the pool is empty - p.6 empties the
        /// turn area at the round boundary, and a clause naming a slot that is not there offers
        /// nothing. That is the same fail-closed answer milestone 8.38c gave the first turn, reached by
        /// naming a slot index below the first rather than by finding an empty order.</para>
        ///
        /// <para><b>If the card standing in that slot cannot supply a primary, the pool is empty rather
        /// than falling through to the slot behind it.</b> The face names one slot.
        /// <see cref="BorrowRefusal"/>'s arms apply to that card and nothing else, exactly as milestone
        /// 8.38c decided for the card its own face names.</para>
        /// </summary>
        PreviousTurnSlotCard = 4,

        /// <summary>Fight as One: owned resolved, face-up Skill cards in the turn area.</summary>
        OwnedResolvedSkills = 5,

        /// <summary>Dragon Knight WI-Q2: owned public face-up Skills, including the current card.</summary>
        OwnedFaceUpSkills = 6,

        /// <summary>
        /// Milestone 9.30c. A spell lying in a spellbook - Prepare Spells: "As the spell is cast, remove
        /// it from the spellbook faceup. The action on the spell card is performed by the hero casting
        /// the spell." The cast (<c>cast-spell@1</c>) asks this pool whether the spell its text named
        /// may be cast, BEFORE it moves the spell, so the borrow's ownership filter applies to it as to
        /// every other pool: today the caster casts only their own spells.
        ///
        /// <para><b>The book's owner and the caster are two inputs</b>, and this pool is the one place
        /// they are compared: the spell is found by its owner and its definition, and the caster is
        /// the borrower handed to <see cref="BorrowedPrimaryActionQuery.EligibleCards"/>. NebKher's
        /// gold card, which casts from Gydion's book (GY-05), is a change to that ownership filter for
        /// this pool and nothing else.</para>
        ///
        /// <para><b>No chooser is ever offered a card from this pool.</b> <c>borrow-primary-action@1</c>
        /// answers it with an empty pool: a spell is named by card text, so a book's facedown card
        /// identities never reach a decision.</para>
        /// </summary>
        OwnedSpellbookSpells = 7
    }

    /// <summary>
    /// THE BORROW CONTRACT. One card's printed primary action, performed off another card, as a
    /// declaration that names the pool and states what performing it means.
    ///
    /// <para><b>The rule this implements, in the user's words (2026-09-06).</b> A borrowed primary runs
    /// as a <b>full action instance</b> with the borrowed card's own values, text and icons - including
    /// its own <c>This round: You are enraged.</c> grant; the borrowed card does <b>not</b> change
    /// state; it is <b>not</b> a rulebook p.10 repeat; the borrowed card's supertype carries, so
    /// borrowing a basic card's primary is a basic action performed and feeds every trigger that reads
    /// one; and the turn still spends <b>one action in total</b>. The same ruling was given twice, once
    /// for Tali's Commune with Spirits and once for Ursafar's three faces, which is why it is written
    /// here once rather than inside either hero.</para>
    ///
    /// <para><b>Why the invariants are structural rather than checked.</b> Every one of them falls out
    /// of a single decision - that a borrow builds an ordinary <see cref="ActionOffer"/> whose
    /// <see cref="ActionOffer.SourceCardInstanceId"/> and
    /// <see cref="ActionOffer.SourceDefinitionReference"/> are the <em>borrowed</em> card's:</para>
    /// <list type="bullet">
    /// <item>Its values, Range, Radius and text are the borrowed card's because every value query and
    /// every card-text seam in this engine reads them off the offer's own source definition. Nothing
    /// had to be copied, so nothing can be copied wrongly.</item>
    /// <item>Its supertype is the borrowed card's for the same reason: <c>PerformedBasicAction</c>
    /// resolves the offer's source definition, so a borrowed Gold card is a basic Attack performed.</item>
    /// <item>The borrowed card does not change state because a printed Attack, Skill or Movement primary
    /// declares no <see cref="ActionCardStateTransition"/> - only a secondary Defense does - and the
    /// borrow never enters the turn's commitment path, which is the only other thing that moves a
    /// card. <see cref="Refuses"/> states that as a rule rather than leaving it to luck.</item>
    /// <item>It is invisible to every repeat reader because it is stamped
    /// <see cref="ActionInvocationContext.TextGranted"/>, and the whole repeat mechanism - the
    /// continuation invariant, the bounded-repeat count, the same-target binding, the snapshot's
    /// conditional write - keys on <see cref="ActionInvocationContext.Repeated"/> and on nothing
    /// else.</item>
    /// <item>The turn spends one action because a borrow is started from inside the borrowing card's
    /// own effect frame. The turn's action was spent committing that card; this instance is part of
    /// resolving its text, exactly as p.14's step-1 move inside an Attack is part of the Attack.</item>
    /// </list>
    ///
    /// <para><b>Tali's consumption surface</b> is <see cref="FromDiscardedCards"/> with a colour: her
    /// Commune with Spirits has an ally name one of p.9's six colours and then performs the primary
    /// action on a discarded card of that colour. Everything above is hers unchanged - the ruling was
    /// given about her card first - so her milestone declares a pool and composes the same capability,
    /// and adds only the naming step that chooses the colour. Ursafar's two Greens pass
    /// <see cref="CardColor.None"/>, which is this contract's way of writing "the face narrows
    /// nothing".</para>
    ///
    /// <para><b>What a newer printing would add, recorded rather than built.</b> The designer channel
    /// records a later printing of Ursafar's Silver carrying the help text "(You may choose to perform
    /// a replacement action instead)", and the designer confirming that replacement actions are then
    /// legal on a borrowed primary. Our extraction prints no such line and the pack-wide source ruling
    /// makes the extraction sole authority, so nothing here admits a replacement; the extension point
    /// is a third member of this declaration, and the printing question is in the ceremony's
    /// ask-the-designer queue.</para>
    /// </summary>
    public sealed class BorrowedPrimaryActionDeclaration : IEquatable<BorrowedPrimaryActionDeclaration>
    {
        private BorrowedPrimaryActionDeclaration(BorrowedActionPool pool, CardColor requiredColor)
        {
            Pool = pool;
            RequiredColor = requiredColor;
        }

        /// <summary>
        /// `one of your discarded cards`, optionally of one named colour.
        ///
        /// <paramref name="requiredColor"/> is <see cref="CardColor.None"/> where the face narrows
        /// nothing - Ursafar's two Greens - and one of p.9's six colours where a face names one, which
        /// is Tali's Commune with Spirits. The parameter is required rather than defaulted for the
        /// reason milestone 6.8f required the modification set: a narrowing dropped by omission
        /// silently widens a card's pool, and <c>None</c> is where a declaration that narrows nothing
        /// says so.
        /// </summary>
        public static BorrowedPrimaryActionDeclaration FromDiscardedCards(CardColor requiredColor)
        {
            if (!Enum.IsDefined(typeof(CardColor), requiredColor))
            {
                throw new ArgumentException(
                    "A borrow that narrows its pool by colour names one of rulebook p.9's colours.",
                    nameof(requiredColor));
            }

            return new BorrowedPrimaryActionDeclaration(
                BorrowedActionPool.OwnedDiscardedCards, requiredColor);
        }

        /// <summary>
        /// `one of your active cards with an active effect` - Angry Roar's pool, whose two qualifiers
        /// are both applied by <see cref="BorrowedPrimaryActionQuery"/>.
        ///
        /// It takes no colour because no face prints one on this pool, and a parameter no content uses
        /// is a reading nobody has checked.
        /// </summary>
        public static BorrowedPrimaryActionDeclaration FromActiveCardsWithAnActiveEffect() =>
            new BorrowedPrimaryActionDeclaration(
                BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect, CardColor.None);

        /// <summary>
        /// Milestone 8.38c. `your rightmost resolved card` - Reload!'s first bullet and Backup's whole
        /// sentence.
        ///
        /// It takes no colour for the reason the pool above takes none: neither face prints one, and a
        /// parameter no content uses is a reading nobody has checked.
        /// </summary>
        public static BorrowedPrimaryActionDeclaration FromRightmostResolvedCard() =>
            new BorrowedPrimaryActionDeclaration(
                BorrowedActionPool.OwnedRightmostResolvedCard, CardColor.None);

        /// <summary>
        /// Milestone 8.39f. `a card in the previous turn slot` - Legend of the Skies' whole borrow.
        ///
        /// It takes no colour for the reason the two pools above take none: the face prints none, and a
        /// parameter no content uses is a reading nobody has checked.
        /// </summary>
        public static BorrowedPrimaryActionDeclaration FromPreviousTurnSlotCard() =>
            new BorrowedPrimaryActionDeclaration(
                BorrowedActionPool.PreviousTurnSlotCard, CardColor.None);

        public BorrowedActionPool Pool { get; }

        public static BorrowedPrimaryActionDeclaration FromResolvedSkills() =>
            new BorrowedPrimaryActionDeclaration(BorrowedActionPool.OwnedResolvedSkills, CardColor.None);

        public static BorrowedPrimaryActionDeclaration FromFaceUpSkills() =>
            new BorrowedPrimaryActionDeclaration(BorrowedActionPool.OwnedFaceUpSkills, CardColor.None);

        /// <summary>Milestone 9.30c. A spell in the caster's own spellbook - the pool a cast asks.</summary>
        public static BorrowedPrimaryActionDeclaration FromSpellbook() =>
            new BorrowedPrimaryActionDeclaration(BorrowedActionPool.OwnedSpellbookSpells, CardColor.None);

        /// <summary>The colour this borrow narrows its pool to, or <see cref="CardColor.None"/>.</summary>
        public CardColor RequiredColor { get; }

        /// <summary>
        /// The rule kind of the standing declaration that makes a hero's resolved cards count as
        /// active - the widening Ursafar's Unbound Fury prints, and the roster's only one.
        ///
        /// <para>It is a rule kind rather than a card identity because that is what the engine already
        /// carries: <c>UnlockedUltimate</c> builds a <see cref="ContinuousRuleSource"/> from the
        /// Ultimate's own declared rule kind while the card sits beside the dashboard and its hero is
        /// on the board, so the widening lives exactly as long as p.16 says it does and nothing stores
        /// it.</para>
        /// </summary>
        public const string ResolvedCardsCountAsActiveRuleKind = "UrsafarUnboundFury";

        /// <summary>
        /// Whether <paramref name="heroId"/> currently has the standing declaration that makes their
        /// resolved cards count as active.
        /// </summary>
        public static bool ResolvedCardsCountAsActive(
            HeroId heroId, IEnumerable<ContinuousRuleSource> continuousSources) =>
            heroId.IsDefined &&
            (continuousSources ?? Enumerable.Empty<ContinuousRuleSource>()).Any(source =>
                source != null &&
                source.ControllerHeroId.Equals(heroId) &&
                string.Equals(
                    source.RuleKind,
                    ResolvedCardsCountAsActiveRuleKind,
                    StringComparison.Ordinal));

        /// <summary>
        /// The invocation context a borrowed instance is stamped with, stated once so that no consumer
        /// has to decide it and no reader has to guess what it means.
        ///
        /// <para><see cref="ActionInvocationContext.TextGranted"/> has existed since the offer
        /// vocabulary was built and has never had a producer. It is the right member and not a
        /// convenient one: a borrowed primary <em>is</em> an action granted by card text. What matters
        /// as much is what it is not - it is not <see cref="ActionInvocationContext.Repeated"/>, and
        /// that single fact is what makes a borrow invisible to every repeat reader in the engine
        /// without any of them being touched.</para>
        /// </summary>
        public const ActionInvocationContext InvocationContext = ActionInvocationContext.TextGranted;

        /// <summary>
        /// Why a candidate card cannot be borrowed from, checked before an offer is built. Each arm is
        /// a printed statement rather than an implementation limit.
        /// </summary>
        public static BorrowRefusal Refuses(CardDefinition definition)
        {
            if (definition == null)
            {
                return BorrowRefusal.UnresolvedDefinition;
            }

            var primary = definition.ActionDefinitions.FirstOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            if (primary == null)
            {
                // p.16's Ultimate is the case: it prints a permanent ability and no action at all, so
                // "the primary action on that card" names nothing. The user's Tali ruling says the same
                // thing from the other side - a Purple naming is legal and inert.
                return BorrowRefusal.NoPrintedPrimaryAction;
            }

            if (primary.TextComposition.IsAwaited)
            {
                // A face whose text is transcribed but not composed would perform a shortened version
                // of itself. p.20's Handicap cards are the standing instance.
                return BorrowRefusal.PrintedTextNotComposed;
            }

            if (primary.SourceCardStateTransition.IsDefined)
            {
                // The contract's own invariant, stated as a rule: the borrowed card does not change
                // state, so an action that moves its card cannot be borrowed. No printed primary in the
                // verified roster declares one - only a secondary Defense does - so this refuses
                // nothing today and would refuse the first face that broke the invariant silently.
                return BorrowRefusal.ActionMovesItsOwnCard;
            }

            return BorrowRefusal.None;
        }

        public bool Equals(BorrowedPrimaryActionDeclaration other) =>
            other != null && Pool == other.Pool && RequiredColor == other.RequiredColor;

        public override bool Equals(object obj) =>
            Equals(obj as BorrowedPrimaryActionDeclaration);

        public override int GetHashCode() => ((int)Pool * 397) ^ (int)RequiredColor;

        public override string ToString() =>
            RequiredColor == CardColor.None
                ? Pool.ToString()
                : $"{Pool} ({RequiredColor})";
    }

    /// <summary>Why one card cannot supply a borrowed primary action.</summary>
    public enum BorrowRefusal
    {
        None = 0,

        /// <summary>The candidate's definition is not in the supplied content set.</summary>
        UnresolvedDefinition = 1,

        /// <summary>The card prints no primary action - p.16's Ultimate.</summary>
        NoPrintedPrimaryAction = 2,

        /// <summary>The card's printed text is transcribed but not composed - a p.20 Handicap card.</summary>
        PrintedTextNotComposed = 3,

        /// <summary>The action would move its own card, which a borrow may never do.</summary>
        ActionMovesItsOwnCard = 4
    }

    /// <summary>
    /// Which of a hero's cards a borrow declaration can currently reach, and the two derivations the
    /// user's rulings turn on.
    ///
    /// <para>Pure and offline, deliberately. Ursafar's Silver is the face the whole hero is built
    /// around and its pool changes shape the moment his Ultimate is unlocked; a pool that could only be
    /// checked by playing him would be a rule nobody could prove. Everything here is a function of card
    /// definitions, card instances, the live Active effects and the registered capability catalog, so a
    /// desk test can put the Ultimate on the board and read the answer.</para>
    /// </summary>
    public static class BorrowedPrimaryActionQuery
    {
        /// <summary>
        /// Whether this card's <em>text prints</em> an active-effect keyword - p.17's "This turn:",
        /// "This round:", "Next turn:" and the rest of that family.
        ///
        /// <para><b>Derived from the composition, never declared beside it.</b> A card whose text
        /// prints one of those keywords is exactly a card whose composed text creates an Active effect,
        /// and creating one is a single capability shape - <see cref="ActiveEffectCreationHandler"/> -
        /// which every registered handler advertises through
        /// <see cref="IEffectCapabilityHandler.ActiveEffectDefinitions"/>. So the question is answered
        /// by walking what the card actually composes, and a face whose keyword line was dropped in
        /// transcription drops out of the pool at the same moment rather than lingering as a stale
        /// flag.</para>
        ///
        /// <para><b>It asks about the text and not about the effect.</b> The user's ruling is explicit
        /// that after the Ultimate the pool admits any resolved card whose text prints an active
        /// effect, "even one whose effect fizzled or expired". A composition contains its creation node
        /// whether or not that node ever ran, so this reading is true of a card whose conditional never
        /// fired - which is the case the ruling names.</para>
        /// </summary>
        public static bool PrintsAnActiveEffect(
            CardDefinition definition, EffectCapabilityCatalog capabilities)
        {
            if (definition == null || capabilities == null)
            {
                return false;
            }

            foreach (var action in definition.ActionDefinitions)
            {
                if (CreatesAnActiveEffect(action.EffectRoot, capabilities) ||
                    CreatesAnActiveEffect(action.SecondCardTextRoot, capabilities))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether one card counts as active in p.17's sense, for the hero it belongs to.
        ///
        /// <para>Two readings, and the second only exists because a printed card grants it. Ordinarily
        /// a card is active while it is the source of a live Active effect - that is precisely what
        /// turning it on its side signals, and it is the derivation the match projection has always
        /// used for the same word. Where the hero has the standing declaration that makes resolved
        /// cards count as active, every resolved card of theirs qualifies whether or not anything of
        /// its is still running.</para>
        /// </summary>
        public static bool CountsAsActive(
            CardInstance card,
            IEnumerable<ActiveEffectInstance> effects,
            bool resolvedCardsCountAsActive)
        {
            if (card == null)
            {
                return false;
            }

            if (resolvedCardsCountAsActive &&
                card.State.ResolutionStatus == CardResolutionStatus.Resolved)
            {
                return true;
            }

            return (effects ?? Enumerable.Empty<ActiveEffectInstance>()).Any(effect =>
                effect != null &&
                effect.Phase == ActiveEffectPhase.Active &&
                effect.SourceCardInstanceId.Equals(card.Id));
        }

        /// <summary>
        /// Every card <paramref name="borrowerId"/> may currently borrow a primary action from under
        /// <paramref name="declaration"/>, in card-instance order so the offer a player is shown is a
        /// fact rather than an accident of collection order.
        ///
        /// <para>Ownership is applied first and is never relaxed: all three printed clauses say
        /// "<b>your</b>" cards, and p.10 states outright that you can never reach another hero's.</para>
        /// </summary>
        public static IReadOnlyList<CardInstance> EligibleCards(
            BorrowedPrimaryActionDeclaration declaration,
            HeroId borrowerId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            IEnumerable<ActiveEffectInstance> effects,
            EffectCapabilityCatalog capabilities,
            bool resolvedCardsCountAsActive = false,
            IReadOnlyList<CardInstanceId> resolvedOrder = null)
        {
            if (declaration == null) throw new ArgumentNullException(nameof(declaration));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!borrowerId.IsDefined)
            {
                throw new ArgumentException(
                    "A borrow names the hero whose cards it reaches.", nameof(borrowerId));
            }

            var definitionsByReference = definitions
                .Where(definition => definition != null)
                .GroupBy(definition => definition.Reference)
                .ToDictionary(group => group.Key, group => group.First());
            var materializedEffects = (effects ?? Enumerable.Empty<ActiveEffectInstance>()).ToArray();

            var owned = instances
                .Where(card => card != null && card.OwnerId.Equals(borrowerId))
                .ToArray();

            // Milestone 8.38c. The ordered pool is narrowed BEFORE the per-card filters rather than
            // after, and the difference is the whole reading: "your rightmost resolved card" names one
            // card, so a card that cannot be borrowed from leaves the pool EMPTY instead of handing the
            // clause the card behind it. Narrowing afterwards would have walked backwards until it
            // found a borrowable card, which is a sentence Swift's faces do not print.
            if (declaration.Pool == BorrowedActionPool.OwnedRightmostResolvedCard)
            {
                owned = Rightmost(owned, resolvedOrder);
            }

            // Milestone 8.39f. The same discipline for a pool that names a SLOT rather than a position
            // in an order, and a separate narrowing rather than a second caller of Rightmost: that one
            // walks backwards until it finds a card the borrower still owns, which is right for "the
            // rightmost card you have resolved" and wrong for "the card in the previous turn slot",
            // where there is nothing behind the named slot to walk to.
            else if (declaration.Pool == BorrowedActionPool.PreviousTurnSlotCard)
            {
                owned = TheNamedSlotsCard(owned, resolvedOrder);
            }

            return Array.AsReadOnly(owned
                .Where(card => InPool(
                    declaration, card, materializedEffects, definitionsByReference, capabilities,
                    resolvedCardsCountAsActive))
                .Where(card => definitionsByReference.TryGetValue(
                        card.DefinitionReference, out var definition) &&
                    BorrowedPrimaryActionDeclaration.Refuses(definition) == BorrowRefusal.None &&
                    MatchesColor(declaration, definition))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// The last card of <paramref name="resolvedOrder"/> that the borrower still owns, as a set of
        /// at most one. Milestone 8.38c.
        ///
        /// <para>The order is supplied by the caller rather than derived here, because deriving it
        /// needs the round's turn slots and this file deliberately knows about cards and nothing else.
        /// <c>SyntheticBorrowedActionCapabilities</c> is where the slots are read, and it is also where
        /// the borrowing card itself is dropped.</para>
        ///
        /// <para>A null or empty order is an empty pool, not an unfiltered one. That is the first-turn
        /// case - nothing resolved yet - and it has to fail closed, because an unfiltered pool here
        /// would silently become "any card you own".</para>
        /// </summary>
        private static CardInstance[] Rightmost(
            CardInstance[] owned, IReadOnlyList<CardInstanceId> resolvedOrder)
        {
            if (resolvedOrder == null || resolvedOrder.Count == 0)
            {
                return Array.Empty<CardInstance>();
            }

            for (var index = resolvedOrder.Count - 1; index >= 0; index--)
            {
                var candidate = owned.FirstOrDefault(card => card.Id.Equals(resolvedOrder[index]));
                if (candidate != null)
                {
                    return new[] { candidate };
                }
            }

            return Array.Empty<CardInstance>();
        }

        /// <summary>
        /// The one card the named turn slot holds, if the borrower still owns it. Milestone 8.39f.
        ///
        /// <para>A slot holds exactly one assignment per hero and the caller has already narrowed to
        /// the borrower's, so anything but exactly one card id is a slot that names no card of theirs -
        /// the first turn of a round, which has no previous slot at all, and a slot they passed in with
        /// an empty hand, which holds an assignment and no card. Both are an empty pool rather than an
        /// unfiltered one, for the reason <see cref="Rightmost"/> records: an unfiltered pool here
        /// would silently become "any card you own".</para>
        ///
        /// <para><b>It does not walk backwards.</b> That is the whole difference from
        /// <see cref="Rightmost"/> and the whole of what "the previous turn slot" means.</para>
        /// </summary>
        private static CardInstance[] TheNamedSlotsCard(
            CardInstance[] owned, IReadOnlyList<CardInstanceId> slotContents)
        {
            if (slotContents == null || slotContents.Count != 1)
            {
                return Array.Empty<CardInstance>();
            }

            var candidate = owned.FirstOrDefault(card => card.Id.Equals(slotContents[0]));
            return candidate == null ? Array.Empty<CardInstance>() : new[] { candidate };
        }

        private static bool InPool(
            BorrowedPrimaryActionDeclaration declaration,
            CardInstance card,
            IReadOnlyList<ActiveEffectInstance> effects,
            IReadOnlyDictionary<CardDefinitionReference, CardDefinition> definitionsByReference,
            EffectCapabilityCatalog capabilities,
            bool resolvedCardsCountAsActive)
        {
            switch (declaration.Pool)
            {
                case BorrowedActionPool.OwnedDiscardedCards:
                    return card.State.Location == CardLocation.DiscardPile;

                case BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect:
                    // Both printed qualifiers, in the order the face prints them.
                    return CountsAsActive(card, effects, resolvedCardsCountAsActive) &&
                        definitionsByReference.TryGetValue(
                            card.DefinitionReference, out var definition) &&
                        PrintsAnActiveEffect(definition, capabilities);

                // Milestone 8.38c. The ordered narrowing above has already reduced this pool to the one
                // card the face names, so there is no per-card qualifier left to apply: whichever card
                // survived selection IS the rightmost resolved one. Stating that here rather than
                // letting it fall to the default is what keeps the switch exhaustive.
                case BorrowedActionPool.OwnedRightmostResolvedCard:
                    return true;

                // Milestone 8.39f. Same reason as the arm above: the slot narrowing has already reduced
                // this pool to the one card the face names, so whichever card survived selection IS the
                // card in the previous turn slot and there is no per-card qualifier left to apply.
                case BorrowedActionPool.PreviousTurnSlotCard:
                    return true;

                case BorrowedActionPool.OwnedResolvedSkills:
                    return CardLifecycleAuthority.IsResolvedCardInTurnArea(card) && IsFaceUpSkill(card, definitionsByReference);

                case BorrowedActionPool.OwnedFaceUpSkills:
                    return (card.State.Location == CardLocation.TurnArea || card.State.Location == CardLocation.DiscardPile) &&
                        IsFaceUpSkill(card, definitionsByReference);

                // Milestone 9.30c. In the book, facedown: a removed spell is cast only after a refill.
                case BorrowedActionPool.OwnedSpellbookSpells:
                    return card.State.Location == CardLocation.Spellbook;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(declaration), declaration.Pool,
                        "A borrow declaration names the pool its cards are standing in.");
            }
        }

        private static bool MatchesColor(
            BorrowedPrimaryActionDeclaration declaration, CardDefinition definition) =>
            declaration.RequiredColor == CardColor.None ||
            definition.Progression.Color == declaration.RequiredColor;

        private static bool IsFaceUpSkill(CardInstance card,
            IReadOnlyDictionary<CardDefinitionReference, CardDefinition> definitions) =>
            card.State.FaceOrientation == CardFaceOrientation.FaceUp &&
            definitions.TryGetValue(card.DefinitionReference, out var definition) &&
            definition.ActionDefinitions.Any(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == ActionKind.Skill);

        private static bool CreatesAnActiveEffect(
            EffectDefinition root, EffectCapabilityCatalog capabilities) =>
            root != null &&
            root.Nodes.Any(node =>
                node.Kind == EffectNodeKind.Capability &&
                node.Capability.IsDefined &&
                capabilities.TryLoad(node.Capability, out var handler) &&
                handler.ActiveEffectDefinitions.Count > 0);
    }
}

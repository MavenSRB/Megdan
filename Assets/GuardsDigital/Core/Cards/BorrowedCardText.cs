using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.61g. Where the cards a RIDER borrow may reach are standing, as Min's two Gold faces
    /// print the clause: "Apply the \"After the attack\" text of your resolved or discarded red card."
    ///
    /// <para><b>Why this is not a member of <see cref="BorrowedActionPool"/>.</b> That enum is the pool
    /// vocabulary of <see cref="BorrowedPrimaryActionDeclaration"/>, whose whole subject is a card's
    /// printed PRIMARY ACTION - <see cref="BorrowRefusal"/>'s four arms ask whether the candidate
    /// prints one, whether its text is composed, and whether performing it would move its own card.
    /// None of those three questions is the one Min's sentence asks. Her clause names a BODY OF CARD
    /// TEXT at one of rulebook p.14's numbered moments, and a card can satisfy either question while
    /// failing the other: a Red face printing a primary Attack with no "After the attack:" line
    /// supplies a borrowable primary and no borrowable rider, and nothing in the roster guarantees the
    /// converse either. Sharing the label would attach the primary-action rules to a sentence that does
    /// not carry them, which is exactly the split the project's reuse rule names.</para>
    ///
    /// <para><b>What IS shared is the reading of "your".</b> Ownership is applied first and is never
    /// relaxed, for the reason milestone 8.35z31 recorded on the primary pool: rulebook p.10 states
    /// outright that a hero can never reach another hero's cards.</para>
    /// </summary>
    public enum BorrowedCardTextPool
    {
        None = 0,

        /// <summary>
        /// `your resolved or discarded red card` - Min's Fast as Lightning, and her p.20 Handicap Chop
        /// Down when the established deferral is discharged.
        ///
        /// <para><b>Two of rulebook p.10's card states, and deliberately not the other two.</b> The
        /// page gives a played card three lives - in hand, played and unresolved, played and resolved -
        /// and a discard pile beside them. Min's sentence names the last two: a card that has been
        /// performed, and a card that has been discarded. A card still in her hand and a card she has
        /// committed but not yet performed supply nothing, which makes the rider p.10's impossible step
        /// and leaves the Attack itself completely unaffected. That is MI-Q7 in her shard, and it is
        /// the whole reason the pool is a state predicate rather than "her Red card".</para>
        ///
        /// <para><b>It is at most one card in practice and the engine does not rely on that.</b> p.15
        /// replaces a coloured card with its next Tier rather than adding one, so a hero holds one card
        /// of the Red line in a round; the untaken sibling becomes a p.16 item and an item is in
        /// neither of the two named states. Where the pool did somehow hold two, p.10 gives the
        /// undesignated decision to the card's controller and
        /// <c>SyntheticBorrowedCardTextCapabilities</c> opens exactly that choice - it never picks for
        /// her and it never invents a question for a pool of one.</para>
        /// </summary>
        OwnedResolvedOrDiscardedCards = 1
    }

    /// <summary>Why one card cannot supply a borrowed "After the attack" rider.</summary>
    public enum BorrowedCardTextRefusal
    {
        None = 0,

        /// <summary>The candidate's definition is not in the supplied content set.</summary>
        UnresolvedDefinition = 1,

        /// <summary>The card prints no primary action - rulebook p.16's Ultimate.</summary>
        NoPrintedPrimaryAction = 2,

        /// <summary>The card's printed text is transcribed but not composed - a p.20 Handicap card.</summary>
        PrintedTextNotComposed = 3,

        /// <summary>
        /// The card's primary prints no "After the attack:" text at all, so the clause's own subject -
        /// "the \"After the attack\" text of..." - names nothing on it.
        /// </summary>
        NoAfterTheAttackText = 4,

        /// <summary>
        /// The candidate is the very card whose text is asking. A rider that borrowed itself would run
        /// forever; the clause names another card of hers by construction, so this is a guard rather
        /// than a filter - see <see cref="BorrowedCardTextDeclaration.EligibleCards"/>.
        /// </summary>
        TheAskingCardItself = 5
    }

    /// <summary>
    /// THE RIDER-BORROW CONTRACT. One card's printed "After the attack" text, applied inside ANOTHER
    /// card's Attack, at rulebook p.14's step 7.
    ///
    /// <para><b>What performing it means, stated once so no consumer has to decide it.</b> The borrowed
    /// body is applied as an ordinary p.14 step-7 card text whose SOURCE CARD is the borrowed card.
    /// Every consequence follows from that one decision rather than from a rule written for each:</para>
    /// <list type="bullet">
    /// <item><b>"In radius" is the borrowed card's Radius.</b> <c>synthetic-card-radius@1</c> reads the
    /// frame's own source card, action and hero through <c>RadiusValueQuery</c>, so the printed icon and
    /// p.16's matching items are the RED card's - never Fast as Lightning's, which prints no Radius
    /// icon at all. The printed parenthesis, "(If it has radius, use that card's value)", is therefore
    /// satisfied by construction: nothing is copied, so nothing can be copied wrongly.</item>
    /// <item><b>"The target" is the enclosing Attack's target.</b> A card-text step at step 7 carries
    /// the attack its remainder belongs to, and the resolution context builds
    /// <c>EnclosingAttackFacts</c> from it - so the borrowed rider reads the unit Fast as Lightning
    /// just attacked. Where that unit was removed by the attack, a clause naming it has no referent and
    /// stops at p.10's impossible step, exactly as it would on the Red card itself.</item>
    /// <item><b>The borrowed card does not change state.</b> Applying card text moves no card: only a
    /// commitment path and a printed <c>ActionCardStateTransition</c> do, and neither is reached here.</item>
    /// <item><b>It is not a second action.</b> No <c>ActionEnded</c> and no
    /// <c>AttackResolutionCompleted</c> is produced for it - the enclosing Attack's own remainder still
    /// produces exactly one of each - so the scoreboard counts one Attack and rulebook p.10's one
    /// action per turn is untouched.</item>
    /// <item><b>Its pauses are this Attack's pauses.</b> A push distance, an optional move or an
    /// optional swap inside the borrowed body stops p.14 the way any step-7 question does, and the page
    /// is rebuilt from the paused frame's own identity - the borrowed card, its action, its effect
    /// body - plus the attack. Nothing new is persisted, so
    /// <c>AuthoritativeSnapshotSerializer.CurrentSchemaVersion</c> does not move.</item>
    /// </list>
    /// </summary>
    public sealed class BorrowedCardTextDeclaration : IEquatable<BorrowedCardTextDeclaration>
    {
        private BorrowedCardTextDeclaration(BorrowedCardTextPool pool, CardColor requiredColor)
        {
            Pool = pool;
            RequiredColor = requiredColor;
        }

        /// <summary>
        /// `your resolved or discarded red card`, narrowed to one of rulebook p.9's colours.
        ///
        /// <paramref name="requiredColor"/> is required rather than defaulted for the reason milestone
        /// 8.35z31 required it on the primary borrow: a narrowing dropped by omission silently widens a
        /// card's pool, and <see cref="CardColor.None"/> is how a declaration that narrows nothing says
        /// so. Both of Min's faces pass <see cref="CardColor.Red"/>.
        /// </summary>
        public static BorrowedCardTextDeclaration FromResolvedOrDiscardedCards(
            CardColor requiredColor)
        {
            if (!Enum.IsDefined(typeof(CardColor), requiredColor))
            {
                throw new ArgumentException(
                    "A borrow that narrows its pool by colour names one of rulebook p.9's colours.",
                    nameof(requiredColor));
            }

            return new BorrowedCardTextDeclaration(
                BorrowedCardTextPool.OwnedResolvedOrDiscardedCards, requiredColor);
        }

        public BorrowedCardTextPool Pool { get; }

        /// <summary>The colour this borrow narrows its pool to, or <see cref="CardColor.None"/>.</summary>
        public CardColor RequiredColor { get; }

        /// <summary>
        /// The moment a borrowed rider is read from and applied at. It is a constant rather than a
        /// parameter because the printed clause names one - the "After the attack" text - and a
        /// parameter no content uses is a reading nobody has checked.
        /// </summary>
        public const CardTextTiming BorrowedMoment = CardTextTiming.AfterTheAttack;

        /// <summary>
        /// The action whose <see cref="BorrowedMoment"/> body this card would supply, or <c>null</c>
        /// where it supplies none.
        ///
        /// <para>ONE function, asked by the pool, by the dispatcher that splices the borrowed step and
        /// by every fixture - so an offer and the step it produces cannot disagree about which body
        /// runs. It asks the same three questions the Attack backend's own <c>FindCardTextAction</c>
        /// asks of a card about to apply its text: the action is the card's printed primary, rulebook
        /// p.13's policy lets that card's text apply at all, and the action prints a body at this
        /// moment.</para>
        /// </summary>
        public static ActionDefinition AfterTheAttackAction(CardDefinition definition) =>
            definition?.ActionDefinitions.FirstOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies &&
                !action.TextComposition.IsAwaited &&
                action.CardTextAt(BorrowedMoment) != null);

        /// <summary>
        /// Why a candidate card cannot supply a borrowed rider, checked before an offer is built. Each
        /// arm is a printed statement rather than an implementation limit.
        /// </summary>
        public static BorrowedCardTextRefusal Refuses(CardDefinition definition)
        {
            if (definition == null)
            {
                return BorrowedCardTextRefusal.UnresolvedDefinition;
            }

            var primary = definition.ActionDefinitions.FirstOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            if (primary == null)
            {
                // p.16's Ultimate is the case: it prints a permanent ability and no action at all, so
                // "the \"After the attack\" text of that card" names nothing.
                return BorrowedCardTextRefusal.NoPrintedPrimaryAction;
            }

            if (primary.TextComposition.IsAwaited)
            {
                // A face whose text is transcribed but not composed would apply a shortened version of
                // itself. p.20's Handicap cards are the standing instance - Min's own Chop Down among
                // them, which is why this arm is not hypothetical for her set.
                return BorrowedCardTextRefusal.PrintedTextNotComposed;
            }

            return AfterTheAttackAction(definition) == null
                ? BorrowedCardTextRefusal.NoAfterTheAttackText
                : BorrowedCardTextRefusal.None;
        }

        /// <summary>
        /// Every card <paramref name="borrowerId"/> may currently borrow an "After the attack" rider
        /// from under <paramref name="declaration"/>, in card-instance order so the answer is a fact
        /// rather than an accident of collection order.
        ///
        /// <para>Pure and offline, deliberately, for the reason milestone 8.35z31 gave the primary
        /// pool: this is a function of card states and trusted definitions alone, so a desk test can
        /// put her Red card in each of rulebook p.10's four positions and read the answer without
        /// playing her.</para>
        /// </summary>
        /// <param name="askingCardInstanceId">
        /// The card whose text is asking, dropped from the pool. It cannot arise from either printed
        /// face - both are Gold and the pool is narrowed to Red - so this is a guard rather than a
        /// filter, and it is here rather than left to luck because a rider that borrowed itself would
        /// apply itself forever.
        /// </param>
        public static IReadOnlyList<CardInstance> EligibleCards(
            BorrowedCardTextDeclaration declaration,
            HeroId borrowerId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            CardInstanceId askingCardInstanceId = default)
        {
            if (declaration == null) throw new ArgumentNullException(nameof(declaration));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!borrowerId.IsDefined)
            {
                throw new ArgumentException(
                    "A borrow names the hero whose cards it reaches.", nameof(borrowerId));
            }

            if (declaration.Pool != BorrowedCardTextPool.OwnedResolvedOrDiscardedCards)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaration), declaration.Pool,
                    "A rider borrow names the pool its cards are standing in.");
            }

            var definitionsByReference = definitions
                .Where(definition => definition != null)
                .GroupBy(definition => definition.Reference)
                .ToDictionary(group => group.Key, group => group.First());

            return Array.AsReadOnly(instances
                .Where(card =>
                    card != null &&
                    card.OwnerId.Equals(borrowerId) &&
                    !card.Id.Equals(askingCardInstanceId) &&
                    IsResolvedOrDiscarded(card))
                .Where(card =>
                    definitionsByReference.TryGetValue(
                        card.DefinitionReference, out var definition) &&
                    Refuses(definition) == BorrowedCardTextRefusal.None &&
                    MatchesColor(declaration, definition))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// p.10's "resolved or discarded", asked through the lifecycle authority that already owns both
        /// words rather than restated here - so a state the two disagreed about would be a card the
        /// clause reached and the page does not.
        /// </summary>
        public static bool IsResolvedOrDiscarded(CardInstance card) =>
            CardLifecycleAuthority.IsResolvedCardInTurnArea(card) ||
            CardLifecycleAuthority.IsDiscardedRestingCard(card);

        private static bool MatchesColor(
            BorrowedCardTextDeclaration declaration, CardDefinition definition) =>
            declaration.RequiredColor == CardColor.None ||
            definition.Progression.Color == declaration.RequiredColor;

        public bool Equals(BorrowedCardTextDeclaration other) =>
            other != null && Pool == other.Pool && RequiredColor == other.RequiredColor;

        public override bool Equals(object obj) => Equals(obj as BorrowedCardTextDeclaration);

        public override int GetHashCode() => ((int)Pool * 397) ^ (int)RequiredColor;

        public override string ToString() =>
            RequiredColor == CardColor.None
                ? Pool.ToString()
                : $"{Pool} ({RequiredColor})";
    }
}

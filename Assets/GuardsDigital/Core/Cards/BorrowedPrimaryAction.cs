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
        OwnedActiveCardsWithAnActiveEffect = 2
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

        public BorrowedActionPool Pool { get; }

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
                effect.Phase != ActiveEffectPhase.Ended &&
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
            bool resolvedCardsCountAsActive = false)
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

            return Array.AsReadOnly(instances
                .Where(card => card != null && card.OwnerId.Equals(borrowerId))
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

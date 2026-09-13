using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.40f. The two capabilities behind the guessing game Bain's Blue line prints: a hero
    /// picks one of their own hand cards and KEEPS it, and a later node turns that card face up for
    /// the whole table and says whether a named colour matched it.
    ///
    /// <para><b>What was actually missing, measured rather than assumed.</b> Every card-valued choice
    /// in this build before these two terminates immediately in a discard, a retrieval, a borrow or a
    /// play, so the answer stops being interesting the instant it is given. Bain's sentence needs the
    /// answer to survive across the next node - visible to the chooser's seat and to nobody else -
    /// while the controller names a colour against it. The candidate half needed nothing at all:
    /// <c>EffectChoiceRequest.ChooserHeroId</c> has directed a pause to a named hero since milestone
    /// 7.1a, and a seat that does not own a pause is handed no pause. What needed building is the
    /// ANSWER's privacy, and that is an application-layer projection arm rather than anything here -
    /// see <c>SeatPendingSurface</c>. Nothing in this file knows about seats.</para>
    ///
    /// <para><b>Neither handler moves a card.</b> The choice publishes an identity and leaves the card
    /// exactly where it was; the reveal publishes a colour and a verdict and leaves it there too. What
    /// happens next - a discard of that one card, or coins - is the definition's own business, which
    /// is what lets three faces print three different consequences over one pair of nodes.</para>
    ///
    /// <para><b>Deferred, on milestone 7.9's gate.</b> These are new handler TYPES and no dealt card
    /// starts one, so they are declared here and registered by the dealing that first prints them.
    /// Each owes a showcase consumer then.</para>
    /// </summary>
    public static class SyntheticHiddenChoiceCapabilities
    {
        /// <summary>
        /// <c>hidden-card-choice@1</c> - "An enemy hero in radius with two or more cards in hand
        /// <b>chooses one of those cards</b>".
        ///
        /// <para><b>The chooser is a parameter, never a role</b>, for the reason
        /// <c>name-color@1</c>'s is: with three opponents on a side there is no single "the" opposing
        /// hero, and the hero who chooses is the one an earlier selection named. The pause is directed
        /// at that hero through <see cref="EffectChoiceRequest.ChooserHeroId"/>.</para>
        ///
        /// <para><b>It is a separate capability rather than a parameter on the discard</b>, and the
        /// difference is not a shade of meaning: <c>synthetic-discard-chosen-card</c>'s whole contract
        /// is that the chosen card is discarded, and a flag that made it sometimes not discard would
        /// be a second capability wearing the first one's name. What the two share - the criteria, the
        /// re-derivation on resume, the deterministic order - they share by asking the same query.</para>
        /// </summary>
        public static EffectCapabilityId HiddenCardChoice { get; } =
            new EffectCapabilityId("synthetic-hidden-card-choice");

        public static EffectCapabilityReference HiddenCardChoiceV1 =>
            new EffectCapabilityReference(HiddenCardChoice, 1);

        /// <summary>The hero whose hand is read and whose decision this is.</summary>
        public const string HiddenChoiceHeroParameter = "hero";

        /// <summary>The card that hero picked, published for a later node to bind.</summary>
        public const string ChosenCardOutput = "chosenCard";

        /// <summary>
        /// <c>reveal-chosen-card@1</c> - "then reveal the card", and the comparison the sentence
        /// immediately makes against it.
        ///
        /// <para><b>One node with two outputs, on <c>defended-card-color@1</c>'s argument.</b> That
        /// capability publishes <c>defended</c> and <c>color</c> together "so the gate and the colour
        /// are the same read ... One node rather than two keeps the predicate and the fact it gates
        /// from disagreeing about which defense the sentence means." The identical reasoning applies
        /// here and is why this build does not gain a general integer comparator: the revealed colour
        /// and the verdict about it are one reading of one card, and a
        /// <c>card-color@1</c> + <c>integers-equal@1</c> pair would be two. The recorded stance
        /// against definition-level arithmetic - "every card that printed a different number would
        /// declare the comparison again" - points the same way.</para>
        ///
        /// <para><b>The reveal is reported, not performed.</b> The card does not move and no flag is
        /// written on it. The handler reports an <see cref="RevealedCard"/>, the backend turns that
        /// into one seat-public log row naming hero and card, and
        /// <c>SeatProjection.IsPublic</c>'s hand arm makes the card nameable in every seat because
        /// the log names it. That is the memory a retrieved card already uses, which is what makes
        /// the reveal permanent for free - after a wrong guess the card goes back to a hand everybody
        /// has now seen it in, and the row does not expire until the bounded window does.</para>
        /// </summary>
        public static EffectCapabilityId RevealChosenCard { get; } =
            new EffectCapabilityId("synthetic-reveal-chosen-card");

        public static EffectCapabilityReference RevealChosenCardV1 =>
            new EffectCapabilityReference(RevealChosenCard, 1);

        /// <summary>The card to turn face up - bound from <see cref="ChosenCardOutput"/>.</summary>
        public const string RevealCardParameter = "card";

        /// <summary>
        /// The colour that was named, as a <see cref="CardColor"/> - bound from
        /// <c>name-color@1</c>'s own output.
        /// </summary>
        public const string NamedColorParameter = "namedColor";

        /// <summary>The revealed card's own printed colour, as a <see cref="CardColor"/>.</summary>
        public const string RevealedColorOutput = "revealedColor";

        /// <summary>Whether the named colour is the revealed card's colour.</summary>
        public const string MatchedOutput = "matched";

        /// <summary>
        /// Nothing here is registered by this build: no dealt card prints either sentence yet. The
        /// member exists so the offline content fixtures can compose a catalog of their own, exactly
        /// as every other deferred list in this folder is used.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new HiddenCardChoiceHandler(),
                new RevealChosenCardHandler()
            };

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// The cards a named hero could pick right now: their own unplayed hand, in the query's
        /// deterministic order.
        ///
        /// <para>Exposed beside the handler for the reason every candidate derivation in this build
        /// is: the offer, the feasibility probe and the commit must read ONE function, or a greyed
        /// control and the answer it refuses can come to disagree.</para>
        /// </summary>
        public static IReadOnlyList<CardInstanceId> ChoosableHandCards(
            EffectCapabilityContext context, HeroId chooser)
        {
            if (context == null || !context.CardState.IsAvailable || !chooser.IsDefined)
            {
                return Array.Empty<CardInstanceId>();
            }

            return CardCharacteristicQuery.FindCandidates(
                new CardCharacteristicCriteria(
                    ownerId: chooser,
                    locations: new[] { CardLocation.Hand },
                    hasBeenPlayed: false),
                context.CardState.Definitions,
                context.CardState.Instances);
        }

        private sealed class HiddenCardChoiceHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => HiddenCardChoiceV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            HiddenChoiceHeroParameter, EffectValueKind.Hero)
                    },
                    new[] { new EffectOutputBinding(ChosenCardOutput, EffectValueKind.CardInstance) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static HeroId Chooser(EffectCapabilityContext context)
            {
                var named = context.Input(HiddenChoiceHeroParameter);
                return named.Kind == EffectValueKind.Hero ? named.HeroId : default;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var candidates = ChoosableHandCards(context, chooser);

                // A step whose subject was never named, and a named hero holding nothing, are both
                // reported impossible rather than thrown - the printed clause narrows its own
                // selection to heroes holding two or more cards, so reaching this with an empty hand
                // means the board moved between the offer and the answer.
                if (!chooser.IsDefined || candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // A single candidate still opens the choice rather than being taken automatically,
                // which is this codebase's uniform precedent for a chooser-owned selection: the
                // chooser owns the decision even when it has one option. It matters more here than
                // anywhere else, because the guesser can count the chooser's hand and a hand of one
                // that answered itself would tell them the card was never chosen at all.
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.None,
                    candidates.Select(EffectValue.Card),
                    1,
                    1,
                    chooserHeroId: chooser));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.CardInstance)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived, never trusted: the answer is checked against the hand as it stands now,
                // so a card played or discarded between the offer and the answer is refused rather
                // than named to the table a moment later.
                var candidates = ChoosableHandCards(context, Chooser(context));
                if (!candidates.Contains(selected[0].CardInstanceId))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        ChosenCardOutput, EffectValue.Card(selected[0].CardInstanceId))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                ChoosableHandCards(context, Chooser(context)).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                ChoosableHandCards(context, Chooser(context)).Select(EffectValue.Card).ToArray();

            /// <summary>One card, which is what the offer requests.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;
        }

        private sealed class RevealChosenCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RevealChosenCardV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            RevealCardParameter, EffectValueKind.CardInstance),
                        new EffectCapabilityParameter(
                            NamedColorParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<CardColor>())
                    },
                    new[]
                    {
                        new EffectOutputBinding(RevealedColorOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(MatchedOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var cardId = context.Input(RevealCardParameter).CardInstanceId;
                var instance = !context.CardState.IsAvailable || !cardId.IsDefined
                    ? null
                    : context.CardState.Instances.FirstOrDefault(card => card.Id.Equals(cardId));
                var definition = instance == null
                    ? null
                    : context.CardState.Definitions.FirstOrDefault(
                        value => value.Reference.Equals(instance.DefinitionReference));

                // A card nobody named, or one that has left the state this text is about, cannot be
                // revealed - and a mandatory step that cannot be performed says so rather than
                // publishing a colour of None and letting a Conditional read the absence as "wrong".
                if (definition == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var revealed = definition.Progression.Color;
                var named = (CardColor)context.Input(NamedColorParameter).IntegerValue;

                // CardColor.None on either side is a miss and never a match, which is what keeps an
                // unbound guess from matching an unreadable card.
                var matched = named != CardColor.None && revealed != CardColor.None && named == revealed;

                return EffectExecutionResult.Applied(
                    new[]
                    {
                        new EffectOutcomeFact(
                            RevealedColorOutput, EffectValue.Integer((int)revealed)),
                        new EffectOutcomeFact(MatchedOutput, EffectValue.Boolean(matched))
                    },
                    revealedCards: new[] { new RevealedCard(instance.OwnerId, instance.Id) });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var cardId = context.Input(RevealCardParameter).CardInstanceId;
                return context.CardState.IsAvailable && cardId.IsDefined &&
                    context.CardState.Instances.Any(card => card.Id.Equals(cardId))
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.ImpossibleMandatory;
            }
        }
    }
}

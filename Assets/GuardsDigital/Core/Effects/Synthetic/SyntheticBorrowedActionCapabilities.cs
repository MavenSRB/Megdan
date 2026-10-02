using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z31's <c>borrow-primary-action@1</c>: the one capability that names a card
    /// whose printed primary action is about to be performed off it.
    ///
    /// <para>It is one capability and not three, although three faces of Ursafar's print the clause and
    /// a fourth of Tali's prints it again, because the user's ruling made all four the same rule. What
    /// differs between them is the pool and the colour narrowing, and both of those are inputs.
    /// <see cref="BorrowedPrimaryActionDeclaration"/> is where the rule itself is written down.</para>
    ///
    /// <para><b>What this capability does and deliberately does not do.</b> It chooses the card. It does
    /// not start the action - a capability handler mutates the frame it is in, and a borrowed primary is
    /// a whole action instance with its own resolution, its own pauses and its own card-text seams, so
    /// starting it belongs to the same authority that starts every other action. The shape is milestone
    /// 6.7's exactly: <see cref="EffectNodeKind.RepeatAction"/> "performs nothing itself and invokes no
    /// capability: it records the request on the frame, and the card-action dispatcher creates the
    /// second instance". This records its request the same way, and
    /// <see cref="BorrowedActionQuery"/> is how the dispatcher reads it back - the direct twin of
    /// <c>RepeatedActionQuery.Requested</c>, and separate from it on purpose, because a borrow must be
    /// invisible to every repeat reader.</para>
    ///
    /// <para><b>Deferred, not registered</b>, for the reason
    /// <see cref="SyntheticConditionCapabilities"/> records: the registered set is serialized, and the
    /// only content naming this is a hero this build does not deal.</para>
    /// </summary>
    public static class SyntheticBorrowedActionCapabilities
    {
        public static EffectCapabilityId BorrowPrimaryAction { get; } =
            new EffectCapabilityId("borrow-primary-action");

        public static EffectCapabilityReference BorrowPrimaryActionV1 =>
            new EffectCapabilityReference(BorrowPrimaryAction, 1);

        /// <summary>Which pool the clause reaches, as a closed vocabulary.</summary>
        public const string PoolParameter = "pool";

        /// <summary>
        /// The colour the clause narrows its pool to, as a closed vocabulary including
        /// <see cref="CardColor.None"/> for a face that narrows nothing. It is a required parameter
        /// rather than an optional one so that a composition cannot narrow nothing by accident.
        /// </summary>
        public const string ColorParameter = "color";

        /// <summary>The card whose printed primary the borrowing hero is about to perform.</summary>
        public const string BorrowedCardOutput = "borrowedCard";
        public const string ExcludeAttackedUnitParameter = "excludeAttackedUnit";
        public const string ExcludedHeroOutput = "excludedHero";
        public const string ExcludedMinionOutput = "excludedMinion";

        /// <summary>
        /// Milestone 8.37's <c>friendly-hero-in-play@1</c>: Commune with Spirits' "A
        /// friendly hero in play" - the mandatory selection that names WHICH friendly hero performs the
        /// naming step below, published so <see cref="NameColorV1"/> can bind its own chooser from it.
        ///
        /// <para><b>Why this is a new selection rather than the printed-Radius hero family.</b> Every
        /// existing hero selection in the build is anchored on p.9's Range or Radius icon, which
        /// Commune with Spirits prints neither of - her whole offer set is the primary, the Defense
        /// secondary and Hold. "In play" is p.19's own board membership, unqualified by distance:
        /// RULING 20 reads it as ON THE BOARD, so a defeated ally awaiting respawn - who has left the
        /// board's occupancy entirely - is not a candidate and the mandatory step cannot complete
        /// without one, which is exactly p.10's rule for a mandatory step with no legal subject.</para>
        ///
        /// <para><b>"Friendly" includes the actor</b>, by the standing reading every earlier "friendly"
        /// selection in this build already carries: unlike RULING 8's "a hero", which needed an explicit
        /// user ruling to admit self because p.17 states a special exclusion for "friendly" that a bare
        /// "a hero" lacks, "a friendly hero" is squarely inside the ordinary meaning that exclusion
        /// exists to carve OUT of - so Tali naming herself needs no ruling to be legal, and is in fact
        /// her only candidate whenever she plays this Skill alone.</para>
        ///
        /// <para><b>The chooser is the actor, not the named hero.</b> p.10 gives an unassigned decision
        /// to the card's controller: which teammate performs the naming is Tali's own choice among her
        /// legal candidates, exactly as Silverarrow's Blessing selects which hero retrieves before that
        /// hero's own "may" takes over. What the SELECTED hero then owns is the naming itself, which
        /// <see cref="NameColorV1"/> directs to them by binding its own <c>chooser</c> parameter from
        /// this node's published output - the same two-step shape <c>retrieve-card@1</c> already
        /// proves.</para>
        /// </summary>
        public static EffectCapabilityId FriendlyHeroInPlay { get; } =
            new EffectCapabilityId("friendly-hero-in-play");

        public static EffectCapabilityReference FriendlyHeroInPlayV1 =>
            new EffectCapabilityReference(FriendlyHeroInPlay, 1);

        /// <summary>The chosen friendly hero, published for a later node to bind.</summary>
        public const string ChosenFriendlyHeroOutput = "chosenFriendlyHero";

        /// <summary>
        /// Milestone 8.37's <c>name-color@1</c>: a bound hero mandatorily names one of
        /// p.9's six printed colours.
        ///
        /// <para><b>The runtime chooser is a parameter, not a role.</b> Every chooser role this build
        /// declares - <see cref="EffectChooserRole.Actor"/>, <c>Controller</c>, <c>OpposingHero</c>,
        /// <c>BoundHero</c> - names either a fixed party to the action or a hero a CONTROL node
        /// identifies through its own dedicated input. A capability LEAF directs its pause to an
        /// explicit hero the same way Whisper's <c>defended-card-forced-move@1</c> already does -
        /// through <see cref="EffectChoiceRequest.ChooserHeroId"/>, which milestone 7.1a documented as
        /// taking precedence over the role - so this reads its <c>chooser</c> input parameter (bound
        /// <see cref="EffectInputBinding.FromNodeOutput"/> from <see cref="FriendlyHeroInPlayV1"/>'s own
        /// published hero) and opens the choice to exactly that hero, never to the card's controller.</para>
        ///
        /// <para><b>Mandatory, not optional, per the designer's own words.</b> Arty's 8/15 ruling is
        /// explicit: "No, discard step is optional ('may'). Naming the color is not." So this capability
        /// is never wrapped in an <see cref="EffectFailurePolicy.May"/> or <c>IfAble</c> node, and it
        /// offers all six colours unconditionally - not narrowed by whose hand holds what, because
        /// "Naming the color is not [required to have cards], the action you are performing is
        /// referring to the color" (the same ruling, second half).</para>
        ///
        /// <para><b>All six are offered by default, including the newly-inert
        /// <see cref="CardColor.Purple"/>.</b> RULING 21 makes every one of p.9's colours nameable,
        /// and Commune with Spirits narrows nothing: the naming step and the hand are deliberately
        /// independent there (Arty's ruling again), so narrowing ITS offer would be inventing a
        /// restriction that card does not print.</para>
        ///
        /// <para><b>Which card asks which question, stated because the two readings sit side by
        /// side.</b> Milestone 8.40f adds <see cref="RestrictToColorsOwnedByParameter"/> for a card
        /// that DOES print one - Bain's A Game of Chance, "(You can only guess colors that could be
        /// in that player's hand)". The paragraph above is Tali's ruling about Tali's sentence rather
        /// than a property of this capability, and the narrowing is optional and unbound for her, so
        /// her offer is unchanged. What the narrowing reads is still never a hand: see
        /// <see cref="GuessableColors"/>.</para>
        /// </summary>
        public static EffectCapabilityId NameColor { get; } = new EffectCapabilityId("name-color");

        public static EffectCapabilityReference NameColorV1 =>
            new EffectCapabilityReference(NameColor, 1);

        /// <summary>The hero this naming step is directed to - a required parameter, never the actor.</summary>
        public const string NameColorChooserParameter = "chooser";

        /// <summary>The colour that hero named, published for the borrow node to bind.</summary>
        public const string NamedColorOutput = "namedColor";

        /// <summary>
        /// Milestone 8.40f. The hero whose OWN deck bounds the offer, or unbound for the
        /// all-six reading every declaration before Bain's Blue line has. Optional, so Commune with
        /// Spirits keeps its exact candidate set without naming it.
        ///
        /// <para>A parameter rather than a sibling <c>guess-color@1</c>, on catalog rule 11: the
        /// semantics are identical - a bound hero names one of p.9's colours - and only the admitted
        /// set differs, which is what <c>synthetic-discard-chosen-card</c>'s optional <c>color</c>
        /// and <c>retrieve-card@1</c>'s optional <c>restrictToCard</c> already are. A sibling would
        /// also move the serialized registered-handler count and every pinned corpus digest, which a
        /// parameter cannot.</para>
        /// </summary>
        public const string RestrictToColorsOwnedByParameter = "restrictToColorsOwnedBy";

        /// <summary>Every colour <see cref="NameColorV1"/> may offer, in p.9's own printed order.</summary>
        public static IReadOnlyList<CardColor> AllNameableColors { get; } = Array.AsReadOnly(new[]
        {
            CardColor.Gold, CardColor.Silver, CardColor.Red, CardColor.Green, CardColor.Blue,
            CardColor.Purple
        });

        /// <summary>
        /// Milestone 8.40f. The colours a guess against <paramref name="target"/> may name - p.9's
        /// six narrowed to those PUBLIC information cannot rule out of that hero's hand.
        ///
        /// <para><b>It never reads a hand, and that is the whole clause.</b> A Game of Chance prints
        /// "(You can only guess colors that could be in that player's hand)", and the tempting
        /// implementation - offer the colours actually in the hand - would leak, from the authority
        /// to the guesser, the exact fact the reveal exists to sell. The offer is a projection and
        /// the colours in a hand are hidden information, so an offer derived from them is an
        /// authorization filter doing a projection filter's job. Milestone 8.35u's lesson, stated on
        /// the sharpest case this roster has.</para>
        ///
        /// <para><b>What it reads instead is the subtraction any player can do at the table.</b> A
        /// hero's printed deck is public, and every zone but the hand, the deck and a facedown card
        /// is open - so a colour is guessable exactly when that hero owns more cards of it than the
        /// open zones currently account for. <see cref="Cards.PublicCardZoneQuery"/> is the same
        /// reading <c>SeatProjection.IsPublic</c> uses, asked here rather than restated, because a
        /// zone the two disagreed about would be a colour ruled out that no seat can see.</para>
        ///
        /// <para><b>Purple is never guessable</b>, and it is excluded by construction rather than by
        /// name: p.16's Ultimate sits in the Ultimate area, which is an open zone, so a hero holding
        /// theirs has it fully accounted for - and one whose Ultimate is still in the deck is caught
        /// by the explicit exclusion below, because p.9's Purple never enters a hand at all.</para>
        ///
        /// <para><b>Set aside counts as hidden</b>, which is the conservative direction: p.15 tucks
        /// the untaken card of an upgrade pair under the dashboard facedown, so the open zones do not
        /// account for it and its colour stays guessable. Ruling it out would name a card nobody can
        /// read.</para>
        /// </summary>
        public static IReadOnlyList<CardColor> GuessableColors(
            EffectCapabilityContext context, HeroId target)
        {
            // A narrowing that cannot read the cards narrows by the one thing that is a rule rather
            // than a board fact - p.9's Purple never enters a hand - and by nothing else. Widening is
            // always safe here and narrowing on an unreadable board would not be: a colour ruled out
            // by a state this function could not see is a colour ruled out on no evidence.
            var everyGuessable = Array.AsReadOnly(AllNameableColors
                .Where(color => color != CardColor.Purple)
                .ToArray());
            if (context == null || !context.CardState.IsAvailable || !target.IsDefined)
            {
                return everyGuessable;
            }

            // Milestone 9.30c. A spell lies in a spellbook or faceup beside it and is never in a hand,
            // so it is no card "that could be in that player's hand": a facedown spell counted here
            // would make its colour guessable on no evidence.
            var owned = context.CardState.Instances
                .Where(card => card != null && card.OwnerId.Equals(target) &&
                    !Cards.SpellbookQuery.IsInASpellbook(card.State.Location))
                .ToArray();
            if (owned.Length == 0)
            {
                return Array.Empty<CardColor>();
            }

            var definitions = context.CardState.Definitions;
            return Array.AsReadOnly(everyGuessable
                .Where(color =>
                {
                    var ofColor = owned
                        .Where(card => ColorOf(card, definitions) == color)
                        .ToArray();
                    return ofColor.Length >
                        ofColor.Count(Cards.PublicCardZoneQuery.IsPubliclyVisible);
                })
                .ToArray());
        }

        private static CardColor ColorOf(
            CardInstance card, IEnumerable<CardDefinition> definitions)
        {
            var definition = definitions?.FirstOrDefault(
                value => value.Reference.Equals(card.DefinitionReference));
            return definition?.Progression.Color ?? CardColor.None;
        }

        /// <summary>
        /// Milestone 8.35z50 registered the borrow handler beside the hero whose dealt cards start it.
        /// Shared with Tali on ledger row 25's rule - deferred only until the FIRST of its sharers is
        /// dealt - so it crossed with Ursafar and her own dealing finds it already here.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new BorrowPrimaryActionHandler(),
                // Milestone 8.37. Commune with Spirits is a dealt basic Skill, so both halves of her
                // signature - the ally it names and the colour that ally names - are started by a
                // printed card this build puts in a hand.
                new FriendlyHeroInPlayHandler(),
                new NameColorHandler()
            };

        /// <summary>
        /// Nothing is deferred here any longer; the member survives for the offline content tests
        /// that compose catalogs of their own from the deferred lists.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Every friendly hero currently on the board, actor's team, actor included - the candidate set
        /// <see cref="FriendlyHeroInPlayV1"/> offers, exposed beside the handler for the same reason
        /// <see cref="Candidates"/> above is: feasibility and the offer must read one function.
        /// </summary>
        public static IReadOnlyList<HeroId> FriendlyHeroesInPlay(EffectCapabilityContext context)
        {
            if (context == null || context.Components?.Board == null || context.Participants == null ||
                !context.ActorId.IsDefined)
            {
                return Array.Empty<HeroId>();
            }

            var actorSide = context.Participants.GetTeamForHero(context.ActorId).Side;
            return context.Components.Board.Occupancies
                .Where(occupancy =>
                    context.Participants.GetTeamForHero(occupancy.HeroId).Side == actorSide)
                .Select(occupancy => occupancy.HeroId)
                .Distinct()
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class FriendlyHeroInPlayHandler : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                FriendlyHeroesInPlay(context).Select(EffectValue.Hero).ToArray();

            // Milestone 9.6. What each answer would publish, so an offer can follow every answer
            // to what the card does after it - the answers themselves are never narrowed here.
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Resume(context, chosen);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => FriendlyHeroInPlayV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[]
                {
                    new EffectOutputBinding(ChosenFriendlyHeroOutput, EffectValueKind.Hero)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = FriendlyHeroesInPlay(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected != null && selected.Count == 1 &&
                    selected[0].Kind == EffectValueKind.Hero &&
                    FriendlyHeroesInPlay(context).Contains(selected[0].HeroId)
                        ? selected[0].HeroId
                        : default;
                if (!chosen.IsDefined)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ChosenFriendlyHeroOutput, EffectValue.Hero(chosen))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                FriendlyHeroesInPlay(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private sealed class NameColorHandler : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Chooser(context).IsDefined
                    ? Offered(context).Select(color => EffectValue.Integer((int)color)).ToArray()
                    : Array.Empty<EffectValue>();

            // Milestone 9.6. What each answer would publish, so an offer can follow every answer
            // to what the card does after it - the answers themselves are never narrowed here.
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Resume(context, chosen);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => NameColorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(NameColorChooserParameter, EffectValueKind.Hero),
                    new EffectCapabilityParameter(
                        RestrictToColorsOwnedByParameter, EffectValueKind.Hero, isRequired: false)
                },
                new[] { new EffectOutputBinding(NamedColorOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static HeroId Chooser(EffectCapabilityContext context) =>
                context.Input(NameColorChooserParameter).HeroId;

            /// <summary>
            /// The colours this node may offer right now. Milestone 8.40f: ONE function, asked by the
            /// offer, the probe and the resume, so a domain that narrowed for one of them cannot
            /// widen for another.
            /// </summary>
            private static IReadOnlyList<CardColor> Offered(EffectCapabilityContext context)
            {
                var bound = context.Input(RestrictToColorsOwnedByParameter);
                return bound.Kind == EffectValueKind.Hero && bound.HeroId.IsDefined
                    ? GuessableColors(context, bound.HeroId)
                    : AllNameableColors;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var offered = Offered(context);
                if (!chooser.IsDefined || offered.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.37 declares the vocabulary, so the hero this is put to is offered
                // COLOURS rather than the digits 0 through 5. The chooser is not the acting seat, so
                // this is also the first choice in the build whose labels are drawn for somebody
                // other than the player who played the card.
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.None,
                    offered.Select(color => EffectValue.Integer((int)color)),
                    1,
                    1,
                    chooserHeroId: chooser,
                    valueVocabulary: Rules.EffectResolution.EffectChoiceValueVocabulary.CardColor));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.40f. Checked against the NARROWED set, re-derived rather than trusted,
                // so an answer naming a colour the domain excludes is refused however it arrived.
                var color = (CardColor)selected[0].IntegerValue;
                if (!Offered(context).Contains(color))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(NamedColorOutput, EffectValue.Integer((int)color))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Chooser(context).IsDefined && Offered(context).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// The candidate set one borrow node would offer right now, as a function of state alone.
        ///
        /// <para>Exposed beside the handler because feasibility asks the same question the offer does:
        /// milestone 8.35z15's rule is that a card-text step with no legal subject is not offered, and
        /// Angry Roar played with an empty pool is exactly that step. Both readings therefore come from
        /// one function, which is what stops an offer and its greying from disagreeing.</para>
        /// </summary>
        public static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
        {
            if (context == null || !context.CardState.IsAvailable || !context.ActorId.IsDefined)
            {
                return Array.Empty<CardInstance>();
            }

            var pool = (BorrowedActionPool)context.Input(PoolParameter).IntegerValue;
            var color = (CardColor)context.Input(ColorParameter).IntegerValue;
            if (pool == BorrowedActionPool.None ||
                !Enum.IsDefined(typeof(BorrowedActionPool), pool) ||
                !Enum.IsDefined(typeof(CardColor), color))
            {
                return Array.Empty<CardInstance>();
            }

            BorrowedPrimaryActionDeclaration declaration;
            switch (pool)
            {
                case BorrowedActionPool.OwnedDiscardedCards:
                    declaration = BorrowedPrimaryActionDeclaration.FromDiscardedCards(color);
                    break;
                case BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect:
                    declaration =
                        BorrowedPrimaryActionDeclaration.FromActiveCardsWithAnActiveEffect();
                    break;
                case BorrowedActionPool.OwnedRightmostResolvedCard:
                    declaration = BorrowedPrimaryActionDeclaration.FromRightmostResolvedCard();
                    break;
                case BorrowedActionPool.PreviousTurnSlotCard:
                    declaration = BorrowedPrimaryActionDeclaration.FromPreviousTurnSlotCard();
                    break;
                case BorrowedActionPool.OwnedResolvedSkills:
                    declaration = BorrowedPrimaryActionDeclaration.FromResolvedSkills();
                    break;
                case BorrowedActionPool.OwnedFaceUpSkills:
                    declaration = BorrowedPrimaryActionDeclaration.FromFaceUpSkills();
                    break;
                // Milestone 9.30c. A spell is cast by name (`cast-spell@1`), never chosen as a card,
                // so this selection offers no spell and no facedown book identity reaches a chooser.
                case BorrowedActionPool.OwnedSpellbookSpells:
                    return Array.Empty<CardInstance>();
                default:
                    return Array.Empty<CardInstance>();
            }

            return BorrowedPrimaryActionQuery.EligibleCards(
                declaration,
                context.ActorId,
                context.CardState.Definitions,
                context.CardState.Instances,
                context.Timing == null ? null : context.Timing.Effects,
                context.Capabilities,
                BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive(
                    context.ActorId,
                    context.ActorContinuousSources),
                PositionsFor(context, pool));
        }

        /// <summary>
        /// The card positions the declared pool reads, which are two different questions about
        /// rulebook p.6's turn area and not one. Milestone 8.39f.
        ///
        /// <para>Swift's Reload! asks for an ORDER - every card the hero has resolved this round, so
        /// that "rightmost" can name its last entry. Cutter's Legend of the Skies asks for one SLOT.
        /// Handing the ordered form to the slot reading would let it walk back past a slot the hero
        /// passed in, which is a sentence her card does not print.</para>
        /// </summary>
        private static IReadOnlyList<CardInstanceId> PositionsFor(
            EffectCapabilityContext context, BorrowedActionPool pool)
        {
            switch (pool)
            {
                case BorrowedActionPool.OwnedRightmostResolvedCard:
                    return ResolvedOrderFor(context);
                case BorrowedActionPool.PreviousTurnSlotCard:
                    return PreviousTurnSlotFor(context);
                default:
                    // The two state-shaped pools read no position at all, and an empty list is what
                    // BorrowedPrimaryActionQuery ignores for them rather than narrows by.
                    return Array.Empty<CardInstanceId>();
            }
        }

        /// <summary>
        /// The borrower's own card standing in the turn slot before this one, as a list of at most one.
        /// Milestone 8.39f.
        ///
        /// <para><b>The slot is the borrower's, because a turn slot is printed on a hero's own
        /// dashboard</b> - p.6 step 6, "place your card Faceup on the current Turn slot of your hero
        /// dashboard". <c>RoundTurnSlot</c> carries one assignment for every configured hero because it
        /// records a whole turn of the round at once; reading only the borrower's assignment out of it
        /// is what recovers the six separate dashboards the table actually has. See
        /// <see cref="BorrowedActionPool.PreviousTurnSlotCard"/> for the full reading and the shard row
        /// it closes.</para>
        ///
        /// <para><b>A slot index below the first is no slot</b>, which is the first turn of a round:
        /// p.6 empties the turn area at the round boundary, so there is nothing standing behind turn 1.
        /// <b>An assignment carrying no card is no card</b>, which is a turn the hero passed in with an
        /// empty hand. Both leave the pool empty rather than unfiltered.</para>
        ///
        /// <para>The source instance is dropped for the reason <see cref="ResolvedOrderFor"/> drops it:
        /// a borrow of a card's primary off itself is what <c>BorrowRefusal.ActionMovesItsOwnCard</c>
        /// exists to refuse one step later, and reaching it by accident rather than by a printed
        /// reading is not the same thing. It cannot arise from the printed face - the card being
        /// resolved stands in the CURRENT slot - so this is a guard rather than a filter.</para>
        /// </summary>
        private static IReadOnlyList<CardInstanceId> PreviousTurnSlotFor(
            EffectCapabilityContext context)
        {
            if (context.RosterState == null || !context.RosterState.IsAvailable)
            {
                return Array.Empty<CardInstanceId>();
            }

            var previousTurnIndex = context.TurnIndex - 1;
            if (previousTurnIndex < 1)
            {
                return Array.Empty<CardInstanceId>();
            }

            return context.RosterState.TurnSlots
                .Where(slot => slot != null &&
                    slot.RoundIndex == context.RoundIndex &&
                    slot.TurnIndex == previousTurnIndex)
                .SelectMany(slot => slot.Assignments)
                .Where(assignment =>
                    assignment != null &&
                    assignment.HeroId.Equals(context.ActorId) &&
                    assignment.CardInstanceId.HasValue &&
                    !assignment.CardInstanceId.Value.Equals(context.SourceCardInstanceId))
                .Select(assignment => assignment.CardInstanceId.Value)
                .ToArray();
        }

        /// <summary>
        /// The cards the acting hero has resolved so far this round, in rulebook p.6's committed turn
        /// order, with the card asking the question removed. Milestone 8.38c.
        ///
        /// <para><b>Why the source card is dropped.</b> Reload! asks "your rightmost resolved card"
        /// while Reload! is itself the card being resolved, and its own assignment is already in the
        /// round's turn slots by then. Reading the slots without this would name the asking card as its
        /// own answer, which is a borrow of a card's primary off itself - exactly what
        /// <see cref="BorrowRefusal.ActionMovesItsOwnCard"/> exists to refuse one step later, reached
        /// by accident instead of by a printed reading.</para>
        ///
        /// <para><b>Why the round is filtered and the turn index is not.</b> p.6 empties the turn area
        /// at the round boundary, so a card resolved last round is not standing there to be rightmost.
        /// Within the round every earlier turn's card has resolved by construction - a hero takes one
        /// turn at a time - so dropping the source instance is the whole of what "resolved" needs to
        /// mean here, and no resolution-status field is consulted.</para>
        ///
        /// <para>Absent roster state yields an empty order rather than an exception, and
        /// <see cref="BorrowedPrimaryActionQuery.EligibleCards"/> turns that into an empty pool. A
        /// capability that cannot see the round cannot answer a question about it, and failing closed
        /// is what keeps that from becoming "any card you own".</para>
        /// </summary>
        private static IReadOnlyList<CardInstanceId> ResolvedOrderFor(EffectCapabilityContext context)
        {
            if (context.RosterState == null || !context.RosterState.IsAvailable)
            {
                return Array.Empty<CardInstanceId>();
            }

            return context.RosterState.TurnSlots
                .Where(slot => slot != null && slot.RoundIndex == context.RoundIndex)
                .OrderBy(slot => slot.TurnIndex)
                .SelectMany(slot => slot.Assignments)
                .Where(assignment =>
                    assignment != null &&
                    assignment.HeroId.Equals(context.ActorId) &&
                    assignment.CardInstanceId.HasValue &&
                    !assignment.CardInstanceId.Value.Equals(context.SourceCardInstanceId))
                .Select(assignment => assignment.CardInstanceId.Value)
                .ToArray();
        }

        private sealed class BorrowPrimaryActionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BorrowPrimaryActionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            PoolParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(BorrowedActionPool.None)),
                        new EffectCapabilityParameter(
                            ColorParameter,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.All<CardColor>()),
                        new EffectCapabilityParameter(ExcludeAttackedUnitParameter, EffectValueKind.Boolean, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding(BorrowedCardOutput, EffectValueKind.CardInstance),
                        new EffectOutputBinding(ExcludedHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ExcludedMinionOutput, EffectValueKind.Minion)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// An empty pool is impossible rather than declined, and that is the printed reading rather
            /// than a convenience. None of the three faces prints "you may", "up to" or "if able" on
            /// this step, so p.10 makes it mandatory; the enclosing "If enraged" is what makes the whole
            /// clause skippable, and the user's general conditional ruling is explicit that a false
            /// condition skips its clause and the card continues. So the mandatory step reports that it
            /// cannot happen, and the Conditional above it is what decides whether it was ever asked.
            ///
            /// <para>Milestone 9.38: the offer lists only the cards <see cref="ActingCandidates"/>
            /// admits, so it and <see cref="Anticipate"/> are one reading. Before this the live offer
            /// listed the whole pool and Fight as One offered a Fiery Breath whose only subject was the
            /// hero it had just attacked; choosing it emptied the Breath's mandatory target and the card
            /// stopped ("FIERY BREATH CANCELED", the 2026-10-02 playtester's report).</para>
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var acting = ActingCandidates(context, out var poolIsEmpty);
                if (poolIsEmpty)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return acting.Count == 0
                    ? EffectExecutionResult.DeclinedOptional()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        acting.Select(card => EffectValue.Card(card.Id)),
                        1,
                        1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected != null && selected.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                        ? ActingCandidates(context, out _).SingleOrDefault(card =>
                            card.Id.Equals(selected[0].CardInstanceId))
                        : null;
                if (chosen == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The card is named and nothing else happens here. It is not moved, not flipped and not
                // resolved - the contract's "the borrowed card does not change state" is true because
                // this handler is the only thing that touches it and it touches nothing.
                var outcomes = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(BorrowedCardOutput, EffectValue.Card(chosen.Id))
                };
                var excluded = ExcludedUnit(context);
                if (excluded.Kind == RuleObjectKind.Hero)
                    outcomes.Add(new EffectOutcomeFact(ExcludedHeroOutput, EffectValue.Hero(excluded.HeroId)));
                else if (excluded.Kind == RuleObjectKind.Minion)
                    outcomes.Add(new EffectOutcomeFact(ExcludedMinionOutput, EffectValue.Minion(excluded.MinionId)));
                return EffectExecutionResult.Applied(outcomes);
            }

            /// <summary>
            /// Milestone 9.6: where the turn says whether a borrowed primary could act, a pool whose
            /// every card would do nothing is a borrow that performs nothing - answered as declined,
            /// so a bullet offering it stays selectable (8.35z35) while an action it is the only
            /// hope of is greyed (6.5af; user ruling 2026-09-28 on Reload!).
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var acting = ActingCandidates(context, out var poolIsEmpty);
                if (poolIsEmpty)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return acting.Count == 0
                    ? EffectExecutionResultKind.DeclinedOptional
                    : EffectExecutionResultKind.NeedsChoice;
            }

            /// <summary>
            /// Milestone 9.38. The cards this borrow offers: the declared pool, narrowed to those whose
            /// printed primary could act now with the unit this clause bars excluded - where the turn
            /// supplies that judgement (<see cref="EffectCapabilityContext.BorrowedPrimaryCanAct"/>),
            /// and the whole pool where it does not. Rulebook p.10's "if able" and milestone 8.35z15's
            /// rule - a card-text step with no legal subject is not offered - applied at the moment
            /// the offer is built, which is the only moment a player can be shown it. One function
            /// feeds the offer, the answer and the anticipation, so they cannot disagree.
            /// <paramref name="poolIsEmpty"/> keeps the printed distinction between a pool with
            /// nothing in it (impossible) and a pool whose cards can all do nothing (declined).
            /// </summary>
            private static IReadOnlyList<CardInstance> ActingCandidates(
                EffectCapabilityContext context, out bool poolIsEmpty)
            {
                var pool = Candidates(context);
                poolIsEmpty = pool.Count == 0;
                if (poolIsEmpty || context.BorrowedPrimaryCanAct == null)
                {
                    return pool;
                }

                var excluded = ExcludedUnit(context);
                return pool.Where(card => context.BorrowedPrimaryCanAct(card.Id, context.ActorId, excluded))
                    .ToArray();
            }

            /// <summary>
            /// The unit this borrow's clause bars from the borrowed primary: the unit the enclosing
            /// Attack took when <see cref="ExcludeAttackedUnitParameter"/> is set ("targeting a
            /// different unit", WI-07), and none otherwise.
            /// </summary>
            private static RuleObject ExcludedUnit(EffectCapabilityContext context)
            {
                if (!context.Input(ExcludeAttackedUnitParameter).BooleanValue)
                {
                    return RuleObject.None;
                }

                if (context.EnclosingAttack.HeroTargetId.IsDefined)
                {
                    return RuleObject.ForHero(context.EnclosingAttack.HeroTargetId);
                }

                return context.EnclosingAttack.MinionTargetId.IsDefined
                    ? RuleObject.ForMinion(context.EnclosingAttack.MinionTargetId)
                    : RuleObject.None;
            }
        }
    }
}

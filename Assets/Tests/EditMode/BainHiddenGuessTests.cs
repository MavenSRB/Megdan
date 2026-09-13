using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40f, the capability half: the hidden card choice, the public guess domain and the
    /// reveal Bain's three Blue Skills print - "An enemy hero in radius with two or more cards in hand
    /// chooses one of those cards; Guess the card's color, then reveal the card; If you guessed
    /// correctly, discard that card; otherwise you gain 1 coin. (You can only guess colors that could
    /// be in that player's hand)."
    ///
    /// <para><b>Every case here is about a CONTRACT and none is about Bain</b>, whose faces are
    /// composed at 8.40i. The two new capabilities are deferred on milestone 7.9's gate, so this
    /// fixture composes its own catalog from the deferred list exactly as every other pre-dealing
    /// fixture in this folder does.</para>
    ///
    /// <para><b>The guess domain is the sharp one and it is asserted negatively as well as
    /// positively.</b> A domain derived from the hand would be correct on every board where the
    /// answer happens to agree; the case that catches it is the one that runs the SAME board twice
    /// with two different cards hidden and demands the two offers be equal.</para>
    /// </summary>
    public sealed class BainHiddenGuessTests
    {
        private static readonly HeroId Guesser = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Chooser = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static readonly MapId Map = new MapId("bain-guess-map");
        private static readonly ZoneId Battle = new ZoneId("bain-guess-zone");
        private static readonly HexCoordinate GuesserAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate ChooserAt = new HexCoordinate(1, 0);

        private static readonly EffectCapabilityCatalog Catalog =
            SyntheticEffectCapabilities.CreateCatalog(
                SyntheticHiddenChoiceCapabilities.DeferredHandlers());

        private static readonly EffectCapabilityCatalog TargetingCatalog =
            new EffectCapabilityCatalog(SyntheticTargetingCapabilities.Handlers());

        /// <summary>
        /// The showcase definitions, borrowed for their printed COLOURS alone. Nothing here reads a
        /// showcase card's text: a colour is what the guess domain and the comparison are about, and
        /// inventing definitions would have invented the very characteristic under test.
        /// </summary>
        private static readonly IReadOnlyList<CardDefinition> Definitions =
            MatchConfiguration.CreateForCardStateFixture(TeamSide.Blue).CardDefinitions
                .Concat(GuardsDigital.Core.Cards.Verified.DodgerCardContent.AllDefinitions)
                .ToArray();

        /// <summary>
        /// The source card the printed-Radius selection measures its disk from - a real dealt face
        /// carrying a real printed Radius, which is the same source milestone 8.35z28's own narrowing
        /// cases use for this selection.
        /// </summary>
        private static readonly CardDefinition RadiusSourceDefinition =
            GuardsDigital.Core.Cards.Verified.DodgerCardContent.Definition(
                GuardsDigital.Core.Cards.Verified.DodgerCardContent.DeathTrap);

        // -------------------------------------------------------------------------------------------
        // (a) The deferral, asserted from both sides
        // -------------------------------------------------------------------------------------------

        [Test]
        public void NeitherNewCapability_IsRegisteredByThisBuild()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            Assert.That(SyntheticHiddenChoiceCapabilities.RegisteredHandlers(), Is.Empty,
                "No dealt card prints either sentence, and the registered capability list is " +
                "serialized into every snapshot.");
            Assert.That(
                SyntheticHiddenChoiceCapabilities.DeferredHandlers().Select(handler => handler.Reference),
                Is.EquivalentTo(new[]
                {
                    SyntheticHiddenChoiceCapabilities.HiddenCardChoiceV1,
                    SyntheticHiddenChoiceCapabilities.RevealChosenCardV1
                }));
            Assert.That(
                registered.IsRegistered(SyntheticHiddenChoiceCapabilities.HiddenCardChoiceV1),
                Is.False,
                "Registering a contract before a dealt card can start it moves pinned bytes and " +
                "fails milestone 7.9's reachability gate. 8.40j registers both, each with a " +
                "showcase consumer.");
            Assert.That(
                registered.IsRegistered(SyntheticHiddenChoiceCapabilities.RevealChosenCardV1),
                Is.False);
            Assert.That(
                registered.IsRegistered(SyntheticBorrowedActionCapabilities.NameColorV1), Is.True,
                "and the capability the guess domain is a PARAMETER on is registered, so the two " +
                "assertions above are about deferral rather than about a catalog nobody fills.");
        }

        // -------------------------------------------------------------------------------------------
        // (b) The hand-size narrowing on the printed-Radius hero selection
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The printed clause at one, two and three cards. The narrowing is the candidate set and not
        /// a gate on what happens afterwards, which is what `hasCardInDiscard`'s own remarks record
        /// for the identical grammar.
        /// </summary>
        [Test]
        public void TheSelection_AdmitsOnlyEnemyHeroesHoldingAtLeastTheNamedNumberOfCards()
        {
            for (var held = 0; held <= 3; held++)
            {
                var candidates = RadiusHeroCandidates(minimumCardsInHand: 2, chooserHandSize: held);
                Assert.That(
                    candidates,
                    held >= 2 ? Has.Member(Chooser) : Has.No.Member(Chooser),
                    "A hand of " + held + " against a printed minimum of two.");
            }
        }

        [Test]
        public void AnUnboundNarrowing_LeavesTheSelectionExactlyAsEveryEarlierCardHasIt()
        {
            Assert.That(RadiusHeroCandidates(minimumCardsInHand: null, chooserHandSize: 0),
                Has.Member(Chooser),
                "An unbound optional parameter is the absence of the clause. A hero holding nothing " +
                "is still an enemy hero in radius, which is what every selection written before " +
                "Bain offers.");
            Assert.That(RadiusHeroCandidates(minimumCardsInHand: 0, chooserHandSize: 0),
                Has.Member(Chooser),
                "and a declared minimum of zero is no narrowing either, which is what keeps the " +
                "unbound reading and the bound one one rule rather than two.");
        }

        /// <summary>
        /// The count the narrowing reads is the count every seat is already shown, asserted against
        /// the one query both of them ask rather than against a number this fixture computed.
        /// </summary>
        [Test]
        public void TheHandSizeItReads_IsThePubliclyPublishedCount()
        {
            var cards = ChooserHand(3).Concat(GuesserCards()).ToArray();

            Assert.That(HeroHandQuery.Count(cards, Chooser), Is.EqualTo(3));
            Assert.That(HeroHandQuery.HasAtLeast(cards, Chooser, 3), Is.True);
            Assert.That(HeroHandQuery.HasAtLeast(cards, Chooser, 4), Is.False);
            Assert.That(HeroHandQuery.HasAtLeast(cards, Chooser, 0), Is.True,
                "Zero is no narrowing, so it admits every hero including one holding nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // (c) The hidden choice
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheHiddenChoice_PutsTheChoosersOwnHandToTheChooser()
        {
            var hand = ChooserHand(3);
            var opened = HiddenChoiceHandler().Execute(HiddenChoiceContext(hand));

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(opened.Choice.ChooserHeroId, Is.EqualTo(Chooser),
                "The pause belongs to the enemy hero the card named, never to the acting hero.");
            Assert.That(
                opened.Choice.Candidates.Select(candidate => candidate.CardInstanceId),
                Is.EquivalentTo(hand.Select(card => card.Id)));
            Assert.That(opened.Choice.MinimumSelections, Is.EqualTo(1));
            Assert.That(opened.Choice.MaximumSelections, Is.EqualTo(1));
        }

        [Test]
        public void TheHiddenChoice_OpensAPromptEvenForASingleCard()
        {
            var opened = HiddenChoiceHandler().Execute(HiddenChoiceContext(ChooserHand(1)));

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "A hand of one that answered itself would tell the guesser the card was never " +
                "chosen at all, which is information the chooser's decision is supposed to hide.");
        }

        [Test]
        public void TheAnswer_PublishesTheCardAndMovesNothing()
        {
            var hand = ChooserHand(3);
            var chosen = hand[1];
            var applied = HiddenChoiceHandler().Resume(
                HiddenChoiceContext(hand), new[] { EffectValue.Card(chosen.Id) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                applied.Outcomes.Single().OutputName,
                Is.EqualTo(SyntheticHiddenChoiceCapabilities.ChosenCardOutput));
            Assert.That(applied.Outcomes.Single().Value.CardInstanceId, Is.EqualTo(chosen.Id));
            Assert.That(
                hand.All(card => card.State.Location == CardLocation.Hand), Is.True,
                "Nothing moves. The whole point of this capability is that the answer survives " +
                "across the next node, which is what no card-valued choice in the build did before.");
            Assert.That(applied.RevealedCards, Is.Empty,
                "and nothing is published to the table: the reveal is a later node.");
        }

        [Test]
        public void AnAnswerNamingACardThatHasLeftTheHand_IsRefused()
        {
            var hand = ChooserHand(3);
            var stale = new CardInstance(
                hand[0].Id, hand[0].DefinitionReference, Chooser, CardStateFacts.DiscardedUnplayed());
            var state = new List<CardInstance> { stale, hand[1], hand[2] };

            var refused = HiddenChoiceHandler().Resume(
                HiddenChoiceContext(state), new[] { EffectValue.Card(stale.Id) });

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Re-derived, never trusted: a card that moved between the offer and the answer is " +
                "refused rather than named to the table a moment later.");
        }

        // -------------------------------------------------------------------------------------------
        // (d) The guess domain - BA-D2
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheGuessDomain_IsExactlyTheColoursPublicInformationAdmits()
        {
            var owned = ChooserHand(3).Concat(new[] { Discarded(CardColor.Red) }).ToArray();
            var context = GuessContext(owned, restrictToColorsOwnedBy: Chooser);

            var expected = AllOwnedColorsNotFullyVisible(owned);
            Assert.That(
                SyntheticBorrowedActionCapabilities.GuessableColors(context, Chooser),
                Is.EquivalentTo(expected),
                "The domain is the printed deck minus what the open zones account for, computed " +
                "here from the instances rather than by calling the production function twice.");
        }

        /// <summary>
        /// Two independent reasons, both asserted, because either alone would be a coincidence.
        /// </summary>
        [Test]
        public void TheGuessDomain_NeverOffersPurple()
        {
            Assert.That(
                Definitions.Any(definition => definition.Progression.Color == CardColor.Purple),
                Is.False,
                "REASON ONE: no printed definition in this build carries a Purple progression at " +
                "all - CardProgressionProfile refuses it for every printed hand definition, which " +
                "is RULING 21's 'Purple is inert' read off the content rather than re-decided. So " +
                "no hero OWNS a Purple card and the subtraction could never admit one.");

            Assert.That(
                SyntheticBorrowedActionCapabilities.GuessableColors(
                    GuessContext(ChooserHand(3), Chooser), Chooser),
                Has.No.Member(CardColor.Purple),
                "REASON TWO: the narrowing excludes it by name regardless, so a definition that one " +
                "day carried one still could not be guessed - p.9's Purple is the Ultimate and " +
                "sits in the Ultimate area rather than in a hand.");

            Assert.That(
                SyntheticBorrowedActionCapabilities.AllNameableColors,
                Has.Member(CardColor.Purple),
                "and the UNNARROWED offer still carries it, which is what makes the exclusion this " +
                "card's own printed clause rather than a change to Tali's vocabulary.");
        }

        [Test]
        public void TheGuessDomain_ExcludesAColourThePublicZonesFullyAccountFor()
        {
            var hidden = InHand(CardColor.Blue, "blue-hidden");
            var withGreenInHand = new[] { hidden, InHand(CardColor.Green, "green-card") };
            var withGreenDiscarded = new[] { hidden, Discarded(CardColor.Green) };

            Assert.That(
                SyntheticBorrowedActionCapabilities.GuessableColors(
                    GuessContext(withGreenInHand, Chooser), Chooser),
                Has.Member(CardColor.Green));
            Assert.That(
                SyntheticBorrowedActionCapabilities.GuessableColors(
                    GuessContext(withGreenDiscarded, Chooser), Chooser),
                Has.No.Member(CardColor.Green),
                "The only Green this hero owns is lying face up in the discard pile, so no Green " +
                "can be in the hand and the subtraction any player can do at the table removes it.");
        }

        /// <summary>
        /// THE CASE THAT CATCHES A DOMAIN DERIVED FROM THE HAND. The same board, the same owned
        /// cards, two different cards hidden: a domain that read the hand would move and this one
        /// does not.
        /// </summary>
        [Test]
        public void TheGuessDomain_IsIdenticalWhicheverCardIsHidden()
        {
            var owned = new[]
            {
                InHand(CardColor.Blue, "blue-card"),
                InHand(CardColor.Red, "red-card"),
                Discarded(CardColor.Green)
            };

            var firstHidden = Resolve(HiddenChoice(owned, owned[0].Id), owned);
            var secondHidden = Resolve(HiddenChoice(owned, owned[1].Id), owned);

            Assert.That(firstHidden, Is.EqualTo(secondHidden),
                "Two different hidden cards, one offer. The colours a guess may name are a fact " +
                "about what every seat can see, and the hidden answer is not one of them.");
            Assert.That(firstHidden, Has.Member(CardColor.Blue));
            Assert.That(firstHidden, Has.Member(CardColor.Red));
        }

        [Test]
        public void AnUnboundDomain_OffersAllSixExactlyAsCommuneWithSpiritsDoes()
        {
            var opened = NameColorHandler().Execute(
                GuessContext(ChooserHand(2), restrictToColorsOwnedBy: default));

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                opened.Choice.Candidates.Select(candidate => (CardColor)candidate.IntegerValue),
                Is.EqualTo(SyntheticBorrowedActionCapabilities.AllNameableColors),
                "Tali's ruling is about Tali's sentence. An optional narrowing nothing binds leaves " +
                "her offer byte-identical, Purple included.");
            Assert.That(
                opened.Choice.ValueVocabulary,
                Is.EqualTo(GuardsDigital.Core.Rules.EffectResolution
                    .EffectChoiceValueVocabulary.CardColor),
                "and the declared vocabulary is unchanged, so a joined seat still draws swatches.");
        }

        [Test]
        public void ANarrowedGuess_RefusesAnAnswerOutsideItsOwnDomain()
        {
            var owned = new[] { InHand(CardColor.Blue, "blue-card"), Discarded(CardColor.Green) };
            var context = GuessContext(owned, Chooser);

            Assert.That(
                NameColorHandler().Resume(context, new[] { EffectValue.Integer((int)CardColor.Blue) })
                    .Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                NameColorHandler().Resume(context, new[] { EffectValue.Integer((int)CardColor.Green) })
                    .Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Checked against the NARROWED set and re-derived rather than trusted, so an answer " +
                "naming an excluded colour is refused however it arrived.");
        }

        [Test]
        public void AGuessAgainstAHeroWhoOwnsNothingGuessable_ReportsItselfImpossible()
        {
            var owned = new[] { Discarded(CardColor.Green) };
            var context = GuessContext(owned, Chooser);

            Assert.That(NameColorHandler().Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(NameColorHandler().Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The probe and the offer are one function, so a mandatory step with an empty " +
                "domain is greyed rather than offered and then refused.");
        }

        // -------------------------------------------------------------------------------------------
        // (e) The reveal and the comparison - one node, two outputs
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheReveal_PublishesTheCardsColourAndWhetherTheGuessMatched()
        {
            var card = InHand(CardColor.Blue, "revealed-card");

            var matched = RevealHandler().Execute(RevealContext(card, CardColor.Blue));
            Assert.That(Fact(matched, SyntheticHiddenChoiceCapabilities.MatchedOutput).BooleanValue,
                Is.True);
            Assert.That(
                (CardColor)Fact(matched, SyntheticHiddenChoiceCapabilities.RevealedColorOutput)
                    .IntegerValue,
                Is.EqualTo(CardColor.Blue),
                "The gate and the colour are ONE read of one card, which is defended-card-color@1's " +
                "argument applied here - and why this build gains no general integer comparator.");

            var missed = RevealHandler().Execute(RevealContext(card, CardColor.Red));
            Assert.That(Fact(missed, SyntheticHiddenChoiceCapabilities.MatchedOutput).BooleanValue,
                Is.False);
            Assert.That(
                (CardColor)Fact(missed, SyntheticHiddenChoiceCapabilities.RevealedColorOutput)
                    .IntegerValue,
                Is.EqualTo(CardColor.Blue),
                "and the colour is published on BOTH arms, so the losing branch can still name it.");
        }

        [Test]
        public void TheReveal_ReportsTheCardAndItsOwnerRatherThanTheActor()
        {
            var card = InHand(CardColor.Blue, "revealed-card");
            var applied = RevealHandler().Execute(RevealContext(card, CardColor.Blue));

            Assert.That(applied.RevealedCards.Count, Is.EqualTo(1));
            Assert.That(applied.RevealedCards[0].CardInstanceId, Is.EqualTo(card.Id));
            Assert.That(applied.RevealedCards[0].OwnerHeroId, Is.EqualTo(Chooser),
                "The hero is the card's OWNER. A reveal that named the acting hero would print " +
                "Bain's name beside somebody else's card.");
            Assert.That(card.State.Location, Is.EqualTo(CardLocation.Hand),
                "A reveal turns a card over; it does not move it. After a wrong guess the card is " +
                "still in the hand it came from.");
        }

        [Test]
        public void ARevealOfACardTheStateDoesNotHold_ReportsItselfImpossible()
        {
            var context = RevealContext(
                InHand(CardColor.Blue, "absent-card"), CardColor.Blue, includeCard: false);

            Assert.That(RevealHandler().Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Publishing a colour of None would let the Conditional read the absence as a wrong " +
                "guess and pay coins for a card nobody revealed.");
        }

        /// <summary>
        /// The reveal's one published row, end to end through the production translation: the fact
        /// the handler reports becomes a card-action event, which becomes a rule-event log line,
        /// which becomes exactly one seat-public row naming the card's owner and the card.
        /// </summary>
        [Test]
        public void TheRevealFact_BecomesOneSeatPublicRowNamingTheOwnerAndTheCard()
        {
            var card = InHand(CardColor.Blue, "revealed-card");
            var node = new GuardsDigital.Core.Rules.EffectResolution.EffectResolutionEvent(
                new EffectNodeId("reveal-node"),
                EffectExecutionResultKind.Applied,
                revealedCards: new[] { new RevealedCard(Chooser, card.Id) });

            var derived = DeriveLog(node);

            Assert.That(derived.Count, Is.EqualTo(1));
            Assert.That(derived[0].Kind, Is.EqualTo(MatchLogEntryKind.CardRevealedByCardText));
            Assert.That(derived[0].SubjectHeroId, Is.EqualTo(Chooser));
            Assert.That(derived[0].SubjectCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(derived[0].Kind, Is.Not.EqualTo(MatchLogEntryKind.CardRevealed),
                "Reusing p.7's commitment kind would have animated the hero's turn-card slot and " +
                "falsified the channel's own one-for-one ledger. See the kind's own remarks.");
        }

        [Test]
        public void ANodeThatRevealedNothing_WritesNoRevealRow()
        {
            var derived = DeriveLog(
                new GuardsDigital.Core.Rules.EffectResolution.EffectResolutionEvent(
                    new EffectNodeId("ordinary-node"), EffectExecutionResultKind.Applied));

            Assert.That(derived, Is.Empty,
                "Every node in the build but one reveals nothing, and none of them gains a line.");
        }

        /// <summary>
        /// The vocabulary guard the channel already carries, restated against the new member: it must
        /// not be named for anything the selection contract excludes.
        /// </summary>
        [Test]
        public void TheNewKind_NamesNothingTheSelectionExcludes()
        {
            foreach (var forbidden in new[]
            {
                "Hand", "Commitment", "Facedown", "Committed",
                "Step", "Choice", "Requested", "Option", "Assignment", "Progressed", "Calculated"
            })
            {
                Assert.That(
                    nameof(MatchLogEntryKind.CardRevealedByCardText)
                        .IndexOf(forbidden, StringComparison.OrdinalIgnoreCase),
                    Is.LessThan(0),
                    "A log kind named for '" + forbidden + "' would put an excluded event on this " +
                    "channel, which is the guard MatchLogChannelTests already enforces.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // (f) `restrictToCard` on the discard - "discard that card"
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ARestrictedDiscard_TakesThatCardAndOnlyThatCard_WithoutAsking()
        {
            var hand = ChooserHand(3);
            var named = hand[1];
            var applied = DiscardHandler().Execute(DiscardContext(hand, restrictToCard: named.Id));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A restriction that has already named the one legal answer has nothing to ask.");
            Assert.That(named.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(
                hand.Where(card => !card.Id.Equals(named.Id))
                    .All(card => card.State.Location == CardLocation.Hand),
                Is.True,
                "and only that card: a prompt here would let the chooser spend a different one.");
        }

        [Test]
        public void ARestrictedDiscard_WhoseCardHasLeftTheHand_TakesNothing()
        {
            var hand = ChooserHand(2);
            var absent = new CardInstanceId("card-that-is-not-there");

            var result = DiscardHandler().Execute(DiscardContext(hand, restrictToCard: absent));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Revision 2 reports the inability rather than declining - the revision's own " +
                "reading of an empty hand, reached here by a named card that is no longer there.");
            Assert.That(
                Fact(result, SyntheticCardStateCapabilities.UnableToDiscardOutput).BooleanValue,
                Is.True);
            Assert.That(hand.All(card => card.State.Location == CardLocation.Hand), Is.True,
                "and nothing else is taken in its place.");
        }

        [Test]
        public void AnUnboundRestriction_KeepsTheAskWhichOneReading()
        {
            var hand = ChooserHand(3);
            var opened = DiscardHandler().Execute(DiscardContext(hand, restrictToCard: default));

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "Every composition written before Bain binds nothing here and keeps its prompt.");
            Assert.That(
                opened.Choice.Candidates.Select(candidate => candidate.CardInstanceId),
                Is.EquivalentTo(hand.Select(card => card.Id)));
        }

        [Test]
        public void ARestrictedDiscard_IsProbedAsApplyingRatherThanAsPausing()
        {
            var hand = ChooserHand(3);

            Assert.That(
                DiscardHandler().Anticipate(DiscardContext(hand, restrictToCard: hand[0].Id)),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "A probe that reported NeedsChoice would be describing a pause Execute never " +
                "creates, which is the offer/resolution divergence the fidelity rule exists to stop.");
            Assert.That(
                DiscardHandler().Anticipate(DiscardContext(hand, restrictToCard: default)),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "and the unrestricted probe is untouched.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler HiddenChoiceHandler() =>
            SyntheticHiddenChoiceCapabilities.DeferredHandlers().Single(handler =>
                handler.Reference.Equals(SyntheticHiddenChoiceCapabilities.HiddenCardChoiceV1));

        private static IEffectCapabilityHandler RevealHandler() =>
            SyntheticHiddenChoiceCapabilities.DeferredHandlers().Single(handler =>
                handler.Reference.Equals(SyntheticHiddenChoiceCapabilities.RevealChosenCardV1));

        private static IEffectCapabilityHandler NameColorHandler() =>
            SyntheticBorrowedActionCapabilities.RegisteredHandlers().Single(handler =>
                handler.Reference.Equals(SyntheticBorrowedActionCapabilities.NameColorV1));

        private static IEffectCapabilityHandler DiscardHandler() =>
            SyntheticCardStateCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SyntheticCardStateCapabilities.DiscardChosenCardV2));

        private static EffectValue Fact(EffectExecutionResult result, string outputName) =>
            result.Outcomes.Single(outcome => outcome.OutputName == outputName).Value;

        /// <summary>The colours the open zones cannot account for, computed from the instances.</summary>
        private static IReadOnlyList<CardColor> AllOwnedColorsNotFullyVisible(
            IReadOnlyList<CardInstance> owned) =>
            SyntheticBorrowedActionCapabilities.AllNameableColors
                .Where(color => color != CardColor.Purple)
                .Where(color =>
                {
                    var ofColor = owned.Where(card => ColorOf(card) == color).ToArray();
                    return ofColor.Length > ofColor.Count(PublicCardZoneQuery.IsPubliclyVisible);
                })
                .ToArray();

        private static CardColor ColorOf(CardInstance card) =>
            Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Progression.Color;

        /// <summary>The guess domain a hidden-card board offers, so two boards can be compared.</summary>
        private static IReadOnlyList<CardColor> Resolve(
            CardInstanceId hidden, IReadOnlyList<CardInstance> owned)
        {
            Assert.That(owned.Any(card => card.Id.Equals(hidden)), Is.True,
                "This helper compares two boards that differ only in which card is hidden.");
            return SyntheticBorrowedActionCapabilities
                .GuessableColors(GuessContext(owned, Chooser), Chooser)
                .ToArray();
        }

        private static CardInstanceId HiddenChoice(
            IReadOnlyList<CardInstance> owned, CardInstanceId chosen)
        {
            var applied = HiddenChoiceHandler().Resume(
                HiddenChoiceContext(owned), new[] { EffectValue.Card(chosen) });
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            return applied.Outcomes.Single().Value.CardInstanceId;
        }

        private static IReadOnlyList<MatchLogEntry> DeriveLog(
            GuardsDigital.Core.Rules.EffectResolution.EffectResolutionEvent node)
        {
            var facts = CardActionResolutionEvent.RevealedCardFacts(
                new ActionResolutionId("bain-guess-resolution"),
                new CardInstanceId("bain-guess-source"),
                new ActionDefinitionId("bain-guess-action"),
                Guesser,
                node);

            var log = new RuleEventLogProjection();
            log.Append(facts.Select(fact => RoundResolutionEvent.FromTurn(
                1, 1, TurnResolutionEvent.FromSkillAction(fact))));

            return MatchLogProjection.Derive(log.Entries, int.MaxValue);
        }

        // ---- Card state

        private static CardInstance InHand(CardColor color, string id) =>
            CardInstance.CreateInHand(
                new CardInstanceId(id), DefinitionOfColor(color).Reference, Chooser);

        private static CardInstance Discarded(CardColor color) =>
            new CardInstance(
                new CardInstanceId("discarded-" + color),
                DefinitionOfColor(color).Reference,
                Chooser,
                CardStateFacts.DiscardedUnplayed());

        private static CardDefinition DefinitionOfColor(CardColor color) =>
            Definitions.First(definition => definition.Progression.Color == color);

        /// <summary>
        /// A hand of distinct cards for the chooser, drawn from the showcase definitions so every
        /// card carries a real printed colour.
        /// </summary>
        private static IReadOnlyList<CardInstance> ChooserHand(int size)
        {
            var colors = Definitions
                .Select(definition => definition.Progression.Color)
                .Where(color => color != CardColor.None && color != CardColor.Purple)
                .Distinct()
                .ToArray();
            Assert.That(colors.Length, Is.GreaterThanOrEqualTo(size),
                "This fixture needs one printed colour per card in the hand it builds.");

            return colors.Take(size)
                .Select(color => InHand(color, "chooser-" + color))
                .ToArray();
        }

        private static IReadOnlyList<CardInstance> GuesserCards() =>
            new[]
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("guesser-card"),
                    DefinitionOfColor(CardColor.Gold).Reference,
                    Guesser)
            };

        // ---- Contexts

        private static EffectCapabilityContext HiddenChoiceContext(
            IReadOnlyList<CardInstance> chooserCards) =>
            Context(
                chooserCards,
                new Dictionary<string, EffectValue>
                {
                    [SyntheticHiddenChoiceCapabilities.HiddenChoiceHeroParameter] =
                        EffectValue.Hero(Chooser)
                });

        private static EffectCapabilityContext GuessContext(
            IReadOnlyList<CardInstance> chooserCards, HeroId restrictToColorsOwnedBy)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticBorrowedActionCapabilities.NameColorChooserParameter] =
                    EffectValue.Hero(Guesser)
            };
            if (restrictToColorsOwnedBy.IsDefined)
            {
                inputs[SyntheticBorrowedActionCapabilities.RestrictToColorsOwnedByParameter] =
                    EffectValue.Hero(restrictToColorsOwnedBy);
            }

            return Context(chooserCards, inputs);
        }

        private static EffectCapabilityContext RevealContext(
            CardInstance card, CardColor named, bool includeCard = true) =>
            Context(
                includeCard ? new[] { card } : Array.Empty<CardInstance>(),
                new Dictionary<string, EffectValue>
                {
                    [SyntheticHiddenChoiceCapabilities.RevealCardParameter] =
                        EffectValue.Card(card.Id),
                    [SyntheticHiddenChoiceCapabilities.NamedColorParameter] =
                        EffectValue.Integer((int)named)
                });

        private static EffectCapabilityContext DiscardContext(
            IReadOnlyList<CardInstance> chooserCards, CardInstanceId restrictToCard)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticCardStateCapabilities.HeroParameter] = EffectValue.Hero(Chooser)
            };
            if (restrictToCard.IsDefined)
            {
                inputs[SyntheticCardStateCapabilities.RestrictToCardParameter] =
                    EffectValue.Card(restrictToCard);
            }

            return Context(chooserCards, inputs);
        }

        private static IReadOnlyList<HeroId> RadiusHeroCandidates(
            int? minimumCardsInHand, int chooserHandSize)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                [SyntheticTargetingCapabilities.ExcludeAdjacentParameterName] =
                    EffectValue.Boolean(false)
            };
            if (minimumCardsInHand.HasValue)
            {
                inputs[SyntheticTargetingCapabilities.MinimumCardsInHandParameterName] =
                    EffectValue.Integer(minimumCardsInHand.Value);
            }

            var handler = (IEffectSelectionCapabilityHandler)TargetingCatalog.Load(
                SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            return handler
                .AnticipateCandidates(Context(ChooserHand(chooserHandSize), inputs, RadiusSource))
                .Select(value => value.HeroId)
                .ToArray();
        }

        /// <summary>
        /// A source card carrying a real printed Radius, so the disk this selection measures is a
        /// printed value rather than a number the fixture invented. Dodger's Death Trap is the same
        /// source milestone 8.35z28's own narrowing cases use for the identical selection.
        /// </summary>
        private static CardInstance RadiusSource =>
            CardInstance.CreateInHand(
                new CardInstanceId("bain-guess-source"), RadiusSourceDefinition.Reference, Guesser);

        private static EffectCapabilityContext Context(
            IReadOnlyList<CardInstance> chooserCards,
            IReadOnlyDictionary<string, EffectValue> inputs,
            CardInstance sourceCard = null)
        {
            var cards = chooserCards.ToList();
            if (sourceCard != null)
            {
                cards.Add(sourceCard);
            }

            return new EffectCapabilityContext(
                new EffectNodeId("bain-guess-node"),
                EffectFailurePolicy.Mandatory,
                Guesser,
                Guesser,
                sourceCard?.Id ?? new CardInstanceId("bain-guess-source"),
                sourceCard?.DefinitionReference ?? new CardDefinitionReference(
                    new CardDefinitionId("bain-guess-definition"), "rev-1"),
                sourceCard == null
                    ? new ActionDefinitionId("bain-guess-action")
                    : RadiusSourceDefinition.ActionDefinitions
                        .First(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).Id,
                inputs,
                1,
                1,
                StagedBoardParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(Definitions, cards),
                ActionKind.Skill,
                Components());
        }

        private static MatchComponents Components()
        {
            var map = new BoardMapDefinition(
                Map, 1, "Bain Guess Map",
                Disk(GuesserAt, 3)
                    .Select(coordinate => new BoardMapSpace(
                        new SpaceId("s" + coordinate.Q + "_" + coordinate.R),
                        coordinate,
                        BoardMapSpaceKind.Playable,
                        Battle))
                    .ToArray(),
                new[] { new BoardMapZone(Battle, "Battle", "lane", null, false, true) },
                Array.Empty<BoardMapSpawnPoint>(),
                Array.Empty<ZoneId>());

            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Guesser, GuesserAt),
                    new HeroOccupancy(Chooser, ChooserAt)
                },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            return new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                board,
                new MarkerAttachmentRegister(),
                board,
                board,
                board);
        }

        private static IEnumerable<HexCoordinate> Disk(HexCoordinate centre, int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = Math.Max(-radius, -q - radius); r <= Math.Min(radius, -q + radius); r++)
                {
                    yield return new HexCoordinate(centre.Q + q, centre.R + r);
                }
            }
        }
    }
}

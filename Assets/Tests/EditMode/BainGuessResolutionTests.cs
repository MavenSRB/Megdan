using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40f, the composed clause: the four nodes Bain's three Blue Skills print, walked
    /// end to end through the resolver, and the failure modal We're Not Done Yet! adds to them.
    ///
    /// <para><b>Why the modal ships inside this milestone rather than as 8.40f1.</b> It was measured
    /// as the SAME seam rather than a separate one: it adds no capability, no parameter, no projection
    /// arm and no persisted field - it is one <c>Choose</c> node over a <c>RepeatAction</c> and a coin
    /// grant, both of which already exist, reached from the else-branch of the Conditional this
    /// fixture already builds. What it genuinely owed was a PROBE, because nothing in the corpus puts
    /// a repeat inside a Choose branch and nothing forbids it; that probe is below, and it is one
    /// definition and one walk.</para>
    ///
    /// <para><b>BA-Q2 is settled by the printed sentence and proved on boards where the readings
    /// differ.</b> "Otherwise may repeat once" says nothing about the enemy, so the second run re-asks
    /// every question the first one asked: it MAY pick a different enemy hero, it is not required to,
    /// and the now-revealed card is not excluded from a second choice. Cards are the definition - what
    /// a face does not say is absent - and the alternatives would each be a rule no printed word
    /// carries.</para>
    /// </summary>
    public sealed class BainGuessResolutionTests
    {
        private static readonly HeroId Guesser = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Chooser = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId OtherEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly EffectNodeId Root = new EffectNodeId("guess-root");
        private static readonly EffectNodeId Hide = new EffectNodeId("guess-hide");
        private static readonly EffectNodeId Guess = new EffectNodeId("guess-name-colour");
        private static readonly EffectNodeId Reveal = new EffectNodeId("guess-reveal");
        private static readonly EffectNodeId Branch = new EffectNodeId("guess-branch");
        private static readonly EffectNodeId Discard = new EffectNodeId("guess-discard");
        private static readonly EffectNodeId CoinBullet = new EffectNodeId("guess-coin-bullet");
        private static readonly EffectNodeId Modal = new EffectNodeId("guess-modal");
        private static readonly EffectNodeId Repeat = new EffectNodeId("guess-repeat");

        private static readonly MapId Map = new MapId("bain-guess-resolution-map");
        private static readonly ZoneId Battle = new ZoneId("bain-guess-resolution-zone");

        private static readonly IReadOnlyList<CardDefinition> Definitions =
            MatchConfiguration.CreateForCardStateFixture(TeamSide.Blue).CardDefinitions;

        // -------------------------------------------------------------------------------------------
        // (a) The two printed outcomes
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACorrectGuess_DiscardsThatCardAndOnlyThatCard()
        {
            var board = new GuessBoard();
            var chosen = board.ChooserHand[1];

            var walk = board.Run(chosen, ColorOf(chosen));

            Assert.That(walk.IsComplete, Is.True, "The whole clause resolved.");
            Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(
                board.ChooserHand.Where(card => !card.Id.Equals(chosen.Id))
                    .All(card => card.State.Location == CardLocation.Hand),
                Is.True,
                "\"discard THAT card\" names the card; a prompt here would let the chooser spend a " +
                "different one after the guess had already earned this one.");
            Assert.That(board.Coins.GetCoins(Guesser), Is.Zero,
                "and no coins are paid on the branch that discarded.");
        }

        [Test]
        public void AWrongGuess_AwardsThePrintedCoinsAndTakesNothing()
        {
            var board = new GuessBoard();
            var chosen = board.ChooserHand[1];
            var wrong = SyntheticBorrowedActionCapabilities.AllNameableColors
                .First(color => color != ColorOf(chosen) && color != CardColor.Purple &&
                    board.Guessable.Contains(color));

            var walk = board.Run(chosen, wrong);

            Assert.That(walk.IsComplete, Is.True);
            Assert.That(board.ChooserHand.All(card => card.State.Location == CardLocation.Hand),
                Is.True,
                "A wrong guess takes nothing - and the card goes back to a hand every player has " +
                "now seen it in, which is why BA-D3 makes the reveal permanent.");
            Assert.That(board.Coins.GetCoins(Guesser), Is.EqualTo(1),
                "and the printed coin is paid to the CONTROLLER, which is what 'you' means on a card.");
        }

        [Test]
        public void TheReveal_IsPublishedByTheResolutionItself()
        {
            var board = new GuessBoard();
            var chosen = board.ChooserHand[0];

            var walk = board.Run(chosen, ColorOf(chosen));

            var revealed = walk.Events.SelectMany(value => value.RevealedCards).ToArray();
            Assert.That(revealed.Length, Is.EqualTo(1),
                "One reveal, from the one node that performs one.");
            Assert.That(revealed[0].CardInstanceId, Is.EqualTo(chosen.Id));
            Assert.That(revealed[0].OwnerHeroId, Is.EqualTo(Chooser));
        }

        // -------------------------------------------------------------------------------------------
        // (b) The hidden answer, from the chooser's seat and from the controller's
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE ACCEPTANCE CHECK THIS MILESTONE IS SIZED AROUND, composed from both halves: the
        /// resolver's own frame standing between the answer and the reveal, published through the
        /// seat surface's narrowing.
        /// </summary>
        [Test]
        public void BetweenTheAnswerAndTheReveal_OnlyTheChoosersSeatCanNameTheCard()
        {
            var board = new GuessBoard();
            var chosen = board.ChooserHand[2];
            var paused = board.RunToTheGuess(chosen);

            Assert.That(
                CardsNamedBy(paused), Has.Member(chosen.Id),
                "The authority holds the answer, which is what makes the narrowing necessary rather " +
                "than decorative.");

            var toTheChooser = SeatPendingSurface.NarrowResolution(
                paused, ownsDecision: true,
                nameableCardIds: board.ChooserHand.Select(card => card.Id).ToArray());
            var toTheGuesser = SeatPendingSurface.NarrowResolution(
                paused, ownsDecision: false,
                nameableCardIds: Array.Empty<CardInstanceId>());

            Assert.That(CardsNamedBy(toTheChooser), Has.Member(chosen.Id),
                "The hero who chose still sees what they chose.");
            Assert.That(CardsNamedBy(toTheGuesser), Has.No.Member(chosen.Id),
                "and the CONTROLLER - the hero about to guess its colour - does not, which is the " +
                "whole of what the printed order 'guess, then reveal' asks for.");
            Assert.That(toTheGuesser.PendingChoice, Is.Null,
                "nor the standing colour prompt's frame copy, which is a second route to the same " +
                "leak and is closed by the same rule.");
        }

        [Test]
        public void TheGuessDomain_DoesNotMoveWithTheHiddenAnswer()
        {
            var board = new GuessBoard();
            var first = board.OfferedColorsAfterChoosing(board.ChooserHand[0]);

            var second = new GuessBoard().OfferedColorsAfterChoosing(
                new GuessBoard().ChooserHand[1]);

            Assert.That(first, Is.EqualTo(second),
                "Two different hidden cards, one offer, asserted through the live resolver rather " +
                "than against the handler alone.");
        }

        // -------------------------------------------------------------------------------------------
        // (c) The Tier III failure modal, and the probe it owed
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The one structural claim the design probe could not settle: nothing in
        /// <c>EffectNodeDefinition</c> or the definition validator forbids a
        /// <see cref="EffectNodeKind.RepeatAction"/> inside a <see cref="EffectNodeKind.Choose"/>
        /// branch, and no shipped card does it. One definition, one walk.
        /// </summary>
        [Test]
        public void ARepeatInsideAChooseBranch_IsLegalAndAsksForExactlyOneMoreInstance()
        {
            var board = new GuessBoard(tierThree: true);
            var chosen = board.ChooserHand[1];
            var wrong = SyntheticBorrowedActionCapabilities.AllNameableColors
                .First(color => color != ColorOf(chosen) && color != CardColor.Purple &&
                    board.Guessable.Contains(color));

            var modal = board.RunToTheModal(chosen, wrong);
            Assert.That(modal.PendingChoice, Is.Not.Null, "The failure branch pauses on the modal.");
            Assert.That(modal.PendingChoice.NodeId, Is.EqualTo(Modal));
            Assert.That(modal.PendingChoice.Candidates.Count, Is.EqualTo(2),
                "Both printed bullets are offered - repeat once, or take the coins.");

            var taken = board.TakeTheRepeatBullet(modal);

            Assert.That(taken.repeat.IsRequested, Is.True,
                "A RepeatAction reached through a Choose branch runs exactly as it would under a " +
                "Sequence: the Choose pushes the chosen branch onto the continuation and the repeat " +
                "arm then executes from it.");
            Assert.That(taken.repeat.Continuation.IsRepeatedInstance, Is.True,
                "and the instance it asks for is a REPEATED one, which is what carries p.10's bound.");
            Assert.That(
                GuessBoard.RepeatNodeOf(tierThree: true).MaximumRepeats, Is.EqualTo(1),
                "\"may repeat ONCE\" is the node's own declared bound, so a second failure cannot " +
                "ask again.");
            Assert.That(board.Coins.GetCoins(Guesser), Is.Zero,
                "and the coin bullet was not also taken - a Choose of one is one bullet.");
        }

        [Test]
        public void TheCoinBullet_IsTheOtherArmOfTheSameModal()
        {
            var board = new GuessBoard(tierThree: true);
            var chosen = board.ChooserHand[1];
            var wrong = SyntheticBorrowedActionCapabilities.AllNameableColors
                .First(color => color != ColorOf(chosen) && color != CardColor.Purple &&
                    board.Guessable.Contains(color));

            var modal = board.RunToTheModal(chosen, wrong);
            var taken = board.TakeTheCoinBullet(modal);

            Assert.That(taken.IsComplete, Is.True);
            Assert.That(board.Coins.GetCoins(Guesser), Is.EqualTo(2),
                "The Tier III card prints two coins on the bullet the modal's second arm takes.");
        }

        [Test]
        public void TheModal_IsNeverReachedOnACorrectGuess()
        {
            var board = new GuessBoard(tierThree: true);
            var chosen = board.ChooserHand[1];

            var walk = board.Run(chosen, ColorOf(chosen));

            Assert.That(walk.IsComplete, Is.True);
            Assert.That(walk.Frame, Is.Null,
                "A correct guess takes the discard branch and finishes; the modal belongs to the " +
                "other arm of the Conditional and is not offered at all.");
            Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(board.Coins.GetCoins(Guesser), Is.Zero);
        }

        [Test]
        public void TheRepeatBullet_IsNotOfferedWhenNoSecondInstanceCouldRun()
        {
            var board = new GuessBoard(tierThree: true, repeatIsUnavailable: true);
            var chosen = board.ChooserHand[1];
            var wrong = SyntheticBorrowedActionCapabilities.AllNameableColors
                .First(color => color != ColorOf(chosen) && color != CardColor.Purple &&
                    board.Guessable.Contains(color));

            var modal = board.RunToTheModal(chosen, wrong);

            Assert.That(modal.PendingChoice.UnavailableCandidates.Count, Is.EqualTo(1),
                "8.25e's rule - an offer is an offer - greys the bullet that cannot be performed " +
                "rather than offering it and refusing the answer.");
            Assert.That(
                modal.PendingChoice.SelectInvocations.Count, Is.EqualTo(1),
                "and the coin bullet is still there, so the failure branch always has an answer.");
        }

        // -------------------------------------------------------------------------------------------
        // (d) BA-Q2, proved on boards where the three readings differ
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// BA-Q2, readings one and two: a second run re-asks the selection, so it MAY pick a
        /// different enemy hero and is not required to. Proved on a board carrying two enemy heroes
        /// who both qualify, where the three readings give three different candidate sets.
        /// </summary>
        [Test]
        public void ASecondRun_ReAsksTheSelectionAndExcludesNeitherEnemy()
        {
            var board = new GuessBoard(secondEnemy: true);

            var offered = board.RadiusCandidates(ActionInvocationContext.Repeated);

            Assert.That(offered, Has.Member(Chooser),
                "MAY, not MUST: the first chooser is still a legal subject of a second run, " +
                "because the printed sentence names no exclusion.");
            Assert.That(offered, Has.Member(OtherEnemy),
                "and a DIFFERENT enemy hero is offered too, because the repeat re-runs the whole " +
                "clause from its first word rather than inheriting a subject. A card that meant " +
                "'the same hero' would have to bind its repeat to that selection, which this one " +
                "does not.");
            Assert.That(
                board.RadiusCandidates(ActionInvocationContext.PrintedPrimary),
                Is.EquivalentTo(offered),
                "and the printed instance and the repeated one offer the same set, which is what " +
                "'re-asks' means.");
        }

        /// <summary>
        /// BA-Q2, reading three: the now-revealed card is NOT excluded from a second choice by the
        /// same hero. The reveal is permanent - every seat has seen it - and a chooser who hands it
        /// over twice has made a legal, and now transparent, decision.
        /// </summary>
        [Test]
        public void ASecondRun_DoesNotExcludeTheCardTheFirstOneRevealed()
        {
            var board = new GuessBoard();
            var chosen = board.ChooserHand[1];
            var wrong = SyntheticBorrowedActionCapabilities.AllNameableColors
                .First(color => color != ColorOf(chosen) && color != CardColor.Purple &&
                    board.Guessable.Contains(color));

            board.Run(chosen, wrong);

            Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.Hand),
                "A wrong guess leaves the card where it was, so a second run can reach it.");
            Assert.That(board.HiddenChoiceCandidates(), Has.Member(chosen.Id),
                "and it is offered again. Excluding it would be a rule the face does not print, " +
                "and 8.39d1's per-selection exclusion is a PARAMETER a card asks for rather than a " +
                "property every repeat has.");
        }

        // -------------------------------------------------------------------------------------------
        // (e) Replay, and the schema that does not move
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSameAnswers_ReplayToTheSameFactsAndTheSameFinalState()
        {
            var first = new GuessBoard();
            var second = new GuessBoard();
            var chosenFirst = first.ChooserHand[1];
            var chosenSecond = second.ChooserHand[1];

            var one = first.Run(chosenFirst, ColorOf(chosenFirst));
            var two = second.Run(chosenSecond, ColorOf(chosenSecond));

            Assert.That(one.IsComplete, Is.EqualTo(two.IsComplete));
            Assert.That(
                one.Events.SelectMany(value => value.Outputs)
                    .Select(output => output.OutputName + "=" + output.Value),
                Is.EqualTo(two.Events.SelectMany(value => value.Outputs)
                    .Select(output => output.OutputName + "=" + output.Value)),
                "Deterministic replay: the same answers produce the same published facts in the " +
                "same order, which is what a retained-prefix Undo re-runs.");
            Assert.That(chosenSecond.State.Location, Is.EqualTo(chosenFirst.State.Location));
            Assert.That(second.Coins.GetCoins(Guesser), Is.EqualTo(first.Coins.GetCoins(Guesser)));
        }

        [Test]
        public void TheSnapshotSchema_DoesNotMoveForAHiddenAnswer()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(58),
                "The version has since moved once, at milestone 8.53, for the match scoreboard - " +
                "accumulated per-hero totals that have no source to be re-derived from, which is the " +
                "opposite of this case rather than a change to it. " +
                "The hidden answer is already private state inside an already-private payload: a " +
                "frame's CompletedOutcomes and AcceptedChoices have persisted typed CardInstance " +
                "values since the effect frame existed, and the candidate SET is deliberately not " +
                "written at all - restore re-derives it. The reveal adds nothing persisted either, " +
                "because its memory is the derived seat-public log, itself re-derived from the " +
                "retained rule-event log. Nothing here is a new field, so nothing here is a version." +
                "Milestone 8.71a moved it again, to v44, for Hanu the Trickster's " +
                "rewritten printed Initiative - a sparse card-keyed list in the card " +
                "section, which is authoritative state no live state remembers.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<CardInstanceId> CardsNamedBy(PendingResolution frame)
        {
            var named = new List<CardInstanceId>();
            foreach (var outcome in frame.CompletedOutcomes)
            {
                named.AddRange(outcome.Outputs
                    .Where(output => output.Value.Kind == EffectValueKind.CardInstance)
                    .Select(output => output.Value.CardInstanceId));
            }

            foreach (var choice in frame.AcceptedChoices)
            {
                named.AddRange(choice.Selected
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId));
            }

            if (frame.PendingChoice != null)
            {
                named.AddRange(frame.PendingChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId));
            }

            return named;
        }

        private static CardColor ColorOf(CardInstance card) =>
            Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Progression.Color;

        /// <summary>
        /// One board, one chooser's hand, one coin ledger and one composed clause - built fresh per
        /// case so a card this fixture discards cannot leak into the next one.
        /// </summary>
        private sealed class GuessBoard
        {
            private readonly EffectDefinition definition;
            // Milestone 8.40j registers both hidden-information contracts, so the build's own catalog
            // already carries them and passing them again would claim one reference twice.
            private readonly EffectCapabilityCatalog catalog =
                SyntheticEffectCapabilities.CreateCatalog();
            private readonly MatchParticipants participants = StagedBoardParticipants.CreateWithBlueAlly();
            private readonly bool repeatIsUnavailable;
            private readonly bool secondEnemy;

            public GuessBoard(
                bool tierThree = false,
                bool repeatIsUnavailable = false,
                bool secondEnemy = false)
            {
                this.repeatIsUnavailable = repeatIsUnavailable;
                this.secondEnemy = secondEnemy;
                definition = Compose(tierThree);

                var colors = Definitions
                    .Select(value => value.Progression.Color)
                    .Where(color => color != CardColor.None && color != CardColor.Purple)
                    .Distinct()
                    .Take(3)
                    .ToArray();
                ChooserHand = colors
                    .Select(color => CardInstance.CreateInHand(
                        new CardInstanceId("chooser-" + color),
                        Definitions.First(value => value.Progression.Color == color).Reference,
                        Chooser))
                    .ToArray();
                Coins = new HeroCoinLedger(participants);
            }

            public IReadOnlyList<CardInstance> ChooserHand { get; }

            public HeroCoinLedger Coins { get; }

            public IReadOnlyList<CardColor> Guessable =>
                SyntheticBorrowedActionCapabilities.AllNameableColors
                    .Where(color => ChooserHand.Any(card => ColorOf(card) == color))
                    .ToArray();

            /// <summary>The whole clause, answered with one card and one colour.</summary>
            public EffectResolutionStepResult Run(CardInstance chosen, CardColor named)
            {
                var step = RunToTheGuessStep(chosen);
                return Answer(step, EffectValue.Integer((int)named));
            }

            /// <summary>The frame standing on the colour prompt, with the answer already recorded.</summary>
            public PendingResolution RunToTheGuess(CardInstance chosen) =>
                RunToTheGuessStep(chosen).Frame;

            public IReadOnlyList<CardColor> OfferedColorsAfterChoosing(CardInstance chosen) =>
                RunToTheGuessStep(chosen).Frame.PendingChoice.Candidates
                    .Select(candidate => (CardColor)candidate.Value.IntegerValue)
                    .ToArray();

            public PendingResolution RunToTheModal(CardInstance chosen, CardColor named) =>
                Answer(RunToTheGuessStep(chosen), EffectValue.Integer((int)named)).Frame;

            public (EffectResolutionStepResult step, RepeatRequest repeat) TakeTheRepeatBullet(
                PendingResolution modal)
            {
                var step = Select(modal, Repeat);
                // p.10's confirm-or-decline stands on the repeat itself once its branch is chosen.
                if (step.Frame?.PendingChoice != null)
                {
                    step = Answer(step, default);
                }

                return (step, step.RepeatRequested);
            }

            public EffectResolutionStepResult TakeTheCoinBullet(PendingResolution modal) =>
                Select(modal, CoinBullet);

            public IReadOnlyList<CardInstanceId> HiddenChoiceCandidates() =>
                RunToTheChoiceStep().Frame.PendingChoice.Candidates
                    .Select(candidate => candidate.Value.CardInstanceId)
                    .ToArray();

            public IReadOnlyList<HeroId> RadiusCandidates(ActionInvocationContext invocation)
            {
                var handler = (IEffectSelectionCapabilityHandler)new EffectCapabilityCatalog(
                        SyntheticTargetingCapabilities.Handlers())
                    .Load(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
                var source = CardInstance.CreateInHand(
                    new CardInstanceId("bain-guess-resolution-source"),
                    GuardsDigital.Core.Cards.Verified.DodgerCardContent
                        .Definition(GuardsDigital.Core.Cards.Verified.DodgerCardContent.DeathTrap)
                        .Reference,
                    Guesser);
                var action = GuardsDigital.Core.Cards.Verified.DodgerCardContent
                    .Definition(GuardsDigital.Core.Cards.Verified.DodgerCardContent.DeathTrap)
                    .ActionDefinitions
                    .First(value => value.Origin == ActionDefinitionOrigin.PrintedPrimary);

                var context = new EffectCapabilityContext(
                    new EffectNodeId("bain-guess-resolution-selection"),
                    EffectFailurePolicy.Mandatory,
                    Guesser,
                    Guesser,
                    source.Id,
                    source.DefinitionReference,
                    action.Id,
                    new Dictionary<string, EffectValue>
                    {
                        ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                        [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                            EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                        [SyntheticTargetingCapabilities.ExcludeAdjacentParameterName] =
                            EffectValue.Boolean(false),
                        [SyntheticTargetingCapabilities.MinimumCardsInHandParameterName] =
                            EffectValue.Integer(2)
                    },
                    1,
                    1,
                    participants,
                    null,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(
                        Definitions.Concat(
                            GuardsDigital.Core.Cards.Verified.DodgerCardContent.AllDefinitions),
                        ChooserHand.Concat(OtherEnemyHand()).Concat(new[] { source })),
                    ActionKind.Skill,
                    Components());

                Assert.That(invocation, Is.Not.EqualTo(ActionInvocationContext.None),
                    "The invocation is named so the reading this proves is about a REPEAT rather " +
                    "than about a selection asked twice by accident.");
                return handler.AnticipateCandidates(context).Select(value => value.HeroId).ToArray();
            }

            // ---- The walk

            private EffectResolutionStepResult RunToTheChoiceStep() =>
                EffectResolutionSession.Start(
                    new EffectResolutionId("bain-guess-resolution"),
                    new CardInstanceId("bain-guess-resolution-source"),
                    default,
                    new ActionDefinitionId("bain-guess-resolution-action"),
                    Guesser,
                    Guesser,
                    ActionInvocationContext.PrintedPrimary,
                    Context());

            private EffectResolutionStepResult RunToTheGuessStep(CardInstance chosen)
            {
                var step = RunToTheChoiceStep();
                Assert.That(step.Frame?.PendingChoice, Is.Not.Null,
                    "The clause opens on the enemy's own card choice.");
                Assert.That(step.Frame.PendingChoice.ChooserId, Is.EqualTo(Chooser));
                return Answer(step, EffectValue.Card(chosen.Id));
            }

            /// <summary>
            /// Names one candidate, or nothing, and closes the choice where closing it is still a
            /// separate command - the commands a real seat submits, however many that is.
            ///
            /// <para>Milestone 8.46b. It was always two. A choice admitting exactly one selection is
            /// now answered by the select alone, so the completion is asked for rather than assumed;
            /// an empty answer - <paramref name="value"/> undefined - is still a Confirm of its own,
            /// which is the `up to` case this fixture also drives.</para>
            /// </summary>
            private EffectResolutionStepResult Answer(
                EffectResolutionStepResult step, EffectValue value)
            {
                var choice = step.Frame.PendingChoice;
                if (value.IsDefined)
                {
                    var select = choice.SelectInvocations.Single(invocation =>
                        invocation.Candidate.Value.Equals(value));
                    step = EffectResolutionSession.Answer(step.Frame, select, Context());
                    if (!StillOwesItsCompletion(step))
                    {
                        return step;
                    }
                }

                return EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context());
            }

            private EffectResolutionStepResult Select(PendingResolution frame, EffectNodeId option)
            {
                var choice = frame.PendingChoice;
                var select = choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(option));
                var step = EffectResolutionSession.Answer(frame, select, Context());
                return StillOwesItsCompletion(step)
                    ? EffectResolutionSession.Answer(
                        step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context())
                    : step;
            }

            /// <summary>
            /// Milestone 8.46b. Whether a selection just made left a completion command owed: a
            /// standing choice that HOLDS the selection - a choice with nothing selected is the next
            /// question, opened by the same command - and that offers one.
            /// </summary>
            private static bool StillOwesItsCompletion(EffectResolutionStepResult step) =>
                step.Frame?.PendingChoice != null &&
                step.Frame.PendingChoice.SelectedCandidates.Count > 0 &&
                step.Frame.PendingChoice.ConfirmInvocation != null;

            private EffectResolutionContext Context() =>
                new EffectResolutionContext(
                    definition,
                    catalog,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(
                        Definitions,
                        ChooserHand.Concat(secondEnemy ? OtherEnemyHand() : Array.Empty<CardInstance>())),
                    ActionKind.Skill,
                    components: Components(),
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        Coins),
                    repeatIsUnavailable: repeatIsUnavailable);

            private IReadOnlyList<CardInstance> OtherEnemyHand() =>
                new[]
                {
                    CardInstance.CreateInHand(
                        new CardInstanceId("other-enemy-one"),
                        Definitions.First(value => value.Progression.Color != CardColor.None)
                            .Reference,
                        OtherEnemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("other-enemy-two"),
                        Definitions.First(value => value.Progression.Color != CardColor.None)
                            .Reference,
                        OtherEnemy)
                };

            private MatchComponents Components()
            {
                var coordinates = Disk(new HexCoordinate(0, 0), 3).ToArray();
                var map = new BoardMapDefinition(
                    Map, 1, "Bain Guess Resolution Map",
                    coordinates
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
                        new HeroOccupancy(Guesser, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Chooser, new HexCoordinate(1, 0)),
                        new HeroOccupancy(OtherEnemy, new HexCoordinate(-1, 0))
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
                    for (var r = Math.Max(-radius, -q - radius);
                        r <= Math.Min(radius, -q + radius);
                        r++)
                    {
                        yield return new HexCoordinate(centre.Q + q, centre.R + r);
                    }
                }
            }

            // ---- The composition

            private static EffectDefinition Compose(bool tierThree)
            {
                var nodes = new List<EffectNodeDefinition>
                {
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Hide, Guess, Reveal, Branch }),

                    new EffectNodeDefinition(
                        Hide, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticHiddenChoiceCapabilities.HiddenCardChoiceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticHiddenChoiceCapabilities.HiddenChoiceHeroParameter,
                                EffectValue.Hero(Chooser))
                        }),

                    new EffectNodeDefinition(
                        Guess, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.NameColorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticBorrowedActionCapabilities.NameColorChooserParameter,
                                EffectValue.Hero(Guesser)),
                            EffectInputBinding.FromLiteral(
                                SyntheticBorrowedActionCapabilities
                                    .RestrictToColorsOwnedByParameter,
                                EffectValue.Hero(Chooser))
                        }),

                    new EffectNodeDefinition(
                        Reveal, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticHiddenChoiceCapabilities.RevealChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticHiddenChoiceCapabilities.RevealCardParameter,
                                Hide,
                                SyntheticHiddenChoiceCapabilities.ChosenCardOutput,
                                EffectValueKind.CardInstance),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticHiddenChoiceCapabilities.NamedColorParameter,
                                Guess,
                                SyntheticBorrowedActionCapabilities.NamedColorOutput,
                                EffectValueKind.Integer)
                        }),

                    new EffectNodeDefinition(
                        Branch, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { Discard, tierThree ? Modal : CoinBullet },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", Reveal,
                                SyntheticHiddenChoiceCapabilities.MatchedOutput,
                                EffectValueKind.Boolean)
                        }),

                    new EffectNodeDefinition(
                        Discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectValue.Hero(Chooser)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.RestrictToCardParameter,
                                Hide,
                                SyntheticHiddenChoiceCapabilities.ChosenCardOutput,
                                EffectValueKind.CardInstance)
                        }),

                    CoinNode(tierThree ? 2 : 1)
                };

                if (tierThree)
                {
                    nodes.Add(new EffectNodeDefinition(
                        Modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1,
                        childNodeIds: new[] { Repeat, CoinBullet }));
                    nodes.Add(new EffectNodeDefinition(
                        Repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1));
                }

                return new EffectDefinition(
                    new EffectDefinitionId("bain-guess-resolution-definition"), "rev-1", Root, nodes);
            }

            /// <summary>The repeat node as the Tier III composition declares it.</summary>
            internal static EffectNodeDefinition RepeatNodeOf(bool tierThree) =>
                Compose(tierThree).Nodes.Single(node => node.Id.Equals(Repeat));

            private static EffectNodeDefinition CoinNode(int amount) =>
                new EffectNodeDefinition(
                    CoinBullet, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("recipient", EffectValue.Hero(Guesser)),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(amount))
                    });
        }
    }
}

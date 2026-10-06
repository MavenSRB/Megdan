using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40f, the privacy half: what a seat may SEE of a running effect frame, as against
    /// what it may submit.
    ///
    /// <para><b>The leak these cases close is not hypothetical and is not Bain's.</b> A foreign-chooser
    /// card choice - Whisper's Greater Good, Misa's Power Overwhelming, Dodger's Death Trap, and the
    /// card-state fixture these cases drive - opens a pause whose candidates are the ENEMY's hand. The
    /// seat surface published the running frame to the ACTING seat as well as to the pause's owner, and
    /// that frame carries the pause. So the acting player's own screen held every card in an opponent's
    /// hand, in process, on shipped content, and has since milestone 7.1a. The wire never carried it -
    /// <c>NetworkMatchReadModel.PendingSkillResolution</c> is null - which made it an asymmetry as well
    /// as a leak: a remote Bain would have been playing a different game from a local one.</para>
    ///
    /// <para><b>Bain's own sentence is what makes it unfixable by authorization.</b> "Guess the card's
    /// color, then reveal the card" needs the answer to survive across the next node, visible to the
    /// chooser's seat and to nobody else. An authorization filter cannot express that - the acting seat
    /// is entitled to act, and it is the seeing that must stop. AUTHORIZATION FILTER IS NOT PROJECTION
    /// FILTER, on the sharpest case this roster has.</para>
    /// </summary>
    public sealed class BainHiddenAnswerProjectionTests
    {
        private static readonly ActionDefinitionId CardStateSkill =
            new ActionDefinitionId("primary-card-state-skill");

        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Chooser = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate ChooserAt = new HexCoordinate(-1, -5);

        // -------------------------------------------------------------------------------------------
        // (a) The live pause, from the chooser's seat and from the seat that is acting
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheChoosersOwnSeat_HoldsTheWholeDecisionAndCanAnswerIt()
        {
            var host = PausedOnAForeignChooserCardChoice();
            var seat = host.Seats.FindByHero(Chooser);
            var projection = host.ProjectionForSeat(seat.Id);

            Assert.That(projection.PendingEffectChoice, Is.Not.Null,
                "The hero whose hand it is owns the decision and sees all of it.");
            Assert.That(projection.PendingEffectChoice.ChooserId, Is.EqualTo(Chooser));
            Assert.That(projection.PendingEffectChoice.Candidates, Is.Not.Empty);
            Assert.That(projection.DecisionContract, Is.Not.Null,
                "and the contract the prompt is drawn from reaches that seat and no other.");
            Assert.That(
                projection.PendingSkillResolution.PendingChoice, Is.Not.Null,
                "The frame published to the OWNER keeps its pause; narrowing it there would take a " +
                "decision away from the only seat entitled to make it.");
            Assert.That(
                projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.CardInstanceId)
                    .All(id => projection.Cards.Any(card => card.Id.Equals(id))),
                Is.True,
                "and every card it is offered is one it may name, which is what lets the HUD draw " +
                "the faces.");
        }

        [Test]
        public void TheActingSeat_IsHandedTheFrameWithoutTheEnemysHand()
        {
            var host = PausedOnAForeignChooserCardChoice();
            var seat = host.Seats.FindByHero(Actor);
            var projection = host.ProjectionForSeat(seat.Id);
            var enemyHand = host.Projection.Cards
                .Where(card => card.OwnerId.Equals(Chooser) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.Id)
                .ToArray();

            Assert.That(enemyHand, Is.Not.Empty, "This case measures nothing against an empty hand.");
            Assert.That(projection.PendingEffectChoice, Is.Null,
                "The pause is not this seat's decision, so this seat is handed no pause.");
            Assert.That(projection.DecisionContract, Is.Null);
            Assert.That(projection.PendingSkillResolution, Is.Not.Null,
                "The frame itself still reaches the acting seat - its identity is what tells a " +
                "surface a resolution is running and what a bot's own intent reads.");
            Assert.That(projection.PendingSkillResolution.PendingChoice, Is.Null,
                "THE LEAK. A copy of the pause riding inside the frame carried the enemy's whole " +
                "hand to the acting player's screen. A non-owner is handed no pause on either " +
                "route now, so there is one rule rather than two that can disagree.");
            Assert.That(
                CardsNamedBy(projection.PendingSkillResolution),
                Has.No.Member(enemyHand[0]));
            Assert.That(
                projection.Cards.Select(card => card.Id).Intersect(enemyHand), Is.Empty,
                "and the card list itself still withholds the hand, which is where the narrowing " +
                "takes its own answer from.");
        }

        [Test]
        public void TheWholeTable_StillLearnsWhoIsBeingWaitedOn()
        {
            var host = PausedOnAForeignChooserCardChoice();

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                Assert.That(projection.DecisionOwner.HeroIds, Has.Member(Chooser),
                    "Who is still choosing is public; what they may choose is not. Hiding the " +
                    "first would leave every other player staring at a table that had stopped.");
            }
        }

        [Test]
        public void TheChoosersSeat_StillAnswersAndTheDiscardStillHappens()
        {
            var host = PausedOnAForeignChooserCardChoice();
            var seat = host.Seats.FindByHero(Chooser);
            var projection = host.ProjectionForSeat(seat.Id);
            var chosen = projection.PendingEffectChoice.Candidates[0].Value.CardInstanceId;

            // Milestone 8.46b: the chooser's seat names exactly one card, so its single press is the
            // whole answer - which is the half of the rule a foreign chooser makes visible, since
            // the seat that must send that command is not the acting one.
            Submit(host, seat.Id, DirectorCommand.AnswerEffectChoice(
                projection.PendingEffectChoice.SelectInvocations
                    .Single(invocation => invocation.Candidate.Value.CardInstanceId.Equals(chosen))));

            Assert.That(
                host.Projection.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The narrowing takes nothing away from the seat that owns the decision: it still " +
                "sees the offer, still selects, still confirms, and the discard still lands.");
        }

        // -------------------------------------------------------------------------------------------
        // (b) The rule itself, as a function - what Bain's own sentence needs
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The state no registered capability can reach, because the two that do are deferred until
        /// Bain is dealt: an ANSWER already given, recorded on the frame, naming a card still sitting
        /// in its owner's hand. Asserted on the arm directly rather than through a match that cannot
        /// yet produce one.
        /// </summary>
        [Test]
        public void AFrameNamingACardThisSeatMayNotSee_IsPublishedWithoutIt()
        {
            var hidden = new CardInstanceId("hidden-answer-card");
            var visible = new CardInstanceId("public-turn-card");
            var frame = FrameNaming(hidden, visible);

            var narrowed = SeatPendingSurface.NarrowResolution(
                frame, ownsDecision: false, nameableCardIds: new[] { visible });

            Assert.That(CardsNamedBy(narrowed), Has.No.Member(hidden),
                "This is the whole of what 'hidden until revealed' needs from the projection: an " +
                "answer the chooser gave reaches no seat that may not already name the card.");
            Assert.That(CardsNamedBy(narrowed), Has.Member(visible),
                "and a card this seat may see is untouched, so nothing a surface reads today breaks.");
            Assert.That(narrowed.ResolutionId, Is.EqualTo(frame.ResolutionId));
            Assert.That(narrowed.ActorId, Is.EqualTo(frame.ActorId));
            Assert.That(narrowed.ControllerId, Is.EqualTo(frame.ControllerId));
            Assert.That(narrowed.EffectDefinitionReference,
                Is.EqualTo(frame.EffectDefinitionReference),
                "It is a NARROWING and not a summary: the definition reference a bot's own intent " +
                "reads is exactly where it was.");
            Assert.That(narrowed.Continuation, Is.EqualTo(frame.Continuation));
            Assert.That(narrowed.CompletedOutcomes.Count, Is.EqualTo(frame.CompletedOutcomes.Count),
                "Every node's outcome is still reported; what is dropped is one fact inside one of " +
                "them, so nothing downstream has to cope with a missing node.");
        }

        [Test]
        public void AFrameNamingOnlyCardsThisSeatMaySee_IsHandedOverUnchanged()
        {
            var visible = new CardInstanceId("public-turn-card");
            var frame = FrameNaming(visible, visible);

            var narrowed = SeatPendingSurface.NarrowResolution(
                frame, ownsDecision: true, nameableCardIds: new[] { visible });

            Assert.That(narrowed, Is.SameAs(frame),
                "The ordinary case allocates nothing and is the frame itself, which is what keeps " +
                "this arm off every path it has nothing to say about.");
        }

        [Test]
        public void AnUnboundedNameableSet_NarrowsNothingButThePause()
        {
            var hidden = new CardInstanceId("hidden-answer-card");
            var frame = FrameNaming(hidden, hidden);

            Assert.That(
                CardsNamedBy(SeatPendingSurface.NarrowResolution(frame, true, null)),
                Has.Member(hidden),
                "A caller that derived no set gets no card narrowing, which is what makes the " +
                "derived set in ForSeat the single place the rule is decided.");
            Assert.That(SeatPendingSurface.NarrowResolution(frame, false, null), Is.Not.Null);
        }

        /// <summary>
        /// The Undo channel, closed structurally rather than by a case that has to reach the right
        /// moment: the marker every seat is published names a hero and a rewind count and has no
        /// member of card type at all, so an Undo taken between the answer and the reveal has
        /// nothing to leak. What the rewind then produces is a frame, and a frame reaches a seat
        /// through the narrowing above.
        /// </summary>
        [Test]
        public void TheUndoMarkerEverySeatIsPublished_HasNoCardShapedMemberToLeakThrough()
        {
            var carriers = typeof(MatchUndoMarker)
                .GetProperties(System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance)
                .Where(property => property.PropertyType == typeof(CardInstanceId))
                .Select(property => property.Name)
                .ToArray();

            Assert.That(carriers, Is.Empty,
                "A rewind marker that named a card would publish one to every seat that can see the " +
                "marker, which is all of them.");

            var host = PausedOnAForeignChooserCardChoice();
            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(
                    CardsNamedBy(host.ProjectionForSeat(seat.Id).PendingSkillResolution)
                        .Except(host.ProjectionForSeat(seat.Id).Cards.Select(card => card.Id)),
                    Is.Empty,
                    "and the frame a rewind would leave standing is narrowed by the same rule, so " +
                    "nothing here has to know about Undo.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // (c) The invariant, over a driven match
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Stated positively and checked at every accepted command of a real six-seat walk: a seat's
        /// running frame names no card that seat's own inventory does not name.
        /// </summary>
        /// <remarks>
        /// It is the general form of the rule Bain's sentence needs, so a later change that re-widens
        /// the frame fails here rather than in one hero's fixture - and it is asserted over shipped
        /// content, where the leak actually lived.
        /// </remarks>
        [Test]
        public void NoSeatsFrame_EverNamesACardThatSeatsOwnInventoryDoesNot()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var checkedSteps = 0;

            var submitted = OwnerScopedProjectionTests.DriveThroughSeats(host, 400, projection =>
            {
                foreach (var seat in host.Seats.Seats)
                {
                    var view = host.ProjectionForSeat(seat.Id);
                    if (view.PendingSkillResolution == null)
                    {
                        continue;
                    }

                    checkedSteps++;
                    var nameable = view.Cards.Select(card => card.Id).ToArray();
                    Assert.That(
                        CardsNamedBy(view.PendingSkillResolution).Except(nameable), Is.Empty,
                        "Seat " + seat.Id + " was handed a frame naming a card it may not see.");
                }

                return false;
            });

            Assert.That(submitted, Is.GreaterThan(0), "The walk carried nothing.");
            Assert.That(checkedSteps, Is.GreaterThan(0),
                "The walk never reached a running Skill frame, so the invariant above measured " +
                "nothing. A seed that stops reaching one is a coverage regression, not a pass.");
        }

        /// <summary>
        /// The memory the reveal reuses rather than duplicating, asserted as an equivalence over a
        /// driven match: another hero's HAND card is in a seat's public list exactly when the bounded
        /// seat-public log names it, and never otherwise.
        /// </summary>
        /// <remarks>
        /// This is why the reveal needed no rules-facing flag and why it is permanent: the row it
        /// writes is the publication, in every seat at once, and it keeps being the publication after
        /// a wrong guess sends the card back to the hand it came from. It is asserted over shipped
        /// content, where the same memory already carries a retrieved card.
        /// </remarks>
        [Test]
        public void AnotherHerosHandCard_IsPublicExactlyWhenTheSeatPublicLogNamesIt()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 260);

            var namedByTheLog = host.Projection.MatchLog
                .Where(entry => entry.SubjectCardInstanceId.IsDefined)
                .Select(entry => entry.SubjectCardInstanceId)
                .ToArray();

            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                var foreignHandCards = host.Projection.Cards
                    .Where(card => !card.OwnerId.Equals(seat.HeroId) &&
                        card.State.Location == CardLocation.Hand &&
                        card.State.FaceOrientation == CardFaceOrientation.FaceUp)
                    .ToArray();

                foreach (var card in foreignHandCards)
                {
                    Assert.That(
                        view.PublicCards.Any(value => value.Id.Equals(card.Id)),
                        Is.EqualTo(namedByTheLog.Contains(card.Id)),
                        "Seat " + seat.Id + " and card " + card.Id + ": the bounded public log, and " +
                        "not a rules-facing flag, is the memory of a hand card's revealed ID.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // (d) The wire the new log kind moves, and the frame it has to keep fitting in
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheRevealRow_CrossesTheWireCarryingTheSameHeroAndCard()
        {
            var entry = new GuardsDigital.Core.Diagnostics.MatchLogEntry(
                7,
                GuardsDigital.Core.Diagnostics.MatchLogEntryKind.CardRevealedByCardText,
                2,
                3,
                subjectHeroId: Chooser,
                subjectCardInstanceId: new CardInstanceId("revealed-card"));

            var row = NetworkMatchLogCodec.ToRow(entry);
            using (var buffer = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
                {
                    NetworkMatchLogCodec.Write(writer, row);
                }

                buffer.Position = 0;
                using (var reader = new System.IO.BinaryReader(buffer, System.Text.Encoding.UTF8, true))
                {
                    Assert.That(NetworkMatchLogCodec.TryRead(reader, out var decoded), Is.True);
                    var back = NetworkMatchLogCodec.ToEntry(decoded);
                    Assert.That(back.Kind, Is.EqualTo(
                        GuardsDigital.Core.Diagnostics.MatchLogEntryKind.CardRevealedByCardText));
                    Assert.That(back.SubjectHeroId, Is.EqualTo(Chooser));
                    Assert.That(back.SubjectCardInstanceId.Value, Is.EqualTo("revealed-card"));
                }
            }

            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(42),
                "8.80 carries the ATTACKING CARD ON THE DEFENSE QUESTION: the pending Defense "
                + "row gained the attacking card's instance id, its definition id and that "
                + "definition's content revision, and the action definition id, all written "
                + "between the attacker id and the attack total - so a /20 peer reads the first "
                + "of those strings where it expects the total's four bytes and mis-decodes the "
                + "rest of the detail record. It rides the CHOICE rather than the Attack FRAME "
                + "because SeatProjection hands that frame to the ACTING seat alone, and the seat "
                + "being asked to defend is by construction not that seat - which is why the "
                + "attacker's card had been missing from the Defense window on every network "
                + "seat, the host's own loopback seat included, since 8.58 drew it. The three "
                + "facts are table-public: the card stands face up in p.4's turn area, already in "
                + "every seat's own card list. SCHEMA IS UNMOVED at v44 - the choice is derived "
                + "from the authoritative Attack state at projection time and never persisted. " +
                "8.35z73 carries COIN-AWARD ROWS: the seat-public match log now names each " +
                "authoritative reward recipient and amount. No bytes grew, but the closed enum " +
                "vocabulary widened, so an /18 peer refuses rather than going dark. " +
                "8.61e carries the SPRUNG-TRAP ROW: the seat-public match log gained " +
                "MoveThroughTrapSprung, which is Min the Dragonmonk's mines going off - an enemy " +
                "hero moves through a mine, it is removed, and for every blast removed that hero " +
                "discards a card if able. NO BYTES GREW: the row spends the subject hero, the " +
                "origin and the amount the layout has carried since 8.35r, and the bump is here for " +
                "the reason 8.40f's and 8.51's were - NetworkMatchLogCodec.TryRead refuses any kind " +
                "its own build does not declare, so a version 17 peer meeting this number refuses the " +
                "whole FRAME and that seat goes dark. The row has to exist because the reveal has no " +
                "other surface: until it goes off a mine is facedown and every seat but its " +
                "controller's is handed a face naming no kind, and the board shows only a space " +
                "emptying. SCHEMA IS UNMOVED at v43 - the row is re-derived from the retained " +
                "rule-event log, and the pause the trap opens is an ordinary effect interrupt whose " +
                "kind the card-action payload already writes as an int. " +
                "8.53 carries the MATCH SCOREBOARD: NetworkSeatProjectionRecord gained a repeated " +
                "row - hero id plus six counters, kills, deaths, attacks at heroes, times defended, " +
                "minions killed and coins earned - written between the per-hero progression rows and " +
                "the Active-effect source list, so a version 16 peer reads that row count as the " +
                "source count and mis-decodes the entire remainder of the frame. It is CARRIED " +
                "because it cannot be derived: every fact these columns count does cross the wire, " +
                "as a seat-public match-log row, but that log is a BOUNDED WINDOW of recent lines, " +
                "so a client counting its own totals would report fewer kills the longer a match ran " +
                "and would start a reconnected seat's board from the moment it came back. SCHEMA " +
                "MOVED WITH IT to v43 - only the second time these two have moved together - because " +
                "the board is accumulated state rather than a derived reading, so it has to be " +
                "persisted. " +
                "8.51 then took it to 16 for the seat-public Hold row (CardHeld, rulebook p.12's Hold by the seat's own command), on the same terms: a version 15 peer meeting kind 32 refuses the whole frame, no bytes grew, schema v42 unmoved. At 8.40f: " +
                "THE ROW GREW NO BYTES - it spends the subject hero and subject card the log has " +
                "carried since 8.35r - so what moved is the VOCABULARY, and that is exactly what " +
                "the decoder validates: a version 14 peer meeting kind 31 refuses the whole frame " +
                "rather than mis-reading one row, which is why this is a version rather than a " +
                "silent widening." +
                "Milestone 8.71a carries the REWRITTEN PRINTED INITIATIVE at /20: each "
                + "seat projection's card sections end in a sparse block naming the cards "
                + "Hanu's Hurry up! has set to 11, which a /19 peer would read as whatever "
                + "field follows the last card row. SCHEMA MOVES WITH IT to v44, because "
                + "the rewrite is authoritative state no live state remembers.");
        }

        [Test]
        public void AKindThisBuildDoesNotDefine_IsRefusedByTheDecoder()
        {
            using (var buffer = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(1);
                    writer.Write(9999);
                }

                buffer.Position = 0;
                using (var reader = new System.IO.BinaryReader(buffer, System.Text.Encoding.UTF8, true))
                {
                    Assert.That(NetworkMatchLogCodec.TryRead(reader, out _), Is.False,
                        "This is the mechanism the protocol bump pays for: an unknown kind is not a " +
                        "row a peer skips, it is a frame a peer cannot decode.");
                }
            }
        }

        /// <summary>
        /// The measurement the reveal owes rather than the estimate: the largest complete seat
        /// projection a six-seat wire walk produces, against the sealed-frame guard.
        /// </summary>
        [Test]
        public void TheLargestSixSeatFrame_StillFitsWithHeadroom()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var largest = 0;

            harness.DriveThroughWire(120, afterAcceptedStep: () =>
            {
                for (var ordinal = 1; ordinal <= 6; ordinal++)
                {
                    largest = Math.Max(largest, harness.LastProjectionPayload(ordinal).Length);
                }
            }, seed: 22);

            Assert.That(largest, Is.GreaterThan(4096),
                "A complete projection this small means the walk measured the wrong thing.");
            Assert.That(
                largest,
                Is.LessThan(PrivateSessionSecureChannel.MaxSealedPayloadBytes / 2),
                "The largest complete projection was " + largest + " bytes against the " +
                PrivateSessionSecureChannel.MaxSealedPayloadBytes + "-byte sealed-frame bound, " +
                "whose own ratchet is half of it. A reveal adds ONE bounded log row to a window " +
                "that is already bounded, so the headroom is measured rather than argued.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>Every card instance reachable from a published frame.</summary>
        private static IReadOnlyList<CardInstanceId> CardsNamedBy(PendingResolution frame)
        {
            if (frame == null)
            {
                return Array.Empty<CardInstanceId>();
            }

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
                named.AddRange(frame.PendingChoice.SelectedCandidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId));
            }

            return named;
        }

        /// <summary>
        /// A frame that has recorded an answer and published a card-valued fact, which is the shape
        /// Bain's clause stands in between the choice and the reveal.
        /// </summary>
        private static PendingResolution FrameNaming(
            CardInstanceId published, CardInstanceId selected)
        {
            var nodeId = new EffectNodeId("hidden-choice-node");
            return new PendingResolution(
                new EffectResolutionId("hidden-answer-frame"),
                new CardInstanceId("hidden-answer-source"),
                new CardDefinitionReference(
                    new CardDefinitionId("hidden-answer-definition"), "rev-1"),
                new ActionDefinitionId("hidden-answer-action"),
                new EffectDefinitionReference(
                    new EffectDefinitionId("hidden-answer-effect"), "rev-1"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                new[] { new EffectContinuationFrame(new EffectNodeId("hidden-answer-root"), 1) },
                new[]
                {
                    new EffectNodeOutcome(
                        nodeId,
                        EffectExecutionResultKind.Applied,
                        new[] { new EffectOutcomeFact("chosenCard", EffectValue.Card(published)) })
                },
                new[]
                {
                    new EffectChoiceRecord(
                        nodeId,
                        new EffectChoiceId("hidden-answer-choice"),
                        EffectChoiceCommandKind.Confirm,
                        new[] { EffectChoiceCandidate.Value_(EffectValue.Card(selected)) })
                });
        }

        /// <summary>
        /// The seat whose own published commands currently offer the card-state Skill's start, or a
        /// pair of nulls while none does.
        /// </summary>
        private static (SeatId seatId, DirectorCommand command) FindStartOfTheCardStateSkill(
            MatchHost host)
        {
            foreach (var seat in host.Seats.Seats)
            {
                var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands
                    .FirstOrDefault(candidate =>
                        candidate.Kind == DirectorCommandKind.StartAction &&
                        candidate.ActionDefinitionId.Equals(CardStateSkill));
                if (command != null)
                {
                    return (seat.Id, command);
                }
            }

            return (default, null);
        }

        private static void Submit(MatchHost host, SeatId seatId, DirectorCommand command)
        {
            var client = host.ClientForSeat(seatId);
            var result = client.Submit(new SeatSubmission(
                new SubmissionId("bain-hidden-" + host.Sequence),
                client.Projection.Version,
                command));
            Assert.That(result.Accepted, Is.True,
                result.Detail + " Command: " + command + ". Published: " +
                string.Join(", ", client.Projection.AuthorizedCommands.Select(value =>
                    value.Kind + ":" + value.ActionDefinitionId + ":" + value.CardInstanceId)));
        }

        /// <summary>
        /// A host paused exactly where Bain's first node pauses: a card choice belonging to a hero who
        /// is not the one acting. It is built from milestone 7.1a's own fixture card, which is
        /// REGISTERED content - the two capabilities 8.40f mints are deferred until Bain is dealt, and
        /// a projection case that needed one of them would be proving the arm on content no host can
        /// run.
        /// </summary>
        private static MatchHost PausedOnAForeignChooserCardChoice()
        {
            var configuration = MatchConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(Actor, ActorAt),
                    new HeroOccupancy(Chooser, ChooserAt)
                },
                Array.Empty<ObstacleOccupancy>());

            var host = MatchHost.Compose(new MatchSessionInitialState(scenario, setup, new[] { map }));

            foreach (var heroId in new[] { Actor, Chooser })
            {
                var card = host.Projection.Cards.Single(candidate =>
                    candidate.OwnerId.Equals(heroId) &&
                    candidate.Id.Value.Contains("demand") &&
                    candidate.State.Location == CardLocation.Hand);
                Submit(host, host.Seats.FindByHero(heroId).Id,
                    DirectorCommand.CommitCard(heroId, card.Id));
            }

            // Rulebook p.7's reveal and p.9's Initiative stand between the commitments and the first
            // action, and on this two-hero board they are answered by whichever seat owns them. The
            // walk is driven from the seats' own published commands rather than from a remembered
            // order, so a later change to that sequence moves this fixture rather than breaking it.
            var start = FindStartOfTheCardStateSkill(host);
            for (var guard = 0; start.command == null && guard < 16; guard++)
            {
                var next = host.Seats.Seats
                    .Select(seat => new
                    {
                        seat.Id,
                        Command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.FirstOrDefault()
                    })
                    .FirstOrDefault(candidate => candidate.Command != null);
                Assert.That(next, Is.Not.Null,
                    "No seat may act and the card-state Skill has not been reached.");
                Submit(host, next.Id, next.Command);
                start = FindStartOfTheCardStateSkill(host);
            }

            Assert.That(start.command, Is.Not.Null,
                "The card-state Skill never became this actor's offer.");
            Submit(host, start.seatId, start.command);

            Assert.That(host.Projection.PendingEffectChoice, Is.Not.Null,
                "This helper is about a standing foreign-chooser card choice.");
            Assert.That(host.Projection.PendingEffectChoice.ChooserId, Is.EqualTo(Chooser));
            return host;
        }
    }
}

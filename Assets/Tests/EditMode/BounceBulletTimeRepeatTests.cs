using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.54. Bounce: "Move 2 spaces in a straight line, ignoring obstacles; if this card is
    /// already resolved as you perform this action, may repeat once." Bullet Time: "After you resolve a
    /// basic card you may perform the primary action on that card."
    ///
    /// <para><b>The user's ruling of 2026-09-19, which is this fixture's spec:</b> "Bounce through
    /// Bullet Time counts as already resolved. Bounce moves 2, the card resolves, the Ultimate grants
    /// the primary on the resolved card, and that performance offers the repeat. Three moves of
    /// 2."</para>
    ///
    /// <para><b>What was wrong.</b> Milestone 8.48 gave the gate the wide reading and it held through
    /// Reload!, where the borrowed card had resolved a turn earlier and the state said so. Through
    /// Bullet Time it still read false, and the cause is an ORDERING rather than a reading: milestone
    /// 8.25e commits a card's resolution status BELOW the <c>ActionEnded</c> checkpoint, so that an
    /// action the turn holds open still has an unresolved card and a selected actor to run on - and
    /// Bullet Time grants its instance AT that checkpoint. The rulebook had resolved the card; the
    /// state had not yet said so; the predicate read the state. Two moves, never three.</para>
    ///
    /// <para><b>What closes it.</b> The predicate now asks which PERFORMANCE is running as well as
    /// where the card is. An instance taken off the card - the one invocation context every printed
    /// borrow and grant produces - counts the card as resolved; the card's own performance and
    /// rulebook p.10's repeat of it do not, because both of those run while the card genuinely is
    /// still being resolved. <c>BasicActionQuery.IsPerformedOffACardAlreadyResolved</c> is where that
    /// classification lives.</para>
    /// </summary>
    [TestFixture]
    public sealed class BounceBulletTimeRepeatTests
    {
        // -------------------------------------------------------------------------------------------
        // The ruling, driven through a turn
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheGrantedPerformance_IsOfferedBouncesRepeat_AndTakingItIsAThirdMoveOfTwo()
        {
            var fixture = new BulletTimeTurn();

            fixture.Drive();

            Assert.That(fixture.GrantOffered, Is.True,
                fixture.Trace() + " | Resolving a basic card offers p.16's grant.");
            Assert.That(fixture.RepeatOffered, Is.True,
                fixture.Trace() + " | The granted performance is taken off a card p.16 has already " +
                "resolved, so Bounce's \"may repeat once\" has to be asked.");
            Assert.That(fixture.RepeatWasDeclinable, Is.True,
                fixture.Trace() + " | \"May\" is a decision the player may refuse, so the question " +
                "carries its own Decline.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(3),
                fixture.Trace() + " | Her own primary, the instance the Ultimate granted, and that " +
                "instance's one repeat: three moves of 2.");
            Assert.That(fixture.MovesOfTwo, Is.EqualTo(3),
                fixture.Trace() + " | Every one of them walked the printed distance exactly.");
            Assert.That(fixture.RepeatOfferedTwice, Is.False,
                fixture.Trace() + " | \"Once\" - the repeat's own instance is not offered another.");
        }

        [Test]
        public void TheRepeat_IsDeclinable_AndDecliningLeavesTwoPerformances()
        {
            var fixture = new BulletTimeTurn(declineTheRepeat: true);

            fixture.Drive();

            Assert.That(fixture.RepeatOffered, Is.True, fixture.Trace());
            Assert.That(fixture.RepeatDeclined, Is.True,
                fixture.Trace() + " | The fixture's premise: this run answered the question with its " +
                "Decline rather than its Confirm.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(2),
                fixture.Trace() + " | Refusing a `may` performs nothing, so the turn is the two " +
                "moves p.16 alone buys.");
        }

        /// <summary>
        /// The invariant milestone 8.48 pinned and this milestone must not move: on the turn Bounce
        /// itself resolves, its own performance is not offered a repeat - the card is mid-resolution,
        /// and a performance that IS the resolution reads the narrow answer.
        /// </summary>
        [Test]
        public void HerOwnPerformanceOfBounce_IsStillOfferedNoRepeat_EvenWithBulletTimeStanding()
        {
            var fixture = new BulletTimeTurn(declineTheGrant: true);

            fixture.Drive();

            Assert.That(fixture.GrantOffered, Is.True,
                fixture.Trace() + " | The Ultimate still fires; this case declines it.");
            Assert.That(fixture.RepeatOffered, Is.False,
                fixture.Trace() + " | Her own performance resolves the card, so \"already resolved\" " +
                "is false while it runs - which is what milestone 8.48 measured and 8.54 keeps.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(1), fixture.Trace());
        }

        // -------------------------------------------------------------------------------------------
        // What the repeat's own question offers - milestone 8.59's floor, on the third performance
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The repeat re-derives its question from where the granted performance left her, and every
        /// space it lights is two away in a straight line - the floor milestone 8.59 stated to the
        /// offer, holding on a performance that milestone could not reach.
        /// </summary>
        [Test]
        public void TheRepeatsQuestion_IsTheStraightLineSpacesTwoAwayFromWhereTheGrantLeftHer()
        {
            var fixture = new BulletTimeTurn();

            fixture.Drive();

            Assert.That(fixture.LineOffers.Count, Is.EqualTo(3),
                fixture.Trace() + " | Three performances, three questions.");
            var stoodAt = fixture.LineOffers.Select(offer => offer.StoodAt).ToArray();
            Assert.That(stoodAt[1], Is.Not.EqualTo(stoodAt[0]),
                fixture.Trace() + " | She moved before the grant asked.");
            Assert.That(stoodAt[2], Is.Not.EqualTo(stoodAt[1]),
                fixture.Trace() + " | And again before the repeat asked.");

            foreach (var offer in fixture.LineOffers)
            {
                Assert.That(offer.Offered, Is.EquivalentTo(offer.StraightLineSpacesTwoAway),
                    fixture.Trace() + " | Each question is exactly the spaces the board leaves free " +
                    "at the printed count, derived where she was standing when it opened.");
                Assert.That(
                    offer.Offered.Any(space => HexDistanceQuery.Calculate(space, offer.StoodAt) != 2),
                    Is.False,
                    fixture.Trace() + " | Not one space nearer than the card's own count.");
            }
        }

        /// <summary>
        /// The case the milestone brief asked for, and the measurement that answers it: a board on
        /// which the repeat can reach nothing CANNOT BE STAGED, and the reason is geometry rather than
        /// a gap in the fixture.
        ///
        /// <para>Every performance of this card is an exact two-space walk down one of p.19's six
        /// files. The reverse of such a walk is the same two spaces in the opposite direction, so its
        /// own file is complete by construction, and its far end is the space the walk just vacated -
        /// which is free precisely because she left it. So the space she came from is always in the
        /// next question, and "the repeat has no legal space" is a state this card cannot reach on any
        /// board.</para>
        ///
        /// <para>What that leaves worth proving is the half that IS reachable and the half a fixture
        /// can starve: the question is re-derived from the live board rather than replayed, so on a
        /// board pared down to one file it offers exactly what that file admits - which is one space
        /// on the pared board and six on the open one. Milestone 8.59 pins the fully starved case
        /// where it is real, on the action's own offer, in <see cref="BounceBorrowedRepeatTests"/>.</para>
        /// </summary>
        [Test]
        public void OnABoardParedToOneFile_TheRepeatStillOffersTheSpaceSheCameFromAndNothingElse()
        {
            var fixture = new BulletTimeTurn(oneFileOnly: true);

            fixture.Drive();

            Assert.That(fixture.LineOffers.Count, Is.EqualTo(3), fixture.Trace());
            var repeat = fixture.LineOffers[2];
            Assert.That(repeat.Offered, Is.EquivalentTo(repeat.StraightLineSpacesTwoAway),
                fixture.Trace() + " | Derived from the pared board, not from the first question.");
            // `.Count` explicitly rather than `Has.Count`: the offer is an array, and Unity's own
            // NUnit resolves that constraint by looking for a `Count` PROPERTY, which an array has
            // not - a difference invisible to the cross-platform harness, whose newer NUnit accepts it.
            Assert.That(repeat.Offered.Count, Is.EqualTo(1),
                fixture.Trace() + " | One file, one direction the printed count can still take.");
            Assert.That(repeat.Offered[0], Is.EqualTo(fixture.LineOffers[1].StoodAt),
                fixture.Trace() + " | And it is the space the granted performance came from, which " +
                "is why a repeat of this card is never asked an empty question.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(3), fixture.Trace());
        }

        // -------------------------------------------------------------------------------------------
        // The same state on a whole composed match: restore, Undo, and a seeded walk that plays on
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.35i's shape, on the frame this milestone opened: a capability choice paused
        /// INSIDE an instance a trigger granted. A restore re-asks the handler rather than replaying a
        /// written answer, so a question whose very existence depends on which performance is running
        /// is exactly the kind a restore can lose - and the invocation context the re-ask reads is the
        /// one the payload carried, not a default.
        /// </summary>
        [Test]
        public void TheGrantsRepeatQuestion_SurvivesSnapshotRestore()
        {
            var session = StagedSwiftTurnPausedOnTheGrantsRepeat(out var swiftId);
            var paused = session.Projection.PendingEffectChoice;

            var payload = session.CaptureSnapshot();
            var revived = ConfiguredMatchFixtures.CreateEngagedStagedSwiftTierThreeSession();
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);

            var restored = revived.Projection.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "The restored match is still standing on Bounce's repeat.");
            Assert.That(restored.NodeId.Value, Is.EqualTo("bounce-repeat"));
            Assert.That(restored.ChooserId, Is.EqualTo(paused.ChooserId));
            Assert.That(restored.DeclineInvocation != null, Is.EqualTo(paused.DeclineInvocation != null),
                "and it is still the decision the live pause offered, refusal included.");
            Assert.That(revived.Projection.AuthoritativeSnapshot,
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A restore of a pause inside a granted performance is the same authoritative state.");
            Assert.That(swiftId.IsDefined, Is.True);
        }

        /// <summary>
        /// Retained-prefix Undo across the answer that takes the repeat. Undo in this build re-derives
        /// from a truncated history rather than inverting an operation, so the whole chain - the turn's
        /// own action, the trigger, the grant, and the repeat the grant's performance was offered - is
        /// re-run from the commitment forward, and has to land on the same bytes.
        /// </summary>
        [Test]
        public void TakingTheRepeat_SurvivesRetainedPrefixUndoAndReplay()
        {
            var session = StagedSwiftTurnPausedOnTheGrantsRepeat(out _);
            var take = session.Projection.PendingEffectChoice.ConfirmInvocation;
            Assert.That(take, Is.Not.Null, "The repeat is taken by confirming its question.");

            var beforeTheAnswer = session.Projection.DiagnosticSnapshot;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(take));
            var afterTheAnswer = session.Projection.DiagnosticSnapshot;
            Assert.That(afterTheAnswer, Is.Not.EqualTo(beforeTheAnswer),
                "Taking the repeat is a command that changed the match, or this case proves nothing.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeTheAnswer),
                "A retained-prefix Undo steps the whole answer back, re-derived through the grant " +
                "and the trigger that produced the question.");

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(take));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterTheAnswer),
                "and replaying the same answer reproduces the same ordered facts, byte for byte.");
        }

        /// <summary>
        /// The seeded walk, STAGED rather than waited for - milestone 8.35f's recorded lesson, that a
        /// state a random walk passes through cannot be tested by waiting for the walk to stop in it.
        /// The match is driven to the repeat, the repeat is taken, and the seeded driver plays the rest
        /// of the match from there answering only what the projection offers.
        ///
        /// <para>What it proves is the half the fixtures above cannot: that a turn which took three
        /// performances of one card leaves a match every later decision can still be answered from. A
        /// stall is a live match a player has no legal command for, and a refused command is an offer
        /// the rules do not honour; the driver fails on either.</para>
        /// </summary>
        [Test]
        public void AStagedSeededWalkThatTakesTheRepeat_PlaysTheMatchOnWithoutStalling()
        {
            var session = StagedSwiftTurnPausedOnTheGrantsRepeat(out var swiftId);
            var start = HeroSpace(session, swiftId);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            // The repeat's own destination question, answered so the third performance really happens.
            var destination = session.Projection.PendingEffectChoice;
            Assert.That(destination, Is.Not.Null, "The repeat asks where to bounce a third time.");
            Assert.That(destination.NodeId.Value, Is.EqualTo("bounce-line"));
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, destination.SelectInvocations[0]);
            Assert.That(HeroSpace(session, swiftId), Is.Not.EqualTo(start),
                "The third move moved her.");

            var record = DeterministicMatchLoop.Play(session, 1, 4000);

            Assert.That(record.Stalled, Is.False,
                "A turn that spent three performances of one card left a decision nothing could " +
                "answer.");
        }

        // -------------------------------------------------------------------------------------------
        // The turn itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The composed match, driven to the exact moment Bounce's repeat is asked of the performance
        /// Bullet Time granted: Swift commits Bounce, resolves it, the Ultimate offers the primary of
        /// the card she just resolved, and that performance reaches the repeat.
        ///
        /// <para>Nothing here names a command the projection was not already offering, which is what
        /// makes the position a match can actually reach rather than one the fixture asserted.</para>
        /// </summary>
        private static MatchSession StagedSwiftTurnPausedOnTheGrantsRepeat(out HeroId swiftId)
        {
            var session = ConfiguredMatchFixtures.CreateEngagedStagedSwiftTierThreeSession();
            var bounce = session.Projection.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(SwiftCardContent.Bounce));
            swiftId = bounce.OwnerId;

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand &&
                    (side != TeamSide.Blue || candidate.Id.Equals(bounce.Id)));
                ConfiguredMatchFixtures.Accept(
                    session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide,
                    tie.CandidateCardInstanceIds.Contains(bounce.Id)
                        ? bounce.Id
                        : tie.CandidateCardInstanceIds[0]));
            }

            // Whoever Initiative reaches first Holds until Swift's own card is the selected one.
            for (var step = 0; step < 8; step++)
            {
                if (session.Projection.PendingTurnRelease != null)
                {
                    ConfiguredMatchFixtures.Accept(session, DirectorCommand.ReleaseTurn());
                    continue;
                }

                if (session.Projection.SelectedActor.CardInstanceId.Equals(bounce.Id))
                {
                    break;
                }

                ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(bounce.Id),
                "Initiative reached the card this fixture is about.");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(SwiftCardContent.PrimaryBasicSkill));

            for (var step = 0; step < 40; step++)
            {
                var pending = session.Projection.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null,
                    "The drive is still inside the chain that reaches the repeat, at step " + step);
                if (pending.NodeId.Value == "bounce-repeat")
                {
                    return session;
                }

                var answer = pending.SelectInvocations.Count > 0
                    ? pending.SelectInvocations[0]
                    : pending.ConfirmInvocation;
                Assert.That(answer, Is.Not.Null,
                    "Node " + pending.NodeId.Value + " offered no answer.");
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(answer));
            }

            throw new InvalidOperationException(
                "The staged Swift turn never reached Bounce's repeat.");
        }

        private static HexCoordinate HeroSpace(MatchSession session, HeroId heroId) =>
            session.Projection.Heroes.Single(hero => hero.HeroId.Equals(heroId)).Coordinate;

        /// <summary>
        /// One of Swift's turns with Bullet Time unlocked and Bounce committed, staged so p.16's
        /// ability fires on the card she is resolving.
        ///
        /// <para>Shaped after <see cref="BounceBorrowedRepeatTests"/>'s own driver and
        /// <c>GrantedTargetExclusionTests.BulletTimeTurn</c>: it answers whichever decision the turn is
        /// actually waiting on and records what it was shown, so nothing here names a command the rules
        /// were not already offering.</para>
        /// </summary>
        private sealed class BulletTimeTurn
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly CardInstance bounce;
            private readonly bool declineTheGrant;
            private readonly bool declineTheRepeat;
            private readonly List<string> trace = new List<string>();
            private readonly List<LineOffer> lineOffers = new List<LineOffer>();
            private readonly TurnResolutionAuthority turn;
            private readonly AdjacentMoveAuthority board;
            private readonly IReadOnlyList<HexCoordinate> playable;
            private readonly IReadOnlyList<HeroId> everyHero;
            private readonly HeroId actorId;

            public BulletTimeTurn(
                bool declineTheGrant = false,
                bool declineTheRepeat = false,
                bool oneFileOnly = false)
            {
                this.declineTheGrant = declineTheGrant;
                this.declineTheRepeat = declineTheRepeat;
                var configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var others = configuration.Participants.Heroes
                    .Select(hero => hero.Id)
                    .Where(id => !id.Equals(actor))
                    .ToArray();
                var bounceDefinition = SwiftCardContent.Definition(SwiftCardContent.Bounce);
                var ultimateDefinition = SwiftCardContent.Definition(SwiftCardContent.BulletTime);
                bounce = CardInstance.CreateInHand(
                    new CardInstanceId("bullet-repeat-bounce"), bounceDefinition.Reference, actor);
                var ultimate = CardInstance.CreateInDeck(
                    new CardInstanceId("bullet-repeat-ultimate"), ultimateDefinition.Reference, actor);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                    "p.16 stands the unlocked Ultimate out of the deck, which is what makes it fire.");

                definitions = configuration.CardDefinitions
                    .Concat(new[] { bounceDefinition, ultimateDefinition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                instances = configuration.CardInstances.Concat(new[] { bounce, ultimate }).ToArray();

                var startsAt = new HexCoordinate(0, 0);
                var coordinates = new List<HexCoordinate>();
                for (var q = -5; q <= 5; q++)
                for (var r = -5; r <= 5; r++)
                {
                    if (Math.Abs(q + r) <= 5) coordinates.Add(new HexCoordinate(q, r));
                }

                // The pared board: one file through her starting space and nothing else, which is the
                // smallest board three performances of this card can be walked on.
                if (oneFileOnly)
                {
                    var file = new List<HexCoordinate>();
                    for (var step = -4; step <= 4; step++)
                    {
                        file.Add(new HexCoordinate(step, 0));
                    }

                    coordinates.RemoveAll(value => !file.Contains(value));
                }

                playable = coordinates.ToArray();
                actorId = actor;
                everyHero = new[] { actor }.Concat(others).ToArray();

                // The other seats stand off the file on the open board and off the board entirely on
                // the pared one, so nothing of theirs narrows what this card is offered.
                var stands = new[]
                {
                    new HexCoordinate(4, -4), new HexCoordinate(0, 4), new HexCoordinate(-4, 4)
                };
                board = new AdjacentMoveAuthority(
                    coordinates,
                    new[] { new HeroOccupancy(actor, startsAt) }
                        .Concat(others
                            .Select((id, index) => new HeroOccupancy(id, stands[index]))
                            .Where(occupancy => coordinates.Contains(occupancy.Coordinate))),
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                var cardActions = new CardActionResolutionAuthority(
                    new MatchContentCatalogs(
                        SyntheticEffectCapabilities.CreateCatalog(
                            SyntheticPlacementCapabilities.DeferredHandlers()
                                .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                        SyntheticComponentDefinitions.CreateCatalog(),
                        new TriggeredEffectCatalog(
                            SwiftEffectContent.TriggeredEffects(SwiftCardContent.ContentRevision))),
                    board);

                var committed = new List<CardInstance> { bounce };
                foreach (var owner in others)
                {
                    committed.Add(configuration.CardInstances.First(value =>
                        value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand));
                }

                foreach (var card in committed)
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                }

                typeof(CardActionResolutionAuthority)
                    .GetMethod("BindRosterState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(cardActions, new object[]
                    {
                        EffectRosterState.Of(
                            new[]
                            {
                                new RoundTurnSlot(
                                    1, 1,
                                    committed.Select(card =>
                                        new TurnCardAssignment(card.OwnerId, card.Id, false)),
                                    instances,
                                    configuration.Participants)
                            },
                            new RoundHeroDefeatHistory(configuration.Participants),
                            new HeroCoinLedger(configuration.Participants))
                    });

                turn = new TurnResolutionAuthority(
                    definitions, instances, configuration.Participants,
                    new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
                Assert.That(turn.TryStart().Accepted, Is.True);
            }

            public bool GrantOffered { get; private set; }

            public bool RepeatOffered { get; private set; }

            public bool RepeatOfferedTwice { get; private set; }

            public bool RepeatWasDeclinable { get; private set; }

            public bool RepeatDeclined { get; private set; }

            public int PerformedMoves { get; private set; }

            /// <summary>How many of those performances walked exactly the printed two spaces.</summary>
            public int MovesOfTwo { get; private set; }

            public IReadOnlyList<LineOffer> LineOffers => lineOffers;

            public string Trace() => string.Join(",", trace);

            public void Drive()
            {
                var started = false;
                var repeatsAsked = 0;
                for (var step = 0; step < 120 && !turn.IsEnded; step++)
                {
                    if (!started && turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        trace.Add("tie");
                        Assert.That(turn.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(bounce.Id)
                                ? bounce.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (!started && turn.PendingTurnRelease != null)
                    {
                        trace.Add("release");
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (turn.PendingEffectChoice != null)
                    {
                        var effect = turn.PendingEffectChoice;
                        trace.Add("choice:" + effect.NodeId.Value);
                        var answer = effect.SelectInvocations.Count > 0
                            ? effect.SelectInvocations[0]
                            : effect.ConfirmInvocation;

                        if (effect.NodeId.Value == "bullet-borrow")
                        {
                            GrantOffered = true;
                            if (declineTheGrant)
                            {
                                Assert.That(effect.DeclineInvocation, Is.Not.Null,
                                    Trace() + " | \"You MAY perform\" is a decision p.10 lets its " +
                                    "controller refuse.");
                                answer = effect.DeclineInvocation;
                                trace.Add("decline:grant");
                            }
                        }

                        if (effect.NodeId.Value == "bounce-repeat")
                        {
                            repeatsAsked++;
                            RepeatOffered = true;
                            RepeatOfferedTwice = repeatsAsked > 1;
                            RepeatWasDeclinable = effect.DeclineInvocation != null;
                            if (declineTheRepeat)
                            {
                                Assert.That(effect.DeclineInvocation, Is.Not.Null, Trace());
                                answer = effect.DeclineInvocation;
                                RepeatDeclined = true;
                                trace.Add("decline:repeat");
                            }
                        }

                        // One line question is visited once to select and once to confirm, so a
                        // performance is counted at its first visit.
                        if (effect.NodeId.Value == "bounce-line" &&
                            effect.SelectedCandidates.Count == 0)
                        {
                            PerformedMoves++;
                            var stoodAt = ActorStandsAt();
                            lineOffers.Add(new LineOffer(
                                stoodAt,
                                effect.Candidates
                                    .Where(candidate =>
                                        candidate.Value.Kind == EffectValueKind.Coordinate)
                                    .Select(candidate => candidate.Value.Coordinate)
                                    .ToArray(),
                                SpacesAtExactly(stoodAt, 2)));
                        }

                        Assert.That(answer, Is.Not.Null,
                            Trace() + " | " + effect.NodeId.Value + " offered no answer at all.");
                        var before = ActorStandsAt();
                        Assert.That(turn.TryAnswerEffectChoice(answer).Accepted, Is.True,
                            Trace() + " | " + effect.NodeId.Value + " refused its answer.");
                        var after = ActorStandsAt();
                        if (!after.Equals(before) && HexDistanceQuery.Calculate(before, after) == 2)
                        {
                            MovesOfTwo++;
                        }

                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(bounce.Id))
                        {
                            trace.Add("hold");
                            Assert.That(turn.TryPerformHold(
                                turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, bounce.OwnerId, bounce.Id)
                            .Single(value => value.ActionDefinitionId.Equals(
                                SwiftCardContent.PrimaryBasicSkill));
                        Assert.That(turn.TryStartSkill(
                            new ActionResolutionId("bullet-repeat-own"), offer).Accepted, Is.True);
                        started = true;
                        continue;
                    }

                    trace.Add("break:ended=" + turn.IsEnded);
                    break;
                }
            }

            private HexCoordinate ActorStandsAt()
            {
                Assert.That(
                    board.TryGetObjectCoordinate(BoardObjectReference.ForHero(actorId), out var at),
                    Is.True, "The acting hero is on the board.");
                return at;
            }

            /// <summary>
            /// The spaces exactly <paramref name="steps"/> away from <paramref name="from"/> down one
            /// of p.19's six files that this board can end a walk on: every space of the file exists
            /// and nobody is standing on the last one. Derived from the live board, because each
            /// question is asked from wherever the last walk left her.
            /// </summary>
            private IReadOnlyList<HexCoordinate> SpacesAtExactly(HexCoordinate from, int steps)
            {
                var found = new List<HexCoordinate>();
                var taken = everyHero
                    .Where(id => !id.Equals(actorId))
                    .Where(id => board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(id), out _))
                    .Select(id =>
                    {
                        board.TryGetObjectCoordinate(BoardObjectReference.ForHero(id), out var at);
                        return at;
                    })
                    .ToArray();

                foreach (var direction in EveryDirection())
                {
                    var file = Enumerable.Range(1, steps)
                        .Select(step => StraightLineQuery.Step(from, direction, step))
                        .ToArray();
                    if (file.All(space => playable.Contains(space)) &&
                        !taken.Contains(file[file.Length - 1]))
                    {
                        found.Add(file[file.Length - 1]);
                    }
                }

                return found;
            }

            private static IEnumerable<HexDirection> EveryDirection() =>
                new[]
                {
                    HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                    HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
                };
        }

        /// <summary>One opening of <c>bounce-line</c>: where she stood and what she was offered.</summary>
        private sealed class LineOffer
        {
            public LineOffer(
                HexCoordinate stoodAt,
                IReadOnlyList<HexCoordinate> offered,
                IReadOnlyList<HexCoordinate> straightLineSpacesTwoAway)
            {
                StoodAt = stoodAt;
                Offered = offered;
                StraightLineSpacesTwoAway = straightLineSpacesTwoAway;
            }

            public HexCoordinate StoodAt { get; }

            public IReadOnlyList<HexCoordinate> Offered { get; }

            public IReadOnlyList<HexCoordinate> StraightLineSpacesTwoAway { get; }
        }
    }
}

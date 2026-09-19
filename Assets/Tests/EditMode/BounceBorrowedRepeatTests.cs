using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.48. Bounce: "Move 2 spaces in a straight line, ignoring obstacles; if this card is
    /// already resolved as you perform this action, may repeat once." Reload!: "Perform the primary
    /// action of your rightmost resolved card."
    ///
    /// <para><b>Found at the table, in the 2026-09-18 playtest:</b> "Swift's gold didn't let me play
    /// her silver twice." The player resolved Bounce, then played Reload! and borrowed it - the one
    /// path Bounce's own sentence exists for - and the repeat never came. The gate asked
    /// <c>synthetic-source-card-in-discard@1</c> on the NARROW reading, which is true only of a card
    /// in the discard pile; a card resolved this round sits in the turn area, and Reload!'s pool names
    /// precisely such a card, so the clause could never hold through Reload!. Blizzard's "in your
    /// discard" met the same shape at milestone 8.37a and opted into the wide reading
    /// (<see cref="SyntheticConditionCapabilities.CountResolvedPlayedParameter"/>); Bounce now does the
    /// same, at its one call site.</para>
    ///
    /// <para><b>What stays as it was:</b> on Bounce's own turn the card is mid-resolution - played,
    /// revealed, not yet resolved - so the wide reading is still false and the repeat is still not
    /// offered, which is the printed reading the Play Mode journey already pins.</para>
    ///
    /// <para><b>Milestone 8.59 added the offer's own shape to this fixture</b>, because the second
    /// table report about this card is about the question rather than the repeat: "why are unavailable
    /// spaces offered for this silver? It should just offer legal spaces 2 away, not this space 1 away
    /// which is illegal." The cases below drive the same turn and read what <c>bounce-line</c> actually
    /// put in front of the player, first performance and repeat both. The parameter that carries the
    /// fix, and what it does to every other body of the shared declaration, is
    /// <see cref="BounceOfferTests"/>.</para>
    /// </summary>
    [TestFixture]
    public sealed class BounceBorrowedRepeatTests
    {
        [Test]
        public void ReloadsBorrowOfBounce_OffersBouncesRepeat_BecauseBounceIsAlreadyResolved()
        {
            var fixture = new SwiftTurn(borrowThroughReload: true);

            fixture.Drive();

            Assert.That(fixture.BorrowCandidates, Has.Member(fixture.BounceInstanceId),
                fixture.Trace() + " | Reload!'s pool is the rightmost resolved card, and Bounce is the " +
                "card resolved in the turn before.");
            Assert.That(fixture.RepeatOffered, Is.True,
                fixture.Trace() + " | Performed from Reload!, Bounce IS already resolved, so its " +
                "\"may repeat once\" has to be asked.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(2),
                fixture.Trace() + " | The borrowed performance and its one repeat: the silver played " +
                "twice, which is what the gold buys.");
        }

        [Test]
        public void BounceOnItsOwnTurn_StillOffersNoRepeat()
        {
            var fixture = new SwiftTurn(borrowThroughReload: false);

            fixture.Drive();

            Assert.That(fixture.RepeatOffered, Is.False,
                fixture.Trace() + " | On the turn Bounce itself resolves it is not yet resolved, " +
                "and the wide reading does not change that.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(1), fixture.Trace());
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.59. What the question actually offers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The report, answered on her own turn: every space the question lights is two spaces away in
        /// a straight line, and no space one away is among them.
        ///
        /// <para>Asserted as a SET EQUALITY against the six spaces the board admits at that distance,
        /// rather than as "no candidate is one away". A rule stated only as an absence passes on an
        /// offer that lost half its legal answers as well as its illegal ones, and the card has to keep
        /// offering every direction she can actually take.</para>
        /// </summary>
        [Test]
        public void BouncesQuestion_OffersEveryStraightLineSpaceTwoAwayAndNothingNearer()
        {
            var fixture = new SwiftTurn(borrowThroughReload: false);

            fixture.Drive();

            Assert.That(fixture.LineOffers, Has.Count.EqualTo(1),
                fixture.Trace() + " | One performance, one question.");
            var offer = fixture.LineOffers[0];
            Assert.That(offer.Offered, Is.EquivalentTo(offer.StraightLineSpacesTwoAway),
                fixture.Trace() + " | \"Move 2 spaces in a straight line\" is a count: the offer is " +
                "exactly the spaces at that count which the board leaves free.");
            Assert.That(offer.Offered, Is.Not.Empty, fixture.Trace() +
                " | The fixture's own premise - a bare board around her - so an empty offer here " +
                "would mean the narrowing took everything rather than the illegal half.");
            Assert.That(
                offer.Offered.Any(space =>
                    HexDistanceQuery.Calculate(space, offer.StoodAt) != 2),
                Is.False,
                fixture.Trace() + " | Not one space one away, which is the whole report.");
        }

        /// <summary>
        /// The repeat asks the same question again from wherever the first walk left her, which is the
        /// half a fix applied to one node could have missed: a repeated action re-derives its offer
        /// rather than replaying the first one.
        /// </summary>
        [Test]
        public void TheBorrowedRepeat_AsksTheSameExactQuestionFromTheSpaceTheFirstWalkLeftHerOn()
        {
            var fixture = new SwiftTurn(borrowThroughReload: true);

            fixture.Drive();

            Assert.That(fixture.LineOffers, Has.Count.EqualTo(2),
                fixture.Trace() + " | The borrowed performance and its one repeat.");
            Assert.That(
                fixture.LineOffers[1].StoodAt, Is.Not.EqualTo(fixture.LineOffers[0].StoodAt),
                fixture.Trace() + " | She moved, so the second question is asked from somewhere else.");

            foreach (var offer in fixture.LineOffers)
            {
                Assert.That(offer.Offered, Is.EquivalentTo(offer.StraightLineSpacesTwoAway),
                    fixture.Trace() + " | Both offers are the same shape, derived where she stands.");
            }
        }

        /// <summary>
        /// Rulebook p.11 on an exact distance that cannot be walked: the action stops. A board whose
        /// every straight-line space two away is missing offers nothing at all - and, crucially, does
        /// NOT fall back to the six spaces one away that the same walk still reaches, which is exactly
        /// what it used to offer.
        /// </summary>
        [Test]
        public void ABoardWithNoStraightLineSpaceTwoAway_OffersNothingRatherThanASpaceNearer()
        {
            var fixture = new SwiftTurn(borrowThroughReload: false, boxedIn: true);

            fixture.Drive();

            Assert.That(fixture.OneSpaceAwayIsReachable, Is.True,
                fixture.Trace() + " | The fixture's premise: the walk still reaches six spaces one " +
                "away, so an empty offer below is the printed distance refusing them rather than the " +
                "board refusing everything.");
            Assert.That(fixture.LineOffers, Is.Empty,
                fixture.Trace() + " | No question is opened at all, because it would have no answer.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(0), fixture.Trace());
            Assert.That(fixture.SkillWasAvailable, Is.False,
                fixture.Trace() + " | And the action is not offered as available either, so the stop " +
                "is a control the player never presses rather than a turn spent on nothing.");
        }

        /// <summary>
        /// One of Swift's turns on a bare board. With <c>borrowThroughReload</c> the round's first slot
        /// holds Bounce, already resolved, and the turn being played is Reload!'s; without it the
        /// turn being played is Bounce's own. With <c>boxedIn</c> the board is the same disk minus
        /// every straight-line space two away from where she stands.
        /// </summary>
        private sealed class SwiftTurn
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly CardInstance bounce;
            private readonly CardInstance reload;
            private readonly CardInstance acting;
            private readonly bool borrowThroughReload;
            private readonly bool boxedIn;
            private readonly List<string> trace = new List<string>();
            private readonly List<CardInstanceId> borrowCandidates = new List<CardInstanceId>();
            private readonly List<LineOffer> lineOffers = new List<LineOffer>();
            private readonly TurnResolutionAuthority turn;
            private readonly AdjacentMoveAuthority board;
            private readonly IReadOnlyList<HexCoordinate> playable;
            private readonly IReadOnlyList<HeroId> everyHero;
            private readonly HeroId actorId;

            public SwiftTurn(bool borrowThroughReload, bool boxedIn = false)
            {
                this.borrowThroughReload = borrowThroughReload;
                this.boxedIn = boxedIn;
                var configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var others = configuration.Participants.Heroes
                    .Select(hero => hero.Id)
                    .Where(id => !id.Equals(actor))
                    .ToArray();
                var bounceDefinition = SwiftCardContent.Definition(SwiftCardContent.Bounce);
                var reloadDefinition = SwiftCardContent.Definition(SwiftCardContent.Reload);
                bounce = CardInstance.CreateInHand(
                    new CardInstanceId("swift-bounce"), bounceDefinition.Reference, actor);
                reload = CardInstance.CreateInHand(
                    new CardInstanceId("swift-reload"), reloadDefinition.Reference, actor);
                acting = borrowThroughReload ? reload : bounce;

                definitions = configuration.CardDefinitions
                    .Concat(new[] { bounceDefinition, reloadDefinition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                instances = configuration.CardInstances.Concat(new[] { bounce, reload }).ToArray();

                var startsAt = new HexCoordinate(0, 0);
                var coordinates = new List<HexCoordinate>();
                for (var q = -5; q <= 5; q++)
                for (var r = -5; r <= 5; r++)
                {
                    if (Math.Abs(q + r) <= 5) coordinates.Add(new HexCoordinate(q, r));
                }

                // Milestone 8.59. The p.11 board: the same disk with every straight-line space TWO
                // away from her removed, and every space one away left where it is. The walk still
                // reaches six spaces; the printed distance admits none of them.
                if (boxedIn)
                {
                    var unreachable = EveryDirection()
                        .Select(direction => StraightLineQuery.Step(startsAt, direction, 2))
                        .ToArray();
                    coordinates.RemoveAll(value => unreachable.Contains(value));
                }

                playable = coordinates.ToArray();
                actorId = actor;
                everyHero = new[] { actor }.Concat(others).ToArray();

                var stands = new[] { new HexCoordinate(4, 0), new HexCoordinate(0, 4), new HexCoordinate(-4, 4) };
                board = new AdjacentMoveAuthority(
                    coordinates,
                    new[] { new HeroOccupancy(actor, startsAt) }
                        .Concat(others.Select((id, index) => new HeroOccupancy(id, stands[index]))),
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

                // The turn being played: the acting card and one card from each other hero, all
                // committed and revealed the way p.7 leaves them when the turn starts.
                var committed = new List<CardInstance> { acting };
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

                var slots = new List<RoundTurnSlot>();
                var turnIndex = 1;
                if (borrowThroughReload)
                {
                    // The turn before: Bounce, played, revealed and RESOLVED - which p.6's turn area
                    // keeps until the end of the round. This is the card the table's player had just
                    // played when the gold refused to play it again.
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(bounce), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(bounce), Is.True);
                    Assert.That(CardLifecycleAuthority.TryResolveAfterAction(bounce), Is.True);
                    Assert.That(bounce.State.Location, Is.EqualTo(CardLocation.TurnArea),
                        "A card resolved this round is in the turn area, not the discard pile.");
                    // A slot names one card per configured hero, so the others resolved one too.
                    var earlier = new List<TurnCardAssignment>
                    {
                        new TurnCardAssignment(actor, bounce.Id, false)
                    };
                    foreach (var owner in others)
                    {
                        var card = configuration.CardInstances.First(value =>
                            value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand);
                        Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
                        earlier.Add(new TurnCardAssignment(owner, card.Id, false));
                    }

                    slots.Add(new RoundTurnSlot(1, 1, earlier, instances, configuration.Participants));
                    turnIndex = 2;
                }

                slots.Add(new RoundTurnSlot(
                    1, turnIndex,
                    committed.Select(card => new TurnCardAssignment(card.OwnerId, card.Id, false)),
                    instances,
                    configuration.Participants));

                typeof(CardActionResolutionAuthority)
                    .GetMethod("BindRosterState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(cardActions, new object[]
                    {
                        EffectRosterState.Of(
                            slots,
                            new RoundHeroDefeatHistory(configuration.Participants),
                            new HeroCoinLedger(configuration.Participants))
                    });

                turn = new TurnResolutionAuthority(
                    definitions, instances, configuration.Participants,
                    new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(),
                    1, turnIndex);
                Assert.That(turn.TryStart().Accepted, Is.True);
            }

            public CardInstanceId BounceInstanceId => bounce.Id;

            public bool RepeatOffered { get; private set; }

            public int PerformedMoves { get; private set; }

            public IReadOnlyList<CardInstanceId> BorrowCandidates => borrowCandidates;

            /// <summary>Milestone 8.59. Every <c>bounce-line</c> question, in the order it was asked.</summary>
            public IReadOnlyList<LineOffer> LineOffers => lineOffers;

            /// <summary>Whether the primary action was offered as one the player may press at all.</summary>
            public bool SkillWasAvailable { get; private set; }

            /// <summary>
            /// The premise the boxed-in board needs stated rather than assumed: the walk still reaches
            /// spaces ONE away from her. An empty offer is then the printed distance refusing them,
            /// not a board that had nothing to give.
            /// </summary>
            public bool OneSpaceAwayIsReachable =>
                SpacesAtExactly(ActorStandsAt(), 1).Count > 0;

            public string Trace() => string.Join(",", trace);

            public void Drive()
            {
                var started = false;
                for (var step = 0; step < 80 && !turn.IsEnded; step++)
                {
                    if (!started && turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        trace.Add("tie");
                        Assert.That(turn.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(acting.Id)
                                ? acting.Id
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
                        if (effect.NodeId.Value == "reload-borrow")
                        {
                            borrowCandidates.AddRange(effect.Candidates
                                .Where(candidate =>
                                    candidate.Value.Kind == EffectValueKind.CardInstance)
                                .Select(candidate => candidate.Value.CardInstanceId));
                        }

                        if (effect.NodeId.Value == "bounce-repeat")
                        {
                            RepeatOffered = true;
                        }

                        // One line choice is visited twice - once to select the destination and once
                        // to confirm it - so the performance is counted at its first visit.
                        if (effect.NodeId.Value == "bounce-line" &&
                            effect.SelectedCandidates.Count == 0)
                        {
                            PerformedMoves++;

                            // Milestone 8.59. What the player is actually looking at, read where she
                            // is standing when the question opens rather than where she started.
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

                        // Reload!'s modal is answered on its borrow arm, never its defeat arm; every
                        // other question takes its first candidate, or its confirm when it has none -
                        // which is what accepts the repeat when it is asked.
                        var borrowArm = effect.SelectInvocations.FirstOrDefault(invocation =>
                            invocation.Candidate.OptionNodeId.IsDefined &&
                            invocation.Candidate.OptionNodeId.Value == "reload-borrow-arm");
                        var answer = borrowArm ??
                            (effect.SelectInvocations.Count > 0
                                ? effect.SelectInvocations[0]
                                : effect.ConfirmInvocation);
                        Assert.That(turn.TryAnswerEffectChoice(answer).Accepted, Is.True,
                            Trace() + " | " + effect.NodeId.Value + " refused its answer.");
                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(acting.Id))
                        {
                            trace.Add("hold");
                            Assert.That(turn.TryPerformHold(
                                turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, acting.OwnerId, acting.Id)
                            .Single(value => value.ActionDefinitionId.Equals(
                                SwiftCardContent.PrimaryBasicSkill));
                        var startedSkill = turn.TryStartSkill(
                            new ActionResolutionId("swift-own"), offer);
                        SkillWasAvailable = startedSkill.Accepted;
                        trace.Add("start:accepted=" + startedSkill.Accepted);
                        if (!startedSkill.Accepted)
                        {
                            // Milestone 8.59's boxed-in board reaches here: an exact distance the
                            // board cannot supply is a control the player never presses.
                            Assert.That(boxedIn, Is.True,
                                Trace() + " | Only the boxed-in board may refuse this action.");
                            break;
                        }

                        started = true;
                        continue;
                    }

                    trace.Add("break:ended=" + turn.IsEnded);
                    break;
                }
            }

            /// <summary>Where the acting hero is standing right now, read off the live board.</summary>
            private HexCoordinate ActorStandsAt()
            {
                Assert.That(
                    board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(actorId), out var at),
                    Is.True, "The acting hero is on the board.");
                return at;
            }

            /// <summary>
            /// The spaces exactly <paramref name="steps"/> away from <paramref name="from"/> along one
            /// of p.19's six files that this board can actually end a walk on: the file's spaces all
            /// exist and nobody is standing on the last one. Derived from the board rather than from a
            /// remembered list, because the second offer is asked from wherever the first walk left
            /// her.
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

        /// <summary>
        /// Milestone 8.59. One opening of <c>bounce-line</c>: where she stood, what she was offered,
        /// and what the board admits at the printed distance from there.
        /// </summary>
        internal sealed class LineOffer
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

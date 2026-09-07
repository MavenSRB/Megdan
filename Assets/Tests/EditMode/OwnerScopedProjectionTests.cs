using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30a. What each of the six seats may see.
    /// </summary>
    /// <remarks>
    /// The privacy claim these cases make is structural, and they are written to fail if it ever
    /// stops being: they assert that another seat's cards are <em>absent from the object</em>, not
    /// that some flag on them is set. No case here logs another seat's private fields to prove they
    /// are hidden - it inspects the typed inventory instead, which is the rule the private-online
    /// validation shard states for every privacy check on this track.
    /// </remarks>
    public sealed class OwnerScopedProjectionTests
    {
        [Test]
        public void SixSeatsAreDerivedFromTheParticipantGraphWithOneOwnedHeroEach()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            var participants = host.Projection.Participants;

            var seats = host.Seats.Seats;

            Assert.That(seats.Count, Is.EqualTo(6));
            Assert.That(seats.Select(seat => seat.Id).Distinct().Count(), Is.EqualTo(6));
            Assert.That(seats.Select(seat => seat.HeroId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(host.Seats.ForSide(TeamSide.Red).Count(), Is.EqualTo(3));
            Assert.That(host.Seats.ForSide(TeamSide.Blue).Count(), Is.EqualTo(3));
            foreach (var seat in seats)
            {
                Assert.That(seat.PlayerId,
                    Is.EqualTo(participants.GetPlayerForHero(seat.HeroId).Id),
                    "A seat's player is the one the participant graph gives its hero, never a claim.");
                Assert.That(seat.TeamId, Is.EqualTo(participants.GetTeamForHero(seat.HeroId).Id));
                Assert.That(seat.TeamSide, Is.EqualTo(participants.GetTeamForHero(seat.HeroId).Side));
                Assert.That(host.Seats.Find(seat.Id), Is.SameAs(seat));
                Assert.That(host.Seats.FindByHero(seat.HeroId), Is.SameAs(seat));
            }

            Assert.That(host.Seats.Find(new SeatId("no-such-seat")), Is.Null,
                "An unknown seat is an absence to answer with, not an exception to throw.");
        }

        [Test]
        public void EachSeatSeesItsOwnCardsAndNoOtherSeatsHandOrDeck()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            var everyCard = host.Projection.Cards;

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);

                Assert.That(projection.OwnCards.Select(card => card.Id),
                    Is.EquivalentTo(everyCard.Where(card => card.OwnerId.Equals(seat.HeroId))
                        .Select(card => card.Id)),
                    "A seat holds every one of its own cards, wherever they are.");
                Assert.That(projection.PublicCards.Any(card => card.OwnerId.Equals(seat.HeroId)),
                    Is.False, "Own cards are not repeated as public ones.");
                Assert.That(projection.PublicCards.Where(card =>
                        card.State.Location == CardLocation.Hand ||
                        card.State.Location == CardLocation.Deck),
                    Is.Empty,
                    "Another seat's hand and deck are absent together: a client that could read one " +
                    "opponent deck could subtract it from the printed set and read the hand.");
                Assert.That(projection.HandSummaries.Select(value => value.HeroId),
                    Is.EquivalentTo(host.Seats.Seats.Select(value => value.HeroId)),
                    "Hand and deck sizes are countable at the table, so every hero has a summary.");
                Assert.That(projection.HandSummaries.Sum(value => value.HandCount),
                    Is.EqualTo(everyCard.Count(card => card.State.Location == CardLocation.Hand)));
            }
        }

        [Test]
        public void AttackTargetDefensePreviewsReachOnlyTheSeatChoosingThatTarget()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            DriveThroughSeats(
                host,
                1800,
                projection => projection.PendingAttackTargetChoice != null,
                // Milestone 8.35g re-derived the seed offline over seeds 1-16: the release draws
                // from the same stream even when it is the only legal command, so seed 3 no longer
                // reaches an Attack target inside 3,000 steps at all. Seed 6 reaches one at step
                // 348 - the earliest of the eleven seeds that reach one, and this walk exists to
                // arrive at the decision rather than to be long.
                6);

            var choice = host.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null,
                "The deterministic seat walk must reach an Attack-target decision.");
            var owner = host.Seats.FindByHero(choice.ChooserId);
            var ownerProjection = host.ProjectionForSeat(owner.Id);
            Assert.That(ownerProjection.PendingAttackTargetDefensePreviews.Select(value =>
                    value.HeroId),
                Is.EqualTo(choice.LegalTargetHeroIds));
            foreach (var waiting in host.Seats.Seats.Where(value => !value.Id.Equals(owner.Id)))
            {
                Assert.That(host.ProjectionForSeat(waiting.Id)
                        .PendingAttackTargetDefensePreviews,
                    Is.Empty,
                    "A waiting seat receives no consequence projection for a choice it cannot answer.");
            }
        }

        [Test]
        public void AFacedownCommitmentIsAbsentFromEveryOtherSeatUntilItIsRevealed()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            var harness = new LocalSeatHarness(host);
            var committed = new List<CardInstanceId>();

            // Commit for every seat but the last, so a facedown commitment is standing while other
            // seats still hold live projections to inspect.
            var seats = host.Seats.Seats;
            for (var index = 0; index < seats.Count - 1; index++)
            {
                var asked = host.Projection.Commitments.First(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                var client = harness.SelectByHero(asked.HeroId);
                var command = client.Projection.AuthorizedCommands
                    .First(value => value.Kind == DirectorCommandKind.CommitCard);
                committed.Add(command.CardInstanceId);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("commit-" + index), client.Projection.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            foreach (var seat in seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var othersCommitments = committed
                    .Where(cardId => !projection.OwnCards.Any(card => card.Id.Equals(cardId)))
                    .ToArray();
                Assert.That(othersCommitments, Is.Not.Empty,
                    "Every seat has at least one other seat's facedown commitment to not see.");
                foreach (var cardId in othersCommitments)
                {
                    Assert.That(projection.PublicCards.Any(card => card.Id.Equals(cardId)), Is.False,
                        "A facedown commitment is absent from every seat but its owner's.");
                }

                Assert.That(projection.Commitments.Where(value =>
                        value.Status == TurnCardCommitmentStatus.CardCommittedFacedown)
                    .All(value => !value.RevealedCardInstanceId.HasValue), Is.True,
                    "A committed card has no projected identity before the reveal.");
            }

            // The last commitment reveals them all, and a revealed card is public to everyone.
            var last = host.Projection.Commitments.Single(value =>
                value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            var lastClient = harness.SelectByHero(last.HeroId);
            var lastCommand = lastClient.Projection.AuthorizedCommands
                .First(value => value.Kind == DirectorCommandKind.CommitCard);
            Assert.That(lastClient.Submit(new SeatSubmission(
                new SubmissionId("commit-last"), lastClient.Projection.Version, lastCommand)).Accepted,
                Is.True);

            foreach (var seat in seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var visible = projection.OwnCards.Select(card => card.Id)
                    .Concat(projection.PublicCards.Select(card => card.Id))
                    .ToArray();
                foreach (var cardId in committed)
                {
                    Assert.That(visible, Does.Contain(cardId),
                        "Once the reveal happens, every revealed card is public to every seat.");
                }
            }
        }

        /// <summary>
        /// Milestone 8.30a. The visibility rule is rulebook p.4's, restated here independently of the
        /// production one.
        /// </summary>
        /// <remarks>
        /// <para>p.4: a player may always be asked to read "any of their unresolved cards, resolved
        /// cards, and discarded cards, as well as visible information on their dashboards", and
        /// "everything else is hidden information, including: Cards in the deck. Cards in hand. ...
        /// Any facedown cards."</para>
        /// <para>The facedown half is the one a location list alone gets wrong. p.7 lets a resolved or
        /// discarded card be facedown, and such a card is hidden where it lies - so orientation is
        /// checked first. And orientation alone is not the rule either: a card in hand is face-up to
        /// its owner, and milestone 7.5 made deck cards face-up so p.15's colour/Tier search is
        /// answerable, so both are face-up and both are hidden.</para>
        /// </remarks>
        [Test]
        public void AnotherSeatsCardsFollowTheRulebooksHiddenInformationRule()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            var harness = new LocalSeatHarness(host);

            // One facedown commitment standing, so the hidden half is real rather than hypothetical.
            var asked = host.Projection.Commitments.First(value =>
                value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            var committing = harness.SelectByHero(asked.HeroId);
            Assert.That(committing.Submit(new SeatSubmission(
                    new SubmissionId("facedown"),
                    committing.Projection.Version,
                    committing.Projection.AuthorizedCommands.First(value =>
                        value.Kind == DirectorCommandKind.CommitCard))).Accepted,
                Is.True);

            AssertVisibilityRule(host);

            // Milestone 8.35d. Driven to a state that still carries both hidden shapes rather than
            // to a fixed step count. The host now runs p.8's stages itself, so the walk no longer
            // spends a step per stage and a fixed budget lands elsewhere in the round than it used
            // to - and "sixty commands" was never what this case is about.
            var stepped = 0;
            DriveThroughSeats(host, 600, projection =>
                ++stepped > 60 && CarriesBothHiddenShapes((MatchSessionProjection)projection));

            AssertVisibilityRule(host);
        }

        /// <summary>
        /// Whether the position under test still contains the two shapes this case's claim needs:
        /// a facedown card, and a face-up card in a hand.
        /// </summary>
        private static bool CarriesBothHiddenShapes(MatchSessionProjection projection) =>
            projection.Cards.Any(card =>
                card.State.FaceOrientation == CardFaceOrientation.FaceDown) &&
            projection.Cards.Any(card =>
                card.State.Location == CardLocation.Hand &&
                card.State.FaceOrientation == CardFaceOrientation.FaceUp);

        private static void AssertVisibilityRule(IMatchHost host)
        {
            var open = new[]
            {
                CardLocation.TurnArea,
                CardLocation.DiscardPile,
                CardLocation.ItemSlot,
                CardLocation.UltimateArea
            };

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var visible = projection.PublicCards.Select(card => card.Id.Value).ToArray();
                foreach (var card in host.Projection.Cards.Where(value =>
                    !value.OwnerId.Equals(seat.HeroId)))
                {
                    var readable = card.State.FaceOrientation == CardFaceOrientation.FaceUp &&
                        open.Contains(card.State.Location);
                    Assert.That(visible.Contains(card.Id.Value), Is.EqualTo(readable),
                        "Rulebook p.4 decides " + card.Id.Value + " at " + card.State.Location +
                        " / " + card.State.FaceOrientation + " for seat " + seat.Id + ".");
                }
            }

            var everyCard = host.Projection.Cards;
            Assert.That(everyCard.Any(card =>
                    card.State.FaceOrientation == CardFaceOrientation.FaceDown), Is.True,
                "The state under test must contain a facedown card, or the hidden half is vacuous.");
            Assert.That(everyCard.Any(card => card.State.Location == CardLocation.Hand &&
                    card.State.FaceOrientation == CardFaceOrientation.FaceUp), Is.True,
                "It must also contain a face-up hand card, or the claim that orientation alone is " +
                "not the rule is vacuous.");
        }

        /// <summary>
        /// The line between a player's surface and the Director's. Milestone 8.35f added the
        /// one crossing there has ever been - a seat may ask for the bounded rewind of its own
        /// turn - and this case records the boundary that replaced the old one rather than
        /// dropping the claim: everything unbounded is still absent, and the request the seat
        /// does carry is too small to aim.
        /// </summary>
        [Test]
        public void ThePlayerSurfaceCarriesNoDirectorAuthority()
        {
            var directorOnly = new[]
            {
                "AcceptedCommands",
                "AuthoritativeSnapshot",
                "DiagnosticSnapshot",
                "DiagnosticEvents",
                "IsHistoryAvailable",
                "RoundEvents",
                "Undo",
                "Restart",
                "RestoreSnapshot",
                "CaptureSnapshot",
                "Execute",
                "Submit"
            };

            foreach (var name in directorOnly)
            {
                Assert.That(typeof(SeatProjection).GetMember(name), Is.Empty,
                    "A seat projection must not carry the Director-only member " + name + ".");
            }

            foreach (var name in directorOnly.Where(value => value != "Submit"))
            {
                Assert.That(typeof(ISeatClient).GetMember(name), Is.Empty,
                    "A seat client must not carry the Director-only member " + name + ".");
            }

            Assert.That(typeof(SeatProjection)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.PropertyType == typeof(MatchSessionProjection)),
                Is.Empty,
                "Holding the full-trust read model would make every absence above cosmetic.");
            Assert.That(typeof(ISeatClient).GetMethods().Select(method => method.Name)
                    .Where(name => !name.StartsWith("get_", StringComparison.Ordinal)),
                Is.EquivalentTo(new[] { "Submit", "SubmitAgreement", "SubmitUndo" }),
                "A player's authority is limited to rules commands, bounded team statements, and " +
                "the bounded rewind of its own turn.");

            // Milestone 8.35f moved this boundary exactly once, and this is where it now stands.
            // A seat may ask to step its own turn back; the Director-only absences above are
            // unchanged, so its unbounded Undo, its Restart and its snapshots stay unreachable.
            // What keeps the two apart is that the seat's request has no room to say how far,
            // whose turn, or from which state: it names an identity and the version it was decided
            // against, and the host answers all three questions itself.
            Assert.That(typeof(SeatUndoSubmission)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(property => property.Name),
                Is.EquivalentTo(new[] { "Id", "ExpectedVersion" }),
                "A seat's rewind carries nothing it could aim: no seat, no count, no target.");
        }

        [Test]
        public void SnapshotRestoreAndSessionReplacementRebuildEverySeatIdentically()
        {
            var initialState = CreateOfficialInitialState();
            var played = MatchHost.Compose(initialState);
            DriveThroughSeats(played, 24);
            var payload = played.CaptureSnapshot();
            var before = played.Seats.Seats
                .ToDictionary(seat => seat.Id.Value, seat => Describe(played.ProjectionForSeat(seat.Id)));

            // A replacement session: a second host over the same initial state, restored from the
            // first one's snapshot. Nothing carries over but the payload.
            var replacement = MatchHost.Compose(initialState);
            Assert.That(replacement.Submit(MatchSubmission.ForSnapshotRestore(payload)).Accepted,
                Is.True);

            Assert.That(replacement.Seats.Seats.Select(seat => seat.Id.Value),
                Is.EqualTo(played.Seats.Seats.Select(seat => seat.Id.Value)),
                "Seats are reseated from the restored authority, not carried across it.");
            foreach (var seat in replacement.Seats.Seats)
            {
                Assert.That(Describe(replacement.ProjectionForSeat(seat.Id)),
                    Is.EqualTo(before[seat.Id.Value]),
                    "Seat " + seat.Id + " must rebuild exactly, including which cards it cannot see.");
            }

            var restoredInPlace = played.Submit(MatchSubmission.ForSnapshotRestore(payload));
            Assert.That(restoredInPlace.Accepted, Is.True);
            foreach (var seat in played.Seats.Seats)
            {
                Assert.That(Describe(played.ProjectionForSeat(seat.Id)),
                    Is.EqualTo(before[seat.Id.Value]));
            }
        }

        [Test]
        public void TheDevelopmentHarnessSelectsEverySeatAndEachClientIsFixedToItsOwn()
        {
            var host = MatchHost.Compose(CreateOfficialInitialState());
            var harness = new LocalSeatHarness(host);

            var clients = harness.SelectAll();

            Assert.That(clients.Count, Is.EqualTo(6));
            Assert.That(clients.Select(client => client.Seat.Id.Value),
                Is.EqualTo(host.Seats.Seats.Select(seat => seat.Id.Value)));
            foreach (var client in clients)
            {
                Assert.That(client.Projection.Seat, Is.SameAs(client.Seat));
                Assert.That(client.Projection.OwnCards.All(card =>
                    card.OwnerId.Equals(client.Seat.HeroId)), Is.True);
            }

            Assert.That(harness.SelectSide(TeamSide.Blue).Select(client => client.Seat.TeamSide),
                Is.All.EqualTo(TeamSide.Blue));
            Assert.That(typeof(ISeatClient).GetProperty(nameof(ISeatClient.Seat)).CanWrite, Is.False,
                "A development seat switcher chooses which seat to look through and can never " +
                "change what a seat owns.");
        }

        /// <summary>
        /// One seat's whole visible content as a comparable string. Card identities appear only as
        /// the seat is already entitled to see them, so a rebuild that leaked would change this
        /// string rather than pass unnoticed.
        /// </summary>
        internal static string Describe(SeatProjection projection)
        {
            var parts = new List<string>
            {
                "seat=" + projection.Seat.Id,
                "hero=" + projection.Seat.HeroId.Value,
                "team=" + projection.Seat.TeamSide,
                "round=" + projection.RoundIndex + "/" + projection.TurnIndex,
                "phase=" + projection.Phase,
                "own=" + string.Join(",", projection.OwnCards
                    .Select(card => card.Id.Value + ":" + card.State.Location)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "public=" + string.Join(",", projection.PublicCards
                    .Select(card => card.Id.Value + ":" + card.State.Location)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "hands=" + string.Join(",", projection.HandSummaries
                    .Select(value => value.HeroId.Value + ":" + value.HandCount + "/" + value.DeckCount)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "commitments=" + string.Join(",", projection.Commitments
                    .Select(value => value.HeroId.Value + ":" + value.Status + ":" +
                        (value.RevealedCardInstanceId?.Value ?? "-"))
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "owner=" + (projection.DecisionOwner.IsDefined
                    ? projection.DecisionOwner.FamilyId.Value + ":" +
                        projection.DecisionOwner.HeroId.Value + ":" + projection.DecisionOwner.TeamSide
                    : "-"),
                "authorized=" + string.Join(",", projection.AuthorizedCommands
                    .Select(command => command.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal))
            };
            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Plays a bounded prefix of a real match through the seats themselves, so that whatever the
        /// other cases inspect was produced the way a client would produce it.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35d added <paramref name="seed"/>. The walk's own draw sequence is what
        /// decides which pending families a bounded prefix reaches, and removing the seat-pressed
        /// end-of-round advance removed one draw per p.8 stage - so seed 2 now reaches the Attack
        /// target and the Minion Battle around step 2,540 rather than inside 1,500. A coverage
        /// fixture names the seed that reaches what it is about instead of paying for a walk four
        /// times longer; every caller that does not care keeps seed 2 and its old trajectory
        /// wherever the round boundary has not moved it.
        /// </remarks>
        internal static int DriveThroughSeats(
            IMatchHost host,
            int commandBudget,
            Func<IMatchReadModel, bool> stopBeforeCommand = null,
            int seed = 2)
        {
            var random = new MatchLoopRandom(seed);
            var submitted = 0;
            while (submitted < commandBudget)
            {
                if (stopBeforeCommand?.Invoke(host.Projection) == true)
                {
                    break;
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(host.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                    host, command, "drive-agreement-" + host.Sequence + "-" + submitted))
                {
                    submitted++;
                    continue;
                }

                var seat = host.Seats.Seats.FirstOrDefault(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                Assert.That(seat, Is.Not.Null,
                    "Every command the Director could take belongs to some seat: " + command);
                var client = host.ClientForSeat(seat.Id);

                // Named from the host's own stream position rather than this call's counter. A caller
                // that drives the same host twice would otherwise reuse `drive-0`, and the host would
                // refuse it as the duplicate it genuinely is - the idempotency window doing its job
                // to a fixture that did not mean to test it.
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("drive-" + host.Sequence + "-" + submitted),
                    client.Projection.Version,
                    command));
                Assert.That(result.Accepted, Is.True, result.Detail);
                submitted++;
            }

            return submitted;
        }

        internal static MatchSessionInitialState CreateOfficialInitialState()
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }

        /// <summary>
        /// 8.35z54. Composes the match a CHOSEN line-up plays, exactly as
        /// <c>MatchSceneCompositionRoot.ComposeForLineup</c> does - the scene half of milestone
        /// 8.35z37's recomposition seam, without a scene.
        /// </summary>
        /// <remarks>
        /// A fixture that hands the session a composer which returns null is not testing the seam:
        /// null means "leave the session on the match it has", which is the answer that made a
        /// chosen line-up indistinguishable from the printed one. Every lobby-flow case composes a
        /// real second match, so the projections the seats then hold are the projections of a
        /// DIFFERENT board and any confusion between the two is visible.
        /// </remarks>
        internal static IMatchHost ComposeForLineup(HeroLineup lineup)
        {
            var format = MatchFormat.All.Single(
                candidate => candidate.HeroesPerTeam == lineup.HeroesPerTeam &&
                    candidate.Id.IndexOf("quick", StringComparison.OrdinalIgnoreCase) >= 0);
            var printed = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var configuration = MatchConfiguration.CreateForLineupFromRecord(
                format, lineup, printed.TieBreakerState, SetupRandomnessSeed.Unrecorded);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return MatchHost.Compose(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}

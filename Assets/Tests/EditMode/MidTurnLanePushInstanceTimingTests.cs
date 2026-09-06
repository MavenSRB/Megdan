using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z21. Rulebook p.8's mid-turn Lane Push fires at the end of the action that caused
    /// it, and rulebook p.10 makes each repeat instance a separate action - so a push owed by the
    /// first instance resolves <em>before</em> the second instance begins.
    ///
    /// <para><b>The ruling.</b> Designer Arty, 2026-06-18, asked what happens when an attack that also
    /// carries an "after the attack" effect removes the last enemy minion in the Battle Zone: the push
    /// happens <em>"At the end of the action. However, repeat instances are separate actions."</em>
    /// The first half was already true of this build - the six Attack completion points all gate on
    /// <c>TheAttackIsOver</c>, so p.14 step 7's text runs first. The second half was not. Every repeat
    /// family started its next instance on the far side of the single drain site in
    /// <c>TurnResolutionAuthority.AdvanceInitiative</c>, so one push fired after every instance and the
    /// second instance chose its target on a board the rules say had already advanced - a different
    /// Battle Zone, the previous zone's minions still standing, no Wave counter spent.</para>
    ///
    /// <para><b>Why Violent Torrent is the subject.</b> Arien's Tier III Red face prints "Target a unit
    /// adjacent to you. Before the attack: ... May repeat once on a different unit", so its repeat is
    /// elected at p.14 step 1 - before the attack that empties the zone - and the second instance's own
    /// target question is therefore asked strictly after the first instance ended. The push it owes and
    /// the instance it owes are guaranteed to meet, which no synthetic fixture had to be built to
    /// arrange.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public class MidTurnLanePushInstanceTimingTests
    {
        // -----------------------------------------------------------------------------------------
        // The ordering itself
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// THE REGRESSION. Before this milestone the checkpoints below landed after the second
        /// instance's whole resolution instead of between the two.
        /// </summary>
        [Test]
        public void APushOwedByTheFirstInstance_ResolvesBeforeTheRepeatAsksForItsTarget()
        {
            var board = StageArienOverTheLastEnemyMinion();
            DriveTheFirstInstanceOntoTheLastEnemyMinion(board);

            var repeatQuestion = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(repeatQuestion, Is.Not.Null,
                "p.10's second instance is under way and asking for its own target. " + Trace(board));

            Assert.That(
                Sequence(board, "AttackResolutionCompleted"),
                Is.LessThan(Sequence(board, "ImmediateLanePushRequested")),
                "The half that already conformed: p.8's push does not run until the action that " +
                "caused it has reached p.14's end. " + Trace(board));
            Assert.That(
                Sequence(board, "ImmediateLanePushResolved"),
                Is.LessThan(Sequence(board, "AttackTargetChoiceRequested")),
                "The half this milestone adds, and the designer's ruling stated as an ordering: the " +
                "push finishes before the separate action that follows it opens its question. " +
                Trace(board));
            Assert.That(board.Session.Projection.PendingImmediateLanePush, Is.Null,
                "Nothing is still held when the next instance starts, which is what makes a push " +
                "the second instance owes recordable rather than silently dropped.");
        }

        /// <summary>
        /// The composition with milestone 8.35z15. That milestone judges a repeat against "the board
        /// as it then stands"; after this one, "then" is after the push, and the board it stands on is
        /// measurably the pushed one.
        /// </summary>
        [Test]
        public void TheRepeatsTargetQuestionIsAskedOnThePushedBoard()
        {
            var board = StageArienOverTheLastEnemyMinion();
            Assert.That(board.MinionAt(StagedHeroBoard.ThirdAdjacent).Value, Is.EqualTo(BlueHolder),
                "Arien's own minion holds the Battle Zone beside him, which is both what makes his " +
                "side win the push and a piece the push then moves out of his reach.");

            DriveTheFirstInstanceOntoTheLastEnemyMinion(board);

            var repeatQuestion = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(repeatQuestion, Is.Not.Null, Trace(board));

            Assert.That(
                board.Session.Projection.Minions.Any(minion =>
                    minion.MinionId.Value == BlueHolder &&
                    minion.Coordinate.Equals(StagedHeroBoard.ThirdAdjacent)),
                Is.False,
                "p.8 step 3 removed the previous Battle Zone's minions and respawned them in the new " +
                "one, so the holder has left the space it occupied when the first instance ended. " +
                "This is the board the question below was asked against.");
            Assert.That(
                board.Session.Projection.Minions.Any(minion => minion.MinionId.Value == RedTarget),
                Is.True,
                "and the minion the first instance defeated is back on the board, respawned by the " +
                "same transition - a piece that did not exist between the Defeat and the push.");

            // The two halves are asserted against each other rather than one of them alone, because
            // "no minion is offered" is only evidence when the board is the reason. Both statements
            // are about the PUSHED board: before the transition ran, two minions stood beside him.
            var adjacentToArien = StagedHeroBoard.HeroAt.GetNeighbors().ToArray();
            var minionsBesideArien = board.Session.Projection.Minions
                .Where(minion => adjacentToArien.Contains(minion.Coordinate))
                .Select(minion => minion.MinionId.Value)
                .ToArray();
            Assert.That(minionsBesideArien, Is.Empty,
                "p.8's transition moved every minion out of Arien's neighbourhood - the holder into " +
                "the new Battle Zone and the respawned target onto a Spawn point in it - so the board " +
                "the second instance is asked about has no minion beside him. Standing beside him: " +
                Describe(minionsBesideArien));
            Assert.That(repeatQuestion.LegalTargetMinionIds, Is.Empty,
                "and the question offers exactly that: no minion, because none is there. Offered: " +
                Describe(repeatQuestion));
            Assert.That(repeatQuestion.LegalTargetHeroIds, Is.EqualTo(new[] { board.Enemy }),
                "The enemy hero is what is left, because p.8's transition moves minions and not " +
                "heroes - which is what keeps this case from being an empty offer proving nothing. " +
                "Offered: " + Describe(repeatQuestion));
        }

        // -----------------------------------------------------------------------------------------
        // The push that stops inside the action
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The state this milestone creates and no earlier build could reach: p.8's sidebar owes a
        /// Place, so the push stops in the middle of a card resolution, with the first instance over,
        /// the second decided and not started, the card unresolved and its actor still selected.
        ///
        /// <para>It is also why schema v37 exists. Every command captures a pre-image and a refused
        /// one is rolled back by deserializing it, so the owed repeat standing here has to cross the
        /// payload or it disappears at the next refusal - not merely after a reload.</para>
        /// </summary>
        [Test]
        public void APushStoppedInsideTheActionHoldsTheOwedRepeat_AndSurvivesASnapshotByteForByte()
        {
            var board = StageArienWithTheNewZonesSpawnPointsBlocked();
            DriveTheFirstInstanceOntoTheLastEnemyMinion(board);

            var pending = board.Session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null,
                "An obstacle stands on a Spawn point of the new Battle Zone, so p.8's sidebar owes " +
                "its owning team a Place and the push stops on it. " + Trace(board));
            Assert.That(pending.StepId, Is.EqualTo(LanePushSystemTransition.RespawnStepId));

            var held = board.Session.Projection.PendingImmediateLanePush;
            Assert.That(held, Is.Not.Null,
                "The request stands until the push it began has finished, exactly as it did when the " +
                "drain lived at the end of the card.");
            Assert.That(held.RequestId.Value, Does.Contain("primary-attack-lane-push"),
                "and it is named by the instance that caused it - the printed primary, not the " +
                "repeat - which is what keeps two instances' pushes apart. Actual: " +
                held.RequestId.Value);
            Assert.That(held.RequestId.Value, Does.Not.Contain(":repeat"));
            Assert.That(board.Session.Projection.PendingAttackTargetChoice, Is.Null,
                "The second instance has not started: p.10's separate action begins after the push " +
                "its predecessor owed, not beside it.");

            var refused = board.Session.Execute(DirectorCommand.ReleaseTurn());
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.Detail, Does.Contain(
                TurnResolutionRejectionReason.IncompleteLanePushTransition.ToString()),
                "p.8's turn continues 'as normal' only once the push is done, and that gate is " +
                "unchanged by the drain moving: it reads a held request beside a closed action.");

            var payload = board.Session.CaptureSnapshot();
            var restored = new MatchSession(board.InitialState);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "The mid-card pushed state round-trips rather than being approximated.");
            Assert.That(
                restored.Projection.PendingSystemTransitionStep.ItemId, Is.EqualTo(pending.ItemId));

            // The byte comparison above is necessary and NOT sufficient, and saying so is the point of
            // this milestone's schema move: a v36 payload writes nothing here either, so it would also
            // round-trip byte-identically and would also be one instance short. Only driving the
            // restored match past the Place can tell the two apart.
            AnswerEveryStandingPlace(restored);
            Assert.That(restored.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "The restored match finished the push and went on to start p.10's second instance, " +
                "so the decided-and-unstarted repeat genuinely crossed the payload. On schema v36 " +
                "this is where the instance quietly disappeared and the card finished a step early.");
        }

        /// <summary>
        /// The other half of the same state: answering the Place finishes the push and the action then
        /// carries on from where the push interrupted it, rather than from Initiative.
        /// </summary>
        [Test]
        public void AnsweringThePlace_ResumesTheActionAndStartsTheRepeatOnThePushedBoard()
        {
            var board = StageArienWithTheNewZonesSpawnPointsBlocked();
            DriveTheFirstInstanceOntoTheLastEnemyMinion(board);
            AnswerEveryStandingPlace(board);

            Assert.That(board.Session.Projection.PendingImmediateLanePush, Is.Null,
                "The push finished.");
            Assert.That(board.Session.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "and the action it stopped in the middle of carried on into p.10's second instance. " +
                "Before this milestone a finished mid-turn push always handed the turn back to " +
                "Initiative, because it could only ever have run at the end of a card. " + Trace(board));
            Assert.That(
                Sequence(board, "ImmediateLanePushResolved"),
                Is.LessThan(Sequence(board, "AttackTargetChoiceRequested")),
                "and the ordering survives the pause: the push is finished before the separate " +
                "action that follows it opens its question. " + Trace(board));
        }

        // -----------------------------------------------------------------------------------------
        // The winner, read at the end of the action
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// p.8 states the winner by looking - "The team with one or more minions still remaining in
        /// the Battle Zone wins the push" - and the ruling puts the push at the end of the action, so
        /// the look happens there. p.14 step 7's "After the attack:" text runs in between and can
        /// move, place or remove minions in the zone, so the recorded answer and the end-of-action
        /// answer genuinely differ.
        /// </summary>
        [Test]
        public void AWinnerTheRestOfTheActionChanged_IsTheOneThePushUses()
        {
            var fixture = CreateTwoZoneAttackFixture(
                Minion("blue-last", TeamSide.Blue, InsideTheZone),
                Minion("red-holder", TeamSide.Red, SecondInsideTheZone),
                Minion("blue-outside", TeamSide.Blue, OutsideTheZone));

            var recorded = RecordACauseByDefeating(fixture, "blue-last");
            Assert.That(recorded.WinningSide, Is.EqualTo(TeamSide.Red),
                "At the moment the cause was recorded, Red was the side still holding the zone.");

            // The rest of the action moves Red's holder out and walks Blue's outsider in. Neither is
            // a p.8 cause - "A Lane Push can never be triggered by moving, pushing, or swapping a
            // minion" - but both change the answer to p.8's winner sentence.
            Displace(fixture, "red-holder", SecondOutsideTheZone);
            Displace(fixture, "blue-outside", InsideTheZone);

            var atTheEnd = fixture.Authority.DeriveImmediateLanePushAtEndOfAction(
                fixture.Configuration.Participants, RuleModificationSet.Empty);

            Assert.That(atTheEnd, Is.Not.Null);
            Assert.That(atTheEnd.WinningSide, Is.EqualTo(TeamSide.Blue),
                "p.8 awards the push to the team still holding the zone, and at the end of the " +
                "action that is Blue. Replaying the recorded Red would award it by a fact that " +
                "stopped being true inside the same action.");
            Assert.That(atTheEnd.Cause, Is.EqualTo(recorded.Cause),
                "The cause is not re-derived and could not be: nothing about an emptied zone " +
                "distinguishes p.8's two sentences from each other, which is why the operation " +
                "supplies it and the board supplies the rest.");
            Assert.That(atTheEnd.RequestId, Is.EqualTo(recorded.RequestId),
                "The identity is minted from the action instance either way, so a replay of the " +
                "same accepted commands still names the same push.");
        }

        /// <summary>
        /// The reading recorded with this milestone for the case the rulebook does not state: when the
        /// end of the action names no winner at all, the recorded answer stands.
        /// </summary>
        [Test]
        public void AnEndOfActionThatNamesNoWinner_LeavesTheRecordedAnswerStanding()
        {
            var fixture = CreateTwoZoneAttackFixture(
                Minion("blue-last", TeamSide.Blue, InsideTheZone),
                Minion("red-holder", TeamSide.Red, SecondInsideTheZone));

            var recorded = RecordACauseByDefeating(fixture, "blue-last");
            Assert.That(recorded.WinningSide, Is.EqualTo(TeamSide.Red));

            Displace(fixture, "red-holder", OutsideTheZone);

            Assert.That(
                fixture.Authority.DeriveImmediateLanePushAtEndOfAction(
                    fixture.Configuration.Participants, RuleModificationSet.Empty),
                Is.Null,
                "With neither side holding a minion in the zone, p.8's winner sentence has no " +
                "subject, and the query reports nothing rather than awarding the push to nobody - " +
                "the same answer it gives when a cause is recorded on such a board in the first " +
                "place. The drain reads that as 'no better answer than the recorded one', because " +
                "p.8 says nothing about a push whose subject has gone and dropping it would invent " +
                "an outcome the rulebook does not print.");
        }

        // -----------------------------------------------------------------------------------------
        // The driven fixture
        // -----------------------------------------------------------------------------------------

        private const string RedTarget = "red-melee-01";
        private const string BlueHolder = "blue-melee-01";

        private static readonly string[] AllMinions =
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };

        /// <summary>
        /// Arien at Tier III with Violent Torrent, the one enemy minion left in the Battle Zone
        /// adjacent to him, one of his own minions adjacent and holding the zone for his side, and the
        /// enemy hero adjacent as the different unit the second instance can name.
        /// </summary>
        private static StagedHeroBoard StageArienOverTheLastEnemyMinion() =>
            ArienBoard.Create(
                tier: 3,
                minionPlacements: StagedHeroBoard.At(
                    RedTarget, StagedHeroBoard.FirstAdjacent,
                    BlueHolder, StagedHeroBoard.ThirdAdjacent),
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.SecondAdjacent),
                banishedMinions: AllMinions
                    .Where(minion => minion != RedTarget && minion != BlueHolder)
                    .ToArray());

        /// <summary>
        /// The same engagement with Arien's whole side left standing, so p.8's respawn has enough
        /// minions to reach the blocked Spawn points and stop on the sidebar's Place.
        /// </summary>
        private static StagedHeroBoard StageArienWithTheNewZonesSpawnPointsBlocked() =>
            ArienBoard.Create(
                tier: 3,
                minionPlacements: StagedHeroBoard.At(
                    RedTarget, StagedHeroBoard.FirstAdjacent,
                    BlueHolder, StagedHeroBoard.ThirdAdjacent),
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.SecondAdjacent),
                banishedMinions: AllMinions
                    .Where(minion =>
                        minion.StartsWith("red-", StringComparison.Ordinal) && minion != RedTarget)
                    .ToArray(),
                obstacles: MinionSpawnPointsOf(TheZoneArienWinsInto));

        private static void DriveTheFirstInstanceOntoTheLastEnemyMinion(StagedHeroBoard board)
        {
            board.StartPrimary(ArienCardContent.ViolentTorrent);

            // p.15 has taken him to Tier III, so p.16's Ultimate is out of the deck and Living
            // Tsunami's before-action trigger intercepts the start. Declined, exactly as the other
            // Tier III fixtures decline it, because it belongs to the staging rather than to the
            // ordering under test.
            Assert.That(board.Choice, Is.Not.Null, "Tier III opens the Ultimate's trigger first.");
            board.Decline();

            var target = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, "Violent Torrent opens p.14's target question.");
            board.Accept(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, board.MinionAt(StagedHeroBoard.FirstAdjacent)));

            // "May repeat once on a different unit" prints at p.14 step 1, so the second instance is
            // elected here - before the attack that empties the zone, which is what guarantees the
            // owed repeat and the owed push meet.
            Assert.That(board.Choice, Is.Not.Null,
                "Violent Torrent's before-the-attack text offers p.10's repeat.");
            board.Confirm();
        }

        private static void AnswerEveryStandingPlace(StagedHeroBoard board) =>
            AnswerEveryStandingPlace(board.Session);

        private static void AnswerEveryStandingPlace(MatchSession session)
        {
            for (var step = 0; step < 12; step++)
            {
                var pending = session.Projection.PendingSystemTransitionStep;
                if (pending == null)
                {
                    return;
                }

                var answered = session.Execute(
                    DirectorCommand.AnswerSystemTransitionStep(pending.Invocations[0]));
                Assert.That(answered.Accepted, Is.True, answered.Detail);
            }

            Assert.Fail("p.8's sidebar asked more Places than this board has minions to respawn.");
        }

        private static readonly ZoneId TheZoneArienWinsInto = new ZoneId("red-approach");

        /// <summary>
        /// One Red-owned and one Blue-owned minion Spawn point of the named zone, so p.8's sidebar has
        /// something to ask about however the respawn assigns the two sides.
        /// </summary>
        private static IReadOnlyCollection<HexCoordinate> MinionSpawnPointsOf(ZoneId zone)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var candidates = map.SpawnPoints
                .Where(spawn => spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal))
                .Select(spawn => new
                {
                    Spawn = spawn,
                    Space = map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId))
                })
                .Where(value => value.Space.ZoneId.HasValue && value.Space.ZoneId.Value.Equals(zone))
                .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                .ToArray();
            return new[] { "red", "blue" }
                .Select(teamId => candidates.First(value =>
                    string.Equals(value.Spawn.TeamId, teamId, StringComparison.Ordinal)))
                .Select(value => value.Space.Coordinate)
                .ToArray();
        }

        private static int Sequence(StagedHeroBoard board, string eventTypeFragment)
        {
            var sequence = board.Session.Projection.DiagnosticEvents
                .Where(entry => entry.EventType.Contains(eventTypeFragment))
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            Assert.That(sequence, Is.GreaterThan(0),
                $"No rule event of kind '{eventTypeFragment}' was logged. " + Trace(board));
            return sequence;
        }

        private static string Trace(StagedHeroBoard board) =>
            "Rule events, oldest first: " + string.Join(", ",
                board.Session.Projection.DiagnosticEvents
                    .Select(entry => entry.Sequence + ":" + entry.EventType));

        private static string Describe(IEnumerable<string> values)
        {
            var text = string.Join("+", values);
            return string.IsNullOrEmpty(text) ? "nothing" : text;
        }

        private static string Describe(PendingAttackTargetChoice choice) =>
            string.Join("+", choice.LegalTargetMinionIds.Select(minion => "minion:" + minion.Value)
                .Concat(choice.LegalTargetHeroIds.Select(hero => "hero:" + hero.Value)))
                .DefaultIfEmptyText("nothing");

        // -----------------------------------------------------------------------------------------
        // The winner fixture: a small map whose Battle Zone is not the whole board
        // -----------------------------------------------------------------------------------------

        private static readonly HexCoordinate InsideTheZone = new HexCoordinate(1, 0);
        private static readonly HexCoordinate SecondInsideTheZone = new HexCoordinate(-1, 0);
        private static readonly HexCoordinate OutsideTheZone = new HexCoordinate(3, 0);
        private static readonly HexCoordinate SecondOutsideTheZone = new HexCoordinate(0, 3);

        private static readonly HeroId RedHero =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HeroId BlueHero =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private sealed class TwoZoneAttackFixture
        {
            public TwoZoneAttackFixture(
                MatchConfiguration configuration,
                AdjacentMoveAuthority board,
                CardActionResolutionAuthority authority,
                ActionOffer offer)
            {
                Configuration = configuration;
                Board = board;
                Authority = authority;
                Offer = offer;
            }

            public MatchConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer Offer { get; }
        }

        private static PendingImmediateLanePushRequest RecordACauseByDefeating(
            TwoZoneAttackFixture fixture, string minionId)
        {
            var opened = fixture.Authority.StartAttackResolution(
                new ActionResolutionId($"end-of-action-winner-{minionId}"),
                fixture.Offer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            Assert.That(opened.Accepted, Is.True);
            var resolved = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId,
                    new MinionId(minionId)),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            Assert.That(resolved.Accepted, Is.True);
            Assert.That(resolved.MinionDefeat, Is.Not.Null);

            var recorded = fixture.Authority.PendingImmediateLanePush;
            Assert.That(recorded, Is.Not.Null,
                "The Defeat took the last minion of its side out of the Battle Zone, so p.8's " +
                "sentence is met and a request is recorded.");
            return recorded;
        }

        private static void Displace(
            TwoZoneAttackFixture fixture, string minionId, HexCoordinate destination) =>
            Assert.That(
                fixture.Board.CommitValidatedDisplacement(
                    BoardObjectReference.ForMinion(new MinionId(minionId)), destination).Accepted,
                Is.True,
                $"'{minionId}' could not be moved to {destination}.");

        private static QuickGameMinionSetupState Minion(
            string id, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(side == TeamSide.Red
                    ? StagedBoardParticipants.RedTeamIdValue
                    : StagedBoardParticipants.BlueTeamIdValue),
                side,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(
                    new MapId("two-zone-lane-push-map"), new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(
                    new MapId("two-zone-lane-push-map"), new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static TwoZoneAttackFixture CreateTwoZoneAttackFixture(
            params QuickGameMinionSetupState[] minions)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var board = new AdjacentMoveAuthority(
                CreateTwoZoneMap(),
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(-3, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    RedHero,
                    ActionOfferWindow.NormalPlay)
                .Single(value => value.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new TwoZoneAttackFixture(
                configuration,
                board,
                new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board),
                offer);
        }

        /// <summary>
        /// <c>ImmediateLanePushTests</c>' flagged map with an outskirt added. That one flags every
        /// space it has as the Battle Zone, which is exactly right for the cases it owns and leaves
        /// nowhere for a minion to stand outside the zone - the one thing a case about the winner
        /// changing inside an action needs.
        /// </summary>
        private static BoardMapDefinition CreateTwoZoneMap()
        {
            var battle = new BoardMapZone(
                new ZoneId("battle-zone"), "Battle Zone", "lane", null, false, true);
            var outskirt = new BoardMapZone(
                new ZoneId("outskirt"), "Outskirt", "lane", null, false, false);
            var spaces = RadiusThree().Select(coordinate =>
                new BoardMapSpace(
                    new SpaceId($"q{coordinate.Q}_r{coordinate.R}"),
                    coordinate,
                    BoardMapSpaceKind.Playable,
                    Math.Abs(coordinate.Q) + Math.Abs(coordinate.R) +
                        Math.Abs(coordinate.Q + coordinate.R) >= 6
                        ? outskirt.Id
                        : battle.Id));
            return new BoardMapDefinition(
                new MapId("two-zone-lane-push-map"), 1, "Two Zone Lane Push Map", spaces,
                new[] { battle, outskirt },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static IEnumerable<HexCoordinate> RadiusThree()
        {
            for (var q = -3; q <= 3; q++)
            {
                for (var r = Math.Max(-3, -q - 3); r <= Math.Min(3, -q + 3); r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }
    }

    internal static class MidTurnLanePushInstanceTimingTextExtensions
    {
        internal static string DefaultIfEmptyText(this string value, string fallback) =>
            string.IsNullOrEmpty(value) ? fallback : value;
    }
}

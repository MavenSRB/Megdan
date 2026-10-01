using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21a: the many-bodies model (Razzle, RZ-Q1-RZ-Q4), proven on staged bodies of an
    /// existing hero. No Razzle content is registered; the Blue hero of the configured mirror is
    /// given a supply of four bodies (RZ-Q1) and a second body through the internal-test scenario seam,
    /// the way the staged boards place any other piece.
    /// </summary>
    public sealed class HeroBodiesTests
    {
        // -------------------------------------------------------------------------------------------
        // The board authority: identity, supply, guards, reads and commits.
        // -------------------------------------------------------------------------------------------

        private static readonly HeroId Razzle = new HeroId("hero-staged-bodies");
        private static readonly HeroId Other = new HeroId("hero-single-body");

        private static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes, IEnumerable<HeroBodySupply> supplies = null)
        {
            var coordinates = Enumerable.Range(-3, 7)
                .SelectMany(q => Enumerable.Range(-3, 7).Select(r => new HexCoordinate(q, r)))
                .ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("hero-bodies"), 1, coordinates),
                heroes,
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>(),
                supplies ?? new[] { new HeroBodySupply(Razzle, 4) });
        }

        [Test]
        public void ASingleBodiedHero_KeepsBodyOneAndItsOneSpaceRule()
        {
            var occupancy = new HeroOccupancy(Other, new HexCoordinate(0, 0));
            Assert.That(occupancy.Body, Is.EqualTo(HeroOccupancy.FirstBody));
            Assert.That(occupancy, Is.EqualTo(new HeroOccupancy(Other, 1, new HexCoordinate(0, 0))));
            Assert.That(occupancy, Is.Not.EqualTo(new HeroOccupancy(Other, 2, new HexCoordinate(0, 0))));

            Assert.That(() => Board(new[]
                {
                    occupancy,
                    new HeroOccupancy(Other, 2, new HexCoordinate(1, 0))
                }),
                Throws.ArgumentException.With.Message.Contains("unique hero IDs"),
                "A hero that declares no bodies still stands on one space.");
            Assert.That(() => Board(new[] { new HeroOccupancy(Other, 2, new HexCoordinate(1, 0)) }),
                Throws.ArgumentException, "Body 2 of a hero whose supply is one does not exist.");
        }

        [Test]
        public void AHeroThatDeclaresBodies_StandsOncePerBodyUpToItsSupply()
        {
            var board = Board(new[]
            {
                new HeroOccupancy(Razzle, 2, new HexCoordinate(2, 0)),
                new HeroOccupancy(Razzle, 1, new HexCoordinate(0, 0)),
                new HeroOccupancy(Other, new HexCoordinate(1, 0))
            });

            Assert.That(board.HeroBodySupplyOf(Razzle), Is.EqualTo(4));
            Assert.That(board.HeroBodySupplyOf(Other), Is.EqualTo(1));
            Assert.That(board.GetHeroBodies(Razzle).Select(body => body.Body), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(board.IsHeroOnBoard(Razzle), Is.True);
            Assert.That(board.TryGetHeroBodyCoordinate(Razzle, 2, out var bodyTwo), Is.True);
            Assert.That(bodyTwo, Is.EqualTo(new HexCoordinate(2, 0)));

            // RZ-Q2: presence and obstacles per body - the second body occupies its space.
            Assert.That(board.IsEmptyPlayableSpace(new HexCoordinate(2, 0)), Is.False);
            Assert.That(board.TryGetObjectAt(new HexCoordinate(2, 0), out var standing), Is.True);
            Assert.That(standing, Is.EqualTo(BoardObjectReference.ForHero(Razzle)),
                "Every body is the same hero (RZ-Q2, RZ-Q5's hero-grain equality).");

            Assert.That(() => Board(new[]
                {
                    new HeroOccupancy(Razzle, 1, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Razzle, 1, new HexCoordinate(2, 0))
                }),
                Throws.ArgumentException, "One body cannot stand twice.");
            Assert.That(() => Board(new[] { new HeroOccupancy(Razzle, 5, new HexCoordinate(0, 0)) }),
                Throws.ArgumentException.With.Message.Contains("supply is 4"));
        }

        [Test]
        public void TheHeroCoordinateRead_AnswersTheActingBody_AndRefusesWhenNoneIsNamed()
        {
            var board = Board(new[]
            {
                new HeroOccupancy(Razzle, 2, new HexCoordinate(2, 0)),
                new HeroOccupancy(Razzle, 1, new HexCoordinate(0, 0))
            });
            Assert.That(board.TryGetHeroCoordinate(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(new HexCoordinate(0, 0)),
                "With no acting body settled (another hero's turn), the read answers body 1 (RZ-Q4's seam).");

            var withoutBodyOne = Board(new[]
            {
                new HeroOccupancy(Razzle, 2, new HexCoordinate(2, 0)),
                new HeroOccupancy(Razzle, 3, new HexCoordinate(-2, 0))
            });
            Assert.That(() => withoutBodyOne.TryGetHeroCoordinate(Razzle, out _),
                Throws.InvalidOperationException.With.Message.Contains("acting body"),
                "Never silently the first listed body.");
            Assert.That(withoutBodyOne.IsHeroOnBoard(Razzle), Is.True);

            var alone = Board(new[] { new HeroOccupancy(Razzle, 3, new HexCoordinate(-2, 0)) });
            Assert.That(alone.TryGetHeroCoordinate(Razzle, out var only), Is.True);
            Assert.That(only, Is.EqualTo(new HexCoordinate(-2, 0)), "One standing body is unambiguous.");

            // Milestone 9.21h (RZ-Q13): a radius anchored on him is drawn around every body; 9.21a's
            // named refusal (TryGetSoleHeroCoordinate) is gone with the reading settled.
            var anchors = board.GetHeroRadiusAnchors(Razzle);
            Assert.That(anchors, Is.EqualTo(board.GetHeroBodies(Razzle).Select(body => body.Coordinate)),
                "every standing body anchors \"in radius of you\", in body order.");
            Assert.That(alone.GetHeroRadiusAnchors(Razzle), Is.EqualTo(new[] { new HexCoordinate(-2, 0) }));
            Assert.That(AdjacentMoveAuthority.IsWithinRadiusOfAnyBody(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(4, 0) }, 1, new HexCoordinate(5, 0)), Is.True,
                "a space is in radius of him if it is in radius of at least one body.");
            Assert.That(AdjacentMoveAuthority.IsWithinRadiusOfAnyBody(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(4, 0) }, 1, new HexCoordinate(2, 0)), Is.False);
        }

        [Test]
        public void AMove_KeepsTheBodyThatStepped_AndADefeatRemovesEveryBody_AndRespawnIsOneBody()
        {
            var board = Board(new[]
            {
                new HeroOccupancy(Razzle, 1, new HexCoordinate(0, 0)),
                new HeroOccupancy(Razzle, 2, new HexCoordinate(2, 0))
            });
            var moved = board.TryCommit(new AdjacentMoveCommand(
                Razzle, new HexCoordinate(2, 0), new HexCoordinate(2, 1)));
            Assert.That(moved.Accepted, Is.True);
            Assert.That(board.TryGetHeroBodyCoordinate(Razzle, 2, out var stepped), Is.True);
            Assert.That(stepped, Is.EqualTo(new HexCoordinate(2, 1)), "The body standing on the origin stepped.");
            Assert.That(board.TryGetHeroBodyCoordinate(Razzle, 1, out var still), Is.True);
            Assert.That(still, Is.EqualTo(new HexCoordinate(0, 0)));

            var restored = AdjacentMoveAuthority.Restore(
                board.CaptureRestoreState(), board.MapDefinition, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(restored.Occupancies, Is.EquivalentTo(board.Occupancies));
            Assert.That(restored.HeroBodySupplyOf(Razzle), Is.EqualTo(4));
        }

        [Test]
        public void TheAttackTargetQuery_OffersEachBody_AndNamesTheHeroOnce()
        {
            var participants = StagedBoardParticipants.Create();
            var red = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blue = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var board = Board(
                new[]
                {
                    new HeroOccupancy(red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue, 1, new HexCoordinate(1, 0)),
                    new HeroOccupancy(blue, 2, new HexCoordinate(-1, 0)),
                    new HeroOccupancy(blue, 3, new HexCoordinate(3, 0))
                },
                new[] { new HeroBodySupply(blue, 4) });

            var targets = AttackTargetQuery.FindLegalEnemyTargets(
                red, AttackRangeKind.NonRanged, 1, participants, board, RuleModificationSet.Empty);
            Assert.That(targets, Is.EqualTo(new[]
            {
                AttackTarget.HeroBodyOf(blue, 1),
                AttackTarget.HeroBodyOf(blue, 2)
            }), "An Attack targets a body (RZ-Q3); the body out of reach is not offered.");
            Assert.That(AttackTargetQuery.FindLegalEnemyHeroes(
                    red, AttackRangeKind.NonRanged, 1, participants, board, RuleModificationSet.Empty),
                Is.EqualTo(new[] { blue }), "The hero is named once.");
            Assert.That(AttackTarget.HeroBodyOf(blue, 2).IsSameUnit(AttackTarget.Hero(blue)), Is.True,
                "A different-target exclusion counts him once (RZ-Q2).");

            Assert.That(
                UnitPresenceQuery.FindUnitsWithin(new HexCoordinate(0, 0), 1,
                        EffectiveUnitKind.Hero, board, RuleModificationSet.Empty)
                    .Count(unit => unit.Equals(RuleObject.ForHero(blue))),
                Is.EqualTo(1), "Presence reads every body and counts the hero once.");

            Assert.That(() => DefenseValueQuery.CalculateMinionContributions(blue, participants, board),
                Throws.ArgumentException.With.Message.Contains("defending body"),
                "A Defense read that does not name the attacked body is refused, not answered from one.");
            Assert.That(DefenseValueQuery.CalculateMinionContributions(blue, 2, participants, board),
                Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // A staged match: an enemy attacks either body; a failed defense defeats him once.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheAttackChoice_OffersBothBodies_EachPreviewedFromItsOwnSpace()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(
                out _, out var blue, out var besideBodyTwo);
            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargets.Where(target => target.Kind == AttackTargetKind.Hero),
                Is.EqualTo(new[] { AttackTarget.HeroBodyOf(blue, 1), AttackTarget.HeroBodyOf(blue, 2) }));
            Assert.That(choice.LegalTargetHeroIds, Is.EqualTo(new[] { blue }));

            var previews = session.Projection.PendingAttackTargetDefensePreviews;
            Assert.That(previews.Select(value => value.HeroBody), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(previews.Single(value => value.HeroBody == 1).MinionContributions, Is.Empty,
                "No minion stands beside body 1.");
            Assert.That(previews.Single(value => value.HeroBody == 2).MinionContributions
                    .Select(value => value.SourceMinionId),
                Is.EqualTo(new[] { besideBodyTwo }),
                "p.14's minion modifier is measured from the attacked body (RZ-Q3).");

            var answers = session.Projection.DecisionContract.Commands
                .Where(command => command.Kind == DirectorCommandKind.ChooseAttackTarget && command.HeroId.IsDefined)
                .ToArray();
            Assert.That(answers.Select(command => command.HeroBody), Is.EqualTo(new[] { 1, 2 }),
                "Each body is its own answer, and each names its body.");

            var before = session.CaptureSnapshot();
            var heroOnly = session.Execute(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, blue));
            Assert.That(heroOnly.Accepted, Is.False,
                "An answer naming only a hero standing on two legal spaces is refused, never aimed at one.");
            Assert.That(heroOnly.Detail, Does.Contain("must name the body"));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before), "The refusal is atomic.");
        }

        [Test]
        public void AttackingEitherBody_MeasuresTheDefenseFromThatBody()
        {
            foreach (var body in new[] { 1, 2 })
            {
                var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(
                    out _, out var blue, out var besideBodyTwo);
                var choice = session.Projection.PendingAttackTargetChoice;
                HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                    choice.ResolutionId, choice.ChoiceId, blue, body));

                Assert.That(session.Projection.PendingAttackResolution.Target,
                    Is.EqualTo(AttackTarget.HeroBodyOf(blue, body)));
                var preview = session.Projection.PendingDefensePreview;
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.MinionContributions.Select(value => value.SourceMinionId),
                    body == 2 ? Is.EqualTo(new[] { besideBodyTwo }) : Is.Empty,
                    "Body " + body + ": the Defense is the attacked body's (RZ-Q3, RZ-Q4).");
            }
        }

        [Test]
        public void AFailedDefense_DefeatsHimOnce_RemovesEveryBody_AndHeRespawnsAsOne()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(
                out var red, out var blue, out _);
            var livesBefore = Lives(session, blue);
            var coinsBefore = Coins(session, red);
            var choice = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, blue, 2));
            var defense = session.Projection.PendingDefenseChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(blue)), Is.False,
                "\"If defeated, remove all of you\" (RZ-Q3): both bodies left the board.");
            var scoreboard = session.Projection.Scoreboard;
            Assert.That(scoreboard.Single(entry => entry.HeroId.Equals(blue)).Deaths, Is.EqualTo(1),
                "One Defeat of the hero, not one per body.");
            Assert.That(scoreboard.Single(entry => entry.HeroId.Equals(red)).Kills, Is.EqualTo(1));
            Assert.That(livesBefore - Lives(session, blue), Is.EqualTo(1),
                "p.13's Life counters are spent once, by the lowest Tier.");
            Assert.That(Coins(session, red) - coinsBefore, Is.EqualTo(1),
                "p.13's coins are paid once, by the defeated hero's Level (1).");

            // Red closes its turn; Blue's next card offers p.7's respawn, as one body.
            HeroBodiesBoard.Accept(session, DirectorCommand.ReleaseTurn());
            HeroBodiesBoard.CommitNextTurn(session, red, blue);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(red));
            HeroBodiesBoard.Accept(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
            if (session.Projection.PendingHeroRespawnChoice == null)
            {
                HeroBodiesBoard.Accept(session, DirectorCommand.ReleaseTurn());
            }

            var respawn = session.Projection.PendingHeroRespawnChoice;
            Assert.That(respawn, Is.Not.Null, "Off the board, he is offered the respawn.");
            HeroBodiesBoard.Accept(session, DirectorCommand.RespawnHero(respawn.RespawnInvocations[0]));
            var bodies = session.Projection.Heroes.Where(hero => hero.HeroId.Equals(blue)).ToArray();
            Assert.That(bodies.Select(hero => hero.Body), Is.EqualTo(new[] { HeroOccupancy.FirstBody }),
                "He respawns as one body.");
        }

        [Test]
        public void TwoBodies_RestoreRewindAndReplay_Identically()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out var blue, out _);
            var choice = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, blue, 2));
            var atDefense = session.CaptureSnapshot();

            // Snapshot restore at the Defense window with body 2 attacked.
            var restored = HeroBodiesBoard.CreateSession(out _, out _, out _);
            Assert.That(restored.RestoreSnapshot(atDefense).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(atDefense));
            Assert.That(restored.Projection.Heroes.Where(hero => hero.HeroId.Equals(blue))
                .Select(hero => hero.Body), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(restored.Projection.PendingAttackResolution.Target,
                Is.EqualTo(AttackTarget.HeroBodyOf(blue, 2)));

            // Continue both and converge.
            var defense = session.Projection.PendingDefenseChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            HeroBodiesBoard.Accept(restored, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            var after = session.CaptureSnapshot();
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(after), "Restore-and-continue converges.");

            // The replay oracle: a fresh session replaying the accepted commands.
            var replayed = HeroBodiesBoard.CreateSession(out _, out _, out _);
            DeterministicMatchLoop.Replay(replayed, session.Projection.AcceptedCommands.ToArray());
            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(after));

            // Undo walks back through the Defeat to the Defense window, and redoing converges.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(atDefense),
                "Undo restores both bodies and the attacked body.");
            HeroBodiesBoard.Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(after));

            // The prior schema is refused.
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(52));
        }

        [Test]
        public void TheWire_CarriesEveryBody_AndTheAttackedBody()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out var blue, out _);
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True);
                var client = new NetworkMatchReadModel(
                    host.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
                Assert.That(client.Heroes.Where(hero => hero.HeroId.Equals(blue)), Is.EqualTo(
                    session.Projection.Heroes.Where(hero => hero.HeroId.Equals(blue))));
                if (client.PendingAttackTargetChoice != null)
                {
                    Assert.That(client.PendingAttackTargetChoice.LegalTargets,
                        Is.EqualTo(session.Projection.PendingAttackTargetChoice.LegalTargets));
                    Assert.That(client.PendingAttackTargetDefensePreviews.Select(value => value.HeroBody),
                        Is.EqualTo(new[] { 1, 2 }));
                }
            }

            var command = DirectorCommand.ChooseAttackTarget(
                new ActionResolutionId("r"), new ActionResolutionStepId("c"), blue, 2);
            Assert.That(NetworkCommandCodec.TryDecode(NetworkCommandCodec.Encode(command), out var decoded), Is.True);
            Assert.That(decoded, Is.EqualTo(command));
            Assert.That(decoded.HeroBody, Is.EqualTo(2));
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(33));
        }

        private static int Lives(MatchSession session, HeroId hero)
        {
            var team = session.Projection.Participants.GetTeamForHero(hero).Id;
            return session.Projection.TeamLifeCounters.Single(entry => entry.Key.Equals(team)).Value;
        }

        private static int Coins(MatchSession session, HeroId hero) =>
            session.Projection.HeroProgression.Single(entry => entry.HeroId.Equals(hero)).Coins;
    }

    /// <summary>
    /// Milestone 9.21a. The configured mirror (Red Pursuit against Blue Longbow on the official map)
    /// with the Blue hero declaring four bodies (RZ-Q1) and standing on two spaces beside the Red
    /// hero, and one Blue melee minion beside body 2 only - so the two bodies' Defense previews differ.
    /// </summary>
    internal static class HeroBodiesBoard
    {
        /// <param name="bodyTwoAtSpawn">
        /// Milestone 9.21b. Stands body 2 on the Blue hero's own spawn instead, far from every Red unit,
        /// so the two bodies' offers differ: an Attack from body 1 only, and a Fast Travel from body 2.
        /// </param>
        internal static MatchSession CreateSession(
            out HeroId red, out HeroId blue, out MinionId besideBodyTwo, bool bodyTwoAtSpawn = false)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redSetup = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blueSetup = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            red = redSetup.HeroId;
            blue = blueSetup.HeroId;

            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            bool Free(HexCoordinate at) => playable.Contains(at) && !occupied.Contains(at);
            var redAt = redSetup.Coordinate;
            var beside = redAt.GetNeighbors().Where(Free).ToArray();

            // Two spaces beside the Red hero that are not beside each other, and a third beside body 2
            // and neither the Red hero nor body 1, for the minion only body 2's Defense reads.
            var (bodyOne, bodyTwo, minionAt) = beside
                .SelectMany(first => beside.Select(second => (first, second)))
                .Where(pair => HexDistanceQuery.Calculate(pair.first, pair.second) > 1)
                .SelectMany(pair => pair.second.GetNeighbors()
                    .Where(at => Free(at) &&
                        HexDistanceQuery.Calculate(at, pair.first) > 1 &&
                        HexDistanceQuery.Calculate(at, redAt) > 1)
                    .Select(at => (pair.first, pair.second, at)))
                .First();
            if (bodyTwoAtSpawn)
            {
                bodyTwo = blueSetup.Coordinate;
            }

            var moved = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee);
            besideBodyTwo = moved.MinionId;
            var movedId = moved.MinionId;
            var minions = setup.Minions
                .Select(minion => minion.MinionId.Equals(movedId)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId, minion.TeamId, minion.TeamSide, minion.MinionType,
                        minion.SpawnPoint, SpaceAt(map, minionAt), minionAt)
                    : minion)
                .ToArray();
            var heroes = setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, SpaceAt(map, bodyOne), bodyOne)
                    : hero)
                .ToArray();
            var staged = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes, minions);

            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(red, redAt),
                    new HeroOccupancy(blue, 1, bodyOne),
                    new HeroOccupancy(blue, 2, bodyTwo)
                },
                Array.Empty<ObstacleOccupancy>(),
                new[] { new HeroBodySupply(blue, 4) });
            return new MatchSession(new MatchSessionInitialState(scenario, staged, new[] { map }));
        }

        /// <summary>
        /// Milestone 9.21b. The Blue hero's card resolves first, with two bodies standing, so
        /// "which of you acts" (RZ-Q4) stands before any action of it.
        /// </summary>
        internal static MatchSession CreateSessionAtTheActingBodyPick(
            out HeroId red, out HeroId blue, bool bodyTwoAtSpawn = false)
        {
            var session = CreateSession(out red, out blue, out _, bodyTwoAtSpawn);
            CommitBlueFirst(session, red, blue);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(blue),
                "Fixture precondition: the many-bodied hero acts first.");
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Not.Null,
                "Fixture precondition: two bodies stand as his card resolves.");
            return session;
        }

        /// <summary>
        /// Milestone 9.21b. Blue commits its highest-Initiative Attack card and Red its lowest card, so
        /// Blue acts first (a tie is broken for Blue).
        /// </summary>
        internal static void CommitBlueFirst(
            MatchSession session, HeroId red, HeroId blue, Action<DirectorCommand> accept = null)
        {
            accept = accept ?? (command => Accept(session, command));
            var blueCard = Hand(session, blue)
                .Where(card => Definition(session, card).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Attack))
                .OrderByDescending(card => Definition(session, card).Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();
            var redCard = Hand(session, red)
                .OrderBy(card => Definition(session, card).Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();
            accept(DirectorCommand.CommitCard(red, redCard.Id));
            accept(DirectorCommand.CommitCard(blue, blueCard.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                accept(DirectorCommand.ChooseTiedActor(tie.OwningSide, blueCard.Id));
            }
        }

        /// <summary>Red's Attack started, both Blue bodies adjacent and legal.</summary>
        internal static MatchSession CreateSessionAtTheAttackTargetChoice(
            out HeroId red, out HeroId blue, out MinionId besideBodyTwo)
        {
            var session = CreateSession(out red, out blue, out besideBodyTwo);
            CommitNextTurn(session, red, blue);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(red),
                "Fixture precondition: the attacker acts first.");
            var attack = session.Projection.AvailableTurnActions.First(offer =>
                offer.IsAvailable &&
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null);
            return session;
        }

        /// <summary>Red's Attack aimed at body 2, standing at the Defense window.</summary>
        internal static MatchSession CreateSessionAtTheDefenseOfBodyTwo()
        {
            var session = CreateSessionAtTheAttackTargetChoice(out _, out var blue, out _);
            var choice = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, blue, 2));
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null);
            return session;
        }

        /// <summary>
        /// Both heroes commit a card: Red its highest-Initiative Attack card, Blue its lowest card, so
        /// Red acts first (a tie is broken for Red).
        /// </summary>
        internal static void CommitNextTurn(MatchSession session, HeroId red, HeroId blue)
        {
            var redCard = Hand(session, red)
                .Where(card => Definition(session, card).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Attack))
                .OrderByDescending(card => Definition(session, card).Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();
            var blueCard = Hand(session, blue)
                .OrderBy(card => Definition(session, card).Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();
            Accept(session, DirectorCommand.CommitCard(red, redCard.Id));
            Accept(session, DirectorCommand.CommitCard(blue, blueCard.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, redCard.Id));
            }
        }

        internal static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, command.Kind + ": " + result.Detail);
        }

        private static ConfiguredSpaceReference SpaceAt(BoardMapDefinition map, HexCoordinate at) =>
            new ConfiguredSpaceReference(map.Id, map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(at)).Id);

        private static IEnumerable<CardInstance> Hand(MatchSession session, HeroId hero) =>
            session.Projection.Cards.Where(card =>
                card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

        private static CardDefinition Definition(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
    }
}

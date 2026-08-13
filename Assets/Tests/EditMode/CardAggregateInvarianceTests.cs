using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    /// The behaviour-invariance harness for milestone 6.1f. It pins, for six scenarios, a digest of
    /// the authoritative snapshot and a digest of the diagnostic snapshot - the latter being the
    /// serialized ordered rule-event log, so an event that changed order, count, or content moves
    /// its digest.
    /// </summary>
    /// <remarks>
    /// 6.1f is a pure refactor: one card mutation path, one aggregate root, one commit boundary. Its
    /// whole claim is that no observable behaviour changes, and a refactor of that size cannot be
    /// argued safe from a green suite alone - the suites would also pass if the refactor quietly
    /// reordered an event or dropped a field that no assertion happens to name.
    ///
    /// So the digests below were captured at the PRE-refactor revision and are asserted unchanged
    /// afterwards. They are not free-floating expected values: any difference means the refactor
    /// changed authoritative state or the event stream, and the refactor is wrong until explained.
    ///
    /// Each run also writes the measured digests to tmp/ so a mismatch can be diffed rather than
    /// only reported.
    /// </remarks>
    public sealed class CardAggregateInvarianceTests
    {
        // Captured at pre-refactor revision 18bf74d. Do not "update to match" a failing run.
        private static readonly ScenarioDigest[] ExpectedDigests =
        {
            new ScenarioDigest("accepted", "36185da66c358dccfc10d1739088d9a50dae193b60f244a95609b93e32f9626d", "36f7e3ee424f823aff4510c04c8b65819c04145dbf5ba641dc2656baba35d2cb"),
            new ScenarioDigest("rejected", "36185da66c358dccfc10d1739088d9a50dae193b60f244a95609b93e32f9626d", "36f7e3ee424f823aff4510c04c8b65819c04145dbf5ba641dc2656baba35d2cb"),
            new ScenarioDigest("stale", "36185da66c358dccfc10d1739088d9a50dae193b60f244a95609b93e32f9626d", "36f7e3ee424f823aff4510c04c8b65819c04145dbf5ba641dc2656baba35d2cb"),
            new ScenarioDigest("restored", "36185da66c358dccfc10d1739088d9a50dae193b60f244a95609b93e32f9626d", "36f7e3ee424f823aff4510c04c8b65819c04145dbf5ba641dc2656baba35d2cb"),
            new ScenarioDigest("replayed", "36185da66c358dccfc10d1739088d9a50dae193b60f244a95609b93e32f9626d", "36f7e3ee424f823aff4510c04c8b65819c04145dbf5ba641dc2656baba35d2cb"),
            new ScenarioDigest("undo-truncated", "b64dfb1808e615ee98056dad98ec9022baa02dd762205d1a14483ae12d27be2d", "e6a2f133474546332f55732d1b835759a48d7d17de603b02acd6890292294dee"),
            new ScenarioDigest("defense-discarded", "1aeb67526c3a7c5d16ca957b467e316e487fdb2396f3a299cc66601d2e9db0e2", "95a64c0f33626ed20fcd871bdf15f9089586a490cef02cbb3681b4189a46b3d5"),
            new ScenarioDigest("defense-discarded-restored", "1aeb67526c3a7c5d16ca957b467e316e487fdb2396f3a299cc66601d2e9db0e2", "95a64c0f33626ed20fcd871bdf15f9089586a490cef02cbb3681b4189a46b3d5"),
            new ScenarioDigest("defense-discarded-replayed", "1aeb67526c3a7c5d16ca957b467e316e487fdb2396f3a299cc66601d2e9db0e2", "95a64c0f33626ed20fcd871bdf15f9089586a490cef02cbb3681b4189a46b3d5")
        };

        // Three invariants are visible in the pinned values themselves, and each would break loudly:
        //   - accepted, rejected, and stale share one digest pair, because a refused command must
        //     leave authoritative state and the event log untouched.
        //   - restored and replayed match their live counterparts, because a snapshot round trip and
        //     a command replay must both converge on the state they came from.
        //   - the two defense-discard families differ from every other scenario, because a card
        //     actually changed location.

        [Test]
        public void SixScenarios_ProduceByteIdenticalSnapshotsAndOrderedEvents()
        {
            var measured = MeasureAllScenarios();

            WriteMeasuredDigests(measured);

            Assert.That(measured.Select(value => value.Name),
                Is.EqualTo(ExpectedDigests.Select(value => value.Name)),
                "The scenario set itself changed; the pinned digests no longer describe the same six cases.");

            foreach (var expected in ExpectedDigests)
            {
                var actual = measured.Single(value => value.Name == expected.Name);
                Assert.That(actual.AuthoritativeSha256, Is.EqualTo(expected.AuthoritativeSha256),
                    $"Scenario '{expected.Name}': the authoritative snapshot changed. A refactor that " +
                    "must preserve behaviour changed authoritative state.");
                Assert.That(actual.DiagnosticSha256, Is.EqualTo(expected.DiagnosticSha256),
                    $"Scenario '{expected.Name}': the ordered rule-event log changed. Events must be " +
                    "emitted in the same order, with the same content, as before the refactor.");
            }
        }

        private static IReadOnlyList<ScenarioDigest> MeasureAllScenarios()
        {
            return new[]
            {
                MeasureAccepted(),
                MeasureRejected(),
                MeasureStale(),
                MeasureRestored(),
                MeasureReplayed(),
                MeasureUndoTruncated(),
                MeasureDefenseDiscarded(),
                MeasureDefenseDiscardedRestored(),
                MeasureDefenseDiscardedReplayed()
            };
        }

        /// <summary>
        /// Drives an Attack through to a chosen Defense card, which is the ONE path that discards a
        /// card by calling the location transition directly instead of through
        /// <see cref="CardLifecycleAuthority"/>. 6.1f reroutes exactly this call, so a harness that
        /// never reaches it would not be measuring the thing being changed.
        /// </summary>
        private static ScenarioDigest MeasureDefenseDiscarded()
        {
            var session = CreateAdjacentSession();
            DiscardOneDefenseCard(session);
            return Digest("defense-discarded", session.Projection);
        }

        private static ScenarioDigest MeasureDefenseDiscardedRestored()
        {
            var source = CreateAdjacentSession();
            DiscardOneDefenseCard(source);
            var payload = source.CaptureSnapshot();

            var restored = CreateAdjacentSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True,
                "A discarded-Defense snapshot must restore.");

            return Digest("defense-discarded-restored", restored.Projection);
        }

        private static ScenarioDigest MeasureDefenseDiscardedReplayed()
        {
            var source = CreateAdjacentSession();
            DiscardOneDefenseCard(source);
            var commands = source.Projection.AcceptedCommands.ToArray();

            var replayed = CreateAdjacentSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay, including the Defense discard.");
            }

            return Digest("defense-discarded-replayed", replayed.Projection);
        }

        private static void DiscardOneDefenseCard(MatchSession session)
        {
            StartTiedAttackTurn(session);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);

            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())).Accepted, Is.True);

            var defenseChoice = session.Projection.PendingDefenseChoice;
            Assert.That(defenseChoice, Is.Not.Null, "The scenario must actually reach a Defense choice.");
            var defenseCardId = defenseChoice.LegalDefenseOffers
                .Select(offer => offer.SourceCardInstanceId)
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .First();

            Assert.That(session.Execute(DirectorCommand.ChooseDefenseCard(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId,
                defenseCardId)).Accepted, Is.True);

            var discarded = session.Projection.Cards.Single(card => card.Id.Equals(defenseCardId));
            Assert.That(discarded.State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "The Defense card must actually have moved to the discard pile; otherwise this " +
                "scenario is not exercising the transition it exists to pin.");
        }

        private static void StartTiedAttackTurn(MatchSession session)
        {
            var red = HandCards(session, TeamSide.Red)
                .Where(card => DefinitionOf(session, card).ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack))
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            var blue = HandCards(session, TeamSide.Blue)
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();

            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);

            var choice = session.Projection.PendingInitiativeChoice;
            if (choice != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(choice.OwningSide, red.Id)).Accepted, Is.True);
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));
        }

        private static ActionKind ActionKindOf(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        /// <summary>
        /// Two heroes adjacent on a two-space synthetic board, so an Attack has a legal target and a
        /// Defense response is reachable. Mirrors the adjacent fixture in MatchFeaturePresenterTests;
        /// the project keeps session builders fixture-local, and consolidating them is 6.1k's subject.
        /// </summary>
        private static MatchSession CreateAdjacentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("aggregate-invariance-adjacent"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-aggregate"),
                        "Red Aggregate",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-aggregate"),
                        "Blue Aggregate",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>Both sides commit, and any tie is resolved. The ordinary accepted path.</summary>
        private static ScenarioDigest MeasureAccepted()
        {
            var session = CreateSession();
            CommitBothSides(session);
            return Digest("accepted", session.Projection);
        }

        /// <summary>
        /// A rejected command must leave authoritative state and the event log untouched, so this
        /// scenario's digests must equal the accepted scenario's.
        /// </summary>
        private static ScenarioDigest MeasureRejected()
        {
            var session = CreateSession();
            CommitBothSides(session);

            var unknownHero = new HeroId("no-such-hero");
            var anyCard = session.Projection.Cards[0];
            var result = session.Execute(DirectorCommand.CommitCard(unknownHero, anyCard.Id));
            Assert.That(result.Accepted, Is.False, "The rejected scenario must actually reject.");

            return Digest("rejected", session.Projection);
        }

        /// <summary>Re-committing an already committed card is the stale-choice path.</summary>
        private static ScenarioDigest MeasureStale()
        {
            var session = CreateSession();
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);

            var stale = session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Assert.That(stale.Accepted, Is.False, "The stale scenario must actually reject.");

            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);

            return Digest("stale", session.Projection);
        }

        /// <summary>Restoring a captured snapshot into a fresh session must reproduce it exactly.</summary>
        private static ScenarioDigest MeasureRestored()
        {
            var source = CreateSession();
            CommitBothSides(source);
            var payload = source.CaptureSnapshot();

            var restored = CreateSession();
            var result = restored.RestoreSnapshot(payload);
            Assert.That(result.Accepted, Is.True, "The restored scenario must actually restore.");

            return Digest("restored", restored.Projection);
        }

        /// <summary>Replaying the accepted command prefix into a fresh session must converge.</summary>
        private static ScenarioDigest MeasureReplayed()
        {
            var source = CreateSession();
            CommitBothSides(source);
            var commands = source.Projection.AcceptedCommands.ToArray();

            var replayed = CreateSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay.");
            }

            return Digest("replayed", replayed.Projection);
        }

        /// <summary>Undo must truncate to exactly the state the prefix produced.</summary>
        private static ScenarioDigest MeasureUndoTruncated()
        {
            var session = CreateSession();
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);

            var undo = session.Undo();
            Assert.That(undo.Accepted, Is.True, "The undo-truncated scenario must actually undo.");

            return Digest("undo-truncated", session.Projection);
        }

        private static void CommitBothSides(MatchSession session)
        {
            var red = HandCards(session, TeamSide.Red)[0];
            var blue = HandCards(session, TeamSide.Blue)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            ResolveInitiativeTieIfPending(session);
        }

        private static void ResolveInitiativeTieIfPending(MatchSession session)
        {
            var choice = session.Projection.PendingInitiativeChoice;
            if (choice == null)
            {
                return;
            }

            Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(
                choice.OwningSide, choice.CandidateCardInstanceIds[0])).Accepted, Is.True);
        }

        private static IReadOnlyList<CardInstance> HandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static ScenarioDigest Digest(string name, MatchSessionProjection projection)
        {
            return new ScenarioDigest(
                name,
                Sha256(projection.AuthoritativeSnapshot),
                Sha256(projection.DiagnosticSnapshot));
        }

        private static void WriteMeasuredDigests(IReadOnlyList<ScenarioDigest> measured)
        {
            var builder = new StringBuilder();
            foreach (var digest in measured)
            {
                builder.AppendLine(
                    $"new ScenarioDigest(\"{digest.Name}\", \"{digest.AuthoritativeSha256}\", \"{digest.DiagnosticSha256}\"),");
            }

            var directory = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName, "tmp");
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "milestone-6-1f-measured-digests.txt"),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static string Sha256(string payload)
        {
            Assert.That(payload, Is.Not.Null.And.Not.Empty,
                "A scenario produced no snapshot, so its digest would be meaningless.");
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))
                    .Select(value => value.ToString("x2")));
            }
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
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
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class ScenarioDigest
        {
            public ScenarioDigest(string name, string authoritativeSha256, string diagnosticSha256)
            {
                Name = name;
                AuthoritativeSha256 = authoritativeSha256;
                DiagnosticSha256 = diagnosticSha256;
            }

            public string Name { get; }
            public string AuthoritativeSha256 { get; }
            public string DiagnosticSha256 { get; }
        }
    }
}

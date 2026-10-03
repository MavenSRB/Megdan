using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8a. Mortimer's Zombie token (MO-Q1 four, MO-Q4 retained at every round end) and
    /// Awaken!'s up-to-four placement into spawn points in radius or spaces adjacent to him (MO-Q2),
    /// with p.18's short supply, through a configured round, with restore/replay of the placement
    /// pause.
    ///
    /// <para><b>The board.</b> A radius-5 disk in two zones - the inner three rings and the outer two -
    /// so MO-Q2's "not limited to the battle zone" is a measured difference. Mortimer stands at the
    /// origin; the spawn points are:</para>
    /// <list type="table">
    /// <item><term>(2, 1)</term><description>minion spawn point, inner zone, distance 3</description></item>
    /// <item><term>(-3, 0)</term><description><b>hero</b> spawn point, inner zone, distance 3</description></item>
    /// <item><term>(0, 4)</term><description>minion spawn point, <b>outer</b> zone, distance 4</description></item>
    /// <item><term>(5, 0)</term><description>minion spawn point, distance 5 - beyond Radius 4</description></item>
    /// <item><term>(-2, -1)</term><description>minion spawn point, distance 3, a minion standing on it</description></item>
    /// </list>
    /// </summary>
    public sealed class MortimerAwakenTests
    {
        private static readonly HeroId Mortimer = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("mortimer-awaken");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate MinionSpawn = H(2, 1);
        private static readonly HexCoordinate HeroSpawn = H(-3, 0);
        private static readonly HexCoordinate OuterSpawn = H(0, 4);
        private static readonly HexCoordinate FarSpawn = H(5, 0);
        private static readonly HexCoordinate OccupiedSpawn = H(-2, -1);

        private static readonly HexCoordinate[] Neighbours =
        {
            H(1, 0), H(1, -1), H(0, -1), H(-1, 0), H(-1, 1), H(0, 1)
        };

        // --- The Zombie token -----------------------------------------------------------------------

        [Test]
        public void Zombie_IsAFourTokenSupplyThatNoRoundEndRemoves()
        {
            Assert.That(MortimerComponentContent.Zombie.SupplyCount, Is.EqualTo(4),
                "MO-Q1, user-settled: four.");
            Assert.That(MortimerComponentContent.Zombie.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.RetainedAtEndOfRound),
                "MO-Q4, user-settled: Awaken!'s second sentence is every Zombie's lifetime.");

            var zombie = new PlacedToken(new TokenInstanceId("zombie"), MortimerComponentContent.Zombie,
                Mortimer, Mortimer, new CardInstanceId("card"), H(1, 0));
            var rock = new PlacedToken(new TokenInstanceId("rock"), MrakComponentContent.Rock, Mortimer,
                Mortimer, new CardInstanceId("card"), H(2, 0));
            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfRound(new[] { zombie, rock }),
                Is.EqualTo(new[] { rock }),
                "p.8 takes an ordinary token back at the round's end and leaves the Zombie standing.");
            Assert.That(MortimerComponentContent.Zombie.Properties, Is.Empty,
                "no face gives a Zombie a property: it is a bare p.19 obstacle.");
        }

        [Test]
        public void Awaken_IsTheVerifiedSilverBasic()
        {
            var awaken = MortimerCardContent.AwakenDefinition;
            Assert.That(awaken.Initiative.Value, Is.EqualTo(6));
            Assert.That(awaken.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill)
                .BaseValues.Single().Amount, Is.EqualTo(4), "Radius 4.");
            Assert.That(awaken.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single(value => value.Channel == PrintedValueChannel.Defense).Amount, Is.EqualTo(3),
                "Basic Defense 3.");
        }

        // --- Where a Zombie may go ------------------------------------------------------------------

        [Test]
        public void Awaken_OffersEveryEmptySpawnPointInRadiusAndEverySpaceBesideHim()
        {
            var round = new Round();
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-1"),
                "a full pile needs nothing back, so the first question is where the first Zombie goes.");
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(
                Neighbours.Concat(new[] { MinionSpawn, HeroSpawn, OuterSpawn })),
                "MO-Q2: any empty spawn point in Radius 4 - minion or hero, either zone - and every " +
                "empty space adjacent to Mortimer; nothing else.");
        }

        [Test]
        public void Awaken_NeverOffersAnOrdinarySpaceAnOccupiedSpawnPointOrOneBeyondRadius()
        {
            var round = new Round();
            round.StartPrimarySkill();
            var offered = round.OfferedSpaces();

            Assert.That(offered, Has.No.Member(H(0, 2)), "an ordinary space two out is neither.");
            Assert.That(offered, Has.No.Member(OccupiedSpawn), "a Place goes only into an empty space (p.18).");
            Assert.That(offered, Has.No.Member(FarSpawn), "p.9's Radius 4 bounds the spawn points too.");
        }

        [Test]
        public void Awaken_AdjacencyIsMeasuredFromWhereMortimerStands()
        {
            var round = new Round(mortimerAt: H(3, -3));
            round.StartPrimarySkill();

            Assert.That(round.OfferedSpaces(), Has.Member(H(4, -3)), "beside him where he stands.");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(1, 0)),
                "the origin's neighbours are ordinary spaces once he has moved away.");
            Assert.That(round.OfferedSpaces(), Has.Member(MinionSpawn),
                "(2, 1) is still a spawn point within four of (3, -3).");
        }

        // --- Up to four -----------------------------------------------------------------------------

        [Test]
        public void Awaken_PlacesFourZombies()
        {
            var round = new Round();
            round.StartPrimarySkill();
            round.PickSpaces(MinionSpawn);
            round.PickSpaces(OuterSpawn);
            round.PickSpaces(H(1, 0));
            round.PickSpaces(HeroSpawn);

            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { MinionSpawn, OuterSpawn, H(1, 0), HeroSpawn }));
            Assert.That(round.ActionEnded, Is.True, "four placed is the printed most.");
        }

        /// <summary>
        /// Milestone 9.39, ruling W10 (the user, 2026-10-03): "A decline ends it. 'Up to 4' is one
        /// decision about how many. Once you say no, you are done." Before it each placement was its
        /// own "up to" step and the third and fourth were still asked after the second was declined.
        /// </summary>
        [Test]
        public void Awaken_UpToFour_StopsWhereMortimerDeclines()
        {
            var round = new Round();
            round.StartPrimarySkill();
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-2"));
            round.Decline();

            Assert.That(round.PendingNode, Is.Null, "W10: the decline closes the count; no third placement is asked.");
            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(0, 1) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        /// <summary>
        /// The 9.13 reader's own case (`mortimer-awaken-up-to-declined-after-two`), under W10: two of
        /// four placed, one decline, nothing further asked, and both Zombies standing.
        /// </summary>
        [Test]
        public void Awaken_TwoOfFourThenADecline_EndsTheCountWithBothStanding()
        {
            var round = new Round();
            round.StartPrimarySkill();
            round.PickSpaces(MinionSpawn);
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-3"));
            round.Decline();

            Assert.That(round.ActionEnded, Is.True, "no fourth pass is offered after the decline.");
            Assert.That(round.PendingNode, Is.Null);
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { MinionSpawn, H(0, 1) }),
                "everything already placed stands.");
        }

        [Test]
        public void Awaken_ZeroPlaced_IsALegalPlay()
        {
            var round = new Round();
            round.StartPrimarySkill();
            round.Decline();

            Assert.That(round.Zombies(), Is.Empty, "\"up to 4\" includes none, and one decline says so (W10).");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Awaken_AnEmptyPileAsksWhichStandingZombieComesBack()
        {
            var standing = new[] { H(4, -4), H(-4, 4), H(4, 0), H(-5, 1) };
            var round = new Round(zombiesAt: standing);
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("awaken-reclaim-1"),
                "all four Zombies stand, so p.18's short supply comes first.");
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(standing));
            round.PickSpaces(standing[1]);

            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-1"));
            round.PickSpaces(MinionSpawn);
            Assert.That(round.PendingNode, Is.EqualTo("awaken-reclaim-2"),
                "the pile is empty again, so the second pass opens with p.18's step.");
            round.Decline();

            Assert.That(round.ActionEnded, Is.True,
                "W10: declining the short-supply step is declining the pass, and the count closes with it.");
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { standing[0], standing[2], standing[3], MinionSpawn }),
                "the named Zombie came back to the pile and went out again; the other three stayed.");
        }

        [Test]
        public void Awaken_TheShortSupplyStepStandsAsideWhileThePileHoldsAZombie()
        {
            var round = new Round(zombiesAt: new[] { H(4, -4), H(-4, 4), H(4, 0) });
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("awaken-place-1"),
                "one Zombie is still in the pile, so the first placement asks nothing first - and the " +
                "step standing aside on its own is not a decline, so W10 closes nothing here.");
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo("awaken-reclaim-2"),
                "the pile is empty now, so the second placement starts with p.18's step.");
        }

        [Test]
        public void Awaken_APausedPlacementRestoresAndReplaysToTheSameBytes()
        {
            var round = new Round();
            round.StartPrimarySkill();
            round.PickSpaces(MinionSpawn);

            round.AssertRestoreReplays(H(-1, 1));
        }

        // --- Offer and answer agree -----------------------------------------------------------------

        [Test]
        public void Awaken_WithASpaceToFill_IsOffered()
        {
            Assert.That(new Round().IsOffered(MortimerCardContent.PrimarySkill), Is.True);
        }

        [Test]
        public void Awaken_BoxedInWithEverySpawnPointInRadiusTaken_IsNotOffered()
        {
            var taken = Neighbours.Concat(new[] { MinionSpawn, HeroSpawn, OuterSpawn }).ToArray();
            var round = new Round(minionsAt: taken);

            Assert.That(round.IsOffered(MortimerCardContent.PrimarySkill), Is.False,
                "every placement could only decline - an offer must be able to do what it offers.");
        }

        [Test]
        public void Awaken_BoxedInButForOneSpawnPoint_OffersExactlyThatSpace()
        {
            var taken = Neighbours.Concat(new[] { MinionSpawn, HeroSpawn }).ToArray();
            var round = new Round(minionsAt: taken);

            Assert.That(round.IsOffered(MortimerCardContent.PrimarySkill), Is.True);
            round.StartPrimarySkill();
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { OuterSpawn }));
        }

        // --- The harness ----------------------------------------------------------------------------

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("mortimer-awaken-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static SpaceId SpaceIdFor(HexCoordinate at) => new SpaceId($"q{at.Q}_r{at.R}");

        private static BoardMapDefinition CreateMap()
        {
            var inner = new BoardMapZone(new ZoneId("mortimer-inner"), "Inner", "synthetic", null, false, false);
            var outer = new BoardMapZone(new ZoneId("mortimer-outer"), "Outer", "synthetic", null, false, false);
            var spaces = (from q in Enumerable.Range(-5, 11)
                          from r in Enumerable.Range(-5, 11)
                          where Math.Abs(q + r) <= 5
                          let at = new HexCoordinate(q, r)
                          select new BoardMapSpace(SpaceIdFor(at), at, BoardMapSpaceKind.Playable,
                              HexDistanceQuery.Calculate(at, H(0, 0)) <= 3 ? inner.Id : outer.Id))
                .ToArray();
            var spawnPoints = new[]
            {
                new BoardMapSpawnPoint(new SpawnPointId("minion"), SpaceIdFor(MinionSpawn), "red", "melee-minion"),
                new BoardMapSpawnPoint(new SpawnPointId("hero"), SpaceIdFor(HeroSpawn), "blue", "hero"),
                new BoardMapSpawnPoint(new SpawnPointId("outer"), SpaceIdFor(OuterSpawn), "blue", "ranged-minion"),
                new BoardMapSpawnPoint(new SpawnPointId("far"), SpaceIdFor(FarSpawn), "red", "melee-minion"),
                new BoardMapSpawnPoint(new SpawnPointId("occupied"), SpaceIdFor(OccupiedSpawn), "red", "melee-minion")
            };
            return new BoardMapDefinition(Map, 1, "Mortimer Awaken", spaces, new[] { inner, outer },
                spawnPoints, Array.Empty<ZoneId>());
        }

        /// <summary>One round on the board above with Awaken! committed and Mortimer selected to act.</summary>
        private sealed class Round
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly MatchContentCatalogs catalogs;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly QuickGameMinionSetupState[] minions;

            internal Round(
                HexCoordinate? mortimerAt = null,
                IReadOnlyList<HexCoordinate> zombiesAt = null,
                IReadOnlyList<HexCoordinate> minionsAt = null)
            {
                var participants = StagedBoardParticipants.Create();
                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Mortimer, mortimerAt ?? H(0, 0)),
                    new HeroOccupancy(Enemy, H(-5, 5))
                };
                minions = new[] { OccupiedSpawn }.Concat(minionsAt ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"mortimer-awaken-minion-{index:00}"),
                        new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("occupied")),
                        new ConfiguredSpaceReference(Map, SpaceIdFor(at)), at))
                    .ToArray();
                board = new AdjacentMoveAuthority(
                    CreateMap(), heroes, Array.Empty<ObstacleOccupancy>(), minions);
                foreach (var at in zombiesAt ?? Array.Empty<HexCoordinate>())
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"mortimer-standing-zombie-{at.Q}-{at.R}"),
                        MortimerComponentContent.Zombie, Mortimer, Mortimer,
                        new CardInstanceId("mortimer-earlier-card"), at));
                }

                var face = MortimerCardContent.AwakenDefinition;
                source = CardInstance.CreateInHand(new CardInstanceId("mortimer-awaken-source"),
                    face.Reference, Mortimer);
                var hold = CardInstance.CreateInHand(new CardInstanceId("mortimer-awaken-enemy-hold"),
                    EnemyHold.Reference, Enemy);
                definitions = new[] { face, EnemyHold };
                instances = new[] { source, hold };
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(MortimerComponentContent.Handlers()),
                    new ComponentDefinitionCatalog(
                        MortimerComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Mortimer, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, hold.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Mortimer));
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal HexCoordinate[] Zombies() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MortimerComponentContent.ZombieId))
                    .Select(token => token.Coordinate).ToArray();

            internal HexCoordinate[] OfferedSpaces() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();

            private ActionOffer Offer(ActionDefinitionId id) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions, instances, Mortimer, source.Id)
                    .Single(offer => offer.ActionDefinitionId.Equals(id));

            internal bool IsOffered(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

            internal void StartPrimarySkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("mortimer-awaken-skill"),
                    Offer(MortimerCardContent.PrimarySkill)).Accepted, Is.True);

            internal void PickSpaces(params HexCoordinate[] spaces)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                AnswerSpaces(round, spaces);
            }

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an \"up to\" step can be declined");
                Assert.That(round.TryAnswerEffectChoice(choice.DeclineInvocation).Accepted, Is.True);
            }

            /// <summary>
            /// Snapshot at the open question, restore it, answer both the same way, and require the
            /// same next question and the same authoritative bytes - then once more from the retained
            /// pre-answer prefix.
            /// </summary>
            internal void AssertRestoreReplays(params HexCoordinate[] answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = Restore(bytes);
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

                PickSpaces(answer);
                AnswerSpaces(restored.Round, answer);
                var live = AuthoritativeSnapshotSerializer.Serialize(round, board);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(live), "the restored pause replays to the same authoritative bytes");

                var prefix = Restore(bytes);
                AnswerSpaces(prefix.Round, answer);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                    Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
            }

            private RestoredAuthoritativeMatch Restore(string bytes) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

            private static void AnswerSpaces(RoundResolutionAuthority target, HexCoordinate[] spaces)
            {
                var asked = target.CurrentTurnAuthority.PendingEffectChoice;
                foreach (var space in spaces)
                {
                    var open = target.CurrentTurnAuthority.PendingEffectChoice;
                    Assert.That(target.TryAnswerEffectChoice(open.SelectInvocations.Single(
                        invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space))))
                        .Accepted, Is.True);
                }

                var still = target.CurrentTurnAuthority.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(asked.ChoiceId) &&
                    still.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                }
            }
        }
    }
}

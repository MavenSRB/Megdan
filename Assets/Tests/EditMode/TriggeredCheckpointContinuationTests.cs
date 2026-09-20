using System;
using System.Collections.Generic;
using System.Linq;
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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.67. A triggered checkpoint continues past its first pause.
    ///
    /// <para><b>The defect, in one sentence, as milestone 8.61f measured it on shipped content:</b>
    /// <c>TriggeredEffectResolutionAuthority.ApplyCheckpoint</c> broke out of its candidate loop the
    /// moment one triggered effect paused, and nothing resumed the rest when that pause was answered -
    /// so a SECOND triggered face at the same checkpoint never ran at all. Min's Death Grenade beside
    /// Silverarrow's Warning Shot at one end-of-turn checkpoint was the measurement, and only the
    /// trigger the pipeline reached first ever took a card.</para>
    ///
    /// <para><b>What this file drives is the general case rather than that pair.</b> The subject is the
    /// pipeline: an ordered candidate list, a pause anywhere in it, and the same list carried on from
    /// the next candidate with the same tie-breaker order and the same per-candidate context - at the
    /// turn's close and at a checkpoint that is not the turn's close, across a candidate that finishes
    /// quietly, across a snapshot, and across an undo. Silverarrow's two "End of turn:" faces are the
    /// content it is driven with because they are the only shipped pair that can stand at one
    /// checkpoint under one controller, and a third synthetic occurrence carrying her Wild Hunt body is
    /// the candidate that finishes without asking anything.</para>
    ///
    /// <para><b>Nothing here is persisted that was not persisted before.</b> The remainder is held in
    /// memory while the match is live and REGENERATED from the paused frame's own identity when the
    /// match is restored - milestone 8.10's discipline for a restored card-text page - so snapshot
    /// schema v43 does not move. <see cref="ASnapshotTakenAtTheFirstPauseRestoresAndTheSecondStillRuns"/>
    /// is what proves that regeneration produces the same match the live remainder does.</para>
    /// </summary>
    public sealed class TriggeredCheckpointContinuationTests
    {
        private static readonly HeroId Silverarrow = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static HexCoordinate At(int q) => new HexCoordinate(q, 0);

        /// <summary>
        /// The whole milestone in one case. Two "End of turn:" faces stand at one checkpoint, each asks
        /// its own two questions, and each takes a card - in the order the pipeline built rather than
        /// the order they are named here.
        ///
        /// <para>The second cell creates them the other way round and gets the other order, which is
        /// what makes the first cell an assertion about the pipeline's tie-breaker rather than about
        /// how the two definitions happen to spell their ids.</para>
        /// </summary>
        [TestCase(false, TestName = "TwoPausingTriggers_SentinelCreatedFirst")]
        [TestCase(true, TestName = "TwoPausingTriggers_WarningShotCreatedFirst")]
        public void TwoPausingTriggersAtOneCheckpointBothRun_InThePipelinesOwnOrder(bool warningFirst)
        {
            var drive = Drive(
                warningFirst
                    ? new[] { Standing.WarningShot, Standing.TreetopSentinel }
                    : new[] { Standing.TreetopSentinel, Standing.WarningShot },
                enemySpareCards: 2);

            Assert.That(drive.PauseNodes, Is.EqualTo(warningFirst
                    ? new[] { "warning-select", "warning-discard", "sentinel-select", "sentinel-discard" }
                    : new[] { "sentinel-select", "sentinel-discard", "warning-select", "warning-discard" }),
                "Both candidates run, and the one created first runs first: the checkpoint's order is " +
                "the revealed side, then when each effect was created, then its identity.");
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(2),
                "Each face takes a card, which is the observable the 8.61f measurement was missing.");
            Assert.That(drive.TurnEnded, Is.True);
            Assert.That(drive.EffectPhases, Is.All.EqualTo(ActiveEffectPhase.Ended),
                "Both expire at the checkpoint they fire at, so neither can fire twice.");
        }

        /// <summary>
        /// A candidate between two pausing ones that finishes without asking anything. It is reached,
        /// it runs, it applies nothing - and the walk carries on to the candidate behind it, which is
        /// the half a dropped continuation would look identical on if the quiet one were last.
        ///
        /// <para>Wild Hunt's body is the quiet one because its offer is empty on this board: the only
        /// space exactly two away in a straight line is the one the enemy hero is standing on, so p.10's
        /// <c>May</c> step is simply not taken and nothing is staged for anybody to answer. The hero
        /// not having moved is what says it ran rather than that it was skipped.</para>
        /// </summary>
        [Test]
        public void ACandidateThatFinishesQuietlyDoesNotStopTheWalk()
        {
            var drive = Drive(
                new[] { Standing.TreetopSentinel, Standing.QuietWildHunt, Standing.WarningShot },
                enemySpareCards: 2);

            Assert.That(drive.PauseNodes, Is.EqualTo(new[]
                {
                    "sentinel-select", "sentinel-discard", "warning-select", "warning-discard"
                }),
                "The quiet candidate asks nothing, and the candidate behind it still asks everything.");
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(2));
            Assert.That(drive.SilverarrowSpace, Is.EqualTo(At(0)),
                "and the quiet candidate applied nothing: its own step had no legal destination.");
        }

        /// <summary>
        /// The list runs out. Nothing is owed once the last candidate has been answered, the turn
        /// closes inside that same answer, and no question is left standing for a seat that has none.
        /// </summary>
        [Test]
        public void ACheckpointIsExhaustedAfterItsLastPause_AndTheTurnCloses()
        {
            var drive = Drive(
                new[] { Standing.TreetopSentinel, Standing.WarningShot }, enemySpareCards: 2);

            Assert.That(drive.TurnEnded, Is.True);
            Assert.That(drive.Round.CurrentTurnAuthority?.PendingEffectChoice, Is.Null);
            Assert.That(drive.PauseNodes.Count(node => node.EndsWith("-select")), Is.EqualTo(2),
                "Each face asked its selection exactly once - a checkpoint resumed twice would ask " +
                "one of them again.");
        }

        /// <summary>
        /// The same continuation at a checkpoint that is not the turn's close. Two occurrences carrying
        /// the same two bodies declare rulebook p.8's Action Ended instead, and the pipeline does the
        /// same thing there - because the continuation belongs to the candidate loop rather than to any
        /// one moment.
        /// </summary>
        [Test]
        public void ACheckpointThatIsNotTheTurnsCloseContinuesTheSameWay()
        {
            var drive = Drive(
                new[] { Standing.SentinelAtActionEnded, Standing.WarningShotAtActionEnded },
                enemySpareCards: 2);

            Assert.That(drive.PauseNodes, Is.EqualTo(new[]
                {
                    "sentinel-select", "sentinel-discard", "warning-select", "warning-discard"
                }),
                "Action Ended visits the same authority with the same ordered list.");
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(2));
            Assert.That(drive.TurnEnded, Is.True);
        }

        /// <summary>
        /// The continuation survives a snapshot taken while the first candidate is still asking - which
        /// it does without a byte being added to the payload, by regenerating the remainder from the
        /// paused frame's own identity and the lifecycle it is restored beside.
        ///
        /// <para>Both worlds are then run to the end and serialized, and the two payloads are compared:
        /// a restored match that dropped the second candidate would hold one more card in the enemy's
        /// hand and could not match.</para>
        /// </summary>
        [Test]
        public void ASnapshotTakenAtTheFirstPauseRestoresAndTheSecondStillRuns()
        {
            var drive = Drive(
                new[] { Standing.TreetopSentinel, Standing.WarningShot },
                enemySpareCards: 2,
                stopAtNode: "sentinel-select");

            var standing = drive.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null);
            Assert.That(standing.NodeId.Value, Is.EqualTo("sentinel-select"));

            var payload = AuthoritativeSnapshotSerializer.Serialize(drive.Round, drive.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                drive.Catalogs, payload, drive.Definitions,
                new[] { drive.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());

            var restoredNodes = RunToTheEnd(restored.Round);
            Assert.That(restoredNodes, Is.EqualTo(new[]
                {
                    "sentinel-select", "sentinel-discard", "warning-select", "warning-discard"
                }),
                "The restored world finishes the question it was standing on and then runs the " +
                "candidate behind it, regenerated from the lifecycle - and starts neither twice.");

            var liveNodes = RunToTheEnd(drive.Round);
            Assert.That(restoredNodes, Is.EqualTo(liveNodes),
                "A regenerated remainder asks exactly what the live one asks.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(drive.Round, drive.Board)));
        }

        /// <summary>
        /// An undo taken across the continuation. The rewind is a restore of an earlier payload, so
        /// what this measures is that a world rewound to the first pause and replayed lands on exactly
        /// the bytes the world that never rewound landed on - both candidates run in both.
        /// </summary>
        [Test]
        public void AnUndoAcrossTheContinuationReplaysToTheSameState()
        {
            var rewound = Drive(
                new[] { Standing.TreetopSentinel, Standing.WarningShot },
                enemySpareCards: 2,
                stopAtNode: "sentinel-select");
            var beforeTheAnswer = AuthoritativeSnapshotSerializer.Serialize(
                rewound.Round, rewound.Board);
            RunToTheEnd(rewound.Round);
            var straightThrough = AuthoritativeSnapshotSerializer.Serialize(
                rewound.Round, rewound.Board);

            var undone = AuthoritativeSnapshotSerializer.Deserialize(
                rewound.Catalogs, beforeTheAnswer, rewound.Definitions,
                new[] { rewound.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(undone.Round.CurrentTurnAuthority.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("sentinel-select"),
                "The rewind lands back on the question the first candidate was asking.");

            RunToTheEnd(undone.Round);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(undone.Round, undone.Board),
                Is.EqualTo(straightThrough),
                "and replaying from there reaches the same match, second candidate and all.");
        }

        // -----------------------------------------------------------------------------------------
        // The drive.
        // -----------------------------------------------------------------------------------------

        private enum Standing
        {
            TreetopSentinel,
            WarningShot,
            QuietWildHunt,
            SentinelAtActionEnded,
            WarningShotAtActionEnded
        }

        private sealed class DriveResult
        {
            internal RoundResolutionAuthority Round;
            internal AdjacentMoveAuthority Board;
            internal MatchContentCatalogs Catalogs;
            internal CardDefinition[] Definitions;
            internal CardInstance[] Instances;
            internal ActiveEffectLifecycle Timing;
            internal readonly List<string> PauseNodes = new List<string>();
            internal int EnemyHandBefore;
            internal bool TurnEnded;

            internal int EnemyHandAfter => Instances.Count(card =>
                card.OwnerId.Equals(Enemy) && card.State.Location == CardLocation.Hand);

            internal HexCoordinate SilverarrowSpace => Board.Occupancies
                .Single(hero => hero.HeroId.Equals(Silverarrow)).Coordinate;

            internal IEnumerable<ActiveEffectPhase> EffectPhases =>
                Timing.Effects.Select(effect => effect.Phase);
        }

        /// <summary>
        /// One turn with the named occurrences standing. Composed by hand for the reason
        /// <c>SilverarrowEndOfTurnDriveTests</c> documents - a staged configuration cannot name her
        /// deferred contracts - and through a round rather than a turn so a snapshot can be taken.
        /// </summary>
        private static DriveResult Drive(
            IReadOnlyList<Standing> standing,
            int enemySpareCards,
            string stopAtNode = null)
        {
            var participants = StagedBoardParticipants.Create();
            var warningShot = SilverarrowCardContent.Definition(SilverarrowCardContent.WarningShot);
            var sentinel = SilverarrowCardContent.Definition(SilverarrowCardContent.TreetopSentinel);
            var warningShotSkill = warningShot.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);
            var sentinelSkill = sentinel.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);

            var silverarrowCommit = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-commit"), warningShot.Reference, Silverarrow);
            var warningShotSource = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-warning-shot-source"), warningShot.Reference,
                Silverarrow);
            var sentinelSource = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-treetop-sentinel-source"), sentinel.Reference,
                Silverarrow);
            var enemyCards = new List<CardInstance>
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("enemy-commit"), warningShot.Reference, Enemy)
            };
            for (var spare = 0; spare < enemySpareCards; spare++)
            {
                enemyCards.Add(CardInstance.CreateInHand(
                    new CardInstanceId("enemy-spare-" + spare), warningShot.Reference, Enemy));
            }

            var instances = new[] { silverarrowCommit, warningShotSource, sentinelSource }
                .Concat(enemyCards)
                .ToArray();
            var definitions = new[] { warningShot, sentinel };

            // A straight line of spaces, which is what makes Wild Hunt's "2 spaces in a straight line"
            // offer empty while both radius selections still reach the enemy hero.
            var spaces = Enumerable.Range(0, 6).Select(At).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("triggered-continuation-map"), 1, spaces);
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Silverarrow, At(0)),
                    new HeroOccupancy(Enemy, At(2))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            var build = MatchContentCatalogs.ForThisBuild();
            var capabilities = new EffectCapabilityCatalog(
                build.Capabilities.Registered.Select(build.Capabilities.Load)
                    .Concat(SilverarrowActiveEffectContent.Handlers())
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
            var catalogs = new MatchContentCatalogs(
                capabilities,
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(
                    SilverarrowEffectContent.TriggeredEffects(
                        SilverarrowCardContent.ContentRevision)));

            var markers = new MarkerAttachmentRegister();
            var timing = new ActiveEffectLifecycle();
            for (var index = 0; index < standing.Count; index++)
            {
                var occurrence = standing[index];
                timing.Create(
                    new ActiveEffectId("continuation-" + index + "-" + occurrence),
                    DefinitionFor(occurrence),
                    Silverarrow,
                    TeamSide.Red,
                    Silverarrow,
                    occurrence == Standing.TreetopSentinel ||
                    occurrence == Standing.SentinelAtActionEnded
                        ? sentinelSource.Id
                        : warningShotSource.Id,
                    occurrence == Standing.TreetopSentinel ||
                    occurrence == Standing.SentinelAtActionEnded
                        ? sentinelSkill.Id
                        : warningShotSkill.Id,
                    1,
                    1);
            }

            var result = new DriveResult
            {
                Board = board,
                Catalogs = catalogs,
                Definitions = definitions,
                Instances = instances,
                Timing = timing
            };

            var round = new RoundResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForMatch(
                    new CardActionResolutionAuthority(catalogs, board, markers),
                    board, timing, markers, null, null,
                    startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
            result.Round = round;

            Assert.That(round.TryCommitCard(Silverarrow, silverarrowCommit.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Enemy, enemyCards[0].Id).Accepted, Is.True);
            result.EnemyHandBefore = instances.Count(card =>
                card.OwnerId.Equals(Enemy) && card.State.Location == CardLocation.Hand);

            Advance(round, result.PauseNodes, stopAtNode, preferredActor: silverarrowCommit.Id);
            result.TurnEnded = round.CurrentTurnAuthority == null ||
                round.CurrentTurnAuthority.IsEnded;
            return result;
        }

        /// <summary>Runs a round on to its turn's close, answering whatever it asks, and reports the
        /// questions it asked.</summary>
        private static List<string> RunToTheEnd(RoundResolutionAuthority round)
        {
            var nodes = new List<string>();
            Advance(round, nodes, null, default);
            return nodes;
        }

        private static void Advance(
            RoundResolutionAuthority round,
            List<string> nodes,
            string stopAtNode,
            CardInstanceId preferredActor)
        {
            for (var step = 0; step < 60; step++)
            {
                var turn = round.CurrentTurnAuthority;
                if (turn == null || turn.IsEnded) return;

                if (turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(round.TryChooseTiedActor(
                            tie.OwningSide,
                            preferredActor.IsDefined &&
                            tie.CandidateCardInstanceIds.Contains(preferredActor)
                                ? preferredActor
                                : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                    continue;
                }

                var choice = turn.PendingEffectChoice;
                if (choice != null)
                {
                    nodes.Add(choice.NodeId.Value);
                    if (choice.NodeId.Value == stopAtNode) return;
                    Assert.That(round.TryAnswerEffectChoice(
                            choice.SelectInvocations.Count > 0
                                ? choice.SelectInvocations[0]
                                : choice.ConfirmInvocation).Accepted,
                        Is.True,
                        "A triggered question must be answerable at the moment it is asked.");
                    continue;
                }

                if (turn.PendingTurnRelease != null)
                {
                    Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                    continue;
                }

                if (!turn.SelectedActor.CardInstanceId.IsDefined) return;
                Assert.That(
                    round.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
            }
        }

        // -----------------------------------------------------------------------------------------
        // The occurrences this file stands.
        // -----------------------------------------------------------------------------------------

        private static ActiveEffectDefinition DefinitionFor(Standing standing)
        {
            switch (standing)
            {
                case Standing.TreetopSentinel:
                    return SilverarrowActiveEffectContent.TreetopSentinelDefinition;
                case Standing.WarningShot:
                    return SilverarrowActiveEffectContent.WarningShotDefinition;
                case Standing.QuietWildHunt:
                    return QuietWildHuntDefinition;
                case Standing.SentinelAtActionEnded:
                    return SentinelAtActionEndedDefinition;
                case Standing.WarningShotAtActionEnded:
                    return WarningShotAtActionEndedDefinition;
                default:
                    throw new ArgumentOutOfRangeException(nameof(standing));
            }
        }

        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated
        };

        /// <summary>
        /// A third end-of-turn occurrence whose body is Silverarrow's Wild Hunt - a straight-line walk
        /// of exactly two spaces the hero may decline. It is composed here rather than taken from her
        /// content because no shipped card stands Wild Hunt at the turn's close; what this file needs
        /// is a candidate whose printed work finds nothing to do, and hers is the one body in the build
        /// whose emptiness is a fact about the BOARD rather than about a hand.
        /// </summary>
        private static ActiveEffectDefinition QuietWildHuntDefinition { get; } =
            new ActiveEffectDefinition(
                "triggered-continuation-quiet-wild-hunt",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                Cancellations,
                false,
                "TriggeredContinuationQuietWildHunt",
                "triggered-continuation-quiet-wild-hunt",
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: SilverarrowEffectContent
                    .WildHuntTriggeredEffect(SilverarrowCardContent.ContentRevision).Reference);

        private static ActiveEffectDefinition SentinelAtActionEndedDefinition { get; } =
            AtActionEnded(
                "triggered-continuation-sentinel-action-ended",
                SilverarrowEffectContent.TreetopSentinelTriggeredEffect.Reference);

        private static ActiveEffectDefinition WarningShotAtActionEndedDefinition { get; } =
            AtActionEnded(
                "triggered-continuation-warning-shot-action-ended",
                SilverarrowEffectContent.WarningShotTriggeredEffect.Reference);

        /// <summary>
        /// The same two bodies declared at rulebook p.8's Action Ended, which is what makes
        /// <see cref="ACheckpointThatIsNotTheTurnsCloseContinuesTheSameWay"/> a statement about the
        /// pipeline rather than about the turn's close.
        /// </summary>
        private static ActiveEffectDefinition AtActionEnded(
            string definitionId, EffectDefinitionReference triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                definitionId,
                definitionId,
                new[] { TimingCheckpoint.ActionEnded },
                triggeredEffect: triggeredEffect);
    }
}

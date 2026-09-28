using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.115. Whisper Rulings 16a-16b (user, 2026-09-28): Swift Justice's step-7 rider makes
    /// the defending hero perform a Movement ACTION "as specified" - exactly the defended card's
    /// Movement in one straight line, or nothing ("If able"). A direction stopped short is not
    /// offered; a Movement restriction or a Movement limit below the full distance prevents the move.
    /// </summary>
    public sealed class WhisperForcedMoveRulingTests
    {
        private static readonly HeroId Whisper = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("whisper-forced-move");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        /// <summary>The defender stands two from Whisper, inside Swift Justice's Range 3.</summary>
        private static readonly HexCoordinate EnemyAt = H(0, 2);

        [Test]
        public void OnAnOpenBoard_EverySixDirectionsFullThreeIsOffered_AndNothingShorter()
        {
            var round = new Round();
            round.AttackAndDefend();

            var offered = round.OfferedSpaces();
            Assert.That(round.Chooser, Is.EqualTo(Enemy), "Ruling 16: the defender's own seat steers.");
            Assert.That(offered, Has.All.Matches<HexCoordinate>(space =>
                HexDistanceQuery.Calculate(space, EnemyAt) == 3),
                "the defended card prints Movement 3: exactly three, never fewer (Ruling 16a).");
        }

        [Test]
        public void ADirectionStoppedShort_IsNotOffered()
        {
            // Whisper stands at (0,0), two west-north of the defender; a minion two spaces east of the
            // defender stops that line at one space.
            var round = new Round(minionsAt: new[] { H(2, 2) });
            round.AttackAndDefend();

            Assert.That(round.OfferedSpaces(), Has.No.Member(H(1, 2)),
                "before 8.115 the furthest space a blocked direction reached (one) was offered; now a " +
                "direction that cannot go the full three is not a destination at all.");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(3, 2)));
        }

        [Test]
        public void WhenNoDirectionReachesTheFullDistance_TheDefenderDoesNotMove()
        {
            // The defender sits in a corner pocket: every line out is stopped before three.
            var round = new Round(minionsAt: new[] { H(1, 2), H(-1, 2), H(0, 3), H(-1, 3), H(1, 1) });
            round.AttackAndDefend();

            Assert.That(round.PendingNode, Is.Null, "\"If able\": no full-distance line, no question.");
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(EnemyAt), "and nobody moved.");
        }

        [Test]
        public void ARestrictedDefender_CannotPerformTheMovementAction()
        {
            var round = new Round(standingOnEnemy: RuleModification.ForRestriction(RuleActivity.Movement));
            round.AttackAndDefend();

            Assert.That(round.PendingNode, Is.Null,
                "Ruling 16b: it is the defender's Movement action, so a standing Movement restriction " +
                "(a petrification) prevents it.");
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(EnemyAt));
        }

        [Test]
        public void AMovementLimitBelowTheFullDistance_PreventsIt()
        {
            var round = new Round(standingOnEnemy: RuleModification.ForActivityLimit(RuleActivity.Movement, 2));
            round.AttackAndDefend();

            Assert.That(round.PendingNode, Is.Null,
                "a Movement action limited to two cannot move the full three, so it is not able.");
            Assert.That(round.SpaceOf(Enemy), Is.EqualTo(EnemyAt));
        }

        [Test]
        public void AMovementScopedObstaclePermission_LetsTheForcedMoveCross()
        {
            var blocked = new Round(minionsAt: new[] { H(1, 2) });
            blocked.AttackAndDefend();
            Assert.That(blocked.OfferedSpaces(), Has.No.Member(H(3, 2)), "the minion stops the eastward line.");

            var permitted = new Round(minionsAt: new[] { H(1, 2) },
                standingOnEnemy: RuleModification.ForPermission(
                    RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement));
            permitted.AttackAndDefend();
            Assert.That(permitted.OfferedSpaces(), Has.Member(H(3, 2)),
                "Ruling 16b: the forced move is a Movement action, so a permission scoped to Movement " +
                "actions (RULING 47) is read and the line crosses the minion.");
        }

        // --- harness ------------------------------------------------------------------------------

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("whisper-forced-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        /// <summary>Defense 9 so the defender survives, and a printed Movement 3 for the rider to read.</summary>
        private static readonly CardDefinition EnemyGuard = new CardDefinition(
            new CardDefinitionId("whisper-forced-guard"), "rev-1", "TEST - Guard",
            new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)),
                new ActionDefinition(new ActionDefinitionId("secondary-movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private sealed class Round
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;

            internal Round(IReadOnlyList<HexCoordinate> minionsAt = null, RuleModification standingOnEnemy = null)
            {
                var participants = StagedBoardParticipants.Create();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                var minions = (minionsAt ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"whisper-forced-minion-{index}"),
                        new TeamId(StagedBoardParticipants.RedTeamIdValue), TeamSide.Red,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId($"spawn-{index}")),
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                        at))
                    .ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces),
                    new[] { new HeroOccupancy(Whisper, H(0, 0)), new HeroOccupancy(Enemy, EnemyAt) },
                    Array.Empty<ObstacleOccupancy>(), minions);

                var face = WhisperCardContent.Definition(WhisperCardContent.SwiftJustice);
                source = CardInstance.CreateInHand(new CardInstanceId("whisper-source"), face.Reference, Whisper);
                var hold = CardInstance.CreateInHand(new CardInstanceId("enemy-hold"), EnemyHold.Reference, Enemy);
                var guard = CardInstance.CreateInHand(new CardInstanceId("enemy-guard"), EnemyGuard.Reference, Enemy);
                definitions = new[] { face, EnemyHold, EnemyGuard };
                instances = new[] { source, hold, guard };
                var catalogs = MatchContentCatalogs.ForThisBuild();
                var markers = new MarkerAttachmentRegister();
                var timing = new ActiveEffectLifecycle();
                if (standingOnEnemy != null)
                {
                    timing.Create(new ActiveEffectId("forced-move-standing-rule"),
                        StandingRule(standingOnEnemy), Enemy, TeamSide.Blue, Enemy,
                        new CardInstanceId("enemy-earlier"), new ActionDefinitionId("earlier"), 1, 1);
                }

                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(
                        new CardActionResolutionAuthority(catalogs, board, markers),
                        board, timing, markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Whisper, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, hold.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Whisper));
            }

            private static ActiveEffectDefinition StandingRule(RuleModification rule) =>
                new ActiveEffectDefinition(
                    "whisper-forced-standing-rule",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.ThisRound,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.RoundEnd,
                    new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                    false,
                    "WhisperForcedStandingRule",
                    "whisper-forced-standing-rule",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { rule });

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal HeroId Chooser => Turn.PendingEffectChoice.ChooserId;

            internal HexCoordinate SpaceOf(HeroId hero)
            {
                Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out var at), Is.True);
                return at;
            }

            internal HexCoordinate[] OfferedSpaces()
            {
                Assert.That(Turn.PendingEffectChoice, Is.Not.Null, "the forced move asks where");
                return Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();
            }

            /// <summary>Swift Justice's ranged-hero bullet at the enemy, answered with the Guard.</summary>
            internal void AttackAndDefend()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, Whisper, source.Id)
                    .Single(value => value.ActionDefinitionId.Value == "primary-ranged-attack");
                Assert.That(round.TryStartAttack(new ActionResolutionId("whisper-attack"), offer).Accepted, Is.True);

                var modal = Turn.PendingEffectChoice;
                Assert.That(modal, Is.Not.Null, "Swift Justice opens its modal");
                Assert.That(round.TryAnswerEffectChoice(modal.SelectInvocations.Single(command =>
                    command.Candidate.OptionNodeId.Value == "modal-ranged-hero")).Accepted, Is.True);
                var still = Turn.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(modal.ChoiceId) && still.ConfirmInvocation != null)
                {
                    Assert.That(round.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                }

                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "p.14 asks whom");
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    target.ResolutionId, target.ChoiceId, Enemy)).Accepted, Is.True);

                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null, "the enemy may defend");
                var guard = defense.LegalDefenseOffers.First(value =>
                    value.SourceDefinitionReference.Equals(EnemyGuard.Reference));
                Assert.That(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, guard.SourceCardInstanceId)).Accepted, Is.True);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.SilverarrowRangeBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z30. The rules Silverarrow's set needed, measured on a board rather
    /// than read back off the declarations that name them.
    ///
    /// <para><see cref="SilverarrowPrintedSetTests"/> asserts that each ruling reached the composition.
    /// This file asserts that the composition then behaves as the ruling says - which is the difference
    /// the 2026-09-06 corrective work kept finding: a rule recorded in a document, declared in a
    /// definition, and never wired to a query that answers it.</para>
    /// </summary>
    public sealed class SilverarrowCapabilityTests
    {
        private static readonly PrintedAttackTargetPolicy RangedDefault =
            PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged);

        [TestCase(1, true)]
        [TestCase(4, true)]
        [TestCase(5, false)]
        public void ActiveTrailblazer_DerivesSelfAndOnlyFriendlyHeroesInsideItsLiveRadius(int distance, bool affected)
        {
            var board = CreateBoard(new[] { (Silverarrow, Origin), (Ally, At(distance)), (Enemy, AtOtherFile(1)) },
                new[] { ("friendly-minion", RedTeam, AtOtherFile(2)), ("enemy-minion", BlueTeam, AtOtherFile(3)) });
            var lifecycle = new ActiveEffectLifecycle();
            Create(lifecycle, SilverarrowActiveEffectContent.TrailblazerDefinition);
            var rules = RuleModificationQuery.Build(lifecycle, board, CreateParticipants(),
                new TieBreakerState(TeamSide.Red), GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None,
                RuleObject.None, null, null);
            Assert.That(rules.HasPermission(RuleObject.ForHero(Silverarrow), RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement), Is.True);
            Assert.That(rules.HasPermission(RuleObject.ForHero(Ally), RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement), Is.EqualTo(affected));
            Assert.That(rules.HasPermission(RuleObject.ForHero(Enemy), RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement), Is.False);
            foreach (var minion in board.Minions)
                Assert.That(rules.HasPermission(RuleObject.ForMinion(minion.MinionId), RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement), Is.False);
            Assert.That(rules.HasPermission(RuleObject.ForHero(Silverarrow), RulePermissionKind.MoveThroughObstacles, RuleActivity.DirectMove), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // RULING 1 - "at maximum range" is a band, not a ceiling
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The ruling, measured: an enemy at exactly the Range Total is a legal target and the same
        /// enemy one space nearer is not. Under the reading RULING 1 rejected, both would be.
        /// </summary>
        [Test]
        public void Ruling1_AtMaximumRange_AdmitsTheRingAndRefusesEverythingInsideIt()
        {
            for (var distance = 1; distance <= 3; distance++)
            {
                var board = CreateBoard(new[]
                {
                    (Silverarrow, Origin),
                    (Enemy, At(distance))
                });

                var band = Targets(board, RangedDefault.AtExactlyThePrintedRange(), rangeTotal: 3);
                var ceiling = Targets(board, RangedDefault, rangeTotal: 3);

                Assert.That(ceiling.Count, Is.EqualTo(1),
                    $"p.13's ceiling reaches an enemy at {distance} with a Range Total of 3.");
                Assert.That(band.Count, Is.EqualTo(distance == 3 ? 1 : 0),
                    $"\"at maximum range\" reaches an enemy at exactly 3, and no enemy at {distance}.");
            }
        }

        /// <summary>
        /// The consequence the inventory flagged as the reason SL-01 was worth answering first: both of
        /// her Gold basics become unable to answer an enemy who has closed with her. It is asserted
        /// because it is the whole of her play pattern, not an accident of one board.
        /// </summary>
        [Test]
        public void Ruling1_AnEnemyAdjacentToHer_IsNotATargetOfEitherGoldBasic()
        {
            var board = CreateBoard(new[] { (Silverarrow, Origin), (Enemy, At(1)) });

            Assert.That(
                Targets(board, RangedDefault.AtExactlyThePrintedRange(), rangeTotal: 2),
                Is.Empty,
                "Shoot and Scoot prints Range 2 and \"at maximum range\", so a unit beside her is out " +
                "of the band entirely.");
            Assert.That(
                Targets(
                    board,
                    RangedDefault.NarrowedToHeroes().AtExactlyThePrintedRange(),
                    rangeTotal: 2),
                Is.Empty,
                "And Keep the Distance narrows further, to heroes, which is what its name describes.");
        }

        /// <summary>
        /// p.16's Range item moves the band outward rather than widening it - the observable difference
        /// between a band and a ceiling, and the reason the reach reads the composed Range Total rather
        /// than the printed icon.
        /// </summary>
        [Test]
        public void Ruling1_ARangeItem_MovesTheBandOutwardRatherThanWideningIt()
        {
            var board = CreateBoard(new[]
            {
                (Silverarrow, Origin),
                (Enemy, At(2)),
                (SecondEnemy, At(3))
            });
            var band = RangedDefault.AtExactlyThePrintedRange();

            Assert.That(
                Targets(board, band, rangeTotal: 2).Select(target => target.HeroId),
                Is.EquivalentTo(new[] { Enemy }));
            Assert.That(
                Targets(board, band, rangeTotal: 3).Select(target => target.HeroId),
                Is.EquivalentTo(new[] { SecondEnemy }),
                "A +1 Range item leaves the nearer enemy behind rather than keeping both.");
        }

        // -----------------------------------------------------------------------------------------
        // The Red modals' union, and RULING 39
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The union both readings of "any other unit" produce, measured against a board built to
        /// separate them: an isolated enemy far out, an enemy with a neighbour far out, and an enemy
        /// standing beside her.
        /// </summary>
        [Test]
        public void TheRedModal_AdmitsIsolatedUnitsAndUnitsBesideHer_AndNothingElse()
        {
            var board = CreateBoard(
                new[]
                {
                    (Silverarrow, Origin),
                    (Enemy, At(3)),
                    (SecondEnemy, At(1))
                },
                new[]
                {
                    // A friendly minion standing beside the far enemy is what takes that enemy out of
                    // the first bullet: "any other unit" is p.19's unit of either side.
                    ("modal-neighbour", RedTeam, new HexCoordinate(4, 0))
                });

            var modal = Targets(
                board, RangedDefault.NarrowedToIsolatedUnitsAndThoseAdjacentToYou(), rangeTotal: 4);

            Assert.That(
                modal.Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId),
                Is.EquivalentTo(new[] { SecondEnemy }),
                "The enemy beside her is admitted by the second bullet; the enemy with a neighbour is " +
                "admitted by neither.");
        }

        /// <summary>
        /// The equality the composition rests on, asserted directly: the union is the same set whether
        /// or not the attacker counts as "any other unit". It is the reason one candidate set states
        /// both printed bullets and the reason the face's silence needed no ruling.
        /// </summary>
        [Test]
        public void TheRedModal_IsTheSameSetUnderBothReadingsOfAnyOtherUnit()
        {
            foreach (var enemyDistance in new[] { 1, 2, 3, 4 })
            {
                var board = CreateBoard(new[]
                {
                    (Silverarrow, Origin),
                    (Enemy, At(enemyDistance))
                });

                var measured = Targets(
                    board,
                    RangedDefault.NarrowedToIsolatedUnitsAndThoseAdjacentToYou(),
                    rangeTotal: 4)
                    .Select(target => target.HeroId)
                    .ToArray();

                // The reading in which the attacker is not a unit: an enemy is admitted when it stands
                // beside her, or when no unit other than her stands beside it. On this board the enemy
                // is alone, so every distance qualifies under that reading.
                Assert.That(measured, Is.EquivalentTo(new[] { Enemy }),
                    $"An isolated enemy at {enemyDistance} is admitted under either reading.");
            }

            // And the case that separates the two readings if anything does: an enemy beside her whose
            // only neighbour is her. Bullet two admits it regardless, which is what makes the union
            // invariant.
            var adjacent = CreateBoard(new[] { (Silverarrow, Origin), (Enemy, At(1)) });
            Assert.That(
                Targets(
                    adjacent,
                    RangedDefault.NarrowedToIsolatedUnitsAndThoseAdjacentToYou(),
                    rangeTotal: 4).Select(target => target.HeroId),
                Is.EquivalentTo(new[] { Enemy }));
        }

        /// <summary>
        /// RULING 39, which the composition satisfies by construction rather than by a rule: the
        /// adjacent bullet does not change the action's type, because the bullets are not actions. The
        /// card declares <see cref="AttackRangeKind.Ranged"/> and the policy only says which units it
        /// reaches, so a point-blank Snap Shot is still a ranged attack for a Block to answer.
        /// </summary>
        [Test]
        public void Ruling39_TheAdjacentBulletIsStillARangedAttack()
        {
            var snapShot = SilverarrowCardContent.Definition(SilverarrowCardContent.SnapShot);
            var attack = snapShot.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

            Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(
                attack.AttackTargetPolicy.Value.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou));
            Assert.That(
                attack.AttackTargetPolicy.Value.Reach,
                Is.EqualTo(AttackTargetReach.PrintedRange),
                "The first bullet says \"in range\", which is p.13's ceiling and not RULING 1's band.");
        }

        /// <summary>
        /// The refusal that keeps the union writable only where a face prints it: the modal's first
        /// bullet says "a unit in range", so pairing it with adjacency-only or with RULING 1's band is
        /// a sentence no face has and this file does not get to invent one.
        /// </summary>
        [Test]
        public void TheRedModal_IsRefusedOverAnyReachButThePrintedCeiling()
        {
            Assert.Throws<ArgumentException>(() => new PrintedAttackTargetPolicy(
                AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou,
                AttackTargetReach.ExactlyPrintedRange));
            Assert.Throws<ArgumentException>(() => new PrintedAttackTargetPolicy(
                AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou,
                AttackTargetReach.AdjacentOnly));
        }

        /// <summary>
        /// p.13's own refusal, extended to the new reach: an Attack that reaches a printed Range prints
        /// one, whichever way it reads it.
        /// </summary>
        [Test]
        public void TheExactBand_RequiresThePrintedRangeItReads()
        {
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("bandless"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged,
                default,
                null,
                default,
                PrintedDefenseResolution.None,
                CardTextTiming.None,
                RangedDefault.AtExactlyThePrintedRange()));
        }

        // -----------------------------------------------------------------------------------------
        // RULING 47 - a movement-action permission does not reach a card-text Move
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The ruling, measured in both directions on one board. An obstacle sits beside the hero; the
        /// question is whether she may walk through it.
        ///
        /// <para>Trailblazer's permission is scoped to movement actions, so the Movement action's own
        /// reachability crosses and the card-text direct Move does not. Xargatha's unscoped grant
        /// crosses both, which is the control that proves the scope is doing the work rather than the
        /// plumbing simply having stopped answering.</para>
        /// </summary>
        [Test]
        public void Ruling47_TheScopedPermission_ReachesAMovementActionAndNotACardTextMove()
        {
            var blocker = At(1);
            var beyond = At(2);
            var board = CreateBoard(new[] { (Silverarrow, Origin), (Enemy, blocker) });
            var subject = RuleObject.ForHero(Silverarrow);

            var scoped = Modifications(
                subject,
                RuleModification.ForPermission(
                    RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement));
            var unscoped = Modifications(
                subject, RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles));

            Assert.That(MovementActionReaches(board, beyond, scoped), Is.True,
                "Trailblazer's grant is what a Movement action asks for.");
            Assert.That(CardTextMoveReaches(board, beyond, scoped), Is.False,
                "RULING 47: the same grant does not reach a Move written in card text.");

            Assert.That(MovementActionReaches(board, beyond, unscoped), Is.True);
            Assert.That(CardTextMoveReaches(board, beyond, unscoped), Is.True,
                "Xargatha's Metamorphosis prints no scope, so it still answers every ask - which is " +
                "the control that says the scope decided the case above.");

            Assert.That(
                MovementActionReaches(board, beyond, RuleModificationSet.Empty), Is.False,
                "And with no permission at all, neither walk crosses.");
            Assert.That(CardTextMoveReaches(board, beyond, RuleModificationSet.Empty), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // The End-of-turn duration
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The ordering this milestone changed, measured on the lifecycle rather than assumed from the
        /// code: an effect whose trigger checkpoint is also its expiry fires <em>and then</em> ends.
        /// Before the change it expired instead, which would have made both "End of turn:" faces silent
        /// at the only moment they name.
        /// </summary>
        [Test]
        public void EndOfTurn_FiresAtTheTurnsEndAndIsThenOver()
        {
            var lifecycle = new ActiveEffectLifecycle();
            var instance = Create(lifecycle, SilverarrowActiveEffectContent.WarningShotDefinition);

            var events = lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnEnded, 1, 1),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                events.Where(value => value.EffectId.Equals(instance)).Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    ActiveEffectLifecycleEventKind.Triggered,
                    ActiveEffectLifecycleEventKind.Expired
                }),
                "The trigger is raised before the expiry, and both happen at the turn's end.");
        }

        /// <summary>
        /// The second visit produces nothing, which is what makes the turn's close safe to resume after
        /// an answered question: the effect ended the first time, so no work can happen twice.
        /// </summary>
        [Test]
        public void EndOfTurn_DoesNotFireASecondTimeWhenTheCheckpointIsVisitedAgain()
        {
            var lifecycle = new ActiveEffectLifecycle();
            var instance = Create(lifecycle, SilverarrowActiveEffectContent.TreetopSentinelDefinition);
            var tieBreaker = new TieBreakerState(TeamSide.Red);

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnEnded, 1, 1), tieBreaker);
            var second = lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnEnded, 1, 1), tieBreaker);

            Assert.That(second.Where(value => value.EffectId.Equals(instance)), Is.Empty);
        }

        /// <summary>
        /// No effect already in this build declares one checkpoint as both a trigger and an expiry, so
        /// the ordering change above is invisible to every shipped face. Asserted rather than left true
        /// by accident, because the next face that pairs them would silently change meaning.
        /// </summary>
        [Test]
        public void NoShippedActiveEffect_TriggersAtItsOwnExpiryCheckpoint()
        {
            var shipped = WaspActiveEffectContent.Definitions
                .Concat(ArienActiveEffectContent.Definitions)
                .Concat(BroganActiveEffectContent.Definitions)
                .Concat(XargathaActiveEffectContent.Definitions)
                .Concat(new[] { TigerclawActiveEffectContent.ShadowedDefinition })
                .ToArray();

            Assert.That(shipped, Is.Not.Empty);
            Assert.That(
                shipped.Where(definition =>
                    definition.TriggerCheckpoints.Contains(definition.ExpirationCheckpoint)),
                Is.Empty);
        }

        /// <summary>
        /// The duration refuses every shape but its own, so a later face cannot declare the keyword and
        /// quietly put its work somewhere else.
        /// </summary>
        [Test]
        public void EndOfTurn_RefusesAnExpiryThatIsNotTheTurnsEnd()
        {
            Assert.Throws<ArgumentException>(() => new ActiveEffectDefinition(
                "silverarrow-bad-end-of-turn",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                false,
                "SilverarrowBad",
                "silverarrow-bad",
                new[] { TimingCheckpoint.TurnEnded }));
        }

        // -----------------------------------------------------------------------------------------
        // Wild Hunt's before-action declaration
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Each time before you perform an action": the declaration fires before all three kinds a
        /// hero performs from a card, and is never written into the turn's spent-sources record.
        /// Arien's Living Tsunami is the control on both counts.
        /// </summary>
        [Test]
        public void WildHunt_FiresBeforeEveryActionKindAndIsNeverConsumed()
        {
            var wildHunt = new BeforeActionTrigger(
                ActionKind.None, BeforeActionTriggerFrequency.EveryAction);
            var livingTsunami = new BeforeActionTrigger(
                ActionKind.Attack, BeforeActionTriggerFrequency.OncePerTurn);

            foreach (var kind in new[] { ActionKind.Attack, ActionKind.Movement, ActionKind.Skill })
            {
                Assert.That(wildHunt.FiresBefore(kind), Is.True, $"Wild Hunt fires before a {kind}.");
            }

            Assert.That(livingTsunami.FiresBefore(ActionKind.Attack), Is.True);
            Assert.That(livingTsunami.FiresBefore(ActionKind.Skill), Is.False,
                "Living Tsunami names one kind, and naming one is what excludes the others.");

            Assert.That(wildHunt.IsConsumedWhenItFires(), Is.False);
            Assert.That(livingTsunami.IsConsumedWhenItFires(), Is.True);
        }

        /// <summary>
        /// The pairing is exact in both directions, so each printed sentence has one spelling and no
        /// combination is left for a reader to interpret.
        /// </summary>
        [Test]
        public void TheBeforeActionDeclaration_PairsEachFrequencyWithItsOwnScope()
        {
            Assert.Throws<ArgumentException>(() => new BeforeActionTrigger(
                ActionKind.Attack, BeforeActionTriggerFrequency.EveryAction));
            Assert.Throws<ArgumentException>(() => new BeforeActionTrigger(
                ActionKind.None, BeforeActionTriggerFrequency.OncePerTurn));
        }

        // -----------------------------------------------------------------------------------------
        // Fast Travel written in card text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The narrowing both Gold basics print, measured against p.11's own reach on one board: the
        /// card-text Fast Travel that says "to an adjacent zone" offers strictly fewer spaces than the
        /// one that says nothing, and the difference is exactly her own Zone.
        /// </summary>
        [Test]
        public void CardTextFastTravel_NarrowsToAnAdjacentZoneWhereTheFacePrintsIt()
        {
            var board = CreateBoard(new[] { (Silverarrow, Origin) });
            var participants = CreateParticipants();

            var wide = FastTravelActionQuery.FindDestinationsForActor(
                Silverarrow, board, board, participants, board.Minions,
                FastTravelZoneScope.SameOrAdjacentZone, RuleModificationSet.Empty);
            var narrow = FastTravelActionQuery.FindDestinationsForActor(
                Silverarrow, board, board, participants, board.Minions,
                FastTravelZoneScope.AdjacentZoneOnly, RuleModificationSet.Empty);

            Assert.That(wide, Is.Not.Empty,
                "p.11 admits the hero's own Zone, and this map is one Zone.");
            Assert.That(narrow, Is.Empty,
                "So a card that says \"to an adjacent zone\" has nowhere to go here - which is the " +
                "\"if able\" both faces print, rather than an error.");
        }

        [Test]
        public void ABoardPartitionBlocksBothCardTextAndReplacementFastTravelAcrossSides()
        {
            var board = CreateBoard(new[] { (Silverarrow, Origin) });
            var participants = CreateParticipants();
            var rules = new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(), Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed,
                Array.Empty<ActionTargetExclusion>(),
                new[] { new BoardPartitionRestriction(
                    new BoardPartitionLine(At(1), BoardPartitionAxis.NorthWestSouthEast),
                    Silverarrow, separatesController: false) },
                RuleObject.ForHero(Silverarrow), Origin);

            var destinations = FastTravelActionQuery.FindDestinationsForActor(
                Silverarrow, board, board, participants, board.Minions,
                FastTravelZoneScope.SameOrAdjacentZone, rules);
            Assert.That(destinations, Has.Member(At(1)),
                "The neutral line is available.");
            Assert.That(destinations, Has.No.Member(At(2)),
                "The opposite side is absent from a card-text destination offer.");
            Assert.That(FastTravelActionQuery.EvaluateDestinationForActor(
                Silverarrow, At(2), board, board, participants, board.Minions,
                FastTravelZoneScope.SameOrAdjacentZone, rules),
                Is.EqualTo(CardActionResolutionRejectionReason.FastTravelDestinationUnavailable),
                "The direct replacement-action authority refuses that same destination.");
        }

        /// <summary>
        /// p.11's other two conditions still hold for a card-text Fast Travel, because it is the same
        /// rule asked by a different caller: an enemy in the Zone being left stops it.
        /// </summary>
        [Test]
        public void CardTextFastTravel_IsStoppedByAnEnemyInTheZoneSheIsLeaving()
        {
            var board = CreateBoard(new[] { (Silverarrow, Origin), (Enemy, At(4)) });

            Assert.That(
                FastTravelActionQuery.FindDestinationsForActor(
                    Silverarrow, board, board, CreateParticipants(), board.Minions,
                    FastTravelZoneScope.SameOrAdjacentZone, RuleModificationSet.Empty),
                Is.Empty);
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        private static IReadOnlyList<AttackTarget> Targets(
            AdjacentMoveAuthority board, PrintedAttackTargetPolicy policy, int rangeTotal) =>
            AttackTargetQuery.FindLegalEnemyTargets(
                Silverarrow, policy, rangeTotal, CreateParticipants(), board,
                RuleModificationSet.Empty);

        private static bool MovementActionReaches(
            AdjacentMoveAuthority board, HexCoordinate destination, RuleModificationSet modifications) =>
            MovementReachabilityQuery
                .FindReachableDestinations(
                    board.Occupancies.Single(value => value.HeroId.Equals(Silverarrow)),
                    board,
                    2,
                    modifications)
                .Any(value => value.Destination.Equals(destination));

        private static bool CardTextMoveReaches(
            AdjacentMoveAuthority board, HexCoordinate destination, RuleModificationSet modifications) =>
            board
                .FindDirectMoveDestinations(
                    GuardsDigital.Core.Components.BoardObjectReference.ForHero(Silverarrow),
                    2,
                    modifications,
                    moveIgnoresObstacles: false)
                .Any(value => value.Destination.Equals(destination));

        private static RuleModificationSet Modifications(
            RuleObject subject, RuleModification modification) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        modification,
                        subject,
                        new ActiveEffectId("silverarrow-permission"),
                        Silverarrow,
                        TeamSide.Red,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                new TieBreakerState(TeamSide.Red));

        private static ActiveEffectId Create(
            ActiveEffectLifecycle lifecycle, ActiveEffectDefinition definition)
        {
            var id = new ActiveEffectId(definition.DefinitionId + "-instance");
            lifecycle.Create(
                id,
                definition,
                Silverarrow,
                TeamSide.Red,
                Silverarrow,
                new CardInstanceId("silverarrow-source-card"),
                new ActionDefinitionId("primary-skill"),
                // Round and turn anchors are one-based; an effect created at nought is refused.
                1,
                1);
            return id;
        }
    }
}

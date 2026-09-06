using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// RULING 48 driven through the handler that implements it, rather than through the query it reads.
    /// Milestone 8.35z34.
    ///
    /// <para><b>Why this fixture exists at all.</b> Milestone 8.35z28 composed Necromancy and
    /// Necromastery and measured them at the definition and query tiers only - there was no case
    /// anywhere that executed <c>synthetic-respawn-friendly-minion-*</c>. RULING 48 gives that handler a
    /// second question, and a second question is exactly the kind of change a definition-tier suite is
    /// structurally blind to: the composition does not move a byte, so every existing Dodger case would
    /// have stayed green whether the type choice was asked, asked wrongly, or never asked at all.</para>
    ///
    /// <para><b>The supply is filled by removing minions, which is the rule under test.</b> p.19 says
    /// "a minion must first be removed, before it can be respawned", so these boards reach their
    /// eligible state the way a match reaches it - a miniature comes off the board and into the reserve
    /// - rather than by being handed a supply. That is what makes the eligibility count and the
    /// available miniature agree for a reason instead of by fixture construction.</para>
    /// </summary>
    [TestFixture]
    public sealed class DodgerRespawnCapabilityTests
    {
        private static readonly HeroId Dodger = new HeroId("dodger");
        private static readonly HeroId Enemy = new HeroId("dodger-enemy");

        private static readonly TeamId RedTeam = new TeamId("dodger-red");
        private static readonly TeamId BlueTeam = new TeamId("dodger-blue");

        /// <summary>
        /// The adjacent arm - Necromancy's, at p.19's innermost ring - which is the one whose reach
        /// needs no card state, so a fixture can drive it without staging a printed Radius.
        ///
        /// <para>Taken from the file's WHOLE vocabulary rather than from either of its two lists,
        /// deliberately: which list holds a contract is a statement about whether a dealt card starts
        /// it, and this fixture's subject is what the handler does. Reading the registered list would
        /// have made these cases fail the moment milestone 8.35z34 moved the declaration across, which
        /// is exactly what reading the deferred list did.</para>
        /// </summary>
        private static IEffectCapabilityHandler Handler() =>
            SyntheticPlacementCapabilities.Handlers()
                .Single(handler => handler.Reference.Equals(
                    SyntheticPlacementCapabilities.RespawnFriendlyMinionAdjacentV1));

        /// <summary>
        /// Pre-Ultimate, the only destinations are the map's own friendly points, and the type is the
        /// point's. This is RULING 12 unchanged, and it is here so that the ruling's change is measured
        /// against a driven baseline rather than only a queried one.
        /// </summary>
        [Test]
        public void WithoutTheUltimate_TheOnlyQuestionIsWhichPrintedPoint()
        {
            var board = BoardWithOneRemovedMelee();
            var context = Context(board, overlay: false);

            var opened = Handler().Execute(context);

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(
                opened.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EquivalentTo(new[]
                {
                    DodgerSpawnPointBoard.MeleeSpawnNear
                }),
                "One Red melee point stands empty inside the Battle Zone at range 1, and p.19 admits " +
                "melee because a melee miniature was removed. The Throne-Zone melee point is Red, " +
                "empty and adjacent to nothing she reaches; the ranged point is in reach and its type " +
                "is not eligible.");

            var applied = Handler().Resume(
                context, new[] { EffectValue.Space(DodgerSpawnPointBoard.MeleeSpawnNear) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A printed point decides what comes back, so answering the destination finishes the " +
                "step - there is no second question to ask.");
            AssertRespawnedAt(applied, DodgerSpawnPointBoard.MeleeSpawnNear);
            Assert.That(
                board.Minions.Any(minion =>
                    minion.Coordinate.Equals(DodgerSpawnPointBoard.MeleeSpawnNear) &&
                    minion.MinionType == StandardMinionType.Melee &&
                    minion.TeamSide == TeamSide.Red),
                Is.True,
                "The miniature is standing on the point it was respawned into.");
        }

        /// <summary>
        /// RULING 48's first half: with the Ultimate up, the destinations are the empty spaces in
        /// reach, not the printed points in reach.
        /// </summary>
        [Test]
        public void WithTheUltimate_TheDestinationsAreEveryEmptySpaceInReach()
        {
            var board = BoardWithOneRemovedMelee();
            var context = Context(board, overlay: true);

            var opened = Handler().Execute(context);

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));

            var offered = opened.Choice.Candidates
                .Select(candidate => candidate.Coordinate)
                .ToArray();
            Assert.That(
                offered, Is.EquivalentTo(Adjacent(DodgerSpawnPointBoard.Origin)
                    .Where(coordinate => !coordinate.Equals(DodgerSpawnPointBoard.RangedSpawnNear))
                    .ToArray()),
                "\"Respawn anywhere in reach\": every empty space at range 1 is offered. The one " +
                "exclusion is the Red RANGED point - a space carrying a printed friendly point is " +
                "governed by that point, and no ranged miniature has been removed, so p.19 refuses it.");
        }

        /// <summary>
        /// RULING 48's second half, and the reason this handler now asks twice: at a granted point the
        /// type comes from p.19's eligibility count, and p.10's default hands the actor the choice when
        /// the page admits more than one.
        /// </summary>
        [Test]
        public void AtAGrantedPoint_TheActorIsAskedWhichEligibleTypeComesBack()
        {
            var board = BoardWithARemovedMeleeAndARemovedRanged();
            var context = Context(board, overlay: true);

            var destination = DodgerSpawnPointBoard.HeroSpawn;
            var second = Handler().Resume(context, new[] { EffectValue.Space(destination) });

            Assert.That(second.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "Two types are eligible and the destination names none, so the node asks its second " +
                "question rather than choosing for the player.");
            Assert.That(second.Choice.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "p.10's default: a decision the card leaves open belongs to whoever performs it.");
            Assert.That(second.Choice.MinimumSelections, Is.EqualTo(1));
            Assert.That(second.Choice.MaximumSelections, Is.EqualTo(1));

            var types = second.Choice.Candidates
                .Select(candidate => board.FindUnitSupply()
                    .Single(minion => minion.MinionId.Equals(candidate.MinionId))
                    .MinionType)
                .ToArray();
            Assert.That(
                types, Is.EquivalentTo(new[] { StandardMinionType.Melee, StandardMinionType.Ranged }),
                "Exactly the types p.19 admits - and no Heavy, which is RULING 12's derived exclusion " +
                "arriving through the arithmetic.");

            var chosen = second.Choice.Candidates.Single(candidate => board.FindUnitSupply()
                .Single(minion => minion.MinionId.Equals(candidate.MinionId))
                .MinionType == StandardMinionType.Ranged);

            var applied = Handler().Resume(
                new EffectCapabilityContext[] { Context(board, overlay: true, answered: destination) }[0],
                new[] { chosen });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            AssertRespawnedAt(applied, destination);
            Assert.That(
                board.Minions.Any(minion =>
                    minion.Coordinate.Equals(destination) &&
                    minion.MinionType == StandardMinionType.Ranged),
                Is.True,
                "The type the actor picked is the type standing on the granted space - a ranged " +
                "miniature on a point the map never printed.");
        }

        /// <summary>
        /// One eligible type is not a question. The same reading milestone 8.20b took for a single
        /// printed push distance: a control with one legal answer is a control no player is shown.
        /// </summary>
        [Test]
        public void AtAGrantedPoint_OneEligibleTypeOpensNoSecondQuestion()
        {
            var board = BoardWithOneRemovedMelee();
            var context = Context(board, overlay: true);

            var applied = Handler().Resume(
                context, new[] { EffectValue.Space(DodgerSpawnPointBoard.HeroSpawn) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Only melee is eligible, so the answer is settled and the step finishes on the first " +
                "answer.");
            AssertRespawnedAt(applied, DodgerSpawnPointBoard.HeroSpawn);
        }

        /// <summary>
        /// A node that asks twice must not ask the FIRST question again once it has been answered,
        /// because a restored pause re-enters through <c>Execute</c> rather than through
        /// <c>Resume</c>. Milestone 8.35z34 measured this failure in Play Mode - a snapshot captured
        /// while the type question stood came back showing the destination question - and this is the
        /// same statement made offline, where it costs a second rather than a Unity launch.
        ///
        /// <para>What it pins is idempotence: given a destination already recorded against this node,
        /// <c>Execute</c> answers the question that is actually outstanding.</para>
        /// </summary>
        [Test]
        public void WithADestinationAlreadyAnswered_ExecuteAsksTheQuestionThatIsOutstanding()
        {
            var board = BoardWithARemovedMeleeAndARemovedRanged();
            var destination = DodgerSpawnPointBoard.HeroSpawn;

            var reEntered = Handler().Execute(
                Context(board, overlay: true, answered: destination));

            Assert.That(reEntered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "The node is still owed an answer, so re-entering it asks for one.");
            Assert.That(
                reEntered.Choice.Candidates.All(candidate =>
                    candidate.Kind == EffectValueKind.Minion),
                Is.True,
                "and the question it asks is the TYPE one, not the destination it already has. " +
                "Asking for a space again is what a restored pause did before this was fixed, and " +
                "the two questions share a node, so nothing but the candidates' KIND says which is " +
                "standing.");
        }

        /// <summary>
        /// The destination answer is re-read and re-validated at the type answer, never trusted: a
        /// coordinate the earlier choice named is checked against the board the second answer arrives
        /// at. Here the type answer arrives with no destination answer behind it at all, which is the
        /// shape a tampered or truncated replay produces.
        /// </summary>
        [Test]
        public void ATypeAnswerWithNoDestinationBehindIt_IsRefused()
        {
            var board = BoardWithARemovedMeleeAndARemovedRanged();
            var context = Context(board, overlay: true);
            var supplied = board.FindUnitSupply().First().MinionId;

            var refused = Handler().Resume(context, new[] { EffectValue.Minion(supplied) });

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Nothing in this node's accepted answers names a destination, so there is no space " +
                "for the chosen miniature to come back to and the handler refuses rather than " +
                "inventing one.");
        }

        // -----------------------------------------------------------------------------------------
        // Boards. Each one reaches its eligible state by REMOVING a miniature, which is p.19's own
        // precondition rather than a fixture convenience.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Red's whole wave standing, less one melee: two Red melee points against one standing melee
        /// miniature makes melee eligible, and the single ranged point against its standing miniature
        /// leaves ranged ineligible. So exactly one type may come back.
        /// </summary>
        private static AdjacentMoveAuthority BoardWithOneRemovedMelee()
        {
            var board = FullRedWave();
            Remove(board, StandardMinionType.Melee, DodgerSpawnPointBoard.MeleeSpawnNear);
            return board;
        }

        /// <summary>The same board with the ranged miniature removed as well, so p.19 admits two.</summary>
        private static AdjacentMoveAuthority BoardWithARemovedMeleeAndARemovedRanged()
        {
            var board = BoardWithOneRemovedMelee();
            Remove(board, StandardMinionType.Ranged, DodgerSpawnPointBoard.RangedSpawnNear);
            return board;
        }

        private static AdjacentMoveAuthority FullRedWave() =>
            DodgerSpawnPointBoard.CreateBoardWithTypedOccupiers(
                Dodger,
                DodgerSpawnPointBoard.Origin,
                (DodgerSpawnPointBoard.MeleeSpawnNear, TeamSide.Red, StandardMinionType.Melee),
                (DodgerSpawnPointBoard.ThroneMinionSpawn, TeamSide.Red, StandardMinionType.Melee),
                (DodgerSpawnPointBoard.RangedSpawnNear, TeamSide.Red, StandardMinionType.Ranged));

        private static void Remove(
            AdjacentMoveAuthority board, StandardMinionType type, HexCoordinate from)
        {
            var standing = board.Minions.Single(minion =>
                minion.MinionType == type && minion.Coordinate.Equals(from));
            Assert.That(
                board.CommitValidatedLanePushRemoval(standing.MinionId), Is.True,
                "p.19's own precondition: a minion must first be removed before it can be respawned.");
        }

        private static IEnumerable<HexCoordinate> Adjacent(HexCoordinate centre) =>
            new[]
            {
                new HexCoordinate(centre.Q + 1, centre.R),
                new HexCoordinate(centre.Q - 1, centre.R),
                new HexCoordinate(centre.Q, centre.R + 1),
                new HexCoordinate(centre.Q, centre.R - 1),
                new HexCoordinate(centre.Q + 1, centre.R - 1),
                new HexCoordinate(centre.Q - 1, centre.R + 1)
            };

        private static void AssertRespawnedAt(EffectExecutionResult result, HexCoordinate expected)
        {
            var at = result.Outcomes.Single(outcome => string.Equals(
                outcome.OutputName,
                SyntheticPlacementCapabilities.RespawnedAtOutputName,
                StringComparison.Ordinal));
            Assert.That(at.Value.Coordinate, Is.EqualTo(expected));
            Assert.That(
                result.Outcomes.Any(outcome => string.Equals(
                    outcome.OutputName,
                    SyntheticPlacementCapabilities.RespawnedOutputName,
                    StringComparison.Ordinal)),
                Is.True,
                "Both declared outputs are published, whichever commit path the destination took.");
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, bool overlay, HexCoordinate? answered = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("dodger-respawn-node"),
                EffectFailurePolicy.Mandatory,
                Dodger,
                Dodger,
                new CardInstanceId("dodger-necromancy-instance"),
                new CardDefinitionReference(
                    GuardsDigital.Core.Cards.Verified.DodgerCardContent.Necromancy,
                    GuardsDigital.Core.Cards.Verified.DodgerCardContent.ContentRevision),
                new ActionDefinitionId("dodger-respawn-action"),
                new Dictionary<string, EffectValue>(),
                1,
                1,
                Participants(),
                null,
                overlay ? UltimateUp() : RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                answersAlreadyGiven: answered.HasValue
                    ? new[] { new[] { EffectValue.Space(answered.Value) } }
                    : null);

        /// <summary>
        /// Tide of Darkness as p.16's permanent ability - a continuous source rather than an Active
        /// effect, which is what the Ultimate composes and therefore what the overlay must be read
        /// from.
        /// </summary>
        private static RuleModificationSet UltimateUp() =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        RuleModification.ForAllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone(),
                        RuleObject.ForHero(Dodger),
                        new ContinuousRuleSource(
                            "dodger-tide-of-darkness",
                            ContinuousRuleSourceKind.Ultimate,
                            Dodger,
                            "all-spaces-count-as-friendly-minion-spawn-points",
                            "while-performing-an-action"),
                        TeamSide.Red,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        private static MatchParticipants Participants()
        {
            var dodgerPlayer = new PlayerId("dodger-player");
            var enemyPlayer = new PlayerId("dodger-enemy-player");
            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                new[] { new Player(dodgerPlayer, RedTeam), new Player(enemyPlayer, BlueTeam) },
                new[]
                {
                    new Hero(Dodger, dodgerPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout)
                });
        }
    }
}

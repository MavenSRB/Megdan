using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38b. The two placement destination clauses Swift's Blue line prints, proved as
    /// vocabulary before any of her content exists.
    ///
    /// <para>Three of her five Blue faces print "Place yourself into a space <b>in a straight line</b>
    /// in radius" and the other two print "Place yourself into a space in radius <b>not in a straight
    /// line from you</b>". Both are qualified <see cref="SyntheticPlacementCapabilities"/> arms over
    /// the printed Radius, declared apart by the rule this catalog has followed since milestone 8.20:
    /// a printed clause that qualifies a declaration belongs in the identity of the thing declared,
    /// because a shared declaration gaining a field moves every hero who already referenced it.</para>
    ///
    /// <para><b>Both are DEFERRED handlers here and that is deliberate.</b> Milestone 7.9's gate
    /// refuses a registration no dealt card can start, which is also what keeps the pinned capability
    /// list - and therefore every snapshot's bytes - from moving for a hero who is not in a match.
    /// These cases therefore take the file's WHOLE vocabulary rather than either list, exactly as
    /// <c>DodgerRespawnCapabilityTests</c> records: which list holds a contract is a statement about
    /// whether a dealt card starts it, and the subject here is what the handler does. Reading the
    /// deferred list would make every case below fail the moment milestone 8.38e moves them across.
    /// One case does read the lists, and it reads them to assert exactly that pending move.</para>
    ///
    /// <para><b>The board is <see cref="SwiftJumpBoard"/>, which is featureless on purpose</b> - one
    /// zone, a radius-4 disk, no Spawn points, nobody but the actor - so the only thing that can
    /// remove a candidate space is the clause under test.</para>
    /// </summary>
    [TestFixture]
    public sealed class PlacementLineQualifierTests
    {
        private const int PrintedRadius = 3;

        private static readonly ActionDefinitionId JumpAction = new ActionDefinitionId("probe-jump");

        private static readonly CardDefinitionId JumpCard = new CardDefinitionId("probe-jump-card");

        private static readonly CardInstanceId JumpInstance =
            new CardInstanceId("probe-jump-instance");

        // -------------------------------------------------------------------------------------------
        // What the two declarations are
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Both are registered and neither is deferred, which is milestone 7.9's gate SATISFIED: the
        /// milestone that deals Swift is what moved them, and her Blue line is what a configured match
        /// now puts in a hand.
        ///
        /// <para>Milestone 8.38b wrote this case the other way round - "both are built and neither is
        /// registered" - and wrote it as the PAIR rather than as two memberships so that a dealing
        /// moving one and forgetting the other would be a failure rather than a half-green. Milestone
        /// 8.38e inverts it on the same terms and for the same reason: the claim that has to keep being
        /// true is that these two are exactly together, whichever list they are in.</para>
        ///
        /// <para>Milestone 8.57b refills the deferral list this case used to assert empty, and the
        /// assertion is narrowed rather than dropped: what it now says is which declaration is held
        /// back and why, so a pair that drifted apart still fails and a hero mid-series does not have
        /// to delete the check to land.</para>
        /// </summary>
        [Test]
        public void BothLineQualifiedPlaces_AreRegisteredNowThatADealtCardNamesThem()
        {
            var deferred = SyntheticPlacementCapabilities.DeferredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();
            var registered = SyntheticPlacementCapabilities.RegisteredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();

            Assert.That(
                registered.Intersect(new[]
                {
                    SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1,
                    SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1
                }),
                Is.EquivalentTo(new[]
                {
                    SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1,
                    SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1
                }),
                "Both of Swift's straight-line Places are registered, together, now that Steam Jump " +
                "and Delayed Jump are cards a configured match deals.");
            Assert.That(deferred, Has.No.Member(
                    SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1)
                .And.No.Member(SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1),
                "and neither of them is still being held back, which is the other half of what the " +
                "dealing moved.");

            // Milestone 8.57b. The list milestone 8.38e emptied and kept is the list this milestone
            // was kept for: Rowenna's Throw the Gauntlet mints a third qualifier and no configured
            // match deals her until 8.57g, so the file holds exactly one declaration back. Asserting
            // WHICH one rather than asserting emptiness is what keeps this case about the gate rather
            // than about how many heroes happen to be mid-series.
            // Milestone 8.57d adds the second, and it is the same hero's: her three Green Movement
            // faces name the printed-Radius twin of 8.57b's declaration. Both leave this list together
            // at 8.57g, which is what deals her.
            Assert.That(deferred, Is.EquivalentTo(new[]
                {
                    SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1,
                    SyntheticPlacementCapabilities.PlaceInRadiusBesideANamedHeroV1
                }),
                "Two declarations deferred, and they are the two whose only printed consumers belong " +
                "to a hero this build does not deal yet.");
            Assert.That(registered.Intersect(deferred), Is.Empty,
                "and nothing is in both lists, which is what the gate means either way round.");
        }

        // -------------------------------------------------------------------------------------------
        // The partition: one membership question, read two ways
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The claim the pair rests on. Every empty playable space the printed Radius reaches is
        /// admitted by exactly one of the two declarations, because both read the same
        /// <see cref="StraightLineQuery"/> membership question from the actor's own space.
        /// </summary>
        [Test]
        public void TheTwoDeclarations_PartitionThePrintedDiskExactly()
        {
            var board = SwiftJumpBoard.CreateBoard();

            var onFile = Candidates(board, SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1);
            var offFile = Candidates(
                board, SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1);
            var wholeDisk = SwiftJumpBoard.Playable
                .Where(space =>
                    !space.Equals(SwiftJumpBoard.Origin) &&
                    HexDistanceQuery.Calculate(SwiftJumpBoard.Origin, space) <= PrintedRadius)
                .ToArray();

            Assert.That(onFile, Is.Not.Empty);
            Assert.That(offFile, Is.Not.Empty);
            Assert.That(onFile.Intersect(offFile), Is.Empty,
                "A space cannot be both on and off the actor's file.");
            Assert.That(
                onFile.Concat(offFile),
                Is.EquivalentTo(wholeDisk),
                "and between them they admit the whole disk the printed Radius reaches, less the " +
                "actor's own space - so the two are one question read two ways.");
        }

        /// <summary>
        /// What the on-file set actually is, checked against the geometry rather than against a
        /// remembered coordinate list: six files, each cut to the printed Radius, is 6 x 3 = 18 spaces
        /// on a board wide enough to hold all of them.
        /// </summary>
        [Test]
        public void TheOnFileSet_IsTheSixFilesCutToThePrintedRadius()
        {
            var board = SwiftJumpBoard.CreateBoard();

            var onFile = Candidates(board, SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1);

            Assert.That(onFile.Count, Is.EqualTo(6 * PrintedRadius));
            Assert.That(
                onFile.All(space =>
                    StraightLineQuery.AreInStraightLine(SwiftJumpBoard.Origin, space)),
                Is.True);
            Assert.That(
                onFile.All(space =>
                    HexDistanceQuery.Calculate(SwiftJumpBoard.Origin, space) <= PrintedRadius),
                Is.True);
        }

        /// <summary>
        /// The consequence of p.19 that the printed words do not say out loud, and which a player will
        /// meet the first time they read Delayed Jump: an adjacent space is a straight line, so "not in
        /// a straight line from you" excludes every one of the actor's six neighbours.
        /// </summary>
        [Test]
        public void TheOffFileSet_ExcludesEveryNeighbourBecausePageNineteenCountsAdjacencyAsALine()
        {
            var board = SwiftJumpBoard.CreateBoard();

            var offFile = Candidates(
                board, SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1);

            Assert.That(
                offFile.All(space =>
                    HexDistanceQuery.Calculate(SwiftJumpBoard.Origin, space) > 1),
                Is.True,
                "Every neighbour of the actor is on a file with them, so none survives the clause.");
            Assert.That(
                offFile.Any(space =>
                    HexDistanceQuery.Calculate(SwiftJumpBoard.Origin, space) == 2),
                Is.True,
                "and the nearest space the clause does admit is at two, which is what makes Delayed " +
                "Jump a displacement rather than a step.");
        }

        // -------------------------------------------------------------------------------------------
        // Occupancy still decides, and the clause is not a substitute for it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A qualifier narrows the destination set; it does not replace the emptiness rule underneath
        /// it. A minion standing on a file inside the Radius is not a destination for either card.
        /// </summary>
        [Test]
        public void AnOccupiedSpace_IsNoDestinationForEitherDeclaration()
        {
            var occupiedOnFile = new HexCoordinate(2, 0);
            var occupiedOffFile = new HexCoordinate(2, -1);

            Assert.That(
                StraightLineQuery.AreInStraightLine(SwiftJumpBoard.Origin, occupiedOnFile), Is.True,
                "The fixture's own premise: this coordinate is on a file through the origin.");
            Assert.That(
                StraightLineQuery.AreInStraightLine(SwiftJumpBoard.Origin, occupiedOffFile), Is.False,
                "and this one is not, so the case covers both declarations.");

            var board = SwiftJumpBoard.CreateBoard(occupiedOnFile, occupiedOffFile);

            Assert.That(
                Candidates(board, SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1),
                Has.No.Member(occupiedOnFile));
            Assert.That(
                Candidates(board, SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1),
                Has.No.Member(occupiedOffFile));
        }

        // -------------------------------------------------------------------------------------------
        // Driving the handler
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticPlacementCapabilities.Handlers()
                .Single(handler => handler.Reference.Equals(reference));

        private static IReadOnlyList<HexCoordinate> Candidates(
            AdjacentMoveAuthority board, EffectCapabilityReference reference)
        {
            var opened = Handler(reference).Execute(Context(board));

            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "A Place with destinations asks which one, which is the only decision it makes.");
            return opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static EffectCapabilityContext Context(AdjacentMoveAuthority board) =>
            new EffectCapabilityContext(
                new EffectNodeId("probe-jump-node"),
                EffectFailurePolicy.Mandatory,
                SwiftJumpBoard.Swift,
                SwiftJumpBoard.Swift,
                JumpInstance,
                new CardDefinitionReference(JumpCard, "probe-rev-1"),
                JumpAction,
                new Dictionary<string, EffectValue>
                {
                    ["subject"] = EffectValue.Space(SwiftJumpBoard.Origin),
                    [SyntheticPlacementCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)PlaceEligiblePieceKind.GamePiece),
                    [SyntheticPlacementCapabilities.DestinationAnchorParameterName] =
                        EffectValue.Integer((int)PlaceDestinationAnchor.ActingHero),
                    [SyntheticPlacementCapabilities.SpawnPointExclusionParameterName] =
                        EffectValue.Integer((int)PlaceSpawnPointExclusion.None)
                },
                1,
                1,
                SwiftJumpBoard.Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { JumpDefinition() }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board));

        /// <summary>
        /// The one thing the printed arm needs that the written arm does not: a card printing the
        /// Radius its reach is read from. It is a probe definition rather than one of Swift's, because
        /// Swift has no content until milestone 8.38d and the clause is not hers alone.
        /// </summary>
        private static CardDefinition JumpDefinition() =>
            new CardDefinition(
                JumpCard,
                "probe-rev-1",
                "Probe Jump",
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        JumpAction,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Radius, PrintedRadius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        null,
                        // A Skill has to carry an effect root or say which milestone owes it one.
                        // This probe carries neither a root nor a debt a milestone will pay: the
                        // capability under test is driven directly, and the definition exists only to
                        // print the Radius its reach is read from. Saying so is the alternative to
                        // composing a body nothing runs.
                        PrintedTextComposition.DeferredUntil(
                            "never - this is milestone 8.38b's Radius-printing probe, not content"))
                });
    }
}

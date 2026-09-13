using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40e. The pull Bain's Silver basic prints, proved as vocabulary before any of his
    /// content exists: "Target a unit or a token in a straight line and in range with no obstacles
    /// between you and the target; move the target towards you in a straight line, until you are
    /// adjacent."
    ///
    /// <para><b>Half of it was already built and this block only had to widen the other half.</b>
    /// <c>synthetic-range-obstacle-target@1</c> - milestone 8.39b's, for Cutter's mirror-image
    /// Grappling Bolt - already walks p.19's six directions to the printed Range Total, already stops
    /// at the first obstacle on each file, and already blocks on exactly p.18's full list, which is
    /// what "no obstacles" asks for. Two things his sentence needs that hers does not: the offer
    /// narrowed to units and tokens, and the approach space published on the ACTOR's side of the line
    /// rather than the obstacle's. Both are optional additions to that one declaration.</para>
    ///
    /// <para><b>The narrowing is about what may be NAMED and never about what BLOCKS.</b> His own deck
    /// is what makes that load-bearing and BA-03 records it: this face prints "no obstacles" and his
    /// five crossbows print "no units, or terrain", so a token blocks the pull and does not block a
    /// shot. A file whose first obstacle is terrain therefore offers NOTHING on that file rather than
    /// offering the unit standing behind the wall.</para>
    ///
    /// <para><b>The move half is registered already.</b> <c>synthetic-direct-move@3</c>'s subject is a
    /// typed coordinate naming a hero, a minion or a token, and
    /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/> takes a space an earlier node published and
    /// opens no choice. <see cref="DirectMoveDestinationPolicy.TowardActor"/> is NOT the contract -
    /// it is a per-step filter bounded by the node's own distance, with no straight line and no
    /// adjacency stop - so nothing here touches it.</para>
    /// </summary>
    [TestFixture]
    public sealed class PullLineTargetTests
    {
        private const int PrintedRange = 5;

        private static readonly ActionDefinitionId PullAction = new ActionDefinitionId("probe-pull");
        private static readonly CardDefinitionId PullCard = new CardDefinitionId("probe-pull-card");
        private static readonly CardInstanceId PullInstance =
            new CardInstanceId("probe-pull-instance");

        private static HexCoordinate East(int steps) => PullLineBoard.At(HexDirection.East, steps);
        private static HexCoordinate West(int steps) => PullLineBoard.At(HexDirection.West, steps);
        private static HexCoordinate NorthEast(int steps) =>
            PullLineBoard.At(HexDirection.NorthEast, steps);
        private static HexCoordinate NorthWest(int steps) =>
            PullLineBoard.At(HexDirection.NorthWest, steps);
        private static HexCoordinate SouthWest(int steps) =>
            PullLineBoard.At(HexDirection.SouthWest, steps);
        private static HexCoordinate SouthEast(int steps) =>
            PullLineBoard.At(HexDirection.SouthEast, steps);

        // -------------------------------------------------------------------------------------------
        // "Target a unit or a token in a straight line and in range with no obstacles between you"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The set equality the block is scoped on, on a board where every one of p.19's six files
        /// excludes its candidate for a different reason: a unit, a token and a unit further out are
        /// offered; the terrain ring, an inner wall with a unit behind it, and a second unit behind the
        /// first are not.
        /// </summary>
        [Test]
        public void TheNarrowedSelection_OffersExactlyTheFirstUnitOrTokenOnEachFile()
        {
            var board = SixFileBoard();

            Assert.That(
                Candidates(board),
                Is.EquivalentTo(new[] { East(2), West(1), NorthEast(4) }),
                "East offers the first hero and not the one behind him, West offers the token, " +
                "NorthEast offers the minion; the two empty files reach only the terrain ring, and " +
                "SouthWest's wall offers nothing rather than the minion behind it.");
        }

        /// <summary>
        /// <b>The narrowing does not widen the blocking set, which is the sentence this whole block
        /// turns on.</b> "No obstacles" is p.18's full list, so the wall still stops the walk; what
        /// changes is only that the wall may not be named. Offering the minion behind it would be
        /// reading his crossbows' narrower clause into this face.
        /// </summary>
        [Test]
        public void AFileWhoseFirstObstacleIsTerrain_OffersNothingRatherThanWhatStandsBehindIt()
        {
            var board = PullLineBoard.CreateBoard(
                enemyMinionsAt: new[] { SouthWest(3) },
                innerTerrainAt: new[] { SouthWest(2) });

            Assert.That(
                Candidates(board).Where(at => at.Equals(SouthWest(3)) || at.Equals(SouthWest(2))),
                Is.Empty,
                "p.18 makes terrain an obstacle, so the minion behind the wall has the wall " +
                "'between you and the target' and the wall itself is not a unit or a token.");
            Assert.That(
                Candidates(board, unitsAndTokensOnly: false), Has.Member(SouthWest(2)),
                "and the same board offers the wall itself to a declaration that does not print the " +
                "narrowing, which is what proves the walk is untouched.");
        }

        /// <summary>
        /// Unbound is the absence of the clause, and the absence of the clause is milestone 8.39b's
        /// set exactly - terrain included. Asserted on the same board as the narrowed case above, so
        /// the two readings are compared rather than described.
        /// </summary>
        [Test]
        public void AnUnboundSelection_OffersTheSetItAlwaysDid_TerrainIncluded()
        {
            var board = SixFileBoard();

            Assert.That(
                Candidates(board, unitsAndTokensOnly: false),
                Is.EquivalentTo(new[]
                {
                    East(2), West(1), NorthEast(4), SouthWest(2), NorthWest(5), SouthEast(5)
                }),
                "Six files, one first obstacle each, exactly as the family has offered since 8.39b.");
        }

        // -------------------------------------------------------------------------------------------
        // "move the target towards you in a straight line, until you are adjacent"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Until you are adjacent" is a destination and not a distance, so the selection publishes the
        /// space beside the ACTOR on the chosen file and the node that moves asks nobody anything.
        /// </summary>
        [Test]
        public void ThePublishedDestination_IsTheSpaceBesideTheActorAlongTheChosenFile()
        {
            var board = PullLineBoard.CreateBoard(enemyMinionsAt: new[] { East(3) });

            Assert.That(Destination(board, East(3)), Is.EqualTo(East(1)));
            Assert.That(
                Answer(board, East(3)).Select(fact => fact.OutputName),
                Is.EquivalentTo(new[]
                {
                    SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                    SyntheticTargetingCapabilities.ActorApproachSpaceOutputName
                }),
                "and a node that asked for the actor-side space is published that and nothing else - " +
                "an undeclared fact is a snapshot the build refuses to read back.");
        }

        /// <summary>
        /// The existing Move lands the subject there. It is <c>synthetic-direct-move@3</c> under
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/>, unchanged: this block adds no
        /// displacement contract at all.
        /// </summary>
        [Test]
        public void TheExistingMove_LandsTheSubjectOnThePublishedDestination()
        {
            var board = PullLineBoard.CreateBoard(enemyMinionsAt: new[] { East(3) });
            var destination = Destination(board, East(3));

            Assert.That(
                Move().Execute(MoveContext(board, East(3), destination)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryGetObjectAt(East(1), out var landed), Is.True);
            Assert.That(landed.Kind, Is.EqualTo(BoardObjectKind.Minion));
            Assert.That(board.TryGetObjectAt(East(3), out _), Is.False,
                "and the space it came from is empty, because a pull moves rather than copies.");
        }

        /// <summary>
        /// <b>A target already adjacent is legal and moves nowhere</b>, which the block states rather
        /// than treats as a failure: the clause is satisfied where it stands. The selection offers it,
        /// the published destination is its own space, and the Move that consumes it has nowhere to go
        /// - p.11's reachability never offers the mover's own space - which an <c>IfAble</c> node reads
        /// as a step not taken. That is the shape Grappling Bolt's own Move already has.
        /// </summary>
        [Test]
        public void AnAlreadyAdjacentSubject_IsOfferedAndTheMoveLeavesItWhereItStands()
        {
            var board = PullLineBoard.CreateBoard();
            var token = PlaceToken(board, East(1));

            Assert.That(Candidates(board), Has.Member(East(1)));
            Assert.That(Destination(board, East(1)), Is.EqualTo(East(1)));
            Assert.That(
                Move().Execute(MoveContext(board, East(1), East(1))).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(East(1)));
            Assert.That(board.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
        }

        /// <summary>
        /// <b>A token is a legal subject, which is the one thing no other printed pull in this build
        /// has asked for.</b> p.19's Target keyword admits a unit, a token or a space, and this face
        /// prints "a unit or a token". The moved token keeps its instance identity, its owner, its
        /// controller and its definition, because p.19 ties a token's properties to the instance.
        /// </summary>
        [Test]
        public void ATokenSubject_IsPulledAndKeepsItsIdentityAndItsOwner()
        {
            var board = PullLineBoard.CreateBoard();
            var token = PlaceToken(board, East(4));

            Assert.That(Candidates(board), Has.Member(East(4)));
            Assert.That(
                Move().Execute(MoveContext(board, East(4), Destination(board, East(4)))).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));

            var moved = board.PlacedTokens.Single();
            Assert.That(moved.Coordinate, Is.EqualTo(East(1)));
            Assert.That(moved.Id, Is.EqualTo(token.Id));
            Assert.That(moved.OwnerHeroId, Is.EqualTo(token.OwnerHeroId));
            Assert.That(moved.ControllerHeroId, Is.EqualTo(token.ControllerHeroId));
            Assert.That(moved.Definition.Reference, Is.EqualTo(token.Definition.Reference));
        }

        /// <summary>
        /// A hero and a minion each work too, on their own files of one board - the three kinds p.19's
        /// Target keyword admits, proved together rather than one of them inferred from another.
        /// </summary>
        [Test]
        public void AHeroSubjectAndAMinionSubject_AreEachPulledTheSameWay()
        {
            var board = PullLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(3) }, enemyMinionsAt: new[] { West(4) });

            Assert.That(
                Move().Execute(MoveContext(board, East(3), Destination(board, East(3)))).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryGetObjectAt(East(1), out var hero), Is.True);
            Assert.That(hero.HeroId, Is.EqualTo(PullLineBoard.Enemies[0]));

            Assert.That(
                Move().Execute(MoveContext(board, West(4), Destination(board, West(4)))).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.TryGetObjectAt(West(1), out var minion), Is.True);
            Assert.That(minion.Kind, Is.EqualTo(BoardObjectKind.Minion));
        }

        // -------------------------------------------------------------------------------------------
        // p.18's immunity, asked of the obstacles that can carry it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Immunity is a rule about units, so it is asked of units and of nothing else - and an immune
        /// unit still occupies its file, because standing somewhere is not being targeted. The token
        /// and the terrain ring on the other files are unaffected by an immunity standing on the board,
        /// which is the half worth asserting: nothing is asked on their behalf.
        /// </summary>
        [Test]
        public void Immunity_IsAskedOfUnitsAndNeverOfTerrainOrTokens()
        {
            var board = PullLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(1) }, enemyMinionsAt: new[] { East(3) });
            PlaceToken(board, West(2));
            var immunity = ImmunityFor(PullLineBoard.Enemies[0], RuleActivity.All);

            Assert.That(
                Candidates(board, modifications: immunity), Has.No.Member(East(1)),
                "p.18: 'if a unit is immune, you cannot target that unit'.");
            Assert.That(
                Candidates(board, modifications: immunity), Has.No.Member(East(3)),
                "and the minion behind him is not offered in his place - he is an obstacle whether " +
                "or not he can be chosen.");
            Assert.That(
                Candidates(board, modifications: immunity), Has.Member(West(2)),
                "The token on another file is offered with the same immunity standing, because the " +
                "Immune keyword is never asked about a token.");
            Assert.That(
                Candidates(board, unitsAndTokensOnly: false, modifications: immunity),
                Has.Member(NorthEast(5)),
                "and so is the terrain ring, to a declaration that admits terrain at all.");
        }

        // -------------------------------------------------------------------------------------------
        // What every declaration written before this milestone still gets
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Asserted over the corpus rather than over a remembered list: every node in the build that
        /// names this family binds no narrowing and declares the two outputs it always declared. A
        /// fifteenth hero's face added without this block's parameter fails here rather than going
        /// silently uncovered.
        /// </summary>
        [Test]
        public void EveryDeclarationWrittenBeforeThisMilestone_BindsNoNarrowingAndDeclaresTheSameOutputs()
        {
            var found = 0;
            foreach (var entry in Corpus())
            {
                foreach (var node in entry.Definition.Nodes)
                {
                    if (!node.Capability.Equals(
                            SyntheticTargetingCapabilities.RangeObstacleTargetV1))
                    {
                        continue;
                    }

                    found++;
                    Assert.That(
                        node.Inputs.Select(input => input.ParameterName),
                        Has.No.Member(SyntheticTargetingCapabilities.ExcludeTerrainParameterName),
                        $"{entry.Owner}/{node.Id.Value} prints no narrowing and binds none.");
                    Assert.That(
                        node.Outputs.Select(output => output.OutputName),
                        Is.EquivalentTo(new[]
                        {
                            SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                            SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName
                        }),
                        $"{entry.Owner}/{node.Id.Value} declares the pair it always declared.");
                }
            }

            Assert.That(found, Is.EqualTo(2),
                "Cutter's Grappling Bolt and the showcase Red Grapnel Skill are the two declarations " +
                "this family had before Bain, and a sweep that found neither would prove nothing.");
        }

        /// <summary>
        /// And it still publishes them, which is the behavioural half of the same claim: a node
        /// declaring 8.39b's pair is handed 8.39b's pair, with the actor-side space absent because it
        /// never asked.
        /// </summary>
        [Test]
        public void ADeclarationWrittenBeforeThisMilestone_IsPublishedTheExactPairItAlwaysWas()
        {
            var board = PullLineBoard.CreateBoard(enemyMinionsAt: new[] { East(3) });
            var applied = Selection().Resume(
                Context(board, unitsAndTokensOnly: false, declaredOutputs: GrappleOutputs()),
                new[] { EffectValue.Space(East(3)) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                applied.Outcomes.Select(fact => fact.OutputName),
                Is.EquivalentTo(new[]
                {
                    SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                    SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName
                }));
            Assert.That(
                applied.Outcomes.Single(fact => fact.OutputName ==
                    SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName)
                    .Value.Coordinate,
                Is.EqualTo(East(2)),
                "one step back from the obstacle, which is where the ACTOR ends up beside it.");
        }

        /// <summary>
        /// The two approach spaces are the two ends of one file and this is the case that says so on
        /// one board: the obstacle-side value leaves the hero beside the target, the actor-side value
        /// leaves the target beside the hero, and they are different spaces whenever anything moves.
        /// </summary>
        [Test]
        public void TheTwoApproachSpaces_AreTheTwoEndsOfTheSameFile()
        {
            var board = PullLineBoard.CreateBoard(enemyMinionsAt: new[] { East(4) });
            var both = Selection().Resume(
                Context(board, declaredOutputs: GrappleOutputs().Concat(PullOutputs())
                    .Distinct()
                    .ToArray()),
                new[] { EffectValue.Space(East(4)) });

            Assert.That(
                Coordinate(both, SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName),
                Is.EqualTo(East(3)));
            Assert.That(
                Coordinate(both, SyntheticTargetingCapabilities.ActorApproachSpaceOutputName),
                Is.EqualTo(East(1)));
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One board carrying all six of p.19's files, each excluding its candidate for a different
        /// reason. The token supply is one Hazard, which is all this board needs.
        /// </summary>
        private static AdjacentMoveAuthority SixFileBoard()
        {
            var board = PullLineBoard.CreateBoard(
                enemyHeroesAt: new[] { East(2), East(3) },
                enemyMinionsAt: new[] { NorthEast(4), SouthWest(3) },
                innerTerrainAt: new[] { SouthWest(2) });
            PlaceToken(board, West(1));
            return board;
        }

        /// <remarks>
        /// Reached through <c>Handlers()</c> - registered and deferred concatenated - for the reason
        /// milestone 8.39b's own fixture does: this declaration has already moved from one list to the
        /// other once, and milestone 8.39i is what moved it. It is REGISTERED in this tree.
        /// </remarks>
        private static IEffectCapabilityHandler Selection() =>
            SyntheticTargetingCapabilities.Handlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticTargetingCapabilities.RangeObstacleTargetV1));

        private static IEffectCapabilityHandler Move() =>
            SyntheticDisplacementCapabilities.Handlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));

        private static IReadOnlyList<HexCoordinate> Candidates(
            AdjacentMoveAuthority board,
            bool unitsAndTokensOnly = true,
            int printedRange = PrintedRange,
            RuleModificationSet modifications = null)
        {
            var opened = Selection().Execute(
                Context(board, unitsAndTokensOnly, printedRange, modifications));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HexCoordinate>()
                : opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static IReadOnlyList<EffectOutcomeFact> Answer(
            AdjacentMoveAuthority board, HexCoordinate chosen)
        {
            var applied = Selection().Resume(
                Context(board), new[] { EffectValue.Space(chosen) });
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            return applied.Outcomes;
        }

        private static HexCoordinate Destination(
            AdjacentMoveAuthority board, HexCoordinate chosen) =>
            Answer(board, chosen)
                .Single(fact => fact.OutputName ==
                    SyntheticTargetingCapabilities.ActorApproachSpaceOutputName)
                .Value.Coordinate;

        private static HexCoordinate Coordinate(EffectExecutionResult result, string outputName)
        {
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            return result.Outcomes.Single(fact => fact.OutputName == outputName).Value.Coordinate;
        }

        /// <summary>What Cutter's Grappling Bolt and the showcase Skill beside it declare.</summary>
        private static IReadOnlyList<EffectOutputBinding> GrappleOutputs() =>
            new[]
            {
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                    EffectValueKind.Coordinate),
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName,
                    EffectValueKind.Coordinate)
            };

        /// <summary>What Bain's pull will declare: the target, and where it lands.</summary>
        private static IReadOnlyList<EffectOutputBinding> PullOutputs() =>
            new[]
            {
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                    EffectValueKind.Coordinate),
                new EffectOutputBinding(
                    SyntheticTargetingCapabilities.ActorApproachSpaceOutputName,
                    EffectValueKind.Coordinate)
            };

        private static PlacedToken PlaceToken(AdjacentMoveAuthority board, HexCoordinate at)
        {
            var token = new PlacedToken(
                new TokenInstanceId($"pull-line-token-{at.Q}-{at.R}"),
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.HazardV4),
                PullLineBoard.Enemies[5],
                PullLineBoard.Enemies[5],
                new CardInstanceId("pull-line-token-source"),
                at);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            bool unitsAndTokensOnly = true,
            int printedRange = PrintedRange,
            RuleModificationSet modifications = null,
            IReadOnlyList<EffectOutputBinding> declaredOutputs = null)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.All)
            };
            if (unitsAndTokensOnly)
            {
                inputs[SyntheticTargetingCapabilities.ExcludeTerrainParameterName] =
                    EffectValue.Boolean(true);
            }

            return new EffectCapabilityContext(
                new EffectNodeId("probe-pull-node"),
                EffectFailurePolicy.Mandatory,
                PullLineBoard.Actor,
                PullLineBoard.Actor,
                PullInstance,
                new CardDefinitionReference(PullCard, "probe-rev-1"),
                PullAction,
                inputs,
                1,
                1,
                PullLineBoard.Participants(),
                null,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(
                    new[] { PullDefinition(printedRange) }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                Components(board),
                declaredOutputs: declaredOutputs ?? PullOutputs());
        }

        /// <summary>
        /// The Move this selection feeds, declared exactly as Bain's composition will: a named
        /// destination, and a distance that is a budget rather than an exact count. Its failure policy
        /// is <see cref="EffectFailurePolicy.IfAble"/>, which is what makes an already-adjacent target
        /// a step not taken rather than a stopped action - the shape Grappling Bolt's own Move has.
        /// </summary>
        private static EffectCapabilityContext MoveContext(
            AdjacentMoveAuthority board, HexCoordinate mover, HexCoordinate destination) =>
            new EffectCapabilityContext(
                new EffectNodeId("probe-pull-move"),
                EffectFailurePolicy.IfAble,
                PullLineBoard.Actor,
                PullLineBoard.Actor,
                PullInstance,
                new CardDefinitionReference(PullCard, "probe-rev-1"),
                PullAction,
                new Dictionary<string, EffectValue>
                {
                    ["mover"] = EffectValue.Space(mover),
                    ["distance"] = EffectValue.Integer(6),
                    [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                        EffectValue.Integer((int)DirectMoveDestinationPolicy.NamedSpace),
                    [SyntheticDisplacementCapabilities.NamedDestinationParameterName] =
                        EffectValue.Space(destination)
                },
                1,
                1,
                PullLineBoard.Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(
                    new[] { PullDefinition(PrintedRange) }, Array.Empty<CardInstance>()),
                ActionKind.Skill,
                Components(board),
                declaredOutputs: new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                });

        private static MatchComponents Components(AdjacentMoveAuthority board) =>
            new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                board,
                new MarkerAttachmentRegister(),
                board,
                board,
                board,
                board);

        /// <summary>
        /// A card printing the Range this selection reads its reach from. A probe definition rather
        /// than Bain's, because he has no content until milestone 8.40i and the clause is not his
        /// alone - a later hero printing the same sentence takes the same declaration.
        /// </summary>
        private static CardDefinition PullDefinition(int printedRange) =>
            new CardDefinition(
                PullCard,
                "probe-rev-1",
                "Probe Pull",
                new InitiativeValue(13),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PullAction,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        printedRange > 0
                            ? new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Range, printedRange)
                            }
                            : Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        null,
                        // The capabilities under test are driven directly; this definition exists only
                        // to print the Range the reach is read from, which is milestone 8.39b's own
                        // reason for a Range-printing probe card that carries no root.
                        PrintedTextComposition.DeferredUntil(
                            "never - this is milestone 8.40e's Range-printing probe, not content"))
                });

        /// <summary>
        /// Every composed effect in the build that could name this family - each verified hero's
        /// content, derived from the content classes the assembly declares rather than from a list of
        /// heroes, plus the showcase hand that carries milestone 8.39i's obstacle Skill.
        /// </summary>
        private static IEnumerable<(string Owner, EffectDefinition Definition)> Corpus()
        {
            var contentClasses = typeof(GuardsDigital.Core.Cards.Verified.WaspCardContent).Assembly
                .GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace ==
                        typeof(GuardsDigital.Core.Cards.Verified.WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (var type in contentClasses)
            {
                var owner = type.Name
                    .Substring(0, type.Name.Length - "CardContent".Length)
                    .ToLowerInvariant();
                var property = type.GetProperty(
                    "AllDefinitions", BindingFlags.Public | BindingFlags.Static);
                Assert.That(property, Is.Not.Null,
                    $"{type.Name} declares no AllDefinitions, so this sweep would skip it silently.");

                foreach (var card in (IReadOnlyList<CardDefinition>)property.GetValue(null))
                {
                    foreach (var root in Roots(card))
                    {
                        yield return (owner, root);
                    }
                }
            }

            var showcase = GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                .CreateRedWithObstacleGrappleSkill(PullLineBoard.Actor);
            foreach (var card in showcase.Definitions)
            {
                foreach (var root in Roots(card))
                {
                    yield return ("showcase", root);
                }
            }
        }

        private static IEnumerable<EffectDefinition> Roots(CardDefinition card)
        {
            foreach (var action in card.ActionDefinitions)
            {
                if (action.EffectRoot != null)
                {
                    yield return action.EffectRoot;
                }

                if (action.SecondCardTextRoot != null)
                {
                    yield return action.SecondCardTextRoot;
                }
            }
        }

        /// <summary>
        /// A live modification set carrying one immunity, built the way the match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled, on milestone 8.39b's terms.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"pull-line-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"PullLine{activity}Immunity",
                $"pull-line-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"pull-line-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("pull-line-immunity-source"),
                new ActionDefinitionId("pull-line-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                StagedBoardParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null,
                null);
            Assert.That(
                set.IsImmuneTo(
                    RuleObject.ForHero(heroId), activity, RuleObject.ForHero(PullLineBoard.Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the cases above prove nothing.");
            return set;
        }
    }
}

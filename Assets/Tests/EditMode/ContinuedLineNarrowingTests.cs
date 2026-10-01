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
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39a. The two directional clauses Cutter's Red line prints, proved as vocabulary
    /// before any of her content exists.
    ///
    /// <para>Her three lunges print "Before the attack: Move 1 space in a straight line. Target a hero
    /// adjacent to you <b>in the direction of the move</b>", and her two ranged Reds print "After the
    /// attack: Move up to 2 spaces <b>in the opposite direction</b>". Neither is a property of the
    /// board or of the target: each reads a heading an earlier step of the same action fixed, which is
    /// why they are one block and one query - <see cref="ContinuedLineQuery"/> - rather than two
    /// geometries that would drift apart.</para>
    ///
    /// <para><b>Both are the same optional parameter on a declaration that already exists</b>, which is
    /// the discipline milestone 8.22's <c>adjacentToFriendlyMinion</c> records and milestone 8.39e
    /// inherits: a printed clause that narrows a declaration is carried by that declaration, optional,
    /// defaulting to the set every consumer written before this milestone already gets.
    /// <c>choose-target@2</c> carries three such narrowings already and this is its fourth;
    /// <c>choose-line-destination@1</c> carries <c>ignoresObstacles</c> and this is its second.</para>
    ///
    /// <para><b>The board is <see cref="ContinuedLineBoard"/>, featureless on purpose</b> - one zone, a
    /// radius-4 disk, no Spawn points, nobody on it but the pieces a case names - so the only things
    /// that can remove a candidate are the clause under test and the pieces the case puts down.</para>
    ///
    /// <para>No Cutter content here. These cases bind the parameter from probe nodes, because the
    /// milestone that composes her faces is 8.39h and an inventory row authorizes nothing.</para>
    /// </summary>
    [TestFixture]
    public sealed class ContinuedLineNarrowingTests
    {
        private static readonly ActionDefinitionId ProbeAction = new ActionDefinitionId("probe-line");
        private static readonly CardDefinitionId ProbeCard = new CardDefinitionId("probe-line-card");
        private static readonly CardInstanceId ProbeInstance =
            new CardInstanceId("probe-line-instance");

        /// <summary>
        /// Where the actor came from on a one-space move East: she left the space West of the origin,
        /// so the line runs West-of-origin -> origin -> East-of-origin.
        /// </summary>
        private static HexCoordinate CameFrom(int distance) =>
            ContinuedLineBoard.At(HexDirection.West, distance);

        private static HexCoordinate Ahead(int distance) =>
            ContinuedLineBoard.At(HexDirection.East, distance);

        // -----------------------------------------------------------------------------------------
        // The shared query
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// What both clauses actually ask, stated once. A line from one space through another has one
        /// heading; its continuation is the spaces strictly past the second, along that heading; and
        /// the near side is not on it, which is the whole difference between "the direction of the
        /// move" and "the opposite direction" read from the same pair of ends.
        /// </summary>
        [Test]
        public void TheContinuation_IsTheFarSideOfTheLineAndNothingElse()
        {
            var from = CameFrom(1);
            var through = ContinuedLineBoard.Origin;

            Assert.That(ContinuedLineQuery.TryGetHeading(from, through, out var heading), Is.True);
            Assert.That(heading, Is.EqualTo(HexDirection.East),
                "Leaving the space to the West and standing on the origin is a move East.");

            Assert.That(ContinuedLineQuery.ContinuesTheLine(from, through, Ahead(1)), Is.True);
            Assert.That(ContinuedLineQuery.ContinuesTheLine(from, through, Ahead(3)), Is.True,
                "The continuation is the whole file past the pivot, not only its first space.");
            Assert.That(ContinuedLineQuery.ContinuesTheLine(from, through, through), Is.False,
                "The pivot is not on its own continuation.");
            Assert.That(ContinuedLineQuery.ContinuesTheLine(from, through, from), Is.False,
                "and neither is the space the line came from.");
            Assert.That(
                ContinuedLineQuery.ContinuesTheLine(
                    from, through, ContinuedLineBoard.At(HexDirection.NorthEast, 1)),
                Is.False,
                "A space adjacent to the pivot on a different file is not a continuation of this one.");
        }

        /// <summary>
        /// A line of no length names no heading, and the answer is the empty set rather than every
        /// direction. This is the reading the lunge branch that performs no move depends on.
        /// </summary>
        [Test]
        public void ALineOfNoLength_NamesNoHeadingAtAll()
        {
            var origin = ContinuedLineBoard.Origin;

            Assert.That(ContinuedLineQuery.TryGetHeading(origin, origin, out _), Is.False);
            Assert.That(ContinuedLineQuery.TryGetContinuation(origin, origin, 1, out _), Is.False);
            Assert.That(ContinuedLineQuery.ContinuesTheLine(origin, origin, Ahead(1)), Is.False);
        }

        /// <summary>
        /// Two spaces that share no file name no heading either, which is what keeps a bent path from
        /// claiming a direction. p.19's own rule: a straight line is a sequence arranged in a file.
        /// </summary>
        [Test]
        public void APairSharingNoFile_NamesNoHeading()
        {
            var bent = new HexCoordinate(1, 1);

            Assert.That(StraightLineQuery.AreInStraightLine(ContinuedLineBoard.Origin, bent), Is.False,
                "The fixture's own premise: this pair is off every file through the origin.");
            Assert.That(
                ContinuedLineQuery.TryGetHeading(bent, ContinuedLineBoard.Origin, out _), Is.False);
        }

        // -----------------------------------------------------------------------------------------
        // "Target a hero adjacent to you in the direction of the move"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The narrowing, asserted as a set equality on a board where the unnarrowed clause holds
        /// several heroes. Brogan's charge admits the unit beside him in any of p.19's six directions;
        /// Cutter's lunge admits the one on the continuation of the heading she just travelled, which
        /// is exactly one space.
        /// </summary>
        [Test]
        public void TheDirectionNarrowing_OffersOnlyTheHeroOnTheContinuation()
        {
            var board = CrowdedBoard();

            Assert.That(AdjacentHeroes(board, null), Is.EquivalentTo(new[]
                {
                    ContinuedLineBoard.Enemies[0],
                    ContinuedLineBoard.Enemies[1],
                    ContinuedLineBoard.Enemies[2]
                }),
                "The premise: unnarrowed, three enemy heroes stand beside her.");

            Assert.That(AdjacentHeroes(board, CameFrom(1)),
                Is.EqualTo(new[] { ContinuedLineBoard.Enemies[0] }),
                "Narrowed, exactly the hero on the continuation of the heading just travelled - " +
                "not the one behind her and not the one off the file.");
        }

        /// <summary>
        /// The heading is what the clause reads, not the distance - so a move p.19 stopped after one
        /// space and a move that ran its whole printed distance along the same file answer alike. This
        /// is the degenerate case the block names: a lunge that never reached where it was aimed still
        /// travelled a direction.
        /// </summary>
        [Test]
        public void AMoveStoppedEarly_StillFixesTheHeadingItTravelled()
        {
            var board = CrowdedBoard();

            foreach (var travelled in new[] { 1, 2, 3 })
            {
                Assert.That(AdjacentHeroes(board, CameFrom(travelled)),
                    Is.EqualTo(new[] { ContinuedLineBoard.Enemies[0] }),
                    $"A {travelled}-space move along this file fixes the same heading.");
            }
        }

        /// <summary>
        /// The second branch of each lunge - "Target a unit adjacent to you" - performs no move, so it
        /// has no heading to read. A clause narrowing to the continuation of a line nobody travelled
        /// offers NOTHING rather than falling back to the unnarrowed set, which is the difference
        /// between the two bullets the card prints.
        /// </summary>
        [Test]
        public void ABranchThatPerformsNoMove_OffersNoDirectionalTargetRatherThanAllOfThem()
        {
            var board = CrowdedBoard();
            var context = TargetContext(board, ContinuedLineBoard.Origin);

            Assert.That(AdjacentHeroes(board, ContinuedLineBoard.Origin), Is.Empty);
            Assert.That(Handler(SyntheticRosterCapabilities.ChooseTargetV2).Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and it says so as an impossible step rather than as an empty question.");
        }

        /// <summary>
        /// One query, read three times. The set the offer opens with, the set the answer is checked
        /// against and the set a restored pause would re-derive are the same set - so a hero standing
        /// off the line cannot be submitted even though the unnarrowed clause would have offered them.
        /// </summary>
        [Test]
        public void TheOfferTheAnswerAndTheResolution_ReadTheOneQuery()
        {
            var board = CrowdedBoard();
            var handler = Handler(SyntheticRosterCapabilities.ChooseTargetV2);
            var context = TargetContext(board, CameFrom(1));

            var opened = handler.Execute(context);
            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(opened.Choice.Candidates.Select(candidate => candidate.HeroId),
                Is.EqualTo(new[] { ContinuedLineBoard.Enemies[0] }));
            Assert.That(handler.Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice));

            Assert.That(
                handler.Resume(
                    context, new[] { EffectValue.Hero(ContinuedLineBoard.Enemies[0]) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The hero on the continuation is the answer the offer named.");
            Assert.That(
                handler.Resume(
                    context, new[] { EffectValue.Hero(ContinuedLineBoard.Enemies[1]) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A hero the unnarrowed clause WOULD have offered is refused by the same query that " +
                "left them out of the offer, rather than by a second rule written beside it.");
        }

        // -----------------------------------------------------------------------------------------
        // "Move up to N spaces in the opposite direction"
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The retreat, asserted as a set equality against the unnarrowed line. "The opposite
        /// direction" is one of the six files this offer already walks, named by the Attack rather than
        /// chosen again - so the narrowed set is that file's spaces and the unnarrowed one is all six
        /// files'.
        /// </summary>
        [Test]
        public void TheOppositeDirectionMove_TravelsOnlyTheReverseOfTheAttacksOwnLine()
        {
            var board = ContinuedLineBoard.CreateBoard();

            var everyDirection = Destinations(board, maximum: 3, awayFrom: null);
            Assert.That(everyDirection.Count, Is.EqualTo(18),
                "The premise: unnarrowed, three spaces along each of p.19's six files.");

            Assert.That(Destinations(board, maximum: 3, awayFrom: CameFrom(1)),
                Is.EqualTo(new[] { Ahead(1), Ahead(2), Ahead(3) }.OrderBy(space => space.Q)
                    .ThenBy(space => space.R).ToArray()),
                "Narrowed, only the file that continues the attack's own line past her.");
        }

        /// <summary>
        /// The narrowing is applied to the walked route, not to the six headings, so a retreat still
        /// stops at the first thing p.19 stops it at - and stopping short is not the same as being
        /// offered nothing.
        /// </summary>
        [Test]
        public void TheRetreat_StopsAtTheFirstThingThatStopsIt()
        {
            var blockedByAMinion = ContinuedLineBoard.CreateBoard(
                Array.Empty<HexCoordinate>(), new[] { Ahead(2) }, Array.Empty<HexCoordinate>());
            Assert.That(Destinations(blockedByAMinion, maximum: 3, awayFrom: CameFrom(1)),
                Is.EqualTo(new[] { Ahead(1) }),
                "A unit two spaces along the retreat ends it after one, rather than being stepped " +
                "over to reach the printed distance.");

            var blockedByAnObstacle = ContinuedLineBoard.CreateBoard(
                Array.Empty<HexCoordinate>(), Array.Empty<HexCoordinate>(), new[] { Ahead(2) });
            Assert.That(Destinations(blockedByAnObstacle, maximum: 3, awayFrom: CameFrom(1)),
                Is.EqualTo(new[] { Ahead(1) }),
                "and p.18's obstacle stops it on the same terms, because this clause declares no " +
                "crossing of its own.");
        }

        /// <summary>
        /// "Up to" is the declaring node's policy and this handler does not decide it: the candidate
        /// set is the same whichever policy asks, and a retreat with nowhere to go reports an
        /// impossible step, which an <c>up to</c> node turns into a step simply not taken.
        /// </summary>
        [Test]
        public void TheRetreat_IsDeclinableBecauseThePolicyIsTheNodes()
        {
            var board = ContinuedLineBoard.CreateBoard();
            var mandatory = LineContext(board, EffectFailurePolicy.Mandatory, 3, CameFrom(1));
            var upTo = LineContext(board, EffectFailurePolicy.UpTo, 3, CameFrom(1));
            var handler = Handler(SyntheticRosterCapabilities.ChooseLineDestinationV1);

            Assert.That(
                handler.Execute(upTo).Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EqualTo(
                    handler.Execute(mandatory).Choice.Candidates
                        .Select(candidate => candidate.Coordinate)),
                "The clause narrows who may be offered; whether the step may be declined stays the " +
                "node's own word, exactly as it was before this milestone.");

            var walledIn = ContinuedLineBoard.CreateBoard(
                Array.Empty<HexCoordinate>(), new[] { Ahead(1) }, Array.Empty<HexCoordinate>());
            Assert.That(
                handler.Anticipate(LineContext(walledIn, EffectFailurePolicy.UpTo, 3, CameFrom(1))),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and a retreat with nowhere to go is an empty offer, which the node's own up-to " +
                "policy turns into a step not taken.");
        }

        /// <summary>
        /// An Attack that named no line to retreat along - the far space equal to the actor's own -
        /// offers nothing, on the same reading the lunge's no-move branch takes.
        /// </summary>
        [Test]
        public void ARetreatFromNowhere_OffersNothing()
        {
            var board = ContinuedLineBoard.CreateBoard();

            Assert.That(
                Destinations(board, maximum: 3, awayFrom: ContinuedLineBoard.Origin), Is.Empty);
        }

        /// <summary>
        /// Milestone 9.35, from live play: "Cutters evasive shot should give option to move 0
        /// spaces". "Up to" makes 0 a real choice (W5), so a floor BOUND to nought offers the mover's
        /// own space beside the spaces along the file, and answering it names that space.
        /// </summary>
        [Test]
        public void AFloorBoundToNought_OffersStandingStillBesideTheFile()
        {
            var board = ContinuedLineBoard.CreateBoard();
            var handler = Handler(SyntheticRosterCapabilities.ChooseLineDestinationV1);
            var context = LineContext(
                board, EffectFailurePolicy.UpTo, 3, CameFrom(1), minimumDistance: 0);

            Assert.That(
                handler.Execute(context).Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EqualTo(new[] { ContinuedLineBoard.Origin, Ahead(1), Ahead(2), Ahead(3) }
                    .OrderBy(space => space.Q).ThenBy(space => space.R).ToArray()),
                "W5: moving 0 spaces is offered as her own space, beside 1..3 along the file.");
            Assert.That(
                handler.Resume(context, new[] { EffectValue.Space(ContinuedLineBoard.Origin) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "and answering it is an accepted answer.");
            Assert.That(Destinations(board, maximum: 3, awayFrom: CameFrom(1)),
                Has.No.Member(ContinuedLineBoard.Origin),
                "An unbound floor is still one: every body written before 9.35 is unchanged.");
        }

        /// <summary>
        /// Milestone 9.35. With no space along the file, standing still is not offered on its own: there
        /// is nothing to choose between, so the empty offer stays the step not taken it was.
        /// </summary>
        [Test]
        public void AWalledInRetreat_WithAFloorOfNought_IsStillAStepNotTaken()
        {
            var walledIn = ContinuedLineBoard.CreateBoard(
                Array.Empty<HexCoordinate>(), new[] { Ahead(1) }, Array.Empty<HexCoordinate>());

            Assert.That(
                Handler(SyntheticRosterCapabilities.ChooseLineDestinationV1).Anticipate(
                    LineContext(walledIn, EffectFailurePolicy.UpTo, 3, CameFrom(1), minimumDistance: 0)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        /// <summary>
        /// The retreat's half of the same claim the lunge's makes: the set the offer opens with and the
        /// set the answer is checked against are one set, so a space on another file cannot be
        /// submitted even though the unnarrowed clause would have offered it.
        /// </summary>
        [Test]
        public void TheRetreatsOfferAndItsAnswer_ReadTheOneQuery()
        {
            var handler = Handler(SyntheticRosterCapabilities.ChooseLineDestinationV1);
            var context = LineContext(
                ContinuedLineBoard.CreateBoard(), EffectFailurePolicy.UpTo, 3, CameFrom(1));

            Assert.That(
                handler.Resume(context, new[] { EffectValue.Space(Ahead(2)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "A space on the continuation is the answer the offer named.");
            Assert.That(
                handler.Resume(
                    context,
                    new[]
                    {
                        EffectValue.Space(ContinuedLineBoard.At(HexDirection.NorthEast, 2))
                    }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and a space the unnarrowed clause WOULD have offered is refused by the same query " +
                "that left it out of the offer.");
        }

        // -----------------------------------------------------------------------------------------
        // Nothing written before this milestone moves
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The claim every optional narrowing in this catalog owes: a consumer that does not bind the
        /// parameter gets exactly the set it got before the parameter existed. Both declarations are
        /// asserted, because this milestone widened both.
        /// </summary>
        [Test]
        public void ADeclarationThatBindsNothing_KeepsItsExactCandidateSet()
        {
            var board = CrowdedBoard();

            Assert.That(AdjacentHeroes(board, null), Is.EquivalentTo(new[]
                {
                    ContinuedLineBoard.Enemies[0],
                    ContinuedLineBoard.Enemies[1],
                    ContinuedLineBoard.Enemies[2]
                }),
                "choose-target@2 unbound is still p.19's whole adjacency.");
            Assert.That(
                Destinations(ContinuedLineBoard.CreateBoard(), maximum: 3, awayFrom: null).Count,
                Is.EqualTo(18),
                "and choose-line-destination@1 unbound is still every straight-line destination the " +
                "walk reaches - three spaces along each of p.19's six files.");
        }

        /// <summary>
        /// Brogan's three charges are the faces this milestone could have moved without meaning to:
        /// each of them ends in a <c>choose-target@2</c> node, and that is the declaration widened
        /// here. None of them binds the new parameter, so each still admits the unit beside him in any
        /// of p.19's six directions.
        ///
        /// <para>Asserted over the definitions themselves rather than over a remembered list, so a
        /// later edit that bound the parameter on one of his nodes would fail here rather than change
        /// a shipped face quietly.</para>
        /// </summary>
        [Test]
        public void BrogansThreeCharges_BindNoDirectionalNarrowing()
        {
            var charges = new[]
            {
                BroganCardContent.MadDash, BroganCardContent.Bullrush, BroganCardContent.FuriousCharge
            };

            foreach (var charge in charges)
            {
                var definition = BroganEffectContent.AttackTextFor(charge, "probe-rev-1");
                Assert.That(definition, Is.Not.Null, $"{charge} composes an attack text.");

                var targeting = definition.Nodes
                    .Where(node => node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetV2))
                    .ToArray();
                Assert.That(targeting, Is.Not.Empty,
                    $"{charge} ends in the selection this milestone widened, which is why it is here.");
                Assert.That(
                    targeting.SelectMany(node => node.Inputs)
                        .Select(input => input.ParameterName),
                    Has.None.EqualTo(SyntheticRosterCapabilities.ContinuedLineFromParameter),
                    $"{charge} prints no directional clause and binds none, so its candidate set is " +
                    "what it was before milestone 8.39a.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Fixture
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Three enemy heroes beside the actor: one on the continuation of the heading she travelled,
        /// one on a different file, and one directly behind her on the same file. The third is the
        /// candidate a narrowing that read MEMBERSHIP of a file rather than a DIRECTION along it would
        /// wrongly keep.
        /// </summary>
        private static AdjacentMoveAuthority CrowdedBoard() =>
            ContinuedLineBoard.CreateBoard(
                Ahead(1),
                ContinuedLineBoard.At(HexDirection.NorthEast, 1),
                CameFrom(1));

        private static IReadOnlyList<HeroId> AdjacentHeroes(
            AdjacentMoveAuthority board, HexCoordinate? cameFrom)
        {
            var opened = Handler(SyntheticRosterCapabilities.ChooseTargetV2)
                .Execute(TargetContext(board, cameFrom));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HeroId>()
                : opened.Choice.Candidates.Select(candidate => candidate.HeroId).ToArray();
        }

        private static IReadOnlyList<HexCoordinate> Destinations(
            AdjacentMoveAuthority board, int maximum, HexCoordinate? awayFrom)
        {
            var opened = Handler(SyntheticRosterCapabilities.ChooseLineDestinationV1)
                .Execute(LineContext(board, EffectFailurePolicy.Mandatory, maximum, awayFrom));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HexCoordinate>()
                : opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(handler => handler.Reference.Equals(reference));

        private static EffectCapabilityContext TargetContext(
            AdjacentMoveAuthority board, HexCoordinate? cameFrom)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] =
                    EffectValue.Integer((int)RosterQueryOrigin.Actor),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(1),
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Hero),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            };
            if (cameFrom.HasValue)
            {
                inputs[SyntheticRosterCapabilities.ContinuedLineFromParameter] =
                    EffectValue.Space(cameFrom.Value);
            }

            return Context(board, EffectFailurePolicy.Mandatory, inputs);
        }

        private static EffectCapabilityContext LineContext(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            int maximum,
            HexCoordinate? awayFrom,
            int? minimumDistance = null)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["maximumDistance"] = EffectValue.Integer(maximum)
            };
            if (minimumDistance.HasValue)
            {
                inputs["minimumDistance"] = EffectValue.Integer(minimumDistance.Value);
            }

            if (awayFrom.HasValue)
            {
                inputs[SyntheticRosterCapabilities.ContinuedLineFromParameter] =
                    EffectValue.Space(awayFrom.Value);
            }

            return Context(board, policy, inputs);
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            EffectFailurePolicy policy,
            IReadOnlyDictionary<string, EffectValue> inputs) =>
            new EffectCapabilityContext(
                new EffectNodeId("probe-line-node"),
                policy,
                ContinuedLineBoard.Actor,
                ContinuedLineBoard.Actor,
                ProbeInstance,
                new CardDefinitionReference(ProbeCard, "probe-rev-1"),
                ProbeAction,
                inputs,
                1,
                1,
                ContinuedLineBoard.Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Attack,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39e. The charge line Cutter's three Green faces need, proved as vocabulary before
    /// any of her content exists: "Move 3 spaces in a straight line, <b>ignoring obstacles</b>, to a
    /// space adjacent to an enemy hero; that hero discards a card, if able."
    ///
    /// <para><b>The milestone builds one optional parameter, because the rest of that sentence was
    /// already registered.</b> <c>choose-charge-line@1</c> is Brogan's Mad Dash - "Move 2 spaces in a
    /// straight line to a space adjacent to an enemy unit, then target that unit" - and its schema has
    /// carried <c>minimumDistance</c> and <c>maximumDistance</c> as separate integers since milestone
    /// 8.19b, so "3" is <c>min 3, max 3</c> and "3, 4 or 5" is <c>min 3, max 5</c>; its
    /// <c>unitKind</c> domain already admits <see cref="RosterUnitKind.Hero"/>, which is the value
    /// Ursafar's Gold binds today. The block was drafted to mint a destination-constrained move and
    /// must not: reading the catalog's PARAMETERS rather than its capability names is what found
    /// that, for the second time in this series.</para>
    ///
    /// <para><b>The crossing permission reaches the transit predicate and nothing else.</b> p.18's
    /// second sentence still forbids stopping on an obstacle, so a crossing charge has exactly the
    /// same endpoints available to it as an ordinary one - it can simply get to more of them. That is
    /// why <see cref="CrossingObstacles_DoesNotWaiveTheAdjacencyTheClausePrints"/> is a case and not a
    /// remark.</para>
    ///
    /// <para><b>What "that unit" and "that hero" name is settled here, and the answer is that this
    /// node names nothing.</b> See
    /// <see cref="TheNodeNamesNoUnit_SoASecondSelectionAsksWhichHeroTheConsequenceReads"/>.</para>
    ///
    /// <para>Unity-free on purpose, so the cross-platform harness and the Edit Mode assembly both run
    /// it.</para>
    /// </summary>
    [TestFixture]
    public sealed class ChargeLineObstacleCrossingTests
    {
        private static readonly MapId Map = new MapId("charge-crossing-board");
        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);
        private const int MapRadius = 5;

        private static readonly HeroId Actor = new HeroId("charge-crossing-actor");

        /// <summary>Beside the East file's distance-4 space, and beside its distance-5 space.</summary>
        private static readonly HeroId Marked = new HeroId("charge-crossing-marked");

        /// <summary>Beside the NorthEast file's distance-3 space.</summary>
        private static readonly HeroId Near = new HeroId("charge-crossing-near");

        /// <summary>Beside the West file's distance-1 space, and beside nothing in bounds.</summary>
        private static readonly HeroId TooNear = new HeroId("charge-crossing-too-near");

        /// <summary>Beside spaces that lie on no straight file from the actor.</summary>
        private static readonly HeroId OffAxis = new HeroId("charge-crossing-off-axis");

        private static readonly HeroId[] EnemyHeroes = { Marked, Near, TooNear, OffAxis };

        private static readonly TeamId RedTeam = new TeamId("charge-crossing-red");
        private static readonly TeamId BlueTeam = new TeamId("charge-crossing-blue");

        private static HexCoordinate At(HexDirection direction, int steps) =>
            StraightLineQuery.Step(Origin, direction, steps);

        // -------------------------------------------------------------------------------------------
        // The clause, unchanged: what a charge line has always offered
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The set equality the block asks for, on a board where each printed condition excludes a
        /// different space and every exclusion is named by its own witness below. Bounds are 3 to 4,
        /// which is the shape Cutter's Ramming Speed prints ("Move 3 or 4 spaces").
        /// </summary>
        [Test]
        public void TheOffer_AdmitsOnlyStraightDestinationsInBoundsBesideAnEnemyHero()
        {
            var board = WitnessBoard();

            Assert.That(
                Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Hero),
                Is.EqualTo(new[] { At(HexDirection.NorthEast, 3), At(HexDirection.East, 4) }),
                "Exactly two spaces satisfy all three printed conditions at once.");
        }

        /// <summary>
        /// The distance witnesses, one on each side of the printed bounds. Both lie on a straight file
        /// and both have an enemy hero beside them, so distance is the only thing that excludes them.
        /// </summary>
        [Test]
        public void ASpaceOutsideThePrintedBounds_IsNotOfferedThoughItSatisfiesEveryOtherCondition()
        {
            var board = WitnessBoard();
            var tooNear = At(HexDirection.West, 1);
            var tooFar = At(HexDirection.East, 5);

            Assert.That(EnemyHeroesBeside(board, tooNear), Is.Not.Empty,
                "The premise: the West file's first space has an enemy hero beside it.");
            Assert.That(EnemyHeroesBeside(board, tooFar), Is.Not.Empty,
                "The premise: the East file's fifth space has an enemy hero beside it.");

            var offered = Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Hero);
            Assert.That(offered, Has.No.Member(tooNear),
                "One space short of the printed minimum is not a shorter charge, it is no charge.");
            Assert.That(offered, Has.No.Member(tooFar),
                "and one space past the printed maximum is not offered either.");
        }

        /// <summary>
        /// The straight-line witness. The space is inside the bounds and has an enemy hero beside it;
        /// it simply lies on no file running out of the actor's own space.
        /// </summary>
        [Test]
        public void ASpaceOffEveryStraightFile_IsNotOfferedThoughItSatisfiesEveryOtherCondition()
        {
            var board = WitnessBoard();
            var offAxis = new HexCoordinate(1, 2);

            Assert.That(Distance(Origin, offAxis), Is.EqualTo(3),
                "The premise: it is inside the printed bounds.");
            Assert.That(EnemyHeroesBeside(board, offAxis), Is.Not.Empty,
                "The premise: it has an enemy hero beside it.");
            Assert.That(
                StraightLineQuery.TryGetDirection(Origin, offAxis, out _, out _), Is.False,
                "and the premise that makes it the witness: no straight file reaches it.");

            Assert.That(
                Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Hero),
                Has.No.Member(offAxis));
        }

        /// <summary>
        /// The adjacency witness. The East file's distance-3 space is reachable, straight and in
        /// bounds, and is excluded only because nobody is standing beside it.
        /// </summary>
        [Test]
        public void AStraightDestinationWithNobodyBesideIt_IsNotOffered()
        {
            var board = WitnessBoard();
            var empty = At(HexDirection.East, 3);

            Assert.That(EnemyHeroesBeside(board, empty), Is.Empty,
                "The premise: the space that fails only the adjacency clause.");
            Assert.That(
                Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Hero),
                Has.No.Member(empty));
        }

        /// <summary>
        /// The noun the card prints is the noun the offer reads. Cutter's Greens and Ursafar's Gold say
        /// "an enemy HERO"; Brogan's three charges say "an enemy UNIT", which p.19 defines as a hero or
        /// a minion. Both are values <c>unitKind</c>'s domain already admitted, and this is the case
        /// that shows they are not the same question: one enemy minion on the board moves two spaces
        /// into the offer.
        /// </summary>
        [Test]
        public void TheUnitKind_SeparatesTheHeroNounFromTheUnitNoun()
        {
            var board = WitnessBoard();
            var besideTheMinion = new[] { At(HexDirection.West, 3), At(HexDirection.West, 4) };

            Assert.That(
                Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Hero)
                    .Intersect(besideTheMinion),
                Is.Empty,
                "An enemy minion is not an enemy hero, so the West file offers nothing to a face " +
                "that prints the hero noun.");
            Assert.That(
                Offer(board, minimum: 3, maximum: 4, kind: RosterUnitKind.Unit),
                Is.EqualTo(new[]
                {
                    At(HexDirection.West, 4),
                    At(HexDirection.West, 3),
                    At(HexDirection.NorthEast, 3),
                    At(HexDirection.East, 4)
                }),
                "and the same board offers both of those spaces to a face that prints the unit noun.");
        }

        /// <summary>
        /// The exact-distance arm, which is how "Move 3 spaces" is stated: the two bounds are equal.
        /// A distance nothing qualifies at offers nothing, rather than falling back to the shorter
        /// move that would have qualified - proved on the one board where the fall-back would have
        /// succeeded, so the case cannot pass by there being nothing to fall back to.
        /// </summary>
        [Test]
        public void TheExactDistanceArm_OffersNothingRatherThanFallingBackToAShorterMove()
        {
            var board = CreateBoard(new Dictionary<HeroId, HexCoordinate>
            {
                [Near] = new HexCoordinate(2, -3)
            });

            Assert.That(
                Offer(board, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero),
                Is.EqualTo(new[] { At(HexDirection.NorthEast, 3) }),
                "The premise: exactly three spaces qualifies.");
            Assert.That(
                Offer(board, minimum: 4, maximum: 4, kind: RosterUnitKind.Hero), Is.Empty,
                "and exactly four qualifies nowhere, so the charge is refused rather than quietly " +
                "becoming the three-space one the board would have allowed.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.39e: the crossing
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The parameter this milestone builds, on the board milestone 8.35p's
        /// <c>ChargeLine_RejectsAStraightDestinationWhoseRouteIsBlocked</c> already uses: terrain one
        /// step along the East file, and the destination three steps along it.
        /// </summary>
        [Test]
        public void CrossingObstacles_ReachesADestinationTheBlockedRouteCouldNot()
        {
            var board = CreateBoard(
                new Dictionary<HeroId, HexCoordinate> { [Marked] = new HexCoordinate(4, 0) },
                terrain: new[] { new HexCoordinate(1, 0) });
            var destination = At(HexDirection.East, 3);

            Assert.That(
                Offer(board, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero), Is.Empty,
                "The premise: the only qualifying destination sits behind an obstacle, so a charge " +
                "that prints no crossing has nowhere to go.");
            Assert.That(
                Offer(board, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero, crossing: true),
                Is.EqualTo(new[] { destination }),
                "and a charge whose own text ignores obstacles reaches it.");
        }

        /// <summary>
        /// p.18's two sentences are separate, and the parameter only carries the first. A crossing
        /// charge may pass through an obstacle; it still may not stop beside nobody, and it still may
        /// not stop on an obstacle.
        /// </summary>
        [Test]
        public void CrossingObstacles_DoesNotWaiveTheAdjacencyTheClausePrints()
        {
            var board = CreateBoard(
                new Dictionary<HeroId, HexCoordinate> { [OffAxis] = new HexCoordinate(2, 2) },
                terrain: new[] { new HexCoordinate(1, 0) });

            Assert.That(
                Offer(board, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero, crossing: true),
                Is.Empty,
                "The one enemy hero on this board stands beside no straight destination at the " +
                "printed distance, and crossing obstacles does not make it stand beside one.");
        }

        /// <summary>
        /// The discipline every optional parameter in this catalog is added under: a declaration that
        /// does not bind it keeps the exact candidate set it had. Asserted on the board where the two
        /// readings differ, so it cannot pass by the parameter making no difference anywhere.
        /// </summary>
        [Test]
        public void LeavingTheParameterUnbound_IsTheSameOfferAsBindingItFalse()
        {
            var blocked = CreateBoard(
                new Dictionary<HeroId, HexCoordinate> { [Marked] = new HexCoordinate(4, 0) },
                terrain: new[] { new HexCoordinate(1, 0) });

            Assert.That(
                Offer(blocked, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero, crossing: false),
                Is.EqualTo(Offer(blocked, minimum: 3, maximum: 3, kind: RosterUnitKind.Hero)));
            Assert.That(
                Offer(WitnessBoard(), minimum: 3, maximum: 4, kind: RosterUnitKind.Hero,
                    crossing: false),
                Is.EqualTo(Offer(WitnessBoard(), minimum: 3, maximum: 4, kind: RosterUnitKind.Hero)));
        }

        /// <summary>
        /// The other half of that discipline, read off the shipped corpus rather than off this
        /// fixture: Brogan's Mad Dash, Bullrush and Furious Charge and Ursafar's Gold basic are the
        /// declarations that existed before this milestone, and none of them binds the new parameter,
        /// so none of their candidate sets can have moved.
        ///
        /// <para>The corpus is derived from the verified content classes the assembly contains rather
        /// than listed, on milestone 8.38d's precedent: a fourteenth hero who binds the crossing is
        /// supposed to appear here, and a hero this check simply forgot is not.</para>
        ///
        /// <para><b>Milestone 8.39h is the fourteenth hero arriving, and this case caught her rather
        /// than being told about her.</b> Cutter's Brace for Impact, Ramming Speed and Crashland are
        /// the faces this parameter was built for, so the premise is now a PARTITION: her three bind
        /// the crossing and every declaration written before 8.39e binds the same three parameters it
        /// was written with. "A new name here is a hero to check" was the instruction, and the check is
        /// that hers bind it TRUE while the four before hers bind it at all.</para>
        /// </summary>
        [Test]
        public void EveryChargeDeclarationWrittenBeforeThisMilestone_BindsNoCrossing()
        {
            var nodes = VerifiedCorpus()
                .SelectMany(entry => entry.Definition.Nodes
                    .Where(node => SyntheticRosterCapabilities.ChooseChargeLineV1
                        .Equals(node.Capability))
                    .Select(node => (entry.Owner, entry.Definition.Id, node)))
                .ToArray();

            Assert.That(nodes.Select(entry => entry.Owner).Distinct().OrderBy(value => value),
                Is.EqualTo(new[] { "brogan", "cutter", "ursafar" }),
                "The premise: these are the heroes whose printed cards name the charge line today. " +
                "A new name here is a hero to check, not a failure to suppress.");
            Assert.That(nodes.Length, Is.EqualTo(7),
                "Brogan's three charges, Ursafar's one, and Cutter's three Green charges.");

            foreach (var entry in nodes.Where(entry => entry.Owner != "cutter"))
            {
                Assert.That(
                    entry.node.Inputs.Select(input => input.ParameterName)
                        .OrderBy(value => value, StringComparer.Ordinal),
                    Is.EqualTo(new[]
                    {
                        "maximumDistance",
                        "minimumDistance",
                        SyntheticRosterCapabilities.UnitKindParameter
                    }),
                    $"{entry.Owner}'s {entry.Id.Value} binds the three parameters it was written " +
                    "with and no fourth.");
            }

            var crossing = nodes.Where(entry => entry.Owner == "cutter").ToArray();

            Assert.That(crossing.Length, Is.EqualTo(3));
            foreach (var entry in crossing)
            {
                Assert.That(
                    entry.node.Inputs.Select(input => input.ParameterName)
                        .OrderBy(value => value, StringComparer.Ordinal),
                    Is.EqualTo(new[]
                    {
                        SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                        "maximumDistance",
                        "minimumDistance",
                        SyntheticRosterCapabilities.UnitKindParameter
                    }.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                    $"{entry.Id.Value} prints \"ignoring obstacles\" and binds it.");
                Assert.That(
                    entry.node.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticRosterCapabilities.IgnoresObstaclesParameter)
                        .Literal.BooleanValue,
                    Is.True, entry.Id.Value);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.39e: what "that unit" and "that hero" name
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE BLOCK'S OPEN DESIGN QUESTION, SETTLED. Brogan prints "then target that unit" and
        /// Cutter prints "that hero discards a card", and a destination adjacent to two enemy heroes
        /// is a board state both reach.
        ///
        /// <para><b>The answer is that this node has no such hero to read.</b> Its offer is a set of
        /// destinations - the qualifying units decide only whether a destination is offered at all,
        /// and are then collapsed away - so the one destination beside two enemy heroes is offered
        /// once rather than twice, and answering it publishes a space and nothing else. The subject is
        /// named afterwards by the ordinary radius-1 <c>choose-target@2</c> that Brogan's composition
        /// has asked since milestone 8.35p, measured from where the hero now stands, and on this board
        /// that selection offers both heroes and the mover chooses.</para>
        ///
        /// <para><b>The alternative reading was to have the charge choose against a named unit and
        /// publish it, and it is refused for a stated reason</b>: that is exactly the second question
        /// milestone 8.35p took OUT of this node, on the user's rule that "they move as soon as they
        /// select, and then they attack". Putting it back to disambiguate an adverb would name the
        /// victim while the hero authoritatively still stood on his starting space.</para>
        /// </summary>
        [Test]
        public void TheNodeNamesNoUnit_SoASecondSelectionAsksWhichHeroTheConsequenceReads()
        {
            var first = new HeroId("charge-crossing-first");
            var second = new HeroId("charge-crossing-second");
            var destination = At(HexDirection.East, 2);
            var board = CreateBoard(new Dictionary<HeroId, HexCoordinate>
            {
                [first] = new HexCoordinate(3, 0),
                [second] = new HexCoordinate(2, 1)
            });

            Assert.That(EnemyHeroesBeside(board, destination),
                Is.EquivalentTo(new[] { first, second }),
                "The premise: one destination, two enemy heroes beside it.");

            var handler = Handler();
            var context = Context(board, Inputs(minimum: 2, maximum: 2, kind: RosterUnitKind.Hero));
            var opened = handler.Execute(context);

            Assert.That(
                opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray(),
                Is.EqualTo(new[] { destination }),
                "It is one question about a space, asked once, not one per unit standing beside it.");

            var answered = handler.Resume(context, new[] { EffectValue.Space(destination) });
            Assert.That(answered.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(answered.Outcomes.Select(fact => fact.OutputName), Is.EqualTo(new[] { "destination" }),
                "and the whole of its result is the space, so there is no unit here for a " +
                "consequence to read.");

            var moved = CreateBoard(
                new Dictionary<HeroId, HexCoordinate>
                {
                    [first] = new HexCoordinate(3, 0),
                    [second] = new HexCoordinate(2, 1)
                },
                actorAt: destination);

            Assert.That(SubjectsBesideTheActor(moved), Is.EquivalentTo(new[] { first, second }),
                "The selection that follows the Move offers both, which is the mover's choice to " +
                "make - and it is the same disk the charge measured, because the hero is standing " +
                "on the space it measured from.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The witness board. Featureless but for the four enemy heroes and the one enemy minion each
        /// case names, so nothing but the clause under test removes a candidate.
        ///
        /// <list type="bullet">
        /// <item><c>Marked</c> at (5,-1) is beside the East file's distance-4 space, which qualifies,
        /// and beside its distance-5 space, which is past the printed maximum.</item>
        /// <item><c>Near</c> at (2,-3) is beside the NorthEast file's distance-3 space, which
        /// qualifies.</item>
        /// <item><c>TooNear</c> at (-1,-1) is beside the West file's distance-1 space and beside no
        /// space in bounds.</item>
        /// <item><c>OffAxis</c> at (2,2) is beside spaces that lie on no straight file.</item>
        /// <item>One enemy minion at (-4,1) is beside the West file's distance-3 and distance-4
        /// spaces, which the hero noun excludes and the unit noun admits.</item>
        /// </list>
        /// </summary>
        private static AdjacentMoveAuthority WitnessBoard() =>
            CreateBoard(
                new Dictionary<HeroId, HexCoordinate>
                {
                    [Marked] = new HexCoordinate(5, -1),
                    [Near] = new HexCoordinate(2, -3),
                    [TooNear] = new HexCoordinate(-1, -1),
                    [OffAxis] = new HexCoordinate(2, 2)
                },
                enemyMinionsAt: new[] { new HexCoordinate(-4, 1) });

        private static IReadOnlyList<HexCoordinate> Offer(
            AdjacentMoveAuthority board, int minimum, int maximum, RosterUnitKind kind,
            bool? crossing = null)
        {
            var opened = Handler().Execute(
                Context(board, Inputs(minimum, maximum, kind, crossing)));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HexCoordinate>()
                : opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static IEffectCapabilityHandler Handler() =>
            SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(value =>
                    value.Reference.Equals(SyntheticRosterCapabilities.ChooseChargeLineV1));

        private static Dictionary<string, EffectValue> Inputs(
            int minimum, int maximum, RosterUnitKind kind, bool? crossing = null)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["minimumDistance"] = EffectValue.Integer(minimum),
                ["maximumDistance"] = EffectValue.Integer(maximum),
                [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)kind)
            };
            if (crossing.HasValue)
            {
                inputs[SyntheticRosterCapabilities.IgnoresObstaclesParameter] =
                    EffectValue.Boolean(crossing.Value);
            }

            return inputs;
        }

        /// <summary>
        /// The enemy heroes an ordinary radius-1 <c>choose-target@2</c> would offer from where the
        /// actor stands - the selection Brogan's charge asks after its Move.
        /// </summary>
        private static IReadOnlyList<HeroId> SubjectsBesideTheActor(AdjacentMoveAuthority board)
        {
            var handler = SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(value => value.Reference.Equals(SyntheticRosterCapabilities.ChooseTargetV2));
            var opened = handler.Execute(Context(board, new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] =
                    EffectValue.Integer((int)RosterQueryOrigin.Actor),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(1),
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Hero),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            }));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HeroId>()
                : opened.Choice.Candidates.Select(candidate => candidate.HeroId).ToArray();
        }

        private static IReadOnlyList<HeroId> EnemyHeroesBeside(
            AdjacentMoveAuthority board, HexCoordinate at) =>
            board.Occupancies
                .Where(hero => !hero.HeroId.Equals(Actor) && Distance(at, hero.Coordinate) == 1)
                .Select(hero => hero.HeroId)
                .ToArray();

        private static int Distance(HexCoordinate from, HexCoordinate to)
        {
            var q = to.Q - from.Q;
            var r = to.R - from.R;
            return (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, IReadOnlyDictionary<string, EffectValue> inputs) =>
            new EffectCapabilityContext(
                new EffectNodeId("charge-crossing-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId("charge-crossing-instance"),
                new CardDefinitionReference(
                    new CardDefinitionId("charge-crossing-card"), "charge-crossing-rev-1"),
                new ActionDefinitionId("charge-crossing-action"),
                inputs,
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board));

        private static AdjacentMoveAuthority CreateBoard(
            IReadOnlyDictionary<HeroId, HexCoordinate> enemyHeroes,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            IReadOnlyList<HexCoordinate> terrain = null,
            HexCoordinate? actorAt = null)
        {
            var terrainSet = new HashSet<HexCoordinate>(terrain ?? Array.Empty<HexCoordinate>());
            var playable = Disk(MapRadius).Where(at => !terrainSet.Contains(at)).ToArray();
            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(Actor, actorAt ?? Origin)
            };
            heroes.AddRange(enemyHeroes.Select(pair => new HeroOccupancy(pair.Key, pair.Value)));

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrainSet),
                heroes,
                Array.Empty<ObstacleOccupancy>(),
                (enemyMinionsAt ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => Minion(index, at))
                    .ToArray());
        }

        private static QuickGameMinionSetupState Minion(int index, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId($"charge-crossing-minion-{index:00}"),
                BlueTeam,
                TeamSide.Blue,
                StandardMinionType.Melee,
                default,
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        /// <summary>
        /// One player per hero, which is what <see cref="MatchParticipants"/> requires, and every
        /// enemy this fixture can name whether or not a given board places them.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var actorPlayer = new PlayerId("charge-crossing-actor-player");
            var players = new List<Player> { new Player(actorPlayer, RedTeam) };
            var heroes = new List<Hero>
            {
                new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout)
            };

            foreach (var enemy in EnemyHeroes
                .Concat(new[]
                {
                    new HeroId("charge-crossing-first"),
                    new HeroId("charge-crossing-second")
                }))
            {
                var player = new PlayerId($"{enemy.Value}-player");
                players.Add(new Player(player, BlueTeam));
                heroes.Add(new Hero(enemy, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        /// <summary>
        /// Every composed `EffectDefinition` in the build's verified content, derived from the content
        /// classes the assembly declares rather than from a list of heroes.
        /// </summary>
        private static IEnumerable<(string Owner, EffectDefinition Definition)> VerifiedCorpus()
        {
            var contentClasses = typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
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
                    foreach (var action in card.ActionDefinitions)
                    {
                        if (action.EffectRoot != null) yield return (owner, action.EffectRoot);
                        if (action.SecondCardTextRoot != null)
                            yield return (owner, action.SecondCardTextRoot);
                    }
                }
            }
        }

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius);
                    dr <= Math.Min(radius, -dq + radius);
                    dr++)
                {
                    yield return new HexCoordinate(dq, dr);
                }
            }
        }
    }
}

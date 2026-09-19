using System;
using System.Collections;
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
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.59. Reported at the table on a screenshot of Swift's Silver basic Bounce - "Move 2
    /// spaces in a straight line, ignoring obstacles" - "why are unavailable spaces offered for this
    /// silver? It should just offer legal spaces 2 away, not this space 1 away which is illegal."
    ///
    /// <para><b>The offer had a ceiling and no floor.</b> <c>choose-line-destination@1</c> walked
    /// 1..<c>maximumDistance</c>, so a card printing an exact distance lit every shorter space on the
    /// same file; Bounce's own Move then declares <c>leastDistance</c> 2, so answering with one of them
    /// named a move the card cannot perform, and rulebook p.11 stops an action over an exact distance
    /// that cannot be walked rather than shortening it. The player was offered spaces that could only
    /// throw the action away.</para>
    ///
    /// <para><b>The floor belongs to the OFFER.</b> By the time the Move reads a named space the space
    /// has been named; only the question can decline to ask it. So the floor is a third optional
    /// narrowing on the shared declaration, on the discipline <c>ignoresObstacles</c> (8.35z30) and
    /// <c>continuedLineFrom</c> (8.39a) already record: unbound - or bound to nought, which is what an
    /// unbound optional Integer reads as - is a floor of ONE, which is the floor every body written
    /// before this milestone already walked. Its sibling <c>choose-charge-line@1</c> has declared the
    /// same bound since Brogan, required rather than optional because all three of its consumers print
    /// an exact or enumerated distance.</para>
    ///
    /// <para>The turn-level half of this milestone - what a real Bounce offers a real player, and what
    /// its borrowed repeat offers from the space the first walk left her on - is in
    /// <see cref="BounceBorrowedRepeatTests"/>, beside the fixture that drives her turn.</para>
    /// </summary>
    [TestFixture]
    public sealed class BounceOfferTests
    {
        private const string FloorParameter = "minimumDistance";
        private const string CeilingParameter = "maximumDistance";

        private static readonly ActionDefinitionId ProbeAction = new ActionDefinitionId("probe-floor");
        private static readonly CardDefinitionId ProbeCard = new CardDefinitionId("probe-floor-card");
        private static readonly CardInstanceId ProbeInstance =
            new CardInstanceId("probe-floor-instance");

        // -------------------------------------------------------------------------------------------
        // What the two corrected faces declare
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Bounce's own node, and the reason both bounds are equal: "Move 2 spaces" is a count, not a
        /// ceiling. The Move's floor is asserted in the same case because the defect was exactly the
        /// two disagreeing - one said 1..2 and the other said 2.
        /// </summary>
        [Test]
        public void BouncesLine_BindsTheSameExactTwoItsMovePerforms()
        {
            var nodes = SkillNodes(SwiftCardContent.Bounce);
            var line = nodes.Single(node =>
                node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1));
            var move = nodes.Single(node =>
                node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));

            Assert.That(Literal(line, FloorParameter).IntegerValue, Is.EqualTo(2),
                "The offer's floor is the printed count, so no space one away is ever lit.");
            Assert.That(Literal(line, CeilingParameter).IntegerValue, Is.EqualTo(2),
                "and its ceiling is the same number, because the sentence states one.");
            Assert.That(
                Literal(move, SyntheticDisplacementCapabilities.LeastDistanceParameterName)
                    .IntegerValue,
                Is.EqualTo(2),
                "The Move still refuses a shorter walk; what changed is that it is no longer the only " +
                "thing that knows the distance.");
            Assert.That(Literal(move, "distance").IntegerValue, Is.EqualTo(2));
            Assert.That(
                Literal(line, SyntheticRosterCapabilities.IgnoresObstaclesParameter).BooleanValue,
                Is.True,
                "\"ignoring obstacles\" is unchanged by this milestone and is asserted so a floor " +
                "added to the wrong binding list could not pass here.");
        }

        /// <summary>
        /// Wild Hunt, the second face the sweep found: "Each time before you perform an action you may
        /// move 2 spaces in a straight line." The "may" governs whether she moves, which is the node's
        /// own <c>May</c> policy, and not how far - so the distance is exact and the offer says so.
        /// </summary>
        [Test]
        public void WildHunts_FreeLine_BindsItsPrintedTwoAsACountRatherThanACeiling()
        {
            var wildHunt = SilverarrowEffectContent.WildHuntTriggeredEffect(
                SilverarrowCardContent.ContentRevision);
            var line = wildHunt.Nodes.Single(node =>
                node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1));

            Assert.That(Literal(line, FloorParameter).IntegerValue, Is.EqualTo(2));
            Assert.That(Literal(line, CeilingParameter).IntegerValue, Is.EqualTo(2));
            Assert.That(line.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                "\"you may move\" is the decision, and it stays on the node rather than being " +
                "smuggled into the distance as a shorter walk she never chose.");
        }

        /// <summary>
        /// The sweep, stated as a set equality over the composed content rather than as a list of
        /// cards remembered here: EVERY other straight-line offer this build composes prints "up to",
        /// an enumerated range, or a distance whose floor is already one, and binds no floor.
        ///
        /// <para>Cutter's three lunges are the instructive ones. "Move 1 space", "Move 1 or 2 spaces"
        /// and "Move 1, 2 or 3 spaces" are exact and enumerated distances, and every one of them is
        /// already right with no floor bound, because the walk's own floor is one. A milestone that
        /// bound the floor on them would move three digests, a content revision and an artwork catalog
        /// to change nothing at all.</para>
        /// </summary>
        [Test]
        public void EveryOtherStraightLineOffer_BindsNoFloorAndKeepsItsPrintedRange()
        {
            var found = EveryLineOffer();

            Assert.That(found.Count, Is.GreaterThanOrEqualTo(12),
                "A sweep that reaches almost nothing would pass this rule while guarding nothing. " +
                "The composed set is Cutter's five Reds, Misa's five Blues, Silverarrow's two Greens " +
                "and her Ultimate, and Swift's Silver basic.");

            Assert.That(
                found.Where(offer => offer.Node.Inputs.Any(input =>
                        input.ParameterName == FloorParameter))
                    .Select(offer => offer.Owner)
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[] { "silverarrow-wild-hunt-effect", "swift-bounce" }),
                "Only the two faces printing an exact distance whose floor is above one declare a " +
                "floor. A third name here is a face whose printed text somebody read as exact: check " +
                "it against its Docs/AI/Rules shard before pinning it.");
        }

        // -------------------------------------------------------------------------------------------
        // What the floor does to an offer
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The claim every optional narrowing in this catalog owes, and the one this milestone's own
        /// safety rests on: a body that binds nothing gets exactly the set it got before the parameter
        /// existed. Asserted twice - unbound, and bound to the nought an unbound optional Integer
        /// reads as - because those are two different ways to write "I said nothing about a floor".
        /// </summary>
        [Test]
        public void ADeclarationBindingNoFloor_KeepsItsExactCandidateSet()
        {
            var board = ContinuedLineBoard.CreateBoard();

            Assert.That(Destinations(board, floor: null, ceiling: 3).Count, Is.EqualTo(18),
                "Three spaces along each of p.19's six files, which is what this declaration offered " +
                "before milestone 8.59 and what every body that binds no floor still gets.");
            Assert.That(
                Destinations(board, floor: 0, ceiling: 3),
                Is.EqualTo(Destinations(board, floor: null, ceiling: 3)),
                "A floor of nought is the absence of a floor: standing still was never on this offer, " +
                "so nought and unbound are one reading rather than two.");
            Assert.That(
                Destinations(board, floor: 1, ceiling: 3),
                Is.EqualTo(Destinations(board, floor: null, ceiling: 3)),
                "and a floor of one states the walk's own floor, so it narrows nothing either.");
        }

        /// <summary>
        /// The offer Bounce actually asks, on a board with nothing on it: the six spaces two away
        /// along p.19's six files, and not one of the six spaces one away.
        /// </summary>
        [Test]
        public void AFloorEqualToTheCeiling_OffersOnlyTheSpacesAtThatExactDistance()
        {
            var board = ContinuedLineBoard.CreateBoard();
            var exactlyTwo = Destinations(board, floor: 2, ceiling: 2);

            Assert.That(exactlyTwo, Is.EquivalentTo(EveryDirection(2)),
                "Exactly the six spaces two away.");
            Assert.That(
                exactlyTwo.Any(space => EveryDirection(1).Contains(space)), Is.False,
                "and no space one away, which is the whole report: \"not this space 1 away which is " +
                "illegal\".");
            Assert.That(Destinations(board, floor: 2, ceiling: 3).Count, Is.EqualTo(12),
                "A floor below the ceiling is still a range - the parameter narrows the file, it does " +
                "not collapse it.");
        }

        /// <summary>
        /// Occupancy and the map's edge still answer for themselves, which is what makes the offer a
        /// set of spaces the Move can actually reach rather than a distance drawn on paper. A file
        /// whose far space is taken contributes NOTHING - it does not fall back to the near space the
        /// printed distance skips over.
        /// </summary>
        [Test]
        public void AFileWhoseFarSpaceIsTaken_ContributesNothingRatherThanItsNearOne()
        {
            var taken = ContinuedLineBoard.At(HexDirection.East, 2);
            var board = ContinuedLineBoard.CreateBoard(taken);

            var offered = Destinations(board, floor: 2, ceiling: 2);

            Assert.That(offered, Has.No.Member(taken), "Somebody is standing there.");
            Assert.That(offered, Has.No.Member(ContinuedLineBoard.At(HexDirection.East, 1)),
                "and the space one short of it is not a consolation: the card prints two.");
            Assert.That(offered.Count, Is.EqualTo(5), "The five files nobody is standing on.");
        }

        /// <summary>
        /// The p.11 case, and the one a Mandatory node turns into a stopped action: an exact distance
        /// the board cannot supply is a legal question with no answer. Every file's far space is taken
        /// here, so the walk reaches six spaces and the sentence admits none of them.
        /// </summary>
        [Test]
        public void AnExactDistanceTheBoardCannotSupply_OffersNothingAtAll()
        {
            var board = ContinuedLineBoard.CreateBoard(EveryDirection(2).ToArray());

            Assert.That(Destinations(board, floor: 2, ceiling: 2), Is.Empty);
            Assert.That(Destinations(board, floor: null, ceiling: 2).Count, Is.EqualTo(6),
                "The same board WITHOUT the floor offers the six spaces one away - which is the " +
                "defect measured rather than argued: every one of those six is a move Bounce cannot " +
                "perform.");
            Assert.That(
                Handler().Anticipate(Context(board, EffectFailurePolicy.Mandatory, 2, 2)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and the anticipation agrees with the offer, so a feasibility probe cannot promise a " +
                "question the ask will refuse.");
        }

        /// <summary>
        /// A floor above the ceiling is that same empty question rather than an inverted range quietly
        /// read backwards. The sibling charge line answers the same way.
        /// </summary>
        [Test]
        public void AFloorAboveTheCeiling_IsALegalQuestionWithNoAnswer()
        {
            Assert.That(
                Destinations(ContinuedLineBoard.CreateBoard(), floor: 3, ceiling: 2), Is.Empty);
        }

        /// <summary>
        /// The answer is re-derived, so the floor holds on the way back in too - a space the offer
        /// never named cannot be smuggled past it by a restored pause or a hand-built command.
        /// </summary>
        [Test]
        public void AnAnswerBelowTheFloor_IsRefusedByTheSameQueryThatLeftItOut()
        {
            var board = ContinuedLineBoard.CreateBoard();
            var context = Context(board, EffectFailurePolicy.Mandatory, 2, 2);

            Assert.That(
                Handler().Resume(
                    context,
                    new[] { EffectValue.Space(ContinuedLineBoard.At(HexDirection.East, 2)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The space the offer named is the answer it takes.");
            Assert.That(
                Handler().Resume(
                    context,
                    new[] { EffectValue.Space(ContinuedLineBoard.At(HexDirection.East, 1)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "and the space one away - which the offer WOULD have named before this milestone - " +
                "is refused by the same derivation that left it out.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private sealed class LineOffer
        {
            public LineOffer(string owner, EffectNodeDefinition node)
            {
                Owner = owner;
                Node = node;
            }

            public string Owner { get; }

            public EffectNodeDefinition Node { get; }
        }

        /// <summary>
        /// Every straight-line offer this build composes, discovered from the content rather than
        /// named: each verified hero's own definitions, plus the triggered effects a permanent ability
        /// runs, which is where Wild Hunt's lives.
        /// </summary>
        private static IReadOnlyList<LineOffer> EveryLineOffer()
        {
            var found = new List<LineOffer>();
            foreach (var effect in EveryComposedEffect())
            {
                found.AddRange(effect.Value
                    .Where(node =>
                        node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1))
                    .Select(node => new LineOffer(effect.Key, node)));
            }

            return found;
        }

        private static IEnumerable<KeyValuePair<string, IReadOnlyList<EffectNodeDefinition>>>
            EveryComposedEffect()
        {
            var verified = typeof(SwiftCardContent);
            var contentTypes = verified.Assembly.GetTypes()
                .Where(type => type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == verified.Namespace)
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (var type in contentTypes)
            {
                var property = type.GetProperty(
                    "AllDefinitions", BindingFlags.Public | BindingFlags.Static);
                if (property == null ||
                    !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                foreach (var card in ((IEnumerable)property.GetValue(null)).OfType<CardDefinition>())
                foreach (var action in card.ActionDefinitions)
                foreach (var root in new[] { action.EffectRoot, action.SecondCardTextRoot })
                {
                    if (root != null)
                    {
                        yield return Pair(card.Reference.DefinitionId.Value, root.Nodes);
                    }
                }
            }

            foreach (var triggered in MatchContentCatalogs.ForThisBuild().TriggeredEffects.Definitions)
            {
                yield return Pair(triggered.Id.Value, triggered.Nodes);
            }
        }

        private static KeyValuePair<string, IReadOnlyList<EffectNodeDefinition>> Pair(
            string owner, IReadOnlyList<EffectNodeDefinition> nodes) =>
            new KeyValuePair<string, IReadOnlyList<EffectNodeDefinition>>(owner, nodes);

        /// <summary>
        /// The one effect on a Swift face that walks a straight line, selected by what it composes
        /// rather than by which action slot it sits in.
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> SkillNodes(CardDefinitionId cardId) =>
            SwiftCardContent.Definition(cardId).ActionDefinitions
                .Where(action => action.EffectRoot != null)
                .Select(action => action.EffectRoot)
                .Single(effect => effect.Nodes.Any(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1)))
                .Nodes;

        private static EffectValue Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal;

        private static IReadOnlyList<HexCoordinate> EveryDirection(int steps) =>
            new[]
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            }
            .Select(direction => ContinuedLineBoard.At(direction, steps))
            .ToArray();

        private static IReadOnlyList<HexCoordinate> Destinations(
            AdjacentMoveAuthority board, int? floor, int ceiling)
        {
            var opened = Handler().Execute(
                Context(board, EffectFailurePolicy.Mandatory, floor, ceiling));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HexCoordinate>()
                : opened.Choice.Candidates.Select(candidate => candidate.Coordinate).ToArray();
        }

        private static IEffectCapabilityHandler Handler() =>
            SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(handler =>
                    handler.Reference.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1));

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, EffectFailurePolicy policy, int? floor, int ceiling)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                [CeilingParameter] = EffectValue.Integer(ceiling)
            };
            if (floor.HasValue)
            {
                inputs[FloorParameter] = EffectValue.Integer(floor.Value);
            }

            return new EffectCapabilityContext(
                new EffectNodeId("probe-floor-node"),
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
                ActionKind.Skill,
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
}

using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's Smoke bomb clauses, as composable bodies. Milestone 8.61a.
    ///
    /// <para><b>One face is composed here and five are not.</b> Smoke Bomb, her Silver basic, is the
    /// face this block owns: "Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot
    /// target you or another unit if the smoke bomb token is on a straight line between that enemy
    /// hero and their target." The second clause is not composed at all - it is the token's own
    /// property (see <see cref="MinComponentContent.SmokeBombDefinition"/>), which is what p.19 makes
    /// it: "Once a token is placed, it retains all its properties until that token is removed."</para>
    ///
    /// <para><b>The other five faces name the same piece and belong to other blocks</b>, so what this
    /// file gives them is the sub-graphs rather than their bodies: Viper and Cobra Stance's "You may
    /// swap with a [Smoke bomb] Smoke bomb in radius" and Cobra's "if you do, you may place the Smoke
    /// bomb into a space in radius" (8.61c), and Poof!, Vanish and Ruse's "Swap with a [Smoke bomb]
    /// Smoke bomb token in range" and Ruse's "You may place the Smoke bomb into a space in range"
    /// (8.61b). Each is offered as a node list under a caller-chosen prefix rather than as a finished
    /// body, because the optionality, the ordering and the timing are the consuming face's - "you may"
    /// is printed on Viper and Cobra and is <em>not</em> printed on Poof!, Vanish or Ruse, whose swap
    /// is the mandatory precondition of their Block.</para>
    ///
    /// <para>These factories deal no cards and register no handlers.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>The output every Smoke bomb space fact publishes, and every Place consumes.</summary>
        private const string SpaceOutput = "space";

        /// <summary>The output a Place publishes - the same name p.18's placements already publish.</summary>
        private const string PlacedOutput = "placed";

        /// <summary>
        /// Smoke Bomb, her Silver basic, whole: "Place a [Smoke bomb] smoke bomb token in radius".
        ///
        /// <para><b>The branch is Widget's Dragon Bond, and it is there for the same printed reason.</b>
        /// With a supply of one (MI-Q1), a Place while the bomb already stands cannot come from the
        /// supply - there is nothing in it - and p.18's short-supply reclaim would take the bomb off
        /// the board and put a different one down. What the sentence means with one piece is that the
        /// piece moves, so the standing arm is an identity-preserving board Place of the token that is
        /// already out, and the supply arm runs only when it is not. The presence fact is what chooses,
        /// and it asks the board rather than remembering.</para>
        ///
        /// <para>Both arms are mandatory and both measure from Min: p.10 makes a Place with no legal
        /// destination an impossible step rather than a skipped one, and p.9's "in radius" is the
        /// card's own printed icon with p.16's items on it - so a Radius item makes this reach 4
        /// without anything here knowing an item exists.</para>
        /// </summary>
        public static EffectDefinition SmokeBomb(CardDefinitionId card, string revision)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    Id("root"),
                    EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    MinComponentContent.SmokeBombPresentV1,
                    childNodeIds: new[] { Id("relocate"), Id("from-supply") },
                    outputs: new[] { Output("present", EffectValueKind.Boolean) })
            };
            nodes.AddRange(RelocateSmokeBomb(
                "relocate", SyntheticPlacementCapabilities.PlaceInRadiusV1));
            nodes.Add(new EffectNodeDefinition(
                Id("from-supply"),
                EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                MinComponentContent.PlaceSmokeBombInRadiusV1,
                outputs: new[] { Output(PlacedOutput, EffectValueKind.Coordinate) }));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), revision, Id("root"), nodes);
        }

        /// <summary>
        /// "Swap with a [Smoke bomb] Smoke bomb in radius" - Viper Stance and Cobra Stance (8.61c).
        ///
        /// <para>The nodes are <c>{prefix}</c> (the root of the pair), <c>{prefix}-bomb</c> and
        /// <c>{prefix}-swap</c>. The reach is the performing card's printed Radius icon with p.16's
        /// items, read off the card at the moment the fact is taken, which is why the bomb's position
        /// is a <em>bounded</em> fact rather than a free one followed by a distance test: a bomb out of
        /// radius publishes nothing, and the mandatory pair is then p.10's impossible step - which is
        /// exactly what a "you may" wrapper on the consuming face turns into a declined option.</para>
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> SwapWithSmokeBombInRadius(string prefix) =>
            SwapWithSmokeBomb(prefix, MinComponentContent.SmokeBombInRadiusV1);

        /// <summary>
        /// "Swap with a [Smoke bomb] Smoke bomb token in range" - Poof!, Vanish and Ruse (8.61b). The
        /// twin of <see cref="SwapWithSmokeBombInRadius"/>, reaching the printed Range icon instead,
        /// because p.9 gives the two icons two meanings and p.16 two consequences.
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> SwapWithSmokeBombInRange(string prefix) =>
            SwapWithSmokeBomb(prefix, MinComponentContent.SmokeBombInRangeV1);

        /// <summary>
        /// "you may place the Smoke bomb into a space in radius" - Cobra Stance's second clause
        /// (8.61c).
        ///
        /// <para>The nodes are <c>{prefix}</c>, <c>{prefix}-standing</c> and <c>{prefix}-place</c>.
        /// <b>The bomb's own position is read unbounded here and that is the printed reading</b>: the
        /// sentence places "the Smoke bomb" - the piece just swapped with, which after the swap is
        /// standing where Min was - and the radius it names bounds the DESTINATION, measured from her,
        /// not the piece being picked up. Reading the bomb through the bounded fact instead would have
        /// re-imposed the swap's own reach on a clause that does not print one.</para>
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> PlaceSmokeBombInRadius(string prefix) =>
            RelocateSmokeBomb(prefix, SyntheticPlacementCapabilities.PlaceInRadiusV1);

        /// <summary>
        /// "You may place the Smoke bomb into a space in range" - Ruse's second clause (8.61b). The
        /// printed-Range twin of <see cref="PlaceSmokeBombInRadius"/>.
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> PlaceSmokeBombInRange(string prefix) =>
            RelocateSmokeBomb(prefix, SyntheticPlacementCapabilities.PlaceInRangeV1);

        /// <summary>The node a caller sequences or wraps: every sub-graph above roots on its prefix.</summary>
        public static EffectNodeId SubGraphRootNodeId(string prefix) => Id(Require(prefix));

        private static IReadOnlyList<EffectNodeDefinition> SwapWithSmokeBomb(
            string prefix, EffectCapabilityReference bombFact)
        {
            var root = Require(prefix);
            return new[]
            {
                Sequence(root, root + "-bomb", root + "-swap"),
                new EffectNodeDefinition(
                    Id(root + "-bomb"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    bombFact,
                    outputs: new[] { Output(SpaceOutput, EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    Id(root + "-swap"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        From("subject", root + "-bomb", SpaceOutput),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.GamePiece)
                    },
                    outputs: new[]
                    {
                        Output("actorAt", EffectValueKind.Coordinate),
                        Output("subjectAt", EffectValueKind.Coordinate)
                    })
            };
        }

        /// <summary>
        /// The standing bomb picked up and put down again, measured from Min - the shape both her
        /// re-place clauses print and the shape the Silver's own relocate arm takes.
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> RelocateSmokeBomb(
            string prefix, EffectCapabilityReference place)
        {
            var root = Require(prefix);
            return new[]
            {
                Sequence(root, root + "-standing", root + "-place"),
                new EffectNodeDefinition(
                    Id(root + "-standing"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    MinComponentContent.SmokeBombSpaceV1,
                    outputs: new[] { Output(SpaceOutput, EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    Id(root + "-place"),
                    EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    place,
                    inputs: new[]
                    {
                        From("subject", root + "-standing", SpaceOutput),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.Token),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None)
                    },
                    outputs: new[] { Output(PlacedOutput, EffectValueKind.Coordinate) })
            };
        }

        private static string Require(string prefix) =>
            string.IsNullOrWhiteSpace(prefix)
                ? throw new ArgumentException(
                    "A Smoke bomb sub-graph is composed under the consuming face's own node prefix; " +
                    "two clauses on one card would otherwise mint the same node identity twice.",
                    nameof(prefix))
                : prefix;

        private static EffectNodeDefinition Sequence(string id, params string[] children) =>
            new EffectNodeDefinition(
                Id(id),
                EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory,
                childNodeIds: Array.ConvertAll(children, Id));

        private static EffectInputBinding From(string name, string node, string output) =>
            EffectInputBinding.FromNodeOutput(name, Id(node), output, EffectValueKind.Coordinate);

        private static EffectOutputBinding Output(string name, EffectValueKind kind) =>
            new EffectOutputBinding(name, kind);

        private static EffectNodeId Id(string id) => new EffectNodeId(id);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114d. Wuk's Green faces.
    ///
    /// <para><b>The throws</b> - Toss Away, Mighty Throw, Monstrous Throw: "Place a token, or an enemy
    /// unit, adjacent to you into a space in range." The subject is Mrak's Boulder selection -
    /// <c>synthetic-choose-target@4</c> at distance one on the "enemy unit or a token" side, naming the
    /// Place as its consumer - so a token of any owner (tokens are never immune, p.19) or an enemy unit
    /// the Skill may affect; then p.18's Place into an empty space in the printed Range with p.16's
    /// items, measured from Wuk, as Arien's Liquid Leap places. Monstrous Throw's "May repeat once"
    /// prints no "different target", so p.10's repeat allows the same subject (Mrak's Avalanche).</para>
    ///
    /// <para><b>The Tree swaps</b> - Into the Canopy: "Choose one - Swap with a Tree token in radius. /
    /// Swap a friendly unit in radius with a Tree token in radius." Treetop Ride prints "Choose up to
    /// two times". Bullet 1 is Trinkets' swap-with-actor over the printed-Radius object selection
    /// narrowed to Wuk's Trees; bullet 2 chooses a friendly unit in radius (p.17: never Wuk) and then a
    /// Tree, and exchanges them with <c>synthetic-swap@2</c>. p.19's Swap is neither movement nor
    /// placement. Treetop Ride's two performances are Rowenna's Grand Melee shape: two sibling "up to"
    /// wrappers over the same modal, each performed before the next is offered, the same bullet allowed
    /// twice (BR-Q4's reading).</para>
    ///
    /// <para>Nothing here is registered until 8.114f deals him.</para>
    /// </summary>
    public static class WukGreenFaces
    {
        public static CardDefinitionId TossAway { get; } = WukCardContent.Id("toss-away");
        public static CardDefinitionId MightyThrow { get; } = WukCardContent.Id("mighty-throw");
        public static CardDefinitionId IntoTheCanopy { get; } = WukCardContent.Id("into-the-canopy");
        public static CardDefinitionId MonstrousThrow { get; } = WukCardContent.Id("monstrous-throw");
        public static CardDefinitionId TreetopRide { get; } = WukCardContent.Id("treetop-ride");

        public const string ThrowText =
            "Place a token, or an enemy unit, adjacent to you into a space in range.";

        public const string CanopyText =
            "Choose one - • Swap with a Tree token in radius. • Swap a friendly unit in radius with a " +
            "Tree token in radius.";

        public const string TreetopText =
            "Choose up to two times - • Swap with a Tree token in radius. • Swap a friendly unit in " +
            "radius with a Tree token in radius.";

        public const string ThrowSubjectNode = "throw-subject";
        public const string ThrowPlaceNode = "throw-place";
        public const string ThrowRepeatNode = "throw-repeat";

        /// <summary>The five Green faces, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Throw(TossAway, "Toss Away", CardTier.I, 6, 4, 3, PrintedValueChannel.None,
                    new[] { MightyThrow, IntoTheCanopy }, false),
                Throw(MightyThrow, "Mighty Throw", CardTier.II, 5, 4, 4, PrintedValueChannel.Attack,
                    new[] { MonstrousThrow, TreetopRide }, false),
                Swaps(IntoTheCanopy, "Into the Canopy", CardTier.II, 4, PrintedValueChannel.Defense,
                    new[] { MonstrousThrow, TreetopRide }, 1),
                Throw(MonstrousThrow, "Monstrous Throw", CardTier.III, 5, 5, 4, PrintedValueChannel.Attack,
                    Array.Empty<CardDefinitionId>(), true),
                Swaps(TreetopRide, "Treetop Ride", CardTier.III, 5, PrintedValueChannel.Defense,
                    Array.Empty<CardDefinitionId>(), 2)
            }));

        // -------------------------------------------------------------------------------------------
        // The throws
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Throw(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int range,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, bool repeats) =>
            new CardDefinition(
                card, WukCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        WukCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        ThrowBody(card, repeats)),
                    WukCardContent.Movement(2),
                    WukCardContent.Defense(defense)
                },
                null, CardSourceContent.Transcribed(repeats ? ThrowText + " May repeat once." : ThrowText));

        private static EffectDefinition ThrowBody(CardDefinitionId card, bool repeats)
        {
            var root = new EffectNodeId("throw-root");
            var subject = new EffectNodeId(ThrowSubjectNode);
            var place = new EffectNodeId(ThrowPlaceNode);
            var repeat = new EffectNodeId(ThrowRepeatNode);
            var children = new List<EffectNodeId> { subject, place };
            if (repeats) children.Add(repeat);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: children),

                // "a token, or an enemy unit, adjacent to you" - Mrak's Boulder subject.
                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV4,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSourceOrUnaligned),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(place))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),

                // "into a space in range" - p.18's Place, measured from Wuk (p.9).
                new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.PlaceInRangeV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("subject", subject, "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.GamePiece),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None)
                    },
                    outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) })
            };

            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), WukCardContent.ContentRevision, root, nodes);
        }

        // -------------------------------------------------------------------------------------------
        // The Tree swaps
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Swaps(
            CardDefinitionId card, string title, CardTier tier, int defense, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> next, int performances) =>
            new CardDefinition(
                card, WukCardContent.ContentRevision, title, new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        WukCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SwapBody(card, performances)),
                    WukCardContent.Movement(2),
                    WukCardContent.Defense(defense)
                },
                null, CardSourceContent.Transcribed(performances == 1 ? CanopyText : TreetopText));

        /// <summary>The modal node of one performance, so a paused chain names what it is owed.</summary>
        public static string SwapModalNode(int pass) => $"swap-{pass}-modal";

        public static string SwapWithTreeBulletNode(int pass) => $"swap-{pass}-with-tree";

        public static string SwapFriendBulletNode(int pass) => $"swap-{pass}-friend";

        private static EffectDefinition SwapBody(CardDefinitionId card, int performances)
        {
            var root = new EffectNodeId("swap-root");
            var nodes = new List<EffectNodeDefinition>();
            EffectNodeId entry;
            if (performances == 1)
            {
                nodes.AddRange(SwapPerformance(0, wrapped: false));
                entry = new EffectNodeId(SwapModalNode(0));
                nodes.Add(new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { entry }));
            }
            else
            {
                var wrappers = Enumerable.Range(0, performances)
                    .Select(pass => new EffectNodeId($"swap-{pass}-up-to")).ToArray();
                for (var pass = 0; pass < performances; pass++)
                {
                    nodes.AddRange(SwapPerformance(pass, wrapped: true));
                }

                nodes.Add(new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.UpTo, childNodeIds: wrappers));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), WukCardContent.ContentRevision, root, nodes);
        }

        /// <summary>
        /// One performance of the two-bullet modal. Treetop Ride wraps each in p.10's "up to" (a count
        /// whose floor is zero); Into the Canopy's single "Choose one" stands bare.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> SwapPerformance(int pass, bool wrapped)
        {
            var modal = new EffectNodeId(SwapModalNode(pass));
            var withTree = new EffectNodeId(SwapWithTreeBulletNode(pass));
            var withTreeSelect = new EffectNodeId($"swap-{pass}-with-tree-select");
            var withTreeSwap = new EffectNodeId($"swap-{pass}-with-tree-swap");
            var friend = new EffectNodeId(SwapFriendBulletNode(pass));
            var friendSelect = new EffectNodeId($"swap-{pass}-friend-select");
            var friendSwap = new EffectNodeId($"swap-{pass}-friend-swap");

            if (wrapped)
            {
                yield return new EffectNodeDefinition(
                    new EffectNodeId($"swap-{pass}-up-to"), EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                    childNodeIds: new[] { modal }, chooser: EffectChooserRole.Actor);
            }

            yield return new EffectNodeDefinition(
                modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { withTree, friend },
                chooser: EffectChooserRole.Actor,
                minimumSelections: 1,
                maximumSelections: 1);

            // "Swap with a Tree token in radius."
            yield return new EffectNodeDefinition(
                withTree, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { withTreeSelect, withTreeSwap });
            yield return TreeInRadius(withTreeSelect, withTreeSwap);
            yield return new EffectNodeDefinition(
                withTreeSwap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapWithActorV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("subject", withTreeSelect, "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                });

            // "Swap a friendly unit in radius with a Tree token in radius." - one selection of both
            // pieces (WukTreeSwapContent says why), then p.19's Swap of the two.
            yield return new EffectNodeDefinition(
                friend, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { friendSelect, friendSwap });
            yield return new EffectNodeDefinition(
                friendSelect, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                WukTreeSwapContent.ChooseFriendlyUnitAndTreeV1,
                outputs: new[]
                {
                    new EffectOutputBinding(WukTreeSwapContent.FriendSpaceOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(WukTreeSwapContent.TreeSpaceOutput, EffectValueKind.Coordinate)
                });
            yield return new EffectNodeDefinition(
                friendSwap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "first", friendSelect, WukTreeSwapContent.FriendSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromNodeOutput(
                        "second", friendSelect, WukTreeSwapContent.TreeSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                });
        }

        /// <summary>"a Tree token in radius": the printed-Radius object selection narrowed to his Trees.</summary>
        private static EffectNodeDefinition TreeInRadius(EffectNodeId node, EffectNodeId consumer) =>
            new EffectNodeDefinition(
                node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                        PrintedIconObjectKind.TokensOnly),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.TokenKindParameterName,
                        EffectValue.TokenKind(WukComponentContent.TreeId)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer))
                },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114d. "Swap a friendly unit in radius with a Tree token in radius" - the second
    /// bullet of Into the Canopy and Treetop Ride - as ONE selection of the two pieces the Swap
    /// exchanges.
    ///
    /// <para><b>Why one selection and not two.</b> The offer walk (6.5af, 8.35l) judges a sequence by
    /// its first step, and a selection is narrowed only by a consumer it can already decide. Written as
    /// "choose a friendly unit, then a Tree, then swap", the first selection's consumer (the Swap) waits
    /// on the Tree, so the bullet was offered with a friendly unit in reach and no Tree - taken, it
    /// chose a unit and did nothing (carried lesson 4). Either order has the same hole. One selection
    /// that is impossible when either half is empty is the shape the walk can read: it is refused
    /// whenever the Swap could not happen.</para>
    ///
    /// <para><b>What it offers.</b> Both halves are asked of the printed-icon family's one candidate
    /// derivation - the card's printed Radius with p.16's items, measured from Wuk - with the clauses
    /// the sentence prints: p.19's units on Wuk's side and never Wuk himself (p.17's Friendly), and
    /// tokens narrowed to his Trees (8.88's <c>tokenKind</c>). The answer is exactly two spaces, one
    /// of each half, re-derived against the board when it arrives; it publishes them as
    /// <see cref="FriendSpaceOutput"/> and <see cref="TreeSpaceOutput"/> for <c>synthetic-swap@2</c>.
    /// Selecting mutates nothing.</para>
    /// </summary>
    public static class WukTreeSwapContent
    {
        public const string FriendSpaceOutput = "friendSpace";
        public const string TreeSpaceOutput = "treeSpace";

        public static EffectCapabilityReference ChooseFriendlyUnitAndTreeV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-choose-friendly-unit-and-tree"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[] { new ChooseFriendlyUnitAndTreeHandler() };

        private sealed class ChooseFriendlyUnitAndTreeHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseFriendlyUnitAndTreeV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                Array.Empty<EffectCapabilityParameter>(),
                new[]
                {
                    new EffectOutputBinding(FriendSpaceOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(TreeSpaceOutput, EffectValueKind.Coordinate)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var halves = Halves(context);
                if (halves.Friends.Count == 0 || halves.Trees.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    halves.Friends.Concat(halves.Trees).Select(EffectValue.Space).ToArray(),
                    2,
                    2));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = Publish(context, selected);
                return facts == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var halves = Halves(context);
                return halves.Friends.Count == 0 || halves.Trees.Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
            }

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context)
            {
                var halves = Halves(context);
                return halves.Friends.Count == 0 || halves.Trees.Count == 0
                    ? Array.Empty<EffectValue>()
                    : halves.Friends.Concat(halves.Trees).Select(EffectValue.Space).ToArray();
            }

            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 2;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();

            /// <summary>One friendly unit's space and one Tree's space, re-derived, or null.</summary>
            private static IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                if (chosen == null || chosen.Count != 2 ||
                    chosen.Any(value => value.Kind != EffectValueKind.Coordinate))
                {
                    return null;
                }

                var halves = Halves(context);
                var first = chosen[0].Coordinate;
                var second = chosen[1].Coordinate;
                HexCoordinate friend, tree;
                if (halves.Friends.Contains(first) && halves.Trees.Contains(second))
                {
                    friend = first;
                    tree = second;
                }
                else if (halves.Friends.Contains(second) && halves.Trees.Contains(first))
                {
                    friend = second;
                    tree = first;
                }
                else
                {
                    return null;
                }

                return new[]
                {
                    new EffectOutcomeFact(FriendSpaceOutput, EffectValue.Space(friend)),
                    new EffectOutcomeFact(TreeSpaceOutput, EffectValue.Space(tree))
                };
            }

            private static (IReadOnlyList<HexCoordinate> Friends, IReadOnlyList<HexCoordinate> Trees) Halves(
                EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null)
                {
                    return (Array.Empty<HexCoordinate>(), Array.Empty<HexCoordinate>());
                }

                var friends = SyntheticTargetingCapabilities.PrintedIconSelection(
                    context.WithAdditionalInputs(new Dictionary<string, EffectValue>
                    {
                        ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                        [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                            EffectValue.Integer((int)SourceTeamRelation.AlliedWithSource),
                        [SyntheticTargetingCapabilities.IncludeSelfParameterName] = EffectValue.Boolean(false)
                    }),
                    PrintedValueChannel.Radius,
                    SyntheticTargetingCapabilities.PrintedIconKinds.Units,
                    relationRequired: true);
                var trees = SyntheticTargetingCapabilities.PrintedIconSelection(
                    context.WithAdditionalInputs(new Dictionary<string, EffectValue>
                    {
                        ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                        [SyntheticTargetingCapabilities.IncludeSelfParameterName] = EffectValue.Boolean(false),
                        [SyntheticTargetingCapabilities.AdmittedObjectKindParameterName] =
                            EffectValue.Integer((int)PrintedIconObjectKind.TokensOnly),
                        [SyntheticTargetingCapabilities.TokenKindParameterName] =
                            EffectValue.TokenKind(WukComponentContent.TreeId)
                    }),
                    PrintedValueChannel.Radius,
                    SyntheticTargetingCapabilities.PrintedIconKinds.All,
                    relationRequired: false);
                return (Spaces(board, friends), Spaces(board, trees));
            }

            private static IReadOnlyList<HexCoordinate> Spaces(
                IBoardDisplacementAuthority board, IEnumerable<BoardObjectReference> objects) =>
                objects.Select(value => board.TryGetObjectCoordinate(value, out var at) ? (HexCoordinate?)at : null)
                    .Where(value => value.HasValue).Select(value => value.Value).Distinct().ToArray();
        }
    }
}

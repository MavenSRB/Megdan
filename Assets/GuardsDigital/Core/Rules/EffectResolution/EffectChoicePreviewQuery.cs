using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// What one space-valued candidate of a pending effect choice currently means on the board: the
    /// object standing there, and where the operation waiting on this choice would leave that object.
    ///
    /// Both are facts read from authoritative state, never a decision. A candidate that names no
    /// object is a destination the actor may take; a candidate that names one is a target.
    /// </summary>
    public readonly struct EffectChoiceCandidatePreview
    {
        public EffectChoiceCandidatePreview(
            HexCoordinate coordinate,
            BoardObjectReference occupant,
            HexCoordinate? resultingCoordinate)
        {
            if (resultingCoordinate.HasValue && !occupant.IsDefined)
            {
                throw new ArgumentException(
                    "Only a candidate naming a board object has a resulting space.",
                    nameof(resultingCoordinate));
            }

            Coordinate = coordinate;
            Occupant = occupant;
            ResultingCoordinate = resultingCoordinate;
        }

        /// <summary>The candidate itself: the space the chooser would be selecting.</summary>
        public HexCoordinate Coordinate { get; }

        /// <summary>The object standing on that space, or the undefined reference when it is empty.</summary>
        public BoardObjectReference Occupant { get; }

        /// <summary>
        /// Where the consuming operation would leave that object, when the operation is one whose
        /// outcome is decided by geometry alone. Null when nothing consumes the choice that way -
        /// including for an empty destination, which relocates nothing that already stands there.
        /// A zero-space Push is a value, not a null: p.18 makes standing still a legal result.
        /// </summary>
        public HexCoordinate? ResultingCoordinate { get; }

        /// <summary>Whether this candidate names a board object rather than an empty space.</summary>
        public bool NamesObject => Occupant.IsDefined;
    }

    /// <summary>
    /// Describes a pending effect choice's space-valued candidates against current board state, so a
    /// player can be shown what each one would do before answering.
    ///
    /// It is the same move milestone 5.8b made for the Defense choice: the authority computes the
    /// preview with the same queries command resolution uses, and presentation formats the result
    /// rather than deriving it. Push geometry in particular is not presentable arithmetic - direction,
    /// distance, transit, and the last empty endpoint are p.18 rules that <see cref="PushPathQuery"/>
    /// owns - so the alternative to this query is a view that reimplements the rulebook.
    ///
    /// Nothing here reads a card, hero, or node name. The consuming operation is found by walking the
    /// definition the paused frame is executing for the node that binds this choice's own output, and
    /// recognised by its registered capability, so every later definition of the same shape - 6.5b's
    /// direct Move of a named unit, 6.6a's Swap - is previewed by this code rather than by its own.
    /// </summary>
    public static class EffectChoicePreviewQuery
    {
        public static IReadOnlyList<EffectChoiceCandidatePreview> Describe(
            EffectDefinition definition,
            PendingEffectChoice choice,
            HeroId actorId,
            MatchComponents components,
            RuleModificationSet modifications)
        {
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (definition == null || choice == null || components == null || !components.CanDisplace ||
                !actorId.IsDefined)
            {
                return Array.Empty<EffectChoiceCandidatePreview>();
            }

            var spaces = choice.Candidates
                .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            if (spaces.Length == 0)
            {
                return Array.Empty<EffectChoiceCandidatePreview>();
            }

            var board = components.Board;
            var pusherAt = default(HexCoordinate);
            var hasPush = TryFindConsumingPush(definition, choice.NodeId, out var push) &&
                board.TryGetObjectCoordinate(BoardObjectReference.ForHero(actorId), out pusherAt);

            var previews = new List<EffectChoiceCandidatePreview>(spaces.Length);
            foreach (var space in spaces)
            {
                var occupant = board.TryGetObjectAt(space, out var standing)
                    ? standing
                    : default;
                HexCoordinate? resulting = null;
                if (hasPush && occupant.IsDefined)
                {
                    var path = board.ResolvePush(
                        pusherAt,
                        space,
                        push.Distance,
                        occupant.AsRuleObject(),
                        push.IgnoresObstacles,
                        modifications);
                    if (path.HasDirection)
                    {
                        resulting = path.Endpoint;
                    }
                }

                previews.Add(new EffectChoiceCandidatePreview(space, occupant, resulting));
            }

            return previews;
        }

        /// <summary>
        /// The declared Push, if any, that this choice's output feeds. A definition binds a chosen
        /// space to a later node through the node-output binding milestone 6.8k established, so the
        /// consumer is whichever node names this one as a source - not whichever node comes next, and
        /// never a node identified by name.
        /// </summary>
        private static bool TryFindConsumingPush(
            EffectDefinition definition, EffectNodeId choiceNodeId, out ConsumingPush push)
        {
            push = default;
            foreach (var node in definition.Nodes)
            {
                if (node.Kind != EffectNodeKind.Capability ||
                    !node.Capability.CapabilityId.Equals(SyntheticDisplacementCapabilities.PushTarget) ||
                    !node.Inputs.Any(input =>
                        input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                        input.SourceNodeId.Equals(choiceNodeId)))
                {
                    continue;
                }

                // Only a literal distance is previewable. Under `up to` the distance is itself a later
                // decision, so there is no single outcome to show yet, and reporting one would be a
                // guess at an answer the chooser has not given.
                if (!TryReadLiteral(node, "distance", EffectValueKind.Integer, out var distance) ||
                    !TryReadLiteral(node, "ignoresObstacles", EffectValueKind.Boolean, out var ignores))
                {
                    continue;
                }

                push = new ConsumingPush(distance.IntegerValue, ignores.BooleanValue);
                return true;
            }

            return false;
        }

        private static bool TryReadLiteral(
            EffectNodeDefinition node, string parameterName, EffectValueKind kind, out EffectValue value)
        {
            foreach (var input in node.Inputs)
            {
                if (!string.Equals(input.ParameterName, parameterName, StringComparison.Ordinal) ||
                    input.SourceKind != EffectBindingSourceKind.Literal ||
                    input.Literal.Kind != kind)
                {
                    continue;
                }

                value = input.Literal;
                return true;
            }

            value = default;
            return false;
        }

        private readonly struct ConsumingPush
        {
            public ConsumingPush(int distance, bool ignoresObstacles)
            {
                Distance = distance;
                IgnoresObstacles = ignoresObstacles;
            }

            public int Distance { get; }
            public bool IgnoresObstacles { get; }
        }
    }
}

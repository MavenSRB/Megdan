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
    /// What kind of consequence the operation consuming an effect choice declares for the piece the
    /// choice names. This is a projection fact, not an outcome prediction: removal intent stays
    /// removal intent even when prevention later leaves the piece on the board.
    /// </summary>
    public enum EffectChoiceConsequenceIntent
    {
        None = 0,
        Affect = 1,
        Removal = 2,
        Movement = 3,
        Attack = 4
    }

    /// <summary>
    /// What one candidate of a pending effect choice currently means on the board: the space it puts
    /// the chooser's finger on, the object standing there, and where the operation waiting on this
    /// choice would leave that object.
    ///
    /// All three are facts read from authoritative state, never a decision. A candidate that names no
    /// object is a destination the actor may take; a candidate that names one is a target.
    ///
    /// <para>Milestone 8.11c added <see cref="CandidateValue"/>, and it is what makes the space a
    /// <em>reading</em> rather than a restatement. Until then a candidate had to carry its own
    /// coordinate to be previewed at all, so a candidate naming a hero by identity was invisible here
    /// and unanswerable on the board. Now the value is carried and the coordinate is resolved from
    /// occupancy, so both shapes arrive at the same pair and every consumer addresses a candidate by
    /// its value and the board by its space.</para>
    /// </summary>
    public readonly struct EffectChoiceCandidatePreview
    {
        public EffectChoiceCandidatePreview(
            EffectValue candidateValue,
            HexCoordinate coordinate,
            BoardObjectReference occupant,
            HexCoordinate? resultingCoordinate,
            EffectChoiceConsequenceIntent consequenceIntent = EffectChoiceConsequenceIntent.Affect)
        {
            if (resultingCoordinate.HasValue && !occupant.IsDefined)
            {
                throw new ArgumentException(
                    "Only a candidate naming a board object has a resulting space.",
                    nameof(resultingCoordinate));
            }

            if (!candidateValue.IsDefined)
            {
                throw new ArgumentException(
                    "A preview describes one candidate, so it names that candidate's value.",
                    nameof(candidateValue));
            }

            if (consequenceIntent != EffectChoiceConsequenceIntent.Affect &&
                consequenceIntent != EffectChoiceConsequenceIntent.Removal &&
                consequenceIntent != EffectChoiceConsequenceIntent.Movement &&
                consequenceIntent != EffectChoiceConsequenceIntent.Attack)
            {
                throw new ArgumentOutOfRangeException(nameof(consequenceIntent));
            }

            CandidateValue = candidateValue;
            Coordinate = coordinate;
            Occupant = occupant;
            ResultingCoordinate = resultingCoordinate;
            ConsequenceIntent = consequenceIntent;
        }

        /// <summary>
        /// The candidate's own value, as the authority offered it - a coordinate, a hero, or a minion.
        /// It is how a consumer matches a select invocation to this preview, because that is the one
        /// thing the invocation and the preview always share.
        /// </summary>
        public EffectValue CandidateValue { get; }

        /// <summary>The space this candidate puts the chooser on, whether it named one or not.</summary>
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

        /// <summary>
        /// The typed intent of the declared operation consuming this choice. Presentation may use it
        /// to distinguish a removal choice from every other piece-affecting choice without reading a
        /// capability name, card text, display name, target kind, or colour.
        /// </summary>
        public EffectChoiceConsequenceIntent ConsequenceIntent { get; }

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

            var board = components.Board;
            var values = choice.Candidates
                .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value)
                .Select(candidate => candidate.Value)
                .ToArray();
            if (values.Length == 0)
            {
                return Array.Empty<EffectChoiceCandidatePreview>();
            }

            var pusherAt = default(HexCoordinate);
            var hasPush = TryFindConsumingPush(definition, choice.NodeId, out var push) &&
                board.TryGetObjectCoordinate(BoardObjectReference.ForHero(actorId), out pusherAt);
            var previews = new List<EffectChoiceCandidatePreview>(values.Length);
            var claimed = new Dictionary<HexCoordinate, EffectValue>();
            foreach (var value in values)
            {
                if (!TryResolveSpace(board, value, out var space))
                {
                    continue;
                }

                // Milestone 8.11c. `TryGetObjectAt` answers with at most one object per space, so two
                // candidates resolving to one space means two candidates naming one piece - which no
                // offered set can legitimately contain, because the chooser could then answer one
                // question two ways and the board could not say which. It is a violated invariant
                // rather than a shape to tolerate, so it stops here instead of quietly collapsing into
                // a single mark or falling back to a control nobody would notice was missing.
                if (claimed.TryGetValue(space, out var already))
                {
                    throw new InvalidOperationException(
                        $"Effect choice candidates '{already.Kind}' and '{value.Kind}' both resolve to " +
                        $"space {space}, but a space holds at most one board object.");
                }

                claimed.Add(space, value);
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

                // Milestone 9.21c. The preview names its occupant at HERO grain: the candidate's own
                // space already says which body of a many-bodied hero it is, and a projection that
                // carried a body the seat wire does not would differ on the far side of it.
                if (occupant.Kind == BoardObjectKind.Hero && occupant.HeroBody > 0)
                {
                    occupant = BoardObjectReference.ForHero(occupant.HeroId);
                }

                previews.Add(new EffectChoiceCandidatePreview(
                    value,
                    space,
                    occupant,
                    resulting,
                    DescribeConsequenceIntent(definition, choice.NodeId, value.Kind)));
            }

            return previews;
        }

        /// <summary>
        /// Reads the selection's declared consumer, which is the same typed definition link used to
        /// decide whether the consumer can act on a candidate. Defeat and Remove are distinct rules
        /// operations, but both tell presentation that the choice is invoking a removal consequence.
        /// Every other consumer remains an ordinary piece-affecting intent.
        /// </summary>
        private static EffectChoiceConsequenceIntent DescribeConsequenceIntent(
            EffectDefinition definition,
            EffectNodeId choiceNodeId,
            EffectValueKind candidateKind)
        {
            var selection = definition.Nodes.SingleOrDefault(node => node.Id.Equals(choiceNodeId));
            if (selection == null)
            {
                return EffectChoiceConsequenceIntent.Affect;
            }

            var consumers = new List<EffectNodeDefinition> { selection };
            EffectNodeDefinition declaredConsumer = null;
            if (EffectSelectionDeclaration.TryReadConsumer(selection, out var declaredConsumerId))
            {
                var declared = definition.Nodes.SingleOrDefault(node => node.Id.Equals(declaredConsumerId));
                if (declared != null)
                {
                    declaredConsumer = declared;
                    consumers.Add(declared);
                }
            }

            // A compound selection can publish more than one typed answer. Tigerclaw's Blink Strike,
            // for example, publishes a Coordinate to Direct Move and a Hero/Minion to Set Attack
            // Target out of the one crossing it names. Follow only bindings of this candidate's value
            // kind so each stage gets the meaning of the operation that will consume that answer.
            // (Brogan's charge was the other example until milestone 8.35p put its two questions in
            // their printed order; each of its nodes now publishes one kind, and this rule reads them
            // the same way it always did - from the consumer, not from the selection.)
            consumers.AddRange(definition.Nodes.Where(node =>
                node.Inputs.Any(input =>
                    input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                    input.SourceNodeId.Equals(choiceNodeId) &&
                    input.ValueKind == candidateKind)));

            var capabilities = consumers
                .Where(node => node.Kind == EffectNodeKind.Capability)
                .Select(node => node.Capability.CapabilityId)
                .Distinct()
                .ToArray();
            if (declaredConsumer != null &&
                declaredConsumer.Kind == EffectNodeKind.Capability &&
                IsRemoval(declaredConsumer.Capability.CapabilityId))
            {
                return EffectChoiceConsequenceIntent.Removal;
            }

            if (capabilities.Contains(SyntheticContinuationCapabilities.SetAttackTarget))
            {
                return EffectChoiceConsequenceIntent.Attack;
            }

            if (candidateKind == EffectValueKind.Coordinate && capabilities.Any(IsMovementOrPlacement))
            {
                return EffectChoiceConsequenceIntent.Movement;
            }

            return EffectChoiceConsequenceIntent.Affect;
        }

        private static bool IsRemoval(EffectCapabilityId capabilityId) =>
            capabilityId.Equals(SyntheticExchangeCapabilities.Remove) ||
            capabilityId.Equals(SyntheticConsequenceCapabilities.DefeatHero) ||
            capabilityId.Equals(SyntheticConsequenceCapabilities.DefeatMinion);

        private static bool IsMovementOrPlacement(EffectCapabilityId capabilityId) =>
            capabilityId.Equals(SyntheticDisplacementCapabilities.DirectMove) ||
            capabilityId.Equals(SyntheticPlacementCapabilities.Place) ||
            capabilityId.Equals(SyntheticPlacementCapabilities.PlaceInRange) ||
            capabilityId.Equals(SyntheticPlacementCapabilities.PlaceInRadius) ||
            capabilityId.Equals(SyntheticPlacementCapabilities.Spawn) ||
            capabilityId.Equals(SyntheticComponentCapabilities.PlaceToken);

        /// <summary>
        /// The space a candidate's value puts the chooser on: the coordinate it carries, or - milestone
        /// 8.11c - the space authoritative occupancy currently has that hero or minion standing on.
        ///
        /// <para>A candidate naming an object that is on no space answers false and is simply not
        /// previewed, which leaves it with its prompt control. That is the honest outcome rather than a
        /// fault: it is the only shape for which the board has nothing to offer. It is also one the
        /// live content cannot produce - a printed-icon selection offers heroes measured from a disk
        /// drawn on the board - and `PromptReachabilityConformanceTests` is what would notice if some
        /// later card did, because it asks occupancy the same question independently.</para>
        ///
        /// <para>A card instance is not a board object and is never resolved here. p.10's discard
        /// chooses among cards in a hand, which is a question about a hand rather than about a
        /// space.</para>
        /// </summary>
        private static bool TryResolveSpace(
            IBoardDisplacementAuthority board, EffectValue value, out HexCoordinate space)
        {
            switch (value.Kind)
            {
                case EffectValueKind.Coordinate:
                    space = value.Coordinate;
                    return true;
                case EffectValueKind.Hero:
                    return board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(value.HeroId), out space);
                case EffectValueKind.Minion:
                    return board.TryGetObjectCoordinate(
                        BoardObjectReference.ForMinion(value.MinionId), out space);
                default:
                    space = default;
                    return false;
            }
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

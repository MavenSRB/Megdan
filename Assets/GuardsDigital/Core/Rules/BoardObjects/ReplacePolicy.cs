using System;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>
    /// Where the incoming object comes from. p.19's Replace names "a different object" without saying
    /// where it is found, so the declaration says it: a substitute taken out of a finite component
    /// supply is a different rules event from one lifted off another space, exactly as p.18 separates
    /// Place's two arms.
    /// </summary>
    public enum ReplaceIncomingSource
    {
        None = 0,

        /// <summary>The component supply, whose count p.19 calls intentionally limited.</summary>
        Supply = 1
    }

    /// <summary>Where the outgoing object goes once the substitute has taken its space.</summary>
    public enum ReplaceOutgoingDestination
    {
        None = 0,

        /// <summary>Back into the supply it was taken from, where it can be placed again.</summary>
        Supply = 1
    }

    /// <summary>
    /// Whether the incoming object inherits anything from the one it replaces.
    ///
    /// It is declared rather than assumed because both answers are expressible and they are not
    /// interchangeable: p.19 ties a token's properties to the instance - "once a token is placed, it
    /// retains all its properties until that token is removed" - so a substitute that silently kept the
    /// outgoing token's properties would be supplying rules its own definition never declared.
    /// </summary>
    public enum ReplacePropertyTransferPolicy
    {
        None = 0,

        /// <summary>
        /// The incoming object brings its own properties and inherits none. The outgoing object's
        /// property sources end with its placement, which is the lifetime p.19 gives them.
        /// </summary>
        NoTransfer = 1
    }

    /// <summary>
    /// Which triggers observe this Replace.
    ///
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> makes this a declaration rather than a consequence: a Replace
    /// "is not an alias for Remove followed by Spawn unless verified rules text defines those exact
    /// observable stages". The two members are those two readings, and a definition states which one it
    /// means instead of leaving observers to infer it from how the commit happens to be written.
    /// </summary>
    public enum ReplaceObservationPolicy
    {
        None = 0,

        /// <summary>
        /// One observable replacement. Anything watching for a removal or a placement sees neither,
        /// because neither happened: the space is never empty and no piece is ever off the board.
        /// </summary>
        ReplacementOnly = 1,

        /// <summary>
        /// A removal and a placement that observers see separately. Declared here so the alternative
        /// has a name, and deliberately unimplemented: it needs verified text defining those exact
        /// stages, and the shared operation refuses a policy it cannot honour rather than quietly
        /// producing the single-event form under a two-event declaration.
        /// </summary>
        RemovalAndPlacement = 2
    }

    /// <summary>
    /// The complete declaration of one Replace operation: where the substitute comes from, where the
    /// replaced object goes, whether properties transfer, and what observes it.
    ///
    /// It exists as one object rather than four arguments for the reason <c>PlacePolicy</c> does: those
    /// four are exactly what <c>CARD_EFFECT_CAPABILITIES.md</c> requires a replacement definition to
    /// state, and one that fails to state any of them cannot be constructed at all.
    /// </summary>
    public sealed class ReplacePolicy
    {
        public ReplacePolicy(
            ReplaceIncomingSource incomingSource,
            ReplaceOutgoingDestination outgoingDestination,
            ReplacePropertyTransferPolicy propertyTransfer,
            ReplaceObservationPolicy observation)
        {
            if (incomingSource == ReplaceIncomingSource.None)
            {
                throw new ArgumentException(
                    "A Replace declares where its incoming object comes from.", nameof(incomingSource));
            }

            if (outgoingDestination == ReplaceOutgoingDestination.None)
            {
                throw new ArgumentException(
                    "A Replace declares where the object it replaces goes.", nameof(outgoingDestination));
            }

            if (propertyTransfer == ReplacePropertyTransferPolicy.None)
            {
                throw new ArgumentException(
                    "A Replace declares whether properties transfer to the incoming object.",
                    nameof(propertyTransfer));
            }

            if (observation == ReplaceObservationPolicy.None)
            {
                throw new ArgumentException(
                    "A Replace declares which triggers observe it.", nameof(observation));
            }

            IncomingSource = incomingSource;
            OutgoingDestination = outgoingDestination;
            PropertyTransfer = propertyTransfer;
            Observation = observation;
        }

        public ReplaceIncomingSource IncomingSource { get; }
        public ReplaceOutgoingDestination OutgoingDestination { get; }
        public ReplacePropertyTransferPolicy PropertyTransfer { get; }
        public ReplaceObservationPolicy Observation { get; }

        public override string ToString() =>
            $"Replace from {IncomingSource} to {OutgoingDestination}, {PropertyTransfer}, {Observation}";
    }
}

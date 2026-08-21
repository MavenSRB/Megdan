using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    public readonly struct EffectResolutionId : IEquatable<EffectResolutionId>
    {
        public EffectResolutionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Effect resolution ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EffectResolutionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectResolutionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct EffectChoiceId : IEquatable<EffectChoiceId>
    {
        public EffectChoiceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Effect choice ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EffectChoiceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectChoiceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// One position on the continuation stack: the node being executed and how far through its children
    /// it has progressed. A stack of these is the whole "where am I" state, and it serializes as a
    /// count-prefixed array of two fields.
    /// </summary>
    public readonly struct EffectContinuationFrame : IEquatable<EffectContinuationFrame>
    {
        public EffectContinuationFrame(EffectNodeId nodeId, int childIndex)
        {
            if (!nodeId.IsDefined)
            {
                throw new ArgumentException("A continuation frame requires a node.", nameof(nodeId));
            }

            if (childIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(childIndex));
            }

            NodeId = nodeId;
            ChildIndex = childIndex;
        }

        public EffectNodeId NodeId { get; }
        public int ChildIndex { get; }

        public EffectContinuationFrame Advanced() => new EffectContinuationFrame(NodeId, ChildIndex + 1);

        public bool Equals(EffectContinuationFrame other) =>
            NodeId.Equals(other.NodeId) && ChildIndex == other.ChildIndex;

        public override bool Equals(object obj) => obj is EffectContinuationFrame other && Equals(other);
        public override int GetHashCode() => (NodeId.GetHashCode() * 397) ^ ChildIndex;
        public override string ToString() => $"{NodeId}[{ChildIndex}]";
    }

    /// <summary>What one node did, and the typed facts it published for later nodes to bind.</summary>
    public sealed class EffectNodeOutcome
    {
        public EffectNodeOutcome(
            EffectNodeId nodeId,
            EffectExecutionResultKind resultKind,
            IEnumerable<EffectOutcomeFact> outputs = null)
        {
            if (!nodeId.IsDefined)
            {
                throw new ArgumentException("A node outcome requires a node.", nameof(nodeId));
            }

            if (resultKind == EffectExecutionResultKind.None)
            {
                throw new ArgumentException("A node outcome requires a result kind.", nameof(resultKind));
            }

            var facts = (outputs ?? Enumerable.Empty<EffectOutcomeFact>()).ToArray();
            if (facts.Select(fact => fact.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != facts.Length)
            {
                throw new ArgumentException("Node outcome output names must be unique.", nameof(outputs));
            }

            NodeId = nodeId;
            ResultKind = resultKind;
            Outputs = new ReadOnlyCollection<EffectOutcomeFact>(facts);
        }

        public EffectNodeId NodeId { get; }
        public EffectExecutionResultKind ResultKind { get; }
        public IReadOnlyList<EffectOutcomeFact> Outputs { get; }
    }

    /// <summary>An accepted decision, kept so replay and restore reproduce the same execution.</summary>
    public sealed class EffectChoiceRecord
    {
        public EffectChoiceRecord(
            EffectNodeId nodeId,
            EffectChoiceId choiceId,
            EffectChoiceCommandKind resolvedBy,
            IEnumerable<EffectChoiceCandidate> selected = null)
        {
            if (!nodeId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("A choice record requires stable identities.");
            }

            if (resolvedBy != EffectChoiceCommandKind.Confirm &&
                resolvedBy != EffectChoiceCommandKind.Decline)
            {
                throw new ArgumentException(
                    "A choice record closes on Confirm or Decline.", nameof(resolvedBy));
            }

            NodeId = nodeId;
            ChoiceId = choiceId;
            ResolvedBy = resolvedBy;
            Selected = new ReadOnlyCollection<EffectChoiceCandidate>(
                (selected ?? Enumerable.Empty<EffectChoiceCandidate>()).ToArray());
        }

        public EffectNodeId NodeId { get; }
        public EffectChoiceId ChoiceId { get; }
        public EffectChoiceCommandKind ResolvedBy { get; }
        public IReadOnlyList<EffectChoiceCandidate> Selected { get; }
    }

    /// <summary>
    /// The general serializable execution frame: which effect definition is running, who is running it,
    /// exactly where execution stopped, what has already happened, and what decision is outstanding.
    ///
    /// It holds an <see cref="EffectDefinitionReference"/> and never an <c>EffectDefinition</c>, and it
    /// holds no handler. That is deliberate and load-bearing: because the runtime type has nowhere to
    /// put capability semantics, neither does the snapshot written from it, so a restore has to rebind
    /// to the trusted catalog rather than reconstruct meaning from a payload.
    /// </summary>
    public sealed class PendingResolution
    {
        public PendingResolution(
            EffectResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            EffectDefinitionReference effectDefinitionReference,
            HeroId actorId,
            HeroId controllerId,
            ActionInvocationContext invocationContext,
            IEnumerable<EffectContinuationFrame> continuation,
            IEnumerable<EffectNodeOutcome> completedOutcomes = null,
            IEnumerable<EffectChoiceRecord> acceptedChoices = null,
            PendingEffectChoice pendingChoice = null,
            RepeatContinuation repeat = default)
        {
            // Milestone 8.6. The two ways of saying "this instance is a repeat" are kept in step here,
            // because they answer different halves of the same fact and nothing else could catch them
            // disagreeing: the invocation context is what the dispatcher's start gate and the offer
            // queries read, and the continuation is what the bound repeat counts.
            if (repeat.IsRepeatedInstance != (invocationContext == ActionInvocationContext.Repeated))
            {
                throw new ArgumentException(
                    "A repeated instance runs under the repeated invocation context and carries a " +
                    "repeat continuation; a printed one carries neither.", nameof(repeat));
            }

            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("A pending resolution requires a resolution ID.", nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("A pending resolution requires its source card and action.");
            }

            if (!effectDefinitionReference.IsDefined)
            {
                throw new ArgumentException(
                    "A pending resolution requires a defined effect definition reference.",
                    nameof(effectDefinitionReference));
            }

            if (!actorId.IsDefined || !controllerId.IsDefined)
            {
                throw new ArgumentException("A pending resolution requires an actor and a controller.");
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            EffectDefinitionReference = effectDefinitionReference;
            ActorId = actorId;
            ControllerId = controllerId;
            InvocationContext = invocationContext;
            Repeat = repeat;
            Continuation = new ReadOnlyCollection<EffectContinuationFrame>(
                (continuation ?? throw new ArgumentNullException(nameof(continuation))).ToArray());
            CompletedOutcomes = new ReadOnlyCollection<EffectNodeOutcome>(
                (completedOutcomes ?? Enumerable.Empty<EffectNodeOutcome>()).ToArray());
            AcceptedChoices = new ReadOnlyCollection<EffectChoiceRecord>(
                (acceptedChoices ?? Enumerable.Empty<EffectChoiceRecord>()).ToArray());
            PendingChoice = pendingChoice;

            if (pendingChoice != null && !pendingChoice.ResolutionId.Equals(resolutionId))
            {
                throw new ArgumentException(
                    "A pending choice must belong to the resolution that carries it.", nameof(pendingChoice));
            }
        }

        public EffectResolutionId ResolutionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public EffectDefinitionReference EffectDefinitionReference { get; }
        public HeroId ActorId { get; }
        public HeroId ControllerId { get; }
        public ActionInvocationContext InvocationContext { get; }

        /// <summary>
        /// Milestone 8.6: how far through a bounded repeat this instance is, and the target it
        /// inherited from the instance before it. <see cref="RepeatContinuation.FirstInstance"/> for
        /// every printed instance, which is the default value, so nothing that never repeats names it.
        /// </summary>
        public RepeatContinuation Repeat { get; }

        /// <summary>Index 0 is the root frame; the last entry is where execution resumes.</summary>
        public IReadOnlyList<EffectContinuationFrame> Continuation { get; }

        public IReadOnlyList<EffectNodeOutcome> CompletedOutcomes { get; }
        public IReadOnlyList<EffectChoiceRecord> AcceptedChoices { get; }

        /// <summary>Null while the resolution is running rather than awaiting input.</summary>
        public PendingEffectChoice PendingChoice { get; }

        public bool IsAwaitingChoice => PendingChoice != null;

        /// <summary>
        /// The same frame carrying a different repeat continuation. Milestone 8.6: the piece an
        /// `on the same target` repeat will bind is recorded the moment the selection names it, which
        /// is mid-resolution, so the frame has to be able to take one without rebuilding everything
        /// else about it.
        /// </summary>
        public PendingResolution WithRepeat(RepeatContinuation repeat) =>
            new PendingResolution(
                ResolutionId,
                SourceCardInstanceId,
                SourceDefinitionReference,
                ActionDefinitionId,
                EffectDefinitionReference,
                ActorId,
                ControllerId,
                InvocationContext,
                Continuation,
                CompletedOutcomes,
                AcceptedChoices,
                PendingChoice,
                repeat);

        public PendingResolution With(
            IEnumerable<EffectContinuationFrame> continuation,
            IEnumerable<EffectNodeOutcome> completedOutcomes,
            IEnumerable<EffectChoiceRecord> acceptedChoices,
            PendingEffectChoice pendingChoice) =>
            new PendingResolution(
                ResolutionId,
                SourceCardInstanceId,
                SourceDefinitionReference,
                ActionDefinitionId,
                EffectDefinitionReference,
                ActorId,
                ControllerId,
                InvocationContext,
                continuation,
                completedOutcomes,
                acceptedChoices,
                pendingChoice,
                Repeat);
    }
}

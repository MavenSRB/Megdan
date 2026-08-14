using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>What kind of decision is being asked for.</summary>
    public enum EffectChoiceKind
    {
        None = 0,

        /// <summary>Choose among a control node's options.</summary>
        SelectOption = 1,

        /// <summary>Choose among typed values a capability offered.</summary>
        SelectTarget = 2,

        /// <summary>Take or decline an optional step.</summary>
        ConfirmOrDecline = 3
    }

    public enum EffectChoiceCandidateKind
    {
        None = 0,
        Option = 1,
        Value = 2
    }

    /// <summary>
    /// One thing a chooser may pick: an option node of a control structure, or a typed value a
    /// capability offered. Both carry a stable identity, so a selection survives the snapshot boundary
    /// without the payload having to describe what it meant.
    /// </summary>
    public readonly struct EffectChoiceCandidate : IEquatable<EffectChoiceCandidate>
    {
        private EffectChoiceCandidate(
            EffectChoiceCandidateKind kind,
            EffectNodeId optionNodeId,
            EffectValue value)
        {
            Kind = kind;
            OptionNodeId = optionNodeId;
            Value = value;
        }

        public EffectChoiceCandidateKind Kind { get; }
        public EffectNodeId OptionNodeId { get; }
        public EffectValue Value { get; }

        public bool IsDefined => Kind != EffectChoiceCandidateKind.None;

        public static EffectChoiceCandidate Option(EffectNodeId optionNodeId)
        {
            if (!optionNodeId.IsDefined)
            {
                throw new ArgumentException(
                    "An option candidate requires a defined node.", nameof(optionNodeId));
            }

            return new EffectChoiceCandidate(EffectChoiceCandidateKind.Option, optionNodeId, default);
        }

        public static EffectChoiceCandidate Value_(EffectValue value)
        {
            if (!value.IsDefined)
            {
                throw new ArgumentException("A value candidate requires a defined value.", nameof(value));
            }

            return new EffectChoiceCandidate(EffectChoiceCandidateKind.Value, default, value);
        }

        public bool Equals(EffectChoiceCandidate other) =>
            Kind == other.Kind &&
            OptionNodeId.Equals(other.OptionNodeId) &&
            Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is EffectChoiceCandidate other && Equals(other);

        public override int GetHashCode() =>
            ((int)Kind * 397) ^ OptionNodeId.GetHashCode() ^ Value.GetHashCode();

        public override string ToString() =>
            Kind == EffectChoiceCandidateKind.Option ? OptionNodeId.ToString() : Value.ToString();
    }

    public enum EffectChoiceCommandKind
    {
        None = 0,
        Select = 1,
        Deselect = 2,
        Confirm = 3,
        Decline = 4
    }

    /// <summary>
    /// One invocation against a pending effect choice. Projected pre-built by the pending choice, the
    /// way <c>MinionBattleChoiceCommand</c> is, so presentation forwards an authoritative command
    /// rather than composing identities of its own.
    /// </summary>
    public sealed class EffectChoiceCommand : IEquatable<EffectChoiceCommand>
    {
        public EffectChoiceCommand(
            EffectResolutionId resolutionId,
            EffectChoiceId choiceId,
            EffectChoiceCommandKind kind,
            EffectChoiceCandidate candidate = default)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("An effect choice command requires stable identities.");
            }

            if (kind == EffectChoiceCommandKind.None)
            {
                throw new ArgumentException("An effect choice command requires a kind.", nameof(kind));
            }

            var carriesCandidate =
                kind == EffectChoiceCommandKind.Select || kind == EffectChoiceCommandKind.Deselect;
            if (carriesCandidate != candidate.IsDefined)
            {
                throw new ArgumentException(
                    "Select and Deselect carry exactly one candidate; Confirm and Decline carry none.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            Kind = kind;
            Candidate = candidate;
        }

        public EffectResolutionId ResolutionId { get; }
        public EffectChoiceId ChoiceId { get; }
        public EffectChoiceCommandKind Kind { get; }
        public EffectChoiceCandidate Candidate { get; }

        public bool Equals(EffectChoiceCommand other) =>
            other != null &&
            ResolutionId.Equals(other.ResolutionId) &&
            ChoiceId.Equals(other.ChoiceId) &&
            Kind == other.Kind &&
            Candidate.Equals(other.Candidate);

        public override bool Equals(object obj) => Equals(obj as EffectChoiceCommand);

        public override int GetHashCode() =>
            (ResolutionId.GetHashCode() * 397) ^ ChoiceId.GetHashCode() ^
            ((int)Kind * 31) ^ Candidate.GetHashCode();

        public override string ToString() => $"{Kind} {Candidate} @{ResolutionId}/{ChoiceId}";
    }

    /// <summary>
    /// The exact decision currently required: its chooser, its legal candidates, what is selected so
    /// far, and the pre-built invocations that are legal right now. It never holds an assumed answer.
    ///
    /// Modelled on <c>PendingMinionBattleChoice</c>, which milestone 6.1 built for the first real
    /// end-of-round choice: stable resolution and choice IDs, ordered candidates, legal select and
    /// deselect sets derived rather than declared, and an immutable <c>WithSelected</c> transition.
    /// </summary>
    public sealed class PendingEffectChoice
    {
        public PendingEffectChoice(
            EffectResolutionId resolutionId,
            EffectChoiceId choiceId,
            EffectNodeId nodeId,
            HeroId chooserId,
            EffectChoiceKind kind,
            IEnumerable<EffectChoiceCandidate> candidates,
            int minimumSelections,
            int maximumSelections,
            IEnumerable<EffectChoiceCandidate> selected = null)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined || !nodeId.IsDefined)
            {
                throw new ArgumentException("A pending effect choice requires stable identities.");
            }

            if (!chooserId.IsDefined)
            {
                throw new ArgumentException("A pending effect choice requires a chooser.", nameof(chooserId));
            }

            if (kind == EffectChoiceKind.None)
            {
                throw new ArgumentException("A pending effect choice requires a kind.", nameof(kind));
            }

            var options = (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
            if (options.Any(candidate => !candidate.IsDefined) ||
                options.Distinct().Count() != options.Length)
            {
                throw new ArgumentException(
                    "Effect choice candidates must be defined and unique.", nameof(candidates));
            }

            var chosen = (selected ?? Enumerable.Empty<EffectChoiceCandidate>()).ToArray();
            if (chosen.Any(candidate => !options.Contains(candidate)) ||
                chosen.Distinct().Count() != chosen.Length)
            {
                throw new ArgumentException(
                    "Selected effect choice candidates must be distinct offered candidates.", nameof(selected));
            }

            if (kind == EffectChoiceKind.ConfirmOrDecline && options.Length != 0)
            {
                throw new ArgumentException(
                    "A confirm-or-decline choice offers no candidates.", nameof(candidates));
            }

            if (minimumSelections < 0 || maximumSelections < minimumSelections ||
                maximumSelections > options.Length)
            {
                throw new ArgumentException("Effect choice bounds must be a range within the candidates.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            NodeId = nodeId;
            ChooserId = chooserId;
            Kind = kind;
            Candidates = new ReadOnlyCollection<EffectChoiceCandidate>(options);
            SelectedCandidates = new ReadOnlyCollection<EffectChoiceCandidate>(chosen);
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;

            SelectInvocations = new ReadOnlyCollection<EffectChoiceCommand>(options
                .Where(candidate => !chosen.Contains(candidate) && chosen.Length < maximumSelections)
                .Select(candidate => new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Select, candidate))
                .ToArray());
            DeselectInvocations = new ReadOnlyCollection<EffectChoiceCommand>(chosen
                .Select(candidate => new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Deselect, candidate))
                .ToArray());
            ConfirmInvocation = chosen.Length >= minimumSelections
                ? new EffectChoiceCommand(resolutionId, choiceId, EffectChoiceCommandKind.Confirm)
                : null;
            DeclineInvocation = kind == EffectChoiceKind.ConfirmOrDecline
                ? new EffectChoiceCommand(resolutionId, choiceId, EffectChoiceCommandKind.Decline)
                : null;
        }

        public EffectResolutionId ResolutionId { get; }
        public EffectChoiceId ChoiceId { get; }
        public EffectNodeId NodeId { get; }
        public HeroId ChooserId { get; }
        public EffectChoiceKind Kind { get; }
        public IReadOnlyList<EffectChoiceCandidate> Candidates { get; }
        public IReadOnlyList<EffectChoiceCandidate> SelectedCandidates { get; }
        public int MinimumSelections { get; }
        public int MaximumSelections { get; }

        public IReadOnlyList<EffectChoiceCommand> SelectInvocations { get; }
        public IReadOnlyList<EffectChoiceCommand> DeselectInvocations { get; }

        /// <summary>Null while the selection does not yet satisfy the minimum.</summary>
        public EffectChoiceCommand ConfirmInvocation { get; }

        /// <summary>Null unless the decision is one a chooser is allowed to decline outright.</summary>
        public EffectChoiceCommand DeclineInvocation { get; }

        public PendingEffectChoice WithSelected(IEnumerable<EffectChoiceCandidate> selected) =>
            new PendingEffectChoice(
                ResolutionId, ChoiceId, NodeId, ChooserId, Kind,
                Candidates, MinimumSelections, MaximumSelections, selected);
    }
}

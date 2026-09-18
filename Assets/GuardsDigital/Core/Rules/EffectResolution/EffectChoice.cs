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

    /// <summary>
    /// Milestone 8.37. What a choice's INTEGER candidates mean, when they mean something other than a
    /// count.
    ///
    /// <para><b>Why presentation cannot work this out for itself.</b> An
    /// <see cref="EffectValueKind.Integer"/> candidate has been a printed NUMBER every time it has
    /// appeared until now - the kinetic "3 or 4 spaces" distance - so the HUD labels one with its own
    /// digits and calls the decision a number. Tali's Commune with Spirits offers six
    /// <see cref="Cards.CardColor"/> members and her Cold Snap offers six
    /// <see cref="Board.HexDirection"/> members, and both would reach a player as the buttons "0"
    /// through "5". Inferring the meaning from the values is exactly what this codebase refuses
    /// elsewhere - see <c>EffectChoiceCandidatePreview.ConsequenceIntent</c>, which exists so
    /// presentation can tell a removal from every other choice "without reading a capability name,
    /// card text, display name, target kind, or colour" - so the meaning is DECLARED by the handler
    /// that opened the choice and carried to whoever draws it.</para>
    ///
    /// <para>It is not persisted in a snapshot and does not move the schema: restore re-asks the
    /// registered handler what it offered, and the handler declares this on the way back out.</para>
    /// </summary>
    public enum EffectChoiceValueVocabulary
    {
        /// <summary>An ordinary value: a printed number, or a candidate of some non-integer kind.</summary>
        None = 0,

        /// <summary>Rulebook p.9's six printed card colours.</summary>
        CardColor = 1,

        /// <summary>Rulebook p.17's six board directions.</summary>
        HexDirection = 2
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
        Decline = 4,

        /// <summary>
        /// Milestone 6.5ae: closes an action whose every option was refused. It is not a decline -
        /// a mandatory <c>Choose</c> has nothing to decline - but p.10's stop, the same one a paused
        /// Movement has offered since Phase 1, asked for by the player rather than taken for them.
        /// </summary>
        Stop = 5
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
            EffectChoiceCandidate candidate = default,
            HeroId answeringHeroId = default)
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
                    "Select and Deselect carry exactly one candidate; Confirm, Decline, and Stop " +
                    "carry none.");
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            Kind = kind;
            Candidate = candidate;
            AnsweringHeroId = answeringHeroId;
        }

        public EffectResolutionId ResolutionId { get; }
        public EffectChoiceId ChoiceId { get; }
        public EffectChoiceCommandKind Kind { get; }
        public EffectChoiceCandidate Candidate { get; }

        /// <summary>
        /// Milestone 7.1a: the hero this answer is submitted for.
        ///
        /// Every legitimate command is minted by <see cref="PendingEffectChoice"/> carrying its own
        /// <see cref="PendingEffectChoice.ChooserId"/>, so presentation forwards the projected
        /// invocation and never composes an identity of its own. The field exists so that a command
        /// built by hand for a hero whose decision this is not can be refused as exactly that, rather
        /// than as a generic invalid choice: once a chooser may be someone other than the actor, "who
        /// sent this" is a question the resolver has to be able to ask.
        /// </summary>
        public HeroId AnsweringHeroId { get; }

        public bool Equals(EffectChoiceCommand other) =>
            other != null &&
            ResolutionId.Equals(other.ResolutionId) &&
            ChoiceId.Equals(other.ChoiceId) &&
            Kind == other.Kind &&
            Candidate.Equals(other.Candidate) &&
            AnsweringHeroId.Equals(other.AnsweringHeroId);

        public override bool Equals(object obj) => Equals(obj as EffectChoiceCommand);

        public override int GetHashCode() =>
            (ResolutionId.GetHashCode() * 397) ^ ChoiceId.GetHashCode() ^
            ((int)Kind * 31) ^ Candidate.GetHashCode() ^ AnsweringHeroId.GetHashCode();

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
            IEnumerable<EffectChoiceCandidate> selected = null,
            IEnumerable<EffectChoiceCandidate> unavailableCandidates = null,
            bool canStop = false,
            bool declinableRegardlessOfMinimum = false,
            EffectChoiceValueVocabulary valueVocabulary = EffectChoiceValueVocabulary.None)
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

            if (valueVocabulary != EffectChoiceValueVocabulary.None &&
                options.Any(candidate =>
                    candidate.Kind != EffectChoiceCandidateKind.Value ||
                    candidate.Value.Kind != EffectValueKind.Integer))
            {
                throw new ArgumentException(
                    "A declared value vocabulary describes integer candidates, so every candidate must " +
                    "be one.", nameof(valueVocabulary));
            }

            ValueVocabulary = valueVocabulary;

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

            var refused = (unavailableCandidates ?? Enumerable.Empty<EffectChoiceCandidate>()).ToArray();
            if (refused.Any(candidate => !options.Contains(candidate)) ||
                refused.Distinct().Count() != refused.Length)
            {
                throw new ArgumentException(
                    "Unperformable effect choice candidates must be distinct offered candidates.",
                    nameof(unavailableCandidates));
            }

            if (refused.Any(candidate => candidate.Kind != EffectChoiceCandidateKind.Option))
            {
                throw new ArgumentException(
                    "Only an option can be unperformable. A capability's value candidates are already " +
                    "the set it derived, so one it offered is one it can act on.",
                    nameof(unavailableCandidates));
            }

            if (chosen.Any(candidate => refused.Contains(candidate)))
            {
                throw new ArgumentException(
                    "An unperformable option cannot be selected.", nameof(selected));
            }

            if (canStop &&
                (kind != EffectChoiceKind.SelectOption ||
                    options.Length == 0 ||
                    refused.Length != options.Length))
            {
                throw new ArgumentException(
                    "A stop closes a mode selection whose every offered option was refused. Where " +
                    "one can still be taken there is a decision to make, and taking it is how the " +
                    "action goes on.",
                    nameof(canStop));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            NodeId = nodeId;
            ChooserId = chooserId;
            Kind = kind;
            Candidates = new ReadOnlyCollection<EffectChoiceCandidate>(options);
            SelectedCandidates = new ReadOnlyCollection<EffectChoiceCandidate>(chosen);
            UnavailableCandidates = new ReadOnlyCollection<EffectChoiceCandidate>(refused);
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
            DeclinableRegardlessOfMinimum = declinableRegardlessOfMinimum;

            SelectInvocations = new ReadOnlyCollection<EffectChoiceCommand>(options
                .Where(candidate => !refused.Contains(candidate))
                .Where(candidate => !chosen.Contains(candidate) && chosen.Length < maximumSelections)
                .Select(candidate => new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Select, candidate, chooserId))
                .ToArray());
            DeselectInvocations = new ReadOnlyCollection<EffectChoiceCommand>(chosen
                .Select(candidate => new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Deselect, candidate, chooserId))
                .ToArray());
            // Milestone 8.20a, at the user's direction: "if it says may we need to be able to just
            // decline it". A printed `may` or `up to` opens a choice that may legally be completed
            // having taken nothing, and refusing it is then an answer the player must be able to
            // give by name. Before this it was offered only for the confirm-or-decline shape, so
            // Arien's Living Tsunami - "Once per turn, before performing an Attack action, you may
            // move 1 space" - reached the table as four spaces and no way to stay put.
            //
            // Milestone 8.35z5 correction. That fix derived the offer from `minimumSelections` alone,
            // which conflated two different questions for a genuine `may` node whose HANDLER needs
            // more than zero: "is the whole step declinable" (yes, always, for `may`) and "how few
            // may a taken answer name" (whatever the handler's operation actually requires - one
            // space, exactly, for Living Tsunami's move). Forcing the second down to zero to answer
            // the first also legalised an empty CONFIRM the handler had never agreed to, so the
            // table offered a Confirm the rules then refused right beside the Decline that was the
            // honest empty answer. `declinableRegardlessOfMinimum` carries the first question on its
            // own, sourced fresh from the node's `EffectFailurePolicy` at every pause exactly as
            // `minimumSelections` already is - so a restored, replayed or undone pause still offers
            // exactly what the live one did, and nothing here is persisted separately from the
            // content that decides it.
            DeclineInvocation = kind == EffectChoiceKind.ConfirmOrDecline || minimumSelections == 0 ||
                declinableRegardlessOfMinimum
                ? new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Decline, default, chooserId)
                : null;

            // Milestone 8.46b. FOR AN EXACTLY-ONE CHOICE THIS IS ALWAYS NULL ON A STANDING PAUSE,
            // and no line here was needed to make it so.
            //
            // The user's rule is that a choice admitting exactly one selection is answered in ONE
            // press: the Select that fills it is applied as select-and-confirm inside the same
            // command, in <c>EffectResolutionSession.Answer</c>. So the only state such a choice is
            // ever published in is the empty one, and `chosen.Length >= minimumSelections` reads
            // `0 >= 1` there, which is false. The pause offers no Confirm, and - `chosen` being
            // empty - no Deselect beside it: exactly the standing shape the rule requires, derived
            // from the bounds rather than special-cased into them.
            //
            // The derivation is deliberately NOT narrowed to force null whenever
            // <see cref="ResolvesOnSelection"/> holds. Two callers legitimately hold such a choice
            // WITH its one selection: the transient value Answer builds on its way into Resume, and
            // a snapshot written by an earlier build while the old two-step pause stood - the
            // schema still carries a standing choice's selected set (see
            // <c>AuthoritativeSnapshotSerializer.CardAction</c>) and does not move for this
            // milestone, so a frame restored from one has to stay answerable rather than come back
            // as a pause with nothing that closes it.
            //
            // Confirming and declining are not two names for one answer, even where nothing is
            // named: closing an `up to` with nothing chosen runs the node having taken none, and
            // declining records that it was refused. Milestone 6.5's own cases hold the first of
            // those - "`up to 2` includes none, so the decision may be closed with nothing named" -
            // so the confirm stays exactly as it was and the decline is added beside it. That is
            // exactly the "up to" case: its handler already declares the zero floor honestly, so
            // `minimumSelections` says the same thing for both invocations and nothing above ever
            // has to widen it separately. A `may` node whose handler needs more stays gated here on
            // that real floor - `declinableRegardlessOfMinimum` never relaxes Confirm, only Decline.
            ConfirmInvocation = chosen.Length >= minimumSelections
                ? new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Confirm, default, chooserId)
                : null;
            StopInvocation = canStop
                ? new EffectChoiceCommand(
                    resolutionId, choiceId, EffectChoiceCommandKind.Stop, default, chooserId)
                : null;
        }

        public EffectResolutionId ResolutionId { get; }
        public EffectChoiceId ChoiceId { get; }
        public EffectNodeId NodeId { get; }
        public HeroId ChooserId { get; }
        public EffectChoiceKind Kind { get; }
        public IReadOnlyList<EffectChoiceCandidate> Candidates { get; }
        public IReadOnlyList<EffectChoiceCandidate> SelectedCandidates { get; }

        /// <summary>
        /// Milestone 6.5ac: the offered options the chooser cannot perform. They stay in
        /// <see cref="Candidates"/> and carry no <see cref="SelectInvocations"/> entry, so a player
        /// still sees that the mode exists and cannot spend a card on it.
        ///
        /// Never written to a snapshot. It is derived beside the candidate set at every pause and
        /// re-derived on restore, replay, and retained-prefix Undo, so a payload can neither carry a
        /// stale refusal nor invent one.
        /// </summary>
        public IReadOnlyList<EffectChoiceCandidate> UnavailableCandidates { get; }

        public int MinimumSelections { get; }
        public int MaximumSelections { get; }

        /// <summary>
        /// Milestone 8.46b: whether naming a candidate is the whole answer, so the Select that fills
        /// this choice closes it inside the same command.
        /// </summary>
        /// <remarks>
        /// <para><b>The user's rule, from the decision-window design of 2026-09-18:</b> "choose-
        /// exactly-one answers in one press". A choice whose floor and ceiling are both one has
        /// nothing left to say once a candidate is named - there is no second selection to add, none
        /// to take away that would leave a legal answer, and a Confirm carries no information the
        /// Select did not already carry. Asking for it twice is what playtesters read as the strip
        /// not responding.</para>
        /// <para><b>Why the bounds decide it and not the candidate kind.</b> A mode, a printed
        /// number, a colour, a card and a piece all reach this type as candidates of a choice with
        /// its own bounds; what makes the press final is that exactly one of them is wanted, which
        /// is what the two numbers say. <see cref="EffectChoiceKind.ConfirmOrDecline"/> is excluded
        /// because it offers no candidates at all - its press IS the Confirm - and an `up to 1`
        /// choice is excluded because its floor is zero: closing it having taken none is a real
        /// answer, so it keeps the completion action that gives it.</para>
        /// <para><b>It is read where Select is applied, in the resolution session, and nowhere
        /// else.</b> Presentation has forwarded the authority's own Confirm behind a filled
        /// selection since milestone 6.5c (<c>EffectChoiceCompletion.FindImmediateConfirm</c>), but
        /// that is two accepted commands: the pause really did stand on Deselect and Confirm in
        /// between, a seat rewind stepped back only half the answer, and a remote seat had to
        /// re-send the second command after the host's projection arrived. Moving the rule here
        /// makes the one press one command for every surface at once.</para>
        /// </remarks>
        public bool ResolvesOnSelection =>
            Kind != EffectChoiceKind.ConfirmOrDecline &&
            MinimumSelections == 1 &&
            MaximumSelections == 1;

        /// <summary>
        /// Milestone 8.37. What this choice's integer candidates MEAN, declared by the handler that
        /// offered them. See <see cref="EffectChoiceValueVocabulary"/>.
        /// </summary>
        public EffectChoiceValueVocabulary ValueVocabulary { get; }

        /// <summary>
        /// Milestone 8.35z5. Whether <see cref="DeclineInvocation"/> is offered on the node's own
        /// `may` policy rather than on <see cref="MinimumSelections"/> being zero - the two used to
        /// be answered by the same number, which is what let an empty <see cref="ConfirmInvocation"/>
        /// stand beside Decline for a `may` node whose handler needs more than nothing. See the
        /// constructor for the full account.
        /// </summary>
        public bool DeclinableRegardlessOfMinimum { get; }

        public IReadOnlyList<EffectChoiceCommand> SelectInvocations { get; }
        public IReadOnlyList<EffectChoiceCommand> DeselectInvocations { get; }

        /// <summary>Null while the selection does not yet satisfy the minimum.</summary>
        public EffectChoiceCommand ConfirmInvocation { get; }

        /// <summary>Null unless the decision is one a chooser is allowed to decline outright.</summary>
        public EffectChoiceCommand DeclineInvocation { get; }

        /// <summary>
        /// Milestone 6.5ae: the one command left where no option can be performed, and null wherever
        /// one still can. Accepting it stops the action exactly as p.10's stop already does, so the
        /// player closes a decision that has nothing in it rather than the resolver closing it for
        /// them - the same affordance <c>PendingMovementChoice.CanStop</c> has projected since
        /// Phase 1, on the decision family that had lacked it.
        ///
        /// Never written to a snapshot, for the reason <see cref="UnavailableCandidates"/> is not:
        /// it is derived from that refusal set and the node's own failure policy at every pause and
        /// re-derived on restore, replay, and retained-prefix Undo, so a frame restored after the
        /// last refusal lifts offers the option again rather than the stop.
        /// </summary>
        public EffectChoiceCommand StopInvocation { get; }

        /// <summary>
        /// The same decision with a different selection. The refused options are carried rather than
        /// re-derived, because selecting and deselecting change nothing a feasibility question reads:
        /// a selection is recorded on the frame, and no board, supply, or timing state moves until the
        /// choice is confirmed and the capability resumes. The stop rides with them for the same
        /// reason, and a choice that carries one offers nothing to select in the first place.
        /// </summary>
        public PendingEffectChoice WithSelected(IEnumerable<EffectChoiceCandidate> selected) =>
            new PendingEffectChoice(
                ResolutionId, ChoiceId, NodeId, ChooserId, Kind,
                Candidates, MinimumSelections, MaximumSelections, selected, UnavailableCandidates,
                StopInvocation != null, DeclinableRegardlessOfMinimum, ValueVocabulary);
    }
}

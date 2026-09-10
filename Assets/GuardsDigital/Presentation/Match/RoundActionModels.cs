using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{

    public sealed class TypedInvocationViewModel
    {
        public TypedInvocationViewModel(
            string label,
            DirectorCommand command,
            DefenseBreakdownViewModel defenseBreakdown = null,
            TrayCardViewModel card = null,
            CardShineSlot defenseShineSlot = CardShineSlot.None,
            CardColor swatch = CardColor.None)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A typed invocation requires a label.", nameof(label))
                : label;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            DefenseBreakdown = defenseBreakdown;
            Card = card;
            DefenseShineSlot = defenseShineSlot;
            Swatch = swatch;
        }

        public string Label { get; }
        public DirectorCommand Command { get; }
        public DefenseBreakdownViewModel DefenseBreakdown { get; }

        /// <summary>
        /// Milestone 8.37. The printed colour this control NAMES, when the choice it answers declared
        /// a card-colour vocabulary; <see cref="CardColor.None"/> on every other invocation.
        ///
        /// <para>Tali's Commune with Spirits hands this decision to an ALLY, who is being asked to
        /// name a colour out of the six with no card in front of them. The word alone is answerable;
        /// the word plus the colour it names is the control the table actually has, and the palette
        /// it wears is the SAME one the rest of the HUD draws that colour with
        /// (<c>MatchHudTheme.Face</c>), so a Green named here and a Green card in the tray are one
        /// colour rather than two opinions.</para>
        /// </summary>
        public CardColor Swatch { get; }

        /// <summary>
        /// Which printed badge this Defense offer would defend with, stated by the presenter from
        /// the offered action's typed origin - the rail shield for a secondary Defense, the primary
        /// printed-lines ring for a Duelist or Block - so the view lights it without inferring anything from
        /// card content. <see cref="CardShineSlot.None"/> on every invocation that is not a card
        /// Defense offer, DECLINE included. Milestone 8.30d.
        /// </summary>
        public CardShineSlot DefenseShineSlot { get; }

        /// <summary>
        /// The card this invocation is about, as the tray draws one, or null when it is not about a
        /// card.
        ///
        /// Milestone 8.11b. p.11's Defense response is a choice between cards in the defender's own
        /// hand, and until now it was offered as an abbreviated text button while the same cards sat
        /// on the tray as whole faces - two designs for one card. The face is carried here so the
        /// region can draw the offer with <see cref="DirectorHandsUnityView.BuildStandaloneCard"/>,
        /// which is the builder every other card surface uses. <c>DECLINE DEFENSE</c> is about no card
        /// and carries none, which is what keeps it a distinct control rather than a sixth face.
        /// </summary>
        public TrayCardViewModel Card { get; }
    }

    public sealed class DefenseContributionRowViewModel
    {
        public DefenseContributionRowViewModel(int amount, string sourceLabel)
        {
            Amount = amount;
            SourceLabel = string.IsNullOrWhiteSpace(sourceLabel)
                ? throw new ArgumentException(
                    "A Defense contribution row requires a source label.", nameof(sourceLabel))
                : sourceLabel;
        }

        public int Amount { get; }
        public string SourceLabel { get; }
        public string SignedAmount => Amount >= 0 ? $"+{Amount}" : Amount.ToString();
    }

    public sealed class DefenseBreakdownViewModel
    {
        public DefenseBreakdownViewModel(
            long version,
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            CardInstanceId? cardInstanceId,
            string title,
            string text,
            AttackResolutionOutcome outcome,
            IEnumerable<DefenseContributionRowViewModel> attackRows,
            int attackTotal,
            IEnumerable<DefenseContributionRowViewModel> defenseRows,
            int? defenseTotal,
            DefenseBlockResult blockResult,
            string resultStatement)
        {
            Version = version;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            CardInstanceId = cardInstanceId;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("A Defense breakdown requires a title.", nameof(title))
                : title;
            Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException("A Defense breakdown requires text.", nameof(text))
                : text;
            Outcome = outcome;
            AttackRows = Array.AsReadOnly((attackRows ??
                throw new ArgumentNullException(nameof(attackRows))).ToArray());
            DefenseRows = Array.AsReadOnly((defenseRows ??
                throw new ArgumentNullException(nameof(defenseRows))).ToArray());
            if (AttackRows.Sum(value => value.Amount) != attackTotal ||
                (defenseTotal.HasValue && DefenseRows.Sum(value => value.Amount) != defenseTotal.Value))
            {
                throw new ArgumentException(
                    "Defense breakdown totals must equal their typed contribution rows.");
            }
            AttackTotal = attackTotal;
            DefenseTotal = defenseTotal;
            BlockResult = blockResult;
            ResultStatement = string.IsNullOrWhiteSpace(resultStatement)
                ? throw new ArgumentException(
                    "A Defense breakdown requires a result statement.", nameof(resultStatement))
                : resultStatement;
        }

        public long Version { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public CardInstanceId? CardInstanceId { get; }
        public string Title { get; }
        public string Text { get; }
        public AttackResolutionOutcome Outcome { get; }
        public IReadOnlyList<DefenseContributionRowViewModel> AttackRows { get; }
        public int AttackTotal { get; }
        public IReadOnlyList<DefenseContributionRowViewModel> DefenseRows { get; }
        public int? DefenseTotal { get; }
        public DefenseBlockResult BlockResult { get; }
        public string ResultStatement { get; }
    }

    public sealed class FastTravelOfferViewModel
    {
        public FastTravelOfferViewModel(
            string label,
            ActionDefinitionId replacedMovementActionDefinitionId,
            int legalDestinationCount)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Fast Travel offer requires a label.", nameof(label))
                : label;
            if (!replacedMovementActionDefinitionId.IsDefined || legalDestinationCount <= 0)
            {
                throw new ArgumentException("A Fast Travel offer requires its replaced action and legal destinations.");
            }

            ReplacedMovementActionDefinitionId = replacedMovementActionDefinitionId;
            LegalDestinationCount = legalDestinationCount;
        }

        public string Label { get; }
        public ActionDefinitionId ReplacedMovementActionDefinitionId { get; }
        public int LegalDestinationCount { get; }
    }

    /// <summary>
    /// One printed action on the currently resolving card, bound to the exact offer the authority
    /// projected. The view may place this on a fixed card region; it never has to infer an action from
    /// artwork, printed words, a hero, or a card name.
    /// </summary>
    public sealed class PrintedActionRegionViewModel
    {
        public PrintedActionRegionViewModel(
            ActionOffer offer,
            ActionKind actionKind,
            string actionLabel,
            int secondaryIndex,
            int secondaryCount,
            string assistiveLabel,
            TypedInvocationViewModel invocation,
            FastTravelOfferViewModel fastTravelOffer = null,
            ClearOfferViewModel clearOffer = null,
            CardColor cardColor = CardColor.None)
        {
            if (!offer.SourceCardInstanceId.IsDefined || !offer.ActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("A printed action region requires its exact projected offer.");
            }

            if (offer.InvocationContext != ActionInvocationContext.PrintedPrimary && secondaryIndex < 0)
            {
                throw new ArgumentException("A Secondary printed region requires its ordered index.");
            }

            Offer = offer;
            ActionKind = actionKind;
            ActionLabel = string.IsNullOrWhiteSpace(actionLabel)
                ? throw new ArgumentException("A printed action region requires its typed label.", nameof(actionLabel))
                : actionLabel;
            SecondaryIndex = secondaryIndex;
            SecondaryCount = secondaryCount;
            AssistiveLabel = string.IsNullOrWhiteSpace(assistiveLabel)
                ? throw new ArgumentException(
                    "A printed action region requires an assistive reading.", nameof(assistiveLabel))
                : assistiveLabel;
            Invocation = invocation;
            FastTravelOffer = fastTravelOffer;
            ClearOffer = clearOffer;
            CardColor = cardColor;
        }

        public ActionOffer Offer { get; }
        public CardInstanceId SourceCardInstanceId => Offer.SourceCardInstanceId;
        public ActionDefinitionId ActionDefinitionId => Offer.ActionDefinitionId;
        public ActionInvocationContext InvocationContext => Offer.InvocationContext;
        public ActionKind ActionKind { get; }
        public string ActionLabel { get; }
        public int SecondaryIndex { get; }
        public int SecondaryCount { get; }
        public string AssistiveLabel { get; }

        /// <summary>The exact projected start, or null when the authority stamped the offer unavailable.</summary>
        public TypedInvocationViewModel Invocation { get; }

        /// <summary>The existing typed replacement selectors attached to the action they replace.</summary>
        public FastTravelOfferViewModel FastTravelOffer { get; }
        public ClearOfferViewModel ClearOffer { get; }

        /// <summary>
        /// The printed colour of the card this region sits on - a typed fact from the definition,
        /// carried so the replacement ribbons can wear the card's own cloth.
        /// </summary>
        public CardColor CardColor { get; }
    }

    /// <summary>
    /// Milestone 6.5c: one control of a pending effect choice that names a board object. The space is
    /// the object's identity for this purpose - the same space-as-identity the effect definition binds
    /// - so hovering the control and lighting the board agree without presentation holding a second
    /// notion of which object a control means.
    /// </summary>
    public sealed class EffectChoiceTargetViewModel
    {
        public EffectChoiceTargetViewModel(
            string label,
            HexCoordinate space,
            HexCoordinate? resultingSpace,
            DirectorCommand command,
            TokenInstanceId tokenId = default)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("An effect choice target requires a label.", nameof(label))
                : label;
            Space = space;
            ResultingSpace = resultingSpace;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            TokenId = tokenId;
        }

        public string Label { get; }
        public HexCoordinate Space { get; }

        /// <summary>Where the authority projects this target would end up, when an operation moves it.</summary>
        public HexCoordinate? ResultingSpace { get; }
        public DirectorCommand Command { get; }

        /// <summary>
        /// Milestone 6.3a: the token this control names, or an undefined identity when it names a
        /// hero, a minion, or an empty space.
        ///
        /// It rides beside <see cref="Space"/> rather than replacing it, because the two answer
        /// different questions. The space is what the command carries and what a hero or minion cue is
        /// resolved by; the identity is what lets the board mark the piece without reading occupancy
        /// back out of a coordinate. Milestone 6.6a's finding was exactly this gap: the control read
        /// `TEST - HAZARD` and the board marked nothing, because a token view had no cue to set and
        /// the space alone could not say which piece was meant.
        /// </summary>
        public TokenInstanceId TokenId { get; }
    }

    /// <summary>
    /// Milestone 6.3a: the control that starts a Clear, bound to the exact Attack it replaces.
    ///
    /// It carries the source card and the actor beside the replaced action, because a Clear is one
    /// card's replacement rather than a general-purpose button: the presenter re-reads the offer off
    /// the current projection before it starts anything, and these three are what it re-reads it by.
    /// </summary>
    public sealed class ClearOfferViewModel
    {
        public ClearOfferViewModel(
            string label,
            ActionDefinitionId replacedAttackActionDefinitionId,
            CardInstanceId sourceCardInstanceId,
            HeroId actorId,
            int candidateCount,
            bool isAvailable = true)
        {
            IsAvailable = isAvailable;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Clear offer requires a label.", nameof(label))
                : label;
            if (!replacedAttackActionDefinitionId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !actorId.IsDefined || candidateCount < 0)
            {
                throw new ArgumentException(
                    "A Clear offer requires its replaced Attack, source card, and actor.");
            }

            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActorId = actorId;
            CandidateCount = candidateCount;
        }

        public string Label { get; }
        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public HeroId ActorId { get; }

        /// <summary>
        /// How many adjacent tokens this Clear would offer. Zero is drawn rather than hidden: milestone
        /// 8.8b greys such an offer and refuses it, on the treatment every other refused control here
        /// gets, rather than taking the control off the surface.
        /// </summary>
        public int CandidateCount { get; }

        /// <summary>
        /// Whether the authority currently offers this Clear as one that can be started.
        ///
        /// Milestone 8.8b. It is the authority's own stamp on the replacement offer, never a count made
        /// here: a Clear with no adjacent token could remove nothing whatever the hero chose, so it is
        /// drawn greyed and refused at the start, exactly as an inert Skill and a targetless Attack are.
        /// p.13's "any number" still includes zero wherever a token *is* adjacent, and that case stays
        /// available.
        /// </summary>
        public bool IsAvailable { get; }
    }

    /// <summary>
    /// Milestone 6.3a: one token the open Clear selection offers, and whether it is currently chosen.
    /// The identity is the token's own, so pointing at this control and marking the piece agree
    /// without presentation holding a second notion of which object a control means.
    /// </summary>
    public sealed class ClearTokenCandidateViewModel
    {
        public ClearTokenCandidateViewModel(
            string label,
            TokenInstanceId tokenId,
            HexCoordinate space,
            bool isSelected)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A Clear candidate requires a label.", nameof(label))
                : label;
            TokenId = tokenId.IsDefined
                ? tokenId
                : throw new ArgumentException(
                    "A Clear candidate names the token it stands for.", nameof(tokenId));
            Space = space;
            IsSelected = isSelected;
        }

        public string Label { get; }
        public TokenInstanceId TokenId { get; }
        public HexCoordinate Space { get; }
        public bool IsSelected { get; }
    }

    /// <summary>
    /// Milestone 6.3a: the open Clear selection - every offered token, the exact command for the
    /// subset currently chosen, and the way back out.
    ///
    /// Selection is local to presentation and reaches the session exactly once, on confirm. That is
    /// the shape milestone 6.3 built for: it made Clear one atomic command with no pending
    /// continuation, so there is no authoritative selection state to answer into and inventing one
    /// would be a second Clear rule. <see cref="Cancel"/> is deliberately a label rather than a
    /// <see cref="TypedInvocationViewModel"/> - it submits nothing at all, and a type that cannot
    /// carry a command cannot be routed into one by mistake.
    /// </summary>
    public sealed class ClearSelectionViewModel
    {
        public ClearSelectionViewModel(
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<ClearTokenCandidateViewModel> candidates,
            TypedInvocationViewModel confirm,
            string cancelLabel)
        {
            ReplacedAttackActionDefinitionId = replacedAttackActionDefinitionId.IsDefined
                ? replacedAttackActionDefinitionId
                : throw new ArgumentException(
                    "A Clear selection names the Attack it replaces.",
                    nameof(replacedAttackActionDefinitionId));
            Candidates = Array.AsReadOnly((candidates ??
                throw new ArgumentNullException(nameof(candidates))).ToArray());
            if (Candidates.Select(value => value.TokenId).Distinct().Count() != Candidates.Count)
            {
                throw new ArgumentException(
                    "A Clear selection offers each token once.", nameof(candidates));
            }

            Confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
            CancelLabel = string.IsNullOrWhiteSpace(cancelLabel)
                ? throw new ArgumentException(
                    "A Clear selection requires a cancel label.", nameof(cancelLabel))
                : cancelLabel;
            SelectedTokenIds = Array.AsReadOnly(Candidates
                .Where(value => value.IsSelected)
                .Select(value => value.TokenId)
                .ToArray());
        }

        public ActionDefinitionId ReplacedAttackActionDefinitionId { get; }
        public IReadOnlyList<ClearTokenCandidateViewModel> Candidates { get; }
        public IReadOnlyList<TokenInstanceId> SelectedTokenIds { get; }

        /// <summary>
        /// The projected invocation for exactly the chosen subset. Always present, because p.13
        /// permits any number and the empty subset is a confirmable answer rather than a decline.
        /// </summary>
        public TypedInvocationViewModel Confirm { get; }

        public string CancelLabel { get; }
    }

    /// <summary>
    /// Milestone 6.5ac: one option of a pending effect choice the acting player cannot perform.
    ///
    /// It carries a label and the candidate it stands for, and deliberately no
    /// <see cref="DirectorCommand"/> at all - which is why it is a type of its own rather than a
    /// <see cref="TypedInvocationViewModel"/> with a flag beside it. A control with no command cannot
    /// be submitted through any route a view has, so "greyed" is a structural fact here rather than a
    /// rendering convention a later view could forget.
    ///
    /// There is no reason text. The 2026-08-16 user decision recorded as open rules question 11 is
    /// that greying alone is the surface this owes.
    /// </summary>
    /// <summary>
    /// Milestone 6.5af: one printed action the authority judged unstartable, drawn rather than
    /// dropped. It carries no <c>DirectorCommand</c> at all, for the reason a refused mode does not:
    /// a click cannot reach the session by any route rather than being refused once it arrives.
    /// </summary>
    public sealed class UnavailableActionViewModel
    {
        public UnavailableActionViewModel(string label, ActionDefinitionId actionDefinitionId)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException(
                    "An unavailable action requires a label.", nameof(label))
                : label;
            ActionDefinitionId = actionDefinitionId.IsDefined
                ? actionDefinitionId
                : throw new ArgumentException(
                    "An unavailable action names the action it stands for.", nameof(actionDefinitionId));
        }

        public string Label { get; }

        /// <summary>
        /// The action this control would have started. Presentation holds it so the presenter can
        /// refuse a command naming it rather than only failing to offer one.
        /// </summary>
        public ActionDefinitionId ActionDefinitionId { get; }
    }

    public sealed class UnavailableEffectChoiceViewModel
    {
        public UnavailableEffectChoiceViewModel(string label, EffectChoiceCandidate candidate)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException(
                    "An unavailable effect choice requires a label.", nameof(label))
                : label;
            Candidate = candidate.IsDefined
                ? candidate
                : throw new ArgumentException(
                    "An unavailable effect choice names the candidate it stands for.", nameof(candidate));
        }

        public string Label { get; }

        /// <summary>
        /// The option this control would have answered. Presentation holds it so the presenter can
        /// refuse a command naming it rather than only failing to offer one.
        /// </summary>
        public EffectChoiceCandidate Candidate { get; }
    }

    /// <summary>
    /// Milestone 8.20d: the standing effect choice as the region that draws it needs to know it -
    /// what it is asking about, and its semantic section label.
    ///
    /// <para>Null whenever no effect choice is pending, and non-null whenever one is, so the region
    /// cannot draw a decision's controls without knowing their family. Non-card families paint the
    /// label; card-valued choices use purpose-bearing narration and an unlabelled whole-face row. The
    /// words are composed by <see cref="EffectChoicePromptLabels"/>, which is also where
    /// <see cref="RoundActionViewModel.Status"/> gets its sentence, so a heading and a status line
    /// describing one pause cannot disagree about which pause it is.</para>
    /// </summary>
    public sealed class EffectChoicePromptViewModel
    {
        public EffectChoicePromptViewModel(EffectChoiceSubject subject)
        {
            Subject = subject;
            SectionLabel = EffectChoicePromptLabels.SectionLabel(subject);
        }

        public EffectChoiceSubject Subject { get; }

        public string SectionLabel { get; }
    }

    /// <summary>
    /// Milestone 8.21. The standing removal-prevention decision as the region draws it: the heading it
    /// arrives under, one control per affordable payment - each the whole printed face of the card that
    /// would pay - the decline that is always offered, and the minion the price would keep.
    ///
    /// <para>The payments are the projection's own minted commands, carried through unchanged. This
    /// model composes no option of its own, which is what makes an answer the pause did not project
    /// unsubmittable from this surface rather than merely absent from it.</para>
    /// </summary>
    public sealed class RemovalPreventionPromptViewModel
    {
        public RemovalPreventionPromptViewModel(
            MinionId subjectMinionId,
            IEnumerable<TypedInvocationViewModel> payments,
            TypedInvocationViewModel decline)
        {
            if (!subjectMinionId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention prompt names the miniature it is about, so the board can " +
                    "cue it.",
                    nameof(subjectMinionId));
            }

            SubjectMinionId = subjectMinionId;
            Payments = Array.AsReadOnly((payments ??
                throw new ArgumentNullException(nameof(payments))).ToArray());
            Decline = decline ?? throw new ArgumentNullException(
                nameof(decline),
                "Declining is always offered: the removal was going to happen anyway, and a decision " +
                "that could not be refused would stop the match.");
            SectionLabel = RemovalPreventionPromptLabels.SectionLabel;
        }

        /// <summary>The minion the price would keep on the board, cued there rather than named here.</summary>
        public MinionId SubjectMinionId { get; }

        /// <summary>One control per affordable payment, each carrying the face of the card that pays.</summary>
        public IReadOnlyList<TypedInvocationViewModel> Payments { get; }

        public TypedInvocationViewModel Decline { get; }

        public string SectionLabel { get; }
    }

    /// <summary>
    /// The two voices milestone 8.30d gives the top status bar's narration: the warm lane for a
    /// sentence addressed to the viewing seat, the calm one for everything the match is doing on
    /// its own or at another seat.
    /// </summary>
    public enum NarrationVoice
    {
        Calm = 0,
        YourMove = 1
    }

    public sealed class TeamAgreementChoiceViewModel
    {
        public TeamAgreementChoiceViewModel(
            string label, HeroId heroId, TeamAgreementCommand command)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            HeroId = heroId;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public HeroId HeroId { get; }
        public TeamAgreementCommand Command { get; }
    }

    public sealed class TeamAgreementParticipantViewModel
    {
        public TeamAgreementParticipantViewModel(string label, string status)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }

        public string Label { get; }
        public string Status { get; }
    }

    public sealed class TeamAgreementHudViewModel
    {
        public TeamAgreementHudViewModel(
            TeamAgreementKind kind,
            string heading,
            IEnumerable<TeamAgreementParticipantViewModel> participants,
            IEnumerable<TeamAgreementChoiceViewModel> choices,
            bool isInputEnabled = true)
        {
            Kind = kind;
            Heading = heading ?? throw new ArgumentNullException(nameof(heading));
            Participants = Array.AsReadOnly((participants ??
                throw new ArgumentNullException(nameof(participants))).ToArray());
            Choices = Array.AsReadOnly((choices ??
                Enumerable.Empty<TeamAgreementChoiceViewModel>()).ToArray());
            IsInputEnabled = isInputEnabled;
        }

        public TeamAgreementKind Kind { get; }
        public string Heading { get; }
        public IReadOnlyList<TeamAgreementParticipantViewModel> Participants { get; }
        public IReadOnlyList<TeamAgreementChoiceViewModel> Choices { get; }

        /// <summary>
        /// Milestone 8.35z2. Mirrors <c>IMatchReadModel.IsInputEnabled</c> at the moment this model was
        /// built. A seat whose <see cref="Choices"/> is empty because it is not a participant at all
        /// keeps this <c>true</c> (nothing to wait on); a participant whose own statement is in flight
        /// - or whose whole client is momentarily desynchronized - reads <c>false</c> here, which is
        /// what tells an empty <see cref="Choices"/> apart from a genuinely quiet one.
        /// </summary>
        public bool IsInputEnabled { get; }
    }

    internal static class TeamAgreementHudPresentation
    {
        public static TeamAgreementHudViewModel Create(IMatchReadModel projection)
        {
            var agreement = projection?.TeamAgreement;
            if (agreement == null)
                return null;

            // Milestone 8.35z2. `NetworkMatchClient` forces `AuthorizedCommands` empty for the whole
            // window its own `actionable` gate is false - a participant's just-submitted statement in
            // flight, or a stale/resynchronizing/disconnected client - which is otherwise
            // indistinguishable here from a seat that was never a participant at all (permanently
            // empty, e.g. the non-choosing Minion Battle side). Carrying the flag lets the view draw
            // "WAITING FOR HOST…" only for the first case.
            var isInputEnabled = projection.IsInputEnabled;

            var heroNames = projection.Setup.Heroes.ToDictionary(
                hero => hero.HeroId,
                hero => hero.DisplayName.ToUpperInvariant());
            string Name(HeroId heroId) => heroNames.TryGetValue(heroId, out var name)
                ? name
                : heroId.Value.ToUpperInvariant();

            if (agreement.Kind == TeamAgreementKind.Initiative)
            {
                var candidateNames = agreement.Candidates.ToDictionary(
                    candidate => candidate.CardInstanceId,
                    candidate => Name(candidate.HeroId));

                // Milestone 8.35h. Read with the same tolerance the hero-name lookup above already
                // has. A bare indexer and a `Single` sat beside that `TryGetValue` fallback in this
                // one method, which is the shape milestone 8.35e's scoped-seat crash came in: a
                // presenter shared between the Director and a seat must not assume the Director's
                // complete collections. A candidate this seat cannot name degrades to its own
                // identity rather than taking the panel - and, before 8.35h, the whole HUD - down.
                string CandidateName(CardInstanceId cardInstanceId) =>
                    candidateNames.TryGetValue(cardInstanceId, out var candidateName)
                        ? candidateName
                        : cardInstanceId.Value.ToUpperInvariant();
                HeroId CandidateHero(CardInstanceId cardInstanceId)
                {
                    foreach (var candidate in agreement.Candidates)
                    {
                        if (candidate.CardInstanceId.Equals(cardInstanceId))
                        {
                            return candidate.HeroId;
                        }
                    }

                    return default;
                }

                return new TeamAgreementHudViewModel(
                    agreement.Kind,
                    "WHO ACTS FIRST?",
                    agreement.Participants.Select(participant =>
                        new TeamAgreementParticipantViewModel(
                            Name(participant.HeroId),
                            participant.HasStatement
                                ? "chose " + CandidateName(participant.PreferredCardInstanceId)
                                : "is choosing")),
                    agreement.AuthorizedCommands.Select(command =>
                        new TeamAgreementChoiceViewModel(
                            CandidateName(command.PreferredCardInstanceId),
                            CandidateHero(command.PreferredCardInstanceId),
                            command)),
                    isInputEnabled);
            }

            return new TeamAgreementHudViewModel(
                agreement.Kind,
                "TEAM AGREEMENT",
                agreement.Participants.Select(participant =>
                    new TeamAgreementParticipantViewModel(
                        Name(participant.HeroId),
                        participant.Agreed ? "agreed" : "is deciding")),
                agreement.AuthorizedCommands.Select(command =>
                    new TeamAgreementChoiceViewModel(
                        command.Kind == TeamAgreementCommandKind.WithdrawMinionBattleAgreement
                            ? "WITHDRAW MY AGREEMENT"
                            : "AGREE WITH THIS SELECTION",
                        default,
                        command)),
                isInputEnabled);
        }
    }

    public sealed class RoundActionViewModel
    {
        public RoundActionViewModel(
            long version,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            string status,
            IEnumerable<TypedInvocationViewModel> actions,
            TypedInvocationViewModel endTurnInvocation,
            TypedInvocationViewModel continueEndOfRoundInvocation,
            IEnumerable<TypedInvocationViewModel> initiativeChoices,
            IEnumerable<TypedInvocationViewModel> defenseChoices,
            IEnumerable<FastTravelOfferViewModel> fastTravelOffers = null,
            IEnumerable<TypedInvocationViewModel> effectChoices = null,
            IEnumerable<EffectChoiceTargetViewModel> effectChoiceTargets = null,
            IEnumerable<TypedInvocationViewModel> effectChoiceDestinations = null,
            IEnumerable<HeroRespawnOptionViewModel> heroRespawnOptions = null,
            TypedInvocationViewModel resolveWithoutRespawn = null,
            IEnumerable<UnavailableEffectChoiceViewModel> unavailableEffectChoices = null,
            IEnumerable<UnavailableActionViewModel> unavailableActions = null,
            IEnumerable<ClearOfferViewModel> clearOffers = null,
            ClearSelectionViewModel clearSelection = null,
            SystemTransitionPromptViewModel systemTransitionPrompt = null,
            EffectChoicePromptViewModel effectChoicePrompt = null,
            RemovalPreventionPromptViewModel removalPreventionPrompt = null,
            IEnumerable<PrintedActionRegionViewModel> printedActionRegions = null,
            TeamAgreementHudViewModel teamAgreement = null,
            bool seatUndoAvailable = false,
            bool isAwaitingViewerInput = false,
            string statusDetail = null,
            bool endTurnCancelsOpenAttack = false,
            string endTurnUnavailableReason = null,
            IEnumerable<TrayCardViewModel> levelUpHandPreview = null)
        {
            LevelUpHandPreview = Array.AsReadOnly((levelUpHandPreview ??
                Enumerable.Empty<TrayCardViewModel>()).ToArray());
            IsAwaitingViewerInput = isAwaitingViewerInput;
            StatusDetail = statusDetail;
            SeatUndoAvailable = seatUndoAvailable;
            EndTurnCancelsOpenAttack = endTurnCancelsOpenAttack;
            EndTurnUnavailableReason = endTurnUnavailableReason;
            SystemTransitionPrompt = systemTransitionPrompt;
            EffectChoicePrompt = effectChoicePrompt;
            RemovalPreventionPrompt = removalPreventionPrompt;
            TeamAgreement = teamAgreement;
            Version = version;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Phase = phase;
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Actions = Array.AsReadOnly((actions ?? throw new ArgumentNullException(nameof(actions))).ToArray());
            EndTurnInvocation = endTurnInvocation;
            ContinueEndOfRoundInvocation = continueEndOfRoundInvocation;
            InitiativeChoices = Array.AsReadOnly((initiativeChoices ??
                throw new ArgumentNullException(nameof(initiativeChoices))).ToArray());
            DefenseChoices = Array.AsReadOnly((defenseChoices ??
                throw new ArgumentNullException(nameof(defenseChoices))).ToArray());
            FastTravelOffers = Array.AsReadOnly((fastTravelOffers ??
                Enumerable.Empty<FastTravelOfferViewModel>()).ToArray());
            EffectChoices = Array.AsReadOnly((effectChoices ??
                Enumerable.Empty<TypedInvocationViewModel>()).ToArray());
            EffectChoiceTargets = Array.AsReadOnly((effectChoiceTargets ??
                Enumerable.Empty<EffectChoiceTargetViewModel>()).ToArray());
            EffectChoiceDestinations = Array.AsReadOnly((effectChoiceDestinations ??
                Enumerable.Empty<TypedInvocationViewModel>()).ToArray());
            HeroRespawnOptions = Array.AsReadOnly((heroRespawnOptions ??
                Enumerable.Empty<HeroRespawnOptionViewModel>()).ToArray());
            ResolveWithoutRespawn = resolveWithoutRespawn;
            UnavailableEffectChoices = Array.AsReadOnly((unavailableEffectChoices ??
                Enumerable.Empty<UnavailableEffectChoiceViewModel>()).ToArray());
            UnavailableActions = Array.AsReadOnly((unavailableActions ??
                Enumerable.Empty<UnavailableActionViewModel>()).ToArray());
            ClearOffers = Array.AsReadOnly((clearOffers ??
                Enumerable.Empty<ClearOfferViewModel>()).ToArray());
            ClearSelection = clearSelection;
            PrintedActionRegions = Array.AsReadOnly((printedActionRegions ??
                Enumerable.Empty<PrintedActionRegionViewModel>()).ToArray());
            if (clearSelection != null && !ClearOffers.Any(offer =>
                    offer.ReplacedAttackActionDefinitionId.Equals(
                        clearSelection.ReplacedAttackActionDefinitionId)))
            {
                throw new ArgumentException(
                    "An open Clear selection belongs to an offer this model still carries.",
                    nameof(clearSelection));
            }

            // Milestone 8.20d: a decision's controls and the heading that names them arrive together
            // or not at all. Before this the controls were appended to the shared `ACTIONS` row, so
            // there was nothing to disagree with; now that the region asks the model what to call
            // them, a model carrying controls and no prompt would put a discard chooser back under
            // the heading this milestone exists to stop.
            if (effectChoicePrompt == null &&
                (EffectChoices.Count > 0 || UnavailableEffectChoices.Count > 0))
            {
                throw new ArgumentException(
                    "An effect choice's controls are drawn under the prompt that names them, so a " +
                    "model carrying either list carries the prompt too.",
                    nameof(effectChoicePrompt));
            }

            // Milestone 8.35v (the THIRD amendment): END TURN reads as exactly one of three
            // mutually exclusive facts - a direct invocation, "press it and it cancels the open
            // attack selection first", or a named reason it is drawn disabled - never two at once,
            // which is what would let a view draw one and a driver read another.
            var endTurnFactCount = (endTurnInvocation != null ? 1 : 0) +
                (endTurnCancelsOpenAttack ? 1 : 0) +
                (endTurnUnavailableReason != null ? 1 : 0);
            if (endTurnFactCount > 1)
            {
                throw new ArgumentException(
                    "End Turn's invocation, its cancels-open-attack flag and its unavailable " +
                    "reason are mutually exclusive.",
                    nameof(endTurnInvocation));
            }

            HudHeader =$"MIRRORED WASP QUICK GAME - DIRECTOR MODE - ROUND {roundIndex} - " +
                $"TURN {turnIndex}/{RoundResolutionAuthority.TurnsPerRound} - {phase.ToString().ToUpperInvariant()}";
        }

        public long Version { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundResolutionPhase Phase { get; }
        public string Status { get; }

        /// <summary>
        /// Which voice the narration lane speaks <see cref="Status"/> in. Milestone 8.30d: warm
        /// when this model carries anything the viewing seat can act on - the sentence is then
        /// addressed to the player - and calm otherwise. It is derived from the same lists the
        /// region renders, so the voice and the controls cannot drift apart; a seat-scoped model
        /// carries only that seat's own offers, which is what makes presence mean "yours".
        /// </summary>
        /// <summary>
        /// Whether the match is waiting on this viewer, from
        /// <see cref="AwaitedInputQuery"/>. The top bar's gold and this lane's warm voice read the
        /// one flag, so the two surfaces cannot come to disagree about whose move it is.
        /// </summary>
        public bool IsAwaitingViewerInput { get; }

        /// <summary>
        /// Milestone 8.35n (the FIFTH amendment, typography). A secondary fact the narration lane
        /// draws smaller and quieter beside <see cref="Status"/>, demoting Initiative to secondary
        /// detail exactly as the amendment specifies - "Initiative may stay as secondary detail" -
        /// rather than folding it back into the headline sentence it used to be the whole of. Null
        /// wherever a branch has nothing to demote.
        /// </summary>
        public string StatusDetail { get; }

        public NarrationVoice Voice =>
            IsAwaitingViewerInput ? NarrationVoice.YourMove : NarrationVoice.Calm;
        public IReadOnlyList<TypedInvocationViewModel> Actions { get; }
        public TypedInvocationViewModel EndTurnInvocation { get; }

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment: "end turn button should be present the whole time
        /// on players action turn, even if he has attack selection on"). True exactly when the
        /// acting seat has opened an Attack whose target is not yet chosen - the one open,
        /// not-yet-committed selection this build has no authority-offered decline for, so END TURN
        /// composes the seat's own bounded rewind (milestone 8.35f, still short of its wall) to
        /// discard it before completing and releasing the turn, all in the one press. Mutually
        /// exclusive with <see cref="EndTurnInvocation"/> and <see cref="EndTurnUnavailableReason"/>.
        /// </summary>
        public bool EndTurnCancelsOpenAttack { get; }

        /// <summary>
        /// Milestone 8.35v. Non-null exactly when it is this seat's own active turn and END TURN is
        /// drawn DISABLED rather than hidden - the user's rule that nothing is greyed without saying
        /// why, extended to END TURN itself: a printed rule leaves the standing step no legal decline
        /// (<see cref="ActionUnavailabilityLabels.MandatoryStep"/>), or the resolution has moved past
        /// anything this seat can answer or cancel (<see cref="ActionUnavailabilityLabels.AnotherSeatDeciding"/>).
        /// Mutually exclusive with <see cref="EndTurnInvocation"/> and
        /// <see cref="EndTurnCancelsOpenAttack"/>; null (and the control hidden) wherever it is not
        /// this seat's own active turn at all, exactly as before this milestone.
        /// </summary>
        public string EndTurnUnavailableReason { get; }

        public TypedInvocationViewModel ContinueEndOfRoundInvocation { get; }

        /// <summary>
        /// Milestone 8.35f. Whether this seat's own UNDO control stands. It carries no invocation,
        /// because a rewind is not a command a surface could forward: the control asks the client,
        /// the client asks the host, and the host decides by the same rule that published this.
        /// </summary>
        public bool SeatUndoAvailable { get; }
        public IReadOnlyList<TypedInvocationViewModel> InitiativeChoices { get; }

        public TeamAgreementHudViewModel TeamAgreement { get; }
        public IReadOnlyList<TypedInvocationViewModel> DefenseChoices { get; }
        public IReadOnlyList<FastTravelOfferViewModel> FastTravelOffers { get; }

        /// <summary>
        /// The decision a paused effect frame is waiting on, as the exact invocations the projection
        /// already built. Empty whenever no effect choice is pending.
        /// </summary>
        public IReadOnlyList<TypedInvocationViewModel> EffectChoices { get; }

        /// <summary>
        /// Milestone 6.5c: the candidates of that decision which name a board object. Each carries its
        /// space, so pointing at the control tells the board which object it means.
        /// </summary>
        public IReadOnlyList<EffectChoiceTargetViewModel> EffectChoiceTargets { get; }

        /// <summary>
        /// The candidates which name an empty space. They carry legal invocations but no control: the
        /// board lights them, and clicking a lit space is how they are answered.
        /// </summary>
        public IReadOnlyList<TypedInvocationViewModel> EffectChoiceDestinations { get; }

        /// <summary>
        /// Milestone 6.5ac: the options of that decision the acting player cannot perform. They are
        /// shown greyed rather than hidden, so a player sees the mode exists and that it cannot be
        /// taken, and none of them carries a command.
        /// </summary>
        public IReadOnlyList<UnavailableEffectChoiceViewModel> UnavailableEffectChoices { get; }

        /// <summary>
        /// Milestone 6.5af: the actions this card prints that the authority has judged unstartable.
        /// They keep their label and their place in the offer order and carry no command, so a player
        /// reads that the action exists and cannot be taken - the treatment milestone 6.5ac gave a
        /// refused mode, on an offer.
        /// </summary>
        public IReadOnlyList<UnavailableActionViewModel> UnavailableActions { get; }

        /// <summary>
        /// Ordinary printed starts as typed regions of the resolving face. Headless callers retain
        /// <see cref="Actions"/> and <see cref="UnavailableActions"/>; the configured HUD draws this
        /// list instead of duplicating those starts in its external dock.
        /// </summary>
        public IReadOnlyList<PrintedActionRegionViewModel> PrintedActionRegions { get; }

        public IReadOnlyList<HeroRespawnOptionViewModel> HeroRespawnOptions { get; }
        public TypedInvocationViewModel ResolveWithoutRespawn { get; }

        /// <summary>
        /// Milestone 6.3a: the Clear replacements the current card offers, one per available
        /// replaceable Attack. Empty when the card prints none, which is how a player reads that this
        /// card cannot Clear rather than reading nothing at all.
        /// </summary>
        public IReadOnlyList<ClearOfferViewModel> ClearOffers { get; }

        /// <summary>
        /// The Clear the player has opened, or <c>null</c>. It is presentation state and no command
        /// has been submitted while it stands: the card is still unresolved, the authoritative action
        /// choice is unchanged, and cancelling returns to exactly that.
        /// </summary>
        public ClearSelectionViewModel ClearSelection { get; }

        /// <summary>
        /// Milestone 7.8a: the decision a paused multi-step system transition is waiting on, or
        /// <c>null</c> when none is paused. It shares the one contextual region with every other
        /// decision, because the match asks exactly one thing at a time.
        /// </summary>
        public SystemTransitionPromptViewModel SystemTransitionPrompt { get; }

        /// <summary>
        /// User directive (2026-09-07): "when leveling. Show current hand (all 5 cards) above the
        /// offer." This seat's own current hand, read-only, populated only while
        /// <see cref="SystemTransitionPrompt"/> is rulebook p.15's Upgrade a Card
        /// (<see cref="GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade"/>)
        /// and only for a real seat (empty for the Director, who already sees every hand). Empty
        /// otherwise. Never padded to five - a hand already down to fewer cards shows exactly what
        /// stands.
        /// </summary>
        public IReadOnlyList<TrayCardViewModel> LevelUpHandPreview { get; }

        /// <summary>
        /// Milestone 8.20d: the standing effect choice, or <c>null</c> when none is pending. It is
        /// what the contextual region reads to head this decision's own section, rather than adding
        /// its controls to the row of actions the player is not being asked for.
        /// </summary>
        public EffectChoicePromptViewModel EffectChoicePrompt { get; }

        /// <summary>
        /// Milestone 8.21: the standing removal-prevention decision, or <c>null</c> when none is
        /// pending. It is the whole of the contextual region while it stands, for the reason a paused
        /// system transition is: the authority refuses every other command until it is answered.
        /// </summary>
        public RemovalPreventionPromptViewModel RemovalPreventionPrompt { get; }

        public string HudHeader { get; }
    }

    /// <summary>
    /// One option of a paused system transition, labelled from the projected description and carrying
    /// the exact invocation the continuation already built.
    ///
    /// Milestone 7.8a. The label is composed here from typed facts - card instances, definition
    /// references, a printed icon, a space - and never by decoding the option identity: this type has no
    /// access to one beyond the command it forwards unchanged.
    /// </summary>
    public sealed class SystemTransitionOptionViewModel
    {
        public SystemTransitionOptionViewModel(
            string label,
            string detail,
            DirectorCommand command,
            CardColor color = CardColor.None,
            CardInstanceId sourceCardInstanceId = default,
            TrayCardViewModel card = null,
            string returnedCardName = null,
            PrintedValueChannel itemIcon = PrintedValueChannel.None,
            string setAsideCardName = null)
        {
            SourceCardInstanceId = sourceCardInstanceId;
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A transition option requires a label.", nameof(label))
                : label;
            Detail = detail ?? string.Empty;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Color = color;
            ColorName = color == CardColor.None ? string.Empty : MatchHudTheme.ColorName(color);
            Card = card;
            ReturnedCardName = returnedCardName ?? string.Empty;
            ItemIcon = itemIcon;
            SetAsideCardName = setAsideCardName ?? string.Empty;
            ItemLabel = itemIcon == PrintedValueChannel.None
                ? string.Empty
                : $"{MatchHudTheme.ChannelToken(itemIcon)} {MatchHudTheme.Signed(1)}";
        }

        public string Label { get; }

        /// <summary>What else taking this option does - the sibling set aside, and the item it becomes.</summary>
        public string Detail { get; }

        public DirectorCommand Command { get; }

        /// <summary>
        /// The printed colour of the card this option is about, or <see cref="CardColor.None"/> for an
        /// option that is not about a card at all - p.8's Place names a space.
        ///
        /// For p.15's upgrade it is the upgraded card's own projected colour, which is also the taken
        /// card's: the page's own rule is that the two cards found in the deck "have the same color as
        /// the chosen card". So the control can wear that colour the way the tray's cards do, and a
        /// player matches the option to the card in their hand by the same swatch rather than by
        /// reading two names. It is paired with <see cref="ColorName"/> wherever it is drawn.
        /// </summary>
        public CardColor Color { get; }

        public string ColorName { get; }

        /// <summary>
        /// The card this option is one of the answers for, or undefined when the decision is not about a
        /// card. p.15 asks two questions in one option - which card to upgrade, and which of the two
        /// next-Tier cards to take - so several consecutive options share one upgraded card.
        ///
        /// It exists so a surface can group what the projection has already grouped, by breaking a line
        /// where this changes. That is a line break rather than a sort: the options stay in the order the
        /// pause offered them, which is the rule presentation must not bend.
        /// </summary>
        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>
        /// The card taking this option puts in the hero's hand, as the tray draws a card, or null for an
        /// option that is not about a card at all - p.8's Place names a space.
        ///
        /// <para>Milestone 8.8a. p.15's decision is a choice between two cards, and every fact a player
        /// needs to make it - Initiative, the printed values, the action and its qualifier, p.9's Range
        /// or Radius icon, and the exact words the face prints - is on the card rather than in a name.
        /// It is built by the same reading the tray builds a card with, so a candidate and the card that
        /// arrives in hand cannot be drawn as two different cards.</para>
        /// </summary>
        public TrayCardViewModel Card { get; }

        /// <summary>
        /// p.15's "Return this card to your deck": the display name of the card the hero gives up. Empty
        /// when the option is not an upgrade.
        /// </summary>
        public string ReturnedCardName { get; }

        /// <summary>
        /// p.16's icon on the card p.15 sets aside - the item this option gains - or
        /// <see cref="PrintedValueChannel.None"/> when nothing is set aside or no dashboard slot accepts
        /// its icon.
        ///
        /// <para>It is the <em>other</em> candidate's icon, which is why it belongs on the option rather
        /// than on the card face: a player choosing between two cards of one colour is also choosing
        /// which of the two items they gain, and the item they gain is printed on the card they do not
        /// take.</para>
        /// </summary>
        public PrintedValueChannel ItemIcon { get; }

        /// <summary>`ATK +1` - the gained item as the rails already draw an item bonus, or empty.</summary>
        public string ItemLabel { get; }

        /// <summary>The display name of the card p.15 sets aside, or empty when none is.</summary>
        public string SetAsideCardName { get; }
    }

    /// <summary>
    /// The decision a paused multi-step system transition is waiting on, as a surface.
    ///
    /// Milestone 7.8a builds this because no view drew one: rulebook p.8's Lane-Push respawn Place has
    /// been reachable since milestone 6.11 and p.15's Upgrade a Card since milestone 7.6, and the
    /// configured scene could only pass them through a director command that named an option without
    /// being able to say what it was.
    ///
    /// The options are the projection's own, in the projection's order. This adds none, removes none,
    /// reorders none, and chooses none.
    /// </summary>
    public sealed class SystemTransitionPromptViewModel
    {
        public SystemTransitionPromptViewModel(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind kind,
            string sectionLabel,
            string promptLabel,
            string statusLabel,
            IEnumerable<SystemTransitionOptionViewModel> options)
        {
            Kind = kind;
            SectionLabel = sectionLabel ?? throw new ArgumentNullException(nameof(sectionLabel));
            PromptLabel = promptLabel ?? throw new ArgumentNullException(nameof(promptLabel));
            StatusLabel = statusLabel ?? string.Empty;
            Options = Array.AsReadOnly((options ?? throw new ArgumentNullException(nameof(options))).ToArray());
            if (Options.Count == 0)
            {
                throw new ArgumentException(
                    "A paused step offers at least one option, so a prompt for one is never empty.",
                    nameof(options));
            }
        }

        public GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind Kind { get; }

        /// <summary>The family the shared contextual gutter names - `LEVEL UP` or `LANE PUSH`.</summary>
        public string SectionLabel { get; }

        /// <summary>Who is deciding and what they are deciding.</summary>
        public string PromptLabel { get; }

        /// <summary>What the decision has already cost and what is left, for a purchase that has one.</summary>
        public string StatusLabel { get; }

        public IReadOnlyList<SystemTransitionOptionViewModel> Options { get; }
    }

    public sealed class HeroRespawnOptionViewModel
    {
        public HeroRespawnOptionViewModel(
            string label,
            HexCoordinate destination,
            DirectorCommand command)
        {
            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("A hero respawn option requires a label.", nameof(label))
                : label;
            Destination = destination;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Label { get; }
        public HexCoordinate Destination { get; }
        public DirectorCommand Command { get; }
    }

    public interface IRoundActionView
    {
        void Render(RoundActionViewModel model);
    }
}

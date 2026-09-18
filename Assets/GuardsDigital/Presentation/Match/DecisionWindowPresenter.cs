using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.46c. The one surface that asks the player a question.
    ///
    /// <para><b>Why it exists.</b> On 2026-09-18 the user watched playtesters meet the round-action
    /// strip's prompts - "Who acts first?", a card's <c>MODE #1 RELOAD-BORROW-ARM</c> buttons, the
    /// accept-or-decline banners - and reported that they did not understand what they were choosing
    /// and often did not see the controls at all, because that strip reads as STATUS rather than as a
    /// QUESTION. The window is the replacement they designed row by row: the card or cards in
    /// question, one control per choice, and text saying what happens when it is pressed.</para>
    ///
    /// <para><b>It composes nothing.</b> Every control carries a command the standing contract already
    /// published, resolved through <see cref="PendingDecisionCommandBinding"/> exactly as every other
    /// presenter here does, so an answer the authority did not offer is unsubmittable from this
    /// surface rather than merely undrawn.</para>
    ///
    /// <para><b>Owner-only.</b> A model is built only for the seat that owns the standing decision -
    /// team-owned decisions belong to every seat on the side, and the Director owns them all at once,
    /// in which case the title names the side or the hero. Every other viewer gets no window and keeps
    /// the narration line. The ownership reading is
    /// <see cref="IMatchReadModel.DecisionOwner"/>'s, the same one
    /// <c>RoundActionPresenter.FormatPublicDecisionWait</c> makes, rather than a new predicate.</para>
    /// </summary>
    public sealed class DecisionWindowPresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly IDecisionWindowView view;

        // NOT readonly, for the reason ISeatScopedPresenter exists: a networked client is composed
        // before the lobby grants its seat, so this identity is bound a second time afterwards.
        private HeroId homeHeroId;
        private IMatchReadModel lastProjection;
        private DecisionWindowViewModel lastViewModel;

        // The manual toggle. It is keyed by the decision it was made on, so it survives exactly as
        // long as that decision stands: the user's rule is that every NEW decision re-decides the
        // state from its own shape rather than remembering what the last one was toggled to
        // (Brogan's Throwing Axe expands for its arm, then docks for its board target).
        private string manualStateKey;
        private DecisionWindowState manualState;

        /// <summary>The Director-shaped constructor, composing the Director's own client.</summary>
        public DecisionWindowPresenter(IMatchSession session, IDecisionWindowView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public DecisionWindowPresenter(IMatchClient session, IDecisionWindowView view)
            : this(session, view, default)
        {
        }

        public DecisionWindowPresenter(IMatchClient session, IDecisionWindowView view, HeroId homeHeroId)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.homeHeroId = homeHeroId;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => homeHeroId;

        public event Action<IMatchReadModel> SessionProjectionChanged;

        /// <summary>The window currently drawn, or null while this viewer owns no standing decision.</summary>
        public DecisionWindowViewModel Model => lastViewModel;

        /// <inheritdoc />
        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            homeHeroId = heroId;
            lastProjection = null;
            manualStateKey = null;
            Refresh();
            return true;
        }

        public void Refresh() => view.Render(GetOrCreateViewModel(session.Projection));

        /// <summary>
        /// Toggles the standing window between its two states, and remembers the choice for as long as
        /// this decision stands. It answers nothing and submits nothing: minimising is a reading
        /// preference, not a move.
        /// </summary>
        public bool ToggleState()
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (current == null || !current.IsMinimisable)
            {
                return false;
            }

            manualStateKey = current.DecisionKey;
            manualState = current.State == DecisionWindowState.Expanded
                ? DecisionWindowState.Docked
                : DecisionWindowState.Expanded;
            lastProjection = null;
            Refresh();
            return true;
        }

        /// <summary>
        /// Answers the standing decision with one drawn control.
        ///
        /// <para>It refuses a control this presenter's own current model does not carry, which is the
        /// same discipline <c>RoundActionPresenter.Submit</c> applies: what that refuses in practice is
        /// a control left over from a model the authority has moved past.</para>
        ///
        /// <para>An effect-choice select is followed by the confirm the authority itself offers when
        /// the selection has reached its maximum - <see cref="EffectChoiceCompletion"/>, the same
        /// one-press completion a board click has had since milestone 6.5c - so a choose-exactly-one
        /// answers in one press and no <c>DONE</c> ever needs drawing for it.</para>
        /// </summary>
        public bool Submit(DecisionWindowControlViewModel control)
        {
            var current = GetOrCreateViewModel(session.Projection);
            if (control == null || current == null || !control.IsAnswerable ||
                !Carries(current, control))
            {
                Refresh();
                return false;
            }

            if (control.AgreementCommand != null)
            {
                var agreed = session.SubmitAgreement(control.AgreementCommand);
                if (agreed.Accepted)
                {
                    SessionProjectionChanged?.Invoke(agreed.Projection);
                }
                else
                {
                    Refresh();
                }

                return agreed.Accepted;
            }

            var result = session.Submit(control.Command);
            if (!result.Accepted)
            {
                // A refusal leaves the authoritative decision standing and hands back the projection
                // that still holds it; rebuilding from that is what puts the drawn window back in step
                // with authority rather than with the control the player just used.
                Refresh();
                return false;
            }

            SessionProjectionChanged?.Invoke(result.Projection);
            if (control.Command.Kind == DirectorCommandKind.AnswerEffectChoice)
            {
                var confirm = EffectChoiceCompletion.FindImmediateConfirm(
                    result.Projection, control.Command.EffectChoice);
                if (confirm != null)
                {
                    var completed = session.Submit(confirm);
                    if (completed.Accepted)
                    {
                        SessionProjectionChanged?.Invoke(completed.Projection);
                    }
                }
            }

            return true;
        }

        private static bool Carries(
            DecisionWindowViewModel model, DecisionWindowControlViewModel control) =>
            model.Controls.Any(candidate => ReferenceEquals(candidate, control)) ||
            ReferenceEquals(model.CornerControl, control) ||
            model.Controls.Any(candidate =>
                candidate.Command != null && control.Command != null &&
                candidate.Command.Equals(control.Command)) ||
            (model.CornerControl?.Command != null && control.Command != null &&
                model.CornerControl.Command.Equals(control.Command)) ||
            model.Controls.Any(candidate =>
                candidate.AgreementCommand != null && control.AgreementCommand != null &&
                candidate.AgreementCommand.Equals(control.AgreementCommand)) ||
            (model.CornerControl?.AgreementCommand != null && control.AgreementCommand != null &&
                model.CornerControl.AgreementCommand.Equals(control.AgreementCommand));

        private DecisionWindowViewModel GetOrCreateViewModel(IMatchReadModel projection)
        {
            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = ApplyManualState(CreateViewModel(projection, homeHeroId));
            }

            return lastViewModel;
        }

        private DecisionWindowViewModel ApplyManualState(DecisionWindowViewModel model)
        {
            if (model == null)
            {
                manualStateKey = null;
                return null;
            }

            if (manualStateKey == null || !string.Equals(manualStateKey, model.DecisionKey, StringComparison.Ordinal))
            {
                // A NEW decision: the toggle does not carry over, and the family decides again.
                manualStateKey = null;
                return model;
            }

            return manualState == model.State
                ? model
                : new DecisionWindowViewModel(
                    model.Version, model.Family, manualState, model.DecisionKey, model.Title,
                    model.Eyebrow, model.Question, model.Count, model.ContextCards, model.Controls,
                    model.CornerControl, model.Statements, model.IsMinimisable);
        }

        /// <summary>
        /// The standing decision this viewer owns, as the window that asks it - or <c>null</c> when
        /// nothing this viewer may answer is standing.
        ///
        /// <para>The families are read in <c>RoundActionPresenter.FormatStatus</c>'s own precedence, so
        /// the window and the narration line can never disagree about which decision is standing.</para>
        /// </summary>
        public static DecisionWindowViewModel CreateViewModel(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            if (projection.Phase == RoundResolutionPhase.GameEnded ||
                !OwnsTheStandingDecision(projection, viewerHeroId))
            {
                return null;
            }

            return CreateMinionBattle(projection, viewerHeroId) ??
                CreateTiedInitiative(projection, viewerHeroId) ??
                CreateDefense(projection, viewerHeroId) ??
                CreateRemovalPrevention(projection, viewerHeroId) ??
                CreateEffectChoice(projection, viewerHeroId) ??
                CreateCorrection(projection, viewerHeroId) ??
                CreateRespawn(projection, viewerHeroId);
        }

        /// <summary>
        /// Whether this viewer may answer whatever is standing. Read from
        /// <see cref="IMatchReadModel.DecisionOwner"/> - public by design, naming who is awaited while
        /// the answers stay owner-private - rather than from the presence of a pending field, because
        /// a seat projection publishes some public stages (the Minion Battle's agreement) to both
        /// sides and the ownership question has exactly one honest answer.
        /// </summary>
        private static bool OwnsTheStandingDecision(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var owner = projection.DecisionOwner;
            if (!owner.IsDefined)
            {
                return false;
            }

            if (!viewerHeroId.IsDefined)
            {
                // The undefined hero is the DIRECTOR'S identity, and the Director legitimately owns
                // every decision at once.
                return true;
            }

            if (owner.IsTeamOwned)
            {
                var viewer = projection.Setup.Heroes
                    .FirstOrDefault(hero => hero.HeroId.Equals(viewerHeroId));
                return viewer != null && viewer.TeamSide == owner.TeamSide;
            }

            return owner.HeroIds.Any(hero => hero.Equals(viewerHeroId));
        }

        // ---------------------------------------------------------------------------------------
        // Minion Battle removals - docked, single-verb ratification.
        // ---------------------------------------------------------------------------------------

        private static DecisionWindowViewModel CreateMinionBattle(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var agreement = projection.TeamAgreement?.Kind == TeamAgreementKind.MinionBattle
                ? projection.TeamAgreement
                : null;
            var choice = projection.PendingMinionBattleChoice;
            if (agreement == null && choice == null)
            {
                return null;
            }

            var side = agreement?.ChoosingSide ?? choice.ChoosingSide;
            var required = agreement?.RequiredSelectionCount ?? choice.RequiredRemovalCount;
            var selected = agreement?.SelectedUnits.Count ?? choice.SelectedUnits.Count;
            var controls = new List<DecisionWindowControlViewModel>();
            DecisionWindowControlViewModel corner = null;

            if (agreement != null)
            {
                // On a TEAM the first press marks that hero ready and the last one resolves, which is
                // exactly what the agreement's own AGREE command already does. The heading, the
                // per-hero ledger and the agree/withdraw wording go; one verb stays.
                var agree = agreement.AuthorizedCommands.FirstOrDefault(command =>
                    command.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                if (agree != null && selected >= required)
                {
                    controls.Add(new DecisionWindowControlViewModel(
                        RemoveTheseMinionsLabel, agreementCommand: agree, isPrimary: true));
                }

                var withdraw = agreement.AuthorizedCommands.FirstOrDefault(command =>
                    command.Kind == TeamAgreementCommandKind.WithdrawMinionBattleAgreement);
                if (withdraw != null)
                {
                    // "Withdraw My Agreement survives as NOT YET, the one take-back the seat undo
                    // cannot replace" - the user's own cross-cutting rule for this series.
                    corner = new DecisionWindowControlViewModel(
                        NotYetLabel, agreementCommand: withdraw,
                        role: DecisionWindowControlRole.Corner);
                }
            }
            else if (choice.CanConfirm)
            {
                // A SOLO side has no agreement to make: the same verb IS the confirm.
                controls.Add(new DecisionWindowControlViewModel(
                    RemoveTheseMinionsLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.MinionBattle,
                        command => command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals &&
                            command.TeamSide == choice.ConfirmInvocation.ChoosingSide &&
                            command.EndOfRoundResolutionId.Equals(
                                choice.ConfirmInvocation.ResolutionId) &&
                            command.EndOfRoundChoiceId.Equals(choice.ConfirmInvocation.ChoiceId)),
                    isPrimary: true));
            }

            var ready = agreement == null
                ? Array.Empty<string>()
                : agreement.Participants
                    .Where(participant => participant.Agreed && !participant.HeroId.Equals(viewerHeroId))
                    .Select(participant => HeroName(projection, participant.HeroId) + " is ready")
                    .ToArray();
            var waitingOn = agreement == null
                ? Array.Empty<string>()
                : agreement.Participants
                    .Where(participant => !participant.Agreed)
                    .Select(participant => HeroName(projection, participant.HeroId))
                    .ToArray();
            var viewerAgreed = viewerHeroId.IsDefined && agreement != null &&
                agreement.Participants.Any(participant =>
                    participant.HeroId.Equals(viewerHeroId) && participant.Agreed);

            string question;
            if (viewerAgreed && waitingOn.Length > 0)
            {
                question = "Waiting for " + string.Join(", ", waitingOn) + ".";
            }
            else if (selected < required)
            {
                question = "Click minions on the board until " + required +
                    (required == 1 ? " is chosen." : " are chosen.");
            }
            else if (ready.Length > 0)
            {
                question = string.Join(", ", ready) + ". Remove these minions?";
            }
            else
            {
                question = "Remove these minions?";
            }

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.MinionBattleRemoval,
                DecisionWindowState.Docked,
                "minion-battle/" + (choice != null
                    ? choice.ResolutionId + "/" + choice.ChoiceId
                    : side.ToString()),
                Scoped($"REMOVE {required} {(required == 1 ? "MINION" : "MINIONS")}",
                    MinionChoiceLabels.TeamLabel(side), viewerHeroId),
                "MINION BATTLE · " + MinionChoiceLabels.TeamLabel(side),
                question,
                $"{selected} of {required} chosen",
                controls: controls,
                cornerControl: corner,
                statements: ready);
        }

        // ---------------------------------------------------------------------------------------
        // Tied Initiative - both forms, expanded, the tied cards as faces.
        // ---------------------------------------------------------------------------------------

        private static DecisionWindowViewModel CreateTiedInitiative(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var agreement = projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative
                ? projection.TeamAgreement
                : null;
            var pending = projection.PendingInitiativeChoice;
            if (agreement == null && pending == null)
            {
                return null;
            }

            var side = agreement?.ChoosingSide ?? pending.OwningSide;
            var controls = new List<DecisionWindowControlViewModel>();
            var statements = new List<string>();
            string key;

            if (agreement != null)
            {
                HeroId CandidateHero(CardInstanceId cardInstanceId) => agreement.Candidates
                    .Where(candidate => candidate.CardInstanceId.Equals(cardInstanceId))
                    .Select(candidate => candidate.HeroId)
                    .FirstOrDefault();

                var mine = viewerHeroId.IsDefined
                    ? agreement.Participants.FirstOrDefault(participant =>
                        participant.HeroId.Equals(viewerHeroId))
                    : null;
                foreach (var command in agreement.AuthorizedCommands.Where(candidate =>
                    candidate.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate))
                {
                    var hero = CandidateHero(command.PreferredCardInstanceId);
                    controls.Add(new DecisionWindowControlViewModel(
                        ActsFirstLabel(HeroName(projection, hero)),
                        agreementCommand: command,
                        card: CardFace(projection, command.PreferredCardInstanceId),
                        // Re-pressing replaces a pick, so a stated preference is marked rather than
                        // withdrawn: the control stays live with the same command behind it.
                        isChosen: mine != null && mine.HasStatement &&
                            mine.PreferredCardInstanceId.Equals(command.PreferredCardInstanceId)));
                }

                statements.AddRange(agreement.Participants
                    .Where(participant => participant.HasStatement)
                    .Select(participant =>
                        HeroName(projection, participant.HeroId) + " prefers " +
                        HeroName(projection, CandidateHero(participant.PreferredCardInstanceId))));
                key = "initiative/" + agreement.ProposalId;
            }
            else
            {
                foreach (var cardId in pending.CandidateCardInstanceIds)
                {
                    var owner = projection.Cards
                        .Where(card => card.Id.Equals(cardId))
                        .Select(card => card.OwnerId)
                        .FirstOrDefault();
                    var captured = cardId;
                    controls.Add(new DecisionWindowControlViewModel(
                        ActsFirstLabel(HeroName(projection, owner)),
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Initiative,
                            command => command.Kind == DirectorCommandKind.ChooseTiedActor &&
                                command.TeamSide == pending.OwningSide &&
                                command.CardInstanceId.Equals(captured)),
                        card: CardFace(projection, cardId)));
                }

                key = "initiative/" + side + "/" + string.Join(
                    ",", pending.CandidateCardInstanceIds.Select(card => card.Value));
            }

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.TiedInitiative,
                DecisionWindowState.Expanded,
                key,
                Scoped("WHO ACTS FIRST?", MinionChoiceLabels.TeamLabel(side), viewerHeroId),
                "TIED INITIATIVE · " + MinionChoiceLabels.TeamLabel(side),
                "These cards tie on Initiative. Choose which hero takes the turn first.",
                controls: controls,
                statements: statements);
        }

        // ---------------------------------------------------------------------------------------
        // Defense, and rulebook p.13's removal prevention in the same window.
        // ---------------------------------------------------------------------------------------

        private static DecisionWindowViewModel CreateDefense(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var choice = projection.PendingDefenseChoice;
            var preview = projection.PendingDefensePreview;
            if (choice == null || preview == null ||
                !preview.ResolutionId.Equals(choice.ResolutionId) ||
                !preview.ChoiceId.Equals(choice.ChoiceId))
            {
                return null;
            }

            var attack = projection.HasPendingAttackResolution ? projection.PendingAttackResolution : null;
            var attackCardName = attack == null
                ? null
                : CardName(projection, attack.SourceCardInstanceId);
            var contextCards = new List<DecisionWindowCardViewModel>();
            if (attack != null)
            {
                var face = CardFace(projection, attack.SourceCardInstanceId);
                if (face != null)
                {
                    var range = projection.Definitions
                        .Where(definition => definition.Reference.Equals(attack.SourceDefinitionReference))
                        .SelectMany(definition => definition.ActionDefinitions)
                        .Where(action => action.Id.Equals(attack.ActionDefinitionId))
                        .Select(action => action.AttackRange)
                        .FirstOrDefault();
                    contextCards.Add(new DecisionWindowCardViewModel(
                        face,
                        "ATTACK " + choice.AttackTotal,
                        range == AttackRangeKind.Ranged ? "RANGED" : null));
                }
            }

            var controls = new List<DecisionWindowControlViewModel>();
            foreach (var offer in choice.LegalDefenseOffers)
            {
                var offerPreview = preview.Choices.FirstOrDefault(value =>
                    value.CardInstanceId.HasValue &&
                    value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));
                if (offerPreview == null)
                {
                    continue;
                }

                var action = projection.Definitions
                    .Where(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .SelectMany(definition => definition.ActionDefinitions)
                    .FirstOrDefault(value => value.Id.Equals(offer.ActionDefinitionId));
                var captured = offer;
                controls.Add(new DecisionWindowControlViewModel(
                    (CardName(projection, offer.SourceCardInstanceId) ??
                        offer.SourceCardInstanceId.Value).ToUpperInvariant(),
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Defense,
                        command => command.Kind == DirectorCommandKind.ChooseDefenseCard &&
                            command.ResolutionId.Equals(choice.ResolutionId) &&
                            command.ChoiceId.Equals(choice.ChoiceId) &&
                            command.CardInstanceId.Equals(captured.SourceCardInstanceId)),
                    card: CardFace(projection, offer.SourceCardInstanceId),
                    // Rulebook p.14's Block prints no Defense value at all, so it is named by what it
                    // does rather than by a number it does not have.
                    valueChip: offerPreview.DefenseTotal.HasValue
                        ? "DEFENSE " + offerPreview.DefenseTotal.Value
                        : offerPreview.BlockResult == DefenseBlockResult.Blocked
                            ? "BLOCK - CONDITION MET"
                            : "BLOCK - CONDITION NOT MET",
                    shineSlot: action?.Origin == ActionDefinitionOrigin.PrintedPrimary
                        ? CardShineSlot.PrimaryRing
                        : CardShineSlot.SecondaryDefense));
            }

            var corner = choice.CanDecline
                ? new DecisionWindowControlViewModel(
                    TakeTheHitLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Defense,
                        command => command.Kind == DirectorCommandKind.DeclineDefense &&
                            command.ResolutionId.Equals(choice.ResolutionId) &&
                            command.ChoiceId.Equals(choice.ChoiceId)),
                    role: DecisionWindowControlRole.Corner)
                : null;

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.Defense,
                DecisionWindowState.Expanded,
                "defense/" + choice.ResolutionId + "/" + choice.ChoiceId,
                Scoped(
                    attackCardName == null
                        ? "DEFEND AGAINST THIS ATTACK?"
                        : "DEFEND AGAINST " + attackCardName.ToUpperInvariant() + "?",
                    HeroName(projection, choice.ChooserId), viewerHeroId),
                HeroName(projection, choice.AttackerId) + " ATTACKS " +
                    HeroName(projection, choice.ChooserId),
                controls.Count == 0
                    ? "No card can answer this attack."
                    : "Choose a card to defend with, or take the hit.",
                controls: controls,
                cornerControl: corner,
                contextCards: contextCards);
        }

        private static DecisionWindowViewModel CreateRemovalPrevention(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var prompt = projection.PendingRemovalPrevention;
            if (prompt == null)
            {
                return null;
            }

            var subject = projection.Minions.FirstOrDefault(minion =>
                minion.MinionId.Equals(prompt.SubjectMinionId));
            var subjectName = subject == null
                ? "A FRIENDLY MINION"
                : MinionChoiceLabels.TeamLabel(subject.TeamSide) + " " +
                    subject.MinionType.ToString().ToUpperInvariant();
            var controls = prompt.Payments.Select(payment => new DecisionWindowControlViewModel(
                    (CardName(projection, payment.PaymentCardInstanceId) ??
                        payment.PaymentCardInstanceId.Value).ToUpperInvariant(),
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, payment.Invocation)),
                    card: CardFace(projection, payment.PaymentCardInstanceId),
                    valueChip: "DISCARD TO KEEP IT"))
                .ToArray();

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.RemovalPrevention,
                DecisionWindowState.Expanded,
                "removal-prevention/" + prompt.RequestId,
                Scoped("KEEP " + subjectName + "?",
                    HeroName(projection, prompt.DecidingHeroId), viewerHeroId),
                RemovalPreventionPromptLabels.SectionLabel,
                RemovalPreventionPromptLabels.StatusLine(prompt),
                controls: controls,
                cornerControl: new DecisionWindowControlViewModel(
                    RemovalPreventionPromptLabels.DeclineLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, prompt.DeclineInvocation)),
                    role: DecisionWindowControlRole.Corner));
        }

        // ---------------------------------------------------------------------------------------
        // Effect choices, by what they are asking about.
        // ---------------------------------------------------------------------------------------

        private static DecisionWindowViewModel CreateEffectChoice(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var choice = projection.PendingEffectChoice;
            if (choice == null)
            {
                return null;
            }

            var subject = EffectChoicePromptLabels.SubjectOf(
                choice, projection.PendingEffectChoicePreview);
            var family = FamilyOf(subject);
            var boardAnswered = family == DecisionWindowFamily.Direction ||
                family == DecisionWindowFamily.PieceOrSpace;
            var sourceCardId = SourceCardOf(projection, choice);
            var sourceDefinitionId = DefinitionIdOf(projection, sourceCardId);
            var previews = new HashSet<EffectValue>(
                projection.PendingEffectChoicePreview.Select(preview => preview.CandidateValue));

            var controls = new List<DecisionWindowControlViewModel>();
            DecisionWindowControlViewModel corner = null;
            BuildEffectControls(
                projection, choice, subject, sourceDefinitionId, previews, boardAnswered, controls);

            if (choice.DeclineInvocation != null)
            {
                corner = new DecisionWindowControlViewModel(
                    DeclineLabelFor(subject, sourceDefinitionId, choice),
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.DeclineInvocation)),
                    role: DecisionWindowControlRole.Corner);
            }

            // An optional step's decline IS one of its two answers, so it stands beside the accept
            // rather than small in a corner: "two buttons naming the consequence" is the user's row.
            if (family == DecisionWindowFamily.OptionalStep && corner != null)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    corner.Label, corner.Command, role: DecisionWindowControlRole.Answer));
                corner = null;
            }

            var contextCards = new List<DecisionWindowCardViewModel>();
            if (!boardAnswered && family != DecisionWindowFamily.CardChoice)
            {
                var face = CardFace(projection, sourceCardId);
                if (face != null)
                {
                    contextCards.Add(new DecisionWindowCardViewModel(face));
                }
            }

            return new DecisionWindowViewModel(
                projection.Version,
                family,
                boardAnswered ? DecisionWindowState.Docked : DecisionWindowState.Expanded,
                "effect/" + choice.ResolutionId + "/" + choice.ChoiceId + "/" + choice.NodeId,
                Scoped(
                    TitleOf(subject, sourceCardId, projection),
                    HeroName(projection, choice.ChooserId), viewerHeroId),
                EffectChoicePromptLabels.SectionLabel(subject),
                family == DecisionWindowFamily.Direction
                    ? "Choose a direction"
                    : EffectChoicePromptLabels.StatusLine(choice, subject, projection),
                choice.MaximumSelections > 1
                    ? $"{choice.SelectedCandidates.Count} of {choice.MaximumSelections} chosen"
                    : null,
                contextCards,
                controls,
                corner);
        }

        private static void BuildEffectControls(
            IMatchReadModel projection,
            PendingEffectChoice choice,
            EffectChoiceSubject subject,
            CardDefinitionId sourceDefinitionId,
            HashSet<EffectValue> previews,
            bool boardAnswered,
            List<DecisionWindowControlViewModel> controls)
        {
            var ordinals = new Dictionary<EffectChoiceCandidate, int>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                ordinals[choice.Candidates[index]] = index + 1;
            }

            // A board-answered choice has no CANDIDATE controls anywhere - the pieces, the spaces and
            // (from milestone 8.46d) the six direction arrows are the controls, where they stand. It
            // keeps the answers that have no board surface, which is why this gate covers the two
            // candidate loops and not the verbs below them: rulebook p.10's "up to two" is answered
            // by clicking two minions and then pressing DONE, and a docked bar with no DONE would
            // leave that selection unconfirmable.
            foreach (var candidate in boardAnswered
                ? Enumerable.Empty<EffectChoiceCandidate>()
                : choice.Candidates)
            {
                // A candidate the board already draws is answered by clicking the piece or the space
                // it names - milestone 8.11a's rule - so the window never offers it a second time.
                if (candidate.Kind != EffectChoiceCandidateKind.Option &&
                    previews.Contains(candidate.Value))
                {
                    continue;
                }

                var deselect = choice.DeselectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.Equals(candidate));
                var select = choice.SelectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.Equals(candidate));
                var invocation = deselect ?? select;
                if (invocation == null)
                {
                    continue;
                }

                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    value => value.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(value.EffectChoice, invocation));
                controls.Add(new DecisionWindowControlViewModel(
                    CandidateLabel(projection, choice, candidate, ordinals[candidate], sourceDefinitionId),
                    command,
                    role: deselect != null
                        ? DecisionWindowControlRole.Deselect
                        : DecisionWindowControlRole.Answer,
                    card: candidate.Value.Kind == EffectValueKind.CardInstance
                        ? CardFace(projection, candidate.Value.CardInstanceId)
                        : null,
                    swatch: subject == EffectChoiceSubject.Colour &&
                        candidate.Value.Kind == EffectValueKind.Integer
                            ? (CardColor)candidate.Value.IntegerValue
                            : CardColor.None,
                    isChosen: deselect != null));
            }

            foreach (var unavailable in boardAnswered
                ? Enumerable.Empty<EffectChoiceCandidate>()
                : choice.UnavailableCandidates)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    CandidateLabel(
                        projection, choice, unavailable,
                        ordinals.TryGetValue(unavailable, out var ordinal) ? ordinal : 1,
                        sourceDefinitionId),
                    unavailableReason: ActionUnavailabilityLabels.Generic));
            }

            // DONE stands only where the selection may really be unfinished. An exactly-one choice
            // answers on its own press - EffectChoiceCompletion follows the select with the confirm
            // the authority itself offered - so drawing a second press for it would ask the player to
            // say the same thing twice.
            if (choice.ConfirmInvocation != null &&
                choice.Kind != EffectChoiceKind.ConfirmOrDecline &&
                choice.MaximumSelections > 1)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    EffectChoiceLabels.CompletionLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation)),
                    role: DecisionWindowControlRole.Confirm,
                    isPrimary: true));
            }

            // A printed `may` is accepted by its own one control, named for what accepting does.
            if (choice.ConfirmInvocation != null && choice.Kind == EffectChoiceKind.ConfirmOrDecline)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    AcceptLabelFor(sourceDefinitionId, choice),
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation)),
                    isPrimary: true));
            }

            if (choice.StopInvocation != null)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    "STOP HERE",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.StopInvocation))));
            }
        }

        // ---------------------------------------------------------------------------------------
        // The two decisions that already resolve on the click: correction and respawn.
        // ---------------------------------------------------------------------------------------

        private static DecisionWindowViewModel CreateCorrection(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var pending = projection.PendingDisplacedMinionCorrection;
            if (pending == null)
            {
                return null;
            }

            var question = pending.Stage == DisplacedMinionCorrectionStage.SelectSubject
                ? "Click the displaced minion to bring back."
                : pending.Kind == DisplacedMinionCorrectionKind.Place
                    ? "Click a lit space to place it."
                    : "Click a lit space to move it to.";
            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.Correction,
                DecisionWindowState.Docked,
                "correction/" + pending.ResolutionId + "/" + pending.ChoiceId + "/" + pending.Stage,
                Scoped("PUT THE MINION BACK", MinionChoiceLabels.TeamLabel(pending.ChoosingSide),
                    viewerHeroId),
                "CORRECTION · " + MinionChoiceLabels.TeamLabel(pending.ChoosingSide),
                question);
        }

        private static DecisionWindowViewModel CreateRespawn(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var respawn = projection.PendingHeroRespawnChoice;
            if (respawn == null)
            {
                return null;
            }

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.HeroRespawn,
                DecisionWindowState.Docked,
                "respawn/" + respawn.HeroId.Value + "/" + respawn.CardInstanceId.Value,
                Scoped("COME BACK TO THE BOARD", HeroName(projection, respawn.HeroId), viewerHeroId),
                "RESPAWN",
                "Click a lit Spawn space to come back.",
                cornerControl: new DecisionWindowControlViewModel(
                    DontRespawnLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.HeroRespawn,
                        command => command.Kind == DirectorCommandKind.ResolveCardWithoutRespawn &&
                            command.HeroId.Equals(respawn.DeclineInvocation.HeroId) &&
                            command.CardInstanceId.Equals(respawn.DeclineInvocation.CardInstanceId)),
                    role: DecisionWindowControlRole.Corner));
        }

        // ---------------------------------------------------------------------------------------
        // Words. Every label names the CONSEQUENCE, never the mechanism, and every declared one comes
        // from the content's own table with today's label as the fallback.
        // ---------------------------------------------------------------------------------------

        /// <summary>The single verb rulebook p.8's Minion Battle ratifies with.</summary>
        public const string RemoveTheseMinionsLabel = "REMOVE THESE MINIONS";

        /// <summary>The one take-back the seat undo cannot replace.</summary>
        public const string NotYetLabel = "NOT YET";

        /// <summary>Declining rulebook p.11's Defense, named for what it costs.</summary>
        public const string TakeTheHitLabel = "TAKE THE HIT";

        /// <summary>Resolving the card without coming back onto the board.</summary>
        public const string DontRespawnLabel = "DON'T RESPAWN";

        private static string ActsFirstLabel(string heroName) =>
            heroName.ToUpperInvariant() + " ACTS FIRST";

        /// <summary>
        /// The consequence declared beside the content, upper-cased here rather than authored in
        /// capitals - and today's label wherever milestone 8.46a's catalog answers nothing, so this
        /// window is complete against a catalog that is still being filled.
        /// </summary>
        private static string CandidateLabel(
            IMatchReadModel projection,
            PendingEffectChoice choice,
            EffectChoiceCandidate candidate,
            int ordinal,
            CardDefinitionId sourceDefinitionId)
        {
            if (candidate.Kind == EffectChoiceCandidateKind.Option &&
                DecisionLabelCatalog.TryGetLabel(sourceDefinitionId, candidate.OptionNodeId, out var declared))
            {
                return declared.Chosen.ToUpperInvariant();
            }

            if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                candidate.Value.Kind == EffectValueKind.Integer &&
                choice.ValueVocabulary != EffectChoiceValueVocabulary.None)
            {
                return EffectChoiceLabels.VocabularyLabel(
                    choice.ValueVocabulary, candidate.Value.IntegerValue);
            }

            if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                candidate.Value.Kind == EffectValueKind.Integer)
            {
                return candidate.Value.IntegerValue.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                candidate.Value.Kind == EffectValueKind.CardInstance)
            {
                var name = CardName(projection, candidate.Value.CardInstanceId);
                if (name != null)
                {
                    return name.ToUpperInvariant();
                }
            }

            return EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinal);
        }

        private static string AcceptLabelFor(
            CardDefinitionId sourceDefinitionId, PendingEffectChoice choice) =>
            DecisionLabelCatalog.TryGetLabel(sourceDefinitionId, choice.NodeId, out var declared)
                ? declared.Chosen.ToUpperInvariant()
                : EffectChoiceLabels.AcceptLabel;

        private static string DeclineLabelFor(
            EffectChoiceSubject subject, CardDefinitionId sourceDefinitionId, PendingEffectChoice choice) =>
            subject == EffectChoiceSubject.OptionalStep &&
            DecisionLabelCatalog.TryGetLabel(sourceDefinitionId, choice.NodeId, out var declared) &&
            declared.HasDeclined
                ? declared.Declined.ToUpperInvariant()
                : EffectChoiceLabels.DeclineLabel;

        private static string TitleOf(
            EffectChoiceSubject subject, CardInstanceId sourceCardId, IMatchReadModel projection)
        {
            var name = CardName(projection, sourceCardId);
            switch (subject)
            {
                case EffectChoiceSubject.OptionalStep:
                    return name == null ? "DO IT?" : name.ToUpperInvariant() + "?";
                case EffectChoiceSubject.Colour:
                    return "NAME A COLOUR";
                case EffectChoiceSubject.Direction:
                    return "WHICH WAY?";
                case EffectChoiceSubject.Number:
                    return "HOW FAR?";
                case EffectChoiceSubject.Card:
                    return "WHICH CARD?";
                case EffectChoiceSubject.Mode:
                    return name == null ? "CHOOSE ONE" : name.ToUpperInvariant();
                default:
                    return "WHICH ONE?";
            }
        }

        private static DecisionWindowFamily FamilyOf(EffectChoiceSubject subject)
        {
            switch (subject)
            {
                case EffectChoiceSubject.OptionalStep:
                    return DecisionWindowFamily.OptionalStep;
                case EffectChoiceSubject.Mode:
                    return DecisionWindowFamily.Mode;
                case EffectChoiceSubject.Card:
                    return DecisionWindowFamily.CardChoice;
                case EffectChoiceSubject.Number:
                    return DecisionWindowFamily.Number;
                case EffectChoiceSubject.Colour:
                    return DecisionWindowFamily.Colour;
                case EffectChoiceSubject.Direction:
                    return DecisionWindowFamily.Direction;
                default:
                    return DecisionWindowFamily.PieceOrSpace;
            }
        }

        /// <summary>
        /// On the Director - who answers for every seat at once - the title names whose decision this
        /// is. On a seat it does not, because a seat's own screen already knows.
        /// </summary>
        private static string Scoped(string title, string scope, HeroId viewerHeroId) =>
            viewerHeroId.IsDefined || string.IsNullOrEmpty(scope)
                ? title
                : title + " — " + scope.ToUpperInvariant();

        private static CardInstanceId SourceCardOf(
            IMatchReadModel projection, PendingEffectChoice choice)
        {
            var resolution = projection.PendingSkillResolution;
            return resolution != null && resolution.ResolutionId.Equals(choice.ResolutionId)
                ? resolution.SourceCardInstanceId
                : default;
        }

        private static CardDefinitionId DefinitionIdOf(
            IMatchReadModel projection, CardInstanceId cardInstanceId) =>
            !cardInstanceId.IsDefined
                ? default
                : projection.Cards
                    .Where(card => card.Id.Equals(cardInstanceId))
                    .Select(card => card.DefinitionReference.DefinitionId)
                    .FirstOrDefault();

        private static string CardName(IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (!cardInstanceId.IsDefined)
            {
                return null;
            }

            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(cardInstanceId));
            return card == null
                ? null
                : projection.Definitions
                    .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference))?
                    .DisplayName;
        }

        /// <summary>
        /// One offered card as the tray draws it - <c>CurrentRoundCards.TrayCard</c> and nothing
        /// composed beside it, so a card in this window and the same card in the hand are one face.
        /// </summary>
        private static TrayCardViewModel CardFace(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (!cardInstanceId.IsDefined)
            {
                return null;
            }

            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(cardInstanceId));
            return card == null
                ? null
                : CurrentRoundCards.TrayCard(
                    projection,
                    card,
                    card.State.Location == CardLocation.Hand
                        ? TrayCardGroup.Available
                        : TrayCardGroup.Discarded,
                    0,
                    true,
                    false,
                    isAwaitedAnswer: true);
        }

        private static string HeroName(IMatchReadModel projection, HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                return string.Empty;
            }

            var hero = projection.Setup.Heroes.FirstOrDefault(value => value.HeroId.Equals(heroId));
            return hero?.DisplayName ?? heroId.Value;
        }
    }
}

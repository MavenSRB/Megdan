using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
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
                var next = CreateViewModel(projection, homeHeroId);
                if (next == null && RetainsTiedInitiativeAcrossConnectionPause(projection))
                {
                    // A teammate going away pauses the session and strips this seat's private detail
                    // and commands, but the public owner and authority version still say the SAME
                    // tied-Initiative question stands. Keep its already-drawn reading until either the
                    // detail returns or authority advances; the session interaction wall owns the
                    // temporary inability to press it.
                    return lastViewModel;
                }

                lastViewModel = ApplyManualState(next);
            }

            return lastViewModel;
        }

        private bool RetainsTiedInitiativeAcrossConnectionPause(IMatchReadModel projection) =>
            lastViewModel?.Family == DecisionWindowFamily.TiedInitiative &&
            lastViewModel.Version == projection.Version &&
            projection.Phase != RoundResolutionPhase.GameEnded &&
            DecisionOwnerNamesViewer(projection, homeHeroId);

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
                    model.CornerControl, model.Statements, model.IsMinimisable, model.HandPreview);
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
                CreateLevelUp(projection, viewerHeroId) ??
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

            // Milestone 8.35z2's window, and the reason this guard is here rather than left to each
            // family: a client whose own accepted answer is still in flight - or which is stale,
            // resynchronizing or disconnected - has its pending answers stripped and its authorized
            // commands emptied, while the PUBLIC owner fact stays. Building a window there would
            // either draw controls that answer nothing or throw inside
            // `PendingDecisionCommandBinding`, and milestone 8.35e's live playtest is the record of
            // what a throw on a surface reader path costs. The narration line covers the wait, which
            // is exactly what `RoundActionPresenter.FormatPublicDecisionWait` already does for it.
            if (!projection.IsInputEnabled)
            {
                return false;
            }

            return DecisionOwnerNamesViewer(projection, viewerHeroId);
        }

        private static bool DecisionOwnerNamesViewer(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var owner = projection.DecisionOwner;

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
        // Minion Battle removals - docked. Milestone 9.29: each player picks for themselves and the
        // minions go when every pick matches; the single verb stays only for the Director.
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
            var title = Scoped($"REMOVE {required} {(required == 1 ? "MINION" : "MINIONS")}",
                MinionChoiceLabels.TeamLabel(side), viewerHeroId);
            var eyebrow = "MINION BATTLE · " + MinionChoiceLabels.TeamLabel(side);
            var key = "minion-battle/" + (choice != null
                ? choice.ResolutionId + "/" + choice.ChoiceId
                : side.ToString());

            if (agreement != null)
            {
                // Milestone 9.29. Each player picks for themselves on the board and the minions go
                // the moment every pick matches, so this bar has no verb at all: it counts the
                // viewer's own picks and says whom the removal is waiting on. The 2026-09-30
                // playtest's shared selection changed under three players at once; this bar only
                // ever names a teammate, never moves the viewer's picks.
                var own = agreement.ViewerPicks;
                string question;
                if (own.Count < required)
                {
                    // Milestone 8.58's short second line: the COUNT beside it already says how many.
                    question = "Click the board.";
                }
                else
                {
                    var unfinished = agreement.Participants
                        .Where(participant => !participant.HeroId.Equals(agreement.ViewerHeroId) &&
                            !participant.PicksComplete)
                        .Select(participant => ShortHeroName(projection, participant.HeroId))
                        .ToArray();
                    var different = agreement.Participants
                        .Where(participant => !participant.HeroId.Equals(agreement.ViewerHeroId) &&
                            participant.PicksComplete &&
                            !(participant.Picks.Count == own.Count && participant.Picks.All(own.Contains)))
                        .Select(participant => ShortHeroName(projection, participant.HeroId))
                        .ToArray();
                    question = different.Length > 0
                        ? string.Join(", ", different) +
                            (different.Length == 1 && required == 1
                                ? " picked a different minion."
                                : " picked differently.")
                        : unfinished.Length > 0
                            ? "Waiting for " + string.Join(", ", unfinished) + "."
                            : "Removing.";
                }

                // Not minimisable: with no verb the expanded panel would hold nothing at all, and every
                // answer is a click on the board the bar must never cover.
                return new DecisionWindowViewModel(
                    projection.Version,
                    DecisionWindowFamily.MinionBattleRemoval,
                    DecisionWindowState.Docked,
                    key,
                    title,
                    eyebrow,
                    question,
                    $"{own.Count} of {required} picked",
                    isMinimisable: false);
            }

            // The Director, and a side with no agreement to make: the same verb IS the confirm.
            var selected = choice.SelectedUnits.Count;
            var controls = new List<DecisionWindowControlViewModel>();
            if (choice.CanConfirm)
            {
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

            var soloQuestion = selected < required ? "Click the board." : "Remove these minions?";

            // Milestone 8.58, the mockup's own bar, and the user's note on the shipped one: the verb is
            // DRAWN AND DISABLED while the bar is still counting, rather than appearing out of nowhere
            // on the last click. A control drawn unavailable carries no command at all, so a press
            // cannot reach the session by any route - the structural rule
            // `DecisionWindowControlViewModel` has enforced since 8.46c.
            if (controls.Count == 0)
            {
                controls.Add(new DecisionWindowControlViewModel(
                    RemoveTheseMinionsLabel, isPrimary: true, unavailableReason: soloQuestion));
            }

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.MinionBattleRemoval,
                DecisionWindowState.Docked,
                key,
                title,
                eyebrow,
                soloQuestion,
                $"{selected} of {required} chosen",
                controls: controls);
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
                            mine.PreferredCardInstanceId.Equals(command.PreferredCardInstanceId),
                        hero: hero));
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
                        card: CardFace(projection, cardId),
                        hero: owner));
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
        // Rulebook p.15's Upgrade a Card - expanded, minimisable, everything the strip showed.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.46g. p.15's Upgrade a Card as the window that asks it.
        ///
        /// <para><b>It derives nothing.</b> Every fact here comes from
        /// <see cref="RoundActionPresenter.CreateSystemTransitionPrompt(IMatchReadModel)"/> - the
        /// prompt the strip has built since milestone 7.8a - so the hero and the level reached, the
        /// coins paid and remaining, each candidate's whole printed face, the card it sets aside and
        /// the item that card becomes are read from one derivation rather than two. The three strings the
        /// strip draws are carried WHOLE and unedited: its section label is this window's eyebrow, its
        /// prompt line is the question, its status line is the count.</para>
        ///
        /// <para>The OTHER system-transition kind - p.8's blocked-Spawn Lane Push placement - is
        /// answered by clicking a lit space on the board and is deliberately not taken: it opens no
        /// window at all and keeps the strip's own explanation beside the board.</para>
        /// </summary>
        private static DecisionWindowViewModel CreateLevelUp(
            IMatchReadModel projection, HeroId viewerHeroId)
        {
            var description = projection.PendingSystemTransitionDescription;
            if (description == null ||
                description.Kind != GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind
                    .LevelUpUpgrade)
            {
                return null;
            }

            var prompt = RoundActionPresenter.CreateSystemTransitionPrompt(projection);
            if (prompt == null)
            {
                return null;
            }

            var controls = prompt.Options
                .Select(option => new DecisionWindowControlViewModel(
                    option.Label,
                    option.Command,
                    card: option.Card,
                    detail: option.Detail,
                    upgradeOption: option))
                .ToArray();

            // The user's 2026-09-07 directive, carried over from the strip with its own rule intact:
            // "when leveling. Show current hand (all 5 cards) above the offer." It is the SAME
            // per-hero derivation the ordinary tray uses - `DirectorHandsPresenter.BuildTray`,
            // filtered to the cards actually in hand - rather than a second reading of the
            // projection, and only a real seat gets one: an undefined viewer is the Director, who
            // already sees every hand in the drawer.
            // Milestone 8.58. THE LEVELLING HERO'S hand, whoever is looking at it.
            //
            // 8.46g read it from the VIEWER and therefore gave the Director none, on the reasoning that
            // an undefined viewer already sees every hand in the drawer. The live capture of a real
            // p.15 pause is what retired that: the window's own directive is "show current hand (all 5
            // cards) above the offer", the deciding seat is named in the title, and a Director screen
            // that drew the offer with the row above it missing left the panel half empty - the same
            // "waste of space" the user struck out everywhere else in this block. The hand shown is the
            // hand being chosen FOR, which is one hero either way.
            var handPreview = DirectorHandsPresenter.BuildTray(projection, description.HeroId, null)
                .Where(card => card.Group == TrayCardGroup.Available)
                .ToArray();

            return new DecisionWindowViewModel(
                projection.Version,
                DecisionWindowFamily.LevelUp,
                DecisionWindowState.Expanded,
                "level-up/" + description.ItemId.Value + "/" + description.HeroId.Value + "/" +
                    description.Level,
                Scoped("UPGRADE A CARD", HeroName(projection, description.HeroId), viewerHeroId),
                prompt.SectionLabel,
                prompt.PromptLabel,
                prompt.StatusLabel,
                controls: controls,
                handPreview: handPreview);
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

            // Milestone 8.80, the user from the 8.79 playtest: "Defense window does not show
            // attacking card anymroe". The card is read off the CHOICE, which is the one thing about
            // this attack the defending seat is actually handed: `SeatProjection` gives
            // `PendingAttackResolution` to the ACTING seat alone, so on every joined seat - and on
            // the host's own loopback seat, whose client is an ordinary network client - the frame
            // this used to read was null and the window drew the candidates with nothing above them.
            var attackCardName = CardName(projection, choice.SourceCardInstanceId);
            var contextCards = new List<DecisionWindowCardViewModel>();

            // The block is kept so the attack's own `action` stays local to it; the candidate loop
            // below names its own.
            {
                var face = CardFace(projection, choice.SourceCardInstanceId);
                if (face != null)
                {
                    var action = projection.Definitions
                        .Where(definition => definition.Reference.Equals(choice.SourceDefinitionReference))
                        .SelectMany(definition => definition.ActionDefinitions)
                        .FirstOrDefault(value => value.Id.Equals(choice.ActionDefinitionId));
                    // Milestone 8.58. The attack's own total, and its Range where p.13 classifies it
                    // ranged, as ONE chip - the mockup's `Attack 5 · Range 4` - because the two facts
                    // are one sentence about the card the player is being asked to answer. The total is
                    // the authority's composed value off the standing choice, never re-added here.
                    // Milestone 8.57g. The classification is the PRINTED one, then p.13's own overlay:
                    // Rowenna's Fabled Lance makes every Attack action of hers Ranged while it is
                    // unlocked, and her Attack faces print NonRanged - so the chip has to ask the same
                    // question the defence preview beside it asks. The offer's classification
                    // deliberately stays printed (ActionOffer equality guards authorization), which is
                    // why the overlay is applied by the drawing rather than read off the offer; the
                    // Range the chip names is then the printed icon where the card prints one and the
                    // Ultimate's declared printed Range where it does not.
                    var printedClassification = action == null ? AttackRangeKind.NonRanged : action.AttackRange;
                    var unlockedUltimates = GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        projection.Definitions, projection.Cards);
                    var classification = RangedAttackOverlayQuery.ClassifyFromUnlockedUltimates(
                        printedClassification, choice.AttackerId, unlockedUltimates);
                    var chip = "ATTACK " + choice.AttackTotal;
                    if (classification == AttackRangeKind.Ranged)
                    {
                        var range = action == null
                            ? null
                            : action.BaseValues
                                .Where(value => value.Channel == PrintedValueChannel.Range)
                                .Select(value => (int?)value.Amount)
                                .FirstOrDefault();
                        if (!range.HasValue || range.Value <= 0)
                        {
                            var declared = RangedAttackOverlayQuery.DeclaredPrintedRangeFromUnlockedUltimates(
                                choice.AttackerId, unlockedUltimates);
                            if (declared > 0)
                            {
                                range = declared;
                            }
                        }

                        if (range.HasValue)
                        {
                            chip += " · RANGE " + range.Value;
                        }
                    }

                    // Milestone 8.58, the user on the inline arithmetic: "Remove it and keep the old
                    // mouseover when mousing over the defense cards." The card and its one chip stay;
                    // what the total is MADE OF is the hover breakdown's to say, on the one layer that
                    // has said it since 8.11b.
                    contextCards.Add(new DecisionWindowCardViewModel(face, chip));
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
                    //
                    // Milestone 8.58, the user on the shipped window: "This is not showing the attack
                    // card, nor our defense breakdown popup". `HOLDS` is the AUTHORITY'S OWN VERDICT
                    // for this exact card against this exact attack - `PendingDefensePreview`'s
                    // outcome, which p.14's comparison, Block and every contribution already produced -
                    // and is never re-derived here from the two numbers.
                    valueChip: (offerPreview.DefenseTotal.HasValue
                        ? "DEFENSE " + offerPreview.DefenseTotal.Value
                        : offerPreview.BlockResult == DefenseBlockResult.Blocked
                            ? "BLOCK - CONDITION MET"
                            : "BLOCK - CONDITION NOT MET") +
                        (offerPreview.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                            ? " · HOLDS"
                            : string.Empty),
                    isPrimary: offerPreview.Outcome == AttackResolutionOutcome.SuccessfullyDefended,
                    shineSlot: action?.Origin == ActionDefinitionOrigin.PrintedPrimary
                        ? CardShineSlot.PrimaryRing
                        : CardShineSlot.SecondaryDefense,
                    // Milestone 8.58, the user: "Remove it and keep the old mouseover when mousing
                    // over the defense cards." The SAME breakdown the strip's own offer opened - built
                    // by the strip's own presenter off the same preview - so the window did not lose the
                    // popup when it took the question, and the two surfaces cannot disagree about a
                    // single contribution.
                    defenseBreakdown: RoundActionPresenter.CreateDefenseBreakdown(
                        projection.Version, preview, offerPreview)));
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
                    : "Discard a card for its printed defense, or take the hit.",
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
            var family = choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot
                ? DecisionWindowFamily.RuneSlots
                : FamilyOf(subject);
            var sourceCardId = SourceCardOf(projection, choice);
            var sourceDefinitionId = DefinitionIdOf(projection, sourceCardId);
            var previews = new HashSet<EffectValue>(
                projection.PendingEffectChoicePreview.Select(preview => preview.CandidateValue));
            // Milestone 8.46e. A piece-or-space question docks only when the board can answer ALL
            // of it: every valued candidate is one the board previews, where clicking the piece or
            // the space answers. Dodger's Necromastery asks which minion TYPE comes back from the
            // SUPPLY - candidates with no piece on the board to click - and the first Play Mode run
            // docked that question into a bar with no answers on it at all.
            var boardAnswered = family == DecisionWindowFamily.Direction ||
                (family == DecisionWindowFamily.PieceOrSpace &&
                    choice.Candidates
                        .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value)
                        .All(candidate => previews.Contains(candidate.Value)));

            var controls = new List<DecisionWindowControlViewModel>();
            DecisionWindowControlViewModel corner = null;
            BuildEffectControls(
                projection, choice, subject, sourceDefinitionId, previews, boardAnswered, controls,
                out var spellArms);

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
                // Milestone 8.58: the accept leads and the decline stands beside it, both in the one
                // control style, the leading one told apart by the mockup's gold border. They were both
                // "primary" at 8.46c because both were pewter banners; there are no banners here now,
                // and two gold borders would emphasise nothing.
                controls.Add(new DecisionWindowControlViewModel(
                    corner.Label, corner.Command, role: DecisionWindowControlRole.Answer));
                corner = null;
            }

            // Milestone 8.58. EVERY expanded window whose question comes from a card's text shows that
            // card's face - the card chooser included, where the card asking and the cards offered are
            // different cards and the strip could only ever draw the second set. A board-answered
            // question is the slim bar and shows none.
            var contextCards = new List<DecisionWindowCardViewModel>();
            if (!boardAnswered)
            {
                var face = CardFace(projection, sourceCardId);
                if (face != null)
                {
                    // No caption: the window's own title IS this card's name, and a second copy of it
                    // under the face would be the strip's `MODE CHOICE` gutter back again.
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
                // Milestone 9.30h. A choice whose options are spells is a SPELL CHOICE (approved
                // mockup A), and says what the card printed: cast one, or remove up to N.
                spellArms != SyntheticSpellbookCapabilities.SpellArmKind.None
                    ? SpellbookPresentation.SpellChoiceEyebrow
                    : EffectChoicePromptLabels.SectionLabel(subject),
                spellArms == SyntheticSpellbookCapabilities.SpellArmKind.Cast
                    ? SpellbookPresentation.CastQuestion
                    : spellArms == SyntheticSpellbookCapabilities.SpellArmKind.Remove
                    ? "Remove up to " + choice.MaximumSelections.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) +
                        " other spells from the spellbook faceup, then confirm."
                    : family == DecisionWindowFamily.RuneSlots
                    ? "Drag each rune onto one turn slot."
                    : family == DecisionWindowFamily.Direction
                    ? "Choose a direction"
                    // A board-answered question is a LINE on the slim bar; the bounds it would
                    // otherwise spell out are already in the count beside it.
                    : family == DecisionWindowFamily.PieceOrSpace
                        ? (subject == EffectChoiceSubject.Space
                            ? "Click a lit space."
                            : "Click a marked piece.")
                        : EffectChoicePromptLabels.StatusLine(choice, subject, projection),
                choice.MaximumSelections > 1
                    ? $"{choice.SelectedCandidates.Count} of {choice.MaximumSelections} chosen"
                    : null,
                contextCards,
                controls,
                corner,
                isMinimisable: family != DecisionWindowFamily.RuneSlots);
        }

        private static void BuildEffectControls(
            IMatchReadModel projection,
            PendingEffectChoice choice,
            EffectChoiceSubject subject,
            CardDefinitionId sourceDefinitionId,
            HashSet<EffectValue> previews,
            bool boardAnswered,
            List<DecisionWindowControlViewModel> controls,
            out SyntheticSpellbookCapabilities.SpellArmKind spellArms)
        {
            spellArms = SyntheticSpellbookCapabilities.SpellArmKind.None;
            var spellOptions = new Dictionary<DecisionWindowControlViewModel, EffectNodeId>();
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

                // Milestone 9.30h. An option that names a spell is drawn as that spell's FACE, the
                // definition read off the public effect graph and the caption off the public removed
                // pile (approved mockup A). The commands are the contract's own, exactly as for a
                // worded bullet; the face is only what the control looks like.
                var spell = SpellbookPresentation.OptionFace(
                    projection, sourceDefinitionId, candidate, invocation != null, out var armKind);
                if (spell != null)
                {
                    spellArms = armKind;
                }

                if (invocation == null)
                {
                    // A spell multi-pick that has reached its maximum keeps every face where it stands,
                    // inert, rather than reflowing the window under the pointer.
                    if (spell != null)
                    {
                        var inert = new DecisionWindowControlViewModel(
                            spell.Caption,
                            unavailableReason: ActionUnavailabilityLabels.Generic,
                            spell: spell);
                        controls.Add(inert);
                        spellOptions[inert] = candidate.OptionNodeId;
                    }

                    continue;
                }

                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    value => value.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(value.EffectChoice, invocation));
                var answer = new DecisionWindowControlViewModel(
                    spell?.Caption ??
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
                    isChosen: deselect != null,
                    runeOrdinal: choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot
                        ? ((candidate.Value.IntegerValue - 1) / 4) + 1 : 0,
                    turnSlotIndex: choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot
                        ? ((candidate.Value.IntegerValue - 1) % 4) + 1 : 0,
                    spell: spell);
                controls.Add(answer);
                if (spell != null)
                {
                    spellOptions[answer] = candidate.OptionNodeId;
                }
            }

            foreach (var unavailable in boardAnswered
                ? Enumerable.Empty<EffectChoiceCandidate>()
                : choice.UnavailableCandidates)
            {
                // Milestone 9.30h. A cast bullet whose spell is not in the book is its face GREYED,
                // captioned REMOVED (mockup A). A removal option for a spell already removed is not
                // drawn at all: Find Familiar offers "the spells still in his book" (the user,
                // 2026-10-01), and a spell cannot be removed twice.
                var spell = SpellbookPresentation.OptionFace(
                    projection, sourceDefinitionId, unavailable, false, out var armKind);
                if (spell != null)
                {
                    spellArms = armKind;
                    if (armKind == SyntheticSpellbookCapabilities.SpellArmKind.Remove)
                    {
                        continue;
                    }
                }

                var refused = new DecisionWindowControlViewModel(
                    spell?.Caption ?? CandidateLabel(
                        projection, choice, unavailable,
                        ordinals.TryGetValue(unavailable, out var ordinal) ? ordinal : 1,
                        sourceDefinitionId),
                    unavailableReason: ActionUnavailabilityLabels.Generic,
                    spell: spell,
                    runeOrdinal: choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot
                        ? ((unavailable.Value.IntegerValue - 1) / 4) + 1 : 0,
                    turnSlotIndex: choice.ValueVocabulary == EffectChoiceValueVocabulary.RuneTurnSlot
                        ? ((unavailable.Value.IntegerValue - 1) % 4) + 1 : 0);
                controls.Add(refused);
                if (spell != null)
                {
                    spellOptions[refused] = unavailable.OptionNodeId;
                }
            }

            // Milestone 9.30h. Spell faces stand in the order the card PRINTS its spells (mockup A:
            // Burning Hands, the greyed Fireball, Sunburst), not available-first: a removed spell keeps
            // its printed place, so the row reads like the card beside it.
            if (spellOptions.Count > 1)
            {
                var printed = SpellbookPresentation.PrintedOptionOrder(
                    projection, sourceDefinitionId, choice.NodeId);
                var first = controls.FindIndex(control => spellOptions.ContainsKey(control));
                var ordered = controls
                    .Where(control => spellOptions.ContainsKey(control))
                    .OrderBy(control =>
                    {
                        var index = printed.IndexOf(spellOptions[control]);
                        return index < 0 ? int.MaxValue : index;
                    })
                    .ToArray();
                controls.RemoveAll(control => spellOptions.ContainsKey(control));
                controls.InsertRange(first, ordered);
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
                    // Milestone 9.30h. A spell multi-pick is confirmed in the user's own word.
                    spellArms != SyntheticSpellbookCapabilities.SpellArmKind.None
                        ? SpellbookPresentation.ConfirmLabel
                        : EffectChoiceLabels.CompletionLabel,
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

        /// <summary>
        /// Declining rulebook p.11's Defense, named for what it costs - the mockup's quiet corner
        /// control, "Take the hit · no defense".
        /// </summary>
        public const string TakeTheHitLabel = "TAKE THE HIT · NO DEFENSE";

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

            return PrintedBulletLabel(ordinal);
        }

        /// <summary>
        /// Milestone 8.58. What a printed bullet is called when the content declares no words for it.
        ///
        /// <para><b>A seat never sees a node id.</b> <c>EffectChoiceLabels.CandidateLabel</c> answers
        /// <c>MODE #1 RELOAD-BORROW-ARM</c> - the engine's own vocabulary, and the exact string the user
        /// photographed on 2026-09-19 - which is a DIRECTOR diagnostic and stays one: the Director's
        /// surface and the headless command list still print it. In the window a missing label falls
        /// back to the bullet's own position on the card, beside the card face itself, so the worst case
        /// is "the first printed option" rather than an identifier out of the source. The content check
        /// <c>DecisionLabelCoverageTests</c> is what makes this unreachable for dealt content.</para>
        /// </summary>
        public static string PrintedBulletLabel(int ordinal) => "OPTION " + ordinal;

        /// <summary>
        /// Milestone 8.58. A printed `may` accepted in the card's own words - and, where the content
        /// declares none, named for what the step DOES rather than with the word `ACCEPT`, which the
        /// user struck out of this window by name ("two buttons whose labels NAME THE CONSEQUENCE ...
        /// never ACCEPT/DECLINE").
        /// </summary>
        public const string DoItLabel = "DO IT";

        /// <summary>The undeclared decline, named for the board rather than for the mechanism.</summary>
        public const string StopHereLabel = "STOP HERE";

        private static string AcceptLabelFor(
            CardDefinitionId sourceDefinitionId, PendingEffectChoice choice) =>
            DecisionLabelCatalog.TryGetLabel(sourceDefinitionId, choice.NodeId, out var declared)
                ? declared.Chosen.ToUpperInvariant()
                : DoItLabel;

        private static string DeclineLabelFor(
            EffectChoiceSubject subject, CardDefinitionId sourceDefinitionId, PendingEffectChoice choice) =>
            subject != EffectChoiceSubject.OptionalStep
                ? EffectChoiceLabels.DeclineLabel
                : DecisionLabelCatalog.TryGetLabel(sourceDefinitionId, choice.NodeId, out var declared) &&
                    declared.HasDeclined
                    ? declared.Declined.ToUpperInvariant()
                    : StopHereLabel;

        /// <summary>
        /// Milestone 8.58. The window's title is the CARD wherever a card's text is asking.
        ///
        /// <para>The user's words on the shipped window: "We said we will show the card". A title that
        /// read <c>DO IT?</c> or <c>CHOOSE ONE</c> over an empty box named neither the card nor the
        /// question; those two strings survive only where nothing names a card at all, which for dealt
        /// content is unreachable now that the resolving card is published (8.58's
        /// <see cref="IMatchReadModel.ResolvingSourceCardInstanceId"/>).</para>
        /// </summary>
        private static string TitleOf(
            EffectChoiceSubject subject, CardInstanceId sourceCardId, IMatchReadModel projection)
        {
            var name = CardName(projection, sourceCardId)?.ToUpperInvariant();
            if (name != null)
            {
                // One rule for every family a card's text raises: the card's own name, because the
                // card is drawn beside it and the eyebrow already says what kind of question it is.
                return subject == EffectChoiceSubject.OptionalStep ? name + "?" : name;
            }

            switch (subject)
            {
                case EffectChoiceSubject.OptionalStep:
                    return "DO IT?";
                case EffectChoiceSubject.Colour:
                    return "NAME A COLOUR";
                case EffectChoiceSubject.Direction:
                    return "WHICH WAY?";
                case EffectChoiceSubject.Number:
                    return "HOW FAR?";
                case EffectChoiceSubject.Card:
                    return "WHICH CARD?";
                case EffectChoiceSubject.Mode:
                    return "CHOOSE ONE";
                case EffectChoiceSubject.TextFace:
                    return PrintedTextFaceLabels.FallbackTitle;
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
                case EffectChoiceSubject.TextFace:
                    // Milestone 8.75b. Deliberately NOT a fifteenth family. The window's family decides
                    // layout - where the question opens, whether the board answers it, which element
                    // names a card-faced control - and "which of this card's two printed texts applies"
                    // wants exactly what a mode choice already draws: the card itself, with one control
                    // per printed arm beside it. What makes it its own question is the eyebrow
                    // (`EffectChoicePromptLabels.SectionLabel`) and the two controls' own labels, both of
                    // which come from the SUBJECT. A new member here would have been a second name for
                    // the same shape, and every switch over this enum would have had to learn it.
                    return DecisionWindowFamily.Mode;
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

        /// <summary>
        /// The card whose printed text is asking.
        ///
        /// <para><b>Milestone 8.58, and the defect the 2026-09-19 packaged playtest photographed.</b>
        /// This read used to be the running frame's own source card and nothing else - and a NETWORK
        /// seat's read model reports that frame as <c>null</c> on purpose (8.40f's privacy arm), so on
        /// every client in that playtest the source card was undefined: no face beside the question,
        /// and no card id to look a declared label up by, which is why Swift's Reload! - a card that
        /// HAS both its arms declared - drew <c>MODE #1 RELOAD-BORROW-ARM</c>. The projection now
        /// publishes WHICH card is resolving on its own
        /// (<see cref="IMatchReadModel.ResolvingSourceCardInstanceId"/>), which is public by
        /// construction, and this reads that wherever the frame is withheld.</para>
        /// </summary>
        private static CardInstanceId SourceCardOf(
            IMatchReadModel projection, PendingEffectChoice choice)
        {
            var resolution = projection.PendingSkillResolution;
            if (resolution != null && resolution.ResolutionId.Equals(choice.ResolutionId))
            {
                return resolution.SourceCardInstanceId;
            }

            return projection.ResolvingSourceCardInstanceId;
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

        /// <summary>
        /// Milestone 9.29. A hero's name without its epithet - "Sabina" for "Sabina the Gunslinger" -
        /// for the docked bar, whose second line is short by construction (8.58) and may have to name
        /// two teammates at once.
        /// </summary>
        private static string ShortHeroName(IMatchReadModel projection, HeroId heroId)
        {
            var name = HeroName(projection, heroId);
            var epithet = name.IndexOf(" the ", StringComparison.Ordinal);
            return epithet > 0 ? name.Substring(0, epithet) : name;
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

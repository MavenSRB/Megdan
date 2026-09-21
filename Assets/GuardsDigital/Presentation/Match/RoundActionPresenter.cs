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

    public sealed class RoundActionPresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly IRoundActionView view;
        // Milestone 8.35z4: NOT readonly. A networked client is composed before its seat is granted,
        // so this identity is bound a second time by `ConfiguredMatchSceneRuntime.BindHomeHero`
        // once the lobby admits the player. See `ISeatScopedPresenter` for what an unrepaired
        // identity does to every predicate below that asks "is this decision mine?".
        private HeroId homeHeroId;
        private readonly List<TokenInstanceId> clearSelection = new List<TokenInstanceId>();
        private IMatchReadModel lastProjection;
        private RoundActionViewModel lastViewModel;
        private IMatchReadModel clearModeProjection;
        private ActionDefinitionId clearModeActionDefinitionId;


        /// <summary>
        /// Milestone 8.30b. The Director-shaped constructor every existing fixture and journey uses,
        /// composing the Director's own client. A seat-scoped surface passes its client instead.
        /// </summary>
        public RoundActionPresenter(IMatchSession session, IRoundActionView view)
            : this(new DirectorMatchClient(session), view)
        {
        }

        public RoundActionPresenter(IMatchClient session, IRoundActionView view)
            : this(session, view, default)
        {
        }

        /// <summary>
        /// A seat-scoped composition names the hero it is drawn for, on the terms
        /// <see cref="DirectorHandsPresenter"/> already takes one: every other fact this presenter
        /// reads is scoped to the viewer by the projection itself, and the viewer's own commitment is
        /// the one wait that is public and therefore has to be named. The Director passes the
        /// undefined hero and answers for everybody.
        /// </summary>
        public RoundActionPresenter(IMatchClient session, IRoundActionView view, HeroId homeHeroId)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.homeHeroId = homeHeroId;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => homeHeroId;

        /// <summary>
        /// Milestone 8.35z4. Binds this panel's viewer identity once the interactive lobby admits the
        /// seat, exactly as <see cref="DirectorHandsPresenter.SetHomeHero"/> has always done for the
        /// tray. Until this existed the field was <c>readonly</c> and a networked client's round
        /// panel kept the undefined hero for the whole session, reading every seat's turn release,
        /// every commitment wait and every END TURN as its own.
        /// </summary>
        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            homeHeroId = heroId;
            // The cached model was composed against the OLD identity, and the projection reference
            // does not have to change for the binding to - so without this the next Refresh could
            // serve back the very view model this binding exists to correct.
            lastProjection = null;
            Refresh();
            return true;
        }

        public event Action<IMatchReadModel> SessionProjectionChanged;
        public event Action<FastTravelOfferViewModel> FastTravelSelectionRequested;

        /// <summary>
        /// Milestone 6.3a: the Clear selection as it now stands, or <c>null</c> once it is closed.
        /// The board listens so the chosen pieces keep their cue; nothing authoritative rides on it.
        /// </summary>
        public event Action<ClearSelectionViewModel> ClearSelectionChanged;

        public void Refresh()
        {
            // Milestone 8.35z4: an END TURN press that ran out of things it could submit while the
            // client was waiting on the host picks up here, on the projection that answered it.
            ResumeEndTurnComposition();
            view.Render(GetOrCreateViewModel(session.Projection));
        }

        /// <summary>
        /// Milestone 8.35f. Steps this seat's own turn back by one accepted command.
        /// </summary>
        /// <remarks>
        /// It takes no invocation and composes no command, because there is none: the control's
        /// whole content is "the seat that owns this turn asks to step back". The presenter refuses
        /// to ask when the view it is currently rendering says the control does not stand, so a
        /// stale click cannot become a question the host has to refuse; the host re-decides anyway.
        /// </remarks>
        public bool SubmitSeatUndo()
        {
            if (!GetOrCreateViewModel(session.Projection).SeatUndoAvailable)
            {
                return false;
            }

            var result = session.SubmitUndo();
            if (result.Accepted)
            {
                Refresh();
                SessionProjectionChanged?.Invoke(session.Projection);
            }

            return result.Accepted;
        }

        public bool Submit(TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);

            // Milestone 6.5ac: the unavailable list joins this legality decision rather than sitting
            // outside it. Its own entries carry no command, so nothing a view holds can reach here
            // through them; what this refuses is a command left over from an older model naming an
            // option the authority has since refused - the presenter's half of the same atomicity the
            // session enforces on a stale or forged select.
            if (current.UnavailableEffectChoices.Any(option =>
                    invocation.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    invocation.Command.EffectChoice.Candidate.Equals(option.Candidate)))
            {
                return false;
            }

            // Milestone 6.5af: the same guard for a greyed action. Its own entry carries no command,
            // so nothing a view holds reaches here through it; what this refuses is a start left over
            // from an older model naming an action the authority has since judged unstartable.
            if (current.UnavailableActions.Any(action =>
                    invocation.Command.Kind == DirectorCommandKind.StartAction &&
                    invocation.Command.ActionDefinitionId.Equals(action.ActionDefinitionId)))
            {
                return false;
            }

            var legal = current.Actions
                .Concat(current.InitiativeChoices)
                .Concat(current.DefenseChoices)
                .Concat(current.EffectChoices)
                // Milestone 6.5c: a destination carries no control, but it is still a projected
                // invocation this presenter owns, so the board's click submits through here.
                .Concat(current.EffectChoiceDestinations)
                .Concat(current.ResolveWithoutRespawn != null
                    ? new[] { current.ResolveWithoutRespawn }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.EndTurnInvocation != null
                    ? new[] { current.EndTurnInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.FinishMovementInvocation != null
                    ? new[] { current.FinishMovementInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Concat(current.ContinueEndOfRoundInvocation != null
                    ? new[] { current.ContinueEndOfRoundInvocation }
                    : Array.Empty<TypedInvocationViewModel>())
                .Any(value => value.Command.Equals(invocation.Command)) ||
                current.EffectChoiceTargets.Any(value => value.Command.Equals(invocation.Command)) ||
                current.HeroRespawnOptions.Any(value => value.Command.Equals(invocation.Command)) ||
                // Milestone 7.8a: a paused transition's options join the same legality decision as
                // everything else this presenter projects. An option from an older prompt - one the
                // pause has since moved past, or one a restore replaced - is refused here and never
                // reaches the authority, which would refuse it too and for the same reason.
                current.SystemTransitionPrompt?.Options
                    .Any(value => value.Command.Equals(invocation.Command)) == true ||
                // Milestone 8.21: the standing removal-prevention decision's own controls. Every one
                // of them is a command the pause minted and this presenter projected, so they belong
                // in the same legality decision as every other control here - and until they were in
                // it, this method refused each of them, which drew a player a prompt whose faces did
                // nothing when clicked. It was invisible to the projection and presenter tiers alike,
                // because both compose the controls correctly; only a pointer on the live panel finds
                // a control that forwards a command the presenter then declines to execute.
                (current.RemovalPreventionPrompt != null &&
                    (current.RemovalPreventionPrompt.Payments
                            .Any(value => value.Command.Equals(invocation.Command)) ||
                        current.RemovalPreventionPrompt.Decline.Command.Equals(
                            invocation.Command))) ||
                // Milestone 6.3a: the open Clear's confirm joins the same legality decision, because
                // this presenter refuses anything it did not project and a Clear confirm is projected
                // like every other control here. What it refuses in practice is a confirm left over
                // from a selection the current projection no longer offers.
                current.ClearSelection != null &&
                    current.ClearSelection.Confirm.Command.Equals(invocation.Command);
            if (!legal)
            {
                return false;
            }

            // Milestone 8.35v (the THIRD amendment's REVISED SEMANTICS): one END TURN press ends
            // the turn regardless of how many accepted commands that takes. `invocation` is END
            // TURN's own exactly when it is the same object this presenter's own view model just
            // drew - never a coincidental match on some other control that happens to submit the
            // same command kind - so a movement Stop, an effect choice's own stop, or a Hold
            // submitted through here is the completion half of the press; the release below is the
            // second half, performed in the same press rather than left for a separate one. Release
            // itself needs no further chaining - reaching this branch already IS the whole press.
            var isEndTurnPress = ReferenceEquals(invocation, current.EndTurnInvocation);
            // Milestone 8.35z4: read BEFORE the submission, because a network client hands back a
            // projection that has not yet been decided and may already be stripped of the actor.
            var pressActorId = isEndTurnPress ? PressActorOf(session.Projection) : default;
            if (!Execute(invocation.Command))
            {
                return false;
            }

            if (isEndTurnPress && invocation.Command.Kind != DirectorCommandKind.ReleaseTurn)
            {
                ReleaseTheTurnIfItNowStandsReleasable();
                // The same asynchronous-client fault this press path has always had: over the wire
                // the release above finds no standing contract yet, because the completion it
                // follows has only been SENT. Arm the continuation so the release lands on the
                // projection that answers it rather than waiting for a second press.
                ArmOrClearEndTurnContinuation(pressActorId, isFreshPress: true);
            }

            return true;
        }

        /// <summary>
        /// Milestone 8.35v. The end-turn press's own composition, reachable from every state the
        /// omnipresent control draws enabled: a direct completion via <see cref="Execute"/> below;
        /// the seat's one open, uncommitted Attack-target selection cancelled by its own bounded
        /// rewind (milestone 8.35f) first; or an open Clear selection (milestone 6.3a) cancelled by
        /// <see cref="CancelClear"/> first - purely a presentation reset, since nothing is submitted
        /// to the authority until Clear is confirmed, so there is nothing to rewind. Clear is
        /// checked first of the three: it is presentation-only and the authority has not paused
        /// underneath it, so <see cref="CreateEndTurnInvocation"/> can already project an ordinary
        /// completion (p.12's Hold, most commonly) alongside it, and submitting that FIRST would
        /// leave a confirmed Hold sitting beside a still-open, now-orphaned board selection - the
        /// discard has to land before the completion it would otherwise race. All three paths
        /// converge on the same one-press rule: keep composing whatever the projection legally
        /// offers next until the turn is released, disabled would refuse it, or nothing more is
        /// offered - never a fabricated stop the rules do not.
        ///
        /// <para>Bounded to the ONE actor this press is for. A Director client "answers for
        /// everybody" - unlike a seat, which sees only its own hero - so the instant this press's
        /// own release lands, the very next actor's own untouched card can already project its OWN
        /// Hold; composing past that boundary would silently spend a second player's turn inside a
        /// press that was only ever about the first. `pressActorId` is read once, before any
        /// submission, and every loop iteration verifies the state still concerns that same hero -
        /// either still resolving it, or still awaiting ITS release - before composing anything
        /// else. `Submit`'s own release-chain is not reused here for exactly this reason: it cannot
        /// tell "this call's own actor" from "whoever is offered next", which is what let 8.35v's
        /// first draft submit a second player's Hold inside a press the seeded suite proved was
        /// only ever about the first.</para>
        /// </summary>
        public bool SubmitEndTurn()
        {
            var pressActorId = PressActorOf(session.Projection);
            var progressed = ComposeEndTurn(pressActorId);
            ArmOrClearEndTurnContinuation(pressActorId, isFreshPress: true);
            return progressed;
        }

        /// <summary>
        /// The actor one END TURN press is about: the hero currently acting, or - once the turn's
        /// resolution has finished - the hero its release is standing on.
        /// </summary>
        private static HeroId PressActorOf(IMatchReadModel projection) =>
            projection?.SelectedActor.IsDefined == true
                ? projection.SelectedActor.ActorId
                : projection?.PendingTurnRelease?.ActorId ?? default;

        private bool ComposeEndTurn(HeroId pressActorId)
        {
            var progressed = false;
            for (var guard = 0; guard < MaxEndTurnComposedSteps; guard++)
            {
                var projection = session.Projection;
                var stillThisActor = pressActorId.IsDefined &&
                    (projection.SelectedActor.ActorId.Equals(pressActorId) ||
                        (projection.PendingTurnRelease?.ActorId.Equals(pressActorId) ?? false));
                if (!stillThisActor)
                {
                    return progressed;
                }

                var current = GetOrCreateViewModel(projection);
                if (current.ClearSelection != null)
                {
                    if (!CancelClear())
                    {
                        return progressed;
                    }

                    progressed = true;
                    continue;
                }

                if (current.EndTurnInvocation != null)
                {
                    if (!Execute(current.EndTurnInvocation.Command))
                    {
                        return progressed;
                    }

                    progressed = true;
                    continue;
                }

                if (!current.EndTurnCancelsOpenAttack)
                {
                    return progressed;
                }

                if (!SubmitSeatUndo())
                {
                    return progressed;
                }

                progressed = true;
            }

            // Never spin: a state that keeps offering a cancel without ever reaching a direct
            // completion is a defect this composition should surface loudly rather than loop on.
            throw new InvalidOperationException(
                "End Turn's cancel-and-release composition did not reach a legal completion " +
                "within its bound.");
        }

        /// <summary>The bound in <see cref="SubmitEndTurn"/>: one rewind past the deepest observed
        /// case (an open Attack target choice) plus the completion and release it composes into,
        /// with headroom for a repeat-ability's own extra cycle.</summary>
        private const int MaxEndTurnComposedSteps = 8;

        /// <summary>
        /// Milestone 8.35z4, and the SECOND defect the first live internet playtest found: "first end
        /// turn clears the card, second ends the turn".
        ///
        /// <para><b>What 8.35v's one-press rule assumed, and why only the network broke it.</b>
        /// <see cref="ComposeEndTurn"/> re-reads <c>session.Projection</c> after every step and keeps
        /// composing until the turn is released. That is exact against a SYNCHRONOUS client - the
        /// Director's and a local seat's - where a submission has already been decided by the time
        /// <see cref="Execute"/> returns. Over the wire it is not: <c>NetworkMatchClient.Submit</c>
        /// returns as soon as the command is SENT, handing back the pre-command projection, and the
        /// session then refuses every further submission until the host answers. So the second step
        /// of the composition - the release after a Hold, a movement Stop, an effect Decline - was
        /// refused, the loop ended having spent exactly one command, and the player had to press
        /// again. Every existing one-press case passes because every one of them drives a
        /// synchronous client; nothing in the suite ever composed over the client the shipped build
        /// actually uses.</para>
        ///
        /// <para><b>The fix, and why it lives here.</b> A press that could not finish because the
        /// client is mid-round-trip is not a finished press - it is a press with more to compose as
        /// soon as there is something to compose against. The continuation is remembered here, in
        /// the one place that knows what END TURN means, and resumed from <see cref="Refresh"/>,
        /// which every projection push already calls. <c>NetworkMatchClient</c> carries a
        /// hand-written continuation of exactly this shape for the effect select-then-confirm pair
        /// (<c>PendingEffectAutoConfirm</c>); that one was built before 8.35v's composition and
        /// 8.35g's release chain existed, and neither got an equivalent until now.</para>
        ///
        /// <para><b>What keeps it from spending somebody else's turn.</b> The continuation is bound
        /// to the ONE actor the press was for and is dropped the moment the state stops concerning
        /// that hero - the identical bound <see cref="ComposeEndTurn"/> already enforces inside a
        /// single press, and for the identical reason 8.35v records: a Director client answers for
        /// everybody, so composing past the actor boundary would silently end a second player's
        /// turn. It is also dropped after <see cref="MaxEndTurnResumes"/> resumptions, so a state
        /// that never becomes composable cannot leave a press armed for the rest of the match.</para>
        /// </summary>
        private HeroId composingEndTurnFor;

        private int composingEndTurnResumes;

        /// <summary>
        /// The bound on how many projection pushes one press may be resumed across. Generous enough
        /// for the deepest composition (rewind, completion, release) to survive a round trip each,
        /// and finite so a press can never become permanent.
        /// </summary>
        private const int MaxEndTurnResumes = 12;

        /// <summary>
        /// Remembers an END TURN press that has more to compose, or forgets one that is finished.
        ///
        /// <para>The signal that a press is unfinished rather than complete is the surface's own
        /// input gate: a client waiting on the host reports <c>IsInputEnabled == false</c>, which is
        /// precisely "this press could not continue for a reason that will pass". A synchronous
        /// client never reports it, so a local press - which always finishes inside its own loop -
        /// never arms a continuation at all.</para>
        /// </summary>
        private void ArmOrClearEndTurnContinuation(HeroId pressActorId, bool isFreshPress)
        {
            // Milestone 8.35z4. The budget belongs to the PRESS, not to each resumption of it.
            // Resetting it unconditionally here made MaxEndTurnResumes unreachable: every resume
            // re-arms through this method, so the counter returned to zero before it could ever be
            // compared, and a press whose submission kept being refused resumed forever. The
            // six-client rehearsal found it - one seat spent 17,486 refused submissions at a single
            // authoritative version while the match stood still. No offline probe could reach it,
            // because against an in-memory wire the composition always completed within two frames
            // and disarmed at the actor boundary long before the bound could matter.
            if (isFreshPress)
            {
                composingEndTurnResumes = 0;
            }

            var projection = session.Projection;

            // Deliberately NOT conditioned on whether this press submitted anything. An impatient
            // second press, landing while the first one's command is still in flight, submits
            // nothing and must leave the first press's continuation ARMED - clearing it there would
            // punish the player for pressing twice by making the turn need a third press, which is
            // the very failure this whole mechanism exists to remove. What decides is the state: a
            // disabled input gate means the press has somewhere left to go once the host answers.
            composingEndTurnFor =
                pressActorId.IsDefined && projection != null &&
                !projection.IsInputEnabled &&
                StillConcerns(projection, pressActorId)
                    ? pressActorId
                    : default;
        }

        /// <summary>
        /// Continues an armed press once the host's answer has landed. Called from
        /// <see cref="Refresh"/>, which every projection push already reaches.
        /// </summary>
        private void ResumeEndTurnComposition()
        {
            var actor = composingEndTurnFor;
            if (!actor.IsDefined)
            {
                return;
            }

            var projection = session.Projection;
            if (projection == null || !projection.IsInputEnabled)
            {
                // Still waiting on the host. Nothing has changed that this press can act on.
                return;
            }

            if (++composingEndTurnResumes > MaxEndTurnResumes ||
                !StillConcerns(projection, actor))
            {
                composingEndTurnFor = default;
                return;
            }

            // Cleared BEFORE composing, so the refreshes this composition itself provokes find no
            // armed press and cannot recurse into it. The step below re-arms if there is still more.
            composingEndTurnFor = default;
            ComposeEndTurn(actor);
            ArmOrClearEndTurnContinuation(actor, isFreshPress: false);
        }

        /// <summary>
        /// Whether the state still concerns the hero a press was for - still resolving that hero's
        /// turn, or still awaiting ITS release. The press's own boundary, asked in one place.
        /// </summary>
        private static bool StillConcerns(IMatchReadModel projection, HeroId actorId) =>
            projection.SelectedActor.ActorId.Equals(actorId) ||
            (projection.PendingTurnRelease?.ActorId.Equals(actorId) ?? false);

        private void ReleaseTheTurnIfItNowStandsReleasable()
        {
            var projection = session.Projection;
            if (projection?.DecisionContract != null &&
                projection.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
            {
                Execute(DirectorCommand.ReleaseTurn());
            }
        }

        public bool SubmitAgreement(TeamAgreementChoiceViewModel choice)
        {
            if (choice == null)
                return false;
            var current = GetOrCreateViewModel(session.Projection);
            if (current.TeamAgreement?.Choices.Any(value =>
                    value.Command.Equals(choice.Command)) != true)
                return false;

            var result = session.SubmitAgreement(choice.Command);
            if (result.Accepted)
                SessionProjectionChanged?.Invoke(result.Projection);
            else
                Refresh();
            return result.Accepted;
        }

        /// <summary>
        /// Milestone 8.11a, at the user's request. Answers a Hero Respawn by clicking the space rather
        /// than a control naming it. Declining to respawn has no board surface, so it stays a control.
        /// </summary>
        public bool SubmitRespawnAtDestination(HexCoordinate destination)
        {
            var current = GetOrCreateViewModel(session.Projection);
            var option = current?.HeroRespawnOptions
                .SingleOrDefault(value => value.Destination.Equals(destination));
            return option != null && Execute(option.Command);
        }

        /// <summary>
        /// Forwards one named target control. Selecting a target of an exactly-one choice finishes it,
        /// so the confirm the authority offers straight afterwards is forwarded with it.
        /// </summary>
        public bool SubmitTarget(EffectChoiceTargetViewModel target)
        {
            if (target == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            return current.EffectChoiceTargets.Any(value => value.Command.Equals(target.Command)) &&
                Execute(target.Command);
        }

        private bool Execute(DirectorCommand command)
        {
            var result = session.Submit(command);
            if (!result.Accepted)
            {
                return false;
            }

            SessionProjectionChanged?.Invoke(result.Projection);
            if (command.Kind != DirectorCommandKind.AnswerEffectChoice)
            {
                return true;
            }

            var confirm = EffectChoiceCompletion.FindImmediateConfirm(
                result.Projection, command.EffectChoice);
            if (confirm == null)
            {
                return true;
            }

            var confirmed = session.Submit(confirm);
            if (confirmed.Accepted)
            {
                SessionProjectionChanged?.Invoke(confirmed.Projection);
            }

            return true;
        }

        /// <summary>
        /// Milestone 6.3a: opens the Clear selection for one projected offer. Nothing is submitted and
        /// no authoritative state moves - the card stays unresolved and the action choice stands - so
        /// this returns to exactly where it started if the player cancels.
        /// </summary>
        public bool SelectClear(ClearOfferViewModel offer)
        {
            if (offer == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legal = current.ClearOffers.SingleOrDefault(value =>
                value.ReplacedAttackActionDefinitionId.Equals(
                    offer.ReplacedAttackActionDefinitionId) &&
                value.SourceCardInstanceId.Equals(offer.SourceCardInstanceId) &&
                value.ActorId.Equals(offer.ActorId));
            // Milestone 8.8b: a Clear the authority stamped unavailable opens nothing. Refusing the
            // mode is what keeps the surface from standing in a selection whose only confirm the
            // authority would reject - the same reason a greyed action carries no start.
            if (legal == null || !legal.IsAvailable)
            {
                return false;
            }

            clearModeProjection = session.Projection;
            clearModeActionDefinitionId = legal.ReplacedAttackActionDefinitionId;
            clearSelection.Clear();
            return AfterClearSelectionChanged();
        }

        /// <summary>
        /// Adds or removes one offered token from the subset. The presenter tracks nothing else about
        /// it: which tokens may be chosen, and which command each subset means, are both projected.
        /// </summary>
        public bool ToggleClearToken(ClearTokenCandidateViewModel candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            if (current.ClearSelection == null ||
                !current.ClearSelection.Candidates.Any(value =>
                    value.TokenId.Equals(candidate.TokenId)))
            {
                return false;
            }

            if (!clearSelection.Remove(candidate.TokenId))
            {
                clearSelection.Add(candidate.TokenId);
            }

            return AfterClearSelectionChanged();
        }

        /// <summary>
        /// Toggles the offered token standing on this space, for the board click. Milestone 8.15a, at
        /// the user's direction: a Clear candidate is a piece on the map, so the map is where it is
        /// chosen - the same board-first rule every other standing piece already follows. A space
        /// carrying no offered token toggles nothing, so the click falls through to whatever else the
        /// board may mean by it.
        /// </summary>
        public bool ToggleClearTokenAtSpace(HexCoordinate space)
        {
            var current = GetOrCreateViewModel(session.Projection);
            var candidate = current.ClearSelection?.Candidates
                .FirstOrDefault(value => value.Space.Equals(space));
            return candidate != null && ToggleClearToken(candidate);
        }

        /// <summary>Forwards the projected invocation for exactly the chosen subset, unchanged.</summary>
        public bool ConfirmClear()
        {
            var current = GetOrCreateViewModel(session.Projection);
            return current.ClearSelection != null && Submit(current.ClearSelection.Confirm);
        }

        /// <summary>
        /// Closes the selection without submitting anything. The card is left unresolved and the
        /// authoritative action choice is the one it always was.
        /// </summary>
        public bool CancelClear()
        {
            if (GetOrCreateViewModel(session.Projection).ClearSelection == null)
            {
                return false;
            }

            clearModeProjection = null;
            clearModeActionDefinitionId = default;
            clearSelection.Clear();
            return AfterClearSelectionChanged();
        }

        private bool AfterClearSelectionChanged()
        {
            lastProjection = null;
            ClearSelectionChanged?.Invoke(
                GetOrCreateViewModel(session.Projection).ClearSelection);
            return true;
        }

        public bool SelectFastTravel(FastTravelOfferViewModel selection)
        {
            if (selection == null)
            {
                return false;
            }

            var current = GetOrCreateViewModel(session.Projection);
            var legalSelection = current.FastTravelOffers.SingleOrDefault(option =>
                option.ReplacedMovementActionDefinitionId.Equals(
                    selection.ReplacedMovementActionDefinitionId) &&
                option.LegalDestinationCount == selection.LegalDestinationCount);
            if (legalSelection == null)
            {
                return false;
            }

            FastTravelSelectionRequested?.Invoke(legalSelection);
            return true;
        }

        /// <summary>
        /// The Director-shaped overload, which keeps its own manual end-of-round control. Milestone
        /// 8.35d added the parameter below rather than changing this signature, so every existing
        /// caller still describes the surface it always described.
        /// </summary>
        public static RoundActionViewModel CreateViewModel(IMatchReadModel projection) =>
            CreateViewModel(projection, default, Array.Empty<TokenInstanceId>(), true, default);

        public static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection, bool offersRoundContinuation) =>
            CreateViewModel(
                projection, default, Array.Empty<TokenInstanceId>(), offersRoundContinuation, default);

        /// <summary>
        /// Milestone 8.35v. The seat-shaped overload: every fact below this presenter's
        /// per-turn-actor gate now needs the exact viewer <see cref="IMatchReadModel.SelectedActor"/>
        /// is compared against - <see cref="IsViewerTheActingHero"/>'s own remarks name the trap a
        /// caller passing the Director's default sentinel falls into against a seat-scoped
        /// projection: every seat's own <c>SelectedActor</c> and <c>IsTurnActive</c> are unscoped
        /// public facts, so only the homeHeroId comparison tells "the acting hero" apart from "a
        /// seat that merely owns some standing decision the acting hero's turn produced" or a
        /// plain spectator. Test and tooling code composing a seat's own view without a full
        /// <see cref="RoundActionPresenter"/> instance should call this rather than the Director-
        /// shaped overload above.
        /// </summary>
        public static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection, HeroId homeHeroId) =>
            CreateViewModel(
                projection, default, Array.Empty<TokenInstanceId>(), true, homeHeroId);

        /// <summary>
        /// Whether the match is waiting on a decision inside this turn, asked in one place.
        ///
        /// A paused Skill frame counts here for the same reason paused Movement and Attack do: Core
        /// refuses a second action while one is in progress, so an offer shown during the pause is a
        /// control that can only produce a rejection.
        ///
        /// Milestone 7.9c, finding F-PRES-01. Two lists asked this, and they had drifted by one entry:
        /// the End Turn derivation's copy omitted <c>PendingHeroRespawnChoice</c>, so a defeated hero's
        /// mandatory respawn answer was the one decision beside which the HUD still drew END TURN - a
        /// control the turn authority refuses with <c>HeroRespawnChoicePending</c> every time. Adding the
        /// missing entry to the second list would have left the next pending kind free to drift the same
        /// way, so there is now one list and no second place to forget.
        ///
        /// <para>Milestone 8.20d separates the two things that list had always held together, and
        /// reads nothing of its own so neither half can be bypassed. A <b>question</b> is a family the
        /// match is waiting for an answer to, and every one of them is named by
        /// <see cref="FormatStatus"/>; <c>StatusDerivationConformanceTests</c> compares the two by
        /// what the compiled code reads, so a family added to the question list without a status
        /// branch fails there rather than reaching a player as "choose an action or End Turn". A
        /// <b>frame</b> is a resolution in progress, which asks nothing itself - its question, when it
        /// has one, is one of the families above it.</para>
        /// </summary>
        private static bool HasPendingDecision(IMatchReadModel projection) =>
            IsAwaitingAnAnsweredDecision(projection) || IsInsideAnUnfinishedResolution(projection);

        /// <summary>
        /// Every pending family the match is waiting for a player's answer to. Milestone 8.20d: the
        /// set <see cref="FormatStatus"/> must name, one entry per family, checked as compiled reads
        /// rather than as a written-down list a later milestone could forget to extend.
        /// </summary>
        private static bool IsAwaitingAnAnsweredDecision(IMatchReadModel projection) =>
            projection.PendingInitiativeChoice != null ||
            projection.PendingDefenseChoice != null ||
            projection.PendingEffectChoice != null ||
            projection.PendingHeroRespawnChoice != null ||
            projection.PendingAttackTargetChoice != null ||
            projection.PendingMovementChoice != null ||
            projection.PendingDisplacedMinionCorrection != null ||
            projection.PendingRemovalPrevention != null;

        /// <summary>
        /// A resolution that has started and not finished. It suppresses the offers a second action
        /// would produce, exactly as a question does - Core refuses a second action while one is in
        /// progress - but it is not itself something a player answers, so it carries no status words
        /// of its own.
        /// </summary>
        private static bool IsInsideAnUnfinishedResolution(IMatchReadModel projection) =>
            projection.HasPendingMovementResolution ||
            projection.HasPendingAttackResolution ||
            projection.HasPendingSkillResolution;

        private static RoundActionViewModel CreateViewModel(
            IMatchReadModel projection,
            ActionDefinitionId openClearActionDefinitionId,
            IReadOnlyList<TokenInstanceId> selectedClearTokenIds,
            bool offersRoundContinuation,
            HeroId homeHeroId)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var hasPendingResolution = HasPendingDecision(projection);
            var offered = projection.IsTurnActive && !hasPendingResolution
                ? projection.AvailableTurnActions
                : Array.Empty<ActionOffer>();
            var actions = offered
                .Where(offer => offer.IsAvailable)
                .Select(offer => new TypedInvocationViewModel(
                    FormatActionLabel(projection, offer),
                    DirectorCommand.StartAction(
                        offer.SourceCardInstanceId, offer.ActionDefinitionId)))
                .ToArray();

            // Milestone 6.5af: an offer the authority judged unstartable is drawn greyed rather than
            // dropped, keeping its label and its place. Presentation decides nothing here - which
            // actions those are is projected, exactly as which options are refused already is.
            var unavailableActions = offered
                .Where(offer => !offer.IsAvailable)
                .Select(offer => new UnavailableActionViewModel(
                    FormatActionLabel(projection, offer),
                    offer.ActionDefinitionId))
                .ToArray();
            var initiativeChoices = projection.PendingInitiativeChoice == null ||
                projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative
                ? Array.Empty<TypedInvocationViewModel>()
                : projection.PendingInitiativeChoice.CandidateCardInstanceIds
                    .Select(cardId => new TypedInvocationViewModel(
                        $"ACT WITH {GetCardDisplayName(projection, cardId)}",
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Initiative,
                            command => command.Kind == DirectorCommandKind.ChooseTiedActor &&
                                command.TeamSide == projection.PendingInitiativeChoice.OwningSide &&
                                command.CardInstanceId.Equals(cardId))))
                    .ToArray();
            var defenseChoices = CreateDefenseChoices(projection);
            var fastTravelOffers = hasPendingResolution
                ? Array.Empty<FastTravelOfferViewModel>()
                : projection.AvailableFastTravelOptions
                    .Select(option => new FastTravelOfferViewModel(
                        "FAST TRAVEL",
                        option.ReplacedMovementActionDefinitionId,
                        option.LegalDestinations.Count))
                    .ToArray();
            // Milestone 6.3a: a Clear is offered wherever the projection carries one, and the open
            // selection is drawn from that same option, so a control and the command behind it can
            // never come from different readings of the position.
            var clearOptions = hasPendingResolution || !projection.IsTurnActive
                ? Array.Empty<ClearOptionProjection>()
                : projection.AvailableClearOptions.ToArray();
            var clearOffers = clearOptions
                .Select(option => new ClearOfferViewModel(
                    ClearLabels.OfferLabel,
                    option.ReplacedAttackActionDefinitionId,
                    option.ReplacementOffer.SourceCardInstanceId,
                    option.ReplacementOffer.ActorId,
                    option.AdjacentTokens.Count,
                    // Milestone 8.8b: the authority's own stamp, never a count made here.
                    option.ReplacementOffer.IsAvailable))
                .ToArray();
            var printedActionRegions = CreatePrintedActionRegions(
                projection, offered, actions, fastTravelOffers, clearOffers);
            var clearSelection = CreateClearSelection(
                clearOptions, openClearActionDefinitionId, selectedClearTokenIds);
            CreateEffectChoices(
                projection,
                out var effectChoiceControls,
                out var effectChoiceTargets,
                out var effectChoiceDestinations,
                out var unavailableEffectChoices);
            var respawn = projection.PendingHeroRespawnChoice;
            var respawnOptions = respawn == null
                ? Array.Empty<HeroRespawnOptionViewModel>()
                : respawn.Destinations.Select((destination, index) =>
                    new HeroRespawnOptionViewModel(
                        destination.IsFallback
                            ? $"RESPAWN NEAR SPAWN #{index + 1}"
                            : $"RESPAWN AT SPAWN #{index + 1}",
                        destination.Coordinate,
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.HeroRespawn,
                            command => command.Kind == DirectorCommandKind.RespawnHero &&
                                command.HeroId.Equals(respawn.RespawnInvocations[index].HeroId) &&
                                command.CardInstanceId.Equals(
                                    respawn.RespawnInvocations[index].CardInstanceId) &&
                                command.Coordinate.Equals(destination.Coordinate))))
                    .ToArray();

            var status = FormatStatus(projection, homeHeroId, out var statusDetail);
            var endTurnInvocation = CreateEndTurnInvocation(projection);
            var endTurnCancelsOpenAttack = ResolveEndTurnCancelsOpenAttack(
                projection, homeHeroId, endTurnInvocation);
            var endTurnUnavailableReason = endTurnInvocation != null || endTurnCancelsOpenAttack
                ? null
                : ResolveEndTurnUnavailableReason(projection, homeHeroId);
            var systemTransitionPrompt = CreateSystemTransitionPrompt(projection);
            // User directive (2026-09-07): "when leveling. Show current hand (all 5 cards) above
            // the offer." Gated strictly to the LEVEL-UP prompt kind, not to "any card-valued
            // system transition" - a future card-valued transition kind must opt in deliberately
            // rather than inherit this by coincidence. Sourced from the SAME per-hero hand
            // derivation the ordinary tray uses (DirectorHandsPresenter.BuildTray), filtered to
            // TrayCardGroup.Available (Location == Hand) - never a second, invented reading of the
            // projection. AUTHORIZATION FILTER != PROJECTION FILTER: only a real seat
            // (homeHeroId.IsDefined) gets a preview; an undefined viewer is the Director, who
            // already sees every hand in the drawer and gets nothing extra here. If the seat holds
            // fewer than five (cards played or discarded mid-flow), exactly that many show - the
            // printed truth, never a padded five.
            var levelUpHandPreview = homeHeroId.IsDefined &&
                systemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade
                ? DirectorHandsPresenter.BuildTray(projection, homeHeroId, null)
                    .Where(card => card.Group == TrayCardGroup.Available)
                    .ToArray()
                : Array.Empty<TrayCardViewModel>();
            return new RoundActionViewModel(
                projection.Version,
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase,
                status,
                actions,
                endTurnInvocation,
                // A pending removal choice is mandatory: the authority rejects an advance while it
                // stands, so the control that would produce that rejection is not offered. Milestone
                // 7.8a adds the second such pause on the same terms - a paused system transition is
                // answered by answering it, and an advance offered beside it could only be refused.
                //
                // Milestone 8.35d adds the first clause, and it is why the control was drawn for a
                // seat that could never submit it: the two pauses below are read from the seat's own
                // scoped surface, which reports the standing decision it does NOT own as absent, so
                // five of six seats saw a CONTINUE control beside somebody else's question and
                // pressing it produced an unauthorized refusal. A seat-served round is now continued
                // by its host; only the Director's direct surface still steps it by hand.
                offersRoundContinuation &&
                    projection.IsInputEnabled &&
                    projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.PendingMinionBattleChoice == null &&
                    projection.PendingSystemTransitionStep == null
                    ? new TypedInvocationViewModel("CONTINUE END OF ROUND", DirectorCommand.AdvanceEndOfRound())
                    : null,
                initiativeChoices,
                defenseChoices,
                fastTravelOffers,
                effectChoiceControls,
                effectChoiceTargets,
                effectChoiceDestinations,
                respawnOptions,
                respawn == null
                    ? null
                    : new TypedInvocationViewModel(
                        "RESOLVE WITHOUT ACTION",
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.HeroRespawn,
                            command => command.Kind ==
                                    DirectorCommandKind.ResolveCardWithoutRespawn &&
                                command.HeroId.Equals(respawn.DeclineInvocation.HeroId) &&
                                command.CardInstanceId.Equals(
                                    respawn.DeclineInvocation.CardInstanceId))),
                unavailableEffectChoices,
                unavailableActions,
                clearOffers,
                clearSelection,
                systemTransitionPrompt,
                // Milestone 8.20d: read from the same pause the controls above were built from, so
                // the heading names the decision those controls answer and nothing else.
                projection.PendingEffectChoice == null
                    ? null
                    : new EffectChoicePromptViewModel(EffectChoicePromptLabels.SubjectOf(
                        projection.PendingEffectChoice, projection.PendingEffectChoicePreview)),
                CreateRemovalPreventionPrompt(projection),
                printedActionRegions,
                TeamAgreementHudPresentation.Create(projection),
                // Milestone 8.35f. Read straight off the projection: the host publishes whether
                // this seat may step back, and the control is enabled exactly when it says so.
                projection.IsSeatUndoAvailable,
                AwaitedInputQuery.IsWaitingOnViewer(projection, homeHeroId),
                statusDetail,
                endTurnCancelsOpenAttack,
                endTurnUnavailableReason,
                levelUpHandPreview,
                endTurnInvocation?.Command.Kind == DirectorCommandKind.StopMovement
                    ? new TypedInvocationViewModel("FINISH MOVEMENT",endTurnInvocation.Command)
                    : null);
        }

        private static IReadOnlyList<PrintedActionRegionViewModel> CreatePrintedActionRegions(
            IMatchReadModel projection,
            IReadOnlyList<ActionOffer> offered,
            IReadOnlyList<TypedInvocationViewModel> actions,
            IReadOnlyList<FastTravelOfferViewModel> fastTravelOffers,
            IReadOnlyList<ClearOfferViewModel> clearOffers)
        {
            var regions = new List<PrintedActionRegionViewModel>(offered.Count);
            for (var index = 0; index < offered.Count; index++)
            {
                var offer = offered[index];
                var definition = projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference));
                var action = definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId));
                var secondaries = offered.Where(value =>
                    value.SourceCardInstanceId.Equals(offer.SourceCardInstanceId) &&
                    value.InvocationContext != ActionInvocationContext.PrintedPrimary).ToArray();
                var secondaryIndex = offer.InvocationContext == ActionInvocationContext.PrintedPrimary
                    ? -1
                    : Array.FindIndex(secondaries, value => value.Equals(offer));
                var invocation = actions.SingleOrDefault(value =>
                    value.Command.Kind == DirectorCommandKind.StartAction &&
                    value.Command.ActionDefinitionId.Equals(offer.ActionDefinitionId));
                var label = FormatActionLabel(projection, offer);
                regions.Add(new PrintedActionRegionViewModel(
                    offer,
                    action.Kind,
                    label,
                    secondaryIndex,
                    secondaries.Length,
                    offer.IsAvailable
                        ? $"{label} - available"
                        : $"{label} - {FormatUnavailabilityReason(projection, offer)}",
                    invocation,
                    fastTravelOffers.SingleOrDefault(value =>
                        value.ReplacedMovementActionDefinitionId.Equals(offer.ActionDefinitionId)),
                    clearOffers.SingleOrDefault(value =>
                        value.ReplacedAttackActionDefinitionId.Equals(offer.ActionDefinitionId)),
                    definition.Progression.Color));
            }

            // Keyboard order follows the face from its upper Secondary strip into its Primary band.
            return Array.AsReadOnly(regions
                .OrderBy(value => value.InvocationContext == ActionInvocationContext.PrintedPrimary ? 1 : 0)
                .ThenBy(value => value.SecondaryIndex)
                .ToArray());
        }

        /// <summary>
        /// Milestone 8.21. The standing removal-prevention decision as controls, or <c>null</c> when
        /// none is pending.
        ///
        /// <para>Each payment is drawn as the whole printed face of the card that would pay it, on the
        /// terms milestone 8.15a set for every card-valued choice and milestone 8.11b set for a Defense
        /// offer: the tray's own standalone builder, with the card's name kept as the control's hover
        /// and assistive reading. The decline carries no card, which is what keeps it a distinct
        /// control rather than another face.</para>
        ///
        /// <para>Every command here is the pause's own minted invocation, forwarded unchanged. Nothing
        /// composes an answer, so a source or a card the prompt did not project cannot be submitted
        /// from this surface at all.</para>
        /// </summary>
        private static RemovalPreventionPromptViewModel CreateRemovalPreventionPrompt(
            IMatchReadModel projection)
        {
            var prompt = projection.PendingRemovalPrevention;
            if (prompt == null)
            {
                return null;
            }

            return new RemovalPreventionPromptViewModel(
                prompt.SubjectMinionId,
                prompt.Payments.Select(payment => new TypedInvocationViewModel(
                    FindCardName(projection, payment.PaymentCardInstanceId) ??
                        payment.PaymentCardInstanceId.Value,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, payment.Invocation)),
                    card: CreateCardFace(projection, payment.PaymentCardInstanceId))),
                new TypedInvocationViewModel(
                    RemovalPreventionPromptLabels.DeclineLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.RemovalPrevention,
                        command => command.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                            ReferenceEquals(command.RemovalPrevention, prompt.DeclineInvocation))));
        }

        /// <summary>
        /// Milestone 7.8a: the paused transition as a prompt, or <c>null</c> when nothing is paused or
        /// the rules that paused it describe nothing.
        ///
        /// Every option is built from the description's typed content and carries the invocation the
        /// continuation itself projected, forwarded unchanged. An undescribed pause draws no prompt
        /// rather than a prompt of option identities, because a control a player cannot read is worse
        /// than the absence the milestone is fixing.
        /// </summary>
        public static SystemTransitionPromptViewModel CreateSystemTransitionPrompt(
            IMatchReadModel projection) =>
            CreateSystemTransitionPrompt(
                projection?.PendingSystemTransitionDescription,
                projection?.Definitions,
                projection?.Setup?.Heroes,
                projection?.DecisionContract);

        public static SystemTransitionPromptViewModel CreateSystemTransitionPrompt(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription description,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<QuickGameHeroSetupState> heroes,
            PendingDecisionContract contract)
        {
            if (description == null || description.Options.Count == 0)
            {
                return null;
            }

            // Milestone 8.35u: the DESCRIPTION's own chooser, not the pause's. Rulebook p.15 can
            // hold one open item per hero, so the pause has no single side and asking it throws.
            var side = description.ChoosingSide == TeamSide.Red ? "RED" : "BLUE";
            if (description.Kind ==
                GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade)
            {
                var heroName = HeroDisplayName(heroes, description.HeroId);
                return new SystemTransitionPromptViewModel(
                    description.Kind,
                    "LEVEL UP",
                    $"{side} {heroName} REACHED LEVEL {description.Level} - UPGRADE A CARD",
                    $"PAID {description.PaidCoins} COIN{(description.PaidCoins == 1 ? string.Empty : "S")} - " +
                        $"{description.RemainingCoins} REMAINING",
                    description.Options.Select(option => new SystemTransitionOptionViewModel(
                        $"UPGRADE {DefinitionName(definitions, option.UpgradedCard)} " +
                            $"INTO {DefinitionName(definitions, option.TakenCard)}",
                        FormatUpgradeDetail(definitions, option),
                        PendingDecisionCommandBinding.Require(
                            contract,
                            PendingDecisionFamilies.SystemTransition,
                            command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                                command.SystemTransitionChoice.Equals(
                                    Invocation(description, option))),
                        DefinitionColor(definitions, option.UpgradedCard),
                        option.UpgradedCardInstanceId,
                        // Milestone 8.8a: the candidate itself, as a whole printed face. A definition the
                        // projection does not carry reads as a face that states nothing, which is what
                        // `ReadFace` already does for a tray card - one missing registration must not
                        // take a mandatory decision off the screen.
                        CurrentRoundCards.DeckCard(
                            option.TakenCardInstanceId, Definition(definitions, option.TakenCard)),
                        DefinitionName(definitions, option.UpgradedCard),
                        option.ItemIcon,
                        option.SetAsideCardInstanceId.IsDefined
                            ? DefinitionName(definitions, option.SetAsideCard)
                            : string.Empty)));
            }

            return new SystemTransitionPromptViewModel(
                description.Kind,
                "LANE PUSH",
                $"{side} PLACES THE RESPAWNING MINION",
                "ITS SPAWN POINT IS BLOCKED - CHOOSE A NEAREST EMPTY SPACE",
                description.Options.Select(option => new SystemTransitionOptionViewModel(
                    $"PLACE AT {option.DestinationSpaceId.Value}",
                    $"({option.Destination.Q}, {option.Destination.R})",
                    PendingDecisionCommandBinding.Require(
                        contract,
                        PendingDecisionFamilies.SystemTransition,
                        command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                            command.SystemTransitionChoice.Equals(
                                Invocation(description, option))))));
        }

        private static GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand Invocation(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription description,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionDescription option) =>
            description.PendingStep.Invocations.Single(candidate =>
                candidate.OptionId.Equals(option.OptionId));

        private static string FormatUpgradeDetail(
            IReadOnlyList<CardDefinition> definitions,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionDescription option)
        {
            if (!option.SetAsideCardInstanceId.IsDefined)
            {
                return "NOTHING IS SET ASIDE - NO ITEM";
            }

            var setAside = DefinitionName(definitions, option.SetAsideCard);
            return option.ItemIcon == PrintedValueChannel.None
                ? $"SETS {setAside} ASIDE - NO MATCHING ITEM SLOT"
                : $"SETS {setAside} ASIDE AS A " +
                    $"{MatchHudTheme.ChannelToken(option.ItemIcon)} {MatchHudTheme.Signed(1)} ITEM";
        }

        /// <summary>
        /// The card's own printed colour, read from its definition exactly as the tray reads one, so a
        /// transition option and a card in hand can never disagree about what colour a card is.
        /// </summary>
        private static CardColor DefinitionColor(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference))
                ?.Progression.Color ?? CardColor.None;

        /// <summary>
        /// The definition behind one reference, or null when this projection does not carry it - which
        /// only happens for content that was never registered. Milestone 8.8a: the upgrade prompt draws
        /// a whole card face from it, and <see cref="CurrentRoundCards.ReadFace"/> already states the
        /// absence as a face rather than faulting.
        /// </summary>
        private static CardDefinition Definition(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference));

        private static string DefinitionName(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference reference) =>
            (definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(reference))?.DisplayName
                ?? reference.DefinitionId.Value;

        private static string HeroDisplayName(
            IReadOnlyList<QuickGameHeroSetupState> heroes, HeroId heroId) =>
            (heroes ?? Array.Empty<QuickGameHeroSetupState>())
                .FirstOrDefault(hero => hero.HeroId.Equals(heroId))?.DisplayName.ToUpperInvariant()
                ?? heroId.Value;

        /// <summary>
        /// Milestone 6.3a: the open Clear's controls, or <c>null</c> when none is open.
        ///
        /// The subset the player has chosen is matched to one projected invocation rather than being
        /// turned into a payload here - <see cref="ClearOptionProjection.FindInvocation"/> answers it,
        /// and an answer of none closes the selection instead of composing a command the projection
        /// never offered. A token the player had chosen and the projection no longer offers therefore
        /// cannot survive into a confirm.
        /// </summary>
        private static ClearSelectionViewModel CreateClearSelection(
            IReadOnlyList<ClearOptionProjection> options,
            ActionDefinitionId openClearActionDefinitionId,
            IReadOnlyList<TokenInstanceId> selectedTokenIds)
        {
            if (!openClearActionDefinitionId.IsDefined)
            {
                return null;
            }

            var option = options.SingleOrDefault(value =>
                value.ReplacedAttackActionDefinitionId.Equals(openClearActionDefinitionId));
            if (option == null)
            {
                return null;
            }

            var offered = new HashSet<TokenInstanceId>(option.AdjacentTokens.Select(token => token.Id));
            var selected = selectedTokenIds.Where(offered.Contains).Distinct().ToArray();
            var invocation = option.FindInvocation(selected);
            if (invocation == null)
            {
                return null;
            }

            // The ordinal rule the token label has followed since milestone 6.2d, decided once over
            // the whole offered set: a name that occurs twice keeps its ordinal, one that occurs once
            // drops it.
            var repeated = new HashSet<string>(option.AdjacentTokens
                .GroupBy(token => token.Definition.DisplayName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);
            var candidates = option.AdjacentTokens
                .Select((token, index) => new ClearTokenCandidateViewModel(
                    ClearLabels.CandidateLabel(
                        token.Definition.DisplayName,
                        repeated.Contains(token.Definition.DisplayName) ? index + 1 : (int?)null),
                    token.Id,
                    token.Coordinate,
                    selected.Contains(token.Id)))
                .ToArray();
            return new ClearSelectionViewModel(
                option.ReplacedAttackActionDefinitionId,
                candidates,
                new TypedInvocationViewModel(ClearLabels.ConfirmLabel, invocation),
                ClearLabels.CancelLabel);
        }

        /// <summary>
        /// Forwards a paused effect frame's decision without deciding anything. Every invocation here
        /// is one the projection already constructed and bounded - which candidates may still be
        /// selected, whether the selection satisfies the minimum, whether declining is allowed - so
        /// this method chooses only the order they are shown in, which is the projection's own.
        ///
        /// Milestone 6.5c splits where a candidate is answered, and the split is the option/value one
        /// the authority already draws plus the projected occupancy of a value. An option is a mode
        /// and stays a control; a value naming an object is a control named after that object; a value
        /// naming an empty space has no control at all, because the board says it better. No branch
        /// here reads a card, a hero, or a node name.
        /// </summary>
        private static void CreateEffectChoices(
            IMatchReadModel projection,
            out IReadOnlyList<TypedInvocationViewModel> controls,
            out IReadOnlyList<EffectChoiceTargetViewModel> targets,
            out IReadOnlyList<TypedInvocationViewModel> destinations,
            out IReadOnlyList<UnavailableEffectChoiceViewModel> unavailable)
        {
            controls = Array.Empty<TypedInvocationViewModel>();
            targets = Array.Empty<EffectChoiceTargetViewModel>();
            destinations = Array.Empty<TypedInvocationViewModel>();
            unavailable = Array.Empty<UnavailableEffectChoiceViewModel>();
            var choice = projection.PendingEffectChoice;
            if (choice == null)
            {
                return;
            }

            var ordinals = new Dictionary<EffectChoiceCandidate, int>();
            for (var index = 0; index < choice.Candidates.Count; index++)
            {
                ordinals[choice.Candidates[index]] = index + 1;
            }

            // Milestone 8.11c: keyed by the candidate's value rather than by its coordinate, so a
            // candidate naming a hero or a minion by identity finds its own preview. The query
            // guarantees one preview per candidate value and refuses two on one space, so neither
            // this nor the reverse lookup below can be ambiguous.
            var previews = projection.PendingEffectChoicePreview
                .ToDictionary(preview => preview.CandidateValue);

            // Milestone 6.2d: which token names this choice would repeat, decided once over the whole
            // offered set rather than per candidate. A name that occurs twice keeps its ordinal, so two
            // Hazards stay distinguishable; a name that occurs once drops it.
            var repeatedTokenNames = new HashSet<string>(projection.PendingEffectChoicePreview
                .Select(preview => FindTokenName(projection, preview))
                .Where(name => name != null)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);

            // Milestone 8.15, on the same decided-once-over-the-set terms: which card names this
            // choice offers more than once. Wasp's eighteen are distinct, so the ordinal this feeds
            // is for content that deals duplicates, exactly as the token rule is.
            var repeatedCardNames = new HashSet<string>(choice.SelectInvocations
                .Select(invocation => invocation.Candidate)
                .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => FindCardName(projection, candidate.Value.CardInstanceId))
                .Where(name => name != null)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key), StringComparer.Ordinal);
            var invocations = new List<TypedInvocationViewModel>();
            var namedTargets = new List<EffectChoiceTargetViewModel>();
            var boardDestinations = new List<TypedInvocationViewModel>();
            foreach (var invocation in choice.SelectInvocations)
            {
                var candidate = invocation.Candidate;
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    candidateCommand => candidateCommand.Kind ==
                            DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(candidateCommand.EffectChoice, invocation));

                // Milestone 6.5b: a value candidate can also name a piece that is not on the board at
                // all - a Spawn draws from the supply - and such a candidate is a control like a mode,
                // because there is no space for the board to light and nothing standing for a cue to
                // mark. What it is not is anonymous.
                //
                // Milestone 8.11c: "not on the board" is now asked of the board rather than assumed
                // from the value's kind. A minion named by identity is a supply candidate only when
                // occupancy has it standing nowhere; one that is standing somewhere is marked and
                // clicked like every other piece, and the preview is what knows which.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Minion &&
                    !previews.ContainsKey(candidate.Value))
                {
                    invocations.Add(new TypedInvocationViewModel(
                        FormatSupplyMinionLabel(projection, candidate.Value.MinionId, ordinals[candidate]),
                        command));
                    continue;
                }

                // Milestone 8.37: an integer whose choice DECLARED a vocabulary is named by that
                // vocabulary's own word, not by its number. Tali's Commune with Spirits offers six
                // card colours and her Cold Snap six board directions, and both reached a player as
                // the buttons "0" through "5" under the rule below - which is unanswerable, and worse
                // for Commune, whose decision is put to an ALLY who never saw the card. The meaning
                // is read off the choice's own declaration rather than guessed from the values; see
                // EffectChoiceValueVocabulary for why guessing was refused.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Integer &&
                    choice.ValueVocabulary != EffectChoiceValueVocabulary.None)
                {
                    invocations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.VocabularyLabel(
                            choice.ValueVocabulary, candidate.Value.IntegerValue),
                        command,
                        swatch: choice.ValueVocabulary == EffectChoiceValueVocabulary.CardColor
                            ? (CardColor)candidate.Value.IntegerValue
                            : CardColor.None));
                    continue;
                }

                // Milestone 8.15: a candidate that is a printed number is named as that number. The
                // one production select of this shape is the kinetic "3 or 4 spaces" distance, whose
                // two controls read `OPTION #1` and `OPTION #2` until the player-pipeline journey
                // met them - a player choosing between the printed 3 and the printed 4 reads 3 and 4.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Integer)
                {
                    invocations.Add(new TypedInvocationViewModel(
                        candidate.Value.IntegerValue.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                        command));
                    continue;
                }

                // Milestone 8.15: a candidate that names a card is named as that card - the chooser
                // rulebook p.10 hands a discard to was reading `OPTION #1` where their own hand
                // stood, which the first player-pipeline journey met and no headless fixture could.
                // Milestone 8.15a carries the whole printed face beside the name, at the user's
                // direction, so the control is the card itself exactly as a Defense offer is; the
                // name stays as the control's hover and assistive reading.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.CardInstance)
                {
                    var cardName = FindCardName(projection, candidate.Value.CardInstanceId);
                    invocations.Add(new TypedInvocationViewModel(
                        cardName == null
                            ? EffectChoiceLabels.CandidateLabel(
                                candidate.OptionNodeId, ordinals[candidate])
                            : EffectChoiceLabels.CardTargetLabel(
                                cardName,
                                repeatedCardNames.Contains(cardName)
                                    ? ordinals[candidate]
                                    : (int?)null),
                        command,
                        card: CreateCardFace(projection, candidate.Value.CardInstanceId)));
                    continue;
                }

                if (candidate.Kind != EffectChoiceCandidateKind.Value ||
                    !previews.TryGetValue(candidate.Value, out var preview))
                {
                    invocations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                        command));
                    continue;
                }

                if (!preview.NamesObject)
                {
                    boardDestinations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                        command));
                    continue;
                }

                // Milestone 6.3a: a target that names a token carries that token's identity, so the
                // board can mark the piece rather than reading occupancy back out of the space.
                namedTargets.Add(new EffectChoiceTargetViewModel(
                    FormatTargetLabel(projection, preview, ordinals[candidate], repeatedTokenNames),
                    preview.Coordinate,
                    preview.ResultingCoordinate,
                    command,
                    FindToken(projection, preview)?.Id ?? default));
            }

            foreach (var invocation in choice.DeselectInvocations)
            {
                var command = PendingDecisionCommandBinding.Require(
                    projection,
                    PendingDecisionFamilies.Effect,
                    candidateCommand => candidateCommand.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        ReferenceEquals(candidateCommand.EffectChoice, invocation));
                var candidate = invocation.Candidate;
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    previews.TryGetValue(candidate.Value, out var preview))
                {
                    if (preview.NamesObject)
                    {
                        namedTargets.Add(new EffectChoiceTargetViewModel(
                            FormatTargetLabel(
                                projection, preview, ordinals[candidate], repeatedTokenNames),
                            preview.Coordinate,
                            preview.ResultingCoordinate,
                            command,
                            FindToken(projection, preview)?.Id ?? default));
                    }
                    else
                    {
                        boardDestinations.Add(new TypedInvocationViewModel(
                            EffectChoiceLabels.DeselectLabel(
                                candidate.OptionNodeId, ordinals[candidate]),
                            command));
                    }

                    continue;
                }

                // Milestone 8.37b. A declared vocabulary names the take-back too, and wears the same
                // swatch the control that offered it did.
                if (candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Integer &&
                    choice.ValueVocabulary != EffectChoiceValueVocabulary.None)
                {
                    invocations.Add(new TypedInvocationViewModel(
                        EffectChoiceLabels.DeselectLabel(
                            choice.ValueVocabulary, candidate.Value.IntegerValue),
                        command,
                        swatch: choice.ValueVocabulary == EffectChoiceValueVocabulary.CardColor
                            ? (CardColor)candidate.Value.IntegerValue
                            : CardColor.None));
                    continue;
                }

                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeselectLabel(
                        candidate.OptionNodeId, ordinals[candidate]),
                    command));
            }

            if (choice.ConfirmInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    choice.Kind == EffectChoiceKind.ConfirmOrDecline
                        ? EffectChoiceLabels.AcceptLabel
                        : EffectChoiceLabels.CompletionLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.ConfirmInvocation))));
            }

            if (choice.DeclineInvocation != null)
            {
                invocations.Add(new TypedInvocationViewModel(
                    EffectChoiceLabels.DeclineLabel,
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, choice.DeclineInvocation))));
            }

            controls = invocations;
            targets = namedTargets;
            destinations = boardDestinations;

            // Milestone 6.5ac: an option the authority refused keeps its label and its place in the
            // ordering, and gains no command. Presentation decides nothing here - which options are
            // unperformable is projected, exactly as which candidates are legal always has been.
            unavailable = choice.UnavailableCandidates
                .Select(candidate => new UnavailableEffectChoiceViewModel(
                    EffectChoiceLabels.CandidateLabel(candidate.OptionNodeId, ordinals[candidate]),
                    candidate))
                .ToArray();
        }

        /// <summary>
        /// The projected display name of one card instance, or null when the projection does not
        /// carry it. Read exactly the way the tray names the same card - instance to definition to
        /// display name - and never from printed card text.
        /// </summary>
        private static string FindCardName(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(cardInstanceId));
            if (card == null)
            {
                return null;
            }

            return projection.Definitions
                .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference))?
                .DisplayName;
        }

        /// <summary>
        /// Names the object a target candidate stands on, from projected state alone. A hero is named
        /// by its configured display name, a minion by the team and intrinsic type its occupancy
        /// carries, and - since milestone 6.2d - a token by the display name its projected definition
        /// carries, with an ordinal only where the choice offers that name twice.
        /// </summary>
        private static string FormatTargetLabel(
            IMatchReadModel projection,
            EffectChoiceCandidatePreview preview,
            int ordinal,
            ICollection<string> repeatedTokenNames)
        {
            switch (preview.Occupant.Kind)
            {
                case BoardObjectKind.Hero:
                    var hero = projection.Setup.Heroes.SingleOrDefault(value =>
                        value.HeroId.Equals(preview.Occupant.HeroId));
                    return hero == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.HeroTargetLabel(hero.TeamSide, hero.DisplayName);
                case BoardObjectKind.Minion:
                    var minion = projection.Minions.SingleOrDefault(value =>
                        value.MinionId.Equals(preview.Occupant.MinionId));
                    return minion == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.MinionTargetLabel(
                            minion.TeamSide, minion.MinionType, ordinal);
                default:
                    var tokenName = FindTokenName(projection, preview);
                    return tokenName == null
                        ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                        : EffectChoiceLabels.TokenTargetLabel(
                            tokenName,
                            repeatedTokenNames.Contains(tokenName) ? ordinal : (int?)null);
            }
        }

        /// <summary>
        /// The projected display name of the token a preview names, or <c>null</c> when the preview
        /// names something else. A projection that has lost the instance falls back to the anonymous
        /// label rather than faulting, which is the same tolerance the hero and minion arms show.
        /// </summary>
        private static string FindTokenName(
            IMatchReadModel projection, EffectChoiceCandidatePreview preview) =>
            FindToken(projection, preview)?.Definition.DisplayName;

        /// <summary>
        /// The projected token a preview names, or <c>null</c> when it names something else or the
        /// projection no longer carries that instance. Milestone 6.3a made it the shared reading: the
        /// label and the board cue must be talking about the same piece, and they are only guaranteed
        /// to be when both come from one lookup against one list.
        /// </summary>
        private static PlacedToken FindToken(
            IMatchReadModel projection, EffectChoiceCandidatePreview preview) =>
            preview.Occupant.Kind != BoardObjectKind.Token
                ? null
                : projection.PlacedTokens.SingleOrDefault(token =>
                    token.Id.Equals(preview.Occupant.TokenInstanceId));

        /// <summary>
        /// Names a miniature a candidate would take from the supply. The board projection cannot
        /// describe it, because it is standing nowhere; the declared setup roster carries the team and
        /// the intrinsic type, which is the same pair a standing minion is named by, so a supply
        /// candidate and a board candidate read alike rather than one of them reading as an ordinal.
        /// </summary>
        private static string FormatSupplyMinionLabel(
            IMatchReadModel projection, MinionId minionId, int ordinal)
        {
            var declared = projection.Setup.Minions.SingleOrDefault(value =>
                value.MinionId.Equals(minionId));
            return declared == null
                ? EffectChoiceLabels.CandidateLabel(default, ordinal)
                : EffectChoiceLabels.MinionTargetLabel(
                    declared.TeamSide, declared.MinionType, ordinal);
        }

        private RoundActionViewModel GetOrCreateViewModel(IMatchReadModel projection)
        {
            // Milestone 6.3a: an open Clear belongs to the projection it was opened against. A
            // projection-version change - an accepted or refused command, End Turn, Undo, restart,
            // restore, or replay - drops it, so a stale token control or subset can never be
            // submitted and the surface is rebuilt from the position as it now is.
            if (clearModeProjection != null && !ReferenceEquals(projection, clearModeProjection))
            {
                clearModeProjection = null;
                clearModeActionDefinitionId = default;
                clearSelection.Clear();
                lastProjection = null;
            }

            if (!ReferenceEquals(projection, lastProjection))
            {
                lastProjection = projection;
                lastViewModel = CreateViewModel(
                    projection, clearModeActionDefinitionId, clearSelection, session.IsDirector,
                    homeHeroId);
            }

            return lastViewModel;
        }

        private static TypedInvocationViewModel CreateEndTurnInvocation(IMatchReadModel projection)
        {
            var movementChoice = projection.PendingMovementChoice;
            if (movementChoice != null && movementChoice.CanStop)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Movement,
                        command => command.Kind == DirectorCommandKind.StopMovement &&
                            command.ResolutionId.Equals(movementChoice.ResolutionId) &&
                            command.ChoiceId.Equals(movementChoice.ChoiceId)));
            }

            // Milestone 8.35v (the THIRD amendment: "an undeclined optional branch" is named among
            // the states a player may walk away from). A confirm-or-decline step, or any bounded
            // choice whose printed minimum is zero, already projects its own Decline the same way
            // the contextual region's own ACCEPT/DECLINE banners read it - this branch reads that
            // existing invocation rather than composing a new one, so END TURN's press and the
            // banner's own DECLINE press are provably the same authoritative answer.
            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice?.DeclineInvocation != null)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, effectChoice.DeclineInvocation)));
            }

            // Milestone 6.5ae: the same contextual control on the decision family that had lacked it.
            // A paused effect choice whose every option was refused projects a stop of its own, and it
            // is offered exactly where the authority offers it - this branch reads the invocation and
            // never decides that a choice is closable.
            if (effectChoice?.StopInvocation != null)
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Effect,
                        command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                            ReferenceEquals(command.EffectChoice, effectChoice.StopInvocation)));
            }

            // Milestone 8.35g's pause; milestone 8.35v (the THIRD amendment's REVISED SEMANTICS)
            // collapses what used to be its own second press into the SAME press that stopped,
            // declined or Held above - see <see cref="RoundActionPresenter.Submit"/>, which chains
            // straight into this branch's command once the completion it just submitted lands the
            // projection here. Read here, this branch is unchanged: the turn's resolution is
            // finished and waiting for this player to hand it on, read off the standing contract so
            // only the seat that owns the pause is drawn one.
            if (projection.DecisionContract != null &&
                projection.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
            {
                return new TypedInvocationViewModel(
                    "END TURN",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.TurnRelease,
                        command => command.Kind == DirectorCommandKind.ReleaseTurn));
            }

            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined ||
                HasPendingDecision(projection))
            {
                return null;
            }


            // Milestone 8.7: the authority decides whether p.12's Hold is offered, and this draws its
            // answer. Deriving it here a second time is what made the control and the command able to
            // disagree.
            var hold = projection.AvailableHoldOffer;
            return hold.HasValue && hold.Value.IsAvailable
                ? new TypedInvocationViewModel(
                    "END TURN",
                    DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId))
                : null;
        }

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment). The one open, not-yet-committed selection this
        /// build has no authority-offered decline for: an Attack whose target is not yet chosen.
        /// Rulebook p.14's target choice is mandatory once an Attack starts and the authority offers
        /// no stop for it the way Movement and an optional effect choice do - but the choice itself
        /// has not committed anything yet (milestone 8.35f's own wall rises only once
        /// <c>ChooseAttackTarget</c> is accepted, and 8.35p extended that same wall to a card-text-
        /// bound Attack that never emits the command at all), so the seat's own bounded rewind can
        /// discard it. True only when that rewind is actually standing - the same
        /// <see cref="IMatchReadModel.IsSeatUndoAvailable"/> fact milestone 8.35f's own square
        /// reads - so a state this method has not anticipated falls through to
        /// <see cref="ResolveEndTurnUnavailableReason"/> rather than composing a rewind the host
        /// would refuse. Gated by <see cref="IsViewerTheActingHero"/> for the same reason that
        /// gates the other helper below - see its remarks.
        /// </summary>
        private static bool ResolveEndTurnCancelsOpenAttack(
            IMatchReadModel projection, HeroId homeHeroId, TypedInvocationViewModel endTurnInvocation) =>
            endTurnInvocation == null &&
            IsViewerTheActingHero(projection, homeHeroId) &&
            projection.PendingAttackTargetChoice != null &&
            projection.IsSeatUndoAvailable;

        /// <summary>
        /// Milestone 8.35v. Whether this viewer is the hero the match is presently waiting on for a
        /// turn action - never true for a seat that merely owns SOME standing decision the acting
        /// hero's own turn produced (a defender answering Defense, a hero displaced by a push, one
        /// paying a removal prevention). <see cref="IMatchReadModel.SelectedActor"/> is public
        /// (every viewer's board marks the same acting piece, milestone 8.35n), so a seat-scoped
        /// projection cannot be trusted to read as "mine" from its mere presence the way an
        /// owner-narrowed pending choice can - <c>AwaitedInputQuery</c> names the identical trap for
        /// the top-bar predicate: "an attacker is still the selected actor while the DEFENDER is
        /// being asked, so a SelectedActor-based predicate lights the attacker's bar during someone
        /// else's decision." The Director answers every family at once and passes a default
        /// <paramref name="homeHeroId"/>, so it reads every branch below as its own.
        /// </summary>
        /// <remarks>
        /// <b>Milestone 8.71h.</b> The comparison is against the DECIDING hero rather than against the
        /// selected actor, which is the whole of Hanu the Trickster's The Ultimate Trick at this tier:
        /// "You choose the next action, and how it is performed for a hero you target with the 'Hurry
        /// Up!'." While that delegation stands, the acting piece on the board is the targeted hero and
        /// the seat that must be given the controls is Hanu's - so an actor-based comparison lit the
        /// wrong tray in both directions at once, offering the targeted player controls the host would
        /// refuse and darkening the controls of the seat the host was actually waiting on.
        /// <c>DelegatedActionQuery.DecidingHeroFor</c> answers the actor itself whenever no delegation
        /// stands, so every other hero, every other card and every earlier milestone reads exactly as
        /// before; and both the rewritten card and the unlocked Ultimate are rulebook p.4 OPEN zones,
        /// so every seat derives the same answer from the frame it already receives rather than
        /// needing a fact of its own on the wire.
        /// </remarks>
        private static bool IsViewerTheActingHero(IMatchReadModel projection, HeroId homeHeroId) =>
            projection.IsTurnActive &&
            projection.SelectedActor.IsDefined &&
            (!homeHeroId.IsDefined || DecidingHeroFor(projection).Equals(homeHeroId));

        /// <summary>
        /// Milestone 8.71h. Whose seat is choosing the selected actor's action - the actor themselves
        /// on every turn no delegation stands over, and Hanu while one does.
        ///
        /// <para><b>Milestone 8.72</b> moved the derivation itself into
        /// <see cref="DelegatedActionPresentation"/>, which is the one owner of "which card's
        /// controls does this seat draw, and why" - this presenter, <see cref="AwaitedInputQuery"/>
        /// and <see cref="DirectorHandsPresenter"/> all read the same answer rather than three
        /// copies of one call.</para>
        /// </summary>
        private static HeroId DecidingHeroFor(IMatchReadModel projection) =>
            DelegatedActionPresentation.DecidingHeroFor(projection);

        /// <summary>
        /// Milestone 8.35v. Reached only once <see cref="CreateEndTurnInvocation"/> found no direct
        /// completion and <see cref="ResolveEndTurnCancelsOpenAttack"/> found no open Attack
        /// selection to discard - so if this viewer is genuinely the acting hero, every
        /// printed-mandatory step and every wall the game can put in front of it is exactly one of
        /// the two named reasons below. Named per the SIXTH amendment's cursor-tooltip rule: nothing
        /// is ever greyed without saying why.
        /// </summary>
        private static string ResolveEndTurnUnavailableReason(
            IMatchReadModel projection, HeroId homeHeroId)
        {
            if (!IsViewerTheActingHero(projection, homeHeroId))
            {
                return null;
            }

            // A printed rule leaves the standing step no legal decline: Movement below its required
            // minimum, or an effect choice with real candidates, no printed "may", and no stop
            // invocation of its own - neither an undeclined optional branch nor an emptied mandatory
            // one.
            if (projection.PendingMovementChoice != null && !projection.PendingMovementChoice.CanStop)
            {
                return ActionUnavailabilityLabels.MandatoryStep;
            }

            if (projection.PendingEffectChoice != null &&
                projection.PendingEffectChoice.DeclineInvocation == null &&
                projection.PendingEffectChoice.StopInvocation == null)
            {
                return ActionUnavailabilityLabels.MandatoryStep;
            }

            // The resolution has moved past anything this seat can answer or cancel: a target was
            // already chosen and the defender (or the card's own text) now owns the wait, exactly
            // milestone 8.35f/8.35p's own wall. The remaining declared families never coincide with
            // this seat's own acting turn in practice - defense, respawn, correction and removal
            // prevention are each asked of a different hero, and tied Initiative precedes a turn
            // rather than standing inside one - but each is named here rather than assumed absent,
            // so a state nobody anticipated still reads as "another seat's decision" instead of
            // silently drawing an END TURN that would refuse.
            if ((projection.HasPendingAttackResolution && projection.PendingAttackTargetChoice == null) ||
                (projection.HasPendingMovementResolution && projection.PendingMovementChoice == null) ||
                (projection.HasPendingSkillResolution && projection.PendingEffectChoice == null) ||
                projection.PendingDefenseChoice != null ||
                projection.PendingHeroRespawnChoice != null ||
                projection.PendingDisplacedMinionCorrection != null ||
                projection.PendingRemovalPrevention != null ||
                projection.PendingInitiativeChoice != null)
            {
                return ActionUnavailabilityLabels.AnotherSeatDeciding;
            }

            // Every named cause is accounted for above; a state that reaches here has none of them
            // and still could not compose a direct completion (Hold refused for some other reason a
            // future milestone may name) - the honest generic word rather than a guess.
            return ActionUnavailabilityLabels.Generic;
        }

        private static IReadOnlyList<TypedInvocationViewModel> CreateDefenseChoices(
            IMatchReadModel projection)
        {
            var choice = projection.PendingDefenseChoice;
            var preview = projection.PendingDefensePreview;
            if (choice == null || preview == null ||
                !preview.ResolutionId.Equals(choice.ResolutionId) ||
                !preview.ChoiceId.Equals(choice.ChoiceId))
            {
                return Array.Empty<TypedInvocationViewModel>();
            }

            var values = choice.LegalDefenseOffers
                .Select(offer =>
                {
                    var sourceDefinition = projection.Definitions.Single(value =>
                        value.Reference.Equals(offer.SourceDefinitionReference));
                    var sourceAction = sourceDefinition.ActionDefinitions.Single(value =>
                        value.Id.Equals(offer.ActionDefinitionId));
                    var choicePreview = preview.Choices.Single(value =>
                        value.CardInstanceId.HasValue &&
                        value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));

                    // Milestone 8.11a. A Block has no printed Defense to name, so it is named by what
                    // it does. Asking for its printed icon was the fault that stopped an attack
                    // answered by one: `Single` found nothing on a Block's empty contribution list and
                    // threw where the prompt is drawn, so the defender was asked nothing at all.
                    var label = choicePreview.DefenseTotal.HasValue
                        ? $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                            "PRINTED DEFENSE " +
                            choicePreview.DefenseContributions
                                .Single(value => value.Kind == ValueContributionKind.PrintedIcon).Amount
                        : $"DISCARD {choicePreview.ChoiceLabel.ToUpperInvariant()}\n" +
                            (choicePreview.BlockResult == DefenseBlockResult.Blocked
                                ? "BLOCK - CONDITION MET"
                                : "BLOCK - CONDITION NOT MET");
                    return new TypedInvocationViewModel(
                        label,
                        PendingDecisionCommandBinding.Require(
                            projection,
                            PendingDecisionFamilies.Defense,
                            command => command.Kind == DirectorCommandKind.ChooseDefenseCard &&
                                command.ResolutionId.Equals(choice.ResolutionId) &&
                                command.ChoiceId.Equals(choice.ChoiceId) &&
                                command.CardInstanceId.Equals(offer.SourceCardInstanceId)),
                        CreateDefenseBreakdown(projection.Version, preview, choicePreview),
                        CreateCardFace(projection, offer.SourceCardInstanceId),
                        sourceAction.Origin == ActionDefinitionOrigin.PrintedPrimary
                            ? CardShineSlot.PrimaryRing
                            : CardShineSlot.SecondaryDefense);
                })
                .ToList();
            if (choice.CanDecline)
            {
                var declinePreview = preview.Choices.Single(value => value.IsDecline);
                values.Add(new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    PendingDecisionCommandBinding.Require(
                        projection,
                        PendingDecisionFamilies.Defense,
                        command => command.Kind == DirectorCommandKind.DeclineDefense &&
                            command.ResolutionId.Equals(choice.ResolutionId) &&
                            command.ChoiceId.Equals(choice.ChoiceId)),
                    CreateDefenseBreakdown(projection.Version, preview, declinePreview)));
            }

            return values;
        }

        /// <summary>
        /// One offered card as the tray draws it.
        ///
        /// <para>Milestone 8.11b built it for p.11's Defense response; milestone 8.15a hands the same
        /// face to every card-instance effect-choice candidate - rulebook p.10's discard chooser
        /// included - so a choice among cards is a choice among the cards themselves. It is
        /// <see cref="CurrentRoundCards.TrayCard"/> and nothing composed beside it, so an offer shows
        /// the same Initiative, name, printed values, qualifiers and exact source text the same card
        /// shows in the hand it is still sitting in - one card, one face. It is built selectable
        /// because the authority is offering it, which is what keeps the face at full strength rather
        /// than wearing the spent-card treatment.</para>
        /// </summary>
        private static TrayCardViewModel CreateCardFace(
            IMatchReadModel projection,
            CardInstanceId cardInstanceId)
        {
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
                    // Milestone 8.35m: every caller of this method is offering the card as a legal
                    // answer - the doc comment above already says so - so the card-scope shine is
                    // unconditional here too, on the same reasoning `isSelectable: true` already used.
                    isAwaitedAnswer: true);
        }

        public static DefenseBreakdownViewModel CreateDefenseBreakdown(
            long version,
            PendingDefensePreviewProjection preview,
            DefenseChoicePreviewProjection choice)
        {
            var lines = new List<string> { "ATTACK:" };
            lines.AddRange(preview.AttackContributions.Select(FormatContribution));
            lines.Add($"ATTACK TOTAL: {preview.AttackTotal}");
            lines.Add(string.Empty);
            lines.Add("DEFENSE:");
            if (!choice.IsDecline)
            {
                lines.Add($"  CARD: {choice.ChoiceLabel.ToUpperInvariant()}");
                lines.AddRange(choice.DefenseContributions.Select(FormatContribution));
            }
            if (choice.BlockResult == DefenseBlockResult.Blocked)
            {
                lines.Add("  BLOCK");
                lines.Add("RESULT: BLOCK - DEFENDED");
            }
            else if (choice.BlockResult == DefenseBlockResult.ConditionUnfulfilled)
            {
                // Milestone 8.11a: named apart from the no-Defense-Total case below, because p.14's
                // other half is not "the attack succeeds" - it is "you are defeated", and a defender
                // deciding between offers has to be told which of the two this is.
                lines.Add("  BLOCK - CONDITION NOT MET");
                lines.Add("RESULT: DEFEATED");
            }
            else if (!choice.DefenseTotal.HasValue)
            {
                lines.Add("  NO DEFENSE TOTAL");
                lines.Add("RESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS");
            }
            else
            {
                lines.Add($"DEFENSE TOTAL: {choice.DefenseTotal.Value}");
                lines.Add(choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                    ? "RESULT: DEFENDED"
                    : "RESULT: ATTACK SUCCEEDS");
            }

            var resultStatement = choice.BlockResult == DefenseBlockResult.Blocked
                ? "BLOCK — DEFENDED"
                : choice.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                    ? "DEFEATED"
                    : !choice.DefenseTotal.HasValue
                        ? "NO DEFENSE — ATTACK SUCCEEDS"
                        : choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                            ? "DEFENDED"
                            : "ATTACK SUCCEEDS";
            return new DefenseBreakdownViewModel(
                version,
                preview.ResolutionId,
                preview.ChoiceId,
                choice.CardInstanceId,
                choice.IsDecline
                    ? "DEFENSE PREVIEW: DECLINE"
                    : $"DEFENSE PREVIEW: {choice.ChoiceLabel.ToUpperInvariant()}",
                string.Join("\n", lines),
                choice.Outcome,
                preview.AttackContributions.Select(CreateBreakdownRow),
                preview.AttackTotal,
                choice.DefenseContributions.Select(CreateBreakdownRow),
                choice.DefenseTotal,
                choice.BlockResult,
                resultStatement);
        }

        private static DefenseContributionRowViewModel CreateBreakdownRow(
            ValueContributionProjection contribution) =>
            new DefenseContributionRowViewModel(
                contribution.Amount, contribution.SourceLabel);

        private static string FormatContribution(ValueContributionProjection contribution)
        {
            var sign = contribution.Amount >= 0 ? "+" : string.Empty;
            return $"  {sign}{contribution.Amount} — {contribution.SourceLabel}";
        }

        private static string FormatActionLabel(IMatchReadModel projection, ActionOffer offer)
        {
            var definition = projection.Definitions.Single(value =>
                value.Reference.Equals(offer.SourceDefinitionReference));
            var action = definition.ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId));
            var channel = action.Kind == ActionKind.Attack
                ? PrintedValueChannel.Attack
                : action.Kind == ActionKind.Movement
                    ? PrintedValueChannel.Movement
                    : PrintedValueChannel.None;
            var value = channel == PrintedValueChannel.None
                ? string.Empty
                : $" {PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, channel).Total}";
            // Milestone 8.57g. `offer.AttackRange` is and stays the card's PRINTED classification -
            // `ActionOffer` equality guards every authorization path, so an offer whose subtype moved
            // with an Ultimate would stop matching the command that answers it. p.13's overlay is
            // therefore applied where the label is drawn, through the same query the attack backend and
            // the defence preview read, and the printed Range the Ultimate declares REPLACES the card's
            // own icon exactly as `RangedAttackOverlayQuery.OverlayPrintedRange` replaces it.
            var unlockedUltimates = GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                projection.Definitions, projection.Cards);
            var declaredRange = GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery
                .DeclaredPrintedRangeFromUnlockedUltimates(offer.ActorId, unlockedUltimates);
            var range = action.Kind == ActionKind.Attack &&
                GuardsDigital.Core.Rules.CardResolution.RangedAttackOverlayQuery.ClassifyFromUnlockedUltimates(
                    offer.AttackRange, offer.ActorId, unlockedUltimates) == AttackRangeKind.Ranged
                ? $" RANGE {(declaredRange > 0 ? declaredRange : PrintedValueQuery.CalculatePrintedValue(offer, projection.Definitions, PrintedValueChannel.Range).Total)}"
                : string.Empty;
            var policy = action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                ? "text applies"
                : "text suppressed";
            return $"{FormatInvocationContext(offer.InvocationContext)} " +
                $"{action.Kind.ToString().ToUpperInvariant()}{value}{range}\n{policy}";
        }

        private static string FormatInvocationContext(ActionInvocationContext context)
        {
            return context == ActionInvocationContext.PrintedPrimary ? "PRIMARY" : "SECONDARY";
        }

        private static string GetCardDisplayName(IMatchReadModel projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardId));
            return projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).DisplayName.ToUpperInvariant();
        }

        /// <summary>
        /// Milestone 8.35s (the SIXTH amendment's cursor-tooltip design): the vocabulary word an
        /// unavailable printed-action offer's hover reads, sourced from the authority's own answer and
        /// never re-derived here.
        ///
        /// <para>Only <see cref="ActionUnavailabilityKind.BlockedByRuleModification"/> is a stated
        /// reason today - milestone 8.35l's Swap/Place restriction, threaded end to end as
        /// <see cref="ActionOffer.Unavailability"/>. Every other unavailable printed-action offer
        /// carries <see cref="ActionUnavailabilityKind.None"/>, and that single unstated bit is honestly
        /// ambiguous: <c>TurnResolutionAuthority.StampAvailability</c> greys the same way whether the
        /// cause is an Attack or Skill with nothing left to target, an action definition whose card text
        /// has no typed composition yet, an actor a rule modification restricts that
        /// <c>ActorMayPerform</c> does not yet thread through <c>ActionUnavailability</c>, or - milestone
        /// 8.29f, and the most common of the four in ordinary play - the turn's other printed action
        /// already under way while this one's sibling resolves. No fact published to presentation tells
        /// these apart, so naming one specifically would be a guess; the generic word is what the
        /// milestone's own no-invented-reasons rule asks for instead. This is a recorded gap, not an
        /// oversight - see the milestone's own notes for the Core-side extension it would take
        /// (a typed reason per clause, the way milestone 8.35l added one for the Skill clause alone).
        /// </para>
        /// </summary>
        private static string FormatUnavailabilityReason(IMatchReadModel projection, ActionOffer offer)
        {
            if (offer.Unavailability.Kind == ActionUnavailabilityKind.BlockedByRuleModification)
            {
                return ActionUnavailabilityLabels.BlockedBy(
                    HeroDisplayName(projection.Setup.Heroes, offer.Unavailability.BlockingHeroId),
                    GetCardDisplayName(projection, offer.Unavailability.BlockingCardInstanceId));
            }

            return ActionUnavailabilityLabels.Generic;
        }

        private static string FormatPublicDecisionWait(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var owner = projection.DecisionOwner;
            if (!viewerHeroId.IsDefined || !owner.IsDefined) return null;
            var viewer = projection.Setup.Heroes.SingleOrDefault(hero => hero.HeroId.Equals(viewerHeroId));
            var ownsDecision = owner.IsTeamOwned
                ? viewer != null && viewer.TeamSide == owner.TeamSide
                : owner.HeroIds.Any(hero => hero.Equals(viewerHeroId));
            // An enabled responder keeps their detailed actionable prompt. An in-flight reader
            // may have no private pending surface, but still knows this same public wait.
            if (ownsDecision && projection.IsInputEnabled) return null;
            var names = owner.IsTeamOwned
                ? MinionChoiceLabels.TeamLabel(owner.TeamSide).ToUpperInvariant()
                : string.Join(", ", owner.HeroIds.Select(hero =>
                    HeroDisplayName(projection.Setup.Heroes, hero).ToUpperInvariant()));
            return string.IsNullOrEmpty(names) ? null : NarrationLabels.HeroDeciding(
                names, NarrationLabels.PublicDecisionActivity(owner.FamilyId));
        }

        /// <summary>
        /// Milestone 8.35n (the FIFTH amendment, playtest finding: "ACTING ON INITIATIVE X is not
        /// enough ... hero name, who is acting, not only initiative"). Every branch below reads as
        /// one of two shapes, and the shape is what the narration says rather than a choice made
        /// here: a decision <paramref name="projection"/> carries in full - defense, leveling,
        /// respawn, movement, an attack target, tied Initiative, a Minion Battle removal, a
        /// correction, a removal prevention, an effect choice, and the viewer's own acting turn or
        /// release - is the VIEWER'S OWN, because a seat projection publishes those fields only to
        /// the seat that owns the standing decision (the Director owns every family at once, so
        /// every branch below is honest for both compositions without a second read). Everything
        /// else this method says is a fact <see cref="IMatchReadModel"/> already publishes to EVERY
        /// viewer regardless of ownership - the acting hero's name (public, from
        /// <see cref="IMatchReadModel.SelectedActor"/>), the turn-release owner (public, unscoped by
        /// 8.35g's own decision), and who still owes a commitment or a team-agreement answer (both
        /// stated public on their own declarations).
        ///
        /// <para>Observers read the public DecisionOwner before ordinary phase/actor narration.
        /// Its family and awaited heroes are public even when the answer details are withheld.
        /// This changes no turn actor, exposes no candidates and persists no presentation state.</para>
        /// </summary>
        private static string FormatStatus(
            IMatchReadModel projection, HeroId viewerHeroId, out string statusDetail)
        {
            statusDetail = null;
            var agreement = projection.TeamAgreement;
            if (agreement?.Kind == TeamAgreementKind.Initiative)
            {
                return "TIED INITIATIVE — " + FormatAgreementWait(projection, agreement);
            }

            if (agreement?.Kind == TeamAgreementKind.MinionBattle)
            {
                statusDetail = $"{agreement.SelectedUnits.Count} OF " +
                    $"{agreement.RequiredSelectionCount} MINIONS SELECTED";
                return "MINION BATTLE PROPOSAL — " + FormatAgreementWait(projection, agreement);
            }

            // Milestone 8.30b, and these two branches are what the user-approved amendment found.
            //
            // Moving this sentence into the top status bar made it the standing report of what the
            // match is doing, and two of the ten declared pending families had never been named here.
            // Both stand while the round phase reads EndOfRound, so both were reaching a player as
            // "ROUND 1 COMPLETE - END OF ROUND" while the match was in fact waiting on an answer - the
            // same shape milestone 8.20d found for the effect choice, which reached the table as
            // "choose an action or End Turn". They are read before the phase for exactly that reason:
            // a question the match is asking now outranks the phase it is asking it in.
            //
            // Neither is added to `IsAwaitingAnAnsweredDecision`. That list decides which offers are
            // suppressed, and these two already suppress their own - the end-of-round advance is
            // withheld against both by name - so moving them onto it would change what is offered in
            // order to fix what is said. `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation`
            // compares this method against the declared inventory instead, which is the comparison
            // that was missing.
            // Milestone 8.30d rewords every sentence here into the player's language - who acts,
            // in game words, no engine vocabulary - without touching which family each branch
            // reads: `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation` still compares this
            // method's compiled reads against the declared inventory, and the branches are the
            // same branches.
            // Milestone 8.30c. An ended match is a terminal player state, not another wait. Before
            // this branch it reached the fallback as "Waiting for the match to continue", which
            // drew a live-sounding prompt after every control had correctly disappeared. The outcome
            // is public and identical in all six seat projections, so the one shared derivation names
            // the winning side and the rulebook victory sentence for every perspective.
            if (projection.Phase == RoundResolutionPhase.GameEnded)
            {
                var outcome = projection.Outcome;

                // Milestone 8.39g. A solo ending has no winning TEAM to look up, and asking for one
                // would throw rather than read oddly. It names the hero instead, which is the whole
                // of what "you alone win the game" awards.
                if (outcome.HasSoloWinner)
                {
                    // Milestone 8.39j names them the way every other line on this panel does. The
                    // raw seat identity read `BLUE-TEST-HERO WINS ALONE` on the one board that can
                    // reach this sentence, which is a seat id rather than a hero.
                    return HeroDisplayName(projection.Setup.Heroes, outcome.WinningHeroId)
                            .ToUpperInvariant() + " WINS ALONE: " +
                        FormatVictoryCause(outcome.Cause) + ".";
                }

                var side = projection.Participants.GetTeam(outcome.WinningTeamId).Side;
                return MinionChoiceLabels.TeamLabel(side) + " wins: " +
                    FormatVictoryCause(outcome.Cause) + ".";
            }

            var publicWait = FormatPublicDecisionWait(projection, viewerHeroId);
            if (publicWait != null) return publicWait;

            var minionBattle = projection.PendingMinionBattleChoice;
            if (minionBattle != null)
            {
                // The deciding side is kept in the demoted detail rather than the headline: a
                // seat viewer's own screen already knows whose decision "YOUR" is, and the
                // Director - who answers for both sides at once - is exactly who still needs it
                // named.
                statusDetail = $"{MinionChoiceLabels.TeamLabel(minionBattle.ChoosingSide)} — " +
                    $"{minionBattle.SelectedUnits.Count} CHOSEN";
                return NarrationLabels.Own(
                    "MINION BATTLE",
                    $"REMOVE {minionBattle.RequiredRemovalCount} " +
                    (minionBattle.RequiredRemovalCount == 1 ? "MINION" : "MINIONS"));
            }

            var transitionStep = projection.PendingSystemTransitionStep;
            if (transitionStep != null)
            {
                return NarrationLabels.Own("LEVEL UP", "CHOOSE HOW TO PROCEED");
            }

            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                return $"Round {projection.RoundIndex} complete.";
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var stillOwed = CommitmentsStillOwed(projection);
                var viewerOwes = viewerHeroId.IsDefined
                    ? stillOwed.Contains(viewerHeroId)
                    : stillOwed.Count > 0;
                var names = stillOwed
                    .Select(heroId => HeroDisplayName(projection.Setup.Heroes, heroId))
                    .ToArray();
                return viewerOwes
                    ? NarrationLabels.Own("COMMITMENT", "CHOOSE A CARD, OR PASS")
                    : names.Length > 0
                        ? "COMMITTING — " + NarrationLabels.WaitingOn(names)
                        : $"Round {projection.RoundIndex}, Turn {projection.TurnIndex}: " +
                            "players are choosing their cards.";
            }

            if (projection.PendingInitiativeChoice != null)
            {
                return NarrationLabels.Own("INITIATIVE", "CHOOSE WHO ACTS FIRST");
            }

            if (projection.PendingDefenseChoice != null)
            {
                statusDetail = $"ATTACK {projection.PendingDefenseChoice.AttackTotal}";
                return NarrationLabels.Own("DEFENSE", "CHOOSE A CARD, OR DECLINE");
            }

            // Milestone 8.20d: the seventh pending family, and the one that had never been here.
            //
            // It is read directly after the Defense response because that is the order the region
            // itself draws in: a standing Defense response is the whole of the contextual region and
            // returns before any effect control is built, so the status must say Defense there; below
            // it the decision has its own labelled section, and the four families under this branch
            // share the action row with it.
            // Milestone 8.21: the eighth pending family, read here for the reason the seventh is read
            // where it is - this is the order the region draws in. A standing removal prevention is
            // the whole of the contextual region: the Defeat it stands inside has stopped, so no
            // effect question of its own can be open beneath it, and everything below this branch
            // belongs to a turn that is currently accepting nothing.
            var removalPrevention = projection.PendingRemovalPrevention;
            if (removalPrevention != null)
            {
                return NarrationLabels.Own(
                    RemovalPreventionPromptLabels.SectionLabel,
                    RemovalPreventionPromptLabels.StatusLine(removalPrevention));
            }

            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                var subject = EffectChoicePromptLabels.SubjectOf(
                    effectChoice, projection.PendingEffectChoicePreview);

                // Milestone 8.37. A pending choice reaches a seat's projection whether or not that
                // seat owns it - AUTHORIZATION FILTER is not PROJECTION FILTER - and until Tali the
                // two were the same in practice, because every effect choice belonged to the hero
                // playing the card. Commune with Spirits breaks that: she plays it and an ALLY names
                // the colour. Addressing the acting seat with "YOUR NAME A COLOUR" would name a
                // control it does not have, so a decision owned by somebody else is narrated as
                // theirs.
                if (viewerHeroId.IsDefined && effectChoice.ChooserId.IsDefined &&
                    !effectChoice.ChooserId.Equals(viewerHeroId))
                {
                    var chooser = projection.Setup.Heroes.SingleOrDefault(
                        candidate => candidate.HeroId.Equals(effectChoice.ChooserId));
                    return NarrationLabels.HeroDeciding(
                        chooser?.DisplayName ?? effectChoice.ChooserId.Value,
                        EffectChoicePromptLabels.SectionLabel(subject));
                }

                return NarrationLabels.Own(
                    EffectChoicePromptLabels.SectionLabel(subject),
                    EffectChoicePromptLabels.StatusLine(effectChoice, subject, projection));
            }

            if (projection.PendingHeroRespawnChoice != null)
            {
                return NarrationLabels.Own(
                    "RESPAWN", "CHOOSE A SPACE, OR RESOLVE WITHOUT ACTING");
            }

            if (projection.PendingAttackTargetChoice != null)
            {
                return NarrationLabels.Own("ATTACK", "CHOOSE A TARGET");
            }

            if (projection.PendingMovementChoice != null)
            {
                var remaining = projection.PendingMovementChoice.RemainingAllowance;
                statusDetail = $"{remaining} " + (remaining == 1 ? "SPACE LEFT" : "SPACES LEFT");
                return NarrationLabels.Own("MOVEMENT", "CHOOSE A DESTINATION");
            }

            if (projection.PendingDisplacedMinionCorrection != null)
            {
                return NarrationLabels.Own("CORRECTION", "PLACE THE DISPLACED MINION");
            }

            // Milestone 8.35g: the eleventh pending family, and the last decision branch because the
            // pause is withheld while anything else stands - so anything above it that is standing
            // is the truer sentence. It has to be here at all because the acknowledgement that
            // raised the pause is what empties the selection: without this branch a turn waiting on
            // its own player fell through to "Waiting for the match to continue", the live-sounding
            // fallback milestone 8.30c removed for the ended match and 8.20d for the effect choice.
            // It names the side rather than the pressing seat, because one derivation serves every
            // viewer and the other five are waiting on exactly this.
            // Milestone 8.35n: `ActorId` is unscoped by 8.35g's own deliberate decision, so unlike
            // every branch above this one reads the SAME object on every viewer's projection - the
            // comparison below is what decides which of the two vocabularies applies, not the field.
            var turnRelease = projection.PendingTurnRelease;
            if (turnRelease != null)
            {
                return !viewerHeroId.IsDefined || turnRelease.ActorId.Equals(viewerHeroId)
                    ? NarrationLabels.Own("TURN", "RELEASE TO CONTINUE")
                    : NarrationLabels.HeroAwaitingRelease(
                        HeroDisplayName(projection.Setup.Heroes, turnRelease.ActorId));
            }

            if (projection.SelectedActor.IsDefined)
            {
                var initiative = projection.SelectedActor.InitiativeTotal;

                // Milestone 8.72. A delegated turn is the one turn in this build whose acting piece
                // and whose deciding player are two different heroes, so neither of the two sentences
                // below can tell the truth about it on its own: "YOUR TURN" is said to a player whose
                // turn it is not, and "X - ACTING" names the piece while withholding the only fact
                // that explains why that player has nothing to press. The line names BOTH heroes on
                // every seat at the table, and differs only in what it then tells this viewer to do.
                // Both halves are derived from p.4 open zones, so no seat is told anything it could
                // not already read off its own frame (milestone 8.71b).
                var delegation = DelegatedActionPresentation.Standing(projection);
                if (delegation.IsDefined)
                {
                    statusDetail = $"INITIATIVE {initiative}";
                    return NarrationLabels.HeroDecidingFor(
                        HeroDisplayName(projection.Setup.Heroes, delegation.DecidingHeroId),
                        HeroDisplayName(projection.Setup.Heroes, delegation.DelegatedHeroId),
                        !viewerHeroId.IsDefined || delegation.DecidingHeroId.Equals(viewerHeroId)
                            ? DelegatedActionPresentation.DecidingActivity
                            : DelegatedActionPresentation.WaitingActivity);
                }

                if (AwaitedInputQuery.MayTakeATurnAction(projection))
                {
                    statusDetail = $"INITIATIVE {initiative}";
                    return NarrationLabels.Own("TURN", "CHOOSE AN ACTION, OR END YOUR TURN");
                }

                return NarrationLabels.HeroActing(
                    HeroDisplayName(projection.Setup.Heroes, projection.SelectedActor.ActorId),
                    initiative);
            }

            return "Waiting for the match to continue.";
        }

        /// <summary>
        /// The heroes still owed a commitment - public, per <see cref="IMatchReadModel.Commitments"/>'s
        /// own declaration, the same fact <see cref="AwaitedInputQuery"/> reads at viewer and hero
        /// scope. Read directly here rather than through that class because the narration also needs
        /// the full set of names, not a single hero's yes/no answer.
        /// </summary>
        private static IReadOnlyList<HeroId> CommitmentsStillOwed(IMatchReadModel projection) =>
            Array.AsReadOnly((projection.Commitments ?? Array.Empty<MatchCommitmentProjection>())
                .Where(commitment => commitment.Status == TurnCardCommitmentStatus.AwaitingResponse)
                .Select(commitment => commitment.HeroId)
                .ToArray());

        /// <summary>
        /// The heroes on the standing team agreement's own participant list who have not yet given
        /// their part - a stated Initiative preference, or an explicit Minion Battle agreement, the
        /// same per-kind reading <c>AwaitedInputQuery.OwesAnAgreementAnswer</c> and
        /// <c>TeamAgreementHudPresentation.Create</c> both already use.
        /// </summary>
        /// <summary>
        /// Milestone 8.35z4, and the defect the first live internet playtest found within minutes.
        ///
        /// <para>A standing team agreement whose participants have ALL answered is an ordinary,
        /// legal, transient state - the moment between the last statement landing and the authority
        /// resolving what it unblocked - and every seat refreshes through it. Both branches above
        /// handed that empty set straight to <see cref="NarrationLabels.WaitingOn"/>, which refuses
        /// it by design, so the round panel threw on EVERY refresh from that moment on. Milestone
        /// 8.35h's panel isolation then did its job exactly as designed and contained the throw to
        /// this one panel - which is what turned a crash into something far quieter and worse: the
        /// panel froze at its last good text (the playtest's immortal "YOUR TURN — RELEASE TO
        /// CONTINUE") and everything else riding the same refresh, the kinetic cue emission
        /// included, was starved for the rest of the match.
        /// </para>
        ///
        /// <para>The fix is here, at the caller, and deliberately not in the vocabulary: an empty
        /// owed-set is a real state that deserves a real sentence, not a constructed empty list and
        /// not a relaxed guard. <see cref="NarrationLabels.WaitingOn"/> stays defensive, so the next
        /// caller that forgets the empty case is still caught rather than quietly printing
        /// "WAITING ON: ".</para>
        /// </summary>
        private static string FormatAgreementWait(
            IMatchReadModel projection, TeamAgreementProjection agreement)
        {
            var stillOwed = AgreementStillOwed(projection, agreement);
            return stillOwed.Count == 0
                ? NarrationLabels.EveryAnswerIn
                : NarrationLabels.WaitingOn(stillOwed);
        }

        private static IReadOnlyList<string> AgreementStillOwed(
            IMatchReadModel projection, TeamAgreementProjection agreement) =>
            Array.AsReadOnly(agreement.Participants
                .Where(participant => agreement.Kind == TeamAgreementKind.Initiative
                    ? !participant.HasStatement
                    : !participant.Agreed)
                .Select(participant =>
                    HeroDisplayName(projection.Setup.Heroes, participant.HeroId))
                .ToArray());

        /// <summary>
        /// The status line's own cause sentence.
        ///
        /// <para><b>Milestone 8.53 moved the words to <see cref="VictoryCauseLabels"/> and kept this
        /// method.</b> The victory screen prints the same sentence under its banner, and two copies of
        /// these four strings would let the screen and the status line give a player two different
        /// answers to "why did the match end". The method itself stays because
        /// <c>SeatScopedMatchHudTests.EveryVictoryCause_HasAWordedSentenceTheStatusLineCanSay</c>
        /// reflects on it by name to walk the enum - the guard milestone 8.39j added after a cause
        /// shipped with no sentence at all and the HUD threw at the moment somebody won.</para>
        /// </summary>
        private static string FormatVictoryCause(MatchVictoryCause cause) =>
            VictoryCauseLabels.Describe(cause);
    }
}

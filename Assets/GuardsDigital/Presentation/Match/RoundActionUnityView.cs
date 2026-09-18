using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{

    /// <summary>
    /// The one shared contextual region, and the tray's own action row.
    ///
    /// Every decision the match can ask is drawn here: commitment Initiative choices, the selected card's
    /// action offers, Defense responses, effect modes and targets, Clear, Hero Respawn, and - milestone
    /// 7.8a - a paused system transition. They share one region because the match asks exactly one thing
    /// at a time, and because a second region would be a second place a player has to look for the
    /// question.
    /// </summary>
    public sealed class RoundActionUnityView : IRoundActionView
    {
        public const float OptionalStepActionWidth = 240f;
        public const float OptionalStepActionHeight = 36f;
        public const float OptionalStepActionGap = 44f;
        public const float TeamAgreementHeight = 78f;

        private const double PreviewDismissDelaySeconds = 0.12d;

        /// <summary>
        /// How wide every transition option is. Without one width the longest label sets the width of
        /// its own row, and p.15's options carry two card names each, so six of them came out as three
        /// ragged rows that did not line up with each other.
        ///
        /// Wide enough that a two-card label stays on one line: at 336 every option wrapped, which made
        /// each one half as tall again and pushed six of them past the tray's height bound into a
        /// scrollbar. Two of these plus the gutter still fit the tray's width.
        /// </summary>
        private const float OptionWidth = 430f;

        /// <summary>
        /// The gap between p.15's two candidates for one source card. Milestone 8.30j removes the
        /// option and pair frames, so the pair width is now only two standard card faces and this gap.
        /// </summary>
        private const float UpgradePairGap = 4f;
        public const float UpgradePairWidth =
            (CardFootprint.Width * 2f) + UpgradePairGap +
            (CardFootprint.ExteriorOutlineWidth * 4f);

        /// <summary>
        /// The printed item sits at the centre of the card's bottom band. The projected sibling item
        /// covers only that conflicting symbol and remains wholly inside the standard face.
        /// </summary>
        private const float UpgradeItemOverlayHeight = 46f;

        /// <summary>
        /// Readable comparison faces; hovering reveals the full standard face without a command.
        /// </summary>
        private const float LevelUpHandPreviewScale = 0.85f;
        public const float LevelUpHandPreviewHeight = CardFootprint.Height * LevelUpHandPreviewScale;

        private const float LevelUpHandPreviewGap = 6f;
        private const float UpgradeItemOverlayTop =
            CardFootprint.Height - UpgradeItemOverlayHeight;
        private static readonly Color UpgradeItemOverlayBackground =
            new Color(0.216f, 0.216f, 0.216f, 1f);

        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);

        private readonly VisualElement status;
        private readonly VisualElement rows;
        private readonly VisualElement preview;
        private readonly VisualElement declineDefenseHost;
        private readonly VisualElement optionalStepActionsHost;
        private readonly VisualElement defenseCardHoverPreview;
        private readonly VisualElement defenseDragProxy;
        private readonly CardCommitmentDragState defenseDragState = new CardCommitmentDragState();
        private VisualElement defenseDragCapture;
        private VisualElement draggedDefenseCard;
        private Func<Vector2, bool> isMapDropPoint;

        /// <summary>
        /// End Turn's own surface. Milestone 8.30b's user-approved amendment took the control out of
        /// the shared action row and gave it a larger control of its own. Milestone 8.30l moves that
        /// control into the status bar's top-right slot; the renderer moves this element out of the contextual region onto the panel and
        /// places it, exactly as it already does with this view's Defense preview.
        ///
        /// <para>It stays a child of <see cref="Root"/> until something moves it, so a fixture that
        /// renders this view alone still finds the control where every other one it draws is.</para>
        /// </summary>
        private readonly VisualElement endTurnHost;
        private readonly VisualElement seatUndoHost;

        /// <summary>
        /// Milestone 8.35z8 (SIXTH amendment part b, "old M7"). The acknowledge-to-continue notice
        /// for a resolution the recorded <c>ImpossibleMandatory</c> fact says applied nothing mid-
        /// resolution - hosted on the contextual region itself, an absolute overlay so it can stand
        /// over whatever the region is otherwise drawing without displacing it, exactly as the
        /// Defense breakdown preview already does on this same view.
        /// </summary>
        private readonly VisualElement fizzleNoticeHost;
        private readonly Label fizzleNoticeLabel;

        /// <summary>
        /// The Defense-card controls this rebuild drew. Milestone 8.11b, for milestone 8.8a's reason:
        /// a driver with no pointer has to reach an offer the way its own control does, and an offer
        /// this rebuild did not draw must reach nothing.
        /// </summary>
        private readonly List<KeyValuePair<TypedInvocationViewModel, VisualElement>>
            defenseCardControls =
                new List<KeyValuePair<TypedInvocationViewModel, VisualElement>>();

        /// <summary>
        /// The upgrade-option controls this rebuild drew, in their rendered and focus order. The list
        /// refuses stale option forwarding and lets pointer or keyboard focus strengthen exactly one
        /// centred item box without painting an enclosing option frame.
        /// </summary>
        private readonly List<KeyValuePair<SystemTransitionOptionViewModel, VisualElement>>
            upgradeOptionControls =
                new List<KeyValuePair<SystemTransitionOptionViewModel, VisualElement>>();

        private DefenseBreakdownViewModel activeDefensePreview;
        private Rect defensePreviewAnchor;
        private double dismissPreviewAt = -1d;

        public RoundActionUnityView()
        {
            Root = new VisualElement { name = "gd-context" };
            Root.style.flexDirection = FlexDirection.Column;
            // Milestone 8.30b: the narration is named, because the renderer now moves it into the top
            // status bar and a check has to be able to say where it ended up.
            status = HudElements.Row(8f);
            status.name = "gd-narration";
            status.style.marginBottom = 6f;
            rows = HudElements.Column(6f);
            endTurnHost = new VisualElement { name = "gd-end-turn" };
            endTurnHost.style.flexDirection = FlexDirection.Column;
            endTurnHost.style.alignItems = Align.Stretch;
            endTurnHost.style.justifyContent = Justify.Center;
            endTurnHost.style.display = DisplayStyle.None;
            seatUndoHost = new VisualElement { name = "gd-seat-undo" };
            seatUndoHost.style.flexDirection = FlexDirection.Column;
            seatUndoHost.style.alignItems = Align.Stretch;
            seatUndoHost.style.justifyContent = Justify.Center;
            seatUndoHost.style.display = DisplayStyle.None;
            Root.Add(status);
            Root.Add(rows);
            Root.Add(endTurnHost);
            Root.Add(seatUndoHost);
            preview = HudElements.Panel(DefensePreviewBackgroundColor);
            preview.name = "gd-defense-preview";
            preview.style.position = Position.Absolute;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            HudElements.Padding(preview, 10f);
            HudElements.Border(preview, MatchHudTheme.PanelBorder, 1f, 4f);
            declineDefenseHost = new VisualElement { name = "gd-decline-defense-host" };
            declineDefenseHost.style.flexDirection = FlexDirection.Column;
            declineDefenseHost.style.alignItems = Align.Stretch;
            declineDefenseHost.style.justifyContent = Justify.FlexStart;
            declineDefenseHost.style.display = DisplayStyle.None;
            Root.Add(declineDefenseHost);
            optionalStepActionsHost = new VisualElement { name = "gd-optional-step-actions" };
            optionalStepActionsHost.pickingMode = PickingMode.Ignore;
            optionalStepActionsHost.style.flexDirection = FlexDirection.Row;
            optionalStepActionsHost.style.alignItems = Align.Stretch;
            optionalStepActionsHost.style.justifyContent = Justify.FlexStart;
            optionalStepActionsHost.style.overflow = Overflow.Visible;
            optionalStepActionsHost.style.display = DisplayStyle.None;
            Root.Add(optionalStepActionsHost);

            fizzleNoticeHost = new VisualElement { name = "gd-fizzle-notice" };
            fizzleNoticeHost.style.position = Position.Absolute;
            fizzleNoticeHost.style.left = 0f;
            fizzleNoticeHost.style.top = 0f;
            fizzleNoticeHost.style.right = 0f;
            fizzleNoticeHost.style.bottom = 0f;
            fizzleNoticeHost.style.display = DisplayStyle.None;
            fizzleNoticeHost.style.alignItems = Align.Center;
            fizzleNoticeHost.style.justifyContent = Justify.Center;
            fizzleNoticeHost.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            var fizzleNoticePanel = HudElements.Panel(MatchHudTheme.PanelBackground);
            fizzleNoticePanel.name = "gd-fizzle-notice-panel";
            fizzleNoticePanel.style.flexDirection = FlexDirection.Column;
            fizzleNoticePanel.style.alignItems = Align.Center;
            fizzleNoticePanel.style.maxWidth = 420f;
            HudElements.Padding(fizzleNoticePanel, 16f);
            HudElements.Border(fizzleNoticePanel, MatchHudTheme.PanelBorder, 1f, MatchHudTheme.ButtonCornerRadius);
            fizzleNoticeLabel = HudElements.Text(
                string.Empty, 16f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            fizzleNoticeLabel.name = "gd-fizzle-notice-label";
            fizzleNoticeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            fizzleNoticePanel.Add(fizzleNoticeLabel);
            var fizzleNoticeOk = HudElements.Control("OK", () => FizzleNoticeAcknowledged?.Invoke(), 32f);
            fizzleNoticeOk.name = "gd-fizzle-notice-ok";
            fizzleNoticeOk.style.marginTop = 12f;
            fizzleNoticeOk.style.minWidth = 96f;
            fizzleNoticePanel.Add(fizzleNoticeOk);
            fizzleNoticeHost.Add(fizzleNoticePanel);
            // Milestone 8.35z9. Built once and kept DETACHED until a notice actually stands. It is
            // not added to Root here, because a hidden control is still a control: this HUD's own
            // prompt-reachability rules enumerate the buttons under the round view and refuse any
            // that answers nothing, and the first complete Edit Mode run after this notice shipped
            // failed four of them on this one OK button - three reachability cases and a Defense
            // label sweep - while it sat in the tree behind `display: none`. Attaching on demand is
            // both the fix and the honest reading: when no step has fizzled there is no notice.

            defenseCardHoverPreview = new VisualElement { name = "gd-defense-card-hover-preview" };
            defenseCardHoverPreview.style.position = Position.Absolute;
            defenseCardHoverPreview.style.display = DisplayStyle.None;
            defenseCardHoverPreview.pickingMode = PickingMode.Ignore;
            defenseDragProxy = new VisualElement { name = "gd-defense-card-drag-proxy" };
            defenseDragProxy.style.position = Position.Absolute;
            defenseDragProxy.style.display = DisplayStyle.None;
            defenseDragProxy.style.width = CardFootprint.Width;
            defenseDragProxy.style.height = CardFootprint.Height;
            defenseDragProxy.pickingMode = PickingMode.Ignore;
        }

        public VisualElement Root { get; }

        /// <summary>The Defense breakdown popup, placed on the HUD root so it can leave the tray.</summary>
        public VisualElement PreviewRoot => preview;

        /// <summary>The Defense decline banner hosted at the facedown confirmation rectangle.</summary>
        public VisualElement DeclineDefenseRoot => declineDefenseHost;

        /// <summary>The two optional-step answers hosted above the retained ordinary hand.</summary>
        public VisualElement OptionalStepActionsRoot => optionalStepActionsHost;

        /// <summary>
        /// Milestone 8.35z8. The fizzle notice's own element, hosted on the contextual region so it
        /// reads as belonging to it - the same idiom the game already uses to pause for a decision.
        /// </summary>
        public VisualElement FizzleNoticeRoot => fizzleNoticeHost;

        /// <summary>The viewer's OK, dismissing the standing fizzle notice.</summary>
        public event Action FizzleNoticeAcknowledged;

        /// <summary>Whether the fizzle notice is currently shown, for a driver with no pointer.</summary>
        public bool HasFizzleNotice => fizzleNoticeHost.style.display == DisplayStyle.Flex;

        /// <summary>
        /// Milestone 8.35z8. Drawn independently of <see cref="Render"/>: the recorded
        /// <c>ImpossibleMandatory</c> fact this notice answers is not one of
        /// <see cref="RoundActionViewModel"/>'s own pending-decision families (Core does not pause
        /// on it - the resolution has already stopped by the time the match log names it), so this
        /// is a second, additive entry point rather than a new field threaded through the model
        /// every other pause is built from.
        /// </summary>
        public void RenderFizzleNotice(FizzleNoticeViewModel model)
        {
            if (!model.HasNotice)
            {
                fizzleNoticeHost.style.display = DisplayStyle.None;
                fizzleNoticeHost.RemoveFromHierarchy();
                return;
            }

            fizzleNoticeLabel.text = model.Message;
            if (fizzleNoticeHost.parent == null)
            {
                Root.Add(fizzleNoticeHost);
            }

            fizzleNoticeHost.BringToFront();
            fizzleNoticeHost.style.display = DisplayStyle.Flex;
        }

        /// <summary>The ordinary hand-style complete face raised from an overlapped Defense card.</summary>
        public VisualElement DefenseCardHoverPreviewRoot => defenseCardHoverPreview;

        /// <summary>The pointer-transparent complete Defense card reparented to the HUD root.</summary>
        public VisualElement DefenseDragProxyRoot => defenseDragProxy;

        /// <summary>Uses the commitment drag's exact live board boundary for Defense-card drops.</summary>
        public void SetMapDropPredicate(Func<Vector2, bool> predicate)
        {
            isMapDropPoint = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// The narration element, so the renderer can host it in the top status bar. Milestone 8.30b.
        /// </summary>
        public VisualElement NarrationRoot => status;

        /// <summary>
        /// End Turn's own element, so the renderer can place it in the status bar's top-right slot.
        /// Milestones 8.30b and 8.30l. It is hidden whenever the projection offers no End Turn, which is every
        /// state in which the turn authority would refuse one.
        /// </summary>
        public VisualElement EndTurnRoot => endTurnHost;

        /// <summary>
        /// Milestone 8.51. How the last END TURN press reached this view - the report
        /// <see cref="EndTurnPressProvenance.Take"/> composed at the moment of the press, read by
        /// the runtime to write the player's own log line. Null until the first press.
        /// </summary>
        public string LastEndTurnPressReport { get; private set; }

        private readonly EndTurnPressProvenance endTurnPress = new EndTurnPressProvenance();
        private Button endTurnControl;

        /// <summary>
        /// Milestone 8.35f. The seat undo square's own element, so the renderer can place it
        /// immediately to the left of End Turn on the status strip's top edge. Hidden in every state
        /// where the host has not published this seat a rewind it may take.
        /// </summary>
        public VisualElement SeatUndoRoot => seatUndoHost;

        /// <summary>
        /// Whether the drawn End Turn control carries its sole-action outline - ending the turn is the
        /// only offer left. Milestone 8.30d, exposed as a fact a test can read rather than a colour
        /// only a screenshot carries; false whenever the control is hidden.
        /// </summary>
        public bool EndTurnShinesFully { get; private set; }

        public RoundActionViewModel Model { get; private set; }
        public DefenseBreakdownViewModel ActiveDefensePreview => activeDefensePreview;
        public Rect DefensePreviewAnchor => defensePreviewAnchor;

        public event Action<TypedInvocationViewModel> InvocationSubmitted;

        /// <summary>
        /// Milestone 8.35f. The seat asked to step its own turn back one command. It carries no
        /// payload because there is none to carry.
        /// </summary>
        public event Action SeatUndoRequested;

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment's REVISED SEMANTICS). END TURN's own press, raised
        /// unconditionally by the drawn control whenever it is enabled - a direct completion, or a
        /// press that must first discard the seat's one open Attack-target selection. It carries no
        /// payload for the same reason <see cref="SeatUndoRequested"/> carries none: which of those
        /// the press composes is the presenter's decision from the live projection, never a command
        /// this view assembles.
        /// </summary>
        public event Action EndTurnRequested;
        public event Action<TeamAgreementChoiceViewModel> AgreementSubmitted;
        public event Action<HeroId> AgreementHeroHighlighted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;

        /// <summary>Milestone 6.5c: the named target control that was activated.</summary>
        public event Action<EffectChoiceTargetViewModel> TargetSubmitted;

        /// <summary>
        /// The target control the pointer or keyboard focus is on, or null when none is. The board
        /// reads this to mark that object and light where the projected operation would leave it.
        /// </summary>
        public event Action<EffectChoiceTargetViewModel> TargetHighlighted;
        public event Action<HeroRespawnOptionViewModel> RespawnHighlighted;

        /// <summary>Milestone 6.3a: the Clear replacement whose control was activated.</summary>
        public event Action<ClearOfferViewModel> ClearSelected;

        public event Action ClearConfirmed;
        public event Action ClearCancelled;

        public EffectChoiceTargetViewModel HighlightedTarget { get; private set; }
        public HeroRespawnOptionViewModel HighlightedRespawn { get; private set; }

        /// <summary>
        /// The upgrade option the pointer or keyboard focus is on, or null when none is.
        ///
        /// Milestone 8.8a. It is view-local and drives nothing outside this region - p.15's choice is
        /// about two cards rather than about a space, so there is no board cue to raise - and it exists
        /// so the selection a player sees on the card is a fact a test can read rather than a colour
        /// only a screenshot carries.
        /// </summary>
        public SystemTransitionOptionViewModel HighlightedUpgradeOption { get; private set; }

        /// <summary>
        /// Whether the current model carries a decision that must be answered before anything else can
        /// happen. The tray is hidden while one stands, which is how "at most one usable interaction
        /// context" is kept structurally rather than by discipline.
        /// </summary>
        /// <summary>
        /// Whether the region is drawing p.15's six card-faced upgrade choices. The renderer uses this
        /// to select the contained six-card overlay and short-view vertical fallback while keeping
        /// every standing HUD and board-safe rectangle on its ordinary geometry.
        /// </summary>
        public bool HasUpgradeComparisonBand =>
            Model?.SystemTransitionPrompt != null &&
            Model.SystemTransitionPrompt.Options.All(option => option.Card != null);

        public bool HasOptionalStepDecision =>
            Model?.EffectChoicePrompt?.Subject == EffectChoiceSubject.OptionalStep &&
            Model.EffectChoices.Count > 0;

        public bool HasTeamAgreementDecision =>
            Model?.TeamAgreement?.Kind == TeamAgreementKind.Initiative;

        public bool RetainsOrdinaryHand => HasOptionalStepDecision || HasTeamAgreementDecision;

        public bool HasExclusiveDecision =>
            Model != null &&
            (Model.SystemTransitionPrompt != null ||
             Model.RemovalPreventionPrompt != null ||
             Model.DefenseChoices.Count > 0 ||
             Model.InitiativeChoices.Count > 0 ||
             Model.TeamAgreement != null ||
             Model.EffectChoices.Count > 0 ||
             Model.EffectChoiceTargets.Count > 0 ||
             Model.UnavailableEffectChoices.Count > 0 ||
             Model.HeroRespawnOptions.Count > 0 ||
             Model.ResolveWithoutRespawn != null ||
             Model.ClearSelection != null);

        public void Render(RoundActionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            if (activeDefensePreview != null &&
                (activeDefensePreview.Version != Model.Version ||
                 !Model.DefenseChoices.Any(value =>
                     ReferenceEquals(value.DefenseBreakdown, activeDefensePreview))))
            {
                ClearDefensePreview();
            }

            // A highlight belongs to the model it was read from. A rebuilt model drops it, so the
            // board never keeps a cue for a control that is no longer offered.
            if (HighlightedTarget != null &&
                !Model.EffectChoiceTargets.Any(value => ReferenceEquals(value, HighlightedTarget)))
            {
                SetTargetHighlight(null);
            }

            if (HighlightedRespawn != null &&
                !Model.HeroRespawnOptions.Any(value => ReferenceEquals(value, HighlightedRespawn)))
            {
                SetRespawnHighlight(null);
            }

            Rebuild();
        }

        /// <summary>Forwards one named target control, for a driver that has no pointer.</summary>
        public void ForwardTarget(EffectChoiceTargetViewModel target)
        {
            if (target != null)
            {
                TargetSubmitted?.Invoke(target);
            }
        }

        /// <summary>Reports a pointed-at target directly, for a driver that has no pointer.</summary>
        public void ForwardTargetHighlight(EffectChoiceTargetViewModel target) =>
            SetTargetHighlight(target);

        /// <summary>Reports a hovered or keyboard-focused respawn control without a pointer driver.</summary>
        public void ForwardRespawnHighlight(HeroRespawnOptionViewModel option) =>
            SetRespawnHighlight(option);

        /// <summary>
        /// Milestone 8.35f. Raises the seat's own rewind request the way
        /// <see cref="ForwardInvocation"/> raises an ordinary one: the drawn square calls it,
        /// and so can a driver or a fixture that has no live panel to press with.
        /// </summary>
        public void RequestSeatUndo() => SeatUndoRequested?.Invoke();

        /// <summary>
        /// Milestone 8.35v. Raises END TURN's own press the way <see cref="RequestSeatUndo"/> raises
        /// its rewind: the drawn control calls it unconditionally, and so can a driver or a fixture
        /// that has no live panel to press with.
        /// </summary>
        public void RequestEndTurn()
        {
            // Milestone 8.51: name the press before anything composes from it, so the runtime can
            // write the record beside the command. A driver reaching this directly reads as one.
            LastEndTurnPressReport = endTurnPress.Take(FocusedElementName(endTurnControl));
            EndTurnRequested?.Invoke();
        }

        private static string FocusedElementName(VisualElement control)
        {
            var focused = control?.focusController?.focusedElement as VisualElement;
            return focused == null
                ? null
                : string.IsNullOrEmpty(focused.name) ? focused.GetType().Name : focused.name;
        }

        public void ForwardInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                InvocationSubmitted?.Invoke(invocation);
            }
        }

        public void ForwardFastTravelSelection(FastTravelOfferViewModel selection)
        {
            if (selection != null)
            {
                FastTravelSelected?.Invoke(selection);
            }
        }

        private void SetTargetHighlight(EffectChoiceTargetViewModel target)
        {
            if (ReferenceEquals(HighlightedTarget, target))
            {
                return;
            }

            HighlightedTarget = target;
            TargetHighlighted?.Invoke(target);
        }

        private void SetRespawnHighlight(HeroRespawnOptionViewModel option)
        {
            if (ReferenceEquals(HighlightedRespawn, option))
            {
                return;
            }

            HighlightedRespawn = option;
            RespawnHighlighted?.Invoke(option);
        }

        private void Rebuild()
        {
            CancelDefenseCardDrag();
            ClearDefenseCardPeekPreview();
            declineDefenseHost.Clear();
            declineDefenseHost.style.display = DisplayStyle.None;
            optionalStepActionsHost.Clear();
            optionalStepActionsHost.style.flexDirection = FlexDirection.Row;
            optionalStepActionsHost.style.display = DisplayStyle.None;
            // Milestone 8.35z2: the team-agreement panel is the only one of this shared host's two
            // occupants that wants its own plate (the optional-step accept/decline pair is drawn as
            // two independently-shined `PewterBanner`s with no parent background). Reset here so a
            // rebuild that lands on the OTHER path never inherits the panel BuildTeamAgreement applied
            // last time this host was populated.
            optionalStepActionsHost.style.backgroundColor = Color.clear;
            HudElements.Padding(optionalStepActionsHost, 0f);
            HudElements.Border(optionalStepActionsHost, Color.clear, 0f, 0f);
            status.Clear();
            rows.Clear();
            // Milestone 8.30b: End Turn is rebuilt before the branches below, because four of them
            // return early and the control is no longer inside the region any of them draw. Every one
            // of those four is a state the turn authority refuses an End Turn in, so the model carries
            // none and this leaves the corner empty rather than leaving a stale control standing.
            RebuildEndTurn();
            RebuildSeatUndo();
            // Milestone 8.8a: the controls a rebuild is about to discard, and the selection that belonged
            // to them. A highlight belongs to the model it was read from, exactly as every other one here
            // does.
            upgradeOptionControls.Clear();
            defenseCardControls.Clear();
            HighlightedUpgradeOption = null;
            // Milestone 8.30d: the narration speaks in two voices. When the sentence is addressed
            // to the viewing seat - the model carries something to act on - the lane lights warm
            // with the shared shine accent; every other state stays calm and quiet. The word is
            // never carried by colour alone: the sentence itself says who acts.
            var warm = Model.Voice == NarrationVoice.YourMove;
            status.style.backgroundColor = warm
                ? MatchHudTheme.NarrationWarmBackground
                : Color.clear;
            var accent = new VisualElement { name = "gd-narration-accent" };
            accent.style.width = 5f;
            accent.style.alignSelf = Align.Stretch;
            accent.style.flexShrink = 0f;
            accent.style.backgroundColor = MatchHudTheme.ShineGlow;
            accent.style.display = warm ? DisplayStyle.Flex : DisplayStyle.None;
            status.Add(accent);
            // Milestone 8.35n (the FIFTH amendment, typography): "its important, make it look it".
            // Both voices are now bold, at two clearly different weights of size rather than one
            // fixed size that only changed colour, and the acting hero's name and activity carry
            // letter-spacing the way the rest of this HUD's important short labels already do
            // (`level.style.letterSpacing` above). A secondary fact - Initiative, an attack total, a
            // count still owed - is demoted to `Model.StatusDetail`, drawn smaller and quieter in the
            // same row rather than folded back into the headline sentence.
            var narration = HudElements.Text(
                Model.Status,
                warm ? MatchHudTheme.NarrationHeadlineSize : MatchHudTheme.NarrationCalmHeadlineSize,
                warm ? MatchHudTheme.NarrationWarmText : MatchHudTheme.MutedText,
                FontStyle.Bold);
            narration.name = "gd-narration-text";
            narration.style.marginLeft = warm ? 8f : 0f;
            narration.style.letterSpacing = warm ? 0.4f : 0.2f;
            status.Add(narration);
            if (!string.IsNullOrEmpty(Model.StatusDetail))
            {
                var detail = HudElements.Text(
                    Model.StatusDetail,
                    MatchHudTheme.NarrationDetailSize,
                    warm ? MatchHudTheme.NarrationWarmText : MatchHudTheme.MutedText,
                    FontStyle.Normal);
                detail.name = "gd-narration-detail";
                detail.style.marginLeft = 8f;
                detail.style.opacity = 0.72f;
                status.Add(detail);
            }

            if (HasTeamAgreementDecision)
            {
                BuildTeamAgreement(Model.TeamAgreement);
                HideEmptySections();
                return;
            }

            // Milestone 7.8a: a paused system transition is the whole of the region while it stands.
            // Nothing else the model carries can be acted on - the authority refuses every other
            // command - so drawing anything beside it would be drawing refusals.
            if (Model.SystemTransitionPrompt != null)
            {
                BuildTransitionPrompt(Model.SystemTransitionPrompt);
                return;
            }

            if (Model.ClearSelection != null)
            {
                BuildClearSelection(Model.ClearSelection);
                return;
            }

            if (Model.DefenseChoices.Count > 0)
            {
                BuildDefense();
                HideEmptySections();
                return;
            }

            // Milestone 8.21: a standing removal prevention is the whole of the region while it
            // stands, for the reason a paused system transition is. The Defeat it stopped has already
            // recorded its outcome and paid its reward; nothing else the model carries can be acted on
            // until the removal stage is answered, so drawing anything beside it would be drawing
            // refusals - and rulebook p.13's own ordering is why the tray is hidden with it.
            if (Model.RemovalPreventionPrompt != null)
            {
                BuildRemovalPrevention(Model.RemovalPreventionPrompt);
                HideEmptySections();
                return;
            }

            BuildActionRow();
            if (Model.FinishMovementInvocation != null)
            {
                // Stop the legal Movement without the End Turn composition declining its follow-up.
                var invocation = Model.FinishMovementInvocation;
                optionalStepActionsHost.style.display = DisplayStyle.Flex;
                var finish = new PewterBanner(() => ForwardInvocation(invocation),false,
                    DirectorHandsUnityView.CornerBannerFontSize,"gd-finish-movement-label",hoverOnlyShine:true)
                {
                    name = "gd-finish-movement",
                    text = invocation.Label,
                    tooltip = "Stop moving and resolve any after-movement choices."
                };
                finish.SetVisibleLabel(invocation.Label);
                finish.style.width = OptionalStepActionWidth;
                finish.style.height = OptionalStepActionHeight;
                finish.style.flexShrink = 0f;
                optionalStepActionsHost.Add(finish);
            }
            HideEmptySections();
        }

        private void BuildTeamAgreement(TeamAgreementHudViewModel agreement)
        {
            optionalStepActionsHost.Clear();
            optionalStepActionsHost.style.flexDirection = FlexDirection.Column;
            optionalStepActionsHost.style.alignItems = Align.Stretch;
            optionalStepActionsHost.style.display = DisplayStyle.Flex;
            // Milestone 8.35z2 (playtest finding: the panel drew text with no plate of its own, so it
            // read as broken rather than as a positioned surface). The same panel language every other
            // standing HUD surface uses - the Defense preview popup, the event log - rather than a new
            // treatment.
            optionalStepActionsHost.style.backgroundColor = MatchHudTheme.PanelBackground;
            HudElements.Border(optionalStepActionsHost, MatchHudTheme.PanelBorder, 1f, 4f);
            HudElements.Padding(optionalStepActionsHost, 6f);

            var heading = HudElements.Text(
                agreement.Heading, 13f, MatchHudTheme.PrimaryText, FontStyle.Bold);
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            heading.style.flexShrink = 0f;
            optionalStepActionsHost.Add(heading);

            var publicStatus = string.Join("  ·  ", agreement.Participants.Select(participant =>
                participant.Label + " " + participant.Status));
            var statusLine = HudElements.Text(
                publicStatus, 10f, MatchHudTheme.MutedText, FontStyle.Normal);
            statusLine.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLine.style.flexShrink = 0f;
            optionalStepActionsHost.Add(statusLine);

            if (agreement.Choices.Count > 0)
            {
                var buttons = HudElements.Row(6f);
                buttons.style.width = Length.Percent(100f);
                buttons.style.flexGrow = 1f;
                buttons.style.alignItems = Align.Stretch;
                foreach (var choice in agreement.Choices)
                {
                    var captured = choice;
                    var control = HudElements.Control(
                        choice.Label, () => AgreementSubmitted?.Invoke(captured));
                    control.name = "gd-initiative-preference";
                    control.style.flexGrow = 1f;
                    control.style.flexBasis = 0f;
                    control.style.minWidth = 0f;
                    HudElements.OnPointedAt(control, pointedAt =>
                        AgreementHeroHighlighted?.Invoke(pointedAt ? captured.HeroId : default));
                    buttons.Add(control);
                }

                optionalStepActionsHost.Add(buttons);
            }
            else if (!agreement.IsInputEnabled)
            {
                // Milestone 8.35z2. A participant's own statement is in flight -
                // `TeamAgreement.AuthorizedCommands` is deliberately empty until the host's correlated
                // projection returns - and drawing nothing here read as a vanished control rather than
                // an ordinary, momentary wait (the false-alarm finding an invited playtest recorded).
                var waiting = HudElements.Text(
                    ActionUnavailabilityLabels.WaitingForHost,
                    11f, MatchHudTheme.MutedText, FontStyle.Bold);
                waiting.name = "gd-agreement-waiting";
                waiting.style.unityTextAlign = TextAnchor.MiddleCenter;
                waiting.style.flexShrink = 0f;
                optionalStepActionsHost.Add(waiting);
            }
        }

        /// <summary>
        /// The standing removal-prevention decision, under a gutter that names what the price buys.
        ///
        /// <para>Milestone 8.21, and it is milestone 8.15a's direction applied to a second family:
        /// each payment is the whole printed face of the card that would pay, drawn by the tray's own
        /// standalone builder at the one fixed width, with the card's name kept as the control's hover
        /// and assistive reading. The decline is about no card and keeps its labelled control.</para>
        ///
        /// <para>The minion is not named here. It is cued on the board where it is standing, which is
        /// the one place a player can see which miniature the price would keep.</para>
        /// </summary>
        private void BuildRemovalPrevention(RemovalPreventionPromptViewModel prompt)
        {
            var section = Section(prompt.SectionLabel);
            foreach (var payment in prompt.Payments)
            {
                // The same branch the effect-choice section takes, and for the same reason: a choice
                // that is about a card is drawn as the card, and one carrying no face keeps its
                // labelled control rather than being drawn as an empty frame. A projection always
                // supplies the face, so the second arm is what a surface composed without one gets.
                if (payment.Card != null)
                {
                    section.Add(BuildEffectCardChoice(payment));
                }
                else
                {
                    AddInvocation(section, payment);
                }
            }

            AddInvocation(section, prompt.Decline);
        }

        /// <summary>
        /// A section whose gutter names a family with nothing in it is not drawn. During card
        /// commitment the match offers no action at all, and an `ACTIONS` label over an empty row says
        /// only that this surface expected something it did not get.
        /// </summary>
        private void HideEmptySections()
        {
            foreach (var section in rows.Children())
            {
                var contentCount = section.name == "gd-action-section" ||
                                   section.name == "gd-defense-section" ||
                                   section.name == "gd-effect-card-section"
                    ? section.childCount
                    : section.childCount - 1;
                section.style.display = contentCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>The gutter every section's family label reserves down this region's left edge.</summary>
        public const float SectionGutterWidth = 96f;

        /// <summary>The space between two controls in one section.</summary>
        public const float SectionGap = 6f;

        /// <summary>
        /// The tighter spacing used only by the unlabelled standing action row. Five ordinary offers
        /// are the widest routine set; spending less chrome between them keeps End Turn on that same
        /// line without abbreviating any rule-facing label.
        /// </summary>
        public const float ActionControlGap = 3f;

        public const float ActionControlHorizontalPadding = 6f;

        public const float ActionControlFontSize = 11f;

        private VisualElement Section(string gutter)
        {
            var section = HudElements.Row(SectionGap);
            var label = HudElements.Text(gutter, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            label.style.width = SectionGutterWidth;
            section.Add(label);
            rows.Add(section);
            return section;
        }

        private void BuildTransitionPrompt(SystemTransitionPromptViewModel prompt)
        {
            status.Add(HudElements.Text(prompt.PromptLabel, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            if (prompt.StatusLabel.Length > 0)
            {
                status.Add(HudElements.Text(prompt.StatusLabel, 11f, MatchHudTheme.MutedText));
            }

            // Milestone 8.16c: p.8's blocked-Spawn-point Place is answered by clicking one of the
            // standing highlighted spaces. The contextual region still explains who chooses and why,
            // but drawing a PLACE AT control here would restore the duplicate input surface this
            // milestone closes. Upgrade a Card continues below as card-faced controls.
            if (prompt.Kind == GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind
                    .LanePushRespawnPlace)
            {
                return;
            }

            // Milestone 8.29b: Level Up uses the whole width between the standing side rails. Its
            // family label joins the prompt line instead of consuming the ordinary 96-unit section
            // gutter, leaving six unchanged CardFootprints enough room in one comparison row.
            if (prompt.Options.All(option => option.Card != null))
            {
                status.Add(HudElements.Text(
                    prompt.SectionLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold));
                // User directive (2026-09-07): "when leveling. Show current hand (all 5 cards)
                // above the offer." Strictly the LEVEL-UP kind - a card-valued transition of a
                // future kind does not inherit this by coincidence of also satisfying the
                // all-Card check above.
                if (prompt.Kind == GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind
                        .LevelUpUpgrade)
                {
                    var handPreview = BuildLevelUpHandPreviewRow();
                    if (handPreview != null)
                    {
                        rows.Add(handPreview);
                    }
                }

                var upgradeGrid = HudElements.Column(6f);
                upgradeGrid.style.flexGrow = 1f;
                rows.Add(upgradeGrid);
                BuildUpgradeCardRow(upgradeGrid, prompt);
                return;
            }

            var section = Section(prompt.SectionLabel);
            var grid = HudElements.Column(6f);
            grid.style.flexGrow = 1f;
            section.Add(grid);

            // One row per card the options are about, which p.15's own option order already groups them
            // by: the page asks which card to upgrade and which of its two next-Tier cards to take, so
            // the answers for one card arrive together. Breaking the line where that card changes is a
            // line break, not a sort - the options keep the order the pause offered them, and every row
            // ends up one colour wide because the two cards p.15 finds share the chosen card's colour.
            VisualElement row = null;
            var group = default(CardInstanceId);
            foreach (var option in prompt.Options)
            {
                if (row == null || !option.SourceCardInstanceId.Equals(group))
                {
                    row = HudElements.Row(8f);
                    row.style.alignItems = Align.Stretch;
                    row.style.justifyContent = Justify.FlexStart;
                    grid.Add(row);
                    group = option.SourceCardInstanceId;
                }

                row.Add(BuildTransitionOption(option));
            }
        }

        private VisualElement FramelessEffectCardSection()
        {
            var section = HudElements.Row(SectionGap);
            section.name = "gd-effect-card-section";
            section.style.flexWrap = Wrap.NoWrap;
            section.style.alignItems = Align.FlexStart;
            rows.Add(section);
            return section;
        }

        /// <summary>
        /// The seat's current hand above its upgrade offer, using the same standalone card faces.
        /// The panel reserves this row's height and scrolls on short viewports instead of clipping
        /// the candidate cards. Hovering a read-only slot opens full-size inspection.
        /// </summary>
        /// <remarks>
        /// Read-only by construction, not merely by omission: <see cref="DirectorHandsUnityView.BuildStandaloneCard"/>
        /// returns a plain <see cref="VisualElement"/> with no click handler at all (unlike the hand
        /// tray's own interactive card, which wraps a <see cref="Button"/>). Only the containing
        /// slot receives hover; all face descendants and the floating inspection ignore picking.
        /// </remarks>
        private VisualElement BuildLevelUpHandPreviewRow()
        {
            var cards = Model.LevelUpHandPreview;
            if (cards.Count == 0)
            {
                return null;
            }

            var row = HudElements.Row(LevelUpHandPreviewGap);
            row.name = "gd-levelup-hand-preview";
            row.style.flexWrap = Wrap.NoWrap;
            row.style.alignItems = Align.FlexStart;
            row.style.marginBottom = 6f;
            row.pickingMode = PickingMode.Ignore;
            foreach (var card in cards)
            {
                var slot = new VisualElement { name = "gd-levelup-hand-preview-card" };
                slot.style.width = CardFootprint.Width * LevelUpHandPreviewScale;
                slot.style.height = CardFootprint.Height * LevelUpHandPreviewScale;
                slot.style.flexShrink = 0f;
                slot.style.overflow = Overflow.Hidden;
                slot.pickingMode = PickingMode.Position;
                slot.focusable = false;

                var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                // Comparison is read-only, not an unavailable in-play action. Never let the map
                // bleed through a card that the player needs to read while choosing an upgrade.
                face.style.opacity = 1f;
                face.style.position = Position.Absolute;
                face.style.left = 0f;
                face.style.top = 0f;
                face.style.width = CardFootprint.Width;
                face.style.height = CardFootprint.Height;
                face.style.scale = new Scale(
                    new Vector3(LevelUpHandPreviewScale, LevelUpHandPreviewScale, 1f));
                face.style.transformOrigin = new TransformOrigin(new Length(0f), new Length(0f));
                face.pickingMode = PickingMode.Ignore;
                face.focusable = false;

                // Milestone 8.35z62. THE WHOLE SUBTREE, not just the face's own root.
                //
                // Setting the root alone was the shape milestone 8.35z58 shipped, and it does not
                // hold: UI Toolkit picks the DEEPEST element under the pointer, so a child of the
                // face that kept the default PickingMode.Position is still hit-testable even though
                // its parent is not. `LevelUpHandPreviewTests` asserts exactly that - every element
                // under a preview slot ignores the pointer - and it failed the first time it was
                // ever EXECUTED, on this merged tree. It could not have failed earlier: the file is
                // exclude-listed in Tools/DotnetTests, and the worktree that wrote it was not
                // permitted to launch Unity, so its four cases were compile-checked and never run.
                // That is the reason this wave runs complete unfiltered Unity suites rather than
                // trusting a worktree's own green.
                IgnorePointerThroughout(face);

                slot.Add(face);
                slot.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    if (!Model.LevelUpHandPreview.Contains(card)) return;
                    // Defense and leveling are exclusive decisions, sharing one unclipped,
                    // pointer-transparent inspection layer without adding playable controls.
                    ClearDefenseCardPeekPreview();
                    DirectorHandsUnityView.PopulatePeekPreview(
                        defenseCardHoverPreview, card, slot, isRightSideGroup: true, dimUnavailable: false);
                });
                slot.RegisterCallback<PointerLeaveEvent>(_ => ClearDefenseCardPeekPreview());
                row.Add(slot);
            }

            return row;
        }

        /// <summary>
        /// Milestone 8.35z62. Makes an element and everything beneath it pointer-transparent, so a
        /// decorative copy of a card can never take a click from the control it is drawn beside.
        /// Focus is dropped with it, because a read-only face should not be reachable by keyboard
        /// either.
        /// </summary>
        private static void IgnorePointerThroughout(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.pickingMode = PickingMode.Ignore;
            element.focusable = false;
            foreach (var child in element.Children())
            {
                IgnorePointerThroughout(child);
            }
        }

        /// <summary>
        /// p.15's six complete candidate cards in the stable Blue, Green, Red reading order.
        ///
        /// <para>The groups are the pairs the pause itself supplied - options for one
        /// upgraded card arrive together, so grouping them is reading the projection rather than sorting
        /// it - and the candidates inside a pair keep the order the pause offered them.</para>
        ///
        /// <para>Presentation decides only the left-to-right order of the pairs, and takes
        /// it from <see cref="MatchHudTheme.ColorReadingOrder"/> rather than from the pause. The pause
        /// orders its pairs by card instance identity, which puts the same three colours in a different
        /// order for every hero and in no order a player could learn; the colours are the one thing about
        /// this decision that is the same every time it is asked, so they are what the columns are read
        /// by. It reorders pairs and nothing else: no option is added, removed, or moved out of its own
        /// pair.</para>
        /// </summary>
        private void BuildUpgradeCardRow(
            VisualElement grid, SystemTransitionPromptViewModel prompt)
        {
            var columns = new List<List<SystemTransitionOptionViewModel>>();
            foreach (var option in prompt.Options)
            {
                var column = columns.FirstOrDefault(candidate =>
                    candidate[0].SourceCardInstanceId.Equals(option.SourceCardInstanceId));
                if (column == null)
                {
                    column = new List<SystemTransitionOptionViewModel>();
                    columns.Add(column);
                }

                column.Add(option);
            }

            // A stable order, so two pairs of one colour - which p.15's "1 card of each color" makes
            // unreachable, but which nothing in this method needs to assume - keep the pause's order.
            var ordered = columns
                .OrderBy(column => MatchHudTheme.ColorReadingRank(column[0].Color))
                .ToArray();
            var band = HudElements.Row(DirectorHandsUnityView.CardGap);
            band.name = "gd-upgrade-comparison-band";
            band.style.alignItems = Align.FlexStart;
            band.style.justifyContent = Justify.FlexStart;
            band.style.flexWrap = Wrap.NoWrap;
            band.style.backgroundColor = Color.clear;
            grid.Add(band);
            foreach (var column in ordered)
            {
                // Pairing remains the authority's grouping and focus order, but 8.30j makes the
                // group a transparent layout fact: no caption, plate, border, or enclosing outline.
                var pair = HudElements.Row(UpgradePairGap);
                pair.name = "gd-upgrade-pair";
                pair.style.width = UpgradePairWidth;
                pair.style.minWidth = UpgradePairWidth;
                pair.style.maxWidth = UpgradePairWidth;
                pair.style.flexShrink = 0f;
                pair.style.alignItems = Align.FlexStart;
                pair.style.flexWrap = Wrap.NoWrap;
                pair.style.backgroundColor = Color.clear;
                foreach (var option in column)
                {
                    pair.Add(BuildUpgradeOption(option));
                }

                band.Add(pair);
            }
        }

        /// <summary>
        /// One upgrade option: the candidate's whole printed face with the fact that belongs to taking
        /// it centred over the face's bottom item position.
        ///
        /// <para>The face is <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> - the tray's own
        /// builder - so this prompt introduces no second, abbreviated card design and cannot decode a
        /// printed sentence. p.16's projected item is the icon printed on the <em>other</em> candidate,
        /// so the compact backed reading covers this candidate's conflicting bottom item symbol and a
        /// player sees exactly which item this choice leaves them holding. The card returned to the deck
        /// remains named in the option's assistive reading.</para>
        ///
        /// <para>The whole control is the option. It paints no plate or outline; focus and pointer
        /// strengthen only the already-visible item box, so the face and every layout measurement stay
        /// unchanged.</para>
        /// </summary>
        private Button BuildUpgradeOption(SystemTransitionOptionViewModel option)
        {
            var control = new Button(() => ForwardUpgradeOption(option))
            {
                name = "gd-upgrade-option",
                text = string.Empty
            };
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Center;
            control.style.flexShrink = 0f;
            control.style.width = CardFootprint.Width;
            control.style.height = CardFootprint.Height;
            control.style.marginLeft = CardFootprint.ExteriorOutlineWidth;
            control.style.marginRight = CardFootprint.ExteriorOutlineWidth;
            control.style.marginTop = CardFootprint.ExteriorOutlineWidth;
            control.style.marginBottom = CardFootprint.ExteriorOutlineWidth;
            HudElements.Padding(control, 0f);
            HudElements.Border(control, Color.clear, 0f, 0f);
            control.style.backgroundColor = Color.clear;
            upgradeOptionControls.Add(
                new KeyValuePair<SystemTransitionOptionViewModel, VisualElement>(option, control));
            HudElements.OnPointedAt(control, pointedAt =>
                ForwardUpgradeOptionHighlight(pointedAt ? option : null));
            HudElements.ApplyButtonMotion(control);

            control.Add(DirectorHandsUnityView.BuildStandaloneCard(option.Card));
            control.Add(BuildUpgradeOptionFacts(option));
            control.tooltip = option.Detail.Length > 0
                ? $"{option.Label} - {option.Detail}"
                : option.Label;
            return control;
        }

        /// <summary>
        /// What taking this option does, over the candidate's bottom printed item position: p.16's item,
        /// which is the icon printed on the candidate this option does <em>not</em> take.
        /// </summary>
        private static VisualElement BuildUpgradeOptionFacts(SystemTransitionOptionViewModel option)
        {
            var facts = HudElements.Column(3f);
            facts.name = "gd-upgrade-facts";
            facts.style.position = Position.Absolute;
            facts.style.left = 0f;
            facts.style.top = UpgradeItemOverlayTop;
            facts.style.marginTop = 0f;
            facts.style.width = CardFootprint.Width;
            facts.style.alignItems = Align.Center;
            facts.style.opacity = 1f;
            facts.pickingMode = PickingMode.Ignore;

            var itemBox = HudElements.Row(3f);
            itemBox.name = "gd-upgrade-item";
            itemBox.style.flexWrap = Wrap.NoWrap;
            itemBox.style.minWidth = 86f;
            itemBox.style.height = UpgradeItemOverlayHeight;
            itemBox.style.minHeight = UpgradeItemOverlayHeight;
            itemBox.style.maxHeight = UpgradeItemOverlayHeight;
            itemBox.style.justifyContent = Justify.Center;
            itemBox.style.alignItems = Align.Center;
            itemBox.style.paddingLeft = 7f;
            itemBox.style.paddingRight = 7f;
            itemBox.style.paddingTop = 3f;
            itemBox.style.paddingBottom = 3f;
            itemBox.style.backgroundColor = UpgradeItemOverlayBackground;
            itemBox.pickingMode = PickingMode.Ignore;
            HudElements.Border(itemBox, UpgradeItemOverlayBackground, 2f, 3f);
            itemBox.Add(HudElements.Text(
                "GAINS:", 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            if (option.ItemIcon == PrintedValueChannel.None)
            {
                // Two absences p.15 and p.16 produce between them - a deck with no second candidate, and
                // a dashboard with no slot for the icon that one prints - and `Detail` already states
                // which of the two this is. The chip is not drawn at all rather than drawn empty.
                itemBox.Add(HudElements.Text(
                    "NO ITEM", 10f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            }
            else
            {
                itemBox.Add(HudElements.PrintedChannelSymbol(
                    option.ItemIcon, 26f, MatchHudTheme.PrimaryText));
                var amount = HudElements.Text(
                    MatchHudTheme.Signed(1), 13f, MatchHudTheme.PrimaryText, FontStyle.Bold);
                amount.style.marginLeft = 3f;
                itemBox.Add(amount);
            }
            itemBox.tooltip = option.Detail;

            // The user's 2026-08-22 Game View decision: the item is the only one of the three facts
            // written under the card. Which card is set aside and which is returned to the deck are
            // both already on the screen - the sibling is the other card in this column, and the
            // returned card is the one whose colour the column is - so spelling them out six times
            // said what the grid says. Both stay in the option's hover and assistive reading, where
            // this HUD already keeps a fact a surface stops drawing.
            facts.Add(itemBox);
            return facts;
        }

        /// <summary>
        /// Activates one drawn upgrade option: the same forwarding its own control does, for a driver
        /// that has no pointer.
        ///
        /// Milestone 8.8a. It forwards the invocation the continuation itself projected and nothing
        /// composed here, and an option this rebuild did not draw forwards nothing at all - so a stale
        /// option cannot be answered through the surface any more than a forged command can be submitted
        /// through the presenter.
        /// </summary>
        /// <summary>
        /// Activates one drawn Defense card: the same forwarding its own control does, for a driver
        /// that has no pointer.
        ///
        /// Milestone 8.11b, on milestone 8.8a's terms. The card's <see cref="Button"/> calls this, so
        /// there is one path rather than two that could drift, and an offer this rebuild did not draw
        /// forwards nothing at all - a stale face cannot answer a question the authority has moved on
        /// from. What it forwards is the offer's own projected invocation, unchanged.
        /// </summary>
        public bool ForwardDefenseCard(TypedInvocationViewModel choice)
        {
            if (choice == null ||
                !defenseCardControls.Any(entry => ReferenceEquals(entry.Key, choice)))
            {
                return false;
            }

            ForwardInvocation(choice);
            return true;
        }

        public bool ForwardUpgradeOption(SystemTransitionOptionViewModel option)
        {
            if (option == null ||
                !upgradeOptionControls.Any(entry => ReferenceEquals(entry.Key, option)))
            {
                return false;
            }

            ForwardInvocation(new TypedInvocationViewModel(option.Label, option.Command));
            return true;
        }

        /// <summary>
        /// Marks one upgrade option as the pointed-at or focused one, and every other as not.
        ///
        /// Milestone 8.8a. It is public for the same reason every other highlight here is: a driver with
        /// no pointer - the build-smoke path, and every fixture that reaches this pause without one -
        /// has to be able to report the same thing a pointer reports. An option this prompt did not draw
        /// selects nothing, which is the same refusal <see cref="Submit"/> makes for a forged command.
        /// </summary>
        public void ForwardUpgradeOptionHighlight(SystemTransitionOptionViewModel option)
        {
            HighlightedUpgradeOption =
                option != null &&
                upgradeOptionControls.Any(entry => ReferenceEquals(entry.Key, option))
                    ? option
                    : null;
            foreach (var entry in upgradeOptionControls)
            {
                var facts = entry.Value.Children()
                    .FirstOrDefault(child => child.name == "gd-upgrade-facts");
                if (facts != null)
                {
                    var highlighted = ReferenceEquals(entry.Key, HighlightedUpgradeOption);
                    var item = facts.Children()
                        .FirstOrDefault(child => child.name == "gd-upgrade-item");
                    if (item != null)
                    {
                        item.style.backgroundColor = highlighted
                            ? Color.Lerp(UpgradeItemOverlayBackground, MatchHudTheme.ShineGlow, 0.16f)
                            : UpgradeItemOverlayBackground;
                        item.style.borderTopColor = highlighted
                            ? MatchHudTheme.ShineRim
                            : UpgradeItemOverlayBackground;
                        item.style.borderRightColor = item.style.borderTopColor;
                        item.style.borderBottomColor = item.style.borderTopColor;
                        item.style.borderLeftColor = item.style.borderTopColor;
                    }
                }
            }
        }

        private Button BuildTransitionOption(SystemTransitionOptionViewModel option)
        {
            var control = HudElements.Control(
                option.Label,
                () => ForwardInvocation(new TypedInvocationViewModel(option.Label, option.Command)),
                58f);
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.FlexStart;
            control.style.justifyContent = Justify.SpaceBetween;

            // One width for every option, so the rows line up as a grid instead of each row being as
            // wide as its own longest pair of card names.
            control.style.width = OptionWidth;
            control.text = string.Empty;

            // An option about a card wears that card's own colour, exactly as the tray's cards do, so a
            // player matches the option to the card in their hand by the swatch rather than by reading
            // two names. p.15 puts the taken card at the same colour as the upgraded one, so one colour
            // is right for the whole option. The colour name is drawn beside it, because nothing in this
            // HUD means something by colour alone.
            var body = option.Color == CardColor.None
                ? MatchHudTheme.PrimaryText
                : MatchHudTheme.FaceText(option.Color);
            var muted = option.Color == CardColor.None
                ? MatchHudTheme.MutedText
                : new Color(body.r, body.g, body.b, 0.78f);
            if (option.Color != CardColor.None)
            {
                control.style.backgroundColor = MatchHudTheme.Face(option.Color);
                control.Add(HudElements.Text(option.ColorName, 9f, muted, FontStyle.Bold));
            }

            control.Add(HudElements.Text(option.Label, 12f, body, FontStyle.Bold));
            if (option.Detail.Length > 0)
            {
                control.Add(HudElements.Text(option.Detail, 10f, muted));
            }

            return control;
        }

        private void BuildClearSelection(ClearSelectionViewModel selection)
        {
            // Milestone 8.15a, at the user's direction: every offered token is a piece standing on
            // the board, so the board is where it is chosen - marked Eligible or Selected where it
            // stands and toggled by clicking it, exactly as the Minion Battle's candidates have been
            // since 8.11a. The controls that used to name them are gone; what stays is what has no
            // board surface: agreeing to the chosen subset, and closing the selection.
            var section = Section("CLEAR");
            var confirm = HudElements.Control(
                selection.Confirm.Label, () => ClearConfirmed?.Invoke());
            confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
            confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
            section.Add(confirm);
            section.Add(HudElements.Control(selection.CancelLabel, () => ClearCancelled?.Invoke()));
        }

        /// <summary>
        /// p.11's Defense response: every legal card as its own whole printed face, then the one
        /// control that is not a card.
        ///
        /// <para>Milestone 8.11b. A card was offered here as two lines of abbreviated text while the
        /// same cards stood on the tray as complete faces, so a defender was comparing a summary
        /// against the thing it summarised. The face is
        /// <see cref="DirectorHandsUnityView.BuildStandaloneCard"/> - the tray's own builder, and the
        /// same one p.15's upgrade prompt uses - so this region introduces no second card design and
        /// decodes no printed sentence.</para>
        ///
        /// <para>The authority-computed
        /// Attack/Defense/result preview is still what hover and keyboard focus raise, still read from
        /// the offer's own <see cref="DefenseBreakdownViewModel"/>, and the visible order is the
        /// projected response order, which is the keyboard order too because the controls are added in
        /// it. A card may be clicked or dragged onto the same live board surface used by facedown
        /// commitment; both paths forward that card's exact projected invocation once. The redundant
        /// DEFENSE gutter and card frames are absent, and <c>DECLINE DEFENSE</c> uses the same pewter
        /// banner language as <c>CONFIRM FACEDOWN</c>.</para>
        /// </summary>
        private void BuildDefense()
        {
            var section = HudElements.Row(SectionGap);
            section.name = "gd-defense-section";
            section.style.flexWrap = Wrap.NoWrap;
            section.style.alignItems = Align.FlexStart;
            section.style.flexGrow = 0f;
            section.style.flexShrink = 0f;
            section.style.width = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.minWidth = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.maxWidth = ConfiguredMatchHudRenderer.CompactHandRightEdge;
            section.style.marginTop = DirectorHandsUnityView.AvailableCardRaise;
            rows.Add(section);
            var cardChoices = Model.DefenseChoices.Where(choice => choice.Card != null).ToArray();
            for (var index = 0; index < cardChoices.Length; index++)
            {
                var choice = cardChoices[index];
                var control = BuildDefenseCard(choice);
                var slot = new VisualElement { name = "gd-defense-card-slot" };
                slot.style.flexDirection = FlexDirection.Column;
                slot.style.alignItems = Align.Stretch;
                slot.style.flexShrink = 0f;
                slot.style.marginRight = index == cardChoices.Length - 1
                    ? 0f
                    : -DirectorHandsUnityView.PeekCardOverlap;
                slot.style.top = -DirectorHandsUnityView.AvailableCardRaise;
                HudElements.Padding(slot, DirectorHandsUnityView.ActiveOutlineGap);
                HudElements.Border(slot, Color.clear,
                    DirectorHandsUnityView.ActiveOutlineWidth, 11f);
                slot.Add(control);
                BindDefenseCardDrag(control, slot, choice);
                HudElements.OnPointedAt(control, pointedAt =>
                    SetDefenseCardPeekPreview(pointedAt ? choice : null, slot));
                if (choice.DefenseBreakdown != null)
                {
                    HudElements.OnPointedAt(control, pointedAt =>
                    {
                        if (pointedAt)
                        {
                            ShowDefensePreview(
                                choice.DefenseBreakdown,
                                CalculateDefenseCardFanWorldBound(control.worldBound));
                        }
                        else
                        {
                            ScheduleDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
                        }
                    });
                }

                section.Add(slot);
            }

            var declineChoice = Model.DefenseChoices.FirstOrDefault(choice => choice.Card == null);
            if (declineChoice != null)
            {
                var decline = new PewterBanner(
                    () => ForwardInvocation(declineChoice), true,
                    DirectorHandsUnityView.CornerBannerFontSize,
                    "gd-decline-defense-banner-label")
                {
                    name = "gd-decline-defense-control",
                    text = declineChoice.Label
                };
                decline.SetVisibleLabel(declineChoice.Label);
                decline.style.minHeight = DirectorHandsUnityView.CornerBannerHeight;
                decline.style.whiteSpace = WhiteSpace.Normal;
                declineDefenseHost.Add(decline);
                declineDefenseHost.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// The breakdown belongs to the complete set of available Defense cards, not to the one
        /// overlapped face currently under the pointer. Anchor it to the visible fan so its right
        /// edge is stable while the pointer crosses cards and the popup never sits over that hand.
        /// </summary>
        private Rect CalculateDefenseCardFanWorldBound(Rect fallback)
        {
            var hasBounds = false;
            var xMin = 0f;
            var yMin = 0f;
            var xMax = 0f;
            var yMax = 0f;
            foreach (var pair in defenseCardControls)
            {
                var bounds = pair.Value.worldBound;
                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    xMin = bounds.xMin;
                    yMin = bounds.yMin;
                    xMax = bounds.xMax;
                    yMax = bounds.yMax;
                    hasBounds = true;
                    continue;
                }

                xMin = Mathf.Min(xMin, bounds.xMin);
                yMin = Mathf.Min(yMin, bounds.yMin);
                xMax = Mathf.Max(xMax, bounds.xMax);
                yMax = Mathf.Max(yMax, bounds.yMax);
            }

            return hasBounds ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : fallback;
        }

        /// <summary>
        /// One legal Defense card: the whole card is the control, and it forwards exactly the
        /// invocation the authority projected for it.
        ///
        /// The control adds no surrounding plate, inset, or selection box, so the shared card face is
        /// the complete visible offer and pointing at it cannot move or resize the row.
        /// </summary>
        private Button BuildDefenseCard(TypedInvocationViewModel choice)
        {
            var control = new Button(() => ForwardDefenseCard(choice))
            {
                name = "gd-defense-card",
                text = string.Empty
            };
            defenseCardControls.Add(
                new KeyValuePair<TypedInvocationViewModel, VisualElement>(choice, control));
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Stretch;
            control.style.flexShrink = 0f;
            // Milestone 8.20c: the offers line up because every face is the standard footprint, not
            // because the row stretches them. A Block prints no Defense value and an Attack's rules
            // text runs longer, so content-sized offers used to come out four different heights at
            // four different baselines; stretching them to the row fixed that inside this prompt and
            // left the same four cards a different size everywhere else.
            control.style.alignSelf = Align.FlexStart;
            control.style.width = CardFootprint.Width;
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, 0f);
            control.style.backgroundColor = Color.clear;
            HudElements.Border(control, Color.clear, 0f, 0f);
            HudElements.ApplyButtonMotion(control);

            var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
            // Milestone 8.30d: a card Defense offer shines exactly the shield it would defend
            // with, at the typed placement the presenter stated - never one the view inferred
            // from the face. Every other card-faced invocation carries None and lights nothing.
            DirectorHandsUnityView.AddFaceShine(face, choice.Card, choice.DefenseShineSlot);
            control.Add(face);
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
        }

        private void BindDefenseCardDrag(
            Button control,
            VisualElement sourceSlot,
            TypedInvocationViewModel choice)
        {
            control.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || choice.Card == null ||
                    !defenseDragState.Begin(
                        evt.pointerId, choice.Card.CardInstanceId, evt.position))
                {
                    return;
                }

                defenseDragCapture = control;
                control.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!defenseDragState.Move(
                        evt.pointerId, evt.position, DirectorHandsUnityView.CardDragThreshold))
                {
                    return;
                }

                ShowDefenseDragProxy(choice, sourceSlot, evt.position);
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!defenseDragState.Release(evt.pointerId, out var cardId, out var wasDrag))
                {
                    return;
                }

                ReleaseDefenseDragCapture(evt.pointerId);
                HideDefenseDragProxy();
                if (!wasDrag)
                {
                    return;
                }

                evt.StopImmediatePropagation();
                if (choice.Card.CardInstanceId.Equals(cardId) &&
                    isMapDropPoint?.Invoke(evt.position) == true)
                {
                    ForwardDefenseCard(choice);
                }
            }, TrickleDown.TrickleDown);

            control.RegisterCallback<PointerCancelEvent>(_ => CancelDefenseCardDrag());
            control.RegisterCallback<DetachFromPanelEvent>(_ => CancelDefenseCardDrag());
        }

        private void ShowDefenseDragProxy(
            TypedInvocationViewModel choice,
            VisualElement source,
            Vector2 pointerPosition)
        {
            ClearDefenseCardPeekPreview();
            if (defenseDragProxy.childCount == 0)
            {
                var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
                DirectorHandsUnityView.AddFaceShine(
                    face, choice.Card, choice.DefenseShineSlot);
                DirectorHandsUnityView.IgnorePickingRecursively(face);
                defenseDragProxy.Add(face);
                defenseDragProxy.tooltip = choice.Label.Replace('\n', ' ');
            }

            if (draggedDefenseCard == null)
            {
                draggedDefenseCard = source;
                draggedDefenseCard.style.visibility = Visibility.Hidden;
            }

            defenseDragProxy.style.left = pointerPosition.x - (CardFootprint.Width * 0.5f);
            defenseDragProxy.style.top = pointerPosition.y - (CardFootprint.Height * 0.5f);
            defenseDragProxy.style.display = DisplayStyle.Flex;
            defenseDragProxy.BringToFront();
        }

        private void CancelDefenseCardDrag()
        {
            if (defenseDragCapture != null && defenseDragState.IsTracking)
            {
                ReleaseDefenseDragCapture(defenseDragState.PointerId);
            }

            defenseDragState.Cancel();
            defenseDragCapture = null;
            HideDefenseDragProxy();
        }

        private void ReleaseDefenseDragCapture(int pointerId)
        {
            if (defenseDragCapture != null && defenseDragCapture.HasPointerCapture(pointerId))
            {
                defenseDragCapture.ReleasePointer(pointerId);
            }

            defenseDragCapture = null;
        }

        private void HideDefenseDragProxy()
        {
            if (draggedDefenseCard != null)
            {
                draggedDefenseCard.style.visibility = Visibility.Visible;
                draggedDefenseCard = null;
            }

            defenseDragProxy.style.display = DisplayStyle.None;
            defenseDragProxy.Clear();
        }

        private VisualElement defenseCardPeekAnchor;

        private void SetDefenseCardPeekPreview(
            TypedInvocationViewModel choice,
            VisualElement anchor)
        {
            if (choice?.Card == null)
            {
                ClearDefenseCardPeekPreview();
                return;
            }

            if (defenseCardPeekAnchor != null && defenseCardPeekAnchor != anchor)
            {
                defenseCardPeekAnchor.style.opacity = 1f;
            }

            defenseCardPeekAnchor = anchor;
            defenseCardPeekAnchor.style.opacity = 0f;
            DirectorHandsUnityView.PopulatePeekPreview(
                defenseCardHoverPreview, choice.Card, anchor);
            DirectorHandsUnityView.AddFaceShine(
                defenseCardHoverPreview.ElementAt(0),
                choice.Card,
                choice.DefenseShineSlot);
        }

        private void ClearDefenseCardPeekPreview()
        {
            if (defenseCardPeekAnchor != null)
            {
                defenseCardPeekAnchor.style.opacity = 1f;
                defenseCardPeekAnchor = null;
            }

            defenseCardHoverPreview.Clear();
            defenseCardHoverPreview.style.display = DisplayStyle.None;
        }

        private void BuildActionRow()
        {
            // Milestone 8.20d: a standing decision gets its own labelled section, and it is built
            // before the action row rather than appended into it. Everything below is what the match
            // is offering; a pause is what the match is asking, and the two had been one row under
            // one heading that said `ACTIONS`.
            BuildEffectChoiceSection();

            // The status line already says that the controls are actions. Keeping a 96-unit ACTIONS
            // gutter repeated that fact and forced the final controls onto a staggered second line in
            // the compact Director-side dock. This unlabelled row spends its whole width on the actual
            // choices. The controls may flex narrower and wrap their own words, but the action row
            // itself never wraps: End Turn belongs beside the actions, not alone beneath them.
            var section = HudElements.Row(ActionControlGap);
            section.name = "gd-action-section";
            section.style.alignItems = Align.Stretch;
            section.style.flexWrap = Wrap.NoWrap;
            section.style.flexShrink = 1f;
            section.style.width = Length.Percent(100f);
            rows.Add(section);
            AddActionInvocation(section, Model.ContinueEndOfRoundInvocation);
            foreach (var choice in Model.InitiativeChoices)
            {
                AddActionInvocation(section, choice);
            }

            // Printed starts and the Clear/Fast Travel replacements attached to them are drawn on the
            // fully exposed resolving card. This dock retains only controls with no printed region.

            // Milestone 8.11a, at the user's request: each Respawn destination is a space, so it is
            // marked on the board and chosen by clicking it. The controls that used to name them are
            // gone; `HeroRespawnOptions` stays on the model because the board resolves a click through
            // it, and because the prompt still counts them to know it has a question to ask.

            // Milestone 8.11a: declining to respawn has no space on the board, so it stays a control
            // even though every destination it sits beside has become a click on the map.
            AddActionInvocation(section, Model.ResolveWithoutRespawn);

            // Milestone 6.5c drew one control per target the choice names, while a destination
            // candidate had none - the lit board was its control.
            //
            // Milestone 8.11a, at the user's request, makes the board the control for both. A candidate
            // that names a hero, a minion or a token is marked where it stands and answered by clicking
            // it, exactly as an Attack target has always been, so a second list of the same choices in
            // the prompt would be two ways to say one thing. The prompt keeps the question and the
            // choices that have no board surface at all; `EffectChoiceTargets` stays on the model
            // because the presenter still resolves a click through it.

            // Milestone 8.30b, by the user-approved amendment: End Turn is no longer one of these.
            // It is built by `RebuildEndTurn` into its own surface, which the renderer places in the
            // status bar's top-right slot.

            // A trailing gap is useful only between controls, and the row is content-sized: the last
            // control keeps none.
            var last = section.Children().LastOrDefault();
            if (last != null)
            {
                last.style.marginRight = 0f;
            }
        }

        /// <summary>
        /// Milestone 8.35f. The seat's own UNDO control: a square immediately left of End Turn,
        /// drawn with a counter-clockwise arrow and no words.
        /// </summary>
        /// <remarks>
        /// <para>The user placed it there and named its shape. It carries no label because a glyph
        /// says "step back" in every language this build will ever be read in, and because the slot
        /// is one status-strip height square - there is no room for End Turn's lettering in it.</para>
        /// <para>It carries no invocation either, which is the structural half: a rewind is not a
        /// command, so there is nothing here for a view to forward. The press raises an event, the
        /// presenter asks its client, and the host decides. A view that could compose an undo could
        /// compose one for another seat.</para>
        /// <para><b>Milestone 8.42d: it is not suppressed by <see cref="HasExclusiveDecision"/>.</b>
        /// That suppression is the action region's "one usable surface" rule for the hand and the
        /// tray, and this square is not in that region - it is a status-strip corner control, like
        /// END TURN, which milestone 8.35v stopped hiding there for the same reason. Hiding it cost
        /// the player the rewind in exactly the states it exists for: a card whose text asks the
        /// actor a question drew no square at all, while the host went on publishing the rewind and
        /// would have accepted it. A seat the host offers nothing still gets no square, because
        /// then there is genuinely nothing to press.</para>
        /// </remarks>
        private void RebuildSeatUndo()
        {
            seatUndoHost.Clear();
            if (Model == null || !Model.SeatUndoAvailable)
            {
                seatUndoHost.style.display = DisplayStyle.None;
                return;
            }

            seatUndoHost.style.display = DisplayStyle.Flex;
            var control = CreateCornerControl(
                SeatUndoGlyph, RequestSeatUndo, "gd-seat-undo-control",
                ConfiguredMatchHudRenderer.SeatUndoControlSide, SeatUndoGlyphFontSize);
            control.tooltip = SeatUndoTooltip;
            seatUndoHost.Add(control);
        }

        /// <summary>
        /// End Turn, as its own control in its own surface. Milestone 8.30b's user-approved amendment.
        ///
        /// <para>Milestone 8.35v (the THIRD amendment: "end turn button should be present the whole
        /// time on players action turn"). What forwards is unchanged when a direct invocation
        /// stands: the same projected command, through the same <c>ForwardInvocation</c>. The rest
        /// of the control now draws in two further states this milestone adds - a press that must
        /// first discard the seat's own open Attack-target selection, and a DISABLED state naming
        /// why (the SIXTH amendment's rule: nothing is ever greyed without saying why) - and it is
        /// no longer suppressed by <see cref="HasExclusiveDecision"/>, which used to hide it during
        /// exactly the mandatory-step and another-seat's-decision states the disabled reason now
        /// names; the region's other "one usable surface" behaviour for the hand and tray is
        /// untouched, only END TURN itself stops disappearing.</para>
        /// </summary>
        private void RebuildEndTurn()
        {
            endTurnHost.Clear();
            // Milestone 8.51: a rebuilt control starts with no press signals behind it.
            endTurnPress.Reset();
            endTurnControl = null;
            var invocation = Model?.EndTurnInvocation;
            var cancelsOpenAttack = Model?.EndTurnCancelsOpenAttack ?? false;
            var unavailableReason = Model?.EndTurnUnavailableReason;

            if (invocation == null && !cancelsOpenAttack && unavailableReason == null)
            {
                EndTurnShinesFully = false;
                endTurnHost.style.display = DisplayStyle.None;
                return;
            }

            endTurnHost.style.display = DisplayStyle.Flex;

            if (unavailableReason != null)
            {
                EndTurnShinesFully = false;
                var disabled = CreateCornerControl(
                    "END TURN", RequestEndTurn, "gd-end-turn-control",
                    ConfiguredMatchHudRenderer.EndTurnControlHeight, EndTurnControlFontSize);
                disabled.tooltip = unavailableReason;
                HudElements.MarkUnavailable(disabled);
                endTurnHost.Add(disabled);
                return;
            }

            // Milestone 8.30d: whether ending the turn is the only thing left to do. Everything
            // else this region could offer alongside an End Turn is enumerable from the model the
            // view is already rendering: a startable printed region, a live replacement, a tied
            // Initiative, or the end-of-round continue. When none stands, the banner shines fully -
            // the game itself points at the player's one move. Milestone 8.35v: a press that must
            // cancel an open Attack selection first reads the identical way - the resolution being
            // cancelled offers nothing else either, so `PrintedActionRegions` is already empty.
            var onlyOfferLeft =
                Model.FinishMovementInvocation == null &&
                Model.InitiativeChoices.Count == 0 &&
                Model.ContinueEndOfRoundInvocation == null &&
                Model.PrintedActionRegions.All(region =>
                    region.Invocation == null &&
                    region.FastTravelOffer == null &&
                    (region.ClearOffer == null || !region.ClearOffer.IsAvailable));

            EndTurnShinesFully = onlyOfferLeft;
            var label = invocation?.Label ?? "END TURN";
            var control = CreateCornerControl(
                label, RequestEndTurn, "gd-end-turn-control",
                ConfiguredMatchHudRenderer.EndTurnControlHeight, EndTurnControlFontSize);
            // Milestone 8.51: the live control is the one whose pointer, key and submit signals
            // name the press; the greyed one above cannot be pressed and is not observed.
            endTurnPress.Observe(control);
            endTurnControl = control;
            if (onlyOfferLeft)
            {
                // Keep the existing sole-action cue outside the shared chrome, never over its face.
                var shine = new VisualElement
                {
                    name = "gd-end-turn-shine",
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                shine.style.position = Position.Absolute;
                shine.style.left = -3f;
                shine.style.right = -3f;
                shine.style.top = -3f;
                shine.style.bottom = -3f;
                HudElements.Border(shine, MatchHudTheme.ShineGlow, 3f,
                    MatchHudTheme.ButtonCornerRadius + 3f);
                control.Add(shine);
            }
            endTurnHost.Add(control);
        }

        private static Button CreateCornerControl(
            string label, Action selected, string name, float height, float fontSize)
        {
            var control = HudElements.Control(label, selected, height);
            control.name = name;
            control.style.fontSize = fontSize;
            control.style.unityFontStyleAndWeight = FontStyle.Bold;
            control.style.unityTextAlign = TextAnchor.MiddleCenter;
            control.style.whiteSpace = WhiteSpace.NoWrap;
            control.style.flexGrow = 1f;
            control.style.overflow = Overflow.Visible;
            return control;
        }

        /// <summary>
        /// The neutral pewter banner - swallowtailed at both ends in the cards' own cloth
        /// language, but in the silver of their printed icons rather than a fifth patch of gold,
        /// by the user's 8.30d decision. Born as End Turn's clothes, and shared with the hand's
        /// Confirm and Pass controls by the user's follow-up: one banner language for "the button
        /// that moves the game on". A strong-shine banner carries the full warm glow (End Turn as
        /// the only move left, or a confirm that IS the move); otherwise a modest steady one,
        /// raised while pointed at.
        /// </summary>
        internal sealed class PewterBanner : Button
        {
            private const float NotchDepth = 14f;

            private readonly bool strongShine;
            private readonly bool hoverOnlyShine;
            private bool hovered;

            private readonly VisualElement cloth;

            internal PewterBanner(
                Action selected,
                bool strongShine,
                float fontSize,
                string labelName,
                bool rectangular = false,
                bool hoverOnlyShine = false)
                : base(selected)
            {
                this.strongShine = strongShine;
                this.hoverOnlyShine = hoverOnlyShine;
                this.rectangular = rectangular;
                style.backgroundColor = Color.clear;
                HudElements.Border(this, Color.clear, 0f, 0f);
                style.marginLeft = 0f;
                style.marginRight = 0f;
                style.marginTop = 0f;
                style.marginBottom = 0f;
                style.paddingLeft = rectangular ? 12f : NotchDepth + 8f;
                style.paddingRight = rectangular ? 12f : NotchDepth + 8f;
                style.paddingTop = 6f;
                style.paddingBottom = 6f;
                style.whiteSpace = WhiteSpace.NoWrap;
                style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                style.overflow = Overflow.Visible;

                // A TextElement paints its own words FIRST - both a generateVisualContent mesh and
                // any child render above them - so the cloth is a child, and the VISIBLE words are
                // a second child above it. The Button's own text stays set, buried under the
                // pewter, because it is the selector every existing check finds End Turn by.
                cloth = new VisualElement
                {
                    name = "gd-banner-cloth",
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                cloth.style.position = Position.Absolute;
                cloth.style.left = 0f;
                cloth.style.top = 0f;
                cloth.style.right = 0f;
                cloth.style.bottom = 0f;
                cloth.style.overflow = Overflow.Visible;
                cloth.generateVisualContent += OnGenerateVisualContent;
                Add(cloth);
                cloth.SendToBack();

                visibleLabel = new Label
                {
                    name = labelName,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                visibleLabel.style.position = Position.Absolute;
                visibleLabel.style.left = rectangular ? 0f : NotchDepth;
                visibleLabel.style.top = 0f;
                visibleLabel.style.right = rectangular ? 0f : NotchDepth;
                visibleLabel.style.bottom = 0f;
                visibleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                visibleLabel.style.fontSize = fontSize;
                visibleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                visibleLabel.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                Add(visibleLabel);
                RegisterCallback<PointerEnterEvent>(_ => SetHovered(true));
                RegisterCallback<PointerLeaveEvent>(_ => SetHovered(false));
                // Milestone 8.35z8: the banner already lifts its own glow on hover; the shared
                // scale motion adds the same quick press dip every other control now gets, on top
                // of - never instead of - that cloth.
                HudElements.ApplyButtonMotion(this);
            }

            private readonly Label visibleLabel;
            private readonly bool rectangular;

            /// <summary>The words the banner shows; the Button's own text is the selector only.</summary>
            internal void SetVisibleLabel(string label)
            {
                visibleLabel.text = label ?? string.Empty;
            }

            /// <summary>Whether this banner is in its full-shine "your one move" state.</summary>
            internal bool ShinesFully => strongShine && !hoverOnlyShine;

            /// <summary>Whether all shine is withheld until the pointer enters this banner.</summary>
            internal bool LightsOnlyWhenHovered => hoverOnlyShine;

            internal bool IsLit => hovered || (strongShine && !hoverOnlyShine);

            /// <summary>Whether this instance uses the End Turn rectangle instead of swallowtails.</summary>
            internal bool IsRectangular => rectangular;

            private void SetHovered(bool value)
            {
                if (hovered == value)
                {
                    return;
                }

                hovered = value;
                cloth.MarkDirtyRepaint();
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var rect = cloth.contentRect;
                var left = rect.xMin;
                var right = rect.xMax;
                var mid = rect.center.y;
                if (right - left <= NotchDepth * 2f + 4f || rect.height <= 8f)
                {
                    return;
                }

                var strong = (strongShine && !hoverOnlyShine) || hovered;
                var restingGlow = !hoverOnlyShine;
                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    new Color(MatchHudTheme.ShineHalo.r, MatchHudTheme.ShineHalo.g,
                        MatchHudTheme.ShineHalo.b, strong ? 0.30f : restingGlow ? 0.10f : 0f),
                    strong ? 18f : restingGlow ? 8f : 0f);
                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    new Color(MatchHudTheme.ShineGlow.r, MatchHudTheme.ShineGlow.g,
                        MatchHudTheme.ShineGlow.b, strong ? 0.50f : restingGlow ? 0.16f : 0f),
                    strong ? 10f : restingGlow ? 5f : 0f);
                if (strong)
                {
                    DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                        new Color(MatchHudTheme.ShineRim.r, MatchHudTheme.ShineRim.g,
                            MatchHudTheme.ShineRim.b, 0.85f), 4f);
                }

                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid,
                    MatchHudTheme.PewterBanner, Color.clear, 0f);

                painter.fillColor = new Color(0f, 0f, 0f, 0.12f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(rectangular ? left : left + NotchDepth, mid));
                painter.LineTo(new Vector2(rectangular ? right : right - NotchDepth, mid));
                painter.LineTo(new Vector2(right, rect.yMax));
                painter.LineTo(new Vector2(left, rect.yMax));
                painter.ClosePath();
                painter.Fill();

                DrawShape(painter, left, right, rect.yMin, rect.yMax, mid, Color.clear,
                    MatchHudTheme.PewterBannerEdge, 3f);
            }

            private void DrawShape(
                Painter2D painter, float left, float right, float top, float bottom, float mid,
                Color fill, Color stroke, float strokeWidth)
            {
                if (!rectangular)
                {
                    DrawBanner(painter, left, right, top, bottom, mid, fill, stroke, strokeWidth);
                    return;
                }

                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.ClosePath();
                if (fill.a > 0f)
                {
                    painter.fillColor = fill;
                    painter.Fill();
                }

                if (stroke.a > 0f && strokeWidth > 0f)
                {
                    painter.strokeColor = stroke;
                    painter.lineWidth = strokeWidth;
                    painter.lineJoin = LineJoin.Round;
                    painter.Stroke();
                }
            }

            private static void DrawBanner(
                Painter2D painter, float left, float right, float top, float bottom, float mid,
                Color fill, Color stroke, float strokeWidth)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                painter.LineTo(new Vector2(right - NotchDepth, mid));
                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.LineTo(new Vector2(left + NotchDepth, mid));
                painter.ClosePath();
                if (fill.a > 0f)
                {
                    painter.fillColor = fill;
                    painter.Fill();
                }

                if (stroke.a > 0f && strokeWidth > 0f)
                {
                    painter.strokeColor = stroke;
                    painter.lineWidth = strokeWidth;
                    painter.lineJoin = LineJoin.Round;
                    painter.Stroke();
                }
            }
        }

        /// <summary>
        /// End Turn's type size. Milestone 8.30b: it is the largest label on the HUD, which is the
        /// point - it is the one control that ends what the player is doing.
        /// </summary>
        public const float EndTurnControlFontSize = 18f;

        /// <summary>
        /// Milestone 8.35f. The counter-clockwise open-circle arrow the seat undo square draws, and
        /// the only thing it draws: no words, by the user's own placement decision.
        /// </summary>
        public const string SeatUndoGlyph = "↺";

        public const string SeatUndoTooltip = "Step back one action of this turn";

        public const float SeatUndoGlyphFontSize = 40f;

        private void AddActionInvocation(
            VisualElement section,
            TypedInvocationViewModel invocation)
        {
            if (invocation != null)
            {
                AddActionControl(
                    section,
                    HudElements.Control(invocation.Label, () => ForwardInvocation(invocation)));
            }
        }

        private static void AddActionControl(VisualElement section, VisualElement control)
        {
            control.style.marginLeft = 0f;
            control.style.marginRight = ActionControlGap;
            control.style.paddingLeft = ActionControlHorizontalPadding;
            control.style.paddingRight = ActionControlHorizontalPadding;
            control.style.fontSize = ActionControlFontSize;
            control.style.flexShrink = 1f;
            control.style.minWidth = 56f;
            section.Add(control);
        }

        /// <summary>
        /// The standing effect choice, under a gutter that names the decision it is.
        ///
        /// <para>Milestone 8.20d. These controls used to be appended to the action row, so rulebook
        /// p.10's discard chooser reached the table as four card faces under a heading reading
        /// <c>ACTIONS</c>, beneath a status line reading "choose an action or End Turn" - the shape
        /// milestone 8.20b's Wasp journey met and recorded. The controls, their order and their
        /// invocations are exactly what they were; what changed is the heading they arrive under,
        /// which the model now carries because the pause projected it.</para>
        ///
        /// <para>The section is empty, and therefore hidden, for a choice answered entirely on the
        /// board - milestone 8.11a moved every candidate naming a piece or a space out of this region
        /// - and the status line is then the whole of what names the pause.</para>
        /// </summary>
        private void BuildEffectChoiceSection()
        {
            var prompt = Model.EffectChoicePrompt;
            if (prompt == null)
            {
                return;
            }

            if (prompt.Subject == EffectChoiceSubject.OptionalStep)
            {
                BuildOptionalStepActions();
                return;
            }

            // A card-valued choice is already self-identifying through its whole printed faces and
            // the narration now says why one is being chosen. It therefore uses the same frameless
            // row as Defense instead of spending a left gutter on redundant `CARD CHOICE` text.
            var section = prompt.Subject == EffectChoiceSubject.Card
                ? FramelessEffectCardSection()
                : Section(prompt.SectionLabel);
            if (Model.EffectChoices.Any(choice => choice.Card != null))
            {
                section.style.flexWrap = Wrap.NoWrap;
            }
            foreach (var effectChoice in Model.EffectChoices)
            {
                // Milestone 8.15a, at the user's direction: a choice that is about a card is drawn
                // as the card - rulebook p.10's discard chooser above all - through the same shared
                // face a Defense offer wears. A choice about no card keeps its labelled control.
                if (effectChoice.Card != null)
                {
                    section.Add(BuildEffectCardChoice(effectChoice));
                }
                else
                {
                    AddInvocation(section, effectChoice);
                }
            }

            // Milestone 6.5ac: a mode the actor cannot perform keeps its label and its ordinal and
            // carries no command at all. Milestone 8.35s: the pause names no cause for this - it is a
            // bare label plus its candidate, unlike a printed-action offer's stated Unavailability - so
            // the generic word is what the hover says rather than a guessed one.
            foreach (var unavailable in Model.UnavailableEffectChoices)
            {
                var control = HudElements.Control(unavailable.Label, null);
                control.tooltip = ActionUnavailabilityLabels.Generic;
                HudElements.MarkUnavailable(control);
                section.Add(control);
            }
        }

        private void BuildOptionalStepActions()
        {
            optionalStepActionsHost.style.display = DisplayStyle.Flex;
            for (var index = 0; index < Model.EffectChoices.Count; index++)
            {
                var invocation = Model.EffectChoices[index];
                var accepts = invocation.Label == EffectChoiceLabels.AcceptLabel;
                var control = new PewterBanner(
                    () => ForwardInvocation(invocation), false,
                    DirectorHandsUnityView.CornerBannerFontSize,
                    accepts
                        ? "gd-optional-step-accept-label"
                        : "gd-optional-step-decline-label",
                    hoverOnlyShine: true)
                {
                    name = accepts ? "gd-optional-step-accept" : "gd-optional-step-decline",
                    text = invocation.Label
                };
                control.SetVisibleLabel(invocation.Label);
                control.style.width = OptionalStepActionWidth;
                control.style.minHeight = OptionalStepActionHeight;
                control.style.height = OptionalStepActionHeight;
                control.style.flexGrow = 0f;
                control.style.flexBasis = OptionalStepActionWidth;
                control.style.flexShrink = 1f;
                control.style.marginRight = index < Model.EffectChoices.Count - 1
                    ? OptionalStepActionGap
                    : 0f;
                optionalStepActionsHost.Add(control);
            }
        }

        private void AddInvocation(VisualElement section, TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return;
            }

            var control = HudElements.Control(invocation.Label, () => ForwardInvocation(invocation));

            // Milestone 8.37. A control that NAMES a printed colour wears it, through the same
            // palette every other surface draws that colour with. The word stays and the tint is
            // added to it rather than replacing it, so the control is still readable to anyone who
            // cannot separate the six by hue - which matters more here than anywhere else in the HUD,
            // because Tali's ally is asked to name a colour with no card in front of them.
            if (invocation.Swatch != CardColor.None)
            {
                control.style.backgroundColor = MatchHudTheme.Face(invocation.Swatch);
                control.style.color = MatchHudTheme.FaceText(invocation.Swatch);
            }

            section.Add(control);
        }

        /// <summary>
        /// One card-valued effect choice as the whole printed face, on the terms milestone 8.11b gave
        /// a legal Defense offer: the tray's own standalone builder without a surrounding plate,
        /// inset, or selection outline, the whole face forwarding exactly the projected invocation,
        /// and the card's name in the control's hover and assistive reading.
        /// </summary>
        private Button BuildEffectCardChoice(TypedInvocationViewModel choice)
        {
            var control = new Button(() => ForwardInvocation(choice))
            {
                name = "gd-effect-card",
                text = string.Empty
            };
            control.style.flexDirection = FlexDirection.Column;
            control.style.alignItems = Align.Stretch;
            control.style.flexShrink = 0f;
            control.style.alignSelf = Align.FlexStart;
            control.style.width = CardFootprint.Width;
            control.style.marginLeft = 0f;
            control.style.marginRight = 0f;
            control.style.marginTop = 0f;
            control.style.marginBottom = 0f;
            HudElements.Padding(control, 0f);
            control.style.backgroundColor = Color.clear;
            HudElements.Border(control, Color.clear, 0f, 0f);
            HudElements.ApplyButtonMotion(control);

            var face = DirectorHandsUnityView.BuildStandaloneCard(choice.Card);
            // Milestone 8.30d: a card Defense offer shines exactly the shield it would defend
            // with, at the typed placement the presenter stated - never one the view inferred
            // from the face. Every other card-faced invocation carries None and lights nothing.
            DirectorHandsUnityView.AddFaceShine(face, choice.Card, choice.DefenseShineSlot);
            control.Add(face);
            control.tooltip = choice.Label.Replace('\n', ' ');
            return control;
        }

        public void ShowDefensePreview(DefenseBreakdownViewModel model, Rect anchorScreenRect)
        {
            activeDefensePreview = model ?? throw new ArgumentNullException(nameof(model));
            defensePreviewAnchor = anchorScreenRect;
            dismissPreviewAt = -1d;
            preview.Clear();
            var title = HudElements.Text(model.Title, 13f, MatchHudTheme.PrimaryText);
            title.name = "gd-defense-preview-title";
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6f;
            preview.Add(title);

            AddDefenseSection("ATTACK", model.AttackRows);
            var attackTotal = HudElements.Text(
                $"ATTACK TOTAL  {model.AttackTotal}", 13f, MatchHudTheme.PrimaryText);
            attackTotal.name = "gd-defense-attack-total";
            attackTotal.style.unityFontStyleAndWeight = FontStyle.Bold;
            attackTotal.style.marginTop = 3f;
            attackTotal.style.marginBottom = 7f;
            preview.Add(attackTotal);

            AddDefenseSection("DEFENSE", model.DefenseRows);
            var defenseTotalText = model.DefenseTotal.HasValue
                ? $"DEFENSE TOTAL  {model.DefenseTotal.Value}"
                : model.BlockResult == DefenseBlockResult.Blocked
                    ? "BLOCK"
                    : model.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                        ? "BLOCK CONDITION NOT MET"
                        : "NO DEFENSE TOTAL";
            var defenseTotal = HudElements.Text(
                defenseTotalText, 13f, MatchHudTheme.PrimaryText);
            defenseTotal.name = "gd-defense-defense-total";
            defenseTotal.style.unityFontStyleAndWeight = FontStyle.Bold;
            defenseTotal.style.marginTop = 3f;
            defenseTotal.style.marginBottom = 7f;
            preview.Add(defenseTotal);

            var resultColor = model.Outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? new Color(0.30f, 0.88f, 0.48f, 1f)
                : model.BlockResult == DefenseBlockResult.ConditionUnfulfilled
                    ? new Color(1f, 0.24f, 0.16f, 1f)
                    : new Color(1f, 0.63f, 0.18f, 1f);
            var result = HudElements.Text(model.ResultStatement, 14f, resultColor);
            result.name = "gd-defense-result";
            result.style.unityFontStyleAndWeight = FontStyle.Bold;
            result.style.unityTextAlign = TextAnchor.MiddleCenter;
            result.style.backgroundColor = new Color(
                resultColor.r * 0.12f,
                resultColor.g * 0.12f,
                resultColor.b * 0.12f,
                1f);
            HudElements.Padding(result, 6f);
            HudElements.Border(result, resultColor, 1f, 3f);
            preview.Add(result);
            preview.style.display = DisplayStyle.Flex;
            LayoutPreview();
        }

        private void AddDefenseSection(
            string heading,
            IReadOnlyList<DefenseContributionRowViewModel> contributionRows)
        {
            var section = HudElements.Text(heading, 10f, MatchHudTheme.MutedText);
            section.name = "gd-defense-section";
            section.style.unityFontStyleAndWeight = FontStyle.Bold;
            preview.Add(section);
            foreach (var contribution in contributionRows)
            {
                var row = HudElements.Row(8f);
                row.name = "gd-defense-contribution-row";
                row.style.justifyContent = Justify.SpaceBetween;
                var source = HudElements.Text(
                    contribution.SourceLabel, 11f, MatchHudTheme.PrimaryText);
                source.name = "gd-defense-contribution-source";
                source.style.flexGrow = 1f;
                var amountColor = contribution.Amount >= 0
                    ? new Color(0.30f, 0.90f, 0.48f, 1f)
                    : new Color(1f, 0.30f, 0.20f, 1f);
                var amount = HudElements.Text(contribution.SignedAmount, 12f, amountColor);
                amount.name = "gd-defense-contribution-amount";
                amount.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(source);
                row.Add(amount);
                preview.Add(row);
            }
        }

        public void ScheduleDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt < 0d)
            {
                dismissPreviewAt = now + PreviewDismissDelaySeconds;
            }
        }

        public void UpdateDefensePreviewDismissal(double now)
        {
            if (activeDefensePreview != null && dismissPreviewAt >= 0d && now >= dismissPreviewAt)
            {
                ClearDefensePreview();
            }
        }

        public void ClearDefensePreview()
        {
            activeDefensePreview = null;
            dismissPreviewAt = -1d;
            preview.Clear();
            preview.style.display = DisplayStyle.None;
        }

        private void LayoutPreview()
        {
            // Milestone 8.25c: the panel's rectangle in reference units, not the screen's in pixels.
            // The three style writes below are panel coordinates, so a device pixel here would put the
            // breakdown outside the panel on any screen that is not the reference one.
            var viewport = ConfiguredMatchHudRenderer.PanelViewport;
            var size = new Vector2(
                Mathf.Min(440f, Mathf.Max(240f, viewport.x - 24f)),
                Mathf.Min(240f, Mathf.Max(120f, viewport.y - 24f)));
            var rect = CalculateDefensePreviewRect(defensePreviewAnchor, viewport, size);
            preview.style.left = rect.x;
            preview.style.top = rect.y;
            preview.style.width = rect.width;
            preview.style.maxHeight = rect.height;
        }

        /// <summary>
        /// Where the breakdown sits: immediately right of the complete available-card fan and
        /// inside the viewport. A constrained right edge falls back to the fan's left.
        /// </summary>
        public static Rect CalculateDefensePreviewRect(Rect anchor, Vector2 viewport, Vector2 size)
        {
            var width = Mathf.Min(size.x, Mathf.Max(0f, viewport.x));
            var height = Mathf.Min(size.y, Mathf.Max(0f, viewport.y));
            var maximumX = Mathf.Max(0f, viewport.x - width);
            var right = anchor.xMax + 8f;
            var left = anchor.xMin - width - 8f;
            var x = right + width <= viewport.x
                ? right
                : left >= 0f
                    ? left
                    : Mathf.Clamp(right, 0f, maximumX);
            var y = Mathf.Clamp(anchor.yMin, 0f, Mathf.Max(0f, viewport.y - height));
            return new Rect(x, y, width, height);
        }
    }
}

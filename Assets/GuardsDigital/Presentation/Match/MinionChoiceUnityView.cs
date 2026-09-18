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
    /// Milestone 6.1a's Minion Battle removal prompt and milestone 6.4b's displaced-minion correction,
    /// which are one prompt with a family rather than two prompts.
    /// </summary>
    public sealed class MinionChoiceUnityView : IMinionChoiceView
    {
        private readonly VisualElement header;
        private readonly VisualElement controls;

        public MinionChoiceUnityView()
        {
            Root = new VisualElement { name = "gd-minion-choice" };
            Root.style.flexDirection = FlexDirection.Column;
            header = HudElements.Row(8f);
            header.style.marginBottom = 6f;
            controls = HudElements.Row(6f);
            Root.Add(header);
            Root.Add(controls);
        }

        public VisualElement Root { get; }

        public MinionChoiceViewModel Model { get; private set; }

        /// <summary>
        /// Milestone 8.46c. Which decision the DECISION WINDOW is drawing, or
        /// <see cref="DecisionWindowFamily.None"/> when it is drawing none; <c>null</c> in a
        /// composition with no window.
        ///
        /// <para>Rulebook p.8's Minion Battle became a single-verb ratification on the docked bar:
        /// `REMOVE THESE MINIONS` with the running count, and `NOT YET` for the one take-back the seat
        /// undo cannot replace. The `TEAM AGREEMENT` heading, the agree-or-withdraw wording and the
        /// per-hero ledger go with it. This prompt keeps its own header line and yields only the
        /// controls, and only while the window is actually drawing them - so a composition with no
        /// window still confirms a removal here.</para>
        /// </summary>
        public Func<DecisionWindowFamily> YieldedDecisionFamily { get; set; }

        private bool YieldsMinionBattle =>
            (YieldedDecisionFamily?.Invoke() ?? DecisionWindowFamily.None) ==
            DecisionWindowFamily.MinionBattleRemoval;

        /// <summary>
        /// The candidate the pointer or keyboard focus is currently on, or <c>null</c> when none is.
        /// </summary>
        public MinionChoiceCandidateViewModel HighlightedCandidate { get; private set; }

        /// <summary>The unit of the candidate being pointed at, undefined when none is.</summary>
        public RuleObject HighlightedUnit => HighlightedCandidate?.Unit ?? RuleObject.None;

        public event Action<MinionChoiceCandidateViewModel> CandidateSubmitted;
        public event Action ConfirmRequested;
        public event Action<TeamAgreementChoiceViewModel> AgreementRequested;
        public event Action<MinionChoiceCandidateViewModel> CandidateHighlighted;

        public void Render(MinionChoiceViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            // A highlight belongs to the model it was read from, exactly as the round view's target and
            // respawn highlights do; a rebuilt prompt drops it rather than keeping a cue for a control
            // that is no longer drawn.
            if (HighlightedCandidate != null &&
                Model?.Candidates.Any(value => ReferenceEquals(value, HighlightedCandidate)) != true)
            {
                SetHighlight(null);
            }

            Rebuild();
        }

        public void ForwardCandidate(MinionChoiceCandidateViewModel candidate)
        {
            if (candidate != null)
            {
                CandidateSubmitted?.Invoke(candidate);
            }
        }

        public void ForwardConfirm() => ConfirmRequested?.Invoke();

        /// <summary>Reports a hovered or keyboard-focused candidate directly, for a driver with no pointer.</summary>
        public void ForwardHighlight(MinionChoiceCandidateViewModel candidate) => SetHighlight(candidate);

        private void Rebuild()
        {
            header.Clear();
            controls.Clear();
            var model = Model;
            Root.style.display = model == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (model == null)
            {
                return;
            }

            var gutter = HudElements.Text(model.SectionLabel, 10f, MatchHudTheme.MutedText, FontStyle.Bold);
            gutter.style.width = 96f;
            header.Add(gutter);
            header.Add(HudElements.Text(model.PromptLabel, 12f, MatchHudTheme.PrimaryText, FontStyle.Bold));
            header.Add(HudElements.Text(model.StatusLabel, 11f, MatchHudTheme.MutedText));

            var spacer = new VisualElement();
            spacer.style.width = 96f;
            spacer.style.flexShrink = 0f;
            controls.Add(spacer);
            // Milestone 8.11a, at the user's request: every candidate is a piece standing on the board,
            // so the board is where it is chosen. Each one is marked where it stands - and only while
            // the authority says it may be selected, which is how rulebook p.8's Heavy-last ordering
            // stays honest - and clicking it selects or deselects it. The controls that used to name
            // them are gone; the confirm below stays, because agreeing to a completed selection is not
            // a thing anywhere on the map.
            if (model.ConfirmInvocation != null && !YieldsMinionBattle)
            {
                var confirm = HudElements.Control(
                    model.ConfirmInvocation.Label, () => ConfirmRequested?.Invoke());
                confirm.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                confirm.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                controls.Add(confirm);
            }

            if (model.Agreement != null && !YieldsMinionBattle)
            {
                var publicStatus = string.Join("  ·  ", model.Agreement.Participants.Select(participant =>
                    participant.Label + " " + participant.Status));
                controls.Add(HudElements.Text(
                    publicStatus, 10f, MatchHudTheme.MutedText, FontStyle.Normal));
                if (model.Agreement.Choices.Count > 0)
                {
                    foreach (var choice in model.Agreement.Choices)
                    {
                        var captured = choice;
                        var agreement = HudElements.Control(
                            choice.Label, () => AgreementRequested?.Invoke(captured));
                        agreement.style.backgroundColor = MatchHudTheme.ConfirmAccent;
                        agreement.style.color = new Color(0.09f, 0.094f, 0.102f, 1f);
                        controls.Add(agreement);
                    }
                }
                else if (!model.Agreement.IsInputEnabled)
                {
                    // Milestone 8.35z2, the same empty-command window `BuildTeamAgreement` draws for
                    // tied Initiative: the choosing side's own AGREE/WITHDRAW press is in flight and
                    // `AuthorizedCommands` is deliberately empty until the host's correlated projection
                    // returns. The non-choosing side keeps `IsInputEnabled` true and draws nothing here,
                    // exactly as before - it was never waiting on anything of its own.
                    controls.Add(HudElements.Text(
                        ActionUnavailabilityLabels.WaitingForHost,
                        10f, MatchHudTheme.MutedText, FontStyle.Bold));
                }
            }
        }

        private void SetHighlight(MinionChoiceCandidateViewModel candidate)
        {
            if (ReferenceEquals(HighlightedCandidate, candidate))
            {
                return;
            }

            HighlightedCandidate = candidate;
            CandidateHighlighted?.Invoke(candidate);
        }
    }
}

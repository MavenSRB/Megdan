using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionUnityView : IBoardInteractionView
    {
        private readonly IReadOnlyList<PrimitiveHeroView> heroes;
        private IReadOnlyList<StandardMinionView> minions;
        private readonly List<PlacedTokenView> tokens = new List<PlacedTokenView>();
        private readonly Func<PlacedToken, PlacedTokenView> tokenViewFactory;
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly Material highlightMaterial;
        private readonly Material resultingSpaceMaterial;
        private Func<HeroId, bool> heroIntentHandler;
        private Func<MinionId, bool> minionIntentHandler;
        private Func<GuardsDigital.Core.Board.HexCoordinate, bool> destinationIntentHandler;

        public BoardInteractionUnityView(
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<HexTileView> tiles,
            Material highlightMaterial,
            IEnumerable<StandardMinionView> minions = null,
            Material resultingSpaceMaterial = null,
            Func<PlacedToken, PlacedTokenView> tokenViewFactory = null)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.minions = Array.AsReadOnly((minions ?? Enumerable.Empty<StandardMinionView>())
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            this.highlightMaterial = highlightMaterial ?? throw new ArgumentNullException(nameof(highlightMaterial));
            // Milestone 6.5c: without its own material the resulting space cannot be drawn at all,
            // because painting a prediction in the destination color would say "click here".
            this.resultingSpaceMaterial = resultingSpaceMaterial;
            // Milestone 6.2d: where a view for a newly placed token comes from. A token has no
            // declared roster - its identity is minted when an effect places it - so unlike heroes and
            // minions its views cannot all exist before the match does. Left unbound, a projected
            // token faults rather than being quietly left undrawn.
            this.tokenViewFactory = tokenViewFactory;
        }

        public BoardInteractionViewModel Model { get; private set; }
        public IReadOnlyList<StandardMinionView> MinionViews => minions;

        /// <summary>Every token view this board has ever built, active or pooled.</summary>
        public IReadOnlyList<PlacedTokenView> TokenViews => tokens;

        /// <summary>The tiles this board paints, so a caller can read what is currently lit.</summary>
        public IReadOnlyList<HexTileView> TileViews => tiles;

        public event Action<HeroId> HeroSubmitted;
        public event Action<MinionId> MinionSubmitted;
        public event Action<GuardsDigital.Core.Board.HexCoordinate> DestinationSubmitted;

        public void BindIntentHandlers(
            Func<HeroId, bool> submitHero,
            Func<MinionId, bool> submitMinion,
            Func<GuardsDigital.Core.Board.HexCoordinate, bool> submitDestination)
        {
            heroIntentHandler = submitHero ?? throw new ArgumentNullException(nameof(submitHero));
            minionIntentHandler = submitMinion ?? throw new ArgumentNullException(nameof(submitMinion));
            destinationIntentHandler = submitDestination ??
                throw new ArgumentNullException(nameof(submitDestination));
        }

        public void Render(BoardInteractionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            var heroChoiceCues = model.HeroChoiceCues.ToDictionary(value => value.HeroId);
            foreach (var hero in heroes)
            {
                var occupancy = model.Heroes.SingleOrDefault(value =>
                    value.HeroId.Equals(hero.Occupancy.HeroId));
                if (!occupancy.HeroId.IsDefined)
                {
                    hero.SetChoiceCue(MinionChoiceCueKind.None);
                    hero.gameObject.SetActive(false);
                    continue;
                }

                // Hero views are retained across removal for Respawn, Undo, and restore, just as
                // minion views are retained across removal. Projection presence owns visibility;
                // an absent hero must not remain as a stale selectable board object.
                hero.Configure(occupancy, hero.OuterRadius);
                hero.gameObject.SetActive(true);

                // Milestone 6.5c: an effect-choice target is addressed by its space, so whichever
                // unit stands there wears the cue. The hero path exists because a Push may name one.
                hero.SetChoiceCue(
                    heroChoiceCues.TryGetValue(occupancy.HeroId, out var heroChoiceCue)
                        ? heroChoiceCue.Kind
                        : model.EffectChoiceTargetSpace.HasValue &&
                        occupancy.Coordinate.Equals(model.EffectChoiceTargetSpace.Value)
                        ? MinionChoiceCueKind.Highlighted
                        : MinionChoiceCueKind.None);
            }

            RenderMinions(model);
            RenderTokens(model);

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(model.MovementDestinations);
            targetCoordinates.UnionWith(model.FastTravelDestinations);
            // Milestone 6.4b highlighted every correction route endpoint and Place space at once;
            // milestone 6.2d lights the one whose control is being pointed at, because each of them
            // carries its own control and the board was saying nothing the controls did not already.
            if (model.CorrectionDestination.HasValue)
            {
                targetCoordinates.Add(model.CorrectionDestination.Value);
            }
            // Milestone 6.5c: an empty effect-choice candidate is a destination like any other, and
            // is the whole surface for that candidate - no control is drawn for it anywhere.
            targetCoordinates.UnionWith(model.EffectChoiceDestinations);
            if (model.HeroRespawnHighlight.HasValue)
            {
                targetCoordinates.Add(model.HeroRespawnHighlight.Value);
            }
            foreach (var targetId in model.AttackTargets)
            {
                var target = model.Heroes.Single(value => value.HeroId.Equals(targetId));
                targetCoordinates.Add(target.Coordinate);
            }
            foreach (var targetId in model.MinionAttackTargets)
            {
                var target = model.Minions.Single(value => value.MinionId.Equals(targetId));
                targetCoordinates.Add(target.Coordinate);
            }

            // The predicted landing space is drawn in its own color and is never selectable, so it is
            // resolved after the selectable set and can never be added to it.
            var resultingSpace = resultingSpaceMaterial == null
                ? null
                : model.EffectChoiceResultingSpace;
            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                if (resultingSpace.HasValue && tile.Coordinate.Equals(resultingSpace.Value) &&
                    !targetCoordinates.Contains(tile.Coordinate))
                {
                    tile.SetHighlighted(true, resultingSpaceMaterial);
                    continue;
                }

                var highlighted = targetCoordinates.Contains(tile.Coordinate);
                tile.SetHighlighted(highlighted, highlighted ? highlightMaterial : null);
            }
        }

        public void ReplaceMinionViews(IEnumerable<StandardMinionView> currentMinions)
        {
            minions = Array.AsReadOnly((currentMinions ??
                    throw new ArgumentNullException(nameof(currentMinions)))
                .Where(value => value != null)
                .Distinct()
                .ToArray());
            if (Model != null)
            {
                RenderMinions(Model);
            }
        }

        private void RenderMinions(BoardInteractionViewModel model)
        {
            var projectedMinions = model.Minions.ToDictionary(value => value.MinionId);
            var defenseCues = model.MinionDefenseCues.ToDictionary(value => value.MinionId, value => value.Amount);
            var choiceCues = model.MinionChoiceCues.ToDictionary(value => value.MinionId);
            foreach (var minion in minions)
            {
                var boundId = minion.BoundMinionId;
                if (boundId.IsDefined && projectedMinions.TryGetValue(boundId, out var state))
                {
                    minion.gameObject.SetActive(true);
                    if (!ReferenceEquals(minion.Occupancy, state))
                    {
                        minion.Configure(state, minion.OuterRadius);
                    }

                    minion.SetDefenseContributionCue(
                        defenseCues.TryGetValue(boundId, out var amount) ? amount : 0);
                    minion.SetChoiceCue(choiceCues.TryGetValue(boundId, out var choiceCue)
                        ? choiceCue.Kind
                        : model.EffectChoiceTargetSpace.HasValue &&
                            state.Coordinate.Equals(model.EffectChoiceTargetSpace.Value)
                            ? MinionChoiceCueKind.Highlighted
                            : MinionChoiceCueKind.None);

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.SetDefenseContributionCue(0);
                    minion.SetChoiceCue(MinionChoiceCueKind.None);
                    minion.gameObject.SetActive(false);
                }
            }

            if (projectedMinions.Count > 0)
            {
                throw new InvalidOperationException("The configured board has no view for a projected minion identity.");
            }
        }

        /// <summary>
        /// Milestone 6.2d: draws one piece per projected token and nothing else.
        ///
        /// The discipline is the minions': projection presence owns visibility, a view is never
        /// destroyed, and a projected identity with nowhere to go is a fault rather than a silently
        /// missing piece. What differs is where a view comes from. A minion's identity is declared in
        /// the setup, so every minion view exists before the match starts and stays bound to its own
        /// identity forever. A token's identity is minted the moment an effect places one, so views
        /// are pooled instead: a view is bound while its token is projected, released when it is not,
        /// and the pool grows only when more tokens stand at once than ever have before. That is what
        /// makes a Remove, a Clear and an Undo all reach the same place - the piece stops being drawn
        /// or starts again, and no identity outlives the projection that named it.
        ///
        /// A Replace needs no arm of its own. The outgoing token leaves the projection and the
        /// incoming one arrives in the same command, so the space is redrawn once, carrying the new
        /// definition's name; it is never drawn empty in between, which is the single-stage property
        /// the operation itself declares.
        /// </summary>
        private void RenderTokens(BoardInteractionViewModel model)
        {
            var projectedTokens = model.PlacedTokens.ToDictionary(value => value.Id);
            var released = new List<PlacedTokenView>();
            foreach (var token in tokens)
            {
                var boundId = token.BoundTokenId;
                if (boundId.IsDefined && projectedTokens.TryGetValue(boundId, out var state))
                {
                    token.gameObject.SetActive(true);
                    if (!ReferenceEquals(token.Token, state))
                    {
                        token.Configure(state, token.OuterRadius);
                    }

                    projectedTokens.Remove(boundId);
                }
                else
                {
                    token.gameObject.SetActive(false);
                    released.Add(token);
                }
            }

            var freeIndex = 0;
            foreach (var state in model.PlacedTokens.Where(value => projectedTokens.ContainsKey(value.Id)))
            {
                PlacedTokenView view;
                if (freeIndex < released.Count)
                {
                    view = released[freeIndex++];
                    view.Configure(state, view.OuterRadius);
                }
                else
                {
                    view = CreateTokenView(state);
                }

                view.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Asks the board for a view of a token it has never drawn. The factory owns the board's own
        /// outer radius and hands back a view already configured for this token, which is why nothing
        /// here has to know one.
        /// </summary>
        private PlacedTokenView CreateTokenView(PlacedToken state)
        {
            var created = tokenViewFactory?.Invoke(state);
            if (created == null)
            {
                throw new InvalidOperationException(
                    "The configured board has no view for a projected token identity.");
            }

            tokens.Add(created);
            return created;
        }

        public bool TryForwardHeroIntent(HeroId heroId)
        {
            if (heroIntentHandler != null)
            {
                return heroIntentHandler(heroId);
            }

            if (HeroSubmitted == null)
            {
                return false;
            }

            HeroSubmitted.Invoke(heroId);
            return true;
        }

        public bool TryForwardMinionIntent(MinionId minionId)
        {
            if (minionIntentHandler != null)
            {
                return minionIntentHandler(minionId);
            }

            if (MinionSubmitted == null)
            {
                return false;
            }

            MinionSubmitted.Invoke(minionId);
            return true;
        }

        public bool TryForwardDestinationIntent(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            if (destinationIntentHandler != null)
            {
                return destinationIntentHandler(destination);
            }

            if (DestinationSubmitted == null)
            {
                return false;
            }

            DestinationSubmitted.Invoke(destination);
            return true;
        }
    }

    public sealed class DirectorHandsUnityView : IDirectorHandsView
    {
        public DirectorHandsViewModel Model { get; private set; }

        public event Action<CardInstanceId> CardSelected;
        public event Action ConfirmRequested;
        public event Action<HeroId> PassRequested;

        public void Render(DirectorHandsViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            foreach (var hand in Model.Hands)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(hand.HeaderLabel, GUILayout.Width(122f));
                GUILayout.Label(hand.CommitmentLabel);
                if (hand.CanPass && GUILayout.Button("PASS - NO CARD", GUILayout.Width(125f), GUILayout.Height(24f)))
                {
                    PassRequested?.Invoke(hand.HeroId);
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                foreach (var card in hand.Cards)
                {
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = hand.SelectedCardInstanceId.HasValue &&
                        hand.SelectedCardInstanceId.Value.Equals(card.Id)
                            ? new Color(0.35f, 0.75f, 1f)
                            : Color.white;
                    if (GUILayout.Button(
                        hand.CardButtonLabels[card.Id],
                        GUILayout.MinWidth(112f),
                        GUILayout.Height(46f)))
                    {
                        CardSelected?.Invoke(card.Id);
                    }

                    GUI.backgroundColor = previous;
                }

                GUILayout.EndHorizontal();
            }

            var hasSelection = false;
            for (var index = 0; index < Model.Hands.Count; index++)
            {
                if (Model.Hands[index].SelectedCardInstanceId.HasValue)
                {
                    hasSelection = true;
                    break;
                }
            }

            if (hasSelection)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                if (GUILayout.Button("CONFIRM FACEDOWN", GUILayout.Width(170f), GUILayout.Height(38f)))
                {
                    ConfirmRequested?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
        }

    }

    public sealed class RoundActionUnityView : IRoundActionView
    {
        private const double PreviewDismissDelaySeconds = 0.12d;
        public static readonly Color DefensePreviewBackgroundColor =
            new Color(0.025f, 0.035f, 0.045f, 1f);
        private DefenseBreakdownViewModel activeDefensePreview;
        private Rect defensePreviewAnchor;
        private double dismissPreviewAt = -1d;

        public RoundActionViewModel Model { get; private set; }
        public DefenseBreakdownViewModel ActiveDefensePreview => activeDefensePreview;
        public Rect DefensePreviewAnchor => defensePreviewAnchor;

        public event Action<TypedInvocationViewModel> InvocationSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;

        /// <summary>Milestone 6.5c: the named target control that was activated.</summary>
        public event Action<EffectChoiceTargetViewModel> TargetSubmitted;

        /// <summary>
        /// The target control the pointer or keyboard focus is on, or null when none is. The board
        /// reads this to mark that object and light where the projected operation would leave it.
        /// </summary>
        public event Action<EffectChoiceTargetViewModel> TargetHighlighted;
        public event Action<HeroRespawnOptionViewModel> RespawnHighlighted;

        public EffectChoiceTargetViewModel HighlightedTarget { get; private set; }
        public HeroRespawnOptionViewModel HighlightedRespawn { get; private set; }

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

        private bool DrawEffectChoiceTarget(EffectChoiceTargetViewModel target, int index)
        {
            var controlName = $"EffectChoiceTarget-{Model.Version}-{index}";
            GUI.SetNextControlName(controlName);
            var pressed = GUILayout.Button(
                target.Label,
                GUILayout.MinWidth(CalculateContentWidth(target.Label, GUI.skin.button)),
                GUILayout.Height(46f));
            if (pressed)
            {
                ForwardTarget(target);
            }

            if (Event.current.type != EventType.Repaint)
            {
                return false;
            }

            var rect = GUILayoutUtility.GetLastRect();
            return rect.Contains(Event.current.mousePosition) ||
                string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal);
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

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            GUILayout.Label(Model.Status);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Actions", GUILayout.Width(54f));
            DrawInvocation(Model.ContinueEndOfRoundInvocation, 200f);
            foreach (var choice in Model.InitiativeChoices)
            {
                DrawInvocation(choice, 150f);
            }

            foreach (var action in Model.Actions)
            {
                DrawInvocation(action);
            }

            // Milestone 6.5af: an action the authority judged unstartable is drawn beside the ones
            // that can be started, disabled, for the reason a refused mode is - a player reads that
            // the action exists and cannot be taken, and reaches for another one or for Hold.
            foreach (var unavailable in Model.UnavailableActions)
            {
                DrawUnavailableAction(unavailable);
            }

            foreach (var fastTravel in Model.FastTravelOffers)
            {
                if (GUILayout.Button(fastTravel.Label, GUILayout.Height(46f)))
                {
                    ForwardFastTravelSelection(fastTravel);
                }
            }

            // The decision a paused effect frame waits on. It sits in the same row as the actions it
            // replaces, because the two are never offered at once.
            foreach (var effectChoice in Model.EffectChoices)
            {
                DrawInvocation(effectChoice, 170f);
            }

            // Milestone 6.5ac: an option the actor cannot perform is drawn beside the ones they can,
            // disabled. Showing it is the point - a player reads that the mode exists and that it
            // cannot be taken - and there is nothing to forward if it is clicked, because the view
            // model carries no command for it at all.
            foreach (var unavailable in Model.UnavailableEffectChoices)
            {
                DrawUnavailableEffectChoice(unavailable);
            }

            var hoveredRespawn = default(HeroRespawnOptionViewModel);
            for (var index = 0; index < Model.HeroRespawnOptions.Count; index++)
            {
                var option = Model.HeroRespawnOptions[index];
                var controlName = $"HeroRespawn-{Model.Version}-{index}";
                GUI.SetNextControlName(controlName);
                if (GUILayout.Button(
                    option.Label,
                    GUILayout.MinWidth(CalculateContentWidth(option.Label, GUI.skin.button)),
                    GUILayout.Height(46f)))
                {
                    InvocationSubmitted?.Invoke(new TypedInvocationViewModel(option.Label, option.Command));
                }

                if (Event.current.type == EventType.Repaint)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    if (rect.Contains(Event.current.mousePosition) ||
                        string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal))
                    {
                        hoveredRespawn = option;
                    }
                }
            }

            DrawInvocation(Model.ResolveWithoutRespawn, 190f);

            // Milestone 6.5c: one control per target the choice names, reporting which one the
            // pointer or keyboard focus is on so the board can mark it and show where it would land.
            // Nothing is drawn for a destination candidate - the lit board is its control.
            var hoveredTarget = default(EffectChoiceTargetViewModel);
            for (var index = 0; index < Model.EffectChoiceTargets.Count; index++)
            {
                if (DrawEffectChoiceTarget(Model.EffectChoiceTargets[index], index))
                {
                    hoveredTarget = Model.EffectChoiceTargets[index];
                }
            }

            GUILayout.FlexibleSpace();
            DrawInvocation(Model.EndTurnInvocation, 130f);
            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                SetTargetHighlight(hoveredTarget);
                SetRespawnHighlight(hoveredRespawn);
            }

            if (Model.DefenseChoices.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Defense", GUILayout.Width(54f));
                var previewActiveThisEvent = false;
                for (var index = 0; index < Model.DefenseChoices.Count; index++)
                {
                    previewActiveThisEvent |= DrawContentSizedDefenseInvocation(
                        Model.DefenseChoices[index],
                        index);
                }

                GUILayout.EndHorizontal();
                if (Event.current.type == EventType.Repaint && !previewActiveThisEvent)
                {
                    ScheduleDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
                }
            }
            else
            {
                ClearDefensePreview();
            }

            UpdateDefensePreviewDismissal(Time.realtimeSinceStartupAsDouble);
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

        private void DrawInvocation(TypedInvocationViewModel invocation, float width)
        {
            if (invocation != null && GUILayout.Button(invocation.Label, GUILayout.Width(width), GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
        }

        private void DrawInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation != null && GUILayout.Button(invocation.Label, GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
        }

        /// <summary>
        /// One refused option, drawn as a disabled control. <c>GUI.enabled</c> is restored rather than
        /// assigned back to true, so this cannot silently re-enable a row an enclosing scope had
        /// already disabled.
        /// </summary>
        private void DrawUnavailableAction(UnavailableActionViewModel action)
        {
            if (action == null)
            {
                return;
            }

            // `GUI.enabled` is restored rather than assigned true, so this cannot re-enable a row an
            // enclosing scope had disabled - the same care the refused-mode row takes.
            var wasEnabled = GUI.enabled;
            GUI.enabled = false;
            GUILayout.Button(
                action.Label,
                GUILayout.MinWidth(CalculateContentWidth(action.Label, GUI.skin.button)),
                GUILayout.Height(46f));
            GUI.enabled = wasEnabled;
        }

        private void DrawUnavailableEffectChoice(UnavailableEffectChoiceViewModel option)
        {
            if (option == null)
            {
                return;
            }

            var wasEnabled = GUI.enabled;
            GUI.enabled = false;
            GUILayout.Button(
                option.Label,
                GUILayout.MinWidth(CalculateContentWidth(option.Label, GUI.skin.button)),
                GUILayout.Height(46f));
            GUI.enabled = wasEnabled;
        }

        private bool DrawContentSizedDefenseInvocation(TypedInvocationViewModel invocation, int index)
        {
            if (invocation == null)
            {
                return false;
            }

            var minimumWidth = CalculateContentWidth(invocation.Label, GUI.skin.button);
            var controlName = $"DefenseChoice-{Model.Version}-{index}";
            GUI.SetNextControlName(controlName);
            if (GUILayout.Button(
                invocation.Label,
                GUILayout.MinWidth(minimumWidth),
                GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }

            if (Event.current.type != EventType.Repaint || invocation.DefenseBreakdown == null)
            {
                return false;
            }

            var localRect = GUILayoutUtility.GetLastRect();
            var screenTopLeft = GUIUtility.GUIToScreenPoint(localRect.position);
            var screenRect = new Rect(screenTopLeft, localRect.size);
            var active = localRect.Contains(Event.current.mousePosition) ||
                string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal);
            if (active)
            {
                ShowDefensePreview(invocation.DefenseBreakdown, screenRect);
            }

            return active;
        }

        public void ShowDefensePreview(DefenseBreakdownViewModel preview, Rect anchorScreenRect)
        {
            activeDefensePreview = preview ?? throw new ArgumentNullException(nameof(preview));
            defensePreviewAnchor = anchorScreenRect;
            dismissPreviewAt = -1d;
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

        public void DrawDefensePreviewOverlay()
        {
            if (activeDefensePreview == null)
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontSize = 12,
                richText = false
            };
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
            var lines = activeDefensePreview.Text.Split('\n');
            var columnCount = lines.Length > 12 ? 2 : 1;
            var rowsPerColumn = Mathf.CeilToInt(lines.Length / (float)columnCount);
            var columns = Enumerable.Range(0, columnCount)
                .Select(column => string.Join("\n", lines
                    .Skip(column * rowsPerColumn)
                    .Take(rowsPerColumn)))
                .ToArray();
            var width = columnCount == 1 ? 440f : 680f;
            var columnWidth = (width - 24f) / columnCount;
            var textHeight = columns.Max(column => style.CalcHeight(
                new GUIContent(column),
                columnWidth));
            var preferred = new Vector2(width, textHeight + 44f);
            var rect = CalculateDefensePreviewRect(
                defensePreviewAnchor,
                new Vector2(Screen.width, Screen.height),
                preferred);
            var previousBackgroundColor = GUI.color;
            GUI.color = DefensePreviewBackgroundColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousBackgroundColor;
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f),
                activeDefensePreview.Title, titleStyle);
            var body = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, rect.height - 40f);
            var renderedColumnWidth = body.width / columnCount;
            for (var column = 0; column < columns.Length; column++)
            {
                GUI.Label(new Rect(
                    body.x + (column * renderedColumnWidth),
                    body.y,
                    renderedColumnWidth,
                    body.height),
                    columns[column],
                    style);
            }
        }

        public static Rect CalculateDefensePreviewRect(
            Rect anchor,
            Vector2 viewport,
            Vector2 preferredSize)
        {
            const float margin = 8f;
            var width = Mathf.Min(preferredSize.x, Mathf.Max(0f, viewport.x - (margin * 2f)));
            var height = Mathf.Min(preferredSize.y, Mathf.Max(0f, viewport.y - (margin * 2f)));
            var above = anchor.y - margin;
            var below = viewport.y - anchor.yMax - margin;
            var right = viewport.x - anchor.xMax - margin;
            var left = anchor.x - margin;

            if (above >= height)
            {
                return new Rect(
                    Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                    anchor.y - height - margin,
                    width,
                    height);
            }

            if (right >= width)
            {
                return new Rect(anchor.xMax + margin,
                    Mathf.Clamp(anchor.center.y - (height * 0.5f), margin, viewport.y - height - margin),
                    width, height);
            }

            if (left >= width)
            {
                return new Rect(anchor.x - width - margin,
                    Mathf.Clamp(anchor.center.y - (height * 0.5f), margin, viewport.y - height - margin),
                    width, height);
            }

            if (below >= height)
            {
                return new Rect(
                    Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                    anchor.yMax + margin,
                    width,
                    height);
            }

            var verticalSpace = Mathf.Max(above, below);
            var boundedHeight = Mathf.Min(height, verticalSpace);
            var y = above >= below ? anchor.y - boundedHeight - margin : anchor.yMax + margin;
            return new Rect(
                Mathf.Clamp(anchor.center.x - (width * 0.5f), margin, viewport.x - width - margin),
                y,
                width,
                boundedHeight);
        }

        private void ClearDefensePreview()
        {
            activeDefensePreview = null;
            dismissPreviewAt = -1d;
        }

        public static float CalculateContentWidth(string label, GUIStyle style)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            return Mathf.Ceil(style.CalcSize(new GUIContent(label)).x);
        }
    }

    /// <summary>
    /// The configured contextual prompt for a pending minion choice. It draws one control per
    /// candidate the authority currently offers a legal invocation for, reports which control the
    /// pointer or keyboard focus is on so the board can mark that minion, and forwards the projected
    /// typed invocation on click. It holds no session and decides no legality.
    /// </summary>
    public sealed class MinionChoiceUnityView : IMinionChoiceView
    {
        public MinionChoiceViewModel Model { get; private set; }

        /// <summary>
        /// The candidate the pointer or keyboard focus is currently on, or <c>null</c> when none is.
        ///
        /// Milestone 6.2d reports the whole candidate rather than its unit alone. Every candidate of a
        /// correction destination prompt shares the one displaced subject and is told apart by its
        /// destination, so reporting the unit said the same thing for every control - which is why the
        /// board could only light all of them or none.
        /// </summary>
        public MinionChoiceCandidateViewModel HighlightedCandidate { get; private set; }

        /// <summary>The unit of the candidate being pointed at, undefined when none is.</summary>
        public RuleObject HighlightedUnit =>
            HighlightedCandidate?.Unit ?? RuleObject.None;

        public event Action<MinionChoiceCandidateViewModel> CandidateSubmitted;
        public event Action ConfirmRequested;
        public event Action<MinionChoiceCandidateViewModel> CandidateHighlighted;

        public void Render(MinionChoiceViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model;
            // A highlight belongs to the model it was read from, exactly as the round view's target
            // and respawn highlights do; a rebuilt prompt drops it rather than keeping a cue for a
            // control that is no longer drawn.
            if (HighlightedCandidate != null &&
                Model?.Candidates.Any(value => ReferenceEquals(value, HighlightedCandidate)) != true)
            {
                SetHighlight(null);
            }
        }

        public void Draw()
        {
            // The model is read once: forwarding an invocation below refreshes the presenters inside
            // this same draw, and the loop must keep walking the controls it started with.
            var model = Model;
            if (model == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            // Milestone 6.4b: the gutter names the prompt family from the model - "Minion Battle" or
            // "Correction" - so the one shared view carries no family literal of its own.
            GUILayout.Label(model.SectionLabel, GUILayout.Width(84f));
            GUILayout.Label($"{model.PromptLabel}   {model.StatusLabel}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(84f);
            var hovered = default(MinionChoiceCandidateViewModel);
            for (var index = 0; index < model.Candidates.Count; index++)
            {
                if (DrawCandidate(model, index))
                {
                    hovered = model.Candidates[index];
                }
            }

            if (model.ConfirmInvocation != null && GUILayout.Button(
                model.ConfirmInvocation.Label,
                GUILayout.MinWidth(RoundActionUnityView.CalculateContentWidth(
                    model.ConfirmInvocation.Label,
                    GUI.skin.button)),
                GUILayout.Height(46f)))
            {
                ConfirmRequested?.Invoke();
            }

            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                SetHighlight(hovered);
            }
        }

        public void ForwardCandidate(MinionChoiceCandidateViewModel candidate)
        {
            if (candidate != null)
            {
                CandidateSubmitted?.Invoke(candidate);
            }
        }

        public void ForwardConfirm()
        {
            ConfirmRequested?.Invoke();
        }

        /// <summary>Reports a hovered or keyboard-focused candidate directly, for a driver with no pointer.</summary>
        public void ForwardHighlight(MinionChoiceCandidateViewModel candidate)
        {
            SetHighlight(candidate);
        }

        private bool DrawCandidate(MinionChoiceViewModel model, int index)
        {
            var candidate = model.Candidates[index];
            var controlName = $"MinionChoice-{model.Version}-{index}";
            GUI.SetNextControlName(controlName);
            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = candidate.IsSelected
                ? new Color(0.95f, 0.62f, 0.25f)
                : previousBackground;
            var pressed = GUILayout.Button(
                candidate.Label,
                GUILayout.MinWidth(RoundActionUnityView.CalculateContentWidth(candidate.Label, GUI.skin.button)),
                GUILayout.Height(46f));
            GUI.backgroundColor = previousBackground;
            if (pressed)
            {
                ForwardCandidate(candidate);
            }

            if (Event.current.type != EventType.Repaint)
            {
                return false;
            }

            var rect = GUILayoutUtility.GetLastRect();
            return rect.Contains(Event.current.mousePosition) ||
                string.Equals(GUI.GetNameOfFocusedControl(), controlName, StringComparison.Ordinal);
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

    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;
        public event Action RestartRequested;

        public void Render(UndoRestartViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Draw()
        {
            if (Model == null)
            {
                return;
            }

            GUI.enabled = Model.CanUndo;
            if (GUILayout.Button("UNDO", GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                UndoRequested?.Invoke();
            }

            GUI.enabled = Model.CanRestart;
            if (GUILayout.Button("RESTART", GUILayout.Width(90f), GUILayout.Height(24f)))
            {
                RestartRequested?.Invoke();
            }

            GUI.enabled = true;
        }
    }

    public sealed class DiagnosticLogUnityView : IDiagnosticLogView
    {
        private Vector2 scrollPosition;

        public DiagnosticLogViewModel Model { get; private set; }

        public event Action ToggleRequested;

        public void Render(DiagnosticLogViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void DrawToggle()
        {
            if (Model != null && GUILayout.Button(
                Model.ToggleLabel,
                GUILayout.Width(126f),
                GUILayout.Height(24f)))
            {
                ToggleRequested?.Invoke();
            }
        }

        public void DrawPanel(Rect panel)
        {
            if (Model == null || !Model.IsExpanded)
            {
                return;
            }

            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));
            GUILayout.Label(Model.PanelLabel);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var entry in Model.Entries)
            {
                GUILayout.Label(entry.DisplayText);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

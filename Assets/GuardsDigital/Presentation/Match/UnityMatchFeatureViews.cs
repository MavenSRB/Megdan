using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly Material highlightMaterial;
        private Func<HeroId, bool> heroIntentHandler;
        private Func<MinionId, bool> minionIntentHandler;
        private Func<GuardsDigital.Core.Board.HexCoordinate, bool> destinationIntentHandler;

        public BoardInteractionUnityView(
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<HexTileView> tiles,
            Material highlightMaterial,
            IEnumerable<StandardMinionView> minions = null)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.minions = Array.AsReadOnly((minions ?? Enumerable.Empty<StandardMinionView>())
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            this.highlightMaterial = highlightMaterial ?? throw new ArgumentNullException(nameof(highlightMaterial));
        }

        public BoardInteractionViewModel Model { get; private set; }
        public IReadOnlyList<StandardMinionView> MinionViews => minions;

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
            foreach (var hero in heroes)
            {
                var occupancy = model.Heroes.SingleOrDefault(value =>
                    value.HeroId.Equals(hero.Occupancy.HeroId));
                if (occupancy.HeroId.IsDefined)
                {
                    hero.Configure(occupancy, hero.OuterRadius);
                }
            }

            RenderMinions(model);

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(model.MovementDestinations);
            targetCoordinates.UnionWith(model.FastTravelDestinations);
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

            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
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
            foreach (var minion in minions)
            {
                var boundId = minion.BoundMinionId;
                if (boundId.IsDefined && projectedMinions.TryGetValue(boundId, out var state))
                {
                    minion.gameObject.SetActive(true);
                    if (!ReferenceEquals(minion.SetupState, state))
                    {
                        minion.Configure(state, minion.OuterRadius);
                    }

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.gameObject.SetActive(false);
                }
            }

            if (projectedMinions.Count > 0)
            {
                throw new InvalidOperationException("The configured board has no view for a projected minion identity.");
            }
        }

        public void ForwardHeroIntent(HeroId heroId)
        {
            TryForwardHeroIntent(heroId);
        }

        public void ForwardDestinationIntent(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            TryForwardDestinationIntent(destination);
        }

        public void ForwardMinionIntent(MinionId minionId)
        {
            TryForwardMinionIntent(minionId);
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
        public RoundActionViewModel Model { get; private set; }

        public event Action<TypedInvocationViewModel> InvocationSubmitted;
        public event Action<FastTravelOfferViewModel> FastTravelSelected;

        public void Render(RoundActionViewModel model)
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

            foreach (var fastTravel in Model.FastTravelOffers)
            {
                if (GUILayout.Button(fastTravel.Label, GUILayout.Height(46f)))
                {
                    ForwardFastTravelSelection(fastTravel);
                }
            }

            GUILayout.FlexibleSpace();
            DrawInvocation(Model.EndTurnInvocation, 130f);
            GUILayout.EndHorizontal();

            if (Model.DefenseChoices.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Defense", GUILayout.Width(54f));
                foreach (var defense in Model.DefenseChoices)
                {
                    DrawContentSizedDefenseInvocation(defense);
                }

                GUILayout.EndHorizontal();
            }
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

        private void DrawContentSizedDefenseInvocation(TypedInvocationViewModel invocation)
        {
            if (invocation == null)
            {
                return;
            }

            var minimumWidth = CalculateContentWidth(invocation.Label, GUI.skin.button);
            if (GUILayout.Button(
                invocation.Label,
                GUILayout.MinWidth(minimumWidth),
                GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
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

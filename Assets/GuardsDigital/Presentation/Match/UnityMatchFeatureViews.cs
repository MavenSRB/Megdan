using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionUnityView : IBoardInteractionView
    {
        private readonly IReadOnlyList<PrimitiveHeroView> heroes;
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly Material highlightMaterial;

        public BoardInteractionUnityView(
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<HexTileView> tiles,
            Material highlightMaterial)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            this.highlightMaterial = highlightMaterial ?? throw new ArgumentNullException(nameof(highlightMaterial));
        }

        public BoardInteractionViewModel Model { get; private set; }

        public event Action<HeroId> HeroSubmitted;
        public event Action<GuardsDigital.Core.Board.HexCoordinate> DestinationSubmitted;

        public void Render(BoardInteractionViewModel model)
        {
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

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(model.MovementDestinations);
            foreach (var targetId in model.AttackTargets)
            {
                var target = model.Heroes.Single(value => value.HeroId.Equals(targetId));
                targetCoordinates.Add(target.Coordinate);
            }

            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                var highlighted = targetCoordinates.Contains(tile.Coordinate);
                tile.SetHighlighted(highlighted, highlighted ? highlightMaterial : null);
            }
        }

        public void ForwardHeroIntent(HeroId heroId)
        {
            HeroSubmitted?.Invoke(heroId);
        }

        public void ForwardDestinationIntent(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            DestinationSubmitted?.Invoke(destination);
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
                GUILayout.Label(
                    hand.TeamSide == TeamSide.Red
                        ? $"[R] RED {hand.HeroDisplayName.ToUpperInvariant()}"
                        : $"[B] BLUE {hand.HeroDisplayName.ToUpperInvariant()}",
                    GUILayout.Width(122f));
                GUILayout.Label(FormatCommitment(hand));
                if (hand.CanPass && GUILayout.Button("PASS - NO CARD", GUILayout.Width(125f), GUILayout.Height(24f)))
                {
                    PassRequested?.Invoke(hand.HeroId);
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(122f);
                foreach (var card in hand.Cards)
                {
                    var definition = hand.Definitions.Single(value =>
                        value.Reference.Equals(card.DefinitionReference));
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = hand.SelectedCardInstanceId.HasValue &&
                        hand.SelectedCardInstanceId.Value.Equals(card.Id)
                            ? new Color(0.35f, 0.75f, 1f)
                            : Color.white;
                    if (GUILayout.Button(
                        $"{definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}",
                        GUILayout.MinWidth(112f),
                        GUILayout.Height(46f)))
                    {
                        CardSelected?.Invoke(card.Id);
                    }

                    GUI.backgroundColor = previous;
                }

                GUILayout.EndHorizontal();
            }

            if (Model.Hands.Any(hand => hand.SelectedCardInstanceId.HasValue))
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

        private static string FormatCommitment(DirectorHandViewModel hand)
        {
            switch (hand.CommitmentStatus)
            {
                case TurnCardCommitmentStatus.AwaitingResponse:
                    return "AWAITING CARD";
                case TurnCardCommitmentStatus.CardCommittedFacedown:
                    return "FACEDOWN COMMITTED";
                case TurnCardCommitmentStatus.Passed:
                    return "PASS - NO CARD";
                case TurnCardCommitmentStatus.Revealed:
                    if (hand.RevealedCard != null)
                    {
                        var definition = hand.Definitions.Single(value =>
                            value.Reference.Equals(hand.RevealedCard.DefinitionReference));
                        return $"REVEALED: {definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}";
                    }

                    return "REVEALED";
                default:
                    return hand.CommitmentStatus.ToString().ToUpperInvariant();
            }
        }
    }

    public sealed class RoundActionUnityView : IRoundActionView
    {
        public RoundActionViewModel Model { get; private set; }

        public event Action<TypedInvocationViewModel> InvocationSubmitted;

        public void Render(RoundActionViewModel model)
        {
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
                DrawInvocation(action, 130f);
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
                    DrawInvocation(defense, 150f);
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

        private void DrawInvocation(TypedInvocationViewModel invocation, float width)
        {
            if (invocation != null && GUILayout.Button(invocation.Label, GUILayout.Width(width), GUILayout.Height(46f)))
            {
                ForwardInvocation(invocation);
            }
        }
    }

    public sealed class UndoRestartUnityView : IUndoRestartView
    {
        public UndoRestartViewModel Model { get; private set; }

        public event Action UndoRequested;
        public event Action RestartRequested;

        public void Render(UndoRestartViewModel model)
        {
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
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void DrawToggle()
        {
            if (Model != null && GUILayout.Button(
                Model.IsExpanded ? "HIDE EVENT LOG" : $"EVENT LOG ({Model.Entries.Count})",
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
            GUILayout.Label($"INTERNAL RULE-EVENT LOG - {Model.Entries.Count} ACCEPTED EVENTS");
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

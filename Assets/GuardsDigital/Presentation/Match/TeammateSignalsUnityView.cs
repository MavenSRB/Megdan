using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>Pointer-transparent communication overlays, independent of every rules highlight.</summary>
    public sealed class TeammateSignalsUnityView : MonoBehaviour
    {
        private ConfiguredMatchSceneRuntime runtime;
        private Camera boardCamera;
        private HexTileView[] tiles;
        private VisualElement layer;
        private VisualElement cardLayer;
        private VisualElement inspectionAnchor;
        private VisualElement inspectionRow;
        private readonly List<(VisualElement Anchor, VisualElement Badges)> cardBadges =
            new List<(VisualElement, VisualElement)>();
        private long renderedRevision = -1;
        private long renderedVersion = -1;
        private readonly List<Signal> signals = new List<Signal>();
        private sealed class Signal
        {
            public HexTileView Tile;
            public VisualElement Ring;
            public VisualElement Badges;
            public Vector2[] Points = new Vector2[6];
        }
        public static void Attach(GameObject owner, ConfiguredMatchSceneRuntime runtime,
            ConfiguredMatchHudRenderer hud, Camera camera, IEnumerable<HexTileView> tiles)
        {
            var view = owner.GetComponent<TeammateSignalsUnityView>();
            if (view == null) view = owner.AddComponent<TeammateSignalsUnityView>();
            view.layer?.RemoveFromHierarchy();
            view.cardLayer?.RemoveFromHierarchy();
            view.cardBadges.Clear();
            view.inspectionAnchor = null; view.inspectionRow = null;
            view.runtime = runtime; view.boardCamera = camera; view.tiles = tiles.ToArray();
            view.renderedRevision = -1; view.renderedVersion = -1;
            view.layer = new VisualElement { name = "teammate-board-signals", pickingMode = PickingMode.Ignore };
            view.layer.style.position = Position.Absolute;
            view.layer.style.left = 0; view.layer.style.right = 0; view.layer.style.top = 0; view.layer.style.bottom = 0;
            hud.RootElement.Add(view.layer);
            view.cardLayer = new VisualElement { name = "teammate-card-badges", pickingMode = PickingMode.Ignore };
            view.cardLayer.style.position = Position.Absolute;
            view.cardLayer.style.left = 0; view.cardLayer.style.right = 0;
            view.cardLayer.style.top = 0; view.cardLayer.style.bottom = 0;
            hud.RootElement.Add(view.cardLayer);
        }
        private void OnDestroy()
        { layer?.RemoveFromHierarchy(); cardLayer?.RemoveFromHierarchy(); }
        private void LateUpdate()
        {
            if (layer?.panel == null || runtime == null || boardCamera == null) return;
            var read = runtime.ReadModel;
            var experimental = read as IExperimentalReadModel;
            bool enabled = experimental?.Experiments.TeammateCoordination == true || experimental?.Experiments.TurnTimer == true;
            layer.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            cardLayer.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            if (!enabled) return;
            if (renderedRevision != experimental.CoordinationRevision + experimental.Clock.Revision + experimental.Clock.BoardVotes.Count || renderedVersion != read.Version)
            {
                renderedRevision = experimental.CoordinationRevision + experimental.Clock.Revision + experimental.Clock.BoardVotes.Count; renderedVersion = read.Version;
                layer.Clear(); signals.Clear();
                foreach (var group in experimental.TeammateMarkers.Concat(experimental.Clock.BoardVotes.Select(v=>new TeammateMarker(v.Voter,0,v.Position,default,default))).Where(m => m.Hex.HasValue).GroupBy(m => m.Hex.Value))
                {
                    var tile = tiles.FirstOrDefault(t => t != null && t.Coordinate.Equals(group.Key));
                    if (tile == null) continue;
                    var signal = new Signal { Tile = tile,
                        Ring = new VisualElement { name = "teammate-hex-outline", pickingMode = PickingMode.Ignore },
                        Badges = new VisualElement { name = "teammate-hex-owners", pickingMode = PickingMode.Ignore } };
                    signal.Ring.style.position = Position.Absolute;
                    signal.Ring.style.left = 0; signal.Ring.style.top = 0;
                    signal.Ring.style.width = Length.Percent(100); signal.Ring.style.height = Length.Percent(100);
                    signal.Ring.generateVisualContent += context =>
                    {
                        var painter = context.painter2D;
                        painter.lineWidth = 2f; painter.strokeColor = Color.white;
                        painter.BeginPath(); painter.MoveTo(signal.Points[0]);
                        for (var i = 1; i < 6; i++) painter.LineTo(signal.Points[i]);
                        painter.ClosePath(); painter.Stroke();
                    };
                    signal.Badges.style.position = Position.Absolute;
                    signal.Badges.style.flexDirection = FlexDirection.Row;
                    foreach (var marker in group.GroupBy(m=>m.Sender).Select(g=>g.First()).OrderBy(m => m.Sender.Value, StringComparer.Ordinal))
                    {
                        var hero = runtime.HeroOverviewsView?.Model?.Heroes.FirstOrDefault(h => h.HeroId.Equals(marker.Sender));
                        if (hero == null) continue;
                        var badge = HeroOverviewsUnityView.BuildSharedHeroPortrait(hero, MatchHudTheme.TeamFace(hero.TeamSide), 0.62f);
                        badge.pickingMode = PickingMode.Ignore;
                        // Every child is decorative; an icon must never intercept a board gesture.
                        foreach (var child in badge.Children()) child.pickingMode = PickingMode.Ignore;
                        signal.Badges.Add(badge);
                    }
                    layer.Add(signal.Ring); layer.Add(signal.Badges); signals.Add(signal);
                }
            }
            if (inspectionAnchor != null && (inspectionAnchor.panel == null || runtime.HandsView.Model?.ViewedHand?.IsReadOnly != true))
            { inspectionRow.RemoveFromHierarchy(); inspectionAnchor = null; inspectionRow = null; }
            var newInspection = runtime.HandsView.Root.Q("allied-hand-inspection");
            if (newInspection != null)
            {
                inspectionRow?.RemoveFromHierarchy();
                inspectionRow = newInspection; inspectionAnchor = newInspection.parent;
                cardLayer.Add(inspectionRow);
            }
            if (inspectionAnchor != null)
            {
                var point = cardLayer.WorldToLocal(inspectionAnchor.worldBound.position);
                inspectionRow.style.left = point.x + 140;
                inspectionRow.style.top = point.y - 72;
            }
            // The hand lives inside a ScrollView; badges above a card must escape its clip,
            // while the card outline remains attached to the original slot and rules highlight.
            foreach (var entry in cardBadges.ToArray())
                if (entry.Anchor.panel == null)
                { entry.Badges.RemoveFromHierarchy(); cardBadges.Remove(entry); }
            foreach (var badges in runtime.HandsView.Root.Query<VisualElement>("teammate-card-owners").ToList())
            {
                var anchor = badges.parent;
                cardLayer.Add(badges);
                cardBadges.Add((anchor, badges));
            }
            foreach (var entry in cardBadges)
            {
                var point = cardLayer.WorldToLocal(entry.Anchor.worldBound.position);
                entry.Badges.style.left = point.x + 8;
                entry.Badges.style.top = point.y - 18;
            }
            foreach (var signal in signals)
            {
                if (signal.Tile == null) continue;
                var radius = signal.Tile.OuterRadius * 1.07f;
                for (var i = 0; i < 6; i++)
                {
                    var angle = (30f + 60f * i) * Mathf.Deg2Rad;
                    var world = signal.Tile.transform.TransformPoint(new Vector3(radius * Mathf.Cos(angle), 0.05f, radius * Mathf.Sin(angle)));
                    signal.Points[i] = PanelPoint(world);
                }
                var centre = PanelPoint(signal.Tile.transform.position);
                signal.Badges.style.left = centre.x - signal.Badges.childCount * 17.36f;
                signal.Badges.style.top = signal.Points.Min(p => p.y) - 31;
                signal.Ring.MarkDirtyRepaint();
            }
        }
        private Vector2 PanelPoint(Vector3 world)
        {
            var screen = boardCamera.WorldToScreenPoint(world);
            return RuntimePanelUtils.ScreenToPanel(layer.panel, new Vector2(screen.x, Screen.height - screen.y));
        }
    }
}

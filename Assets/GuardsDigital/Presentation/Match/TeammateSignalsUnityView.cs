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
        /// <summary>Milestone 9.89. Ping perimeters are intentionally heavier than cursor and vote lines.</summary>
        public const float PingSteadyLineWidth = 4f;

        public const float PingWaveLineWidth = 4.5f;

        /// <summary>Milestone 9.89. The restored ping owner is smaller than 9.58's 0.62-scale badge.</summary>
        public const float PingOwnerPortraitScale = 0.42f;

        private const float PingOwnerPortraitGap = 4f;

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
        private string renderedVotes;
        private readonly List<Signal> signals = new List<Signal>();
        private sealed class Signal
        {
            public HexTileView Tile;
            public Color[] Colours;
            public bool Cursor;
            public bool Vote;
            public Vector2 Centre;
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
            // Board decorations belong behind every HUD surface, including nested drawers.
            // Appending here paints them over windows already composed by the HUD.
            hud.RootElement.Insert(0, view.layer);
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
            var votes=runtime.DisplayClock.BoardVotes;
            var voteIdentity=string.Join(";",votes.Select(v=>v.Voter.Value+":"+v.Position.Q+":"+v.Position.R));
            if (renderedRevision != experimental.CoordinationRevision || renderedVersion != read.Version || renderedVotes!=voteIdentity)
            {
                renderedRevision = experimental.CoordinationRevision; renderedVersion = read.Version; renderedVotes=voteIdentity;
                foreach (var entry in cardBadges) entry.Badges.RemoveFromHierarchy();
                cardBadges.Clear();
                runtime.HandsView.RefreshTeammateSignals();
                layer.Clear(); signals.Clear();
                BuildSignals(experimental.TeammateMarkers.Where(m => m.Hex.HasValue), false, false);
                BuildSignals(experimental.TeammateMarkers.Where(m => m.Cursor.HasValue), true, false);
                BuildSignals(votes.Select(v => new TeammateMarker(v.Voter, 0, v.Position, default, default)), false, true);
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
                // The full hover face owns its own signal; hide the badge of its transparent anchor.
                entry.Badges.style.display = entry.Anchor.parent?.resolvedStyle.opacity == 0f
                    ? DisplayStyle.None : DisplayStyle.Flex;
                var point = cardLayer.WorldToLocal(entry.Anchor.worldBound.position);
                entry.Badges.style.left = point.x + 8;
                entry.Badges.style.top = point.y - 18;
            }
            foreach (var signal in signals)
            {
                if (signal.Tile == null) continue;
                var radius = signal.Tile.OuterRadius;
                for (var i = 0; i < 6; i++)
                {
                    var angle = (30f + 60f * i) * Mathf.Deg2Rad;
                    var world = signal.Tile.transform.TransformPoint(new Vector3(radius * Mathf.Cos(angle), 0.05f, radius * Mathf.Sin(angle)));
                    signal.Points[i] = PanelPoint(world);
                }
                var centre = PanelPoint(signal.Tile.transform.position);
                signal.Centre = centre;
                var badgeWidth = signal.Badges.resolvedStyle.width;
                if (float.IsNaN(badgeWidth) || badgeWidth <= 0f)
                    badgeWidth = signal.Badges.Children().Sum(badge => badge.style.width.value.value);
                var badgeHeight = signal.Badges.resolvedStyle.height;
                if (float.IsNaN(badgeHeight) || badgeHeight <= 0f)
                    badgeHeight = signal.Badges.Children()
                        .Select(badge => badge.style.height.value.value).DefaultIfEmpty(0f).Max();
                signal.Badges.style.left = centre.x - badgeWidth * 0.5f;
                signal.Badges.style.top = signal.Points.Min(p => p.y) - badgeHeight - PingOwnerPortraitGap;
                signal.Ring.MarkDirtyRepaint();
            }
        }
        private void BuildSignals(IEnumerable<TeammateMarker> markers, bool cursor, bool vote)
        {
            foreach (var group in markers.GroupBy(m => cursor ? m.Cursor.Value : m.Hex.Value))
            {
                var tile = tiles.FirstOrDefault(t => t != null && t.Coordinate.Equals(group.Key));
                if (tile == null) continue;
                var owners = group.GroupBy(m => m.Sender).Select(g => g.First())
                    .OrderBy(m => m.Sender.Value, StringComparer.Ordinal).ToArray();
                var signal = new Signal { Tile = tile, Cursor = cursor, Vote = vote,
                    Colours = owners.Select(m => PlayerAccent.For(runtime.ReadModel.Participants, m.Sender)).ToArray(),
                    Ring = new VisualElement { name = cursor ? "teammate-cursor-outline" : "teammate-hex-outline", pickingMode = PickingMode.Ignore },
                    Badges = new VisualElement { name = "teammate-hex-owners", pickingMode = PickingMode.Ignore } };
                signal.Ring.style.position = Position.Absolute;
                signal.Ring.style.left = 0; signal.Ring.style.top = 0;
                signal.Ring.style.width = Length.Percent(100); signal.Ring.style.height = Length.Percent(100);
                signal.Ring.generateVisualContent += context => PaintSignal(context, signal);
                signal.Badges.style.position = Position.Absolute;
                signal.Badges.style.flexDirection = FlexDirection.Row;
                signal.Badges.style.alignItems = Align.Center;
                signal.Badges.style.justifyContent = Justify.Center;
                // Timer votes retain their existing size. Milestone 9.89 restores the earlier ping-owner
                // portraits at a smaller scale; stable owner ordering above keeps several senders in one row.
                if (vote || !cursor) foreach (var marker in owners)
                {
                    var hero = runtime.HeroOverviewsView?.Model?.Heroes.FirstOrDefault(h => h.HeroId.Equals(marker.Sender));
                    if (hero == null) continue;
                    var badge = HeroOverviewsUnityView.BuildSharedHeroPortrait(
                        hero,
                        MatchHudTheme.TeamFace(hero.TeamSide),
                        vote ? 0.62f : PingOwnerPortraitScale);
                    badge.userData = hero;
                    badge.pickingMode = PickingMode.Ignore;
                    foreach (var child in badge.Children()) child.pickingMode = PickingMode.Ignore;
                    signal.Badges.Add(badge);
                }
                layer.Add(signal.Ring); layer.Add(signal.Badges); signals.Add(signal);
            }
        }

        private void PaintSignal(MeshGenerationContext context, Signal signal)
        {
            var settings = runtime.DisplaySignalVisuals;
            float phase = Mathf.Repeat(Time.unscaledTime * 1000f / settings.PeriodMilliseconds, 1f);
            float baseline = signal.Cursor ? 0.88f : 1.05f;
            float wave = signal.Cursor ? baseline - 0.42f * phase : baseline + 0.30f * phase;
            var painter = context.painter2D;
            for (int edge = 0; edge < 6; edge++)
            {
                var colour = signal.Vote ? Color.white : signal.Colours[edge * signal.Colours.Length / 6];
                void Stroke(float scale, float opacity, float width)
                {
                    colour.a = opacity;
                    painter.strokeColor = colour; painter.lineWidth = width;
                    painter.BeginPath();
                    painter.MoveTo(signal.Centre + (signal.Points[edge] - signal.Centre) * scale);
                    painter.LineTo(signal.Centre + (signal.Points[(edge + 1) % 6] - signal.Centre) * scale);
                    painter.Stroke();
                }
                var steadyWidth = !signal.Cursor && !signal.Vote ? PingSteadyLineWidth : 2f;
                Stroke(signal.Vote ? 1.07f : baseline, signal.Cursor ? 0.55f : 0.9f, steadyWidth);
                if (!signal.Vote)
                    Stroke(wave, (1f - phase) * settings.IntensityPercent / 100f,
                        signal.Cursor ? 2.5f : PingWaveLineWidth);
            }
        }

        private Vector2 PanelPoint(Vector3 world)
        {
            var screen = boardCamera.WorldToScreenPoint(world);
            return RuntimePanelUtils.ScreenToPanel(layer.panel, new Vector2(screen.x, Screen.height - screen.y));
        }
    }
}

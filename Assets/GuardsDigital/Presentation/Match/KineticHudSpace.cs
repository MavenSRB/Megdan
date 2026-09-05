using GuardsDigital.Core.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11. THE UI-SPACE TO BOARD-SPACE BRIDGE - the one genuinely technical piece
    /// the TWENTIETH AMENDMENT named, and the reason a card can fly between a hand and a hex at all.
    ///
    /// <para><b>The problem.</b> The HUD lays out in panel points - a 2560x1440 reference space that
    /// <see cref="MatchHudPanelScaling"/> maps to whatever screen the player has, with Y growing
    /// DOWNWARD from the top-left. The board lives in world units, seen through a camera, with Y
    /// growing UP. A flight from a hand card to a hero's miniature crosses between them, and without a
    /// conversion the only honest options are to keep every flight inside one space or to guess.</para>
    ///
    /// <para><b>The conversion, both ways, and where it is thin on purpose.</b> Board to panel is
    /// <c>Camera.WorldToScreenPoint</c> followed by
    /// <see cref="MatchHudPanelScaling.ScreenToPanelPoint"/> - the second half being the exact mapping
    /// every board click in the game already passes through, so the flight lands where the player would
    /// click. Panel to board is that composition inverted through a camera ray onto the board's own
    /// plane. Neither does any geometry of its own: the arcs, leans and settles are
    /// <see cref="KineticFlightPath"/>'s, which is Unity-free and fully tested, and this file exists
    /// only to move a point between two coordinate systems.</para>
    /// </summary>
    public static class KineticSpaceBridge
    {
        /// <summary>The board's plane. Pieces are placed on it and lifted by their own vertical offset.</summary>
        public const float BoardPlaneY = 0f;

        /// <summary>
        /// Where a point on the board appears on the HUD's panel, or false when it does not appear at
        /// all - behind the camera, which <c>WorldToScreenPoint</c> reports by a negative Z and would
        /// otherwise mirror to a plausible-looking wrong place.
        /// </summary>
        public static bool TryBoardToPanel(Camera camera, Vector3 world, out Vector2 panelPoint)
        {
            panelPoint = Vector2.zero;
            if (camera == null)
            {
                return false;
            }

            var screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return false;
            }

            panelPoint = MatchHudPanelScaling.ScreenToPanelPoint(
                new Vector2(screen.x, screen.y), Screen.width, Screen.height);
            return true;
        }

        /// <summary>
        /// Where a point on the panel meets the board's plane, or false when the view is so oblique
        /// that the ray never does.
        /// </summary>
        public static bool TryPanelToBoard(Camera camera, Vector2 panelPoint, out Vector3 world)
        {
            world = Vector3.zero;
            if (camera == null)
            {
                return false;
            }

            var screen = MatchHudPanelScaling.PanelPointToScreen(
                panelPoint, Screen.width, Screen.height);
            var ray = camera.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            var distance = (BoardPlaneY - ray.origin.y) / ray.direction.y;
            if (distance <= 0f)
            {
                return false;
            }

            world = ray.origin + (ray.direction * distance);
            return true;
        }

        /// <summary>The centre of a HUD element, in the panel points a flight is computed in.</summary>
        public static Vector2 CentreOf(VisualElement element)
        {
            if (element == null)
            {
                return Vector2.zero;
            }

            var bounds = element.worldBound;
            return new Vector2(bounds.x + (bounds.width / 2f), bounds.y + (bounds.height / 2f));
        }

        /// <summary>Whether an element is actually laid out, and so has a position worth flying to.</summary>
        public static bool IsPlaced(VisualElement element)
        {
            if (element == null || element.panel == null)
            {
                return false;
            }

            var bounds = element.worldBound;
            return bounds.width > 0f && bounds.height > 0f &&
                !float.IsNaN(bounds.x) && !float.IsNaN(bounds.y);
        }
    }

    /// <summary>
    /// Milestone 8.35z11. Where each journey starts and ends on the real HUD.
    ///
    /// <para><b>Every anchor is found by querying the live tree, and not one HUD view is asked to
    /// publish anything</b> - which is the whole reason this pass adds a single line to
    /// <c>UiToolkitMatchHudViews</c> instead of dozens. The names are the ones the HUD already gives
    /// its own elements (<c>gd-current-card-slot</c>, <c>gd-card-tray</c>, <c>gd-hero-coins</c>,
    /// <c>gd-hero-level</c>), and the one addition is a <c>userData</c> stamp on each hero rail,
    /// because six rails share the name <c>gd-hero-rail</c> and a flight has to reach a PARTICULAR
    /// hero's.</para>
    ///
    /// <para><b>Not finding an anchor is a supported outcome, and that is deliberate.</b> Milestone
    /// 8.35z9 spent four milestones sweeping a shine that was never there, because a query for the
    /// wrong element name returns an empty list and an empty list is a legitimate state. So every
    /// lookup here returns null rather than throwing, every caller skips the flight when it does, and
    /// <see cref="HeroRail"/> falls back gracefully when the rail carries no stamp at all - which is
    /// exactly what a build merged with a HUD that predates that line would look like.</para>
    /// </summary>
    public static class KineticFlightAnchors
    {
        public const string HeroRailName = "gd-hero-rail";
        public const string CurrentCardSlotName = "gd-current-card-slot";
        public const string HandTrayName = "gd-card-tray";
        public const string HeroCoinsName = "gd-hero-coins";
        public const string HeroLevelName = "gd-hero-level";
        public const string HeroMedallionName = "gd-hero-medallion";

        /// <summary>
        /// One hero's rail, identified by the <see cref="HeroId"/> the rail carries in its
        /// <c>userData</c>. Null when no rail claims that hero - a spectator's HUD, a rail not built
        /// yet, or a HUD without the stamp.
        /// </summary>
        public static VisualElement HeroRail(VisualElement root, HeroId heroId)
        {
            if (root == null || !heroId.IsDefined)
            {
                return null;
            }

            var rails = root.Query<VisualElement>(name: HeroRailName).ToList();
            for (var index = 0; index < rails.Count; index++)
            {
                if (rails[index]?.userData is HeroId stamped && stamped.Equals(heroId))
                {
                    return rails[index];
                }
            }

            return null;
        }

        /// <summary>The facedown/played card slot on one hero's rail.</summary>
        public static VisualElement CurrentCardSlot(VisualElement root, HeroId heroId) =>
            Within(HeroRail(root, heroId), CurrentCardSlotName);

        /// <summary>One hero's coin display on their rail.</summary>
        public static VisualElement Coins(VisualElement root, HeroId heroId) =>
            Within(HeroRail(root, heroId), HeroCoinsName);

        /// <summary>One hero's Level label on their rail.</summary>
        public static VisualElement Level(VisualElement root, HeroId heroId) =>
            Within(HeroRail(root, heroId), HeroLevelName);

        /// <summary>
        /// The viewer's own hand. There is exactly one on a HUD, because a seat sees one hand - which
        /// is why this takes no hero and why a commitment by anyone else flies from their rail instead.
        /// </summary>
        public static VisualElement HandTray(VisualElement root) =>
            root?.Q<VisualElement>(HandTrayName);

        /// <summary>
        /// Where a card belonging to <paramref name="heroId"/> comes from and goes back to: the hand
        /// when it is the viewer's own and the hand is on screen, and that hero's rail otherwise.
        /// </summary>
        public static VisualElement HandOrRail(VisualElement root, HeroId heroId, HeroId viewerHeroId)
        {
            if (heroId.IsDefined && heroId.Equals(viewerHeroId))
            {
                var tray = HandTray(root);
                if (KineticSpaceBridge.IsPlaced(tray))
                {
                    return tray;
                }
            }

            var rail = HeroRail(root, heroId);
            return Within(rail, HeroMedallionName) ?? rail;
        }

        private static VisualElement Within(VisualElement host, string name)
        {
            if (host == null)
            {
                return null;
            }

            var found = host.Q<VisualElement>(name);
            return KineticSpaceBridge.IsPlaced(found) ? found : null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.47. The banner itself: two labels on a shaded plate, centred on the whole HUD
    /// panel, fading in, holding, fading out, and never taking the pointer.
    ///
    /// <para>Every number is a reference unit under <see cref="MatchHudPanelScaling"/>, because the
    /// host is a child of the HUD's own root: the letters scale with the rails and the hand, never
    /// with the screen. The line is set at a size no other HUD text uses, on the user's own words:
    /// "make the letters big, impossible to miss, centre screen"; blood red, black outline, a shaded
    /// box behind.</para>
    ///
    /// <para>Built once and kept DETACHED until a banner actually stands, for the reason the fizzle
    /// notice is (8.35z9): a hidden element is still an element to every sweep that enumerates the
    /// HUD. It attaches on demand to the root it is handed lazily - the renderer's root does not
    /// exist when the scene runtime composes its views, which is the same reason the kinetic layer
    /// reads <c>hud?.RootElement</c> when it draws rather than when it is built.</para>
    /// </summary>
    public sealed class PhaseBannerUnityView
    {
        public const string HostName = "gd-phase-banner";
        public const string PlateName = "gd-phase-banner-plate";
        public const string EyebrowName = "gd-phase-banner-eyebrow";
        public const string LineName = "gd-phase-banner-line";

        /// <summary>The big line, in reference units: about a tenth of the reference height.</summary>
        public const float LineFontSize = 150f;
        public const float EyebrowFontSize = 44f;
        public const float OutlineWidth = 6f;
        public const float PlateHorizontalPadding = 72f;
        public const float PlateVerticalPadding = 28f;
        public const float LabelGap = 12f;

        public const long FadeInMs = 250;
        public const long HoldMs = 1300;
        public const long FadeOutMs = 350;
        public const long TotalMs = FadeInMs + HoldMs + FadeOutMs;

        /// <summary>Blood red, as asked; the outline keeps it legible over the brightest board art.</summary>
        public static readonly Color BloodRed = new Color(0.62f, 0.05f, 0.08f, 1f);
        public static readonly Color Outline = Color.black;
        public static readonly Color Plate = new Color(0f, 0f, 0f, 0.62f);

        private readonly Func<VisualElement> rootSource;
        private readonly VisualElement host;
        private readonly VisualElement plate;
        private readonly Label eyebrow;
        private readonly Label line;
        private long token;

        public PhaseBannerUnityView(Func<VisualElement> rootSource)
        {
            this.rootSource = rootSource ?? throw new ArgumentNullException(nameof(rootSource));

            host = new VisualElement { name = HostName, pickingMode = PickingMode.Ignore };
            host.style.position = Position.Absolute;
            host.style.left = 0f;
            host.style.top = 0f;
            host.style.right = 0f;
            host.style.bottom = 0f;
            host.style.alignItems = Align.Center;
            host.style.justifyContent = Justify.Center;
            host.style.display = DisplayStyle.None;
            host.style.opacity = 0f;
            host.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            host.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseInOut) };

            plate = new VisualElement { name = PlateName, pickingMode = PickingMode.Ignore };
            plate.style.flexDirection = FlexDirection.Column;
            plate.style.alignItems = Align.Center;
            plate.style.justifyContent = Justify.Center;
            plate.style.backgroundColor = Plate;
            plate.style.paddingLeft = PlateHorizontalPadding;
            plate.style.paddingRight = PlateHorizontalPadding;
            plate.style.paddingTop = PlateVerticalPadding;
            plate.style.paddingBottom = PlateVerticalPadding;
            plate.style.borderTopLeftRadius = MatchHudTheme.ButtonCornerRadius;
            plate.style.borderTopRightRadius = MatchHudTheme.ButtonCornerRadius;
            plate.style.borderBottomLeftRadius = MatchHudTheme.ButtonCornerRadius;
            plate.style.borderBottomRightRadius = MatchHudTheme.ButtonCornerRadius;
            host.Add(plate);

            eyebrow = HudElements.Text(string.Empty, EyebrowFontSize, MatchHudTheme.PrimaryText, FontStyle.Bold);
            eyebrow.name = EyebrowName;
            eyebrow.pickingMode = PickingMode.Ignore;
            eyebrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            eyebrow.style.letterSpacing = 6f;
            eyebrow.style.marginBottom = LabelGap;
            eyebrow.style.unityTextOutlineWidth = OutlineWidth * 0.4f;
            eyebrow.style.unityTextOutlineColor = Outline;
            plate.Add(eyebrow);

            line = HudElements.Text(string.Empty, LineFontSize, BloodRed, FontStyle.Bold);
            line.name = LineName;
            line.pickingMode = PickingMode.Ignore;
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            line.style.letterSpacing = 4f;
            line.style.unityTextOutlineWidth = OutlineWidth;
            line.style.unityTextOutlineColor = Outline;
            plate.Add(line);
        }

        public VisualElement Root => host;

        /// <summary>The banner currently shown, or <see cref="PhaseBannerViewModel.None"/>.</summary>
        public PhaseBannerViewModel Current { get; private set; } = PhaseBannerViewModel.None;

        public bool IsShowing => host.parent != null && host.style.display == DisplayStyle.Flex;

        public string EyebrowText => eyebrow.text;

        public string LineText => line.text;

        /// <summary>
        /// Shows one banner: attach, fade in, hold, fade out, detach. A newer banner arriving during
        /// the hold restarts the whole sequence with its own words; the older one's scheduled hide is
        /// disarmed by the token so it cannot cut the newer short.
        /// </summary>
        public void Show(PhaseBannerViewModel model)
        {
            if (!model.HasBanner)
            {
                return;
            }

            var root = rootSource();
            if (root == null)
            {
                return;
            }

            var mine = ++token;
            Current = model;
            eyebrow.text = model.Eyebrow;
            eyebrow.style.display = string.IsNullOrEmpty(model.Eyebrow) ? DisplayStyle.None : DisplayStyle.Flex;
            line.text = model.Line;

            if (!ReferenceEquals(host.parent, root))
            {
                host.RemoveFromHierarchy();
                root.Add(host);
            }

            host.BringToFront();
            host.style.display = DisplayStyle.Flex;
            host.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };
            host.style.opacity = 0f;
            host.schedule.Execute(() =>
            {
                if (mine != token) return;
                host.style.transitionDuration = new List<TimeValue> { new TimeValue(FadeInMs, TimeUnit.Millisecond) };
                host.style.opacity = 1f;
            }).StartingIn(1);
            host.schedule.Execute(() =>
            {
                if (mine != token) return;
                host.style.transitionDuration = new List<TimeValue> { new TimeValue(FadeOutMs, TimeUnit.Millisecond) };
                host.style.opacity = 0f;
            }).StartingIn(FadeInMs + HoldMs);
            host.schedule.Execute(() =>
            {
                if (mine != token) return;
                Hide();
            }).StartingIn(TotalMs);
        }

        /// <summary>Takes the banner down at once, detaching it so no sweep sees a hidden element.</summary>
        public void Hide()
        {
            token++;
            Current = PhaseBannerViewModel.None;
            host.style.display = DisplayStyle.None;
            host.style.opacity = 0f;
            host.RemoveFromHierarchy();
        }
    }
}

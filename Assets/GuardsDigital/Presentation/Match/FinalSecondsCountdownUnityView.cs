using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The last ten displayed seconds of the standing allowance or team bank, centred over the HUD.
    /// It is paint only: the host and every descendant ignore picking, so board and HUD input pass
    /// through exactly as if the number were absent.
    /// </summary>
    public sealed class FinalSecondsCountdownUnityView
    {
        public const string HostName = "gd-final-seconds-countdown";
        public const string ValueName = "gd-final-seconds-countdown-value";
        public const int FinalSecond = 10;
        public const float FontSize = 260f;

        private readonly Label value;
        private TeamSide teamSide;

        public FinalSecondsCountdownUnityView(TeamSide teamSide = TeamSide.Red)
        {
            this.teamSide = teamSide == TeamSide.Blue ? TeamSide.Blue : TeamSide.Red;
            Root = new VisualElement { name = HostName, pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f;
            Root.style.top = 0f;
            Root.style.right = 0f;
            Root.style.bottom = 0f;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = Color.clear;
            Root.style.display = DisplayStyle.None;

            value = HudElements.Text(
                string.Empty,
                FontSize,
                PhaseBannerUnityView.ColorFor(this.teamSide),
                FontStyle.Bold);
            value.name = ValueName;
            value.pickingMode = PickingMode.Ignore;
            value.style.unityTextAlign = TextAnchor.MiddleCenter;
            value.style.unityTextOutlineWidth = PhaseBannerUnityView.OutlineWidth;
            value.style.unityTextOutlineColor = PhaseBannerUnityView.Outline;
            value.style.backgroundColor = Color.clear;
            value.style.whiteSpace = WhiteSpace.NoWrap;
            Root.Add(value);
        }

        public VisualElement Root { get; }

        public int DisplayedSecond { get; private set; }

        public TeamSide TeamSide => teamSide;

        public void SetTeamSide(TeamSide side)
        {
            teamSide = side == TeamSide.Blue ? TeamSide.Blue : TeamSide.Red;
            value.style.color = PhaseBannerUnityView.ColorFor(teamSide);
        }

        public void Render(MatchClockState clock)
        {
            DisplayedSecond = SelectDisplayedSecond(clock);
            value.text = DisplayedSecond == 0 ? string.Empty : DisplayedSecond.ToString();
            Root.style.display = DisplayedSecond == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Mirrors the header's one-window treatment for simultaneous allowances, then chooses the
        /// earliest final-ten deadline when a mixed allowance/bank state displays more than one clock.
        /// Zero is hidden; ceiling keeps each integer visible for its full displayed second.
        /// </summary>
        public static int SelectDisplayedSecond(MatchClockState clock)
        {
            if (clock == null || !clock.Enabled)
            {
                return 0;
            }

            var current = clock.Entries.Where(entry => entry.IsCurrent && entry.Running).ToArray();
            if (current.Length == 0)
            {
                return 0;
            }

            var displayedTicks = new List<long>();
            var allowances = current.Where(entry => !entry.UsesBank).ToArray();
            if (allowances.Length > 0)
            {
                displayedTicks.Add(allowances.Max(entry => entry.RemainingTicks));
            }

            foreach (var side in current.Where(entry => entry.UsesBank)
                .Select(entry => entry.Team)
                .Distinct())
            {
                displayedTicks.Add(clock.Bank(side));
            }

            return displayedTicks
                .Select(ticks => (int)Math.Ceiling(TimeSpan.FromTicks(ticks).TotalSeconds))
                .Where(seconds => seconds >= 1 && seconds <= FinalSecond)
                .DefaultIfEmpty(0)
                .Min();
        }
    }
}

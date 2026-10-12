using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class FinalSecondsCountdownTests
    {
        private static readonly HeroId Red = new HeroId("countdown-red");
        private static readonly HeroId Blue = new HeroId("countdown-blue");

        [Test]
        public void View_ReusesTheLargeTeamColouredLineWithoutABackground_AndTakesNoPointer()
        {
            var view = new FinalSecondsCountdownUnityView();
            view.Render(Clock(TimeSpan.FromSeconds(10).Ticks));

            Assert.That(view.Root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(view.Root.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(view.Root.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(view.Root.Query<VisualElement>().ToList()
                .All(element => element.pickingMode == PickingMode.Ignore), Is.True);
            Assert.That(view.Root.Q<VisualElement>(PhaseBannerUnityView.PlateName), Is.Null,
                "The countdown has no phase-banner plate or other background.");

            var value = view.Root.Q<Label>(FinalSecondsCountdownUnityView.ValueName);
            Assert.That(value.text, Is.EqualTo("10"));
            Assert.That(value.style.fontSize.value.value, Is.EqualTo(FinalSecondsCountdownUnityView.FontSize));
            Assert.That(FinalSecondsCountdownUnityView.FontSize,
                Is.GreaterThan(PhaseBannerUnityView.LineFontSize),
                "The countdown keeps the phase-banner treatment but uses the user's larger digits.");
            Assert.That(value.style.color.value, Is.EqualTo(PhaseBannerUnityView.BloodRed));
            Assert.That(value.style.unityTextOutlineWidth.value,
                Is.EqualTo(PhaseBannerUnityView.OutlineWidth));
            Assert.That(value.style.unityTextOutlineColor.value, Is.EqualTo(PhaseBannerUnityView.Outline));
            Assert.That(value.style.backgroundColor.value, Is.EqualTo(Color.clear));

            view.SetTeamSide(TeamSide.Blue);
            Assert.That(view.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(value.style.color.value, Is.EqualTo(PhaseBannerUnityView.DarkBlue));

            view.SetTeamSide(TeamSide.None);
            Assert.That(view.TeamSide, Is.EqualTo(TeamSide.Red),
                "An unscoped Director keeps the established Red fallback.");
            Assert.That(value.style.color.value, Is.EqualTo(PhaseBannerUnityView.BloodRed));
        }

        [Test]
        public void Selection_UsesDisplayedIntegerBoundaries_NormalBankAndMixedClocks()
        {
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(null), Is.Zero);
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(MatchClockState.Disabled), Is.Zero);
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(TimeSpan.FromSeconds(10).Ticks + 1)), Is.Zero, "Ceiling is still 11.");
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(TimeSpan.FromSeconds(9).Ticks + 1)), Is.EqualTo(10));
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(1)), Is.EqualTo(1), "Any positive fraction is displayed as 1.");
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(TimeSpan.FromSeconds(5).Ticks, running: false)), Is.Zero);
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(TimeSpan.FromSeconds(5).Ticks, current: false)), Is.Zero);

            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(0, redBank: TimeSpan.FromSeconds(10).Ticks)), Is.EqualTo(10));
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(0, redBank: TimeSpan.FromSeconds(10).Ticks + 1)), Is.Zero);
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(
                Clock(0, redBank: 0)), Is.Zero, "Expired clocks never paint a 0.");

            var simultaneous = new MatchClockState(
                true, 1, 1, TimeSpan.FromSeconds(20).Ticks, TimeSpan.FromSeconds(20).Ticks,
                new[]
                {
                    Entry("a", Red, TeamSide.Red, TimeSpan.FromSeconds(7).Ticks),
                    Entry("b", Blue, TeamSide.Blue, TimeSpan.FromSeconds(9).Ticks)
                });
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(simultaneous), Is.EqualTo(9),
                "Simultaneous allowances follow the header's latest-expiry shared window.");

            var mixed = new MatchClockState(
                true, 1, 1, TimeSpan.FromSeconds(5).Ticks, TimeSpan.FromSeconds(20).Ticks,
                new[]
                {
                    Entry("bank", Red, TeamSide.Red, 0),
                    Entry("allowance", Blue, TeamSide.Blue, TimeSpan.FromSeconds(7).Ticks)
                });
            Assert.That(FinalSecondsCountdownUnityView.SelectDisplayedSecond(mixed), Is.EqualTo(5),
                "When several header clocks stand, the large number warns about the earliest deadline.");
        }

        private static MatchClockState Clock(
            long allowance,
            bool running = true,
            bool current = true,
            long? redBank = null)
        {
            return new MatchClockState(
                true, 1, 1,
                redBank ?? TimeSpan.FromSeconds(20).Ticks,
                TimeSpan.FromSeconds(20).Ticks,
                new[] { Entry("clock", Red, TeamSide.Red, allowance, running, current) });
        }

        private static MatchClockEntry Entry(
            string id,
            HeroId owner,
            TeamSide side,
            long ticks,
            bool running = true,
            bool current = true) =>
            new MatchClockEntry(id, MatchClockKind.Commitment, owner, side, ticks, running, current);
    }
}

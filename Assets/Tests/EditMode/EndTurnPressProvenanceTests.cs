using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.51. The record of HOW an END TURN press reached the view, and the log line it
    /// becomes - built for the 2026-09-18 report "his card got resolved without input", which the
    /// host log could answer only as "his own client pressed END TURN" and never as "with what".
    /// </summary>
    /// <remarks>
    /// The signals are exercised through the recorder's own note methods here, because an Edit
    /// Mode view sits on no panel and UI Toolkit dispatches no event without one; the live wiring -
    /// a real pointer click and a real keyboard submit on the drawn control - is
    /// <c>EndTurnPressProvenancePlayModeTests</c>' case on the running HUD.
    /// </remarks>
    public sealed class EndTurnPressProvenanceTests
    {
        [Test]
        public void APointerRelease_ReadsAsAPointerPress()
        {
            var press = new EndTurnPressProvenance();
            press.NotePointerUp(0);

            var report = press.Take("gd-end-turn-control");

            Assert.That(report, Does.Contain("source=pointer"));
            Assert.That(report, Does.Contain("button=0"));
            Assert.That(report, Does.Contain("key=-"), "No key was involved, and the report says so.");
            Assert.That(report, Does.Contain("focused=gd-end-turn-control"));
        }

        [Test]
        public void AKeyboardSubmit_ReadsAsAKeyboardPress_NamingTheKey()
        {
            var press = new EndTurnPressProvenance();
            press.NoteKeyDown(KeyCode.Space);
            press.NoteNavigationSubmit();

            var report = press.Take(null);

            Assert.That(report, Does.Contain("source=keyboard"));
            Assert.That(report, Does.Contain("key=Space"),
                "The key is the fact a push-to-talk explanation stands or falls on.");
            Assert.That(report, Does.Contain("button=-"), "No pointer was involved.");
            Assert.That(report, Does.Contain("focused=-"));
        }

        [Test]
        public void NoSignalAtAll_ReadsAsADriver()
        {
            var report = new EndTurnPressProvenance().Take(null);

            Assert.That(report, Does.StartWith("source=driver"),
                "A press with no input behind it came from code - a driver, a fixture, a bot.");
        }

        [Test]
        public void BothSignals_AreBothReported_RatherThanOneGuessedAway()
        {
            var press = new EndTurnPressProvenance();
            press.NotePointerUp(1);
            press.NoteNavigationSubmit();

            Assert.That(press.Take(null), Does.Contain("source=pointer+keyboard"));
        }

        [Test]
        public void APress_ConsumesItsSignals()
        {
            var press = new EndTurnPressProvenance();
            press.NotePointerUp(0);
            press.NoteKeyDown(KeyCode.Return);

            Assert.That(press.Take(null), Does.Contain("source=pointer").And.Contain("key=Return"));
            Assert.That(press.Take(null), Does.StartWith("source=driver").And.Contain("key=-"),
                "The second press reads nothing the first already spent.");
        }

        [Test]
        public void TheMarker_NamesThePressTheClockTheVersionTheActorAndTheOutcome()
        {
            var at = new DateTimeOffset(2026, 9, 18, 20, 28, 25, 682, TimeSpan.Zero);

            var line = EndTurnPressProvenance.Marker(
                "source=keyboard button=- key=Space focused=gd-end-turn-control",
                at, 729, "hero-silverarrow", true);

            Assert.That(line, Does.StartWith(EndTurnPressProvenance.MarkerPrefix + " source=keyboard"));
            Assert.That(line, Does.Contain("key=Space"));
            Assert.That(line, Does.Contain("at=20:28:25.682"),
                "UTC to the millisecond, the same stamp the host narration carries.");
            Assert.That(line, Does.Contain("version=729"));
            Assert.That(line, Does.Contain("actor=hero-silverarrow"));
            Assert.That(line, Does.Contain("progressed=true"));
        }

        [Test]
        public void TheMarker_WithoutAReportOrAnActor_StillStatesBoth()
        {
            var line = EndTurnPressProvenance.Marker(null, DateTimeOffset.UtcNow, 1, null, false);

            Assert.That(line, Does.Contain("source=driver"));
            Assert.That(line, Does.Contain("actor=-"));
            Assert.That(line, Does.Contain("progressed=false"));
        }

        /// <summary>
        /// The view names the press at the moment it is raised, before anything composes from it,
        /// and a driver's direct call reads as a driver.
        /// </summary>
        [Test]
        public void TheView_ReportsTheDriver_WhenNoInputReachedItsControl()
        {
            var view = new RoundActionUnityView();
            var invocation = new TypedInvocationViewModel(
                "END TURN", DirectorCommand.PerformHold(new CardInstanceId("card-1")));
            view.Render(new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), invocation, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>()));
            Assert.That(view.LastEndTurnPressReport, Is.Null, "Nothing has been pressed yet.");

            string seenAtTheEvent = null;
            view.EndTurnRequested += () => seenAtTheEvent = view.LastEndTurnPressReport;
            view.RequestEndTurn();

            Assert.That(seenAtTheEvent, Is.Not.Null, "The report exists by the time the event fires.");
            Assert.That(seenAtTheEvent, Does.StartWith("source=driver"));
            Assert.That(seenAtTheEvent, Does.Contain("focused=-"),
                "Off any panel there is no focus controller, and the report says so rather than inventing one.");
        }

        [Test]
        public void TheLiveControl_IsTheOneObserved_AndItStillDrawsAsBefore()
        {
            var view = new RoundActionUnityView();
            var invocation = new TypedInvocationViewModel(
                "END TURN", DirectorCommand.PerformHold(new CardInstanceId("card-1")));
            view.Render(new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), invocation, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>()));

            var controls = Descendants(view.Root).Where(element => element.name == "gd-end-turn-control").ToArray();
            Assert.That(controls, Has.Length.EqualTo(1), "One drawn END TURN, as before.");
            Assert.That(controls[0], Is.InstanceOf<Button>());
            Assert.That(controls[0].enabledInHierarchy, Is.True);
        }

        private static System.Collections.Generic.IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }
    }
}

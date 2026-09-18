using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.51. On the running HUD, with a real panel dispatching real events, the drawn
    /// END TURN tells a pointer click from a keyboard submit and names the key - the two ways an
    /// alt-tabbing player could have ended Silverarrow's turn on 2026-09-18 without meaning to.
    /// </summary>
    public sealed class EndTurnPressProvenancePlayModeTests
    {
        [UnityTest]
        public IEnumerator TheLivePanel_TellsAPointerPressFromAKeyboardSubmit_AndTheSubmitNoLongerPresses()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            var root = UnityEngine.Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            var runtime = root.Runtime;
            Assert.That(runtime, Is.Not.Null);
            runtime.RefreshAll();
            yield return null;

            // Draw an END TURN regardless of where the freshly composed match stands: the press
            // itself is what is under test, and the presenter refusing a stale Hold afterwards is
            // exactly what it does with any press the projection no longer offers.
            var view = runtime.RoundView;
            var invocation = new TypedInvocationViewModel(
                "END TURN", DirectorCommand.PerformHold(new CardInstanceId("card-1")));
            view.Render(new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), invocation, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>()));
            yield return null;
            yield return null;

            var control = UiPlayer.Descendants(root.Hud.RootElement)
                .Single(element => element.name == "gd-end-turn-control");
            var presses = new List<string>();
            view.EndTurnRequested += () => presses.Add(view.LastEndTurnPressReport);

            UiPlayer.Click(control);
            yield return null;

            Assert.That(presses, Has.Count.EqualTo(1), "One click, one press.");
            Assert.That(presses[0], Does.Contain("source=pointer"),
                "A pointer click reads as a pointer press: " + presses[0]);
            Assert.That(presses[0], Does.Contain("button=0"));
            Assert.That(presses[0], Does.Contain("key=-"));

            control.Focus();
            yield return null;
            using (var key = KeyDownEvent.GetPooled('\0', KeyCode.Space, EventModifiers.None))
            {
                key.target = control;
                control.SendEvent(key);
            }

            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = control;
                control.SendEvent(submit);
            }

            yield return null;

            // Milestone 8.51 found that a navigation submit on the focused Button IS a press - which
            // is how an alt-tabbing player ended a turn. Milestone 8.52 closes it, at the user's word
            // ("have it make space and enter not end a turn"): the control cannot take focus, and a
            // submit dispatched at it anyway is stopped before the Button's own handler runs.
            Assert.That(control.focusable, Is.False,
                "END TURN is not a keyboard target, so Enter and Space have nothing to land on.");
            Assert.That(control.panel.focusController.focusedElement, Is.Not.SameAs(control),
                "Focus() on an unfocusable control leaves focus elsewhere.");
            Assert.That(presses, Has.Count.EqualTo(1),
                "A keyboard submit presses nothing: the one press is still the pointer's. " +
                string.Join(" | ", presses));
        }
    }
}

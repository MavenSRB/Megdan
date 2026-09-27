using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class SnorriRuneDragPlayModeTests
    {
        [UnityTest]
        public IEnumerator APhysicalRuneDrag_DropsOnANumberedSlotThroughTheDecisionWindow()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var host = new GameObject("snorri-rune-drag-test");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            var view = new DecisionWindowUnityView();
            view.Root.style.left = 0f;
            view.Root.style.top = 0f;
            view.Root.style.width = 1100f;
            view.Root.style.height = 720f;
            document.rootVisualElement.Add(view.Root);
            var activated = new List<DecisionWindowControlViewModel>();
            view.ControlActivated += activated.Add;

            try
            {
                view.Render(Model());
                yield return null;
                yield return null;

                var piece = view.Root.Q<Button>("gd-rune-piece-1");
                var slot = view.Root.Q<Button>("gd-rune-turn-slot-3");
                Assert.That(piece, Is.Not.Null);
                Assert.That(slot, Is.Not.Null);
                Assert.That(piece.worldBound.width, Is.GreaterThan(0f));
                Assert.That(slot.worldBound.width, Is.GreaterThan(0f));
                Assert.That(piece.Q("gd-rune-glyph-1"), Is.Not.Null,
                    "The piece draws its printed glyph from the prepared marker artwork.");

                var start = piece.worldBound.center;
                var end = slot.worldBound.center;
                Pointer(piece, EventType.MouseDown, start);
                Pointer(piece, EventType.MouseDrag, end);
                Assert.That(view.Root.Q("gd-rune-drag-proxy"), Is.Not.Null,
                    "The carried rune is visible while the pointer moves.");
                Pointer(piece, EventType.MouseUp, end);

                Assert.That(activated, Has.Count.EqualTo(1),
                    "A drag must submit one typed pending-choice command, not a source click as well.");
                Assert.That(activated.Single().RuneOrdinal, Is.EqualTo(1));
                Assert.That(activated.Single().TurnSlotIndex, Is.EqualTo(3));
                Assert.That(view.Root.Q("gd-rune-drag-proxy"), Is.Null);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(panelSettings);
            }
        }

        private static DecisionWindowViewModel Model()
        {
            var pairs = Enumerable.Range(1, 16).Select(value =>
                new DecisionWindowControlViewModel(
                    "RUNE " + ((value - 1) / 4 + 1) + " TO TURN " + ((value - 1) % 4 + 1),
                    DirectorCommand.ReleaseTurn(),
                    runeOrdinal: (value - 1) / 4 + 1,
                    turnSlotIndex: (value - 1) % 4 + 1)).ToArray();
            return new DecisionWindowViewModel(
                1L, DecisionWindowFamily.RuneSlots, DecisionWindowState.Expanded,
                "snorri-rune-drag", "INSCRIBE THE RUNES", "SKILL",
                "Drag each rune onto one turn slot.",
                count: "0 of 4 chosen",
                controls: pairs,
                isMinimisable: false);
        }

        private static void Pointer(VisualElement source, EventType type, Vector2 position)
        {
            var mouse = new Event
            {
                type = type,
                mousePosition = position,
                button = 0,
                clickCount = 1
            };
            if (type == EventType.MouseDown)
            {
                using (var evt = PointerDownEvent.GetPooled(mouse))
                {
                    evt.target = source;
                    source.SendEvent(evt);
                }
            }
            else if (type == EventType.MouseDrag)
            {
                using (var evt = PointerMoveEvent.GetPooled(mouse))
                {
                    evt.target = source;
                    source.SendEvent(evt);
                }
            }
            else
            {
                using (var evt = PointerUpEvent.GetPooled(mouse))
                {
                    evt.target = source;
                    source.SendEvent(evt);
                }
            }
        }
    }
}

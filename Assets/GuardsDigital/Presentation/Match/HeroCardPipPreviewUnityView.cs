using System;
using GuardsDigital.Core.Cards;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// One pointer-transparent complete face raised from a board-token card pip. The caller supplies
    /// only a definition already admitted by the viewer's inspection projection.
    /// </summary>
    public sealed class HeroCardPipPreviewUnityView
    {
        public const float PointerGap = 16f;
        public const float SafeMargin = 8f;

        private CardDefinitionReference definitionReference;

        public HeroCardPipPreviewUnityView()
        {
            Root = new VisualElement
            {
                name = "gd-hero-token-card-pip-preview",
                pickingMode = PickingMode.Ignore
            };
            Root.style.position = Position.Absolute;
            Root.style.display = DisplayStyle.None;
            Root.style.width = CardFootprint.Width;
            Root.style.height = CardFootprint.Height;
            Root.style.minWidth = CardFootprint.Width;
            Root.style.minHeight = CardFootprint.Height;
            Root.style.maxWidth = CardFootprint.Width;
            Root.style.maxHeight = CardFootprint.Height;
        }

        public VisualElement Root { get; }
        public bool IsVisible => Root.style.display.value == DisplayStyle.Flex;
        public CardDefinitionReference DefinitionReference => definitionReference;
        public Rect LastRect { get; private set; }

        public void Show(CardDefinition definition, Vector2 panelPointer, Rect safeRegion)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!definitionReference.Equals(definition.Reference))
            {
                Root.Clear();
                var model = new TrayCardViewModel(
                    new CardInstanceId("token-pip-preview:" + definition.Id.Value),
                    definition.Reference,
                    definition.DisplayName,
                    definition.Progression.Color,
                    CurrentRoundCards.ReadFace(definition),
                    TrayCardGroup.Available,
                    0,
                    false,
                    false);
                var face = new VisualElement
                {
                    name = "gd-hero-token-card-pip-face",
                    tooltip = definition.DisplayName,
                    pickingMode = PickingMode.Ignore
                };
                ConfiguredCardFaceView.Build(face, model);
                face.Q("gd-card-history")?.RemoveFromHierarchy();
                IgnorePicking(face);
                Root.Add(face);
                definitionReference = definition.Reference;
            }

            LastRect = CalculateRect(panelPointer, safeRegion);
            Root.style.left = LastRect.x;
            Root.style.top = LastRect.y;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        public void Hide()
        {
            if (!IsVisible && !definitionReference.IsDefined) return;
            definitionReference = default;
            Root.Clear();
            Root.style.display = DisplayStyle.None;
        }

        public static Rect CalculateRect(Vector2 pointer, Rect safeRegion)
        {
            var minimumX = safeRegion.xMin + SafeMargin;
            var maximumX = safeRegion.xMax - SafeMargin - CardFootprint.Width;
            var minimumY = safeRegion.yMin + SafeMargin;
            var maximumY = safeRegion.yMax - SafeMargin - CardFootprint.Height;
            if (maximumX < minimumX || maximumY < minimumY)
                return new Rect(minimumX, minimumY, CardFootprint.Width, CardFootprint.Height);

            var right = pointer.x + PointerGap;
            var left = pointer.x - PointerGap - CardFootprint.Width;
            var x = pointer.x < safeRegion.center.x ? right : left;
            x = Mathf.Clamp(x, minimumX, maximumX);
            var y = Mathf.Clamp(pointer.y - (CardFootprint.Height * 0.5f), minimumY, maximumY);
            return new Rect(x, y, CardFootprint.Width, CardFootprint.Height);
        }

        private static void IgnorePicking(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                IgnorePicking(child);
        }
    }
}

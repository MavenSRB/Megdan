using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Cards;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>Individually inspected artwork positions, in original source pixels.</summary>
    public sealed class CardValueAnchors
    {
        private static Dictionary<CardDefinitionReference, CardValueAnchors> entries;
        private readonly float[] values;
        private CardValueAnchors(float[] values) { this.values = values; }
        public float Width => values[8];
        public float Height => values[9];

        public static bool TryGet(CardDefinitionReference reference, out CardValueAnchors anchors)
        {
            if (entries == null)
            {
                var loaded = new Dictionary<CardDefinitionReference, CardValueAnchors>();
                var asset = Resources.Load<TextAsset>("UI/CardValueAnchors");
                if (asset == null) throw new InvalidOperationException("Missing inspected card-value anchors.");
                foreach (var line in asset.text.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var fields = line.Trim().Split(',');
                    if (fields.Length != 12) throw new FormatException("Invalid card-value anchor row.");
                    loaded.Add(new CardDefinitionReference(new CardDefinitionId(fields[0]), fields[1]),
                        new CardValueAnchors(fields.Skip(2).Select(value =>
                            float.Parse(value, CultureInfo.InvariantCulture)).ToArray()));
                }
                entries = loaded;
            }
            return entries.TryGetValue(reference, out anchors);
        }

        public bool TryPosition(TrayCardFaceViewModel face, PrintedValueChannel channel, out Vector2 at)
        {
            at = default;
            if (channel == PrintedValueChannel.Initiative && values[0] > 0)
                at = new Vector2(values[0], 39);
            else if (face.PrimaryValue?.Channel == channel && values[3] > 0)
                at = new Vector2(values[5], values[3]);
            else if (face.SpatialValue?.Channel == channel && values[4] > 0)
                at = new Vector2(values[7], values[4]);
            else if (face.SecondaryValues.Any(value => value.Channel == channel))
            {
                var y = channel == PrintedValueChannel.Defense ? values[1] :
                    channel == PrintedValueChannel.Movement ? values[2] : values[6];
                if (y > 0) at = new Vector2(55, y);
            }
            return at != Vector2.zero;
        }
    }

    public static class CardValueOverlay
    {
        public static string Signed(int bonus) =>
            bonus > 0 ? "+" + bonus.ToString(CultureInfo.InvariantCulture) :
            bonus.ToString(CultureInfo.InvariantCulture);

        public static void AddArtwork(VisualElement parent, CardDefinitionReference reference,
            TrayCardFaceViewModel face, IReadOnlyList<CardValuePreview> values, float width, float height)
        {
            if (values == null || values.All(value => value.Bonus == 0) ||
                !CardValueAnchors.TryGet(reference, out var anchors)) return;
            var sx = width / anchors.Width;
            var sy = height / anchors.Height;
            foreach (var value in values.Where(value => value.Bonus != 0))
            {
                if (!anchors.TryPosition(face, value.Channel, out var at)) continue;
                var label = BonusLabel(value, 32f * sx);
                label.style.position = Position.Absolute;
                // Never shift left or wrap: fit the available right-hand space instead.
                var available = Mathf.Min(74f, anchors.Width - at.x - 5f) * sx;
                label.style.left = (at.x + 2f) * sx;
                label.style.top = (at.y - 21f) * sy;
                label.style.width = available;
                label.style.height = 42f * sy;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.overflow = Overflow.Hidden;
                parent.Add(label);
                void Fit()
                {
                    var size = 32f * sx;
                    label.style.fontSize = size;
                    var measured = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined,
                        0, VisualElement.MeasureMode.Undefined).x;
                    if (measured > available - 2f * sx)
                        size *= (available - 2f * sx) / measured;
                    label.style.fontSize = size;
                    label.style.unityTextOutlineWidth = Mathf.Max(0.35f, size / 36f);
                }
                label.RegisterCallback<AttachToPanelEvent>(_ => label.schedule.Execute(Fit));
            }
        }

        public static Label BonusLabel(CardValuePreview value, float size)
        {
            var label = HudElements.Text(Signed(value.Bonus), size, Color.white, FontStyle.Bold);
            label.name = "gd-card-bonus-" + value.Channel.ToString().ToLowerInvariant();
            label.pickingMode = PickingMode.Ignore;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.marginLeft = label.style.marginRight = 0f;
            label.style.marginTop = label.style.marginBottom = 0f;
            label.style.paddingLeft = label.style.paddingRight = 0f;
            label.style.paddingTop = label.style.paddingBottom = 0f;
            label.style.unityTextOutlineColor = Color.black;
            label.style.unityTextOutlineWidth = Mathf.Max(0.35f, size / 36f);
            label.tooltip = value.Channel + ": " + Signed(value.Bonus) +
                " from current game state. Unchosen targets and not-yet-applied action text may change this.";
            return label;
        }

        public static void AddInline(VisualElement parent, IReadOnlyList<CardValuePreview> values,
            PrintedValueChannel channel, float size = 12f)
        {
            var value = values?.FirstOrDefault(v => v.Channel == channel);
            if (value == null || value.Bonus == 0) return;
            var label = BonusLabel(value, size);
            label.style.marginLeft = 2f;
            label.style.flexShrink = 0;
            parent.Add(label);
        }
    }
}

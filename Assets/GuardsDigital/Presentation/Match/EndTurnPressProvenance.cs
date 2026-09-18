using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.51. Records HOW the drawn END TURN control was activated - by the pointer, by a
    /// keyboard submit on the focused control, or by a driver calling the view directly - so the
    /// press can be written to the player's own log beside the command it composed.
    /// </summary>
    /// <remarks>
    /// <para><b>Why.</b> At the 2026-09-18 playtest a player's untouched card was Held and his turn
    /// released 1.1 s after it began, and he reported pressing nothing; he had been alt-tabbing.
    /// The host log could show that his own client sent the press composition - a Hold, then the
    /// release one round trip later - and nothing more. Whether a pointer landed on the control or
    /// Enter/Space reached a focused one was recorded nowhere: UI Toolkit's <see cref="Button"/>
    /// answers a <see cref="NavigationSubmitEvent"/> exactly like a click, and neither the host nor
    /// the client wrote which it was. This object is that record.</para>
    /// <para><b>How it reads events.</b> Every callback is registered
    /// <see cref="TrickleDown.TrickleDown"/> on the control itself, so it runs before the Button's
    /// own handlers turn the event into a press, and every stamp is consumed by <see cref="Take"/>:
    /// a press reads what reached the control since the last press or rebuild, never a stamp from
    /// an earlier control. A stamp with no press behind it - a pointer released over the control
    /// that did not click it - is reported as it stands rather than guessed away: the report lists
    /// every signal seen, and a reader shown both signals is told both.</para>
    /// </remarks>
    public sealed class EndTurnPressProvenance
    {
        public const string DriverSource = "driver";
        public const string PointerSource = "pointer";
        public const string KeyboardSource = "keyboard";
        public const string MixedSource = "pointer+keyboard";

        /// <summary>The one line the player's log carries per press; grep for it.</summary>
        public const string MarkerPrefix = "GUARDS_INPUT: END_TURN_PRESS";

        private bool pointerSeen;
        private int pointerButton = -1;
        private bool navigationSubmitSeen;
        private KeyCode lastKey = KeyCode.None;

        /// <summary>
        /// Wires the three signals onto one drawn control, ahead of the control's own handlers.
        /// </summary>
        public void Observe(VisualElement control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            control.RegisterCallback<PointerUpEvent>(
                evt => NotePointerUp(evt.button), TrickleDown.TrickleDown);
            control.RegisterCallback<NavigationSubmitEvent>(
                _ => NoteNavigationSubmit(), TrickleDown.TrickleDown);
            control.RegisterCallback<KeyDownEvent>(
                evt => NoteKeyDown(evt.keyCode), TrickleDown.TrickleDown);
        }

        /// <summary>A pointer was released over the control; the Button's click follows it.</summary>
        public void NotePointerUp(int button)
        {
            pointerSeen = true;
            pointerButton = button;
        }

        /// <summary>
        /// A navigation submit reached the control - Enter or Space on a focused Button, which
        /// <see cref="Button"/> turns into a click with no pointer anywhere near it.
        /// </summary>
        public void NoteNavigationSubmit() => navigationSubmitSeen = true;

        /// <summary>A key went down on the control; the last one is named beside a keyboard press.</summary>
        public void NoteKeyDown(KeyCode key) => lastKey = key;

        /// <summary>Forgets every signal - a rebuilt control starts with none behind it.</summary>
        public void Reset()
        {
            pointerSeen = false;
            pointerButton = -1;
            navigationSubmitSeen = false;
            lastKey = KeyCode.None;
        }

        /// <summary>
        /// The report for one press, after which the signals behind it are cleared for the next.
        /// </summary>
        public string Take(string focusedElementName)
        {
            var source = pointerSeen && navigationSubmitSeen
                ? MixedSource
                : pointerSeen
                    ? PointerSource
                    : navigationSubmitSeen
                        ? KeyboardSource
                        : DriverSource;
            var report =
                "source=" + source +
                " button=" + (pointerSeen ? pointerButton.ToString(CultureInfo.InvariantCulture) : "-") +
                " key=" + (lastKey == KeyCode.None ? "-" : lastKey.ToString()) +
                " focused=" + (string.IsNullOrEmpty(focusedElementName) ? "-" : focusedElementName);
            Reset();
            return report;
        }

        /// <summary>
        /// The marker line: the press report, the UTC wall clock (the host narration is stamped the
        /// same way, so the two files line up), the projection version the press was made against,
        /// the hero the press was for, and whether the presenter composed anything from it.
        /// </summary>
        public static string Marker(
            string report, DateTimeOffset at, long version, string actor, bool progressed) =>
            MarkerPrefix + " " +
            (string.IsNullOrEmpty(report) ? "source=" + DriverSource : report) +
            " at=" + at.ToUniversalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
            " version=" + version.ToString(CultureInfo.InvariantCulture) +
            " actor=" + (string.IsNullOrEmpty(actor) ? "-" : actor) +
            " progressed=" + (progressed ? "true" : "false");
    }
}

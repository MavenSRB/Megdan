using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.75b, narrowed at milestone 8.75e. The one place a surface puts WORDS to
    /// <see cref="PrintedTextFace"/> - now only where a player is being ASKED which face to apply:
    /// the two controls the decision window draws while the coin is not what decides, and its eyebrow.
    ///
    /// <para><b>What 8.75e took out.</b> This file also held the words for a corner badge on every
    /// card whose printed text the Tie Breaker coin selects, and for the line that badge added to a
    /// card's hover reading. The user dropped the badge from the first live playtest of the
    /// twenty-two-hero build - "why do we have top right SUN symbol ? Oo", then "Drop the badge and
    /// rely on the coin strip alone" - so the reading lives where it always did, on the rail's
    /// tie-breaker banner, and no card surface repeats it. <see cref="Word"/> and
    /// <see cref="SideWord"/> stay because they are what a reading of the coin SAYS, and the journeys
    /// that prove which text is live before a card is pressed say it with them.</para>
    ///
    /// <para><b>Words rather than the printed symbols, and that is measured rather than preferred.</b>
    /// The cards themselves print ☀ (U+2600) and ☾ (U+263E), and the obvious control would carry those
    /// two glyphs. The HUD's own runtime font is not guaranteed to carry them: the only Miscellaneous
    /// Symbols character this build has ever drawn is ♥ (U+2665) on the life counter, and a glyph the
    /// font lacks renders as an empty box - a control that says NOTHING, on the one decision
    /// Ignatia's whole hand depends on. `SUN` and `MOON` cannot fail that way, and they are what an
    /// assistive reading has to say anyway. The printed card behind the window still carries the
    /// symbols themselves. If a font carrying both glyphs is ever bundled, this is the one file that
    /// changes.</para>
    ///
    /// <para>Nothing here branches on a hero, a card or a capability: every member is a total function
    /// of a typed <see cref="PrintedTextFace"/> or a <see cref="TeamSide"/>, both of which the content
    /// declares and the projection supplies.</para>
    /// </summary>
    public static class PrintedTextFaceLabels
    {
        /// <summary>The word for the sun (☀) face.</summary>
        public const string SunWord = "SUN";

        /// <summary>The word for the moon (☾) face.</summary>
        public const string MoonWord = "MOON";

        /// <summary>
        /// The decision window's control for the sun line. It names the CONSEQUENCE, which is the rule
        /// milestone 8.46 set for every control in that window.
        /// </summary>
        public const string ApplySunTextLabel = "APPLY THE " + SunWord + " TEXT";

        /// <summary>The same for the moon line.</summary>
        public const string ApplyMoonTextLabel = "APPLY THE " + MoonWord + " TEXT";

        /// <summary>The window's eyebrow while that choice stands.</summary>
        public const string SectionLabel = "CHOOSE THE FACE";

        /// <summary>
        /// The window's title where nothing names the card asking - unreachable for dealt content since
        /// milestone 8.58 published the resolving card, and kept for the same reason every other family
        /// keeps one.
        /// </summary>
        public const string FallbackTitle = "SUN OR MOON?";

        /// <summary>The one word for a face, and an empty string for the unrevealed coin.</summary>
        public static string Word(PrintedTextFace face)
        {
            switch (face)
            {
                case PrintedTextFace.Sun:
                    return SunWord;
                case PrintedTextFace.Moon:
                    return MoonWord;
                case PrintedTextFace.None:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face));
            }
        }

        /// <summary>
        /// The team whose side the coin is showing, named the way the status bar names one. Since
        /// milestone 8.75e the rail's tie-breaker banner is the ONLY place the live face is read, so
        /// this is the word for the thing a player goes and looks at to find out why one of a card's
        /// two printed lines is the live one.
        /// </summary>
        public static string SideWord(TeamSide side)
        {
            switch (side)
            {
                case TeamSide.Red:
                    return "RED";
                case TeamSide.Blue:
                    return "BLUE";
                default:
                    return string.Empty;
            }
        }
    }
}

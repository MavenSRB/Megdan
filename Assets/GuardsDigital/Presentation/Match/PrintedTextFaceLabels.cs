using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.75b. The one place a surface puts WORDS to
    /// <see cref="PrintedTextFace"/> - the corner marker on a card, the card's hover and assistive
    /// reading, and the two controls the decision window draws when a card's controller is choosing the
    /// face instead of reading the Tie Breaker coin.
    ///
    /// <para><b>Words rather than the printed symbols, and that is measured rather than preferred.</b>
    /// The cards themselves print ☀ (U+2600) and ☾ (U+263E), and the obvious marker is those two
    /// glyphs. The HUD's own runtime font is not guaranteed to carry them: the only Miscellaneous
    /// Symbols character this build has ever drawn is ♥ (U+2665) on the life counter, and a glyph the
    /// font lacks renders as an empty box - a marker that says NOTHING, on the one reading Ignatia's
    /// whole hand depends on. `SUN` and `MOON` cannot fail that way, they read at the marker's size,
    /// and they are what an assistive reading has to say anyway. The printed card behind the marker
    /// still carries the symbols themselves, so nothing is lost: the marker's job is to say which of
    /// the two lines already printed there is live. If a font carrying both glyphs is ever bundled,
    /// this is the one file that changes.</para>
    ///
    /// <para>Nothing here branches on a hero, a card or a capability: every member is a total function
    /// of a typed <see cref="PrintedTextFace"/> and the one boolean saying whether the controller is
    /// choosing, both of which the content declares and the projection supplies.</para>
    /// </summary>
    public static class PrintedTextFaceLabels
    {
        /// <summary>The word for the sun (☀) face.</summary>
        public const string SunWord = "SUN";

        /// <summary>The word for the moon (☾) face.</summary>
        public const string MoonWord = "MOON";

        /// <summary>
        /// The marker a card wears while its controller may apply either text - both faces, because
        /// neither is ruled out and the coin is not what decides.
        /// </summary>
        public const string EitherFaceWord = SunWord + " / " + MoonWord;

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
        /// What the corner marker on a two-text card reads: the live face, or both faces while the
        /// controller is free to pick either. An empty string is the one case a marker is not drawn at
        /// all - a coin that has revealed nothing, so no text is live and none may be guessed.
        /// </summary>
        public static string MarkerLabel(PrintedTextFace liveFace, bool controllerChoosesTheFace) =>
            controllerChoosesTheFace ? EitherFaceWord : Word(liveFace);

        /// <summary>
        /// The line the card's hover and assistive reading adds, naming the live text and why it is the
        /// live one. It is appended to the card's existing reading rather than replacing any of it, so
        /// a player hovering an Ignatia card hears the whole card and then which half of it applies.
        /// </summary>
        public static string AssistiveLine(PrintedTextFace liveFace, bool controllerChoosesTheFace)
        {
            if (controllerChoosesTheFace)
            {
                return "LIVE TEXT: " + EitherFaceWord +
                    " - YOU CHOOSE ON EACH PRIMARY ACTION THIS ROUND";
            }

            var word = Word(liveFace);
            if (word.Length == 0)
            {
                return string.Empty;
            }

            return "LIVE TEXT: " + word + " - THE TIE BREAKER COIN SHOWS " +
                SideWord(CoinSelectedTextQuery.SideShowing(liveFace));
        }

        /// <summary>
        /// The team whose side the coin is showing, named the way the status bar names one. It is said
        /// because the coin is already on screen at all times - the rail's tie-breaker banner - so the
        /// card's reading ties the marker to the thing a player can go and look at.
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

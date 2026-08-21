using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>What a definition says about the printed words on the face it was transcribed from.</summary>
    public enum CardSourceContentKind
    {
        /// <summary>
        /// The definition has no source face at all. A synthetic fixture card is the only thing this
        /// describes: it was invented for a test, so there are no printed words to transcribe and
        /// claiming any would be inventing source authority.
        /// </summary>
        NotSourced = 0,

        /// <summary>The source face was read and carries no rules text.</summary>
        PrintsNoText = 1,

        /// <summary>The source face was read and its rules text is carried verbatim.</summary>
        Transcribed = 2
    }

    /// <summary>
    /// The exact words one card's source face prints, carried on the definition and read by nothing.
    ///
    /// Milestone 8.3, closing the 7.9a review's F-CARD-08: transcribed card text had no home in the
    /// content model at all, so a verified definition's numbers lived in code while its sentences lived
    /// only in a document. That is a gap in both directions - an implemented definition could not be
    /// audited against its own printed wording, and a future card's text had nowhere to sit while its
    /// composition was still owed.
    ///
    /// <para><b>Inert on purpose.</b> This is audit provenance, not a rules input. No rule may read it,
    /// which <c>CardDefinitionTests.EveryDefinitionStatesItsSourceContentOrAnExplicitAbsence_AndNoRuleReadsIt</c>
    /// enforces mechanically over the production assemblies: the moment a query branched on these
    /// words the project would have a printed-text parser, which every card milestone since 4.4 has
    /// gone out of its way not to build. What a card's text *does* is composed as typed effect content
    /// through <see cref="PrintedTextComposition"/>; what it *says* is this.</para>
    ///
    /// <para><b>Three states, because two would lie.</b> A verified card that prints no rules text and
    /// a synthetic card that has no printed face at all are different facts, and collapsing them would
    /// let a fixture pass a check that exists to make verified content answerable to its source.
    /// <see cref="CardDefinition"/> couples the state to <see cref="CardContentAuthority"/> in both
    /// directions rather than merely permitting one: verified content must state
    /// <see cref="CardSourceContentKind.Transcribed"/> or <see cref="CardSourceContentKind.PrintsNoText"/>,
    /// and a synthetic fixture may state only <see cref="NotSourced"/> - which is milestone 8.3's
    /// "synthetic test values are not used as Wasp authority" made true by construction rather than by
    /// review.</para>
    /// </summary>
    public sealed class CardSourceContent
    {
        private CardSourceContent(CardSourceContentKind kind, string printedText)
        {
            Kind = kind;
            PrintedText = printedText;
        }

        /// <summary>
        /// The explicit statement that this definition has no source face. It is the only value a
        /// synthetic fixture may carry, and the only one a verified definition may not.
        /// </summary>
        public static CardSourceContent NotSourced { get; } =
            new CardSourceContent(CardSourceContentKind.NotSourced, string.Empty);

        /// <summary>The explicit statement that the source face was read and prints no rules text.</summary>
        public static CardSourceContent PrintsNoText { get; } =
            new CardSourceContent(CardSourceContentKind.PrintsNoText, string.Empty);

        /// <summary>
        /// The words the face prints, verbatim. Printed line breaks are typographic rather than
        /// semantic and are joined with a single space, which is the same convention
        /// <c>Rules/HERO_WASP.md</c>'s transcription table states; nothing else is normalised, so a
        /// reminder-text parenthesis or a missing comma survives into the definition exactly as
        /// printed.
        /// </summary>
        public static CardSourceContent Transcribed(string printedText)
        {
            if (string.IsNullOrWhiteSpace(printedText))
            {
                throw new ArgumentException(
                    "A transcription carries the words the face prints; state CardSourceContent." +
                    "PrintsNoText when the face carries none.",
                    nameof(printedText));
            }

            return new CardSourceContent(CardSourceContentKind.Transcribed, printedText);
        }

        public CardSourceContentKind Kind { get; }

        /// <summary>
        /// The transcribed words, or an empty string for either absence. Empty rather than null so a
        /// diagnostic that prints provenance cannot fault on a card that prints nothing.
        /// </summary>
        public string PrintedText { get; }

        /// <summary>Whether this definition states anything about a source face it was read from.</summary>
        public bool StatesASourceFace => Kind != CardSourceContentKind.NotSourced;
    }
}

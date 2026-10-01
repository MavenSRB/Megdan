using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    public sealed class CardDefinition
    {
        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression)
            : this(
                id,
                contentRevision,
                displayName,
                initiative,
                contentAuthority,
                progression,
                Enumerable.Empty<ActionDefinition>())
        {
        }

        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression,
            IEnumerable<ActionDefinition> actionDefinitions,
            CardSourceContent sourceContent = null)
            : this(id, contentRevision, displayName, initiative, contentAuthority, progression,
                actionDefinitions, null, sourceContent)
        {
        }

        /// <summary>
        /// Milestone 7.3's overload, and the only one that can build rulebook p.16's Ultimate.
        ///
        /// <paramref name="ultimateRule"/> is the permanent ability the Ultimate prints, and it is
        /// required for an Ultimate and refused for everything else. Both directions matter: an
        /// Ultimate with no ability is a card whose entire printed content is missing, and an ordinary
        /// card with one would supply a rule from a card the page gives no permanent ability to.
        ///
        /// <paramref name="sourceContent"/> is milestone 8.3's, and it is optional here only in the
        /// sense that a synthetic fixture has nothing to say. What makes the absence a statement rather
        /// than an omission is the coupling below: a definition claiming
        /// <see cref="CardContentAuthority.VerifiedOfficial"/> must carry its transcription or state
        /// that its face prints none, and a synthetic fixture may carry neither. So a verified card
        /// cannot reach an unstated source content by leaving an argument out - it cannot be
        /// constructed at all.
        ///
        /// <paramref name="printedTextSelection"/> is milestone 8.75b's, and it defaults to
        /// <see cref="Cards.PrintedTextSelection.None"/> because that is what every printed face in
        /// the game said until Ignatia the Mad: one text, always in force. See
        /// <see cref="PrintedTextSelection"/>.
        /// </summary>
        public CardDefinition(
            CardDefinitionId id,
            string contentRevision,
            string displayName,
            InitiativeValue initiative,
            CardContentAuthority contentAuthority,
            CardProgressionProfile progression,
            IEnumerable<ActionDefinition> actionDefinitions,
            UltimateRuleDefinition ultimateRule,
            CardSourceContent sourceContent = null,
            PrintedTextSelection printedTextSelection = Cards.PrintedTextSelection.None)
        {
            if (!Enum.IsDefined(typeof(PrintedTextSelection), printedTextSelection))
            {
                throw new ArgumentException(
                    "A card definition states one of the declared printed-text selections.",
                    nameof(printedTextSelection));
            }

            if (!id.IsDefined)
            {
                throw new ArgumentException("Card definition requires a defined ID.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(contentRevision))
            {
                throw new ArgumentException("Card definition requires a content revision.", nameof(contentRevision));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Card definition requires a display name.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(CardContentAuthority), contentAuthority) ||
                contentAuthority == CardContentAuthority.None)
            {
                throw new ArgumentException("Card definition requires explicit content authority.", nameof(contentAuthority));
            }

            // Milestone 8.3, closing the 7.9a review's F-CARD-08. The coupling runs both ways because
            // each direction removes a different lie. Verified content with no transcription is a
            // definition nobody can audit against the face it claims to come from; a synthetic fixture
            // carrying printed words would be inventing source authority, which is exactly what this
            // milestone's "synthetic test values are not used as Wasp authority" forbids. Neither is
            // reachable by omission - the argument's default is the synthetic answer, and a verified
            // definition is refused it.
            var statedSourceContent = sourceContent ?? CardSourceContent.NotSourced;
            if (contentAuthority == CardContentAuthority.VerifiedOfficial &&
                !statedSourceContent.StatesASourceFace)
            {
                throw new ArgumentException(
                    "Verified official content carries the words its source face prints, or states " +
                    "through CardSourceContent.PrintsNoText that the face prints none.",
                    nameof(sourceContent));
            }

            if (contentAuthority != CardContentAuthority.VerifiedOfficial &&
                statedSourceContent.StatesASourceFace)
            {
                throw new ArgumentException(
                    $"A {contentAuthority} definition has no source face; only verified official " +
                    "content may state what one prints.",
                    nameof(sourceContent));
            }

            if (progression == null)
            {
                // Deliberately not defaulted. Milestone 7.1's acceptance check requires every
                // definition to represent its applicable card colour or a verified absence, and a
                // required argument is what makes the absence a statement - CardProgressionProfile.
                // NotStated - instead of the value a caller happened not to pass.
                throw new ArgumentNullException(
                    nameof(progression),
                    "Card definition requires an explicit progression profile; state " +
                    "CardProgressionProfile.NotStated when the definition carries no colour or Tier.");
            }

            // Milestone 7.3. Initiative is required of every card except p.16's Ultimate, which prints
            // none: the printed card carries no value column at all, and it never enters the Initiative
            // order because it is never played - the page places it "to the left side of your hero
            // dashboard" and its ability "is always in effect". Giving one an Initiative would print a
            // value the card does not have, which is the mistake milestone 7.2c corrected elsewhere.
            if (progression.IsUltimate)
            {
                if (initiative.IsDefined)
                {
                    throw new ArgumentException(
                        "An Ultimate prints no Initiative; it is never played (rulebook p.16).",
                        nameof(initiative));
                }
            }
            else if (progression.IsSpell)
            {
                // Milestone 9.30b. A spell card prints no Initiative either: it is never played, only
                // cast from the spellbook by the card that is.
                if (initiative.IsDefined)
                {
                    throw new ArgumentException(
                        "A spell card prints no Initiative; it is cast, never played.",
                        nameof(initiative));
                }
            }
            else if (!initiative.IsDefined)
            {
                throw new ArgumentException(
                    "Card definition requires a positive Initiative value.", nameof(initiative));
            }

            if (progression.IsUltimate == (ultimateRule == null))
            {
                throw new ArgumentException(
                    progression.IsUltimate
                        ? "An Ultimate states the permanent ability it supplies (rulebook p.16)."
                        : "Only an Ultimate carries a permanent ability; this card is " +
                          $"{progression.Classification}.",
                    nameof(ultimateRule));
            }

            if (actionDefinitions == null)
            {
                throw new ArgumentNullException(nameof(actionDefinitions));
            }

            Id = id;
            ContentRevision = contentRevision;
            DisplayName = displayName;
            Initiative = initiative;
            ContentAuthority = contentAuthority;
            Progression = progression;
            UltimateRule = ultimateRule;
            SourceContent = statedSourceContent;
            PrintedTextSelection = printedTextSelection;
            var materializedActionDefinitions = actionDefinitions.ToArray();
            ActionDefinitions = Array.AsReadOnly(materializedActionDefinitions);

            if (ActionDefinitions.Any(actionDefinition => actionDefinition == null))
            {
                throw new ArgumentException("Action definitions must not contain null entries.", nameof(actionDefinitions));
            }

            if (ActionDefinitions.Select(actionDefinition => actionDefinition.Id).Distinct().Count() != ActionDefinitions.Count)
            {
                throw new ArgumentException("Action definitions must have unique local IDs.", nameof(actionDefinitions));
            }
        }

        public CardDefinitionId Id { get; }

        public string ContentRevision { get; }

        public string DisplayName { get; }

        public InitiativeValue Initiative { get; }

        public CardContentAuthority ContentAuthority { get; }

        /// <summary>
        /// Milestone 7.1's printed colour, Tier, Basic classification, and same-colour next-Tier
        /// candidates. Never null: the constructor refuses a definition that states nothing rather than
        /// substituting an absence of its own.
        /// </summary>
        public CardProgressionProfile Progression { get; }

        /// <summary>
        /// Milestone 7.3's permanent ability, on an Ultimate and on nothing else. The constructor
        /// refuses both mismatches, so a non-null value here is a card p.16 gives an always-on rule to
        /// and a null one is a card it does not.
        /// </summary>
        public UltimateRuleDefinition UltimateRule { get; }

        /// <summary>
        /// Milestone 8.3's transcribed printed text, or the explicit statement that there is none to
        /// transcribe. Never null, and read by no rule: it is audit provenance that ties a definition
        /// to the face it came from, and
        /// <c>CardDefinitionTests.EveryDefinitionStatesItsSourceContentOrAnExplicitAbsence_AndNoRuleReadsIt</c>
        /// fails the build if any production type calls this getter. What a card's text does is typed
        /// effect content; what it says is here.
        /// </summary>
        public CardSourceContent SourceContent { get; }

        /// <summary>
        /// Milestone 8.75b. Whether this face prints more than one text, and what selects which one is
        /// in force - <see cref="Cards.PrintedTextSelection"/>.
        ///
        /// <para><b>Unlike <see cref="SourceContent"/>, a surface IS meant to read this.</b> The two
        /// are opposites on purpose: the transcription carries the printed WORDS and no production type
        /// may touch it, because reading words would be a printed-text parser; this carries one typed
        /// fact ABOUT those words, so the HUD can tell a player which half of a two-text card is live
        /// without recognising a hero, a card or a capability. No rule reads it either - the coin branch
        /// is an ordinary <c>Conditional</c> over a predicate capability and would resolve identically
        /// if this property did not exist.</para>
        ///
        /// <para>It is schema- and wire-neutral: definitions are content every seat reconstructs from
        /// its own registered catalogs (<c>MatchSessionProjection.Definitions</c> passes them through
        /// and <c>NetworkMatchClient.Definitions</c> reads the locally trusted list), so no snapshot
        /// byte and no frame carries a definition field.</para>
        /// </summary>
        public PrintedTextSelection PrintedTextSelection { get; }

        public IReadOnlyList<ActionDefinition> ActionDefinitions { get; }

        public CardDefinitionReference Reference => new CardDefinitionReference(Id, ContentRevision);
    }
}

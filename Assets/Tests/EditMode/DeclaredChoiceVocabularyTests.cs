using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.37b. What a player is actually shown when a decision's candidates are integers
    /// that are not counts.
    ///
    /// <para><b>The defect these cases pin.</b> An <see cref="EffectValueKind.Integer"/> candidate
    /// meant a printed NUMBER every time one appeared in this build, so the presenter labelled it with
    /// its own digits and the prompt called the decision a number. Tali the Spiritcaller offers six
    /// card colours on Commune with Spirits and six board directions on Cold Snap, and both reached a
    /// player as the buttons "0" through "5". Every tier below presentation was green while that was
    /// true - the gate table, the journeys and the whole harness - because none of them reads a
    /// label.</para>
    ///
    /// <para><b>Why the meaning is declared rather than inferred.</b> Nothing in a set of integers
    /// says which enum they came from, and guessing from their values is exactly what this build
    /// refuses elsewhere: <c>EffectChoiceCandidatePreview.ConsequenceIntent</c> exists so presentation
    /// can tell a removal from every other choice "without reading a capability name, card text,
    /// display name, target kind, or colour". So the handler that opens the choice declares it, and
    /// these cases assert the declaration survives every hop to the surface that draws it.</para>
    /// </summary>
    public sealed class DeclaredChoiceVocabularyTests
    {
        // -------------------------------------------------------------------------------------------
        // The label. A colour is named, never numbered.
        // -------------------------------------------------------------------------------------------

        [TestCase(CardColor.Gold, "GOLD")]
        [TestCase(CardColor.Silver, "SILVER")]
        [TestCase(CardColor.Red, "RED")]
        [TestCase(CardColor.Green, "GREEN")]
        [TestCase(CardColor.Blue, "BLUE")]
        [TestCase(CardColor.Purple, "PURPLE")]
        public void ADeclaredColour_IsNamedRatherThanNumbered(CardColor colour, string expected)
        {
            Assert.That(
                EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.CardColor, (int)colour),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// The two-word directions are the reason the label is composed from the member name rather
        /// than taken from a per-member table: `NorthEast` is the printed name with its space removed,
        /// so putting it back is a reading and not a translation.
        /// </summary>
        [TestCase(HexDirection.East, "EAST")]
        [TestCase(HexDirection.NorthEast, "NORTH EAST")]
        [TestCase(HexDirection.NorthWest, "NORTH WEST")]
        [TestCase(HexDirection.West, "WEST")]
        [TestCase(HexDirection.SouthWest, "SOUTH WEST")]
        [TestCase(HexDirection.SouthEast, "SOUTH EAST")]
        public void ADeclaredDirection_IsNamedRatherThanNumbered(HexDirection direction, string expected)
        {
            Assert.That(
                EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.HexDirection, (int)direction),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// A value outside the declared vocabulary is a fault rather than a fallback to its digits.
        /// The vocabulary is the handler's own statement about the set it offered, so a candidate
        /// outside it means the two disagree - and drawing a number would hide exactly that.
        /// </summary>
        [Test]
        public void AValueOutsideTheDeclaredVocabulary_IsRefusedRatherThanNumbered()
        {
            Assert.That(
                () => EffectChoiceLabels.VocabularyLabel(EffectChoiceValueVocabulary.CardColor, 99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NoDeclaredVocabulary_HasNoLabelToGive()
        {
            Assert.That(
                () => EffectChoiceLabels.VocabularyLabel(EffectChoiceValueVocabulary.None, 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // -------------------------------------------------------------------------------------------
        // The prompt. A declared vocabulary answers the subject outright.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ADeclaredColourChoice_IsAColourDecisionRatherThanANumberOne()
        {
            var choice = ColourChoice();

            var subject = EffectChoicePromptLabels.SubjectOf(choice, null);

            Assert.That(subject, Is.EqualTo(EffectChoiceSubject.Colour));
            Assert.That(EffectChoicePromptLabels.SectionLabel(subject), Is.EqualTo("NAME A COLOUR"));
        }

        [Test]
        public void ADeclaredDirectionChoice_IsADirectionDecision()
        {
            var choice = DirectionChoice();

            var subject = EffectChoicePromptLabels.SubjectOf(choice, null);

            Assert.That(subject, Is.EqualTo(EffectChoiceSubject.Direction));
            Assert.That(
                EffectChoicePromptLabels.SectionLabel(subject), Is.EqualTo("CHOOSE A DIRECTION"));
        }

        /// <summary>
        /// The negative control the two cases above need: an integer choice that declares NOTHING is
        /// still the printed number it has always been, so nothing about the kinetic "3 or 4 spaces"
        /// distance moved.
        /// </summary>
        [Test]
        public void AnUndeclaredIntegerChoice_IsStillAPrintedNumber()
        {
            var choice = new PendingEffectChoice(
                new EffectResolutionId("r"), new EffectChoiceId("c"), new EffectNodeId("n"),
                new HeroId("hero-a"), EffectChoiceKind.SelectTarget,
                new[] { Candidate(3), Candidate(4) }, 1, 1);

            Assert.That(
                EffectChoicePromptLabels.SubjectOf(choice, null),
                Is.EqualTo(EffectChoiceSubject.Number));
        }

        // -------------------------------------------------------------------------------------------
        // The declaration's own guard, and the hops it has to survive.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A vocabulary describes integers, so a choice declaring one over candidates that are not
        /// integers is malformed rather than merely unhelpful.
        /// </summary>
        [Test]
        public void ADeclaredVocabularyOverNonIntegerCandidates_IsRefused()
        {
            Assert.That(
                () => new PendingEffectChoice(
                    new EffectResolutionId("r"), new EffectChoiceId("c"), new EffectNodeId("n"),
                    new HeroId("hero-a"), EffectChoiceKind.SelectTarget,
                    new[] { EffectChoiceCandidate.Value_(EffectValue.Hero(new HeroId("hero-b"))) },
                    1, 1, valueVocabulary: EffectChoiceValueVocabulary.CardColor),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        /// Selecting inside a standing decision must not erase what its candidates mean. It would:
        /// <c>WithSelected</c> rebuilds the pause, and a rebuild that dropped the declaration would
        /// turn six colours back into six digits the moment a player pressed one of them.
        /// </summary>
        [Test]
        public void SelectingWithinAColourChoice_KeepsTheDeclaration()
        {
            var choice = ColourChoice();

            var afterSelecting = choice.WithSelected(new[] { Candidate((int)CardColor.Green) });

            Assert.That(
                afterSelecting.ValueVocabulary, Is.EqualTo(EffectChoiceValueVocabulary.CardColor));
        }

        /// <summary>
        /// The handler is the only thing that knows, so the request is where the declaration starts.
        /// </summary>
        [Test]
        public void TheRequestCarriesTheDeclarationFromTheHandler()
        {
            var request = new EffectChoiceRequest(
                EffectChooserRole.Actor,
                new[] { EffectValue.Integer((int)CardColor.Gold) },
                1,
                1,
                valueVocabulary: EffectChoiceValueVocabulary.CardColor);

            Assert.That(request.ValueVocabulary, Is.EqualTo(EffectChoiceValueVocabulary.CardColor));
        }

        // -------------------------------------------------------------------------------------------
        // The placed-token face.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A token is addressed by its definition id and nothing else, so two definitions that share a
        /// physical piece share a file rather than a code path - which is what Tali's two Totems are.
        /// </summary>
        [Test]
        public void APlacedTokenFace_IsAddressedByItsDefinitionId()
        {
            Assert.That(
                PlacedTokenArtwork.ResourcePath(new TokenDefinitionId("tali-ice")),
                Is.EqualTo("Private/Tokens/tali-ice"));
        }

        /// <summary>
        /// Absence is the tracked project's ordinary state - the art is git-ignored - so an undefined
        /// or unprepared definition answers with no path and no throw, and the view keeps its disc.
        /// </summary>
        [Test]
        public void AnUndefinedTokenDefinition_HasNoFaceAndDoesNotThrow()
        {
            Assert.That(PlacedTokenArtwork.ResourcePath(default), Is.Null);
        }

        // -------------------------------------------------------------------------------------------

        private static PendingEffectChoice ColourChoice() =>
            new PendingEffectChoice(
                new EffectResolutionId("r"), new EffectChoiceId("c"), new EffectNodeId("n"),
                new HeroId("hero-a"), EffectChoiceKind.SelectTarget,
                new[]
                {
                    CardColor.Gold, CardColor.Silver, CardColor.Red,
                    CardColor.Green, CardColor.Blue, CardColor.Purple
                }.Select(colour => Candidate((int)colour)).ToArray(),
                1, 1, valueVocabulary: EffectChoiceValueVocabulary.CardColor);

        private static PendingEffectChoice DirectionChoice() =>
            new PendingEffectChoice(
                new EffectResolutionId("r"), new EffectChoiceId("c"), new EffectNodeId("n"),
                new HeroId("hero-a"), EffectChoiceKind.SelectTarget,
                Enum.GetValues(typeof(HexDirection)).Cast<HexDirection>()
                    .Select(direction => Candidate((int)direction)).ToArray(),
                1, 1, valueVocabulary: EffectChoiceValueVocabulary.HexDirection);

        private static EffectChoiceCandidate Candidate(int value) =>
            EffectChoiceCandidate.Value_(EffectValue.Integer(value));
    }
}

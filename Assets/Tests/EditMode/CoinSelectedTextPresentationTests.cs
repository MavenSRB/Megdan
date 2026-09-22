using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75b. What a player actually SEES of which printed text is live: the corner marker
    /// on every card surface, the card's hover reading, and the two controls the decision window draws
    /// while the coin is not what decides.
    ///
    /// <para><b>The defect these cases pin.</b> A card that prints a ☀ line and a ☾ line is half
    /// unreadable without being told which half applies, and the fact selecting it - the Tie Breaker
    /// coin - lives on the opposite side of the screen. Every tier below presentation can be green
    /// while that is true, which is the same blind spot milestone 8.37b's own fixture records: none of
    /// them reads a label or looks at a card.</para>
    ///
    /// <para>Unity-only (it builds real <c>VisualElement</c> faces), so it is named in
    /// <c>Tools/DotnetTests/UnityOnlyTests.props</c>. The declaration and the mapping underneath are
    /// <c>CoinSelectedTextDeclarationTests</c>, which runs in the offline harness too.</para>
    /// </summary>
    public sealed class CoinSelectedTextPresentationTests
    {
        // -------------------------------------------------------------------------------------------
        // The controls. Two buttons naming the consequence, never the buttons "1" and "2".
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheOfferedFaces_AreNamedByWhatApplyingThemDoes()
        {
            Assert.That(
                EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.Sun),
                Is.EqualTo("APPLY THE SUN TEXT"));
            Assert.That(
                EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.Moon),
                Is.EqualTo("APPLY THE MOON TEXT"));
        }

        /// <summary>
        /// Taking the answer back says the answer's own name, which is the correction milestone 8.37b
        /// made once for every vocabulary and this one inherits rather than re-learning.
        /// </summary>
        [Test]
        public void TakingTheAnswerBack_SaysTheAnswerBack()
        {
            Assert.That(
                EffectChoiceLabels.DeselectLabel(
                    EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.Moon),
                Is.EqualTo("UNDO APPLY THE MOON TEXT"));
        }

        /// <summary>
        /// The unrevealed coin is not an offered candidate, so meeting it here means the handler and
        /// its declared vocabulary disagree - a fault, never a digit drawn instead.
        /// </summary>
        [Test]
        public void AFaceOutsideTheOfferedPair_IsRefusedRatherThanNumbered()
        {
            Assert.That(
                () => EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.None),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => EffectChoiceLabels.VocabularyLabel(
                    EffectChoiceValueVocabulary.PrintedTextFace, 99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// The prompt names the question. Without the declaration the same two candidates are integers,
        /// so the status line would read "Choose 1 of 2 printed numbers" over a choice about text.
        /// </summary>
        [Test]
        public void ADeclaredFaceChoice_IsAChoiceOfPrintedTextRatherThanOfNumbers()
        {
            var choice = FaceChoice();

            var subject = EffectChoicePromptLabels.SubjectOf(choice, null);

            Assert.That(subject, Is.EqualTo(EffectChoiceSubject.TextFace));
            Assert.That(
                EffectChoicePromptLabels.SectionLabel(subject), Is.EqualTo("CHOOSE THE FACE"));
            Assert.That(
                EffectChoicePromptLabels.StatusLine(choice, subject),
                Does.Contain("2 printed texts"));
        }

        /// <summary>
        /// The declaration survives a selection inside the standing pause. It would not survive a
        /// rebuild that dropped it, and the two faces would become the digits 1 and 2 the moment a
        /// player pressed one of them.
        /// </summary>
        [Test]
        public void SelectingWithinAFaceChoice_KeepsTheDeclaration()
        {
            var afterSelecting = FaceChoice()
                .WithSelected(new[] { Candidate((int)PrintedTextFace.Sun) });

            Assert.That(
                afterSelecting.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.PrintedTextFace));
        }

        /// <summary>
        /// The handler is the only thing that knows what its two integers mean, so the declaration
        /// starts at the pause it opens - and this is the exact request Ignatia's coin predicate makes
        /// while Equilibrium's round marker stands.
        /// </summary>
        [Test]
        public void TheCoinPredicatesOwnPause_DeclaresTheVocabularyAndOffersBothFaces()
        {
            var request = new EffectChoiceRequest(
                EffectChooserRole.Actor,
                new[]
                {
                    EffectValue.Integer((int)PrintedTextFace.Sun),
                    EffectValue.Integer((int)PrintedTextFace.Moon)
                },
                1,
                1,
                valueVocabulary: EffectChoiceValueVocabulary.PrintedTextFace);

            Assert.That(
                request.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.PrintedTextFace));
            Assert.That(
                request.Candidates.Select(candidate => candidate.IntegerValue).ToArray(),
                Is.EqualTo(new[] { (int)PrintedTextFace.Sun, (int)PrintedTextFace.Moon }));
        }

        // -------------------------------------------------------------------------------------------
        // The marker, on the one builder every card face in the HUD goes through.
        // -------------------------------------------------------------------------------------------

        [TestCase(TeamSide.Red, "SUN")]
        [TestCase(TeamSide.Blue, "MOON")]
        public void ACoinSelectedCard_WearsTheLiveFaceInItsCorner(TeamSide revealed, string expected)
        {
            var face = BuildFace(CoinSelectedCard(
                CoinSelectedTextViewModel.ForCoinOnly(
                    CoinSelectedDefinition(),
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(revealed))));

            var label = Descendants(face).OfType<Label>().SingleOrDefault(element =>
                element.name == ConfiguredCardFaceView.LiveTextFaceMarkerLabelName);

            Assert.That(label, Is.Not.Null,
                "a card whose printed text the coin selects says which half of itself is live.");
            Assert.That(label.text, Is.EqualTo(expected));
        }

        /// <summary>
        /// While the controller may apply either text, naming one of them would be a lie - so the
        /// marker names both, in the HUD's one Active-effect colour rather than the coin's.
        /// </summary>
        [Test]
        public void UnderTheRoundLongFreeChoice_TheMarkerNamesBothFaces()
        {
            var face = BuildFace(CoinSelectedCard(CoinSelectedTextViewModel.ControllerChooses()));

            var marker = Descendants(face).Single(element =>
                element.name == ConfiguredCardFaceView.LiveTextFaceMarkerName);
            var label = Descendants(marker).OfType<Label>().Single();

            Assert.That(label.text, Is.EqualTo("SUN / MOON"));
            Assert.That(
                marker.style.backgroundColor.value,
                Is.EqualTo(MatchHudTheme.ActiveEffectAccent));
        }

        /// <summary>
        /// An unrevealed coin selects neither text, so there is nothing true to put in the corner and
        /// nothing is put there. It is the one case a DECLARED card still draws no marker.
        /// </summary>
        [Test]
        public void AnUnrevealedCoin_DrawsNoMarkerEvenOnADeclaredCard()
        {
            var face = BuildFace(CoinSelectedCard(
                CoinSelectedTextViewModel.ForCoinOnly(
                    CoinSelectedDefinition(), TieBreakerState.Unrevealed)));

            Assert.That(
                Descendants(face).Any(element =>
                    element.name == ConfiguredCardFaceView.LiveTextFaceMarkerName),
                Is.False);
        }

        /// <summary>
        /// The negative control the whole feature rests on: a card of a hero who prints one text is
        /// rendered exactly as it was, with no marker element anywhere in its subtree.
        /// </summary>
        [Test]
        public void AOneTextCard_IsRenderedExactlyAsItWas()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var card = new TrayCardViewModel(
                new CardInstanceId("wasp-shock"),
                definition.Reference,
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false);

            Assert.That(card.LiveText, Is.SameAs(CoinSelectedTextViewModel.NotSelected));
            Assert.That(card.AssistiveLabel, Does.Not.Contain("LIVE TEXT"));
            Assert.That(
                Descendants(BuildFace(card)).Any(element =>
                    element.name == ConfiguredCardFaceView.LiveTextFaceMarkerName),
                Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // The hover and assistive reading.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheCardsHoverReading_NamesTheLiveTextAndTheSideTheCoinShows()
        {
            var card = CoinSelectedCard(
                CoinSelectedTextViewModel.ForCoinOnly(
                    CoinSelectedDefinition(),
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Blue)));

            Assert.That(
                card.AssistiveLabel,
                Does.EndWith("LIVE TEXT: MOON - THE TIE BREAKER COIN SHOWS BLUE"));
            Assert.That(card.AssistiveLabel, Does.StartWith(card.DisplayName),
                "the live text is said last, after the whole card has been read.");
        }

        [Test]
        public void UnderTheFreeChoice_TheHoverReadingSaysTheChoiceIsTheirs()
        {
            var card = CoinSelectedCard(CoinSelectedTextViewModel.ControllerChooses());

            Assert.That(
                card.AssistiveLabel,
                Does.EndWith("LIVE TEXT: SUN / MOON - YOU CHOOSE ON EACH PRIMARY ACTION THIS ROUND"));
        }

        // -------------------------------------------------------------------------------------------
        // The composition, over the facts a seat holds.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheReading_IsComposedFromTheDeclarationTheCoinAndTheLiveGrant()
        {
            var hero = new HeroId("hero-ignatia");
            var bolt = CardInstance.CreateInHand(
                new CardInstanceId("bolt"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosBolt).Reference,
                hero);
            var equilibrium = CardInstance.CreateInHand(
                new CardInstanceId("equilibrium"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium).Reference,
                hero);
            var cards = new[] { bolt, equilibrium };
            var sun = TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red);

            var byCoin = CoinSelectedTextViewModel.For(
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosBolt),
                hero,
                sun,
                Array.Empty<CardInstanceId>(),
                cards,
                IgnatiaCardContent.AllDefinitions);
            var freed = CoinSelectedTextViewModel.For(
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosBolt),
                hero,
                sun,
                new[] { equilibrium.Id },
                cards,
                IgnatiaCardContent.AllDefinitions);
            var oneText = CoinSelectedTextViewModel.For(
                IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium),
                hero,
                sun,
                new[] { equilibrium.Id },
                cards,
                IgnatiaCardContent.AllDefinitions);

            Assert.That(byCoin.LiveFace, Is.EqualTo(PrintedTextFace.Sun));
            Assert.That(byCoin.RevealedSide, Is.EqualTo(TeamSide.Red));
            Assert.That(byCoin.ControllerChoosesTheFace, Is.False);
            Assert.That(freed.ControllerChoosesTheFace, Is.True,
                "the live grant overrides the coin while it stands.");
            Assert.That(oneText, Is.SameAs(CoinSelectedTextViewModel.NotSelected),
                "the card that frees the choice prints one text of its own.");
        }

        /// <summary>
        /// A caller with no projection - p.15's upgrade candidates built from definitions alone - reads
        /// no live face rather than guessing one.
        /// </summary>
        [Test]
        public void NoProjection_ReadsNoLiveFace()
        {
            Assert.That(
                CoinSelectedTextViewModel.For(CoinSelectedDefinition(), new HeroId("h"), null),
                Is.SameAs(CoinSelectedTextViewModel.NotSelected));
        }

        // -------------------------------------------------------------------------------------------
        // The bot seat. Milestone 8.75b's correction of milestone 8.40j's reach.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// 8.40j's colour arm is untouched: among the colours a guess may name, one this seat can see
        /// lying faceup in an open zone is scored down.
        /// </summary>
        [Test]
        public void AColourCandidate_IsStillScoredByWhatIsLyingFaceup()
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var seen = new CardInstance(
                new CardInstanceId("seen"),
                definition.Reference,
                new HeroId("hero-wasp"),
                CardStateFacts.DiscardedUnplayed());
            var definitions = new[] { definition };

            var unseen = BotPolicy.DeclaredVocabularyUtility(
                EffectChoiceValueVocabulary.CardColor,
                (int)CardColor.Purple,
                new[] { seen },
                definitions);
            var visible = BotPolicy.DeclaredVocabularyUtility(
                EffectChoiceValueVocabulary.CardColor,
                (int)definition.Progression.Color,
                new[] { seen },
                definitions);

            Assert.That(visible, Is.LessThan(unseen));
        }

        /// <summary>
        /// The correction. Every other declared vocabulary scores NEUTRALLY, so the conductor's
        /// deterministic tiebreak names the answer.
        ///
        /// <para>Before this, the colour arm was reached by every declared vocabulary and read its
        /// integer as a <see cref="CardColor"/> - so a board DIRECTION has been scored since milestone
        /// 8.40j by counting the cards of the colour whose ordinal happened to match it. Nothing looked
        /// wrong because both readings produce a number and a bot always answers.</para>
        /// </summary>
        [TestCase(EffectChoiceValueVocabulary.HexDirection, (int)HexDirection.East)]
        [TestCase(EffectChoiceValueVocabulary.HexDirection, (int)HexDirection.NorthWest)]
        [TestCase(EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.Sun)]
        [TestCase(EffectChoiceValueVocabulary.PrintedTextFace, (int)PrintedTextFace.Moon)]
        public void AnyOtherDeclaredVocabulary_IsNotScoredAsAColour(
            EffectChoiceValueVocabulary vocabulary, int value)
        {
            var definition = WaspCardContent.Definition(WaspCardContent.Shock);
            var seen = new CardInstance(
                new CardInstanceId("seen"),
                definition.Reference,
                new HeroId("hero-wasp"),
                CardStateFacts.DiscardedUnplayed());

            Assert.That(
                BotPolicy.DeclaredVocabularyUtility(
                    vocabulary, value, new[] { seen }, new[] { definition }),
                Is.Zero,
                "a vocabulary with no tactical reading of its own is answered by the conductor's " +
                "tiebreak, not by counting cards of an unrelated colour.");
        }

        // -------------------------------------------------------------------------------------------

        private static PendingEffectChoice FaceChoice() =>
            new PendingEffectChoice(
                new EffectResolutionId("r"), new EffectChoiceId("c"), new EffectNodeId("n"),
                new HeroId("hero-ignatia"), EffectChoiceKind.SelectTarget,
                new[]
                {
                    Candidate((int)PrintedTextFace.Sun), Candidate((int)PrintedTextFace.Moon)
                },
                1, 1, valueVocabulary: EffectChoiceValueVocabulary.PrintedTextFace);

        private static EffectChoiceCandidate Candidate(int value) =>
            EffectChoiceCandidate.Value_(EffectValue.Integer(value));

        /// <summary>
        /// A synthetic face that DECLARES its text is coin-selected. Ignatia is not dealt, so this is
        /// the shape a surface meets rather than her own catalog - and it is the same declaration her
        /// cards carry, which <c>CoinSelectedTextDeclarationTests</c> pins face by face.
        /// </summary>
        private static CardDefinition CoinSelectedDefinition() =>
            new CardDefinition(
                new CardDefinitionId("coin-selected-showcase"),
                "coin-selected-rev-1",
                "Coin Selected Showcase",
                new InitiativeValue(7),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                Array.Empty<ActionDefinition>(),
                null,
                null,
                PrintedTextSelection.TieBreakerCoin);

        private static TrayCardViewModel CoinSelectedCard(CoinSelectedTextViewModel liveText)
        {
            var definition = CoinSelectedDefinition();
            return new TrayCardViewModel(
                new CardInstanceId("coin-selected-1"),
                definition.Reference,
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false,
                liveText: liveText);
        }

        /// <summary>
        /// The card through the one builder every surface in the HUD uses. No artwork is prepared for a
        /// synthetic definition, so this exercises the generated fallback - which is the harder case for
        /// the marker, because the fallback fills the content box with its own frame.
        /// </summary>
        private static VisualElement BuildFace(TrayCardViewModel card) =>
            DirectorHandsUnityView.BuildStandaloneCard(card);

        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            yield return root;
            foreach (var child in root.Children())
            {
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}

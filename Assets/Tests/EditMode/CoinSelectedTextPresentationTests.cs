using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    /// Milestone 8.75b, RE-STATED at milestone 8.75e. What a player actually sees of which printed
    /// text is live - which, since 8.75e, is the rail's tie-breaker banner and the decision window's
    /// two controls, and NOTHING ON THE CARD.
    ///
    /// <para><b>The correction these cases now pin.</b> 8.75b put a `SUN` / `MOON` badge in the
    /// top-right corner of every card whose printed text the Tie Breaker coin selects. The user met it
    /// in the first live playtest of the twenty-two-hero build - "why do we have top right SUN symbol
    /// ? Oo" - and settled it: "Drop the badge and rely on the coin strip alone." The coin is on
    /// screen at all times already; the badge repeated that one fact in the corner of every card in
    /// the hand. So the marker is gone, the reading it added to a card's hover is gone with it, and
    /// what these cases hold down is the ABSENCE: every card of every hero - hers included - goes
    /// through the one shared builder and comes out exactly as it did before 8.75b.</para>
    ///
    /// <para><b>What was deliberately kept.</b> The typed declaration
    /// (<see cref="CardDefinition.PrintedTextSelection"/>) and the mapping underneath it
    /// (<see cref="CoinSelectedTextQuery"/>): her content branches on the mapping, the decision
    /// window's face choice is named from it, and milestone 7.9's player-path gate reads the
    /// declaration to bind the <c>CoinSelectedTextReading</c> shape. What went was the one consumer
    /// nobody wanted - the card surface.</para>
    ///
    /// <para>Unity-only (it builds real <c>VisualElement</c> faces), so it is named in
    /// <c>Tools/DotnetTests/UnityOnlyTests.props</c>. The declaration and the mapping underneath are
    /// <c>CoinSelectedTextDeclarationTests</c>, which runs in the offline harness too.</para>
    /// </summary>
    public sealed class CoinSelectedTextPresentationTests
    {
        // -------------------------------------------------------------------------------------------
        // The controls. Two buttons naming the consequence, never the buttons "1" and "2". UNTOUCHED
        // by 8.75e: the window is where a player is ASKED, and an asked question needs its words.
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
        // Milestone 8.75e. The card, on the one builder every card face in the HUD goes through: the
        // declaration changes NOTHING a player sees, which is the whole of what the user asked for.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The claim in its strongest form: two cards identical in every respect except that one
        /// DECLARES its printed text is selected by the coin build the same tree, element for element,
        /// name for name, word for word. A badge, a tooltip tail, a colour - any of them would show up
        /// here as a difference, and the declaration is what used to produce all three.
        /// </summary>
        [Test]
        public void ACoinSelectedCard_IsRenderedExactlyAsAOneTextCardIs()
        {
            Assert.That(
                Describe(BuildFace(CardDeclaring(PrintedTextSelection.TieBreakerCoin))),
                Is.EqualTo(Describe(BuildFace(CardDeclaring(PrintedTextSelection.None)))),
                "Since milestone 8.75e no card surface reads `PrintedTextSelection` at all, so the " +
                "declaration cannot change one pixel of the card that carries it.");
        }

        /// <summary>
        /// The same for the other declared member - Equilibrium's, the card that FREES the choice. It
        /// wore the loudest badge of the three (the HUD's Active-effect violet, naming both faces), and
        /// it is the one the user was looking at when the tray showed `ACTIVE` beside it anyway.
        /// </summary>
        [Test]
        public void ACardThatFreesTheFaceChoice_IsRenderedExactlyAsAOneTextCardIs()
        {
            Assert.That(
                Describe(BuildFace(CardDeclaring(PrintedTextSelection.FreesTheCoinChoice))),
                Is.EqualTo(Describe(BuildFace(CardDeclaring(PrintedTextSelection.None)))));
        }

        /// <summary>
        /// The sweep behind the two equalities: whichever of the three declarations a card carries, no
        /// element of its tree is the retired marker and no reading of it names a printed face. Stated
        /// over the words rather than over the element name alone, because the badge's tooltip said
        /// `LIVE TEXT: SUN - THE TIE BREAKER COIN SHOWS RED` and a marker rebuilt under a different
        /// name would still be that sentence on a card.
        /// </summary>
        [TestCase(PrintedTextSelection.None)]
        [TestCase(PrintedTextSelection.TieBreakerCoin)]
        [TestCase(PrintedTextSelection.FreesTheCoinChoice)]
        public void NoCardSurface_CarriesAMarkerOrNamesAPrintedFace(PrintedTextSelection declared)
        {
            var card = CardDeclaring(declared);

            foreach (var element in Descendants(BuildFace(card)))
            {
                Assert.That(element.name ?? string.Empty, Does.Not.Contain("live-text"),
                    "The corner badge was `gd-card-live-text-face`, and it is gone.");
                Assert.That(element.tooltip ?? string.Empty, Does.Not.Contain("LIVE TEXT"),
                    "and so is the sentence it hovered.");
            }

            Assert.That(card.AssistiveLabel, Does.Not.Contain("LIVE TEXT"));
            Assert.That(card.AssistiveLabel, Does.Not.Contain("TIE BREAKER"));
        }

        /// <summary>
        /// The negative control the whole correction rests on: a card of a hero who prints one text is
        /// rendered exactly as it always was, with nothing anywhere in its subtree about a coin.
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

            Assert.That(definition.PrintedTextSelection, Is.EqualTo(PrintedTextSelection.None));
            Assert.That(card.AssistiveLabel, Does.Not.Contain("LIVE TEXT"));
            Assert.That(
                Descendants(BuildFace(card)).Any(element =>
                    (element.name ?? string.Empty).Contains("live-text")),
                Is.False);
        }

        /// <summary>
        /// HER OWN DEALT FACE, not a synthetic one. Chaos Bolt prints a ☀ line and a ☾ line and
        /// declares <see cref="PrintedTextSelection.TieBreakerCoin"/>; it must now draw the way Wasp's
        /// Shock draws, which is to say with nothing added. The synthetic cases above prove the
        /// builder; this proves the hero the user was actually looking at.
        /// </summary>
        [Test]
        public void HerOwnDealtFace_DrawsWithNothingAddedToIt()
        {
            var definition = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosBolt);
            Assert.That(
                definition.PrintedTextSelection,
                Is.EqualTo(PrintedTextSelection.TieBreakerCoin),
                "Arrange: the declaration is still made - it is what the gate matrix and her own " +
                "content read. Only the badge went.");

            var card = new TrayCardViewModel(
                new CardInstanceId("ignatia-chaos-bolt"),
                definition.Reference,
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false);

            Assert.That(card.AssistiveLabel, Does.Not.Contain("LIVE TEXT"));
            Assert.That(card.AssistiveLabel, Does.EndWith("AVAILABLE"),
                "Her card's reading ends where every other hero's ends.");
            Assert.That(
                Descendants(BuildFace(card)).Any(element =>
                    (element.name ?? string.Empty).Contains("live-text")),
                Is.False);
        }

        /// <summary>
        /// The builder keeps no name for a marker it no longer draws, and a tray card keeps no reading
        /// of the coin for a surface to find. A re-added badge would have to re-add both, which is the
        /// point of asking: this is the case that reds if the corner the user cleared is refilled.
        /// </summary>
        [Test]
        public void NeitherTheBuilderNorTheTrayCard_KeepsADeadLiveTextReading()
        {
            Assert.That(
                typeof(ConfiguredCardFaceView)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => field.FieldType == typeof(string))
                    .Select(field => (string)field.GetValue(null))
                    .Any(value => (value ?? string.Empty).Contains("live-text")),
                Is.False,
                "`gd-card-live-text-face` is not a name this builder knows any more.");
            Assert.That(
                typeof(TrayCardViewModel).GetProperty(
                    "LiveText" /* absence-intended: 8.75e removed the reading; it must stay removed */,
                    BindingFlags.Public | BindingFlags.Instance),
                Is.Null,
                "and no view model carries a live-text reading nothing reads.");
        }

        // -------------------------------------------------------------------------------------------
        // The hover reading.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheCardsHoverReading_SaysNothingAboutWhichPrintedTextIsLive()
        {
            Assert.That(
                CardDeclaring(PrintedTextSelection.TieBreakerCoin).AssistiveLabel,
                Is.EqualTo(CardDeclaring(PrintedTextSelection.None).AssistiveLabel),
                "Milestone 8.75b appended `- LIVE TEXT: SUN - THE TIE BREAKER COIN SHOWS RED` to a " +
                "declared card's reading; 8.75e dropped that with the badge it explained.");
        }

        [Test]
        public void UnderTheFreeChoice_TheHoverReadingSaysNothingEither()
        {
            Assert.That(
                CardDeclaring(PrintedTextSelection.FreesTheCoinChoice).AssistiveLabel,
                Is.EqualTo(CardDeclaring(PrintedTextSelection.None).AssistiveLabel));
        }

        // -------------------------------------------------------------------------------------------
        // The query that survived, over the facts a seat holds. It is what the decision window, the
        // gate matrix and her own content read - so it is pinned here as well as in the offline
        // declaration fixture, which is where the milestone 8.75b composition cases went.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The live face is the coin's and the grant overrides it - the same three facts milestone
        /// 8.75b composed a view model from, now read straight off the query by the two readers that
        /// are left.
        /// </summary>
        [Test]
        public void TheLiveFace_IsStillTheCoinsUnlessAStandingGrantFreesIt()
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

            Assert.That(CoinSelectedTextQuery.LiveFace(sun), Is.EqualTo(PrintedTextFace.Sun));
            Assert.That(
                CoinSelectedTextQuery.SideShowing(PrintedTextFace.Sun), Is.EqualTo(TeamSide.Red),
                "and the side it is showing is where the rail's banner puts it, which is now the " +
                "only place that reading is drawn.");
            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    hero, Array.Empty<CardInstanceId>(), cards, IgnatiaCardContent.AllDefinitions),
                Is.False);
            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    hero, new[] { equilibrium.Id }, cards, IgnatiaCardContent.AllDefinitions),
                Is.True,
                "the live grant overrides the coin while it stands - which is what raises the " +
                "decision window's own face choice.");
        }

        /// <summary>
        /// The window's words are the last place a face is named to a player, so they are pinned where
        /// the badge's used to be.
        /// </summary>
        [Test]
        public void TheWindowsOwnWords_AreTheOnlyPlaceAFaceIsNamedOnTheHud()
        {
            Assert.That(PrintedTextFaceLabels.ApplySunTextLabel, Is.EqualTo("APPLY THE SUN TEXT"));
            Assert.That(PrintedTextFaceLabels.ApplyMoonTextLabel, Is.EqualTo("APPLY THE MOON TEXT"));
            Assert.That(PrintedTextFaceLabels.SectionLabel, Is.EqualTo("CHOOSE THE FACE"));
            Assert.That(
                PrintedTextFaceLabels.Word(PrintedTextFace.Sun), Is.EqualTo("SUN"));
            Assert.That(
                PrintedTextFaceLabels.SideWord(CoinSelectedTextQuery.SunSide), Is.EqualTo("RED"),
                "and the word for the side the rail's banner is on, which is what a journey says " +
                "while it proves which text is live.");
        }

        // -------------------------------------------------------------------------------------------
        // The bot seat. Milestone 8.75b's correction of milestone 8.40j's reach. UNTOUCHED.
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
        /// A synthetic face carrying one of the three declarations and IDENTICAL in every other
        /// respect - the same id, revision, name, Initiative and colour - so two of them differ by the
        /// declaration alone and a tree comparison between them measures exactly that.
        /// </summary>
        private static CardDefinition DefinitionDeclaring(PrintedTextSelection declared) =>
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
                declared);

        private static TrayCardViewModel CardDeclaring(PrintedTextSelection declared)
        {
            var definition = DefinitionDeclaring(declared);
            return new TrayCardViewModel(
                new CardInstanceId("coin-selected-1"),
                definition.Reference,
                definition.DisplayName,
                definition.Progression.Color,
                CurrentRoundCards.ReadFace(definition),
                TrayCardGroup.Available,
                0,
                true,
                false);
        }

        /// <summary>
        /// The card through the one builder every surface in the HUD uses. No artwork is prepared for a
        /// synthetic definition, so this exercises the generated fallback - which is the harder case,
        /// because the fallback fills the content box with its own frame and the badge used to sit on
        /// top of it.
        /// </summary>
        private static VisualElement BuildFace(TrayCardViewModel card) =>
            DirectorHandsUnityView.BuildStandaloneCard(card);

        /// <summary>
        /// The whole built tree as one comparable string: every element's type, name, tooltip, drawn
        /// text and background colour, in draw order and at its depth. Two faces that describe
        /// identically are the same card on screen.
        /// </summary>
        private static string Describe(VisualElement root)
        {
            var builder = new StringBuilder();
            Describe(root, 0, builder);
            return builder.ToString();
        }

        private static void Describe(VisualElement element, int depth, StringBuilder builder)
        {
            builder
                .Append(new string(' ', depth * 2))
                .Append(element.GetType().Name)
                .Append('|').Append(element.name ?? string.Empty)
                .Append('|').Append(element.tooltip ?? string.Empty)
                .Append('|').Append((element as TextElement)?.text ?? string.Empty)
                .Append('|').Append(element.style.backgroundColor.value)
                .Append('|').Append(element.style.color.value)
                .Append('\n');
            foreach (var child in element.Children())
            {
                Describe(child, depth + 1, builder);
            }
        }

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

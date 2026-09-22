using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75b. The typed declaration that lets a surface say WHICH of a two-text card's
    /// printed texts is live, and the one query that owns the sun/moon mapping.
    ///
    /// <para><b>What these cases exist to stop.</b> Ignatia the Mad is the first hero whose action
    /// cards print two texts with the Tie Breaker coin selecting between them, so a player needs to be
    /// told which half applies - and the only ways presentation could work that out for itself are the
    /// ones this codebase forbids: recognising a hero, a card name, a capability id, or the printed
    /// words. The content therefore STATES it and the HUD reads the statement. Every case below fails
    /// for a reason the printed cards name.</para>
    ///
    /// <para>Core only, so it runs in the offline harness as well as under Unity. The drawing of the
    /// marker itself is <c>CoinSelectedTextPresentationTests</c>.</para>
    /// </summary>
    public sealed class CoinSelectedTextDeclarationTests
    {
        // -------------------------------------------------------------------------------------------
        // The mapping. One home, and the user's own ruling.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// USER-SETTLED 2026-09-22: "Moon is blue team, sun is read team." The engine models the coin
        /// only as a revealed side, so this is the whole of what makes a face a sun or a moon.
        /// </summary>
        [Test]
        public void TheCoinsSunFaceIsRedAndItsMoonFaceIsBlue()
        {
            Assert.That(CoinSelectedTextQuery.SunSide, Is.EqualTo(TeamSide.Red));
            Assert.That(CoinSelectedTextQuery.MoonSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(
                CoinSelectedTextQuery.FaceOf(TeamSide.Red), Is.EqualTo(PrintedTextFace.Sun));
            Assert.That(
                CoinSelectedTextQuery.FaceOf(TeamSide.Blue), Is.EqualTo(PrintedTextFace.Moon));
        }

        /// <summary>
        /// The mapping is stated once. The hero's own content reads it rather than repeating it,
        /// because presentation may not reference the verified namespace at all and two copies of one
        /// ruling could disagree silently about which face is which.
        /// </summary>
        [Test]
        public void TheHerosContentReadsTheSharedMappingRatherThanRepeatingIt()
        {
            Assert.That(IgnatiaComponentContent.SunFace, Is.EqualTo(CoinSelectedTextQuery.SunSide));
            Assert.That(IgnatiaComponentContent.MoonFace, Is.EqualTo(CoinSelectedTextQuery.MoonSide));
            Assert.That(IgnatiaComponentContent.SunFace,
                Is.Not.EqualTo(IgnatiaComponentContent.MoonFace));
        }

        /// <summary>
        /// An unrevealed coin selects NEITHER text. It is a real state, and the one answer that must
        /// never be guessed: a card resolved under it has no live text at all.
        /// </summary>
        [Test]
        public void AnUnrevealedCoinSelectsNeitherPrintedText()
        {
            Assert.That(
                CoinSelectedTextQuery.LiveFace(TieBreakerState.Unrevealed),
                Is.EqualTo(PrintedTextFace.None));
            Assert.That(
                CoinSelectedTextQuery.FaceOf(TeamSide.None), Is.EqualTo(PrintedTextFace.None));
            Assert.That(
                CoinSelectedTextQuery.SideShowing(PrintedTextFace.None), Is.EqualTo(TeamSide.None));
            Assert.That(
                CoinSelectedTextQuery.Opposite(PrintedTextFace.None),
                Is.EqualTo(PrintedTextFace.None));
        }

        [Test]
        public void TheLiveFaceAndTheSideItIsPrintedOnAreInverses()
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var face = CoinSelectedTextQuery.LiveFace(
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(side));
                Assert.That(face, Is.Not.EqualTo(PrintedTextFace.None));
                Assert.That(CoinSelectedTextQuery.SideShowing(face), Is.EqualTo(side));
                Assert.That(
                    CoinSelectedTextQuery.Opposite(CoinSelectedTextQuery.Opposite(face)),
                    Is.EqualTo(face));
            }

            Assert.That(
                CoinSelectedTextQuery.Opposite(PrintedTextFace.Sun),
                Is.EqualTo(PrintedTextFace.Moon));
        }

        /// <summary>
        /// The two offered integers are the two symbols' ordinals, and neither is the default integer -
        /// the trap every optional Integer in this codebase carries, and the reason
        /// <see cref="CardColor.None"/> takes zero in the vocabulary beside this one.
        /// </summary>
        [Test]
        public void NeitherOfferedFaceIsTheDefaultInteger()
        {
            Assert.That((int)PrintedTextFace.None, Is.Zero);
            Assert.That((int)PrintedTextFace.Sun, Is.Not.Zero);
            Assert.That((int)PrintedTextFace.Moon, Is.Not.Zero);
            Assert.That((int)PrintedTextFace.Sun, Is.Not.EqualTo((int)PrintedTextFace.Moon));
        }

        // -------------------------------------------------------------------------------------------
        // The declaration on the definition.
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every card printed before this hero states one text, and the default says so rather than
        /// leaving the question open.
        /// </summary>
        [Test]
        public void ADefinitionThatSaysNothing_PrintsOneText()
        {
            var definition = new CardDefinition(
                new CardDefinitionId("plain"),
                "rev-1",
                "Plain",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                Array.Empty<ActionDefinition>());

            Assert.That(
                definition.PrintedTextSelection, Is.EqualTo(PrintedTextSelection.None));
        }

        [Test]
        public void ADefinitionStatesOneOfTheDeclaredSelections()
        {
            Assert.That(
                () => new CardDefinition(
                    new CardDefinitionId("bogus"),
                    "rev-1",
                    "Bogus",
                    new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    Array.Empty<ActionDefinition>(),
                    null,
                    null,
                    (PrintedTextSelection)99),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        /// The printed reading, face by face. Seventeen of Ignatia's nineteen faces carry a ☀ line and
        /// a ☾ line with "Apply the text matching the symbol on the Tie Breaker coin"; Equilibrium is
        /// the one that FREES the choice instead of being subject to it, and the Ultimate, Chaos
        /// Incarnate, prints a standing ability rather than two texts.
        /// </summary>
        [Test]
        public void EveryTwoTextFaceDeclaresTheCoinAndOnlyEquilibriumFreesIt()
        {
            var freed = new[] { IgnatiaCardContent.Equilibrium };
            var noTwoTexts = new[] { IgnatiaCardContent.ChaosIncarnate };

            foreach (var definition in IgnatiaCardContent.AllDefinitions)
            {
                var expected =
                    freed.Any(id => id.Equals(definition.Id))
                        ? PrintedTextSelection.FreesTheCoinChoice
                        : noTwoTexts.Any(id => id.Equals(definition.Id))
                            ? PrintedTextSelection.None
                            : PrintedTextSelection.TieBreakerCoin;

                Assert.That(
                    definition.PrintedTextSelection, Is.EqualTo(expected),
                    definition.DisplayName + " declares the wrong printed-text selection; its " +
                    "transcription is '" + definition.SourceContent.PrintedText + "'.");
            }
        }

        /// <summary>
        /// The negative control every marker test needs: nothing any of the twenty-one dealt heroes
        /// prints selects its text by the coin, so the marker cannot appear on one of their cards and
        /// their faces render exactly as they did.
        /// </summary>
        [Test]
        public void NoOtherHerosPrintedFaceDeclaresACoinSelectedText()
        {
            foreach (var contentClass in VerifiedContentCorpus.ContentClasses())
            {
                if (contentClass == typeof(IgnatiaCardContent))
                {
                    continue;
                }

                foreach (var definition in VerifiedContentCorpus.AllDefinitionsOf(contentClass))
                {
                    Assert.That(
                        definition.PrintedTextSelection,
                        Is.EqualTo(PrintedTextSelection.None),
                        VerifiedContentCorpus.OwnerOf(contentClass) + "'s " +
                        definition.DisplayName + " prints one text.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // The round-long free choice, read from a seat's own facts.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AHeroSourcingTheFreeingCardMayChooseTheFace()
        {
            var hero = new HeroId("hero-ignatia");
            var card = CardInstance.CreateInHand(
                new CardInstanceId("equilibrium-1"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium).Reference,
                hero);

            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    hero, new[] { card.Id }, new[] { card }, IgnatiaCardContent.AllDefinitions),
                Is.True);
        }

        /// <summary>
        /// The grant is the CONTROLLER's. A second hero standing beside them reads the coin, which is
        /// what "you may apply either ☀ or ☾ card text" says and what the effect's own
        /// controller-hero scope already means.
        /// </summary>
        [Test]
        public void AnotherHeroIsStillBoundByTheCoin()
        {
            var hero = new HeroId("hero-ignatia");
            var other = new HeroId("hero-wasp");
            var card = CardInstance.CreateInHand(
                new CardInstanceId("equilibrium-1"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium).Reference,
                hero);

            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    other, new[] { card.Id }, new[] { card }, IgnatiaCardContent.AllDefinitions),
                Is.False);
        }

        /// <summary>
        /// A card that is not sourcing a live effect grants nothing, which is the whole of what makes
        /// the round-long marker a round-long one: the moment the authority ends it, the card drops out
        /// of the live-source list and this answers false again.
        /// </summary>
        [Test]
        public void NoLiveEffect_MeansNoFreeChoice()
        {
            var hero = new HeroId("hero-ignatia");
            var card = CardInstance.CreateInHand(
                new CardInstanceId("equilibrium-1"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium).Reference,
                hero);

            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    hero,
                    Array.Empty<CardInstanceId>(),
                    new[] { card },
                    IgnatiaCardContent.AllDefinitions),
                Is.False);
        }

        /// <summary>
        /// A live effect from a card that does NOT free the choice - every Active effect the twenty-one
        /// dealt heroes create, and Ignatia's own coin-selected faces - leaves the coin deciding.
        /// </summary>
        [Test]
        public void ALiveEffectFromSomeOtherCard_LeavesTheCoinDeciding()
        {
            var hero = new HeroId("hero-ignatia");
            var card = CardInstance.CreateInHand(
                new CardInstanceId("chaos-bolt-1"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosBolt).Reference,
                hero);

            Assert.That(
                CoinSelectedTextQuery.MayChooseTheFace(
                    hero, new[] { card.Id }, new[] { card }, IgnatiaCardContent.AllDefinitions),
                Is.False);
        }
    }
}

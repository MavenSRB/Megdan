using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.8a's player-facing surfaces: the colour-coded current-round tray, the two compact
    /// hero overviews, the `DIRECTOR` drawer, and the one contextual region that now also draws a paused
    /// system transition.
    ///
    /// Everything here is driven through a real <see cref="MatchSession"/>. There is no projection double
    /// anywhere in this fixture, deliberately: the acceptance rules are about what a player sees of an
    /// authoritative position, and a hand-built projection could show a position no match reaches.
    /// </summary>
    public sealed class MatchHudPresentationTests
    {
        private IDisposable generatedCardFallbackScope;

        [SetUp]
        public void UseGeneratedCardsUnlessATestExplicitlySelectsArtwork()
        {
            generatedCardFallbackScope = ConfiguredCardArtwork.UseTextureLoader(_ => null);
        }

        [TearDown]
        public void RestoreConfiguredCardArtworkLoader()
        {
            generatedCardFallbackScope?.Dispose();
            generatedCardFallbackScope = null;
        }

        // -------------------------------------------------------------------------------------------
        // The rail's Ultimate marker (milestone 8.15)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheRail_ShowsTheUnlockedUltimateAsARevealableIconReadingUnlocked()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorInRadiusOfAMinion(
                MatchConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                    TeamSide.Red),
                out _, out _, out _);
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            foreach (var hero in view.Model.Heroes)
            {
                Assert.That(hero.UltimateCard, Is.Not.Null,
                    "Both mirrored Wasps unlocked High Voltage, and each rail says so.");
                Assert.That(hero.UltimateCard.DisplayName, Is.EqualTo("High Voltage"));
                Assert.That(hero.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"),
                    "The lifecycle band claims no turn, hand, pile or deck the card is not in.");
                Assert.That(hero.UltimateLabel, Is.EqualTo("ULTIMATE"));
                Assert.That(hero.UltimateCard.AssistiveLabel, Does.Contain("UNLOCKED"),
                    "The fact is available without a pointer.");
            }

            var icons = Descendants(view.Root)
                .Where(element => element.name == "gd-ultimate-card")
                .ToArray();
            Assert.That(icons, Has.Length.EqualTo(2),
                "One marker per rail, drawn through the shared mini-card builder.");
        }

        [Test]
        public void TheRail_DrawsNoUltimateRowWhileTheCardWaitsInTheDeck()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            Assert.That(view.Model.Heroes, Has.All.Matches<HeroOverviewViewModel>(hero =>
                hero.UltimateCard == null),
                "p.6 leaves the Ultimate in the deck, and a deck card is not player-visible state.");
            Assert.That(Descendants(view.Root).Any(element => element.name == "gd-ultimate-card"),
                Is.False, "No marker is drawn for a card the unlock has not turned faceup.");
        }

        // -------------------------------------------------------------------------------------------
        // The tray: five colours, the three groups, and the order they are read in
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheTray_DrawsEveryCurrentRoundCardWithItsOwnPrintedColourAndTheColoursName()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            var hand = view.Model.ViewedHand;
            Assert.That(hand.TrayCards, Has.Count.EqualTo(5),
                "p.6 deals five cards, and the tray is that hero's whole current round.");
            Assert.That(
                hand.TrayCards.Select(card => card.Color).OrderBy(color => (int)color),
                Is.EqualTo(new[]
                {
                    CardColor.Gold, CardColor.Silver, CardColor.Red, CardColor.Green, CardColor.Blue
                }),
                "All five printed colours are drawn, each from its own definition.");

            foreach (var card in hand.TrayCards)
            {
                var instance = session.Projection.Cards.Single(value =>
                    value.Id.Equals(card.CardInstanceId));
                var definition = session.Projection.Definitions.Single(value =>
                    value.Reference.Equals(instance.DefinitionReference));
                Assert.That(card.Color, Is.EqualTo(definition.Progression.Color),
                    "The button surface is the card's projected authoritative colour.");
                Assert.That(card.ColorName, Is.EqualTo(MatchHudTheme.ColorName(card.Color)),
                    "and the colour is never the only thing that says which colour it is.");
                Assert.That(card.DisplayName, Is.EqualTo(definition.DisplayName));
                Assert.That(
                    card.ValueLine,
                    Does.Contain(MatchHudTheme.ChannelToken(PrintedValueChannel.Initiative)),
                    "The card's own printed values stay readable beside its name.");
            }
        }

        [Test]
        public void ACardsColour_IsNeverInferredFromTheTeamThatHoldsIt()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            foreach (var hand in view.Model.Hands)
            {
                var side = session.Projection.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(hand.HeroId)).TeamSide;
                var teamColour = side == TeamSide.Red ? CardColor.Red : CardColor.Blue;
                Assert.That(hand.TrayCards.Count(card => card.Color == teamColour), Is.EqualTo(1),
                    $"The {side} hand holds exactly one card of the colour that shares the team's " +
                    "name, which is what a colour derived from the team could never produce.");
                Assert.That(hand.TrayCards.Select(card => card.Color).Distinct().Count(), Is.EqualTo(5));
            }
        }

        [Test]
        public void TheTray_SortsPlayedThenAvailableThenDiscardedAndNamesNoRound()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            Assert.That(view.Model.ViewedHand.TrayCards.Select(card => card.Group).Distinct(),
                Is.EqualTo(new[] { TrayCardGroup.Available }),
                "Before anybody commits, every card of the round is still available.");

            PlayTurns(session, 2);
            presenter.Refresh();
            var hand = view.Model.ViewedHand;
            var groups = hand.TrayCards.Select(card => card.Group).ToArray();
            Assert.That(groups.Take(2), Is.EqualTo(new[] { TrayCardGroup.Played, TrayCardGroup.Played }));
            Assert.That(hand.TrayCards.Take(2).Select(card => card.PlayedTurnIndex),
                Is.EqualTo(new[] { 1, 2 }),
                "Played cards read left to right in ascending turn order.");
            Assert.That(hand.TrayCards[0].HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
            Assert.That(hand.TrayCards.Skip(2).Select(card => card.Group).Distinct(),
                Is.EqualTo(new[] { TrayCardGroup.Available }));

            foreach (var card in hand.TrayCards)
            {
                Assert.That(card.HistoryLabel, Does.Not.Contain("ROUND"),
                    "A played card names its turn and never a round.");
            }
        }

        [Test]
        public void ADiscardedCard_SitsAtTheFarRightAndIsReadFromTheProjectedLifecycle()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var heroId);
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.SelectViewedHero(heroId);

            var hand = view.Model.Hands.Single(value => value.HeroId.Equals(heroId));
            var discarded = hand.TrayCards.Where(card => card.Group == TrayCardGroup.Discarded).ToArray();
            Assert.That(discarded, Is.Not.Empty, "The fixture discarded a card through a Defense.");
            Assert.That(hand.TrayCards.Last().Group, Is.EqualTo(TrayCardGroup.Discarded));
            Assert.That(discarded.Select(card => card.HistoryLabel).Distinct(),
                Is.EqualTo(new[] { "DISCARDED" }));
            foreach (var card in discarded)
            {
                Assert.That(
                    session.Projection.Cards.Single(value => value.Id.Equals(card.CardInstanceId))
                        .State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "The group is a reading of authoritative card location, not presenter bookkeeping.");
                Assert.That(card.IsSelectable, Is.False);
            }
        }

        [Test]
        public void ARoundReset_RedrawsTheTrayFromTheNewRoundsOwnHistory()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            ConfiguredMatchFixtures.PlayOneOrdinaryRound(session);
            ConfiguredMatchFixtures.FinishEndOfRound(session);
            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));

            presenter.Refresh();
            foreach (var card in view.Model.ViewedHand.TrayCards)
            {
                Assert.That(card.Group, Is.Not.EqualTo(TrayCardGroup.Played),
                    "The new round has no history yet, so no card is drawn as played in it.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.5a: the printed face - its words, and the four homes its values are read into
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryConfiguredWaspCard_ShowsItsVerifiedWordsAndItsNameKindAndLifecycle()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            var hand = view.Model.ViewedHand;
            Assert.That(hand.TrayCards, Has.Count.EqualTo(5),
                "p.5 deals Wasp's three Tier I cards and her two Basic ones.");
            foreach (var card in hand.TrayCards)
            {
                var definition = DefinitionOf(session, card.CardInstanceId);
                Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));
                Assert.That(definition.SourceContent.Kind, Is.EqualTo(CardSourceContentKind.Transcribed));
                Assert.That(card.DisplayName, Is.EqualTo(definition.DisplayName));
                Assert.That(card.Face.PrintedText, Is.EqualTo(definition.SourceContent.PrintedText),
                    "The tray shows the face's own transcribed words, verbatim and complete.");
                Assert.That(card.Face.EmptyTextLabel, Is.Empty,
                    "A face that prints words states no absence.");
                Assert.That(card.Face.PrimaryActionLabel, Is.Not.Empty);
                Assert.That(card.HistoryLabel, Is.EqualTo("AVAILABLE"));
            }

            // The five faces, as `Rules/HERO_WASP.md` records them. Named here rather than derived so
            // that a definition edited into a different card fails this rather than agreeing with itself.
            Assert.That(
                hand.TrayCards
                    .OrderBy(card => card.DisplayName, StringComparer.Ordinal)
                    .Select(card => $"{card.DisplayName}|{card.Face.ValueLine}"),
                Is.EqualTo(new[]
                {
                    "Lift Up|INI 10 · SKILL · RAD 2 · DEF 5 · MOV 3",
                    "Magnetic Dagger|INI 12 · BASIC ATTACK · ATK 3 · RAD 3 · DEF 2 · MOV 1",
                    "Shock|INI 8 · ATTACK · ATK 5 · RAD 2 · DEF 6 · MOV 4",
                    "Static Barrier|INI 13 · BASIC SKILL · RAD 2 · DEF 2",
                    "Stop Projectiles|INI 3 · DEFENSE · BLOCK · MOV 2"
                }));
        }

        [Test]
        public void EveryPrintedValue_IsDrawnInExactlyOneOfTheFourHomesItsTypedContentPutsItIn()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            foreach (var card in view.Model.ViewedHand.TrayCards)
            {
                var definition = DefinitionOf(session, card.CardInstanceId);
                var face = card.Face;
                var primary = definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                Assert.That(face.Initiative, Is.Not.Null);
                Assert.That(face.Initiative.Channel, Is.EqualTo(PrintedValueChannel.Initiative));
                Assert.That(face.Initiative.Amount, Is.EqualTo(definition.Initiative.Value),
                    "Initiative is the card's own, on its own flag.");

                // p.9's Range/Radius icon is an icon beside the primary action, not one of its numbers,
                // so the primary band takes the action's value and the spatial badge takes the icon.
                var primaryNumbers = primary.BaseValues
                    .Where(value => value.Channel != PrintedValueChannel.Range &&
                        value.Channel != PrintedValueChannel.Radius)
                    .ToArray();
                var icons = primary.BaseValues
                    .Where(value => value.Channel == PrintedValueChannel.Range ||
                        value.Channel == PrintedValueChannel.Radius)
                    .ToArray();
                Assert.That(face.PrimaryValue?.Label,
                    Is.EqualTo(primaryNumbers.Length == 0
                        ? null
                        : $"{MatchHudTheme.ChannelToken(primaryNumbers[0].Channel)} {primaryNumbers[0].Amount}"),
                    $"{card.DisplayName}: the primary band carries the primary action's own value.");
                Assert.That(face.SpatialValue?.Label,
                    Is.EqualTo(icons.Length == 0
                        ? null
                        : $"{MatchHudTheme.ChannelToken(icons[0].Channel)} {icons[0].Amount}"),
                    $"{card.DisplayName}: the spatial badge carries p.9's one Range or Radius icon.");

                var expectedSecondary = definition.ActionDefinitions
                    .Where(action => action.Origin != ActionDefinitionOrigin.PrintedPrimary)
                    .SelectMany(action => action.BaseValues)
                    .OrderBy(value => MatchHudTheme.ChannelOrder.ToList().IndexOf(value.Channel))
                    .Select(value => $"{MatchHudTheme.ChannelToken(value.Channel)} {value.Amount}")
                    .ToArray();
                Assert.That(face.SecondaryValues.Select(value => value.Label), Is.EqualTo(expectedSecondary),
                    $"{card.DisplayName}: every alternate action's printed value, and only those.");

                // Nothing appears twice and nothing is dropped: the four homes partition the face.
                var drawn = new[] { face.PrimaryValue, face.SpatialValue }
                    .Concat(face.SecondaryValues)
                    .Where(value => value != null)
                    .Select(value => $"{value.Channel}:{value.Amount}")
                    .OrderBy(value => value, StringComparer.Ordinal);
                var printed = definition.ActionDefinitions
                    .SelectMany(action => action.BaseValues)
                    .Select(value => $"{value.Channel}:{value.Amount}")
                    .OrderBy(value => value, StringComparer.Ordinal);
                Assert.That(drawn, Is.EqualTo(printed),
                    $"{card.DisplayName}: every printed value is drawn exactly once, in one region.");
            }
        }

        /// <summary>
        /// The grouping follows the action a value is printed on, not the words on the face.
        ///
        /// Wasp is the proof rather than a fixture built to pass: Static Barrier's text says "radius"
        /// five times and its face prints one Radius icon; Shock's text says "adjacent" while its badge
        /// reads the printed Radius; Charged Boomerang's text says "in range" and its Ranged
        /// classification comes from <see cref="AttackRangeKind"/>. A presenter that had learned to read
        /// the sentences would produce different regions on every one of them.
        /// </summary>
        [Test]
        public void TheGrouping_ComesFromTypedActionContentRatherThanFromTheWordsOrTheName()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();

            var barrier = FaceOf(session, "Static Barrier");
            Assert.That(barrier.PrintedText.Split(new[] { "radius" }, StringSplitOptions.None),
                Has.Length.GreaterThan(2), "The fixture is only meaningful while the words say it often.");
            Assert.That(barrier.SpatialValue.Label, Is.EqualTo("RAD 2"));
            Assert.That(barrier.PrimaryValue, Is.Null, "A Skill prints no combat number.");
            Assert.That(barrier.PrimaryActionLabel, Is.EqualTo("BASIC SKILL"),
                "p.15's Basic supertype is read from the progression profile.");

            var shock = FaceOf(session, "Shock");
            Assert.That(shock.PrintedText, Does.Contain("adjacent to you"));
            Assert.That(shock.PrimaryActionLabel, Is.EqualTo("ATTACK"),
                "A non-Ranged Attack carries no qualifier; p.13 gives it adjacency by rule.");
            Assert.That(shock.PrimaryValue.Label, Is.EqualTo("ATK 5"));
            Assert.That(shock.SpatialValue.Label, Is.EqualTo("RAD 2"));

            var block = FaceOf(session, "Stop Projectiles");
            Assert.That(block.PrintedText, Is.EqualTo("Block a ranged attack."));
            Assert.That(block.PrimaryActionLabel, Is.EqualTo("DEFENSE · BLOCK"),
                "p.14's Block is read from PrintedDefenseResolution, and the word 'ranged' in the " +
                "printed sentence is left where the player can read it rather than parsed into a label.");
            Assert.That(block.SpatialValue, Is.Null, "This face prints no Range or Radius icon.");
            Assert.That(block.SecondaryValues.Select(value => value.Label), Is.EqualTo(new[] { "MOV 2" }));
        }

        [Test]
        public void ARangedAttackAndTheUltimate_AreReadFromTheirOwnTypedContent()
        {
            // Neither is in p.5's starting hand, so both are read straight from the verified content the
            // configured match deals from. The Ultimate is the one card with no Initiative and no action
            // at all, and the one whose p.9 icon rides its permanent rule instead of an action.
            var boomerang = CurrentRoundCards.ReadFace(
                WaspCardContent.Definition(WaspCardContent.ChargedBoomerang));
            Assert.That(boomerang.PrimaryActionLabel, Is.EqualTo("ATTACK · RANGED"));
            Assert.That(boomerang.PrimaryValue.Label, Is.EqualTo("ATK 3"));
            Assert.That(boomerang.SpatialValue.Label, Is.EqualTo("RNG 3"));

            var ultimate = CurrentRoundCards.ReadFace(
                WaspCardContent.Definition(WaspCardContent.HighVoltage));
            Assert.That(ultimate.Initiative, Is.Null, "p.16's Ultimate prints no Initiative.");
            Assert.That(ultimate.PrimaryActionLabel, Is.EqualTo("ULTIMATE"));
            Assert.That(ultimate.PrimaryValue, Is.Null);
            Assert.That(ultimate.SecondaryValues, Is.Empty);
            Assert.That(ultimate.SpatialValue.Label, Is.EqualTo("RAD 3"),
                "Its Radius is printed on the permanent ability, which is the whole of what it has.");
        }

        [Test]
        public void TheTwoAbsences_AreDrawnAsTwoExplicitStatementsRatherThanAsInventedCopy()
        {
            var silent = CurrentRoundCards.ReadFace(new CardDefinition(
                new CardDefinitionId("test-silent-face"),
                "test-8-5a-rev-1",
                "Test - Silent Face",
                new InitiativeValue(7),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                Array.Empty<ActionDefinition>(),
                CardSourceContent.PrintsNoText));
            Assert.That(silent.PrintedTextKind, Is.EqualTo(CardSourceContentKind.PrintsNoText));
            Assert.That(silent.PrintedText, Is.Empty, "No words are invented for a face that prints none.");
            Assert.That(silent.EmptyTextLabel, Is.EqualTo("THIS FACE PRINTS NO RULES TEXT"));

            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            new DirectorHandsPresenter(session, view).Refresh();
            foreach (var card in view.Model.ViewedHand.TrayCards)
            {
                // The showcase hand is synthetic, so its cards have no source face at all - a different
                // fact from a read face that prints nothing, and drawn as a different sentence.
                Assert.That(card.Face.PrintedTextKind, Is.EqualTo(CardSourceContentKind.NotSourced));
                Assert.That(card.Face.PrintedText, Is.Empty);
                Assert.That(card.Face.EmptyTextLabel, Is.EqualTo("NO SOURCE FACE"));
            }
        }

        [Test]
        public void TheCardsRegions_AreDrawnAsNamedElementsAndTheWordsAreNeverTruncated()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            var buttons = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .ToArray();
            Assert.That(buttons, Has.Length.EqualTo(5));
            foreach (var button in buttons)
            {
                foreach (var region in new[]
                    {
                        "gd-card-name-band", "gd-card-secondary-values", "gd-card-primary-action",
                        "gd-card-rules", "gd-card-history"
                    })
                {
                    Assert.That(Descendants(button).Count(element => element.name == region), Is.EqualTo(1),
                        $"Each card draws exactly one {region}.");
                }

                Assert.That(Descendants(button).Count(element => element.name == "gd-card-initiative"),
                    Is.EqualTo(1), "Initiative has an upper-left flag of its own.");
            }

            var labels = buttons
                .SelectMany(Descendants)
                .OfType<Label>()
                .ToArray();
            foreach (var card in view.Model.ViewedHand.TrayCards)
            {
                foreach (var expected in new[]
                    {
                        card.DisplayName.ToUpperInvariant(), card.HistoryLabel,
                        card.Face.PrintedText, card.Face.PrimaryActionLabel
                    })
                {
                    Assert.That(labels.Any(label => label.text == expected), Is.True,
                        $"'{expected}' is drawn whole - no ellipsis, no prefix, no summary.");
                }

                // The printed colour is not written on the face - the user's 2026-08-21 decision - and
                // is therefore required to be in the reading that replaces it, on the same terms
                // milestone 7.8a set for a hero rail's turn slot.
                Assert.That(labels.Any(label => label.text == card.ColorName), Is.False,
                    "A card wears its colour rather than spelling it.");
                Assert.That(card.AssistiveLabel, Does.Contain(card.ColorName),
                    "but the colour still has a word, in this card's assistive and hover reading.");

                // Wrapping is what keeps a long sentence readable inside a fixed-width card; a label that
                // clipped instead would silently drop rules text.
                var words = labels.Single(label => label.text == card.Face.PrintedText);
                Assert.That(words.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
            }
        }

        [Test]
        public void TheFiveCardHand_FitsOneReadableRowWithoutHorizontalScrolling()
        {
            // Every 16:9 desktop resolves to the panel's 2560x1440 reference rectangle, regardless of
            // device-pixel count. Layout arithmetic is therefore asserted in that coordinate space.
            var panelWidth = MatchHudPanelScaling.ReferenceWidth;
            var panelHeight = MatchHudPanelScaling.ReferenceHeight;
            var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(panelWidth, panelHeight);
            var available = tray.width;
            var requiredWidth = ConfiguredMatchHudRenderer.CompactHandRightEdge;

            Assert.That(requiredWidth, Is.LessThanOrEqualTo(available),
                "The readable source-ratio hand fits the reserved band without horizontal travel.");
            var view = new DirectorHandsUnityView();
            new DirectorHandsPresenter(ConfiguredMatchFixtures.CreateVerifiedMirrorSession(), view)
                .Refresh();
            view.SetCompactPeekMode(true);
            var row = Descendants(view.Root).Single(element => element.name == "gd-card-buttons");
            Assert.That(row.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap));
            Assert.That(row.style.width.value.value, Is.EqualTo(requiredWidth).Within(0.01f),
                "All five ordered slots remain on one declared row inside the tray.");
            Assert.That(tray.height,
                Is.EqualTo(CardFootprint.Height + 40f).Within(0.01f),
                "and the tray spends only 40 reference units beyond the complete face; hand metadata " +
                "lives in the Director-side status dock.");
            Assert.That(
                CardFootprint.Width / CardFootprint.Height,
                Is.EqualTo(CardFootprint.ArtworkSourceWidth /
                    CardFootprint.ArtworkSourceHeight).Within(0.0001f),
                "The enlarged card remains exactly as portrait as its verified source.");
            Assert.That(ConfiguredMatchHudRenderer.CalculatePlayfieldRect(panelWidth, panelHeight).height,
                Is.GreaterThan(tray.height),
                "while the board stays the larger of the two surfaces.");
        }

        [Test]
        public void EveryCardSurface_KeepsItsTextReadableWithoutRelyingOnColourAlone()
        {
            foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
            {
                var ink = MatchHudTheme.FaceText(color);
                foreach (var plate in new[]
                    {
                        MatchHudTheme.Face(color), MatchHudTheme.FaceBand(color),
                        MatchHudTheme.FaceFlag(color)
                    })
                {
                    Assert.That(Contrast(plate, ink), Is.GreaterThanOrEqualTo(4.5f),
                        $"{color}: every plate on a card carries its own text legibly.");
                }

                Assert.That(
                    Contrast(MatchHudTheme.RulesPanelBackground, MatchHudTheme.RulesPanelAccent(color)),
                    Is.GreaterThanOrEqualTo(4.5f),
                    $"{color}: the spatial badge belongs to its card without becoming unreadable.");
                Assert.That(MatchHudTheme.ColorName(color), Is.Not.Empty,
                    "and every printed colour still has a word, for the reading that carries it.");
            }

            Assert.That(
                Contrast(MatchHudTheme.RulesPanelBackground, MatchHudTheme.RulesPanelText),
                Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(
                Contrast(MatchHudTheme.RulesPanelBackground, MatchHudTheme.RulesPanelMutedText),
                Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void TheRicherCard_ChangesNothingAboutSelectionAvailabilityOrTheOrderOfTheRow()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            var buttons = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .OfType<Button>()
                .ToArray();
            Assert.That(
                buttons.Select(button => button.tooltip.Split(new[] { " - " }, StringSplitOptions.None)[0]),
                Is.EqualTo(view.Model.ViewedHand.TrayCards.Select(card => card.DisplayName)),
                "Keyboard and pointer order is still the tray's own played/available/discarded order.");
            Assert.That(
                Descendants(view.Root)
                    .Where(element => element.name == "gd-card-slot")
                    .Count(slot => slot.style.top.value.value < 0f),
                Is.EqualTo(5),
                "Every still-playable card keeps the milestone 7.8a lift, which milestone 8.5b moved " +
                "onto the slot so the Active-effect outline rises with the card it belongs to.");
            Assert.That(buttons.All(button => button.enabledSelf), Is.True,
                "Every card the authority offers still carries its command.");

            var chosen = view.Model.ViewedHand.TrayCards[1];
            Assert.That(presenter.SelectCommitmentCard(chosen.CardInstanceId), Is.True);
            var selected = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .OfType<Button>()
                .ToArray();
            Assert.That(view.Model.ViewedHand.TrayCards.Count(card => card.IsSelected), Is.EqualTo(1));
            Assert.That(
                selected.Count(button =>
                    button.style.borderTopColor.value == MatchHudTheme.SelectedOutline),
                Is.EqualTo(1),
                "Selection is still one shared outline, never a colour swap - the card keeps its own " +
                "printed colour while it is chosen.");
            Assert.That(
                selected.Select(button => button.style.borderTopWidth.value).Distinct(),
                Is.EqualTo(new[] { 3f }),
                "and the outline's width never moves. A tray card's width is fixed and its height comes " +
                "from its content, so a border that grew on selection would re-wrap that card's rules " +
                "text, make it taller, and stretch the whole row with it.");
            Assert.That(view.Model.ViewedHand.TrayCards[1].DisplayName, Is.EqualTo(chosen.DisplayName),
                "and the row's order did not move because a card was selected.");
        }

        [Test]
        public void ASpentCard_KeepsItsWholeFaceWhileCarryingNoCommand()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            presenter.Refresh();

            var played = view.Model.ViewedHand.TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
            Assert.That(played.IsSelectable, Is.False);
            Assert.That(played.Face.PrintedText, Is.Not.Empty,
                "A spent card still shows what it said - reading the round back is what the tray is for.");

            var button = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .OfType<Button>()
                .First();
            Assert.That(button.enabledSelf, Is.False, "and it carries no command.");
            Assert.That(
                Descendants(view.Root).First(element => element.name == "gd-card-slot")
                    .style.top.value.value,
                Is.EqualTo(0f), "and it is not lifted.");
        }

        [Test]
        public void RestingOnAnOccupiedTurnSlot_RevealsTheCardThatWasPlayedOnThatTurn()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            presenter.Refresh();

            var hero = view.Model.Heroes[0];
            var played = hero.TurnSlots.Single(slot => slot.IsOccupied);
            var empty = hero.TurnSlots.First(slot => !slot.IsOccupied);
            Assert.That(played.TurnIndex, Is.EqualTo(1));
            Assert.That(played.PlayedCard, Is.Not.Null);
            Assert.That(played.PlayedCard.Group, Is.EqualTo(TrayCardGroup.Played));
            Assert.That(played.PlayedCard.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
            Assert.That(played.PlayedCard.Color, Is.EqualTo(played.Color),
                "The slot's swatch and the card it reveals are one card, read once.");
            Assert.That(empty.PlayedCard, Is.Null, "An empty slot has no card to reveal.");
            Assert.That(played.AssistiveLabel, Does.Contain(played.PlayedCard.DisplayName),
                "and the same fact is in the slot's assistive reading, without a pointer.");

            HeroTurnSlotViewModel reported = null;
            view.TurnSlotHighlighted += slot => reported = slot;
            Assert.That(view.HighlightedTurnSlot, Is.Null, "Nothing is revealed until the pointer rests.");

            view.ForwardTurnSlotHighlight(played);
            Assert.That(view.HighlightedTurnSlot, Is.SameAs(played));
            Assert.That(reported, Is.SameAs(played));
            var preview = Descendants(view.Root)
                .Single(element => element.name == "gd-turn-slot-preview");
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                Descendants(preview).Prepend(preview)
                    .All(element => element.pickingMode == PickingMode.Ignore),
                Is.True,
                "Every painted layer of a rail hover face is pointer-transparent.");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == played.PlayedCard.DisplayName.ToUpperInvariant()),
                Is.True,
                "The revealed face is the card's own, drawn by the builder the tray draws with.");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == played.PlayedCard.Face.PrintedText),
                Is.True,
                "including its verified printed words.");

            view.ForwardTurnSlotHighlight(empty);
            Assert.That(view.HighlightedTurnSlot, Is.Null,
                "An empty slot reveals nothing rather than an empty card.");
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Descendants(preview).Count(), Is.EqualTo(1), "and the face is taken down.");
        }

        [Test]
        public void ARevealedTurnSlotCard_IsDroppedWhenTheRailIsRedrawn()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            presenter.Refresh();

            view.ForwardTurnSlotHighlight(
                view.Model.Heroes[0].TurnSlots.Single(slot => slot.IsOccupied));
            Assert.That(view.HighlightedTurnSlot, Is.Not.Null);

            ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            presenter.Refresh();
            Assert.That(view.HighlightedTurnSlot, Is.Null,
                "A rebuilt rail keeps no face for a slot element it no longer draws.");
            Assert.That(
                Descendants(view.Root).Single(element => element.name == "gd-turn-slot-preview")
                    .style.display.value,
                Is.EqualTo(DisplayStyle.None));
            Assert.That(view.Model.Heroes[0].TurnSlots.Count(slot => slot.IsOccupied), Is.EqualTo(2),
                "and the round's second turn is now a slot of its own.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.7a: the current unresolved card, and the four slots as resolved turn history
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AFacedownCommitment_IsANeutralBackThatNamesNothingAboutTheCard()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithAFacedownCommitment(out var heroId);
            var view = new HeroOverviewsUnityView();
            var artworkLoads = 0;
            using (ConfiguredCardArtwork.UseTextureLoader(_ =>
                   {
                       artworkLoads++;
                       return null;
                   }))
            {
                new HeroOverviewsPresenter(session, view).Refresh();
            }

            var committed = view.Model.Heroes.Single(hero => hero.HeroId.Equals(heroId));
            var waiting = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(heroId));
            Assert.That(committed.CurrentCard, Is.Not.Null);
            Assert.That(committed.CurrentCard.IsFacedown, Is.True);
            Assert.That(committed.CurrentCard.StateLabel, Is.EqualTo("FACEDOWN"));
            Assert.That(committed.CurrentCard.Card, Is.Null,
                "A facedown marker carries no card at all - the authority does not name the instance " +
                "until joint reveal, and this shape means presentation could not leak it if it did.");
            Assert.That(committed.CurrentCard.Color, Is.EqualTo(CardColor.None));
            Assert.That(committed.CurrentCard.InitiativeLabel, Is.Empty);
            Assert.That(committed.TurnSlots.Any(slot => slot.IsOccupied), Is.False,
                "and a commitment is not a resolved turn.");
            Assert.That(waiting.CurrentCard, Is.Null, "The hero who has not responded has nothing yet.");
            Assert.That(artworkLoads, Is.Zero,
                "A facedown projection carries no definition identity, so presentation never asks " +
                "the artwork catalog what front it might be.");

            // The committed card really is a card this presenter could have found by location, and its
            // name really is in the director's definition list, so the absence above is a decision.
            var hidden = session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.TurnArea);
            var hiddenName = session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(hidden.DefinitionReference)).DisplayName;
            Assert.That(hiddenName, Is.Not.Empty);
            Assert.That(committed.CurrentCard.AssistiveLabel, Does.Not.Contain(hiddenName));
            Assert.That(
                Descendants(view.Root).OfType<Label>().Any(label => label.text.Contains(hiddenName)),
                Is.False,
                "and no label anywhere on the rail spells the facedown card's name.");
            var marker = Descendants(view.Root).Single(element => element.name == "gd-current-card");
            Assert.That(marker.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.CardBack));
            foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
            {
                Assert.That(marker.style.backgroundColor.value,
                    Is.Not.EqualTo(MatchHudTheme.FaceFlag(color)),
                    $"and the back is not {color}'s own plate, so it names no printed colour either.");
            }

            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-current-card-slot"),
                Is.EqualTo(view.Model.Heroes.Count),
                "Both rails reserve the marker's footprint, so the header does not move when a " +
                "commitment arrives or resolves.");
        }

        [Test]
        public void ARevealedUnresolvedCard_SitsInTheMarkerRatherThanInTheNumberedHistory()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn();
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            foreach (var hero in view.Model.Heroes)
            {
                var revealed = session.Projection.GetCommitment(hero.HeroId).RevealedCardInstanceId.Value;
                var instance = session.Projection.Cards.Single(card => card.Id.Equals(revealed));
                Assert.That(instance.State.ResolutionStatus,
                    Is.EqualTo(CardResolutionStatus.Unresolved),
                    "The fixture only proves the split while the card really has not resolved.");
                Assert.That(
                    CurrentRoundCards.PlayedTurnIndex(session.Projection, hero.HeroId, revealed),
                    Is.EqualTo(1),
                    "and the round's own assignment history already names this card's turn, which is " +
                    "exactly why occupying a slot from the history alone was wrong.");

                Assert.That(hero.CurrentCard, Is.Not.Null);
                Assert.That(hero.CurrentCard.IsFacedown, Is.False);
                Assert.That(hero.CurrentCard.StateLabel, Is.EqualTo("RESOLVING"));
                Assert.That(hero.CurrentCard.Card.CardInstanceId, Is.EqualTo(revealed));
                Assert.That(hero.CurrentCard.Card.HistoryLabel, Is.EqualTo("RESOLVING"));
                Assert.That(hero.CurrentCard.Color, Is.EqualTo(hero.CurrentCard.Card.Color));
                Assert.That(hero.CurrentCard.InitiativeLabel,
                    Is.EqualTo(hero.CurrentCard.Card.Face.Initiative.AmountLabel),
                    "p.7's Initiative is on the marker, from the card's own face.");
                Assert.That(hero.CurrentCard.AssistiveLabel,
                    Does.Contain(hero.CurrentCard.Card.ColorName),
                    "and the colour is named, so the marker means nothing by colour alone.");
                Assert.That(hero.TurnSlots.Any(slot => slot.IsOccupied), Is.False,
                    "No turn is history yet: the card is being played, not played.");
            }

            var initiatives = Descendants(view.Root)
                .Where(element => element.name == "gd-current-card")
                .Select(marker => Descendants(marker).OfType<Label>().First().text)
                .ToArray();
            Assert.That(initiatives, Is.EqualTo(view.Model.Heroes
                .Select(hero => hero.CurrentCard.Card.Face.Initiative.AmountLabel).ToArray()),
                "The marker draws the card's colour and its Initiative, and nothing else.");
            Assert.That(
                Descendants(view.Root).Any(element => element.name == "gd-current-card-state"),
                Is.False,
                "By the user's 2026-08-21 decision the revealed marker carries no `RESOLVING` caption: " +
                "a card's own colour and Initiative in the header say it is the card in play.");
            Assert.That(
                Descendants(view.Root).Where(element => element.name == "gd-current-card")
                    .All(marker => marker.tooltip.Contains("RESOLVING")),
                Is.True,
                "The word stays in the marker's hover and assistive reading, which is where milestone " +
                "8.5a keeps a card's printed colour for the same reason.");
        }

        [Test]
        public void APausedResolution_KeepsTheCardInTheMarkerUntilTheActionCompletes()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "The fixture only proves this while the match really is paused inside an Attack.");
            foreach (var hero in view.Model.Heroes)
            {
                Assert.That(hero.CurrentCard, Is.Not.Null,
                    "A turn paused mid-action has not resolved either card.");
                Assert.That(hero.CurrentCard.StateLabel, Is.EqualTo("RESOLVING"));
                Assert.That(hero.TurnSlots.Any(slot => slot.IsOccupied), Is.False);
            }
        }

        [Test]
        public void AResolvedCard_TakesItsNumberedSlotAndClearsTheMarkerOnTheSameRefresh()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();
            var acting = session.Projection.SelectedActor.ActorId;
            var actingCardId = session.Projection.GetCommitment(acting).RevealedCardInstanceId.Value;
            Assert.That(view.Model.Heroes.All(hero => hero.CurrentCard != null), Is.True);

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(actingCardId));
            presenter.Refresh();

            var resolved = view.Model.Heroes.Single(hero => hero.HeroId.Equals(acting));
            var stillPlaying = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(acting));
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(actingCardId))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(resolved.CurrentCard, Is.Null, "The marker clears when that card resolves.");
            var slot = resolved.TurnSlots.Single(value => value.IsOccupied);
            Assert.That(slot.TurnIndex, Is.EqualTo(1));
            Assert.That(slot.PlayedCard.CardInstanceId, Is.EqualTo(actingCardId),
                "and the same instance is now that turn's history.");
            Assert.That(slot.PlayedCard.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
            Assert.That(slot.IsActiveEffectSource, Is.False, "This card started no Active effect.");
            Assert.That(slot.ActiveLabel, Is.Empty);

            // Milestone 8.7a's cross-surface rule: one turn resolves hero by hero in Initiative order,
            // so this is a real position with one card resolved and one still in play.
            Assert.That(stillPlaying.CurrentCard, Is.Not.Null);
            Assert.That(stillPlaying.TurnSlots.Any(value => value.IsOccupied), Is.False,
                "and nothing from the resolved hero's rail leaked into the other one.");

            var handsView = new DirectorHandsUnityView();
            var hands = new DirectorHandsPresenter(session, handsView);
            hands.SelectViewedHero(acting);
            Assert.That(handsView.Model.ViewedHand.TrayCards.Any(card => card.IsResolving), Is.False,
                "The tray agrees: a resolved card is no longer the card in play.");
            hands.SelectViewedHero(stillPlaying.HeroId);
            Assert.That(
                handsView.Model.ViewedHand.TrayCards.Single(card => card.IsResolving).CardInstanceId,
                Is.EqualTo(stillPlaying.CurrentCard.Card.CardInstanceId),
                "and the hero who has not acted still has one.");
        }

        [Test]
        public void AResolvedSlotWhoseCardIsStillActive_WearsTheSameOutlineAndWordTheTrayGivesIt()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out var sourceCardId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();
            var ownerId = session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId)).OwnerId;

            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds,
                Does.Contain(sourceCardId),
                "The fixture only proves the indicator while the effect really is live.");
            var owner = view.Model.Heroes.Single(hero => hero.HeroId.Equals(ownerId));
            var slot = owner.TurnSlots.Single(value => value.IsOccupied);
            Assert.That(slot.PlayedCard.CardInstanceId, Is.EqualTo(sourceCardId),
                "The Skill completed, so its card is resolved history and its slot is occupied.");
            Assert.That(slot.IsActiveEffectSource, Is.True);
            Assert.That(slot.ActiveLabel, Is.EqualTo("ACTIVE"));
            Assert.That(slot.AssistiveLabel, Does.Contain("ACTIVE EFFECT IN EFFECT"));
            Assert.That(owner.CurrentCard, Is.Null, "and it is not also in the marker.");

            var outlines = Descendants(view.Root)
                .Where(element => element.name == "gd-turn-slot-outline")
                .ToArray();
            Assert.That(outlines,
                Has.Length.EqualTo(view.Model.Heroes.Count * RoundResolutionAuthority.TurnsPerRound),
                "Every slot sits in an outline, so a slot does not resize when an effect starts.");
            Assert.That(
                outlines.Count(outline =>
                    outline.style.borderTopColor.value == MatchHudTheme.ActiveEffectAccent),
                Is.EqualTo(1),
                "One outline, in milestone 8.5b's own accent - the same mark the tray gives the card.");
            Assert.That(
                Descendants(view.Root).OfType<Label>()
                    .Count(label => label.name == "gd-turn-slot-active" && label.text == "ACTIVE"),
                Is.EqualTo(1),
                "and the word is drawn, so the state is never carried by colour alone.");

            // The other surface, from the same projected fact, on the same position.
            var handsView = new DirectorHandsUnityView();
            var hands = new DirectorHandsPresenter(session, handsView);
            hands.SelectViewedHero(ownerId);
            Assert.That(
                handsView.Model.ViewedHand.TrayCards
                    .Single(card => card.IsActiveEffectSource).CardInstanceId,
                Is.EqualTo(sourceCardId));

            // and both let go on the same refresh when the lifecycle ends it.
            ConfiguredMatchFixtures.PlayOutTheRestOfTheRound(session);
            presenter.Refresh();
            hands.SelectViewedHero(ownerId);
            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds, Is.Empty);
            Assert.That(
                view.Model.Heroes.SelectMany(hero => hero.TurnSlots)
                    .Any(value => value.IsActiveEffectSource),
                Is.False);
            Assert.That(
                Descendants(view.Root).OfType<Label>()
                    .Count(label => label.name == "gd-turn-slot-active" && label.text == "ACTIVE"),
                Is.EqualTo(0));
            Assert.That(
                handsView.Model.ViewedHand.TrayCards.Any(card => card.IsActiveEffectSource), Is.False,
                "on both surfaces, from the one projected list.");
        }

        [Test]
        public void APassedTurn_LeavesNoMarkerAndNoOccupiedSlot()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithAPassingHero(out var passingHeroId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            ConfiguredMatchFixtures.CommitBothHands(session);
            presenter.Refresh();

            Assert.That(session.Projection.GetCommitment(passingHeroId).Status,
                Is.EqualTo(TurnCardCommitmentStatus.Passed),
                "The fixture only proves this while p.7's Pass really was the response.");
            var passed = view.Model.Heroes.Single(hero => hero.HeroId.Equals(passingHeroId));
            Assert.That(passed.CurrentCard, Is.Null, "A hero who passed has no card in play.");
            Assert.That(passed.TurnSlots[1].IsOccupied, Is.False,
                "and the turn they passed on stays an empty slot rather than borrowing one.");
            Assert.That(passed.TurnSlots[1].AssistiveLabel, Does.Contain("empty"));
            Assert.That(passed.TurnSlots[0].IsOccupied, Is.True,
                "while the turn they did play is still their resolved history.");

            var opponent = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(passingHeroId));
            Assert.That(opponent.CurrentCard, Is.Not.Null,
                "and the hero who did commit is unaffected by the other one's Pass.");
        }

        [Test]
        public void RestingOnARevealedMarker_ShowsTheSameCompleteFaceTheHistorySlotsShow()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();

            var hero = view.Model.Heroes[0];
            var current = hero.CurrentCard;
            HeroCurrentCardViewModel reported = null;
            view.CurrentCardHighlighted += marker => reported = marker;
            Assert.That(view.HighlightedCurrentCard, Is.Null, "Nothing is revealed until the pointer rests.");

            view.ForwardCurrentCardHighlight(current);
            Assert.That(view.HighlightedCurrentCard, Is.SameAs(current));
            Assert.That(reported, Is.SameAs(current));
            var preview = Descendants(view.Root)
                .Single(element => element.name == "gd-turn-slot-preview");
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == current.Card.DisplayName.ToUpperInvariant()),
                Is.True,
                "The revealed face is the card's own, drawn by the builder the tray draws with.");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == current.Card.Face.PrintedText),
                Is.True,
                "including its verified printed words,");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label => label.text == "RESOLVING"),
                Is.True,
                "and the face says the card is being played rather than naming a turn already history.");

            var acceptedBefore = session.Projection.Version;
            view.ForwardCurrentCardHighlight(null);
            Assert.That(view.HighlightedCurrentCard, Is.Null);
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(session.Projection.Version, Is.EqualTo(acceptedBefore),
                "A hover submits nothing.");

            // A facedown marker reveals nothing, and neither does an empty one.
            var facedownSession =
                ConfiguredMatchFixtures.CreateSessionWithAFacedownCommitment(out var committedHeroId);
            var facedownView = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(facedownSession, facedownView).Refresh();
            var facedown = facedownView.Model.Heroes
                .Single(value => value.HeroId.Equals(committedHeroId)).CurrentCard;
            facedownView.ForwardCurrentCardHighlight(facedown);
            Assert.That(facedownView.HighlightedCurrentCard, Is.Null);
            Assert.That(
                Descendants(facedownView.Root)
                    .Single(element => element.name == "gd-turn-slot-preview").style.display.value,
                Is.EqualTo(DisplayStyle.None));

            // and a redraw drops a stale face, exactly as it does for a turn slot.
            view.ForwardCurrentCardHighlight(current);
            Assert.That(view.HighlightedCurrentCard, Is.Not.Null);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            presenter.Refresh();
            Assert.That(view.HighlightedCurrentCard, Is.Null,
                "A rebuilt rail keeps no face for a marker it no longer draws.");
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void OneRevealAtATime_IsShownWhicheverRailTheThePointerMovesBetween()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            presenter.Refresh();

            var resolved = view.Model.Heroes.Single(hero => hero.CurrentCard == null);
            var playing = view.Model.Heroes.Single(hero => hero.CurrentCard != null);
            var slot = resolved.TurnSlots.Single(value => value.IsOccupied);

            view.ForwardTurnSlotHighlight(slot);
            Assert.That(view.HighlightedTurnSlot, Is.SameAs(slot));
            Assert.That(view.HighlightedCurrentCard, Is.Null);

            view.ForwardCurrentCardHighlight(playing.CurrentCard);
            Assert.That(view.HighlightedCurrentCard, Is.SameAs(playing.CurrentCard));
            Assert.That(view.HighlightedTurnSlot, Is.Null,
                "Moving to the other rail's marker takes the first face down - one preview, one card.");
            Assert.That(
                Descendants(view.Root)
                    .Count(element => element.name == "gd-card-preview"),
                Is.EqualTo(1));
        }

        [Test]
        public void TheCardsTitle_SitsBesideTheInitiativeBannerAndStaysCentredOnTheCard()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var view = new DirectorHandsUnityView();
            new DirectorHandsPresenter(session, view).Refresh();

            var bands = Descendants(view.Root)
                .Where(element => element.name == "gd-card-name-band")
                .ToArray();
            Assert.That(bands, Has.Length.EqualTo(view.Model.ViewedHand.TrayCards.Count));
            foreach (var band in bands)
            {
                var flag = Descendants(band).Single(element => element.name == "gd-card-initiative");
                var title = Descendants(band).OfType<Label>()
                    .Single(element => element.name == "gd-card-name");
                Assert.That(flag.style.position.value, Is.EqualTo(Position.Absolute),
                    "The banner is out of the flow, so the title can centre on the card itself.");
                Assert.That(title.style.paddingLeft.value.value,
                    Is.EqualTo(title.style.paddingRight.value.value),
                    "The reservation is symmetric, so the title's centre is the card's centre rather " +
                    "than the centre of what is left beside the banner.");
                Assert.That(
                    title.style.paddingLeft.value.value + band.style.paddingLeft.value.value,
                    Is.GreaterThanOrEqualTo(flag.style.width.value.value),
                    "and it is wide enough that a wrapped title cannot reach the banner.");
                Assert.That(band.style.minHeight.value.value,
                    Is.LessThan(flag.style.height.value.value + 20f),
                    "The band is the header strip the banner hangs in, not a line opened below it.");
                Assert.That(title.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal),
                    "A long title wraps rather than being cut off.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.5b: the card whose Active effect is live, and the card being resolved
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACardWhoseActiveEffectIsLive_IsOutlinedAndLabelledAndOnlyThatCard()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out var sourceCardId);
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.SelectViewedHero(session.Projection.Cards
                .Single(card => card.Id.Equals(sourceCardId)).OwnerId);

            var hand = view.Model.ViewedHand;
            var active = hand.TrayCards.Where(card => card.IsActiveEffectSource).ToArray();
            Assert.That(active.Select(card => card.CardInstanceId), Is.EqualTo(new[] { sourceCardId }),
                "Exactly the card the authority named, and no card that merely could create an effect.");
            Assert.That(active[0].ActiveLabel, Is.EqualTo("ACTIVE"));
            Assert.That(active[0].AssistiveLabel, Does.Contain("ACTIVE EFFECT IN EFFECT"));
            foreach (var card in hand.TrayCards.Where(card => !card.IsActiveEffectSource))
            {
                Assert.That(card.ActiveLabel, Is.Empty);
                Assert.That(card.AssistiveLabel, Does.Not.Contain("ACTIVE EFFECT"));
            }

            var slots = Descendants(view.Root)
                .Where(element => element.name == "gd-card-slot")
                .ToArray();
            Assert.That(slots, Has.Length.EqualTo(hand.TrayCards.Count),
                "Every card sits in a slot, so a card does not change size when an effect starts.");
            Assert.That(
                slots.Count(slot =>
                    slot.style.borderTopColor.value == MatchHudTheme.ActiveEffectAccent),
                Is.EqualTo(1),
                "One outline, outside the card's own border - which is the selection treatment - so " +
                "a card can be selected and active at once without either meaning being lost.");
            Assert.That(
                slots.Select(slot => slot.style.borderTopWidth.value).Distinct(),
                Is.EqualTo(new[] { DirectorHandsUnityView.ActiveOutlineWidth }),
                "and the outline is always drawn, so only its colour changes.");
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-card-active"),
                Is.EqualTo(1),
                "The word ACTIVE is drawn beside the lifecycle label, so the state is never colour alone.");
            Assert.That(
                MatchHudTheme.ActiveEffectAccent, Is.Not.EqualTo(MatchHudTheme.SelectedOutline));
            Assert.That(
                MatchHudTheme.ActiveEffectAccent, Is.Not.EqualTo(MatchHudTheme.ConfirmAccent));
            foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
            {
                Assert.That(MatchHudTheme.ActiveEffectAccent, Is.Not.EqualTo(MatchHudTheme.Face(color)),
                    $"and it is not {color}'s own face, so it cannot be read as a card colour.");
            }
        }

        [Test]
        public void WhenTheEffectEnds_TheIndicatorGoesWithItOnTheSameRefresh()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out var sourceCardId);
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            var ownerId = session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId)).OwnerId;
            presenter.SelectViewedHero(ownerId);
            Assert.That(view.Model.ViewedHand.TrayCards.Any(card => card.IsActiveEffectSource), Is.True);

            // Play the round out. The mark is cancelled when its source card's state changes and expires
            // at the turn it was made on, so by the round's end nothing it created is live any more.
            ConfiguredMatchFixtures.PlayOutTheRestOfTheRound(session);
            presenter.SelectViewedHero(ownerId);

            Assert.That(RestoredMatch(session).Round.Timing.Effects
                .Any(effect => effect.Phase == ActiveEffectPhase.Active), Is.False,
                "The fixture only proves the removal while the lifecycle really has ended it.");
            Assert.That(session.Projection.ActiveEffectSourceCardInstanceIds, Is.Empty);
            Assert.That(view.Model.ViewedHand.TrayCards.Any(card => card.IsActiveEffectSource), Is.False,
                "so no card is outlined, on the same refresh that reported the ending.");
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-card-active"), Is.EqualTo(0));
        }

        [Test]
        public void TheCardBeingResolved_IsDrawnAtFullStrengthWhileItsTurnRuns()
        {
            // Milestone 8.7a moved this off the live-Active-effect fixture. That fixture's Skill has
            // already completed, so its card is resolved and is no longer the card in play; the position
            // this rule is about is the one right after joint reveal, where a revealed card has not run.
            var session = ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            var ownerId = session.Projection.Setup.Heroes.First().HeroId;
            var sourceCardId = session.Projection.GetCommitment(ownerId).RevealedCardInstanceId.Value;
            presenter.SelectViewedHero(ownerId);

            var hand = view.Model.ViewedHand;
            var resolving = hand.TrayCards.Single(card => card.IsResolving);
            Assert.That(resolving.CardInstanceId, Is.EqualTo(sourceCardId),
                "The card in play is the one this hero revealed for the current turn.");
            Assert.That(resolving.HistoryLabel, Is.EqualTo("RESOLVING"),
                "and it says it is being played rather than claiming to be a turn already history.");
            Assert.That(resolving.IsSelectable, Is.False,
                "It carries no commitment command - the round is past choosing.");

            var buttons = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .OfType<Button>()
                .ToArray();
            var index = hand.TrayCards.Select((card, position) => (card, position))
                .Single(entry => entry.card.IsResolving).position;
            Assert.That(buttons[index].enabledSelf, Is.True,
                "the inert outer face stays enabled so its printed action descendants can receive input.");
            Assert.That(buttons[index].focusable, Is.False,
                "The outer face itself remains no duplicate command or focus stop.");
            Assert.That(buttons[index].style.opacity.value, Is.EqualTo(1f),
                "but it is the card being played, so it is not dimmed as though it were spent.");
            foreach (var other in buttons.Where((_, position) => position != index))
            {
                Assert.That(other.style.opacity.value, Is.LessThan(1f),
                    "while a card that carries no command and is not in play still reads as unavailable.");
            }

            view.SetCompactPeekMode(true);
            var overlappedButtons = Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice")
                .OfType<Button>()
                .ToArray();
            var overlappedOrder = overlappedButtons
                .Select(button => button.tooltip.Split(
                    new[] { " - " }, StringSplitOptions.None)[0])
                .ToArray();
            var expectedOrder = hand.TrayCards
                .Where(card => !card.IsResolving)
                .Select(card => card.DisplayName)
                .Concat(new[] { resolving.DisplayName })
                .ToArray();
            Assert.That(overlappedOrder, Is.EqualTo(expectedOrder),
                "The configured overlapped hand moves only the resolving card to its fully visible " +
                "rightmost position and preserves every sibling's relative order.");
            Assert.That(overlappedButtons.Last().style.opacity.value, Is.EqualTo(1f));
            Assert.That(Descendants(overlappedButtons.Last())
                    .Any(element => element.name == "gd-card-unavailable-veil"),
                Is.False,
                "The resolving card remains undimmed at the visible end of the hand.");
            foreach (var spent in overlappedButtons.Take(overlappedButtons.Length - 1))
            {
                Assert.That(spent.style.opacity.value, Is.EqualTo(1f),
                    "Overlapped spent cards keep an opaque surface, so siblings cannot bleed through.");
                var veil = Descendants(spent).Single(element =>
                    element.name == "gd-card-unavailable-veil");
                Assert.That(veil.style.backgroundColor.value.a, Is.GreaterThan(0f),
                    "A card-local veil still makes the unavailable state visibly dim.");
                Assert.That(veil.pickingMode, Is.EqualTo(PickingMode.Ignore));
            }
        }

        // -------------------------------------------------------------------------------------------
        // The viewed hero: the map is the only control, and the tray follows what the match is waiting on
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheViewedHero_FollowsTheAwaitedHeroUntilAMapSelectionPinsAnother()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();

            var awaited = presenter.AwaitedHeroId;
            Assert.That(presenter.ViewedHeroId, Is.EqualTo(awaited));
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(awaited));

            var other = session.Projection.Setup.Heroes
                .First(hero => !hero.HeroId.Equals(awaited)).HeroId;
            var before = session.Projection;
            Assert.That(presenter.SelectViewedHero(other), Is.True);
            Assert.That(presenter.ViewedHeroId, Is.EqualTo(other));
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(other));
            Assert.That(session.Projection, Is.SameAs(before),
                "Selecting a hero submits no command and changes no authoritative state.");
            Assert.That(session.Projection.AcceptedCommands, Is.Empty);
        }

        [Test]
        public void APinnedHero_IsReleasedOnceTheMatchIsWaitingOnSomebodyElse()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(session, view);
            presenter.Refresh();
            var awaited = presenter.AwaitedHeroId;
            var other = session.Projection.Setup.Heroes
                .First(hero => !hero.HeroId.Equals(awaited)).HeroId;
            presenter.SelectViewedHero(other);

            var card = session.Projection.Cards.First(value =>
                value.OwnerId.Equals(awaited) && value.State.Location == CardLocation.Hand);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(awaited, card.Id));
            presenter.Refresh();

            Assert.That(presenter.AwaitedHeroId, Is.Not.EqualTo(awaited),
                "The hero that owed a commitment has given one, so the match waits on the other.");
            Assert.That(presenter.ViewedHeroId, Is.EqualTo(presenter.AwaitedHeroId),
                "and the tray follows, rather than staying on a hand nothing can be played from.");
        }

        [Test]
        public void AHeroThisMatchDoesNotHave_ChangesNothing()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var presenter = new DirectorHandsPresenter(session, new DirectorHandsUnityView());
            presenter.Refresh();
            var viewed = presenter.ViewedHeroId;

            Assert.That(presenter.SelectViewedHero(new HeroId("not-in-this-match")), Is.False);
            Assert.That(presenter.SelectViewedHero(default), Is.False);
            Assert.That(presenter.ViewedHeroId, Is.EqualTo(viewed));
        }

        // -------------------------------------------------------------------------------------------
        // The hero overviews
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EachOverview_IsARepeatedComponentCarryingIdentityLevelSlotsAndDiscard()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();

            Assert.That(view.Model.Heroes, Has.Count.EqualTo(session.Projection.Setup.Heroes.Count));
            Assert.That(view.Model.Heroes.Select(hero => hero.TeamSide),
                Is.EquivalentTo(new[] { TeamSide.Red, TeamSide.Blue }),
                "One overview on each edge for the configured one-hero-per-team scene.");

            foreach (var hero in view.Model.Heroes)
            {
                var setup = session.Projection.Setup.Heroes.Single(value =>
                    value.HeroId.Equals(hero.HeroId));
                Assert.That(hero.HeroDisplayName, Is.EqualTo(setup.DisplayName));
                Assert.That(hero.Level, Is.EqualTo(1), "p.15: every hero starts at level 1.");
                Assert.That(hero.TurnSlots, Has.Count.EqualTo(RoundResolutionAuthority.TurnsPerRound));
                Assert.That(hero.TurnSlots.Select(slot => slot.TurnIndex), Is.EqualTo(new[] { 1, 2, 3, 4 }));
                Assert.That(hero.TurnSlots.All(slot => !slot.IsOccupied), Is.True);
                Assert.That(hero.ItemBonuses, Is.Empty, "Nobody has levelled, so nobody has an item.");
                Assert.That(hero.DiscardCount, Is.EqualTo(0));
                Assert.That(hero.DiscardLabel, Is.EqualTo("DISCARD"));
            }
        }

        [Test]
        public void AnOccupiedSlot_TakesThePlayedCardsColourAndSaysSoOnlyInItsAssistiveLabel()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            PlayTurns(session, 2);
            presenter.Refresh();

            foreach (var hero in view.Model.Heroes)
            {
                var played = CurrentRoundCards.TurnSlots(session.Projection, hero.HeroId);
                Assert.That(hero.TurnSlots.Select(slot => slot.Color), Is.EqualTo(
                    played.Select(slot => slot.Color)));
                Assert.That(hero.TurnSlots.Take(2).All(slot => slot.IsOccupied), Is.True);
                Assert.That(hero.TurnSlots.Skip(2).All(slot => !slot.IsOccupied), Is.True);
                Assert.That(hero.TurnSlots[0].AssistiveLabel, Does.Contain("Turn 1"),
                    "Milestone 8.11b: the ordinal is in the assistive reading and nowhere on the icon.");
                Assert.That(hero.TurnSlots[0].AssistiveLabel,
                    Does.Contain(MatchHudTheme.ColorName(hero.TurnSlots[0].Color)),
                    "The colour is readable, and only from the assistive label.");
                Assert.That(hero.TurnSlots[3].AssistiveLabel, Does.Contain("empty"));
            }
        }

        [Test]
        public void ItemBonuses_AreProjectedCountsPerIconAndAreDrawnAsSignedValuesBesideTheirSymbol()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out var redHeroId);
            var pending = session.Projection.PendingSystemTransitionStep;
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.AnswerSystemTransitionStep(pending.Invocations[0]));
            ConfiguredMatchFixtures.FinishEndOfRound(session);

            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();
            var red = view.Model.Heroes.Single(hero => hero.HeroId.Equals(redHeroId));
            var projected = session.Projection.HeroProgression
                .Single(entry => entry.HeroId.Equals(redHeroId));

            Assert.That(projected.Level, Is.EqualTo(2), "p.15's purchase raised the Level by one.");
            Assert.That(red.Level, Is.EqualTo(projected.Level));
            Assert.That(red.ItemBonuses, Has.Count.EqualTo(projected.ItemBonuses.Count));
            Assert.That(red.ItemBonuses, Is.Not.Empty,
                "p.16 turned the set-aside card into an item, which the strip now shows.");
            foreach (var bonus in red.ItemBonuses)
            {
                Assert.That(bonus.AmountLabel, Is.EqualTo("+" + bonus.Amount),
                    "p.16 states a bonus as +1, never as 1.");
                Assert.That(bonus.Token, Is.EqualTo(MatchHudTheme.ChannelToken(bonus.Channel)));
                Assert.That(bonus.AssistiveLabel,
                    Does.Contain(MatchHudTheme.ChannelName(bonus.Channel)),
                    "The full channel name stays available even though the chip is compact.");
                Assert.That(
                    projected.ItemBonuses.Single(entry => entry.Key == bonus.Channel).Value,
                    Is.EqualTo(bonus.Amount),
                    "The count is the projection's, not a recount of items here.");
            }

            var blue = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(redHeroId));
            Assert.That(blue.ItemBonuses, Is.Empty, "The hero that did not level gained no item.");
        }

        // -------------------------------------------------------------------------------------------
        // The rail's coin balance (milestone 8.21a)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.23a redrew the balance as the coin it counts, so what the readout carries is the
        /// amount and nothing else - the word `COINS` explained a number that no longer needs
        /// explaining, on the same ground milestone 8.11b took the count off the discard row and the
        /// ordinals off the turn slots.
        /// </summary>
        [Test]
        public void EveryRail_ShowsItsOwnHerosProjectedCoinBalanceOnAStruckCoin()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            Assert.That(view.Model.Heroes, Is.Not.Empty);
            AssertEveryRailReadsTheProjectedBalance(session, view, "the opening position");
            Assert.That(view.Model.Heroes.All(hero => hero.Coins == 0), Is.True,
                "Nobody has defeated anything, so every hero starts the match with an empty purse - " +
                "and the rail says so with a nought rather than by drawing nothing.");

            foreach (var readout in Descendants(view.Root)
                .Where(element => element.name == "gd-hero-coins"))
            {
                var parts = Descendants(readout).OfType<Label>().Select(label => label.text).ToArray();
                Assert.That(parts, Is.EqualTo(new[] { "0" }),
                    "The readout is the amount and nothing else; the coin says what the amount is of.");
                var disc = Descendants(readout)
                    .Single(element => element.name == "gd-hero-coin-disc");
                Assert.That(disc.style.width.value.value,
                    Is.EqualTo(disc.style.height.value.value).Within(0.01f),
                    "A coin is round, so the disc's two axes are one number.");
                Assert.That(readout.tooltip, Is.EqualTo("0 coins"),
                    "and the word is where this HUD puts every name it draws as a shape: one " +
                    "pointer rest away, and spoken in full to a reader with no pointer.");
            }
        }

        /// <summary>
        /// The reading is the ledger's, at every position: a Defeat reward, p.15's purchase and p.8's
        /// Pity Coin all reach the rail because the rail asks the projection rather than counting coin
        /// events it watched.
        /// </summary>
        [Test]
        public void TheCoinCount_FollowsEveryAuthoritativeGrantAndSpendWithoutAccountingForItHere()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithACoinEarningRound(out var redHeroId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();

            var earned = view.Model.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coins;
            Assert.That(earned, Is.GreaterThan(0),
                "p.13's Defeat reward is the source this fixture earns from, and a zero here would " +
                "make every later comparison vacuous.");
            AssertEveryRailReadsTheProjectedBalance(session, view, "the round that earned the coins");

            // p.15 charges for the level it forces, and p.8 pays the Pity Coin to the hero that
            // bought none - a spend and a grant in the same end of round, on two different rails.
            ConfiguredMatchFixtures.AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AdvanceEndOfRound());
            var pending = session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null, "p.15 stops the round on the upgrade the level forces.");
            presenter.Refresh();
            AssertEveryRailReadsTheProjectedBalance(session, view, "the standing Upgrade a Card pause");
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-hero-coins"),
                Is.EqualTo(view.Model.Heroes.Count),
                "The balance is public match information and is drawn whether or not the decision " +
                "that would spend it is standing. A number a player can see only while they are " +
                "being asked to spend it arrives at the one moment it is too late to plan with.");

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.AnswerSystemTransitionStep(pending.Invocations[0]));
            ConfiguredMatchFixtures.FinishEndOfRound(session);
            presenter.Refresh();

            var red = view.Model.Heroes.Single(hero => hero.HeroId.Equals(redHeroId));
            var blue = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(redHeroId));
            Assert.That(red.Coins, Is.LessThan(earned), "The level was bought with the coins earned.");
            Assert.That(blue.Coins, Is.GreaterThan(0),
                "and p.8 paid its Pity Coin to the hero that levelled nothing.");
            AssertEveryRailReadsTheProjectedBalance(session, view, "the finished end of round");
        }

        /// <summary>
        /// The count is rebuilt by each reconstruction path rather than remembered, which is what
        /// reading the projected balance straight through buys: no presentation-owned lifetime, no new
        /// serialized answer, and nothing for a restore to disagree with.
        /// </summary>
        [Test]
        public void TheCoinCount_IsRebuiltByRestoreReplayAndUndoRatherThanRemembered()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithACoinEarningRound(out var redHeroId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();
            var earned = Describe(view);
            var snapshot = session.CaptureSnapshot();
            Assert.That(
                view.Model.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coins,
                Is.GreaterThan(0),
                "The captured position really is one holding p.13's reward, so the descriptions " +
                "compared below are about a balance rather than about two noughts.");

            // The same commands reach the same reading, which is the deterministic half.
            var replayed = ConfiguredMatchFixtures.CreateSessionWithACoinEarningRound(out _);
            var replayedView = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(replayed, replayedView).Refresh();
            Assert.That(Describe(replayedView), Is.EqualTo(earned));

            // Spend it, on the live history - a snapshot restore clears the history Undo walks back
            // through, so the Undo half has to be proven before anything is restored.
            SpendTheEarnedLevel(session);
            presenter.Refresh();
            var spent = Describe(view);
            Assert.That(spent, Is.Not.EqualTo(earned), "The purchase moved the reading.");

            while (session.Projection.IsHistoryAvailable && Describe(view) != earned)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
                presenter.Refresh();
            }

            Assert.That(Describe(view), Is.EqualTo(earned),
                "Undo redraws the balance from the truncated history rather than from anything the " +
                "rail kept.");

            // and the same position again from bytes, which is the other reconstruction path.
            SpendTheEarnedLevel(session);
            presenter.Refresh();
            Assert.That(Describe(view), Is.EqualTo(spent));
            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            presenter.Refresh();
            Assert.That(Describe(view), Is.EqualTo(earned),
                "and a restored position redraws it from current authority.");
        }
        /// <summary>
        /// Milestone 8.23a: the readout stands in the item strip and the rail lost the caption row it
        /// used to share.
        ///
        /// <para>Milestone 8.21a's claim was that the count cost the rail nothing, proved by putting it
        /// on the one line whose content could not grow. This milestone makes the stronger claim the
        /// recomposition is for: the line is gone, so what the count cost is now negative. What the
        /// test still guards is the same property under a new position - the strip does not wrap, the
        /// disc does not stretch it, and the rail's row count is exactly the rows it draws.</para>
        /// </summary>
        [Test]
        public void TheCoinReadout_StandsInTheItemStripAndTheRailNoLongerDrawsACaptionRow()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            var rails = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(view.Model.Heroes.Count));

            Assert.That(
                Descendants(view.Root).Any(element =>
                    element.name == "gd-turn-slots-caption-row" ||
                    element.name == "gd-turn-slots-caption"),
                Is.False,
                "The caption and the row it stood on are gone from every rail, which is the line " +
                "this milestone gives back. What the four slots mean is on their own tooltips.");

            foreach (var rail in rails)
            {
                Assert.That(rail.childCount, Is.EqualTo(4),
                    "This staged position draws the header, the item strip, the four slots and the " +
                    "discard pile. It was five rows before the caption row went, and the two " +
                    "conditional rows - the Ultimate and a held marker - are absent here because " +
                    "nothing is unlocked and nobody is holding anything.");

                var strip = rail.Children().Single(child => child.name == "gd-bonus-strip");
                Assert.That(strip.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap),
                    "A wrapping strip would put the coin on a second line and make the rail taller.");
                Assert.That(strip.childCount, Is.EqualTo(2),
                    "The strip carries the item chips and the coin and nothing else.");

                var coins = strip.Children().Single(child => child.name == "gd-hero-coins");
                var disc = Descendants(coins).Single(child => child.name == "gd-hero-coin-disc");
                Assert.That(disc.style.width.value.value, Is.EqualTo(18f).Within(0.01f),
                    "The coin is a disc, so its two axes are one number.");
                Assert.That(disc.style.height.value.value, Is.EqualTo(18f).Within(0.01f));
                Assert.That(disc.style.flexShrink.value, Is.EqualTo(0f).Within(0.01f),
                    "and a squashed coin at a narrow rail would be an ellipse with a number in it.");

                foreach (var part in Descendants(coins).OfType<Label>())
                {
                    Assert.That(part.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap),
                        "A wrapped balance would grow the disc it is centred in.");
                    Assert.That(part.style.marginTop.value.value, Is.EqualTo(0f).Within(0.01f),
                        "and the runtime theme's own three pixels above and below a label would " +
                        "push the disc past the strip's line, so the balance gives them up.");
                    Assert.That(part.style.marginBottom.value.value, Is.EqualTo(0f).Within(0.01f));
                }
            }
        }

        /// <summary>
        /// Milestone 8.7a's mirror, kept through the move: the item chips read against the screen edge
        /// on either rail and the coin sits inward of them, which is what every other row on the
        /// mirrored rail does.
        /// </summary>
        [Test]
        public void TheCoinReadout_SitsInwardOfTheItemChipsOnBothRailsRatherThanRepeatingOneOrder()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            var columns = view.BlockingRegions.ToArray();
            Assert.That(columns, Has.Length.EqualTo(2), "One column per screen edge.");
            Assert.That(ItemStripOrder(columns[0]),
                Is.EqualTo(new[] { "gd-item-chips", "gd-hero-coins" }),
                "On the left rail the chips keep the screen edge and the coin sits inward.");
            Assert.That(ItemStripOrder(columns[1]),
                Is.EqualTo(new[] { "gd-hero-coins", "gd-item-chips" }),
                "and the mirrored rail is that reflected, which is what every other row on it is.");
        }

        /// <summary>Crosses p.8's stages and buys the level the earned coins force.</summary>
        private static void SpendTheEarnedLevel(MatchSession session)
        {
            ConfiguredMatchFixtures.AdvanceToStage(session, EndOfRoundStage.LevelUpAndUpgrade);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AdvanceEndOfRound());
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0]));
        }

        private static string[] ItemStripOrder(VisualElement column) =>
            Descendants(column)
                .Single(element => element.name == "gd-bonus-strip")
                .Children()
                .Select(child => child.name)
                .ToArray();

        /// <summary>
        /// Every rail's count is the projection's own, in every form it is drawn in. It is a helper
        /// rather than one assertion because "no presentation-owned accounting" is a claim about every
        /// position rather than about one.
        /// </summary>
        private static void AssertEveryRailReadsTheProjectedBalance(
            MatchSession session, HeroOverviewsUnityView view, string where)
        {
            foreach (var hero in view.Model.Heroes)
            {
                var projected = session.Projection.HeroProgression
                    .Single(entry => entry.HeroId.Equals(hero.HeroId));
                Assert.That(hero.Coins, Is.EqualTo(projected.Coins),
                    "The rail counted its own coins at " + where + ".");
                Assert.That(hero.CoinAmountLabel, Is.EqualTo(
                    projected.Coins.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(
                    hero.CoinAssistiveLabel,
                    Is.EqualTo(projected.Coins == 1 ? "1 coin" : hero.CoinAmountLabel + " coins"),
                    "p.13 pays coins, and one of them is a coin.");
            }

            var drawn = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-coins-amount")
                .OfType<Label>()
                .Select(label => label.text)
                .ToArray();
            Assert.That(drawn, Is.EquivalentTo(view.Model.Heroes.Select(hero => hero.CoinAmountLabel)),
                "and the drawn amounts are the model's own at " + where + ".");
        }

        [Test]
        public void TheDiscardSlot_ReportsTheProjectedPileRatherThanCardMovementSeen()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var heroId);
            var view = new HeroOverviewsUnityView();
            new HeroOverviewsPresenter(session, view).Refresh();

            var hero = view.Model.Heroes.Single(value => value.HeroId.Equals(heroId));
            var pile = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .ToArray();
            Assert.That(hero.DiscardCount, Is.EqualTo(pile.Length));
            Assert.That(hero.DiscardCards.Select(card => card.CardInstanceId),
                Is.EqualTo(pile.Select(card => card.Id).OrderBy(id => id.Value, StringComparer.Ordinal)),
                "Milestone 8.11b: one card per card in the projected pile, in the order card instance " +
                "identity settles - which is the order the tray's own discarded group is in.");
            Assert.That(hero.DiscardCards.Select(card => card.Color),
                Is.EqualTo(pile
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .Select(card => CurrentRoundCards.ColorOf(session.Projection, card))));
            Assert.That(hero.DiscardCards.All(card => card.Group == TrayCardGroup.Discarded), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // The Director drawer
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDirectorDrawer_ForwardsUndoResetAndTheEventLogAndOpensWithoutTouchingTheMatch()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var undoRestartView = new UndoRestartUnityView();
            var diagnosticView = new DiagnosticLogUnityView();
            var undoRestart = new UndoRestartPresenter(session, undoRestartView);
            var diagnostics = new DiagnosticLogPresenter(session, diagnosticView);
            undoRestartView.UndoRequested += () => undoRestart.Undo();
            undoRestartView.RestartRequested += () => undoRestart.Restart();
            diagnosticView.ToggleRequested += () => diagnostics.Toggle();
            undoRestart.Refresh();
            diagnostics.Refresh();

            // The drawer holds exactly the three tools the milestone names, each on its own control.
            Assert.That(
                Descendants(undoRestartView.Root).OfType<Button>().Select(button => button.text),
                Is.EqualTo(new[] { "UNDO", "RESET" }));
            Assert.That(((Button)diagnosticView.ToggleRoot).text, Does.StartWith("EVENT LOG"));

            var card = session.Projection.Cards.First(value => value.State.Location == CardLocation.Hand);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            undoRestart.Refresh();
            var committed = session.Projection.AcceptedCommands.Count;
            Assert.That(committed, Is.EqualTo(1));

            undoRestartView.ForwardUndo();
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(committed - 1),
                "`UNDO` forwards the existing Undo operation and nothing else.");

            Assert.That(diagnosticView.Model.IsExpanded, Is.False);
            diagnosticView.ForwardToggle();
            diagnostics.Refresh();
            Assert.That(diagnosticView.Model.IsExpanded, Is.True,
                "`EVENT LOG` toggles the existing collapsible diagnostic log.");

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            Assert.That(session.Projection.AcceptedCommands, Is.Not.Empty);
            undoRestartView.ForwardRestart();
            Assert.That(session.Projection.AcceptedCommands, Is.Empty,
                "`RESET` is presentation terminology for the existing Restart, which empties the history.");
        }

        [Test]
        public void TheDirectorDrawer_OpensAndClosesWithoutChangingAnyMatchState()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var host = new GameObject("hud-drawer-fixture");
            try
            {
                var hud = host.AddComponent<ConfiguredMatchHudRenderer>();
                hud.Configure(
                    new DirectorHandsUnityView(),
                    new RoundActionUnityView(),
                    new UndoRestartUnityView(),
                    new DiagnosticLogUnityView(),
                    new MinionChoiceUnityView(),
                    new HeroOverviewsUnityView());
                var before = session.Projection;

                Assert.That(hud.IsDirectorDrawerOpen, Is.False, "The drawer starts closed.");
                hud.ToggleDirectorDrawer();
                Assert.That(hud.IsDirectorDrawerOpen, Is.True);
                hud.ToggleDirectorDrawer();
                Assert.That(hud.IsDirectorDrawerOpen, Is.False);
                Assert.That(session.Projection, Is.SameAs(before),
                    "Opening or closing the drawer changes no match state.");
                Assert.That(session.Projection.AcceptedCommands, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.11b: the rail draws cards rather than numbers, and a Defense offer is a card face
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryResolvedTurnAndEveryDiscardedCard_IsItsOwnIcon_AndNoNumberIsDrawnForEither()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var defenderId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            FinishTheCurrentTurn(session);
            presenter.Refresh();

            var hero = view.Model.Heroes.Single(value => value.HeroId.Equals(defenderId));
            Assert.That(hero.DiscardCards, Is.Not.Empty,
                "The fixture answered an Attack with a card, which p.11's reaction discards.");
            Assert.That(view.Model.Heroes.Any(value => value.TurnSlots.Any(slot => slot.IsOccupied)),
                Is.True,
                "and the turn it happened in has resolved, so both rows have something in them.");
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-turn-slot"),
                Is.EqualTo(RoundResolutionAuthority.TurnsPerRound * view.Model.Heroes.Count),
                "Every reserved position is drawn, occupied or not, so the four keep their places.");
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-discard-card"),
                Is.EqualTo(view.Model.Heroes.Sum(value => value.DiscardCards.Count)),
                "and the discard pile is one icon per projected card rather than a count.");

            // The point of the milestone, as a fact rather than a screenshot: nothing on either row
            // prints a number any more.
            //
            // The discard row is asserted on its own text first, so it stands apart from the ordinal
            // loop below rather than passing or failing with it whenever a pile happens to hold one to
            // four cards - which is exactly the collision the knockout evidence for this case hit.
            foreach (var row in Descendants(view.Root).Where(element => element.name == "gd-discard"))
            {
                Assert.That(
                    Descendants(row).OfType<Label>().Select(label => label.text),
                    Is.SubsetOf(new[] { "DISCARD", "EMPTY" }),
                    "The discard row prints its caption and nothing else - no count, because the " +
                    "visible icons are the count.");
            }

            var railText = Descendants(view.Root).OfType<Label>().Select(label => label.text).ToArray();
            foreach (var ordinal in new[] { "1", "2", "3", "4" })
            {
                Assert.That(railText, Has.No.Member(ordinal),
                    $"The slot ordinal '{ordinal}' is off the rail: the row's own order is the sequence.");
            }

            // An empty slot is an outline with nothing in it, rather than an invented card.
            var drawnNames = view.Model.Heroes
                .SelectMany(value => value.TurnSlots
                    .Where(slot => slot.IsOccupied)
                    .Select(slot => slot.PlayedCard.DisplayName)
                    .Concat(value.DiscardCards.Select(card => card.DisplayName)))
                .ToArray();
            Assert.That(drawnNames, Is.Not.Empty);
            Assert.That(railText.Intersect(drawnNames), Is.Empty,
                "An icon carries no text at all - the card's name is one pointer rest away.");
            Assert.That(hero.TurnSlots.Where(slot => !slot.IsOccupied)
                .All(slot => slot.PlayedCard == null), Is.True);
            Assert.That(hero.TurnSlots[3].AssistiveLabel, Does.Contain("empty"),
                "and the empty position says so in its own assistive reading.");
            Assert.That(hero.DiscardCards.All(card => card.AssistiveLabel.Contains("DISCARDED")),
                Is.True,
                "while a discard icon's own reading names the exact card and where it is.");
        }

        [Test]
        public void RestingOnADiscardIcon_RevealsThatCardsWholeFaceInTheOneRailPreview()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var defenderId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            FinishTheCurrentTurn(session);
            presenter.Refresh();

            var hero = view.Model.Heroes.Single(value => value.HeroId.Equals(defenderId));
            var first = hero.DiscardCards[0];
            var resolved = view.Model.Heroes
                .SelectMany(value => value.TurnSlots)
                .First(slot => slot.IsOccupied);
            TrayCardViewModel reported = null;
            view.DiscardCardHighlighted += card => reported = card;
            var preview = Descendants(view.Root)
                .Single(element => element.name == "gd-turn-slot-preview");
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.None),
                "Nothing is revealed until the pointer rests.");

            view.ForwardDiscardCardHighlight(first);
            Assert.That(view.HighlightedDiscardCard, Is.SameAs(first));
            Assert.That(reported, Is.SameAs(first));
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                Descendants(preview).Count(element => element.name == "gd-card-preview"),
                Is.EqualTo(1),
                "The face is the tray's own builder, and there is exactly one of it.");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == first.DisplayName.ToUpperInvariant()),
                Is.True);
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label => label.text == "DISCARDED"),
                Is.True,
                "and it names the lifecycle location the icon stands in.");

            // Moving to any other icon replaces the one face. The two kinds of icon share the preview
            // deliberately: two card faces on screen at once is the thing a hover reveal must not do.
            view.ForwardTurnSlotHighlight(resolved);
            Assert.That(view.HighlightedDiscardCard, Is.Null);
            Assert.That(reported, Is.Null, "and the discard highlight is reported as having left.");
            Assert.That(view.HighlightedTurnSlot, Is.SameAs(resolved));
            Assert.That(
                Descendants(preview).Count(element => element.name == "gd-card-preview"),
                Is.EqualTo(1),
                "Moving between icons replaces the face rather than putting two on screen.");
            Assert.That(
                Descendants(preview).OfType<Label>().Any(label =>
                    label.text == resolved.PlayedCard.DisplayName.ToUpperInvariant()),
                Is.True);

            view.ForwardTurnSlotHighlight(null);
            Assert.That(view.HighlightedDiscardCard, Is.Null);
            Assert.That(view.HighlightedTurnSlot, Is.Null);
            Assert.That(preview.style.display.value, Is.EqualTo(DisplayStyle.None),
                "and leaving the rail closes it.");
        }

        [Test]
        public void ARevealedDiscardCard_IsDroppedWhenTheRailIsRedrawn()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var defenderId);
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            presenter.Refresh();

            var hero = view.Model.Heroes.Single(value => value.HeroId.Equals(defenderId));
            Assert.That(hero.DiscardCards, Is.Not.Empty);
            view.ForwardDiscardCardHighlight(hero.DiscardCards[0]);
            Assert.That(view.HighlightedDiscardCard, Is.Not.Null);

            FinishTheCurrentTurn(session);
            presenter.Refresh();
            Assert.That(view.HighlightedDiscardCard, Is.Null,
                "A rebuilt rail keeps no face for an icon element it no longer draws.");
            Assert.That(
                Descendants(view.Root).Single(element => element.name == "gd-turn-slot-preview")
                    .style.display.value,
                Is.EqualTo(DisplayStyle.None));
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-discard-card"),
                Is.EqualTo(view.Model.Heroes.Sum(value => value.DiscardCards.Count)),
                "and the redrawn pile is exactly the projected one, with no icon left over.");
        }

        /// <summary>
        /// Resolves whatever turn is currently being played, without reaching end of round - which
        /// matters for the discard pile, because p.8's retrieval empties it.
        /// </summary>
        private static void FinishTheCurrentTurn(MatchSession session)
        {
            for (var guard = 0; guard < 8 &&
                session.Projection.Phase == RoundResolutionPhase.ResolvingTurn; guard++)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        [Test]
        public void EveryLegalDefenseOffer_IsDrawnAsTheTraysOwnCompleteCardFace_AndDeclineIsNot()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var choice = session.Projection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null);
            var offered = view.Model.DefenseChoices.Where(value => value.Card != null).ToArray();
            Assert.That(offered.Select(value => value.Card.CardInstanceId),
                Is.EqualTo(choice.LegalDefenseOffers.Select(offer => offer.SourceCardInstanceId)),
                "One card face per projected legal offer, in the projected response order.");
            Assert.That(view.Model.DefenseChoices.Last().Card, Is.Null,
                "DECLINE DEFENSE is about no card, so it carries none.");
            Assert.That(view.Model.DefenseChoices.Last().Label, Is.EqualTo("DECLINE DEFENSE"));

            var controls = Descendants(view.Root)
                .Where(element => element.name == "gd-defense-card")
                .ToArray();
            Assert.That(controls, Has.Length.EqualTo(offered.Length));
            Assert.That(controls.All(control => control.focusable), Is.True);
            Assert.That(controls.All(control => control.enabledSelf), Is.True);
            Assert.That(
                controls.All(control =>
                    Descendants(control).Count(element => element.name == "gd-card-preview") == 1),
                Is.True,
                "Each is the shared standalone card face, so no second abbreviated card design exists.");

            // The regions a whole printed face is drawn in, on the offer, exactly as on a tray card.
            var first = controls[0];
            foreach (var band in new[]
                     {
                         "gd-card-name-band", "gd-card-secondary-values", "gd-card-primary-action",
                         "gd-card-rules", "gd-card-history"
                     })
            {
                Assert.That(Descendants(first).Any(element => element.name == band), Is.True,
                    $"The offered face carries the tray card's {band}.");
            }

            var decline = Descendants(view.Root).OfType<Button>()
                .Single(button => button.text == "DECLINE DEFENSE");
            Assert.That(decline.name, Is.Not.EqualTo("gd-defense-card"));
            Assert.That(Descendants(decline).Any(element => element.name == "gd-card-preview"), Is.False,
                "and it stays a distinct non-card control.");
        }

        [Test]
        public void ADefenseCard_ForwardsOnlyItsProjectedInvocationAndKeepsTheAuthorityComputedPreview()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var offer = view.Model.DefenseChoices.First(value => value.Card != null);
            TypedInvocationViewModel submitted = null;
            view.InvocationSubmitted += invocation => submitted = invocation;

            Assert.That(view.ForwardDefenseCard(offer), Is.True,
                "The whole card is the control, and it is the path its own Button activates.");
            Assert.That(submitted, Is.SameAs(offer),
                "It forwards the offer's own invocation unchanged.");
            Assert.That(submitted.Command, Is.SameAs(offer.Command));

            // A face this rebuild did not draw answers nothing, which is the refusal every other
            // forwarded control here makes.
            submitted = null;
            Assert.That(
                view.ForwardDefenseCard(new TypedInvocationViewModel(offer.Label, offer.Command)),
                Is.False);
            Assert.That(
                view.ForwardDefenseCard(view.Model.DefenseChoices.Last()),
                Is.False,
                "and DECLINE DEFENSE is not a card control, so it is not reachable as one.");
            Assert.That(submitted, Is.Null);

            // The preview stays the authority's: it is the offer's own breakdown object, not a total
            // this surface recomputed from the face it is drawing.
            var breakdown = offer.DefenseBreakdown;
            Assert.That(breakdown, Is.Not.Null);
            view.ShowDefensePreview(breakdown, new Rect(0f, 0f, 10f, 10f));
            Assert.That(view.ActiveDefensePreview, Is.SameAs(breakdown));
            Assert.That(
                Descendants(view.PreviewRoot).OfType<Label>().Any(label =>
                    label.text.Contains("ATTACK TOTAL")),
                Is.True,
                "and it still reads the projected totals rather than anything on the card.");
        }

        [Test]
        public void FourDefenseCardFacesPlusDecline_FitOneReadableRowWithoutHorizontalScrolling()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var cards = Descendants(view.Root)
                .Where(element => element.name == "gd-defense-card")
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(4),
                "p.6 deals five, one is committed, and the rest are the response - which is the row " +
                "milestone 5.1c sized this region for.");
            Assert.That(
                cards.All(card =>
                    Mathf.Approximately(
                        card.style.width.value.value, RoundActionUnityView.DefenseCardFootprint)),
                Is.True,
                "One width for every offer, so choosing one cannot re-wrap or resize another.");

            var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(2048f, 1152f);
            var occupied = (RoundActionUnityView.DefenseCardFootprint * cards.Length) +
                (RoundActionUnityView.SectionGap * cards.Length) +
                RoundActionUnityView.SectionGutterWidth;
            Assert.That(tray.width - occupied, Is.GreaterThan(120f),
                "Four enlarged whole faces, their gutters, and DECLINE DEFENSE fit without sideways " +
                "scrolling.");
            var section = cards[0].parent;
            Assert.That(section.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap),
                "Defense order remains one row instead of wrapping into a second card stack.");
            Assert.That(CardFootprint.Height, Is.LessThan(tray.height),
                "and one row of them stands inside the tray's ordinary bound, so this response needs " +
                "no taller context than it has always had.");

            // The short desktop viewport keeps the existing bounded-scroll fallback rather than a new
            // rule: the region is smaller than the row, and the tray region already scrolls.
            var short0 = ConfiguredMatchHudRenderer.CalculateCardPanelRect(778f, 300f);
            Assert.That(short0.height, Is.LessThan(CardFootprint.Height));
        }

        // -------------------------------------------------------------------------------------------
        // The standard card footprint (milestone 8.20c)
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryCompleteFaceConsumer_UsesTheOneArtworkAwareBuilder()
        {
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                var requested = new List<string>();
                var surfaces = new List<(string Name, VisualElement Root, string Control)>();
                using (ConfiguredCardArtwork.UseTextureLoader(path =>
                       {
                           requested.Add(path);
                           return texture;
                       }))
                {
                    var tray = new DirectorHandsUnityView();
                    new DirectorHandsPresenter(
                        ConfiguredMatchFixtures.CreateVerifiedMirrorSession(), tray).Refresh();
                    surfaces.Add(("the ordinary tray", tray.Root, "gd-card-choice"));

                    var preview = new VisualElement();
                    preview.Add(DirectorHandsUnityView.BuildStandaloneCard(
                        CurrentRoundCards.DeckCard(
                            new CardInstanceId("artwork-preview"),
                            WaspCardContent.Definition(WaspCardContent.Shock))));
                    surfaces.Add(("a direct complete-face preview", preview, "gd-card-preview"));

                    var defense = new RoundActionUnityView();
                    new RoundActionPresenter(
                        ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse(), defense)
                        .Refresh();
                    surfaces.Add(("p.14's Defense response", defense.Root, "gd-defense-card"));

                    var upgrade = new RoundActionUnityView();
                    new RoundActionPresenter(
                        ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _),
                        upgrade).Refresh();
                    surfaces.Add(("p.15's Upgrade a Card", upgrade.Root, "gd-upgrade-option"));

                    var chooser = new RoundActionUnityView();
                    new RoundActionPresenter(SessionAwaitingACardValuedEffectChoice(), chooser)
                        .Refresh();
                    surfaces.Add(("a card-valued effect choice", chooser.Root, "gd-effect-card"));

                    var resolvedSession =
                        ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out _);
                    var turnSlot = new HeroOverviewsUnityView();
                    new HeroOverviewsPresenter(resolvedSession, turnSlot).Refresh();
                    turnSlot.ForwardTurnSlotHighlight(turnSlot.Model.Heroes
                        .SelectMany(hero => hero.TurnSlots).First(slot => slot.IsOccupied));
                    surfaces.Add(("a revealed turn-slot preview", turnSlot.Root, "gd-turn-slot-preview"));

                    var discard = new HeroOverviewsUnityView();
                    new HeroOverviewsPresenter(resolvedSession, discard).Refresh();
                    discard.ForwardDiscardCardHighlight(discard.Model.Heroes
                        .SelectMany(hero => hero.DiscardCards).First());
                    surfaces.Add(("a discard-entry preview", discard.Root, "gd-turn-slot-preview"));

                    var current = new HeroOverviewsUnityView();
                    new HeroOverviewsPresenter(
                        ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn(), current)
                        .Refresh();
                    current.ForwardCurrentCardHighlight(current.Model.Heroes
                        .Select(hero => hero.CurrentCard).First(card => card != null));
                    surfaces.Add(("a current-card preview", current.Root, "gd-turn-slot-preview"));

                    var ultimate = new HeroOverviewsUnityView();
                    new HeroOverviewsPresenter(
                        ConfiguredMatchFixtures.CreateVerifiedMirrorInRadiusOfAMinion(
                            MatchConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                                TeamSide.Red),
                            out _, out _, out _),
                        ultimate).Refresh();
                    ultimate.ForwardDiscardCardHighlight(ultimate.Model.Heroes
                        .Select(hero => hero.UltimateCard).First(card => card != null));
                    surfaces.Add(("an unlocked-Ultimate preview", ultimate.Root, "gd-turn-slot-preview"));
                }

                var artworkCount = 0;
                foreach (var surface in surfaces)
                {
                    Assert.That(
                        Descendants(surface.Root).Any(element => element.name == surface.Control),
                        Is.True,
                        $"{surface.Name} was not actually reached, so its artwork path is unproved.");
                    var faces = Descendants(surface.Root)
                        .Where(element =>
                            element.name == "gd-card-choice" ||
                            element.name == "gd-card-preview")
                        .ToArray();
                    Assert.That(faces, Is.Not.Empty, $"{surface.Name} draws no complete face.");
                    foreach (var face in faces)
                    {
                        Assert.That(face.userData, Is.TypeOf<CardDefinitionReference>(),
                            $"{surface.Name} did not carry the stable identity consumed by the one " +
                            "complete-face builder.");
                        var reference = (CardDefinitionReference)face.userData;
                        var hasConfiguredArtwork =
                            ConfiguredCardArtworkCatalog.Catalog.TryGet(reference, out _);
                        Assert.That(
                            Descendants(face).Count(element => element.name == "gd-card-artwork"),
                            Is.EqualTo(hasConfiguredArtwork ? 1 : 0),
                            $"{surface.Name} bypassed or duplicated the shared per-card " +
                            "artwork/fallback decision.");
                        Assert.That(
                            Descendants(face).Count(element => element.name == "gd-card-frame"),
                            Is.EqualTo(hasConfiguredArtwork ? 0 : 1),
                            $"{surface.Name} bypassed or duplicated the shared face construction.");
                        if (hasConfiguredArtwork)
                        {
                            AssertIsTheArtworkFootprint(face, surface.Name);
                            Assert.That(face.style.borderTopWidth.value, Is.Zero,
                                $"{surface.Name} put a synthetic border around the printed front.");
                        }
                        else
                        {
                            AssertIsTheStandardFootprint(face, surface.Name);
                        }
                        if (hasConfiguredArtwork)
                        {
                            artworkCount++;
                        }
                    }
                }

                Assert.That(requested, Has.Count.EqualTo(artworkCount),
                    "Each complete face performs one identity-to-resource lookup, with no second " +
                    "surface-specific artwork path.");
                Assert.That(
                    requested.All(path => path.StartsWith(
                        "Private/Cards/", StringComparison.Ordinal)),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ArtworkFaces_KeepStateReadableWithoutReintroducingAPermanentFrame()
        {
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    var selectedSession = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
                    var selectedView = new DirectorHandsUnityView();
                    var selectedPresenter = new DirectorHandsPresenter(selectedSession, selectedView);
                    selectedPresenter.Refresh();
                    var selectedCard = selectedView.Model.ViewedHand.TrayCards.First(card =>
                        card.IsSelectable);
                    Assert.That(selectedPresenter.SelectCommitmentCard(selectedCard.CardInstanceId),
                        Is.True);
                    var selected = Descendants(selectedView.Root)
                        .Single(element => element.name == "gd-card-choice" &&
                            element.tooltip.Contains(selectedCard.DisplayName));
                    Assert.That(selected.style.borderTopWidth.value, Is.Zero,
                        "The prepared front itself has no synthetic frame.");
                    var selection = Descendants(selected).Single(element =>
                        element.name == "gd-card-selection-outline");
                    Assert.That(selection.style.borderTopColor.value,
                        Is.EqualTo(MatchHudTheme.SelectedOutline));
                    Assert.That(selection.style.borderTopWidth.value, Is.EqualTo(3f),
                        "Selection is an overlay and never changes the artwork footprint.");
                    AssertIsTheArtworkFootprint(selected, "the selected tray card");

                    var discardedView = new DirectorHandsUnityView();
                    new DirectorHandsPresenter(
                        ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn(),
                        discardedView).Refresh();
                    var unavailableCard = discardedView.Model.ViewedHand.TrayCards.First(card =>
                        !card.IsSelectable && !card.IsResolving);
                    var unavailable = Descendants(discardedView.Root)
                        .Where(element => element.name == "gd-card-choice")
                        .Single(element => element.tooltip.Contains(unavailableCard.DisplayName));
                    Assert.That(unavailable.enabledSelf, Is.False);
                    Assert.That(unavailable.style.opacity.value, Is.EqualTo(0.45f));
                    Assert.That(Descendants(unavailable)
                        .Any(element => element.name == "gd-card-history"), Is.False,
                        "Lifecycle remains in the typed tooltip rather than a generated strip.");
                    Assert.That(unavailable.tooltip, Does.Contain(unavailableCard.HistoryLabel));
                    AssertIsTheArtworkFootprint(unavailable, "the unavailable tray card");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Every surface that draws a complete card face draws it at the one shared footprint.
        ///
        /// <para>Milestone 8.20c. Before it the width was shared and the height was not: the tray gave
        /// a card a floor and stretched the row to the tallest one's content, a Defense offer and a
        /// card-valued effect choice grew their face to the frame around them, and the rail's preview
        /// drew the same card at whatever its own words needed - so one printed face came out four
        /// different heights depending on which surface asked for it. The check is on the face's own
        /// box rather than on the control around it, because the frame is each surface's own business
        /// and only the face is shared.</para>
        /// </summary>
        [Test]
        public void EveryFullCardSurface_DrawsItsFaceAtTheStandardFootprint()
        {
            var tray = new DirectorHandsUnityView();
            new DirectorHandsPresenter(ConfiguredMatchFixtures.CreateVerifiedMirrorSession(), tray)
                .Refresh();

            var preview = new VisualElement();
            preview.Add(DirectorHandsUnityView.BuildStandaloneCard(CurrentRoundCards.DeckCard(
                new CardInstanceId("footprint-preview"),
                WaspCardContent.Definition(WaspCardContent.Shock))));

            var defense = new RoundActionUnityView();
            new RoundActionPresenter(
                ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse(), defense).Refresh();

            var upgrade = new RoundActionUnityView();
            new RoundActionPresenter(
                ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _), upgrade)
                .Refresh();

            var chooser = new RoundActionUnityView();
            new RoundActionPresenter(SessionAwaitingACardValuedEffectChoice(), chooser).Refresh();

            var surfaces = new (string Name, VisualElement Root, string Control)[]
            {
                ("the ordinary tray", tray.Root, "gd-card-choice"),
                ("a complete-face preview", preview, "gd-card-preview"),
                ("p.14's Defense response", defense.Root, "gd-defense-card"),
                ("p.15's Upgrade a Card", upgrade.Root, "gd-upgrade-option"),
                ("a card-valued effect choice", chooser.Root, "gd-effect-card")
            };

            foreach (var surface in surfaces)
            {
                Assert.That(
                    Descendants(surface.Root).Any(element => element.name == surface.Control),
                    Is.True,
                    $"{surface.Name} draws no {surface.Control}, so this case proves nothing about " +
                    "it and the surface must be reached differently rather than dropped.");

                var faces = Descendants(surface.Root)
                    .Where(element =>
                        element.name == "gd-card-preview" || element.name == "gd-card-choice")
                    .ToArray();
                Assert.That(faces, Is.Not.Empty,
                    $"{surface.Name} draws no complete card face.");
                foreach (var face in faces)
                {
                    AssertIsTheStandardFootprint(face, surface.Name);
                }
            }
        }

        /// <summary>
        /// Neither what a card says nor what state it is in changes its footprint.
        ///
        /// <para>Milestone 8.20c, and this is the exhaustive half the Play Mode journey deliberately
        /// does not attempt: every verified face of both configured heroes, every lifecycle group, and
        /// each of the five state treatments a card can wear - selected, in play, unavailable,
        /// Active-effect source, and the plain available card. The journey proves the two states a
        /// player reaches by pointing at the live surface; reaching all of these through real play
        /// would prove the same property several times over at several times the cost.</para>
        /// </summary>
        [Test]
        public void NoCardStateOrContent_ChangesTheStandardFootprint()
        {
            // Content: every printed face both configured heroes have, including the ones no player
            // can reach - a footprint the box's cards do not keep is a footprint the next staging
            // could break by dealing one.
            var everyFace = WaspCardContent.AllDefinitions
                .Concat(ArienCardContent.AllDefinitions)
                .ToArray();
            Assert.That(everyFace, Has.Length.GreaterThan(30),
                "Both configured heroes' whole sets are measured, not one hand of five.");
            foreach (var definition in everyFace)
            {
                var face = DirectorHandsUnityView.BuildStandaloneCard(CurrentRoundCards.DeckCard(
                    new CardInstanceId($"footprint-{definition.Id.Value}"), definition));
                AssertIsTheStandardFootprint(face, definition.DisplayName);
            }

            // State: the five treatments, each on the tray's own control, taken from real positions
            // rather than from hand-built view models - a state a match cannot reach is not a state
            // this HUD has to keep a promise about.
            var states = new (string Name, MatchSession Session)[]
            {
                ("an untouched hand", ConfiguredMatchFixtures.CreateVerifiedMirrorSession()),
                ("a card picked up and committed facedown",
                    ConfiguredMatchFixtures.CreateSessionWithAFacedownCommitment(out _)),
                ("a revealed card being resolved",
                    ConfiguredMatchFixtures.CreateSessionWithARevealedUnresolvedTurn()),
                ("a spent, discarded card",
                    ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out _)),
                ("a card sourcing a live Active effect",
                    ConfiguredMatchFixtures.CreateSessionWithALiveActiveEffect(out _))
            };

            foreach (var state in states)
            {
                var view = new DirectorHandsUnityView();
                new DirectorHandsPresenter(state.Session, view).Refresh();
                var cards = Descendants(view.Root)
                    .Where(element => element.name == "gd-card-choice")
                    .ToArray();
                Assert.That(cards, Is.Not.Empty, $"The tray draws no card for {state.Name}.");
                foreach (var card in cards)
                {
                    AssertIsTheStandardFootprint(card, state.Name);
                }
            }
        }

        /// <summary>
        /// The reserved band keeps the whole readable hand at a short landscape desktop viewport
        /// without shrinking a card or introducing horizontal travel.
        /// </summary>
        [Test]
        public void AtAShortDesktopViewport_TheReservedBandKeepsTheWholeReadableHand()
        {
            // Milestone 8.25c: both are PANEL rectangles in reference units. The reference is what a
            // 16:9 window of any pixel count resolves to, so the window that reaches the short case is
            // one whose SHAPE is short rather than one whose pixel count is - a 2560x540 window on a
            // wide monitor, which resolves to a panel about 3724 by 785.
            var referencePanel = new Vector2(
                MatchHudPanelScaling.ReferenceWidth, MatchHudPanelScaling.ReferenceHeight);
            var shortPanel = MatchHudPanelScaling.ReferenceViewport(2560f, 540f);

            var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(
                referencePanel.x, referencePanel.y);
            Assert.That(CardFootprint.Height, Is.LessThan(tray.height),
                "One card stands inside the tray region at the reference resolution.");
            Assert.That(CardFootprint.Height,
                Is.EqualTo(CardFootprint.StandardHeight),
                "Every complete face keeps the readable height.");

            var shortViewport = ConfiguredMatchHudRenderer.CalculateCardPanelRect(
                shortPanel.x, shortPanel.y);
            Assert.That(shortViewport.height, Is.GreaterThan(CardFootprint.Height),
                "The short landscape panel still reserves enough height for the readable card.");

            var view = new DirectorHandsUnityView();
            new DirectorHandsPresenter(ConfiguredMatchFixtures.CreateVerifiedMirrorSession(), view)
                .Refresh();
            view.SetCompactPeekMode(true);
            foreach (var card in Descendants(view.Root)
                .Where(element => element.name == "gd-card-choice"))
            {
                AssertIsTheStandardFootprint(card, "the short viewport");
            }

            Assert.That(ConfiguredMatchHudRenderer.CompactHandRightEdge,
                Is.LessThan(shortViewport.width),
                "The wide-short desktop panel fits the complete hand without horizontal travel.");

            // The tray keeps the window margins and takes the full width at both shapes; neither the
            // rail width nor the card footprint gives.
            foreach (var panel in new[] { shortPanel, referencePanel })
            {
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculateCardPanelRect(panel.x, panel.y).x,
                    Is.Zero,
                    $"The card reservation reaches the bottom-left anchor at {panel}.");
                Assert.That(
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Red).width,
                    Is.EqualTo(ConfiguredMatchHudRenderer.HeroRailWidth),
                    $"The rail is one width at every supported panel, and {panel} is one.");
            }
        }

        /// <summary>
        /// One card face's outer box, held to the shared contract at both ends. The minimum and the
        /// maximum are checked as well as the size, because a size alone is a preference a container
        /// can overrule and containers had been overruling it.
        /// </summary>
        private static void AssertIsTheStandardFootprint(VisualElement face, string where)
        {
            Assert.That(face.style.width.value.value, Is.EqualTo(CardFootprint.Width).Within(0.01f),
                $"A card face on {where} is not the standard width.");
            Assert.That(face.style.height.value.value, Is.EqualTo(CardFootprint.Height).Within(0.01f),
                $"A card face on {where} is not the standard height.");
            Assert.That(face.style.minHeight.value.value,
                Is.EqualTo(CardFootprint.Height).Within(0.01f),
                $"A card face on {where} may still be squeezed shorter than the footprint.");
            Assert.That(face.style.maxHeight.value.value,
                Is.EqualTo(CardFootprint.Height).Within(0.01f),
                $"A card face on {where} may still be stretched taller than the footprint.");
            Assert.That(face.style.flexGrow.value, Is.EqualTo(0f),
                $"A card face on {where} still grows with the space it is given.");
            Assert.That(face.style.flexShrink.value, Is.EqualTo(0f),
                $"A card face on {where} still shrinks with the space it is given.");
        }

        private static void AssertIsTheArtworkFootprint(VisualElement face, string where)
        {
            Assert.That(face.style.width.value.value,
                Is.EqualTo(CardFootprint.ArtworkWidth).Within(0.01f),
                $"A prepared card face on {where} is not the shared width.");
            Assert.That(face.style.height.value.value,
                Is.EqualTo(CardFootprint.ArtworkHeight).Within(0.01f),
                $"A prepared card face on {where} does not preserve 372:520.");
            Assert.That(face.style.minHeight.value.value,
                Is.EqualTo(CardFootprint.ArtworkHeight).Within(0.01f),
                $"A prepared card face on {where} may still be squeezed shorter.");
            Assert.That(face.style.maxHeight.value.value,
                Is.EqualTo(CardFootprint.ArtworkHeight).Within(0.01f),
                $"A prepared card face on {where} may still be stretched taller.");
            Assert.That(face.style.flexGrow.value, Is.Zero);
            Assert.That(face.style.flexShrink.value, Is.Zero);
        }

        /// <summary>
        /// A position standing on rulebook p.10's discard chooser, which is the card-valued effect
        /// choice this build reaches: the chosen hero is being asked which of their own cards to give
        /// up, and every option is drawn as a whole printed face.
        /// </summary>
        private static MatchSession SessionAwaitingACardValuedEffectChoice()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var red = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            var blue = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(
                red, HandCardOf(session, red, WaspCardContent.Shock).Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(
                blue, HandCardOf(session, blue, WaspCardContent.StopProjectiles).Id));
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable && offer.ActionDefinitionId.Equals(WaspCardContent.PrimaryAttack));
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, minionId));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.SelectInvocations.Single()));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.PendingEffectChoice?.ChooserId, Is.EqualTo(blue),
                "The fixture must be standing on the chosen hero's own card question.");
            return session;
        }

        private static CardInstance HandCardOf(
            MatchSession session, HeroId heroId, CardDefinitionId definitionId) =>
            session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(heroId) &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(definitionId));

        // -------------------------------------------------------------------------------------------
        // One usable interaction context
        // -------------------------------------------------------------------------------------------

        [Test]
        public void WhileAMandatoryResponseStands_TheOrdinaryTrayIsNotOnScreen()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var roundView = new RoundActionUnityView();
            new RoundActionPresenter(session, roundView).Refresh();
            var handsView = new DirectorHandsUnityView();
            new DirectorHandsPresenter(session, handsView).Refresh();

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null);
            Assert.That(roundView.HasExclusiveDecision, Is.True);
            handsView.SetHidden(roundView.HasExclusiveDecision);
            Assert.That(handsView.Root.style.display.value, Is.EqualTo(DisplayStyle.None),
                "The hand is not merely inert while a response stands: it is not the question.");
            Assert.That(roundView.Model.Actions, Is.Empty,
                "and no ordinary action offer is drawn beside the response.");
        }

        /// <summary>
        /// Milestone 7.8a1's half of the greyed control. That the offer is projected unavailable is
        /// proved on the cross-platform tier in <see cref="AttackOfferAvailabilityTests"/>; what this
        /// adds is that presentation does with an unavailable Attack exactly what milestone 6.5af made
        /// it do with an unstartable Skill, and infers nothing of its own.
        /// </summary>
        [Test]
        public void AnUnavailableAttack_IsDrawnAsADisabledControlThatCarriesNoCommand()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand &&
                    session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(candidate.DefinitionReference))
                        .ActionDefinitions.Any(action => action.Kind == ActionKind.Attack));
                ConfiguredMatchFixtures.Accept(
                    session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var attack = session.Projection.AvailableTurnActions.First(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            Assert.That(attack.IsAvailable, Is.False,
                "The configured setup starts the heroes far apart, so this Attack reaches nobody.");

            var view = new RoundActionUnityView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();

            Assert.That(
                view.Model.UnavailableActions.Any(action =>
                    action.ActionDefinitionId.Equals(attack.ActionDefinitionId)),
                Is.True,
                "Milestone 6.5af's greyed control, reached by an Attack for the first time.");
            Assert.That(
                view.Model.Actions.Any(action =>
                    action.Command.ActionDefinitionId.Equals(attack.ActionDefinitionId)),
                Is.False,
                "and it carries no command at all rather than a refused one.");
            Assert.That(
                presenter.Submit(new TypedInvocationViewModel(
                    "STALE", DirectorCommand.StartAction(attack.ActionDefinitionId))),
                Is.False,
                "The round presenter refuses a start naming it even from an older model.");
        }

        [Test]
        public void TheResolvingCardsPrintedActionRegions_ForwardOnlyTheirExactProjectedOffers()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand);
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var view = new RoundActionUnityView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();
            Assert.That(view.Model.PrintedActionRegions, Has.Count.EqualTo(
                session.Projection.AvailableTurnActions.Count));
            foreach (var region in view.Model.PrintedActionRegions)
            {
                var offer = session.Projection.AvailableTurnActions.Single(value =>
                    value.Equals(region.Offer));
                var definition = session.Projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference));
                var action = definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId));
                Assert.That(region.SourceCardInstanceId,
                    Is.EqualTo(session.Projection.SelectedActor.CardInstanceId));
                Assert.That(region.ActionDefinitionId, Is.EqualTo(action.Id));
                Assert.That(region.ActionKind, Is.EqualTo(action.Kind));
                Assert.That(region.InvocationContext, Is.EqualTo(offer.InvocationContext));
                Assert.That(region.AssistiveLabel,
                    Does.Contain(offer.InvocationContext == ActionInvocationContext.PrintedPrimary
                        ? "PRIMARY"
                        : "SECONDARY"));
                Assert.That(region.AssistiveLabel, Does.Contain(action.Kind.ToString().ToUpperInvariant()));
                Assert.That(region.AssistiveLabel, Does.Contain(
                    action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Applies
                        ? "text applies"
                        : "text suppressed"));
                Assert.That(region.Invocation == null, Is.EqualTo(!offer.IsAvailable),
                    "An unavailable printed region structurally carries no command.");
                if (region.Invocation != null)
                {
                    // Milestone 8.29f: the exact projected offer now includes the card it was drawn
                    // on, which is what makes a region's command refusable once that card is spent.
                    // An action definition id alone named no card, so a region held across the
                    // action performed from the region beside it rebound to the next hero's.
                    Assert.That(region.Invocation.Command,
                        Is.EqualTo(DirectorCommand.StartAction(
                            offer.SourceCardInstanceId, offer.ActionDefinitionId)));
                }
            }

            Assert.That(view.Model.PrintedActionRegions
                    .TakeWhile(region => region.InvocationContext != ActionInvocationContext.PrintedPrimary)
                    .Select(region => region.SecondaryIndex),
                Is.Ordered,
                "Keyboard order follows projected Secondary actions before the Primary action.");
            Assert.That(view.Model.FastTravelOffers, Has.All.Matches<FastTravelOfferViewModel>(replacement =>
                view.Model.PrintedActionRegions.Any(region =>
                    ReferenceEquals(region.FastTravelOffer, replacement) &&
                    region.ActionDefinitionId.Equals(replacement.ReplacedMovementActionDefinitionId))));
            Assert.That(view.Model.ClearOffers, Has.All.Matches<ClearOfferViewModel>(replacement =>
                view.Model.PrintedActionRegions.Any(region =>
                    ReferenceEquals(region.ClearOffer, replacement) &&
                    region.ActionDefinitionId.Equals(replacement.ReplacedAttackActionDefinitionId))));

            var actionSection = view.Root.Q<VisualElement>("gd-action-section");
            Assert.That(actionSection, Is.Not.Null);
            Assert.That(actionSection.Children().OfType<Button>().ToArray(), Has.Length.EqualTo(1),
                "Printed Primary and Secondary starts belong to typed regions on the resolving card; " +
                "only End Turn remains an ordinary external control.");

            var hands = new DirectorHandsUnityView();
            new DirectorHandsPresenter(session, hands).Refresh();
            hands.SetPrintedActionRegions(view.Model.PrintedActionRegions);
            var controls = Descendants(hands.Root)
                .Where(element => element.name == "gd-printed-action-region").ToArray();
            Assert.That(controls, Has.Length.EqualTo(view.Model.PrintedActionRegions.Count));
            Assert.That(controls.Select(control => control.userData),
                Is.EqualTo(view.Model.PrintedActionRegions));

            TypedInvocationViewModel forwarded = null;
            hands.PrintedActionSubmitted += invocation => forwarded = invocation;
            var available = view.Model.PrintedActionRegions.First(region => region.Invocation != null);
            Assert.That(hands.ForwardPrintedActionRegion(available), Is.True);
            Assert.That(forwarded, Is.SameAs(available.Invocation),
                "The region forwards the standing typed invocation without rebuilding it.");

            hands.SetPrintedActionRegions(Array.Empty<PrintedActionRegionViewModel>());
            forwarded = null;
            Assert.That(hands.ForwardPrintedActionRegion(available), Is.False,
                "A region retained from the prior projection is stale.");
            Assert.That(forwarded, Is.Null);
            var forged = new PrintedActionRegionViewModel(
                available.Offer,
                available.ActionKind,
                available.ActionLabel,
                available.SecondaryIndex,
                available.SecondaryCount,
                available.AssistiveLabel,
                available.Invocation,
                available.FastTravelOffer,
                available.ClearOffer);
            Assert.That(hands.ForwardPrintedActionRegion(forged), Is.False,
                "An equivalent but unprojected region cannot forge the current card's start.");
        }

        [Test]
        public void PreparedArtworkPrintedActionRegions_AreSilentAndUseTheOfficialTypedSlots()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand);
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var round = new RoundActionUnityView();
            new RoundActionPresenter(session, round).Refresh();
            var texture = new Texture2D(372, 520, TextureFormat.RGBA32, false);
            try
            {
                using (ConfiguredCardArtwork.UseTextureLoader(_ => texture))
                {
                    var hands = new DirectorHandsUnityView();
                    new DirectorHandsPresenter(session, hands).Refresh();
                    hands.SetPrintedActionRegions(round.Model.PrintedActionRegions);
                    var controls = Descendants(hands.Root)
                        .Where(element => element.name == "gd-printed-action-region")
                        .OfType<Button>().ToArray();
                    Assert.That(controls, Is.Not.Empty);
                    Assert.That(controls.Select(control => control.text), Is.All.Empty,
                        "The printed face already names and values its actions; overlays add no copy.");
                    Assert.That(controls.Select(control => control.style.backgroundColor.value.a),
                        Is.All.EqualTo(0f), "The typed hit surfaces add no synthetic fill.");
                    Assert.That(controls.Select(control => control.style.borderTopWidth.value),
                        Is.All.EqualTo(0f),
                        "Prepared artwork uses its own symbol shine instead of a blue box outline.");
                    var availableControls = controls.Where(control => control.enabledSelf).ToArray();
                    Assert.That(availableControls, Is.Not.Empty);
                    Assert.That(availableControls.All(control => Descendants(control)
                            .Count(element => element.name == "gd-prepared-action-glow") == 1),
                        Is.True,
                        "Each available hotspot owns exactly one stable prepared-action glow.");
                    Assert.That(availableControls.SelectMany(Descendants)
                            .Any(element => element.name == "gd-prepared-action-artwork"), Is.False,
                        "An opaque prepared-card crop must never be raised over the printed face.");
                    Assert.That(availableControls.All(control => Descendants(control)
                            .Single(element => element.name == "gd-prepared-action-glow")
                            .userData.Equals(((PrintedActionRegionViewModel)control.userData).ActionKind)),
                        Is.True,
                        "The glow follows the projected typed action kind without card names.");
                    var glowLayers = availableControls.SelectMany(Descendants)
                        .Where(element => element.name == "gd-prepared-action-glow-halo" ||
                            element.name == "gd-prepared-action-glow-bloom" ||
                            element.name == "gd-prepared-action-glow-core")
                        .ToArray();
                    Assert.That(glowLayers, Has.Length.EqualTo(availableControls.Length * 3),
                        "Each prepared region owns an outer halo, amber bloom and bright core.");
                    Assert.That(glowLayers.OfType<Image>(), Is.Empty,
                        "The simple geometry never copies or re-layers prepared-card pixels.");

                    var primary = controls.Single(control =>
                        ((PrintedActionRegionViewModel)control.userData).InvocationContext ==
                            ActionInvocationContext.PrintedPrimary);
                    Assert.That(primary.style.top.value.value,
                        Is.GreaterThan(CardFootprint.Height * 0.55f),
                        "The prepared Primary hotspot belongs to the official lower action strip.");
                    var primaryModel = (PrintedActionRegionViewModel)primary.userData;
                    var primaryGlow = Descendants(primary)
                        .SingleOrDefault(element => element.name == "gd-prepared-action-glow");
                    if (primaryModel.ActionKind == ActionKind.Movement)
                    {
                        Assert.That(primary.style.width.value.value,
                            Is.LessThan(CardFootprint.Width * 0.25f));
                        Assert.That(primaryGlow == null ||
                            primaryGlow.ClassListContains("gd-prepared-action-glow-circle"), Is.True,
                            "Primary Movement uses the same compact circle as the rail boot.");
                    }
                    else
                    {
                        Assert.That(primaryModel.ActionKind,
                            Is.EqualTo(ActionKind.Skill).Or.EqualTo(ActionKind.Attack));
                        Assert.That(primary.style.width.value.value,
                            Is.GreaterThan(CardFootprint.Width * 0.90f),
                            "Primary Skill or Attack boxes span the complete printed action panel.");
                        Assert.That(primary.style.height.value.value,
                            Is.GreaterThan(CardFootprint.Height * 0.25f));
                        Assert.That(primaryGlow == null ||
                            primaryGlow.ClassListContains("gd-prepared-action-glow-box"), Is.True);
                    }

                    var secondary = controls.Single(control =>
                        ((PrintedActionRegionViewModel)control.userData).InvocationContext ==
                            ActionInvocationContext.PrintedSecondary);
                    Assert.That(((PrintedActionRegionViewModel)secondary.userData).ActionKind,
                        Is.EqualTo(ActionKind.Movement));
                    Assert.That(secondary.style.left.value.value,
                        Is.LessThan(CardFootprint.Width * 0.10f));
                    Assert.That(secondary.style.width.value.value,
                        Is.LessThan(CardFootprint.Width * 0.25f),
                        "Secondary Movement circles its left-rail boot/value badge.");
                    Assert.That(secondary.style.top.value.value,
                        Is.InRange(CardFootprint.Height * 0.30f, CardFootprint.Height * 0.50f));
                    var secondaryGlow = Descendants(secondary)
                        .SingleOrDefault(element => element.name == "gd-prepared-action-glow");
                    Assert.That(secondaryGlow == null ||
                        secondaryGlow.ClassListContains("gd-prepared-action-glow-circle"), Is.True);
                }

                var fallbackHands = new DirectorHandsUnityView();
                new DirectorHandsPresenter(session, fallbackHands).Refresh();
                fallbackHands.SetPrintedActionRegions(round.Model.PrintedActionRegions);
                var fallbackPrimary = Descendants(fallbackHands.Root)
                    .Where(element => element.name == "gd-printed-action-region")
                    .OfType<Button>().Single(control =>
                        ((PrintedActionRegionViewModel)control.userData).InvocationContext ==
                            ActionInvocationContext.PrintedPrimary);
                Assert.That(fallbackPrimary.style.top.value.value,
                    Is.LessThan(CardFootprint.Height * 0.40f),
                    "The generated fallback keeps its own upper-band anatomy.");
                Assert.That(Descendants(fallbackPrimary)
                        .Any(element => element.name == "gd-prepared-action-glow"), Is.False);
                Assert.That(fallbackPrimary.style.borderTopWidth.value, Is.GreaterThan(0f),
                    "The generated face retains its synthetic action-region treatment.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The paused system transition, both kinds
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APausedLevelUp_IsDrawnWithItsHeroLevelCostRemainingCoinsAndEveryProjectedOption()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out var redHeroId);
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var prompt = view.Model.SystemTransitionPrompt;
            var description = session.Projection.PendingSystemTransitionDescription;
            Assert.That(description, Is.Not.Null);
            Assert.That(description.PendingStep,
                Is.SameAs(session.Projection.PendingSystemTransitionStep),
                "The description describes the pause the projection carries, never another one.");
            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            Assert.That(prompt.SectionLabel, Is.EqualTo("LEVEL UP"));

            var heroName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(redHeroId)).DisplayName.ToUpperInvariant();
            Assert.That(prompt.PromptLabel, Does.Contain(heroName));
            Assert.That(prompt.PromptLabel, Does.Contain("LEVEL 2"));
            Assert.That(prompt.PromptLabel, Does.Contain("UPGRADE A CARD"));
            Assert.That(prompt.StatusLabel, Does.Contain("PAID 1 COIN"),
                "p.15's table prices the first level at 1.");
            Assert.That(prompt.StatusLabel, Does.Contain("1 REMAINING"),
                "p.13's melee minion paid 2, and 1 of them bought the level.");

            Assert.That(prompt.Options, Has.Count.EqualTo(description.Options.Count),
                "Presentation adds no option and removes none.");

            // The grid breaks a row where the upgraded card changes, which is only faithful because the
            // pause already groups its options that way. If p.15's options for one card ever stopped
            // arriving together, that layout would be a sort rather than a line break.
            var groups = prompt.Options.Select(option => option.SourceCardInstanceId).ToArray();
            Assert.That(groups.Distinct().Count(),
                Is.EqualTo(groups.Where((value, index) =>
                    index == 0 || !value.Equals(groups[index - 1])).Count()),
                "Every option for one upgraded card is contiguous in the projected order.");
            for (var index = 0; index < prompt.Options.Count; index++)
            {
                var option = description.Options[index];
                var drawn = prompt.Options[index];
                Assert.That(drawn.Label,
                    Does.Contain(NameOf(session, option.UpgradedCard))
                        .And.Contain(NameOf(session, option.TakenCard)),
                    "Each option names the card returned to the deck and the card taken in its place.");
                Assert.That(drawn.Detail, Does.Contain(NameOf(session, option.SetAsideCard)),
                    "and the sibling p.15 sets aside.");
                Assert.That(drawn.Detail,
                    Does.Contain(MatchHudTheme.ChannelToken(option.ItemIcon)),
                    "and the printed-value symbol whose slot p.16 files it under.");
                // The option wears the card's own colour, the way the tray's cards do, and never a
                // colour alone: the colour's name is on the control beside it.
                var upgraded = ColorOf(session, option.UpgradedCard);
                Assert.That(drawn.Color, Is.EqualTo(upgraded));
                Assert.That(drawn.Color, Is.Not.EqualTo(CardColor.None));
                Assert.That(ColorOf(session, option.TakenCard), Is.EqualTo(upgraded),
                    "p.15 finds the next-Tier cards at \"the same color as the chosen card\", so one " +
                    "colour is right for the whole option.");
                Assert.That(drawn.ColorName, Is.EqualTo(MatchHudTheme.ColorName(upgraded)));

                Assert.That(drawn.Command.Kind,
                    Is.EqualTo(DirectorCommandKind.AnswerSystemTransitionStep));
                Assert.That(
                    session.Projection.PendingSystemTransitionStep.Invocations
                        .Any(invocation => invocation.OptionId.Equals(option.OptionId)),
                    Is.True,
                    "and forwards the invocation the continuation itself projected.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.8a: p.15's upgrade choices as tray-style cards
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EachUpgradeChoice_DrawsItsCandidateThroughTheTraysOwnCardFace()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var description = session.Projection.PendingSystemTransitionDescription;
            var prompt = view.Model.SystemTransitionPrompt;
            Assert.That(prompt.Options, Has.Count.EqualTo(6),
                "p.5 leaves Wasp holding one card of each non-basic colour, and p.15 finds two " +
                "candidates for each of the three.");

            for (var index = 0; index < prompt.Options.Count; index++)
            {
                var option = description.Options[index];
                var drawn = prompt.Options[index];
                var taken = session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(option.TakenCard));
                var expected = CurrentRoundCards.ReadFace(taken);

                Assert.That(drawn.Card, Is.Not.Null);
                Assert.That(drawn.Card.CardInstanceId, Is.EqualTo(option.TakenCardInstanceId),
                    "The face is the candidate replacement card, which is the card the option puts " +
                    "in the hand - not the card it takes out.");
                Assert.That(drawn.Card.DisplayName, Is.EqualTo(taken.DisplayName));
                Assert.That(drawn.Card.Color, Is.EqualTo(taken.Progression.Color));
                Assert.That(drawn.Card.ColorName,
                    Is.EqualTo(MatchHudTheme.ColorName(taken.Progression.Color)),
                    "and its colour is readable as a word, because nothing here means a thing by " +
                    "colour alone.");
                Assert.That(drawn.Card.Group, Is.EqualTo(TrayCardGroup.Deck));
                Assert.That(drawn.Card.HistoryLabel, Is.EqualTo("IN DECK"),
                    "p.15 finds the candidates in the deck, and the lifecycle band says so rather " +
                    "than borrowing a word about a card in somebody's hand.");
                Assert.That(drawn.Card.IsActiveEffectSource, Is.False);
                Assert.That(drawn.Card.IsResolving, Is.False);

                Assert.That(drawn.Card.Face.ValueLine, Is.EqualTo(expected.ValueLine),
                    "Every printed value is read into the same regions the tray reads them into.");
                Assert.That(drawn.Card.Face.PrimaryActionLabel,
                    Is.EqualTo(expected.PrimaryActionLabel),
                    "including p.13's Ranged and p.14's Block qualifiers, which are typed facts.");
                Assert.That(drawn.Card.Face.PrintedTextKind,
                    Is.EqualTo(CardSourceContentKind.Transcribed),
                    "Wasp's deck is verified content, so the prompt shows printed words.");
                Assert.That(drawn.Card.Face.PrintedText, Is.EqualTo(taken.SourceContent.PrintedText),
                    "and shows them verbatim, without decoding them.");
                Assert.That(
                    drawn.Card.Face.Initiative?.Amount,
                    Is.EqualTo(taken.Initiative.IsDefined ? taken.Initiative.Value : (int?)null));
            }

            var options = Descendants(view.Root)
                .Where(element => element.name == "gd-upgrade-option")
                .ToArray();
            Assert.That(options, Has.Length.EqualTo(6));
            foreach (var control in options)
            {
                foreach (var region in new[]
                    {
                        "gd-card-preview", "gd-card-name-band", "gd-card-secondary-values",
                        "gd-card-primary-action", "gd-card-rules", "gd-card-history"
                    })
                {
                    Assert.That(Descendants(control).Count(element => element.name == region),
                        Is.EqualTo(1),
                        $"Each option draws exactly one {region} - the tray's builder, not a second " +
                        "abbreviated card design.");
                }
            }

            // The user's 2026-08-22 Game View decision. The card returned to the deck and the sibling
            // set aside are not written under six card faces - the grid already says both, because a
            // column is one upgraded card and the sibling is the other card in it - but neither fact is
            // lost: each option's own hover and assistive reading still carries them.
            var drawnText = options
                .SelectMany(Descendants)
                .OfType<Label>()
                .Select(label => label.text)
                .ToArray();
            foreach (var option in prompt.Options)
            {
                Assert.That(drawnText.Any(text => text.Contains(option.ReturnedCardName)), Is.False,
                    "The card p.15 returns to the deck is not written on or under the face.");
                Assert.That(drawnText.Any(text => text.Contains(option.SetAsideCardName)), Is.False,
                    "and neither is the card it sets aside.");
            }

            foreach (var control in options)
            {
                var option = prompt.Options.Single(candidate =>
                    control.tooltip.StartsWith(candidate.Label, StringComparison.Ordinal));
                Assert.That(control.tooltip,
                    Does.Contain(option.ReturnedCardName).And.Contain(option.SetAsideCardName),
                    "Both stay available as words, in the reading this HUD keeps a fact it stops " +
                    "drawing in.");
            }
        }

        [Test]
        public void TheSixUpgradeChoices_AreThreeContainedPairsInOneFullWidthComparisonBand()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var prompt = view.Model.SystemTransitionPrompt;
            var bands = Descendants(view.Root)
                .Where(element => element.name == "gd-upgrade-comparison-band")
                .ToArray();
            Assert.That(bands, Has.Length.EqualTo(1),
                "Level Up is one full-width comparison band, not the legacy two-row grid.");

            var pairs = Descendants(bands[0])
                .Where(element => element.name == "gd-upgrade-pair")
                .ToArray();
            Assert.That(pairs, Has.Length.EqualTo(3), "Blue, Green, Red are three explicit pairs.");
            foreach (var pair in pairs)
            {
                Assert.That(Descendants(pair).Count(cell => cell.name == "gd-upgrade-option"),
                    Is.EqualTo(2), "Each source card keeps its two projected candidates together.");
            }

            // The reading and focus order is the element order, which is what a pointer follows down a
            // pair and what keyboard focus follows through the tree.
            var order = pairs
                .SelectMany(pair => Descendants(pair)
                    .Where(cell => cell.name == "gd-upgrade-option"))
                .ToArray();
            Assert.That(order, Has.Length.EqualTo(6));

            var byControl = order
                .Select(control => prompt.Options.Single(option =>
                    control.tooltip.StartsWith(option.Label, StringComparison.Ordinal)))
                .ToArray();
            Assert.That(
                byControl.Select(option => option.Color),
                Is.EqualTo(new[]
                {
                    CardColor.Blue, CardColor.Blue,
                    CardColor.Green, CardColor.Green,
                    CardColor.Red, CardColor.Red
                }),
                "The single band reads as Blue pair, Green pair, Red pair.");

            // The pause's own order is not Blue, Green, Red - it orders its pairs by card instance
            // identity - so the grid is doing real work here rather than being handed the answer.
            Assert.That(
                prompt.Options.Select(option => option.Color).Distinct(),
                Is.Not.EqualTo(new[] { CardColor.Blue, CardColor.Green, CardColor.Red }),
                "and it is a reordering: the projected order is not already this one.");

            for (var pairIndex = 0; pairIndex < 3; pairIndex++)
            {
                var pair = byControl.Skip(pairIndex * 2).Take(2).ToArray();
                Assert.That(pair[0].SourceCardInstanceId, Is.EqualTo(pair[1].SourceCardInstanceId),
                    "A pair is p.15's two answers for one upgraded card.");
                var projected = prompt.Options
                    .Where(option => option.SourceCardInstanceId.Equals(pair[0].SourceCardInstanceId))
                    .ToArray();
                Assert.That(pair, Is.EqualTo(projected),
                    "and the two candidates keep the order the pause offered them, so presentation " +
                    "invents no choice authority.");
            }

            var width = pairs.Sum(pair => pair.style.width.value.value) +
                (DirectorHandsUnityView.CardGap * (pairs.Length - 1));
            Assert.That(width, Is.LessThanOrEqualTo(2048f),
                "The three complete pairs fit one validated desktop width without horizontal travel.");
        }

        [Test]
        public void AnUpgradeChoice_ForwardsOnlyItsProjectedInvocationAndNamesTheItemItGains()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();

            var description = session.Projection.PendingSystemTransitionDescription;
            var prompt = view.Model.SystemTransitionPrompt;
            var forwarded = new List<TypedInvocationViewModel>();
            view.InvocationSubmitted += invocation => forwarded.Add(invocation);

            for (var index = 0; index < prompt.Options.Count; index++)
            {
                var option = description.Options[index];
                var drawn = prompt.Options[index];

                // The item is printed on the card the option does *not* take, which is why it belongs
                // to the option rather than to the face: choosing between two cards of one colour is
                // also choosing which of the two items the hero ends the round holding.
                var setAside = session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(option.SetAsideCard));
                Assert.That(drawn.SetAsideCardName, Is.EqualTo(setAside.DisplayName));
                Assert.That(drawn.ItemIcon, Is.EqualTo(option.ItemIcon));
                Assert.That(drawn.ItemIcon, Is.Not.EqualTo(PrintedValueChannel.None),
                    "Wasp's dashboard accepts every icon her Tier II cards print.");
                Assert.That(drawn.ItemLabel,
                    Is.EqualTo($"{MatchHudTheme.ChannelToken(option.ItemIcon)} +1"),
                    "and it reads the way p.16's bonus already reads on the hero rail.");
                Assert.That(drawn.ReturnedCardName,
                    Is.EqualTo(session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(option.UpgradedCard))
                        .DisplayName),
                    "p.15's returned card is still stated.");
                Assert.That(drawn.Card.DisplayName, Is.Not.EqualTo(drawn.SetAsideCardName),
                    "The card drawn is the one taken; the item comes from the other one.");
            }

            var items = Descendants(view.Root)
                .Where(element => element.name == "gd-upgrade-item")
                .ToArray();
            Assert.That(items, Has.Length.EqualTo(6),
                "Every option states the item it gains, beside its card rather than on it.");

            var chosen = prompt.Options[0];
            Assert.That(view.ForwardUpgradeOption(chosen), Is.True);
            Assert.That(forwarded, Has.Count.EqualTo(1));
            Assert.That(forwarded[0].Command, Is.SameAs(chosen.Command),
                "The whole option card forwards the invocation the continuation itself projected, " +
                "unchanged.");
            Assert.That(presenter.Submit(forwarded[0]), Is.True);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);

            var stale = chosen;
            view.Render(view.Model);
            presenter.Refresh();
            Assert.That(view.ForwardUpgradeOption(stale), Is.False,
                "An option this rebuild did not draw forwards nothing at all.");
            Assert.That(forwarded, Has.Count.EqualTo(1));
        }

        [Test]
        public void PointingAtAnUpgradeChoice_MarksExactlyOneWithoutResizingAnyCard()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var prompt = view.Model.SystemTransitionPrompt;
            var controls = Descendants(view.Root)
                .Where(element => element.name == "gd-upgrade-option")
                .ToArray();
            var widths = controls.Select(control => control.style.width.value.value).ToArray();
            Assert.That(widths.Distinct().Count(), Is.EqualTo(1), "Every option is one width.");

            Assert.That(view.HighlightedUpgradeOption, Is.Null, "Nothing is marked to begin with.");
            Assert.That(
                controls.Count(control =>
                    control.style.borderTopColor.value == MatchHudTheme.SelectedOutline),
                Is.EqualTo(0));

            var chosen = prompt.Options[3];
            view.ForwardUpgradeOptionHighlight(chosen);
            Assert.That(view.HighlightedUpgradeOption, Is.SameAs(chosen));
            Assert.That(
                controls.Count(control =>
                    control.style.borderTopColor.value == MatchHudTheme.SelectedOutline),
                Is.EqualTo(1),
                "Exactly one option wears the HUD's shared selection outline.");
            Assert.That(
                controls.Select(control => control.style.width.value.value), Is.EqualTo(widths),
                "and marking it changes no card's width, so nothing re-wraps and no row moves.");

            var marked = controls.Single(control =>
                control.style.borderTopColor.value == MatchHudTheme.SelectedOutline);
            Assert.That(marked.tooltip, Does.Contain(chosen.Label).And.Contain(chosen.Detail),
                "The selected option states what it is and what taking it does, so the selection is " +
                "assistive as well as visible.");

            view.ForwardUpgradeOptionHighlight(null);
            Assert.That(view.HighlightedUpgradeOption, Is.Null);
            Assert.That(
                controls.Count(control =>
                    control.style.borderTopColor.value == MatchHudTheme.SelectedOutline),
                Is.EqualTo(0));
        }

        [Test]
        public void TheUpgradeComparisonBand_IsTheOnlyContextAndKeepsEveryOptionReachable()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            Assert.That(view.HasExclusiveDecision, Is.True,
                "A paused transition is the only usable interaction context, so the ordinary tray, " +
                "End Turn and the end-of-round advance all step aside.");
            Assert.That(view.Model.EndTurnInvocation, Is.Null);
            Assert.That(view.Model.ContinueEndOfRoundInvocation, Is.Null);

            var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(2048f, 1152f);
            var band = Descendants(view.Root)
                .Single(element => element.name == "gd-upgrade-comparison-band");
            Assert.That(band.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap));
            Assert.That(view.HasUpgradeComparisonBand, Is.True);
            Assert.That(CardFootprint.Height, Is.LessThanOrEqualTo(tray.height),
                "One row of complete faces fits the standing bottom band; no near-full-height " +
                "context is required.");

            var controls = Descendants(view.Root)
                .Where(element => element.name == "gd-upgrade-option")
                .ToArray();
            Assert.That(controls, Has.Length.EqualTo(6));
            Assert.That(controls.All(control => control.focusable), Is.True,
                "so all six stay keyboard-reachable inside the scrolled body.");
            Assert.That(controls.All(control => control.enabledSelf), Is.True,
                "and none of them is drawn as a refusal.");
            Assert.That(Descendants(view.Root)
                    .Count(element => element.name == "gd-upgrade-pair-label"),
                Is.EqualTo(3), "Every source-card pair is explicitly labelled.");
        }

        [Test]
        public void AnsweringAProjectedOption_AdvancesTheStageAndSuppressesNothingElseWrongly()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();

            Assert.That(view.Model.EndTurnInvocation, Is.Null,
                "A paused transition projects no stop, so no End Turn control is drawn.");
            Assert.That(view.Model.ContinueEndOfRoundInvocation, Is.Null,
                "and no advance is offered beside a pause the advance would be refused by.");

            var option = view.Model.SystemTransitionPrompt.Options[0];
            Assert.That(
                presenter.Submit(new TypedInvocationViewModel(option.Label, option.Command)),
                Is.True);
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
        }

        [Test]
        public void AStaleOption_ExecutesNothingAndLeavesThePauseStanding()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            var view = new RoundActionUnityView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();
            var stale = view.Model.SystemTransitionPrompt.Options[0];

            var forged = new SystemTransitionChoiceCommand(
                session.Projection.PendingSystemTransitionStep.InstanceId,
                session.Projection.PendingSystemTransitionStep.StepId,
                session.Projection.PendingSystemTransitionStep.ItemId,
                session.Projection.PendingSystemTransitionStep.ChoosingSide,
                new SystemTransitionOptionId("no-such-option"));
            Assert.That(
                presenter.Submit(new TypedInvocationViewModel(
                    "FORGED", DirectorCommand.AnswerSystemTransitionStep(forged))),
                Is.False,
                "An option this prompt never projected is refused before it reaches the authority.");
            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Not.Null);

            Assert.That(
                presenter.Submit(new TypedInvocationViewModel(stale.Label, stale.Command)), Is.True);
            Assert.That(
                presenter.Submit(new TypedInvocationViewModel(stale.Label, stale.Command)), Is.False,
                "and the same option submitted twice does nothing the second time.");
        }

        [Test]
        public void APausedLanePushPlace_NamesTheDestinationSpaceRatherThanAnOptionIdentity()
        {
            var fixture = LanePushPlacePause.Create();
            var description = fixture.Transitions.DescribePendingStep();

            Assert.That(description, Is.Not.Null);
            Assert.That(description.Kind, Is.EqualTo(SystemTransitionPromptKind.LanePushRespawnPlace));
            Assert.That(description.HeroId.IsDefined, Is.False,
                "p.8's Place belongs to a team and a minion rather than to a hero.");

            var contract = new PendingDecisionContract(
                PendingDecisionFamilies.SystemTransition,
                description.PendingStep.Invocations.Select(
                    DirectorCommand.AnswerSystemTransitionStep));

            var prompt = RoundActionPresenter.CreateSystemTransitionPrompt(
                description,
                Array.Empty<CardDefinition>(),
                Array.Empty<QuickGameHeroSetupState>(),
                contract);
            Assert.That(prompt.SectionLabel, Is.EqualTo("LANE PUSH"));
            Assert.That(prompt.Options, Has.Count.EqualTo(description.Options.Count));
            for (var index = 0; index < prompt.Options.Count; index++)
            {
                var option = description.Options[index];
                Assert.That(prompt.Options[index].Label,
                    Is.EqualTo($"PLACE AT {option.DestinationSpaceId.Value}"));
                Assert.That(prompt.Options[index].Detail,
                    Is.EqualTo($"({option.Destination.Q}, {option.Destination.R})"));
                Assert.That(option.DestinationSpaceId.Value, Is.EqualTo(option.OptionId.Value),
                    "The space is read from the map, not decoded from the option identity - they " +
                    "agree here because the rules chose the space id as the identity.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Reconstruction and layout
        // -------------------------------------------------------------------------------------------

        [Test]
        public void RestoreReplayAndUndo_RedrawTheSameColoursSlotsAndDiscardFromCurrentAuthority()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var handsView = new DirectorHandsUnityView();
            var overviewView = new HeroOverviewsUnityView();
            var hands = new DirectorHandsPresenter(session, handsView);
            var overviews = new HeroOverviewsPresenter(session, overviewView);
            PlayTurns(session, 2);
            hands.Refresh();
            overviews.Refresh();
            var trayBefore = Describe(handsView);
            var slotsBefore = Describe(overviewView);
            var snapshot = session.CaptureSnapshot();

            PlayTurns(session, 1);
            hands.Refresh();
            overviews.Refresh();
            var afterOneMore = Describe(handsView);
            Assert.That(afterOneMore, Is.Not.EqualTo(trayBefore));

            // Retained-prefix Undo first, while the live history is still the one that produced this
            // position. It walks back one accepted command at a time, so the round is unwound to where
            // the tray and the slots were captured.
            while (session.Projection.IsHistoryAvailable && Describe(handsView) != trayBefore)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
                hands.Refresh();
                overviews.Refresh();
            }

            Assert.That(Describe(handsView), Is.EqualTo(trayBefore),
                "Undo redraws from the truncated history rather than from anything the views kept.");
            Assert.That(Describe(overviewView), Is.EqualTo(slotsBefore));

            // and then the same position again from bytes, which is the other reconstruction path.
            PlayTurns(session, 1);
            hands.Refresh();
            Assert.That(Describe(handsView), Is.EqualTo(afterOneMore));
            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            hands.Refresh();
            overviews.Refresh();
            Assert.That(Describe(handsView), Is.EqualTo(trayBefore),
                "A restored position redraws the same tray from current authority.");
            Assert.That(Describe(overviewView), Is.EqualTo(slotsBefore));
            Assert.That(handsView.Model.Version, Is.EqualTo(session.Projection.Version));
        }

        [Test]
        public void TheCurrentCardMarker_IsRebuiltByRestoreReplayUndoAndRestartRatherThanRemembered()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(session, view);
            PlayTurns(session, 1);
            ConfiguredMatchFixtures.CommitBothHands(session);
            presenter.Refresh();
            var midTurn = Describe(view);
            var snapshot = session.CaptureSnapshot();
            Assert.That(midTurn, Does.Contain("RESOLVING"),
                "The captured position really is one with a card in play and one turn behind it.");
            Assert.That(view.Model.Heroes.All(hero =>
                hero.TurnSlots.Count(slot => slot.IsOccupied) == 1), Is.True);

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            presenter.Refresh();
            Assert.That(Describe(view), Is.Not.EqualTo(midTurn));

            // Retained-prefix Undo, while the live history is still the one that produced this position.
            while (session.Projection.IsHistoryAvailable && Describe(view) != midTurn)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
                presenter.Refresh();
            }

            Assert.That(Describe(view), Is.EqualTo(midTurn),
                "Undo redraws the marker from the truncated history rather than from anything kept here.");

            // The same position again from bytes.
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            presenter.Refresh();
            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            presenter.Refresh();
            Assert.That(Describe(view), Is.EqualTo(midTurn),
                "and a restored position redraws it from current authority.");
            Assert.That(view.HighlightedCurrentCard, Is.Null,
                "A rebuilt rail carries no hover across a reconstruction.");

            // and a restart empties both the marker and the history rather than leaving either
            // standing. It runs on its own session because a snapshot restore clears the command
            // history a restart replays from, so the two reconstructions cannot share one.
            var restarted = ConfiguredMatchFixtures.CreateConfiguredSession();
            var restartedView = new HeroOverviewsUnityView();
            var restartedPresenter = new HeroOverviewsPresenter(restarted, restartedView);
            PlayTurns(restarted, 1);
            ConfiguredMatchFixtures.CommitBothHands(restarted);
            restartedPresenter.Refresh();
            Assert.That(Describe(restartedView), Is.EqualTo(midTurn),
                "The same commands reach the same position, which is what a restart unwinds.");

            Assert.That(restarted.Restart().Accepted, Is.True);
            restartedPresenter.Refresh();
            Assert.That(restartedView.Model.Heroes.All(hero => hero.CurrentCard == null), Is.True);
            Assert.That(
                restartedView.Model.Heroes.SelectMany(hero => hero.TurnSlots)
                    .Any(slot => slot.IsOccupied),
                Is.False);
        }

        /// <summary>
        /// Every region stays inside the viewport it is laid out into, and the standing product rule -
        /// both rails together under half of it - holds, at two extreme shapes.
        ///
        /// <para>Milestone 8.25c: the two pairs below are <em>panel</em> rectangles in reference units
        /// rather than screens in device pixels, and they are the two extreme shapes a window can
        /// resolve to rather than two screens a player might own. Which device resolutions produce
        /// which panel, and that every 16:9 one produces the same layout, is
        /// <c>MatchHudPanelScalingTests</c>'s.</para>
        /// </summary>
        [Test]
        public void TheStandingLayout_KeepsEveryRegionInsideBothValidatedViewports()
        {
            foreach (var viewport in new[] { new Vector2(2048f, 1152f), new Vector2(778f, 300f) })
            {
                var bar = ConfiguredMatchHudRenderer.CalculateStatusBarRect(viewport.x, viewport.y);
                var tray = ConfiguredMatchHudRenderer.CalculateCardPanelRect(viewport.x, viewport.y);
                var playfield = ConfiguredMatchHudRenderer.CalculatePlayfieldRect(viewport.x, viewport.y);
                var red = ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                    viewport.x, viewport.y, TeamSide.Red);
                var blue = ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                    viewport.x, viewport.y, TeamSide.Blue);

                foreach (var rect in new[] { bar, tray, playfield, red, blue })
                {
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f), $"{viewport}");
                    Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f), $"{viewport}");
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(viewport.x + 0.01f), $"{viewport}");
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(viewport.y + 0.01f), $"{viewport}");
                }

                Assert.That(bar.yMax, Is.LessThanOrEqualTo(playfield.yMin + 0.01f));
                Assert.That(playfield.yMax, Is.LessThanOrEqualTo(tray.yMin + 0.01f),
                    "The board keeps the whole middle; nothing overlaps the tray or the bar.");
                Assert.That(red.xMax, Is.LessThan(blue.xMin),
                    "The two rails are on opposite edges and never meet.");
                Assert.That(red.width + blue.width, Is.LessThan(viewport.x / 2f),
                    "Both rails together take less than half the width, so the board stays dominant.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static CardColor ColorOf(MatchSession session, CardDefinitionReference reference) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(reference)).Progression.Color;

        /// <summary>
        /// The authoritative graph behind a session, so a test can read the Active-effect lifecycle the
        /// projection derives its answer from rather than trusting that answer against itself.
        /// </summary>
        private static RestoredAuthoritativeMatch RestoredMatch(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static CardDefinition DefinitionOf(MatchSession session, CardInstanceId cardInstanceId)
        {
            var instance = session.Projection.Cards.Single(card => card.Id.Equals(cardInstanceId));
            return session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(instance.DefinitionReference));
        }

        private static TrayCardFaceViewModel FaceOf(MatchSession session, string displayName)
        {
            var view = new DirectorHandsUnityView();
            new DirectorHandsPresenter(session, view).Refresh();
            return view.Model.ViewedHand.TrayCards
                .Single(card => card.DisplayName == displayName).Face;
        }

        /// <summary>
        /// The WCAG 2.1 contrast ratio between two opaque colours, so "readable without colour alone" is
        /// a measurement rather than an opinion. 4.5 is the standard's normal-text threshold.
        /// </summary>
        private static float Contrast(Color background, Color foreground)
        {
            var first = RelativeLuminance(background);
            var second = RelativeLuminance(foreground);
            return (Mathf.Max(first, second) + 0.05f) / (Mathf.Min(first, second) + 0.05f);
        }

        private static float RelativeLuminance(Color color) =>
            (0.2126f * LinearChannel(color.r)) +
            (0.7152f * LinearChannel(color.g)) +
            (0.0722f * LinearChannel(color.b));

        private static float LinearChannel(float value) =>
            value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);

        private static string NameOf(MatchSession session, CardDefinitionReference reference) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(reference)).DisplayName;


        /// <summary>
        /// Milestone 8.23a: a held marker is drawn on its holder's rail, and every part of the reading
        /// is the definition's rather than this presenter's.
        ///
        /// <para>Driven from a real staged Tigerclaw position that actually plays Poisoned Dagger, so
        /// what the rail is rendered from is the register the rules wrote rather than a marker this
        /// test attached by hand. The reading lands on the rail of the hero who was given it - which
        /// is the enemy, because p.18 gives markers to other heroes and a marker property affects its
        /// HOLDER where a token property affects its controller.</para>
        /// </summary>
        [Test]
        public void AHeldMarker_IsDrawnOnItsHoldersRailFromTheProjectionAlone()
        {
            var board = TigerclawBoard.Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: StagedHeroBoard.At("primitive-hero", StagedHeroBoard.TwoAway));
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(board.Session, view);
            presenter.Refresh();

            Assert.That(view.Model.Heroes.SelectMany(hero => hero.HeldMarkers), Is.Empty,
                "Nothing is held before the Skill is performed, so the row this test reads is one " +
                "this test caused.");

            board.StartPrimary(TigerclawCardContent.PoisonedDagger);
            board.SelectFirst();
            board.AnswerEveryStandingChoice();
            presenter.Refresh();

            // The projection carries what the register holds, which is what "no presentation-owned
            // accounting" means at this seam: the same attachment, read back through a restore.
            var attachment = board.MarkersHeldBy(board.Enemy).Single();
            Assert.That(
                board.Session.Projection.MarkersHeldBy(board.Enemy).Select(marker => marker.Id),
                Is.EqualTo(new[] { attachment.Id }),
                "The projection's held markers are the register's, so a surface reading one is " +
                "reading the authority rather than a copy of it.");

            var holder = view.Model.Heroes.Single(hero => hero.HeroId.Equals(board.Enemy));
            var giver = view.Model.Heroes.Single(hero => !hero.HeroId.Equals(board.Enemy));
            Assert.That(giver.HeldMarkers, Is.Empty,
                "The hero who GAVE the marker is not the hero it affects. A build drawing it here " +
                "would be telling the wrong player their values dropped.");
            Assert.That(holder.HeldMarkers.Count, Is.EqualTo(1),
                "One marker, from the one supply his face names.");

            var marker = holder.HeldMarkers.Single();
            Assert.That(marker.HolderHeroId, Is.EqualTo(board.Enemy));
            Assert.That(marker.NameLabel, Is.EqualTo("POISON"),
                "The marker's own display name, in the rail's caption case.");
            Assert.That(
                marker.ValueChanges.Select(change => change.Channel).ToArray(),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.Initiative,
                    PrintedValueChannel.Attack,
                    PrintedValueChannel.Defense
                }),
                "The three channels the printed sentence names, in the definition's own order.");
            foreach (var change in marker.ValueChanges)
            {
                Assert.That(change.Amount, Is.EqualTo(-1),
                    "Poisoned Dagger's strength is one, and the sign arrives from the definition " +
                    "rather than being applied here.");
                Assert.That(change.AmountLabel, Is.EqualTo("-1"));
            }

            Assert.That(marker.AssistiveLabel,
                Is.EqualTo("Poison: Initiative -1, Attack -1, Defense -1"),
                "and the whole reading is spoken in words for a reader with no pointer.");

            // and it is on the screen rather than only in the model.
            var rail = RailOf(view, board.Enemy);
            var row = Descendants(rail).Single(element => element.name == "gd-hero-marker");
            Assert.That(
                Descendants(row).OfType<Label>().Select(label => label.text).ToArray(),
                Is.EqualTo(new[] { "POISON", "INI", "-1", "ATK", "-1", "DEF", "-1" }),
                "The row a player reads carries the marker and its three losses. Shown: " +
                    Describe(view));
            Assert.That(row.tooltip, Is.EqualTo(marker.AssistiveLabel));
        }

        /// <summary>
        /// Milestone 8.23a: the reading ends with the attachment, leaving nothing behind on any rail.
        ///
        /// <para>Rulebook p.8 returns every marker at the end of every round, so the row must be gone
        /// once the register is empty - and gone from a presenter that was rendered while it was full,
        /// which is the case a presentation-owned cache would fail.</para>
        /// </summary>
        [Test]
        public void AReturnedMarker_LeavesNoReadingBehindOnAnyRail()
        {
            var board = TigerclawBoard.Create(
                tier: 2,
                branch: TigerclawUpgradeBranch.SecondSibling,
                heroPlacements: StagedHeroBoard.At("primitive-hero", StagedHeroBoard.TwoAway));
            var view = new HeroOverviewsUnityView();
            var presenter = new HeroOverviewsPresenter(board.Session, view);

            board.StartPrimary(TigerclawCardContent.PoisonedDagger);
            board.SelectFirst();
            board.AnswerEveryStandingChoice();
            presenter.Refresh();
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-hero-marker"),
                Is.EqualTo(1),
                "The row has to be there before its absence proves anything.");

            board.RoundTripThroughASnapshot();
            presenter.Refresh();
            Assert.That(
                Descendants(view.Root).Count(element => element.name == "gd-hero-marker"),
                Is.EqualTo(1),
                "A restored register redraws the same one row rather than none or two.");

            board.CrossTheRoundBoundary();
            presenter.Refresh();

            Assert.That(board.MarkersHeldBy(board.Enemy), Is.Empty,
                "p.8's Remove Tokens, Return Markers is what ends the attachment.");
            Assert.That(view.Model.Heroes.SelectMany(hero => hero.HeldMarkers), Is.Empty,
                "and no rail's model still claims one.");
            Assert.That(
                Descendants(view.Root).Any(element => element.name == "gd-hero-marker"),
                Is.False,
                "and nothing is left on the screen. Shown: " + Describe(view));
        }

        /// <summary>The rail element drawn for one hero, found by the model it was rendered from.</summary>
        private static VisualElement RailOf(HeroOverviewsUnityView view, HeroId heroId)
        {
            var index = view.Model.Heroes
                .Select((hero, position) => new { hero, position })
                .Single(entry => entry.hero.HeroId.Equals(heroId))
                .position;
            var rails = Descendants(view.Root)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails.Length, Is.EqualTo(view.Model.Heroes.Count),
                "One rail per projected hero.");
            return rails[index];
        }
        private static string Describe(DirectorHandsUnityView view) =>
            string.Join("|", view.Model.Hands.SelectMany(hand => hand.TrayCards.Select(card =>
                $"{hand.HeroId}:{card.CardInstanceId}:{card.Color}:{card.Group}:{card.PlayedTurnIndex}")));

        private static string Describe(HeroOverviewsUnityView view) =>
            string.Join("|", view.Model.Heroes.Select(hero =>
                $"{hero.HeroId}:{hero.Level}:{hero.DiscardCount}:" +
                // Milestone 8.21a: the coin balance joins it for milestone 8.7a's own reason - a
                // reconstruction path is then compared on the reading this milestone introduced
                // rather than only on the facts the earlier ones already agreed about.
                $"{hero.Coins}:" +
                // Milestone 8.7a: occupancy and the current-card marker join the description, so the
                // reconstruction paths are compared on the split this milestone introduced rather than
                // only on the four colours they always agreed about.
                $"{hero.CurrentCard?.StateLabel ?? "-"}:" +
                $"{hero.CurrentCard?.Card?.CardInstanceId.Value ?? "-"}:" +
                string.Join(",", hero.TurnSlots.Select(slot =>
                    $"{slot.TurnIndex}{slot.Color}{(slot.IsOccupied ? "+" : "-")}"))));

        private static IEnumerable<VisualElement> Descendants(VisualElement element)
        {
            yield return element;
            foreach (var child in element.Children())
            {
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        private static void PlayTurns(MatchSession session, int turns)
        {
            for (var index = 0; index < turns; index++)
            {
                ConfiguredMatchFixtures.CommitAndHoldOneTurn(session);
            }
        }

        /// <summary>
        /// Rulebook p.8's sidebar Place, paused, at the board seam.
        ///
        /// It is built here rather than through a session for the reason
        /// <see cref="LanePushTransitionTests"/> records: the obstacle has to stand on a Spawn point of
        /// the new Battle Zone, and an initial scenario cannot put one there without moving every pinned
        /// digest. What this fixture is for is the description and the labels built from it, and both
        /// are the same objects a session would carry.
        /// </summary>
        internal static class LanePushPlacePause
        {
            private static readonly ZoneId BlueApproach = new ZoneId("blue-approach");

            public static Fixture Create()
            {
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    StagedBoardParticipants.Create(),
                    map,
                    new TieBreakerState(TeamSide.Red));
                var blocked = map.SpawnPoints
                    .Where(spawn => spawn.UnitType.EndsWith("-minion", StringComparison.Ordinal))
                    .Select(spawn => new
                    {
                        Spawn = spawn,
                        Space = map.Spaces.Single(space => space.Id.Equals(spawn.SpaceId))
                    })
                    .Where(value => value.Space.ZoneId.HasValue &&
                        value.Space.ZoneId.Value.Equals(BlueApproach) &&
                        string.Equals(value.Spawn.TeamId, "red", StringComparison.Ordinal))
                    .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                    .First();
                var board = new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                    map,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    new[]
                    {
                        new GuardsDigital.Core.Obstacles.ObstacleOccupancy(
                            new GuardsDigital.Core.Obstacles.ObstacleId("hud-lane-push-block"),
                            blocked.Space.Coordinate)
                    },
                    setup.Minions);
                var lanePush = new GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority(board, 3);
                var transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(lanePush, null));
                var consumption = new GuardsDigital.Core.Rules.LanePush.LanePushConsumption(
                    lanePush, transitions);
                var result = consumption.TryConsume(
                    new LanePushRequestId("hud-lane-push"),
                    TeamSide.Red,
                    TeamSide.Blue,
                    new TieBreakerState(TeamSide.Red));
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                Assert.That(result.IsPaused, Is.True,
                    "An obstacle on a Spawn point of the new Battle Zone is what p.8 asks about.");
                return new Fixture(transitions);
            }

            public sealed class Fixture
            {
                public Fixture(SystemTransitionAuthority transitions)
                {
                    Transitions = transitions;
                }

                public SystemTransitionAuthority Transitions { get; }
            }
        }

        /// <summary>
        /// Milestone 8.21. A standing removal-prevention decision, as the projection carries it and as
        /// the region draws it.
        ///
        /// <para>The decision is composed from the typed pieces rather than driven out of a live
        /// match, for the reason the Lane-Push pause beside it is: what these surfaces are checked
        /// against is the authority's own offer set, and reaching this pause through a played card
        /// would make the case about the card. Brogan's Bolster supplies the price so the shape is a
        /// printed one - <see cref="BroganRemovalPreventionTests"/> is what proves his faces reach it
        /// in a real match.</para>
        /// </summary>
        internal static class RemovalPreventionPause
        {
            private const string SourceId = "brogan-bolster-effect:probe";

            public static RemovalPreventionPrompt CreatePrompt()
            {
                var deciding = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                var hand = BroganCardContent.CreateHand(deciding, "blue");
                var payable = hand.Instances
                    .Where(card => BroganCardContent
                        .Definition(card.DefinitionReference.DefinitionId)
                        .Progression.IsBasic)
                    .Select(card => card.Id)
                    .ToArray();
                Assert.That(payable, Is.Not.Empty,
                    "p.6 deals Brogan two Basic cards, and a pause nobody can pay would check " +
                    "nothing about the controls a payment produces.");

                return RemovalPreventionPrompt.Create(
                    Decision(deciding),
                    new[]
                    {
                        new RemovalPreventionOption(
                            SourceId,
                            deciding,
                            RemovalPreventionCost.DiscardOwnedCard(
                                CardLocation.Hand,
                                admittedClassification: CardClassification.Basic),
                            payable)
                    });
            }

            /// <summary>
            /// The prompt as the contextual region draws it. Each payment carries no tray face here,
            /// because this fixture composes the pause rather than playing a match and the card faces
            /// belong to a projection: what the reachability case measures is one control per offer,
            /// and <see cref="RoundActionUnityView"/> draws a card-valued invocation the same way
            /// whether the face is present or not.
            /// </summary>
            public static RemovalPreventionPromptViewModel CreateViewModel(
                RemovalPreventionPrompt prompt) =>
                new RemovalPreventionPromptViewModel(
                    prompt.SubjectMinionId,
                    prompt.Payments.Select(payment => new TypedInvocationViewModel(
                        payment.PaymentCardInstanceId.Value,
                        DirectorCommand.AnswerRemovalPrevention(payment.Invocation))),
                    new TypedInvocationViewModel(
                        RemovalPreventionPromptLabels.DeclineLabel,
                        DirectorCommand.AnswerRemovalPrevention(prompt.DeclineInvocation)));

            private static readonly MinionId Subject = new MinionId("brogan-guard-probe-minion");

            private static PendingRemovalPreventionDecision Decision(HeroId deciding)
            {
                // p.13's Defeat has already been recorded and its reward already paid by the time this
                // decision stands, which is why the outcome carries amounts and the operations are the
                // ones the ledger performed.
                var outcome = new DefeatOutcome(
                    new DefeatOutcomeId("brogan-guard-probe:minion-defeat"),
                    new DefeatTargetId(Subject.Value),
                    new TeamId("brogan-guard-probe-team"),
                    deciding,
                    2,
                    0);

                return new PendingRemovalPreventionDecision(
                    RemovalPreventionOrigin.AttackMinionDefeat,
                    new PendingAttackResolution(
                        new ActionResolutionId("brogan-guard-probe-resolution"),
                        new CardInstanceId("brogan-guard-probe-card"),
                        new CardDefinitionReference(
                            new CardDefinitionId("brogan-guard-probe-definition"), "probe-rev"),
                        new ActionDefinitionId("brogan-guard-probe-action"),
                        deciding,
                        ActionInvocationContext.PrintedPrimary,
                        new ActionResolutionStepId("minion-defeat"),
                        AttackResolutionPhase.ResolveMinionDefeat,
                        AttackTarget.Minion(Subject),
                        null),
                    Subject,
                    outcome,
                    new[]
                    {
                        new CoinResourceOperation(
                            outcome.Id,
                            new DefeatTargetId(Subject.Value),
                            deciding,
                            new TeamId("brogan-guard-probe-team"),
                            CoinAwardKind.Defeat,
                            2,
                            0,
                            2)
                    },
                    deciding,
                    GuardsDigital.Core.Rules.Modifications.RuleActivity.Attack);
            }
        }
    }
}

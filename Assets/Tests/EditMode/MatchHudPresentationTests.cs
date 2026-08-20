using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
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
                Assert.That(card.ValueLine, Does.Contain("INIT"),
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
                Assert.That(hero.TurnSlots[0].AssistiveLabel, Does.Contain("Turn 1"));
                Assert.That(hero.TurnSlots[0].AssistiveLabel,
                    Does.Contain(MatchHudTheme.ColorName(hero.TurnSlots[0].Color)),
                    "The ordinal and the colour are readable, and only from the assistive label.");
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
            Assert.That(hero.DiscardCountLabel, Is.EqualTo(pile.Length.ToString()));
            Assert.That(hero.DiscardColors,
                Is.EquivalentTo(pile
                    .Select(card => CurrentRoundCards.ColorOf(session.Projection, card))
                    .Distinct()));
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

            var prompt = RoundActionPresenter.CreateSystemTransitionPrompt(
                description,
                Array.Empty<CardDefinition>(),
                Array.Empty<QuickGameHeroSetupState>());
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

        private static string NameOf(MatchSession session, CardDefinitionReference reference) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(reference)).DisplayName;

        private static string Describe(DirectorHandsUnityView view) =>
            string.Join("|", view.Model.Hands.SelectMany(hand => hand.TrayCards.Select(card =>
                $"{hand.HeroId}:{card.CardInstanceId}:{card.Color}:{card.Group}:{card.PlayedTurnIndex}")));

        private static string Describe(HeroOverviewsUnityView view) =>
            string.Join("|", view.Model.Heroes.Select(hero =>
                $"{hero.HeroId}:{hero.Level}:{hero.DiscardCount}:" +
                string.Join(",", hero.TurnSlots.Select(slot => $"{slot.TurnIndex}{slot.Color}"))));

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
        private static class LanePushPlacePause
        {
            private static readonly ZoneId BlueApproach = new ZoneId("blue-approach");

            public static Fixture Create()
            {
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    PlaceholderOneVsOneParticipants.Create(),
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
    }
}

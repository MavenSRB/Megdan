using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.46c. What the decision window says, for whom, and in which of its two states.
    ///
    /// <para><b>What these cases are for.</b> On 2026-09-18 the user reported that playtesters did not
    /// understand what they were choosing and often did not see the controls at all, because the
    /// round-action strip reads as STATUS rather than as a QUESTION. The window is the replacement.
    /// Every case here drives real configured content to a real pause and reads what a player would
    /// actually meet: the question, the count, the cards, the controls and the small corner answer -
    /// and, just as importantly, that no window opens at all for a seat that does not own the standing
    /// decision.</para>
    ///
    /// <para>The state is asserted per family rather than per case, because the state is what the user
    /// settled row by row: a button-answered decision opens EXPANDED over the board, a board-answered
    /// one opens DOCKED beside the hand, and the toggle survives exactly one decision.</para>
    /// </summary>
    public sealed class DecisionWindowPresenterTests
    {
        // -------------------------------------------------------------------------------------
        // Rulebook p.11's Defense - expanded, the attacker's card beside the offer
        // -------------------------------------------------------------------------------------

        [Test]
        public void ADefenseResponse_OpensExpandedWithTheAttackersCardAndEveryCandidatesPrintedValue()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var choice = session.Projection.PendingDefenseChoice;
            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);

            Assert.That(model, Is.Not.Null, "The defender owns the standing decision.");
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.Defense));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded),
                "A Defense is answered by pressing a card, so it opens over the board.");
            Assert.That(model.Title, Does.StartWith("DEFEND AGAINST "),
                "The title asks the question in the player's own words. " + Describe(model));
            Assert.That(model.Question, Is.EqualTo("Choose a card to defend with, or take the hit."));

            Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                "The attacker's own played card is shown beside the offer rather than summarised.");
            Assert.That(model.ContextCards[0].Caption,
                Is.EqualTo("ATTACK " + choice.AttackTotal));

            Assert.That(model.Controls, Has.Count.EqualTo(choice.LegalDefenseOffers.Count),
                "One control per legal Defense offer, and no more. " + Describe(model));
            Assert.That(model.Controls.Select(control => control.Card), Has.All.Not.Null,
                "Every Defense answer is the whole printed face of the card that would pay it.");
            Assert.That(
                model.Controls.All(control =>
                    control.ValueChip.StartsWith("DEFENSE ") || control.ValueChip.StartsWith("BLOCK")),
                Is.True,
                "and each one states the printed Defense it brings, or - for p.14's Block, which " +
                "prints none - what the Block does. " + Describe(model));
            Assert.That(model.Controls.Select(control => control.Command), Has.All.Not.Null,
                "Each carries the exact command the standing contract published.");

            Assert.That(model.CornerControl, Is.Not.Null);
            Assert.That(model.CornerControl.Label,
                Is.EqualTo(DecisionWindowPresenter.TakeTheHitLabel),
                "Declining is small, in a corner, and named for what it costs rather than for the " +
                "mechanism it refuses.");
            Assert.That(model.CornerControl.Command.Kind,
                Is.EqualTo(DirectorCommandKind.DeclineDefense));
        }

        // -------------------------------------------------------------------------------------
        // Ownership: the one seat that answers, everybody else, and the Director
        // -------------------------------------------------------------------------------------

        [Test]
        public void AStandingDecision_OpensForItsOwnerAlone_AndNamesTheHeroOnTheDirector()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var projection = session.Projection;
            var defender = projection.PendingDefenseChoice.ChooserId;
            var somebodyElse = projection.Setup.Heroes
                .Select(hero => hero.HeroId)
                .First(heroId => !heroId.Equals(defender));

            Assert.That(
                DecisionWindowPresenter.CreateViewModel(projection, defender), Is.Not.Null,
                "The seat being asked gets the window.");
            Assert.That(
                DecisionWindowPresenter.CreateViewModel(projection, somebodyElse), Is.Null,
                "Every other seat gets no window at all and keeps the narration line - which is the " +
                "whole reason this presenter reads DecisionOwner rather than the presence of a " +
                "pending field.");

            // `default(HeroId)` spelled out: the undefined hero is the DIRECTOR'S identity, and the
            // Director legitimately owns every decision at once.
            var director = DecisionWindowPresenter.CreateViewModel(projection, default(HeroId));
            Assert.That(director, Is.Not.Null, "The Director answers for everybody.");
            var defenderName = projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(defender)).DisplayName.ToUpperInvariant();
            Assert.That(director.Title, Does.Contain(defenderName),
                "and its title says WHOSE decision this is, because one Director screen carries " +
                "every seat's questions at once. " + Describe(director));
        }

        [Test]
        public void NoStandingDecision_DrawsNoWindowAtAll()
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentConfiguredSession();
            Assert.That(session.Projection.DecisionOwner.IsDefined, Is.False,
                "This fixture must be standing on nothing, or the case proves nothing.");
            Assert.That(
                DecisionWindowPresenter.CreateViewModel(session.Projection, default(HeroId)),
                Is.Null,
                "A window that opened with no question in it would be the empty box the user had " +
                "already asked to be killed.");
        }

        // -------------------------------------------------------------------------------------
        // Rulebook p.10's card chooser - expanded, and answered in one press
        // -------------------------------------------------------------------------------------

        [Test]
        public void ACardValuedChoice_IsExpandedWithEveryOfferedFace_AndNoDoneToPressAfterwards()
        {
            var session = CreateSessionAwaitingTheDiscardChooser();
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                new[] { choice.MinimumSelections, choice.MaximumSelections },
                Is.EqualTo(new[] { 1, 1 }),
                "p.10's discard names exactly one card, which is the rule this case is about.");
            // Milestone 8.46b: an exactly-one choice resolves on its selection, so the authority never
            // publishes a confirm for it - before a selection there is nothing to confirm, and after
            // one there is no pause left. The window has nothing to decline to draw.
            Assert.That(choice.ConfirmInvocation, Is.Null,
                "An exactly-one choice never stands on a confirm once it answers in one press.");

            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.CardChoice));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded));
            Assert.That(model.Controls.Select(control => control.Card), Has.All.Not.Null,
                "A choice about cards is a choice among the cards themselves. " + Describe(model));
            Assert.That(model.Controls, Has.Count.EqualTo(choice.Candidates.Count));
            Assert.That(
                model.Controls.Any(control =>
                    control.Role == DecisionWindowControlRole.Confirm), Is.False,
                "An exactly-one choice answers on its own press - the presenter follows the select " +
                "with the confirm the authority itself offered - so asking the player to say the " +
                "same thing twice is exactly what this window removes. " + Describe(model));
        }

        /// <summary>
        /// Kinetic Blast's `up to 2 enemy units adjacent to you`: a choice of SEVERAL whose candidates
        /// are pieces standing on the board. It is the case that holds both halves of the user's rule
        /// at once - a board-answered decision opens DOCKED and offers no candidate control anywhere,
        /// and a choice that may really be unfinished keeps its DONE.
        /// </summary>
        [Test]
        public void ABoardAnsweredChoiceOfSeveral_IsDockedWithNoCandidateControls_AndKeepsItsDone()
        {
            // Milestone 8.46e. The mirrored one-versus-one board offers exactly ONE adjacent enemy, and
            // the authority narrows `up to 2` to the candidates it can offer - so the first Unity run
            // of this case asserted a bound of two over a choice already narrowed to one. Two Blue
            // units beside Wasp is what makes this a choice of several at all.
            var session = ConfiguredMatchFixtures.CreateKineticSessionWithTwoAdjacentEnemies(
                WaspCardContent.KineticBlast);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
            StartPrimary(session, ActionKind.Skill);

            var opening = session.Projection.PendingEffectChoice;
            Assert.That(opening.MaximumSelections, Is.EqualTo(2),
                "`up to 2` is the bound this case is about.");
            // One target named, so the selection is genuinely unfinished and a confirm stands.
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.AnswerEffectChoice(opening.SelectInvocations[0]));

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.ConfirmInvocation, Is.Not.Null,
                "A half-filled `up to 2` is confirmable, or there is no DONE to assert.");

            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.PieceOrSpace));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Docked),
                "The pieces are clicked where they stand, so the window is the slim bar and never " +
                "covers them.");
            Assert.That(model.Controls.Select(control => control.Role),
                Is.EqualTo(new[] { DecisionWindowControlRole.Confirm }),
                "The only control a board-answered choice carries is the one that ENDS it - the " +
                "docked bar's single verb. " + Describe(model));
            Assert.That(model.Controls[0].Label, Is.EqualTo(EffectChoiceLabels.CompletionLabel));
            Assert.That(model.Count, Is.EqualTo("1 of 2 chosen"),
                "and the bar runs the count where a bound applies. " + Describe(model));
        }

        [Test]
        public void APrintedNumberChoice_IsExpandedWithTheSourceCardAndThePrintedNumbers()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
            StartPrimary(session, ActionKind.Skill);
            AnswerFirstSelection(session);

            var choice = session.Projection.PendingEffectChoice;
            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.Number));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded));
            Assert.That(model.Controls.Select(control => control.Label),
                Is.EqualTo(new[] { "3", "4" }),
                "The printed numbers are the controls, exactly as milestone 8.15 named them. " +
                Describe(model));
            Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                "and the card whose printed text is asking stands beside them, which is the whole " +
                "of what the strip could never show.");
        }

        [Test]
        public void APrintedMay_IsExpandedWithTwoControls_NamedByTheDeclaredConsequence()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
            StartPrimary(session, ActionKind.Skill);
            AnswerFirstSelection(session);
            AnswerFirstSelection(session);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "`May repeat once` is p.10's confirm-or-decline and nothing else.");

            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.OptionalStep));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded));
            Assert.That(model.CornerControl, Is.Null,
                "An optional step's decline IS one of its two answers, so it stands beside the " +
                "accept rather than small in a corner. " + Describe(model));
            // Milestone 8.46a declared both consequences for Lift Up's `may repeat once`; the window
            // reads them through the catalog and upper-cases them, the way every HUD control is set.
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(WaspCardContent.LiftUp, choice.NodeId, out var declared),
                Is.True, "Lift Up's repeat carries a declared label after 8.46a.");
            Assert.That(model.Controls.Select(control => control.Label),
                Is.EqualTo(new[] { declared.Chosen.ToUpperInvariant(), declared.Declined.ToUpperInvariant() }),
                "The two answers say what accepting and declining DO, in the card's own words. " +
                Describe(model));
            Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                "The card asking the question is shown, which is what makes the two answers " +
                "readable at all.");
        }

        // -------------------------------------------------------------------------------------
        // The two decisions that already resolve on the click: docked, question only
        // -------------------------------------------------------------------------------------

        [Test]
        public void ADisplacedMinionCorrection_IsTheDockedQuestionWithNoControlsAtAll()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 1200, projection => projection.PendingDisplacedMinionCorrection != null, seed: 29);
            var projection = host.Projection;
            Assert.That(projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "The seeded walk must reach a correction, or this case proves nothing.");

            var owner = projection.DecisionOwner;
            var onTheSide = projection.Setup.Heroes.First(hero =>
                owner.IsTeamOwned ? hero.TeamSide == owner.TeamSide : owner.HeroIds.Contains(hero.HeroId));
            var model = DecisionWindowPresenter.CreateViewModel(projection, onTheSide.HeroId);

            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.Correction));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Docked));
            Assert.That(model.Controls, Is.Empty,
                "A correction resolves on the click, so the window asks and offers nothing. " +
                Describe(model));
            Assert.That(model.CornerControl, Is.Null);
            Assert.That(model.Question, Does.StartWith("Click "), Describe(model));
        }

        [Test]
        public void AHeroRespawn_IsTheDockedQuestionWithTheResolveWithoutControlInItsCorner()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 2200, projection => projection.PendingHeroRespawnChoice != null, seed: 29);
            var projection = host.Projection;
            Assert.That(projection.PendingHeroRespawnChoice, Is.Not.Null,
                "The seeded walk must reach a respawn, or this case proves nothing.");

            var model = DecisionWindowPresenter.CreateViewModel(
                projection, projection.PendingHeroRespawnChoice.HeroId);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.HeroRespawn));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Docked),
                "Every destination is a Spawn space on the board, so the question is docked beside " +
                "the hand rather than drawn over the spaces it is about.");
            Assert.That(model.Controls, Is.Empty, Describe(model));
            Assert.That(model.CornerControl, Is.Not.Null);
            Assert.That(model.CornerControl.Label,
                Is.EqualTo(DecisionWindowPresenter.DontRespawnLabel),
                "Declining to come back has no space on the board, so it stays a control - named " +
                "for what it does rather than for the command behind it.");
            Assert.That(model.CornerControl.Command.Kind,
                Is.EqualTo(DirectorCommandKind.ResolveCardWithoutRespawn));
        }

        [Test]
        public void AModeControl_KeepsTodaysLabelWhileTheDeclaredCatalogAnswersNothing()
        {
            var session = CreateSessionAwaitingTheDiscardChooser();
            var choice = session.Projection.PendingEffectChoice;
            var model = DecisionWindowPresenter.CreateViewModel(session.Projection, choice.ChooserId);

            // Milestone 8.46a fills DecisionLabelCatalog; until it does, every lookup answers false
            // and the window must still be able to name every control it draws. The card-valued
            // candidates here are named as the cards they give up, which is the fallback for this
            // family, and nothing anywhere reads an option identity as though it were a label.
            Assert.That(
                model.Controls.Where(control => control.Card != null)
                    .Select(control => control.Label),
                Is.EquivalentTo(session.Projection.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => CardName(session.Projection, candidate.Value.CardInstanceId))),
                "The fallback is today's label, not an ordinal. " + Describe(model));
            Assert.That(model.Controls.Select(control => control.Label),
                Has.All.Not.Null.And.All.Not.Empty);
        }

        // -------------------------------------------------------------------------------------
        // The two team-owned families the seeded six-player walk reaches
        // -------------------------------------------------------------------------------------

        [Test]
        public void ATiedInitiative_IsExpandedWithOneButtonPerTiedCard_AndBelongsToTheWholeSide()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 400,
                projection => projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative ||
                    projection.PendingInitiativeChoice != null);
            var projection = host.Projection;
            Assert.That(
                projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative ||
                projection.PendingInitiativeChoice != null, Is.True,
                "The seeded walk must reach a tied Initiative, or this case proves nothing.");

            var owner = projection.DecisionOwner;
            var onTheSide = projection.Setup.Heroes.First(hero =>
                owner.IsTeamOwned ? hero.TeamSide == owner.TeamSide : owner.HeroIds.Contains(hero.HeroId));
            var model = DecisionWindowPresenter.CreateViewModel(projection, onTheSide.HeroId);

            Assert.That(model, Is.Not.Null,
                "A tied Initiative is owned by every seat on the side, not by one of them.");
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.TiedInitiative));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded));
            Assert.That(model.Title, Does.StartWith("WHO ACTS FIRST?"));
            Assert.That(model.Controls, Is.Not.Empty, Describe(model));
            Assert.That(model.Controls.Select(control => control.Label),
                Has.All.EndsWith(" ACTS FIRST"),
                "Each button names the hero it would put first, rather than the card it is. " +
                Describe(model));
            Assert.That(model.Controls.Select(control => control.Card), Has.All.Not.Null,
                "and shows the tied card itself.");
        }

        [Test]
        public void AMinionBattleRemoval_IsTheDockedBarWithItsCount_AndOneVerbOnlyWhenTheCountIsMet()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 1500,
                projection => projection.PendingMinionBattleChoice != null ||
                    projection.TeamAgreement?.Kind == TeamAgreementKind.MinionBattle, seed: 29);
            var projection = host.Projection;
            Assert.That(
                projection.PendingMinionBattleChoice != null ||
                projection.TeamAgreement?.Kind == TeamAgreementKind.MinionBattle, Is.True,
                "The seeded walk must reach rulebook p.8's Minion Battle, or this case proves nothing.");

            var owner = projection.DecisionOwner;
            var onTheSide = projection.Setup.Heroes.First(hero =>
                owner.IsTeamOwned ? hero.TeamSide == owner.TeamSide : owner.HeroIds.Contains(hero.HeroId));
            var model = DecisionWindowPresenter.CreateViewModel(projection, onTheSide.HeroId);

            Assert.That(model, Is.Not.Null);
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.MinionBattleRemoval));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Docked),
                "The minions are clicked on the board, so the window is the slim bar beside the hand " +
                "and never covers the pieces the question is about.");
            Assert.That(model.Title, Does.StartWith("REMOVE "));
            Assert.That(model.Count, Does.Contain(" of ").And.Contain(" chosen"), Describe(model));
            Assert.That(model.Controls.Count, Is.LessThanOrEqualTo(1),
                "At most one verb stands on the docked bar. " + Describe(model));
            Assert.That(
                model.Controls.All(control =>
                    control.Label == DecisionWindowPresenter.RemoveTheseMinionsLabel), Is.True,
                "and the verb is the single ratification the user settled, on a solo side and a team " +
                "alike. " + Describe(model));
            if (model.CornerControl != null)
            {
                Assert.That(model.CornerControl.Label, Is.EqualTo(DecisionWindowPresenter.NotYetLabel),
                    "Withdraw My Agreement survives as NOT YET - the one take-back the seat undo " +
                    "cannot replace.");
            }
        }

        // -------------------------------------------------------------------------------------
        // The state is re-decided per decision; a manual toggle survives exactly one
        // -------------------------------------------------------------------------------------

        [Test]
        public void AManualToggle_SurvivesItsOwnDecisionAndIsDroppedByTheNextOne()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var defender = session.Projection.PendingDefenseChoice.ChooserId;
            var client = new DirectorMatchClient(session);
            var view = new CapturingDecisionWindowView();
            var presenter = new DecisionWindowPresenter(client, view, defender);

            presenter.Refresh();
            Assert.That(view.Model.State, Is.EqualTo(DecisionWindowState.Expanded),
                "The family decides the state when a decision first stands.");
            var key = view.Model.DecisionKey;

            Assert.That(presenter.ToggleState(), Is.True);
            Assert.That(view.Model.State, Is.EqualTo(DecisionWindowState.Docked),
                "A minimise is a reading preference, and it applies at once.");
            presenter.Refresh();
            Assert.That(view.Model.State, Is.EqualTo(DecisionWindowState.Docked),
                "and it survives for as long as the SAME decision stands. " + Describe(view.Model));
            Assert.That(view.Model.DecisionKey, Is.EqualTo(key));

            // The same decision answered; the next one re-decides from its own shape rather than
            // remembering what the last one was toggled to.
            var decline = session.Projection.DecisionContract.Commands.Single(command =>
                command.Kind == DirectorCommandKind.DeclineDefense);
            Assert.That(session.Execute(decline).Accepted, Is.True);
            presenter.Refresh();
            Assert.That(
                view.Model == null || view.Model.DecisionKey != key, Is.True,
                "The answered decision is gone, so nothing may still be carrying its toggle.");
        }

        // -------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.10's discard chooser, reached through Shock's rider exactly as
        /// <c>EffectChoiceStatusNamingTests</c> and <c>ConfiguredAttackCardTests</c> reach it.
        /// </summary>
        private static MatchSession CreateSessionAwaitingTheDiscardChooser()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var red = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Red &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Shock));
            var blue = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionOf(session, offer).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, offer).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, minionId));

            // The rider's first question names a hero; its second is the chosen hero's own hand.
            // Milestone 8.46b: aimed at the question the selection went into, never at whatever
            // stands next.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, session.Projection.PendingEffectChoice.SelectInvocations[0]);

            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The card-valued pause must be standing, or every case built on this holds over " +
                "nothing.");
            return session;
        }

        private static ActionDefinition ActionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static string CardName(IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardInstanceId));
            return projection.Definitions
                .Single(value => value.Reference.Equals(card.DefinitionReference))
                .DisplayName.ToUpperInvariant();
        }

        private static void CommitAgainstStopProjectiles(
            MatchSession session, CardDefinitionId cardId)
        {
            var red = HandCard(session, TeamSide.Red, cardId);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The tested card has to be the one acting, or every assertion below is about Blue.");
        }

        private static void StartPrimary(MatchSession session, ActionKind kind)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == kind);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        // Milestone 8.46b: a completion is aimed at the choice its selection went into. The unaimed
        // form this replaced would have accepted Lift Up's `may repeat once` the moment it stood.
        private static void AnswerFirstSelection(MatchSession session) =>
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session, session.Projection.PendingEffectChoice.SelectInvocations[0]);

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        private static string Describe(DecisionWindowViewModel model) =>
            model == null
                ? "(no window)"
                : $"[{model.Family}/{model.State}] \"{model.Title}\" / \"{model.Question}\" / " +
                    $"count={model.Count ?? "-"} controls=" +
                    string.Join(", ", model.Controls.Select(control =>
                        control.Role + ":'" + control.Label + "'")) +
                    " corner=" + (model.CornerControl?.Label ?? "-");

        private sealed class CapturingDecisionWindowView : IDecisionWindowView
        {
            public void Render(DecisionWindowViewModel model) => Model = model;

            public DecisionWindowViewModel Model { get; private set; }
        }
    }
}

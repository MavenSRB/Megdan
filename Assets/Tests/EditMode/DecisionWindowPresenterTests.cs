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
            Assert.That(model.Question,
                Is.EqualTo("Discard a card for its printed defense, or take the hit."),
                "Milestone 8.58: the mockup's own sentence, which says what the answer COSTS.");

            Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                "The attacker's own played card is shown beside the offer rather than summarised.");
            Assert.That(model.ContextCards[0].Card, Is.Not.Null,
                "and it is a real printed face, which is what `gd-decision-attack-card` draws.");
            Assert.That(model.ContextCards[0].Caption,
                Does.StartWith("ATTACK " + choice.AttackTotal),
                "The chip names the composed attack total the p.14 preview already published. " +
                Describe(model));

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

            // Milestone 8.58, the user on the shipped window: "This is not showing the attack card,
            // nor our defense breakdown popup". The breakdown is the AUTHORITY'S OWN preview, read
            // card by card and never re-derived from the two numbers here.
            var preview = session.Projection.PendingDefensePreview;
            foreach (var control in model.Controls)
            {
                var offer = preview.Choices.Single(value =>
                    value.CardInstanceId.HasValue &&
                    value.CardInstanceId.Value.Equals(control.Card.CardInstanceId));
                if (offer.DefenseTotal.HasValue)
                {
                    Assert.That(control.ValueChip,
                        Does.StartWith("DEFENSE " + offer.DefenseTotal.Value),
                        "A candidate's chip is its previewed Defense. " + Describe(model));
                }

                var holds = offer.Outcome == AttackResolutionOutcome.SuccessfullyDefended;
                Assert.That(control.ValueChip.Contains("HOLDS"), Is.EqualTo(holds),
                    "`HOLDS` appears exactly where the authority's preview says this card would stop " +
                    $"the attack ('{control.ValueChip}', outcome {offer.Outcome}). " + Describe(model));
                Assert.That(control.IsPrimary, Is.EqualTo(holds),
                    "and that candidate is the emphasised one.");
            }

            // Milestone 8.58, the user's LAST word on the arithmetic they had asked for one round
            // earlier: "Remove it and keep the old mouseover when mousing over the defense cards."
            // The window carries the chips and the BREAKDOWN OBJECT, and prints no section of its own.
            Assert.That(model.Statements, Is.Empty,
                "No modifiers section is printed in the panel. " + Describe(model));
            foreach (var control in model.Controls)
            {
                Assert.That(control.DefenseBreakdown, Is.Not.Null,
                    "Every candidate carries the hover breakdown the strip's own offer opened, so the " +
                    "popup did not move away with the question. " + Describe(model));
                Assert.That(control.DefenseBreakdown.CardInstanceId.HasValue, Is.True);
                Assert.That(
                    control.DefenseBreakdown.CardInstanceId.Value.Equals(
                        control.Card.CardInstanceId),
                    Is.True,
                    "and it is THAT card's breakdown.");
                Assert.That(control.DefenseBreakdown.AttackTotal,
                    Is.EqualTo(choice.AttackTotal),
                    "read off the same standing preview the chips are read from.");
            }

            Assert.That(model.CornerControl, Is.Not.Null);
            Assert.That(model.CornerControl.Label,
                Is.EqualTo(DecisionWindowPresenter.TakeTheHitLabel),
                "Declining is small, in a corner, and named for what it costs rather than for the " +
                "mechanism it refuses.");
            Assert.That(model.CornerControl.Command.Kind,
                Is.EqualTo(DirectorCommandKind.DeclineDefense));
        }

        /// <summary>
        /// THE SHIPPED DEFECT milestone 8.80 closes, reported by the user off the 8.79 playtest:
        /// "Defense window does not show attacking card anymroe", from a JOINED seat.
        /// </summary>
        /// <remarks>
        /// <para>The case above runs on a Director session, where <c>PendingAttackResolution</c> is
        /// a whole object - which is exactly why it stayed green while every network seat drew the
        /// candidates with nothing above them. The host does send the defender the frame (it owns the
        /// standing decision, and <c>SeatProjection</c>'s <c>ownsDecision || isActor</c> hands the
        /// 8.40f-narrowed frame to both), but <c>NetworkMatchClient</c> returns <c>null</c> for it
        /// while <c>HasPendingAttackResolution</c> reads the row that arrived - so the window's old
        /// <c>Has ? Pending : null</c> evaluated to null on every joined seat and on the hosting
        /// player's own seat, which reads through the same client. This case composes the same staged
        /// Defense, puts it on a REAL wire, and reads the window off the DEFENDER'S OWN network
        /// projection.</para>
        ///
        /// <para>Observed red on main at <c>4139446f</c> before the fix, with the frame still
        /// supplying the card: "The attacker's card is beside the offer on a joined seat too - this
        /// is the whole of milestone 8.80. Expected: 1 But was: 0".</para>
        /// </remarks>
        [Test]
        public void ADefenseResponseOnAJoinedSeat_DrawsTheAttackersCardFromTheChoiceItself()
        {
            var harness = PrivateOnlineWireHarness.Compose(
                new MatchHost(ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()));
            harness.SeatAll();

            var host = harness.MatchHost;
            var defenderSeat = host.Seats.Seats.Single(seat =>
                host.ProjectionForSeat(seat.Id).PendingDefenseChoice != null);
            var adapter = new NetworkMatchClient(
                harness.Client(harness.OrdinalOf(defenderSeat.Id.Value)),
                host.Projection,
                MatchContentCatalogs.ForThisBuild());
            var seatProjection = adapter.Projection;

            var choice = seatProjection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null,
                "The question itself has always reached the defender; only the attack did not.");
            Assert.That(seatProjection.HasPendingAttackResolution, Is.True,
                "The frame's ROW does arrive - this seat owns the standing decision.");
            Assert.That(seatProjection.PendingAttackResolution, Is.Null,
                "THE CAUSE, pinned: and this client materialises none of it, so the window's old " +
                "`HasPendingAttackResolution ? PendingAttackResolution : null` read null here " +
                "while the Director's own read whole. 8.40f withholds the frame on purpose and " +
                "this milestone does not widen it - the question carries the fact instead.");

            var directorChoice = host.Projection.PendingDefenseChoice;
            Assert.That(choice.SourceCardInstanceId, Is.EqualTo(directorChoice.SourceCardInstanceId),
                "The attacking card crosses the wire on the CHOICE, unchanged.");
            Assert.That(choice.SourceDefinitionReference,
                Is.EqualTo(directorChoice.SourceDefinitionReference));
            Assert.That(choice.ActionDefinitionId, Is.EqualTo(directorChoice.ActionDefinitionId));

            var model = DecisionWindowPresenter.CreateViewModel(seatProjection, choice.ChooserId);
            Assert.That(model, Is.Not.Null, "The defending seat owns the standing decision.");
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.Defense));
            Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                "The attacker's card is beside the offer on a joined seat too - this is the whole " +
                "of milestone 8.80. " + Describe(model));
            Assert.That(model.ContextCards[0].Card, Is.Not.Null,
                "and it is the real printed face `gd-decision-attack-card` draws.");
            Assert.That(model.ContextCards[0].Card.CardInstanceId,
                Is.EqualTo(choice.SourceCardInstanceId),
                "and it is THE ATTACKING card, not some other face this seat can see.");
            Assert.That(model.ContextCards[0].Caption,
                Does.StartWith("ATTACK " + choice.AttackTotal),
                "with the same composed total the chip carries on the Director. " + Describe(model));
            Assert.That(model.Title, Does.Not.EqualTo("DEFEND AGAINST THIS ATTACK?"),
                "and the title names the card, which it can only do if the card arrived. " +
                Describe(model));

            var directorModel = DecisionWindowPresenter.CreateViewModel(
                host.Projection, choice.ChooserId);
            Assert.That(model.Title, Is.EqualTo(directorModel.Title),
                "The joined seat and the Director ask the very same question.");
            Assert.That(model.ContextCards[0].Caption,
                Is.EqualTo(directorModel.ContextCards[0].Caption),
                "and print the same chip, which is what 'on every seat' means.");
        }

        /// <summary>
        /// Milestone 8.80. The resolving card id is read off the RECORD the wire delivered, which
        /// has carried it since 8.32 - 8.58's comment claimed the Attack frame "already crosses
        /// whole", and it does not.
        /// </summary>
        [Test]
        public void AnAttackingSeatsResolvingCardId_IsReadOffTheWireRecordRatherThanAWithheldFrame()
        {
            var harness = PrivateOnlineWireHarness.Compose(
                new MatchHost(ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()));
            harness.SeatAll();

            var host = harness.MatchHost;
            var attackerId = host.Projection.PendingDefenseChoice.AttackerId;
            var attackerSeat = host.Seats.Seats.Single(seat => seat.Owns(attackerId));
            var attackerProjection = new NetworkMatchClient(
                harness.Client(harness.OrdinalOf(attackerSeat.Id.Value)),
                host.Projection,
                MatchContentCatalogs.ForThisBuild()).Projection;

            Assert.That(attackerProjection.HasPendingAttackResolution, Is.True,
                "The acting seat is the one seat that does receive the frame.");
            Assert.That(attackerProjection.PendingAttackResolution, Is.Null,
                "and it is still withheld as an OBJECT - 8.40f's arm is untouched.");
            Assert.That(attackerProjection.ResolvingSourceCardInstanceId,
                Is.EqualTo(host.Projection.ResolvingSourceCardInstanceId),
                "so the id has to come off the decoded record, which is what this milestone " +
                "corrects. Before it, a network seat standing inside an Attack named no card at " +
                "all and a surface asking for one got `default`.");
            Assert.That(attackerProjection.ResolvingSourceCardInstanceId.IsDefined, Is.True);
            Assert.That(host.Projection.PendingDefenseChoice.AttackerId, Is.EqualTo(attackerId));
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
            // Milestone 9.6 re-derived the seed, 23 -> 27: the offer walk now greys actions that can do
            // nothing, so the walk reroutes and seed 23 reaches no respawn in 1,200 commands. Of seeds
            // 1-40 only 27 does (at command 337).
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 1200, projection => projection.PendingHeroRespawnChoice != null, seed: 27);
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
        public void ATiedInitiative_RemainsVisibleWhileAReconnectingTeammatePausesTheSameVersion()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 400,
                projection => projection.TeamAgreement?.Kind == TeamAgreementKind.Initiative ||
                    projection.PendingInitiativeChoice != null);
            var owner = host.Projection.DecisionOwner;
            var seat = host.Seats.Seats.First(owner.IsOwnedBy);
            var seatProjection = host.ProjectionForSeat(seat.Id);
            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                NetworkSeatProjectionCodec.Encode(seatProjection), out var record), Is.True);

            var active = new NetworkMatchReadModel(
                host.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
            var pausedRecord = WithoutPrivateDetail(record);
            var paused = new NetworkMatchReadModel(
                host.Projection, MatchContentCatalogs.ForThisBuild(), pausedRecord, false);
            Assert.That(paused.Version, Is.EqualTo(active.Version));
            Assert.That(paused.DecisionOwner, Is.EqualTo(active.DecisionOwner),
                "The public owner survives the teammate-away pause.");
            Assert.That(paused.TeamAgreement, Is.Null);
            Assert.That(paused.PendingInitiativeChoice, Is.Null,
                "The reconnect pause strips the private question detail and its commands.");

            var client = new SwitchingMatchClient(active);
            var view = new CapturingDecisionWindowView();
            var presenter = new DecisionWindowPresenter(client, view, seat.HeroId);
            presenter.Refresh();
            var beforePause = view.Model;
            Assert.That(beforePause?.Family, Is.EqualTo(DecisionWindowFamily.TiedInitiative));

            client.Projection = paused;
            presenter.Refresh();

            Assert.That(view.Model, Is.SameAs(beforePause),
                "A same-version connection pause must not erase WHO ACTS FIRST; reconnecting merely " +
                "restores its answerability.");
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
        // Rulebook p.15's Upgrade a Card - milestone 8.46g
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.46g. The user, mid-playtest on 2026-09-18: "players want the same treatment for
        /// level up screen, to be minimisable. So we need to put it in new thing you made, and show
        /// all information that we show now."
        ///
        /// <para>"All information" is asserted against the strip's OWN prompt rather than against
        /// typed strings, which is the whole point of the composition: the window is built from
        /// <c>RoundActionPresenter.CreateSystemTransitionPrompt</c>, so a fact it failed to carry
        /// would be a fact missing from one of two readings of one derivation - and there is only
        /// one.</para>
        /// </summary>
        [Test]
        public void ALevelUpUpgrade_OpensExpandedAndMinimisable_CarryingEveryFactTheStripShowed()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(
                out var levellingHeroId);
            var projection = session.Projection;
            var prompt = RoundActionPresenter.CreateSystemTransitionPrompt(projection);
            Assert.That(prompt, Is.Not.Null,
                "Sanity: the fixture stands on p.15's Upgrade a Card.");

            var model = DecisionWindowPresenter.CreateViewModel(projection, levellingHeroId);
            Assert.That(model, Is.Not.Null, "The levelling seat owns its own upgrade.");
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.LevelUp));
            Assert.That(model.State, Is.EqualTo(DecisionWindowState.Expanded),
                "The question is answered by pressing a card, so it opens over the board.");
            Assert.That(model.IsMinimisable, Is.True,
                "\"players want the same treatment for level up screen, to be minimisable\".");

            // The three strings the strip drew, carried whole: its section label, its prompt line and
            // its status line. Between them they hold the hero, the level reached, and the coins paid
            // and remaining.
            Assert.That(model.Eyebrow, Is.EqualTo(prompt.SectionLabel));
            Assert.That(model.Eyebrow, Is.EqualTo("LEVEL UP"));
            Assert.That(model.Question, Is.EqualTo(prompt.PromptLabel), Describe(model));
            Assert.That(model.Count, Is.EqualTo(prompt.StatusLabel), Describe(model));
            Assert.That(model.Title, Does.Contain("UPGRADE A CARD"), Describe(model));
            var heroName = projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(levellingHeroId)).DisplayName.ToUpperInvariant();
            Assert.That(model.Question, Does.Contain(heroName).And.Contain("LEVEL "),
                "The one-line question names the hero and the level reached - which is what a DOCKED " +
                "level-up shows on the slim bar beside the paid/remaining count. " + Describe(model));
            Assert.That(model.Count, Does.Contain("PAID ").And.Contain("REMAINING"), Describe(model));

            // One control per option, each the candidate's whole printed face, each carrying the
            // option's own detail (what is set aside, and the item it becomes) and the exact command
            // the pause published.
            Assert.That(model.Controls, Has.Count.EqualTo(prompt.Options.Count));
            Assert.That(model.Controls.Select(control => control.Role),
                Has.All.EqualTo(DecisionWindowControlRole.Answer));
            Assert.That(model.Controls.Select(control => control.Card), Has.All.Not.Null,
                "p.15 is a choice between CARDS, so every answer is the card itself. " + Describe(model));
            Assert.That(model.Controls.Select(control => control.Label),
                Is.EqualTo(prompt.Options.Select(option => option.Label)));
            Assert.That(model.Controls.Select(control => control.Detail),
                Is.EqualTo(prompt.Options.Select(option => option.Detail)));
            Assert.That(model.Controls.Select(control => control.Command),
                Is.EqualTo(prompt.Options.Select(option => option.Command)));
            Assert.That(model.Controls.Select(control => control.UpgradeOption), Has.All.Not.Null,
                "and carries the option WHOLE, so the gained item, the set-aside card and the pair " +
                "it belongs to are read from the strip's own derivation rather than from a second one.");
            Assert.That(
                model.Controls.Select(control => control.UpgradeOption.SetAsideCardName),
                Is.EqualTo(prompt.Options.Select(option => option.SetAsideCardName)),
                "The card p.15 sets aside travels with the control that would set it aside.");
            Assert.That(
                model.Controls.Select(control => control.UpgradeOption.SourceCardInstanceId),
                Is.EqualTo(prompt.Options.Select(option => option.SourceCardInstanceId)),
                "and so does the card it upgrades, which is what the pairs are grouped by.");

            // The pairs the pause itself grouped: one source card, two next-Tier candidates.
            var pairs = model.Controls
                .GroupBy(control => control.UpgradeOption.SourceCardInstanceId)
                .ToArray();
            Assert.That(pairs, Has.Length.EqualTo(3),
                "p.15 offers one card of each colour. " + Describe(model));
            Assert.That(pairs.Select(pair => pair.Count()), Has.All.EqualTo(2),
                "and two candidates for each of them.");
            Assert.That(model.Controls.Select(control => control.UpgradeOption.ItemIcon),
                Is.EqualTo(prompt.Options.Select(option => option.ItemIcon)),
                "The item each choice GAINS travels with the control that would take it.");

            // "Show current hand (all 5 cards) above the offer" - the user's 2026-09-07 directive,
            // inherited by the window rather than lost with the strip's prompt.
            Assert.That(model.HandPreview, Is.Not.Empty);
            Assert.That(
                model.HandPreview.All(card => projection.Cards
                    .Single(instance => instance.Id.Equals(card.CardInstanceId))
                    .OwnerId.Equals(levellingHeroId)),
                Is.True, "Exactly the deciding seat's own hand, and never another hero's.");
            Assert.That(
                model.HandPreview.Select(card => card.CardInstanceId).OrderBy(id => id.Value),
                Is.EqualTo(projection.Cards
                    .Where(card => card.OwnerId.Equals(levellingHeroId) &&
                        card.State.Location == CardLocation.Hand)
                    .Select(card => card.Id)
                    .OrderBy(id => id.Value)),
                "derived the same way the ordinary tray derives it - never a padded five.");
        }

        [Test]
        public void ALevelUpUpgrade_MinimisesToTheSlimBar_AndShowsTheDirectorWhoseUpgradeItIs()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorPausedOnLevelUpUpgrade(
                out var levellingHeroId);
            var heroName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(levellingHeroId)).DisplayName.ToUpperInvariant();

            var director = DecisionWindowPresenter.CreateViewModel(
                session.Projection, default(HeroId));
            Assert.That(director, Is.Not.Null, "The Director answers for everybody.");
            Assert.That(director.Title, Does.Contain(heroName),
                "One Director screen carries every seat's questions, so its title says whose this " +
                "is. " + Describe(director));
            Assert.That(
                director.HandPreview.Select(card => card.CardInstanceId).OrderBy(id => id.Value),
                Is.EqualTo(session.Projection.Cards
                    .Where(card => card.OwnerId.Equals(levellingHeroId) &&
                        card.State.Location == CardLocation.Hand)
                    .Select(card => card.Id)
                    .OrderBy(id => id.Value)),
                "and the hand the Director sees above the offer is THE LEVELLING HERO'S, not the " +
                "viewer's. 8.46g read it from the viewer and gave a Director screen none at all, " +
                "which left the panel drawing the offer with an empty row where the hand belongs - " +
                "the hand shown is the hand being chosen FOR, which is one hero either way.");

            var view = new CapturingDecisionWindowView();
            var presenter = new DecisionWindowPresenter(
                new DirectorMatchClient(session), view, levellingHeroId);
            presenter.Refresh();
            Assert.That(view.Model.State, Is.EqualTo(DecisionWindowState.Expanded));

            Assert.That(presenter.ToggleState(), Is.True,
                "The user asked for this family to be minimisable, which is what IsMinimisable means.");
            Assert.That(view.Model.State, Is.EqualTo(DecisionWindowState.Docked));
            Assert.That(view.Model.Question, Does.Contain(heroName),
                "The docked bar keeps the one-line question with the hero's name. " +
                Describe(view.Model));
            Assert.That(view.Model.Count, Does.Contain("PAID ").And.Contain("REMAINING"),
                "and the paid/remaining count beside it. " + Describe(view.Model));
            Assert.That(view.Model.Controls.Select(control => control.UpgradeOption),
                Has.All.Not.Null,
                "Minimising is a reading preference, not an answer: every option survives it.");
        }

        // -------------------------------------------------------------------------------------
        // Milestone 8.58: the card in the window, and the words on its controls
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The user, on the shipped window, 2026-09-19: "This sucks. We said we will show the card, and
        /// have nice labels on the buttons? This box is a waste of space like this."
        ///
        /// <para>Every family whose question comes from a card's printed text shows that card - a
        /// modal's arms, a printed number, a printed `may`, and the card chooser, which at 8.46c was
        /// the one that deliberately showed none because its ANSWERS were already cards. The card the
        /// question comes from and the cards the question is between are different cards.</para>
        /// </summary>
        [Test]
        public void EveryCardTextQuestion_ShowsTheCardItComesFrom_AndIsTitledWithThatCardsName()
        {
            foreach (var staged in new (string Name, Func<MatchSession> Stage)[]
            {
                ("a printed number", () =>
                {
                    var session = ConfiguredMatchFixtures.CreateKineticSession(
                        WaspCardContent.KineticBlast, stoppedShort: false);
                    CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
                    StartPrimary(session, ActionKind.Skill);
                    AnswerFirstSelection(session);
                    return session;
                }),
                ("a printed may", () =>
                {
                    var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
                    CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
                    StartPrimary(session, ActionKind.Skill);
                    AnswerFirstSelection(session);
                    AnswerFirstSelection(session);
                    return session;
                }),
                ("a card chooser", CreateSessionAwaitingTheDiscardChooser)
            })
            {
                var session = staged.Stage();
                var projection = session.Projection;
                var choice = projection.PendingEffectChoice;
                var model = DecisionWindowPresenter.CreateViewModel(projection, choice.ChooserId);

                Assert.That(model.ContextCards, Has.Count.EqualTo(1),
                    $"{staged.Name}: the card whose text is asking is drawn beside the question. " +
                    Describe(model));
                var sourceCardId = projection.ResolvingSourceCardInstanceId;
                Assert.That(sourceCardId.IsDefined, Is.True,
                    $"{staged.Name}: the projection names the resolving card.");
                Assert.That(model.ContextCards[0].Card.CardInstanceId, Is.EqualTo(sourceCardId),
                    $"{staged.Name}: and it is THAT card, not another.");
                Assert.That(model.ContextCards[0].Caption, Is.Null,
                    $"{staged.Name}: with no `MODE CHOICE` gutter under it - the title is its name.");
                Assert.That(model.Title, Does.StartWith(CardName(projection, sourceCardId)),
                    $"{staged.Name}: the window is titled with the card. " + Describe(model));
            }
        }

        /// <summary>
        /// Milestone 8.58, and the defect the 2026-09-19 packaged playtest photographed.
        ///
        /// <para>A NETWORK seat's read model withholds the running frame on purpose (8.40f's privacy
        /// arm), and the window used to read the source card off that frame alone - so on every client
        /// in that playtest Swift's Reload! drew <c>MODE #1 RELOAD-BORROW-ARM</c> with no card beside
        /// it, although both of its arms have been declared since 8.46a. The projection publishes WHICH
        /// card is resolving on its own now, and this poses exactly that shape: a real projection with
        /// the frame taken away.</para>
        /// </summary>
        [Test]
        public void AProjectionThatWithholdsTheRunningFrame_StillShowsTheCardAndItsDeclaredLabels()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
            StartPrimary(session, ActionKind.Skill);
            AnswerFirstSelection(session);
            AnswerFirstSelection(session);

            var projection = session.Projection;
            var choice = projection.PendingEffectChoice;
            var whole = DecisionWindowPresenter.CreateViewModel(projection, choice.ChooserId);
            var withheld = DecisionWindowPresenter.CreateViewModel(
                new FrameWithheldReadModel(projection), choice.ChooserId);

            Assert.That(withheld, Is.Not.Null);
            Assert.That(withheld.ContextCards, Has.Count.EqualTo(1),
                "The card is shown from the published resolving-card fact alone. " + Describe(withheld));
            Assert.That(withheld.ContextCards[0].Card.CardInstanceId,
                Is.EqualTo(whole.ContextCards[0].Card.CardInstanceId));
            Assert.That(withheld.Title, Is.EqualTo(whole.Title));
            Assert.That(withheld.Controls.Select(control => control.Label),
                Is.EqualTo(whole.Controls.Select(control => control.Label)),
                "and the declared labels are found, because they are keyed on that card. " +
                Describe(withheld));
        }

        /// <summary>
        /// A seat never sees a node id. The engine's own <c>MODE #n &lt;node-id&gt;</c> vocabulary is a
        /// DIRECTOR diagnostic; the window's own fallback is the printed bullet's position, and the
        /// content check makes even that unreachable for dealt content.
        /// </summary>
        [Test]
        public void NoDrawnControl_EverNamesANodeIdOrTheWordAcceptOrDecline()
        {
            var seen = 0;
            foreach (var stage in new Func<MatchSession>[]
            {
                CreateSessionAwaitingTheDiscardChooser,
                () =>
                {
                    var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
                    CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
                    StartPrimary(session, ActionKind.Skill);
                    AnswerFirstSelection(session);
                    AnswerFirstSelection(session);
                    return session;
                }
            })
            {
                var session = stage();
                var choice = session.Projection.PendingEffectChoice;
                var model = DecisionWindowPresenter.CreateViewModel(
                    session.Projection, choice.ChooserId);
                foreach (var control in model.Controls.Concat(
                    model.CornerControl == null
                        ? Enumerable.Empty<DecisionWindowControlViewModel>()
                        : new[] { model.CornerControl }))
                {
                    seen++;
                    Assert.That(control.Label, Does.Not.StartWith("MODE #"),
                        "A node id is engine vocabulary and never reaches a seat. " + Describe(model));
                    Assert.That(control.Label, Does.Not.Contain("-ARM"), Describe(model));
                    Assert.That(control.Label, Is.Not.EqualTo(EffectChoiceLabels.AcceptLabel),
                        "\"never ACCEPT/DECLINE\" is the user's own row. " + Describe(model));
                    Assert.That(control.Label, Is.Not.EqualTo(EffectChoiceLabels.DeclineLabel),
                        Describe(model));
                }
            }

            Assert.That(seen, Is.GreaterThan(0), "A sweep that drew nothing proves nothing.");
            Assert.That(DecisionWindowPresenter.PrintedBulletLabel(1), Is.EqualTo("OPTION 1"),
                "and a bullet with no declared words is named by its position on the card.");
        }

        /// <summary>
        /// Milestone 8.58. The two cards the user photographed, read through the one lookup the window
        /// makes: Swift's Reload! - both printed bullets - and Bounce's borrowed "may repeat once".
        ///
        /// <para>Reload!'s modal is driven end to end by
        /// <c>SwiftProgressedUiJourneyPlayModeTests.ReloadOffersItsTwoBulletsAndDefeatsAnAdjacentMinion</c>,
        /// which asserts the drawn window itself. Bounce's repeat is only reachable THROUGH Reload!'s
        /// borrow (8.48's own finding) and its fixture drives the turn authority rather than a match
        /// session, so what is asked here is the half a session-level case would add: that the words the
        /// window would draw for those exact nodes are the printed consequences, keyed on the card the
        /// window names.</para>
        /// </summary>
        [Test]
        public void TheTwoCardsTheUserPhotographed_DeclareThePrintedConsequenceForEveryControl()
        {
            Assert.That(
                DecisionLabelCatalog.TryGetLabel(
                    SwiftCardContent.Reload, new EffectNodeId("reload-borrow-arm"), out var borrow),
                Is.True);
            Assert.That(borrow.Chosen.ToUpperInvariant(),
                Is.EqualTo("PERFORM THE PRIMARY ACTION OF YOUR RIGHTMOST RESOLVED CARD"));
            Assert.That(borrow.HasDeclined, Is.False, "A Choose arm declines nothing of its own.");

            Assert.That(
                DecisionLabelCatalog.TryGetLabel(
                    SwiftCardContent.Reload, new EffectNodeId("reload-defeat-arm"), out var defeat),
                Is.True);
            Assert.That(defeat.Chosen.ToUpperInvariant(),
                Is.EqualTo("DEFEAT A MINION ADJACENT TO YOU"));

            Assert.That(
                DecisionLabelCatalog.TryGetLabel(
                    SwiftCardContent.Bounce, new EffectNodeId("bounce-repeat"), out var repeat),
                Is.True);
            Assert.That(repeat.Chosen.ToUpperInvariant(),
                Is.EqualTo("MOVE 2 MORE SPACES IN A STRAIGHT LINE"),
                "\"Move it again\", in the card's own printed words.");
            Assert.That(repeat.Declined.ToUpperInvariant(), Is.EqualTo("STOP HERE"),
                "and the decline names the consequence too - never `DECLINE`.");
        }

        /// <summary>A projection that names the resolving card and withholds the frame that holds it.</summary>
        private sealed class FrameWithheldReadModel : MatchFeaturePresenterTests.DelegatingReadModel
        {
            public FrameWithheldReadModel(IMatchReadModel inner)
                : base(inner)
            {
            }

            public override PendingResolution PendingSkillResolution => null;
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

        private static NetworkSeatProjectionRecord WithoutPrivateDetail(
            NetworkSeatProjectionRecord source) =>
            new NetworkSeatProjectionRecord(
                source.Version, source.SeatId, source.PlayerId, source.HeroId, source.TeamSide,
                source.RoundIndex, source.TurnIndex, source.Phase, source.IsTurnActive,
                source.IsSeatUndoAvailable, source.EndOfRoundStage, source.TieBreakerRevealedSide,
                source.SelectedActor, source.Outcome, source.TeamLifeCounters,
                source.RemainingWaveCounters, source.Heroes, source.Minions, source.Obstacles,
                source.PlacedTokens, source.HeldMarkers, source.TurnSlots, source.Commitments,
                source.HandSummaries, source.OwnCards, source.PublicCards,
                source.HeroProgression, source.Scoreboard, source.ActiveEffectSourceCardInstanceIds,
                source.DecisionOwner, source.DecisionContractFamilyId,
                source.DecisionContractCommands, source.AuthorizedCommands, detail: null);

        private sealed class SwitchingMatchClient : IMatchClient
        {
            public SwitchingMatchClient(IMatchReadModel projection) => Projection = projection;

            public IMatchReadModel Projection { get; set; }
            public bool IsDirector => false;
            public MatchClientResult Submit(DirectorCommand command) =>
                throw new System.NotSupportedException();
            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                throw new System.NotSupportedException();
            public MatchClientResult SubmitUndo() => throw new System.NotSupportedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.20d. What the match says while it is waiting on a paused effect frame.
    ///
    /// <para>Every case here drives configured content to a real pause and then reads the two things
    /// a player actually meets: the status line, and the gutter over the controls that answer it.
    /// Before this milestone both were wrong for every effect choice in the build - the status line
    /// read "Initiative N: choose an action or End Turn" and the controls sat under <c>ACTIONS</c> -
    /// and the reason no fixture caught it is that no fixture had ever read them. The authority
    /// fixtures each drive their own decision and assert what it does.</para>
    ///
    /// <para><c>StatusDerivationConformanceTests</c> is what keeps the next family from arriving
    /// unnamed; these cases are what say the words are right for the families that are here.</para>
    /// </summary>
    public sealed class EffectChoiceStatusNamingTests
    {
        private const string ActionRowGutter = "ACTIONS";

        /// <summary>
        /// What the derivation falls through to when nothing is pending. No standing decision may
        /// ever reach a player under it, which is the whole of the defect this milestone closes.
        /// </summary>
        private const string OrdinaryTurnStatusFragment = "choose an action, or end your turn";

        // -------------------------------------------------------------------------------------
        // The shapes the configured roster can reach
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.10's discard chooser, reached through Shock's rider exactly as
        /// <c>ConfiguredAttackCardTests</c> reaches it. It is the shape milestone 8.20b's Wasp
        /// journey met under the action heading, and the one milestone 8.15a draws as whole faces.
        /// </summary>
        [Test]
        public void ThePageTenDiscardChooser_IsFramelessAndNarratesWhyTheCardIsChosen()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            AttackTheAdjacentMinion(session, WaspCardContent.Shock, minionId);

            // The rider's first question names a hero; its second is the chosen hero's own hand.
            AnswerFirstSelection(session);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.Value.Kind),
                Is.All.EqualTo(EffectValueKind.CardInstance),
                "This is the card-valued question, or the case below is about something else.");
            Assert.That(
                new[] { choice.MinimumSelections, choice.MaximumSelections },
                Is.EqualTo(new[] { 1, 1 }),
                "p.10's discard names exactly one card, which is the sentence the words below state.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EffectChoicePrompt, Is.Not.Null);
            Assert.That(model.EffectChoicePrompt.Subject, Is.EqualTo(EffectChoiceSubject.Card));
            Assert.That(model.EffectChoicePrompt.SectionLabel, Is.EqualTo("CARD CHOICE"));
            var chooserName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(choice.ChooserId)).DisplayName;
            // Milestone 8.35n: "YOUR CARD CHOICE — " leads the own-decision line.
            Assert.That(model.Status,
                Is.EqualTo(
                    $"YOUR CARD CHOICE — Choose 1 of {choice.Candidates.Count} cards for " +
                    $"{chooserName} to discard."),
                "The narration states both the bounds and why this hero is choosing a card.");
            AssertNamesNoOtherPause(model.Status);

            var view = new RoundActionUnityView();
            view.Render(model);
            var cardSection = Descendants(view.Root)
                .Single(element => element.name == "gd-effect-card-section");
            Assert.That(Descendants(view.Root).OfType<Label>()
                .Any(label => label.text == "CARD CHOICE"), Is.False,
                "Whole card faces plus purpose-bearing narration do not need a CARD CHOICE gutter.");
            var cardControls = Descendants(cardSection).OfType<Button>()
                .Where(control => control.name == "gd-effect-card").ToArray();
            Assert.That(cardControls, Has.Length.EqualTo(choice.Candidates.Count));
            Assert.That(cardControls.All(control =>
                    control.style.paddingLeft.value.value == 0f &&
                    control.style.borderTopWidth.value == 0f &&
                    control.style.backgroundColor.value.a == 0f), Is.True,
                "The legacy plate, inset and selection outline are absent around every choice face.");
            Assert.That(cardControls.All(control => Descendants(control)
                    .Count(element => element.name == "gd-card-exterior-outline") == 1), Is.True,
                "The requested choice frame is removed without deleting the shared card silhouette.");

            var sections = Render(model);
            AssertEveryControlIsUnderItsOwnHeading(model, sections);
            Assert.That(sections["CARD CHOICE"].Select(control => control.name),
                Is.All.EqualTo("gd-effect-card"),
                "Milestone 8.15a's whole printed faces remain the complete controls. " +
                Describe(sections));
            Assert.That(sections.ContainsKey(ActionRowGutter), Is.False,
                "and nothing is left in the row of actions the player is not being offered. " +
                Describe(sections));
        }

        /// <summary>
        /// Kinetic Blast's `3 or 4 spaces`. Milestone 8.15 named the controls after the printed
        /// numbers; this is the line above them.
        /// </summary>
        [Test]
        public void ThePrintedNumberChoice_IsNamedANumberChoice()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
            StartPrimarySkill(session);

            // The units first - a board-answered target question - and then the distance.
            var targets = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(targets.EffectChoicePrompt.Subject, Is.EqualTo(EffectChoiceSubject.Piece),
                "`up to 2 enemy units adjacent to you` names pieces standing on the board.");
            // Milestone 8.35n prefixes the family header ("YOUR TARGET CHOICE — ") the FIFTH
            // amendment's own-decision narration requires; the `up to` claim itself is unmoved.
            Assert.That(targets.Status, Does.Contain("Choose up to "),
                "An `up to` states its maximum rather than pretending a floor. Read: " + targets.Status);
            Assert.That(targets.Status, Does.Contain(" target"));
            Assert.That(targets.Status, Does.Contain("or decline"),
                "Milestone 8.20a made declining an answer a player can give by name.");
            AssertNamesNoOtherPause(targets.Status);

            AnswerFirstSelection(session);
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EffectChoicePrompt.Subject, Is.EqualTo(EffectChoiceSubject.Number));
            Assert.That(model.EffectChoicePrompt.SectionLabel, Is.EqualTo("NUMBER CHOICE"));
            // Milestone 8.35n: the own-decision family header now leads the sentence.
            Assert.That(model.Status, Is.EqualTo("YOUR NUMBER CHOICE — Choose 1 of 2 printed numbers."));
            AssertNamesNoOtherPause(model.Status);

            var sections = Render(model);
            AssertEveryControlIsUnderItsOwnHeading(model, sections);
            Assert.That(sections["NUMBER CHOICE"].Select(control => control.text),
                Is.EqualTo(new[] { "3", "4" }),
                "and the printed numbers milestone 8.15 put on the controls are under it. " +
                Describe(sections));
        }

        /// <summary>
        /// Lift Up's `May repeat once` - rulebook p.10's confirm-or-decline, on verified content.
        /// A printed `may` was the shape most damaged by the old fallback: a player being asked
        /// whether to take an optional step read that they were choosing an action.
        /// </summary>
        [Test]
        public void ThePrintedMay_IsNamedAnOptionalStepRatherThanAnAction()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
            StartPrimarySkill(session);
            AnswerFirstSelection(session);
            AnswerFirstSelection(session);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "`May repeat once` is p.10's confirm-or-decline and nothing else.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EffectChoicePrompt.Subject, Is.EqualTo(EffectChoiceSubject.OptionalStep));
            Assert.That(model.EffectChoicePrompt.SectionLabel, Is.EqualTo("OPTIONAL STEP"));
            // Milestone 8.35n: "YOUR OPTIONAL STEP — " leads the own-decision line.
            Assert.That(model.Status,
                Is.EqualTo("YOUR OPTIONAL STEP — An optional step is offered: confirm it or decline."));
            AssertNamesNoOtherPause(model.Status);

            var sections = Render(model);
            AssertEveryControlIsUnderItsOwnHeading(model, sections);
            Assert.That(sections["OPTIONAL STEP"].Select(control => control.text),
                Is.EqualTo(new[] { EffectChoiceLabels.AcceptLabel, EffectChoiceLabels.DeclineLabel }),
                "Both answers keep their labels, their order and their invocations; only the " +
                "heading over them changed. " + Describe(sections));
        }

        /// <summary>
        /// Milestone 8.11a moved every candidate naming a piece or a space onto the board, so such a
        /// pause draws no control at all. The status line is then the whole of what tells a player
        /// the match is waiting on them, which is why it may not read as an ordinary turn.
        /// </summary>
        [Test]
        public void ABoardAnsweredChoice_IsNamedEvenThoughItDrawsNoControl()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            AttackTheAdjacentMinion(session, WaspCardContent.Shock, minionId);

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EffectChoiceTargets, Is.Not.Empty,
                "Shock's first question names an enemy hero, and the board is where it is answered.");
            Assert.That(model.EffectChoices, Is.Empty, "so this pause has no control of its own.");
            Assert.That(model.EffectChoicePrompt, Is.Not.Null);
            Assert.That(model.EffectChoicePrompt.Subject, Is.EqualTo(EffectChoiceSubject.Piece));
            // Milestone 8.35n: "YOUR TARGET CHOICE — " leads; the claim itself is unmoved.
            Assert.That(model.Status, Does.Contain("Choose 1 of 1 target"));
            AssertNamesNoOtherPause(model.Status);
            Assert.That(Render(model), Is.Empty,
                "Nothing is drawn in the contextual region, and a section with no control in it is " +
                "not drawn under a heading either.");
        }

        // -------------------------------------------------------------------------------------
        // The claim over the whole configured match
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The sweep milestone 8.11e's inventory case established, asked of the words instead of the
        /// transitions: play the configured match through and let every effect choice it reaches be
        /// checked. It is the case that would have failed for the whole build before this milestone.
        /// </summary>
        [Test]
        public void NoStandingEffectChoiceInAConfiguredMatchEverReadsAsAnOrdinaryTurn()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var reached = new HashSet<EffectChoiceSubject>();
            var pauses = 0;

            for (var step = 0;
                step < 160 && session.Projection.Phase != RoundResolutionPhase.GameEnded;
                step++)
            {
                var projection = session.Projection;
                if (projection.PendingEffectChoice != null)
                {
                    pauses++;
                    var model = RoundActionPresenter.CreateViewModel(projection);
                    Assert.That(model.EffectChoicePrompt, Is.Not.Null,
                        "A standing effect choice always carries the prompt that names it.");
                    reached.Add(model.EffectChoicePrompt.Subject);
                    AssertNamesNoOtherPause(model.Status);
                    AssertEveryControlIsUnderItsOwnHeading(model, Render(model));
                }

                var offered = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                Assert.That(offered, Is.Not.Empty,
                    $"The configured match stalled at {projection.Phase}, round " +
                    $"{projection.RoundIndex}, turn {projection.TurnIndex}.");
                Assert.That(session.Execute(offered[0]).Accepted, Is.True);
            }

            Assert.That(pauses, Is.GreaterThan(0),
                "A sweep that met no effect choice proves nothing; the engaged verified mirror " +
                "must still reach one.");
            Assert.That(reached, Has.No.Member(EffectChoiceSubject.Mixed),
                "Every shape the configured roster reaches has a noun of its own. A mixed candidate " +
                "set is legal and would be named `options`, but no dealt content prints one - so " +
                "one appearing here is content this reading has not been checked against.");
        }

        // -------------------------------------------------------------------------------------
        // The composer's own arithmetic
        // -------------------------------------------------------------------------------------

        [TestCase(1, 1, 0, false, "Choose 1 of 3 modes.")]
        [TestCase(2, 2, 1, false, "Choose 2 of 3 modes. 1 chosen.")]
        [TestCase(0, 2, 0, true, "Choose up to 2 of 3 modes, or decline. 0 chosen.")]
        [TestCase(1, 3, 2, false, "Choose 1 to 3 of 3 modes. 2 chosen.")]
        public void TheStatusWordsStateTheBoundsTheAuthoritySet(
            int minimum, int maximum, int selected, bool declinable, string expected)
        {
            var choice = Choice(minimum, maximum, selected);

            Assert.That(choice.DeclineInvocation != null, Is.EqualTo(declinable),
                "Declining is derived from the minimum rather than declared, so this case must be " +
                "asking about the shape it thinks it is.");
            Assert.That(
                EffectChoicePromptLabels.StatusLine(choice, EffectChoiceSubject.Mode),
                Is.EqualTo(expected));
        }

        [Test]
        public void ACardOutsideTheHandDoesNotImplyThatTheChoiceRetrievesIt()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out var owner);
            var card = session.Projection.Cards.First(candidate =>
                candidate.OwnerId.Equals(owner) && candidate.State.Location == CardLocation.DiscardPile);
            var choice = new PendingEffectChoice(
                new EffectResolutionId("card-purpose"), new EffectChoiceId("card-purpose"),
                new EffectNodeId("card-purpose"), owner, EffectChoiceKind.SelectTarget,
                new[] { EffectChoiceCandidate.Value_(EffectValue.Card(card.Id)) }, 1, 1);
            var name = session.Projection.Setup.Heroes.Single(hero => hero.HeroId.Equals(owner)).DisplayName;
            Assert.That(EffectChoicePromptLabels.StatusLine(choice, EffectChoiceSubject.Card, session.Projection),
                Is.EqualTo($"Choose 1 of 1 card belonging to {name}."),
                "Location establishes ownership, not the operation: public cards may be borrowed or retrieved.");
        }

        [Test]
        public void ASingleCandidateIsCountedInTheSingular()
        {
            Assert.That(
                EffectChoicePromptLabels.StatusLine(
                    Choice(1, 1, 0, candidates: 1), EffectChoiceSubject.Card),
                Is.EqualTo("Choose 1 of 1 card."));
        }

        /// <summary>
        /// Milestone 6.5ae's stop: every offered mode was refused, so the only answer left is the one
        /// that closes the action. Saying "choose 1 of 3 modes" over three controls that cannot be
        /// pressed would be worse than the fallback this milestone is replacing.
        /// </summary>
        [Test]
        public void AChoiceWhoseEveryModeWasRefused_SaysSoRatherThanAskingForOne()
        {
            var candidates = Modes(3);
            var choice = new PendingEffectChoice(
                new EffectResolutionId("resolution"),
                new EffectChoiceId("choice"),
                new EffectNodeId("node"),
                new HeroId("hero"),
                EffectChoiceKind.SelectOption,
                candidates,
                1,
                1,
                null,
                candidates,
                canStop: true);

            Assert.That(choice.StopInvocation, Is.Not.Null);
            Assert.That(
                EffectChoicePromptLabels.StatusLine(choice, EffectChoiceSubject.Mode),
                Is.EqualTo("No offered mode can be performed: End Turn closes the action."));
        }

        [Test]
        public void EverySubjectHasAHeadingOfItsOwnAndNoneIsTheActionRow()
        {
            var labels = Enum.GetValues(typeof(EffectChoiceSubject))
                .Cast<EffectChoiceSubject>()
                .Select(EffectChoicePromptLabels.SectionLabel)
                .ToArray();

            Assert.That(labels, Has.None.Null);
            Assert.That(labels, Has.None.EqualTo(string.Empty));
            Assert.That(labels, Has.None.EqualTo(ActionRowGutter),
                "A heading equal to the action row would restore the defect this milestone closes.");
        }

        // -------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Every control this decision projects is drawn, once, under the heading that names it -
        /// which is also what says none of them is left in the action row, because
        /// <c>BuildEffectChoiceSection</c> is the only place they are added.
        /// </summary>
        private static void AssertEveryControlIsUnderItsOwnHeading(
            RoundActionViewModel model, Dictionary<string, List<Button>> sections)
        {
            var expected = model.EffectChoices.Count + model.UnavailableEffectChoices.Count;
            if (expected == 0)
            {
                return;
            }

            var heading = model.EffectChoicePrompt.SectionLabel;
            Assert.That(sections.ContainsKey(heading), Is.True,
                $"The decision's controls are drawn under `{heading}`. " + Describe(sections));
            Assert.That(sections[heading], Has.Count.EqualTo(expected),
                "one control per projected invocation, and none of them anywhere else. " +
                Describe(sections));
        }

        private static EffectChoiceCandidate[] Modes(int count) =>
            Enumerable.Range(1, count)
                .Select(index => EffectChoiceCandidate.Option(new EffectNodeId($"mode-{index}")))
                .ToArray();

        private static PendingEffectChoice Choice(
            int minimum, int maximum, int selected, int candidates = 3)
        {
            var offered = Modes(candidates);
            return new PendingEffectChoice(
                new EffectResolutionId("resolution"),
                new EffectChoiceId("choice"),
                new EffectNodeId("node"),
                new HeroId("hero"),
                EffectChoiceKind.SelectOption,
                offered,
                minimum,
                maximum,
                offered.Take(selected));
        }

        /// <summary>
        /// No status line for one pause may be the words of another, and none may be the ordinary
        /// turn's. The fallback is checked by fragment because it carries the actor's Initiative.
        /// </summary>
        private static void AssertNamesNoOtherPause(string status)
        {
            Assert.That(status, Does.Not.Contain(OrdinaryTurnStatusFragment),
                "A standing decision drawn under the ordinary turn's words is the defect itself.");
            Assert.That(status, Does.Not.Contain("Incoming Attack"));
            Assert.That(status, Does.Not.Contain("target for the Attack"));
            Assert.That(status, Does.Not.Contain("respawn"));
            Assert.That(status, Does.Not.Contain("displaced minion"));
        }

        /// <summary>
        /// The contextual region as it is drawn, keyed by each section's gutter label. A section is
        /// recognised by that label's own reserved width rather than by its text, so a renamed
        /// heading is still found and still compared, and a section the region hid for having
        /// nothing in it is not reported as drawn.
        /// </summary>
        private static Dictionary<string, List<Button>> Render(RoundActionViewModel model)
        {
            var view = new RoundActionUnityView();
            view.Render(model);
            var sections = new Dictionary<string, List<Button>>(StringComparer.Ordinal);
            foreach (var element in Descendants(view.Root))
            {
                if (element.childCount == 0 ||
                    !(element[0] is Label gutter) ||
                    gutter.style.width.value.value != RoundActionUnityView.SectionGutterWidth ||
                    element.style.display.value == DisplayStyle.None)
                {
                    continue;
                }

                sections[gutter.text] = Descendants(element).OfType<Button>().ToList();
            }

            // Milestone 8.30i3 moved confirm-or-decline answers above the retained hand. They still
            // belong to the prompt's named decision family even though that dedicated host no longer
            // spends horizontal room on the contextual region's gutter label.
            if (model.EffectChoicePrompt?.Subject == EffectChoiceSubject.OptionalStep &&
                view.OptionalStepActionsRoot.style.display.value != DisplayStyle.None)
            {
                sections[model.EffectChoicePrompt.SectionLabel] =
                    Descendants(view.OptionalStepActionsRoot).OfType<Button>().ToList();
            }

            // Card-valued choices deliberately have no visible gutter. Keep them keyed by their
            // semantic family here so the general one-control-per-invocation sweep still covers the
            // frameless row without mistaking a test dictionary key for painted UI text.
            if (model.EffectChoicePrompt?.Subject == EffectChoiceSubject.Card)
            {
                var cardSection = Descendants(view.Root)
                    .SingleOrDefault(element => element.name == "gd-effect-card-section");
                if (cardSection != null &&
                    cardSection.style.display.value != DisplayStyle.None)
                {
                    sections[model.EffectChoicePrompt.SectionLabel] =
                        Descendants(cardSection).OfType<Button>().ToList();
                }
            }

            return sections;
        }

        private static string Describe(Dictionary<string, List<Button>> sections) =>
            "Shown: " + string.Join("; ", sections.Select(section =>
                $"{section.Key}: [{string.Join(", ", section.Value.Select(Name))}]"));

        private static string Name(Button control) =>
            string.IsNullOrEmpty(control.text) ? control.name : control.text;

        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        private static void AnswerFirstSelection(MatchSession session)
        {
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            var confirm = session.Projection.PendingEffectChoice?.ConfirmInvocation;
            if (confirm != null)
            {
                Answer(session, confirm);
            }
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(invocation));

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

        private static void StartPrimarySkill(MatchSession session) =>
            StartPrimary(session, ActionKind.Skill);

        private static void StartPrimary(MatchSession session, ActionKind kind)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == kind);
            Assert.That(offer.IsAvailable, Is.True,
                "The composed action must be takeable in this position, or the case holds over " +
                "nothing.");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        /// <summary>
        /// Both sides commit, the actor starts its printed Attack and names the adjacent minion -
        /// the path <c>ConfiguredAttackCardTests</c> takes to reach a rider's own pause.
        /// </summary>
        private static void AttackTheAdjacentMinion(
            MatchSession session, CardDefinitionId cardId, MinionId targetId)
        {
            CommitAgainstStopProjectiles(session, cardId);
            StartPrimary(session, ActionKind.Attack);
            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(target.LegalTargetMinionIds, Does.Contain(targetId),
                "The minion the fixture placed beside Wasp has to be a legal target, or the case " +
                "holds over nothing.");
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, targetId));
        }

        private static ActionDefinition ActionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));
    }
}

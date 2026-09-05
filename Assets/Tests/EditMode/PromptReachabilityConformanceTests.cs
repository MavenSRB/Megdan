using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11a, at the user's request, and written because one of these fell through.
    ///
    /// <para>Milestone 8.11 shipped rulebook p.14's Block. Every case it wrote drove the authority
    /// directly - offer the Block, choose it, check what happened - and every one passed. None of them
    /// ever built the prompt a defender is actually shown. That prompt looked for the offer's printed
    /// Defense icon, a Block has none, and it threw where it was drawn: the authority asked the
    /// question, the screen asked nothing, and the match stopped. It took playing the game to find it.
    /// </para>
    ///
    /// <para>The gap was not the Block. The gap was that nothing checked <b>the authority's offers
    /// against the surfaces a player answers them on</b>. These cases check exactly that, from the
    /// authority's own legal sets, so a future choice that presentation cannot draw fails here rather
    /// than in play. They deliberately assert reachability rather than appearance: what a control says
    /// is a judgement, but whether an offered answer can be given at all is not.</para>
    /// </summary>
    public sealed class PromptReachabilityConformanceTests
    {
        /// <summary>
        /// The case that fell through, generalised: every Defense the authority offers must produce
        /// exactly one control, whatever kind of Defense it is. Wasp's hand holds both kinds at once -
        /// a numeric secondary and a printed Block - so this covers the shape that worked and the shape
        /// that did not from a single pending choice.
        /// </summary>
        [Test]
        public void EveryLegalDefenseOffer_ProducesExactlyOneControlIncludingABlock()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            var defender = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defender);

            var choice = session.Projection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null, "The scenario reaches p.14 step 3, or this asserts nothing.");
            Assert.That(choice.LegalDefenseOffers, Is.Not.Empty);
            Assert.That(
                choice.LegalDefenseOffers.Any(offer => IsBlock(session, offer)), Is.True,
                "and one of them is a Block, which is the shape that was unreachable.");
            Assert.That(
                choice.LegalDefenseOffers.Any(offer => !IsBlock(session, offer)), Is.True,
                "and one is an ordinary numeric Defense, so both shapes are covered at once.");

            RoundActionViewModel model = null;
            Assert.That(
                () => model = RoundActionPresenter.CreateViewModel(session.Projection),
                Throws.Nothing,
                "Building the prompt must not throw. It threw here, and a thrown prompt draws nothing.");

            Assert.That(model.DefenseChoices, Has.Count.EqualTo(
                    choice.LegalDefenseOffers.Count + (choice.CanDecline ? 1 : 0)),
                "One control per offer the authority allows, plus declining when it is allowed.");
            Assert.That(
                model.DefenseChoices.All(value => !string.IsNullOrWhiteSpace(value.Label)), Is.True,
                "and each says something, because a blank control is an answer nobody can find.");
            Assert.That(
                model.DefenseChoices.Select(value => value.Label).Distinct().Count(),
                Is.EqualTo(model.DefenseChoices.Count),
                "and says something different, because two identical controls are one answer lost.");
        }

        /// <summary>
        /// The general form of the same rule, and the one that matters most now that targeting moved
        /// onto the board: <b>every candidate the authority offers must be answerable somewhere</b> -
        /// by clicking a marked piece, by clicking a lit space, or by a control in the prompt.
        ///
        /// <para>Milestone 8.11a left this indifferent about which, because a candidate naming a hero
        /// by identity rather than by coordinate still carried a control: the board is addressed by
        /// space, and such a candidate names none. Milestone 8.11c closes exactly that, so the rule
        /// tightens for the candidates it closed - a candidate that names a board object is answered
        /// <b>on the board</b>, and its prompt control is gone. Everything else keeps the weaker rule,
        /// because a mode, a Spawn drawn from the supply, and a confirm still have no board surface at
        /// all.</para>
        ///
        /// <para>Which candidates name an object is read here from authoritative occupancy directly,
        /// never from the preview the production path builds. That is deliberate: reading it from the
        /// preview would make the case agree with whatever the preview happens to cover, which is the
        /// very thing 8.11c changes.</para>
        /// </summary>
        [Test]
        public void EveryLegalEffectChoiceCandidateNamingAnObject_IsAnsweredOnTheBoard()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            var view = new CapturingBoardInteractionView();
            var board = new BoardInteractionPresenter(session, view);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(
                session, WaspCardContent.ReflectProjectiles);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "The rider asks a question, or this asserts nothing.");
            Assert.That(choice.SelectInvocations, Is.Not.Empty);
            Assert.That(
                choice.SelectInvocations.Any(value =>
                    value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Hero),
                Is.True,
                "Reflect Projectiles' rider names the attacker by HeroId, which is the shape milestone " +
                "8.11c closes. Without one offered here this case asserts nothing about it.");

            board.Refresh();
            var model = view.Model;
            var prompt = RoundActionPresenter.CreateViewModel(session.Projection);

            var boardDestinations = new HashSet<HexCoordinate>(model.EffectChoiceDestinations);
            var pieceCues = model.PieceTargetCues.ToDictionary(value => value.Target);
            var expectedPieceTargets = choice.SelectInvocations
                .Select(value => OccupiedObjectOf(session.Projection, value.Candidate))
                .Where(value => value.HasValue)
                .Select(value => value.Value)
                .Distinct()
                .ToArray();
            Assert.That(pieceCues.Keys, Is.EquivalentTo(expectedPieceTargets),
                "Every offered candidate naming a live piece is projected by stable identity, and " +
                "no coordinate marker or unrelated piece joins that set.");

            var promptControls = new HashSet<DirectorCommand>(
                prompt.EffectChoices.Select(value => value.Command));
            var answeredByAControl = new HashSet<DirectorCommand>(promptControls
                .Concat(prompt.EffectChoiceTargets.Select(value => value.Command))
                .Concat(prompt.EffectChoiceDestinations.Select(value => value.Command)));

            foreach (var invocation in choice.SelectInvocations)
            {
                var candidate = invocation.Candidate;
                var command = DirectorCommand.AnswerEffectChoice(invocation);
                var occupied = OccupiedObjectOf(session.Projection, candidate);
                if (occupied.HasValue)
                {
                    Assert.That(pieceCues.ContainsKey(occupied.Value), Is.True,
                        $"The candidate on option node '{candidate.OptionNodeId.Value}' names the piece " +
                        $"'{occupied.Value}', so that identity is marked and a click on it " +
                        "answers the choice.");
                    var cue = pieceCues[occupied.Value];
                    var preview = session.Projection.PendingEffectChoicePreview.Single(value =>
                        value.CandidateValue.Equals(candidate.Value));
                    Assert.That(cue.State,
                        Is.EqualTo(choice.SelectedCandidates.Contains(candidate)
                            ? BoardTargetCueState.Selected
                            : BoardTargetCueState.Eligible),
                        "Selection state is forwarded from the pending choice rather than inferred " +
                        "from hover or occupancy.");
                    Assert.That(cue.ConsequenceIntent,
                        Is.EqualTo(ExpectedBoardIntent(
                            session.Projection, choice.ChooserId, preview)),
                        "The board palette follows the authority preview's typed invoked consequence " +
                        "and reserves warm ivory for a teammate.");
                    Assert.That(promptControls, Has.No.Member(command),
                        $"and the candidate on option node '{candidate.OptionNodeId.Value}' keeps no " +
                        "prompt control, because two ways to say one thing is one of them redundant.");
                    continue;
                }

                var namesASpace =
                    candidate.Kind == EffectChoiceCandidateKind.Value &&
                    candidate.Value.Kind == EffectValueKind.Coordinate &&
                    boardDestinations.Contains(candidate.Value.Coordinate);
                Assert.That(namesASpace || answeredByAControl.Contains(command), Is.True,
                    $"The candidate on option node '{candidate.OptionNodeId.Value}' can be " +
                    "answered on the board or from the prompt. On neither, it is a question with no " +
                    "way to answer it.");
            }
        }

        /// <summary>
        /// The stable identity a candidate's own value names on the board, or null when it names no
        /// live piece.
        ///
        /// Read straight from projected occupancy, by value kind, so this case decides for itself what
        /// counts as naming an object rather than inheriting the production path's answer.
        /// </summary>
        private static BoardObjectReference? OccupiedObjectOf(
            MatchSessionProjection projection, EffectChoiceCandidate candidate)
        {
            if (candidate.Kind != EffectChoiceCandidateKind.Value)
            {
                return null;
            }

            switch (candidate.Value.Kind)
            {
                case EffectValueKind.Hero:
                    var hero = projection.Heroes.FirstOrDefault(value =>
                        value.HeroId.Equals(candidate.Value.HeroId));
                    return hero.HeroId.IsDefined
                        ? BoardObjectReference.ForHero(hero.HeroId)
                        : (BoardObjectReference?)null;
                case EffectValueKind.Minion:
                    var minion = projection.Minions.FirstOrDefault(value =>
                        value.MinionId.Equals(candidate.Value.MinionId));
                    return minion != null
                        ? BoardObjectReference.ForMinion(minion.MinionId)
                        : (BoardObjectReference?)null;
                case EffectValueKind.Coordinate:
                    var space = candidate.Value.Coordinate;
                    var heroAtSpace = projection.Heroes.FirstOrDefault(value =>
                        value.Coordinate.Equals(space));
                    if (heroAtSpace.HeroId.IsDefined)
                    {
                        return BoardObjectReference.ForHero(heroAtSpace.HeroId);
                    }

                    var minionAtSpace = projection.Minions.FirstOrDefault(value =>
                        value.Coordinate.Equals(space));
                    if (minionAtSpace != null)
                    {
                        return BoardObjectReference.ForMinion(minionAtSpace.MinionId);
                    }

                    var tokenAtSpace = projection.PlacedTokens.FirstOrDefault(value =>
                        value.Coordinate.Equals(space));
                    return tokenAtSpace != null
                        ? BoardObjectReference.ForToken(tokenAtSpace.Id)
                        : (BoardObjectReference?)null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// And for the Minion Battle, which moved onto the board at the same time. Here the rule cuts
        /// both ways: every legal selection must be markable, and a candidate the authority withholds -
        /// rulebook p.8 removes Heavy minions last - must not be, or the board offers what the rules
        /// refuse.
        /// </summary>
        [Test]
        public void TheMinionBattle_MarksEveryLegalSelectionAndNothingTheRulesWithhold()
        {
            var session = MinionChoicePresentationTests.CreatePendingRemovalSession();
            var view = new CapturingBoardInteractionView();
            var board = new BoardInteractionPresenter(session, view);
            var choice = session.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);

            board.Refresh();
            var model = view.Model;
            var marked = model.PieceTargetCues.Select(value => value.Target.MinionId).ToArray();

            Assert.That(marked, Is.EquivalentTo(
                    choice.LegalSelectMinionIds.Concat(choice.SelectedMinionIds).Distinct()),
                "Exactly the selections the authority allows, plus the ones already made.");
            Assert.That(model.PieceTargetCues, Has.All.Matches<PieceTargetCueViewModel>(value =>
                    value.Target.Kind == BoardObjectKind.Minion &&
                    value.ConsequenceIntent == BoardTargetConsequenceIntent.Removal &&
                    value.State == (choice.SelectedMinionIds.Contains(value.Target.MinionId)
                        ? BoardTargetCueState.Selected
                        : BoardTargetCueState.Eligible)),
                "Minion Battle projects only identity-keyed removal cues, with selected state read " +
                "from the authority's pending choice.");
            foreach (var withheld in choice.Candidates
                .Select(candidate => candidate.MinionId)
                .Where(id => id.IsDefined)
                .Except(choice.LegalSelectMinionIds)
                .Except(choice.SelectedMinionIds))
            {
                Assert.That(marked, Has.No.Member(withheld),
                    $"'{withheld.Value}' is a candidate the rules currently withhold, so the board " +
                    "must not offer it.");
            }
        }

        /// <summary>
        /// And for the Clear selection, which milestone 8.15a moved onto the board at the user's
        /// direction - the last choice whose subject stands on the map that still carried prompt
        /// controls. The rule is the Minion Battle's: every offered token markable where it stands
        /// and toggleable by a click on its space, no prompt control naming one, and the controls
        /// that stay are exactly the ones with no board surface - confirm and cancel.
        /// </summary>
        [Test]
        public void TheOpenClearSelection_IsAnsweredOnTheBoardAndKeepsNoCandidateControl()
        {
            var session = ClearPresentationTests.StartAttackTurnBesideAHazard(out var token, out _);
            var boardView = new CapturingBoardInteractionView();
            var board = new BoardInteractionPresenter(session, boardView);
            var roundView = new RoundActionUnityView();
            var round = new RoundActionPresenter(session, roundView);
            round.ClearSelectionChanged += selection => board.SetOpenClearSelection(selection);
            round.Refresh();
            Assert.That(round.SelectClear(roundView.Model.ClearOffers.Single()), Is.True);
            round.Refresh();
            board.Refresh();

            var selection = roundView.Model.ClearSelection;
            Assert.That(selection, Is.Not.Null, "The selection is open, or this asserts nothing.");
            Assert.That(selection.Candidates, Is.Not.Empty);

            foreach (var candidate in selection.Candidates)
            {
                var cue = boardView.Model.PieceTargetCues.SingleOrDefault(value =>
                    value.Target.Equals(BoardObjectReference.ForToken(candidate.TokenId)));
                Assert.That(cue, Is.Not.Null,
                    $"The offered token '{candidate.TokenId.Value}' is marked where it stands, " +
                    "before any pointer finds it.");
                Assert.That(cue.State, Is.EqualTo(candidate.IsSelected
                    ? BoardTargetCueState.Selected
                    : BoardTargetCueState.Eligible));
                Assert.That(cue.ConsequenceIntent,
                    Is.EqualTo(BoardTargetConsequenceIntent.Removal));
                Assert.That(round.ToggleClearTokenAtSpace(candidate.Space), Is.True,
                    "and a click on its space is what toggles it.");
                round.Refresh();
                board.Refresh();
            }

            Assert.That(roundView.Model.ClearSelection.SelectedTokenIds,
                Is.EquivalentTo(selection.Candidates.Select(value => value.TokenId)),
                "Every offered token was chosen by its own space click.");

            // Milestone 8.35v: END TURN is now drawn throughout the seat's own turn, an open Clear
            // selection included - it names no candidate at all, so its presence does not reopen
            // the gap this case is about (a control naming an offered token, which the board
            // already names). Pressing it composes `CancelClear` first (a pure presentation reset,
            // since nothing was ever submitted to the authority for an unconfirmed Clear) before
            // whatever the turn's own completion is - `RoundPresenter_EndTurnCancelsAnOpenClearSelectionFirst`
            // in `MatchFeaturePresenterTests` proves that composition; this case stays about the
            // board-first claim its own name makes.
            var drawnControls = ClearSectionButtons(roundView);
            Assert.That(drawnControls, Is.EquivalentTo(new[]
                {
                    selection.Confirm.Label, selection.CancelLabel, "END TURN"
                }),
                "The prompt keeps exactly the controls that have no board surface, plus the " +
                "omnipresent END TURN. A control naming an offered token would be a second way to " +
                "say what the board already says.");
        }

        /// <summary>
        /// Milestone 8.16c closes the space-valued choice missed by both board-first audits: p.8's
        /// blocked-Spawn-point Place is a paused system transition rather than an effect, correction,
        /// Hero Respawn, or Clear choice, so its destination buttons survived every family-specific
        /// conformance case. The prompt still explains the mandatory placement; the options themselves
        /// belong only to the board.
        /// </summary>
        [Test]
        public void APausedLanePushRespawnPlace_IsAnsweredOnlyOnTheBoard()
        {
            var description = MatchHudPresentationTests.LanePushPlacePause.Create()
                .Transitions.DescribePendingStep();
            Assert.That(description, Is.Not.Null);
            Assert.That(description.Kind, Is.EqualTo(SystemTransitionPromptKind.LanePushRespawnPlace));

            var contract = new PendingDecisionContract(
                PendingDecisionFamilies.SystemTransition,
                description.PendingStep.Invocations.Select(
                    DirectorCommand.AnswerSystemTransitionStep));

            var destinations = BoardInteractionPresenter.CreateSystemTransitionDestinations(
                description, contract);
            Assert.That(destinations.Select(value => value.Destination),
                Is.EqualTo(description.Options.Select(value => value.Destination)),
                "Every offered destination is projected in the continuation's own order.");
            Assert.That(destinations.Select(value => value.Command),
                Is.EqualTo(contract.Commands),
                "Each space carries the exact application command paired to that option; " +
                "presentation does not reconstruct one from the coordinate or option id.");

            var prompt = RoundActionPresenter.CreateSystemTransitionPrompt(
                description,
                Array.Empty<CardDefinition>(),
                Array.Empty<QuickGameHeroSetupState>(),
                contract);
            var view = new RoundActionUnityView();
            view.Render(new RoundActionViewModel(
                1,
                1,
                1,
                RoundResolutionPhase.EndOfRound,
                "Round 1 complete.",
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>(),
                systemTransitionPrompt: prompt));

            var buttons = Descendants(view.Root).OfType<Button>().ToArray();
            Assert.That(buttons, Is.Empty,
                "The Lane Push prompt explains the pause but a space-valued answer has no HUD " +
                "control; every PLACE AT button is a duplicate input surface beside the board.");
            Assert.That(Descendants(view.Root).OfType<Label>().Select(label => label.text),
                Has.Some.Contains("PLACES THE RESPAWNING MINION"),
                "Withdrawing destination controls must not withdraw who is deciding or why.");
        }

        /// <summary>
        /// Milestone 8.21. Every payment the removal-prevention authority offers produces exactly one
        /// control, and the decline it always projects produces one more.
        ///
        /// <para>This is the case milestone 8.19c1 could not write. It built the decision as rules only
        /// and recorded that no configured card could open it, so there was nothing to draw and nothing
        /// to check; Brogan's Shield, Bolster and Fortify are the first content that can open it, and
        /// <c>HasPendingResolution</c> refuses every other turn command while it stands. A payment the
        /// authority offered and the screen could not draw would therefore be a match that has stopped
        /// accepting input, which is exactly the shape milestone 8.11a wrote these cases for.</para>
        ///
        /// <para>It asserts reachability rather than appearance: one control per projected payment, in
        /// the projected order, each forwarding that payment's own minted command - never one this
        /// surface reconstructed from a source id or a card id.</para>
        /// </summary>
        [Test]
        public void EveryProjectedRemovalPreventionPayment_ProducesExactlyOneControl()
        {
            var prompt = MatchHudPresentationTests.RemovalPreventionPause.CreatePrompt();
            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.Payments, Is.Not.Empty,
                "The pause under test has to be one somebody can pay, or this checks an empty set.");

            var view = new RoundActionUnityView();
            view.Render(new RoundActionViewModel(
                1,
                1,
                1,
                RoundResolutionPhase.ResolvingTurn,
                RemovalPreventionPromptLabels.StatusLine(prompt),
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>(),
                removalPreventionPrompt: MatchHudPresentationTests.RemovalPreventionPause
                    .CreateViewModel(prompt)));

            var buttons = Descendants(view.Root).OfType<Button>().ToArray();
            Assert.That(buttons, Has.Length.EqualTo(prompt.Payments.Count + 1),
                "One control per affordable payment, plus the decline the rules always allow. A " +
                "payment with no control is an answer the authority accepts and the screen cannot " +
                "give, and the decision refuses every other command while it stands.");
            Assert.That(
                Descendants(view.Root).OfType<Label>().Select(label => label.text),
                Has.Some.Contains(RemovalPreventionPromptLabels.SectionLabel),
                "and they arrive under a heading that names the decision rather than the actions the " +
                "player is not being asked for.");
        }

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

        /// <summary>Every button label the rendered Clear section currently draws.</summary>
        private static string[] ClearSectionButtons(RoundActionUnityView view)
        {
            var labels = new List<string>();
            void Collect(UnityEngine.UIElements.VisualElement element)
            {
                if (element is UnityEngine.UIElements.Button button)
                {
                    labels.Add(button.text);
                }

                foreach (var child in element.Children())
                {
                    Collect(child);
                }
            }

            Collect(view.Root);
            return labels.ToArray();
        }

        /// <summary>
        /// Milestone 8.20a, and the defect its player-path journeys walked into. Every target a
        /// pending Attack lists as legal must be accepted when it is chosen - the same claim this
        /// fixture makes about Defense offers, on the choice that comes one step earlier.
        ///
        /// <para>The board is a Blue melee minion beside the attacker and the Blue hero adjacent to
        /// him, so one printed non-ranged Attack offers a hero and a minion at once. No fixture had
        /// that combination: milestone 8.9's session puts the hero at distance 2, and the hero-Defense
        /// fixtures reach their hero with a ranged Attack or across an empty space. Rulebook p.13's
        /// own worked example is explicit that the combination is legal - "he can target either
        /// Dodger to the north-east, or Wasp to the south-east" - so a hero the authority lists and
        /// then refuses is the authority disagreeing with itself, whichever half is corrected.</para>
        ///
        /// <para>Each candidate is chosen on its own session, because choosing one answers the choice
        /// and the next candidate would have nothing to be offered to.</para>
        /// </summary>
        [TestCase(false, TestName = "EveryLegalAttackTargetOffer_IsAcceptedWhenItIsChosen(mirror)")]
        [TestCase(true, TestName = "EveryLegalAttackTargetOffer_IsAcceptedWhenItIsChosen(stagedArien)")]
        public void EveryLegalAttackTargetOffer_IsAcceptedWhenItIsChosen(bool stagedArien)
        {
            var rejected = new List<string>();
            foreach (var chooseHero in new[] { true, false })
            {
                var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                    out _, stagedArien);
                ConfiguredDefenseCardTests.CommitRedAgainst(
                    session,
                    WaspCardContent.MagneticDagger,
                    stagedArien ? ArienCardContent.LiquidLeap : WaspCardContent.Shock);
                var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                    ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    ActionOf(session, candidate).Kind == ActionKind.Attack);
                Assert.That(
                    session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                    Is.True);

                var choice = session.Projection.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "The Attack must reach its target choice.");
                Assert.That(choice.LegalTargetHeroIds, Is.Not.Empty,
                    "The adjacent enemy hero must be offered, or this case holds over nothing.");
                Assert.That(choice.LegalTargetMinionIds, Is.Not.Empty,
                    "and so must the adjacent enemy minion.");

                var command = chooseHero
                    ? DirectorCommand.ChooseAttackTarget(
                        choice.ResolutionId, choice.ChoiceId, choice.LegalTargetHeroIds[0])
                    : DirectorCommand.ChooseAttackTarget(
                        choice.ResolutionId, choice.ChoiceId, choice.LegalTargetMinionIds[0]);
                var result = session.Execute(command);
                if (!result.Accepted)
                {
                    rejected.Add(
                        (chooseHero
                            ? "hero " + choice.LegalTargetHeroIds[0].Value
                            : "minion " + choice.LegalTargetMinionIds[0].Value) +
                        $" was offered and refused: {result.RejectionReason}/{result.TurnRejectionReason} {result.Detail}");
                }
            }

            Assert.That(rejected, Is.Empty,
                "An Attack target the authority offers must be one the authority takes:" +
                Environment.NewLine + string.Join(Environment.NewLine, rejected));
        }

        /// <summary>
        /// Milestone 8.20a, and the sibling of the case above one step further into p.14: a Defense
        /// the authority offers must be one it takes. The first case in this fixture proved every
        /// offer produces a control; this proves the control's own invocation is honoured, which is
        /// the half that was never asked and the half a player actually experiences.
        ///
        /// <para>Arien's three Duelist cards print p.14's Primary Defense Value, so the staged
        /// configuration offers a valued primary Defense beside ordinary secondary icons in one
        /// prompt - the combination no fixture had.</para>
        /// </summary>
        [TestCase(false, TestName = "EveryLegalDefenseOffer_IsAcceptedWhenItIsChosen(mirror)")]
        [TestCase(true, TestName = "EveryLegalDefenseOffer_IsAcceptedWhenItIsChosen(stagedArien)")]
        public void EveryLegalDefenseOffer_IsAcceptedWhenItIsChosen(bool stagedArien)
        {
            var offerCount = DefenseOfferCount(stagedArien);
            Assert.That(offerCount, Is.GreaterThanOrEqualTo(2),
                "The defender must be offered more than one Defense, or this proves one path.");

            var rejected = new List<string>();
            for (var index = 0; index < offerCount; index++)
            {
                var session = CreateDefenseResponseSession(stagedArien);
                var choice = session.Projection.PendingDefenseChoice;
                var offer = choice.LegalDefenseOffers[index];
                var result = session.Execute(DirectorCommand.ChooseDefenseCard(
                    choice.ResolutionId, choice.ChoiceId, offer.SourceCardInstanceId));
                if (!result.Accepted)
                {
                    rejected.Add(
                        $"{offer.SourceCardInstanceId.Value} ({offer.InvocationContext}) was " +
                        $"offered and refused: {result.RejectionReason}/{result.TurnRejectionReason} " +
                        result.Detail);
                }
            }

            Assert.That(rejected, Is.Empty,
                "A Defense the authority offers must be one the authority takes:" +
                Environment.NewLine + string.Join(Environment.NewLine, rejected));
        }

        private static int DefenseOfferCount(bool stagedArien) =>
            CreateDefenseResponseSession(stagedArien)
                .Projection.PendingDefenseChoice.LegalDefenseOffers.Count;

        /// <summary>
        /// A session stopped on p.14's Defense response, with the defender holding every Defense shape
        /// their printed set gives them.
        /// </summary>
        private static MatchSession CreateDefenseResponseSession(bool stagedArien)
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien);
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session,
                WaspCardContent.MagneticDagger,
                stagedArien ? ArienCardContent.LiquidLeap : WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(
                session, ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue));
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response.");
            return session;
        }

        private static BoardTargetConsequenceIntent ExpectedBoardIntent(
            IMatchReadModel projection,
            HeroId chooserId,
            EffectChoiceCandidatePreview preview)
        {
            if (preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Removal)
            {
                return BoardTargetConsequenceIntent.Removal;
            }

            if (preview.ConsequenceIntent == EffectChoiceConsequenceIntent.Attack)
            {
                return BoardTargetConsequenceIntent.Attack;
            }

            var chooserSide = projection.Participants.GetTeamForHero(chooserId).Side;
            var isTeammate = preview.Occupant.Kind == BoardObjectKind.Hero
                ? projection.Participants.GetTeamForHero(preview.Occupant.HeroId).Side == chooserSide
                : preview.Occupant.Kind == BoardObjectKind.Minion
                    ? projection.Minions.Single(value =>
                        value.MinionId.Equals(preview.Occupant.MinionId)).TeamSide == chooserSide
                    : preview.Occupant.Kind == BoardObjectKind.Token &&
                      projection.Participants.GetTeamForHero(projection.PlacedTokens.Single(value =>
                          value.Id.Equals(preview.Occupant.TokenInstanceId)).ControllerHeroId).Side == chooserSide;
            return isTeammate
                ? BoardTargetConsequenceIntent.Ally
                : BoardTargetConsequenceIntent.Affect;
        }

        private static ActionDefinition ActionOf(IMatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static bool IsBlock(IMatchSession session, ActionOffer offer) =>
            GuardsDigital.Core.Rules.CardResolution.DefenseBlockQuery.IsBlock(
                offer, session.Projection.Definitions);


        private sealed class CapturingBoardInteractionView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }

            public event Action<HeroId> HeroSubmitted;
            public event Action<MinionId> MinionSubmitted;
            public event Action<HexCoordinate> DestinationSubmitted;

            public void Render(BoardInteractionViewModel model) => Model = model;
        }
    }
}

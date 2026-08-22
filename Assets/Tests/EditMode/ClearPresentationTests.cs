using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.3a: the player-facing surface over milestone 6.3's implemented Clear.
    ///
    /// Clear has been an authoritative action since 6.3 - an offer, a validation, a command and a
    /// resolution - and 6.3 deliberately shipped no UI for it, so the configured HUD projected no
    /// entry and a player had no way to submit the command that already existed. Nothing about the
    /// rule moves here. What these cases hold is the seam: which controls the projection produces,
    /// that opening the selection commits nothing, that the confirm is a projected invocation rather
    /// than a payload presentation composed, and that the surface cannot outlive the position it was
    /// read from.
    ///
    /// The board cue half is here too, because the two are one correction. A control that names a
    /// token reports the token's stable identity, and the board resolves that identity against the
    /// projected token list alone - never by display name, and never by asking which piece stands on a
    /// space. Milestone 6.6a's handoff finding was exactly that gap.
    /// </summary>
    public sealed class ClearPresentationTests
    {
        private static readonly GuardsDigital.Core.Effects.EffectNodeId PlaceBranch =
            new GuardsDigital.Core.Effects.EffectNodeId("component-place-branch");

        [Test]
        public void ACardWithNoReplaceableAttack_OffersNoClearControlAtAll()
        {
            var session = CreateComponentSession();
            CommitComponentCards(session);

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(session.Projection.AvailableClearOptions, Is.Empty,
                "The Cache card prints a Skill and a Movement and no Attack, so there is nothing to " +
                "replace and the absence is the projection's answer rather than a view's.");
            Assert.That(model.ClearOffers, Is.Empty);
            Assert.That(model.ClearSelection, Is.Null);
        }

        [Test]
        public void AnAvailableReplaceableAttack_OffersOneClearBoundToItsCardActorAndAction()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out var actorId);

            var option = session.Projection.AvailableClearOptions.Single();
            var offer = RoundActionPresenter.CreateViewModel(session.Projection).ClearOffers.Single();

            var attack = AttackOffer(session);
            Assert.That(option.ReplacedAttackActionDefinitionId, Is.EqualTo(attack.ActionDefinitionId));
            Assert.That(option.ReplacementOffer.SourceCardInstanceId,
                Is.EqualTo(attack.SourceCardInstanceId));
            Assert.That(option.ReplacementOffer.ActorId, Is.EqualTo(actorId));
            Assert.That(option.AdjacentTokens.Select(value => value.Id), Is.EqualTo(new[] { token.Id }));
            Assert.That(offer.Label, Is.EqualTo("CLEAR (REPLACES ATTACK)"),
                "The control says what it costs: Clear replaces the card's Attack.");
            Assert.That(offer.ReplacedAttackActionDefinitionId, Is.EqualTo(attack.ActionDefinitionId));
            Assert.That(offer.SourceCardInstanceId, Is.EqualTo(attack.SourceCardInstanceId));
            Assert.That(offer.ActorId, Is.EqualTo(actorId));
            Assert.That(offer.CandidateCount, Is.EqualTo(1));
        }

        /// <summary>
        /// One adjacent token means two confirmable subsets and therefore two commands, and both are
        /// the projection's. Presentation never builds a `PerformClear` payload, which is why the
        /// enumeration lives where the candidates do.
        /// </summary>
        [Test]
        public void TheProjection_CarriesOneCanonicalInvocationPerConfirmableSubset()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var option = session.Projection.AvailableClearOptions.Single();

            Assert.That(option.SubsetInvocations.Count, Is.EqualTo(2));
            Assert.That(option.SubsetInvocations[0].TokenInstanceIds, Is.Empty,
                "p.13 permits any number, so clearing none is an invocation rather than a decline.");
            Assert.That(option.SubsetInvocations[1].TokenInstanceIds,
                Is.EqualTo(new[] { token.Id }));
            Assert.That(
                option.SubsetInvocations.All(invocation =>
                    invocation.Kind == DirectorCommandKind.PerformClear &&
                    invocation.ActionDefinitionId.Equals(option.ReplacedAttackActionDefinitionId)),
                Is.True);
            Assert.That(option.FindInvocation(new[] { token.Id }),
                Is.SameAs(option.SubsetInvocations[1]));
            Assert.That(option.FindInvocation(Array.Empty<TokenInstanceId>()),
                Is.SameAs(option.SubsetInvocations[0]));
            Assert.That(option.FindInvocation(new[] { new TokenInstanceId("not-offered") }), Is.Null,
                "A set this option does not offer matches nothing, rather than being composed anyway.");
        }

        [Test]
        public void EnteringClearSelection_ShowsTheAdjacentTokensAndSubmitsNothing()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var presenter = CreatePresenter(session, out var view);
            var before = session.Projection;
            var acceptedBefore = session.Projection.AcceptedCommands.Count;

            presenter.Refresh();
            Assert.That(presenter.SelectClear(view.Model.ClearOffers.Single()), Is.True);
            presenter.Refresh();

            var selection = view.Model.ClearSelection;
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.Candidates.Select(value => value.TokenId),
                Is.EqualTo(new[] { token.Id }));
            Assert.That(selection.Candidates.Single().Label, Is.EqualTo("TEST - HAZARD"),
                "The candidate is named from the projected definition, with no ordinal because the " +
                "offered set says that name once.");
            Assert.That(selection.Candidates.Single().Space, Is.EqualTo(token.Coordinate));
            Assert.That(selection.Candidates.Single().IsSelected, Is.False);
            Assert.That(selection.Confirm.Label, Is.EqualTo("CONFIRM CLEAR"));
            Assert.That(selection.CancelLabel, Is.EqualTo("CANCEL CLEAR"));
            Assert.That(selection.Confirm.Command.TokenInstanceIds, Is.Empty,
                "Nothing is selected yet, and confirming that is legal.");
            Assert.That(session.Projection, Is.SameAs(before),
                "Opening the selection accepts no command and changes no authoritative state.");
            Assert.That(session.Projection.AcceptedCommands.Count, Is.EqualTo(acceptedBefore));
        }

        [Test]
        public void SelectingAndDeselectingAToken_MovesOnlyWhichInvocationTheConfirmCarries()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();

            Assert.That(presenter.ToggleClearToken(view.Model.ClearSelection.Candidates.Single()), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection.Candidates.Single().IsSelected, Is.True);
            Assert.That(view.Model.ClearSelection.SelectedTokenIds, Is.EqualTo(new[] { token.Id }));
            Assert.That(view.Model.ClearSelection.Confirm.Command.TokenInstanceIds,
                Is.EqualTo(new[] { token.Id }));

            Assert.That(presenter.ToggleClearToken(view.Model.ClearSelection.Candidates.Single()), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection.Candidates.Single().IsSelected, Is.False);
            Assert.That(view.Model.ClearSelection.Confirm.Command.TokenInstanceIds, Is.Empty);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.Not.EqualTo(DirectorCommandKind.PerformClear),
                "Every one of those was local; nothing reached the session.");
        }

        [Test]
        public void ConfirmingASelectedToken_ForwardsTheProjectedInvocationAndTakesExactlyThatPiece()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();
            presenter.ToggleClearToken(view.Model.ClearSelection.Candidates.Single());
            presenter.Refresh();
            var expected = view.Model.ClearSelection.Confirm.Command;
            var sourceCardId = AttackOffer(session).SourceCardInstanceId;

            Assert.That(presenter.ConfirmClear(), Is.True);

            Assert.That(session.Projection.AcceptedCommands.Last(), Is.EqualTo(expected),
                "The command the session accepted is the invocation the projection built, unchanged.");
            Assert.That(session.Projection.PlacedTokens, Is.Empty);
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "Clear replaces the whole Attack and resolves the card, exactly as milestone 6.3 has.");
            presenter.Refresh();
            Assert.That(view.Model.ClearSelection, Is.Null,
                "The accepted command moved the projection on, so the surface is rebuilt without it.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PlacedTokens.Single().Id, Is.EqualTo(token.Id),
                "Undo restores the piece, because the removal was authoritative rather than a view state.");
        }

        [Test]
        public void ConfirmingTheEmptySubset_ResolvesTheReplacementAndTakesNoPiece()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();
            var sourceCardId = AttackOffer(session).SourceCardInstanceId;

            Assert.That(presenter.ConfirmClear(), Is.True);

            Assert.That(session.Projection.PlacedTokens.Single().Id, Is.EqualTo(token.Id),
                "An unselected token stays where it was.");
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void CancellingClear_SubmitsNothingAndPutsTheActionChoiceBack()
        {
            var session = StartAttackTurnBesideAHazard(out _, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            var actionsBefore = view.Model.Actions.Select(value => value.Command).ToArray();
            var before = session.Projection;
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();
            presenter.ToggleClearToken(view.Model.ClearSelection.Candidates.Single());
            presenter.Refresh();

            Assert.That(presenter.CancelClear(), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection, Is.Null);
            Assert.That(view.Model.ClearOffers, Has.Count.EqualTo(1),
                "The replacement is still offered; only the open selection went away.");
            Assert.That(view.Model.Actions.Select(value => value.Command), Is.EqualTo(actionsBefore),
                "The authoritative action choice is the one it always was.");
            Assert.That(session.Projection, Is.SameAs(before));
            Assert.That(presenter.CancelClear(), Is.False,
                "There is nothing left to cancel, and saying so is not the same as cancelling twice.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.8b: a Clear that can remove nothing
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.13's Clear removes "any number of tokens adjacent to you". With no token adjacent
        /// there is no number to choose, so the replacement can do nothing whatever the hero picks -
        /// which is milestone 6.5af's judgement about an inert Skill, and gets 6.5af's treatment.
        /// </summary>
        [Test]
        public void AClearWithNoAdjacentToken_IsOfferedGreyedAndCarriesNoCommand()
        {
            var session = StartAttackTurnWithNothingToClear(out var actorId);
            var option = session.Projection.AvailableClearOptions.Single();

            Assert.That(option.AdjacentTokens, Is.Empty,
                "The fixture has to reach an Attack turn with nothing beside the hero, or this case " +
                "holds over the wrong position.");
            Assert.That(option.ReplacementOffer.IsAvailable, Is.False,
                "The authority decides it, on the same field milestones 6.5af, 7.8a1, 8.4 and 8.7 " +
                "decide theirs on.");
            Assert.That(option.ReplacementOffer.ActorId, Is.EqualTo(actorId));

            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            var offer = view.Model.ClearOffers.Single();

            Assert.That(offer.IsAvailable, Is.False,
                "Presentation reads the authority's stamp rather than counting candidates itself.");
            Assert.That(offer.CandidateCount, Is.EqualTo(0));
            Assert.That(offer.Label, Is.EqualTo("CLEAR (REPLACES ATTACK)"),
                "It keeps its label and its place, which is what every other refused control here does.");
            Assert.That(presenter.SelectClear(offer), Is.False,
                "and the selection cannot be opened on it, so no surface enters a mode the authority " +
                "would not complete.");
            presenter.Refresh();
            Assert.That(view.Model.ClearSelection, Is.Null);
        }

        /// <summary>
        /// The drawn control, on the HUD's own view rather than on the capturing test view: greyed
        /// through opacity as well as through being inert, so the state is never carried by colour or by
        /// inertness alone, and still labelled and still in its place.
        /// </summary>
        [Test]
        public void TheGreyedClearControl_KeepsItsLabelAndReadsAsUnavailable()
        {
            var greyed = StartAttackTurnWithNothingToClear(out _);
            var greyedView = new RoundActionUnityView();
            new RoundActionPresenter(greyed, greyedView).Refresh();
            var greyedControl = Descendants(greyedView.Root)
                .Single(element => element.name == "gd-clear-offer");

            Assert.That(greyedControl.enabledSelf, Is.False);
            Assert.That(greyedControl.style.opacity.value, Is.EqualTo(0.45f).Within(0.001f),
                "Milestone 6.5ac's rule: unavailability is never carried by inertness alone, so the " +
                "shared dimming is applied explicitly.");
            Assert.That(greyedControl.tooltip, Does.Contain("could remove nothing"));
            Assert.That(
                Descendants(greyedView.Root).OfType<UnityEngine.UIElements.Button>()
                    .Any(button => button.text == "CLEAR (REPLACES ATTACK)"),
                Is.True,
                "It keeps its label and its place rather than leaving the surface.");

            var available = StartAttackTurnBesideAHazard(out _, out _);
            var availableView = new RoundActionUnityView();
            new RoundActionPresenter(available, availableView).Refresh();
            var availableControl = Descendants(availableView.Root)
                .Single(element => element.name == "gd-clear-offer");

            Assert.That(availableControl.enabledSelf, Is.True);
            Assert.That(availableControl.style.opacity.value, Is.Not.EqualTo(0.45f).Within(0.001f),
                "and an offer the authority does allow is never dimmed - the shared treatment is " +
                "applied to one of these two controls and not the other.");
            Assert.That(availableControl.tooltip, Does.Not.Contain("could remove nothing"));
        }

        [Test]
        public void AStartNamingAClearThatCanRemoveNothing_IsRefusedAndChangesNothing()
        {
            var session = StartAttackTurnWithNothingToClear(out _);
            var option = session.Projection.AvailableClearOptions.Single();
            var sourceCardId = AttackOffer(session).SourceCardInstanceId;
            var boardBefore = session.Projection.PlacedTokens.Select(value => value.Id).ToArray();
            var versionBefore = session.Projection.Version;

            Assert.That(option.SubsetInvocations, Is.Empty,
                "A greyed offer carries no command at all rather than one the rules would reject, " +
                "which is what keeps \"the projection offers only what the authority accepts\" true.");
            Assert.That(option.FindInvocation(Array.Empty<TokenInstanceId>()), Is.Null);

            // Forged rather than projected, because the projection no longer offers it: this is the
            // exact command that was accepted here before milestone 8.8b, submitted directly.
            var result = session.Execute(DirectorCommand.PerformClear(
                option.ReplacedAttackActionDefinitionId, Array.Empty<TokenInstanceId>()));

            Assert.That(result.Accepted, Is.False,
                "The start is refused atomically rather than resolving a card on an action that could " +
                "do nothing.");
            Assert.That(session.Projection.Version, Is.EqualTo(versionBefore),
                "A refused start advances nothing, so there was nothing to roll back.");
            Assert.That(session.Projection.PlacedTokens.Select(value => value.Id),
                Is.EqualTo(boardBefore));
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "and the card is still the hero's to spend - p.12's Hold is what makes the refusal " +
                "cost them nothing.");
        }

        /// <summary>
        /// The other half of the rule, and the one that keeps this from being a rules change: p.13's
        /// "any number" includes zero, so a hero standing beside a token may still clear none. What
        /// 8.8b refuses is a Clear that *cannot* remove anything, never one whose chooser declines to.
        /// </summary>
        [Test]
        public void AClearBesideAToken_StaysAvailableAndStillConfirmsTheEmptySubset()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var option = session.Projection.AvailableClearOptions.Single();
            var sourceCardId = AttackOffer(session).SourceCardInstanceId;

            Assert.That(option.AdjacentTokens.Select(value => value.Id), Is.EqualTo(new[] { token.Id }));
            Assert.That(option.ReplacementOffer.IsAvailable, Is.True);

            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            Assert.That(view.Model.ClearOffers.Single().IsAvailable, Is.True);
            Assert.That(presenter.SelectClear(view.Model.ClearOffers.Single()), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection.Candidates.Single().TokenId, Is.EqualTo(token.Id));
            Assert.That(presenter.ConfirmClear(), Is.True,
                "Confirming none beside a token is still p.13's own answer.");
            Assert.That(session.Projection.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// A confirm read from an older model names a subset of a position that has moved on. The
        /// presenter refuses it for the reason it refuses every stale invocation: it validates against
        /// the model it currently projects, and this one is not in it.
        /// </summary>
        [Test]
        public void AStaleClearConfirm_IsRefusedAfterTheProjectionMoves()
        {
            var session = StartAttackTurnBesideAHazard(out _, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();
            presenter.ToggleClearToken(view.Model.ClearSelection.Candidates.Single());
            presenter.Refresh();
            var stale = view.Model.ClearSelection.Confirm;

            // Any accepted command rebuilds the projection. End Turn is the one always available here.
            Assert.That(presenter.Submit(view.Model.EndTurnInvocation), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection, Is.Null);
            Assert.That(presenter.Submit(stale), Is.False,
                "A Clear confirm the current model does not project cannot reach the session.");
            Assert.That(session.Projection.PlacedTokens, Has.Count.EqualTo(1),
                "And the piece it named is untouched.");
        }

        [Test]
        public void AClearControlForAnOfferTheModelDoesNotCarry_IsRefusedRatherThanOpened()
        {
            var session = StartAttackTurnBesideAHazard(out _, out _);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            var real = view.Model.ClearOffers.Single();
            var forged = new ClearOfferViewModel(
                "CLEAR (REPLACES ATTACK)",
                new ActionDefinitionId("not-an-offered-action"),
                real.SourceCardInstanceId,
                real.ActorId,
                1);

            Assert.That(presenter.SelectClear(forged), Is.False);
            Assert.That(presenter.SelectClear(null), Is.False);
            presenter.Refresh();
            Assert.That(view.Model.ClearSelection, Is.Null);
        }

        /// <summary>
        /// The board half. A control reports the token's identity; the board resolves it against the
        /// projected list and marks that piece with the shared cue. A selected piece keeps its own
        /// treatment while the pointer moves on, which is the rule the minion prompt set.
        /// </summary>
        [Test]
        public void PointingAtAClearTokenControl_MarksThatExactPieceAndSelectionOutranksIt()
        {
            var session = StartAttackTurnBesideAHazard(out var token, out _);
            var board = new CapturingBoardView();
            var boardPresenter = new BoardInteractionPresenter(session, board);
            var presenter = CreatePresenter(session, out var view);
            presenter.Refresh();
            presenter.SelectClear(view.Model.ClearOffers.Single());
            presenter.Refresh();
            boardPresenter.Refresh();

            Assert.That(board.Model.TokenChoiceCues, Is.Empty,
                "Nothing is pointed at and nothing is chosen, so the board says nothing.");

            Assert.That(boardPresenter.HighlightTokenChoiceCandidate(token.Id), Is.True);
            boardPresenter.Refresh();

            var pointed = board.Model.TokenChoiceCues.Single();
            Assert.That(pointed.TokenId, Is.EqualTo(token.Id));
            Assert.That(pointed.Kind, Is.EqualTo(MinionChoiceCueKind.Highlighted));

            Assert.That(boardPresenter.SetSelectedClearTokens(new[] { token.Id }), Is.True);
            boardPresenter.Refresh();

            Assert.That(board.Model.TokenChoiceCues.Single().Kind,
                Is.EqualTo(MinionChoiceCueKind.Selected),
                "A chosen piece keeps saying so while the pointer is still on its control.");

            Assert.That(boardPresenter.HighlightTokenChoiceCandidate(default), Is.True);
            boardPresenter.Refresh();

            Assert.That(board.Model.TokenChoiceCues.Single().Kind,
                Is.EqualTo(MinionChoiceCueKind.Selected),
                "Moving the pointer away leaves the selection's own cue standing.");
        }

        [Test]
        public void AnIdentityTheProjectionDoesNotCarry_MarksNothing()
        {
            var session = StartAttackTurnBesideAHazard(out _, out _);
            var board = new CapturingBoardView();
            var boardPresenter = new BoardInteractionPresenter(session, board);

            Assert.That(
                boardPresenter.HighlightTokenChoiceCandidate(new TokenInstanceId("gone")), Is.False);
            Assert.That(
                boardPresenter.SetSelectedClearTokens(new[] { new TokenInstanceId("gone") }), Is.False);
            boardPresenter.Refresh();

            Assert.That(board.Model.TokenChoiceCues, Is.Empty,
                "A stale control names a piece that is no longer drawn, and marks nothing.");
        }

        [Test]
        public void TheClearSurface_IsNotOfferedWhileAnActionIsAlreadyResolving()
        {
            var session = StartAttackTurnBesideAHazard(out _, out _);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Movement);
            Accept(session, DirectorCommand.StartAction(movement.ActionDefinitionId));

            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null);
            Assert.That(session.Projection.AvailableClearOptions, Is.Empty);
            Assert.That(RoundActionPresenter.CreateViewModel(session.Projection).ClearOffers, Is.Empty,
                "Core refuses a second action while one is in progress, so a control that could only " +
                "produce a rejection is not drawn.");
        }

        private static IEnumerable<UnityEngine.UIElements.VisualElement> Descendants(
            UnityEngine.UIElements.VisualElement element)
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

        private static RoundActionPresenter CreatePresenter(
            MatchSession session, out CapturingRoundView view)
        {
            view = new CapturingRoundView();
            return new RoundActionPresenter(session, view);
        }

        private static ActionOffer AttackOffer(MatchSession session) =>
            session.Projection.AvailableTurnActions.Single(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);

        /// <summary>
        /// Plays milestone 6.2's Cache Skill to put a Hazard beside the acting hero, then reaches the
        /// next round with that same hero committing a card that prints an Attack. It is the position
        /// milestone 6.3's own command fixture reaches, which is the point: the surface under test is
        /// over an action that already worked.
        /// </summary>
        /// <summary>
        /// The same position with the Hazard never placed: the acting hero reaches a later turn of the
        /// same round holding a card that prints a replaceable Attack, with nothing adjacent to clear.
        ///
        /// Milestone 8.8b. It spends the first turn on p.12's Hold instead of the Cache Skill, which is
        /// the whole difference from <see cref="StartAttackTurnBesideAHazard"/> - so a case that fails
        /// here and passes there is failing on the token rather than on anything else about the turn.
        /// </summary>
        private static MatchSession StartAttackTurnWithNothingToClear(out HeroId actorId)
        {
            var session = CreateComponentSession();
            CommitComponentCards(session);
            actorId = session.Projection.SelectedActor.ActorId;
            AdvanceToNextTurnInSameRound(session);
            CommitAttackTurnForActor(session, actorId);
            Assert.That(session.Projection.PlacedTokens, Is.Empty,
                "No Skill placed one and p.8's cleanup has not run, so the board carries no token at all.");
            return session;
        }

        private static MatchSession StartAttackTurnBesideAHazard(
            out PlacedToken token, out HeroId actorId)
        {
            var session = CreateComponentSession();
            CommitComponentCards(session);
            var actor = session.Projection.SelectedActor.ActorId;
            actorId = actor;
            Accept(session, DirectorCommand.StartAction(
                new ActionDefinitionId("primary-component-skill")));
            ChooseOption(session, PlaceBranch);

            var actorCoordinate = session.Projection.Heroes
                .Single(hero => hero.HeroId.Equals(actor)).Coordinate;
            var adjacent = session.Projection.PendingEffectChoice.SelectInvocations
                .First(invocation => actorCoordinate.GetNeighbors()
                    .Contains(invocation.Candidate.Value.Coordinate));
            Accept(session, DirectorCommand.AnswerEffectChoice(adjacent));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            token = session.Projection.PlacedTokens.Single();

            // Milestone 6.12: a later turn of the same round, not the next one. p.8's end-of-round
            // cleanup returns every placed token, so a fixture that crossed the boundary would reach
            // its Attack turn with nothing left to Clear.
            AdvanceToNextTurnInSameRound(session);
            CommitAttackTurnForActor(session, actor);
            return session;
        }

        private static void ChooseOption(
            MatchSession session, GuardsDigital.Core.Effects.EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(option))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void CommitAttackTurnForActor(MatchSession session, HeroId actorId)
        {
            var actorSide = session.Projection.Participants.GetTeamForHero(actorId).Side;
            var opposingSide = actorSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
            var actorCard = HandCards(session, actorSide)
                .Where(card => Definition(session, card).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Attack))
                .OrderByDescending(card => Definition(session, card).Initiative.Value)
                .First();
            // The opponent commits their lowest, so the card under test acts whatever the showcase
            // conversions have done to the top of either band.
            var opposingCard = HandCards(session, opposingSide)
                .OrderBy(card => Definition(session, card).Initiative.Value)
                .First();
            Accept(session, DirectorCommand.CommitCard(actorCard.OwnerId, actorCard.Id));
            Accept(session, DirectorCommand.CommitCard(opposingCard.OwnerId, opposingCard.Id));
            ResolveInitiativeTie(session);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actorId));
        }

        /// <summary>
        /// Finishes the current turn and stops at the next turn's commitments, asserting the round did
        /// not turn over. Since milestone 6.12 that distinction decides whether a placed token is still
        /// standing when the fixture reaches its Attack.
        /// </summary>
        private static void AdvanceToNextTurnInSameRound(MatchSession session)
        {
            var round = session.Projection.RoundIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(round),
                "The Hazard must not cross a round boundary, or p.8's cleanup takes it before Clear can.");
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
        }

        private static void CommitAnyEligibleCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side).FirstOrDefault();
                if (card != null)
                {
                    Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
                    continue;
                }

                Accept(session, DirectorCommand.Pass(
                    session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId));
            }

            ResolveInitiativeTie(session);
        }

        private static void CommitComponentCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side)
                    .Single(candidate => candidate.Id.Value.Contains("cache"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            ResolveInitiativeTie(session);
        }

        private static void ResolveInitiativeTie(MatchSession session)
        {
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static CardDefinition Definition(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));

        private static CardInstance[] HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateComponentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class CapturingRoundView : IRoundActionView
        {
            public RoundActionViewModel Model { get; private set; }
            public void Render(RoundActionViewModel model) => Model = model;
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }
    }
}

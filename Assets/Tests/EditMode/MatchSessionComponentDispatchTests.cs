using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2 acceptance check 3: token placement, token reclamation, and marker attachment are
    /// authoritative choices and transitions, executed end to end through <see cref="IMatchSession"/>.
    ///
    /// Every decision here rides <c>AnswerEffectChoice</c> on milestone 6.2a's frame. That is the point
    /// of the ordering amendment that put 6.2a first: this milestone adds no <c>DirectorCommandKind</c>
    /// member, no pending-choice class, no projection property, and no continuation arm of its own, and
    /// the fixture below is what shows the frame actually carries the weight.
    ///
    /// Tokens were read back here by deserializing the authoritative snapshot rather than from a
    /// projection field, because 6.2's non-goals barred a token-specific projection property, and this
    /// comment predicted that what the wire carries is exactly what a later presentation milestone
    /// would project. Milestone 6.2d is that milestone, and it redeemed the prediction rather than
    /// correcting it: <c>MatchSessionProjection.PlacedTokens</c> hands over the board authority's own
    /// instances, and the case below pins the two readings against each other so neither can drift.
    /// The snapshot readback stays, because it is the only one that proves what crossed the wire.
    /// Milestone 6.3 reuses that same readback to prove Clear's authority-level command, diagnostic
    /// facts, source-card completion, retained-prefix Undo, and deterministic replay.
    /// </summary>
    public sealed class MatchSessionComponentDispatchTests
    {
        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("component-place-branch");
        private static readonly EffectNodeId GiveBranch = new EffectNodeId("component-give-marker");
        private static readonly EffectNodeId ReturnBranch = new EffectNodeId("component-return-marker");

        [Test]
        public void TheComponentSkill_OpensAChooserOwnedSelectionAmongItsOperations()
        {
            var session = StartComponentSkill(out _);

            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.ChooserId, Is.EqualTo(session.Projection.SelectedActor.ActorId),
                "The acting hero owns the decision; the resolver never picks for a player.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { PlaceBranch, GiveBranch, ReturnBranch }),
                "Options are offered in the definition's own order.");
        }

        [Test]
        public void PlacingAToken_LeavesTheSpaceOccupiedAndTheSupplyReduced()
        {
            var session = StartComponentSkill(out var actorId);
            ChooseOption(session, PlaceBranch);

            var spaces = session.Projection.PendingEffectChoice;
            Assert.That(spaces, Is.Not.Null, "Which empty space receives the token is an authoritative choice.");
            Assert.That(spaces.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            var chosen = spaces.Candidates[0].Value.Coordinate;

            Accept(session, DirectorCommand.AnswerEffectChoice(spaces.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var board = RestoreBoard(session);
            var token = board.PlacedTokens.Single();

            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
            Assert.That(token.Coordinate, Is.EqualTo(chosen));
            Assert.That(token.OwnerHeroId, Is.EqualTo(actorId));
            Assert.That(token.Definition.Reference, Is.EqualTo(SyntheticComponentDefinitions.HazardV4));
            Assert.That(board.IsEmptyPlayableSpace(chosen), Is.False,
                "A placed token occupies its space for every consumer of the shared occupancy index.");
            Assert.That(
                SyntheticComponentDefinitions.CreateCatalog().CountAvailableTokens(
                    SyntheticComponentDefinitions.HazardV4, board.PlacedTokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount - 1));
        }

        /// <summary>
        /// Milestone 6.2d's first acceptance check, and the redemption of this fixture's own
        /// prediction. The projection carries the board authority's instances rather than a summary of
        /// them, so what a reader sees is the identity, the coordinate and the trusted definition the
        /// board already holds - checked here against the same tokens read back off the wire, which is
        /// the only comparison that can catch the projection quietly deriving something of its own.
        /// </summary>
        [Test]
        public void TheProjection_CarriesEveryPlacedTokenTheWireDoes()
        {
            var session = StartComponentSkill(out _);
            Assert.That(session.Projection.PlacedTokens, Is.Empty,
                "Nothing has been placed yet, and a projection invents no piece.");

            ChooseOption(session, PlaceBranch);
            var spaces = session.Projection.PendingEffectChoice;
            var chosen = spaces.Candidates[0].Value.Coordinate;
            Accept(session, DirectorCommand.AnswerEffectChoice(spaces.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var projected = session.Projection.PlacedTokens.Single();
            var authoritative = RestoreBoard(session).PlacedTokens.Single();

            Assert.That(projected.Id, Is.EqualTo(authoritative.Id));
            Assert.That(projected.Coordinate, Is.EqualTo(chosen));
            Assert.That(projected.Definition,
                Is.SameAs(SyntheticComponentDefinitions.HazardDefinition),
                "The trusted definition object travels with the instance, which is how a reader " +
                "names a token without ever reaching for the component catalog.");
            Assert.That(projected.Definition.DisplayName, Is.EqualTo("TEST - Hazard"));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PlacedTokens, Is.Empty,
                "Undo takes the piece off the board, and the projection says so.");
        }

        [Test]
        public void GivingAMarker_AttachesItToTheChosenHolderWithoutTouchingTheBoard()
        {
            var session = StartComponentSkill(out var actorId);
            var occupiedBefore = RestoreBoard(session).PlacedTokens.Count;
            ChooseOption(session, GiveBranch);

            var holders = session.Projection.PendingEffectChoice;
            Assert.That(holders, Is.Not.Null, "Who receives the marker is the attachment decision.");
            Assert.That(holders.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            var holder = holders.Candidates
                .Select(candidate => candidate.Value.HeroId)
                .First(hero => !hero.Equals(actorId));
            var invocation = holders.SelectInvocations.Single(candidate =>
                candidate.Candidate.Value.HeroId.Equals(holder));

            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var attachment = RestoreMarkers(session).Attachments.Single();

            Assert.That(attachment.HolderHeroId, Is.EqualTo(holder),
                "Rulebook p.18: markers are usually given to other heroes.");
            Assert.That(attachment.OwnerHeroId, Is.EqualTo(actorId));
            Assert.That(attachment.ReturnCondition, Is.EqualTo(MarkerReturnCondition.GeneralCleanup));
            Assert.That(attachment.Definition,
                Is.SameAs(SyntheticComponentDefinitions.FocusDefinition),
                "Restore rebinds the marker to the catalog's own definition; the payload never says " +
                "what the marker means.");
            Assert.That(RestoreBoard(session).PlacedTokens.Count, Is.EqualTo(occupiedBefore),
                "A marker changes no board state.");
        }

        [Test]
        public void AShortSupply_OpensTheRulebookReclamationChoiceBeforePlacing()
        {
            // Two placements exhaust the synthetic two-token supply, so the third must reclaim first.
            var session = PlaceTokensUntilSupplyIsExhausted(out var placed);
            Assert.That(placed, Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount));

            StartComponentSkillOnCurrentTurn(session);
            ChooseOption(session, PlaceBranch);

            var reclamation = session.Projection.PendingEffectChoice;
            Assert.That(reclamation, Is.Not.Null,
                "Rulebook p.18: when the supply is short, remove same-type tokens from the board first.");
            Assert.That(reclamation.NodeId, Is.EqualTo(new EffectNodeId("component-reclaim-token")));
            Assert.That(reclamation.Candidates, Has.Count.EqualTo(placed),
                "Every same-type token on the board is a legal thing to take back.");

            var reclaimedSpace = reclamation.Candidates[0].Value.Coordinate;
            var reclaimedToken = RestoreBoard(session).PlacedTokens
                .Single(token => token.Coordinate.Equals(reclaimedSpace)).Id;
            Accept(session, DirectorCommand.AnswerEffectChoice(reclamation.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var placement = session.Projection.PendingEffectChoice;
            Assert.That(placement, Is.Not.Null,
                "Reclaiming is its own node; placement is the next one, so each carries one decision.");
            Assert.That(placement.NodeId, Is.EqualTo(new EffectNodeId("component-place-token")));

            Accept(session, DirectorCommand.AnswerEffectChoice(placement.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var board = RestoreBoard(session);
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(placed),
                "One token came back to the supply and one left it; the total on the board is unchanged.");
            Assert.That(
                board.PlacedTokens.Any(token => token.Id.Equals(reclaimedToken)),
                Is.False,
                "The reclaimed token instance is gone. Identity is what matters here rather than the " +
                "space: placing the new token back onto the freed space is a legal outcome, and only " +
                "the instance tells the two apart.");
        }

        [Test]
        public void StaleComponentChoiceInput_IsRejectedAtomically()
        {
            var session = StartComponentSkill(out _);
            var before = session.Projection;

            var rejected = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                new EffectChoiceId("stale-component-choice"),
                EffectChoiceCommandKind.Confirm)));

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable));
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
        }

        [Test]
        public void APausedComponentSkill_SurvivesSnapshotRestoreAndContinuesIdentically()
        {
            var session = StartComponentSkill(out _);
            ChooseOption(session, PlaceBranch);
            var live = session.Projection.PendingEffectChoice;
            var payload = session.CaptureSnapshot();

            var restored = CreateComponentSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            var revived = restored.Projection.PendingEffectChoice;
            Assert.That(revived, Is.Not.Null);
            Assert.That(revived.NodeId, Is.EqualTo(live.NodeId));
            Assert.That(
                revived.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(live.Candidates.Select(candidate => candidate.ToString())),
                "Candidate spaces are re-derived from the board and the trusted definition, never read " +
                "from the payload.");

            Accept(restored, DirectorCommand.AnswerEffectChoice(revived.SelectInvocations[0]));
            Accept(restored, DirectorCommand.AnswerEffectChoice(
                restored.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(RestoreBoard(restored).PlacedTokens, Has.Count.EqualTo(1));
        }

        [Test]
        public void PlacedTokens_ReplayAndUndoDeterministically()
        {
            var session = StartComponentSkill(out _);
            ChooseOption(session, PlaceBranch);
            var beforePlacement = session.Projection.DiagnosticSnapshot;

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterPlacement = session.Projection.DiagnosticSnapshot;

            Assert.That(afterPlacement, Is.Not.EqualTo(beforePlacement));
            Assert.That(RestoreBoard(session).PlacedTokens, Has.Count.EqualTo(1));

            Assert.That(session.Undo().Accepted, Is.True);

            Assert.That(RestoreBoard(session).PlacedTokens, Is.Empty,
                "Retained-prefix Undo removes the token, because the placement is authoritative state " +
                "and not a side effect held somewhere the replay cannot reach.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterPlacement),
                "Replaying the same answers reproduces the same token identity and ordered facts.");
        }

        [Test]
        public void ClearCommand_RemovesAnAdjacentTokenAndReplaysDeterministicallyAcrossUndo()
        {
            var session = StartComponentSkill(out var actorId);
            ChooseOption(session, PlaceBranch);
            var actorCoordinate = RestoreBoard(session).Occupancies
                .Single(hero => hero.HeroId.Equals(actorId)).Coordinate;
            var adjacentPlacement = session.Projection.PendingEffectChoice.SelectInvocations
                .First(invocation => actorCoordinate.GetNeighbors()
                    .Contains(invocation.Candidate.Value.Coordinate));
            Accept(session, DirectorCommand.AnswerEffectChoice(adjacentPlacement));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var token = RestoreBoard(session).PlacedTokens.Single();

            // Milestone 6.12: the Clear now happens in a later turn of the same round rather than in the
            // next one. p.8's end-of-round cleanup removes every token, so a fixture that crossed the
            // round boundary would be Clearing a token the round had already taken back.
            AdvanceToNextTurnInSameRound(session);
            CommitAttackTurnForActor(session, actorId);
            var attackOffer = session.Projection.AvailableTurnActions.Single(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Attack);
            var command = DirectorCommand.PerformClear(
                attackOffer.ActionDefinitionId,
                new[] { token.Id });

            var first = session.Execute(command);

            Assert.That(first.Accepted, Is.True, first.Detail);
            Assert.That(first.Events.Any(ruleEvent =>
                ruleEvent.TurnEvent.Kind == TurnResolutionEventKind.ClearActionProgressed), Is.True);
            var clearedLog = first.Projection.DiagnosticEvents.Single(entry =>
                entry.EventType == "Action.TokenCleared");
            Assert.That(clearedLog.StableReferences, Does.Contain($"token={token.Id.Value}"));
            Assert.That(clearedLog.Detail, Does.Contain("tokenDefinition="));
            Assert.That(clearedLog.Detail, Does.Contain($"space={token.Coordinate}"));
            Assert.That(RestoreBoard(session).PlacedTokens, Is.Empty);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformClear));
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(
                attackOffer.SourceCardInstanceId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "Clear replaces the entire Attack and resolves the source card as the turn action.");
            var expectedSnapshot = first.Projection.AuthoritativeSnapshot;
            var expectedDiagnostic = first.Projection.DiagnosticSnapshot;

            Assert.That(session.Undo().Accepted, Is.True);

            Assert.That(RestoreBoard(session).PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actorId));

            var replayed = session.Execute(command);
            Assert.That(replayed.Accepted, Is.True, replayed.Detail);
            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(expectedSnapshot));
            Assert.That(replayed.Projection.DiagnosticSnapshot, Is.EqualTo(expectedDiagnostic));
        }

        /// <summary>
        /// Plays the component Skill until the one-token supply is empty, then leaves the session with
        /// the Skill available again so the caller can drive the short-supply path.
        ///
        /// It stays inside one round, and must. This helper used to play the Skill once per round and
        /// said so - "the supply has to survive round boundaries to run out at all" - which milestone
        /// 6.12 made false: p.8's cleanup returns every placed token at the end of every round, so a
        /// token can no longer be accumulated across one. The supply is therefore exhausted by the two
        /// heroes' Skills inside a single turn, which is why the Hazard supply is one rather than two.
        /// </summary>
        private static MatchSession PlaceTokensUntilSupplyIsExhausted(out int placed)
        {
            var session = CreateComponentSession();
            var supply = SyntheticComponentDefinitions.HazardDefinition.SupplyCount;
            placed = 0;
            CommitComponentCards(session);
            while (placed < supply)
            {
                StartComponentSkillOnCurrentTurn(session);
                ChooseOption(session, PlaceBranch);

                var choice = session.Projection.PendingEffectChoice;
                Assert.That(choice.NodeId, Is.EqualTo(new EffectNodeId("component-place-token")),
                    "While the supply still has a copy, placement asks directly and no reclamation runs.");
                Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0]));
                Accept(session, DirectorCommand.AnswerEffectChoice(
                    session.Projection.PendingEffectChoice.ConfirmInvocation));
                placed++;
            }

            return session;
        }

        /// <summary>
        /// Plays out the rest of the round with Holds and advances every end-of-round stage, so the
        /// resolved Skill card comes back to hand at Retrieve Cards. A round is four turns and a card
        /// stays in the turn area for all of them, which is exactly why a supply can be spent across
        /// rounds rather than within one.
        /// </summary>
        /// <summary>
        /// Finishes the current turn and stops at the next turn's commitments, asserting that the round
        /// did not turn over. Milestone 6.12 made that distinction load-bearing for any fixture holding a
        /// token: within a round a placed token persists, and across one p.8's cleanup takes it.
        /// </summary>
        private static void AdvanceToNextTurnInSameRound(MatchSession session)
        {
            var round = session.Projection.RoundIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, so this loop presses the release exactly as that player does.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(round),
                "The token must not cross a round boundary, or p.8's cleanup rather than Clear removes it.");
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
        }

        private static void AdvanceToNextRound(MatchSession session)
        {
            var round = session.Projection.RoundIndex;
            while (session.Projection.RoundIndex == round)
            {
                Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                    "The fixture must reach the next round rather than ending the match.");
                switch (session.Projection.Phase)
                {
                    case RoundResolutionPhase.AwaitingCommitments:
                        CommitAnyEligibleCards(session);
                        break;
                    case RoundResolutionPhase.ResolvingTurn:
                        // Milestone 8.35g. A finished resolution waits for its own player to hand the
                        // turn on, so this loop presses the release exactly as that player does.
                        if (session.Projection.PendingTurnRelease != null)
                        {
                            Accept(session, DirectorCommand.ReleaseTurn());
                            continue;
                        }

                        Accept(session, DirectorCommand.PerformHold(
                            session.Projection.SelectedActor.CardInstanceId));
                        break;
                    default:
                        Accept(session, DirectorCommand.AdvanceEndOfRound());
                        break;
                }
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
        }

        /// <summary>Commits whatever each side still holds, to carry the round forward.</summary>
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

                var hero = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
                Accept(session, DirectorCommand.Pass(hero));
            }

            ResolveInitiativeTie(session);
        }

        private static void CommitAttackTurnForActor(MatchSession session, HeroId actorId)
        {
            Assert.That(session.Projection.Phase,
                Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            var actorSide = session.Projection.Participants.GetTeamForHero(actorId).Side;
            var opposingSide = actorSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
            Func<CardInstance, CardDefinition> definitionFor = card =>
                session.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference));
            var actorCard = HandCards(session, actorSide)
                .Where(card => definitionFor(card).ActionDefinitions.Any(action => action.Kind == ActionKind.Attack))
                .OrderByDescending(card => definitionFor(card).Initiative.Value)
                .First();
            var opposingCard = HandCards(session, opposingSide)
                .Where(card => definitionFor(card).ActionDefinitions.Any(action => action.Kind == ActionKind.Attack))
                .OrderBy(card => definitionFor(card).Initiative.Value)
                .First();

            Accept(session, DirectorCommand.CommitCard(actorCard.OwnerId, actorCard.Id));
            Accept(session, DirectorCommand.CommitCard(opposingCard.OwnerId, opposingCard.Id));
            ResolveInitiativeTie(session);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actorId));
        }

        private static void StartComponentSkillOnCurrentTurn(MatchSession session)
        {
            // Milestone 8.35g. A placement finishes the acting hero's resolution, and the turn then
            // waits for that hero's own player before the next actor - who holds the other component
            // card - is selected. The release goes above the offer read below, which is the read that
            // would otherwise see an actor that has not been chosen yet.
            if (session.Projection.PendingTurnRelease != null)
            {
                Accept(session, DirectorCommand.ReleaseTurn());
            }

            var offer = FindComponentSkillOffer(session);
            Assert.That(offer, Is.Not.Null, "The component Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.Value.ActionDefinitionId));
        }

        private static MatchSession StartComponentSkill(out HeroId actorId)
        {
            var session = CreateComponentSession();
            CommitComponentCards(session);
            actorId = session.Projection.SelectedActor.ActorId;
            StartComponentSkillOnCurrentTurn(session);
            return session;
        }

        /// <summary>
        /// Selects one option of the root <c>Choose</c> and confirms it, which is the two-command shape
        /// every selection choice in this engine already has.
        /// </summary>
        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static ActionOffer? FindComponentSkillOffer(MatchSession session)
        {
            var matches = session.Projection.AvailableTurnActions
                .Where(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-component-skill")))
                .ToArray();
            return matches.Length == 1 ? matches[0] : (ActionOffer?)null;
        }

        /// <summary>
        /// Commits each side's component Skill card. Red's carries the highest Initiative in the fixture
        /// and Blue's the lowest, so the Red hero reliably acts first and the fixture never has to steer
        /// Initiative to reach the Skill.
        /// </summary>
        private static void CommitComponentCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase,
                Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side)
                    .Single(candidate => candidate.Id.Value.Contains("cache"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            ResolveInitiativeTie(session);
        }

        private static CardInstance[] HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static void ResolveInitiativeTie(MatchSession session)
        {
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority RestoreBoard(
            MatchSession session) => Restore(session).Board;

        private static MarkerAttachmentRegister RestoreMarkers(MatchSession session) =>
            Restore(session).Round.Markers;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateComponentSession()
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
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
    }
}

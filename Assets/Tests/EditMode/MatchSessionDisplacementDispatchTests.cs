using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4 end to end through <see cref="IMatchSession"/>: direct Move and Object Push as
    /// authoritative commands, with restore, replay, and retained-prefix Undo.
    ///
    /// The fixture line is real official-map geography rather than a synthetic board, which is what
    /// makes the interesting case free. The Red hero stands on its own spawn point <c>q-2_r-5</c>; the
    /// Blue hero is placed adjacent to its East at <c>q-1_r-5</c>; <c>q0_r-5</c> is playable, and the
    /// next space along that file, <c>q1_r-5</c>, is a declared terrain cell with nothing but off-map
    /// beyond it. So a push of 2 directly away from the Red hero can only travel 1, which is p.18's
    /// "move it as far as possible" against milestone 6.8e's terrain classification, proved on the map
    /// the game is actually played on.
    ///
    /// Board state is read back by deserializing the authoritative snapshot, as milestone 6.2's
    /// component fixture does, because 6.4's non-goals bar a displacement-specific projection property.
    /// </summary>
    public sealed class MatchSessionDisplacementDispatchTests
    {
        private static readonly EffectNodeId MoveBranch = new EffectNodeId("displacement-move-branch");
        private static readonly EffectNodeId PushBranch = new EffectNodeId("displacement-object-push");

        private static readonly ActionDefinitionId DisplacementSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate BeyondBlue = new HexCoordinate(0, -5);
        private static readonly HexCoordinate TerrainBeyond = new HexCoordinate(1, -5);

        [Test]
        public void TheDisplacementSkill_OffersItsBranchesInDefinitionOrder()
        {
            var session = StartDisplacementSkill();

            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.ChooserId, Is.EqualTo(session.Projection.SelectedActor.ActorId),
                "The acting hero owns the decision; the resolver never picks for a player.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[]
                {
                    MoveBranch,
                    PushBranch,
                    new EffectNodeId("placement-place-branch"),
                    new EffectNodeId("placement-spawn"),
                    new EffectNodeId("exchange-swap-branch"),
                    new EffectNodeId("exchange-replace-branch"),
                    new EffectNodeId("exchange-remove-branch")
                }),
                "Milestones 6.5 and 6.6 appended their branches, so 6.4's two keep their positions.");
        }

        [Test]
        public void TheFixtureLine_IsTheOfficialGeographyTheseCasesAssume()
        {
            // The cases below are only meaningful if the map really is shaped this way, so the shape
            // is asserted rather than trusted.
            var board = RestoreBoard(StartDisplacementSkill());

            Assert.That(board.IsEmptyPlayableSpace(BeyondBlue), Is.True);
            Assert.That(board.IsEmptyPlayableSpace(TerrainBeyond), Is.False,
                "q1_r-5 is a declared terrain cell, so nothing may end up there.");
            Assert.That(
                board.MapDefinition.Spaces.Any(space =>
                    space.Coordinate.Equals(TerrainBeyond) && space.Kind == BoardMapSpaceKind.Terrain),
                Is.True);
            Assert.That(
                board.MapDefinition.Spaces.Any(space => space.Coordinate.Equals(new HexCoordinate(2, -5))),
                Is.False,
                "Nothing exists beyond the terrain on this file, so a permitted push has nowhere further.");
        }

        [Test]
        public void TheObjectPushBranch_MovesTheAdjacentHeroAsFarAsTerrainAllows()
        {
            var session = StartDisplacementSkill();
            var blueId = BlueHero(session);

            ChooseOption(session, PushBranch);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(BeyondBlue),
                "A push of 2 travels 1: the second space is terrain, and p.18 moves the target as far " +
                "as possible rather than failing.");
            Assert.That(board.IsEmptyPlayableSpace(BlueAt), Is.True,
                "The pushed hero's former space is free for every occupancy consumer.");
            Assert.That(Coordinate(board, session.Projection.Participants.Heroes
                .Single(hero => !hero.Id.Equals(blueId)).Id), Is.EqualTo(RedAt),
                "The pushing unit does not move.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void APushWithNothingAdjacent_IsRefusedRatherThanOfferedAndSpentSilently()
        {
            // p.18's zero-space push is a target that cannot travel, and that stays a legal push. No
            // target at all is a different thing: the mode can do nothing, so milestone 6.5ac refuses
            // it instead of letting the actor spend the card on a step that stops (p.10).
            var session = StartDisplacementSkill(blueAt: new HexCoordinate(-3, 10));

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Has.Member(PushBranch));
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Has.No.Member(PushBranch),
                "A refused option carries no way to select it.");
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Has.Member(PushBranch),
                "It is still projected: a player sees the mode exists and that it cannot be taken.");

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, BlueHero(session)), Is.EqualTo(new HexCoordinate(-3, 10)),
                "Nothing moved.");
        }

        [Test]
        public void TheDirectMoveBranch_RelocatesTheActorThroughItsOwnUpToChoice()
        {
            var session = StartDisplacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseOption(session, MoveBranch);

            // The `up to` Optional node asks whether to move at all before anything else runs.
            var confirm = session.Projection.PendingEffectChoice;
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Accept(session, DirectorCommand.AnswerEffectChoice(confirm.ConfirmInvocation));

            // 6.8k: the sequence's first step names the subject. The actor's own space is the original
            // self-move; the adjacent Blue hero is the widened breadth.
            var subject = session.Projection.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, "Which object moves is the selection's decision.");
            Assert.That(subject.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { RedAt, BlueAt }),
                "The selection offers the actor itself and every adjacent object, in axial order.");
            SelectValue(session, RedAt);

            var spaces = session.Projection.PendingEffectChoice;
            Assert.That(spaces, Is.Not.Null, "Which destination is the move's one decision.");
            Assert.That(spaces.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(
                spaces.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.Member(RedAt),
                "Milestone 6.5ad: `up to` is p.10's non-mandatory wording, so no distance at all is " +
                "one of the distances offered, and the mover's own space is what that looks like " +
                "among destinations. This assertion used to state the opposite, which was the defect.");

            // This scenario is about the relocation, so it takes one of the destinations that is one.
            var chosen = spaces.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .First(space => !space.Equals(RedAt));
            Accept(session, DirectorCommand.AnswerEffectChoice(spaces.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.Coordinate.Equals(chosen))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(chosen));
            Assert.That(board.IsEmptyPlayableSpace(RedAt), Is.True);
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void TheDirectMoveBranch_NamesTheAdjacentHeroAndMovesItInstead()
        {
            // 6.8k's observable outcome end to end: the definition names another board object as a
            // typed selection and the displacement capability accepts it as its subject.
            var session = StartDisplacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;
            var blueId = BlueHero(session);

            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            SelectValue(session, BlueAt);

            var spaces = session.Projection.PendingEffectChoice;
            Assert.That(spaces.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(
                spaces.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.Member(BeyondBlue),
                "The destinations are the named subject's reachability, not the actor's.");
            SelectValue(session, BeyondBlue);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(BeyondBlue),
                "The named subject moved.");
            Assert.That(Coordinate(board, actorId), Is.EqualTo(RedAt),
                "The actor stays where it stands: it chose, it did not move.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void AnIllegalSubjectSelection_IsRejectedAtomicallyAndThePendingChoiceSurvives()
        {
            var session = StartDisplacementSkill();
            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(TerrainBeyond)))));

            Assert.That(illegal.Accepted, Is.False,
                "A coordinate the selection never offered is refused outright.");
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal is atomic: the pending selection is exactly what it was.");

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                new EffectChoiceId("stale-subject-selection"),
                EffectChoiceCommandKind.Confirm)));
            Assert.That(stale.Accepted, Is.False,
                "An answer naming a choice that is not the live one is stale and refused.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
        }

        [Test]
        public void APausedSubjectSelection_SurvivesSnapshotRestoreWithRederivedCandidates()
        {
            var session = StartDisplacementSkill();
            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var paused = session.Projection.PendingEffectChoice;
            Assert.That(paused.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));

            var byteIdentical = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(byteIdentical.Round, byteIdentical.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A paused subject selection round-trips byte-identically.");

            var payload = session.CaptureSnapshot();
            var restored = StartDisplacementSkill();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            var revived = restored.Projection.PendingEffectChoice;
            Assert.That(
                revived.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer.");

            SelectValue(restored, BlueAt);
            SelectValue(restored, BeyondBlue);
            Assert.That(restored.Projection.PendingSkillResolution, Is.Null,
                "The restored selection continues to the same completion the live one would.");
        }

        [Test]
        public void ADirectMoveOfTheAdjacentHero_ReplaysAndUndoesDeterministically()
        {
            var session = StartDisplacementSkill();
            var blueId = BlueHero(session);

            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            SelectValue(session, BlueAt);
            var destination = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(BeyondBlue));
            Accept(session, DirectorCommand.AnswerEffectChoice(destination));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BeyondBlue));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm),
                "Retained-prefix Undo returns to the selected-but-unconfirmed destination.");
            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BlueAt),
                "The named subject is back where it stood, because the displacement is authoritative " +
                "board state the replay reaches.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void DecliningTheUpToMove_CompletesTheSkillWithoutMoving()
        {
            var session = StartDisplacementSkill();
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseOption(session, MoveBranch);
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.DeclineInvocation));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(RedAt),
                "`up to` includes not moving, which is a legal result rather than a failure.");
        }

        [Test]
        public void APush_ReplaysAndUndoesDeterministically()
        {
            var session = StartDisplacementSkill();
            var blueId = BlueHero(session);
            var selectInvocation = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.OptionNodeId.Equals(PushBranch));
            var beforePush = session.Projection.DiagnosticSnapshot;

            Accept(session, DirectorCommand.AnswerEffectChoice(selectInvocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterPush = session.Projection.DiagnosticSnapshot;

            Assert.That(afterPush, Is.Not.EqualTo(beforePush));
            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BeyondBlue));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BlueAt),
                "Retained-prefix Undo puts the pushed hero back, because the displacement is " +
                "authoritative board state and not a side effect the replay cannot reach.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterPush),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void APush_SurvivesSnapshotRestoreByteIdentically()
        {
            var session = StartDisplacementSkill();
            ChooseOption(session, PushBranch);

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var restored = Restore(session);

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(snapshot),
                "A committed displacement round-trips with no field the writer and reader disagree on.");
        }

        [Test]
        public void ADisplacement_NeverRecordsALanePushRequest()
        {
            // p.18: "Displacing a minion miniature (by moving, pushing, or swapping it) has no impact
            // on the Minion Battle." The request family exists and is recorded elsewhere, so this is a
            // real absence rather than an untested one.
            var session = StartDisplacementSkill();
            ChooseOption(session, PushBranch);

            Assert.That(Restore(session).Round.PendingLanePushRequest, Is.Null);
        }

        [Test]
        public void TheOnlyOrdinaryCardExposingDisplacement_IsTheOneTheShowcaseConverted()
        {
            // Milestone 6.4's non-goals barred configured live exposure until the after-action
            // correction existed. It does, and milestones 6.5a, 6.5b and 6.7a are the conversions
            // that spend that permission - each on exactly one card. The adjacency-derived batch
            // stays behind the Skill fixture, because no showcase card declares "every adjacent
            // object".
            var ordinary = PlaceholderOneVsOneTestConfiguration.Create();

            var byCapability = ordinary.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes)
                    .Select(node => new { definition.Id, node.Capability }))
                .ToArray();

            Assert.That(
                byCapability.Where(entry => entry.Capability.Equals(
                    GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.ObjectPushV2)),
                Is.Empty,
                "The adjacency-derived batch reaches a player through no configured card.");
            Assert.That(
                byCapability
                    .Where(entry => entry.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.DirectMoveV2))
                    .Select(entry => entry.Id.Value),
                Is.EqualTo(new[] { "placeholder-red-bombard" }),
                "Direct Move is exposed by exactly the card milestone 6.5b converted.");
            Assert.That(
                byCapability
                    .Where(entry => entry.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.PushTargetV2))
                    .Select(entry => entry.Id.Value),
                Is.EqualTo(new[] { "placeholder-red-needle", "placeholder-blue-longbow" }),
                "Card-text Push is exposed by 6.5a's conversion at distance 3 and 6.7a's at 1.");
            Assert.That(
                byCapability
                    .Where(entry => entry.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities.ChooseTargetV4))
                    .Select(entry => entry.Id.Value),
                Is.EqualTo(new[] { "placeholder-red-needle", "placeholder-blue-longbow" }),
                "Both are card-text Push, so both name the activity their consumer asks immunity for.");

            // The two selection revisions are exposed by different cards, which is the visible half
            // of the reason each branch picked the one it did: a Push names its own activity, and a
            // card-text direct Move has none to name. Blue Sentinel joined the second group at
            // milestone 6.6a with three selections of its own, and p.19 gives a Swap, a Replace and a
            // Remove no activity either.
            Assert.That(
                byCapability
                    .Where(entry => entry.Capability.Equals(
                        GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities.ChooseTargetV3))
                    .Select(entry => entry.Id.Value)
                    .Distinct(),
                Is.EqualTo(new[] { "placeholder-red-bombard", "placeholder-blue-sentinel" }));
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static HeroId BlueHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static MatchSession StartDisplacementSkill(HexCoordinate? blueAt = null)
        {
            var session = CreateDisplacementSession(blueAt ?? BlueAt);
            CommitDisplacementCards(session);
            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(DisplacementSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(DisplacementSkill),
                "The displacement Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>Selects one coordinate-valued candidate of the live choice and confirms it.</summary>
        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.Value.Coordinate.Equals(coordinate));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        /// <summary>
        /// Commits each side's displacement Skill card. Red's carries the highest Initiative in the
        /// fixture hand and Blue's the lowest, so the Red hero reliably acts first.
        /// </summary>
        private static void CommitDisplacementCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("shove") &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateDisplacementSession(HexCoordinate blueAt)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, RedAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}

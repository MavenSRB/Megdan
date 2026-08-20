using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.9. Rulebook p.8: "A Lane can be Pushed both at the end of round, and in the middle
    /// of a turn. If you defeat, or remove, the last remaining enemy minion in the Battle Zone, the Lane
    /// is immediately Pushed in the same way as it would during a Minion Battle. Note: A Lane Push can
    /// never be triggered by moving, pushing, or swapping a minion. It must be either defeated, or
    /// removed. If the Lane is pushed in the middle of a turn, the turn continues in the Initiative
    /// order as normal."
    ///
    /// What this milestone owns is the request and the checkpoint it reaches, not the transition. The
    /// Wave counter, the Battle-Zone advance, the old-zone removal and the respawn belong to milestones
    /// 6.10 and 6.11, so a turn holding a request stops rather than pushing part of the way.
    ///
    /// Everything about the Battle Zone runs on a map that flags one, because a Battle Zone is map
    /// content: the Defeat cases use the same small flagged map <c>MinionAttackResolutionTests</c> uses,
    /// and the Remove cases play a real turn on the official map.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class ImmediateLanePushTests
    {
        private static readonly HeroId RedHero =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId BlueHero =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        private static readonly CardInstanceId RemoveCardId = new CardInstanceId("lane-push-remove-card");

        private static readonly ActionDefinitionId RemoveSkill =
            new ActionDefinitionId("lane-push-remove-skill");

        // -------------------------------------------------------------------------------------------
        // Acceptance check 1: a Defeat of the last relevant enemy Battle-Zone minion emits the request
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DefeatOfTheLastEnemyBattleZoneMinion_EmitsTheRequestNamingBothTeamsAndTheZone()
        {
            var fixture = CreateAttackFixture(
                Minion("blue-last", TeamSide.Blue, new HexCoordinate(1, 0)),
                Minion("red-holder", TeamSide.Red, new HexCoordinate(-1, 0)));

            var defeat = AttackMinion(fixture, "blue-last");

            Assert.That(defeat.MinionDefeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.Defeat));
            var request = fixture.Authority.PendingImmediateLanePush;
            Assert.That(request, Is.Not.Null,
                "p.8 pushes the Lane immediately when a Defeat takes the last enemy minion out of the " +
                "Battle Zone, so the completed Attack owes a request.");
            Assert.That(request.Cause, Is.EqualTo(LanePushCauseKind.Defeat),
                "p.8 names two causes separately - 'It must be either defeated, or removed' - so the " +
                "request says which sentence produced it rather than that something left the board.");
            Assert.That(request.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(request.WinningSide, Is.EqualTo(TeamSide.Red),
                "p.8: 'The team with one or more minions still remaining in the Battle Zone wins the push.'");
            Assert.That(
                request.BattleZoneId,
                Is.EqualTo(BattleZoneQuery.GetCurrentBattleZone(fixture.Board).Id));
            Assert.That(request.LosingSide, Is.EqualTo(defeat.MinionDefeat.LanePush.LosingSide),
                "The side read from the board at the push and the side the operation recorded are the " +
                "same fact; if they could disagree, one of the two readings would be wrong.");
        }

        [Test]
        public void DefeatThatLeavesTheSideAMinionInTheZone_EmitsNothing()
        {
            var fixture = CreateAttackFixture(
                Minion("blue-target", TeamSide.Blue, new HexCoordinate(1, 0)),
                Minion("blue-other", TeamSide.Blue, new HexCoordinate(2, 0)),
                Minion("red-holder", TeamSide.Red, new HexCoordinate(-1, 0)));

            var defeat = AttackMinion(fixture, "blue-target");

            Assert.That(defeat.MinionDefeat.RemovalResult.Kind, Is.EqualTo(DefeatRemovalResultKind.Removed));
            Assert.That(fixture.Authority.PendingImmediateLanePush, Is.Null,
                "p.8 asks for the last remaining enemy minion, and one still stands in the zone.");
        }

        [Test]
        public void DefeatThatEmptiesBothSides_EmitsNothingBecauseNoTeamWinsThePush()
        {
            var fixture = CreateAttackFixture(
                Minion("blue-last", TeamSide.Blue, new HexCoordinate(1, 0)));

            var defeat = AttackMinion(fixture, "blue-last");

            Assert.That(defeat.MinionDefeat.LanePush.Qualifies, Is.True,
                "The removal still emptied a side, which is the operation's own record.");
            Assert.That(fixture.Authority.PendingImmediateLanePush, Is.Null,
                "p.8's push has a winner - 'the team with one or more minions still remaining in the " +
                "Battle Zone' - and with neither side holding one there is no team to win it. Reporting " +
                "nothing is honest; awarding the push to nobody would not be.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 7.9c, findings F-BOARD-01 and F-BOARD-02: p.8 counts what is effectively a minion
        //
        // A hero is not a minion, so its Defeat qualifies nothing on its own - unless p.17's replacement
        // has made it count as one, which is the single case where the hero path and the minion path
        // must agree. The two cases below are the same Attack on the same board, differing only in
        // whether that declaration is live, so together they say "exactly then, and never otherwise".
        //
        // Before this milestone each half was wrong in an opposite direction, and the two cancelled: the
        // Attack's hero arm recorded a qualification nothing consumed, and the qualification itself never
        // asked whether the removed piece counted as a minion. Routing the record without the guard
        // would have turned a skipped consequence into a fabricated one.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void HeroDefeatOfTheLastCountsAsMinion_EmitsTheMidTurnRequest()
        {
            var fixture = CreateHeroDefeatFixture(
                Minion("red-holder", TeamSide.Red, new HexCoordinate(-1, 0)));
            var countsAsMinion = CountsAsMinion(BlueHero);

            Assert.That(
                BattleZoneQuery.FindEffectiveMinionsInside(
                        fixture.Board, fixture.Configuration.Participants, countsAsMinion)
                    .Where(minion => minion.TeamSide == TeamSide.Blue)
                    .Select(minion => minion.Unit),
                Is.EqualTo(new[] { RuleObject.ForHero(BlueHero) }),
                "The declaration is what puts the hero in the set p.8 counts, so the case is only the " +
                "case while it holds: Blue's last effective Battle-Zone minion is the hero itself.");

            var defeat = DefeatTheBlueHero(fixture, countsAsMinion);

            Assert.That(defeat.HeroDefeat, Is.Not.Null);
            Assert.That(defeat.HeroDefeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.Defeat),
                "The shared removal stage answers for the hero path exactly as it does for the minion " +
                "path - this half was already true before milestone 7.9c, and nothing read it.");
            var request = fixture.Authority.PendingImmediateLanePush;
            Assert.That(request, Is.Not.Null,
                "p.8: 'If you defeat, or remove, the last remaining enemy minion in the Battle Zone, " +
                "the Lane is immediately Pushed.' A hero that counts as a minion is that minion, so the " +
                "completed Attack owes the request rather than dropping the cause it just recorded.");
            Assert.That(request.Cause, Is.EqualTo(LanePushCauseKind.Defeat));
            Assert.That(request.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(request.WinningSide, Is.EqualTo(TeamSide.Red),
                "p.8: 'The team with one or more minions still remaining in the Battle Zone wins the " +
                "push' - here the Red holder, which is why this board has one.");
            Assert.That(
                request.BattleZoneId,
                Is.EqualTo(BattleZoneQuery.GetCurrentBattleZone(fixture.Board).Id));
        }

        [Test]
        public void HeroDefeatOfAnOrdinaryHero_RecordsNoCause_EvenInAnEmptiedBattleZone()
        {
            var fixture = CreateHeroDefeatFixture(
                Minion("red-holder", TeamSide.Red, new HexCoordinate(-1, 0)));

            Assert.That(
                BattleZoneQuery.FindEffectiveMinionsInside(
                        fixture.Board, fixture.Configuration.Participants, RuleModificationSet.Empty)
                    .Any(minion => minion.TeamSide == TeamSide.Blue),
                Is.False,
                "Blue holds no effective minion in the zone, so this is the board on which an emptied " +
                "side is most tempting to read as a qualifying one.");

            var defeat = DefeatTheBlueHero(fixture, RuleModificationSet.Empty);

            Assert.That(defeat.HeroDefeat, Is.Not.Null);
            Assert.That(defeat.HeroDefeat.LanePush.Qualifies, Is.False,
                "p.8 asks about the last remaining enemy *minion*. An ordinary hero is not one, however " +
                "empty of minions its side's Battle Zone already was, so the removal of the hero " +
                "records no cause at all - the property's own contract, which the query did not keep.");
            Assert.That(fixture.Authority.PendingImmediateLanePush, Is.Null,
                "And with no cause there is no request: no Wave is spent and the Battle Zone does not " +
                "advance for a Defeat p.8 never named.");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 3: a Defeat whose later removal is prevented still emits
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DefeatWhoseRemovalIsPrevented_StillRecordsTheCause()
        {
            var fixture = CreateAttackFixture(
                Minion("blue-last", TeamSide.Blue, new HexCoordinate(1, 0)),
                Minion("red-holder", TeamSide.Red, new HexCoordinate(-1, 0)));
            var subject = BoardObjectReference.ForMinion(new MinionId("blue-last"));
            var stage = new RemovalStageDecision(
                RuleActivity.Attack,
                RuleObject.ForHero(RedHero),
                fixture.Board,
                fixture.Board,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty,
                LanePushCauseKind.Defeat);

            // The miniature is still standing, exactly as a prevented removal leaves it.
            var qualification = stage.QualifyLanePush(subject, new HexCoordinate(1, 0));

            Assert.That(qualification.Qualifies, Is.True,
                "p.8 names two independent triggers, and p.13's Bolster example makes a Defeat whose " +
                "removal is prevented a complete Defeat that still collects its coins. So the first " +
                "sentence - 'If you defeat ... the last remaining enemy minion' - is answered by the " +
                "Defeat rather than by the piece leaving the board.");
            Assert.That(qualification.Cause, Is.EqualTo(LanePushCauseKind.Defeat));
            Assert.That(qualification.LosingSide, Is.EqualTo(TeamSide.Blue),
                "The side is read from the board while the miniature is still on it, rather than from " +
                "the reserve it never joined.");
            Assert.That(
                fixture.Board.Minions.Any(minion => minion.MinionId.Value == "blue-last"), Is.True,
                "Asking the question moves nothing; the prevented piece is still where it stood.");
        }

        [Test]
        public void PreventedDefeatResolution_CarriesTheCauseItWasGiven()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId("prevented:minion-defeat"),
                new DefeatTargetId("blue-last"),
                new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                RedHero,
                2,
                0);
            var accounting = new HeroCoinLedger(configuration.Participants);
            var qualification = LanePushQualification.For(
                LanePushCauseKind.Defeat, TeamSide.Blue, new ZoneId("battle-zone"));

            var prevented = MinionDefeatResolution.Prevented(
                outcome,
                accounting.Award(outcome),
                new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                new DefeatRemovalResult(
                    outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                qualification);

            Assert.That(prevented.LanePush, Is.EqualTo(qualification));
            Assert.That(prevented.RemovedMinion, Is.Null,
                "Nothing left the board, so no removed occupancy is named. The Defeat is what p.8 reads.");
            Assert.Throws<ArgumentException>(
                () => MinionDefeatResolution.Prevented(
                    outcome,
                    accounting.Award(outcome),
                    new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                    new DefeatRemovalResult(
                        outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                    LanePushQualification.For(
                        LanePushCauseKind.Removal, TeamSide.Blue, new ZoneId("battle-zone"))),
                "A Defeat records p.8's Defeat sentence; carrying the removal's cause would attribute " +
                "it to the wrong one of the two.");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 1: a successful standalone Remove emits the request
        // Acceptance checks 4 and 5: the checkpoint stops the turn, and nothing else moves
        // -------------------------------------------------------------------------------------------

        [Test]
        public void RemoveOfTheLastEnemyBattleZoneMinion_PushesTheLaneBeforeInitiativeContinues()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(out var target, out _);

            Assert.That(
                session.Projection.RoundEvents.Any(value =>
                    value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.ImmediateLanePushRequested),
                Is.True,
                "p.19's standalone Remove is p.8's second sentence, so taking the last enemy minion out " +
                "of the Battle Zone owes the same immediate push a Defeat does, and the request reaches " +
                "the checkpoint as a recorded fact.");
            Assert.That(
                session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)), Is.True,
                "The removal itself happened, which is what caused the push - and p.8's third step then " +
                "respawned that same miniature out of the supply into the new Battle Zone.");

            var requested = FirstIndexOf(session, TurnResolutionEventKind.ImmediateLanePushRequested);
            var resolved = FirstIndexOf(session, TurnResolutionEventKind.ImmediateLanePushResolved);
            Assert.That(resolved, Is.GreaterThan(requested),
                "Milestone 6.11 consumes what milestone 6.9 recorded, so the push finishes after the " +
                "request rather than instead of it.");
            Assert.That(
                session.Projection.RoundEvents
                    .Select((value, index) => new { value, index })
                    .Where(entry => entry.value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                        entry.value.TurnEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
                    .All(entry => entry.index < requested || entry.index > resolved),
                Is.True,
                "p.8 pushes the Lane first and only then does 'the turn continue in the Initiative " +
                "order as normal', so no Initiative event falls between the two.");
        }

        [Test]
        public void AResolvedRequest_LetsEveryLaterTurnCommandThrough()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(out _, out _);
            var blueCard = session.Projection.Cards.First(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.TurnArea);

            var held = session.Execute(DirectorCommand.PerformHold(blueCard.Id));

            Assert.That(held.Accepted, Is.True, held.Detail);
            Assert.That(session.Projection.PendingImmediateLanePush, Is.Null,
                "Nothing is outstanding at the checkpoint, which is what lets the turn go on. Until " +
                "milestone 6.11 this same command refused with the checkpoint's own reason.");
        }

        [Test]
        public void AResolvedRequest_ExposesTheWholeTransitionRatherThanPartOfIt()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(out var target, out _);

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn),
                "The round has not advanced; p.8's mid-turn push happens inside the turn.");
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null,
                "The end-of-round request is a different request at a different checkpoint, and this " +
                "push produced no Minion Battle.");
            Assert.That(
                session.Projection.Minions.Select(minion => minion.MinionId.Value),
                Has.Member(target.Value),
                "p.8 removes every minion of the previous Battle Zone into the supply and then respawns " +
                "from that supply, so the miniature the Remove took is standing again - in the new " +
                "Battle Zone rather than where it fell.");
            Assert.That(session.Projection.Minions, Is.Not.Empty,
                "And the new Battle Zone was filled, so the transition ran to its end rather than " +
                "stopping two thirds of the way through it.");
        }

        [Test]
        public void AResolvedPush_SurvivesASnapshotRestoreByteForByte()
        {
            var session = StartRemoveSkillAgainstTheLastMinion(out _, out _);
            var payload = session.Projection.AuthoritativeSnapshot;

            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

            Assert.That(restored.Round.CurrentTurnAuthority.PendingImmediateLanePush, Is.Null,
                "A snapshot taken after the push restores a turn with nothing outstanding, or the " +
                "consumption would be a property of the running process rather than of the match.");
            Assert.That(restored.Round.LanePush.LanePush.RemainingWaveCounters, Is.EqualTo(2),
                "p.8 spent one of the three Wave counters, and the restore says so rather than handing " +
                "the match back a full pool.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "The restored match writes the same bytes, so the push round-trips rather than being " +
                "approximated.");
        }

        // -------------------------------------------------------------------------------------------
        // Acceptance check 2: the excluded operations emit nothing
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.8: "A Lane Push can never be triggered by moving, pushing, or swapping a minion. It must be
        /// either defeated, or removed."
        ///
        /// Held over the implemented capabilities themselves rather than over a list of them. Every
        /// capability this build registers is asked what it declares it can publish, and exactly one -
        /// p.19's standalone Remove - declares p.8's answer. A direct Move, an Object Push, a Place, a
        /// Spawn, a Swap and a Replace are all in that set and none of them can carry the fact, so an
        /// operation added after this milestone is audited by existing rather than by being remembered.
        /// </summary>
        [Test]
        public void OnlyTheRemove_DeclaresTheOutputTheRequestIsBuiltFrom()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();

            var declaring = catalog.Registered
                .Where(reference => catalog.Load(reference).Schema.Outputs
                    .Any(output => output.OutputName == RecordedLanePushQuery.OutcomeFactName))
                .ToArray();

            Assert.That(declaring, Is.EqualTo(new[] { SyntheticExchangeCapabilities.RemoveV2 }),
                "p.8's excluded operations stay excluded because they have nowhere to say it. If a " +
                "displacement ever declares this output, it has acquired the cause by being written " +
                "later, which is the defect this rule exists to catch.");
            Assert.That(
                catalog.Registered,
                Has.Member(SyntheticDisplacementCapabilities.ObjectPushV2)
                    .And.Member(SyntheticDisplacementCapabilities.DirectMoveV2)
                    .And.Member(SyntheticPlacementCapabilities.PlaceV1)
                    .And.Member(SyntheticPlacementCapabilities.SpawnV1)
                    .And.Member(SyntheticExchangeCapabilities.SwapV2)
                    .And.Member(SyntheticExchangeCapabilities.ReplaceTokenV1),
                "The rule is only worth as much as the set it ran over, so the operations p.8 names are " +
                "asserted to be in it rather than assumed.");
        }

        [Test]
        public void ADirectDisplacementOfTheLastBattleZoneMinion_LeavesNothingToBuildARequestFrom()
        {
            var board = CreateOfficialBoard();
            var last = ClearBattleZoneExceptOne(board, TeamSide.Red);
            var away = board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate)
                .First(coordinate =>
                    !BattleZoneQuery.IsInCurrentBattleZone(board, coordinate) &&
                    board.IsEmptyPlayableSpace(coordinate));

            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForMinion(last), away).Accepted, Is.True);

            Assert.That(
                BattleZoneQuery.FindEffectiveMinionsInside(
                        board,
                        PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue)
                            .Participants,
                        RuleModificationSet.Empty)
                    .Any(minion => minion.TeamSide == TeamSide.Red),
                Is.False,
                "The Battle Zone is as empty of Red as a removal would have left it, which is what makes " +
                "this the case p.8 rules out rather than a case it never reaches.");
            Assert.That(board.Minions.Any(minion => minion.MinionId.Equals(last)), Is.True,
                "The minion moved rather than left.");
            Assert.That(
                ImmediateLanePushQuery.From(
                    LanePushCauseKind.None,
                    new ActionResolutionId("displacement"),
                    board,
                    PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue).Participants,
                    RuleModificationSet.Empty),
                Is.Null,
                "An emptied zone is not the trigger; a Defeat or a removal is.");
        }

        [Test]
        public void APreventedRemove_PublishesNoCause()
        {
            var board = CreateOfficialBoard();
            var heavy = BattleZoneQuery.FindMinionsInside(board)
                .First(minion => minion.MinionType == StandardMinionType.Heavy);
            var beside = BesideCoordinate(board, heavy.Coordinate);

            var applied = Handler(SyntheticExchangeCapabilities.RemoveV2).Execute(
                Context(
                    board,
                    beside,
                    new Dictionary<string, EffectValue>
                    {
                        ["subject"] = EffectValue.Space(heavy.Coordinate),
                        [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                            EffectValue.Integer((int)RemoveEligiblePieceKind.GamePiece)
                    }));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A prevented removal is an applied step whose rules answer was 'it stays'.");
            Assert.That(board.Minions.Any(minion => minion.MinionId.Equals(heavy.MinionId)), Is.True,
                "p.13 protects the Heavy while friendly non-Heavy minions stand in the zone.");
            Assert.That(Published(applied), Is.EqualTo(LanePushCauseKind.None),
                "p.8's second sentence is about a piece being removed, and nothing was. That is the " +
                "asymmetry against a prevented Defeat, whose own sentence is about the Defeat.");
        }

        // -------------------------------------------------------------------------------------------
        // Boards the rule says nothing about
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ABoardThatFlagsNoBattleZone_OwesNoRequest()
        {
            var board = CreateSyntheticLineBoard();

            Assert.That(
                ImmediateLanePushQuery.From(
                    LanePushCauseKind.Removal,
                    new ActionResolutionId("synthetic-cause"),
                    board,
                    PlaceholderOneVsOneParticipants.Create(),
                    RuleModificationSet.Empty),
                Is.Null,
                "A map that flags no single Battle Zone has no lane to push, which is the reading the " +
                "recorded cause and the displaced-minion correction already take.");
        }

        [Test]
        public void NoRecordedCause_OwesNoRequestHoweverEmptyTheZoneIs()
        {
            var board = CreateOfficialBoard();
            ClearBattleZone(board, TeamSide.Blue);

            Assert.That(
                ImmediateLanePushQuery.From(
                    LanePushCauseKind.None,
                    new ActionResolutionId("no-cause"),
                    board,
                    PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red).Participants,
                    RuleModificationSet.Empty),
                Is.Null,
                "An emptied Battle Zone is not p.8's trigger. Something has to have defeated or removed " +
                "the last minion, and nothing about the board says which sentence, or whether either.");
        }

        // -------------------------------------------------------------------------------------------
        // The Attack fixture: a small map that flags a Battle Zone
        // -------------------------------------------------------------------------------------------

        private sealed class AttackFixture
        {
            public AttackFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                AdjacentMoveAuthority board,
                CardActionResolutionAuthority authority,
                ActionOffer offer)
            {
                Configuration = configuration;
                Board = board;
                Authority = authority;
                Offer = offer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer Offer { get; }
        }

        private static AttackActionResolutionResult AttackMinion(AttackFixture fixture, string minionId)
        {
            var opened = fixture.Authority.StartAttackResolution(
                new ActionResolutionId($"lane-push-attack-{minionId}"),
                fixture.Offer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            Assert.That(opened.Accepted, Is.True);
            var resolved = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId,
                    new MinionId(minionId)),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            Assert.That(resolved.Accepted, Is.True);
            Assert.That(resolved.MinionDefeat, Is.Not.Null);
            return resolved;
        }

        private static AttackFixture CreateAttackFixture(params QuickGameMinionSetupState[] minions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = CreateFlaggedBattleZoneMap();
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(-3, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    RedHero,
                    ActionOfferWindow.NormalPlay)
                .Single(value => value.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new AttackFixture(
                configuration, board, new CardActionResolutionAuthority(board), offer);
        }

        /// <summary>
        /// Milestone 7.9c. The same flagged-Battle-Zone board as the minion cases, with the two heroes
        /// adjacent so the configured non-Ranged Attack can reach, and composed through the hero-Defeat
        /// dispatcher because the Attack refuses to resolve a Defeat it cannot price.
        /// </summary>
        private static HeroDefeatFixture CreateHeroDefeatFixture(
            params QuickGameMinionSetupState[] minions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var board = new AdjacentMoveAuthority(
                CreateFlaggedBattleZoneMap(),
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    RedHero,
                    ActionOfferWindow.NormalPlay)
                .Single(value => value.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new HeroDefeatFixture(
                configuration,
                board,
                DefeatConsequenceComposition.Dispatcher(board, configuration.Participants),
                offer);
        }

        private sealed class HeroDefeatFixture
        {
            public HeroDefeatFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                AdjacentMoveAuthority board,
                CardActionResolutionAuthority authority,
                ActionOffer offer)
            {
                Configuration = configuration;
                Board = board;
                Authority = authority;
                Offer = offer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer Offer { get; }
        }

        /// <summary>
        /// Drives the Attack to a Defeat through the defender's weakest Defense, the route
        /// <c>DefeatMarkerReturnTests</c> uses. The modification set is carried through every step,
        /// because the declaration that makes the hero count as a minion has to be live at the removal
        /// as well as at the target choice.
        /// </summary>
        private static AttackActionResolutionResult DefeatTheBlueHero(
            HeroDefeatFixture fixture,
            RuleModificationSet modifications)
        {
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("lane-push-hero-defeat"),
                fixture.Offer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                modifications);
            Assert.That(start.Accepted, Is.True);
            var opened = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    BlueHero),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                modifications);
            Assert.That(opened.Accepted, Is.True);
            var weakest = opened.PendingDefenseChoice.LegalDefenseOffers
                .OrderBy(offer => PrintedValueQuery.CalculatePrintedValue(
                    offer, fixture.Configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                .First();
            fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    weakest.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                modifications);

            return fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                modifications);
        }

        /// <summary>p.17's replacement, declared on one hero by an Active effect, as a live set.</summary>
        private static RuleModificationSet CountsAsMinion(HeroId heroId) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForCountsAs(EffectiveUnitKind.Minion),
                        RuleObject.ForHero(heroId),
                        new ActiveEffectId("lane-push-counts-as-minion"),
                        heroId,
                        TeamSide.Blue,
                        0)
                },
                TieBreakerState.Unrevealed);

        private static QuickGameMinionSetupState Minion(string id, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(side == TeamSide.Red
                    ? PlaceholderOneVsOneParticipants.RedTeamIdValue
                    : PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                side,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(
                    new MapId("lane-push-map"), new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(
                    new MapId("lane-push-map"), new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static BoardMapDefinition CreateFlaggedBattleZoneMap()
        {
            var mapId = new MapId("lane-push-map");
            var zone = new BoardMapZone(
                new ZoneId("battle-zone"), "Battle Zone", "lane", null, false, true);
            var spaces = RadiusThree().Select(coordinate =>
                new BoardMapSpace(
                    new SpaceId($"q{coordinate.Q}_r{coordinate.R}"), coordinate,
                    BoardMapSpaceKind.Playable, zone.Id));
            return new BoardMapDefinition(
                mapId, 1, "Lane Push Map", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static IEnumerable<HexCoordinate> RadiusThree()
        {
            for (var q = -3; q <= 3; q++)
            {
                for (var r = Math.Max(-3, -q - 3); r <= Math.Min(3, -q + 3); r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Remove fixture: a real turn on the official map
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A played turn in which the Red hero's Skill removes the one Blue minion left in the Battle
        /// Zone.
        ///
        /// The card is declared by this fixture rather than added to the configured hand: no shipped
        /// card removes a minion - the showcase Sentinel's Remove takes a token, which empties nothing
        /// p.8 counts - and adding one would move every pinned digest for a rule that needs no content.
        /// The Skill itself is entirely ordinary: one mandatory p.19 Remove node naming a space.
        /// </summary>
        private static MatchSession StartRemoveSkillAgainstTheLastMinion(
            out MinionId target, out HeroId actor)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            // Blue starts the match with exactly one minion, so the Remove under test is the one p.8
            // names; Red keeps all of its own, which is what gives the push a winner. The others are
            // left out of the setup rather than stood outside the Battle Zone, because p.18's
            // after-action correction reads board position at the boundary and would pause the action
            // to bring every one of them back - a different rule than the one this fixture is about.
            var survivor = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue &&
                minion.MinionType != StandardMinionType.Heavy &&
                BattleZoneQuery.IsInStartingBattleZone(map, minion.Coordinate));
            target = survivor.MinionId;
            actor = RedHero;

            var minions = setup.Minions
                .Where(minion =>
                    minion.TeamSide != TeamSide.Blue || minion.MinionId.Equals(survivor.MinionId))
                .ToArray();
            var relocatedSetup = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, setup.Heroes,
                minions);

            var definition = CreateRemoveSkillDefinition(survivor.Coordinate);
            var instance = CardInstance.CreateInHand(
                RemoveCardId,
                definition.Reference,
                configuration.RedHand.Instances[0].OwnerId);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions.Concat(new[] { definition }),
                configuration.CardInstances.Concat(new[] { instance }),
                configuration.TieBreakerState,
                map.Reference,
                relocatedSetup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(
                new MatchSessionInitialState(scenario, relocatedSetup, new[] { map }));

            CommitBothSides(session);
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(RemoveSkill));
            var started = session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId));
            Assert.That(started.Accepted, Is.True, started.Detail);
            return session;
        }

        /// <summary>Where a turn checkpoint first appears in the round's ordered event stream.</summary>
        private static int FirstIndexOf(MatchSession session, TurnResolutionEventKind kind)
        {
            var events = session.Projection.RoundEvents;
            for (var index = 0; index < events.Count; index++)
            {
                if (events[index].Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    events[index].TurnEvent.Kind == kind)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void CommitBothSides(MatchSession session)
        {
            var owner = session.Projection.Cards.Single(card => card.Id.Equals(RemoveCardId)).OwnerId;
            var red = session.Execute(DirectorCommand.CommitCard(owner, RemoveCardId));
            Assert.That(red.Accepted, Is.True, red.Detail);

            var other = session.Projection.Cards.First(card =>
                !card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            var blue = session.Execute(DirectorCommand.CommitCard(other.OwnerId, other.Id));
            Assert.That(blue.Accepted, Is.True, blue.Detail);

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "Initiative 30 against a placeholder card cannot tie.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(RemoveCardId),
                "The Skill under test must be the action this turn starts with.");
        }

        /// <summary>
        /// One mandatory p.19 Remove naming a space. The subject is a literal rather than a chosen
        /// target because what is under test is p.8's consequence, not the choice that reaches it.
        /// </summary>
        private static CardDefinition CreateRemoveSkillDefinition(HexCoordinate subject)
        {
            var root = new EffectNodeId("lane-push-remove-root");
            var effect = new EffectDefinition(
                new EffectDefinitionId("lane-push-remove-effect"),
                "phase-6-9-test-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("subject", EffectValue.Space(subject)),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer((int)RemoveEligiblePieceKind.GamePiece))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                        })
                });

            return new CardDefinition(
                new CardDefinitionId("lane-push-remove-test"),
                "phase-6-9-test-rev-1",
                "TEST - Immediate Lane Push Remove",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        RemoveSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        // -------------------------------------------------------------------------------------------
        // Shared board helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// What a capability's published facts would tell the action boundary, asked through the same
        /// query the boundary asks. A capability that publishes no <c>lanePush</c> fact cannot reach the
        /// request however empty it leaves the zone.
        /// </summary>
        private static LanePushCauseKind Published(EffectExecutionResult result) =>
            RecordedLanePushQuery.Recorded(
                ProbeDefinition(),
                new[]
                {
                    new EffectNodeOutcome(
                        new EffectNodeId("lane-push-probe-node"), ProbeKind(result), result.Outcomes)
                });

        private static EffectExecutionResultKind ProbeKind(EffectExecutionResult result) =>
            result.Kind == EffectExecutionResultKind.None
                ? EffectExecutionResultKind.Applied
                : result.Kind;

        private static EffectDefinition ProbeDefinition()
        {
            var node = new EffectNodeId("lane-push-probe-node");
            return new EffectDefinition(
                new EffectDefinitionId("lane-push-probe"),
                "phase-6-9-test-rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.MarkActorV1)
                });
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticEffectCapabilities.CreateCatalog().Load(reference);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HexCoordinate actorAt,
            IReadOnlyDictionary<string, EffectValue> inputs)
        {
            var actor = board.Occupancies.Any(occupancy => occupancy.Coordinate.Equals(actorAt))
                ? board.Occupancies.First(occupancy => occupancy.Coordinate.Equals(actorAt)).HeroId
                : MoveActorBeside(board, actorAt);
            return new EffectCapabilityContext(
                new EffectNodeId("lane-push-probe-node"),
                EffectFailurePolicy.Mandatory,
                actor,
                actor,
                new CardInstanceId("lane-push-card"),
                new CardDefinitionReference(new CardDefinitionId("fixture-radius-context"), "fixture-rev-1"),
                new ActionDefinitionId("lane-push-action"),
                inputs,
                1,
                1,
                PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red).Participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(),
                    board, board, board));
        }

        private static HeroId MoveActorBeside(AdjacentMoveAuthority board, HexCoordinate at)
        {
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(BlueHero), at).Accepted, Is.True);
            return BlueHero;
        }

        private static HexCoordinate BesideCoordinate(AdjacentMoveAuthority board, HexCoordinate at) =>
            board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate)
                .First(coordinate =>
                    HexDistanceQuery.Calculate(coordinate, at) == 1 &&
                    board.IsEmptyPlayableSpace(coordinate));

        private static MinionId ClearBattleZoneExceptOne(AdjacentMoveAuthority board, TeamSide side)
        {
            var inside = BattleZoneQuery.FindMinionsInside(board)
                .Where(minion => minion.TeamSide == side)
                .ToArray();
            var survivor = inside.First(minion => minion.MinionType != StandardMinionType.Heavy);
            foreach (var minion in inside.Where(candidate => !candidate.MinionId.Equals(survivor.MinionId)))
            {
                Assert.That(
                    board.CommitValidatedRemoval(
                        BoardObjectReference.ForMinion(minion.MinionId)).Accepted, Is.True);
            }

            return survivor.MinionId;
        }

        private static void ClearBattleZone(AdjacentMoveAuthority board, TeamSide side)
        {
            foreach (var minion in BattleZoneQuery.FindMinionsInside(board)
                .Where(minion => minion.TeamSide == side))
            {
                Assert.That(
                    board.CommitValidatedRemoval(
                        BoardObjectReference.ForMinion(minion.MinionId)).Accepted, Is.True);
            }
        }

        private static AdjacentMoveAuthority CreateOfficialBoard()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            return new AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                Array.Empty<ObstacleOccupancy>(),
                setup.Minions);
        }

        private static AdjacentMoveAuthority CreateSyntheticLineBoard()
        {
            var mapId = new MapId("lane-push-line-map");
            var map = BoardMapDefinition.CreateSynthetic(
                mapId, 1, Enumerable.Range(0, 6).Select(q => new HexCoordinate(q, 0)).ToArray());
            return new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(5, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
        }
    }
}

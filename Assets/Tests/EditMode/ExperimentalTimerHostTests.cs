using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using NUnit.Framework;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExperimentalTimerHostTests
    {
        private static MatchHost Create(int allowance=1,int bank=0)
        {
            var s=OwnerScopedProjectionTests.CreateOfficialInitialState();
            return MatchHost.Compose(new MatchSessionInitialState(s.Scenario,s.Setup,s.AvailableMaps,s.InitialConfiguration,
                experiments:new ExperimentalMatchOptions(turnTimer:true,cardCommitmentSeconds:allowance,
                    cardResolutionSeconds:allowance,responseSeconds:allowance,cardUpgradeSeconds:allowance,teamBankSeconds:bank)));
        }
        internal static MatchHost WithTimer(MatchSession source)
        {
            var initial=(MatchSessionInitialState)typeof(MatchSession).GetField("initialState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(source);
            var copy=new MatchSession(new MatchSessionInitialState(initial.Scenario,initial.Setup,initial.AvailableMaps,
                initial.InitialConfiguration,initial.InitialTiming,initial.StagedProgression,
                new ExperimentalMatchOptions(turnTimer:true,cardCommitmentSeconds:1,cardResolutionSeconds:1,responseSeconds:1,cardUpgradeSeconds:1,teamBankSeconds:0)));
            foreach(var command in source.Projection.AcceptedCommands)
                Assert.That(copy.Execute(command).Accepted,Is.True,"Replay the original fixture before enabling its host clock.");
            return new MatchHost(copy);
        }
        private static MatchSession SessionOf(MatchHost host) =>
            (MatchSession)typeof(MatchHost).GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(host);
        private static MatchSession EngagedAt(string family)
        {
            var session=ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            for(int i=0;i<300;i++)
            {
                if(session.Projection.DecisionContract?.FamilyId.Value==family) return session;
                Assert.That(session.Execute(DeterministicMatchLoop.EnumerateLegalCommands(session.Projection)[0]).Accepted,Is.True);
            }
            throw new AssertionException("Engaged fixture did not reach "+family);
        }
        [Test] public void EveryDeclaredPendingFamilyMakesProgressWithAnExpiredClock()
        {
            var cases=new Func<MatchSession>[] {
                ()=>SessionOf((MatchHost)TeamAgreementTests.ReachAgreement(TeamAgreementKind.Initiative)),
                ()=>EngagedAt("movement"),()=>EngagedAt("attack-target"),()=>EngagedAt("effect"),()=>EngagedAt("turn-release"),
                ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse,
                DisplacedMinionCorrectionTests.CreateSubjectDecisionPause,
                DisplacedMinionCorrectionTests.CreateDestinationDecisionPause,
                ()=>SessionOf((MatchHost)TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle)),
                HeroRespawnTests.CreatePendingRespawnSession,
                ()=>ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _),
                ()=>LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true),
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause,
                ()=>HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _,out _),
                PendingDecisionTransportContractTests.CreateSessionAwaitingRevealRetrieval };
            var families=new System.Collections.Generic.HashSet<string>();
            foreach(var create in cases)
            {
                var h=WithTimer(create()); var name=h.Projection.DecisionContract.FamilyId.Value; families.Add(name);
                for(int step=0;step<12 && h.Projection.Phase!=RoundResolutionPhase.GameEnded;step++)
                {
                    Ready(h); var version=h.Version; h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
                    Assert.That(h.TimeoutFailure,Is.Empty,name);
                    Assert.That(h.Version,Is.GreaterThan(version),name+" must accept a command or release the turn.");
                }
            }
            Assert.That(families.Count,Is.EqualTo(13),"Every declared pending family must be exercised.");
        }
        [Test] public void SharedBoardVotesRejectStaleAndDirectCommandsAndResolveUnanimity()
        {
            var h=WithTimer(LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true));
            var command=h.Projection.DecisionContract.Commands.First();
            var owner=SeatProjectionFactory.OwnerOfStandingDecision(h.Projection);
            var seats=h.Seats.Seats.Where(owner.IsOwnedBy).ToArray(); var version=h.Version;
            Assert.That(h.SubmitBoardVoteForSeat(seats[0].Id,version-1,command),Is.False);
            Assert.That(h.SubmitForSeat(seats[0].Id,new SeatSubmission(new SubmissionId("bypass-vote"),version,command)).Accepted,Is.False);
            foreach(var seat in seats) Assert.That(h.SubmitBoardVoteForSeat(seat.Id,version,command),Is.True);
            Assert.That(h.Version,Is.GreaterThan(version));
            Assert.That(h.Projection.AcceptedCommands,Does.Contain(command));
        }
        [Test] public void SharedBoardTimeoutUsesTheOnlyVoteAndSnapshotKeepsTheBallot()
        {
            var h=WithTimer(LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true));
            var owner=SeatProjectionFactory.OwnerOfStandingDecision(h.Projection); var seats=h.Seats.Seats.Where(owner.IsOwnedBy).ToArray();
            Assert.That(seats.Length,Is.GreaterThan(1));
            var command=h.Projection.DecisionContract.Commands.Last();
            Assert.That(h.SubmitBoardVoteForSeat(seats[0].Id,h.Version,command),Is.True);
            var copy=WithTimer(LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true));
            Assert.That(copy.Submit(MatchSubmission.ForSnapshotRestore(h.CaptureSnapshot())).Accepted,Is.True);
            Assert.That(copy.ClockState.BoardVotes.Count,Is.EqualTo(1));
            Ready(copy); copy.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
            Ready(h); h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
            Assert.That(h.Projection.AcceptedCommands,Does.Contain(command));
            Assert.That(copy.Projection.Minions.Select(m=>m.MinionId.Value+":"+m.Coordinate),
                Is.EqualTo(h.Projection.Minions.Select(m=>m.MinionId.Value+":"+m.Coordinate)));
        }
        [TestCase(false)] [TestCase(true)]
        public void DelegatedChoiceUsesResponseTimeAndPausesTheOriginalResolution(bool enemy)
        {
            var staged=HanuUltimateTrickTests.DelegatedMatch.Stage(enemy);
            var source=SessionOf((MatchHost)staged.Host);
            var h=WithTimer(source);
            // This fixture stamps the printed delegation during its staged setup; carry its exact
            // authoritative snapshot rather than pretending its command log includes that stamp.
            Assert.That(SessionOf(h).RestoreSnapshot(source.CaptureSnapshot()).Accepted,Is.True);
            Ready(h); h.AdvanceClock(TimeSpan.FromMilliseconds(250).Ticks);
            var clocks=h.ClockState.Entries;
            var resolution=clocks.Single(e=>e.Kind==MatchClockKind.Resolution);
            var response=clocks.Single(e=>e.Kind==MatchClockKind.Response && e.IsCurrent);
            Assert.That(resolution.Owner,Is.EqualTo(staged.Delegated));
            Assert.That(resolution.RemainingTicks,Is.EqualTo(TimeSpan.TicksPerSecond));
            Assert.That(resolution.Running,Is.False);
            Assert.That(response.Owner,Is.EqualTo(staged.Decider));
            Assert.That(response.RemainingTicks,Is.EqualTo(TimeSpan.FromMilliseconds(750).Ticks));
        }
        [Test] public void DefenseResponsePreservesParentBudgetAndDirectorUndoRestoresIt()
        {
            var source=HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _,out _,out _);
            var h=WithTimer(source); Ready(h); h.AdvanceClock(TimeSpan.FromMilliseconds(200).Ticks);
            var root=h.ClockState.Entries.Single(e=>e.Kind==MatchClockKind.Resolution);
            var attack=h.Projection.DecisionContract.Commands.First(c=>c.HeroId.IsDefined);
            Assert.That(h.Submit(MatchSubmission.ForCommand(attack)).Accepted,Is.True);
            Ready(h); h.AdvanceClock(TimeSpan.FromMilliseconds(300).Ticks);
            Assert.That(h.ClockState.Entries.Single(e=>e.Id==root.Id).RemainingTicks,Is.EqualTo(root.RemainingTicks));
            Assert.That(h.ClockState.Entries.Any(e=>e.Kind==MatchClockKind.Response && e.Running),Is.True);
            Assert.That(h.Submit(MatchSubmission.ForUndo()).Accepted,Is.True);
            Assert.That(h.ClockState.Entries.Single(e=>e.Kind==MatchClockKind.Resolution).RemainingTicks,Is.EqualTo(root.RemainingTicks));
        }
        [Test] public void MinionBattleTimeoutUsesVotesAndFinishesTheConstrainedRemovalSet()
        {
            var h=WithTimer(SessionOf((MatchHost)TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle)));
            var seats=TeamAgreementTests.ChoosingSeats(h); var count=h.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var picks=TeamAgreementTests.PickFirstOffered(h,seats[0],count,"timer-vote");
            TeamAgreementTests.PickSet(h,seats[1],picks,"timer-second-vote");
            var before=h.Projection.AcceptedCommands.Count;
            for(int step=0;step<20 && h.Projection.PendingMinionBattleChoice!=null;step++)
            { Ready(h);h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks); }
            Assert.That(h.TimeoutFailure,Is.Empty); Assert.That(h.Projection.PendingMinionBattleChoice,Is.Null);
            var selected=h.Projection.AcceptedCommands.Skip(before).Where(c=>c.Kind==DirectorCommandKind.SelectMinionBattleRemoval)
                .Select(c=>c.MinionId.IsDefined?GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(c.MinionId):GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(c.HeroId));
            Assert.That(selected,Is.EquivalentTo(picks));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void TiedOrEmptyBoardVotesRemainDeterministicAcrossSnapshotRestore(int voters)
        {
            var h=WithTimer(LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true));
            var owner=SeatProjectionFactory.OwnerOfStandingDecision(h.Projection);var seats=h.Seats.Seats.Where(owner.IsOwnedBy).ToArray();
            var choices=h.Projection.DecisionContract.Commands.ToArray();
            for(int i=0;i<voters;i++) Assert.That(h.SubmitBoardVoteForSeat(seats[i].Id,h.Version,choices[i]),Is.True);
            var copy=WithTimer(LanePushTransitionTests.CreatePausedLanePushPlaceSession(sixPlayers:true));
            Assert.That(copy.Submit(MatchSubmission.ForSnapshotRestore(h.CaptureSnapshot())).Accepted,Is.True);
            Ready(h);Ready(copy);h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);copy.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
            var chosen=h.Projection.AcceptedCommands.Last(c=>c.Kind==DirectorCommandKind.AnswerSystemTransitionStep);
            if(voters>0) Assert.That(choices.Take(voters),Does.Contain(chosen));
            Assert.That(copy.Projection.Minions.Select(m=>m.MinionId.Value+":"+m.Coordinate),
                Is.EqualTo(h.Projection.Minions.Select(m=>m.MinionId.Value+":"+m.Coordinate)));
        }
        [Test] public void PartialRuneAssignmentCompletesAValidPermutationWithoutReplacingAcceptedPairs()
        {
            var session=ConfiguredMatchFixtures.CreateEngagedStagedSnorriSession();
            var silver=session.Projection.Cards.Single(c=>c.DefinitionReference.Equals(GuardsDigital.Core.Cards.Verified.SnorriSilverFace.Silver.Reference));
            for(int i=0;i<30 && (!session.Projection.SelectedActor.IsDefined || !session.Projection.SelectedActor.CardInstanceId.Equals(silver.Id));i++)
            {
                var p=session.Projection;var offers=DeterministicMatchLoop.EnumerateLegalCommands(p);
                var command=offers.FirstOrDefault(c=>c.Kind==DirectorCommandKind.CommitCard && c.CardInstanceId.Equals(silver.Id)) ??
                    offers.FirstOrDefault(c=>c.Kind==DirectorCommandKind.ChooseTiedActor && c.CardInstanceId.Equals(silver.Id)) ??
                    offers.FirstOrDefault(c=>c.Kind==DirectorCommandKind.PerformHold) ?? offers[0];
                Assert.That(session.Execute(command).Accepted,Is.True);
            }
            Assert.That(session.Projection.SelectedActor.CardInstanceId,Is.EqualTo(silver.Id));
            var skill=GuardsDigital.Core.Cards.Verified.SnorriSilverFace.Silver.ActionDefinitions.Single(a=>a.Kind==ActionKind.Skill);
            Assert.That(session.Execute(DirectorCommand.StartAction(skill.Id)).Accepted,Is.True);
            Assert.That(session.Projection.PendingEffectChoice.ValueVocabulary,Is.EqualTo(GuardsDigital.Core.Rules.EffectResolution.EffectChoiceValueVocabulary.RuneTurnSlot));
            var first=session.Projection.DecisionContract.Commands.Single(c=>c.EffectChoice?.Candidate.Value.IntegerValue==3);
            Assert.That(session.Execute(first).Accepted,Is.True);
            int guard=0;
            while(session.Projection.PendingEffectChoice!=null && guard++<8)
            {
                var p=session.Projection;var answer=TimeoutChoicePolicy.Choose(p,p.DecisionContract.Commands,n=>n-1);
                Assert.That(session.Execute(answer).Accepted,Is.True);
            }
            Assert.That(guard,Is.LessThan(8)); var runes=session.Projection.MarkersHeldBy(silver.OwnerId).ToArray();
            Assert.That(runes.Length,Is.EqualTo(4));Assert.That(runes.Select(r=>r.TurnSlotIndex),Is.EquivalentTo(new int?[]{1,2,3,4}));
            Assert.That(runes.Single(r=>r.Definition.Reference.Equals(GuardsDigital.Core.Cards.Verified.SnorriComponentContent.Runes[0].Reference)).TurnSlotIndex,Is.EqualTo(3));
        }
        [Test] public void UpgradeTimeoutRanksOnlyTheOfferedTakenCardsByInitiative()
        {
            var session=ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);var p=session.Projection;
            var commands=p.DecisionContract.Commands.Where(c=>c.SystemTransitionChoice.ItemId.Equals(p.DecisionContract.Commands[0].SystemTransitionChoice.ItemId)).ToArray();
            int Rank(DirectorCommand c) {var id=GuardsDigital.Core.Rules.Progression.HeroLevelUpOption.Read(c.SystemTransitionChoice.OptionId).TakenCardInstanceId;
                var card=p.Cards.Single(v=>v.Id.Equals(id));return p.Definitions.Single(d=>d.Reference.Equals(card.DefinitionReference)).Initiative.Value;}
            var answer=TimeoutChoicePolicy.Choose(p,commands,n=>n-1);
            Assert.That(Rank(answer),Is.EqualTo(commands.Min(Rank)));Assert.That(session.Execute(answer).Accepted,Is.True);
        }
        [Test] public void ReadinessLeaseNeverRetroactivelyChargesPausedTime()
        {
            var h=Create(10,20);h.SetTimerStarted(true);h.AdvanceClock(TimeSpan.FromSeconds(100).Ticks,true);
            h.AdvanceClock(TimeSpan.FromSeconds(30).Ticks,false);
            Assert.That(h.ClockState.Entries.All(e=>e.RemainingTicks==TimeSpan.FromSeconds(10).Ticks),Is.True);
            h.AdvanceClock(TimeSpan.TicksPerSecond);
            Assert.That(h.ClockState.Entries.All(e=>e.RemainingTicks==TimeSpan.FromSeconds(9).Ticks),Is.True);
        }
        [Test] public void OrdinarySeatUndoCannotRefundTheLiveResolutionClock()
        {
            var h=Create(10,20);
            OwnerScopedProjectionTests.DriveThroughSeats(h,1200,p=>p.PendingTurnRelease==null && h.Seats.Seats.Any(seat=>h.ProjectionForSeat(seat.Id).IsSeatUndoAvailable));
            var seat=h.Seats.Seats.First(seat=>h.ProjectionForSeat(seat.Id).IsSeatUndoAvailable);
            Ready(h);h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);var before=h.ClockState;
            var result=h.ClientForSeat(seat.Id).SubmitUndo(new SeatUndoSubmission(new SubmissionId("timed-seat-undo"),h.Version));
            Assert.That(result.Accepted,Is.True,result.Detail);
            Assert.That(h.ClockState.Entries.Single(e=>e.Kind==MatchClockKind.Resolution).RemainingTicks,
                Is.EqualTo(before.Entries.Single(e=>e.Kind==MatchClockKind.Resolution).RemainingTicks));
            Assert.That(h.ClockState.RedBankTicks,Is.EqualTo(before.RedBankTicks));Assert.That(h.ClockState.BlueBankTicks,Is.EqualTo(before.BlueBankTicks));
        }
        private static void Ready(MatchHost host)
        { host.SetTimerStarted(true); foreach(var seat in host.Seats.Seats) host.SetClockReadiness(seat.Id,host.Version,true); }
        [TestCase("")] [TestCase("timer-test")]
        public void ClockOnlyWireUpdatesPreserveGameplayProjectionAndLogIdentity(string key)
        {
            var h=Create(10,20); var wire=PrivateOnlineWireHarness.Compose(h,key); wire.SeatAll(); wire.ReadyAndPickEverySeat();
            var machine=wire.Client(1);
            var adapter=new NetworkMatchClient(machine,h.Projection,MatchContentCatalogs.ForThisBuild());
            var record=machine.LatestProjection; var projected=adapter.Projection;
            var projectionRevision=machine.ProjectionRevision; var logRevision=machine.MatchLogRevision;
            h.SetTimerStarted(true); machine.ReportTimerReadiness(true); wire.Pump();
            h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks); wire.Session.PublishClockUpdate(); wire.Pump();
            Assert.That(machine.LatestProjection,Is.SameAs(record));
            Assert.That(adapter.Projection,Is.SameAs(projected),"A clock tick cannot rebuild any gameplay read model.");
            Assert.That(machine.ProjectionRevision,Is.EqualTo(projectionRevision));
            Assert.That(machine.MatchLogRevision,Is.EqualTo(logRevision));
            Assert.That(machine.LatestClock.Entries.Count(e=>e.RemainingTicks==TimeSpan.FromSeconds(8).Ticks),Is.EqualTo(1));
            Assert.That(machine.IsWaitingForHost,Is.False);
            Assert.That(machine.RecoveredProjectionGapCount,Is.Zero);
            wire.DisconnectClient(1); var before=h.ClockState;
            h.AdvanceClock(TimeSpan.FromSeconds(7).Ticks,wire.Session.IsPaused);
            Assert.That(h.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(before.Entries.Select(e=>e.RemainingTicks)));
        }
        [Test] public void ClockFramesNeverAcknowledgeCommandsOrApplyToAnotherGameVersion()
        {
            var h=Create(10,20);var wire=PrivateOnlineWireHarness.Compose(h);wire.SeatAll();wire.ReadyAndPickEverySeat();
            var machine=wire.Client(1);var original=machine.LatestProjection;
            Assert.That(machine.TrySubmitCommand(original.AuthorizedCommands[0],out _),Is.True);
            Assert.That(machine.IsWaitingForHost,Is.True);
            void Deliver(long sequence,long version)
            {
                var bytes=NetworkMessageCodec.ClockUpdateFrame(new NetworkEnvelope(machine.SessionCorrelation,
                    machine.ConnectionEpoch,sequence,0),version,h.ClockState);
                machine.OnFrame(new SessionFrame(SessionMessageKind.ClockUpdate,bytes.Skip(PrivateSessionProtocol.FrameHeaderBytes).ToArray()));
            }
            var revision=machine.ClockDisplayRevision;
            Deliver(1,original.Version+1);
            Assert.That(machine.ClockDisplayRevision,Is.EqualTo(revision));
            Deliver(2,original.Version);
            Assert.That(machine.ClockDisplayRevision,Is.EqualTo(revision+1));
            Deliver(2,original.Version);
            Assert.That(machine.ClockDisplayRevision,Is.EqualTo(revision+1),"Duplicate ticks are discarded.");
            Assert.That(machine.IsWaitingForHost,Is.True,"Only a gameplay acknowledgement can release the pending command.");
            Assert.That(machine.LatestProjection,Is.SameAs(original));
            wire.Pump();
            Assert.That(machine.IsWaitingForHost,Is.False);
            Assert.That(machine.RecoveredProjectionGapCount,Is.Zero,"Clock sequences must not consume gameplay sequences.");
        }
        [Test] public void ReclaimedSeatMustReportReadinessAgainBeforeItsClockResumes()
        {
            var h=Create(10,20);var wire=PrivateOnlineWireHarness.Compose(h,"timer-reconnect");wire.SeatAll();wire.ReadyAndPickEverySeat();
            h.SetTimerStarted(true);wire.Client(1).ReportTimerReadiness(true);wire.Pump();h.AdvanceClock(TimeSpan.TicksPerSecond);
            var before=h.ClockState.Entries.Select(e=>e.RemainingTicks).ToArray();wire.DisconnectClient(1);
            h.AdvanceClock(TimeSpan.FromSeconds(8).Ticks,wire.Session.IsPaused);
            var replacement=wire.ReconnectClient(1);
            h.AdvanceClock(TimeSpan.TicksPerSecond,wire.Session.IsPaused);
            Assert.That(h.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(before),"A new connection has not yet completed its visual resynchronization.");
            wire.Client(replacement).ReportTimerReadiness(true);wire.Pump();h.AdvanceClock(TimeSpan.TicksPerSecond,wire.Session.IsPaused);
            Assert.That(h.ClockState.Entries.Count(e=>e.RemainingTicks==TimeSpan.FromSeconds(8).Ticks),Is.EqualTo(1));
            var reconnected=wire.Client(replacement); var projection=reconnected.LatestProjection;
            var projectionRevision=reconnected.ProjectionRevision; var displayRevision=reconnected.ClockDisplayRevision;
            wire.Session.PublishClockUpdate(); wire.Pump();
            Assert.That(reconnected.ClockDisplayRevision,Is.EqualTo(displayRevision+1));
            Assert.That(reconnected.LatestProjection,Is.SameAs(projection));
            Assert.That(reconnected.ProjectionRevision,Is.EqualTo(projectionRevision));
            Assert.That(reconnected.LatestClock.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(h.ClockState.Entries.Select(e=>e.RemainingTicks)));
            Assert.That(reconnected.RecoveredProjectionGapCount,Is.Zero);
        }
        [Test] public void StaleBusyReportsCannotFreezeAnExpiredMatch()
        {
            var h=Create(); h.SetTimerStarted(true);
            for(int i=0;i<31;i++)
            { foreach(var seat in h.Seats.Seats) h.SetClockReadiness(seat.Id,h.Version,false); h.AdvanceClock(TimeSpan.TicksPerSecond); }
            Assert.That(h.Version,Is.GreaterThan(0));
        }
        [Test] public void PendingChoicePolicyCompletesEngagedCombatAndOptionalResponses()
        {
            var session=ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var families=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<220 && session.Projection.Phase!=RoundResolutionPhase.GameEnded;i++)
            {
                var p=session.Projection; var offered=DeterministicMatchLoop.EnumerateLegalCommands(p);
                var command=p.DecisionContract==null ? offered[0] : TimeoutChoicePolicy.Choose(p,offered,n=>n-1);
                if(p.DecisionContract!=null) families.Add(p.DecisionContract.FamilyId.Value);
                var result=session.Execute(command); Assert.That(result.Accepted,Is.True,result.Detail);
            }
            Assert.That(families,Does.Contain("movement")); Assert.That(families,Does.Contain("attack-target"));
            Assert.That(families,Does.Contain("defense")); Assert.That(families,Does.Contain("effect"));
            var removal=ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause();
            var answer=TimeoutChoicePolicy.Choose(removal.Projection,removal.Projection.DecisionContract.Commands,n=>0);
            Assert.That(answer.RemovalPrevention,Is.EqualTo(removal.Projection.PendingRemovalPrevention.DeclineInvocation));
            Assert.That(removal.Execute(answer).Accepted,Is.True);
        }
        [Test] public void EmptyBankTimeoutsPlayThroughCompleteRoundsAndUpgradePauses()
        {
            var h=Create(); int guard=0; var families=new System.Collections.Generic.HashSet<string>();
            while(h.Projection.RoundIndex<3 && h.Projection.Phase!=RoundResolutionPhase.GameEnded && guard++<500)
            {
                if(h.Projection.DecisionContract!=null) families.Add(h.Projection.DecisionContract.FamilyId.Value);
                Ready(h); var version=h.Version; h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
                Assert.That(h.Version,Is.GreaterThan(version),"An expired ready match must advance: "+h.Projection.Phase+" / "+h.Projection.DecisionContract?.FamilyId);
            }
            Assert.That(h.TimeoutFailure,Is.Empty); Assert.That(guard,Is.LessThan(500));
            Assert.That(h.Projection.RoundIndex>=3 || h.Projection.Phase==RoundResolutionPhase.GameEnded,Is.True);
            Assert.That(families,Does.Contain("system-transition"));
        }
        [Test] public void SnapshotRoundTripPreservesNonDefaultClocksAndDirectorUndoJournal()
        {
            var h=Create(10,30); Ready(h); h.AdvanceClock(TimeSpan.FromSeconds(3).Ticks);
            var prior=h.ClockState; var command=h.ProjectionForSeat(h.Seats.Seats[0].Id).AuthorizedCommands.First(c=>c.Kind==DirectorCommandKind.CommitCard);
            Assert.That(h.Submit(MatchSubmission.ForCommand(command)).Accepted,Is.True);
            var payload=h.CaptureSnapshot(); var restored=Create(10,30);
            Assert.That(restored.Submit(MatchSubmission.ForSnapshotRestore(payload)).Accepted,Is.True);
            Assert.That(restored.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(h.ClockState.Entries.Select(e=>e.RemainingTicks)));
            Assert.That(h.Submit(MatchSubmission.ForUndo()).Accepted,Is.True);
            Assert.That(h.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(prior.Entries.Select(e=>e.RemainingTicks)));
            Assert.That(restored.Projection.IsHistoryAvailable,Is.False);
            var stable=restored.CaptureSnapshot();
            Assert.That(restored.Submit(MatchSubmission.ForSnapshotRestore("invalid")).Accepted,Is.False);
            Assert.That(restored.CaptureSnapshot(),Is.EqualTo(stable));
        }
        [Test] public void PauseAndVersionBoundReadinessDoNotChargeUntilActionable()
        {
            var h=Create(10,20); h.SetTimerStarted(true); var before=h.ClockState;
            h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
            Assert.That(h.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(before.Entries.Select(e=>e.RemainingTicks)));
            Ready(h); h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks,true);
            Assert.That(h.ClockState.Entries.Select(e=>e.RemainingTicks),Is.EqualTo(before.Entries.Select(e=>e.RemainingTicks)));
            h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks,false);
            Assert.That(h.ClockState.Entries.All(e=>e.RemainingTicks==TimeSpan.FromSeconds(8).Ticks),Is.True);
            var seat=h.Seats.Seats[0]; h.SetClockReadiness(seat.Id,h.Version-1,false);
            Assert.That(h.ClockState.Entries.First(e=>e.Owner.Equals(seat.HeroId)).Running,Is.True);
        }
        [Test] public void ManualSubmissionAndTimeoutCannotBothCommitTheSameCard()
        {
            var h=Create(); Ready(h); var seat=h.Seats.Seats[0];
            var old=h.ProjectionForSeat(seat.Id); var command=old.AuthorizedCommands.First(c=>c.Kind==DirectorCommandKind.CommitCard);
            h.AdvanceClock(TimeSpan.FromSeconds(2).Ticks);
            var result=h.SubmitForSeat(seat.Id,new SeatSubmission(new SubmissionId("late"),old.Version,command));
            Assert.That(result.Accepted,Is.False);
            Assert.That(h.Projection.AcceptedCommands.Count(c=>c.Kind==DirectorCommandKind.CommitCard && c.CardInstanceId.Equals(command.CardInstanceId)),Is.LessThanOrEqualTo(1));
        }
    }
}

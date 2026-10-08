using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public sealed partial class MatchHost
    {
        private MatchClock clock;
        private readonly DecisionWindowTracker decisionWindows = new DecisionWindowTracker();
        private bool acceptingTimeout;
        public IReadOnlyList<DecisionWindowMeasurement> DecisionWindows
        {
            get { lock (gate) return decisionWindows.Completed.ToArray(); }
        }
        private sealed class TimerCheckpoint
        {
            public MatchClockState State;
            public string Turn;
            public string[] Stack;
            public long Ordinal;
            public ulong Seed, Draw;
            public Dictionary<string,IReadOnlyDictionary<RuleObject,int>> Tallies;
        }
        private TimerCheckpoint CaptureTimerCheckpoint() => new TimerCheckpoint { State=ClockState,
            Turn=clockTurn, Stack=responseStack.ToArray(), Ordinal=responseOrdinal, Seed=timeoutSeed, Draw=timeoutDraw,
            Tallies=timedVoteTallies.ToDictionary(p=>p.Key,p=>p.Value) };
        private void RestoreTimerCheckpoint(TimerCheckpoint value,bool refund)
        {
            clock.Restore(value.State,refund); clockTurn=value.Turn;
            responseStack.Clear(); responseStack.AddRange(value.Stack); responseOrdinal=refund ? value.Ordinal : Math.Max(responseOrdinal,value.Ordinal);
            boardVotes.Clear(); foreach(var vote in value.State.BoardVotes) boardVotes.Add(vote.Voter,vote);
            boardVoteVersion=Version; boardVoteRevision++;
            timedVoteTallies.Clear(); foreach(var pair in value.Tallies) timedVoteTallies.Add(pair.Key,pair.Value);
            if (refund) { timeoutSeed=value.Seed; timeoutDraw=value.Draw; }
        }
        private readonly Dictionary<int, TimerCheckpoint> clockCheckpoints = new Dictionary<int, TimerCheckpoint>();
        private readonly Dictionary<SeatId, (long Version, bool Ready, long Age)> clockReadiness = new Dictionary<SeatId, (long,bool,long)>();
        private readonly List<string> responseStack = new List<string>();
        private string clockTurn = string.Empty;
        private long responseOrdinal;
        private ulong timeoutSeed, timeoutDraw;
        private bool timerStarted;
        private bool timerPaused;
        private string timeoutFailure = string.Empty;
        public string TimeoutFailure { get { lock(gate) return timeoutFailure; } }
        public MatchClockState ClockState { get { lock(gate) { SynchronizeClocks(); var state=clock.Capture();
                if(boardVoteVersion!=Version) {boardVotes.Clear();boardVoteVersion=Version;}
                return new MatchClockState(state.Enabled,state.Revision+boardVoteRevision,state.Wave,state.RedBankTicks,state.BlueBankTicks,state.Entries,boardVotes.Values); } } }
        private long boardVoteVersion=-1, boardVoteRevision;
        private readonly Dictionary<string,IReadOnlyDictionary<RuleObject,int>> timedVoteTallies=new Dictionary<string,IReadOnlyDictionary<RuleObject,int>>();
        private readonly Dictionary<HeroId,BoardChoiceVote> boardVotes=new Dictionary<HeroId,BoardChoiceVote>();
        public bool SubmitBoardVoteForSeat(SeatId seatId,long version,DirectorCommand command)
        {
            lock(gate)
            {
                var seat=CurrentSeats().Find(seatId); var p=session.Projection;
                if(executing || !Experiments.TurnTimer || seat==null || version!=Version || command==null || !SharedBoardChoice.RequiresVote(p,command)) return false;
                var owner=SeatProjectionFactory.OwnerOfStandingDecision(p);
                if(!owner.IsOwnedBy(seat) || !SeatProjectionFactory.AuthorizedCommands(p,seat,owner).Contains(command)) return false;
                if(boardVoteVersion!=Version) {boardVotes.Clear();boardVoteVersion=Version;}
                boardVoteRevision++;
                boardVotes[seat.HeroId]=new BoardChoiceVote(seat.HeroId,SharedBoardChoice.Position(p,command),command);
                var deciding=CurrentSeats().Seats.Where(s=>owner.IsOwnedBy(s)).ToArray();
                if(deciding.All(s=>boardVotes.TryGetValue(s.HeroId,out var vote) && vote.Command.Equals(command)))
                {
                    executing=true;
                    try { AcceptTimeout(command,seat); ContinueEndOfRound(); }
                    finally { executing=false; }
                    boardVotes.Clear();
                }
                return true;
            }
        }
        private sealed class TimedDecision
        {
            public string Id;
            public MatchSeat Seat;
            public MatchClockKind Kind;
            public string Context;
            public IReadOnlyList<DirectorCommand> Commands;
        }
        public void ResetClockReadiness(SeatId seat)
        { lock(gate) { clockReadiness.Remove(seat); } }
        public void SetClockReadiness(SeatId seat, long version, bool ready)
        {
            lock(gate)
            {
                if (version != Version || CurrentSeats().Find(seat) == null) return;
                if (clockReadiness.TryGetValue(seat,out var previous) && previous.Version==version)
                {
                    if (previous.Ready || !ready) return;
                }
                clockReadiness[seat] = (version, ready, 0);
            }
        }
        public void SetTimerStarted(bool started)
        { lock(gate) { timerStarted = started; SynchronizeClocks(); } }
        public void AdvanceClock(long elapsedTicks, bool paused = false)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            lock(gate)
            {
                if (executing || !timerStarted) return;
                timerPaused = paused;
                var decisions = SynchronizeClocks();
                if (paused) return;
                decisionWindows.Advance(elapsedTicks);
                if (Experiments.TurnTimer) clock.Advance(elapsedTicks);
                foreach (var pair in clockReadiness.ToArray())
                    clockReadiness[pair.Key] = (pair.Value.Version, pair.Value.Ready,
                        Math.Min(TimeSpan.FromSeconds(30).Ticks, pair.Value.Age + Math.Min(elapsedTicks,TimeSpan.FromSeconds(30).Ticks)));
                decisions = SynchronizeClocks();
                if (!Experiments.TurnTimer) return;
                executing = true;
                try
                {
                    // Every iteration must accept a real command or stop at a fresh actionable budget.
                    var seen = new HashSet<long>();
                    for (int step=0; step<512; step++)
                    {
                        var p=session.Projection;
                        if (p.Phase == RoundResolutionPhase.GameEnded) return;
                        if (!seen.Add(p.Version)) throw new InvalidOperationException("Timeout did not advance authority.");
                        if (p.Phase == RoundResolutionPhase.EndOfRound && p.DecisionContract == null)
                        { ContinueEndOfRound(); decisions=SynchronizeClocks(); if (session.Projection.Version == p.Version) return; continue; }
                        var expired=decisions.FirstOrDefault(d=>clock.Expired(d.Id) && IsClockReady(d.Seat,p.Version));
                        if (expired == null) return;
                        var actor=SeatProjectionFactory.DecidingActor(p);
                        var entry=clock.Capture().Entries.Single(e=>e.Id==expired.Id);
                        if (p.PendingTurnRelease==null && entry.Kind==MatchClockKind.Resolution && expired.Seat.Owns(actor) &&
                            !expired.Commands.Any(c=>c.Kind==DirectorCommandKind.PerformHold) && CanSeatUndo(expired.Seat,out _))
                        {
                            var undo=Execute(MatchSubmission.ForUndo());
                            if (!undo.Accepted) throw new InvalidOperationException("Authorized timeout Undo was refused: "+undo.Detail);
                            RefreshSeatUndoWatermark(default,null,afterRewind:true);
                        }
                        else
                        {
                            IReadOnlyDictionary<RuleObject,int> votes=null;
                            if (p.PendingMinionBattleChoice != null)
                            {
                                var agreement=teamAgreement.ProjectionFor(p,CurrentSeats(),expired.Seat);
                                if(!timedVoteTallies.TryGetValue(expired.Id,out votes))
                                { votes=agreement.Participants.SelectMany(v=>v.Picks).GroupBy(v=>v).ToDictionary(g=>g.Key,g=>g.Count()); timedVoteTallies[expired.Id]=votes; }
                            }
                            var offered=expired.Commands;
                            if(offered.All(c=>SharedBoardChoice.RequiresVote(p,c)))
                            {
                                int Count(DirectorCommand c)=>boardVoteVersion==p.Version?boardVotes.Values.Count(v=>v.Command.Equals(c)):0;
                                var best=offered.Max(Count); offered=offered.Where(c=>Count(c)==best).ToArray();
                            }
                            var command=TimeoutChoicePolicy.Choose(p,offered,NextTimeoutRandom,votes);
                            AcceptTimeout(command,expired.Seat);
                        }
                        decisions=SynchronizeClocks();
                    }
                    throw new InvalidOperationException("Timeout exceeded its progress bound before reaching a live decision.");
                }
                catch(Exception e) { timeoutFailure=e.Message; throw; }
                finally { executing=false; }
            }
        }
        private void AcceptTimeout(DirectorCommand command, MatchSeat seat)
        {
            acceptingTimeout = true;
            try
            {
                var result=Execute(MatchSubmission.ForCommand(command));
                if (!result.Accepted) throw new InvalidOperationException("Timeout command refused: "+command+" / "+result.Detail);
                RefreshSeatUndoWatermark(seat?.Id ?? default,command);
            }
            finally { acceptingTimeout = false; }
        }
        private int NextTimeoutRandom(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (timeoutSeed == 0) timeoutSeed=SetupRandomnessAuthority.DrawSeed().Value;
            ulong value;
            unchecked { value=SetupRandomnessAuthority.Derive(new SetupRandomnessSeed(timeoutSeed),SetupRandomnessPurpose.ExperimentalTimeout,++timeoutDraw); }
            return (int)(value % (ulong)count);
        }
        private bool IsClockReady(MatchSeat seat,long version)
        {
            if (timerPaused || !timerStarted) return false;
            if (!clockReadiness.TryGetValue(seat.Id,out var ready)) return false;
            // A version-bound readiness report cannot hold the game forever. Thirty seconds is a
            // bounded fail-safe for a client that stops completing its visual transition.
            return ready.Version==version && (ready.Ready || ready.Age>=TimeSpan.FromSeconds(30).Ticks);
        }
        private List<TimedDecision> SynchronizeClocks()
        {
            var result=new List<TimedDecision>();
            if (clock == null) return result;
            var p=session.Projection;
            clock.SetWave(p.RemainingWaveCounters);
            foreach(var seat in CurrentSeats().Seats)
                if (!clockReadiness.TryGetValue(seat.Id,out var readiness) || readiness.Version!=p.Version)
                    clockReadiness[seat.Id]=(p.Version,false,0);
            var owner=SeatProjectionFactory.OwnerOfStandingDecision(p);
            var release=p.PendingTurnRelease;
            var actor=p.SelectedActor.IsDefined ? p.SelectedActor.ActorId : release?.ActorId ?? default;
            var card=p.SelectedActor.IsDefined ? p.SelectedActor.CardInstanceId : release?.CardInstanceId ?? default;
            var actorSeat=CurrentSeats().FindByHero(actor);
            var turn=actor.IsDefined && card.IsDefined
                ? "resolution:"+p.RoundIndex+":"+p.TurnIndex+":"+card.Value
                : string.Empty;
            if (clockTurn != turn) { clockTurn=turn; responseStack.Clear(); }
            if (actorSeat!=null && turn.Length>0)
                clock.Ensure(turn,MatchClockKind.Resolution,actor,actorSeat.TeamSide);
            foreach (var seat in CurrentSeats().Seats)
            {
                var commands=SeatProjectionFactory.AuthorizedCommands(p,seat,owner);
                if (p.DecisionContract != null && owner.IsOwnedBy(seat) &&
                    (p.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.Initiative) ||
                     p.DecisionContract.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))) commands=p.DecisionContract.Commands;
                if (commands.Count==0) continue;
                string id; MatchClockKind kind;
                if (p.Phase==RoundResolutionPhase.AwaitingCommitments && p.DecisionContract==null)
                { id="commitment:"+p.RoundIndex+":"+p.TurnIndex+":"+seat.HeroId.Value; kind=MatchClockKind.Commitment; }
                else if (p.PendingSystemTransitionStep?.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId)==true)
                {
                    foreach(var group in commands.GroupBy(c=>c.SystemTransitionChoice.ItemId))
                    {
                        var key="upgrade:"+p.PendingSystemTransitionStep.InstanceId.Value+":"+group.Key.Value;
                        clock.Ensure(key,MatchClockKind.Upgrade,seat.HeroId,seat.TeamSide);
                        result.Add(new TimedDecision{Id=key,Seat=seat,Kind=MatchClockKind.Upgrade,
                            Context="Card upgrade",Commands=group.ToArray()});
                    }
                    continue;
                }
                else if (turn.Length>0 && actorSeat!=null && !owner.IsTeamOwned)
                {
                    // Returning to the attacker during the defender's text is another response;
                    // the original resolution stays paused until that Defense continuation ends.
                    var inDefenseText = session is MatchSession live && live.IsResolvingDefenseText;
                    if (seat.Owns(actor) && !inDefenseText) { id=turn; kind=MatchClockKind.Resolution; responseStack.Clear(); }
                    else
                    {
                        var states=clock.Entries;
                        var existing=responseStack.FindIndex(key=>states.Any(e=>e.Id==key && e.Owner.Equals(seat.HeroId)));
                        if (existing>=0) responseStack.RemoveRange(existing+1,responseStack.Count-existing-1);
                        else responseStack.Add(turn+":response:"+(++responseOrdinal));
                        id=responseStack.Last(); kind=MatchClockKind.Response;
                    }
                }
                else
                {
                    var scope=p.PendingSystemTransitionStep != null ? p.PendingSystemTransitionStep.InstanceId.Value+":"+commands[0].SystemTransitionChoice.ItemId.Value :
                        p.PendingEffectChoice != null ? p.PendingEffectChoice.ResolutionId.Value :
                        p.PendingMinionBattleChoice != null ? commands[0].EndOfRoundResolutionId.Value :
                        p.DecisionContract?.FamilyId.Value ?? p.Phase.ToString();
                    id="response:"+p.RoundIndex+":"+p.TurnIndex+":"+scope+":"+(owner.IsTeamOwned ? seat.TeamSide.ToString() : seat.HeroId.Value);
                    kind=MatchClockKind.Response;
                }
                clock.Ensure(id,kind,owner.IsTeamOwned?default:seat.HeroId,seat.TeamSide);
                var context = kind == MatchClockKind.Commitment ? "Card commitment" :
                    kind == MatchClockKind.Resolution ? "Card resolution" :
                    p.DecisionContract?.FamilyId.Value ?? commands[0].Kind.ToString();
                result.Add(new TimedDecision{Id=id,Seat=seat,Kind=kind,Context=context,Commands=commands});
            }
            var retained=new HashSet<string>(result.Select(d=>d.Id));
            if (turn.Length>0)
                foreach(var entry in clock.Entries.Where(e=>(e.Id==turn || e.Id.StartsWith(turn+":response:",StringComparison.Ordinal)))) retained.Add(entry.Id);
            var retainedClocks = new HashSet<string>(retained);
            // Retraction reopens this same commitment allowance, never a fresh budget.
            if (p.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var prefix = "commitment:" + p.RoundIndex + ":" + p.TurnIndex + ":";
                foreach (var entry in clock.Entries.Where(e => e.Kind == MatchClockKind.Commitment &&
                    e.Id.StartsWith(prefix, StringComparison.Ordinal))) retainedClocks.Add(entry.Id);
            }
            clock.Retain(retainedClocks);
            foreach(var key in timedVoteTallies.Keys.Where(k=>!retained.Contains(k)).ToArray()) timedVoteTallies.Remove(key);
            var current = new HashSet<string>(result.Select(d=>d.Id));
            var running = new HashSet<string>(result.Where(d=>IsClockReady(d.Seat,p.Version)).Select(d=>d.Id));
            clock.SetCurrent(current);
            clock.SetRunning(running);
            string MeasurementId(TimedDecision decision) =>
                decision.Id+"|seat:"+decision.Seat.Id.Value;
            var measurementRunning = new HashSet<string>(result
                .Where(d=>IsClockReady(d.Seat,p.Version)).Select(MeasurementId));
            decisionWindows.Reconcile(result.Select(d=>new DecisionWindowDescriptor
                {Id=MeasurementId(d),Kind=d.Kind,Context=d.Context,Seat=d.Seat}),
                measurementRunning,p.Version,pendingDecisionClosure,retained);
            pendingDecisionClosure=null;
            return result;
        }
        private (TimerCheckpoint Current, Dictionary<int,TimerCheckpoint> Journal)? pendingTimerRestore;
        private void WriteTimerCheckpoint(System.IO.BinaryWriter w,TimerCheckpoint value)
        {
            ExperimentalWireCodec.WriteClock(w,value.State); w.Write(value.Turn); w.Write(value.Ordinal);
            w.Write(value.Seed); w.Write(value.Draw); w.Write(value.Stack.Length);
            foreach(var key in value.Stack) w.Write(key);
            w.Write(value.Tallies.Count);
            foreach(var tally in value.Tallies.OrderBy(t=>t.Key,StringComparer.Ordinal))
            {
                w.Write(tally.Key); w.Write(tally.Value.Count);
                foreach(var vote in tally.Value.OrderBy(v=>v.Key.Kind).ThenBy(v=>v.Key.HeroId.Value??v.Key.MinionId.Value,StringComparer.Ordinal))
                {w.Write((int)vote.Key.Kind);w.Write(vote.Key.Kind==RuleObjectKind.Hero?vote.Key.HeroId.Value:vote.Key.MinionId.Value);w.Write(vote.Value);}
            }
        }
        private TimerCheckpoint ReadTimerCheckpoint(System.IO.BinaryReader r)
        {
            var state=ExperimentalWireCodec.ReadClock(r); var turn=ExperimentalWireCodec.ReadId(r);
            var ordinal=r.ReadInt64(); var seed=r.ReadUInt64(); var draw=r.ReadUInt64(); var count=r.ReadInt32();
            if (ordinal<0 || count<0 || count>4096) throw new ArgumentException("Invalid timer checkpoint.");
            var stack=new string[count]; for(int i=0;i<count;i++) stack[i]=ExperimentalWireCodec.ReadId(r);
            if (stack.Any(id=>!state.Entries.Any(e=>e.Id==id))) throw new ArgumentException("Missing response frame.");
            var tallyCount=r.ReadInt32(); if(tallyCount<0 || tallyCount>4096) throw new ArgumentException("Invalid vote tally count.");
            var tallies=new Dictionary<string,IReadOnlyDictionary<RuleObject,int>>();
            for(int i=0;i<tallyCount;i++)
            {
                var key=ExperimentalWireCodec.ReadId(r); var size=r.ReadInt32();
                if(size<0 || size>4096 || tallies.ContainsKey(key)) throw new ArgumentException("Invalid vote tally.");
                var votes=new Dictionary<RuleObject,int>();
                for(int j=0;j<size;j++)
                {
                    var kind=(RuleObjectKind)r.ReadInt32(); var id=ExperimentalWireCodec.ReadId(r); var n=r.ReadInt32();
                    if((kind!=RuleObjectKind.Hero && kind!=RuleObjectKind.Minion) || n<0 || n>10) throw new ArgumentException("Invalid vote.");
                    var unit=kind==RuleObjectKind.Hero?RuleObject.ForHero(new HeroId(id)):RuleObject.ForMinion(new MinionId(id));
                    if(votes.ContainsKey(unit)) throw new ArgumentException("Duplicate vote."); votes.Add(unit,n);
                }
                tallies.Add(key,votes);
            }
            return new TimerCheckpoint{State=state,Turn=turn,Ordinal=ordinal,Seed=seed,Draw=draw,Stack=stack,Tallies=tallies};
        }
        private void WriteTimerSnapshot(System.IO.BinaryWriter w)
        {
            // Core snapshots deliberately start without replay ancestry. Only the current timer
            // checkpoint belongs in that snapshot; live Director Undo uses the in-memory journal.
            SynchronizeClocks(); WriteTimerCheckpoint(w,CaptureTimerCheckpoint()); w.Write(0);
        }
        private (TimerCheckpoint Current,Dictionary<int,TimerCheckpoint> Journal) ReadTimerSnapshot(System.IO.BinaryReader r)
        {
            var current=ReadTimerCheckpoint(r); var count=r.ReadInt32();
            if (count!=0) throw new ArgumentException("Core snapshots do not carry replay ancestry.");
            var journal=new Dictionary<int,TimerCheckpoint>();
            for(int i=0;i<count;i++) { var key=r.ReadInt32(); if(key<0 || journal.ContainsKey(key)) throw new ArgumentException("Invalid timer history key."); journal.Add(key,ReadTimerCheckpoint(r)); }
            return (current,journal);
        }
        private TimerCheckpoint timedBefore;
        private int timedBeforeCount;
        private DecisionWindowClosure pendingDecisionClosure;
        private void BeforeTimedSubmission()
        {
            pendingTimerRestore=null;
            SynchronizeClocks();
            if (!Experiments.TurnTimer) return;
            timedBeforeCount=session.Projection.AcceptedCommands.Count; timedBefore=CaptureTimerCheckpoint();
        }
        private void AfterTimedSubmission(MatchSubmission submission,bool directorUndo)
        {
            if (Experiments.TurnTimer)
            {
                if (timedBefore!=null) clockCheckpoints[timedBeforeCount]=timedBefore;
                if (submission.Kind==MatchSubmissionKind.RestoreSnapshot && pendingTimerRestore.HasValue)
                {
                    var restored=pendingTimerRestore.Value; RestoreTimerCheckpoint(restored.Current,true);
                    clockCheckpoints.Clear(); foreach(var pair in restored.Journal) clockCheckpoints.Add(pair.Key,pair.Value);
                    pendingTimerRestore=null;
                }
                if (submission.Kind==MatchSubmissionKind.Undo && clockCheckpoints.TryGetValue(session.Projection.AcceptedCommands.Count,out var checkpoint))
                    RestoreTimerCheckpoint(checkpoint,directorUndo);
            }
            if (submission.Kind==MatchSubmissionKind.Restart)
            { clock=new MatchClock(Experiments); clockCheckpoints.Clear(); clockTurn=string.Empty; responseStack.Clear(); timedVoteTallies.Clear(); boardVotes.Clear(); boardVoteVersion=Version; timeoutDraw=0; }
            pendingDecisionClosure=ClosureOf(submission,directorUndo);
            if (submission.Kind!=MatchSubmissionKind.Command)
                decisionWindows.CloseAll(session.Projection.Version,pendingDecisionClosure);
            SynchronizeClocks();
        }

        private DecisionWindowClosure ClosureOf(MatchSubmission submission, bool directorUndo)
        {
            if (acceptingTimeout)
                return new DecisionWindowClosure {Reason=DecisionWindowCloseReason.TimeoutCommand,
                    Command=submission.Command?.Kind};
            switch (submission.Kind)
            {
                case MatchSubmissionKind.Command:
                    return new DecisionWindowClosure {Reason=DecisionWindowCloseReason.PlayerCommand,
                        Command=submission.Command?.Kind};
                case MatchSubmissionKind.Undo:
                    return new DecisionWindowClosure {Reason=directorUndo
                        ? DecisionWindowCloseReason.DirectorUndo : DecisionWindowCloseReason.Undo};
                case MatchSubmissionKind.Restart:
                    return new DecisionWindowClosure {Reason=DecisionWindowCloseReason.Restart};
                case MatchSubmissionKind.RestoreSnapshot:
                    return new DecisionWindowClosure {Reason=DecisionWindowCloseReason.SnapshotRestore};
                default:
                    return new DecisionWindowClosure {Reason=DecisionWindowCloseReason.StateAdvanced};
            }
        }
    }
}

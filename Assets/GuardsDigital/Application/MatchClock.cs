using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    public enum MatchClockKind { Commitment = 1, Resolution = 2, Response = 3, Upgrade = 4 }
    public sealed class MatchClockEntry
    {
        public MatchClockEntry(string id, MatchClockKind kind, HeroId owner, TeamSide team, long remainingTicks, bool running, bool current = true)
        {
            if (string.IsNullOrEmpty(id) || remainingTicks < 0 ||
                !Enum.IsDefined(typeof(MatchClockKind), kind) || (team != TeamSide.Red && team != TeamSide.Blue))
                throw new ArgumentException("Invalid clock entry.");
            Id = id; Kind = kind; Owner = owner; Team = team; RemainingTicks = remainingTicks; Running = running; IsCurrent = current;
        }
        public string Id { get; }
        public MatchClockKind Kind { get; }
        public HeroId Owner { get; }
        public TeamSide Team { get; }
        public long RemainingTicks { get; }
        public bool Running { get; }
        public bool IsCurrent { get; }
        public bool UsesBank => RemainingTicks == 0;
    }
    public sealed class MatchClockState
    {
        public static readonly MatchClockState Disabled = new MatchClockState(false, 0, 0, 0, 0, Array.Empty<MatchClockEntry>());
        public MatchClockState(bool enabled, long revision, int wave, long redBankTicks, long blueBankTicks,
            IEnumerable<MatchClockEntry> entries, IEnumerable<BoardChoiceVote> boardVotes = null)
        {
            if (revision < 0 || redBankTicks < 0 || blueBankTicks < 0) throw new ArgumentException("Invalid clock state.");
            var values = (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            if (values.Any(e => e == null) || values.Select(e => e.Id).Distinct().Count() != values.Length)
                throw new ArgumentException("Clock identities must be unique.");
            Enabled = enabled; Revision = revision; Wave = wave; RedBankTicks = redBankTicks; BlueBankTicks = blueBankTicks;
            Entries = Array.AsReadOnly(values);
            var votes=(boardVotes??Enumerable.Empty<BoardChoiceVote>()).ToArray();
            if(votes.Any(v=>v==null) || votes.Select(v=>v.Voter).Distinct().Count()!=votes.Length)
                throw new ArgumentException("Board voters must be unique.");
            BoardVotes=Array.AsReadOnly(votes);
        }
        public bool Enabled { get; }
        public long Revision { get; }
        public int Wave { get; }
        public long RedBankTicks { get; }
        public long BlueBankTicks { get; }
        public IReadOnlyList<MatchClockEntry> Entries { get; }
        public IReadOnlyList<BoardChoiceVote> BoardVotes { get; }
        public long Bank(TeamSide team) => team == TeamSide.Red ? RedBankTicks : BlueBankTicks;
    }
    /// <summary>Pure elapsed-time accounting. The host supplies actionable owners; no wall clock or Unity dependency.</summary>
    internal sealed class MatchClock
    {
        private readonly ExperimentalMatchOptions options;
        private readonly Dictionary<string, MatchClockEntry> entries = new Dictionary<string, MatchClockEntry>(StringComparer.Ordinal);
        private long redBank, blueBank, revision;
        private int wave = int.MinValue;
        public MatchClock(ExperimentalMatchOptions options) { this.options = options; }
        public MatchClockState Capture() => options.TurnTimer
            ? new MatchClockState(true, revision, wave, redBank, blueBank, entries.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
            : MatchClockState.Disabled;
        public void SetWave(int value)
        {
            if (wave == value) return;
            wave = value; redBank = blueBank = TimeSpan.FromSeconds(options.TeamBankSeconds).Ticks; revision++;
        }
        public void Ensure(string id, MatchClockKind kind, HeroId owner, TeamSide side)
        {
            if (entries.ContainsKey(id)) return;
            var seconds = kind == MatchClockKind.Commitment ? options.CardCommitmentSeconds :
                kind == MatchClockKind.Resolution ? options.CardResolutionSeconds :
                kind == MatchClockKind.Upgrade ? options.CardUpgradeSeconds : options.ResponseSeconds;
            entries.Add(id, new MatchClockEntry(id, kind, owner, side, TimeSpan.FromSeconds(seconds).Ticks, false, false)); revision++;
        }
        public void Retain(ISet<string> ids)
        {
            foreach (var id in entries.Keys.Where(id => !ids.Contains(id)).ToArray()) { entries.Remove(id); revision++; }
        }
        public void SetCurrent(ISet<string> ids)
        {
            foreach(var e in entries.Values.ToArray())
                if(e.IsCurrent!=ids.Contains(e.Id))
                { entries[e.Id]=new MatchClockEntry(e.Id,e.Kind,e.Owner,e.Team,e.RemainingTicks,e.Running,ids.Contains(e.Id)); revision++; }
        }
        public void SetRunning(ISet<string> ids)
        {
            foreach (var pair in entries.ToArray())
            {
                var e = pair.Value; var running = ids.Contains(e.Id);
                if (e.Running == running) continue;
                entries[pair.Key] = new MatchClockEntry(e.Id, e.Kind, e.Owner, e.Team, e.RemainingTicks, running, e.IsCurrent); revision++;
            }
        }
        public void Advance(long elapsedTicks)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            if (!options.TurnTimer || elapsedTicks == 0) return;
            // Union of active bank intervals: the earliest allowance expiry starts one charge per team.
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var active = entries.Values.Where(e => e.Team == side && e.Running).ToArray();
                if (active.Length == 0) continue;
                long bankUse = Math.Max(0, elapsedTicks - active.Min(e => e.RemainingTicks));
                if (side == TeamSide.Red) redBank = Math.Max(0, redBank - bankUse);
                else blueBank = Math.Max(0, blueBank - bankUse);
                foreach (var e in active)
                    entries[e.Id] = new MatchClockEntry(e.Id, e.Kind, e.Owner, e.Team,
                        Math.Max(0, e.RemainingTicks - elapsedTicks), true, e.IsCurrent);
                revision++;
            }
        }
        public bool Expired(string id) => entries.TryGetValue(id, out var e) && e.RemainingTicks == 0 &&
            (e.Team == TeamSide.Red ? redBank : blueBank) == 0;
        public void Restore(MatchClockState state, bool refund)
        {
            var previous = Capture();
            entries.Clear(); wave = state.Wave;
            redBank = refund ? state.RedBankTicks : Math.Min(previous.RedBankTicks, state.RedBankTicks);
            blueBank = refund ? state.BlueBankTicks : Math.Min(previous.BlueBankTicks, state.BlueBankTicks);
            foreach (var e in state.Entries)
            {
                var old = previous.Entries.FirstOrDefault(v => v.Id == e.Id);
                var remaining = refund || old == null ? e.RemainingTicks : Math.Min(e.RemainingTicks, old.RemainingTicks);
                entries.Add(e.Id, new MatchClockEntry(e.Id, e.Kind, e.Owner, e.Team, remaining, false, e.IsCurrent));
            }
            revision++;
        }
    }
}

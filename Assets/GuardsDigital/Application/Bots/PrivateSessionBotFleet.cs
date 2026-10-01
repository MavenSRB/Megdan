using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application.Bots
{
    /// <summary>Host-owned bots on the ordinary encoded seat protocol, alongside human TCP clients.</summary>
    public sealed class PrivateSessionBotFleet : IPrivateSessionOutbox, IDisposable
    {
        public const int MaxDeliveriesPerPump = 256;
        private const int MaxQueuedDeliveries = 1024;

        private sealed class Bot : IPrivateSessionClientOutbox
        {
            private readonly PrivateSessionBotFleet fleet;
            internal Bot(PrivateSessionBotFleet fleet, int ordinal, SeatId seat)
            { this.fleet = fleet; Ordinal = ordinal; Seat = seat; }
            internal readonly int Ordinal;
            internal readonly SeatId Seat;
            internal readonly PrivateSessionFrameAssembler ToHost = new PrivateSessionFrameAssembler();
            internal readonly PrivateSessionFrameAssembler FromHost = new PrivateSessionFrameAssembler();
            internal readonly HashSet<DirectorCommand> Refused = new HashSet<DirectorCommand>();
            internal PrivateSessionClient Machine;
            internal NetworkMatchClient Client;
            internal bool Closed;
            internal long NextThink;
            internal long RefusedVersion = -1;
            internal DirectorCommand PendingCommand;
            internal string PendingSubmission;
            public void Send(byte[] frame) => fleet.Enqueue(this, true, frame);
            public void Close() => fleet.Enqueue(this, true, null);
        }

        private readonly struct Delivery
        {
            internal Delivery(Bot bot, bool toHost, byte[] bytes)
            { Bot = bot; ToHost = toHost; Bytes = bytes; }
            internal readonly Bot Bot;
            internal readonly bool ToHost;
            internal readonly byte[] Bytes;
        }

        private readonly IPrivateSessionOutbox humanOutbox;
        private readonly Func<IMatchReadModel> trustedContent;
        private readonly MatchContentCatalogs catalogs;
        private readonly IPrivateSessionNonceSource nonceSource;
        private readonly string sessionKey;
        private readonly List<Bot> bots = new List<Bot>();
        private readonly Dictionary<int, Bot> byOrdinal = new Dictionary<int, Bot>();
        private readonly Queue<Delivery> deliveries = new Queue<Delivery>();
        private readonly DeterministicBotRandom random;
        private readonly DeterministicBotRandom draftRandom;
        private long nowTicks;
        private long thinkTicks;
        private int nextOrdinal;
        private int nextBot;
        private bool disposed;

        public PrivateSessionBotFleet(
            IMatchHost matchHost, PrivateSessionIdentity identity, string sessionKey,
            IPrivateSessionOutbox humanOutbox, IPrivateSessionNonceSource nonceSource,
            Func<IMatchReadModel> trustedContent, MatchContentCatalogs catalogs, int seed = 0, int? draftSeed = null)
        {
            this.humanOutbox = humanOutbox ?? throw new ArgumentNullException(nameof(humanOutbox));
            this.trustedContent = trustedContent ?? throw new ArgumentNullException(nameof(trustedContent));
            this.catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
            this.nonceSource = nonceSource;
            this.sessionKey = sessionKey ?? string.Empty;
            random = new DeterministicBotRandom(seed);
            draftRandom = new DeterministicBotRandom(draftSeed ?? seed);
            Host = new PrivateSessionHost(matchHost, identity, this.sessionKey, this, nonceSource);
        }

        public PrivateSessionHost Host { get; }
        public IReadOnlyCollection<SeatId> Seats => bots.Where(bot => !bot.Closed).Select(bot => bot.Seat).ToArray();
        public int PendingDeliveries => deliveries.Count;
        public int AcceptedCommands { get; private set; }
        public string LastFailure { get; private set; } = string.Empty;

        /// <summary>
        /// Milestone 8.38e. What the host last REFUSED, and from which seat. The fleet tolerates a
        /// refusal by design - it remembers the command for that version and tries another - so the
        /// command that was refused used to be discarded unrecorded. That is exactly the fact a
        /// dealing needs when a new hero's published offers and the host's authorization disagree,
        /// which is the defect class this tier exists to find, so it is kept here rather than
        /// reconstructed by instrumenting the fixture.
        /// </summary>
        public string LastRefusal { get; private set; } = string.Empty;

        public int FillOpenSeats(long ticks, double thinkSeconds = 2.0)
        {
            if (disposed) throw new ObjectDisposedException(nameof(PrivateSessionBotFleet));
            if (double.IsNaN(thinkSeconds) || double.IsInfinity(thinkSeconds) || thinkSeconds < 0 || thinkSeconds > 60)
                throw new ArgumentOutOfRangeException(nameof(thinkSeconds));
            if (Host.HasMatchStarted || Host.HasEnded) return 0;
            nowTicks = ticks;
            thinkTicks = (long)(thinkSeconds * TimeSpan.TicksPerSecond);
            var added = 0;
            foreach (var entry in Host.Roster)
            {
                if (entry.Occupancy != SeatOccupancyState.Open || bots.Any(bot => !bot.Closed && bot.Seat.Equals(entry.Seat)))
                    continue;
                // TCP assigns positive ordinals. Local links use a disjoint, host-owned namespace.
                var bot = new Bot(this, --nextOrdinal, entry.Seat);
                bot.Machine = new PrivateSessionClient(Host.Identity, entry.Seat, sessionKey, bot,
                    nonceSource, () => nowTicks);
                bots.Add(bot);
                byOrdinal.Add(bot.Ordinal, bot);
                bot.Machine.OnConnected();
                Host.OnConnected(bot.Ordinal, nowTicks);
                added++;
            }
            Pump();
            return added;
        }

        /// <summary>Bounded wire delivery and at most one bot decision; never owns an update loop.</summary>
        public bool Tick(long ticks)
        {
            if (disposed) return false;
            nowTicks = ticks;
            Pump();
            foreach (var bot in bots) ObserveResult(bot);
            if (Host.IsPaused || Host.HasEnded) return false;
            for (var examined = 0; examined < bots.Count; examined++)
            {
                var bot = bots[nextBot];
                nextBot = (nextBot + 1) % bots.Count;
                if (bot.Closed || nowTicks < bot.NextThink) continue;
                if (!TryDecide(bot)) continue;
                bot.NextThink = nowTicks + thinkTicks;
                Pump();
                foreach (var value in bots) ObserveResult(value);
                return true;
            }
            return false;
        }

        private bool TryDecide(Bot bot)
        {
            var machine = bot.Machine;
            if (machine.State == PrivateSessionClientState.Rejected || machine.State == PrivateSessionClientState.Faulted)
            {
                LastFailure = "Bot " + bot.Seat.Value + " stopped: " + machine.State + " / " + machine.RejectionReason;
                return false;
            }
            if (machine.State != PrivateSessionClientState.Seated || machine.IsWaitingForHost) return false;
            if (!machine.HasMatchStarted)
            {
                var lobby = PrivateLobbyModel.ForClient(machine);
                if (lobby.Offers(PrivateLobbyControl.Ready)) return machine.TrySetReadiness(true);
                if (!lobby.Selection.IsOwnPick) return false;
                var legal = lobby.Selection.Heroes.Where(option => option.IsAvailable).ToArray();
                return legal.Length > 0 && machine.TryChooseHero(legal[draftRandom.Next(legal.Length)].HeroIdValue);
            }
            if (machine.LatestProjection == null) return false;
            if (bot.Client == null) bot.Client = new NetworkMatchClient(machine, trustedContent(), catalogs);
            var projection = bot.Client.Projection;
            if (projection.Outcome.HasEnded) return false;
            // Admission precedes drafting: the grant's initial hero is not the final picked hero.
            var hero = new HeroId(machine.LatestProjection.HeroId);
            var agreement = projection.TeamAgreement;
            var own = agreement?.Participants.FirstOrDefault(value => value.HeroId.Equals(hero));
            if (own != null)
            {
                TeamAgreementCommand statement = null;
                if (agreement.Kind == TeamAgreementKind.Initiative && !own.HasStatement && agreement.Candidates.Count > 0)
                    statement = agreement.AuthorizedCommands.FirstOrDefault(value =>
                        value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                        value.PreferredCardInstanceId.Equals(agreement.Candidates[0].CardInstanceId));
                else if (agreement.Kind == TeamAgreementKind.MinionBattle)
                    // Milestone 9.29. Every fleet bot is a bot; any other choosing seat is a human
                    // whose picks the bots copy.
                    statement = MinionBattlePickPolicy.NextStatement(agreement, hero, IsFleetHero);
                if (statement != null) return machine.TrySubmitAgreement(statement, out _);
            }
            var version = machine.LatestProjection.Version;
            if (version != bot.RefusedVersion) { bot.Refused.Clear(); bot.RefusedVersion = version; }
            var commands = machine.LatestProjection.AuthorizedCommands.Where(command => !bot.Refused.Contains(command)).ToArray();
            if (commands.Length == 0) return false;
            // Effect confirmations are scored by the tactical policy: blindly confirming was one
            // source of repeated ineffective Skills. The tactical policy scores only this owner's
            // published choices. Deliberately omit lookahead: a lobby bot must not borrow the host's
            // hidden hands or commitments.
            var selected = BotPolicy.SelectPrivateSessionTactics(commands, projection, hero, random)
                ?? commands[random.Next(commands.Length)];
            if (!machine.TrySubmitCommand(selected, out var submission)) return false;
            bot.PendingCommand = selected;
            bot.PendingSubmission = submission;
            return true;
        }

        /// <summary>
        /// Milestone 9.29. Whether a hero is played by one of this fleet's bots, read from each
        /// bot's own latest grant because the admission hero is not the drafted one.
        /// </summary>
        private bool IsFleetHero(HeroId hero) =>
            bots.Any(value => !value.Closed && value.Machine?.LatestProjection != null &&
                hero.Equals(new HeroId(value.Machine.LatestProjection.HeroId)));

        private void ObserveResult(Bot bot)
        {
            var result = bot.Machine.LastSubmissionResult;
            if (bot.PendingCommand == null || result == null || result.SubmissionId != bot.PendingSubmission) return;
            if (result.Accepted) AcceptedCommands++;
            else
            {
                LastRefusal = bot.Seat.Value + " v" + result.Version + " " + bot.PendingCommand.Kind +
                    " -> " + result.RejectionReason + "/" + result.SessionRejectionReason + " " + result.Detail;
                if (result.Version == bot.RefusedVersion) bot.Refused.Add(bot.PendingCommand);
            }
            bot.PendingCommand = null;
            bot.PendingSubmission = null;
        }

        public void Send(int connectionOrdinal, byte[] frame)
        {
            if (connectionOrdinal >= 0) { humanOutbox.Send(connectionOrdinal, frame); return; }
            if (byOrdinal.TryGetValue(connectionOrdinal, out var bot)) Enqueue(bot, false, frame);
        }

        public void Close(int connectionOrdinal)
        {
            if (connectionOrdinal >= 0) { humanOutbox.Close(connectionOrdinal); return; }
            if (byOrdinal.TryGetValue(connectionOrdinal, out var bot)) Enqueue(bot, false, null);
        }

        private void Enqueue(Bot bot, bool toHost, byte[] bytes)
        {
            if (disposed || bot.Closed) return;
            if (deliveries.Count >= MaxQueuedDeliveries) throw new InvalidOperationException("Local bot delivery bound exceeded.");
            deliveries.Enqueue(new Delivery(bot, toHost, bytes));
        }

        private void Pump()
        {
            for (var count = 0; count < MaxDeliveriesPerPump && deliveries.Count > 0; count++)
            {
                var delivery = deliveries.Dequeue();
                var bot = delivery.Bot;
                if (bot.Closed) continue;
                if (delivery.Bytes == null)
                {
                    bot.Closed = true;
                    Host.OnDisconnected(bot.Ordinal);
                    bot.Machine.OnDisconnected();
                    continue;
                }
                var assembler = delivery.ToHost ? bot.ToHost : bot.FromHost;
                if (!assembler.Add(delivery.Bytes, 0, delivery.Bytes.Length))
                {
                    if (delivery.ToHost) Host.OnStreamRejected(bot.Ordinal, assembler.Rejection);
                    else bot.Machine.OnStreamRejected(assembler.Rejection);
                    continue;
                }
                while (assembler.TryDequeue(out var frame))
                {
                    if (delivery.ToHost) Host.OnFrame(bot.Ordinal, frame);
                    else bot.Machine.OnFrame(frame);
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            foreach (var bot in bots.Where(value => !value.Closed)) bot.Machine.SendGoodbye();
            Pump();
            foreach (var bot in bots.Where(value => !value.Closed))
            {
                bot.Closed = true;
                Host.OnDisconnected(bot.Ordinal);
                bot.Machine.OnDisconnected();
            }
            disposed = true;
            deliveries.Clear();
        }
    }
}

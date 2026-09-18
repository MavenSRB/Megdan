using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.47. One transient banner over the middle of the screen, telling the players the
    /// game state changed: commit a card, resolve your card, minion battle. The user's brief, during
    /// the 2026-09-18 playtest preparation: "text over the screen to let the players know game state
    /// changed. It pops up, then fades away after like a second or two" - big blood-red letters with
    /// a black outline on a shaded box, impossible to miss, centre screen.
    ///
    /// <para>The words a banner shows: an eyebrow naming the round and turn, and the line that says
    /// what the player does now. Round start is bundled into the round's first commit banner rather
    /// than announced on its own, and a turn's end is never announced: the next banner already says
    /// it, and every extra banner makes the useful ones easier to ignore.</para>
    /// </summary>
    public readonly struct PhaseBannerViewModel : IEquatable<PhaseBannerViewModel>
    {
        public static readonly PhaseBannerViewModel None = default;

        public PhaseBannerViewModel(string key, string eyebrow, string line)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A banner is identified by the transition it announces.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentException("A banner says what the player does now.", nameof(line));
            }

            Key = key;
            Eyebrow = eyebrow ?? string.Empty;
            Line = line;
        }

        /// <summary>The transition this banner announces, once: the same key never shows twice.</summary>
        public string Key { get; }

        /// <summary>The small line above: the round and turn, or nothing.</summary>
        public string Eyebrow { get; }

        /// <summary>The big line: what the player does now.</summary>
        public string Line { get; }

        public bool HasBanner => Key != null;

        public bool Equals(PhaseBannerViewModel other) =>
            string.Equals(Key, other.Key, StringComparison.Ordinal) &&
            string.Equals(Eyebrow, other.Eyebrow, StringComparison.Ordinal) &&
            string.Equals(Line, other.Line, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PhaseBannerViewModel other && Equals(other);

        public override int GetHashCode() => Key == null ? 0 : StringComparer.Ordinal.GetHashCode(Key);

        public override string ToString() => HasBanner ? Eyebrow + " / " + Line : "(none)";
    }

    /// <summary>
    /// The banner's vocabulary, composed in one place so the presenter and its tests read the same
    /// words. Upper case throughout: the letters are the whole point of the surface.
    /// </summary>
    public static class PhaseBannerLabels
    {
        public const string CommitLine = "COMMIT A CARD";
        public const string ResolveOwnEyebrow = "YOUR TURN";
        public const string ResolveOwnLine = "RESOLVE YOUR CARD";
        public const string MinionBattleLine = "MINION BATTLE";

        public static string RoundAndTurn(int roundIndex, int turnIndex) =>
            "ROUND " + roundIndex + " · TURN " + turnIndex;

        public static string EndOfRound(int roundIndex) => "END OF ROUND " + roundIndex;

        public static PhaseBannerViewModel Commit(int roundIndex, int turnIndex) =>
            new PhaseBannerViewModel(
                "commit:" + roundIndex + ":" + turnIndex,
                RoundAndTurn(roundIndex, turnIndex),
                CommitLine);

        public static PhaseBannerViewModel ResolveOwn(int roundIndex, int turnIndex, HeroId actor) =>
            new PhaseBannerViewModel(
                ResolveKey(roundIndex, turnIndex, actor), ResolveOwnEyebrow, ResolveOwnLine);

        // Milestone 8.50 retired the other seats' "<NAME>'S TURN" line: the banner speaks only when
        // THIS player is up - "COMMIT A CARD", "YOUR TURN" and "MINION BATTLE" - at the user's word
        // after the 2026-09-18 playtest.

        public static PhaseBannerViewModel MinionBattle(int roundIndex) =>
            new PhaseBannerViewModel("battle:" + roundIndex, EndOfRound(roundIndex), MinionBattleLine);

        internal static string ResolveKey(int roundIndex, int turnIndex, HeroId actor) =>
            "resolve:" + roundIndex + ":" + turnIndex + ":" + actor.Value;
    }

    /// <summary>
    /// The few facts of a projection the banner turns on, read once per refresh. A struct so the
    /// detector below can be exercised on plain values, without a session behind it.
    /// </summary>
    public readonly struct PhaseSnapshot
    {
        public PhaseSnapshot(
            int roundIndex, int turnIndex, bool isCommitting, HeroId actingHero, bool isMinionBattle)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            IsCommitting = isCommitting;
            ActingHero = actingHero;
            IsMinionBattle = isMinionBattle;
        }

        public int RoundIndex { get; }
        public int TurnIndex { get; }

        /// <summary>The round is waiting on commitments: everyone chooses a card at once.</summary>
        public bool IsCommitting { get; }

        /// <summary>The hero whose turn is being resolved, or undefined between turns.</summary>
        public HeroId ActingHero { get; }

        /// <summary>The end of round stands on p.8's Minion Battle.</summary>
        public bool IsMinionBattle { get; }

        public static PhaseSnapshot Of(IMatchReadModel projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var acting = projection.IsTurnActive && projection.SelectedActor.IsDefined
                ? projection.SelectedActor.ActorId
                : default;
            var battle = projection.Phase == RoundResolutionPhase.EndOfRound &&
                (projection.CurrentEndOfRoundStage == EndOfRoundStage.MinionBattle ||
                    projection.PendingMinionBattleChoice != null);
            return new PhaseSnapshot(
                projection.RoundIndex,
                projection.TurnIndex,
                projection.Phase == RoundResolutionPhase.AwaitingCommitments,
                acting,
                battle);
        }
    }

    /// <summary>
    /// Decides, from one snapshot after another, which transition has just happened for THIS viewer.
    ///
    /// <para><b>The first snapshot primes and announces nothing.</b> A banner says "the state just
    /// changed"; a client that has only just composed - or only just rebound its seat, or resynced
    /// after a reconnect - has not watched anything change, and announcing the state it found would
    /// be a lie told to exactly the player least able to tell. Every key seen while priming is
    /// remembered as already announced.</para>
    ///
    /// <para><b>One banner per observation, the latest phase first.</b> A single refresh can carry a
    /// whole turn's worth of change (a bot's turn resolved between two frames); the player is told
    /// where the game IS, not walked through where it went: minion battle over resolve over commit.</para>
    /// </summary>
    public sealed class PhaseBannerCueDetector
    {
        private bool primed;
        private string lastCommitKey;
        private string lastResolveKey;
        private string lastBattleKey;

        public PhaseBannerViewModel Observe(
            PhaseSnapshot snapshot, HeroId viewer, Func<HeroId, string> heroDisplayName)
        {
            if (heroDisplayName == null) throw new ArgumentNullException(nameof(heroDisplayName));

            var commit = snapshot.IsCommitting ? PhaseBannerLabels.Commit(snapshot.RoundIndex, snapshot.TurnIndex) : PhaseBannerViewModel.None;
            // Milestone 8.50. The user, after the 2026-09-18 playtest: "I don't think I want those
            // banner letters for all the players. Just when a player is up. So only COMMIT A CARD and
            // YOUR TURN + MINION BATTLE." Another seat's turn is not this viewer's moment, so it is
            // no longer announced - to a seat or to the Director, who is up for nobody.
            var resolve = snapshot.ActingHero.IsDefined && viewer.IsDefined &&
                snapshot.ActingHero.Equals(viewer)
                    ? PhaseBannerLabels.ResolveOwn(snapshot.RoundIndex, snapshot.TurnIndex, snapshot.ActingHero)
                    : PhaseBannerViewModel.None;

            var battle = snapshot.IsMinionBattle ? PhaseBannerLabels.MinionBattle(snapshot.RoundIndex) : PhaseBannerViewModel.None;

            if (!primed)
            {
                primed = true;
                lastCommitKey = commit.Key;
                lastResolveKey = resolve.Key;
                lastBattleKey = battle.Key;
                return PhaseBannerViewModel.None;
            }

            var announced = PhaseBannerViewModel.None;
            if (battle.HasBanner && battle.Key != lastBattleKey)
            {
                announced = battle;
            }
            else if (resolve.HasBanner && resolve.Key != lastResolveKey)
            {
                announced = resolve;
            }
            else if (commit.HasBanner && commit.Key != lastCommitKey)
            {
                announced = commit;
            }

            // Every current key counts as seen, announced or not: a transition the viewer was shown
            // past in the same refresh is not owed later.
            if (battle.HasBanner) lastBattleKey = battle.Key;
            if (resolve.HasBanner) lastResolveKey = resolve.Key;
            if (commit.HasBanner) lastCommitKey = commit.Key;
            return announced;
        }

        /// <summary>Forget everything and prime again on the next observation.</summary>
        public void Reprime()
        {
            primed = false;
            lastCommitKey = null;
            lastResolveKey = null;
            lastBattleKey = null;
        }
    }

    /// <summary>
    /// Milestone 8.47. Reads the seat's projection on every refresh, and when a transition has just
    /// happened for this viewer, shows the banner once. Seat-scoped for the reason
    /// <see cref="ISeatScopedPresenter"/> states: "YOUR TURN" is only true on one seat.
    /// </summary>
    public sealed class PhaseBannerPresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly PhaseBannerUnityView view;
        private readonly PhaseBannerCueDetector detector = new PhaseBannerCueDetector();
        private HeroId viewerHeroId;

        public PhaseBannerPresenter(IMatchClient session, PhaseBannerUnityView view, HeroId viewerHeroId = default)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.viewerHeroId = viewerHeroId;
        }

        public HeroId HomeHeroId => viewerHeroId;

        /// <summary>The banner most recently announced, for tests and diagnostics.</summary>
        public PhaseBannerViewModel LastAnnounced { get; private set; } = PhaseBannerViewModel.None;

        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined || projection == null ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            viewerHeroId = heroId;
            // A rebind is not a transition: prime again on the state the seat is joining.
            detector.Reprime();
            Refresh();
            return true;
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (projection == null)
            {
                return;
            }

            var banner = detector.Observe(
                PhaseSnapshot.Of(projection),
                viewerHeroId,
                heroId => projection.Setup?.Heroes?
                    .FirstOrDefault(hero => hero.HeroId.Equals(heroId))?.DisplayName);
            if (!banner.HasBanner)
            {
                return;
            }

            LastAnnounced = banner;
            view.Show(banner);
        }
    }
}

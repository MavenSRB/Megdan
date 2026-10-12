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
    /// changed. It pops up, then fades away after like a second or two" - big team-coloured letters
    /// with a black outline on a shaded box, impossible to miss, centre screen.
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

        // Milestone 9.28. The user, on 8.35g's release pause: "big blood red lights should say
        // 'end turn', same way they say 'commit a card'". The eyebrow is the strip's own words for
        // the state; the line is the control's own label, so the banner names what to press.
        public const string EndTurnEyebrow = "TURN COMPLETE";
        public const string EndTurnLine = "END TURN";

        internal const string EndTurnKeyPrefix = "release:";

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

        /// <summary>
        /// Milestone 9.28. The viewer's own turn has finished resolving and waits on its release.
        /// Keyed by the turn rather than by the pause, so a rewind that reaches the release again
        /// inside the same turn is not announced twice - the flashing control still says it.
        /// </summary>
        public static PhaseBannerViewModel EndTurn(int roundIndex, int turnIndex, HeroId actor) =>
            new PhaseBannerViewModel(
                EndTurnKeyPrefix + roundIndex + ":" + turnIndex + ":" + actor.Value,
                EndTurnEyebrow,
                EndTurnLine);

        public static bool IsEndTurn(PhaseBannerViewModel model) =>
            model.HasBanner && model.Key.StartsWith(EndTurnKeyPrefix, StringComparison.Ordinal);

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
            int roundIndex,
            int turnIndex,
            bool isCommitting,
            HeroId actingHero,
            bool isMinionBattle,
            HeroId releasingHero = default)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            IsCommitting = isCommitting;
            ActingHero = actingHero;
            IsMinionBattle = isMinionBattle;
            ReleasingHero = releasingHero;
        }

        public int RoundIndex { get; }
        public int TurnIndex { get; }

        /// <summary>The round is waiting on commitments: everyone chooses a card at once.</summary>
        public bool IsCommitting { get; }

        /// <summary>The hero whose turn is being resolved, or undefined between turns.</summary>
        public HeroId ActingHero { get; }

        /// <summary>The end of round stands on p.8's Minion Battle.</summary>
        public bool IsMinionBattle { get; }

        /// <summary>
        /// Milestone 9.28. The hero whose finished turn waits on its release (8.35g), or undefined.
        /// Read off <see cref="IMatchReadModel.PendingTurnRelease"/>, whose actor is unscoped by
        /// 8.35g's own decision - the same fact the strip's "AWAITING RELEASE" line names.
        /// </summary>
        public HeroId ReleasingHero { get; }

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
                battle,
                projection.PendingTurnRelease?.ActorId ?? default);
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
    /// where the game IS, not walked through where it went: minion battle over the viewer's own
    /// release (9.28) over resolve over commit.</para>
    /// </summary>
    public sealed class PhaseBannerCueDetector
    {
        private bool primed;
        private string lastCommitKey;
        private string lastResolveKey;
        private string lastBattleKey;
        private string lastEndTurnKey;

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

            // Milestone 9.28. Only the seat whose own turn waits on it is up; the Director, who
            // answers for everybody, is up for nobody, exactly as 8.50 reads the turn banner.
            var endTurn = snapshot.ReleasingHero.IsDefined && viewer.IsDefined &&
                snapshot.ReleasingHero.Equals(viewer)
                    ? PhaseBannerLabels.EndTurn(snapshot.RoundIndex, snapshot.TurnIndex, snapshot.ReleasingHero)
                    : PhaseBannerViewModel.None;

            if (!primed)
            {
                primed = true;
                lastCommitKey = commit.Key;
                lastResolveKey = resolve.Key;
                lastBattleKey = battle.Key;
                lastEndTurnKey = endTurn.Key;
                return PhaseBannerViewModel.None;
            }

            var announced = PhaseBannerViewModel.None;
            if (battle.HasBanner && battle.Key != lastBattleKey)
            {
                announced = battle;
            }
            else if (endTurn.HasBanner && endTurn.Key != lastEndTurnKey)
            {
                announced = endTurn;
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
            if (endTurn.HasBanner) lastEndTurnKey = endTurn.Key;
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
            lastEndTurnKey = null;
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

        /// <summary>
        /// Milestone 8.46e (owed by 8.47). When this answers true at the moment a banner would show, the
        /// banner yields and the transition counts as announced: the decision window opening EXPANDED
        /// for this seat on the same refresh is itself the thing nobody misses, and two surfaces
        /// drawn over the middle of the screen at once would make both harder to read. A docked bar
        /// and a banner coexist, so the runtime wires this to the expanded state alone.
        /// </summary>
        public Func<bool> YieldsWhen { get; set; }

        /// <summary>The banner most recently yielded rather than shown, for tests and diagnostics.</summary>
        public PhaseBannerViewModel LastYielded { get; private set; } = PhaseBannerViewModel.None;

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

            var snapshot = PhaseSnapshot.Of(projection);
            var banner = detector.Observe(
                snapshot,
                viewerHeroId,
                heroId => projection.Setup?.Heroes?
                    .FirstOrDefault(hero => hero.HeroId.Equals(heroId))?.DisplayName);
            // Milestone 8.58. The yield is asked on EVERY refresh rather than only when a new banner
            // would show, and it CUTS one that is already on screen.
            //
            // The user's live Reload! capture is why: a banner announcing the turn was still fading
            // when the decision window expanded over it, so its letters read through the panel's right
            // half. 8.47's rule is that the banner yields to a window opening on the same refresh; a
            // banner that had already opened was outside it, and "the window is the thing nobody
            // misses" is not true while another surface is drawn through it.
            if (YieldsWhen?.Invoke() == true)
            {
                view.Hide();
                if (banner.HasBanner)
                {
                    LastYielded = banner;
                }

                return;
            }

            // Milestone 9.28. An END TURN banner is true only while the release it announced still
            // stands: once the player has pressed END TURN (or rewound past the pause) it comes down
            // at once, rather than telling them for another two seconds to do what they just did.
            if (PhaseBannerLabels.IsEndTurn(view.Current) &&
                !(snapshot.ReleasingHero.IsDefined && snapshot.ReleasingHero.Equals(viewerHeroId)))
            {
                view.Hide();
            }

            if (!banner.HasBanner)
            {
                return;
            }

            LastAnnounced = banner;
            view.Show(banner);
        }
    }
}

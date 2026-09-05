using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12. What the spotlight surface should be showing right now.
    /// </summary>
    public sealed class AttackSpotlightViewModel
    {
        public static readonly AttackSpotlightViewModel Empty = new AttackSpotlightViewModel(
            AttackSpotlightPhase.None, null, 0f, false, 0);

        public AttackSpotlightViewModel(
            AttackSpotlightPhase phase,
            AttackSpotlightScene scene,
            float framingWeight,
            bool isContinueArmed,
            int queuedSceneCount,
            bool snapToAuthoritative = false)
        {
            Phase = phase;
            Scene = scene;
            FramingWeight = framingWeight;
            IsContinueArmed = isContinueArmed;
            QueuedSceneCount = queuedSceneCount;
            SnapToAuthoritative = snapToAuthoritative;
        }

        public AttackSpotlightPhase Phase { get; }

        /// <summary>The attack being shown, or <c>null</c> when nothing is.</summary>
        public AttackSpotlightScene Scene { get; }

        /// <summary>0 is the ordinary board and HUD; 1 is the framed, tilted, chrome-free view.</summary>
        public float FramingWeight { get; }

        public bool IsContinueArmed { get; }

        public int QueuedSceneCount { get; }

        /// <summary>
        /// Whether the surface must drop everything it is doing and show authoritative state directly.
        /// Set by an interruption - an undo, a rewind, a restore, a lost projection - and it is what
        /// puts the camera and the HUD back with no easing at all.
        /// </summary>
        public bool SnapToAuthoritative { get; }

        /// <summary>Whether the spotlight owns the screen: chrome hidden, camera framed.</summary>
        public bool IsActive =>
            Phase == AttackSpotlightPhase.Entering ||
            Phase == AttackSpotlightPhase.Held ||
            Phase == AttackSpotlightPhase.Exiting;
    }

    public interface IAttackSpotlightView
    {
        void Render(AttackSpotlightViewModel model);
    }

    /// <summary>
    /// Milestone 8.35z12. The NINETEENTH AMENDMENT's attack spotlight: "lose the UI, zoom
    /// in, show the cards, show the smack, and have a continue button."
    ///
    /// <para><b>It reads the seat-public match log and nothing else</b>, for the reasons
    /// <see cref="KineticCuePresenter"/> states: the log is the one event stream Presentation can see,
    /// it is derived rather than stored, and milestone 8.35r's selection contract makes it IDENTICAL
    /// on every client. So every seat frames the same attack from the same row, and a spotlight can
    /// never show something the contract excluded.</para>
    ///
    /// <para><b>Both faces are already public at exactly the moment they are needed,</b> which is the
    /// feasibility answer the amendment records and it was verified at HEAD rather than assumed. The
    /// Attack row carries the attacking card's instance
    /// (<c>MatchLogProjection</c> passes <c>subjectCardInstanceId: Card(references)</c> on
    /// <c>AttackResolutionCompleted</c>); the Defense row carries the spent defense card's instance
    /// (from its own <c>DefenseFold.DefenseCardInstanceId</c>); and <c>SeatProjection.IsPublic</c>
    /// admits any face-up card in the turn area or a discard pile to every seat's
    /// <c>Cards</c>. There is no privacy work here and no new wire field, because there is nothing to
    /// send.</para>
    ///
    /// <para><b>The three safety rules are the cue layer's</b>, applied to scenes: a scene is built
    /// ONCE per Attack row keyed on that row's own sequence; the first projection a presenter ever
    /// sees primes the watermark and shows nothing (a client joining a match in progress must not
    /// watch forty spotlights); and a log that went BACKWARDS re-primes and interrupts whatever is on
    /// screen, because a spotlight describing an undone attack is the one thing this feature must
    /// never do.</para>
    /// </summary>
    public sealed class AttackSpotlightPresenter
    {
        private readonly IMatchClient session;
        private readonly IAttackSpotlightView view;
        private readonly AttackSpotlightStage stage = new AttackSpotlightStage();
        private IMatchReadModel lastProjection;
        private int observedSequence = NeverObserved;
        private int skippedWindowCount;
        private bool pendingSnap;
        private AttackSpotlightPhase lastRenderedPhase = AttackSpotlightPhase.None;
        private float lastRenderedWeight = -1f;

        private const int NeverObserved = -1;

        /// <remarks>
        /// It deliberately does NOT implement <see cref="ISeatScopedPresenter"/>. Nothing here
        /// branches on who is looking: milestone 8.35r's log is the same log for every seat, so every
        /// client frames the same attack, and the only seat-scoping a spotlight needs - which card
        /// faces this viewer may see - already happened upstream, in the seat projection this
        /// presenter reads. Declaring a viewer identity it never consults would put a fourth name on
        /// the binding list for no behaviour at all.
        /// </remarks>
        public AttackSpotlightPresenter(IMatchClient session, IAttackSpotlightView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>
        /// Raised the moment <see cref="DefersCues"/> stops being true, so a buffered cue layer can
        /// drain. It carries nothing: the only fact is "you may play again".
        /// </summary>
        public event Action CuesReleased;

        /// <summary>The lifecycle, exposed so a conductor can schedule it and a test can drive it.</summary>
        public AttackSpotlightStage Stage => stage;

        /// <summary>
        /// Whether the viewer's presentation is currently paced by a spotlight. It is what a
        /// conductor asks; it says nothing about the authority, which never waits.
        /// </summary>
        public bool HoldsStage => stage.HoldsStage;

        /// <summary>
        /// Whether a cue layer should hold its cues rather than play them - the amendment's "the cue
        /// queue buffers subsequent events".
        ///
        /// <para><b>Held is deliberately NOT a deferring phase, and that is the whole point.</b> The
        /// user's own sequence is "lose the UI, zoom in, show the cards, show the smack": the smack
        /// belongs at the END, framed, with both faces up. Deferring only while the camera is TRAVELLING
        /// - and again while it travels home - lands the lunge and the flash exactly when the viewer is
        /// looking at the two pieces, instead of half of it playing off-centre during a 450ms move. A
        /// cue that arrives during the hold or the exit waits for the next release, which is the
        /// buffering the amendment asks for.</para>
        /// </summary>
        public bool DefersCues => DefersCuesInPhase(stage.Phase);

        /// <summary>
        /// The deferral as a pure function of the phase - ONE definition, so the cue layer's hold and
        /// the conductor's `IConductedScene.DefersOtherScenes` gate cannot drift apart. The wave-one
        /// integration's first complete Play Mode run is why that matters: the conductor was gating on
        /// `HoldsStage`, which also covers HELD, and that kept the framed smack queued until the
        /// spotlight had closed.
        /// </summary>
        public static bool DefersCuesInPhase(AttackSpotlightPhase phase) =>
            phase == AttackSpotlightPhase.Requested ||
            phase == AttackSpotlightPhase.Entering ||
            phase == AttackSpotlightPhase.Exiting;

        /// <summary>The highest match-log sequence already turned into scenes.</summary>
        public int ObservedSequence => observedSequence;

        /// <summary>
        /// How many observations delivered more resolved attacks at once than a viewer could
        /// sensibly be walked through, and were therefore primed rather than shown. It is exposed so
        /// a defect that quietly skips every spotlight in a match is visible instead of silent.
        /// </summary>
        public int SkippedWindowCount => skippedWindowCount;

        /// <summary>The viewer's press, forwarded from whatever control the surface drew.</summary>
        public bool Continue()
        {
            if (!stage.Continue())
            {
                return false;
            }

            RenderCurrent();
            return true;
        }

        /// <summary>
        /// Stops the spotlight now and puts the surface back. The composition calls it on teardown;
        /// a conductor calls it to take the stage back.
        /// </summary>
        public void Interrupt(ConductedSceneInterruption reason)
        {
            if (stage.Phase == AttackSpotlightPhase.None && stage.QueuedSceneCount == 0)
            {
                return;
            }

            var wasDeferring = DefersCues;
            stage.Interrupt(reason);
            RenderCurrent(snap: true);
            if (wasDeferring)
            {
                CuesReleased?.Invoke();
            }
        }

        /// <summary>
        /// One frame of the spotlight's own clock. Called by
        /// <see cref="AttackSpotlightDriver"/>, or directly by a test that wants to advance
        /// deterministically instead of waiting on frames.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            var wasDeferring = DefersCues;
            stage.Advance(deltaSeconds);

            // Only when something a surface could see has changed. A spotlight that is doing nothing
            // costs one comparison per frame, which is what keeps this off the Phase 4 frame budget.
            if (stage.Phase != lastRenderedPhase ||
                Math.Abs(stage.FramingWeight - lastRenderedWeight) >= 0.0005f)
            {
                RenderCurrent();
            }

            if (wasDeferring && !DefersCues)
            {
                CuesReleased?.Invoke();
            }
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (projection == null)
            {
                // A composition without a projection yet, or one that lost it. Either way nothing on
                // screen can still be true.
                Interrupt(ConductedSceneInterruption.Disconnected);
                lastProjection = null;
                return;
            }

            if (ReferenceEquals(projection, lastProjection))
            {
                RenderCurrent();
                return;
            }

            lastProjection = projection;
            Observe(projection);
            RenderCurrent(pendingSnap);
            pendingSnap = false;
        }

        /// <summary>
        /// The watermark and the row-to-scene translation. Separated from
        /// <see cref="Refresh"/> so a test can hand it exactly the rows it means.
        /// </summary>
        internal void Observe(IMatchReadModel projection)
        {
            var log = projection.MatchLog;
            if (log == null || log.Count == 0)
            {
                // A cleared log is a restore to a point before anything happened.
                if (observedSequence > 0)
                {
                    pendingSnap = true;
                    stage.Interrupt(ConductedSceneInterruption.ProjectionRewound);
                }

                observedSequence = 0;
                return;
            }

            var highest = 0;
            for (var index = 0; index < log.Count; index++)
            {
                if (log[index] != null && log[index].Sequence > highest)
                {
                    highest = log[index].Sequence;
                }
            }

            if (observedSequence == NeverObserved)
            {
                // First sight. Prime and show nothing: a client joining a match in progress, a scene
                // load, and a snapshot restore all hand over a log that is already full.
                observedSequence = highest;
                return;
            }

            if (highest < observedSequence)
            {
                // The log went backwards - an undo, a rewind, a restore. Whatever is on screen may
                // describe an attack that no longer happened, so it goes immediately.
                pendingSnap = true;
                stage.Interrupt(ConductedSceneInterruption.ProjectionRewound);
                observedSequence = highest;
                return;
            }

            if (highest == observedSequence)
            {
                return;
            }

            var arrived = new List<AttackSpotlightScene>();
            for (var index = 0; index < log.Count; index++)
            {
                var entry = log[index];
                if (entry == null ||
                    entry.Sequence <= observedSequence ||
                    entry.Kind != MatchLogEntryKind.Attack)
                {
                    continue;
                }

                var scene = Compose(projection, log, index, entry);
                if (scene != null)
                {
                    arrived.Add(scene);
                }
            }

            observedSequence = highest;
            if (arrived.Count > AttackSpotlightTiming.MaxQueuedScenes)
            {
                // A WINDOW, not a story. One authoritative step can legitimately resolve two attacks -
                // a repeated attack does - but a single projection change carrying more resolved
                // attacks than the queue can hold is a resync, a seat admission, or a snapshot
                // restore handing over a log that was already full. Showing them would be the
                // hundred-smacks-at-once failure the cue layer's rule 2 exists to stop, one screen at
                // a time and with a button between each. So this primes and shows none, which is that
                // same rule reaching the case a watermark alone does not cover: a client whose FIRST
                // sight was an empty log and whose second is a match in progress.
                skippedWindowCount++;
                return;
            }

            for (var index = 0; index < arrived.Count; index++)
            {
                stage.Offer(arrived[index]);
            }
        }

        /// <summary>
        /// One Attack row as a scene, with both faces captured from the projection as it stands now.
        /// </summary>
        private AttackSpotlightScene Compose(
            IMatchReadModel projection,
            IReadOnlyList<MatchLogEntry> log,
            int entryIndex,
            MatchLogEntry entry)
        {
            var attacker = KineticSubject.ForHero(entry.SubjectHeroId);
            var defender = entry.TargetHeroId.IsDefined
                ? KineticSubject.ForHero(entry.TargetHeroId)
                : KineticSubject.ForMinion(entry.SubjectMinionId);
            if (!attacker.IsDefined || !defender.IsDefined)
            {
                return null;
            }

            return new AttackSpotlightScene(
                entry.Sequence,
                attacker,
                defender,
                entry.Outcome,
                HeroName(projection, entry.SubjectHeroId),
                defender.Kind == KineticSubjectKind.Hero
                    ? HeroName(projection, entry.TargetHeroId)
                    : MinionName(projection, entry.SubjectMinionId),
                CaptureCard(projection, entry.SubjectCardInstanceId, resolving: true, turnIndex: entry.TurnIndex),
                CaptureCard(
                    projection,
                    FindDefenseCard(log, entryIndex, entry),
                    resolving: false,
                    turnIndex: 0),
                PrintsRangedAttack(projection, entry.SubjectCardInstanceId));
        }

        /// <summary>
        /// Whether the card the Attack row names prints a Ranged Attack. See
        /// <see cref="AttackSpotlightScene.IsRangedAttack"/> for why this is read from the definition
        /// rather than from the row, and why it does not decide how the smack is drawn.
        /// </summary>
        private static bool PrintsRangedAttack(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (!cardInstanceId.IsDefined)
            {
                return false;
            }

            var cards = projection.Cards;
            var definitions = projection.Definitions;
            if (cards == null || definitions == null)
            {
                return false;
            }

            CardDefinitionReference reference = default;
            for (var index = 0; index < cards.Count; index++)
            {
                if (cards[index] != null && cards[index].Id.Equals(cardInstanceId))
                {
                    reference = cards[index].DefinitionReference;
                    break;
                }
            }

            if (!reference.IsDefined)
            {
                return false;
            }

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || !definition.Reference.Equals(reference))
                {
                    continue;
                }

                var actions = definition.ActionDefinitions;
                if (actions == null)
                {
                    return false;
                }

                for (var action = 0; action < actions.Count; action++)
                {
                    if (actions[action] != null &&
                        actions[action].AttackRange == AttackRangeKind.Ranged)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// The card the defender spent answering THIS attack.
        ///
        /// <para>A match-log row carries no resolution id - the selection contract deliberately does
        /// not publish engine-internal identity - so the pairing is made from what the rows do say. A
        /// Defense row is written when the defense response COMPLETES, which is strictly before the
        /// Attack row this method is pairing (the attack cannot resolve until the answer is in), and
        /// its subject is the hero who ANSWERED. So: walk backwards from the attack, stop at the
        /// previous Attack row so a defense from an earlier attack in the same turn can never be
        /// borrowed, and take the Defense row whose subject is this attack's target.</para>
        /// </summary>
        private static CardInstanceId FindDefenseCard(
            IReadOnlyList<MatchLogEntry> log, int entryIndex, MatchLogEntry attack)
        {
            if (!attack.TargetHeroId.IsDefined)
            {
                // Nothing but a hero answers a defense, so an attack on a minion has no card to find.
                return default;
            }

            for (var index = entryIndex - 1; index >= 0; index--)
            {
                var candidate = log[index];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Kind == MatchLogEntryKind.Attack)
                {
                    return default;
                }

                if (candidate.Kind != MatchLogEntryKind.Defense ||
                    candidate.Outcome != MatchLogOutcome.DefenseCardDiscarded ||
                    !candidate.SubjectHeroId.Equals(attack.TargetHeroId))
                {
                    continue;
                }

                return candidate.SubjectCardInstanceId;
            }

            return default;
        }

        private static TrayCardViewModel CaptureCard(
            IMatchReadModel projection, CardInstanceId cardInstanceId, bool resolving, int turnIndex)
        {
            if (!cardInstanceId.IsDefined)
            {
                return null;
            }

            var cards = projection.Cards;
            if (cards == null)
            {
                return null;
            }

            CardInstance instance = null;
            for (var index = 0; index < cards.Count; index++)
            {
                if (cards[index] != null && cards[index].Id.Equals(cardInstanceId))
                {
                    instance = cards[index];
                    break;
                }
            }

            if (instance == null)
            {
                // The log named a card this viewer's projection does not carry. It is an ordinary
                // answer rather than a fault: the spotlight shows the other face, or none, and still
                // frames and smacks.
                return null;
            }

            return resolving
                ? CurrentRoundCards.TrayCard(
                    projection,
                    instance,
                    TrayCardGroup.Played,
                    Math.Max(1, turnIndex),
                    isSelectable: false,
                    isSelected: false,
                    isResolving: true)
                : CurrentRoundCards.TrayCard(
                    projection,
                    instance,
                    TrayCardGroup.Discarded,
                    0,
                    isSelectable: false,
                    isSelected: false);
        }

        private static string HeroName(IMatchReadModel projection, HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                return string.Empty;
            }

            var heroes = projection.Setup?.Heroes;
            if (heroes == null)
            {
                return heroId.Value;
            }

            for (var index = 0; index < heroes.Count; index++)
            {
                if (heroes[index].HeroId.Equals(heroId))
                {
                    return heroes[index].DisplayName ?? heroId.Value;
                }
            }

            return heroId.Value;
        }

        private static string MinionName(
            IMatchReadModel projection, GuardsDigital.Core.Setup.MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                return string.Empty;
            }

            var minions = projection.Minions;
            if (minions == null)
            {
                return minionId.Value;
            }

            for (var index = 0; index < minions.Count; index++)
            {
                if (minions[index] != null && minions[index].MinionId.Equals(minionId))
                {
                    return minions[index].MinionType + " MINION";
                }
            }

            return minionId.Value;
        }

        private void RenderCurrent(bool snap = false)
        {
            lastRenderedPhase = stage.Phase;
            lastRenderedWeight = stage.FramingWeight;
            view.Render(new AttackSpotlightViewModel(
                stage.Phase,
                stage.Current,
                stage.FramingWeight,
                stage.IsContinueArmed,
                stage.QueuedSceneCount,
                snap));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35y. What a surface should play right now, and what should currently be
    /// breathing.
    ///
    /// <para>The two halves are different in kind and deliberately kept apart. <see cref="Cues"/> is
    /// TRANSIENT - the kinetics that arrived with this projection and nothing else, so a view that
    /// renders the same model twice does not play the same smack twice. The highlight fields are
    /// STANDING state - what is awaited right now - which is what the amendment's "only elements
    /// currently in a waiting/awaited state animate" asks for.</para>
    /// </summary>
    public sealed class KineticCueViewModel
    {
        public static readonly KineticCueViewModel Empty = new KineticCueViewModel(
            Array.Empty<KineticCue>(), false, Array.Empty<HeroId>());

        public KineticCueViewModel(
            IEnumerable<KineticCue> cues,
            bool pulseBoardMarks,
            IEnumerable<HeroId> sweepingShineHeroes,
            bool snapToAuthoritative = false)
        {
            SnapToAuthoritative = snapToAuthoritative;
            Cues = new ReadOnlyCollection<KineticCue>(
                System.Linq.Enumerable.ToArray(cues ?? throw new ArgumentNullException(nameof(cues))));
            Scenes = Array.Empty<KineticScene>();
            PulseBoardMarks = pulseBoardMarks;
            SweepingShineHeroes = new ReadOnlyCollection<HeroId>(
                System.Linq.Enumerable.ToArray(
                    sweepingShineHeroes ?? throw new ArgumentNullException(nameof(sweepingShineHeroes))));
        }

        /// <summary>
        /// Milestone 8.35z11. The same model, expressed as the STAGED scenes the conductor plays
        /// rather than as a flat pile of cues that all start at once.
        /// </summary>
        /// <remarks>
        /// <see cref="Cues"/> is kept, flattened from the scenes, and is still exactly "the cues that
        /// arrived with this projection". A reader that only wants to know what happened is unchanged;
        /// a reader that wants to know in what ORDER and with what beats reads <see cref="Scenes"/>.
        /// </remarks>
        public KineticCueViewModel(
            IReadOnlyList<KineticScene> scenes,
            bool pulseBoardMarks,
            IEnumerable<HeroId> sweepingShineHeroes,
            bool snapToAuthoritative = false,
            HeroId viewerHeroId = default)
        {
            ViewerHeroId = viewerHeroId;
            SnapToAuthoritative = snapToAuthoritative;
            var staged = scenes ?? Array.Empty<KineticScene>();
            Scenes = staged is ReadOnlyCollection<KineticScene> already
                ? already
                : new ReadOnlyCollection<KineticScene>(System.Linq.Enumerable.ToArray(staged));
            Cues = KineticScene.Flatten(Scenes);
            PulseBoardMarks = pulseBoardMarks;
            SweepingShineHeroes = new ReadOnlyCollection<HeroId>(
                System.Linq.Enumerable.ToArray(
                    sweepingShineHeroes ?? throw new ArgumentNullException(nameof(sweepingShineHeroes))));
        }

        /// <summary>The kinetics that arrived with this projection, oldest first. Never replayed.</summary>
        public IReadOnlyList<KineticCue> Cues { get; }

        /// <summary>
        /// Milestone 8.35z11. The same kinetics grouped into the moments they belong to and in the
        /// order they should be staged - what the conductor queues.
        /// </summary>
        public IReadOnlyList<KineticScene> Scenes { get; }

        /// <summary>
        /// Milestone 8.35z11. Whose seat this HUD belongs to, or undefined for a spectator or the
        /// Director.
        ///
        /// <para>A HUD draws exactly one hand - the viewer's - so a committed card can only be shown
        /// leaving a hand when it is that hand. Every other hero's commitment sets out from their own
        /// rail instead, which is the honest picture: from here, that is all a player can see happen.</para>
        /// </summary>
        public HeroId ViewerHeroId { get; }

        /// <summary>
        /// Whether the board's own marks - the acting-hero ring and the awaited hex-corner cues -
        /// should breathe.
        ///
        /// <para>Read from <see cref="AwaitedInputQuery.IsWaitingOnViewer"/>, which is precisely "this
        /// viewer has something to answer". The marks a viewer must act on are the marks that move;
        /// a spectator's board, and a board mid-resolution, stay still. That is both the amendment's
        /// animate-only-what-is-awaited rule and the whole of the repaint bound on the board side -
        /// one ring and one hex's corners, or nothing at all.</para>
        /// </summary>
        public bool PulseBoardMarks { get; }

        /// <summary>
        /// The heroes whose gold tray and card shine should carry the slow circling light.
        ///
        /// <para>Read per hero from <see cref="AwaitedInputQuery.IsHeroAwaited"/> - the same predicate
        /// milestone 8.35m's tray shine already lights a tray with, so the sweep travels exactly the
        /// shine that is already there rather than lighting a second, differently-chosen set. Bounded
        /// by the roster: at most six trays, and in practice one or two.</para>
        /// </summary>
        public IReadOnlyList<HeroId> SweepingShineHeroes { get; }

        /// <summary>
        /// Whether the surface must drop every kinetic it is playing and show authoritative state
        /// directly, with nothing carried over.
        ///
        /// <para>Set when the presenter primed or re-primed its watermark: the first projection of a
        /// match or of a joining client, and a log that went BACKWARDS - an undo, a rewind, a restore.
        /// The piece kinetics would end at the next projection change anyway; this is what additionally
        /// clears the floating figures, which deliberately outlive an ordinary change. A number
        /// reporting damage from an event that has been undone is the one thing a flourish must never
        /// do.</para>
        /// </summary>
        public bool SnapToAuthoritative { get; }

        public bool HasAnimation =>
            Cues.Count > 0 || PulseBoardMarks || SweepingShineHeroes.Count > 0;
    }

    public interface IKineticCueView
    {
        void Render(KineticCueViewModel model);
    }

    /// <summary>
    /// Milestone 8.35y. Turns the seat-public match log into kinetic cues - the presenter half
    /// of the user's "tokens smacking each other".
    ///
    /// <para><b>Why the match log and nothing else.</b> <see cref="IMatchReadModel.MatchLog"/> is the
    /// one event stream Presentation can see, it is derived rather than stored, and it is IDENTICAL on
    /// every client by construction - milestone 8.35r's selection contract admits only events the
    /// whole table can already see, which is why its seat filter is an identity rather than a
    /// redaction. Deriving kinetics from it is therefore what makes the amendment's "identical on
    /// every client" true, rather than a property to be tested for afterwards.</para>
    ///
    /// <para><b>Three rules make a kinetic layer safe to bolt onto an authoritative surface,</b> and
    /// each is enforced here rather than left to a view:</para>
    /// <list type="number">
    /// <item>A cue is emitted ONCE, keyed on the log row's own sequence. Re-rendering a model, a
    /// resync redelivering a window, or a second refresh on the same projection cannot replay it.</item>
    /// <item>The FIRST projection a presenter ever sees primes the watermark and emits nothing. A
    /// client joining a match in progress, a scene load, and a snapshot restore all hand over a log
    /// that is already full; playing it would be a hundred smacks at once and a false story.</item>
    /// <item>A log that went BACKWARDS - an undo, a rewind, a restore to an earlier point - re-primes
    /// the watermark and emits nothing. The board snaps to the authoritative state, which is exactly
    /// the amendment's interruption rule, and is why kinetics can never accumulate into a second
    /// account of what happened.</item>
    /// </list>
    /// </summary>
    public sealed class KineticCuePresenter : ISeatScopedPresenter
    {
        private readonly IMatchClient session;
        private readonly IKineticCueView view;
        // Milestone 8.35x: NOT readonly, for the reason `ISeatScopedPresenter` states - a networked
        // client is composed before its seat exists, so this is bound a second time at admission.
        private HeroId viewerHeroId;
        private readonly KineticCueStream stream = new KineticCueStream();

        // Milestone 8.35z11. The second derivation, for the journeys the seat-public log
        // deliberately does not describe. See KineticProjectionWatch for the finding that forced it.
        private readonly KineticProjectionWatch watch = new KineticProjectionWatch();
        private IMatchReadModel lastProjection;
        private KineticCueViewModel lastViewModel = KineticCueViewModel.Empty;
        // Milestone 8.35z12: null on a composition with no spotlight, which is exactly the
        // behaviour this presenter had before one existed.
        private Func<bool> deferral;

        public KineticCuePresenter(IMatchClient session, IKineticCueView view, HeroId viewerHeroId = default)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.viewerHeroId = viewerHeroId;
        }

        /// <inheritdoc />
        public HeroId HomeHeroId => viewerHeroId;

        /// <summary>
        /// Milestone 8.35x. Binds the viewer identity the board-mark pulse is branched on once the
        /// lobby admits the seat. Until this existed a networked client pulsed its board marks on
        /// the Director's "everything is mine" branch for the whole session.
        /// </summary>
        /// <remarks>
        /// Dropping <c>lastProjection</c> here cannot replay kinetics. The stream emits each cue
        /// once, keyed on the match log's own sequence (rule 1 in this class's own remarks), so
        /// re-observing an already-observed log yields nothing; what the drop actually buys is the
        /// standing pulse half being recomputed under the new identity instead of being served back
        /// from a model composed under the old one.
        /// </remarks>
        public bool SetHomeHero(HeroId heroId)
        {
            var projection = session.Projection;
            if (!heroId.IsDefined || projection == null ||
                !projection.Setup.Heroes.Any(hero => hero.HeroId.Equals(heroId)))
            {
                return false;
            }

            viewerHeroId = heroId;
            lastProjection = null;
            Refresh();
            return true;
        }

        /// <summary>The highest match-log sequence this presenter has already turned into cues.</summary>
        public int ObservedSequence => stream.ObservedSequence;

        /// <summary>
        /// Milestone 8.35z12. Asked before every observation: while it answers true, this
        /// presenter holds its cues instead of playing them, and - crucially - does NOT advance its
        /// watermark or consume the projection, so nothing is lost while it waits.
        ///
        /// <para><b>Why the presenter and not the stream.</b> The three rules that make a kinetic layer
        /// safe all live in <see cref="KineticCueStream"/> and are about the LOG. Pacing is not about
        /// the log at all - it is about what this viewer is looking at - so it belongs on the side that
        /// knows about a viewer. A composition that sets no deferral behaves exactly as this presenter
        /// did before the spotlight existed.</para>
        ///
        /// <para>Set it with <see cref="DeferWhile"/>, and call <see cref="Resume"/> when the
        /// predicate goes false so the held rows drain on that frame rather than on the next
        /// authoritative change - which might be a long time coming, or never.</para>
        /// </summary>
        public bool IsDeferred => deferral != null && deferral();

        /// <summary>
        /// Holds this presenter's cues while <paramref name="predicate"/> answers true. Passing
        /// <c>null</c> removes the deferral. See <see cref="IsDeferred"/>.
        /// </summary>
        public void DeferWhile(Func<bool> predicate)
        {
            deferral = predicate;
        }

        /// <summary>
        /// Drains whatever the deferral held. It is an ordinary refresh: the stream's own emit-once
        /// watermark decides what has not been played yet, so calling this when nothing was held
        /// costs one comparison and plays nothing.
        /// </summary>
        public void Resume()
        {
            if (IsDeferred)
            {
                return;
            }

            Refresh();
        }

        public void Refresh()
        {
            var projection = session.Projection;
            if (IsDeferred)
            {
                // Held. The standing highlight half is handed over again so a waiting tray keeps
                // breathing, and the transient half is empty - but `lastProjection` is deliberately
                // NOT advanced, so the rows that arrived while this was held are still unobserved and
                // play in full on the next resume.
                lastViewModel = new KineticCueViewModel(
                    Array.Empty<KineticCue>(),
                    lastViewModel.PulseBoardMarks,
                    lastViewModel.SweepingShineHeroes);
                view.Render(lastViewModel);
                return;
            }

            if (ReferenceEquals(projection, lastProjection))
            {
                // The same projection cannot produce a second round of kinetics. It can still be
                // re-rendered - a view may have been rebuilt - so the standing highlight half is
                // handed over again, with the transient half emptied.
                lastViewModel = new KineticCueViewModel(
                    Array.Empty<KineticCue>(),
                    lastViewModel.PulseBoardMarks,
                    lastViewModel.SweepingShineHeroes);
                view.Render(lastViewModel);
                return;
            }

            lastProjection = projection;
            var scenes = projection == null
                ? Array.Empty<KineticScene>()
                : Stage(projection);

            lastViewModel = new KineticCueViewModel(
                scenes,
                projection != null && AwaitedInputQuery.IsWaitingOnViewer(projection, viewerHeroId),
                projection == null ? Array.Empty<HeroId>() : AwaitedHeroes(projection),
                stream.PrimedOnLastObservation,
                viewerHeroId);
            view.Render(lastViewModel);
        }

        /// <summary>
        /// Milestone 8.35z11. One projection change's scenes, from BOTH derivations.
        ///
        /// <para>The log's scenes come first because they describe things that have already been
        /// recorded; the projection watch's come after, ordered at the log's own watermark, because a
        /// commitment, a coin and a retrieval observed alongside a batch of rows happened at or after
        /// the last of them. The watch is handed the stream's own priming signal, so a rewind silences
        /// both derivations in the same breath - the one way a queued flourish could otherwise
        /// describe an event that no longer happened.</para>
        /// </summary>
        private IReadOnlyList<KineticScene> Stage(IMatchReadModel projection)
        {
            var fromLog = stream.ObserveScenes(projection.MatchLog, projection);
            var fromProjection = watch.Observe(
                projection, stream.ObservedSequence, stream.PrimedOnLastObservation);

            if (fromProjection.Count == 0)
            {
                return fromLog;
            }

            if (fromLog.Count == 0)
            {
                return fromProjection;
            }

            var combined = new List<KineticScene>(fromLog.Count + fromProjection.Count);
            combined.AddRange(fromLog);
            combined.AddRange(fromProjection);
            return new ReadOnlyCollection<KineticScene>(combined.ToArray());
        }

        private IReadOnlyList<HeroId> AwaitedHeroes(IMatchReadModel projection)
        {
            var heroes = projection.Heroes;
            if (heroes == null || heroes.Count == 0)
            {
                return Array.Empty<HeroId>();
            }

            var awaited = new List<HeroId>();
            for (var index = 0; index < heroes.Count; index++)
            {
                var heroId = heroes[index].HeroId;
                if (heroId.IsDefined && AwaitedInputQuery.IsHeroAwaited(projection, heroId))
                {
                    awaited.Add(heroId);
                }
            }

            return awaited.Count == 0
                ? Array.Empty<HeroId>()
                : new ReadOnlyCollection<HeroId>(awaited.ToArray());
        }
    }

    /// <summary>
    /// Milestone 8.35y. The watermark and the row-to-cue translation, with no session and no
    /// view attached to them.
    ///
    /// <para>It is a separate type from <see cref="KineticCuePresenter"/> for one reason worth stating:
    /// this is where every rule that could get a kinetic layer WRONG lives - emit once, prime on first
    /// sight, re-prime on a rewind, drop a burst rather than queue it - and a test of those rules
    /// should not have to stand up an <see cref="IMatchReadModel"/> with its sixty-odd members to ask
    /// the question. <see cref="Observe"/> takes the log directly, so a case builds the exact rows it
    /// means and reads the exact cues they produce. The projection is passed only for the card facts a
    /// floating number needs, and is optional: with none, cues still carry their full shape and their
    /// numbers are simply absent.</para>
    /// </summary>
    public sealed class KineticCueStream
    {
        /// <summary>The watermark's value before any log has been observed.</summary>
        private const int NeverObserved = -1;

        private int observedSequence = NeverObserved;

        /// <summary>The highest match-log sequence already turned into cues.</summary>
        public int ObservedSequence => observedSequence;

        /// <summary>
        /// Whether the most recent <see cref="Observe"/> primed or re-primed the watermark rather than
        /// emitting cues - a first sight of a match, or a log that went backwards.
        /// </summary>
        public bool PrimedOnLastObservation { get; private set; }

        /// <summary>
        /// The cues one observation of the log produces, oldest first. Never the same row twice.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z11 made <see cref="ObserveScenes"/> the real translation and left this
        /// as its flattening, so every caller and case written against the flat shape is unchanged and
        /// there is still exactly one implementation of the emit-once, prime-on-first-sight and
        /// re-prime-on-a-rewind rules.
        /// </remarks>
        public IReadOnlyList<KineticCue> Observe(
            IReadOnlyList<MatchLogEntry> log, IMatchReadModel projection = null) =>
            KineticScene.Flatten(ObserveScenes(log, projection));

        /// <summary>
        /// Milestone 8.35z11. The same observation, grouped into the MOMENTS the conductor stages
        /// and ordered the way a player should watch them.
        ///
        /// <para><b>Two rows become one scene when they are one moment</b> - an attack's lunge and the
        /// flash it causes - and one row becomes several scenes when it is several moments. Everything
        /// else about the stream is untouched.</para>
        /// </summary>
        public IReadOnlyList<KineticScene> ObserveScenes(
            IReadOnlyList<MatchLogEntry> log, IMatchReadModel projection = null)
        {
            PrimedOnLastObservation = false;
            if (log == null || log.Count == 0)
            {
                // A cleared log is a restore to a point before anything happened; re-prime rather than
                // hold a watermark no future row can exceed.
                PrimedOnLastObservation = observedSequence != 0 && observedSequence != NeverObserved;
                observedSequence = 0;
                return Array.Empty<KineticScene>();
            }

            var highest = 0;
            for (var index = 0; index < log.Count; index++)
            {
                if (log[index] != null && log[index].Sequence > highest)
                {
                    highest = log[index].Sequence;
                }
            }

            if (observedSequence == NeverObserved || highest < observedSequence)
            {
                // Rules 2 and 3: the first sight of a match, and a log that went backwards. Both prime
                // the watermark and play nothing, so the surface shows authoritative state directly.
                PrimedOnLastObservation = true;
                observedSequence = highest;
                return Array.Empty<KineticScene>();
            }

            if (highest == observedSequence)
            {
                return Array.Empty<KineticScene>();
            }

            var scenes = new List<KineticScene>();
            for (var index = 0; index < log.Count; index++)
            {
                var entry = log[index];
                if (entry == null || entry.Sequence <= observedSequence)
                {
                    continue;
                }

                Append(scenes, entry, projection);
            }

            observedSequence = highest;

            // Milestone 8.35z11. THE BURST NO LONGER TRUNCATES HERE, and that is the conductor's
            // arrival rather than a rule being dropped. Milestone 8.35y kept only the newest
            // KineticTiming.MaxConcurrentCues because every surviving cue started at the SAME INSTANT,
            // so a window of thirty was thirty simultaneous animations. Under a conductor they are
            // thirty queued moments, played one at a time and compressed when the viewer falls behind,
            // and the bound that matters moved with them: KineticStaging.MaxQueuedScenes. What
            // MaxConcurrentCues still names - the most kinetics playing at once - is now guaranteed
            // structurally by the size of a scene, and asserted as such.
            RankRevealsByInitiative(scenes, cardId => InitiativeOf(cardId, projection));
            return scenes.Count == 0
                ? Array.Empty<KineticScene>()
                : new ReadOnlyCollection<KineticScene>(scenes.ToArray());
        }

        /// <summary>
        /// Milestone 8.35z11 (principle 4: STAGED REVEALS IN INITIATIVE ORDER). Re-ranks a batch
        /// of reveal scenes so they are staged by the Initiative their cards print, highest first.
        ///
        /// <para><b>Why this is real work and not a no-op.</b> The engine reveals in its own roster
        /// order: <c>TurnCardCommitmentAuthority.CompleteRevealIfReady</c> walks <c>expectedHeroIds</c>
        /// and emits one <c>CardRevealed</c> per hero in that order, and <c>RoundTurnSlot</c> builds
        /// <c>expectedHeroIds</c> by ORDINAL HERO-ID SORT. So the log's own order for a reveal batch is
        /// alphabetical by hero identifier - a stable order, and a meaningless one to watch. Staging by
        /// sequence alone would have satisfied the letter of "staged" and none of the point.</para>
        ///
        /// <para><b>It decides nothing.</b> Highest printed Initiative first is the direction
        /// <c>InitiativeResolutionAuthority</c> reads (it takes <c>candidates.Max(InitiativeTotal)</c>),
        /// but this reads the PRINTED number while the authority reads a total that item bonuses and
        /// standing modifications can raise, and ties there are settled by a player's own choice. This
        /// is the order the cards are TURNED OVER in, which is presentation; who acts first stays the
        /// authority's answer and is never taken from here. A card whose Initiative cannot be read at
        /// all - p.16's Ultimate prints none - sorts last and keeps its arrival order.</para>
        /// </summary>
        /// <remarks>
        /// It takes the ranking as a FUNCTION rather than a projection for the reason the rest of this
        /// file already follows: the interesting rules here are the sort's stability and the span it
        /// stays inside, and a case about those should not have to stand up an
        /// <see cref="IMatchReadModel"/> with its sixty-odd members and a real card catalogue to pose
        /// its question. <see cref="InitiativeOf(CardInstanceId, IMatchReadModel)"/> is the production
        /// ranking, tested separately for the one thing that can go wrong in it - a card it cannot
        /// read.
        /// </remarks>
        internal static void RankRevealsByInitiative(
            List<KineticScene> scenes, Func<CardInstanceId, int> initiativeOf)
        {
            var reveals = new List<int>();
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].Kind == KineticSceneKind.Reveal)
                {
                    reveals.Add(index);
                }
            }

            if (reveals.Count < 2)
            {
                return;
            }

            var lowestOrder = int.MaxValue;
            for (var index = 0; index < reveals.Count; index++)
            {
                var order = scenes[reveals[index]].Order;
                if (order < lowestOrder)
                {
                    lowestOrder = order;
                }
            }

            // A stable sort by descending printed Initiative, arrival order breaking every tie, so two
            // clients reading the same log stage the same reveals in the same order.
            var ranked = new List<int>(reveals);
            for (var index = 1; index < ranked.Count; index++)
            {
                var item = ranked[index];
                var value = initiativeOf(CardOf(scenes[item]));
                var scan = index - 1;
                while (scan >= 0 && initiativeOf(CardOf(scenes[ranked[scan]])) < value)
                {
                    ranked[scan + 1] = ranked[scan];
                    scan--;
                }

                ranked[scan + 1] = item;
            }

            for (var rank = 0; rank < ranked.Count; rank++)
            {
                var original = scenes[ranked[rank]];
                scenes[ranked[rank]] = new KineticScene(
                    lowestOrder + rank,
                    original.Kind,
                    original.Cues,
                    original.StaggerAfterSeconds,
                    original.HoldsForAcknowledgement,
                    original.Label);
            }
        }

        /// <summary>
        /// The printed Initiative of the card a reveal scene turns over, or <see cref="int.MinValue"/>
        /// when it cannot be read - an unresolvable definition, or p.16's Ultimate, which prints none.
        /// </summary>
        private static CardInstanceId CardOf(KineticScene scene) =>
            scene.Cues.Count > 0 ? scene.Cues[0].CardInstanceId : default;

        internal static int InitiativeOf(CardInstanceId cardId, IMatchReadModel projection)
        {
            if (!cardId.IsDefined || projection?.Cards == null || projection.Definitions == null)
            {
                return int.MinValue;
            }

            try
            {
                for (var index = 0; index < projection.Cards.Count; index++)
                {
                    var card = projection.Cards[index];
                    if (card == null || !card.Id.Equals(cardId))
                    {
                        continue;
                    }

                    return PrintedValueQuery
                        .CalculatePrintedInitiative(card, projection.Definitions)
                        .Total;
                }
            }
            catch (Exception)
            {
                // A card the catalog cannot resolve costs this scene its rank and nothing else. The
                // reveal still plays, in arrival order, which is the same degradation milestone 8.35y
                // chose when a figure's card could not be resolved.
                return int.MinValue;
            }

            return int.MinValue;
        }

        /// <summary>
        /// The one-row-to-cues translation. Every arm is a row of milestone 8.35r's own selection
        /// contract; a kind with no kinetic meaning writes nothing, which is why the log's other
        /// twenty-odd kinds pass through here silently rather than needing a case each.
        /// </summary>
        private static void Append(
            List<KineticScene> scenes, MatchLogEntry entry, IMatchReadModel projection)
        {
            switch (entry.Kind)
            {
                // ---- Milestone 8.35z11. The card and progression journeys.

                case MatchLogEntryKind.CardRevealed:
                {
                    // The one commitment event the selection contract admits, and it admits it in
                    // "only ever the revealed form" - which is exactly what a flip needs and exactly
                    // why the FACEDOWN half of the journey had to be derived elsewhere. See
                    // KineticProjectionWatch for that finding.
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (!subject.IsDefined || !entry.SubjectCardInstanceId.IsDefined)
                    {
                        return;
                    }

                    scenes.Add(new KineticScene(
                        entry.Sequence,
                        KineticSceneKind.Reveal,
                        new[]
                        {
                            new KineticCue(
                                entry.Sequence,
                                KineticCueKind.CardReveal,
                                subject,
                                cardInstanceId: entry.SubjectCardInstanceId)
                        },
                        label: "reveal:" + entry.SubjectHeroId));
                    return;
                }

                case MatchLogEntryKind.LevelPurchased:
                {
                    // Milestone 8.35u levels every hero AT ONCE, so this row arrives in batches of up
                    // to six. Six simultaneous pops was precisely the "simultaneous soup" principle 3
                    // names; one scene each is what stages them.
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (!subject.IsDefined || entry.Amount <= 0)
                    {
                        return;
                    }

                    scenes.Add(new KineticScene(
                        entry.Sequence,
                        KineticSceneKind.Progression,
                        new[]
                        {
                            new KineticCue(
                                entry.Sequence,
                                KineticCueKind.LevelPop,
                                subject,
                                amount: entry.Amount)
                        },
                        label: "level:" + entry.SubjectHeroId));
                    return;
                }

                case MatchLogEntryKind.Move:
                case MatchLogEntryKind.FastTravel:
                {
                    // A Fast Travel is not board movement by the rules, and it slides here anyway: the
                    // statement a slide makes is "this piece left there and arrived here", which is
                    // true of both, and a player who cannot see the piece leave is exactly the player
                    // the amendment was written for. The rules distinction is not this layer's to
                    // draw, and nothing downstream reads a cue as a rules fact.
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (!subject.IsDefined || !entry.HasOrigin || !entry.HasDestination ||
                        entry.Origin.Equals(entry.Destination))
                    {
                        return;
                    }

                    scenes.Add(new KineticScene(
                        entry.Sequence,
                        KineticSceneKind.Board,
                        new[]
                        {
                            new KineticCue(
                                entry.Sequence,
                                KineticCueKind.Slide,
                                subject,
                                route: new[] { entry.Origin, entry.Destination })
                        },
                        label: "slide:" + entry.SubjectHeroId));
                    return;
                }

                case MatchLogEntryKind.Attack:
                {
                    var attacker = KineticSubject.ForHero(entry.SubjectHeroId);
                    var defender = entry.TargetHeroId.IsDefined
                        ? KineticSubject.ForHero(entry.TargetHeroId)
                        : KineticSubject.ForMinion(entry.SubjectMinionId);
                    if (!attacker.IsDefined || !defender.IsDefined)
                    {
                        return;
                    }

                    // Milestone 8.35z9 retracted the floating figures on the user's own call after
                    // live testing ("they just don't work"), so the lunge now carries motion alone.
                    // `landed` still decides whether the defender flashes, which is a separate claim.
                    var landed = entry.Outcome == MatchLogOutcome.AttackSuccessful;

                    // Milestone 8.35z11: ONE SCENE, both cues. A lunge and the flash it causes
                    // are one moment; staging them apart would tell the player two things happened.
                    var strike = new List<KineticCue>
                    {
                        new KineticCue(
                            entry.Sequence,
                            KineticCueKind.Lunge,
                            attacker,
                            defender)
                    };

                    if (landed)
                    {
                        strike.Add(new KineticCue(
                            entry.Sequence, KineticCueKind.Flash, defender));
                    }

                    scenes.Add(new KineticScene(
                        entry.Sequence,
                        KineticSceneKind.Board,
                        strike,
                        label: "attack:" + entry.SubjectHeroId));
                    return;
                }

                case MatchLogEntryKind.Defense:
                {
                    // The Defense row's subject is the hero who ANSWERED - see MatchLogProjection's own
                    // note on why the defender is the subject there and the attacker the target.
                    var defender = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (!defender.IsDefined || entry.Outcome != MatchLogOutcome.DefenseCardDiscarded)
                    {
                        return;
                    }

                    scenes.Add(new KineticScene(
                        entry.Sequence,
                        KineticSceneKind.Board,
                        new[] { new KineticCue(entry.Sequence, KineticCueKind.Flash, defender) },
                        label: "defense:" + entry.SubjectHeroId));
                    return;
                }

                case MatchLogEntryKind.HeroRemoved:
                {
                    // The REMOVAL rather than the defeat: the shrink-and-fade has to cover the moment
                    // the piece leaves the board, and the defeat row is recorded before anything has
                    // been taken off it.
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (subject.IsDefined)
                    {
                        scenes.Add(new KineticScene(
                            entry.Sequence,
                            KineticSceneKind.Board,
                            new[] { new KineticCue(entry.Sequence, KineticCueKind.Defeat, subject) },
                            label: "defeat:" + entry.SubjectHeroId));
                    }

                    return;
                }

                case MatchLogEntryKind.MinionRemoved:
                case MatchLogEntryKind.MinionBattleMinionRemoved:
                {
                    var subject = KineticSubject.ForMinion(entry.SubjectMinionId);
                    if (subject.IsDefined)
                    {
                        scenes.Add(new KineticScene(
                            entry.Sequence,
                            KineticSceneKind.Board,
                            new[] { new KineticCue(entry.Sequence, KineticCueKind.Defeat, subject) },
                            label: "defeat:" + entry.SubjectMinionId));
                    }

                    return;
                }

                case MatchLogEntryKind.HeroRespawned:
                {
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (subject.IsDefined)
                    {
                        scenes.Add(new KineticScene(
                            entry.Sequence,
                            KineticSceneKind.Board,
                            new[] { new KineticCue(entry.Sequence, KineticCueKind.Respawn, subject) },
                            label: "respawn:" + entry.SubjectHeroId));
                    }

                    return;
                }

                default:
                    return;
            }
        }
    }
}

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
            PulseBoardMarks = pulseBoardMarks;
            SweepingShineHeroes = new ReadOnlyCollection<HeroId>(
                System.Linq.Enumerable.ToArray(
                    sweepingShineHeroes ?? throw new ArgumentNullException(nameof(sweepingShineHeroes))));
        }

        /// <summary>The kinetics that arrived with this projection, oldest first. Never replayed.</summary>
        public IReadOnlyList<KineticCue> Cues { get; }

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
        private IMatchReadModel lastProjection;
        private KineticCueViewModel lastViewModel = KineticCueViewModel.Empty;

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

        public void Refresh()
        {
            var projection = session.Projection;
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
            var cues = projection == null
                ? Array.Empty<KineticCue>()
                : stream.Observe(projection.MatchLog, projection);

            lastViewModel = new KineticCueViewModel(
                cues,
                projection != null && AwaitedInputQuery.IsWaitingOnViewer(projection, viewerHeroId),
                projection == null ? Array.Empty<HeroId>() : AwaitedHeroes(projection),
                stream.PrimedOnLastObservation);
            view.Render(lastViewModel);
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
        public IReadOnlyList<KineticCue> Observe(
            IReadOnlyList<MatchLogEntry> log, IMatchReadModel projection = null)
        {
            PrimedOnLastObservation = false;
            if (log == null || log.Count == 0)
            {
                // A cleared log is a restore to a point before anything happened; re-prime rather than
                // hold a watermark no future row can exceed.
                PrimedOnLastObservation = observedSequence != 0 && observedSequence != NeverObserved;
                observedSequence = 0;
                return Array.Empty<KineticCue>();
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
                return Array.Empty<KineticCue>();
            }

            if (highest == observedSequence)
            {
                return Array.Empty<KineticCue>();
            }

            var cues = new List<KineticCue>();
            for (var index = 0; index < log.Count; index++)
            {
                var entry = log[index];
                if (entry == null || entry.Sequence <= observedSequence)
                {
                    continue;
                }

                Append(cues, entry, projection);
            }

            observedSequence = highest;
            if (cues.Count <= KineticTiming.MaxConcurrentCues)
            {
                return new ReadOnlyCollection<KineticCue>(cues.ToArray());
            }

            // The newest win. See KineticTiming.MaxConcurrentCues for why a burst is dropped rather
            // than queued: the end state is projection-true either way.
            return new ReadOnlyCollection<KineticCue>(
                cues.GetRange(cues.Count - KineticTiming.MaxConcurrentCues, KineticTiming.MaxConcurrentCues)
                    .ToArray());
        }

        /// <summary>
        /// The one-row-to-cues translation. Every arm is a row of milestone 8.35r's own selection
        /// contract; a kind with no kinetic meaning writes nothing, which is why the log's other
        /// twenty-odd kinds pass through here silently rather than needing a case each.
        /// </summary>
        private static void Append(
            List<KineticCue> cues, MatchLogEntry entry, IMatchReadModel projection)
        {
            switch (entry.Kind)
            {
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

                    cues.Add(new KineticCue(
                        entry.Sequence,
                        KineticCueKind.Slide,
                        subject,
                        route: new[] { entry.Origin, entry.Destination }));
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

                    // The number rides the lunge only when the attack actually landed. A defended
                    // attack's figure is the BLOCK, and it belongs to the defender's own row below -
                    // so a blocked hit never shows a damage number that was not dealt.
                    var landed = entry.Outcome == MatchLogOutcome.AttackSuccessful;
                    cues.Add(new KineticCue(
                        entry.Sequence,
                        KineticCueKind.Lunge,
                        attacker,
                        defender,
                        number: landed
                            ? KineticCardFacts.PrintedNumber(
                                projection,
                                entry.SubjectCardInstanceId,
                                PrintedValueChannel.Attack,
                                KineticNumberRole.Damage)
                            : KineticNumber.None));

                    if (landed)
                    {
                        cues.Add(new KineticCue(
                            entry.Sequence, KineticCueKind.Flash, defender));
                    }

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

                    cues.Add(new KineticCue(
                        entry.Sequence,
                        KineticCueKind.Flash,
                        defender,
                        number: KineticCardFacts.PrintedNumber(
                            projection,
                            entry.SubjectCardInstanceId,
                            PrintedValueChannel.Defense,
                            KineticNumberRole.Block)));
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
                        cues.Add(new KineticCue(entry.Sequence, KineticCueKind.Defeat, subject));
                    }

                    return;
                }

                case MatchLogEntryKind.MinionRemoved:
                case MatchLogEntryKind.MinionBattleMinionRemoved:
                {
                    var subject = KineticSubject.ForMinion(entry.SubjectMinionId);
                    if (subject.IsDefined)
                    {
                        cues.Add(new KineticCue(entry.Sequence, KineticCueKind.Defeat, subject));
                    }

                    return;
                }

                case MatchLogEntryKind.HeroRespawned:
                {
                    var subject = KineticSubject.ForHero(entry.SubjectHeroId);
                    if (subject.IsDefined)
                    {
                        cues.Add(new KineticCue(entry.Sequence, KineticCueKind.Respawn, subject));
                    }

                    return;
                }

                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Milestone 8.35y. Reads the two printed facts a floating number needs off the definition
    /// catalog: the value, and the colour it is tinted with.
    ///
    /// <para>Both are read from the same place for the same reason - the user's direction that the
    /// colour source "is the definition catalog, never a presentation-side palette guess". A card's
    /// printed colour is stated by its own <see cref="CardProgressionProfile"/>, which
    /// <see cref="CardDefinition"/> refuses to be constructed without, and its printed values by its
    /// own action definitions. Every client holds the same catalog, so two clients shown the same log
    /// row draw the same number in the same colour without anything being sent between them.</para>
    ///
    /// <para>An unreadable card answers <see cref="KineticNumber.None"/> rather than throwing or
    /// guessing: a kinetic is a flourish, and a missing figure must cost the flourish and never the
    /// frame.</para>
    /// </summary>
    public static class KineticCardFacts
    {
        public static KineticNumber PrintedNumber(
            IMatchReadModel projection,
            CardInstanceId cardInstanceId,
            PrintedValueChannel channel,
            KineticNumberRole role)
        {
            var definition = FindDefinition(projection, cardInstanceId);
            if (definition == null || !TryPrintedValue(definition, channel, out var amount))
            {
                return KineticNumber.None;
            }

            return new KineticNumber(amount, role, definition.Progression.Color);
        }

        /// <summary>The printed colour of a named card, or <see cref="CardColor.None"/>.</summary>
        public static CardColor ColorOf(IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            var definition = FindDefinition(projection, cardInstanceId);
            return definition == null ? CardColor.None : definition.Progression.Color;
        }

        private static CardDefinition FindDefinition(
            IMatchReadModel projection, CardInstanceId cardInstanceId)
        {
            if (projection == null || !cardInstanceId.IsDefined)
            {
                return null;
            }

            var cards = projection.Cards;
            var definitions = projection.Definitions;
            if (cards == null || definitions == null)
            {
                return null;
            }

            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                if (card == null || !card.Id.Equals(cardInstanceId))
                {
                    continue;
                }

                for (var candidate = 0; candidate < definitions.Count; candidate++)
                {
                    var definition = definitions[candidate];
                    if (definition != null && definition.Reference.Equals(card.DefinitionReference))
                    {
                        return definition;
                    }
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// The first value the card prints on <paramref name="channel"/>, in the card's own declared
        /// action order. Deterministic, so every client reads the same figure off the same card.
        /// </summary>
        private static bool TryPrintedValue(
            CardDefinition definition, PrintedValueChannel channel, out int amount)
        {
            amount = 0;
            var actions = definition.ActionDefinitions;
            if (actions == null)
            {
                return false;
            }

            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                if (action?.BaseValues == null)
                {
                    continue;
                }

                for (var value = 0; value < action.BaseValues.Count; value++)
                {
                    if (action.BaseValues[value].Channel != channel)
                    {
                        continue;
                    }

                    amount = action.BaseValues[value].Amount;
                    return amount >= 0;
                }
            }

            return false;
        }
    }
}

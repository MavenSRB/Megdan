using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z7 (the TENTH amendment's drop-down). One rendered line of the seat-public match
    /// log, or a ROUND/TURN boundary header among them.
    /// </summary>
    /// <remarks>
    /// A header is presentation-only styling, never a second selection: every line here still comes
    /// from exactly one <see cref="MatchLogEntry"/> the contract already carries, or from the one
    /// published <see cref="MatchUndoMarker"/> fact - nothing here re-derives what the read model
    /// already states.
    /// </remarks>
    public readonly struct MatchLogLineViewModel : IEquatable<MatchLogLineViewModel>
    {
        public MatchLogLineViewModel(int sequence, string text, bool isHeader)
        {
            Sequence = sequence;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            IsHeader = isHeader;
        }

        /// <summary>
        /// The source entry's own <see cref="MatchLogEntry.Sequence"/>, or <see cref="int.MaxValue"/>
        /// for the standing undo marker - which names no rule event and reads as the match's current
        /// status rather than as a moment in its history, so it takes whichever end of the list
        /// "now" is. Under item 2's newest-first ordering that is the TOP; before it, the bottom.
        /// The value itself is unchanged: it is the marker's "later than every sequence" fact, not
        /// its position.
        /// </summary>
        public int Sequence { get; }

        public string Text { get; }

        /// <summary>Whether this line is a ROUND/TURN boundary rather than a recorded activity.</summary>
        public bool IsHeader { get; }

        public bool Equals(MatchLogLineViewModel other) =>
            Sequence == other.Sequence &&
            string.Equals(Text, other.Text, StringComparison.Ordinal) &&
            IsHeader == other.IsHeader;

        public override bool Equals(object obj) => obj is MatchLogLineViewModel other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Sequence;
                hashCode = (hashCode * 397) ^ (Text?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ IsHeader.GetHashCode();
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Milestone 8.35z7. What the narration bar's drop-down draws: the log's rendered lines, NEWEST
    /// first (milestone 8.35z40, item 2 of the 2026-09-06 refinements), and whether the drop-down is
    /// currently open.
    /// </summary>
    /// <remarks>
    /// <para><b>Open/closed is local presentation state, not match state</b> - the same distinction
    /// <c>ConfiguredMatchHudRenderer.IsDirectorDrawerOpen</c> and
    /// <see cref="DiagnosticLogViewModel.IsExpanded"/> already draw. It changes no command, no
    /// projection and nothing on the wire; only <see cref="MatchLogPresenter"/> remembers it between
    /// refreshes.</b></para>
    /// <para><b>Lines are empty while closed.</b> The amendment's placement is a collapsed narration
    /// line by default, and there is nothing for a closed drop-down to draw - composing the whole
    /// history every frame whether or not anyone can see it would be waste with no reader.</para>
    /// </remarks>
    public sealed class MatchLogViewModel
    {
        public MatchLogViewModel(long version, bool isOpen, IReadOnlyList<MatchLogLineViewModel> lines)
        {
            Version = version;
            IsOpen = isOpen;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            ToggleLabel = isOpen ? "▴" : "▾"; // ▴ / ▾ - the chevron affordance.
        }

        public long Version { get; }

        public bool IsOpen { get; }

        public IReadOnlyList<MatchLogLineViewModel> Lines { get; }

        /// <summary>The chevron glyph the toggle control wears, naming its own open/closed state.</summary>
        public string ToggleLabel { get; }
    }

    public interface IMatchLogView
    {
        void Render(MatchLogViewModel model);
    }

    /// <summary>
    /// Milestone 8.35z7. Drives the narration bar's drop-down from <see cref="IMatchReadModel.MatchLog"/>
    /// and <see cref="IMatchReadModel.UndoMarker"/>, exactly as <c>DiagnosticLogPresenter</c> drives the
    /// Director-only event log from its own source - open/closed is this presenter's own remembered
    /// state, toggled by the narration bar's click or chevron and by nothing the match itself does.
    /// </summary>
    public sealed class MatchLogPresenter
    {
        private readonly IMatchClient client;
        private readonly IMatchLogView view;
        private bool isExpanded;
        private IMatchReadModel lastProjection;
        private bool lastExpanded;
        private MatchLogViewModel lastViewModel;

        private readonly EffectCapabilityCatalog capabilities;

        /// <param name="capabilities">Milestone 9.32g. The composition's capability catalog, which
        /// lets an attack row say the enemy-obstacle effect its card started; without it that line is
        /// simply absent.</param>
        public MatchLogPresenter(
            IMatchClient client, IMatchLogView view, EffectCapabilityCatalog capabilities = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.capabilities = capabilities;
        }

        /// <summary>Whether the drop-down is open. Local state; it changes no match state.</summary>
        public bool IsOpen => isExpanded;

        public void Refresh()
        {
            var projection = client.Projection;
            if (!ReferenceEquals(projection, lastProjection) || isExpanded != lastExpanded)
            {
                lastProjection = projection;
                lastExpanded = isExpanded;
                lastViewModel = MatchLogFormatter.CreateViewModel(projection, isExpanded, capabilities);
            }

            view.Render(lastViewModel);
        }

        /// <summary>Opens the drop-down if closed, closes it if open - the click/chevron affordance.</summary>
        public void Toggle()
        {
            isExpanded = !isExpanded;
            Refresh();
        }

        /// <summary>
        /// Closes the drop-down if it is open; does nothing otherwise. The click-away and the
        /// arriving-decision closes both want "closed", never "toggled" - calling this on an already
        /// closed drop-down must not reopen it.
        /// </summary>
        public void Dismiss()
        {
            if (!isExpanded)
            {
                return;
            }

            isExpanded = false;
            Refresh();
        }
    }

    /// <summary>
    /// Milestone 8.35z7. Turns <see cref="IMatchReadModel.MatchLog"/>'s typed entries and
    /// <see cref="IMatchReadModel.UndoMarker"/> into the lines the drop-down draws.
    /// </summary>
    /// <remarks>
    /// <para><b>It reads the closed <see cref="MatchLogEntryKind"/> vocabulary only.</b> Nothing here
    /// parses <c>RuleEventLogEntry.EventType</c> or any other diagnostic string - the selection and the
    /// privacy boundary were already drawn once, in <see cref="MatchLogProjection"/>, and this is
    /// purely a rendering of what that projection already decided to carry.</para>
    /// <para><b>Wording follows the same voice <see cref="NarrationLabels"/> speaks in</b> - the hero's
    /// own display name, upper case, read the identical way <c>HeroDisplayName</c> reads it elsewhere
    /// in this HUD - even though no single <see cref="NarrationLabels"/> method fits a closed
    /// history line's shape (each of its four speaks to "what the match is doing right now", not "what
    /// already happened"), so the vocabulary is reused as a style rather than as a shared call.</para>
    /// <para><b>A missing lookup degrades the word, never the line.</b> A minion or a card the log
    /// names may since have left the board entirely - that is the ordinary shape of a defeat or a
    /// discard - so every lookup here falls back to a generic noun instead of throwing. The log
    /// is recent history a player scrolls, and a blank scroll from one unresolved lookup would be
    /// worse than a slightly generic word.</para>
    /// </remarks>
    public static class MatchLogFormatter
    {
        public static MatchLogViewModel CreateViewModel(
            IMatchReadModel projection, bool isOpen, EffectCapabilityCatalog capabilities = null)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var lines = new List<MatchLogLineViewModel>();
            if (isOpen)
            {
                // The standing undo marker leads, for the reason it used to trail: it names no
                // rule event, so it is the match's CURRENT status rather than a moment in the
                // history. Under item 2's newest-first ordering the top is where "current" reads.
                AppendUndoMarker(lines, projection);
                lines.AddRange(NewestFirst(ComposeOldestFirst(projection, capabilities)));
            }

            return new MatchLogViewModel(projection.Version, isOpen, Array.AsReadOnly(lines.ToArray()));
        }

        /// <summary>
        /// The log's activity lines in the order the recorded event stream states them - oldest
        /// first, one line per <see cref="MatchLogEntry"/>, the undo marker excluded because it is
        /// not one of them.
        /// </summary>
        /// <remarks>
        /// Exposed as its own step so the closed-vocabulary rendering (one line per
        /// <see cref="MatchLogEntryKind"/>) and the reading ORDER (<see cref="NewestFirst"/>) can be
        /// proved separately. They are two different decisions and a single fixture over the
        /// composed result would have to know both to assert either.
        /// </remarks>
        public static IReadOnlyList<MatchLogLineViewModel> ComposeOldestFirst(
            IMatchReadModel projection, EffectCapabilityCatalog capabilities = null)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var lines = new List<MatchLogLineViewModel>();
            foreach (var entry in projection.MatchLog ?? Array.Empty<MatchLogEntry>())
            {
                if (entry == null)
                {
                    continue;
                }

                AppendLine(lines, projection, entry, capabilities);
            }

            return Array.AsReadOnly(lines.ToArray());
        }

        /// <summary>
        /// Item 2 of the 2026-09-06 log drop-down refinements: "NEWEST TEXT ON TOP -
        /// reverse-chronological ordering, with the ROUND/TURN group headers adapting correctly (the
        /// newest round's group at the top, entries within a group also newest-first)".
        /// </summary>
        /// <remarks>
        /// <para><b>Why this is not <c>Reverse()</c>.</b> A plain reversal puts every ROUND/TURN
        /// header BELOW the entries it heads, because a header is emitted before its group in the
        /// oldest-first stream - the "headers adapting correctly" the item names is exactly the part
        /// a plain reversal gets wrong. So the stream is split into groups first (a header line and
        /// the activity lines following it), the GROUPS are reversed, and each group's own body is
        /// reversed beneath its header, which stays on top of it. The result reads the way a chat
        /// log grouped by day reads: newest group first, newest line first inside it, and every
        /// heading still labelling what is under it.</para>
        /// <para>A leading run of activity lines with no header of its own - the shape of a retained
        /// window truncated mid-round - forms a headerless group and is reversed like any other, so
        /// nothing is dropped and nothing is invented to head it.</para>
        /// <para><b>It re-orders, and only re-orders.</b> No line is added, removed or re-worded
        /// here, so the seat-public selection and the privacy predicate drawn once in
        /// <c>MatchLogProjection</c> are untouched by definition - this cannot surface a fact that
        /// was not already in the list handed to it.</para>
        /// </remarks>
        public static IReadOnlyList<MatchLogLineViewModel> NewestFirst(
            IReadOnlyList<MatchLogLineViewModel> oldestFirst)
        {
            if (oldestFirst == null)
            {
                throw new ArgumentNullException(nameof(oldestFirst));
            }

            var groups = new List<List<MatchLogLineViewModel>>();
            foreach (var line in oldestFirst)
            {
                if (line.IsHeader || groups.Count == 0)
                {
                    groups.Add(new List<MatchLogLineViewModel>());
                }

                groups[groups.Count - 1].Add(line);
            }

            var ordered = new List<MatchLogLineViewModel>(oldestFirst.Count);
            for (var group = groups.Count - 1; group >= 0; group--)
            {
                var lines = groups[group];
                var headed = lines[0].IsHeader;
                if (headed)
                {
                    ordered.Add(lines[0]);
                }

                for (var body = lines.Count - 1; body >= (headed ? 1 : 0); body--)
                {
                    ordered.Add(lines[body]);
                }
            }

            return Array.AsReadOnly(ordered.ToArray());
        }

        private static void AppendUndoMarker(List<MatchLogLineViewModel> lines, IMatchReadModel projection)
        {
            var marker = projection.UndoMarker;
            if (!marker.IsStated)
            {
                return;
            }

            var noun = marker.Count == 1 ? "ACTION" : "ACTIONS";
            lines.Add(new MatchLogLineViewModel(
                int.MaxValue,
                $"{HeroName(projection, marker.HeroId)} TOOK BACK {marker.Count} {noun}",
                isHeader: false));
        }

        private static void AppendLine(
            List<MatchLogLineViewModel> lines,
            IMatchReadModel projection,
            MatchLogEntry entry,
            EffectCapabilityCatalog capabilities = null)
        {
            switch (entry.Kind)
            {
                case MatchLogEntryKind.RoundStarted:
                    lines.Add(Header(entry, $"ROUND {entry.RoundIndex}"));
                    return;

                case MatchLogEntryKind.RoundCompleted:
                    lines.Add(Header(entry, $"ROUND {entry.RoundIndex} COMPLETE"));
                    return;

                case MatchLogEntryKind.TurnStarted:
                    lines.Add(Header(entry, $"TURN {entry.TurnIndex}"));
                    return;

                case MatchLogEntryKind.TurnEnded:
                {
                    var hero = HeroNameOrNull(projection, entry.SubjectHeroId);
                    lines.Add(Header(
                        entry,
                        hero == null ? $"TURN {entry.TurnIndex} ENDED" : $"TURN {entry.TurnIndex} ENDED — {hero}"));
                    return;
                }

                case MatchLogEntryKind.Move:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} MOVED{SpacesPhrase(entry)}"));
                    return;

                case MatchLogEntryKind.FastTravel:
                    // USER DIRECTIVE (2026-09-08): the directive's own second example is exactly
                    // this line with no distance at all - see SpacesPhrase's remarks for why a
                    // zone teleport never states one.
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} FAST TRAVELED"));
                    return;

                case MatchLogEntryKind.Attack:
                {
                    var attacker = HeroName(projection, entry.SubjectHeroId);
                    var target = HeroNameOrNull(projection, entry.TargetHeroId) ??
                        MinionName(projection, entry.SubjectMinionId);
                    var outcome = AttackOutcomeWord(entry.Outcome);
                    lines.Add(Line(entry,
                        target == null
                            ? $"{attacker} ATTACKED{outcome}"
                            : $"{attacker} ATTACKED {target}{outcome}"));

                    // Milestone 9.32g (approved mockup F). An attack whose card starts an effect that
                    // bars the enemy's units from empty spaces says so on the next line, worded from
                    // the card - "adjacent" where the text writes it, "in radius" where it prints it.
                    var obstacles = EnemyObstacleWords(projection, entry.SubjectCardInstanceId, capabilities);
                    if (obstacles != null)
                    {
                        lines.Add(Line(entry,
                            $"{attacker}: EMPTY SPACES {obstacles} ARE OBSTACLES FOR THE ENEMY THIS TURN"));
                    }

                    return;
                }

                case MatchLogEntryKind.Defense:
                {
                    var defender = HeroName(projection, entry.SubjectHeroId);
                    var outcome = entry.Outcome == MatchLogOutcome.DefenseCardDiscarded
                        ? "DEFENDED"
                        : "DECLINED TO DEFEND";
                    lines.Add(Line(entry, $"{defender} {outcome}"));
                    return;
                }

                case MatchLogEntryKind.SkillUsed:
                {
                    var card = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        card == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} USED A SKILL"
                            : $"{HeroName(projection, entry.SubjectHeroId)} USED {card}"));
                    return;
                }

                case MatchLogEntryKind.Clear:
                    lines.Add(Line(entry, $"{HeroName(projection, entry.SubjectHeroId)} CLEARED A TOKEN"));
                    return;

                // Milestone 8.61e. The tray says what the table watched happen: a hero walked onto a
                // facedown piece and it turned out to be a blast or a dud.
                case MatchLogEntryKind.MoveThroughTrapSprung:
                    lines.Add(Line(
                        entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} MOVED THROUGH " +
                            (entry.Amount > 0 ? "A BLAST" : "A DUD")));
                    return;

                case MatchLogEntryKind.BlockedAction:
                    // The contract's own limitation: the blocker is not named on this channel today.
                    lines.Add(Line(entry, $"{HeroName(projection, entry.SubjectHeroId)}'S ACTION WAS BLOCKED"));
                    return;

                case MatchLogEntryKind.MinionDefeated:
                    lines.Add(Line(entry, $"{MinionName(projection, entry.SubjectMinionId)} WAS DEFEATED"));
                    return;

                case MatchLogEntryKind.MinionRemoved:
                    lines.Add(Line(entry, $"{MinionName(projection, entry.SubjectMinionId)} WAS REMOVED"));
                    return;

                case MatchLogEntryKind.MinionRemovalPrevented:
                    lines.Add(Line(entry,
                        $"{MinionName(projection, entry.SubjectMinionId)}'S REMOVAL WAS PREVENTED"));
                    return;

                case MatchLogEntryKind.HeroDefeated:
                    lines.Add(Line(entry, $"{HeroName(projection, entry.SubjectHeroId)} WAS DEFEATED"));
                    return;

                case MatchLogEntryKind.HeroRemoved:
                    lines.Add(Line(entry, $"{HeroName(projection, entry.SubjectHeroId)} WAS REMOVED"));
                    return;

                case MatchLogEntryKind.HeroRemovalPrevented:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)}'S REMOVAL WAS PREVENTED"));
                    return;

                case MatchLogEntryKind.HeroRespawned:
                    lines.Add(Line(entry, $"{HeroName(projection, entry.SubjectHeroId)} RESPAWNED"));
                    return;

                // Milestone 8.35z61. p.13's second line, said out loud. The sentence names
                // the CAUSE as well as the fact, because the fact alone is what the playtest could
                // not explain: a player whose card resolved without them being asked for anything
                // needs to read why, and "was defeated" is the whole of the why.
                case MatchLogEntryKind.HeroCardResolvedWithoutAction:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)}'S CARD RESOLVED WITH NO " +
                        "ACTION - DEFEATED BEFORE ITS TURN"));
                    return;

                case MatchLogEntryKind.TeamLifeSpent:
                {
                    var hero = HeroNameOrNull(projection, entry.SubjectHeroId);
                    lines.Add(Line(entry,
                        hero == null ? "A TEAM SPENT A LIFE" : $"{hero}'S TEAM SPENT A LIFE"));
                    return;
                }

                case MatchLogEntryKind.TeamEliminated:
                {
                    var hero = HeroNameOrNull(projection, entry.SubjectHeroId);
                    lines.Add(Line(entry,
                        hero == null ? "A TEAM WAS ELIMINATED" : $"{hero}'S TEAM WAS ELIMINATED"));
                    return;
                }

                case MatchLogEntryKind.DisplacedMinionCorrected:
                    lines.Add(Line(entry,
                        $"{MinionName(projection, entry.SubjectMinionId)}'S DISPLACEMENT WAS " +
                        $"CORRECTED{SpacesPhrase(entry)}"));
                    return;

                case MatchLogEntryKind.MinionBattleMinionRemoved:
                    lines.Add(Line(entry,
                        $"MINION BATTLE REMOVED {MinionName(projection, entry.SubjectMinionId)}"));
                    return;

                case MatchLogEntryKind.LanePush:
                {
                    var hero = HeroNameOrNull(projection, entry.SubjectHeroId);
                    lines.Add(Line(entry, hero == null ? "A LANE PUSH RESOLVED" : $"{hero} FACED A LANE PUSH"));
                    return;
                }

                case MatchLogEntryKind.LevelPurchased:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} BOUGHT LEVEL {entry.Amount}"));
                    return;

                case MatchLogEntryKind.CardUpgraded:
                {
                    var card = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        card == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} UPGRADED A CARD"
                            : $"{HeroName(projection, entry.SubjectHeroId)} UPGRADED {card}"));
                    return;
                }

                case MatchLogEntryKind.ItemGained:
                {
                    var card = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        card == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} GAINED AN ITEM"
                            : $"{HeroName(projection, entry.SubjectHeroId)} GAINED {card}"));
                    return;
                }

                case MatchLogEntryKind.PityCoinCollected:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} COLLECTED {entry.Amount} PITY COIN" +
                        (entry.Amount == 1 ? string.Empty : "S")));
                    return;

                case MatchLogEntryKind.CoinAwarded:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} GAINED {entry.Amount} " +
                        (entry.Amount == 1 ? "COIN" : "COINS")));
                    return;

                case MatchLogEntryKind.GameEnded:
                {
                    var team = TeamNameOrNull(projection, entry.SubjectTeamId);
                    lines.Add(Header(entry, team == null ? "GAME ENDED" : $"GAME ENDED — {team} WINS"));
                    return;
                }

                case MatchLogEntryKind.CardRevealed:
                {
                    var card = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        card == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} REVEALED A CARD"
                            : $"{HeroName(projection, entry.SubjectHeroId)} REVEALED {card}"));
                    return;
                }

                // Milestone 8.40f. The same sentence for the same fact, and a separate arm because
                // the two rows are separate kinds - see MatchLogEntryKind.CardRevealedByCardText for
                // the three turn-card rules this one deliberately does not inherit. The card resolves
                // by name in EVERY seat, because the row itself is what made it nameable.
                case MatchLogEntryKind.CardRevealedByCardText:
                {
                    var shown = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        shown == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} REVEALED A CARD"
                            : $"{HeroName(projection, entry.SubjectHeroId)} REVEALED {shown}"));
                    return;
                }

                // Milestone 9.19. Card text sent a card out of a hand. The card resolves by name in
                // every seat for the reason the reveal does: the row is what made it nameable.
                case MatchLogEntryKind.CardDiscardedByCardText:
                {
                    var shown = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        shown == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} DISCARDED A CARD"
                            : $"{HeroName(projection, entry.SubjectHeroId)} DISCARDED {shown}"));
                    return;
                }

                // Milestone 9.32g (approved mockup F). A swap, a kept swap and an Ultimate's unlock
                // that moved cards. Each move names its card through the definition the row carries,
                // which the seat projection has already cleared wherever this seat may not read it - so
                // a card is named exactly where its owner, or the whole table, saw it, and an unnamed
                // one is told by its zone and the colour its text fixes. No pronouns.
                case MatchLogEntryKind.CardSwappedByCardText:
                    lines.Add(Line(entry, CardMoveWords.Swapped(
                        projection.Definitions, HeroName(projection, entry.SubjectHeroId), entry.CardMoves,
                        SourceSuffix(projection, entry))));
                    return;

                case MatchLogEntryKind.CardKeptByCardText:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} KEPT " +
                        $"{CardMoveWords.NameOrNull(projection.Definitions, entry.CardMoves.FirstOrDefault()) ?? "A CARD"}" +
                        SourceSuffix(projection, entry)));
                    return;

                case MatchLogEntryKind.UltimateMovedCards:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} UNLOCKED " +
                        $"{CardNameOrNull(projection, entry.SubjectCardInstanceId) ?? "AN ULTIMATE"}: " +
                        CardMoveWords.Grouped(projection.Definitions, entry.CardMoves)));
                    return;

                // Milestone 9.30h. A spellbook's three public facts. A cast and a removal name the
                // spell through the seat's own projection, which holds it while it lies faceup in the
                // open removed pile; once a refill turns it facedown again, a seat that is not the
                // book's owner reads "A SPELL" - the row never names a spell back in the book.
                case MatchLogEntryKind.SpellCast:
                {
                    var spell = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} CAST {spell ?? "A SPELL"}"));
                    return;
                }

                case MatchLogEntryKind.SpellRemovedFaceup:
                {
                    var spell = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} REMOVED {spell ?? "A SPELL"} " +
                        "FROM THE SPELLBOOK"));
                    return;
                }

                case MatchLogEntryKind.SpellbookRefilled:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} RETURNED {entry.Amount} " +
                        (entry.Amount == 1 ? "SPELL" : "SPELLS") + " TO THE SPELLBOOK"));
                    return;

                // Milestone 8.51. Rulebook p.12's Hold by the seat's own command. The card is a
                // resolved turn card every seat sees, so it resolves by name wherever the
                // projection can name it, and the line says outright that no action was taken -
                // the fact the 2026-09-18 report needed at the table, not afterwards from a file.
                case MatchLogEntryKind.CardHeld:
                {
                    var held = CardNameOrNull(projection, entry.SubjectCardInstanceId);
                    lines.Add(Line(entry,
                        held == null
                            ? $"{HeroName(projection, entry.SubjectHeroId)} HELD THEIR CARD — NO ACTION"
                            : $"{HeroName(projection, entry.SubjectHeroId)} HELD {held} — NO ACTION"));
                    return;
                }

                default:
                    // MatchLogEntryKind.None never reaches an entry (the type throws on it), and every
                    // other member is handled above - a closed switch over a closed vocabulary.
                    return;
            }
        }

        /// <summary>
        /// USER DIRECTIVE (2026-09-08, verbatim): "in the history log tray thing. Can you lose the
        /// coordinates; they dont mean shit to player. Just say Hero-name moved 5 spaces, or hero
        /// fast traveled." A raw <see cref="HexCoordinate"/> is never player vocabulary; a hex
        /// distance in spaces is. This reads <see cref="MatchLogEntry.Origin"/> and
        /// <see cref="MatchLogEntry.Destination"/> ONLY to hand them to the board's own
        /// <see cref="HexDistanceQuery.Calculate"/> - the exact function the reach/radius machinery
        /// already measures attack range and push distance with - and then discards the coordinates
        /// themselves. Neither value is ever interpolated into a line anywhere in this file.
        /// </summary>
        /// <remarks>
        /// <para><b>Why <see cref="MatchLogEntryKind.FastTravel"/> never calls this.</b> The
        /// directive's own second example is a Fast Travel line with NO number at all - a zone
        /// teleport has no hex-by-hex distance a player would recognise as "spaces travelled", so
        /// the action name alone is the whole player-meaningful fact for that kind.</para>
        /// <para><b>Only Origin/Destination are read, and only when both are stated.</b> A route
        /// with just one end stated derives no distance, so this returns nothing - the action name
        /// alone (e.g. plain "MOVED") is the fact, never a half-invented number.</para>
        /// </remarks>
        private static string SpacesPhrase(MatchLogEntry entry)
        {
            if (!entry.HasOrigin || !entry.HasDestination)
            {
                return string.Empty;
            }

            var spaces = HexDistanceQuery.Calculate(entry.Origin, entry.Destination);
            return $" {spaces} {(spaces == 1 ? "SPACE" : "SPACES")}";
        }

        private static string AttackOutcomeWord(MatchLogOutcome outcome)
        {
            switch (outcome)
            {
                case MatchLogOutcome.AttackSuccessful: return " — SUCCESSFUL";
                case MatchLogOutcome.AttackSuccessfullyDefended: return " — DEFENDED";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Milestone 9.32g. "(SET AN EXAMPLE)" where the swap's text came off another hero's card - a
        /// teammate's swap - and nothing where the swapping hero's own card made it: Takahide's own
        /// swaps already read as his.
        /// </summary>
        private static string SourceSuffix(IMatchReadModel projection, MatchLogEntry entry)
        {
            var source = (projection.Cards ?? Array.Empty<CardInstance>())
                .FirstOrDefault(card => card.Id.Equals(entry.SubjectCardInstanceId));
            var name = CardNameOrNull(projection, entry.SubjectCardInstanceId);
            return source == null || name == null || source.OwnerId.Equals(entry.SubjectHeroId)
                ? string.Empty
                : " (" + name + ")";
        }

        /// <summary>
        /// Milestone 9.32g. "ADJACENT" or "IN RADIUS" where the attack's card declares an Active effect
        /// whose overlay makes empty spaces obstacles for the enemy's units, read from the card's own
        /// typed effects through the catalog; null for every other card, or with no catalog.
        /// </summary>
        private static string EnemyObstacleWords(
            IMatchReadModel projection, CardInstanceId cardId, EffectCapabilityCatalog capabilities)
        {
            if (capabilities == null || !cardId.IsDefined)
            {
                return null;
            }

            var card = (projection.Cards ?? Array.Empty<CardInstance>()).FirstOrDefault(value => value.Id.Equals(cardId));
            var definition = card == null
                ? null
                : (projection.Definitions ?? Array.Empty<CardDefinition>())
                    .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            var declared = (definition?.ActionDefinitions ?? Array.Empty<ActionDefinition>())
                .SelectMany(action => ActiveEffectVisibility.DeclaredActiveEffects(action, capabilities))
                .FirstOrDefault(effect => effect.ObstacleOverlays.Any(overlay => overlay != null &&
                    overlay.Arms.Contains(ObstacleOverlayArm.EmptySpacesInsideForUnits)));
            if (declared == null)
            {
                return null;
            }

            return declared.Radius.Source == DeclaredRadiusSource.WrittenInText && declared.Radius.Value == 1
                ? "ADJACENT"
                : "IN RADIUS";
        }

        private static MatchLogLineViewModel Line(MatchLogEntry entry, string text) =>
            new MatchLogLineViewModel(entry.Sequence, text, isHeader: false);

        private static MatchLogLineViewModel Header(MatchLogEntry entry, string text) =>
            new MatchLogLineViewModel(entry.Sequence, text, isHeader: true);

        private static string HeroName(IMatchReadModel projection, HeroId heroId) =>
            HeroNameOrNull(projection, heroId) ?? "SOMEONE";

        private static string HeroNameOrNull(IMatchReadModel projection, HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                return null;
            }

            var heroes = projection.Setup?.Heroes ?? Array.Empty<QuickGameHeroSetupState>();
            var found = heroes.FirstOrDefault(hero => hero.HeroId.Equals(heroId));
            return (found?.DisplayName ?? heroId.Value).ToUpperInvariant();
        }

        private static string MinionName(IMatchReadModel projection, MinionId minionId) =>
            MinionNameOrNull(projection, minionId) ?? "A MINION";

        private static string MinionNameOrNull(IMatchReadModel projection, MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                return null;
            }

            var minion = (projection.Minions ?? Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>())
                .FirstOrDefault(occupancy => occupancy.MinionId.Equals(minionId));
            return minion == null
                ? null
                : $"{MinionChoiceLabels.TeamLabel(minion.TeamSide)} {minion.MinionType.ToString().ToUpperInvariant()} MINION";
        }

        private static string CardNameOrNull(IMatchReadModel projection, CardInstanceId cardId)
        {
            if (!cardId.IsDefined)
            {
                return null;
            }

            var card = (projection.Cards ?? Array.Empty<CardInstance>())
                .FirstOrDefault(instance => instance.Id.Equals(cardId));
            if (card == null)
            {
                return null;
            }

            var definition = (projection.Definitions ?? Array.Empty<CardDefinition>())
                .FirstOrDefault(candidate => candidate.Reference.Equals(card.DefinitionReference));
            return definition?.DisplayName?.ToUpperInvariant();
        }

        private static string TeamNameOrNull(IMatchReadModel projection, TeamId teamId)
        {
            if (!teamId.IsDefined)
            {
                return null;
            }

            try
            {
                var team = projection.Participants.GetTeam(teamId);
                return MinionChoiceLabels.TeamLabel(team.Side);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
}

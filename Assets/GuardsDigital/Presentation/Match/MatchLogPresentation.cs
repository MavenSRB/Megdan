using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35x (the TENTH amendment's drop-down). One rendered line of the seat-public match
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
        /// for the standing undo marker - which names no rule event and always reads last, as the
        /// match's current status rather than a moment in its history.
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
    /// Milestone 8.35x. What the narration bar's drop-down draws: the log's rendered lines, oldest
    /// first, and whether the drop-down is currently open.
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
    /// Milestone 8.35x. Drives the narration bar's drop-down from <see cref="IMatchReadModel.MatchLog"/>
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

        public MatchLogPresenter(IMatchClient client, IMatchLogView view)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
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
                lastViewModel = MatchLogFormatter.CreateViewModel(projection, isExpanded);
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
    /// Milestone 8.35x. Turns <see cref="IMatchReadModel.MatchLog"/>'s typed entries and
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
        public static MatchLogViewModel CreateViewModel(IMatchReadModel projection, bool isOpen)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            var lines = new List<MatchLogLineViewModel>();
            if (isOpen)
            {
                foreach (var entry in projection.MatchLog ?? Array.Empty<MatchLogEntry>())
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    AppendLine(lines, projection, entry);
                }

                AppendUndoMarker(lines, projection);
            }

            return new MatchLogViewModel(projection.Version, isOpen, Array.AsReadOnly(lines.ToArray()));
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
            List<MatchLogLineViewModel> lines, IMatchReadModel projection, MatchLogEntry entry)
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
                        $"{HeroName(projection, entry.SubjectHeroId)} MOVED{Route(entry)}"));
                    return;

                case MatchLogEntryKind.FastTravel:
                    lines.Add(Line(entry,
                        $"{HeroName(projection, entry.SubjectHeroId)} FAST TRAVELED{Route(entry)}"));
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
                        $"{MinionName(projection, entry.SubjectMinionId)}'S DISPLACEMENT WAS CORRECTED{Route(entry)}"));
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

                default:
                    // MatchLogEntryKind.None never reaches an entry (the type throws on it), and every
                    // other member is handled above - a closed switch over a closed vocabulary.
                    return;
            }
        }

        private static string Route(MatchLogEntry entry) =>
            entry.HasOrigin && entry.HasDestination
                ? $" {entry.Origin} → {entry.Destination}"
                : entry.HasDestination
                    ? $" TO {entry.Destination}"
                    : string.Empty;

        private static string AttackOutcomeWord(MatchLogOutcome outcome)
        {
            switch (outcome)
            {
                case MatchLogOutcome.AttackSuccessful: return " — SUCCESSFUL";
                case MatchLogOutcome.AttackSuccessfullyDefended: return " — DEFENDED";
                default: return string.Empty;
            }
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

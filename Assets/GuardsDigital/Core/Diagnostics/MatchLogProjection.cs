using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Diagnostics
{
    /// <summary>
    /// Milestone 8.35r. Turns the recorded rule-event log into the seat-public match log: the
    /// selection contract's rows, typed, in order, bounded to a recent window.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this reads the rule-event log and not the round-event stream.</b> The round
    /// stream is the CURRENT ROUND's events and it is installed empty by every snapshot restore
    /// (<c>MatchAggregate.Restore</c> passes <c>Array.Empty&lt;RoundResolutionEvent&gt;()</c>). The
    /// rule-event log is the only match-long history the engine keeps and the only one a restore
    /// brings back. Deriving from it is therefore what makes the contract's promise true - that an
    /// undo, a replay, a resync and a restore each redraw the log for free - rather than a
    /// preference between two equivalent sources.</para>
    /// <para><b>Why the event names are built with <c>nameof</c>.</b> The log composes its type
    /// string from the rule-event enum member (<c>$"Action.{kind}"</c>), so naming the members here
    /// with <c>nameof</c> makes a renamed kind a COMPILE error instead of a line that silently stops
    /// being selected. That is the drift this translation could otherwise suffer in silence.</para>
    /// <para><b>What it refuses to do.</b> It never emits a kind the selection contract did not
    /// name, so an excluded event cannot reach a client by being mistaken for an included one. A
    /// carried event whose required reference is missing throws rather than emitting a blank line:
    /// a log that quietly says nothing is worse than one that fails loudly.</para>
    /// </remarks>
    public static class MatchLogProjection
    {
        /// <summary>
        /// How many of the most recent lines the log keeps. The log is recent history a player
        /// scrolls, not an archive: a bounded window is what lets one frame carry the whole thing to
        /// a seat that just arrived, and is why a returning seat and a seat that never left hold the
        /// SAME log.
        /// </summary>
        public const int RetainedEntries = 256;

        private const string ActionPrefix = "Action.";
        private const string RoundPrefix = "Round.";
        private const string TurnPrefix = "Turn.";
        private const string CommitmentPrefix = "Commitment.";

        // The carried event types, each built from the enum member the log composes its name from.
        private static readonly string MovementStepCommitted =
            ActionPrefix + nameof(CardActionResolutionEventKind.MovementStepCommitted);
        private static readonly string MovementActionCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.MovementActionCompleted);
        private static readonly string FastTravelActionCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.FastTravelActionCompleted);
        private static readonly string AttackStarted =
            ActionPrefix + nameof(CardActionResolutionEventKind.AttackStarted);
        private static readonly string AttackSuccessful =
            ActionPrefix + nameof(CardActionResolutionEventKind.AttackSuccessful);
        private static readonly string AttackSuccessfullyDefended =
            ActionPrefix + nameof(CardActionResolutionEventKind.AttackSuccessfullyDefended);
        private static readonly string AttackResolutionCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.AttackResolutionCompleted);
        private static readonly string DefenseCardDiscarded =
            ActionPrefix + nameof(CardActionResolutionEventKind.DefenseCardDiscarded);
        private static readonly string DefenseDeclined =
            ActionPrefix + nameof(CardActionResolutionEventKind.DefenseDeclined);
        private static readonly string DefenseResponseCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.DefenseResponseCompleted);
        private static readonly string SkillActionCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.SkillActionCompleted);
        private static readonly string TokenCleared =
            ActionPrefix + nameof(CardActionResolutionEventKind.TokenCleared);
        private static readonly string SkillEffectNodeStopped =
            ActionPrefix + nameof(CardActionResolutionEventKind.SkillEffectNodeStopped);
        private static readonly string CardTextApplied =
            ActionPrefix + nameof(CardActionResolutionEventKind.CardTextApplied);
        private static readonly string MinionDefeatRecorded =
            ActionPrefix + nameof(CardActionResolutionEventKind.MinionDefeatRecorded);
        private static readonly string MinionRemovalCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.MinionRemovalCompleted);
        private static readonly string MinionRemovalPrevented =
            ActionPrefix + nameof(CardActionResolutionEventKind.MinionRemovalPrevented);
        private static readonly string HeroDefeatRecorded =
            ActionPrefix + nameof(CardActionResolutionEventKind.HeroDefeatRecorded);
        private static readonly string HeroRemovalCompleted =
            ActionPrefix + nameof(CardActionResolutionEventKind.HeroRemovalCompleted);
        private static readonly string HeroRemovalPrevented =
            ActionPrefix + nameof(CardActionResolutionEventKind.HeroRemovalPrevented);

        // Milestone 8.35z61. p.13's second line, which the engine has been publishing since
        // milestone 7.4 with nothing reading it. See MatchLogEntryKind.HeroCardResolvedWithoutAction.
        private static readonly string HeroUnresolvedCardResolved =
            ActionPrefix + nameof(CardActionResolutionEventKind.HeroUnresolvedCardResolved);
        private static readonly string TeamLifeSpent =
            ActionPrefix + nameof(CardActionResolutionEventKind.TeamLifeSpent);
        private static readonly string TeamEliminated =
            ActionPrefix + nameof(CardActionResolutionEventKind.TeamEliminated);
        private static readonly string DisplacedMinionCorrectionMoved =
            ActionPrefix + nameof(CardActionResolutionEventKind.DisplacedMinionCorrectionMoved);
        private static readonly string DisplacedMinionCorrectionPlaced =
            ActionPrefix + nameof(CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced);
        private static readonly string DisplacedMinionCorrectionLeftInPlace =
            ActionPrefix + nameof(CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace);

        private static readonly string HeroRespawned =
            TurnPrefix + nameof(TurnResolutionEventKind.HeroRespawned);
        private static readonly string TurnEnded =
            TurnPrefix + nameof(TurnResolutionEventKind.TurnEnded);
        private static readonly string ImmediateLanePushResolved =
            TurnPrefix + nameof(TurnResolutionEventKind.ImmediateLanePushResolved);

        private static readonly string TurnSlotRecorded =
            RoundPrefix + nameof(RoundResolutionEventKind.TurnSlotRecorded);
        private static readonly string NextRoundStarted =
            RoundPrefix + nameof(RoundResolutionEventKind.NextRoundStarted);
        private static readonly string RoundCompleted =
            RoundPrefix + nameof(RoundResolutionEventKind.RoundCompleted);
        private static readonly string GameEnded =
            RoundPrefix + nameof(RoundResolutionEventKind.GameEnded);
        private static readonly string LanePushRequested =
            RoundPrefix + nameof(RoundResolutionEventKind.LanePushRequested);
        private static readonly string MinionBattleMinionRemoved =
            RoundPrefix + nameof(RoundResolutionEventKind.MinionBattleMinionRemoved);
        private static readonly string PityCoinCollected =
            RoundPrefix + nameof(RoundResolutionEventKind.PityCoinCollected);
        private static readonly string HeroLevelUpLevelPurchased =
            RoundPrefix + "HeroLevelUp" + nameof(HeroLevelUpOutcomeKind.LevelPurchased);
        private static readonly string HeroLevelUpCardUpgraded =
            RoundPrefix + "HeroLevelUp" + nameof(HeroLevelUpOutcomeKind.CardUpgraded);
        private static readonly string HeroLevelUpItemGained =
            RoundPrefix + "HeroLevelUp" + nameof(HeroLevelUpOutcomeKind.ItemGained);

        private static readonly string CardRevealed =
            CommitmentPrefix + nameof(TurnCardCommitmentEventKind.CardRevealed);

        /// <summary>
        /// The suffix a card-text node's step id carries when the node could not be performed. The
        /// same <c>ResultKind</c> the skill backend translates into
        /// <see cref="CardActionResolutionEventKind.SkillEffectNodeStopped"/>, so the two blocked
        /// forms are recognised by one rule.
        /// </summary>
        private static readonly string StoppedNodeSuffix =
            ":" + nameof(EffectExecutionResultKind.ImpossibleMandatory);

        /// <summary>The whole derived log, bounded to <see cref="RetainedEntries"/>.</summary>
        public static IReadOnlyList<MatchLogEntry> Derive(IReadOnlyList<RuleEventLogEntry> entries) =>
            Derive(entries, RetainedEntries);

        /// <summary>
        /// The derived log, bounded to <paramref name="retainedEntries"/> most recent lines.
        /// </summary>
        public static IReadOnlyList<MatchLogEntry> Derive(
            IReadOnlyList<RuleEventLogEntry> entries,
            int retainedEntries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (retainedEntries < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(retainedEntries),
                    "The match log retains at least one line, or it is not a log.");
            }

            var derived = new List<MatchLogEntry>();
            var moves = new Dictionary<string, MoveFold>(StringComparer.Ordinal);
            var attacks = new Dictionary<string, AttackFold>(StringComparer.Ordinal);
            var defenses = new Dictionary<string, DefenseFold>(StringComparer.Ordinal);

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    throw new ArgumentException(
                        "The rule-event log contains a null entry.", nameof(entries));
                }

                Translate(entry, derived, moves, attacks, defenses);
            }

            if (derived.Count <= retainedEntries)
            {
                return derived;
            }

            return derived.GetRange(derived.Count - retainedEntries, retainedEntries);
        }

        private sealed class MoveFold
        {
            public bool HasOrigin;
            public HexCoordinate Origin;
            public bool HasDestination;
            public HexCoordinate Destination;
        }

        private sealed class AttackFold
        {
            public MatchLogOutcome Outcome;
        }

        private sealed class DefenseFold
        {
            public MatchLogOutcome Outcome;
            public string DefenseCardInstanceId;
        }

        private static void Translate(
            RuleEventLogEntry entry,
            List<MatchLogEntry> derived,
            Dictionary<string, MoveFold> moves,
            Dictionary<string, AttackFold> attacks,
            Dictionary<string, DefenseFold> defenses)
        {
            var type = entry.EventType;
            var references = Fields(entry.StableReferences);
            var detail = Fields(entry.Detail);
            var round = Number(references, "round");
            var turn = Number(references, "turn");

            // ---- The two folds. A move and an attack are ONE line each, closed by their own
            // completion event, so the steps and totals in between are read and never emitted.
            if (type == MovementStepCommitted)
            {
                var resolution = Required(references, "resolution", entry);
                if (!moves.TryGetValue(resolution, out var fold))
                {
                    fold = new MoveFold();
                    moves[resolution] = fold;
                }

                if (TryReadMove(detail, "move", out var origin, out var destination))
                {
                    if (!fold.HasOrigin)
                    {
                        fold.Origin = origin;
                        fold.HasOrigin = true;
                    }

                    fold.Destination = destination;
                    fold.HasDestination = true;
                }

                return;
            }

            if (type == MovementActionCompleted)
            {
                var resolution = Required(references, "resolution", entry);
                moves.TryGetValue(resolution, out var fold);
                moves.Remove(resolution);
                derived.Add(new MatchLogEntry(
                    entry.Sequence,
                    MatchLogEntryKind.Move,
                    round,
                    turn,
                    subjectHeroId: Hero(references, entry),
                    subjectCardInstanceId: Card(references),
                    origin: fold?.Origin ?? default,
                    destination: fold?.Destination ?? default,
                    hasOrigin: fold?.HasOrigin ?? false,
                    hasDestination: fold?.HasDestination ?? false));
                return;
            }

            if (type == AttackStarted)
            {
                attacks[Required(references, "resolution", entry)] = new AttackFold();
                return;
            }

            if (type == AttackSuccessful || type == AttackSuccessfullyDefended)
            {
                var resolution = Required(references, "resolution", entry);
                if (!attacks.TryGetValue(resolution, out var fold))
                {
                    fold = new AttackFold();
                    attacks[resolution] = fold;
                }

                fold.Outcome = type == AttackSuccessful
                    ? MatchLogOutcome.AttackSuccessful
                    : MatchLogOutcome.AttackSuccessfullyDefended;
                return;
            }

            if (type == AttackResolutionCompleted)
            {
                var resolution = Required(references, "resolution", entry);
                attacks.TryGetValue(resolution, out var fold);
                attacks.Remove(resolution);
                derived.Add(new MatchLogEntry(
                    entry.Sequence,
                    MatchLogEntryKind.Attack,
                    round,
                    turn,
                    subjectHeroId: Hero(references, entry),
                    targetHeroId: Optional(references, "target", value => new HeroId(value)),
                    subjectMinionId: Optional(references, "minion", value => new MinionId(value)),
                    subjectCardInstanceId: Card(references),
                    outcome: fold?.Outcome ?? MatchLogOutcome.None));
                return;
            }

            if (type == DefenseCardDiscarded || type == DefenseDeclined)
            {
                var resolution = Required(references, "resolution", entry);
                if (!defenses.TryGetValue(resolution, out var fold))
                {
                    fold = new DefenseFold();
                    defenses[resolution] = fold;
                }

                fold.Outcome = type == DefenseCardDiscarded
                    ? MatchLogOutcome.DefenseCardDiscarded
                    : MatchLogOutcome.DefenseDeclined;
                Text(references, "defenseCard", out fold.DefenseCardInstanceId);
                return;
            }

            if (type == DefenseResponseCompleted)
            {
                var resolution = Required(references, "resolution", entry);
                defenses.TryGetValue(resolution, out var fold);
                defenses.Remove(resolution);

                // The defender is the attack's TARGET; the attacker is the resolution's actor. The
                // line is about the hero who answered, so that hero is the subject.
                derived.Add(new MatchLogEntry(
                    entry.Sequence,
                    MatchLogEntryKind.Defense,
                    round,
                    turn,
                    subjectHeroId: Optional(references, "target", value => new HeroId(value)),
                    targetHeroId: Hero(references, entry),
                    subjectCardInstanceId: fold?.DefenseCardInstanceId == null
                        ? default
                        : new CardInstanceId(fold.DefenseCardInstanceId),
                    outcome: fold?.Outcome ?? MatchLogOutcome.None));
                return;
            }

            // ---- The blocked-action pair. Milestone 8.35z14 named the blocker where the refusing node
            // (or the consumer that narrowed its selection) could explain itself; an undefined
            // blocking hero/card is a real, honest absence and not something this reads as a fault.
            if (type == SkillEffectNodeStopped ||
                (type == CardTextApplied && EndsWith(detail, "step", StoppedNodeSuffix)))
            {
                derived.Add(new MatchLogEntry(
                    entry.Sequence,
                    MatchLogEntryKind.BlockedAction,
                    round,
                    turn,
                    subjectHeroId: Hero(references, entry),
                    subjectCardInstanceId: Card(references),
                    blockingHeroId: Optional(references, "blockingHero", value => new HeroId(value)),
                    blockingCardInstanceId: Optional(
                        references, "blockingCard", value => new CardInstanceId(value))));
                return;
            }

            // ---- The one-for-one rows.
            var kind = KindOf(type);
            if (kind == MatchLogEntryKind.None)
            {
                return;
            }

            derived.Add(Compose(kind, entry, references, detail, round, turn));
        }

        private static MatchLogEntry Compose(
            MatchLogEntryKind kind,
            RuleEventLogEntry entry,
            Dictionary<string, string> references,
            Dictionary<string, string> detail,
            int round,
            int turn)
        {
            switch (kind)
            {
                case MatchLogEntryKind.FastTravel:
                {
                    var stated = TryReadMove(detail, "fastTravel", out var origin, out var destination);
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Hero(references, entry),
                        subjectCardInstanceId: Card(references),
                        origin: origin,
                        destination: destination,
                        hasOrigin: stated,
                        hasDestination: stated);
                }

                case MatchLogEntryKind.Clear:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Hero(references, entry),
                        subjectCardInstanceId: Card(references),
                        origin: ReadCoordinate(detail, "space", out var hasSpace),
                        hasOrigin: hasSpace);

                case MatchLogEntryKind.MinionDefeated:
                case MatchLogEntryKind.MinionRemoved:
                case MatchLogEntryKind.MinionRemovalPrevented:
                case MatchLogEntryKind.MinionBattleMinionRemoved:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Optional(references, "hero", value => new HeroId(value)),
                        subjectMinionId: Optional(references, "minion", value => new MinionId(value)),
                        subjectTeamId: Optional(references, "team", value => new TeamId(value)));

                case MatchLogEntryKind.DisplacedMinionCorrected:
                {
                    var corrected = TryReadMove(
                        detail, "correction", out var from, out var to);
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Optional(references, "hero", value => new HeroId(value)),
                        subjectMinionId: Optional(references, "minion", value => new MinionId(value)),
                        origin: from,
                        destination: to,
                        hasOrigin: corrected,
                        hasDestination: corrected);
                }

                case MatchLogEntryKind.LevelPurchased:
                case MatchLogEntryKind.CardUpgraded:
                case MatchLogEntryKind.ItemGained:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Hero(references, entry),
                        subjectCardInstanceId: Optional(
                            detail, "card", value => new CardInstanceId(value)),
                        amount: Number(references, "level"));

                case MatchLogEntryKind.PityCoinCollected:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Hero(references, entry),
                        amount: Number(detail, "coins"));

                case MatchLogEntryKind.GameEnded:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectTeamId: Optional(references, "winner", value => new TeamId(value)));

                case MatchLogEntryKind.LanePush:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Optional(references, "hero", value => new HeroId(value)),
                        subjectTeamId: Optional(references, "losingTeam", value => new TeamId(value)));

                case MatchLogEntryKind.CardRevealed:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Hero(references, entry),
                        subjectCardInstanceId: Card(references));

                // Milestone 8.35z61. THE SUBJECT IS THE TARGET, NOT THE ACTOR. On every
                // card-action event `hero` is the acting hero and `target` is the hero the action was
                // about, and this row is about the hero who was defeated and thereby passed over -
                // the attacker is already named by the Defeat's own rows. Reading `hero` here would
                // have printed the attacker's name beside "their card resolved with no action",
                // which is the one sentence in this log that has to name the right player.
                //
                // `card` is deliberately NOT read: on this event it is the ATTACKER's source card,
                // not the card that was eaten. The eaten card rides the event as its own field and
                // is commitment content this channel does not carry - the line CardRevealed draws.
                case MatchLogEntryKind.HeroCardResolvedWithoutAction:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Optional(references, "target", value => new HeroId(value)));

                case MatchLogEntryKind.RoundStarted:
                case MatchLogEntryKind.RoundCompleted:
                case MatchLogEntryKind.TurnStarted:
                    // The turn-slot header ONLY. Its assignment payload names every hero's
                    // committed card before the reveal, and is the contract's own exclusion.
                    return new MatchLogEntry(entry.Sequence, kind, round, turn);

                default:
                    return new MatchLogEntry(
                        entry.Sequence, kind, round, turn,
                        subjectHeroId: Optional(references, "hero", value => new HeroId(value)),
                        subjectCardInstanceId: Card(references));
            }
        }

        private static MatchLogEntryKind KindOf(string type)
        {
            if (type == FastTravelActionCompleted) return MatchLogEntryKind.FastTravel;
            if (type == SkillActionCompleted) return MatchLogEntryKind.SkillUsed;

            // The token's removal, NOT the action's closure. Carrying both would put two lines on
            // one clear: the closure names nothing the removal has not already said, and the
            // removal is the half that names the token and its space. A clear that removed nothing
            // therefore writes no line, which is correct - nothing happened.
            if (type == TokenCleared) return MatchLogEntryKind.Clear;
            if (type == MinionDefeatRecorded) return MatchLogEntryKind.MinionDefeated;
            if (type == MinionRemovalCompleted) return MatchLogEntryKind.MinionRemoved;
            if (type == MinionRemovalPrevented) return MatchLogEntryKind.MinionRemovalPrevented;
            if (type == HeroDefeatRecorded) return MatchLogEntryKind.HeroDefeated;
            if (type == HeroRemovalCompleted) return MatchLogEntryKind.HeroRemoved;
            if (type == HeroRemovalPrevented) return MatchLogEntryKind.HeroRemovalPrevented;
            if (type == HeroUnresolvedCardResolved)
            {
                return MatchLogEntryKind.HeroCardResolvedWithoutAction;
            }

            if (type == HeroRespawned) return MatchLogEntryKind.HeroRespawned;
            if (type == TeamLifeSpent) return MatchLogEntryKind.TeamLifeSpent;
            if (type == TeamEliminated) return MatchLogEntryKind.TeamEliminated;
            if (type == DisplacedMinionCorrectionMoved ||
                type == DisplacedMinionCorrectionPlaced ||
                type == DisplacedMinionCorrectionLeftInPlace)
            {
                return MatchLogEntryKind.DisplacedMinionCorrected;
            }

            if (type == MinionBattleMinionRemoved) return MatchLogEntryKind.MinionBattleMinionRemoved;
            if (type == LanePushRequested || type == ImmediateLanePushResolved)
            {
                return MatchLogEntryKind.LanePush;
            }

            if (type == HeroLevelUpLevelPurchased) return MatchLogEntryKind.LevelPurchased;
            if (type == HeroLevelUpCardUpgraded) return MatchLogEntryKind.CardUpgraded;
            if (type == HeroLevelUpItemGained) return MatchLogEntryKind.ItemGained;
            if (type == PityCoinCollected) return MatchLogEntryKind.PityCoinCollected;
            if (type == NextRoundStarted) return MatchLogEntryKind.RoundStarted;
            if (type == RoundCompleted) return MatchLogEntryKind.RoundCompleted;
            if (type == TurnSlotRecorded) return MatchLogEntryKind.TurnStarted;
            if (type == TurnEnded) return MatchLogEntryKind.TurnEnded;
            if (type == GameEnded) return MatchLogEntryKind.GameEnded;
            if (type == CardRevealed) return MatchLogEntryKind.CardRevealed;
            return MatchLogEntryKind.None;
        }

        // ---- Reading the log's own two structured halves.

        private static Dictionary<string, string> Fields(string composed)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(composed))
            {
                return fields;
            }

            foreach (var part in composed.Split(';'))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = part.Substring(0, separator);
                if (!fields.ContainsKey(key))
                {
                    fields[key] = part.Substring(separator + 1);
                }
            }

            return fields;
        }

        private static bool Text(Dictionary<string, string> fields, string key, out string value) =>
            fields.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value);

        private static int Number(Dictionary<string, string> fields, string key) =>
            Text(fields, key, out var value) &&
            int.TryParse(value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;

        private static string Required(
            Dictionary<string, string> fields, string key, RuleEventLogEntry entry)
        {
            if (Text(fields, key, out var value))
            {
                return value;
            }

            throw new InvalidOperationException(
                "The match log carries '" + entry.EventType + "' but its recorded references name no '" +
                key + "'. The selection contract's rows and the rule-event log's own references have " +
                "drifted apart, and a line that silently said nothing would hide it.");
        }

        private static HeroId Hero(Dictionary<string, string> references, RuleEventLogEntry entry) =>
            new HeroId(Required(references, "hero", entry));

        private static CardInstanceId Card(Dictionary<string, string> references) =>
            Optional(references, "card", value => new CardInstanceId(value));

        private static T Optional<T>(
            Dictionary<string, string> fields, string key, Func<string, T> compose) =>
            Text(fields, key, out var value) ? compose(value) : default;

        private static bool EndsWith(Dictionary<string, string> fields, string key, string suffix) =>
            Text(fields, key, out var value) &&
            value.EndsWith(suffix, StringComparison.Ordinal);

        /// <summary>Reads a <c>key=(q, r)-&gt;(q, r)</c> detail part.</summary>
        private static bool TryReadMove(
            Dictionary<string, string> detail,
            string key,
            out HexCoordinate origin,
            out HexCoordinate destination)
        {
            origin = default;
            destination = default;
            if (!Text(detail, key, out var value))
            {
                return false;
            }

            var arrow = value.IndexOf("->", StringComparison.Ordinal);
            if (arrow < 0)
            {
                return false;
            }

            return HexCoordinateText.TryParse(value.Substring(0, arrow), out origin) &&
                HexCoordinateText.TryParse(value.Substring(arrow + 2), out destination);
        }

        /// <summary>One <c>key=(q, r)</c> detail part.</summary>
        private static HexCoordinate ReadCoordinate(
            Dictionary<string, string> detail, string key, out bool stated)
        {
            if (!Text(detail, key, out var value) ||
                !HexCoordinateText.TryParse(value, out var coordinate))
            {
                stated = false;
                return default;
            }

            stated = true;
            return coordinate;
        }
    }
}

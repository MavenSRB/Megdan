using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.13. Executes one expected-outcome case: stages its setup on an open part of the
    /// official map, plays the card through Director commands, answers each question from the table,
    /// releases the turn and compares every expectation. Format v1 plus the v1.1 additions; the
    /// recipe (<c>Docs/AI/Testing/EXPECTED_OUTCOME_TABLES.md</c>) lists every phrase and how it is
    /// staged or read.
    ///
    /// <para><b>Refuse, never guess.</b> Every phrase is parsed against a closed vocabulary. A phrase
    /// outside it, a card name the hero was not dealt, a placement the map cannot hold, or a question
    /// the answers do not cover ends the case with a message quoting the phrase and the case id.
    /// Answers left unused and a <c>defends-with</c> never asked fail too, because each is a place
    /// where the reader and the engine read the card differently.</para>
    ///
    /// <para><b>No production seam.</b> Heroes, minions, obstacles, hands, discards, Levels, coins and
    /// Life counters are composed through the same initial-state path every staged fixture uses
    /// (<see cref="MatchStagedProgression"/> for Levels and coins). Tokens and items have no staging
    /// entry in that path, so they are committed to the freshly built graphs through the test
    /// assembly's existing internal access (the board's own validated-placement commit and the item
    /// state's box grant), captured as a snapshot and restored into a fresh session, so every command
    /// after that runs on an ordinary restored match.</para>
    /// </summary>
    internal static partial class ExpectedOutcomeExecutor
    {
        internal sealed class Report
        {
            internal Report(List<string> failures, string trace, string skipped)
            {
                Failures = failures;
                Trace = trace;
                Skipped = skipped;
            }

            internal List<string> Failures { get; }
            internal string Trace { get; }

            /// <summary>Why the case was not executed (an open question, a boxed Handicap card), or null.</summary>
            internal string Skipped { get; }
        }

        private sealed class Refusal : Exception
        {
            internal Refusal(string message) : base(message)
            {
            }
        }

        internal static readonly HashSet<string> DispositionKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            "ruling", "engine-defect", "executor-gap", "not-executable", "open-question"
        };

        internal static Report Run(JsonObject document, JsonObject entry, string name, JsonObject disposition = null)
        {
            var run = new CaseRun(document, entry, name, disposition);
            try
            {
                run.Execute();
            }
            catch (Refusal refusal)
            {
                run.Failures.Add(refusal.Message);
            }
            catch (Exception exception)
            {
                run.Failures.Add($"{name}: the executor faulted - {exception.GetType().Name}: {exception.Message}");
                run.Trace.AppendLine(exception.ToString());
            }

            return new Report(run.Failures, run.Trace.ToString(), run.Skipped);
        }

        private static bool IsFamily(PendingDecisionContract contract, PendingDecisionFamilyId family) =>
            contract != null && contract.FamilyId.Equals(family);

        private static readonly Lazy<BoardMapDefinition> Map = new Lazy<BoardMapDefinition>(() =>
            OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap));

        private enum RefKind { Actor, EnemyHero, FriendlyHero, EnemyMinion, FriendlyMinion, Token, Obstacle }

        private sealed class Piece
        {
            internal string Ref;
            internal RefKind Kind;
            internal JsonObject Spec;
            internal Placement Where;
            internal HexCoordinate At;
            internal HeroId Hero;
            internal MinionId Minion;
            internal StandardMinionType MinionType = StandardMinionType.Melee;
            internal TokenInstanceId Token;
            internal TokenDefinition TokenDefinition;
            internal HeroId TokenOwner;
            internal ObstacleId Obstacle;
            internal string ObstacleKind;
            internal string OwnedObstacleKind;
            internal bool BattleZone;
            internal bool IsTerrain => Kind == RefKind.Obstacle && ObstacleKind == "terrain";
            internal bool IsTokenLike => Kind == RefKind.Token || (Kind == RefKind.Obstacle && TokenDefinition != null);
            internal bool IsHero => Kind == RefKind.Actor || Kind == RefKind.EnemyHero || Kind == RefKind.FriendlyHero;
            internal bool IsMinion => Kind == RefKind.EnemyMinion || Kind == RefKind.FriendlyMinion;
        }

        /// <summary>A relative placement, parsed from the closed grammar.</summary>
        private sealed class Placement
        {
            internal string Text;
            internal string Kind; // adjacent | distance | beyond | online
            internal string A = "actor";
            internal string B;
            internal int D;
            internal string Line; // null | straight | off
            internal string Through;
            internal string Prefer;
            internal bool PreferAway;

            /// <summary>A second condition the same space must meet ("... and adjacent to token-1").</summary>
            internal Placement Also;

            internal string Primary => Kind == "beyond" || Kind == "online" ? B : A;

            internal IEnumerable<string> Refs() =>
                new[] { A, B, Through, Prefer }.Where(value => value != null)
                    .Concat(Also?.Refs() ?? Enumerable.Empty<string>());
        }

        private const string RefPattern = "([a-z]+(?:-[a-z]+)*(?:-[0-9]+)?)";

        private static readonly Regex PreferSuffix =
            new Regex("^(.*?)\\s+(toward|away from)\\s+" + RefPattern + "$", RegexOptions.CultureInvariant);

        private static readonly Regex AdjacentPattern = new Regex(
            "^adjacent(?: to " + RefPattern + "(?: and " + RefPattern + ")?)?$", RegexOptions.CultureInvariant);

        private static readonly Regex DistancePattern = new Regex(
            "^distance ([0-9]+)(?: from " + RefPattern + ")?(?: (in a straight line)(?: through " + RefPattern +
            ")?| (off every straight line))?$", RegexOptions.CultureInvariant);

        private static readonly Regex BeyondPattern = new Regex(
            "^distance ([0-9]+) beyond " + RefPattern + " from " + RefPattern + "$", RegexOptions.CultureInvariant);

        private static readonly Regex OnLinePattern = new Regex(
            "^on the straight line through " + RefPattern + " and " + RefPattern + "$", RegexOptions.CultureInvariant);

        private static readonly Regex RefShape = new Regex(
            "^(actor|enemy-hero|friendly-hero|enemy-minion|friendly-minion|token|obstacle)(?:-([0-9]+))?$",
            RegexOptions.CultureInvariant);

        private static readonly Regex SpawnPattern = new Regex(
            "^(?:an )?empty spawn point within distance ([0-9]+) of " + RefPattern + "$", RegexOptions.CultureInvariant);

        private static Placement ParsePlacement(string raw)
        {
            if (raw == null) return null;
            var stripped = Regex.Replace(raw, "\\s*\\([^)]*\\)", string.Empty);
            var single = ParseSingle(raw, Normalize(stripped));
            if (single != null) return single;

            // Two conditions on one space: "<placement> and <placement>".
            var text = Normalize(stripped);
            for (var at = text.IndexOf(" and ", StringComparison.Ordinal); at > 0;
                 at = text.IndexOf(" and ", at + 1, StringComparison.Ordinal))
            {
                var left = ParseSingle(raw, text.Substring(0, at));
                var right = ParsePlacement(text.Substring(at + 5));
                if (left != null && right != null && left.Also == null)
                {
                    left.Also = right;
                    return left;
                }
            }

            return null;
        }

        private static Placement ParseSingle(string raw, string text)
        {
            var placement = new Placement { Text = raw };
            var prefer = PreferSuffix.Match(text);
            if (prefer.Success && !text.StartsWith("on the straight line", StringComparison.Ordinal))
            {
                text = prefer.Groups[1].Value;
                placement.PreferAway = prefer.Groups[2].Value != "toward";
                placement.Prefer = prefer.Groups[3].Value;
            }

            Match match;
            if ((match = AdjacentPattern.Match(text)).Success)
            {
                placement.Kind = "adjacent";
                placement.D = 1;
                if (match.Groups[1].Success) placement.A = match.Groups[1].Value;
                if (match.Groups[2].Success) placement.B = match.Groups[2].Value;
                return placement;
            }

            if ((match = DistancePattern.Match(text)).Success)
            {
                placement.Kind = "distance";
                placement.D = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (match.Groups[2].Success) placement.A = match.Groups[2].Value;
                if (match.Groups[3].Success) placement.Line = "straight";
                if (match.Groups[4].Success) placement.Through = match.Groups[4].Value;
                if (match.Groups[5].Success) placement.Line = "off";
                return placement.D >= 0 ? placement : null;
            }

            if ((match = BeyondPattern.Match(text)).Success)
            {
                placement.Kind = "beyond";
                placement.D = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                placement.B = match.Groups[2].Value;
                placement.A = match.Groups[3].Value;
                return placement.D >= 1 ? placement : null;
            }

            if ((match = OnLinePattern.Match(text)).Success)
            {
                placement.Kind = "online";
                placement.A = match.Groups[1].Value;
                placement.B = match.Groups[2].Value;
                return placement;
            }

            if ((match = SpawnPattern.Match(text)).Success)
            {
                placement.Kind = "spawn";
                placement.D = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                placement.A = match.Groups[2].Value;
                return placement;
            }

            return null;
        }

        private static readonly Lazy<HashSet<HexCoordinate>> SpawnSpaces = new Lazy<HashSet<HexCoordinate>>(() =>
            new HashSet<HexCoordinate>(Map.Value.SpawnPoints.Select(point =>
                Map.Value.Spaces.Single(space => space.Id.Equals(point.SpaceId)).Coordinate)));

        private static string Normalize(string text) =>
            Regex.Replace((text ?? string.Empty).Trim().ToLowerInvariant(), "\\s+", " ");

        /// <summary>
        /// Card names compare without case, punctuation variants or spacing, and a reader's printed
        /// spelling must be the content's: the one bridge this held (9.13, Mortimer's BRAAAAAAAAINS...!
        /// printed with eight a's against a seven-a display name) was retired when 9.14 corrected the
        /// display name, so a spelling the content does not print is an unresolved card, by design.
        /// </summary>
        private static string CardKey(string name) =>
            Regex.Replace((name ?? string.Empty).ToLowerInvariant()
                .Replace('’', '\'').Replace('‘', '\'').Replace('–', '-').Replace('—', '-'),
                "[^a-z0-9']+", " ").Trim();

        private static int Dist(HexCoordinate a, HexCoordinate b) => HexDistanceQuery.Calculate(a, b);

        private static bool Aligned(HexCoordinate a, HexCoordinate b) =>
            !a.Equals(b) && (a.Q == b.Q || a.R == b.R || a.Q + a.R == b.Q + b.R);

        /// <summary><paramref name="m"/> lies strictly between <paramref name="a"/> and <paramref name="b"/> on one straight line.</summary>
        private static bool Between(HexCoordinate a, HexCoordinate m, HexCoordinate b) =>
            Aligned(a, b) && Aligned(a, m) && !m.Equals(b) && Dist(a, m) + Dist(m, b) == Dist(a, b);

        /// <summary>
        /// Whether <paramref name="at"/> lies on the straight line from <paramref name="origin"/> through
        /// <paramref name="toward"/> (or, away, on its continuation behind the origin): "toward X" on a
        /// walk first means along X's file, as a straight-line walk through X does.
        /// </summary>
        private static bool OnFile(HexCoordinate origin, HexCoordinate toward, HexCoordinate at, bool away) =>
            Aligned(origin, toward) && Aligned(origin, at) &&
            (away ? SectorOf(origin, at) == (SectorOf(origin, toward) + 3) % 6 : SectorOf(origin, at) == SectorOf(origin, toward));

        private static int SectorOf(HexCoordinate from, HexCoordinate to)
        {
            var best = 0;
            var bestDistance = int.MaxValue;
            for (var direction = 0; direction < 6; direction++)
            {
                var distance = Dist(from.GetNeighbor((HexDirection)direction), to);
                if (distance < bestDistance)
                {
                    best = direction;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private sealed partial class CaseRun
        {
            internal readonly List<string> Failures = new List<string>();
            internal readonly StringBuilder Trace = new StringBuilder();

            private readonly JsonObject document;
            private readonly JsonObject entry;
            private readonly string name;
            private readonly JsonObject disposition;
            internal string Skipped;

            private RegisteredHero hero;
            private MatchConfiguration configuration;
            private QuickGameSetupState setup;
            private HeroId actor;
            private TeamSide actorSide;
            private readonly List<Piece> pieces = new List<Piece>();
            private readonly Dictionary<string, Piece> byRef = new Dictionary<string, Piece>(StringComparer.Ordinal);
            private CardDefinition caseCard;
            private CardInstance caseInstance;
            private ActionDefinition caseAction;
            private string actionName;
            private bool isHold;
            private bool isDefend;
            private HeroId attacker;
            private CardInstance attackCard;
            private JsonObject followup;
            private HeroId followupHero;
            private CardInstance followupCard;
            private bool followupNextTurn;
            private bool? defendOffered;
            private MatchSessionInitialState initial;
            private MatchSession session;
            private readonly Queue<string> answers = new Queue<string>();
            private readonly Dictionary<string, JsonObject> refSetups = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            private readonly HashSet<string> defendsWithAsked = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<(string Phrase, HeroId Creator, CardInstanceId Card, bool LastTurn)> standing =
                new List<(string, HeroId, CardInstanceId, bool)>();
            private readonly Dictionary<HeroId, List<CardInstanceId>> fillers = new Dictionary<HeroId, List<CardInstanceId>>();
            private readonly Dictionary<HeroId, HashSet<CardInstanceId>> reserved = new Dictionary<HeroId, HashSet<CardInstanceId>>();
            private readonly HashSet<CardInstanceId> heldStanding = new HashSet<CardInstanceId>();

            // What the play did, observed while it ran.
            private int? observedAttack;
            private readonly Dictionary<HeroId, int> observedAttacks = new Dictionary<HeroId, int>();
            private readonly Dictionary<HeroId, int> attackCounts = new Dictionary<HeroId, int>();
            private readonly Dictionary<HeroId, int> maximumMovementOffered = new Dictionary<HeroId, int>();
            private readonly Dictionary<string, int> walked = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly List<HashSet<string>> targetQuestions = new List<HashSet<string>>();
            private readonly List<HashSet<HexCoordinate>> offeredEffectDestinations = new List<HashSet<HexCoordinate>>();
            private bool cardTextFastTravelOffered;
            private bool cardTextFastTravelCommitted;
            private readonly Dictionary<string, HashSet<string>> defenseQuestions = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            private readonly List<(CardDefinition Card, EffectNodeId Node, HashSet<EffectNodeId> Available, List<EffectChoiceCandidate> All)> optionQuestions =
                new List<(CardDefinition, EffectNodeId, HashSet<EffectNodeId>, List<EffectChoiceCandidate>)>();
            private string lastReferenced;
            private int extraConsumed;
            private (string Resolution, HeroId Mover)? fulfilledWalk;

            // The position the play started from.
            private MatchSessionProjection before;
            private MatchSessionProjection afterOriginalAction;
            private MatchSessionProjection atFollowup;
            private int logBaseline;
            private MatchSessionProjection after;
            private MatchSessionProjection afterRound;
            private string minionBattleWinner;

            internal CaseRun(JsonObject document, JsonObject entry, string name, JsonObject disposition)
            {
                this.document = document;
                this.name = name;
                this.disposition = disposition;
                this.entry = entry;

                // A ruling disposition evaluates its override in place of the reader's facts (and
                // answers, where the ruling moves the question); every other kind runs as written.
                if (disposition != null && (disposition.Get("kind") as string) == "ruling")
                {
                    var copy = new JsonObject();
                    foreach (var pair in entry)
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (key == "expect" && disposition.Has("expect-override")) value = disposition.Get("expect-override");
                        if (key == "answers" && disposition.Has("answers-override")) value = disposition.Get("answers-override");
                        copy.Add(new KeyValuePair<string, object>(key, value));
                    }

                    if (!copy.Has("answers") && disposition.Has("answers-override"))
                    {
                        copy.Add(new KeyValuePair<string, object>("answers", disposition.Get("answers-override")));
                    }

                    this.entry = copy;
                }
            }

            private Refusal Refuse(string message) => new Refusal($"{name}: {message}");

            private Refusal Unsupported(string what, string phrase) =>
                Refuse($"unsupported {what} \"{phrase}\"");

            private void Log(string line) => Trace.AppendLine(line);

            /// <summary>
            /// A reader's <c>"unsupported"</c> note names a fact the format could not say. The case's
            /// own facts still run; the note is then reported as a failure of its own, so a case is
            /// never counted as agreeing with a reading the executor did not check.
            /// </summary>
            internal void Execute()
            {
                var kind = disposition?.Get("kind") as string;
                var reference = disposition?.Get("ref") as string ?? string.Empty;
                if (entry.Get("handicap") is bool handicap && handicap)
                {
                    Skipped = "not-executable (rulebook p.6): the p.20 Handicap cards are returned to the box at setup, " +
                        "so this build deals none";
                    return;
                }

                if (kind == "not-executable" && disposition.Get("execute") is bool execute && !execute)
                {
                    Skipped = $"not-executable ({reference}): {disposition.Get("note")}";
                    return;
                }

                if (kind == "open-question")
                {
                    Skipped = $"OPEN QUESTION ({reference}): {disposition.Get("note")}";
                    return;
                }

                try
                {
                    ExecuteTheCase();
                }
                finally
                {
                    // A disposition records the reader's own note; without one it is a failure of its own.
                    if (disposition == null && entry.Get("unsupported") is object declared && declared != null)
                    {
                        Failures.Add($"{name}: DECLARED UNSUPPORTED (not executed): \"{Describe(declared)}\"");
                    }
                }
            }

            private void ExecuteTheCase()
            {
                ReadCase();
                StageAndBuild();
                ReachTheActorsTurn();
                ValidateRuneSlotConsumption();
                var expectations = ReadExpectations();
                var offered = TheOffer(out var offer);
                Log($"offered: {offered}");
                if (isDefend)
                {
                    RunTheDefence(expectations);
                    return;
                }

                var expectedOffered = expectations
                    .Where(value => value.Has("offered") && !value.Has("bullet"))
                    .Select(value => value.Get("offered"))
                    .ToArray();
                foreach (var value in expectedOffered)
                {
                    if (!(value is bool expected)) throw Unsupported("expectation", $"offered: {value}");
                    if (expected != offered)
                    {
                        Failures.Add($"{name}: expected offered {expected.ToString().ToLowerInvariant()}, " +
                            $"the engine {(offered ? "offers" : "does not offer")} {actionName} on {caseCard.DisplayName}" +
                            (offer.HasValue && !offer.Value.IsAvailable
                                ? $" (greyed: {Grey(offer.Value.Unavailability)})"
                                : string.Empty));
                    }
                }

                var consequences = expectations.Where(value => !value.Has("offered") || value.Has("bullet")).ToArray();
                if (followup == null && !entry.Has("checkpoint") && consequences.Length == 0 && answers.Count == 0 && expectedOffered.Length > 0)
                {
                    return;
                }

                var forced = expectedOffered.OfType<bool>().Any(value => !value);
                if (!forced && !offered)
                {
                    if (expectedOffered.Length == 0)
                    {
                        Failures.Add($"{name}: {actionName} on {caseCard.DisplayName} is not offered from this setup" +
                            (offer.HasValue ? $" (greyed: {Grey(offer.Value.Unavailability)})" : " (no such offer)"));
                    }

                    return;
                }

                Capture();
                Play(forced);
                afterOriginalAction = session.Projection;
                if (followup != null) RunFollowup();
                after = session.Projection;
                if (entry.Has("checkpoint"))
                {
                    RunTheRoundEnd(entry.Get("checkpoint") as string == "before-minion-battle",
                        entry.Get("checkpoint") as string == "end-of-turn");
                    after = afterRound;
                }
                LogAfter(after);
                if (consequences.Any(NeedsTheRoundEnd))
                {
                    RunTheRoundEnd();
                }

                foreach (var expectation in consequences)
                {
                    Evaluate(expectation);
                }

                FinalChecks();
            }

            private void FinalChecks()
            {
                // A trailing "decline" of a question the engine never asked (a "choose up to N times" or
                // "may" that had nothing left to act on, so p.10 stops it unasked) declines nothing.
                if (answers.Count > 0 && answers.All(value => Normalize(value) == "decline"))
                {
                    Log($"unasked trailing declines ignored: {answers.Count}");
                    answers.Clear();
                }

                if (answers.Count > 0)
                {
                    Failures.Add($"{name}: answers never asked for: " +
                        string.Join(", ", answers.Select(value => $"\"{value}\"")));
                }

                // A Defense the table stages for an attack it expects not to be offered is a precaution.
                var expectsNoOffer = ReadExpectations().Any(value => value.Has("offered") && !value.Has("bullet") &&
                    value.Get("offered") is bool offeredFact && !offeredFact);
                foreach (var pair in refSetups.Where(pair => pair.Value.Has("defends-with") && !expectsNoOffer))
                {
                    if (!defendsWithAsked.Contains(pair.Key))
                    {
                        Failures.Add($"{name}: setup {pair.Key}.defends-with was never asked - no Defense " +
                            $"question reached {pair.Key}");
                    }
                }
            }

            // ------------------------------------------------------------------ reading the case

            private static readonly HashSet<string> CaseKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "id", "card", "action", "source", "setup", "answers", "expect", "unsupported", "hero",
                "handicap", "note", "notes", "comment", "why", "description", "follow-up", "checkpoint"
            };

            private void ReadCase()
            {
                foreach (var pair in entry)
                {
                    if (!CaseKeys.Contains(pair.Key)) throw Unsupported("case key", pair.Key);
                }


                var heroName = (entry.Get("hero") ?? document.Get("hero")) as string;
                if (string.IsNullOrWhiteSpace(heroName)) throw Refuse("the table names no \"hero\"");
                var matches = QuickGameSetupConfiguration.DealtHeroes.Where(candidate =>
                    string.Equals(candidate.ContentId.Value, heroName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidate.DisplayName, heroName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    candidate.DisplayName.StartsWith(heroName.Trim() + " ", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length != 1) throw Refuse($"unknown hero \"{heroName}\"");
                hero = matches[0];

                var lineup = Lineup();
                configuration = MatchConfiguration.CreateForLineupForInternalTest(
                    MatchFormat.ThreeVersusThreeQuick, lineup,
                    (entry.Get("setup") as JsonObject)?.Get("tie-breaker") is object coin
                        ? ReadCoinSide(coin) : TeamSide.Red);
                setup = QuickGameSetupAuthority.Create(
                    HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, lineup),
                    configuration.Participants, Map.Value, configuration.TieBreakerState);
                var seat = setup.Heroes.Single(value => value.ContentId.Equals(hero.ContentId));
                actor = seat.HeroId;
                actorSide = seat.TeamSide;
                Log($"hero {hero.DisplayName} as {actor.Value} ({actorSide}); line-up " +
                    string.Join(", ", setup.Heroes.Select(value => $"{value.HeroId.Value}:{value.TeamSide}")));

                var cardName = entry.Get("card") as string;
                if (string.IsNullOrWhiteSpace(cardName)) throw Refuse("the case names no \"card\"");
                caseCard = OwnedDefinition(actor, cardName);
                caseInstance = Instance(actor, caseCard);
                if (entry.Has("follow-up"))
                {
                    followup = entry.Get("follow-up") as JsonObject ?? throw Unsupported("follow-up", Describe(entry.Get("follow-up")));
                    foreach (var pair in followup)
                        if (!new[] { "hero", "card", "action", "answers", "offered", "turn" }.Contains(pair.Key))
                            throw Unsupported("follow-up key", pair.Key);
                    if (followup.Has("offered") && !(followup.Get("offered") is bool))
                        throw Unsupported("follow-up offered", Describe(followup.Get("offered")));
                    if (followup.Has("answers") && (!(followup.Get("answers") is List<object> followupAnswers) ||
                        followupAnswers.Any(answer => !(answer is string))))
                        throw Unsupported("follow-up answers", Describe(followup.Get("answers")));
                    if (followup.Has("turn") && !(followup.Get("turn") is string nextTurn && nextTurn == "next"))
                        throw Unsupported("follow-up turn", Describe(followup.Get("turn")));
                    followupNextTurn = (string)followup.Get("turn") == "next";
                    followupHero = HeroOfRef(followup.Get("hero") as string ?? string.Empty);
                    if (!followupHero.IsDefined || followupHero.Equals(actor) && !followupNextTurn)
                        throw Refuse("follow-up must name another hero this turn, or a hero next turn");
                    followupCard = Instance(followupHero, OwnedDefinition(followupHero, followup.Get("card") as string));
                }

                actionName = entry.Get("action") as string;
                switch (actionName)
                {
                    case "Skill": caseAction = PrimaryOf(ActionKind.Skill); break;
                    case "Attack": caseAction = PrimaryOf(ActionKind.Attack); break;
                    case "Movement":
                        caseAction = caseCard.ActionDefinitions
                            .Where(action => action.Kind == ActionKind.Movement)
                            .OrderBy(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary ? 0 : 1)
                            .FirstOrDefault();
                        break;
                    case "Hold": isHold = true; break;
                    case "Defend":
                    case "Defense": isDefend = true; caseAction = PrimaryOf(ActionKind.Defense); break;
                    case "Ultimate":
                        throw Refuse($"unsupported action \"Ultimate\" on \"{cardName}\": an Ultimate prints no " +
                            "action of its own, so format v1 cannot name the play that uses it - play the card " +
                            "that triggers it with setup.actor.ultimate true");
                    default: throw Unsupported("action", actionName ?? "(none)");
                }

                if (entry.Has("checkpoint") && (isDefend || !(entry.Get("checkpoint") is string checkpoint) ||
                    checkpoint != "before-minion-battle" && checkpoint != "end-of-turn"))
                    throw Refuse("checkpoint must be before-minion-battle or end-of-turn on a non-Defense case");

                if (!isHold && !isDefend && caseAction == null)
                {
                    throw Refuse($"\"{caseCard.DisplayName}\" prints no {actionName} action");
                }

                var answerList = entry.Get("answers");
                if (answerList != null)
                {
                    if (!(answerList is List<object> list)) throw Unsupported("answers", Describe(answerList));
                    foreach (var answer in list)
                    {
                        if (!(answer is string text)) throw Unsupported("answer", Describe(answer));
                        answers.Enqueue(text.Trim());
                    }
                }
            }

            private string Grey(ActionUnavailability unavailability) =>
                unavailability.IsStated
                    ? $"{unavailability.Kind} {unavailability.BlockedActivity} by " +
                        (unavailability.BlockingHeroId.IsDefined ? RefOfHero(unavailability.BlockingHeroId) : "a standing rule")
                    : "the offer judges it can do nothing here";

            /// <summary>The card's printed action of that kind: its primary, else its secondary.</summary>
            /// <summary>
            /// The actor with the next dealt heroes. An enemy seat whose table says it defends with a
            /// Defense value is given, in roster order, the first other dealt hero whose printed deck
            /// holds a card with that Defense, so a value read off the card is always stageable.
            /// </summary>
            private HeroLineup Lineup()
            {
                var spec = entry.Get("setup") as JsonObject ?? new JsonObject();
                var seats = new RegisteredHero?[3];
                var friends = new RegisteredHero?[2];
                var assigned = new HashSet<string>(StringComparer.Ordinal) { hero.ContentId.Value };
                foreach (var side in new[] { "enemy-hero-", "friendly-hero-" })
                {
                    var selected = side == "enemy-hero-" ? seats : friends;
                    for (var index = 0; index < selected.Length; index++)
                    {
                        if (!((spec.Get(side + (index + 1)) as JsonObject)?.Get("hero") is string requested)) continue;
                        var matches = QuickGameSetupConfiguration.DealtHeroes.Where(candidate =>
                            string.Equals(candidate.ContentId.Value, requested, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(candidate.DisplayName, requested, StringComparison.OrdinalIgnoreCase) ||
                            candidate.DisplayName.StartsWith(requested + " ", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (matches.Length != 1 || !assigned.Add(matches[0].ContentId.Value))
                            throw Refuse("unknown or duplicate staged hero: " + requested);
                        selected[index] = matches[0];
                    }
                }
                if (spec.Get("attack") is JsonObject attackSpec)
                {
                    var attackRef = Normalize(attackSpec.Get("by") as string ?? string.Empty);
                    var shape = RefShape.Match(attackRef);
                    if (!shape.Success || shape.Groups[1].Value != "enemy-hero" ||
                        !int.TryParse(shape.Groups[2].Value, out var seat) || seat < 1 || seat > 3)
                        throw Unsupported("setup", "attack.by: " + attackRef);
                    var printed = IntOf(attackSpec.Get("value"), "attack.value");
                    if (attackSpec.Has("card") && !seats[seat - 1].HasValue)
                        throw Refuse("setup attack.card requires the attacker's named hero in setup " + attackRef + ".hero");
                    var chosen = seats[seat - 1] ?? QuickGameSetupConfiguration.DealtHeroes.FirstOrDefault(candidate =>
                    {
                        if (assigned.Contains(candidate.ContentId.Value)) return false;
                        DefenseValues(candidate); // Populates the shared printed-value inventory.
                        return AttacksByHero[candidate.ContentId.Value].Any(attack => attack.Value == printed &&
                            (!(attackSpec.Get("ranged") is bool ranged) || attack.Ranged == ranged));
                    });
                    if (!chosen.ContentId.IsDefined) throw Refuse("no dealt hero has the specified ranged/Attack value");
                    seats[seat - 1] = chosen;
                    assigned.Add(chosen.ContentId.Value);
                }
                for (var seat = 1; seat <= 3; seat++)
                {
                    if (seats[seat - 1].HasValue) continue;
                    if (!((spec.Get($"enemy-hero-{seat}") as JsonObject)?.Get("defends-with") is double value)) continue;
                    var wanted = (int)value;
                    var chosen = QuickGameSetupConfiguration.DealtHeroes.FirstOrDefault(candidate =>
                        !assigned.Contains(candidate.ContentId.Value) && DefenseValues(candidate).Contains(wanted));
                    if (!chosen.ContentId.IsDefined)
                    {
                        throw Refuse($"setup enemy-hero-{seat}.defends-with {wanted}: no dealt hero's deck prints that Defense");
                    }

                    seats[seat - 1] = chosen;
                    assigned.Add(chosen.ContentId.Value);
                }

                var others = new Queue<RegisteredHero>(QuickGameSetupConfiguration.DealtHeroes
                    .Where(candidate => !assigned.Contains(candidate.ContentId.Value)));
                var red = new[] { hero }.Concat(friends.Select(value => value ?? others.Dequeue())).ToArray();
                var blue = seats.Select(value => value ?? others.Dequeue()).ToArray();
                return new HeroLineup(red, blue);
            }

            private static readonly Dictionary<string, HashSet<int>> DefenseByHero =
                new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            private static readonly Dictionary<string, HashSet<(int Value, bool Ranged)>> AttacksByHero =
                new Dictionary<string, HashSet<(int Value, bool Ranged)>>(StringComparer.Ordinal);

            private static HashSet<int> DefenseValues(RegisteredHero candidate)
            {
                lock (DefenseByHero)
                {
                    if (DefenseByHero.TryGetValue(candidate.ContentId.Value, out var known)) return known;
                    var dealt = QuickGameSetupConfiguration.DealtHeroes.ToList();
                    for (var start = 0; start < dealt.Count; start += 6)
                    {
                        var six = Enumerable.Range(0, 6).Select(offset => dealt[(start + offset) % dealt.Count]).ToArray();
                        var probe = MatchConfiguration.CreateForLineupForInternalTest(
                            MatchFormat.ThreeVersusThreeQuick, new HeroLineup(six.Take(3), six.Skip(3)), TeamSide.Red);
                        foreach (var member in six)
                        {
                            var values = new HashSet<int>(probe.CardInstances
                                .Where(card => card.DefinitionReference.DefinitionId.Value.StartsWith(
                                    member.ContentId.Value + "-", StringComparison.Ordinal))
                                .Select(card => probe.CardDefinitions.Single(definition => definition.Reference.Equals(card.DefinitionReference)))
                                .Where(definition => !definition.Progression.IsUltimate)
                                .SelectMany(definition => definition.ActionDefinitions)
                                .Where(action => action.Kind == ActionKind.Defense)
                                .SelectMany(action => action.BaseValues)
                                .Where(item => item.Channel == PrintedValueChannel.Defense)
                                .Select(item => item.Amount));
                            DefenseByHero[member.ContentId.Value] = values;
                            AttacksByHero[member.ContentId.Value] = new HashSet<(int, bool)>(probe.CardInstances
                                .Where(card => card.DefinitionReference.DefinitionId.Value.StartsWith(
                                    member.ContentId.Value + "-", StringComparison.Ordinal))
                                .Select(card => probe.CardDefinitions.Single(definition => definition.Reference.Equals(card.DefinitionReference)))
                                .SelectMany(definition => definition.ActionDefinitions)
                                .Where(action => action.Kind == ActionKind.Attack && action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                                .SelectMany(action => action.BaseValues.Where(item => item.Channel == PrintedValueChannel.Attack)
                                    .Select(item => (item.Amount, action.AttackRange == AttackRangeKind.Ranged))));
                        }
                    }

                    return DefenseByHero[candidate.ContentId.Value];
                }
            }

            private ActionDefinition PrimaryOf(ActionKind kind) =>
                caseCard.ActionDefinitions
                    .Where(action => action.Kind == kind && (action.Origin == ActionDefinitionOrigin.PrintedPrimary ||
                        action.Origin == ActionDefinitionOrigin.PrintedSecondary))
                    .OrderBy(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary ? 0 : 1)
                    .FirstOrDefault();

            private IEnumerable<CardDefinition> OwnedDefinitions(HeroId owner) =>
                configuration.CardInstances
                    .Where(card => card.OwnerId.Equals(owner))
                    .Select(card => configuration.CardDefinitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)))
                    .GroupBy(definition => definition.Reference)
                    .Select(group => group.First());

            private CardDefinition OwnedDefinition(HeroId owner, string cardName)
            {
                var key = CardKey(cardName);
                var found = OwnedDefinitions(owner).Where(definition => CardKey(definition.DisplayName) == key).ToArray();
                if (found.Length == 1) return found[0];
                var ownerName = setup.Heroes.Single(value => value.HeroId.Equals(owner)).DisplayName;
                throw Refuse($"unknown card name \"{cardName}\" for {ownerName}; the dealt cards are " +
                    string.Join(", ", OwnedDefinitions(owner).Select(definition => $"\"{definition.DisplayName}\"")));
            }

            private CardInstance Instance(HeroId owner, CardDefinition definition) =>
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(owner) && card.DefinitionReference.Equals(definition.Reference));

            private static string Describe(object value)
            {
                switch (value)
                {
                    case null: return "null";
                    case string text: return text;
                    case bool flag: return flag ? "true" : "false";
                    case double number: return number.ToString(CultureInfo.InvariantCulture);
                    case JsonObject json: return "{" + string.Join(", ", json.Select(pair => $"{pair.Key}: {Describe(pair.Value)}")) + "}";
                    case List<object> list: return "[" + string.Join(", ", list.Select(Describe)) + "]";
                    default: return value.ToString();
                }
            }

            private int IntOf(object value, string phrase)
            {
                if (value is double number && Math.Abs(number - Math.Round(number)) < 1e-9) return (int)Math.Round(number);
                throw Unsupported("number", $"{phrase}: {Describe(value)}");
            }

            private List<string> Names(object value, string phrase)
            {
                if (!(value is List<object> list) || list.Any(item => !(item is string)))
                {
                    throw Unsupported("card list", $"{phrase}: {Describe(value)}");
                }

                return list.Cast<string>().ToList();
            }

            // ------------------------------------------------------------------ staging

            private static readonly HashSet<string> SetupKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "actor", "pieces", "standing", "hand", "discard", "team-lives", "enemy-team-lives", "attack",
                "minions-in-lane", "empty-minion-spawns", "reserve-minions", "rune-slots", "tie-breaker"
            };

            private static readonly HashSet<string> ActorKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "level", "coins", "items", "ultimate", "battle-zone", "hand", "discard"
            };

            private static readonly HashSet<string> PieceKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "ref", "where", "kind", "owner", "type", "battle-zone", "identity-kind", "push-zone"
            };

            private static readonly HashSet<string> RefSetupKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "defends-with", "hand", "discard", "coins", "hero", "items"
            };

            private void StageAndBuild()
            {
                var spec = entry.Get("setup") as JsonObject ?? new JsonObject();
                if (entry.Has("setup") && !(entry.Get("setup") is JsonObject)) throw Unsupported("setup", Describe(entry.Get("setup")));
                var actorSpec = spec.Get("actor") as JsonObject ?? new JsonObject();
                ValidateRuneSlotDeclaration();
                foreach (var pair in spec)
                {
                    if (SetupKeys.Contains(pair.Key)) continue;
                    var shape = RefShape.Match(pair.Key);
                    if (!shape.Success || pair.Key == "actor" || !(pair.Value is JsonObject refSpec))
                    {
                        throw Unsupported("setup key", pair.Key);
                    }

                    foreach (var inner in refSpec)
                    {
                        if (!RefSetupKeys.Contains(inner.Key)) throw Unsupported("setup key", $"{pair.Key}.{inner.Key}");
                    }

                    refSetups[pair.Key] = refSpec;
                }

                foreach (var pair in actorSpec)
                {
                    if (!ActorKeys.Contains(pair.Key)) throw Unsupported("setup key", $"actor.{pair.Key}");
                }

                if (spec.Has("minions-in-lane"))
                {
                    throw Refuse($"unsupported setup \"minions-in-lane: {Describe(spec.Get("minions-in-lane"))}\": the " +
                        "staged board holds only the minions a table places; place them with \"battle-zone\" instead");
                }

                if (isDefend != spec.Has("attack"))
                {
                    throw Refuse(isDefend
                        ? $"action \"{actionName}\" needs setup \"attack\": {{ \"by\": <enemy hero>, \"value\": N }}"
                        : "unsupported setup key \"attack\" outside a Defend case");
                }

                // The pieces, in the order the table lists them.
                var actorPiece = new Piece { Ref = "actor", Kind = RefKind.Actor, Hero = actor, Spec = actorSpec };
                actorPiece.BattleZone = actorSpec.Get("battle-zone") is bool actorZone && actorZone;
                pieces.Add(actorPiece);
                byRef["actor"] = actorPiece;
                var pieceList = spec.Get("pieces");
                if (pieceList != null && !(pieceList is List<object>)) throw Unsupported("pieces", Describe(pieceList));
                foreach (var item in (pieceList as List<object>) ?? new List<object>())
                {
                    if (!(item is JsonObject pieceSpec)) throw Unsupported("piece", Describe(item));
                    foreach (var pair in pieceSpec)
                    {
                        if (!PieceKeys.Contains(pair.Key)) throw Unsupported("piece key", pair.Key);
                    }

                    var reference = pieceSpec.Get("ref") as string;
                    var shape = RefShape.Match(reference ?? string.Empty);
                    if (!shape.Success || reference == "actor" || !shape.Groups[2].Success)
                    {
                        throw Unsupported("reference", reference ?? "(none)");
                    }

                    if (byRef.ContainsKey(reference)) throw Refuse($"reference \"{reference}\" is placed twice");
                    var piece = new Piece { Ref = reference, Spec = pieceSpec };
                    switch (shape.Groups[1].Value)
                    {
                        case "enemy-hero": piece.Kind = RefKind.EnemyHero; break;
                        case "friendly-hero": piece.Kind = RefKind.FriendlyHero; break;
                        case "enemy-minion": piece.Kind = RefKind.EnemyMinion; break;
                        case "friendly-minion": piece.Kind = RefKind.FriendlyMinion; break;
                        case "token": piece.Kind = RefKind.Token; break;
                        default: piece.Kind = RefKind.Obstacle; break;
                    }

                    var where = pieceSpec.Get("where") as string;
                    if (where == null) throw Refuse($"piece \"{reference}\" has no \"where\"");
                    piece.Where = ParsePlacement(where) ?? throw Unsupported("placement", where);
                    foreach (var named in piece.Where.Refs())
                    {
                        if (!byRef.ContainsKey(named))
                        {
                            throw Refuse($"unresolvable placement \"{where}\": \"{named}\" is not placed before \"{reference}\"");
                        }
                    }

                    piece.BattleZone = pieceSpec.Get("battle-zone") is bool zone && zone;
                    ReadPieceKind(piece);
                    if (pieceSpec.Has("push-zone") &&
                        (!piece.IsHero || !(pieceSpec.Get("push-zone") is string relation) ||
                         relation != "same" && relation != "cross" ||
                         piece.Where.Kind != "adjacent" || piece.Where.A != "actor"))
                        throw Unsupported("push-zone setup", Describe(pieceSpec.Get("push-zone")));
                    pieces.Add(piece);
                    byRef[reference] = piece;
                }

                ReadSpawnStaging(spec);
                AssignIdentities();
                StageCards(spec, actorSpec);
                CollectSpawnNeeds();
                PlaceEverything();
                Build(spec, actorSpec);
                VerifySpawnStaging();
            }

            private void CollectSpawnNeeds()
            {
                var phrases = answers.Select(answer => Regex.Match(answer, "^(?:space|place):\\s*(.+)$",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    .Where(match => match.Success).Select(match => match.Groups[1].Value)
                    .Concat(((entry.Get("expect") as List<object>) ?? new List<object>()).OfType<JsonObject>()
                        .SelectMany(fact => new[] { fact.Get("at"), fact.Get("pushed") }).OfType<string>());
                foreach (var phrase in phrases)
                {
                    for (var placement = ParsePlacement(phrase); placement != null; placement = placement.Also)
                    {
                        if (placement.Kind == "spawn" && placement.A == "actor") spawnNeeds.Add(placement.D);
                    }
                }
            }

            private void ReadPieceKind(Piece piece)
            {
                var kind = piece.Spec.Get("kind") as string;
                var owner = piece.Spec.Get("owner") as string;
                var type = piece.Spec.Get("type") as string;
                if (piece.Spec.Has("identity-kind") &&
                    (piece.Kind != RefKind.Obstacle || Normalize(kind ?? "") != "owned"))
                    throw Unsupported("piece key", piece.Ref + ".identity-kind");
                if (piece.Spec.Has("kind") && kind == null) throw Unsupported("piece kind", Describe(piece.Spec.Get("kind")));
                if (type != null && !piece.IsMinion) throw Unsupported("piece key", $"{piece.Ref}.type");
                if (piece.IsMinion)
                {
                    if (kind != null) throw Unsupported("piece key", $"{piece.Ref}.kind");
                    switch (Normalize(type ?? "melee"))
                    {
                        case "melee": piece.MinionType = StandardMinionType.Melee; break;
                        case "ranged": piece.MinionType = StandardMinionType.Ranged; break;
                        case "heavy": piece.MinionType = StandardMinionType.Heavy; break;
                        default: throw Unsupported("minion type", type);
                    }
                }

                if (piece.IsHero && (kind != null || owner != null))
                {
                    throw Unsupported("piece key", $"{piece.Ref}.{(kind != null ? "kind" : "owner")}");
                }

                if (piece.Kind == RefKind.Token)
                {
                    if (kind == null) throw Refuse($"token \"{piece.Ref}\" names no \"kind\"");
                    piece.TokenDefinition = TokenKind(kind);
                    piece.TokenOwner = OwnerOf(owner ?? "actor", piece.Ref);
                }

                if (piece.Kind == RefKind.Obstacle)
                {
                    piece.ObstacleKind = kind == null ? null : Normalize(kind);
                    switch (piece.ObstacleKind)
                    {
                        case "owned":
                            piece.OwnedObstacleKind = piece.Spec.Get("identity-kind") as string;
                            if (string.IsNullOrWhiteSpace(piece.OwnedObstacleKind))
                                throw Refuse("owned obstacle requires a nonempty identity-kind");
                            piece.TokenOwner = OwnerOf(owner ?? "actor", piece.Ref);
                            break;
                        case null:
                        case "terrain":
                            if (owner != null) throw Unsupported("piece key", $"{piece.Ref}.owner");
                            break;
                        case "rock":
                        case "tree":
                            piece.TokenDefinition = TokenKind(piece.ObstacleKind);
                            piece.TokenOwner = OwnerOf(owner ?? "actor", piece.Ref);
                            break;
                        default:
                            throw Unsupported("obstacle kind", kind);
                    }
                }
                else if (owner != null && piece.Kind != RefKind.Token)
                {
                    throw Unsupported("piece key", $"{piece.Ref}.owner");
                }
            }

            private HeroId OwnerOf(string owner, string reference)
            {
                switch (Normalize(owner))
                {
                    case "actor": return actor;
                    case "enemy": return setup.Heroes.First(value => value.TeamSide != actorSide).HeroId;
                    case "friendly":
                        return setup.Heroes.First(value => value.TeamSide == actorSide && !value.HeroId.Equals(actor)).HeroId;
                    default: throw Unsupported("token owner", $"{reference}.owner: {owner}");
                }
            }

            private IEnumerable<TokenDefinition> AllTokens() =>
                configuration.ContentCatalogs.Components.RegisteredTokens
                    .Select(reference => configuration.ContentCatalogs.Components.LoadToken(reference));

            private static string TokenKey(string text)
            {
                var key = CardKey(text);
                return key.EndsWith(" token", StringComparison.Ordinal) ? key.Substring(0, key.Length - 6) : key;
            }

            private TokenDefinition TokenKind(string kind)
            {
                var key = TokenKey(kind);
                var found = AllTokens()
                    .Where(token => TokenKey(token.DisplayName) == key || TokenKey(token.Id.Value) == key)
                    .ToArray();
                if (found.Length == 1) return found[0];
                throw Refuse($"unknown token kind \"{kind}\"; the catalog's kinds are " +
                    string.Join(", ", AllTokens().Select(token => $"\"{token.DisplayName}\"").Distinct()));
            }

            private void AssignIdentities()
            {
                var enemies = setup.Heroes.Where(value => value.TeamSide != actorSide).ToArray();
                var friends = setup.Heroes.Where(value => value.TeamSide == actorSide && !value.HeroId.Equals(actor)).ToArray();
                var usedMinions = new HashSet<string>(StringComparer.Ordinal);
                foreach (var piece in pieces.Skip(1))
                {
                    var index = int.Parse(RefShape.Match(piece.Ref).Groups[2].Value, CultureInfo.InvariantCulture);
                    switch (piece.Kind)
                    {
                        case RefKind.EnemyHero:
                        case RefKind.FriendlyHero:
                            var side = piece.Kind == RefKind.EnemyHero ? enemies : friends;
                            if (index < 1 || index > side.Length)
                            {
                                throw Refuse($"reference \"{piece.Ref}\": the 3v3 line-up seats only {side.Length} " +
                                    (piece.Kind == RefKind.EnemyHero ? "enemy heroes" : "friendly heroes besides the actor"));
                            }

                            piece.Hero = side[index - 1].HeroId;
                            break;
                        case RefKind.EnemyMinion:
                        case RefKind.FriendlyMinion:
                            var wanted = piece.Kind == RefKind.EnemyMinion ? Opposite(actorSide) : actorSide;
                            var minion = setup.Minions.FirstOrDefault(value => value.TeamSide == wanted &&
                                value.MinionType == piece.MinionType && usedMinions.Add(value.MinionId.Value));
                            if (minion == null)
                            {
                                throw Refuse($"reference \"{piece.Ref}\": the official setup places no further " +
                                    $"{wanted} {piece.MinionType} minion to stage");
                            }

                            piece.Minion = minion.MinionId;
                            break;
                        case RefKind.Token:
                            piece.Token = new TokenInstanceId("table-" + piece.Ref);
                            break;
                        case RefKind.Obstacle:
                            if (piece.TokenDefinition != null) piece.Token = new TokenInstanceId("table-" + piece.Ref);
                            else if (piece.OwnedObstacleKind != null)
                                piece.Obstacle = OwnedObstacleIdentity.For(piece.OwnedObstacleKind, piece.TokenOwner);
                            else if (!piece.IsTerrain) piece.Obstacle = new ObstacleId("table-" + piece.Ref);
                            break;
                    }
                }
            }

            private static TeamSide Opposite(TeamSide side) => side == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;

            private string RefOfHero(HeroId heroId)
            {
                var piece = pieces.FirstOrDefault(value => value.IsHero && value.Hero.Equals(heroId));
                if (piece != null) return piece.Ref;
                var seat = setup.Heroes.First(value => value.HeroId.Equals(heroId));
                var list = setup.Heroes.Where(value => value.TeamSide == seat.TeamSide &&
                    (seat.TeamSide != actorSide || !value.HeroId.Equals(actor))).ToList();
                return (seat.TeamSide == actorSide ? "friendly-hero-" : "enemy-hero-") +
                    (list.FindIndex(value => value.HeroId.Equals(heroId)) + 1);
            }

            private HeroId HeroOfRef(string reference)
            {
                if (reference == "actor") return actor;
                if (byRef.TryGetValue(reference, out var piece) && piece.IsHero) return piece.Hero;
                var shape = RefShape.Match(reference);
                if (!shape.Success || !shape.Groups[2].Success) return default;
                var index = int.Parse(shape.Groups[2].Value, CultureInfo.InvariantCulture);
                var list = shape.Groups[1].Value == "enemy-hero"
                    ? setup.Heroes.Where(value => value.TeamSide != actorSide).ToArray()
                    : shape.Groups[1].Value == "friendly-hero"
                        ? setup.Heroes.Where(value => value.TeamSide == actorSide && !value.HeroId.Equals(actor)).ToArray()
                        : Array.Empty<QuickGameHeroSetupState>();
                return index >= 1 && index <= list.Length ? list[index - 1].HeroId : default;
            }

            // ---- hands, discards, Levels

            private void StageCards(JsonObject spec, JsonObject actorSpec)
            {
                // The standing plays first: their cards must be in hand to be committed.
                var standingList = spec.Get("standing");
                if (standingList != null && !(standingList is List<object>)) throw Unsupported("standing", Describe(standingList));
                var standingPattern = new Regex("^" + RefPattern + " played (.+) (last turn|this turn)( using Hold)?$", RegexOptions.CultureInvariant);
                foreach (var item in (standingList as List<object>) ?? new List<object>())
                {
                    var phrase = item as string ?? throw Unsupported("standing", Describe(item));
                    var match = standingPattern.Match(phrase.Trim());
                    if (!match.Success) throw Unsupported("standing", phrase);
                    var who = match.Groups[1].Value;
                    var creator = who == "actor" ? actor : HeroOfRef(who);
                    if (!creator.IsDefined) throw Unsupported("standing", phrase);
                    var lastTurn = match.Groups[3].Value == "last turn";
                    if (lastTurn && !creator.Equals(actor))
                    {
                        throw Refuse($"unsupported standing \"{phrase}\": only the actor's last turn can be staged");
                    }

                    if (!lastTurn && creator.Equals(actor))
                    {
                        throw Refuse($"unsupported standing \"{phrase}\": the actor plays only the case card this turn");
                    }

                    var definition = OwnedDefinition(creator, match.Groups[2].Value.Trim());
                    standing.Add((phrase, creator, Instance(creator, definition).Id, lastTurn));
                    if (match.Groups[4].Success) heldStanding.Add(Instance(creator, definition).Id);
                }

                var turnsBefore = standing.Any(value => value.LastTurn) ? 1 : 0;
                if (isDefend) ReadTheAttack(spec.Get("attack"));

                // The actor.
                var actorHand = spec.Get("hand") ?? actorSpec.Get("hand");
                var actorDiscard = spec.Get("discard") ?? actorSpec.Get("discard");
                var actorNeeds = new List<CardInstance> { caseInstance };
                if (followupCard != null && followupHero.Equals(actor)) actorNeeds.Add(followupCard);
                actorNeeds.AddRange(standing.Where(value => value.Creator.Equals(actor))
                    .Select(value => configuration.CardInstances.Single(card => card.Id.Equals(value.Card))));
                StageHand(actor, "actor", actorHand == null ? null : Names(actorHand, "hand"),
                    actorDiscard == null ? null : Names(actorDiscard, "discard"), actorNeeds,
                    isDefend ? turnsBefore + 1 : followupNextTurn ? 1 : 0, null);

                StageItems(actor, "actor", actorSpec);

                // Everyone else: their hand, their discard, their Defense card, and one card to commit
                // for each turn up to and including the actor's.
                foreach (var seat in setup.Heroes.Where(value => !value.HeroId.Equals(actor)))
                {
                    var reference = RefOfHero(seat.HeroId);
                    refSetups.TryGetValue(reference, out var refSpec);
                    var needs = standing.Where(value => value.Creator.Equals(seat.HeroId))
                        .Select(value => configuration.CardInstances.Single(card => card.Id.Equals(value.Card)))
                        .ToList();
                    if (isDefend && seat.HeroId.Equals(attacker)) needs.Add(attackCard);
                    if (followupCard != null && seat.HeroId.Equals(followupHero)) needs.Add(followupCard);
                    StageHand(seat.HeroId, reference,
                        refSpec?.Get("hand") == null && refSpec?.Has("hand") != true ? null : Names(refSpec.Get("hand"), reference + ".hand"),
                        refSpec?.Get("discard") == null ? null : Names(refSpec.Get("discard"), reference + ".discard"),
                        needs, turnsBefore + (followupNextTurn ? 2 : 1) - needs.Count, refSpec?.Get("defends-with"));
                    if (refSpec != null) StageItems(seat.HeroId, reference, refSpec);
                }

                // Levels: p.15's eighth Level is the Ultimate's (rulebook p.16).
                var level = actorSpec.Get("level");
                var levelValue = level == null ? 1 : IntOf(level, "actor.level");
                if (levelValue < HeroLevel.Minimum || levelValue > HeroLevel.Maximum)
                {
                    throw Unsupported("setup", $"actor.level: {levelValue}");
                }

                var ultimate = actorSpec.Get("ultimate");
                if (ultimate != null && !(ultimate is bool)) throw Unsupported("setup", $"actor.ultimate: {Describe(ultimate)}");
                if ((ultimate is bool unlocked && unlocked) || levelValue >= HeroLevel.Maximum)
                {
                    var card = configuration.CardInstances.FirstOrDefault(instance => instance.OwnerId.Equals(actor) &&
                        DefinitionOf(instance).Progression.IsUltimate);
                    if (card == null || !CardLifecycleAuthority.TryUnlockUltimateFromDeck(card))
                    {
                        throw Refuse("setup actor.ultimate: this hero has no Ultimate in the deck to unlock");
                    }
                }
            }

            /// <summary>
            /// Stage real set-aside cards for either the actor or another named hero.
            /// </summary>
            private void StageItems(HeroId owner, string reference, JsonObject itemSpec)
            {
                var items = itemSpec.Get("items") == null ? new List<string>() : Names(itemSpec.Get("items"), reference + ".items");
                foreach (var item in items)
                {
                    var match = Regex.Match(Normalize(item), "^(attack|defense|initiative|movement|range|radius) \\+1$");
                    var namedCard = item.StartsWith("card: ", StringComparison.OrdinalIgnoreCase)
                        ? item.Substring("card: ".Length) : null;
                    if (!match.Success && namedCard == null) throw Unsupported("item", item);
                    var icon = match.Success
                        ? (PrintedValueChannel)Enum.Parse(typeof(PrintedValueChannel), match.Groups[1].Value, true)
                        : PrintedValueChannel.None;
                    var card = configuration.CardInstances
                        .Where(value => value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Deck &&
                            (namedCard != null
                                ? CardKey(DefinitionOf(value).DisplayName) == CardKey(namedCard) &&
                                  DefinitionOf(value).Progression.ItemIcon != PrintedValueChannel.None
                                : DefinitionOf(value).Progression.ItemIcon == icon))
                        .OrderBy(value => value.Id.Value, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (card == null || !CardLifecycleAuthority.TrySetAsideFromDeck(card))
                    {
                        throw Refuse($"setup {reference}.items \"{item}\": no matching item card remains in " +
                            $"{reference}'s deck (rulebook p.16 items are set-aside cards)");
                    }

                    itemCards.Add((item, card));
                }
            }

            /// <summary>
            /// v1.1 item 18. Choose a real printed Attack with the requested base value.
            /// Prefer a non-ranged primary with no card text where one is available.
            /// </summary>
            private void ReadTheAttack(object value)
            {
                var spec = value as JsonObject ?? throw Unsupported("setup", $"attack: {Describe(value)}");
                foreach (var pair in spec)
                {
                    if (pair.Key != "by" && pair.Key != "value" && pair.Key != "ranged" && pair.Key != "card")
                        throw Unsupported("setup key", $"attack.{pair.Key}");
                }
                if (spec.Has("ranged") && !(spec.Get("ranged") is bool)) throw Unsupported("setup", "attack.ranged must be boolean");
                var namedAttack = spec.Get("card") as string;
                if (spec.Has("card") && string.IsNullOrWhiteSpace(namedAttack))
                    throw Unsupported("setup", "attack.card must be a printed card name");

                var by = spec.Get("by") as string ?? throw Unsupported("setup", $"attack: {Describe(value)}");
                attacker = HeroOfRef(Normalize(by));
                if (!attacker.IsDefined || setup.Heroes.Single(seat => seat.HeroId.Equals(attacker)).TeamSide == actorSide)
                {
                    throw Unsupported("setup", $"attack.by: {by}");
                }

                var printed = IntOf(spec.Get("value"), "attack.value");
                attackCard = configuration.CardInstances
                    .Where(card => card.OwnerId.Equals(attacker) && !DefinitionOf(card).Progression.IsUltimate)
                    .Select(card => (card, action: DefinitionOf(card).ActionDefinitions.FirstOrDefault(action =>
                        action.Kind == ActionKind.Attack && action.Origin == ActionDefinitionOrigin.PrintedPrimary)))
                    .Where(pair => pair.action != null && pair.action.BaseValues.Any(item =>
                        item.Channel == PrintedValueChannel.Attack && item.Amount == printed))
                    .Where(pair => namedAttack == null || CardKey(DefinitionOf(pair.card).DisplayName) == CardKey(namedAttack))
                    .Where(pair => !(spec.Get("ranged") is bool ranged) ||
                        (pair.action.AttackRange == AttackRangeKind.Ranged) == ranged)
                    .OrderBy(pair => pair.action.AttackRange == AttackRangeKind.Ranged ? 1 : 0)
                    .ThenBy(pair => pair.action.EffectRoot == null ? 0 : 1)
                    .ThenBy(pair => pair.card.Id.Value, StringComparer.Ordinal)
                    .Select(pair => pair.card)
                    .FirstOrDefault()
                    ?? throw Refuse($"setup attack: {by} ({setup.Heroes.Single(seat => seat.HeroId.Equals(attacker)).DisplayName}) " +
                        $"has no {(namedAttack == null ? "card" : "named " + namedAttack)} whose primary Attack prints {printed}" +
                        (spec.Get("ranged") is bool requiredRange ? $" with ranged={requiredRange}" : string.Empty));
                Log($"the attack: {by} plays {DefinitionOf(attackCard).DisplayName}");
            }

            private CardDefinition DefinitionOf(CardInstance instance) =>
                configuration.CardDefinitions.Single(definition => definition.Reference.Equals(instance.DefinitionReference));

            private int DefenseValueOf(CardDefinition definition, ActionDefinitionId actionId = default) =>
                definition.ActionDefinitions
                    .Where(action => action.Kind == ActionKind.Defense && (!actionId.IsDefined || action.Id.Equals(actionId)))
                    .SelectMany(action => action.BaseValues)
                    .Where(value => value.Channel == PrintedValueChannel.Defense)
                    .Select(value => (int?)value.Amount)
                    .DefaultIfEmpty(null)
                    .Max() ?? -1;

            private void StageHand(
                HeroId owner, string reference, List<string> hand, List<string> discard,
                List<CardInstance> needs, int fillersWanted, object defendsWith)
            {
                var owned = configuration.CardInstances.Where(card => card.OwnerId.Equals(owner) &&
                    !DefinitionOf(card).Progression.IsUltimate).ToList();
                var wantedHand = hand?.Select(value => Instance(owner, OwnedDefinition(owner, value))).ToList();
                var wantedDiscard = discard?.Select(value => Instance(owner, OwnedDefinition(owner, value))).ToList()
                    ?? new List<CardInstance>();
                var keep = new HashSet<CardInstance>(needs);
                if (wantedHand != null) keep.UnionWith(wantedHand);

                // The Defense card the enemy discards, where the table names its value.
                CardInstance defense = null;
                if (defendsWith != null && !(defendsWith is string none && Normalize(none) == "none"))
                {
                    var holding = wantedHand ?? owned.Where(card => card.State.Location == CardLocation.Hand).ToList();
                    Func<CardInstance, bool> fits = defendsWith is double
                        ? (Func<CardInstance, bool>)(card => DefenseValueOf(DefinitionOf(card)) == IntOf(defendsWith, reference + ".defends-with"))
                        : defendsWith is string cardName
                            ? card => CardKey(DefinitionOf(card).DisplayName) == CardKey(cardName)
                            : throw Unsupported("setup", $"{reference}.defends-with: {Describe(defendsWith)}");
                    defense = holding.Where(fits).OrderBy(card => card.Id.Value, StringComparer.Ordinal).FirstOrDefault();
                    if (defense == null && wantedHand == null)
                    {
                        defense = owned.Where(card => card.State.Location == CardLocation.Deck && fits(card) &&
                                !wantedDiscard.Contains(card))
                            .OrderBy(card => card.Id.Value, StringComparer.Ordinal).FirstOrDefault();
                    }

                    if (defense == null)
                    {
                        throw Refuse($"setup {reference}.defends-with {Describe(defendsWith)}: no card with that " +
                            "Defense value can be in that hero's hand");
                    }

                    keep.Add(defense);
                }

                var defaultHand = wantedHand == null;
                var target = defaultHand
                    ? owned.Where(card => card.State.Location == CardLocation.Hand && !wantedDiscard.Contains(card)).ToList()
                    : new List<CardInstance>(wantedHand);
                foreach (var needed in keep.Where(card => !target.Contains(card)).ToList())
                {
                    if (defaultHand)
                    {
                        // A starting hand holds one card of each colour: the needed card takes the place
                        // of the one of its colour, or of the first card nothing else needs.
                        var color = DefinitionOf(needed).Progression.Color;
                        var replaced = target.Where(card => !keep.Contains(card))
                            .OrderBy(card => DefinitionOf(card).Progression.Color == color ? 0 : 1)
                            .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                            .FirstOrDefault();
                        if (replaced != null) target.Remove(replaced);
                    }

                    target.Add(needed);
                }

                // The cards committed on the turns before the actor's play: the lowest-Initiative
                // cards nothing else needs, taken from the hand first and then from the deck.
                var turnFillers = new List<CardInstanceId>();
                for (var index = 0; index < fillersWanted; index++)
                {
                    var filler = target.Where(card => !keep.Contains(card) && !turnFillers.Contains(card.Id))
                        .Concat(owned.Where(card => !target.Contains(card) && !wantedDiscard.Contains(card)))
                        .Where(card => !keep.Contains(card) && !turnFillers.Contains(card.Id))
                        .OrderBy(card => target.Contains(card) && defaultHand ? 0 : 1)
                        .ThenBy(card => DefinitionOf(card).Initiative.Value)
                        .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (filler == null) break;
                    if (!target.Contains(filler)) target.Add(filler);
                    turnFillers.Add(filler.Id);
                }

                fillers[owner] = turnFillers;
                reserved[owner] = new HashSet<CardInstanceId>(keep.Select(card => card.Id));

                foreach (var card in owned.Where(card => card.State.Location == CardLocation.Hand && !target.Contains(card)))
                {
                    if (!CardLifecycleAuthority.TryReturnToDeck(card)) throw Refuse($"setup: {card.Id.Value} cannot leave the hand");
                }

                foreach (var card in target.Where(card => card.State.Location != CardLocation.Hand))
                {
                    if (card.State.Location != CardLocation.Deck || !CardLifecycleAuthority.TryTakeFromDeckIntoHand(card))
                    {
                        throw Refuse($"setup: {reference}'s \"{DefinitionOf(card).DisplayName}\" cannot be put in hand");
                    }
                }

                foreach (var card in wantedDiscard)
                {
                    if (target.Contains(card)) throw Refuse($"setup: {reference}'s \"{DefinitionOf(card).DisplayName}\" is named in hand and discard");
                    if (card.State.Location == CardLocation.Deck) CardLifecycleAuthority.TryTakeFromDeckIntoHand(card);
                    if (!CardLifecycleAuthority.TryDiscardFromHand(card))
                    {
                        throw Refuse($"setup: {reference}'s \"{DefinitionOf(card).DisplayName}\" cannot be discarded");
                    }
                }
            }

            // ---- the board

            private HexCoordinate anchor;
            private readonly List<(string Item, CardInstance Card)> itemCards = new List<(string, CardInstance)>();
            private readonly List<int> spawnNeeds = new List<int>();
            private bool clearOfTerrain;
            private bool strictFiles;
            private readonly Dictionary<string, int> rotation = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> files = new Dictionary<string, int>(StringComparer.Ordinal);

            private void PlaceEverything()
            {
                var map = Map.Value;
                var spaces = map.Spaces.ToDictionary(space => space.Coordinate);
                var battleZone = map.Zones.Single(zone => zone.IsStartingBattleZone).Id;
                var anyFlag = pieces.Any(piece => piece.BattleZone);
                var centre = BeforeActionStepPositions.Centre;
                var playable = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable)
                    .Select(space => space.Coordinate).ToArray();
                var anchors = new[] { centre }.Concat(playable
                    .Where(space => !space.Equals(centre))
                    .OrderByDescending(space => playable.Count(other => Dist(other, space) <= 2))
                    .ThenBy(space => Dist(space, centre))
                    .ThenBy(space => space.Q)
                    .ThenBy(space => space.R));

                string firstReason = null;
                var anchorList = anchors.ToList();

                // Open ground first: unless the table stages terrain, no staged piece stands beside a
                // terrain space, so "adjacent to terrain" is never an accident of the anchor.
                // Straight-line pieces from one piece share a file where any anchor allows it, then open
                // ground; each is relaxed in turn only when no anchor meets it.
                var noTerrain = pieces.All(piece => !piece.IsTerrain);
                var passes = new[] { (true, noTerrain), (true, false), (false, noTerrain), (false, false) }.Distinct();
                foreach (var (shareFiles, avoidTerrain) in passes)
                {
                    strictFiles = shareFiles;
                    clearOfTerrain = avoidTerrain;
                    foreach (var candidate in anchorList)
                    {
                        if (TryPlaceFrom(candidate, spaces, battleZone, anyFlag, out var reason))
                        {
                            anchor = candidate;
                            Log("anchor " + candidate + "; " + string.Join("; ", pieces.Select(piece => $"{piece.Ref} {piece.At}")));
                            return;
                        }

                        firstReason = firstReason ?? reason;
                    }
                }

                throw Refuse(firstReason ?? "unresolvable placement");
            }

            private bool TryPlaceFrom(
                HexCoordinate from, IReadOnlyDictionary<HexCoordinate, BoardMapSpace> spaces, ZoneId battleZone,
                bool anyFlag, out string reason)
            {
                reason = null;
                rotation.Clear();
                files.Clear();
                var occupied = new HashSet<HexCoordinate>();
                Func<Piece, HexCoordinate, bool> acceptable = (piece, at) =>
                {
                    if (!spaces.TryGetValue(at, out var space) || occupied.Contains(at)) return false;
                    if (piece.IsTerrain) return space.Kind == BoardMapSpaceKind.Terrain;
                    if (space.Kind != BoardMapSpaceKind.Playable) return false;
                    var inZone = space.ZoneId.HasValue && space.ZoneId.Value.Equals(battleZone);
                    // A minion outside the Battle Zone is corrected back into it (p.19) the moment an
                    // action ends, so a staged minion stands inside it or the board would not hold.
                    if ((piece.BattleZone || piece.IsMinion) && !inZone) return false;
                    if (clearOfTerrain && at.GetNeighbors().Any(neighbor =>
                        spaces.TryGetValue(neighbor, out var beside) && beside.Kind == BoardMapSpaceKind.Terrain))
                    {
                        return false;
                    }

                    return true;
                };

                if (!acceptable(pieces[0], from))
                {
                    reason = "unresolvable placement: the actor cannot stand in the Battle Zone's open ground";
                    return false;
                }

                pieces[0].At = from;
                occupied.Add(from);
                var positions = new Dictionary<string, HexCoordinate>(StringComparer.Ordinal) { ["actor"] = from };
                foreach (var piece in pieces.Skip(1))
                {
                    var placement = piece.Where;
                    var start = rotation.TryGetValue(placement.Primary, out var turned) ? turned : 0;
                    // Pieces placed "in a straight line" from the same piece share that piece's first
                    // file, so "distance 1 / 2 / 3 from actor in a straight line" is one line.
                    var straight = placement.Kind == "distance" && placement.Line == "straight" && placement.Through == null;
                    if (straight && files.TryGetValue(placement.A, out var file)) start = file;
                    var fileNeeded = straight && strictFiles && files.ContainsKey(placement.A) ? files[placement.A] : (int?)null;
                    var chosen = spaces.Keys
                        .Where(at => acceptable(piece, at) && Satisfies(placement, at, positions) &&
                            PushZoneFits(piece, at, positions, spaces) &&
                            (fileNeeded == null || SectorOf(positions[placement.A], at) == fileNeeded.Value))
                        .OrderBy(at => placement.Prefer == null ? 0 : (placement.PreferAway ? -1 : 1) * Dist(at, positions[placement.Prefer]))
                        .ThenBy(at => placement.Kind == "distance" && placement.Line == null && !Aligned(positions[placement.A], at) ? 1 : 0)
                        .ThenBy(at => (SectorOf(positions[placement.Primary], at) - start + 6) % 6)
                        .ThenBy(at => at.Q)
                        .ThenBy(at => at.R)
                        .Select(at => (HexCoordinate?)at)
                        .FirstOrDefault();
                    if (chosen == null)
                    {
                        reason = $"unresolvable placement \"{placement.Text}\" for \"{piece.Ref}\"" +
                            (piece.IsTerrain ? " (no terrain space of the official map fits it)" : string.Empty);
                        return false;
                    }

                    if (straight && !files.ContainsKey(placement.A))
                    {
                        files[placement.A] = SectorOf(positions[placement.A], chosen.Value);
                        rotation[placement.Primary] = start + 1;
                    }
                    else if (!straight)
                    {
                        rotation[placement.Primary] = start + 1;
                    }

                    piece.At = chosen.Value;
                    positions[piece.Ref] = chosen.Value;
                    occupied.Add(chosen.Value);
                }

                if (!FitsSpawnStaging(positions, occupied, out reason)) return false;

                // An answer or fact naming an empty spawn point near the actor needs one there.
                foreach (var distance in spawnNeeds)
                {
                    if (!SpawnSpaces.Value.Any(space => Dist(space, from) <= distance && !occupied.Contains(space) &&
                            spaces[space].Kind == BoardMapSpaceKind.Playable))
                    {
                        reason = $"unresolvable placement \"empty spawn point within distance {distance} of actor\"";
                        return false;
                    }
                }

                return true;
            }

            private static bool Satisfies(
                Placement placement, HexCoordinate at, IReadOnlyDictionary<string, HexCoordinate> positions,
                Func<HexCoordinate, bool> isEmpty = null) =>
                SatisfiesOne(placement, at, positions, isEmpty) &&
                (placement.Also == null || Satisfies(placement.Also, at, positions, isEmpty));

            private static bool SatisfiesOne(
                Placement placement, HexCoordinate at, IReadOnlyDictionary<string, HexCoordinate> positions,
                Func<HexCoordinate, bool> isEmpty)
            {
                HexCoordinate Of(string reference) => positions[reference];
                foreach (var named in placement.Refs())
                {
                    if (!positions.ContainsKey(named)) return false;
                }

                switch (placement.Kind)
                {
                    case "adjacent":
                        return Dist(at, Of(placement.A)) == 1 && (placement.B == null || Dist(at, Of(placement.B)) == 1);
                    case "distance":
                        var from = Of(placement.A);
                        if (Dist(at, from) != placement.D) return false;
                        if (placement.Line == "straight" && !Aligned(from, at)) return false;
                        if (placement.Line == "off" && Aligned(from, at)) return false;
                        return placement.Through == null || Between(from, Of(placement.Through), at);
                    case "beyond":
                        var origin = Of(placement.A);
                        var past = Of(placement.B);
                        return Aligned(origin, past) && Dist(past, at) == placement.D && Between(origin, past, at);
                    case "online":
                        return Aligned(Of(placement.A), Of(placement.B)) && Between(Of(placement.A), Of(placement.B), at);
                    case "spawn":
                        return SpawnSpaces.Value.Contains(at) && Dist(at, Of(placement.A)) <= placement.D &&
                            (isEmpty == null || isEmpty(at));
                    default:
                        return false;
                }
            }

            private void Build(JsonObject spec, JsonObject actorSpec)
            {
                var map = Map.Value;
                var staged = new HashSet<HexCoordinate>(pieces.Select(piece => piece.At));
                var far = map.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Playable && Dist(space.Coordinate, anchor) >= 8)
                    .Select(space => space.Coordinate)
                    .OrderBy(space => space.Q)
                    .ThenBy(space => space.R)
                    .ToList();
                var farChosen = new List<HexCoordinate>();
                foreach (var space in far)
                {
                    if (farChosen.All(other => Dist(other, space) >= 3) && staged.All(other => Dist(other, space) >= 3))
                    {
                        farChosen.Add(space);
                    }
                }

                var heroes = new Dictionary<HeroId, HexCoordinate>();
                var farIndex = 0;
                foreach (var seat in setup.Heroes)
                {
                    var piece = pieces.FirstOrDefault(value => value.IsHero && value.Hero.Equals(seat.HeroId));
                    if (piece != null)
                    {
                        heroes[seat.HeroId] = piece.At;
                    }
                    else
                    {
                        if (farIndex >= farChosen.Count) throw Refuse("unresolvable placement: no far space for an unnamed hero");
                        heroes[seat.HeroId] = farChosen[farIndex++];
                    }
                }

                var minions = pieces.Where(piece => piece.IsMinion).Select(piece =>
                {
                    var kept = setup.Minions.Single(value => value.MinionId.Equals(piece.Minion));
                    return new QuickGameMinionSetupState(kept.MinionId, kept.TeamId, kept.TeamSide, kept.MinionType,
                        kept.SpawnPoint, SpaceAt(piece.At), piece.At);
                }).ToArray();

                var teams = setup.Teams.Select(team =>
                {
                    var key = team.TeamSide == actorSide ? "team-lives" : "enemy-team-lives";
                    return spec.Get(key) == null
                        ? team
                        : new QuickGameTeamSetupState(team.TeamId, team.TeamSide, IntOf(spec.Get(key), key), team.ThroneZoneId);
                }).ToArray();

                var stagedSetup = new QuickGameSetupState(
                    setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, teams,
                    setup.Heroes.Select(seat => new QuickGameHeroSetupState(
                        seat.HeroId, seat.ContentId, seat.DisplayName, seat.TeamId, seat.TeamSide,
                        seat.SpawnPoint, SpaceAt(heroes[seat.HeroId]), heroes[seat.HeroId])),
                    minions);
                var obstacles = pieces.Where(piece => piece.Obstacle.IsDefined)
                    .Select(piece => new ObstacleOccupancy(piece.Obstacle, piece.At))
                    .ToArray();

                var level = actorSpec.Get("level") == null ? 1 : IntOf(actorSpec.Get("level"), "actor.level");
                var coins = actorSpec.Get("coins") == null ? 0 : IntOf(actorSpec.Get("coins"), "actor.coins");
                int CoinsOfSeat(HeroId id)
                {
                    if (id.Equals(actor)) return coins;
                    var reference = RefOfHero(id);
                    return refSetups.TryGetValue(reference, out var values) && values.Has("coins")
                        ? IntOf(values.Get("coins"), reference + ".coins") : 0;
                }
                var progression = new MatchStagedProgression(
                        setup.Heroes.Select(seat => new KeyValuePair<HeroId, HeroLevel>(
                            seat.HeroId, new HeroLevel(seat.HeroId.Equals(actor) ? level : 1))),
                        setup.Heroes.Select(seat => new KeyValuePair<HeroId, int>(
                            seat.HeroId, CoinsOfSeat(seat.HeroId))));

                var scenario = new InitialScenarioDescription(
                    configuration.Participants,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.TieBreakerState,
                    configuration.SetupSeed,
                    map.Reference,
                    stagedSetup.Heroes.Select(seat => new HeroOccupancy(seat.HeroId, seat.Coordinate)),
                    obstacles);
                initial = new MatchSessionInitialState(scenario, stagedSetup, new[] { map }, null, null, progression);
                session = new MatchSession(initial);

                // Tokens and items: committed to the fresh graphs, then carried by a snapshot.
                var tokens = pieces.Where(piece => piece.IsTokenLike).ToArray();
                foreach (var reserve in reservePieces)
                {
                    var removed = session.aggregate.Board.CommitValidatedRemoval(BoardObjectReference.ForMinion(reserve.Minion));
                    if (!removed.Accepted) throw Refuse("staged reserve removal refused: " + reserve.Ref);
                }
                if (tokens.Length == 0 && itemCards.Count == 0 && reservePieces.Count == 0) return;
                foreach (var group in tokens.GroupBy(piece => (piece.TokenDefinition.Id.Value, piece.TokenOwner.Value)))
                {
                    var definition = group.First().TokenDefinition;
                    if (group.Count() > definition.SupplyCount)
                    {
                        throw Refuse($"setup places {group.Count()} {definition.DisplayName} tokens of one owner; " +
                            $"the supply holds {definition.SupplyCount}");
                    }
                }

                foreach (var piece in tokens)
                {
                    session.aggregate.Board.CommitValidatedTokenPlacement(new PlacedToken(
                        piece.Token, piece.TokenDefinition, piece.TokenOwner, piece.TokenOwner,
                        new CardInstanceId("table-staged-token"), piece.At));
                }

                // p.16's items are the set-aside cards' icons: the staged card was set aside from the
                // deck with the hands, and is placed here exactly as the level-up's item step places it.
                foreach (var (item, card) in itemCards)
                {
                    var live = session.aggregate.Configuration.CardInstances.Single(value => value.Id.Equals(card.Id));
                    var placed = session.aggregate.Round.HeroItems.TryPlaceSetAsideCard(
                        live, session.aggregate.Configuration.CardDefinitions);
                    if (!placed.IsPlaced) throw Refuse($"setup actor.items \"{item}\": {placed.RejectionReason}");
                }

                // The aggregate memoizes its capture until a commit or a graph replacement; re-adopting
                // its own graphs is the replacement that makes the capture read the staged board.
                session.aggregate.Adopt(session.aggregate.Capture());
                var snapshot = session.CaptureSnapshot();
                session = new MatchSession(initial);
                var restored = session.RestoreSnapshot(snapshot);
                if (!restored.Accepted) throw Refuse("the staged tokens and items did not restore: " + restored.Detail);
                if (session.Projection.PlacedTokens.Count != tokens.Length)
                {
                    throw Refuse($"the staged tokens did not reach the restored board ({session.Projection.PlacedTokens.Count} of {tokens.Length})");
                }
            }

            private static ConfiguredSpaceReference SpaceAt(HexCoordinate coordinate) =>
                new ConfiguredSpaceReference(Map.Value.Id, Map.Value.Spaces.Single(space => space.Coordinate.Equals(coordinate)).Id);

            // ------------------------------------------------------------------ the turns before the play

            private void ReachTheActorsTurn()
            {
                var creatorsThisTurn = standing.Where(value => !value.LastTurn).ToList();
                var lastTurn = standing.Where(value => value.LastTurn).ToList();
                var played = new HashSet<CardInstanceId>();
                var startedCreator = default(CardInstanceId);
                var turnsCommitted = new Dictionary<HeroId, int>();
                for (var step = 0; step < 600; step++)
                {
                    var projection = session.Projection;
                    if (startedCreator.IsDefined && !(projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                        projection.SelectedActor.CardInstanceId.Equals(startedCreator)))
                    {
                        played.Add(startedCreator);
                        startedCreator = default;
                    }

                    if (projection.Phase == RoundResolutionPhase.GameEnded) throw Refuse("the match ended before the actor's turn");
                    DirectorCommand next = null;
                    if (projection.Phase == RoundResolutionPhase.AwaitingCommitments && projection.DecisionContract == null)
                    {
                        var asked = projection.Commitments.First(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                        turnsCommitted.TryGetValue(asked.HeroId, out var turn);
                        turnsCommitted[asked.HeroId] = turn + 1;
                        next = Commitment(projection, asked, turn, lastTurn.Count > 0);
                    }
                    else if (projection.PendingInitiativeChoice != null)
                    {
                        var tie = projection.PendingInitiativeChoice;
                        var preferred = tie.CandidateCardInstanceIds
                            .OrderBy(card => standing.Any(value => value.Card.Equals(card) && !played.Contains(card)) ? 0
                                : card.Equals(isDefend ? attackCard.Id : caseInstance.Id) ? 1 : 2)
                            .First();
                        next = DirectorCommand.ChooseTiedActor(tie.OwningSide, preferred);
                    }
                    else if (projection.PendingTurnRelease != null && !IsFamily(projection.DecisionContract, PendingDecisionFamilies.Effect))
                    {
                        next = DirectorCommand.ReleaseTurn();
                    }
                    else if (projection.IsTurnActive && projection.SelectedActor.IsDefined && projection.DecisionContract == null)
                    {
                        var selected = projection.SelectedActor;
                        if (followupCard != null && selected.CardInstanceId.Equals(followupCard.Id))
                            throw Refuse("follow-up card acts before the case card; choose a lower-Initiative follow-up");
                        if (isDefend && selected.CardInstanceId.Equals(attackCard.Id))
                        {
                            return;
                        }

                        if (!isDefend && selected.CardInstanceId.Equals(caseInstance.Id))
                        {
                            var missing = creatorsThisTurn.Where(value => !played.Contains(value.Card)).ToList();
                            if (missing.Count > 0)
                            {
                                throw Refuse($"unsupported standing \"{missing[0].Phrase}\": that card does not act " +
                                    $"before the actor's \"{caseCard.DisplayName}\" this turn");
                            }

                            if (lastTurn.Any(value => !played.Contains(value.Card)))
                            {
                                throw Refuse($"standing \"{lastTurn[0].Phrase}\" was not played");
                            }

                            return;
                        }

                        var creation = standing.FirstOrDefault(value => value.Card.Equals(selected.CardInstanceId));
                        if (creation.Phrase != null && !played.Contains(creation.Card))
                        {
                            if (heldStanding.Contains(creation.Card))
                            {
                                next = DirectorCommand.PerformHold(creation.Card);
                                startedCreator = creation.Card;
                            }
                            else
                            {
                            var primary = projection.AvailableTurnActions.FirstOrDefault(offer => offer.IsAvailable &&
                                offer.SourceCardInstanceId.Equals(creation.Card) &&
                                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
                            if (!primary.ActionDefinitionId.IsDefined)
                            {
                                throw Refuse($"standing \"{creation.Phrase}\": its primary action is not offered");
                            }

                            next = DirectorCommand.StartAction(primary.SourceCardInstanceId, primary.ActionDefinitionId);
                            startedCreator = creation.Card;
                            }
                        }
                        else if (projection.AvailableHoldOffer.HasValue && projection.AvailableHoldOffer.Value.IsAvailable)
                        {
                            next = DirectorCommand.PerformHold(selected.CardInstanceId);
                        }
                    }
                    else if (startedCreator.IsDefined && projection.DecisionContract != null)
                    {
                        // Explicit staged Defense applies to the prerequisite play too. Other
                        // prerequisite questions retain the documented doing-way default.
                        var defender = projection.PendingDefenseChoice;
                        if (TryAnswerStagedRuneAssignment(projection, out var runeCommand)) next = runeCommand;
                        else next = defender != null && refSetups.TryGetValue(RefOfHero(defender.ChooserId), out var spec) &&
                            spec.Has("defends-with")
                            ? AnswerDefense(projection, projection.DecisionContract, null)
                            : projection.DecisionContract.Commands.OrderBy(OfferOutcomeTests.Doing).First();
                    }

                    if (next == null)
                    {
                        var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                        if (legal.Count == 0) throw Refuse($"no legal command before the actor's turn ({projection.Phase})");
                        next = legal[0];
                    }

                    var result = session.Execute(next);
                    if (!result.Accepted) throw Refuse($"{next.Kind} {next.CardInstanceId} refused before the actor's turn: {result.Detail}");
                }

                throw Refuse("the actor's turn was not reached");
            }

            private DirectorCommand Commitment(
                MatchSessionProjection projection, MatchCommitmentProjection asked, int turn, bool hasLastTurn)
            {
                var owner = asked.HeroId;
                CardInstanceId chosen = default;
                if (owner.Equals(actor) && hasLastTurn && turn == 0)
                {
                    chosen = standing.First(value => value.LastTurn).Card;
                }
                else if (owner.Equals(actor) && isDefend)
                {
                    chosen = fillers[actor].Count > turn ? fillers[actor][turn] : default;
                }
                else if (owner.Equals(actor) && turn <= (hasLastTurn ? 1 : 0))
                {
                    chosen = hasLastTurn && turn == 0 ? standing.First(value => value.LastTurn).Card : caseInstance.Id;
                }
                else if (isDefend && owner.Equals(attacker) && turn == (hasLastTurn ? 1 : 0))
                {
                    chosen = attackCard.Id;
                }
                else if (followupCard != null && owner.Equals(followupHero) && turn == (hasLastTurn ? 1 : 0) + (followupNextTurn ? 1 : 0))
                {
                    chosen = followupCard.Id;
                }
                else
                {
                    var creation = standing.FirstOrDefault(value => value.Creator.Equals(owner) && !value.LastTurn);
                    var isCaseTurn = turn == (hasLastTurn ? 1 : 0);
                    if (creation.Phrase != null && isCaseTurn)
                    {
                        chosen = creation.Card;
                    }
                    else if (fillers.TryGetValue(owner, out var list) && turn < list.Count &&
                        LocationIn(projection, list[turn]) == CardLocation.Hand)
                    {
                        chosen = list[turn];
                    }
                    else
                    {
                        var held = reserved.TryGetValue(owner, out var set) ? set : new HashSet<CardInstanceId>();
                        chosen = projection.Cards
                            .Where(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand)
                            .OrderBy(card => held.Contains(card.Id) ? 1 : 0)
                            .ThenBy(card => DefinitionIn(projection, card).Initiative.Value)
                            .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                            .Select(card => card.Id)
                            .FirstOrDefault();
                    }
                }

                if (!chosen.IsDefined)
                {
                    return asked.CanPass ? DirectorCommand.Pass(owner) : throw Refuse($"{RefOfHero(owner)} has no card to commit");
                }

                return DirectorCommand.CommitCard(owner, chosen);
            }

            private static CardLocation LocationIn(MatchSessionProjection projection, CardInstanceId card) =>
                projection.Cards.Single(value => value.Id.Equals(card)).State.Location;

            private static CardDefinition DefinitionIn(MatchSessionProjection projection, CardInstance card) =>
                projection.Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference));

            private bool TheOffer(out ActionOffer? offer)
            {
                var projection = session.Projection;
                if (isHold)
                {
                    offer = projection.AvailableHoldOffer;
                    return offer.HasValue && offer.Value.IsAvailable;
                }

                var found = projection.AvailableTurnActions.FirstOrDefault(candidate =>
                    candidate.SourceCardInstanceId.Equals(caseInstance.Id) &&
                    candidate.ActionDefinitionId.Equals(caseAction.Id));
                offer = found.ActionDefinitionId.IsDefined ? found : (ActionOffer?)null;
                return offer.HasValue && offer.Value.IsAvailable;
            }

            // ------------------------------------------------------------------ the play

            private void Capture()
            {
                before = session.Projection;
                logBaseline = before.MatchLog.Select(value => value.Sequence).DefaultIfEmpty(0).Max();
                Log("at the play: " + string.Join("; ", byRef.Keys.Select(reference => $"{reference} {PositionOf(before, reference)}")));
            }

            private void LogAfter(MatchSessionProjection projection)
            {
                Log("after the play: " + string.Join("; ", byRef.Keys.Select(reference => $"{reference} {PositionOf(projection, reference)}")));
                foreach (var line in projection.MatchLog.Where(value => value.Sequence > logBaseline))
                {
                    Log("  log: " + line);
                }

                Log("scoreboard: " + string.Join("; ", projection.Scoreboard.Select(value =>
                    $"{value.HeroId.Value} kills {value.Kills} deaths {value.Deaths} minions {value.MinionsKilled}")));
            }

            private void Play(bool forced)
            {
                MatchCommandResult started;
                if (isHold)
                {
                    started = session.Execute(DirectorCommand.PerformHold(caseInstance.Id));
                }
                else if (forced)
                {
                    started = session.StartActionIgnoringFeasibilityForInternalTest(caseInstance.Id, caseAction.Id);
                }
                else
                {
                    started = session.Execute(DirectorCommand.StartAction(caseInstance.Id, caseAction.Id));
                }

                ObserveEvents(started);
                if (!started.Accepted && forced)
                {
                    // The table expects the play not to be offered and lists what it leaves unchanged;
                    // the rules refuse it even past the offer (an Attack with no target, p.13), so the
                    // board stands as it was and the facts are read from it.
                    Log($"forced start refused, the board is unchanged: {started.Detail}");
                    answers.Clear();
                    return;
                }

                if (!started.Accepted)
                {
                    throw Refuse($"the engine refused to start {actionName} on {caseCard.DisplayName}" +
                        (forced ? " even ignoring feasibility" : string.Empty) + $": {started.Detail}");
                }

                RunTheAnswers();
            }

            private void RunFollowup()
            {
                if (answers.Count > 0 && answers.All(value => Normalize(value) == "decline")) answers.Clear();
                if (answers.Count > 0) throw Refuse("case answers remain before follow-up: " + string.Join(", ", answers));
                for (var step = 0; step < 200; step++)
                {
                    var projection = session.Projection;
                    if (projection.Phase == RoundResolutionPhase.GameEnded)
                        throw Refuse("match ended before the follow-up");
                    if (projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                        projection.SelectedActor.CardInstanceId.Equals(followupCard.Id) && projection.DecisionContract == null &&
                        projection.PendingTurnRelease == null)
                    {
                        atFollowup = projection;
                        if (!Enum.TryParse(followup.Get("action") as string, out ActionKind kind))
                            throw Unsupported("follow-up action", followup.Get("action") as string);
                        var definition = DefinitionOf(followupCard).ActionDefinitions.Where(action => action.Kind == kind)
                            .OrderBy(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary ? 0 : 1).FirstOrDefault();
                        if (definition == null) throw Refuse("follow-up card has no such primary action");
                        var offers = projection.AvailableTurnActions.Where(offer => offer.SourceCardInstanceId.Equals(followupCard.Id) &&
                            offer.ActionDefinitionId.Equals(definition.Id)).ToArray();
                        var available = offers.Any(offer => offer.IsAvailable);
                        var wanted = !(followup.Get("offered") is bool explicitOffer) || explicitOffer;
                        Log("follow-up " + DefinitionOf(followupCard).DisplayName + ": offered " + available + ", expected " + wanted);
                        if (available != wanted) Failures.Add(name + ": follow-up offered " + available + ", expected " + wanted);
                        if (!available || !wanted) return;
                        foreach (var answer in (followup.Get("answers") as List<object>) ?? new List<object>())
                            answers.Enqueue(answer as string ?? throw Unsupported("follow-up answer", Describe(answer)));
                        var deciding = SeatProjectionFactory.DecidingActor(projection);
                        var started = session.Execute(DirectorCommand.StartAction(followupCard.Id, definition.Id));
                        if (!started.Accepted) throw Refuse("follow-up start refused: " + started.Detail);
                        actionChoosers[followupCard.OwnerId] = deciding;
                        ObserveEvents(started);
                        RunTheAnswers();
                        return;
                    }
                    DirectorCommand next;
                    if (projection.Phase == RoundResolutionPhase.AwaitingCommitments && projection.DecisionContract == null)
                    {
                        if (!followupNextTurn) throw Refuse("follow-up did not act later in the same turn");
                        var asked = projection.Commitments.First(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                        var hasLastTurn = standing.Any(value => value.LastTurn);
                        next = Commitment(projection, asked, hasLastTurn ? 2 : 1, hasLastTurn);
                    }
                    else if (projection.PendingTurnRelease != null && projection.DecisionContract == null) next = DirectorCommand.ReleaseTurn();
                    else if (projection.PendingInitiativeChoice != null)
                    {
                        var tie = projection.PendingInitiativeChoice;
                        next = DirectorCommand.ChooseTiedActor(tie.OwningSide,
                            tie.CandidateCardInstanceIds.OrderBy(id => id.Equals(followupCard.Id) ? 0 : 1).First());
                    }
                    else if (projection.DecisionContract != null) next = projection.DecisionContract.Commands.OrderBy(OfferOutcomeTests.Doing).First();
                    else if (projection.IsTurnActive && projection.SelectedActor.IsDefined) next = DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId);
                    else throw Refuse("follow-up cannot advance from " + projection.Phase);
                    var result = session.Execute(next);
                    if (!result.Accepted) throw Refuse("follow-up advance refused: " + result.Detail);
                    ObserveEvents(result);
                }
                throw Refuse("follow-up exceeded its step budget");
            }

            private void RunTheAnswers()
            {
                for (var step = 0; step < 200; step++)
                {
                    var projection = session.Projection;
                    Observe(projection);
                    if (projection.Phase == RoundResolutionPhase.GameEnded) return;
                    var contract = projection.DecisionContract;
                    if (contract != null && !IsFamily(contract, PendingDecisionFamilies.TurnRelease))
                    {
                        var pending = answers.Count;
                        var isCardTextFastTravel = projection.PendingEffectChoice?.NodeId.Value == "late-travel";
                        var travelFrom = isCardTextFastTravel ? PositionOf(projection, "actor") : null;
                        if (isCardTextFastTravel) cardTextFastTravelOffered = true;
                        lastConsumed.Clear();
                        var command = Answer(projection, contract);
                        Log($"  asked: {DescribeQuestion(projection, contract)} -> {command.Kind}" +
                            (command.EffectChoice != null ? $" {command.EffectChoice.Kind} {command.EffectChoice.Candidate}" : string.Empty) +
                            (answers.Count < pending ? $" (answered \"{string.Join("\", \"", lastConsumed)}\")" : " (implicit)"));
                        var result = session.Execute(command);
                        if (!result.Accepted) throw Refuse($"the engine refused the answer {command.Kind}: {result.Detail}");
                        if (isCardTextFastTravel && travelFrom.HasValue &&
                            !PositionOf(session.Projection, "actor").Equals(travelFrom))
                            cardTextFastTravelCommitted = true;
                        ObserveEvents(result);
                        continue;
                    }

                    if (projection.PendingTurnRelease != null)
                    {
                        var released = session.Execute(DirectorCommand.ReleaseTurn());
                        if (!released.Accepted) throw Refuse("the turn did not release: " + released.Detail);
                        Observe(session.Projection);
                    }

                    return;
                }

                throw Refuse("the play did not finish within 200 answers");
            }

            private void RunTheDefence(List<JsonObject> expectations)
            {
                Capture();
                var projection = session.Projection;
                var attack = projection.AvailableTurnActions.FirstOrDefault(offer => offer.IsAvailable &&
                    offer.SourceCardInstanceId.Equals(attackCard.Id) &&
                    offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
                if (!attack.ActionDefinitionId.IsDefined)
                {
                    throw Refuse($"setup attack: {RefOfHero(attacker)}'s {DefinitionOf(attackCard).DisplayName} is not offered " +
                        "from this setup");
                }

                var started = session.Execute(DirectorCommand.StartAction(attack.SourceCardInstanceId, attack.ActionDefinitionId));
                if (!started.Accepted) throw Refuse("the attack did not start: " + started.Detail);
                RunTheAnswers();
                afterOriginalAction = session.Projection;
                if (followup != null) RunFollowup();
                after = session.Projection;
                LogAfter(after);
                foreach (var expectation in expectations)
                {
                    if (expectation.Has("offered") && !expectation.Has("bullet"))
                    {
                        if (!(expectation.Get("offered") is bool expected)) throw Unsupported("expectation", Describe(expectation));
                        var actual = defendOffered ?? false;
                        if (actual != expected)
                        {
                            Mismatch(expectation, defendOffered.HasValue
                                ? $"the Defense question {(actual ? "offers" : "does not offer")} {caseCard.DisplayName}"
                                : "no Defense question reached the actor");
                        }

                        continue;
                    }

                    if (NeedsTheRoundEnd(expectation) && afterRound == null) RunTheRoundEnd();
                    Evaluate(expectation);
                }

                FinalChecks();
            }

            /// <summary>
            /// The Attack total an attack resolved with, read from its completion event: an attack on
            /// a minion asks no Defense, so no pending state ever shows the total.
            /// </summary>
            private void ObserveEvents(MatchCommandResult result)
            {
                foreach (var value in result.Events ?? Array.Empty<RoundResolutionEvent>())
                {
                    var fact = value.TurnEvent.CardActionEvent;
                    if (
                        (fact.Kind == GuardsDigital.Core.Rules.CardResolution.CardActionResolutionEventKind.AttackResolutionCompleted ||
                         fact.Kind == GuardsDigital.Core.Rules.CardResolution.CardActionResolutionEventKind.AttackSuccessful ||
                         fact.Kind == GuardsDigital.Core.Rules.CardResolution.CardActionResolutionEventKind.AttackSuccessfullyDefended))
                    {
                        // Minion attacks do not compare values; their event's default zero is not a computed total.
                        if (fact.TargetHeroId.IsDefined)
                        {
                            observedAttacks[fact.ActorId] = fact.AttackTotal;
                            if (fact.ActorId.Equals(actor)) observedAttack = fact.AttackTotal;
                        }
                        if (fact.Kind == GuardsDigital.Core.Rules.CardResolution.CardActionResolutionEventKind.AttackResolutionCompleted)
                        {
                            attackCounts.TryGetValue(fact.ActorId, out var count);
                            attackCounts[fact.ActorId] = count + 1;
                        }
                    }
                }
            }

            private void Observe(MatchSessionProjection projection)
            {
                var attack = projection.PendingAttackResolution?.AttackValue?.Total ??
                    projection.PendingDefenseChoice?.AttackTotal ?? projection.PendingDefensePreview?.AttackTotal;
                var attackingHero = projection.PendingAttackResolution?.ActorId ?? projection.PendingDefenseChoice?.AttackerId;
                if (attack.HasValue && attackingHero.HasValue) observedAttacks[attackingHero.Value] = attack.Value;
                if (attack.HasValue && projection.PendingAttackResolution?.ActorId.Equals(actor) != false)
                {
                    observedAttack = attack;
                }
            }

            private string Peek() => answers.Count > 0 ? answers.Peek() : null;

            private readonly List<string> lastConsumed = new List<string>();

            private string Dequeue()
            {
                var answer = answers.Dequeue();
                lastConsumed.Add(answer);
                return answer;
            }

            private enum AnswerShape { None, Reference, Confirm, Decline, Distance, Toward, Away, Option, Card, Defense, Space, RefDiscard, RefDefeat, RefDecline, Color, RuneSlot, Rune, TextFace }

            private AnswerShape ShapeOf(string answer, out string value, out string who)
            {
                value = null;
                who = null;
                if (answer == null) return AnswerShape.None;
                var text = Normalize(answer);
                if (text.StartsWith("text-face:", StringComparison.Ordinal))
                {
                    value = text.Substring(10).Trim();
                    if (value != "sun" && value != "moon") throw Unsupported("printed text face", value);
                    return AnswerShape.TextFace;
                }
                var runeSlot = Regex.Match(text, @"^slot:\s*([1-4]),\s*rune ([1-4])$");
                if (runeSlot.Success)
                {
                    value = runeSlot.Groups[1].Value + "," + runeSlot.Groups[2].Value;
                    return AnswerShape.RuneSlot;
                }
                var rune = Regex.Match(text, @"^rune:\s*([1-4])$");
                if (rune.Success)
                {
                    value = rune.Groups[1].Value;
                    return AnswerShape.Rune;
                }
                if (text.StartsWith("color:", StringComparison.Ordinal))
                {
                    value = text.Substring(6).Trim();
                    if (!Regex.IsMatch(value, "^[a-z]+$") || !Enum.TryParse(value, true, out CardColor color) ||
                        color == CardColor.None || !Enum.IsDefined(typeof(CardColor), color))
                        throw Unsupported("printed color", value);
                    return AnswerShape.Color;
                }
                var ownedDecline = Regex.Match(text, "^" + RefPattern + @":\s*decline$");
                if (ownedDecline.Success)
                {
                    who = ownedDecline.Groups[1].Value;
                    return AnswerShape.RefDecline;
                }
                var prefixed = Regex.Match(answer.Trim(), "^" + RefPattern + ":\\s*(discard (.+)|take defeat)$",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                if (prefixed.Success && RefShape.IsMatch(prefixed.Groups[1].Value.ToLowerInvariant()))
                {
                    who = prefixed.Groups[1].Value.ToLowerInvariant();
                    if (prefixed.Groups[3].Success)
                    {
                        value = prefixed.Groups[3].Value.Trim();
                        return AnswerShape.RefDiscard;
                    }

                    return AnswerShape.RefDefeat;
                }

                if (RefShape.IsMatch(text) && RefShape.Match(text).Groups[2].Success || text == "actor")
                {
                    value = text;
                    return AnswerShape.Reference;
                }

                if (text == "confirm") return AnswerShape.Confirm;
                if (text == "decline") return AnswerShape.Decline;
                Match match;
                if ((match = Regex.Match(text, "^distance ([0-9]+)$")).Success)
                {
                    value = match.Groups[1].Value;
                    return AnswerShape.Distance;
                }

                if ((match = Regex.Match(text, "^toward " + RefPattern + "$")).Success)
                {
                    value = match.Groups[1].Value;
                    return AnswerShape.Toward;
                }

                if ((match = Regex.Match(text, "^away from " + RefPattern + "$")).Success)
                {
                    value = match.Groups[1].Value;
                    return AnswerShape.Away;
                }

                if ((match = Regex.Match(answer.Trim(), "^(option|card|defense|space|place):\\s*(.+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                {
                    value = match.Groups[2].Value.Trim();
                    switch (match.Groups[1].Value.ToLowerInvariant())
                    {
                        case "option": return AnswerShape.Option;
                        case "card": return AnswerShape.Card;
                        case "defense": return AnswerShape.Defense;
                        default: return AnswerShape.Space;
                    }
                }

                throw Unsupported("answer", answer);
            }

            private string AnswerShapeCheck(string answer)
            {
                ShapeOf(answer, out _, out _);
                return answer;
            }

            /// <summary>The live position of a reference, or null where it is not on the board.</summary>
            private HexCoordinate? PositionOf(MatchSessionProjection projection, string reference)
            {
                if (!byRef.TryGetValue(reference, out var piece))
                {
                    var heroId = HeroOfRef(reference);
                    if (!heroId.IsDefined) throw Unsupported("reference", reference);
                    return projection.Heroes.Where(value => value.HeroId.Equals(heroId)).Select(value => (HexCoordinate?)value.Coordinate).FirstOrDefault();
                }

                if (piece.IsHero)
                    return projection.Heroes.Where(value => value.HeroId.Equals(piece.Hero)).Select(value => (HexCoordinate?)value.Coordinate).FirstOrDefault();
                if (piece.IsMinion)
                    return projection.Minions.Where(value => value.MinionId.Equals(piece.Minion)).Select(value => (HexCoordinate?)value.Coordinate).FirstOrDefault();
                if (piece.IsTokenLike)
                    return projection.PlacedTokens.Where(value => value.Id.Equals(piece.Token)).Select(value => (HexCoordinate?)value.Coordinate).FirstOrDefault();
                if (piece.IsTerrain) return piece.At;
                return projection.Obstacles.Where(value => value.ObstacleId.Equals(piece.Obstacle)).Select(value => (HexCoordinate?)value.Coordinate).FirstOrDefault();
            }

            private static bool Occupied(MatchSessionProjection projection, HexCoordinate space) =>
                projection.Heroes.Any(value => value.Coordinate.Equals(space)) ||
                projection.Minions.Any(value => value.Coordinate.Equals(space)) ||
                projection.PlacedTokens.Any(value => value.Coordinate.Equals(space)) ||
                projection.Obstacles.Any(value => value.Coordinate.Equals(space));

            private Dictionary<string, HexCoordinate> Positions(MatchSessionProjection projection, IEnumerable<string> refs)
            {
                var result = new Dictionary<string, HexCoordinate>(StringComparer.Ordinal);
                foreach (var reference in refs.Distinct())
                {
                    var at = PositionOf(projection, reference);
                    if (at.HasValue) result[reference] = at.Value;
                }

                return result;
            }

            private IEnumerable<HexCoordinate> Ordered(IEnumerable<HexCoordinate> candidates, Placement placement,
                IReadOnlyDictionary<string, HexCoordinate> positions, string preferRef, bool preferAway)
            {
                var prefer = preferRef ?? placement?.Prefer;
                var away = preferRef != null ? preferAway : placement != null && placement.PreferAway;
                var origin = placement != null && positions.ContainsKey(placement.Primary) ? positions[placement.Primary] : anchor;
                return candidates
                    .OrderBy(at => prefer == null || !positions.ContainsKey(prefer) ? 0 : (away ? -1 : 1) * Dist(at, positions[prefer]))
                    .ThenBy(at => placement != null && placement.Kind == "distance" && placement.Line == null &&
                        positions.ContainsKey(placement.A) && !Aligned(positions[placement.A], at) ? 1 : 0)
                    .ThenBy(at => SectorOf(origin, at))
                    .ThenBy(at => at.Q)
                    .ThenBy(at => at.R);
            }

            private string DescribeQuestion(MatchSessionProjection projection, PendingDecisionContract contract)
            {
                var chooser = projection.DecisionOwner.HeroId;
                var owner = chooser.IsDefined ? RefOfHero(chooser) : "the table";
                if (IsFamily(contract, PendingDecisionFamilies.Effect) && projection.PendingEffectChoice != null)
                {
                    var choice = projection.PendingEffectChoice;
                    return $"{choice.Kind} question of {owner} on {CardNameOf(projection, projection.ResolvingSourceCardInstanceId)} " +
                        $"(candidates: {string.Join(", ", choice.Candidates.Select(candidate => DescribeCandidate(projection, choice, candidate)))})";
                }

                return $"{contract.FamilyId} question of {owner} ({contract.Commands.Count} answers offered)";
            }

            private string CardNameOf(MatchSessionProjection projection, CardInstanceId card)
            {
                var instance = projection.Cards.FirstOrDefault(value => value.Id.Equals(card));
                return instance == null ? card.Value ?? "?" : DefinitionIn(projection, instance).DisplayName;
            }

            private string DescribeCandidate(MatchSessionProjection projection, PendingEffectChoice choice, EffectChoiceCandidate candidate)
            {
                if (candidate.Kind == EffectChoiceCandidateKind.Option) return "option " + candidate.OptionNodeId.Value;
                var value = candidate.Value;
                switch (value.Kind)
                {
                    case EffectValueKind.Hero: return RefOfHero(value.HeroId);
                    case EffectValueKind.Minion:
                        return pieces.FirstOrDefault(piece => piece.IsMinion && piece.Minion.Equals(value.MinionId))?.Ref ?? value.MinionId.Value;
                    case EffectValueKind.CardInstance: return "card " + CardNameOf(projection, value.CardInstanceId);
                    case EffectValueKind.Coordinate:
                        var here = pieces.FirstOrDefault(piece => PositionOf(projection, piece.Ref).Equals(value.Coordinate));
                        return here != null ? $"space of {here.Ref}" : "space " + value.Coordinate;
                    case EffectValueKind.Integer:
                        return choice.ValueVocabulary == EffectChoiceValueVocabulary.HexDirection
                            ? "direction " + (HexDirection)value.IntegerValue
                            : value.IntegerValue.ToString(CultureInfo.InvariantCulture);
                    default: return value.Kind.ToString();
                }
            }

            private Refusal NotCovered(MatchSessionProjection projection, PendingDecisionContract contract, string answer) =>
                Refuse(answer == null
                    ? $"the answers do not cover the {DescribeQuestion(projection, contract)}"
                    : $"answer \"{answer}\" does not answer the {DescribeQuestion(projection, contract)}");

            private DirectorCommand Answer(MatchSessionProjection projection, PendingDecisionContract contract)
            {
                var commands = contract.Commands;
                var family = contract.FamilyId;
                var next = Peek();
                if (isDefend && projection.DecisionOwner.HeroId.Equals(attacker) &&
                    !family.Equals(PendingDecisionFamilies.Defense))
                {
                    // The attacker's own questions: the attack names the actor, everything else the doing way.
                    var atActor = commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.ChooseAttackTarget &&
                        command.HeroId.Equals(actor));
                    return atActor ?? commands.OrderBy(OfferOutcomeTests.Doing).First();
                }
                if (family.Equals(PendingDecisionFamilies.Movement)) return AnswerMovement(projection, contract, next);
                if (family.Equals(PendingDecisionFamilies.AttackTarget)) return AnswerAttackTarget(projection, contract, next);
                if (family.Equals(PendingDecisionFamilies.Defense)) return AnswerDefense(projection, contract, next);
                if (family.Equals(PendingDecisionFamilies.Effect)) return AnswerEffect(projection, contract, next);
                if (family.Equals(PendingDecisionFamilies.RemovalPrevention)) return AnswerRemovalPrevention(projection, contract, next);
                if (family.Equals(PendingDecisionFamilies.Initiative))
                {
                    return commands.First();
                }

                // p.19's displaced-minion correction (a pushed or placed minion returned to the Battle
                // Zone): the table never names where; the first space offered is taken, as documented.
                if (family.Equals(PendingDecisionFamilies.Correction))
                {
                    return commands.First();
                }

                throw Refuse($"unsupported question: the {DescribeQuestion(projection, contract)}");
            }

            private DirectorCommand Consume(DirectorCommand command, string reference = null)
            {
                Dequeue();
                if (reference != null) lastReferenced = reference;
                return command;
            }

            private DirectorCommand AnswerMovement(MatchSessionProjection projection, PendingDecisionContract contract, string next)
            {
                var choice = projection.PendingMovementChoice;
                var maximum = choice.LegalDestinations.Select(destination => destination.Cost).DefaultIfEmpty(0).Max();
                maximumMovementOffered.TryGetValue(choice.MovingHeroId, out var previousMaximum);
                maximumMovementOffered[choice.MovingHeroId] = Math.Max(previousMaximum, maximum);
                var stop = contract.Commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.StopMovement);
                var shape = ShapeOf(next, out var value, out _);
                var moverRef = RefOfHero(choice.MovingHeroId);
                Log($"    walk from {choice.Origin}, {choice.RemainingAllowance} left: " + string.Join(" ",
                    choice.LegalDestinations.Select(destination => $"{destination.Destination}:{destination.Cost}")));
                var positions = Positions(projection, byRef.Keys.Concat(new[] { moverRef }));
                IEnumerable<Movement.Destination> destinations = choice.LegalDestinations
                    .Select(destination => new Movement.Destination(destination.Destination, destination.Cost));
                DirectorCommand Go(Movement.Destination destination, int consumed)
                {
                    for (var index = 0; index < consumed; index++) Dequeue();
                    walked.TryGetValue(moverRef, out var sum);
                    walked[moverRef] = sum + destination.Cost;
                    fulfilledWalk = (choice.ResolutionId.Value, choice.MovingHeroId);
                    return contract.Commands.First(command => command.Kind == DirectorCommandKind.ChooseMovementDestination &&
                        command.Coordinate.Equals(destination.At));
                }

                if (fulfilledWalk.HasValue && fulfilledWalk.Value.Resolution == choice.ResolutionId.Value &&
                    fulfilledWalk.Value.Mover.Equals(choice.MovingHeroId) && stop != null &&
                    shape != AnswerShape.Distance && shape != AnswerShape.Space)
                {
                    return stop;
                }

                switch (shape)
                {
                    case AnswerShape.Decline when stop != null:
                        return Consume(stop);
                    case AnswerShape.Distance when value == "0" && stop != null:
                        return Consume(stop);
                    case AnswerShape.Distance:
                    {
                        var distance = int.Parse(value, CultureInfo.InvariantCulture);
                        var fitting = destinations.Where(destination => destination.Cost == distance).ToList();
                        var following = answers.Count > 1 ? answers.ElementAt(1) : null;
                        var followingShape = following == null ? AnswerShape.None : ShapeOf(following, out var toward, out _);
                        if (fitting.Count == 0) throw NotCovered(projection, contract, next);
                        if (followingShape == AnswerShape.Toward || followingShape == AnswerShape.Away)
                        {
                            ShapeOf(following, out var target, out _);
                            var at = positions.ContainsKey(target) ? positions[target] : throw Refuse($"answer \"{following}\": {target} is not on the board");
                            var best = fitting.OrderBy(destination => OnFile(choice.Origin, at, destination.At, followingShape == AnswerShape.Away) ? 0 : 1)
                                .ThenBy(destination => (followingShape == AnswerShape.Away ? -1 : 1) * Dist(destination.At, at))
                                .ThenBy(destination => destination.At.Q).ThenBy(destination => destination.At.R).First();
                            return Go(best, 2);
                        }

                        var canonical = fitting.OrderByDescending(destination => Dist(destination.At, choice.Origin))
                            .ThenBy(destination => SectorOf(choice.Origin, destination.At))
                            .ThenBy(destination => destination.At.Q).ThenBy(destination => destination.At.R).First();
                        return Go(canonical, 1);
                    }
                    case AnswerShape.Toward:
                    case AnswerShape.Away:
                    {
                        if (!positions.ContainsKey(value)) throw Refuse($"answer \"{next}\": {value} is not on the board");
                        var target = positions[value];
                        var sign = shape == AnswerShape.Away ? -1 : 1;
                        var paired = answers.Count > 1 ? answers.ElementAt(1) : null;
                        if (paired != null && ShapeOf(paired, out var walk, out _) == AnswerShape.Distance)
                        {
                            var length = int.Parse(walk, CultureInfo.InvariantCulture);
                            var walked = destinations.Where(destination => destination.Cost == length)
                                .OrderBy(destination => OnFile(choice.Origin, target, destination.At, sign < 0) ? 0 : 1)
                                .ThenBy(destination => sign * Dist(destination.At, target))
                                .ThenBy(destination => destination.At.Q).ThenBy(destination => destination.At.R)
                                .FirstOrDefault();
                            if (walked == null) throw NotCovered(projection, contract, $"{next}\", \"{paired}");
                            return Go(walked, 2);
                        }

                        var best = destinations.OrderBy(destination => OnFile(choice.Origin, target, destination.At, sign < 0) ? 0 : 1)
                            .ThenBy(destination => sign * Dist(destination.At, target))
                            .ThenBy(destination => destination.Cost)
                            .ThenBy(destination => destination.At.Q).ThenBy(destination => destination.At.R)
                            .FirstOrDefault();
                        if (best == null) throw NotCovered(projection, contract, next);
                        return Go(best, 1);
                    }
                    case AnswerShape.Space:
                    {
                        var placement = ParsePlacement(value) ?? throw Unsupported("placement", value);
                        positions = Positions(projection, byRef.Keys.Concat(placement.Refs()));
                        var fitting = destinations.Where(destination => Satisfies(placement, destination.At, positions,
                            space => !Occupied(projection, space))).ToList();
                        var best = Ordered(fitting.Select(destination => destination.At), placement, positions, null, false)
                            .Select(at => fitting.First(destination => destination.At.Equals(at)))
                            .FirstOrDefault();
                        if (best == null) throw NotCovered(projection, contract, next);
                        return Go(best, 1);
                    }
                }

                if (contract.Commands.Count == 1) return contract.Commands[0];
                throw NotCovered(projection, contract, next);
            }

            private DirectorCommand AnswerAttackTarget(MatchSessionProjection projection, PendingDecisionContract contract, string next)
            {
                var choice = projection.PendingAttackTargetChoice;
                targetQuestions.Add(new HashSet<string>(choice.LegalTargets.Select(target =>
                    target.Kind == GuardsDigital.Core.Rules.CardResolution.AttackTargetKind.Hero
                        ? RefOfHero(target.HeroId)
                        : pieces.FirstOrDefault(piece => piece.IsMinion && piece.Minion.Equals(target.MinionId))?.Ref ?? target.MinionId.Value),
                    StringComparer.Ordinal));
                var shape = ShapeOf(next, out var value, out _);
                if (shape == AnswerShape.Reference)
                {
                    var heroId = HeroOfRef(value);
                    var minion = byRef.TryGetValue(value, out var piece) && piece.IsMinion ? piece.Minion : default;
                    var command = contract.Commands.FirstOrDefault(candidate => candidate.Kind == DirectorCommandKind.ChooseAttackTarget &&
                        (heroId.IsDefined && candidate.HeroId.Equals(heroId) || minion.IsDefined && candidate.MinionId.Equals(minion)));
                    if (command != null) return Consume(command, value);
                }

                if (contract.Commands.Count == 1) return contract.Commands[0];
                throw NotCovered(projection, contract, next);
            }

            private DirectorCommand AnswerDefense(MatchSessionProjection projection, PendingDecisionContract contract, string next)
            {
                var choice = projection.PendingDefenseChoice;
                var chooserRef = RefOfHero(choice.ChooserId);
                defenseQuestions[chooserRef] = new HashSet<string>(choice.LegalDefenseOffers.Select(offer =>
                    CardKey(projection.Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference)).DisplayName)),
                    StringComparer.Ordinal);
                if (isDefend && choice.ChooserId.Equals(actor))
                {
                    defendOffered = choice.LegalDefenseOffers.Any(offer => offer.SourceCardInstanceId.Equals(caseInstance.Id) &&
                        (caseAction == null || caseAction.Origin != ActionDefinitionOrigin.PrintedPrimary ||
                            offer.ActionDefinitionId.Equals(caseAction.Id)));
                }
                var decline = contract.Commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.DeclineDefense);
                DirectorCommand ByValue(object wanted, string phrase)
                {
                    if (wanted is string none && Normalize(none) == "none")
                    {
                        return decline ?? throw Refuse($"{phrase}: the defender cannot decline");
                    }

                    var offers = choice.LegalDefenseOffers.Where(offer =>
                    {
                        var definition = projection.Definitions.Single(value => value.Reference.Equals(offer.SourceDefinitionReference));
                        return wanted is string cardName
                            ? CardKey(definition.DisplayName) == CardKey(cardName)
                            : DefenseValueOf(definition, offer.ActionDefinitionId) == IntOf(wanted, phrase);
                    }).OrderBy(offer => offer.SourceCardInstanceId.Value, StringComparer.Ordinal).ToList();
                    if (offers.Count == 0) throw Refuse($"{phrase}: {chooserRef} has no Defense card offered with that value");
                    return contract.Commands.First(command => command.Kind == DirectorCommandKind.ChooseDefenseCard &&
                        command.CardInstanceId.Equals(offers[0].SourceCardInstanceId));
                }

                if (refSetups.TryGetValue(chooserRef, out var refSpec) && refSpec.Has("defends-with"))
                {
                    defendsWithAsked.Add(chooserRef);
                    return ByValue(refSpec.Get("defends-with"), $"setup {chooserRef}.defends-with");
                }

                var shape = ShapeOf(next, out var value, out _);
                if (shape == AnswerShape.Defense)
                {
                    object wanted = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                        ? (object)(double)number
                        : value;
                    var command = ByValue(wanted, $"answer \"{next}\"");
                    Dequeue();
                    return command;
                }

                if (isDefend && choice.ChooserId.Equals(actor))
                    return defendOffered == true ? ByValue(caseCard.DisplayName, "case Defense") : decline;

                throw NotCovered(projection, contract, next);
            }

            private DirectorCommand AnswerRemovalPrevention(MatchSessionProjection projection, PendingDecisionContract contract, string next)
            {
                var shape = ShapeOf(next, out var value, out var who);
                if (shape == AnswerShape.Decline || shape == AnswerShape.RefDefeat)
                {
                    var decline = contract.Commands.FirstOrDefault(command => command.RemovalPrevention != null && !command.RemovalPrevention.Accepted);
                    if (decline != null) return Consume(decline);
                }

                if (shape == AnswerShape.Card || shape == AnswerShape.RefDiscard)
                {
                    var accept = contract.Commands.FirstOrDefault(command => command.RemovalPrevention != null &&
                        command.RemovalPrevention.Accepted && command.RemovalPrevention.PaymentCardInstanceId.IsDefined &&
                        CardKey(CardNameOf(projection, command.RemovalPrevention.PaymentCardInstanceId)) == CardKey(value));
                    if (accept != null) return Consume(accept);
                }

                if (contract.Commands.Count == 1) return contract.Commands[0];
                throw NotCovered(projection, contract, next);
            }

            private DirectorCommand EffectCommand(PendingDecisionContract contract, EffectChoiceCommand invocation) =>
                invocation == null
                    ? null
                    : contract.Commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        command.EffectChoice.Equals(invocation));

            private DirectorCommand AnswerEffect(MatchSessionProjection projection, PendingDecisionContract contract, string next)
            {
                var choice = projection.PendingEffectChoice;
                var shape = ShapeOf(next, out var value, out var who);
                var decline = EffectCommand(contract, choice.DeclineInvocation) ?? EffectCommand(contract, choice.StopInvocation);
                var confirm = EffectCommand(contract, choice.ConfirmInvocation);
                var chooserRef = RefOfHero(choice.ChooserId);
                if (shape == AnswerShape.RefDecline)
                {
                    if (who != chooserRef || decline == null) throw NotCovered(projection, contract, next);
                    return Consume(decline);
                }

                if (choice.Kind == EffectChoiceKind.SelectOption)
                {
                    var source = projection.Cards.FirstOrDefault(card => card.Id.Equals(projection.ResolvingSourceCardInstanceId));
                    optionQuestions.Add((source == null ? caseCard : DefinitionIn(projection, source), choice.NodeId,
                        new HashSet<EffectNodeId>(choice.Candidates
                            .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option &&
                                !choice.UnavailableCandidates.Contains(candidate))
                            .Select(candidate => candidate.OptionNodeId)),
                        choice.Candidates.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option).ToList()));
                }

                if (choice.Kind == EffectChoiceKind.SelectTarget)
                {
                    var offeredSpaces = choice.Candidates
                        .Where(candidate => !choice.UnavailableCandidates.Contains(candidate) &&
                            candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Coordinate)
                        .Select(candidate => candidate.Value.Coordinate).ToArray();
                    if (offeredSpaces.Length > 0)
                        offeredEffectDestinations.Add(new HashSet<HexCoordinate>(offeredSpaces));
                    targetQuestions.Add(new HashSet<string>(choice.Candidates
                        .Where(candidate => !choice.UnavailableCandidates.Contains(candidate))
                        .Select(candidate => DescribeCandidate(projection, choice, candidate)
                            .Replace("space of ", string.Empty)), StringComparer.Ordinal));
                }

                // An opponent's own which-card question (the discard its hero must make) that the table
                // does not answer names the first card in that hand: the recipe's documented default.
                // It is any other hero's own choice of one of its cards: an enemy's discard, or the card a
                // friendly hero retrieves when the actor's text lets it (Wuk's Tree of Plenty).
                var opponent = !choice.ChooserId.Equals(actor) &&
                    setup.Heroes.Any(seat => seat.HeroId.Equals(choice.ChooserId));
                var named = (shape == AnswerShape.RefDiscard || shape == AnswerShape.RefDefeat) && who == chooserRef ||
                    shape == AnswerShape.Card;
                if (opponent && !named && choice.Kind == EffectChoiceKind.SelectTarget && choice.Candidates.Count > 0 &&
                    choice.Candidates.All(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                        candidate.Value.Kind == EffectValueKind.CardInstance))
                {
                    var hand = projection.Cards.Where(card => card.OwnerId.Equals(choice.ChooserId))
                        .OrderBy(card => card.State.Location == CardLocation.Hand ? 0 : 1)
                        .Select(card => card.Id).ToList();
                    var first = choice.SelectInvocations
                        .Where(invocation => hand.Contains(invocation.Candidate.Value.CardInstanceId))
                        .OrderBy(invocation => hand.IndexOf(invocation.Candidate.Value.CardInstanceId))
                        .FirstOrDefault() ?? choice.SelectInvocations.FirstOrDefault();
                    var command = EffectCommand(contract, first);
                    if (command != null)
                    {
                        Log($"  (default) {chooserRef} chooses {CardNameOf(projection, first.Candidate.Value.CardInstanceId)}, its first card");
                        return command;
                    }
                }

                // A reference-prefixed answer speaks only for that hero's own question.
                if ((shape == AnswerShape.RefDiscard || shape == AnswerShape.RefDefeat) && who != chooserRef)
                {
                    throw NotCovered(projection, contract, next);
                }

                if (choice.Kind == EffectChoiceKind.ConfirmOrDecline)
                {
                    if (shape == AnswerShape.Confirm && confirm != null) return Consume(confirm);
                    if ((shape == AnswerShape.Decline || shape == AnswerShape.RefDefeat) && decline != null) return Consume(decline);
                    if (confirm != null) return confirm;
                    throw NotCovered(projection, contract, next);
                }

                if (shape == AnswerShape.Decline || shape == AnswerShape.RefDefeat)
                {
                    // A selection already at its maximum closes by itself; the decline is for a later question.
                    if (choice.SelectedCandidates.Count >= choice.MaximumSelections && confirm != null) return confirm;
                    if (choice.SelectedCandidates.Count > 0 && confirm != null) return Consume(confirm);
                    if (decline != null) return Consume(decline);
                    throw NotCovered(projection, contract, next);
                }

                var selected = SelectFor(projection, choice, shape, value);
                if (selected.HasValue)
                {
                    var invocation = choice.SelectInvocations.FirstOrDefault(candidate => candidate.Candidate.Equals(selected.Value));
                    var command = EffectCommand(contract, invocation);
                    if (command != null)
                    {
                        Dequeue();
                        for (; extraConsumed > 0; extraConsumed--) Dequeue();
                        if (shape == AnswerShape.Reference) lastReferenced = value;
                        return command;
                    }

                    extraConsumed = 0;
                }

                // "Choose one or both": the second bullet may be answered later in the list, after the
                // answers the first bullet's steps ask; it is selected now, where the choice asks it.
                if (choice.Kind == EffectChoiceKind.SelectOption && choice.SelectedCandidates.Count > 0)
                {
                    var queued = answers.ToList();
                    for (var index = 0; index < queued.Count; index++)
                    {
                        if (ShapeOf(queued[index], out var later, out _) != AnswerShape.Option) continue;
                        EffectChoiceCandidate? arm = null;
                        try { arm = SelectFor(projection, choice, AnswerShape.Option, later); } catch (Refusal) { }
                        var invocation = arm.HasValue
                            ? choice.SelectInvocations.FirstOrDefault(candidate => candidate.Candidate.Equals(arm.Value))
                            : null;
                        var command = EffectCommand(contract, invocation);
                        if (command == null) continue;
                        var rest = queued.Where((_, position) => position != index).ToList();
                        answers.Clear();
                        foreach (var answer in rest) answers.Enqueue(answer);
                        lastConsumed.Add(queued[index]);
                        return command;
                    }
                }

                // A choice whose selections are made, or which has only one answer, closes by itself.
                if (choice.SelectedCandidates.Count > 0 && confirm != null) return confirm;
                if (contract.Commands.Count == 1) return contract.Commands[0];
                throw NotCovered(projection, contract, next);
            }

            private EffectChoiceCandidate? SelectFor(
                MatchSessionProjection projection, PendingEffectChoice choice, AnswerShape shape, string value)
            {
                var open = choice.Candidates.Where(candidate => !choice.UnavailableCandidates.Contains(candidate) &&
                    !choice.SelectedCandidates.Contains(candidate)).ToList();
                if (shape == AnswerShape.RuneSlot || shape == AnswerShape.Rune)
                    return SelectRuneCandidate(projection, choice, open, shape, value);
                switch (shape)
                {
                    case AnswerShape.TextFace:
                    {
                        if (choice.ValueVocabulary != EffectChoiceValueVocabulary.PrintedTextFace) return null;
                        var face = value == "sun" ? PrintedTextFace.Sun : PrintedTextFace.Moon;
                        return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                                candidate.Value.Kind == EffectValueKind.Integer && candidate.Value.IntegerValue == (int)face)
                            .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                    }
                    case AnswerShape.Color:
                    {
                        if (choice.ValueVocabulary != EffectChoiceValueVocabulary.CardColor) return null;
                        var color = (CardColor)Enum.Parse(typeof(CardColor), value, true);
                        return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                                candidate.Value.Kind == EffectValueKind.Integer && candidate.Value.IntegerValue == (int)color)
                            .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                    }
                    case AnswerShape.Option:
                        return choice.Candidates.Any(candidate => candidate.Kind == EffectChoiceCandidateKind.Option)
                            ? OptionFor(projection, choice, open, value)
                            : null;
                    case AnswerShape.Reference:
                    {
                        var at = PositionOf(projection, value);
                        var heroId = HeroOfRef(value);
                        var minion = byRef.TryGetValue(value, out var piece) && piece.IsMinion ? piece.Minion : default;
                        var matches = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value && (
                            candidate.Value.Kind == EffectValueKind.Hero && heroId.IsDefined && candidate.Value.HeroId.Equals(heroId) ||
                            candidate.Value.Kind == EffectValueKind.Minion && minion.IsDefined && candidate.Value.MinionId.Equals(minion) ||
                            candidate.Value.Kind == EffectValueKind.Coordinate && at.HasValue && candidate.Value.Coordinate.Equals(at.Value)))
                            .ToList();
                        return matches.Count > 0 ? matches[0] : (EffectChoiceCandidate?)null;
                    }
                    case AnswerShape.Card:
                    case AnswerShape.RefDiscard:
                    {
                        var matches = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.CardInstance &&
                            CardKey(CardNameOf(projection, candidate.Value.CardInstanceId)) == CardKey(value)).ToList();
                        return matches.Count > 0 ? matches[0] : (EffectChoiceCandidate?)null;
                    }
                    case AnswerShape.Distance:
                    {
                        var number = int.Parse(value, CultureInfo.InvariantCulture);
                        var matches = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Integer &&
                            choice.ValueVocabulary == EffectChoiceValueVocabulary.None &&
                            candidate.Value.IntegerValue == number).ToList();
                        if (matches.Count > 0) return matches[0];

                        // A "move up to N" asked as a choice of space: the space the walk of D ends on,
                        // measured from the piece that moves - the actor or the piece the answers last
                        // named, whichever the offered spaces surround more closely (the actor on a tie).
                        // A "toward"/"away from" answer right after it picks among those spaces.
                        var spaces = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Coordinate).ToList();
                        if (spaces.Count == 0) return null;
                        var origin = new[] { "actor", lastReferenced }.Where(reference => reference != null)
                            .Select(reference => PositionOf(projection, reference))
                            .Where(at => at.HasValue).Select(at => at.Value)
                            .OrderBy(at => spaces.Max(candidate => Dist(candidate.Value.Coordinate, at)))
                            .First();
                        var fitting = spaces.Where(candidate => Dist(candidate.Value.Coordinate, origin) == number).ToList();
                        var paired = answers.Count > 1 ? answers.ElementAt(1) : null;
                        string pairedRef = null;
                        var pairedShape = paired == null ? AnswerShape.None : ShapeOf(paired, out pairedRef, out _);
                        if ((pairedShape == AnswerShape.Toward || pairedShape == AnswerShape.Away) && fitting.Count > 0)
                        {
                            var toward = PositionOf(projection, pairedRef) ?? throw Refuse($"answer \"{paired}\": {pairedRef} is not on the board");
                            extraConsumed = 1;
                            return fitting.OrderBy(candidate => (pairedShape == AnswerShape.Away ? -1 : 1) * Dist(candidate.Value.Coordinate, toward))
                                .ThenBy(candidate => candidate.Value.Coordinate.Q).ThenBy(candidate => candidate.Value.Coordinate.R)
                                .First();
                        }

                        return fitting.OrderBy(candidate => SectorOf(origin, candidate.Value.Coordinate))
                            .ThenBy(candidate => candidate.Value.Coordinate.Q).ThenBy(candidate => candidate.Value.Coordinate.R)
                            .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                    }
                    case AnswerShape.Toward:
                    case AnswerShape.Away:
                    {
                        var target = PositionOf(projection, value) ?? throw Refuse($"answer \"{Peek()}\": {value} is not on the board");
                        var sign = shape == AnswerShape.Away ? -1 : 1;
                        if (choice.ValueVocabulary == EffectChoiceValueVocabulary.HexDirection)
                        {
                            var subject = PositionOf(projection, lastReferenced ?? "actor") ?? PositionOf(projection, "actor").Value;
                            var direction = shape == AnswerShape.Toward || !subject.Equals(target)
                                ? (shape == AnswerShape.Toward ? SectorOf(subject, target) : SectorOf(target, subject))
                                : -1;
                            return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                                    candidate.Value.Kind == EffectValueKind.Integer && candidate.Value.IntegerValue == direction)
                                .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                        }

                        var spaces = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Coordinate).ToList();
                        var paired = answers.Count > 1 ? answers.ElementAt(1) : null;
                        if (spaces.Count > 0 && paired != null && ShapeOf(paired, out var walk, out _) == AnswerShape.Distance)
                        {
                            // "toward X", "distance D": one walk of D, ending nearest (or farthest from) X,
                            // measured from the piece that moves as a lone "distance D" is.
                            var length = int.Parse(walk, CultureInfo.InvariantCulture);
                            var mover = new[] { "actor", lastReferenced }.Where(reference => reference != null)
                                .Select(reference => PositionOf(projection, reference))
                                .Where(at => at.HasValue).Select(at => at.Value)
                                .OrderBy(at => spaces.Max(candidate => Dist(candidate.Value.Coordinate, at)))
                                .First();
                            spaces = spaces.Where(candidate => Dist(candidate.Value.Coordinate, mover) == length).ToList();
                            if (spaces.Count == 0) return null;
                            extraConsumed = 1;
                        }

                        return spaces.OrderBy(candidate => sign * Dist(candidate.Value.Coordinate, target))
                            .ThenBy(candidate => candidate.Value.Coordinate.Q).ThenBy(candidate => candidate.Value.Coordinate.R)
                            .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
                    }
                    case AnswerShape.Space:
                    {
                        var zoneClause = Regex.Match(value, @"^(.*?) in zone of (actor|(?:enemy|friendly)-(?:hero|minion)-[1-9][0-9]*)$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                        var placementText = zoneClause.Success ? zoneClause.Groups[1].Value : value;
                        var placement = ParsePlacement(placementText) ?? throw Unsupported("placement", value);
                        var positions = Positions(projection, byRef.Keys.Concat(placement.Refs()));
                        ZoneId? targetZone = null;
                        if (zoneClause.Success)
                        {
                            var zoneRef = zoneClause.Groups[2].Value.ToLowerInvariant();
                            var zoneAt = PositionOf(projection, zoneRef) ??
                                throw Refuse("zone-scoped placement reference " + zoneRef + " is not on the board");
                            targetZone = Map.Value.Spaces.Single(space => space.Coordinate.Equals(zoneAt)).ZoneId;
                            if (!targetZone.HasValue)
                                throw Refuse("zone-scoped placement reference " + zoneRef + " has no zone");
                        }
                        var spaces = open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Coordinate &&
                            Satisfies(placement, candidate.Value.Coordinate, positions, space => !Occupied(projection, space)) &&
                            (!targetZone.HasValue || Map.Value.Spaces.Any(space =>
                                space.Coordinate.Equals(candidate.Value.Coordinate) &&
                                space.ZoneId.HasValue && space.ZoneId.Value.Equals(targetZone.Value)))).ToList();
                        var best = Ordered(spaces.Select(candidate => candidate.Value.Coordinate), placement, positions, null, false)
                            .Select(at => (HexCoordinate?)at).FirstOrDefault();
                        return best.HasValue
                            ? spaces.First(candidate => candidate.Value.Coordinate.Equals(best.Value))
                            : (EffectChoiceCandidate?)null;
                    }
                    default:
                        return null;
                }
            }

            /// <summary>
            /// "option: &lt;printed bullet text&gt;": the bullet is found in the card's printed text by
            /// its words, and its position among the bullets names the Choose node's arm in printed
            /// order; a declared decision label naming the same words is accepted as well.
            /// </summary>
            private EffectChoiceCandidate? OptionFor(
                MatchSessionProjection projection, PendingEffectChoice choice, List<EffectChoiceCandidate> open, string text)
            {
                var source = projection.Cards.FirstOrDefault(card => card.Id.Equals(projection.ResolvingSourceCardInstanceId));
                var definition = source == null ? caseCard : DefinitionIn(projection, source);
                var options = choice.Candidates.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option).ToList();
                var arm = ArmFor(definition, choice.NodeId, text, options);
                return open.Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option &&
                        candidate.OptionNodeId.Equals(arm.Value))
                    .Select(candidate => (EffectChoiceCandidate?)candidate).FirstOrDefault();
            }

            private EffectNodeId? ArmFor(
                CardDefinition definition, EffectNodeId choiceNode, string text, List<EffectChoiceCandidate> options)
            {
                // The arms in printed order: the children of the node that holds every offered option
                // (the asking node itself, or the Choose node a repeated choice re-asks).
                var offered = options?.Select(candidate => candidate.OptionNodeId).ToList() ?? new List<EffectNodeId>();
                var nodes = definition.ActionDefinitions.Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes).ToList();
                var asking = nodes.FirstOrDefault(node => node.Id.Equals(choiceNode));
                var holder = asking != null && offered.All(asking.ChildNodeIds.Contains)
                    ? asking
                    : nodes.FirstOrDefault(node => node.ChildNodeIds.Count > 1 && offered.Count > 0 &&
                        offered.All(node.ChildNodeIds.Contains));
                var arms = holder?.ChildNodeIds.ToList() ?? offered;
                var wanted = Words(text);
                var bullets = Bullets(definition.SourceContent?.PrintedText ?? string.Empty).Select(Words).ToList();
                var hits = bullets.Select((bullet, index) => (bullet, index))
                    .Where(pair => pair.bullet.Length > 0 && (pair.bullet.StartsWith(wanted, StringComparison.Ordinal) ||
                        wanted.StartsWith(pair.bullet, StringComparison.Ordinal)))
                    .Select(pair => pair.index)
                    .ToList();
                EffectNodeId? arm = null;
                if (hits.Count == 1 && hits[0] < arms.Count) arm = arms[hits[0]];
                if (arm == null && options != null)
                {
                    var labelled = options.Where(candidate =>
                        DecisionLabelCatalog.TryGetLabel(definition.Id, candidate.OptionNodeId, out var label) &&
                        Words(label.Chosen) == wanted).ToList();
                    if (labelled.Count == 1) arm = labelled[0].OptionNodeId;
                }

                if (arm == null)
                {
                    throw Refuse($"\"{text}\" names no single bullet of \"{definition.DisplayName}\"" +
                        $" (bullets: {string.Join(" | ", bullets)}; offered arms: {string.Join(", ", arms.Select(value => value.Value))})");
                }

                return arm;
            }

            /// <summary>
            /// A face's printed bullets: after '•' marks, or - where the transcription writes a
            /// "Choose ... -" clause with slash-separated or consecutively numbered arms - those arms.
            /// </summary>
            private static List<string> Bullets(string printed)
            {
                if (printed.IndexOf('\u2022') >= 0) return printed.Split('\u2022').Skip(1).ToList();
                var clause = Regex.Match(printed, "choose[^-\u2014\u2013.]*[-\u2014\u2013]\\s*(.*)$",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
                if (!clause.Success) return new List<string>();
                var body = clause.Groups[1].Value;
                if (body.StartsWith("(1)", StringComparison.Ordinal))
                {
                    var marks = Regex.Matches(body, @"\(([1-9][0-9]*)\)\s*");
                    if (marks.Count > 1 && marks.Cast<Match>().Select((mark, index) =>
                            mark.Groups[1].Value == (index + 1).ToString(CultureInfo.InvariantCulture)).All(value => value))
                        return marks.Cast<Match>().Select((mark, index) => body.Substring(mark.Index + mark.Length,
                            (index + 1 < marks.Count ? marks[index + 1].Index : body.Length) - mark.Index - mark.Length)).ToList();
                }
                return body.Split(new[] { " / " }, StringSplitOptions.None).ToList();
            }

            private static string Words(string text) =>
                Regex.Replace(Regex.Replace((text ?? string.Empty).ToLowerInvariant(), "\\[[^\\]]*\\]", " "),
                    "[^a-z0-9]+", " ").Trim();

            // ------------------------------------------------------------------ the round end

            private static bool NeedsTheRoundEnd(JsonObject expectation) =>
                expectation.Has("on-board-after-round-end") || expectation.Has("minion-battle") ||
                expectation.Has("lane-pushed");

            private void RunTheRoundEnd(bool stopBeforeMinionBattle = false, bool stopAfterTurn = false)
            {
                var round = session.Projection.RoundIndex;
                var turn = session.Projection.TurnIndex;
                var preBattleStarted = false;
                var checkpointReached = false;
                var mark = session.Projection.MatchLog.Select(value => value.Sequence).DefaultIfEmpty(0).Max();
                var turnsCommitted = new Dictionary<HeroId, int>();
                for (var step = 0; step < 1000; step++)
                {
                    var projection = session.Projection;
                    if (stopAfterTurn && projection.TurnIndex != turn && projection.DecisionContract == null)
                    {
                        checkpointReached = true;
                        break;
                    }
                    if (stopBeforeMinionBattle && preBattleStarted &&
                        projection.Phase == RoundResolutionPhase.EndOfRound &&
                        projection.CurrentEndOfRoundStage == EndOfRoundStage.MinionBattle &&
                        projection.DecisionContract == null && projection.PendingMinionBattleChoice == null)
                    {
                        checkpointReached = true;
                        break;
                    }
                    if (projection.Phase == RoundResolutionPhase.GameEnded || projection.RoundIndex != round) break;
                    DirectorCommand next = null;
                    if (projection.Phase == RoundResolutionPhase.AwaitingCommitments && projection.DecisionContract == null)
                    {
                        var asked = projection.Commitments.First(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                        var card = projection.Cards
                            .Where(value => value.OwnerId.Equals(asked.HeroId) && value.State.Location == CardLocation.Hand)
                            .OrderBy(value => DefinitionIn(projection, value).Initiative.Value)
                            .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                            .FirstOrDefault();
                        next = card != null ? DirectorCommand.CommitCard(asked.HeroId, card.Id)
                            : asked.CanPass ? DirectorCommand.Pass(asked.HeroId) : null;
                    }
                    else if (projection.PendingTurnRelease != null && !IsFamily(projection.DecisionContract, PendingDecisionFamilies.Effect))
                    {
                        next = DirectorCommand.ReleaseTurn();
                    }
                    else if (projection.IsTurnActive && projection.SelectedActor.IsDefined && projection.DecisionContract == null &&
                        projection.AvailableHoldOffer.HasValue && projection.AvailableHoldOffer.Value.IsAvailable)
                    {
                        next = DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId);
                    }
                    else if (projection.DecisionContract != null && answers.Count > 0 &&
                        (projection.DecisionOwner.HeroId.Equals(actor) ||
                         ShapeOf(Peek(), out _, out var answerOwner) != AnswerShape.None &&
                         answerOwner != null && projection.DecisionOwner.HeroId.IsDefined &&
                         answerOwner == RefOfHero(projection.DecisionOwner.HeroId)) && projection.PendingMinionBattleChoice == null)
                    {
                        // The actor's own question at the round's end (Wuk's Claim Dominance choosing the
                        // minions that do not count, WU-Q3) is answered from the table like any other.
                        next = Answer(projection, projection.DecisionContract);
                        Log($"  round end asked: {DescribeQuestion(projection, projection.DecisionContract)} -> {next.Kind}");
                    }
                    else if (projection.PendingMinionBattleChoice != null)
                    {
                        var battle = projection.PendingMinionBattleChoice;
                        minionBattleWinner = battle.ChoosingSide == TeamSide.Red ? "blue" : "red";
                        next = battle.SelectInvocations.Count > 0 && battle.SelectedUnits.Count < battle.RequiredRemovalCount
                            ? DirectorCommand.SelectMinionBattleRemoval(battle.SelectInvocations[0])
                            : DirectorCommand.ConfirmMinionBattleRemovals(battle.ConfirmInvocation);
                    }

                    if (next == null)
                    {
                        var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                        if (legal.Count == 0) throw Refuse($"the round end stalled ({projection.Phase})");
                        next = legal[0];
                    }

                    var visitsPreBattle = next.Kind == DirectorCommandKind.AdvanceEndOfRound &&
                        projection.Phase == RoundResolutionPhase.EndOfRound &&
                        projection.CurrentEndOfRoundStage == EndOfRoundStage.MinionBattle;
                    var isCardTextFastTravel = projection.PendingEffectChoice?.NodeId.Value == "late-travel";
                    var travelFrom = isCardTextFastTravel ? PositionOf(projection, "actor") : null;
                    if (isCardTextFastTravel) cardTextFastTravelOffered = true;
                    var result = session.Execute(next);
                    if (!result.Accepted) throw Refuse($"{next.Kind} refused while running the round end: {result.Detail}");
                    if (isCardTextFastTravel && travelFrom.HasValue &&
                        !PositionOf(session.Projection, "actor").Equals(travelFrom))
                        cardTextFastTravelCommitted = true;
                    if (visitsPreBattle) preBattleStarted = true;
                }

                afterRound = session.Projection;
                if (stopBeforeMinionBattle || stopAfterTurn)
                {
                    if (!checkpointReached) throw Refuse("requested timing checkpoint was not reached before the round ended");
                    Log(stopAfterTurn ? "end of turn: all delayed questions resolved before next commitments" :
                        "round end: resolved trigger choices before minion battle; no battle or pity coins applied");
                    return;
                }
                if (minionBattleWinner == null)
                {
                    var removed = afterRound.MatchLog.Where(value => value.Sequence > mark &&
                        value.Kind == MatchLogEntryKind.MinionBattleMinionRemoved).ToList();
                    var sides = removed.Select(value => setup.Minions.FirstOrDefault(minion => minion.MinionId.Equals(value.SubjectMinionId))?.TeamSide)
                        .Where(side => side.HasValue).Select(side => side.Value).Distinct().ToList();
                    minionBattleWinner = sides.Count == 1 ? (sides[0] == TeamSide.Red ? "blue" : "red") : "tie";
                }

                Log($"round end: minion battle {minionBattleWinner}");
            }

            // ------------------------------------------------------------------ the expectations

            private List<JsonObject> ReadExpectations()
            {
                var list = entry.Get("expect");
                if (!(list is List<object> items) || items.Count == 0) throw Refuse("the case lists no \"expect\" facts");
                return items.Select(item => item as JsonObject ?? throw Unsupported("expectation", Describe(item))).ToList();
            }

            private void Mismatch(JsonObject expectation, string actual) =>
                Failures.Add($"{name}: expected {Describe(expectation)} - {actual}");

            private Piece PieceOf(string reference, JsonObject expectation)
            {
                if (byRef.TryGetValue(reference, out var piece)) return piece;
                if (HeroOfRef(reference).IsDefined)
                {
                    return new Piece { Ref = reference, Kind = reference.StartsWith("enemy", StringComparison.Ordinal) ? RefKind.EnemyHero : RefKind.FriendlyHero, Hero = HeroOfRef(reference) };
                }

                throw Refuse($"expectation {Describe(expectation)} names \"{reference}\", which the setup never placed");
            }

            private IEnumerable<MatchLogEntry> LogSince(MatchSessionProjection projection) =>
                projection.MatchLog.Where(value => value.Sequence > logBaseline);

            /// <summary>
            /// A Defeat is read from the match log and nowhere else. 9.13's smoke table had found the
            /// log silent on a Defeat inside a triggered effect (Wasp's High Voltage) and on a
            /// Removal made by card text (Ignatia's Abrupt Combustion) and read the scoreboard's
            /// Deaths and minion kills instead; milestone 9.15 put both on the log, so the fallback
            /// is retired: a Defeat the log does not record is a defect the table should surface.
            /// </summary>
            private bool Defeated(MatchSessionProjection projection, Piece piece) =>
                (piece.IsHero || piece.IsMinion) && DefeatLogged(projection, piece);

            private bool DefeatLogged(MatchSessionProjection projection, Piece piece) =>
                LogSince(projection).Any(value =>
                    piece.IsHero && value.Kind == MatchLogEntryKind.HeroDefeated && value.SubjectHeroId.Equals(piece.Hero) ||
                    piece.IsMinion && value.Kind == MatchLogEntryKind.MinionDefeated && value.SubjectMinionId.Equals(piece.Minion));

            private bool RemovalLogged(MatchSessionProjection projection, Piece piece) =>
                LogSince(projection).Any(value =>
                    piece.IsHero && value.Kind == MatchLogEntryKind.HeroRemoved && value.SubjectHeroId.Equals(piece.Hero) ||
                    piece.IsMinion && (value.Kind == MatchLogEntryKind.MinionRemoved ||
                        value.Kind == MatchLogEntryKind.MinionBattleMinionRemoved) && value.SubjectMinionId.Equals(piece.Minion));

            private static readonly HashSet<string> ExpectationKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "ref", "is", "at", "moved", "pushed", "coins", "hand", "attack", "level", "targetable", "chose",
                "attack-count", "movement-offered-max", "at-initial"
            };

            private void Evaluate(JsonObject expectation)
            {
                if (TryEvaluateSupplemental(expectation)) return;
                if (TryEvaluateRunes(expectation)) return;
                if (TryEvaluateBatchEFacts(expectation)) return;
                if (expectation.Has("owned-obstacle"))
                {
                    if (expectation.Count != 1 || !(expectation.Get("owned-obstacle") is JsonObject spec) ||
                        !(spec.Get("kind") is string kind) || string.IsNullOrWhiteSpace(kind) ||
                        !(spec.Get("owner") is string owner) ||
                        spec.Any(pair => pair.Key != "kind" && pair.Key != "owner" &&
                            pair.Key != "at" && pair.Key != "on-board") ||
                        spec.Has("at") == spec.Has("on-board"))
                        throw Unsupported("owned-obstacle expectation", Describe(expectation));
                    var id = OwnedObstacleIdentity.For(kind, OwnerOf(owner, "owned-obstacle"));
                    var found = after.Obstacles.Where(value => value.ObstacleId.Equals(id))
                        .Select(value => (HexCoordinate?)value.Coordinate).SingleOrDefault();
                    if (spec.Has("on-board"))
                    {
                        if (!(spec.Get("on-board") is bool wanted))
                            throw Unsupported("owned-obstacle expectation", Describe(expectation));
                        if (found.HasValue != wanted) Mismatch(expectation, "on-board is " + found.HasValue);
                    }
                    else
                    {
                        var phrase = spec.Get("at") as string;
                        var placement = ParsePlacement(phrase ?? "") ??
                            throw Unsupported("placement", phrase ?? "(none)");
                        if (!found.HasValue) Mismatch(expectation, "the owned obstacle is not on the board");
                        else if (!Satisfies(placement, found.Value, Positions(after, placement.Refs())))
                            Mismatch(expectation, "it is at " + found.Value);
                    }
                    return;
                }
                if (expectation.Get("ref") is string itemOwner && expectation.Get("item-count") is JsonObject itemCounts)
                {
                    if (expectation.Count != 2 || itemCounts.Count == 0) throw Unsupported("item-count expectation", Describe(expectation));
                    var progression = after.HeroProgression.Single(value => value.HeroId.Equals(HeroOfRef(itemOwner)));
                    foreach (var item in itemCounts)
                    {
                        var match = Regex.Match(item.Key, @"^(Attack|Defense|Movement|Initiative|Range) \+1$", RegexOptions.IgnoreCase);
                        if (!match.Success || !Enum.TryParse(match.Groups[1].Value, true, out PrintedValueChannel channel))
                            throw Unsupported("item-count expectation", Describe(expectation));
                        var actual = progression.ItemBonuses.Where(value => value.Key == channel).Sum(value => value.Value);
                        if (actual != IntOf(item.Value, "item-count")) Mismatch(expectation, item.Key + " count is " + actual);
                    }
                    return;
                }
                if (expectation.Get("ref") is string moving && expectation.Get("moved-through") is string crossed)
                {
                    if (expectation.Count != 2) throw Unsupported("moved-through expectation", Describe(expectation));
                    var restored = AuthoritativeSnapshotSerializer.Deserialize(MatchContentCatalogs.ForThisBuild(),
                        after.AuthoritativeSnapshot, after.Definitions, new[] { after.SelectedMap }, after.Setup.Minions);
                    var walk = restored.Board.LastCompletedMovementWalk;
                    var at = PositionOf(before, crossed);
                    if (walk == null || !walk.Mover.Equals(HeroOfRef(moving)) || !at.HasValue ||
                        !walk.SpacesMovedThrough.Contains(at.Value))
                        Mismatch(expectation, "the last completed Movement did not traverse the named initial space");
                    return;
                }
                if (expectation.Get("ref") is string defender && expectation.Get("defense-offered") is JsonObject wantedDefenses)
                {
                    if (expectation.Count != 2) throw Unsupported("expectation", Describe(expectation));
                    if (!defenseQuestions.TryGetValue(defender, out var offeredDefenses))
                    {
                        Mismatch(expectation, "no Defense question was asked of " + defender);
                        return;
                    }
                    foreach (var namedDefense in wantedDefenses)
                    {
                        if (!(namedDefense.Value is bool wanted)) throw Unsupported("expectation", Describe(expectation));
                        if (offeredDefenses.Contains(CardKey(namedDefense.Key)) != wanted)
                            Mismatch(expectation, namedDefense.Key + " offered: " + offeredDefenses.Contains(CardKey(namedDefense.Key)));
                    }
                    return;
                }
                if (expectation.Get("ref") is string holder && expectation.Has("marker"))
                {
                    var marker = expectation.Get("marker") as string;
                    if (expectation.Count != 2 || string.IsNullOrWhiteSpace(marker))
                        throw Unsupported("marker expectation", Describe(expectation));
                    if (!after.HeldMarkers.Any(value => value.HolderHeroId.Equals(HeroOfRef(holder)) &&
                        value.Definition.DisplayName.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
                        Mismatch(expectation, "no such marker is attached to " + holder);
                    return;
                }
                if (expectation.Get("ref") is string modified && expectation.Any(pair => pair.Key.EndsWith("-modifier", StringComparison.Ordinal)))
                {
                    var restored = AuthoritativeSnapshotSerializer.Deserialize(MatchContentCatalogs.ForThisBuild(),
                        after.AuthoritativeSnapshot, after.Definitions, new[] { after.SelectedMap }, after.Setup.Minions);
                    var modifications = RuleModificationQuery.Build(restored.Round.Timing, restored.Board, restored.Participants,
                        restored.Round.TieBreakerState, UltimateUnlockQuery.Derive(after.Definitions, restored.Cards),
                        RuleObject.None, restored.Round.HeroItems, restored.Round.Markers);
                    foreach (var modifierFact in expectation.Where(pair => pair.Key != "ref"))
                    {
                        if (!modifierFact.Key.EndsWith("-modifier", StringComparison.Ordinal) || !Enum.TryParse(
                            modifierFact.Key.Substring(0, modifierFact.Key.Length - 9), true, out PrintedValueChannel channel))
                            throw Unsupported("expectation", Describe(expectation));
                        var actual = modifications.ValueContributions(RuleObject.ForHero(HeroOfRef(modified)), channel,
                            RuleValueApplicability.Always).Sum(contribution => contribution.Amount);
                        if (actual != IntOf(modifierFact.Value, modifierFact.Key)) Mismatch(expectation, modifierFact.Key + " is " + actual);
                    }
                    return;
                }
                if (expectation.Has("swapped-with"))
                {
                    if (expectation.Count != 2 || !(expectation.Get("ref") is string subject) ||
                        !(expectation.Get("swapped-with") is string other) || subject == other)
                        throw Unsupported("swap expectation", Describe(expectation));
                    var original = PositionOf(before, subject);
                    var otherOriginal = PositionOf(before, other);
                    if (!original.HasValue || !otherOriginal.HasValue || original.Equals(otherOriginal))
                        throw Refuse("swap expectation requires two distinct initially on-board pieces");
                    if (!PositionOf(after, subject).Equals(otherOriginal) || !PositionOf(after, other).Equals(original))
                        Mismatch(expectation, "the two pieces did not exchange their initial positions");
                    return;
                }
                if (expectation.Has("respawned")) { EvaluateRespawn(expectation); return; }
                if (expectation.Has("tokens")) { EvaluateTokens(expectation); return; }
                if (expectation.Has("card")) { EvaluateCard(expectation); return; }
                if (expectation.Has("team-lives") || expectation.Has("enemy-team-lives")) { EvaluateLives(expectation); return; }
                if (expectation.Has("minion-battle")) { EvaluateMinionBattle(expectation); return; }
                if (expectation.Has("lane-pushed")) { EvaluateLanePush(expectation); return; }
                if (expectation.Has("bullet")) { EvaluateBullet(expectation); return; }
                if (!(expectation.Get("ref") is string reference) || expectation.Count != 2 ||
                    expectation.Any(pair => !ExpectationKeys.Contains(pair.Key)))
                {
                    if (expectation.Get("ref") is string && NewTokenReference(expectation)) return;
                    throw Unsupported("expectation", Describe(expectation));
                }

                var fact = expectation.First(pair => pair.Key != "ref");
                if (!byRef.ContainsKey(reference) && fact.Key == "at" && NewTokenReference(expectation)) return;
                var piece = PieceOf(reference, expectation);
                var now = PositionOf(after, reference);
                switch (fact.Key)
                {
                    case "is":
                        var state = Normalize(fact.Value as string ?? throw Unsupported("expectation", Describe(expectation)));
                        var defeated = Defeated(after, piece);
                        var gone = !now.HasValue;
                        switch (state)
                        {
                            case "defeated":
                                if (piece.IsTokenLike ? !gone : !defeated) Mismatch(expectation, piece.IsTokenLike ? $"the token is still at {now}" : "no Defeat was recorded");
                                break;
                            case "not defeated":
                                if (defeated || gone) Mismatch(expectation, defeated ? "a Defeat was recorded" : "it left the board");
                                break;
                            case "removed":
                                if (!gone || defeated) Mismatch(expectation, defeated ? "it was defeated, not removed" : $"it is still at {now}");
                                break;
                            case "immune":
                                throw Refuse($"unsupported expectation {Describe(expectation)}: immunity is a standing rule " +
                                    "the executor cannot read without naming the activity; express it as the targeting " +
                                    "outcome instead ({\"ref\": X, \"targetable\": false})");
                            default:
                                throw Unsupported("expectation", Describe(expectation));
                        }

                        break;
                    case "at":
                    case "at-initial":
                    case "pushed":
                    {
                        var placement = ParsePlacement(fact.Value as string) ?? throw Unsupported("placement", Describe(fact.Value));
                        var basis = fact.Key == "at-initial" ? before : after;
                        var positions = Positions(basis, byRef.Keys.Concat(placement.Refs()).Concat(new[] { reference }));
                        if (!now.HasValue) { Mismatch(expectation, "it is not on the board"); break; }
                        var missing = placement.Refs().FirstOrDefault(value => !positions.ContainsKey(value));
                        if (missing != null) { Mismatch(expectation, $"{missing} is not on the board"); break; }
                        if (!Satisfies(placement, now.Value, positions,
                            fact.Key == "at-initial" ? space => !Occupied(before, space) : (Func<HexCoordinate, bool>)null))
                        {
                            Mismatch(expectation, $"it is at {now.Value}; " + string.Join(", ",
                                placement.Refs().Select(value => $"{value} at {positions[value]} (distance {Dist(now.Value, positions[value])})")));
                        }
                        else if (fact.Key == "pushed" && PositionOf(before, reference).Equals(now))
                        {
                            Mismatch(expectation, "it did not move");
                        }

                        break;
                    }
                    case "moved":
                    {
                        var expected = IntOf(fact.Value, "moved");
                        var start = PositionOf(before, reference);
                        var actual = walked.TryGetValue(reference, out var steps) ? steps
                            : start.HasValue && now.HasValue ? Dist(start.Value, now.Value) : 0;
                        if (actual != expected) Mismatch(expectation, $"it moved {actual} (from {start} to {now})");
                        break;
                    }
                    case "coins":
                    case "level":
                    {
                        var expected = IntOf(fact.Value, fact.Key);
                        var progression = after.HeroProgression.FirstOrDefault(value => value.HeroId.Equals(piece.Hero));
                        if (progression == null) throw Refuse($"expectation {Describe(expectation)}: {reference} has no Level or coins");
                        var actual = fact.Key == "coins" ? progression.Coins : progression.Level;
                        if (actual != expected) Mismatch(expectation, $"it has {actual}");
                        break;
                    }
                    case "hand":
                    {
                        var expected = IntOf(fact.Value, "hand");
                        int Count(MatchSessionProjection projection) =>
                            projection.Cards.Count(card => card.OwnerId.Equals(piece.Hero) && card.State.Location == CardLocation.Hand);
                        var actual = Count(after) - Count(before);
                        if (actual != expected) Mismatch(expectation, $"the hand changed by {actual}");
                        break;
                    }
                    case "attack":
                    {
                        var expected = IntOf(fact.Value, "attack");
                        if (!observedAttacks.TryGetValue(piece.Hero, out var total)) Mismatch(expectation, "no Attack total was observed for " + reference);
                        else if (total != expected) Mismatch(expectation, $"the attack resolved with {total}");
                        break;
                    }
                    case "attack-count":
                    {
                        attackCounts.TryGetValue(piece.Hero, out var actual);
                        if (actual != IntOf(fact.Value, "attack-count")) Mismatch(expectation, $"completed {actual} attacks");
                        break;
                    }
                    case "movement-offered-max":
                    {
                        if (!maximumMovementOffered.TryGetValue(piece.Hero, out var actual)) Mismatch(expectation, "no Movement question was observed");
                        else if (actual != IntOf(fact.Value, "movement-offered-max")) Mismatch(expectation, $"maximum offered walk cost was {actual}");
                        break;
                    }
                    case "targetable":
                    {
                        if (!(fact.Value is bool expected)) throw Unsupported("expectation", Describe(expectation));
                        if (targetQuestions.Count == 0) { Mismatch(expectation, "no targeting question was asked"); break; }
                        var actual = targetQuestions.Any(question => question.Contains(reference));
                        if (actual != expected) Mismatch(expectation, actual ? "it was offered as a target" : "it was never offered as a target");
                        break;
                    }
                    case "chose":
                    {
                        var outcome = Normalize(fact.Value as string ?? string.Empty);
                        var wasDefeated = Defeated(after, piece);
                        int Discards(MatchSessionProjection projection) =>
                            projection.Cards.Count(card => card.OwnerId.Equals(piece.Hero) && card.State.Location == CardLocation.DiscardPile);
                        var discarded = Discards(after) > Discards(before);
                        if (outcome == "defeated" && !wasDefeated) Mismatch(expectation, discarded ? "it discarded" : "neither happened");
                        else if (outcome == "discard" && (!discarded || wasDefeated)) Mismatch(expectation, wasDefeated ? "it was defeated" : "it discarded nothing");
                        else if (outcome != "defeated" && outcome != "discard") throw Unsupported("expectation", Describe(expectation));
                        break;
                    }
                    default:
                        throw Unsupported("expectation", Describe(expectation));
                }
            }

            private readonly List<JsonObject> newTokenFacts = new List<JsonObject>();
            private bool newTokensChecked;

            /// <summary>
            /// v1.1 item 15: a token reference the setup never placed names a token the play placed.
            /// Every such reference's <c>at</c> facts are matched together against the tokens that are
            /// new since the play began, one token each; the case passes when some assignment satisfies
            /// every fact.
            /// </summary>
            private bool NewTokenReference(JsonObject expectation)
            {
                var reference = expectation.Get("ref") as string;
                var shape = RefShape.Match(reference ?? string.Empty);
                if (!shape.Success || byRef.ContainsKey(reference) ||
                    (shape.Groups[1].Value != "token" && shape.Groups[1].Value != "obstacle") ||
                    expectation.Count != 2 || !expectation.Has("at"))
                {
                    return false;
                }

                newTokenFacts.Add(expectation);
                if (newTokensChecked) return true;
                newTokensChecked = true;
                var all = ReadExpectations().Where(value => value.Get("ref") is string named && !byRef.ContainsKey(named) &&
                    RefShape.Match(named).Success && (named.StartsWith("token-", StringComparison.Ordinal) ||
                        named.StartsWith("obstacle-", StringComparison.Ordinal)) && value.Count == 2 && value.Has("at")).ToList();
                var refs = all.Select(value => (string)value.Get("ref")).Distinct().ToList();
                var fresh = after.PlacedTokens.Where(token => !before.PlacedTokens.Any(old => old.Id.Equals(token.Id))).ToList();
                if (fresh.Count < refs.Count)
                {
                    Failures.Add($"{name}: the table names {refs.Count} placed tokens ({string.Join(", ", refs)}), " +
                        $"the play placed {fresh.Count}" + (fresh.Count > 0 ? " at " + string.Join(", ", fresh.Select(token => token.Coordinate.ToString())) : string.Empty));
                    return true;
                }

                var assignment = new Dictionary<string, HexCoordinate>(StringComparer.Ordinal);
                bool Assign(int index, HashSet<int> used)
                {
                    if (index == refs.Count) return true;
                    for (var candidate = 0; candidate < fresh.Count; candidate++)
                    {
                        if (used.Contains(candidate)) continue;
                        assignment[refs[index]] = fresh[candidate].Coordinate;
                        var fits = all.Where(value => (string)value.Get("ref") == refs[index]).All(value =>
                        {
                            var placement = ParsePlacement(value.Get("at") as string) ?? throw Unsupported("placement", Describe(value.Get("at")));
                            var positions = Positions(after, byRef.Keys);
                            foreach (var pair in assignment) positions[pair.Key] = pair.Value;
                            return placement.Refs().All(positions.ContainsKey) && Satisfies(placement, fresh[candidate].Coordinate, positions);
                        });
                        if (!fits) continue;
                        used.Add(candidate);
                        if (Assign(index + 1, used)) return true;
                        used.Remove(candidate);
                    }

                    assignment.Remove(refs[index]);
                    return false;
                }

                if (!Assign(0, new HashSet<int>()))
                {
                    Failures.Add($"{name}: no assignment of the newly placed tokens (at " +
                        string.Join(", ", fresh.Select(token => $"{token.Definition.DisplayName} {token.Coordinate}")) +
                        ") satisfies " + string.Join("; ", all.Select(Describe)));
                }

                return true;
            }

            private bool KindMatches(PlacedToken token, string kind) =>
                TokenKey(token.Definition.DisplayName) == TokenKey(kind) || TokenKey(token.Definition.Id.Value) == TokenKey(kind);

            private void EvaluateTokens(JsonObject expectation)
            {
                var kind = expectation.Get("tokens") as string ?? throw Unsupported("expectation", Describe(expectation));
                if (!AllTokens().Any(token => TokenKey(token.DisplayName) == TokenKey(kind) || TokenKey(token.Id.Value) == TokenKey(kind)))
                    throw Unsupported("token kind", kind);
                if (expectation.Count != 2) throw Unsupported("expectation", Describe(expectation));
                var fact = expectation.First(pair => pair.Key != "tokens");
                var expected = IntOf(fact.Value, fact.Key);
                switch (fact.Key)
                {
                    case "on-board":
                    {
                        var actual = after.PlacedTokens.Count(token => KindMatches(token, kind));
                        if (actual != expected) Mismatch(expectation, $"{actual} on the board");
                        break;
                    }
                    case "on-board-after-round-end":
                    {
                        var actual = afterRound.PlacedTokens.Count(token => KindMatches(token, kind));
                        if (actual != expected) Mismatch(expectation, $"{actual} on the board after the round end");
                        break;
                    }
                    case "supply":
                    {
                        var definition = TokenKind(kind);
                        var placed = after.PlacedTokens.Count(token => token.OwnerHeroId.Equals(actor) &&
                            (token.Definition.Id.Equals(definition.Id) ||
                                definition.SharesASupply && token.Definition.SupplyGroupId == definition.SupplyGroupId));
                        var actual = definition.SupplyCount - placed;
                        if (actual != expected) Mismatch(expectation, $"{actual} left in the actor's supply of {definition.SupplyCount}");
                        break;
                    }
                    default:
                        throw Unsupported("expectation", Describe(expectation));
                }
            }

            private void EvaluateCard(JsonObject expectation)
            {
                var cardName = expectation.Get("card") as string;
                var state = Normalize(expectation.Get("state") as string ?? string.Empty);
                if (cardName == null || expectation.Count != (expectation.Has("ref") ? 3 : 2) || !expectation.Has("state")) throw Unsupported("expectation", Describe(expectation));
                var key = CardKey(cardName);
                var matching = after.Cards.Where(card => CardKey(DefinitionIn(after, card).DisplayName) == key).ToList();
                if (expectation.Has("ref"))
                {
                    var owner = HeroOfRef(expectation.Get("ref") as string);
                    if (!owner.IsDefined) throw Unsupported("card owner", Describe(expectation));
                    matching = matching.Where(card => card.OwnerId.Equals(owner)).ToList();
                }
                var mine = matching.Where(card => card.OwnerId.Equals(actor)).ToList();
                var chosen = mine.Count == 1 ? mine[0] : matching.Count == 1 ? matching[0] : null;
                if (chosen == null) throw Refuse($"unknown card name \"{cardName}\" in expectation {Describe(expectation)}");
                var then = before.Cards.Single(card => card.Id.Equals(chosen.Id)).State;
                var now = chosen.State;
                bool holds;
                switch (state)
                {
                    case "hand": holds = now.Location == CardLocation.Hand; break;
                    case "played": holds = now.Location == CardLocation.TurnArea; break;
                    case "resolved": holds = now.Location == CardLocation.TurnArea && now.ResolutionStatus == CardResolutionStatus.Resolved; break;
                    case "discarded": holds = now.Location == CardLocation.DiscardPile; break;
                    case "item": holds = now.Location == CardLocation.ItemSlot; break;
                    case "retrieved": holds = now.Location == CardLocation.Hand && then.Location != CardLocation.Hand; break;
                    default: throw Unsupported("card state", state);
                }

                if (!holds) Mismatch(expectation, $"it is {now.Location}/{now.ResolutionStatus} (was {then.Location}/{then.ResolutionStatus})");
            }

            private void EvaluateLives(JsonObject expectation)
            {
                if (expectation.Count != 1) throw Unsupported("expectation", Describe(expectation));
                var fact = expectation[0];
                var side = fact.Key == "team-lives" ? actorSide : Opposite(actorSide);
                var team = setup.Teams.Single(value => value.TeamSide == side).TeamId;
                var actual = after.TeamLifeCounters.Single(pair => pair.Key.Equals(team)).Value;
                if (actual != IntOf(fact.Value, fact.Key)) Mismatch(expectation, $"{actual} left");
            }

            private void EvaluateLanePush(JsonObject expectation)
            {
                if (expectation.Count != 1 || !(expectation.Get("lane-pushed") is bool expected))
                {
                    throw Unsupported("expectation", Describe(expectation));
                }

                var actual = LogSince(afterRound ?? after).Any(value => value.Kind == MatchLogEntryKind.LanePush);
                if (actual != expected) Mismatch(expectation, actual ? "the lane was pushed" : "no lane push happened by the round's end");
            }

            /// <summary>
            /// v1.1 item 21. <c>{"bullet": text, "offered": false}</c>: at every Choose question the
            /// card asked during the play, that bullet's arm was (or was not) among the available options.
            /// </summary>
            private void EvaluateBullet(JsonObject expectation)
            {
                if (expectation.Count != 2 || !(expectation.Get("bullet") is string text) ||
                    !(expectation.Get("offered") is bool expected))
                {
                    throw Unsupported("expectation", Describe(expectation));
                }

                if (optionQuestions.Count == 0) { Mismatch(expectation, "no Choose question was asked"); return; }
                var offeredAnywhere = false;
                foreach (var (card, node, available, all) in optionQuestions)
                {
                    var arm = ArmFor(card, node, text, all);
                    if (arm.HasValue && available.Contains(arm.Value)) offeredAnywhere = true;
                }

                if (offeredAnywhere != expected)
                {
                    Mismatch(expectation, offeredAnywhere ? "the bullet was offered" : "the bullet was never offered");
                }
            }

            private void EvaluateMinionBattle(JsonObject expectation)
            {
                if (expectation.Count != 1) throw Unsupported("expectation", Describe(expectation));
                var wanted = Normalize(expectation.Get("minion-battle") as string ?? string.Empty);
                var actorColour = actorSide == TeamSide.Red ? "red" : "blue";
                var enemyColour = actorSide == TeamSide.Red ? "blue" : "red";
                if (wanted == "friendly") wanted = actorColour;
                if (wanted == "enemy") wanted = enemyColour;
                if (wanted != "red" && wanted != "blue" && wanted != "tie") throw Unsupported("expectation", Describe(expectation));
                if (minionBattleWinner != wanted) Mismatch(expectation, $"the Minion Battle went {minionBattleWinner} (the actor's team is {actorColour})");
            }
        }

        private static class Movement
        {
            internal sealed class Destination
            {
                internal Destination(HexCoordinate at, int cost)
                {
                    At = at;
                    Cost = cost;
                }

                internal HexCoordinate At { get; }
                internal int Cost { get; }
            }
        }
    }
}

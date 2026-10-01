using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.6. Every printed primary a hero is offered does something.
    ///
    /// <para><b>Why the check is an outcome and not a reading of the card.</b> The printed-wording audit
    /// (9.1-9.5) holds each composition to its words, and it would not have caught most of what the
    /// playtests found: an action offered in a position where it could only spend the card. Rules
    /// 6.5af, 7.8a1 and 8.35z15 say such an action is not offered, and nothing measured whether the
    /// offer set obeyed them across real positions. This does, the way a player would find out:
    /// whole six-hero matches are played from fixed seeds, and at every turn each offered printed
    /// primary is taken on a copy of the match and every sequence of answers is tried. If every one of
    /// them leaves the match exactly as Holding the same card would - the authoritative snapshot,
    /// byte for byte, after the turn is released - the offer was a card spent on nothing.</para>
    ///
    /// <para><b>RULING 53 is the one legitimate empty offer.</b> A play whose printed "If" gate is
    /// false is offered as a legal do-nothing play (user, 2026-09-08, superseding RULING 42), so a face
    /// that opens a sentence with "If" and changes nothing is counted rather than flagged. Every other
    /// empty offer is a card the walk failed to read.</para>
    ///
    /// <para><b>Hold is the comparison because p.12 makes it the "do nothing" of this game.</b> The
    /// card is spent either way, so the two positions differ in exactly what the action did. No
    /// hand-listed fingerprint is involved, which is why this sees a Next-turn effect, a rewritten
    /// Initiative and a rune marker as readily as a moved unit.</para>
    ///
    /// <para>The line-ups rotate through every dealt hero, so a newly dealt hero is played here the
    /// moment it is dealt, and <see cref="EveryHeroInTheLineup_HadAPrimaryOffered"/> fails if the matches stop
    /// reaching one. Reaching is all it asks: random play keeps heroes apart, so a hero whose every
    /// face needs a target or a spawn point nearby (Dodger) can see each one greyed, and whether a
    /// greyed action really could not be taken is milestone 9.7's check, not this one's.</para>
    /// </summary>
    public sealed class OfferOutcomeTests
    {
        internal const int SeedsPerLineup = 2;
        internal const int CommandsPerMatch = 300;
        internal const int AnswerPathsPerOffer = 60;

        /// <summary>
        /// Milestone 9.9 landing: one survey per line-up, each built by the first case that asks for
        /// that line-up and shared by its sibling cases, so no single case pays for every line-up and
        /// every case fits Unity's 180-second per-case budget. What each line-up plays is unchanged:
        /// the same line-ups, seeds, commands and answer budget as the single survey it replaces.
        /// </summary>
        private static readonly Lazy<Lazy<Survey>[]> Surveys = new Lazy<Lazy<Survey>[]>(() =>
            RotatedLineups()
                .Select((picked, index) => new Lazy<Survey>(() => Run(picked)))
                .ToArray());

        /// <summary>The line-ups, by index, as the per-line-up cases' source.</summary>
        public static IEnumerable<int> Lineups() => Enumerable.Range(0, RotatedLineups().Count);

        private static Survey Surveyed(int lineup) => Surveys.Value[lineup].Value;

        /// <summary>
        /// Faces whose answers can do real things that cancel out. The comparison is with where the
        /// match ends, so these would read as empty; each is listed with the sequence that does it.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> CancelsOut =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tali-commune-with-spirits"] =
                    "Tali discards a Spirit of the named colour and performs its primary, which retrieves " +
                    "the Spirit to her hand: a discard and a retrieval, ending where Hold does."
            };

        /// <summary>
        /// Milestone 9.21j. W9 (user, 2026-09-30, superseding W6; built at 9.22): a target the step
        /// would move 0 spaces is still offered, and the card resolves - so with only an adjacent
        /// target these faces end where Hold does BY RULING. Found when dealing Razzle rotated a new
        /// line-up (garrus, bain, cutter, swift, tali, whisper; seed 2, step 146): Bain's only target
        /// stood beside him. The faces are the two W9 names; any other reason for an empty play still
        /// fails.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> ZeroSpaceMoveByRuling =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bain-get-over-here"] = "W9: an already-adjacent unit or token is pulled 0 spaces.",
                ["cutter-grappling-bolt"] = "W9: an obstacle Cutter already stands beside moves her 0 spaces."
            };

        /// <summary>
        /// Milestone 9.30i. GY-Q7 (the user, 2026-10-01, against the recommendation): "Always playable" -
        /// Prepare Spells is offered even with nothing removed from the book, and an empty refill changes
        /// nothing, so it ends where Hold does BY RULING. A refill that returns a spell is never empty,
        /// so the row covers exactly the ruling's case; any other reason for an empty play still fails.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> AlwaysPlayableByRuling =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["gydion-prepare-spells"] = "GY-Q7: 'Always playable' (user, 2026-10-01); an empty refill ends where Hold does."
            };

        [TestCaseSource(nameof(Lineups))]
        public void EveryOfferedPrintedPrimary_DoesSomething(int lineup)
        {
            Assert.That(Surveyed(lineup).Empty.Where(entry => !CancelsOut.ContainsKey(entry.Split(' ')[0]) &&
                    !ZeroSpaceMoveByRuling.ContainsKey(entry.Split(' ')[0]) &&
                    !AlwaysPlayableByRuling.ContainsKey(entry.Split(' ')[0])),
                Is.Empty,
                "These printed primaries were offered in a position where every answer left the match " +
                "exactly as Holding the card would. Rules 6.5af, 7.8a1 and 8.35z15 grey an action that " +
                "can do nothing, so the offer walk is failing to read something the action depends on.");
        }

        /// <summary>
        /// Milestone 9.7, the converse. Every printed primary the offer set greys for being unable to
        /// do anything (6.5af, 7.8a1 - a grey with no standing restriction named) is taken anyway,
        /// through <c>StartActionIgnoringFeasibilityForInternalTest</c>, and every answer is tried.
        /// A greyed action that could have ended anywhere but where Hold does was a legal play the
        /// player was denied - Crimson Trail's shape (8.104).
        /// </summary>
        [TestCaseSource(nameof(Lineups))]
        public void EveryGreyedPrintedPrimary_CouldNotHaveChangedTheGame(int lineup)
        {
            Assert.That(Surveyed(lineup).GreyedButEffective, Is.Empty,
                "These printed primaries were greyed as unable to do anything, yet taken anyway some " +
                "answer left the match somewhere Hold does not. The offer walk is refusing a legal play.");
            Assert.That(Surveyed(lineup).GreyedTried, Is.GreaterThan(50 / RotatedLineups().Count),
                "A survey forcing few greyed offers is not reading the matches it plays.");
        }

        /// <summary>
        /// Milestone 9.7, found by the forced sweep: Arien's Spell Break ("This turn: Enemy heroes in
        /// radius cannot perform Skill actions, except on the gold cards") greyed Xargatha's Stone Gaze,
        /// yet a start command naming it was performed - the restriction was asked of the offer and
        /// never of the start. Seed 2 of the first rotation's line-up reaches that turn at command 110.
        /// </summary>
        [Test]
        public void ASkillASpellBreakForbids_IsRefusedAtItsStart_AsWellAsGreyed()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var picked = new[] { "wasp", "xargatha", "tigerclaw", "arien", "brogan", "sabina" }
                .Select(name => QuickGameSetupConfiguration.DealtHeroes.Single(hero => hero.ContentId.Value == name))
                .ToArray();
            var session = new MatchSession(Compose(map, picked));
            var random = new MatchLoopRandom(2);
            for (var step = 0; step < 110; step++)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                session.Execute(legal[random.Next(legal.Count)]);
            }

            var projection = session.Projection;
            var stoneGaze = projection.AvailableTurnActions.SingleOrDefault(offer =>
                offer.SourceCardInstanceId.Value.EndsWith("xargatha-stone-gaze", StringComparison.Ordinal) &&
                offer.ActionDefinitionId.Value == "primary-skill");
            Assert.That(stoneGaze.ActionDefinitionId.IsDefined, Is.True,
                "The seeded walk must reach Xargatha's Stone Gaze turn; if a later change reroutes it, " +
                "find the Spell Break position again rather than weakening this case.");
            Assert.That(projection.ActiveEffectSourceCardInstanceIds.Any(id =>
                id.Value.EndsWith("arien-spell-break", StringComparison.Ordinal)), Is.True,
                "and Spell Break must be standing over her.");
            Assert.That(stoneGaze.IsAvailable, Is.False, "The offer greys the barred Skill.");

            var started = session.Execute(
                DirectorCommand.StartAction(stoneGaze.SourceCardInstanceId, stoneGaze.ActionDefinitionId));
            Assert.That(started.Accepted, Is.False,
                "and the rules refuse to start it - one answer, asked in both places.");
        }

        /// <summary>
        /// Milestone 9.9. At every step of every walk, the seat that decides the acting hero's action
        /// is projected exactly the rules' offers - each one's availability and named blocker - and is
        /// authorized to start exactly the available ones; every other seat is offered nothing.
        /// </summary>
        [TestCaseSource(nameof(Lineups))]
        public void EverySeat_IsOfferedExactlyWhatTheRulesOffer_AtEveryStep(int lineup)
        {
            Assert.That(Surveyed(lineup).SeatDisagreements, Is.Empty,
                "A seat's projection disagreed with the rules offer at these steps.");
            Assert.That(Surveyed(lineup).SeatReadings, Is.GreaterThan(1000 / RotatedLineups().Count),
                "The walks compared few seat readings; they are not reading the matches they play.");
        }

        [TestCaseSource(nameof(Lineups))]
        public void EveryOfferedCommand_IsAccepted(int lineup)
        {
            Assert.That(Surveyed(lineup).Refused, Is.Empty,
                "The projection offered these commands and the rules refused them. An offer is a " +
                "promise that the command is legal where it is offered.");
        }

        /// <summary>
        /// Restated per line-up at the 9.9 landing, and stronger than the single survey's form: that
        /// asked that every dealt hero be reached in SOME line-up; this asks that every hero be
        /// reached in EVERY line-up it plays in. <see cref="TheRotation_SeatsEveryDealtHero"/> keeps
        /// the global half - every dealt hero plays in some line-up - without walking a match.
        /// </summary>
        [TestCaseSource(nameof(Lineups))]
        public void EveryHeroInTheLineup_HadAPrimaryOffered(int lineup)
        {
            var missing = RotatedLineups()[lineup]
                .Select(hero => hero.ContentId.Value)
                .Except(Surveyed(lineup).HeroesOffered)
                .ToArray();
            Assert.That(missing, Is.Empty,
                "The rotation no longer reaches every dealt hero's printed primaries; lengthen the " +
                "matches or add a seed so each one is played.");
        }

        [Test]
        public void TheRotation_SeatsEveryDealtHero()
        {
            var seated = RotatedLineups().SelectMany(picked => picked).Select(hero => hero.ContentId.Value);
            Assert.That(QuickGameSetupConfiguration.DealtHeroes.Select(hero => hero.ContentId.Value)
                    .Except(seated),
                Is.Empty, "Every dealt hero plays in the rotation's line-ups.");
        }

        [TestCaseSource(nameof(Lineups))]
        public void TheSurvey_ExploresEveryOfferToTheEnd(int lineup)
        {
            var survey = Surveyed(lineup);
            TestContext.WriteLine(
                $"line-up {lineup}: {survey.Tried} offers tried, {survey.GreyedTried} greyed forced, " +
                $"{survey.SeatReadings} seat readings, {survey.LegalDoNothing} legal do-nothing.");
            Assert.That(Surveyed(lineup).Undecided, Is.Empty,
                "An offer's answers outgrew the exploration budget, so it was neither proved to do " +
                "something nor reported; raise the budget rather than let it pass unread.");
            // Landing restatement: the single survey asked for more than 400 offers in all; a line-up
            // is asked for three quarters of an even share of that, because the line-ups are not
            // even (the thinnest tries 36 against a mean of 59) and a floor per line-up cannot be
            // summed without building every line-up in one case.
            Assert.That(Surveyed(lineup).Tried, Is.GreaterThan(400 / RotatedLineups().Count * 3 / 4),
                "A survey trying few offers is not reading the matches it plays.");
        }

        private sealed class Survey
        {
            public readonly List<string> Empty = new List<string>();
            public readonly List<string> Undecided = new List<string>();
            public readonly List<string> Refused = new List<string>();
            public readonly List<string> GreyedButEffective = new List<string>();
            public readonly List<string> SeatDisagreements = new List<string>();
            public int SeatReadings;
            public int GreyedTried;
            public int LegalDoNothing;
            public readonly HashSet<string> HeroesOffered = new HashSet<string>(StringComparer.Ordinal);
            public int Tried;
        }

        private static Survey Run(RegisteredHero[] picked)
        {
            var survey = new Survey();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            for (var seed = 1; seed <= SeedsPerLineup; seed++)
            {
                Play(survey, map, picked, seed);
            }

            return survey;
        }

        /// <summary>
        /// Two rotations through every dealt hero, the second in reverse, so every hero plays in two
        /// different six-hero line-ups. Milestone 9.10's soak draws its line-ups from here too.
        /// </summary>
        internal static IReadOnlyList<RegisteredHero[]> RotatedLineups()
        {
            var drawn = new List<RegisteredHero[]>();
            var dealt = QuickGameSetupConfiguration.DealtHeroes.ToArray();
            foreach (var heroes in new[] { dealt, dealt.Reverse().ToArray() })
            {
                var lineups = (heroes.Length + 5) / 6;
                for (var lineup = 0; lineup < lineups; lineup++)
                {
                    drawn.Add(Enumerable.Range(0, 6)
                        .Select(offset => heroes[(lineup * 6 + offset) % heroes.Length])
                        .ToArray());
                }
            }

            return drawn;
        }

        private static void Play(Survey survey, BoardMapDefinition map, RegisteredHero[] picked, int seed)
        {
            var initial = Compose(map, picked);
            var live = new MatchSession(initial);
            // One host per match: it fronts the live session, so every step's seat projections are
            // read through it without rebuilding the seat binding.
            var host = new MatchHost(live);
            var random = new MatchLoopRandom(seed);
            var names = string.Join(",", picked.Select(hero => hero.ContentId.Value));
            for (var step = 0; step < CommandsPerMatch &&
                 live.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
            {
                var projection = live.Projection;
                if (projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                    projection.DecisionContract == null && projection.PendingTurnRelease == null)
                {
                    TryEveryOffer(survey, initial, live, $"{names} seed {seed} step {step}");
                }

                // Milestone 9.9. At every step, what each seat's own projection offers and authorizes
                // is what the rules offer: the seat surface is built from these two readings.
                CompareEverySeat(survey, live, host, $"{names} seed {seed} step {step}");

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(live.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var chosen = legal[random.Next(legal.Count)];
                var result = live.Execute(chosen);
                if (!result.Accepted)
                {
                    survey.Refused.Add($"{chosen.Kind} ({names} seed {seed} step {step}): {result.Detail}");
                }
            }
        }

        /// <summary>
        /// Milestone 9.9. Every seat's reading of the turn's offers against the rules' own.
        /// </summary>
        private static void CompareEverySeat(
            Survey survey, MatchSession live, MatchHost host, string where)
        {
            var rules = live.Projection;
            var offering = rules.IsTurnActive && rules.SelectedActor.IsDefined &&
                rules.DecisionContract == null && rules.Phase != RoundResolutionPhase.GameEnded;
            var deciding = offering ? SeatProjectionFactory.DecidingActor(rules) : default;
            foreach (var seat in host.Seats.Seats)
            {
                survey.SeatReadings++;
                var projection = host.ProjectionForSeat(seat.Id);
                var owns = offering && seat.Owns(deciding);
                var expected = owns ? rules.AvailableTurnActions : Array.Empty<ActionOffer>();
                if (!projection.AvailableTurnActions.SequenceEqual(expected))
                {
                    survey.SeatDisagreements.Add(
                        $"{seat.HeroId.Value} ({where}): projected {projection.AvailableTurnActions.Count} " +
                        $"offers, the rules offer {expected.Count}");
                    continue;
                }

                var starts = projection.AuthorizedCommands
                    .Where(command => command.Kind == DirectorCommandKind.StartAction)
                    .ToArray();
                var available = expected.Where(offer => offer.IsAvailable)
                    .Select(offer => DirectorCommand.StartAction(
                        offer.SourceCardInstanceId, offer.ActionDefinitionId))
                    .ToArray();
                if (!starts.OrderBy(command => command.ToString(), StringComparer.Ordinal)
                        .SequenceEqual(available.OrderBy(command => command.ToString(), StringComparer.Ordinal)))
                {
                    survey.SeatDisagreements.Add(
                        $"{seat.HeroId.Value} ({where}): authorized {starts.Length} starts, the rules " +
                        $"make {available.Length} available");
                }
            }
        }

        internal static MatchSessionInitialState Compose(BoardMapDefinition map, RegisteredHero[] picked)
        {
            var lineup = new HeroLineup(picked.Take(3), picked.Skip(3));
            var format = MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<Core.Obstacles.ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }

        private static void TryEveryOffer(
            Survey survey, MatchSessionInitialState initial, MatchSession live, string where)
        {
            var projection = live.Projection;
            var snapshot = live.CaptureSnapshot();
            foreach (var offer in projection.AvailableTurnActions)
            {
                var definition = projection.Definitions.Single(
                    candidate => candidate.Reference.Equals(offer.SourceDefinitionReference));
                var action = definition.ActionDefinitions.Single(
                    candidate => candidate.Id.Equals(offer.ActionDefinitionId));
                if (action.Origin != ActionDefinitionOrigin.PrintedPrimary)
                {
                    continue;
                }

                // Reached, whether or not it is available: a greyed offer is 9.7's question.
                survey.HeroesOffered.Add(definition.Id.Value.Split('-')[0]);
                if (!offer.IsAvailable)
                {
                    TryTheGreyedOffer(survey, initial, snapshot, offer, action.Kind, definition.Id.Value, where);
                    continue;
                }

                survey.Tried++;

                var hold = Restored(initial, snapshot);
                Assert.That(hold.Execute(DirectorCommand.PerformHold(offer.SourceCardInstanceId)).Accepted,
                    Is.True, $"p.12: any card can Hold ({definition.Id.Value}, {where}).");
                var held = Settled(hold);

                var taken = Restored(initial, snapshot);
                var actor = taken.Projection.SelectedActor.ActorId;
                var started = taken.Execute(
                    DirectorCommand.StartAction(offer.SourceCardInstanceId, offer.ActionDefinitionId));
                Assert.That(started.Accepted, Is.True,
                    $"An offered action must start ({definition.Id.Value}, {where}): {started.Detail}");

                var budget = AnswerPathsPerOffer;
                if (DoesSomething(survey, initial, taken, actor, held, 0, ref budget, definition.Id.Value))
                {
                    continue;
                }

                if (budget > 0 && OpensWithAnIf(definition))
                {
                    // RULING 53 (user, 2026-09-08): a play whose "If" gate is false is offered as a
                    // legal do-nothing play - "it's a Hold". Counted, not flagged.
                    survey.LegalDoNothing++;
                    continue;
                }

                (budget <= 0 ? survey.Undecided : survey.Empty).Add($"{definition.Id.Value} ({where})");
            }
        }

        private static readonly System.Text.RegularExpressions.Regex IfGate =
            new System.Text.RegularExpressions.Regex(@"(^|[.;:]\s*)If\b");

        /// <summary>
        /// Whether the face's printed text opens a sentence with "If" - the gate RULING 53 speaks to.
        /// "if able" and "if you do" are lower-case clauses inside a sentence and are not it.
        ///
        /// <para>Milestone 9.21j. Razzle's Stunt Doubles (and the boxed Stunted Doubles) print
        /// ". If defeated, remove all of you." - a sentence opening with "If" that is NOT a gate on
        /// the play: it is RZ-Q3's Defeat, composed to no node. The pattern matches it all the same, so
        /// an empty Stunt Doubles play would be counted as a RULING 53 do-nothing rather than flagged.
        /// It cannot arise: the face's primary is a Basic Attack, offered only with a unit adjacent to
        /// the acting body, and an Attack always does something. RazzleDealingTests pins which of his
        /// faces the pattern matches (the two Doubles for that reason; Phantom Strike and Rummage for
        /// their printed gates), so a new "If" sentence on his faces is seen rather than absorbed.</para>
        /// </summary>
        internal static bool OpensWithAnIf(CardDefinition definition) =>
            IfGate.IsMatch(definition.SourceContent?.PrintedText ?? string.Empty);

        /// <summary>
        /// Milestone 9.7. A grey that names a standing restriction is a rule the offer was told, not a
        /// judgement it made, so it is left alone; every other grey is taken anyway.
        /// </summary>
        private static void TryTheGreyedOffer(
            Survey survey, MatchSessionInitialState initial, string snapshot, ActionOffer offer,
            ActionKind kind, string card, string where)
        {
            if (offer.Unavailability.IsStated)
            {
                return;
            }

            survey.GreyedTried++;
            var hold = Restored(initial, snapshot);
            if (!hold.Execute(DirectorCommand.PerformHold(offer.SourceCardInstanceId)).Accepted)
            {
                return;
            }

            var held = Settled(hold);
            var forced = Restored(initial, snapshot);
            var actor = forced.Projection.SelectedActor.ActorId;
            if (!forced.StartActionIgnoringFeasibilityForInternalTest(
                    offer.SourceCardInstanceId, offer.ActionDefinitionId).Accepted)
            {
                // The rules refuse it too: nothing to perform, and the grey was right.
                return;
            }

            var budget = AnswerPathsPerOffer;

            // An Attack is legal only if it can take a target (p.13). Its card text may move the
            // attacker or spend a token before the targeting step stops it (RULING 45's dead end,
            // 8.35z42's per-space proof), so "something changed" is not the question for it -
            // "some answer reaches a target" is.
            var legal = kind == ActionKind.Attack
                ? ReachesATarget(initial, forced, actor, 0, ref budget)
                // A refusal met inside an action the offer never made is not an offered command refused.
                : DoesSomething(new Survey(), initial, forced, actor, held, 0, ref budget, card);
            if (legal)
            {
                survey.GreyedButEffective.Add($"{card} ({where})");
            }
        }

        /// <summary>Whether some sequence of answers brings a forced Attack to a target.</summary>
        internal static bool ReachesATarget(
            MatchSessionInitialState initial, MatchSession session, HeroId actor, int depth, ref int budget)
        {
            var projection = session.Projection;
            if (projection.PendingAttackResolution != null &&
                projection.PendingAttackResolution.Target.Kind != AttackTargetKind.None ||
                projection.PendingAttackTargetChoice != null &&
                projection.PendingAttackTargetChoice.LegalTargets.Count > 0 ||
                projection.PendingDefenseChoice != null)
            {
                return true;
            }

            var stillActing = projection.SelectedActor.IsDefined &&
                projection.SelectedActor.ActorId.Equals(actor) &&
                projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                projection.DecisionContract != null &&
                projection.PendingTurnRelease == null &&
                depth < 12;
            if (!stillActing)
            {
                budget--;
                return false;
            }

            var here = session.CaptureSnapshot();
            foreach (var answer in projection.DecisionContract.Commands.OrderBy(Doing))
            {
                if (budget <= 0)
                {
                    return false;
                }

                var branch = Restored(initial, here);
                if (branch.Execute(answer).Accepted &&
                    ReachesATarget(initial, branch, actor, depth + 1, ref budget))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Milestone 9.9. <see cref="DoesSomething"/> for an action forced past the offer, where a
        /// refusal met inside it is not an offered command refused.
        /// </summary>
        internal static bool ForcedDoesSomething(
            MatchSessionInitialState initial, MatchSession session, HeroId actor, string held,
            ref int budget, string card) =>
            DoesSomething(new Survey(), initial, session, actor, held, 0, ref budget, card);

        /// <summary>Whether some sequence of answers ends anywhere other than where Hold does.</summary>
        private static bool DoesSomething(
            Survey survey, MatchSessionInitialState initial, MatchSession session, HeroId actor, string held,
            int depth, ref int budget, string card)
        {
            var projection = session.Projection;
            var stillActing = projection.SelectedActor.IsDefined &&
                projection.SelectedActor.ActorId.Equals(actor) &&
                projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                projection.DecisionContract != null &&
                projection.PendingTurnRelease == null &&
                depth < 12;
            if (!stillActing)
            {
                budget--;
                return Settled(session) != held;
            }

            var here = session.CaptureSnapshot();
            foreach (var answer in projection.DecisionContract.Commands.OrderBy(Doing))
            {
                if (budget <= 0)
                {
                    return false;
                }

                var branch = Restored(initial, here);
                var answered = branch.Execute(answer);
                if (!answered.Accepted)
                {
                    survey.Refused.Add($"{answer.Kind} inside {card}: {answered.Detail}");
                    continue;
                }

                if (DoesSomething(survey, initial, branch, actor, held, depth + 1, ref budget, card))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Answers that act before answers that decline, so a doing path is found early.</summary>
        internal static int Doing(DirectorCommand command)
        {
            switch (command.Kind)
            {
                case DirectorCommandKind.AnswerEffectChoice:
                    switch (command.EffectChoice.Kind)
                    {
                        case EffectChoiceCommandKind.Select: return 0;
                        case EffectChoiceCommandKind.Confirm: return 1;
                        case EffectChoiceCommandKind.Decline: return 9;
                        default: return 8;
                    }
                case DirectorCommandKind.ChooseMovementDestination:
                case DirectorCommandKind.ChooseAttackTarget:
                case DirectorCommandKind.DeclineDefense:
                    return 0;
                case DirectorCommandKind.ChooseDefenseCard: return 5;
                case DirectorCommandKind.StopMovement: return 7;
                default: return 4;
            }
        }

        internal static MatchSession Restored(MatchSessionInitialState initial, string snapshot)
        {
            var session = new MatchSession(initial);
            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            return session;
        }

        /// <summary>The authoritative state once the turn is released, without the event log.</summary>
        internal static string Settled(MatchSession session)
        {
            if (session.Projection.PendingTurnRelease != null)
            {
                session.Execute(DirectorCommand.ReleaseTurn());
            }

            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(session.CaptureSnapshot(), out var envelope),
                Is.True);
            return envelope.AuthoritativePayload;
        }
    }
}

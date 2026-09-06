using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Which brain answers for a hero.</summary>
    public enum BotTier
    {
        /// <summary>The THIRTEENTH amendment's original: uniformly among legal answers.</summary>
        RandomLegal = 0,

        /// <summary>The tier-0.5 greedy-nudge pass (milestone 8.35z13).</summary>
        Greedy = 1,

        /// <summary>The FOURTEENTH amendment's tier-1 evaluation and policies.</summary>
        TierOne = 2
    }

    /// <summary>
    /// One command the projection offered and the rules then refused - an offer-feasibility finding,
    /// which is the class of defect a driver that never names a command exists to surface.
    /// </summary>
    public sealed class BotSelfPlayRefusal
    {
        public BotSelfPlayRefusal(
            int seed, int commandOrdinal, DirectorCommand command, string detail, string defensePreview)
        {
            Seed = seed;
            CommandOrdinal = commandOrdinal;
            Command = command;
            Detail = detail;
            DefensePreview = defensePreview;
        }

        public int Seed { get; }
        public int CommandOrdinal { get; }
        public DirectorCommand Command { get; }
        public string Detail { get; }

        /// <summary>The standing defense preview at the moment of refusal, when there was one.</summary>
        public string DefensePreview { get; }

        public override string ToString() =>
            "seed " + Seed + " command " + CommandOrdinal + " (" + Command.Kind +
            (Command.CardInstanceId.IsDefined ? " card=" + Command.CardInstanceId.Value : string.Empty) +
            ") refused: " + Detail + " | preview: " + DefensePreview;
    }

    /// <summary>What one seeded self-play match did.</summary>
    public sealed class BotSelfPlayResult
    {
        public BotSelfPlayResult(
            int seed,
            MatchOutcome outcome,
            int commands,
            int roundsReached,
            bool exhaustedItsBudget,
            bool stalled,
            IReadOnlyDictionary<BotTier, long> decisionTicks,
            IReadOnlyDictionary<BotTier, int> decisionCounts,
            int simulatedPlies,
            IReadOnlyList<BotSelfPlayRefusal> refusals)
        {
            Refusals = refusals;
            Seed = seed;
            Outcome = outcome;
            Commands = commands;
            RoundsReached = roundsReached;
            ExhaustedItsBudget = exhaustedItsBudget;
            Stalled = stalled;
            DecisionTicks = decisionTicks;
            DecisionCounts = decisionCounts;
            SimulatedPlies = simulatedPlies;
        }

        public int Seed { get; }
        public MatchOutcome Outcome { get; }
        public int Commands { get; }
        public int RoundsReached { get; }
        public bool ExhaustedItsBudget { get; }
        public bool Stalled { get; }

        /// <summary>Stopwatch ticks spent inside the policy, per tier. Latency evidence.</summary>
        public IReadOnlyDictionary<BotTier, long> DecisionTicks { get; }

        public IReadOnlyDictionary<BotTier, int> DecisionCounts { get; }

        /// <summary>Candidate plies the lookahead actually simulated, when one was supplied.</summary>
        public int SimulatedPlies { get; }

        /// <summary>Every offered-then-refused command this run met. Empty is the expected value.</summary>
        public IReadOnlyList<BotSelfPlayRefusal> Refusals { get; }

        /// <summary>
        /// True when the match ended with a winner. A budget-exhausted or stalled run has no winner
        /// and must not be counted as a win for anybody.
        /// </summary>
        public bool HasWinner => !ExhaustedItsBudget && !Stalled && Outcome.HasEnded &&
            Outcome.WinningTeamId.IsDefined;

        public double MillisecondsPerDecision(BotTier tier) =>
            DecisionCounts.TryGetValue(tier, out var count) && count > 0
                ? DecisionTicks[tier] * 1000.0 / Stopwatch.Frequency / count
                : 0.0;
    }

    /// <summary>
    /// FOURTEENTH amendment, tier-1 phase A. Plays a whole configured match with a different brain on
    /// each side, so two policies can be compared over the same seeds on the same board.
    /// </summary>
    /// <remarks>
    /// <para>This is <see cref="DeterministicMatchLoop"/> with one substitution: where that driver
    /// draws uniformly from the pending decision's own candidates, this asks whichever brain owns the
    /// hero the authority is waiting on. Everything else is deliberately identical - the same
    /// <see cref="DeterministicMatchLoop.EnumerateLegalCommands"/>, so a policy can only ever choose
    /// something the rules were already offering, and the same refusal check, so an offered-then-
    /// refused command still fails loudly rather than being steered around.</para>
    /// <para><b>The fidelity caveat, stated rather than buried.</b> The brains here read the
    /// DIRECTOR's projection, because that is what a headless <see cref="MatchSession"/> has; a
    /// shipped bot reads its own seat's. The two agree only if every term the evaluation reads is
    /// public - which is not assumed here, it is asserted by
    /// <c>BotBrainTests.TheEvaluationReadsOnlyWhatASeatCanSee</c>. If that case ever fails, this
    /// measurement stops describing the shipped bot and the failing term is the reason.</para>
    /// </remarks>
    public static class BotSelfPlay
    {
        /// <summary>
        /// THE MEASUREMENT BENCH: the official six seats at their own spawns, with the Life counters
        /// dialled down so a match reaches p.13's ending inside a bounded run.
        /// </summary>
        /// <remarks>
        /// <para><b>Symmetric on purpose, and the first measurement is why.</b> The obvious bench,
        /// <c>ConfiguredMatchFixtures.CreateEngagedOfficialSixPlayerSession</c>, stages ONE blue hero
        /// beside ONE red hero - deliberately, because it exists to make a Life-counter ending
        /// reachable. That staging is asymmetric, and the asymmetry is enormous: with the SAME brain
        /// on both sides it hands Blue 72-85% of the decided matches. A brain difference measured on
        /// it is measured underneath a side advantage several times its size, and a side-swapped
        /// comparison on it returns 50% whatever the brains do - which is exactly what the first run
        /// of this measurement produced, and what sent it here.</para>
        /// <para>This bench changes one thing: the heroes stay on their own spawns. Both sides get
        /// the same board, and what is left to differ is the play.</para>
        /// </remarks>
        public static MatchSession CreateSymmetricBench(int lifeCountersPerTeam)
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var official = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var setup = new QuickGameSetupState(
                official.SelectedMap,
                official.WaveCount,
                official.TieBreakerState,
                official.Teams.Select(team => new QuickGameTeamSetupState(
                    team.TeamId, team.TeamSide, lifeCountersPerTeam, team.ThroneZoneId)).ToArray(),
                official.Heroes,
                official.Minions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<Core.Obstacles.ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        public static BotSelfPlayResult Play(
            MatchSession session,
            int seed,
            int maximumCommands,
            Func<HeroId, BotTier> tierForHero,
            bool useLookahead = false,
            BotPolicyFamilies families = BotPolicyFamilies.All)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (tierForHero == null) throw new ArgumentNullException(nameof(tierForHero));
            if (maximumCommands <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCommands), maximumCommands, "A run needs a positive command budget.");
            }

            var random = new DeterministicBotRandom(seed);
            var lookahead = useLookahead ? new SessionBotLookahead(session) : null;
            var ticks = new Dictionary<BotTier, long>();
            var counts = new Dictionary<BotTier, int>();
            foreach (BotTier tier in Enum.GetValues(typeof(BotTier)))
            {
                ticks[tier] = 0;
                counts[tier] = 0;
            }

            var executed = 0;
            var stalled = false;
            var stopwatch = new Stopwatch();
            var refusals = new List<BotSelfPlayRefusal>();

            while (session.Projection.Phase != RoundResolutionPhase.GameEnded)
            {
                if (executed >= maximumCommands)
                {
                    return Result(seed, session, executed, true, false, ticks, counts, lookahead, refusals);
                }

                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    stalled = true;
                    break;
                }

                var decidingHero = DecidingHero(session.Projection);
                var tier = decidingHero.IsDefined ? tierForHero(decidingHero) : BotTier.RandomLegal;

                stopwatch.Restart();
                var chosen = Choose(tier, candidates, session.Projection, decidingHero, random, lookahead, families);
                stopwatch.Stop();
                ticks[tier] += stopwatch.ElapsedTicks;
                counts[tier]++;

                var result = session.Execute(chosen);
                if (!result.Accepted)
                {
                    // AN OFFERED COMMAND THE RULES REFUSED. Recorded rather than thrown, and the
                    // reason is not convenience: the SHIPPED driver already behaves exactly this way
                    // - PrivateSeatDriver submits, SubmitProjectedCommand returns false, and the bot
                    // simply tries again on its next think-tick. A self-play run that aborted here
                    // would be measuring a bot that does not exist. So the refusal is captured with
                    // everything needed to reproduce it (see Refusals), the candidate is dropped, and
                    // the same decision is answered again from what remains - which is what the live
                    // bot's next tick amounts to.
                    refusals.Add(new BotSelfPlayRefusal(
                        seed, executed + 1, chosen, result.Detail, DescribeDefensePreview(session.Projection)));
                    var remaining = candidates.Where(value => !ReferenceEquals(value, chosen)).ToList();
                    if (remaining.Count == 0)
                    {
                        stalled = true;
                        break;
                    }

                    candidates = remaining;
                    chosen = Choose(tier, candidates, session.Projection, decidingHero, random, lookahead, families);
                    if (!session.Execute(chosen).Accepted)
                    {
                        stalled = true;
                        break;
                    }
                }

                executed++;
            }

            return Result(seed, session, executed, false, stalled, ticks, counts, lookahead, refusals);
        }

        /// <summary>
        /// The standing defense preview, rendered for a refusal record. This is the evidence that
        /// says whether an offered-then-refused Defense answer was one the preview itself listed.
        /// </summary>
        private static string DescribeDefensePreview(MatchSessionProjection projection)
        {
            var preview = projection?.PendingDefensePreview;
            if (preview?.Choices == null)
            {
                return "(no standing defense preview)";
            }

            return string.Join("; ", preview.Choices.Select(choice =>
                (choice.IsDecline ? "decline" : "card " + choice.CardInstanceId?.Value) +
                " -> " + choice.Outcome +
                (choice.DefenseTotal.HasValue ? " total=" + choice.DefenseTotal.Value : string.Empty)));
        }

        private static DirectorCommand Choose(
            BotTier tier,
            IReadOnlyList<DirectorCommand> candidates,
            IMatchReadModel readModel,
            HeroId decidingHero,
            IBotRandom random,
            IBotLookahead lookahead,
            BotPolicyFamilies families)
        {
            switch (tier)
            {
                case BotTier.TierOne:
                    return BotPolicy.SelectTierOne(candidates, readModel, decidingHero, random, lookahead, families) ??
                        candidates[random.Next(candidates.Count)];
                case BotTier.Greedy:
                    return BotPolicy.SelectGreedy(candidates, readModel, decidingHero, random) ??
                        candidates[random.Next(candidates.Count)];
                default:
                    return candidates[random.Next(candidates.Count)];
            }
        }

        /// <summary>
        /// Whose decision this is, read from what the projection publishes rather than inferred.
        /// </summary>
        /// <remarks>
        /// The order mirrors <see cref="DeterministicMatchLoop.EnumerateLegalCommands"/>'s own
        /// branches, because the question "who is being asked" has to be answered about the same
        /// decision the candidates came from. A team-owned decision (an agreement, the end of a
        /// round) reports no hero, and its answer is left to the die on purpose: convergence, not
        /// cleverness, is what a team decision needs, and the shipped driver settles those with the
        /// deterministic first-candidate rule for exactly that reason.
        /// </remarks>
        public static HeroId DecidingHero(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                return default;
            }

            if (projection.DecisionContract != null)
            {
                return projection.DecisionOwner.HeroId;
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var pending = projection.Commitments
                    .FirstOrDefault(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                return pending.HeroId;
            }

            return projection.SelectedActor.IsDefined ? projection.SelectedActor.ActorId : default;
        }

        /// <summary>
        /// Assigns tiers by team side: one whole team plays each brain, which is what makes a win rate
        /// a statement about the brains rather than about which hero drew which card.
        /// </summary>
        public static Func<HeroId, BotTier> ByTeam(
            MatchParticipants participants, TeamId tierOneTeam, BotTier opponent)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            return heroId =>
            {
                try
                {
                    return participants.GetTeamForHero(heroId).Id.Equals(tierOneTeam)
                        ? BotTier.TierOne
                        : opponent;
                }
                catch (Exception)
                {
                    return opponent;
                }
            };
        }

        private static BotSelfPlayResult Result(
            int seed,
            MatchSession session,
            int executed,
            bool exhausted,
            bool stalled,
            Dictionary<BotTier, long> ticks,
            Dictionary<BotTier, int> counts,
            SessionBotLookahead lookahead,
            List<BotSelfPlayRefusal> refusals) =>
            new BotSelfPlayResult(
                seed,
                session.Projection.Outcome,
                executed,
                session.Projection.RoundIndex,
                exhausted,
                stalled,
                ticks,
                counts,
                lookahead?.SimulatedPlies ?? 0,
                refusals);
    }
}

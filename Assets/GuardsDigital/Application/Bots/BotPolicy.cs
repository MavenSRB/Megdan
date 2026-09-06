using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// Which decision families tier-1 forms an opinion about. Every family can be switched off
    /// independently so that self-play can ABLATE it - run the same seeds with one family back on the
    /// die and see what the win rate does. A family that costs win rate when enabled is a heuristic
    /// that is wrong, and a measurement is the only thing that can tell you which.
    /// </summary>
    [Flags]
    public enum BotPolicyFamilies
    {
        None = 0,
        Defense = 1 << 0,
        AttackTarget = 1 << 1,
        Geometry = 1 << 2,
        ActionStart = 1 << 3,
        Commitment = 1 << 4,
        TurnEconomy = 1 << 5,
        Respawn = 1 << 6,
        All = Defense | AttackTarget | Geometry | ActionStart | Commitment | TurnEconomy | Respawn
    }

    /// <summary>
    /// FOURTEENTH amendment, tier-1 phase A. Chooses one command from what a seat is offered, by
    /// scoring every candidate and letting the seeded die decide only among genuine ties.
    /// </summary>
    /// <remarks>
    /// <para><b>The shape, and why it is not the simulation the brief imagined.</b> A candidate is
    /// scored three ways, in descending order of honesty:</para>
    /// <list type="number">
    /// <item><b>A real ply</b>, when an <see cref="IBotLookahead"/> is supplied - the engine applies
    /// the candidate and <see cref="BotEvaluation"/> scores the position that results. Only a
    /// full-information composition can supply one; see that interface's remarks for why a seat
    /// cannot.</item>
    /// <item><b>The authority's own published preview</b>, where one exists. A defense choice already
    /// carries the outcome each answer would produce, and an attack target already carries the
    /// Defense its neighbours would lend it. These are one-ply results computed by the rules
    /// themselves - better than a simulation, because they cannot disagree with it.</item>
    /// <item><b>An applied delta</b> over the terms the command itself determines exactly. A
    /// movement destination names the space this hero would stand on, so the geometry terms can be
    /// recomputed there rather than guessed. This is exact for those terms and silent about the
    /// rest.</item>
    /// </list>
    /// <para>Where none of the three applies, the candidate scores the position as it stands, every
    /// candidate ties, and the die picks - which is precisely the random-legal behaviour the
    /// tier-0.5 pass fell back to. <b>A family this policy has no opinion about must stay random
    /// rather than acquire a made-up preference.</b></para>
    /// <para><b>What is deliberately left to the die, and why.</b> Effect choices - which includes
    /// the level-up item pick the FOURTEENTH amendment lists - are answered at random. The amendment
    /// asked for a leveling policy and this does not have one, on purpose: the generic shape of an
    /// effect-choice candidate carries no content-generic signal about which option is better, so any
    /// preference here would be an invention rather than a reading, and an invention keyed on option
    /// identity is precisely what <see cref="BotEvaluation"/>'s maintenance covenant forbids. Team
    /// agreements are also untouched, and for a stronger reason: the driver settles those with the
    /// deterministic first-candidate rule because every bot runs its own die, and a "preference" at
    /// a team agreement is a disagreement nobody is scripted to resolve. Convergence beats cleverness
    /// there, and the first Solo cohort run hung forever proving it.</para>
    /// <para><b>Content-generic only.</b> Every judgment below reads a card's STRUCTURE - does it
    /// offer an Attack, what is its printed Initiative, what Range does it print - and never its
    /// identity. See <see cref="BotEvaluation"/>'s maintenance covenant, which governs this file too.</para>
    /// </remarks>
    public static class BotPolicy
    {
        /// <summary>
        /// The policy weights moved to <see cref="BotPolicyWeights"/> at tier-2, where a tuner can
        /// construct a vector instead of rebuilding - and where the finding that they, not the
        /// evaluation's nine, are what actually rank candidates is recorded.
        /// </summary>
        public static BotPolicyWeights DefaultPolicyWeights => BotPolicyWeights.Default;

        /// <summary>
        /// The tier-0.5 greedy-nudge selection, moved here unchanged from
        /// <c>PrivateSeatDriver.SelectGreedyCommand</c> so that both tiers live behind one door and
        /// self-play can pit them against each other. Its own fixture still drives it through the
        /// driver, which now delegates here - so those cases prove this move was faithful.
        /// </summary>
        public static DirectorCommand SelectGreedy(
            IReadOnlyList<DirectorCommand> commands,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            IBotRandom random)
        {
            if (commands == null || commands.Count == 0 || readModel == null || random == null)
            {
                return null;
            }

            return PreferSufficientDefense(commands, readModel, random) ??
                PreferKillableAttackTarget(commands, readModel, random) ??
                PreferAdvancingMovement(commands, readModel, ownHeroId, random) ??
                PreferAttackStart(commands, readModel, random);
        }

        /// <summary>
        /// The tier-1 selection: score every candidate, keep the best, break ties with the seeded die.
        /// Never widens <paramref name="commands"/>, so the answer is always as legal as a random one.
        /// </summary>
        public static DirectorCommand SelectTierOne(
            IReadOnlyList<DirectorCommand> commands,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            IBotRandom random,
            IBotLookahead lookahead = null,
            BotPolicyFamilies families = BotPolicyFamilies.All) =>
            Select(
                commands, readModel, ownHeroId, random, lookahead, families,
                BotEvaluationWeights.PhaseA, BotPolicyWeights.PhaseA, ExactTiesOnly);

        /// <summary>
        /// FOURTEENTH amendment, tier-2. Tier-1's judgment plus a REAL ply where one is available,
        /// under the tuned weights, with the seeded near-equal pick Ruling 41 asked for.
        /// </summary>
        /// <remarks>
        /// <para><b>The ply ADDS to the static judgment; it does not replace it, and phase A's
        /// version replacing it was the bug.</b> One ply on this engine is one COMMAND, not one turn.
        /// Starting an Attack does not resolve it, choosing a target does not land the hit, and
        /// committing a card does not play it - so for most candidate families the position that
        /// immediately results is worth almost exactly what the position before it was worth, and a
        /// score taken from the ply alone would call them all equal. Tier-1's static deltas are
        /// PREDICTIONS about a consequence one command has not reached yet; the ply is a MEASUREMENT
        /// of the consequence a command actually did reach. Both are worth having, and adding them
        /// keeps every ablation-proven family that tier-1 measured at 8-45 points each.</para>
        /// <para><b>Geometry is the one family the ply supersedes, and it is suppressed rather than
        /// added to.</b> Milestone 8.35p made a move happen the instant the destination is chosen, so
        /// the ply after a movement command measures the same proximity and adjacency terms
        /// <see cref="GeometryDelta"/> predicts - exactly, on the same board, with the same weights.
        /// Counting both would double the one family that is already only a tiebreak-scale nudge.
        /// <c>BotTierTwoTests.TheMeasuredPlyReproducesTheGeometryPrediction</c> is the proof that they
        /// are the same number rather than the claim that they are.</para>
        /// <para><b>Where no ply is available this is tier-1 under tuned weights</b>, which is the
        /// honest degradation: a poisoned or absent lookahead costs cleverness, never correctness.</para>
        /// </remarks>
        public static DirectorCommand SelectTierTwo(
            IReadOnlyList<DirectorCommand> commands,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            IBotRandom random,
            IBotLookahead lookahead = null,
            BotPolicyFamilies families = BotPolicyFamilies.All,
            BotEvaluationWeights weights = null,
            int nearEqualBand = NearEqualBand,
            int lookaheadCandidateCap = LookaheadCandidateCap,
            BotPolicyWeights policy = null) =>
            Select(
                commands, readModel, ownHeroId, random, lookahead, families,
                weights ?? BotEvaluationWeights.Default, policy ?? BotPolicyWeights.Default,
                nearEqualBand, lookaheadCandidateCap);

        /// <summary>Tier-1's tiebreak rule: only an EXACT tie goes to the die.</summary>
        public const int ExactTiesOnly = 0;

        /// <summary>
        /// RULING 41. How far below the best score a candidate may sit and still be drawn from.
        /// </summary>
        /// <remarks>
        /// <para><b>SIZED BY MEASUREMENT, AND THE MEASUREMENT SAYS THIS BAND IS A COST, NOT A FREE
        /// LUNCH.</b> <c>BotTierTwoTuning.TheNearEqualBandSweep</c> plays the band against the
        /// shipped tier-1 brain over the twenty tuning seeds, both sides - forty matches each:</para>
        /// <code>
        /// band    0     50.0%   (19W 19L 2U)   <- the control: at band 0 this selection IS tier-1
        /// band    8     43.6%   (17W 22L 1U)
        /// band   16     35.0%   (14W 26L 0U)
        /// band   24     37.5%   (15W 25L 0U)
        /// band   32     25.6%   (10W 29L 1U)
        /// band   64     37.5%   (15W 25L 0U)
        /// band  128      7.7%   ( 3W 36L 1U)
        /// </code>
        /// <para>Forty matches carry roughly +/-8 points of noise, so 8 is the largest band whose
        /// result is not measurably below the control, and everything from 16 up is a real and
        /// growing handicap. <b>The first draft of this constant was 24, chosen by taste, and it cost
        /// 12.5 points</b> - which is why the number now comes from the table above.</para>
        /// <para><b>What it means in the evaluation's own units.</b> It sits below every weight that
        /// names a MATERIAL change of position - <see cref="BotEvaluationWeights.TeamLife"/> at 1000,
        /// <see cref="BotEvaluationWeights.LevelAdvantage"/> at 40,
        /// <see cref="BotEvaluationWeights.BoardPresence"/> at 30 - so it can never swallow a Life
        /// counter, a level, or a hero standing on the board. What it swallows is the tiebreak-scale
        /// terms: a hex of engagement proximity, a coin, a card in hand. What it must NOT be allowed
        /// to swallow is the commitment family's own spread, which runs about 6-10 points per printed
        /// Initiative - that family is worth 33.8 points in tier-1's ablation, and a band that
        /// randomises it is how the 24 above lost 12.5.</para>
        /// <para><b>WHAT THE BAND DOES NOT BUY, recorded because it corrects a prediction rather than
        /// confirming one.</b> Phase A's fourth recommendation expected tier-1 to be so deterministic
        /// that the seed decided nothing - "with the same brain on both sides the symmetric bench
        /// gives Blue 20/20". On the current build that does not reproduce.
        /// <c>BotTierTwoTuning.TheMirrorVarietyDiagnostic</c>, twelve seeds, same brain both sides:
        /// band 0 already gives red 4 / blue 8, and no band from 2 to 16 widens it (1/11, 5/7, 5/7,
        /// 4/8 - noise, not a trend). So the repetitiveness the ruling exists to prevent is not
        /// currently present, and the band is implemented as the ruling directs, sized to be as close
        /// to free as measurement can establish, rather than sold as a fix for a problem this
        /// milestone could not find.</para>
        /// </remarks>
        public const int NearEqualBand = 8;

        private static DirectorCommand Select(
            IReadOnlyList<DirectorCommand> commands,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            IBotRandom random,
            IBotLookahead lookahead,
            BotPolicyFamilies families,
            BotEvaluationWeights weights,
            BotPolicyWeights policy,
            int nearEqualBand,
            int lookaheadCandidateCap = LookaheadCandidateCap)
        {
            if (commands == null || commands.Count == 0 || random == null)
            {
                return null;
            }

            if (readModel == null || !ownHeroId.IsDefined)
            {
                return commands[random.Next(commands.Count)];
            }

            var standing = BotEvaluation.Evaluate(readModel, ownHeroId, weights);
            var scores = new int[commands.Count];
            for (var index = 0; index < commands.Count; index++)
            {
                scores[index] = standing +
                    StaticDelta(commands[index], readModel, ownHeroId, families, weights, policy);
            }

            // The ply must be measured in the SAME units the standing position is, or the gain below
            // is a difference of vectors rather than of positions. A lookahead built with a different
            // vector is refused the ply rather than quietly mis-scored.
            var comparable = lookahead != null && SameVector(lookahead.Weights, weights);
            if (comparable)
            {
                lookahead.BeginDecision();
                foreach (var index in Shortlist(scores, lookaheadCandidateCap))
                {
                    if (!lookahead.TryEvaluateAfter(commands[index], ownHeroId, out var simulated))
                    {
                        continue;
                    }

                    // Geometry is re-derived WITHOUT its prediction, because the ply just measured
                    // the same terms exactly. Every other family keeps its prediction and gains the
                    // measurement beside it - see this method's remarks.
                    scores[index] = standing + (simulated - standing) + StaticDelta(
                        commands[index], readModel, ownHeroId,
                        families & ~BotPolicyFamilies.Geometry, weights, policy);
                }
            }

            var best = scores[0];
            for (var index = 1; index < scores.Length; index++)
            {
                if (scores[index] > best)
                {
                    best = scores[index];
                }
            }

            var floor = best - Math.Max(0, nearEqualBand);
            var pool = new List<DirectorCommand>();
            for (var index = 0; index < commands.Count; index++)
            {
                if (scores[index] >= floor)
                {
                    pool.Add(commands[index]);
                }
            }

            return pool.Count == 1 ? pool[0] : pool[random.Next(pool.Count)];
        }

        /// <summary>
        /// How many candidates of one decision the ply is actually spent on.
        /// </summary>
        /// <remarks>
        /// <para><b>A live bot's worst case is what this bounds, and the worst case is not the
        /// average.</b> Measured on the symmetric bench: a decision offers 10.4 candidates on
        /// average and one ply costs about 8.6 ms, so an uncapped tier-2 averages ~90 ms - fine
        /// against the 2.0 s pace a solo player watches. But a commitment decision with six heroes'
        /// worth of offers, or a movement decision over a wide open board, is not the average, and an
        /// uncapped probe there would spend seconds inside one decision and stall the very pace this
        /// bot exists to be watchable at. Capping the probe makes the cost of a decision a bounded
        /// number rather than a property of the position.</para>
        /// <para><b>What it costs in strength is small, because the shortlist is not arbitrary.</b>
        /// The static judgment scores every candidate first and the ply is spent on the best ones -
        /// so the ply refines the shortlist rather than replacing the search. A candidate the static
        /// judgment ranks below the shortlist keeps its static score and can still win if the plies
        /// above it measure worse than they predicted, which is exactly the correction the ply exists
        /// to make.</para>
        /// <para>Six, measured: <c>BotTierTwoTests.TheLookaheadCapDiagnostic</c> plays the cap at 2,
        /// 4, 6 and unbounded. It is the smallest cap whose win rate against tier-1 is not measurably
        /// below the uncapped one.</para>
        /// </remarks>
        public const int LookaheadCandidateCap = 6;

        /// <summary>
        /// The indices of the best <see cref="LookaheadCandidateCap"/> static scores, best first.
        /// </summary>
        /// <remarks>
        /// Ties are broken by index, which keeps the shortlist a deterministic function of the
        /// candidate order - the seeded die decides among near-equals AFTER the plies land, not
        /// before, or the same seed would probe different candidates on two runs of one match.
        /// </remarks>
        private static IEnumerable<int> Shortlist(int[] scores, int cap)
        {
            if (cap <= 0 || scores.Length <= cap)
            {
                return Enumerable.Range(0, scores.Length);
            }

            return Enumerable.Range(0, scores.Length)
                .OrderByDescending(index => scores[index])
                .ThenBy(index => index)
                .Take(cap)
                .ToArray();
        }

        private static bool SameVector(BotEvaluationWeights left, BotEvaluationWeights right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            for (var index = 0; index < BotEvaluationWeights.TermCount; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// What this candidate is worth relative to standing still, from published facts and exact
        /// deltas only. Zero means "no opinion", which leaves the candidate tied with its siblings.
        /// </summary>
        private static int StaticDelta(
            DirectorCommand candidate,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            BotPolicyFamilies families,
            BotEvaluationWeights weights,
            BotPolicyWeights policy)
        {
            switch (candidate.Kind)
            {
                case DirectorCommandKind.ChooseDefenseCard:
                case DirectorCommandKind.DeclineDefense:
                    return Enabled(families, BotPolicyFamilies.Defense)
                        ? DefenseDelta(candidate, readModel, weights, policy) : 0;
                case DirectorCommandKind.ChooseAttackTarget:
                    return Enabled(families, BotPolicyFamilies.AttackTarget)
                        ? AttackTargetDelta(candidate, readModel, weights, policy) : 0;
                case DirectorCommandKind.ChooseMovementDestination:
                case DirectorCommandKind.PerformFastTravel:
                case DirectorCommandKind.ChooseCorrectionDestination:
                    return Enabled(families, BotPolicyFamilies.Geometry)
                        ? GeometryDelta(candidate.Coordinate, readModel, ownHeroId, weights, policy) : 0;
                case DirectorCommandKind.StartAction:
                    return Enabled(families, BotPolicyFamilies.ActionStart)
                        ? ActionStartDelta(candidate, readModel, policy) : 0;
                case DirectorCommandKind.CommitCard:
                    return Enabled(families, BotPolicyFamilies.Commitment)
                        ? CommitDelta(candidate, readModel, ownHeroId, policy) : 0;
                case DirectorCommandKind.Pass when !Enabled(families, BotPolicyFamilies.TurnEconomy):
                case DirectorCommandKind.PerformHold when !Enabled(families, BotPolicyFamilies.TurnEconomy):
                    return 0;
                case DirectorCommandKind.RespawnHero when !Enabled(families, BotPolicyFamilies.Respawn):
                    return 0;
                case DirectorCommandKind.Pass:
                    // Passing forfeits the turn. It stays legal and stays reachable - a hand with
                    // nothing committable leaves it as the only candidate, where a penalty changes
                    // nothing because everything ties.
                    return -policy.Pass;
                case DirectorCommandKind.PerformHold:
                    return -policy.EndTurn;
                case DirectorCommandKind.RespawnHero:
                    return policy.Respawn;
                case DirectorCommandKind.ResolveCardWithoutRespawn:
                    return 0;
                default:
                    return 0;
            }
        }

        private static bool Enabled(BotPolicyFamilies families, BotPolicyFamilies family) =>
            (families & family) == family;

        /// <summary>
        /// Defense spend, by cost and benefit rather than the tier-0.5 lethal-only rule.
        /// </summary>
        /// <remarks>
        /// <para><b>The content-dependency guard is the load-bearing line here and must survive every
        /// later edit.</b> The tier-0.5 pass recorded that in the CURRENT content a Decline is always
        /// <see cref="AttackResolutionOutcome.AttackSuccessful"/>, and that this is an observation
        /// about this build's cards and never an invariant: the design side notes that unimplemented
        /// heroes may carry decline-triggered saves, and that removal prevention already exists as a
        /// post-hit save. So every branch below reads the per-state preview and prices what the
        /// preview actually says. A reader who rediscovers the observation and "simplifies" this into
        /// an unconditional would be writing a rule the game does not have.</para>
        /// <para>The improvement over tier-0.5 is that this spends by comparison rather than by a
        /// single lethal trigger: it will decline when the hit is cheaper than the card, spend the
        /// cheapest sufficient card when it is not, and - unlike the greedy pass - never burn a card
        /// that would not have saved anything.</para>
        /// </remarks>
        private static int DefenseDelta(
            DirectorCommand candidate, IMatchReadModel readModel, BotEvaluationWeights weights,
            BotPolicyWeights policy)
        {
            var preview = readModel.PendingDefensePreview;
            if (preview?.Choices == null)
            {
                return 0;
            }

            var choice = candidate.Kind == DirectorCommandKind.DeclineDefense
                ? preview.Choices.FirstOrDefault(value => value.IsDecline)
                : preview.Choices.FirstOrDefault(value =>
                    !value.IsDecline && value.CardInstanceId.HasValue &&
                    value.CardInstanceId.Value.Equals(candidate.CardInstanceId));
            if (choice == null)
            {
                return 0;
            }

            // Read per state, every time. Never assume which way a Decline resolves.
            var delta = choice.Outcome == AttackResolutionOutcome.AttackSuccessful ? -policy.HitTaken : 0;
            if (!choice.IsDecline)
            {
                // A spent card is a card gone from the hand, and a bigger Defense Total is a bigger
                // card to have spent: among answers that do the same thing, keep the cheapest.
                delta -= policy.DefenseCardSpend + weights.OwnHandSize;
                delta -= choice.DefenseTotal ?? 0;
            }

            return delta;
        }

        /// <summary>
        /// Target choice, priced by what the rulebook says a Defeat is worth rather than by "lowest
        /// Life" alone.
        /// </summary>
        /// <remarks>
        /// p.13 charges the defeated hero's team Life counters equal to that hero's lowest card Tier
        /// and pays the attacker coins equal to that hero's Level, so a levelled enemy is worth more
        /// to defeat than a fresh one. The Tier itself is not seat-visible (it is read from the
        /// hero's cards), so <see cref="BotEvaluation.PublicDefeatLifeCost"/>'s public proxy stands
        /// in - see its remarks for exactly how much of an approximation that is. The team nearest to
        /// losing is still preferred, and the Defense its neighbouring minions would lend it is still
        /// subtracted, both from the authority's own published preview.
        /// </remarks>
        private static int AttackTargetDelta(
            DirectorCommand candidate, IMatchReadModel readModel, BotEvaluationWeights weights,
            BotPolicyWeights policy)
        {
            if (!candidate.HeroId.IsDefined)
            {
                // A minion target. No Life counter rides on it and no published Life fact describes
                // one, so this policy has no opinion between minions - exactly as tier-0.5 had none.
                return 0;
            }

            var progression = (readModel.HeroProgression ?? Array.Empty<HeroProgressionProjection>())
                .FirstOrDefault(row => row != null && row.HeroId.Equals(candidate.HeroId));
            var level = progression?.Level ?? 1;
            var delta = BotEvaluation.PublicDefeatLifeCost(level) * weights.TeamLife;
            delta += level * weights.OwnCoin;

            TeamId targetTeam;
            try
            {
                targetTeam = readModel.Participants.GetTeamForHero(candidate.HeroId).Id;
            }
            catch (Exception)
            {
                return delta;
            }

            var lives = (readModel.TeamLifeCounters ?? Array.Empty<KeyValuePair<TeamId, int>>())
                .Where(entry => entry.Key.Equals(targetTeam))
                .Select(entry => entry.Value)
                .DefaultIfEmpty(int.MaxValue)
                .Min();
            if (lives != int.MaxValue)
            {
                // Nearer to zero is worth more. Bounded so a large counter cannot swamp the rest.
                delta += Math.Max(0, BotEvaluation.ProximityCap - lives) * weights.TeamLife / 4;
            }

            var bonus = (readModel.PendingAttackTargetDefensePreviews ??
                    Array.Empty<AttackTargetDefensePreviewProjection>())
                .FirstOrDefault(value => value.HeroId.Equals(candidate.HeroId));
            if (bonus?.MinionContributions != null)
            {
                delta -= bonus.MinionContributions.Sum(value => value.Amount) * weights.MinionPressure;
            }

            return delta;
        }

        /// <summary>
        /// The exact one-ply the command itself supplies: this hero would stand on
        /// <paramref name="destination"/>, so the geometry terms are recomputed there instead of
        /// estimated. Every other term is unchanged by a step, so the difference of the two totals is
        /// the difference of the geometry.
        /// </summary>
        private static int GeometryDelta(
            HexCoordinate destination, IMatchReadModel readModel, HeroId ownHeroId,
            BotEvaluationWeights weights,
            BotPolicyWeights policy)
        {
            var current = BotEvaluation.Explain(readModel, ownHeroId);
            var enemies = EnemyPositions(readModel, ownHeroId);
            if (enemies.Count == 0)
            {
                return 0;
            }

            var nearest = enemies.Min(position => HexDistanceQuery.Calculate(destination, position));
            var proximity = BotEvaluation.ProximityCap - Math.Min(nearest, BotEvaluation.ProximityCap);
            var adjacent = enemies.Count(position => HexDistanceQuery.Calculate(destination, position) <= 1);

            var before = (current.EngagementProximity * weights.EngagementProximity) -
                (current.AdjacentThreats * current.OwnDefeatLifeCost * weights.ThreatAdjacency);
            var after = (proximity * weights.EngagementProximity) -
                (adjacent * current.OwnDefeatLifeCost * weights.ThreatAdjacency);
            return after - before;
        }

        /// <summary>
        /// Action selection. An offered Attack has already cleared
        /// <c>TurnResolutionAuthority.CanStartAttack</c>, which refuses to offer an Attack with
        /// nobody to hit, so an Attack on this list is an Attack with a legal target - the tier-0.5
        /// pass established that and it still holds. Movement is next, because closing on the fight
        /// is what makes the next Attack possible. Everything else is left alone.
        /// </summary>
        private static int ActionStartDelta(
            DirectorCommand candidate, IMatchReadModel readModel, BotPolicyWeights policy)
        {
            switch (ActionKindOf(readModel, candidate.CardInstanceId, candidate.ActionDefinitionId))
            {
                case ActionKind.Attack:
                    return policy.AttackAction;
                case ActionKind.Movement:
                case ActionKind.FastTravel:
                    return policy.MovementAction;
                case ActionKind.Hold:
                    return -policy.EndTurn;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// COMMITMENT CHOICE - the decision the amendment calls the biggest skill decision, and the
        /// one tier-0.5 deliberately left to the die.
        /// </summary>
        /// <remarks>
        /// Judged entirely from a card's printed structure: whether it offers an Attack that could
        /// actually reach something from where this hero stands (using the card's OWN printed Range,
        /// not an invented threshold), whether it offers Movement when there is ground to cover, its
        /// printed Initiative - weighted NEGATIVELY, for the measured reason
        /// <see cref="BotPolicyWeights.CommitInitiative"/> records at length - and the size of its printed
        /// values. No card is named and no card's text is read.
        /// </remarks>
        private static int CommitDelta(
            DirectorCommand candidate, IMatchReadModel readModel, HeroId ownHeroId,
            BotPolicyWeights policy)
        {
            var definition = DefinitionOf(readModel, candidate.CardInstanceId);
            if (definition == null)
            {
                return 0;
            }

            var delta = definition.Initiative.Value * policy.CommitInitiative;

            var enemies = EnemyPositions(readModel, ownHeroId);
            var own = (readModel.Heroes ?? Array.Empty<HeroOccupancy>())
                .Where(occupancy => occupancy.HeroId.Equals(ownHeroId))
                .Select(occupancy => (HexCoordinate?)occupancy.Coordinate)
                .FirstOrDefault();
            var nearest = own.HasValue && enemies.Count > 0
                ? enemies.Min(position => HexDistanceQuery.Calculate(own.Value, position))
                : int.MaxValue;

            foreach (var action in definition.ActionDefinitions ?? Array.Empty<ActionDefinition>())
            {
                if (action == null)
                {
                    continue;
                }

                var printed = action.BaseValues ?? Array.Empty<PrintedValueDefinition>();
                switch (action.Kind)
                {
                    case ActionKind.Attack:
                        // The card's own printed Range decides what "in reach" means for it. A card
                        // that prints none is a melee card, so reach is adjacency.
                        var reach = Math.Max(1, printed
                            .Where(value => value.Channel == PrintedValueChannel.Range)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max());
                        if (nearest <= reach)
                        {
                            delta += policy.CommitReachableAttack;
                        }

                        delta += printed.Where(value => value.Channel == PrintedValueChannel.Attack)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max() * policy.CommitPrintedValue;
                        break;
                    case ActionKind.Movement:
                    case ActionKind.FastTravel:
                        var distance = printed.Where(value => value.Channel == PrintedValueChannel.Movement)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max();
                        if (nearest != int.MaxValue && nearest > 1)
                        {
                            delta += policy.CommitMovement + (Math.Min(distance, nearest) * policy.CommitPrintedValue);
                        }

                        break;
                    case ActionKind.Defense:
                        delta += printed.Where(value => value.Channel == PrintedValueChannel.Defense)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max() * policy.CommitPrintedValue / 2;
                        break;
                }
            }

            return delta;
        }

        private static List<HexCoordinate> EnemyPositions(IMatchReadModel readModel, HeroId ownHeroId)
        {
            var positions = new List<HexCoordinate>();
            TeamId ownTeamId;
            try
            {
                ownTeamId = readModel.Participants.GetTeamForHero(ownHeroId).Id;
            }
            catch (Exception)
            {
                return positions;
            }

            foreach (var occupancy in readModel.Heroes ?? Array.Empty<HeroOccupancy>())
            {
                if (occupancy.HeroId.Equals(ownHeroId))
                {
                    continue;
                }

                try
                {
                    if (!readModel.Participants.GetTeamForHero(occupancy.HeroId).Id.Equals(ownTeamId))
                    {
                        positions.Add(occupancy.Coordinate);
                    }
                }
                catch (Exception)
                {
                    // A hero the graph cannot place on a team is not one this policy takes a side on.
                }
            }

            foreach (var minion in readModel.Minions ??
                Array.Empty<Core.Units.Minions.MinionOccupancy>())
            {
                if (!minion.TeamId.Equals(ownTeamId))
                {
                    positions.Add(minion.Coordinate);
                }
            }

            return positions;
        }

        private static CardDefinition DefinitionOf(IMatchReadModel readModel, CardInstanceId cardInstanceId)
        {
            var card = (readModel.Cards ?? Array.Empty<CardInstance>())
                .FirstOrDefault(value => value != null && value.Id.Equals(cardInstanceId));
            return card == null
                ? null
                : (readModel.Definitions ?? Array.Empty<CardDefinition>())
                    .FirstOrDefault(value => value != null && value.Reference.Equals(card.DefinitionReference));
        }

        private static ActionKind ActionKindOf(
            IMatchReadModel readModel, CardInstanceId cardInstanceId, ActionDefinitionId actionDefinitionId)
        {
            var definition = DefinitionOf(readModel, cardInstanceId);
            var action = definition?.ActionDefinitions?.FirstOrDefault(
                value => value != null && value.Id.Equals(actionDefinitionId));
            return action?.Kind ?? ActionKind.None;
        }

        // ---------------------------------------------------------------------------------------
        // Tier-0.5, moved verbatim. Behaviour must not drift: PrivateSeatDriverGreedyPreferenceTests
        // still drives these through the driver and is the proof that the move was faithful.
        // ---------------------------------------------------------------------------------------

        public static DirectorCommand PreferAttackStart(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, IBotRandom random)
        {
            var starts = commands.Where(command => command.Kind == DirectorCommandKind.StartAction).ToArray();
            if (starts.Length == 0)
            {
                return null;
            }

            var attackStarts = starts.Where(command =>
                ActionKindOf(readModel, command.CardInstanceId, command.ActionDefinitionId) ==
                ActionKind.Attack).ToArray();
            return attackStarts.Length == 0 ? null : attackStarts[random.Next(attackStarts.Length)];
        }

        public static DirectorCommand PreferKillableAttackTarget(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, IBotRandom random)
        {
            var targets = commands.Where(command =>
                command.Kind == DirectorCommandKind.ChooseAttackTarget && command.HeroId.IsDefined).ToArray();
            if (targets.Length == 0)
            {
                return null;
            }

            var lifeByTeam = (readModel.TeamLifeCounters ?? Array.Empty<KeyValuePair<TeamId, int>>())
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            var previews = readModel.PendingAttackTargetDefensePreviews ??
                Array.Empty<AttackTargetDefensePreviewProjection>();

            var scored = targets
                .Select(command =>
                {
                    var team = readModel.Participants.GetTeamForHero(command.HeroId);
                    var life = lifeByTeam.TryGetValue(team.Id, out var counters) ? counters : int.MaxValue;
                    var preview = previews.FirstOrDefault(value => value.HeroId.Equals(command.HeroId));
                    var defenseBonus = preview?.MinionContributions.Sum(value => value.Amount) ?? 0;
                    return (Command: command, Life: life, Bonus: defenseBonus);
                })
                .OrderBy(value => value.Life)
                .ThenBy(value => value.Bonus)
                .ToArray();

            var best = scored[0];
            var tied = scored
                .Where(value => value.Life == best.Life && value.Bonus == best.Bonus)
                .Select(value => value.Command)
                .ToArray();
            return tied[random.Next(tied.Length)];
        }

        public static DirectorCommand PreferAdvancingMovement(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, HeroId ownHeroId, IBotRandom random)
        {
            var moves = commands.Where(command =>
                command.Kind == DirectorCommandKind.ChooseMovementDestination).ToArray();
            if (moves.Length == 0)
            {
                return null;
            }

            var objectives = EnemyPositions(readModel, ownHeroId).ToArray();
            if (objectives.Length == 0)
            {
                return null;
            }

            var scored = moves
                .Select(command => (
                    Command: command,
                    Distance: objectives.Min(objective => HexDistanceQuery.Calculate(command.Coordinate, objective))))
                .OrderBy(value => value.Distance)
                .ToArray();
            var bestDistance = scored[0].Distance;
            var tied = scored
                .Where(value => value.Distance == bestDistance)
                .Select(value => value.Command)
                .ToArray();
            return tied[random.Next(tied.Length)];
        }

        public static DirectorCommand PreferSufficientDefense(
            IReadOnlyList<DirectorCommand> commands, IMatchReadModel readModel, IBotRandom random)
        {
            var defenseCommands = commands.Where(command =>
                command.Kind == DirectorCommandKind.ChooseDefenseCard ||
                command.Kind == DirectorCommandKind.DeclineDefense).ToArray();
            if (defenseCommands.Length == 0)
            {
                return null;
            }

            var preview = readModel.PendingDefensePreview;
            var decline = preview?.Choices.FirstOrDefault(choice => choice.IsDecline);
            if (decline == null || decline.Outcome != AttackResolutionOutcome.AttackSuccessful)
            {
                return null;
            }

            var sufficientCardIds = preview.Choices
                .Where(choice => !choice.IsDecline && choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended)
                .OrderBy(choice => choice.DefenseTotal ?? int.MaxValue)
                .ThenBy(choice => choice.CardInstanceId.Value.Value, StringComparer.Ordinal)
                .Select(choice => choice.CardInstanceId.Value)
                .ToArray();

            foreach (var cardId in sufficientCardIds)
            {
                var match = defenseCommands.FirstOrDefault(command =>
                    command.Kind == DirectorCommandKind.ChooseDefenseCard && command.CardInstanceId.Equals(cardId));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}

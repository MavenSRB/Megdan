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
        /// What a landed attack on this hero is treated as costing. Deliberately large and
        /// deliberately NOT <see cref="BotEvaluation.TeamLifeWeight"/>.
        /// </summary>
        /// <remarks>
        /// A successful attack is not the same event as a Defeat, and a Defeat is not always one Life
        /// counter (p.13 prices it by the defeated hero's lowest card Tier). Pricing a hit as "one
        /// Life" would be asserting a rule the game does not have. This is an ordering statement -
        /// taking a hit is worse than spending any card and better than losing the match - and it is
        /// applied only where the authority's own preview says a hit is what would happen.
        /// </remarks>
        public const int HitTakenCost = 600;

        /// <summary>What spending a Defense card costs beyond the hand-size term: the card is gone.</summary>
        public const int DefenseCardSpendCost = 20;

        public const int AttackActionBonus = 120;
        public const int MovementActionBonus = 40;
        public const int EndTurnPenalty = 200;

        /// <summary>
        /// <b>NEGATIVE ON PURPOSE. Do not "fix" the sign.</b> A lower printed Initiative is preferred,
        /// which means this bot deliberately tries to act LATER in the round.
        /// </summary>
        /// <remarks>
        /// <para>This started at <c>+10</c>, from a fact that is true and an inference from it that
        /// was not. The fact: <c>InitiativeResolutionAuthority</c> takes the MAXIMUM Initiative total,
        /// so a higher printed Initiative genuinely does act first. The inference - that acting first
        /// is therefore worth having - is what self-play falsified.</para>
        /// <para>Measured, on the symmetric six-player bench, tier-1 against the tier-0.5 pass over
        /// twenty seeds played from both sides: at <c>+10</c> the win rate was <b>51.3%</b> and tier-1
        /// won only 3 of 20 matches from the side that moves first; at <c>-10</c> it was <b>82.5%</b>
        /// and 13 of 20 from that same side. Nothing else changed. Held-out seeds the weight was NOT
        /// chosen on give <b>80.0%</b>, so this is not twenty matches' worth of luck.</para>
        /// <para>Why it should be so is a game-design observation this milestone does not claim to
        /// have proven: committing later means committing against a revealed board, and in a race
        /// decided by Life counters the second attacker in an exchange appears to keep the initiative
        /// that matters. Recorded as a measurement, not as a rule - it is a property of the current
        /// content and match length, exactly the kind of thing the roster expansion could move, which
        /// is why the self-play gate is a test and not a note.</para>
        /// </remarks>
        public const int CommitInitiativeWeight = -10;
        public const int CommitReachableAttackBonus = 90;
        public const int CommitMovementBonus = 45;
        public const int CommitPrintedValueWeight = 6;
        public const int PassPenalty = 400;
        public const int RespawnBonus = 500;

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
            BotPolicyFamilies families = BotPolicyFamilies.All)
        {
            if (commands == null || commands.Count == 0 || random == null)
            {
                return null;
            }

            if (readModel == null || !ownHeroId.IsDefined)
            {
                return commands[random.Next(commands.Count)];
            }

            var standing = BotEvaluation.Evaluate(readModel, ownHeroId);
            var scores = new int[commands.Count];

            if (lookahead is SessionBotLookahead session)
            {
                session.BeginDecision();
            }

            for (var index = 0; index < commands.Count; index++)
            {
                var candidate = commands[index];
                if (lookahead != null &&
                    lookahead.TryEvaluateAfter(candidate, ownHeroId, out var simulated))
                {
                    scores[index] = simulated;
                    continue;
                }

                scores[index] = standing +
                    StaticDelta(candidate, readModel, ownHeroId, standing, families);
            }

            var best = scores[0];
            for (var index = 1; index < scores.Length; index++)
            {
                if (scores[index] > best)
                {
                    best = scores[index];
                }
            }

            var tied = new List<DirectorCommand>();
            for (var index = 0; index < commands.Count; index++)
            {
                if (scores[index] == best)
                {
                    tied.Add(commands[index]);
                }
            }

            return tied.Count == 1 ? tied[0] : tied[random.Next(tied.Count)];
        }

        /// <summary>
        /// What this candidate is worth relative to standing still, from published facts and exact
        /// deltas only. Zero means "no opinion", which leaves the candidate tied with its siblings.
        /// </summary>
        private static int StaticDelta(
            DirectorCommand candidate,
            IMatchReadModel readModel,
            HeroId ownHeroId,
            int standing,
            BotPolicyFamilies families)
        {
            switch (candidate.Kind)
            {
                case DirectorCommandKind.ChooseDefenseCard:
                case DirectorCommandKind.DeclineDefense:
                    return Enabled(families, BotPolicyFamilies.Defense)
                        ? DefenseDelta(candidate, readModel) : 0;
                case DirectorCommandKind.ChooseAttackTarget:
                    return Enabled(families, BotPolicyFamilies.AttackTarget)
                        ? AttackTargetDelta(candidate, readModel) : 0;
                case DirectorCommandKind.ChooseMovementDestination:
                case DirectorCommandKind.PerformFastTravel:
                case DirectorCommandKind.ChooseCorrectionDestination:
                    return Enabled(families, BotPolicyFamilies.Geometry)
                        ? GeometryDelta(candidate.Coordinate, readModel, ownHeroId, standing) : 0;
                case DirectorCommandKind.StartAction:
                    return Enabled(families, BotPolicyFamilies.ActionStart)
                        ? ActionStartDelta(candidate, readModel) : 0;
                case DirectorCommandKind.CommitCard:
                    return Enabled(families, BotPolicyFamilies.Commitment)
                        ? CommitDelta(candidate, readModel, ownHeroId) : 0;
                case DirectorCommandKind.Pass when !Enabled(families, BotPolicyFamilies.TurnEconomy):
                case DirectorCommandKind.PerformHold when !Enabled(families, BotPolicyFamilies.TurnEconomy):
                    return 0;
                case DirectorCommandKind.RespawnHero when !Enabled(families, BotPolicyFamilies.Respawn):
                    return 0;
                case DirectorCommandKind.Pass:
                    // Passing forfeits the turn. It stays legal and stays reachable - a hand with
                    // nothing committable leaves it as the only candidate, where a penalty changes
                    // nothing because everything ties.
                    return -PassPenalty;
                case DirectorCommandKind.PerformHold:
                    return -EndTurnPenalty;
                case DirectorCommandKind.RespawnHero:
                    return RespawnBonus;
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
        private static int DefenseDelta(DirectorCommand candidate, IMatchReadModel readModel)
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
            var delta = choice.Outcome == AttackResolutionOutcome.AttackSuccessful ? -HitTakenCost : 0;
            if (!choice.IsDecline)
            {
                // A spent card is a card gone from the hand, and a bigger Defense Total is a bigger
                // card to have spent: among answers that do the same thing, keep the cheapest.
                delta -= DefenseCardSpendCost + BotEvaluation.OwnHandSizeWeight;
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
        private static int AttackTargetDelta(DirectorCommand candidate, IMatchReadModel readModel)
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
            var delta = BotEvaluation.PublicDefeatLifeCost(level) * BotEvaluation.TeamLifeWeight;
            delta += level * BotEvaluation.OwnCoinWeight;

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
                delta += Math.Max(0, BotEvaluation.ProximityCap - lives) * BotEvaluation.TeamLifeWeight / 4;
            }

            var bonus = (readModel.PendingAttackTargetDefensePreviews ??
                    Array.Empty<AttackTargetDefensePreviewProjection>())
                .FirstOrDefault(value => value.HeroId.Equals(candidate.HeroId));
            if (bonus?.MinionContributions != null)
            {
                delta -= bonus.MinionContributions.Sum(value => value.Amount) * BotEvaluation.MinionPressureWeight;
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
            HexCoordinate destination, IMatchReadModel readModel, HeroId ownHeroId, int standing)
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

            var before = (current.EngagementProximity * BotEvaluation.EngagementProximityWeight) -
                (current.AdjacentThreats * current.OwnDefeatLifeCost * BotEvaluation.ThreatAdjacencyWeight);
            var after = (proximity * BotEvaluation.EngagementProximityWeight) -
                (adjacent * current.OwnDefeatLifeCost * BotEvaluation.ThreatAdjacencyWeight);
            return after - before;
        }

        /// <summary>
        /// Action selection. An offered Attack has already cleared
        /// <c>TurnResolutionAuthority.CanStartAttack</c>, which refuses to offer an Attack with
        /// nobody to hit, so an Attack on this list is an Attack with a legal target - the tier-0.5
        /// pass established that and it still holds. Movement is next, because closing on the fight
        /// is what makes the next Attack possible. Everything else is left alone.
        /// </summary>
        private static int ActionStartDelta(DirectorCommand candidate, IMatchReadModel readModel)
        {
            switch (ActionKindOf(readModel, candidate.CardInstanceId, candidate.ActionDefinitionId))
            {
                case ActionKind.Attack:
                    return AttackActionBonus;
                case ActionKind.Movement:
                case ActionKind.FastTravel:
                    return MovementActionBonus;
                case ActionKind.Hold:
                    return -EndTurnPenalty;
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
        /// <see cref="CommitInitiativeWeight"/> records at length - and the size of its printed
        /// values. No card is named and no card's text is read.
        /// </remarks>
        private static int CommitDelta(DirectorCommand candidate, IMatchReadModel readModel, HeroId ownHeroId)
        {
            var definition = DefinitionOf(readModel, candidate.CardInstanceId);
            if (definition == null)
            {
                return 0;
            }

            var delta = definition.Initiative.Value * CommitInitiativeWeight;

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
                            delta += CommitReachableAttackBonus;
                        }

                        delta += printed.Where(value => value.Channel == PrintedValueChannel.Attack)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max() * CommitPrintedValueWeight;
                        break;
                    case ActionKind.Movement:
                    case ActionKind.FastTravel:
                        var distance = printed.Where(value => value.Channel == PrintedValueChannel.Movement)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max();
                        if (nearest != int.MaxValue && nearest > 1)
                        {
                            delta += CommitMovementBonus + (Math.Min(distance, nearest) * CommitPrintedValueWeight);
                        }

                        break;
                    case ActionKind.Defense:
                        delta += printed.Where(value => value.Channel == PrintedValueChannel.Defense)
                            .Select(value => value.Amount).DefaultIfEmpty(0).Max() * CommitPrintedValueWeight / 2;
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

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Play
{
    public enum PveStepKind
    {
        /// <summary>Nothing automatic is owed: the match waits on the player.</summary>
        AwaitingPlayer = 0,

        /// <summary>The enemies or the encounter's own flow take this command next.</summary>
        Command = 1,

        /// <summary>The player settles a tie on an enemy's behalf (PVE-D10/D12/D20).</summary>
        Prompt = 2,

        /// <summary>No creep remains, or the engine ended the match (PVE-D04).</summary>
        EncounterLost = 3
    }

    public sealed class PveStep
    {
        private PveStep(PveStepKind kind, DirectorCommand command, PvePrompt prompt, string note)
        {
            Kind = kind;
            Command = command;
            Prompt = prompt;
            Note = note;
        }

        public PveStepKind Kind { get; }
        public DirectorCommand Command { get; }
        public PvePrompt Prompt { get; }

        /// <summary>A short reason for the log; "PVE_FALLBACK" marks a decision no PvE rule names.</summary>
        public string Note { get; }

        public static readonly PveStep AwaitingPlayer = new PveStep(PveStepKind.AwaitingPlayer, null, null, null);
        public static readonly PveStep Lost = new PveStep(PveStepKind.EncounterLost, null, null, null);

        public static PveStep Run(DirectorCommand command, string note) =>
            new PveStep(PveStepKind.Command, command ?? throw new ArgumentNullException(nameof(command)), null, note);

        public static PveStep Ask(PvePrompt prompt) =>
            new PveStep(PveStepKind.Prompt, null, prompt ?? throw new ArgumentNullException(nameof(prompt)), prompt.Text);
    }

    /// <summary>
    /// The enemies' side of the encounter (PVE-D05/D07-D12/D19-D21) and the encounter's own flow:
    /// round-start placement, enemy commitments from their public plans, each enemy's first legal
    /// option, defense, never respawning and advancing the end of round. It reads the match and
    /// names one command at a time; it owns no state, so Undo, restart and replay need nothing from it.
    /// </summary>
    public sealed class PveEnemyController
    {
        private readonly PveComposedEncounter encounter;
        private readonly PveCardIndex cards;
        private readonly Dictionary<HeroId, PveEnemyRecord> enemies;
        private readonly MatchModeRules modeRules;

        public PveEnemyController(PveComposedEncounter encounter)
        {
            this.encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            cards = new PveCardIndex(encounter.Catalog);
            enemies = encounter.Enemies.ToDictionary(enemy => enemy.HeroId);
            modeRules = encounter.InitialState.ModeRules;
        }

        public bool IsEnemy(HeroId heroId) => heroId.IsDefined && enemies.ContainsKey(heroId);

        /// <summary>PVE-D04. Lost when no Blue creep stands, or when the engine itself ended the match.</summary>
        public static bool IsLost(IMatchReadModel match) =>
            match.Phase == RoundResolutionPhase.GameEnded ||
            !match.Minions.Any(minion => minion.TeamSide == TeamSide.Blue);

        public PveStep NextStep(
            IMatchReadModel match, Func<HeroId, IReadOnlyList<DirectorCommand>> authorizedFor = null)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (IsLost(match)) return PveStep.Lost;

            var pending = match.PendingSystemTransitionStep;
            if (pending != null) return TransitionStep(pending);

            if (match.Phase == RoundResolutionPhase.EndOfRound)
            {
                return match.DecisionContract == null
                    ? PveStep.Run(DirectorCommand.AdvanceEndOfRound(), "end of round")
                    : DecisionStep(match, authorizedFor);
            }

            return match.Phase == RoundResolutionPhase.AwaitingCommitments
                ? CommitmentStep(match)
                : DecisionStep(match, authorizedFor);
        }

        /// <summary>The scheduled heroes of this round still off the board.</summary>
        public IReadOnlyList<ModeHeroPlacement> OutstandingPlacements(IMatchReadModel match) =>
            modeRules == null
                ? Array.Empty<ModeHeroPlacement>()
                : modeRules.RoundStartPlacements
                    .Where(placement => placement.Round == match.RoundIndex && !IsOnBoard(match, placement.HeroId))
                    .ToArray();

        private PveStep TransitionStep(PendingSystemTransitionStep pending)
        {
            if (MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _))
            {
                var enemyItem = pending.OpenItems.FirstOrDefault(item => item.ChoosingSide == TeamSide.Red);
                if (enemyItem != null &&
                    ModeRoundStartPlacementRules.TryReadItem(enemyItem.ItemId, out var enemy))
                {
                    return PveStep.Ask(new PvePrompt(
                        PvePromptKind.EnemySpawnSpace,
                        enemy,
                        Name(enemy) + "'s spawn space is taken: choose where it enters.",
                        enemyItem.Options.Select(option => DirectorCommand.AnswerSystemTransitionStep(
                            new SystemTransitionChoiceCommand(
                                pending.InstanceId, pending.StepId, enemyItem.ItemId, TeamSide.Red, option)))));
                }
            }

            return PveStep.AwaitingPlayer;
        }

        private PveStep CommitmentStep(IMatchReadModel match)
        {
            var nobodyResponded = match.Commitments.All(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            if (match.TurnIndex == 1 && nobodyResponded && OutstandingPlacements(match).Count > 0 &&
                match.SelectedMap.PlayableCoordinates.Any(space => IsEmpty(match, space)))
            {
                return PveStep.Run(
                    DirectorCommand.BeginSystemTransition(MatchModeRules.PlacementTransitionId(match.RoundIndex)),
                    "round " + match.RoundIndex + " placement");
            }

            foreach (var enemy in encounter.Enemies)
            {
                var commitment = match.Commitments.FirstOrDefault(value => value.HeroId.Equals(enemy.HeroId));
                if (!commitment.HeroId.IsDefined || commitment.Status != TurnCardCommitmentStatus.AwaitingResponse)
                {
                    continue;
                }

                var card = CommitmentCard(match, enemy);
                return card == null
                    ? PveStep.Run(DirectorCommand.Pass(enemy.HeroId), enemy.DisplayName + " passes")
                    : PveStep.Run(DirectorCommand.CommitCard(enemy.HeroId, card.Id), enemy.DisplayName + " commits");
            }

            return PveStep.AwaitingPlayer;
        }

        /// <summary>
        /// PVE-D07/D21. The card an enemy commits this turn: its planned card; otherwise a non-purple
        /// card it holds that no later turn of the round plans; otherwise its weakest purple card,
        /// otherwise any card it holds (and it will hold the turn).
        /// </summary>
        public CardInstance CommitmentCard(IMatchReadModel match, PveEnemyRecord enemy)
        {
            var hand = HandOf(match, enemy.HeroId);
            if (hand.Count == 0) return null;
            var plan = PvePlans.PlanFor(encounter, enemy, match.RoundIndex);
            var planned = match.TurnIndex >= 1 && match.TurnIndex <= plan.Count
                ? enemy.Type.FindOrange(plan[match.TurnIndex - 1])
                : null;
            if (planned != null)
            {
                var plannedCard = hand.FirstOrDefault(card => cards.Orange(card.DefinitionReference.DefinitionId) == planned);
                if (plannedCard != null) return plannedCard;
            }

            var unplanned = UnreservedOrange(match, enemy, hand);
            if (unplanned != null) return unplanned;
            return hand.Where(card => cards.Purple(card.DefinitionReference.DefinitionId) != null)
                       .OrderBy(card => cards.Purple(card.DefinitionReference.DefinitionId).Defense)
                       .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                       .FirstOrDefault()
                   ?? hand.First();
        }

        private CardInstance UnreservedOrange(IMatchReadModel match, PveEnemyRecord enemy, IReadOnlyList<CardInstance> hand)
        {
            var plan = PvePlans.PlanFor(encounter, enemy, match.RoundIndex);
            var reserved = plan.Skip(Math.Max(0, match.TurnIndex)).ToList();
            foreach (var card in hand)
            {
                var orange = cards.Orange(card.DefinitionReference.DefinitionId);
                if (orange == null) continue;
                if (reserved.Remove(orange.Key)) continue;
                return card;
            }

            return null;
        }

        private PveStep DecisionStep(IMatchReadModel match, Func<HeroId, IReadOnlyList<DirectorCommand>> authorizedFor)
        {
            var initiative = match.PendingInitiativeChoice;
            if (initiative != null && initiative.OwningSide == TeamSide.Red)
            {
                // An enemy off the board resolves its card without acting, so a tie in which at most
                // one tied enemy stands has only one observable order; it is settled without asking.
                var standing = initiative.CandidateCardInstanceIds
                    .Where(card => IsOnBoard(match, OwnerOf(match, card)))
                    .ToArray();
                if (standing.Length <= 1)
                {
                    return PveStep.Run(
                        DirectorCommand.ChooseTiedActor(
                            TeamSide.Red, standing.Length == 1 ? standing[0] : initiative.CandidateCardInstanceIds[0]),
                        "enemy tie with one observable order");
                }
            }

            if (initiative != null)
            {
                return initiative.OwningSide == TeamSide.Red
                    ? PveStep.Ask(new PvePrompt(
                        PvePromptKind.EnemyInitiativeOrder,
                        default,
                        "Enemies tie at Initiative " + initiative.InitiativeTotal + ": choose which acts first.",
                        initiative.CandidateCardInstanceIds.Select(card => DirectorCommand.ChooseTiedActor(TeamSide.Red, card))))
                    : PveStep.AwaitingPlayer;
            }

            var respawn = match.PendingHeroRespawnChoice;
            if (respawn != null && IsEnemy(respawn.HeroId))
            {
                return PveStep.Run(
                    DirectorCommand.ResolveCardWithoutRespawn(respawn.DeclineInvocation),
                    Name(respawn.HeroId) + " stays off the board");
            }

            var defense = match.PendingDefenseChoice;
            if (defense != null && IsEnemy(defense.ChooserId))
            {
                return DefenseStep(match, defense.ChooserId);
            }

            var movement = match.PendingMovementChoice;
            if (movement != null && IsEnemy(movement.ChooserId))
            {
                return MovementStep(match, movement);
            }

            var attack = match.PendingAttackTargetChoice;
            if (attack != null && IsEnemy(attack.ChooserId))
            {
                return AttackStep(match, attack);
            }

            var release = match.PendingTurnRelease;
            if (release != null && IsEnemy(release.ActorId))
            {
                return PveStep.Run(DirectorCommand.ReleaseTurn(), Name(release.ActorId) + " ends its turn");
            }

            var owner = match.DecisionOwner;
            if (owner.IsDefined)
            {
                var ownerEnemy = owner.HeroIds.FirstOrDefault(IsEnemy);
                if (ownerEnemy.IsDefined) return FallbackStep(match, ownerEnemy, authorizedFor);
                if (owner.IsTeamOwned && owner.TeamSide == TeamSide.Red)
                {
                    foreach (var enemy in encounter.Enemies)
                    {
                        var step = FallbackStep(match, enemy.HeroId, authorizedFor);
                        if (step.Kind == PveStepKind.Command) return step;
                    }
                }

                // W-026. Any other standing decision is a Blue hero's (a Removal prevention during an
                // enemy's turn, say): the enemy's turn waits for it rather than acting past it.
                return PveStep.AwaitingPlayer;
            }

            var actor = match.SelectedActor;
            if (actor.ActorId.IsDefined && IsEnemy(actor.ActorId) &&
                !match.HasPendingAttackResolution && !match.HasPendingMovementResolution &&
                !match.HasPendingSkillResolution &&
                (match.AvailableTurnActions.Count > 0 || match.AvailableHoldOffer.HasValue))
            {
                return ActionStep(match, actor);
            }

            return PveStep.AwaitingPlayer;
        }

        /// <summary>
        /// PVE-D07/D08. The first option of the committed card that applies now: an Attack with a
        /// target of its kind in reach, or a Move toward the nearest target of its kind not yet within
        /// its stop distance. With none, the enemy holds.
        /// </summary>
        private PveStep ActionStep(IMatchReadModel match, SelectedInitiativeActor actor)
        {
            var card = match.Cards.FirstOrDefault(value => value.Id.Equals(actor.CardInstanceId));
            var orange = card == null ? null : cards.Orange(card.DefinitionReference.DefinitionId);
            var enemy = enemies[actor.ActorId];
            var position = PositionOf(match, actor.ActorId);
            if (orange != null && !orange.IsIdle && position.HasValue && Acts(match, enemy, card, orange))
            {
                if (orange.Skill != PveSkillKind.None)
                {
                    var skill = Offer(match, card.Id, PveCardFactory.SkillActionId);
                    if (skill.HasValue && skill.Value.IsAvailable)
                        return PveStep.Run(DirectorCommand.StartAction(card.Id, PveCardFactory.SkillActionId),
                            enemy.DisplayName + " uses " + orange.Name);
                }
                foreach (var option in orange.Options)
                {
                    var targets = Targets(match, actor.ActorId, option.Target);
                    if (option.Kind == PveOptionKind.Attack)
                    {
                        var offer = Offer(match, card.Id, PveCardFactory.AttackActionId);
                        // Rule-derived values are not the optional on-card display deltas. Retain
                        // this option's target kind while honoring modified reach; the offer still
                        // owns legality and AttackStep chooses only authoritative legal targets.
                        var rangeBonus = match is MatchSessionProjection authority
                            ? authority.ModeCardRuleValues.Where(value => value.Owner.Equals(actor.ActorId) &&
                                value.Definition.Equals(card.DefinitionReference) && value.Channel == PrintedValueChannel.Range)
                                .Sum(value => value.Bonus) : 0;
                        var reach = Math.Max(1, orange.AttackRange + rangeBonus);
                        if (offer.HasValue && offer.Value.IsAvailable &&
                            targets.Any(target => HexDistanceQuery.Calculate(position.Value, target) <= reach))
                        {
                            return PveStep.Run(
                                DirectorCommand.StartAction(card.Id, PveCardFactory.AttackActionId),
                                enemy.DisplayName + " attacks");
                        }
                    }
                    else if (option.Kind == PveOptionKind.Move)
                    {
                        var offer = Offer(match, card.Id, PveCardFactory.MovementActionId);
                        if (offer.HasValue && offer.Value.IsAvailable && targets.Count > 0 &&
                            targets.Min(target => HexDistanceQuery.Calculate(position.Value, target)) > option.StopDistance)
                        {
                            return PveStep.Run(
                                DirectorCommand.StartAction(card.Id, PveCardFactory.MovementActionId),
                                enemy.DisplayName + " moves toward a " + option.Target.ToString().ToLowerInvariant());
                        }
                    }
                }
            }

            var hold = match.AvailableHoldOffer;
            if (hold.HasValue)
            {
                return PveStep.Run(DirectorCommand.PerformHold(hold.Value.SourceCardInstanceId), enemy.DisplayName + " holds");
            }

            // No PvE option and no Hold offered: fall back to the first offered action.
            var first = match.AvailableTurnActions.FirstOrDefault(value => value.IsAvailable);
            return first.ActionDefinitionId.IsDefined
                ? PveStep.Run(DirectorCommand.StartAction(first.SourceCardInstanceId, first.ActionDefinitionId), "PVE_FALLBACK action")
                : PveStep.AwaitingPlayer;
        }

        /// <summary>PVE-D21. Whether the committed card is one the enemy acts with this turn.</summary>
        private bool Acts(IMatchReadModel match, PveEnemyRecord enemy, CardInstance card, PveOrangeCard orange)
        {
            var planned = PvePlans.PlannedCard(encounter, enemy, match.RoundIndex, match.TurnIndex);
            if (planned == orange) return true;
            var reserved = PvePlans.PlanFor(encounter, enemy, match.RoundIndex).Skip(Math.Max(0, match.TurnIndex)).ToList();
            var held = HandOf(match, enemy.HeroId)
                .Select(value => cards.Orange(value.DefinitionReference.DefinitionId))
                .Where(value => value != null)
                .Count(value => value == orange);
            // Acts unless every copy of this card still owed to a later turn is accounted for by what remains in hand.
            return reserved.Count(key => key == orange.Key) <= held;
        }

        /// <summary>PVE-D08/D10. Move to the destination nearest the targets (no nearer than the stop distance).</summary>
        private PveStep MovementStep(IMatchReadModel match, PendingMovementChoice movement)
        {
            var actorCard = match.Cards.FirstOrDefault(value => value.Id.Equals(match.SelectedActor.CardInstanceId));
            var orange = actorCard == null ? null : cards.Orange(actorCard.DefinitionReference.DefinitionId);
            var option = orange?.Options.FirstOrDefault(value => value.Kind == PveOptionKind.Move &&
                Targets(match, movement.MovingHeroId, value.Target).Count > 0);
            var targets = option == null
                ? Array.Empty<HexCoordinate>()
                : Targets(match, movement.MovingHeroId, option.Target);
            var stop = option?.StopDistance ?? 1;
            int Score(HexCoordinate space) =>
                targets.Count == 0 ? 0 : Math.Max(stop, targets.Min(target => HexDistanceQuery.Calculate(space, target)));

            var here = Score(movement.Origin);
            var destinations = movement.LegalDestinations.Select(destination => destination.Destination).Distinct().ToArray();
            if (targets.Count == 0 || destinations.Length == 0 || destinations.Min(Score) >= here)
            {
                return movement.CanStop
                    ? PveStep.Run(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId), Name(movement.MovingHeroId) + " stops")
                    : PveStep.Run(DirectorCommand.ChooseMovementDestination(movement.ResolutionId, movement.ChoiceId, destinations.FirstOrDefault()), "PVE_FALLBACK move");
            }

            var best = destinations.Min(Score);
            var tied = destinations.Where(space => Score(space) == best).ToArray();
            var cheapest = tied.Min(space => Cost(movement, space));
            tied = tied.Where(space => Cost(movement, space) == cheapest)
                .OrderBy(space => space.R).ThenBy(space => space.Q).ToArray();
            var commands = tied.Select(space =>
                DirectorCommand.ChooseMovementDestination(movement.ResolutionId, movement.ChoiceId, space)).ToArray();
            return commands.Length == 1
                ? PveStep.Run(commands[0], Name(movement.MovingHeroId) + " moves")
                : PveStep.Ask(new PvePrompt(
                    PvePromptKind.EnemyMovementDestination,
                    movement.MovingHeroId,
                    Name(movement.MovingHeroId) + " has equally good spaces: choose where it moves.",
                    commands));
        }

        /// <summary>PVE-D07/D10. Attack a target of the first listed kind that is legal; ties go to the player.</summary>
        private PveStep AttackStep(IMatchReadModel match, PendingAttackTargetChoice attack)
        {
            var actorCard = match.Cards.FirstOrDefault(value => value.Id.Equals(match.SelectedActor.CardInstanceId));
            var orange = actorCard == null ? null : cards.Orange(actorCard.DefinitionReference.DefinitionId);
            var order = (orange?.Options ?? Array.Empty<PveCardOption>())
                .Where(option => option.Kind == PveOptionKind.Attack)
                .Select(option => option.Target)
                .Concat(new[] { PveTargetKind.Hero, PveTargetKind.Minion })
                .Distinct();
            foreach (var kind in order)
            {
                var commands = kind == PveTargetKind.Hero
                    ? attack.LegalTargetHeroIds.Select(hero =>
                        DirectorCommand.ChooseAttackTarget(attack.ResolutionId, attack.ChoiceId, hero)).ToArray()
                    : attack.LegalTargetMinionIds.Select(minion =>
                        DirectorCommand.ChooseAttackTarget(attack.ResolutionId, attack.ChoiceId, minion)).ToArray();
                if (commands.Length == 1)
                {
                    return PveStep.Run(commands[0], Name(attack.ChooserId) + " chooses its target");
                }

                if (commands.Length > 1)
                {
                    return PveStep.Ask(new PvePrompt(
                        PvePromptKind.EnemyAttackTarget,
                        attack.ChooserId,
                        Name(attack.ChooserId) + " can attack several targets: choose one.",
                        commands));
                }
            }

            return PveStep.AwaitingPlayer;
        }

        /// <summary>PVE-D09. The weakest purple card that defends successfully; otherwise decline.</summary>
        private PveStep DefenseStep(IMatchReadModel match, HeroId defender)
        {
            var preview = match.PendingDefensePreview;
            var sufficient = preview?.Choices
                .Where(choice => !choice.IsDecline && choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended)
                .OrderBy(choice => choice.DefenseTotal ?? int.MaxValue)
                .ThenBy(choice => choice.CardInstanceId.Value.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            return sufficient != null
                ? PveStep.Run(DirectorCommand.ChooseDefenseCard(sufficient.CardInstanceId.Value), Name(defender) + " defends")
                : PveStep.Run(DirectorCommand.DeclineDefense(), Name(defender) + " does not defend");
        }

        /// <summary>
        /// Any other decision an enemy owns (a card effect's choice, a prevention): its weakest purple
        /// card when a card is asked for (PVE-D19), otherwise the first command its seat may submit.
        /// </summary>
        private PveStep FallbackStep(IMatchReadModel match, HeroId enemy, Func<HeroId, IReadOnlyList<DirectorCommand>> authorizedFor)
        {
            var commands = authorizedFor?.Invoke(enemy) ?? Array.Empty<DirectorCommand>();
            if (commands.Count == 0) return PveStep.AwaitingPlayer;
            if (match.PendingEffectChoice?.NodeId.Equals(PveHookContent.SelectionNode) == true)
                return PveStep.Ask(new PvePrompt(PvePromptKind.EnemyClosestUnit, enemy,
                    Name(enemy) + " has equally close Blue units: choose one to hook.", commands));
            var purple = commands
                .Where(command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice != null &&
                    command.EffectChoice.Candidate.IsDefined &&
                    PurpleDefense(match, command.EffectChoice.Candidate) != null)
                .OrderBy(command => PurpleDefense(match, command.EffectChoice.Candidate).Value)
                .FirstOrDefault();
            if (purple != null) return PveStep.Run(purple, Name(enemy) + " gives up a purple card");
            var confirm = commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice != null &&
                command.EffectChoice.Kind == GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommandKind.Confirm);
            return PveStep.Run(confirm ?? commands[0], "PVE_FALLBACK " + Name(enemy));
        }

        private int? PurpleDefense(IMatchReadModel match, GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCandidate candidate)
        {
            if (candidate.Value.Kind != GuardsDigital.Core.Effects.EffectValueKind.CardInstance) return null;
            var card = match.Cards.FirstOrDefault(value => value.Id.Equals(candidate.Value.CardInstanceId));
            return card == null ? null : cards.Purple(card.DefinitionReference.DefinitionId)?.Defense;
        }

        private IReadOnlyList<HexCoordinate> Targets(IMatchReadModel match, HeroId actor, PveTargetKind kind)
        {
            var side = match.Participants.GetTeamForHero(actor).Side;
            var heroes = match.Heroes.Where(hero => match.Participants.GetTeamForHero(hero.HeroId).Side != side)
                .Select(hero => hero.Coordinate);
            var minions = match.Minions.Where(minion => minion.TeamSide != side).Select(minion => minion.Coordinate);
            switch (kind)
            {
                case PveTargetKind.Hero: return heroes.ToArray();
                case PveTargetKind.Minion: return minions.ToArray();
                case PveTargetKind.Unit: return heroes.Concat(minions).Distinct().ToArray();
                default: return Array.Empty<HexCoordinate>();
            }
        }

        private static ActionOffer? Offer(IMatchReadModel match, CardInstanceId card, ActionDefinitionId action)
        {
            foreach (var offer in match.AvailableTurnActions)
            {
                if (offer.SourceCardInstanceId.Equals(card) && offer.ActionDefinitionId.Equals(action)) return offer;
            }

            return null;
        }

        private static int Cost(PendingMovementChoice movement, HexCoordinate space) =>
            movement.LegalDestinations.Where(destination => destination.Destination.Equals(space))
                .Select(destination => destination.Cost).DefaultIfEmpty(int.MaxValue).Min();

        private static IReadOnlyList<CardInstance> HandOf(IMatchReadModel match, HeroId hero) =>
            match.Cards.Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();

        private static HeroId OwnerOf(IMatchReadModel match, CardInstanceId card) =>
            match.Cards.FirstOrDefault(value => value.Id.Equals(card))?.OwnerId ?? default;

        private static bool IsOnBoard(IMatchReadModel match, HeroId hero) =>
            match.Heroes.Any(value => value.HeroId.Equals(hero));

        private static HexCoordinate? PositionOf(IMatchReadModel match, HeroId hero)
        {
            foreach (var value in match.Heroes)
            {
                if (value.HeroId.Equals(hero)) return value.Coordinate;
            }

            return null;
        }

        private static bool IsEmpty(IMatchReadModel match, HexCoordinate space) =>
            !match.Heroes.Any(value => value.Coordinate.Equals(space)) &&
            !match.Minions.Any(value => value.Coordinate.Equals(space)) &&
            !match.Obstacles.Any(value => value.Coordinate.Equals(space));

        private string Name(HeroId hero) =>
            enemies.TryGetValue(hero, out var enemy) ? enemy.DisplayName : hero.Value;
    }
}

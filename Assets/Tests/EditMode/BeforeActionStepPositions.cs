using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.9. Every dealt face whose action carries a step taken before the action proper,
    /// read off the content rather than listed, and the positions each one is asked about.
    ///
    /// <para><b>Which faces.</b> Three declarations put a step in front of an action, and each is
    /// read where the content states it:</para>
    /// <list type="bullet">
    /// <item>An Attack whose own "Before the attack:" text exists (<c>CardTextTiming.BeforeTheAttack</c>)
    /// - a move, a bind, a line behind, a token placed first.</item>
    /// <item>A Skill whose effect relocates a piece (<c>direct-move@3</c>, <c>swap-with-actor@1</c>) and
    /// then does something further - the pre-action move of a Skill.</item>
    /// <item>A p.16 ability that runs before its owner's action (<c>UltimateRuleDefinition.BeforeAction</c>),
    /// asked of every printed Attack and Skill of that owner it fires before - the p.10 hold-then-act
    /// shape, where the action is held while the step is taken.</item>
    /// </list>
    ///
    /// <para><b>Which positions.</b> The actor stands in open ground; one enemy stands at a named
    /// distance and direction, every other unit is far away. "Beyond reach" positions put the enemy
    /// where the printed targeting alone reaches nobody, so only the step can bring a target within
    /// reach. The walled position rings the actor with obstacles so the step itself cannot be taken.
    /// Nothing here is chosen per card: the geometry is the same for every face, and only the
    /// printed range (for a Ranged face) and the printed unit kind (a minion-only face is offered a
    /// minion) are read from the face to say what "beyond reach" means.</para>
    /// </summary>
    internal static class BeforeActionStepPositions
    {
        internal enum StepShape
        {
            BeforeTheAttackText,
            SkillPreStep,
            BeforeActionAbility
        }

        internal sealed class SteppedFace
        {
            internal SteppedFace(
                string hero, CardDefinition card, ActionDefinition action, StepShape shape,
                CardDefinition ability)
            {
                Hero = hero;
                Card = card;
                Action = action;
                Shape = shape;
                Ability = ability;
            }

            internal string Hero { get; }
            internal CardDefinition Card { get; }
            internal ActionDefinition Action { get; }
            internal StepShape Shape { get; }

            /// <summary>The p.16 card whose before-action ability stands, for that shape alone.</summary>
            internal CardDefinition Ability { get; }

            internal string Name => Ability == null
                ? $"{Card.Id.Value} [{Shape}]"
                : $"{Card.Id.Value} under {Ability.Id.Value} [{Shape}]";

            internal bool TargetsMinionsOnly =>
                Action.AttackTargetPolicy?.TargetableUnits == AttackTargetableUnits.MinionsOnly;

            internal int PrintedRange =>
                Action.Kind == ActionKind.Attack && Action.AttackRange == AttackRangeKind.Ranged
                    ? Math.Max(1, Action.BaseValues
                        .Where(value => value.Channel == PrintedValueChannel.Range)
                        .Select(value => value.Amount)
                        .DefaultIfEmpty(1)
                        .Max())
                    : 1;
        }

        internal sealed class StepPosition
        {
            internal StepPosition(string name, int distance, bool inLine, bool walled, bool beyondReach)
            {
                Name = name;
                Distance = distance;
                InLine = inLine;
                Walled = walled;
                BeyondReach = beyondReach;
            }

            internal string Name { get; }
            internal int Distance { get; }
            internal bool InLine { get; }
            internal bool Walled { get; }

            /// <summary>No target stands within the face's printed reach without the step.</summary>
            internal bool BeyondReach { get; }
        }

        /// <summary>The staged turn: the actor selected with the face, nothing standing.</summary>
        internal sealed class StagedStep
        {
            internal StagedStep(
                MatchSessionInitialState initial, MatchSession session, HeroId actor, ActionOffer offer)
            {
                Initial = initial;
                Session = session;
                Actor = actor;
                Offer = offer;
            }

            internal MatchSessionInitialState Initial { get; }
            internal MatchSession Session { get; }
            internal HeroId Actor { get; }
            internal ActionOffer Offer { get; }
        }

        private static readonly Lazy<BoardMapDefinition> Map = new Lazy<BoardMapDefinition>(() =>
            OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap));

        private static readonly Lazy<IReadOnlyList<SteppedFace>> Derived =
            new Lazy<IReadOnlyList<SteppedFace>>(Derive);

        internal static IReadOnlyList<SteppedFace> Faces => Derived.Value;

        /// <summary>The positions every face is asked about, derived from what it prints.</summary>
        internal static IReadOnlyList<StepPosition> PositionsFor(SteppedFace face)
        {
            var reach = face.PrintedRange;
            var positions = new List<StepPosition>
            {
                new StepPosition("target within printed reach, adjacent", 1, true, false, false),
                new StepPosition($"target {reach + 1} away in a straight line, beyond printed reach",
                    reach + 1, true, false, true),
                new StepPosition($"target {reach + 1} away off every straight line, beyond printed reach",
                    reach + 1, false, false, true),
                new StepPosition($"target {reach + 2} away in a straight line, beyond printed reach",
                    reach + 2, true, false, true),
                new StepPosition($"walled in on every side so no step can be taken, target {reach + 1} away",
                    reach + 1, true, true, true)
            };
            return positions;
        }

        private static IReadOnlyList<SteppedFace> Derive()
        {
            var faces = new List<SteppedFace>();
            foreach (var hero in QuickGameSetupConfiguration.DealtHeroes)
            {
                var configuration = Configure(hero, out var lineup);
                var heroId = HeroIdOf(configuration, lineup, hero);
                var owned = configuration.CardInstances
                    .Where(card => card.OwnerId.Equals(heroId))
                    .Select(card => configuration.CardDefinitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)))
                    .GroupBy(definition => definition.Reference)
                    .Select(group => group.First())
                    .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal)
                    .ToArray();

                foreach (var card in owned)
                {
                    foreach (var action in card.ActionDefinitions.Where(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary))
                    {
                        if (action.Kind == ActionKind.Attack &&
                            action.CardTextAt(CardTextTiming.BeforeTheAttack) != null)
                        {
                            faces.Add(new SteppedFace(
                                hero.ContentId.Value, card, action, StepShape.BeforeTheAttackText, null));
                        }
                        else if (action.Kind == ActionKind.Skill && action.EffectRoot != null &&
                            RelocatesBeforeAFurtherStep(action.EffectRoot))
                        {
                            faces.Add(new SteppedFace(
                                hero.ContentId.Value, card, action, StepShape.SkillPreStep, null));
                        }
                    }
                }

                foreach (var ability in owned.Where(card =>
                    card.UltimateRule?.BeforeAction.IsDeclared == true))
                {
                    foreach (var card in owned.Where(card => !card.Progression.IsUltimate))
                    {
                        foreach (var action in card.ActionDefinitions.Where(action =>
                            action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                            (action.Kind == ActionKind.Attack || action.Kind == ActionKind.Skill) &&
                            ability.UltimateRule.BeforeAction.FiresBefore(action.Kind)))
                        {
                            faces.Add(new SteppedFace(
                                hero.ContentId.Value, card, action, StepShape.BeforeActionAbility, ability));
                        }
                    }
                }
            }

            return faces;
        }

        /// <summary>
        /// Whether the effect, in printed order, relocates a piece and then has a further step - the
        /// shape in which the later step can be reached only because of the earlier move.
        /// </summary>
        private static bool RelocatesBeforeAFurtherStep(EffectDefinition effect)
        {
            var ordered = new List<EffectNodeDefinition>();
            Flatten(effect, effect.RootNodeId, ordered, new HashSet<EffectNodeId>());
            var firstMove = ordered.FindIndex(node =>
                node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3) ||
                node.Capability.Equals(SyntheticExchangeCapabilities.SwapWithActorV1));
            return firstMove >= 0 && firstMove < ordered.Count - 1;
        }

        private static void Flatten(
            EffectDefinition effect, EffectNodeId id, List<EffectNodeDefinition> ordered,
            HashSet<EffectNodeId> seen)
        {
            if (!seen.Add(id) || !effect.TryGetNode(id, out var node))
            {
                return;
            }

            if (node.Kind == EffectNodeKind.Capability)
            {
                ordered.Add(node);
            }

            foreach (var child in node.ChildNodeIds)
            {
                Flatten(effect, child, ordered, seen);
            }
        }

        private static MatchConfiguration Configure(RegisteredHero hero, out HeroLineup lineup)
        {
            var others = QuickGameSetupConfiguration.DealtHeroes
                .Where(candidate => !candidate.ContentId.Equals(hero.ContentId))
                .Take(5)
                .ToArray();
            lineup = new HeroLineup(new[] { hero, others[0], others[1] }, others.Skip(2).Take(3));
            return MatchConfiguration.CreateForLineupForInternalTest(
                MatchFormat.ThreeVersusThreeQuick, lineup, TeamSide.Red);
        }

        private static HeroId HeroIdOf(MatchConfiguration configuration, HeroLineup lineup, RegisteredHero hero)
        {
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, lineup),
                configuration.Participants, Map.Value, configuration.TieBreakerState);
            return setup.Heroes.Single(seat => seat.ContentId.Equals(hero.ContentId)).HeroId;
        }

        /// <summary>
        /// The open ground every position is built on: the playable space nearest the centre whose
        /// every space within four is playable, so every direction and distance used is free.
        /// </summary>
        internal static HexCoordinate Centre
        {
            get
            {
                var playable = new HashSet<HexCoordinate>(Map.Value.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                    .Select(space => space.Coordinate));
                return playable
                    .Where(candidate => playable.Count(space =>
                        HexDistanceQuery.Calculate(space, candidate) <= 2) == 19)
                    .OrderByDescending(candidate => playable.Count(space =>
                        HexDistanceQuery.Calculate(space, candidate) <= 5))
                    .ThenBy(candidate => HexDistanceQuery.Calculate(candidate, new HexCoordinate(0, 0)))
                    .ThenBy(candidate => candidate.Q)
                    .ThenBy(candidate => candidate.R)
                    .First();
            }
        }

        /// <summary>
        /// The match at the actor's turn with the face's card selected, or <c>null</c> with the
        /// reason where the content cannot be staged this way.
        /// </summary>
        internal static StagedStep Stage(
            SteppedFace face, StepPosition position, out string unstaged,
            HexCoordinate? actorAt = null, bool withoutTheAbility = false)
        {
            unstaged = null;
            var map = Map.Value;
            var hero = QuickGameSetupConfiguration.DealtHeroes.Single(
                candidate => candidate.ContentId.Value == face.Hero);
            var configuration = Configure(hero, out var lineup);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(MatchFormat.ThreeVersusThreeQuick, lineup),
                configuration.Participants, map, configuration.TieBreakerState);
            var actor = setup.Heroes.Single(seat => seat.ContentId.Equals(hero.ContentId)).HeroId;
            var enemy = setup.Heroes.First(seat => seat.TeamSide == TeamSide.Blue).HeroId;

            var card = configuration.CardInstances.Single(instance =>
                instance.OwnerId.Equals(actor) &&
                instance.DefinitionReference.Equals(face.Card.Reference));
            if (card.State.Location != CardLocation.Hand)
            {
                if (card.State.Location != CardLocation.Deck)
                {
                    unstaged = $"{face.Card.Id.Value} starts in {card.State.Location}, not in hand or deck";
                    return null;
                }

                var yielded = configuration.CardInstances
                    .Where(instance => instance.OwnerId.Equals(actor) &&
                        instance.State.Location == CardLocation.Hand)
                    .OrderBy(instance => instance.Id.Value, StringComparer.Ordinal)
                    .First();
                Assert.That(CardLifecycleAuthority.TryReturnToDeck(yielded), Is.True);
                Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(card), Is.True);
            }

            if (face.Ability != null && !withoutTheAbility)
            {
                var ability = configuration.CardInstances.Single(instance =>
                    instance.OwnerId.Equals(actor) &&
                    instance.DefinitionReference.Equals(face.Ability.Reference));
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ability), Is.True,
                    $"p.16 unlocks {face.Ability.Id.Value} by moving it out of the deck.");
            }

            var centre = Centre;
            if (!TryPlaceTarget(map, centre, position, out var target))
            {
                unstaged = $"no open line from the centre for {position.Name}";
                return null;
            }

            Assert.That(HexDistanceQuery.Calculate(centre, target), Is.EqualTo(position.Distance));
            var far = FarSpaces(map, centre);
            var heroes = new Dictionary<HeroId, HexCoordinate>();
            var farIndex = 0;
            foreach (var seat in setup.Heroes)
            {
                heroes[seat.HeroId] = seat.HeroId.Equals(actor)
                    ? actorAt ?? centre
                    : seat.HeroId.Equals(enemy) && !face.TargetsMinionsOnly
                        ? target
                        : far[farIndex++];
            }

            var minions = new List<QuickGameMinionSetupState>();
            if (face.TargetsMinionsOnly)
            {
                var kept = setup.Minions.First(minion => minion.TeamSide == TeamSide.Blue);
                minions.Add(new QuickGameMinionSetupState(
                    kept.MinionId, kept.TeamId, kept.TeamSide, kept.MinionType, kept.SpawnPoint,
                    SpaceAt(map, target), target));
            }

            var staged = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams,
                setup.Heroes.Select(seat => new QuickGameHeroSetupState(
                    seat.HeroId, seat.ContentId, seat.DisplayName, seat.TeamId, seat.TeamSide,
                    seat.SpawnPoint, SpaceAt(map, heroes[seat.HeroId]), heroes[seat.HeroId])),
                minions);
            var obstacles = position.Walled
                ? centre.GetNeighbors()
                    .Select((space, index) => new ObstacleOccupancy(
                        new ObstacleId($"step-wall-{index}"), space))
                    .ToArray()
                : Array.Empty<ObstacleOccupancy>();
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                staged.Heroes.Select(seat => new HeroOccupancy(seat.HeroId, seat.Coordinate)),
                obstacles);
            var initial = new MatchSessionInitialState(scenario, staged, new[] { map });
            var session = new MatchSession(initial);

            if (!ReachTheActorsTurn(session, actor, card.Id, out unstaged))
            {
                return null;
            }

            var offer = session.Projection.AvailableTurnActions.SingleOrDefault(candidate =>
                candidate.SourceCardInstanceId.Equals(card.Id) &&
                candidate.ActionDefinitionId.Equals(face.Action.Id));
            if (!offer.ActionDefinitionId.IsDefined)
            {
                unstaged = $"{face.Card.Id.Value}'s {face.Action.Id.Value} is not among the offers at all";
                return null;
            }

            return new StagedStep(initial, session, actor, offer);
        }

        /// <summary>
        /// Commits every seat - the actor its face, everyone else their lowest-Initiative card - and
        /// lets every earlier turn Hold (p.12), so nothing on the board moves before the actor acts.
        /// </summary>
        private static bool ReachTheActorsTurn(
            MatchSession session, HeroId actor, CardInstanceId card, out string unstaged)
        {
            unstaged = null;
            for (var step = 0; step < 200; step++)
            {
                var projection = session.Projection;
                if (projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(actor) &&
                    projection.DecisionContract == null && projection.PendingTurnRelease == null)
                {
                    if (!projection.SelectedActor.CardInstanceId.Equals(card))
                    {
                        unstaged = "the actor's selected card is not the staged face";
                        return false;
                    }

                    return true;
                }

                DirectorCommand next;
                if (projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
                    projection.DecisionContract == null)
                {
                    var asked = projection.Commitments.First(value =>
                        value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                    var chosen = asked.HeroId.Equals(actor)
                        ? card
                        : projection.Cards
                            .Where(value => value.OwnerId.Equals(asked.HeroId) &&
                                value.State.Location == CardLocation.Hand)
                            .OrderBy(value => projection.Definitions
                                .Single(definition => definition.Reference.Equals(value.DefinitionReference))
                                .Initiative.Value)
                            .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                            .First().Id;
                    next = DirectorCommand.CommitCard(asked.HeroId, chosen);
                }
                else if (projection.PendingTurnRelease != null)
                {
                    next = DirectorCommand.ReleaseTurn();
                }
                else if (projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                    !projection.SelectedActor.ActorId.Equals(actor) &&
                    projection.DecisionContract == null &&
                    projection.AvailableHoldOffer.HasValue && projection.AvailableHoldOffer.Value.IsAvailable)
                {
                    next = DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId);
                }
                else
                {
                    var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                    if (legal.Count == 0)
                    {
                        unstaged = $"no legal command before the actor's turn ({projection.Phase})";
                        return false;
                    }

                    next = legal[0];
                }

                var result = session.Execute(next);
                if (!result.Accepted)
                {
                    unstaged = $"{next.Kind} refused before the actor's turn: {result.Detail}";
                    return false;
                }
            }

            unstaged = "the actor's turn was not reached";
            return false;
        }

        /// <summary>
        /// The target's space: the first direction, in the fixed order, whose whole walk out to the
        /// named distance is playable (and, off the line, whose turned last step is too).
        /// </summary>
        private static bool TryPlaceTarget(
            BoardMapDefinition map, HexCoordinate centre, StepPosition position, out HexCoordinate target)
        {
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));
            for (var index = 0; index < 6; index++)
            {
                var direction = (HexDirection)index;
                var path = Enumerable.Range(1, position.Distance)
                    .Select(steps => Walk(centre, direction, steps))
                    .ToList();
                target = path[path.Count - 1];
                if (!position.InLine)
                {
                    // One step short along the line, then one step turned: the same distance, on no
                    // straight line from the actor.
                    target = Walk(path[path.Count - 2], (HexDirection)((index + 1) % 6), 1);
                    path[path.Count - 1] = target;
                }

                if (path.All(playable.Contains) &&
                    HexDistanceQuery.Calculate(centre, target) == position.Distance)
                {
                    return true;
                }
            }

            target = default;
            return false;
        }

        private static HexCoordinate Walk(HexCoordinate from, HexDirection direction, int steps)
        {
            var at = from;
            for (var step = 0; step < steps; step++)
            {
                at = at.GetNeighbor(direction);
            }

            return at;
        }

        /// <summary>Playable spaces at least eight from the centre and two from each other.</summary>
        private static IReadOnlyList<HexCoordinate> FarSpaces(BoardMapDefinition map, HexCoordinate centre)
        {
            var chosen = new List<HexCoordinate>();
            foreach (var space in map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable &&
                    HexDistanceQuery.Calculate(space.Coordinate, centre) >= 8)
                .Select(space => space.Coordinate)
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R))
            {
                if (chosen.All(other => HexDistanceQuery.Calculate(other, space) >= 3))
                {
                    chosen.Add(space);
                }

                if (chosen.Count == 6)
                {
                    break;
                }
            }

            return chosen;
        }

        private static ConfiguredSpaceReference SpaceAt(BoardMapDefinition map, HexCoordinate coordinate) =>
            new ConfiguredSpaceReference(
                map.Id, map.Spaces.Single(space => space.Coordinate.Equals(coordinate)).Id);
    }
}

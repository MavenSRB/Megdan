using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8c. One configured round on a radius-5 board with one of Mortimer's faces committed
    /// and selected to act - Wuk's round (<see cref="WukRound"/>) with Zombies for Trees, shared by
    /// every Mortimer block from here on. Every enemy commits a one-Movement hold and keeps a spare
    /// card in hand; the enemy also holds a Defense 9 wall.
    /// </summary>
    internal sealed class MortimerRound
    {
        internal static readonly HeroId Mortimer = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        internal static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        internal static readonly HeroId Ally = new HeroId("mortimer-red-ally");
        private static readonly MapId Map = new MapId("mortimer-round");

        internal static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        internal static MinionId EnemyMinion(int index) => new MinionId($"mortimer-blue-melee-{index}");
        internal static MinionId FriendlyMinion(int index) => new MinionId($"mortimer-red-melee-{index}");

        private readonly CardDefinition[] definitions;
        private readonly CardInstance source;
        private readonly CardInstance[] instances;
        private readonly MatchContentCatalogs catalogs;
        private readonly AdjacentMoveAuthority board;
        private readonly RoundResolutionAuthority round;
        private readonly CardActionResolutionAuthority actions;
        private readonly QuickGameMinionSetupState[] minions;

        internal MortimerRound(
            CardDefinition face,
            HexCoordinate? mortimerAt = null,
            HexCoordinate? enemyAt = null,
            HexCoordinate? enemyAllyAt = null,
            HexCoordinate? allyAt = null,
            int mortimerDiscards = 0,
            int allyDiscards = 0,
            bool withUltimate = false,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            IReadOnlyList<HexCoordinate> friendlyMinionsAt = null,
            IReadOnlyList<HexCoordinate> zombiesAt = null,
            IReadOnlyList<HexCoordinate> enemyRocksAt = null,
            bool battleZone = false,
            IReadOnlyList<(HeroId Hero, TeamSide Side, RuleModification Modification)> standingEffects = null,
            IReadOnlyList<(HeroId Hero, TeamSide Side, ActiveEffectDefinition Definition)> standingDefinitions = null)
        {
            var participants = Participants(enemyAllyAt.HasValue, allyAt.HasValue);
            var spaces = (from q in Enumerable.Range(-5, 11)
                          from r in Enumerable.Range(-5, 11)
                          where Math.Abs(q + r) <= 5
                          select new HexCoordinate(q, r)).ToArray();
            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(Mortimer, mortimerAt ?? H(0, 0)),
                new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5))
            };
            if (enemyAllyAt.HasValue) heroes.Add(new HeroOccupancy(EnemyAlly, enemyAllyAt.Value));
            if (allyAt.HasValue) heroes.Add(new HeroOccupancy(Ally, allyAt.Value));

            var placed = new List<QuickGameMinionSetupState>();
            var index = 0;
            foreach (var at in enemyMinionsAt ?? Array.Empty<HexCoordinate>())
            {
                placed.Add(Minion(EnemyMinion(index++), StagedBoardParticipants.BlueTeamIdValue,
                    TeamSide.Blue, at));
            }

            index = 0;
            foreach (var at in friendlyMinionsAt ?? Array.Empty<HexCoordinate>())
            {
                placed.Add(Minion(FriendlyMinion(index++), StagedBoardParticipants.RedTeamIdValue,
                    TeamSide.Red, at));
            }

            minions = placed.ToArray();
            board = new AdjacentMoveAuthority(
                battleZone ? BattleZoneMap(spaces) : BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                Array.Empty<ObstacleOccupancy>(), minions);
            foreach (var at in zombiesAt ?? Array.Empty<HexCoordinate>())
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId($"mortimer-standing-zombie-{at.Q}-{at.R}"),
                    MortimerComponentContent.Zombie, Mortimer, Mortimer,
                    new CardInstanceId("mortimer-earlier-card"), at));
            }

            foreach (var at in enemyRocksAt ?? Array.Empty<HexCoordinate>())
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId($"enemy-rock-{at.Q}-{at.R}"),
                    MrakComponentContent.Rock, Enemy, Enemy, new CardInstanceId("enemy-earlier-card"), at));
            }

            source = CardInstance.CreateInHand(new CardInstanceId("mortimer-source"), face.Reference, Mortimer);
            var hold = CardInstance.CreateInHand(new CardInstanceId("mortimer-enemy-hold"),
                EnemyHold.Reference, Enemy);
            var wall = CardInstance.CreateInHand(new CardInstanceId("mortimer-enemy-wall"),
                EnemyWall.Reference, Enemy);
            var discardable = CardInstance.CreateInHand(new CardInstanceId("mortimer-enemy-spare"),
                EnemyWall.Reference, Enemy);
            definitions = withUltimate
                ? new[] { face, EnemyHold, EnemyWall, MortimerCardContent.MasterOfPuppetsDefinition }
                : new[] { face, EnemyHold, EnemyWall };
            var all = new List<CardInstance> { source, hold, wall, discardable };
            if (withUltimate)
            {
                var ultimate = CardInstance.CreateInDeck(new CardInstanceId("mortimer-ultimate-card"),
                    MortimerCardContent.MasterOfPuppetsDefinition.Reference, Mortimer);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                all.Add(ultimate);
            }
            var holds = new List<(HeroId Hero, CardInstance Card)> { (Enemy, hold) };
            foreach (var other in heroes.Select(value => value.HeroId)
                         .Where(hero => !hero.Equals(Mortimer) && !hero.Equals(Enemy)))
            {
                var otherHold = CardInstance.CreateInHand(
                    new CardInstanceId($"mortimer-hold-{other.Value}"), EnemyHold.Reference, other);
                all.Add(otherHold);
                all.Add(CardInstance.CreateInHand(
                    new CardInstanceId($"mortimer-spare-{other.Value}"), EnemyWall.Reference, other));
                holds.Add((other, otherHold));
            }

            for (var discard = 0; discard < mortimerDiscards; discard++)
            {
                all.Add(new CardInstance(new CardInstanceId($"mortimer-discarded-{discard}"),
                    EnemyWall.Reference, Mortimer, CardStateFacts.DiscardedUnplayed()));
            }

            for (var discard = 0; discard < allyDiscards; discard++)
            {
                all.Add(new CardInstance(new CardInstanceId($"mortimer-ally-discarded-{discard}"),
                    EnemyWall.Reference, Ally, CardStateFacts.DiscardedUnplayed()));
            }

            instances = all.ToArray();
            catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(MortimerComponentContent.Handlers()
                    .Concat((standingDefinitions ?? Array.Empty<(HeroId, TeamSide, ActiveEffectDefinition)>())
                        .Select((standing, at) => (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                            new EffectCapabilityReference(new EffectCapabilityId("mortimer-test-standing-" + at), 1),
                            standing.Item3)))),
                new ComponentDefinitionCatalog(
                    MortimerComponentContent.Tokens().Concat(MrakComponentContent.Tokens()),
                    Array.Empty<MarkerDefinition>()),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            // Milestone 8.114b1. Effects already standing when the round's first action starts - an
            // immunity or a Movement raise - built as the match builds one, active from that start.
            var timing = new ActiveEffectLifecycle();
            var standing = 0;
            foreach (var (hero, side, modification) in
                     standingEffects ?? Array.Empty<(HeroId, TeamSide, RuleModification)>())
            {
                var id = "mortimer-standing-" + standing++;
                timing.Create(new ActiveEffectId(id),
                    new ActiveEffectDefinition(id + "-effect", ActiveEffectAffectedScopeKind.ControllerHero,
                        ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted,
                        TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd,
                        new[] { ActiveEffectCancellationCondition.CardsRetrieved }, false,
                        "MortimerStanding", id, Array.Empty<TimingCheckpoint>(), new[] { modification }),
                    hero, side, default, new CardInstanceId(id + "-card"),
                    new ActionDefinitionId(id + "-action"), 1, 1);
            }

            // Milestone 8.118. A whole standing definition - a guard over minions in a disk - built the
            // same way, for effects whose scope is not the controller alone.
            foreach (var (hero, side, definition) in
                     standingDefinitions ?? Array.Empty<(HeroId, TeamSide, ActiveEffectDefinition)>())
            {
                var id = "mortimer-standing-" + standing++;
                timing.Create(new ActiveEffectId(id), definition, hero, side, default,
                    new CardInstanceId(id + "-card"), new ActionDefinitionId(id + "-action"), 1, 1);
            }

            var markers = new MarkerAttachmentRegister();
            actions = new CardActionResolutionAuthority(catalogs, board, markers);
            round = new RoundResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                new RoundResolutionComposition(actions, board, timing,
                    markers, null, null, null, null, null, null,
                    startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
            Assert.That(round.TryCommitCard(Mortimer, source.Id).Accepted, Is.True);
            foreach (var (hero, card) in holds)
            {
                Assert.That(round.TryCommitCard(hero, card.Id).Accepted, Is.True, hero.Value + " commits");
            }

            Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Mortimer));
        }

        /// <summary>
        /// Milestone 8.114b1. The same board with every space in one starting Battle Zone, so p.8's
        /// Minion Battle counts every minion placed.
        /// </summary>
        private static BoardMapDefinition BattleZoneMap(IEnumerable<HexCoordinate> spaces)
        {
            var zone = new BoardMapZone(new ZoneId("mortimer-battle-zone"), "Mortimer Battle Zone", "lane", null, false, true);
            return new BoardMapDefinition(Map, 1, Map.Value,
                spaces.Select(at => new BoardMapSpace(
                    new SpaceId($"q{at.Q}_r{at.R}"), at, BoardMapSpaceKind.Playable, zone.Id)),
                new[] { zone }, Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        internal AdjacentMoveAuthority Board => board;

        internal RoundResolutionAuthority Round => round;

        /// <summary>The question an end-of-round trigger stopped on, or null.</summary>
        internal PendingEffectChoice EndOfRoundChoice => actions.PendingEffectChoice;

        /// <summary>
        /// Milestone 8.114b1. Ends the turn Mortimer just acted in, holds every later actor of turn one,
        /// passes turns two to four, and offers p.8's Minion Battle stage once - which stops on a
        /// pre-battle trigger's question when there is one.
        /// </summary>
        internal RoundResolutionResult RunToMinionBattle()
        {
            var heroes = instances.Select(card => card.OwnerId).Distinct().ToArray();
            for (var guard = 0; round.Phase != RoundResolutionPhase.EndOfRound && guard < 64; guard++)
            {
                if (round.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    if (round.CurrentTurnAuthority.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    Assert.That(round.TryPerformHold(
                        round.CurrentTurnAuthority.SelectedActor.CardInstanceId).Accepted, Is.True);
                    continue;
                }

                // p.7: a hero with a card to commit commits one; a hero with none passes.
                foreach (var hero in heroes)
                {
                    if (round.Phase != RoundResolutionPhase.AwaitingCommitments) break;
                    if (round.TryPass(hero).Accepted) continue;
                    foreach (var card in instances.Where(card =>
                                 card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand))
                    {
                        if (round.TryCommitCard(hero, card.Id).Accepted) break;
                    }
                }
            }

            Assert.That(round.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(round.CurrentEndOfRoundStage, Is.EqualTo(EndOfRoundStage.MinionBattle));
            return round.TryAdvanceEndOfRound();
        }

        /// <summary>Answers the pre-battle question with exactly these minions (none is an answer).</summary>
        internal void AnswerEndOfRound(params MinionId[] chosen) => AnswerEndOfRound(round, actions, chosen);

        internal static void AnswerEndOfRound(
            RoundResolutionAuthority target, CardActionResolutionAuthority targetActions, MinionId[] chosen)
        {
            var asked = targetActions.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, "a pre-battle trigger is asking");
            foreach (var minion in chosen)
            {
                var open = targetActions.PendingEffectChoice;
                Assert.That(target.TryAnswerEndOfRoundTriggerChoice(open.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                    invocation.Candidate.Value.MinionId.Equals(minion))).Accepted, Is.True);
            }

            var still = targetActions.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(target.TryAnswerEndOfRoundTriggerChoice(still.ConfirmInvocation).Accepted, Is.True);
            }
        }

        /// <summary>Snapshot bytes of the live match, and a match restored from them.</summary>
        internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(round, board);

        internal RestoredAuthoritativeMatch RestoreFrom(string bytes) => Restore(bytes);

        /// <summary>Red: Mortimer and, when present, his friendly ally; Blue: the enemy and their ally.</summary>
        private static MatchParticipants Participants(bool withEnemyAlly, bool withAlly)
        {
            var red = new TeamId(StagedBoardParticipants.RedTeamIdValue);
            var blue = new TeamId(StagedBoardParticipants.BlueTeamIdValue);
            var players = new List<Player>
            {
                new Player(new PlayerId(StagedBoardParticipants.RedPlayerIdValue), red),
                new Player(new PlayerId(StagedBoardParticipants.BluePlayerIdValue), blue)
            };
            var heroes = new List<Hero>
            {
                new Hero(Mortimer, new PlayerId(StagedBoardParticipants.RedPlayerIdValue), HeroDashboard.RulebookLayout),
                new Hero(Enemy, new PlayerId(StagedBoardParticipants.BluePlayerIdValue), HeroDashboard.RulebookLayout)
            };
            if (withEnemyAlly)
            {
                var player = new PlayerId(StagedBoardParticipants.BlueAllyPlayerIdValue);
                players.Add(new Player(player, blue));
                heroes.Add(new Hero(EnemyAlly, player, HeroDashboard.RulebookLayout));
            }

            if (withAlly)
            {
                var player = new PlayerId("mortimer-red-ally-player");
                players.Add(new Player(player, red));
                heroes.Add(new Hero(Ally, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(red, TeamSide.Red), new Team(blue, TeamSide.Blue) }, players, heroes);
        }

        /// <summary>Where each of a hero's cards is now.</summary>
        internal CardLocation[] LocationsOf(HeroId owner) =>
            instances.Where(card => card.OwnerId.Equals(owner))
                .Select(card => card.State.Location).ToArray();

        private static QuickGameMinionSetupState Minion(
            MinionId id, string team, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                id, new TeamId(team), side, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId($"spawn-{id.Value}")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        internal static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("mortimer-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        internal static readonly CardDefinition EnemyWall = new CardDefinition(
            new CardDefinitionId("mortimer-enemy-wall"), "rev-1", "TEST - Wall",
            new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        internal TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

        internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

        internal int AttackTotal => Turn.PendingDefenseChoice.AttackTotal;

        internal bool ActionEnded =>
            Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
            Turn.PendingAttackTargetChoice == null;

        internal HexCoordinate[] Zombies() =>
            board.PlacedTokens
                .Where(token => token.Definition.Id.Equals(MortimerComponentContent.ZombieId))
                .Select(token => token.Coordinate).ToArray();

        internal bool Stands(MinionId minion) =>
            board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out _);

        internal HexCoordinate SpaceOf(HeroId hero)
        {
            Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out var at), Is.True);
            return at;
        }

        internal HexCoordinate SpaceOf(MinionId minion)
        {
            Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out var at), Is.True);
            return at;
        }

        internal HexCoordinate[] OfferedSpaces() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate).ToArray();

        internal HeroId[] OfferedHeroes() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                .Select(candidate => candidate.Value.HeroId).ToArray();

        internal MinionId[] OfferedMinions() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                .Select(candidate => candidate.Value.MinionId).ToArray();

        internal string[] AvailableBullets() =>
            Turn.PendingEffectChoice.SelectInvocations
                .Select(command => command.Candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        internal string[] UnavailableBullets() =>
            Turn.PendingEffectChoice.UnavailableCandidates
                .Select(candidate => candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        /// <summary>p.14's target question: the heroes the open Attack may name.</summary>
        internal HeroId[] LegalTargetHeroes() =>
            Turn.PendingAttackTargetChoice?.LegalTargetHeroIds.ToArray() ?? Array.Empty<HeroId>();

        /// <summary>p.14's target question: the minions the open Attack may name.</summary>
        internal MinionId[] LegalTargetMinions() =>
            Turn.PendingAttackTargetChoice?.LegalTargetMinionIds.ToArray() ?? Array.Empty<MinionId>();

        private ActionOffer Offer(ActionDefinitionId id) =>
            ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, Mortimer, source.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(id));

        internal bool IsOffered(ActionDefinitionId id) =>
            Turn.GetTurnActionOffers().Any(offer => offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

        internal void StartPrimaryAttack() =>
            Assert.That(round.TryStartAttack(new ActionResolutionId("mortimer-attack"),
                Offer(MortimerAttackFaces.PrimaryAttack)).Accepted, Is.True);

        internal void StartPrimarySkill() =>
            Assert.That(round.TryStartSkill(new ActionResolutionId("mortimer-skill"),
                Offer(MortimerCardContent.PrimarySkill)).Accepted, Is.True);

        /// <summary>The open Movement question's destinations, each with the route it walks.</summary>
        internal ReachableMovementDestination[] Destinations() =>
            Turn.PendingMovementChoice?.LegalDestinations.ToArray() ?? Array.Empty<ReachableMovementDestination>();

        internal void MoveTo(HexCoordinate space)
        {
            var choice = Turn.PendingMovementChoice;
            Assert.That(choice, Is.Not.Null, "a Movement question is open");
            Assert.That(round.TryAnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                choice.ResolutionId, choice.ChoiceId, space)).Accepted, Is.True);
        }

        internal void StopMoving()
        {
            var choice = Turn.PendingMovementChoice;
            Assert.That(choice, Is.Not.Null, "a Movement question is open");
            Assert.That(round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(
                choice.ResolutionId, choice.ChoiceId)).Accepted, Is.True);
        }

        /// <summary>Answers the open question with exactly these minions (none is an answer).</summary>
        internal void PickMinions(params MinionId[] chosen)
        {
            var asked = Turn.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null);
            foreach (var minion in chosen)
            {
                Answer(Turn.PendingEffectChoice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                    invocation.Candidate.Value.MinionId.Equals(minion)));
            }

            ConfirmIfStillOpen(asked);
        }

        /// <summary>Milestone 8.118. The standing removal-prevention question, as a player is shown it.</summary>
        internal RemovalPreventionPrompt Prevention =>
            Turn.DescribeRemovalPrevention(Turn.CurrentModifications());

        /// <summary>Milestone 8.118. Pays the first offered price, or declines it.</summary>
        internal void AnswerPrevention(bool pay)
        {
            var prompt = Prevention;
            Assert.That(prompt, Is.Not.Null, "a removal-prevention question is standing");
            Assert.That(round.TryAnswerRemovalPrevention(
                pay ? prompt.Payments.First().Invocation : prompt.DeclineInvocation).Accepted, Is.True);
        }

        internal bool OnBoard(HeroId hero) =>
            board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out _);

        /// <summary>Answers the open modal with the named bullets, then confirms if the window asks.</summary>
        internal void TakeBullets(params string[] optionNodes)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            foreach (var node in optionNodes)
            {
                var open = Turn.PendingEffectChoice;
                Answer(open.SelectInvocations.Single(command => command.Candidate.OptionNodeId.Value == node));
            }

            ConfirmIfStillOpen(choice);
        }

        internal void Target(HeroId hero)
        {
            var choice = Turn.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "p.14's target question is open");
            Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                choice.ResolutionId, choice.ChoiceId, hero)).Accepted, Is.True);
        }

        internal void Target(MinionId minion)
        {
            var choice = Turn.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "p.14's target question is open");
            Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                choice.ResolutionId, choice.ChoiceId, minion)).Accepted, Is.True);
        }

        internal void Defend() => Defend(round);

        internal void PickHero(HeroId hero)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero)));
            ConfirmIfStillOpen(choice);
        }

        internal void PickMinion(MinionId minion)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                invocation.Candidate.Value.MinionId.Equals(minion)));
            ConfirmIfStillOpen(choice);
        }

        internal void PickSpaces(params HexCoordinate[] spaces)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            foreach (var space in spaces)
            {
                var open = Turn.PendingEffectChoice;
                Answer(open.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Equals(EffectValue.Space(space))));
            }

            ConfirmIfStillOpen(choice);
        }

        internal CardInstanceId[] OfferedCards() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => candidate.Value.CardInstanceId).ToArray();

        /// <summary>The hero the open effect question is addressed to.</summary>
        internal HeroId Chooser => Turn.PendingEffectChoice.ChooserId;

        internal void PickCard(CardInstanceId card)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Equals(card)));
            ConfirmIfStillOpen(choice);
        }

        /// <summary>Takes an open "you may" step.</summary>
        internal void Accept()
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice?.ConfirmInvocation, Is.Not.Null, "an optional step can be taken");
            Answer(choice.ConfirmInvocation);
        }

        internal void Decline()
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an optional step can be declined");
            Answer(choice.DeclineInvocation);
        }

        private void ConfirmIfStillOpen(PendingEffectChoice asked)
        {
            var open = Turn.PendingEffectChoice;
            if (open != null && open.ChoiceId.Equals(asked.ChoiceId) && open.ConfirmInvocation != null)
            {
                Answer(open.ConfirmInvocation);
            }
        }

        private void Answer(EffectChoiceCommand command) =>
            Assert.That(round.TryAnswerEffectChoice(command).Accepted, Is.True);

        /// <summary>
        /// Snapshot at the open question, restore it, answer both the same way, and require the same
        /// next question and the same authoritative bytes - then once more from the retained prefix.
        /// </summary>
        internal void AssertRestoreReplays(params HexCoordinate[] answer)
        {
            var pending = Turn.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

            PickSpaces(answer);
            AnswerSpaces(restored.Round, answer);
            var live = AuthoritativeSnapshotSerializer.Serialize(round, board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(live), "the restored pause replays to the same authoritative bytes");

            var prefix = Restore(bytes);
            AnswerSpaces(prefix.Round, answer);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
        }

        /// <summary>Restores at an open card question and requires the same pick to converge.</summary>
        internal void AssertRestoreRepliesWithCard(CardInstanceId card)
        {
            var pending = Turn.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.ChooserId, Is.EqualTo(pending.ChooserId));
            PickCard(card);
            var command = resumed.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Equals(card));
            Assert.That(restored.Round.TryAnswerEffectChoice(command).Accepted, Is.True);
            var still = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(resumed.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(restored.Round.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
            }

            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        /// <summary>Restores at p.14's target question and requires the same answer to converge.</summary>
        internal void AssertTargetRestoreReplays(HeroId hero)
        {
            var pending = Turn.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.LegalTargetHeroIds, Is.EqualTo(pending.LegalTargetHeroIds));
            Assert.That(resumed.LegalTargetMinionIds, Is.EqualTo(pending.LegalTargetMinionIds));
            Target(hero);
            Assert.That(restored.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                resumed.ResolutionId, resumed.ChoiceId, hero)).Accepted, Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        internal void AssertDefenseRestoreReplays()
        {
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var pending = restored.Round.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.AttackTotal, Is.EqualTo(Turn.PendingDefenseChoice.AttackTotal));
            Defend(round);
            Defend(restored.Round);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        private static void Defend(RoundResolutionAuthority target)
        {
            var defense = target.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "p.14's Defense question is open");
            var wall = defense.LegalDefenseOffers.First(offer =>
                offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
            Assert.That(target.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted, Is.True);
        }

        private RestoredAuthoritativeMatch Restore(string bytes) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

        private static void AnswerSpaces(RoundResolutionAuthority target, HexCoordinate[] spaces)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            foreach (var space in spaces)
            {
                var open = target.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(target.TryAnswerEffectChoice(open.SelectInvocations.Single(
                    invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space)))).Accepted,
                    Is.True);
            }

            var still = target.CurrentTurnAuthority.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
            }
        }
    }
}

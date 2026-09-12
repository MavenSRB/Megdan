using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39d. The subject-adjacency clause Cutter's Blue line prints, proved as vocabulary
    /// before any of her content exists: "An enemy hero <b>adjacent to another enemy unit</b> and not
    /// adjacent to you discards a card, if able."
    ///
    /// <para><b>It is the sibling of Sabina's <c>adjacentToFriendlyMinion</c>, not a widening of it.</b>
    /// Her Close Support and Covering Fire print "An enemy hero in radius adjacent to YOUR FRIENDLY
    /// MINION": the same radius-hero selection narrowed by an adjacency measured from the ACTOR's team
    /// and restricted to p.19's minions. Cutter's measures from the SUBJECT's team and admits any unit.
    /// Two parameter differences on one clause, so a shared pair would have had to change Sabina's node
    /// inputs and move a shipped digest.</para>
    ///
    /// <para><b>"Another" does real work here where it did not for Sabina.</b> p.9 measures a disk
    /// inclusively, and the candidate is itself an enemy unit, so without an explicit self-exclusion
    /// every enemy hero on the board would qualify by standing next to itself.</para>
    /// </summary>
    [TestFixture]
    public sealed class SubjectAdjacencyNarrowingTests
    {
        private static readonly ActionDefinitionId BombardAction =
            new ActionDefinitionId("probe-bombard");
        private static readonly CardDefinitionId BombardCard =
            new CardDefinitionId("probe-bombard-card");
        private static readonly CardInstanceId BombardInstance =
            new CardInstanceId("probe-bombard-instance");

        private static readonly MapId Map = new MapId("subject-adjacency-board");
        private static readonly ZoneId Battle = new ZoneId("subject-adjacency-zone");
        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        private static readonly HeroId Actor = new HeroId("subject-adjacency-actor");
        private static readonly HeroId Lonely = new HeroId("subject-adjacency-lonely");
        private static readonly HeroId Escorted = new HeroId("subject-adjacency-escorted");
        private static readonly HeroId Paired = new HeroId("subject-adjacency-paired");

        private static readonly TeamId RedTeam = new TeamId("subject-adjacency-red");
        private static readonly TeamId BlueTeam = new TeamId("subject-adjacency-blue");

        private static HexCoordinate At(HexDirection direction, int steps) =>
            StraightLineQuery.Step(Origin, direction, steps);

        // -------------------------------------------------------------------------------------------
        // The clause
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The set equality the block asks for, on a board where an isolated enemy hero and an enemy
        /// hero standing beside a unit of the ACTOR's own side are both excluded, for different
        /// reasons: one has nobody beside it, and the other's neighbour is on the wrong side.
        /// </summary>
        [Test]
        public void TheClause_AdmitsOnlyHeroesBesideAnotherUnitOfTheirOwnSide()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    // Escorted by an enemy minion at two spaces out: qualifies.
                    [Escorted] = At(HexDirection.East, 2),

                    // Alone in the open: excluded for having nobody beside it.
                    [Lonely] = At(HexDirection.NorthWest, 2),

                    // Beside a minion of the ACTOR's own side: excluded because the clause reads the
                    // subject's side, not the actor's.
                    [Paired] = At(HexDirection.SouthWest, 2)
                },
                enemyMinionsAt: new[] { At(HexDirection.East, 3) },
                friendlyMinionsAt: new[] { At(HexDirection.SouthWest, 3) });

            Assert.That(Subjects(board, narrowed: false),
                Is.EquivalentTo(new[] { Escorted, Lonely, Paired }),
                "The premise: unnarrowed, all three enemy heroes are in radius.");
            Assert.That(Subjects(board, narrowed: true), Is.EqualTo(new[] { Escorted }));
        }

        /// <summary>
        /// The self-exclusion, stated as its own case because it is what "another" means and what
        /// Sabina's filter never had to say. A lone enemy hero is beside an enemy unit - itself - and
        /// the clause must not admit it.
        /// </summary>
        [Test]
        public void AHeroStandingAlone_IsNotAdjacentToAnotherEnemyUnit()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    [Lonely] = At(HexDirection.East, 2)
                });

            Assert.That(Subjects(board, narrowed: false), Is.EqualTo(new[] { Lonely }),
                "The premise: it is a legal subject of the unnarrowed clause.");
            Assert.That(Subjects(board, narrowed: true), Is.Empty,
                "and it is excluded from its own neighbourhood, or every enemy hero on the board " +
                "would qualify by standing next to itself.");
        }

        /// <summary>
        /// Two enemy heroes beside each other each qualify through the other, which is the reading
        /// "another enemy unit" takes: the clause says unit, and p.19's units are heroes and minions.
        /// </summary>
        [Test]
        public void TwoEnemyHeroesBesideEachOther_EachQualifyThroughTheOther()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    [Escorted] = At(HexDirection.East, 2),
                    [Paired] = At(HexDirection.East, 3)
                });

            Assert.That(Subjects(board, narrowed: true), Is.EquivalentTo(new[] { Escorted, Paired }));
        }

        /// <summary>
        /// A hero adjacent to two qualifying units is offered once, not twice - the clause narrows a
        /// set rather than counting neighbours.
        /// </summary>
        [Test]
        public void AHeroBesideTwoQualifyingUnits_IsOfferedOnce()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    [Escorted] = At(HexDirection.East, 2)
                },
                enemyMinionsAt: new[]
                {
                    At(HexDirection.East, 3),
                    StraightLineQuery.Step(At(HexDirection.East, 2), HexDirection.NorthEast, 1)
                });

            Assert.That(Subjects(board, narrowed: true), Is.EqualTo(new[] { Escorted }));
        }

        // -------------------------------------------------------------------------------------------
        // Nothing written before this milestone moves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Sabina's parameter is unchanged and still asks its own question, which is the byte-identity
        /// this milestone owes the declaration it widened. Her clause admits the hero beside a minion
        /// of the ACTOR's side; this milestone's admits the hero beside a minion of the SUBJECT's. On
        /// one board they select different heroes, which is the sharpest way to say they are two
        /// clauses.
        /// </summary>
        [Test]
        public void SabinasParameter_StillAsksItsOwnQuestionAndADifferentOne()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    [Escorted] = At(HexDirection.East, 2),
                    [Paired] = At(HexDirection.SouthWest, 2)
                },
                enemyMinionsAt: new[] { At(HexDirection.East, 3) },
                friendlyMinionsAt: new[] { At(HexDirection.SouthWest, 3) });

            Assert.That(
                Subjects(board, narrowed: false, sabinasClause: true),
                Is.EqualTo(new[] { Paired }),
                "Hers reads the actor's own minions.");
            Assert.That(Subjects(board, narrowed: true), Is.EqualTo(new[] { Escorted }),
                "and this milestone's reads the subject's, on the same board, selecting the other " +
                "hero entirely.");
        }

        /// <summary>
        /// A declaration binding neither parameter keeps the whole radius, which is what every
        /// selection written before either milestone gets.
        /// </summary>
        [Test]
        public void ADeclarationThatBindsNeither_KeepsItsExactCandidateSet()
        {
            var board = CreateBoard(
                enemyHeroes: new Dictionary<HeroId, HexCoordinate>
                {
                    [Escorted] = At(HexDirection.East, 2),
                    [Lonely] = At(HexDirection.NorthWest, 2)
                });

            Assert.That(Subjects(board, narrowed: false), Is.EquivalentTo(new[] { Escorted, Lonely }));
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<HeroId> Subjects(
            AdjacentMoveAuthority board, bool narrowed, bool sabinasClause = false)
        {
            var handler = SyntheticRosterCapabilities.RegisteredHandlers()
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .Single(value => value.Reference.Equals(SyntheticRosterCapabilities.ChooseTargetV2));
            var opened = handler.Execute(Context(board, narrowed, sabinasClause));
            return opened.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<HeroId>()
                : opened.Choice.Candidates.Select(candidate => candidate.HeroId).ToArray();
        }

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, bool narrowed, bool sabinasClause)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] =
                    EffectValue.Integer((int)RosterQueryOrigin.Actor),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(3),
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Hero),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            };
            if (narrowed)
            {
                inputs[SyntheticRosterCapabilities.AdjacentToAnotherEnemyUnitParameter] =
                    EffectValue.Boolean(true);
            }

            if (sabinasClause)
            {
                inputs[SyntheticRosterCapabilities.AdjacentToFriendlyMinionParameter] =
                    EffectValue.Boolean(true);
            }

            return new EffectCapabilityContext(
                new EffectNodeId("probe-bombard-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                BombardInstance,
                new CardDefinitionReference(BombardCard, "probe-rev-1"),
                BombardAction,
                inputs,
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board));
        }

        private static BoardMapDefinition CreateMap()
        {
            var zone = new BoardMapZone(Battle, "Subject Adjacency Zone", "lane", null, false, true);
            var spaces = Disk(4)
                .Select(at => new BoardMapSpace(
                    new SpaceId($"q{at.Q}_r{at.R}"), at, BoardMapSpaceKind.Playable, Battle))
                .ToArray();

            return new BoardMapDefinition(
                Map, 1, "Subject Adjacency Board", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static AdjacentMoveAuthority CreateBoard(
            IReadOnlyDictionary<HeroId, HexCoordinate> enemyHeroes,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            IReadOnlyList<HexCoordinate> friendlyMinionsAt = null)
        {
            var heroes = new List<HeroOccupancy> { new HeroOccupancy(Actor, Origin) };
            heroes.AddRange(enemyHeroes.Select(pair => new HeroOccupancy(pair.Key, pair.Value)));

            var minions = new List<QuickGameMinionSetupState>();
            minions.AddRange((enemyMinionsAt ?? Array.Empty<HexCoordinate>())
                .Select((at, index) => Minion($"enemy-{index:00}", BlueTeam, TeamSide.Blue, at)));
            minions.AddRange((friendlyMinionsAt ?? Array.Empty<HexCoordinate>())
                .Select((at, index) => Minion($"friendly-{index:00}", RedTeam, TeamSide.Red, at)));

            return new AdjacentMoveAuthority(
                CreateMap(),
                heroes,
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minions);
        }

        private static QuickGameMinionSetupState Minion(
            string name, TeamId team, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId($"subject-adjacency-minion-{name}"),
                team,
                side,
                StandardMinionType.Melee,
                default,
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static MatchParticipants Participants()
        {
            var actorPlayer = new PlayerId("subject-adjacency-actor-player");
            var players = new List<Player> { new Player(actorPlayer, RedTeam) };
            var heroes = new List<Hero>
            {
                new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout)
            };

            foreach (var enemy in new[] { Lonely, Escorted, Paired })
            {
                var player = new PlayerId($"subject-adjacency-{enemy.Value}-player");
                players.Add(new Player(player, BlueTeam));
                heroes.Add(new Hero(enemy, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    yield return new HexCoordinate(dq, dr);
                }
            }
        }
    }
}

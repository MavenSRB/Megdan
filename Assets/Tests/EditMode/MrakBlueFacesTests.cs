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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112d. Mrak's Blue faces through configured rounds: the Boulder line's push
    /// ignoring obstacles and the follow move along its heading (kept after a zero-space push), and
    /// the stomps' one-space move with a rock left in the vacated space, p.18's short supply and
    /// Ground Shaker's different-target repeat; restore and replay of each face's pause.
    /// </summary>
    public sealed class MrakBlueFacesTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId FriendlyMinion = new MinionId("mrak-blue-red-melee");
        private static readonly MinionId EnemyMinion = new MinionId("mrak-blue-blue-melee");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- Printed values against the fronts (carried lesson 10) --------------------------------

        [Test]
        public void EveryBlueFaceCarriesItsPrintedValues()
        {
            var faces = MrakBlueFaces.Definitions.ToDictionary(face => face.Id);
            void Expect(CardDefinitionId id, CardTier tier, int initiative, int defense,
                PrintedValueChannel item, int radius)
            {
                var face = faces[id];
                Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Blue), face.DisplayName);
                Assert.That(face.Progression.Tier, Is.EqualTo(tier), face.DisplayName);
                Assert.That(face.Initiative.Value, Is.EqualTo(initiative), face.DisplayName);
                Assert.That(face.Progression.ItemIcon, Is.EqualTo(item), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo(defense), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo(2), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Skill, PrintedValueChannel.Radius),
                    Is.EqualTo(radius), face.DisplayName + ": a missing icon is absent, never zero");
            }

            Expect(MrakBlueFaces.BoulderRush, CardTier.I, 8, 6, PrintedValueChannel.None, 0);
            Expect(MrakBlueFaces.BoulderBlitz, CardTier.II, 9, 7, PrintedValueChannel.Defense, 0);
            Expect(MrakBlueFaces.StompingStep, CardTier.II, 9, 7, PrintedValueChannel.Attack, 4);
            Expect(MrakBlueFaces.Boulderdozer, CardTier.III, 9, 7, PrintedValueChannel.Movement, 0);
            Expect(MrakBlueFaces.GroundShaker, CardTier.III, 9, 7, PrintedValueChannel.Attack, 4);
            Assert.That(faces[MrakBlueFaces.BoulderRush].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakBlueFaces.BoulderBlitz, MrakBlueFaces.StompingStep }));
            Assert.That(faces[MrakBlueFaces.BoulderBlitz].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakBlueFaces.Boulderdozer, MrakBlueFaces.GroundShaker }));
            Assert.That(faces[MrakBlueFaces.StompingStep].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakBlueFaces.Boulderdozer, MrakBlueFaces.GroundShaker }));
        }

        [TestCase(2, new[] { 1, 2 })]
        [TestCase(3, new[] { 1, 2, 3 })]
        [TestCase(4, new[] { 1, 2, 3, 4 })]
        public void EachBoulderOffersItsPrintedPushDistances(int most, int[] distances)
        {
            var face = most == 2 ? MrakBlueFaces.BoulderRush
                : most == 3 ? MrakBlueFaces.BoulderBlitz : MrakBlueFaces.Boulderdozer;
            var match = new Match(heroes: new[] { (Mrak, H(0, -3)), (Enemy, H(0, -2)) });
            var card = match.PlayBlue(face);
            match.StartSkill(card);
            match.PickSpace(H(0, -2));

            Assert.That(match.OfferedIntegers(), Is.EqualTo(distances));
        }

        // --- The Boulder line ----------------------------------------------------------------------

        [Test]
        public void BoulderRush_PushesThroughARockToTheFirstReachableEmptySpace_ThenFollows()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 1)) },
                rocksAt: new[] { H(0, 2) });
            var rush = match.PlayBlue(MrakBlueFaces.BoulderRush);
            match.StartSkill(rush);

            Assert.That(match.OfferedSpaces(), Is.EqualTo(new[] { H(0, 1) }),
                "the only token or enemy unit adjacent to Mrak");
            match.PickSpace(H(0, 1));
            match.PickInteger(2);
            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(0, 3)),
                "\"ignoring obstacles\": two spaces on, passing through the rock");

            Assert.That(match.OfferedSpaces(), Is.EqualTo(new[] { H(0, 1) }),
                "up to 2 along the push: the rock at (0,2) may be crossed but never ended on");
            match.PickSpace(H(0, 1));
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(0, 1)));
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void BoulderRush_AShortPushAndADeclinedFollow()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(1, 0)) });
            var rush = match.PlayBlue(MrakBlueFaces.BoulderRush);
            match.StartSkill(rush);
            match.PickSpace(H(1, 0));
            match.PickInteger(1);
            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(2, 0)));

            Assert.That(match.OfferedSpaces(), Is.EqualTo(new[] { H(1, 0) }),
                "the enemy now stands two along the heading, so one space is the whole follow");
            match.Decline();
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(0, 0)), "\"you may move\" - declined");
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void BoulderBlitz_AZeroSpacePushStillOffersTheFollowAlongItsHeading()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 1)) },
                rocksAt: new[] { H(0, 2) });
            var blitz = match.PlayBlue(MrakBlueFaces.BoulderBlitz);
            match.StartSkill(blitz);
            match.PickSpace(H(0, 1));
            match.PickInteger(1);
            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(0, 1)),
                "one space lands on the rock and no empty space is reachable: p.18's zero");

            Assert.That(match.OfferedSpaces(), Is.EqualTo(new[] { H(0, 3) }),
                "the heading was fixed by the chosen target; Mrak crosses the enemy and the rock");
            match.PickSpace(H(0, 3));
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(0, 3)));
        }

        [Test]
        public void BoulderRush_PushesARock_WhichIsNotAPlacement()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-5, 5)) },
                rocksAt: new[] { H(-1, 0) });
            var rush = match.PlayBlue(MrakBlueFaces.BoulderRush);
            var before = match.RockIds();
            match.StartSkill(rush);
            match.PickSpace(H(-1, 0));
            match.PickInteger(2);

            Assert.That(match.Rocks(), Is.EqualTo(new[] { H(-3, 0) }), "a token is pushed like a unit");
            Assert.That(match.RockIds(), Is.EqualTo(before),
                "the same token moved - no rock was placed, so nothing an after-place fact could read");
        }

        [Test]
        public void TheFollowMoveEndsAtTheBoardsEdgeAndOnNoObstacle()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 4)), (Enemy, H(0, 5)) });
            var rush = match.PlayBlue(MrakBlueFaces.BoulderRush);
            match.StartSkill(rush);
            match.PickSpace(H(0, 5));
            match.PickInteger(2);

            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(0, 5)), "the board ends behind it");
            Assert.That(match.ActionEnded, Is.True,
                "the heading holds only the enemy and the board's edge, so the follow has no space");
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(0, 4)));
        }

        [Test]
        public void ABoulderIsNotOfferedWithNothingToPushBesideMrak()
        {
            var nobody = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) });
            nobody.PlayBlue(MrakBlueFaces.BoulderRush, startTurn: false);
            Assert.That(nobody.IsOffered(MrakCardContent.PrimarySkill), Is.False);
            Assert.That(nobody.IsOffered(MrakCardContent.SecondaryMovement), Is.True);

            var friendOnly = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                minions: new[] { (FriendlyMinion, TeamSide.Red, H(1, 0)) });
            friendOnly.PlayBlue(MrakBlueFaces.BoulderRush, startTurn: false);
            Assert.That(friendOnly.IsOffered(MrakCardContent.PrimarySkill), Is.False,
                "\"a token, or an enemy unit\" - a friendly minion is neither");

            var enemyMinion = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                minions: new[] { (FriendlyMinion, TeamSide.Red, H(1, 0)), (EnemyMinion, TeamSide.Blue, H(-1, 0)) });
            var card = enemyMinion.PlayBlue(MrakBlueFaces.BoulderRush);
            enemyMinion.StartSkill(card);
            Assert.That(enemyMinion.OfferedSpaces(), Is.EqualTo(new[] { H(-1, 0) }));
        }

        [Test]
        public void TheFollowLinePauseRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 1)) });
            var dozer = match.PlayBlue(MrakBlueFaces.Boulderdozer);
            match.StartSkill(dozer);
            match.PickSpace(H(0, 1));
            match.PickInteger(1);
            Assert.That(match.OfferedSpaces(), Is.EquivalentTo(new[] { H(0, 1), H(0, 3), H(0, 4) }),
                "up to four along the heading, crossing the enemy at (0,2) and never ending on it");
            match.AssertRestoreReplays(EffectValue.Space(H(0, 4)));
        }

        [Test]
        public void ThePushDistancePauseRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 1)) });
            var blitz = match.PlayBlue(MrakBlueFaces.BoulderBlitz);
            match.StartSkill(blitz);
            match.PickSpace(H(0, 1));
            match.AssertRestoreReplays(EffectValue.Integer(3));
        }

        // --- The stomps ----------------------------------------------------------------------------

        [Test]
        public void StompingStep_MovesAUnitBesideARockAndLeavesARockWhereItStood()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) });
            var step = match.PlayBlue(MrakBlueFaces.StompingStep);
            match.StartSkill(step);

            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { Enemy }));
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));

            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(1, 2)), "\"1 space\"");
            Assert.That(match.Rocks(), Is.EquivalentTo(new[] { H(0, 4), H(0, 3) }),
                "\"Place a rock token in the space it occupied\"");
            Assert.That(match.RockPlacedBy(H(0, 3)), Is.EqualTo(step.Id),
                "the placed rock names the card whose placing step laid it (8.112f's reader)");
        }

        [Test]
        public void StompingStep_ReachesAUnitBesideTerrain_OfEitherSide()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-5, 5)) },
                minions: new[] { (FriendlyMinion, TeamSide.Red, H(2, 0)) },
                terrainAt: new[] { H(3, 0) });
            var step = match.PlayBlue(MrakBlueFaces.StompingStep);
            match.StartSkill(step);

            Assert.That(match.OfferedMinions(), Is.EqualTo(new[] { FriendlyMinion }),
                "\"a unit\" names no side, and terrain alone qualifies");
            Assert.That(match.OfferedHeroes(), Is.Empty,
                "Mrak is not a unit in his own radius, and the enemy stands beside nothing");
        }

        [Test]
        public void AStompIsNotOfferedWithNoUnitBesideTerrainOrARock()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) });
            match.PlayBlue(MrakBlueFaces.StompingStep, startTurn: false);
            Assert.That(match.IsOffered(MrakCardContent.PrimarySkill), Is.False);
        }

        [Test]
        public void AStompIsNotOfferedWhenTheOnlyQualifyingUnitCannotStep()
        {
            var ring = new[] { H(1, 3), H(1, 2), H(0, 2), H(-1, 3), H(-1, 4), H(0, 4) };
            var boxed = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                terrainAt: ring);
            boxed.PlayBlue(MrakBlueFaces.StompingStep, startTurn: false);
            Assert.That(boxed.IsOffered(MrakCardContent.PrimarySkill), Is.False,
                "beside terrain on every side and nowhere to move: offering it would do nothing");

            var open = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                terrainAt: ring.Skip(1).ToArray());
            open.PlayBlue(MrakBlueFaces.StompingStep, startTurn: false);
            Assert.That(open.IsOffered(MrakCardContent.PrimarySkill), Is.True);
        }

        [Test]
        public void AStompWithTheWholePileStandingAsksWhichRockReturns()
        {
            var standing = new[] { H(0, 4), H(4, -4), H(-4, 0) };
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) }, rocksAt: standing);
            var step = match.PlayBlue(MrakBlueFaces.StompingStep);
            match.StartSkill(step);
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));

            Assert.That(match.PendingNode, Is.EqualTo("stomp-reclaim"),
                "three rocks is the whole supply (MR-Q1), so p.18 takes one back first");
            Assert.That(match.OfferedSpaces(), Is.EquivalentTo(standing));
            match.PickSpace(H(4, -4));
            Assert.That(match.Rocks(), Is.EquivalentTo(new[] { H(0, 4), H(-4, 0), H(0, 3) }));
        }

        [Test]
        public void GroundShaker_RepeatsOnADifferentTarget()
        {
            var match = new Match(
                heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)), (EnemyAlly, H(3, 0)) },
                rocksAt: new[] { H(0, 4) }, terrainAt: new[] { H(4, 0) }, withAlly: true);
            var shaker = match.PlayBlue(MrakBlueFaces.GroundShaker);
            match.StartSkill(shaker);
            Assert.That(match.OfferedHeroes(), Is.EquivalentTo(new[] { Enemy, EnemyAlly }));
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));
            match.AcceptRepeat();

            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { EnemyAlly }),
                "\"on a different target\" - the enemy, now beside the new rock, is taken out");
            match.PickHero(EnemyAlly);
            match.PickSpace(H(2, 1));
            Assert.That(match.Rocks(), Is.EquivalentTo(new[] { H(0, 4), H(0, 3), H(3, 0) }));
        }

        [Test]
        public void TheStompMovePauseRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) });
            var step = match.PlayBlue(MrakBlueFaces.StompingStep);
            match.StartSkill(step);
            match.PickHero(Enemy);
            match.AssertRestoreReplays(EffectValue.Space(H(1, 2)));
        }

        // -------------------------------------------------------------------------------------------

        private static int Printed(CardDefinition face, ActionKind kind, PrintedValueChannel channel) =>
            face.ActionDefinitions.Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount).DefaultIfEmpty(0).Single();

        private static readonly CardDefinition Hold = new CardDefinition(
            new CardDefinitionId("mrak-blue-test-hold"), "rev-1", "TEST - Hold", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        /// <summary>A configured round over Mrak's Blue faces.</summary>
        private sealed class Match
        {
            private static readonly MapId Map = new MapId("mrak-blue-faces");
            private readonly List<CardDefinition> definitions = new List<CardDefinition> { Hold };
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly (HeroId Hero, HexCoordinate At)[] heroes;
            private readonly HexCoordinate[] rocks;
            private readonly HexCoordinate[] terrain;
            private readonly QuickGameMinionSetupState[] minions;
            private readonly bool withAlly;
            private MatchContentCatalogs catalogs;
            private AdjacentMoveAuthority board;
            private RoundResolutionAuthority round;
            private int serial;

            internal Match((HeroId, HexCoordinate)[] heroes, HexCoordinate[] rocksAt = null,
                bool withAlly = false, HexCoordinate[] terrainAt = null,
                (MinionId Id, TeamSide Side, HexCoordinate At)[] minions = null)
            {
                terrain = terrainAt ?? Array.Empty<HexCoordinate>();
                this.heroes = heroes;
                rocks = rocksAt ?? Array.Empty<HexCoordinate>();
                this.withAlly = withAlly;
                this.minions = (minions ?? Array.Empty<(MinionId, TeamSide, HexCoordinate)>())
                    .Select(value => new QuickGameMinionSetupState(
                        value.Id,
                        new TeamId(value.Side == TeamSide.Red
                            ? StagedBoardParticipants.RedTeamIdValue
                            : StagedBoardParticipants.BlueTeamIdValue),
                        value.Side, StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("mrak-blue-spawn")),
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{value.At.Q}_r{value.At.R}")),
                        value.At))
                    .ToArray();
                definitions.AddRange(MrakBlueFaces.Definitions);
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            /// <summary>Gives Mrak the face, the enemies a hold each, and brings the face up to act.</summary>
            internal CardInstance PlayBlue(CardDefinitionId face, bool startTurn = true)
            {
                var card = Give(Mrak, definitions.Single(value => value.Id.Equals(face)));
                var commits = new List<(HeroId, CardInstance)> { (Mrak, card), (Enemy, Give(Enemy, Hold)) };
                if (withAlly) commits.Add((EnemyAlly, Give(EnemyAlly, Hold)));
                EnsureStarted();
                foreach (var (hero, instance) in commits)
                {
                    Assert.That(round.TryCommitCard(hero, instance.Id).Accepted, Is.True);
                }

                ActWith(card);
                return card;
            }

            private CardInstance Give(HeroId owner, CardDefinition definition)
            {
                var card = CardInstance.CreateInHand(
                    new CardInstanceId($"mrak-blue-{owner.Value}-{definition.Id.Value}-{serial++}"),
                    definition.Reference, owner);
                instances.Add(card);
                return card;
            }

            private void EnsureStarted()
            {
                if (round != null) return;
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r))
                    .Where(space => !terrain.Contains(space)).ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces, terrain),
                    heroes.Select(value => new HeroOccupancy(value.Hero, value.At)),
                    Array.Empty<ObstacleOccupancy>(), minions);
                foreach (var at in rocks)
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"mrak-blue-rock-{at.Q}-{at.R}"), MrakComponentContent.Rock,
                        Mrak, Mrak, new CardInstanceId("mrak-blue-earlier"), at));
                }

                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(MrakComponentContent.Handlers()
                        .Concat(MrakRockPlacementContent.Handlers())),
                    new ComponentDefinitionCatalog(MrakComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, instances,
                    withAlly ? StagedBoardParticipants.CreateWithBlueAlly() : StagedBoardParticipants.Create(),
                    new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, board, markers),
                        board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
            }

            private void ActWith(CardInstance card)
            {
                for (var step = 0; step < 30; step++)
                {
                    if (Turn.PendingChoice != null)
                    {
                        var tie = Turn.PendingChoice;
                        Assert.That(round.TryChooseTiedActor(tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(card.Id)
                                ? card.Id : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (Turn.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (Turn.SelectedActor.CardInstanceId.Equals(card.Id)) return;
                    Assert.That(Turn.SelectedActor.CardInstanceId.IsDefined, Is.True,
                        card.Id.Value + " never came up to act.");
                    Assert.That(round.TryPerformHold(Turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                }

                Assert.Fail(card.Id.Value + " never came up to act.");
            }

            internal bool IsOffered(ActionDefinitionId action) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(action) && offer.IsAvailable);

            internal void StartSkill(CardInstance card)
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions, instances, card.OwnerId, card.Id)
                    .Single(value => value.ActionDefinitionId.Equals(MrakCardContent.PrimarySkill));
                Assert.That(round.TryStartSkill(new ActionResolutionId("mrak-blue-skill-" + serial++),
                    offer).Accepted, Is.True);
            }

            internal HexCoordinate HeroAt(HeroId hero)
            {
                Assert.That(board.TryGetHeroCoordinate(hero, out var at), Is.True);
                return at;
            }

            internal HexCoordinate[] Rocks() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MrakComponentContent.RockId))
                    .Select(token => token.Coordinate).ToArray();

            internal string[] RockIds() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MrakComponentContent.RockId))
                    .Select(token => token.Id.Value).OrderBy(id => id, StringComparer.Ordinal).ToArray();

            internal CardInstanceId RockPlacedBy(HexCoordinate at) =>
                board.PlacedTokens.Single(token =>
                    token.Definition.Id.Equals(MrakComponentContent.RockId) && token.Coordinate.Equals(at))
                    .SourceCardInstanceId;

            private IEnumerable<EffectValue> Offered()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "a question is open");
                return choice.Candidates.Select(candidate => candidate.Value);
            }

            internal HexCoordinate[] OfferedSpaces() =>
                Offered().Where(value => value.Kind == EffectValueKind.Coordinate)
                    .Select(value => value.Coordinate).ToArray();

            internal int[] OfferedIntegers() =>
                Offered().Where(value => value.Kind == EffectValueKind.Integer)
                    .Select(value => value.IntegerValue).ToArray();

            internal HeroId[] OfferedHeroes() =>
                Offered().Where(value => value.Kind == EffectValueKind.Hero)
                    .Select(value => value.HeroId).ToArray();

            internal MinionId[] OfferedMinions() =>
                Offered().Where(value => value.Kind == EffectValueKind.Minion)
                    .Select(value => value.MinionId).ToArray();

            internal void PickSpace(HexCoordinate space) => Pick(round, EffectValue.Space(space));

            internal void PickInteger(int value) => Pick(round, EffectValue.Integer(value));

            internal void PickHero(HeroId hero) => Pick(round, EffectValue.Hero(hero));

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                var decline = choice.DeclineInvocation ?? choice.ConfirmInvocation;
                Assert.That(decline, Is.Not.Null, "an `up to` step can be left untaken");
                Assert.That(round.TryAnswerEffectChoice(decline).Accepted, Is.True);
            }

            internal void AcceptRepeat()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "the repeat is offered");
                Assert.That(round.TryAnswerEffectChoice(choice.ConfirmInvocation).Accepted, Is.True);
            }

            private static void Pick(RoundResolutionAuthority target, EffectValue value)
            {
                var choice = target.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "a question is open for " + value);
                var select = choice.SelectInvocations.SingleOrDefault(
                    invocation => invocation.Candidate.Value.Equals(value));
                Assert.That(select, Is.Not.Null, value + " is offered");
                Assert.That(target.TryAnswerEffectChoice(select).Accepted, Is.True);
                var still = target.CurrentTurnAuthority.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(choice.ChoiceId) && still.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                }
            }

            /// <summary>
            /// Snapshot at the open question, restore it, answer both the same way, and require the
            /// same question before and the same authoritative bytes after.
            /// </summary>
            internal void AssertRestoreReplays(EffectValue answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

                Pick(round, answer);
                Pick(restored.Round, answer);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)),
                    "the restored pause replays to the same authoritative bytes");
            }
        }
    }
}

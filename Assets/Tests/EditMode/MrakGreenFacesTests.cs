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
    /// Milestone 8.112e. Mrak's Green faces through configured rounds: the one-space shift of a unit
    /// in range to a space beside terrain or a rock (declinable, Avalanche repeating on the same
    /// unit), and Rolling and Strolling Stone's straight line through obstacles with an empty-space
    /// budget; restore and replay of each face's pause.
    /// </summary>
    public sealed class MrakGreenFacesTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId FriendlyMinion = new MinionId("mrak-green-red-melee");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- Printed values against the fronts (carried lesson 10) --------------------------------

        [Test]
        public void EveryGreenFaceCarriesItsPrintedValues()
        {
            var faces = MrakGreenFaces.Definitions.ToDictionary(face => face.Id);
            void Expect(CardDefinitionId id, CardTier tier, int initiative, int defense,
                PrintedValueChannel item, int range)
            {
                var face = faces[id];
                Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Green), face.DisplayName);
                Assert.That(face.Progression.Tier, Is.EqualTo(tier), face.DisplayName);
                Assert.That(face.Initiative.Value, Is.EqualTo(initiative), face.DisplayName);
                Assert.That(face.Progression.ItemIcon, Is.EqualTo(item), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo(defense), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo(2), face.DisplayName);
                Assert.That(Printed(face, ActionKind.Skill, PrintedValueChannel.Range),
                    Is.EqualTo(range), face.DisplayName + ": a missing icon is absent, never zero");
                Assert.That(Printed(face, ActionKind.Skill, PrintedValueChannel.Radius), Is.EqualTo(0));
            }

            Expect(MrakGreenFaces.TreacherousGround, CardTier.I, 6, 4, PrintedValueChannel.None, 3);
            Expect(MrakGreenFaces.Rockslide, CardTier.II, 5, 4, PrintedValueChannel.Attack, 4);
            Expect(MrakGreenFaces.RollingStone, CardTier.II, 5, 4, PrintedValueChannel.Initiative, 0);
            Expect(MrakGreenFaces.Avalanche, CardTier.III, 5, 5, PrintedValueChannel.Radius, 4);
            Expect(MrakGreenFaces.StrollingStone, CardTier.III, 5, 5, PrintedValueChannel.Initiative, 0);
            Assert.That(faces[MrakGreenFaces.TreacherousGround].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakGreenFaces.Rockslide, MrakGreenFaces.RollingStone }));
            Assert.That(faces[MrakGreenFaces.Rockslide].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakGreenFaces.Avalanche, MrakGreenFaces.StrollingStone }));
            Assert.That(faces[MrakGreenFaces.RollingStone].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MrakGreenFaces.Avalanche, MrakGreenFaces.StrollingStone }));
        }

        // --- The shift ------------------------------------------------------------------------------

        [Test]
        public void TreacherousGround_OffersOnlyDestinationsBesideARock()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.AcceptMay();
            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { Enemy }));
            match.PickHero(Enemy);

            Assert.That(match.OfferedSpaces(), Is.EqualTo(new[] { H(0, 3) }),
                "of the enemy's six neighbours only (0,3) stands beside the rock");
            match.PickSpace(H(0, 3));
            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(0, 3)));
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void TreacherousGround_TerrainQualifiesADestinationToo()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) }, terrainAt: new[] { H(2, 1) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.AcceptMay();
            match.PickHero(Enemy);

            Assert.That(match.OfferedSpaces(), Is.EquivalentTo(new[] { H(0, 3), H(1, 1), H(1, 2) }),
                "\"adjacent to terrain, or a rock token\" - either qualifies");
        }

        [Test]
        public void TreacherousGround_AUnitWithNoQualifyingStepIsNotOffered()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                minions: new[] { (FriendlyMinion, TeamSide.Red, H(-2, 0)) },
                rocksAt: new[] { H(-4, 0) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.AcceptMay();

            Assert.That(match.OfferedMinions(), Is.EqualTo(new[] { FriendlyMinion }),
                "\"a unit\" names no side; the friendly minion can step to (-3,0) beside the rock");
            Assert.That(match.OfferedHeroes(), Is.Empty,
                "the enemy has no neighbour beside terrain or a rock, and Mrak is not his own subject");
        }

        [Test]
        public void TreacherousGround_ReachesItsPrintedRangeAndNoFurther()
        {
            var far = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 4)) },
                rocksAt: new[] { H(1, 4) });
            far.Play(MrakGreenFaces.TreacherousGround);
            Assert.That(far.IsOffered(MrakCardContent.PrimarySkill), Is.False,
                "Range 3; the enemy stands four away and Mrak is not his own subject, so the face can apply nothing (6.5af)");
            Assert.That(far.IsOffered(MrakCardContent.SecondaryMovement), Is.True);

            var slide = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 4)) },
                rocksAt: new[] { H(1, 4) });
            var rockslide = slide.Play(MrakGreenFaces.Rockslide);
            slide.StartSkill(rockslide);
            slide.AcceptMay();
            Assert.That(slide.OfferedHeroes(), Is.EqualTo(new[] { Enemy }), "Rockslide prints Range 4");
        }

        [Test]
        public void TreacherousGround_MayBeDeclined()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.Decline();

            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(0, 2)), "\"You may move\" - declined");
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void Avalanche_MayRepeatOnTheSameUnit()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) });
            var avalanche = match.Play(MrakGreenFaces.Avalanche);
            match.StartSkill(avalanche);
            match.AcceptMay();
            match.PickHero(Enemy);
            match.PickSpace(H(0, 3));
            match.AcceptRepeat();
            match.AcceptMay();

            Assert.That(match.OfferedHeroes(), Does.Contain(Enemy),
                "\"May repeat once\" names no different target, so p.10 lets the same unit be chosen");
            match.PickHero(Enemy);
            Assert.That(match.OfferedSpaces(), Is.EquivalentTo(new[] { H(1, 3), H(-1, 4) }),
                "from (0,3), the neighbours beside the rock at (0,4)");
            match.PickSpace(H(1, 3));
            Assert.That(match.HeroAt(Enemy), Is.EqualTo(H(1, 3)));
        }

        [Test]
        public void TheShiftDestinationPauseRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) }, terrainAt: new[] { H(2, 1) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.AcceptMay();
            match.PickHero(Enemy);
            match.AssertRestoreReplays(EffectValue.Space(H(1, 1)));
        }

        // --- The roll -------------------------------------------------------------------------------

        [Test]
        public void RollingStone_CrossesObstaclesFreely_AndOneEmptySpaceAtMost()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(1, 0)) },
                rocksAt: new[] { H(2, 0) });
            var rolling = match.Play(MrakGreenFaces.RollingStone);
            match.StartSkill(rolling);
            var east = match.OfferedSpaces().Where(space => space.R == 0 && space.Q > 0).ToArray();

            Assert.That(east, Is.EquivalentTo(new[] { H(3, 0), H(4, 0) }),
                "the enemy and the rock cost nothing; (4,0) crosses one empty space, (5,0) would cross two");
            match.PickSpace(H(4, 0));
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(4, 0)));
        }

        [Test]
        public void RollingStone_OnAnOpenBoardReachesTwoSpaces()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-5, 5)) });
            var rolling = match.Play(MrakGreenFaces.RollingStone);
            match.StartSkill(rolling);
            Assert.That(match.OfferedSpaces().Select(space => HexDistanceQuery.Calculate(H(0, 0), space))
                .Distinct().OrderBy(value => value), Is.EqualTo(new[] { 1, 2 }),
                "\"The starting space and the destination space do not count\": one empty space between");
            Assert.That(match.OfferedSpaces().Length, Is.EqualTo(12), "six files, two spaces each");
        }

        [Test]
        public void StrollingStone_AllowsTwoEmptySpaces()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(1, 0)) },
                rocksAt: new[] { H(2, 0) });
            var strolling = match.Play(MrakGreenFaces.StrollingStone);
            match.StartSkill(strolling);
            var east = match.OfferedSpaces().Where(space => space.R == 0 && space.Q > 0).ToArray();

            Assert.That(east, Is.EquivalentTo(new[] { H(3, 0), H(4, 0), H(5, 0) }),
                "two empty spaces may be crossed; the board ends at (5,0)");
        }

        [Test]
        public void TheRollCrossesTerrainAndNeverEndsOnAnObstacle()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-5, 5)) },
                terrainAt: new[] { H(-1, 0), H(-2, 0) }, rocksAt: new[] { H(-4, 0) });
            var rolling = match.Play(MrakGreenFaces.RollingStone);
            match.StartSkill(rolling);
            var west = match.OfferedSpaces().Where(space => space.R == 0 && space.Q < 0).ToArray();

            Assert.That(west, Is.EquivalentTo(new[] { H(-3, 0), H(-5, 0) }),
                "terrain is crossed, the rock at (-4,0) is never an ending, and (-5,0) crosses one empty space");
        }

        [Test]
        public void TheRollMayStandStill()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-5, 5)) });
            var rolling = match.Play(MrakGreenFaces.RollingStone);
            match.StartSkill(rolling);
            match.Decline();
            Assert.That(match.HeroAt(Mrak), Is.EqualTo(H(0, 0)), "\"any number\" includes none (GA-Q1)");
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void TheRollLinePauseRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(1, 0)) },
                rocksAt: new[] { H(2, 0) });
            var rolling = match.Play(MrakGreenFaces.RollingStone);
            match.StartSkill(rolling);
            match.AssertRestoreReplays(EffectValue.Space(H(4, 0)));
        }

        // -------------------------------------------------------------------------------------------

        private static int Printed(CardDefinition face, ActionKind kind, PrintedValueChannel channel) =>
            face.ActionDefinitions.Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount).DefaultIfEmpty(0).Single();

        private static readonly CardDefinition Hold = new CardDefinition(
            new CardDefinitionId("mrak-green-test-hold"), "rev-1", "TEST - Hold", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        /// <summary>A configured round over Mrak's Green faces.</summary>
        private sealed class Match
        {
            private static readonly MapId Map = new MapId("mrak-green-faces");
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
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("mrak-green-spawn")),
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{value.At.Q}_r{value.At.R}")),
                        value.At))
                    .ToArray();
                definitions.AddRange(MrakGreenFaces.Definitions);
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            /// <summary>Gives Mrak the face, the enemies a hold each, and brings the face up to act.</summary>
            internal CardInstance Play(CardDefinitionId face, bool startTurn = true)
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
                    new CardInstanceId($"mrak-green-{owner.Value}-{definition.Id.Value}-{serial++}"),
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
                        new TokenInstanceId($"mrak-green-rock-{at.Q}-{at.R}"), MrakComponentContent.Rock,
                        Mrak, Mrak, new CardInstanceId("mrak-green-earlier"), at));
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
                Assert.That(round.TryStartSkill(new ActionResolutionId("mrak-green-skill-" + serial++),
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

            internal void AcceptMay()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "the `may` is asked");
                Assert.That(choice.ConfirmInvocation, Is.Not.Null);
                Assert.That(round.TryAnswerEffectChoice(choice.ConfirmInvocation).Accepted, Is.True);
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

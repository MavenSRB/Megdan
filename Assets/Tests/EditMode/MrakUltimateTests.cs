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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112f. Rock and a Hard Place through configured rounds - "Once per turn, after you
    /// place one or more Rock tokens into one or more spaces adjacent to one or more enemy heroes, each
    /// of those heroes discards a card, if able." - from a Skill's placement, an Attack's after-the-
    /// attack placement and a stomp, never from a push or a move; once per turn; one run per hero;
    /// restore of a paused run with a hero still owed. And the printed set reconciled with his shard's
    /// inventory table.
    /// </summary>
    public sealed class MrakUltimateTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId EnemyMinion = new MinionId("mrak-ultimate-blue-melee");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- The trigger ---------------------------------------------------------------------------

        [Test]
        public void AStompPlacingARockBesideAnEnemyHero_MakesThatHeroDiscard()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) });
            var step = match.Play(MrakBlueFaces.StompingStep);
            match.StartSkill(step);
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));

            Assert.That(match.HardPlaceAsked, Is.True,
                "the rock at (0,3) stands beside the enemy it moved to (1,2)");
            Assert.That(match.Question.ChooserId, Is.EqualTo(Enemy), "\"each of those heroes discards\"");
            match.PickCard(match.WallOf(Enemy).Id);

            Assert.That(match.WallOf(Enemy).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(match.HardPlaceAsked, Is.False);
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void StoneGrip_EachAdjacentEnemyHeroDiscards_InOrder()
        {
            var match = new Match(
                heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)), (EnemyAlly, H(-1, 4)) },
                withAlly: true);
            var grip = match.Play(MrakCardContent.StoneGrip);
            match.StartSkill(grip);
            match.PickHero(Enemy);

            Assert.That(match.HardPlaceAsked, Is.True);
            Assert.That(match.Question.ChooserId, Is.EqualTo(EnemyAlly),
                "both heroes stand beside a placed rock; one trigger owes each a run, in identity order");
            match.PickCard(match.WallOf(EnemyAlly).Id);

            Assert.That(match.Question.ChooserId, Is.EqualTo(Enemy));
            match.PickCard(match.WallOf(Enemy).Id);
            Assert.That(match.WallOf(Enemy).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(match.WallOf(EnemyAlly).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void Fissure_TriggersFromTheAttacksOwnText()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-2, 1)) },
                minions: new[] { (EnemyMinion, H(-1, 0)) });
            var fissure = match.Play(MrakCardContent.Fissure);
            match.StartAttack(fissure);
            match.Target(EnemyMinion);

            Assert.That(match.Rocks(), Is.EquivalentTo(new[] { H(-1, 0), H(-2, 0), H(-3, 0) }));
            Assert.That(match.HardPlaceAsked, Is.True,
                "After the attack: the placement is card text inside p.14, and the moment still happens");
            match.PickCard(match.WallOf(Enemy).Id);
            Assert.That(match.WallOf(Enemy).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void GroundShaker_TheSecondPlacementThisTurnTriggersNothing()
        {
            var match = new Match(
                heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)), (EnemyAlly, H(3, 0)) },
                rocksAt: new[] { H(0, 4) }, terrainAt: new[] { H(4, 0) }, withAlly: true);
            var shaker = match.Play(MrakBlueFaces.GroundShaker);
            match.StartSkill(shaker);
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));
            match.PickCard(match.WallOf(Enemy).Id);
            match.AcceptRepeat();
            match.PickHero(EnemyAlly);
            match.PickSpace(H(2, 1));

            Assert.That(match.Rocks(), Does.Contain(H(3, 0)), "the second rock stands beside the ally");
            Assert.That(match.HardPlaceAsked, Is.False, "\"Once per turn\" - already spent");
            Assert.That(match.WallOf(EnemyAlly).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void APushedRockBesideAnEnemyIsNotAPlacement()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(-4, 1)) },
                rocksAt: new[] { H(-1, 0) });
            var rush = match.Play(MrakBlueFaces.BoulderRush);
            match.StartSkill(rush);
            match.PickSpace(H(-1, 0));
            match.PickInteger(2);

            Assert.That(match.Rocks(), Is.EqualTo(new[] { H(-3, 0) }), "the rock now stands beside the enemy");
            Assert.That(match.HardPlaceAsked, Is.False, "a Push is not a Place");
            Assert.That(match.WallOf(Enemy).State.Location, Is.EqualTo(CardLocation.Hand));
        }

        [Test]
        public void MovingAUnitBesideARockIsNotAPlacement()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(0, 4) });
            var ground = match.Play(MrakGreenFaces.TreacherousGround);
            match.StartSkill(ground);
            match.AcceptMay();
            match.PickHero(Enemy);
            match.PickSpace(H(0, 3));

            Assert.That(match.HardPlaceAsked, Is.False);
            Assert.That(match.WallOf(Enemy).State.Location, Is.EqualTo(CardLocation.Hand));
        }

        [Test]
        public void ALockedUltimateWatchesNothing()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) }, unlock: false);
            var step = match.Play(MrakBlueFaces.StompingStep);
            match.StartSkill(step);
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));

            Assert.That(match.HardPlaceAsked, Is.False);
            Assert.That(match.ActionEnded, Is.True);
        }

        [Test]
        public void AnEmptyHandedEnemyGivesNothing_IfAble()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) }, enemyHoldsAWall: false);
            var step = match.Play(MrakBlueFaces.StompingStep);
            match.StartSkill(step);
            match.PickHero(Enemy);
            match.PickSpace(H(1, 2));

            Assert.That(match.HardPlaceAsked, Is.False, "\"if able\": nothing to discard, nothing asked");
            Assert.That(match.ActionEnded, Is.True);
            Assert.That(match.OnBoard(Enemy), Is.True, "and nothing escalates to a defeat");
        }

        [Test]
        public void APausedRunWithAHeroStillOwedRestoresAndReplaysToTheSameBytes()
        {
            var match = new Match(
                heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)), (EnemyAlly, H(-1, 4)) },
                withAlly: true);
            var grip = match.Play(MrakCardContent.StoneGrip);
            match.StartSkill(grip);
            match.PickHero(Enemy);
            match.AssertRestoreReplays(EffectValue.Card(match.WallOf(EnemyAlly).Id));
        }

        [Test]
        public void OnlyThePlacersOwnUltimateAndItsDeclaredKindWatch()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) });
            match.Play(MrakBlueFaces.StompingStep, startTurn: false);
            var ultimates = match.Ultimates;
            var rock = new PlacedToken(new TokenInstanceId("probe-rock"), MrakComponentContent.Rock,
                Mrak, Mrak, new CardInstanceId("probe"), H(0, 2));

            Assert.That(AfterTokensPlacedTriggerQuery.Derive(
                ultimates, match.Board, match.Participants, Mrak, new[] { rock }).Select(s => s.RecipientId),
                Is.EqualTo(new[] { Enemy }));
            Assert.That(AfterTokensPlacedTriggerQuery.Derive(
                ultimates, match.Board, match.Participants, Enemy, new[] { rock }), Is.Empty,
                "another hero's placement is not \"you place\"");
            var other = new TokenDefinition(new TokenDefinitionId("probe-other"), 1, "Other", 3,
                Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfRoundCleanup);
            Assert.That(AfterTokensPlacedTriggerQuery.Derive(
                ultimates, match.Board, match.Participants, Mrak,
                new[] { new PlacedToken(new TokenInstanceId("probe-other"), other, Mrak, Mrak,
                    new CardInstanceId("probe"), H(0, 2)) }), Is.Empty,
                "\"Rock tokens\" - another kind watches nothing");
        }

        // --- The set reconciled with the inventory (carried lessons 1, 3 and 10) ------------------

        private static readonly (CardDefinitionId Id, CardColor Color, CardTier Tier, int Init, int D, int M,
            int SA, PrintedValueChannel Icon, int IconValue, PrintedValueChannel Item)[] Table =
        {
            (MrakCardContent.Fissure, CardColor.Gold, CardTier.None, 11, 3, 1, 0, PrintedValueChannel.Attack, 4, PrintedValueChannel.None),
            (MrakCardContent.StoneGrip, CardColor.Silver, CardTier.None, 12, 4, 0, 0, PrintedValueChannel.Range, 3, PrintedValueChannel.None),
            (MrakRedFaces.SeismicSlam, CardColor.Red, CardTier.I, 7, 7, 3, 6, PrintedValueChannel.Radius, 3, PrintedValueChannel.None),
            (MrakRedFaces.SeismicAssault, CardColor.Red, CardTier.II, 7, 8, 3, 7, PrintedValueChannel.Radius, 4, PrintedValueChannel.Initiative),
            (MrakRedFaces.StoneCarapace, CardColor.Red, CardTier.II, 7, 6, 0, 7, PrintedValueChannel.Movement, 4, PrintedValueChannel.Defense),
            (MrakRedFaces.Epicentre, CardColor.Red, CardTier.III, 8, 8, 3, 7, PrintedValueChannel.Radius, 4, PrintedValueChannel.Range),
            (MrakRedFaces.RockSolid, CardColor.Red, CardTier.III, 8, 6, 0, 7, PrintedValueChannel.Movement, 4, PrintedValueChannel.Defense),
            (MrakBlueFaces.BoulderRush, CardColor.Blue, CardTier.I, 8, 6, 2, 0, PrintedValueChannel.None, 0, PrintedValueChannel.None),
            (MrakBlueFaces.BoulderBlitz, CardColor.Blue, CardTier.II, 9, 7, 2, 0, PrintedValueChannel.None, 0, PrintedValueChannel.Defense),
            (MrakBlueFaces.StompingStep, CardColor.Blue, CardTier.II, 9, 7, 2, 0, PrintedValueChannel.Radius, 4, PrintedValueChannel.Attack),
            (MrakBlueFaces.Boulderdozer, CardColor.Blue, CardTier.III, 9, 7, 2, 0, PrintedValueChannel.None, 0, PrintedValueChannel.Movement),
            (MrakBlueFaces.GroundShaker, CardColor.Blue, CardTier.III, 9, 7, 2, 0, PrintedValueChannel.Radius, 4, PrintedValueChannel.Attack),
            (MrakGreenFaces.TreacherousGround, CardColor.Green, CardTier.I, 6, 4, 2, 0, PrintedValueChannel.Range, 3, PrintedValueChannel.None),
            (MrakGreenFaces.Rockslide, CardColor.Green, CardTier.II, 5, 4, 2, 0, PrintedValueChannel.Range, 4, PrintedValueChannel.Attack),
            (MrakGreenFaces.RollingStone, CardColor.Green, CardTier.II, 5, 4, 2, 0, PrintedValueChannel.None, 0, PrintedValueChannel.Initiative),
            (MrakGreenFaces.Avalanche, CardColor.Green, CardTier.III, 5, 5, 2, 0, PrintedValueChannel.Range, 4, PrintedValueChannel.Radius),
            (MrakGreenFaces.StrollingStone, CardColor.Green, CardTier.III, 5, 5, 2, 0, PrintedValueChannel.None, 0, PrintedValueChannel.Initiative),
            (MrakCardContent.LesserFissure, CardColor.Gold, CardTier.None, 10, 2, 1, 0, PrintedValueChannel.Attack, 3, PrintedValueChannel.None),
        };

        [Test]
        public void EveryDefinitionMatchesItsInventoryRow()
        {
            foreach (var row in Table)
            {
                var face = MrakCardContent.Definition(row.Id);
                var name = face.DisplayName;
                Assert.That(face.Progression.Color, Is.EqualTo(row.Color), name);
                if (row.Tier != CardTier.None) Assert.That(face.Progression.Tier, Is.EqualTo(row.Tier), name);
                Assert.That(face.Initiative.Value, Is.EqualTo(row.Init), name);
                Assert.That(face.Progression.ItemIcon, Is.EqualTo(row.Item), name);
                Assert.That(Value(face, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                    PrintedValueChannel.Defense), Is.EqualTo(row.D), name + " Defense");
                Assert.That(Value(face, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    PrintedValueChannel.Movement), Is.EqualTo(row.M), name + " secondary Movement");
                Assert.That(Value(face, ActionKind.Attack, ActionDefinitionOrigin.PrintedSecondary,
                    PrintedValueChannel.Attack), Is.EqualTo(row.SA), name + " secondary Attack");
                var primary = face.ActionDefinitions.Single(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var icons = primary.BaseValues.Where(v => v.Channel == row.Icon).ToArray();
                if (row.Icon == PrintedValueChannel.None)
                {
                    Assert.That(primary.BaseValues, Is.Empty, name + ": the primary prints no icon");
                }
                else
                {
                    Assert.That(icons.Single().Amount, Is.EqualTo(row.IconValue), name + " primary icon");
                }
            }

            var ultimate = MrakCardContent.Definition(MrakCardContent.RockAndAHardPlace);
            Assert.That(ultimate.Progression.IsUltimate, Is.True);
            Assert.That(ultimate.UltimateRule.AfterTokensPlaced.TokenKind, Is.EqualTo(MrakComponentContent.RockId));
            Assert.That(ultimate.ActionDefinitions, Is.Empty);
        }

        [Test]
        public void TheSetIsTheNineteenRowTable_AndTheDealtEighteenSplitAsP6Deals()
        {
            Assert.That(MrakCardContent.AllDefinitions.Select(d => d.Id), Is.EqualTo(MrakCardContent.InventoryOrder));
            Assert.That(MrakCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(MrakCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(MrakCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { MrakCardContent.LesserFissure }));
            Assert.That(MrakCardContent.StartingHand, Is.EquivalentTo(new[]
            {
                MrakCardContent.Fissure, MrakCardContent.StoneGrip, MrakRedFaces.SeismicSlam,
                MrakBlueFaces.BoulderRush, MrakGreenFaces.TreacherousGround
            }));
            Assert.That(MrakCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(MrakCardContent.Deck, Does.Contain(MrakCardContent.RockAndAHardPlace));

            var hand = MrakCardContent.CreateHand(Mrak, "probe");
            Assert.That(hand.Definitions.Count, Is.EqualTo(18));
        }

        [Test]
        public void P15sUpgradeListsNameBothSuccessorsOfTheSameColour()
        {
            var faces = MrakCardContent.DealtDefinitions.ToDictionary(d => d.Id);
            foreach (var color in new[] { CardColor.Red, CardColor.Blue, CardColor.Green })
            {
                var tierOne = faces.Values.Single(d => d.Progression.Color == color && d.Progression.Tier == CardTier.I);
                var tierTwo = faces.Values.Where(d => d.Progression.Color == color && d.Progression.Tier == CardTier.II)
                    .Select(d => d.Id).ToArray();
                var tierThree = faces.Values.Where(d => d.Progression.Color == color && d.Progression.Tier == CardTier.III)
                    .Select(d => d.Id).ToArray();
                Assert.That(tierTwo.Length, Is.EqualTo(2), color.ToString());
                Assert.That(tierThree.Length, Is.EqualTo(2), color.ToString());
                Assert.That(tierOne.Progression.NextTierCandidateIds, Is.EquivalentTo(tierTwo), tierOne.DisplayName);
                foreach (var id in tierTwo)
                {
                    Assert.That(faces[id].Progression.NextTierCandidateIds, Is.EquivalentTo(tierThree),
                        faces[id].DisplayName + " names BOTH Tier III faces (the Snorri lesson)");
                }

                foreach (var id in tierThree)
                {
                    Assert.That(faces[id].Progression.NextTierCandidateIds, Is.Empty, faces[id].DisplayName);
                }
            }
        }

        private static int Value(CardDefinition face, ActionKind kind, ActionDefinitionOrigin origin,
            PrintedValueChannel channel) =>
            face.ActionDefinitions.Where(a => a.Kind == kind && a.Origin == origin)
                .SelectMany(a => a.BaseValues).Where(v => v.Channel == channel)
                .Select(v => v.Amount).DefaultIfEmpty(0).Single();

        // -------------------------------------------------------------------------------------------

        private static readonly CardDefinition Hold = new CardDefinition(
            new CardDefinitionId("mrak-ultimate-test-hold"), "rev-1", "TEST - Hold", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("mrak-ultimate-test-wall"), "rev-1", "TEST - Wall", new InitiativeValue(2),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NonBasicCard(CardColor.Blue, CardTier.I, PrintedValueChannel.None),
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        /// <summary>A configured round with Mrak's Ultimate unlocked beside his dashboard.</summary>
        private sealed class Match
        {
            private static readonly MapId Map = new MapId("mrak-ultimate");
            private readonly List<CardDefinition> definitions = new List<CardDefinition> { Hold, Wall };
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly Dictionary<HeroId, CardInstance> walls = new Dictionary<HeroId, CardInstance>();
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
                (MinionId Id, HexCoordinate At)[] minions = null, bool unlock = true,
                bool enemyHoldsAWall = true)
            {
                terrain = terrainAt ?? Array.Empty<HexCoordinate>();
                this.heroes = heroes;
                rocks = rocksAt ?? Array.Empty<HexCoordinate>();
                this.withAlly = withAlly;
                this.minions = (minions ?? Array.Empty<(MinionId, HexCoordinate)>())
                    .Select(value => new QuickGameMinionSetupState(
                        value.Id, new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("mrak-ultimate-spawn")),
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{value.At.Q}_r{value.At.R}")),
                        value.At))
                    .ToArray();
                definitions.AddRange(MrakCardContent.AllDefinitions);
                var ultimate = CardInstance.CreateInDeck(new CardInstanceId("mrak-ultimate-card"),
                    MrakCardContent.RockAndAHardPlaceDefinition.Reference, Mrak);
                instances.Add(ultimate);
                if (unlock)
                {
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                }

                if (enemyHoldsAWall) walls[Enemy] = Give(Enemy, Wall);
                if (withAlly) walls[EnemyAlly] = Give(EnemyAlly, Wall);
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal bool HardPlaceAsked =>
                Turn.PendingEffectChoice?.NodeId.Value == "hard-place-discard";

            internal PendingEffectChoice Question
            {
                get
                {
                    var choice = Turn.PendingEffectChoice;
                    Assert.That(choice, Is.Not.Null, "a question is open");
                    return choice;
                }
            }

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal CardInstance WallOf(HeroId hero) => walls[hero];

            internal UnlockedUltimateSet Ultimates => UltimateUnlockQuery.Derive(definitions, instances);

            internal AdjacentMoveAuthority Board => board;

            internal MatchParticipants Participants =>
                withAlly ? StagedBoardParticipants.CreateWithBlueAlly() : StagedBoardParticipants.Create();

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
                    new CardInstanceId($"mrak-ultimate-{owner.Value}-{definition.Id.Value}-{serial++}"),
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
                        new TokenInstanceId($"mrak-ultimate-rock-{at.Q}-{at.R}"), MrakComponentContent.Rock,
                        Mrak, Mrak, new CardInstanceId("mrak-ultimate-earlier"), at));
                }

                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(MrakComponentContent.Handlers()
                        .Concat(MrakRockPlacementContent.Handlers())),
                    new ComponentDefinitionCatalog(MrakComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(MrakCardContent.TriggeredEffects()));
                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, instances, Participants,
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

            private ActionOffer Offer(CardInstance card, ActionDefinitionId action) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, card.OwnerId, card.Id)
                    .Single(value => value.ActionDefinitionId.Equals(action));

            internal void StartSkill(CardInstance card) =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("mrak-ultimate-skill-" + serial++),
                    Offer(card, MrakCardContent.PrimarySkill)).Accepted, Is.True);

            internal void StartAttack(CardInstance card) =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("mrak-ultimate-attack-" + serial++),
                    Offer(card, MrakCardContent.PrimaryAttack)).Accepted, Is.True);

            internal void Target(MinionId minion)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, minion)).Accepted, Is.True);
            }

            internal bool OnBoard(HeroId hero) => board.TryGetHeroCoordinate(hero, out _);

            internal HexCoordinate[] Rocks() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MrakComponentContent.RockId))
                    .Select(token => token.Coordinate).ToArray();

            internal void PickSpace(HexCoordinate space) => Pick(round, EffectValue.Space(space));

            internal void PickInteger(int value) => Pick(round, EffectValue.Integer(value));

            internal void PickHero(HeroId hero) => Pick(round, EffectValue.Hero(hero));

            internal void PickCard(CardInstanceId card) => Pick(round, EffectValue.Card(card));

            internal void AcceptMay()
            {
                Assert.That(round.TryAnswerEffectChoice(Question.ConfirmInvocation).Accepted, Is.True);
            }

            internal void AcceptRepeat()
            {
                Assert.That(round.TryAnswerEffectChoice(Question.ConfirmInvocation).Accepted, Is.True);
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

            internal void AssertRestoreReplays(EffectValue answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.ChooserId, Is.EqualTo(pending.ChooserId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

                Pick(round, answer);
                Pick(restored.Round, answer);
                Assert.That(restored.Round.CurrentTurnAuthority.PendingEffectChoice?.ChooserId,
                    Is.EqualTo(Turn.PendingEffectChoice?.ChooserId),
                    "the hero still owed a run crossed the snapshot and is asked next");
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)),
                    "the restored pause replays to the same authoritative bytes");
            }
        }
    }
}

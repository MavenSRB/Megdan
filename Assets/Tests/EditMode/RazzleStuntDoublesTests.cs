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
    /// Milestone 9.21d. Razzle's Stunt Doubles and the boxed Stunted Doubles: p.13's adjacent Attack,
    /// then "Spawn up to 3 more of you in radius" - 0-3 bodies from his supply of four (RZ-Q1, no
    /// reclaim) into empty spaces within the printed Radius 1 of the acting body (RZ-Q4), widened by a
    /// Radius item (R1). Nothing is registered: the round below composes Razzle's handlers into its
    /// own catalog and gives the Red hero Razzle's supply of four bodies.
    ///
    /// <para><b>The board.</b> A radius-5 disk. Razzle's body 1 stands at the origin with an enemy
    /// minion beside it at (-1, 0); the enemy hero waits far away at (-5, 5) unless a case moves it.</para>
    /// </summary>
    public sealed class RazzleStuntDoublesTests
    {
        private static readonly HeroId Razzle = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("razzle-stunt-doubles");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate Origin = H(0, 0);
        private static readonly HexCoordinate BesideHim = H(-1, 0);

        private static readonly HexCoordinate[] Neighbours =
        {
            H(1, 0), H(1, -1), H(0, -1), H(-1, 0), H(-1, 1), H(0, 1)
        };

        // --- The printed faces ----------------------------------------------------------------------

        [Test]
        public void StuntDoubles_IsTheVerifiedGoldBasic()
        {
            var face = RazzleCardContent.StuntDoublesDefinition;
            Assert.That(face.Initiative.Value, Is.EqualTo(13));
            Assert.That(face.ContentRevision, Is.EqualTo("razzle-9-21j-rev-1"), "his dealt revision (9.21j)");
            Assert.That((face.Progression.Color, face.Progression.Classification), Is.EqualTo((CardColor.Gold, CardClassification.Basic)));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(
                "Target a unit adjacent to you. After the attack: Spawn up to 3 more of you in radius; " +
                "each of you is the same hero, except when actions are performed. If defeated, remove " +
                "all of you."));
            var attack = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            Assert.That(attack.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged), "no Range icon: adjacent.");
            Assert.That(attack.BaseValues.Select(value => (value.Channel, value.Amount)), Is.EquivalentTo(new[]
            {
                (PrintedValueChannel.Attack, 2), (PrintedValueChannel.Radius, 1)
            }));
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(Secondary(face, ActionKind.Movement), Is.EqualTo(1));
            Assert.That(Secondary(face, ActionKind.Defense), Is.EqualTo(1));
        }

        [Test]
        public void StuntedDoubles_IsTheBoxedHandicap_TargetingAHero_WithTheSameSpawn()
        {
            var face = RazzleCardContent.StuntedDoublesDefinition;
            Assert.That(face.Initiative.Value, Is.EqualTo(12));
            Assert.That((face.Progression.Color, face.Progression.Classification), Is.EqualTo((CardColor.Gold, CardClassification.Basic)));
            Assert.That(face.SourceContent.PrintedText, Does.StartWith("Target a hero adjacent to you. After the attack: Spawn up to 3 more of you in radius;"));
            var attack = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            Assert.That(attack.BaseValues.Select(value => (value.Channel, value.Amount)), Is.EquivalentTo(new[]
            {
                (PrintedValueChannel.Attack, 1), (PrintedValueChannel.Radius, 1)
            }));
            Assert.That(attack.AttackTargetPolicy.Value.TargetableUnits, Is.EqualTo(AttackTargetableUnits.HeroesOnly));
            Assert.That(
                RazzleCardContent.StuntDoublesDefinition.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Attack).AttackTargetPolicy.Value.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesAndMinions));
            Assert.That(attack.EffectRoot.Nodes.Select(node => (node.Id.Value, node.FailurePolicy, node.Capability)),
                Is.EqualTo(RazzleCardContent.StuntDoublesDefinition.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Attack).EffectRoot.Nodes
                    .Select(node => (node.Id.Value, node.FailurePolicy, node.Capability))),
                "the Handicap prints the same spawn.");
            Assert.That(Secondary(face, ActionKind.Movement), Is.EqualTo(1));
            Assert.That(Secondary(face, ActionKind.Defense), Is.EqualTo(1));
        }

        [Test]
        public void TheSpawn_IsThreeOptionalSpawnsOfOneBody_BindingNoLiteralReach()
        {
            var text = RazzleCardContent.StuntDoublesDefinition.ActionDefinitions
                .Single(action => action.Kind == ActionKind.Attack).EffectRoot;
            var spawns = text.Nodes.Where(node => node.Kind == EffectNodeKind.Capability).ToArray();
            Assert.That(spawns.Select(node => node.Id.Value),
                Is.EqualTo(new[] { "stunt-doubles-spawn-1", "stunt-doubles-spawn-2", "stunt-doubles-spawn-3" }));
            Assert.That(spawns.Select(node => node.FailurePolicy), Is.All.EqualTo(EffectFailurePolicy.May),
                "\"up to 3\": each body is optional, so 0-3 is the player's (W5).");
            Assert.That(spawns.Select(node => node.Capability), Is.All.EqualTo(RazzleEffectContent.SpawnBodyV1));
            Assert.That(spawns.SelectMany(node => node.Inputs), Is.Empty,
                "\"in radius\" is the printed icon with its items (R1), read by the handler, never a literal.");
        }

        // --- Where a body may go ---------------------------------------------------------------------

        [Test]
        public void TheSpawn_OffersEveryEmptySpaceBesideTheActingBody()
        {
            var round = new Round(friendlyMinionsAt: new[] { H(0, 1) });
            round.AttackTheMinionBesideHim();

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(1)));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(
                Neighbours.Where(at => !at.Equals(H(0, 1)) && round.IsEmpty(at))),
                "Radius 1 from the body that attacked, only empty spaces (p.18-19).");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(0, 1)), "a friendly minion stands there.");
            Assert.That(round.OfferedSpaces().All(at => HexDistanceQuery.Calculate(at, Origin) == 1), Is.True);
        }

        [Test]
        public void ThreeSpawns_PutThreeMoreBodiesOfTheSameHeroOnTheBoard()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(1, 0));
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(2)));
            round.PickSpaces(H(0, -1));
            round.PickSpaces(H(0, 1));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, Origin),
                new HeroOccupancy(Razzle, 2, H(1, 0)),
                new HeroOccupancy(Razzle, 3, H(0, -1)),
                new HeroOccupancy(Razzle, 4, H(0, 1))
            }), "each spawn is the next body of the same hero (9.21a), four in all (RZ-Q1).");
            Assert.That(round.Board.IsEmptyPlayableSpace(H(1, 0)), Is.False, "a body is a unit and an obstacle.");
        }

        [Test]
        public void EachSpawnIsMeasuredFromTheActingBody_NotFromTheBodiesItSpawned()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(1, 0));

            Assert.That(round.OfferedSpaces(), Has.No.Member(H(2, 0)),
                "the second body stands at (1, 0), but the spawn is measured from the body acting.");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(1, 0)), "it stands there now.");
        }

        [Test]
        public void SpawningNone_IsALegalUpTo_AndLeavesOneBody()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            round.Decline();
            round.Decline();
            round.Decline();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, Origin) }));
        }

        [Test]
        public void SpawningOne_ThenDecliningTheRest_LeavesTwoBodies()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(1, -1));
            round.Decline();
            round.Decline();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 1, 2 }));
        }

        // --- The supply ------------------------------------------------------------------------------

        [Test]
        public void AShortSupply_SpawnsOnlyWhatIsLeft_AndSkipsTheRest()
        {
            var round = new Round(bodiesAt: new[] { H(4, -4), H(-4, 4) });
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(1)),
                "three stand, so one body is left in the supply.");
            round.PickSpaces(H(1, 0));

            Assert.That(round.ActionEnded, Is.True,
                "spawns two and three find the supply empty: a legal empty \"up to\", never a reclaim.");
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, Origin),
                new HeroOccupancy(Razzle, 2, H(4, -4)),
                new HeroOccupancy(Razzle, 3, H(-4, 4)),
                new HeroOccupancy(Razzle, 4, H(1, 0))
            }), "no standing body is taken back to spawn (p.19: never from the board).");
        }

        [Test]
        public void AFullSupplyOnTheBoard_SpawnsNothing_AndTheAttackStillResolves()
        {
            var round = new Round(bodiesAt: new[] { H(4, -4), H(-4, 4), H(4, 0) });
            round.PickActingBody(1);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True,
                "the Attack is judged on its target; the spawn after it can be empty.");
            round.AttackTheMinionBesideHim();

            Assert.That(round.ActionEnded, Is.True, "no spawn question opens with four bodies standing.");
            Assert.That(round.Bodies().Select(body => body.Coordinate),
                Is.EqualTo(new[] { Origin, H(4, -4), H(-4, 4), H(4, 0) }));
        }

        [Test]
        public void ABodyOffTheBoard_IsInTheSupply_AndTheLowestIsSpawnedFirst()
        {
            // Bodies 1 and 3 stand (as after a 9.21e removal); 2 and 4 are in the supply.
            var round = new Round(bodiesAt: new[] { H(-4, 4) }, stagedBodyNumbers: new[] { 3 });
            Assert.That(round.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 2, 4 }));
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(1, -1));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, Origin),
                new HeroOccupancy(Razzle, 2, H(1, 0)),
                new HeroOccupancy(Razzle, 3, H(-4, 4)),
                new HeroOccupancy(Razzle, 4, H(1, -1))
            }));
        }

        // --- The Radius item (R1) --------------------------------------------------------------------

        [Test]
        public void ARadiusItem_WidensTheSpawnToTwoSpaces()
        {
            var round = new Round(radiusItems: 1);
            round.AttackTheMinionBesideHim();

            var offered = round.OfferedSpaces();
            Assert.That(offered, Has.Member(H(2, 0)), "Radius 1 + 1 item reaches two spaces out (p.16).");
            Assert.That(offered.All(at => HexDistanceQuery.Calculate(at, Origin) <= 2), Is.True);
            Assert.That(offered, Has.No.Member(H(3, 0)));
            Assert.That(offered, Is.EquivalentTo(
                round.PlayableWithin(Origin, 2).Where(at => !at.Equals(Origin) && round.IsEmpty(at))));
        }

        [Test]
        public void WithoutTheItem_TwoSpacesOutIsNeverOffered()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(2, 0)));
        }

        // --- The acting body (RZ-Q4, lesson 23) -----------------------------------------------------

        [Test]
        public void TheSpawn_IsMeasuredFromTheBodyThePlayerPicked()
        {
            // Body 2 at (3, -3) stands beside a second enemy minion; body 1 at the origin beside the first.
            var round = new Round(bodiesAt: new[] { H(3, -3) }, enemyMinionsAt: new[] { BesideHim, H(4, -3) });
            round.PickActingBody(2);
            round.StartPrimaryAttack();
            Assert.That(round.LegalTargetMinions(), Is.EqualTo(new[] { Round.EnemyMinion(2) }),
                "the Attack reaches only what is beside the acting body.");
            round.Target(Round.EnemyMinion(2));

            Assert.That(round.OfferedSpaces().All(at => HexDistanceQuery.Calculate(at, H(3, -3)) == 1), Is.True,
                "every space offered is beside body 2.");
            Assert.That(round.OfferedSpaces(), Has.No.Member(H(1, 0)), "beside body 1 only.");
            round.PickSpaces(H(2, -3));
            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(2), "a body spawned during the action never becomes the actor.");
        }

        [Test]
        public void WithTheMinionBesideBodyOneOnly_TheAttackIsGreyedFromBodyTwo_AndOfferedFromBodyOne()
        {
            var fromTwo = new Round(bodiesAt: new[] { H(3, -3) });
            fromTwo.PickActingBody(2);
            Assert.That(fromTwo.IsOffered(RazzleCardContent.PrimaryAttack), Is.False,
                "nothing is adjacent to body 2.");

            var fromOne = new Round(bodiesAt: new[] { H(3, -3) });
            fromOne.PickActingBody(1);
            Assert.That(fromOne.IsOffered(RazzleCardContent.PrimaryAttack), Is.True);
        }

        // --- Offer and answer agree -----------------------------------------------------------------

        [Test]
        public void StuntDoubles_WithNoUnitBesideHim_IsNotOffered()
        {
            Assert.That(new Round(enemyMinionsAt: Array.Empty<HexCoordinate>())
                .IsOffered(RazzleCardContent.PrimaryAttack), Is.False);
        }

        [Test]
        public void EverySpaceTheSpawnOffers_IsAnsweredWithABodyOnThatSpace()
        {
            var round = new Round(radiusItems: 1);
            round.AttackTheMinionBesideHim();
            round.AssertEveryOfferedSpaceIsTaken();
        }

        [Test]
        public void StuntedDoubles_TargetsAHeroOnly()
        {
            var minionOnly = new Round(face: RazzleCardContent.StuntedDoublesDefinition);
            Assert.That(minionOnly.IsOffered(RazzleCardContent.PrimaryAttack), Is.False,
                "\"Target a hero\": the minion beside him is not a target.");

            var heroBeside = new Round(face: RazzleCardContent.StuntedDoublesDefinition, enemyAt: H(1, -1));
            Assert.That(heroBeside.IsOffered(RazzleCardContent.PrimaryAttack), Is.True);
            heroBeside.StartPrimaryAttack();
            Assert.That(heroBeside.LegalTargetMinions(), Is.Empty);
            Assert.That(heroBeside.LegalTargetHeroes(), Is.EqualTo(new[] { Enemy }));
            heroBeside.Target(Enemy);
            heroBeside.DefendWithTheWall();

            Assert.That(heroBeside.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(1)),
                "the same spawn follows the Handicap's Attack.");
            heroBeside.PickSpaces(H(1, 0));
            heroBeside.Decline();
            heroBeside.Decline();
            Assert.That(heroBeside.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 1, 2 }));
        }

        // --- Restore and replay mid-spawn ------------------------------------------------------------

        [Test]
        public void ThePausedFirstSpawn_RestoresAndReplaysToTheSameBytes()
        {
            var round = new Round();
            round.AttackTheMinionBesideHim();
            round.AssertRestoreReplays(H(1, 0));
        }

        [Test]
        public void APausedSecondSpawn_WithABodyAlreadySpawned_RestoresAndReplaysToTheSameBytes()
        {
            var round = new Round(radiusItems: 1);
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(2, 0));
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(2)));
            round.AssertRestoreReplays(H(0, 2));
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { Origin, H(2, 0), H(0, 2) }));
        }

        [Test]
        public void APausedSpawn_RestoredWithAnotherBodyActing_KeepsThatBodyAndItsSpaces()
        {
            var round = new Round(bodiesAt: new[] { H(3, -3) }, enemyMinionsAt: new[] { BesideHim, H(4, -3) });
            round.PickActingBody(2);
            round.StartPrimaryAttack();
            round.Target(Round.EnemyMinion(2));
            round.AssertRestoreReplays(H(2, -2));
        }

        // --- The spawned bodies are the hero (RZ-Q3) ------------------------------------------------

        [Test]
        public void ASpawnedBodyAttackedAndDefeated_RemovesEveryBodyOfHim()
        {
            // The enemy hero waits two spaces out at (2, -1), beside (1, 0) and (1, -1) only.
            var round = new Round(enemyAt: H(2, -1), enemyStrikes: true);
            round.AttackTheMinionBesideHim();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(0, 1));
            round.Decline();
            Assert.That(round.Bodies(), Has.Length.EqualTo(3));

            round.ReleaseTurn();
            round.EnemyStrikes();
            Assert.That(round.LegalTargetHeroBodies(), Is.EqualTo(new[] { AttackTarget.HeroBodyOf(Razzle, 2) }),
                "only the spawned body stands beside the enemy.");
            round.TargetBody(2);
            round.DeclineDefense();

            Assert.That(round.Bodies(), Is.Empty, "\"If defeated, remove all of you\" (RZ-Q3).");
        }

        private static int Secondary(CardDefinition face, ActionKind kind) =>
            face.ActionDefinitions.Single(action => action.Kind == kind &&
                action.Origin != ActionDefinitionOrigin.PrintedPrimary).BaseValues.Single().Amount;

        // --- The harness ----------------------------------------------------------------------------

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("razzle-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static readonly CardDefinition EnemyStrike = new CardDefinition(
            new CardDefinitionId("razzle-enemy-strike"), "rev-1", "TEST - Strike",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("strike"),
                    ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default)
            });

        private static readonly CardDefinition EnemyWall = new CardDefinition(
            new CardDefinitionId("razzle-enemy-wall"), "rev-1", "TEST - Wall",
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

        /// <summary>
        /// One round on the disk with one of Razzle's two faces committed and Razzle selected to act,
        /// his supply of four bodies declared on the board (RZ-Q1).
        /// </summary>
        internal sealed class Round
        {
            internal static MinionId EnemyMinion(int index) => new MinionId($"razzle-blue-melee-{index}");

            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly MatchContentCatalogs catalogs;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly QuickGameMinionSetupState[] minions;

            internal Round(
                CardDefinition face = null,
                IReadOnlyList<HexCoordinate> bodiesAt = null,
                IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
                IReadOnlyList<HexCoordinate> friendlyMinionsAt = null,
                HexCoordinate? enemyAt = null,
                int radiusItems = 0,
                bool enemyStrikes = false,
                IReadOnlyList<int> stagedBodyNumbers = null,
                bool secondCardInHand = false,
                // ===== 9.21g: Razzle's cards in hand (HandCard) and in his discard (DiscardedCard) =====
                IReadOnlyList<CardDefinition> hand = null,
                int discarded = 0,
                // ===== 9.21h: an Ultimate unlocked in his Ultimate area (Twin Strike) =====
                CardDefinition ultimate = null)
            {
                face = face ?? RazzleCardContent.StuntDoublesDefinition;
                var participants = StagedBoardParticipants.Create();
                var heroes = new List<HeroOccupancy> { new HeroOccupancy(Razzle, Origin) };
                var staged = bodiesAt ?? Array.Empty<HexCoordinate>();
                for (var at = 0; at < staged.Count; at++)
                {
                    // Bodies 2, 3, ... in order, unless the case names which bodies stand.
                    heroes.Add(new HeroOccupancy(Razzle, stagedBodyNumbers?[at] ?? at + 2, staged[at]));
                }

                heroes.Add(new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5)));

                var placed = new List<QuickGameMinionSetupState>();
                var index = 0;
                foreach (var at in enemyMinionsAt ?? new[] { BesideHim })
                {
                    placed.Add(Minion(EnemyMinion(++index), StagedBoardParticipants.BlueTeamIdValue,
                        TeamSide.Blue, at));
                }

                index = 0;
                foreach (var at in friendlyMinionsAt ?? Array.Empty<HexCoordinate>())
                {
                    placed.Add(Minion(new MinionId($"razzle-red-melee-{++index}"),
                        StagedBoardParticipants.RedTeamIdValue, TeamSide.Red, at));
                }

                minions = placed.ToArray();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                    Array.Empty<ObstacleOccupancy>(), minions,
                    new[] { new HeroBodySupply(Razzle, 4) });

                source = CardInstance.CreateInHand(new CardInstanceId("razzle-source"), face.Reference, Razzle);
                var enemyCard = CardInstance.CreateInHand(new CardInstanceId("razzle-enemy-card"),
                    (enemyStrikes ? EnemyStrike : EnemyHold).Reference, Enemy);
                var wall = CardInstance.CreateInHand(new CardInstanceId("razzle-enemy-wall"),
                    EnemyWall.Reference, Enemy);
                definitions = new[] { face, EnemyHold, EnemyStrike, EnemyWall };
                // Milestone 9.21e. A second card of his (a plain Movement) so a case reaches his next turn.
                instances = secondCardInHand
                    ? new[] { source, enemyCard, wall,
                        CardInstance.CreateInHand(SecondCard, EnemyHold.Reference, Razzle) }
                    : new[] { source, enemyCard, wall };
                // ===== 9.21g: more of his cards - in hand (a Defense card to answer with) and resting
                // in his discard (plain Movement cards, for a retrieval) =====
                var hands = hand ?? Array.Empty<CardDefinition>();
                definitions = definitions.Concat(hands.Where(value => !definitions.Contains(value))).ToArray();
                instances = instances
                    .Concat(hands.Select((value, at) => CardInstance.CreateInHand(HandCard(at + 1), value.Reference, Razzle)))
                    .Concat(Enumerable.Range(1, discarded).Select(at => new CardInstance(
                        DiscardedCard(at), EnemyHold.Reference, Razzle, CardStateFacts.DiscardedUnplayed())))
                    .ToArray();
                // ===== END 9.21g =====
                // ===== 9.21h: the unlocked Ultimate and its triggered effect =====
                var triggered = new List<EffectDefinition>();
                if (ultimate != null)
                {
                    var ability = CardInstance.CreateInDeck(UltimateCard, ultimate.Reference, Razzle);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ability), Is.True,
                        "p.16 unlocks the Ultimate by moving it out of the deck.");
                    definitions = definitions.Concat(new[] { ultimate }).ToArray();
                    instances = instances.Concat(new[] { ability }).ToArray();
                    triggered.Add(RazzleEffectContent.TwinStrikeTriggeredEffect(ultimate.ContentRevision));
                }
                // ===== END 9.21h =====
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(RazzleEffectContent.Handlers()),
                    new ComponentDefinitionCatalog(
                        Array.Empty<TokenDefinition>(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(triggered));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));
                for (var item = 0; item < radiusItems; item++)
                {
                    Assert.That(round.HeroItems.TryGrantItemFromTheBox(Razzle, PrintedValueChannel.Radius)
                        .IsPlaced, Is.True, "p.16's Radius slot takes the item.");
                }

                Assert.That(round.TryCommitCard(Razzle, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, enemyCard.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Razzle));
            }

            internal AdjacentMoveAuthority Board => board;

            internal TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            // Milestone 9.21e. The removal faces (RazzleRemovalFacesTests) drive the same round.
            internal RoundResolutionAuthority Authority => round;

            internal CardInstanceId Source => source.Id;

            internal static readonly CardInstanceId SecondCard = new CardInstanceId("razzle-second");

            // ===== 9.21h: his unlocked Ultimate =====
            internal static readonly CardInstanceId UltimateCard = new CardInstanceId("razzle-ultimate");

            // ===== 9.21g: the staged hand and discard =====
            internal static CardInstanceId HandCard(int index) => new CardInstanceId($"razzle-hand-{index}");

            internal static CardInstanceId DiscardedCard(int index) => new CardInstanceId($"razzle-discard-{index}");

            internal IReadOnlyList<CardDefinition> Definitions => definitions;

            internal bool HasCard(CardInstanceId card) => instances.Any(value => value.Id.Equals(card));

            internal CardLocation LocationOf(CardInstanceId card) =>
                instances.Single(value => value.Id.Equals(card)).State.Location;
            // ===== END 9.21g =====

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal HeroOccupancy[] Bodies() => board.GetHeroBodies(Razzle).ToArray();

            internal bool IsEmpty(HexCoordinate at) => board.IsEmptyPlayableSpace(at);

            internal IEnumerable<HexCoordinate> PlayableWithin(HexCoordinate from, int radius) =>
                board.BoardCoordinates.Where(at => HexDistanceQuery.Calculate(at, from) <= radius);

            internal HexCoordinate[] OfferedSpaces() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();

            internal MinionId[] LegalTargetMinions() =>
                Turn.PendingAttackTargetChoice?.LegalTargetMinionIds.ToArray() ?? Array.Empty<MinionId>();

            internal HeroId[] LegalTargetHeroes() =>
                Turn.PendingAttackTargetChoice?.LegalTargetHeroIds.ToArray() ?? Array.Empty<HeroId>();

            internal AttackTarget[] LegalTargetHeroBodies() =>
                Turn.PendingAttackTargetChoice.LegalTargets
                    .Where(target => target.Kind == AttackTargetKind.Hero).ToArray();


            internal void PickActingBody(int body)
            {
                var pick = Turn.PendingActingBodyChoice;
                Assert.That(pick, Is.Not.Null, "several bodies stand, so \"which of you acts\" is asked first.");
                Assert.That(round.TryAnswerActingBody(new ActingBodyCommand(Razzle, source.Id, body)).Accepted,
                    Is.True);
            }

            private ActionOffer Offer(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Single(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.SourceCardInstanceId.Equals(source.Id));

            internal bool IsOffered(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.SourceCardInstanceId.Equals(source.Id) &&
                    offer.IsAvailable);

            internal void StartPrimaryAttack() =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("razzle-attack"),
                    Offer(RazzleCardContent.PrimaryAttack)).Accepted, Is.True);

            internal void Target(MinionId minion)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "p.14's target question is open");
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, minion)).Accepted, Is.True);
            }

            internal void Target(HeroId hero)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "p.14's target question is open");
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, hero)).Accepted, Is.True);
            }

            internal void TargetBody(int body)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "p.14's target question is open");
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, AttackTarget.HeroBodyOf(Razzle, body))).Accepted,
                    Is.True);
            }

            /// <summary>Stunt Doubles on the minion beside body 1, which the Attack defeats or not.</summary>
            internal void AttackTheMinionBesideHim()
            {
                StartPrimaryAttack();
                Target(EnemyMinion(1));
            }

            internal void DefendWithTheWall()
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null, "p.14's Defense question is open");
                var wall = defense.LegalDefenseOffers.First(offer =>
                    offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
                Assert.That(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted, Is.True);
            }

            internal void DeclineDefense()
            {
                var defense = Turn.PendingDefenseChoice;
                if (defense == null)
                {
                    return;
                }

                Assert.That(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.Decline(
                    defense.ResolutionId, defense.ChoiceId)).Accepted, Is.True);
            }

            internal void ReleaseTurn()
            {
                Assert.That(Turn.PendingTurnRelease, Is.Not.Null);
                Assert.That(round.TryReleaseTurn().Accepted, Is.True);
            }

            internal void EnemyStrikes()
            {
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
                var offer = Turn.GetTurnActionOffers().Single(value =>
                    value.ActionDefinitionId.Value == "strike" && value.IsAvailable);
                Assert.That(round.TryStartAttack(new ActionResolutionId("enemy-strike"), offer).Accepted,
                    Is.True);
            }

            internal void PickSpaces(params HexCoordinate[] spaces)
            {
                Assert.That(Turn.PendingEffectChoice, Is.Not.Null);
                AnswerSpaces(round, spaces);
            }

            /// <summary>Milestone 9.21e. Crowd Control's Skill primary.</summary>
            internal void StartSkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("razzle-skill"),
                    Offer(RazzleCardContent.PrimarySkill)).Accepted, Is.True);

            /// <summary>Milestone 9.21e. One body of a many-body removal, without closing it.</summary>
            internal void Select(HexCoordinate space)
            {
                var asked = Turn.PendingEffectChoice;
                Assert.That(asked, Is.Not.Null);
                Assert.That(round.TryAnswerEffectChoice(asked.SelectInvocations.Single(
                    invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space)))).Accepted,
                    Is.True);
            }

            /// <summary>Milestone 9.21e. Closes a many-body removal with the bodies chosen.</summary>
            internal void Confirm()
            {
                var asked = Turn.PendingEffectChoice;
                Assert.That(asked?.ConfirmInvocation, Is.Not.Null);
                Assert.That(round.TryAnswerEffectChoice(asked.ConfirmInvocation).Accepted, Is.True);
            }

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an \"up to\" spawn can be declined");
                Assert.That(round.TryAnswerEffectChoice(choice.DeclineInvocation).Accepted, Is.True);
            }

            /// <summary>
            /// Every space the open spawn offers, answered on a restored copy: each is accepted and puts
            /// the next body exactly there.
            /// </summary>
            internal void AssertEveryOfferedSpaceIsTaken()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var offered = OfferedSpaces();
                Assert.That(offered, Is.Not.Empty);
                foreach (var space in offered)
                {
                    var copy = Restore(bytes);
                    var before = copy.Board.GetHeroBodies(Razzle).Count;
                    AnswerSpaces(copy.Round, new[] { space });
                    var bodies = copy.Board.GetHeroBodies(Razzle);
                    Assert.That(bodies.Count, Is.EqualTo(before + 1), $"{space} spawns a body");
                    Assert.That(bodies.Any(body => body.Coordinate.Equals(space)), Is.True, $"{space} holds it");
                }
            }

            /// <summary>
            /// Snapshot at the open spawn, restore it, answer both the same way, and require the same
            /// question and the same authoritative bytes - then once more from the retained prefix.
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
                Assert.That(restored.Board.GetHeroBodies(Razzle), Is.EqualTo(board.GetHeroBodies(Razzle)));
                Assert.That(restored.Board.HeroBodySupplies, Is.EqualTo(board.HeroBodySupplies));
                Assert.That(restored.Board.ActingBodies, Is.EqualTo(board.ActingBodies));

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

            internal RestoredAuthoritativeMatch Restore(string bytes) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

            private static void AnswerSpaces(RoundResolutionAuthority target, IEnumerable<HexCoordinate> spaces)
            {
                foreach (var space in spaces)
                {
                    var asked = target.CurrentTurnAuthority.PendingEffectChoice;
                    Assert.That(target.TryAnswerEffectChoice(asked.SelectInvocations.Single(
                        invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space)))).Accepted,
                        Is.True);
                    var still = target.CurrentTurnAuthority.PendingEffectChoice;
                    if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
                    {
                        Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                    }
                }
            }

            private static QuickGameMinionSetupState Minion(
                MinionId id, string team, TeamSide side, HexCoordinate at) =>
                new QuickGameMinionSetupState(
                    id, new TeamId(team), side, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(Map, new SpawnPointId($"spawn-{id.Value}")),
                    new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                    at);
        }
    }
}

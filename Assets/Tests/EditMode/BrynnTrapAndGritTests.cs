using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
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
    /// Milestone 8.69e. Brynn the Seeker's five GREEN faces: Bear Trap, Log Trap and Deadfall Trap,
    /// which make one enemy hero give a card up, and True Grit and Die Hard, which let him take a card
    /// back and walk.
    ///
    /// <para><b>What the five have in common and where they part.</b> All three traps print one modal
    /// with two bullets, and the traps differ from each other in exactly one printed phrase - "if able"
    /// against "or is defeated" - which is the pair <c>synthetic-discard-chosen-card</c>'s two live
    /// revisions were minted for at milestone 8.10. The two grit faces print two independent sentences
    /// and differ in one number. Every case below is written so that the only thing separating a pass
    /// from a fail is something the printed text says.</para>
    ///
    /// <para><b>Two probes, because the block has two kinds of question.</b> Which candidates a
    /// selection offers is a question about one node on one board, so it is asked of the composed node's
    /// OWN declared inputs through the registered handler - which is what lets a Radius item, a
    /// counts-as overlay and an immunity be varied one at a time. What a card DOES - a hand emptied, a
    /// hero defeated with p.13's coins, a card taken back, a walk of nought to four - is a question
    /// about a turn, so it is driven through a real <see cref="RoundResolutionAuthority"/> with the
    /// Skill actually played.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class BrynnTrapAndGritTests
    {
        private static readonly HeroId Brynn = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("brynn-trap-and-grit");
        private static readonly MinionId Minion = new MinionId("brynn-trap-minion");

        private static readonly HexCoordinate BrynnAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent to Brynn, and standing beside nothing else - bullet 1's only subject.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        /// <summary>Inside a printed Radius 3, boxed in - bullet 2's only subject.</summary>
        private static readonly HexCoordinate SecondEnemyNear = new HexCoordinate(0, 2);

        /// <summary>Four spaces out: outside Radius 3, inside Radius 4 and inside 3 plus an item.</summary>
        private static readonly HexCoordinate SecondEnemyFar = new HexCoordinate(0, 4);

        /// <summary>Adjacent to Brynn and on no bullet's list - p.19's other kind of unit.</summary>
        private static readonly HexCoordinate MinionAt = new HexCoordinate(-1, 0);

        // -------------------------------------------------------------------------------------------
        // The printed set
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The five faces as the shard's printed-inventory table states them, and the whole Green
        /// ladder p.15 forks twice. Literal rather than derived, for the reason every verified set's
        /// table is literal: a case that read its expectations out of the definition would pass
        /// whatever the definition happened to say.
        /// </summary>
        [Test]
        public void TheFiveGreenFaces_CarryThePrintedValuesAndTheWholeLadder()
        {
            foreach (var expected in new[]
            {
                new object[] { BrynnCardContent.BearTrap, 3, CardTier.I, 3, 3, PrintedValueChannel.None },
                new object[]
                {
                    BrynnCardContent.LogTrap, 2, CardTier.II, 4, 3, PrintedValueChannel.Initiative
                },
                new object[]
                {
                    BrynnCardContent.TrueGrit, 2, CardTier.II, 4, 3, PrintedValueChannel.Attack
                },
                new object[]
                {
                    BrynnCardContent.DeadfallTrap, 2, CardTier.III, 4, 4,
                    PrintedValueChannel.Initiative
                },
                new object[]
                {
                    BrynnCardContent.DieHard, 2, CardTier.III, 4, 4, PrintedValueChannel.Movement
                }
            })
            {
                var face = Face((CardDefinitionId)expected[0]);
                var name = face.DisplayName;

                Assert.That(face.Initiative.Value, Is.EqualTo((int)expected[1]), name);
                Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Green), name);
                Assert.That(face.Progression.Tier, Is.EqualTo((CardTier)expected[2]), name);
                Assert.That(
                    PrintedValue(face, ActionKind.Skill, PrintedValueChannel.Radius),
                    Is.EqualTo((int)expected[3]), name + " prints one Radius icon");
                Assert.That(
                    PrintedValue(face, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo((int)expected[4]), name);
                Assert.That(
                    PrintedValue(face, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo(2), name + " - every Green face of his prints Movement 2");
                Assert.That(
                    face.Progression.ItemIcon, Is.EqualTo((PrintedValueChannel)expected[5]), name);
                Assert.That(
                    face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill).AttackRange,
                    Is.EqualTo(AttackRangeKind.None),
                    name + " is a Skill and prints no Attack band at all");
            }

            Assert.That(
                Face(BrynnCardContent.BearTrap).Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { BrynnCardContent.LogTrap, BrynnCardContent.TrueGrit }),
                "p.15 forks Green at Tier I, and BOTH halves are this block's faces - which is why " +
                "this ladder is stated whole where 8.69a left the Red one half-stated.");
            foreach (var tierTwo in new[] { BrynnCardContent.LogTrap, BrynnCardContent.TrueGrit })
            {
                Assert.That(
                    Face(tierTwo).Progression.NextTierCandidateIds,
                    Is.EquivalentTo(new[] { BrynnCardContent.DeadfallTrap, BrynnCardContent.DieHard }),
                    "and forks it again at Tier II - either Tier II climbs to either Tier III.");
            }

            Assert.That(
                Face(BrynnCardContent.DeadfallTrap).Progression.NextTierCandidateIds, Is.Empty);
            Assert.That(Face(BrynnCardContent.DieHard).Progression.NextTierCandidateIds, Is.Empty);

            Assert.That(
                Face(BrynnCardContent.LogTrap).SourceContent.PrintedText,
                Is.EqualTo(Face(BrynnCardContent.BearTrap).SourceContent.PrintedText),
                "Log Trap prints Bear Trap's sentence exactly; only the icons differ.");
            Assert.That(
                Face(BrynnCardContent.DeadfallTrap).SourceContent.PrintedText,
                Does.Contain("or is defeated").And.Not.Contain("if able"),
                "and the Tier III trap prints the escalation in place of p.17's \"if able\".");
        }

        /// <summary>
        /// Every body this block composes is a definition the bounded catalog admits - the same walk
        /// the dealt-corpus validator performs, run here because 8.69g is what registers his contracts
        /// and nothing before it can reach that validator.
        /// </summary>
        [Test]
        public void EveryComposedBody_ValidatesAgainstTheBoundedCatalog()
        {
            var capabilities = Catalogs().Capabilities;

            foreach (var face in BrynnCardContent.TrapAndGritDefinitions)
            {
                var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                Assert.That(skill.EffectRoot, Is.Not.Null, face.DisplayName);
                Assert.That(
                    EffectDefinitionValidator.Validate(skill.EffectRoot, capabilities), Is.Empty,
                    face.DisplayName);
                Assert.That(
                    skill.EffectRoot.Reference.ContentRevision,
                    Is.EqualTo(BrynnComponentContent.TrapPrepRevision),
                    face.DisplayName + " carries this block's own placeholder revision until 8.69f.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The modal and its two bullets
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.10's modal, read off the composed definition: 1..1, the actor's decision, two arms, and
        /// only the second arm's selection carrying the printed obstacle clause.
        /// </summary>
        [Test]
        public void TheTrapModal_IsOneOfTwo_AndOnlyTheSecondBulletNamesTheObstacles()
        {
            foreach (var trap in new[]
            {
                BrynnCardContent.BearTrap, BrynnCardContent.LogTrap, BrynnCardContent.DeadfallTrap
            })
            {
                var body = SkillBody(trap);
                var modal = Node(body, BrynnEffectContent.TrapModalNode);

                Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose));
                Assert.That(modal.MinimumSelections, Is.EqualTo(1));
                Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                    "\"Choose one\" is 1..1 - one bullet is taken and the other ignored (BR-Q11).");
                Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                    "p.10 gives an unassigned decision to the card's controller.");
                Assert.That(
                    modal.ChildNodeIds.Select(child => child.Value).ToArray(),
                    Is.EqualTo(new[]
                    {
                        BrynnEffectContent.TrapAdjacentArmNode, BrynnEffectContent.TrapRadiusArmNode
                    }),
                    "in the order the card prints its bullets.");

                var adjacent = Node(body, BrynnEffectContent.TrapAdjacentSelectNode);
                Assert.That(adjacent.Capability, Is.EqualTo(SyntheticRosterCapabilities.ChooseTargetV2),
                    "\"An enemy hero adjacent to you\" is the selection Misa's Power Overwhelming " +
                    "already reaches for the identical printed phrase.");
                Assert.That(
                    Literal(adjacent, SyntheticRosterCapabilities.DistanceParameter).IntegerValue,
                    Is.EqualTo(1),
                    "\"adjacent\" is p.17's innermost ring, written by the definition rather than " +
                    "read off an icon the card does not print for it.");
                Assert.That(
                    Literal(adjacent, SyntheticRosterCapabilities.UnitKindParameter).IntegerValue,
                    Is.EqualTo((int)RosterUnitKind.Hero),
                    "\"an enemy HERO\" - p.19's other kind of unit is not a subject of this clause.");
                Assert.That(
                    adjacent.Inputs.Any(input => input.ParameterName ==
                        SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName),
                    Is.False,
                    "and bullet 1 prints no obstacle clause at all.");

                var radius = Node(body, BrynnEffectContent.TrapRadiusSelectNode);
                Assert.That(radius.Capability,
                    Is.EqualTo(SyntheticTargetingCapabilities.RadiusHeroTargetV1),
                    "\"in radius\" is p.9's printed icon, which is what makes his Radius item matter.");
                Assert.That(
                    Literal(
                        radius,
                        SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName)
                        .BooleanValue,
                    Is.True,
                    "\"who is adjacent to 3 or more obstacles\" is 8.69a's flag, which is the ONE " +
                    "place the printed phrase is answered for a selection - so his Ultimate reaches " +
                    "this bullet without this card knowing an Ultimate exists.");
                Assert.That(
                    Literal(radius, SyntheticTargetingCapabilities.ExcludeAdjacentParameterName)
                        .BooleanValue,
                    Is.False,
                    "and the sentence prints no \"not adjacent to you\", so nothing is taken out of " +
                    "the disk's innermost ring.");

                foreach (var select in new[] { adjacent, radius })
                {
                    Assert.That(
                        Literal(select, "activity").IntegerValue, Is.EqualTo((int)RuleActivity.Skill),
                        "Both bullets TARGET (p.19: an effect on another unit targets it), so both " +
                        "declare the activity p.18 asks immunity about - and these faces are Skills.");
                    Assert.That(
                        select.Inputs.Any(input => input.ParameterName ==
                            EffectSelectionDeclaration.ConsumerParameterName),
                        Is.False,
                        "and NEITHER declares its discard as a consumer (BR-Q17e): both bullets " +
                        "print one clause, so both must offer one set, and \"if able\" is written " +
                        "precisely because a hero with nothing to give is a legal pick.");
                }
            }
        }

        /// <summary>
        /// Bullet 1's candidates, on a board staged so that every refusal is a different rule: one
        /// adjacent enemy hero is offered, the enemy MINION beside him is not, the enemy hero two
        /// spaces out is not, and Brynn is never his own subject.
        /// </summary>
        [Test]
        public void BulletOne_OffersTheAdjacentEnemyHeroAlone_AndNeitherAMinionNorBrynnHimself()
        {
            var board = Board(SecondEnemyNear);

            Assert.That(
                Offered(BrynnCardContent.BearTrap, BrynnEffectContent.TrapAdjacentSelectNode, board),
                Is.EqualTo(new[] { Enemy.Value }),
                "\"An enemy hero adjacent to you\": the one enemy hero standing in p.17's innermost " +
                "ring. The enemy minion beside him is a unit and not a hero; the second enemy hero " +
                "is two spaces out; and Brynn is on no side opposing himself.");

            Assert.That(
                OfferedObjects(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapAdjacentSelectNode, board)
                    .Any(candidate => candidate.Kind == BoardObjectKind.Minion),
                Is.False,
                "and no minion is a candidate at all, which is what unitKind Hero means.");
        }

        /// <summary>
        /// Bullet 2's candidates: the narrowing, measured as a difference on ONE board. Both enemy
        /// heroes stand inside the printed disk and only the boxed-in one is offered.
        /// </summary>
        [Test]
        public void BulletTwo_OffersTheBoxedInEnemyHero_AndRefusesTheOneStandingInTheOpen()
        {
            var board = Board(SecondEnemyNear);

            Assert.That(
                HeroesInTheDisk(board), Is.EquivalentTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "Both enemy heroes stand inside Bear Trap's printed Radius 3, which is what makes " +
                "the case below about the printed clause and about nothing else.");

            Assert.That(
                Offered(BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, board),
                Is.EqualTo(new[] { SecondEnemy.Value }),
                "\"who is adjacent to 3 or more obstacles\" - three terrain spaces beside one of " +
                "them, one hero beside the other, and only the first is offered. The narrowing " +
                "decides who may be PICKED, so the hero in the open is never offered rather than " +
                "offered and then refused.");
        }

        /// <summary>
        /// p.18's two answers about an immune hero, on one board and in one case, because the card
        /// asks both at once: an immune hero is COUNTED by the clause that narrows the disk (a
        /// presence check targets nobody, p.19) and cannot BE the subject the discard is performed on.
        /// </summary>
        [Test]
        public void AnImmuneEnemyHeroIsNoTrapSubject_ThoughImmunityNeverEntersTheNarrowing()
        {
            var board = Board(SecondEnemyNear);
            var immune = Immunity(SecondEnemy);

            Assert.That(
                Offered(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, board,
                    modifications: immune),
                Is.Empty,
                "The only hero the clause admits is immune, and a discard is an effect on the hero " +
                "it takes a card from - p.19's own definition of targeting - so the bullet names " +
                "nobody.");

            Assert.That(
                Offered(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapAdjacentSelectNode, board,
                    modifications: Immunity(Enemy)),
                Is.Empty,
                "and bullet 1 refuses an immune subject on the same terms, because it prints the " +
                "same verb.");

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(board, SecondEnemy, immune),
                Is.True,
                "while the clause that NARROWS the disk still counts them exactly as before: p.18 " +
                "counts immune units in a presence check, and BR-Q1 is a presence check.");
        }

        /// <summary>
        /// "in radius" is the performing card's own printed icon with p.16's items composed onto it -
        /// so the same board answers differently for Bear Trap's 3, Log Trap's 4, and Bear Trap's 3
        /// with one Radius item on the dashboard.
        /// </summary>
        [Test]
        public void ThePrintedRadiusIsTheCardsOwn_AndAP16ItemWidensBulletTwo()
        {
            var board = Board(SecondEnemyFar);

            Assert.That(
                Offered(BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, board),
                Is.Empty,
                "The one hero the clause admits stands four spaces out, and Bear Trap prints " +
                "Radius 3.");
            Assert.That(
                Offered(BrynnCardContent.LogTrap, BrynnEffectContent.TrapRadiusSelectNode, board),
                Is.EqualTo(new[] { SecondEnemy.Value }),
                "Log Trap prints the identical sentence over a Radius 4 icon, and that is the whole " +
                "difference between the two faces' offers.");
            Assert.That(
                Offered(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, board,
                    withRadiusItem: true),
                Is.EqualTo(new[] { SecondEnemy.Value }),
                "and p.16's Radius item raises Bear Trap's own icon by one, which reaches him - the " +
                "same composition every printed-icon consumer performs.");
        }

        /// <summary>
        /// Over the Top through the trap. On a board with no obstacle on it at all, every enemy hero
        /// in the disk becomes a legal subject of bullet 2 while Brynn acts - and this card names no
        /// Ultimate, because the overlay lives inside the one query the flag asks.
        /// </summary>
        [Test]
        public void OverTheTop_WidensBulletTwoToEveryEnemyHeroInTheDisk()
        {
            var open = OpenBoard();

            Assert.That(
                Offered(BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, open),
                Is.Empty,
                "Nothing stands beside anybody on this board, so the printed clause admits nobody.");

            Assert.That(
                Offered(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapRadiusSelectNode, open,
                    modifications: UnlockedUltimate(open)),
                Is.EquivalentTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "\"While you are performing actions all enemy heroes in play count as adjacent to 3 " +
                "or more obstacles\" - the identical board and the identical card, with the Purple " +
                "face unlocked and Brynn the acting object.");

            Assert.That(
                Offered(
                    BrynnCardContent.BearTrap, BrynnEffectContent.TrapAdjacentSelectNode, open,
                    modifications: UnlockedUltimate(open)),
                Is.EqualTo(
                    Offered(BrynnCardContent.BearTrap, BrynnEffectContent.TrapAdjacentSelectNode, open)),
                "and bullet 1 offers exactly what it offered without the Ultimate - that bullet's " +
                "clause is a DISTANCE rather than the printed phrase the overlay answers, so a " +
                "counts-as about obstacles says nothing at all about it.");
        }

        // -------------------------------------------------------------------------------------------
        // What the traps do, driven through a turn
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole of Bear Trap's first bullet, played: Brynn picks the bullet, Brynn picks the hero,
        /// and THAT HERO picks the card - three decisions across two seats, which is the shape
        /// <c>synthetic-discard-chosen-card</c> exists for.
        /// </summary>
        [Test]
        public void BearTrap_TakesOneCard_AndTheChosenHeroPicksItOnTheirOwnSeat()
        {
            var turn = new Turn(BrynnCardContent.BearTrap);

            Assert.That(turn.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.TrapModalNode));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(Brynn));
            turn.Option(BrynnEffectContent.TrapAdjacentArmNode);

            Assert.That(
                turn.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.TrapAdjacentSelectNode));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(Brynn),
                "\"An enemy hero\" is an unassigned decision, which p.10 gives to the controller.");
            turn.Pick(EffectValue.Hero(Enemy));

            Assert.That(
                turn.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.TrapAdjacentDiscardNode));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(Enemy),
                "and which card leaves the hand is the discarding hero's own decision, on their own " +
                "seat - which is the fact that closes the 8.42e seat rewind over this pause.");
            Assert.That(turn.Choice.ChooserId, Is.Not.EqualTo(Brynn));

            var before = turn.HandCount(Enemy);
            turn.Pick(turn.Choice.Candidates[0].Value);

            Assert.That(turn.HandCount(Enemy), Is.EqualTo(before - 1));
            Assert.That(turn.OnBoard(Enemy), Is.True,
                "\"if able\" ends the sentence there: nothing else happens to a hero who paid.");
            Assert.That(turn.Choice, Is.Null, "and the card is finished.");
        }

        /// <summary>
        /// The other bullet of the same card, so that the modal is measured as a modal: taking bullet 2
        /// reaches the OTHER hero and the first bullet is ignored entirely.
        /// </summary>
        [Test]
        public void BearTrap_TakingTheSecondBulletReachesTheBoxedInHero_AndIgnoresTheFirst()
        {
            var turn = new Turn(BrynnCardContent.BearTrap);
            turn.Option(BrynnEffectContent.TrapRadiusArmNode);

            Assert.That(
                turn.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.TrapRadiusSelectNode));
            Assert.That(
                turn.Choice.Candidates.Select(candidate => candidate.Value.HeroId.Value).ToArray(),
                Is.EqualTo(new[] { SecondEnemy.Value }));
            turn.Pick(EffectValue.Hero(SecondEnemy));

            var enemyBefore = turn.HandCount(Enemy);
            var secondBefore = turn.HandCount(SecondEnemy);
            turn.Pick(turn.Choice.Candidates[0].Value);

            Assert.That(turn.HandCount(SecondEnemy), Is.EqualTo(secondBefore - 1));
            Assert.That(turn.HandCount(Enemy), Is.EqualTo(enemyBefore),
                "p.10's own word for the bullet not chosen is \"ignored\", and the adjacent hero " +
                "keeps every card.");
        }

        /// <summary>
        /// p.17's "if able" with nothing to give. The empty-handed hero IS offered - the printed clause
        /// says nothing about hands - and giving nothing is the whole of what happens (BR-Q17e).
        /// </summary>
        [Test]
        public void BearTrap_AnEmptyHandedHeroIsStillOffered_AndGivesNothing()
        {
            var turn = new Turn(BrynnCardContent.BearTrap, enemyHasSpareCards: false);
            turn.Option(BrynnEffectContent.TrapAdjacentArmNode);

            Assert.That(
                turn.Choice.Candidates.Select(candidate => candidate.Value.HeroId.Value).ToArray(),
                Is.EqualTo(new[] { Enemy.Value }),
                "\"if able\" is written because an empty-handed hero is a legal pick; a build that " +
                "hid them would be reading a clause the card does not print.");
            Assert.That(turn.HandCount(Enemy), Is.Zero);

            turn.Pick(EffectValue.Hero(Enemy));

            Assert.That(turn.Choice, Is.Null,
                "Nothing is asked of a hero with nothing to give - the step declines and the card " +
                "is over.");
            Assert.That(turn.OnBoard(Enemy), Is.True, "and Bear Trap prints no second arm.");
            Assert.That(turn.HandCount(Enemy), Is.Zero);
        }

        /// <summary>
        /// Deadfall Trap's escalation - one mandatory step with two ways to complete. The hero who
        /// cannot pay is defeated by p.13's own Defeat, which means the coins and the scoreboard's kill
        /// as well as the miniature.
        /// </summary>
        [Test]
        public void DeadfallTrap_DefeatsTheHeroWhoCannotDiscard_WithP13sCoinsAndTheScoreboardsKill()
        {
            var turn = new Turn(BrynnCardContent.DeadfallTrap, enemyHasSpareCards: false);
            var coins = turn.Coins(Brynn);
            var kills = turn.Kills(Brynn);

            turn.Option(BrynnEffectContent.TrapAdjacentArmNode);
            Assert.That(
                turn.Choice.Candidates.Select(candidate => candidate.Value.HeroId.Value).ToArray(),
                Is.EqualTo(new[] { Enemy.Value }),
                "An empty-handed hero is the candidate the second half of the sentence exists for, " +
                "so the selection must offer them - milestone 8.66's rule, which is why this face " +
                "declares no consumer.");

            turn.Pick(EffectValue.Hero(Enemy));

            Assert.That(turn.Choice, Is.Null,
                "There is no card to choose between, so the discard applies without asking and " +
                "reports the inability the escalation reads.");
            Assert.That(turn.OnBoard(Enemy), Is.False, "p.13's Defeat removes the miniature,");
            Assert.That(turn.Coins(Brynn), Is.GreaterThan(coins),
                "the defeating hero collects p.13's coins,");
            Assert.That(turn.Kills(Brynn), Is.EqualTo(kills + 1),
                "and the 8.53 scoreboard records the kill.");
        }

        /// <summary>
        /// The other half of that pair, and the reason the escalation is not a choice: a hero who CAN
        /// discard does, and stands.
        /// </summary>
        [Test]
        public void DeadfallTrap_LeavesAHeroWhoCanPayStandingAndOneCardLighter()
        {
            var turn = new Turn(BrynnCardContent.DeadfallTrap);
            var kills = turn.Kills(Brynn);

            turn.Option(BrynnEffectContent.TrapAdjacentArmNode);
            turn.Pick(EffectValue.Hero(Enemy));

            Assert.That(turn.Choice.NodeId.Value,
                Is.EqualTo(BrynnEffectContent.TrapAdjacentDiscardNode));
            var before = turn.HandCount(Enemy);
            turn.Pick(turn.Choice.Candidates[0].Value);

            Assert.That(turn.HandCount(Enemy), Is.EqualTo(before - 1));
            Assert.That(turn.OnBoard(Enemy), Is.True,
                "\"discards a card, OR is defeated\" - only the hero who cannot pay is defeated.");
            Assert.That(turn.Kills(Brynn), Is.EqualTo(kills));
        }

        /// <summary>
        /// The pause the other seat owns survives a snapshot: restored, it is the same question with
        /// the same chooser and the same candidates, and answering both leaves two identical
        /// authoritative payloads - which is what an Undo re-runs.
        /// </summary>
        [Test]
        public void TheDiscardPauseRestoresAndReplaysToTheSameAuthoritativeState()
        {
            var turn = new Turn(BrynnCardContent.BearTrap);
            turn.Option(BrynnEffectContent.TrapAdjacentArmNode);
            turn.Pick(EffectValue.Hero(Enemy));

            var standing = turn.Choice;
            Assert.That(standing.ChooserId, Is.EqualTo(Enemy));

            turn.RoundTrip();

            Assert.That(turn.Choice, Is.Not.Null);
            Assert.That(turn.Choice.NodeId, Is.EqualTo(standing.NodeId));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(standing.ChooserId));
            Assert.That(turn.Choice.Candidates.Count, Is.EqualTo(standing.Candidates.Count),
                "The candidate set is DERIVED again on restore rather than carried, so a card that " +
                "left the hand in between would not be offered on the strength of a stale offer.");

            turn.Pick(turn.Choice.Candidates[0].Value);
            Assert.That(turn.Choice, Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // True Grit and Die Hard
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two printed sentences, read off the composed body: an optional retrieval that says what
        /// it retrieves, a presence question that targets nobody, and a mandatory move whose distance is
        /// the optional part.
        /// </summary>
        [Test]
        public void TheGritBody_IsAnOptionalRetrievalAndAMandatoryMoveOfOptionalLength()
        {
            foreach (var pair in new[]
            {
                new object[] { BrynnCardContent.TrueGrit, 3 },
                new object[] { BrynnCardContent.DieHard, 4 }
            })
            {
                var body = SkillBody((CardDefinitionId)pair[0]);

                var root = Node(body, BrynnEffectContent.GritRootNode);
                Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Sequence));
                Assert.That(
                    root.ChildNodeIds.Select(child => child.Value).ToArray(),
                    Is.EqualTo(new[]
                    {
                        BrynnEffectContent.GritRetrieveNode, BrynnEffectContent.GritPresenceNode,
                        BrynnEffectContent.GritGateNode
                    }),
                    "Two independent sentences in printed order - the move is NOT gated on the " +
                    "retrieval, which is BR-Q8's reading and the reason these are siblings.");

                var retrieve = Node(body, BrynnEffectContent.GritRetrieveNode);
                Assert.That(retrieve.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                    "\"You MAY retrieve\" is the step's own optionality.");
                Assert.That(retrieve.Capability, Is.EqualTo(SyntheticRosterCapabilities.RetrieveCardV1));
                Assert.That(
                    Literal(retrieve, SyntheticRosterCapabilities.AttackCardsOnlyParameter)
                        .BooleanValue,
                    Is.True,
                    "\"a discarded ATTACK card\" is p.9's noun, bound as this milestone's filter.");
                Assert.That(
                    Literal(retrieve, "kind").IntegerValue,
                    Is.EqualTo((int)RosterCardRetrievalKind.OwnedDiscardedCard),
                    "and it comes out of the discard pile.");
                foreach (var role in new[] { "owner", "chooser" })
                {
                    Assert.That(
                        retrieve.Inputs.Single(input => input.ParameterName == role).Role,
                        Is.EqualTo(EffectRoleKind.Actor),
                        "p.10: \"you can never retrieve another hero's cards\", so ONE source " +
                        "supplies both the pile and the choice, and it is the acting hero.");
                }

                Assert.That(
                    Node(body, BrynnEffectContent.GritPresenceNode).Capability,
                    Is.EqualTo(BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1),
                    "the presence predicate is 8.69a's, asked rather than re-derived,");

                var move = Node(body, BrynnEffectContent.GritMoveNode);
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    "and \"move up to N spaces\" is mandatory with the DISTANCE optional - 8.21's " +
                    "distinction, and never a step that may be declined.");
                Assert.That(Literal(move, "distance").IntegerValue, Is.EqualTo((int)pair[1]));
            }
        }

        /// <summary>
        /// p.9's "Attack card", measured as a candidate list. His Gold basic and his Red both print an
        /// Attack primary and are offered; a Green and a Blue print a Skill primary and are not; a card
        /// still in his hand is not in the discard; and another hero's discarded Attack card is never
        /// his to take.
        /// </summary>
        [Test]
        public void TheRetrieval_OffersOnlyHisOwnDiscardedAttackCards()
        {
            var turn = new Turn(BrynnCardContent.TrueGrit);

            Assert.That(turn.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.GritRetrieveNode));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(Brynn));
            Assert.That(
                turn.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { "brynn-discarded-gold", "brynn-discarded-red" }),
                "Familiar Ground prints \"BASIC ATTACK - RANGED\" and High Ground prints an Attack, " +
                "so p.9 calls both Attack cards - the Ranged subtype classifies the reach and not " +
                "the action. His discarded Green and the Blue beside it print Skill primaries, the " +
                "Attack card still in his hand is not in the discard, and the enemy's discarded " +
                "Attack card is one p.10 says he can never retrieve.");
        }

        /// <summary>
        /// "You may" is his to decline, and a discard holding no Attack card at all offers nothing
        /// rather than refusing the card - the second sentence still runs either way.
        /// </summary>
        [Test]
        public void TheRetrievalIsDeclinable_AndADiscardWithNoAttackCardOffersNothing()
        {
            var declined = new Turn(BrynnCardContent.TrueGrit);
            Assert.That(declined.Choice.DeclineInvocation, Is.Not.Null,
                "\"You may\" opens a decline the controller owns.");
            declined.Answer(declined.Choice.DeclineInvocation);
            Assert.That(declined.DiscardCount(Brynn), Is.EqualTo(4),
                "and declining leaves every card where it was.");

            var empty = new Turn(BrynnCardContent.TrueGrit, brynnHasDiscardedAttackCards: false);
            Assert.That(empty.Choice, Is.Not.Null);
            Assert.That(empty.Choice.NodeId.Value, Is.Not.EqualTo(BrynnEffectContent.GritRetrieveNode),
                "A discard with nothing the clause admits is nothing to offer, and \"may\" turns " +
                "that into a step not taken rather than a card that cannot be played.");
        }

        /// <summary>
        /// The retrieval performed: the named card leaves the discard for his hand, and the second
        /// sentence runs regardless.
        /// </summary>
        [Test]
        public void TheRetrieval_TakesTheNamedCardBackIntoHisHand()
        {
            var turn = new Turn(BrynnCardContent.TrueGrit);
            var hand = turn.HandCount(Brynn);

            turn.Pick(EffectValue.Card(new CardInstanceId("brynn-discarded-red")));

            Assert.That(turn.HandCount(Brynn), Is.EqualTo(hand + 1));
            Assert.That(turn.DiscardCount(Brynn), Is.EqualTo(3));
            Assert.That(
                turn.Card(new CardInstanceId("brynn-discarded-red")).State.Location,
                Is.EqualTo(CardLocation.Hand));
        }

        /// <summary>
        /// The second sentence's condition, both ways round on two boards that differ in one thing:
        /// with a qualifying enemy hero in the disk the move is asked, and without one the card ends
        /// after the retrieval. The question is a PRESENCE check (p.19), so nothing is targeted by
        /// asking it.
        /// </summary>
        [Test]
        public void TheMove_IsAskedOnlyWhenAnEnemyHeroInRadiusQualifies()
        {
            var qualifying = new Turn(BrynnCardContent.TrueGrit);
            qualifying.Answer(qualifying.Choice.DeclineInvocation);

            Assert.That(qualifying.Choice, Is.Not.Null);
            Assert.That(qualifying.Choice.NodeId.Value, Is.EqualTo(BrynnEffectContent.GritMoveNode),
                "The boxed-in enemy hero stands inside True Grit's Radius 4, so the printed \"If\" " +
                "holds and the move is asked - even though the retrieval before it was declined, " +
                "which is the independence BR-Q8 records.");

            var open = new Turn(BrynnCardContent.TrueGrit, secondEnemyIsBoxedIn: false);
            open.Answer(open.Choice.DeclineInvocation);

            Assert.That(open.Choice, Is.Null,
                "and the identical card on a board where nobody in the disk qualifies ends there - " +
                "a condition that does not hold is a clause not reached, not a failure.");
            Assert.That(open.Space(Brynn), Is.EqualTo(BrynnAt));
        }

        /// <summary>
        /// "up to N spaces" is the distance's own optionality and not the step's: standing still is one
        /// of the answers, the declared number is still the bound, and True Grit's 3 and Die Hard's 4
        /// are the only thing separating the two faces.
        /// </summary>
        [Test]
        public void TheMove_AdmitsNoughtThroughThree_AndDieHardsFourth()
        {
            var grit = new Turn(BrynnCardContent.TrueGrit);
            grit.Answer(grit.Choice.DeclineInvocation);

            var reach = grit.Choice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(reach, Contains.Item(BrynnAt),
                "\"up to\" admits moving none at all (p.11), so his own space is one of the answers.");
            Assert.That(reach.Max(space => Distance(BrynnAt, space)), Is.EqualTo(3),
                "and three is the bound True Grit prints.");

            var dieHard = new Turn(BrynnCardContent.DieHard);
            dieHard.Answer(dieHard.Choice.DeclineInvocation);
            Assert.That(
                dieHard.Choice.Candidates
                    .Select(candidate => Distance(BrynnAt, candidate.Value.Coordinate))
                    .Max(),
                Is.EqualTo(4),
                "Die Hard prints four, which is the whole difference between the two bodies.");

            // A space three away that p.11 can actually be walked to: the enemy minion standing
            // beside him closes one file, and a move never passes through an obstacle.
            var destination = reach.First(space => Distance(BrynnAt, space) == 3);
            grit.Pick(EffectValue.Space(destination));
            Assert.That(grit.Space(Brynn), Is.EqualTo(destination));
            Assert.That(grit.Choice, Is.Null);
        }

        /// <summary>
        /// The pause the move opens survives a snapshot, restored to the same question with the same
        /// chooser - the second of this block's two pauses, and the one the acting seat owns.
        /// </summary>
        [Test]
        public void TheMovePauseRestoresToTheSameQuestion()
        {
            var turn = new Turn(BrynnCardContent.DieHard);
            turn.Answer(turn.Choice.DeclineInvocation);

            var standing = turn.Choice;
            Assert.That(standing.NodeId.Value, Is.EqualTo(BrynnEffectContent.GritMoveNode));
            Assert.That(standing.ChooserId, Is.EqualTo(Brynn));

            turn.RoundTrip();

            Assert.That(turn.Choice.NodeId, Is.EqualTo(standing.NodeId));
            Assert.That(turn.Choice.ChooserId, Is.EqualTo(standing.ChooserId));
            Assert.That(turn.Choice.Candidates.Count, Is.EqualTo(standing.Candidates.Count));
        }

        // -------------------------------------------------------------------------------------------
        // The one shared foundation this block widened
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The filter's shape, told by hand because a silently widened schema is a schema nobody was
        /// watching: an optional BOOLEAN on the one retrieval capability, narrowing whichever place its
        /// <c>kind</c> named, and unbound everywhere it was unbound before.
        /// </summary>
        [Test]
        public void TheAttackCardFilter_IsAnOptionalBooleanAndChangesNoRetrievalThatOmitsIt()
        {
            var handler = SyntheticRosterCapabilities.Handlers()
                .Single(candidate =>
                    candidate.Reference.Equals(SyntheticRosterCapabilities.RetrieveCardV1));
            var parameter = handler.Schema.Parameters.Single(value =>
                value.Name == SyntheticRosterCapabilities.AttackCardsOnlyParameter);

            Assert.That(parameter.ValueKind, Is.EqualTo(EffectValueKind.Boolean),
                "A Boolean, never an action-kind Integer: an unbound optional Integer reads as zero " +
                "and zero is ActionKind.None, so a retrieval that forgot to bind it would ask for a " +
                "card whose primary is nothing at all.");
            Assert.That(parameter.IsRequired, Is.False);

            Assert.That(
                RetrievalHandlerNames(SyntheticRosterCapabilities.RetrieveCardV1),
                Does.Not.Contain("kind:attack"),
                "and the enum it sits beside is untouched - it answers WHERE a card rests and this " +
                "answers WHAT is printed on it.");
            Assert.That((int)RosterCardRetrievalKind.None, Is.EqualTo(0));
            Assert.That((int)RosterCardRetrievalKind.OwnedDiscardedCard, Is.EqualTo(1));
            Assert.That((int)RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill,
                Is.EqualTo(2),
                "Two members, exactly as before this milestone: the filter is a parameter, so no " +
                "pairing had to be minted.");

            foreach (var hero in new[]
            {
                SilverarrowCardContent.AllDefinitions, RowennaCardContent.AllDefinitions
            })
            {
                foreach (var node in hero
                    .SelectMany(face => face.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes)
                    .Where(node => node.Capability.Equals(
                        SyntheticRosterCapabilities.RetrieveCardV1)))
                {
                    Assert.That(
                        node.Inputs.Any(input => input.ParameterName ==
                            SyntheticRosterCapabilities.AttackCardsOnlyParameter),
                        Is.False,
                        node.Id + " prints no such clause and must not declare one, which is what " +
                        "keeps its candidate set exactly what it was.");
                }
            }
        }

        /// <summary>
        /// Milestone 7.9's gate, asserted from BOTH sides. This block mints no contract at all - every
        /// clause its five faces print composes on something already registered, or on the predicate
        /// 8.69a deferred - and none of it reaches a configured match before 8.69g deals him.
        /// </summary>
        [Test]
        public void ThisBlockMintsNothing_AndEveryContractItsFacesNameIsRegisteredOrDeferredBy869a()
        {
            Assert.That(BrynnComponentContent.TrapAndGritDeferredHandlers(), Is.Empty,
                "Five printed faces and not one new capability, which is this block's result rather " +
                "than a gap - see BrynnComponentContent.Traps' own remarks.");
            Assert.That(BrynnComponentContent.TrapAndGritRegisteredHandlers(), Is.Empty);

            var build = MatchContentCatalogs.ForThisBuild();
            var deferred = BrynnComponentContent.ObstacleDeferredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();

            var named = BrynnCardContent.TrapAndGritDefinitions
                .SelectMany(face => face.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();

            Assert.That(named, Is.Not.Empty);
            foreach (var reference in named)
            {
                Assert.That(
                    build.Capabilities.Registered.Contains(reference) || deferred.Contains(reference),
                    Is.True,
                    reference + " is named by a face of his, so it is either a contract this build " +
                    "already registers or one 8.69a deferred - never a third thing nobody owns.");
            }

            Assert.That(
                named.Where(reference => deferred.Contains(reference)).ToArray(),
                Is.EqualTo(new[] { BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1 }),
                "and exactly one of them is deferred: the presence predicate the two grit faces ask.");

            foreach (var reference in deferred)
            {
                Assert.That(build.Capabilities.IsRegistered(reference), Is.False,
                    reference + " must not be reachable from a configured match before a dealt card " +
                    "of his can start it.");
            }

            Assert.That(BrynnCardContent.DealtDefinitions, Is.Empty,
                "and he still deals nothing, which is what keeps the dealt-corpus validator green " +
                "while his set is composed.");
            Assert.That(
                BrynnCardContent.AllDefinitions.Select(face => face.Id.Value).ToArray(),
                Is.SupersetOf(
                    BrynnCardContent.TrapAndGritDefinitions.Select(face => face.Id.Value).ToArray()),
                "while the five faces this block composes are visible to every content sweep.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging: the board
        // -------------------------------------------------------------------------------------------

        private static MatchParticipants Participants() =>
            StagedBoardParticipants.CreateWithBlueAlly();

        /// <summary>
        /// The one board every selection case reads, staged so that each refusal below is a DIFFERENT
        /// rule: Brynn at the origin; one enemy hero beside him with only Brynn standing near them;
        /// one enemy minion also beside him; and the second enemy hero out in the disk with three
        /// terrain spaces around them, none of which touches either of the first two.
        /// </summary>
        private static AdjacentMoveAuthority Board(
            HexCoordinate secondEnemyAt, bool boxIn = true) =>
            Compose(
                secondEnemyAt,
                boxIn ? BoxingTerrain(secondEnemyAt) : Array.Empty<HexCoordinate>());

        /// <summary>The same three heroes with no obstacle on the board at all - the Ultimate's case.</summary>
        private static AdjacentMoveAuthority OpenBoard() =>
            Compose(SecondEnemyNear, Array.Empty<HexCoordinate>(), withMinion: false);

        private static AdjacentMoveAuthority Compose(
            HexCoordinate secondEnemyAt,
            IReadOnlyList<HexCoordinate> terrain,
            bool withMinion = true)
        {
            var playable = Disk(6).Where(space => !terrain.Contains(space)).ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrain.ToArray()),
                new[]
                {
                    new HeroOccupancy(Brynn, BrynnAt),
                    new HeroOccupancy(Enemy, EnemyAt),
                    new HeroOccupancy(SecondEnemy, secondEnemyAt)
                },
                Array.Empty<ObstacleOccupancy>(),
                withMinion
                    ? new[] { MinionAt_() }
                    : Array.Empty<QuickGameMinionSetupState>());
        }

        /// <summary>
        /// Three of the subject's six neighbours, chosen so that none of them stands beside Brynn or
        /// beside the first enemy hero - which is what keeps "three obstacles" a fact about ONE hero.
        /// </summary>
        private static HexCoordinate[] BoxingTerrain(HexCoordinate subject) =>
            subject.GetNeighbors()
                .Where(space => Distance(space, BrynnAt) >= 2 && Distance(space, EnemyAt) >= 2 &&
                    Distance(space, MinionAt) >= 2)
                .Take(ObstacleAdjacencyQuery.PrintedObstacleThreshold)
                .ToArray();

        private static QuickGameMinionSetupState MinionAt_() =>
            new QuickGameMinionSetupState(
                Minion,
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("brynn-trap-spawn")),
                new ConfiguredSpaceReference(
                    Map, new SpaceId($"q{MinionAt.Q}_r{MinionAt.R}")),
                MinionAt);

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        private static int Distance(HexCoordinate a, HexCoordinate b) =>
            (Math.Abs(a.Q - b.Q) + Math.Abs(a.Q + a.R - b.Q - b.R) + Math.Abs(a.R - b.R)) / 2;

        // -------------------------------------------------------------------------------------------
        // Staging: one selection node, asked of its own declared inputs
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The heroes a composed selection node offers on one board. The inputs are the NODE's own -
        /// read off the definition rather than written again here - so a case that passes is a case
        /// about the card rather than about the probe.
        /// </summary>
        private static string[] Offered(
            CardDefinitionId cardId,
            string nodeId,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications = null,
            bool withRadiusItem = false) =>
            OfferedObjects(cardId, nodeId, board, modifications, withRadiusItem)
                .Where(candidate => candidate.Kind == BoardObjectKind.Hero)
                .Select(candidate => candidate.HeroId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static BoardObjectReference[] OfferedObjects(
            CardDefinitionId cardId,
            string nodeId,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications = null,
            bool withRadiusItem = false)
        {
            var face = Face(cardId);
            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var node = Node(skill.EffectRoot, nodeId);
            var handler = Catalogs().Capabilities.Load(node.Capability);
            var participants = Participants();
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-selection-probe"), face.Reference, Brynn);

            var inputs = new Dictionary<string, EffectValue>();
            foreach (var input in node.Inputs)
            {
                if (input.SourceKind == EffectBindingSourceKind.Literal)
                {
                    inputs[input.ParameterName] = input.Literal;
                }
            }

            var items = withRadiusItem
                ? HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                {
                    new HeroItem(
                        Brynn,
                        new CardInstanceId("brynn-trap-radius-item"),
                        HeroDashboard.RulebookLayout.Slots[3].Position,
                        PrintedValueChannel.Radius,
                        1)
                }))
                : new HeroItemState(participants);

            var result = handler.Execute(new EffectCapabilityContext(
                new EffectNodeId(nodeId),
                EffectFailurePolicy.Mandatory,
                Brynn,
                Brynn,
                card.Id,
                card.DefinitionReference,
                skill.Id,
                inputs,
                1,
                1,
                participants,
                null,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(new[] { face }, new[] { card }),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                items));

            return result.Kind == EffectExecutionResultKind.NeedsChoice
                ? result.Choice.Candidates
                    .Select(candidate => candidate.Kind == EffectValueKind.Hero
                        ? BoardObjectReference.ForHero(candidate.HeroId)
                        : BoardObjectReference.ForMinion(candidate.MinionId))
                    .ToArray()
                : Array.Empty<BoardObjectReference>();
        }

        /// <summary>
        /// Every enemy hero inside Bear Trap's printed disk, narrowing by nothing - the control the
        /// bullet-2 case is measured against.
        /// </summary>
        private static string[] HeroesInTheDisk(AdjacentMoveAuthority board) =>
            RosterPresenceQuery.FindUnitsWithin(
                    BrynnAt, 3, RosterUnitKind.Hero, SourceTeamRelation.OpposingSource, Brynn,
                    board, Participants(), RuleModificationSet.Empty)
                .Where(unit => unit.Kind == BoardObjectKind.Hero)
                .Select(unit => unit.HeroId.Value)
                .ToArray();

        /// <summary>One hero made immune to everything, for the two target refusals.</summary>
        private static RuleModificationSet Immunity(HeroId hero) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        RuleModification.ForImmunity(RuleActivity.All),
                        RuleObject.ForHero(hero),
                        new ContinuousRuleSource(
                            "brynn-trap-immunity", ContinuousRuleSourceKind.Ultimate, hero,
                            "probe", UltimateRuleDefinition.ApplicabilityKind,
                            new CardInstanceId("brynn-trap-immunity-card")),
                        TeamSide.Blue,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>The live set his unlocked Ultimate supplies while he is the acting object.</summary>
        private static RuleModificationSet UnlockedUltimate(AdjacentMoveAuthority board) =>
            RuleModificationQuery.Build(
                null,
                board,
                Participants(),
                TieBreakerState.Unrevealed,
                UltimateUnlockQuery.Derive(
                    new[] { Face(BrynnCardContent.OverTheTop) },
                    new[]
                    {
                        new CardInstance(
                            new CardInstanceId("brynn-trap-over-the-top"),
                            Face(BrynnCardContent.OverTheTop).Reference,
                            Brynn,
                            new CardStateFacts(
                                CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                CardFaceOrientation.FaceUp))
                    }),
                RuleObject.ForHero(Brynn),
                null,
                null);

        // -------------------------------------------------------------------------------------------
        // Staging: one whole turn
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The bounded catalog this block's content runs against: everything the build registers, plus
        /// the ONE contract 8.69a deferred. Nothing of this block's own is added, because this block
        /// declares nothing.
        /// </summary>
        private static MatchContentCatalogs Catalogs()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            return new MatchContentCatalogs(
                new EffectCapabilityCatalog(
                    build.Capabilities.Registered.Select(build.Capabilities.Load)
                        .Concat(BrynnComponentContent.ObstacleDeferredHandlers())
                        .GroupBy(handler => handler.Reference)
                        .Select(group => group.First())),
                build.Components,
                build.TriggeredEffects);
        }

        /// <summary>
        /// One Green Skill, actually played, in a real round: the two enemy seats commit Initiative 1
        /// so the turn is his, and the Skill starts through the production offer query.
        /// </summary>
        private sealed class Turn
        {
            private readonly CardDefinition face;
            private readonly MatchParticipants participants = Participants();
            private CardDefinition[] definitions;
            private CardInstance[] cards;

            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;

            internal Turn(
                CardDefinitionId cardId,
                bool enemyHasSpareCards = true,
                bool brynnHasDiscardedAttackCards = true,
                bool secondEnemyIsBoxedIn = true)
            {
                face = Face(cardId);
                Board = BrynnTrapAndGritTests.Board(SecondEnemyNear, secondEnemyIsBoxedIn);

                var source = CardInstance.CreateInHand(
                    new CardInstanceId("brynn-source"), face.Reference, Brynn);
                var held = new List<CardInstance>
                {
                    source,

                    // An Attack card of his still in HAND, which the retrieval must not offer: p.10
                    // puts a card in exactly one place, and this one is not in the discard.
                    CardInstance.CreateInHand(
                        new CardInstanceId("brynn-held-gold"),
                        Face(BrynnCardContent.FamiliarGround).Reference, Brynn)
                };

                if (brynnHasDiscardedAttackCards)
                {
                    held.Add(Discarded("brynn-discarded-gold", BrynnCardContent.FamiliarGround, Brynn));
                    held.Add(Discarded("brynn-discarded-red", BrynnCardContent.HighGround, Brynn));
                }
                else
                {
                    held.Add(Discarded("brynn-discarded-green-b", BrynnCardContent.LogTrap, Brynn));
                    held.Add(Discarded("brynn-discarded-blue-b", BlueSkill.Id, Brynn));
                }

                held.Add(Discarded("brynn-discarded-green", BrynnCardContent.BearTrap, Brynn));
                held.Add(Discarded("brynn-discarded-blue", BlueSkill.Id, Brynn));

                // Another hero's discarded Attack card: p.10 says outright that he can never
                // retrieve one.
                held.Add(Discarded("enemy-discarded-gold", BrynnCardContent.FamiliarGround, Enemy));

                held.Add(CardInstance.CreateInHand(
                    new CardInstanceId("enemy-commit"), Wall.Reference, Enemy));
                held.Add(CardInstance.CreateInHand(
                    new CardInstanceId("second-enemy-commit"), Wall.Reference, SecondEnemy));
                held.Add(CardInstance.CreateInHand(
                    new CardInstanceId("second-enemy-spare"), Wall.Reference, SecondEnemy));
                if (enemyHasSpareCards)
                {
                    held.Add(CardInstance.CreateInHand(
                        new CardInstanceId("enemy-spare"), Wall.Reference, Enemy));
                }

                cards = held.ToArray();
                definitions = BrynnCardContent.AllDefinitions
                    .Concat(new[] { Wall, BlueSkill })
                    .ToArray();

                var markers = new MarkerAttachmentRegister();
                var catalogs = Catalogs();
                Round = new RoundResolutionAuthority(
                    definitions, cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForMatch(
                        new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null,
                        startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));

                Commit(Brynn, source.Id);
                Commit(Enemy, new CardInstanceId("enemy-commit"));
                Commit(SecondEnemy, new CardInstanceId("second-enemy-commit"));

                Assert.That(Round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(Brynn),
                    "The two enemy seats commit Initiative 1, so his Green face - Initiative 2 or 3 " +
                    "- is the highest committed and the turn under test is his.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Brynn, source.Id)
                    .Single(candidate =>
                        candidate.ActionDefinitionId.Equals(BrynnCardContent.PrimarySkill));
                Observe(Round.TryStartSkill(new ActionResolutionId("brynn-trap-action"), offer));
            }

            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;

            internal void Option(string optionNodeId) => Select(Choice.SelectInvocations.Single(
                command => command.Candidate.OptionNodeId.Value == optionNodeId));

            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(
                command => command.Candidate.Value.Equals(value)));

            internal void Answer(EffectChoiceCommand command) =>
                Observe(Round.TryAnswerEffectChoice(command));

            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }

            private void Commit(HeroId hero, CardInstanceId card) =>
                Observe(Round.TryCommitCard(hero, card));

            private void Observe(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" +
                    result.CardActionRejectionReason);
                if (result.Events == null) return;
                foreach (var value in result.Events) Round.Scoreboard.Observe(value);
            }

            /// <summary>
            /// The whole authoritative state through a serialize/deserialize cycle - which is what a
            /// restore, a replay and an Undo each re-run.
            /// </summary>
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    Catalogs(), bytes, definitions,
                    new[] { Board.MapDefinition }, new[] { MinionAt_() });

                Assert.That(
                    AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(bytes),
                    "A payload that does not re-serialize to itself is a payload that lost " +
                    "something on the way through.");

                Round = restored.Round;
                Board = restored.Board;
                cards = restored.Cards.ToArray();
            }

            internal int HandCount(HeroId hero) => cards.Count(card =>
                card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

            internal int DiscardCount(HeroId hero) => cards.Count(card =>
                card.OwnerId.Equals(hero) && card.State.Location == CardLocation.DiscardPile);

            internal CardInstance Card(CardInstanceId id) =>
                cards.Single(card => card.Id.Equals(id));

            internal bool OnBoard(HeroId hero) =>
                Board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(hero));

            internal HexCoordinate Space(HeroId hero) =>
                Board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(hero)).Coordinate;

            internal int Coins(HeroId hero) =>
                Round.CoinBalances.Single(balance => balance.Key.Equals(hero)).Value;

            internal int Kills(HeroId hero) => Round.Scoreboard.For(hero).Kills;

            private static CardInstance Discarded(
                string id, CardDefinitionId definitionId, HeroId owner) =>
                new CardInstance(
                    new CardInstanceId(id),
                    Face(definitionId).Reference,
                    owner,
                    new CardStateFacts(
                        CardLocation.DiscardPile, true, CardResolutionStatus.Resolved,
                        CardFaceOrientation.FaceUp));
        }

        // -------------------------------------------------------------------------------------------
        // Two synthetic faces the staging needs and his own set does not supply
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Initiative 1, so the two enemy seats commit something his Green faces outrank; Defense 9,
        /// so nothing here turns into an Attack this block is not about.
        /// </summary>
        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("brynn-trap-wall"), "brynn-trap-fixture-rev-1",
            "TEST - Brynn trap wall", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("wall-move"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                new ActionDefinition(
                    new ActionDefinitionId("wall-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        /// <summary>
        /// A BLUE face with a Skill primary, which the retrieval must refuse. His own twelve Blues and
        /// Greens all print Skill primaries and 8.69c and 8.69d compose the Blue half of them, so the
        /// colour's half of BR-Q8 is stated here on a fixture card rather than deferred to a block
        /// that has not landed.
        /// </summary>
        private static readonly CardDefinition BlueSkill = new CardDefinition(
            new CardDefinitionId("brynn-trap-blue-skill"), "brynn-trap-fixture-rev-1",
            "TEST - Brynn blue skill", new InitiativeValue(10),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NonBasicCard(
                CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                Array.Empty<CardDefinitionId>()),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("blue-skill"), ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    // A Skill definition owes an effect root; the honest one for a card that exists
                    // only to be refused by a filter is the node that asks where its owner stands.
                    new EffectDefinition(
                        new EffectDefinitionId("brynn-trap-blue-skill-body"),
                        "brynn-trap-fixture-rev-1",
                        new EffectNodeId("blue-root"),
                        new[]
                        {
                            new EffectNodeDefinition(
                                new EffectNodeId("blue-root"), EffectNodeKind.Capability,
                                EffectFailurePolicy.Mandatory,
                                SyntheticPlacementCapabilities.ActorSpaceV1,
                                outputs: new[]
                                {
                                    new EffectOutputBinding("space", EffectValueKind.Coordinate)
                                })
                        })),
                new ActionDefinition(
                    new ActionDefinitionId("blue-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        // -------------------------------------------------------------------------------------------
        // Reading the composition
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Face(CardDefinitionId id) =>
            id.Equals(BlueSkill.Id)
                ? BlueSkill
                : BrynnCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private static EffectDefinition SkillBody(CardDefinitionId id) =>
            Face(id).ActionDefinitions.Single(action => action.Kind == ActionKind.Skill).EffectRoot;

        private static EffectNodeDefinition Node(EffectDefinition body, string nodeId) =>
            body.Nodes.Single(node => node.Id.Value == nodeId);

        private static EffectValue Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal;

        private static string[] RetrievalHandlerNames(EffectCapabilityReference reference) =>
            SyntheticRosterCapabilities.Handlers()
                .Single(handler => handler.Reference.Equals(reference))
                .Schema.Parameters
                .Select(parameter => parameter.Name)
                .ToArray();

        private static int PrintedValue(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Single(action => action.Kind == kind)
                .BaseValues
                .Single(value => value.Channel == channel)
                .Amount;
    }
}

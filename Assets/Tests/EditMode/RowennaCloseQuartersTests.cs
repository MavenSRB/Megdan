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
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57d. Rowenna's five printed Green faces - the three Movement cards that place a
    /// minion after her walk, and the two Skills that empty an enemy hero's hand by one card while
    /// both discard piles are empty.
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Rowenna's is registered - milestone 7.9's gate holds her content out of
    /// <see cref="MatchContentCatalogs"/> until 8.57g deals her - so no composed match can put one of
    /// these cards in a hand and perform its primary. <see cref="RowennaSwapAndGauntletTests"/> records
    /// the same boundary for the same reason. What is proved here is everything the text itself
    /// decides: when the rider runs at all, who is offered, whose decision each question is, where the
    /// pieces end up and which piles move. What is deliberately NOT proved here is the enclosing action
    /// pipeline - the held continuation that runs this body when the walk completes is milestone
    /// 8.35z31's and Ursafar's fixtures own it - and 8.57f and 8.57g own the dealt version.</para>
    ///
    /// <para><b>The catalog these cases load is this build's plus the TWO deferred declarations.</b>
    /// <c>synthetic-place-in-range-beside-a-named-hero@1</c> and this milestone's
    /// <c>synthetic-place-in-radius-beside-a-named-hero@1</c> both sit in
    /// <see cref="SyntheticPlacementCapabilities.DeferredHandlers"/> because no dealt card starts either
    /// yet, and a fixture that could not load them could not prove the faces at all. That the build
    /// itself does NOT register the new one is asserted below rather than assumed.</para>
    ///
    /// <para><b>The board is four seats wide and says why.</b> Two Blue - Rowenna and one ally, so a
    /// friendly minion is not the only friendly thing on the board - and two Red, so a case can stand
    /// two enemy heroes adjacent to her and a case can stand one inside her printed Radius and one
    /// outside it without moving anybody between cases.</para>
    /// </summary>
    public sealed class RowennaCloseQuartersTests
    {
        private static readonly HeroId Rowenna = new HeroId("rowenna-vanguard");
        private static readonly HeroId Ally = new HeroId("rowenna-blue-ally");
        private static readonly HeroId Enemy = new HeroId("rowenna-red-enemy");
        private static readonly HeroId SecondEnemy = new HeroId("rowenna-red-second-enemy");

        private static readonly TeamId BlueTeam = new TeamId("rowenna-blue-team");
        private static readonly TeamId RedTeam = new TeamId("rowenna-red-team");

        private static readonly MapId Map = new MapId("rowenna-close-quarters-map");

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent to Rowenna: the hero the rider's predicate finds.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        /// <summary>Also adjacent, for the cases where two enemy heroes stand beside her.</summary>
        private static readonly HexCoordinate SecondEnemyBeside = new HexCoordinate(0, 1);

        /// <summary>Three spaces out: inside Opening Shots' printed Radius 3 and beside nobody.</summary>
        private static readonly HexCoordinate SecondEnemyInRadius = new HexCoordinate(3, 0);

        /// <summary>Five spaces out: outside every Radius her Green faces print.</summary>
        private static readonly HexCoordinate FarAway = new HexCoordinate(5, 0);

        /// <summary>Where the ally stands: well clear of every disk and every neighbourhood.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(0, -4);

        private static readonly MinionId FriendlyNear = new MinionId("rowenna-friendly-near");
        private static readonly MinionId FriendlyMid = new MinionId("rowenna-friendly-mid");
        private static readonly MinionId FriendlyFar = new MinionId("rowenna-friendly-far");
        private static readonly MinionId EnemyNear = new MinionId("rowenna-enemy-near");
        private static readonly MinionId EnemySecond = new MinionId("rowenna-enemy-second");
        private static readonly MinionId EnemyFar = new MinionId("rowenna-enemy-far");

        /// <summary>Two spaces from Rowenna: inside Close Quarters' printed Radius 2.</summary>
        private static readonly HexCoordinate FriendlyNearAt = new HexCoordinate(0, 2);

        /// <summary>Three out: outside Close Quarters' Radius 2 and inside both upgrades' Radius 3.</summary>
        private static readonly HexCoordinate FriendlyMidAt = new HexCoordinate(0, 3);

        /// <summary>Four out: outside every Radius these three faces print.</summary>
        private static readonly HexCoordinate FriendlyFarAt = new HexCoordinate(0, 4);

        private static readonly HexCoordinate EnemyNearAt = new HexCoordinate(-2, 0);
        private static readonly HexCoordinate EnemySecondAt = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate EnemyFarAt = new HexCoordinate(-4, 0);

        private static readonly (MinionId Id, HexCoordinate At, TeamSide Side)[] MeleeMinions =
        {
            (FriendlyNear, FriendlyNearAt, TeamSide.Blue),
            (FriendlyMid, FriendlyMidAt, TeamSide.Blue),
            (FriendlyFar, FriendlyFarAt, TeamSide.Blue),
            (EnemyNear, EnemyNearAt, TeamSide.Red),
            (EnemySecond, EnemySecondAt, TeamSide.Red),
            (EnemyFar, EnemyFarAt, TeamSide.Red)
        };

        // -------------------------------------------------------------------------------------------
        // What the three Movement faces DECLARE - the timing is the whole of "after movement"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "After movement" is not a node in the body: it is the timing the action declares, which is
        /// milestone 8.35z31's member and the seam that parks this text until the walk is over. Three
        /// consequences ride on the declaration alone - a zero-space walk still completes and still
        /// applies the text, a card-text move is not a Movement ACTION and reaches the seam never, and
        /// the adjacency below is therefore read against the board as the walk left it.
        /// </summary>
        [Test]
        public void TheThreeMovementFaces_DeclareTheirTextAfterTheMovementAndNothingElseOfHersDoes()
        {
            foreach (var cardId in new[]
            {
                RowennaCardContent.CloseQuarters,
                RowennaCardContent.Melee,
                RowennaCardContent.GrandMelee
            })
            {
                var primary = RowennaCardContent.Definition(cardId).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                Assert.That(primary.Kind, Is.EqualTo(ActionKind.Movement), cardId.Value);
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheMovement),
                    cardId.Value + " prints 'After movement' in so many words, which is the one " +
                    "thing rulebook p.11's 'unless it says otherwise' is about.");
                Assert.That(primary.EffectRoot, Is.Not.Null, cardId.Value);
                Assert.That(
                    primary.BaseValues.Single(value =>
                        value.Channel == PrintedValueChannel.Movement).Amount,
                    Is.EqualTo(2),
                    cardId.Value + " prints a Movement 2 icon in its own band.");
            }

            Assert.That(
                RowennaCardContent.AllDefinitions
                    .SelectMany(card => card.ActionDefinitions)
                    .Count(action => action.CardTextTiming == CardTextTiming.AfterTheMovement),
                Is.EqualTo(3),
                "and no other composed face of hers carries the timing, so nothing else can reach " +
                "this seam.");
        }

        /// <summary>
        /// Close Quarters prints Radius 2 and both upgrades print 3, which is the whole of what
        /// separates the bodies' reach - no distance is written into the definition, so p.16's Radius
        /// item raises each face by its own icon.
        /// </summary>
        [Test]
        public void TheGreenFaces_CarryTheirOwnPrintedRadiusIconRatherThanAWrittenDistance()
        {
            Assert.That(PrintedRadius(RowennaCardContent.CloseQuarters), Is.EqualTo(2));
            Assert.That(PrintedRadius(RowennaCardContent.Melee), Is.EqualTo(3));
            Assert.That(PrintedRadius(RowennaCardContent.GrandMelee), Is.EqualTo(3));
            Assert.That(PrintedRadius(RowennaCardContent.OpeningShots), Is.EqualTo(3));
            Assert.That(PrintedRadius(RowennaCardContent.OpeningVolley), Is.EqualTo(3));
        }

        // -------------------------------------------------------------------------------------------
        // The rider's own condition
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "If you are adjacent to an enemy hero" read on the board as it stands: with every enemy hero
        /// further away the text is over before it asks anything, and no minion moves.
        /// </summary>
        [Test]
        public void CloseQuarters_AsksNothingWhenNoEnemyHeroIsAdjacent()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters, enemyAt: SecondEnemyInRadius);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "An unsatisfied 'if' is a step that simply does not happen.");
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(FriendlyNearAt));
            Assert.That(world.Where(EnemyNear), Is.EqualTo(EnemyNearAt));
        }

        /// <summary>
        /// An adjacent enemy MINION is not an adjacent enemy hero: the predicate names p.19's hero, so
        /// a board with minions beside her and every hero at a distance asks nothing either.
        /// </summary>
        [Test]
        public void CloseQuarters_ReadsTheConditionAboutHeroesRatherThanAboutUnits()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                enemyAt: SecondEnemyInRadius,
                minions: new[]
                {
                    (FriendlyNear, FriendlyNearAt, TeamSide.Blue),
                    (EnemyNear, new HexCoordinate(-1, 0), TeamSide.Red)
                });

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "'An enemy hero' is p.19's hero, and a minion standing beside her is not one.");
        }

        /// <summary>
        /// With an enemy hero beside her the rider opens rulebook p.10's confirm-or-decline, and it is
        /// hers: "you may Choose one" is her own optional step.
        /// </summary>
        [Test]
        public void CloseQuarters_OpensHerOwnOptionalStepWhenAnEnemyHeroIsAdjacent()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var choice = world.RunToPause();

            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.ChooserId, Is.EqualTo(Rowenna));
            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "'you may' has to be declinable by name.");
        }

        /// <summary>
        /// And declining is a complete, legal answer: nothing is placed and the text is over.
        /// </summary>
        [Test]
        public void CloseQuarters_PlacesNothingWhenSheDeclines()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var choice = world.RunToPause();
            var finished = world.SubmitToCompletion(choice.DeclineInvocation);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(FriendlyNearAt));
            Assert.That(world.Where(EnemyNear), Is.EqualTo(EnemyNearAt));
        }

        /// <summary>
        /// Confirming opens the printed modal: exactly two arms, chosen by her, exactly one of them.
        /// </summary>
        [Test]
        public void CloseQuarters_OffersTheTwoPrintedBulletsAsOneModal()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var may = world.RunToPause();
            var modal = world.Answer(may, may.ConfirmInvocation);

            Assert.That(modal.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(modal.ChooserId, Is.EqualTo(Rowenna));
            Assert.That(modal.MinimumSelections, Is.EqualTo(1));
            Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                "'Choose one' is exactly one bullet.");
            Assert.That(Arms(modal), Is.EqualTo(new[]
            {
                "melee-first-friendly", "melee-first-enemy"
            }));
        }

        // -------------------------------------------------------------------------------------------
        // Bullet 1 - "Place a friendly minion in radius into a space adjacent to that enemy hero"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The subjects are exactly the friendly minions inside the acting card's printed Radius, and
        /// every exclusion is a different word of the sentence doing work: "friendly" keeps the enemy
        /// minions out, "in radius" keeps the two further ones out, and p.19's minion keeps the ally
        /// HERO out.
        /// </summary>
        [Test]
        public void CloseQuarters_FirstBulletOffersExactlyTheFriendlyMinionsInsideThePrintedRadius()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var minions = world.ToFirstBullet();

            Assert.That(Named(minions), Is.EqualTo(new[] { FriendlyNear.Value }),
                "Radius 2 reaches the minion at two spaces and neither of the ones at three and " +
                "four; the enemy minions are the wrong side and the ally is not a minion.");
        }

        /// <summary>
        /// The same board read by the upgrade: Melee prints Radius 3, so the minion three spaces out
        /// joins the offer without one word of the sentence changing. That is what makes the icon the
        /// reach rather than a number the body wrote down.
        /// </summary>
        [Test]
        public void Melee_FirstBulletReachesTheMinionAtThreeThatCloseQuartersCannot()
        {
            var world = World.Melee(RowennaCardContent.Melee);

            var minions = world.ToFirstBullet();

            Assert.That(Named(minions), Is.EqualTo(new[]
            {
                FriendlyNear.Value, FriendlyMid.Value
            }));
        }

        /// <summary>
        /// "That enemy hero" is named once the minion is picked, and with only one enemy hero beside
        /// her the prompt still names him rather than assuming him - the sentence chooses, and a
        /// one-candidate choice is still the choice the card prints.
        /// </summary>
        [Test]
        public void CloseQuarters_NamesTheEnemyHeroAfterTheMinionAndBeforeTheSpace()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var minions = world.ToFirstBullet();
            var hero = world.Answer(minions, Pick(minions, FriendlyNear));

            Assert.That(Named(hero), Is.EqualTo(new[] { Enemy.Value }));
            Assert.That(hero.ChooserId, Is.EqualTo(Rowenna));
        }

        /// <summary>
        /// With several enemy heroes adjacent she names ONE, which is RO-Q4 - the condition pinned
        /// nobody, so both are offered and the pronoun follows her answer.
        /// </summary>
        [Test]
        public void CloseQuarters_OffersEveryAdjacentEnemyHeroAndTakesExactlyOne()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters, secondEnemyAt: SecondEnemyBeside);

            var minions = world.ToFirstBullet();
            var hero = world.Answer(minions, Pick(minions, FriendlyNear));

            Assert.That(Named(hero), Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));
            Assert.That(hero.MinimumSelections, Is.EqualTo(1));
            Assert.That(hero.MaximumSelections, Is.EqualTo(1));

            var spaces = world.Answer(hero, Pick(hero, SecondEnemy));

            Assert.That(Spaces(spaces), Is.EquivalentTo(EmptyNeighboursOf(SecondEnemyBeside, world)),
                "and the destinations follow the hero she named rather than the other one.");
        }

        /// <summary>
        /// The destinations are EXACTLY the empty spaces beside the named hero. The card's own printed
        /// Radius bounds the disk this Place measures, and this is the measurement that shows the bound
        /// can never cut the sentence short: the hero is adjacent to her by the rider's own predicate,
        /// so every neighbour of his stands at most two spaces away, and the smallest Radius any face
        /// printing this clause carries is two.
        /// </summary>
        [Test]
        public void CloseQuarters_FirstBulletOffersEveryEmptySpaceBesideTheNamedHeroAndNoOther()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var spaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);

            Assert.That(Spaces(spaces), Is.EquivalentTo(EmptyNeighboursOf(EnemyAt, world)));
            Assert.That(Spaces(spaces), Has.No.Member(RowennaAt),
                "Her own space is occupied, so p.18 refuses it without any clause saying so.");
            Assert.That(Spaces(spaces), Has.No.Member(new HexCoordinate(-1, 0)),
                "A space beside HER and not beside HIM is not what the sentence admits, even " +
                "though it is well inside her printed Radius.");
            Assert.That(Spaces(spaces).Length, Is.EqualTo(5),
                "Five of his six neighbours are empty; the sixth is where she is standing.");
        }

        /// <summary>
        /// An occupied neighbour is not a destination, and an obstacle on one is not either - p.18's
        /// own two refusals, asked by the Place rather than restated by the card.
        /// </summary>
        [Test]
        public void CloseQuarters_LeavesOutTheNeighboursAnObstacleOrAUnitAlreadyHolds()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                obstaclesAt: new[] { new HexCoordinate(2, 0) },
                minions: new[]
                {
                    (FriendlyNear, FriendlyNearAt, TeamSide.Blue),
                    (EnemyNear, new HexCoordinate(1, 1), TeamSide.Red)
                });

            var spaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);

            Assert.That(Spaces(spaces), Has.No.Member(new HexCoordinate(2, 0)),
                "An obstacle closes a space to a Place exactly as it closes it to a walk.");
            Assert.That(Spaces(spaces), Has.No.Member(new HexCoordinate(1, 1)),
                "and p.19 admits no second occupant in a space.");
            Assert.That(Spaces(spaces), Is.EquivalentTo(new[]
            {
                new HexCoordinate(1, -1), new HexCoordinate(2, -1), new HexCoordinate(0, 1)
            }));
        }

        /// <summary>
        /// The whole bullet, committed: the minion is standing beside the named hero, it is the same
        /// minion of the same side and type it was, nothing else on the board moved, and Rowenna did
        /// not - a Place is not a movement and p.18 says so.
        /// </summary>
        [Test]
        public void CloseQuarters_PlacesTheNamedMinionBesideTheNamedHeroAndMovesNothingElse()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var spaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);
            var destination = new HexCoordinate(1, 1);
            var finished = world.AnswerToCompletion(spaces, Pick(spaces, destination));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(destination));
            Assert.That(world.SideOf(FriendlyNear), Is.EqualTo(TeamSide.Blue),
                "A placed minion is just a minion that moved: its identity, its side and its type " +
                "are untouched.");
            Assert.That(world.TypeOf(FriendlyNear), Is.EqualTo(StandardMinionType.Melee));
            Assert.That(world.Where(Rowenna), Is.EqualTo(RowennaAt));
            Assert.That(world.Where(Enemy), Is.EqualTo(EnemyAt));
            Assert.That(world.Where(EnemyNear), Is.EqualTo(EnemyNearAt));
            Assert.That(world.Where(FriendlyMid), Is.EqualTo(FriendlyMidAt));
        }

        /// <summary>
        /// The block's declared boundary, measured rather than argued. A hero with no empty space
        /// beside him is still OFFERED - milestone 6.8l's narrowing is asked of a selection handler and
        /// <c>choose-target@2</c> is not one - and naming him stops the bullet at the Place, with
        /// everything before it applied and nothing else asked. He is walled in here by four obstacles,
        /// by Rowenna and by the second enemy hero.
        /// </summary>
        [Test]
        public void CloseQuarters_StopsTheBulletWhenTheNamedHeroHasNoEmptySpaceBesideHim()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                secondEnemyAt: SecondEnemyBeside,
                obstaclesAt: EnemyAt.GetNeighbors()
                    .Where(space => !space.Equals(RowennaAt) && !space.Equals(SecondEnemyBeside))
                    .ToArray());

            var minions = world.ToFirstBullet();
            var hero = world.Answer(minions, Pick(minions, FriendlyNear));

            Assert.That(Named(hero), Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));

            var finished = world.SubmitToCompletion(Pick(hero, Enemy));

            Assert.That(finished.IsComplete, Is.True,
                "Every neighbour of the hero she named is closed, so p.10 stops the text at the " +
                "mandatory step it cannot take.");
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(FriendlyNearAt),
                "and nothing was placed.");
        }

        /// <summary>
        /// An enemy hero immune to the action this text is printed inside cannot be named at all - p.18
        /// makes immunity a question about what may be done to a unit, and the question this selection
        /// asks is the enclosing Movement action's own, because nothing is done to the hero himself.
        /// </summary>
        [Test]
        public void CloseQuarters_CannotNameAnEnemyHeroImmuneToTheActionTheTextIsPrintedInside()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                secondEnemyAt: SecondEnemyBeside,
                modifications: ImmunityFor(Enemy, RuleActivity.Movement));

            var minions = world.ToFirstBullet();
            var hero = world.Answer(minions, Pick(minions, FriendlyNear));

            Assert.That(Named(hero), Is.EqualTo(new[] { SecondEnemy.Value }));
        }

        // -------------------------------------------------------------------------------------------
        // Bullet 2 - "Place an enemy minion in radius into a space adjacent to you"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The mirror image of bullet 1's subject clause: enemy minions inside the printed Radius, and
        /// nothing else.
        /// </summary>
        [Test]
        public void CloseQuarters_SecondBulletOffersExactlyTheEnemyMinionsInsideThePrintedRadius()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var minions = world.ToSecondBullet();

            Assert.That(Named(minions), Is.EqualTo(new[] { EnemyNear.Value, EnemySecond.Value }),
                "Both enemy minions at two spaces qualify; the one at four is out of Radius 2 and " +
                "the three friendly ones are the wrong side.");
        }

        /// <summary>
        /// "Into a space adjacent to you" is measured from HER, which is what no radius around the
        /// piece could ask - Wasp's Telekinesis' own declaration, read a second time.
        /// </summary>
        [Test]
        public void CloseQuarters_SecondBulletOffersExactlyTheEmptySpacesBesideHer()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var minions = world.ToSecondBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));

            Assert.That(Spaces(spaces), Is.EquivalentTo(EmptyNeighboursOf(RowennaAt, world)));
            Assert.That(Spaces(spaces), Has.No.Member(EnemyAt),
                "The enemy hero is standing on one of them.");
            Assert.That(Spaces(spaces), Has.No.Member(new HexCoordinate(2, 0)),
                "and a space beside the enemy HERO is not a space beside her.");
        }

        /// <summary>
        /// Committed: the enemy minion stands beside her, keeps its own side, and nothing was defeated -
        /// which is the sentence containing no such word.
        /// </summary>
        [Test]
        public void CloseQuarters_SecondBulletPlacesTheEnemyMinionBesideHer()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters, withScoreboard: true);

            var minions = world.ToSecondBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            var destination = new HexCoordinate(-1, 1);
            world.AnswerToCompletion(spaces, Pick(spaces, destination));

            Assert.That(world.Where(EnemyNear), Is.EqualTo(destination));
            Assert.That(world.SideOf(EnemyNear), Is.EqualTo(TeamSide.Red));
        }

        /// <summary>
        /// Milestone 8.53's scoreboard cannot move for a Place, and this is the case that says so
        /// rather than leaving it to be noticed: the column counts recorded minion Defeats, and placing
        /// a minion defeats nothing.
        /// </summary>
        [Test]
        public void CloseQuarters_PlacingAMinionLeavesTheScoreboardsMinionColumnAtZero()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters, withScoreboard: true);

            var minions = world.ToSecondBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            world.AnswerToCompletion(spaces, Pick(spaces, new HexCoordinate(-1, 1)));

            Assert.That(world.MinionsKilled(Rowenna), Is.Zero);
            Assert.That(world.MinionsKilled(Enemy), Is.Zero);
        }

        /// <summary>
        /// A bullet with no legal minion is not offered, and the modal still opens on the other one -
        /// which is what a <c>Choose</c> does with an option it can prove unperformable.
        /// </summary>
        [Test]
        public void CloseQuarters_OffersOnlyTheBulletThatHasASubject()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                minions: new[] { (EnemyNear, EnemyNearAt, TeamSide.Red) });

            var may = world.RunToPause();
            var modal = world.Answer(may, may.ConfirmInvocation);

            Assert.That(
                modal.UnavailableCandidates.Select(candidate => candidate.OptionNodeId.Value),
                Is.EqualTo(new[] { "melee-first-friendly" }),
                "The bullet with no friendly minion in radius is greyed rather than missing, which " +
                "is what lets a control name its own blocker.");
            Assert.That(
                modal.SelectInvocations.Select(invocation =>
                    invocation.Candidate.OptionNodeId.Value),
                Is.EqualTo(new[] { "melee-first-enemy" }),
                "and only the other one can be answered.");
        }

        /// <summary>
        /// With neither bullet takeable the optional step is not opened at all: milestone 8.29e's rule
        /// that an offer whose branch can do nothing is not an offer.
        /// </summary>
        [Test]
        public void CloseQuarters_AsksNothingWhenNeitherBulletHasASubject()
        {
            var world = World.Melee(
                RowennaCardContent.CloseQuarters,
                minions: new[]
                {
                    (FriendlyFar, FriendlyFarAt, TeamSide.Blue),
                    (EnemyFar, EnemyFarAt, TeamSide.Red)
                });

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "Both minions are outside the printed Radius, so there is nothing here for a " +
                "player to answer.");
        }

        // -------------------------------------------------------------------------------------------
        // Grand Melee - "Choose up to two times"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Grand Melee asks the same question twice and neither asking is a "may": "up to two times"
        /// makes zero a legal count, so each performance is declinable on its own.
        /// </summary>
        [Test]
        public void GrandMelee_AsksTwiceAndBothPerformancesAreDeclinable()
        {
            var world = World.Melee(RowennaCardContent.GrandMelee);

            var first = world.RunToPause();
            Assert.That(first.NodeId.Value, Is.EqualTo("melee-first-may"));
            Assert.That(first.DeclineInvocation, Is.Not.Null);

            var second = world.Answer(first, first.DeclineInvocation);
            Assert.That(second.NodeId.Value, Is.EqualTo("melee-second-may"));
            Assert.That(second.DeclineInvocation, Is.Not.Null);

            var finished = world.SubmitToCompletion(second.DeclineInvocation);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(FriendlyNearAt),
                "Zero is a legal count, which is what 'up to' means.");
            Assert.That(world.Where(EnemyNear), Is.EqualTo(EnemyNearAt));
        }

        /// <summary>
        /// Taking one and stopping is the middle count, and the second question is still asked before
        /// it is refused - "up to two times" does not end at the first refusal.
        /// </summary>
        [Test]
        public void GrandMelee_MayTakeOnePerformanceAndStop()
        {
            var world = World.Melee(RowennaCardContent.GrandMelee);

            var spaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);
            var second = world.Answer(spaces, Pick(spaces, new HexCoordinate(1, 1)));

            Assert.That(second.NodeId.Value, Is.EqualTo("melee-second-may"));
            var finished = world.SubmitToCompletion(second.DeclineInvocation);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyNear), Is.EqualTo(new HexCoordinate(1, 1)));
            Assert.That(world.Where(FriendlyMid), Is.EqualTo(FriendlyMidAt));
        }

        /// <summary>
        /// The same bullet twice, and the second performance is offered against the board the first one
        /// left: the minion it already moved is gone from where it was, the space it now fills is no
        /// longer a destination, and its identity is still an ENEMY minion - which is the whole proof
        /// that a Place preserves what it moves.
        /// </summary>
        [Test]
        public void GrandMelee_TakesTheSameBulletTwiceAgainstTheChangedBoard()
        {
            var world = World.Melee(RowennaCardContent.GrandMelee);

            var first = world.ToSecondBullet();
            var firstSpaces = world.Answer(first, Pick(first, EnemyNear));
            var filled = new HexCoordinate(-1, 1);
            var secondMay = world.Answer(firstSpaces, Pick(firstSpaces, filled));

            var secondModal = world.Answer(secondMay, secondMay.ConfirmInvocation);
            var second = world.Answer(
                secondModal, PickArm(secondModal, "melee-second-enemy"));

            Assert.That(Named(second), Is.EquivalentTo(new[]
            {
                EnemyNear.Value, EnemySecond.Value
            }),
                "The minion the first performance placed is still an enemy minion and is still in " +
                "radius, so it is still a candidate.");

            var secondSpaces = world.Answer(second, Pick(second, EnemySecond));

            Assert.That(Spaces(secondSpaces), Has.No.Member(filled),
                "and the space the first performance filled is no longer empty.");
            Assert.That(Spaces(secondSpaces), Is.EquivalentTo(EmptyNeighboursOf(RowennaAt, world)));

            world.AnswerToCompletion(secondSpaces, Pick(secondSpaces, new HexCoordinate(0, -1)));

            Assert.That(world.Where(EnemyNear), Is.EqualTo(filled));
            Assert.That(world.Where(EnemySecond), Is.EqualTo(new HexCoordinate(0, -1)));
        }

        /// <summary>
        /// RO-Q7. The second performance of the first bullet names its OWN "that enemy hero": the
        /// condition pinned nobody, the two performances are two choices made against a board that
        /// changed in between, and a naming carried across both could not even be composed - a player
        /// who took the second bullet first would reach it with nothing named.
        /// </summary>
        [Test]
        public void GrandMelee_LetsTheSecondPerformanceNameADifferentAdjacentEnemyHero()
        {
            var world = World.Melee(
                RowennaCardContent.GrandMelee, secondEnemyAt: SecondEnemyBeside);

            var firstSpaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);
            var secondMay = world.Answer(firstSpaces, Pick(firstSpaces, new HexCoordinate(2, -1)));

            var secondModal = world.Answer(secondMay, secondMay.ConfirmInvocation);
            var minions = world.Answer(
                secondModal, PickArm(secondModal, "melee-second-friendly"));
            var hero = world.Answer(minions, Pick(minions, FriendlyMid));

            Assert.That(Named(hero), Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "The second performance asks the question again rather than inheriting an answer.");

            var spaces = world.Answer(hero, Pick(hero, SecondEnemy));
            var destination = new HexCoordinate(-1, 2);
            world.AnswerToCompletion(spaces, Pick(spaces, destination));

            Assert.That(world.Where(FriendlyNear), Is.EqualTo(new HexCoordinate(2, -1)));
            Assert.That(world.Where(FriendlyMid), Is.EqualTo(destination),
                "and the second minion went beside the OTHER hero.");
        }

        /// <summary>
        /// A restore taken while the modal stands resumes against freshly rebuilt content and answers
        /// the same question, which is what a snapshot carries and what the resolver has to be able to
        /// resume from. Milestone 8.35i is where that boundary was learned.
        /// </summary>
        [Test]
        public void CloseQuarters_ResumesThroughARestoreWhileTheModalIsPending()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var may = world.RunToPause();
            var modal = world.Answer(may, may.ConfirmInvocation);

            world.RebindTrustedContent();

            var minions = world.Answer(modal, PickArm(modal, "melee-first-friendly"));
            var hero = world.Answer(minions, Pick(minions, FriendlyNear));
            var spaces = world.Answer(hero, Pick(hero, Enemy));
            world.AnswerToCompletion(spaces, Pick(spaces, new HexCoordinate(1, 1)));

            Assert.That(world.Where(FriendlyNear), Is.EqualTo(new HexCoordinate(1, 1)));
        }

        /// <summary>
        /// A stale answer is refused: the destinations are re-derived when the answer arrives rather
        /// than trusted from the moment the question opened, and a space the Place never offered has no
        /// invocation to forge.
        /// </summary>
        [Test]
        public void CloseQuarters_OffersNoInvocationForASpaceThePlaceNeverAdmitted()
        {
            var world = World.Melee(RowennaCardContent.CloseQuarters);

            var spaces = world.ToFirstBulletDestinations(FriendlyNear, Enemy);

            Assert.That(
                spaces.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    invocation.Candidate.Value.Coordinate.Equals(RowennaAt)),
                Is.False,
                "She is standing there, so it was never one of the destinations.");
        }

        // -------------------------------------------------------------------------------------------
        // Opening Shots and Opening Volley
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The candidate set is exactly the enemy heroes inside the printed Radius whose own discard
        /// pile is empty, read while hers is empty too. Every exclusion here is a different half of the
        /// one printed clause.
        /// </summary>
        [Test]
        public void OpeningShots_OffersExactlyTheEnemyHeroesWhoseDiscardIsEmpty()
        {
            var world = World.Shots(
                RowennaCardContent.OpeningShots,
                discards: new[] { (SecondEnemy, 1) });

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { Enemy.Value }),
                "The second enemy holds a card in his own discard, so the clause does not admit " +
                "him; the ally is the wrong side and Rowenna is not an enemy hero.");
        }

        /// <summary>
        /// Her OWN pile empties the list at once, which is the half of the clause no earlier narrowing
        /// in this catalog has: a card in her discard leaves this face no legal target on any board,
        /// and a mandatory selection with nothing to name is impossible rather than offered.
        /// </summary>
        [Test]
        public void OpeningShots_HasNoLegalTargetAtAllWhileHerOwnDiscardHoldsACard()
        {
            var world = World.Shots(
                RowennaCardContent.OpeningShots,
                discards: new[] { (Rowenna, 1) });

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True);
            Assert.That(world.HandCount(Enemy), Is.EqualTo(2),
                "and nobody discarded anything.");
        }

        /// <summary>
        /// "In radius" is the printed icon: an enemy hero with an empty pile standing outside it is not
        /// a candidate.
        /// </summary>
        [Test]
        public void OpeningShots_LeavesOutAnEmptyHandedEnemyOutsideThePrintedRadius()
        {
            var world = World.Shots(
                RowennaCardContent.OpeningShots, secondEnemyAt: FarAway);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { Enemy.Value }));
        }

        /// <summary>
        /// "That hero discards a card" is HIS decision from HIS own hand - p.10's rule that a step's
        /// choice belongs to whoever performs it - so the prompt opens on his seat and offers his cards
        /// rather than hers.
        /// </summary>
        [Test]
        public void OpeningShots_PutsTheDiscardToThatHeroAndOffersHisOwnHand()
        {
            var world = World.Shots(RowennaCardContent.OpeningShots);

            var target = world.RunToPause();
            var card = world.Answer(target, Pick(target, Enemy));

            Assert.That(card.ChooserId, Is.EqualTo(Enemy));
            Assert.That(CardIds(card), Is.EqualTo(new[]
            {
                "rowenna-red-enemy-hand-a", "rowenna-red-enemy-hand-b"
            }));

            var finished = world.AnswerToCompletion(card, Pick(card, "rowenna-red-enemy-hand-a"));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.InDiscard("rowenna-red-enemy-hand-a"), Is.True);
            Assert.That(world.HandCount(Enemy), Is.EqualTo(1));
            Assert.That(world.HandCount(Rowenna), Is.EqualTo(2), "Hers is not the hand that paid.");
        }

        /// <summary>
        /// "If able" is the skipping kind: a named hero holding nothing gives nothing, no prompt opens,
        /// and the Skill resolves. He is still a legal TARGET - the clause is about his discard pile,
        /// not about his hand - which is why the selection declares no consumer.
        /// </summary>
        [Test]
        public void OpeningShots_NamesAnEmptyHandedHeroAndResolvesWithoutADiscard()
        {
            var world = World.Shots(
                RowennaCardContent.OpeningShots, hands: new[] { (Enemy, 0) });

            var target = world.RunToPause();

            Assert.That(Named(target), Has.Member(Enemy.Value),
                "An empty hand is not an empty discard pile, so the clause still admits him.");

            var finished = world.AnswerToCompletion(target, Pick(target, Enemy));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.HandCount(Enemy), Is.Zero);
        }

        /// <summary>
        /// Opening Volley prints one sentence more, and it is p.10's repeat: hers to take or decline,
        /// requested for the whole Skill, with no "different target" clause because the predicate does
        /// that work by itself.
        /// </summary>
        [Test]
        public void OpeningVolley_OffersThePrintedRepeatOnceTheDiscardIsTaken()
        {
            var world = World.Shots(RowennaCardContent.OpeningVolley);

            var target = world.RunToPause();
            var card = world.Answer(target, Pick(target, Enemy));
            var repeat = world.Answer(card, Pick(card, "rowenna-red-enemy-hand-a"));

            Assert.That(repeat.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(repeat.ChooserId, Is.EqualTo(Rowenna),
                "p.10 gives an unassigned decision to the card's controller, and 'May repeat once' " +
                "assigns none.");

            var finished = world.SubmitToCompletion(repeat.ConfirmInvocation);

            Assert.That(finished.RepeatRequested.IsRequested, Is.True);
            Assert.That(
                finished.RepeatRequested.Binding, Is.EqualTo(RepeatTargetBinding.Unbound),
                "The face prints no 'different target', and it needs none: the hero who just " +
                "discarded owns a card in his discard and the predicate drops him by itself.");
        }

        /// <summary>
        /// And that is measurable rather than argued. The repeated instance re-derives its own
        /// candidates from the piles as they now stand: the hero who discarded is gone, Rowenna - who
        /// discarded nothing - is still empty, and the second eligible hero is what the repeat is for.
        /// </summary>
        [Test]
        public void OpeningVolley_ReReadsThePredicateSoTheFirstTargetDropsOutOfTheRepeat()
        {
            var world = World.Shots(RowennaCardContent.OpeningVolley);

            var target = world.RunToPause();
            Assert.That(Named(target), Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }));

            var card = world.Answer(target, Pick(target, Enemy));
            var repeat = world.Answer(card, Pick(card, "rowenna-red-enemy-hand-a"));
            world.SubmitToCompletion(repeat.ConfirmInvocation);

            var repeated = world.RunRepeat();

            Assert.That(Named(repeated), Is.EqualTo(new[] { SecondEnemy.Value }),
                "He discarded, so his own pile is no longer empty; hers still is, because a forced " +
                "discard is not hers to pay.");
        }

        /// <summary>
        /// And the other side of the same re-derivation: a repeated instance that can name nobody
        /// resolves having done nothing at all. The decision to ask for the repeat is hers and the
        /// frame offers it either way - a printed repeat is bounded by its own frame, which is
        /// <see cref="EffectResolutionContext.RepeatIsUnavailableFor"/>'s declared boundary - so this
        /// is what the instance it asks for actually does.
        /// </summary>
        [Test]
        public void OpeningVolley_ResolvesTheRepeatedInstanceWithoutATargetWhenNobodyQualifies()
        {
            var world = World.Shots(
                RowennaCardContent.OpeningVolley, secondEnemyAt: FarAway);

            var target = world.RunToPause();
            var card = world.Answer(target, Pick(target, Enemy));
            var repeat = world.Answer(card, Pick(card, "rowenna-red-enemy-hand-a"));
            world.SubmitToCompletion(repeat.ConfirmInvocation);

            var repeated = world.RunRepeatToCompletion();

            Assert.That(repeated.IsComplete, Is.True,
                "The only hero who qualified has discarded, so the mandatory selection is " +
                "impossible and the repeated instance stops there.");
            Assert.That(world.HandCount(Enemy), Is.EqualTo(1), "and nobody discarded twice.");
        }

        /// <summary>
        /// A restore taken while the OPPONENT's discard is pending resumes against freshly rebuilt
        /// content and the prompt is still his.
        /// </summary>
        [Test]
        public void OpeningShots_ResumesThroughARestoreWhileTheOpponentsDiscardIsPending()
        {
            var world = World.Shots(RowennaCardContent.OpeningShots);

            var target = world.RunToPause();
            var card = world.Answer(target, Pick(target, Enemy));

            world.RebindTrustedContent();

            Assert.That(card.ChooserId, Is.EqualTo(Enemy));
            world.AnswerToCompletion(card, Pick(card, "rowenna-red-enemy-hand-b"));

            Assert.That(world.InDiscard("rowenna-red-enemy-hand-b"), Is.True);
        }

        /// <summary>
        /// A stale answer cannot name the same hero twice: the choice is gone once it is answered, and
        /// replaying it against the frame it left behind is refused rather than applied.
        /// </summary>
        [Test]
        public void OpeningShots_RefusesAStaleAnswerToAChoiceThatHasAlreadyClosed()
        {
            var world = World.Shots(RowennaCardContent.OpeningShots);

            var target = world.RunToPause();
            var naming = Pick(target, Enemy);
            world.Answer(target, naming);

            var stale = world.Submit(naming);

            Assert.That(stale.Accepted, Is.False);
            Assert.That(world.HandCount(Enemy), Is.EqualTo(2), "and nothing was discarded by it.");
        }

        // -------------------------------------------------------------------------------------------
        // The vocabulary this block moved
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The one new declaration this block minted is IN what the build registers, which is
        /// milestone 7.9's gate satisfied: milestone 8.57g deals her, so her three Green Movement
        /// faces are cards a configured match puts in a hand. Both of Rowenna's Places moved together,
        /// which is how 8.57d asserted them and how this asserts them back.
        /// </summary>
        [Test]
        public void ThePlaceBesideANamedHeroInRadius_IsRegisteredNowThatADealtCardStartsIt()
        {
            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities.TryLoad(
                    SyntheticPlacementCapabilities.PlaceInRadiusBesideANamedHeroV1, out _),
                Is.True,
                "8.57g deals her, and Close Quarters, Melee and Grand Melee are what start it.");

            Assert.That(
                SyntheticPlacementCapabilities.RegisteredHandlers()
                    .Select(handler => handler.Reference),
                Is.SupersetOf(new[]
                {
                    SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1,
                    SyntheticPlacementCapabilities.PlaceInRadiusBesideANamedHeroV1
                }),
                "Exactly her two, moved together, which is the half a dealing could get wrong by " +
                "registering one and forgetting the other.");

            // Milestone 9.30e refilled the file's deferred list for another hero (Gydion's Dimension
            // Door), so the claim is narrowed to what her dealing means: neither of hers is held back.
            Assert.That(
                SyntheticPlacementCapabilities.DeferredHandlers().Select(handler => handler.Reference),
                Has.No.Member(SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1)
                    .And.No.Member(SyntheticPlacementCapabilities.PlaceInRadiusBesideANamedHeroV1),
                "and neither of hers is in the deferred list any more.");
        }

        /// <summary>
        /// The one parameter this block added to a REGISTERED capability is optional, which is what
        /// makes every body composed before this milestone offer exactly what it did - and it is a
        /// Boolean rather than a count, because an unbound optional Integer reads as zero and "at most
        /// zero cards in the discard" would then be indistinguishable from no narrowing at all.
        /// </summary>
        [Test]
        public void TheBothDiscardPilesEmptyNarrowing_IsCarriedAsAnOptionalBoolean()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;

            var radiusArm = catalog.Load(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var narrowing = radiusArm.Schema.Parameters.Single(parameter =>
                parameter.Name ==
                SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName);

            Assert.That(narrowing.IsRequired, Is.False);
            Assert.That(narrowing.ValueKind, Is.EqualTo(EffectValueKind.Boolean));

            // Milestone 8.88. The Range arm declares it too, optional - the family composes every
            // clause on every arm it can mean something on.
            Assert.That(
                catalog.Load(SyntheticTargetingCapabilities.RangeHeroTargetV1)
                    .Schema.Parameters
                    .Single(parameter => parameter.Name ==
                        SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName)
                    .IsRequired,
                Is.False,
                "and optional on the Range arm as well, so no Range selection changes.");
        }

        /// <summary>
        /// Five faces composed and nothing registered. The whole set is still deliberately partial, and
        /// the count is stated here so a face added without a definition fails rather than being
        /// noticed at the dealing.
        /// </summary>
        [Test]
        public void TheFiveGreenFaces_AreComposedAndNoneOfThemIsRegistered()
        {
            var green = new[]
            {
                RowennaCardContent.CloseQuarters,
                RowennaCardContent.Melee,
                RowennaCardContent.OpeningShots,
                RowennaCardContent.GrandMelee,
                RowennaCardContent.OpeningVolley
            };

            foreach (var cardId in green)
            {
                var card = RowennaCardContent.Definition(cardId);
                Assert.That(card.Progression.Color, Is.EqualTo(CardColor.Green), cardId.Value);
                Assert.That(
                    card.ActionDefinitions.Single(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary).EffectRoot,
                    Is.Not.Null,
                    cardId.Value + " composes a body.");
            }

            // Milestone 8.57f added p.20's boxed Earn the Spurs, which is the nineteenth and last face
            // her shard records, so the set these five sit in is whole. The count is pinned rather than
            // derived on purpose: it moved once per composing block and has nowhere left to move.
            Assert.That(RowennaCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(RowennaCardContent.DealtDefinitions.Count, Is.EqualTo(18),
                "and the one face outside the dealt eighteen is the boxed Handicap, never a Green.");
        }

        /// <summary>
        /// Every pause these five bodies can open has words authored for it. The catalog is not wired
        /// up until she is dealt - <c>DecisionLabelCatalog</c> is reachable content - so this case is
        /// what keeps the declarations honest in the meantime.
        /// </summary>
        [Test]
        public void EveryDecisionTheseFacesOpen_HasAnAuthoredLabel()
        {
            var declared = RowennaEffectContent.DecisionLabels()
                .Select(label => label.CardId.Value + "/" + label.NodeId.Value)
                .ToArray();

            foreach (var cardId in new[]
            {
                RowennaCardContent.CloseQuarters,
                RowennaCardContent.Melee,
                RowennaCardContent.GrandMelee
            })
            {
                var passes = cardId.Equals(RowennaCardContent.GrandMelee)
                    ? new[] { "first", "second" }
                    : new[] { "first" };
                foreach (var pass in passes)
                {
                    Assert.That(declared, Has.Member(cardId.Value + "/melee-" + pass + "-may"));
                    Assert.That(declared, Has.Member(cardId.Value + "/melee-" + pass + "-friendly"));
                    Assert.That(declared, Has.Member(cardId.Value + "/melee-" + pass + "-enemy"));
                }
            }

            Assert.That(
                declared,
                Has.Member(RowennaCardContent.OpeningVolley.Value + "/volley-repeat"));
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static int PrintedRadius(CardDefinitionId cardId) =>
            RowennaCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .BaseValues.Single(value => value.Channel == PrintedValueChannel.Radius)
                .Amount;

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.Kind == EffectValueKind.Hero
                    ? candidate.Value.HeroId.Value
                    : candidate.Value.MinionId.Value)
                .ToArray();

        private static string[] Arms(PendingEffectChoice choice) =>
            choice.Candidates.Select(candidate => candidate.OptionNodeId.Value).ToArray();

        private static HexCoordinate[] Spaces(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();

        private static string[] CardIds(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.CardInstanceId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HeroId hero) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero));

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, MinionId minion) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                invocation.Candidate.Value.MinionId.Equals(minion));

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HexCoordinate space) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                invocation.Candidate.Value.Coordinate.Equals(space));

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, string cardInstanceId) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Value == cardInstanceId);

        private static EffectChoiceCommand PickArm(PendingEffectChoice choice, string armNodeId) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.OptionNodeId.IsDefined &&
                invocation.Candidate.OptionNodeId.Value == armNodeId);

        /// <summary>
        /// The empty playable neighbours of a space, derived from the live board rather than written
        /// down - a case that listed them would be asserting its own arithmetic.
        /// </summary>
        private static HexCoordinate[] EmptyNeighboursOf(HexCoordinate at, World world) =>
            at.GetNeighbors().Where(world.IsEmpty).ToArray();

        /// <summary>
        /// A live modification set carrying one immunity, built the way a match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"rowenna-green-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"RowennaGreen{activity}Immunity",
                $"rowenna-green-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"rowenna-green-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("rowenna-green-immunity-source"),
                new ActionDefinitionId("rowenna-green-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                Participants(),
                new TieBreakerState(TeamSide.Red),
                GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet.None, RuleObject.None, null,
                null);
            Assert.That(
                set.IsImmuneTo(
                    RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Rowenna)),
                Is.True,
                "The fixture must actually hold the immunity, or the case above proves nothing.");
            return set;
        }

        /// <summary>
        /// Four seats: two Blue, so the board holds a friendly hero who is not a minion, and two Red,
        /// so a case can stand two enemy heroes beside her or one of them out of reach.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var rowennaPlayer = new PlayerId("rowenna-player");
            var allyPlayer = new PlayerId("rowenna-ally-player");
            var enemyPlayer = new PlayerId("rowenna-enemy-player");
            var secondEnemyPlayer = new PlayerId("rowenna-second-enemy-player");

            return new MatchParticipants(
                new[]
                {
                    new Team(BlueTeam, TeamSide.Blue),
                    new Team(RedTeam, TeamSide.Red)
                },
                new[]
                {
                    new Player(rowennaPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(secondEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Rowenna, rowennaPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(SecondEnemy, secondEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// One staged position and the frame walking one of Rowenna's Green bodies over it.
        ///
        /// <para>It holds the live card instances and the board, and rebuilds the trusted content on
        /// demand so a case can ask what a restore would ask. Nothing here is a configured match: see
        /// the fixture's own remarks for why, and for who owns the part this cannot reach.</para>
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly bool movementText;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly List<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private readonly HeroCoinLedger ledger;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                CardDefinitionId cardId,
                bool movementText,
                IReadOnlyList<CardDefinition> definitions,
                List<CardInstance> instances,
                MatchParticipants participants,
                AdjacentMoveAuthority board,
                RuleModificationSet modifications,
                HeroCoinLedger ledger,
                MatchScoreboard scoreboard)
            {
                this.cardId = cardId;
                this.movementText = movementText;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.board = board;
                this.modifications = modifications;
                this.ledger = ledger;
                Scoreboard = scoreboard;
                components = new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board, board);
                context = BuildContext();
            }

            internal MatchScoreboard Scoreboard { get; }

            /// <summary>
            /// The melee board: one enemy hero beside her, one far away, three friendly minions at two,
            /// three and four spaces, and three enemy minions at two, two and four.
            /// </summary>
            internal static World Melee(
                CardDefinitionId cardId,
                HexCoordinate? enemyAt = null,
                HexCoordinate? secondEnemyAt = null,
                HexCoordinate[] obstaclesAt = null,
                (MinionId, HexCoordinate, TeamSide)[] minions = null,
                RuleModificationSet modifications = null,
                bool withScoreboard = false) =>
                Create(
                    cardId,
                    true,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, enemyAt ?? EnemyAt),
                        new HeroOccupancy(SecondEnemy, secondEnemyAt ?? FarAway)
                    },
                    minions ?? MeleeMinions,
                    obstaclesAt ?? Array.Empty<HexCoordinate>(),
                    Array.Empty<(HeroId, int)>(),
                    null,
                    modifications,
                    withScoreboard);

            /// <summary>
            /// The opening-shots board: two enemy heroes inside her printed Radius 3, every pile empty
            /// and every hero holding two cards, so a case states only what it changes.
            /// </summary>
            internal static World Shots(
                CardDefinitionId cardId,
                HexCoordinate? secondEnemyAt = null,
                (HeroId, int)[] discards = null,
                (HeroId, int)[] hands = null) =>
                Create(
                    cardId,
                    false,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(SecondEnemy, secondEnemyAt ?? SecondEnemyInRadius)
                    },
                    Array.Empty<(MinionId, HexCoordinate, TeamSide)>(),
                    Array.Empty<HexCoordinate>(),
                    discards ?? Array.Empty<(HeroId, int)>(),
                    hands,
                    null,
                    false);

            private static World Create(
                CardDefinitionId cardId,
                bool movementText,
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At, TeamSide Side)> minions,
                IReadOnlyList<HexCoordinate> obstaclesAt,
                IReadOnlyList<(HeroId Hero, int Count)> discards,
                IReadOnlyList<(HeroId Hero, int Count)> hands,
                RuleModificationSet modifications,
                bool withScoreboard)
            {
                var participants = Participants();
                var ledger = new HeroCoinLedger(participants);
                MatchScoreboard scoreboard = null;
                if (withScoreboard)
                {
                    scoreboard = new MatchScoreboard(participants);
                    ledger.BindScoreboard(scoreboard);
                }

                var pile = PileDefinition();
                var definitions = RowennaCardContent.AllDefinitions
                    .Concat(new[] { pile })
                    .ToArray();

                var instances = new List<CardInstance>();
                foreach (var hero in new[] { Rowenna, Ally, Enemy, SecondEnemy })
                {
                    Add(instances, pile, hero, "discard",
                        Count(discards, hero, 0), CardStateFacts.DiscardedUnplayed());
                    Add(instances, pile, hero, "hand",
                        Count(hands, hero, 2), CardStateFacts.InitialInHand());
                }

                return new World(
                    cardId,
                    movementText,
                    definitions,
                    instances,
                    participants,
                    Board(heroes, minions, obstaclesAt),
                    modifications ?? RuleModificationSet.Empty,
                    ledger,
                    scoreboard);
            }

            private static int Count(
                IReadOnlyList<(HeroId Hero, int Count)> stated, HeroId hero, int fallback)
            {
                if (stated == null)
                {
                    return fallback;
                }

                foreach (var row in stated)
                {
                    if (row.Hero.Equals(hero))
                    {
                        return row.Count;
                    }
                }

                return fallback;
            }

            private static void Add(
                ICollection<CardInstance> into,
                CardDefinition definition,
                HeroId owner,
                string where,
                int count,
                CardStateFacts state)
            {
                for (var index = 0; index < count; index++)
                {
                    into.Add(new CardInstance(
                        new CardInstanceId($"{owner.Value}-{where}-{(char)('a' + index)}"),
                        definition.Reference,
                        owner,
                        state));
                }
            }

            /// <summary>
            /// A definition for the cards sitting in the piles and hands. It is a synthetic fixture card
            /// rather than one of Rowenna's, because what these cases are about is whether a pile is
            /// empty and whose it is - not which face it holds.
            /// </summary>
            private static CardDefinition PileDefinition() =>
                new CardDefinition(
                    new CardDefinitionId("rowenna-green-pile-card"),
                    "rev-1",
                    "TEST - Rowenna Green Pile Card",
                    new InitiativeValue(7),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[]
                    {
                        new ActionDefinition(
                            new ActionDefinitionId("pile-movement"),
                            ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedPrimary,
                            new[]
                            {
                                new PrintedValueDefinition(PrintedValueChannel.Movement, 2)
                            },
                            new ActionDefinitionPolicy(
                                CardTextApplicationPolicy.Applies, false, true))
                    });

            private static AdjacentMoveAuthority Board(
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At, TeamSide Side)> minions,
                IReadOnlyList<HexCoordinate> obstaclesAt)
            {
                var zone = new ZoneId("rowenna-green-zone");
                var spaces = new List<BoardMapSpace>();
                for (var q = -8; q <= 8; q++)
                {
                    for (var r = -8; r <= 8; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 8)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, zone));
                    }
                }

                var map = new BoardMapDefinition(
                    Map, 1, "Rowenna Close Quarters Map",
                    spaces,
                    new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

                return new AdjacentMoveAuthority(
                    map,
                    heroes,
                    obstaclesAt
                        .Select((at, index) => new ObstacleOccupancy(
                            new ObstacleId($"rowenna-green-obstacle-{index:00}"), at))
                        .ToArray(),
                    minions
                        .Select(minion => new QuickGameMinionSetupState(
                            minion.Id,
                            minion.Side == TeamSide.Blue ? BlueTeam : RedTeam,
                            minion.Side,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(minion.At)),
                            minion.At))
                        .ToArray());
            }

            private static SpaceId SpaceIdFor(HexCoordinate at) =>
                new SpaceId($"q{at.Q}_r{at.R}");

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            internal void RebindTrustedContent() => context = BuildContext();

            private EffectResolutionContext BuildContext()
            {
                var definition = movementText
                    ? RowennaEffectContent.MovementTextFor(
                        cardId, RowennaCardContent.ContentRevision)
                    : RowennaEffectContent.SkillEffectFor(
                        cardId, RowennaCardContent.ContentRevision);
                return new EffectResolutionContext(
                    definition,
                    Catalog(),
                    participants,
                    null,
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    movementText ? ActionKind.Movement : ActionKind.Skill,
                    components: components,
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        ledger));
            }

            /// <summary>
            /// This build's whole synthetic vocabulary plus whatever this file still defers, which
            /// since milestone 8.57g's dealing is nothing at all - see the fixture's own remarks. It
            /// is written as that SUM rather than as <see cref="MatchContentCatalogs"/>' registered
            /// set because the faces under test named a deferred capability while the series ran, and
            /// the sum means the same thing on both sides of the dealing. Naming the DEFERRED list
            /// rather than the whole one is milestone 8.40j's BA-18: a fixture that hands back
            /// already-registered handlers is refused for a duplicate reference.
            /// </summary>
            private static EffectCapabilityCatalog Catalog() =>
                SyntheticEffectCapabilities.CreateCatalog(
                    SyntheticPlacementCapabilities.DeferredHandlers());

            internal EffectResolutionStepResult Run() =>
                Start(ActionInvocationContext.PrintedPrimary, RepeatContinuation.FirstInstance);

            private EffectResolutionStepResult Start(
                ActionInvocationContext invocation, RepeatContinuation continuation)
            {
                var definition = RowennaCardContent.Definition(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("rowenna-green-resolution"),
                    new CardInstanceId("rowenna-green-source"),
                    definition.Reference,
                    movementText
                        ? RowennaCardContent.PrimaryMovement
                        : RowennaCardContent.PrimarySkill,
                    Rowenna,
                    Rowenna,
                    invocation,
                    context,
                    continuation);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            /// <summary>
            /// The instance p.10's repeat asks for, started against the board and the piles the first
            /// one left - which is what makes the predicate's re-derivation measurable.
            /// </summary>
            internal PendingEffectChoice RunRepeat()
            {
                var result = RunRepeatToCompletion();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult RunRepeatToCompletion() =>
                Start(ActionInvocationContext.Repeated, RepeatContinuation.Next(1));

            internal PendingEffectChoice RunToPause()
            {
                var result = Run();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            /// <summary>Through the optional step and the modal into bullet 1's minion prompt.</summary>
            internal PendingEffectChoice ToFirstBullet()
            {
                var may = RunToPause();
                var modal = Answer(may, may.ConfirmInvocation);
                return Answer(modal, PickArm(modal, "melee-first-friendly"));
            }

            /// <summary>Through bullet 1's minion and hero prompts into its destination prompt.</summary>
            internal PendingEffectChoice ToFirstBulletDestinations(MinionId minion, HeroId hero)
            {
                var minions = ToFirstBullet();
                var heroes = Answer(minions, Pick(minions, minion));
                return Answer(heroes, Pick(heroes, hero));
            }

            /// <summary>Through the optional step and the modal into bullet 2's minion prompt.</summary>
            internal PendingEffectChoice ToSecondBullet()
            {
                var may = RunToPause();
                var modal = Answer(may, may.ConfirmInvocation);
                return Answer(modal, PickArm(modal, "melee-first-enemy"));
            }

            internal PendingEffectChoice Answer(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.False, "This case expects another question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult AnswerToCompletion(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            internal EffectResolutionStepResult SubmitToCompletion(EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True);
                return result;
            }

            internal EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            internal HexCoordinate Where(HeroId hero) =>
                board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(hero), out var at)
                    ? at
                    : throw new InvalidOperationException($"{hero.Value} stands nowhere.");

            internal HexCoordinate Where(MinionId minion) =>
                board.TryGetObjectCoordinate(
                    BoardObjectReference.ForMinion(minion), out var at)
                    ? at
                    : throw new InvalidOperationException($"{minion.Value} stands nowhere.");

            internal TeamSide SideOf(MinionId minion) =>
                board.Minions.Single(occupancy => occupancy.MinionId.Equals(minion)).TeamSide;

            internal StandardMinionType TypeOf(MinionId minion) =>
                board.Minions.Single(occupancy => occupancy.MinionId.Equals(minion)).MinionType;

            internal bool IsEmpty(HexCoordinate at) => board.IsEmptyPlayableSpace(at);

            internal int HandCount(HeroId hero) =>
                instances.Count(card =>
                    card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

            internal bool InDiscard(string cardInstanceId) =>
                instances.Single(card => card.Id.Value == cardInstanceId).State.Location ==
                CardLocation.DiscardPile;

            internal int MinionsKilled(HeroId hero) =>
                Scoreboard.Entries.Single(entry => entry.HeroId.Equals(hero)).MinionsKilled;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Min the Dragonmonk's five Red faces - the Stances - and the three printed clauses they carry
    /// after rulebook p.14's step 7. Milestone 8.61c.
    ///
    /// <para><b>The board every case measures on.</b> Min stands at the origin and the Attack's target
    /// - the enemy hero - stands one space East of her at (1,0). Those two positions decide every
    /// number below and are worth stating once: her six neighbours are the spaces she can step to, the
    /// target's six are the ring the move clause names, and the two sets intersect in exactly two
    /// spaces, (1,-1) and (0,1). The space beyond the target, (2,0), is adjacent to it and is NOT
    /// reachable on foot - every two-step route to it runs through the target's own space - which is
    /// what makes this board able to tell a p.11 move apart from a Place.</para>
    ///
    /// <para>Composition and the printed set are asserted here too, because nothing else can see them:
    /// none of her faces is dealt or registered until milestone 8.61j.</para>
    /// </summary>
    public sealed class MinStanceTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);

        /// <summary>The Attack's target: one space East of Min, which is p.13's "adjacent to you".</summary>
        private static readonly HexCoordinate TargetAt = new HexCoordinate(1, 0);

        /// <summary>The two spaces that are both one step from Min and one step from the target.</summary>
        private static readonly HexCoordinate[] BesideBoth =
        {
            new HexCoordinate(1, -1), new HexCoordinate(0, 1)
        };

        /// <summary>The two more that Dragon's second space reaches, by walking round the target.</summary>
        private static readonly HexCoordinate[] BesideBothAtTwo =
        {
            new HexCoordinate(2, -1), new HexCoordinate(1, 1)
        };

        /// <summary>Adjacent to the target and behind it, so only a Place or a Fast Travel could reach it.</summary>
        private static readonly HexCoordinate BehindTheTarget = new HexCoordinate(2, 0);

        private static readonly MinionId EnemyMinion = new MinionId("min-stance-blue-minion");
        private static readonly MinionId FriendlyMinion = new MinionId("min-stance-red-minion");

        private static readonly MapId MapIdentity = new MapId("min-stance-map");

        // =========================================================================================
        // The printed set
        // =========================================================================================

        [Test]
        public void TheFiveRedsCarryTheirPrintedValuesTiersAndItems()
        {
            var expected = new[]
            {
                new { Id = MinCardContent.CraneStance, Name = "Crane Stance", Initiative = 9,
                    Defense = 5, Movement = 5, Attack = 3, Radius = 0, Tier = CardTier.I,
                    Item = PrintedValueChannel.None },
                new { Id = MinCardContent.TigerStance, Name = "Tiger Stance", Initiative = 9,
                    Defense = 5, Movement = 5, Attack = 4, Radius = 0, Tier = CardTier.II,
                    Item = PrintedValueChannel.Initiative },
                new { Id = MinCardContent.ViperStance, Name = "Viper Stance", Initiative = 9,
                    Defense = 5, Movement = 5, Attack = 5, Radius = 4, Tier = CardTier.II,
                    Item = PrintedValueChannel.Defense },
                new { Id = MinCardContent.DragonStance, Name = "Dragon Stance", Initiative = 10,
                    Defense = 6, Movement = 5, Attack = 4, Radius = 0, Tier = CardTier.III,
                    Item = PrintedValueChannel.Movement },
                new { Id = MinCardContent.CobraStance, Name = "Cobra Stance", Initiative = 10,
                    Defense = 6, Movement = 5, Attack = 5, Radius = 4, Tier = CardTier.III,
                    Item = PrintedValueChannel.Defense }
            };

            Assert.That(MinCardContent.StanceDefinitions.Count, Is.EqualTo(5));
            foreach (var row in expected)
            {
                var definition = MinCardContent.StanceDefinition(row.Id);
                Assert.That(definition.DisplayName, Is.EqualTo(row.Name));
                Assert.That(definition.Initiative.Value, Is.EqualTo(row.Initiative));
                Assert.That(definition.Progression.Color, Is.EqualTo(CardColor.Red));
                Assert.That(definition.Progression.Tier, Is.EqualTo(row.Tier));
                Assert.That(definition.Progression.ItemIcon, Is.EqualTo(row.Item),
                    "p.15: the face not taken becomes its printed item, so the icon is content.");

                var attack = Action(definition, MinCardContent.PrimaryAttack);
                Assert.That(attack.Kind, Is.EqualTo(ActionKind.Attack));
                Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged));
                Assert.That(Printed(attack, PrintedValueChannel.Attack), Is.EqualTo(row.Attack));
                Assert.That(Printed(attack, PrintedValueChannel.Radius), Is.EqualTo(row.Radius),
                    "MI-05: a missing Radius icon is absent, never zero - which this reads back as 0 " +
                    "only because the face declares no such printed value at all.");
                Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack),
                    "All five print \"After the attack:\", which is p.14's step 7.");
                Assert.That(
                    Printed(Action(definition, MinCardContent.SecondaryMovement),
                        PrintedValueChannel.Movement),
                    Is.EqualTo(row.Movement));
                Assert.That(
                    Printed(Action(definition, MinCardContent.SecondaryDefense),
                        PrintedValueChannel.Defense),
                    Is.EqualTo(row.Defense));
            }
        }

        [Test]
        public void EveryStanceTargetsAUnitAdjacentToHerAndNarrowsNothingElse()
        {
            foreach (var definition in MinCardContent.StanceDefinitions)
            {
                var policy = Action(definition, MinCardContent.PrimaryAttack).AttackTargetPolicy;
                Assert.That(policy.HasValue, Is.True);
                Assert.That(policy.Value.Reach, Is.EqualTo(AttackTargetReach.AdjacentOnly),
                    "p.13's own reach for a non-Ranged Attack, \"even if the card has a radius icon\" " +
                    "- which is why Viper's and Cobra's printed 4 reaches their card TEXT alone.");
                Assert.That(policy.Value.TargetableUnits,
                    Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                    "\"a unit\" is p.19's word for both kinds.");
                Assert.That(policy.Value.Line, Is.EqualTo(AttackTargetLine.AnyLine));
            }
        }

        [Test]
        public void TheRedLadderForksTwiceAndEndsAtTheTwoTierThrees()
        {
            Assert.That(
                MinCardContent.StanceDefinition(MinCardContent.CraneStance)
                    .Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { MinCardContent.TigerStance, MinCardContent.ViperStance }));
            foreach (var second in new[] { MinCardContent.TigerStance, MinCardContent.ViperStance })
            {
                Assert.That(
                    MinCardContent.StanceDefinition(second).Progression.NextTierCandidateIds,
                    Is.EquivalentTo(new[] { MinCardContent.DragonStance, MinCardContent.CobraStance }),
                    "p.15's ladder is per colour, so either Tier II reaches either Tier III.");
            }

            foreach (var third in new[] { MinCardContent.DragonStance, MinCardContent.CobraStance })
            {
                Assert.That(
                    MinCardContent.StanceDefinition(third).Progression.NextTierCandidateIds,
                    Is.Empty, "Tier III is the top of the ladder.");
            }
        }

        [Test]
        public void HerAggregateNowCarriesTheSilverAndTheFiveRedsUnderOnePlaceholderRevision()
        {
            var six = new[] { MinCardContent.SmokeBomb }
                .Concat(MinCardContent.StanceDefinitions.Select(card => card.Id))
                .ToArray();
            Assert.That(
                MinCardContent.AllDefinitions.Select(definition => definition.Id),
                Is.SupersetOf(six),
                "Six of nineteen: this block adds its five to milestone 8.61a's aggregate rather " +
                "than declaring a second AllDefinitions beside it. On the integrated tree the " +
                "aggregate also carries every other block's faces, so this reads containment.");
            Assert.That(
                MinCardContent.AllDefinitions
                    .Where(definition => six.Contains(definition.Id))
                    .Select(definition => definition.Reference.ContentRevision)
                    .Distinct().Single(),
                Is.EqualTo(MinComponentContent.PrepRevision),
                "Still the deliberately malformed placeholder on these six; 8.61i mints the one " +
                "real string for all nineteen.");
        }

        [Test]
        public void EveryComposedStanceBodyIsAValidDefinitionRootedOnItsOwnCard()
        {
            foreach (var definition in MinCardContent.StanceDefinitions)
            {
                var body = MinEffectContent.StanceTextFor(
                    definition.Id, MinComponentContent.PrepRevision);
                Assert.That(body, Is.Not.Null);
                Assert.That(body.Id.Value, Is.EqualTo(definition.Id.Value + "-attack-text"),
                    "Five bodies rather than one shape referenced five times, so a paused " +
                    "resolution can name the face it is inside.");
                Assert.That(EffectDefinitionValidator.Validate(body, Capabilities), Is.Empty);
            }

            Assert.That(
                MinEffectContent.StanceTextFor(MinCardContent.SmokeBomb, "rev"), Is.Null,
                "A face this block does not own answers nothing rather than something wrong.");
        }

        // =========================================================================================
        // "Push an enemy unit adjacent to you up to 3 spaces"
        // =========================================================================================

        [Test]
        public void ThePushOffersEveryAdjacentEnemyUnitAndNeitherHerOwnSideNorADistantOne()
        {
            var rider = new Rider(
                MinCardContent.CraneStance,
                enemyMinionAt: new HexCoordinate(0, 1),
                friendlyMinionAt: new HexCoordinate(-1, 0),
                enemyAllyAt: new HexCoordinate(2, 0));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("crane-subject"));
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Hero(Enemy), EffectValue.Minion(EnemyMinion)
                }),
                "\"an enemy unit adjacent to you\" is p.19's unit on the opposing side at a " +
                "distance of one: her own minion is not enemy, and the enemy ally two spaces away " +
                "is not adjacent.");
        }

        [Test]
        public void ThePushTravelsDirectlyAwayFromHerAndTheDistanceIsTheOnlyDecision()
        {
            var rider = new Rider(MinCardContent.CraneStance);
            rider.Pick(EffectValue.Hero(Enemy));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("crane-push"));
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.IntegerValue),
                Is.EqualTo(new[] { 0, 1, 2, 3 }),
                "\"up to 3 spaces\" makes the distance the decision and p.18 admits nought - " +
                "\"this can result in pushing it 0 spaces\".");

            rider.Pick(EffectValue.Integer(3));
            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Enemy), Is.EqualTo(new HexCoordinate(4, 0)),
                "p.18: a straight line directly away from the pushing unit.");
        }

        [Test]
        public void ThePushMayBeNothingAtAllWithoutBeingDeclined()
        {
            var rider = new Rider(MinCardContent.CraneStance);
            rider.Pick(EffectValue.Hero(Enemy));
            rider.Pick(EffectValue.Integer(0));

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Enemy), Is.EqualTo(TargetAt),
                "Nought is one of the numbers \"up to 3\" names, not a refusal of the step.");
        }

        [Test]
        public void ThePushStopsShortOfAnObstacleRatherThanFailing()
        {
            var rider = new Rider(MinCardContent.CraneStance, terrain: new[] { new HexCoordinate(3, 0) });
            rider.Pick(EffectValue.Hero(Enemy));
            rider.Pick(EffectValue.Integer(3));

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Enemy), Is.EqualTo(BehindTheTarget),
                "p.18: a pushed unit stops in front of an obstacle on its line.");
        }

        [Test]
        public void ThePushCanNameAMinionAndCarryItOutOfTheBattleZone()
        {
            // p.18: "displacing a minion miniature (by moving, pushing, or swapping it) has no impact
            // on the Minion Battle" - so nothing here refuses the push. What happens to a minion that
            // ends outside the zone is the ACTION boundary's own correction, which
            // MinionLeavesTheBattleZone_AndTheActionBoundaryCorrectsIt drives through a real one.
            var rider = new Rider(
                MinCardContent.CraneStance, enemyMinionAt: new HexCoordinate(0, 1),
                withBattleZone: true);
            rider.Pick(EffectValue.Minion(EnemyMinion));
            rider.Pick(EffectValue.Integer(3));

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.MinionSpace(EnemyMinion), Is.EqualTo(new HexCoordinate(0, 4)));
            Assert.That(
                BattleZoneQuery.IsInCurrentBattleZone(
                    rider.Board, SpaceAt(rider.Board, new HexCoordinate(0, 4))),
                Is.False,
                "The push put it outside, which is the precondition the correction exists for.");
        }

        [Test]
        public void AUnitImmuneToBeingPushedIsNotOfferedAsASubject()
        {
            var rider = new Rider(
                MinCardContent.CraneStance,
                enemyMinionAt: new HexCoordinate(0, 1),
                modifications: PushImmunityFor(Enemy));

            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value),
                Is.EqualTo(new[] { EffectValue.Minion(EnemyMinion) }),
                "p.18's immunity is asked as ObjectPush at the selection, so an immune unit is " +
                "greyed rather than offered and then refused.");
        }

        [Test]
        public void WithNoEnemyUnitBesideHerThePushIsAnImpossibleStepThatKeepsWhatCameBefore()
        {
            var rider = new Rider(
                MinCardContent.CraneStance, enemyAt: new HexCoordinate(4, 0), targetRemoved: false);

            Assert.That(rider.Step.IsComplete, Is.True, "Nothing was asked.");
            Assert.That(rider.Outcome("crane-subject"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.10: the sentence is not a `you may` and not an `if able`, so a step with nobody " +
                "to name stops the text at itself rather than being skipped in silence.");
            Assert.That(rider.HeroSpace(Enemy), Is.EqualTo(new HexCoordinate(4, 0)),
                "and nothing moved.");
        }

        [Test]
        public void ATargetTheAttackRemovedIsNeverPushedAndAnotherAdjacentEnemyStillIs()
        {
            var rider = new Rider(
                MinCardContent.CraneStance,
                targetRemoved: true,
                enemyMinionAt: new HexCoordinate(0, 1));

            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value),
                Is.EqualTo(new[] { EffectValue.Minion(EnemyMinion) }),
                "p.14 step 6 took the target off the board before step 7 ran, and a piece that is " +
                "not standing anywhere is not adjacent to anything.");

            rider.Pick(EffectValue.Minion(EnemyMinion));
            rider.Pick(EffectValue.Integer(2));
            Assert.That(rider.MinionSpace(EnemyMinion), Is.EqualTo(new HexCoordinate(0, 3)));
        }

        // =========================================================================================
        // "You may move 1 space [1 or 2 spaces] to a space adjacent to the target"
        // =========================================================================================

        [Test]
        public void TigersMoveOffersOnlyTheSpacesBesideTheTargetSheCanWalkToInOneStep()
        {
            var rider = new Rider(MinCardContent.TigerStance);

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("tiger-move-step"));
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EquivalentTo(BesideBoth),
                "One step from her AND one step from the target - the intersection of the two rings.");
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(BehindTheTarget),
                "p.11: the move follows the Movement action's own reachability, and every route to " +
                "the space behind the target runs through the target itself. A Place would reach " +
                "it; a card that says \"move\" does not.");
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(TargetAt),
                "Adjacency is a distance of ONE, so the target's own space is not on its own ring " +
                "- and it is occupied besides.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TigersMoveActuallyWalksToTheSpaceSheNames(int index)
        {
            var rider = new Rider(MinCardContent.TigerStance);
            rider.Pick(EffectValue.Space(BesideBoth[index]));

            Assert.That(rider.HeroSpace(Min), Is.EqualTo(BesideBoth[index]));
            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("tiger-push-subject"),
                "and the sentence carries on to the push.");
        }

        [Test]
        public void TigersMoveMayBeDeclinedAndThePushStillHappens()
        {
            var rider = new Rider(MinCardContent.TigerStance);
            rider.Answer(rider.Choice.DeclineInvocation);

            Assert.That(rider.HeroSpace(Min), Is.EqualTo(MinAt));
            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("tiger-push-subject"),
                "\"You may\" governs one sentence; the next one is not conditional on it.");
        }

        [Test]
        public void WithTheTargetRemovedTigersMoveIsNotOfferedAtAllAndThePushStillFollows()
        {
            var rider = new Rider(
                MinCardContent.TigerStance,
                targetRemoved: true,
                enemyMinionAt: new HexCoordinate(0, 1));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("tiger-push-subject"),
                "\"a space adjacent to the target\" has no referent once p.14 step 6 removed it, " +
                "so p.10 skips that step - and never offers the ring around the space it vacated.");
            Assert.That(rider.Outcome("tiger-move-gate"),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The question was asked and answered no, which is an ordinary answer.");
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(MinAt));
        }

        [Test]
        public void AnObstacleBesideHerNarrowsTigersMoveWithoutStoppingIt()
        {
            var rider = new Rider(
                MinCardContent.TigerStance, terrain: new[] { new HexCoordinate(1, -1) });

            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { new HexCoordinate(0, 1) }),
                "p.18's obstacle is not a space a Movement action may enter.");
        }

        [Test]
        public void HemmedInCompletelyTigersMoveIsSilentlySkippedAndThePushStillRuns()
        {
            var rider = new Rider(
                MinCardContent.TigerStance,
                terrain: BesideBoth,
                enemyMinionAt: new HexCoordinate(0, -1));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("tiger-push-subject"),
                "A `may` step with nowhere legal to go is p.10's skip, not a stop - which is what " +
                "keeps the push a separate sentence.");
        }

        [Test]
        public void ThePushAfterTigersMoveReadsAdjacencyFromHerNewSpace()
        {
            // The enemy ally stands beside (0,1) and NOT beside the origin, so it becomes a legal
            // push subject only because she moved.
            var rider = new Rider(
                MinCardContent.TigerStance, enemyAllyAt: new HexCoordinate(-1, 2));

            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.Member(new HexCoordinate(0, 1)));
            rider.Pick(EffectValue.Space(new HexCoordinate(0, 1)));

            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value),
                Is.EquivalentTo(new[] { EffectValue.Hero(Enemy), EffectValue.Hero(EnemyAlly) }),
                "p.18 measures the push from the pusher, and the sentence moved her first.");

            rider.Pick(EffectValue.Hero(EnemyAlly));
            rider.Pick(EffectValue.Integer(1));
            Assert.That(rider.HeroSpace(EnemyAlly), Is.EqualTo(new HexCoordinate(-2, 3)),
                "and directly away from where she is standing NOW.");
        }

        [Test]
        public void DragonsMoveOffersOneSpaceOrTwoAndNothingFurther()
        {
            var rider = new Rider(MinCardContent.DragonStance);

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("dragon-move-may"),
                "\"You may\" is its own step on this face, because \"1 or 2\" has already spent " +
                "the wording that makes a distance a decision.");
            rider.Answer(rider.Choice.ConfirmInvocation);

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("dragon-move-step"));
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EquivalentTo(BesideBoth.Concat(BesideBothAtTwo)),
                "One step or two, each ending beside the target - and (2,0) is still unreachable " +
                "on foot at either distance.");
            Assert.That(
                rider.Choice.Candidates.Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(MinAt),
                "\"1 or 2 spaces\" names a floor of one, so standing still is not one of the " +
                "distances - declining the step is the way not to move.");
        }

        [Test]
        public void DragonsMoveCanBeDeclinedOutrightAndThePushStillFollows()
        {
            var rider = new Rider(MinCardContent.DragonStance);
            rider.Answer(rider.Choice.DeclineInvocation);

            Assert.That(rider.HeroSpace(Min), Is.EqualTo(MinAt));
            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("dragon-push-subject"));
        }

        [Test]
        public void DragonsSecondSpaceWalksRoundTheTargetRatherThanThroughIt()
        {
            var rider = new Rider(MinCardContent.DragonStance);
            rider.Answer(rider.Choice.ConfirmInvocation);
            rider.Pick(EffectValue.Space(BesideBothAtTwo[0]));

            Assert.That(rider.HeroSpace(Min), Is.EqualTo(new HexCoordinate(2, -1)));
            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("dragon-push-subject"));
        }

        [Test]
        public void WithTheTargetRemovedDragonsMoveIsNotOfferedEither()
        {
            var rider = new Rider(
                MinCardContent.DragonStance,
                targetRemoved: true,
                enemyMinionAt: new HexCoordinate(0, 1));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("dragon-push-subject"));
        }

        // =========================================================================================
        // "You may swap with a [Smoke bomb] Smoke bomb in radius"
        // =========================================================================================

        [TestCase(4, true)]
        [TestCase(5, false)]
        public void VipersSwapIsOfferedOnlyWhileTheBombStandsInsideThePrintedRadius(
            int distance, bool offered)
        {
            var rider = new Rider(
                MinCardContent.ViperStance,
                smokeBombAt: new HexCoordinate(0, -distance));

            if (!offered)
            {
                Assert.That(rider.Step.IsComplete, Is.True,
                    "A bomb out of radius leaves the branch with nothing to do, so the question is " +
                    "never asked - which is what keeps a `may` honest.");
                Assert.That(rider.HeroSpace(Min), Is.EqualTo(MinAt));
                return;
            }

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("viper"));
            rider.Answer(rider.Choice.ConfirmInvocation);

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(new HexCoordinate(0, -4)));
            Assert.That(rider.TokenSpace(), Is.EqualTo(MinAt),
                "p.19's Swap exchanges the two positions and is neither a movement nor a placement.");
        }

        [Test]
        public void ARadiusItemWidensVipersSwapWithoutTheClauseKnowingAnItemExists()
        {
            var rider = new Rider(
                MinCardContent.ViperStance,
                smokeBombAt: new HexCoordinate(0, -5),
                radiusItem: true);

            Assert.That(rider.Choice, Is.Not.Null,
                "p.16 raises the printed icon, and p.9's \"in radius\" is that icon.");
            rider.Answer(rider.Choice.ConfirmInvocation);
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(new HexCoordinate(0, -5)));
        }

        [Test]
        public void VipersSwapMayBeDeclinedAndNothingElseIsPrinted()
        {
            var rider = new Rider(
                MinCardContent.ViperStance, smokeBombAt: new HexCoordinate(0, -4));
            rider.Answer(rider.Choice.DeclineInvocation);

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(MinAt));
            Assert.That(rider.TokenSpace(), Is.EqualTo(new HexCoordinate(0, -4)));
        }

        [Test]
        public void CobrasReplaceMeasuresItsRadiusFromHerNewSpaceAndMovesTheSamePiece()
        {
            var bombAt = new HexCoordinate(0, -4);
            var rider = new Rider(MinCardContent.CobraStance, smokeBombAt: bombAt);

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("cobra"));
            rider.Answer(rider.Choice.ConfirmInvocation);
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(bombAt));

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("cobra-replace-may"));
            rider.Answer(rider.Choice.ConfirmInvocation);

            Assert.That(rider.Choice.NodeId.Value, Is.EqualTo("cobra-replace-place"));
            var offered = rider.Choice.Candidates
                .Select(candidate => candidate.Value.Coordinate).ToArray();
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(bombAt, space) <= 4), Is.True,
                "\"into a space in radius\" is measured from where she is standing after the swap, " +
                "which is where the bomb was.");
            Assert.That(offered, Has.Member(new HexCoordinate(0, -6)),
                "Four spaces from her new position and eight from her old one, which is the whole " +
                "difference this clause makes.");

            var standing = rider.TokenId();
            rider.Pick(EffectValue.Space(new HexCoordinate(0, -6)));
            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.TokenSpace(), Is.EqualTo(new HexCoordinate(0, -6)));
            Assert.That(rider.TokenId(), Is.EqualTo(standing),
                "One physical bomb, moved - not a second one drawn from a supply of one.");
        }

        [Test]
        public void CobrasReplaceIsNeverOfferedAfterADeclinedSwap()
        {
            var rider = new Rider(
                MinCardContent.CobraStance, smokeBombAt: new HexCoordinate(0, -4));
            rider.Answer(rider.Choice.DeclineInvocation);

            Assert.That(rider.Step.IsComplete, Is.True,
                "\"if you do\" is the nesting: declining the swap skips the clause that depends on it.");
            Assert.That(rider.TokenSpace(), Is.EqualTo(new HexCoordinate(0, -4)));
        }

        [Test]
        public void CobrasReplaceMayItselfBeDeclinedLeavingTheBombWhereTheSwapPutIt()
        {
            var bombAt = new HexCoordinate(0, -4);
            var rider = new Rider(MinCardContent.CobraStance, smokeBombAt: bombAt);
            rider.Answer(rider.Choice.ConfirmInvocation);
            rider.Answer(rider.Choice.DeclineInvocation);

            Assert.That(rider.Step.IsComplete, Is.True);
            Assert.That(rider.HeroSpace(Min), Is.EqualTo(bombAt));
            Assert.That(rider.TokenSpace(), Is.EqualTo(MinAt));
        }

        [Test]
        public void NeitherSwapFaceIsOfferedWithNoBombOnTheBoardAtAll()
        {
            foreach (var card in new[] { MinCardContent.ViperStance, MinCardContent.CobraStance })
            {
                var rider = new Rider(card);
                Assert.That(rider.Step.IsComplete, Is.True,
                    $"{card.Value}: with nothing to swap with there is no question to ask.");
            }
        }

        // =========================================================================================
        // Snapshot, restore, replay and the retained prefix
        // =========================================================================================

        [Test]
        public void EveryStancePauseSurvivesSnapshotRestoreAndContinuesIdentically()
        {
            foreach (var card in new[]
            {
                MinCardContent.CraneStance, MinCardContent.TigerStance, MinCardContent.ViperStance,
                MinCardContent.DragonStance, MinCardContent.CobraStance
            })
            {
                var rider = new Rider(card, smokeBombAt: new HexCoordinate(0, -4));
                Assert.That(rider.Choice, Is.Not.Null, $"{card.Value} pauses on its first clause.");

                var round = rider.IntoRound();
                var payload = AuthoritativeSnapshotSerializer.Serialize(round, rider.Board);
                var replay = AuthoritativeSnapshotSerializer.Deserialize(
                    rider.Catalogs, payload, rider.Definitions,
                    new[] { rider.Board.MapDefinition }, rider.MinionSetup);

                Assert.That(
                    AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board),
                    Is.EqualTo(payload),
                    $"{card.Value}: a restore that re-serializes differently has changed the match " +
                    "it claims to be.");
                Assert.That(
                    replay.Round.CurrentTurnAuthority.PendingEffectChoice.NodeId,
                    Is.EqualTo(round.CurrentTurnAuthority.PendingEffectChoice.NodeId),
                    $"{card.Value}: and it is standing on the same question.");
            }
        }

        [Test]
        public void ThePushAnsweredOnBothBranchesLeavesTwoIdenticalMatches()
        {
            var live = new Rider(MinCardContent.CraneStance);
            var liveRound = live.IntoRound();
            var payload = AuthoritativeSnapshotSerializer.Serialize(liveRound, live.Board);
            var replay = AuthoritativeSnapshotSerializer.Deserialize(
                live.Catalogs, payload, live.Definitions,
                new[] { live.Board.MapDefinition }, live.MinionSetup);

            foreach (var branch in new[] { liveRound, replay.Round })
            {
                PickRound(branch, EffectValue.Hero(Enemy));
                PickRound(branch, EffectValue.Integer(2));
            }

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(liveRound, live.Board)),
                "The same prefix of answers replayed onto a restored match reaches the same match, " +
                "which is what a retained-prefix Undo replays.");
            Assert.That(live.HeroSpace(Enemy), Is.EqualTo(new HexCoordinate(3, 0)));
        }

        [Test]
        public void ARetainedPrefixReplayOfTigersMoveReachesTheSameBoardAsTheLiveWalk()
        {
            // 8.35i's shape: the prefix is re-answered into a match restored from a snapshot taken
            // BEFORE it, and the two are compared byte for byte rather than by eye.
            var live = new Rider(MinCardContent.TigerStance);
            var liveRound = live.IntoRound();
            var payload = AuthoritativeSnapshotSerializer.Serialize(liveRound, live.Board);
            var rewound = AuthoritativeSnapshotSerializer.Deserialize(
                live.Catalogs, payload, live.Definitions,
                new[] { live.Board.MapDefinition }, live.MinionSetup);

            foreach (var branch in new[] { liveRound, rewound.Round })
            {
                PickRound(branch, EffectValue.Space(BesideBoth[0]));
                PickRound(branch, EffectValue.Hero(Enemy));
                PickRound(branch, EffectValue.Integer(3));
            }

            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(rewound.Round, rewound.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(liveRound, live.Board)));
            Assert.That(live.HeroSpace(Min), Is.EqualTo(BesideBoth[0]));
            Assert.That(live.HeroSpace(Enemy), Is.EqualTo(new HexCoordinate(1, 3)),
                "Three spaces from (1,0) directly away from (1,-1), which is where the move left " +
                "her - not away from the origin she started the sentence on.");
        }

        [Test]
        public void MinionLeavesTheBattleZone_AndTheActionBoundaryCorrectsIt()
        {
            var rider = new Rider(
                MinCardContent.CraneStance, enemyMinionAt: new HexCoordinate(0, 1),
                withBattleZone: true);
            var round = rider.IntoRound();

            PickRound(round, EffectValue.Minion(EnemyMinion));
            PickRound(round, EffectValue.Integer(3));

            var space = SpaceAt(rider.Board, rider.MinionSpace(EnemyMinion));
            Assert.That(BattleZoneQuery.IsInCurrentBattleZone(rider.Board, space), Is.True,
                "p.18's own correction closes the action: a minion the push carried outside is " +
                "walked back in at the boundary, and this is the seam rather than the clause.");
        }

        // =========================================================================================
        // A whole performance
        // =========================================================================================

        [Test]
        public void OneStancePerformanceCountsExactlyOneAttackAtAHeroAndStillRunsItsRider()
        {
            var performance = new Performance(MinCardContent.CraneStance);
            performance.PlayAgainstTheEnemyHero();

            Assert.That(performance.Scoreboard.Entries
                    .Single(entry => entry.HeroId.Equals(Min)).AttacksAtHeroes,
                Is.EqualTo(1),
                "p.14's order is staged once and counted once, whichever way it ends.");
            Assert.That(performance.Scoreboard.Entries
                    .Single(entry => entry.HeroId.Equals(Enemy)).AttacksAtHeroes,
                Is.EqualTo(0),
                "Answering an attack is not making one.");

            Assert.That(performance.PendingEffectChoice, Is.Not.Null,
                "and the rider ran, at step 7, inside a real performance.");
            Assert.That(performance.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("crane-subject"));
        }

        // =========================================================================================
        // The gate, the labels and the claim
        // =========================================================================================

        [Test]
        public void NothingOfTheStancesIsRegisteredOrDealtInThisBuild()
        {
            Assert.That(
                MinComponentContent.SmokeBombRegisteredHandlers(), Is.Empty,
                "This block adds no handler at all, so the one list her content has stays empty.");
            Assert.That(
                RegisteredHeroRoster.ForThisBuild.Select(hero => hero.ContentId.Value),
                Has.No.Member("min"),
                "The other side of milestone 7.9's gate: no configured match deals her, so nothing " +
                "reachable can play one of these faces. 8.61j is the block that flips it.");
            Assert.That(
                DecisionLabelCatalog.Registered.Keys.Select(card => card.Value)
                    .Intersect(MinCardContent.StanceDefinitions.Select(card => card.Id.Value)),
                Is.Empty,
                "and her labels are declared beside her content rather than aggregated, because " +
                "what a table is FOR is a window over a card a match deals.");

            Assert.That(
                MinCardContent.DealtDefinitions.Select(card => card.Id)
                    .Intersect(MinCardContent.StanceDefinitions.Select(card => card.Id)),
                Is.Empty,
                "The dealt set milestone 8.61a's follow-up declares is EMPTY until 8.61i fills it " +
                "and 8.61j makes the word true; none of the Stances is in it.");
        }

        [Test]
        public void EveryStanceControlNamesItsConsequenceAndTigersMoveRaisesNone()
        {
            var required = MinCardContent.StanceDefinitions
                .SelectMany(definition => DecisionLabelRequirementQuery.For(definition, Catalogs))
                .ToArray();
            var declared = MinEffectContent.StanceDecisionLabels().ToArray();

            Assert.That(required, Is.Not.Empty);
            Assert.That(
                declared.Select(row => row.CardId.Value + "/" + row.NodeId.Value),
                Is.EquivalentTo(
                    required.Select(row => row.CardId.Value + "/" + row.NodeId.Value)),
                "One label per control these five raise - no more, and none missing.");
            Assert.That(
                required.Select(row => row.NodeId.Value), Has.No.Member("tiger-move-step"),
                "Tiger's move is a capability node carrying `may`, which opens its own destination " +
                "prompt rather than p.10's confirm-or-decline.");
            Assert.That(
                declared.All(row => row.Label.Chosen.Length <= 60 && row.Label.Declined.Length <= 60),
                Is.True);
        }

        [Test]
        public void TheNewMovePolicyAdmitsTheRingAroundABoundSpaceAndNothingWithoutOne()
        {
            var reachable = new[]
            {
                new HexCoordinate(0, 0), new HexCoordinate(1, -1), new HexCoordinate(0, 1),
                new HexCoordinate(2, 0), new HexCoordinate(3, 0)
            };

            Assert.That(
                DirectMoveDestinations.Admit(
                    DirectMoveDestinationPolicy.AdjacentToTheNamedSpace, MinAt, MinAt, reachable,
                    besideSpace: TargetAt),
                Is.EquivalentTo(new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, -1), new HexCoordinate(0, 1),
                    new HexCoordinate(2, 0)
                }),
                "p.19's adjacency is a distance of exactly one, so (3,0) is two away and out - and " +
                "the filter narrows a reachable set rather than producing one.");

            Assert.That(
                DirectMoveDestinations.Admit(
                    DirectMoveDestinationPolicy.AdjacentToTheNamedSpace, MinAt, MinAt, reachable),
                Is.Empty,
                "An unbound name admits nothing rather than everything, which is what every other " +
                "bound narrowing in this file does with a referent that has not happened.");

            Assert.That(
                DirectMoveDestinations.NeedsActorPosition(
                    DirectMoveDestinationPolicy.AdjacentToTheNamedSpace),
                Is.False,
                "It measures from the bound space and never from the acting hero.");
        }

        [Test]
        public void ThePolicyIsCarriedOnTheMoveByTheSameNameThePlaceUsesForTheSameClause()
        {
            Assert.That(
                SyntheticDisplacementCapabilities.BesideSpaceParameterName,
                Is.EqualTo(SyntheticPlacementCapabilities.BesideSpaceParameterName),
                "One printed clause, one parameter name: a card author who declared the bound for " +
                "a Place must not be able to get it wrong for a Move.");

            var handler = Capabilities.Load(SyntheticDisplacementCapabilities.DirectMoveV3);
            var parameter = handler.Schema.Parameters.Single(candidate =>
                candidate.Name == SyntheticDisplacementCapabilities.BesideSpaceParameterName);
            Assert.That(parameter.ValueKind, Is.EqualTo(EffectValueKind.Coordinate));
            Assert.That(parameter.IsRequired, Is.False,
                "Optional and unbound everywhere else, so every Move composed before this milestone " +
                "keeps its exact candidate set - and no revision 4 is minted.");
            Assert.That(SyntheticDisplacementCapabilities.DirectMoveV3.Revision, Is.EqualTo(3));
        }

        // =========================================================================================
        // Fixture support
        // =========================================================================================

        private static ActionDefinition Action(CardDefinition definition, ActionDefinitionId id) =>
            definition.ActionDefinitions.Single(action => action.Id.Equals(id));

        private static int Printed(ActionDefinition action, PrintedValueChannel channel) =>
            action.BaseValues
                .Where(printed => printed.Channel == channel)
                .Select(printed => printed.Amount)
                .DefaultIfEmpty(0)
                .Single();

        private static SpaceId SpaceAt(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            board.MapDefinition.Spaces
                .Single(space => space.Coordinate.Equals(coordinate)).Id;

        private static void PickRound(RoundResolutionAuthority round, EffectValue value)
        {
            var choice = round.CurrentTurnAuthority.PendingEffectChoice;
            var select = choice.SelectInvocations.Single(
                command => command.Candidate.Value.Equals(value));
            Assert.That(round.TryAnswerEffectChoice(select).Accepted, Is.True);
            var next = round.CurrentTurnAuthority.PendingEffectChoice;
            if (next != null && next.ChoiceId.Equals(choice.ChoiceId))
            {
                Assert.That(round.TryAnswerEffectChoice(next.ConfirmInvocation).Accepted, Is.True);
            }
        }

        private static MatchParticipants Participants => StagedBoardParticipants.CreateWithBlueAlly();

        /// <summary>
        /// The build's own catalog plus milestone 8.61a's six deferred Smoke bomb operations, which is
        /// the only thing about Min this build cannot load on its own. This block adds nothing to it.
        /// </summary>
        private static EffectCapabilityCatalog Capabilities
        {
            get
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                return new EffectCapabilityCatalog(defaults.Capabilities.Registered
                    .Select(defaults.Capabilities.Load)
                    .Concat(MinComponentContent.SmokeBombDeferredHandlers()));
            }
        }

        private static ComponentDefinitionCatalog Components =>
            new ComponentDefinitionCatalog(
                MinComponentContent.SmokeBombTokens(), Array.Empty<MarkerDefinition>());

        private static MatchContentCatalogs Catalogs =>
            new MatchContentCatalogs(Capabilities, Components);

        private static HeroItemState RadiusItem() => HeroItemState.Restore(
            Participants,
            new HeroItemRestoreState(new[]
            {
                new HeroItem(
                    Min, new CardInstanceId("min-stance-radius-item"),
                    HeroDashboard.RulebookLayout.Slots[3].Position, PrintedValueChannel.Radius, 1)
            }));

        /// <summary>
        /// A live modification set carrying one Object Push immunity, built the way a match builds one
        /// - from an Active effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet PushImmunityFor(HeroId heroId)
        {
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("min-stance-push-immunity"),
                SyntheticModificationCapabilities.PushImmunityDefinition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("min-stance-immunity-source"),
                new ActionDefinitionId("min-stance-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));
            return RuleModificationQuery.Build(
                timing, null, Participants, new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.None, null, null);
        }

        /// <summary>
        /// One Stance's "After the attack:" body, driven on a staged board inside a staged Attack -
        /// which is the only way to reach p.14's step 7 without dealing her.
        /// </summary>
        private sealed class Rider
        {
            private readonly EffectResolutionContext context;
            private readonly CardDefinition card;
            private readonly List<EffectResolutionEvent> events = new List<EffectResolutionEvent>();

            internal Rider(
                CardDefinitionId stance,
                HexCoordinate? enemyAt = null,
                bool targetRemoved = false,
                HexCoordinate? enemyMinionAt = null,
                HexCoordinate? friendlyMinionAt = null,
                HexCoordinate? enemyAllyAt = null,
                HexCoordinate? smokeBombAt = null,
                IEnumerable<HexCoordinate> terrain = null,
                RuleModificationSet modifications = null,
                bool radiusItem = false,
                bool withBattleZone = false)
            {
                card = MinCardContent.StanceDefinition(stance);
                var effect = MinEffectContent.StanceTextFor(stance, MinComponentContent.PrepRevision);
                var capabilities = Capabilities;
                Assert.That(EffectDefinitionValidator.Validate(effect, capabilities), Is.Empty);

                var target = enemyAt ?? TargetAt;
                var blocked = (terrain ?? Array.Empty<HexCoordinate>()).ToArray();
                Catalogs = new MatchContentCatalogs(capabilities, Components);
                Definitions = new[] { card };
                Cards = new[]
                {
                    CardInstance.CreateInHand(
                        new CardInstanceId("min-stance-card"), card.Reference, Min),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-stance-card"), card.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-ally-stance-card"), card.Reference, EnemyAlly)
                };

                var heroes = new List<HeroOccupancy> { new HeroOccupancy(Min, MinAt) };
                if (!targetRemoved)
                {
                    heroes.Add(new HeroOccupancy(Enemy, target));
                }

                if (enemyAllyAt.HasValue)
                {
                    heroes.Add(new HeroOccupancy(EnemyAlly, enemyAllyAt.Value));
                }

                MinionSetup = Minions(enemyMinionAt, friendlyMinionAt);
                Board = new AdjacentMoveAuthority(
                    Map(blocked, withBattleZone), heroes, Array.Empty<ObstacleOccupancy>(),
                    MinionSetup);

                if (smokeBombAt.HasValue)
                {
                    Board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId("min-stance-bomb"),
                        MinComponentContent.SmokeBombDefinition,
                        Min, Min, new CardInstanceId("min-stance-card"), smokeBombAt.Value));
                }

                context = new EffectResolutionContext(
                    effect, capabilities, Participants, null, 1, 1,
                    modifications ?? RuleModificationSet.Empty,
                    EffectCardState.Of(Definitions, Cards),
                    ActionKind.Attack,
                    components: new MatchComponents(
                        Components, Board, new MarkerAttachmentRegister(), Board, Board, Board, Board),
                    heroItems: radiusItem ? RadiusItem() : null,
                    enclosingAttack: new EnclosingAttackFacts(
                        Min, Enemy, default, AttackRangeKind.NonRanged, target, MinAt, default,
                        targetRemoved));

                Step = EffectResolutionSession.Start(
                    new EffectResolutionId("min-stance-resolution"),
                    Cards[0].Id, card.Reference, MinCardContent.PrimaryAttack, Min, Min,
                    ActionInvocationContext.PrintedPrimary, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
                events.AddRange(Step.Events);
            }

            internal AdjacentMoveAuthority Board { get; }
            internal MatchContentCatalogs Catalogs { get; }
            internal CardDefinition[] Definitions { get; }
            internal CardInstance[] Cards { get; }
            internal QuickGameMinionSetupState[] MinionSetup { get; }
            internal EffectResolutionStepResult Step { get; private set; }
            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

            internal HexCoordinate HeroSpace(HeroId hero) =>
                Board.Occupancies.Single(value => value.HeroId.Equals(hero)).Coordinate;

            internal HexCoordinate MinionSpace(MinionId minion) =>
                Board.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;

            internal HexCoordinate TokenSpace() => Board.PlacedTokens.Single().Coordinate;

            internal TokenInstanceId TokenId() => Board.PlacedTokens.Single().Id;

            /// <summary>
            /// What a named node reported, read off the events every step emits rather than off the
            /// frame - a resolution that finished has no frame left to ask.
            /// </summary>
            internal EffectExecutionResultKind Outcome(string nodeId) =>
                events.Last(value => value.NodeId.Value == nodeId).ResultKind;

            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(
                command => command.Candidate.Value.Equals(value)));

            internal void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
                events.AddRange(Step.Events);
            }

            /// <summary>
            /// The paused walk, restored into a real card-action authority under a real round - which
            /// is what makes a snapshot of it a snapshot of a match rather than of a fixture.
            /// </summary>
            internal RoundResolutionAuthority IntoRound()
            {
                var ledger = new HeroCoinLedger(Participants);
                var actions = CardActionResolutionAuthority.Restore(
                    Catalogs, Board,
                    CardActionResolutionRestoreState.WithPendingSkill(
                        Step.Frame,
                        HeroCoinLedgerRestoreStateHolder.Present(ledger.CaptureRestoreState())),
                    null, null, null, Definitions, Cards, Participants,
                    new MarkerAttachmentRegister(), RuleModificationSet.Empty);
                var round = new RoundResolutionAuthority(
                    Definitions, Cards, Participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForCardActionsOnly(actions));
                foreach (var hero in new[] { Min, Enemy, EnemyAlly })
                {
                    var owned = Cards.Single(instance => instance.OwnerId.Equals(hero));
                    Assert.That(round.TryCommitCard(hero, owned.Id).Accepted, Is.True);
                }

                return round;
            }

            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
                if (Choice != null && Choice.ChoiceId.Equals(id))
                {
                    Answer(Choice.ConfirmInvocation);
                }
            }
        }

        private static QuickGameMinionSetupState[] Minions(
            HexCoordinate? enemyAt, HexCoordinate? friendlyAt)
        {
            var minions = new List<QuickGameMinionSetupState>();
            if (enemyAt.HasValue)
            {
                minions.Add(Minion(
                    EnemyMinion, StagedBoardParticipants.BlueTeamIdValue, TeamSide.Blue,
                    enemyAt.Value));
            }

            if (friendlyAt.HasValue)
            {
                minions.Add(Minion(
                    FriendlyMinion, StagedBoardParticipants.RedTeamIdValue, TeamSide.Red,
                    friendlyAt.Value));
            }

            return minions.ToArray();
        }

        private static QuickGameMinionSetupState Minion(
            MinionId id, string teamId, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                id, new TeamId(teamId), side, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(MapIdentity, new SpawnPointId("min-stance-spawn")),
                new ConfiguredSpaceReference(MapIdentity, SpaceIdOf(at)),
                at);

        private static SpaceId SpaceIdOf(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        /// <summary>
        /// A radius-6 disc around Min, with the named spaces as terrain. The battle-zone variant marks
        /// the inner radius-2 disc as the starting Battle Zone, which is what makes "outside the zone"
        /// expressible at all.
        /// </summary>
        private static BoardMapDefinition Map(
            IReadOnlyList<HexCoordinate> terrain, bool withBattleZone)
        {
            var playable = new List<HexCoordinate>();
            for (var q = -6; q <= 6; q++)
            {
                for (var r = -6; r <= 6; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(MinAt, coordinate) <= 6 &&
                        !terrain.Contains(coordinate))
                    {
                        playable.Add(coordinate);
                    }
                }
            }

            return withBattleZone
                ? BoardMapDefinition.CreateSyntheticWithStartingBattleZone(
                    MapIdentity, 1,
                    playable.Where(space => HexDistanceQuery.Calculate(MinAt, space) <= 2),
                    playable.Where(space => HexDistanceQuery.Calculate(MinAt, space) > 2),
                    terrain)
                : BoardMapDefinition.CreateSynthetic(MapIdentity, 1, playable, terrain);
        }

        /// <summary>
        /// A whole p.14 order performed with a Stance: the target locked, the defence answered, the
        /// outcome resolved and the rider applied at step 7. It exists for the two facts no body-level
        /// probe can see - what the scoreboard counted, and that the card's own timing is wired.
        /// </summary>
        private sealed class Performance
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] cards;
            private readonly CardInstance source;
            private readonly MatchParticipants participants = Participants;
            private readonly RoundResolutionAuthority round;

            internal Performance(CardDefinitionId stance)
            {
                var card = MinCardContent.StanceDefinition(stance);
                definitions = new[] { card, Wall };
                cards = new[]
                {
                    CardInstance.CreateInHand(
                        new CardInstanceId("min-performance-card"), card.Reference, Min),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-committed"), Wall.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-defence"), Wall.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-ally-committed"), Wall.Reference, EnemyAlly)
                };
                source = cards[0];
                Board = new AdjacentMoveAuthority(
                    Map(Array.Empty<HexCoordinate>(), false),
                    new[]
                    {
                        new HeroOccupancy(Min, MinAt),
                        new HeroOccupancy(Enemy, TargetAt),
                        new HeroOccupancy(EnemyAlly, new HexCoordinate(-5, 0))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForMatch(
                        new CardActionResolutionAuthority(Catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null,
                        startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
                Scoreboard = new MatchScoreboard(participants);

                Commit(Min, source.Id);
                Commit(Enemy, new CardInstanceId("enemy-committed"));
                Commit(EnemyAlly, new CardInstanceId("enemy-ally-committed"));
            }

            internal AdjacentMoveAuthority Board { get; }
            internal MatchScoreboard Scoreboard { get; }
            internal PendingEffectChoice PendingEffectChoice =>
                round.CurrentTurnAuthority.PendingEffectChoice;

            internal void PlayAgainstTheEnemyHero()
            {
                Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(Min),
                    "The two other seats commit Initiative 1, so the Stance's 9 or 10 acts first " +
                    "and the p.14 order under test is hers.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Min, source.Id)
                    .Single(candidate =>
                        candidate.ActionDefinitionId.Equals(MinCardContent.PrimaryAttack));
                Observe(round.TryStartAttack(
                    new ActionResolutionId("min-performance-resolution"), offer));

                var targetChoice = round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Observe(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    targetChoice.ResolutionId, targetChoice.ChoiceId, Enemy)));

                var defence = round.CurrentTurnAuthority.PendingDefenseChoice;
                Observe(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defence.ResolutionId, defence.ChoiceId,
                    defence.LegalDefenseOffers
                        .Single(value => value.SourceCardInstanceId.Value == "enemy-defence")
                        .SourceCardInstanceId)));

                Assert.That(
                    Board.Occupancies.Single(value => value.HeroId.Equals(Enemy)).Coordinate,
                    Is.EqualTo(TargetAt),
                    "The Block held, which is the board step 7 needs.");
            }

            private void Commit(HeroId hero, CardInstanceId card) =>
                Observe(round.TryCommitCard(hero, card));

            private void Observe(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                foreach (var ruleEvent in result.Events)
                {
                    Scoreboard.Observe(ruleEvent);
                }
            }

            /// <summary>
            /// A synthetic card the other two seats commit and the defender stands behind: Initiative
            /// 1, so hers is always the highest committed and the turn is hers; Defense 9 against a
            /// printed Attack of 3, so p.14's comparison leaves the target on the board and the rider
            /// has somebody to push.
            /// </summary>
            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("min-stance-wall"), "min-8-61c-fixture",
                "TEST - Min stance wall", new InitiativeValue(1),
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
        }
    }
}

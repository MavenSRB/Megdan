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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71e. Hanu the Trickster's two co-moves - This Way! and That Way! - and the one
    /// sentence they print: "A friendly hero in radius chooses a distance of 1 or 2 [1, 2 or 3]; move
    /// both of you that number of spaces in the same direction of your choice."
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Hanu's is registered - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.71h deals him - so no composed match can put one of
    /// these cards in a hand and offer its Skill. <see cref="RowennaSwapAndGauntletTests"/> records the
    /// same boundary for the same reason. What is proved here is everything the text itself decides:
    /// who is offered, whose decision each question is, which distances stand and which are greyed,
    /// where the two pieces end up, and that neither moves when the pair cannot. What is deliberately
    /// NOT proved here is the seat projection, the lobby bots and the rewind, because all three need a
    /// dealt hero and a running host; 8.71h and the permanent player-path gate at 8.71i own them.</para>
    ///
    /// <para><b>The catalog these cases load is this build's, with nothing added.</b> The two faces
    /// compose on <c>actor-space@1</c>, <c>synthetic-radius-hero-target@1</c> and
    /// <c>coupled-direct-move@1</c>, all three of which this build already registers, which is the
    /// headline result of the block and is asserted below rather than assumed.</para>
    ///
    /// <para><b>The board is four seats wide and says why.</b> Two Blue - Hanu and one ally, which is
    /// the smallest board on which "a friendly hero" can be anybody at all - and two Red, so a case can
    /// stand one enemy inside the printed Radius and one outside it without moving anybody between
    /// cases.</para>
    /// </summary>
    public sealed class HanuCoMoveTests
    {
        private static readonly HeroId Hanu = new HeroId("hanu-trickster");
        private static readonly HeroId Ally = new HeroId("hanu-blue-ally");
        private static readonly HeroId Enemy = new HeroId("hanu-red-enemy");
        private static readonly HeroId FarEnemy = new HeroId("hanu-red-far-enemy");

        private static readonly TeamId BlueTeam = new TeamId("hanu-blue-team");
        private static readonly TeamId RedTeam = new TeamId("hanu-red-team");

        private static readonly MapId Map = new MapId("hanu-co-move-map");

        private static readonly HexCoordinate HanuAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent, due East: the commonest board this card is played on.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(1, 0);

        /// <summary>Three spaces out: the far edge of the printed Radius 3 both faces carry.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(0, 3);

        /// <summary>Five spaces out: outside the Radius either face prints, item or no item.</summary>
        private static readonly HexCoordinate FarEnemyAt = new HexCoordinate(0, 5);

        private static readonly HexDirection[] EveryDirection =
            Enum.GetValues(typeof(HexDirection)).Cast<HexDirection>().ToArray();

        // -------------------------------------------------------------------------------------------
        // The printed faces
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two faces as his shard's inventory prints them. Every number here is a reading of a card
        /// front, so a composition that drifted from one is a defect this case names rather than a
        /// preference.
        /// </summary>
        [Test]
        public void TheTwoFaces_CarryExactlyTheValuesIconsAndItemsTheShardPrints()
        {
            var thisWay = Face(HanuCardContent.ThisWay);
            var thatWay = Face(HanuCardContent.ThatWay);

            // MERGE NOTE (8.71 wave): the integrated aggregate holds every landed block's faces, so
            // this block's two are asserted by containment and its own list by equality.
            Assert.That(HanuCardContent.CoMoveDefinitions().Select(card => card.Id.Value).ToArray(),
                Is.EqualTo(new[] { "hanu-this-way", "hanu-that-way" }),
                "This block composes two of his nineteen faces and no others.");
            Assert.That(HanuCardContent.AllDefinitions.Select(card => card.Id.Value).ToArray(),
                Is.SupersetOf(new[] { "hanu-this-way", "hanu-that-way" }));

            Assert.That(thisWay.DisplayName, Is.EqualTo("This Way!"));
            Assert.That(thatWay.DisplayName, Is.EqualTo("That Way!"));
            Assert.That(thisWay.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));
            Assert.That(thatWay.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));

            Assert.That(thisWay.Initiative.Value, Is.EqualTo(11));
            Assert.That(thatWay.Initiative.Value, Is.EqualTo(11));
            Assert.That(Printed(thisWay, HanuCardContent.SecondaryDefense, PrintedValueChannel.Defense),
                Is.EqualTo(4));
            Assert.That(Printed(thatWay, HanuCardContent.SecondaryDefense, PrintedValueChannel.Defense),
                Is.EqualTo(5));

            foreach (var face in new[] { thisWay, thatWay })
            {
                Assert.That(Printed(face, HanuCardContent.SecondaryMovement, PrintedValueChannel.Movement),
                    Is.EqualTo(3), face.DisplayName + " prints Movement 3.");
                Assert.That(Printed(face, HanuCardContent.PrimarySkill, PrintedValueChannel.Radius),
                    Is.EqualTo(3), face.DisplayName + " prints Radius 3.");
                Assert.That(
                    face.ActionDefinitions.Single(action => action.Id.Equals(HanuCardContent.PrimarySkill)).Kind,
                    Is.EqualTo(ActionKind.Skill),
                    "p.9's Skill band, so the card text IS the action.");
                Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Blue));
                Assert.That(face.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Initiative),
                    "Both print the Initiative item on the branch they are not chosen for.");
                Assert.That(
                    face.ActionDefinitions.Any(action => action.BaseValues
                        .Any(value => value.Channel == PrintedValueChannel.Range)),
                    Is.False,
                    "Neither face prints a Range icon; a missing icon is absent, never zero.");
            }

            Assert.That(thisWay.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(thatWay.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(
                thisWay.Progression.NextTierCandidateIds.Select(id => id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { "hanu-safe-travels", "hanu-that-way" }),
                "MILESTONE 8.71g COMPLETED THIS ROW. 8.71e could name only the half of the Blue " +
                "ladder it owned, because Safe Travels is 8.71d's face and was being written on " +
                "another branch; p.15 forks Blue twice and This Way! climbs to EITHER Tier III.");
            Assert.That(thatWay.Progression.NextTierCandidateIds, Is.Empty);

            Assert.That(thisWay.SourceContent.PrintedText, Is.EqualTo(HanuCardContent.ThisWayText));
            Assert.That(thatWay.SourceContent.PrintedText, Is.EqualTo(HanuCardContent.ThatWayText));
            Assert.That(HanuCardContent.ThisWayText,
                Does.Contain("( Both must be moved the full distance, or neither one moves )"),
                "This Way! prints the reminder; That Way! prints the same rule without it.");
            Assert.That(HanuCardContent.ThatWayText, Does.Not.Contain("Both must be moved"));
        }

        /// <summary>
        /// Each printed distance is one bullet at exactly that distance, with p.11 in full: the two
        /// bounds are equal, and nothing ignores an obstacle. Asserted off the definition because it is
        /// the claim every behavioural case below rests on.
        /// </summary>
        [Test]
        public void EachPrintedDistance_IsOneBulletAtExactlyThatDistanceUnderP11()
        {
            foreach (var (cardId, distances) in new[]
            {
                (HanuCardContent.ThisWay, new[] { 1, 2 }),
                (HanuCardContent.ThatWay, new[] { 1, 2, 3 })
            })
            {
                var definition = SkillBody(cardId);
                Assert.That(definition.TryGetNode(
                    new EffectNodeId(HanuEffectContent.CoMoveDistanceNode), out var modal), Is.True);
                Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose));
                Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.BoundHero),
                    "p.10: the card text instructs the other hero to make this choice.");
                Assert.That(modal.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
                Assert.That(modal.MinimumSelections, Is.EqualTo(1));
                Assert.That(modal.MaximumSelections, Is.EqualTo(1));
                Assert.That(modal.ChildNodeIds.Count, Is.EqualTo(distances.Length));

                foreach (var (bullet, distance) in modal.ChildNodeIds.Zip(distances, (a, b) => (a, b)))
                {
                    Assert.That(definition.TryGetNode(bullet, out var node), Is.True);
                    Assert.That(node.Capability, Is.EqualTo(SyntheticCoupledMoveCapabilities.MoveV1));
                    Assert.That(Literal(node, "leastDistance").IntegerValue, Is.EqualTo(distance));
                    Assert.That(Literal(node, "distance").IntegerValue, Is.EqualTo(distance),
                        "Equal bounds are what make one bullet exactly one printed distance.");
                    Assert.That(Literal(node, "ignoresObstacles").BooleanValue, Is.False,
                        "p.11 in full for both units: neither face prints anything that ignores one.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // "A friendly hero in radius" - p.9's disk, his own side, never himself
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheFriendOffer_IsExactlyTheFriendlyHeroesInsideThePrintedRadius()
        {
            var world = World.Open(HanuCardContent.ThisWay);

            var choice = world.RunToPause();

            Assert.That(choice.NodeId.Value, Is.EqualTo(HanuEffectContent.CoMoveFriendNode));
            Assert.That(choice.ChooserId, Is.EqualTo(Hanu), "Hanu targets; p.19 makes it a target.");
            Assert.That(Heroes(choice), Is.EqualTo(new[] { Ally.Value }),
                "Both enemies are the wrong side, and p.17's \"friendly\" never includes Hanu.");
        }

        /// <summary>
        /// Hanu is kept out by the selection's own <c>includeSelf</c> declaration and not by luck: his
        /// side does NOT exclude him the way "an enemy hero" would, so flipping that one parameter puts
        /// him straight into the offer. p.17 is the rule, and this is the parameter that states it.
        /// </summary>
        [Test]
        public void HanuIsKeptOutByTheArmsOwnDeclaration_NotByTheSideHeIsOn()
        {
            var world = World.Open(HanuCardContent.ThisWay);

            Assert.That(Heroes(world.RunToPause()), Has.No.Member(Hanu.Value));

            var widened = World.Open(HanuCardContent.ThisWay, friendSelection: FriendProbe(true));
            Assert.That(Heroes(widened.RunToPause()), Is.EqualTo(new[] { Ally.Value, Hanu.Value }),
                "The same node with includeSelf true offers him, which is what proves the composed " +
                "face's false is doing the work p.17 asks of it.");
        }

        /// <summary>
        /// p.16's Radius item widens the disk the sentence measures, exactly as it does for every other
        /// printed-icon selection: an ally four spaces out is outside Radius 3 and inside Radius 3+1.
        /// </summary>
        [Test]
        public void APrintedRadiusItem_WidensTheDiskTheSentenceMeasures()
        {
            var farAlly = new HexCoordinate(0, 4);

            Assert.That(
                World.Open(HanuCardContent.ThisWay, allyAt: farAlly).OfferedFriends(),
                Is.Empty,
                "Four spaces is outside the printed Radius 3, so the sentence reaches nobody.");

            Assert.That(
                World.Open(HanuCardContent.ThisWay, allyAt: farAlly, radiusItems: 1).OfferedFriends(),
                Is.EqualTo(new[] { Ally.Value }),
                "p.16's item raises the icon the text reads, so the same ally is now in radius.");
        }

        /// <summary>
        /// p.19 makes the friendly hero a TARGET, so p.18's immunity applies to the selection - and it
        /// is asked in the activity the enclosing action is, which for both faces is p.12's Skill.
        /// </summary>
        [TestCase(RuleActivity.Skill, false)]
        [TestCase(RuleActivity.Attack, true)]
        public void AnImmuneTeammateIsRefused_InTheActivityTheActionActuallyIs(
            RuleActivity immuneTo, bool stillOffered)
        {
            var world = World.Open(
                HanuCardContent.ThisWay, modifications: ImmunityFor(Ally, immuneTo));

            Assert.That(world.OfferedFriends().Contains(Ally.Value), Is.EqualTo(stillOffered),
                "p.18 declares immunity per activity, so an immunity to Attacks leaves a Skill's " +
                "target alone and an immunity to Skills takes the only candidate away.");
            Assert.That(world.CanBeOffered(), Is.EqualTo(stillOffered),
                "And the only candidate being immune is what makes the whole primary empty, so the " +
                "card is greyed rather than spent on a question with no answer.");
        }

        /// <summary>
        /// No friendly hero in radius is p.10's impossible mandatory step: the action is proven empty
        /// before it is ever offered, and a frame started anyway performs nothing.
        /// </summary>
        [Test]
        public void WithNoFriendlyHeroInRadius_ThePrimaryIsRefusedBeforeItIsOffered()
        {
            var world = World.Open(HanuCardContent.ThisWay, allyAt: new HexCoordinate(0, -6));

            Assert.That(world.CanBeOffered(), Is.False,
                "Milestone 6.5af refuses an action it can prove empty rather than spending the card.");

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True, "There is no question to ask.");
            Assert.That(
                result.Events.Any(e => e.ResultKind == EffectExecutionResultKind.ImpossibleMandatory),
                Is.True);
            Assert.That(world.Where(Hanu), Is.EqualTo(HanuAt));
        }

        // -------------------------------------------------------------------------------------------
        // "chooses a distance of ..." - the friend's own seat, the printed numbers
        // -------------------------------------------------------------------------------------------

        [TestCase("hanu-this-way", 2)]
        [TestCase("hanu-that-way", 3)]
        public void TheDistanceModal_IsTheFriendsOwnDecisionOverExactlyThePrintedDistances(
            string cardId, int printedDistances)
        {
            var world = World.Open(new CardDefinitionId(cardId));

            var modal = world.Answer(world.RunToPause(), Select(world.RunToPause(), EffectValue.Hero(Ally)));

            Assert.That(modal.NodeId.Value, Is.EqualTo(HanuEffectContent.CoMoveDistanceNode));
            Assert.That(modal.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(modal.ChooserId, Is.EqualTo(Ally),
                "p.10: \"unless the card text instructs the other hero to make a choice\" - and this " +
                "text does, so the question is put to the friend's own seat.");
            Assert.That(modal.Candidates.Select(candidate => candidate.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.CoMoveOneSpaceNode,
                    HanuEffectContent.CoMoveTwoSpacesNode,
                    HanuEffectContent.CoMoveThreeSpacesNode
                }.Take(printedDistances).ToArray()));
            Assert.That(modal.MinimumSelections, Is.EqualTo(1));
            Assert.That(modal.MaximumSelections, Is.EqualTo(1));
            Assert.That(modal.DeclineInvocation, Is.Null, "The sentence is mandatory.");
            Assert.That(modal.StopInvocation, Is.Null, "Nothing here is refused on an open board.");
            Assert.That(
                modal.SelectInvocations.Select(command => command.AnsweringHeroId).Distinct().ToArray(),
                Is.EqualTo(new[] { Ally }),
                "Every pre-built invocation answers FOR the friend, which is what the seat " +
                "projection and a lobby bot's authorized-command intersection both read.");
        }

        /// <summary>
        /// A distance no direction admits is GREYED at the friend's choice rather than dropped -
        /// milestone 6.5ac's channel, which is the one milestone 8.70 rides, and which is reachable
        /// here exactly because the printed distances are composed as OPTIONS (HA-Q13e).
        ///
        /// <para>The obstacles ring Hanu's six two-step squares, so every direction is blocked at 2 and
        /// at 3 - the walk cannot transit an obstacle - while stepping one space away from the ring
        /// remains legal.</para>
        /// </summary>
        [Test]
        public void ADistanceNoDirectionAdmits_IsGreyedAtTheFriendsChoiceRatherThanDropped()
        {
            var world = World.Open(
                HanuCardContent.ThatWay,
                obstacles: EveryDirection.Select(d => StraightLineQuery.Step(HanuAt, d, 2)).ToArray());

            var modal = world.Answer(world.RunToPause(), Select(world.RunToPause(), EffectValue.Hero(Ally)));

            Assert.That(modal.Candidates.Count, Is.EqualTo(3), "All three are still OFFERED.");
            Assert.That(
                modal.UnavailableCandidates.Select(candidate => candidate.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.CoMoveTwoSpacesNode, HanuEffectContent.CoMoveThreeSpacesNode
                }),
                "Two and three are impossible in every direction, so they are greyed with no way to " +
                "select them.");
            Assert.That(
                modal.SelectInvocations.Select(command => command.Candidate.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[] { HanuEffectContent.CoMoveOneSpaceNode }));
            Assert.That(modal.StopInvocation, Is.Null,
                "One distance can still be taken, so there is a decision to make.");
        }

        /// <summary>
        /// Every distance infeasible is p.10's stop, asked of the player rather than taken for them -
        /// milestone 6.5ae. Hanu is ringed by obstacles, so no direction admits even one space.
        /// </summary>
        [Test]
        public void WhenNoDistanceIsPossibleAtAll_EveryBulletIsGreyedAndTheStopClosesIt()
        {
            var world = World.Open(
                HanuCardContent.ThatWay,
                allyAt: new HexCoordinate(2, 0),
                obstacles: EveryDirection.Select(d => StraightLineQuery.Step(HanuAt, d, 1)).ToArray());

            var modal = world.Answer(world.RunToPause(), Select(world.RunToPause(), EffectValue.Hero(Ally)));

            Assert.That(modal.UnavailableCandidates, Is.EqualTo(modal.Candidates));
            Assert.That(modal.SelectInvocations, Is.Empty);
            Assert.That(modal.StopInvocation, Is.Not.Null,
                "A mandatory modal whose every bullet is refused closes on p.10's stop.");

            var stopped = world.SubmitToCompletion(modal.StopInvocation);

            Assert.That(stopped.IsComplete, Is.True);
            Assert.That(world.Where(Hanu), Is.EqualTo(HanuAt), "Neither one moves.");
            Assert.That(world.Where(Ally), Is.EqualTo(new HexCoordinate(2, 0)));
        }

        // -------------------------------------------------------------------------------------------
        // "in the same direction of your choice" - Hanu's half
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDirectionPrompt_IsHanusAndListsExactlyTheDirectionsLegalAtTheChosenDistance()
        {
            var world = World.Open(HanuCardContent.ThatWay);
            var friend = world.RunToPause();
            var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));

            var destinations = world.Answer(modal, SelectOption(modal, HanuEffectContent.CoMoveTwoSpacesNode));

            Assert.That(destinations.NodeId.Value, Is.EqualTo(HanuEffectContent.CoMoveTwoSpacesNode));
            Assert.That(destinations.ChooserId, Is.EqualTo(Hanu),
                "\"in the same direction of your choice\" is Hanu's half of the sentence.");
            Assert.That(Spaces(destinations),
                Is.EquivalentTo(EveryDirection.Select(d => StraightLineQuery.Step(HanuAt, d, 2))),
                "On an open board every direction is legal at two spaces, and the candidate list is " +
                "one square per direction because the two bounds are equal.");
        }

        [Test]
        public void AnObstacleTakesExactlyItsOwnDirectionOutOfHanusOffer()
        {
            var blocked = StraightLineQuery.Step(HanuAt, HexDirection.NorthWest, 2);
            var world = World.Open(HanuCardContent.ThatWay, obstacles: new[] { blocked });
            var friend = world.RunToPause();
            var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));

            var destinations = world.Answer(modal, SelectOption(modal, HanuEffectContent.CoMoveTwoSpacesNode));

            Assert.That(Spaces(destinations), Has.No.Member(blocked));
            Assert.That(Spaces(destinations).Count, Is.EqualTo(EveryDirection.Length - 1),
                "p.11: no unit ends its movement on an obstacle, and this face ignores none.");
        }

        /// <summary>
        /// The whole sentence, over every direction and every printed distance: both of them travel the
        /// full distance in the one direction Hanu named, and nothing else on the board moves.
        /// </summary>
        [Test]
        public void BothTravelTheFullDistanceInTheOneDirectionHanuNames()
        {
            foreach (var direction in EveryDirection)
            foreach (var (bullet, distance) in new[]
            {
                (HanuEffectContent.CoMoveOneSpaceNode, 1),
                (HanuEffectContent.CoMoveTwoSpacesNode, 2),
                (HanuEffectContent.CoMoveThreeSpacesNode, 3)
            })
            {
                // The enemies are stood well clear for this sweep, because a hero on an endpoint is
                // itself a p.11 refusal and the case below is the one that says so.
                var world = World.Open(
                    HanuCardContent.ThatWay,
                    enemyAt: new HexCoordinate(0, 8),
                    farEnemyAt: new HexCoordinate(0, 9));
                var friend = world.RunToPause();
                var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));
                var destinations = world.Answer(modal, SelectOption(modal, bullet));

                var landing = StraightLineQuery.Step(HanuAt, direction, distance);
                var finished = world.SubmitToCompletion(Select(destinations, EffectValue.Space(landing)));

                Assert.That(finished.IsComplete, Is.True,
                    $"{direction} at {distance} completes the text.");
                Assert.That(world.Where(Hanu), Is.EqualTo(landing));
                Assert.That(world.Where(Ally),
                    Is.EqualTo(StraightLineQuery.Step(AllyAt, direction, distance)),
                    "The friendly hero travels the same distance in the same direction.");
                Assert.That(world.Where(Enemy), Is.EqualTo(new HexCoordinate(0, 8)),
                    "The sentence names two units and moves two units.");
            }
        }

        /// <summary>
        /// A hero standing where one of the pair would land takes that direction out of Hanu's offer
        /// exactly as an obstacle does: p.11 is asked of BOTH units, and the endpoints an occupant
        /// blocks are theirs rather than the actor's alone.
        /// </summary>
        [Test]
        public void AHeroStandingOnEitherEndpointTakesThatDirectionOut()
        {
            var world = World.Open(
                HanuCardContent.ThatWay,
                enemyAt: StraightLineQuery.Step(AllyAt, HexDirection.NorthEast, 3),
                farEnemyAt: new HexCoordinate(0, 9));
            var friend = world.RunToPause();
            var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));

            var destinations = world.Answer(modal, SelectOption(modal, HanuEffectContent.CoMoveThreeSpacesNode));

            Assert.That(Spaces(destinations),
                Has.No.Member(StraightLineQuery.Step(HanuAt, HexDirection.NorthEast, 3)),
                "The FRIEND's endpoint is occupied, and the pair moves together or not at all.");
            Assert.That(Spaces(destinations).Count, Is.EqualTo(EveryDirection.Length - 1));
        }

        /// <summary>
        /// "Both must be moved the full distance, or neither one moves." A board that changed under a
        /// standing offer is re-read when the answer arrives, and a pair that can no longer travel is
        /// refused whole - which is <c>coupled-direct-move@1</c>'s own contract and the reason This
        /// Way! prints the reminder at all.
        /// </summary>
        [Test]
        public void WhenThePairCanNoLongerTravel_NeitherOneMoves()
        {
            var world = World.Open(HanuCardContent.ThatWay);
            var friend = world.RunToPause();
            var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));
            var destinations = world.Answer(modal, SelectOption(modal, HanuEffectContent.CoMoveTwoSpacesNode));
            var landing = StraightLineQuery.Step(HanuAt, HexDirection.West, 2);
            var stale = Select(destinations, EffectValue.Space(landing));

            // The ally's own endpoint fills up while the question stands: Hanu's half of the route is
            // still clear, and the pair is refused anyway.
            world.Displace(FarEnemy, StraightLineQuery.Step(AllyAt, HexDirection.West, 2));

            var result = world.Submit(stale);

            Assert.That(result.Accepted, Is.True, "The command itself was a legal one to send.");
            Assert.That(
                result.Events.Any(e => e.ResultKind == EffectExecutionResultKind.ImpossibleMandatory),
                Is.True);
            Assert.That(world.Where(Hanu), Is.EqualTo(HanuAt), "Neither one moves.");
            Assert.That(world.Where(Ally), Is.EqualTo(AllyAt));
        }

        // -------------------------------------------------------------------------------------------
        // Restore and replay
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Both pauses are re-derived from trusted content rather than carried: rebuilding the catalog
        /// and the definitions at each one leaves the same question standing and the same answer legal,
        /// and running the identical decisions from scratch lands on the identical board.
        /// </summary>
        [Test]
        public void BothPausesSurviveARebuildOfTheTrustedContent_AndTheDecisionsReplayIdentically()
        {
            foreach (var rebuildAt in new[] { 0, 1, 2 })
            {
                var world = World.Open(HanuCardContent.ThatWay);

                if (rebuildAt == 0) world.RebindTrustedContent();
                var friend = world.RunToPause();
                Assert.That(Heroes(friend), Is.EqualTo(new[] { Ally.Value }));

                var modal = world.Answer(friend, Select(friend, EffectValue.Hero(Ally)));
                if (rebuildAt == 1) world.RebindTrustedContent();
                Assert.That(modal.ChooserId, Is.EqualTo(Ally));

                var destinations = world.Answer(
                    modal, SelectOption(modal, HanuEffectContent.CoMoveThreeSpacesNode));
                if (rebuildAt == 2) world.RebindTrustedContent();

                var landing = StraightLineQuery.Step(HanuAt, HexDirection.SouthWest, 3);
                world.SubmitToCompletion(Select(destinations, EffectValue.Space(landing)));

                Assert.That(world.Where(Hanu), Is.EqualTo(landing));
                Assert.That(world.Where(Ally),
                    Is.EqualTo(StraightLineQuery.Step(AllyAt, HexDirection.SouthWest, 3)),
                    "A rebuild at any of the three moments leaves the same text running.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Words, and the gate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The labels this block declares are exactly the controls its faces raise, DERIVED from the
        /// definitions by the resolver's own rule rather than listed - which is the check milestone
        /// 8.69f found ten missing labels with.
        /// </summary>
        [Test]
        public void TheDeclaredLabels_AreExactlyTheControlsTheseTwoFacesRaise()
        {
            // MERGE NOTE (8.71 wave): derived over THIS block's two faces, not the aggregate.
            var required = HanuCardContent.CoMoveDefinitions()
                .SelectMany(card => DecisionLabelRequirementQuery.For(
                    card, MatchContentCatalogs.ForThisBuild()))
                .Select(requirement => requirement.CardId.Value + "/" + requirement.NodeId.Value +
                    " (" + requirement.Kind + ")")
                .OrderBy(row => row, StringComparer.Ordinal)
                .ToArray();
            var declared = HanuEffectContent.CoMoveDecisionLabels()
                .Select(label => label.CardId.Value + "/" + label.NodeId.Value +
                    " (" + DecisionLabelKind.ChooseArm + ")")
                .OrderBy(row => row, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.EqualTo(required),
                "Five controls: one per printed distance, two on This Way! and three on That Way!.");
            foreach (var label in HanuEffectContent.CoMoveDecisionLabels())
            {
                Assert.That(label.Label.Chosen.Length, Is.LessThanOrEqualTo(60),
                    label.ToString() + " is over the authoring bound.");
                Assert.That(label.Label.HasDeclined, Is.False,
                    "A Choose arm has no decline of its own.");
                Assert.That(label.Label.Chosen, Does.Not.Contain("hanu"),
                    "A label names the consequence, never the mechanism.");
            }
        }

        /// <summary>
        /// Milestone 7.9's gate, asserted from both sides: this block declares no capability of its
        /// own, this build registers nothing of Hanu's, and nothing of his is dealable yet.
        /// </summary>
        [Test]
        public void ThisBlockDeclaresNoContractAtAll_AndTheDealingRegisteredNoneOfItsOwn()
        {
            Assert.That(HanuComponentContent.CoMoveDeferredHandlers(), Is.Empty,
                "The two faces compose entirely on shipped contracts - the block's headline result.");
            Assert.That(HanuComponentContent.CoMoveRegisteredHandlers(), Is.Empty);
            // MILESTONE 8.71g STATED HIS SET and 8.71h DEALT it, so the claim that matters here is
            // narrower than it was: the six capabilities the build now registers for him are OTHER
            // blocks' - not one of them is named by either of these two faces, which is this block's
            // headline result read from the build's own catalog.
            Assert.That(HanuCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "and 8.71h registered him without adding anything for these two.");

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(reference => reference.CapabilityId.Value)
                .ToArray();
            Assert.That(
                registered.Where(id => id.StartsWith("hanu", StringComparison.Ordinal))
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "hanu-friendly-unit-pair",
                    "hanu-hurry-up-initiative",
                    "hanu-journey-partner",
                    "hanu-safe-travels-effect",
                    "hanu-there-and-back-again-effect",
                    "hanu-unexpected-journey-effect"
                }),
                "SIX contracts of his are registered since the dealing, and NONE of them is this " +
                "block's: the co-moves mint nothing, which is what this case is for.");

            foreach (var required in new[]
            {
                SyntheticPlacementCapabilities.ActorSpaceV1,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                SyntheticCoupledMoveCapabilities.MoveV1
            })
            {
                Assert.That(registered, Has.Member(required.CapabilityId.Value),
                    "Every contract these two faces name is one this build already registers, so " +
                    "the dealing has no registration to add for them.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Face(CardDefinitionId id) =>
            HanuCardContent.AllDefinitions.Single(card => card.Id.Equals(id));

        private static EffectDefinition SkillBody(CardDefinitionId id) =>
            Face(id).ActionDefinitions.Single(action => action.Id.Equals(HanuCardContent.PrimarySkill)).EffectRoot;

        private static int Printed(
            CardDefinition card, ActionDefinitionId action, PrintedValueChannel channel) =>
            card.ActionDefinitions.Single(candidate => candidate.Id.Equals(action)).BaseValues
                .Single(value => value.Channel == channel).Amount;

        private static EffectValue Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal;

        private static string[] Heroes(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                .Select(candidate => candidate.Value.HeroId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static IReadOnlyList<HexCoordinate> Spaces(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();

        private static EffectChoiceCommand Select(PendingEffectChoice choice, EffectValue value) =>
            choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value));

        private static EffectChoiceCommand SelectOption(PendingEffectChoice choice, string nodeId) =>
            choice.SelectInvocations.Single(
                command => command.Candidate.OptionNodeId.Value == nodeId);

        /// <summary>
        /// The composed face's friend selection with <c>includeSelf</c> set as the caller asks, for the
        /// one case that proves which parameter keeps Hanu out of his own disk.
        /// </summary>
        private static EffectDefinition FriendProbe(bool includeSelf)
        {
            var node = new EffectNodeId(HanuEffectContent.CoMoveFriendNode);
            return new EffectDefinition(
                new EffectDefinitionId("hanu-friend-probe"),
                // MILESTONE 8.71g: this probe is the FIXTURE's own body and carries a revision of its
                // own, so that a grep for his set's revision finds only content. It read
                // "hanu-8-71e-prep" - this block's deleted placeholder - until the set was stated.
                "hanu-co-move-probe-rev",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(includeSelf))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// A standing whole immunity for one hero against one activity, built the way
        /// <see cref="RowennaSwapAndGauntletTests"/> builds its own and asserted to actually hold.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"hanu-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"Hanu{activity}Immunity",
                $"hanu-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"hanu-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("hanu-immunity-source"),
                new ActionDefinitionId("hanu-immunity-action"),
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
                UnlockedUltimateSet.None, RuleObject.None, null, null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Hanu)),
                Is.True,
                "The fixture must actually hold the immunity, or the case above proves nothing.");
            return set;
        }

        /// <summary>
        /// Four seats: two Blue, so "a friendly hero" can be somebody, and two Red, so one enemy can
        /// stand inside the printed Radius and one outside it.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var hanuPlayer = new PlayerId("hanu-player");
            var allyPlayer = new PlayerId("hanu-ally-player");
            var enemyPlayer = new PlayerId("hanu-enemy-player");
            var farEnemyPlayer = new PlayerId("hanu-far-enemy-player");

            return new MatchParticipants(
                new[] { new Team(BlueTeam, TeamSide.Blue), new Team(RedTeam, TeamSide.Red) },
                new[]
                {
                    new Player(hanuPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(farEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Hanu, hanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(FarEnemy, farEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// One staged position and the frame walking one of the two co-moves over it. Nothing here is a
        /// configured match: see the fixture's own remarks for why, and for who owns the part this
        /// cannot reach.
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly EffectDefinition overrideDefinition;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly MatchParticipants participants;
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private readonly HeroItemState heroItems;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                CardDefinitionId cardId,
                EffectDefinition overrideDefinition,
                IReadOnlyList<CardDefinition> definitions,
                MatchParticipants participants,
                MatchComponents components,
                RuleModificationSet modifications,
                HeroItemState heroItems)
            {
                this.cardId = cardId;
                this.overrideDefinition = overrideDefinition;
                this.definitions = definitions;
                this.participants = participants;
                this.components = components;
                this.modifications = modifications;
                this.heroItems = heroItems;
                context = BuildContext();
            }

            internal static World Open(
                CardDefinitionId cardId,
                HexCoordinate? allyAt = null,
                IReadOnlyList<HexCoordinate> obstacles = null,
                RuleModificationSet modifications = null,
                int radiusItems = 0,
                EffectDefinition friendSelection = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? farEnemyAt = null)
            {
                var participants = Participants();
                var items = new HeroItemState(participants);
                for (var index = 0; index < radiusItems; index++)
                {
                    Assert.That(
                        items.TryGrantItemFromTheBox(Hanu, PrintedValueChannel.Radius).IsPlaced,
                        Is.True, "p.16's Radius slot must actually take the item.");
                }

                return new World(
                    cardId,
                    friendSelection,
                    HanuCardContent.AllDefinitions,
                    participants,
                    Components(
                        new[]
                        {
                            new HeroOccupancy(Hanu, HanuAt),
                            new HeroOccupancy(Ally, allyAt ?? AllyAt),
                            new HeroOccupancy(Enemy, enemyAt ?? EnemyAt),
                            new HeroOccupancy(FarEnemy, farEnemyAt ?? FarEnemyAt)
                        },
                        obstacles ?? Array.Empty<HexCoordinate>()),
                    modifications ?? RuleModificationSet.Empty,
                    items);
            }

            private static MatchComponents Components(
                IReadOnlyList<HeroOccupancy> heroes, IReadOnlyList<HexCoordinate> obstaclesAt)
            {
                var zone = new ZoneId("hanu-co-move-zone");
                var spaces = new List<BoardMapSpace>();
                for (var q = -9; q <= 9; q++)
                for (var r = -9; r <= 9; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(coordinate, HanuAt) > 9)
                    {
                        continue;
                    }

                    spaces.Add(new BoardMapSpace(
                        SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, zone));
                }

                var map = new BoardMapDefinition(
                    Map, 1, "Hanu Co-Move Map",
                    spaces,
                    new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

                var board = new AdjacentMoveAuthority(
                    map,
                    heroes,
                    obstaclesAt
                        .Select((at, index) => new ObstacleOccupancy(
                            new ObstacleId($"hanu-obstacle-{index:00}"), at))
                        .ToArray(),
                    Array.Empty<QuickGameMinionSetupState>());

                return new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board);
            }

            private static SpaceId SpaceIdFor(HexCoordinate at) => new SpaceId($"q{at.Q}_r{at.R}");

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            internal void RebindTrustedContent() => context = BuildContext();

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    overrideDefinition ?? Body(),
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    null,
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, Array.Empty<CardInstance>()),
                    ActionKind.Skill,
                    components: components,
                    heroItems: heroItems);

            private EffectDefinition Body() =>
                Face(cardId).ActionDefinitions
                    .Single(action => action.Id.Equals(HanuCardContent.PrimarySkill)).EffectRoot;

            /// <summary>
            /// The friendly heroes the sentence reaches on this board, and empty where it reaches
            /// nobody - which is a completed walk rather than a question, so it is asked here rather
            /// than through <see cref="RunToPause"/>.
            /// </summary>
            internal string[] OfferedFriends()
            {
                var result = Run();
                return result.IsComplete || result.Frame.PendingChoice == null
                    ? Array.Empty<string>()
                    : Heroes(result.Frame.PendingChoice);
            }

            /// <summary>Whether milestone 6.5af would offer the primary at all, on this board.</summary>
            internal bool CanBeOffered() =>
                EffectOptionFeasibilityQuery.CanApplyAnything(StartFrame(), context);

            private PendingResolution StartFrame() =>
                EffectResolutionSession.BuildStartFrame(
                    new EffectResolutionId("hanu-co-move-resolution"),
                    new CardInstanceId("hanu-co-move-source"),
                    Face(cardId).Reference,
                    HanuCardContent.PrimarySkill,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    context);

            internal EffectResolutionStepResult Run()
            {
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-co-move-resolution"),
                    new CardInstanceId("hanu-co-move-source"),
                    Face(cardId).Reference,
                    HanuCardContent.PrimarySkill,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            /// <summary>
            /// The first question, starting the walk the first time it is asked and re-reading the
            /// standing one afterwards - so a case may name it twice in one expression.
            /// </summary>
            internal PendingEffectChoice RunToPause()
            {
                if (frame?.PendingChoice != null)
                {
                    return frame.PendingChoice;
                }

                var result = Run();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
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

            /// <summary>Moves a piece under a standing offer, for the stale-answer case.</summary>
            internal void Displace(HeroId hero, HexCoordinate to) =>
                Assert.That(
                    components.Board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(hero), to).Accepted,
                    Is.True);

            internal HexCoordinate Where(HeroId hero) =>
                components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(hero), out var at)
                    ? at
                    : throw new InvalidOperationException($"{hero.Value} stands nowhere.");
        }
    }
}

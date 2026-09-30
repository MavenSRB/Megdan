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
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21f. Razzle's moves and swaps of his bodies: Alleyoop, Group Performance and Team
    /// Spirit (swap with a friendly hero in range, then move one other body 1 / up to 2 / up to 3);
    /// Magic Trick and Aaaand it's gone! (push a unit adjacent up to 2 / 3, then the acting body retreats
    /// the realised count in the opposite direction); Tightrope, High Wire and Wire Dancers (after the
    /// Movement, move another body 1 / up to 2 / up to 3); Theatrics (swap with a minion in range).
    /// RZ-Q5, RZ-Q8, RZ-Q9 and RZ-Q10 as the user settled them. Nothing is registered: the round below
    /// composes Razzle's handlers into its own catalog.
    ///
    /// <para><b>The board.</b> A radius-5 disk. Razzle is the Blue hero with a supply of four bodies;
    /// body 1 stands at the origin. Red is the enemy, far away at (-5, 5) unless a case moves it; a
    /// Blue ally joins only where a face names a friendly hero.</para>
    /// </summary>
    public sealed class RazzleMoveFacesTests
    {
        private static readonly HeroId Razzle = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly MapId Map = new MapId("razzle-move-faces");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate Origin = H(0, 0);

        // --- The printed faces ----------------------------------------------------------------------

        [Test]
        public void TheNineFaces_CarryTheirPrintedValuesAndText()
        {
            AssertFace(RazzleCardContent.AlleyoopDefinition, CardColor.Blue, CardTier.I, 10, 2, 3,
                PrintedValueChannel.None, 3,
                "Swap places with a friendly hero in range. Move one other you 1 space.");
            AssertFace(RazzleCardContent.GroupPerformanceDefinition, CardColor.Blue, CardTier.II, 11, 3, 3,
                PrintedValueChannel.Attack, 4,
                "Swap places with a friendly hero in range. Move one other you up to 2 spaces.");
            AssertFace(RazzleCardContent.MagicTrickDefinition, CardColor.Blue, CardTier.II, 11, 3, 3,
                PrintedValueChannel.Defense, 0,
                "Push a unit adjacent to you up to 2 spaces; for every space the target moved, move this " +
                "one of you one space in the opposite direction.");
            AssertFace(RazzleCardContent.TeamSpiritDefinition, CardColor.Blue, CardTier.III, 11, 3, 3,
                PrintedValueChannel.Radius, 4,
                "Swap places with a friendly hero in range. Move one other you up to 3 spaces.");
            AssertFace(RazzleCardContent.AaaandItsGoneDefinition, CardColor.Blue, CardTier.III, 11, 3, 3,
                PrintedValueChannel.Defense, 0,
                "Push a unit adjacent to you up to 3 spaces; for every space the target moved, move this " +
                "one of you one space in the opposite direction.");
            AssertFace(RazzleCardContent.TheatricsDefinition, CardColor.Green, CardTier.II, 2, 2, 2,
                PrintedValueChannel.Attack, 2, "Swap places with a minion in range.");

            foreach (var (face, tier, initiative, defense, item, text) in new[]
            {
                (RazzleCardContent.TightropeDefinition, CardTier.I, 1 + 1, 1, PrintedValueChannel.None,
                    "After you move you may move another one of you 1 space."),
                (RazzleCardContent.HighWireDefinition, CardTier.II, 2, 2, PrintedValueChannel.Initiative,
                    "After you move you may move another one of you up to 2 spaces."),
                (RazzleCardContent.WireDancersDefinition, CardTier.III, 1, 2, PrintedValueChannel.Initiative,
                    "After you move you may move another one of you up to 3 spaces.")
            })
            {
                Assert.That(face.Initiative.Value, Is.EqualTo(initiative), face.DisplayName);
                Assert.That((face.Progression.Color, face.Progression.Tier, face.Progression.ItemIcon),
                    Is.EqualTo((CardColor.Green, tier, item)), face.DisplayName);
                Assert.That(face.SourceContent.PrintedText, Is.EqualTo(text));
                var movement = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement);
                Assert.That(movement.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary),
                    "the Movement is the primary; the face prints no secondary Movement.");
                Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(2));
                Assert.That(movement.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheMovement),
                    "\"After you move\" is p.11's \"otherwise\".");
                Assert.That(face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount, Is.EqualTo(defense));
                Assert.That(face.ActionDefinitions.Count, Is.EqualTo(2));
            }

            Assert.That(RazzleCardContent.MoveDefinitions.Select(face => face.Id), Is.Unique);
            Assert.That(RazzleCardContent.MoveDefinitions.Count, Is.EqualTo(9));
            Assert.That(RazzleCardContent.MoveDefinitions.Select(face => face.ContentRevision),
                Is.All.EqualTo(RazzleCardContent.ContentRevision));
        }

        [Test]
        public void TheUpgradeLines_FollowTheNameFamilies()
        {
            Assert.That(RazzleCardContent.AlleyoopDefinition.Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { RazzleCardContent.GroupPerformance, RazzleCardContent.MagicTrick }));
            Assert.That(RazzleCardContent.GroupPerformanceDefinition.Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { RazzleCardContent.TeamSpirit, RazzleCardContent.AaaandItsGone }));
            Assert.That(RazzleCardContent.MagicTrickDefinition.Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { RazzleCardContent.TeamSpirit, RazzleCardContent.AaaandItsGone }));
            Assert.That(RazzleCardContent.TightropeDefinition.Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { RazzleCardContent.HighWire, RazzleCardContent.Theatrics }));
            Assert.That(RazzleCardContent.HighWireDefinition.Progression.NextTierCandidateIds.Select(id => id.Value),
                Is.EqualTo(new[] { "razzle-wire-dancers", "razzle-spectacle" }));
            Assert.That(RazzleCardContent.TheatricsDefinition.Progression.NextTierCandidateIds.Select(id => id.Value),
                Is.EqualTo(new[] { "razzle-wire-dancers", "razzle-spectacle" }));
        }

        [Test]
        public void TheMoveOfAnotherBody_IsExactAtOneAndUpToAtTwoAndThree()
        {
            foreach (var (face, spaces, policy) in new[]
            {
                (RazzleCardContent.AlleyoopDefinition, 1, EffectFailurePolicy.IfAble),
                (RazzleCardContent.GroupPerformanceDefinition, 2, EffectFailurePolicy.UpTo),
                (RazzleCardContent.TeamSpiritDefinition, 3, EffectFailurePolicy.UpTo),
                (RazzleCardContent.TightropeDefinition, 1, EffectFailurePolicy.IfAble),
                (RazzleCardContent.HighWireDefinition, 2, EffectFailurePolicy.UpTo),
                (RazzleCardContent.WireDancersDefinition, 3, EffectFailurePolicy.UpTo)
            })
            {
                var text = face.ActionDefinitions.Single(action => action.EffectRoot != null).EffectRoot;
                var move = text.Nodes.Single(node =>
                    node.Id.Value == RazzleEffectContent.OtherBodyMoveNode(face.Id));
                Assert.That(move.Capability, Is.EqualTo(SyntheticDisplacementCapabilities.DirectMoveV3));
                Assert.That(move.FailurePolicy, Is.EqualTo(policy), face.DisplayName);
                Assert.That(move.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                    Is.EqualTo(spaces), face.DisplayName);
                var other = text.Nodes.Single(node => node.Id.Value == RazzleEffectContent.OtherBodyNode(face.Id));
                Assert.That(other.Capability, Is.EqualTo(RazzleEffectContent.OtherBodyV1));
                Assert.That(other.FailurePolicy,
                    Is.EqualTo(face.ActionDefinitions.Any(action =>
                            action.Kind == ActionKind.Movement && action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        ? EffectFailurePolicy.May
                        : EffectFailurePolicy.IfAble),
                    "\"you may move another\" is the Green faces' choice; the Blue move is p.10's " +
                    "step, not performed with no other body (RZ-Q8).");
            }
        }

        // --- Alleyoop, Group Performance, Team Spirit ------------------------------------------------

        [Test]
        public void Alleyoop_OffersTheFriendlyHeroInRange_NeverHisOwnBodyOrAnEnemy()
        {
            // Body 2 stands beside him, well in Range 3; the enemy hero too.
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(1, 0) }, allyAt: H(2, -1), enemyAt: H(0, 2));
            round.PickActingBody(1);
            round.StartSkill();

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SwapSubjectNode(RazzleCardContent.Alleyoop)));
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Hero(Ally) }),
                "a friendly hero only: never one of his bodies (RZ-Q5, p.17), never an enemy.");
        }

        [Test]
        public void Alleyoop_WithTheAllyOutOfRange_IsGreyed_EvenWithHisOwnBodyInRange()
        {
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(1, 0) }, allyAt: H(4, 0));
            round.PickActingBody(1);
            Assert.That(round.IsOffered(RazzleCardContent.PrimarySkill), Is.False,
                "four spaces away is out of Range 3, and his own body is not a friendly hero (RZ-Q5).");

            var teamSpirit = new MoveRound(RazzleCardContent.TeamSpiritDefinition,
                bodiesAt: new[] { H(1, 0) }, allyAt: H(4, 0));
            teamSpirit.PickActingBody(1);
            Assert.That(teamSpirit.IsOffered(RazzleCardContent.PrimarySkill), Is.True, "Range 4 reaches it.");
        }

        [Test]
        public void Alleyoop_SwapsTheActingBody_ThenMovesAnyOtherBodyOneSpace_KeepingItsIdentity()
        {
            // Body 2 far outside Range 3: "Move one other you" reaches any body (RZ-Q8).
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(-4, 4) }, allyAt: H(2, -1));
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));

            Assert.That(round.Board.TryGetHeroBodyCoordinate(Razzle, 1, out var bodyOne), Is.True);
            Assert.That(bodyOne, Is.EqualTo(H(2, -1)), "p.19's Swap moved the acting body.");
            Assert.That(round.Board.TryGetHeroCoordinate(Ally, out var ally) && ally.Equals(Origin), Is.True);

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.OtherBodyNode(RazzleCardContent.Alleyoop)));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(-4, 4) }),
                "the one other body, wherever it stands - never the acting body.");
            round.Answer(EffectValue.Space(H(-4, 4)));

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.OtherBodyMoveNode(RazzleCardContent.Alleyoop)));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(
                    H(-4, 4).GetNeighbors().Where(at => round.Board.IsEmptyPlayableSpace(at))),
                "exactly 1 space: every empty space beside it, and not its own.");
            round.Answer(EffectValue.Space(H(-3, 3)));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, H(2, -1)),
                new HeroOccupancy(Razzle, 2, H(-3, 3))
            }), "body 2 moved and is still body 2; body 1 stayed where the Swap put it.");
            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting) && acting == 1, Is.True);
        }

        [Test]
        public void Alleyoop_WithNoOtherBody_StillSwaps_AndAsksNothingMore()
        {
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition, allyAt: H(0, -2));
            Assert.That(round.IsOffered(RazzleCardContent.PrimarySkill), Is.True,
                "the offer is judged on the Swap (RZ-Q8).");
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));

            Assert.That(round.ActionEnded, Is.True, "the move step is not performed (RZ-Q8, p.10).");
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, H(0, -2)) }));
            Assert.That(round.Board.TryGetHeroCoordinate(Ally, out var ally) && ally.Equals(Origin), Is.True);
        }

        [Test]
        public void Alleyoop_WithTheOtherBodyBoxedIn_StillSwaps_AndSkipsTheMove()
        {
            // Body 2 at the board's corner (5, -5), its three neighbours filled with minions.
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(5, -5) }, allyAt: H(0, -2),
                blueMinionsAt: new[] { H(4, -5), H(4, -4), H(5, -4) });
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));

            Assert.That(round.ActionEnded, Is.True,
                "a body with no space to step to is not offered, and with none left the step is not performed.");
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { H(0, -2), H(5, -5) }));
        }

        [Test]
        public void TheActingBodyIsTheOneThatSwaps_AndTheOtherIsTheOneThatMoves()
        {
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(3, -3) }, allyAt: H(3, -1));
            round.PickActingBody(2);
            round.StartSkill();
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Hero(Ally) }),
                "Range 3 measured from body 2 (RZ-Q4).");
            round.Answer(EffectValue.Hero(Ally));
            Assert.That(round.Board.TryGetHeroBodyCoordinate(Razzle, 2, out var bodyTwo) &&
                bodyTwo.Equals(H(3, -1)), Is.True);
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { Origin }), "body 1 is the other one now.");
            round.Answer(EffectValue.Space(Origin));
            round.Answer(EffectValue.Space(H(1, 0)));

            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, H(1, 0)),
                new HeroOccupancy(Razzle, 2, H(3, -1))
            }));
        }

        [Test]
        public void GroupPerformance_MovesTheOtherBodyUpToTwo_ZeroIncluded()
        {
            var round = new MoveRound(RazzleCardContent.GroupPerformanceDefinition,
                bodiesAt: new[] { H(-3, 0) }, allyAt: H(0, -2));
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));
            round.Answer(EffectValue.Space(H(-3, 0)));

            var offered = round.OfferedSpaces();
            Assert.That(offered, Has.Member(H(-3, 0)), "\"up to 2\" admits staying put (W5).");
            Assert.That(offered, Has.Member(H(-5, 0)), "two spaces.");
            Assert.That(offered.All(at => HexDistanceQuery.Calculate(at, H(-3, 0)) <= 2), Is.True);
            round.Answer(EffectValue.Space(H(-5, 1)));
            Assert.That(round.Bodies().Single(body => body.Body == 2).Coordinate, Is.EqualTo(H(-5, 1)));
        }

        [Test]
        public void TeamSpirit_MovesTheOtherBodyUpToThree()
        {
            var round = new MoveRound(RazzleCardContent.TeamSpiritDefinition,
                bodiesAt: new[] { H(-1, 3) }, allyAt: H(4, 0));
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));
            round.Answer(EffectValue.Space(H(-1, 3)));

            var offered = round.OfferedSpaces();
            Assert.That(offered.Max(at => HexDistanceQuery.Calculate(at, H(-1, 3))), Is.EqualTo(3));
            round.Answer(EffectValue.Space(H(-4, 3)));
            Assert.That(round.Bodies().Single(body => body.Body == 2).Coordinate, Is.EqualTo(H(-4, 3)));
        }

        [Test]
        public void EveryOtherBodyOffered_IsTheBodyTheMoveThenMoves()
        {
            var round = new MoveRound(RazzleCardContent.GroupPerformanceDefinition,
                bodiesAt: new[] { H(-3, 0), H(3, -3), H(0, 4) }, allyAt: H(0, -2));
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));

            var bytes = round.Snapshot();
            var offered = round.OfferedSpaces();
            Assert.That(offered, Is.EquivalentTo(new[] { H(-3, 0), H(3, -3), H(0, 4) }));
            foreach (var space in offered)
            {
                var copy = round.Restore(bytes);
                var body = copy.Board.GetHeroBodies(Razzle).Single(value => value.Coordinate.Equals(space)).Body;
                MoveRound.Answer(copy.Round, EffectValue.Space(space));
                var step = space.GetNeighbors().First(at => copy.Board.IsEmptyPlayableSpace(at));
                MoveRound.Answer(copy.Round, EffectValue.Space(step));
                Assert.That(copy.Board.TryGetHeroBodyCoordinate(Razzle, body, out var now) && now.Equals(step),
                    Is.True, $"the body on {space} is the one that moved");
            }
        }

        [Test]
        public void Alleyoop_RestoresAndReplays_AtTheOtherBody_AndAtItsMove()
        {
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(-3, 0) }, allyAt: H(2, -1));
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Hero(Ally));
            round.AssertRestoreReplays(EffectValue.Space(H(-3, 0)));
            round.AssertRestoreReplays(EffectValue.Space(H(-4, 0)));
            Assert.That(round.Bodies().Single(body => body.Body == 2).Coordinate, Is.EqualTo(H(-4, 0)));
        }

        [Test]
        public void Alleyoop_RestoresAndReplays_AtTheFriendlyHero_WithBodyTwoActing()
        {
            var round = new MoveRound(RazzleCardContent.AlleyoopDefinition,
                bodiesAt: new[] { H(3, -3) }, allyAt: H(3, -1));
            round.PickActingBody(2);
            round.StartSkill();
            round.AssertRestoreReplays(EffectValue.Hero(Ally));
            Assert.That(round.Board.TryGetHeroBodyCoordinate(Razzle, 2, out var bodyTwo) &&
                bodyTwo.Equals(H(3, -1)), Is.True);
        }

        // --- Magic Trick, Aaaand it's gone! ---------------------------------------------------------

        [Test]
        public void MagicTrick_OffersAUnitOfEitherSide_NeverHisOwnBody()
        {
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                bodiesAt: new[] { H(1, 0) }, redMinionsAt: new[] { H(-1, 0) }, blueMinionsAt: new[] { H(0, 1) });
            round.PickActingBody(1);
            round.StartSkill();
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.PushSubjectNode(RazzleCardContent.MagicTrick)));
            Assert.That(round.Candidates(), Is.EquivalentTo(new[]
            {
                EffectValue.Minion(MoveRound.RedMinion(1)), EffectValue.Minion(MoveRound.BlueMinion(1))
            }), "\"a unit\" is either side (p.19); body 2 beside him is never one (RZ-Q5).");
        }

        [Test]
        public void MagicTrick_WithOnlyHisOwnBodyBesideHim_IsGreyed()
        {
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition, bodiesAt: new[] { H(1, 0) });
            round.PickActingBody(1);
            Assert.That(round.IsOffered(RazzleCardContent.PrimarySkill), Is.False);
        }

        [TestCase(2, 2)]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void MagicTrick_RetreatsExactlyTheSpacesTheTargetMoved(int pushed, int retreated)
        {
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition, redMinionsAt: new[] { H(-1, 0) });
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.PushNode(RazzleCardContent.MagicTrick)));
            Assert.That(round.Candidates(), Is.EqualTo(new[]
            {
                EffectValue.Integer(0), EffectValue.Integer(1), EffectValue.Integer(2)
            }), "\"up to 2\" (W5).");
            round.Answer(EffectValue.Integer(pushed));

            Assert.That(round.ActionEnded, Is.True, "the retreat asks nothing: the count is the card's.");
            Assert.That(round.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-1 - pushed, 0)));
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, H(retreated, 0)) }),
                "straight away from where the target stood, one space per space it moved (RZ-Q10).");
        }

        [Test]
        public void MagicTrick_APushStoppedShort_RetreatsOnlyTheSpacesTheTargetActuallyMoved()
        {
            // A second minion at (-3, 0) stops the pushed one after 1 space.
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                redMinionsAt: new[] { H(-1, 0), H(-3, 0) });
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            round.Answer(EffectValue.Integer(2));

            Assert.That(round.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-2, 0)));
            Assert.That(round.Bodies().Single().Coordinate, Is.EqualTo(H(1, 0)),
                "the target moved 1, so he moves 1 - not the 2 chosen.");
        }

        [Test]
        public void MagicTrick_APushBlockedAtOnce_LeavesHimWhereHeIs()
        {
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                redMinionsAt: new[] { H(-1, 0), H(-2, 0) });
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            round.Answer(EffectValue.Integer(2));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-1, 0)));
            Assert.That(round.Bodies().Single().Coordinate, Is.EqualTo(Origin));
        }

        [Test]
        public void MagicTrick_ABlockedRetreat_StopsBeforeTheBlock()
        {
            // A friendly minion at (2, 0): the retreat of 2 stops after 1.
            var stopped = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                redMinionsAt: new[] { H(-1, 0) }, blueMinionsAt: new[] { H(2, 0) });
            stopped.StartSkill();
            stopped.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            stopped.Answer(EffectValue.Integer(2));
            Assert.That(stopped.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-3, 0)));
            Assert.That(stopped.Bodies().Single().Coordinate, Is.EqualTo(H(1, 0)), "blocked, it stops (p.11).");

            // The board's edge: from (4, 0) there is one space, (5, 0), before the edge.
            var edge = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                bodyOneAt: H(4, 0), redMinionsAt: new[] { H(3, 0) });
            edge.StartSkill();
            edge.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            edge.Answer(EffectValue.Integer(2));
            Assert.That(edge.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(1, 0)));
            Assert.That(edge.Bodies().Single().Coordinate, Is.EqualTo(H(5, 0)));

            // Blocked at the first space: the target moves, he does not.
            var still = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                redMinionsAt: new[] { H(-1, 0) }, blueMinionsAt: new[] { H(1, 0) });
            still.StartSkill();
            still.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            still.Answer(EffectValue.Integer(2));
            Assert.That(still.ActionEnded, Is.True);
            Assert.That(still.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-3, 0)));
            Assert.That(still.Bodies().Single().Coordinate, Is.EqualTo(Origin));
        }

        [Test]
        public void MagicTrick_PushesAnEnemyHero_AndRetreatsTheActingBodyOnly()
        {
            // Body 2 acts from (3, -3), the enemy hero beside it at (3, -2); body 1 stays at the origin.
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                bodiesAt: new[] { H(3, -3) }, enemyAt: H(3, -2));
            round.PickActingBody(2);
            round.StartSkill();
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Hero(Enemy) }),
                "adjacent to the acting body only.");
            round.Answer(EffectValue.Hero(Enemy));
            round.Answer(EffectValue.Integer(2));

            Assert.That(round.Board.TryGetHeroCoordinate(Enemy, out var enemy) && enemy.Equals(H(3, 0)), Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, Origin),
                new HeroOccupancy(Razzle, 2, H(3, -5))
            }), "\"this one of you\" is the acting body.");
        }

        [Test]
        public void AaaandItsGone_PushesUpToThree_AndRetreatsThree()
        {
            var round = new MoveRound(RazzleCardContent.AaaandItsGoneDefinition,
                bodyOneAt: H(1, 0), redMinionsAt: new[] { H(0, 0) });
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            Assert.That(round.Candidates(), Has.Member(EffectValue.Integer(3)));
            round.Answer(EffectValue.Integer(3));
            Assert.That(round.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(H(-3, 0)));
            Assert.That(round.Bodies().Single().Coordinate, Is.EqualTo(H(4, 0)));
        }

        [Test]
        public void MagicTrick_RestoresAndReplays_AtThePushDistance()
        {
            var round = new MoveRound(RazzleCardContent.MagicTrickDefinition,
                bodiesAt: new[] { H(3, -3) }, redMinionsAt: new[] { H(-1, 0) });
            round.PickActingBody(1);
            round.StartSkill();
            round.AssertRestoreReplays(EffectValue.Minion(MoveRound.RedMinion(1)));
            round.AssertRestoreReplays(EffectValue.Integer(2));
            Assert.That(round.Bodies().First().Coordinate, Is.EqualTo(H(2, 0)));
        }

        // --- Tightrope, High Wire, Wire Dancers -----------------------------------------------------

        [Test]
        public void Tightrope_AfterTheMovement_MovesAnotherBodyOneSpace()
        {
            var round = new MoveRound(RazzleCardContent.TightropeDefinition, bodiesAt: new[] { H(-4, 4) });
            round.PickActingBody(1);
            round.StartMovement();
            round.MoveTo(H(2, 0));

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.OtherBodyNode(RazzleCardContent.Tightrope)));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(-4, 4) }), "any other body, anywhere (RZ-Q9).");
            Assert.That(round.Turn.PendingEffectChoice.DeclineInvocation, Is.Not.Null, "\"you may\".");
            round.Answer(EffectValue.Space(H(-4, 4)));
            Assert.That(round.OfferedSpaces().All(at => HexDistanceQuery.Calculate(at, H(-4, 4)) == 1), Is.True);
            round.Answer(EffectValue.Space(H(-3, 3)));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, H(2, 0)),
                new HeroOccupancy(Razzle, 2, H(-3, 3))
            }));
        }

        [Test]
        public void Tightrope_AfterAMovementOfZeroSpaces_StillApplies()
        {
            var round = new MoveRound(RazzleCardContent.TightropeDefinition, bodiesAt: new[] { H(-4, 4) });
            round.PickActingBody(1);
            round.StartMovement();
            round.StopMovement();

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.OtherBodyNode(RazzleCardContent.Tightrope)),
                "p.11: the text applies even after moving zero spaces (RZ-Q9).");
            round.Answer(EffectValue.Space(H(-4, 4)));
            round.Answer(EffectValue.Space(H(-4, 3)));
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { Origin, H(-4, 3) }));
        }

        [Test]
        public void Tightrope_AfterAFastTravel_DoesNotApply()
        {
            // No enemy unit on the board, so p.11's Fast Travel is open within the one Zone.
            var round = new MoveRound(RazzleCardContent.TightropeDefinition,
                bodiesAt: new[] { H(-4, 4) }, enemyOffBoard: true);
            round.PickActingBody(1);
            round.FastTravelTo(H(3, -3));

            Assert.That(round.ActionEnded, Is.True, "Fast Travel replaces the Movement; no text (RZ-Q9).");
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, H(3, -3)),
                new HeroOccupancy(Razzle, 2, H(-4, 4))
            }));
        }

        [Test]
        public void Tightrope_WithOneBody_AsksNothingAfterTheMovement()
        {
            var round = new MoveRound(RazzleCardContent.TightropeDefinition);
            round.StartMovement();
            round.MoveTo(H(1, 0));
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, H(1, 0)) }));
        }

        [Test]
        public void Tightrope_Declined_MovesNoOtherBody()
        {
            var round = new MoveRound(RazzleCardContent.TightropeDefinition, bodiesAt: new[] { H(-4, 4) });
            round.PickActingBody(1);
            round.StartMovement();
            round.MoveTo(H(1, 0));
            round.Decline();
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { H(1, 0), H(-4, 4) }));
        }

        [Test]
        public void HighWireAndWireDancers_MoveAnotherBodyUpToTwoAndThree_FromBodyTwoActing()
        {
            foreach (var (face, most) in new[]
            {
                (RazzleCardContent.HighWireDefinition, 2), (RazzleCardContent.WireDancersDefinition, 3)
            })
            {
                var round = new MoveRound(face, bodiesAt: new[] { H(3, -3) });
                round.PickActingBody(2);
                round.StartMovement();
                round.MoveTo(H(3, -1));
                Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { Origin }), face.DisplayName + ": body 1 is the other.");
                round.Answer(EffectValue.Space(Origin));
                var offered = round.OfferedSpaces();
                Assert.That(offered, Has.Member(Origin), face.DisplayName + ": \"up to\" admits 0 (W5).");
                Assert.That(offered.Max(at => HexDistanceQuery.Calculate(at, Origin)), Is.EqualTo(most), face.DisplayName);
                var far = offered.First(at => HexDistanceQuery.Calculate(at, Origin) == most);
                round.Answer(EffectValue.Space(far));
                Assert.That(round.Bodies(), Is.EqualTo(new[]
                {
                    new HeroOccupancy(Razzle, 1, far),
                    new HeroOccupancy(Razzle, 2, H(3, -1))
                }), face.DisplayName);
            }
        }

        [Test]
        public void Tightrope_RestoresAndReplays_AtTheOtherBody()
        {
            var round = new MoveRound(RazzleCardContent.TightropeDefinition,
                bodiesAt: new[] { H(-4, 4), H(4, -1) });
            round.PickActingBody(3);
            round.StartMovement();
            round.StopMovement();
            round.AssertRestoreReplays(EffectValue.Space(H(-4, 4)));
            round.AssertRestoreReplays(EffectValue.Space(H(-3, 4)));
            Assert.That(round.Bodies().Select(body => body.Coordinate),
                Is.EqualTo(new[] { Origin, H(-3, 4), H(4, -1) }));
        }

        // --- Theatrics ------------------------------------------------------------------------------

        [Test]
        public void Theatrics_SwapsTheActingBodyWithAMinionOfEitherSideInRangeTwo()
        {
            var round = new MoveRound(RazzleCardContent.TheatricsDefinition,
                bodiesAt: new[] { H(0, -1) }, enemyAt: H(1, 0),
                redMinionsAt: new[] { H(2, -1), H(3, 0) }, blueMinionsAt: new[] { H(-2, 2) });
            round.PickActingBody(1);
            round.StartSkill();
            Assert.That(round.Candidates(), Is.EquivalentTo(new[]
            {
                EffectValue.Minion(MoveRound.RedMinion(1)), EffectValue.Minion(MoveRound.BlueMinion(1))
            }), "minions of either side within Range 2; not the one 3 away, not a hero, not his body.");
            round.Answer(EffectValue.Minion(MoveRound.BlueMinion(1)));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.MinionAt(MoveRound.BlueMinion(1)), Is.EqualTo(Origin));
            Assert.That(round.Bodies(), Is.EqualTo(new[]
            {
                new HeroOccupancy(Razzle, 1, H(-2, 2)),
                new HeroOccupancy(Razzle, 2, H(0, -1))
            }));
        }

        [Test]
        public void Theatrics_FromBodyTwo_MeasuresItsRangeFromThatBody()
        {
            var round = new MoveRound(RazzleCardContent.TheatricsDefinition,
                bodiesAt: new[] { H(4, -4) }, redMinionsAt: new[] { H(-2, 0), H(4, -2) });
            round.PickActingBody(2);
            round.StartSkill();
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Minion(MoveRound.RedMinion(2)) }));
            round.AssertRestoreReplays(EffectValue.Minion(MoveRound.RedMinion(2)));
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { Origin, H(4, -2) }));
        }

        // --- RZ-Q5 in the printed-icon selection itself ---------------------------------------------

        /// <summary>
        /// The printed-icon selections' own contract, with no Swap after it to refuse: "a friendly hero
        /// in range" never names one of the actor's own bodies (RZ-Q5, p.17). Before 9.21f the disk left
        /// out only the acting body's space, so body 2 in range was offered as a friendly hero.
        /// </summary>
        [Test]
        public void AFriendlyHeroSelection_WithNoConsumer_NeverNamesHisOtherBody()
        {
            var alone = new MoveRound(FriendlyHeroProbe, bodiesAt: new[] { H(1, 0) });
            alone.PickActingBody(1);
            Assert.That(alone.IsOffered(RazzleCardContent.PrimarySkill), Is.False,
                "body 2 beside him is not \"a friendly hero\"; nobody else is in range.");

            var withAlly = new MoveRound(FriendlyHeroProbe, bodiesAt: new[] { H(1, 0) }, allyAt: H(0, 2));
            withAlly.PickActingBody(1);
            withAlly.StartSkill();
            Assert.That(withAlly.Candidates(), Is.EqualTo(new[] { EffectValue.Hero(Ally) }));
        }

        private static readonly CardDefinition FriendlyHeroProbe = new CardDefinition(
            new CardDefinitionId("razzle-move-friendly-probe"), "rev-1", "TEST - A friendly hero in range",
            new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    RazzleCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    new EffectDefinition(
                        new EffectDefinitionId("razzle-move-friendly-probe-text"), "rev-1",
                        new EffectNodeId("probe-friend"),
                        new[]
                        {
                            new EffectNodeDefinition(
                                new EffectNodeId("probe-friend"), EffectNodeKind.Capability,
                                EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                                inputs: new[]
                                {
                                    EffectInputBinding.FromClosedVocabulary(
                                        "activity", RuleActivity.Skill),
                                    EffectInputBinding.FromClosedVocabulary(
                                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                                        SourceTeamRelation.AlliedWithSource),
                                    EffectInputBinding.FromLiteral(
                                        SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                        EffectValue.Boolean(false))
                                },
                                outputs: new[]
                                {
                                    new EffectOutputBinding(
                                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                                })
                        }))
            });

        // --- The new capabilities, alone ------------------------------------------------------------

        [Test]
        public void TheNamedPush_PublishesItsRealisedTravel_AndTheBatchDoesNotDeclareIt()
        {
            var named = SyntheticEffectCapabilities.CreateCatalog(RazzleEffectContent.Handlers());
            Assert.That(named.TryLoad(SyntheticDisplacementCapabilities.PushTargetV3, out var push), Is.True);
            Assert.That(push.Schema.Outputs.Select(output => output.OutputName),
                Has.Member(SyntheticDisplacementCapabilities.PushedSpacesOutputName));
            Assert.That(named.TryLoad(SyntheticDisplacementCapabilities.ObjectPushV3, out var batch), Is.True);
            Assert.That(batch.Schema.Outputs.Select(output => output.OutputName),
                Has.No.Member(SyntheticDisplacementCapabilities.PushedSpacesOutputName));
        }

        // --- The harness ----------------------------------------------------------------------------

        private static void AssertFace(
            CardDefinition face, CardColor color, CardTier tier, int initiative, int defense, int movement,
            PrintedValueChannel item, int range, string text)
        {
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative), face.DisplayName);
            Assert.That((face.Progression.Color, face.Progression.Tier, face.Progression.ItemIcon),
                Is.EqualTo((color, tier, item)), face.DisplayName);
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(text), face.DisplayName);
            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(skill.BaseValues.Select(value => (value.Channel, value.Amount)),
                range > 0
                    ? (NUnit.Framework.Constraints.Constraint)Is.EqualTo(new[] { (PrintedValueChannel.Range, range) })
                    : Is.Empty,
                face.DisplayName + ": \"Skill - Ranged\" prints the Range icon; \"Skill\" prints none.");
            Assert.That(face.ActionDefinitions.Single(action =>
                    action.Kind == ActionKind.Movement && action.Origin != ActionDefinitionOrigin.PrintedPrimary)
                .BaseValues.Single().Amount, Is.EqualTo(movement), face.DisplayName);
            Assert.That(face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(defense), face.DisplayName);
        }

        private static readonly CardDefinition OtherHold = new CardDefinition(
            new CardDefinitionId("razzle-move-other-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        /// <summary>
        /// One round on the disk with one of Razzle's faces committed and Razzle (Blue) selected to act,
        /// his supply of four bodies declared on the board (RZ-Q1).
        /// </summary>
        private sealed class MoveRound
        {
            internal static MinionId RedMinion(int index) => new MinionId($"razzle-move-red-{index}");

            internal static MinionId BlueMinion(int index) => new MinionId($"razzle-move-blue-{index}");

            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly MatchContentCatalogs catalogs;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly QuickGameMinionSetupState[] minions;

            internal MoveRound(
                CardDefinition face,
                HexCoordinate? bodyOneAt = null,
                IReadOnlyList<HexCoordinate> bodiesAt = null,
                HexCoordinate? allyAt = null,
                HexCoordinate? enemyAt = null,
                bool enemyOffBoard = false,
                IReadOnlyList<HexCoordinate> redMinionsAt = null,
                IReadOnlyList<HexCoordinate> blueMinionsAt = null)
            {
                var participants = allyAt.HasValue
                    ? StagedBoardParticipants.CreateWithBlueAlly()
                    : StagedBoardParticipants.Create();
                var heroes = new List<HeroOccupancy> { new HeroOccupancy(Razzle, bodyOneAt ?? Origin) };
                var staged = bodiesAt ?? Array.Empty<HexCoordinate>();
                for (var at = 0; at < staged.Count; at++)
                {
                    heroes.Add(new HeroOccupancy(Razzle, at + 2, staged[at]));
                }

                if (allyAt.HasValue)
                {
                    heroes.Add(new HeroOccupancy(Ally, allyAt.Value));
                }

                if (!enemyOffBoard)
                {
                    heroes.Add(new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5)));
                }

                var placed = new List<QuickGameMinionSetupState>();
                var index = 0;
                foreach (var at in redMinionsAt ?? Array.Empty<HexCoordinate>())
                {
                    placed.Add(Minion(RedMinion(++index), StagedBoardParticipants.RedTeamIdValue, TeamSide.Red, at));
                }

                index = 0;
                foreach (var at in blueMinionsAt ?? Array.Empty<HexCoordinate>())
                {
                    placed.Add(Minion(BlueMinion(++index), StagedBoardParticipants.BlueTeamIdValue, TeamSide.Blue, at));
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

                source = CardInstance.CreateInHand(new CardInstanceId("razzle-move-source"), face.Reference, Razzle);
                var others = new List<CardInstance>
                {
                    CardInstance.CreateInHand(new CardInstanceId("razzle-move-enemy"), OtherHold.Reference, Enemy)
                };
                if (allyAt.HasValue)
                {
                    others.Add(CardInstance.CreateInHand(new CardInstanceId("razzle-move-ally"), OtherHold.Reference, Ally));
                }

                definitions = new[] { face, OtherHold };
                instances = new[] { source }.Concat(others).ToArray();
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(RazzleEffectContent.Handlers()),
                    new ComponentDefinitionCatalog(
                        Array.Empty<TokenDefinition>(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Blue), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));

                Assert.That(round.TryCommitCard(Razzle, source.Id).Accepted, Is.True);
                foreach (var card in others)
                {
                    Assert.That(round.TryCommitCard(card.OwnerId, card.Id).Accepted, Is.True);
                }

                // Wire Dancers' Initiative 1 ties the others' 1: the Blue Tie Breaker puts him first.
                var tie = Turn.PendingChoice;
                if (tie != null)
                {
                    Assert.That(round.TryChooseTiedActor(TeamSide.Blue, source.Id).Accepted, Is.True);
                }

                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Razzle), "Fixture precondition: Razzle acts first.");
            }

            internal AdjacentMoveAuthority Board => board;

            internal TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingMovementChoice == null &&
                Turn.PendingDefenseChoice == null && Turn.PendingAttackTargetChoice == null;

            internal HeroOccupancy[] Bodies() => board.GetHeroBodies(Razzle).ToArray();

            internal HexCoordinate MinionAt(MinionId minion) =>
                board.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;

            internal EffectValue[] Candidates() =>
                Turn.PendingEffectChoice.Candidates.Select(candidate => candidate.Value).ToArray();

            internal HexCoordinate[] OfferedSpaces() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();

            internal void PickActingBody(int body)
            {
                Assert.That(Turn.PendingActingBodyChoice, Is.Not.Null,
                    "several bodies stand, so \"which of you acts\" is asked first.");
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

            internal void StartSkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("razzle-move-skill"),
                    Offer(RazzleCardContent.PrimarySkill)).Accepted, Is.True);

            internal void StartMovement() =>
                Assert.That(round.TryStartMovement(new ActionResolutionId("razzle-move-movement"),
                    Offer(RazzleCardContent.PrimaryMovement)).Accepted, Is.True);

            internal void MoveTo(HexCoordinate destination)
            {
                var choice = Turn.PendingMovementChoice;
                Assert.That(choice, Is.Not.Null, "p.11's Movement is asking where to go");
                Assert.That(round.TryAnswerMovementChoice(MovementChoiceCommand.SelectDestination(
                    choice.ResolutionId, choice.ChoiceId, destination)).Accepted, Is.True);
                if (Turn.PendingMovementChoice != null)
                {
                    StopMovement();
                }
            }

            internal void StopMovement()
            {
                var choice = Turn.PendingMovementChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(
                    choice.ResolutionId, choice.ChoiceId)).Accepted, Is.True);
            }

            internal void FastTravelTo(HexCoordinate destination)
            {
                var offer = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                    RazzleCardContent.PrimaryMovement, source.Id, Razzle, definitions,
                    instances, Turn.CurrentModifications());
                Assert.That(offer.HasValue, Is.True, "p.11: Fast Travel may replace the Movement.");
                Assert.That(round.TryPerformFastTravel(new ActionResolutionId("razzle-move-fast-travel"),
                    offer.Value, RazzleCardContent.PrimaryMovement, destination, board.Minions).Accepted,
                    Is.True);
            }

            internal void Answer(EffectValue value) => Answer(round, value);

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null);
                Assert.That(round.TryAnswerEffectChoice(choice.DeclineInvocation).Accepted, Is.True);
            }

            internal string Snapshot() => AuthoritativeSnapshotSerializer.Serialize(round, board);

            internal RestoredAuthoritativeMatch Restore(string bytes) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

            /// <summary>
            /// Snapshot at the open question, restore it, answer both the same way, and require the same
            /// question and the same authoritative bytes - then once more from the retained prefix.
            /// </summary>
            internal void AssertRestoreReplays(EffectValue answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = Snapshot();
                var restored = Restore(bytes);
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));
                Assert.That(restored.Board.GetHeroBodies(Razzle), Is.EqualTo(board.GetHeroBodies(Razzle)));
                Assert.That(restored.Board.ActingBodies, Is.EqualTo(board.ActingBodies));

                Answer(answer);
                Answer(restored.Round, answer);
                var live = Snapshot();
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(live), "the restored pause replays to the same authoritative bytes");

                var prefix = Restore(bytes);
                Answer(prefix.Round, answer);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                    Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
            }

            internal static void Answer(RoundResolutionAuthority target, EffectValue value)
            {
                var asked = target.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(asked, Is.Not.Null, "a question is open");
                var invocation = asked.SelectInvocations.SingleOrDefault(
                    candidate => candidate.Candidate.Value.Equals(value));
                Assert.That(invocation, Is.Not.Null, $"{value} is offered");
                Assert.That(target.TryAnswerEffectChoice(invocation).Accepted, Is.True);
                var still = target.CurrentTurnAuthority.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
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

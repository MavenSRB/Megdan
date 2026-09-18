using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WidgetAnchoredSkillTests;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Drag Off / Carry Away; WI-Q1 same distance/direction, simultaneous feasibility.</summary>
    public sealed class WidgetCoupledMoveTests
    {
        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void PairFollowsVacatedSpacesInEveryDirectionAtBothPrintedDistances(HexDirection direction)
        {
            foreach (var distance in new[] { 2, 3 })
            foreach (var tokenLeads in new[] { false, true })
            {
                var back = At(2, 2);
                var front = StraightLineQuery.Step(back, direction, 1);
                var pyro = tokenLeads ? front : back;
                var enemy = tokenLeads ? back : front;
                var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), pyro, enemy);
                var original = p.Board.PlacedTokens.Single();
                p.Pick(EffectValue.Hero(Enemy));
                Assert.That(p.Choices.All(v => new[] { 2, 3 }.Contains(HexDistanceQuery.Calculate(pyro, v.Coordinate))), Is.True);
                p.Pick(EffectValue.Space(StraightLineQuery.Step(pyro, direction, distance)));
                Assert.That(p.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(StraightLineQuery.Step(pyro, direction, distance)));
                Assert.That(p.Board.Occupancies.Single(h => h.HeroId.Equals(Enemy)).Coordinate,
                    Is.EqualTo(StraightLineQuery.Step(enemy, direction, distance)));
                Assert.That(p.Board.PlacedTokens.Single().Id, Is.EqualTo(original.Id));
                Assert.That(p.Board.PlacedTokens.Single().SourceCardInstanceId, Is.EqualTo(original.SourceCardInstanceId));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PairedRouteRevalidatesBothObjectsAndRefusesAtomically(bool blocksToken)
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), At(1), At(1, 1));
            p.Pick(EffectValue.Hero(Enemy));
            var choice = p.Choice;
            var command = choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(EffectValue.Space(At(3))));
            // A live occupancy change invalidates the whole pair, even though the other path is clear.
            var blocking = blocksToken ? At(3) : At(3, 1);
            Assert.That(p.Board.CommitValidatedDisplacement(BoardObjectReference.ForHero(Actor), blocking).Accepted, Is.True);
            p.Answer(command);
            if (p.Choice != null) p.Answer(p.Choice.ConfirmInvocation);
            Assert.That(p.Step.Events.Any(e => e.ResultKind == EffectExecutionResultKind.ImpossibleMandatory), Is.True);
            Assert.That(p.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(1)));
            Assert.That(p.Board.Occupancies.Single(h => h.HeroId.Equals(Enemy)).Coordinate, Is.EqualTo(At(1, 1)));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void OnlyCarryAwayIgnoresTransitObstacles(bool carry, bool admitted)
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", carry), At(1), At(1, 1),
                terrain: new[] { At(2), At(2, 1) });
            p.Pick(EffectValue.Hero(Enemy));
            Assert.That(p.Choices.Contains(EffectValue.Space(At(3))), Is.EqualTo(admitted));
            Assert.That(p.Choices, Has.No.Member(EffectValue.Space(At(2))));
            if (admitted) p.Pick(EffectValue.Space(At(3)));
        }

        [Test]
        public void CarryAwayStillRequiresBothEmptyPlayableEndpointsAndCannotLeaveMap()
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", true), At(1), At(1, 1), terrain: new[] { At(3, 1) });
            p.Pick(EffectValue.Hero(Enemy));
            Assert.That(p.Choices, Has.No.Member(EffectValue.Space(At(3))));
            Assert.That(p.Board.TryMoveCoupled(BoardObjectReference.ForToken(p.Board.PlacedTokens.Single().Id),
                BoardObjectReference.ForHero(Enemy), HexDirection.East, 10, RuleModificationSet.Empty, true), Is.False);
            Assert.That(p.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(1)));
        }

        [TestCase(RuleActivity.Skill, false)]
        [TestCase(RuleActivity.DirectMove, false)]
        [TestCase(RuleActivity.Attack, true)]
        public void ConsumerFeasibilityFiltersSkillAndDirectMoveImmunity(RuleActivity activity, bool admitted)
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), At(1), At(2), rules: Immunity(activity));
            Assert.That(p.Choices.Contains(EffectValue.Hero(Enemy)), Is.EqualTo(admitted));
        }

        [Test]
        public void DragOffHasNoWidgetRangeLimitButRequiresEnemyAdjacentToPyro()
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), At(4), At(5), range: 1);
            Assert.That(p.Choices, Has.Member(EffectValue.Hero(Enemy)));
            var nonadjacent = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), At(4), At(2));
            Assert.That(nonadjacent.Choices, Is.Empty);
            var missing = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), null, At(2));
            Assert.That(missing.Choices, Is.Empty);
        }

        [Test]
        public void VacatingPartnerDoesNotEraseAnOverlay_AndCarryOnlyIgnoresItsTransit()
        {
            var overlay = AppliedObstacleOverlay.Bind(ConditionalObstacleOverlay.Declare(SourceTeamRelation.AnyTeam,
                ObstacleOverlayArm.SpacesInsideWhileActorOutside), At(2), 0, "test-overlay", Enemy,
                TeamSide.Blue, TeamSide.Red, At(0));
            var rules = new RuleModificationSet(Array.Empty<AppliedRuleModification>(), new[] { overlay }, new TieBreakerState(TeamSide.Red));
            foreach (var carry in new[] { false, true })
            {
                var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", carry), At(1), At(2), rules: rules);
                p.Pick(EffectValue.Hero(Enemy));
                Assert.That(p.Choices.Contains(EffectValue.Space(At(3))), Is.EqualTo(carry), "The partner's old square still has the overlay.");
                Assert.That(p.Board.CanMoveCoupled(BoardObjectReference.ForToken(p.Board.PlacedTokens.Single().Id),
                    BoardObjectReference.ForHero(Enemy), HexDirection.East, 1, rules, true), Is.False, "An overlaid endpoint is never legal.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StandingHeavyProtectionAppliesToAnchoredSkillsAndPairSelection(bool accompanied)
        {
            var map = BoardMapDefinition.CreateSyntheticWithStartingBattleZone(new MapId("widget-heavy"), 1,
                Enumerable.Range(0, 6).Select(q => At(q)), new[] { At(-1), At(-2) }, Array.Empty<HexCoordinate>());
            QuickGameMinionSetupState Piece(string id, int q, StandardMinionType type) => new QuickGameMinionSetupState(
                new MinionId(id), new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue, type,
                new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("spawn-" + id)),
                new ConfiguredSpaceReference(map.Id, map.Spaces.Single(s => s.Coordinate.Equals(At(q))).Id), At(q));
            var heavy = Piece("heavy", 2, StandardMinionType.Heavy);
            var minions = accompanied ? new[] { heavy, Piece("escort", 5, StandardMinionType.Melee) } : new[] { heavy };
            foreach (var effect in new[] { WidgetEffectContent.DragOff(Card, "rev-1", true),
                WidgetEffectContent.Nibble(Card, "rev-1", false), WidgetEffectContent.Nibble(Card, "rev-1", true) })
            {
                var p = new Probe(effect, At(1), At(-2), map: map, minions: minions);
                Assert.That(p.Choices.Contains(EffectValue.Minion(heavy.MinionId)), Is.EqualTo(!accompanied));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothChoicePausesRestoreAndContinueIdentically(bool destinationPause)
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", true), At(1), At(2));
            if (destinationPause) p.Pick(EffectValue.Hero(Enemy));
            var round = p.AsRound();
            var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, p.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(p.Catalogs, snapshot, p.Definitions,
                new[] { p.Board.MapDefinition }, p.Minions);
            foreach (var branch in new[] { round, restored.Round })
            {
                if (!destinationPause) PickRound(branch, EffectValue.Hero(Enemy));
                var stale = branch.CurrentTurnAuthority.PendingEffectChoice.SelectInvocations.First();
                PickRound(branch, EffectValue.Space(At(3)));
                Assert.That(branch.TryAnswerEffectChoice(stale).Accepted, Is.False);
            }
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, p.Board)));
            var repeated = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", true), At(1), At(2));
            if (destinationPause) repeated.Pick(EffectValue.Hero(Enemy));
            var replay = repeated.AsRound();
            if (!destinationPause) PickRound(replay, EffectValue.Hero(Enemy));
            PickRound(replay, EffectValue.Space(At(3)));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replay, repeated.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, p.Board)), "Fresh execution of the same decisions is deterministic.");
        }

        [Test]
        public void EnemyMinionMovesWithPyro_ThenCorrectsOnlyAtTheActionBoundary()
        {
            var map = BoardMapDefinition.CreateSyntheticWithStartingBattleZone(new MapId("widget-correction"), 1,
                new[] { At(0), At(1), At(2) }, new[] { At(-1), At(-2), At(3), At(4), At(5) }, Array.Empty<HexCoordinate>());
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", false), At(1), At(-2), minion: At(2), map: map);
            var round = p.AsRound();
            PickRound(round, EffectValue.Minion(Minion));
            Assert.That(p.Board.Minions.Single().Coordinate, Is.EqualTo(At(2)));
            PickRound(round, EffectValue.Space(At(3)));
            Assert.That(p.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(3)));
            // Pyro blocks the only path back, so the existing correction Places into nearest empty BZ space.
            Assert.That(p.Board.Minions.Single().Coordinate, Is.EqualTo(At(2)));
            Assert.That(round.CurrentTurnAuthority.PendingEffectChoice, Is.Null);
        }

        [Test]
        public void CarryAwayCanCrossAnotherTokenWithoutRemovingOrRetargetingIt()
        {
            var p = new Probe(WidgetEffectContent.DragOff(Card, "rev-1", true), At(1), At(1, 1));
            var other = new TokenDefinition(new TokenDefinitionId("test-transit-token"), 1, "TEST", 1,
                Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.RetainedAtEndOfRound);
            p.Board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("other-token"), other, Enemy, Enemy,
                new CardInstanceId("blue"), At(2, 1)));
            p.Pick(EffectValue.Hero(Enemy)); p.Pick(EffectValue.Space(At(3)));
            Assert.That(p.Board.PlacedTokens.Single(t => t.Definition.Reference.Equals(other.Reference)).Coordinate, Is.EqualTo(At(2, 1)));
            Assert.That(p.Board.Occupancies.Single(h => h.HeroId.Equals(Enemy)).Coordinate, Is.EqualTo(At(3, 1)));
        }
    }
}

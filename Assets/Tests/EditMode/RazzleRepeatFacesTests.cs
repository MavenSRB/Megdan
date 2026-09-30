using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using Round = GuardsDigital.Tests.EditMode.RazzleStuntDoublesTests.Round;
using MoveRound = GuardsDigital.Tests.EditMode.RazzleMoveFacesTests.MoveRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21h. Razzle's repeats performed by a different body: Twin Strike (p.16 Ultimate, "After
    /// you perform a basic attack action a different you may repeat it once, targeting a different
    /// unit") and Spectacle (Green III, "Swap with a minion in range. May repeat once by a different one
    /// of you, targeting a different minion"), as the user settled RZ-Q11 and RZ-Q12 (A): another
    /// standing body performs the whole action again, measured from itself, against a different unit;
    /// Twin Strike's repeat includes the spawn, bounded by the supply of four.
    ///
    /// <para>Twin Strike runs on 9.21d's staged round (<see cref="RazzleStuntDoublesTests.Round"/>:
    /// Razzle Red, body 1 at the origin, Stunt Doubles committed) with the Ultimate unlocked; Spectacle on
    /// 9.21f's (<see cref="RazzleMoveFacesTests.MoveRound"/>: Razzle Blue). Nothing is registered.</para>
    /// </summary>
    public sealed class RazzleRepeatFacesTests
    {
        private static readonly HeroId RedRazzle = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId BlueEnemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId BlueRazzle = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate Origin = H(0, 0);

        // Twin Strike's board: body 2 two spaces west, the enemy hero between the two bodies (beside
        // both), a minion beside body 2 only and a minion beside body 1 only.
        private static readonly HexCoordinate BodyTwo = H(-2, 0);
        private static readonly HexCoordinate BetweenThem = H(-1, 0);
        private static readonly HexCoordinate BesideBodyTwo = H(-3, 0);
        private static readonly HexCoordinate BesideBodyOne = H(1, 0);
        private static readonly HexCoordinate FarCorner = H(4, -4);

        private static string TwinStrikeNode => RazzleEffectContent.TwinStrikeRepeatNode;

        // --- The printed faces ----------------------------------------------------------------------

        [Test]
        public void TwinStrike_IsAPermanentUltimateAfterEachBasicAttack_OneMayRepeatByADifferentBody()
        {
            var face = RazzleCardContent.TwinStrikeDefinition;
            Assert.That(face.DisplayName, Is.EqualTo("Twin Strike"));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(
                "After you perform a basic attack action a different you may repeat it once, targeting a " +
                "different unit."));
            Assert.That(face.Progression.Classification, Is.EqualTo(CardClassification.Ultimate));
            Assert.That(face.ActionDefinitions, Is.Empty, "p.16: a permanent ability, no action.");
            Assert.That(face.UltimateRule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsBasicAttack),
                "\"a basic attack action\": p.9's basic supertype of p.13's Attack, never a Red Attack.");

            var effect = RazzleEffectContent.TwinStrikeTriggeredEffect(face.ContentRevision);
            var repeat = effect.Nodes.Single();
            Assert.That((repeat.Kind, repeat.FailurePolicy, repeat.MaximumRepeats, repeat.RepeatTargetBinding),
                Is.EqualTo((EffectNodeKind.RepeatAction, EffectFailurePolicy.May, 1, RepeatTargetBinding.DifferentTarget)),
                "\"may repeat it once, targeting a different unit\".");
            Assert.That(repeat.Outputs.Select(output => (output.OutputName, output.ValueKind)), Is.EqualTo(new[]
            {
                (EffectNodeDefinition.RepeatActorBodyOutput, EffectValueKind.Coordinate)
            }), "\"a different you\": the node names the body that performs it.");
        }

        [Test]
        public void Spectacle_IsTheatricsSwapThenAMayRepeatByADifferentBody_OnADifferentMinion()
        {
            var face = RazzleCardContent.SpectacleDefinition;
            Assert.That(face.Initiative.Value, Is.EqualTo(1));
            Assert.That((face.Progression.Color, face.Progression.Tier, face.Progression.ItemIcon),
                Is.EqualTo((CardColor.Green, CardTier.III, PrintedValueChannel.Attack)));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(
                "Swap with a minion in range. May repeat once by a different one of you, targeting a " +
                "different minion."));
            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(2));
            Assert.That(face.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                .BaseValues.Single().Amount, Is.EqualTo(2));
            Assert.That(face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(2));

            var repeat = skill.EffectRoot.Nodes.Single(node => node.Kind == EffectNodeKind.RepeatAction);
            Assert.That((repeat.FailurePolicy, repeat.MaximumRepeats, repeat.RepeatTargetBinding),
                Is.EqualTo((EffectFailurePolicy.May, 1, RepeatTargetBinding.DifferentSelectionSubject)));
            Assert.That(repeat.BoundSelectionNodeId.Value,
                Is.EqualTo(RazzleEffectContent.SwapSubjectNode(RazzleCardContent.Spectacle)),
                "the minion the first swap named is the one the repeat may not take.");
            Assert.That(repeat.Outputs.Single().OutputName, Is.EqualTo(EffectNodeDefinition.RepeatActorBodyOutput));
            Assert.That(RazzleCardContent.TheatricsDefinition.Progression.NextTierCandidateIds,
                Has.Member(RazzleCardContent.Spectacle), "Theatrics upgrades to Spectacle.");
        }

        [Test]
        public void ADifferentBodyRepeat_IsAMayNode_AndPublishesOnlyTheBodysSpace()
        {
            Assert.That(() => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                    outputs: new[] { EffectNodeDefinition.PerformedByADifferentBody }),
                Throws.ArgumentException, "which body performs it is a question, so it is a `may` node.");
            Assert.That(() => new EffectNodeDefinition(
                    new EffectNodeId("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    outputs: new[] { new EffectOutputBinding("elsewhere", EffectValueKind.Coordinate) },
                    chooser: EffectChooserRole.Actor),
                Throws.ArgumentException, "a repeat publishes nothing but the performing body's space.");
        }

        // --- Twin Strike: who may repeat it ---------------------------------------------------------

        [Test]
        public void AfterStuntDoubles_TheOtherBody_PerformsTheRepeat_AgainstADifferentUnitBesideItself()
        {
            var round = TwinStrikeRound();
            AttackTheEnemyBetweenThem(round);

            Assert.That(round.PendingNode, Is.EqualTo(TwinStrikeNode),
                "after the basic Attack (and its spawn), a different you may repeat it.");
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { BodyTwo }),
                "only another body: body 1 performed the first instance.");
            Assert.That(round.Turn.PendingEffectChoice.DeclineInvocation, Is.Not.Null, "\"may\".");

            round.PickSpaces(BodyTwo);
            Assert.That(round.Board.TryGetActingBody(RedRazzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(2), "the second body acts for the repeated instance.");
            Assert.That(round.LegalTargetHeroes(), Is.Empty,
                "the enemy beside body 2 was the first target (\"targeting a different unit\").");
            Assert.That(round.LegalTargetMinions(), Is.EqualTo(new[] { Round.EnemyMinion(1) }),
                "adjacent to body 2, not the minion beside body 1 (p.12: non-Ranged means adjacent).");
        }

        [Test]
        public void TheRepeatedInstance_IncludesTheSpawn_MeasuredFromTheSecondBody()
        {
            var round = TwinStrikeRound();
            AttackTheEnemyBetweenThem(round);
            round.PickSpaces(BodyTwo);
            round.Target(Round.EnemyMinion(1));

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.SpawnNode(1)),
                "RZ-Q11: the whole action, its After-the-attack spawn included.");
            Assert.That(round.OfferedSpaces(), Is.Not.Empty);
            Assert.That(round.OfferedSpaces().All(at => HexDistanceQuery.Calculate(at, BodyTwo) == 1), Is.True,
                "Radius 1 of the body that performs the repeat.");
            round.PickSpaces(H(-2, -1));
            DeclineWhileSpawning(round);

            Assert.That(round.ActionEnded, Is.True,
                "p.10: a repeated action is not repeated again, so nothing further is asked.");
            Assert.That(round.Bodies().Select(body => (body.Body, body.Coordinate)),
                Is.EqualTo(new[] { (1, Origin), (2, BodyTwo), (3, H(-2, -1)) }));
        }

        [Test]
        public void TheRepeatedSpawn_IsBoundedByTheSupply()
        {
            // Three stand; the first spawn takes the last body, so the repeat's spawn finds none.
            var round = TwinStrikeRound(extraBodies: new[] { FarCorner });
            AttackTheEnemyBetweenThem(round, spawnAt: H(0, 1));
            Assert.That(round.Bodies(), Has.Length.EqualTo(4));
            Assert.That(round.PendingNode, Is.EqualTo(TwinStrikeNode));
            round.PickSpaces(BodyTwo);
            round.Target(Round.EnemyMinion(1));

            Assert.That(round.ActionEnded, Is.True,
                "four bodies stand: the repeat's \"up to 3\" spawns nothing (RZ-Q1).");
            Assert.That(round.Bodies(), Has.Length.EqualTo(4));
        }

        [Test]
        public void ABodyWithNoOtherUnitBesideIt_IsNotOfferedTheRepeat()
        {
            var round = TwinStrikeRound(extraBodies: new[] { FarCorner });
            AttackTheEnemyBetweenThem(round);

            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { BodyTwo }),
                "body 3 has nobody beside it: the repeat is judged from the body that would perform it.");
        }

        [Test]
        public void WhenTheOnlyUnitBesideTheOtherBodyIsTheFirstTarget_NoRepeatIsOffered()
        {
            var round = TwinStrikeRound(minionBesideBodyTwo: false);
            AttackTheEnemyBetweenThem(round);

            Assert.That(round.ActionEnded, Is.True,
                "a different unit must be targeted, and body 2 has none beside it.");
        }

        [Test]
        public void WithOneBody_ThereIsNoDifferentYou_AndNothingIsAsked()
        {
            var round = new Round(ultimate: RazzleCardContent.TwinStrikeDefinition,
                enemyMinionsAt: new[] { H(-1, 0) });
            round.AttackTheMinionBesideHim();
            DeclineWhileSpawning(round);

            Assert.That(round.ActionEnded, Is.True, "no repeat with one body standing.");
            Assert.That(round.Bodies(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Declining_EndsTheAction_WithBodyOneStillActing()
        {
            var round = TwinStrikeRound();
            AttackTheEnemyBetweenThem(round);
            round.Decline();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Board.TryGetActingBody(RedRazzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(1));
        }

        [Test]
        public void TwinStrike_IsNotAskedAfterAColouredAttack_NorAfterAHold()
        {
            // Phantom Strike (Red I) is an Attack, but not a basic one (p.9).
            var red = new Round(face: RazzleCardContent.PhantomStrikeDefinition,
                ultimate: RazzleCardContent.TwinStrikeDefinition,
                bodiesAt: new[] { BodyTwo }, enemyMinionsAt: new[] { BesideBodyOne, BesideBodyTwo });
            red.PickActingBody(1);
            red.StartPrimaryAttack();
            red.Target(Round.EnemyMinion(1));
            if (red.PendingNode != null)
            {
                red.Decline();
            }

            Assert.That(red.ActionEnded, Is.True, "Twin Strike reads a basic attack action only.");

            // A Hold on Stunt Doubles is a basic action that is not an Attack.
            var hold = TwinStrikeRound();
            hold.PickActingBody(1);
            Assert.That(hold.Authority.TryPerformHold(hold.Source).Accepted, Is.True);
            Assert.That(hold.Turn.PendingEffectChoice, Is.Null, "no repeat after a Hold.");
        }

        // --- Twin Strike: offer and answer agree; restore and replay ---------------------------------

        [Test]
        public void EveryBodyTheRepeatOffers_StartsARepeatedAttackWithATargetBesideIt()
        {
            var round = TwinStrikeRound(extraBodies: new[] { H(3, -3) }, extraMinions: new[] { H(4, -3) });
            AttackTheEnemyBetweenThem(round);
            var offered = round.OfferedSpaces();
            Assert.That(offered, Is.EqualTo(new[] { BodyTwo, H(3, -3) }), "two bodies, each with a unit beside it.");

            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            foreach (var body in offered)
            {
                var copy = round.Restore(bytes);
                var asked = copy.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(copy.Round.TryAnswerEffectChoice(asked.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Equals(EffectValue.Space(body)))).Accepted, Is.True, $"{body}");
                var target = copy.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, $"the repeat from {body} asks its target");
                Assert.That(target.LegalTargets, Is.Not.Empty, $"a different unit stands beside {body}");
                Assert.That(copy.Board.TryGetHeroCoordinate(RedRazzle, out var from), Is.True);
                Assert.That(from, Is.EqualTo(body), "the body answered is the body acting.");
            }
        }

        [Test]
        public void ThePausedRepeat_RestoresTheSameBodies_AndReplaysToTheSameBytes()
        {
            // Body 3 in the far corner is not offered; the restored pause must not offer it either.
            var round = TwinStrikeRound(extraBodies: new[] { FarCorner });
            AttackTheEnemyBetweenThem(round);
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { BodyTwo }));
            round.AssertRestoreReplays(BodyTwo);
            Assert.That(round.LegalTargetMinions(), Is.EqualTo(new[] { Round.EnemyMinion(1) }));
        }

        [Test]
        public void TheRepeatedAttack_RestoredAtItsTarget_KeepsTheSecondBodyActing()
        {
            var round = TwinStrikeRound();
            AttackTheEnemyBetweenThem(round);
            round.PickSpaces(BodyTwo);
            var live = round.Turn.PendingAttackTargetChoice;
            Assert.That(live, Is.Not.Null);

            var restored = round.Restore(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board));
            Assert.That(restored.Board.ActingBodies, Is.EqualTo(round.Board.ActingBodies));
            var resumed = restored.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.LegalTargetMinionIds, Is.EqualTo(live.LegalTargetMinionIds));
            Assert.That(resumed.LegalTargetHeroIds, Is.EqualTo(live.LegalTargetHeroIds),
                "the first target is still excluded after a restore.");

            round.Target(Round.EnemyMinion(1));
            Assert.That(restored.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                resumed.ResolutionId, resumed.ChoiceId, Round.EnemyMinion(1))).Accepted, Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board)));
        }

        // --- Spectacle ------------------------------------------------------------------------------

        // Spectacle's board: minion A beside body 1, minion C beside body 2 at (-2, 0) and out of body
        // 1's Range 2. After the first swap minion A stands at the origin, two from body 2.
        private static readonly HexCoordinate MinionA = H(1, 0);
        private static readonly HexCoordinate MinionC = H(-3, 0);

        [Test]
        public void Spectacle_RepeatsFromTheOtherBody_MeasuringRangeFromIt_OnADifferentMinion()
        {
            var round = SpectacleRound(new[] { BodyTwo }, MinionA, MinionC);
            round.PickActingBody(1);
            round.StartSkill();
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Minion(MoveRound.RedMinion(1)) }),
                "Range 2 from body 1: minion C is three away.");
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.RepeatNode(RazzleCardContent.Spectacle)));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { BodyTwo }), "a different one of you.");
            round.Answer(EffectValue.Space(BodyTwo));

            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Minion(MoveRound.RedMinion(2)) }),
                "Range 2 from body 2 (RZ-Q12): minion A stands in it at the origin, but it was the first " +
                "swap's minion.");
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(2)));

            Assert.That(round.ActionEnded, Is.True, "the repeated instance does not repeat (p.10).");
            Assert.That(round.Bodies().Select(body => (body.Body, body.Coordinate)),
                Is.EqualTo(new[] { (1, MinionA), (2, MinionC) }));
            Assert.That(round.MinionAt(MoveRound.RedMinion(1)), Is.EqualTo(Origin));
            Assert.That(round.MinionAt(MoveRound.RedMinion(2)), Is.EqualTo(BodyTwo));
        }

        [Test]
        public void Spectacle_WithOneBody_OrOnlyTheFirstMinionInRangeOfTheOther_AsksNoRepeat()
        {
            var alone = SpectacleRound(Array.Empty<HexCoordinate>(), MinionA, MinionC);
            alone.StartSkill();
            alone.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            Assert.That(alone.ActionEnded, Is.True, "no different one of him.");

            var onlyTheFirst = SpectacleRound(new[] { BodyTwo }, MinionA);
            onlyTheFirst.PickActingBody(1);
            onlyTheFirst.StartSkill();
            onlyTheFirst.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            Assert.That(onlyTheFirst.ActionEnded, Is.True,
                "body 2's Range 2 holds only the minion already swapped: no different minion, no offer.");
        }

        [Test]
        public void Spectacle_Declined_EndsAfterOneSwap()
        {
            var round = SpectacleRound(new[] { BodyTwo }, MinionA, MinionC);
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            round.Decline();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { MinionA, BodyTwo }));
        }

        [Test]
        public void Spectacle_PausedAtItsRepeat_RestoresTheSameBodies_AndReplays()
        {
            // Body 3 in the far corner has no minion in range and is not offered, live or restored.
            var round = SpectacleRound(new[] { BodyTwo, FarCorner }, MinionA, MinionC);
            round.PickActingBody(1);
            round.StartSkill();
            round.Answer(EffectValue.Minion(MoveRound.RedMinion(1)));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { BodyTwo }));
            round.AssertRestoreReplays(EffectValue.Space(BodyTwo));
            Assert.That(round.Candidates(), Is.EqualTo(new[] { EffectValue.Minion(MoveRound.RedMinion(2)) }));
            round.AssertRestoreReplays(EffectValue.Minion(MoveRound.RedMinion(2)));
            Assert.That(round.Bodies().Select(body => body.Coordinate), Is.EqualTo(new[] { MinionA, MinionC, FarCorner }));
        }

        // --- RZ-Q13: a radius "of you" on a many-bodied hero is read from every body ---------------------

        [Test]
        public void AnOverlayDrawnAroundSeveralBodies_IsTheUnionOfTheirDisks_ForBothArms()
        {
            var declaration = GuardsDigital.Core.Rules.Modifications.ConditionalObstacleOverlay.Declare(
                GuardsDigital.Core.Rules.Modifications.SourceTeamRelation.OpposingSource,
                GuardsDigital.Core.Rules.Modifications.ObstacleOverlayArm.SpacesInsideWhileActorOutside,
                GuardsDigital.Core.Rules.Modifications.ObstacleOverlayArm.SpacesOutsideWhileActorInside);
            var bodies = new[] { Origin, H(4, 0) };

            var outside = GuardsDigital.Core.Rules.Modifications.AppliedObstacleOverlay.Bind(
                declaration, bodies, 1, "rz-q13", RedRazzle, TeamSide.Red, TeamSide.Blue, H(-4, 0));
            Assert.That(outside.CountsAsObstacle(H(1, 0)), Is.True, "inside body 1's disk");
            Assert.That(outside.CountsAsObstacle(H(5, 0)), Is.True, "inside body 2's disk: in radius of him");
            Assert.That(outside.CountsAsObstacle(H(2, 0)), Is.False, "in neither disk");

            var inside = GuardsDigital.Core.Rules.Modifications.AppliedObstacleOverlay.Bind(
                declaration, bodies, 1, "rz-q13", RedRazzle, TeamSide.Red, TeamSide.Blue, H(5, 0));
            Assert.That(inside.CountsAsObstacle(H(1, 0)), Is.False,
                "an actor inside body 2's disk is inside his radius, so body 1's disk is not beyond it.");
            Assert.That(inside.CountsAsObstacle(H(2, 0)), Is.True, "outside every disk");
        }

        // --- The harness ----------------------------------------------------------------------------

        private static Round TwinStrikeRound(
            HexCoordinate[] extraBodies = null,
            bool minionBesideBodyTwo = true,
            HexCoordinate[] extraMinions = null) =>
            new Round(
                ultimate: RazzleCardContent.TwinStrikeDefinition,
                bodiesAt: new[] { BodyTwo }.Concat(extraBodies ?? Array.Empty<HexCoordinate>()).ToArray(),
                enemyAt: BetweenThem,
                enemyMinionsAt: (minionBesideBodyTwo ? new[] { BesideBodyTwo, BesideBodyOne } : new[] { BesideBodyOne })
                    .Concat(extraMinions ?? Array.Empty<HexCoordinate>()).ToArray());

        /// <summary>Stunt Doubles from body 1 on the enemy hero, who defends; the spawn declined or taken once.</summary>
        private static void AttackTheEnemyBetweenThem(Round round, HexCoordinate? spawnAt = null)
        {
            round.PickActingBody(1);
            round.StartPrimaryAttack();
            round.Target(BlueEnemy);
            round.DefendWithTheWall();
            if (spawnAt.HasValue)
            {
                round.PickSpaces(spawnAt.Value);
            }

            DeclineWhileSpawning(round);
        }

        private static void DeclineWhileSpawning(Round round)
        {
            while (round.PendingNode != null && round.PendingNode.StartsWith("stunt-doubles-spawn", StringComparison.Ordinal))
            {
                round.Decline();
            }
        }

        private static MoveRound SpectacleRound(HexCoordinate[] bodies, params HexCoordinate[] minions) =>
            new MoveRound(RazzleCardContent.SpectacleDefinition, bodiesAt: bodies, redMinionsAt: minions);
    }
}

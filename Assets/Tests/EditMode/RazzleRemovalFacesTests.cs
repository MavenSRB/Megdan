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
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using Round = GuardsDigital.Tests.EditMode.RazzleStuntDoublesTests.Round;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21e. Razzle's removal faces on 9.21d's staged round (<see cref="RazzleStuntDoublesTests"/>):
    /// Phantom Strike "If there is more than one of you in play, you may remove one of you", Hit and
    /// Gone "one or more ... except the last one of you", Into Thin Air "one or more ... including all
    /// of you", and Crowd Control's Skill "remove all other you in play". One generic removal of the
    /// actor's own bodies composes all four; it is p.19's Remove, never a Defeat (RZ-Q6), and the
    /// bodies are back in the supply at once. Nothing is registered: the round composes Razzle's
    /// handlers into its own catalog.
    ///
    /// <para><b>The board.</b> 9.21d's radius-5 disk: body 1 at the origin with an enemy minion beside
    /// it at (-1, 0); further bodies where a case stands them; the enemy hero far away.</para>
    /// </summary>
    public sealed class RazzleRemovalFacesTests
    {
        private static readonly HeroId Razzle = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate Origin = H(0, 0);
        private static readonly HexCoordinate Two = H(3, -3);
        private static readonly HexCoordinate Three = H(-3, 3);
        private static readonly HexCoordinate Four = H(3, 0);

        // --- The printed faces ----------------------------------------------------------------------

        [Test]
        public void PhantomStrike_IsTheVerifiedRedTierOne_WithTheIfGateOverOneOptionalRemoval()
        {
            var face = RazzleCardContent.PhantomStrikeDefinition;
            AssertRedAttack(face, 9, 3, 4, CardTier.I, PrintedValueChannel.None,
                "Target a unit adjacent to you. After the attack: If there is more than one of you in " +
                "play, you may remove one of you.");
            Assert.That(face.Progression.NextTierCandidateIds.Select(id => id.Value),
                Is.EquivalentTo(new[] { "razzle-hit-and-gone", "razzle-rummage" }));

            var text = Attack(face).EffectRoot;
            var gate = text.Nodes.Single(node => node.Id.Equals(text.RootNodeId));
            Assert.That(gate.Kind, Is.EqualTo(EffectNodeKind.Conditional), "the printed \"If\"");
            Assert.That(gate.Capability, Is.EqualTo(RazzleEffectContent.SeveralBodiesV1));
            Assert.That(gate.ChildNodeIds, Has.Count.EqualTo(1),
                "no else-child: a false gate is RULING 53's legal do-nothing.");
            AssertRemoval(text, gate.ChildNodeIds[0], EffectFailurePolicy.May, ActorBodyRemoval.OneBody);
        }

        [Test]
        public void HitAndGone_IsTheVerifiedRedTierTwo_WithOneOptionalRemovalNeverOfTheLast()
        {
            var face = RazzleCardContent.HitAndGoneDefinition;
            AssertRedAttack(face, 9, 4, 4, CardTier.II, PrintedValueChannel.Defense,
                "Target a unit adjacent to you. After the attack: You may remove one or more of you in " +
                "play, except the last one of you.");
            Assert.That(face.Progression.NextTierCandidateIds.Select(id => id.Value),
                Is.EquivalentTo(new[] { "razzle-into-thin-air", "razzle-ransack" }));
            var text = Attack(face).EffectRoot;
            AssertRemoval(text, new EffectNodeId(RazzleEffectContent.RemovalNode(RazzleCardContent.HitAndGone)),
                EffectFailurePolicy.May, ActorBodyRemoval.OneOrMoreButNotTheLast);
        }

        [Test]
        public void IntoThinAir_IsTheVerifiedRedTierThree_WithOneOptionalRemovalOfAnyNumber()
        {
            var face = RazzleCardContent.IntoThinAirDefinition;
            AssertRedAttack(face, 10, 4, 5, CardTier.III, PrintedValueChannel.Range,
                "Target a unit adjacent to you. After the attack: You may remove one or more of you in " +
                "play, including all of you. (If all are removed, you are not defeated. Respawn as normal ).");
            Assert.That(face.Progression.NextTierCandidateIds, Is.Empty);
            var text = Attack(face).EffectRoot;
            AssertRemoval(text, new EffectNodeId(RazzleEffectContent.RemovalNode(RazzleCardContent.IntoThinAir)),
                EffectFailurePolicy.May, ActorBodyRemoval.OneOrMoreIncludingAll);
            Assert.That(text.Nodes.Count(node => node.Kind == EffectNodeKind.Capability), Is.EqualTo(1),
                "the reminder composes to no node: removal is never a Defeat, and p.7 keys on absence.");
        }

        [Test]
        public void CrowdControl_IsTheVerifiedSilverDefenseSkill_WhoseSkillRemovesEveryOtherBody()
        {
            var face = RazzleCardContent.CrowdControlDefinition;
            Assert.That(face.Initiative.Value, Is.EqualTo(13));
            Assert.That(face.ContentRevision, Is.EqualTo(RazzleCardContent.ContentRevision));
            Assert.That((face.Progression.Color, face.Progression.Classification),
                Is.EqualTo((CardColor.Silver, CardClassification.Basic)));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(
                "When used as a defense action, +2 [Defense] Defense for each other you in radius. When " +
                "used as a skill action, remove all other you in play."));

            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(skill.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EqualTo(new[] { (PrintedValueChannel.Radius, 3) }));
            AssertRemoval(skill.EffectRoot,
                new EffectNodeId(RazzleEffectContent.RemovalNode(RazzleCardContent.CrowdControl)),
                EffectFailurePolicy.Mandatory, ActorBodyRemoval.EveryOtherBody);

            var defense = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense);
            Assert.That(defense.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary),
                "p.14's Defense/Skill card: the Defense is a primary, not a ribbon.");
            Assert.That(defense.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EquivalentTo(new[] { (PrintedValueChannel.Defense, 1), (PrintedValueChannel.Radius, 3) }));
            Assert.That(defense.EffectRoot, Is.Null,
                "\"When used as a skill action\": the removal never applies to the Defense (its own " +
                "text is 9.21g's).");
            Assert.That(face.ActionDefinitions, Has.Count.EqualTo(2), "no ribbon: no secondary action.");
        }

        // --- Phantom Strike ------------------------------------------------------------------------

        [Test]
        public void PhantomStrike_WithOneBody_OpensNoRemoval_AndTheAttackStillResolves()
        {
            var round = new Round(face: RazzleCardContent.PhantomStrikeDefinition);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True,
                "the Attack is judged on its target; a false \"If\" is a legal do-nothing (RULING 53).");
            round.AttackTheMinionBesideHim();

            Assert.That(round.ActionEnded, Is.True, "\"If there is more than one of you\" is false.");
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, Origin) }));
        }

        [Test]
        public void PhantomStrike_WithTwoBodies_OffersEitherBody_AndRemovesExactlyOne()
        {
            var round = TwoBodyRound(RazzleCardContent.PhantomStrikeDefinition);
            round.AttackTheMinionBesideHim();

            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.RemovalNode(RazzleCardContent.PhantomStrike)));
            var choice = round.Turn.PendingEffectChoice;
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { Origin, Two }),
                "\"one of you\": every body, the acting one included (RZ-Q6).");
            Assert.That((choice.MinimumSelections, choice.MaximumSelections), Is.EqualTo((1, 1)));
            Assert.That(choice.DeclineInvocation, Is.Not.Null, "\"you may\"");

            round.PickSpaces(Two);
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 1, Origin) }));
            Assert.That(round.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 2, 3, 4 }),
                "the removed body is back in the supply (p.19 Remove; the supply is read from the board).");
            Assert.That(round.Board.IsEmptyPlayableSpace(Two), Is.True);
        }

        [Test]
        public void PhantomStrike_Declined_KeepsBothBodies()
        {
            var round = TwoBodyRound(RazzleCardContent.PhantomStrikeDefinition);
            round.AttackTheMinionBesideHim();
            round.Decline();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 1, 2 }));
        }

        // --- The acting body removed mid-action (RZ-Q6) ---------------------------------------------

        [Test]
        public void PhantomStrike_RemovingTheActingBody_SettlesTheLowestBodyLeft_AndTheActionEnds()
        {
            // Bodies 1, 2 and 3 stand; body 2 acts, beside a minion of its own.
            var round = new Round(face: RazzleCardContent.PhantomStrikeDefinition,
                bodiesAt: new[] { Two, Three }, enemyMinionsAt: new[] { H(-1, 0), H(4, -3) });
            round.PickActingBody(2);
            round.StartPrimaryAttack();
            round.Target(Round.EnemyMinion(2));
            round.PickSpaces(Two);

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(1), "the lowest body left answers for the rest of the action.");
            Assert.That(round.Board.TryGetHeroCoordinate(Razzle, out var space), Is.True);
            Assert.That(space, Is.EqualTo(Origin));
            Assert.That(round.Turn.PendingActingBodyChoice, Is.Null,
                "the action is under way: \"which of you acts\" is never asked again inside it.");
            round.ReleaseTurn();
        }

        [Test]
        public void HitAndGone_RemovingTheActingBodyAndBodyOne_SettlesTheLowestBodyLeft()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition,
                bodiesAt: new[] { Two, Three, Four }, enemyMinionsAt: new[] { H(-1, 0), H(4, -3) });
            round.PickActingBody(2);
            round.StartPrimaryAttack();
            round.Target(Round.EnemyMinion(2));
            round.Select(Two);
            round.Select(Origin);
            round.Confirm();

            Assert.That(round.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(3), "neither body 1 nor the acting body stands: body 3 answers.");
            Assert.That(round.Board.TryGetHeroCoordinate(Razzle, out var space), Is.True,
                "9.21a's fallback would refuse here (several bodies, no body 1); the settled body answers.");
            Assert.That(space, Is.EqualTo(Three));
        }

        [Test]
        public void RemovingABodyThatIsNotActing_LeavesTheActingBodyAlone()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition, bodiesAt: new[] { Two, Three });
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();
            round.Select(Three);
            round.Confirm();

            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(1));
        }

        // --- Hit and Gone --------------------------------------------------------------------------

        [Test]
        public void HitAndGone_WithThreeBodies_OffersEveryBody_UpToAllButOne()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition, bodiesAt: new[] { Two, Three });
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();

            var choice = round.Turn.PendingEffectChoice;
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { Origin, Two, Three }));
            Assert.That((choice.MinimumSelections, choice.MaximumSelections), Is.EqualTo((1, 2)),
                "\"one or more ... except the last one of you\".");
            round.Select(Two);
            round.Select(Origin);
            Assert.That(round.Turn.PendingEffectChoice.SelectInvocations, Is.Empty,
                "a third body would be the last: it cannot be chosen.");
            round.Confirm();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 3, Three) }));
            Assert.That(round.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 1, 2, 4 }));
        }

        [Test]
        public void HitAndGone_WithOneBody_OpensNoRemoval()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True);
            round.AttackTheMinionBesideHim();

            Assert.That(round.ActionEnded, Is.True, "the last one of you is never removed.");
            Assert.That(round.Bodies().Select(body => body.Body), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void HitAndGone_EveryOfferedBody_IsAnsweredByRemovingThatBody()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition, bodiesAt: new[] { Two, Three });
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            foreach (var space in round.OfferedSpaces())
            {
                var copy = round.Restore(bytes);
                Select(copy.Round, space);
                Confirm(copy.Round);
                Assert.That(copy.Board.GetHeroBodies(Razzle).Select(body => body.Coordinate),
                    Has.No.Member(space), $"{space}'s body left the board");
                Assert.That(copy.Board.GetHeroBodies(Razzle), Has.Count.EqualTo(2));
            }
        }

        // --- Into Thin Air (RZ-Q6: off the board, never defeated) ----------------------------------

        [Test]
        public void IntoThinAir_WithTwoBodies_MayRemoveBoth_AndHeIsOffTheBoardButNotDefeated()
        {
            var round = TwoBodyRound(RazzleCardContent.IntoThinAirDefinition);
            round.AttackTheMinionBesideHim();

            var choice = round.Turn.PendingEffectChoice;
            Assert.That((choice.MinimumSelections, choice.MaximumSelections), Is.EqualTo((1, 2)),
                "\"one or more of you in play, including all of you\".");
            var coins = Coins(round);
            var lives = round.Authority.TeamLives.Counters.ToArray();
            var defeats = round.Authority.HeroDefeatHistory.Entries.Count;

            round.Select(Origin);
            round.Select(Two);
            round.Confirm();

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Bodies(), Is.Empty);
            Assert.That(round.Board.IsHeroOnBoard(Razzle), Is.False);
            Assert.That(round.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(round.Board.TryGetActingBody(Razzle, out _), Is.False, "no body stands to act.");
            Assert.That(Coins(round), Is.EqualTo(coins), "p.19 Remove: \"Do not collect any coins\".");
            Assert.That(round.Authority.TeamLives.Counters, Is.EqualTo(lives), "no Life is spent.");
            Assert.That(round.Authority.HeroDefeatHistory.Entries, Has.Count.EqualTo(defeats),
                "\"If all are removed, you are not defeated\".");
            Assert.That(round.Authority.Scoreboard.Entries.Sum(entry => entry.Deaths + entry.Kills),
                Is.EqualTo(0), "no Defeat is scored.");
        }

        [Test]
        public void IntoThinAir_WithOneBody_MayRemoveHisOnlyBody()
        {
            var round = new Round(face: RazzleCardContent.IntoThinAirDefinition);
            round.AttackTheMinionBesideHim();

            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { Origin }));
            round.PickSpaces(Origin);
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.Board.IsHeroOnBoard(Razzle), Is.False);
        }

        [Test]
        public void AfterIntoThinAirRemovesEveryBody_HisNextTurnOffersTheRespawn()
        {
            var round = new Round(face: RazzleCardContent.IntoThinAirDefinition, secondCardInHand: true);
            round.AttackTheMinionBesideHim();
            round.PickSpaces(Origin);
            round.ReleaseTurn();

            // The enemy's turn, then the round's next turn, in which he commits his second card.
            Assert.That(round.Turn.SelectedActor.ActorId, Is.EqualTo(Enemy));
            Assert.That(round.Authority.TryPerformHold(round.Turn.SelectedActor.CardInstanceId).Accepted, Is.True);
            Assert.That(round.Authority.TryReleaseTurn().Accepted, Is.True);
            Assert.That(round.Authority.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            Assert.That(round.Authority.TryCommitCard(Razzle, Round.SecondCard).Accepted, Is.True);
            Assert.That(round.Authority.TryCommitCard(Enemy, new CardInstanceId("razzle-enemy-wall")).Accepted,
                Is.True);
            if (round.Turn.SelectedActor.ActorId.Equals(Enemy))
            {
                Assert.That(round.Authority.TryPerformHold(round.Turn.SelectedActor.CardInstanceId).Accepted,
                    Is.True);
                Assert.That(round.Authority.TryReleaseTurn().Accepted, Is.True);
            }

            Assert.That(round.Turn.SelectedActor.ActorId, Is.EqualTo(Razzle));
            Assert.That(round.Turn.PendingHeroRespawnChoice, Is.Not.Null,
                "off the board, p.7 offers his respawn at his next action (RZ-Q6).");
            Assert.That(round.Turn.PendingActingBodyChoice, Is.Null, "no body stands to pick.");
            Assert.That(round.Authority.HeroDefeatHistory.Entries, Is.Empty, "he was never defeated.");
        }

        // --- Crowd Control's Skill (RZ-Q7) ---------------------------------------------------------

        [Test]
        public void CrowdControl_WithOneBody_IsGreyed()
        {
            var round = new Round(face: RazzleCardContent.CrowdControlDefinition);
            Assert.That(round.IsOffered(RazzleCardContent.PrimarySkill), Is.False,
                "\"remove all other you in play\" is mandatory and has nothing to act on (RZ-Q7).");
            Assert.That(round.Authority.TryPerformHold(round.Source).Accepted, Is.True, "Hold stays.");
        }

        [Test]
        public void CrowdControl_WithThreeBodies_RemovesEveryBodyButTheActingOne_AskingNothing()
        {
            var round = new Round(face: RazzleCardContent.CrowdControlDefinition, bodiesAt: new[] { Two, Three });
            round.PickActingBody(2);
            Assert.That(round.IsOffered(RazzleCardContent.PrimarySkill), Is.True);
            var coins = Coins(round);
            round.StartSkill();

            Assert.That(round.ActionEnded, Is.True, "the bodies are named by the text; nothing is chosen.");
            Assert.That(round.Bodies(), Is.EqualTo(new[] { new HeroOccupancy(Razzle, 2, Two) }));
            Assert.That(round.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 1, 3, 4 }));
            Assert.That(round.Board.TryGetActingBody(Razzle, out var acting), Is.True);
            Assert.That(acting, Is.EqualTo(2));
            Assert.That(Coins(round), Is.EqualTo(coins));
            Assert.That(round.Authority.HeroDefeatHistory.Entries, Is.Empty);
        }

        // --- Restore and replay mid-removal ---------------------------------------------------------

        [Test]
        public void HitAndGone_PausedWithOneBodyChosen_RestoresAndReplaysToTheSameBytes()
        {
            var round = new Round(face: RazzleCardContent.HitAndGoneDefinition, bodiesAt: new[] { Two, Three });
            round.PickActingBody(1);
            round.AttackTheMinionBesideHim();
            round.Select(Two);

            var pending = round.Turn.PendingEffectChoice;
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            var restored = round.Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.Candidates.Select(value => value.ToString()),
                Is.EqualTo(pending.Candidates.Select(value => value.ToString())));
            Assert.That(resumed.SelectedCandidates.Select(value => value.ToString()),
                Is.EqualTo(pending.SelectedCandidates.Select(value => value.ToString())));

            round.Select(Origin);
            round.Confirm();
            Select(restored.Round, Origin);
            Confirm(restored.Round);
            var live = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(live));
            Assert.That(restored.Board.ActingBodies, Is.EqualTo(round.Board.ActingBodies),
                "the acting body settled after its removal survives the restore.");

            var prefix = round.Restore(bytes);
            Select(prefix.Round, Origin);
            Confirm(prefix.Round);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board), Is.EqualTo(live));
        }

        [Test]
        public void PhantomStrike_PausedWithBodyTwoActing_ReplaysTheActingBodysRemoval()
        {
            var round = new Round(face: RazzleCardContent.PhantomStrikeDefinition,
                bodiesAt: new[] { Two }, enemyMinionsAt: new[] { H(-1, 0), H(4, -3) });
            round.PickActingBody(2);
            round.StartPrimaryAttack();
            round.Target(Round.EnemyMinion(2));
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);

            round.PickSpaces(Two);
            var restored = round.Restore(bytes);
            Assert.That(restored.Board.ActingBodies, Is.EqualTo(new[] { new HeroActingBody(Razzle, 2) }));
            Select(restored.Round, Two);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board)));
            Assert.That(restored.Board.ActingBodies, Is.EqualTo(new[] { new HeroActingBody(Razzle, 1) }));
        }

        [Test]
        public void OffTheBoardAfterIntoThinAir_RestoresToTheSameBytes()
        {
            var round = new Round(face: RazzleCardContent.IntoThinAirDefinition);
            round.AttackTheMinionBesideHim();
            round.PickSpaces(Origin);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            var restored = round.Restore(bytes);

            Assert.That(restored.Board.IsHeroOnBoard(Razzle), Is.False);
            Assert.That(restored.Board.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
        }

        // --- The board commit: a Removal, never a Defeat ---------------------------------------------

        [Test]
        public void TheBodyRemovalCommit_RefusesWhatIsNotABodyOfAManyBodiedHero()
        {
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("razzle-removal"), 1,
                    new[] { Origin, Two, Three, Four }),
                new[]
                {
                    new HeroOccupancy(Razzle, 1, Origin), new HeroOccupancy(Razzle, 2, Two),
                    new HeroOccupancy(Enemy, Three)
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<GuardsDigital.Core.Setup.QuickGameMinionSetupState>(),
                new[] { new HeroBodySupply(Razzle, 4) });
            IUnitSpawnAuthority units = board;

            Assert.That(units.CommitValidatedHeroBodyRemoval(Enemy, new[] { 1 }, out _), Is.False,
                "a hero declaring one body leaves the board only through p.13's Defeat.");
            Assert.That(units.CommitValidatedHeroBodyRemoval(Razzle, Array.Empty<int>(), out _), Is.False);
            Assert.That(units.CommitValidatedHeroBodyRemoval(Razzle, new[] { 2, 2 }, out _), Is.False);
            Assert.That(units.CommitValidatedHeroBodyRemoval(Razzle, new[] { 3 }, out _), Is.False,
                "body 3 is in the supply, not on the board.");
            Assert.That(board.GetHeroBodies(Razzle), Has.Count.EqualTo(2), "every refusal changed nothing.");
            Assert.That(((IBoardObjectExchangeAuthority)board).CommitValidatedRemoval(
                    BoardObjectReference.ForHeroBody(Razzle, 2)).RejectionReason,
                Is.EqualTo(BoardObjectRemovalRejectionReason.HeroRequiresDefeat),
                "the generic Remove port still refuses every hero: the Defeat authority is untouched.");

            Assert.That(units.CommitValidatedHeroBodyRemoval(Razzle, new[] { 2 }, out var removed), Is.True);
            Assert.That(removed, Is.EqualTo(new[] { new HeroOccupancy(Razzle, 2, Two) }));
            Assert.That(board.IsEmptyPlayableSpace(Two), Is.True);
            Assert.That(units.FindHeroBodySupply(Razzle), Is.EqualTo(new[] { 2, 3, 4 }));
        }

        // --- Helpers --------------------------------------------------------------------------------

        private static Round TwoBodyRound(CardDefinition face)
        {
            var round = new Round(face: face, bodiesAt: new[] { Two });
            round.PickActingBody(1);
            return round;
        }

        private static ActionDefinition Attack(CardDefinition face) =>
            face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

        private static void AssertRedAttack(
            CardDefinition face, int initiative, int defense, int attack, CardTier tier,
            PrintedValueChannel item, string text)
        {
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(face.ContentRevision, Is.EqualTo(RazzleCardContent.ContentRevision));
            Assert.That((face.Progression.Color, face.Progression.Tier, face.Progression.ItemIcon),
                Is.EqualTo((CardColor.Red, tier, item)));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(text));
            var primary = Attack(face);
            Assert.That(primary.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(primary.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged), "no Range icon: adjacent.");
            Assert.That(primary.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EqualTo(new[] { (PrintedValueChannel.Attack, attack) }));
            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(Secondary(face, ActionKind.Movement), Is.EqualTo(4));
            Assert.That(Secondary(face, ActionKind.Defense), Is.EqualTo(defense));
        }

        private static void AssertRemoval(
            EffectDefinition text, EffectNodeId id, EffectFailurePolicy policy, ActorBodyRemoval removal)
        {
            var node = text.Nodes.Single(value => value.Id.Equals(id));
            Assert.That(node.Kind, Is.EqualTo(EffectNodeKind.Capability));
            Assert.That(node.Capability, Is.EqualTo(RazzleEffectContent.RemoveBodiesV1),
                "one generic removal of the actor's bodies, never a per-card capability.");
            Assert.That(node.FailurePolicy, Is.EqualTo(policy));
            Assert.That(node.Inputs.Single().Literal, Is.EqualTo(EffectValue.Integer((int)removal)));
        }

        private static int Secondary(CardDefinition face, ActionKind kind) =>
            face.ActionDefinitions.Single(action => action.Kind == kind &&
                action.Origin != ActionDefinitionOrigin.PrintedPrimary).BaseValues.Single().Amount;

        private static KeyValuePair<HeroId, int>[] Coins(Round round) =>
            round.Authority.CoinBalances.ToArray();

        private static void Select(RoundResolutionAuthority target, HexCoordinate space)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(target.TryAnswerEffectChoice(asked.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space)))).Accepted, Is.True);
        }

        private static void Confirm(RoundResolutionAuthority target)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(asked?.ConfirmInvocation, Is.Not.Null);
            Assert.That(target.TryAnswerEffectChoice(asked.ConfirmInvocation).Accepted, Is.True);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.SabinaBoard;
using static GuardsDigital.Tests.EditMode.StagedHeroBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.22. Big Sodding Gun, unlocked and standing on a board.
    ///
    /// <para>The card prints two sentences and is this build's first Ultimate to do so: "Your basic
    /// attack has +2 [Range] Range and +2 [Attack] Attack. If you push an enemy hero, that hero
    /// discards a card, or is defeated." They are two different kinds of rule and are proved
    /// separately below - the first a standing modification that no action starts and none finishes,
    /// the second a declared after-push trigger that runs an effect.</para>
    ///
    /// <para><b>What is asserted here and what is not.</b> That the card declares two modifications
    /// and one trigger, and that they carry the right channels and the right subject, is
    /// <see cref="SabinaVerifiedContentTests"/>'s and is checked there against the transcription; the
    /// synthetic shape of the same rule is <see cref="RosterPersistentSourceTests"/>'s. What this file
    /// adds is the half a declaration cannot show - which cards the modification reaches once a match
    /// is running, and what a real applied Push does next.</para>
    ///
    /// <para><b>On "a repeated action".</b> No face Sabina prints repeats its Attack, so the reachable
    /// form of that question is whether a second applied Push fires the ability a second time: p.16
    /// makes an unlocked Ultimate permanent, and a trigger that spent itself would be a once-per-match
    /// ability the card does not print. That is
    /// <see cref="TheTrigger_FiresAgainOnASecondPush_BecauseThePrintedAbilityIsPermanent"/>.</para>
    /// </summary>
    public sealed class SabinaBigSoddingGunTests
    {
        // -----------------------------------------------------------------------------------------
        // The first sentence: two channels, one action class
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The unlocked card raises the live Attack Total her Gold basic resolves with, by the two the
        /// sentence prints.
        ///
        /// <para>The number is read where a player reads it - off the Defense preview the attacked
        /// hero is answering - rather than off the declaration, because what is new here is that the
        /// printed card reaches that total in a running match.</para>
        /// </summary>
        [Test]
        public void TheStandingRules_RaiseTheLiveAttackTotalOfHerBasicAttackByTwo()
        {
            var unlocked = BesideTheEnemyHero(unlocked: true);
            var locked = BesideTheEnemyHero(unlocked: false);

            unlocked.PlayAttackAt(
                SabinaCardContent.PointBlankShot, unlocked.Enemy, defendSuccessfully: false);
            locked.PlayAttackAt(
                SabinaCardContent.PointBlankShot, locked.Enemy, defendSuccessfully: false);

            Assert.That(locked.AttackTotal(), Is.EqualTo(2),
                "Point Blank Shot prints Attack 2, and p.6 deals the Ultimate to the deck where it " +
                "grants nothing - so this is the printed number with no rule on it.");
            Assert.That(unlocked.AttackTotal(), Is.EqualTo(locked.AttackTotal() + 2),
                "\"Your basic attack has +2 Attack\", reached from the Ultimate rather than from " +
                "the card being played.");
        }

        /// <summary>
        /// The unlocked card reaches her Gold basic's Range and reaches no other face's, measured as
        /// the target a player is offered.
        ///
        /// <para>The qualification is the whole point of the sentence. "Your basic attack" is p.15's
        /// supertype and her five Red Attacks are not basic, so a modification admitted by channel
        /// alone would hand Quickdraw two spaces of Range the card never granted. The enemy hero
        /// stands three spaces away, which is Point Blank Shot's printed 1 plus the printed 2 and one
        /// more than Quickdraw's printed 2.</para>
        /// </summary>
        [Test]
        public void TheStandingRules_ReachHerBasicAttacksRange_AndNoOtherFaceOfHers()
        {
            var unlocked = ThreeSpacesFromTheEnemyHero(unlocked: true);
            Assert.That(
                LegalHeroTargetsOf(unlocked, SabinaCardContent.PointBlankShot),
                Has.Member(unlocked.Enemy),
                "Range 1 plus the printed 2 is 3, and the hero is standing on the third space.");

            var locked = ThreeSpacesFromTheEnemyHero(unlocked: false);
            Assert.That(
                LegalHeroTargetsOf(locked, SabinaCardContent.PointBlankShot),
                Has.No.Member(locked.Enemy),
                "Without the unlock the same face prints Range 1 and cannot see three spaces, so " +
                "the reach above is the Ultimate's and not the board's.");

            var nonBasic = ThreeSpacesFromTheEnemyHero(unlocked: true);
            Assert.That(
                LegalHeroTargetsOf(nonBasic, SabinaCardContent.Quickdraw),
                Has.No.Member(nonBasic.Enemy),
                "Quickdraw is a Red Tier I Attack rather than a basic one, so \"your basic attack\" " +
                "does not name it: its printed Range 2 stays 2 with the Ultimate unlocked.");
        }

        // -----------------------------------------------------------------------------------------
        // The second sentence: what an applied Push does
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her basic Attack's own rider pushes the enemy hero, and the Ultimate's trigger runs on that
        /// push: the pushed hero is asked to discard, and the question is asked of them.
        /// </summary>
        [Test]
        public void APushOfTheEnemyHero_AsksThatHeroToDiscard_AfterTheyHaveMoved()
        {
            var fixture = BesideTheEnemyHero(unlocked: true);
            var before = fixture.HeroSpace(fixture.Enemy);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);

            var choice = fixture.Choice;
            Assert.That(choice, Is.Not.Null,
                "The push applied, so the declared trigger runs, and \"that hero discards a card\" " +
                "is a decision its own hero makes.");
            Assert.That(choice.ChooserId, Is.EqualTo(fixture.Enemy),
                "p.10 gives a discard to the hero who pays it. The sentence names the pushed hero " +
                "by the role the push already filled rather than by selecting one, which is what " +
                "makes this chooser the pushed hero and not the acting one.");
            Assert.That(fixture.HeroSpace(fixture.Enemy), Is.Not.EqualTo(before),
                "Ordering, stated as a board fact: the trigger fires after the displacement, so by " +
                "the time this question stands the hero has already been moved.");
            Assert.That(
                HexDistanceQuery.Calculate(fixture.HeroSpace(fixture.Enemy), HeroAt), Is.EqualTo(2),
                "One space directly away, which is the push the basic Attack's own rider prints.");

            var handBefore = fixture.Hand(fixture.Enemy).Count;
            fixture.SelectFirst();
            fixture.Confirm();

            Assert.That(fixture.Hand(fixture.Enemy).Count, Is.EqualTo(handBefore - 1),
                "The card the hero named leaves their hand.");
            Assert.That(fixture.DiscardPile(fixture.Enemy), Is.Not.Empty);
            Assert.That(fixture.IsOnBoard(fixture.Enemy), Is.True,
                "\"or is defeated\" is the escalation for a hero who could not pay, and this one " +
                "paid.");
        }

        /// <summary>
        /// The escalation: a hero with nothing left to discard is defeated instead.
        ///
        /// <para>The empty hand is produced the way p.7 produces one - the other cards discarded at
        /// setup and the last one committed for the turn, so the hero holds nothing at the moment the
        /// question is asked. It is the construction Wasp's Electroblast and Brogan's Counterattack
        /// print, and what this adds is that the role-bound subject reaches the Defeat arm as well as
        /// the discard one.</para>
        /// </summary>
        [Test]
        public void APushedHeroWithNoCardsLeft_IsDefeatedInsteadOfDiscarding()
        {
            var fixture = Create(
                heroPlacements: At("primitive-hero", FirstAdjacent),
                unlockUltimate: true,
                enemyHandSize: 2);
            var livesBefore = fixture.Lives(TeamSide.Red);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);

            Assert.That(fixture.Hand(fixture.Enemy), Is.Empty,
                "The hero was dealt two: p.7 spent one on the commitment and p.14's Defense spent " +
                "the other, so there is nothing left to pay with - which is the board this case " +
                "is about.");
            Assert.That(fixture.IsOnBoard(fixture.Enemy), Is.False,
                "p.13's Defeat takes the hero off the board, and the escalation reached it because " +
                "the discard could not be paid.");
            Assert.That(fixture.Lives(TeamSide.Red), Is.EqualTo(livesBefore - 1),
                "A defeated hero costs their team a Life counter, whatever defeated them.");
        }

        /// <summary>
        /// An Attack of hers on a minion reaches no trigger, and the reason is recorded rather than
        /// asserted around.
        ///
        /// <para>The sentence names an enemy hero, and the natural negative case would be a push that
        /// displaced a minion instead. <b>Her printed content cannot produce one.</b> Point Blank Shot
        /// is her only push and it pushes what the Attack targeted; a targeted enemy minion is defeated
        /// outright at p.14 step 6, so the present-tense rider finds nothing to push, and the one
        /// minion that would survive - p.18's immune Heavy - is not a legal Attack target while it is
        /// immune. What is left to assert on a minion target is what is true of it: no push, and
        /// therefore no trigger. That the declaration names <c>EnemyHero</c> and nothing else is
        /// <see cref="SabinaVerifiedContentTests"/>'s, where it is checked against the transcription.
        /// </para>
        /// </summary>
        [Test]
        public void AnAttackOnAMinion_ReachesNoTrigger_BecauseTheDefeatedTargetIsNotPushed()
        {
            var fixture = Create(
                minionPlacements: At("red-melee-01", FirstAdjacent), unlockUltimate: true);
            var target = fixture.MinionAt(FirstAdjacent);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, target);

            Assert.That(fixture.MinionIds(), Has.No.Member(target),
                "p.14 step 6 defeats a targeted enemy minion outright.");
            Assert.That(fixture.Choice, Is.Null,
                "No displacement of a hero, so the Ultimate's second sentence has nothing to run on.");
            Assert.That(fixture.DiscardPile(fixture.Enemy), Is.Empty,
                "and nobody was asked to pay for it.");
        }

        /// <summary>
        /// The same Attack that pushes an enemy hero leaves the immune Heavy minion beside her
        /// untouched, because p.18's immunity keeps it off the target list.
        ///
        /// <para>It is the board that would have carried the minion-push case above, and stating what
        /// it actually does is what keeps that absence honest: the piece is standing beside her, her
        /// basic Attack is in range of it, and it is still not a target.</para>
        /// </summary>
        [Test]
        public void AnImmuneHeavyMinionBesideHer_IsNotEvenATargetForHerBasicAttack()
        {
            var fixture = Create(
                minionPlacements: At("red-heavy-01", FirstAdjacent), unlockUltimate: true);
            var heavy = fixture.MinionAt(FirstAdjacent);

            fixture.StartPrimary(SabinaCardContent.PointBlankShot);

            Assert.That(
                fixture.Session.Projection.PendingAttackTargetChoice.LegalTargetMinionIds,
                Has.No.Member(heavy),
                "p.18 makes it immune while its own side keeps a non-Heavy minion in the Battle " +
                "Zone, and p.7's rule is not to offer a target the rules would then refuse.");
        }

        /// <summary>
        /// A push that moved nothing is not a push that applied, so the trigger is not reached.
        ///
        /// <para>Point Blank Shot's own reminder text - "A unit pushed into an obstacle will stop. This
        /// is a valid push." - is what makes this worth asserting rather than obvious: the push is
        /// valid and the Attack is complete, and the Ultimate still runs nothing, because
        /// <c>after-push-trigger@1</c> fires on the displacement rather than on the attempt. The board
        /// puts the enemy hero against the terrain space directly behind them.</para>
        /// </summary>
        [Test]
        public void APushThatMovedNobody_FiresNothing_EvenThoughThePushWasValid()
        {
            var fixture = Create(
                heroPlacements: At("primitive-hero", AdjacentAgainstTerrain),
                unlockUltimate: true);
            var before = fixture.HeroSpace(fixture.Enemy);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);

            Assert.That(fixture.HeroSpace(fixture.Enemy), Is.EqualTo(before),
                "There is nowhere to go: the space directly behind them is terrain, so p.19 stops " +
                "the pushed unit where it stands.");
            Assert.That(fixture.Choice, Is.Null,
                "No displacement, no trigger - and the Attack itself is unaffected, which is what " +
                "the printed reminder is about.");
        }

        /// <summary>
        /// A locked Ultimate grants nothing, so the same board and the same Attack push the hero and
        /// ask them nothing.
        /// </summary>
        [Test]
        public void WithoutTheUnlock_TheSamePushAsksNothing()
        {
            var fixture = BesideTheEnemyHero(unlocked: false);
            var before = fixture.HeroSpace(fixture.Enemy);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);

            Assert.That(fixture.HeroSpace(fixture.Enemy), Is.Not.EqualTo(before),
                "The push is the basic Attack's own printed rider and does not need the Ultimate.");
            Assert.That(fixture.Choice, Is.Null,
                "The escalation is the Ultimate's, and p.6 deals the Ultimate to the deck.");
        }

        /// <summary>
        /// p.16's unlock is permanent, so a second applied Push later in the same match fires the
        /// ability again.
        /// </summary>
        [Test]
        public void TheTrigger_FiresAgainOnASecondPush_BecauseThePrintedAbilityIsPermanent()
        {
            var fixture = BesideTheEnemyHero(unlocked: true);

            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);
            fixture.SelectFirst();
            fixture.Confirm();

            PlayOnUntilSheHoldsHerBasicAgain(
                fixture, AdjacentTo(fixture.HeroSpace(fixture.Enemy)));
            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);

            Assert.That(fixture.Choice, Is.Not.Null,
                "Nothing about the ability is spent by using it: p.16 leaves the card beside the " +
                "dashboard and the trigger is a standing declaration on it.");
            Assert.That(fixture.Choice.ChooserId, Is.EqualTo(fixture.Enemy),
                "and the second firing asks the hero this push named.");
            var handBeforeTheSecondPayment = fixture.Hand(fixture.Enemy).Count;
            fixture.SelectFirst();
            fixture.Confirm();
            Assert.That(fixture.Hand(fixture.Enemy).Count,
                Is.EqualTo(handBeforeTheSecondPayment - 1),
                "and it is paid a second time, out of the hand p.8 returned to them.");
        }

        /// <summary>
        /// The position the trigger produces is one a saved match comes back to, byte-identically.
        /// </summary>
        [Test]
        public void ThePausedTrigger_SurvivesASnapshotRoundTrip()
        {
            var fixture = BesideTheEnemyHero(unlocked: true);
            fixture.PlayAttackAt(SabinaCardContent.PointBlankShot, fixture.Enemy);
            Assert.That(fixture.Choice, Is.Not.Null);

            var payload = fixture.Session.CaptureSnapshot();
            var restored = BesideTheEnemyHero(unlocked: true);
            Assert.That(restored.Session.RestoreSnapshot(payload).Accepted, Is.True,
                "A hero standing mid-trigger with a discard question open is a position the " +
                "snapshot boundary carries, or the ability could not be used in a saved match.");

            var open = restored.Choice;
            Assert.That(open, Is.Not.Null);
            Assert.That(open.ChooserId, Is.EqualTo(restored.Enemy),
                "The rebuilt pause asks the same hero. The subject is bound from the rebuilt " +
                "context rather than persisted, so this is the assertion that says the rebinding " +
                "happened.");
            Assert.That(
                open.SelectInvocations, Has.Count.EqualTo(fixture.Choice.SelectInvocations.Count),
                "and offers the same cards to discard.");
            Assert.That(restored.Session.CaptureSnapshot(), Is.EqualTo(payload),
                "byte-identical, which is what makes it a restore rather than a re-derivation.");
        }

        // -----------------------------------------------------------------------------------------
        // Boards and steps
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A free playable space beside the one named, preferring the one Sabina can reach: after the
        /// push the hero is two spaces away, and closing to any of their neighbours restores the
        /// adjacency the present-tense rider asks about.
        /// </summary>
        private static HexCoordinate AdjacentTo(HexCoordinate space) =>
            space.GetNeighbors()
                .OrderBy(candidate => HexDistanceQuery.Calculate(candidate, HeroAt))
                .First();

        /// <summary>The enemy hero beside Sabina, with a free playable space directly behind them.</summary>
        private static StagedHeroBoard BesideTheEnemyHero(bool unlocked) =>
            Create(heroPlacements: At("primitive-hero", FirstAdjacent), unlockUltimate: unlocked);

        /// <summary>
        /// The enemy hero on a playable Battle-Zone space exactly three from Sabina, with one enemy
        /// minion standing beside her.
        ///
        /// <para>The minion is there so the Attack is offered at all: milestone 7.8a1 greys an action
        /// with nothing to act on, and a Range 1 face on a board whose only enemy is three spaces away
        /// would be unavailable - which would answer the question about availability instead of the
        /// one about who is a legal target.</para>
        /// </summary>
        private static StagedHeroBoard ThreeSpacesFromTheEnemyHero(bool unlocked) =>
            Create(
                minionPlacements: At("red-melee-01", FirstAdjacent),
                heroPlacements: At("primitive-hero", ThreeAway),
                unlockUltimate: unlocked);

        /// <summary>
        /// Who the named card could target if it were played now - started, and then left with the
        /// target question standing, which is the state a player reads before clicking.
        /// </summary>
        private static IReadOnlyList<HeroId> LegalHeroTargetsOf(
            StagedHeroBoard fixture, CardDefinitionId cardId)
        {
            fixture.StartPrimary(cardId);
            return fixture.Session.Projection.PendingAttackTargetChoice?.LegalTargetHeroIds
                ?? new HeroId[0];
        }

        /// <summary>
        /// Plays the rest of the round out and stops at the next commitment step, so the same Gold
        /// basic can be played again. p.8 returns a spent hand at the end of the round.
        /// </summary>
        private static void PlayOnUntilSheHoldsHerBasicAgain(
            StagedHeroBoard fixture, HexCoordinate closeTo)
        {
            var walked = false;
            for (var budget = 0; budget < 80; budget++)
            {
                var projection = fixture.Session.Projection;
                if (projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
                    fixture.Hand(fixture.Hero).Contains(SabinaCardContent.PointBlankShot))
                {
                    return;
                }

                if (projection.PendingEffectChoice != null)
                {
                    fixture.AnswerEveryStandingChoice(6);
                    continue;
                }

                if (projection.PendingAttackTargetChoice != null)
                {
                    var attack = projection.PendingAttackTargetChoice;
                    fixture.Accept(DirectorCommand.ChooseAttackTarget(
                        attack.ResolutionId, attack.ChoiceId, attack.LegalTargetMinionIds.First()));
                    continue;
                }

                if (projection.PendingDefenseChoice != null)
                {
                    fixture.Accept(DirectorCommand.DeclineDefense());
                    continue;
                }

                // The push moved the enemy hero out of Range 1's adjacency, and the rider asks about
                // adjacency in the present tense - so a second firing needs her to close again, which
                // costs a turn. That is the position a player would be in too. One turn is spent on
                // it; a card of hers printing no secondary Movement simply ends its turn instead.
                if (!walked &&
                    projection.IsTurnActive &&
                    projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(fixture.Hero))
                {
                    walked = true;
                    fixture.WalkTowards(closeTo);
                    continue;
                }

                if (projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    fixture.Accept(DirectorCommand.AdvanceEndOfRound());
                    continue;
                }

                if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    foreach (var hero in projection.Participants.Heroes.Select(value => value.Id))
                    {
                        var card = fixture.Session.Projection.Cards
                            .Where(value =>
                                value.OwnerId.Equals(hero) &&
                                value.State.Location == CardLocation.Hand)
                            .OrderBy(value => fixture.Session.Projection.Definitions
                                .Single(definition =>
                                    definition.Reference.Equals(value.DefinitionReference))
                                .Initiative.Value)
                            .FirstOrDefault();
                        // A hero the trigger has emptied has nothing to commit, and p.7 lets them
                        // pass rather than stopping the round - which is exactly the position this
                        // case has to play through to reach a second Push.
                        fixture.Accept(card == null
                            ? DirectorCommand.Pass(hero)
                            : DirectorCommand.CommitCard(hero, card.Id));
                    }

                    continue;
                }

                var movement = projection.PendingMovementChoice;
                if (movement != null && movement.CanStop)
                {
                    fixture.Accept(
                        DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                    continue;
                }

                fixture.Accept(
                    DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            }

            Assert.Fail("The match never came back to a commitment step with her Gold basic in hand.");
        }
    }
}

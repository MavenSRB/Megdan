using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
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
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.57b. Rowenna's four printed clauses that move somebody - the three Blue faces that
    /// swap her with a qualifying friendly unit, and the Silver basic that places her beside an enemy
    /// hero and then hands THAT hero an optional step of his own.
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Rowenna's is registered - milestone 7.9's gate holds her content out of
    /// <see cref="MatchContentCatalogs"/> until 8.57g deals her - so no composed match can put one of
    /// these cards in a hand and offer its Skill. <see cref="RowennaRewardTests"/> records the same
    /// boundary for the same reason. What is proved here is everything the text itself decides: who is
    /// offered, whose decision each question is, where the pieces end up, and which coins move. What is
    /// deliberately NOT proved here is the enclosing action pipeline; 8.57f and 8.57g own that, and the
    /// permanent player-path gate at 8.57h owns the pointer.</para>
    ///
    /// <para><b>The catalog these cases load is this build's plus ONE deferred declaration.</b>
    /// <c>synthetic-place-in-range-beside-a-named-hero@1</c> sits in
    /// <see cref="SyntheticPlacementCapabilities.DeferredHandlers"/> because no dealt card starts it
    /// yet, and a fixture that could not load it could not prove the face at all -
    /// <see cref="DodgerRespawnCapabilityTests"/> reads its own deferred pair the same way. That the
    /// build itself does NOT register it is asserted below rather than assumed.</para>
    ///
    /// <para><b>The board is four seats wide and says why.</b> Two Blue - Rowenna and one ally, which
    /// is the smallest board on which "a friendly unit" can be a HERO other than her - and two Red, so
    /// a case can stand one enemy inside her printed Range and one outside it without moving anybody
    /// between cases.</para>
    /// </summary>
    public sealed class RowennaSwapAndGauntletTests
    {
        private static readonly HeroId Rowenna = new HeroId("rowenna-vanguard");
        private static readonly HeroId Ally = new HeroId("rowenna-blue-ally");
        private static readonly HeroId Enemy = new HeroId("rowenna-red-enemy");
        private static readonly HeroId SecondEnemy = new HeroId("rowenna-red-second-enemy");

        private static readonly TeamId BlueTeam = new TeamId("rowenna-blue-team");
        private static readonly TeamId RedTeam = new TeamId("rowenna-red-team");

        private static readonly MapId Map = new MapId("rowenna-swap-map");

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);

        /// <summary>Three spaces from Rowenna: the far edge of Stand Guard's printed Range 3.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(3, 0);

        /// <summary>Five spaces from Rowenna: outside every Range her composed Skills print.</summary>
        private static readonly HexCoordinate FarEnemyAt = new HexCoordinate(5, 0);

        /// <summary>Two spaces from Rowenna and adjacent to the enemy hero: qualifies by adjacency.</summary>
        private static readonly HexCoordinate BesideTheEnemy = new HexCoordinate(2, 0);

        /// <summary>Two spaces from Rowenna and beside nobody: qualifies only if it holds a discard.</summary>
        private static readonly HexCoordinate BesideNobody = new HexCoordinate(0, 2);

        /// <summary>Where the ally stands by default - two away, adjacent to no enemy at all.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(0, -2);

        /// <summary>
        /// Four spaces from Rowenna and adjacent to the enemy hero: outside Stand Guard's Range 3 and
        /// inside the Range 4 both its upgrades print.
        /// </summary>
        private static readonly HexCoordinate BeyondThreeBesideTheEnemy = new HexCoordinate(4, 0);

        private static readonly MinionId BesideEnemyMinion = new MinionId("rowenna-minion-beside");
        private static readonly MinionId LoneMinion = new MinionId("rowenna-minion-alone");
        private static readonly MinionId DistantMinion = new MinionId("rowenna-minion-distant");

        /// <summary>Two spaces from Rowenna: where the gauntlet's enemy hero stands.</summary>
        private static readonly HexCoordinate GauntletEnemyAt = new HexCoordinate(2, 0);

        // -------------------------------------------------------------------------------------------
        // Stand Guard / Protect the Weak - "Swap with a friendly unit in range which is adjacent to an
        // enemy hero, or has a card in the discard."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The offer is exactly the friendly units the printed clause admits, and every exclusion in it
        /// is a different word of the sentence doing work: her own side keeps the enemy hero out, "with"
        /// keeps Rowenna herself out, the printed Range keeps the far minion out, and the qualifier
        /// keeps the friendly minion standing beside nobody out.
        /// </summary>
        [Test]
        public void StandGuard_OffersExactlyTheQualifyingFriendlyUnitsInsideItsPrintedRange()
        {
            var world = World.Swap(RowennaCardContent.StandGuard);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[]
                {
                    Ally.Value, BesideEnemyMinion.Value
                }),
                "The ally qualifies by holding a discard, the minion at (2,0) by standing beside the " +
                "enemy hero; the lone minion satisfies neither half, the minion at (4,0) is outside " +
                "Range 3, the enemy hero is the wrong side, and a swap with herself is nothing.");
        }

        /// <summary>
        /// The discard half of the clause is the candidate's OWN pile, so emptying it removes exactly
        /// that candidate and nothing else. The ally stands beside no enemy here, which is what makes
        /// the pile the only thing that could have qualified him.
        /// </summary>
        [Test]
        public void StandGuard_LeavesOutAFriendlyHeroWhoseDiscardIsEmptyAndWhoStandsBesideNobody()
        {
            var world = World.Swap(RowennaCardContent.StandGuard, allyDiscards: 0);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { BesideEnemyMinion.Value }),
                "An empty pile is not a qualifier, and p.10 empties every pile at the round boundary " +
                "- so this is a live reading rather than a property of the hero.");
        }

        /// <summary>
        /// A friendly hero standing beside an enemy qualifies on the other half of the "or" with an
        /// empty pile, which is what makes the clause a disjunction rather than two narrowings.
        /// </summary>
        [Test]
        public void StandGuard_AdmitsAFriendlyHeroBesideAnEnemy_EvenWithNothingInTheDiscard()
        {
            var world = World.Swap(
                RowennaCardContent.StandGuard, allyAt: new HexCoordinate(2, 1), allyDiscards: 0);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Does.Contain(Ally.Value),
                "(2,1) is adjacent to the enemy hero at (3,0) and three spaces from Rowenna, so the " +
                "first half of the printed 'or' admits him on its own.");
        }

        /// <summary>
        /// The reach is the acting card's own printed Range with p.16's items composed onto it, which
        /// is why one factory serves both faces: the minion four spaces away is outside Stand Guard's
        /// 3 and inside Protect the Weak's 4, and nothing else about the two bodies differs.
        /// </summary>
        [Test]
        public void ProtectTheWeak_ReachesFourSpacesWhereStandGuardReachesThree()
        {
            var world = World.Swap(RowennaCardContent.ProtectTheWeak);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[]
                {
                    Ally.Value, BesideEnemyMinion.Value, DistantMinion.Value
                }),
                "Range 4 admits the minion at (4,0), which is adjacent to the enemy hero; the lone " +
                "minion still satisfies neither half of the clause.");
        }

        /// <summary>
        /// p.19's Swap exchanges two spaces and nothing else: each piece ends up standing where the
        /// other was, and the page states outright that this "does not count as movement or placement".
        /// </summary>
        [Test]
        public void StandGuard_ExchangesTheTwoSpacesAndNothingElse()
        {
            var world = World.Swap(RowennaCardContent.StandGuard);

            var choice = world.RunToPause();
            world.AnswerToCompletion(choice, Pick(choice, BesideEnemyMinion));

            Assert.That(world.Where(Rowenna), Is.EqualTo(BesideTheEnemy));
            Assert.That(world.Where(BesideEnemyMinion), Is.EqualTo(RowennaAt));
        }

        /// <summary>
        /// A unit p.18's immunity will not let her swap with is not offered at all. The selection asks
        /// the enclosing action's activity, which p.12 makes the Skill - "card text has no activity of
        /// its own" - so a hero immune to her Skill is out before the qualifier is even asked.
        /// </summary>
        [Test]
        public void StandGuard_DoesNotOfferAFriendlyHeroImmuneToHerSkill()
        {
            var world = World.Swap(
                RowennaCardContent.StandGuard,
                modifications: ImmunityFor(Ally, RuleActivity.Skill));

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { BesideEnemyMinion.Value }),
                "An immune unit cannot be targeted at all, which p.18 states as a rule about " +
                "targeting rather than about the operation that follows.");
        }

        // -------------------------------------------------------------------------------------------
        // Defend the Innocent - "...; you may retrieve a discarded card."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The semicolon puts her own retrieval after the swap, and p.10 makes it hers: her pile, her
        /// choice. Both halves happen in the printed order.
        /// </summary>
        [Test]
        public void DefendTheInnocent_OffersHerOwnRetrievalAfterTheSwapHasHappened()
        {
            var world = World.Swap(RowennaCardContent.DefendTheInnocent);

            var subject = world.RunToPause();
            var retrieval = world.Answer(subject, Pick(subject, BesideEnemyMinion));

            Assert.That(world.Where(Rowenna), Is.EqualTo(BesideTheEnemy),
                "The swap is a mandatory step and runs before the retrieval is offered.");
            Assert.That(retrieval.ChooserId, Is.EqualTo(Rowenna));
            Assert.That(CardIds(retrieval), Is.EqualTo(new[]
                {
                    "rowenna-discard-a", "rowenna-discard-b"
                }),
                "p.10: 'you can never retrieve another hero's cards', so the pile is her own.");

            world.AnswerToCompletion(retrieval, Pick(retrieval, "rowenna-discard-a"));
            Assert.That(world.InHand("rowenna-discard-a"), Is.True);
        }

        /// <summary>"you may" is hers to decline, and declining leaves the swap standing.</summary>
        [Test]
        public void DefendTheInnocent_LetsHerDeclineTheRetrievalWithoutUndoingTheSwap()
        {
            var world = World.Swap(RowennaCardContent.DefendTheInnocent);

            var subject = world.RunToPause();
            var retrieval = world.Answer(subject, Pick(subject, BesideEnemyMinion));
            Assert.That(retrieval.DeclineInvocation, Is.Not.Null);
            world.AnswerToCompletion(retrieval, retrieval.DeclineInvocation);

            Assert.That(world.InHand("rowenna-discard-a"), Is.False);
            Assert.That(world.InHand("rowenna-discard-b"), Is.False);
            Assert.That(world.Where(Rowenna), Is.EqualTo(BesideTheEnemy),
                "A declined second clause says nothing about the first.");
        }

        /// <summary>
        /// A swap with no legal subject stops the card at the selection, and the retrieval after the
        /// semicolon is never offered - p.10's mandatory-step order, which the sequence expresses by
        /// ordering rather than by a condition of its own.
        /// </summary>
        [Test]
        public void DefendTheInnocent_OffersNoRetrievalWhenNoFriendlyUnitQualifies()
        {
            var world = World.Swap(
                RowennaCardContent.DefendTheInnocent,
                allyDiscards: 0,
                minions: new (MinionId, HexCoordinate)[] { (LoneMinion, BesideNobody) });

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "The text is over: nothing pauses, because the step that could have asked cannot be " +
                "taken and the step after it is never reached.");
            Assert.That(world.Where(Rowenna), Is.EqualTo(RowennaAt));
            Assert.That(world.InHand("rowenna-discard-a"), Is.False,
                "and no card comes back, which is the whole point of asking in the printed order.");
        }

        // -------------------------------------------------------------------------------------------
        // Throw the Gauntlet - "Place yourself into a space in range adjacent to an enemy hero in
        // range; that hero may move 1 space; if they do, gain 2 coins."
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The first question is which enemy hero, and the set is those inside her printed Range. The
        /// hero five spaces away has an empty space beside him that IS inside her Range, so a reading
        /// that bounded only the destination would offer him.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_NamesOnlyTheEnemyHeroesInsideHerPrintedRange()
        {
            var world = World.Gauntlet();

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { Enemy.Value }),
                "The hero at (5,0) is outside Range 4 even though (4,0) beside him is inside it - " +
                "the printed sentence bounds the hero as well as the space.");
        }

        /// <summary>
        /// The destinations are exactly the empty spaces beside the named hero that are inside her
        /// Range, and an occupied neighbour is not one of them.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_OffersTheEmptySpacesInRangeBesideTheNamedHero()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));

            Assert.That(Spaces(place), Is.EquivalentTo(new[]
                {
                    new HexCoordinate(3, 0), new HexCoordinate(2, -1), new HexCoordinate(3, -1),
                    new HexCoordinate(1, 1), new HexCoordinate(2, 1)
                }),
                "Six neighbours, minus the one the friendly minion at (1,0) is standing in. Every " +
                "one of them is inside her Range 4.");
        }

        /// <summary>
        /// The destination is bounded by HER printed Range as well as by the hero's neighbourhood, and
        /// an enemy standing at the far edge of it is what proves the two bounds are both applied:
        /// three of his six neighbours are five spaces from her, which is reach the card does not have.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_OffersNoSpaceBeyondHerOwnPrintedRange()
        {
            var atTheEdge = new HexCoordinate(4, 0);
            var world = World.Gauntlet(
                enemyAt: atTheEdge,
                secondEnemyAt: new HexCoordinate(0, 5),
                minions: Array.Empty<(MinionId, HexCoordinate)>());

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));

            Assert.That(Spaces(place), Is.EquivalentTo(new[]
                {
                    new HexCoordinate(3, 0), new HexCoordinate(4, -1), new HexCoordinate(3, 1)
                }),
                "(5,0), (5,-1) and (4,1) are all beside him and all five spaces from her, so the " +
                "Place never offers them - p.16 raises a printed icon and never invents one.");
            Assert.That(
                Spaces(place).Select(space => HexDistanceQuery.Calculate(atTheEdge, space)),
                Is.All.EqualTo(1),
                "and every space it does offer is beside him, which is the other half of the clause.");
        }

        /// <summary>
        /// An enemy hero inside her Range with no empty space beside him is not offered at all. That
        /// is milestone 8.35l's consumer narrowing: the Place is declared as the selection's consumer,
        /// so a hero the Place could do nothing with is greyed rather than named and then refused.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_DoesNotNameAnEnemyWithNoEmptySpaceBesideHim()
        {
            var boxedIn = new HexCoordinate(0, 3);
            var world = World.Gauntlet(
                secondEnemyAt: boxedIn,
                obstaclesAt: boxedIn.GetNeighbors().ToArray());

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { Enemy.Value }),
                "The hero at (0,3) is inside Range 4 and every space beside him is an obstacle, so " +
                "the Place that would follow has nothing to offer and he is never named.");
        }

        /// <summary>
        /// "that hero may move 1 space" is the OPPONENT's decision - RO-Q2, and the 2026-08-19
        /// designer answer behind it - so the pause names him as its chooser and the acting seat is
        /// refused as the wrong chooser rather than as an invalid answer.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_PutsTheMoveToTheNamedHeroAndRefusesTheActorAnsweringIt()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));

            Assert.That(move.ChooserId, Is.EqualTo(Enemy),
                "The mover decides where they move, which is p.10's own rule about whose a step's " +
                "decisions are.");

            var forged = new EffectChoiceCommand(
                move.ResolutionId, move.ChoiceId, EffectChoiceCommandKind.Decline,
                answeringHeroId: Rowenna);
            var refused = world.Submit(forged);

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.WrongChooser));
            Assert.That(world.Coins(Rowenna), Is.Zero,
                "and nothing is paid on a refused answer: the question is still standing.");
        }

        /// <summary>
        /// The distance is exact. "may" makes the step optional and leaves the number alone - p.11's
        /// "up to" is not printed here - so every space offered is one space away and standing still
        /// is the Decline rather than a candidate.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_OffersTheNamedHeroExactlyOneSpace()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));

            Assert.That(
                Spaces(move).Select(space => HexDistanceQuery.Calculate(GauntletEnemyAt, space)),
                Is.All.EqualTo(1));
            Assert.That(Spaces(move), Has.No.Member(GauntletEnemyAt),
                "His own space is not an answer; declining is.");
            Assert.That(move.DeclineInvocation, Is.Not.Null);
        }

        /// <summary>The coins follow the move actually being made, and they are ROWENNA's.</summary>
        [Test]
        public void ThrowTheGauntlet_PaysHerTwoCoinsWhenTheNamedHeroMoves()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));
            world.AnswerToCompletion(move, Pick(move, new HexCoordinate(2, 1)));

            Assert.That(world.Where(Rowenna), Is.EqualTo(new HexCoordinate(3, 0)));
            Assert.That(world.Where(Enemy), Is.EqualTo(new HexCoordinate(2, 1)));
            Assert.That(world.Coins(Rowenna), Is.EqualTo(2),
                "'gain 2 coins' names no other subject, so it is the acting hero's own gain.");
            Assert.That(world.Coins(Enemy), Is.Zero);
        }

        /// <summary>
        /// A declined move pays nothing. The flag is published only on a committed move, and an
        /// unbound flag reads as false - which is exactly what the printed "if they do" asks for.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_PaysNothingWhenTheNamedHeroDeclines()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));
            world.AnswerToCompletion(move, move.DeclineInvocation);

            Assert.That(world.Where(Enemy), Is.EqualTo(GauntletEnemyAt));
            Assert.That(world.Coins(Rowenna), Is.Zero);
        }

        /// <summary>
        /// A hero with nowhere to go is not asked at all, and pays nothing. The move reports itself
        /// impossible and its own `may` turns that into a step not taken - which is the same outcome a
        /// decline produces and the correct one for both.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_AsksNobodyAndPaysNothingWhenTheNamedHeroCannotMove()
        {
            // Five of his six neighbours are obstacles; the sixth is the only space her Place can
            // offer, so she takes it and closes the last one herself.
            var world = World.Gauntlet(
                obstaclesAt: GauntletEnemyAt.GetNeighbors()
                    .Where(space => !space.Equals(new HexCoordinate(3, 0)))
                    .ToArray(),
                minions: Array.Empty<(MinionId, HexCoordinate)>());

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            Assert.That(Spaces(place), Is.EqualTo(new[] { new HexCoordinate(3, 0) }));

            var result = world.SubmitToCompletion(Pick(place, new HexCoordinate(3, 0)));

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses after the Place: the move has no destination, so its `may` makes it " +
                "a step not taken rather than a prompt with no answers.");
            Assert.That(world.Where(Enemy), Is.EqualTo(GauntletEnemyAt));
            Assert.That(world.Coins(Rowenna), Is.Zero);
        }

        /// <summary>
        /// An enemy hero immune to her Skill cannot be named at all, so the whole sentence stops: p.18
        /// makes immunity a rule about targeting, and the Place has nobody to stand beside.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_CannotNameAnEnemyHeroImmuneToHerSkill()
        {
            var world = World.Gauntlet(modifications: ImmunityFor(Enemy, RuleActivity.Skill));

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "The only enemy hero inside her Range is immune, and no substitute exists - the " +
                "mandatory selection is impossible and the card stops there.");
            Assert.That(world.Where(Rowenna), Is.EqualTo(RowennaAt));
            Assert.That(world.Coins(Rowenna), Is.Zero);
        }

        /// <summary>
        /// A restore taken while the OPPONENT's move is pending resumes against freshly rebuilt
        /// content and answers the same question - which is what a snapshot carries and what the
        /// resolver has to be able to resume from. Milestone 8.35i is where that boundary was learned.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_ResumesThroughARestoreWhileTheOpponentsMoveIsPending()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));

            world.RebindTrustedContent();

            Assert.That(move.ChooserId, Is.EqualTo(Enemy));
            world.AnswerToCompletion(move, Pick(move, new HexCoordinate(2, 1)));

            Assert.That(world.Where(Enemy), Is.EqualTo(new HexCoordinate(2, 1)));
            Assert.That(world.Coins(Rowenna), Is.EqualTo(2),
                "The rebuilt definition publishes the same performed flag, so the condition after it " +
                "reads what it read before the restore.");
        }

        /// <summary>
        /// A stale answer to the opponent's move is refused: the destinations are re-derived when the
        /// answer arrives rather than trusted from the moment the question opened.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_RefusesAnAnswerNamingASpaceTheMoveNeverOffered()
        {
            var world = World.Gauntlet();

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));

            Assert.That(Spaces(move), Has.No.Member(new HexCoordinate(3, 0)),
                "She is standing there, so it was never one of his destinations.");
            Assert.That(
                move.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    invocation.Candidate.Value.Coordinate.Equals(new HexCoordinate(3, 0))),
                Is.False,
                "and the frame offers no invocation for it at all, which is what makes a forged " +
                "answer unanswerable rather than merely wrong.");
        }

        /// <summary>
        /// The two coins land in milestone 8.53's own column, which is the scoreboard a match shows at
        /// the end - the same ledger seam every other card-effect coin crosses.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_CreditsTheTwoCoinsToTheScoreboardsEarnedColumn()
        {
            var world = World.Gauntlet(withScoreboard: true);

            var hero = world.RunToPause();
            var place = world.Answer(hero, Pick(hero, Enemy));
            var move = world.Answer(place, Pick(place, new HexCoordinate(3, 0)));
            world.AnswerToCompletion(move, Pick(move, new HexCoordinate(2, 1)));

            Assert.That(world.CoinsEarned(Rowenna), Is.EqualTo(2));
            Assert.That(world.CoinsEarned(Enemy), Is.Zero);
        }

        // -------------------------------------------------------------------------------------------
        // The vocabulary this block moved
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The one new declaration this block minted is IN what the build registers, which is
        /// milestone 7.9's gate satisfied: milestone 8.57g deals her, so Throw the Gauntlet is a card
        /// a configured match puts in a hand and the registration it needs can be made.
        ///
        /// <para>Milestone 8.57b wrote this case the other way round - held out until a dealt card can
        /// start it - and the dealing inverts it rather than deleting it, because the claim that has
        /// to keep being true is the PAIRING of the declaration with a printed consumer, whichever
        /// side of the gate it is on.</para>
        /// </summary>
        [Test]
        public void ThePlaceBesideANamedHero_IsRegisteredNowThatADealtCardStartsIt()
        {
            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities.TryLoad(
                    SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1, out _),
                Is.True,
                "8.57g deals her, and Throw the Gauntlet is what starts it.");

            Assert.That(
                SyntheticPlacementCapabilities.RegisteredHandlers()
                    .Select(handler => handler.Reference),
                Has.Member(SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1));

            Assert.That(
                SyntheticPlacementCapabilities.DeferredHandlers()
                    .Select(handler => handler.Reference),
                Has.No.Member(SyntheticPlacementCapabilities.PlaceInRangeBesideANamedHeroV1));
        }

        /// <summary>
        /// The two parameters and the one output this block added to REGISTERED capabilities are all
        /// optional, which is what makes every body composed before this milestone offer and publish
        /// exactly what it did.
        /// </summary>
        [Test]
        public void TheWidenedContracts_CarryTheirNewPiecesAsOptionalOnes()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;

            var unitSelection = catalog.Load(SyntheticTargetingCapabilities.RangeUnitTargetV1);
            var qualifier = unitSelection.Schema.Parameters.Single(parameter =>
                parameter.Name == SyntheticTargetingCapabilities
                    .AdjacentToAnEnemyHeroOrHoldingADiscardParameterName);
            Assert.That(qualifier.IsRequired, Is.False);
            Assert.That(qualifier.ValueKind, Is.EqualTo(EffectValueKind.Boolean),
                "One printed clause, carried as one flag - two booleans would read the printed 'or' " +
                "as an 'and'.");

            // Milestone 8.88. The hero arm declares it too, optional - the family composes every
            // clause on every arm it can mean something on.
            Assert.That(
                catalog.Load(SyntheticTargetingCapabilities.RangeHeroTargetV1)
                    .Schema.Parameters.Single(parameter =>
                        parameter.Name == SyntheticTargetingCapabilities
                            .AdjacentToAnEnemyHeroOrHoldingADiscardParameterName)
                    .IsRequired,
                Is.False,
                "and optional there, so no hero selection composed before 8.88 changes.");

            var move = catalog.Load(SyntheticDisplacementCapabilities.DirectMoveV3);
            var chooser = move.Schema.Parameters.Single(parameter =>
                parameter.Name == SyntheticDisplacementCapabilities.ChooserParameterName);
            Assert.That(chooser.IsRequired, Is.False);
            Assert.That(chooser.ValueKind, Is.EqualTo(EffectValueKind.Hero));
            Assert.That(
                move.Schema.Outputs.Select(output => output.OutputName),
                Does.Contain(SyntheticDisplacementCapabilities.DidMoveOutputName),
                "and 'if they do' about a move is the third of the family 8.40i's didRetrieve and " +
                "8.57a's didGrant already state.");
        }

        /// <summary>
        /// Her four composed Skills carry the printed values the shard's table records, transcribed a
        /// second time here so a typo in either is a failure rather than a shared assumption. The
        /// Range column is the one these bodies actually read.
        /// </summary>
        [Test]
        public void TheFourComposedSkills_CarryTheirTranscribedPrintedValues()
        {
            var rows = new[]
            {
                new object[] { RowennaCardContent.ThrowTheGauntlet, 5, 3, 0, 4 },
                new object[] { RowennaCardContent.StandGuard, 9, 6, 2, 3 },
                new object[] { RowennaCardContent.ProtectTheWeak, 10, 6, 2, 4 },
                new object[] { RowennaCardContent.DefendTheInnocent, 10, 7, 2, 4 }
            };

            foreach (var row in rows)
            {
                var id = (CardDefinitionId)row[0];
                var definition = RowennaCardContent.Definition(id);
                var skill = definition.ActionDefinitions.Single(
                    action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                Assert.That(definition.Initiative.Value, Is.EqualTo((int)row[1]), id.Value);
                Assert.That(skill.Kind, Is.EqualTo(ActionKind.Skill), id.Value);
                Assert.That(Icon(definition, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo((int)row[2]), id.Value);
                Assert.That(Icon(definition, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo((int)row[3]), id.Value);
                Assert.That(
                    skill.BaseValues.Single(value => value.Channel == PrintedValueChannel.Range)
                        .Amount,
                    Is.EqualTo((int)row[4]), id.Value);
                Assert.That(definition.ContentRevision,
                    Is.EqualTo(RowennaCardContent.ContentRevision), id.Value);
            }
        }

        /// <summary>
        /// RO-05: Throw the Gauntlet prints no Movement ribbon icon at all, and a missing icon is
        /// absent rather than zero - so the card defines no Movement action for a player to take.
        /// </summary>
        [Test]
        public void ThrowTheGauntlet_DefinesNoMovementActionAtAll()
        {
            Assert.That(
                RowennaCardContent.Definition(RowennaCardContent.ThrowTheGauntlet)
                    .ActionDefinitions.Any(action => action.Kind == ActionKind.Movement),
                Is.False);

            Assert.That(
                RowennaCardContent.Definition(RowennaCardContent.StandGuard)
                    .ActionDefinitions.Count(action => action.Kind == ActionKind.Movement),
                Is.EqualTo(1),
                "and every Blue face beside it does print one, so the absence is content rather " +
                "than a builder that forgot.");
        }

        /// <summary>
        /// The Skill bodies belong to the faces that print them and to no others. An Attack face asked
        /// for a Skill body gets nothing, which is what keeps the two switches from overlapping.
        /// </summary>
        [Test]
        public void SkillEffectFor_ComposesTheFourFacesThisBlockOwnsAndNoOthers()
        {
            foreach (var skill in new[]
            {
                RowennaCardContent.ThrowTheGauntlet, RowennaCardContent.StandGuard,
                RowennaCardContent.ProtectTheWeak, RowennaCardContent.DefendTheInnocent
            })
            {
                Assert.That(
                    RowennaEffectContent.SkillEffectFor(skill, RowennaCardContent.ContentRevision),
                    Is.Not.Null, skill.Value);
                Assert.That(
                    RowennaEffectContent.AttackTextFor(skill, RowennaCardContent.ContentRevision),
                    Is.Null, skill.Value);
            }

            Assert.That(
                RowennaEffectContent.SkillEffectFor(
                    RowennaCardContent.CodeOfChivalry, RowennaCardContent.ContentRevision),
                Is.Null);
        }

        /// <summary>
        /// Stand Guard and Protect the Weak print one sentence and compose to one body shape; Defend
        /// the Innocent is that shape with the retrieval after it. Asserting the node ids rather than
        /// the whole tree keeps this about the sentence rather than about the composition's spelling.
        /// </summary>
        [Test]
        public void TheTwoUpgradeSteps_AddOnlyWhatTheirPrintedTextAdds()
        {
            var standGuard = NodeIds(RowennaCardContent.StandGuard);
            Assert.That(NodeIds(RowennaCardContent.ProtectTheWeak), Is.EqualTo(standGuard),
                "Their printed text is identical word for word; only the Range icon differs.");

            Assert.That(
                NodeIds(RowennaCardContent.DefendTheInnocent).Except(standGuard).ToArray(),
                Is.EqualTo(new[] { "guard-retrieve" }),
                "and the Tier III adds exactly the clause after its semicolon.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static string[] NodeIds(CardDefinitionId cardId) =>
            RowennaEffectContent.SkillEffectFor(cardId, RowennaCardContent.ContentRevision)
                .Nodes.Select(node => node.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static int Icon(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .SingleOrDefault();

        /// <summary>
        /// The units a selection offers, by the identity each candidate carries - heroes and minions
        /// in one list, because "a unit" is one offer over p.19's two kinds.
        /// </summary>
        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.Kind == EffectValueKind.Hero
                    ? candidate.Value.HeroId.Value
                    : candidate.Value.MinionId.Value)
                .ToArray();

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

        /// <summary>
        /// A live modification set carrying one immunity, built the way a match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled, on milestone 8.39b's terms.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"rowenna-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"Rowenna{activity}Immunity",
                $"rowenna-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"rowenna-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("rowenna-immunity-source"),
                new ActionDefinitionId("rowenna-immunity-action"),
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
                "The fixture must actually hold the immunity, or the cases above prove nothing.");
            return set;
        }

        /// <summary>
        /// Four seats: two Blue, so "a friendly unit" can be a hero other than Rowenna, and two Red,
        /// so one enemy can stand inside her printed Range and one outside it.
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
        /// One staged position and the frame walking one of Rowenna's Skill bodies over it.
        ///
        /// <para>It holds the live card instances, the coin ledger and the board, and rebuilds the
        /// trusted content on demand so a case can ask what a restore would ask. Nothing here is a
        /// configured match: see the fixture's own remarks for why, and for who owns the part this
        /// cannot reach.</para>
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly HeroCoinLedger ledger;
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                CardDefinitionId cardId,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                MatchParticipants participants,
                HeroCoinLedger ledger,
                MatchComponents components,
                RuleModificationSet modifications,
                MatchScoreboard scoreboard)
            {
                this.cardId = cardId;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.ledger = ledger;
                this.components = components;
                this.modifications = modifications;
                Scoreboard = scoreboard;
                context = BuildContext();
            }

            internal MatchScoreboard Scoreboard { get; }

            /// <summary>
            /// The swap board: one ally, one enemy hero at the far edge of Range 3, one enemy well
            /// outside every Range she prints, and three friendly minions - one beside the enemy, one
            /// beside nobody, and one beside the enemy but four spaces out.
            /// </summary>
            internal static World Swap(
                CardDefinitionId cardId,
                HexCoordinate? allyAt = null,
                int allyDiscards = 2,
                (MinionId, HexCoordinate)[] minions = null,
                RuleModificationSet modifications = null) =>
                Create(
                    cardId,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Ally, allyAt ?? AllyAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(SecondEnemy, FarEnemyAt)
                    },
                    minions ?? new[]
                    {
                        (BesideEnemyMinion, BesideTheEnemy),
                        (LoneMinion, BesideNobody),
                        (DistantMinion, BeyondThreeBesideTheEnemy)
                    },
                    Array.Empty<HexCoordinate>(),
                    allyDiscards,
                    modifications,
                    false);

            /// <summary>
            /// The gauntlet board: one enemy hero two spaces out, one at five - outside her printed
            /// Range 4, though the space beside him is inside it - and a friendly minion standing in
            /// one of the first enemy's six neighbours.
            /// </summary>
            internal static World Gauntlet(
                HexCoordinate? enemyAt = null,
                HexCoordinate? secondEnemyAt = null,
                HexCoordinate[] obstaclesAt = null,
                (MinionId, HexCoordinate)[] minions = null,
                RuleModificationSet modifications = null,
                bool withScoreboard = false) =>
                Create(
                    RowennaCardContent.ThrowTheGauntlet,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Ally, AllyAt),
                        new HeroOccupancy(Enemy, enemyAt ?? GauntletEnemyAt),
                        new HeroOccupancy(SecondEnemy, secondEnemyAt ?? FarEnemyAt)
                    },
                    minions ?? new[] { (BesideEnemyMinion, new HexCoordinate(1, 0)) },
                    obstaclesAt ?? Array.Empty<HexCoordinate>(),
                    2,
                    modifications,
                    withScoreboard);

            private static World Create(
                CardDefinitionId cardId,
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At)> minions,
                IReadOnlyList<HexCoordinate> obstaclesAt,
                int allyDiscards,
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
                AddDiscards(instances, pile, Rowenna, "rowenna-discard", 2);
                AddDiscards(instances, pile, Ally, "ally-discard", allyDiscards);

                return new World(
                    cardId,
                    definitions,
                    instances,
                    participants,
                    ledger,
                    Components(heroes, minions, obstaclesAt),
                    modifications ?? RuleModificationSet.Empty,
                    scoreboard);
            }

            private static void AddDiscards(
                ICollection<CardInstance> into,
                CardDefinition definition,
                HeroId owner,
                string prefix,
                int count)
            {
                for (var index = 0; index < count; index++)
                {
                    into.Add(new CardInstance(
                        new CardInstanceId($"{prefix}-{(char)('a' + index)}"),
                        definition.Reference,
                        owner,
                        CardStateFacts.DiscardedUnplayed()));
                }
            }

            /// <summary>
            /// A definition for the cards sitting in the piles. It is a synthetic fixture card rather
            /// than one of Rowenna's, because what these cases are about is whether a pile is empty and
            /// whose it is - not which face it holds.
            /// </summary>
            private static CardDefinition PileDefinition() =>
                new CardDefinition(
                    new CardDefinitionId("rowenna-swap-pile-card"),
                    "rev-1",
                    "TEST - Rowenna Swap Pile Card",
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

            private static MatchComponents Components(
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At)> minions,
                IReadOnlyList<HexCoordinate> obstaclesAt)
            {
                var zone = new ZoneId("rowenna-swap-zone");
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
                    Map, 1, "Rowenna Swap Map",
                    spaces,
                    new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

                var board = new AdjacentMoveAuthority(
                    map,
                    heroes,
                    obstaclesAt
                        .Select((at, index) => new ObstacleOccupancy(
                            new ObstacleId($"rowenna-obstacle-{index:00}"), at))
                        .ToArray(),
                    minions
                        .Select(minion => new QuickGameMinionSetupState(
                            minion.Id,
                            BlueTeam,
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(minion.At)),
                            minion.At))
                        .ToArray());

                return new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board);
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
                var definition = RowennaEffectContent.SkillEffectFor(
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
                    ActionKind.Skill,
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
            /// set because the face under test named a deferred capability while the series ran, and
            /// the sum means the same thing on both sides of the dealing. Naming the DEFERRED list
            /// rather than the whole one is milestone 8.40j's BA-18: a fixture that hands back
            /// already-registered handlers is refused for a duplicate reference.
            /// </summary>
            private static EffectCapabilityCatalog Catalog() =>
                SyntheticEffectCapabilities.CreateCatalog(
                    SyntheticPlacementCapabilities.DeferredHandlers());

            internal EffectResolutionStepResult Run()
            {
                var definition = RowennaCardContent.Definition(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("rowenna-swap-resolution"),
                    new CardInstanceId("rowenna-swap-source"),
                    definition.Reference,
                    cardId.Equals(RowennaCardContent.ThrowTheGauntlet)
                        ? RowennaCardContent.PrimaryBasicSkill
                        : RowennaCardContent.PrimarySkill,
                    Rowenna,
                    Rowenna,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            internal PendingEffectChoice RunToPause()
            {
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
                components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(hero), out var at)
                    ? at
                    : throw new InvalidOperationException($"{hero.Value} stands nowhere.");

            internal HexCoordinate Where(MinionId minion) =>
                components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForMinion(minion), out var at)
                    ? at
                    : throw new InvalidOperationException($"{minion.Value} stands nowhere.");

            internal bool InHand(string cardInstanceId) =>
                instances.Single(card => card.Id.Value == cardInstanceId).State.Location ==
                CardLocation.Hand;

            internal int Coins(HeroId hero) => ledger.GetCoins(hero);

            internal int CoinsEarned(HeroId hero) =>
                Scoreboard.Entries.Single(entry => entry.HeroId.Equals(hero)).CoinsEarned;
        }
    }
}

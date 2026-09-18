using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35l. A printed action a standing rule modification has already barred is not
    /// offered, the pieces it bars are not published as candidates, and the player is told which hero
    /// and card did the barring.
    ///
    /// <para><b>What this exists for, in the reporter's own words.</b> "I played Sabrinas swap with
    /// minion card, and it didnt work beacuse wasps gold. I was still offered the action, which was
    /// kinda sus, but even worse i then had it canceled withought knowing why." Each case here is one
    /// the 2026-09-03 diagnosis measured, turned into a boundary: before this milestone every one of
    /// them offered the action, accepted every command, moved nothing, and left the card resolved in
    /// the turn area with rulebook p.7's one action spent.</para>
    ///
    /// <para><b>The pin is played, never staged.</b> Wasp's Magnetic Dagger prints "After the attack:
    /// This turn: Enemy units in radius cannot be swapped or placed by themselves, or by enemy
    /// heroes" at Initiative 12, and every hero here acts later in the same turn - `TurnEnded` fires
    /// only when no unresolved cards remain, so "this turn" is still standing when they act. Each
    /// blocked case is paired with a control on the same board where Wasp commits something else and
    /// yields, which is what says these cases measure the pin rather than the geometry.</para>
    ///
    /// <para><b>The neighbourhood is left alone deliberately.</b> Every board here passes
    /// <c>clearTheNeighbourhood: false</c>, because the two pieces the staging leaves beside the hero
    /// are what give Wasp's Attack something adjacent to hit - and an Attack that resolves is what
    /// creates the restriction the whole fixture is about.</para>
    /// </summary>
    public sealed class SwapAndPlaceRestrictionFeasibilityTests
    {
        /// <summary>The staged hero's seat identity, which is what a relocation names.</summary>
        private const string StagedHero = "blue-test-hero";

        /// <summary>
        /// Adjacent to Wasp at (-2, 2) and NOT adjacent to Sabina, so a minion parked here is the
        /// Dagger's victim without ever entering her own candidate set.
        /// </summary>
        private static readonly HexCoordinate DaggerVictim = new HexCoordinate(-2, 3);

        /// <summary>Case A. Sabina stands inside Magnetic Dagger's Radius 3 disk, so p.19's Swap -
        /// which moves both objects and asks the restriction about both - is barred whichever partner
        /// she names. No complete legal continuation exists, so milestone 8.29d's principle applies
        /// and the Skill is drawn greyed rather than offered.</summary>
        [Test]
        public void BackToBackWithTheActorHerselfPinned_IsOfferedGreyedAndNamesTheBlockingCard()
        {
            var board = TotallyPinnedSwapBoard();

            var offer = board.PrimaryOffer(
                SabinaCardContent.BackToBack, WaspCardContent.MagneticDagger, letTheEnemyAct: true);

            Assert.That(offer.IsAvailable, Is.False,
                "Every swap Back to Back could make is barred while Sabina stands in the disk, so " +
                "the printed Skill has no complete legal continuation and is not offered.");
            AssertNamesTheDagger(board, offer, RuleActivity.Swap);
        }

        /// <summary>Case A'. The same board with Wasp committing something else. It is what makes
        /// case A a measurement of the pin rather than of the staging.</summary>
        [Test]
        public void BackToBackWithNoPinStanding_IsOfferedAndTheSwapApplies()
        {
            var board = TotallyPinnedSwapBoard();

            var offer = board.PrimaryOffer(SabinaCardContent.BackToBack);

            Assert.That(offer.IsAvailable, Is.True,
                "Without the pin the same board offers the Skill exactly as it always did.");
            Assert.That(offer.Unavailability.IsStated, Is.False,
                "An available offer never carries a reason it cannot be taken.");

            var before = board.HeroSpace(board.Hero);
            board.StartOffer(offer);
            board.SelectFirst();
            board.ConfirmIfStillOwed();
            Assert.That(board.HeroSpace(board.Hero), Is.Not.EqualTo(before),
                "The control has to actually swap, or it proves nothing about the blocked case.");
        }

        /// <summary>Case B, the partial boundary. Sabina stands outside the disk with one partner
        /// inside it and one outside: a legal continuation exists, so the action is rightly offered,
        /// and milestone 6.8l's own charter - "a selection offers only pieces the step after it can
        /// act on" - is what makes the barred partner's absence from the list the claim.</summary>
        [Test]
        public void BackToBackWithOnePartnerPinned_OffersOnlyTheFreePartnerAndSwapsWithIt()
        {
            var board = SabinaBoard.Create(
                clearHerNeighbourhood: false,
                heroPlacements: StagedHeroBoard.At(StagedHero, new HexCoordinate(-1, -3)),
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-02"] = new HexCoordinate(-2, -1),
                    ["blue-ranged-01"] = new HexCoordinate(-1, -5)
                });

            var offer = board.PrimaryOffer(
                SabinaCardContent.BackToBack, WaspCardContent.MagneticDagger, letTheEnemyAct: true);
            Assert.That(offer.IsAvailable, Is.True,
                "One partner is out of the disk, so a complete legal continuation exists and the " +
                "action is right to be offered - this half of the report was never the defect.");

            board.StartOffer(offer);
            Assert.That(NamedMinions(board), Is.EqualTo(new[] { "blue-ranged-01" }),
                "The pinned partner and the free one were published indistinguishably; only the " +
                "partner the Swap can actually take may be named.");

            var before = board.HeroSpace(board.Hero);
            board.SelectFirst();
            board.ConfirmIfStillOwed();
            Assert.That(board.HeroSpace(board.Hero), Is.Not.EqualTo(before),
                "and naming the one that is left still swaps.");
        }

        /// <summary>
        /// Cases C1 and C2, Sabina's Blue swap trio, which is the row that most constrains the fix:
        /// its pair selection names two minions at once and its floor is two. C2 is the board that
        /// proves reading the eligible COUNT alone is not enough - exactly one minion beside her is
        /// free, the narrowing is correct, and the Skill must still be greyed because a question whose
        /// floor is two cannot be answered with one.
        /// </summary>
        [TestCase("blue-ranged-01", TestName = "ListenUp_WithBothMinionsPinned_IsOfferedGreyed")]
        [TestCase("red-ranged-01", TestName = "ListenUp_WithOnlyOneLegalMinionLeft_IsGreyedByTheFloor")]
        public void ListenUpWithNoLegalPairAvailable_IsOfferedGreyed(string secondPiece)
        {
            var board = SabinaBoard.Create(
                clearHerNeighbourhood: false,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = DaggerVictim,
                    ["blue-melee-02"] = StagedHeroBoard.SecondAdjacent,
                    [secondPiece] = StagedHeroBoard.ThirdAdjacent,
                    [secondPiece == "red-ranged-01" ? "blue-ranged-01" : "red-ranged-01"] =
                        new HexCoordinate(2, 0)
                });

            var offer = board.PrimaryOffer(
                SabinaCardContent.ListenUp, WaspCardContent.MagneticDagger, letTheEnemyAct: true);

            Assert.That(offer.IsAvailable, Is.False,
                "p.19's Swap moves both minions, so one barred piece bars the pair - and a board " +
                "left with a single legal minion cannot answer a question whose floor is two.");
            AssertNamesTheDagger(board, offer, RuleActivity.Swap);
        }

        /// <summary>Case C3. Magnetic Dagger's scope is units opposing its own source, so two of
        /// Wasp's own minions are a legal pair with the pin standing - the narrowing must not reach
        /// them, and this is what says it narrows by the rule rather than by the disk.</summary>
        [Test]
        public void ListenUpOnTwoMinionsThePinNeverCovers_IsOfferedAndNamesBothOfThem()
        {
            var board = SabinaBoard.Create(
                clearHerNeighbourhood: false,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-01"] = DaggerVictim,
                    ["red-ranged-01"] = StagedHeroBoard.SecondAdjacent,
                    ["red-melee-01"] = StagedHeroBoard.ThirdAdjacent,
                    ["blue-melee-02"] = new HexCoordinate(2, 0),
                    ["blue-ranged-01"] = new HexCoordinate(2, 1)
                });

            var offer = board.PrimaryOffer(
                SabinaCardContent.ListenUp, WaspCardContent.MagneticDagger, letTheEnemyAct: true);

            Assert.That(offer.IsAvailable, Is.True,
                "The pin covers only units opposing its source, so Wasp's own two minions are a " +
                "legal pair and the narrowing must leave them alone.");
            Assert.That(offer.Unavailability.IsStated, Is.False);

            board.StartOffer(offer);
            Assert.That(NamedMinions(board),
                Is.EqualTo(new[] { "red-melee-01", "red-ranged-01" }),
                "and both halves of the only legal pair are still named.");
        }

        /// <summary>The Arien class row. Arcane Whirlpool composes through the same builder as Ebb and
        /// Flow and binds the same <c>SwapWithActorV1</c> consumer Back to Back does, from a Range
        /// selection instead of a Radius one.</summary>
        [Test]
        public void ArcaneWhirlpoolWithArienPinned_IsOfferedGreyedAndNamesTheBlockingCard()
        {
            var board = ArienBoard.Create(
                tier: 2,
                branch: ArienUpgradeBranch.SecondSibling,
                clearHisNeighbourhood: false);

            var offer = board.PrimaryOffer(
                ArienCardContent.ArcaneWhirlpool,
                WaspCardContent.MagneticDagger,
                letTheEnemyAct: true);

            Assert.That(offer.IsAvailable, Is.False,
                "Arien inside the disk cannot swap with anybody, and the card was offered, taken " +
                "and spent for nothing before this milestone.");
            AssertNamesTheDagger(board, offer, RuleActivity.Swap);
        }

        [Test]
        public void ArcaneWhirlpoolWithNoPinStanding_IsOfferedAndTheSwapApplies()
        {
            var board = ArienBoard.Create(
                tier: 2,
                branch: ArienUpgradeBranch.SecondSibling,
                clearHisNeighbourhood: false);

            var offer = board.PrimaryOffer(ArienCardContent.ArcaneWhirlpool);

            Assert.That(offer.IsAvailable, Is.True);
            var before = board.HeroSpace(board.Hero);
            board.StartOffer(offer);
            board.SelectFirst();
            board.ConfirmIfStillOwed();
            Assert.That(board.HeroSpace(board.Hero), Is.Not.EqualTo(before));
        }

        /// <summary>
        /// The Tigerclaw class row, and the worst-presenting case the diagnosis measured: its subject
        /// comes from an actor-space bridge rather than a selection, so there was no candidate list to
        /// narrow and the card drew no prompt at all - it was clicked and silently evaporated. It is
        /// also the row that proves the fix is not confined to selection narrowing: the feasibility
        /// walk had to stop short-circuiting at nodes that only publish a fact.
        /// </summary>
        [Test]
        public void BlendIntoShadowsWithTigerclawPinned_IsOfferedGreyedAndNamesTheBlockingCard()
        {
            var board = TerrainAdjacentTigerclawBoard();

            var offer = board.PrimaryOffer(
                TigerclawCardContent.BlendIntoShadows,
                WaspCardContent.MagneticDagger,
                letTheEnemyAct: true);

            Assert.That(offer.IsAvailable, Is.False,
                "p.18's Place is the other arm of Magnetic Dagger's sentence, and this card asked " +
                "the player nothing at all before evaporating.");
            AssertNamesTheDagger(board, offer, RuleActivity.Place);
        }

        [Test]
        public void BlendIntoShadowsWithNoPinStanding_IsOfferedAndThePlaceApplies()
        {
            var board = TerrainAdjacentTigerclawBoard();

            var offer = board.PrimaryOffer(TigerclawCardContent.BlendIntoShadows);

            Assert.That(offer.IsAvailable, Is.True);
            var before = board.HeroSpace(board.Hero);
            board.StartOffer(offer);
            Assert.That(board.Choice, Is.Not.Null,
                "The control has to reach the destination question the blocked case never drew.");
            board.SelectFirst();
            board.ConfirmIfStillOwed();
            Assert.That(board.HeroSpace(board.Hero), Is.Not.EqualTo(before));
        }

        /// <summary>
        /// The consequence the report called the worst part - "i then had it canceled withought
        /// knowing why" - closed at its cause. A greyed offer is not startable, and the refusal a
        /// start produces is the same answer the greying is, so the card stays where it was and
        /// rulebook p.7's one action is not spent on nothing.
        /// </summary>
        [Test]
        public void StartingABarredSkillIsRefused_AndNothingOnTheBoardMoves()
        {
            var board = TotallyPinnedSwapBoard();

            var offer = board.PrimaryOffer(
                SabinaCardContent.BackToBack, WaspCardContent.MagneticDagger, letTheEnemyAct: true);
            Assert.That(offer.IsAvailable, Is.False);

            var before = board.HeroSpace(board.Hero);
            var result = board.Session.Execute(
                DirectorCommand.StartAction(offer.ActionDefinitionId));

            Assert.That(result.Accepted, Is.False,
                "The greyed control and the refusal a start would produce are one answer read at " +
                "two moments. Before this milestone the start was ACCEPTED, the Swap stopped, and " +
                "the card was resolved into the turn area having moved nothing.");
            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(before),
                "and nothing on the board moved.");
        }

        /// <summary>
        /// Sabina inside the disk with two friendly minions beside her: every partner Back to Back
        /// could name is barred, because the restriction is asked about her as well as about them.
        /// `blue-melee-01` stands where the staging left it, adjacent to Wasp, so his Attack has
        /// something to resolve against.
        /// </summary>
        private static StagedHeroBoard TotallyPinnedSwapBoard() =>
            SabinaBoard.Create(
                clearHerNeighbourhood: false,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-02"] = StagedHeroBoard.SecondAdjacent,
                    ["blue-ranged-01"] = StagedHeroBoard.ThirdAdjacent
                });

        /// <summary>
        /// Tigerclaw on (-3, 4), which is adjacent to the terrain space (-3, 3) that Blend Into
        /// Shadows' own gate asks about and inside Wasp's Radius 3 disk; the minion the staging leaves
        /// on that space is parked well clear of it.
        /// </summary>
        private static StagedHeroBoard TerrainAdjacentTigerclawBoard() =>
            TigerclawBoard.Create(
                clearHisNeighbourhood: false,
                heroPlacements: StagedHeroBoard.At(StagedHero, new HexCoordinate(-3, 4)),
                minionPlacements: StagedHeroBoard.At(
                    "blue-ranged-01", new HexCoordinate(2, 0)));

        private static string[] NamedMinions(StagedHeroBoard board) =>
            board.Choice.Candidates
                .Select(candidate => candidate.Value.MinionId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// The reason is the authority's own answer, not a second reading: the hero, the card and the
        /// activity all come off the applied modification the refusing capability named.
        /// </summary>
        private static void AssertNamesTheDagger(
            StagedHeroBoard board, ActionOffer offer, RuleActivity expectedActivity)
        {
            Assert.That(offer.Unavailability.IsStated, Is.True,
                "A control greyed by another hero's standing card text must say so - nothing on the " +
                "board tells the player that Wasp's gold is what stopped them.");
            Assert.That(offer.Unavailability.Kind,
                Is.EqualTo(ActionUnavailabilityKind.BlockedByRuleModification));
            Assert.That(offer.Unavailability.BlockedActivity, Is.EqualTo(expectedActivity));
            Assert.That(offer.Unavailability.BlockingHeroId, Is.EqualTo(board.Enemy),
                "The blocking hero is the one controlling the source, which is what a tooltip names.");

            var blockingCard = board.Session.Projection.Cards.Single(card =>
                card.Id.Equals(offer.Unavailability.BlockingCardInstanceId));
            Assert.That(blockingCard.DefinitionReference.DefinitionId,
                Is.EqualTo(WaspCardContent.MagneticDagger),
                "and the card is the one whose text created the standing effect.");
        }
    }
}

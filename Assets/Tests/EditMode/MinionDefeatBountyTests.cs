using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38c1. The standing promise Swift's Mark for Death and Hunting Season print -
    /// "<b>Next turn:</b> the first time an enemy minion in radius is defeated, gain 1 coin", and the
    /// first two times - proved as vocabulary before her content exists.
    ///
    /// <para><b>The count is instance multiplicity, and most of these cases are about that.</b> A face
    /// promising two payments stands two effects and each is spent once, so nothing counts and nothing
    /// new is persisted: "how many payments remain" is "how many are still live", which the lifecycle
    /// already records, restores and undoes.</para>
    ///
    /// <para>The board is <see cref="SwiftJumpBoard"/> again - one zone, a radius-4 disk, no Spawn
    /// points - so distance is the only thing that can decide a radius case.</para>
    /// </summary>
    [TestFixture]
    public sealed class MinionDefeatBountyTests
    {
        private const int Radius = 2;

        private static readonly ActionDefinitionId BountyAction = new ActionDefinitionId("probe-bounty");

        private static readonly CardInstanceId BountyCard = new CardInstanceId("probe-bounty-card");

        // -------------------------------------------------------------------------------------------
        // Spending one
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One bounty, one enemy minion defeated inside the disk: one coin, and the promise is spent.
        /// </summary>
        [Test]
        public void ADefeatInsideTheRadius_PaysOneCoinAndSpendsThePromise()
        {
            var world = World(bounties: 1);
            var minion = EnemyMinionAt(new HexCoordinate(2, 0));

            var paid = Spend(world, minion);

            Assert.That(paid.Count, Is.EqualTo(1));
            Assert.That(paid[0].RecipientHeroId, Is.EqualTo(SwiftJumpBoard.Swift));
            Assert.That(paid[0].Amount, Is.EqualTo(1), "The face prints one coin, not the minion's own.");
            Assert.That(world.Coins.GetCoins(SwiftJumpBoard.Swift), Is.EqualTo(1));
            Assert.That(Live(world), Is.EqualTo(0), "and the promise it paid is no longer standing.");
        }

        /// <summary>
        /// Hunting Season's whole clause. Two promises stand; two defeats spend both, one each; a third
        /// defeat pays nothing. This is the case the multiplicity design exists for.
        /// </summary>
        [Test]
        public void TwoPromises_PayExactlyTwiceAcrossThreeDefeats()
        {
            var world = World(bounties: 2);

            var first = Spend(world, EnemyMinionAt(new HexCoordinate(1, 0)));
            var second = Spend(world, EnemyMinionAt(new HexCoordinate(2, 0)));
            var third = Spend(world, EnemyMinionAt(new HexCoordinate(0, 1)));

            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(third, Is.Empty, "\"the first two times\" is a count of occurrences.");
            Assert.That(world.Coins.GetCoins(SwiftJumpBoard.Swift), Is.EqualTo(2));
            Assert.That(Live(world), Is.EqualTo(0));
        }

        /// <summary>
        /// One minion's Defeat spends ONE promise even when two are standing over it. Two promises are
        /// two future minions, not two payments for one.
        /// </summary>
        [Test]
        public void OneDefeat_SpendsOnlyOnePromiseEvenWhenTwoCoverIt()
        {
            var world = World(bounties: 2);

            var paid = Spend(world, EnemyMinionAt(new HexCoordinate(1, 0)));

            Assert.That(paid.Count, Is.EqualTo(1));
            Assert.That(Live(world), Is.EqualTo(1), "and the other promise is still standing.");
        }

        // -------------------------------------------------------------------------------------------
        // What does not pay
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Outside the disk is outside the clause. The case puts the minion one space beyond the
        /// resolved radius rather than far away, so the boundary itself is what decides.
        /// </summary>
        [Test]
        public void ADefeatOutsideTheRadius_PaysNothingAndSpendsNothing()
        {
            var world = World(bounties: 1);
            var justOutside = new HexCoordinate(Radius + 1, 0);

            Assert.That(
                HexDistanceQuery.Calculate(SwiftJumpBoard.Origin, justOutside),
                Is.EqualTo(Radius + 1),
                "The fixture's own premise: exactly one space past the disk.");

            var paid = Spend(world, EnemyMinionAt(justOutside));

            Assert.That(paid, Is.Empty);
            Assert.That(Live(world), Is.EqualTo(1), "and an unpaid promise is not spent.");
        }

        /// <summary>
        /// The last space the clause does reach, so the case above is a boundary rather than an
        /// off-by-any-amount.
        /// </summary>
        [Test]
        public void ADefeatAtExactlyTheRadius_Pays()
        {
            var world = World(bounties: 1);

            var paid = Spend(world, EnemyMinionAt(new HexCoordinate(Radius, 0)));

            Assert.That(paid.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// "an ENEMY minion", read from the promise's controller rather than from whoever did the
        /// defeating. A friendly minion dying in the disk is not what the card pays for.
        /// </summary>
        [Test]
        public void AFriendlyMinionsDefeat_PaysNothing()
        {
            var world = World(bounties: 1);

            var paid = Spend(world, MinionAt(new HexCoordinate(1, 0), TeamSide.Red));

            Assert.That(paid, Is.Empty);
            Assert.That(Live(world), Is.EqualTo(1));
        }

        /// <summary>
        /// A promise whose maker is not standing anywhere reaches nothing - p.16 ends an Ultimate's
        /// ability while its hero is off the board and p.17 says the same of an Active effect's
        /// controller.
        /// </summary>
        [Test]
        public void APromiseWhoseControllerIsOffTheBoard_PaysNothing()
        {
            var world = World(bounties: 1, controllerOnBoard: false);

            var paid = Spend(world, EnemyMinionAt(new HexCoordinate(1, 0)));

            Assert.That(paid, Is.Empty);
        }

        /// <summary>
        /// A `Next turn:` effect is created dormant and activated when its window opens, so a Defeat
        /// during the turn that PLAYED the card pays nothing. Without this the promise would pay on the
        /// turn it was made, which is the one turn its own printed keyword excludes.
        /// </summary>
        [Test]
        public void ADefeatBeforeTheWindowOpens_PaysNothing()
        {
            var world = World(bounties: 1, activate: false);

            var paid = Spend(world, EnemyMinionAt(new HexCoordinate(1, 0)));

            Assert.That(paid, Is.Empty);
            Assert.That(Live(world), Is.EqualTo(1),
                "and it is NOT spent either - a promise whose window has not opened is still standing " +
                "to be kept next turn. Consuming it here would silently turn `Next turn:` into `this " +
                "turn and next`, which is the one failure that pays the right number of coins on the " +
                "wrong turn.");
        }

        // -------------------------------------------------------------------------------------------
        // Absence
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every dependency is optional, and absence means no bounty rather than a fault: most
        /// compositions in this build hold no lifecycle or no ledger, and a Defeat in one of them must
        /// resolve exactly as it did before this milestone existed.
        /// </summary>
        [Test]
        public void EveryAbsentDependency_PaysNothingWithoutThrowing()
        {
            var world = World(bounties: 1);
            var minion = EnemyMinionAt(new HexCoordinate(1, 0));

            Assert.That(
                MinionDefeatBountyQuery.Spend(
                    null, world.Coins, world.Board, world.Participants, null, world.Definitions, world.Instances, minion, 1, 1),
                Is.Empty);
            Assert.That(
                MinionDefeatBountyQuery.Spend(
                    world.Timing, null, world.Board, world.Participants, null, world.Definitions, world.Instances, minion, 1, 1),
                Is.Empty);
            Assert.That(
                MinionDefeatBountyQuery.Spend(
                    world.Timing, world.Coins, null, world.Participants, null, world.Definitions, world.Instances, minion, 1, 1),
                Is.Empty);
            Assert.That(
                MinionDefeatBountyQuery.Spend(
                    world.Timing, world.Coins, world.Board, world.Participants, null, world.Definitions, world.Instances, null, 1, 1),
                Is.Empty);
            Assert.That(Live(world), Is.EqualTo(1), "and none of them spent the promise.");
        }

        /// <summary>
        /// Two promises need distinct uniqueness keys or the lifecycle suppresses the second as a
        /// duplicate - which would silently turn Hunting Season into Mark for Death. The case asserts
        /// the suppression directly, so the content that creates them at 8.38d is written against a
        /// stated rule rather than against a surprise.
        /// </summary>
        [Test]
        public void TwoPromisesSharingAUniquenessKey_AreSuppressedToOne()
        {
            var world = World(bounties: 0);
            var definition = BountyDefinition("shared-key");

            world.Timing.Create(
                new ActiveEffectId("bounty-a"), definition, SwiftJumpBoard.Swift, TeamSide.Red,
                SwiftJumpBoard.Swift, BountyCard, BountyAction, 1, 1);
            var second = world.Timing.Create(
                new ActiveEffectId("bounty-b"), definition, SwiftJumpBoard.Swift, TeamSide.Red,
                SwiftJumpBoard.Swift, BountyCard, BountyAction, 1, 1);

            Assert.That(
                second.Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.DuplicateSuppressed),
                "so a face standing two promises must give them different uniqueness keys.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<CoinGrantOperation> Spend(ProbeWorld world, MinionOccupancy minion) =>
            MinionDefeatBountyQuery.Spend(
                world.Timing, world.Coins, world.Board, world.Participants, null,
                world.Definitions, world.Instances, minion, 1, 2);

        private static int Live(ProbeWorld world) =>
            world.Timing.Effects.Count(effect => effect.Phase != ActiveEffectPhase.Ended);

        private static MinionOccupancy EnemyMinionAt(HexCoordinate at) => MinionAt(at, TeamSide.Blue);

        /// <summary>
        /// One minion standing where the case says, built through the setup shape
        /// <see cref="MinionOccupancy.FromSetup"/> takes - the only way to make one, and the same route
        /// a real board uses.
        /// </summary>
        private static MinionOccupancy MinionAt(HexCoordinate at, TeamSide side) =>
            MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                new MinionId("probe-defeated-minion"),
                new TeamId(side == TeamSide.Red ? "swift-probe-red" : "swift-probe-blue"),
                side,
                StandardMinionType.Melee,
                default,
                new ConfiguredSpaceReference(SwiftJumpBoard.Map, SwiftJumpBoard.SpaceIdFor(at)),
                at));

        private static ActiveEffectDefinition BountyDefinition(string uniquenessKey) =>
            new ActiveEffectDefinition(
                "probe-minion-bounty-" + uniquenessKey,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.Explicit },
                carriesAcrossRoundBoundary: true,
                MinionDefeatBountyQuery.EffectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>());

        private static ProbeWorld World(
            int bounties, bool controllerOnBoard = true, bool activate = true)
        {
            var participants = SwiftJumpBoard.Participants();
            var board = controllerOnBoard
                ? SwiftJumpBoard.CreateBoard()
                : SwiftJumpBoard.CreateBoardWithNoHero();
            var timing = new ActiveEffectLifecycle();

            for (var index = 0; index < bounties; index++)
            {
                timing.Create(
                    new ActiveEffectId("probe-bounty-" + index),
                    BountyDefinition("probe-bounty-key-" + index),
                    SwiftJumpBoard.Swift,
                    TeamSide.Red,
                    SwiftJumpBoard.Swift,
                    BountyCard,
                    BountyAction,
                    1,
                    1);
            }

            if (activate)
            {
                // A `Next turn:` effect is created dormant; its window opens at the next turn's start.
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2),
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red));
            }

            return new ProbeWorld(participants, board, timing, new HeroCoinLedger(participants));
        }

        /// <summary>
        /// The one card every probe bounty names as its source, printing the Radius the disk is drawn
        /// at. The bounty reads its reach from the card rather than from the effect - an Active effect
        /// declares a radius only when its own scope is measured from one - so a probe with no card
        /// would measure nothing, which is a case of its own rather than the default.
        /// </summary>
        private static CardDefinition BountyCardDefinition() =>
            new CardDefinition(
                new CardDefinitionId("probe-bounty-definition"),
                "probe-rev-1",
                "Probe Bounty",
                new InitiativeValue(4),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        BountyAction,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, Radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        null,
                        PrintedTextComposition.DeferredUntil(
                            "never - this is milestone 8.38c1's Radius-printing probe, not content"))
                });

        private sealed class ProbeWorld
        {
            public ProbeWorld(
                MatchParticipants participants,
                AdjacentMoveAuthority board,
                ActiveEffectLifecycle timing,
                HeroCoinLedger coins)
            {
                Participants = participants;
                Board = board;
                Timing = timing;
                Coins = coins;
                Definitions = new[] { BountyCardDefinition() };
                Instances = new[]
                {
                    CardInstance.CreateInHand(
                        BountyCard, Definitions[0].Reference, SwiftJumpBoard.Swift)
                };
            }

            public IReadOnlyList<CardDefinition> Definitions { get; }

            public IReadOnlyList<CardInstance> Instances { get; }

            public MatchParticipants Participants { get; }

            public AdjacentMoveAuthority Board { get; }

            public ActiveEffectLifecycle Timing { get; }

            public HeroCoinLedger Coins { get; }
        }
    }
}

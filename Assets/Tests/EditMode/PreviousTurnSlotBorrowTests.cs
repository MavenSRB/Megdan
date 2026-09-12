using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39f. `a card in the previous turn slot` - the pool Cutter's Legend of the Skies
    /// borrows from, proved as vocabulary before any of her content exists: "The first time each turn
    /// after you perform a primary action, you may perform the primary action of a card in the previous
    /// turn slot."
    ///
    /// <para><b>THE SCOPE THIS FACE LEAVES OPEN IS SETTLED BY THE RULEBOOK, NOT BY THE ENGINE'S
    /// CONVENIENCE.</b> The card says "<em>a</em> card" where every other borrow in this build says
    /// "one of <em>your</em> cards", and the engine's <c>RoundTurnSlot</c> carries one assignment for
    /// EVERY configured hero - so a table-wide reading is both grammatically available and
    /// mechanically implementable, and the card state a borrow is handed really does contain the other
    /// heroes' instances. p.6 step 6 answers it outright: "place your card Faceup on the current Turn
    /// slot of <b>your hero dashboard</b>." A turn slot is printed on a dashboard. There is no
    /// table-wide slot to read, the engine's per-turn record is an aggregate of the six that exist, and
    /// "the previous turn slot" can only be the borrower's own. Recorded as row CU-Q3.</para>
    ///
    /// <para><b>It names a SLOT where milestone 8.38c's pool names a POSITION IN AN ORDER.</b> On the
    /// boards a match ordinarily reaches the two name the same card, because a hero commits one card
    /// per turn - but "your rightmost resolved card" walks back until it finds one, and this reads one
    /// slot and takes what is standing in it. <see cref="ASlotHoldingNoCardOfYours_IsAnEmptyPool"/> is
    /// where they part company.</para>
    ///
    /// <para>The borrow VERB is milestone 8.35z50's and is not re-proved here, and the trigger half of
    /// Legend of the Skies is milestone 8.39f1's. What these cases own is which card the pool
    /// names.</para>
    /// </summary>
    [TestFixture]
    public sealed class PreviousTurnSlotBorrowTests
    {
        private static readonly HeroId Actor = new HeroId("cutter-borrow-actor");
        private static readonly HeroId Other = new HeroId("cutter-borrow-other");

        private static readonly TeamId RedTeam = new TeamId("cutter-borrow-red");
        private static readonly TeamId BlueTeam = new TeamId("cutter-borrow-blue");

        /// <summary>
        /// Wasp's faces stand in for Cutter's, because she has no content until 8.39h and the pool is
        /// not hers alone. All three print a primary and none moves its own card, so
        /// <see cref="BorrowRefusal"/> admits every one and the SLOT is the only thing deciding.
        /// </summary>
        private static readonly CardDefinitionId First = WaspCardContent.Shock;
        private static readonly CardDefinitionId Second = WaspCardContent.MagneticDagger;
        private static readonly CardDefinitionId Third = WaspCardContent.LiftUp;

        /// <summary>
        /// A card a borrow REFUSES. Stop Projectiles prints a Block, and a Block discards its own card
        /// to perform, so it trips <see cref="BorrowRefusal.ActionMovesItsOwnCard"/>.
        /// </summary>
        private static readonly CardDefinitionId Unborrowable = WaspCardContent.StopProjectiles;

        /// <summary>The card the node is resolving on, standing in the CURRENT turn slot.</summary>
        private static CardDefinitionId BorrowingCard => WaspCardContent.Electrocute;

        // -------------------------------------------------------------------------------------------
        // Which card the pool names
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole clause in one case: three turns committed, the hero acting in the fourth, and the
        /// pool is the third turn's card alone.
        /// </summary>
        [Test]
        public void ThePool_IsTheCardInTheSlotBeforeThisOneAndNothingElse()
        {
            Assert.That(
                Named(Candidates(
                    ownCards: new Dictionary<int, CardDefinitionId>
                    {
                        [1] = First,
                        [2] = Second,
                        [3] = Third
                    },
                    currentTurnIndex: 4)),
                Is.EqualTo(new[] { Third }));
        }

        /// <summary>
        /// The slot index decides, not the order the instances happen to sort in. The card in the
        /// previous slot is deliberately the one whose instance id sorts FIRST of the three, so an
        /// implementation reading instance-id order rather than the turn area would name a different
        /// card.
        /// </summary>
        [Test]
        public void TheSlotIndexDecides_NotTheInstanceIdOrdering()
        {
            var byInstanceId = new[] { First, Second, Third }
                .OrderBy(card => InstanceIdFor(card).Value, StringComparer.Ordinal)
                .ToArray();
            var previous = byInstanceId[0];
            var others = byInstanceId.Skip(1).ToArray();

            Assert.That(
                Named(Candidates(
                    ownCards: new Dictionary<int, CardDefinitionId>
                    {
                        [1] = others[0],
                        [2] = others[1],
                        [3] = previous
                    },
                    currentTurnIndex: 4)),
                Is.EqualTo(new[] { previous }),
                "The pool is the card the third slot holds, though its instance id sorts first of " +
                "the three and a set ordered by id would have named another.");
        }

        /// <summary>
        /// THE OWNER SCOPE, on the board where the two readings differ - and it is a board every real
        /// match reaches, because a turn slot holds an assignment for every hero at the table. The
        /// previous slot holds a perfectly borrowable card belonging to the OTHER hero, and the
        /// borrower has none there.
        ///
        /// <para>A table-wide reading of "a card in the previous turn slot" would offer that card. p.6
        /// prints turn slots on a hero's own dashboard, so this pool is empty.</para>
        /// </summary>
        [Test]
        public void AnotherHerosCardInThatSlot_IsNotACardInYourPreviousTurnSlot()
        {
            var candidates = Candidates(
                ownCards: new Dictionary<int, CardDefinitionId> { [1] = First },
                currentTurnIndex: 3,
                foreignCards: new Dictionary<int, CardDefinitionId> { [2] = Second });

            Assert.That(Named(candidates), Is.Empty,
                "A turn slot is printed on a hero's own dashboard (p.6 step 6), so the card standing " +
                "in another hero's slot is not a card in YOUR previous turn slot.");
        }

        /// <summary>
        /// The same board read the other way round, so the case above cannot be passing because the
        /// staging simply failed to put a card anywhere. Give the borrower a card in that same slot and
        /// the pool names it.
        /// </summary>
        [Test]
        public void YourOwnCardInThatSlot_IsTheOneThePoolNames()
        {
            Assert.That(
                Named(Candidates(
                    ownCards: new Dictionary<int, CardDefinitionId>
                    {
                        [1] = First,
                        [2] = Second
                    },
                    currentTurnIndex: 3)),
                Is.EqualTo(new[] { Second }),
                "The premise the case above rests on: the staging does reach this slot.");
        }

        /// <summary>
        /// WHERE THIS POOL AND MILESTONE 8.38c'S PART COMPANY. The borrower holds cards in slots 1 and
        /// 3 and none in slot 2; acting in slot 3, the previous slot holds nothing of theirs, and the
        /// pool is empty rather than reaching back to slot 1.
        ///
        /// <para>"Your rightmost resolved card" would have named the slot-1 card here, which is exactly
        /// why sharing milestone 8.38c's member would have made Cutter's face print Swift's sentence.
        /// The board is staged rather than claimed reachable - a hero commits one card per turn unless
        /// their hand is empty - and it is the pool's fail-closed contract that is being asserted.</para>
        /// </summary>
        [Test]
        public void ASlotHoldingNoCardOfYours_IsAnEmptyPool()
        {
            var candidates = Candidates(
                ownCards: new Dictionary<int, CardDefinitionId> { [1] = First },
                currentTurnIndex: 3,
                emptyHandPassTurns: new[] { 2 });

            Assert.That(Named(candidates), Is.Empty,
                "The face names one slot, so a slot holding no card of yours offers nothing - it does " +
                "not reach past to the slot behind it.");
        }

        /// <summary>
        /// p.6 empties the turn area at the round boundary, so the first turn of a round has no
        /// previous slot at all and the clause offers nothing.
        /// </summary>
        [Test]
        public void OnTheFirstTurnOfARound_ThereIsNoPreviousSlot()
        {
            Assert.That(
                Named(Candidates(
                    ownCards: new Dictionary<int, CardDefinitionId>(),
                    currentTurnIndex: 1)),
                Is.Empty);
        }

        /// <summary>
        /// The turn area is emptied at the round boundary, so a card standing in the previous slot of
        /// an EARLIER round is not standing there now.
        /// </summary>
        [Test]
        public void ACardInThatSlotOfAnEarlierRound_IsNotInThePreviousTurnSlot()
        {
            var participants = Participants();
            var instances = OwnedInstances(new[] { First, BorrowingCard, Unborrowable });
            var slots = new[]
            {
                Slot(participants, instances, roundIndex: 1, turnIndex: 2, card: First)
            };

            var candidates = SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots, roundIndex: 2, turnIndex: 3));

            Assert.That(Named(candidates), Is.Empty,
                "Round 2 turn 3's previous slot is round 2 turn 2, and nothing is standing there.");
        }

        /// <summary>
        /// The refusal applies to the card the slot names and nothing else. The previous slot holds a
        /// card the borrow refuses, and an earlier slot holds a perfectly borrowable one - a
        /// fall-through would have succeeded, and the pool must still be empty.
        /// </summary>
        [Test]
        public void AnUnborrowableCardInThatSlot_EmptiesThePoolRatherThanFallingThrough()
        {
            var candidates = Candidates(
                ownCards: new Dictionary<int, CardDefinitionId>
                {
                    [1] = First,
                    [2] = Unborrowable
                },
                currentTurnIndex: 3);

            Assert.That(Named(candidates), Is.Empty,
                "Slot 1 holds a borrowable card and a walk backwards would have found it. The face " +
                "names one slot.");
        }

        /// <summary>
        /// The borrowing card stands in the CURRENT slot, so it can never be its own previous-slot
        /// card. Asserted because the guard that drops the source instance is unreachable from the
        /// printed face, and a reader who assumed it load-bearing would be wrong about where the
        /// guarantee lives - milestone 8.38c's own lesson about an invariant worth stating.
        /// </summary>
        [Test]
        public void TheBorrowingCardIsNeverItsOwnPreviousTurnSlotCard()
        {
            var candidates = Candidates(
                ownCards: new Dictionary<int, CardDefinitionId>
                {
                    [1] = First,
                    [2] = Second
                },
                currentTurnIndex: 3,
                borrowingCardAlsoCommittedThisTurn: true);

            Assert.That(Named(candidates), Is.EqualTo(new[] { Second }),
                "The current slot's own card is not what the previous slot holds.");
        }

        /// <summary>
        /// A capability that cannot see the round cannot answer a question about it. Absent roster
        /// state is an empty pool rather than an exception or an unfiltered set.
        /// </summary>
        [Test]
        public void WithNoRosterState_ThePoolIsEmptyRatherThanUnfiltered()
        {
            var participants = Participants();
            var instances = OwnedInstances(new[] { First, Second, BorrowingCard, Unborrowable });

            var candidates = SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots: null, roundIndex: 1, turnIndex: 3));

            Assert.That(candidates, Is.Empty);
        }

        /// <summary>
        /// Swift's pool is untouched by this milestone, asserted on the board where the two readings
        /// differ rather than assumed: the same slots that leave the previous-slot pool empty still
        /// give the rightmost-resolved pool its card.
        /// </summary>
        [Test]
        public void SwiftsRightmostPool_IsUnchangedOnTheBoardWhereTheTwoReadingsDiffer()
        {
            var participants = Participants();
            var instances = OwnedInstances(new[] { First, BorrowingCard, Unborrowable });
            var slots = new[]
            {
                Slot(participants, instances, roundIndex: 1, turnIndex: 1, card: First),
                PassSlot(participants, instances, roundIndex: 1, turnIndex: 2)
            };

            Assert.That(
                Named(SyntheticBorrowedActionCapabilities.Candidates(Context(
                    participants, instances, slots, roundIndex: 1, turnIndex: 3,
                    pool: BorrowedActionPool.OwnedRightmostResolvedCard))),
                Is.EqualTo(new[] { First }),
                "Swift's face walks back to the card it finds.");
            Assert.That(
                Named(SyntheticBorrowedActionCapabilities.Candidates(Context(
                    participants, instances, slots, roundIndex: 1, turnIndex: 3))),
                Is.Empty,
                "and Cutter's reads the one slot it names, on the identical board.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<CardDefinitionId> Named(
            IReadOnlyList<CardInstance> candidates) =>
            candidates
                .Select(card => new CardDefinitionId(
                    card.Id.Value.Substring("cutter-borrow-".Length)))
                .ToArray();

        private static IReadOnlyList<CardInstance> Candidates(
            IReadOnlyDictionary<int, CardDefinitionId> ownCards,
            int currentTurnIndex,
            IReadOnlyDictionary<int, CardDefinitionId> foreignCards = null,
            IReadOnlyList<int> emptyHandPassTurns = null,
            bool borrowingCardAlsoCommittedThisTurn = false)
        {
            var participants = Participants();
            var foreign = foreignCards ?? new Dictionary<int, CardDefinitionId>();
            var instances = OwnedInstances(
                    ownCards.Values.Concat(new[] { BorrowingCard, Unborrowable }))
                .Concat(ForeignInstances(foreign.Values))
                .ToArray();

            var slots = new List<RoundTurnSlot>();
            foreach (var pair in ownCards)
            {
                slots.Add(Slot(
                    participants, instances, roundIndex: 1, turnIndex: pair.Key, card: pair.Value));
            }

            foreach (var pair in foreign)
            {
                slots.Add(Slot(
                    participants, instances, roundIndex: 1, turnIndex: pair.Key, card: pair.Value,
                    heroId: Other));
            }

            foreach (var turn in emptyHandPassTurns ?? Array.Empty<int>())
            {
                slots.Add(PassSlot(participants, instances, roundIndex: 1, turnIndex: turn));
            }

            if (borrowingCardAlsoCommittedThisTurn)
            {
                slots.Add(Slot(
                    participants, instances, roundIndex: 1, turnIndex: currentTurnIndex,
                    card: BorrowingCard));
            }

            return SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots, roundIndex: 1, turnIndex: currentTurnIndex));
        }

        /// <summary>
        /// One turn slot with an assignment for BOTH configured heroes, which is
        /// <see cref="RoundTurnSlot"/>'s own invariant. The hero not committing a card passes with an
        /// empty hand, because <see cref="TurnCardAssignment"/> admits exactly one of a card and a Pass.
        /// </summary>
        private static RoundTurnSlot Slot(
            MatchParticipants participants,
            IReadOnlyList<CardInstance> instances,
            int roundIndex,
            int turnIndex,
            CardDefinitionId card,
            HeroId? heroId = null)
        {
            var holder = heroId ?? Actor;
            var passer = holder.Equals(Actor) ? Other : Actor;
            return new RoundTurnSlot(
                roundIndex,
                turnIndex,
                new[]
                {
                    new TurnCardAssignment(
                        holder, InstanceIdFor(card, holder), passedWithEmptyHand: false),
                    new TurnCardAssignment(passer, null, passedWithEmptyHand: true)
                },
                instances,
                participants);
        }

        /// <summary>A turn both heroes passed in: the slot exists and holds no card at all.</summary>
        private static RoundTurnSlot PassSlot(
            MatchParticipants participants,
            IReadOnlyList<CardInstance> instances,
            int roundIndex,
            int turnIndex) =>
            new RoundTurnSlot(
                roundIndex,
                turnIndex,
                new[]
                {
                    new TurnCardAssignment(Actor, null, passedWithEmptyHand: true),
                    new TurnCardAssignment(Other, null, passedWithEmptyHand: true)
                },
                instances,
                participants);

        private static CardInstanceId InstanceIdFor(CardDefinitionId card) =>
            InstanceIdFor(card, Actor);

        private static CardInstanceId InstanceIdFor(CardDefinitionId card, HeroId owner) =>
            new CardInstanceId(
                (owner.Equals(Actor) ? "cutter-borrow-" : "cutter-borrow-other-") + card.Value);

        private static IReadOnlyList<CardInstance> OwnedInstances(
            IEnumerable<CardDefinitionId> cards) =>
            Instances(cards, Actor);

        private static IReadOnlyList<CardInstance> ForeignInstances(
            IEnumerable<CardDefinitionId> cards) =>
            Instances(cards, Other);

        private static IReadOnlyList<CardInstance> Instances(
            IEnumerable<CardDefinitionId> cards, HeroId owner) =>
            cards
                .Distinct()
                .Select(card => CardInstance.CreateInHand(
                    InstanceIdFor(card, owner),
                    WaspCardContent.Definition(card).Reference,
                    owner))
                .ToArray();

        private static EffectCapabilityContext Context(
            MatchParticipants participants,
            IReadOnlyList<CardInstance> instances,
            IReadOnlyList<RoundTurnSlot> slots,
            int roundIndex,
            int turnIndex,
            BorrowedActionPool pool = BorrowedActionPool.PreviousTurnSlotCard) =>
            new EffectCapabilityContext(
                new EffectNodeId("cutter-borrow-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                InstanceIdFor(BorrowingCard),
                WaspCardContent.Definition(BorrowingCard).Reference,
                new ActionDefinitionId("cutter-borrow-action"),
                new Dictionary<string, EffectValue>
                {
                    [SyntheticBorrowedActionCapabilities.PoolParameter] =
                        EffectValue.Integer((int)pool),
                    [SyntheticBorrowedActionCapabilities.ColorParameter] =
                        EffectValue.Integer((int)CardColor.None)
                },
                roundIndex,
                turnIndex,
                participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(WaspCardContent.AllDefinitions, instances),
                ActionKind.Skill,
                rosterState: slots == null
                    ? null
                    : EffectRosterState.Of(
                        slots,
                        new RoundHeroDefeatHistory(participants),
                        new HeroCoinLedger(participants)));

        private static MatchParticipants Participants()
        {
            var actorPlayer = new PlayerId("cutter-borrow-actor-player");
            var otherPlayer = new PlayerId("cutter-borrow-other-player");
            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                new[] { new Player(actorPlayer, RedTeam), new Player(otherPlayer, BlueTeam) },
                new[]
                {
                    new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Other, otherPlayer, HeroDashboard.RulebookLayout)
                });
        }
    }
}

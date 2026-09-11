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
    /// Milestone 8.38c. `your rightmost resolved card` - the pool Swift's Reload! and her boxed
    /// Handicap Backup borrow from, proved before her content exists.
    ///
    /// <para><b>What is new here is a pool defined by ORDER.</b> The two pools this build already
    /// carries are per-card predicates - a card is in the discard pile or it is not, it counts as
    /// active or it does not - and one card's answer never depends on another's. This one is a
    /// selection over rulebook p.6's turn area, which holds the cards a hero has resolved this round
    /// in committed order, and "rightmost" takes the last of them. The candidate set is therefore at
    /// most one card however many are standing there, which is the property most of these cases
    /// assert in one form or another.</para>
    ///
    /// <para>The borrow VERB is milestone 8.35z50's and is not re-proved here. What these cases own is
    /// which card the pool names.</para>
    /// </summary>
    [TestFixture]
    public sealed class RightmostResolvedCardBorrowTests
    {
        private static readonly HeroId Actor = new HeroId("swift-borrow-actor");
        private static readonly HeroId Other = new HeroId("swift-borrow-other");

        private static readonly TeamId RedTeam = new TeamId("swift-borrow-red");
        private static readonly TeamId BlueTeam = new TeamId("swift-borrow-blue");

        /// <summary>
        /// Three of Wasp's faces stand in for Swift's, because Swift has no content until 8.38d and the
        /// pool is not hers alone. All three print a primary and none of them moves its own card, so
        /// <see cref="BorrowRefusal"/> admits every one and the ORDER is the only thing deciding.
        /// </summary>
        private static readonly CardDefinitionId First = WaspCardContent.Shock;
        private static readonly CardDefinitionId Second = WaspCardContent.MagneticDagger;
        private static readonly CardDefinitionId Third = WaspCardContent.LiftUp;

        /// <summary>
        /// A card a borrow REFUSES, for the one case that needs one. Stop Projectiles prints a Block,
        /// and a Block discards its own card to perform - so it trips
        /// <see cref="BorrowRefusal.ActionMovesItsOwnCard"/>, which is the refusal most likely to be
        /// met in play because every hero's Defense-family faces carry it.
        /// </summary>
        private static readonly CardDefinitionId Unborrowable = WaspCardContent.StopProjectiles;

        // -------------------------------------------------------------------------------------------
        // Which card the pool names
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole clause in one case: three cards resolved this round, and the pool is the last of
        /// them alone.
        /// </summary>
        [Test]
        public void ThePool_IsTheLastCardResolvedThisRoundAndNothingElse()
        {
            var candidates = Candidates(
                resolvedInTurnOrder: new[] { First, Second, Third },
                currentTurnIndex: 4);

            Assert.That(candidates.Count, Is.EqualTo(1),
                "\"your rightmost resolved card\" names one card, whatever is standing beside it.");
            Assert.That(
                candidates[0].DefinitionReference.DefinitionId, Is.EqualTo(Third),
                "and it is the one committed at the highest turn index.");
        }

        /// <summary>
        /// The turn area's committed order decides, not the instance-id ordering the query sorts by
        /// afterwards. The case stages the cards so the two disagree - the rightmost card is the one
        /// whose instance id sorts FIRST - so a query that read the sort rather than the slots would
        /// pick the other card and fail here.
        /// </summary>
        [Test]
        public void TheCommittedOrderDecides_NotTheInstanceIdOrdering()
        {
            var candidates = Candidates(
                resolvedInTurnOrder: new[] { Third, Second, First },
                currentTurnIndex: 4);

            Assert.That(
                candidates.Single().DefinitionReference.DefinitionId, Is.EqualTo(First),
                "First was committed last, so it is rightmost - even though its instance id sorts " +
                "before the other two.");
        }

        /// <summary>
        /// The first turn of a round: nothing has resolved yet, so the pool is empty and the borrow is
        /// impossible. It has to fail closed - an unfiltered pool here would silently become "any card
        /// you own", which is the one wrong answer that looks plausible.
        /// </summary>
        [Test]
        public void WithNothingResolvedYet_ThePoolIsEmptyRatherThanEveryCardYouOwn()
        {
            var candidates = Candidates(
                resolvedInTurnOrder: Array.Empty<CardDefinitionId>(),
                currentTurnIndex: 1);

            Assert.That(candidates, Is.Empty);
        }

        /// <summary>
        /// The trap this pool exists around. Reload! asks the question while Reload! is the card being
        /// resolved, and its own assignment is already in the round's turn slots by then - so a
        /// selector reading the slots naively names the asking card as its own answer.
        /// </summary>
        [Test]
        public void TheBorrowingCardIsNotItsOwnRightmostResolvedCard()
        {
            var candidates = Candidates(
                resolvedInTurnOrder: new[] { First },
                currentTurnIndex: 2,
                borrowingCardAlsoCommittedThisTurn: true);

            Assert.That(candidates.Count, Is.EqualTo(1));
            Assert.That(
                candidates.Single().DefinitionReference.DefinitionId, Is.EqualTo(First),
                "The card asking the question is dropped, so the answer is the card resolved before " +
                "it rather than itself.");
        }

        /// <summary>
        /// p.6 empties the turn area at the round boundary, so a card resolved last round is not
        /// standing there to be rightmost. The case stages a later turn index in the PREVIOUS round,
        /// which a query filtering only on turn index would wrongly prefer.
        /// </summary>
        [Test]
        public void ACardResolvedInAnEarlierRound_IsNotRightmost()
        {
            var participants = Participants();
            var instances = Instances(new[] { First, Second });

            var slots = new[]
            {
                Slot(participants, instances, roundIndex: 1, turnIndex: 4, card: Second),
                Slot(participants, instances, roundIndex: 2, turnIndex: 1, card: First)
            };

            var candidates = SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots, roundIndex: 2, turnIndex: 2));

            Assert.That(
                candidates.Single().DefinitionReference.DefinitionId, Is.EqualTo(First),
                "Second sits at a higher turn index but in a round the turn area no longer holds.");
        }

        /// <summary>
        /// A rightmost card a borrow refuses leaves the pool EMPTY rather than falling through to the
        /// card behind it. This is the reading the pool is documented on: the face names one card, and
        /// walking backwards until a borrowable one turned up would be answering a sentence Swift does
        /// not print.
        ///
        /// <para>The case is deliberately built so the fall-through would SUCCEED if it existed - a
        /// perfectly borrowable card sits at the turn before - so an empty answer can only come from
        /// the reading under test.</para>
        /// </summary>
        [Test]
        public void AnUnborrowableRightmostCard_EmptiesThePoolRatherThanFallingThrough()
        {
            var candidates = Candidates(
                resolvedInTurnOrder: new[] { First, Unborrowable },
                currentTurnIndex: 3);

            Assert.That(candidates, Is.Empty,
                "Stop Projectiles is the rightmost resolved card and a Block discards its own card " +
                "to perform, so the clause has nothing to borrow - and Shock, standing one turn " +
                "back and perfectly borrowable, is not offered in its place.");
        }

        /// <summary>
        /// Another hero's assignment cannot name a card this hero owns - <see cref="RoundTurnSlot"/>
        /// refuses it at construction, so the hero filter in the selector is defence in depth rather
        /// than the thing that carries this rule.
        ///
        /// <para><b>The case asserts the invariant rather than the filter, because the scenario it was
        /// written to stage turns out to be unconstructible.</b> That is worth a case of its own: a
        /// reader who assumes the selector's `HeroId` test is load-bearing would be wrong about where
        /// the guarantee lives, and a later refactor that dropped it would find nothing failing.</para>
        /// </summary>
        [Test]
        public void ATurnSlotRefusesAnAssignmentNamingAnotherHerosCard()
        {
            var participants = Participants();
            var instances = Instances(new[] { First, Second });

            var message = Assert.Throws<ArgumentException>(() => Slot(
                participants, instances, roundIndex: 1, turnIndex: 2, card: Second,
                heroId: Other)).Message;

            Assert.That(message, Does.Contain("owned by its assigned hero"));
        }

        /// <summary>
        /// A capability that cannot see the round cannot answer a question about it. Absent roster
        /// state is an empty pool rather than an exception or an unfiltered set.
        /// </summary>
        [Test]
        public void WithNoRosterState_ThePoolIsEmptyRatherThanUnfiltered()
        {
            var participants = Participants();
            var instances = Instances(new[] { First, Second });

            var candidates = SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots: null, roundIndex: 1, turnIndex: 2));

            Assert.That(candidates, Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<CardInstance> Candidates(
            IReadOnlyList<CardDefinitionId> resolvedInTurnOrder,
            int currentTurnIndex,
            bool borrowingCardAlsoCommittedThisTurn = false)
        {
            var participants = Participants();
            var all = resolvedInTurnOrder.Concat(new[] { BorrowingCard, Unborrowable }).ToArray();
            var instances = Instances(all);

            var slots = resolvedInTurnOrder
                .Select((card, index) => Slot(
                    participants, instances, roundIndex: 1, turnIndex: index + 1, card: card))
                .ToList();

            if (borrowingCardAlsoCommittedThisTurn)
            {
                slots.Add(Slot(
                    participants, instances, roundIndex: 1, turnIndex: currentTurnIndex,
                    card: BorrowingCard));
            }

            return SyntheticBorrowedActionCapabilities.Candidates(
                Context(participants, instances, slots, roundIndex: 1, turnIndex: currentTurnIndex));
        }

        /// <summary>The card the node is resolving on - Reload!'s stand-in.</summary>
        private static CardDefinitionId BorrowingCard => WaspCardContent.Electrocute;

        /// <summary>
        /// One turn slot with an assignment for BOTH configured heroes, which is
        /// <see cref="RoundTurnSlot"/>'s own invariant: "exactly one assignment for every configured
        /// hero". The hero who is not committing a card that turn passes with an empty hand, because
        /// <see cref="TurnCardAssignment"/> admits exactly one of a card and a Pass.
        ///
        /// <para><paramref name="heroId"/> names which hero the card belongs to in the assignment.
        /// Naming <see cref="Other"/> against a card <see cref="Actor"/> OWNS is the point of one case:
        /// the borrow's own owner filter would reject a foreign instance, so only the assignment's hero
        /// filter can reject this, and the case would pass for the wrong reason without it.</para>
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
                        holder, InstanceIdFor(card), passedWithEmptyHand: false),
                    new TurnCardAssignment(passer, null, passedWithEmptyHand: true)
                },
                instances,
                participants);
        }

        private static CardInstanceId InstanceIdFor(CardDefinitionId card) =>
            new CardInstanceId("swift-borrow-" + card.Value);

        private static IReadOnlyList<CardInstance> Instances(IEnumerable<CardDefinitionId> cards) =>
            cards
                .Distinct()
                .Select(card => CardInstance.CreateInHand(
                    InstanceIdFor(card),
                    WaspCardContent.Definition(card).Reference,
                    Actor))
                .ToArray();

        private static EffectCapabilityContext Context(
            MatchParticipants participants,
            IReadOnlyList<CardInstance> instances,
            IReadOnlyList<RoundTurnSlot> slots,
            int roundIndex,
            int turnIndex) =>
            new EffectCapabilityContext(
                new EffectNodeId("swift-borrow-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                InstanceIdFor(BorrowingCard),
                WaspCardContent.Definition(BorrowingCard).Reference,
                new ActionDefinitionId("swift-borrow-action"),
                new Dictionary<string, EffectValue>
                {
                    [SyntheticBorrowedActionCapabilities.PoolParameter] =
                        EffectValue.Integer((int)BorrowedActionPool.OwnedRightmostResolvedCard),
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
            var actorPlayer = new PlayerId("swift-borrow-actor-player");
            var otherPlayer = new PlayerId("swift-borrow-other-player");
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

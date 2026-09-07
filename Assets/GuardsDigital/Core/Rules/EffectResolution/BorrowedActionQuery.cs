using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// What one resolution's card text asked to borrow, and the card it named.
    ///
    /// <para>It is one value rather than a boolean beside a card id because the two are never
    /// independently true: an unrequested borrow has nothing to perform, and a requested one names
    /// exactly the card the player chose. Milestone 8.35z50, and it is deliberately the same shape
    /// <see cref="RepeatRequest"/> carries, because the two answer the same kind of question about a
    /// completed frame.</para>
    /// </summary>
    public readonly struct BorrowRequest : IEquatable<BorrowRequest>
    {
        private BorrowRequest(bool isRequested, CardInstanceId borrowedCardId)
        {
            IsRequested = isRequested;
            BorrowedCardId = borrowedCardId;
        }

        /// <summary>No borrow was asked for - what every non-borrowing action reports.</summary>
        public static BorrowRequest None => default;

        public bool IsRequested { get; }

        /// <summary>The card whose printed primary is to be performed. Meaningful only when requested.</summary>
        public CardInstanceId BorrowedCardId { get; }

        public static BorrowRequest For(CardInstanceId borrowedCardId) =>
            new BorrowRequest(true, borrowedCardId);

        public bool Equals(BorrowRequest other) =>
            IsRequested == other.IsRequested && BorrowedCardId.Equals(other.BorrowedCardId);

        public override bool Equals(object obj) => obj is BorrowRequest other && Equals(other);

        public override int GetHashCode() =>
            (IsRequested ? 1 : 0) ^ (BorrowedCardId.GetHashCode() * 397);

        public override string ToString() =>
            IsRequested ? BorrowedCardId.ToString() : "no borrow";
    }

    /// <summary>
    /// THE DIRECT TWIN OF <c>RepeatedActionQuery.Requested</c>, and separate from it on purpose.
    ///
    /// <para>Milestone 8.35z31 built <c>borrow-primary-action@1</c>, which CHOOSES the card, and stated
    /// in its own doc comment why it stops there: "a capability handler mutates the frame it is in, and
    /// a borrowed primary is a whole action instance with its own resolution, its own pauses and its own
    /// card-text seams, so starting it belongs to the same authority that starts every other action."
    /// That is milestone 6.7's `RepeatAction` shape exactly - the node records a request on the frame and
    /// the card-action dispatcher creates the instance - and this type is how the dispatcher reads the
    /// request back. Milestone 8.35z50 builds it; the four guards that ADMIT a borrowed instance were
    /// widened by the prep that preceded it, so until now nothing produced one for them to admit.</para>
    ///
    /// <para><b>Why it is not folded into <see cref="RepeatedActionQuery"/>.</b> The contract on
    /// <see cref="Cards.BorrowedPrimaryActionDeclaration"/> makes a borrow INVISIBLE to every repeat
    /// reader, and that invisibility is structural rather than checked: the whole repeat mechanism keys
    /// on <see cref="ActionInvocationContext.Repeated"/>, and a borrowed instance is stamped
    /// <see cref="ActionInvocationContext.TextGranted"/>. A shared reader would be the one place the two
    /// could be confused, so there is no shared reader. RULING 22 is the rule this preserves: a borrowed
    /// primary is not a rulebook p.10 repeat, and p.10's "you cannot repeat a repeated action" therefore
    /// says nothing about it.</para>
    ///
    /// <para><b>What the request carries, and what it deliberately does not.</b> It carries the borrowed
    /// CARD and nothing else. Every other property of the instance - its values, its Range and Radius,
    /// its printed text, its supertype, its own "This round: You are enraged." grant - is read off the
    /// borrowed card's own definition by the ordinary machinery, because the offer the dispatcher builds
    /// names that definition as its source. That is RULING 36 satisfied by construction rather than by
    /// copying: nothing is copied, so nothing can be copied wrongly.</para>
    ///
    /// <para><b>The Ruling-43 pool is not this type's business.</b> Which cards are reachable is decided
    /// when the choice is OFFERED, by
    /// <see cref="SyntheticBorrowedActionCapabilities.Candidates"/> - including the post-Ultimate
    /// widening Unbound Fury gives it. By the time a request exists the player has already answered, so
    /// this reads the answer and never re-derives the pool. Re-deriving it here would be a second copy
    /// of a rule that already has one home.</para>
    /// </summary>
    public static class BorrowedActionQuery
    {
        /// <summary>
        /// What this resolution's completed outcomes say it asked to borrow.
        ///
        /// <para>Asked of the trusted definition rather than of a node-name convention, so only a node
        /// the content author declared as a borrow can request one - the same discipline
        /// <c>RepeatedActionQuery.Requested</c> applies to a `RepeatAction` node. The card is read from
        /// the outcome the handler published under
        /// <see cref="SyntheticBorrowedActionCapabilities.BorrowedCardOutput"/>, which is the only place
        /// the answer exists: the handler names the card and touches nothing, so this outcome IS the
        /// decision.</para>
        ///
        /// <para>A borrow node that applied without publishing a card reports no request at all. That is
        /// unreachable through the handler - its `Resume` returns `ImpossibleMandatory` for an answer it
        /// cannot resolve to a candidate - and refusing is the conservative direction if it were reached
        /// anyway, because starting an action off no card is the one outcome the contract cannot
        /// describe.</para>
        ///
        /// <para>The FIRST applied borrow wins. No composed face prints two, and a face that did would
        /// be asking for two whole action instances from one card text, which is a different rule than
        /// any of the four printings this vocabulary was built for.</para>
        /// </summary>
        public static BorrowRequest Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (outcomes == null)
            {
                return BorrowRequest.None;
            }

            foreach (var outcome in outcomes)
            {
                if (outcome.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(outcome.NodeId, out var node) ||
                    node.Kind != EffectNodeKind.Capability ||
                    !node.Capability.Equals(
                        SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1))
                {
                    continue;
                }

                foreach (var fact in outcome.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.CardInstance)
                    {
                        return BorrowRequest.For(fact.Value.CardInstanceId);
                    }
                }

                return BorrowRequest.None;
            }

            return BorrowRequest.None;
        }
    }
}

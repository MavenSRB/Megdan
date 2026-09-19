using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// What one card text asked to apply, and the card it named.
    ///
    /// <para>One value rather than a boolean beside a card id, because the two are never independently
    /// true: an unrequested borrow has nothing to apply, and a requested one names exactly the card the
    /// clause reached. Milestone 8.61g, and deliberately the same shape
    /// <see cref="BorrowRequest"/> carries.</para>
    /// </summary>
    public readonly struct BorrowedCardTextRequest : IEquatable<BorrowedCardTextRequest>
    {
        private BorrowedCardTextRequest(bool isRequested, CardInstanceId borrowedCardId)
        {
            IsRequested = isRequested;
            BorrowedCardId = borrowedCardId;
        }

        /// <summary>No rider borrow was asked for - what every other card text reports.</summary>
        public static BorrowedCardTextRequest None => default;

        public bool IsRequested { get; }

        /// <summary>
        /// The card whose printed "After the attack" text is to be applied. Meaningful only when
        /// requested.
        /// </summary>
        public CardInstanceId BorrowedCardId { get; }

        public static BorrowedCardTextRequest For(CardInstanceId borrowedCardId) =>
            new BorrowedCardTextRequest(true, borrowedCardId);

        public bool Equals(BorrowedCardTextRequest other) =>
            IsRequested == other.IsRequested && BorrowedCardId.Equals(other.BorrowedCardId);

        public override bool Equals(object obj) =>
            obj is BorrowedCardTextRequest other && Equals(other);

        public override int GetHashCode() =>
            (IsRequested ? 1 : 0) ^ (BorrowedCardId.GetHashCode() * 397);

        public override string ToString() =>
            IsRequested ? BorrowedCardId.ToString() : "no borrowed rider";
    }

    /// <summary>
    /// THE DIRECT TWIN OF <see cref="BorrowedActionQuery"/>, and separate from it on purpose.
    ///
    /// <para>Milestone 8.61g builds <c>borrow-after-attack-text@1</c>, which CHOOSES the card and stops
    /// there for milestone 8.35z31's stated reason. This type is how the card-action authority reads
    /// that request back, and it is not folded into
    /// <see cref="BorrowedActionQuery"/> because the two produce different things: a borrowed PRIMARY
    /// becomes a whole action instance owed to the turn, while a borrowed RIDER is one more rulebook
    /// p.14 step-7 card text spliced into the attack already running. A shared reader would be the one
    /// place those could be confused.</para>
    ///
    /// <para><b>It is read from the step that COMPLETED the body, and that is a statement about where a
    /// borrow node may stand.</b> A frame publishes its outcomes cumulatively but a completing step
    /// carries only the events of that step, so a borrow node has to be the last thing its body does -
    /// which is exactly what the printed sentence is. Both of Min's Gold faces print one clause at
    /// p.14's step 7 and nothing after it, and <c>MinBorrowedRiderTests</c> pins that the composed body
    /// is that single node. A face that wanted work after its borrow would be printing a different
    /// sentence than either of these two, and would owe this reader a second look.</para>
    ///
    /// <para><b>Asked of the trusted definition rather than of a node-name convention</b>, so only a
    /// node the content author declared as a rider borrow can request one - the same discipline
    /// <c>RepeatedActionQuery.Requested</c> and <see cref="BorrowedActionQuery"/> apply.</para>
    /// </summary>
    public static class BorrowedCardTextQuery
    {
        /// <summary>
        /// What this card text's produced events say it asked to borrow.
        ///
        /// <para>The card is read from the output the handler published under
        /// <see cref="SyntheticBorrowedCardTextCapabilities.BorrowedCardOutput"/>, which is the only
        /// place the answer exists: the handler names the card and touches nothing, so that output IS
        /// the decision.</para>
        ///
        /// <para>A borrow node that applied without publishing a card reports no request at all. That
        /// is unreachable through the handler - it publishes on every applied path - and refusing is
        /// the conservative direction if it were reached anyway, because applying a body off no card is
        /// the one outcome this contract cannot describe.</para>
        ///
        /// <para>The FIRST applied borrow wins. No composed face prints two, and a face that did would
        /// be asking for two bodies from one clause, which is a different rule than the one this
        /// vocabulary was built for.</para>
        /// </summary>
        public static BorrowedCardTextRequest Requested(
            EffectDefinition definition,
            IReadOnlyList<EffectResolutionEvent> events)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (events == null)
            {
                return BorrowedCardTextRequest.None;
            }

            foreach (var produced in events)
            {
                if (produced == null ||
                    produced.ResultKind != EffectExecutionResultKind.Applied ||
                    !definition.TryGetNode(produced.NodeId, out var node) ||
                    node.Kind != EffectNodeKind.Capability ||
                    !node.Capability.Equals(
                        SyntheticBorrowedCardTextCapabilities.BorrowAfterAttackTextV1))
                {
                    continue;
                }

                foreach (var fact in produced.Outputs)
                {
                    if (string.Equals(
                            fact.OutputName,
                            SyntheticBorrowedCardTextCapabilities.BorrowedCardOutput,
                            StringComparison.Ordinal) &&
                        fact.Value.Kind == EffectValueKind.CardInstance)
                    {
                        return BorrowedCardTextRequest.For(fact.Value.CardInstanceId);
                    }
                }

                return BorrowedCardTextRequest.None;
            }

            return BorrowedCardTextRequest.None;
        }
    }
}

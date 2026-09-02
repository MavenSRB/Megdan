using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// One way to answer a standing removal-prevention decision by paying: a source's price, the card
    /// that would pay it, and the typed command that does so. Milestone 8.21.
    /// </summary>
    public sealed class RemovalPreventionPayment
    {
        public RemovalPreventionPayment(
            string sourceId,
            RemovalPreventionCost cost,
            CardInstanceId paymentCardInstanceId,
            RemovalPreventionCommand invocation)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "A payment names the source whose price it pays.", nameof(sourceId));
            }

            if (!cost.IsDeclared)
            {
                throw new ArgumentException(
                    "A payment carries the price its source declared.", nameof(cost));
            }

            if (!paymentCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A payment names the card that pays.", nameof(paymentCardInstanceId));
            }

            SourceId = sourceId;
            Cost = cost;
            PaymentCardInstanceId = paymentCardInstanceId;
            Invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        }

        public string SourceId { get; }

        /// <summary>What the source charges, which is what a reader may say about the payment.</summary>
        public RemovalPreventionCost Cost { get; }

        public CardInstanceId PaymentCardInstanceId { get; }

        /// <summary>The command this payment is answered with, minted by the pause.</summary>
        public RemovalPreventionCommand Invocation { get; }
    }

    /// <summary>
    /// Milestone 8.21. The standing removal-prevention decision as one immutable projection carries it:
    /// what is being asked, which payments are affordable right now, and the typed command each of them
    /// is answered with.
    ///
    /// <para><b>Why it exists.</b> Milestone 8.19c1 built the decision as rules only, under its own
    /// presentation non-goal, and recorded that no configured card could open it. Brogan's Shield,
    /// Bolster and Fortify are the first content that can, and because <c>HasPendingResolution</c>
    /// refuses every other turn command while the decision stands, a client confined to the projection
    /// would have seen a board that had silently stopped accepting input. This is the field that keeps
    /// that from happening: the decision reaches the player through the one immutable projection, as
    /// every other pending family does.</para>
    ///
    /// <para><b>It mints its invocations rather than describing its options.</b> A reader composes the
    /// words it shows from these typed facts and submits exactly one of the commands here - which is
    /// what makes an option the prompt did not project unsubmittable rather than merely discouraged.
    /// Declining is always among them, because the removal was going to happen anyway and a decision
    /// that could not be refused would stop the match.</para>
    ///
    /// <para><b>Nothing here is stored.</b> The payments are derived from live state every time the
    /// projection is built - a hand that spent its last silver card between the question and the answer
    /// offers one fewer payment - which is the same re-derivation the answer path performs and the
    /// reason a restored pause offers what the live one would.</para>
    /// </summary>
    public sealed class RemovalPreventionPrompt
    {
        private RemovalPreventionPrompt(
            string requestId,
            HeroId decidingHeroId,
            MinionId subjectMinionId,
            RemovalPreventionOrigin origin,
            IReadOnlyList<RemovalPreventionPayment> payments,
            RemovalPreventionCommand declineInvocation)
        {
            RequestId = requestId;
            DecidingHeroId = decidingHeroId;
            SubjectMinionId = subjectMinionId;
            Origin = origin;
            Payments = payments;
            DeclineInvocation = declineInvocation;
        }

        /// <summary>The decision's stable identity, which every invocation here answers.</summary>
        public string RequestId { get; }

        /// <summary>The hero rulebook p.10 gives the decision to.</summary>
        public HeroId DecidingHeroId { get; }

        /// <summary>
        /// The miniature the price would keep on the board. It is projected so a reader can cue the
        /// piece where it stands rather than naming it in the prompt - the same reading milestone
        /// 8.11a took of every candidate that has a board surface.
        /// </summary>
        public MinionId SubjectMinionId { get; }

        /// <summary>Which Defeat is standing at its removal stage.</summary>
        public RemovalPreventionOrigin Origin { get; }

        /// <summary>
        /// Every affordable payment, in the order the rules offer them: by source, then by card
        /// instance. Two sources offering the same card produce two payments, because they are two
        /// different answers - one spends the card on one source's price.
        /// </summary>
        public IReadOnlyList<RemovalPreventionPayment> Payments { get; }

        /// <summary>Letting the removal happen, which is always offered.</summary>
        public RemovalPreventionCommand DeclineInvocation { get; }

        /// <summary>How many distinct sources are offering, which is what makes a prompt say "or".</summary>
        public int OfferingSourceCount =>
            Payments.Select(payment => payment.SourceId).Distinct(StringComparer.Ordinal).Count();

        /// <summary>
        /// The prompt for a standing decision, or <c>null</c> when none is standing.
        ///
        /// <para>The options are the query's own, already narrowed to what their controller can afford
        /// and to the hero this decision belongs to, so nothing here decides who may pay for what: this
        /// composes a command per affordable card and adds the decline the rules always allow.</para>
        /// </summary>
        public static RemovalPreventionPrompt Create(
            PendingRemovalPreventionDecision decision,
            IReadOnlyList<RemovalPreventionOption> options)
        {
            if (decision == null)
            {
                return null;
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var payments = options
                .Where(option => option.DecidingHeroId.Equals(decision.DecidingHeroId))
                .OrderBy(option => option.SourceId, StringComparer.Ordinal)
                .SelectMany(option => option.PayableCards
                    .OrderBy(card => card.Value, StringComparer.Ordinal)
                    .Select(card => new RemovalPreventionPayment(
                        option.SourceId,
                        option.Cost,
                        card,
                        RemovalPreventionCommand.Accept(
                            decision.RequestId, decision.DecidingHeroId, option.SourceId, card))))
                .ToArray();

            return new RemovalPreventionPrompt(
                decision.RequestId,
                decision.DecidingHeroId,
                decision.SubjectMinionId,
                decision.Origin,
                new ReadOnlyCollection<RemovalPreventionPayment>(payments),
                RemovalPreventionCommand.Decline(decision.RequestId, decision.DecidingHeroId));
        }

        /// <summary>
        /// Rehydrates the already-derived owner-scoped projection received from an admitted peer.
        /// It creates no option: every payment is supplied with the exact typed invocation the
        /// ordinary factory above would mint, and the caller has already validated the bounded DTO.
        /// </summary>
        public static RemovalPreventionPrompt FromProjection(
            string requestId,
            HeroId decidingHeroId,
            MinionId subjectMinionId,
            RemovalPreventionOrigin origin,
            IEnumerable<RemovalPreventionPayment> payments)
        {
            if (string.IsNullOrWhiteSpace(requestId) || !decidingHeroId.IsDefined ||
                !subjectMinionId.IsDefined)
            {
                throw new ArgumentException("A removal-prevention projection requires its stable identities.");
            }

            var materialized = (payments ?? throw new ArgumentNullException(nameof(payments))).ToArray();
            if (materialized.Any(value => value == null ||
                !value.Invocation.DecidingHeroId.Equals(decidingHeroId) ||
                !string.Equals(value.Invocation.RequestId, requestId, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Every projected payment must answer this prompt.", nameof(payments));
            }

            return new RemovalPreventionPrompt(
                requestId, decidingHeroId, subjectMinionId, origin,
                new ReadOnlyCollection<RemovalPreventionPayment>(materialized),
                RemovalPreventionCommand.Decline(requestId, decidingHeroId));
        }
    }
}

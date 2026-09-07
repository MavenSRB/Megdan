using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.35z46. Where one instance of a repeated Attack stands in the chain its
    /// own card printed, and which printed clause that position belongs to.
    ///
    /// <para><b>The problem this exists for, stated as milestone 8.35z30 measured it.</b> A repeated
    /// Attack learns which unit kind it may take by reading its own card's declaration - milestone
    /// 8.24 chose that deliberately, so the clause moves no snapshot field and a tampered payload
    /// cannot widen the set back. But it asks the CARD, not the INSTANCE, and Silverarrow's Rain of
    /// Arrows is the first face in the roster to print TWO repeat clauses with different narrowings.
    /// Under the old shape composing the second clause would have narrowed BOTH repeats to one kind,
    /// which is a rule neither clause prints.</para>
    ///
    /// <para><b>And a second wall behind it, which the costed design missed and a probe found.</b>
    /// Rulebook p.10's "You cannot repeat a repeated action!" is enforced for card text by milestone
    /// 8.25d as a single boolean on the resolution context: a <see cref="EffectNodeKind.RepeatAction"/>
    /// node reached inside a repeated Attack is skipped in silence, whichever clause it is. That
    /// boolean is a SUBSTITUTE COUNTER - card text is always started with
    /// <see cref="RepeatContinuation.FirstInstance"/>, so <c>RepeatedActionQuery.CanRepeat</c> reads
    /// zero completed repeats in every instance and cannot bound a chain by itself. Measured, not
    /// assumed: running Rain of Arrows' own card text with that flag set records
    /// <c>DeclinedOptional</c> against its repeat node and requests nothing.</para>
    ///
    /// <para><b>The rule this states, and it degenerates to the old one.</b> A card that prints a chain
    /// of repeat clauses ADVANCES along it: the instance standing at chain position <c>d</c> may ask
    /// only the clause printed at position <c>d</c>, and p.10's bar is what stops it asking an earlier
    /// one again. A card printing ONE clause therefore behaves exactly as it did before this
    /// milestone - position 0 asks clause 0, and every repeated instance is barred - which is every
    /// face in this build but Rain of Arrows.</para>
    ///
    /// <para><b>Why the position is read from the identity rather than carried.</b> The chain mints its
    /// next identity by appending <see cref="RepeatSuffix"/> to the completing instance's own, and
    /// milestone 8.25d records that minting as what makes each instance's identity unique - p.13's
    /// Defeat outcome pays once per identity, so two instances may not share one. That suffix is
    /// therefore already the chain's own counter, already persisted (it is the resolution's identity),
    /// and already restored. Reading it costs no schema move, and it is what makes the narrowing
    /// survive a restore: <c>AttackDefenseCardActionStrategyBackend</c> REBUILDS a restored target
    /// choice from the restored resolution alone and compares it against the persisted one, so a
    /// clause identity that lived anywhere but in restorable state would throw on the way back.</para>
    ///
    /// <para>The mint and the read are stated here together, as one pair, for the reason milestone
    /// 8.35z15 extracted the repeat exclusion: two halves of one convention that can drift apart are a
    /// defect waiting for a caller.</para>
    /// </summary>
    internal static class CardTextRepeatChain
    {
        /// <summary>
        /// What one card-text repeat appends to the completing instance's identity. Milestone 8.10
        /// chose the value and milestone 8.25d documented why it is unique; this milestone gives it a
        /// name so the reader below cannot disagree with the writer.
        /// </summary>
        internal const string RepeatSuffix = ":repeat";

        /// <summary>
        /// The identity a card-text repeat's next instance runs under.
        ///
        /// <para>Byte for byte what <c>CardActionResolutionAuthority</c> minted before this milestone,
        /// which is what keeps every existing snapshot, replay and pinned digest exactly where it
        /// was.</para>
        /// </summary>
        internal static ActionResolutionId Mint(ActionResolutionId completed) =>
            new ActionResolutionId(completed.Value + RepeatSuffix);

        /// <summary>
        /// How many card-text repeats stand between this instance and the printed one: 0 for a printed
        /// instance, 1 for the instance its card text asked for, and so on.
        ///
        /// <para>The other two identities that reach a repeated Attack cannot be confused with this
        /// one. Milestone 8.23's after-action path mints a <c>after-action-repeat:</c> PREFIX and ends
        /// with an action definition id; milestone 8.35z27's modal sequences end with <c>:seq-N</c>.
        /// Only <see cref="Mint"/> produces this suffix, and only at the end.</para>
        /// </summary>
        internal static int DepthOf(ActionResolutionId resolutionId)
        {
            var value = resolutionId.Value ?? string.Empty;
            var depth = 0;
            while (value.Length > RepeatSuffix.Length &&
                   value.EndsWith(RepeatSuffix, StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - RepeatSuffix.Length);
                depth++;
            }

            return depth;
        }

        /// <summary>
        /// The repeat clauses one action prints, in printed reading order.
        ///
        /// <para>Both of rulebook p.14's card-text moments are walked, in the page's own order, because
        /// a face may print its repeat at either and this build has faces at both. Within one root the
        /// definition's own node order is the printed order, which is the same assumption
        /// <c>RepeatedActionQuery.Requested</c> already makes when it takes the first applied repeat
        /// node it meets.</para>
        /// </summary>
        internal static IReadOnlyList<EffectNodeDefinition> ClausesOf(ActionDefinition action)
        {
            if (action == null)
            {
                return Array.Empty<EffectNodeDefinition>();
            }

            return ClausesIn(action.EffectRoot)
                .Concat(ClausesIn(action.SecondCardTextRoot))
                .ToArray();
        }

        /// <summary>
        /// Whether this action prints more than one repeat clause - the one condition under which
        /// anything in this type changes an answer.
        ///
        /// <para>It is asked at every site that gates on the chain so that a single-clause card keeps
        /// the exact walk it had before this milestone, rather than a new one that happens to agree.
        /// </para>
        /// </summary>
        internal static bool PrintsAChain(ActionDefinition action) => ClausesOf(action).Count > 1;

        /// <summary>
        /// Whether an instance at this chain position may ask the clause declaring this binding.
        ///
        /// <para>True for every card that prints one clause, whose repeats p.10's own bar governs and
        /// which this milestone does not touch. For a chain, the position and the clause's printed
        /// index must agree: the printed instance owns the first clause, the instance that clause
        /// produced owns the second, and a chain longer than the card is printed for simply runs out of
        /// clauses and stops.</para>
        /// </summary>
        internal static bool ClauseIsReachable(
            ActionDefinition action, ActionResolutionId resolutionId, RepeatTargetBinding binding)
        {
            var clauses = ClausesOf(action);
            if (clauses.Count <= 1)
            {
                return true;
            }

            var depth = DepthOf(resolutionId);
            return depth < clauses.Count && clauses[depth].RepeatTargetBinding == binding;
        }

        /// <summary>
        /// The clause a repeated instance at this position came from, or <c>null</c> where the card
        /// prints no chain and the caller's own single-clause derivation still answers.
        /// </summary>
        internal static EffectNodeDefinition ProducingClause(
            ActionDefinition action, ActionResolutionId resolutionId)
        {
            var clauses = ClausesOf(action);
            if (clauses.Count <= 1)
            {
                return null;
            }

            var depth = DepthOf(resolutionId);
            return depth >= 1 && depth <= clauses.Count ? clauses[depth - 1] : null;
        }

        private static IEnumerable<EffectNodeDefinition> ClausesIn(EffectDefinition effect) =>
            effect == null
                ? Enumerable.Empty<EffectNodeDefinition>()
                : effect.Nodes.Where(node => node.Kind == EffectNodeKind.RepeatAction);
    }
}

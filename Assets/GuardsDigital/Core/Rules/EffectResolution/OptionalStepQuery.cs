using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.46a. Whether a node's own declaration is one that opens rulebook p.10's
    /// confirm-or-decline pause - the question a player answers with "Move it again" or "Stop here".
    ///
    /// <para><b>Why this exists.</b> Two places in <see cref="EffectResolutionSession"/> build that
    /// pause: the <c>Optional</c> arm and the <c>RepeatAction</c> arm. Milestone 8.46a's content check
    /// has to know exactly which nodes need an authored consequence label, and a second list of node
    /// kinds written beside the resolver would be a copy that drifts the first time a kind is added or
    /// a policy changes. So the set is named once, here, and both session arms ask it. A node kind
    /// that starts or stops asking changes this predicate, and the coverage check changes with it in
    /// the same edit rather than a milestone later.</para>
    ///
    /// <para><b>What it deliberately does NOT answer.</b> Whether the pause is actually reached in a
    /// given match. The session still refuses to ask a question whose branch can do nothing
    /// (milestone 8.29e) and still skips a repeat that has run its declared maximum, has nothing
    /// repeatable, or could not be performed from the board as it stands (milestones 6.7, 8.23 and
    /// 8.25e). Those are board facts, and a label is authored against the printed text rather than
    /// against a board - so the requirement belongs to the declaration, and a node that CAN ask needs
    /// words even on the turns it stays quiet.</para>
    /// </summary>
    public static class OptionalStepQuery
    {
        /// <summary>
        /// True when this node, by its own kind and failure policy, is one the resolution session
        /// stops on with a confirm-or-decline choice.
        ///
        /// <para><c>Optional</c> asks unless it is `if able`, which never asks: it attempts, and a
        /// refusal is simply skipped. That admits `may` and `up to` alike, which is what the session
        /// has always done and what <c>EffectNodeDefinition</c>'s own shape check assumes when it
        /// requires a chooser on both and forbids one on `if able`. <c>RepeatAction</c> asks only on
        /// `may`; an `if able` repeat requests without asking, and a mandatory one is refused at
        /// construction because an unbounded repeat has no bound.</para>
        /// </summary>
        public static bool OpensConfirmOrDecline(EffectNodeDefinition node)
        {
            if (node == null)
            {
                return false;
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Optional:
                    return node.FailurePolicy != EffectFailurePolicy.IfAble;
                case EffectNodeKind.RepeatAction:
                    return node.FailurePolicy == EffectFailurePolicy.May;
                default:
                    return false;
            }
        }
    }
}

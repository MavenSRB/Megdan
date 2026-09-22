using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Chaos Incarnate, Ignatia the Mad's p.16 permanent ability - milestone 8.75a. "The first time
    /// each turn after you perform a primary action you may flip the Tie Breaker coin; if you do, you
    /// may perform that action again, choosing different targets."
    ///
    /// <para><b>Three of the sentence's four clauses are declarations rather than nodes.</b> "The
    /// first time each turn after you perform a primary action" is
    /// <see cref="UltimateTriggerCondition.AfterControllerPerformsPrimaryAction"/> - Cutter's Legend
    /// of the Skies minted it at milestone 8.39f1 precisely because every one of the five conditions
    /// before it is matched against a fact that is undefined unless the card is p.9-Basic, and most
    /// of what Ignatia plays when her Tier IV is unlocked is not. The once-per-turn bound is that
    /// condition's own, riding the turn's existing consumed-key set
    /// (<c>UltimateTriggeredEffectSourceQuery.OncePerTurnKeyFor</c>, prefix
    /// <c>after-primary-action:</c>). And "choosing different targets" is
    /// <see cref="RepeatTargetBinding.DifferentTarget"/>, the member milestone 8.10 minted for
    /// Thunder Boomerang.</para>
    ///
    /// <para><b>The fourth clause is what the body is, and its shape is the sentence's semicolon.</b>
    /// "you may flip the Tie Breaker coin" is an <see cref="EffectNodeKind.Optional"/> on
    /// <see cref="EffectFailurePolicy.May"/>; "IF YOU DO, you may perform that action again" is the
    /// repeat living INSIDE that optional's one child rather than beside it. A declined flip
    /// therefore reaches no repeat at all, which is exactly what the conditional clause says, and a
    /// confirmed flip that then declines the repeat leaves the coin turned over - the text offers the
    /// flip on its own terms and never takes one back.</para>
    ///
    /// <para><b>The order inside the child is load-bearing.</b> The flip runs first and the repeat
    /// second, because her whole set is composed as a Conditional on the revealed coin face
    /// (<c>ignatia-coin-sun@1</c>) - so the repeat runs the OPPOSITE of the two printed lines the
    /// first instance ran, and whether it has a legal subject at all is a question about the new
    /// face. Milestone 8.75a is what makes that ordering true rather than hoped for: the resolver
    /// stops on the published flip, the authority holding the coin turns it over, and the frame
    /// carries on with the new face in hand.</para>
    ///
    /// <para><b>What "choosing different targets" means for each kind she can repeat, ID-Q6a.</b>
    /// <see cref="RepeatTargetBinding.DifferentTarget"/> is declared once, on the node, exactly as
    /// Tigerclaw's Cloak and Daggers declares it - and his ability repeats p.9's whole basic
    /// supertype, so a basic SKILL of his already reaches this member today. The exclusion is applied
    /// by <c>CardActionResolutionAuthority.ExclusionFor</c>, which carries it for
    /// <see cref="ActionKind.Attack"/> and for no other kind, because an Attack's target is the one
    /// subject this build carries across an action boundary. For a repeated Skill, p.10's own default
    /// stands - "the repeated instance chooses for itself" - and the sentence's promise is kept by
    /// the coin rather than by an exclusion: the repeat runs her OTHER printed line, which names a
    /// different set of subjects. <see cref="RepeatTargetBinding.DifferentSelectionSubject"/> is
    /// deliberately NOT used: it excludes a selection node's published subject, which is captured
    /// onto the frame the selection ran in, and an after-action repeat mints a fresh frame
    /// (<c>PendingRepeatedAction.Mint(..., RepeatContinuation.Next(1))</c>) with nothing captured on
    /// it. Declaring it here would be claiming an exclusion the engine cannot apply.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        /// <summary>The Ultimate's rule identity, named here because the card content references it.</summary>
        public const string ChaosIncarnateRuleId = "ignatia-chaos-incarnate";

        /// <summary>The rule KIND string a composition root matches her Ultimate by.</summary>
        public const string ChaosIncarnateRuleKind = "IgnatiaChaosIncarnate";

        /// <summary>"you may flip the Tie Breaker coin" - the whole ability's one offer.</summary>
        public const string ChaosIncarnateFlipNode = "chaos-incarnate-flip";

        /// <summary>The flip and the repeat, in the order the semicolon prints them.</summary>
        public const string ChaosIncarnateThenNode = "chaos-incarnate-then";

        /// <summary>The capability that asks the match to turn the coin over.</summary>
        public const string ChaosIncarnateCoinNode = "chaos-incarnate-coin";

        /// <summary>"you may perform that action again, choosing different targets".</summary>
        public const string ChaosIncarnateRepeatNode = "chaos-incarnate-repeat";

        public static EffectDefinition ChaosIncarnateTriggeredEffect(string contentRevision)
        {
            var flip = new EffectNodeId(ChaosIncarnateFlipNode);
            var then = new EffectNodeId(ChaosIncarnateThenNode);
            var coin = new EffectNodeId(ChaosIncarnateCoinNode);
            var repeat = new EffectNodeId(ChaosIncarnateRepeatNode);

            return new EffectDefinition(
                new EffectDefinitionId(
                    IgnatiaCardContent.ChaosIncarnate.Value + "-ultimate"),
                contentRevision,
                flip,
                new[]
                {
                    // "you MAY flip" - p.10 gives an unassigned decision to the card's controller,
                    // and this one is unassigned.
                    new EffectNodeDefinition(
                        flip, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { then },
                        chooser: EffectChooserRole.Actor),

                    // "; if you do, ..." - everything after the semicolon is governed by the offer
                    // above, so it is the offer's one child rather than a second child of a root.
                    new EffectNodeDefinition(
                        then, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { coin, repeat }),

                    // Taking the offer flips the coin. There is no second question here: p.5 gives
                    // the coin two faces and the match always has one showing, so the step can
                    // always be taken.
                    new EffectNodeDefinition(
                        coin, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        IgnatiaComponentContent.FlipTieBreakerV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                GuardsDigital.Core.Rules.EffectResolution.TieBreakerFlipQuery
                                    .FlippedOutput,
                                EffectValueKind.Boolean)
                        }),

                    // "you may perform that action again, choosing different targets." Nothing is
                    // performed here - the request is the whole of what confirming does, and the
                    // second instance is created by the card action once this one's boundary is
                    // clean, which is milestone 6.7's division and Tigerclaw's own node.
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }

        /// <summary>
        /// Every triggered effect Ignatia's content declares, for a fixture or a composition root that
        /// configures them. Hers is exactly one: p.16's permanent ability. Registration itself is the
        /// dealing block's - milestone 7.9's gate keeps every declaration of hers out of
        /// <c>MatchContentCatalogs.ForThisBuild</c> until she is dealt.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects(string contentRevision) =>
            Array.AsReadOnly(new[] { ChaosIncarnateTriggeredEffect(contentRevision) });

        /// <summary>
        /// The two controls Chaos Incarnate raises, and the words each one owes - milestone 8.46a.
        /// Both are p.10 confirm-or-decline pauses on the acting hero's own seat, and they open in
        /// printed order: the flip first, the repeat only if the flip was taken.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> UltimateDecisionLabels()
        {
            yield return DecisionStep(
                IgnatiaCardContent.ChaosIncarnate, ChaosIncarnateFlipNode,
                "Flip the Tie Breaker coin", "Leave the coin");
            yield return DecisionStep(
                IgnatiaCardContent.ChaosIncarnate, ChaosIncarnateRepeatNode,
                "Perform that action again", "Stop here");
        }
    }
}

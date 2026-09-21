using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's The Ultimate Trick - his Purple Tier IV, and the one face milestone
    /// 8.71b composes.
    ///
    /// <para><b>The sentence.</b> "You choose the next action, and how it is performed for a hero
    /// you target with the 'Hurry Up!'." (The Silver's own title prints "Hurry up!"; it is the same
    /// card.) It prints no Initiative, no Range, no Radius and no ribbon values at all, so there is
    /// nothing else on the face to compose.</para>
    ///
    /// <para><b>It runs no effect, and that is the measured result of this block rather than an
    /// omission.</b> Every other shape <c>UltimateRuleDefinition</c> offers pairs a moment with a
    /// body to run at it - <c>TriggeredAbility</c>, <c>AfterMarkerGivenAbility</c>,
    /// <c>BeforeActionAbility</c>, <c>AfterPushAbility</c> - and each of those was considered and
    /// costed. What this sentence changes is not the board: it changes WHO ANSWERS, for a window
    /// that opens on a rewrite and closes when an action ends. An effect body can only write state,
    /// and state written there would be a second copy of a fact the rewrite already holds - one
    /// that would then need its own snapshot section, its own wire field, its own expiry at four
    /// exits, and its own agreement with the rewrite at every restore, resync and rewind.</para>
    ///
    /// <para><b>So it is <c>UltimateRuleDefinition.PermanentDeclaration</c></b>: Ursafar's Unbound
    /// Fury shape, a standing declaration other rules read by name. The reader is
    /// <c>DelegatedActionQuery</c>, the rule kind is
    /// <see cref="GuardsDigital.Core.Rules.CardResolution.DelegatedActionQuery.DelegatingRuleKind"/>,
    /// and the hero it names is the one whose card that Ultimate's own controller rewrote - which
    /// is exactly "a hero you target with the Hurry Up!", because nothing else in this build
    /// rewrites a printed Initiative. p.16 gives the ability its lifetime ("as long as your hero is
    /// on the board") and p.10 gives the delegation its own, at the rewritten card's next state
    /// change; neither is stored, so both survive a restore by being re-read.</para>
    ///
    /// <para><b>What "the next action" covers is HA-Q3, settled by the user on 2026-09-21.</b>
    /// Hanu's seat chooses among exactly the offers that hero's seat would have had - the card's
    /// primary, its secondary, p.12's Hold and p.11's Fast Travel and p.13's Clear replacements -
    /// and answers every choice inside that action. The hero's own Defense responses to other
    /// heroes' attacks, their p.13 respawn choice and their end-of-round choices are not "the
    /// action" and stay theirs. It applies to any hero Hanu targets, friend or enemy. The list of
    /// decision families that implements that sentence is
    /// <c>SeatProjectionFactory.DelegatedDecisionFamilies</c>, stated beside the families it
    /// deliberately leaves alone.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        /// <summary>p.16's permanent ability, identified for the continuous source it becomes.</summary>
        public const string TheUltimateTrickRuleId = "hanu-the-ultimate-trick";

        /// <summary>
        /// The rule kind the delegation matches on. It is spelled here and in
        /// <c>DelegatedActionQuery</c>, and <c>HanuUltimateTrickTests</c> asserts the two equal -
        /// the discipline <c>BorrowedPrimaryAction.ResolvedCardsCountAsActiveRuleKind</c> already
        /// follows for the other standing declaration in this build that a rule reads by name.
        /// </summary>
        public const string TheUltimateTrickRuleKind =
            GuardsDigital.Core.Rules.CardResolution.DelegatedActionQuery.DelegatingRuleKind;

        /// <summary>
        /// NONE, and derived rather than asserted.
        ///
        /// <para>The Ultimate Trick raises no prompt of its own at all: it runs no effect, so
        /// <c>OptionalStepQuery</c> has no step of its own to find, and every control a delegated
        /// action draws is the DELEGATED CARD's own control shown to a different seat - which is
        /// that card's label to declare and not this one's. <c>HanuUltimateTrickTests</c> asks
        /// <see cref="DecisionLabelRequirementQuery"/> which controls this face raises and holds
        /// this list against it, so a later composition that starts asking something cannot arrive
        /// wordless.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> UltimateDecisionLabels() =>
            Array.Empty<DecisionLabelDeclaration>();
    }
}

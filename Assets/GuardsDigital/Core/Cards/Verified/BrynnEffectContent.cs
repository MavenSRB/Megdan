using System.Collections.Generic;
using System.Linq;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's effect content as ONE set - milestone 8.69f, and the whole of it is the
    /// decision-label aggregate his five composition blocks could not declare between them.
    ///
    /// <para><b>NINETEEN controls, not the nine 8.69c wrote.</b> That block declared Decoy's - one
    /// optional step and two modal arms for each of its three performances - beside the body that
    /// raises them, and named the rest as the aggregate's business. The rest is ten: Familiar Ground's
    /// two p.10 bullets, the two bullets each of Bear Trap, Log Trap and Deadfall Trap prints, Split
    /// Attack's "may repeat once" and Peak Precision's "you may retrieve this card". They are declared
    /// HERE rather than in those blocks' partials because none of them existed as a set until the set
    /// did, and because what made them findable is a derivation over the set:
    /// <c>BrynnPrintedSetTests</c> asks
    /// <see cref="GuardsDigital.Core.Cards.DecisionLabelRequirementQuery"/> which controls his DEALT
    /// faces raise - the resolver's own rule rather than a second list - and holds this aggregate
    /// against it, so a face that starts asking something cannot arrive wordless.</para>
    ///
    /// <para><b>What deliberately declares nothing.</b> The guides' "You may move a friendly unit",
    /// the swaps' trailing "You may move 1 space", the grit's "You may retrieve a discarded attack
    /// card" and Split Throw's repeat raise no confirm-or-decline at all:
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> is what decides, and a
    /// declinable SELECTION prompt names the unit or the card it would pick while an <c>IfAble</c> step
    /// never asks. Silverarrow's whole set declares nothing for the same reason.</para>
    ///
    /// <para><b>It is not concatenated into <see cref="DecisionLabelCatalog"/> here.</b> Every hero's
    /// contribution joins that catalog at the milestone that DEALS them - Rowenna's fifteen at 8.57g,
    /// Min's eleven at 8.61j - on the rule that file states: what a table is FOR is a window over a
    /// card a match deals. 8.69g is the block that adds the one line.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        /// <summary>
        /// Every decision label his set declares, for the catalog line 8.69g adds and for the coverage
        /// case that measures it against the controls his faces raise.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels() =>
            GuidesDecisionLabels().Concat(SetDecisionLabels());

        /// <summary>
        /// The ten controls no single composition block could see, in his shard's inventory order.
        ///
        /// <para>Each says what its own printed clause does, in that clause's own words. "beside 3
        /// obstacles" is the phrase 8.69c chose for Decoy's bullets and it is kept here, because the
        /// fifteen faces that print "adjacent to 3 or more obstacles" print one sentence and a window
        /// that spelled it two ways would be describing two.</para>
        /// </summary>
        private static IEnumerable<DecisionLabelDeclaration> SetDecisionLabels()
        {
            // "Choose one - • Target a unit adjacent to you. • Target a hero in range who is adjacent
            // to 3 or more obstacles." Two arms of one Choose, so each names only what taking it does:
            // p.10's modal has no decline of its own.
            yield return DecisionArm(
                BrynnCardContent.FamiliarGround, FamiliarGroundAdjacentBulletNode,
                "Target a unit adjacent to you");
            yield return DecisionArm(
                BrynnCardContent.FamiliarGround, FamiliarGroundObstacleBulletNode,
                "Target a hero in range beside 3 obstacles");

            // "If you target a hero who is adjacent to 3 or more obstacles, MAY repeat once on a
            // different unit adjacent to you" - the one repeat in his set that prints the word, and
            // therefore the one that stops to ask. Split Throw's prints none and is `if able`.
            yield return DecisionStep(
                BrynnCardContent.SplitAttack, SplitRepeatNode,
                "Repeat on a different unit adjacent to you", "Do not repeat");

            // "After the attack: You may retrieve this card."
            yield return DecisionStep(
                BrynnCardContent.PeakPrecision, PeakPrecisionRetrievalStepNode,
                "Take this card back into your hand", "Leave it played");

            // The three traps' two printed bullets. Bear Trap and Log Trap print one sentence at two
            // Radii, which is why their words are the same words; Deadfall Trap prints the escalation.
            foreach (var trap in new[] { BrynnCardContent.BearTrap, BrynnCardContent.LogTrap })
            {
                yield return DecisionArm(trap, TrapAdjacentArmNode,
                    "An enemy hero adjacent to you discards a card");
                yield return DecisionArm(trap, TrapRadiusArmNode,
                    "An enemy hero in radius beside 3 obstacles discards a card");
            }

            // MILESTONE 8.69g CUT BOTH OF THESE TO THE SIXTY-CHARACTER BOUND, which is a fact about
            // the control the words are drawn in rather than about the card: `DecisionLabelCoverage-
            // Tests` measures every label in the one catalog, and his table only joined that catalog
            // at the dealing. Printed at the length Bear Trap's and Log Trap's are - "An enemy hero
            // adjacent to you discards a card, or is defeated" and "An enemy hero in radius beside 3
            // obstacles discards, or is defeated" - they read 61 and 67. The escalation is what the
            // two of them say and neither had room for, so the subject is shortened and the
            // escalation kept whole: that is the half of the sentence a player is choosing.
            yield return DecisionArm(BrynnCardContent.DeadfallTrap, TrapAdjacentArmNode,
                "An adjacent enemy hero discards a card, or is defeated");
            yield return DecisionArm(BrynnCardContent.DeadfallTrap, TrapRadiusArmNode,
                "A boxed-in enemy hero in radius discards, or is defeated");
        }

        /// <summary>
        /// Split Attack's repeat, as the node id its own body mints. It is spelled here rather than
        /// exposed from that partial because a label is a fact about a window and not about the
        /// composition, and <c>BrynnPrintedSetTests</c> proves the two agree by deriving the required
        /// set from the definitions themselves.
        /// </summary>
        private const string SplitRepeatNode = "brynn-split-repeat";

        /// <summary>Peak Precision's "you may", the node that opens p.10's confirm-or-decline.</summary>
        private const string PeakPrecisionRetrievalStepNode = "brynn-split-retrieve-may";
    }
}

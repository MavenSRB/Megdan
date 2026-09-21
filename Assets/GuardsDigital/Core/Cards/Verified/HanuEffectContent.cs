using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's effect content as ONE set - milestone 8.71g: the decision-label aggregate
    /// his six composition blocks could not declare between them, and the two conventional effect
    /// identities four of those blocks each spelled for themselves.
    ///
    /// <para><b>FIFTEEN controls, and every one of them was already written.</b> 8.71c declared ten -
    /// the two printed bullets of each of his five graded Reds - and 8.71e five, one per printed
    /// distance across This Way! and That Way!. The other four blocks declared none, and this milestone
    /// DERIVED the requirement over all eighteen dealt faces and the three end-of-turn bodies his Blue
    /// journeys arm, with <see cref="GuardsDigital.Core.Cards.DecisionLabelRequirementQuery"/> - the
    /// resolver's own pause rule rather than a second list - and found the two block lists together to
    /// be exactly it. That is a result rather than a formality: the same derivation on Brynn the Seeker
    /// at 8.69f found TEN of nineteen labels missing, because the blocks had declared them by guess.
    /// Hanu's blocks each derived their own half, which is why there was nothing left over.</para>
    ///
    /// <para><b>What deliberately declares nothing.</b> Fight and Flight's straight line, the journeys'
    /// swaps and their end-of-turn swap back, Safe Travels' and the two Tier III Greens' trailing "Move
    /// up to 1 space", the Greens' friendly pair and Hurry up!'s rewrite raise no confirm-or-decline at
    /// all: <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> is what decides,
    /// and a declinable SELECTION prompt names the unit, the card or the space it would pick while an
    /// <c>IfAble</c> or an <c>UpTo</c> step never asks. Silverarrow's whole set and Brynn's swaps
    /// declare nothing for the same reason. The Ultimate Trick declares nothing because it prints no
    /// action at all - what it changes is who answers somebody else's controls.</para>
    ///
    /// <para><b>It is not concatenated into <see cref="DecisionLabelCatalog"/> here.</b> Every hero's
    /// contribution joins that catalog at the milestone that DEALS them - Rowenna's fifteen at 8.57g,
    /// Min's eleven at 8.61j, Brynn's nineteen at 8.69g - on the rule that file states: what a table is
    /// FOR is a window over a card a match deals. 8.71h is the block that adds the one line.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        /// <summary>
        /// Every decision label his set declares, for the catalog line 8.71h adds and for the coverage
        /// case that measures it against the controls his faces raise.
        ///
        /// <para>The four blocks that declare none contribute empty lists rather than being left out,
        /// so a later block of his that starts asking something finds the shape already here and the
        /// aggregate never has to learn a new member name.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels() =>
            HurryUpDecisionLabels()
                .Concat(UltimateDecisionLabels())
                .Concat(RedDecisionLabels())
                .Concat(JourneyDecisionLabels())
                .Concat(CoMoveDecisionLabels())
                .Concat(GreenDecisionLabels());

        // -----------------------------------------------------------------------------------------
        // The conventional effect identities, declared once for his whole set.
        //
        // Four of his six blocks needed one of these and each had to spell its own, because five
        // partials of one class were being written at once and a member declared twice is a compile
        // error rather than a merge: 8.71c's `AttackTextEffectId`, 8.71d's `JourneySkillEffectId`,
        // 8.71e's `CoMoveEffectId`, 8.71f's `GreenSkillEffectId` and a bare literal inside 8.71a's
        // Hurry up!. FOUR of those five minted the identical string, so they fold into ONE
        // `SkillEffectId` here and the Attack one moves beside it.
        //
        // MILESTONE 8.71g DROPPED `MovementTextEffectId`, which Rowenna's set carries. p.11 suppresses
        // every Movement ribbon's own card text on every face of his - the sentence belongs to the
        // primary on all nineteen - so no composed body of his was ever named by it, exactly as 8.69f
        // recorded for Brynn.
        // -----------------------------------------------------------------------------------------

        /// <summary>The conventional effect identity for one face's Skill card text.</summary>
        public static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        /// <summary>The conventional effect identity for one face's Attack card text.</summary>
        public static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

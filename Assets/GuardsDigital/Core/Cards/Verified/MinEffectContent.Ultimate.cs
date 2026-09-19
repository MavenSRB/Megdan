using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's Purple Tier IV, Flurry of Blows - milestone 8.61h.
    ///
    /// <para>"Each time after you perform an attack action you may repeat it once on a different
    /// target."</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>The permanent ability's stable rule id, which its source identity is minted from.</summary>
        public const string FlurryOfBlowsRuleId = "min-flurry-of-blows";

        /// <summary>
        /// p.16's permanent ability as a typed triggered rule: after every attack action of hers, an
        /// offer to repeat it once on a different target.
        ///
        /// <para><b>Tigerclaw's Cloak and Daggers is the shape, and the two differ on the trigger
        /// alone.</b> His reads "After you perform a basic action"; hers reads "after you perform an
        /// attack action". Everything below the trigger is identical - one
        /// <see cref="EffectNodeKind.RepeatAction"/> node at
        /// <see cref="TimingCheckpoint.ActionEnded"/>, may, at most once, on a different target - so
        /// the difference is expressed as
        /// <see cref="UltimateTriggerCondition.AfterControllerPerformsAttackAction"/> and nowhere
        /// else.</para>
        ///
        /// <para><b>Why the new condition was needed at all.</b> p.9 makes the basic supertype a fact
        /// about the CARD, so the fact Tigerclaw's condition is matched against is undefined for every
        /// coloured card - and five of Min's six Attack faces are her Red Stances. See that member for
        /// the whole of it.</para>
        ///
        /// <para><b>The card prints no Initiative, no icons and no action</b>, so it is never played,
        /// never discarded to defend, and the whole of its content is this.</para>
        /// </summary>
        public static UltimateRuleDefinition FlurryOfBlowsRule(string contentRevision) =>
            UltimateRuleDefinition.TriggeredAbility(
                FlurryOfBlowsRuleId,
                "MinFlurryOfBlows",
                FlurryOfBlowsTriggeredEffect(contentRevision).Reference,
                UltimateTriggerCondition.AfterControllerPerformsAttackAction,
                new[] { TimingCheckpoint.ActionEnded },
                System.Array.Empty<PrintedValueDefinition>());

        /// <summary>
        /// "you may repeat it once on a different target" - the whole of what the ability performs.
        ///
        /// <para><b>One node, and everything the sentence says is on it.</b> "you may" is the failure
        /// policy and the chooser; "once" is the declared maximum; "on a different target" is
        /// <see cref="RepeatTargetBinding.DifferentTarget"/>, which milestone 8.10 minted for Thunder
        /// Boomerang and which applies to an Attack alone because no other action kind the page
        /// repeats has a target to exclude - and this trigger fires for no other kind.</para>
        ///
        /// <para><b>What is not here is everything about the moment.</b> Which action is repeated,
        /// that p.10 forbids repeating a repeat, and that the repeated instance itself fires this
        /// trigger again and finds nothing to offer are all facts carried by the trigger's own
        /// occurrence and by the subject the card-action authority holds. The definition says what the
        /// card says and nothing about how the engine finds it - which is the division Tigerclaw's
        /// identical node already records.</para>
        /// </summary>
        public static EffectDefinition FlurryOfBlowsTriggeredEffect(string contentRevision)
        {
            var repeat = new EffectNodeId("flurry-of-blows-repeat");
            return new EffectDefinition(
                new EffectDefinitionId("min-flurry-of-blows-effect"),
                contentRevision,
                repeat,
                new[]
                {
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }
    }
}

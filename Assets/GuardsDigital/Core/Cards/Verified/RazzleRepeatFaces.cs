using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21h. Razzle's two repeats performed by a different body.
    ///
    /// <list type="bullet">
    /// <item><description><b>Twin Strike</b> (Purple, p.16 Ultimate): "After you perform a basic attack
    /// action a different you may repeat it once, targeting a different unit." RZ-Q11 (A): after Stunt
    /// Doubles' (or the boxed Stunted Doubles') Basic Attack he may pick another standing body, and that
    /// body performs the whole action again - adjacent to itself, against a different unit, its
    /// After-the-attack spawn included and bounded by the supply of four (RZ-Q1).</description></item>
    /// <item><description><b>Spectacle</b> (Green III, Skill - Ranged 2): "Swap with a minion in range.
    /// May repeat once by a different one of you, targeting a different minion." RZ-Q12 (A): Theatrics'
    /// Swap, then a repeat whose Range 2 is measured from the second body, against a minion other than
    /// the first swap's.</description></item>
    /// </list>
    ///
    /// <para><b>One new shape for both, and it is not a capability.</b> "A different you / a different
    /// one of you" is who performs the repeated instance - a declaration on p.10's RepeatAction node
    /// (<see cref="RepeatActor.DifferentBody"/>, declared by the node publishing the performing body's
    /// space). The node asks which of his other bodies performs it, among those from which the repeat
    /// can be performed, and the body named becomes the acting body before the repeated instance starts,
    /// so every read of that instance is measured from it. "Targeting a different unit / minion" is the
    /// existing <see cref="RepeatTargetBinding.DifferentTarget"/> and
    /// <see cref="RepeatTargetBinding.DifferentSelectionSubject"/>, unchanged; a many-bodied first
    /// target is excluded at hero grain, every body of him (9.21a, 9.21c).</para>
    ///
    /// <para>Nothing here is registered until 9.21j deals him.</para>
    /// </summary>
    public static partial class RazzleCardContent
    {
        public static CardDefinitionId Spectacle { get; } = Id("spectacle");
        public static CardDefinitionId TwinStrike { get; } = Id("twin-strike");

        /// <summary>
        /// 9.21h's block list (read by the corpus sweeps until 9.21j mints the aggregate): Spectacle and
        /// the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinition> RepeatDefinitions =>
            new[] { SpectacleDefinition, TwinStrikeDefinition };

        public const string SpectacleText =
            "Swap with a minion in range. May repeat once by a different one of you, targeting a " +
            "different minion.";

        public const string TwinStrikeText =
            "After you perform a basic attack action a different you may repeat it once, targeting a " +
            "different unit.";

        /// <summary>
        /// Green, Tier III: Initiative 1, Defense 2, Movement 2, Skill - Ranged, Range 2, Attack item;
        /// Theatrics' upgrade (the other is Wire Dancers).
        /// </summary>
        public static CardDefinition SpectacleDefinition => LazySpectacle.Value;

        private static readonly Lazy<CardDefinition> LazySpectacle = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                Spectacle, ContentRevision, "Spectacle", new InitiativeValue(1),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack,
                    Array.Empty<CardDefinitionId>()),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        RazzleEffectContent.SwapWithAMinionThenRepeatByADifferentBody(
                            Spectacle, ContentRevision)),
                    Movement(2),
                    Defense(2)
                },
                null, CardSourceContent.Transcribed(SpectacleText)));

        /// <summary>
        /// Purple, p.16 Ultimate: no Initiative, no icons, no actions - a permanent triggered ability
        /// after each basic Attack of his (<see cref="UltimateTriggerCondition.AfterControllerPerformsBasicAttack"/>),
        /// at p.8's action end, as Cloak and Daggers is at every basic action's.
        /// </summary>
        public static CardDefinition TwinStrikeDefinition => LazyTwinStrike.Value;

        private static readonly Lazy<CardDefinition> LazyTwinStrike = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                TwinStrike, ContentRevision, "Twin Strike", default,
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.UltimateCard(),
                Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility(
                    RazzleEffectContent.TwinStrikeRuleId,
                    "RazzleTwinStrike",
                    RazzleEffectContent.TwinStrikeTriggeredEffect(ContentRevision).Reference,
                    UltimateTriggerCondition.AfterControllerPerformsBasicAttack,
                    new[] { TimingCheckpoint.ActionEnded },
                    Array.Empty<PrintedValueDefinition>()),
                CardSourceContent.Transcribed(TwinStrikeText)));
    }

    public static partial class RazzleEffectContent
    {
        /// <summary>The Ultimate's rule identity, named here because the card content references it.</summary>
        public const string TwinStrikeRuleId = "razzle-twin-strike";

        /// <summary>Twin Strike's one node, so a paused repeat names what it is asking.</summary>
        public const string TwinStrikeRepeatNode = "twin-strike-repeat";

        /// <summary>Spectacle's repeat node.</summary>
        public static string RepeatNode(CardDefinitionId card) => card.Value + "-repeat";

        /// <summary>
        /// "A different you may repeat it once, targeting a different unit." One node, as Cloak and
        /// Daggers is one node: "may" is the policy and the chooser, "once" the maximum, "targeting a
        /// different unit" <see cref="RepeatTargetBinding.DifferentTarget"/>, and "a different you" the
        /// performing body the node publishes (<see cref="EffectNodeDefinition.PerformedByADifferentBody"/>).
        /// Which action it repeats - the basic Attack that just ended, never a repeat of it (p.10) - is
        /// the trigger's occurrence, not the definition's business.
        /// </summary>
        public static EffectDefinition TwinStrikeTriggeredEffect(string contentRevision)
        {
            var repeat = new EffectNodeId(TwinStrikeRepeatNode);
            return new EffectDefinition(
                new EffectDefinitionId("razzle-twin-strike-effect"),
                contentRevision,
                repeat,
                new[]
                {
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        outputs: new[] { EffectNodeDefinition.PerformedByADifferentBody },
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }

        /// <summary>
        /// Spectacle: Theatrics' "Swap with a minion in range" (the printed-Range minion selection
        /// naming the Swap with the acting body, either side), then "May repeat once by a different one
        /// of you, targeting a different minion": the repeat excludes the minion the selection named
        /// (<see cref="RepeatTargetBinding.DifferentSelectionSubject"/>, Broadside's shape) and is
        /// performed by another body, so its Range 2 is measured from that body (RZ-Q12).
        /// </summary>
        public static EffectDefinition SwapWithAMinionThenRepeatByADifferentBody(
            CardDefinitionId card, string contentRevision)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var minion = new EffectNodeId(SwapSubjectNode(card));
            var swap = new EffectNodeId(SwapNode(card));
            var repeat = new EffectNodeId(RepeatNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { minion, swap, repeat }),
                    new EffectNodeDefinition(
                        minion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            // "a minion": either side (p.19 Swap).
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AnyTeam),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    SwapWithTheActor(swap, minion, SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        outputs: new[] { EffectNodeDefinition.PerformedByADifferentBody },
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentSelectionSubject,
                        boundSelectionNodeId: minion)
                });
        }
    }
}

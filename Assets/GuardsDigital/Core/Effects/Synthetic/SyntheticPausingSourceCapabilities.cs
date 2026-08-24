using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The bounded synthetic content milestone 8.19c1 proves its two pausing <em>sources</em> against:
    /// a removal prevention bought with a card, and an Active effect that runs at rulebook p.8's moment
    /// before the Minion Battle. The third pause the milestone owns - the after-Push trigger - is an
    /// Ultimate declaration rather than a capability, so it is declared on the synthetic hand beside
    /// milestone 8.19c's four.
    ///
    /// <para>Each declaration is a roster <em>shape</em> on content that is nobody's card, exactly as
    /// milestone 8.19c's five persistent shapes are. The radius, the price and the movement are the
    /// smallest that make each rule observable, and none of them is an official value; the milestone
    /// that composes the printed card supplies those.</para>
    ///
    /// <para>They are separate registrations rather than one capability with a mode selector, for the
    /// reason milestone 6.8 gave the first five: <see cref="IEffectCapabilityHandler.ActiveEffectDefinitions"/>
    /// means "every definition this capability can create", and a selector would make that list a lie or
    /// smuggle the choice through an untyped payload.</para>
    /// </summary>
    public static class SyntheticPausingSourceCapabilities
    {
        /// <summary>Brogan's Shield/Bolster/Fortify shape: friendly minions in radius may be kept.</summary>
        public static EffectCapabilityId ShieldFriendlyMinions { get; } =
            new EffectCapabilityId("synthetic-shield-friendly-minions");

        /// <summary>Xargatha's Constrict shape: an effect that runs before the Minion Battle.</summary>
        public static EffectCapabilityId ConstrictBeforeMinionBattle { get; } =
            new EffectCapabilityId("synthetic-constrict-before-minion-battle");

        public static EffectCapabilityReference ShieldFriendlyMinionsV1 =>
            new EffectCapabilityReference(ShieldFriendlyMinions, 1);

        public static EffectCapabilityReference ConstrictBeforeMinionBattleV1 =>
            new EffectCapabilityReference(ConstrictBeforeMinionBattle, 1);

        /// <summary>
        /// p.17's three cancellations for an Active effect on your card, as milestone 6.8 recorded
        /// them and milestone 8.19c restated them: the effect stops applying, the controller is
        /// defeated, or the card changes state at the end of the round.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] GeneralCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// "You may move one unit one space", declared here rather than on a card because two sources
        /// in this milestone run it and neither of them is a card's own action.
        ///
        /// <para>It is the smallest text that asks two questions - the offer and the selection - which
        /// is exactly what a pause altitude has to be proven against, and it is the same shape
        /// milestone 8.19a used for the same purpose. It also does something the surrounding rules can
        /// see: a minion moved out of the Battle Zone before p.8's battle is planned changes what that
        /// battle counts, which is how "executes before the battle is planned" is observed rather than
        /// asserted.</para>
        /// </summary>
        public static class PausingTriggeredEffect
        {
            public static EffectDefinitionReference Reference => Definition.Reference;

            public static EffectDefinition Definition { get; } = new EffectDefinition(
                new EffectDefinitionId("synthetic-pausing-may-move"),
                "phase-8-19c1-rev-1",
                new EffectNodeId("pausing-may-move"),
                MayMoveOneUnitNodes("pausing-may-move"));
        }

        /// <summary>
        /// `This Round: you may discard a card from your hand to keep a friendly minion in radius on
        /// the board when it would be removed` - Brogan's Shield/Bolster/Fortify shape.
        ///
        /// <para>The scope is the disk rather than the controller because that is what makes the rule
        /// move: the minions protected are decided by the board at the moment a removal stage asks, so
        /// a minion that walks out of the radius stops being protected without the effect changing.
        /// The price is on the declaration because rulebook p.13's own example of the family is card
        /// text that costs something; a prevention that cost nothing would be p.18's immunity, and
        /// <see cref="RuleModification.ForRemovalPrevention"/> refuses one.</para>
        /// </summary>
        public static ActiveEffectDefinition ShieldFriendlyMinionsDefinition { get; } =
            new ActiveEffectDefinition(
                "synthetic-shield-friendly-minions-effect",
                ActiveEffectAffectedScopeKind.UnitsInRadius,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                GeneralCancellations,
                false,
                "SyntheticShieldFriendlyMinions",
                "synthetic-shield-friendly-minions",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForRemovalPrevention(
                        RemovalPreventionCost.DiscardOwnedCard(CardLocation.Hand))
                },
                radius: DeclaredRadius.WrittenInText(2),
                affectedUnitRelation: SourceTeamRelation.AlliedWithSource);

        /// <summary>
        /// `This Round: before the end of round minion battle, you may move one unit one space` -
        /// Xargatha's Constrict shape, whose printed reminder text names exactly that position:
        /// "( Before the end of round minion battle. )".
        ///
        /// <para>It is the first content in this build to declare
        /// <see cref="TimingCheckpoint.EndOfRoundBeforeMinionBattle"/>, which milestone 8.19c added to
        /// the vocabulary with nothing visiting it. The duration is `This Round` because the moment is
        /// inside the round the effect was created in, and p.17 expires a This Round effect at Round
        /// End - which p.8 places after the battle, so the effect is still standing when its own window
        /// opens.</para>
        /// </summary>
        public static ActiveEffectDefinition ConstrictDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-constrict-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            GeneralCancellations,
            false,
            "SyntheticConstrict",
            "synthetic-constrict",
            new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle },
            triggeredEffect: PausingTriggeredEffect.Reference);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ShieldFriendlyMinionsV1, ShieldFriendlyMinionsDefinition),
            new ActiveEffectCreationHandler(ConstrictBeforeMinionBattleV1, ConstrictDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            ShieldFriendlyMinionsDefinition,
            ConstrictDefinition
        });

        /// <summary>
        /// "You may move one unit one space", as nodes. It is the same shape the synthetic hand's
        /// continuation text uses and it is written here rather than shared with it, because a static
        /// reference from this file to that one would make two static initializers depend on each
        /// other - and the capability registry is what the hand reads, not the other way round.
        /// </summary>
        private static EffectNodeDefinition[] MayMoveOneUnitNodes(string rootId) =>
            new[]
            {
                new EffectNodeDefinition(
                    new EffectNodeId(rootId), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { new EffectNodeId($"{rootId}-sequence") },
                    chooser: EffectChooserRole.Actor),
                new EffectNodeDefinition(
                    new EffectNodeId($"{rootId}-sequence"), EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[]
                    {
                        new EffectNodeId($"{rootId}-subject"), new EffectNodeId($"{rootId}-move")
                    }),
                new EffectNodeDefinition(
                    new EffectNodeId($"{rootId}-subject"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(2)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(new EffectNodeId($"{rootId}-move")))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    new EffectNodeId($"{rootId}-move"), EffectNodeKind.Capability,
                    EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", new EffectNodeId($"{rootId}-subject"), "chosen",
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            EffectValue.Integer(
                                (int)GuardsDigital.Core.Rules.Displacement
                                    .DirectMoveDestinationPolicy.Unrestricted))
                    },
                    outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) })
            };
    }
}

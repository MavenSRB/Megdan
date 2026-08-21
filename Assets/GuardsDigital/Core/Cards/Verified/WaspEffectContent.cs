using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Wasp's five telekinetic Skills - milestone 8.6, and the first verified
    /// official card text this project runs.
    ///
    /// <para><b>Nothing here is a handler.</b> Every node below names a capability that already existed
    /// and a policy that is now declarable, which is what milestone 8.2's capability matrix promised for
    /// the whole of Wasp's set: "no custom handler anywhere". What 8.6 added is vocabulary the general
    /// contracts were missing - the orbit destination policy, the printed-Range selection with p.19's
    /// straight-line complement, the Place anchored at the acting hero, and the repeat maximum with its
    /// same-target binding - and each of those lives beside the general rule it belongs to rather than
    /// here.</para>
    ///
    /// <para><b>Why the two families read differently.</b> Lift Up, Control Gravity and Center of Mass
    /// print "in radius"; Telekinesis and Mass Telekinesis print "in range". p.9 says card text
    /// mentioning either "refers to this value" - the printed icon's - so both are selections bounded by
    /// the card rather than by a number written into a node, and they differ only in which icon they
    /// read. The distance that <em>is</em> written down is the single space the Move covers, because
    /// that is card text and p.16 never raises it.</para>
    ///
    /// <para><b>What each definition deliberately does not say.</b> No card-name branch, no immunity
    /// question of its own, no geometry, and no ordering beyond the printed sentence's. Immunity is the
    /// selection's, through the shared targeting contract; the destinations are the declared policy's;
    /// and the repeat is the last child of the root because that is where p.10 puts it - after you
    /// perform this action, you may perform the entire action again.</para>
    /// </summary>
    public static class WaspEffectContent
    {
        /// <summary>
        /// Wasp's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, so a card that has one
        /// carries it and a card that does not still states the milestone that owes it -
        /// <see cref="ActionDefinition"/> refuses both to be absent at once.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WaspCardContent.LiftUp) ||
                cardId.Equals(WaspCardContent.ControlGravity))
            {
                return CreateOrbit(cardId, contentRevision, maximumRepeats: 1);
            }

            if (cardId.Equals(WaspCardContent.CenterOfMass))
            {
                // "May repeat up to two times on the same target" - the first verified repeat count
                // above one, which is the whole of what separates this card from the two above it.
                return CreateOrbit(cardId, contentRevision, maximumRepeats: 2);
            }

            if (cardId.Equals(WaspCardContent.Telekinesis))
            {
                return CreateTelekinesis(cardId, contentRevision, repeats: false);
            }

            if (cardId.Equals(WaspCardContent.MassTelekinesis))
            {
                // "May repeat once" with nothing after it, so p.10's default stands and the repeated
                // instance chooses freely. The absence of `on the same target` is content, and the
                // definition states it by declaring no binding rather than by omitting a parameter.
                return CreateTelekinesis(cardId, contentRevision, repeats: true);
            }

            return null;
        }

        /// <summary>
        /// "Move a unit, or a token, in radius 1 space, without moving it away from you or closer to
        /// you. May repeat once on the same target."
        ///
        /// Three nodes and one sentence each. The selection is bounded by the printed Radius icon and
        /// names the Move as its consumer, so a unit with nowhere to orbit to is never offered
        /// (milestone 6.8l). The Move is mandatory at exactly one space, which is p.11's own rule for a
        /// distance the card does not make optional: "you must move the exact number of spaces, or stop
        /// performing the action at that step". And the repeat binds the selection, so the repeated
        /// instance moves the same piece around the same hero rather than choosing again.
        /// </summary>
        private static EffectDefinition CreateOrbit(
            CardDefinitionId cardId,
            string contentRevision,
            int maximumRepeats)
        {
            var root = new EffectNodeId("orbit-root");
            var select = new EffectNodeId("orbit-select");
            var move = new EffectNodeId("orbit-move");
            var repeat = new EffectNodeId("orbit-repeat");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, move, repeat }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusTargetV1,
                        inputs: new[]
                        {
                            // "a unit, or a token" is p.18's own breadth for what a direct Move may
                            // take, and the selection offers every board object in the disk, so the
                            // card narrows nothing here. What it does exclude is Wasp herself: the
                            // sentence moves something else.
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            // Card-text direct Move has no activity of its own and reads its enclosing
                            // action's, which is the Skill this text is printed on (p.18, and the
                            // reading milestone 6.5b settled). Declaring the same activity the Move
                            // itself will ask immunity for is what makes the offer and the operation
                            // one answer.
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)RuleActivity.Skill)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", select, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer(
                                    (int)DirectMoveDestinationPolicy.EquidistantFromActor))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: maximumRepeats,
                        repeatTargetBinding: RepeatTargetBinding.SameTarget,
                        boundSelectionNodeId: select)
                });
        }

        /// <summary>
        /// "Place a unit or a token in range, which is not in a straight line, into a space adjacent to
        /// you." - and, for Mass Telekinesis, "May repeat once."
        ///
        /// The selection reads the printed Range icon and applies p.19's straight-line rule as a
        /// complement anchored at Wasp, which is the one place that rule is stated. The Place is
        /// mandatory, takes a unit or a token from the board, and measures its destinations from the
        /// acting hero rather than from the piece - "into a space adjacent to you" is a question about
        /// where Wasp stands, and no radius around the piece could ask it.
        /// </summary>
        private static EffectDefinition CreateTelekinesis(
            CardDefinitionId cardId,
            string contentRevision,
            bool repeats)
        {
            var root = new EffectNodeId("telekinesis-root");
            var select = new EffectNodeId("telekinesis-select");
            var place = new EffectNodeId("telekinesis-place");
            var repeat = new EffectNodeId("telekinesis-repeat");

            var children = repeats
                ? new[] { select, place, repeat }
                : new[] { select, place };
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        // p.18 gives the Place keyword no activity of its own, so the immunity question
                        // is the enclosing Skill's - the same one the Place capability asks when it
                        // reaches the piece this selection named.
                        EffectInputBinding.FromLiteral(
                            "activity", EffectValue.Integer((int)RuleActivity.Skill)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeStraightLineParameterName,
                            EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(place))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.PlaceV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", select, "chosen", EffectValueKind.Coordinate),
                        // `adjacent to you` is card text and p.16 never raises it, so the one is
                        // written down here rather than read off an icon.
                        EffectInputBinding.FromLiteral("radius", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            EffectValue.Integer((int)PlaceEligiblePieceKind.UnitOrToken)),
                        EffectInputBinding.FromLiteral(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            EffectValue.Integer((int)PlaceDestinationAnchor.ActingHero))
                    },
                    outputs: new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) })
            };

            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");
    }
}

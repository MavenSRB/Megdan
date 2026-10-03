using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's Green shard bodies - milestone 8.75e. The Green Tier line forks into two, and
    /// BOTH halves now compose here.
    ///
    /// <para><b>The SWAP line.</b> Unstable Portal (☀ swap with a friendly unit in
    /// radius / ☾ swap with an enemy unit in radius) and Chaos Gate (the same swap, then "you may move
    /// that unit 1 space" on the sun face and "you may move 1 space" on the moon face) are built on
    /// vocabulary this build already registers: <c>synthetic-radius-unit-target@1</c> names p.19's unit
    /// inside p.9's Radius disk, <c>synthetic-swap-actor@1</c> performs p.19's Swap between the acting
    /// hero and that unit, and <c>synthetic-direct-move@3</c> walks the one optional space. This is
    /// Rowenna's protective-swap shape (a unit selection whose consumer is <see cref="SwapWithActor"/>)
    /// with the printed reach read as Radius rather than Range, and with Chaos Gate's optional single
    /// step added after it.</para>
    ///
    /// <para><b>The MAGMA line</b> - Path of Ashes, Path of Cinders and Path of Flames (☀ move up to N
    /// spaces in a straight line, dropping a magma token in each empty space moved through or out of /
    /// ☾ place up to N magma tokens in radius) - composes on <see cref="IgnatiaMagmaContent"/>, the
    /// subsystem this milestone adds. The moon face is N optional supply Places of the magma token
    /// reaching the printed Radius (<c>PlaceHeroTokenHandler</c> reused as Min's mines reuse it); the sun
    /// face is her own <c>synthetic-direct-move@3</c> straight-line Move, its origin published by
    /// <c>synthetic-actor-space@1</c> and its wake laid by <c>ignatia-place-magma-wake@1</c>, the new
    /// place-along-a-move's-wake capability. See <see cref="MagmaPaths"/> and
    /// <see cref="IgnatiaMagmaContent"/>.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        static partial void GreenBodyImpl(
            CardDefinitionId cardId, string revision, ref EffectDefinition body)
        {
            if (cardId.Equals(IgnatiaCardContent.UnstablePortal))
            {
                // "☀: Swap with a friendly unit in radius. ☾: Swap with an enemy unit in radius."
                body = SwapWithAUnit(cardId, revision);
            }
            else if (cardId.Equals(IgnatiaCardContent.ChaosGate))
            {
                // "☀: Swap with a friendly unit in radius; you may move that unit 1 space.
                //  ☾: Swap with an enemy unit in radius; you may move 1 space."
                body = SwapWithAUnitThenMove(cardId, revision);
            }
            else if (cardId.Equals(IgnatiaCardContent.PathOfAshes))
            {
                // "Move up to 2 spaces in a straight line. Place a magma token in each empty space you
                //  moved through, or out of. / Place up to 2 magma tokens in radius."
                body = MagmaPaths(cardId, revision, count: 2);
            }
            else if (cardId.Equals(IgnatiaCardContent.PathOfCinders))
            {
                // The same, up to 3.
                body = MagmaPaths(cardId, revision, count: 3);
            }
            else if (cardId.Equals(IgnatiaCardContent.PathOfFlames))
            {
                // The same, up to 4.
                body = MagmaPaths(cardId, revision, count: 4);
            }
        }

        /// <summary>
        /// One coin-branched magma card. The sun arm is her own straight-line Move that drops a magma
        /// token in each empty space it passes through or leaves; the moon arm scatters up to
        /// <paramref name="count"/> magma tokens inside her printed Radius. Both faces place the same
        /// impassable obstacle from the same supply - see <see cref="IgnatiaMagmaContent"/>.
        /// </summary>
        private static EffectDefinition MagmaPaths(
            CardDefinitionId cardId, string revision, int count)
        {
            var sunArm = new EffectNodeId("ignatia-magma-sun");
            var moonArm = new EffectNodeId("ignatia-magma-moon");

            var nodes = new List<EffectNodeDefinition>();
            nodes.AddRange(MagmaMoveArm(sunArm, count));
            nodes.AddRange(MagmaRadiusArm(moonArm, count));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>
        /// The sun face: "Move up to N spaces in a straight line. Place a magma token in each empty
        /// space you moved through, or out of."
        ///
        /// <para>Four nodes in order. <c>synthetic-actor-space@1</c> publishes where she stands before
        /// the Move, which feeds both the Move's mover and the wake's origin.
        /// <c>choose-line-destination@1</c> offers where a STRAIGHT-LINE move of up to N spaces can end
        /// - the capability whose own remarks read "move up to that number of spaces in a straight
        /// line", walking the file rather than filtering one, so a space reachable only by turning a
        /// corner is never offered. <c>synthetic-direct-move@3</c> then walks her to the space that
        /// answer named (<see cref="DirectMoveDestinationPolicy.NamedSpace"/>, Brogan's charge shape and
        /// Silverarrow's), and <c>ignatia-place-magma-wake@1</c> reads the Move's two endpoints and
        /// drops a magma token in each empty space from the origin up to but not including where she
        /// now stands.</para>
        ///
        /// <para><b>The straight line is the offer's, not the Move's</b> - corrected at milestone
        /// 8.75c. <c>synthetic-direct-move@3</c> under
        /// <see cref="DirectMoveDestinationPolicy.Unrestricted"/> is p.11's ordinary Movement
        /// reachability, which turns corners: composed that way, "Move up to 2 spaces in a straight
        /// line" offered every space two steps of walking away, and the printed three words did
        /// nothing. Both the line offer and the Move are <see cref="EffectFailurePolicy.UpTo"/>, so
        /// "up to" is honoured by declining the whole step - which lays no trail, because a Move that
        /// did not happen has no wake.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> MagmaMoveArm(EffectNodeId arm, int count)
        {
            var actorSpace = new EffectNodeId("ignatia-magma-sun-actor-space");
            var line = new EffectNodeId("ignatia-magma-sun-line");
            var move = new EffectNodeId("ignatia-magma-sun-move");
            var wake = new EffectNodeId("ignatia-magma-sun-wake");

            return new[]
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { actorSpace, line, move, wake }),

                // Where she stands before the Move - the "moved out of" space, and the mover the Move
                // walks. It decides nothing and publishes a coordinate.
                new EffectNodeDefinition(
                    actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                    }),

                // "in a straight line", and "up to N". The offer's own floor is one - standing still
                // is not a straight-line move, so her own space is never a destination - and the
                // "up to" is carried by the node's `may`, which is what puts a Decline on the pause:
                // only `EffectFailurePolicy.May` offers one, so an `up to` here would have forced her
                // to move whenever any file was open.
                new EffectNodeDefinition(
                    line, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticRosterCapabilities.ChooseLineDestinationV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            "maximumDistance", EffectValue.Integer(count))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(MagmaLineDestinationOutput, EffectValueKind.Coordinate)
                    }),

                // "Move up to N spaces" - to the space the line offer named, so the Move applies it
                // rather than asking a second question with one answer.
                new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(count)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.NamedSpace),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                            line, MagmaLineDestinationOutput, EffectValueKind.Coordinate)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                    }),

                // "Place a magma token in each empty space you moved through, or out of" - the wake,
                // reading the space she started on and the space the Move left her.
                new EffectNodeDefinition(
                    wake, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    IgnatiaMagmaContent.PlaceMagmaWakeV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            IgnatiaMagmaContent.WakeOriginParameterName, actorSpace, ActorSpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromNodeOutput(
                            IgnatiaMagmaContent.WakeDestinationParameterName, move, "moved",
                            EffectValueKind.Coordinate)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            IgnatiaMagmaContent.WakePlacedCountOutput, EffectValueKind.Integer)
                    })
            };
        }

        /// <summary>
        /// The moon face: "Place up to N magma tokens in radius." N optional supply Places, each reaching
        /// the card's printed Radius and each placing one magma token - the shape
        /// <c>PlaceHeroTokenHandler</c>'s own remarks name for "place up to N", since a stateless handler
        /// cannot tell a second placement from a first and so a second decision belongs to a second node.
        /// Each node is <see cref="EffectFailurePolicy.May"/>, so "up to" is honoured a token at a time.
        ///
        /// <para><b>Milestone 8.113: p.18's short supply before each placement.</b> "If you need to place
        /// more tokens than are available in the supply, first remove up to that number of tokens of
        /// the same type from the board, if able." Each placement is preceded by
        /// <c>ignatia-reclaim-magma@1</c>, which declines on its own while the pile holds a token - so
        /// the placement's question is unchanged - and otherwise asks which standing magma token comes
        /// back. It is a `may`, because the placement it makes room for is one: declining it is
        /// declining that token, and the empty pile then leaves the placement nothing to do.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> MagmaRadiusArm(EffectNodeId arm, int count)
        {
            var places = new EffectNodeId[count];
            var reclaims = new EffectNodeId[count];
            var steps = new List<EffectNodeId>();
            for (var index = 0; index < count; index++)
            {
                reclaims[index] = new EffectNodeId($"ignatia-magma-moon-reclaim-{index + 1}");
                places[index] = new EffectNodeId($"ignatia-magma-moon-place-{index + 1}");
                steps.Add(reclaims[index]);
                steps.Add(places[index]);
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.UpTo,
                    childNodeIds: steps)
            };

            foreach (var reclaim in reclaims)
            {
                nodes.Add(new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    IgnatiaMagmaContent.ReclaimMagmaV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }));
            }

            foreach (var place in places)
            {
                nodes.Add(new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    IgnatiaMagmaContent.PlaceMagmaInRadiusV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }));
            }

            return nodes;
        }

        /// <summary>The coordinate <c>synthetic-actor-space@1</c> publishes - her own space.</summary>
        private const string ActorSpaceOutput = "space";

        /// <summary>The coordinate <c>choose-line-destination@1</c> publishes - where the file ends.</summary>
        private const string MagmaLineDestinationOutput = "destination";

        /// <summary>
        /// One coin-branched "Swap with a friendly/enemy unit in radius": the sun arm exchanges the
        /// acting hero with a unit on her own side, the moon arm with a unit on the opposing side. p.19's
        /// Swap "does not count as movement or placement", so nothing else about it is stated.
        /// </summary>
        private static EffectDefinition SwapWithAUnit(CardDefinitionId cardId, string revision)
        {
            var sunArm = new EffectNodeId("ignatia-swap-sun");
            var moonArm = new EffectNodeId("ignatia-swap-moon");

            var nodes = new List<EffectNodeDefinition>();
            nodes.AddRange(SwapArm(
                "swap-sun", SourceTeamRelation.AlliedWithSource, thenMove: false, moverOutput: null));
            nodes.AddRange(SwapArm(
                "swap-moon", SourceTeamRelation.OpposingSource, thenMove: false, moverOutput: null));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>
        /// Chaos Gate: the same coin-branched Swap, with p.11's one optional space after it. The sun face
        /// moves the unit that was just swapped - so its mover is that unit's new space, which is where
        /// the actor stood before the Swap (<c>subjectAt</c>) - and the moon face moves the acting hero
        /// herself from where the Swap left her (<c>actorAt</c>). Both are "you may move ... 1 space":
        /// the whole step is optional (<see cref="EffectFailurePolicy.May"/>) and the distance is the
        /// exact one the card prints, so <c>leastDistance</c> equals the distance.
        /// </summary>
        private static EffectDefinition SwapWithAUnitThenMove(CardDefinitionId cardId, string revision)
        {
            var sunArm = new EffectNodeId("ignatia-gate-sun");
            var moonArm = new EffectNodeId("ignatia-gate-moon");

            var nodes = new List<EffectNodeDefinition>();
            // ☀ moves "that unit" - the swapped friendly unit - from its new space (subjectAt).
            nodes.AddRange(SwapArm(
                "gate-sun", SourceTeamRelation.AlliedWithSource, thenMove: true, moverOutput: "subjectAt"));
            // ☾ moves the acting hero herself, from where the Swap left her (actorAt).
            nodes.AddRange(SwapArm(
                "gate-moon", SourceTeamRelation.OpposingSource, thenMove: true, moverOutput: "actorAt"));

            return CoinBranched(cardId, revision, sunArm, moonArm, nodes);
        }

        /// <summary>
        /// One arm of a coin-branched Swap: a printed-Radius unit selection whose consumer is the Swap,
        /// then <c>synthetic-swap-actor@1</c> exchanging the actor with the chosen unit's space, and -
        /// where <paramref name="thenMove"/> - one optional 1-space direct Move whose mover is read off
        /// the Swap's <paramref name="moverOutput"/> (the unit's new space on the sun face, the actor's
        /// on the moon face).
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> SwapArm(
            string prefix, SourceTeamRelation relation, bool thenMove, string moverOutput)
        {
            var arm = new EffectNodeId($"ignatia-{prefix}");
            var select = new EffectNodeId($"ignatia-{prefix}-select");
            var swap = new EffectNodeId($"ignatia-{prefix}-swap");
            var move = new EffectNodeId($"ignatia-{prefix}-move");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: thenMove
                        ? new[] { select, swap, move }
                        : new[] { select, swap }),

                // "a friendly/enemy unit in radius" - p.19's units (hero or minion) inside the card's
                // own printed Radius, on the declared side, with the Swap named as the consumer so a
                // unit this hero cannot exchange with is greyed rather than offered and then refused.
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                    inputs: new[]
                    {
                        // A Skill's effect is the action (p.12), so the immunity question this
                        // selection asks is the Skill's own.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName, relation),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                    },
                    outputs: new[]
                    {
                        // A unit selection publishes whichever kind was named plus that unit's space,
                        // and an undeclared recorded outcome is a snapshot the build refuses to read
                        // back - so all three are declared, exactly as Rowenna's own unit Swap does.
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                // p.19's Swap between the acting hero and the chosen unit. The unit arrives as the space
                // it stands on, which is the subject identity every repositioning operation in this
                // build uses.
                new EffectNodeDefinition(
                    swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", select,
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                        new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                    })
            };

            if (thenMove)
            {
                nodes.Add(new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", swap, moverOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                            EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            SyntheticDisplacementCapabilities.DidMoveOutputName,
                            EffectValueKind.Boolean)
                    }));
            }

            return nodes;
        }
    }
}

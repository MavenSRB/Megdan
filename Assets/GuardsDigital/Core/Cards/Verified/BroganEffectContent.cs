using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brogan's printed card text as typed effect definitions - milestone 8.21.
    ///
    /// <para>It sits beside <see cref="BroganCardContent"/> for the reason the two earlier heroes'
    /// pairs do: that file is the transcription and this one is the composition. A reader auditing
    /// what the cards say reads one; a reader auditing what the engine does with it reads the
    /// other.</para>
    ///
    /// <para><b>Every node here is a declaration over a registered contract.</b> Nothing reads a card
    /// name, a hero name or a printed sentence at runtime. Where a shape already existed - milestone
    /// 8.19b's charge line, Wasp's radius discard-or-defeat, Arien's push rider - this file declares
    /// Brogan's numbers against it rather than restating the shape.</para>
    ///
    /// <para><b>What is new here is the modal form.</b> Throwing Axe and Throwing Spear print p.10's
    /// "Choose one —", which composes to <see cref="EffectNodeKind.Choose"/> - a chooser-owned
    /// selection among children, with the actor as the chooser p.10 hands an unassigned decision to.
    /// Both bullets end by naming the Attack's target, which is why both cards declare
    /// <see cref="AttackTargetBinding.ByCardText"/>: an arm that names none leaves the Attack without
    /// one and p.10 stops it there, which is exactly what ledger question 11 answered for a bullet
    /// whose condition cannot be met.</para>
    /// </summary>
    public static class BroganEffectContent
    {
        /// <summary>
        /// Brogan's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, and
        /// <see cref="ActionDefinition"/> refuses a Skill carrying neither this nor a stated debt.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(BroganCardContent.Bulwark))
            {
                return CreateBulwark(cardId, contentRevision);
            }

            if (cardId.Equals(BroganCardContent.BrutalJab))
            {
                // "You may move 1 space" - the move itself is optional and its distance exact, which
                // p.11 keeps as two different kinds of optional and which the two siblings below print
                // the other way round.
                return CreateShove(
                    cardId, contentRevision, moveDistance: 1, moveDistanceIsOptional: false,
                    pushDistance: 1);
            }

            if (cardId.Equals(BroganCardContent.MightyPunch))
            {
                return CreateShove(
                    cardId, contentRevision, moveDistance: 1, moveDistanceIsOptional: true,
                    pushDistance: 2);
            }

            if (cardId.Equals(BroganCardContent.SavageKick))
            {
                return CreateShove(
                    cardId, contentRevision, moveDistance: 2, moveDistanceIsOptional: true,
                    pushDistance: 2);
            }

            if (cardId.Equals(BroganCardContent.ShieldBash))
            {
                return CreateCounter(cardId, contentRevision, escalatesToDefeat: false);
            }

            if (cardId.Equals(BroganCardContent.Counterattack))
            {
                return CreateCounter(cardId, contentRevision, escalatesToDefeat: true);
            }

            if (cardId.Equals(BroganCardContent.Shield))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    BroganActiveEffectContent.ShieldFriendlyMinionsV1, "guard");
            }

            if (cardId.Equals(BroganCardContent.Bolster))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    BroganActiveEffectContent.BolsterFriendlyMinionsV1, "guard");
            }

            if (cardId.Equals(BroganCardContent.Fortify))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    BroganActiveEffectContent.FortifyFriendlyMinionsV1, "guard");
            }

            if (cardId.Equals(BroganCardContent.WarDrummer))
            {
                return CreateSkald(cardId, contentRevision, ordinaryCoins: 1, raisedCoins: 3);
            }

            if (cardId.Equals(BroganCardContent.MasterSkald))
            {
                return CreateSkald(cardId, contentRevision, ordinaryCoins: 2, raisedCoins: 4);
            }

            return null;
        }

        /// <summary>
        /// Brogan's card text for one printed Attack, or <c>null</c> where the face prints none or its
        /// composition is still owed.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(BroganCardContent.Onslaught))
            {
                return CreateStepIntoTheVacatedSpace(cardId, contentRevision);
            }

            if (cardId.Equals(BroganCardContent.MadDash))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 2, mostDistance: 2);
            }

            if (cardId.Equals(BroganCardContent.Bullrush))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 2, mostDistance: 3);
            }

            if (cardId.Equals(BroganCardContent.FuriousCharge))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 2, mostDistance: 4);
            }

            if (cardId.Equals(BroganCardContent.ThrowingAxe))
            {
                // "You may discard a card; if you do, target a unit in range" - the discard is the
                // price of the range bullet.
                return CreateModalThrow(cardId, contentRevision, theDiscardIsThePrice: true);
            }

            if (cardId.Equals(BroganCardContent.ThrowingSpear))
            {
                // "You may discard a card. If you have a card in the discard, target a unit in range" -
                // a card already in the pile pays for it, so this card's own discard is optional in a
                // way the Tier II card's is not.
                return CreateModalThrow(cardId, contentRevision, theDiscardIsThePrice: false);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's three moments an Attack's composed text is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Brogan is the first hero whose set reaches step 7 with something to do there.
        /// Onslaught prints "After the attack:", and that placement is the whole reason its sentence
        /// works: step 6 has already removed the defeated unit, so the space it occupied is a space
        /// anyone can walk into. His three charges print "Before the attack:", which is step 1.</para>
        ///
        /// <para>The two modal Ranged Attacks print no keyword at all, and they are at step 1 for a
        /// different reason: their chosen bullet <em>is</em> the targeting sentence, and p.14 binds a
        /// target before anything else happens. An Attack whose target its own text names has nothing
        /// to apply at any later moment.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(BroganCardContent.Onslaught))
            {
                return CardTextTiming.AfterTheAttack;
            }

            if (cardId.Equals(BroganCardContent.MadDash) ||
                cardId.Equals(BroganCardContent.Bullrush) ||
                cardId.Equals(BroganCardContent.FuriousCharge) ||
                cardId.Equals(BroganCardContent.ThrowingAxe) ||
                cardId.Equals(BroganCardContent.ThrowingSpear))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            // Leap Forward prints "After the attack:" too, and states no timing here because its
            // composition is deferred: ActionDefinition refuses a moment without a root.
            return CardTextTiming.None;
        }

        /// <summary>
        /// "After the attack: Move into the space it occupied." - Onslaught's second sentence, and the
        /// first verified text in this project applied at rulebook p.14's step 7.
        ///
        /// <para><b>Three nodes and no decision in any of them.</b> The space is the one the Attack's
        /// target was standing on before p.14's consequences ran, which
        /// <c>attacked-target-space@1</c> captured for exactly this; the mover is Brogan, who reaches
        /// the move through the actor-space bridge rather than through a selection, because "Move" with
        /// no subject names him and a one-candidate prompt would invent a choice; and the destination is
        /// that captured space, which is milestone 8.21's
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/>. The whole sentence therefore asks
        /// nobody anything, which is what a card that says where to go should do.</para>
        ///
        /// <para><b>Why it is mandatory and what happens when the space is not free.</b> The face
        /// prints no "may". A defender who survived the Attack is still standing there, so the move has
        /// nowhere to go and p.10's mandatory-step rule stops the text at that step, keeping everything
        /// before it - which here is the whole resolved Attack. That is the printed behaviour and not a
        /// failure: the sentence is written for the case where step 6 removed somebody.</para>
        ///
        /// <para>The distance is one because the target was adjacent - p.13 gives a non-Ranged Attack
        /// no other reach - so the vacated space is one step away and the declaration says so rather
        /// than leaving the budget open.</para>
        /// </summary>
        private static EffectDefinition CreateStepIntoTheVacatedSpace(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("onslaught-root");
            var formerSpace = new EffectNodeId("onslaught-former-space");
            var actorSpace = new EffectNodeId("onslaught-actor-space");
            var move = new EffectNodeId("onslaught-move");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { formerSpace, actorSpace, move }),
                    new EffectNodeDefinition(
                        formerSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(FormerSpaceOutput, EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                formerSpace, FormerSpaceOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Before the attack: Move N spaces in a straight line to a space adjacent to an enemy unit,
        /// then target that unit." - Mad Dash's sentence at exactly two spaces, Bullrush's at two or
        /// three, Furious Charge's at two, three or four.
        ///
        /// <para><b>The printed order is the node order, and milestone 8.35p is what made it so.</b>
        /// The sentence moves first and targets second, so the graph does: <c>choose-charge-line@1</c>
        /// walks the straight files at the printed distances and asks only <em>where he lands</em>;
        /// the Move commits on that answer; and only then does an ordinary <c>choose-target@2</c> ask
        /// <em>whom he takes</em>, measured from the space he is now standing on. Until 8.35p both
        /// questions were asked inside the charge node, before the Move node ran, so a player named a
        /// victim while the board correctly still showed the hero on his starting space. The user's
        /// rule: "they move as soon as they select, and then they attack".</para>
        ///
        /// <para><b>It is a reordering and not a rules change, and the reason is one sentence.</b>
        /// The destinations offered are exactly the ones with an enemy unit beside them - that filter
        /// never moved - and a direct Move of the actor removes no unit and moves no other, so the
        /// candidates beside the destination before the Move are the candidates beside the actor
        /// after it. Nothing here checks feasibility a second time: a destination with no legal target
        /// is never offered, so the printed reminder Mad Dash carries - "( If you cannot make this
        /// move, you cannot attack. )" - is the case where the selection has nothing to offer at all,
        /// which p.10 answers by stopping the action at that step.</para>
        ///
        /// <para><b>The move is to the space the selection named</b>, which is why it declares
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/> rather than a distance and a direction.
        /// Under that policy the declared number is the budget rather than an exact count, which is
        /// what lets one declaration serve Bullrush's "2 or 3" without a second node per
        /// distance.</para>
        ///
        /// <para><b>The retarget is what makes this an Attack at all.</b> The card declares
        /// <see cref="AttackTargetBinding.ByCardText"/>, so the action reaches step 1 with no target;
        /// <c>set-attack-target@1</c> names the unit the charge ended beside, and the Attack resolves
        /// against it from there. Binding a target is not resolving an Attack: the capability publishes
        /// a typed fact and the Attack's own authority reads it.</para>
        /// </summary>
        private static EffectDefinition CreateCharge(
            CardDefinitionId cardId, string contentRevision, int leastDistance, int mostDistance)
        {
            var root = new EffectNodeId("charge-root");
            var line = new EffectNodeId("charge-destination");
            var actorSpace = new EffectNodeId("charge-actor-space");
            var move = new EffectNodeId("charge-move");
            var victim = new EffectNodeId("charge-target");
            var retarget = new EffectNodeId("charge-retarget");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move, victim, retarget }),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseChargeLineV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(leastDistance)),
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(mostDistance)),
                            // "an enemy unit", which p.19 defines as a hero or a minion. All three
                            // faces print the same noun.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                RosterUnitKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ChargeDestinationOutput, EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(mostDistance)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, ChargeDestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    // "then target that unit" - asked after the Move, from where the hero now
                    // stands, which is what makes the printed order the played order. Radius 1
                    // measured from the actor is the same disk the charge node measured from the
                    // destination, because the actor is standing on it; the activity is left to the
                    // enclosing Attack rather than declared, so immunity is asked the identical
                    // question it was asked before milestone 8.35p split the node.
                    new EffectNodeDefinition(
                        victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                RosterUnitKind.Unit),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    BindAttackTarget(retarget, victim)
                });
        }

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • You may discard a card[;/.] [if you do,/If
        /// you have a card in the discard,] target a unit in range." - Throwing Axe's two bullets and
        /// Throwing Spear's.
        ///
        /// <para><b>The modal is p.10's and it is a node kind, not a branch.</b>
        /// <see cref="EffectNodeKind.Choose"/> is a chooser-owned selection among children, and p.10
        /// gives an unassigned decision to the card's controller, so the actor picks a bullet and the
        /// other is ignored - which is the page's own word for what happens to the one not
        /// chosen.</para>
        ///
        /// <para><b>Both bullets end by naming the target, and neither performs the Attack.</b> The
        /// card declares <see cref="AttackTargetBinding.ByCardText"/>, so the action reaches p.14's
        /// step 1 with no target and no Attack Total; each arm binds one through
        /// <c>set-attack-target@1</c>. An arm that binds none - a declined discard, an empty hand, an
        /// empty discard pile - leaves the Attack targetless and p.10 stops the action there, which is
        /// exactly the reading ledger question 11 approved for a bullet whose condition cannot be met.
        /// Nothing needs to refuse anything: the absence of a bound target <em>is</em> the stop.</para>
        ///
        /// <para><b>Where the two cards differ.</b> Throwing Axe's range bullet is paid for by the
        /// discard it offers - "if you do" - so its arm reads the positive fact
        /// <c>discard-owned-card@1</c> publishes. Throwing Spear's is paid for by a card already in the
        /// pile - "If you have a card in the discard" - so its arm asks
        /// <c>owned-card-present@1</c> about the discard pile <em>after</em> the optional step, which
        /// is why a card discarded to it counts and why declining costs nothing when the pile is
        /// already occupied. Both discards are the node's own <see cref="EffectFailurePolicy.May"/>,
        /// because both faces print "You may".</para>
        /// </summary>
        private static EffectDefinition CreateModalThrow(
            CardDefinitionId cardId, string contentRevision, bool theDiscardIsThePrice)
        {
            var root = new EffectNodeId("throw-mode");
            var adjacentArm = new EffectNodeId("throw-adjacent");
            var adjacentTarget = new EffectNodeId("throw-adjacent-target");
            var adjacentBind = new EffectNodeId("throw-adjacent-bind");
            var rangeArm = new EffectNodeId("throw-range");
            var discard = new EffectNodeId("throw-discard");
            var gate = new EffectNodeId("throw-gate");
            var rangeSequence = new EffectNodeId("throw-range-sequence");
            var rangeTarget = new EffectNodeId("throw-range-target");
            var rangeBind = new EffectNodeId("throw-range-bind");
            var present = new EffectNodeId("throw-discard-present");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { adjacentArm, rangeArm },
                    chooser: EffectChooserRole.Actor,
                    // p.10's "Choose one" exactly: one bullet is taken and the other is ignored.
                    minimumSelections: 1,
                    maximumSelections: 1),
                new EffectNodeDefinition(
                    adjacentArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { adjacentTarget, adjacentBind }),
                new EffectNodeDefinition(
                    adjacentTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: AdjacentEnemyUnit(),
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                    }),
                BindAttackTarget(adjacentBind, adjacentTarget),
                new EffectNodeDefinition(
                    rangeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: theDiscardIsThePrice
                        ? new[] { discard, gate }
                        : new[] { discard, present, gate }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticRosterCapabilities.DiscardOwnedCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticRosterCapabilities.HeroParameter,
                            EffectRoleKind.Actor, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.DidDiscardOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { rangeSequence },
                    inputs: new[]
                    {
                        theDiscardIsThePrice
                            ? EffectInputBinding.FromNodeOutput(
                                "flag", discard, SyntheticRosterCapabilities.DidDiscardOutput,
                                EffectValueKind.Boolean)
                            : EffectInputBinding.FromNodeOutput(
                                "flag", present, SyntheticRosterCapabilities.PresentOutput,
                                EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    rangeSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { rangeTarget, rangeBind }),
                new EffectNodeDefinition(
                    rangeTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    // "target a unit in range" - p.19's units, inside p.9's printed Range with p.16's
                    // items composed onto it. It is the unit-valued printed-Range selection rather
                    // than the space-valued one because the sentence names a unit and the node that
                    // consumes it binds the Attack's target, which is a piece and not a space.
                    SyntheticTargetingCapabilities.RangeUnitTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Attack),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion)
                    }),
                new EffectNodeDefinition(
                    rangeBind, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticContinuationCapabilities.SetAttackTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticContinuationCapabilities.HeroParameter, rangeTarget,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticContinuationCapabilities.MinionParameter, rangeTarget,
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                            EffectValueKind.Minion)
                    })
            };

            if (!theDiscardIsThePrice)
            {
                nodes.Add(new EffectNodeDefinition(
                    present, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.OwnedCardPresentV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticRosterCapabilities.HeroParameter,
                            EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.LocationParameter,
                            CardLocation.DiscardPile)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }));
            }

            return new EffectDefinition(AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "You may retrieve a discarded card. <b>This turn:</b> You and friendly units in radius
        /// cannot be moved, pushed, swapped or placed by enemy heroes." - Bulwark's two unrelated
        /// clauses, in the order the face prints them.
        ///
        /// <para><b>The retrieve is p.10's and it needs no chooser question.</b> That page settles who
        /// the card belongs to: retrieving "means to take one of <b>your</b> cards back into your hand"
        /// and "You can never retrieve another hero's cards", so the owner and the chooser are both the
        /// acting hero and the only decision is which of his own discarded cards to take back. "You
        /// may" is the node's own policy, so a hero with nothing worth taking - or nothing at all -
        /// declines and the keyword clause still runs.</para>
        ///
        /// <para><b>The second clause is the Active effect and nothing else.</b> Everything it says is
        /// definition content on <see cref="BroganActiveEffectContent.BulwarkDefinition"/> - the scope
        /// that includes its own hero, the disk, the four operations, the side that is barred - so an
        /// effect tree with branches in it would be describing one rule in a second vocabulary.</para>
        /// </summary>
        private static EffectDefinition CreateBulwark(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("bulwark-root");
            var retrieve = new EffectNodeId("bulwark-retrieve");
            var create = new EffectNodeId("bulwark-create");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { retrieve, create }),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind",
                                RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        }),
                    new EffectNodeDefinition(
                        create, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        BroganActiveEffectContent.BulwarkProtectionV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "[You may move 1 space. / Move up to N spaces.] Push an enemy unit[,] or a token[,] adjacent
        /// to you up to N spaces." - Brutal Jab's pair of sentences, Mighty Punch's and Savage Kick's.
        ///
        /// <para><b>The two cards' first sentences are different kinds of optional and p.11 keeps them
        /// apart.</b> "You may move 1 space" makes the <em>move</em> optional at an exact distance,
        /// which is <see cref="EffectFailurePolicy.May"/>; "Move up to N spaces" makes the
        /// <em>distance</em> optional, which is <see cref="EffectFailurePolicy.UpTo"/> and lets the
        /// actor walk anywhere from nowhere to N. The observable difference is what the player is
        /// asked: the first offers a decline beside the destinations, the second offers standing still
        /// as one of them.</para>
        ///
        /// <para><b>The push is one node because the sentence asks one question with three declared
        /// bounds.</b> p.18's Push already takes the objects standing beside the pusher; what the card
        /// adds is which pieces ("an enemy unit or a token"), how many ("an", which is one) and how far
        /// ("up to N").</para>
        ///
        /// <para><b>The push node carries <see cref="EffectFailurePolicy.UpTo"/>, and the printed word
        /// it answers to is the distance's.</b> Milestone 8.21's player-path gate caught this: the node
        /// declared a zero floor beside a mandatory policy, and
        /// <see cref="GuardsDigital.Core.Rules.Displacement.AdmittedDistances.ForDeclared"/> reads a
        /// printed floor only under <c>UpTo</c> - so all three faces silently admitted their declared
        /// distance alone, and a player pushing with Mighty Punch was moved exactly two spaces where the
        /// face offers nought, one or two. Arien's Rogue Wave prints the same "up to N spaces" over the
        /// same capability with the same one-target maximum and declares <c>UpTo</c>; these three now
        /// read the same way. What the policy also admits - confirming with no piece named - is not a
        /// second meaning: "push this unit nought spaces" and "push nobody" leave the board identical,
        /// and p.18 already treats a zero-space push as a legal result.</para>
        ///
        /// <para><b>"an enemy unit or a token" is one relation and it had to be minted.</b> p.19 makes
        /// a token not a unit and gives it no team, so the sentence qualifies its first noun and not
        /// its second: <see cref="SourceTeamRelation.OpposingSourceOrUnaligned"/>. Declaring
        /// <see cref="SourceTeamRelation.AnyTeam"/> would hand these cards the friendly units their
        /// words exclude, and <see cref="SourceTeamRelation.OpposingSource"/> would drop the token
        /// half of the sentence.</para>
        /// </summary>
        private static EffectDefinition CreateShove(
            CardDefinitionId cardId,
            string contentRevision,
            int moveDistance,
            bool moveDistanceIsOptional,
            int pushDistance)
        {
            var root = new EffectNodeId("shove-root");
            var actorSpace = new EffectNodeId("shove-actor-space");
            var move = new EffectNodeId("shove-move");
            var push = new EffectNodeId("shove-push");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move, push }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability,
                        moveDistanceIsOptional
                            ? EffectFailurePolicy.UpTo
                            : EffectFailurePolicy.May,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(moveDistance)),
                            // The card says how far and nothing about where, which is p.11's own
                            // breadth.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.ObjectPushV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(pushDistance)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromLiteral(
                                "ignoresObstacles", EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.UnitOrToken),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSourceOrUnaligned),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MaximumTargetsParameterName,
                                EffectValue.Integer(1))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "An enemy hero adjacent to you[,] who has played an attack card this turn discards a card,
        /// [if able. / or is defeated.]" - Shield Bash's sentence and Counterattack's.
        ///
        /// <para><b>The condition is part of who may be chosen.</b> p.9 makes a card with an Attack
        /// primary an Attack card and p.10 makes a card played the moment it is placed facedown, so the
        /// clause reads the identity of the card that hero has already played this turn - not whether
        /// they performed the Attack, and not whether it has resolved. It narrows the selection's
        /// candidates rather than gating what happens afterwards, because a hero who has played nothing
        /// is not one to offer and then refuse. If nobody beside Brogan qualifies, the step is
        /// impossible and p.10 stops the Skill there.</para>
        ///
        /// <para><b>The two faces differ in one clause and it is the same axis Wasp's Electroblast
        /// opened.</b> "discards a card, if able" is p.17's `if able`: a hero with an empty hand simply
        /// gives nothing and the Skill is done, which is <c>synthetic-discard-chosen-card@1</c>.
        /// "discards a card, or is defeated" is ledger question 13's one mandatory step with two ways
        /// to complete, which is revision 2 plus the arm it reports into - the third verified printing
        /// of that construction, after Wasp's Electroblast and Arien's three Red Attacks.</para>
        /// </summary>
        private static EffectDefinition CreateCounter(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("counter-root");
            var select = new EffectNodeId("counter-target");
            var discard = new EffectNodeId("counter-discard");
            var escalation = new EffectNodeId("counter-escalation");
            var defeat = new EffectNodeId("counter-defeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { select, discard, escalation }
                        : new[] { select, discard }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter,
                            RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        // "An enemy hero", which is p.19's hero and not its unit.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter,
                            RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        // A Skill's own activity is what p.18 asks immunity about here; there is no
                        // enclosing Attack to inherit one from.
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.PlayedAttackThisTurnParameter,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                    }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability,
                    escalatesToDefeat ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    escalatesToDefeat
                        ? SyntheticCardStateCapabilities.DiscardChosenCardV2
                        : SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, select,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: escalatesToDefeat
                        ? new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        }
                        : new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
            };

            if (escalatesToDefeat)
            {
                nodes.Add(new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard,
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, select,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "A friendly hero in range gains N coins; if any hero was defeated this round, that friendly
        /// hero gains M coins[,] instead." - War Drummer's sentence at 1 and 3, Master Skald's at 2 and
        /// 4.
        ///
        /// <para><b>One recipient and two amounts.</b> The selection runs first because both arms give
        /// the coins to the same hero - "that friendly hero" - so the question is asked once and the
        /// condition only decides how much. Brogan cannot name himself: p.10 says an action does not
        /// affect the acting hero unless the text says so, and this text does not, which is the
        /// exclusion the printed-icon selection already makes.</para>
        ///
        /// <para><b>The condition is round-scoped history and not board state.</b>
        /// <c>hero-defeated-this-round@1</c> reads the round's own defeat record - milestone 8.19a1
        /// made that authoritative - so it survives a snapshot and cannot be re-derived wrongly from a
        /// board where everybody has since respawned. These are the first verified cards to ask
        /// it.</para>
        ///
        /// <para><b>The two amounts are two nodes under one conditional, not one node with arithmetic
        /// in it.</b> "instead" replaces the amount rather than adding to it, so the raised arm is the
        /// conditional's then-child and the ordinary arm is its else-child; exactly one of them ever
        /// applies, which is what the word means.</para>
        /// </summary>
        private static EffectDefinition CreateSkald(
            CardDefinitionId cardId, string contentRevision, int ordinaryCoins, int raisedCoins)
        {
            var root = new EffectNodeId("skald-root");
            var select = new EffectNodeId("skald-target");
            var history = new EffectNodeId("skald-history");
            var branch = new EffectNodeId("skald-branch");
            var raised = new EffectNodeId("skald-raised");
            var ordinary = new EffectNodeId("skald-ordinary");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, history, branch }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            // "other than the attacker" is a Block rider's clause and this is a Skill;
                            // there is no enclosing Attack and the face names no such exception.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        history, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.HeroDefeatedThisRoundV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("defeated", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        branch, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { raised, ordinary },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", history, "defeated", EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    CoinGrant(raised, select, raisedCoins),
                    CoinGrant(ordinary, select, ordinaryCoins)
                });
        }

        /// <summary>One arm of a Skald's sentence: the named hero gains exactly this many coins.</summary>
        private static EffectNodeDefinition CoinGrant(
            EffectNodeId nodeId, EffectNodeId recipientNodeId, int coins) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.GainResourceV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "recipient", recipientNodeId,
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("recipient", EffectValueKind.Hero),
                    new EffectOutputBinding("amount", EffectValueKind.Integer)
                });

        /// <summary>
        /// "Target a unit adjacent to you" as a selection, which is what the adjacent bullet of a modal
        /// Ranged Attack is: the card's own reach rather than p.13's subtype reach, because the bullet
        /// says adjacent and the icon says two.
        /// </summary>
        private static IEnumerable<EffectInputBinding> AdjacentEnemyUnit() => new[]
        {
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.OriginParameter,
                RosterQueryOrigin.Actor),
            EffectInputBinding.FromLiteral(
                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.UnitKindParameter,
                RosterUnitKind.Unit),
            EffectInputBinding.FromClosedVocabulary(
                SyntheticRosterCapabilities.RelationParameter,
                SourceTeamRelation.OpposingSource),
            EffectInputBinding.FromClosedVocabulary(
                "activity", RuleActivity.Attack)
        };

        /// <summary>
        /// The node that names the enclosing Attack's target from a unit an earlier node chose. It
        /// performs no Attack: the capability publishes a typed fact and the Attack's own authority
        /// reads it, which is the transport every other cross-altitude request in this build uses.
        /// </summary>
        private static EffectNodeDefinition BindAttackTarget(
            EffectNodeId nodeId, EffectNodeId sourceNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, sourceNodeId,
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, sourceNodeId,
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                        EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                        EffectValueKind.Minion)
                });

        /// <summary>
        /// One node: the capability that creates this card's declared Active effect. Three of Brogan's
        /// four persistent texts compose to this shape and none of them needs more - everything each
        /// sentence says is definition content on the Active effect itself.
        /// </summary>
        private static EffectDefinition CreateActiveEffect(
            EffectDefinitionId effectId,
            string contentRevision,
            EffectCapabilityReference capability,
            string nodePrefix)
        {
            var root = new EffectNodeId(nodePrefix + "-root");
            return new EffectDefinition(
                effectId,
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// The acting hero's own space, published so a following node can name the piece the card's
        /// unqualified "Move" refers to. Written once because four of Brogan's compositions need it.
        /// </summary>
        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        private const string ActorSpaceOutput = "space";
        private const string FormerSpaceOutput = "space";
        private const string ChargeDestinationOutput = "destination";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

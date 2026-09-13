using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Cutter the Sky Pirate's printed card text composes to - milestone 8.39h.
    ///
    /// <para><b>Every contract these bodies name was built before them, which is what the thirteen
    /// lettered blocks of this series were for.</b> Nothing here mints a capability: the charge line's
    /// obstacle permission is 8.39e's, the lunge's heading narrowing and the retreat's continued line
    /// are 8.39a's, the subject-adjacency narrowing is 8.39d's, the excluding repeat is 8.39d1's, the
    /// zone-crossing pair a push publishes is 8.39c's, the obstacle selection is 8.39b's, the
    /// previous-slot borrow pool is 8.39f's and the solo-victory claim is 8.39g1's.</para>
    ///
    /// <para><b>One half of one clause was NOT built before this file, and the correction is recorded
    /// where it belongs rather than worked around.</b> Milestone 8.39d wrote that "and not adjacent to
    /// you" was "already registered as <c>target-is-not-adjacent@1</c> and needs nothing", and her
    /// shard's mapping row said the same. That capability asks whether an enclosing ATTACK's bound
    /// target is adjacent to the actor at p.14's step 7 - Misa's Thunder Shot - and Bombardment,
    /// Barrage and Broadside are Skills with no attack target to ask about. The narrowing is a fifth
    /// optional boolean on <c>choose-target@2</c>, spelled with the printed-icon family's own
    /// <c>excludeAdjacent</c> constant so one clause has one name in this catalog. Composition is where
    /// this project expects such things to surface, and it is the eighth time in this series that a
    /// block or a mapping row named the wrong contract.</para>
    ///
    /// <para><b>Two of her declarations are deliberately unregistered as this file is written</b> -
    /// <c>synthetic-range-obstacle-target@1</c> (8.39b) and <c>claim-solo-victory@1</c> (8.39g1) - both
    /// new handler types held out of the build's registration list by milestone 7.9's deferral gate
    /// until a dealt card can start them. Composing a face that names one is exactly what the gate
    /// waits for; her dealing at 8.39i is what registers them and adds the showcase consumers the
    /// gate's second half requires.</para>
    /// </summary>
    public static class CutterEffectContent
    {
        private const string ActorSpaceOutput = "space";
        private const string LineDestinationOutput = "destination";
        private const string ChargeDestinationOutput = "destination";
        private const string TargetSpaceOutput = "space";
        private const string ActorNoteOutput = "hero";

        // -----------------------------------------------------------------------------------------
        // p.14's Attack text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of her five Red faces applies at the p.14 moment
        /// <see cref="AttackTextTimingFor"/> names, or <c>null</c> for a face whose whole printed text
        /// is its targeting sentence. No face of hers is in that second group.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(CutterCardContent.DaringStrike))
            {
                return CreateLunge(cardId, contentRevision, spaces: 1);
            }

            if (cardId.Equals(CutterCardContent.BoldThrust))
            {
                return CreateLunge(cardId, contentRevision, spaces: 2);
            }

            if (cardId.Equals(CutterCardContent.FearlessLunge))
            {
                return CreateLunge(cardId, contentRevision, spaces: 3);
            }

            if (cardId.Equals(CutterCardContent.EvasiveShot))
            {
                return CreateRetreat(cardId, contentRevision, spaces: 2);
            }

            if (cardId.Equals(CutterCardContent.TumbleShot))
            {
                return CreateRetreat(cardId, contentRevision, spaces: 3);
            }

            return null;
        }

        /// <summary>
        /// Which p.14 moment one of her Red faces applies its text at. Her three melee lunges print
        /// "Before the attack:" inside their first bullet and her two Ranged shots print "After the
        /// attack:", so her Red line uses both ends of the page and nothing in between.
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(CutterCardContent.DaringStrike) ||
                cardId.Equals(CutterCardContent.BoldThrust) ||
                cardId.Equals(CutterCardContent.FearlessLunge))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            if (cardId.Equals(CutterCardContent.EvasiveShot) ||
                cardId.Equals(CutterCardContent.TumbleShot))
            {
                return CardTextTiming.AfterTheAttack;
            }

            return CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // p.12: the Skills
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one of her Skill faces resolves as, or <c>null</c> for the one face that carries a
        /// composition debt: p.20's Handicap card, which p.6 returns to the box.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(CutterCardContent.WalkThePlank))
            {
                return CreatePlankModal(cardId, contentRevision);
            }

            if (cardId.Equals(CutterCardContent.GrapplingBolt))
            {
                return CreateGrapple(cardId, contentRevision);
            }

            if (cardId.Equals(CutterCardContent.Bombardment))
            {
                return CreateBombardment(cardId, contentRevision, radius: 3, repeats: false);
            }

            if (cardId.Equals(CutterCardContent.Barrage))
            {
                return CreateBombardment(cardId, contentRevision, radius: 4, repeats: false);
            }

            if (cardId.Equals(CutterCardContent.Broadside))
            {
                return CreateBombardment(cardId, contentRevision, radius: 4, repeats: true);
            }

            if (cardId.Equals(CutterCardContent.XMarksTheSpot))
            {
                return CreateEnemyChosenModal(cardId, contentRevision, coins: 2, claimsVictory: false);
            }

            if (cardId.Equals(CutterCardContent.AFistfulOfCoins))
            {
                return CreateEnemyChosenModal(cardId, contentRevision, coins: 3, claimsVictory: true);
            }

            if (cardId.Equals(CutterCardContent.BraceForImpact))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 3, mostDistance: 3);
            }

            if (cardId.Equals(CutterCardContent.RammingSpeed))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 3, mostDistance: 4);
            }

            if (cardId.Equals(CutterCardContent.Crashland))
            {
                return CreateCharge(cardId, contentRevision, leastDistance: 3, mostDistance: 5);
            }

            if (cardId.Equals(CutterCardContent.Outmaneuver))
            {
                return CreateMinionSwap(cardId, contentRevision, spaces: 2);
            }

            if (cardId.Equals(CutterCardContent.Outsmart))
            {
                return CreateMinionSwap(cardId, contentRevision, spaces: 3);
            }

            return null;
        }

        /// <summary>
        /// p.16's permanent ability: "The first time each turn after you perform a primary action, you
        /// may perform the primary action of a card in the previous turn slot."
        ///
        /// <para><b>Both halves of the sentence are declarations rather than nodes.</b> "The first time
        /// each turn after you perform a primary action" is
        /// <see cref="UltimateTriggerCondition.AfterControllerPerformsPrimaryAction"/>, minted at
        /// milestone 8.39f1 because every one of the five conditions before it is matched against a
        /// fact that is undefined unless the card is p.9-Basic - and Cutter's Tier IV is unlocked when
        /// most of what she plays is not. The once-per-turn bound is that condition's own, riding the
        /// turn's existing consumed-key set.</para>
        ///
        /// <para><b>The pool is milestone 8.39f's fourth member and not Swift's third.</b>
        /// <see cref="BorrowedActionPool.PreviousTurnSlotCard"/> names a SLOT and takes what stands in
        /// it; <see cref="BorrowedActionPool.OwnedRightmostResolvedCard"/> names a position in an order
        /// and walks back until it finds a card still its owner's. They agree on an ordinary turn and
        /// part company on a slot holding no card of hers, where this one is empty and Swift's reaches
        /// past - which is the reading her face prints, because it names one slot.</para>
        ///
        /// <para>The colour is <see cref="CardColor.None"/> because the face prints none, exactly as
        /// Swift's and Ursafar's borrows do.</para>
        /// </summary>
        public static EffectDefinition LegendOfTheSkiesTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("legend-root");
            var borrow = new EffectNodeId("legend-borrow");

            return new EffectDefinition(
                new EffectDefinitionId(CutterCardContent.LegendOfTheSkies.Value + "-ultimate"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrow }),

                    // "you MAY perform" - p.10 gives an unassigned decision to the card's controller,
                    // and this one is unassigned.
                    new EffectNodeDefinition(
                        borrow, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticBorrowedActionCapabilities.PoolParameter,
                                BorrowedActionPool.PreviousTurnSlotCard),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticBorrowedActionCapabilities.ColorParameter, CardColor.None)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// Every triggered effect Cutter's content declares, for a fixture or a composition root that
        /// configures them. Hers is exactly one: p.16's permanent ability. No face of hers prints
        /// "This turn:", "Next turn:", "End of turn:" or any other Active-effect keyword, so she
        /// contributes no Active-effect definition at all - an explicit empty contribution rather than
        /// a missing one, which is the distinction Ursafar's own set records.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects(string contentRevision) =>
            Array.AsReadOnly(new[] { LegendOfTheSkiesTriggeredEffect(contentRevision) });

        // -----------------------------------------------------------------------------------------
        // The bodies
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose one — • Push an enemy hero adjacent to you up to 4 spaces; if that hero is pushed
        /// into another zone, that hero discards a card, or is defeated. • Defeat a minion adjacent to
        /// you." - Walk the Plank, her Gold basic.
        ///
        /// <para><b>The push names its object through a selection rather than through the batch's own
        /// count, and the printed noun is why.</b> p.18's batch Push takes the objects standing beside
        /// the pusher and narrows them by side and by count, which is how Wasp's and Arien's faces
        /// compose - but its piece kinds are p.19's "unit or token" and "unit", and this face prints
        /// "an enemy HERO". So a radius-1 <c>choose-target@2</c> over heroes names the object and
        /// <c>synthetic-push-target@3</c> pushes the space it published, which is Sabina's Point Blank
        /// Shot's shape with a chosen target in place of an attacked one.</para>
        ///
        /// <para><b>"Up to 4 spaces" is the distance's own optionality and not the step's.</b> p.11
        /// separates the two in as many words, so the node carries <see cref="EffectFailurePolicy.UpTo"/>
        /// with a floor of nought - the actor may push the hero anywhere from nowhere to four - while
        /// the selection above it stays mandatory, because the bullet does not say "you may".</para>
        ///
        /// <para><b>The zone test is the push's own published pair, which is milestone 8.39c's
        /// correction to this row.</b> The first audit read "if that hero is pushed into another zone"
        /// as <c>after-push-trigger@1</c>, which is how a p.16 permanent ability watches a push some
        /// OTHER card performed - Sabina's Big Sodding Gun. This is a consequence on the card doing the
        /// pushing, which is Wasp's Kinetic Repulse shape: a rider read off the push's own outputs.
        /// The crossing is judged on origin against final destination and never on the spaces between,
        /// so a push that leaves a zone and re-enters it has carried nobody anywhere.</para>
        ///
        /// <para><b>"Discards a card, or is defeated" is one mandatory step with two ways to
        /// complete</b>, which rule-system ledger question 13 settled on Arien's identical sentence and
        /// which Whisper's Greater Good already composes: the Defeat is reached only where the discard
        /// could not complete, and p.10 gives nobody a decision at it. The contrast is printed on this
        /// same hero's own Blue and Green lines, whose discards print "if able" and silently skip.</para>
        /// </summary>
        private static EffectDefinition CreatePlankModal(
            CardDefinitionId cardId, string contentRevision, int spaces = 4)
        {
            var root = new EffectNodeId("plank-root");
            var pushArm = new EffectNodeId("plank-push-arm");
            var victim = new EffectNodeId("plank-victim");
            var push = new EffectNodeId("plank-push");
            var crossing = new EffectNodeId("plank-crossing");
            var consequence = new EffectNodeId("plank-consequence");
            var discard = new EffectNodeId("plank-discard");
            var escalation = new EffectNodeId("plank-escalation");
            var defeatHero = new EffectNodeId("plank-defeat-hero");
            var minionArm = new EffectNodeId("plank-minion-arm");
            var minion = new EffectNodeId("plank-minion");
            var defeatMinion = new EffectNodeId("plank-defeat-minion");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                PushSequence(
                        root, pushArm, victim, push, crossing, consequence, discard, escalation,
                        defeatHero, minionArm, spaces)
                    .Concat(new[]
                    {
                        new EffectNodeDefinition(
                            minionArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { minion, defeatMinion }),

                        // "a minion adjacent to you" is a distance the sentence writes down rather
                        // than a printed icon - Walk the Plank prints neither Range nor Radius - which
                        // is the corrective milestone 8.38e recorded for Swift's identical bullet.
                        AdjacentEnemyMinionNode(minion),
                        new EffectNodeDefinition(
                            defeatMinion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticConsequenceCapabilities.DefeatMinionV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticConsequenceCapabilities.MinionParameter, minion,
                                    SyntheticRosterCapabilities.ChosenMinionOutput,
                                    EffectValueKind.Minion)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                                    EffectValueKind.Minion)
                            })
                    })
                    .ToArray());
        }

        /// <summary>
        /// "Push an enemy hero adjacent to you up to N spaces; if that hero is pushed into another
        /// zone, that hero discards a card, or is defeated." - the whole of Few More Steps and the
        /// first bullet of Walk the Plank, shared because they are one sentence printed twice at two
        /// numbers.
        ///
        /// <para>The root is handed in rather than minted here, because Walk the Plank's is p.10's
        /// modal and Few More Steps' is the sentence itself. Few More Steps composes nothing today -
        /// p.20's standing deferral - and the shape is still written once, so the milestone that
        /// unparks it has nothing to re-derive.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> PushSequence(
            EffectNodeId root,
            EffectNodeId pushArm,
            EffectNodeId victim,
            EffectNodeId push,
            EffectNodeId crossing,
            EffectNodeId consequence,
            EffectNodeId discard,
            EffectNodeId escalation,
            EffectNodeId defeatHero,
            EffectNodeId minionArm,
            int spaces) =>
            new[]
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { pushArm, minionArm },

                    // "Choose ONE —": exactly one arm, and p.10 gives an unassigned decision to the
                    // card's controller. A zero minimum would be "up to one", which is a different
                    // sentence and one this face does not print.
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                new EffectNodeDefinition(
                    pushArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { victim, push, crossing }),
                new EffectNodeDefinition(
                    victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),

                        // A Skill's own activity is what p.18 asks immunity about here; there is no
                        // enclosing Attack to inherit one from.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    push, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.PushTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "target", victim, SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(spaces)),

                        // "up to N spaces" makes the DISTANCE optional and not the step, which is
                        // p.11's own separation and the reason the floor is nought.
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                            EffectValue.Integer(0)),

                        // The face says nothing about obstacles, and p.18 makes crossing a property of
                        // the pushing effect rather than a default, so it is declared false.
                        EffectInputBinding.FromLiteral(
                            "ignoresObstacles", EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            PushEligiblePieceKind.Unit)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("distance", EffectValueKind.Integer),
                        new EffectOutputBinding("moved", EffectValueKind.Integer),
                        new EffectOutputBinding(
                            SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticDisplacementCapabilities.FirstHeroCrossedAZoneOutputName,
                            EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    crossing, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { consequence },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", push,
                            SyntheticDisplacementCapabilities.FirstHeroCrossedAZoneOutputName,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    consequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { discard, escalation }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, push,
                            SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeatHero },
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
                    }),
                new EffectNodeDefinition(
                    defeatHero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, push,
                            SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    })
            };

        /// <summary>
        /// "Target an obstacle in range and in a straight line, with no obstacles between you; ignore
        /// immunity. Move in a straight line towards that obstacle until you are adjacent." - Grappling
        /// Bolt, her Silver basic and the only face in the corpus that targets an obstacle.
        ///
        /// <para><b>The target is a SPACE, and that is rulebook fidelity rather than a workaround.</b>
        /// p.18 defines an obstacle as units, terrain, tokens and any object represented by a
        /// miniature, and it is emphatic that "there can never be more than one obstacle in any one
        /// space" - so an obstacle is named one-to-one by where it stands, and p.19's own Target
        /// keyword already reads "Target (Unit / Token / Space)". That is milestone 8.39b's measurement
        /// and it is why no enum here gained an obstacle member.</para>
        ///
        /// <para><b>"Ignore immunity" is a declaration on the node, not a branch inside it.</b>
        /// <c>EffectNodeDefinition.ImmunityOverrides</c> is the built system and
        /// <c>EffectDefinitionValidator</c> already enforces it, so the clause is stated once where the
        /// targeting happens. The reason is <see cref="ImmunityOverrideReason.PrintedUnqualifiedImmunity"/>
        /// - p.18's Immune keyword is written to be overridden in exactly these words, "unless the card
        /// text says otherwise", and this sentence is that card text with no qualifier on it.</para>
        ///
        /// <para><b>The Move is "if able" because arriving is not always a step.</b> An obstacle
        /// already adjacent publishes the actor's own space as the approach - "until you are adjacent"
        /// is satisfied where she stands - and a Move to the space its mover already occupies has no
        /// destination to offer. That is the ONE position this policy covers: the same sentence prints
        /// "with no obstacles between you", so every space on the admitted line is empty by
        /// construction and nothing else can stop her.</para>
        ///
        /// <para>The Move's declared distance is a budget rather than a count, because the destination
        /// is named; six is chosen so it cannot under-serve the longest approach p.16 can produce from
        /// a printed Range of five.</para>
        /// </summary>
        private static EffectDefinition CreateGrapple(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("grapple-root");
            var obstacle = new EffectNodeId("grapple-obstacle");
            var actorSpace = new EffectNodeId("grapple-actor-space");
            var move = new EffectNodeId("grapple-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { obstacle, actorSpace, move }),
                    new EffectNodeDefinition(
                        obstacle, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeObstacleTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.All)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName,
                                EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        immunityOverrides: new[]
                        {
                            new ImmunityOverrideDeclaration(
                                RuleActivity.All,
                                ImmunityOverrideReason.PrintedUnqualifiedImmunity)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(6)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                obstacle,
                                SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Choose one — • Before the attack: Move N space[s] in a straight line. Target a hero
        /// adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent to you." -
        /// Daring Strike at one space, Bold Thrust at one or two, Fearless Lunge at one, two or three.
        ///
        /// <para><b>The move is a straight-line destination and NOT a charge line, which is milestone
        /// 8.39a's correction.</b> Brogan's <c>choose-charge-line@1</c> offers only the spaces with an
        /// enemy unit beside them, because his sentence prints that clause; these three print no
        /// destination clause at all, so every space the line reaches is offered and the victim is a
        /// separate question. <c>choose-line-destination@1</c> walks 1..N, never nought, so "Move 1
        /// space" is exactly one and "Move 1 or 2 spaces" is exactly what it says.</para>
        ///
        /// <para><b>"In the direction of the move" is a narrowing on the victim's own selection.</b>
        /// The line runs from the space this arm started on, through where she now stands, and on;
        /// <c>continuedLineFrom</c> is milestone 8.39a's parameter for exactly that, and it narrows a
        /// radius-1 disk of six neighbours to the one that continues the heading. A heading the actor
        /// never travelled admits nobody rather than everybody, which is why the arm's Move is
        /// mandatory.</para>
        ///
        /// <para><b>The +2 is NOT here, and that is the whole of milestone 8.39g2.</b> Every printed
        /// <c>+N</c> in this build is a <c>RosterValueContributionDefinition</c> declared on the ACTION
        /// and scaled by a <see cref="RosterValueQueryKind"/>, and an action-level declaration cannot be
        /// arm-conditional. <see cref="RosterValueQueryKind.ActorMovedThisAction"/> is what makes it
        /// belong to this arm anyway: it answers the question <c>actor-moved-this-action@1</c> asks,
        /// from the value pipeline, and it is equivalent to "arm one was taken" precisely because the
        /// Move inside arm one is mandatory - CU-Q4's answer, settled there.</para>
        ///
        /// <para><b>Neither arm performs the Attack.</b> The card declares
        /// <see cref="AttackTargetBinding.ByCardText"/>, so the action reaches p.14's step 1 with no
        /// target; each arm binds one through <c>set-attack-target@1</c> and the Attack resolves
        /// against it from there. The first arm binds a HERO alone, because that is the noun its bullet
        /// prints; the second binds either, because "a unit" is p.19's pair.</para>
        /// </summary>
        private static EffectDefinition CreateLunge(
            CardDefinitionId cardId, string contentRevision, int spaces)
        {
            var root = new EffectNodeId("lunge-root");
            var lungeArm = new EffectNodeId("lunge-move-arm");
            var actorSpace = new EffectNodeId("lunge-actor-space");
            var line = new EffectNodeId("lunge-line");
            var move = new EffectNodeId("lunge-move");
            var lungeVictim = new EffectNodeId("lunge-victim");
            var lungeBind = new EffectNodeId("lunge-bind");
            var plainArm = new EffectNodeId("lunge-plain-arm");
            var plainVictim = new EffectNodeId("lunge-plain-victim");
            var plainBind = new EffectNodeId("lunge-plain-bind");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { lungeArm, plainArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        lungeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move, lungeVictim, lungeBind }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(spaces))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                LineDestinationOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, LineDestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        lungeVictim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),

                            // "a hero", which is p.19's hero and not its unit - the one word that
                            // separates this bullet's target set from the other's.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticRosterCapabilities.ContinuedLineFromParameter,
                                actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        lungeBind, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter, lungeVictim,
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        plainArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { plainVictim, plainBind }),
                    new EffectNodeDefinition(
                        plainVictim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
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
                    new EffectNodeDefinition(
                        plainBind, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter, plainVictim,
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.MinionParameter, plainVictim,
                                SyntheticRosterCapabilities.ChosenMinionOutput,
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
                });
        }

        /// <summary>
        /// "After the attack: Move up to N spaces in the opposite direction." - Evasive Shot at two and
        /// Tumble Shot at three, the riders on her two Ranged Attacks.
        ///
        /// <para><b>"The opposite direction" is a heading the Attack already named, not one chosen
        /// again.</b> The line ran from the space she attacked, through where she stands; the retreat
        /// continues it. So <c>attacked-target-space@1</c> publishes the far end and milestone 8.39a's
        /// <c>continuedLineFrom</c> narrows <c>choose-line-destination@1</c> to that one file of the
        /// six it already walks. Nothing here re-derives a direction and nothing asks the player for
        /// one.</para>
        ///
        /// <para><b>The attacked space is read rather than the target.</b> p.14's step 6 may have
        /// removed the defender before this text runs, and a piece standing nowhere has no space - so
        /// this is the capability Sabina's Point Blank Shot already uses for the same reason, and the
        /// retreat works identically whether the Attack killed or not.</para>
        ///
        /// <para><b>Both nodes carry "up to" and that is the printed distance's own optionality.</b>
        /// The chooser offers 1..N and never nought, and the <see cref="EffectFailurePolicy.UpTo"/>
        /// policy is what turns an offer she declines - or an empty one, when the file is blocked or
        /// the target space is her own - into a step simply not taken rather than a Skill that
        /// stops.</para>
        /// </summary>
        private static EffectDefinition CreateRetreat(
            CardDefinitionId cardId, string contentRevision, int spaces)
        {
            var root = new EffectNodeId("retreat-root");
            var targetSpace = new EffectNodeId("retreat-target-space");
            var actorSpace = new EffectNodeId("retreat-actor-space");
            var line = new EffectNodeId("retreat-line");
            var move = new EffectNodeId("retreat-move");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { targetSpace, actorSpace, line, move }),
                    new EffectNodeDefinition(
                        targetSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(TargetSpaceOutput, EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticRosterCapabilities.ContinuedLineFromParameter,
                                targetSpace, TargetSpaceOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                LineDestinationOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, LineDestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "An enemy hero adjacent to another enemy unit and not adjacent to you discards a card, if
        /// able[. May repeat once on a different target]." - Bombardment at Radius 3, Barrage at Radius
        /// 4, and Broadside at Radius 4 with the repeat.
        ///
        /// <para><b>Three definitions and not one, deliberately.</b> Bombardment and Barrage print
        /// byte-identical text and differ only in the Radius their icon carries; Broadside prints that
        /// text plus one sentence. Whisper's Shadow Step and Shadow Walk are the precedent and the
        /// reason: the Radius is an icon rather than a word, and a shared definition would let a p.16
        /// Radius item raise the wrong face.</para>
        ///
        /// <para><b>Two narrowings measured from two different places, both on the selection.</b> The
        /// disk is measured from Cutter; the adjacency is measured from each CANDIDATE, for units on
        /// the candidate's own side, with the candidate excluded from its own neighbourhood - p.9
        /// measures a disk inclusively, so without that exclusion every enemy hero on the board would
        /// qualify by standing next to itself. And "not adjacent to you" drops p.9's innermost ring,
        /// which is the half milestone 8.39d recorded as registered against a capability that reads an
        /// Attack's target. Both are clauses about who may be picked, so neither is a step that offers
        /// a hero and then refuses them.</para>
        ///
        /// <para><b>"May repeat once on a different target" is p.10's repeat with milestone 8.39d1's
        /// fifth binding.</b> The three <c>Different*</c> arms that existed before it all exclude an
        /// ATTACK's own target, stored on the pending attack resolution - state a Skill has none of.
        /// Broadside's "target" is the subject a card-text selection chose, so the repeat names that
        /// node and the exclusion is applied where the candidates are derived. p.10 gives an
        /// unassigned decision to the card's controller, and "may repeat once" assigns none.</para>
        /// </summary>
        private static EffectDefinition CreateBombardment(
            CardDefinitionId cardId, string contentRevision, int radius, bool repeats)
        {
            var root = new EffectNodeId("bombard-root");
            var subject = new EffectNodeId("bombard-subject");
            var discard = new EffectNodeId("bombard-discard");
            var repeat = new EffectNodeId("bombard-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeats
                        ? new[] { subject, discard, repeat }
                        : new[] { subject, discard }),
                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter,
                            EffectValue.Integer(radius)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.AdjacentToAnotherEnemyUnitParameter,
                            EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.ExcludeAdjacentParameter,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    }),

                // "if able" is the skipping kind: a subject holding nothing gives up nothing and the
                // Skill goes on. The contrast is printed on this hero's own Gold basic, whose push
                // rider escalates instead.
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, subject,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    })
            };

            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentSelectionSubject,
                    boundSelectionNodeId: subject));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "An enemy hero in radius chooses one — • You place that hero in a space in radius. • You
        /// gain N coins[. If you have 13 or more coins, you alone win the game]." - X Marks the Spot at
        /// two coins and A Fistful of Coins at three with the victory sentence.
        ///
        /// <para><b>The modal's chooser is the enemy, which is Whisper's seam and not a new one.</b>
        /// Her Lesser Evil and Greater Good already print "An enemy hero in range chooses one —", and
        /// these two print the identical construction with "in radius". So the selection names the
        /// hero, the <see cref="EffectNodeKind.Choose"/> binds that hero as its chooser through
        /// <see cref="EffectChooserRole.BoundHero"/>, and the bullets are its children.</para>
        ///
        /// <para><b>Both bullets are the ACTOR's to carry out, and only the choice between them is the
        /// enemy's.</b> "YOU place that hero" and "YOU gain N coins" - the enemy picks which thing
        /// happens to them and Cutter does it. The Place is measured from Cutter, which is what
        /// <see cref="PlaceDestinationAnchor.ActingHero"/> states, and its subject is the space the
        /// selection published rather than the hero's identity, because p.18's Place moves a piece by
        /// where it stands.</para>
        ///
        /// <para><b>The victory sentence is a node, not a rule the coin grant knows about.</b>
        /// <c>claim-solo-victory@1</c> is milestone 8.39g1's seam: it reads the live ledger at the
        /// branch, writes the winner when the printed threshold is met, and returns - the match ends at
        /// the same command boundary every other mid-turn ending is noticed at, because a capability
        /// must never call <c>EndGame</c> from inside the frame that is still running. The threshold is
        /// a declared parameter because thirteen is a number the CARD prints, which is the discipline
        /// every printed distance in this catalog follows. It is mandatory rather than conditional: the
        /// clause is reached whatever the balance, and a hero holding twelve has resolved the whole
        /// face correctly.</para>
        /// </summary>
        private static EffectDefinition CreateEnemyChosenModal(
            CardDefinitionId cardId, string contentRevision, int coins, bool claimsVictory)
        {
            var root = new EffectNodeId("chosen-root");
            var victim = new EffectNodeId("chosen-victim");
            var modal = new EffectNodeId("chosen-modal");
            var placeArm = new EffectNodeId("chosen-place-arm");
            var place = new EffectNodeId("chosen-place");
            var coinArm = new EffectNodeId("chosen-coin-arm");
            var actorNote = new EffectNodeId("chosen-actor");
            var gain = new EffectNodeId("chosen-gain");
            var claim = new EffectNodeId("chosen-claim");

            var coinChildren = claimsVictory
                ? new[] { actorNote, gain, claim }
                : new[] { actorNote, gain };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { victim, modal }),
                new EffectNodeDefinition(
                    victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(3)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),

                        // p.18's Place takes a space, and a selection whose consumer is a Place has to
                        // publish where its answer is standing as well as who it is.
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { placeArm, coinArm },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            EffectNodeDefinition.ChooserParameterName, victim,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    chooser: EffectChooserRole.BoundHero,
                    minimumSelections: 1,
                    maximumSelections: 1),
                new EffectNodeDefinition(
                    placeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { place }),
                new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.PlaceInRadiusV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", victim, SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate),

                        // "that hero" - p.19's unit, which is the breadth the sentence names.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.UnitOrToken),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),

                        // The face refuses no Spawn point, so this declaration refuses none.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    coinArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: coinChildren),

                // "YOU gain" - the grant names its recipient, and the recipient is the card's own
                // actor, so the frame's actor is published as a hero for the grant to bind.
                new EffectNodeDefinition(
                    actorNote, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.NoteActorV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(ActorNoteOutput, EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    gain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "recipient", actorNote, ActorNoteOutput, EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    })
            };

            if (claimsVictory)
            {
                nodes.Add(new EffectNodeDefinition(
                    claim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ClaimSoloVictoryV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.SoloVictoryThresholdParameter,
                            EffectValue.Integer(SoloVictoryThreshold))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.SoloVictoryClaimedOutput,
                            EffectValueKind.Boolean),
                        new EffectOutputBinding("winner", EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Move N spaces in a straight line, ignoring obstacles, to a space adjacent to an enemy hero;
        /// that hero discards a card, if able." - Brace for Impact at exactly three, Ramming Speed at
        /// three or four, Crashland at three, four or five.
        ///
        /// <para><b>This is Brogan's charge with one parameter added and one row corrected.</b>
        /// <c>choose-charge-line@1</c> has separated <c>minimumDistance</c> from <c>maximumDistance</c>
        /// since milestone 8.19b and its <c>unitKind</c> domain has always admitted
        /// <see cref="RosterUnitKind.Hero"/>, so the exact distance, the enumerated distances and the
        /// adjacency narrowing were all registered before this series began. The only gap was
        /// "ignoring obstacles", which milestone 8.39e added as one optional Boolean - and the
        /// permission reaches the transit predicate only, so p.18's second sentence still forbids
        /// stopping on an obstacle: these faces cross more ground, not more endpoints.</para>
        ///
        /// <para><b>"That hero" is named AFTER the Move, by a separate radius-1 selection, and the
        /// audit row that said otherwise was corrected at 8.39e.</b> The charge offers a set of
        /// DESTINATIONS: it keeps the qualifying heroes only long enough to decide whether a space is
        /// offered at all and then collapses them away, so a destination beside two enemy heroes is
        /// offered once and answering it publishes a space and nothing else. The disk the selection
        /// then measures is the same one the charge measured, because she is standing on it - and on a
        /// destination beside two it offers both and the player chooses, which is what the printed text
        /// leaves to them. Putting the question back inside the charge node is exactly what milestone
        /// 8.35p took out, on the user's rule that "they move as soon as they select, and then they
        /// attack".</para>
        ///
        /// <para>The Move declares the crossing as well, because the charge's permission is about which
        /// destinations are offered and the Move is what travels.</para>
        /// </summary>
        private static EffectDefinition CreateCharge(
            CardDefinitionId cardId, string contentRevision, int leastDistance, int mostDistance)
        {
            var root = new EffectNodeId("charge-root");
            var actorSpace = new EffectNodeId("charge-actor-space");
            var line = new EffectNodeId("charge-destination");
            var move = new EffectNodeId("charge-move");
            var victim = new EffectNodeId("charge-victim");
            var discard = new EffectNodeId("charge-discard");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move, victim, discard }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseChargeLineV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(leastDistance)),
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(mostDistance)),

                            // "an enemy hero", which p.19 separates from its minion by name and which
                            // this declaration's domain has always admitted.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ChargeDestinationOutput, EffectValueKind.Coordinate)
                        }),
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
                                line, ChargeDestinationOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        victim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter, victim,
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "Swap with an enemy minion in radius; you may move that minion up to N spaces." -
        /// Outmaneuver at two spaces and Outsmart at three.
        ///
        /// <para><b>The audit row for these two named the wrong pair, and reading the schemas is what
        /// found it.</b> It cited <c>choose-swap-subject@1</c>, which is Whisper's UNION selection -
        /// "a unit adjacent to you, or an enemy hero in range with a card in the discard" - and whose
        /// whole schema is that union's three parameters, with no reach of its own to set. Cutter's
        /// sentence is Sabina's Back to Back with the side reversed: a printed-Radius minion selection
        /// and p.19's Swap with the actor fixed as one of its two objects. That pair is registered and
        /// this is it.</para>
        ///
        /// <para><b>It is <c>synthetic-swap-actor@1</c> rather than the general Swap</b> for the reason
        /// Sabina's records: the actor is one of the two objects by the sentence's own construction, so
        /// a selection that could offer her as the OTHER one would be offering a swap with herself.</para>
        ///
        /// <para><b>The minion the move takes is the one the Swap just displaced, read from the Swap's
        /// own published pair.</b> After the exchange the minion is standing where Cutter was, and
        /// re-deriving that from the selection's coordinate would be a second source of truth about one
        /// step. p.19 states that a Swap "does not count as movement or placement", so nothing about
        /// the exchange spends the move's budget.</para>
        ///
        /// <para><b>"You may move ... up to N spaces" carries both of p.11's optionalities and the node
        /// policy carries both at once</b>: <see cref="EffectFailurePolicy.UpTo"/> with a floor of
        /// nought makes moving nowhere a legal outcome, which is exactly what declining is. The
        /// destination is unrestricted because the sentence names none.</para>
        /// </summary>
        private static EffectDefinition CreateMinionSwap(
            CardDefinitionId cardId, string contentRevision, int spaces)
        {
            var root = new EffectNodeId("swap-root");
            var subject = new EffectNodeId("swap-subject");
            var swap = new EffectNodeId("swap-exchange");
            var move = new EffectNodeId("swap-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, swap, move }),
                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),

                            // Milestone 8.35l: the Swap below decides whether a minion this offers can
                            // actually be exchanged, so the offer and the operation cannot disagree.
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapWithActorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", subject,
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
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", swap, "subjectAt", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(spaces)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The number A Fistful of Coins prints. It lives here rather than in the capability for the
        /// reason every printed distance in this catalog does: the threshold is the CARD's, and a
        /// second face printing a different one would bind a different number to the same contract.
        /// </summary>
        public const int SoloVictoryThreshold = 13;

        /// <summary>
        /// "A minion adjacent to you" - the second bullet of Walk the Plank, at a distance its own
        /// sentence writes down rather than at a printed icon, because the face prints neither Range
        /// nor Radius.
        /// </summary>
        private static EffectNodeDefinition AdjacentEnemyMinionNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromLiteral(
                        SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),

                    // p.13 forbids defeating friendly units; the card overrides nothing, and the
                    // Skill's own immunity checks apply on top.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                });

        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

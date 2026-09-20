using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Silverarrow the Pathfinder's printed card text composes to - milestone
    /// 8.35z30.
    ///
    /// <para><b>What is not here is as much of the answer as what is.</b> Six of her Attacks print a
    /// targeting sentence and nothing else - "Target a unit at maximum range", and the three Red
    /// modals' two bullets - and a targeting sentence is not an effect. It is the Attack's own
    /// <see cref="PrintedAttackTargetPolicy"/>, declared on the action, which is what keeps the offer,
    /// the target choice and the resolution reading one answer. So those six carry no root at all, and
    /// RULING 1 and RULING 39 both land on the declaration rather than on anything in this
    /// file.</para>
    ///
    /// <para><b>The three shared bodies.</b> Grasping Roots, Entangling Vines and Grappling Branches
    /// print one sentence; Fae Healing and Nature's Blessing differ only in a coin count; Divert
    /// Attention and Lead Astray only in a space count. Each pair or triple is one method here, which
    /// is milestone 8.10's Shock/Electrocute precedent and the reason nine of her nineteen faces are
    /// reachable through four bodies.</para>
    /// </summary>
    public static class SilverarrowEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // p.14 step 7: the attacker's "After the attack:" text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Attack of Silverarrow's applies, or <c>null</c> where it prints none.
        /// Paired with <see cref="AttackTextTimingFor"/>, which names which of p.14's moments it is.
        ///
        /// <para>Two of her seven Attacks compose anything at all. Shoot and Scoot follows its attack
        /// with a Fast Travel, and Rain of Arrows follows it with a repeat; the three Red modals and
        /// Long Shot print targeting alone, and Keep the Distance is the boxed Handicap card whose text
        /// is deferred.</para>
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SilverarrowCardContent.ShootAndScoot))
            {
                return CreateScoot(cardId, contentRevision);
            }

            if (cardId.Equals(SilverarrowCardContent.RainOfArrows))
            {
                return CreateArrowStorm(cardId, contentRevision);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's two card-text moments an Attack of hers applies its text at, read
        /// from the face's own words. Both faces that compose anything print "After the attack:".
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(SilverarrowCardContent.ShootAndScoot) ||
            cardId.Equals(SilverarrowCardContent.RainOfArrows)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.12: the Skills
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Skill of Silverarrow's performs. Every one of her eight Skills composes
        /// something; there is no Skill in her set whose text is a value clause.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SilverarrowCardContent.Trailblazer))
            {
                return CreateTrailblazing(cardId, contentRevision);
            }

            if (cardId.Equals(SilverarrowCardContent.GraspingRoots))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "roots",
                    SilverarrowActiveEffectContent.GraspingRootsInRadiusV1);
            }

            if (cardId.Equals(SilverarrowCardContent.EntanglingVines))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "vines",
                    SilverarrowActiveEffectContent.EntanglingVinesInRadiusV1);
            }

            if (cardId.Equals(SilverarrowCardContent.GrapplingBranches))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "branches",
                    SilverarrowActiveEffectContent.GrapplingBranchesInRadiusV1);
            }

            if (cardId.Equals(SilverarrowCardContent.WarningShot))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "warning",
                    SilverarrowActiveEffectContent.WarningShotAtEndOfTurnV1);
            }

            if (cardId.Equals(SilverarrowCardContent.TreetopSentinel))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "sentinel",
                    SilverarrowActiveEffectContent.TreetopSentinelAtEndOfTurnV1);
            }

            if (cardId.Equals(SilverarrowCardContent.Disorient))
            {
                return CreateDisorient(cardId, contentRevision);
            }

            if (cardId.Equals(SilverarrowCardContent.DivertAttention))
            {
                return CreateMisdirection(cardId, contentRevision, 2);
            }

            if (cardId.Equals(SilverarrowCardContent.LeadAstray))
            {
                return CreateMisdirection(cardId, contentRevision, 3);
            }

            if (cardId.Equals(SilverarrowCardContent.FaeHealing))
            {
                return CreateBlessing(cardId, contentRevision, 1);
            }

            if (cardId.Equals(SilverarrowCardContent.NaturesBlessing))
            {
                return CreateBlessing(cardId, contentRevision, 2);
            }

            return null;
        }

        /// <summary>
        /// "After the attack: If able, you may fast travel to an adjacent zone." - Shoot and Scoot's
        /// second sentence, and Keep the Distance's when the Handicap card's deferral lifts.
        ///
        /// <para><b>Two printed qualifications, and they are not the same one.</b> "you may" is the
        /// actor's decision, which is <see cref="EffectFailurePolicy.May"/>. "If able" is p.17's own
        /// word for a step that simply does not happen when it cannot: a hero with an enemy in their
        /// Zone, or with no adjacent Zone holding an empty space, is offered nothing and the Attack is
        /// over. The capability answers the second by having no destinations, and the node's policy
        /// answers the first.</para>
        ///
        /// <para><b>"to an adjacent zone" is a narrowing of p.11 and it is declared, not assumed.</b>
        /// The page's own Fast Travel admits the hero's current Zone as well; this card does not, so
        /// the node names <see cref="FastTravelZoneScope.AdjacentZoneOnly"/> and a hero whose only
        /// eligible destination is a space in the Zone they are already standing in may not take
        /// it.</para>
        ///
        /// <para><b>Why this is not a Movement.</b> It is a Fast Travel, so p.18's restriction on
        /// <see cref="ActionKind.FastTravel"/> stops it - her own Grasping Roots would stop an enemy's
        /// - and RULING 47's separation does not arise: nothing here is a Move at all.</para>
        /// </summary>
        private static EffectDefinition CreateScoot(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("scoot-root");
            var travel = new EffectNodeId("scoot-travel");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { travel }),
                    FastTravelNode(travel, FastTravelZoneScope.AdjacentZoneOnly)
                });
        }

        /// <summary>
        /// "Target a unit at maximum range. If you target a hero, repeat once on a different hero; if
        /// you do, repeat once on a minion." - Rain of Arrows.
        ///
        /// <para><b>The first sentence is not here.</b> "at maximum range" is the Attack's own reach -
        /// <see cref="AttackTargetReach.ExactlyPrintedRange"/>, RULING 1 - and every instance of this
        /// Attack, printed or repeated, is that same action and reaches the same band. That is what
        /// RULING 38 means by every instance inheriting the full targeting sentence: it is inherited by
        /// being the action's, rather than restated per instance.</para>
        ///
        /// <para><b>The repeat is mandatory where it can happen and skipped where it cannot</b>, which
        /// is RULING 38 and is exactly <see cref="EffectFailurePolicy.IfAble"/>: the face prints no
        /// "may" where Wasp's Thunder Boomerang prints one, so nobody is asked whether to repeat - and
        /// a board with no different enemy hero left is a step that cannot happen rather than an action
        /// that stops. The node therefore names no chooser, which is the observable difference from
        /// Thunder Boomerang's identical-looking declaration.</para>
        ///
        /// <para><b>"a different hero" is milestone 8.24's binding</b>,
        /// <see cref="RepeatTargetBinding.DifferentEnemyHero"/> - Xargatha's Cleave prints "on a
        /// different enemy hero" and this prints "on a different hero", and the exclusion and the
        /// narrowing are the same two halves. The condition in front of it is Wasp's:
        /// <c>attack-took-a-hero@1</c>, because "If you target a hero" is a question about the instance
        /// that just resolved.</para>
        ///
        /// <para><b>The third clause is composed at milestone 8.35z46, and what it took
        /// was not what the seam limit's costed design predicted.</b> "if you do, repeat once on a
        /// minion" needs a second repeat bound to a different unit kind, firing only in the instance
        /// the first repeat produced. Two walls stood in front of it, and only one had been measured.
        /// The known one: a repeated Attack learns which kind it may take by reading its own card's
        /// declaration, deliberately derived rather than carried, so it cannot tell two clauses apart
        /// on one card. The unmeasured one, which a probe found: rulebook p.10's "You cannot repeat a
        /// repeated action!" is enforced for card text as a single boolean about the enclosing attack,
        /// so the minion clause would have been skipped in silence inside the very instance it exists
        /// to fire in - whatever it declared. Both are answered by
        /// <see cref="GuardsDigital.Core.Rules.CardResolution.CardTextRepeatChain"/>, which reads an
        /// instance's position in the chain out of the identity the chain itself mints.</para>
        ///
        /// <para><b>"if you do" needed no condition of its own, and that is the composition's neatest
        /// consequence.</b> The minion clause is printed second, so a chain reaches it only at position
        /// one - and position one exists only because the hero repeat happened. The printed condition
        /// is therefore the chain's own shape rather than a predicate, exactly as RULING 37's "if you
        /// do" turned out to be the bound of a move that did not happen.</para>
        ///
        /// <para><b>The chain terminates in exactly three instances, and the reason is printed rather
        /// than counted.</b> The printed instance may ask clause one and does so when it took a hero;
        /// the instance that produces may ask clause two; the instance THAT produces finds no third
        /// clause printed and the chain ends. Nothing bounds it but the card's own two sentences.</para>
        /// </summary>
        private static EffectDefinition CreateArrowStorm(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("arrow-storm-root");
            var heroClause = new EffectNodeId("arrow-storm-hero-clause");
            var heroRepeat = new EffectNodeId("arrow-storm-repeat");
            var minionRepeat = new EffectNodeId("arrow-storm-minion-repeat");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    // The two clauses in printed reading order, which is the order the chain advances
                    // along. Nothing else about this graph decides which instance may ask which.
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { heroClause, minionRepeat }),
                    new EffectNodeDefinition(
                        heroClause, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.AttackTookAHeroV1,
                        childNodeIds: new[] { heroRepeat },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.TookAHeroOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        heroRepeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentEnemyHero),
                    new EffectNodeDefinition(
                        minionRepeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.IfAble,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentEnemyMinion)
                });
        }

        /// <summary>
        /// "You may fast travel, if able. This round: You and friendly heroes in radius may ignore
        /// obstacles while performing movement actions." - Trailblazer's Basic Skill.
        ///
        /// <para>Two sentences, two nodes, in printed order. The Fast Travel is p.11's own reach -
        /// this card narrows nothing, where both Gold basics narrow to an adjacent Zone - and the
        /// footing is the Active effect
        /// <see cref="SilverarrowActiveEffectContent.TrailblazerDefinition"/>, whose own summary
        /// carries RULING 47 and SL-07.</para>
        ///
        /// <para>The effect is created whether or not the Fast Travel happened: the two sentences are
        /// printed side by side with no "if you do" between them, and a declined optional step is not a
        /// failed one.</para>
        /// </summary>
        private static EffectDefinition CreateTrailblazing(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("trailblazer-root");
            var travel = new EffectNodeId("trailblazer-travel");
            var footing = new EffectNodeId("trailblazer-footing");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { travel, footing }),
                    FastTravelNode(travel, FastTravelZoneScope.SameOrAdjacentZone),
                    new EffectNodeDefinition(
                        footing, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SilverarrowActiveEffectContent.TrailblazerInRadiusV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "Move an enemy unit adjacent to you 1 space; if you do, you may move 1 space." - Disorient.
        ///
        /// <para><b>It is not a smaller Divert Attention, and the face says so twice.</b> Its first
        /// Move is an exact distance with no "up to", which p.11 answers outright - "you must move the
        /// exact number of spaces, or stop performing the action at that step" - so the node is
        /// mandatory at one. And its second Move prints "you <b>may</b>" and prints no straight line,
        /// where both its siblings print a line and no may. Recorded as printed; no reconciliation
        /// attempted.</para>
        ///
        /// <para><b>"if you do" needs nothing to express it.</b> The first Move is mandatory, so a
        /// board where it cannot happen stops the Skill at that step and the second Move is never
        /// reached - which is p.10 doing the work the words describe. The condition only needs
        /// machinery on the two faces whose first Move is optional, and there it is RULING 37's
        /// realised distance.</para>
        ///
        /// <para>The subject is "an enemy unit adjacent to you": p.19's unit, opposing her, at distance
        /// one, chosen through the ordinary target selection that names the Move as its consumer - so a
        /// unit with nowhere to be moved to is never offered (milestone 6.8l).</para>
        /// </summary>
        private static EffectDefinition CreateDisorient(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("disorient-root");
            var select = new EffectNodeId("disorient-select");
            var shove = new EffectNodeId("disorient-shove");
            var actorSpace = new EffectNodeId("disorient-actor-space");
            var step = new EffectNodeId("disorient-step");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, shove, actorSpace, step }),
                    AdjacentEnemyUnitNode(select, shove),
                    new EffectNodeDefinition(
                        shove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", select, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            // The card says how far and nothing about where, which is p.11's own
                            // breadth - and the difference from Wasp's orbiting Move, which does print
                            // a constraint.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        // "you may move 1 space": the move is optional at an exact distance, which is
                        // milestone 8.21's distinction between May and UpTo, and p.10 gives the
                        // unassigned decision to the card's controller.
                        step, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
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

        /// <summary>
        /// "Move an enemy unit adjacent to you up to N spaces; if you do, move up to that number of
        /// spaces in a straight line." - Divert Attention at two, Lead Astray at three.
        ///
        /// <para><b>RULING 37 is the whole of this composition.</b> "that number" is the number of
        /// spaces the enemy was <em>actually</em> moved - push it one, and she may move up to one - so
        /// the second Move's bound is the first Move's realised outcome rather than the printed
        /// maximum. That is a binding this build had no shape for: every binding before it carried a
        /// target, a space or a flag, and none carried a magnitude that only exists once a step has
        /// run.</para>
        ///
        /// <para><b>The shape it took, and why it is that shape.</b> The direct Move now publishes the
        /// distance it committed - <see cref="SyntheticDisplacementCapabilities
        /// .RealisedDistanceOutputName"/>, declared beside its destination exactly as Object Push has
        /// published one since milestone 8.21 - and the straight-line chooser takes its maximum from
        /// that output. Nothing re-derives the number from two coordinates, and nothing stores it: it
        /// is an ordinary node output, so a paused resolution restores it with everything else.</para>
        ///
        /// <para><b>"if you do" is answered by the same number rather than by a second condition.</b>
        /// The first Move is "up to N", so nought is a legal outcome, and nought spaces moved gives a
        /// second Move bounded at nought - which offers nothing and is a step not taken. The two
        /// readings of "if you do" that the face leaves open therefore agree in every position, and
        /// neither needed a ruling.</para>
        ///
        /// <para><b>The line is walked, not filtered.</b> "in a straight line" is a statement about how
        /// she travels, so <c>choose-line-destination@1</c> keeps only routes whose every step is one
        /// p.19 direction, and a space reachable by turning a corner is not offered even though it lies
        /// on a file. The card prints nothing about obstacles, so the walk crosses none - which is why
        /// the capability takes the crossing as a parameter rather than deciding it.</para>
        ///
        /// <para><b>Per RULING 47 this is not a Movement action</b>, so a movement-action restriction
        /// does not stop it and her own Trailblazer's obstacle permission does not help it.</para>
        /// </summary>
        private static EffectDefinition CreateMisdirection(
            CardDefinitionId cardId, string contentRevision, int distance)
        {
            var root = new EffectNodeId("misdirect-root");
            var select = new EffectNodeId("misdirect-select");
            var shove = new EffectNodeId("misdirect-shove");
            var line = new EffectNodeId("misdirect-line");
            var actorSpace = new EffectNodeId("misdirect-actor-space");
            var step = new EffectNodeId("misdirect-step");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, shove, line, actorSpace, step }),
                    AdjacentEnemyUnitNode(select, shove),
                    new EffectNodeDefinition(
                        // "up to N spaces" makes the distance optional rather than the move, which is
                        // milestone 8.21's reading of the two printed shapes and what lets the actor
                        // move the unit anywhere from nowhere to N.
                        shove, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", select, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(distance)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.RealisedDistanceOutputName,
                                EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            // RULING 37, and the one line this whole composition is about.
                            EffectInputBinding.FromNodeOutput(
                                "maximumDistance", shove,
                                SyntheticDisplacementCapabilities.RealisedDistanceOutputName,
                                EffectValueKind.Integer)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        step, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(distance)),
                            // The chooser named the space, so the Move applies it rather than asking a
                            // second question with one answer - milestone 8.21's reading, and Brogan's
                            // charge's shape.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, "destination", EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "A hero in radius may retrieve a discarded card; if they do, that hero gains N coin(s)." -
        /// Fae Healing at one coin, Nature's Blessing at two.
        ///
        /// <para><b>RULING 8 decides who "a hero" is, and it is wider than any selection before
        /// it.</b> The user settled that this reaches enemies - "sure, she can target enemies if she
        /// wants" - and then that it reaches her too: "We include self". So the selection names
        /// <see cref="SourceTeamRelation.AnyTeam"/> and sets the hero arm's <c>includeSelf</c>, which is
        /// the parameter this milestone added for exactly this face. Every hero selection written
        /// before hers names a side its own hero cannot be on, so leaving the actor out had never
        /// decided anything; here it decides whether she can heal herself, and the structural argument
        /// the user's decision rests on is that p.17 needed a special rule to exclude self from
        /// "friendly" and an unqualified "a hero" has no such rule.</para>
        ///
        /// <para><b>The retrieval is theirs, and so is the choice.</b> The card comes from that hero's
        /// own discard pile and they choose it - p.10's rule that a step's choice belongs to whoever
        /// performs it - so the node names the chosen hero as both the owner and the chooser, and the
        /// "may" is theirs to decline. <c>retrieve-card@1</c> already existed for this shape and had no
        /// consumer until now.</para>
        ///
        /// <para><b>"if they do" is the retrieval's own output.</b> The grant binds its recipient from
        /// the hero the retrieval published, which it publishes only on success, so a hero who declined
        /// leaves the binding undefined and the grant reports itself impossible - which its own
        /// <c>if able</c> policy turns into a step not taken. No separate condition asks whether it
        /// happened, and no coin can be paid for a retrieval that was not.</para>
        /// </summary>
        private static EffectDefinition CreateBlessing(
            CardDefinitionId cardId, string contentRevision, int coins)
        {
            var root = new EffectNodeId("blessing-root");
            var select = new EffectNodeId("blessing-select");
            var retrieve = new EffectNodeId("blessing-retrieve");
            var reward = new EffectNodeId("blessing-reward");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, retrieve, reward }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            // A Skill's own activity, since there is no enclosing Attack to inherit
                            // one from.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AnyTeam),
                            // The face prints no "not adjacent to you", so nothing is taken out of the
                            // disk's innermost ring.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(retrieve))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        // The card choice belongs to the retrieving hero and the capability names them
                        // as its own chooser; the enclosing `may` opens its confirm-or-decline to the
                        // card's controller, because a Capability node may name no chooser of its own.
                        // Recorded as a bounded fidelity note rather than papered over: the printed
                        // "may" is the retrieving hero's, and directing an enclosing policy's decline
                        // to a bound hero is vocabulary this build does not have.
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "owner", select,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                "chooser", select,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.RetrievedByOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        reward, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.GainResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "recipient", retrieve,
                                SyntheticRosterCapabilities.RetrievedByOutputName,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "Each time before you perform an action you may move 2 spaces in a straight line." - Wild
        /// Hunt, p.16's permanent ability, and the effect its before-action trigger runs.
        ///
        /// <para>The declaration is on the card - <see cref="BeforeActionTriggerFrequency.EveryAction"/>
        /// paired with no action kind, because the sentence names none - and what runs is this: a
        /// straight-line walk of exactly two spaces that the actor may decline. The line is chosen and
        /// then moved to, which is Brogan's charge's shape without his adjacency clause, and it crosses
        /// no obstacles because the card says nothing about crossing any.</para>
        ///
        /// <para><b>Milestone 8.59 corrected the distance, which this summary had called "up to
        /// two".</b> The "may" governs whether she moves, not how far: the offer now binds both bounds
        /// at two, so a board with no free straight-line space two away offers nothing and the
        /// <c>May</c> policy leaves the step simply not taken - which is what declining looks like
        /// anyway, and is the only shape that never walks a distance the card does not print.</para>
        /// </summary>
        public static EffectDefinition WildHuntTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("wild-hunt-root");
            var line = new EffectNodeId("wild-hunt-line");
            var actorSpace = new EffectNodeId("wild-hunt-actor-space");
            var move = new EffectNodeId("wild-hunt-move");

            return new EffectDefinition(
                new EffectDefinitionId("silverarrow-wild-hunt-effect"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { line, actorSpace, move }),
                    new EffectNodeDefinition(
                        // "you may move" - the decision is the acting hero's, and p.10 gives an
                        // unassigned one to the card's controller, who is the same hero here.
                        line, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(2)),

                            // Milestone 8.59, the second face the Bounce sweep found. "You MAY move 2
                            // spaces" leaves whether to move at all to the hero - which is this node's
                            // `May` policy - and leaves the distance to nobody: two is a count. The
                            // offer had only a ceiling, so it lit the spaces one away as well, and
                            // taking one of them was a shorter walk than the sentence permits.
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(2))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                        }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, "destination", EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// What Warning Shot runs when the turn ends: "An enemy hero in radius discards a card, if
        /// able."
        ///
        /// <para><b>Both halves of SL-06 are settled by this graph running here rather than earlier.</b>
        /// The selection reads her printed Radius against wherever she is standing when the turn ends,
        /// and the disk is re-derived at that moment, so the candidates are whoever is inside it then.
        /// p.16's Radius item raises the icon the selection reads, exactly as it would on any other
        /// face.</para>
        ///
        /// <para>Which enemy hero is her choice, because p.10 gives an unassigned decision to the
        /// card's controller; which card is theirs, because it is their hand. "if able" is p.17's, and
        /// it is declared on the discard where the page puts it: an enemy with an empty hand gives
        /// nothing and the effect is over.</para>
        /// </summary>
        public static class WarningShotTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                CreateEndOfTurnDiscard(
                    new EffectDefinitionId("silverarrow-warning-shot-triggered"),
                    SilverarrowCardContent.ContentRevision,
                    "warning",
                    escalatesToDefeat: false);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>
        /// What Treetop Sentinel runs when the turn ends: "An enemy hero in radius discards a card, or
        /// is defeated."
        ///
        /// <para>Ledger question 13's escalation, which milestone 8.10 built and Brogan's Counterattack
        /// already prints: one mandatory step with two ways to complete. The chosen hero discards if
        /// they can, and only a hero who cannot is defeated - so the pair Warning Shot and Treetop
        /// Sentinel make is Shock and Electroblast's exactly, and raises no new question.</para>
        /// </summary>
        public static class TreetopSentinelTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                CreateEndOfTurnDiscard(
                    new EffectDefinitionId("silverarrow-treetop-sentinel-triggered"),
                    SilverarrowCardContent.ContentRevision,
                    "sentinel",
                    escalatesToDefeat: true);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>Every triggered effect Silverarrow's content declares, for a fixture that configures them.</summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects(string contentRevision) =>
            Array.AsReadOnly(new[]
            {
                WildHuntTriggeredEffect(contentRevision),
                WarningShotTriggeredEffect.Definition,
                TreetopSentinelTriggeredEffect.Definition
            });

        private static EffectDefinition CreateEndOfTurnDiscard(
            EffectDefinitionId effectId,
            string contentRevision,
            string nodePrefix,
            bool escalatesToDefeat)
        {
            var root = new EffectNodeId($"{nodePrefix}-root");
            var select = new EffectNodeId($"{nodePrefix}-select");
            var discard = new EffectNodeId($"{nodePrefix}-discard");
            var escalation = new EffectNodeId($"{nodePrefix}-escalation");
            var defeat = new EffectNodeId($"{nodePrefix}-defeat");

            // Milestone 8.66. THE CONSUMER IS DECLARED BY THE "IF ABLE" FACE AND BY IT ALONE, and the
            // split is the whole of this correction.
            //
            // A selection names the node it feeds so a candidate the consumer could do nothing with is
            // never offered. For Warning Shot that reading is exactly right: "discards a card, IF
            // ABLE" gives an empty-handed hero nothing to do, so offering them would be an offer that
            // admits more than the answer can perform - Bain's 8.40k rule. For Treetop Sentinel it is
            // exactly wrong: "discards a card, OR IS DEFEATED" is one step with two ways to complete,
            // so a hero with an empty hand is not an unusable candidate, they are THE candidate the
            // second half of the sentence was written for. Declared, the discard narrowed every
            // empty-handed hero out of the selection and the face could never defeat anybody - which
            // milestone 8.61f measured on Min's Grenade, whose text prints the same escalation.
            //
            // The escalation is not declared in its place. `synthetic-is-flagged@1` reads a flag the
            // discard has not produced yet, so nothing about a candidate can be put to it before the
            // step runs; an escalating step's offer is the whole radius, which is what the printed
            // sentence says it is. Min's Grenade and Widget's Scorching Breath both declare no
            // consumer for this shape, and this is now the third.
            var selection = new List<EffectInputBinding>
            {
                // The Skill that scheduled this is the action the effect belongs to, so its
                // activity is what p.18 asks immunity about.
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false))
            };
            if (!escalatesToDefeat)
            {
                selection.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValue.Node(discard)));
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { select, discard, escalation }
                        : new[] { select, discard }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: selection,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
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
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
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
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(effectId, contentRevision, root, nodes);
        }

        /// <summary>
        /// The one-node body a Skill whose whole text is an Active effect performs - her three
        /// restriction cards and her two "End of turn:" cards.
        /// </summary>
        private static EffectDefinition CreateActiveEffect(
            CardDefinitionId cardId,
            string contentRevision,
            string nodePrefix,
            EffectCapabilityReference capability)
        {
            var root = new EffectNodeId($"{nodePrefix}-root");
            return new EffectDefinition(
                SkillEffectId(cardId),
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
        /// "an enemy unit adjacent to you", which all three of her Green movement Skills print
        /// identically - p.19's unit, opposing her, at distance one. The selection names the Move it
        /// feeds as its consumer, so a unit with nowhere to go is never offered.
        ///
        /// <para>Where Wasp's five telekinetic Skills read "a unit, or a token", these read "an enemy
        /// unit" only. The token class never appears anywhere in Silverarrow's set, and the absence is
        /// recorded rather than filled in.</para>
        ///
        /// <para><b>Milestone 8.35z46's corrective: distance one is card text, not a printed icon.</b>
        /// All three faces this feeds print no Radius icon at all (the printed-value table's row for
        /// each shows Skill with every one of Radius, Range and Item-bonus dashed), which is exactly
        /// right - "adjacent" is a fixed distance the sentence states, and p.16 never raises a card's
        /// own written number the way it raises a printed icon. A first build read "adjacent" through
        /// <c>RadiusTargetV1</c> anyway, whose own handler is explicit that a card with no printed icon
        /// of that channel gets no bound at all - "the selection is therefore impossible rather than
        /// adjacent-by-default" - so all three faces offered no target on any board, in a real match
        /// exactly as in this journey. <see cref="SyntheticTargetingCapabilities.ChooseTargetV4"/> is
        /// the seam built for exactly this shape: Red Needle's Push and Blue Sentinel's Swap already
        /// name their adjacent object through it, with the distance as a literal the definition writes
        /// rather than a total the card would have to print.</para>
        /// </summary>
        private static EffectNodeDefinition AdjacentEnemyUnitNode(
            EffectNodeId nodeId, EffectNodeId consumerNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.ChooseTargetV4,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                    EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(consumerNodeId))
                },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });

        /// <summary>
        /// One printed Fast Travel written in card text: optional, and offering nothing at all where
        /// p.11 or p.18 leaves the hero unable.
        /// </summary>
        private static EffectNodeDefinition FastTravelNode(
            EffectNodeId nodeId, FastTravelZoneScope zoneScope) =>
            new EffectNodeDefinition(
                // p.10 gives an unassigned decision to the card's controller, and "you may fast travel"
                // assigns none - so the confirm-or-decline the `may` policy opens goes to the acting
                // hero without the node naming one, which is also the only shape a Capability node may
                // take: naming a chooser here is refused outright.
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.May,
                SyntheticRosterCapabilities.FastTravelV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.ZoneScopeParameter, zoneScope)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                });

        /// <summary>
        /// The acting hero's own space, published so a following node can name the piece the card's
        /// "you" refers to. Written once because four of her compositions need it.
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

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

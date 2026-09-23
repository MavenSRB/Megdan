using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The effect bodies Swift the Sharpshooter's printed card text composes to - milestone 8.38d.
    ///
    /// <para><b>What is not here is as much of the answer as what is.</b> Three of her five Red Attacks
    /// print a targeting sentence and nothing else - Snipe's "at maximum range, and in a straight
    /// line", and Prepared Shot's and Killshot's "in range, in a straight line, and not adjacent to
    /// you" - and a targeting sentence is not an effect. It is the Attack's own
    /// <see cref="PrintedAttackTargetPolicy"/>, declared on the action, which is what keeps the offer,
    /// the target choice and the resolution reading one answer. So those three carry no root at all,
    /// and milestone 8.38a's two new members land on the declaration rather than on anything in this
    /// file.</para>
    ///
    /// <para><b>The shared bodies.</b> Shotgun and Super-Shotgun print one sentence and differ only in
    /// whether the discard escalates; Suppress, Pin Down and Killing Ground are the same three-node
    /// shape with the same difference; Steam Jump and Assault Jump differ only in a push distance;
    /// Delayed Jump and Mobile Scout differ only by a Fast Travel; Mark for Death and Hunting Season
    /// only in how many minions they move and how many promises they stand. Each pair or triple is one
    /// method here, which is milestone 8.10's Shock/Electroblast precedent.</para>
    /// </summary>
    public static class SwiftEffectContent
    {
        /// <summary>
        /// Milestone 8.46a. What her own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism. <see cref="DecisionLabelCatalog"/> aggregates it and
        /// <c>DecisionLabelCoverageTests</c> proves it complete against the resolver's own pause rule.
        ///
        /// <para>Reload!'s two arms are the user's own worked example of the rule this whole milestone
        /// serves: a control that read "MODE #1 RELOAD-BORROW-ARM" said nothing a player could act on,
        /// and the printed bullet says everything.</para>
        ///
        /// <para>Her one Active-effect <c>may</c> - Mobile Scout's late fast travel - is a CAPABILITY
        /// node, not an optional step, so the resolution session never stops on it and it owes no
        /// words. It is reached from her card all the same, which is what makes that an answer rather
        /// than a blind spot.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionArm(SwiftCardContent.Reload, "reload-borrow-arm",
                "Perform the primary action of your rightmost resolved card");
            yield return DecisionArm(SwiftCardContent.Reload, "reload-defeat-arm",
                "Defeat a minion adjacent to you");
            yield return DecisionStep(SwiftCardContent.Bounce, "bounce-repeat",
                "Move 2 more spaces in a straight line", "Stop here");

            // Milestone 8.83. Hunting Season's "Move UP TO TWO enemy minions in radius" is two passes
            // with a floor of zero, so each one stops and asks - and each says what taking it does in
            // the printed clause's own words. Mark for Death prints no count and stands no such
            // decision, which is why only this face appears here.
            yield return DecisionStep(SwiftCardContent.HuntingSeason, "hunt-pass-0",
                "Move an enemy minion in radius up to 3 spaces", "Move no minions");
            yield return DecisionStep(SwiftCardContent.HuntingSeason, "hunt-pass-1",
                "Move a second enemy minion up to 3 spaces", "Move no more minions");
        }
        private const string ActorSpaceOutput = "space";
        private const string PresentOutput = "present";
        private const string LineDestinationOutput = "destination";

        // -----------------------------------------------------------------------------------------
        // p.14 step 1: the attacker's "Before the attack:" text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Attack of Swift's applies, or <c>null</c> where it prints none. Paired
        /// with <see cref="AttackTextTimingFor"/>, which names which of p.14's moments it is.
        ///
        /// <para>Two of her five compose anything at all. Shotgun and Super-Shotgun make an enemy hero
        /// beside the target give up a card before the blow lands; Snipe, Prepared Shot and Killshot
        /// print targeting alone.</para>
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SwiftCardContent.Shotgun))
            {
                return CreateScatter(cardId, contentRevision, escalates: false);
            }

            if (cardId.Equals(SwiftCardContent.SuperShotgun))
            {
                return CreateScatter(cardId, contentRevision, escalates: true);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's two card-text moments an Attack of hers applies its text at, read
        /// from the face's own words. Both faces that compose anything print "Before the attack,".
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(SwiftCardContent.Shotgun) || cardId.Equals(SwiftCardContent.SuperShotgun)
                ? CardTextTiming.BeforeTheAttack
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.12: the Skills
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Skill of Swift's performs. Twelve of her thirteen Skill faces compose
        /// something; Backup is p.20's Handicap card and its text is deferred.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SwiftCardContent.Reload))
            {
                return CreateReload(cardId, contentRevision);
            }

            if (cardId.Equals(SwiftCardContent.Bounce))
            {
                return CreateBounce(cardId, contentRevision);
            }

            if (cardId.Equals(SwiftCardContent.SteamJump))
            {
                return CreateJump(cardId, contentRevision, pushDistance: 1, maximumPushTargets: 1);
            }

            if (cardId.Equals(SwiftCardContent.AssaultJump))
            {
                return CreateJump(cardId, contentRevision, pushDistance: 2, maximumPushTargets: 1);
            }

            if (cardId.Equals(SwiftCardContent.DropTrooper))
            {
                return CreateJump(cardId, contentRevision, pushDistance: 2, maximumPushTargets: 2);
            }

            if (cardId.Equals(SwiftCardContent.DelayedJump))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "delayed",
                    SwiftActiveEffectContent.DelayedJumpAtEndOfTurnV1);
            }

            if (cardId.Equals(SwiftCardContent.MobileScout))
            {
                return CreateActiveEffect(
                    cardId, contentRevision, "scout",
                    SwiftActiveEffectContent.MobileScoutAtEndOfTurnV1);
            }

            if (cardId.Equals(SwiftCardContent.Suppress))
            {
                return CreatePin(cardId, contentRevision, "suppress", escalates: false);
            }

            if (cardId.Equals(SwiftCardContent.PinDown))
            {
                return CreatePin(cardId, contentRevision, "pin", escalates: false);
            }

            if (cardId.Equals(SwiftCardContent.KillingGround))
            {
                return CreatePin(cardId, contentRevision, "ground", escalates: true);
            }

            if (cardId.Equals(SwiftCardContent.MarkForDeath))
            {
                return CreateMark(
                    cardId, contentRevision, "mark", movers: 1,
                    SwiftActiveEffectContent.MarkForDeathBountyV1);
            }

            if (cardId.Equals(SwiftCardContent.HuntingSeason))
            {
                return CreateMark(
                    cardId, contentRevision, "hunt", movers: 2,
                    SwiftActiveEffectContent.HuntingSeasonFirstBountyV1,
                    SwiftActiveEffectContent.HuntingSeasonSecondBountyV1);
            }

            return null;
        }

        /// <summary>
        /// p.16's permanent ability, as the effect its trigger runs - Bullet Time, "After you resolve a
        /// basic card you may perform the primary action on that card".
        ///
        /// <para>The grant is <c>borrow-primary-action@1</c> over
        /// <see cref="BorrowedActionPool.OwnedRightmostResolvedCard"/>, and that pool is exactly right
        /// here rather than approximately right: the trigger fires when a card has just been resolved,
        /// so the card it names IS the rightmost resolved one. Reusing the pool rather than minting a
        /// "the card that just resolved" selector is what keeps one reading of p.6's turn area.</para>
        ///
        /// <para><b>It cannot re-trigger itself</b>, and that is structural rather than declared here:
        /// an action performed through this grant carries
        /// <see cref="ActionInvocationContext.TextGranted"/>, which milestone 8.38c2's
        /// <c>BasicActionQuery.IsCardResolution</c> classifies as not a card resolution.</para>
        /// </summary>
        public static EffectDefinition BulletTimeTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("bullet-root");
            var borrow = new EffectNodeId("bullet-borrow");

            return new EffectDefinition(
                new EffectDefinitionId(SwiftCardContent.BulletTime.Value + "-ultimate"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrow }),
                    // "you MAY perform" - p.10 gives an unassigned decision to the card's controller,
                    // and this one is unassigned.
                    BorrowNode(borrow, EffectFailurePolicy.May)
                });
        }

        /// <summary>
        /// Every triggered effect Swift's content declares, for a fixture or a composition root that
        /// configures them - p.16's permanent ability beside the two "End of turn:" bodies her Blue
        /// line stands. Milestone 8.38d, on the shape
        /// <see cref="SilverarrowEffectContent"/> established.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects(string contentRevision) =>
            Array.AsReadOnly(
                new[] { BulletTimeTriggeredEffect(contentRevision) }
                    .Concat(SwiftActiveEffectContent.SwiftTriggeredEffects.All)
                    .ToArray());

        // -----------------------------------------------------------------------------------------
        // The bodies
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Before the attack, an enemy hero adjacent to the target discards a card, if able." -
        /// Shotgun, and Super-Shotgun with ledger question 13's escalation in place of the "if able".
        ///
        /// <para><b>The subject is anchored on the TARGET, not on her</b>, which is what makes this a
        /// scatter rather than a second radius clause: the hero who gives up a card stands beside
        /// whatever she is shooting, however far away that is. <c>choose-target@2</c> takes the anchor
        /// from the enclosing Attack's own bound target.</para>
        ///
        /// <para><b>"if able" is <see cref="EffectFailurePolicy.IfAble"/> and the escalation is not a
        /// question.</b> Shotgun's clause silently does nothing where no eligible hero stands beside
        /// the target or where that hero holds no card; Super-Shotgun's second arm is a branch reached
        /// only where the discard could not complete, and p.10 gives nobody a decision at it.</para>
        /// </summary>
        private static EffectDefinition CreateScatter(
            CardDefinitionId cardId, string contentRevision, bool escalates)
        {
            var root = new EffectNodeId("scatter-root");
            var bystander = new EffectNodeId("scatter-bystander");
            var discard = new EffectNodeId("scatter-discard");
            var gate = new EffectNodeId("scatter-gate");
            var defeat = new EffectNodeId("scatter-defeat");

            var children = escalates
                ? new[] { bystander, discard, gate }
                : new[] { bystander, discard };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),
                new EffectNodeDefinition(
                    bystander, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        // "adjacent to the TARGET" - the anchor is the enclosing Attack's own bound
                        // target, which is what makes this a scatter rather than a second radius
                        // clause, and `choose-target@2` names that anchor in its own origin vocabulary
                        // rather than through a distance from her.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter,
                            RosterQueryOrigin.EnclosingTargetBeforeConsequences),
                        // The disk includes its origin; the attacked unit is not adjacent to itself.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.ExcludeTargetParameter, EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability,
                    escalates ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, bystander,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    })
            };

            if (escalates)
            {
                nodes.Add(new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
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
                            SyntheticConsequenceCapabilities.HeroParameter, bystander,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Choose one — • Perform the primary action of your rightmost resolved card. • Defeat a
        /// minion adjacent to you." - Reload!
        ///
        /// <para>Two bullets with different consequences, so this IS a modal: unlike Silverarrow's Red
        /// pair, whose arms differ only in which unit becomes the target, these two do entirely
        /// different things and no union could state both.</para>
        /// </summary>
        private static EffectDefinition CreateReload(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("reload-root");
            var borrowArm = new EffectNodeId("reload-borrow-arm");
            var borrow = new EffectNodeId("reload-borrow");
            var defeatArm = new EffectNodeId("reload-defeat-arm");
            var minion = new EffectNodeId("reload-minion");
            var defeat = new EffectNodeId("reload-defeat");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrowArm, defeatArm },
                        // "Choose ONE —": exactly one arm, and p.10 gives an unassigned decision to
                        // the card's controller. A zero minimum would be "up to one", which is a
                        // different sentence and one this face does not print.
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        borrowArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrow }),
                    BorrowNode(borrow, EffectFailurePolicy.Mandatory),
                    new EffectNodeDefinition(
                        defeatArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { minion, defeat }),
                    // "adjacent to you" is a distance the sentence writes down, not a printed icon -
                    // Reload! prints neither Range nor Radius - which is the corrective milestone
                    // 8.35z46 recorded for Silverarrow's three Green faces.
                    // Milestone 8.38e corrected the CAPABILITY this reaches for, and the comment above
                    // had already written down why: `synthetic-range-minion-target@1` reads its
                    // distance from the card's own printed Range ICON, and Reload! prints none, so a
                    // literal `range` was never admissible there - the effect-definition validator
                    // refuses it as an undeclared parameter, and that validator runs at MATCH
                    // COMPOSITION, which is a tier no composition milestone reaches. Misa's Leaf on the
                    // Wind prints the identical sentence and uses this same `choose-target@2` with an
                    // explicit distance, which is the vocabulary an unprinted "adjacent to you" has.
                    new EffectNodeDefinition(
                        minion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                            // p.13 forbids defeating friendly units; the card overrides nothing, and
                            // the Skill's own immunity checks apply on top.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
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
                });
        }

        /// <summary>
        /// "Move 2 spaces in a straight line, ignoring obstacles; if this card is already resolved as
        /// you perform this action, may repeat once." - Bounce.
        ///
        /// <para><b>Exactly two spaces, not up to two - and the OFFER says so, since milestone
        /// 8.59.</b> <c>choose-line-destination@1</c> carries the obstacle clause and both bounds of
        /// the file, equal here because "Move 2 spaces" is a count rather than a ceiling; the Move's
        /// own <c>leastDistance</c> states the same floor a second time, where it stops a shortened
        /// walk to a space already named. Until 8.59 only the Move held the floor, so the question lit
        /// every space one away as well and a player at the table answered with one - a move the card
        /// cannot perform, which p.11 stops the action over rather than shortening.</para>
        ///
        /// <para><b>The repeat's gate is the clause this project has met twice before.</b> "If this
        /// card is already resolved as you perform this action" can only hold when the action is a
        /// second performance off a card whose own turn is over - a borrow through Reload! or a grant
        /// through Bullet Time - which is exactly what makes the sentence do work rather than reading
        /// as dead text. It is milestone 8.37a's open question 34 in a narrower form, and it resolves
        /// the same way: the reading is opt-in at this one call site and no earlier face's predicate
        /// changes.</para>
        /// </summary>
        private static EffectDefinition CreateBounce(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("bounce-root");
            var actorSpace = new EffectNodeId("bounce-actor-space");
            var line = new EffectNodeId("bounce-line");
            var move = new EffectNodeId("bounce-move");
            var resolved = new EffectNodeId("bounce-resolved");
            var gate = new EffectNodeId("bounce-gate");
            var repeat = new EffectNodeId("bounce-repeat");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move, resolved, gate }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(2)),

                            // Milestone 8.59. The floor the sentence states, said to the OFFER.
                            // Without it the question lit every space one away too, and answering
                            // with one of them named a move `bounce-move` cannot perform - p.11 stops
                            // the action rather than shortening it - so the card offered spaces it
                            // could not take. The two bounds are equal because "Move 2 spaces" is a
                            // count, not a ceiling.
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                                EffectValue.Boolean(true))
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
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(2)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, LineDestinationOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        resolved, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConditionCapabilities.SourceCardInDiscardV1,
                        // Milestone 8.48. "Already resolved" is the WIDE reading, opt-in exactly as
                        // Blizzard's "in your discard" is: a card resolved this round sits in the turn
                        // area, not the discard, and a borrow through Reload! names precisely such a
                        // card - so without this binding the repeat could never hold on the one path
                        // the sentence exists for. Found at the table: "Swift's gold didn't let me
                        // play her silver twice."
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticConditionCapabilities.CountResolvedPlayedParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConditionCapabilities.InDiscardOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { repeat },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", resolved, SyntheticConditionCapabilities.InDiscardOutput,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        // p.10 gives an unassigned decision to the card's controller, and "may
                        // repeat once" assigns none.
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1)
                });
        }

        /// <summary>
        /// "Place yourself into a space in a straight line in radius. Push [up to two] enemy unit[s]
        /// adjacent to you up to N spaces." - Steam Jump, Assault Jump and Drop Trooper.
        ///
        /// <para>The Place is milestone 8.38b's on-file destination policy over the card's own printed
        /// Radius, so p.16's Radius item widens it. The Push is measured AFTER the Place - "adjacent to
        /// you" is where she now stands, which is the whole point of jumping first - and its
        /// <c>maximumTargets</c> is what separates Drop Trooper from its two siblings, using the
        /// parameter milestone 8.8 minted for Wasp's kinetic Skills.</para>
        ///
        /// <para><b>"up to N spaces" is p.11's OPTIONAL DISTANCE, and milestone 8.83 is where this
        /// node finally says so.</b> It declared <see cref="EffectFailurePolicy.May"/> beside a
        /// <c>leastDistance</c> of nought, and <c>AdmittedDistances.ForDeclared</c> reads `may` as
        /// leaving the number EXACT - the floor is consulted under <see cref="EffectFailurePolicy.UpTo"/>
        /// alone. So all three faces pushed the declared number of spaces and no fewer, and
        /// <c>OfferTargets</c> sets its minimum to the maximum under any policy but `up to`, which
        /// made Drop Trooper's "push UP TO TWO enemy units" force BOTH of them whenever two stood
        /// beside her. The roster sweep that found it is 8.83's, and Wasp's two kinetic Skills - the
        /// only other pushes naming a count - have declared `up to` since milestone 8.8.</para>
        /// </summary>
        private static EffectDefinition CreateJump(
            CardDefinitionId cardId, string contentRevision, int pushDistance, int maximumPushTargets)
        {
            var root = new EffectNodeId("jump-root");
            var actorSpace = new EffectNodeId("jump-actor-space");
            var place = new EffectNodeId("jump-place");
            var push = new EffectNodeId("jump-push");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place, push }),
                    ActorSpaceNode(actorSpace),
                    PlaceNode(
                        place, actorSpace,
                        SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1),
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
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.Unit),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MaximumTargetsParameterName,
                                EffectValue.Integer(maximumPushTargets)),
                            // p.18's ordinary Push: obstacles stop it. Her Blue line prints
                            // "ignoring obstacles" on the PLACE it makes of herself, never on what it
                            // moves of the enemy's, so this is false and the parameter is bound
                            // rather than omitted because `synthetic-object-push@3` requires it.
                            EffectInputBinding.FromLiteral(
                                "ignoresObstacles", EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "An enemy hero in radius, who is not adjacent to terrain, discards a card, if able." -
        /// Suppress and Pin Down, and Killing Ground with ledger question 13's escalation.
        ///
        /// <para><b>The terrain question is asked of the SUBJECT, not of her</b>, which is milestone
        /// 8.38d's <c>subject-adjacent-terrain-presence@1</c> and the correction recorded as HERO_SWIFT
        /// row SW-11: the actor-scoped predicate Tigerclaw's Blend Into Shadows names asks whether HE
        /// stands beside terrain, which is a different sentence.</para>
        ///
        /// <para>The gate is negated - the clause admits a hero who is NOT beside terrain - so the
        /// Conditional's child runs on a false reading, which is what
        /// <see cref="EffectNodeKind.Conditional"/>'s else-branch shape is for.</para>
        /// </summary>
        private static EffectDefinition CreatePin(
            CardDefinitionId cardId, string contentRevision, string prefix, bool escalates)
        {
            var root = new EffectNodeId($"{prefix}-root");
            var subject = new EffectNodeId($"{prefix}-subject");
            var terrain = new EffectNodeId($"{prefix}-terrain");
            var gate = new EffectNodeId($"{prefix}-gate");
            var body = new EffectNodeId($"{prefix}-body");
            var discard = new EffectNodeId($"{prefix}-discard");
            var escalation = new EffectNodeId($"{prefix}-escalation");
            var defeat = new EffectNodeId($"{prefix}-defeat");

            var bodyChildren = escalates
                ? new[] { discard, escalation }
                : new[] { discard };

            // Milestone 8.66, the second face the Silverarrow sweep found and the same one-line
            // defect. "if able" narrows on its consumer and "or is defeated" must not: an empty-handed
            // hero is what Killing Ground's second half is written for, and declaring the discard as
            // this selection's consumer narrowed them out of it, so the face could never defeat
            // anybody. See SilverarrowEffectContent.CreateEndOfTurnDiscard for the whole reading.
            var subjectInputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName,
                    EffectValue.Boolean(false))
            };
            if (!escalates)
            {
                subjectInputs.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValue.Node(discard)));
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { subject, terrain, gate }),
                new EffectNodeDefinition(
                    subject, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: subjectInputs,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }),
                new EffectNodeDefinition(
                    terrain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.SubjectAdjacentTerrainPresenceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticRosterCapabilities.TerrainSubjectParameter, subject,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        // "NOT adjacent to terrain" - the negation rides the predicate, which is where
                        // this build puts one; `synthetic-is-flagged@1` declares none.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.SubjectTerrainNegateParameter,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { body },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", terrain, PresentOutput, EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    body, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: bodyChildren),
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability,
                    escalates ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, subject,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
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
                    })
            };

            if (escalates)
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
                            SyntheticConsequenceCapabilities.HeroParameter, subject,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Move [up to two] enemy minion[s] in radius up to 3 spaces to [a space / spaces] in radius.
        /// Next turn: the first [two] time[s] an enemy minion in radius is defeated, gain 1 coin." -
        /// Mark for Death and Hunting Season.
        ///
        /// <para><b>The move keeps the minion inside her own printed Radius through milestone 8.82's
        /// <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/>.</b> The second "in
        /// radius" is the SAME icon as the first, and p.9 says so - "if the card text mentions
        /// Range/Radius, it refers to this value" - so both halves of the sentence read the card's own
        /// Radius with p.16's items composed onto it. Her Super-Shotgun grants +1 Radius, and a
        /// declared literal 4 would be the wrong disk the moment she holds it: the offer would widen
        /// to 5 and the leash would not, which is the pair coming apart on a live board.</para>
        ///
        /// <para><b>It declared milestone 8.22's
        /// <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> and bound it NO number at all,
        /// and that is the defect milestone 8.82 closes.</b> An undeclared bound is not an absent
        /// bound - <c>DirectMoveDestinations.Admit</c> reads a radius policy with no radius as
        /// admitting nothing - so this move had no destination for any minion on any board, and the
        /// selection above it declares this move as its CONSUMER. A consumer declaration narrows the
        /// offer to candidates the consumer can act on, so the whole face offered the player NOBODY.
        /// It was reported from a playtest in exactly those words - "Swift is not being offered blue
        /// minion to move" - and the host log shows the Skill used with no minion Move row after
        /// it.</para>
        ///
        /// <para><b>"up to 3 spaces" is p.11's optional distance</b>, so the move node declares
        /// <see cref="EffectFailurePolicy.UpTo"/>. It declared <see cref="EffectFailurePolicy.IfAble"/>
        /// until milestone 8.82, and p.11 separates the two in so many words: "unless the distance is
        /// optional ('up to 2 spaces'), or the move itself is optional ('you may move'), you must move
        /// the exact number of spaces". `if able` is the second of those and leaves the number exact,
        /// so even with the leash bound this face would have offered the third ring alone - never a
        /// step of one or two, and never the stand-still p.11 admits when it says a card's text
        /// applies "even if you choose to move zero spaces". It is corrected in the same milestone
        /// because it is the same clause of the same sentence on the same node.</para>
        ///
        /// <para><b>MILESTONE 8.83: HUNTING SEASON MOVES TWO MINIONS, AND IT USED TO MOVE ONE.</b>
        /// Both faces were composed here as ONE subject and ONE move, with only the bounty count
        /// doubling - so the Tier III face that prints "Move up to TWO enemy minions in radius ... to
        /// SPACES in radius" offered exactly the one minion its Tier II sibling does, and the second
        /// half of its own sentence was unreachable on every board. It is <paramref name="movers"/>
        /// now: Mark for Death keeps the single pair, and Hunting Season composes two, each in its own
        /// <see cref="EffectNodeKind.Optional"/> pass so that "up to two" really is none, one or two.
        /// The second pass declares milestone 8.69c's <c>excludeMinion</c> against the first pass's
        /// own chosen minion, which is the roster's one spelling of "a different unit than the one
        /// already chosen" - see <see cref="MarkSubjectNode"/> for why it is two printed-icon
        /// selections rather than <c>choose-target-set@1</c>.</para>
        ///
        /// <para><b>The promise is instance multiplicity</b>, which is milestone 8.38c1's whole design:
        /// Hunting Season stands TWO bounty effects and each is spent once, so "the first two times" is
        /// counted by how many are still live rather than by a counter. The two carry different
        /// uniqueness keys, because the lifecycle suppresses a duplicate and one key would silently
        /// turn Hunting Season into Mark for Death. They are UNCHANGED by 8.83: the promises were
        /// always two, and it was the moves that were one.</para>
        /// </summary>
        /// <param name="movers">
        /// How many minions the printed sentence moves - 1 for Mark for Death's "an enemy minion", 2
        /// for Hunting Season's "up to two enemy minions". The bounty count is a separate number the
        /// same sentence prints ("the first [two] time[s]"), which is why the two are not derived from
        /// each other: a face could print one without the other, and reading the count off
        /// <paramref name="bounties"/> would be this method inventing a rule the cards do not state.
        /// </param>
        private static EffectDefinition CreateMark(
            CardDefinitionId cardId,
            string contentRevision,
            string prefix,
            int movers,
            params EffectCapabilityReference[] bounties)
        {
            var root = new EffectNodeId($"{prefix}-root");
            var promiseIds = bounties
                .Select((_, index) => new EffectNodeId($"{prefix}-promise-{index}"))
                .ToArray();

            var nodes = new List<EffectNodeDefinition>();
            var moverRoots = new List<EffectNodeId>();

            if (movers == 1)
            {
                // Mark for Death names ONE minion and names it without a count, so the pair stands
                // directly under the root exactly as it has since milestone 8.38c: there is no "up
                // to" over the subject to wrap in a decision, and `if able` is p.10's reading of a
                // step with nobody to perform it on.
                var subject = new EffectNodeId($"{prefix}-subject");
                var move = new EffectNodeId($"{prefix}-move");
                moverRoots.Add(subject);
                moverRoots.Add(move);
                nodes.Add(MarkSubjectNode(subject, move, excludedFrom: default));
                nodes.Add(MarkMoveNode(move, subject));
            }
            else
            {
                // "Move UP TO two enemy minions" prints no "may", so each pass carries p.10's own word
                // for a count whose floor is zero rather than a decline of a step - Brynn's Decoy
                // ("Choose up to two times") takes the identical shape for the identical grammar, and
                // this is that shape at one minion a pass.
                //
                // EACH PASS IS NESTED INSIDE THE ONE BEFORE IT, which is Decoy's shape too and is
                // REQUIRED rather than tidy: the second selection binds the first selection's own
                // chosen minion into `excludeMinion`, and `EffectDefinitionValidator` refuses a
                // binding whose producer is not GUARANTEED to have run - which two passes standing as
                // siblings under the root are not, because the first may be declined. Nesting makes
                // the order structural, and it is also what the sentence means: a player who moves no
                // minions at all has finished the clause, not earned a second ask.
                moverRoots.Add(new EffectNodeId($"{prefix}-pass-0"));
                for (var index = 0; index < movers; index++)
                {
                    var pass = new EffectNodeId($"{prefix}-pass-{index}");
                    var body = new EffectNodeId($"{prefix}-body-{index}");
                    var subject = new EffectNodeId($"{prefix}-subject-{index}");
                    var move = new EffectNodeId($"{prefix}-move-{index}");
                    var last = index == movers - 1;

                    nodes.Add(new EffectNodeDefinition(
                        pass, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                        childNodeIds: new[] { body },
                        chooser: EffectChooserRole.Actor));
                    nodes.Add(new EffectNodeDefinition(
                        body, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: last
                            ? new[] { subject, move }
                            : new[] { subject, move, new EffectNodeId($"{prefix}-pass-{index + 1}") }));
                    nodes.Add(MarkSubjectNode(
                        subject, move,
                        excludedFrom: index == 0
                            ? default
                            : new EffectNodeId($"{prefix}-subject-{index - 1}")));
                    nodes.Add(MarkMoveNode(move, subject));
                }
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: moverRoots.Concat(promiseIds).ToArray()));

            nodes.AddRange(promiseIds.Select((nodeId, index) => new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                bounties[index],
                outputs: new[]
                {
                    new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                })));

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "[up to two] enemy minion[s] in radius" - p.9's disk read off the card's own printed Radius
        /// icon with p.16's items composed onto it, on the side opposing hers, declaring the Move it
        /// feeds as its consumer.
        ///
        /// <para><b><paramref name="excludedFrom"/> is what makes "two minions" two minions.</b>
        /// Hunting Season's second pass must not name the minion its first pass already moved, and
        /// milestone 8.69c's <c>excludeMinion</c> is the roster's one spelling of that clause - bound
        /// to the earlier selection's own published minion, exactly as Brynn's Decoy and his two
        /// guides bind it. It is optional, so a first pass the player DECLINED publishes no minion,
        /// the binding resolves to nothing, and the second pass excludes nobody - which is the printed
        /// answer, because a pass that named nobody took nobody out of the disk.</para>
        ///
        /// <para><b>Why two printed-icon selections rather than <c>choose-target-set@1</c>.</b> The
        /// set selection is the roster's usual answer to "up to N units" - Dodger's skulls take it -
        /// but its reach is a REQUIRED literal <c>distance</c>, and "in radius" is the icon rather
        /// than a number the sentence writes down. Composing this face on it would put back exactly
        /// the defect milestone 8.82 closed, one node to the left: her Super-Shotgun's +1 Radius would
        /// widen the leash and not the offer. The printed-icon arm reads
        /// <c>RadiusValueQuery</c> on every ask, so both passes and both leashes read one total.</para>
        /// </summary>
        private static EffectNodeDefinition MarkSubjectNode(
            EffectNodeId nodeId, EffectNodeId consumer, EffectNodeId excludedFrom)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValue.Node(consumer))
            };

            if (excludedFrom.IsDefined)
            {
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.ExcludedMinionParameterName,
                    excludedFrom,
                    SyntheticTargetingCapabilities.ChosenMinionOutputName,
                    EffectValueKind.Minion));
            }

            return new EffectNodeDefinition(
                // `if able` is p.10's reading of a step with nobody to perform it on: a disk with no
                // enemy minion in it asks nothing and stops nothing. The COUNT's optionality is the
                // enclosing pass's, where "up to two" prints it.
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate)
                });
        }

        /// <summary>
        /// "... up to 3 spaces to [a space / spaces] in radius." - p.11's optional distance against
        /// milestone 8.82's printed-Radius leash, one node per minion the sentence moves.
        /// </summary>
        private static EffectNodeDefinition MarkMoveNode(EffectNodeId nodeId, EffectNodeId subject) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", subject,
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.WithinActorPrintedRadius)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                });

        // -----------------------------------------------------------------------------------------
        // Shared nodes
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The borrow both her Gold basic and her Ultimate perform, over the pool milestone 8.38c
        /// minted. It narrows by no colour, because neither face prints one.
        /// </summary>
        private static EffectNodeDefinition BorrowNode(
            EffectNodeId nodeId, EffectFailurePolicy policy) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, policy,
                SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticBorrowedActionCapabilities.PoolParameter,
                        BorrowedActionPool.OwnedRightmostResolvedCard),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticBorrowedActionCapabilities.ColorParameter, CardColor.None)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                        EffectValueKind.CardInstance)
                });

        /// <summary>
        /// One of her Blue placements. The face refuses no Spawn point, so this declaration refuses
        /// none; the piece is her own, which is what "place yourself" names.
        /// </summary>
        private static EffectNodeDefinition PlaceNode(
            EffectNodeId nodeId, EffectNodeId actorSpaceNodeId, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", actorSpaceNodeId, ActorSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.EligiblePieceParameterName,
                        PlaceEligiblePieceKind.GamePiece),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                        PlaceDestinationAnchor.ActingHero),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                        PlaceSpawnPointExclusion.None)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                });

        /// <summary>
        /// The acting hero's own space, published so a following node can name the piece the card's
        /// "you" refers to.
        /// </summary>
        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

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

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

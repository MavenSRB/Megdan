using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Sabina's printed card text as typed effect definitions - milestone 8.22.
    ///
    /// <para>It sits beside <see cref="SabinaCardContent"/> for the reason the three earlier heroes'
    /// pairs do: that file is the transcription and this one is the composition. A reader auditing
    /// what the cards say reads one; a reader auditing what the engine does with it reads the
    /// other.</para>
    ///
    /// <para><b>Every node here is a declaration over a registered contract.</b> Nothing reads a card
    /// name, a hero name or a printed sentence at runtime, and no epithet reaches any of it. Where a
    /// shape already existed - milestone 8.19b's target pair and target set, Wasp's discard-or-defeat,
    /// Brogan's retrieve - this file declares Sabina's numbers against it rather than restating the
    /// shape.</para>
    ///
    /// <para><b>Three of her nineteen faces compose nothing here, and that is the content.</b>
    /// Quickdraw, Gunslinger and Dead Shot print a conditional addition to their own Attack value,
    /// which p.14 marks with the <c>+</c> on the number itself; a value is not a step, so those three
    /// declare a <see cref="RosterValueContributionDefinition"/> on the action and no card text at all.
    /// A composition that turned them into nodes would be inventing a moment the page does not
    /// have.</para>
    ///
    /// <para><b>She prints no Active effect anywhere</b>, so this file has no companion
    /// <c>SabinaActiveEffectContent</c> - the first verified hero for whom that is true. Every card
    /// resolves immediately and Big Sodding Gun is p.16's permanent ability.</para>
    /// </summary>
    public static class SabinaEffectContent
    {
        /// <summary>
        /// Milestone 8.46a. What her own decision controls say - the consequence in the printed text's
        /// own words, never the mechanism. <see cref="DecisionLabelCatalog"/> aggregates it and
        /// <c>DecisionLabelCoverageTests</c> proves it complete against the resolver's own pause rule.
        ///
        /// <para>Her three Green faces print one sentence and differ only in how many times it may be
        /// taken, so they say the same thing at the control: the count is what the repeat's own bound
        /// enforces, and a label that named it would be reading the node rather than the card.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionStep(SabinaCardContent.TroopMovement, "troop-repeat",
                "Move a friendly minion again", "Stop here");
            yield return DecisionStep(SabinaCardContent.MarchingOrders, "troop-repeat",
                "Move a friendly minion again", "Stop here");
            yield return DecisionStep(SabinaCardContent.PathToVictory, "troop-repeat",
                "Move a friendly minion again", "Stop here");
        }
        /// <summary>Big Sodding Gun's stable ability identity inside its card.</summary>
        public const string BigSoddingGunRuleId = "big-sodding-gun";

        /// <summary>
        /// "If you push an enemy hero" - p.18's Push having applied, to a hero on the other side.
        ///
        /// <para>The declaration names the pushed object and nothing else. Which of Sabina's pushes it
        /// was, and whether the push came from her basic Attack's own rider or from anywhere else, is
        /// not part of the sentence: "if you push an enemy hero" is true of any push of hers that
        /// moved one.</para>
        /// </summary>
        public static AfterPushTrigger BigSoddingGunTrigger { get; } =
            new AfterPushTrigger(AfterPushTriggerSubject.EnemyHero);

        /// <summary>
        /// "Your basic attack has +2 [Range] Range and +2 [Attack] Attack" - two channels of one
        /// printed sentence, each qualified to the action class the sentence names.
        ///
        /// <para><see cref="RuleValueApplicability.ControllerBasicAttackAction"/> is what makes the
        /// qualification real. Sabina's five Red Attacks are non-basic and her Blue and Green kit
        /// prints Radius rather than Range, so a declaration admitted by channel alone would hand two
        /// extra Attack to cards the sentence does not name. The two are separate declarations because
        /// a <see cref="RuleModification"/> names one channel - which is why milestone 8.22 made an
        /// Ultimate's rule a list.</para>
        ///
        /// <para>Question S-02 is deliberately not answered by either of them. Her Gold basic prints a
        /// Range icon, so "+2 Range" plainly makes it 3; her Handicap card prints none, and whether
        /// this declaration gives it one is unanswerable from the faces. Nothing here claims a reading:
        /// the declaration says what the card says, and Bang is returned to the box at setup, so no
        /// configuration in this build asks the question.</para>
        /// </summary>
        public static IReadOnlyList<RuleModification> BigSoddingGunStandingRules { get; } =
            Array.AsReadOnly(new[]
            {
                RuleModification.ForValue(
                    PrintedValueChannel.Attack, 2,
                    RuleValueApplicability.ControllerBasicAttackAction),
                RuleModification.ForValue(
                    PrintedValueChannel.Range, 2,
                    RuleValueApplicability.ControllerBasicAttackAction)
            });

        /// <summary>
        /// "that hero discards a card, or is defeated" - what Big Sodding Gun's trigger runs.
        ///
        /// <para>It is ledger question 13's construction, composed exactly as Wasp's Electroblast and
        /// Brogan's Counterattack compose it: one mandatory discard with two ways to complete, and a
        /// Defeat reached only when the hero could not pay. What is new is who it is about. Every
        /// earlier printing of the construction selects its hero; this one does not select at all,
        /// because the push already picked them out - which is what
        /// <see cref="EffectRoleKind.OpposingHero"/> names.</para>
        /// </summary>
        public static EffectDefinition BigSoddingGunTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("gun-root");
            var discard = new EffectNodeId("gun-discard");
            var escalation = new EffectNodeId("gun-escalation");
            var defeat = new EffectNodeId("gun-defeat");

            return new EffectDefinition(
                new EffectDefinitionId(BigSoddingGunRuleId + "-after-push"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { discard, escalation }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticCardStateCapabilities.HeroParameter,
                                EffectRoleKind.OpposingHero, EffectValueKind.Hero)
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
                        }),
                    new EffectNodeDefinition(
                        defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatHeroV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticConsequenceCapabilities.HeroParameter,
                                EffectRoleKind.OpposingHero, EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// Sabina's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, and
        /// <see cref="ActionDefinition"/> refuses a Skill carrying neither this nor a stated debt.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SabinaCardContent.BackToBack))
            {
                return CreateBackToBack(cardId, contentRevision, radius: 2);
            }

            if (cardId.Equals(SabinaCardContent.ListenUp))
            {
                return CreateMinionSwap(cardId, contentRevision, radius: 1, ignoresHeavyImmunity: false);
            }

            if (cardId.Equals(SabinaCardContent.RogerRoger))
            {
                return CreateMinionSwap(cardId, contentRevision, radius: 2, ignoresHeavyImmunity: false);
            }

            if (cardId.Equals(SabinaCardContent.ReadyAndWaiting))
            {
                return CreateMinionSwap(cardId, contentRevision, radius: 2, ignoresHeavyImmunity: true);
            }

            if (cardId.Equals(SabinaCardContent.SteadyAdvance))
            {
                return CreateRegroup(cardId, contentRevision, radius: 2, moveDistance: 1);
            }

            if (cardId.Equals(SabinaCardContent.UnwaveringResolve))
            {
                return CreateRegroup(cardId, contentRevision, radius: 2, moveDistance: 2);
            }

            if (cardId.Equals(SabinaCardContent.TroopMovement))
            {
                return CreateCommandedMove(cardId, contentRevision, maximumRepeats: 1);
            }

            if (cardId.Equals(SabinaCardContent.MarchingOrders))
            {
                return CreateCommandedMove(cardId, contentRevision, maximumRepeats: 1);
            }

            if (cardId.Equals(SabinaCardContent.PathToVictory))
            {
                return CreateCommandedMove(cardId, contentRevision, maximumRepeats: 2);
            }

            if (cardId.Equals(SabinaCardContent.CloseSupport))
            {
                return CreateSupportedDiscard(
                    cardId, contentRevision, radius: 3, escalatesToDefeat: false);
            }

            if (cardId.Equals(SabinaCardContent.CoveringFire))
            {
                return CreateSupportedDiscard(
                    cardId, contentRevision, radius: 3, escalatesToDefeat: true);
            }

            return null;
        }

        /// <summary>
        /// Sabina's card text for one printed Attack, or <c>null</c> where the face prints none or its
        /// composition is still owed.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(SabinaCardContent.PointBlankShot))
            {
                return CreatePointBlankPush(cardId, contentRevision);
            }

            if (cardId.Equals(SabinaCardContent.Shootout))
            {
                return CreateFollowUpRemoval(cardId, contentRevision, maximumRemoved: 1);
            }

            if (cardId.Equals(SabinaCardContent.BulletHell))
            {
                return CreateFollowUpRemoval(cardId, contentRevision, maximumRemoved: 2);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's three moments an Attack's composed text is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Every composed Attack text of Sabina's is at step 7. Point Blank Shot, Shootout and
        /// Bullet Hell all print "After the attack:", and all three riders depend on it: the push and
        /// the removals happen once the defending unit has already been dealt with, which is what makes
        /// the difference between her two adjacency questions matter at all.</para>
        ///
        /// <para>None of her Attacks prints "Before the attack:", and none binds its own target - every
        /// one of them says "Target a unit in range", which is p.13's own sentence for a Ranged Attack
        /// restated on the face. So no Sabina Attack has anything to apply at step 1.</para>
        ///
        /// <para>The three <c>+</c> Attacks return <see cref="CardTextTiming.None"/> because they
        /// compose no text: their clause is read when p.14 computes the Attack Total, which is a value
        /// question rather than one of the page's three text moments. Bang states none for the reason
        /// Brogan's Leap Forward does - its composition is deferred, and
        /// <see cref="ActionDefinition"/> refuses a moment without a root.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(SabinaCardContent.PointBlankShot) ||
                cardId.Equals(SabinaCardContent.Shootout) ||
                cardId.Equals(SabinaCardContent.BulletHell))
            {
                return CardTextTiming.AfterTheAttack;
            }

            return CardTextTiming.None;
        }

        /// <summary>
        /// "After the attack: If the target is adjacent to you, push it 1 space." - Point Blank Shot's
        /// rider, and the first verified text in this project whose condition is about where a piece is
        /// standing <em>now</em>.
        ///
        /// <para><b>Why the tense decides the capability.</b> The face prints "is adjacent", and this
        /// text runs at p.14's step 7 - after step 6 has removed anything the Attack defeated. So the
        /// question is asked of the live board through <c>target-is-adjacent@1</c>, and a target that
        /// is no longer on it answers no. Shootout's rider two cards later prints "<em>was</em>
        /// adjacent" and reaches <c>pre-consequence-adjacency@1</c> instead; the two capabilities exist
        /// separately because the two sentences are separately printed.</para>
        ///
        /// <para><b>Why the push still names the captured space.</b> An Attack does not move its
        /// defender, so a target that survived is standing exactly where it was when the Attack bound
        /// it - which is the space <c>attacked-target-space@1</c> published. Reading the space from
        /// there rather than asking the board twice keeps the whole rider derived from one moment, and
        /// costs nothing: the branch only runs when the condition already said the piece is
        /// there.</para>
        ///
        /// <para><b>The distance is exact and the push is mandatory</b>, because the face prints
        /// neither "may" nor "up to". p.18 admits a push that moves the piece zero spaces as a legal
        /// result - a unit pushed into an obstacle stops, which the card's own parenthesised reminder
        /// states in so many words - so "cannot be pushed" and "was pushed and did not move" stay
        /// different outcomes, and only the first stops the text.</para>
        /// </summary>
        private static EffectDefinition CreatePointBlankPush(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("shot-root");
            var adjacent = new EffectNodeId("shot-adjacent");
            var targetSpace = new EffectNodeId("shot-target-space");
            var gate = new EffectNodeId("shot-gate");
            var push = new EffectNodeId("shot-push");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacent, gate }),
                    new EffectNodeDefinition(
                        adjacent, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.TargetIsAdjacentV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(AdjacentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { pushSequence },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", adjacent, AdjacentOutput, EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        pushSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { targetSpace, push }),
                    new EffectNodeDefinition(
                        targetSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(TargetSpaceOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", targetSpace, TargetSpaceOutput,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            // "push it 1 space" - one number, so the floor is the ceiling and there is
                            // no distance question for the actor to answer.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                "ignoresObstacles", EffectValue.Boolean(false)),
                            // p.13 lets this Attack take a hero or a minion, and the rider says "it" -
                            // whichever it took. p.19 makes both units.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "After the attack: If the target was adjacent to you remove up to one / two enemy minions
        /// adjacent to you." - Shootout's rider and Bullet Hell's.
        ///
        /// <para><b>The condition is past tense and reaches
        /// <c>pre-consequence-adjacency@1</c>.</b> "was adjacent" asks about the moment the Attack
        /// landed, not about step 7, so a target this very Attack removed still satisfies it - which is
        /// the ordinary case, because a Ranged Attack that killed something adjacent is exactly what
        /// the card is written for.</para>
        ///
        /// <para><b>Remove is p.19's keyword and not p.13's Defeat</b>, which is why nothing here pays
        /// a coin. Shootout prints the consequence as reminder text - "( You gain no coins for removing
        /// a minion, only for defeating. )" - and p.19's own entry says the same in general terms, so
        /// the sentence restates a keyword rather than creating an exception. Bullet Hell prints no
        /// reminder and behaves identically, which is the proof that the reminder is not the
        /// rule.</para>
        ///
        /// <para><b>"up to" is the selection's own count</b>, so one node asks for between zero and N
        /// of the minions standing beside her and a player naming none is a legal answer rather than a
        /// failure. The removals then apply in the order the selection canonicalised, each through the
        /// shared operation - so each one's removal decision and any immediate Lane Push it qualifies
        /// runs before the next begins, and a game that ends on the first never reaches the
        /// second.</para>
        ///
        /// <para>Bullet Hell's second removal is a node of its own rather than a repeat, because the
        /// two are one sentence with one count: a repeat would ask a second question, and the card asks
        /// one. Each removal is <c>if able</c>, so a selection that named one minion leaves the second
        /// node nothing to do and continues rather than stopping the text.</para>
        /// </summary>
        private static EffectDefinition CreateFollowUpRemoval(
            CardDefinitionId cardId, string contentRevision, int maximumRemoved)
        {
            var root = new EffectNodeId("shootout-root");
            var wasAdjacent = new EffectNodeId("shootout-adjacent");
            var gate = new EffectNodeId("shootout-gate");
            var sequence = new EffectNodeId("shootout-sequence");
            var select = new EffectNodeId("shootout-targets");
            var removeFirst = new EffectNodeId("shootout-remove");
            var removeSecond = new EffectNodeId("shootout-remove-second");

            var removals = maximumRemoved > 1
                ? new[] { select, removeFirst, removeSecond }
                : new[] { select, removeFirst };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { wasAdjacent, gate }),
                new EffectNodeDefinition(
                    wasAdjacent, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.PreConsequenceAdjacencyV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(WasAdjacentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { sequence },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", wasAdjacent, WasAdjacentOutput, EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: removals),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticRosterCapabilities.ChooseTargetSetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter,
                            RosterQueryOrigin.Actor),
                        // "adjacent to you", which p.19 measures as one space from her own.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter,
                            RosterUnitKind.Minion),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        // A Skill's activity is not the one to ask here: this text is applied inside an
                        // Attack, and p.18 asks immunity about the activity being performed.
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Attack),
                        EffectInputBinding.FromLiteral("minimum", EffectValue.Integer(0)),
                        EffectInputBinding.FromLiteral(
                            "maximum", EffectValue.Integer(maximumRemoved))
                    },
                    outputs: SelectionOutputs(maximumRemoved)),
                RemoveNode(removeFirst, select, "firstSpace")
            };

            if (maximumRemoved > 1)
            {
                nodes.Add(RemoveNode(removeSecond, select, "secondSpace"));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One of the removals the selection above named, by the space it published.
        ///
        /// <para><c>if able</c> rather than mandatory: a chooser who named fewer minions than the card
        /// allowed leaves the later node with nothing bound, and that is the answer they gave rather
        /// than a step that failed. p.10's mandatory rule would stop the text there instead, which
        /// would be a different card.</para>
        /// </summary>
        private static EffectNodeDefinition RemoveNode(
            EffectNodeId nodeId, EffectNodeId selectionNodeId, string spaceOutput) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticExchangeCapabilities.RemoveV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", selectionNodeId, spaceOutput, EffectValueKind.Coordinate),
                    // "enemy minion" - p.19 makes a minion a unit, and Remove's eligible-piece
                    // declaration is what keeps this from reaching a token standing in the same disk.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName,
                        RemoveEligiblePieceKind.MinionMiniature)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                    new EffectOutputBinding(
                        RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                });

        /// <summary>What the removal selection publishes, which is one space per minion it may name.</summary>
        private static EffectOutputBinding[] SelectionOutputs(int maximumRemoved) =>
            maximumRemoved > 1
                ? new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                }
                : new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate)
                };

        /// <summary>
        /// "Swap with a friendly minion in radius." - Back to Back, and the only card of hers that
        /// moves her own piece.
        ///
        /// <para>Two nodes: the minion, and p.19's Swap with the actor fixed as one of its two objects.
        /// It is <c>synthetic-swap-actor@1</c> rather than the general Swap for the reason that
        /// capability records - the actor is in the frame, so a walk judging whether this card can do
        /// anything can ask the same question the choice will ask, and a branch is never offered whose
        /// Swap would then be refused.</para>
        ///
        /// <para>p.19 states that a Swap "does not count as movement or placement", so nothing here
        /// reaches the Movement action's reachability and neither piece has to be able to walk
        /// anywhere. The declared activity is the Skill's own, because there is no enclosing Attack to
        /// inherit one from.</para>
        /// </summary>
        private static EffectDefinition CreateBackToBack(
            CardDefinitionId cardId, string contentRevision, int radius)
        {
            var root = new EffectNodeId("back-root");
            var select = new EffectNodeId("back-target");
            var swap = new EffectNodeId("back-swap");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, swap }),
                    FriendlyMinionInRadius(select, swap),
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
                });
        }

        /// <summary>
        /// "Swap two minions in radius[, ignoring heavy minion immunity]." - Listen Up at Radius 1,
        /// Roger Roger at 2, and Ready and Waiting at 2 with the clause.
        ///
        /// <para><b>One question, not two.</b> <c>choose-target-pair@1</c> asks for both minions at
        /// once and canonicalises the answer, because the card names an unordered pair: swapping A with
        /// B and B with A are the same act, and asking twice would let the same minion be named twice
        /// or make the order look like it mattered.</para>
        ///
        /// <para><b>The pair is any two minions in the disk, of either side.</b> The sentence says
        /// "two minions" without a relation, and Sabina's kit is command rather than aggression - so
        /// the printed reading is that she may reposition an enemy's minions as readily as her own.
        /// <see cref="SourceTeamRelation.AnyTeam"/> is that absence stated rather than a widening.</para>
        ///
        /// <para><b>Ready and Waiting's whole Tier III purchase is the immunity override.</b> Its two
        /// siblings print the same swap and differ from it only in Radius, so the clause is declared
        /// here and nowhere else. It is an <c>ImmunityOverrideDeclaration</c> on both nodes rather than
        /// one, because p.18 makes immunity a targeting rule as well as an operation rule: a Heavy
        /// minion that the override admits has to be offered as a candidate <em>and</em> accepted by
        /// the exchange, and declaring it on only the selection would offer a piece the Swap then
        /// refused.</para>
        /// </summary>
        private static EffectDefinition CreateMinionSwap(
            CardDefinitionId cardId, string contentRevision, int radius, bool ignoresHeavyImmunity)
        {
            var root = new EffectNodeId("swap-root");
            var pair = new EffectNodeId("swap-pair");
            var swap = new EffectNodeId("swap-exchange");
            var overrides = ignoresHeavyImmunity
                ? new[] { new ImmunityOverrideDeclaration(
                    RuleActivity.Swap, ImmunityOverrideReason.PrintedHeavyMinionImmunity) }
                : null;

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { pair, swap }),
                    new EffectNodeDefinition(
                        pair, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetPairV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(radius)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.AnyTeam),
                            // Milestone 8.35l. The pair could not name its consumer until the
                            // capability declared the parameter, so nothing narrowed this offer: with
                            // Wasp's Magnetic Dagger standing over the only two minions beside her,
                            // "Swap two minions in radius" was offered on a board where no legal pair
                            // existed, the player took it, and the card was spent having moved
                            // nothing. p.19's Swap moves both objects, so one barred piece bars the
                            // pair - which is why the narrowing has to be over pairs and why a board
                            // left with a single legal minion greys the Skill rather than offering an
                            // answer that cannot be completed.
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                        },
                        immunityOverrides: overrides),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "first", pair, "firstSpace", EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "second", pair, "secondSpace", EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                        },
                        immunityOverrides: overrides)
                });
        }

        /// <summary>
        /// "If there are two or more friendly minions in radius you may retrieve a discarded card, then
        /// move up to 1 / 2 spaces." - Steady Advance and Unwavering Resolve.
        ///
        /// <para><b>The count gates the whole sentence, both halves of it.</b> "If there are two or
        /// more" is printed once at the front, so the conditional wraps a sequence rather than the
        /// retrieve alone - a hero standing beside one minion neither retrieves nor moves.</para>
        ///
        /// <para><b>Declining the retrieval does not skip the move.</b> They are siblings under the
        /// gate, each carrying its own <c>may</c>: "you may retrieve a discarded card, then move up to
        /// N spaces" offers two decisions, and answering the first with no does not answer the second.
        /// A composition that nested the move inside the retrieval would have made the card read "if
        /// you retrieve, then move", which is a sentence it does not print.</para>
        ///
        /// <para><b>The count is presence rather than a target.</b> Nobody is chosen and nothing
        /// happens to the minions, so <c>presence-count@1</c> answers it and no immunity question is
        /// asked - p.18 makes immunity a rule about being targeted, and counting is not
        /// targeting.</para>
        ///
        /// <para>The move is Sabina's own, at <see cref="DirectMoveDestinationPolicy.Unrestricted"/>:
        /// "move up to N spaces" says how far and nothing about where, so p.11's own reachability is
        /// the whole of the bound, and <c>up to</c> makes standing still one of the answers.</para>
        /// </summary>
        private static EffectDefinition CreateRegroup(
            CardDefinitionId cardId, string contentRevision, int radius, int moveDistance)
        {
            var root = new EffectNodeId("regroup-root");
            var count = new EffectNodeId("regroup-count");
            var gate = new EffectNodeId("regroup-gate");
            var sequence = new EffectNodeId("regroup-sequence");
            var retrieve = new EffectNodeId("regroup-retrieve");
            var actorSpace = new EffectNodeId("regroup-actor-space");
            var move = new EffectNodeId("regroup-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { count, gate }),
                    new EffectNodeDefinition(
                        count, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.PresenceCountV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(radius)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                RosterUnitKind.Minion),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.AlliedWithSource),
                            // "two or more": the presence answer is the count reaching the minimum,
                            // which the capability states as a bound rather than this file comparing.
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.MinimumPresentParameter,
                                EffectValue.Integer(2)),
                            // She is a hero, so neither exclusion can change a count of minions; both
                            // are declared false rather than left to a default, because the schema
                            // requires them and a card that means neither should say so.
                            EffectInputBinding.FromLiteral("excludeActor", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral("excludeTarget", EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("count", EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { sequence },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", count, SyntheticRosterCapabilities.PresentOutput,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { retrieve, actorSpace, move }),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            // p.10: retrieving "means to take one of YOUR cards back into your hand",
                            // and "You can never retrieve another hero's cards" - so the owner and the
                            // chooser are the same hero and both are the actor.
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
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(moveDistance)),
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
        /// "Move a friendly minion in radius 1 space, to a space in radius. May repeat once / up to two
        /// times." - Troop Movement at Radius 2, Marching Orders and Path to Victory at 3.
        ///
        /// <para><b>The sentence prints "in radius" twice and means it twice.</b> The first bounds who
        /// may be moved and is the selection's own reach; the second bounds where that minion may end
        /// up. Without the second, this card could walk a minion one space further out of her command
        /// each time it was played, which is the opposite of what it says.</para>
        ///
        /// <para><b>MILESTONE 8.83: BOTH "IN RADIUS" NOW READ HER ICON, AND ONE OF THEM USED TO READ A
        /// LITERAL.</b> The leash declared milestone 8.22's
        /// <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> with the face's printed number
        /// written out beside it - 2 for Troop Movement, 3 for Marching Orders and Path to Victory -
        /// while the SELECTION above it is a printed-icon arm that asks <c>RadiusValueQuery</c> on
        /// every ask. So p.16's Radius item widened who she could command and left where she could put
        /// them exactly where it was: with the item in play her disk of candidates was one ring wider
        /// than her disk of destinations, and a minion picked from the outer ring had nowhere legal to
        /// go. p.9 settles which of the two readings this sentence wants - "if the card text mentions
        /// Range/Radius, it refers to this value" - so the leash is milestone 8.82's
        /// <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/> and declares no number
        /// at all. The literal went with it; there is nothing left in this method for a
        /// <c>radius</c> parameter to say. Xargatha's Siren's Call keeps the declared member and its
        /// literal 1, because its sentence writes the number down ("to a space adjacent to you") and
        /// her face prints no Radius icon for an item to raise.</para>
        ///
        /// <para><b>The distance is exactly one and the move is mandatory.</b> "Move a friendly minion
        /// 1 space" prints neither "may" nor "up to", so standing still is not one of the answers -
        /// which matters here, because a minion cornered inside the disk with nowhere legal to go stops
        /// the text at that step rather than being shuffled nowhere.</para>
        ///
        /// <para><b>The repeat is a repeat of the action, and it declares NOTHING about its subject.</b>
        /// p.10 gives an unassigned decision to the card's controller, so Sabina is the one asked; the
        /// card says only "May repeat", naming no subject, so the repeated instance chooses again from
        /// scratch - and the disk it measures against is wherever she is standing then. The choice is
        /// free: the same minion again is one of the answers, exactly as a different one is.</para>
        ///
        /// <para><b>These three faces declared <see cref="RepeatTargetBinding.DifferentTarget"/> until
        /// the phantom was removed, and none of them prints a word that supports it.</b> All three read
        /// "May repeat once / up to two times" and stop; no "different target" clause exists on any of
        /// them, so the declaration described nothing the cards say. It was also INERT, which is why it
        /// survived so long: the only route a non-<c>SameTarget</c> binding has into a repeated instance
        /// is <c>CardActionResolutionAuthority.ExclusionFor</c> and its card-text twin
        /// <c>CardTextRepeatExclusionFor</c>, and both yield an exclusion for
        /// <see cref="ActionKind.Attack"/> alone - these are Skills. So the live offer was always the
        /// free one the faces print, and removing the declaration moves no behaviour; it stops the
        /// content claiming a clause the printed text does not carry. Per the standing "cards are the
        /// definition" ruling, an absence in the text is an absence in the content.</para>
        /// </summary>
        private static EffectDefinition CreateCommandedMove(
            CardDefinitionId cardId, string contentRevision, int maximumRepeats)
        {
            var root = new EffectNodeId("troop-root");
            var select = new EffectNodeId("troop-target");
            var move = new EffectNodeId("troop-move");
            var repeat = new EffectNodeId("troop-repeat");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, move, repeat }),
                    FriendlyMinionInRadius(select),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", select,
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.WithinActorPrintedRadius)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: maximumRepeats)
                });
        }

        /// <summary>
        /// "An enemy hero in radius adjacent to your friendly minion discards a card, if able / or is
        /// defeated." - Close Support and Covering Fire, both at Radius 3.
        ///
        /// <para><b>Two bounds measured from two different places.</b> The disk is measured from
        /// Sabina and the adjacency from each of her minions, which is why the second is a narrowing on
        /// the selection rather than a change of its origin. A hero standing alone inside her radius is
        /// not offered at all: the clause is part of who may be picked, so there is nobody to offer and
        /// then refuse.</para>
        ///
        /// <para><b>The pair differs exactly as Wasp's Shock and Electroblast differ.</b> Close Support
        /// prints "if able", which is one step with one way to complete and no consequence for a hero
        /// holding nothing; Covering Fire prints "or is defeated", which is ledger question 13's
        /// construction - one mandatory step with two ways to complete, the second reached only when
        /// the first could not be. The escalation reads the discard's own inability rather than asking
        /// the board a second question.</para>
        ///
        /// <para>The chooser of the discard is the hero discarding, which is what
        /// <c>synthetic-discard-chosen-card</c>'s two revisions carry: p.10 gives a decision about a
        /// hero's own cards to that hero.</para>
        /// </summary>
        private static EffectDefinition CreateSupportedDiscard(
            CardDefinitionId cardId, string contentRevision, int radius, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("support-root");
            var select = new EffectNodeId("supported-enemy");
            var discard = new EffectNodeId("support-discard");
            var escalation = new EffectNodeId("support-escalation");
            var defeat = new EffectNodeId("support-defeat");

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
                            SyntheticRosterCapabilities.DistanceParameter,
                            EffectValue.Integer(radius)),
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
                            SyntheticRosterCapabilities.AdjacentToFriendlyMinionParameter,
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
        /// "a friendly minion in radius", as the printed-icon selection that publishes both who it is
        /// and where it is standing.
        ///
        /// <para>It is the Radius icon's own selection rather than a distance this file writes down,
        /// which is the distinction milestone 7.2b drew: where a card prints an icon, the reach is the
        /// icon's and an item's <c>+1</c> reaches it; where the text states a number, the definition
        /// states it. Both of Sabina's users of this print a Radius.</para>
        ///
        /// <para><b>It declares its consumer where one narrows it, and until milestone 8.35l it could
        /// not.</b> Milestone 6.8l's narrowing put each CANDIDATE to the consuming node, and this
        /// selection's candidates are minions while both consumers here take a coordinate - so every
        /// candidate came back refused, no candidate was offered, and the printed Skill silently
        /// stopped appearing at all. The recorded conclusion was that declaring the consumer was a
        /// mistake; the real fault was that the question was asked with the wrong value. Milestone
        /// 8.35l puts the selection's own published output to the consumer instead, so Back to Back's
        /// Swap is now asked about the space it actually binds and answers about the piece the player
        /// would name.</para>
        ///
        /// <para><b>What that recovered.</b> Wasp's Magnetic Dagger bars enemy units in its Radius
        /// from being swapped; p.19's Swap moves both objects, so Sabina inside that disk cannot swap
        /// with anyone and a partner inside it cannot be swapped with. Undeclared, the Skill was
        /// offered anyway, the player took it, the Swap stopped, and the card was spent for nothing -
        /// which is the defect milestone 8.35l exists to close. Immunity was already filtered here;
        /// a restriction is a separate question and was not.</para>
        ///
        /// <para><b>Troop Movement's copy deliberately declares none.</b> Its consumer is a direct
        /// Move, whose refusals are about destinations rather than about this piece, and narrowing on
        /// them would change which minions that card offers - a different card's behaviour, decided by
        /// evidence that card owns. The parameter is optional precisely so this can be said one call
        /// site at a time.</para>
        /// </summary>
        private static EffectNodeDefinition FriendlyMinionInRadius(
            EffectNodeId nodeId,
            EffectNodeId consumerNodeId = default) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                inputs: consumerNodeId.IsDefined
                    ? new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AlliedWithSource),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(consumerNodeId))
                    }
                    : new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AlliedWithSource)
                    },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate)
                });

        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        private static readonly EffectNodeId pushSequence = new EffectNodeId("shot-push-sequence");

        private const string ActorSpaceOutput = "space";
        private const string TargetSpaceOutput = "space";
        private const string AdjacentOutput = "adjacent";
        private const string WasAdjacentOutput = "wasAdjacent";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

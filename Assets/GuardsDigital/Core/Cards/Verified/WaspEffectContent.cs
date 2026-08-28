using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Wasp's card text: her five telekinetic Skills at milestone 8.6 - the
    /// first verified official card text this project runs - her two kinetic ones at 8.8, and at 8.9
    /// her two basic cards, which are also the first Attack text this project applies at one of
    /// rulebook p.14's numbered moments.
    ///
    /// <para><b>Nothing here is a handler.</b> Every node below names a capability that already existed
    /// and a policy that is now declarable, which is what milestone 8.2's capability matrix promised for
    /// the whole of Wasp's set: "no custom handler anywhere". What 8.6 added is vocabulary the general
    /// contracts were missing - the orbit destination policy, the printed-Range selection with p.19's
    /// straight-line complement, the Place anchored at the acting hero, and the repeat maximum with its
    /// same-target binding - and 8.8 added three more to the Push declarations: the least distance a
    /// card prints, the most units it may take, and the side of the board it may take them from. Each
    /// lives beside the general rule it belongs to rather than here.</para>
    ///
    /// <para><b>Why the families read differently.</b> Lift Up, Control Gravity and Center of Mass
    /// print "in radius"; Telekinesis and Mass Telekinesis print "in range". p.9 says card text
    /// mentioning either "refers to this value" - the printed icon's - so both are selections bounded by
    /// the card rather than by a number written into a node, and they differ only in which icon they
    /// read. Kinetic Repulse and Kinetic Blast print neither and are bounded by adjacency instead, so
    /// every number in them is written down. A distance that <em>is</em> written down is card text and
    /// p.16 never raises it: the single space the Move covers, and the three or four the Push does.</para>
    ///
    /// <para><b>What each definition deliberately does not say.</b> No card-name branch, no immunity
    /// question of its own, no geometry, and no ordering beyond the printed sentence's. Immunity is the
    /// selection's, through the shared targeting contract; the destinations are the declared policy's;
    /// and the repeat is the last child of the root because that is where p.10 puts it - after you
    /// perform this action, you may perform the entire action again.</para>
    /// </summary>
    public static class WaspEffectContent
    {
        public static EffectDefinition HighVoltageTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("high-voltage-may");
            var sequence = new EffectNodeId("high-voltage-sequence");
            var chooseMinion = new EffectNodeId("high-voltage-minion");
            var defeatMinion = new EffectNodeId("high-voltage-defeat");
            var adjacency = new EffectNodeId("high-voltage-adjacency-if-able");
            var rider = new EffectNodeId("high-voltage-adjacency-rider");
            var chooseHero = new EffectNodeId("high-voltage-adjacent-hero");
            var discard = new EffectNodeId("high-voltage-discard");

            return new EffectDefinition(
                new EffectDefinitionId("wasp-high-voltage-triggered"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { sequence }, chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { chooseMinion, defeatMinion, adjacency }),
                    new EffectNodeDefinition(
                        chooseMinion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(defeatMinion))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        defeatMinion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.MinionParameter,
                                chooseMinion,
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        adjacency, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                        childNodeIds: new[] { rider }),
                    new EffectNodeDefinition(
                        rider, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { chooseHero, discard }),
                    new EffectNodeDefinition(
                        chooseHero, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.HeroAdjacentToMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.MinionParameterName,
                                chooseMinion,
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter,
                                chooseHero,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// Wasp's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, so a card that has one
        /// carries it and a card that does not still states the milestone that owes it -
        /// <see cref="ActionDefinition"/> refuses both to be absent at once.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WaspCardContent.StaticBarrier))
            {
                // p.12: "When you perform a Skill action, simply apply the card text." The whole of
                // this card's text is the `This turn` barrier, so its effect is the one node that
                // raises it and there is nothing to sequence it with.
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    WaspActiveEffectContent.CloseBarrierInRadiusV1, "barrier");
            }

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

            if (cardId.Equals(WaspCardContent.KineticRepulse))
            {
                // "3 spaces" - one number, so the least the card names is the same one and nobody is
                // asked how far.
                return CreateKineticPush(cardId, contentRevision, leastDistance: 3, distance: 3);
            }

            if (cardId.Equals(WaspCardContent.KineticBlast))
            {
                // "3 or 4 spaces" - the upgrade's whole difference from Kinetic Repulse, and the first
                // verified distance a card writes as a choice.
                return CreateKineticPush(cardId, contentRevision, leastDistance: 3, distance: 4);
            }

            return null;
        }

        /// <summary>
        /// Wasp's card text for one printed Attack, or <c>null</c> for an Attack whose composition is
        /// still owed - the same pairing <see cref="SkillEffectFor"/> has with the table's own debt
        /// column, and refused the same way by <see cref="ActionDefinition"/> if the two disagree.
        ///
        /// Milestone 8.9 composes one: Magnetic Dagger's. Its Attack itself needs nothing declared -
        /// "Target a unit adjacent to you" is p.13's own sentence for a non-Ranged Attack, word for
        /// word, so the printed target policy narrows nothing and the card runs through the combat
        /// contracts exactly as every Attack before it did. What is new is the rider hanging off
        /// milestone 8.5's seam.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WaspCardContent.MagneticDagger))
            {
                return CreateActiveEffect(
                    AttackTextEffectId(cardId), contentRevision,
                    WaspActiveEffectContent.PinEnemyUnitsInRadiusV1, "pin");
            }

            if (cardId.Equals(WaspCardContent.Shock) || cardId.Equals(WaspCardContent.Electrocute))
            {
                // The two print the same sentence and differ only in the Radius icon beside it, which
                // the selection reads off the card rather than out of the definition - so one
                // composition is not a shortcut here, it is what the two cards actually share.
                return CreateRadiusDiscardRider(cardId, contentRevision, escalatesToDefeat: false);
            }

            if (cardId.Equals(WaspCardContent.ThunderBoomerang))
            {
                return CreateConditionalRepeat(cardId, contentRevision);
            }

            if (cardId.Equals(WaspCardContent.Electroblast))
            {
                // The same sentence with its last three words changed: "or is defeated" where the two
                // below print "if able". Ledger question 13 reads that as one mandatory step with two
                // ways to complete rather than a choice either player makes, so what differs in the
                // composition is one conditional arm and nothing about the question the actor answers.
                return CreateRadiusDiscardRider(cardId, contentRevision, escalatesToDefeat: true);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's three moments an Attack's composed text is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// Every Attack rider in Wasp's set prints "After the attack:", so this answers one moment
        /// today; it is asked per card rather than inferred from "this card has text" because the page
        /// has three moments and a card that printed "Before the attack" would otherwise run its text
        /// at the wrong one silently.
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(WaspCardContent.MagneticDagger) ||
            cardId.Equals(WaspCardContent.Shock) ||
            cardId.Equals(WaspCardContent.Electrocute) ||
            cardId.Equals(WaspCardContent.Electroblast) ||
            cardId.Equals(WaspCardContent.ThunderBoomerang)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        /// <summary>
        /// "After the attack: If you targeted a hero may repeat once on a different target." - Thunder
        /// Boomerang's sentence, and the only Wasp Attack that asks for itself again. Milestone 8.10.
        ///
        /// <para><b>Three printed clauses, three declarations.</b> "If you targeted a hero" is a
        /// predicate over the Attack this text is printed inside, so it reads that Attack's own target
        /// rather than anything a node here did. "may" is p.10's confirm-or-decline, which is the node
        /// policy. "once" is the repeat's declared maximum. "on a different target" is the repeat's
        /// declared binding, and it excludes the unit this instance took rather than naming a selection
        /// node, because the target of an Attack is the action's own state.</para>
        ///
        /// <para>The repeated instance is an ordinary Attack: it chooses its own target out of the
        /// smaller set, takes its own Defense response, and applies this same text again - where the
        /// repeat node is no longer eligible, because one is all the card allows.</para>
        /// </summary>
        private static EffectDefinition CreateConditionalRepeat(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("thunder-repeat-root");
            var repeat = new EffectNodeId("thunder-repeat");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.AttackTookAHeroV1,
                        childNodeIds: new[] { repeat },
                        outputs: new[]
                        {
                            // Declared for the reason the Block rider's is: a predicate that publishes
                            // its answer must have the node admit it, or a restore refuses the recorded
                            // outcome. Milestone 8.11 found this; no corpus payload held a paused
                            // Thunder repeat, so 8.10 never exercised it.
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.TookAHeroOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        // p.10 gives an unassigned decision to the card's controller, and "may repeat"
                        // assigns none - so the hero performing the Attack is the one asked.
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentTarget)
                });
        }

        /// <summary>
        /// Wasp's card text for one printed Block, or <c>null</c> for one whose text is the Block
        /// condition and nothing more. Milestone 8.11.
        ///
        /// <para>All three Green cards print "Block a ranged attack"; that clause is the Block
        /// condition, which <see cref="PrintedDefenseResolution.Block"/> declares and the Attack
        /// resolver evaluates, so it is not composed as an effect. What is composed is what two of them
        /// print <em>after</em> it - "if you do, an enemy hero in range ... discards a card, if able" -
        /// and Stop Projectiles, which prints nothing after it, carries no root.</para>
        /// </summary>
        public static EffectDefinition DefenseTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(WaspCardContent.DeflectProjectiles))
            {
                // "other than the attacker" - the one clause separating this card from Reflect.
                return CreateBlockDiscardRider(cardId, contentRevision, excludesTheAttacker: true);
            }

            if (cardId.Equals(WaspCardContent.ReflectProjectiles))
            {
                return CreateBlockDiscardRider(cardId, contentRevision, excludesTheAttacker: false);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's moments a Block's composed text is applied at. The page numbers the
        /// defender's own text step 4 - "Defender applies Defense action text, if any" - and that is the
        /// only moment a Defense action has.
        /// </summary>
        public static CardTextTiming DefenseTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(WaspCardContent.DeflectProjectiles) ||
            cardId.Equals(WaspCardContent.ReflectProjectiles)
                ? CardTextTiming.DefenseAction
                : CardTextTiming.None;

        /// <summary>
        /// "Block a ranged attack; if you do, an enemy hero in range[, other than the attacker,]
        /// discards a card, if able." - Deflect Projectiles' sentence and Reflect Projectiles', which
        /// differ in one clause. Milestone 8.11.
        ///
        /// <para><b>"if you do" is the Block condition, read again.</b> The rider fires exactly when
        /// the Block succeeds, and a Block succeeds exactly when the attack being answered is ranged -
        /// so the condition is asked of the enclosing Attack's printed subtype rather than of a fact
        /// the resolver would have to publish into a frame it does not own. p.14 puts the defender's
        /// text at step 4, before step 5's total and step 6's check, which is why this can be read at
        /// all: the answer is already decided when the card is revealed.</para>
        ///
        /// <para><b>The selection is the defender's, anchored on the defender.</b> p.9's Range icon is
        /// printed on the Block card, so the disk is measured from the hero performing the Block and
        /// composed with that hero's items; p.10 gives the unassigned choice to the card's controller,
        /// who here is the defender rather than the attacker. Deflect's "other than the attacker"
        /// excludes one named hero, and Reflect excludes nobody - which is the whole of what separates
        /// two otherwise identical cards.</para>
        /// </summary>
        private static EffectDefinition CreateBlockDiscardRider(
            CardDefinitionId cardId, string contentRevision, bool excludesTheAttacker)
        {
            var root = new EffectNodeId("block-rider-root");
            var rider = new EffectNodeId("block-rider-sequence");
            var select = new EffectNodeId("block-rider-select");
            var discard = new EffectNodeId("block-rider-discard");

            return new EffectDefinition(
                DefenseTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.AttackIsRangedV1,
                        childNodeIds: new[] { rider },
                        outputs: new[]
                        {
                            // A predicate that publishes its answer has to have the node declare it, or
                            // a restore re-deriving the recorded outcome finds an output the node does
                            // not admit and refuses the payload. The corpus fixture this milestone adds
                            // is what found that.
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.IsRangedOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        rider, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, discard }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            // The Defense action this text is printed on is what p.18 asks immunity
                            // about, exactly as an Attack's rider asks about its Attack.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Defense),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(excludesTheAttacker))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter,
                                select,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "After the attack: An enemy hero in radius and not adjacent to you discards a card, if
        /// able." - Shock's sentence and Electrocute's, which are the same sentence. Milestone 8.10.
        ///
        /// <para><b>Two questions, both the page's.</b> p.10 gives an unassigned choice to the card's
        /// controller, so the acting hero picks which enemy hero the sentence lands on; the discard
        /// itself is the chosen hero's own, out of their own hand, which is milestone 7.1a's
        /// chooser-owned shape. Both stop the attack where they stand - the first card text in this
        /// build to do so - and milestone 8.10's continuation is what holds rulebook p.14 open across
        /// them.</para>
        ///
        /// <para><b>What is deliberately not declared.</b> The selection names no consuming node, even
        /// though the vocabulary has one. Declaring it would narrow the offer to heroes the discard can
        /// act on, and that is the opposite of what the face says: `if able` exists precisely because
        /// the chosen hero may hold nothing, so a choice restricted to heroes holding something would
        /// make the printed words unreachable. The actor chooses among every enemy hero in the disk,
        /// and the discard then applies to whoever was chosen.</para>
        /// </summary>
        private static EffectDefinition CreateRadiusDiscardRider(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("radius-discard-root");
            var select = new EffectNodeId("radius-discard-select");
            var discard = new EffectNodeId("radius-discard-discard");
            var escalation = new EffectNodeId("radius-discard-escalation");
            var defeat = new EffectNodeId("radius-discard-defeat");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: escalatesToDefeat
                            ? new[] { select, discard, escalation }
                            : new[] { select, discard }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            // The enclosing action is the Attack this text is printed on, and p.18
                            // declares immunity per activity - so the offer asks immunity the same
                            // question the Attack itself asked.
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            // "enemy", from the card's perspective, which is the acting hero's.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            // "and not adjacent to you".
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    escalatesToDefeat
                        ? new EffectNodeDefinition(
                            // No `if able` here, because the face prints none. Ledger question 13 reads
                            // "discards a card, or is defeated" as one mandatory step with two ways to
                            // complete, so the step always applies and reports which way it went -
                            // which is the whole of what revision 2 adds.
                            discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticCardStateCapabilities.DiscardChosenCardV2,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticCardStateCapabilities.HeroParameter,
                                    select,
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
                        : new EffectNodeDefinition(
                            // `if able` is p.17's keyword and p.10's non-mandatory wording, and it is
                            // over the discard alone: a chosen hero with an empty hand gives nothing
                            // and the attack goes on.
                            discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                            SyntheticCardStateCapabilities.DiscardChosenCardV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticCardStateCapabilities.HeroParameter,
                                    select,
                                    SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                    EffectValueKind.Hero)
                            })
                }
                .Concat(escalatesToDefeat
                    ? new[]
                    {
                        new EffectNodeDefinition(
                            // The second arm, reached only where the first could not complete. It is a
                            // branch and not a question: p.10 gives nobody a decision here, and the
                            // reading that made "discards a card" unreachable text is exactly the one
                            // ledger question 13 withdrew.
                            escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                            SyntheticEffectCapabilities.IsFlaggedV1,
                            childNodeIds: new[] { defeat },
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    "flag",
                                    discard,
                                    SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                    EffectValueKind.Boolean)
                            }),
                        new EffectNodeDefinition(
                            defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticConsequenceCapabilities.DefeatHeroV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticConsequenceCapabilities.HeroParameter,
                                    select,
                                    SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                    EffectValueKind.Hero)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                                    EffectValueKind.Hero)
                            })
                    }
                    : Enumerable.Empty<EffectNodeDefinition>())
                .ToArray());
        }

        /// <summary>
        /// One node: the capability that creates this card's declared Active effect.
        ///
        /// Both of milestone 8.9's cards compose to this shape and neither needs more. Everything
        /// either sentence says is definition content on the Active effect itself - the scope, the
        /// radius, the restricted actors, the overlay's arms - so an effect tree with branches in it
        /// would be describing the same rule twice in a second vocabulary. The node is
        /// <see cref="EffectFailurePolicy.Mandatory"/> because the printed text is not optional and
        /// creating the effect cannot fail, and it asks no question, which is what lets Magnetic
        /// Dagger's copy run inside an Attack at all (milestone 8.5's card-text rule).
        /// </summary>
        private static EffectDefinition CreateActiveEffect(
            EffectDefinitionId effectId,
            string contentRevision,
            EffectCapabilityReference capability,
            string nodePrefix)
        {
            var root = new EffectNodeId($"{nodePrefix}-root");
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
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
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
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.EquidistantFromActor)
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
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.Skill),
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
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.UnitOrToken),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero)
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

        /// <summary>
        /// "Push up to 2 enemy units adjacent to you 3 spaces; if a pushed hero is stopped by an
        /// obstacle, that hero discards a card, if able." - and, for Kinetic Blast, "3 or 4 spaces".
        ///
        /// <para><b>One push, then one rider per unit it could have pushed.</b> The batch is a single
        /// node because p.18 makes it one event: "you choose the distance and then push all those units
        /// that many spaces at the same time". It resolves every target against the state before
        /// anything moved and commits farthest-first, and none of that is restated here - the card
        /// declares who may be pushed, how many, and how far, and the capability owns the rest.</para>
        ///
        /// <para><b>Why two riders and not one.</b> The sentence after the semicolon is about a hero,
        /// not about the push: each pushed hero that an obstacle stopped discards. The push publishes a
        /// stop cause per hero it moved, so the rider is a <see cref="EffectNodeKind.Conditional"/> per
        /// hero, and the card's own "up to 2" is why there are exactly two. Both read the same predicate
        /// over a fact the push already decided; neither asks the board anything.</para>
        ///
        /// <para><b>Who discards, and what "if able" means.</b> The card names no chooser for the card
        /// discarded, so p.10's worked example stands - the hero it happened to picks from their own
        /// hand - and that is the discard capability's own contract rather than something declared
        /// here. `if able` is the node's policy: a hero holding nothing gives nothing and the action
        /// carries on, which is what an empty hand means and not what a failure means.</para>
        /// </summary>
        private static EffectDefinition CreateKineticPush(
            CardDefinitionId cardId,
            string contentRevision,
            int leastDistance,
            int distance)
        {
            var root = new EffectNodeId("kinetic-root");
            var push = new EffectNodeId("kinetic-push");
            var firstRider = new EffectNodeId("kinetic-first-rider");
            var firstDiscard = new EffectNodeId("kinetic-first-discard");
            var secondRider = new EffectNodeId("kinetic-second-rider");
            var secondDiscard = new EffectNodeId("kinetic-second-discard");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { push, firstRider, secondRider }),
                    new EffectNodeDefinition(
                        // `up to 2` is p.10's non-mandatory wording, and it is over the units: the
                        // actor may push two, one, or none, and pushing none is a legal result rather
                        // than a step that failed. The spaces are declared separately below, which is
                        // the whole of what milestone 8.8 added to this capability.
                        push, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.ObjectPushV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(distance)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(leastDistance)),
                            // Neither card says "ignoring obstacles", and p.18 makes that a property of
                            // the pushing effect rather than a default, so it is declared false.
                            EffectInputBinding.FromLiteral(
                                "ignoresObstacles", EffectValue.Boolean(false)),
                            // "units", which p.19 separates from tokens by name.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.Unit),
                            // "enemy", from the card's perspective, which is the acting hero's.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MaximumTargetsParameterName,
                                EffectValue.Integer(2))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.FirstStoppedHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName,
                                EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.SecondStoppedHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.SecondHeroWasStoppedOutputName,
                                EffectValueKind.Boolean)
                        })
                }
                .Concat(Rider(
                    firstRider,
                    firstDiscard,
                    push,
                    SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName,
                    SyntheticDisplacementCapabilities.FirstStoppedHeroOutputName))
                .Concat(Rider(
                    secondRider,
                    secondDiscard,
                    push,
                    SyntheticDisplacementCapabilities.SecondHeroWasStoppedOutputName,
                    SyntheticDisplacementCapabilities.SecondStoppedHeroOutputName))
                .ToArray());
        }

        /// <summary>
        /// "if a pushed hero is stopped by an obstacle, that hero discards a card, if able", asked once
        /// about one hero.
        ///
        /// The predicate is a pure reading of a fact the push published - milestone 8.5's own
        /// <see cref="EffectNodeKind.Conditional"/> shape - so the question of whether the board's edge
        /// counts as an obstacle is answered where the push resolves, once, rather than by two riders
        /// that could disagree. A push that pushed nobody publishes no fact, the binding resolves to
        /// nothing, and the predicate reads false, which is the same answer as a hero who was not
        /// stopped and the same branch.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> Rider(
            EffectNodeId rider,
            EffectNodeId discard,
            EffectNodeId push,
            string stoppedOutputName,
            string heroOutputName)
        {
            yield return new EffectNodeDefinition(
                rider, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { discard },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag", push, stoppedOutputName, EffectValueKind.Boolean)
                });

            yield return new EffectNodeDefinition(
                discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter,
                        push,
                        heroOutputName,
                        EffectValueKind.Hero)
                });
        }

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");

        /// <summary>
        /// A printed Attack's card text names the text rather than the action, because unlike a Skill's
        /// root it is not the action - p.14 applies it at a moment inside one.
        /// </summary>
        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-attack-text");

        private static EffectDefinitionId DefenseTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-defense-text");
    }
}

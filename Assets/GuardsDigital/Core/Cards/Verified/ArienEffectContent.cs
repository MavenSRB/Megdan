using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Arien's printed card text as typed effect definitions - milestone 8.20.
    ///
    /// <para>It sits beside <see cref="ArienCardContent"/> for the reason
    /// <see cref="WaspEffectContent"/> sits beside hers: that file is the transcription, and this one is
    /// the composition. A reader auditing what the cards say reads one; a reader auditing what the
    /// engine does with it reads the other.</para>
    ///
    /// <para><b>Every node here is a declaration over a registered contract.</b> Nothing reads a card
    /// name, a hero name or a printed sentence at runtime. Where a shape already existed - Wasp's radius
    /// discard-or-defeat, her orbit's bounded repeat, milestone 8.19b's line behind a target - this file
    /// declares Arien's numbers against it rather than restating the shape.</para>
    /// </summary>
    public static class ArienEffectContent
    {
        /// <summary>
        /// Living Tsunami's permanent ability: "Once per turn, before performing an Attack action, you
        /// may move 1 space."
        ///
        /// <para><b>Three printed clauses, three declarations, and none of them is here.</b> "Once per
        /// turn" is the trigger's frequency, "before performing an Attack action" is the action kind it
        /// precedes, and both are declared on the card's
        /// <see cref="UltimateRuleDefinition.BeforeActionAbility"/>. What is left for this definition is
        /// the fourth clause - the move itself - which is why the table below has no condition in
        /// it.</para>
        ///
        /// <para><b>"you may move" is the node's own policy.</b> p.10's `may` opens a confirm-or-decline
        /// choice and declining carries on, which is exactly what the trigger must do: an Ultimate that
        /// could not be declined would move Arien before every Attack he ever performs. The distance is
        /// the printed one exactly - p.11 makes an unqualified number exact - so the actor is asked
        /// whether, and then where, and never how far.</para>
        ///
        /// <para>The subject reaches the move through <see cref="SyntheticPlacementCapabilities
        /// .ActorSpaceV1"/> rather than through a selection, for the reason milestone 6.5a built that
        /// bridge: "you may move" names no piece to choose between, and a one-candidate prompt would
        /// invent a decision the card does not offer.</para>
        /// </summary>
        public static EffectDefinition LivingTsunamiTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("tsunami-root");
            var actorSpace = new EffectNodeId("tsunami-actor-space");
            var move = new EffectNodeId("tsunami-move");

            return new EffectDefinition(
                new EffectDefinitionId("arien-living-tsunami-effect"),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        // p.10 gives an unassigned decision to the card's controller, and "you may
                        // move" assigns none - so the confirm-or-decline the `may` policy opens goes
                        // to the acting hero without the node naming one.
                        move, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            // The card says how far and nothing about where, so nothing is filtered by
                            // where anybody stands - p.11's own breadth.
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer((int)DirectMoveDestinationPolicy.Unrestricted))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// Arien's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, and
        /// <see cref="ActionDefinition"/> refuses a Skill carrying neither this nor a stated debt.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(ArienCardContent.SpellBreak))
            {
                // p.12: "When you perform a Skill action, simply apply the card text." The whole of
                // this card's text is the `This turn` lock, so its effect is the one node that raises
                // it and there is nothing to sequence it with.
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    ArienActiveEffectContent.BreakEnemySpellsInRadiusV1, "spell-break");
            }

            if (cardId.Equals(ArienCardContent.LiquidLeap) ||
                cardId.Equals(ArienCardContent.MagicalCurrent))
            {
                return CreateLeap(
                    cardId, contentRevision,
                    PlaceSpawnPointExclusion.SpacesWithASpawnPointOrAdjacentToAnEmptyOne);
            }

            if (cardId.Equals(ArienCardContent.StrangerTide))
            {
                // The same Place with the second clause dropped, which is the whole of what the Tier
                // III card buys: "without a spawn point" and no more. Composed through the same
                // builder, so the two cards cannot come to differ in anything they both print.
                return CreateLeap(
                    cardId, contentRevision, PlaceSpawnPointExclusion.SpacesWithASpawnPoint);
            }

            if (cardId.Equals(ArienCardContent.ArcaneWhirlpool))
            {
                return CreateWhirlpool(cardId, contentRevision, repeatsWhenItWasAdjacent: false);
            }

            if (cardId.Equals(ArienCardContent.EbbAndFlow))
            {
                return CreateWhirlpool(cardId, contentRevision, repeatsWhenItWasAdjacent: true);
            }

            return null;
        }

        /// <summary>
        /// Arien's card text for one printed Attack, or <c>null</c> where the face prints none or its
        /// composition is still owed - the same pairing <see cref="SkillEffectFor"/> has with the
        /// table's own debt column, and refused the same way if the two disagree.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(ArienCardContent.NobleBlade))
            {
                return CreateBeforeAttackMove(cardId, contentRevision);
            }

            if (cardId.Equals(ArienCardContent.DangerousCurrent))
            {
                return CreateCurrent(cardId, contentRevision, length: 2, repeats: false);
            }

            if (cardId.Equals(ArienCardContent.RagingStream))
            {
                return CreateCurrent(cardId, contentRevision, length: 3, repeats: false);
            }

            if (cardId.Equals(ArienCardContent.ViolentTorrent))
            {
                // The same sentence over five spaces, plus one clause the other two do not print:
                // "May repeat once on a different unit."
                return CreateCurrent(cardId, contentRevision, length: 5, repeats: true);
            }

            if (cardId.Equals(ArienCardContent.RogueWave))
            {
                return CreateWave(cardId, contentRevision, distance: 2);
            }

            if (cardId.Equals(ArienCardContent.TidalBlast))
            {
                return CreateWave(cardId, contentRevision, distance: 3);
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's three moments an Attack's composed text is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Arien is the first hero in this build whose set uses two of the three. Five of his
        /// Attacks print "Before the attack:" - a moment no implemented card had reached until milestone
        /// 8.19a made it pausable - and two print "After the attack:". Asking per card is what keeps a
        /// face that prints one from being run at the other.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(ArienCardContent.NobleBlade) ||
                cardId.Equals(ArienCardContent.DangerousCurrent) ||
                cardId.Equals(ArienCardContent.RagingStream) ||
                cardId.Equals(ArienCardContent.ViolentTorrent))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            if (cardId.Equals(ArienCardContent.RogueWave) ||
                cardId.Equals(ArienCardContent.TidalBlast))
            {
                return CardTextTiming.AfterTheAttack;
            }

            // Ceremonial Blade prints "Before the attack:" too, and states no timing here because its
            // composition is deferred: ActionDefinition refuses a moment without a root.
            return CardTextTiming.None;
        }

        /// <summary>
        /// Arien's card text for one printed Defense, or <c>null</c> where the face's whole text is the
        /// minion-modifier suppression its action already declares.
        ///
        /// <para>Aspiring Duelist is that card: "Ignore all minion defense modifiers" is a statement
        /// about which contributions the Defense Total takes, and
        /// <c>MinionDefenseContributionPolicy.SuppressAll</c> is where it is said. Expert and Master
        /// Duelist print the same sentence and then one more, and it is only the second that composes
        /// to an effect.</para>
        /// </summary>
        public static EffectDefinition DefenseTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(ArienCardContent.ExpertDuelist))
            {
                return CreateActiveEffect(
                    DefenseTextEffectId(cardId), contentRevision,
                    ArienActiveEffectContent.DuelThisTurnV1, "duel");
            }

            if (cardId.Equals(ArienCardContent.MasterDuelist))
            {
                return CreateActiveEffect(
                    DefenseTextEffectId(cardId), contentRevision,
                    ArienActiveEffectContent.DuelThisRoundV1, "duel");
            }

            return null;
        }

        /// <summary>
        /// p.14's step 4 for the two Duelist cards that carry text there, and the named absence for the
        /// one whose text is a declaration rather than an effect.
        /// </summary>
        public static CardTextTiming DefenseTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(ArienCardContent.ExpertDuelist) ||
            cardId.Equals(ArienCardContent.MasterDuelist)
                ? CardTextTiming.DefenseAction
                : CardTextTiming.None;

        /// <summary>
        /// Arien's card text for one printed Movement primary, applied at rulebook p.11's own default
        /// moment: "Unless it says otherwise, the card text comes into effect before movement."
        ///
        /// <para>Both cards compose to a single Active-effect node, for the reason Wasp's Static
        /// Barrier does: everything either sentence says is definition content on the effect itself -
        /// the scope, the disk, the two prohibitions - so an effect tree with branches in it would be
        /// describing one rule in a second vocabulary.</para>
        /// </summary>
        public static EffectDefinition MovementTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(ArienCardContent.SlipperyGround))
            {
                return CreateActiveEffect(
                    MovementTextEffectId(cardId), contentRevision,
                    ArienActiveEffectContent.SlipperyGroundAdjacentV1, "footing");
            }

            if (cardId.Equals(ArienCardContent.Deluge))
            {
                return CreateActiveEffect(
                    MovementTextEffectId(cardId), contentRevision,
                    ArienActiveEffectContent.DelugeInRadiusV1, "footing");
            }

            return null;
        }

        /// <summary>
        /// "Before the attack: You may move another unit that is adjacent to the target 1 space." -
        /// Noble Blade's sentence, and the first verified text in this project applied at rulebook
        /// p.14's step 1.
        ///
        /// <para><b>Three printed clauses, three declarations.</b> "You may" is the offer, and it is an
        /// <see cref="EffectNodeKind.Optional"/> node with the actor as its chooser, so declining costs
        /// nothing and the Attack carries on. "another unit that is adjacent to the target" is the
        /// selection: milestone 8.19b's target query centred on the enclosing Attack's own target, with
        /// the actor excluded - the reminder text's "'Another unit' never includes you" - and with the
        /// target itself excluded, because p.19's adjacency is a relation between two spaces and the
        /// target is not adjacent to itself. "1 space" is the move's declared distance, exact because
        /// p.11 makes an unqualified number exact.</para>
        ///
        /// <para><b>Why the move is `if able` when the card prints no such keyword.</b> p.11 states the
        /// rule this hangs on: "Unless the distance is optional, or <b>the move itself is optional</b>
        /// ('you may move'), you must move the exact number of spaces, or stop performing the action at
        /// that step." The move here <em>is</em> optional, so a chosen unit with nowhere to go is
        /// skipped rather than stopping the Attack - and skipping silently is what the `if able` policy
        /// names. Declaring it mandatory would have made an unlucky choice end the action; declaring it
        /// `may` would have asked the actor a second time for a decision the offer above already
        /// took.</para>
        ///
        /// <para>The unit's space reaches the move through the selection's own published coordinate,
        /// because p.18's operations name a piece by the space it stands on and the card's sentence
        /// names a unit. Milestone 8.20 is what publishes it.</para>
        /// </summary>
        private static EffectDefinition CreateBeforeAttackMove(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("noble-may");
            var sequence = new EffectNodeId("noble-sequence");
            var select = new EffectNodeId("noble-other");
            var move = new EffectNodeId("noble-move");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { sequence },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, move }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.OriginParameter,
                                EffectValue.Integer(
                                    (int)RosterQueryOrigin.EnclosingTargetBeforeConsequences)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),
                            // "unit", which p.19 defines as a hero or a minion and separates from a
                            // token by name. Noble Blade admits either kind of unit and no token.
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                EffectValue.Integer((int)RosterUnitKind.Unit)),
                            // The card names no side, so neither does this: Arien may move a unit of
                            // either team out of the way, and a declaration naming one would be a
                            // narrower card than the printed one.
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.RelationParameter,
                                EffectValue.Integer((int)SourceTeamRelation.AnyTeam)),
                            // Card-text direct Move reads its enclosing action's activity (p.18, and
                            // milestone 6.5b's reading), which here is the Attack this text is printed
                            // on, so the offer asks immunity the question the Attack itself asked.
                            EffectInputBinding.FromLiteral(
                                "activity", EffectValue.Integer((int)RuleActivity.Attack)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.ExcludeTargetParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            // All three, because the selection publishes whichever kind of unit was
                            // taken and a node must declare every fact its handler may record - a
                            // recorded outcome the node did not declare is a snapshot this build
                            // refuses to read back. Only the space is consumed; the two identities are
                            // declared because they are what actually gets published.
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", select, SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                EffectValue.Integer((int)DirectMoveDestinationPolicy.Unrestricted))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Before the attack: Up to 1 enemy hero in any of the N spaces in a straight line directly
        /// behind the target discards a card, or is defeated." - Dangerous Current's sentence over two
        /// spaces, Raging Stream's over three, Violent Torrent's over five and with a repeat after it.
        ///
        /// <para><b>Where "behind" is measured from.</b> Ledger question A-02, approved by the user on
        /// 2026-08-23: p.19 defines Straight Line and the rulebook never defines "behind", so the
        /// reading is taken from p.18's Push - "directly away from the pushing unit". The file runs from
        /// Arien through the target and is counted outward from the target, which
        /// <c>behind-target-line@1</c> implements. Three consequences of that approval survive here and
        /// are worth naming: the file is fixed by the attacker/target pair so <b>no direction question
        /// is ever asked</b>; "Up to 1 enemy hero in any of the N spaces" is a selection among the heroes
        /// standing there rather than a choice of where to look; and an obstacle space is still one of
        /// the N, because p.18's Nearest counts obstacle spaces and p.9 has no line of sight.</para>
        ///
        /// <para><b>"Up to 1" and "discards a card, or is defeated" are two different kinds of
        /// optional.</b> The first is over the hero: naming nobody is a legal, complete outcome, and the
        /// selection publishes that it named nobody so the branch below can tell it from a question
        /// nobody asked. The second is not optional at all - ledger question 13 reads it as one
        /// mandatory step with two ways to complete, exactly as Wasp's Electroblast prints it - so the
        /// discard is mandatory and the Defeat is the arm it reaches when the chosen hero holds
        /// nothing.</para>
        ///
        /// <para><b>Violent Torrent's repeat.</b> "May repeat once on a different unit" - the face's
        /// noun differs from Wasp's Thunder Boomerang, which prints "different target", and each
        /// transcription keeps its own. What the declaration excludes is the same either way: the unit
        /// this instance's Attack took, which <see cref="RepeatTargetBinding.DifferentTarget"/> names.
        /// The repeated instance is an ordinary Attack that applies this same text again, where the
        /// repeat node is no longer eligible because one is all the card allows.</para>
        /// </summary>
        private static EffectDefinition CreateCurrent(
            CardDefinitionId cardId, string contentRevision, int length, bool repeats)
        {
            var root = new EffectNodeId("current-root");
            var line = new EffectNodeId("current-line");
            var branch = new EffectNodeId("current-branch");
            var price = new EffectNodeId("current-price");
            var discard = new EffectNodeId("current-discard");
            var escalation = new EffectNodeId("current-escalation");
            var defeat = new EffectNodeId("current-defeat");
            var repeat = new EffectNodeId("current-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeats
                        ? new[] { line, branch, repeat }
                        : new[] { line, branch }),
                new EffectNodeDefinition(
                    line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.BehindTargetLineV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("length", EffectValue.Integer(length))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.NamedAHeroOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    branch, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { price },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", line, SyntheticRosterCapabilities.NamedAHeroOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    price, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { discard, escalation }),
                new EffectNodeDefinition(
                    // No `if able` here, because the face prints none: the step always applies and
                    // reports which of its two ways it completed.
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, line,
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
                    }),
                new EffectNodeDefinition(
                    // The second arm, reached only where the first could not complete. It is a branch
                    // and not a question: p.10 gives nobody a decision here.
                    escalation, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", discard, SyntheticCardStateCapabilities.UnableToDiscardOutput,
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
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter, line,
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                    })
            };

            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    // p.10 gives an unassigned decision to the card's controller, and "may repeat"
                    // assigns none - so the hero performing the Attack is the one asked.
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentTarget));
            }

            return new EffectDefinition(
                AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "After the attack: You may push an enemy unit adjacent to you up to N spaces." - Rogue
        /// Wave's sentence over two spaces and Tidal Blast's over three.
        ///
        /// <para><b>One node, because the sentence asks one question with three declared bounds.</b>
        /// p.18's Push already takes the objects standing beside the pusher; what the card adds is a
        /// side ("enemy"), a count ("an", which is one), and a distance range ("up to N"). All three are
        /// declarations milestone 8.8 put on the batch push, so nothing here selects a target
        /// separately - and the `up to` policy is what makes pushing nobody a legal result rather than
        /// a step that failed, which is the whole of the printed "may".</para>
        ///
        /// <para>The distance's floor is zero because the card writes "up to"; Wasp's kinetic Skills
        /// write theirs as a number and declare a floor equal to it. Neither card says "ignoring
        /// obstacles", and p.18 makes that a property of the pushing effect rather than a default, so it
        /// is declared false.</para>
        /// </summary>
        private static EffectDefinition CreateWave(
            CardDefinitionId cardId, string contentRevision, int distance)
        {
            var root = new EffectNodeId("wave-push");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.ObjectPushV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(distance)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromLiteral(
                                "ignoresObstacles", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer((int)PushEligiblePieceKind.Unit)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                                EffectValue.Integer((int)SourceTeamRelation.OpposingSource)),
                            // "an enemy unit" - one, which is what makes which unit a decision.
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
        /// "Place yourself into a space in range without a spawn point[, and not adjacent to an empty
        /// spawn point]." - Liquid Leap's and Magical Current's sentence, and Stranger Tide's with the
        /// bracketed clause dropped.
        ///
        /// <para><b>Two nodes and no decision between them.</b> "yourself" names the piece, so the
        /// subject reaches the Place through the actor-space bridge rather than through a selection -
        /// milestone 6.5a's reason exactly: a one-candidate prompt would invent a choice the card does
        /// not offer. The one decision the sentence does contain is which space, and the Place opens
        /// it.</para>
        ///
        /// <para><b>"in range" is the printed icon and not a number.</b> p.9: "the value on the
        /// Range/Radius icon determines the maximum Range/Radius of your action. If the card text
        /// mentions Range/Radius, it refers to this value" - so the reach is read off the card with
        /// p.16's items composed on, which is what makes the printed-Range Place a second capability
        /// rather than the written-radius one with a number in it.</para>
        ///
        /// <para>The Place is mandatory: the face prints no "may". A hero with no legal destination
        /// therefore stops the Skill at that step, which is p.10's own answer for an impossible
        /// mandatory step and leaves everything before it standing.</para>
        /// </summary>
        private static EffectDefinition CreateLeap(
            CardDefinitionId cardId, string contentRevision, PlaceSpawnPointExclusion exclusion)
        {
            var root = new EffectNodeId("leap-root");
            var actorSpace = new EffectNodeId("leap-actor-space");
            var place = new EffectNodeId("leap-place");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRangeV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            // The subject is already fixed to the acting hero, so this declaration
                            // narrows nothing - and p.18's own unrestricted breadth is how a Place
                            // that narrows nothing says so.
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                EffectValue.Integer((int)PlaceEligiblePieceKind.GamePiece)),
                            // "in range" is measured from the hero whose card it is, which p.9 makes
                            // the anchor of every printed icon.
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                EffectValue.Integer((int)PlaceDestinationAnchor.ActingHero)),
                            EffectInputBinding.FromLiteral(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                EffectValue.Integer((int)exclusion))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "Swap with an enemy minion in range." - Arcane Whirlpool's whole sentence, and Ebb and
        /// Flow's with "; if it was adjacent to you, may repeat once" after it.
        ///
        /// <para><b>The Swap is p.19's and nothing here re-implements it.</b> "Swap places with the
        /// target. This is not movement." is the face's own reminder text, and that is exactly what the
        /// exchange capability does - which is why the Swap emits no movement fact and why nothing about
        /// Arien's position is asked before or after it.</para>
        ///
        /// <para><b>Ebb and Flow's condition, and why it can be asked after the Swap.</b> The face
        /// prints it in the past tense - "if it <em>was</em> adjacent to you" - which is what makes it
        /// readable at all: by the time the condition is asked, the Swap has already moved the minion
        /// into Arien's former space. p.19's Swap exchanges exactly two spaces and the capability
        /// publishes both, so asking whether those two are adjacent is asking where the two pieces were
        /// standing before the exchange. Nothing is captured beforehand and nothing is re-derived from
        /// the board.</para>
        ///
        /// <para>The repeat is unbound: "may repeat once" with nothing after it, so p.10's default
        /// stands and the repeated instance chooses its own minion freely. The absence of a binding is
        /// content, and the definition states it by declaring none.</para>
        /// </summary>
        private static EffectDefinition CreateWhirlpool(
            CardDefinitionId cardId, string contentRevision, bool repeatsWhenItWasAdjacent)
        {
            var root = new EffectNodeId("whirlpool-root");
            var select = new EffectNodeId("whirlpool-target");
            var swap = new EffectNodeId("whirlpool-swap");
            var wasAdjacent = new EffectNodeId("flow-was-adjacent");
            var branch = new EffectNodeId("flow-branch");
            var repeat = new EffectNodeId("flow-repeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: repeatsWhenItWasAdjacent
                        ? new[] { select, swap, wasAdjacent, branch }
                        : new[] { select, swap }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeMinionTargetV1,
                    inputs: new[]
                    {
                        // A Skill's effect is the action (p.12), so the immunity question this
                        // selection asks is the Skill's own.
                        EffectInputBinding.FromLiteral(
                            "activity", EffectValue.Integer((int)RuleActivity.Skill)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            EffectValue.Integer((int)SourceTeamRelation.OpposingSource))
                    },
                    outputs: new[]
                    {
                        // Both, for the reason Noble Blade's selection declares three: the handler
                        // publishes the minion it was given whether or not this card consumes it, and
                        // an undeclared recorded outcome is a snapshot this build refuses to read back.
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
                            "subject", select,
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate),
                        // "an enemy minion", which p.19 makes a unit.
                        EffectInputBinding.FromLiteral(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            EffectValue.Integer((int)SwapEligiblePieceKind.Unit))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                        new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                    })
            };

            if (repeatsWhenItWasAdjacent)
            {
                nodes.Add(new EffectNodeDefinition(
                    wasAdjacent, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.SpacesAdjacentV1,
                    inputs: new[]
                    {
                        // The two spaces the Swap traded, which are where the hero and the minion were
                        // standing when the sentence's "was" refers to.
                        EffectInputBinding.FromNodeOutput(
                            "first", swap, "actorAt", EffectValueKind.Coordinate),
                        EffectInputBinding.FromNodeOutput(
                            "second", swap, "subjectAt", EffectValueKind.Coordinate)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("adjacent", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    branch, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { repeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", wasAdjacent, "adjacent", EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1));
            }

            return new EffectDefinition(
                SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One node: the capability that creates this card's declared Active effect.
        ///
        /// Four of Arien's five persistent texts compose to this shape and none of them needs more.
        /// Everything each sentence says is definition content on the Active effect itself - the scope,
        /// the disk, the restricted activities, the carve-outs - so an effect tree with branches in it
        /// would be describing the same rule in a second vocabulary. The node is
        /// <see cref="EffectFailurePolicy.Mandatory"/> because the printed text is not optional and
        /// creating the effect cannot fail, and it asks no question - which is what lets Spell Break's
        /// copy run inside a Skill, the Duelists' inside p.14's step 4, and the two footings before a
        /// Movement.
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
        /// "yourself" refers to. Written once because three of Arien's compositions need it.
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

        private static EffectDefinitionId DefenseTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-defense-text");

        private static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-movement-text");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ursafar the Savage's printed card text, composed - milestone 8.35z31.
    ///
    /// <para><b>One shape runs through the whole set.</b> Eighteen of his nineteen faces open with
    /// <c>If enraged</c> and thirteen close with <c>This round: You are enraged.</c>, so almost every
    /// composition here is the same three nodes: a Conditional that asks
    /// <c>hero-condition@1</c> whether he already is, the clause that condition governs, and - on every
    /// card but the five Green ones and the Ultimate - the node that grants the state for next
    /// time.</para>
    ///
    /// <para><b>The condition is a condition, not a step.</b> The user's general ruling of 2026-09-06 is
    /// that an "If X, do Y" clause whose condition is false simply skips and the card continues -
    /// p.10's stop-and-skip applies to steps you attempt and cannot complete, never to conditions that
    /// are merely false. A <see cref="EffectNodeKind.Conditional"/> with a then-child and no else-child
    /// is exactly that, which is why Angry Roar played calm still grants enrage and why nothing in this
    /// file guards a grant behind a condition.</para>
    ///
    /// <para><b>What a mandatory step failing does instead, and it is the designer's own answer.</b>
    /// The Ursafar thread records that "the attack is mandatory - the cards will fizzle before you
    /// enrage if there's no target". That reading is why both Gold cards put their grant in the
    /// <em>after</em>-the-attack root rather than beside their step-1 clause: if the mandatory targeting
    /// step cannot complete, p.14 never reaches step 7 and the grant never lands. The rule is the
    /// engine's own, not a special case written here.</para>
    ///
    /// <para><b>The former shortfall, now composed - milestone 8.35z50.</b> Three Red faces
    /// print "and the target was not removed", which phase 1 of this hero left uncomposed because
    /// nothing anywhere derived whether an Attack's target survived and a capability handler cannot
    /// reach the board to find out. It can ask now:
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.TargetWasRemoved"/> is
    /// read live off board occupancy at the exact moment "After the attack:" text runs - the same
    /// answer rulebook p.14 step 6 reached, re-derived rather than stored, so it costs no snapshot
    /// bytes and moves no schema. <see cref="RemovalGatedOnTargetSurvival"/> names the three faces this
    /// unlocked.</para>
    /// </summary>
    public static class UrsafarEffectContent
    {
        /// <summary>p.16's permanent ability, identified for the continuous source it becomes.</summary>
        public const string UnboundFuryRuleId = "ursafar-unbound-fury";

        /// <summary>
        /// The rule kind every reader of Unbound Fury matches on - the enraged condition's standing
        /// source, and the widening that makes his resolved cards count as active. It is spelled here
        /// once and asserted equal to both readings' own constants by this milestone's tests.
        /// </summary>
        public const string UnboundFuryRuleKind = "UrsafarUnboundFury";

        /// <summary>
        /// The three faces whose "and the target was not removed" clause reads whether their own
        /// Attack's target survived, stated as content so a reader of the definitions finds it without
        /// reading a note.
        ///
        /// <para><b>What phase 1 found missing, and what closes it - milestone
        /// 8.35z50.</b> Rulebook p.14 step 6 removes the defender if the attack succeeded,
        /// and step 7 is where these cards ask about it. <c>attack-took-a-hero@1</c> answers a
        /// different question - whether the target was a hero at all - and nothing else in the engine
        /// recorded the removal itself. The fix is three pieces rather than the four phase 1 costed,
        /// because the design changed shape under verification: (1)
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.EnclosingAttackFacts.TargetWasRemoved"/>
        /// - a runtime struct field no card definition reaches, so it is outside the content-identity
        /// oracle. (2) <c>CardActionResolutionAuthority.EnclosingAttackOf</c> computes it live, from
        /// board occupancy, rather than reading it off a stored field - a hero no longer occupying any
        /// space, or a minion no longer standing on the board, is exactly what step 6 just decided, and
        /// nothing else touches the board between step 6 and step 7. That is what phase 1's original
        /// plan - persisting the fact on <c>PendingAttackResolution</c> and re-deriving it from there -
        /// would have cost that this does not: a new serialized field is a schema-version move, and a
        /// live board read is not. (3) <c>attack-target-survived@1</c>
        /// (<see cref="GuardsDigital.Core.Effects.Synthetic.SyntheticAttackTargetSurvivalCapabilities"/>),
        /// a predicate over it, gating the removal exactly as <c>is-flagged@1</c> gates every other
        /// branch here.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> RemovalGatedOnTargetSurvival { get; } =
            Array.AsReadOnly(new[]
            {
                UrsafarCardContent.PreyDrive,
                UrsafarCardContent.PreyAbundance,
                UrsafarCardContent.FeedingFrenzy
            });

        // -----------------------------------------------------------------------------------------
        // Attack text. Two roots are available on an Attack - rulebook p.14's step 1 and step 7 - and
        // three of his faces use both.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(UrsafarCardContent.ClawsThatCatch))
            {
                return CreateProwl(cardId, contentRevision);
            }

            if (cardId.Equals(UrsafarCardContent.Tear))
            {
                return CreateTearDefeatCost(cardId, contentRevision);
            }

            if (cardId.Equals(UrsafarCardContent.Rip))
            {
                return CreateAfterAttackRider(cardId, contentRevision, coins: 1);
            }

            if (cardId.Equals(UrsafarCardContent.PreyDrive) ||
                cardId.Equals(UrsafarCardContent.PreyAbundance))
            {
                return CreateRemovalRider(cardId, contentRevision, maximumRemoved: 1);
            }

            if (cardId.Equals(UrsafarCardContent.FeedingFrenzy))
            {
                return CreateRemovalRider(cardId, contentRevision, maximumRemoved: 2);
            }

            return null;
        }

        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(UrsafarCardContent.ClawsThatCatch) ||
                cardId.Equals(UrsafarCardContent.Tear))
            {
                return CardTextTiming.BeforeTheAttack;
            }

            return AttackTextFor(cardId, UrsafarCardContent.ContentRevision) == null
                ? CardTextTiming.None
                : CardTextTiming.AfterTheAttack;
        }

        public static EffectDefinition SecondAttackTextFor(
            CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(UrsafarCardContent.ClawsThatCatch))
            {
                return CreateGrantOnly(cardId, contentRevision, SecondAttackTextEffectId(cardId));
            }

            if (cardId.Equals(UrsafarCardContent.Tear))
            {
                return CreateAfterAttackRider(
                    cardId, contentRevision, coins: 2, effectId: SecondAttackTextEffectId(cardId));
            }

            return null;
        }

        public static CardTextTiming SecondAttackTextTimingFor(CardDefinitionId cardId) =>
            SecondAttackTextFor(cardId, UrsafarCardContent.ContentRevision) == null
                ? CardTextTiming.None
                : CardTextTiming.AfterTheAttack;

        // -----------------------------------------------------------------------------------------
        // Skill text. His Silver borrows an action; three Greens force a discard at Range; two Greens
        // print p.10's modal over a borrow and a retrieval.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(UrsafarCardContent.AngryRoar))
            {
                return CreateRoar(cardId, contentRevision);
            }

            if (cardId.Equals(UrsafarCardContent.SniffOut) ||
                cardId.Equals(UrsafarCardContent.EyesOnThePrey))
            {
                return CreateRangeDiscard(cardId, contentRevision, escalatesToDefeat: false);
            }

            if (cardId.Equals(UrsafarCardContent.ApexPredator))
            {
                return CreateRangeDiscard(cardId, contentRevision, escalatesToDefeat: true);
            }

            if (cardId.Equals(UrsafarCardContent.InstinctiveReaction))
            {
                return CreateModalBorrow(cardId, contentRevision, mayTakeBoth: false);
            }

            if (cardId.Equals(UrsafarCardContent.EvolutionaryResponse))
            {
                return CreateModalBorrow(cardId, contentRevision, mayTakeBoth: true);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // Movement text. Three faces swap after the walk; two raise the walk's own allowance before it.
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition MovementTextFor(
            CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(UrsafarCardContent.ProwlingBrute))
            {
                return CreateSwapRider(cardId, contentRevision, additionalSpaces: 0);
            }

            if (cardId.Equals(UrsafarCardContent.RampagingBeast))
            {
                return CreateSwapRider(cardId, contentRevision, additionalSpaces: 1);
            }

            if (cardId.Equals(UrsafarCardContent.UnstoppableForce))
            {
                return CreateSwapRider(cardId, contentRevision, additionalSpaces: 2);
            }

            if (cardId.Equals(UrsafarCardContent.ColdIre) ||
                cardId.Equals(UrsafarCardContent.EyesOfFlame))
            {
                return CreateMovementRaise(cardId, contentRevision);
            }

            return null;
        }

        /// <summary>
        /// Which side of the walk a Blue face's text belongs to, decided by the face's own words.
        ///
        /// <para>p.11 states the default and allows the exception in one sentence: "Unless it says
        /// otherwise, the card text comes into effect before movement." The three swap faces say
        /// otherwise - "after movement" - and the two raising faces say nothing, which is the page
        /// answering for them. A raise that ran after the walk would raise an allowance already
        /// spent.</para>
        /// </summary>
        public static CardTextTiming MovementTextTimingFor(CardDefinitionId cardId)
        {
            if (cardId.Equals(UrsafarCardContent.ProwlingBrute) ||
                cardId.Equals(UrsafarCardContent.RampagingBeast) ||
                cardId.Equals(UrsafarCardContent.UnstoppableForce))
            {
                return CardTextTiming.AfterTheMovement;
            }

            if (cardId.Equals(UrsafarCardContent.ColdIre) ||
                cardId.Equals(UrsafarCardContent.EyesOfFlame))
            {
                return CardTextTiming.BeforeTheMovement;
            }

            return CardTextTiming.None;
        }

        // -----------------------------------------------------------------------------------------
        // The compositions.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Claws That Catch's step-1 text: "If enraged, you may move 1 space to a space adjacent to an
        /// enemy hero; target a unit adjacent to you."
        ///
        /// <para><b>Two clauses, and the semicolon is what separates them.</b> The user's 2026-09-06
        /// ruling on a semicolon in card text is that it joins independent clauses, so only the move is
        /// conditional on being enraged; the targeting is unconditional and happens whether or not he
        /// is. That is also the only reading under which the card works at all - a Gold card that could
        /// not attack until he was already enraged would have nothing to open a round with.</para>
        ///
        /// <para><b>Move first, then target</b>, because that is the printed order, and the 2026-09-06
        /// attack-order ruling makes printed reading order govern. It is Brogan's charge shape reused
        /// rather than re-invented: <c>choose-charge-line@1</c> names a destination the sentence's own
        /// constraint admits, the direct Move goes there, and the target question is asked afterwards
        /// from where he now stands. The move is card text inside an Attack and therefore not a Movement
        /// action, so movement-action restrictions do not reach it - the 2026-09-06 ruling on exactly
        /// this, and the reason the displacement is a direct Move rather than an action.</para>
        /// </summary>
        private static EffectDefinition CreateProwl(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("prowl-root");
            var gate = new EffectNodeId("prowl-gate");
            var mayMove = new EffectNodeId("prowl-may-move");
            var moveSequence = new EffectNodeId("prowl-move-sequence");
            var actorSpace = new EffectNodeId("prowl-actor-space");
            var destination = new EffectNodeId("prowl-destination");
            var move = new EffectNodeId("prowl-move");
            var victim = new EffectNodeId("prowl-target");
            var retarget = new EffectNodeId("prowl-retarget");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, victim, retarget }),
                    EnragedGate(gate, mayMove),
                    new EffectNodeDefinition(
                        mayMove, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { moveSequence },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        moveSequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, destination, move }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        destination, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseChargeLineV1,
                        inputs: new[]
                        {
                            // "1 space" - exactly one, so the two bounds are equal. Brogan's charges
                            // print a range and pass different numbers here.
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(1)),
                            // "adjacent to an enemy HERO" - his Gold cards narrow where Brogan's
                            // charges say "unit".
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero)
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
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                destination, ChargeDestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    AdjacentEnemyUnitTarget(victim),
                    BindAttackTarget(retarget, victim)
                });
        }

        /// <summary>
        /// Angry Roar: "If enraged, perform the primary action on one of your active cards with an
        /// active effect. This round: You are enraged."
        ///
        /// <para><b>Composed as our scan prints it.</b> The designer channel records a reply describing
        /// this card as starting with "you may", and a later printing carrying a replacement-action help
        /// line. Our extraction has neither, and the pack-wide source ruling makes the extraction sole
        /// authority; both are consolidated printing questions for the designer rather than code. The
        /// outcome is the same for the case that motivated the question either way, because the general
        /// conditional ruling already lets a false <c>If enraged</c> skip its clause while the grant
        /// still lands.</para>
        ///
        /// <para>The borrow step carries no optionality of its own, which is the printed reading: no
        /// "you may", no "up to", no "if able", so p.10 makes it mandatory, and an empty pool reports
        /// that it cannot happen rather than quietly doing nothing.</para>
        /// </summary>
        private static EffectDefinition CreateRoar(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("roar-root");
            var gate = new EffectNodeId("roar-gate");
            var borrow = new EffectNodeId("roar-borrow");
            var grant = new EffectNodeId("roar-grant");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, grant }),
                    EnragedGate(gate, borrow),
                    BorrowNode(
                        borrow,
                        BorrowedActionPool.OwnedActiveCardsWithAnActiveEffect,
                        CardColor.None),
                    GrantNode(grant, cardId)
                });
        }

        /// <summary>
        /// Instinctive Reaction's "choose one" and Evolutionary Response's "choose one, or both", over
        /// the same two bullets: perform the primary action on one of your discarded cards, and you may
        /// retrieve a discarded card.
        ///
        /// <para><b>The "or both" needs no new vocabulary, and that is a measured finding rather than an
        /// assumption.</b> A <see cref="EffectNodeKind.Choose"/> node already carries a minimum and a
        /// maximum, and nothing in the node's own shape rules or in the definition validator requires
        /// them to be equal - the only bounds are that the maximum is at least the minimum and at most
        /// the number of options. The resolver already runs every selected branch in definition order.
        /// So "choose one" is 1..1 and "choose one, or both" is 1..2, and this is the roster's first
        /// face to use the second, which p.10's Modal Actions licenses outright.</para>
        ///
        /// <para>The retrieval bullet keeps its own "you may" on an Optional above it, because the
        /// bullet is optional even once it has been chosen - which is what the printed words say, and
        /// what separates it from the borrow bullet beside it.</para>
        /// </summary>
        private static EffectDefinition CreateModalBorrow(
            CardDefinitionId cardId, string contentRevision, bool mayTakeBoth)
        {
            var root = new EffectNodeId("modal-root");
            var gate = new EffectNodeId("modal-gate");
            var choose = new EffectNodeId("modal-choose");
            var borrow = new EffectNodeId("modal-borrow");
            var mayRetrieve = new EffectNodeId("modal-may-retrieve");
            var retrieve = new EffectNodeId("modal-retrieve");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate }),
                    EnragedGate(gate, choose),
                    new EffectNodeDefinition(
                        choose, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrow, mayRetrieve },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: mayTakeBoth ? 2 : 1),
                    BorrowNode(borrow, BorrowedActionPool.OwnedDiscardedCards, CardColor.None),
                    new EffectNodeDefinition(
                        mayRetrieve, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { retrieve },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.RetrieveCardV1,
                        inputs: new[]
                        {
                            // p.10 makes both the owner and the chooser the acting hero: "you can never
                            // retrieve another hero's cards", and the card says "a discarded card" with
                            // no other subject.
                            EffectInputBinding.FromRole(
                                "owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromRole(
                                "chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// Sniff Out and Eyes on the Prey - "If enraged, an enemy hero in range discards a card, if
        /// able" - and Apex Predator, which escalates the same sentence to "or is defeated".
        ///
        /// <para>It is Wasp's radius rider read at Range instead, and the Tier contrast is the same one
        /// her Red line prints: the two lower faces make the discard <c>if able</c>, so a hero holding
        /// nothing is skipped silently, while the Tier III face makes it one mandatory step with two
        /// ways to complete - ledger question 13's construction, on its fifth verified printing.</para>
        /// </summary>
        private static EffectDefinition CreateRangeDiscard(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var root = new EffectNodeId("scent-root");
            var gate = new EffectNodeId("scent-gate");
            var sequence = new EffectNodeId("scent-sequence");
            var select = new EffectNodeId("scent-target");
            var discard = new EffectNodeId("scent-discard");
            var escalation = new EffectNodeId("scent-escalation");
            var defeat = new EffectNodeId("scent-defeat");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { gate }),
                EnragedGate(gate, sequence),
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: escalatesToDefeat
                        ? new[] { select, discard, escalation }
                        : new[] { select, discard }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1,
                    inputs: new[]
                    {
                        // A Skill's own activity, which is what p.18 asks immunity about here: this
                        // text is the Skill rather than a rider inside an Attack.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),
                        // Wasp's Deflect excludes the hero attacking her, because her text is a
                        // Defense rider inside somebody else's Attack. These three are his own Skill
                        // and there is no attacker to except, so the exclusion is declared off rather
                        // than left to a default - the capability requires it stated either way.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                            EffectValue.Boolean(false))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    })
            };

            if (escalatesToDefeat)
            {
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, select,
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
                    }));
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
            else
            {
                nodes.Add(new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter, select,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }));
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// The three Blue swap faces: "If enraged, after movement, you may swap with a unit or a token
        /// adjacent to you[; if you do, move up to N additional space(s)]."
        ///
        /// <para><b>"if you do" needs no condition of its own.</b> The whole rider hangs on one
        /// Optional the actor answers, so a declined swap runs nothing after it - which is what "if you
        /// do" says, expressed by the shape rather than by a second question. And p.19 is explicit that
        /// a swap "does not count as movement or placement", so the additional spaces are a distinct
        /// displacement from wherever the swap left him rather than a continuation of it.</para>
        ///
        /// <para>The selection admits "a unit or a token", which is p.19's whole board-object breadth,
        /// so the swap declares <see cref="SwapEligiblePieceKind.GamePiece"/> rather than the narrower
        /// unit reading Sabina's swap uses. It names the swap as its consumer, which is what keeps an
        /// object the swap could not move out of the offer instead of being offered and then refused.
        /// </para>
        /// </summary>
        private static EffectDefinition CreateSwapRider(
            CardDefinitionId cardId, string contentRevision, int additionalSpaces)
        {
            var root = new EffectNodeId("swap-root");
            var gate = new EffectNodeId("swap-gate");
            var maySwap = new EffectNodeId("swap-may");
            var sequence = new EffectNodeId("swap-sequence");
            var select = new EffectNodeId("swap-target");
            var swap = new EffectNodeId("swap-exchange");
            var extraMove = new EffectNodeId("swap-additional-move");
            var grant = new EffectNodeId("swap-grant");

            var swapChildren = additionalSpaces > 0
                ? new[] { select, swap, extraMove }
                : new[] { select, swap };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { gate, grant }),
                EnragedGate(gate, maySwap),
                new EffectNodeDefinition(
                    maySwap, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { sequence },
                    chooser: EffectChooserRole.Actor),
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: swapChildren),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(swap))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("chosen", EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", select, "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            SwapEligiblePieceKind.GamePiece)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                        new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                    }),
                GrantNode(grant, cardId)
            };

            if (additionalSpaces > 0)
            {
                nodes.Add(new EffectNodeDefinition(
                    extraMove, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        // From where the swap left him, which is the space the swap published rather
                        // than the one he started the walk on.
                        EffectInputBinding.FromNodeOutput(
                            "mover", swap, "actorAt", EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral(
                            "distance", EffectValue.Integer(additionalSpaces)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                    }));
            }

            return new EffectDefinition(
                MovementTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// Cold Ire's "If enraged, gain +1 [Movement] Movement;" and Eyes of Flame's "+2".
        ///
        /// <para>These are the two faces printing <c>1+</c> on the Movement icon. The user's ruling of
        /// 2026-09-06 is that the <c>+</c> is p.13/p.14's printed cue extended to a Movement value: the
        /// printed value <b>is</b> 1, and it is this text that raises it. So nothing here reads the
        /// glyph - the raise is an ordinary value contribution the allowance query already composes,
        /// and the icon stays a plain 1 in the definition.</para>
        ///
        /// <para>It runs before the walk because p.11's default puts it there and because a raise
        /// applied afterwards would raise an allowance already spent. That the allowance is composed
        /// before p.11's action-start restriction judges it is the movement query's own documented
        /// order, so a raised total is judged as raised.</para>
        /// </summary>
        private static EffectDefinition CreateMovementRaise(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("raise-root");
            var gate = new EffectNodeId("raise-gate");
            var raise = new EffectNodeId("raise-allowance");
            var grant = new EffectNodeId("raise-grant");

            return new EffectDefinition(
                MovementTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, grant }),
                    EnragedGate(gate, raise),
                    new EffectNodeDefinition(
                        raise, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        UrsafarActiveEffectContent.MovementRaiseFor(cardId),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        }),
                    GrantNode(grant, cardId)
                });
        }

        /// <summary>
        /// Tear's step-1 declaration: the extra Life counter its step-7 sentence charges.
        ///
        /// <para><b>Why the declaration is made before the Attack when the sentence is printed after
        /// it.</b> The user's ruling is that the cost is computed once as Tier + 1 and spent once, at
        /// the defeat - the "After the attack:" placement says when the condition is checked, not when
        /// the counter is paid. p.14 pays at step 6, so a declaration made at step 7 would arrive after
        /// the money had changed hands and could only ever be a second spend, which is the reading the
        /// ruling rejects. Declaring it at step 1 puts one number in front of the one payment.</para>
        ///
        /// <para><b>Checking "if enraged" here rather than there changes no outcome, and that is
        /// provable rather than assumed.</b> Nothing can make Ursafar enraged between step 1 and step 7
        /// of his own Attack: his own grant is dormant until the action ends, and no other card of his
        /// grants the state during an enemy's turn. So the two moments give the same answer, and this
        /// milestone's tests pin that rather than leaving it as a claim.</para>
        /// </summary>
        private static EffectDefinition CreateTearDefeatCost(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("tear-root");
            var gate = new EffectNodeId("tear-gate");
            var declare = new EffectNodeId("tear-declare");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate }),
                    EnragedGate(gate, declare),
                    new EffectNodeDefinition(
                        declare, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        UrsafarActiveEffectContent.AdditionalDefeatLifeCostV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// Rip's "If enraged, gain 1 coin." and Tear's "If enraged, gain 2 coins" - a step-7 rider whose
        /// whole content is a coin grant, followed by the card's own enrage grant.
        /// </summary>
        private static EffectDefinition CreateAfterAttackRider(
            CardDefinitionId cardId,
            string contentRevision,
            int coins,
            EffectDefinitionId? effectId = null)
        {
            var root = new EffectNodeId("rider-root");
            var gate = new EffectNodeId("rider-gate");
            var grantCoins = new EffectNodeId("rider-coins");
            var grant = new EffectNodeId("rider-grant");

            return new EffectDefinition(
                effectId ?? AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, grant }),
                    EnragedGate(gate, grantCoins),
                    new EffectNodeDefinition(
                        grantCoins, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.GainResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        }),
                    GrantNode(grant, cardId)
                });
        }

        /// <summary>
        /// Prey Drive's and Prey Abundance's "After the attack: If enraged, and the target was not
        /// removed, remove up to 1 enemy minion in radius." and Feeding Frenzy's "up to 2" - milestone
        /// 8.35z50, closing the shortfall <see cref="RemovalGatedOnTargetSurvival"/>
        /// documents.
        ///
        /// <para><b>A compound condition, nested rather than combined.</b> The clause reads "if enraged,
        /// AND the target was not removed" - two conjuncts, and this codebase's <c>Conditional</c> node
        /// takes one flag. Nesting one inside the other's then-child is the same AND a single node with
        /// two inputs would give: the outer gate is <see cref="EnragedGate"/> unchanged, and its child
        /// is a second gate over <c>attack-target-survived@1</c>, published as "survived" so the
        /// content never has to invert "not removed" itself.</para>
        ///
        /// <para><b>Reach is the card's own printed Radius</b> - <c>synthetic-card-radius@1</c> - not a
        /// literal the way Sabina's "adjacent to you" is. Everything past that is her
        /// <c>CreateFollowUpRemoval</c> shape verbatim: <c>choose-target-set@1</c> with
        /// <c>minimum</c>/<c>maximum</c> publishing up to two spaces, then one <c>synthetic-remove@2</c>
        /// per published space, each <c>if able</c> so naming fewer than the card allows is the answer
        /// given rather than a failed step. The grant stays the card's own unconditional sibling, as
        /// every other face here has it - Ruling 2's build-up is a property of where the grant sits,
        /// not of this clause.</para>
        /// </summary>
        private static EffectDefinition CreateRemovalRider(
            CardDefinitionId cardId, string contentRevision, int maximumRemoved)
        {
            var root = new EffectNodeId("removal-root");
            var gate = new EffectNodeId("removal-gate");
            var survivedCheck = new EffectNodeId("removal-survived-check");
            var survivedFact = new EffectNodeId("removal-survived-fact");
            var survivedGate = new EffectNodeId("removal-survived-gate");
            var sequence = new EffectNodeId("removal-sequence");
            var radius = new EffectNodeId("removal-radius");
            var select = new EffectNodeId("removal-targets");
            var removeFirst = new EffectNodeId("removal-remove");
            var removeSecond = new EffectNodeId("removal-remove-second");
            var grant = new EffectNodeId("removal-grant");

            var removals = maximumRemoved > 1
                ? new[] { radius, select, removeFirst, removeSecond }
                : new[] { radius, select, removeFirst };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { gate, grant }),
                EnragedGate(gate, survivedCheck),
                new EffectNodeDefinition(
                    survivedCheck, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { survivedFact, survivedGate }),
                new EffectNodeDefinition(
                    survivedFact, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticAttackTargetSurvivalCapabilities.AttackTargetSurvivedV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                            EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    survivedGate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { sequence },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", survivedFact,
                            SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: removals),
                new EffectNodeDefinition(
                    radius, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticValueCapabilities.CardRadiusV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer)
                    }),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticRosterCapabilities.ChooseTargetSetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticRosterCapabilities.DistanceParameter, radius,
                            SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        // A card-text rider applied inside an Attack: p.18 asks immunity about this
                        // action's own activity, exactly as Sabina's equivalent selection does.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                        EffectInputBinding.FromLiteral("minimum", EffectValue.Integer(0)),
                        EffectInputBinding.FromLiteral("maximum", EffectValue.Integer(maximumRemoved))
                    },
                    outputs: maximumRemoved > 1
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
                        }),
                RemovalNode(removeFirst, select, "firstSpace"),
                GrantNode(grant, cardId)
            };

            if (maximumRemoved > 1)
            {
                nodes.Add(RemovalNode(removeSecond, select, "secondSpace"));
            }

            return new EffectDefinition(AttackTextEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One of the removals the selection above named, by the space it published - Sabina's
        /// <c>RemoveNode</c> shape verbatim, over "enemy minion" rather than her "friendly minion".
        /// <c>if able</c> rather than mandatory: a chooser who named fewer minions than the card allowed
        /// leaves the later node with nothing bound, which is the answer they gave rather than a failed
        /// step.
        /// </summary>
        private static EffectNodeDefinition RemovalNode(
            EffectNodeId nodeId, EffectNodeId selectionNodeId, string spaceOutput) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticExchangeCapabilities.RemoveV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", selectionNodeId, spaceOutput, EffectValueKind.Coordinate),
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

        /// <summary>
        /// A root whose whole content is the card's <c>This round: You are enraged.</c> - Claws That
        /// Catch's step-7 root, its only remaining use now the three removal Reds compose their full
        /// clause.
        /// </summary>
        private static EffectDefinition CreateGrantOnly(
            CardDefinitionId cardId, string contentRevision, EffectDefinitionId effectId)
        {
            var root = new EffectNodeId("grant-root");
            return new EffectDefinition(
                effectId,
                contentRevision,
                root,
                new[] { GrantNode(root, cardId) });
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes. Written once because eighteen of nineteen faces use the first and thirteen use
        // the second.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "If enraged, ..." - a Conditional with a then-child and no else-child, which is the shape a
        /// condition that merely skips has.
        /// </summary>
        private static EffectNodeDefinition EnragedGate(EffectNodeId nodeId, EffectNodeId thenChild) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticConditionCapabilities.HeroConditionV1,
                childNodeIds: new[] { thenChild },
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticConditionCapabilities.ConditionParameter, HeroConditionKind.Enraged)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticConditionCapabilities.PresentOutput, EffectValueKind.Boolean)
                });

        /// <summary>"This round: You are enraged." - this card's own instance of the state.</summary>
        private static EffectNodeDefinition GrantNode(EffectNodeId nodeId, CardDefinitionId cardId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                UrsafarActiveEffectContent.GrantFor(cardId),
                outputs: new[]
                {
                    new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                });

        /// <summary>"perform the primary action on one of your ... cards".</summary>
        private static EffectNodeDefinition BorrowNode(
            EffectNodeId nodeId, BorrowedActionPool pool, CardColor requiredColor) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticBorrowedActionCapabilities.PoolParameter, pool),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticBorrowedActionCapabilities.ColorParameter, requiredColor)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                        EffectValueKind.CardInstance)
                });

        /// <summary>"target a unit adjacent to you" - p.19's units, one space away.</summary>
        private static EffectNodeDefinition AdjacentEnemyUnitTarget(EffectNodeId nodeId) =>
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
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                });

        private static EffectNodeDefinition BindAttackTarget(
            EffectNodeId nodeId, EffectNodeId targetNodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, targetNodeId,
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, targetNodeId,
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

        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        private const string ActorSpaceOutput = "space";
        private const string ChargeDestinationOutput = "destination";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");

        private static EffectDefinitionId SecondAttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-second-attack-text");

        private static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-movement-text");
    }
}

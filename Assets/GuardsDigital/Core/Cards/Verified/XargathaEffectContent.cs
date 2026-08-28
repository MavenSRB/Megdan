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
    /// Xargatha's printed card text, composed as typed effect definitions. Milestone 8.24.
    ///
    /// <para>Every sentence below is transcribed in <c>Docs/AI/Rules/HERO_XARGATHA.md</c> and mapped in
    /// the roster capability matrix. What this file adds is the composition: which nodes each sentence
    /// becomes, in the order the face prints them, over contracts that already exist. Nothing here
    /// parses printed text, names a hero in a rule, or reads a card by name at runtime.</para>
    ///
    /// <para><b>Three of her faces print card text around a Movement</b>, which no earlier verified
    /// face does. p.11 says the text "comes into effect before movement" <em>unless it says
    /// otherwise</em>, and Charm, Control and Dominate say otherwise in as many words - "before or
    /// after movement" - so the side is the actor's answer rather than the page's default. That is
    /// <c>around-primary-action@1</c>, which milestone 8.19a built for exactly this shape on content
    /// that was nobody's card.</para>
    ///
    /// <para><b>Five of her faces print a <c>+</c> and compose nothing here.</b> Dangerous Slash, Deadly
    /// Swipe, Lethal Spin, Long Thrust and Rapid Thrusts scale a value with a live count, and a value
    /// contribution is not a step: nobody chooses, nothing moves, and the clause is read when the total
    /// is computed. Those declarations are on the action in <see cref="XargathaCardContent"/>, and what
    /// is here for Rapid Thrusts is only its repeat.</para>
    /// </summary>
    public static class XargathaEffectContent
    {
        /// <summary>The Ultimate's rule identity, named here because the card content references it.</summary>
        public const string MetamorphosisRuleId = "xargatha-metamorphosis";

        private const string PresentOutput = "present";
        private const string CountOutput = "count";
        private const string RetrievedOutput = "retrieved";
        private const string MovedOutput = "moved";

        /// <summary>
        /// "Gain +1 [Movement] Movement and +1 [Initiative] Initiative for each enemy unit adjacent to
        /// you. You may move through obstacles." - Metamorphosis, as p.16's permanent ability.
        ///
        /// <para><b>Three declarations because the card prints three.</b> The two values scale
        /// independently - a hero who gains Movement gains Initiative on the same count, but they are
        /// different channels answered by different queries - and the permission scales with nothing at
        /// all. p.16 gives an Ultimate one ability; several declarations composing one printed sentence
        /// is not several abilities, which is the distinction
        /// <see cref="UltimateRuleDefinition.PermanentAbility"/> exists to keep.</para>
        ///
        /// <para><b>The count is not in the declaration and cannot be.</b> Enemies walk in and out
        /// while the ability stands unchanged, so what the card states is the amount per adjacent unit
        /// and the live board states the total. p.7 rechecks the highest Initiative after every card
        /// resolves, so a minion stepping next to her can reorder her own unresolved card - which is a
        /// consequence of recalculating at each owning query rather than a rule stated anywhere
        /// here.</para>
        ///
        /// <para><b>The Move-Through is p.18's, granted rather than declared on one move.</b> She may
        /// enter and leave obstacle spaces while moving and may never end in one; that last half is
        /// p.18's own sentence and is not restated as a rule of hers.</para>
        /// </summary>
        public static IReadOnlyList<RuleModification> MetamorphosisModifications { get; } =
            Array.AsReadOnly(new[]
            {
                RuleModification.ForDerivedValue(
                    PrintedValueChannel.Movement,
                    SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject,
                    1,
                    RuleValueApplicability.Always),
                RuleModification.ForDerivedValue(
                    PrintedValueChannel.Initiative,
                    SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject,
                    1,
                    RuleValueApplicability.Always),
                RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles)
            });

        // -----------------------------------------------------------------------------------------
        // p.14 step 7: the attacker's "After the attack:" text
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The body one printed Attack of Xargatha's applies, or <c>null</c> where it prints none.
        /// Paired with <see cref="AttackTextTimingFor"/>, which names which of p.14's moments that is.
        ///
        /// <para>Two of her seven Attacks compose anything at all. The three adjacency-scaled melee
        /// Attacks and Long Thrust print a value clause rather than a step, and Desperate Cleave is the
        /// boxed Handicap card.</para>
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(XargathaCardContent.Cleave) ||
                cardId.Equals(XargathaCardContent.RapidThrusts))
            {
                return CreateEnemyHeroRepeat(cardId, contentRevision);
            }

            // Desperate Cleave composes nothing, exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang and Tigerclaw's Nimble Strike compose
            // nothing, by the same 2026-08-21 user decision: p.6 returns the Handicap card to the box
            // and the six-player pitch never brings it back, so its `PrintedTextComposition
            // .DeferredUntil` and a composed body are mutually exclusive. Its printed values and words
            // ship in the card content all the same. What it would need when a milestone deals it is
            // one call to CreateEnemyHeroRepeat, because it prints the identical repeat clause - the
            // only difference between the two faces is the kind its first target admits, and that is
            // on the card rather than here.

            return null;
        }

        /// <summary>
        /// Which of p.14's moments <see cref="AttackTextFor"/>'s body is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription. Both of hers print
        /// "After the attack:", and neither prints a second moment.
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(XargathaCardContent.Cleave) ||
            cardId.Equals(XargathaCardContent.RapidThrusts)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        // -----------------------------------------------------------------------------------------
        // p.12's Skill primaries
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(XargathaCardContent.SirensCall))
            {
                return CreateSirensCall(cardId, contentRevision);
            }

            if (cardId.Equals(XargathaCardContent.StoneGaze))
            {
                return CreatePetrification(
                    cardId, contentRevision, XargathaActiveEffectContent.StoneGazeV1);
            }

            if (cardId.Equals(XargathaCardContent.PetrifyingStare))
            {
                return CreatePetrification(
                    cardId, contentRevision, XargathaActiveEffectContent.PetrifyingStareV1);
            }

            if (cardId.Equals(XargathaCardContent.TurnIntoStatues))
            {
                return CreatePetrification(
                    cardId, contentRevision, XargathaActiveEffectContent.TurnIntoStatuesV1);
            }

            if (cardId.Equals(XargathaCardContent.FreshConverts))
            {
                // "If you are adjacent to an enemy minion" - the narrower of the pair.
                return CreateConditionalRetrieval(cardId, contentRevision, RosterUnitKind.Minion);
            }

            if (cardId.Equals(XargathaCardContent.DevotedFollowers))
            {
                // "an enemy unit" - p.19's word, so a hero satisfies it as well as a minion.
                return CreateConditionalRetrieval(cardId, contentRevision, RosterUnitKind.Unit);
            }

            if (cardId.Equals(XargathaCardContent.Constrict))
            {
                return CreateEndOfRoundSource(
                    cardId, contentRevision, XargathaActiveEffectContent.ConstrictV1);
            }

            if (cardId.Equals(XargathaCardContent.FinalEmbrace))
            {
                return CreateEndOfRoundSource(
                    cardId, contentRevision, XargathaActiveEffectContent.FinalEmbraceV1);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // p.11's Movement primaries, and the text she places either side of them
        // -----------------------------------------------------------------------------------------

        public static EffectDefinition MovementTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(XargathaCardContent.Charm))
            {
                return CreateCommandedMinionMove(
                    cardId, contentRevision, RosterMinionClass.Ranged, overridesHeavyImmunity: false);
            }

            if (cardId.Equals(XargathaCardContent.Control))
            {
                return CreateCommandedMinionMove(
                    cardId, contentRevision, RosterMinionClass.RangedOrMelee,
                    overridesHeavyImmunity: false);
            }

            if (cardId.Equals(XargathaCardContent.Dominate))
            {
                // "an enemy minion ...; ignore heavy minion's immunity" - the only one of the three
                // that prints no class adjective, and the only one that prints the override. The two
                // go together on the face: a card admitting Heavy minions has to say what happens to
                // p.18's immunity, and this one says it.
                return CreateCommandedMinionMove(
                    cardId, contentRevision, RosterMinionClass.AnyClass,
                    overridesHeavyImmunity: true);
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // Compositions
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "After the attack: May repeat once on a different enemy hero." - Cleave and Rapid Thrusts.
        ///
        /// <para><b>One node, and everything the sentence says is on it.</b> "May" is the failure
        /// policy and the chooser; "once" is the declared maximum; "on a different enemy hero" is
        /// <see cref="RepeatTargetBinding.DifferentEnemyHero"/>, which is both an exclusion and a
        /// narrowing - the repeated instance may not take the unit this one took, and may take only an
        /// enemy hero.</para>
        ///
        /// <para><b>Cleave's parenthesis is the consequence of those two halves rather than a third
        /// rule.</b> "You may repeat even if the original target was a minion" follows from the printed
        /// instance admitting units and the repeat admitting heroes; p.9 makes reminder text
        /// clarification, so it composes to nothing.</para>
        ///
        /// <para><b>What Rapid Thrusts recalculates, it recalculates by being a separate action.</b> Its
        /// Range scales with the enemies adjacent to her, and p.10 reapplies the same Range rule and
        /// restrictions to the repeated instance - so a minion the first attack removed is a space of
        /// Range the second one no longer has. Nothing here states that: the contribution is on the
        /// action, and the repeated instance asks for it again.</para>
        /// </summary>
        private static EffectDefinition CreateEnemyHeroRepeat(
            CardDefinitionId cardId, string contentRevision)
        {
            var repeat = new EffectNodeId("repeat");
            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                repeat,
                new[]
                {
                    new EffectNodeDefinition(
                        repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                        chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1,
                        repeatTargetBinding: RepeatTargetBinding.DifferentEnemyHero)
                });
        }

        /// <summary>
        /// "Target an enemy unit not adjacent to you and in range; if able, move the target up to 3
        /// spaces to a space adjacent to you." - Siren's Call.
        ///
        /// <para><b>It is a Skill and its text is Skill text.</b> The face prints "Basic Skill -
        /// Ranged" with a Range icon and no Attack value, so "target" here is the card's own selection
        /// rather than p.14's - nothing is attacked, and there is no Attack Total for the move to
        /// happen before. The roster capability matrix maps this row through an Attack's target seam;
        /// the face is what is composed, by the standing ruling that a hero's cards are the
        /// definition.</para>
        ///
        /// <para><b>Two printed bounds, and they are not the same bound.</b> "not adjacent to you and
        /// in range" is who may be chosen - p.9's Range disk without its innermost ring, which is why
        /// the selection carries the exclusion rather than the move. "to a space adjacent to you" is
        /// where that unit ends up, which is
        /// <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> at one space. That one is
        /// written in her text and not printed as an icon, so p.16's Radius item does not widen it -
        /// "Items never affect values in the card text" - and the capability takes it as a literal for
        /// exactly that reason.</para>
        ///
        /// <para><b>"if able" is on the movement alone.</b> A unit with no legal space beside her, or
        /// one that cannot be moved at all, is still a legal choice: the sentence selects first and
        /// moves second, and only the second half is qualified. An enclosing
        /// <see cref="EffectNodeKind.Optional"/> with <see cref="EffectFailurePolicy.IfAble"/> is what
        /// silently skips an infeasible move without changing how far a feasible move may travel.</para>
        ///
        /// <para><b>"up to 3 spaces" is a budget, not a distance.</b> The capability therefore carries
        /// <see cref="EffectFailurePolicy.UpTo"/> separately from the enclosing "if able". Its least
        /// distance is one: the target selection excludes adjacent units, so standing still cannot
        /// satisfy the final requirement to end beside her.</para>
        /// </summary>
        private static EffectDefinition CreateSirensCall(
            CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("siren-root");
            var target = new EffectNodeId("siren-target");
            var moveIfAble = new EffectNodeId("siren-move-if-able");
            var move = new EffectNodeId("siren-move");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, moveIfAble }),
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Movement)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        moveIfAble, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                        childNodeIds: new[] { move }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", target,
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.WithinActorRadius),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.ActorRadiusParameterName,
                                EffectValue.Integer(1))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// "<b>Next turn:</b> Enemy heroes in radius count as both heroes and terrain, and cannot
        /// perform movement actions." - Stone Gaze, Petrifying Stare and Turn Into Statues.
        ///
        /// <para>One node, because the whole of what the card does is leave an Active effect standing.
        /// It names no input: what the effect means is definition content on the effect itself, in
        /// <see cref="XargathaActiveEffectContent"/>, and whose effect it is comes from the frame's own
        /// actor.</para>
        /// </summary>
        private static EffectDefinition CreatePetrification(
            CardDefinitionId cardId, string contentRevision, EffectCapabilityReference capability)
        {
            var petrify = new EffectNodeId("petrify");
            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                petrify,
                new[]
                {
                    new EffectNodeDefinition(
                        petrify, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "<b>End of round:</b> Defeat an enemy melee minion adjacent to you." - Constrict, and Final
        /// Embrace with "melee or ranged" for "melee".
        ///
        /// <para>The same one-node shape the petrifications use, and for the same reason: neither card
        /// does anything when it is played. What each leaves behind is a source that runs at p.8's
        /// moment before the Minion Battle, and what it runs there is declared on the effect.</para>
        /// </summary>
        private static EffectDefinition CreateEndOfRoundSource(
            CardDefinitionId cardId, string contentRevision, EffectCapabilityReference capability)
        {
            var source = new EffectNodeId("end-of-round-source");
            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                source,
                new[]
                {
                    new EffectNodeDefinition(
                        source, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        /// <summary>
        /// "If you are adjacent to an enemy minion, you may retrieve a discarded card." - Fresh
        /// Converts, and Devoted Followers with p.19's "unit" for "minion".
        ///
        /// <para><b>Two non-mandatory readings in one sentence, and neither swallows the other.</b> The
        /// "if" is a branch nobody is asked about - a hero standing alone is never offered anything -
        /// and the "you may" behind it is a question she answers and may decline. That is the same
        /// pairing Tigerclaw's Combat Reflexes prints, and it is a condition and a choice rather than
        /// one thing twice.</para>
        ///
        /// <para><b>p.18 makes immunity irrelevant here.</b> The question is presence, not targeting,
        /// so an immune Heavy minion beside her satisfies Fresh Converts exactly as any other minion
        /// does.</para>
        ///
        /// <para>p.10: retrieving "means to take one of YOUR cards back into your hand", and "You can
        /// never retrieve another hero's cards" - so the owner and the chooser are the same hero and
        /// both are the actor.</para>
        /// </summary>
        private static EffectDefinition CreateConditionalRetrieval(
            CardDefinitionId cardId, string contentRevision, RosterUnitKind adjacentKind)
        {
            var root = new EffectNodeId("converts-root");
            var presence = new EffectNodeId("converts-presence");
            var gate = new EffectNodeId("converts-gate");
            var may = new EffectNodeId("converts-may");
            var retrieve = new EffectNodeId("converts-retrieve");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { presence, gate }),
                    new EffectNodeDefinition(
                        presence, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.PresenceCountV1,
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
                                adjacentKind),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            // She is the origin of the disk and never inside her own count of enemies,
                            // and this text encloses no target; both are declared false rather than
                            // left to a default, because the schema requires them and a card that
                            // means neither should say so.
                            EffectInputBinding.FromLiteral("excludeActor", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral("excludeTarget", EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(CountOutput, EffectValueKind.Integer),
                            new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { may },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", presence, PresentOutput, EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    // "you may" is the offer, and it is its own node: a Capability node opens no
                    // decision, so the chooser and the decline live on the Optional above it and the
                    // retrieve below is an ordinary mandatory step once it has been accepted.
                    new EffectNodeDefinition(
                        may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { retrieve },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
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
                            new EffectOutputBinding(RetrievedOutput, EffectValueKind.CardInstance)
                        })
                });
        }

        /// <summary>
        /// "You may move an enemy [class] minion in radius up to 2 spaces before or after movement." -
        /// Charm, Control and Dominate.
        ///
        /// <para><b>The order is the first question and it is mandatory.</b> "before or after movement"
        /// offers two sides and the actor must take one - the card does not offer standing aside, only
        /// choosing when. What may be declined is the move itself, which is the "you may" behind it. So
        /// the shape is a mandatory <see cref="EffectNodeKind.Choose"/> of exactly one over two
        /// <c>around-primary-action@1</c> branches, and the optional move sits after it.</para>
        ///
        /// <para><b>Why the placement is a capability rather than a flag on the action.</b> The
        /// enclosing Movement runs once, and where the text runs relative to it is a decision made
        /// inside the text's own frame - which is what milestone 8.19a built when it added a
        /// continuation that owns the primary action's two sides.</para>
        ///
        /// <para><b>The disk is the printed Radius icon and the distance is written in the text.</b>
        /// p.9 gives the first to p.16's Radius item and p.16 expressly withholds the second, so a
        /// Radius item widens who may be moved and never how far.</para>
        ///
        /// <para><b>Dominate's override is on the move, not on her.</b> p.18's Heavy immunity is
        /// something the minion has; "ignore heavy minion's immunity" is this card's permission to move
        /// it anyway, declared on the node that moves it and on the selection that offers it - both,
        /// because a piece filtered out of the candidates is not one to offer and then refuse.</para>
        /// </summary>
        private static EffectDefinition CreateCommandedMinionMove(
            CardDefinitionId cardId,
            string contentRevision,
            RosterMinionClass admittedClass,
            bool overridesHeavyImmunity)
        {
            var root = new EffectNodeId("green-root");
            var order = new EffectNodeId("green-order");
            var before = new EffectNodeId("green-before");
            var after = new EffectNodeId("green-after");
            var may = new EffectNodeId("green-may");
            var sequence = new EffectNodeId("green-sequence");
            var target = new EffectNodeId("green-target");
            var move = new EffectNodeId("green-move");

            var overrides = overridesHeavyImmunity
                ? new[]
                {
                    new ImmunityOverrideDeclaration(
                        RuleActivity.DirectMove, ImmunityOverrideReason.PrintedHeavyMinionImmunity)
                }
                : null;

            return new EffectDefinition(
                MovementTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { order, may }),
                    new EffectNodeDefinition(
                        order, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { before, after },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    PlacementBranch(before, SyntheticContinuationCapabilities.PlacementBeforeTheAction),
                    PlacementBranch(after, SyntheticContinuationCapabilities.PlacementAfterTheAction),
                    new EffectNodeDefinition(
                        may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { sequence },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, move }),
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.AdmittedMinionClassParameterName,
                                admittedClass),
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.DirectMove)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        immunityOverrides: overrides),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", target,
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        },
                        immunityOverrides: overrides)
                });
        }

        /// <summary>One side of p.11's Movement, as the continuation capability milestone 8.19a built.</summary>
        private static EffectNodeDefinition PlacementBranch(EffectNodeId nodeId, int side) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.AroundPrimaryActionV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticContinuationCapabilities.PlacementParameter,
                        (AroundActionPlacement)side)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.PlacementOutput, EffectValueKind.Integer)
                });

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-attack-text");

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-skill");

        private static EffectDefinitionId MovementTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId($"{cardId.Value}-movement-text");
    }
}

using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Misa's printed card text as typed effect definitions - milestone 8.35z29.
    ///
    /// <para>It sits beside <see cref="MisaCardContent"/> for the reason
    /// <see cref="WaspEffectContent"/> sits beside hers: that file is the transcription, and this one
    /// is the composition. A reader auditing what the cards say reads one; a reader auditing what the
    /// engine does with it reads the other.</para>
    ///
    /// <para><b>Every node here is a declaration over a registered contract.</b> Nothing reads a card
    /// name, a hero name or a printed sentence at runtime. Where a shape already existed - Arien's
    /// self-placing Skill, Brogan's modal, Sabina's bounded pair and Swap, Wasp's staged minion Defeat,
    /// Tigerclaw's crossing line - this file declares Misa's numbers against it rather than restating
    /// the shape.</para>
    ///
    /// <para><b>The five rulings this file composes, named where they land.</b> RULING 1 gives
    /// "at maximum range/radius" the exact-distance reading, which is why three of her cards reach
    /// ring-shaped capabilities that can legally offer nothing. RULING 28 makes her Ultimate's first
    /// clause a cardinality modification on her own modal. RULING 29 lets her Blues cross the map's
    /// water ring so long as they end on a playable space, which is the movement rule's own answer
    /// rather than anything declared here. RULING 30 confines the Ultimate's second clause to her own
    /// self-placements. RULING 32 puts Power Shot's condition at targeting time and Thunder Shot's at
    /// p.14's step 7.</para>
    /// </summary>
    public static class MisaEffectContent
    {
        /// <summary>Power Overwhelming's stable ability identity inside its card.</summary>
        public const string PowerOverwhelmingRuleId = "power-overwhelming";

        public static EffectDefinitionId FocusStepEffectId { get; } =
            new EffectDefinitionId("misa-focus-step-move");

        public static EffectDefinitionId DisciplineStepEffectId { get; } =
            new EffectDefinitionId("misa-discipline-step-move");

        public static EffectDefinitionId MasteryStepEffectId { get; } =
            new EffectDefinitionId("misa-mastery-step-move");

        public static EffectDefinitionId PowerOverwhelmingTriggerId { get; } =
            new EffectDefinitionId("misa-power-overwhelming-after-self-place");

        /// <summary>
        /// Power Overwhelming's whole printed content, as p.16's permanent ability: two paragraphs,
        /// two declarations, one card.
        ///
        /// <para><b>Paragraph one is a standing rule.</b> "Whenever you choose one, you may choose two
        /// different options instead, in any order" changes how many bullets a modal on her own card
        /// admits, which is <see cref="RuleModificationKind.ChoiceCardinality"/> - the first
        /// modification in this build of a control-flow node rather than of an object. The user's
        /// 2026-09-06 ruling settles both halves of what it reaches: her own cards' choose-one nodes,
        /// and on a two-bullet modal "two different options" means both. She prints exactly one modal,
        /// Watch How I Soar, so the whole observable effect of this paragraph is that her Gold basic
        /// places her AND defeats an adjacent minion.</para>
        ///
        /// <para><b>Paragraph two is a trigger.</b> "Each time after you place yourself, an enemy hero
        /// adjacent to you discards a card, if able" runs
        /// <see cref="PowerOverwhelmingTriggerId"/> after an action of hers that placed her, which is
        /// <see cref="UltimateTriggerCondition.AfterControllerPlacesSelf"/>. RULING 30 keeps it to her
        /// own placements: another hero's card putting her somewhere is not her placing herself, and
        /// p.18 makes a Swap neither movement nor placement, so neither of her Swap cards can reach
        /// it.</para>
        ///
        /// <para><b>In a handicapped game paragraph one has no trigger at all</b>, because p.20
        /// replaces Watch How I Soar with Leaf on the Wind, which prints the first bullet and no
        /// "Choose one". The ruling records that as printed rather than fixing it, and nothing here
        /// compensates for it: the modification still stands, and there is simply no modal for it to
        /// widen.</para>
        ///
        /// <para>Two declarations on one Ultimate is the shape Sabina's Big Sodding Gun already has -
        /// a standing rule and a trigger together - rather than the mutual exclusion that holds
        /// between the three <em>trigger</em> shapes.</para>
        /// </summary>
        public static UltimateRuleDefinition PowerOverwhelmingRule(string contentRevision) =>
            UltimateRuleDefinition.TriggeredAbility(
                PowerOverwhelmingRuleId,
                "MisaPowerOverwhelming",
                new EffectDefinitionReference(PowerOverwhelmingTriggerId, contentRevision),
                UltimateTriggerCondition.AfterControllerPlacesSelf,
                new[] { TimingCheckpoint.ActionEnded },
                Array.Empty<PrintedValueDefinition>(),
                new[] { RuleModification.ForChoiceCardinality(2) });

        /// <summary>
        /// "an enemy hero adjacent to you discards a card, if able" - what Power Overwhelming's second
        /// paragraph runs.
        ///
        /// <para><b>The adjacency is measured from where she now stands</b>, because the sentence is
        /// read after the placement and the placement is what moved her. Nothing captures her old
        /// space: the selection asks the live board around the actor, which is where she is.</para>
        ///
        /// <para><b>"an enemy hero" is a choice and "if able" is not.</b> p.10 gives an unassigned
        /// decision to the card's controller, so Misa picks which adjacent enemy hero when there is
        /// more than one; the discard itself opens no decision about whether, which is what "if able"
        /// means - a hero holding nothing simply does not discard, and the trigger ends quietly. Which
        /// card leaves the hand is the discarding hero's own decision, which is
        /// <c>discard-chosen-card@2</c>'s shape.</para>
        ///
        /// <para>The selection is <see cref="EffectFailurePolicy.IfAble"/> too, because "an enemy hero
        /// adjacent to you" may name nobody: she can place herself into open ground, and a trigger
        /// with no subject is a sentence with nothing to do rather than a failure.</para>
        /// </summary>
        public static EffectDefinition PowerOverwhelmingTriggeredEffect(string contentRevision)
        {
            var root = new EffectNodeId("overwhelm-root");
            var select = new EffectNodeId("overwhelm-hero");
            var discard = new EffectNodeId("overwhelm-discard");

            return new EffectDefinition(
                PowerOverwhelmingTriggerId,
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, discard }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: AdjacentEnemy(RosterUnitKind.Hero, RuleActivity.Skill),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter,
                                select, SyntheticRosterCapabilities.ChosenHeroOutput,
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
                });
        }

        /// <summary>
        /// Misa's Skill effect for one card, or <c>null</c> for a Skill whose composition is still
        /// owed. The card content asks this for every Skill primary it builds, and
        /// <see cref="ActionDefinition"/> refuses a Skill carrying neither this nor a stated debt.
        /// </summary>
        public static EffectDefinition SkillEffectFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(MisaCardContent.WatchHowISoar))
            {
                return CreateSoar(cardId, contentRevision);
            }

            if (cardId.Equals(MisaCardContent.SwoopIn))
            {
                return CreateSwoop(cardId, contentRevision);
            }

            if (cardId.Equals(MisaCardContent.SuddenBreeze))
            {
                return CreateLine(cardId, contentRevision, 3, LineRider.PlaceTraversedUnit);
            }

            if (cardId.Equals(MisaCardContent.GustOfWind))
            {
                return CreateLine(cardId, contentRevision, 4, LineRider.PlaceTraversedUnit);
            }

            if (cardId.Equals(MisaCardContent.DashAndSlash))
            {
                return CreateLine(cardId, contentRevision, 4, LineRider.TraversedHeroDiscards);
            }

            if (cardId.Equals(MisaCardContent.CrushingSquall))
            {
                return CreateLine(cardId, contentRevision, 5, LineRider.PlaceTraversedUnit);
            }

            if (cardId.Equals(MisaCardContent.DeathFromAbove))
            {
                return CreateLine(cardId, contentRevision, 5, LineRider.TraversedHeroDiscards);
            }

            if (cardId.Equals(MisaCardContent.Focus))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.FocusStepV1, "focus");
            }

            if (cardId.Equals(MisaCardContent.Discipline))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.DisciplineStepV1, "discipline");
            }

            if (cardId.Equals(MisaCardContent.Mastery))
            {
                return CreateActiveEffect(
                    SkillEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.MasteryStepV1, "mastery");
            }

            if (cardId.Equals(MisaCardContent.LivingTornado))
            {
                return CreateSwap(
                    cardId, contentRevision,
                    SyntheticRosterCapabilities.ChooseUnitPairAtMaximumRadiusV1);
            }

            if (cardId.Equals(MisaCardContent.StormSpirit))
            {
                return CreateSwap(
                    cardId, contentRevision,
                    SyntheticRosterCapabilities.ChooseUnitPairAtEqualDistanceInRadiusV1);
            }

            // Leaf on the Wind, whose composition the standing p.20 Handicap deferral parks. Its
            // printed values and words ship as inventory; nothing composes them, and the card content
            // states that debt on the row rather than here.
            return null;
        }

        /// <summary>
        /// Misa's card text for one printed Attack, or <c>null</c> where the face prints none. All five
        /// of her Attacks print text and all five print it at p.14's step 7.
        /// </summary>
        public static EffectDefinition AttackTextFor(CardDefinitionId cardId, string contentRevision)
        {
            if (cardId.Equals(MisaCardContent.ChallengeAccepted))
            {
                return CreateActiveEffect(
                    AttackTextEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.ChallengeAcceptedGuardV1, "challenge");
            }

            if (cardId.Equals(MisaCardContent.MatterOfHonor))
            {
                return CreateActiveEffect(
                    AttackTextEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.MatterOfHonorGuardV1, "honor");
            }

            if (cardId.Equals(MisaCardContent.WorthyOpponent))
            {
                return CreateActiveEffect(
                    AttackTextEffectId(cardId), contentRevision,
                    MisaActiveEffectContent.WorthyOpponentGuardV1, "worthy");
            }

            if (cardId.Equals(MisaCardContent.PowerShot))
            {
                return CreateShove(
                    cardId, contentRevision,
                    SyntheticRosterCapabilities.TargetWasAtMaximumRangeV1, "wasAtMaximumRange");
            }

            if (cardId.Equals(MisaCardContent.ThunderShot))
            {
                return CreateShove(
                    cardId, contentRevision,
                    SyntheticRosterCapabilities.TargetIsNotAdjacentV1, "notAdjacent");
            }

            return null;
        }

        /// <summary>
        /// Which of rulebook p.14's three moments an Attack's composed text is applied at, named by the
        /// keyword the face prints rather than parsed out of the transcription.
        ///
        /// <para>Misa's set uses one of the three. All five of her Attacks print "After the attack:"
        /// and none prints "Before the attack:" - which is a fact about her kit rather than a
        /// simplification here, and the reason her Red line never reorders itself the way Arien's and
        /// Brogan's do.</para>
        /// </summary>
        public static CardTextTiming AttackTextTimingFor(CardDefinitionId cardId) =>
            cardId.Equals(MisaCardContent.ChallengeAccepted) ||
            cardId.Equals(MisaCardContent.MatterOfHonor) ||
            cardId.Equals(MisaCardContent.WorthyOpponent) ||
            cardId.Equals(MisaCardContent.PowerShot) ||
            cardId.Equals(MisaCardContent.ThunderShot)
                ? CardTextTiming.AfterTheAttack
                : CardTextTiming.None;

        /// <summary>
        /// Every triggered definition Misa's content declares, for the catalog that registers them and
        /// for a fixture that needs them all.
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredDefinitions(string contentRevision) =>
            Array.AsReadOnly(new[]
            {
                PowerOverwhelmingTriggeredEffect(contentRevision),
                StepMove(FocusStepEffectId, contentRevision, 1, "focus-step"),
                StepMove(DisciplineStepEffectId, contentRevision, 2, "discipline-step"),
                StepMove(MasteryStepEffectId, contentRevision, 3, "mastery-step")
            });

        /// <summary>
        /// "<b>Choose one —</b> • Place yourself into a space at maximum range. • Defeat a minion
        /// adjacent to you." - Watch How I Soar, the only modal in Misa's nineteen faces.
        ///
        /// <para><b>The modal is p.10's and it is a node kind, not a branch.</b>
        /// <see cref="EffectNodeKind.Choose"/> is a chooser-owned selection among children, and p.10
        /// gives an unassigned decision to the card's controller. Printed, it admits one bullet and
        /// ignores the other; with Power Overwhelming unlocked the ceiling is raised to two and both
        /// run, in whichever order she picks them. Nothing about this definition changes between those
        /// two states - the cardinality is asked of the board when the offer is built, which is what
        /// makes a p.16 permanent ability able to reach a card printed years before it.</para>
        ///
        /// <para><b>Both bullets can be individually impossible, and the two-tier modal rule decides
        /// what that looks like.</b> The first has no legal destination when no space at exactly the
        /// printed Range is empty - a ring, not a disk, under RULING 1 - and the second has none when
        /// no minion stands beside her. An option that cannot be performed at all is greyed with its
        /// reason rather than offered; an option that is merely pointless would still be offered, and
        /// neither of these is that. When both are impossible the mandatory Choose pauses with both
        /// greyed and offers p.10's stop, which is milestone 6.5ae's shape and not a special case
        /// here.</para>
        ///
        /// <para><b>The card does not override p.13's friendly-Defeat prohibition.</b> Enemy relation,
        /// adjacency and immunity all apply. The Defeat is card-text-invoked, which
        /// is p.13's own family and milestone 8.13's staged shape.</para>
        /// </summary>
        private static EffectDefinition CreateSoar(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("soar-choose");
            var placeArm = new EffectNodeId("soar-place-arm");
            var actorSpace = new EffectNodeId("soar-actor-space");
            var place = new EffectNodeId("soar-place");
            var defeatArm = new EffectNodeId("soar-defeat-arm");
            var select = new EffectNodeId("soar-minion");
            var defeat = new EffectNodeId("soar-defeat");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { placeArm, defeatArm },
                        chooser: EffectChooserRole.Actor,
                        // p.10's "Choose one" exactly: one bullet is taken and the other ignored -
                        // until her own Ultimate raises the ceiling, which is asked of the board rather
                        // than written here.
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        placeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place }),
                    ActorSpaceNode(actorSpace),
                    PlaceAtMaximumRangeNode(place, actorSpace),
                    new EffectNodeDefinition(
                        defeatArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { select, defeat }),
                    new EffectNodeDefinition(
                        select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        // p.13 forbids defeating friendly units. The card does not override that
                        // restriction; its adjacency and the Skill's immunity checks also apply.
                        inputs: Adjacent(RosterUnitKind.Minion, RuleActivity.Skill, SourceTeamRelation.OpposingSource),
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
                                SyntheticConsequenceCapabilities.MinionParameter,
                                select, SyntheticRosterCapabilities.ChosenMinionOutput,
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
        /// "Place yourself into a space in radius adjacent to two or more enemy units. If you do, you
        /// may retrieve a discarded card." - Swoop In.
        ///
        /// <para><b>"If you do" is p.10's stop-and-skip restated, so it composes to the order and not
        /// to a condition.</b> The Place prints no "may": it is mandatory, and a mandatory step that
        /// cannot happen stops the action with everything before it standing - which means the retrieve
        /// is reached exactly when the Place happened. Composing a condition in front of it would be a
        /// second way of saying the same thing, and the two could come to disagree.</para>
        ///
        /// <para><b>The clause qualifies the destination, not her.</b> "adjacent to two or more enemy
        /// units" is a test of the space she is considering, which is why it is
        /// <c>place-in-radius-beside-two-or-more-enemies@1</c> rather than a presence count around the
        /// actor: a hero standing beside nobody may still have a destination standing beside two.</para>
        ///
        /// <para><b>The retrieve is her own card.</b> p.10: "Retrieving a card means to take one of
        /// <em>your</em> cards back into your hand ... You can never retrieve another hero's cards." So
        /// both the owner and the chooser are the acting hero, and <c>retrieve-card@1</c> already
        /// carries the whole rule.</para>
        /// </summary>
        private static EffectDefinition CreateSwoop(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId("swoop-root");
            var actorSpace = new EffectNodeId("swoop-actor-space");
            var place = new EffectNodeId("swoop-place");
            var retrieve = new EffectNodeId("swoop-retrieve");

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, place, retrieve }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRadiusBesideTwoOrMoreEnemiesV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.GamePiece),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),
                            // The face names no Spawn-point clause, and p.18 excludes none by default.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                PlaceSpawnPointExclusion.None)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                        }),
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
                        })
                });
        }

        /// <summary>
        /// Which rider one of Misa's five Blue Skills prints after its semicolon. The move in front of
        /// it is identical on all five and differs only in how far.
        /// </summary>
        private enum LineRider
        {
            /// <summary>
            /// "you may place an enemy unit you moved through into a space adjacent to you" - Sudden
            /// Breeze, Gust of Wind and Crushing Squall.
            /// </summary>
            PlaceTraversedUnit = 1,

            /// <summary>
            /// "an enemy hero you moved through discards a card, if able" - Dash and Slash and Death
            /// from Above. It names heroes where the other names units, and it opens no decision about
            /// whether.
            /// </summary>
            TraversedHeroDiscards = 2
        }

        /// <summary>
        /// "Move up to N spaces in a straight line, ignoring obstacles; [rider]" - all five Blue cards,
        /// which are one sentence with two parameters.
        ///
        /// <para><b>The crossing is declared on the movement, not granted to her.</b> "Ignoring
        /// obstacles" is a sentence about this movement, so it is bound as the Move's own
        /// <c>movesThroughObstacles</c> rather than as a standing
        /// <see cref="RulePermissionKind.MoveThroughObstacles"/>: the unit she crossed stops her again
        /// on her very next Move. That is the same reading milestone 8.23 recorded for Tigerclaw's
        /// crossing line.</para>
        ///
        /// <para><b>RULING 29 is the movement rule's own answer and nothing here implements it.</b> The
        /// user settled that the line passes through any obstacle including the map's water ring, "but
        /// as long as hero ends up on a playable space". Milestone 6.8e already classifies terrain as
        /// an obstacle a permitted mover may cross rather than as off-board, and p.18's endpoint rule is
        /// offered no permission at all - so the destinations this composition receives already are the
        /// ruling. Spaces beyond the ring are not obstacles but the absence of a space, which is why a
        /// line aimed at open water simply has no destination there.</para>
        ///
        /// <para><b>"up to" is the node's policy and the reason a boxed-in hero is not stopped.</b>
        /// p.10 makes "up to" a count whose smaller answers are legal results rather than failures, so
        /// a board offering no straight line at all leaves the Skill standing and skips the rest -
        /// which is the honest reading of "move up to 5 spaces" performed by a hero who can move
        /// none.</para>
        ///
        /// <para><b>The traversal is re-derived, not remembered.</b> The interior of a straight line
        /// between her origin and her destination is arithmetic, and the units standing on it are the
        /// board's to answer - so the rider asks <c>choose-traversed-unit@1</c> with both ends bound
        /// rather than reading a record the movement had to publish. Her origin is captured by the
        /// actor-space bridge before the movement, because by the time the rider runs she is standing
        /// on the other end.</para>
        /// </summary>
        private static EffectDefinition CreateLine(
            CardDefinitionId cardId, string contentRevision, int distance, LineRider rider)
        {
            var root = new EffectNodeId("line-root");
            var actorSpace = new EffectNodeId("line-actor-space");
            var line = new EffectNodeId("line-destination");
            var move = new EffectNodeId("line-move");
            var riderRoot = new EffectNodeId("line-rider");
            var select = new EffectNodeId("line-traversed");
            var apply = new EffectNodeId("line-apply");

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { actorSpace, line, move, riderRoot }),
                ActorSpaceNode(actorSpace),
                new EffectNodeDefinition(
                    line, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticRosterCapabilities.ChooseLineDestinationV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            "maximumDistance", EffectValue.Integer(distance)),

                        // "ignoring obstacles", declared rather than assumed. The shared
                        // `choose-line-destination@1` defaults to walking p.18's obstacles - milestone
                        // 8.35z30's three faces print no crossing - so all five of these Blue Skills
                        // say so here. This is the six-hero content wave's unification: one capability
                        // id, the superset schema, and the card's own words in the binding.
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(DestinationOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.NamedSpace),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                            line, DestinationOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    riderRoot, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { select, apply }),
                new EffectNodeDefinition(
                    select,
                    EffectNodeKind.Capability,
                    // Both riders reach for somebody who may not exist - she may have crossed nobody -
                    // and neither prints a condition in front of it, so both simply do not happen. The
                    // first prints "you may" over the whole clause and the second "if able" over the
                    // discard, and the selection in each is the same silent one.
                    EffectFailurePolicy.IfAble,
                    SyntheticRosterCapabilities.ChooseTraversedUnitV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "origin", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromNodeOutput(
                            "destination", line, DestinationOutput, EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter,
                            rider == LineRider.PlaceTraversedUnit
                                ? RosterUnitKind.Unit
                                : RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                    })
            };

            if (rider == LineRider.PlaceTraversedUnit)
            {
                nodes.Add(new EffectNodeDefinition(
                    apply, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticPlacementCapabilities.PlaceV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "subject", select, SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate),
                        // "into a space adjacent to you" - p.19's adjacency is radius one, written in
                        // the text rather than printed on an icon, which is why this arm of the Place
                        // takes a number the definition states.
                        EffectInputBinding.FromLiteral("radius", EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName,
                            PlaceEligiblePieceKind.GamePiece),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                    }));
            }
            else
            {
                nodes.Add(new EffectNodeDefinition(
                    apply, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter,
                            select, SyntheticRosterCapabilities.ChosenHeroOutput,
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
            }

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// "Swap two units at maximum radius." - Living Tornado, and "Swap two units in radius and at
        /// equal distance from you." - Storm Spirit.
        ///
        /// <para><b>Neither card can move Misa.</b> p.10's Picking Targets: "If your action is
        /// affecting you (your hero), it will say so in the card text. Otherwise, it will not affect
        /// you, even if it targets all units around you." Both faces say only "two units", so she is
        /// excluded - which the pair selection does by construction rather than by a declaration
        /// here.</para>
        ///
        /// <para><b>Neither can trigger her own Ultimate either.</b> p.18: a Swap "does not count as
        /// movement or placement". So even a Swap that moved her - which these cannot - would not be a
        /// placement, and RULING 30 records that as one of the two things the trigger excludes.</para>
        ///
        /// <para>The pair is named first and exchanged second, which is milestone 8.19b's bounded
        /// canonical pair feeding p.19's Swap - the same two nodes Sabina's Whirlwind already uses,
        /// with a different question in front of them.</para>
        /// </summary>
        private static EffectDefinition CreateSwap(
            CardDefinitionId cardId, string contentRevision, EffectCapabilityReference pairSelection)
        {
            var root = new EffectNodeId("swap-root");
            var pair = new EffectNodeId("swap-pair");
            var swap = new EffectNodeId("swap-exchange");

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
                        pairSelection,
                        inputs: new[]
                        {
                            // Milestone 8.35l: the Swap below decides whether a pair this offers can
                            // actually be taken, so the selection is told which node consumes it.
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                            new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                            new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                            new EffectOutputBinding("secondMinion", EffectValueKind.Minion),
                            new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
                        }),
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
                        })
                });
        }

        /// <summary>
        /// "After the attack: If the target [was at maximum range / is not adjacent to you], you may
        /// move it 1 space, to a space farther away from you." - Power Shot and Thunder Shot, which are
        /// identical in every printed value and differ only here.
        ///
        /// <para><b>RULING 32 makes the tense the difference and this composition states it as two
        /// capabilities.</b> Power Shot's past tense is judged at targeting time, against the two
        /// spaces p.14 captured when the Attack bound its target; Thunder Shot's present tense is
        /// judged at step 7, against the live board. Between those two moments a Defense card's text
        /// can move either piece, and that is exactly the gap the two tenses open.</para>
        ///
        /// <para><b>The displacement is a direct Move and not p.18's Push.</b> The face says "move it",
        /// names one space, and names where: farther from her. A Push would carry p.18's own chain and
        /// stopping rules, which this sentence does not print. "You may" is the node's policy, so a
        /// target with nowhere farther to go is not a failure - the step is simply declined or, with no
        /// destination at all, not taken.</para>
        /// </summary>
        private static EffectDefinition CreateShove(
            CardDefinitionId cardId,
            string contentRevision,
            EffectCapabilityReference condition,
            string conditionOutput)
        {
            var root = new EffectNodeId("shove-root");
            var test = new EffectNodeId("shove-test");
            var gate = new EffectNodeId("shove-gate");
            var sequence = new EffectNodeId("shove-sequence");
            var space = new EffectNodeId("shove-target-space");
            var move = new EffectNodeId("shove-move");

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { test, gate }),
                    new EffectNodeDefinition(
                        test, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        condition,
                        outputs: new[]
                        {
                            new EffectOutputBinding(conditionOutput, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { sequence },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", test, conditionOutput, EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { space, move }),
                    new EffectNodeDefinition(
                        space, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(TargetSpaceOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", space, TargetSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.AwayFromActor)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// The free step one of Misa's three Green cards gives her at the start of her next turn:
        /// "Before you perform a primary action, you may move 1 / up to 2 / up to 3 spaces."
        ///
        /// <para><b>"You may move" names no piece to choose between</b>, so the subject reaches the
        /// Move through the actor-space bridge rather than through a selection - milestone 6.5a's
        /// reason exactly, and the same shape Arien's Living Tsunami uses for the same sentence. What
        /// the actor is asked is whether, and then where.</para>
        ///
        /// <para><b>The distance policy is the printed word.</b> Focus says "move 1 space" and the
        /// other two say "up to", which p.11 makes the difference between an exact step and a bounded
        /// one - so the node's failure policy carries the distinction rather than a number doing it.
        /// Both are declinable, because all three faces print "you may".</para>
        /// </summary>
        private static EffectDefinition StepMove(
            EffectDefinitionId effectId, string contentRevision, int distance, string nodePrefix)
        {
            var root = new EffectNodeId(nodePrefix + "-root");
            var actorSpace = new EffectNodeId(nodePrefix + "-actor-space");
            var move = new EffectNodeId(nodePrefix + "-move");

            return new EffectDefinition(
                effectId,
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move }),
                    ActorSpaceNode(actorSpace),
                    new EffectNodeDefinition(
                        move,
                        EffectNodeKind.Capability,
                        distance == 1 ? EffectFailurePolicy.May : EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                            // The card says how far and nothing about where, so nothing is filtered by
                            // where anybody stands - p.11's own breadth.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.Unrestricted)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(MovedOutput, EffectValueKind.Coordinate)
                        })
                });
        }

        /// <summary>
        /// The one-node effect that raises one of Misa's Active effects, written once because six of
        /// her faces need it and they differ only in which declaration they raise.
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
        /// "Place yourself into a space at maximum range." - the first bullet of Watch How I Soar, and
        /// the whole of Leaf on the Wind when its deferral is lifted.
        ///
        /// <para>The Place is mandatory: the bullet prints no "may". Under RULING 1 the candidate set
        /// is a ring rather than a disk, so it can be empty on a perfectly ordinary board - and an
        /// empty set makes the bullet unperformable, which the two-tier modal rule greys rather than
        /// offers.</para>
        /// </summary>
        private static EffectNodeDefinition PlaceAtMaximumRangeNode(
            EffectNodeId nodeId, EffectNodeId actorSpace) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.PlaceAtMaximumRangeV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
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
        /// "yourself" refers to - and, on the Blue cards, so the rider can name where the line began.
        /// </summary>
        private static EffectNodeDefinition ActorSpaceNode(EffectNodeId nodeId) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                });

        /// <summary>An adjacent enemy of one kind, as three of Misa's selections declare it.</summary>
        private static IEnumerable<EffectInputBinding> AdjacentEnemy(
            RosterUnitKind unitKind, RuleActivity activity) =>
            Adjacent(unitKind, activity, SourceTeamRelation.OpposingSource);

        /// <summary>
        /// An adjacent unit filtered by the action's declared relation and activity.
        /// </summary>
        private static IEnumerable<EffectInputBinding> Adjacent(
            RosterUnitKind unitKind, RuleActivity activity, SourceTeamRelation relation) =>
            new[]
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                EffectInputBinding.FromLiteral(
                    SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticRosterCapabilities.UnitKindParameter, unitKind),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticRosterCapabilities.RelationParameter, relation),
                EffectInputBinding.FromClosedVocabulary("activity", activity)
            };

        private const string ActorSpaceOutput = "space";
        private const string DestinationOutput = "destination";
        private const string MovedOutput = "moved";
        // `attacked-target-space@1` declares its one output as `space`, which is the name that has to
        // appear on the binding: the definition validator checks a node's published outputs against the
        // capability's own schema, and it is what caught this being spelled `targetSpace` here.
        private const string TargetSpaceOutput = "space";

        private static EffectDefinitionId SkillEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        private static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}

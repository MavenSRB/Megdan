using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's five Green faces - milestone 8.69e. Three traps that make one enemy hero
    /// give a card up, and two that let him take one back and walk.
    ///
    /// <para><b>The traps are one body with one switch.</b> Bear Trap and Log Trap print the identical
    /// sentence and Deadfall Trap prints it with "or is defeated" in place of "if able", so all three
    /// compose through <see cref="TrapModal"/> and the only thing that separates them is which of
    /// <c>synthetic-discard-chosen-card</c>'s two live revisions each bullet reaches. That is the
    /// difference milestone 8.10 minted the second revision for and Wasp's Shock/Electroblast pair is
    /// its first statement: revision 1 is p.17's <c>if able</c>, where a hero with nothing to give
    /// gives none and the step is over, and revision 2 is one mandatory step with two ways to complete,
    /// which reports inability so the escalation can read it.</para>
    ///
    /// <para><b>The two bullets offer the same set, and that is a decision rather than an accident
    /// (BR-Q17e).</b> Milestone 8.66 settled that an <c>if able</c> discard DECLARES its selection's
    /// consumer so an empty-handed hero is never offered, and an escalating one must not. Neither
    /// happens here, for two reasons that point the same way. The first is the printed text: both
    /// bullets of one card print one clause, so a build where bullet 2 hid an empty-handed hero and
    /// bullet 1 offered them would have the card saying two different things about the same words -
    /// and <c>choose-target@2</c>, the selection the corpus already uses for "an enemy hero adjacent to
    /// you", carries no consumer declaration at all, so matching them the other way would mean widening
    /// a selection eight other heroes read. The second is p.10: "if able" is written precisely because
    /// an empty-handed hero is a legal pick, and Misa's Power Overwhelming - the corpus's own composed
    /// reading of the exact sentence bullet 1 prints - offers them and gives nothing. So the escalating
    /// face and the <c>if able</c> faces offer the identical candidates and differ only in what happens
    /// to the hero who cannot pay, which is the only thing their printed texts differ in.</para>
    ///
    /// <para><b>The grit is two independent sentences and the second is not conditioned on the
    /// first.</b> "You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to
    /// 3 or more obstacles, move up to N spaces." A declined retrieval is still a turn in which the
    /// move happens, and a move that cannot be made is still a turn in which the card was taken back -
    /// BR-Q8, and the reason the two sit as siblings under a plain Sequence rather than as a gate over
    /// a rider.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // The traps
        // -----------------------------------------------------------------------------------------

        /// <summary>The modal itself - "Choose one", 1..1, the actor's decision (p.10).</summary>
        public const string TrapModalNode = "trap-modal";

        /// <summary>Bullet 1's arm: "An enemy hero adjacent to you discards a card ...".</summary>
        public const string TrapAdjacentArmNode = "trap-adjacent";

        /// <summary>Bullet 1's hero selection.</summary>
        public const string TrapAdjacentSelectNode = "trap-adjacent-select";

        /// <summary>Bullet 1's discard.</summary>
        public const string TrapAdjacentDiscardNode = "trap-adjacent-discard";

        /// <summary>Bullet 1's "or is defeated" gate - Deadfall Trap only.</summary>
        public const string TrapAdjacentEscalationNode = "trap-adjacent-escalation";

        /// <summary>Bullet 1's Defeat - Deadfall Trap only.</summary>
        public const string TrapAdjacentDefeatNode = "trap-adjacent-defeat";

        /// <summary>
        /// Bullet 2's arm: "An enemy hero in radius who is adjacent to 3 or more obstacles ...".
        /// </summary>
        public const string TrapRadiusArmNode = "trap-radius";

        /// <summary>Bullet 2's hero selection, narrowed by 8.69a's printed-Radius flag.</summary>
        public const string TrapRadiusSelectNode = "trap-radius-select";

        /// <summary>Bullet 2's discard.</summary>
        public const string TrapRadiusDiscardNode = "trap-radius-discard";

        /// <summary>Bullet 2's "or is defeated" gate - Deadfall Trap only.</summary>
        public const string TrapRadiusEscalationNode = "trap-radius-escalation";

        /// <summary>Bullet 2's Defeat - Deadfall Trap only.</summary>
        public const string TrapRadiusDefeatNode = "trap-radius-defeat";

        /// <summary>
        /// "Choose one - • An enemy hero adjacent to you discards a card[, if able / , or is defeated].
        /// • An enemy hero in radius who is adjacent to 3 or more obstacles discards a card[, if able /
        /// , or is defeated]." - Bear Trap and Log Trap with
        /// <paramref name="escalatesToDefeat"/> false, Deadfall Trap with it true.
        ///
        /// <para><b>p.10's modal, composed as Brogan's Throwing Axe composes one</b> - a
        /// <see cref="EffectNodeKind.Choose"/> of 1..1 over two Sequence arms, the actor picking the
        /// bullet because p.10 gives an unassigned decision to the card's controller, and the arm not
        /// taken simply ignored. An arm whose selection can name nobody is not offered, which is what
        /// milestone 8.35l's feasibility does with a mandatory selection over an empty candidate
        /// set.</para>
        ///
        /// <para><b>Both bullets TARGET, so p.18's immunity applies to both.</b> p.19 is explicit -
        /// "Whenever your action has any effect on another unit in any way, you target that unit" - and
        /// taking a card out of somebody's hand is an effect on them. Each selection therefore declares
        /// the action's own activity (a Skill; these five faces are Skill primaries) and an immune
        /// enemy hero is refused as a candidate rather than picked and then found untouchable. That is
        /// the opposite answer to BR-Q1's own presence question, and the two live side by side here on
        /// purpose: bullet 2's NARROWING is a presence check that counts an immune hero as an obstacle,
        /// while bullet 2's SUBJECT is a target that an immune hero cannot be.</para>
        ///
        /// <para><b>Which card leaves the hand is the discarding hero's decision</b>, which is the whole
        /// of what <c>synthetic-discard-chosen-card</c> is: its chooser is the hero its input names, so
        /// the pause belongs to that seat and this build refuses an answer submitted for anyone
        /// else.</para>
        /// </summary>
        public static EffectDefinition TrapModal(
            CardDefinitionId cardId, string contentRevision, bool escalatesToDefeat)
        {
            var modal = new EffectNodeId(TrapModalNode);
            var adjacentArm = new EffectNodeId(TrapAdjacentArmNode);
            var radiusArm = new EffectNodeId(TrapRadiusArmNode);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { adjacentArm, radiusArm },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1)
            };

            nodes.AddRange(TrapBullet(
                adjacentArm,
                new EffectNodeId(TrapAdjacentSelectNode),
                new EffectNodeId(TrapAdjacentDiscardNode),
                new EffectNodeId(TrapAdjacentEscalationNode),
                new EffectNodeId(TrapAdjacentDefeatNode),
                AdjacentEnemyHeroSelection(),
                SyntheticRosterCapabilities.ChooseTargetV2,
                SyntheticRosterCapabilities.ChosenHeroOutput,
                escalatesToDefeat));

            nodes.AddRange(TrapBullet(
                radiusArm,
                new EffectNodeId(TrapRadiusSelectNode),
                new EffectNodeId(TrapRadiusDiscardNode),
                new EffectNodeId(TrapRadiusEscalationNode),
                new EffectNodeId(TrapRadiusDefeatNode),
                NarrowedRadiusEnemyHeroSelection(),
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                escalatesToDefeat));

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, modal, nodes);
        }

        /// <summary>
        /// One bullet: the hero, the card they give up, and - where the sentence prints it - the defeat
        /// of a hero who had none.
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> TrapBullet(
            EffectNodeId armNode,
            EffectNodeId selectNode,
            EffectNodeId discardNode,
            EffectNodeId escalationNode,
            EffectNodeId defeatNode,
            IEnumerable<EffectInputBinding> selection,
            EffectCapabilityReference selectionCapability,
            string chosenHeroOutput,
            bool escalatesToDefeat)
        {
            yield return new EffectNodeDefinition(
                armNode, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: escalatesToDefeat
                    ? new[] { selectNode, discardNode, escalationNode }
                    : new[] { selectNode, discardNode });

            yield return new EffectNodeDefinition(
                selectNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                selectionCapability,
                inputs: selection,
                outputs: new[]
                {
                    new EffectOutputBinding(chosenHeroOutput, EffectValueKind.Hero)
                });

            yield return new EffectNodeDefinition(
                discardNode, EffectNodeKind.Capability,
                // "if able" IS the policy - p.17's own word - and "or is defeated" is one mandatory
                // step with two ways to complete, so it never declines.
                escalatesToDefeat ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.IfAble,
                escalatesToDefeat
                    ? SyntheticCardStateCapabilities.DiscardChosenCardV2
                    : SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter,
                        selectNode, chosenHeroOutput, EffectValueKind.Hero)
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
                    });

            if (!escalatesToDefeat)
            {
                yield break;
            }

            yield return new EffectNodeDefinition(
                escalationNode, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.IsFlaggedV1,
                childNodeIds: new[] { defeatNode },
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "flag", discardNode,
                        SyntheticCardStateCapabilities.UnableToDiscardOutput,
                        EffectValueKind.Boolean)
                },
                outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

            // p.13's Defeat, whole and unmodified: the coins, the respawn and the scoreboard's kill are
            // the page's own schedule, so nothing here declares a multiplier - Deadfall Trap prints no
            // clause about the reward.
            yield return new EffectNodeDefinition(
                defeatNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticConsequenceCapabilities.DefeatHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticConsequenceCapabilities.HeroParameter,
                        selectNode, chosenHeroOutput, EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticConsequenceCapabilities.DefeatedHeroOutput, EffectValueKind.Hero)
                });
        }

        /// <summary>
        /// "An enemy hero adjacent to you" - p.17's innermost ring, opposing, heroes only. Misa's
        /// Power Overwhelming prints the identical phrase and reaches it exactly this way.
        /// </summary>
        private static IEnumerable<EffectInputBinding> AdjacentEnemyHeroSelection() =>
            new[]
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

                // These five faces are Skill primaries, so the immunity p.18 asks about is a Skill's.
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
            };

        /// <summary>
        /// "An enemy hero in radius who is adjacent to 3 or more obstacles" - the printed-Radius hero
        /// selection with milestone 8.69a's optional Boolean set, which is the ONE place the printed
        /// phrase is answered for a selection and therefore the place his Ultimate reaches it.
        ///
        /// <para>Nothing is excluded from the innermost ring, because the sentence prints no "not
        /// adjacent to you" - stated rather than left unbound, the way Silverarrow's own radius
        /// selections state it.</para>
        /// </summary>
        private static IEnumerable<EffectInputBinding> NarrowedRadiusEnemyHeroSelection() =>
            new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName,
                    EffectValue.Boolean(true))
            };

        // -----------------------------------------------------------------------------------------
        // True Grit and Die Hard
        // -----------------------------------------------------------------------------------------

        /// <summary>The Sequence the two printed sentences hang from.</summary>
        public const string GritRootNode = "grit-root";

        /// <summary>"You may retrieve a discarded attack card."</summary>
        public const string GritRetrieveNode = "grit-retrieve";

        /// <summary>"If an enemy hero in radius is adjacent to 3 or more obstacles" - the question.</summary>
        public const string GritPresenceNode = "grit-presence";

        /// <summary>The printed "If ..." over that question.</summary>
        public const string GritGateNode = "grit-gate";

        /// <summary>The move the gate admits, as its own Sequence.</summary>
        public const string GritMoveArmNode = "grit-move-arm";

        /// <summary>Where Brynn is standing when the move starts.</summary>
        public const string GritActorSpaceNode = "grit-actor-space";

        /// <summary>"move up to N spaces".</summary>
        public const string GritMoveNode = "grit-move";

        /// <summary>
        /// "You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to 3 or
        /// more obstacles, move up to <paramref name="distance"/> spaces." - True Grit at 3 and Die Hard
        /// at 4, which is the only thing that separates the two bodies.
        ///
        /// <para><b>"a discarded attack card" is his OWN discard and p.9's noun</b> (BR-Q8). p.10 says
        /// outright that "you can never retrieve another hero's cards", so one source supplies both the
        /// owner and the chooser and both are the acting hero; <c>attackCardsOnly</c> is milestone
        /// 8.69e's filter, reading the candidate's printed primary action exactly as p.9 defines the
        /// word. "You may" is the node's own <see cref="EffectFailurePolicy.May"/>, and an empty
        /// discard - or one holding only Blues and Greens - offers nothing rather than refusing the
        /// card.</para>
        ///
        /// <para><b>The second sentence is checked, not targeted (p.19), so an immune enemy hero still
        /// satisfies it</b> - and his Ultimate satisfies it for every enemy hero in the disk without
        /// this body knowing an Ultimate exists, because the presence node asks
        /// <see cref="ObstacleAdjacencyQuery"/> through 8.69a's one predicate.</para>
        ///
        /// <para><b>The move is MANDATORY and "up to" is the distance's own optionality</b>, which is
        /// milestone 8.21's distinction between <see cref="EffectFailurePolicy.May"/> and
        /// <see cref="EffectFailurePolicy.UpTo"/> and Garrus's GA-Q1 precedent: once the presence holds
        /// the sentence is not optional, and zero spaces is a legal answer to it rather than a
        /// decline.</para>
        /// </summary>
        public static EffectDefinition Grit(
            CardDefinitionId cardId, string contentRevision, int distance)
        {
            var root = new EffectNodeId(GritRootNode);
            var retrieve = new EffectNodeId(GritRetrieveNode);
            var presence = new EffectNodeId(GritPresenceNode);
            var gate = new EffectNodeId(GritGateNode);
            var moveArm = new EffectNodeId(GritMoveArmNode);
            var actorSpace = new EffectNodeId(GritActorSpaceNode);
            var move = new EffectNodeId(GritMoveNode);

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { retrieve, presence, gate }),

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
                                "kind", RosterCardRetrievalKind.OwnedDiscardedCard),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.AttackCardsOnlyParameter,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrieved", EffectValueKind.CardInstance)
                        }),

                    EnemyHeroInRadiusAdjacentToObstaclesNode(presence),
                    GateOnPredicate(gate, presence, moveArm),

                    new EffectNodeDefinition(
                        moveArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, move }),

                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("space", EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(distance)),

                            // The card says only how far, so nothing is filtered by where he stands -
                            // p.11's own breadth.
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
    }
}

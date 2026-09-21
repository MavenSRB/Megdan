using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.69c. The three faces of Brynn the Seeker's that move somebody ELSE: Decoy, his
    /// Silver basic, and the two mountain guides.
    ///
    /// <para><b>Nothing here mints a contract.</b> Every piece of these three sentences is a live
    /// declaration - <c>synthetic-radius-minion-target@1</c> and
    /// <c>synthetic-radius-hero-target@1</c> with 8.69a's narrowing Boolean for Decoy's two bullets,
    /// <c>choose-target@2</c> and <c>synthetic-radius-unit-target@1</c> for the guides' two
    /// sentences, <c>synthetic-direct-move@3</c> for every move, and 8.69a's own presence predicate
    /// under its conditional gate for the guides' "if". What this block adds to the vocabulary is one
    /// printed CLAUSE - "on different targets", "a different friendly unit" - as two optional
    /// exclusion parameters on the printed-icon selections.</para>
    ///
    /// <para><b>BR-Q17c: Decoy's second performance is composed once per bullet the first took, and
    /// the reason is the definition validator rather than a reading.</b> The printed sentence is
    /// Rowenna's Grand Melee shape - "Choose up to two times" over two bullets - and the second
    /// performance has to exclude the first's unit, which means binding the first selection's
    /// published unit into the second selection's exclusion. <c>EffectDefinitionValidator</c> refuses
    /// a binding whose producer is not GUARANTEED to have run first, and nothing inside a
    /// <see cref="EffectNodeKind.Choose"/> arm is ever guaranteed to anything outside it - which is
    /// the same refusal Min's 8.61f met and answered by nesting her two passes. Nesting alone is not
    /// enough here, because Decoy's first performance ends inside one of two ARMS: so the second
    /// performance is the last step of each arm, and each copy excludes exactly what that arm named -
    /// the minion, or the hero, and never the other kind, which is what "on different targets" means
    /// when the two performances took different bullets. The reachable outcomes are still zero, one or
    /// two units, and the two copies are the same two bullets read twice.</para>
    ///
    /// <para><b>BR-Q18c: the guides' "You may" sits on the SELECTION and their "up to N" on the
    /// move.</b> p.11 separates the two - "unless the distance is optional ('up to 2 spaces'), or the
    /// move itself is optional ('you may move')" - and this sentence prints both. The selection is a
    /// <see cref="EffectFailurePolicy.May"/> capability node, so its prompt is declinable and a
    /// declined pass publishes no unit at all; the move is <see cref="EffectFailurePolicy.UpTo"/>, so
    /// the mover's own space is one of its candidates and zero spaces is a real answer (Garrus's
    /// GA-Q1, the user-approved precedent BR-Q9 names). That composition is what makes the exclusion
    /// legal as well: a <c>may</c> on the node itself leaves the node a direct child of the root
    /// Sequence, so the second sentence's selection may bind it - which a <c>may</c> WRAPPER would
    /// have forbidden, by the rule the paragraph above records.</para>
    ///
    /// <para><b>Nothing here is registered</b> - see <see cref="BrynnComponentContent"/>.</para>
    /// </summary>
    public static partial class BrynnEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // Node ids. A pause names the node a player is answering, so every one of them is spelled
        // once here rather than at each call site.
        // -----------------------------------------------------------------------------------------

        /// <summary>Decoy's whole body, as the Sequence its one performance hangs from.</summary>
        public const string DecoyRootNode = "decoy-root";

        /// <summary>"Choose up to two times" - the first performance's own optional step.</summary>
        public const string DecoyFirstPassNode = "decoy-1-may";

        /// <summary>The first performance's two printed bullets.</summary>
        public const string DecoyFirstModalNode = "decoy-1-modal";

        /// <summary>"Move an enemy minion in radius 1 space" - bullet 1, first performance.</summary>
        public const string DecoyFirstMinionBulletNode = "decoy-1-minion";

        /// <summary>
        /// "Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space" - bullet 2,
        /// first performance.
        /// </summary>
        public const string DecoyFirstHeroBulletNode = "decoy-1-hero";

        /// <summary>The second performance, as it stands after the first took the minion bullet.</summary>
        public const string DecoySecondPassAfterMinionNode = "decoy-2-after-minion-may";

        /// <summary>Its two bullets.</summary>
        public const string DecoySecondModalAfterMinionNode = "decoy-2-after-minion-modal";

        public const string DecoySecondMinionBulletAfterMinionNode = "decoy-2-after-minion-minion";
        public const string DecoySecondHeroBulletAfterMinionNode = "decoy-2-after-minion-hero";

        /// <summary>The second performance, as it stands after the first took the hero bullet.</summary>
        public const string DecoySecondPassAfterHeroNode = "decoy-2-after-hero-may";

        /// <summary>Its two bullets.</summary>
        public const string DecoySecondModalAfterHeroNode = "decoy-2-after-hero-modal";

        public const string DecoySecondMinionBulletAfterHeroNode = "decoy-2-after-hero-minion";
        public const string DecoySecondHeroBulletAfterHeroNode = "decoy-2-after-hero-hero";

        /// <summary>A guide's whole body.</summary>
        public const string GuideRootNode = "guide-root";

        /// <summary>"You may move a friendly unit adjacent to you" - the declinable selection.</summary>
        public const string GuideFirstUnitNode = "guide-first-unit";

        /// <summary>"... up to N spaces", skipped in silence where the selection named nobody.</summary>
        public const string GuideFirstMoveNode = "guide-first-move";

        /// <summary>The walk itself.</summary>
        public const string GuideFirstWalkNode = "guide-first-walk";

        /// <summary>"If an enemy hero in radius is adjacent to 3 or more obstacles" - 8.69a's predicate.</summary>
        public const string GuidePresenceNode = "guide-presence";

        /// <summary>The printed "if", as the conditional 8.69a's gate factory builds.</summary>
        public const string GuideGateNode = "guide-gate";

        /// <summary>The conditional sentence itself.</summary>
        public const string GuideSecondSentenceNode = "guide-second";

        /// <summary>"a different friendly unit in radius".</summary>
        public const string GuideSecondUnitNode = "guide-second-unit";

        /// <summary>"... up to N spaces", where zero is a legal distance.</summary>
        public const string GuideSecondWalkNode = "guide-second-walk";

        // -----------------------------------------------------------------------------------------
        // Decision labels
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// What each of this block's pauses says when its control is drawn.
        ///
        /// <para>The member is named for this block rather than <c>DecisionLabels</c> because four
        /// blocks of his series compose faces in parallel and a partial class cannot declare one
        /// member twice - Min's 8.61 blocks took the same arrangement, and her dealing milestone is
        /// what concatenated them. Brynn's aggregate is 8.69f's or 8.69g's to write, and until one of
        /// them names this method <c>DecisionLabelCatalog</c> holds none of his - which is right,
        /// because what a table is FOR is a window over a card a match deals.</para>
        ///
        /// <para><b>Only Decoy declares any, and that is the composition rather than an omission.</b>
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> is what decides
        /// which nodes owe words - an <c>Optional</c> that is not `if able`, and a `may`
        /// <c>RepeatAction</c> - and the guides declare neither: their first sentence's "you may" is a
        /// declinable SELECTION prompt, whose control names the unit it would pick, and the skip above
        /// their first walk is `if able`, which never asks.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> GuidesDecisionLabels()
        {
            yield return DecisionStep(BrynnCardContent.Decoy, DecoyFirstPassNode,
                "Move an enemy unit 1 space", "Move nobody");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoyFirstMinionBulletNode,
                "Move an enemy minion in radius 1 space");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoyFirstHeroBulletNode,
                "Move an enemy hero beside 3 obstacles 1 space");

            yield return DecisionStep(BrynnCardContent.Decoy, DecoySecondPassAfterMinionNode,
                "Move a second enemy unit 1 space", "Stop at one unit");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoySecondMinionBulletAfterMinionNode,
                "Move another enemy minion in radius 1 space");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoySecondHeroBulletAfterMinionNode,
                "Move an enemy hero beside 3 obstacles 1 space");

            yield return DecisionStep(BrynnCardContent.Decoy, DecoySecondPassAfterHeroNode,
                "Move a second enemy unit 1 space", "Stop at one unit");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoySecondMinionBulletAfterHeroNode,
                "Move an enemy minion in radius 1 space");
            yield return DecisionArm(BrynnCardContent.Decoy, DecoySecondHeroBulletAfterHeroNode,
                "Move another boxed-in enemy hero 1 space");
        }

        // -----------------------------------------------------------------------------------------
        // Decoy
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose up to two times, on different targets - • Move an enemy minion in radius 1 space.
        /// • Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space."
        ///
        /// <para><b>Each performance is an optional step over a two-arm modal</b>, which is Rowenna's
        /// Grand Melee composition of the identical printed words: there is no loop in this vocabulary
        /// and <c>RepeatAction</c> would repeat the whole Skill, so a printed count of two is two
        /// subtrees performed one at a time, each before the next is offered. "Up to" is the
        /// performance's own <see cref="EffectFailurePolicy.UpTo"/>, so zero performances is a legal
        /// and complete answer (BR-Q4), and taking the same bullet twice is ordinary rather than
        /// special.</para>
        ///
        /// <para><b>The distance is exact, so a unit with nowhere to go is never offered.</b> "1
        /// space" prints no "up to", which makes the move <see cref="EffectFailurePolicy.Mandatory"/>
        /// at a declared distance of one; each selection therefore names its Move through
        /// <see cref="EffectSelectionDeclaration.ConsumerParameterName"/>, and milestone 8.35l's
        /// narrowing drops a candidate the Move could do nothing with. That is BR-Q4's own sentence -
        /// "a unit with no empty neighbour cannot be moved and is not offered" - answered by the
        /// offer rather than by a refusal after the fact.</para>
        ///
        /// <para><b>Brynn chooses the destination, and no chooser is declared because of it.</b> p.10
        /// gives every unassigned decision in a card's text to its controller, and this sentence
        /// assigns none - so the unit that moves is moved by him, which is the difference between this
        /// and Rowenna's Throw the Gauntlet ("that hero may move 1 space"). The destination policy is
        /// p.11's own breadth: the card says how far and nothing about where.</para>
        ///
        /// <para><b>Bullet 2's narrowing is 8.69a's flag and nothing of this block's.</b> "who is
        /// adjacent to 3 or more obstacles" is
        /// <see cref="SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName"/>
        /// bound true, which reaches
        /// <see cref="ObstacleAdjacencyQuery"/> - so his Ultimate widens this bullet to every enemy
        /// hero in radius without this definition knowing an Ultimate exists.</para>
        /// </summary>
        public static EffectDefinition Decoy(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(DecoyRootNode);
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId(DecoyFirstPassNode) })
            };

            nodes.AddRange(DecoyPerformance(
                DecoyFirstPassNode,
                DecoyFirstModalNode,
                DecoyFirstMinionBulletNode,
                DecoyFirstHeroBulletNode,
                excludedMinionFrom: null,
                excludedHeroFrom: null,
                nextAfterMinion: DecoySecondPassAfterMinionNode,
                nextAfterHero: DecoySecondPassAfterHeroNode));

            // The second performance, once per bullet the first took - see the type's own remarks and
            // BR-Q17c. Each copy excludes exactly what its own branch named and nothing else: a first
            // performance that moved a minion says nothing about which heroes remain, and the reverse.
            nodes.AddRange(DecoyPerformance(
                DecoySecondPassAfterMinionNode,
                DecoySecondModalAfterMinionNode,
                DecoySecondMinionBulletAfterMinionNode,
                DecoySecondHeroBulletAfterMinionNode,
                excludedMinionFrom: DecoyFirstMinionBulletNode + "-target",
                excludedHeroFrom: null,
                nextAfterMinion: null,
                nextAfterHero: null));

            nodes.AddRange(DecoyPerformance(
                DecoySecondPassAfterHeroNode,
                DecoySecondModalAfterHeroNode,
                DecoySecondMinionBulletAfterHeroNode,
                DecoySecondHeroBulletAfterHeroNode,
                excludedMinionFrom: null,
                excludedHeroFrom: DecoyFirstHeroBulletNode + "-target",
                nextAfterMinion: null,
                nextAfterHero: null));

            return new EffectDefinition(SkillEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One printed performance of Decoy: the optional step that opens it, the two bullets, and
        /// each bullet's selection and one-space Move.
        ///
        /// <para><paramref name="nextAfterMinion"/> and <paramref name="nextAfterHero"/> are the next
        /// performance as that bullet leaves it, appended as the LAST step of the bullet's own
        /// Sequence - which is what makes this bullet's selection a guaranteed predecessor of the
        /// exclusion that reads it.</para>
        /// </summary>
        private static IEnumerable<EffectNodeDefinition> DecoyPerformance(
            string passNodeId,
            string modalNodeId,
            string minionBulletNodeId,
            string heroBulletNodeId,
            string excludedMinionFrom,
            string excludedHeroFrom,
            string nextAfterMinion,
            string nextAfterHero)
        {
            var pass = new EffectNodeId(passNodeId);
            var modal = new EffectNodeId(modalNodeId);
            var minionBullet = new EffectNodeId(minionBulletNodeId);
            var minionTarget = new EffectNodeId(minionBulletNodeId + "-target");
            var minionMove = new EffectNodeId(minionBulletNodeId + "-move");
            var heroBullet = new EffectNodeId(heroBulletNodeId);
            var heroTarget = new EffectNodeId(heroBulletNodeId + "-target");
            var heroMove = new EffectNodeId(heroBulletNodeId + "-move");

            // "Choose UP TO two times" prints no "may", so each performance carries p.10's own word
            // for a count whose floor is zero rather than a decline of a step - the shape Grand Melee
            // takes for the identical phrase.
            yield return new EffectNodeDefinition(
                pass, EffectNodeKind.Optional, EffectFailurePolicy.UpTo,
                childNodeIds: new[] { modal },
                chooser: EffectChooserRole.Actor);

            yield return new EffectNodeDefinition(
                modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { minionBullet, heroBullet },
                chooser: EffectChooserRole.Actor,
                minimumSelections: 1,
                maximumSelections: 1);

            // ---- Bullet 1: "Move an enemy minion in radius 1 space." ----
            yield return new EffectNodeDefinition(
                minionBullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: nextAfterMinion == null
                    ? new[] { minionTarget, minionMove }
                    : new[] { minionTarget, minionMove, new EffectNodeId(nextAfterMinion) });

            yield return DecoyMinionTarget(minionTarget, minionMove, excludedMinionFrom);
            yield return DecoyMove(
                minionMove, minionTarget,
                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName);

            // ---- Bullet 2: "Move an enemy hero in radius who is adjacent to 3 or more obstacles
            // 1 space." ----
            yield return new EffectNodeDefinition(
                heroBullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: nextAfterHero == null
                    ? new[] { heroTarget, heroMove }
                    : new[] { heroTarget, heroMove, new EffectNodeId(nextAfterHero) });

            yield return DecoyHeroTarget(heroTarget, heroMove, excludedHeroFrom);
            yield return DecoyMove(
                heroMove, heroTarget,
                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName);
        }

        /// <summary>
        /// "an enemy minion in radius" - p.9's disk read off Decoy's printed Radius 4 with p.16's
        /// items composed onto it, on the side opposing his.
        /// </summary>
        private static EffectNodeDefinition DecoyMinionTarget(
            EffectNodeId nodeId, EffectNodeId consumer, string excludedFrom)
        {
            var inputs = new List<EffectInputBinding>
            {
                // p.12: a Skill's effect IS the action, so the immunity question this selection asks
                // is the Skill's own. There is no enclosing Attack to inherit one from.
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer))
            };

            if (excludedFrom != null)
            {
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.ExcludedMinionParameterName,
                    new EffectNodeId(excludedFrom),
                    SyntheticTargetingCapabilities.ChosenMinionOutputName,
                    EffectValueKind.Minion));
            }

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusMinionTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion),

                    // A Move takes a coordinate and the sentence names a minion, so the arm publishes
                    // both - the pair it has published since milestone 8.20.
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate)
                });
        }

        /// <summary>
        /// "an enemy hero in radius who is adjacent to 3 or more obstacles" - the same disk, narrowed
        /// by the one printed predicate his whole set is built around.
        /// </summary>
        private static EffectNodeDefinition DecoyHeroTarget(
            EffectNodeId nodeId, EffectNodeId consumer, string excludedFrom)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),

                // The bullet prints no "not adjacent to you", so nothing is taken out of the disk's
                // innermost ring.
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                    EffectValue.Boolean(false)),

                // "who is adjacent to 3 or more obstacles" - milestone 8.69a's flag, which asks the
                // one owner of the printed phrase and therefore sees his Ultimate.
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName,
                    EffectValue.Boolean(true)),
                EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer))
            };

            if (excludedFrom != null)
            {
                inputs.Add(EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                    new EffectNodeId(excludedFrom),
                    SyntheticTargetingCapabilities.ChosenHeroOutputName,
                    EffectValueKind.Hero));
            }

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                        EffectValueKind.Coordinate)
                });
        }

        /// <summary>
        /// "... 1 space." - p.11's direct Move at an exact distance of one, of whichever piece the
        /// bullet above named, to any space p.11's reachability admits.
        /// </summary>
        private static EffectNodeDefinition DecoyMove(
            EffectNodeId nodeId, EffectNodeId subjectNodeId, string subjectSpaceOutput) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", subjectNodeId, subjectSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) });

        // -----------------------------------------------------------------------------------------
        // Mountain Guide and Expedition Leader
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "You may move a friendly unit adjacent to you up to N spaces. If an enemy hero in radius is
        /// adjacent to 3 or more obstacles, move a different friendly unit in radius up to N spaces." -
        /// Mountain Guide at two spaces and Expedition Leader at three, which is the only thing
        /// separating them.
        ///
        /// <para><b>The two sentences read two different reaches, and that is why they name two
        /// different selections.</b> "adjacent to you" is card text and p.16's items never raise it, so
        /// the first is the written-distance roster arm at a reach of one - Silverarrow's 8.35z46
        /// lesson. "in radius" is p.9's printed icon with items composed onto it, so the second is the
        /// printed-icon unit arm, which is what makes his Radius item matter to both faces.</para>
        ///
        /// <para><b>"Friendly" never includes him, and nothing here has to say so.</b> p.17 is
        /// explicit - "Friendly ... never includes you" - and both selections exclude the actor by
        /// construction: the roster arm always excludes the selector, and the printed-icon arm asks its
        /// disk without self on every arm that declares no <c>includeSelf</c>.</para>
        ///
        /// <para><b>The predicate is asked AFTER the first sentence, which is the printed order and a
        /// measurable difference.</b> A friendly unit the first sentence walked next to an enemy hero
        /// can be the third obstacle that makes the second sentence true; reading the "if" first would
        /// be asking it of a board the card has not finished changing.</para>
        ///
        /// <para><b>"a DIFFERENT friendly unit" excludes the first sentence's unit whether or not it
        /// moved, and excludes nobody when that sentence was declined.</b> Both halves fall out of one
        /// composition: the exclusion binds the SELECTION's published unit rather than the Move's
        /// result, so a unit named and left standing is still excluded; and a declined selection
        /// publishes nothing, so the binding resolves to no value and narrows nothing. The unit may be
        /// either kind, so both exclusions are bound and exactly one of them ever resolves.</para>
        ///
        /// <para><b>The second sentence is mandatory once the presence holds</b> - it prints no "may" -
        /// and its "up to N" is what lets it cost nothing: the Move admits its own mover's space under
        /// <see cref="EffectFailurePolicy.UpTo"/>, which is p.11's optional distance and the reading
        /// BR-Q9 settles out of Garrus's GA-Q1.</para>
        ///
        /// <para><b>The second selection declares no consumer, and that is measurement rather than an
        /// omission.</b> Milestone 8.35l's narrowing drops a candidate the consuming step could do
        /// nothing with; under an "up to" distance every friendly unit in the disk is one the Move can
        /// act on, because standing still is a legal answer - so a consumer would narrow nothing and
        /// would say that this sentence refuses a unit it does not refuse. Decoy's exact one space is
        /// the opposite case and declares one.</para>
        /// </summary>
        public static EffectDefinition Guide(
            CardDefinitionId cardId, string contentRevision, int spaces)
        {
            var root = new EffectNodeId(GuideRootNode);
            var firstUnit = new EffectNodeId(GuideFirstUnitNode);
            var firstMove = new EffectNodeId(GuideFirstMoveNode);
            var firstWalk = new EffectNodeId(GuideFirstWalkNode);
            var presence = new EffectNodeId(GuidePresenceNode);
            var gate = new EffectNodeId(GuideGateNode);
            var second = new EffectNodeId(GuideSecondSentenceNode);
            var secondUnit = new EffectNodeId(GuideSecondUnitNode);
            var secondWalk = new EffectNodeId(GuideSecondWalkNode);

            return new EffectDefinition(
                SkillEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { firstUnit, firstMove, presence, gate }),

                    // "You may move a friendly unit adjacent to you" - the `may` sits on the selection
                    // itself, so its prompt is declinable and a decline publishes no unit at all. See
                    // BR-Q18c for why that is also what makes the second sentence's exclusion legal.
                    new EffectNodeDefinition(
                        firstUnit, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),

                            // "adjacent", which p.19 makes the one ring around him and which p.16's
                            // items never raise - so the number is written here rather than read off
                            // an icon.
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),

                            // "a friendly UNIT" - p.19's units, heroes and minions both, on his own
                            // side. p.17 keeps him out of it.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),

                            // This text encloses no Attack, so there is no target to keep out of it;
                            // declared rather than left to a default, because a card that means
                            // nothing by it should say so.
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.ExcludeTargetParameter,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate)
                        }),

                    // A selection that named nobody leaves the walk below with no mover, and `if able`
                    // is what turns that into a step not taken rather than a stop - Xargatha's Siren's
                    // Call shape. It asks nothing of anybody.
                    new EffectNodeDefinition(
                        firstMove, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                        childNodeIds: new[] { firstWalk }),

                    GuideWalk(firstWalk, firstUnit,
                        SyntheticRosterCapabilities.ChosenSpaceOutput, spaces),

                    EnemyHeroInRadiusAdjacentToObstaclesNode(presence),
                    GateOnPredicate(gate, presence, second),

                    new EffectNodeDefinition(
                        second, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { secondUnit, secondWalk }),

                    new EffectNodeDefinition(
                        secondUnit, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),

                            // "a DIFFERENT friendly unit" - whichever kind the first sentence named,
                            // and nothing at all where it named nobody.
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                                firstUnit,
                                SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.ExcludedMinionParameterName,
                                firstUnit,
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),

                    GuideWalk(secondWalk, secondUnit,
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName, spaces)
                });
        }

        /// <summary>
        /// "... up to N spaces" - p.11's direct Move with the distance itself the decision, so the
        /// mover's own space is one of its candidates and zero spaces is a real answer.
        /// </summary>
        private static EffectNodeDefinition GuideWalk(
            EffectNodeId nodeId, EffectNodeId subjectNodeId, string subjectSpaceOutput, int spaces) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", subjectNodeId, subjectSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(spaces)),

                    // The card says how far and nothing about where, so nothing is filtered by where
                    // anybody stands - p.11's own breadth.
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) });
    }
}

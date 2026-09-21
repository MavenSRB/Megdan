using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's effect bodies for the clauses milestone 8.71c owns: the four graded Reds'
    /// modal - "Choose one[, or both, in any order] — • Target a unit adjacent to you. • Target a hero
    /// (minion) in range, adjacent to your friendly hero[ and not adjacent to you]" - and Fight and
    /// Flight's after-attack rider.
    ///
    /// <para><b>Helping Hand composes the same modal as the four above it</b>, at one bullet rather
    /// than two: its printed sentence is Even the Odds' word for word and only its Attack, Range,
    /// Defense and item differ, which is what the shard's inventory records. So this file holds ONE
    /// modal factory and one rider factory, and the five faces differ by what they hand them.</para>
    ///
    /// <para><b>Nothing here mints a capability.</b> Every node is a contract a dealt hero already
    /// starts: <c>attack-sequence@1</c> (Bain, Brynn, Dodger, Whisper, Tali), <c>attack-target-
    /// survived@1</c> (Ursafar, Min), <c>synthetic-is-flagged@1</c>, <c>actor-space@1</c>,
    /// <c>choose-line-destination@1</c> (Swift, Cutter, Misa, Silverarrow) and <c>direct-move@3</c>.
    /// What is new is two members on the closed vocabulary the bullets narrow by and one on the closed
    /// vocabulary they target by - see <see cref="HanuComponentContent"/>, whose deferral list is
    /// empty for exactly that reason.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        // -----------------------------------------------------------------------------------------
        // Node identities - spelled as constants because the decision-label table is keyed by them
        // and `BainMarkerPredicateTests`' lesson is that a label and a body must not transcribe one
        // id twice.
        // -----------------------------------------------------------------------------------------

        /// <summary>The Reds' modal node, so a paused chain names what it is owed.</summary>
        public const string RedModalNode = "hanu-red-modal";

        /// <summary>"Target a unit adjacent to you." - bullet 1 of all four graded Reds.</summary>
        public const string RedAdjacentBulletNode = "hanu-red-adjacent-bullet";

        /// <summary>
        /// "Target a hero (minion) in range, adjacent to your friendly hero[ and not adjacent to
        /// you]." - bullet 2. One node id across the four faces, because the four print one bullet
        /// whose noun and whose exclusion clause differ; the catalog is keyed by CARD and node, so
        /// each face still says its own words in the decision window.
        /// </summary>
        public const string RedFriendlyBulletNode = "hanu-red-friendly-bullet";

        /// <summary>Fight and Flight's root: the survival question and the gate over its rider.</summary>
        public const string FlightRootNode = "hanu-flight-root";

        /// <summary>"If the target is not defeated" - p.14 step 6's answer, read at step 7.</summary>
        public const string FlightSurvivedNode = "hanu-flight-survived";

        /// <summary>The printed "If ...", as the shared <c>synthetic-is-flagged@1</c> conditional.</summary>
        public const string FlightGateNode = "hanu-flight-gate";

        /// <summary>The rider itself, once the target is known to have survived.</summary>
        public const string FlightArmNode = "hanu-flight-arm";

        /// <summary>Where Hanu stands at p.14's step 7, which is where the file starts.</summary>
        public const string FlightActorSpaceNode = "hanu-flight-actor-space";

        /// <summary>"move 3 spaces in a straight line" - the offer of the legal ends of that file.</summary>
        public const string FlightLineNode = "hanu-flight-line";

        /// <summary>The move itself, onto the space the offer named.</summary>
        public const string FlightMoveNode = "hanu-flight-move";

        private const string RedActorSpaceOutput = "space";
        private const string RedLineDestinationOutput = "destination";

        // -----------------------------------------------------------------------------------------
        // The Reds
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • Target a hero in range, adjacent to your
        /// friendly hero." - Helping Hand and Even the Odds, whole.
        /// </summary>
        public static EffectDefinition HeroModalChoosingOne(
            CardDefinitionId cardId, string contentRevision) =>
            RedModal(
                cardId, contentRevision,
                mayTakeBoth: false,
                secondBulletUnits: AttackTargetableUnits.HeroesOnly,
                secondBulletNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);

        /// <summary>
        /// "Choose one — • Target a unit adjacent to you. • Target a minion in range, adjacent to your
        /// friendly hero." - Outnumber, whole. The only thing that separates it from the two faces
        /// above is the NOUN of its second bullet, which is what
        /// <see cref="AttackTargetableUnits.MinionsOnly"/> exists to carry.
        /// </summary>
        public static EffectDefinition MinionModalChoosingOne(
            CardDefinitionId cardId, string contentRevision) =>
            RedModal(
                cardId, contentRevision,
                mayTakeBoth: false,
                secondBulletUnits: AttackTargetableUnits.MinionsOnly,
                secondBulletNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);

        /// <summary>
        /// "Choose one, or both, in any order — • Target a unit adjacent to you. • Target a hero in
        /// range, adjacent to your friendly hero and not adjacent to you." - Trusted Sidekick.
        ///
        /// <para><b>Its bullets are disjoint by their own words, so it prints no "(on different
        /// targets)" and declares none</b> (HA-Q6, RULING 19's rider): a unit adjacent to Hanu cannot
        /// satisfy the second bullet at all, so the exclusion the parenthetical would add is already
        /// what the sentence says. Declaring it anyway would attach a rule the card does not carry -
        /// and would be visible the moment the first bullet's own p.14 pushed somebody out of his
        /// ring.</para>
        /// </summary>
        public static EffectDefinition HeroModalChoosingBoth(
            CardDefinitionId cardId, string contentRevision) =>
            RedModal(
                cardId, contentRevision,
                mayTakeBoth: true,
                secondBulletUnits: AttackTargetableUnits.HeroesOnly,
                secondBulletNarrowing: AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor);

        /// <summary>
        /// "Choose one, or both, in any order — • Target a unit adjacent to you. • Target a minion in
        /// range, adjacent to your friendly hero." - Pile On.
        ///
        /// <para><b>ONE MINION MAY TAKE BOTH BULLETS</b>, and that is RULING 19 read literally: the
        /// face prints no "(on different targets)", so <c>distinctTargets</c> is false on both of its
        /// bullets and a minion standing beside Hanu AND beside a teammate satisfies each sentence in
        /// its own turn. The name of the card is the reading. Each bullet still binds afresh against
        /// the board its own p.14 reaches, so a minion the first bullet removed is not a candidate for
        /// the second.</para>
        /// </summary>
        public static EffectDefinition MinionModalChoosingBoth(
            CardDefinitionId cardId, string contentRevision) =>
            RedModal(
                cardId, contentRevision,
                mayTakeBoth: true,
                secondBulletUnits: AttackTargetableUnits.MinionsOnly,
                secondBulletNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);

        /// <summary>
        /// The one modal the four graded Reds print, parameterised by the three things their printed
        /// sentences differ in: how many bullets may be taken, which units the second reaches, and
        /// which predicate it prints about its candidate.
        ///
        /// <para><b>It is Bain's <c>CreateMarkerModal</c> shape</b> - a <see cref="EffectNodeKind.Choose"/>
        /// at 1..1 or 1..2 over two <c>attack-sequence@1</c> leaves - because "Choose one" and "Choose
        /// one, or both" are p.10's Modal Actions read literally, and BA-D4 is the reading of what each
        /// chosen bullet then is: a complete p.14 order with its own targeting, its own Defense window
        /// and its own Attack Total, the whole of it ONE action.</para>
        ///
        /// <para><b>"In any order" needed nothing added, and HA-Q6 measured that rather than assuming
        /// it.</b> Milestone 8.35z29 already pushes a Choose's selected options in reverse of the order
        /// the Select commands named them, for Misa's Power Overwhelming and for p.10's own general
        /// sentence, so the plan <c>RequestedAttackSequenceQuery</c> builds is the actor's order and
        /// not the definition's. The only thing 8.71c had to do about the printed phrase was correct
        /// that query's own remark, which still claimed the opposite.</para>
        ///
        /// <para><b>Neither bullet declares "(on different targets)"</b>: Trusted Sidekick's are
        /// disjoint by their own words and Pile On's deliberately are not. See the two factories
        /// above.</para>
        /// </summary>
        private static EffectDefinition RedModal(
            CardDefinitionId cardId,
            string contentRevision,
            bool mayTakeBoth,
            AttackTargetableUnits secondBulletUnits,
            AttackSequenceSubjectNarrowing secondBulletNarrowing)
        {
            var modal = new EffectNodeId(RedModalNode);
            var adjacentBullet = new EffectNodeId(RedAdjacentBulletNode);
            var friendlyBullet = new EffectNodeId(RedFriendlyBulletNode);

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentBullet, friendlyBullet },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: mayTakeBoth ? 2 : 1),

                    // "Target a unit adjacent to you." - p.19's units at p.19's innermost ring, and no
                    // predicate at all, which the bullet says by printing nothing.
                    RedBulletNode(
                        adjacentBullet,
                        AttackTargetableUnits.HeroesAndMinions,
                        AttackTargetReach.AdjacentOnly,
                        AttackSequenceSubjectNarrowing.None),

                    // "Target a hero (minion) in range, adjacent to your friendly hero[ and not
                    // adjacent to you]." The reach is the card's own printed Range icon with p.16's
                    // items composed onto it - never a written number - which is what makes his
                    // Tier III Range item matter to the faces that print one.
                    RedBulletNode(
                        friendlyBullet,
                        secondBulletUnits,
                        AttackTargetReach.PrintedRange,
                        secondBulletNarrowing)
                });
        }

        // -----------------------------------------------------------------------------------------
        // Fight and Flight
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "Target a unit adjacent to you. If the target is not defeated, After the attack: If able,
        /// move 3 spaces in a straight line." - his Gold basic, whole.
        ///
        /// <para><b>The first sentence composes nothing</b>, which is Brynn's High Ground reading of
        /// the identical words: "Target a unit adjacent to you" is rulebook p.13's own reading of a
        /// non-Ranged Attack, and every Attack definition already carries it. Card text that said it
        /// would be a second statement of a rule the definition already makes.</para>
        ///
        /// <para><b>The condition is p.14 step 6's own answer, asked at step 7</b> (HA-Q7).
        /// <c>attack-target-survived@1</c> publishes whether the enclosing Attack's target is still on
        /// the board, read live off occupancy at the moment the text can ask - so a target that
        /// defended successfully, a target that survived the total and a target somebody else removed
        /// all reach one answer. <c>synthetic-is-flagged@1</c> is the gate, which is Min's Stances'
        /// shape over the same publisher and Brynn's <c>GateOnPredicate</c> over his own.</para>
        ///
        /// <para><b>"3 spaces in a straight line" is a COUNT, not a ceiling</b>, so both of
        /// <c>choose-line-destination@1</c>'s bounds are three and the consuming Move's
        /// <c>leastDistance</c> states the same floor a second time - Swift's Bounce exactly, including
        /// milestone 8.59's reason for the floor living on the OFFER: without it the question lights
        /// spaces the Move cannot reach, and answering with one of them names a move p.11 stops the
        /// action over.</para>
        ///
        /// <para><b>It does NOT ignore obstacles, and Bounce does.</b> Bounce prints "ignoring
        /// obstacles" and this card prints nothing, so neither the offer nor the Move declares the
        /// flag: p.11 is the default, the file stops at the first thing p.18 puts in it, and no unit
        /// ends on an obstacle. An absent clause is absent, never a false that had to be written.</para>
        ///
        /// <para><b>"If able" is the two IfAble nodes and nothing else.</b> A board on which no file of
        /// exactly three spaces is legal offers nothing, and an empty offer under
        /// <see cref="EffectFailurePolicy.IfAble"/> is a step not taken rather than an impossible one
        /// that stops the action - which is the showcase's own line-travel Skill shape, whose remarks
        /// record why the consuming node carries the policy too.</para>
        /// </summary>
        public static EffectDefinition FightAndFlight(CardDefinitionId cardId, string contentRevision)
        {
            var root = new EffectNodeId(FlightRootNode);
            var survived = new EffectNodeId(FlightSurvivedNode);
            var gate = new EffectNodeId(FlightGateNode);
            var arm = new EffectNodeId(FlightArmNode);
            var actorSpace = new EffectNodeId(FlightActorSpaceNode);
            var line = new EffectNodeId(FlightLineNode);
            var move = new EffectNodeId(FlightMoveNode);

            return new EffectDefinition(
                AttackTextEffectId(cardId),
                contentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { survived, gate }),

                    new EffectNodeDefinition(
                        survived, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticAttackTargetSurvivalCapabilities.AttackTargetSurvivedV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                                EffectValueKind.Boolean)
                        }),

                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { arm },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag", survived,
                                SyntheticAttackTargetSurvivalCapabilities.TargetSurvivedOutput,
                                EffectValueKind.Boolean)
                        },

                        // A predicate that publishes its answer must have the node declare it, or a
                        // restore re-deriving the recorded outcome finds an output the node does not
                        // admit and refuses the payload - milestone 8.11's finding on Thunder
                        // Boomerang's own conditional.
                        outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }),

                    new EffectNodeDefinition(
                        arm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move }),

                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(RedActorSpaceOutput, EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(3)),

                            // Equal bounds, because "move 3 spaces" is a count. Milestone 8.59's
                            // correction on Bounce is why the floor is stated to the OFFER and not
                            // only to the Move.
                            EffectInputBinding.FromLiteral(
                                "minimumDistance", EffectValue.Integer(3))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                RedLineDestinationOutput, EffectValueKind.Coordinate)
                        }),

                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, RedActorSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(3)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, RedLineDestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        })
                });
        }

        // -----------------------------------------------------------------------------------------
        // Decision labels
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// What each of this block's controls says when it is drawn, DERIVED from the bodies above
        /// rather than guessed: <c>HanuRedsTests</c> asks
        /// <see cref="GuardsDigital.Core.Cards.DecisionLabelRequirementQuery"/> which controls these
        /// six faces raise and holds this list against it, which is the check milestone 8.69f's ten
        /// missing labels bought.
        ///
        /// <para><b>Eight rows, two per graded Red, and Fight and Flight declares none.</b>
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> is what decides
        /// which nodes owe words - an <see cref="EffectNodeKind.Optional"/> that is not `if able`, and
        /// a `may` repeat - and the rider is neither: its straight-line prompt is a declinable
        /// SELECTION whose controls name the spaces they would move to, and the two nodes around it
        /// are `if able`, which never asks.</para>
        ///
        /// <para>The words are cut to the sixty-character bound the decision window draws in, which is
        /// the fact about the control that milestone 8.69g measured on Brynn's traps. "Your friendly
        /// hero" is printed "your teammate" for that reason, in one spelling across all four faces,
        /// because the four print one phrase and a window that spelled it two ways would be describing
        /// two.</para>
        ///
        /// <para>The member is named for this block rather than <c>DecisionLabels</c> because five
        /// blocks of his series compose faces in parallel and a partial class cannot declare one member
        /// twice; 8.71g concatenates them, and until it does <c>DecisionLabelCatalog</c> holds none of
        /// his - which is right, because what a table is FOR is a window over a card a match
        /// deals.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> RedDecisionLabels()
        {
            foreach (var face in new[]
                {
                    HanuCardContent.HelpingHand,
                    HanuCardContent.EvenTheOdds,
                    HanuCardContent.TrustedSidekick
                })
            {
                yield return DecisionArm(face, RedAdjacentBulletNode, "Target a unit adjacent to you");
            }

            yield return DecisionArm(
                HanuCardContent.HelpingHand, RedFriendlyBulletNode,
                "Target a hero in range beside your teammate");
            yield return DecisionArm(
                HanuCardContent.EvenTheOdds, RedFriendlyBulletNode,
                "Target a hero in range beside your teammate");

            // Trusted Sidekick prints the exclusion, and the exclusion is the half of the sentence a
            // player is choosing between - so it is kept whole and the subject is shortened, which is
            // the cut milestone 8.69g made on Deadfall Trap for the same reason.
            yield return DecisionArm(
                HanuCardContent.TrustedSidekick, RedFriendlyBulletNode,
                "Target a hero beside your teammate, not beside you");

            foreach (var face in new[] { HanuCardContent.Outnumber, HanuCardContent.PileOn })
            {
                yield return DecisionArm(face, RedAdjacentBulletNode, "Target a unit adjacent to you");
                yield return DecisionArm(
                    face, RedFriendlyBulletNode, "Target a minion in range beside your teammate");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Shared nodes and identities
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One bullet of a modal Attack, as an <c>attack-sequence@1</c> leaf. The leaf performs
        /// nothing: it publishes the bullet's printed targeting sentence, and the card-action authority
        /// runs a complete p.14 per chosen bullet against the board as that sequence finds it.
        /// </summary>
        private static EffectNodeDefinition RedBulletNode(
            EffectNodeId nodeId,
            AttackTargetableUnits targetableUnits,
            AttackTargetReach reach,
            AttackSequenceSubjectNarrowing subjectNarrowing)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.TargetableUnitsParameter, targetableUnits),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.ReachParameter, reach),

                // No face of his prints a line clause, so none declares one.
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),

                // RULING 19's rider: none of the four faces prints "(on different targets)", so none
                // declares it. On Trusted Sidekick the bullets are disjoint by their own words and on
                // Pile On one minion may deliberately take both - see the two factories above.
                EffectInputBinding.FromLiteral(
                    SyntheticSequenceCapabilities.DistinctTargetsParameter,
                    EffectValue.Boolean(false))
            };

            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.TargetableUnitsOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.ReachOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.LineOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.DistinctTargetsOutput, EffectValueKind.Boolean)
            };

            if (subjectNarrowing != AttackSequenceSubjectNarrowing.None)
            {
                inputs.Add(EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.SubjectNarrowingParameter, subjectNarrowing));
                outputs.Add(new EffectOutputBinding(
                    SyntheticSequenceCapabilities.SubjectNarrowingOutput, EffectValueKind.Integer));
            }

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: inputs,
                outputs: outputs);
        }

        /// <summary>The conventional effect identity for one face's Attack card text.</summary>
        public static EffectDefinitionId AttackTextEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-attack-text");
    }
}
